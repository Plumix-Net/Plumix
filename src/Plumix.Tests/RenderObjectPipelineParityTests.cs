using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for the pieces of `rendering/object.dart` that Flutter's own
/// `rendering/object_test.dart`, `rendering/layout_builder_mutations_test.dart` and
/// `rendering/repaint_boundary_test.dart` assert.
/// </summary>
public class RenderObjectPipelineParityTests
{
    [Fact]
    public void PrepareInitialFrame_QueuesLayoutAndPaintWithoutRequestingAFrame()
    {
        int requests = 0;
        var view = new RenderView(new FlutterView(new Size(100, 100)))
        {
            Configuration = new ViewConfiguration(logicalConstraints: BoxConstraints.Tight(new Size(100, 100))),
        };
        var owner = new PipelineOwner(onNeedVisualUpdate: () => requests += 1) { RootNode = view };

        view.PrepareInitialFrame();

        Assert.Same(view, Assert.Single(owner.NodesNeedingLayoutForTest));
        Assert.Same(view, Assert.Single(owner.NodesNeedingPaintForTest));
        Assert.Equal(0, requests);
        owner.FlushLayout();
        owner.FlushCompositingBits();
        owner.FlushPaint();
        Assert.Empty(owner.NodesNeedingLayoutForTest);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Equal(0, requests);
    }

