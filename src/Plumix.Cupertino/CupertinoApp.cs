using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/app.dart

/// <summary>An application shell with Cupertino routing, theming, localization, and scrolling defaults.</summary>
public sealed class CupertinoApp : StatefulWidget
{
    private static readonly IReadOnlyList<Locale> DefaultSupportedLocalesValue =
        [new Locale("en", "US")];

    public CupertinoApp(
        GlobalKey<NavigatorState>? navigatorKey = null,
        Widget? home = null,
        CupertinoThemeData? theme = null,
        IReadOnlyDictionary<string, WidgetBuilder>? routes = null,
        string? initialRoute = null,
        RouteFactory? onGenerateRoute = null,
        AppInitialRouteListFactory? onGenerateInitialRoutes = null,
        RouteFactory? onUnknownRoute = null,
        Func<NavigationNotification, bool>? onNavigationNotification = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        TransitionBuilder? builder = null,
        string? title = null,
        GenerateAppTitle? onGenerateTitle = null,
        CupertinoDynamicColor? color = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<Locale>? supportedLocales = null,
        bool showPerformanceOverlay = false,
        bool checkerboardRasterCacheImages = false,
        bool checkerboardOffscreenLayers = false,
        bool showSemanticsDebugger = false,
        bool debugShowCheckedModeBanner = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        ScrollBehavior? scrollBehavior = null,
        bool useInheritedMediaQuery = false,
        Key? key = null) : base(key)
    {
        IReadOnlyDictionary<string, WidgetBuilder> effectiveRoutes =
            routes ?? new Dictionary<string, WidgetBuilder>();
        IReadOnlyList<NavigatorObserver> effectiveObservers = navigatorObservers ?? [];
        ValidateNavigatorConfiguration(
            navigatorKey,
            home,
            effectiveRoutes,
            initialRoute,
            onGenerateRoute,
            onGenerateInitialRoutes,
            onUnknownRoute,
            effectiveObservers,
            builder);

        NavigatorKey = navigatorKey;
        Home = home;
        Theme = theme;
        Routes = effectiveRoutes;
        InitialRoute = initialRoute;
        OnGenerateRoute = onGenerateRoute;
        OnGenerateInitialRoutes = onGenerateInitialRoutes;
        OnUnknownRoute = onUnknownRoute;
        OnNavigationNotification = onNavigationNotification;
        NavigatorObservers = effectiveObservers;
        Builder = builder;
        Title = title;
        OnGenerateTitle = onGenerateTitle;
        Color = color;
        Locale = locale;
        LocalizationsDelegates = localizationsDelegates ?? [];
        LocaleListResolutionCallback = localeListResolutionCallback;
        LocaleResolutionCallback = localeResolutionCallback;
        SupportedLocales = supportedLocales ?? DefaultSupportedLocalesValue;
        if (SupportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        ShowPerformanceOverlay = showPerformanceOverlay;
        CheckerboardRasterCacheImages = checkerboardRasterCacheImages;
        CheckerboardOffscreenLayers = checkerboardOffscreenLayers;
        ShowSemanticsDebugger = showSemanticsDebugger;
        DebugShowCheckedModeBanner = debugShowCheckedModeBanner;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
        ScrollBehavior = scrollBehavior;
        UseInheritedMediaQuery = useInheritedMediaQuery;
    }

    /// <summary>Dart's <c>CupertinoApp.router</c> named constructor.</summary>
    public static CupertinoApp Router<T>(
        RouterDelegate<T>? routerDelegate = null,
        RouteInformationParser<T>? routeInformationParser = null,
        RouteInformationProvider? routeInformationProvider = null,
        RouterConfig<T>? routerConfig = null,
        BackButtonDispatcher? backButtonDispatcher = null,
        CupertinoThemeData? theme = null,
        TransitionBuilder? builder = null,
        string? title = null,
        GenerateAppTitle? onGenerateTitle = null,
        Func<NavigationNotification, bool>? onNavigationNotification = null,
        CupertinoDynamicColor? color = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<Locale>? supportedLocales = null,
        bool showPerformanceOverlay = false,
        bool checkerboardRasterCacheImages = false,
        bool checkerboardOffscreenLayers = false,
        bool showSemanticsDebugger = false,
        bool debugShowCheckedModeBanner = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        ScrollBehavior? scrollBehavior = null,
        bool useInheritedMediaQuery = false,
        Key? key = null)
    {
        if (routerDelegate is null && routerConfig is null)
        {
            throw new ArgumentException(
                "Either one of routerDelegate or routerConfig must be provided.",
                nameof(routerDelegate));
        }

        RouterHost host = WidgetsApp.CreateRouterHost(
            routerDelegate,
            routeInformationParser,
            routeInformationProvider,
            routerConfig,
            backButtonDispatcher);
        return new CupertinoApp(
            routerHost: host,
            routeInformationProvider: routeInformationProvider,
            backButtonDispatcher: backButtonDispatcher,
            theme: theme,
            builder: builder,
            title: title,
            onGenerateTitle: onGenerateTitle,
            onNavigationNotification: onNavigationNotification,
            color: color,
            locale: locale,
            localizationsDelegates: localizationsDelegates,
            localeListResolutionCallback: localeListResolutionCallback,
            localeResolutionCallback: localeResolutionCallback,
            supportedLocales: supportedLocales,
            showPerformanceOverlay: showPerformanceOverlay,
            checkerboardRasterCacheImages: checkerboardRasterCacheImages,
            checkerboardOffscreenLayers: checkerboardOffscreenLayers,
            showSemanticsDebugger: showSemanticsDebugger,
            debugShowCheckedModeBanner: debugShowCheckedModeBanner,
            shortcuts: shortcuts,
            actions: actions,
            restorationScopeId: restorationScopeId,
            scrollBehavior: scrollBehavior,
            useInheritedMediaQuery: useInheritedMediaQuery,
            key: key);
    }

    private CupertinoApp(
        RouterHost routerHost,
        RouteInformationProvider? routeInformationProvider,
        BackButtonDispatcher? backButtonDispatcher,
        CupertinoThemeData? theme,
        TransitionBuilder? builder,
        string? title,
        GenerateAppTitle? onGenerateTitle,
        Func<NavigationNotification, bool>? onNavigationNotification,
        CupertinoDynamicColor? color,
        Locale? locale,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates,
        LocaleListResolutionCallback? localeListResolutionCallback,
        LocaleResolutionCallback? localeResolutionCallback,
        IReadOnlyList<Locale>? supportedLocales,
        bool showPerformanceOverlay,
        bool checkerboardRasterCacheImages,
        bool checkerboardOffscreenLayers,
        bool showSemanticsDebugger,
        bool debugShowCheckedModeBanner,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts,
        IReadOnlyDictionary<Type, FlutterAction>? actions,
        string? restorationScopeId,
        ScrollBehavior? scrollBehavior,
        bool useInheritedMediaQuery,
        Key? key) : base(key)
    {
        RouterHostConfiguration = routerHost;
        RouteInformationProvider = routeInformationProvider;
        BackButtonDispatcher = backButtonDispatcher;
        Routes = new Dictionary<string, WidgetBuilder>();
        NavigatorObservers = [];
        Theme = theme;
        Builder = builder;
        Title = title;
        OnGenerateTitle = onGenerateTitle;
        OnNavigationNotification = onNavigationNotification;
        Color = color;
        Locale = locale;
        LocalizationsDelegates = localizationsDelegates ?? [];
        LocaleListResolutionCallback = localeListResolutionCallback;
        LocaleResolutionCallback = localeResolutionCallback;
        SupportedLocales = supportedLocales ?? DefaultSupportedLocalesValue;
        if (SupportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        ShowPerformanceOverlay = showPerformanceOverlay;
        CheckerboardRasterCacheImages = checkerboardRasterCacheImages;
        CheckerboardOffscreenLayers = checkerboardOffscreenLayers;
        ShowSemanticsDebugger = showSemanticsDebugger;
        DebugShowCheckedModeBanner = debugShowCheckedModeBanner;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
        ScrollBehavior = scrollBehavior;
        UseInheritedMediaQuery = useInheritedMediaQuery;
    }

    public GlobalKey<NavigatorState>? NavigatorKey { get; }
    public Widget? Home { get; }
    public CupertinoThemeData? Theme { get; }
    public IReadOnlyDictionary<string, WidgetBuilder> Routes { get; }
    public string? InitialRoute { get; }
    public RouteFactory? OnGenerateRoute { get; }
    public AppInitialRouteListFactory? OnGenerateInitialRoutes { get; }
    public RouteFactory? OnUnknownRoute { get; }
    public Func<NavigationNotification, bool>? OnNavigationNotification { get; }
    public IReadOnlyList<NavigatorObserver> NavigatorObservers { get; }
    public RouteInformationProvider? RouteInformationProvider { get; }
    public object? RouteInformationParser => RouterHostConfiguration?.RouteInformationParser;
    public object? RouterDelegate => RouterHostConfiguration?.RouterDelegate;
    public BackButtonDispatcher? BackButtonDispatcher { get; }
    public object? RouterConfig => RouterHostConfiguration?.RouterConfig;
    internal RouterHost? RouterHostConfiguration { get; }
    public TransitionBuilder? Builder { get; }
    public string? Title { get; }
    public GenerateAppTitle? OnGenerateTitle { get; }
    public CupertinoDynamicColor? Color { get; }
    public Locale? Locale { get; }
    public IReadOnlyList<LocalizationsDelegate> LocalizationsDelegates { get; }
    public LocaleListResolutionCallback? LocaleListResolutionCallback { get; }
    public LocaleResolutionCallback? LocaleResolutionCallback { get; }
    public IReadOnlyList<Locale> SupportedLocales { get; }
    public bool ShowPerformanceOverlay { get; }
    public bool CheckerboardRasterCacheImages { get; }
    public bool CheckerboardOffscreenLayers { get; }
    public bool ShowSemanticsDebugger { get; }
    public bool DebugShowCheckedModeBanner { get; }
    public IReadOnlyDictionary<ShortcutActivator, Intent>? Shortcuts { get; }
    public IReadOnlyDictionary<Type, FlutterAction>? Actions { get; }
    public string? RestorationScopeId { get; }
    public ScrollBehavior? ScrollBehavior { get; }
    public bool UseInheritedMediaQuery { get; }

    public static HeroController CreateCupertinoHeroController() => new();

    public override State CreateState() => new CupertinoAppState();

    private static void ValidateNavigatorConfiguration(
        GlobalKey<NavigatorState>? navigatorKey,
        Widget? home,
        IReadOnlyDictionary<string, WidgetBuilder> routes,
        string? initialRoute,
        RouteFactory? onGenerateRoute,
        AppInitialRouteListFactory? onGenerateInitialRoutes,
        RouteFactory? onUnknownRoute,
        IReadOnlyList<NavigatorObserver> navigatorObservers,
        TransitionBuilder? builder)
    {
        if (home != null && onGenerateInitialRoutes != null)
        {
            throw new ArgumentException(
                "Home and onGenerateInitialRoutes cannot both be specified.",
                nameof(onGenerateInitialRoutes));
        }

        if (home != null && routes.ContainsKey(Navigator.DefaultRouteName))
        {
            throw new ArgumentException("Home and a '/' route cannot both be specified.", nameof(routes));
        }

        bool hasRoutes = home != null
                         || routes.Count > 0
                         || onGenerateRoute != null
                         || onUnknownRoute != null;
        if (!hasRoutes && builder == null)
        {
            throw new ArgumentException(
                "Either routing properties or builder must provide the application content.");
        }

        if (!hasRoutes && (navigatorKey != null || initialRoute != null || navigatorObservers.Count > 0))
        {
            throw new ArgumentException(
                "Navigator properties cannot be used when builder is the only application content.");
        }
    }

    private sealed class CupertinoAppState : State
    {
        private HeroController _heroController = null!;

        private CupertinoApp CurrentWidget => (CupertinoApp)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _heroController = CreateCupertinoHeroController();
        }

        public override void Dispose()
        {
            _heroController.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            CupertinoThemeData effectiveTheme =
                (CurrentWidget.Theme ?? new CupertinoThemeData()).ResolveFrom(context);
            PlatformBrightness brightness = effectiveTheme.Brightness
                                            ?? MediaQuery.PlatformBrightnessOf(context);
            SystemChrome.SetSystemUiOverlayStyle(
                brightness == PlatformBrightness.Dark
                    ? SystemUiOverlayStyle.Light
                    : SystemUiOverlayStyle.Dark);

            var delegates = new List<LocalizationsDelegate>(CurrentWidget.LocalizationsDelegates)
            {
                DefaultCupertinoLocalizations.Delegate,
            };

            Color primaryColor = effectiveTheme.PrimaryColor.Value;
            return new ScrollConfiguration(
                behavior: CurrentWidget.ScrollBehavior ?? new CupertinoScrollBehavior(),
                child: new HeroControllerScope(
                    controller: _heroController,
                    child: new CupertinoUserInterfaceLevel(
                    data: CupertinoUserInterfaceLevelData.Base,
                    child: new CupertinoTheme(
                        data: effectiveTheme,
                        child: new DefaultSelectionStyle(
                            selectionColor: WithOpacity(primaryColor, 0.2),
                            cursorColor: primaryColor,
                            child: new Builder(
                                builder: appContext => BuildWidgetsApp(
                                    appContext,
                                    effectiveTheme,
                                    delegates)))))));
        }

        private Widget BuildWidgetsApp(
            BuildContext context,
            CupertinoThemeData effectiveTheme,
            IReadOnlyList<LocalizationsDelegate> delegates)
        {
            CupertinoDynamicColor appColor = CurrentWidget.Color ?? effectiveTheme.PrimaryColor;
            Color color = appColor.ResolveFrom(context).Value;
            if (CurrentWidget.RouterHostConfiguration is not null)
            {
                return new WidgetsApp(
                    color: color,
                    routerHost: CurrentWidget.RouterHostConfiguration,
                    routeInformationProvider: CurrentWidget.RouteInformationProvider,
                    backButtonDispatcher: CurrentWidget.BackButtonDispatcher,
                    builder: CurrentWidget.Builder,
                    title: CurrentWidget.Title,
                    onGenerateTitle: CurrentWidget.OnGenerateTitle,
                    onNavigationNotification: CurrentWidget.OnNavigationNotification,
                    textStyle: effectiveTheme.TextTheme.TextStyle,
                    locale: CurrentWidget.Locale,
                    localizationsDelegates: delegates,
                    localeListResolutionCallback: CurrentWidget.LocaleListResolutionCallback,
                    localeResolutionCallback: CurrentWidget.LocaleResolutionCallback,
                    supportedLocales: CurrentWidget.SupportedLocales,
                    showPerformanceOverlay: CurrentWidget.ShowPerformanceOverlay,
                    showSemanticsDebugger: CurrentWidget.ShowSemanticsDebugger,
                    debugShowWidgetInspector: false,
                    debugShowCheckedModeBanner: CurrentWidget.DebugShowCheckedModeBanner,
                    shortcuts: CurrentWidget.Shortcuts,
                    actions: CurrentWidget.Actions,
                    restorationScopeId: CurrentWidget.RestorationScopeId,
                    useInheritedMediaQuery: CurrentWidget.UseInheritedMediaQuery,
                    key: null);
            }

            return new WidgetsApp(
                color: color,
                navigatorKey: CurrentWidget.NavigatorKey,
                navigatorObservers: CurrentWidget.NavigatorObservers,
                pageRouteBuilder: (settings, builder) => new CupertinoPageRoute<object?>(
                    builder: builder,
                    settings: settings),
                home: CurrentWidget.Home,
                routes: CurrentWidget.Routes,
                initialRoute: CurrentWidget.InitialRoute,
                onGenerateRoute: CurrentWidget.OnGenerateRoute,
                onGenerateInitialRoutes: CurrentWidget.OnGenerateInitialRoutes,
                onUnknownRoute: CurrentWidget.OnUnknownRoute,
                onNavigationNotification: CurrentWidget.OnNavigationNotification,
                builder: CurrentWidget.Builder,
                title: CurrentWidget.Title,
                onGenerateTitle: CurrentWidget.OnGenerateTitle,
                textStyle: effectiveTheme.TextTheme.TextStyle,
                locale: CurrentWidget.Locale,
                localizationsDelegates: delegates,
                localeListResolutionCallback: CurrentWidget.LocaleListResolutionCallback,
                localeResolutionCallback: CurrentWidget.LocaleResolutionCallback,
                supportedLocales: CurrentWidget.SupportedLocales,
                showPerformanceOverlay: CurrentWidget.ShowPerformanceOverlay,
                showSemanticsDebugger: CurrentWidget.ShowSemanticsDebugger,
                debugShowCheckedModeBanner: CurrentWidget.DebugShowCheckedModeBanner,
                shortcuts: CurrentWidget.Shortcuts,
                actions: CurrentWidget.Actions,
                restorationScopeId: CurrentWidget.RestorationScopeId,
                useInheritedMediaQuery: CurrentWidget.UseInheritedMediaQuery);
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            byte alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * byte.MaxValue);
            return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}

/// <summary>Cupertino scrolling defaults: bouncing physics, no glow, and desktop scrollbars.</summary>
public sealed class CupertinoScrollBehavior : ScrollBehavior
{
    private static readonly ScrollPhysics DefaultPhysics =
        new BouncingScrollPhysics(parent: new RangeMaintainingScrollPhysics());

    private static readonly ScrollPhysics DesktopPhysics =
        new BouncingScrollPhysics(
            decelerationRate: ScrollDecelerationRate.Fast,
            parent: new RangeMaintainingScrollPhysics());

    public override Widget BuildScrollbar(BuildContext context, Widget child, ScrollableDetails details)
    {
        return GetPlatform(context) switch
        {
            TargetPlatform.Linux or TargetPlatform.MacOS or TargetPlatform.Windows =>
                new CupertinoScrollbar(
                    controller: details.Controller
                                ?? throw new InvalidOperationException(
                                    "A desktop Cupertino scrollbar requires a ScrollController."),
                    child: child),
            _ => child,
        };
    }

    public override Widget BuildOverscrollIndicator(BuildContext context, Widget child, ScrollableDetails details)
    {
        return child;
    }

    public override ScrollPhysics GetScrollPhysics(BuildContext context)
    {
        return GetPlatform(context) == TargetPlatform.MacOS ? DesktopPhysics : DefaultPhysics;
    }

    public override MultitouchDragStrategy GetMultitouchDragStrategy(BuildContext context)
    {
        return MultitouchDragStrategy.AverageBoundaryPointers;
    }
}
