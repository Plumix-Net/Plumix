using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/app.dart
// flutter/packages/flutter/lib/src/widgets/localizations.dart
// material_ui/lib/src/app.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class ApplicationWidgetsTests : IDisposable
{
    public ApplicationWidgetsTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    [Fact]
    public void Localizations_ResolvesLocaleByFlutterPriority_AndUsesFirstDelegatePerResourceType()
    {
        var supportedLocales = new[]
        {
            new Locale("en", "US"),
            new Locale("fr", "CA"),
            new Locale("fr", "FR"),
        };

        Assert.Equal(
            new Locale("fr", "CA"),
            Localizations.BasicLocaleListResolution(
                [new Locale("fr", "CA")],
                supportedLocales));
        Assert.Equal(
            new Locale("fr", "CA"),
            Localizations.BasicLocaleListResolution(
                [new Locale("fr", "BE")],
                supportedLocales));
        Assert.Equal(
            new Locale("en", "US"),
            Localizations.Resolve(
                [new Locale("fr", "CA")],
                supportedLocales,
                localeListResolutionCallback: (_, locales) => locales[0]));
        Assert.Equal(
            new Locale("en", "US"),
            Localizations.BasicLocaleListResolution(
                [new Locale("zz"), new Locale("fr", "BE"), new Locale("en", "US")],
                supportedLocales));

        string? value = null;
        TextDirection? direction = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Localizations(
            locale: new Locale("ar"),
            delegates:
            [
                new UnsupportedTestStringsDelegate(),
                new TestStringsDelegate("first"),
                new TestStringsDelegate("second"),
                DefaultWidgetsLocalizations.Delegate,
            ],
            child: new Builder(context =>
            {
                value = Localizations.Of<TestStrings>(context).Value;
                direction = Directionality.Of(context);
                return new SizedBox();
            })));

        MountAndFlush(root, owner);

        Assert.Equal("first", value);
        Assert.Equal(TextDirection.Ltr, direction);
        root.Unmount();
    }

    [Fact]
    public void Localizations_ReloadsOnlyForLocaleTypeOrDelegatePolicyChanges()
    {
        string? value = null;
        var owner = new BuildOwner();

        Localizations BuildLocalizations(string resourceValue, bool shouldReload)
        {
            return new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    new ReloadingTestStringsDelegate(resourceValue, shouldReload),
                    DefaultWidgetsLocalizations.Delegate,
                ],
                child: new Builder(context =>
                {
                    value = Localizations.Of<TestStrings>(context).Value;
                    return new SizedBox();
                }));
        }

        var root = new TestRootElement(BuildLocalizations("first", shouldReload: false));
        MountAndFlush(root, owner);
        Assert.Equal("first", value);

        root.Update(BuildLocalizations("ignored", shouldReload: false));
        owner.FlushBuild();
        Assert.Equal("first", value);

        root.Update(BuildLocalizations("reloaded", shouldReload: true));
        owner.FlushBuild();
        Assert.Equal("reloaded", value);
        root.Unmount();
    }

    /// <remarks>
    /// Dart parity source: <c>WidgetsApp.defaultShortcuts</c> / <c>WidgetsApp.defaultActions</c>
    /// — the maps that make Tab, the arrow keys, Enter/Space and Escape work in every app.
    /// </remarks>
    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Windows)]
    [InlineData(TargetPlatform.Fuchsia)]
    public void WidgetsApp_DefaultShortcuts_MatchFlutterOnNonAppleDesktopAndMobile(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            AssertNonAppleDefaults(WidgetsApp.DefaultShortcuts);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    private static void AssertNonAppleDefaults(IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts)
    {

        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Enter)]);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.NumpadEnter)]);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Space)]);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.GameButtonA)]);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Select)]);
        Assert.IsType<DismissIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Escape)]);
        Assert.IsType<NextFocusIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Tab)]);
        Assert.IsType<PreviousFocusIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.Tab, shift: true)]);

        var left = Assert.IsType<DirectionalFocusIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowLeft)]);
        Assert.Equal(TraversalDirection.Left, left.Direction);
        var up = Assert.IsType<DirectionalFocusIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowUp)]);
        Assert.Equal(TraversalDirection.Up, up.Direction);

        var controlUp = Assert.IsType<ScrollIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowUp, control: true)]);
        Assert.Equal(AxisDirection.Up, controlUp.Direction);
        Assert.Equal(ScrollIncrementType.Line, controlUp.Type);
        var pageDown = Assert.IsType<ScrollIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.PageDown)]);
        Assert.Equal(AxisDirection.Down, pageDown.Direction);
        Assert.Equal(ScrollIncrementType.Page, pageDown.Type);
    }

    /// <remarks>
    /// Dart parity source: <c>WidgetsApp._defaultAppleOsShortcuts</c> — Apple platforms scroll with
    /// meta rather than control, and drop the game-button/select activators.
    /// </remarks>
    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void WidgetsApp_DefaultShortcuts_UseTheAppleMapOnAppleOperatingSystems(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            AssertAppleDefaults(WidgetsApp.DefaultShortcuts);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    private static void AssertAppleDefaults(IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts)
    {
        Assert.DoesNotContain(new SingleActivator(LogicalKeyboardKey.GameButtonA), shortcuts.Keys);
        Assert.DoesNotContain(new SingleActivator(LogicalKeyboardKey.Select), shortcuts.Keys);
        Assert.DoesNotContain(
            new SingleActivator(LogicalKeyboardKey.ArrowUp, control: true),
            shortcuts.Keys);

        var metaUp = Assert.IsType<ScrollIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowUp, meta: true)]);
        Assert.Equal(AxisDirection.Up, metaUp.Direction);
        Assert.IsType<NextFocusIntent>(shortcuts[new SingleActivator(LogicalKeyboardKey.Tab)]);
        Assert.IsType<DirectionalFocusIntent>(
            shortcuts[new SingleActivator(LogicalKeyboardKey.ArrowRight)]);
    }

    /// <remarks>Dart parity source: <c>WidgetsApp.defaultActions</c>.</remarks>
    [Fact]
    public void WidgetsApp_DefaultActions_CoverEveryDefaultIntent()
    {
        IReadOnlyDictionary<Type, FlutterAction> actions = WidgetsApp.DefaultActions;

        Assert.IsType<DoNothingAction>(actions[typeof(DoNothingIntent)]);
        Assert.IsType<DoNothingAction>(actions[typeof(DoNothingAndStopPropagationIntent)]);
        Assert.IsType<RequestFocusAction>(actions[typeof(RequestFocusIntent)]);
        Assert.IsType<NextFocusAction>(actions[typeof(NextFocusIntent)]);
        Assert.IsType<PreviousFocusAction>(actions[typeof(PreviousFocusIntent)]);
        Assert.IsType<DirectionalFocusAction>(actions[typeof(DirectionalFocusIntent)]);
        Assert.IsType<ScrollAction>(actions[typeof(ScrollIntent)]);
        Assert.IsType<PrioritizedAction>(actions[typeof(PrioritizedIntents)]);
        Assert.IsType<VoidCallbackAction>(actions[typeof(VoidCallbackIntent)]);
    }

    [Fact]
    public void WidgetsApp_ExposesFlutterDefaultsAndValidatesRoutingContracts()
    {
        var app = new WidgetsApp(
            color: Colors.Blue,
            home: new SizedBox(),
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings));

        Assert.Equal("/", app.InitialRoute ?? "/");
        Assert.Single(app.SupportedLocales);
        Assert.Equal(new Locale("en", "US"), app.SupportedLocales[0]);
        Assert.True(app.DebugShowCheckedModeBanner);
        Assert.False(app.ShowPerformanceOverlay);
        Assert.False(app.ShowSemanticsDebugger);
        Assert.Empty(app.NavigatorObservers);
        Assert.Contains(typeof(VoidCallbackIntent), WidgetsApp.DefaultActions.Keys);

        Assert.Throws<ArgumentException>(() => new WidgetsApp(
            color: Colors.Blue,
            home: new SizedBox(),
            routes: new Dictionary<string, WidgetBuilder> { ["/"] = _ => new SizedBox() },
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings)));
        Assert.Throws<ArgumentException>(() => new WidgetsApp(
            color: Colors.Blue,
            supportedLocales: [],
            builder: (_, _) => new SizedBox()));
        Assert.Throws<ArgumentException>(() => new WidgetsApp(color: Colors.Blue));
    }

    [Fact]
    public void WidgetsApp_ComposesRoutingBuilderLocalizationTitleAndApplicationInfrastructure()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("app navigator");
        BuildContext? homeContext = null;
        Widget? builderChild = null;
        string? generatedTitleLocale = null;
        var app = new WidgetsApp(
            color: Color.FromArgb(0x80, 0x11, 0x22, 0x33),
            navigatorKey: navigatorKey,
            home: new Builder(context =>
            {
                homeContext = context;
                return new SizedBox(width: 1, height: 1);
            }),
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings),
            builder: (context, child) =>
            {
                builderChild = child;
                return child!;
            },
            locale: new Locale("ar"),
            supportedLocales: [new Locale("ar")],
            localizationsDelegates: [new TestStringsDelegate("localized")],
            onGenerateTitle: context =>
            {
                generatedTitleLocale = Localizations.LocaleOf(context).LanguageCode;
                return Localizations.Of<TestStrings>(context).Value;
            },
            restorationScopeId: "app");
        var owner = new BuildOwner();
        var root = new TestRootElement(app);

        MountAndFlush(root, owner);

        Assert.NotNull(builderChild);
        Assert.True(homeContext.HasValue);
        Assert.Same(navigatorKey.CurrentState, Navigator.Of(homeContext!.Value));
        Assert.Same(navigatorKey.CurrentState!.Overlay, Overlay.Of(homeContext.Value, rootOverlay: true));
        Assert.Equal(TextDirection.Ltr, Directionality.Of(homeContext.Value));
        Assert.Equal("localized", Localizations.Of<TestStrings>(homeContext.Value).Value);
        Assert.NotNull(SharedAppData.GetValue(homeContext.Value, "key", () => new object()));
        Assert.NotNull(ShortcutRegistry.MaybeOf(homeContext.Value));
        Assert.NotNull(TapRegion.MaybeOf(homeContext.Value));
        Assert.NotNull(homeContext.Value.DependOnInherited<UnmanagedRestorationScope>());
        Assert.Equal("ar", generatedTitleLocale);
        Assert.Equal(
            new ApplicationSwitcherDescription("localized", 0xFF112233),
            SystemChrome.CurrentApplicationSwitcherDescription);
        root.Unmount();
    }

    [Fact]
    public void WidgetsApp_UsesDeepLinkInitialRoutesAndUnknownRouteFallback()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("deep link navigator");
        var observer = new RecordingNavigatorObserver();
        Route? UnknownRoute(RouteSettings settings)
        {
            return new BuilderPageRoute(
                _ => new SizedBox(),
                new RouteSettings(Name: "unknown:" + settings.Name));
        }

        var owner = new BuildOwner();
        var root = new TestRootElement(new WidgetsApp(
            color: Colors.Blue,
            navigatorKey: navigatorKey,
            routes: new Dictionary<string, WidgetBuilder>
            {
                ["/"] = _ => new SizedBox(),
                ["/a"] = _ => new SizedBox(),
                ["/a/b"] = _ => new SizedBox(),
            },
            initialRoute: "/a/b",
            onUnknownRoute: UnknownRoute,
            navigatorObservers: [observer],
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings)));

        MountAndFlush(root, owner);

        Assert.Equal(["/", "/a", "/a/b"], observer.PushedNames);
        Assert.True(navigatorKey.CurrentState!.CanPop);
        navigatorKey.CurrentState.PushNamed("/missing");
        owner.FlushBuild();
        Assert.Equal("unknown:/missing", navigatorKey.CurrentState.CurrentRoute!.Settings.Name);
        root.Unmount();
    }

    [Fact]
    public void WidgetsApp_CustomInitialRoutesOverrideDeepLinks_AndMissingTargetsFallBackToRoot()
    {
        var customObserver = new RecordingNavigatorObserver();
        var customOwner = new BuildOwner();
        var customRoot = new TestRootElement(new WidgetsApp(
            color: Colors.Blue,
            routes: new Dictionary<string, WidgetBuilder> { ["/"] = _ => new SizedBox() },
            initialRoute: "/ignored/deep-link",
            onGenerateInitialRoutes: _ =>
            [
                new BuilderPageRoute(
                    _ => new SizedBox(),
                    new RouteSettings(Name: "first")),
                new BuilderPageRoute(
                    _ => new SizedBox(),
                    new RouteSettings(Name: "second")),
            ],
            navigatorObservers: [customObserver],
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings)));

        MountAndFlush(customRoot, customOwner);

        Assert.Equal(["first", "second"], customObserver.PushedNames);
        customRoot.Unmount();

        int unknownCalls = 0;
        var fallbackObserver = new RecordingNavigatorObserver();
        var fallbackOwner = new BuildOwner();
        var fallbackRoot = new TestRootElement(new WidgetsApp(
            color: Colors.Blue,
            routes: new Dictionary<string, WidgetBuilder> { ["/"] = _ => new SizedBox() },
            initialRoute: "/missing",
            onUnknownRoute: settings =>
            {
                unknownCalls += 1;
                return new BuilderPageRoute(_ => new SizedBox(), settings);
            },
            navigatorObservers: [fallbackObserver],
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings)));

        MountAndFlush(fallbackRoot, fallbackOwner);

        Assert.Equal(["/"], fallbackObserver.PushedNames);
        Assert.Equal(0, unknownCalls);
        fallbackRoot.Unmount();
    }

    [Fact]
    public void MaterialApp_ExposesFlutterDefaultsAndUsesMaterialPageRoutes()
    {
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("material navigator");
        var app = new MaterialApp(
            navigatorKey: navigatorKey,
            theme: new ThemeData(platform: TargetPlatform.Linux),
            home: new SizedBox(),
            debugShowCheckedModeBanner: false);
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(),
            child: app));

        Assert.Equal(ThemeMode.System, app.ThemeMode);
        Assert.Equal(AnimatedTheme.DefaultDuration, app.ThemeAnimationDuration);
        Assert.Equal(Curves.Linear(0.37), app.ThemeAnimationCurve(0.37));
        Assert.Single(app.SupportedLocales);
        Assert.False(app.DebugShowMaterialGrid);

        MountAndFlush(root, owner);

        Assert.IsType<MaterialPageRoute>(navigatorKey.CurrentState!.CurrentRoute);
        root.Unmount();
    }

    [Fact]
    public void MaterialApp_ComposesThemeSelectionMessengerSelectionLocalizationAndScrollBehavior()
    {
        var lightTheme = new ThemeData(
            brightness: Brightness.Light,
            primaryColor: Color.FromRgb(10, 20, 30));
        var darkTheme = new ThemeData(
            brightness: Brightness.Dark,
            primaryColor: Color.FromRgb(40, 50, 60));
        ThemeData? resolvedTheme = null;
        DefaultSelectionStyle? selectionStyle = null;
        ScaffoldMessengerState? messenger = null;
        MaterialLocalizations? materialLocalizations = null;
        CupertinoLocalizations? cupertinoLocalizations = null;
        ScrollBehavior? scrollBehavior = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            child: new MaterialApp(
                theme: lightTheme,
                darkTheme: darkTheme,
                themeAnimationStyle: AnimationStyle.NoAnimation,
                debugShowCheckedModeBanner: false,
                home: new Builder(context =>
                {
                    resolvedTheme = Theme.Of(context);
                    selectionStyle = DefaultSelectionStyle.Of(context);
                    messenger = ScaffoldMessenger.Of(context);
                    materialLocalizations = MaterialLocalizations.Of(context);
                    cupertinoLocalizations = CupertinoLocalizations.Of(context);
                    scrollBehavior = ScrollConfiguration.Of(context);
                    return new SizedBox();
                }))));

        MountAndFlush(root, owner);

        Assert.Equal(ThemeData.Localize(darkTheme, darkTheme.Typography.EnglishLike), resolvedTheme);
        // Dart's `_MaterialAppState._materialBuilder` resolves both defaults from the color scheme.
        Color darkPrimary = darkTheme.ColorScheme.Primary;
        Assert.Equal(
            Color.FromArgb(102, darkPrimary.R, darkPrimary.G, darkPrimary.B),
            selectionStyle!.SelectionColor);
        Assert.Equal(darkPrimary, selectionStyle.CursorColor);
        Assert.NotNull(messenger);
        Assert.Same(DefaultMaterialLocalizations.Instance, materialLocalizations);
        Assert.Same(DefaultCupertinoLocalizations.Instance, cupertinoLocalizations);
        Assert.IsType<MaterialScrollBehavior>(scrollBehavior);
        Assert.Equal(
            SystemUiIconBrightness.Light,
            SystemChrome.CurrentSystemUiOverlayStyle.StatusBarIconBrightness);
        root.Unmount();
    }

    [Fact]
    public void MaterialApp_ThemePrecedenceMatchesDarkAndHighContrastPolicy()
    {
        var lightTheme = new ThemeData(primaryColor: Color.FromRgb(1, 1, 1));
        var darkTheme = new ThemeData(
            brightness: Brightness.Dark,
            primaryColor: Color.FromRgb(2, 2, 2));
        var highContrastTheme = new ThemeData(primaryColor: Color.FromRgb(3, 3, 3));
        var highContrastDarkTheme = new ThemeData(
            brightness: Brightness.Dark,
            primaryColor: Color.FromRgb(4, 4, 4));
        ThemeData? resolvedTheme = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(
                PlatformBrightness: PlatformBrightness.Dark,
                HighContrast: true),
            child: new MaterialApp(
                theme: lightTheme,
                darkTheme: darkTheme,
                highContrastTheme: highContrastTheme,
                highContrastDarkTheme: highContrastDarkTheme,
                themeAnimationStyle: AnimationStyle.NoAnimation,
                debugShowCheckedModeBanner: false,
                home: new Builder(context =>
                {
                    resolvedTheme = Theme.Of(context);
                    return new SizedBox();
                }))));

        MountAndFlush(root, owner);

        Assert.Equal(
            ThemeData.Localize(highContrastDarkTheme, highContrastDarkTheme.Typography.EnglishLike),
            resolvedTheme);
        root.Unmount();
    }

    private static void MountAndFlush(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed record TestStrings(string Value);

    private sealed class TestStringsDelegate : LocalizationsDelegate<TestStrings>
    {
        private readonly string _value;

        public TestStringsDelegate(string value)
        {
            _value = value;
        }

        public override bool IsSupported(Locale locale) => true;

        public override TestStrings LoadTyped(Locale locale) => new(_value);

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class UnsupportedTestStringsDelegate : LocalizationsDelegate<TestStrings>
    {
        public override bool IsSupported(Locale locale) => false;

        public override TestStrings LoadTyped(Locale locale)
        {
            throw new InvalidOperationException("Unsupported delegates must not be loaded.");
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class ReloadingTestStringsDelegate : LocalizationsDelegate<TestStrings>
    {
        private readonly string _value;
        private readonly bool _shouldReload;

        public ReloadingTestStringsDelegate(string value, bool shouldReload)
        {
            _value = value;
            _shouldReload = shouldReload;
        }

        public override bool IsSupported(Locale locale) => true;

        public override TestStrings LoadTyped(Locale locale) => new(_value);

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => _shouldReload;
    }

    private sealed class RecordingNavigatorObserver : NavigatorObserver
    {
        public List<string?> PushedNames { get; } = [];

        public override void DidPush(Route route, Route? previousRoute)
        {
            PushedNames.Add(route.Settings.Name);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
    }
}
