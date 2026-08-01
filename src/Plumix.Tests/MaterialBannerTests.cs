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
public sealed class MaterialBannerTests
{
    [Theory]
    [InlineData(TextDirection.Ltr, BannerLocation.TopStart, 0, 0, -1)]
    [InlineData(TextDirection.Rtl, BannerLocation.TopStart, 200, 0, 1)]
    [InlineData(TextDirection.Ltr, BannerLocation.TopEnd, 200, 0, 1)]
    [InlineData(TextDirection.Rtl, BannerLocation.TopEnd, 0, 0, -1)]
    [InlineData(TextDirection.Ltr, BannerLocation.BottomStart, 48.485281, 71.514719, 1)]
    [InlineData(TextDirection.Rtl, BannerLocation.BottomStart, 151.514719, 71.514719, -1)]
    [InlineData(TextDirection.Ltr, BannerLocation.BottomEnd, 151.514719, 71.514719, -1)]
    [InlineData(TextDirection.Rtl, BannerLocation.BottomEnd, 48.485281, 71.514719, 1)]
    public void BannerPainter_UsesFlutterCornerGeometry(
        TextDirection direction,
        BannerLocation location,
        double expectedX,
        double expectedY,
        int rotationSign)
    {
        using var painter = new BannerPainter("DEBUG", direction, location, direction);

        Assert.Equal(expectedX, painter.TranslationX(200), precision: 5);
        Assert.Equal(expectedY, painter.TranslationY(120), precision: 5);
        Assert.Equal(rotationSign * Math.PI / 4, painter.Rotation, precision: 10);
        Assert.Equal(new Rect(-40, 28, 80, 12), BannerPainter.BannerRect);
        Assert.Equal(Color.FromArgb(0xA0, 0xB7, 0x1C, 0x1C), painter.Color);
        Assert.Equal(10.2, painter.TextStyle.FontSize!.Value, precision: 10);
        Assert.False(painter.HitTest(default));
    }

    [Fact]
    public void BannerPainter_ShouldRepaintMatchesFlutterFields()
    {
        using var original = new BannerPainter("A", TextDirection.Ltr, BannerLocation.TopEnd, TextDirection.Ltr);
        using var directionsOnly = new BannerPainter("A", TextDirection.Rtl, BannerLocation.TopEnd, TextDirection.Rtl);
        using var changedMessage = new BannerPainter("B", TextDirection.Ltr, BannerLocation.TopEnd, TextDirection.Ltr);

        Assert.False(directionsOnly.ShouldRepaint(original));
        Assert.True(changedMessage.ShouldRepaint(original));
    }

