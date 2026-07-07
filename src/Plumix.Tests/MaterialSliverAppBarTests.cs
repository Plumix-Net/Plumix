using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSliverAppBarTests
{
    [Fact]
    public void SliverPersistentHeader_ValidatesContractsAndComputesPinnedGeometry()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverPersistentHeader(new TestHeaderDelegate(100, 40)));

        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 100)));
        var header = new RenderSliverPersistentHeader(56, 180, pinned: true, floating: false, child: child);
        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis.Vertical, 90, 300, 300, 300, RemainingCacheExtent: 300));

        Assert.Equal(90, header.LastShrinkOffset, precision: 3);
        Assert.Equal(90, child.Size.Height, precision: 3);
        Assert.Equal(180, header.Geometry.ScrollExtent, precision: 3);
        Assert.Equal(90, header.Geometry.PaintExtent, precision: 3);
        Assert.False(header.LastOverlapsContent);

        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis.Vertical, 160, 300, 300, 300, RemainingCacheExtent: 300));
        Assert.Equal(56, child.Size.Height, precision: 3);
        Assert.Equal(56, header.Geometry.PaintExtent, precision: 3);
        Assert.True(header.LastOverlapsContent);
    }

    [Fact]
    public void SliverPersistentHeader_FloatingRevealsImmediatelyOnReverseScroll()
    {
        var header = new RenderSliverPersistentHeader(
            56, 180, pinned: false, floating: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 180))));
        var constraints = new SliverConstraints(Axis.Vertical, 160, 300, 300, 300, RemainingCacheExtent: 300);
        header.LayoutWithSliverConstraints(constraints);
        Assert.Equal(124, header.LastShrinkOffset, precision: 3);
        Assert.Equal(20, header.Geometry.PaintExtent, precision: 3);

        header.LayoutWithSliverConstraints(constraints with { ScrollOffset = 130 });
        Assert.Equal(124, header.LastShrinkOffset, precision: 3);
        Assert.Equal(50, header.Geometry.PaintExtent, precision: 3);

        header.LayoutWithSliverConstraints(constraints with { ScrollOffset = 100 });
        Assert.Equal(100, header.LastShrinkOffset, precision: 3);
        Assert.Equal(80, header.Geometry.PaintExtent, precision: 3);
    }

    [Fact]
    public void FlexibleSpaceBar_ValidatesSettingsAndBuildsParallaxBackgroundAndScaledTitle()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlexibleSpaceBar(expandedTitleScale: 0.9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlexibleSpaceBarSettings(2, 56, 200, 200, false, true, new SizedBox()));

        using var expanded = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(
                    title: new Text("Flexible title"),
                    background: new ColoredBox(Colors.Blue))),
            ThemeData.Light with { Platform = TargetPlatform.Android }));
        expanded.Pump(new Size(360, 200));

        var transform = Assert.Single(FindDescendants<RenderTransform>(expanded.RenderView));
        Assert.Equal(Alignment.BottomLeft, transform.Alignment);
        Assert.Contains(FindDescendants<RenderOpacity>(expanded.RenderView), value => Math.Abs(value.Opacity - 1) < 0.001);

        using var collapsed = new WidgetRenderHarness(Wrap(new FlexibleSpaceBarSettings(
            1, 56, 200, 56, true, true,
            new FlexibleSpaceBar(background: new ColoredBox(Colors.Blue)))));
        collapsed.Pump(new Size(360, 56));
        Assert.Contains(FindDescendants<RenderOpacity>(collapsed.RenderView), value => value.Opacity <= 0.001);
    }

    [Fact]
    public void SliverAppBar_ConstructorsExposeFlutterDefaultsAndGuards()
    {
        var regular = new SliverAppBar(title: new Text("Regular"));
        Assert.False(regular.Floating);
        Assert.False(regular.Pinned);
        Assert.False(regular.Snap);
        Assert.Equal(56, regular.ToolbarHeight);
        Assert.Throws<ArgumentException>(() => new SliverAppBar(snap: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAppBar(toolbarHeight: 56, collapsedHeight: 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SliverAppBar(stretchTriggerOffset: 0));

        var medium = SliverAppBar.Medium(title: new Text("Medium"));
        var large = SliverAppBar.Large(title: new Text("Large"));
        Assert.True(medium.Pinned);
        Assert.True(large.Pinned);
        Assert.Equal(64, medium.ToolbarHeight);
        Assert.Equal(SliverAppBarVariant.Medium, medium.Variant);
        Assert.Equal(SliverAppBarVariant.Large, large.Variant);
    }

    [Fact]
    public void SliverAppBar_CustomScrollViewCollapsesToPinnedExtentAndUpdatesSettings()
    {
        var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
            controller: controller,
            slivers:
            [
                new SliverAppBar(
                    title: new Text("Toolbar"),
                    pinned: true,
                    expandedHeight: 200,
                    flexibleSpace: new FlexibleSpaceBar(
                        title: new Text("Flexible"),
                        background: new ColoredBox(Colors.Blue))),
                new SliverToBoxAdapter(new SizedBox(height: 900)),
            ])));
        harness.Pump(new Size(360, 320));
        var header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
        Assert.Equal(200, header.MaxExtent, precision: 3);
        Assert.Equal(56, header.MinExtent, precision: 3);
        Assert.Equal(0, header.LastShrinkOffset, precision: 3);

        controller.JumpTo(180);
        harness.Pump(new Size(360, 320));
        harness.Pump(new Size(360, 320));
        header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
        Assert.Equal(144, header.LastShrinkOffset, precision: 3);
        Assert.Equal(56, header.Child!.Size.Height, precision: 3);
        Assert.True(header.LastOverlapsContent);
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Toolbar");
    }

    [Fact]
    public void SliverAppBar_ThemeAndWidgetSurfacePrecedenceApplyWhenScrolledUnder()
    {
        var controller = new ScrollController(initialScrollOffset: 160);
        var theme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(
                BackgroundColor: Colors.Purple,
                ScrolledUnderElevation: 5,
                ShadowColor: Colors.Black,
                Shape: ShapeBorder.RoundedRectangle(10)),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            new CustomScrollView(
                controller: controller,
                slivers:
                [
                    new SliverAppBar(
                        title: new Text("Themed"),
                        pinned: true,
                        expandedHeight: 160,
                        backgroundColor: Colors.Orange,
                        shape: ShapeBorder.RoundedRectangle(8)),
                    new SliverToBoxAdapter(new SizedBox(height: 800)),
                ]),
            theme));
        harness.Pump(new Size(360, 320));
        harness.Pump(new Size(360, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), value =>
            value.Decoration.Color == Colors.Orange
            && value.Decoration.EffectiveBorderRadius == BorderRadius.Circular(8)
            && value.Decoration.BoxShadows is not null);
    }

    [Fact]
    public void AlignedTransform_ScalesAroundRequestedAnchorAndFeedsSemanticsTransform()
    {
        using var harness = new WidgetRenderHarness(new Plumix.Widgets.Transform(
            Matrix.CreateScale(1.5, 1.5),
            alignment: Alignment.BottomRight,
            child: new SizedBox(width: 100, height: 40)));
        harness.Pump(new Size(100, 40));
        var transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(Alignment.BottomRight, transform.Alignment);
        Assert.NotEqual(transform.Transform, transform.EffectiveTransform);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(Size: new Size(360, 640)),
            new Theme(theme ?? ThemeData.Light, child)));

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class TestHeaderDelegate : SliverPersistentHeaderDelegate
    {
        public TestHeaderDelegate(double min, double max) { MinExtent = min; MaxExtent = max; }
        public override double MinExtent { get; }
        public override double MaxExtent { get; }
        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent) => new SizedBox();
        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => true;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;
        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }
        public RenderView RenderView { get; }
        public void Pump(Size size) { _owner.FlushBuild(); _pipeline.RequestLayout(); _pipeline.FlushLayout(size); _pipeline.FlushCompositingBits(); _pipeline.FlushPaint(); }
        public void Dispose() => _root.Unmount();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
            public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
