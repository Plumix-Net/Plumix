using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

public sealed class FlowRepaintBoundaryTests
{
    [Fact]
    public void RenderFlow_UsesDelegateSizeAndPerChildConstraints()
    {
        var first = new TestRenderBox(new Size(60, 40));
        var second = new TestRenderBox(new Size(18, 12));
        var flowDelegate = new TestFlowDelegate(
            size: new Size(120, 80),
            childConstraints: new BoxConstraints(MaxWidth: 30, MaxHeight: 25));
        var flow = new RenderFlow(flowDelegate, children: [first, second]);

        flow.Layout(new BoxConstraints(MaxWidth: 200, MaxHeight: 100));

        Assert.Equal(new Size(120, 80), flow.Size);
        Assert.Equal(new Size(30, 25), first.Size);
        Assert.Equal(new Size(18, 12), second.Size);
        Assert.Equal(new BoxConstraints(MaxWidth: 30, MaxHeight: 25), first.LastConstraints);
        Assert.Equal([0, 1], flowDelegate.ConstraintIndices);
        Assert.Equal(first.Size, flow.GetChildSize(0));
        Assert.Equal(second.Size, flow.GetChildSize(1));
        Assert.Null(flow.GetChildSize(-1));
        Assert.Null(flow.GetChildSize(2));
    }

