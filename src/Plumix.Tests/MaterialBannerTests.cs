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
    public void MaterialBannerTheme_DefaultCopyLerpAndInheritedCaptureMatchFlutterContract()
    {
        var empty = new MaterialBannerThemeData();
        Assert.Equal(empty, empty.CopyWith());
        Assert.Null(empty.BackgroundColor);
        Assert.Null(empty.SurfaceTintColor);
        Assert.Null(empty.ShadowColor);
        Assert.Null(empty.DividerColor);
        Assert.Null(empty.ContentTextStyle);
        Assert.Null(empty.Elevation);
        Assert.Null(empty.Padding);
        Assert.Null(empty.LeadingPadding);

        var data = new MaterialBannerThemeData(
            BackgroundColor: Colors.DarkCyan,
            DividerColor: Colors.Gold,
            Elevation: 2.0,
            Padding: EdgeInsetsDirectional.Only(start: 6.0));
        var child = new SizedBox();
        var theme = new MaterialBannerTheme(data, child);

        Assert.IsAssignableFrom<InheritedTheme>(theme);
        var wrapped = Assert.IsType<MaterialBannerTheme>(theme.Wrap(default, child));
        Assert.Equal(data, wrapped.Data);
        Assert.Same(child, wrapped.Child);

        var midpoint = MaterialBannerThemeData.Lerp(empty, data, 0.5);
        Assert.Equal(1.0, midpoint.Elevation);
        Assert.Equal(EdgeInsetsDirectional.Only(start: 3.0), midpoint.Padding);

        using var local = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new MaterialBannerTheme(data, Banner())));
        local.Pump(new Size(360, 180));
        var localBackground = Assert.Single(
            FindDescendants<RenderDecoratedBox>(local.RenderView),
            box => box.Decoration.Color.HasValue);
        Assert.Equal(Colors.DarkCyan, localBackground.Decoration.Color);
        Assert.NotNull(localBackground.Decoration.BoxShadows);
    }

    [Fact]
    public void MaterialBanner_M2AndM3DefaultsReadColorSchemeRolesDirectly()
    {
        Color m3Surface = Color.Parse("#FF102030");
        Color m3Outline = Color.Parse("#FF405060");
        var m3Theme = ThemeData.Light with
        {
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(
                surfaceContainerLow: m3Surface,
                outlineVariant: m3Outline),
            SurfaceContainerLowColor = Colors.Red,
            OutlineVariantColor = Colors.Blue,
        };
        using var m3 = new WidgetRenderHarness(Wrap(m3Theme, Banner()));
        m3.Pump(new Size(360, 180));

        var m3Background = Assert.Single(
            FindDescendants<RenderDecoratedBox>(m3.RenderView),
            box => box.Decoration.Color.HasValue);
        var m3Divider = Assert.Single(
            FindDescendants<RenderDecoratedBox>(m3.RenderView),
            box => box.Decoration.BorderSides?.Bottom is not null);
        Assert.Equal(m3Surface, m3Background.Decoration.Color);
        Assert.Equal(m3Outline, m3Divider.Decoration.BorderSides!.Bottom!.Value.Color);
        Assert.Null(m3Background.Decoration.BoxShadows);

        Color m2Surface = Color.Parse("#FF708090");
        var m2Theme = ThemeData.Light with
        {
            UseMaterial3 = false,
            ColorScheme = ThemeData.Light.ColorScheme.CopyWith(surface: m2Surface),
            SurfaceColor = Colors.Green,
        };
        using var m2 = new WidgetRenderHarness(Wrap(m2Theme, Banner()));
        m2.Pump(new Size(360, 180));

        var m2Background = Assert.Single(
            FindDescendants<RenderDecoratedBox>(m2.RenderView),
            box => box.Decoration.Color.HasValue);
        Assert.Equal(m2Surface, m2Background.Decoration.Color);
        Assert.Null(m2Background.Decoration.BoxShadows);
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
        Assert.Equal(ThemeData.Localize(ThemeData.Light, Typography.EnglishLike2021).TextTheme.BodyMedium.FontSize,
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

        double bodyMediumSize = ThemeData.Localize(
            ThemeData.Light,
            Typography.EnglishLike2021).TextTheme.BodyMedium.FontSize!.Value;
        Assert.Equal(bodyMediumSize * 1.5,
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

        Assert.True(liveRegion.Actions.HasFlag(SemanticsActions.Dismiss));
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

    [Fact]
    public void MaterialBanner_AcceptsGenericAnimationAndDirectionalInsets()
    {
        using var controller = MaterialBanner.CreateAnimationController();
        var animation = new ProxyAnimation(controller);
        var original = Banner();
        var animated = original.WithAnimation(animation, new ValueKey<string>("fallback"));

        Assert.Same(animation, animated.Animation);
        Assert.Equal(original.Key ?? new ValueKey<string>("fallback"), animated.Key);

        EdgeInsetsGeometry directional = EdgeInsetsDirectional.Only(
            start: 3,
            top: 5,
            end: 7,
            bottom: 11);
        Assert.Equal(new Thickness(3, 5, 7, 11), directional.Resolve(TextDirection.Ltr));
        Assert.Equal(new Thickness(7, 5, 3, 11), directional.Resolve(TextDirection.Rtl));
        Assert.Equal(
            EdgeInsetsGeometry.All(5),
            EdgeInsetsGeometry.Lerp(EdgeInsetsGeometry.Zero, EdgeInsetsGeometry.All(10), 0.5));
    }

    [Fact]
    public async Task ScaffoldMessenger_QueuesMaterialBannersAndCompletesClosedReasons()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new Text("Body")))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var first = messenger.ShowMaterialBanner(Banner(content: "First"));
        var second = messenger.ShowMaterialBanner(Banner(content: "Second"));
        harness.Pump(new Size(360, 220));

        Assert.NotNull(FindParagraph(harness.RenderView, "First"));
        Assert.Null(FindParagraph(harness.RenderView, "Second"));

        messenger.RemoveCurrentMaterialBanner();
        harness.Pump(new Size(360, 220));
        Assert.Equal(MaterialBannerClosedReason.Remove, await first.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        second.Close();
        PumpAnimation();
        harness.Pump(new Size(360, 220));
        Assert.Equal(MaterialBannerClosedReason.Hide, await second.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public async Task MaterialBanner_SemanticsDismissUsesMessengerReason()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowMaterialBanner(Banner());
        PumpAnimation();

        var semantics = harness.PumpAndGetSemantics(new Size(360, 220));
        var liveRegion = FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.IsLiveRegion)
            && node.Actions.HasFlag(SemanticsActions.Dismiss));

        Assert.NotNull(liveRegion);
        Assert.True(liveRegion!.PerformAction(SemanticsActions.Dismiss));
        harness.Pump(new Size(360, 220));
        Assert.Equal(MaterialBannerClosedReason.Dismiss, await controller.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Content"));
    }

    [Fact]
    public async Task MaterialBanner_AccessibleNavigationHidesImmediately()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox())),
            mediaQuery: new MediaQueryData(
                Size: new Size(360, 220),
                AccessibleNavigation: true)));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowMaterialBanner(Banner());
        harness.Pump(new Size(360, 220));

        messenger.HideCurrentMaterialBanner();
        harness.Pump(new Size(360, 220));

        Assert.Equal(MaterialBannerClosedReason.Hide, await controller.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Content"));
    }

    [Fact]
    public async Task ScaffoldMessenger_ClearMaterialBannersRetainsAndHidesOnlyCurrentEntry()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var current = messenger.ShowMaterialBanner(Banner(content: "Current"));
        var queued = messenger.ShowMaterialBanner(Banner(content: "Queued"));
        harness.Pump(new Size(360, 220));

        messenger.ClearMaterialBanners();
        PumpAnimation();
        harness.Pump(new Size(360, 220));

        Assert.Equal(MaterialBannerClosedReason.Hide, await current.Closed);
        Assert.False(queued.Closed.IsCompleted);
        Assert.Null(FindParagraph(harness.RenderView, "Current"));
        Assert.Null(FindParagraph(harness.RenderView, "Queued"));
    }

    [Fact]
    public void ScaffoldMessenger_RequiresDescendantScaffoldForMaterialBanner()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new SizedBox())));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var error = Assert.Throws<InvalidOperationException>(() => messenger.ShowMaterialBanner(Banner()));
        Assert.Contains("no descendant Scaffold", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScaffoldMessenger_PresentsMaterialBannerAndSnackBarOnceEach()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        messenger.ShowSnackBar(new SnackBar(content: new Text("Snack")));
        messenger.ShowMaterialBanner(Banner(content: "Banner"));
        harness.Pump(new Size(360, 220));

        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Snack");
        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Banner");
    }

    [Fact]
    public void ScaffoldMessenger_PresentsOnlyOnRootOfNestedScaffoldSet()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(
                new Scaffold(body: new Scaffold(body: new SizedBox())))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        messenger.ShowMaterialBanner(Banner(content: "Nested"));
        harness.Pump(new Size(360, 220));

        Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value => value.Text == "Nested");
    }

    [Fact]
    public void ScaffoldMessenger_PresentsOnEverySiblingRootScaffold()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(
                new Row(
                    children:
                    [
                        new Expanded(new Scaffold(body: new SizedBox())),
                        new Expanded(new Scaffold(body: new SizedBox())),
                    ]))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        messenger.ShowMaterialBanner(Banner(content: "Sibling"));
        harness.Pump(new Size(360, 220));

        Assert.Equal(
            2,
            FindDescendants<RenderParagraph>(harness.RenderView).Count(value => value.Text == "Sibling"));
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(2.0, false)]
    public void Scaffold_ZeroElevationBannerPushesBodyWhileElevatedBannerOverlays(
        double elevation,
        bool bodyIsPushed)
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new Text("Body")))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        messenger.ShowMaterialBanner(Banner(elevation: elevation));
        PumpAnimation();
        harness.Pump(new Size(360, 220));

        var scaffoldColumn = Assert.Single(FindDescendants<RenderFlex>(harness.RenderView), flex =>
            flex.Direction == Axis.Vertical
            && flex.MainAxisSize == MainAxisSize.Max
            && flex.CrossAxisAlignment == CrossAxisAlignment.Stretch
            && flex.Size == new Size(360, 220));
        RenderBox first = scaffoldColumn.FirstChild!;
        RenderBox? second = scaffoldColumn.ChildAfter(first);

        Assert.Equal(bodyIsPushed, second is not null);
        if (second is not null)
        {
            Assert.True(((FlexParentData)second.parentData!).offset.Y >= first.Size.Height);
        }
    }

    private static MaterialBanner Banner(
        string content = "Content",
        IReadOnlyList<Widget>? actions = null,
        double? elevation = null,
        Widget? leading = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        double minActionBarHeight = 52,
        Animation<double>? animation = null,
        Action? onVisible = null,
        Key? key = null) => new(
        content: new Text(content),
        actions: actions ?? [new Text("ACTION")],
        elevation: elevation,
        leading: leading,
        backgroundColor: backgroundColor,
        padding: padding,
        minActionBarHeight: minActionBarHeight,
        animation: animation,
        onVisible: onVisible,
        key: key);

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
    }

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

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
        }

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                states.Add(state);
            }
            element.VisitChildren(child => CollectStates(child, states));
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
