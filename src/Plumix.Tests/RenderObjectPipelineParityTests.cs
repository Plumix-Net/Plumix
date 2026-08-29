using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for the pieces of `rendering/object.dart` that Flutter's own
/// `rendering/object_test.dart`, `rendering/layout_builder_mutations_test.dart` and
/// `rendering/repaint_boundary_test.dart` assert.
/// </summary>
public class RenderObjectPipelineParityTests
{
    [Fact]
    public void RedepthChildren_GivesEveryDescendantADepthGreaterThanItsParent()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var inner = new PassThroughRenderBox(leaf);
        var outer = new PassThroughRenderBox(inner);
        var view = new RenderView { Child = outer };

        Assert.True(outer.Depth > view.Depth);
        Assert.True(inner.Depth > outer.Depth);
        Assert.True(leaf.Depth > inner.Depth);
    }

    [Fact]
    public void Attach_RecursesIntoDescendants_AndDetachReversesIt()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var inner = new PassThroughRenderBox(leaf);
        var view = new RenderView { Child = inner };
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
        var owner = new PipelineOwner(new RenderView());
        child.Attach(owner);
        var parent = new ToggleVisitingRenderBox(child) { VisitsChild = true };

        Assert.Throws<AssertionError>(() => parent.Attach(owner));
    }

    [Fact]
    public void Detach_RejectsAChildThatIsAlreadyDetached()
    {
        var child = new SizeRenderBox(new Size(10, 10));
        var owner = new PipelineOwner(new RenderView());
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
        var view = new RenderView { Child = parent };
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
        var view = new RenderView { Child = child };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        Assert.False(child.HasRelayoutBoundaryStateForTest);
        Assert.Contains<RenderObject>(view, pipeline.NodesNeedingLayoutForTest);
        Assert.DoesNotContain(child, pipeline.NodesNeedingLayoutForTest);
    }

    [Fact]
    public void ScheduleInitialLayout_MakesTheRootItsOwnRelayoutBoundary()
    {
        var view = new RenderView { Child = new SizeRenderBox(new Size(10, 10)) };
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
        var view = new RenderView { Child = new SizeRenderBox(new Size(10, 10)) };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

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
        var view = new RenderView { Child = row };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));

        Assert.Equal(new Point(-20, 0), first.LocalToGlobal(default, second));
        Assert.Equal(new Point(20, 0), second.LocalToGlobal(default, first));
    }

    [Fact]
    public void BoxConstraints_DebugAssertIsValid_NamesTheOffendingRule()
    {
        FlutterError nonNormalized = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: 200, MaxWidth: 100).DebugAssertIsValid());
        Assert.Contains("non-normalized width constraints", nonNormalized.Message, StringComparison.Ordinal);

        FlutterError nan = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinHeight: double.NaN).DebugAssertIsValid());
        Assert.Contains("NaN values in MinHeight", nan.Message, StringComparison.Ordinal);

        FlutterError applied = Assert.Throws<FlutterError>(
            () => new BoxConstraints(MinWidth: double.PositiveInfinity, MaxWidth: double.PositiveInfinity)
                .DebugAssertIsValid(isAppliedConstraint: true));
        Assert.Contains("an infinite minimum width constraint", applied.Message, StringComparison.Ordinal);

        Assert.True(new BoxConstraints(0, 100, 0, 100).DebugAssertIsValid(isAppliedConstraint: true));
    }

    [Fact]
    public void PaintingContext_RepaintCompositedChild_ReusesTheBoundaryLayer()
    {
        var leaf = new SizeRenderBox(new Size(10, 10));
        var boundary = new CountingRepaintBoundary(leaf);
        var view = new RenderView { Child = boundary };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

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

        protected override void PerformLayout() => Size = Constraints.Constrain(_size);

        public override void Paint(PaintingContext ctx, Point offset)
        {
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

        public override bool IsRepaintBoundary => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            base.Paint(ctx, offset);
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
