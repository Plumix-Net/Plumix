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
        var header = new RenderSliverPinnedPersistentHeader(56, 180, child: child);
        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis.Vertical, 90, 300, 300, 300, RemainingCacheExtent: 300));

        Assert.Equal(90, header.LastShrinkOffset, precision: 3);
        Assert.Equal(90, child.Size.Height, precision: 3);
        Assert.Equal(180, header.Geometry.ScrollExtent, precision: 3);
        Assert.Equal(90, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(56, header.Geometry.MaxScrollObstructionExtent, precision: 3);
        Assert.False(header.LastOverlapsContent);

        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis.Vertical, 160, 300, 300, 300, RemainingCacheExtent: 300));
        Assert.Equal(56, child.Size.Height, precision: 3);
        Assert.Equal(56, header.Geometry.PaintExtent, precision: 3);
        // Flutter's pinned header reads overlapsContent from the incoming overlap, not the shrink.
        Assert.False(header.LastOverlapsContent);

        header.LayoutWithSliverConstraints(new SliverConstraints(
            Axis.Vertical, 160, 300, 300, 300, RemainingCacheExtent: 300, Overlap: 24));
        Assert.True(header.LastOverlapsContent);
        Assert.Equal(24, header.Geometry.PaintOrigin, precision: 3);
        Assert.Equal(56, header.Geometry.PaintExtent, precision: 3);
    }

    [Fact]
    public void SliverPersistentHeader_FloatingRevealsImmediatelyOnReverseScroll()
    {
        var header = new RenderSliverFloatingPersistentHeader(
            56, 180,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 180))));
        var constraints = new SliverConstraints(Axis.Vertical, 160, 300, 300, 300, RemainingCacheExtent: 300);
        header.LayoutWithSliverConstraints(constraints);
        Assert.Equal(160, header.LastShrinkOffset, precision: 3);
        Assert.Equal(20, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.MaxScrollObstructionExtent, precision: 3);

        // Without a forward user scroll the header may shrink back but never expand.
        header.LayoutWithSliverConstraints(constraints with { ScrollOffset = 130 });
        Assert.Equal(130, header.EffectiveScrollOffset);
        Assert.Equal(50, header.Geometry.PaintExtent, precision: 3);

        header.LayoutWithSliverConstraints(constraints with
        {
            ScrollOffset = 100,
            UserScrollDirection = ScrollDirection.Forward,
        });
        Assert.Equal(100, header.LastShrinkOffset, precision: 3);
        Assert.Equal(80, header.Geometry.PaintExtent, precision: 3);
        Assert.True(header.Geometry.LayoutExtent < header.Geometry.PaintExtent + 0.001);
    }

    [Fact]
    public void FlexibleSpaceBar_ValidatesSettingsAndBuildsParallaxBackgroundAndScaledTitle()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlexibleSpaceBar(expandedTitleScale: 0.9));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlexibleSpaceBarSettings(2, 56, 200, 200, false, true, new SizedBox()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlexibleSpaceBarSettings(1, 56, 200, 55, false, true, new SizedBox()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlexibleSpaceBarSettings(1, 56, 200, 201, false, true, new SizedBox()));

        var api = new FlexibleSpaceBar(
            titlePadding: EdgeInsetsGeometry.DirectionalOnly(start: 12, bottom: 8),
            stretchModes: [StretchMode.BlurBackground, StretchMode.FadeTitle]);
        Assert.Equal(EdgeInsetsGeometry.DirectionalOnly(start: 12, bottom: 8), api.TitlePadding);
        Assert.Equal([StretchMode.BlurBackground, StretchMode.FadeTitle], api.StretchModes);
        Assert.Equal(CollapseMode.Parallax, api.CollapseMode);
        Assert.Equal(1.5, api.ExpandedTitleScale);

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
        Assert.Equal(1.5, transform.Transform[0], precision: 3);
        Assert.Contains(FindDescendants<RenderOpacity>(expanded.RenderView), value => Math.Abs(value.Opacity - 1) < 0.001);

        using var collapsed = new WidgetRenderHarness(Wrap(new FlexibleSpaceBarSettings(
            1, 56, 200, 56, true, true,
            new FlexibleSpaceBar(background: new ColoredBox(Colors.Blue)))));
        collapsed.Pump(new Size(360, 56));
        Assert.Contains(FindDescendants<RenderOpacity>(collapsed.RenderView), value => value.Opacity <= 0.001);
    }

    [Fact]
    public void FlexibleSpaceBar_PlatformControlsTitleAlignmentAndRouteSemantics()
    {
        using var android = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(title: new Text("Android title"))),
            ThemeData.Light with { Platform = TargetPlatform.Android }));
        android.Pump(new Size(360, 200));

        Assert.Equal(
            Alignment.BottomLeft,
            Assert.Single(FindDescendants<RenderTransform>(android.RenderView)).Alignment);
        Assert.Contains(
            FindDescendants<RenderSemanticsAnnotations>(android.RenderView),
            semantics => semantics.Flags.HasFlag(SemanticsFlags.NamesRoute));

        using var ios = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(title: new Text("iOS title"))),
            ThemeData.Light with { Platform = TargetPlatform.IOS }));
        ios.Pump(new Size(360, 200));

        Assert.Equal(
            Alignment.BottomCenter,
            Assert.Single(FindDescendants<RenderTransform>(ios.RenderView)).Alignment);
        Assert.DoesNotContain(
            FindDescendants<RenderSemanticsAnnotations>(ios.RenderView),
            semantics => semantics.Flags.HasFlag(SemanticsFlags.NamesRoute));
    }

    [Fact]
    public void FlexibleSpaceBar_EqualExtentsAndZeroAreaRemainVisibleAndStable()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 0,
                maxExtent: 0,
                currentExtent: 0,
                isScrolledUnder: false,
                hasLeading: false,
                child: new FlexibleSpaceBar(
                    title: new Text("X"),
                    background: new ColoredBox(Colors.Blue)))));

        harness.Pump(new Size(0, 0));

        Assert.Equal(new Size(0, 0), Assert.IsAssignableFrom<RenderBox>(harness.RenderView.Child).Size);
        Assert.Equal(
            1.0,
            FindDescendants<RenderOpacity>(harness.RenderView)
                .Single(opacity => opacity.AlwaysIncludeSemantics)
                .Opacity);
    }

    [Theory]
    [InlineData(CollapseMode.None, 0.0)]
    [InlineData(CollapseMode.Pin, -72.0)]
    [InlineData(CollapseMode.Parallax, -18.0)]
    public void FlexibleSpaceBar_CollapseModesUseFlutterBackgroundOffsets(
        CollapseMode collapseMode,
        double expectedTop)
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 128,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(
                    background: new ColoredBox(Colors.Blue),
                    collapseMode: collapseMode,
                    stretchModes: []))));

        harness.Pump(new Size(360, 128));

        RenderOpacity opacity = Assert.Single(FindDescendants<RenderOpacity>(harness.RenderView));
        var parentData = Assert.IsType<StackParentData>(opacity.parentData);
        Assert.True(parentData.Top.HasValue);
        Assert.Equal(expectedTop, parentData.Top.Value, precision: 3);
        Assert.True(opacity.AlwaysIncludeSemantics);
        Assert.False(opacity.IsRepaintBoundary);
    }

    [Fact]
    public void FlexibleSpaceBar_StretchModesZoomBlurAndFadeFromLayoutExtent()
    {
        using var effects = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(
                    title: new Text("Stretch title"),
                    background: new ColoredBox(Colors.Blue),
                    stretchModes: [StretchMode.BlurBackground, StretchMode.FadeTitle]))));
        effects.Pump(new Size(360, 250));

        RenderOpacity headerOpacity = FindDescendants<RenderOpacity>(effects.RenderView)
            .Single(opacity => opacity.AlwaysIncludeSemantics);
        var headerParentData = Assert.IsType<StackParentData>(headerOpacity.parentData);
        Assert.Equal(200, headerParentData.Height);
        Assert.Contains(
            FindDescendants<RenderOpacity>(effects.RenderView),
            opacity => !opacity.AlwaysIncludeSemantics && Math.Abs(opacity.Opacity - 0.5) < 0.001);
        var backdrop = Assert.Single(FindDescendants<RenderBackdropFilter>(effects.RenderView));
        var blur = Assert.IsType<ImageFilter.Blur>(backdrop.Filter);
        Assert.Equal(5, blur.SigmaX);
        Assert.Equal(5, blur.SigmaY);

        using var zoom = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 1,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(
                    background: new ColoredBox(Colors.Blue)))));
        zoom.Pump(new Size(360, 250));

        RenderOpacity zoomOpacity = Assert.Single(FindDescendants<RenderOpacity>(zoom.RenderView));
        var zoomParentData = Assert.IsType<StackParentData>(zoomOpacity.parentData);
        Assert.Equal(250, zoomParentData.Height);
    }

    [Fact]
    public void FlexibleSpaceBar_TitleUsesLogicalPaddingScaledWidthAndMaterialBranchStyle()
    {
        ThemeData material3 = ThemeData.Light with
        {
            UseMaterial3 = true,
            TextTheme = ThemeData.Light.TextTheme.CopyWith(
                titleLarge: ThemeData.Light.TextTheme.TitleLarge.CopyWith(color: Colors.Purple)),
            PrimaryTextTheme = ThemeData.Light.PrimaryTextTheme.CopyWith(
                titleLarge: ThemeData.Light.PrimaryTextTheme.TitleLarge.CopyWith(color: Colors.Green)),
        };
        using var rtl = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 0.5,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: true,
                child: new FlexibleSpaceBar(
                    title: new Text("RTL title"),
                    centerTitle: false)),
            material3,
            TextDirection.Rtl));
        rtl.Pump(new Size(360, 200));

        var padding = Assert.Single(FindDescendants<RenderPadding>(rtl.RenderView));
        Assert.Equal(new Thickness(0, 0, 72, 16), padding.Padding);
        var transform = Assert.Single(FindDescendants<RenderTransform>(rtl.RenderView));
        Assert.Equal(Alignment.BottomRight, transform.Alignment);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(rtl.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.TightFor(width: 192));
        var paragraph = FindDescendants<RenderParagraph>(rtl.RenderView)
            .Single(value => value.PlainText == "RTL title");
        Assert.Equal(
            Color.FromArgb(128, Colors.Purple.R, Colors.Purple.G, Colors.Purple.B),
            Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);

        ThemeData material2 = material3 with { UseMaterial3 = false };
        using var m2 = new WidgetRenderHarness(Wrap(
            new FlexibleSpaceBarSettings(
                toolbarOpacity: 0.5,
                minExtent: 56,
                maxExtent: 200,
                currentExtent: 200,
                isScrolledUnder: false,
                hasLeading: false,
                child: new FlexibleSpaceBar(title: new Text("M2 title"))),
            material2));
        m2.Pump(new Size(360, 200));

        var m2Paragraph = FindDescendants<RenderParagraph>(m2.RenderView)
            .Single(value => value.PlainText == "M2 title");
        Assert.Equal(
            Color.FromArgb(128, Colors.Green.R, Colors.Green.G, Colors.Green.B),
            Assert.IsType<SolidColorBrush>(m2Paragraph.Foreground).Color);
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
        // Flutter's layoutChild clamps the shrink offset to maxExtent, not to maxExtent - minExtent.
        Assert.Equal(180, header.LastShrinkOffset, precision: 3);
        Assert.Equal(56, header.Child!.Size.Height, precision: 3);
        Assert.Equal(56, header.Geometry.MaxScrollObstructionExtent, precision: 3);
        Assert.Contains(FindDescendants<RenderParagraph>(harness.RenderView), value => value.PlainText == "Toolbar");
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
                Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(10))),
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
                        shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(8))),
                    new SliverToBoxAdapter(new SizedBox(height: 800)),
                ]),
            theme));
        harness.Pump(new Size(360, 320));
        harness.Pump(new Size(360, 320));

        // The header now composes a real AppBar, so its Material owns the surface: the widget's
        // background and shape win over the theme's, tinted at the theme's scrolled-under elevation.
        var surface = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(BorderRadius.Circular(8), surface.Decoration.EffectiveBorderRadius);
        Assert.Equal(
            ElevationOverlay.ApplySurfaceTint(Colors.Orange, theme.ColorScheme.SurfaceTint, 5),
            surface.Decoration.Color);
        Assert.NotEqual(
            ElevationOverlay.ApplySurfaceTint(Colors.Orange, theme.ColorScheme.SurfaceTint, 0),
            surface.Decoration.Color);
    }

    [Fact]
    public void SliverAppBar_LocalAppBarTheme_OverridesGlobalThemeData()
    {
        var globalTheme = ThemeData.Light with
        {
            AppBarTheme = new AppBarThemeData(BackgroundColor: Colors.Purple),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            new AppBarTheme(
                data: new AppBarThemeData(
                    BackgroundColor: Colors.CadetBlue,
                    Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(12))),
                child: new CustomScrollView(
                    slivers:
                    [
                        new SliverAppBar(
                            title: new Text("Local theme"),
                            pinned: true),
                        new SliverToBoxAdapter(new SizedBox(height: 800)),
                    ])),
            globalTheme));

        harness.Pump(new Size(360, 320));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), value =>
            value.Decoration.Color == Colors.CadetBlue
            && value.Decoration.EffectiveBorderRadius == BorderRadius.Circular(12));
    }

    [Fact]
    public void AlignedTransform_ScalesAroundRequestedAnchorAndFeedsSemanticsTransform()
    {
        using var harness = new WidgetRenderHarness(new Plumix.Widgets.Transform(
            Matrix4.Diagonal3Values(1.5, 1.5, 1.0),
            alignment: Alignment.BottomRight,
            child: new SizedBox(width: 100, height: 40)));
        harness.Pump(new Size(100, 40));
        var transform = Assert.Single(FindDescendants<RenderTransform>(harness.RenderView));
        Assert.Equal(Alignment.BottomRight, transform.Alignment);
        Assert.NotEqual(transform.Transform, transform.EffectiveTransform);
    }

    private static Widget Wrap(
        Widget child,
        ThemeData? theme = null,
        TextDirection textDirection = TextDirection.Ltr) => new Directionality(
        textDirection,
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
