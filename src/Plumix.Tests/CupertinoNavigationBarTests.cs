using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/nav_bar_test.dart, cupertino_ui/test/nav_bar_transition_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoNavigationBarTests : IDisposable
{
    public CupertinoNavigationBarTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void Constructor_ExposesDefaultsAndGuards()
    {
        var bar = new CupertinoNavigationBar();
        Assert.True(bar.AutomaticallyImplyLeading);
        Assert.True(bar.AutomaticallyImplyMiddle);
        Assert.True(bar.AutomaticBackgroundVisibility);
        Assert.True(bar.EnableBackgroundFilterBlur);
        Assert.True(bar.TransitionBetweenRoutes);
        Assert.Null(bar.LargeTitle);
        Assert.NotNull(bar.Border);
        Assert.Equal(Color.FromUInt32(0x4D000000), bar.Border!.Bottom.Color);
        Assert.Equal(0.0, bar.Border.Bottom.Width);

        // A custom heroTag cannot be combined with transitionBetweenRoutes: true.
        Assert.Throws<ArgumentException>(() => new CupertinoNavigationBar(heroTag: new object()));
        _ = new CupertinoNavigationBar(heroTag: new object(), transitionBetweenRoutes: false);

        var large = CupertinoNavigationBar.Large(largeTitle: new Text("Large"));
        Assert.NotNull(large.LargeTitle);
        Assert.Null(large.Middle);

        // Sliver: largeTitle is required when automaticallyImplyTitle is false.
        Assert.Throws<ArgumentException>(() =>
            new CupertinoSliverNavigationBar(automaticallyImplyTitle: false));
        // Sliver: a bottomMode requires a bottom.
        Assert.Throws<ArgumentException>(() => new CupertinoSliverNavigationBar(
            largeTitle: new Text("T"),
            bottomMode: NavigationBarBottomMode.Automatic));

        var search = CupertinoSliverNavigationBar.Search(
            searchField: new Text("Search"),
            largeTitle: new Text("T"));
        Assert.True(search.Searchable);
        Assert.Equal(NavigationBarBottomMode.Automatic, search.BottomMode);
        Assert.Null(search.Bottom);
        Assert.False(search.Opaque);
        Assert.True(new CupertinoSliverNavigationBar(
            largeTitle: new Text("T"),
            backgroundColor: Color.FromUInt32(0xFF112233)).Opaque);
    }

    [Fact]
    public void PreferredSize_MatchesFlutterHeights()
    {
        Assert.Equal(44.0, new CupertinoNavigationBar().PreferredSize.Height);
        Assert.Equal(
            74.0,
            new CupertinoNavigationBar(
                bottom: new PreferredSize(new Size(0.0, 30.0), new SizedBox())).PreferredSize.Height);
        Assert.Equal(96.0, CupertinoNavigationBar.Large(largeTitle: new Text("L")).PreferredSize.Height);
        Assert.Equal(
            126.0,
            CupertinoNavigationBar.Large(
                largeTitle: new Text("L"),
                bottom: new PreferredSize(new Size(0.0, 30.0), new SizedBox())).PreferredSize.Height);
    }

    [Fact]
    public void StaticBar_LaysOutAtPersistentHeight()
    {
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("Title"))))))
        {
            harness.Pump(new Size(400, 600));
            var background = FindDescendants<RenderDecoratedBox>(harness.RenderView)[0];
            Assert.Equal(44.0, background.Size.Height, 3);
        }

        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(
                       middle: new Text("Title"),
                       bottom: new PreferredSize(new Size(0.0, 30.0), new SizedBox()))))))
        {
            harness.Pump(new Size(400, 600));
            var background = FindDescendants<RenderDecoratedBox>(harness.RenderView)[0];
            Assert.Equal(74.0, background.Size.Height, 3);
        }

        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   CupertinoNavigationBar.Large(largeTitle: new Text("Large"))))))
        {
            harness.Pump(new Size(400, 600));
            var background = FindDescendants<RenderDecoratedBox>(harness.RenderView)[0];
            Assert.Equal(96.0, background.Size.Height, 3);
        }
    }

    [Fact]
    public void MiddleRemainsCenteredWithAsymmetricalActions()
    {
        using var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
            leading: new Text("Something"),
            middle: new Text("Title")))));
        harness.Pump(new Size(400, 600));

        RenderParagraph title = FindParagraph(harness.RenderView, "Title")!;
        Point center = title.LocalToGlobal(new Point(title.Size.Width / 2, title.Size.Height / 2));
        Assert.Equal(200.0, center.X, 3);
    }

    [Fact]
    public void BackgroundBlur_FollowsOpacityAndFlag()
    {
        // The theme's default bar background (0xF0F9F9F9) is translucent, so the blur is enabled.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("T"), automaticBackgroundVisibility: false)))))
        {
            harness.Pump(new Size(400, 600));
            var filter = Assert.Single(harness.FindWidgets<BackdropFilter>());
            Assert.True(filter.Enabled);
        }

        // An opaque background disables the blur.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   middle: new Text("T"),
                   backgroundColor: Color.FromUInt32(0xFFE5E5E5))))))
        {
            harness.Pump(new Size(400, 600));
            var filter = Assert.Single(harness.FindWidgets<BackdropFilter>());
            Assert.False(filter.Enabled);
        }

        // enableBackgroundFilterBlur: false disables the blur for translucent backgrounds too.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   middle: new Text("T"),
                   automaticBackgroundVisibility: false,
                   enableBackgroundFilterBlur: false)))))
        {
            harness.Pump(new Size(400, 600));
            var filter = Assert.Single(harness.FindWidgets<BackdropFilter>());
            Assert.False(filter.Enabled);
        }
    }

    [Fact]
    public void Border_DefaultOverrideAndNull()
    {
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("T"), automaticBackgroundVisibility: false)))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)[0].Decoration);
            var border = Assert.IsType<Border>(decoration.Border);
            Assert.Equal(Color.FromUInt32(0x4D000000), border.Bottom.Color);
            Assert.Equal(0.0, border.Bottom.Width);
        }

        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   border: new Border(bottom: new BorderSide(Color.FromUInt32(0xFFAABBCC), width: 0.0)),
                   middle: new Text("T"),
                   automaticBackgroundVisibility: false)))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)[0].Decoration);
            var border = Assert.IsType<Border>(decoration.Border);
            Assert.Equal(Color.FromUInt32(0xFFAABBCC), border.Bottom.Color);
        }

        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   border: null,
                   middle: new Text("T"),
                   automaticBackgroundVisibility: false)))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)[0].Decoration);
            Assert.Null(decoration.Border);
        }
    }

    [Fact]
    public void AutomaticBackgroundVisibility_IsTransparentUntilScrolledUnder()
    {
        Color scaffoldColor = Color.FromUInt32(0xFF010203);

        // Inside a scaffold with nothing scrolled under, the bar shows the scaffold color and a
        // transparent border.
        using (var harness = new WidgetRenderHarness(Wrap(new CupertinoPageScaffold(
                   backgroundColor: scaffoldColor,
                   navigationBar: new CupertinoNavigationBar(middle: new Text("T")),
                   child: new SizedBox()))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)
                    .First(box => box.Decoration is BoxDecoration { Border: not null }).Decoration);
            Assert.Equal(scaffoldColor, decoration.Color);
            var border = Assert.IsType<Border>(decoration.Border);
            Assert.Equal(0x00, border.Bottom.Color.A);
        }

        // automaticBackgroundVisibility: false always shows the theme bar background.
        using (var harness = new WidgetRenderHarness(Wrap(new CupertinoPageScaffold(
                   backgroundColor: scaffoldColor,
                   navigationBar: new CupertinoNavigationBar(
                       middle: new Text("T"),
                       automaticBackgroundVisibility: false),
                   child: new SizedBox()))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)
                    .First(box => box.Decoration is BoxDecoration { Border: not null }).Decoration);
            Assert.Equal(Color.FromUInt32(0xF0F9F9F9), decoration.Color);
        }

        // Outside a CupertinoPageScaffold the parameter has no effect.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("T"))))))
        {
            harness.Pump(new Size(400, 600));
            var decoration = Assert.IsType<BoxDecoration>(
                FindDescendants<RenderDecoratedBox>(harness.RenderView)[0].Decoration);
            Assert.Equal(Color.FromUInt32(0xF0F9F9F9), decoration.Color);
        }
    }

    [Fact]
    public void SystemUiOverlay_SetsOnlyStatusBarFields()
    {
        // Light background draws a dark status bar.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("T"), automaticBackgroundVisibility: false)))))
        {
            harness.Pump(new Size(400, 600));
            var region = Assert.Single(harness.FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>());
            Assert.Equal(SystemUiIconBrightness.Dark, region.Value.StatusBarIconBrightness);
            Assert.Null(region.Value.NavigationBarColor);
            Assert.Null(region.Value.NavigationBarIconBrightness);
        }

        // Dark background draws a light status bar (luminance < 0.179).
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   middle: new Text("T"),
                   backgroundColor: Color.FromUInt32(0xFF000000))))))
        {
            harness.Pump(new Size(400, 600));
            var region = Assert.Single(harness.FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>());
            Assert.Equal(SystemUiIconBrightness.Light, region.Value.StatusBarIconBrightness);
        }

        // An explicit brightness overrides the luminance-derived value.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   middle: new Text("T"),
                   backgroundColor: Color.FromUInt32(0xFF000000),
                   brightness: PlatformBrightness.Light)))))
        {
            harness.Pump(new Size(400, 600));
            var region = Assert.Single(harness.FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>());
            Assert.Equal(SystemUiIconBrightness.Dark, region.Value.StatusBarIconBrightness);
        }
    }

    [Fact]
    public void CustomPadding_ReplacesEdgePaddings()
    {
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new CupertinoNavigationBar(
                   leading: new Text("Cheese"),
                   middle: new Text("Title"),
                   trailing: new Text("Puzzle"),
                   padding: new EdgeInsetsDirectional(10.0, 0.0, 20.0, 0.0))))))
        {
            harness.Pump(new Size(400, 600));
            RenderParagraph leading = FindParagraph(harness.RenderView, "Cheese")!;
            Assert.Equal(10.0, leading.LocalToGlobal(new Point(0, 0)).X, 3);
            RenderParagraph trailing = FindParagraph(harness.RenderView, "Puzzle")!;
            Assert.Equal(380.0, trailing.LocalToGlobal(new Point(trailing.Size.Width, 0)).X, 3);
        }

        // In RTL the start padding applies on the right.
        using (var harness = new WidgetRenderHarness(Wrap(
                   TopAligned(new CupertinoNavigationBar(
                       leading: new Text("Cheese"),
                       middle: new Text("Title"),
                       trailing: new Text("Puzzle"),
                       padding: new EdgeInsetsDirectional(10.0, 0.0, 20.0, 0.0))),
                   textDirection: TextDirection.Rtl)))
        {
            harness.Pump(new Size(400, 600));
            RenderParagraph leading = FindParagraph(harness.RenderView, "Cheese")!;
            Assert.Equal(390.0, leading.LocalToGlobal(new Point(leading.Size.Width, 0)).X, 3);
        }
    }

    [Fact]
    public void Semantics_TitlesAreHeaders()
    {
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   new CupertinoNavigationBar(middle: new Text("Title"))))))
        {
            harness.Pump(new Size(400, 600));
            Assert.Contains(
                harness.FindWidgets<Semantics>(),
                semantics => (semantics.Flags & SemanticsFlags.IsHeader) != 0);
        }

        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(
                   CupertinoNavigationBar.Large(largeTitle: new Text("Large"))))))
        {
            harness.Pump(new Size(400, 600));
            Assert.Contains(
                harness.FindWidgets<Semantics>(),
                semantics => (semantics.Flags & SemanticsFlags.IsHeader) != 0);
        }
    }

    [Fact]
    public void TextScaler_ClampsPersistentComponentsAndDampsLargeTitle()
    {
        double? middleScale = null;
        double? largeTitleScale = null;
        using (var harness = new WidgetRenderHarness(Wrap(
                   TopAligned(CupertinoNavigationBar.Large(
                       largeTitle: new CaptureContextWidget(context =>
                           largeTitleScale = MediaQuery.TextScalerOf(context).Scale(52.0) / 52.0),
                       trailing: new CaptureContextWidget(context =>
                           middleScale = MediaQuery.TextScalerOf(context).Scale(10.0) / 10.0))),
                   textScaleFactor: 3.0)))
        {
            harness.Pump(new Size(400, 600));
            // Persistent components clamp to [1.0, 1.235].
            Assert.Equal(1.235, middleScale!.Value, 3);
            // The large title damps growth: 1 + (3 - 1) / 3.
            Assert.Equal(1.0 + (2.0 / 3.0), largeTitleScale!.Value, 3);
        }

        middleScale = null;
        largeTitleScale = null;
        using (var harness = new WidgetRenderHarness(Wrap(
                   TopAligned(CupertinoNavigationBar.Large(
                       largeTitle: new CaptureContextWidget(context =>
                           largeTitleScale = MediaQuery.TextScalerOf(context).Scale(52.0) / 52.0),
                       trailing: new CaptureContextWidget(context =>
                           middleScale = MediaQuery.TextScalerOf(context).Scale(10.0) / 10.0))),
                   textScaleFactor: 0.5)))
        {
            harness.Pump(new Size(400, 600));
            // Persistent components clamp up to 1.0; the large title damps down to 0.9.
            Assert.Equal(1.0, middleScale!.Value, 3);
            Assert.Equal(0.9, largeTitleScale!.Value, 3);
        }
    }

    [Fact]
    public void BackButton_ThrowsWhenRouteCannotBePopped()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var harness = new WidgetRenderHarness(Wrap(new CupertinoNavigationBarBackButton()));
            harness.Pump(new Size(400, 600));
        });
    }

    [Fact]
    public void BackButton_CustomOnPressedWorksAnywhere()
    {
        int pressed = 0;
        using var harness = new WidgetRenderHarness(Wrap(TopAligned(
            new CupertinoNavigationBarBackButton(onPressed: () => pressed++))));
        harness.Pump(new Size(400, 600));

        var button = Assert.Single(harness.FindWidgets<CupertinoButton>());
        button.OnPressed!();
        Assert.Equal(1, pressed);
    }

    [Fact]
    public void BackButton_ShowsSpecifiedPreviousPageTitle()
    {
        using var harness = new WidgetRenderHarness(Wrap(TopAligned(
            new CupertinoNavigationBarBackButton(
                previousPageTitle: "Widgets",
                onPressed: () => { }))));
        harness.Pump(new Size(400, 600));

        Assert.NotNull(FindParagraph(harness.RenderView, "Widgets"));
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(CupertinoIcons.Back.CodePoint)));
    }

    [Fact]
    public void BackButton_LongPreviousTitleTurnsIntoBack()
    {
        using var harness = new WidgetRenderHarness(Wrap(TopAligned(
            new CupertinoNavigationBarBackButton(
                previousPageTitle: "0123456789012",
                onPressed: () => { }))));
        harness.Pump(new Size(400, 600));

        Assert.Null(FindParagraph(harness.RenderView, "0123456789012"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Back"));
    }

    [Fact]
    public void Sliver_ExtentsAndLargeTitlePlacement()
    {
        var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
            controller: controller,
            slivers:
            [
                new CupertinoSliverNavigationBar(largeTitle: new Text("Large")),
                new SliverToBoxAdapter(new SizedBox(height: 1200)),
            ])));
        harness.Pump(new Size(400, 600));

        var header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
        Assert.Equal(96.0, header.MaxExtent, 3);
        Assert.Equal(44.0, header.MinExtent, 3);

        RenderParagraph largeTitle = FindParagraph(harness.RenderView, "Large")!;
        Point titleOrigin = largeTitle.LocalToGlobal(new Point(0, 0));
        Assert.Equal(16.0, titleOrigin.X, 3);
        Assert.True(titleOrigin.Y > 44.0);
    }

    [Fact]
    public void Sliver_CollapseSwapsLargeTitleForMiddle()
    {
        var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
            controller: controller,
            slivers:
            [
                new CupertinoSliverNavigationBar(largeTitle: new Text("Large")),
                new SliverToBoxAdapter(new SizedBox(height: 1200)),
            ])));
        harness.Pump(new Size(400, 600));

        // Expanded: the large title is visible and the implied middle is hidden.
        var opacities = harness.FindWidgets<AnimatedOpacity>();
        Assert.Equal(2, opacities.Count);
        Assert.Equal(1.0, opacities[0].Opacity, 3);
        Assert.Equal(0.0, opacities[1].Opacity, 3);

        controller.JumpTo(52.0);
        harness.Pump(new Size(400, 600));
        harness.Pump(new Size(400, 600));

        opacities = harness.FindWidgets<AnimatedOpacity>();
        Assert.Equal(2, opacities.Count);
        Assert.Equal(0.0, opacities[0].Opacity, 3);
        Assert.Equal(1.0, opacities[1].Opacity, 3);
    }

    [Fact]
    public void Sliver_UserMiddleAlwaysVisibleUnlessOptedOut()
    {
        // With a user middle and alwaysShowMiddle (default), the middle is not opacity-managed.
        using (var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
                   slivers:
                   [
                       new CupertinoSliverNavigationBar(
                           largeTitle: new Text("Large"),
                           middle: new Text("Middle")),
                       new SliverToBoxAdapter(new SizedBox(height: 1200)),
                   ]))))
        {
            harness.Pump(new Size(400, 600));
            // Only the large title's animated opacity remains.
            Assert.Single(harness.FindWidgets<AnimatedOpacity>());
            Assert.NotNull(FindParagraph(harness.RenderView, "Middle"));
        }

        // alwaysShowMiddle: false hides the middle while expanded.
        using (var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
                   slivers:
                   [
                       new CupertinoSliverNavigationBar(
                           largeTitle: new Text("Large"),
                           middle: new Text("Middle"),
                           alwaysShowMiddle: false),
                       new SliverToBoxAdapter(new SizedBox(height: 1200)),
                   ]))))
        {
            harness.Pump(new Size(400, 600));
            var opacities = harness.FindWidgets<AnimatedOpacity>();
            Assert.Equal(2, opacities.Count);
            Assert.Equal(0.0, opacities[1].Opacity, 3);
        }
    }

    [Fact]
    public void Sliver_LandscapeShowsLargeTitleInMiddlePosition()
    {
        // In landscape the large title moves into the middle slot and the extension collapses.
        using var harness = new WidgetRenderHarness(Wrap(
            new CustomScrollView(
                slivers:
                [
                    new CupertinoSliverNavigationBar(largeTitle: new Text("Large")),
                    new SliverToBoxAdapter(new SizedBox(height: 1200)),
                ]),
            mediaSize: new Size(800, 400)));
        harness.Pump(new Size(800, 400));

        var header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
        Assert.Equal(44.0, header.MaxExtent, 3);
        Assert.Equal(44.0, header.MinExtent, 3);
        Assert.NotNull(FindParagraph(harness.RenderView, "Large"));
    }

    [Fact]
    public void Sliver_BottomModesControlCollapsedExtent()
    {
        using (var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
                   slivers:
                   [
                       new CupertinoSliverNavigationBar(
                           largeTitle: new Text("Large"),
                           bottom: new PreferredSize(new Size(0.0, 30.0), new SizedBox()),
                           bottomMode: NavigationBarBottomMode.Automatic),
                       new SliverToBoxAdapter(new SizedBox(height: 1200)),
                   ]))))
        {
            harness.Pump(new Size(400, 600));
            var header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
            Assert.Equal(126.0, header.MaxExtent, 3);
            Assert.Equal(44.0, header.MinExtent, 3);
        }

        using (var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
                   slivers:
                   [
                       new CupertinoSliverNavigationBar(
                           largeTitle: new Text("Large"),
                           bottom: new PreferredSize(new Size(0.0, 30.0), new SizedBox()),
                           bottomMode: NavigationBarBottomMode.Always),
                       new SliverToBoxAdapter(new SizedBox(height: 1200)),
                   ]))))
        {
            harness.Pump(new Size(400, 600));
            var header = Assert.Single(FindDescendants<RenderSliverPersistentHeader>(harness.RenderView));
            Assert.Equal(126.0, header.MaxExtent, 3);
            Assert.Equal(74.0, header.MinExtent, 3);
        }
    }

    [Fact]
    public void Sliver_SearchActivationCollapsesBarAndReportsTaps()
    {
        var taps = new List<bool>();
        using var harness = new WidgetRenderHarness(Wrap(new CustomScrollView(
            slivers:
            [
                CupertinoSliverNavigationBar.Search(
                    searchField: new Text("SearchField"),
                    largeTitle: new Text("Large"),
                    onSearchableBottomTap: value => taps.Add(value)),
                new SliverToBoxAdapter(new SizedBox(height: 1200)),
            ])));
        harness.Pump(new Size(400, 600));

        var inactive = Assert.Single(harness.FindWidgets<InactiveSearchableBottom>());
        Assert.Empty(harness.FindWidgets<ActiveSearchableBottom>());

        inactive.OnSearchFieldTap!();
        Assert.Equal([true], taps);

        AnimationPump.Prime();
        harness.Pump(new Size(400, 600));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.35));
        harness.Pump(new Size(400, 600));

        var active = Assert.Single(harness.FindWidgets<ActiveSearchableBottom>());
        Assert.NotNull(FindParagraph(harness.RenderView, "Cancel"));

        active.OnSearchFieldTap!();
        Assert.Equal([true, false], taps);

        AnimationPump.Prime();
        harness.Pump(new Size(400, 600));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.35));
        harness.Pump(new Size(400, 600));

        Assert.Single(harness.FindWidgets<InactiveSearchableBottom>());
        Assert.Empty(harness.FindWidgets<ActiveSearchableBottom>());
    }

    [Fact]
    public void Transition_PushRunsNavigationBarHeroFlightAndImpliesBackButton()
    {
        var viewportSize = new Size(400, 600);
        NavigatorState? navigatorState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(
            initialRoute: BuildRoute("Home", "home-content", captureState: state => navigatorState = state))));
        harness.Pump(viewportSize);
        Assert.NotNull(navigatorState);

        navigatorState!.Push(BuildRoute("Second", "second-content"));
        harness.Pump(viewportSize);
        PumpHeroTransitionFrame(harness, viewportSize);

        // Mid-flight, the hero shuttle is the navigation bar transition.
        Assert.NotEmpty(harness.FindWidgets<NavigationBarTransition>());

        AdvanceHeroTransition(harness, viewportSize);

        Assert.Empty(harness.FindWidgets<NavigationBarTransition>());
        // The top bar auto-implies a back button with the previous page's title.
        Assert.NotNull(FindParagraph(harness.RenderView, "Home"));
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(CupertinoIcons.Back.CodePoint)));
    }

    [Fact]
    public void Transition_LongPreviousTitleTurnsIntoBack()
    {
        var viewportSize = new Size(400, 600);
        NavigatorState? navigatorState = null;
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(
            initialRoute: BuildRoute(
                "A title too long over 12 characters",
                "home-content",
                captureState: state => navigatorState = state))));
        harness.Pump(viewportSize);

        navigatorState!.Push(BuildRoute("Second", "second-content"));
        harness.Pump(viewportSize);
        AdvanceHeroTransition(harness, viewportSize);

        Assert.NotNull(FindParagraph(harness.RenderView, "Back"));
    }

    [Fact]
    public void Transition_SkippedForFullscreenDialogsAndWhenDisabled()
    {
        var viewportSize = new Size(400, 600);
        NavigatorState? navigatorState = null;
        using (var harness = new WidgetRenderHarness(Wrap(new Navigator(
                   initialRoute: BuildRoute("Home", "home-content", captureState: state => navigatorState = state)))))
        {
            harness.Pump(viewportSize);
            navigatorState!.Push(BuildRoute("Dialog", "dialog-content", fullscreenDialog: true));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Empty(harness.FindWidgets<NavigationBarTransition>());

            AdvanceHeroTransition(harness, viewportSize);
            // Fullscreen dialogs imply a Cancel button instead of a back chevron.
            Assert.NotNull(FindParagraph(harness.RenderView, "Cancel"));
            Assert.Null(FindParagraph(harness.RenderView, char.ConvertFromUtf32(CupertinoIcons.Back.CodePoint)));
        }

        navigatorState = null;
        using (var harness = new WidgetRenderHarness(Wrap(new Navigator(
                   initialRoute: BuildRoute("Home", "home-content", captureState: state => navigatorState = state)))))
        {
            harness.Pump(viewportSize);
            navigatorState!.Push(BuildRoute("Second", "second-content", transitionBetweenRoutes: false));
            harness.Pump(viewportSize);
            PumpHeroTransitionFrame(harness, viewportSize);

            Assert.Empty(harness.FindWidgets<NavigationBarTransition>());
            AdvanceHeroTransition(harness, viewportSize);
        }
    }

    [Fact]
    public void RenderLargeTitle_MagnifiesWithConstraintsUpToMaxScale()
    {
        // At the resting height (52 - 8 = 44 available) the scale is exactly 1.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new SizedBox(
                   width: 300,
                   height: 44,
                   child: new LargeTitleWidget(height: 52.0, child: new Text("T")))))))
        {
            harness.Pump(new Size(300, 600));
            var render = Assert.Single(FindDescendants<RenderLargeTitle>(harness.RenderView));
            Assert.Equal(1.0, render.Scale, 3);
        }

        // Stretched beyond the resting height, the title magnifies proportionally.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new SizedBox(
                   width: 300,
                   height: 80,
                   child: new LargeTitleWidget(height: 52.0, child: new Text("T")))))))
        {
            harness.Pump(new Size(300, 600));
            var render = Assert.Single(FindDescendants<RenderLargeTitle>(harness.RenderView));
            Assert.Equal(1.0 + (0.03 * (80.0 - 44.0) / 44.0), render.Scale, 3);
        }

        // The magnification clamps at 1.1 even for very tall constraints.
        using (var harness = new WidgetRenderHarness(Wrap(TopAligned(new SizedBox(
                   width: 300,
                   height: 400,
                   child: new LargeTitleWidget(height: 52.0, child: new Text("T")))))))
        {
            harness.Pump(new Size(300, 600));
            var render = Assert.Single(FindDescendants<RenderLargeTitle>(harness.RenderView));
            Assert.Equal(1.1, render.Scale, 3);
        }
    }

    private static Route BuildRoute(
        string title,
        string content,
        bool fullscreenDialog = false,
        bool transitionBetweenRoutes = true,
        Action<NavigatorState>? captureState = null)
    {
        return new CupertinoPageRoute<object?>(
            builder: context =>
            {
                captureState?.Invoke(Navigator.Of(context));
                return new CupertinoPageScaffold(
                    navigationBar: new CupertinoNavigationBar(
                        transitionBetweenRoutes: transitionBetweenRoutes),
                    child: new Center(child: new Text(content)));
            },
            title: title,
            fullscreenDialog: fullscreenDialog);
    }

    private static void AdvanceHeroTransition(WidgetRenderHarness harness, Size viewportSize)
    {
        PumpHeroTransitionFrame(harness, viewportSize);

        double afterStart = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(afterStart + 0.60));
        harness.Pump(viewportSize);
    }

    private static void PumpHeroTransitionFrame(WidgetRenderHarness harness, Size viewportSize)
    {
        // The flight controller is created by the build this frame runs, so it takes its start
        // timestamp from the priming frame and only advances on the one after it.
        AnimationPump.Prime();
        harness.Pump(viewportSize);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.016));
        harness.Pump(viewportSize);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.032));
        harness.Pump(viewportSize);
    }

    private static Widget TopAligned(Widget child) =>
        new Align(alignment: Alignment.TopCenter, child: child);

    private static Widget Wrap(
        Widget child,
        TextDirection textDirection = TextDirection.Ltr,
        double textScaleFactor = 1.0,
        Size? mediaSize = null)
    {
        return new MediaQuery(
            new MediaQueryData(
                Size: mediaSize ?? new Size(400, 600),
                TextScaleFactor: textScaleFactor),
            new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    textDirection,
                    new CupertinoTheme(new CupertinoThemeData(), child))));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T value)
        {
            result.Add(value);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class CaptureContextWidget : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;

        public CaptureContextWidget(Action<BuildContext> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return new SizedBox(width: 10.0, height: 10.0);
        }
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

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_root, widgets);
            return widgets;
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget)
            {
                widgets.Add(widget);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
        }

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

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

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _view.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(_view.Child, renderBox))
                {
                    _view.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
