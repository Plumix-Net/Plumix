using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = global::Plumix.Material.Material;

namespace Plumix.Tests;

/// <summary>
/// Covers the parity-critical behavior <c>material_ui/lib/src/app_bar.dart</c> pins for the standard
/// <see cref="AppBar"/>: the preferred-size contract, the toolbar container and title box layout,
/// system-overlay derivation, the scrolled-under state machine, and the defaults chains.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialAppBarTests
{
    public MaterialAppBarTests()
    {
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    // ---------------------------------------------------------------- preferred size

    [Fact]
    public void AppBar_PreferredSize_AddsBottomHeightToTheToolbarHeight()
    {
        Assert.Equal(56.0, new AppBar().PreferredSize.Height);
        Assert.True(double.IsPositiveInfinity(new AppBar().PreferredSize.Width));
        Assert.Equal(64.0, new AppBar(toolbarHeight: 64).PreferredSize.Height);
        Assert.Equal(
            86.0,
            new AppBar(bottom: new PreferredSize(new Size(0, 30), new SizedBox())).PreferredSize.Height);
        Assert.Equal(
            94.0,
            new AppBar(toolbarHeight: 64, bottom: new PreferredSize(new Size(0, 30), new SizedBox()))
                .PreferredSize.Height);

        Assert.Null(new AppBar().PreferredAppBarSize.ToolbarHeight);
        Assert.Equal(64.0, new AppBar(toolbarHeight: 64).PreferredAppBarSize.ToolbarHeight);
    }

    [Fact]
    public void AppBar_PreferredHeightFor_SubstitutesTheThemeToolbarHeight()
    {
        // Flutter's `AppBar.preferredHeightFor`: the theme height only substitutes when the widget left
        // its own toolbar height unset, and never changes `preferredSize` itself.
        BuildContext context = CaptureContext(new AppBarThemeData(ToolbarHeight: 96));

        var themed = new AppBar(title: new Text("Title"));
        Assert.Equal(96.0, AppBar.PreferredHeightFor(context, themed.PreferredAppBarSize));
        Assert.Equal(56.0, themed.PreferredSize.Height);

        var explicitHeight = new AppBar(title: new Text("Title"), toolbarHeight: 64);
        Assert.Equal(64.0, AppBar.PreferredHeightFor(context, explicitHeight.PreferredAppBarSize));
        Assert.Equal(64.0, explicitHeight.PreferredSize.Height);

        var withBottom = new AppBar(bottom: new PreferredSize(new Size(0, 30), new SizedBox()));
        Assert.Equal(126.0, AppBar.PreferredHeightFor(context, withBottom.PreferredAppBarSize));

        // A plain Size carries no metadata, so it always reports its own height.
        Assert.Equal(56.0, AppBar.PreferredHeightFor(context, themed.PreferredSize));
    }

    [Fact]
    public void Scaffold_AppBarSlot_UsesScaffoldPrimaryAndTheThemeToolbarHeight()
    {
        // Dart adds `MediaQuery.paddingOf(context).top` when the *Scaffold* is primary, not the app bar,
        // and takes the app bar's height through `AppBar.preferredHeightFor`.
        ScaffoldState? state = null;
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MediaQuery(
                    data: new MediaQueryData(Padding: new Thickness(0, 24, 0, 0)),
                    child: new Theme(
                        data: ThemeData.Light with
                        {
                            AppBarTheme = new AppBarThemeData(ToolbarHeight: 96),
                        },
                        child: new Scaffold(
                            appBar: new AppBar(title: new Text("Title")),
                            body: new Builder(context =>
                            {
                                state = Scaffold.Of(context);
                                return new SizedBox();
                            }))))));

        harness.Pump(new Size(400, 600));

        Assert.NotNull(state);
        Assert.True(state!.HasAppBar);
        Assert.Equal(120.0, state.AppBarMaxHeight!.Value, 3);
    }

    [Fact]
    public void Scaffold_AcceptsAnyPreferredSizeWidgetAndANullBody()
    {
        // Dart types `Scaffold.appBar` as `PreferredSizeWidget?` and `Scaffold.body` as `Widget?`.
        using var harness = new WidgetRenderHarness(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MediaQuery(
                    data: new MediaQueryData(),
                    child: new Theme(
                        data: ThemeData.Light,
                        child: new Scaffold(
                            appBar: new PreferredSize(
                                new Size(0, 100),
                                new SizedBox(height: 100, child: new ColoredBox(Colors.Teal))))))));

        harness.Pump(new Size(400, 600));

        RenderColoredBox bar = Assert.IsType<RenderColoredBox>(
            FindDescendant<RenderColoredBox>(harness.RenderView, box => box.Color == Colors.Teal));
        Assert.Equal(100.0, bar.Size.Height, 3);
    }

    // ---------------------------------------------------------------- toolbar container / title box

    [Fact]
    public void AppBar_ToolbarContainerLayout_MatchesFlutter()
    {
        // `_ToolbarContainerLayout`: the container spans the incoming width at exactly the toolbar
        // height, tightens the child to that height, and bottom-justifies whatever overflows.
        var layout = new ToolbarContainerLayout(56);

        Assert.Equal(new Size(400, 56), layout.GetSize(new BoxConstraints(MaxWidth: 400, MaxHeight: 600)));
        Assert.Equal(
            BoxConstraints.TightFor(height: 56).MaxHeight,
            layout.GetConstraintsForChild(new BoxConstraints(MaxWidth: 400, MaxHeight: 600)).MaxHeight);
        Assert.Equal(new Point(0.0, -36.0), layout.GetPositionForChild(new Size(400, 20), new Size(400, 56)));
        Assert.Equal(new Point(0.0, 0.0), layout.GetPositionForChild(new Size(400, 56), new Size(400, 56)));
        Assert.True(layout.ShouldRelayout(new ToolbarContainerLayout(64)));
        Assert.False(layout.ShouldRelayout(new ToolbarContainerLayout(56)));
    }

    [Fact]
    public void AppBar_Toolbar_SpansTheFullWidthAtTheToolbarHeight()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new Align(
                alignment: Alignment.TopCenter,
                child: new AppBar(primary: false, toolbarHeight: 48, title: new Text("Title")))));

        harness.Pump(new Size(400, 600));

        var container = FindDescendant<RenderCustomSingleChildLayoutBox>(harness.RenderView);
        Assert.NotNull(container);
        Assert.Equal(new Size(400, 48), container!.Size);

        var toolbar = FindDescendant<RenderCustomMultiChildLayoutBox>(container);
        Assert.NotNull(toolbar);
        Assert.Equal(48.0, toolbar!.Size.Height, 3);
    }

    [Fact]
    public void AppBar_Title_OverflowsSymmetricallyWhenTallerThanTheToolbar()
    {
        // `_RenderAppBarTitleBox` gives the title unbounded height, reports the constrained size and
        // centers the child, so the overflow is symmetric top and bottom.
        using var harness = new WidgetRenderHarness(Wrap(
            new AppBar(
                primary: false,
                toolbarHeight: 20,
                title: new SizedBox(width: 40, height: 60, child: new ColoredBox(Colors.Magenta)))));

        harness.Pump(new Size(400, 600));

        var titleBox = FindDescendant<RenderAppBarTitleBox>(harness.RenderView);
        Assert.NotNull(titleBox);
        Assert.Equal(20.0, titleBox!.Size.Height, 3);
        Assert.Equal(40.0, titleBox.Size.Width, 3);

        RenderBox child = Assert.IsAssignableFrom<RenderBox>(titleBox.Child);
        Assert.Equal(60.0, child.Size.Height, 3);
        Assert.Equal(
            titleBox.LocalToGlobal(default).Y + (titleBox.Size.Height / 2.0),
            child.LocalToGlobal(default).Y + (child.Size.Height / 2.0),
            3);
    }

    // ---------------------------------------------------------------- system overlay style

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AppBar_SystemOverlayStyle_DerivesBrightnessFromTheBackground(bool useMaterial3)
    {
        SystemUiOverlayStyle onDark = OverlayStyleFor(Colors.Black, useMaterial3);
        Assert.Equal(SystemUiIconBrightness.Dark, onDark.StatusBarBrightness);
        Assert.Equal(SystemUiIconBrightness.Light, onDark.StatusBarIconBrightness);

        SystemUiOverlayStyle onLight = OverlayStyleFor(Colors.White, useMaterial3);
        Assert.Equal(SystemUiIconBrightness.Light, onLight.StatusBarBrightness);
        Assert.Equal(SystemUiIconBrightness.Dark, onLight.StatusBarIconBrightness);

        // M3 clears the status-bar color; M2 leaves it untouched.
        Assert.Equal(useMaterial3 ? MaterialColors.Transparent : null, onDark.StatusBarColor);
        Assert.Equal(useMaterial3 ? MaterialColors.Transparent : null, onLight.StatusBarColor);

        // The navigation bar is deliberately left alone.
        Assert.Null(onDark.NavigationBarColor);
        Assert.Null(onDark.NavigationBarIconBrightness);
    }

    // ---------------------------------------------------------------- scrolled under

    [Fact]
    public void AppBar_ScrolledUnder_IgnoresHorizontalScrollUpdates()
    {
        var background = WidgetStateColor.ResolveWith(
            states => states.Contains(WidgetState.ScrolledUnder) ? Colors.DarkGreen : Colors.Goldenrod);
        BuildContext? emitter = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildScrollProbe(background, context => emitter = context));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Equal(Colors.Goldenrod, MaterialColor(root));

        Dispatch(emitter!.Value, AxisDirection.Right, pixels: 12);
        owner.FlushBuild();
        Assert.Equal(Colors.Goldenrod, MaterialColor(root));

        Dispatch(emitter!.Value, AxisDirection.Left, pixels: 12);
        owner.FlushBuild();
        Assert.Equal(Colors.Goldenrod, MaterialColor(root));
    }

    [Fact]
    public void AppBar_ScrolledUnder_ReadsExtentAfterForReversedLists()
    {
        var background = WidgetStateColor.ResolveWith(
            states => states.Contains(WidgetState.ScrolledUnder) ? Colors.DarkGreen : Colors.Goldenrod);
        BuildContext? emitter = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildScrollProbe(background, context => emitter = context));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        // AxisDirection.Up: scrolled under while content remains *after* the viewport.
        Dispatch(emitter!.Value, AxisDirection.Up, pixels: 0);
        owner.FlushBuild();
        Assert.Equal(Colors.DarkGreen, MaterialColor(root));

        Dispatch(emitter!.Value, AxisDirection.Up, pixels: 100);
        owner.FlushBuild();
        Assert.Equal(Colors.Goldenrod, MaterialColor(root));
    }

    [Fact]
    public void AppBar_ScrolledUnder_DropsItsScrollListenerWhileADrawerIsOpen()
    {
        // Dart's `_AppBarState.didChangeDependencies` removes the scroll listener and returns early
        // without re-adding it while a drawer is open, so the last resolved scrolled-under state
        // survives every later scroll until the drawer closes and a dependency changes again.
        // `Scaffold.maybeOf` is `findAncestorStateOfType`, so opening the drawer does not itself
        // notify the app bar; a real dependency change (here the ambient `Theme`) is what re-runs
        // `didChangeDependencies`.
        var background = WidgetStateColor.ResolveWith(
            states => states.Contains(WidgetState.ScrolledUnder) ? Colors.DarkGreen : Colors.Goldenrod);
        BuildContext? emitter = null;
        ScaffoldState? scaffold = null;
        StateSetter? rebuildTheme = null;
        bool dark = false;
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MediaQuery(
                    data: new MediaQueryData(),
                    child: new StatefulBuilder((_, setState) =>
                    {
                        rebuildTheme = setState;
                        return new Theme(
                            data: dark ? ThemeData.Dark : ThemeData.Light,
                            child: new Scaffold(
                                appBar: new AppBar(title: new Text("Title"), backgroundColor: background),
                                drawer: new Drawer(child: new SizedBox()),
                                body: new Builder(context =>
                                {
                                    scaffold = Scaffold.Of(context);
                                    return new CaptureBuildContextWidget(inner => emitter = inner);
                                })));
                    }))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Dispatch(emitter!.Value, AxisDirection.Down, pixels: 12);
        owner.FlushBuild();
        Assert.Equal(Colors.DarkGreen, AppBarMaterialColor(root));

        scaffold!.OpenDrawer();
        owner.FlushBuild();
        AnimationPump.Advance(0.5);
        owner.FlushBuild();
        Assert.True(scaffold.IsDrawerOpen);

        // A dependency change while the drawer is open detaches the listener for good.
        rebuildTheme!(() => dark = true);
        owner.FlushBuild();

        Dispatch(emitter!.Value, AxisDirection.Down, pixels: 0);
        owner.FlushBuild();
        Assert.Equal(Colors.DarkGreen, AppBarMaterialColor(root));

        // Closing the drawer and changing a dependency again re-attaches it.
        scaffold.CloseDrawer();
        owner.FlushBuild();
        AnimationPump.Advance(0.5);
        owner.FlushBuild();
        Assert.False(scaffold.IsDrawerOpen);

        rebuildTheme!(() => dark = false);
        owner.FlushBuild();

        Dispatch(emitter!.Value, AxisDirection.Down, pixels: 0);
        owner.FlushBuild();
        Assert.Equal(Colors.Goldenrod, AppBarMaterialColor(root));
    }

    // ---------------------------------------------------------------- implied slots and themes

    [Fact]
    public void AppBar_ImpliedEndDrawerButton_IsNotWrappedInTheActionsPadding()
    {
        // Dart only pads the explicit `actions` row; the implied EndDrawerButton bypasses it.
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Directionality(
                textDirection: TextDirection.Ltr,
                child: new MediaQuery(
                    data: new MediaQueryData(),
                    child: new Theme(
                        data: ThemeData.Light with
                        {
                            AppBarTheme = new AppBarThemeData(ActionsPadding: new Thickness(0, 0, 37, 0)),
                        },
                        child: new Scaffold(
                            appBar: new AppBar(title: new Text("Title")),
                            endDrawer: new Drawer(child: new SizedBox()),
                            body: new SizedBox())))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Contains(FindWidgets<EndDrawerButton>(root.ChildElement), _ => true);
        Assert.DoesNotContain(
            FindWidgets<Padding>(root.ChildElement),
            padding => Math.Abs(padding.Insets.Right - 37) < 0.001);
    }

    [Fact]
    public void AppBar_TitleTextStyle_ReplacesTheResolvedDefaultInsteadOfMergingIntoIt()
    {
        // Dart resolves `widget.titleTextStyle ?? appBarTheme.titleTextStyle ?? defaults…`; it never
        // merges the widget style onto the default, so unset fields stay unset.
        var owner = new BuildOwner();
        var style = new TextStyle(FontSize: 30);
        var root = new TestRootElement(Wrap(new AppBar(primary: false, title: new Text("T"), titleTextStyle: style)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        DefaultTextStyle titleStyle = Assert.Single(
            FindWidgets<DefaultTextStyle>(root.ChildElement),
            candidate => candidate.Overflow == TextOverflow.Ellipsis);
        Assert.Equal(30, titleStyle.Style.FontSize);
        Assert.Null(titleStyle.Style.Color);
        Assert.False(titleStyle.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, titleStyle.Overflow);
    }

    [Fact]
    public void AppBar_ActionsIconTheme_FallsBackThroughTheIconThemeChain()
    {
        // widget.actionsIconTheme > theme.actionsIconTheme > widget.iconTheme > theme.iconTheme > defaults.
        Assert.Equal(Colors.Red, ResolvedActionsIconColor(
            new AppBar(actionsIconTheme: new IconThemeData(Color: Colors.Red)),
            new AppBarThemeData(ActionsIconTheme: new IconThemeData(Color: Colors.Blue))));
        Assert.Equal(Colors.Blue, ResolvedActionsIconColor(
            new AppBar(),
            new AppBarThemeData(ActionsIconTheme: new IconThemeData(Color: Colors.Blue))));
        Assert.Equal(Colors.Green, ResolvedActionsIconColor(
            new AppBar(iconTheme: new IconThemeData(Color: Colors.Green)),
            new AppBarThemeData()));
        Assert.Equal(Colors.Purple, ResolvedActionsIconColor(
            new AppBar(),
            new AppBarThemeData(IconTheme: new IconThemeData(Color: Colors.Purple))));

        // The M3 default action color is `onSurfaceVariant`, distinct from the leading icon color.
        ThemeData theme = ThemeData.Light;
        Assert.Equal(theme.ColorScheme.OnSurfaceVariant, ResolvedActionsIconColor(new AppBar(), new AppBarThemeData()));
    }

    [Fact]
    public void AppBar_Leading_ConstrainsWidthOnlyAndLeavesHeightToTheToolbar()
    {
        // Dart wraps the leading slot in `ConstrainedBox(BoxConstraints.tightFor(width: …))`; the
        // NavigationToolbar is what forces the height.
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(new AppBar(
            primary: false,
            title: new Text("Title"),
            leading: new SizedBox(width: 12, height: 12),
            leadingWidth: 64,
            toolbarHeight: 72)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Single(
            FindWidgets<ConstrainedBox>(root.ChildElement),
            box => box.Constraints == BoxConstraints.TightFor(width: 64));
    }

    // ---------------------------------------------------------------- opacity

    [Fact]
    public void AppBar_ToolbarOpacity_FadesTextStylesAndIconThemesThroughTheInterval()
    {
        double expected = Curves.Interval(0.25, 1.0, Curves.FastOutSlowIn)(0.5);
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(new AppBar(
            primary: false,
            title: new Text("Title"),
            toolbarOpacity: 0.5)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        // The toolbar is never wrapped in an Opacity: the fade lands on the text colors and icon theme.
        Assert.Empty(FindWidgets<Opacity>(root.ChildElement));

        DefaultTextStyle titleStyle = Assert.Single(
            FindWidgets<DefaultTextStyle>(root.ChildElement),
            candidate => candidate.Overflow == TextOverflow.Ellipsis);
        Assert.Equal(
            (byte)Math.Clamp((int)Math.Round(255 * expected), 0, 255),
            titleStyle.Style.Color!.Value.A);
    }

    [Fact]
    public void AppBar_BottomOpacity_WrapsTheBottomInAnOpacity()
    {
        double expected = Curves.Interval(0.25, 1.0, Curves.FastOutSlowIn)(0.5);
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(new AppBar(
            primary: false,
            title: new Text("Title"),
            bottom: new PreferredSize(new Size(0, 24), new SizedBox()),
            bottomOpacity: 0.5)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Opacity opacity = Assert.Single(FindWidgets<Opacity>(root.ChildElement));
        Assert.Equal(expected, opacity.Value, 3);
    }

    // ---------------------------------------------------------------- defaults

    [Fact]
    public void AppBar_Material2Defaults_MatchFlutter()
    {
        ThemeData theme = ThemeData.Light with { UseMaterial3 = false };
        AppBarThemeData defaults = AppBarDefaults.M2(theme);

        Assert.Equal(theme.ColorScheme.Primary, defaults.BackgroundColor);
        Assert.Equal(theme.ColorScheme.OnPrimary, defaults.ForegroundColor);
        Assert.Equal(4.0, defaults.Elevation);
        Assert.Null(defaults.ScrolledUnderElevation);
        Assert.Equal(Colors.Black, defaults.ShadowColor);
        Assert.Null(defaults.SurfaceTintColor);
        Assert.Equal(theme.IconTheme, defaults.IconTheme);
        Assert.Null(defaults.ActionsIconTheme);
        Assert.Equal(16.0, defaults.TitleSpacing);
        Assert.Equal(56.0, defaults.ToolbarHeight);
        Assert.Equal(new Thickness(), defaults.ActionsPadding);
        Assert.Null(defaults.Shape);
        Assert.Null(defaults.CenterTitle);
        Assert.Null(defaults.LeadingWidth);
        Assert.Null(defaults.SystemOverlayStyle);

        ThemeData dark = ThemeData.Dark with { UseMaterial3 = false };
        Assert.Equal(dark.ColorScheme.Surface, AppBarDefaults.M2(dark).BackgroundColor);
        Assert.Equal(dark.ColorScheme.OnSurface, AppBarDefaults.M2(dark).ForegroundColor);
    }

    [Fact]
    public void AppBar_Material3Defaults_MatchFlutter()
    {
        ThemeData theme = ThemeData.Light;
        AppBarThemeData defaults = AppBarDefaults.M3(theme);

        Assert.Equal(theme.ColorScheme.Surface, defaults.BackgroundColor);
        Assert.Equal(theme.ColorScheme.OnSurface, defaults.ForegroundColor);
        Assert.Equal(0.0, defaults.Elevation);
        Assert.Equal(3.0, defaults.ScrolledUnderElevation);
        Assert.Equal(MaterialColors.Transparent, defaults.ShadowColor);
        Assert.Equal(MaterialColors.Transparent, defaults.SurfaceTintColor);
        Assert.Equal(new IconThemeData(Color: theme.ColorScheme.OnSurface, Size: 24.0), defaults.IconTheme);
        Assert.Equal(
            new IconThemeData(Color: theme.ColorScheme.OnSurfaceVariant, Size: 24.0),
            defaults.ActionsIconTheme);
        Assert.Equal(16.0, defaults.TitleSpacing);

        // 64.0 is the token value; neither `build` nor `preferredHeightFor` reads it.
        Assert.Equal(64.0, defaults.ToolbarHeight);
        Assert.Equal(56.0, new AppBar().PreferredSize.Height);
        Assert.Null(defaults.Shape);
        Assert.Null(defaults.CenterTitle);
        Assert.Null(defaults.LeadingWidth);
        Assert.Null(defaults.SystemOverlayStyle);
    }

    [Fact]
    public void AppBar_SurfaceTintColor_SkipsTheTransparentMaterial3Default()
    {
        // Taking `defaults.surfaceTintColor` (transparent in M3) would defeat scrolledUnderElevation, so
        // Dart falls through to `colorScheme.surfaceTint`.
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(new AppBar(primary: false, title: new Text("Title"))));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        MaterialWidget material = FindWidgets<MaterialWidget>(root.ChildElement)[0];
        Assert.Equal(ThemeData.Light.ColorScheme.SurfaceTint, material.SurfaceTintColor);
    }

    // ---------------------------------------------------------------- helpers

    private static Widget Wrap(Widget child, ThemeData? theme = null)
    {
        return new Directionality(
            textDirection: TextDirection.Ltr,
            child: new MediaQuery(
                data: new MediaQueryData(),
                child: new Theme(data: theme ?? ThemeData.Light, child: child)));
    }

    private static Widget BuildScrollProbe(WidgetStateColor background, Action<BuildContext> capture)
    {
        return Wrap(new ScrollNotificationObserver(
            child: new Column(
                children:
                [
                    new AppBar(primary: false, title: new Text("Title"), backgroundColor: background),
                    new CaptureBuildContextWidget(capture),
                ])));
    }

    private static void Dispatch(BuildContext context, AxisDirection axisDirection, double pixels)
    {
        new ScrollUpdateNotification(
            new FixedScrollMetrics(
                minScrollExtent: 0,
                maxScrollExtent: 100,
                pixels: pixels,
                viewportDimension: 40,
                axisDirection: axisDirection,
                devicePixelRatio: 1.0)).Dispatch(context);
    }

    private static Color MaterialColor(TestRootElement root)
    {
        return FindWidgets<MaterialWidget>(root.ChildElement)[0].Color!.Value;
    }

    // A `Scaffold` roots itself in a `Material`, so inside one the app bar's own Material has to be
    // looked up through the AppBar element rather than as the first Material in the tree.
    private static Color AppBarMaterialColor(TestRootElement root)
    {
        Element? appBar = FindElement(root.ChildElement, element => element.Widget is AppBar);
        Assert.NotNull(appBar);
        return FindWidgets<MaterialWidget>(appBar)[0].Color!.Value;
    }

    private static Element? FindElement(Element? element, Func<Element, bool> predicate)
    {
        if (element is null)
        {
            return null;
        }

        if (predicate(element))
        {
            return element;
        }

        Element? found = null;
        element.VisitChildren(child => found ??= FindElement(child, predicate));
        return found;
    }

    private static SystemUiOverlayStyle OverlayStyleFor(Color background, bool useMaterial3)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(
            new AppBar(primary: false, title: new Text("Title"), backgroundColor: background),
            ThemeData.Light with { UseMaterial3 = useMaterial3 }));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        return Assert.Single(FindWidgets<AnnotatedRegion<SystemUiOverlayStyle>>(root.ChildElement)).Value;
    }

    private static Color? ResolvedActionsIconColor(AppBar template, AppBarThemeData appBarTheme)
    {
        var owner = new BuildOwner();
        var appBar = new AppBar(
            primary: false,
            title: new Text("Title"),
            iconTheme: template.IconTheme,
            actionsIconTheme: template.ActionsIconTheme,
            actions: [new Icon(Icons.Close)]);
        var root = new TestRootElement(Wrap(
            appBar,
            ThemeData.Light with { AppBarTheme = appBarTheme }));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        IReadOnlyList<IconTheme> themes = FindWidgets<IconTheme>(root.ChildElement);
        return themes[^1].Data.Color;
    }

    private static BuildContext CaptureContext(AppBarThemeData appBarTheme)
    {
        BuildContext? captured = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(Wrap(
            new CaptureBuildContextWidget(context => captured = context),
            ThemeData.Light with { AppBarTheme = appBarTheme }));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.NotNull(captured);
        return captured!.Value;
    }

    private static IReadOnlyList<T> FindWidgets<T>(Element? element) where T : Widget
    {
        var widgets = new List<T>();
        FindWidgets(element, widgets);
        return widgets;
    }

    private static void FindWidgets<T>(Element? element, List<T> widgets) where T : Widget
    {
        if (element is null)
        {
            return;
        }

        if (element.Widget is T widget)
        {
            widgets.Add(widget);
        }

        element.VisitChildren(child => FindWidgets(child, widgets));
    }

    private static T? FindDescendant<T>(RenderObject? root, Predicate<T>? predicate = null) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match && (predicate is null || predicate(match)))
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendant(child, predicate);
        });

        return result;
    }

    private sealed class CaptureBuildContextWidget : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;

        public CaptureBuildContextWidget(Action<BuildContext> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return new SizedBox();
        }
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

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
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
                if (_child != null)
                {
                    visitor(_child);
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
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = null;
            }
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
