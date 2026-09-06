using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliverOpacityTests : IDisposable
{
    public SliverOpacityTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SliverOpacity_ExposesFlutterDefaultsAndValidatesOpacity()
    {
        var opacity = new SliverOpacity(opacity: 0.4);

        Assert.Equal(0.4, opacity.Opacity);
        Assert.Null(opacity.Child);
        Assert.False(opacity.AlwaysIncludeSemantics);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverOpacity(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverOpacity(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverOpacity(double.NaN));
    }

    [Fact]
    public void SliverOpacityWidget_CreatesRenderObjectAndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverOpacity(
            opacity: 0.25,
            sliver: new SliverToBoxAdapter(new SizedBox(width: 20, height: 30))));
        Mount(root, owner);

        var renderOpacity = RequireRenderObject<RenderSliverOpacity>(root.ChildElement);
        Assert.Equal(0.25, renderOpacity.Opacity);
        Assert.False(renderOpacity.AlwaysIncludeSemantics);

        root.Update(new SliverOpacity(
            opacity: 0.75,
            alwaysIncludeSemantics: true,
            sliver: new SliverToBoxAdapter(new SizedBox(width: 20, height: 30))));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderSliverOpacity>(root.ChildElement);
        Assert.Same(renderOpacity, updated);
        Assert.Equal(0.75, updated.Opacity);
        Assert.True(updated.AlwaysIncludeSemantics);

        root.Unmount();
    }

    [Fact]
    public void RenderSliverOpacity_PreservesGeometryHitTestingAndSemanticsPolicy()
    {
        var box = new HitTestRenderBox(new Size(100, 80));
        var child = new RenderSliverToBoxAdapter(box);
        var opacity = new RenderSliverOpacity(opacity: 0.0, sliver: child);
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 60,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 60,
            RemainingCacheExtent: 60);

        opacity.LayoutWithSliverConstraints(constraints);

        Assert.Equal(child.Geometry, opacity.Geometry);
        Assert.Equal(80, opacity.Geometry.ScrollExtent);
        Assert.Equal(60, opacity.Geometry.PaintExtent);
        Assert.True(opacity.HitTest(new BoxHitTestResult(), new Point(10, 10)));

        int semanticsVisits = 0;
        opacity.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);

        opacity.AlwaysIncludeSemantics = true;
        opacity.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);
    }

    [Fact]
    public void RenderSliverOpacity_SkipsTransparentPaintAndUsesOpacityLayerWhenVisible()
    {
        var box = new PaintTrackingRenderBox(new Size(100, 80));
        var child = new RenderSliverToBoxAdapter(box);
        var opacity = new RenderSliverOpacity(opacity: 0.0, sliver: child);
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        viewport.Insert(opacity);
        var renderView = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.False(opacity.NeedsCompositing);
        Assert.Equal(0, box.PaintCount);
        Assert.Null(opacity._layer);

        opacity.Opacity = 0.4;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(opacity.NeedsCompositing);
        Assert.Equal(1, box.PaintCount);
        var layer = Assert.IsType<OpacityLayer>(opacity._layer);
        Assert.Equal(0.4, layer.Opacity);

        opacity.Opacity = 0.0;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.False(opacity.NeedsCompositing);
        Assert.Equal(1, box.PaintCount);
        Assert.Null(opacity._layer);
    }

    [Fact]
    public void RenderSliverAnimatedOpacity_TracksAnimationWithoutWidgetRebuilds()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        var box = new PaintTrackingRenderBox(new Size(100, 80));
        var child = new RenderSliverToBoxAdapter(box);
        var opacity = new RenderSliverAnimatedOpacity(controller, sliver: child);
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        viewport.Insert(opacity);
        var renderView = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.False(opacity.NeedsCompositing);
        Assert.Equal(0, box.PaintCount);

        controller.SetValue(0.5);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(opacity.NeedsCompositing);
        Assert.Equal(1, box.PaintCount);
        var layer = Assert.IsType<OpacityLayer>(opacity._layer);
        Assert.Equal(128, layer.Alpha);

        controller.SetValue(0.0);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.False(opacity.NeedsCompositing);
        Assert.Equal(1, box.PaintCount);
        Assert.Null(opacity._layer);
    }

    [Fact]
    public void SliverAnimatedOpacity_InterpolatesFromCurrentValueAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverAnimatedOpacity(
            opacity: 0.0,
            duration: TimeSpan.FromMilliseconds(200),
            sliver: new SliverToBoxAdapter(new SizedBox(width: 20, height: 30)),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new SliverAnimatedOpacity(
            opacity: 1.0,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            sliver: new SliverToBoxAdapter(new SizedBox(width: 20, height: 30)),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderSliverAnimatedOpacity>(root.ChildElement);
        Assert.InRange(halfway.Opacity.Value, 0.01, 0.99);
        double interruptedOpacity = halfway.Opacity.Value;

        root.Update(new SliverAnimatedOpacity(
            opacity: 0.2,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            alwaysIncludeSemantics: true,
            sliver: new SliverToBoxAdapter(new SizedBox(width: 20, height: 30)),
            onEnd: () => completed++));
        owner.FlushBuild();

        var interrupted = RequireRenderObject<RenderSliverAnimatedOpacity>(root.ChildElement);
        Assert.Equal(interruptedOpacity, interrupted.Opacity.Value, precision: 6);
        Assert.True(interrupted.AlwaysIncludeSemantics);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        var finished = RequireRenderObject<RenderSliverAnimatedOpacity>(root.ChildElement);
        Assert.Equal(0.2, finished.Opacity.Value, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void SliverAnimatedOpacity_ExposesFlutterDefaultsAndValidatesArguments()
    {
        var opacity = new SliverAnimatedOpacity(
            opacity: 0.4,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(0.4, opacity.Opacity);
        Assert.Equal(TimeSpan.FromMilliseconds(200), opacity.Duration);
        Assert.Null(opacity.Sliver);
        Assert.Equal(Curves.Linear(0.3), opacity.Curve(0.3));
        Assert.Null(opacity.OnEnd);
        Assert.False(opacity.AlwaysIncludeSemantics);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAnimatedOpacity(
            opacity: -0.1,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAnimatedOpacity(
            opacity: 1.1,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAnimatedOpacity(
            opacity: 0.5,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class HitTestRenderBox(Size size) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class PaintTrackingRenderBox(Size size) : RenderBox
    {
        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount += 1;
            ctx.Canvas.DrawRectangle(Brushes.Blue, null, new Rect(offset, Size));
        }
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