    [Fact]
    public void MarkNeedsLayout_RequestsOneFrameAndCoalescesRepeatedRootRequests()
    {
        int requests = 0;
        (PipelineOwner owner, RenderView view, _) = PumpView(() => requests += 1);
        requests = 0;

        view.MarkNeedsLayout();
        owner.RequestLayout();
        view.MarkNeedsLayout();

        Assert.Equal(1, requests);
        Assert.Same(view, Assert.Single(owner.NodesNeedingLayoutForTest));
        owner.FlushLayout();
        Assert.Empty(owner.NodesNeedingLayoutForTest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoundaryPaintAndLayerUpdates_RequestOneFrame(bool layerUpdateOnly)
    {
        int requests = 0;
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView(() => requests += 1);
        Layer? layer = view.DebugLayer;
        int previousPaints = child.PaintCount;
        requests = 0;

        if (layerUpdateOnly)
        {
            view.MarkNeedsCompositedLayerUpdate();
            view.MarkNeedsCompositedLayerUpdate();
        }
        else
        {
            view.MarkNeedsPaint();
            owner.RequestPaint();
        }

        Assert.Equal(1, requests);
        Assert.Same(view, Assert.Single(owner.NodesNeedingPaintForTest));
        owner.FlushPaint();
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.False(view.NeedsPaint);
        Assert.False(view.NeedsCompositedLayerUpdate);
        Assert.Same(layer, view.DebugLayer);
        Assert.Equal(previousPaints + (layerUpdateOnly ? 0 : 1), child.PaintCount);
    }

    [Fact]
    public void MarkNeedsCompositingBitsUpdate_DoesNotRequestAFrame()
    {
        int requests = 0;
        (PipelineOwner owner, RenderView view, _) = PumpView(() => requests += 1);
        requests = 0;

        view.MarkNeedsCompositingBitsUpdate();
        owner.RequestCompositingBitsUpdate();
        Assert.True(view.NeedsCompositingBitsUpdate);
        Assert.Equal(0, requests);

        owner.FlushCompositingBits();
        Assert.False(view.NeedsCompositingBitsUpdate);
        Assert.Equal(0, requests);
    }

    [Fact]
    public void FlushPaint_DoesNotDiscoverAnUnqueuedDirtyRoot()
    {
        var root = new SizeRenderBox(new Size(10, 10));
        var owner = new PipelineOwner { RootNode = root };

        owner.FlushPaint();

        Assert.True(root.NeedsPaint);
        Assert.Equal(0, root.PaintCount);
        Assert.Empty(owner.NodesNeedingPaintForTest);
    }

    [Fact]
    public void FlushPaint_DrainsADetachedRootLayerOnceAndLeavesItDirty()
    {
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView();
        Layer layer = Assert.IsType<TransformLayer>(view.DebugLayer);
        int previousPaints = child.PaintCount;
        layer.Detach();
        view.MarkNeedsPaint();

        owner.FlushPaint();
        owner.FlushPaint();

        Assert.True(view.NeedsPaint);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Equal(previousPaints, child.PaintCount);
        Assert.False(owner.DebugDoingPaint);
    }

    [Fact]
    public void FlushPaint_SkipsAQueuedRootThatNoLongerBelongsToTheOwner()
    {
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView();
        int previousPaints = child.PaintCount;
        view.MarkNeedsPaint();
        owner.RootNode = null;

        Assert.Empty(owner.NodesNeedingPaintForTest);

        owner.FlushPaint();

        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Equal(previousPaints, child.PaintCount);
        Assert.True(view.NeedsPaint);
    }

    [Fact]
    public void FlushPaint_SkipsAFormerRootDisposedBeforeTheFlush()
    {
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView();
        int previousPaints = child.PaintCount;
        view.MarkNeedsPaint();
        owner.RootNode = null;
        view.Dispose();

        owner.FlushPaint();

        Assert.Null(view.DebugLayer);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Equal(previousPaints, child.PaintCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Dispose_CancelsQueuedPaintBeforeReleasingTheLayer(bool detachFirst, bool layerUpdateOnly)
    {
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView();
        int previousPaints = child.PaintCount;
        if (layerUpdateOnly)
        {
            view.MarkNeedsCompositedLayerUpdate();
        }
        else
        {
            view.MarkNeedsPaint();
        }

        Assert.Same(view, Assert.Single(owner.NodesNeedingPaintForTest));
        if (detachFirst)
        {
            owner.RootNode = null;
        }

        view.Dispose();

        Assert.Null(view.DebugLayer);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        owner.FlushPaint();
        Assert.Equal(previousPaints, child.PaintCount);
        Assert.False(owner.DebugDoingPaint);
        if (!detachFirst)
        {
            owner.RootNode = null;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DetachAndReattach_RequeuesDirtyPaintAndRetainsTheLayer(bool changeOwner, bool layerUpdateOnly)
    {
        // Flutter's reattach_test.dart: "objects can be detached and re-attached: paint".
        (PipelineOwner owner, RenderView view, SizeRenderBox child) = PumpView();
        Layer? layer = view.DebugLayer;
        int previousPaints = child.PaintCount;
        if (layerUpdateOnly)
        {
            view.MarkNeedsCompositedLayerUpdate();
        }
        else
        {
            view.MarkNeedsPaint();
        }
        owner.RootNode = null;

        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Equal(!layerUpdateOnly, view.NeedsPaint);
        Assert.Equal(layerUpdateOnly, view.NeedsCompositedLayerUpdate);
        Assert.Same(layer, view.DebugLayer);

        PipelineOwner nextOwner = changeOwner ? new PipelineOwner() : owner;
        nextOwner.RootNode = view;

        Assert.Same(view, Assert.Single(nextOwner.NodesNeedingPaintForTest));
        if (changeOwner)
        {
            owner.FlushPaint();
            Assert.Equal(previousPaints, child.PaintCount);
        }

        nextOwner.FlushPaint();

        Assert.Equal(previousPaints + (layerUpdateOnly ? 0 : 1), child.PaintCount);
        Assert.Same(layer, view.DebugLayer);
        Assert.False(view.NeedsPaint);
        Assert.False(view.NeedsCompositedLayerUpdate);
        Assert.Empty(nextOwner.NodesNeedingPaintForTest);
    }

    [Fact]
    public void FlushPaint_TeardownCancelsAFormerRootAlreadyInTheSnapshot()
    {
        var boundary = new CountingRepaintBoundary(new SizeRenderBox(new Size(10, 10)));
        var view = new RenderView(new FlutterView(new Size(100, 100)))
        {
            Child = boundary,
            Configuration = new ViewConfiguration(logicalConstraints: BoxConstraints.Tight(new Size(100, 100))),
        };
        var owner = new PipelineOwner { RootNode = view };
        view.PrepareInitialFrame();
        owner.FlushLayout();
        owner.FlushCompositingBits();
        owner.FlushPaint();
        boundary.OnPaint = () =>
        {
            owner.RootNode = null;
            view.Dispose();
        };
        boundary.MarkNeedsPaint();
        view.MarkNeedsPaint();
        Assert.Equal(2, owner.NodesNeedingPaintForTest.Count);

        owner.FlushPaint();

        Assert.Equal(2, boundary.PaintCount);
        Assert.True(view.DebugDisposed);
        Assert.Null(view.DebugLayer);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.False(owner.DebugDoingPaint);
        boundary.OnPaint = null;
        owner.FlushPaint();
        Assert.Equal(2, boundary.PaintCount);
    }

    [Fact]
    public void ScheduleInitialPaint_IsAvailableOnANonViewRootAndDoesNotRequestAFrame()
    {
        int requests = 0;
        var child = new SizeRenderBox(new Size(10, 10));
        var root = new CountingRepaintBoundary(child);
        var owner = new PipelineOwner(onNeedVisualUpdate: () => requests += 1) { RootNode = root };
        root.Layout(BoxConstraints.Tight(new Size(10, 10)));
        var layer = new OffsetLayer();
        layer.Attach(owner);
        requests = 0;

        root.ScheduleInitialPaint(layer);

        Assert.Equal(0, requests);
        Assert.Same(root, Assert.Single(owner.NodesNeedingPaintForTest));
        owner.FlushCompositingBits();
        owner.FlushPaint();
        Assert.Equal(1, root.PaintCount);
        Assert.Empty(owner.NodesNeedingPaintForTest);
        Assert.Same(layer, root.DebugLayer);
    }

    [Fact]
    public void ReplaceRootLayer_OnANonViewRootDetachesOldLayerAndRequestsOneFrame()
    {
        int requests = 0;
        var root = new CountingRepaintBoundary(new SizeRenderBox(new Size(10, 10)));
        var owner = new PipelineOwner(onNeedVisualUpdate: () => requests += 1) { RootNode = root };
        root.Layout(BoxConstraints.Tight(new Size(10, 10)));
        var oldLayer = new OffsetLayer();
        oldLayer.Attach(owner);
        root.ScheduleInitialPaint(oldLayer);
        owner.FlushCompositingBits();
        owner.FlushPaint();
        var replacement = new OffsetLayer();
        replacement.Attach(owner);
        requests = 0;

        root.ReplaceRootLayer(replacement);

        Assert.False(oldLayer.Attached);
        Assert.Same(replacement, root.DebugLayer);
        Assert.Equal(1, requests);
        Assert.Same(root, Assert.Single(owner.NodesNeedingPaintForTest));
        owner.FlushPaint();
        Assert.Equal(2, root.PaintCount);
    }

    private static (PipelineOwner Owner, RenderView View, SizeRenderBox Child) PumpView(Action? onUpdate = null)
    {
        var child = new SizeRenderBox(new Size(10, 10));
        var view = new RenderView(new FlutterView(new Size(100, 100)))
        {
            Child = child,
            Configuration = new ViewConfiguration(logicalConstraints: BoxConstraints.Tight(new Size(100, 100))),
        };
        var owner = new PipelineOwner(onNeedVisualUpdate: onUpdate) { RootNode = view };
        view.PrepareInitialFrame();
        owner.FlushLayout();
        owner.FlushCompositingBits();
        owner.FlushPaint();
        return (owner, view, child);
    }

    [Fact]
    public void RedepthChildren_GivesEveryDescendantADepthGreaterThanItsParent()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var inner = new PassThroughRenderBox(leaf);
        var outer = new PassThroughRenderBox(inner);
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = outer };

        Assert.True(outer.Depth > view.Depth);
        Assert.True(inner.Depth > outer.Depth);
        Assert.True(leaf.Depth > inner.Depth);
    }

    [Fact]
    public void Attach_RecursesIntoDescendants_AndDetachReversesIt()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var inner = new PassThroughRenderBox(leaf);
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = inner };
        var pipeline = new PipelineOwner(view);

        pipeline.Attach(view);
        Assert.Same(pipeline, leaf.Owner);

        view.Child = null;
        Assert.Null(inner.Owner);
        Assert.Null(leaf.Owner);
    }

    [Fact]
    public void Attach_RejectsAChildThatIsAlreadyAttached()
    {
        var child = new SizeRenderBox(new Size(10, 10));
        var owner = new PipelineOwner(new RenderView(new FlutterView(new Size(800, 600))));
        child.Attach(owner);
        var parent = new ToggleVisitingRenderBox(child) { VisitsChild = true };

        Assert.Throws<AssertionError>(() => parent.Attach(owner));
    }

    [Fact]
    public void Detach_RejectsAChildThatIsAlreadyDetached()
    {
        var child = new SizeRenderBox(new Size(10, 10));
        var owner = new PipelineOwner(new RenderView(new FlutterView(new Size(800, 600))));
        var parent = new ToggleVisitingRenderBox(child);
        parent.Attach(owner);
        parent.VisitsChild = true;

        Assert.Throws<AssertionError>(parent.Detach);
    }

    [Fact]
    public void DropChild_ClearsParentDataAndTheRelayoutBoundaryState()
    {
        var child = new SizeRenderBox(new Size(10, 10));
        var parent = new PassThroughRenderBox(child);
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = parent };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.NotNull(child.ParentDataForTest);
        Assert.True(child.HasRelayoutBoundaryStateForTest);

        parent.Child = null;

        Assert.Null(child.ParentDataForTest);
        Assert.False(child.HasRelayoutBoundaryStateForTest);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void Attach_DoesNotEnqueueANodeThatHasNeverBeenLaidOut()
    {
        // Flutter's `attach` skips the layout branch when `_isRelayoutBoundary` is null, because
        // `scheduleInitialLayout` owns the bootstrap.
        var child = new SizeRenderBox(new Size(10, 10));
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = child };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        Assert.False(child.HasRelayoutBoundaryStateForTest);
        Assert.Contains<RenderObject>(view, pipeline.NodesNeedingLayoutForTest);
        Assert.DoesNotContain(child, pipeline.NodesNeedingLayoutForTest);
    }

    [Fact]
    public void ScheduleInitialLayout_MakesTheRootItsOwnRelayoutBoundary()
    {
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = new SizeRenderBox(new Size(10, 10)) };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        Assert.True(view.HasRelayoutBoundaryState);
        Assert.Contains<RenderObject>(view, pipeline.NodesNeedingLayoutForTest);
    }

