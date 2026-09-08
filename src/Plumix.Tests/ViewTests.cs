using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/view.dart (View, RawView, ViewAnchor, ViewCollection)
// flutter/packages/flutter/lib/src/widgets/framework.dart (RenderTreeRootElement)
// flutter/packages/flutter/lib/src/widgets/media_query.dart (MediaQuery.fromView)
// Mirrors flutter/packages/flutter/test/widgets/view_test.dart, multi_view_binding_test.dart,
// multi_view_tree_updates_test.dart and multi_view_parent_data_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class ViewTests
{
    private static FlutterView NewView(int viewId, double devicePixelRatio = 1.0, Size? physicalSize = null)
    {
        // Hosts take ids from 0 upwards and keep them while mounted; stay clear of that range.
        return new FlutterView(physicalSize ?? new Size(800, 600), devicePixelRatio, 5000 + viewId);
    }

    [Fact]
    public void View_OfAndMaybeOf_FindTheView()
    {
        FlutterView view = NewView(1);
        FlutterView? required = null;
        FlutterView? optional = null;
        using var tree = new ViewTree(new View(view, new Builder(context =>
        {
            required = View.Of(context);
            optional = View.MaybeOf(context);
            return new SizedBox(width: 1, height: 1);
        })));

        Assert.Same(view, required);
        Assert.Same(view, optional);
    }

    [Fact]
    public void View_Of_OutsideAView_ThrowsAndMaybeOfIsNull()
    {
        FlutterView? optional = null;
        FlutterError? error = null;
        using var tree = new ViewTree(new Builder(context =>
        {
            optional = View.MaybeOf(context);
            error = Assert.Throws<FlutterError>(() => View.Of(context));
            return new View(NewView(2), new SizedBox(width: 1, height: 1));
        }));

        Assert.Null(optional);
        Assert.Contains(
            "View.of() was called with a context that does not contain a View widget.",
            error!.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void View_HiddenByALookupBoundary_IsNotFound()
    {
        FlutterView? optional = null;
        FlutterError? error = null;
        using var tree = new ViewTree(new View(
            NewView(3),
            new LookupBoundary(child: new Builder(context =>
            {
                optional = View.MaybeOf(context);
                error = Assert.Throws<FlutterError>(() => View.Of(context));
                return new SizedBox(width: 1, height: 1);
            }))));

        Assert.Null(optional);
        Assert.Contains(
            "The context provided to View.of() does have a View widget ancestor, but it is hidden by a "
            + "LookupBoundary.",
            error!.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void View_NotifiesDependentsOnlyWhenTheViewIdentityChanges()
    {
        int builds = 0;
        FlutterView first = NewView(4);
        FlutterView second = NewView(5);
        var probe = new Builder(context =>
        {
            _ = View.Of(context);
            builds += 1;
            return new SizedBox(width: 1, height: 1);
        });
        using var tree = new ViewTree(new View(first, probe));

        first.UpdateMetrics(new Size(900, 700));
        tree.Update(new View(first, probe));
        Assert.Equal(1, builds);

        tree.Update(new View(second, probe));
        Assert.Equal(2, builds);
    }

    [Fact]
    public void View_PipelineOwnerOf_IsTheRootOwnerOutsideAndTheViewOwnerInside()
    {
        PipelineOwner? outside = null;
        PipelineOwner? inside = null;
        RenderObject? leaf = null;
        using var tree = new ViewTree(new Builder(context =>
        {
            outside = View.PipelineOwnerOf(context);
            return new View(NewView(6), new Builder(innerContext =>
            {
                inside = View.PipelineOwnerOf(innerContext);
                return new SizedBoxProbe(box => leaf = box);
            }));
        }));

        PipelineOwner root = RendererBinding.Instance.RootPipelineOwner;
        Assert.Same(root, outside);
        Assert.NotSame(root, inside);
        Assert.Same(leaf!.Owner, inside);
        var children = new List<PipelineOwner>();
        root.VisitChildren(children.Add);
        Assert.Single(children);
        Assert.Same(inside, children[0]);
    }

    [Fact]
    public void View_TwoViewsForOneFlutterView_ReportTheDuplicateGlobalKey()
    {
        FlutterView view = NewView(7);
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        Exception? thrown = null;
        ViewTree? tree = null;
        try
        {
            try
            {
                tree = new ViewTree(new ViewCollection(
                [
                    new View(view, new SizedBox(width: 1, height: 1)),
                    new View(view, new SizedBox(width: 1, height: 1)),
                ]));
            }
            catch (Exception exception)
            {
                thrown = exception;
            }

            string messages = string.Join(
                "\n",
                reported.Select(details => details.Exception.ToString()).Append(thrown?.ToString() ?? string.Empty));
            Assert.Contains("Multiple widgets used the same GlobalKey", messages, StringComparison.Ordinal);
        }
        finally
        {
            FlutterError.OnError = previous;
            tree?.Dispose();
        }
    }

    [Fact]
    public void ViewCollection_MayStartWithZeroViews()
    {
        using var tree = new ViewTree(new ViewCollection([]));

        Assert.NotNull(tree.Root.ChildElement);
        Assert.Null(tree.Root.RenderObject);
    }

    [Fact]
    public void ViewAnchor_ChildDoesNotSeeTheSurroundingView()
    {
        FlutterView outerView = NewView(8);
        FlutterView? insideAnchor = null;
        FlutterView? outsideAnchor = null;
        using var tree = new ViewTree(new View(
            outerView,
            new ViewAnchor(
                view: new Builder(context =>
                {
                    insideAnchor = View.MaybeOf(context);
                    return new View(NewView(9), new SizedBox(width: 1, height: 1));
                }),
                child: new Builder(context =>
                {
                    outsideAnchor = View.MaybeOf(context);
                    return new SizedBox(width: 1, height: 1);
                }))));

        Assert.Null(insideAnchor);
        Assert.Same(outerView, outsideAnchor);
    }

    [Fact]
    public void ViewAnchor_LaysOutTheParentOwnerBeforeItsChildOwners()
    {
        var log = new List<string>();
        var spy3 = new LayoutSpy("layout 3", log);
        var spy2 = new LayoutSpy("layout 2", log);
        var spy1 = new LayoutSpy("layout 1", log);
        using var tree = new ViewTree(new View(
            NewView(10),
            new ViewAnchor(
                view: new View(
                    NewView(11),
                    new ViewAnchor(
                        view: new View(NewView(12), spy3),
                        child: spy2)),
                child: spy1)));
        Assert.Equal(["layout 1", "layout 2", "layout 3"], log);

        log.Clear();
        spy3.RenderObject!.MarkNeedsLayout();
        spy2.RenderObject!.MarkNeedsLayout();
        spy1.RenderObject!.MarkNeedsLayout();
        tree.Pump();

        Assert.Equal(["layout 1", "layout 2", "layout 3"], log);
    }

    [Fact]
    public void ViewAnchor_VisitChildren_VisitsBothChildren()
    {
        FlutterView outer = NewView(13);
        FlutterView side = NewView(14);
        using var tree = new ViewTree(new View(
            outer,
            new ViewAnchor(view: new View(side, new SizedBox(width: 1, height: 1)), child: new SizedBox())));

        Element anchor = FindElement(tree.Root, element => element.Widget is MultiChildComponentWidget);
        Assert.Equal(2, CountChildren(anchor));

        tree.Update(new View(outer, new ViewAnchor(child: new SizedBox())));

        Assert.Equal(1, CountChildren(anchor));
    }

    [Fact]
    public void ViewCollection_VisitChildren_VisitsAllChildren()
    {
        FlutterView[] views = [NewView(15), NewView(16), NewView(17)];
        using var tree = new ViewTree(new ViewCollection(
            views.Select(view => (Widget)new View(view, new SizedBox())).ToList()));

        Assert.Equal(3, CountChildren(tree.Root.ChildElement!));

        tree.Update(new ViewCollection([new View(views[0], new SizedBox())]));

        Assert.Equal(1, CountChildren(tree.Root.ChildElement!));
    }

    [Fact]
    public void RenderObjectGetter_AncestorsOfAViewSeeTheRenderView()
    {
        Element? ancestor = null;
        using var tree = new ViewTree(new Builder(context =>
        {
            ancestor = (Element)context;
            return new View(NewView(18), new SizedBox());
        }));

        Assert.IsType<RenderView>(ancestor!.RenderObject);
        Assert.IsType<RenderView>(ancestor.FindRenderObject());
        Assert.Same(ancestor.RenderObject, tree.Root.RenderObject);
    }

    [Fact]
    public void RenderObjectGetter_AncestorsOfAViewCollectionGetNull()
    {
        Element? ancestor = null;
        using var tree = new ViewTree(new Builder(context =>
        {
            ancestor = (Element)context;
            return new ViewCollection([new View(NewView(19), new SizedBox())]);
        }));

        Assert.Null(ancestor!.RenderObject);
        Assert.Null(ancestor.FindRenderObject());
    }

    [Fact]
    public void RenderObjectGetter_AncestorsOfAViewAnchorSeeTheChildRenderObject()
    {
        Element? ancestor = null;
        using var tree = new ViewTree(new View(
            NewView(20),
            new Builder(context =>
            {
                ancestor = (Element)context;
                return new ViewAnchor(
                    view: new View(NewView(21), new SizedBox()),
                    child: new SizedBox(width: 3, height: 3));
            })));

        Assert.IsType<RenderConstrainedBox>(ancestor!.RenderObject);
    }

    [Fact]
    public void View_SwitchesBetweenTheDeprecatedPairAndItsOwnPipeline()
    {
        FlutterView flutterView = NewView(22);
        var renderView = new ReusableRenderView(flutterView);
        var owner = new PipelineOwner(renderView);
        RenderObject? leaf = null;
        Widget child = new SizedBoxProbe(box => leaf = box);
        using var tree = new ViewTree(new View(
            flutterView,
            child,
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: owner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: renderView));

        Assert.Same(renderView, tree.Root.RenderObject);
        Assert.Same(owner, leaf!.Owner);
        Assert.Same(owner, renderView.Owner);

        tree.Update(new View(flutterView, child));

        Assert.NotSame(renderView, tree.Root.RenderObject);
        Assert.IsType<RenderView>(tree.Root.RenderObject);
        Assert.NotSame(owner, leaf!.Owner);
        Assert.Null(renderView.Owner);

        tree.Update(new View(
            flutterView,
            child,
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: owner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: renderView));

        Assert.Same(renderView, tree.Root.RenderObject);
        Assert.Same(owner, leaf!.Owner);
    }

    [Fact]
    public void View_RejectsHalfOfTheDeprecatedPairOrAMismatchedRenderView()
    {
        FlutterView flutterView = NewView(23);
        var renderView = new ReusableRenderView(flutterView);
        var owner = new PipelineOwner(renderView);

        Assert.Throws<AssertionError>(() => new View(
            flutterView,
            new SizedBox(),
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: owner));
        Assert.Throws<AssertionError>(() => new View(
            flutterView,
            new SizedBox(),
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: renderView));
        Assert.Throws<AssertionError>(() => new View(
            NewView(24),
            new SizedBox(),
            deprecatedDoNotUseWillBeRemovedWithoutNoticePipelineOwner: owner,
            deprecatedDoNotUseWillBeRemovedWithoutNoticeRenderView: renderView));
    }

    [Fact]
    public void View_AttachesAndDetachesItsPipelineAndRenderView()
    {
        FlutterView outer = NewView(25);
        FlutterView inner = NewView(26);
        PipelineOwner? parentOwner = null;
        RenderObject? leaf = null;
        Widget WithSideView() => new View(
            outer,
            new ViewAnchor(
                view: new Builder(context =>
                {
                    parentOwner = View.PipelineOwnerOf(context);
                    return new View(inner, new SizedBoxProbe(box => leaf = box));
                }),
                child: new SizedBox()));
        using var tree = new ViewTree(WithSideView());

        RenderView rawView = RootViewOf(leaf!);
        PipelineOwner viewOwner = rawView.Owner!;
        Assert.NotSame(RendererBinding.Instance.RootPipelineOwner, parentOwner);
        Assert.Contains(rawView, RendererBinding.Instance.RenderViews);
        Assert.Contains(viewOwner, ChildrenOf(parentOwner!));

        tree.Update(new View(outer, new ViewAnchor(child: new SizedBox())));

        Assert.Null(rawView.Owner);
        Assert.DoesNotContain(rawView, RendererBinding.Instance.RenderViews);
        Assert.DoesNotContain(viewOwner, ChildrenOf(parentOwner!));
    }

    [Fact]
    public void RenderView_DoesNotUseTheSizeOfItsChildWhenTheConstraintsAreTight()
    {
        FlutterView view = NewView(27, devicePixelRatio: 3.0, physicalSize: new Size(300, 600));
        RenderObject? leaf = null;
        using var tree = new ViewTree(new View(view, new SizedBoxProbe(box => leaf = box, width: 30, height: 60)));

        RenderView renderView = RootViewOf(leaf!);
        Assert.Equal(BoxConstraints.Tight(new Size(100, 200)), renderView.Constraints);
        Assert.Equal(new Size(100, 200), renderView.Size);
        // Plumix's Focus puts a proxy box between the view and the leaf, so the view's own child
        // carries the parentUsesSize flag Dart's test reads off the leaf.
        Assert.False(renderView.Child!.DebugCanParentUseSize);
    }

    [Fact]
    public void RenderView_SizesItselfToItsChildWhenTheConstraintsAllowIt()
    {
        var view = new FlutterView(
            new Size(300, 600),
            devicePixelRatio: 3.0,
            viewId: 5028,
            physicalConstraints: BoxConstraints.Unbounded);
        RenderObject? leaf = null;
        using var tree = new ViewTree(new View(view, new SizedBoxProbe(box => leaf = box, width: 30, height: 60)));

        RenderView renderView = RootViewOf(leaf!);
        Assert.Equal(BoxConstraints.Unbounded, renderView.Constraints);
        Assert.Equal(new Size(30, 60), renderView.Size);
        Assert.True(renderView.Child!.DebugCanParentUseSize);
    }

    [Fact]
    public void RenderView_SizesItselfToItsChildWithinLooseConstraints()
    {
        var view = new FlutterView(
            new Size(300, 600),
            devicePixelRatio: 3.0,
            viewId: 5029,
            physicalConstraints: new BoxConstraints(MaxWidth: 333, MaxHeight: 666));
        RenderObject? leaf = null;
        using var tree = new ViewTree(new View(view, new SizedBoxProbe(box => leaf = box, width: 30, height: 60)));

        RenderView renderView = RootViewOf(leaf!);
        Assert.Equal(new BoxConstraints(MaxWidth: 333, MaxHeight: 666) / 3.0, renderView.Constraints);
        Assert.Equal(new Size(30, 60), renderView.Size);
    }

    [Fact]
    public void RenderView_RespectsTheConstraintsWhenTheChildWantsToBeBigger()
    {
        var view = new FlutterView(
            new Size(300, 600),
            devicePixelRatio: 3.0,
            viewId: 5030,
            physicalConstraints: new BoxConstraints(MaxWidth: 300, MaxHeight: 600));
        RenderObject? leaf = null;
        using var tree = new ViewTree(new View(
            view,
            new SizedBoxProbe(box => leaf = box, width: 3000, height: 6000)));

        RenderView renderView = RootViewOf(leaf!);
        Assert.Equal(new Size(100, 200), renderView.Size);
    }

    [Fact]
    public void ViewFocusEvents_UnfocusAndRefocusTheView()
    {
        FlutterView view = NewView(31);
        var node = new FocusNode(debugLabel: "node");
        using var tree = new ViewTree(new View(view, new Focus(child: new SizedBox(), focusNode: node)));

        node.RequestFocus();
        tree.Pump();
        Assert.True(node.HasPrimaryFocus);

        WidgetsBinding.Instance.HandleViewFocusChanged(
            new ViewFocusEvent(view.ViewId, ViewFocusState.Unfocused, ViewFocusDirection.Undefined));
        tree.Pump();
        Assert.False(node.HasPrimaryFocus);
        Assert.True(FocusManager.Instance.RootScope.HasPrimaryFocus);

        WidgetsBinding.Instance.HandleViewFocusChanged(
            new ViewFocusEvent(view.ViewId, ViewFocusState.Focused, ViewFocusDirection.Backward));
        tree.Pump();
        Assert.True(node.HasPrimaryFocus);
    }

    [Fact]
    public void View_RequestsPlatformFocusWhenAWidgetInsideItGainsFocus()
    {
        FlutterView view = NewView(32);
        var node = new FocusNode(debugLabel: "node");
        var requests = new List<ViewFocusEvent>();
        PlatformDispatcher.Instance.ViewFocusChangeRequested += requests.Add;
        try
        {
            using var tree = new ViewTree(new View(view, new Focus(child: new SizedBox(), focusNode: node)));

            node.RequestFocus();
            tree.Pump();

            ViewFocusEvent request = Assert.Single(requests);
            Assert.Equal(view.ViewId, request.ViewId);
            Assert.Equal(ViewFocusState.Focused, request.State);
            Assert.Equal(ViewFocusDirection.Forward, request.Direction);
        }
        finally
        {
            PlatformDispatcher.Instance.ViewFocusChangeRequested -= requests.Add;
        }
    }

    [Fact]
    public void View_DoesNotRequestPlatformFocusWhileAnotherViewHoldsIt()
    {
        FlutterView view = NewView(33);
        var node = new FocusNode(debugLabel: "node");
        var requests = new List<ViewFocusEvent>();
        PlatformDispatcher.Instance.ViewFocusChangeRequested += requests.Add;
        try
        {
            using var tree = new ViewTree(new View(view, new Focus(child: new SizedBox(), focusNode: node)));
            node.RequestFocus();
            tree.Pump();
            requests.Clear();

            WidgetsBinding.Instance.HandleViewFocusChanged(
                new ViewFocusEvent(view.ViewId, ViewFocusState.Unfocused, ViewFocusDirection.Undefined));
            tree.Pump();
            Assert.False(node.HasPrimaryFocus);
            Assert.Empty(requests);

            WidgetsBinding.Instance.HandleViewFocusChanged(
                new ViewFocusEvent(100, ViewFocusState.Focused, ViewFocusDirection.Forward));
            tree.Pump();
            Assert.False(node.HasPrimaryFocus);
            Assert.Empty(requests);

            node.RequestFocus();
            tree.Pump();
            ViewFocusEvent request = Assert.Single(requests);
            Assert.Equal(view.ViewId, request.ViewId);
            Assert.Equal(ViewFocusState.Focused, request.State);
            Assert.Equal(ViewFocusDirection.Forward, request.Direction);
        }
        finally
        {
            PlatformDispatcher.Instance.ViewFocusChangeRequested -= requests.Add;
        }
    }

    [Fact]
    public void View_FocusInsideANestedViewNamesOnlyTheNestedView()
    {
        FlutterView parentView = NewView(34);
        FlutterView childView = NewView(35);
        var parentNode = new FocusNode(debugLabel: "parent");
        var childNode = new FocusNode(debugLabel: "child");
        var requests = new List<ViewFocusEvent>();
        PlatformDispatcher.Instance.ViewFocusChangeRequested += requests.Add;
        try
        {
            using var tree = new ViewTree(new View(
                parentView,
                new ViewAnchor(
                    view: new View(childView, new Focus(child: new SizedBox(), focusNode: childNode)),
                    child: new Focus(child: new SizedBox(), focusNode: parentNode))));

            childNode.RequestFocus();
            tree.Pump();
            Assert.True(childNode.HasPrimaryFocus);
            Assert.All(requests, request => Assert.Equal(childView.ViewId, request.ViewId));
            Assert.NotEmpty(requests);

            requests.Clear();
            parentNode.RequestFocus();
            tree.Pump();
            Assert.True(parentNode.HasPrimaryFocus);
            Assert.All(requests, request => Assert.Equal(parentView.ViewId, request.ViewId));
            Assert.NotEmpty(requests);
        }
        finally
        {
            PlatformDispatcher.Instance.ViewFocusChangeRequested -= requests.Add;
        }
    }

    [Fact]
    public void View_InstallsAViewScopeUnderTheRootScope()
    {
        var node = new FocusNode(debugLabel: "node");
        using var tree = new ViewTree(new View(NewView(36), new Focus(child: new SizedBox(), focusNode: node)));

        // node -> the view's scope -> the view's traversal group node -> the root scope.
        FocusNode? scope = node.Parent;
        Assert.NotNull(scope);
        Assert.Equal("View Scope", scope!.DebugLabel);
        Assert.IsType<FocusScopeNode>(scope);
        FocusNode? traversalGroup = scope.Parent;
        Assert.NotNull(traversalGroup);
        Assert.Same(FocusManager.Instance.RootScope, traversalGroup!.Parent);
    }

    [Fact]
    public void RenderTreeRootElement_RejectsARenderObjectAncestor()
    {
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            using var tree = new ViewTree(new View(
                NewView(37),
                new SizedBox(child: new View(NewView(38), new SizedBox()))));

            string messages = string.Join("\n", reported.Select(details => details.Exception.ToString()));
            Assert.Contains(
                "cannot maintain an independent render tree at its current location.",
                messages,
                StringComparison.Ordinal);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void ViewCollection_UnderARenderObjectAncestor_ReportsTheSlotError()
    {
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            using var tree = new ViewTree(new View(
                NewView(39),
                new SizedBox(child: new ViewCollection([]))));

            string messages = string.Join("\n", reported.Select(details => details.Exception.ToString()));
            Assert.Contains("cannot be inserted into slot", messages, StringComparison.Ordinal);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void TreeUpdates_MovingAViewIntoAViewCollectionKeepsItsState()
    {
        FlutterView view = NewView(40);
        CounterState? state = null;
        Widget content = new Counter(reportState: s => state = s);
        using var tree = new ViewTree(new View(view, content));
        CounterState first = state!;
        first.Increment();
        tree.Pump();
        Assert.Equal(1, first.Count);

        tree.Update(new ViewCollection([new View(view, content)]));
        Assert.Same(first, state);
        Assert.Equal(1, state!.Count);
        Assert.Contains(RendererBinding.Instance.RenderViews, renderView => ReferenceEquals(renderView.FlutterView, view));

        tree.Update(new View(view, content));
        Assert.Same(first, state);
        Assert.Equal(1, state!.Count);
    }

    [Fact]
    public void TreeUpdates_ViewsInAViewCollectionUpdateIndependently()
    {
        // Unbounded views, so the boxes below size themselves instead of filling a tight view.
        var view1 = new FlutterView(new Size(800, 600), viewId: 5041, physicalConstraints: BoxConstraints.Unbounded);
        var view2 = new FlutterView(new Size(800, 600), viewId: 5042, physicalConstraints: BoxConstraints.Unbounded);
        RenderObject? leaf1 = null;
        RenderObject? leaf2 = null;
        using var tree = new ViewTree(new ViewCollection(
        [
            new View(view1, new SizedBoxProbe(box => leaf1 = box, width: 1, height: 1)),
            new View(view2, new SizedBoxProbe(box => leaf2 = box, width: 2, height: 2)),
        ]));
        Assert.Equal(new Size(1, 1), ((RenderBox)leaf1!).Size);
        Assert.Equal(new Size(2, 2), ((RenderBox)leaf2!).Size);

        tree.Update(new ViewCollection(
        [
            new View(view1, new SizedBoxProbe(box => leaf1 = box, width: 3, height: 3)),
            new ViewCollection([new View(view2, new SizedBoxProbe(box => leaf2 = box, width: 4, height: 4))]),
        ]));

        Assert.Equal(new Size(3, 3), ((RenderBox)leaf1!).Size);
        Assert.Equal(new Size(4, 4), ((RenderBox)leaf2!).Size);
    }

    [Fact]
    public void ParentData_IsAppliedAcrossAViewAnchor()
    {
        RenderObject? leaf = null;
        Widget Build() => new View(
            NewView(43),
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Stack(
                [
                new Positioned(
                    right: 0,
                    bottom: 0,
                    child: new ViewAnchor(
                        view: new View(NewView(44), new SizedBox()),
                        child: new SizedBoxProbe(box => leaf = box, width: 10, height: 10))),
                ])));
        using var tree = new ViewTree(Build());
        var box = (RenderBox)leaf!;
        Assert.Equal(new Point(790, 590), ((BoxParentData)box.parentData!).offset);

        tree.Update(Build());

        box = (RenderBox)leaf!;
        Assert.Equal(new Point(790, 590), ((BoxParentData)box.parentData!).offset);
    }

    [Fact]
    public void MediaQueryFromView_DerivesTheViewDataFromTheView()
    {
        var view = new FlutterView(new Size(800, 600), devicePixelRatio: 2.0, viewId: 5045);
        view.UpdateMetrics(padding: new Thickness(0, 40, 0, 0), viewInsets: new Thickness(0, 0, 0, 100));
        MediaQueryData? data = null;
        using var tree = new ViewTree(new View(view, new Builder(context =>
        {
            data = MediaQuery.Of(context);
            return new SizedBox();
        })));

        Assert.Equal(new Size(400, 300), data!.Size);
        Assert.Equal(2.0, data.DevicePixelRatio);
        Assert.Equal(new Thickness(0, 20, 0, 0), data.Padding);
        Assert.Equal(new Thickness(0, 0, 0, 50), data.ViewInsets);
        Assert.Equal(5045, data.ViewId);
    }

    [Fact]
    public void MediaQueryFromView_TakesThePlatformDataFromTheSurroundingMediaQuery()
    {
        FlutterView view = NewView(46);
        MediaQueryData? data = null;
        using var tree = new ViewTree(new MediaQuery(
            data: new MediaQueryData(
                Size: new Size(1, 1),
                PlatformBrightness: PlatformBrightness.Dark,
                HighContrast: true,
                TextScaleFactor: 2.0),
            child: new View(view, new Builder(context =>
            {
                data = MediaQuery.Of(context);
                return new SizedBox();
            }))));

        Assert.Equal(new Size(800, 600), data!.Size);
        Assert.Equal(PlatformBrightness.Dark, data.PlatformBrightness);
        Assert.True(data.HighContrast);
        Assert.Equal(2.0, data.TextScaleFactor);
    }

    [Fact]
    public void MediaQueryFromView_UpdatesWhenTheMetricsChange()
    {
        FlutterView view = NewView(47);
        var sizes = new List<Size>();
        using var tree = new ViewTree(new View(view, new Builder(context =>
        {
            sizes.Add(MediaQuery.SizeOf(context));
            return new SizedBox();
        })));
        Assert.Equal([new Size(800, 600)], sizes);

        view.UpdateMetrics(physicalSize: new Size(400, 200));
        WidgetsBinding.Instance.HandleMetricsChanged();
        tree.Pump();

        Assert.Equal([new Size(800, 600), new Size(400, 200)], sizes);
        Assert.Equal(new Size(400, 200), ((RenderView)tree.Root.RenderObject!).Size);
    }

    private static RenderView RootViewOf(RenderObject renderObject)
    {
        RenderObject current = renderObject;
        while (current.Parent is { } parent)
        {
            current = parent;
        }

        return (RenderView)current;
    }

    private static Element FindElement(Element root, Func<Element, bool> predicate)
    {
        Element? found = null;
        void Visit(Element element)
        {
            if (found is not null)
            {
                return;
            }

            if (predicate(element))
            {
                found = element;
                return;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        return found ?? throw new InvalidOperationException("No element matched.");
    }

    private static int CountChildren(Element element)
    {
        int count = 0;
        element.VisitChildren(_ => count += 1);
        return count;
    }

    private static List<PipelineOwner> ChildrenOf(PipelineOwner owner)
    {
        var children = new List<PipelineOwner>();
        owner.VisitChildren(children.Add);
        return children;
    }

    /// <summary>
    /// A widget tree mounted the way <c>runWidget</c> mounts one: a <see cref="RootWidget"/> with no
    /// render-object host, so every render tree below it is rooted by a <see cref="View"/> under
    /// the <see cref="RendererBinding.RootPipelineOwner"/>.
    /// </summary>
    private sealed class ViewTree : IDisposable
    {
        private readonly BuildOwner _owner = new();

        public ViewTree(Widget widget)
        {
            Root = new RootWidget(child: widget).Attach(_owner);
            Pump();
        }

        public RootElement Root { get; }

        public void Update(Widget widget)
        {
            new RootWidget(child: widget).Attach(_owner, Root);
            Pump();
        }

        /// <summary>Dart's <c>drawFrame</c>: build, layout, finalize, then the microtasks the frame queued.</summary>
        public void Pump()
        {
            _owner.FlushBuild();
            RendererBinding.Instance.RootPipelineOwner.FlushLayout();
            _owner.FinalizeTree();
            Scheduler.FlushMicrotasks();
            _owner.FlushBuild();
            RendererBinding.Instance.RootPipelineOwner.FlushLayout();
        }

        public void Dispose()
        {
            Root.Unmount();
            Scheduler.FlushMicrotasks();
        }
    }

    private sealed class SizedBoxProbe : SingleChildRenderObjectWidget
    {
        private readonly Action<RenderObject> _report;
        private readonly double _width;
        private readonly double _height;

        public SizedBoxProbe(Action<RenderObject> report, double width = 1, double height = 1)
        {
            _report = report;
            _width = width;
            _height = height;
        }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            var box = new RenderConstrainedBox(BoxConstraints.TightFor(width: _width, height: _height));
            _report(box);
            return box;
        }

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderConstrainedBox)renderObject).AdditionalConstraints =
                BoxConstraints.TightFor(width: _width, height: _height);
            _report(renderObject);
        }
    }

    private sealed class LayoutSpy : LeafRenderObjectWidget
    {
        private readonly string _label;
        private readonly List<string> _log;

        public LayoutSpy(string label, List<string> log)
        {
            _label = label;
            _log = log;
        }

        public RenderLayoutSpy? RenderObject { get; private set; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return RenderObject = new RenderLayoutSpy(_label, _log);
        }
    }

    private sealed class RenderLayoutSpy(string label, List<string> log) : RenderBox
    {
        protected override void PerformLayout()
        {
            log.Add(label);
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class Counter : StatefulWidget
    {
        public Counter(Action<CounterState> reportState)
        {
            ReportState = reportState;
        }

        public Action<CounterState> ReportState { get; }

        public override State CreateState() => new CounterState();
    }

    private sealed class CounterState : State
    {
        public int Count { get; private set; }

        public void Increment() => SetState(() => Count += 1);

        public override Widget Build(BuildContext context)
        {
            ((Counter)StateWidget).ReportState(this);
            return new SizedBox(width: Count + 1, height: 1);
        }
    }
}
