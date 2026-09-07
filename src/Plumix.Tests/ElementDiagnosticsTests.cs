using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// (Element._debugCheckStateIsActiveForAncestorLookup, describeElement/describeWidget/
//  describeMissingAncestor/describeOwnershipChain, debugGetCreatorChain,
//  _ElementDiagnosticableTreeNode, DebugCreator). The cases mirror Flutter's own
//  test/widgets/dispose_ancestor_lookup_test.dart and test/foundation/diagnostics_json_test.dart.

namespace Plumix.Tests;

public sealed class ElementDiagnosticsTests
{
    // ---- Ancestor lookups from dispose() -------------------------------------------------
    // Flutter's dispose_ancestor_lookup_test.dart asserts a FlutterError for each guarded
    // lookup, and asserts that a dispose() which does not look anything up stays quiet.

    [DebugOnlyFact]
    public void DependOnInherited_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.DependOnInherited<TestInherited>());
    }

    [DebugOnlyFact]
    public void GetElementForInheritedWidgetOfExactType_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(
            context => context.GetElementForInheritedWidgetOfExactType<TestInherited>());
    }

    [DebugOnlyFact]
    public void FindAncestorWidgetOfExactType_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.FindAncestorWidgetOfExactType<TestInherited>());
    }

    [DebugOnlyFact]
    public void FindAncestorStateOfType_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.FindAncestorStateOfType<State>());
    }

    [DebugOnlyFact]
    public void FindRootAncestorStateOfType_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.FindRootAncestorStateOfType<State>());
    }

    [DebugOnlyFact]
    public void FindAncestorRenderObjectOfType_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.FindAncestorRenderObjectOfType<RenderObject>());
    }

    [DebugOnlyFact]
    public void VisitAncestorElements_CalledFromDispose_ThrowsError()
    {
        AssertLookupFromDisposeThrows(context => context.VisitAncestorElements(static _ => true));
    }

    [Fact]
    public void Dispose_DoesNotUnconditionallyThrow()
    {
        bool disposeCalled = false;
        var harness = new WidgetRenderHarness(new DisposeProbe(_ => disposeCalled = true));
        harness.Pump(new Size(40, 40));
        harness.Update(new SizedBox(width: 40, height: 40));
        harness.Pump(new Size(40, 40));
        harness.Dispose();

        Assert.True(disposeCalled);
    }

    [DebugOnlyFact]
    public void AncestorLookupError_NamesTheCauseAndTheFix()
    {
        FlutterError error = CaptureLookupFromDispose(
            context => context.FindAncestorWidgetOfExactType<TestInherited>());

        Assert.Contains("Looking up a deactivated widget's ancestor is unsafe.", error.Message);
        Assert.Contains("no longer stable", error.Message);
        Assert.Contains("DidChangeDependencies()", error.Message);
    }

    // ---- describe* -----------------------------------------------------------------------

    [Fact]
    public void DescribeElementAndDescribeWidget_UseTheErrorPropertyStyle()
    {
        using var harness = new WidgetRenderHarness(new ContextProbe(out Func<BuildContext> read));
        harness.Pump(new Size(40, 40));
        BuildContext context = read();

        DiagnosticsNode element = context.DescribeElement("The element being rebuilt was");
        DiagnosticsNode widget = context.DescribeWidget("The widget being rebuilt was");

        Assert.Equal("The element being rebuilt was", element.Name);
        Assert.Equal(DiagnosticsTreeStyle.ErrorProperty, element.Style);
        Assert.Same(context, element.Value);
        Assert.Equal("The widget being rebuilt was", widget.Name);
        Assert.Equal(DiagnosticsTreeStyle.ErrorProperty, widget.Style);
        Assert.Same(context, widget.Value);

        // An explicit style overrides the default, as Dart's optional `style:` argument does.
        DiagnosticsNode flat = context.DescribeElement("x", DiagnosticsTreeStyle.Flat);
        Assert.Equal(DiagnosticsTreeStyle.Flat, flat.Style);
    }

    [Fact]
    public void DescribeOwnershipChain_CarriesTheCreatorChain()
    {
        using var harness = new WidgetRenderHarness(new ContextProbe(out Func<BuildContext> read));
        harness.Pump(new Size(40, 40));
        var element = (Element)read();

        DiagnosticsNode chain = element.DescribeOwnershipChain("The ownership chain is");

        Assert.Equal("The ownership chain is", chain.Name);
        Assert.Equal(element.DebugGetCreatorChain(10), chain.Value);
    }

    [Fact]
    public void DescribeMissingAncestor_ListsTheAncestorsThatWereSearched()
    {
        using var harness = new WidgetRenderHarness(new ContextProbe(out Func<BuildContext> read));
        harness.Pump(new Size(40, 40));

        List<DiagnosticsNode> information = read().DescribeMissingAncestor(typeof(TestInherited));

        Assert.Equal(2, information.Count);
        Assert.Equal(
            "The specific widget that could not find a TestInherited ancestor was",
            information[0].Name);
        Assert.Equal(DiagnosticsTreeStyle.ErrorProperty, information[0].Style);
        var block = Assert.IsType<DiagnosticsBlock>(information[1]);
        Assert.Equal("The ancestors of this widget were", block.Name);
        Assert.NotEmpty(block.GetChildren());
    }

    [Fact]
    public void DescribeMissingAncestor_AtTheRoot_SaysThereAreNoAncestors()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox());
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        List<DiagnosticsNode> information = root.DescribeMissingAncestor(typeof(TestInherited));

        Assert.Equal(2, information.Count);
        Assert.IsType<ErrorDescription>(information[1]);
        Assert.Contains("This widget is the root of the tree", information[1].ToString());
        Assert.Contains("\"TestInherited\" ancestor", information[1].ToString());

        root.Unmount();
    }

    // ---- creator chain -------------------------------------------------------------------

    [DebugOnlyFact]
    public void DebugGetCreatorChain_JoinsAncestorsWithAnArrowAndTruncatesWithAnEllipsis()
    {
        using var harness = new WidgetRenderHarness(
            new TestInherited(new Padding(EdgeInsets.All(1), new ContextProbe(out Func<BuildContext> read))));
        harness.Pump(new Size(40, 40));
        var element = (Element)read();

        string full = element.DebugGetCreatorChain(10);
        Assert.StartsWith("ContextProbe", full, StringComparison.Ordinal);
        Assert.Contains(" ← ", full);
        Assert.Contains("Padding", full);
        Assert.Contains("TestInherited", full);
        Assert.DoesNotContain("⋯", full);

        // A chain cut short by the limit ends in the midline ellipsis.
        string clipped = element.DebugGetCreatorChain(2);
        Assert.Equal(3, clipped.Split(" ← ").Length);
        Assert.EndsWith("⋯", clipped, StringComparison.Ordinal);

        // debugGetDiagnosticChain is the same walk, as elements.
        List<Element> chain = element.DebugGetDiagnosticChain();
        Assert.Same(element, chain[0]);
        Assert.Null(chain[^1].Parent);
    }

    // ---- ToStringShort / diagnostics tree -------------------------------------------------

    [DebugOnlyFact]
    public void ToStringShort_IsTheWidgetsUntilTheElementIsDefunct()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox(key: new ValueKey<string>("box")));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Element child = root.ChildElement!;
        Assert.Equal(child.Widget.ToStringShort(), child.ToStringShort());
        Assert.Contains("SizedBox", child.ToStringShort());

        root.Unmount();

        Assert.EndsWith("(DEFUNCT)", child.ToStringShort(), StringComparison.Ordinal);
        Assert.Contains(Diagnostics.ShortHash(child), child.ToStringShort());
    }

    [DebugOnlyFact]
    public void DebugFillProperties_ReportsDepthWidgetAndDirtyState()
    {
        using var harness = new WidgetRenderHarness(new ContextProbe(out Func<BuildContext> read));
        harness.Pump(new Size(40, 40));
        var element = (Element)read();

        var builder = new DiagnosticPropertiesBuilder();
        element.DebugFillProperties(builder);

        Assert.Equal(DiagnosticsTreeStyle.Dense, builder.DefaultDiagnosticsTreeStyle);
        Assert.Contains(builder.Properties, node => node.Name == "depth" && Equals(node.Value, element.Depth));
        Assert.Contains(
            builder.Properties,
            node => node.Name == "widget" && ReferenceEquals(node.Value, element.Widget));
        Assert.Contains(builder.Properties, node => node.Name == "dirty");
        Assert.Contains(builder.Properties, node => node.Name == "key");
    }

    [DebugOnlyFact]
    public void DebugFillProperties_ListsInheritedDependencies()
    {
        using var harness = new WidgetRenderHarness(
            new TestInherited(new InheritedReader(out Func<BuildContext> read)));
        harness.Pump(new Size(40, 40));

        var builder = new DiagnosticPropertiesBuilder();
        ((Element)read()).DebugFillProperties(builder);

        DiagnosticsNode dependencies =
            Assert.Single(builder.Properties, node => node.Name == "dependencies");
        Assert.Contains("TestInherited", dependencies.ToString());
    }

    [DebugOnlyFact]
    public void DebugFillProperties_OnADefunctElement_ReportsNoWidget()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox());
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Element child = root.ChildElement!;
        root.Unmount();

        var builder = new DiagnosticPropertiesBuilder();
        child.DebugFillProperties(builder);

        DiagnosticsNode widget = Assert.Single(builder.Properties, node => node.Name == "widget");
        Assert.Null(widget.Value);
        Assert.Contains("no widget", widget.ToString());
    }

    [DebugOnlyFact]
    public void DebugDescribeChildren_ReturnsANodePerChildElement()
    {
        using var harness = new WidgetRenderHarness(
            new Column(children: [new SizedBox(width: 1), new SizedBox(width: 2)]));
        harness.Pump(new Size(40, 40));

        Element column = harness.FindElement(element => element.Widget is Column)!;
        List<DiagnosticsNode> children = column.DebugDescribeChildren();

        Assert.Equal(2, children.Count);
        Assert.All(children, node => Assert.IsAssignableFrom<Element>(node.Value));

        // ToStringDeep, which DiagnosticableTree gives Element, walks the same children.
        string deep = column.ToStringDeep();
        Assert.Contains("Column", deep);
        Assert.Contains("SizedBox", deep);
    }

    // ---- element diagnostics json ---------------------------------------------------------

    [Fact]
    public void ElementDiagnosticsJson_IncludesWidgetRuntimeTypeAndIsNotStateful()
    {
        var element = (Element)new SizedBox().CreateElement();

        Dictionary<string, object?> json =
            element.ToDiagnosticsNode().ToJsonMap(DiagnosticsSerializationDelegate.Create());

        Assert.Equal("SizedBox", json["widgetRuntimeType"]);
        Assert.Equal(false, json["stateful"]);
    }

    [Fact]
    public void StatefulElementDiagnosticsJson_IsStateful()
    {
        var element = (Element)new DisposeProbe(static _ => { }).CreateElement();

        Dictionary<string, object?> json =
            element.ToDiagnosticsNode().ToJsonMap(DiagnosticsSerializationDelegate.Create());

        Assert.Equal("DisposeProbe", json["widgetRuntimeType"]);
        Assert.Equal(true, json["stateful"]);
    }

    [DebugOnlyFact]
    public void ElementDiagnosticsJson_OnADefunctElement_OmitsTheWidgetRuntimeType()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox());
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Element child = root.ChildElement!;
        root.Unmount();

        Dictionary<string, object?> json =
            child.ToDiagnosticsNode().ToJsonMap(DiagnosticsSerializationDelegate.Create());

        Assert.False(json.ContainsKey("widgetRuntimeType"));
        Assert.Equal(false, json["stateful"]);
    }

    [DebugOnlyFact]
    public void StatefulElement_DebugFillProperties_ReportsItsState()
    {
        using var harness = new WidgetRenderHarness(new DisposeProbe(static _ => { }));
        harness.Pump(new Size(40, 40));

        Element element = harness.FindElement(e => e.Widget is DisposeProbe)!;
        var builder = new DiagnosticPropertiesBuilder();
        element.DebugFillProperties(builder);

        Assert.Contains(builder.Properties, node => node.Name == "state" && node.Value is not null);
    }

    // ---- DebugCreator ---------------------------------------------------------------------

    [DebugOnlyFact]
    public void RenderObjectElement_StampsItselfOnItsRenderObjectAsTheDebugCreator()
    {
        using var harness = new WidgetRenderHarness(new SizedBox(width: 10, height: 10));
        harness.Pump(new Size(40, 40));

        Element element = harness.FindElement(e => e.Widget is SizedBox)!;
        var creator = Assert.IsType<DebugCreator>(element.RenderObject!.DebugCreator);
        Assert.Same(element, creator.Element);
        Assert.Equal(element.DebugGetCreatorChain(12), creator.ToString());

        // The stamp is refreshed on update, as Dart's _debugUpdateRenderObjectOwner is.
        harness.Update(new SizedBox(width: 20, height: 20));
        harness.Pump(new Size(40, 40));
        var updated = Assert.IsType<DebugCreator>(element.RenderObject!.DebugCreator);
        Assert.Same(element, updated.Element);
    }

    // ---- error paths that read the describe* members ---------------------------------------

    [DebugOnlyFact]
    public void MediaQueryOf_WithoutAnAncestor_NamesTheWidgetAndItsOwnershipChain()
    {
        FlutterError? error = null;
        using var harness = new WidgetRenderHarness(new ContextProbe(context =>
        {
            error = Record.Exception(() => MediaQuery.Of(context)) as FlutterError;
        }));
        harness.Pump(new Size(40, 40));

        Assert.NotNull(error);
        Assert.Contains("No MediaQuery widget ancestor found.", error!.Message);
        Assert.Contains("The specific widget that could not find a MediaQuery ancestor was", error.Message);
        Assert.Contains("The ownership chain for the affected widget is", error.Message);
    }

    // ---- the scroll offset no longer travels through dispose --------------------------------

    [Fact]
    public void ScrollableInsidePageStorage_DisposesWithoutAnAncestorLookup()
    {
        // Plumix used to save the offset from ScrollableState.Dispose, which reaches PageStorage
        // through findAncestorWidgetOfExactType. Dart saves it from ScrollPosition.didEndScroll,
        // so tearing a scrollable down must not touch the ancestor chain at all.
        var bucket = new PageStorageBucket();
        var controller = new ScrollController();
        var harness = new WidgetRenderHarness(BuildScrollable(bucket, controller));
        harness.Pump(new Size(100, 100));
        controller.JumpTo(120);
        harness.Pump(new Size(100, 100));

        harness.Dispose();

        // The offset was persisted at scroll end, not at dispose.
        var restoreController = new ScrollController();
        using var restored = new WidgetRenderHarness(BuildScrollable(bucket, restoreController));
        restored.Pump(new Size(100, 100));
        Assert.Equal(120.0, restoreController.Offset);
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static Widget BuildScrollable(PageStorageBucket bucket, ScrollController controller)
    {
        return new PageStorage(
            bucket,
            new SingleChildScrollView(
                key: new PageStorageKey<string>("diagnostics-scroll"),
                controller: controller,
                child: new SizedBox(width: 100, height: 500)));
    }

    private static void AssertLookupFromDisposeThrows(Action<BuildContext> lookup)
    {
        _ = CaptureLookupFromDispose(lookup);
    }

    private static FlutterError CaptureLookupFromDispose(Action<BuildContext> lookup)
    {
        bool disposeCalled = false;
        Exception? caught = null;
        var harness = new WidgetRenderHarness(new TestInherited(new DisposeProbe(context =>
        {
            disposeCalled = true;
            caught = Record.Exception(() => lookup(context));
        })));
        harness.Pump(new Size(40, 40));

        harness.Update(new SizedBox(width: 40, height: 40));
        harness.Pump(new Size(40, 40));
        harness.Dispose();

        Assert.True(disposeCalled, "State.Dispose must have run.");
        return Assert.IsType<FlutterError>(caught);
    }

    private sealed class TestInherited : InheritedWidget
    {
        public TestInherited(Widget child, Key? key = null) : base(key)
        {
            Child = child;
        }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
    }

    private sealed class InheritedReader : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;

        public InheritedReader(out Func<BuildContext> read)
        {
            BuildContext? captured = null;
            _capture = context => captured = context;
            read = () => captured ?? throw new InvalidOperationException("The reader has not built yet.");
        }

        public override Widget Build(BuildContext context)
        {
            _ = context.DependOnInherited<TestInherited>();
            _capture(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ContextProbe : StatelessWidget
    {
        private readonly Action<BuildContext> _callback;

        public ContextProbe(Action<BuildContext> callback, Key? key = null) : base(key)
        {
            _callback = callback;
        }

        public ContextProbe(out Func<BuildContext> read)
        {
            BuildContext? captured = null;
            _callback = context => captured = context;
            read = () => captured ?? throw new InvalidOperationException("The probe has not built yet.");
        }

        public override Widget Build(BuildContext context)
        {
            _callback(context);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class DisposeProbe : StatefulWidget
    {
        public DisposeProbe(Action<BuildContext> onDispose, Key? key = null) : base(key)
        {
            OnDispose = onDispose;
        }

        public Action<BuildContext> OnDispose { get; }

        public override State CreateState() => new DisposeProbeState();
    }

    private sealed class DisposeProbeState : State
    {
        public override void Dispose()
        {
            ((DisposeProbe)StateWidget).OnDispose(Context);
            base.Dispose();
        }

        public override Widget Build(BuildContext context) => new SizedBox(width: 1, height: 1);
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public RootElement(RenderView renderView, Widget widget) : base(widget)
        {
            _renderView = renderView;
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            _renderView.Child = child as RenderBox;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
            }
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly RootElement _root;
        private bool _disposed;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Update(Widget widget)
        {
            _root.Update(widget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public Element? FindElement(Func<Element, bool> predicate)
        {
            Element? found = null;
            void Visit(Element element)
            {
                if (found is null && predicate(element))
                {
                    found = element;
                }

                element.VisitChildren(Visit);
            }

            // The root element carries the widget under test as its own configuration, so the
            // search starts below it, like Dart's `find.byType`.
            _root.VisitChildren(Visit);
            return found;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _root.Unmount();
        }
    }
}