    [Fact]
    public void Layout_ReportsAPerformLayoutFailureInsteadOfThrowing()
    {
        var box = new ThrowingRenderBox();
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            box.Layout(BoxConstraints.Tight(new Size(10, 10)));
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(reported);
        Assert.Equal("rendering library", details.Library);
        Assert.Contains("during performLayout()", details.Context?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MarkNeedsPaint_OnAParentlessNonBoundary_DoesNotEnqueueIt()
    {
        // "We don't add ourselves to `_nodesNeedingPaint` in this case, because the root is always
        // told to paint regardless." — `RenderObject.markNeedsPaint`.
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = new SizeRenderBox(new Size(10, 10)) };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();

        var orphan = new SizeRenderBox(new Size(10, 10));
        orphan.MarkNeedsPaint();

        Assert.DoesNotContain(orphan, pipeline.NodesNeedingPaintForTest);
    }

    [Fact]
    public void GetTransformTo_WalksToTheCommonAncestorAndInvertsTheTargetHalf()
    {
        var first = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var second = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var row = new RenderFlex(
            children: [first, second],
            direction: Axis.Horizontal,
            textDirection: TextDirection.Ltr);
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = row };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(new Point(-20, 0), first.LocalToGlobal(default, second));
        Assert.Equal(new Point(20, 0), second.LocalToGlobal(default, first));
    }