    [Fact]
    public void Banner_AndCheckedModeBanner_RenderAtZeroAndFiniteSize()
    {
        using var banner = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Banner("BETA", BannerLocation.TopEnd, child: new SizedBox(width: 80, height: 40))));
        banner.Pump(new Size(180, 100));
        banner.Pump(new Size(0, 0));

        using var checkedMode = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new CheckedModeBanner(new SizedBox(width: 40, height: 20))));
        checkedMode.Pump(new Size(100, 60));
        Assert.NotNull(checkedMode.RenderView.Child);
    }

    [Fact]
    public void MaterialBanner_ValidatesRequiredActionsAndGeometry()
    {
        Assert.Throws<ArgumentException>(() => new MaterialBanner(new Text("Content"), []));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banner(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banner(minActionBarHeight: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Banner(padding: new Thickness(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialBannerThemeData(Elevation: -1));

        using var controller = MaterialBanner.CreateAnimationController();
        Assert.Equal(TimeSpan.FromMilliseconds(250), controller.Duration);
        var original = Banner(key: new ValueKey<string>("banner"));
        var animated = original.WithAnimation(controller, new ValueKey<string>("fallback"));
        Assert.Same(controller, animated.Animation);
        Assert.Equal(original.Key, animated.Key);
    }

    [Fact]
    public void MaterialBanner_M3SingleActionUsesFlutterDefaults()
    {
        using var harness = new WidgetRenderHarness(Wrap(ThemeData.Light, Banner()));
        harness.Pump(new Size(360, 180));

        var decoration = Assert.Single(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color.HasValue);
        Assert.Equal(ThemeData.Light.SurfaceContainerLowColor, decoration.Decoration.Color);
        Assert.Null(decoration.Decoration.BoxShadows);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 2, 0, 0));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 52);
        Assert.Single(FindDescendants<RenderOverflowBar>(harness.RenderView));
        Assert.Equal(ThemeData.Light.TextTheme.BodyMedium.FontSize,
            FindParagraph(harness.RenderView, "Content")!.FontSize);
    }

    [Fact]
    public void MaterialBanner_MultipleActionsMoveBelowAndOverflowVertically()
    {
        var banner = Banner(
            actions:
            [
                new SizedBox(width: 80, height: 24, child: new Text("ONE")),
                new SizedBox(width: 80, height: 24, child: new Text("TWO")),
            ]);
        using var narrow = new WidgetRenderHarness(Wrap(ThemeData.Light, banner));
        narrow.Pump(new Size(150, 240));

        var overflow = Assert.Single(FindDescendants<RenderOverflowBar>(narrow.RenderView));
        Assert.Equal(2, overflow.ChildCount);
        var first = overflow.FirstChild!;
        var second = overflow.LastChild!;
        var firstOffset = ((OverflowBarParentData)first.parentData!).offset;
        var secondOffset = ((OverflowBarParentData)second.parentData!).offset;
        Assert.True(secondOffset.Y > firstOffset.Y);
        Assert.Equal(overflow.Size.Width - first.Size.Width, firstOffset.X, precision: 3);
        Assert.Contains(FindDescendants<RenderPadding>(narrow.RenderView),
            padding => padding.Padding == new Thickness(16, 24, 16, 4));
    }

    [Theory]
    [InlineData(TextDirection.Ltr, 142, 170)]
    [InlineData(TextDirection.Rtl, 38, 0)]
    public void OverflowBar_HorizontalAlignmentMatchesRowDirection(
        TextDirection direction,
        double firstX,
        double secondX)
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ConstrainedBox(
                BoxConstraints.TightFor(width: 200),
                new OverflowBar(
                    spacing: 8,
                    alignment: MainAxisAlignment.End,
                    textDirection: direction,
                    children:
                    [
                        new SizedBox(width: 20, height: 10),
                        new SizedBox(width: 30, height: 10),
                    ])),
            direction: direction));
        harness.Pump(new Size(240, 80));

        var overflow = Assert.Single(FindDescendants<RenderOverflowBar>(harness.RenderView));
        Assert.Equal(200, overflow.Size.Width);
        Assert.Equal(firstX, ((OverflowBarParentData)overflow.FirstChild!.parentData!).offset.X, precision: 3);
        Assert.Equal(secondX, ((OverflowBarParentData)overflow.LastChild!.parentData!).offset.X, precision: 3);
    }

    [Fact]
    public void MaterialBanner_ThemeAndWidgetPropertiesUseCorrectPrecedence()
    {
        var theme = ThemeData.Light with
        {
            BannerTheme = new MaterialBannerThemeData(
                BackgroundColor: Colors.Purple,
                ShadowColor: Colors.Black,
                DividerColor: Colors.Gold,
                ContentTextStyle: ThemeData.Light.TextTheme.BodyMedium.CopyWith(
                    color: Colors.Orange,
                    fontSize: 18),
                Elevation: 3,
                Padding: new Thickness(7),
                LeadingPadding: new Thickness(5)),
        };
        using var themed = new WidgetRenderHarness(Wrap(theme, Banner(leading: new Text("Leading"))));
        themed.Pump(new Size(360, 180));

        var decoration = Assert.Single(
            FindDescendants<RenderDecoratedBox>(themed.RenderView),
            box => box.Decoration.Color.HasValue);
        Assert.Equal(Colors.Purple, decoration.Decoration.Color);
        Assert.NotNull(decoration.Decoration.BoxShadows);
        Assert.Equal(Colors.Orange,
            Assert.IsType<SolidColorBrush>(FindParagraph(themed.RenderView, "Content")!.Foreground).Color);
        Assert.Equal(18, FindParagraph(themed.RenderView, "Content")!.FontSize);
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView), value => value.Padding == new Thickness(7));
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView), value => value.Padding == new Thickness(5));
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView),
            value => value.Padding == new Thickness(0, 0, 0, 10));

        using var explicitColor = new WidgetRenderHarness(Wrap(
            theme,
            Banner(backgroundColor: Colors.Green, elevation: 0)));
        explicitColor.Pump(new Size(360, 180));
        Assert.Equal(Colors.Green,
            Assert.Single(
                FindDescendants<RenderDecoratedBox>(explicitColor.RenderView),
                box => box.Decoration.Color.HasValue).Decoration.Color);
    }

    [Fact]
    public void MaterialBanner_ClampsTextScaleAndResolvesRtlPadding()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Banner(leading: new Text("Leading")),
            direction: TextDirection.Rtl,
            mediaQuery: new MediaQueryData(Size: new Size(360, 180), TextScaleFactor: 3)));
        harness.Pump(new Size(360, 180));

        Assert.Equal(ThemeData.Light.TextTheme.BodyMedium.FontSize!.Value * 1.5,
            FindParagraph(harness.RenderView, "Content")!.FontSize,
            precision: 3);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(0, 2, 16, 0));
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(16, 0, 0, 0));
    }

    [Fact]
    public void MaterialBanner_AnimationAddsSlideHeightLiveRegionAndCallsOnVisibleOnce()
    {
        int visibleCalls = 0;
        using var controller = MaterialBanner.CreateAnimationController();
        controller.Forward(from: 0.5);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Banner(animation: controller, onVisible: () => visibleCalls++)));
        harness.Pump(new Size(360, 180));

        Assert.Contains(FindDescendants<RenderFractionalTranslation>(harness.RenderView),
            translation => translation.Translation == new Vector(0, 0));
        Assert.Contains(FindDescendants<RenderAlign>(harness.RenderView),
            align => align.HeightFactor.HasValue
                     && Math.Abs(align.HeightFactor.Value - Curves.FastOutSlowIn(0.5)) < 0.001);
        var semantics = harness.PumpAndGetSemantics(new Size(360, 180));
        var liveRegion = FindSemantics(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsLiveRegion));
        Assert.NotNull(liveRegion);
        Assert.True(liveRegion!.Actions.HasFlag(SemanticsActions.Dismiss));

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size(360, 180));
        Assert.Equal(1, visibleCalls);

        Assert.True(liveRegion.PerformAction(SemanticsActions.Dismiss));
        now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        Assert.Equal(0, controller.Value);
    }

    [Fact]
    public void MaterialBanner_AccessibleNavigationSkipsSlideAndHeightClipping()
    {
        using var controller = MaterialBanner.CreateAnimationController();
        controller.Forward(from: 0.5);
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Banner(animation: controller),
            mediaQuery: new MediaQueryData(Size: new Size(360, 180), AccessibleNavigation: true)));
        harness.Pump(new Size(360, 180));

        Assert.Empty(FindDescendants<RenderFractionalTranslation>(harness.RenderView));
        Assert.DoesNotContain(FindDescendants<RenderAlign>(harness.RenderView), align => align.HeightFactor.HasValue);
    }

    private static MaterialBanner Banner(
        IReadOnlyList<Widget>? actions = null,
        double? elevation = null,
        Widget? leading = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        double minActionBarHeight = 52,
        AnimationController? animation = null,
        Action? onVisible = null,
        Key? key = null) => new(
        content: new Text("Content"),
        actions: actions ?? [new Text("ACTION")],
        elevation: elevation,
        leading: leading,
        backgroundColor: backgroundColor,
        padding: padding,
        minActionBarHeight: minActionBarHeight,
        animation: animation,
        onVisible: onVisible,
        key: key);

    private static Widget Wrap(
        ThemeData theme,
        Widget child,
        TextDirection direction = TextDirection.Ltr,
        MediaQueryData? mediaQuery = null) =>
        new Directionality(
            direction,
            new MediaQuery(
                mediaQuery ?? new MediaQueryData(Size: new Size(360, 220)),
                new Theme(
                    theme,
                    new Align(alignment: Alignment.TopLeft, child: child))));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T value) result.Add(value);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? root, Func<SemanticsNode, bool> predicate)
    {
        if (root is null) return null;
        if (predicate(root)) return root;
        foreach (var child in root.Children)
        {
            var match = FindSemantics(child, predicate);
            if (match is not null) return match;
        }
        return null;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