    [Fact]
    public void RenderFlow_PaintsInDelegateOrderAndHitTestsReversePaintOrder()
    {
        var first = new TestRenderBox(new Size(20, 20), hitTestSelf: true);
        var second = new TestRenderBox(new Size(20, 20), hitTestSelf: true);
        Matrix translation = Matrix.CreateTranslation(15, 10);
        var flowDelegate = new TestFlowDelegate(
            size: new Size(80, 60),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)),
            paint: context =>
            {
                context.PaintChild(0, translation);
                context.PaintChild(1, translation, opacity: 0.0);
            });
        var flow = new RenderFlow(flowDelegate, children: [first, second]);
        var renderView = new RenderView { Child = flow };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(80, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, first.PaintCount);
        Assert.Equal(0, second.PaintCount);
        var hitResult = new BoxHitTestResult();
        Assert.True(flow.HitTest(hitResult, new Point(20, 15)));
        Assert.Same(second, hitResult.Path[0].Target);

        Matrix? firstSemanticsTransform = null;
        Matrix? secondSemanticsTransform = null;
        int semanticsIndex = 0;
        flow.VisitChildrenForSemantics((_, _, transform) =>
        {
            if (semanticsIndex++ == 0)
            {
                firstSemanticsTransform = transform;
            }
            else
            {
                secondSemanticsTransform = transform;
            }
        });
        Assert.Equal(translation, firstSemanticsTransform);
        Assert.Equal(translation, secondSemanticsTransform);

        var flowLayer = Assert.IsType<OffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
        var clipLayer = Assert.IsType<ClipRectLayer>(Assert.Single(flowLayer.Children));
        Assert.Equal(new Rect(0, 0, 80, 60), clipLayer.ClipRect);
    }

    [Fact]
    public void RenderFlow_RepaintListenableSkipsBuildAndLayout()
    {
        using var repaint = new ChangeNotifier();
        var child = new TestRenderBox(new Size(20, 20));
        var flowDelegate = new TestFlowDelegate(
            size: new Size(80, 60),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)),
            repaint: repaint);
        var flow = new RenderFlow(flowDelegate, children: [child]);
        var renderView = new RenderView { Child = flow };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(80, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        int layoutCount = child.LayoutCount;
        int delegatePaintCount = flowDelegate.PaintCount;

        repaint.NotifyListeners();

        Assert.True(pipeline.NeedsPaint);
        pipeline.FlushPaint();
        Assert.Equal(layoutCount, child.LayoutCount);
        Assert.Equal(delegatePaintCount + 1, flowDelegate.PaintCount);
    }

    [Fact]
    public void RenderFlow_DelegateReplacementChoosesRelayoutBeforeRepaint()
    {
        var child = new TestRenderBox(new Size(20, 20));
        var initial = new TestFlowDelegate(
            size: new Size(80, 60),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)));
        var flow = new RenderFlow(initial, children: [child], clipBehavior: Clip.None);
        var renderView = new RenderView { Child = flow };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(80, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        int layoutCount = child.LayoutCount;
        var repaintOnly = new TestFlowDelegate(
            size: new Size(80, 60),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)),
            shouldRepaint: true);
        flow.Delegate = repaintOnly;
        pipeline.FlushPaint();
        Assert.Equal(layoutCount, child.LayoutCount);
        Assert.Equal(1, repaintOnly.PaintCount);

        var relayout = new TestFlowDelegate(
            size: new Size(80, 60),
            childConstraints: BoxConstraints.Loose(new Size(12, 12)),
            shouldRelayout: true,
            shouldRepaint: true);
        flow.Delegate = relayout;
        pipeline.FlushLayout(new Size(80, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Equal(layoutCount + 1, child.LayoutCount);
        Assert.Equal(new Size(12, 12), child.Size);
        Assert.Equal(0, relayout.ShouldRepaintCallCount);
    }

    [Fact]
    public void RenderFlow_RejectsDuplicatePaintAndInvalidOpacity()
    {
        var child = new TestRenderBox(new Size(20, 20));
        var duplicate = new TestFlowDelegate(
            size: new Size(20, 20),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)),
            paint: context =>
            {
                context.PaintChild(0);
                context.PaintChild(0);
            });
        var flow = new RenderFlow(duplicate, children: [child]);
        flow.Layout(BoxConstraints.Tight(new Size(20, 20)));
        var paintingContext = new PaintingContext(new OffsetLayer());

        Assert.Throws<InvalidOperationException>(() => flow.Paint(paintingContext, default));

        var invalidOpacity = new TestFlowDelegate(
            size: new Size(20, 20),
            childConstraints: BoxConstraints.Loose(new Size(20, 20)),
            paint: context => context.PaintChild(0, opacity: 1.1));
        flow.Delegate = invalidOpacity;
        Assert.Throws<ArgumentOutOfRangeException>(() => flow.Paint(paintingContext, default));
    }

    [Fact]
    public void RenderRepaintBoundary_IsAlwaysACompositedBoundary()
    {
        var child = new TestRenderBox(new Size(24, 16));
        var boundary = new RenderRepaintBoundary(child);
        var renderView = new RenderView { Child = boundary };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(24, 16));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(boundary.IsRepaintBoundary);
        Assert.Equal(new Size(24, 16), boundary.Size);
        Assert.IsType<OffsetLayer>(Assert.Single(pipeline.RootLayer.Children));
    }

    private sealed class TestFlowDelegate : FlowDelegate
    {
        private readonly Size _size;
        private readonly BoxConstraints _childConstraints;
        private readonly Action<FlowPaintingContext>? _paint;
        private readonly bool _shouldRelayout;
        private readonly bool _shouldRepaint;

        public TestFlowDelegate(
            Size size,
            BoxConstraints childConstraints,
            Action<FlowPaintingContext>? paint = null,
            IListenable? repaint = null,
            bool shouldRelayout = false,
            bool shouldRepaint = false) : base(repaint)
        {
            _size = size;
            _childConstraints = childConstraints;
            _paint = paint;
            _shouldRelayout = shouldRelayout;
            _shouldRepaint = shouldRepaint;
        }

        public List<int> ConstraintIndices { get; } = [];

        public int PaintCount { get; private set; }

        public int ShouldRepaintCallCount { get; private set; }

        public override Size GetSize(BoxConstraints constraints) => _size;

        public override BoxConstraints GetConstraintsForChild(int index, BoxConstraints constraints)
        {
            ConstraintIndices.Add(index);
            return _childConstraints;
        }

        public override void PaintChildren(FlowPaintingContext context)
        {
            PaintCount++;
            if (_paint is not null)
            {
                _paint(context);
                return;
            }

            for (int index = 0; index < context.ChildCount; index++)
            {
                context.PaintChild(index);
            }
        }

        public override bool ShouldRelayout(FlowDelegate oldDelegate) => _shouldRelayout;

        public override bool ShouldRepaint(FlowDelegate oldDelegate)
        {
            ShouldRepaintCallCount++;
            return _shouldRepaint;
        }
    }

    private sealed class TestRenderBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly bool _hitTestSelf;

        public TestRenderBox(Size desiredSize, bool hitTestSelf = false)
        {
            _desiredSize = desiredSize;
            _hitTestSelf = hitTestSelf;
        }

        public int LayoutCount { get; private set; }

        public int PaintCount { get; private set; }

        public BoxConstraints LastConstraints { get; private set; }

        protected override void PerformLayout()
        {
            LayoutCount++;
            LastConstraints = Constraints;
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override bool HitTestSelf(Point position) => _hitTestSelf;

        public override void Paint(PaintingContext context, Point offset)
        {
            PaintCount++;
        }
    }
}