    [DebugOnlyFact]
    public void BoxConstraints_DebugAssertIsValid_NamesTheOffendingRule()
    {
        FlutterError nonNormalized = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: 200, MaxWidth: 100).DebugAssertIsValid());
        Assert.Contains("non-normalized width constraints", nonNormalized.Message, StringComparison.Ordinal);

        FlutterError nan = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinHeight: double.NaN).DebugAssertIsValid());
        Assert.Contains("BoxConstraints has a NaN value in minHeight.", nan.Message, StringComparison.Ordinal);

        FlutterError nans = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: double.NaN, MaxWidth: double.NaN, MinHeight: double.NaN)
                .DebugAssertIsValid());
        Assert.Contains(
            "BoxConstraints has NaN values in minWidth, maxWidth, and minHeight.",
            nans.Message,
            StringComparison.Ordinal);

        FlutterError twoNans = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: double.NaN, MaxHeight: double.NaN).DebugAssertIsValid());
        Assert.Contains(
            "BoxConstraints has NaN values in minWidth and maxHeight.",
            twoNans.Message,
            StringComparison.Ordinal);

        FlutterError applied = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: double.PositiveInfinity, MaxWidth: double.PositiveInfinity)
                .DebugAssertIsValid(isAppliedConstraint: true));
        Assert.Contains("BoxConstraints forces an infinite width.", applied.Message, StringComparison.Ordinal);

        Assert.True(new BoxConstraints(0, 100, 0, 100).DebugAssertIsValid(isAppliedConstraint: true));
    }

    [Fact]
    public void PaintingContext_RepaintCompositedChild_ReusesTheBoundaryLayer()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var boundary = new CountingRepaintBoundary(leaf);
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = boundary };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();

        Layer? firstLayer = boundary.DebugLayer;
        Assert.NotNull(firstLayer);
        Assert.Equal(1, boundary.PaintCount);

        boundary.MarkNeedsPaint();
        PaintingContext.RepaintCompositedChild(boundary);

        Assert.Equal(2, boundary.PaintCount);
        Assert.Same(firstLayer, boundary.DebugLayer);
    }

    [Fact]
    public void ContainerRenderObjectMixin_RemoveAll_DropsEveryChildAtOnce()
    {
        var flex = new RenderFlex(direction: Axis.Horizontal, textDirection: TextDirection.Ltr);
        var first = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)));
        var second = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)));
        flex.AddAll([first, second]);
        Assert.Equal(2, flex.ChildCount);

        flex.AddAll(null);
        Assert.Equal(2, flex.ChildCount);

        flex.RemoveAll();

        Assert.Equal(0, flex.ChildCount);
        Assert.Null(flex.FirstChild);
        Assert.Null(flex.LastChild);
        Assert.Null(first.Parent);
        Assert.Null(second.Parent);
    }

    [Fact]
    public void DiagnosticsDebugCreator_CarriesTheCreatorHidden()
    {
        object creator = new();
        var property = new DiagnosticsDebugCreator(creator);

        Assert.Equal("debugCreator", property.Name);
        Assert.Same(creator, property.Value);
        Assert.Equal(DiagnosticLevel.Hidden, property.Level);
    }

    private sealed class SizeRenderBox : RenderBox
    {
        private readonly Size _size;

        public SizeRenderBox(Size size) => _size = size;

        public IParentData? ParentDataForTest => parentData;

        public bool HasRelayoutBoundaryStateForTest => HasRelayoutBoundaryState;

        public int PaintCount { get; private set; }

        protected override void PerformLayout() => Size = Constraints.Constrain(_size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
        }
    }

    private class PassThroughRenderBox : RenderProxyBox
    {
        public PassThroughRenderBox(RenderBox? child) => Child = child;

        public bool HasRelayoutBoundaryStateForTest => HasRelayoutBoundaryState;
    }

    private sealed class ToggleVisitingRenderBox(RenderObject child) : RenderBox
    {
        public bool VisitsChild { get; set; }

        public override void VisitChildren(Action<RenderObject> visitor)
        {
            if (VisitsChild)
            {
                visitor(child);
            }
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class CountingRepaintBoundary : PassThroughRenderBox
    {
        public CountingRepaintBoundary(RenderBox child) : base(child)
        {
        }

        public int PaintCount { get; private set; }

        public Action? OnPaint { get; set; }

        public override bool IsRepaintBoundary => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            base.Paint(ctx, offset);
            OnPaint?.Invoke();
        }
    }

    private sealed class ThrowingRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
            throw new InvalidOperationException("layout boom");
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
