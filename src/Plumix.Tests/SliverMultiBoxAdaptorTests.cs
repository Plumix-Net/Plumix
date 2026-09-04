using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Ported from flutter/packages/flutter/test/rendering/slivers_block_test.dart.

namespace Plumix.Tests;

/// <summary>
/// Covers <see cref="RenderSliverMultiBoxAdaptor"/>'s paint, hit-test and paint-transform paths,
/// which derive a child's position from <see cref="RenderSliver.ChildMainAxisPosition"/> and the
/// resolved axis direction rather than from a stored paint offset.
/// </summary>
public sealed class SliverMultiBoxAdaptorTests
{
    [Fact]
    public void RenderSliverList_BasicTest_Down()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var offset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        PipelineOwner pipeline = Layout(viewport);

        Assert.Equal(new Size(800, 600), viewport.Size);
        Assert.Equal(new Point(0, 0), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 400), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[2].Attached);
        Assert.False(manager.Children[3].Attached);
        Assert.False(manager.Children[4].Attached);

        // Make sure that layout is stable by laying out again.
        inner.MarkNeedsLayout();
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, 0), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 400), manager.Children[1].LocalToGlobal(new Point(0, 0)));

        offset.JumpTo(200);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, -200), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 200), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[2].Attached);

        offset.JumpTo(600);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(manager.Children[0].Attached);
        Assert.Equal(new Point(0, -200), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 200), manager.Children[2].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[3].Attached);

        offset.JumpTo(900);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(manager.Children[1].Attached);
        Assert.Equal(new Point(0, -100), manager.Children[2].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 300), manager.Children[3].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[4].Attached);

        // Try going back up.
        offset.JumpTo(200);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, -200), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 200), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[2].Attached);
    }

    [Fact]
    public void RenderSliverList_BasicTest_Up()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var offset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            axisDirection: AxisDirection.Up,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        PipelineOwner pipeline = Layout(viewport);

        Assert.Equal(new Size(800, 600), viewport.Size);
        Assert.Equal(new Point(0, 200), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, -200), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.False(manager.Children[2].Attached);

        // Make sure that layout is stable by laying out again.
        inner.MarkNeedsLayout();
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, 200), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, -200), manager.Children[1].LocalToGlobal(new Point(0, 0)));

        offset.JumpTo(200);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, 400), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 0), manager.Children[1].LocalToGlobal(new Point(0, 0)));

        offset.JumpTo(600);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(manager.Children[0].Attached);
        Assert.Equal(new Point(0, 400), manager.Children[1].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 0), manager.Children[2].LocalToGlobal(new Point(0, 0)));

        offset.JumpTo(900);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.False(manager.Children[1].Attached);
        Assert.Equal(new Point(0, 300), manager.Children[2].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, -100), manager.Children[3].LocalToGlobal(new Point(0, 0)));

        // Try going back up.
        offset.JumpTo(200);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(new Point(0, 400), manager.Children[0].LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 0), manager.Children[1].LocalToGlobal(new Point(0, 0)));
    }

    /// <summary>
    /// The paint offsets Flutter's <c>RenderSliverMultiBoxAdaptor.paint</c> builds from the
    /// main/cross-axis unit vectors, for each resolved axis direction.
    /// </summary>
    [Theory]
    [InlineData(AxisDirection.Down, 0.0, 0.0, 0.0, 400.0)]
    [InlineData(AxisDirection.Up, 0.0, 200.0, 0.0, -200.0)]
    [InlineData(AxisDirection.Right, 0.0, 0.0, 400.0, 0.0)]
    [InlineData(AxisDirection.Left, 400.0, 0.0, 0.0, 0.0)]
    public void RenderSliverList_Paint_PositionsChildrenAlongTheResolvedAxisDirection(
        AxisDirection axisDirection,
        double firstX,
        double firstY,
        double secondX,
        double secondY)
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5, axisDirection));
        RenderSliverList inner = manager.CreateRenderObject();
        var viewport = new RenderViewport(
            offset: new TestViewportOffset(),
            crossAxisDirection: axisDirection is AxisDirection.Up or AxisDirection.Down
                ? AxisDirection.Right
                : AxisDirection.Down,
            axisDirection: axisDirection,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        Layout(viewport);

        var context = new PaintingContext(new ContainerLayer());
        inner.Paint(context, new Point(0, 0));

        Assert.Equal(new Point(firstX, firstY), ((RecordingBox)manager.Children[0]).PaintedAt);
        Assert.Equal(new Point(secondX, secondY), ((RecordingBox)manager.Children[1]).PaintedAt);
        Assert.Null(((RecordingBox)manager.Children[2]).PaintedAt);
    }

    /// <summary>
    /// The intersection test at the end of Flutter's <c>paint</c>: a child whose visible interval
    /// does not overlap <c>(0, remainingPaintExtent)</c> is laid out but not painted.
    /// </summary>
    [Fact]
    public void RenderSliverList_Paint_SkipsChildrenOutsideTheRemainingPaintExtent()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var offset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(400),
            children: [inner]);
        PipelineOwner pipeline = Layout(viewport);

        offset.JumpTo(800);
        pipeline.FlushLayout(new Size(800, 600));

        var context = new PaintingContext(new ContainerLayer());
        inner.Paint(context, new Point(0, 0));

        // Index 1 is only in the cache area (it ends exactly at the scroll offset), so it is
        // reified but never painted.
        Assert.True(manager.Children[1].Attached);
        Assert.Null(((RecordingBox)manager.Children[1]).PaintedAt);
        Assert.Equal(new Point(0, 0), ((RecordingBox)manager.Children[2]).PaintedAt);
    }

    [Fact]
    public void RenderSliverList_HitTest_FindsTheChildUnderThePosition()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var viewport = new RenderViewport(
            offset: new TestViewportOffset(),
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        Layout(viewport);

        var result = new BoxHitTestResult();
        Assert.True(viewport.HitTest(result, new Point(50, 450)));
        Assert.Contains(result.Path, entry => ReferenceEquals(entry.Target, manager.Children[1]));
    }

    /// <summary>
    /// A reversed sliver hit tests through <c>RenderSliverHelpers.hitTestBoxChild</c>, which flips
    /// the main-axis position when the growth direction is not the right way up.
    /// </summary>
    [Fact]
    public void RenderSliverList_HitTest_FlipsTheMainAxisForAReversedSliver()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var viewport = new RenderViewport(
            offset: new TestViewportOffset(),
            crossAxisDirection: AxisDirection.Right,
            axisDirection: AxisDirection.Up,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        Layout(viewport);

        // With AxisDirection.up the first child occupies the bottom 400 pixels of the viewport.
        var first = new BoxHitTestResult();
        Assert.True(viewport.HitTest(first, new Point(50, 550)));
        Assert.Contains(first.Path, entry => ReferenceEquals(entry.Target, manager.Children[0]));

        var second = new BoxHitTestResult();
        Assert.True(viewport.HitTest(second, new Point(50, 50)));
        Assert.Contains(second.Path, entry => ReferenceEquals(entry.Target, manager.Children[1]));
    }

    /// <summary>
    /// Flutter's <c>paintsChild</c>: false for a child the manager has not indexed, and false for a
    /// child that has been parked in the keep-alive bucket.
    /// </summary>
    [Fact]
    public void RenderSliverList_PaintsChild_IsFalseForAnUnindexedOrKeptAliveChild()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var offset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        PipelineOwner pipeline = Layout(viewport);

        RenderBox first = manager.Children[0];
        Assert.True(inner.PaintsChild(first));

        var firstParentData = (SliverMultiBoxAdaptorParentData)first.parentData!;
        int? index = firstParentData.Index;
        firstParentData.Index = null;
        Assert.False(inner.PaintsChild(first));
        firstParentData.Index = index;

        firstParentData.KeepAlive = true;
        offset.JumpTo(900);
        pipeline.FlushLayout(new Size(800, 600));

        Assert.True(first.Attached);
        Assert.True(firstParentData.KeptAlive);
        Assert.False(inner.PaintsChild(first));
    }

    /// <summary>
    /// Flutter's <c>applyPaintTransform</c>: asking for the transform of a child that is not painted
    /// is valid, and yields a zero matrix rather than throwing.
    /// </summary>
    [Fact]
    public void RenderSliverList_ApplyPaintTransform_ZeroesTheMatrixForAChildThatIsNotPainted()
    {
        var manager = new TestRenderSliverBoxChildManager(SizedChildren(5));
        RenderSliverList inner = manager.CreateRenderObject();
        var offset = new TestViewportOffset();
        var viewport = new RenderViewport(
            offset: offset,
            crossAxisDirection: AxisDirection.Right,
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [inner]);
        PipelineOwner pipeline = Layout(viewport);

        RenderBox first = manager.Children[0];
        var painted = Matrix4.Identity();
        inner.ApplyPaintTransform(first, painted);
        Assert.Equal(new Point(0, 0), MatrixUtils.TransformPoint(painted, new Point(0, 0)));

        ((SliverMultiBoxAdaptorParentData)first.parentData!).KeepAlive = true;
        offset.JumpTo(900);
        pipeline.FlushLayout(new Size(800, 600));

        var keptAlive = Matrix4.Identity();
        inner.ApplyPaintTransform(first, keptAlive);
        Assert.Equal(Matrix4.Zero(), keptAlive);
    }

    /// <summary>Flutter's <c>SliverMultiBoxAdaptorParentData.toString</c> test.</summary>
    [Fact]
    public void SliverMultiBoxAdaptorParentData_ToString()
    {
        var candidate = new SliverMultiBoxAdaptorParentData();
        Assert.False(candidate.KeepAlive);
        Assert.Null(candidate.Index);
        Assert.Equal("index=null; layoutOffset=None", candidate.ToString());
        candidate.KeepAlive = true;
        Assert.Equal("index=null; keepAlive; layoutOffset=None", candidate.ToString());
        candidate.KeepAlive = false;
        Assert.Equal("index=null; layoutOffset=None", candidate.ToString());
        candidate.Index = 0;
        Assert.Equal("index=0; layoutOffset=None", candidate.ToString());
        candidate.Index = 1;
        Assert.Equal("index=1; layoutOffset=None", candidate.ToString());
        candidate.Index = -1;
        Assert.Equal("index=-1; layoutOffset=None", candidate.ToString());
        candidate.LayoutOffset = 100.0;
        Assert.Equal("index=-1; layoutOffset=100.0", candidate.ToString());
    }

    private static PipelineOwner Layout(RenderViewport viewport)
    {
        var root = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(800, 600));
        return pipeline;
    }

    private static List<RenderBox> SizedChildren(int count, AxisDirection axisDirection = AxisDirection.Down)
    {
        Size size = axisDirection is AxisDirection.Up or AxisDirection.Down
            ? new Size(100, 400)
            : new Size(400, 100);
        return [.. Enumerable.Range(0, count).Select(_ => (RenderBox)new RecordingBox(size))];
    }

    /// <summary>Flutter's <c>RenderSizedBox</c>, plus the offset it was last painted at.</summary>
    private sealed class RecordingBox : RenderBox
    {
        private readonly Size _size;

        public RecordingBox(Size size)
        {
            _size = size;
        }

        public Point? PaintedAt { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintedAt = offset;
        }
    }

    /// <summary>Flutter's <c>TestRenderSliverBoxChildManager</c>.</summary>
    private sealed class TestRenderSliverBoxChildManager : IRenderSliverBoxChildManager
    {
        private RenderSliverList? _renderObject;
        private int? _currentlyUpdatingChildIndex;

        public TestRenderSliverBoxChildManager(List<RenderBox> children)
        {
            Children = children;
        }

        public List<RenderBox> Children { get; }

        public int ChildCount => Children.Count;

        public int? EstimatedChildCount => ChildCount;

        public RenderSliverList CreateRenderObject()
        {
            _renderObject = new RenderSliverList(this);
            return _renderObject;
        }

        public void CreateChild(int index, RenderBox? after)
        {
            if (index < 0 || index >= Children.Count)
            {
                return;
            }

            try
            {
                _currentlyUpdatingChildIndex = index;
                _renderObject!.Insert(Children[index], after);
            }
            finally
            {
                _currentlyUpdatingChildIndex = null;
            }
        }

        public void RemoveChild(RenderBox child) => _renderObject!.Remove(child);

        public double EstimateMaxScrollOffset(
            SliverConstraints constraints,
            int? firstIndex = null,
            int? lastIndex = null,
            double? leadingScrollOffset = null,
            double? trailingScrollOffset = null)
        {
            Assert.True(lastIndex >= firstIndex);
            return ChildCount
                   * (trailingScrollOffset!.Value - leadingScrollOffset!.Value)
                   / (lastIndex!.Value - firstIndex!.Value + 1);
        }

        public void DidAdoptChild(RenderBox child)
        {
            Assert.NotNull(_currentlyUpdatingChildIndex);
            ((SliverMultiBoxAdaptorParentData)child.parentData!).Index = _currentlyUpdatingChildIndex;
        }

        public void SetDidUnderflow(bool value)
        {
        }
    }
}
