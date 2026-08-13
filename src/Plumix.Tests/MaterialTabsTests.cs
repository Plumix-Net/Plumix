using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTabsTests
{
    public MaterialTabsTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    // ---------------------------------------------------------------- Tab

    [Fact]
    public void Tab_ValidatesContentAndMatchesPreferredHeights()
    {
        Assert.Throws<ArgumentException>(() => new Tab());
        Assert.Throws<ArgumentException>(() => new Tab(text: "A", child: new Text("B")));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tab(text: "A", height: -1));

        Assert.Equal(46, new Tab(text: "Text").PreferredSize.Height);
        Assert.Equal(46, new Tab(icon: new Icon(Icons.Menu)).PreferredSize.Height);
        Assert.Equal(46, new Tab(child: new Text("Child")).PreferredSize.Height);
        Assert.Equal(72, new Tab(text: "Text", icon: new Icon(Icons.Menu)).PreferredSize.Height);
        Assert.Equal(72, new Tab(child: new Text("Child"), icon: new Icon(Icons.Menu)).PreferredSize.Height);
        Assert.Equal(85, new Tab(text: "Text", height: 85).PreferredSize.Height);
        Assert.Equal(85, new Tab(text: "Text", icon: new Icon(Icons.Menu), height: 85).PreferredSize.Height);
    }

    [Fact]
    public void Tab_DefaultIconMarginFollowsMaterialVersion()
    {
        using var m3 = new WidgetRenderHarness(Wrap(new Tab(text: "A", icon: new SizedBox(width: 10, height: 10))));
        m3.Pump(new Size(200, 100));
        Assert.Equal(
            new Thickness(0, 0, 0, 2),
            FindDescendants<RenderPadding>(m3.RenderView).Select(p => p.Padding).First(i => i.Bottom > 0));

        using var m2 = new WidgetRenderHarness(Wrap(
            new Tab(text: "A", icon: new SizedBox(width: 10, height: 10)),
            ThemeData.Light with { UseMaterial3 = false }));
        m2.Pump(new Size(200, 100));
        Assert.Equal(
            new Thickness(0, 0, 0, 10),
            FindDescendants<RenderPadding>(m2.RenderView).Select(p => p.Padding).First(i => i.Bottom > 0));

        using var custom = new WidgetRenderHarness(Wrap(new Tab(
            text: "A",
            icon: new SizedBox(width: 10, height: 10),
            iconMargin: EdgeInsetsGeometry.Symmetric(horizontal: 100))));
        custom.Pump(new Size(400, 200));
        Assert.Equal(210, FindDescendant<RenderFlex>(custom.RenderView)!.Size.Width, precision: 3);
    }

    // ------------------------------------------------------- TabController

    [Fact]
    public void TabController_DefaultsAndImmediateIndexChangeMatchFlutter()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);

        Assert.Equal(3, controller.Length);
        Assert.Equal(1, controller.Index);
        Assert.Equal(1, controller.PreviousIndex);
        Assert.Equal(1, controller.Animation!.Value);
        Assert.Equal(TimeSpan.FromMilliseconds(300), controller.AnimationDuration);
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(0, controller.Offset);

        controller.Index = 2;

        Assert.Equal(2, controller.Index);
        Assert.Equal(1, controller.PreviousIndex);
        Assert.Equal(2, controller.Animation!.Value);
        Assert.Equal(0, controller.Offset);
        Assert.False(controller.IndexIsChanging);
    }

    [Fact]
    public void TabController_AnimateToExposesChangingLifecycleAndInterpolatedValue()
    {
        using var controller = new TabController(length: 3);
        int notifications = 0;
        controller.AddListener(() => notifications++);
        controller.AnimateTo(2);

        Assert.Equal(2, controller.Index);
        Assert.Equal(0, controller.PreviousIndex);
        Assert.True(controller.IndexIsChanging);
        Assert.Equal(1, notifications);
        Assert.Equal(0, controller.Animation!.Value);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.16));
        Assert.InRange(controller.Animation.Value, 0.5, 1.9);
        Assert.True(controller.IndexIsChanging);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        Assert.Equal(2, controller.Animation.Value);
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void TabController_ZeroDurationChangesIndexInstantly()
    {
        using var controller = new TabController(length: 3, animationDuration: TimeSpan.Zero);
        controller.AnimateTo(2);

        Assert.Equal(2, controller.Index);
        Assert.Equal(0, controller.PreviousIndex);
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(2, controller.Animation!.Value);
    }

    [Fact]
    public void TabController_ValidatesLengthIndexAndOffsetAndIgnoresShortLengths()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabController(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabController(2, 2));

        using var single = new TabController(1);
        single.Index = 0;
        Assert.Equal(0, single.Index);

        // A zero-length controller accepts any index and ignores it, as Dart's asserts allow.
        using var empty = new TabController(0);
        empty.Index = 1;
        Assert.Equal(0, empty.Index);

        using var controller = new TabController(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Offset = 1.1);
        controller.Offset = 0.5;
        Assert.Equal(0.5, controller.Animation!.Value);
        Assert.Equal(0.5, controller.Offset);

        controller.AnimateTo(1);
        Assert.Throws<InvalidOperationException>(() => controller.Offset = 0.2);
    }

    [Fact]
    public void TabController_DisposeClearsAnimationAndSuppressesPendingNotification()
    {
        var controller = new TabController(length: 3);
        int notifications = 0;
        controller.AddListener(() => notifications++);
        controller.AnimateTo(2);
        Assert.Equal(1, notifications);

        controller.Dispose();

        Assert.Null(controller.Animation);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void DefaultTabController_ProvidesConfiguredControllerToDescendants()
    {
        TabController? captured = null;
        using var harness = new WidgetRenderHarness(Wrap(new DefaultTabController(
            length: 3,
            initialIndex: 2,
            animationDuration: TimeSpan.FromMilliseconds(450),
            child: new ControllerProbe(controller => captured = controller))));
        harness.Pump(new Size(100, 40));

        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Length);
        Assert.Equal(2, captured.Index);
        Assert.Equal(TimeSpan.FromMilliseconds(450), captured.AnimationDuration);
    }

    [Fact]
    public void DefaultTabController_LengthChangeKeepsAnimationAndClampsIndex()
    {
        using var controller = new TabController(length: 4, initialIndex: 3);
        Animation<double> animation = controller.Animation!;

        TabController shortened = controller.CopyWithAndDispose(
            index: 1,
            length: 2,
            previousIndex: 3,
            animationDuration: null);

        Assert.Same(animation, shortened.Animation);
        Assert.Null(controller.Animation);
        Assert.Equal(1, shortened.Index);
        Assert.Equal(3, shortened.PreviousIndex);
        Assert.Equal(2, shortened.Length);
        Assert.Equal(1, shortened.Animation!.Value);
        shortened.Dispose();
    }

    [Fact]
    public void DefaultTabController_SupportsZeroAndDynamicLengths()
    {
        var stateKey = new LabeledGlobalKey<State>("dynamic tabs");
        int length = 0;
        Widget Build() => Wrap(new DefaultTabController(
            length: length,
            child: new ControllerProbe(_ => { }, stateKey)));

        using var harness = new WidgetRenderHarness(Build());
        harness.Pump(new Size(200, 60));

        length = 3;
        harness.Update(Build());
        harness.Pump(new Size(200, 60));

        length = 1;
        harness.Update(Build());
        harness.Pump(new Size(200, 60));
    }

    // ------------------------------------------------------ TabBarThemeData

    [Fact]
    public void TabBarThemeData_DefaultsCopyWithAndEqualityMatchFlutter()
    {
        var defaults = new TabBarThemeData();
        Assert.Null(defaults.Indicator);
        Assert.Null(defaults.IndicatorColor);
        Assert.Null(defaults.IndicatorSize);
        Assert.Null(defaults.DividerColor);
        Assert.Null(defaults.DividerHeight);
        Assert.Null(defaults.LabelColor);
        Assert.Null(defaults.LabelPadding);
        Assert.Null(defaults.LabelStyle);
        Assert.Null(defaults.UnselectedLabelColor);
        Assert.Null(defaults.UnselectedLabelStyle);
        Assert.Null(defaults.OverlayColor);
        Assert.Null(defaults.SplashFactory);
        Assert.Null(defaults.MouseCursor);
        Assert.Null(defaults.TabAlignment);
        Assert.Null(defaults.TextScaler);
        Assert.Null(defaults.IndicatorAnimation);
        Assert.Null(defaults.SplashBorderRadius);

        Assert.Equal(defaults, defaults.CopyWith());
        Assert.Equal(defaults.GetHashCode(), defaults.CopyWith().GetHashCode());

        TabBarThemeData populated = defaults.CopyWith(
            indicatorColor: Colors.Red,
            dividerHeight: 20.5,
            tabAlignment: TabAlignment.Center,
            textScaler: TextScaler.NoScaling,
            indicatorAnimation: TabIndicatorAnimation.Elastic,
            splashBorderRadius: BorderRadius.Circular(20));
        Assert.Equal(Colors.Red, populated.IndicatorColor);
        Assert.Equal(20.5, populated.DividerHeight);
        Assert.Equal(TabAlignment.Center, populated.TabAlignment);
        Assert.Same(TextScaler.NoScaling, populated.TextScaler);
        Assert.Equal(TabIndicatorAnimation.Elastic, populated.IndicatorAnimation);
        Assert.Equal(BorderRadius.Circular(20), populated.SplashBorderRadius);
        Assert.NotEqual(defaults, populated);
    }

    [Fact]
    public void TabBarThemeData_LerpIdentityAndFieldRules()
    {
        var theme = new TabBarThemeData(IndicatorColor: Colors.Red);
        Assert.Same(theme, TabBarThemeData.Lerp(theme, theme, 0.5));

        var a = new TabBarThemeData(
            IndicatorColor: Color.FromArgb(255, 0, 0, 0),
            DividerHeight: 1,
            IndicatorSize: TabBarIndicatorSize.Tab,
            LabelPadding: EdgeInsetsGeometry.All(0),
            TabAlignment: TabAlignment.Center);
        var b = new TabBarThemeData(
            IndicatorColor: Color.FromArgb(255, 100, 100, 100),
            DividerHeight: 5,
            IndicatorSize: TabBarIndicatorSize.Label,
            LabelPadding: EdgeInsetsGeometry.All(10),
            TabAlignment: TabAlignment.Fill);

        TabBarThemeData mid = TabBarThemeData.Lerp(a, b, 0.5);
        Assert.Equal(Color.FromArgb(255, 50, 50, 50), mid.IndicatorColor);
        Assert.Equal(EdgeInsetsGeometry.All(5), mid.LabelPadding);
        // Discrete fields snap at the midpoint rather than interpolating.
        Assert.Equal(TabBarIndicatorSize.Label, mid.IndicatorSize);
        Assert.Equal(5, mid.DividerHeight);
        Assert.Equal(TabAlignment.Fill, mid.TabAlignment);
        Assert.Equal(TabBarIndicatorSize.Tab, TabBarThemeData.Lerp(a, b, 0.4).IndicatorSize);
    }

    [Fact]
    public void TabBarTheme_DataAndIndividualPropertiesAreMutuallyExclusive()
    {
        Assert.Throws<ArgumentException>(() => new TabBarTheme(
            data: new TabBarThemeData(),
            indicatorColor: Colors.Red,
            child: new SizedBox()));

        var widget = new TabBarTheme(indicatorColor: Colors.Red, dividerHeight: 4, child: new SizedBox());
        Assert.Equal(Colors.Red, widget.IndicatorColor);
        Assert.Equal(Colors.Red, widget.Data.IndicatorColor);
        Assert.Equal(4, widget.Data.DividerHeight);

        var wrapped = new TabBarTheme(data: new TabBarThemeData(IndicatorColor: Colors.Lime), child: new SizedBox());
        Assert.Equal(Colors.Lime, wrapped.IndicatorColor);

        // copyWith drops key/child/data, matching Dart.
        TabBarTheme copy = widget.CopyWith(dividerHeight: 8);
        Assert.Equal(Colors.Red, copy.IndicatorColor);
        Assert.Equal(8, copy.DividerHeight);
    }

    [Fact]
    public void TabBarTheme_OfFallsBackToThemeDataAndAncestorWins()
    {
        TabBarThemeData? fromTheme = null;
        TabBarThemeData? fromAncestor = null;
        var themeData = ThemeData.Light with
        {
            TabBarTheme = new TabBarThemeData(DividerHeight: 7),
        };

        using var harness = new WidgetRenderHarness(Wrap(
            new Column(children:
            [
                new Builder(context => { fromTheme = TabBarTheme.Of(context); return new SizedBox(); }),
                new TabBarTheme(
                    data: new TabBarThemeData(DividerHeight: 9),
                    child: new Builder(context =>
                    {
                        fromAncestor = TabBarTheme.Of(context);
                        return new SizedBox();
                    })),
            ]),
            themeData));
        harness.Pump(new Size(200, 100));

        Assert.Equal(7, fromTheme!.DividerHeight);
        Assert.Equal(9, fromAncestor!.DividerHeight);
    }

    // ------------------------------------------------- UnderlineTabIndicator

    [Fact]
    public void UnderlineTabIndicator_DefaultsGeometryAndLerp()
    {
        var indicator = new UnderlineTabIndicator();
        Assert.Null(indicator.BorderRadius);
        Assert.Equal(Colors.White, indicator.BorderSide.Color);
        Assert.Equal(2.0, indicator.BorderSide.Width);
        Assert.Equal(EdgeInsetsGeometry.Zero, indicator.Padding);

        var inset = new UnderlineTabIndicator(
            borderSide: new BorderSide(Colors.Red, 8),
            insets: EdgeInsetsGeometry.Only(left: 8, right: 4));
        Rect rect = inset.IndicatorRectFor(new Rect(0, 0, 200, 54), TextDirection.Ltr);
        Assert.Equal(new Rect(8, 46, 188, 8), rect);

        // Directional insets mirror with the text direction.
        var directional = new UnderlineTabIndicator(
            borderSide: new BorderSide(Colors.Red, 2),
            insets: EdgeInsetsGeometry.DirectionalOnly(start: 100));
        Assert.Equal(100, directional.IndicatorRectFor(new Rect(0, 0, 200, 50), TextDirection.Ltr).Left);
        Assert.Equal(0, directional.IndicatorRectFor(new Rect(0, 0, 200, 50), TextDirection.Rtl).Left);

        // lerp interpolates the side and insets and drops the border radius, as Dart does.
        var from = new UnderlineTabIndicator(
            borderRadius: BorderRadius.Circular(3),
            borderSide: new BorderSide(Colors.Black, 2),
            insets: EdgeInsetsGeometry.All(0));
        var to = new UnderlineTabIndicator(
            borderSide: new BorderSide(Colors.Black, 6),
            insets: EdgeInsetsGeometry.All(10));
        var lerped = Assert.IsType<UnderlineTabIndicator>(to.LerpFrom(from, 0.5));
        Assert.Equal(4.0, lerped.BorderSide.Width);
        Assert.Equal(EdgeInsetsGeometry.All(5), lerped.Insets);
        Assert.Null(lerped.BorderRadius);
        Assert.Equal(4.0, Assert.IsType<UnderlineTabIndicator>(from.LerpTo(to, 0.5)).BorderSide.Width);
    }

    // ------------------------------------------------------------- TabBar

    [Fact]
    public void TabBar_DefaultSurfaceMatchesFlutter()
    {
        var tabs = new[] { new Tab(text: "One"), new Tab(text: "Two") };
        var bar = new TabBar(tabs);

        Assert.Same(tabs, bar.Tabs);
        Assert.False(bar.IsScrollable);
        Assert.True(bar.AutomaticIndicatorColorAdjustment);
        Assert.Equal(2, bar.IndicatorWeight);
        Assert.Equal(EdgeInsetsGeometry.Zero, bar.IndicatorPadding);
        Assert.Equal(DragStartBehavior.Start, bar.DragStartBehavior);
        Assert.Null(bar.Controller);
        Assert.Null(bar.IndicatorSize);
        Assert.Null(bar.TextScaler);
        Assert.Null(bar.SplashFactory);
        Assert.False(bar.TabHasTextAndIcon);
        Assert.Equal(48, bar.PreferredSize.Height);

        var withIcons = new TabBar([new Tab(text: "One", icon: new Icon(Icons.Menu)), new Tab(text: "Two")]);
        Assert.True(withIcons.TabHasTextAndIcon);
        Assert.Equal(74, withIcons.PreferredSize.Height);

        var appBar = new AppBar(bottom: bar);
        Assert.Equal(104, appBar.PreferredSize.Height);
    }

    [Fact]
    public void TabBar_Material3PrimaryDefaultsUseLabelIndicatorAndOutlineDivider()
    {
        using var controller = new TabController(3);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "One"), new Tab(text: "Two"), new Tab(text: "Three")])));
        harness.Pump(new Size(300, 100));

        IndicatorPainter painter = RequireIndicatorPainter(harness.RenderView);
        var indicator = Assert.IsType<UnderlineTabIndicator>(painter.Indicator);
        Assert.Equal(TabBarIndicatorSize.Label, painter.IndicatorSize);
        Assert.Equal(TabIndicatorAnimation.Elastic, painter.IndicatorAnimation);
        Assert.Equal(ThemeData.Light.ColorScheme.Primary, indicator.BorderSide.Color);
        // M3 primary with a label indicator forces weight 3 and rounds the top corners.
        Assert.Equal(3.0, indicator.BorderSide.Width);
        Assert.Equal(3.0, indicator.BorderRadius!.Value.TopLeft);
        Assert.Equal(3.0, indicator.BorderRadius.Value.TopRight);
        Assert.Equal(0.0, indicator.BorderRadius.Value.BottomLeft);
        Assert.Equal(ThemeData.Light.ColorScheme.OutlineVariant, painter.DividerColor);
        Assert.Equal(1.0, painter.DividerHeight);
        Assert.True(painter.ShowDivider);

        // TabAlignment.fill splits the bar evenly between the three tabs.
        var flex = FindDescendant<RenderFlex>(harness.RenderView)!;
        Assert.Equal(3, TabRects(flex).Count);
        Assert.All(TabRects(flex), rect => Assert.Equal(100, rect.Width, precision: 3));
    }

    [Fact]
    public void TabBar_Material3SecondaryDefaultsUseTabIndicatorWithoutRadius()
    {
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(TabBar.Secondary(
            controller: controller,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size(300, 100));

        IndicatorPainter painter = RequireIndicatorPainter(harness.RenderView);
        var indicator = Assert.IsType<UnderlineTabIndicator>(painter.Indicator);
        Assert.Equal(TabBarIndicatorSize.Tab, painter.IndicatorSize);
        Assert.Equal(TabIndicatorAnimation.Linear, painter.IndicatorAnimation);
        Assert.Equal(2.0, indicator.BorderSide.Width);
        Assert.Null(indicator.BorderRadius);
        Assert.Equal(ThemeData.Light.ColorScheme.Primary, indicator.BorderSide.Color);
    }

    [Fact]
    public void TabBar_Material2DefaultsUseSecondaryIndicatorAndPrimaryTextTheme()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(
            Wrap(new TabBar(controller: controller, tabs: [new Tab(text: "One"), new Tab(text: "Two")]), theme));
        harness.Pump(new Size(300, 100));

        IndicatorPainter painter = RequireIndicatorPainter(harness.RenderView);
        var indicator = Assert.IsType<UnderlineTabIndicator>(painter.Indicator);
        Assert.Equal(TabBarIndicatorSize.Tab, painter.IndicatorSize);
        Assert.Equal(2.0, indicator.BorderSide.Width);
        Assert.Null(indicator.BorderRadius);
        Assert.Equal(theme.ColorScheme.Secondary, indicator.BorderSide.Color);
        // M2 has no divider defaults at all.
        Assert.Null(painter.DividerColor);
        Assert.Null(painter.DividerHeight);
        Assert.False(painter.ShowDivider);

        TextStyle selected = SelectedLabelStyle(harness.RenderView);
        Assert.Equal(theme.PrimaryTextTheme.BodyLarge.Color, selected.Color);
    }

    [Fact]
    public void TabBar_Material2UnselectedLabelInheritsSelectedColorAt70Percent()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(
            Wrap(
                new TabBar(
                    controller: controller,
                    labelColor: Color.FromArgb(0xFF, 0x00, 0x00, 0xFF),
                    tabs: [new Tab(text: "One"), new Tab(text: "Two")]),
                theme));
        harness.Pump(new Size(300, 100));

        IReadOnlyList<TextStyle> styles = LabelStyles(harness.RenderView);
        Assert.Equal(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF), styles[0].Color);
        Assert.Equal(Color.FromArgb(0xB2, 0x00, 0x00, 0xFF), styles[1].Color);
    }

    [Fact]
    public void TabBar_Material3LabelColorsUseSchemeRolesAndStateResolution()
    {
        using var controller = new TabController(2);
        using var primary = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        primary.Pump(new Size(300, 100));
        IReadOnlyList<TextStyle> primaryStyles = LabelStyles(primary.RenderView);
        Assert.Equal(ThemeData.Light.ColorScheme.Primary, primaryStyles[0].Color);
        Assert.Equal(ThemeData.Light.ColorScheme.OnSurfaceVariant, primaryStyles[1].Color);

        using var secondaryController = new TabController(2);
        using var secondary = new WidgetRenderHarness(Wrap(TabBar.Secondary(
            controller: secondaryController,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        secondary.Pump(new Size(300, 100));
        Assert.Equal(ThemeData.Light.ColorScheme.OnSurface, LabelStyles(secondary.RenderView)[0].Color);

        // A state-resolving labelColor supplies both the selected and unselected colors, and
        // unselectedLabelColor is ignored.
        using var stateController = new TabController(2);
        using var stateful = new WidgetRenderHarness(Wrap(new TabBar(
            controller: stateController,
            labelColor: WidgetStateColor.ResolveWith(states =>
                states.Contains(WidgetState.Selected) ? Colors.Red : Colors.Green),
            unselectedLabelColor: Colors.Purple,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        stateful.Pump(new Size(300, 100));
        IReadOnlyList<TextStyle> stateStyles = LabelStyles(stateful.RenderView);
        Assert.Equal(Colors.Red, stateStyles[0].Color);
        Assert.Equal(Colors.Green, stateStyles[1].Color);
    }

    [Fact]
    public void TabBar_LabelColorAndStyleFollowWidgetThemeDefaultPrecedence()
    {
        var theme = ThemeData.Light with
        {
            TabBarTheme = new TabBarThemeData(
                LabelColor: Colors.Red,
                UnselectedLabelColor: Colors.Blue,
                LabelStyle: new TextStyle(FontSize: 32),
                UnselectedLabelStyle: new TextStyle(FontSize: 11)),
        };
        using var controller = new TabController(2);
        using var themed = new WidgetRenderHarness(
            Wrap(new TabBar(controller: controller, tabs: [new Tab(text: "One"), new Tab(text: "Two")]), theme));
        themed.Pump(new Size(300, 100));
        IReadOnlyList<TextStyle> themedStyles = LabelStyles(themed.RenderView);
        Assert.Equal(Colors.Red, themedStyles[0].Color);
        Assert.Equal(Colors.Blue, themedStyles[1].Color);
        Assert.Equal(32, themedStyles[0].FontSize);
        Assert.Equal(11, themedStyles[1].FontSize);

        using var widgetController = new TabController(2);
        using var overridden = new WidgetRenderHarness(
            Wrap(
                new TabBar(
                    controller: widgetController,
                    labelColor: Colors.Lime,
                    unselectedLabelColor: Colors.Teal,
                    tabs: [new Tab(text: "One"), new Tab(text: "Two")]),
                theme));
        overridden.Pump(new Size(300, 100));
        IReadOnlyList<TextStyle> widgetStyles = LabelStyles(overridden.RenderView);
        Assert.Equal(Colors.Lime, widgetStyles[0].Color);
        Assert.Equal(Colors.Teal, widgetStyles[1].Color);
    }

    [Fact]
    public void TabBar_LabelStyleColorIsUsedWhenNoLabelColorIsSet()
    {
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            labelStyle: new TextStyle(Color: Colors.Red, FontStyle: FontStyle.Italic),
            unselectedLabelStyle: new TextStyle(Color: Colors.Blue),
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size(300, 100));

        IReadOnlyList<TextStyle> styles = LabelStyles(harness.RenderView);
        Assert.Equal(Colors.Red, styles[0].Color);
        Assert.Equal(Colors.Blue, styles[1].Color);
        Assert.Equal(FontStyle.Italic, styles[0].FontStyle);
    }

    [Fact]
    public void TabBar_IndicatorAndDividerPrecedenceAndAutomaticColorAdjustment()
    {
        var theme = ThemeData.Light with
        {
            TabBarTheme = new TabBarThemeData(
                IndicatorColor: Colors.Crimson,
                IndicatorSize: TabBarIndicatorSize.Tab,
                DividerColor: Colors.DarkCyan,
                DividerHeight: 4),
        };
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(
            Wrap(new TabBar(controller: controller, tabs: [new Tab(text: "One"), new Tab(text: "Two")]), theme));
        harness.Pump(new Size(300, 100));

        IndicatorPainter painter = RequireIndicatorPainter(harness.RenderView);
        Assert.Equal(Colors.Crimson, ((UnderlineTabIndicator)painter.Indicator).BorderSide.Color);
        Assert.Equal(TabBarIndicatorSize.Tab, painter.IndicatorSize);
        Assert.Equal(Colors.DarkCyan, painter.DividerColor);
        Assert.Equal(4, painter.DividerHeight);

        using var widgetController = new TabController(2);
        using var widgetLevel = new WidgetRenderHarness(
            Wrap(
                new TabBar(
                    controller: widgetController,
                    indicatorColor: Colors.Lime,
                    dividerColor: Colors.Magenta,
                    dividerHeight: 9,
                    tabs: [new Tab(text: "One"), new Tab(text: "Two")]),
                theme));
        widgetLevel.Pump(new Size(300, 100));
        IndicatorPainter widgetPainter = RequireIndicatorPainter(widgetLevel.RenderView);
        Assert.Equal(Colors.Lime, ((UnderlineTabIndicator)widgetPainter.Indicator).BorderSide.Color);
        Assert.Equal(Colors.Magenta, widgetPainter.DividerColor);
        Assert.Equal(9, widgetPainter.DividerHeight);
    }

    [Fact]
    public void TabBar_AutomaticIndicatorColorAdjustmentSwitchesToWhiteOverMatchingMaterial()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        Color materialColor = theme.ColorScheme.Secondary;
        using var controller = new TabController(2);
        using var matched = new WidgetRenderHarness(
            Wrap(
                new global::Plumix.Material.Material(
                    color: materialColor,
                    child: new TabBar(
                        controller: controller,
                        tabs: [new Tab(text: "One"), new Tab(text: "Two")])),
                theme));
        matched.Pump(new Size(300, 100));
        Assert.Equal(
            Colors.White,
            ((UnderlineTabIndicator)RequireIndicatorPainter(matched.RenderView).Indicator).BorderSide.Color);

        using var offController = new TabController(2);
        using var disabled = new WidgetRenderHarness(
            Wrap(
                new global::Plumix.Material.Material(
                    color: materialColor,
                    child: new TabBar(
                        controller: offController,
                        automaticIndicatorColorAdjustment: false,
                        tabs: [new Tab(text: "One"), new Tab(text: "Two")])),
                theme));
        disabled.Pump(new Size(300, 100));
        Assert.Equal(
            materialColor,
            ((UnderlineTabIndicator)RequireIndicatorPainter(disabled.RenderView).Indicator).BorderSide.Color);
    }

    [Fact]
    public void TabBar_IndicatorWeightAndPaddingProduceExactRectsInBothDirections()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        Widget Build(TextDirection direction, TabController controller) => new Directionality(
            direction,
            new Theme(
                theme,
                new TabBar(
                    controller: controller,
                    indicatorWeight: 8,
                    indicatorPadding: EdgeInsetsGeometry.Only(left: 8, right: 4),
                    tabs:
                    [
                        new Tab(text: "A"),
                        new Tab(text: "B"),
                        new Tab(text: "C"),
                        new Tab(text: "D"),
                    ])));

        using var ltrController = new TabController(4);
        using var ltr = new WidgetRenderHarness(Build(TextDirection.Ltr, ltrController));
        ltr.Pump(new Size(800, 200));
        // 46 (tab height) + 8 (indicator weight)
        Assert.Equal(54, FindDescendant<RenderCustomPaint>(ltr.RenderView)!.Size.Height, precision: 3);
        Assert.Equal(new Rect(8, 0, 188, 54), RequireIndicatorPainter(ltr.RenderView).CurrentRect);

        ltrController.Index = 3;
        ltr.Pump(new Size(800, 200));
        Assert.Equal(new Rect(608, 0, 188, 54), RequireIndicatorPainter(ltr.RenderView).CurrentRect);

        using var rtlController = new TabController(4);
        using var rtl = new WidgetRenderHarness(Build(TextDirection.Rtl, rtlController));
        rtl.Pump(new Size(800, 200));
        // The insets are non-directional, so only the tab rect mirrors.
        Assert.Equal(new Rect(608, 0, 188, 54), RequireIndicatorPainter(rtl.RenderView).CurrentRect);

        rtlController.Index = 3;
        rtl.Pump(new Size(800, 200));
        Assert.Equal(new Rect(8, 0, 188, 54), RequireIndicatorPainter(rtl.RenderView).CurrentRect);
    }

    [Fact]
    public void TabBar_LinearAndElasticIndicatorAnimationsDifferMidTransition()
    {
        var theme = ThemeData.Light with { UseMaterial3 = false };
        Rect RectFor(TabIndicatorAnimation animation, double offset)
        {
            using var controller = new TabController(4);
            using var harness = new WidgetRenderHarness(
                Wrap(
                    new TabBar(
                        controller: controller,
                        indicatorAnimation: animation,
                        tabs:
                        [
                            new Tab(text: "A"),
                            new Tab(text: "B"),
                            new Tab(text: "C"),
                            new Tab(text: "D"),
                        ]),
                    theme));
            harness.Pump(new Size(800, 200));
            controller.Offset = offset;
            harness.Pump(new Size(800, 200));
            return RequireIndicatorPainter(harness.RenderView).CurrentRect!.Value;
        }

        Assert.Equal(new Rect(0, 0, 200, 48), RectFor(TabIndicatorAnimation.Linear, 0));
        // Linear moves both edges by the same fraction: 0.2 * 200 = 40.
        Rect linear = RectFor(TabIndicatorAnimation.Linear, 0.2);
        Assert.Equal(40, linear.Left, precision: 3);
        Assert.Equal(240, linear.Right, precision: 3);

        // Elastic accelerates the trailing edge and decelerates the leading edge, so the rect
        // stretches instead of translating.
        Rect elastic = RectFor(TabIndicatorAnimation.Elastic, 0.2);
        Assert.Equal(200 * IndicatorPainter.AccelerateInterpolation(0.2), elastic.Left, precision: 3);
        Assert.Equal(200 + (200 * IndicatorPainter.DecelerateInterpolation(0.2)), elastic.Right, precision: 3);
        Assert.True(elastic.Width > linear.Width);
    }

    [Fact]
    public void TabBar_TabAlignmentValidationAndScrollableDefaults()
    {
        using var controller = new TabController(2);
        Assert.Throws<ArgumentException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            isScrollable: true,
            tabAlignment: TabAlignment.Fill,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")]))));

        Assert.Throws<ArgumentException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabAlignment: TabAlignment.Start,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")]))));

        Assert.Throws<ArgumentException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabAlignment: TabAlignment.StartOffset,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")]))));

        // Scrollable M3 defaults to startOffset, which inserts a 52px leading pad.
        using var scrollableController = new TabController(2);
        using var scrollable = new WidgetRenderHarness(Wrap(new TabBar(
            controller: scrollableController,
            isScrollable: true,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        scrollable.Pump(new Size(800, 200));
        Assert.Contains(
            FindDescendants<RenderPadding>(scrollable.RenderView),
            padding => padding.Padding.Left == 52);

        // Scrollable M2 defaults to start, so there is no leading offset.
        var m2 = ThemeData.Light with { UseMaterial3 = false };
        using var m2Controller = new TabController(2);
        using var m2Harness = new WidgetRenderHarness(
            Wrap(
                new TabBar(
                    controller: m2Controller,
                    isScrollable: true,
                    tabs: [new Tab(text: "One"), new Tab(text: "Two")]),
                m2));
        m2Harness.Pump(new Size(800, 200));
        Assert.DoesNotContain(
            FindDescendants<RenderPadding>(m2Harness.RenderView),
            padding => padding.Padding.Left == 52);
    }

    [Fact]
    public void TabBar_UniformPaddingIsAppliedWhenSomeTabsHaveTextAndIcon()
    {
        using var controller = new TabController(3);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs:
            [
                new Tab(text: "A", icon: new SizedBox(width: 10, height: 10)),
                new Tab(text: "B"),
                new Tab(text: "C"),
            ])));
        harness.Pump(new Size(600, 200));

        IReadOnlyList<Thickness> labelPaddings = FindDescendants<RenderPadding>(harness.RenderView)
            .Select(padding => padding.Padding)
            .Where(insets => insets.Left == 16 && insets.Right == 16)
            .ToList();
        // The text+icon tab keeps kTabLabelPadding; the shorter tabs get (72 - 46) / 2 = 13 added.
        Assert.Contains(new Thickness(16, 0, 16, 0), labelPaddings);
        Assert.Equal(2, labelPaddings.Count(insets => insets.Top == 13 && insets.Bottom == 13));
    }

    [Fact]
    public void TabBar_TapUsesGestureRouteAndAnimatesIndicator()
    {
        using var controller = new TabController(2);
        int tapped = -1;
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            onTap: index => tapped = index,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size(300, 100));

        var flex = FindDescendant<RenderFlex>(harness.RenderView)!;
        Point target = TabRects(flex)[1].Center;
        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            30, PointerDeviceKind.Mouse, target, PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            30, PointerDeviceKind.Mouse, target, PointerButtons.None, now.AddMilliseconds(20)));

        Assert.Equal(1, controller.Index);
        Assert.Equal(1, tapped);
        Assert.True(controller.IndexIsChanging);

        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 100));
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(
            TabRects(FindDescendant<RenderFlex>(harness.RenderView)!)[1].Center.X,
            RequireIndicatorPainter(harness.RenderView).CurrentRect!.Value.Center.X,
            precision: 3);
    }

    [Fact]
    public void TabBar_MouseCursorSplashBorderRadiusAndOverlayResolveThroughTheTheme()
    {
        var theme = ThemeData.Light with
        {
            TabBarTheme = new TabBarThemeData(
                MouseCursor: MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Text),
                SplashBorderRadius: BorderRadius.Circular(20),
                OverlayColor: MaterialStateProperty<Color?>.All(Colors.Orange)),
        };
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(
            Wrap(new TabBar(controller: controller, tabs: [new Tab(text: "One"), new Tab(text: "Two")]), theme));
        harness.Pump(new Size(300, 100));

        InkWell inkWell = FindWidgets<InkWell>(harness.RenderView).First();
        Assert.Equal(SystemMouseCursors.Text, inkWell.MouseCursor);
        Assert.Equal(BorderRadius.Circular(20), inkWell.BorderRadius);
        Assert.Equal(Colors.Orange, inkWell.OverlayColor!.Resolve(MaterialState.Hovered));
        Assert.True(inkWell.EnableFeedback);
    }

    [Fact]
    public void TabBar_DefaultOverlayColorsFollowMaterial3PrimaryAndSecondaryTables()
    {
        using var primaryController = new TabController(2);
        using var primary = new WidgetRenderHarness(Wrap(new TabBar(
            controller: primaryController,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        primary.Pump(new Size(300, 100));

        ColorScheme colors = ThemeData.Light.ColorScheme;
        IReadOnlyList<InkWell> primaryWells = FindWidgets<InkWell>(primary.RenderView);
        Assert.Equal(
            WithOpacity(colors.Primary, 0.08),
            primaryWells[0].OverlayColor!.Resolve(MaterialState.Selected | MaterialState.Hovered));
        Assert.Equal(
            WithOpacity(colors.Primary, 0.1),
            primaryWells[0].OverlayColor!.Resolve(MaterialState.Selected | MaterialState.Pressed));
        Assert.Equal(
            WithOpacity(colors.OnSurface, 0.08),
            primaryWells[1].OverlayColor!.Resolve(MaterialState.Hovered));
        Assert.Null(primaryWells[1].OverlayColor!.Resolve(MaterialState.None));
        // The ink well only reports interaction states, so each tab's own selected state has to be
        // folded in by the default overlay itself.
        Assert.Equal(
            WithOpacity(colors.Primary, 0.08),
            primaryWells[0].OverlayColor!.Resolve(MaterialState.Hovered));

        using var secondaryController = new TabController(2);
        using var secondary = new WidgetRenderHarness(Wrap(TabBar.Secondary(
            controller: secondaryController,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        secondary.Pump(new Size(300, 100));
        IReadOnlyList<InkWell> secondaryWells = FindWidgets<InkWell>(secondary.RenderView);
        Assert.Equal(
            WithOpacity(colors.OnSurface, 0.08),
            secondaryWells[0].OverlayColor!.Resolve(MaterialState.Selected | MaterialState.Hovered));
        Assert.Equal(
            WithOpacity(colors.OnSurface, 0.1),
            secondaryWells[1].OverlayColor!.Resolve(MaterialState.Pressed));
    }

    [Fact]
    public void TabBar_TextScalerOverridesTheAmbientMediaQuery()
    {
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new MediaQuery(
            new MediaQueryData(Size: new Size(300, 100), TextScaleFactor: 3.0),
            new TabBar(
                controller: controller,
                textScaler: TextScaler.Linear(1.75),
                tabs: [new Tab(text: "One"), new Tab(text: "Two")]))));
        harness.Pump(new Size(300, 100));

        double? scale = null;
        _ = FindWidgets<MediaQuery>(harness.RenderView)
            .Select(query => scale = query.Data.TextScaleFactor)
            .ToList();
        Assert.Equal(1.75, scale);
    }

    [Fact]
    public void TabBar_SemanticsExposeTabBarTabAndSelectedState()
    {
        using var controller = new TabController(length: 2, initialIndex: 1);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size(300, 100));

        IReadOnlyList<RenderSemanticsAnnotations> annotations =
            FindDescendants<RenderSemanticsAnnotations>(harness.RenderView);
        Assert.Contains(annotations, node => node.Role == SemanticsRole.TabBar);
        Assert.Equal(2, annotations.Count(node => node.Role == SemanticsRole.Tab));
        Assert.Contains(annotations, node => node.Label == "Tab 1 of 2");
        Assert.Contains(annotations, node => node.Label == "Tab 2 of 2"
                                     && node.Flags.HasFlag(SemanticsFlags.IsSelected));
    }

    [Fact]
    public void TabBar_ZeroTabsAndControllerMismatchBehaveLikeFlutter()
    {
        using var empty = new TabController(0);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(controller: empty, tabs: [])));
        harness.Pump(new Size(800, 200));
        Assert.Equal(48, FindDescendant<RenderConstrainedBox>(harness.RenderView)!.Size.Height, precision: 3);

        using var controller = new TabController(2);
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "Only")]))));

        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children: [new SizedBox()]))));
    }

    [Fact]
    public void TabBar_ZeroAreaDoesNotCrash()
    {
        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size());
        Assert.Equal(new Size(), harness.RenderView.Child!.Size);
    }

    // ------------------------------------------------- TabBarScrollController

    [Fact]
    public void TabBarScrollController_AttachesToTheBarAndCentersTheSelectedTab()
    {
        var scrollController = new TabBarScrollController();
        Assert.Throws<InvalidOperationException>(() => scrollController.DebugCheckHasTabBarState());

        using var controller = new TabController(length: 6, initialIndex: 5);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            scrollController: scrollController,
            isScrollable: true,
            tabs: Enumerable.Range(0, 6).Select(index => (Widget)new Tab(text: $"Tab {index}")).ToArray())));
        harness.Pump(new Size(300, 100));

        Assert.True(scrollController.DebugCheckHasTabBarState());
        Assert.IsType<TabBarScrollPosition>(scrollController.PrimaryPosition);
        // The last tab is selected, so the initial offset is pinned to the end of the strip.
        Assert.Equal(scrollController.PrimaryPosition!.MaxScrollExtent, scrollController.Offset, precision: 3);

        scrollController.Dispose();
        Assert.Throws<InvalidOperationException>(() => scrollController.DebugCheckHasTabBarState());
    }

    // ---------------------------------------------------------- TabBarView

    [Fact]
    public void TabBarView_InitialPageAndViewportFractionUsePageGeometry()
    {
        using var controller = new TabController(3, initialIndex: 1);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            viewportFraction: 0.8,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
            ])));
        harness.Pump(new Size(300, 180));

        var viewport = RequirePageViewport(harness.RenderView);
        Assert.Equal(new Size(240, 180), viewport.FirstChild!.Size);
        Assert.Equal(-210, ((PageViewportParentData)viewport.FirstChild.parentData!).offset.X, precision: 3);
        var selected = viewport.ChildAfter(viewport.FirstChild)!;
        Assert.Equal(30, ((PageViewportParentData)selected.parentData!).offset.X, precision: 3);
    }

    [Fact]
    public void TabBarView_DefaultsAndSemanticRoleMatchFlutter()
    {
        var view = new TabBarView(children: [new SizedBox(), new SizedBox()]);
        Assert.Equal(1.0, view.ViewportFraction);
        Assert.Equal(Clip.HardEdge, view.ClipBehavior);
        Assert.Equal(DragStartBehavior.Start, view.DragStartBehavior);
        Assert.Null(view.Physics);

        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children: [new SizedBox(), new SizedBox()])));
        harness.Pump(new Size(300, 180));
        Assert.Equal(
            2,
            FindDescendants<RenderSemanticsAnnotations>(harness.RenderView)
                .Count(node => node.Role == SemanticsRole.TabPanel));
    }

    [Fact]
    public void TabBarView_ControllerAnimationAndSwipeStaySynchronized()
    {
        using var controller = new TabController(3);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
            ])));
        harness.Pump(new Size(300, 180));

        controller.AnimateTo(2);
        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));
        Assert.Equal(2, controller.Index);
        var programmaticViewport = RequirePageViewport(harness.RenderView);
        Assert.Equal(2, programmaticViewport.Controller.EffectivePage, precision: 3);
        Assert.Equal(0, ((PageViewportParentData)programmaticViewport.LastChild!.parentData!).offset.X, precision: 3);

        controller.AnimateTo(0);
        clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));
        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            44, PointerDeviceKind.Touch, new Point(260, 90), PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            44, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.Primary, true, now.AddMilliseconds(40)));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            44, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.None, now.AddMilliseconds(50)));
        clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));

        Assert.Equal(1, controller.Index);
        var second = RequirePageViewport(harness.RenderView).ChildAfter(
            RequirePageViewport(harness.RenderView).FirstChild!)!;
        Assert.Equal(0, ((PageViewportParentData)second.parentData!).offset.X, precision: 3);
    }

    [Fact]
    public void TabBarView_NonAdjacentWarpLandsOnTheDestinationPage()
    {
        using var controller = new TabController(4);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
                new ColoredBox(Colors.Yellow),
            ])));
        harness.Pump(new Size(300, 180));

        controller.AnimateTo(3);
        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));

        Assert.Equal(3, controller.Index);
        Assert.Equal(3, RequirePageViewport(harness.RenderView).Controller.EffectivePage, precision: 3);
    }

    [Fact]
    public void TabBarView_ZeroDurationControllerJumpsWithoutAnimating()
    {
        using var controller = new TabController(3, animationDuration: TimeSpan.Zero);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
            ])));
        harness.Pump(new Size(300, 180));

        controller.AnimateTo(2);
        harness.Pump(new Size(300, 180));

        Assert.Equal(2, controller.Index);
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(2, RequirePageViewport(harness.RenderView).Controller.EffectivePage, precision: 3);
    }

    [Fact]
    public void PageView_ReportsScrollUpdateAndEndNotificationsForDrags()
    {
        var notifications = new List<ScrollNotification>();
        var controller = new PageController();
        using var harness = new WidgetRenderHarness(Wrap(new NotificationListener<ScrollNotification>(
            onNotification: notification =>
            {
                notifications.Add(notification);
                return false;
            },
            child: new PageView(
                controller: controller,
                children: [new ColoredBox(Colors.Red), new ColoredBox(Colors.Green)]))));
        harness.Pump(new Size(300, 180));

        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            77, PointerDeviceKind.Touch, new Point(260, 90), PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            77, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.Primary, true, now.AddMilliseconds(40)));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            77, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.None, now.AddMilliseconds(50)));

        Assert.Contains(notifications, notification => notification is ScrollUpdateNotification);
        Assert.All(notifications, notification => Assert.Equal(0, notification.Depth));
        Assert.Equal(300, notifications[0].Metrics.ViewportDimension, precision: 3);

        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));

        ScrollEndNotification end = Assert.IsType<ScrollEndNotification>(notifications[^1]);
        Assert.Equal(300, end.Metrics.Pixels, precision: 3);
        controller.Dispose();
    }

    // ------------------------------------------------------ TabPageSelector

    [Fact]
    public void TabPageSelectorIndicator_MatchesCircleGeometryAndBorderStyle()
    {
        var indicator = new TabPageSelectorIndicator(
            backgroundColor: Colors.Red,
            borderColor: Colors.Blue,
            size: 16);
        Assert.Equal(Colors.Red, indicator.BackgroundColor);
        Assert.Equal(Colors.Blue, indicator.BorderColor);
        Assert.Equal(16, indicator.Size);
        Assert.Equal(BorderStyle.Solid, indicator.BorderStyle);

        using var harness = new WidgetRenderHarness(Wrap(indicator));
        harness.Pump(new Size(100, 100));
        var decoration = Assert.Single(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Shape == BoxShape.Circle);
        Assert.Equal(Colors.Red, decoration.Decoration.Color);
        Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(Colors.Blue)),
            decoration.Decoration.Border);
        Assert.Equal(new Size(16, 16), decoration.Size);
        Assert.Equal(new Size(24, 24), harness.RenderView.Child!.Size);

        using var borderless = new WidgetRenderHarness(Wrap(new TabPageSelectorIndicator(
            backgroundColor: Colors.Red,
            borderColor: Colors.Blue,
            size: 12,
            borderStyle: BorderStyle.None)));
        borderless.Pump(new Size(100, 100));
        Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(Colors.Blue, style: BorderStyle.None)),
            Assert.Single(
                FindDescendants<RenderDecoratedBox>(borderless.RenderView),
                box => box.Decoration.Shape == BoxShape.Circle).Decoration.Border);
    }

    [Fact]
    public void TabPageSelector_DefaultsUseControllerLengthAndSchemeSecondary()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);
        var selector = new TabPageSelector(controller: controller);
        Assert.Equal(12, selector.IndicatorSize);
        Assert.Null(selector.Color);
        Assert.Null(selector.SelectedColor);
        Assert.Null(selector.BorderStyle);

        using var harness = new WidgetRenderHarness(Wrap(selector));
        harness.Pump(new Size(200, 60));
        IReadOnlyList<RenderDecoratedBox> circles = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.Shape == BoxShape.Circle)
            .ToList();
        Assert.Equal(3, circles.Count);
        Assert.Equal(
            [Colors.Transparent, ThemeData.Light.ColorScheme.Secondary, Colors.Transparent],
            circles.Select(circle => circle.Decoration.Color!.Value).ToArray());
        Assert.All(circles, circle => Assert.Equal(
            Plumix.Rendering.Border.FromBorderSide(new BorderSide(ThemeData.Light.ColorScheme.Secondary)),
            circle.Decoration.Border));
        Assert.Equal(new Size(60, 20), harness.RenderView.Child!.Size);
    }

    [Fact]
    public void TabPageSelector_RespondsToImmediateIndexAndDragOffsetChanges()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(
            controller: controller,
            color: Colors.Transparent,
            selectedColor: Colors.Red)));
        harness.Pump(new Size(200, 60));

        controller.Index = 2;
        harness.Pump(new Size(200, 60));
        Assert.Equal([0, 0, 255], SelectorAlphas(harness.RenderView));

        controller.Index = 1;
        controller.Offset = 0.4;
        harness.Pump(new Size(200, 60));
        int[] alphas = SelectorAlphas(harness.RenderView);
        Assert.Equal(0, alphas[0]);
        Assert.InRange(alphas[1], 150, 155);
        Assert.InRange(alphas[2], 100, 105);
    }

    [Fact]
    public void TabPageSelector_InterpolatesOutgoingAndIncomingColorsDuringAnimateTo()
    {
        using var controller = new TabController(length: 3);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(
            controller: controller,
            color: Colors.Transparent,
            selectedColor: Colors.Red)));
        harness.Pump(new Size(200, 60));

        controller.AnimateTo(1, duration: TimeSpan.FromMilliseconds(200));
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.04));
        harness.Pump(new Size(200, 60));
        int[] early = SelectorAlphas(harness.RenderView);
        Assert.True(early[0] > early[1]);
        Assert.Equal(0, early[2]);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.18));
        harness.Pump(new Size(200, 60));
        int[] late = SelectorAlphas(harness.RenderView);
        Assert.True(late[0] < late[1]);
        Assert.Equal(0, late[2]);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(200, 60));
        Assert.Equal([0, 255, 0], SelectorAlphas(harness.RenderView));
    }

    [Fact]
    public void TabPageSelector_UsesDefaultControllerAndLocalizedSemantics()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DefaultTabController(
            length: 3,
            initialIndex: 1,
            child: new TabPageSelector())));
        harness.Pump(new Size(200, 60));

        Assert.Contains(
            FindDescendants<RenderSemanticsAnnotations>(harness.RenderView),
            semantics => semantics.Label == "Tab 2 of 3");
        Assert.Equal([0, 255, 0], SelectorAlphas(harness.RenderView));
    }

    [Fact]
    public void TabPageSelector_ValidatesControllerAndHandlesZeroArea()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabPageSelector(indicatorSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabPageSelector(indicatorSize: double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabPageSelector())));

        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(controller: controller)));
        harness.Pump(new Size());
        controller.AnimateTo(1);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size());
        Assert.Equal(new Size(), harness.RenderView.Child!.Size);
    }

    // ------------------------------------------------------------- Sample

    [Fact]
    public void TabsDemoPage_RendersNestedAppBarBottomAndPageViewAtDesktopSize()
    {
        using var harness = new WidgetRenderHarness(Wrap(new TabsDemoPage()));
        harness.Pump(new Size(1000, 700));

        Assert.NotNull(FindDescendant<RenderPageViewport>(harness.RenderView));
        Assert.NotNull(FindIndicatorPainter(harness.RenderView));
        Assert.Equal(4, FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Count(box => box.Decoration.Shape == BoxShape.Circle));
    }

    // ------------------------------------------------------------- helpers

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new Theme(theme ?? ThemeData.Light, child));

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0.0, 1.0)),
        color.R,
        color.G,
        color.B);

    private static IndicatorPainter RequireIndicatorPainter(RenderObject root) =>
        FindIndicatorPainter(root) ?? throw new InvalidOperationException("No indicator painter was built.");

    private static IndicatorPainter? FindIndicatorPainter(RenderObject root) =>
        FindDescendants<RenderCustomPaint>(root)
            .Select(paint => paint.Painter)
            .OfType<IndicatorPainter>()
            .FirstOrDefault();

    private static IReadOnlyList<Rect> TabRects(RenderFlex flex)
    {
        var rects = new List<Rect>();
        for (RenderBox? child = flex.FirstChild; child is not null; child = flex.ChildAfter(child))
        {
            Point offset = ((FlexParentData)child.parentData!).offset;
            rects.Add(new Rect(offset, child.Size));
        }

        return rects;
    }

    private static TextStyle SelectedLabelStyle(RenderObject root) => LabelStyles(root)[0];

    /// <summary>
    /// The resolved label style of each tab, in tab order. Every tab sits under its own
    /// <c>TabStyle</c>; the first one in the tree is the bar-level style wrapper, which is skipped.
    /// </summary>
    private static IReadOnlyList<TextStyle> LabelStyles(RenderObject root)
    {
        var wrappers = new List<Element>();
        CollectElements<TabStyle>(WidgetRenderHarness.RootElementFor(root), wrappers);
        return wrappers
            .Skip(1)
            .Select(wrapper =>
            {
                var styles = new List<DefaultTextStyle>();
                CollectWidgets(wrapper, styles);
                return styles[0].Style;
            })
            .ToList();
    }

    private static void CollectElements<T>(Element? element, List<Element> sink) where T : Widget
    {
        if (element is null)
        {
            return;
        }

        if (element.Widget is T)
        {
            sink.Add(element);
        }

        element.VisitChildren(child => CollectElements<T>(child, sink));
    }

    private static RenderPageViewport RequirePageViewport(RenderObject root) =>
        Assert.IsType<RenderPageViewport>(FindDescendant<RenderPageViewport>(root));

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T match)
        {
            result.Add(match);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    /// <summary>Collects widgets of a given type from the element tree behind a rendered subtree.</summary>
    private static IReadOnlyList<T> FindWidgets<T>(RenderObject root) where T : Widget
    {
        var result = new List<T>();
        CollectWidgets(WidgetRenderHarness.RootElementFor(root), result);
        return result;
    }

    private static void CollectWidgets<T>(Element? element, List<T> sink) where T : Widget
    {
        if (element is null)
        {
            return;
        }

        if (element.Widget is T match)
        {
            sink.Add(match);
        }

        element.VisitChildren(child => CollectWidgets(child, sink));
    }

    private static int[] SelectorAlphas(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .Where(box => box.Decoration.Shape == BoxShape.Circle)
            .Select(box => (int)box.Decoration.Color!.Value.A)
            .ToArray();
    }

    private sealed class ControllerProbe : StatelessWidget
    {
        private readonly Action<TabController> _capture;

        public ControllerProbe(Action<TabController> capture, Key? key = null) : base(key) => _capture = capture;

        public override Widget Build(BuildContext context)
        {
            TabController? controller = DefaultTabController.MaybeOf(context);
            if (controller is not null)
            {
                _capture(controller);
            }

            return new SizedBox();
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private static readonly Dictionary<RenderObject, Element> Roots = [];
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            Roots[RenderView] = _root;
        }

        public RenderView RenderView { get; }

        public static Element? RootElementFor(RenderObject root)
        {
            RenderObject current = root;
            while (true)
            {
                if (Roots.TryGetValue(current, out Element? element))
                {
                    return element;
                }

                if (current.Parent is not RenderObject parent)
                {
                    return null;
                }

                current = parent;
            }
        }

        public void Update(Widget widget)
        {
            _root.Update(widget);
            _owner.FlushBuild();
        }

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
            _ = Roots.Remove(RenderView);
            _root.Unmount();
        }
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
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
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) =>
            _renderView.Child = (RenderBox)child;

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
            }
        }
    }
}
