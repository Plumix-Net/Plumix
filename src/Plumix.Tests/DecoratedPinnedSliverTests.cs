using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class DecoratedPinnedSliverTests
{
    [Fact]
    public void DecoratedSliver_ExposesFlutterDefaultsAndUpdatesRenderObject()
    {
        var first = new RecordingDecoration([], []);
        var second = new RecordingDecoration([], []);
        var widget = new DecoratedSliver(first);

        Assert.Same(first, widget.Decoration);
        Assert.Equal(DecorationPosition.Background, widget.Position);
        Assert.Null(widget.Child);
        Assert.Throws<ArgumentNullException>(() => new DecoratedSliver(null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new DecoratedSliver(
            decoration: first,
            sliver: new SliverToBoxAdapter(new SizedBox(height: 20))));
        Mount(root, owner);
        var render = RequireRenderObject<RenderDecoratedSliver>(root.ChildElement);

        root.Update(new DecoratedSliver(
            decoration: second,
            position: DecorationPosition.Foreground,
            sliver: new SliverToBoxAdapter(new SizedBox(height: 20))));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderDecoratedSliver>(root.ChildElement);
        Assert.Same(render, updated);
        Assert.Same(second, updated.Decoration);
        Assert.Equal(DecorationPosition.Foreground, updated.Position);
        root.Unmount();
    }

    [Theory]
    [InlineData(DecorationPosition.Background, "decoration,child")]
    [InlineData(DecorationPosition.Foreground, "child,decoration")]
    public void RenderDecoratedSliver_PaintsMaxExtentInRequestedOrder(
        DecorationPosition position,
        string expectedOrder)
    {
        var order = new List<string>();
        var calls = new List<(Point Offset, Size Size)>();
        var decoration = new RecordingDecoration(order, calls);
        var child = new PaintTrackingSliver(order, scrollExtent: 120.0);
        var decorated = new RenderDecoratedSliver(decoration, position, sliver: child);
        var viewport = new RenderViewport(offset: new TestViewportOffset(30.0));
        viewport.Insert(decorated);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(expectedOrder.Split(','), order);
        (Point offset, Size size) = Assert.Single(calls);
        Assert.Equal(new Point(0, -30), offset);
        Assert.Equal(new Size(100, 120), size);
        Assert.Equal(child.Geometry, decorated.Geometry);
    }

    [Fact]
    public void RenderDecoratedSliver_InfiniteExtentPaintsThroughCacheExtent()
    {
        var calls = new List<(Point Offset, Size Size)>();
        var decoration = new RecordingDecoration([], calls);
        var child = new PaintTrackingSliver([], double.PositiveInfinity);
        var decorated = new RenderDecoratedSliver(decoration, sliver: child);
        var viewport = new RenderViewport(
            offset: new TestViewportOffset(40.0),
            scrollCacheExtent: ScrollCacheExtent.Pixels(50.0));
        viewport.Insert(decorated);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        (Point offset, Size size) = Assert.Single(calls);
        Assert.Equal(new Point(0, -40), offset);
        Assert.Equal(150, size.Height, precision: 3);
    }

    [Fact]
    public void PinnedHeaderSliver_WrapsChildInExplicitSemanticsContainer()
    {
        var widget = new PinnedHeaderSliver();
        Assert.Null(widget.Child);

        var owner = new BuildOwner();
        var root = new TestRootElement(new PinnedHeaderSliver(
            child: new SizedBox(width: 80, height: 44)));
        Mount(root, owner);

        var render = RequireRenderObject<RenderPinnedHeaderSliver>(root.ChildElement);
        var semantics = Assert.IsType<RenderSemanticsAnnotations>(render.Child);
        var configuration = new SemanticsConfiguration();
        semantics.InvokeDescribeSemanticsConfiguration(configuration);
        Assert.True(configuration.IsSemanticBoundary);
        Assert.True(configuration.ExplicitChildNodes);
        root.Unmount();
    }

    [Fact]
    public void RenderPinnedHeaderSliver_UsesMeasuredExtentAndPinnedGeometry()
    {
        var child = new FixedSizeRenderBox(new Size(100, 60));
        var header = new RenderPinnedHeaderSliver { Child = child };
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 80,
            RemainingPaintExtent: 200,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 200,
            RemainingCacheExtent: 200,
            Overlap: 12);

        header.LayoutWithSliverConstraints(constraints);

        Assert.Equal(60, header.ChildExtent);
        Assert.Equal(60, header.Geometry.ScrollExtent);
        Assert.Equal(0, header.Geometry.LayoutExtent);
        Assert.Equal(60, header.Geometry.PaintExtent);
        Assert.Equal(12, header.Geometry.PaintOrigin);
        Assert.Equal(60, header.Geometry.MaxPaintExtent);
        Assert.Equal(60, header.Geometry.MaxScrollObstructionExtent);
        Assert.True(header.Geometry.HasVisualOverflow);
        Assert.Equal(default, ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderPinnedHeaderSliver_TracksChildExtentChangesWithoutADelegate()
    {
        var child = new MutableSizeRenderBox(new Size(100, 60));
        var header = new RenderPinnedHeaderSliver { Child = child };
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 20,
            RemainingPaintExtent: 200,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 200,
            RemainingCacheExtent: 200);
        header.LayoutWithSliverConstraints(constraints);
        Assert.Equal(60, header.Geometry.ScrollExtent);
        Assert.Equal(40, header.Geometry.LayoutExtent);

        child.UpdateSize(new Size(100, 90));
        header.LayoutWithSliverConstraints(constraints);

        Assert.Equal(90, header.Geometry.ScrollExtent);
        Assert.Equal(70, header.Geometry.LayoutExtent);
        Assert.Equal(90, header.Geometry.MaxScrollObstructionExtent);
    }

    [Fact]
    public void RenderPinnedHeaderSliver_UsesWidthAsExtentForHorizontalViewports()
    {
        var header = new RenderPinnedHeaderSliver
        {
            Child = new FixedSizeRenderBox(new Size(70, 40)),
        };
        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis: Axis.Horizontal,
            ScrollOffset: 25,
            RemainingPaintExtent: 160,
            CrossAxisExtent: 40,
            ViewportMainAxisExtent: 160,
            RemainingCacheExtent: 160,
            AxisDirection: AxisDirection.Right));

        Assert.Equal(70, header.ChildExtent);
        Assert.Equal(45, header.Geometry.LayoutExtent);
        Assert.Equal(70, header.Geometry.PaintExtent);
        Assert.Equal(new Size(70, 40), header.Child!.Size);
    }

    [Fact]
    public void RenderViewport_UsesLayoutExtentSoFollowingSliverScrollsBehindPinnedHeader()
    {
        var header = new RenderPinnedHeaderSliver
        {
            Child = new FixedSizeRenderBox(new Size(100, 60)),
        };
        var bodyBox = new FixedSizeRenderBox(new Size(100, 300));
        var body = new RenderSliverToBoxAdapter(bodyBox);
        var viewport = new RenderViewport(offset: new TestViewportOffset(80));
        viewport.Insert(header);
        viewport.Insert(body, after: header);
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 200));

        Assert.Equal(0, header.Geometry.LayoutExtent);
        Assert.Equal(60, header.Geometry.PaintExtent);
        Assert.Equal(60, body.ConstraintsForSliver.Overlap);
        Assert.Equal(new Point(0, 0), ((SliverPhysicalParentData)body.parentData!).offset);
        Assert.Equal(new Point(0, -20), ((BoxParentData)bodyBox.parentData!).offset);
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

    private sealed record RecordingDecoration(
        List<string> Order,
        List<(Point Offset, Size Size)> Calls) : Decoration
    {
        public override BoxPainter CreateBoxPainter(Action? onChanged = null)
        {
            return new RecordingBoxPainter(Order, Calls, onChanged);
        }
    }

    private sealed class RecordingBoxPainter(
        List<string> order,
        List<(Point Offset, Size Size)> calls,
        Action? onChanged) : BoxPainter(onChanged)
    {
        public override void Paint(
            PaintingContext context,
            Point offset,
            ImageConfiguration configuration)
        {
            order.Add("decoration");
            calls.Add((offset, configuration.Size ?? default));
        }
    }

    private sealed class PaintTrackingSliver(List<string> order, double scrollExtent) : RenderSliver
    {
        protected override void PerformSliverLayout(SliverConstraints constraints)
        {
            double paintExtent = Math.Min(
                constraints.RemainingPaintExtent,
                double.IsPositiveInfinity(scrollExtent)
                    ? constraints.RemainingPaintExtent
                    : Math.Max(0.0, scrollExtent - constraints.ScrollOffset));
            double cacheExtent = double.IsPositiveInfinity(scrollExtent)
                ? constraints.RemainingCacheExtent
                : Math.Min(scrollExtent, constraints.RemainingCacheExtent);
            Geometry = new SliverGeometry(
                ScrollExtent: scrollExtent,
                PaintExtent: paintExtent,
                LayoutExtent: paintExtent,
                MaxPaintExtent: scrollExtent,
                CacheExtent: cacheExtent);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            order.Add("child");
        }
    }

    private sealed class FixedSizeRenderBox(Size size) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class MutableSizeRenderBox(Size size) : RenderBox
    {
        private Size _size = size;

        public void UpdateSize(Size value)
        {
            if (_size == value)
            {
                return;
            }

            _size = value;
            MarkNeedsLayout();
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
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

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
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
}
