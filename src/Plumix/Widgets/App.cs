using System.Globalization;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using RouterStatics = Plumix.Widgets.Router;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/app.dart

public delegate PageRoute PageRouteFactory(RouteSettings settings, WidgetBuilder builder);
public delegate IReadOnlyList<Route> AppInitialRouteListFactory(string initialRoute);
public delegate string GenerateAppTitle(BuildContext context);

public sealed class WidgetsApp : StatefulWidget
{
    private static readonly IReadOnlyList<Locale> DefaultSupportedLocalesValue =
        [new Locale("en", "US")];

    private static readonly IReadOnlyDictionary<ShortcutActivator, Intent> DefaultShortcutsValue =
        new Dictionary<ShortcutActivator, Intent>();

    private static readonly IReadOnlyDictionary<Type, FlutterAction> DefaultActionsValue =
        new Dictionary<Type, FlutterAction>
        {
            [typeof(DoNothingIntent)] = new DoNothingAction(),
            [typeof(DoNothingAndStopPropagationIntent)] = new DoNothingAction(consumesKey: false),
            [typeof(VoidCallbackIntent)] = new VoidCallbackAction(),
            [typeof(ScrollIntent)] = new ScrollAction(),
        };

    public WidgetsApp(
        Color color,
        GlobalKey<NavigatorState>? navigatorKey = null,
        RouteFactory? onGenerateRoute = null,
        AppInitialRouteListFactory? onGenerateInitialRoutes = null,
        RouteFactory? onUnknownRoute = null,
        Func<NavigationNotification, bool>? onNavigationNotification = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        string? initialRoute = null,
        PageRouteFactory? pageRouteBuilder = null,
        Widget? home = null,
        IReadOnlyDictionary<string, WidgetBuilder>? routes = null,
        TransitionBuilder? builder = null,
        string? title = null,
        GenerateAppTitle? onGenerateTitle = null,
        TextStyle? textStyle = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<Locale>? supportedLocales = null,
        bool showPerformanceOverlay = false,
        bool showSemanticsDebugger = false,
        bool debugShowWidgetInspector = false,
        bool debugShowCheckedModeBanner = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        bool useInheritedMediaQuery = false,
        Key? key = null) : base(key)
    {
        Routes = routes ?? new Dictionary<string, WidgetBuilder>();
        NavigatorObservers = navigatorObservers ?? [];
        SupportedLocales = supportedLocales ?? DefaultSupportedLocalesValue;
        if (SupportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        if (home != null && onGenerateInitialRoutes != null)
        {
            throw new ArgumentException(
                "Home and onGenerateInitialRoutes cannot both be specified.",
                nameof(onGenerateInitialRoutes));
        }

        if (home != null && Routes.ContainsKey("/"))
        {
            throw new ArgumentException("Home and a '/' route cannot both be specified.", nameof(routes));
        }

        bool hasRoutes = home != null
                         || Routes.Count > 0
                         || onGenerateRoute != null
                         || onUnknownRoute != null;
        if (!hasRoutes && builder == null)
        {
            throw new ArgumentException(
                "Either routing properties or builder must provide the application content.");
        }

        if (!hasRoutes
            && (navigatorKey != null || initialRoute != null || NavigatorObservers.Count > 0))
        {
            throw new ArgumentException(
                "Navigator properties cannot be used when builder is the only application content.");
        }

        if (builder == null && onGenerateRoute == null && pageRouteBuilder == null)
        {
            throw new ArgumentException(
                "A pageRouteBuilder is required when default named-route generation is used.");
        }

        Color = color;
        NavigatorKey = navigatorKey;
        OnGenerateRoute = onGenerateRoute;
        OnGenerateInitialRoutes = onGenerateInitialRoutes;
        OnUnknownRoute = onUnknownRoute;
        OnNavigationNotification = onNavigationNotification;
        InitialRoute = initialRoute;
        PageRouteBuilder = pageRouteBuilder;
        Home = home;
        Builder = builder;
        Title = title;
        OnGenerateTitle = onGenerateTitle;
        TextStyle = textStyle;
        Locale = locale;
        LocalizationsDelegates = localizationsDelegates ?? [];
        LocaleListResolutionCallback = localeListResolutionCallback;
        LocaleResolutionCallback = localeResolutionCallback;
        ShowPerformanceOverlay = showPerformanceOverlay;
        ShowSemanticsDebugger = showSemanticsDebugger;
        DebugShowWidgetInspector = debugShowWidgetInspector;
        DebugShowCheckedModeBanner = debugShowCheckedModeBanner;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
        UseInheritedMediaQuery = useInheritedMediaQuery;
    }

    /// <summary>
    /// Dart's <c>WidgetsApp.router</c> named constructor. C# has no named constructors, and generics are
    /// invariant, so the router delegates are captured by a generic factory instead of being stored as
    /// <c>RouterDelegate&lt;object&gt;</c> fields.
    /// </summary>
    public static WidgetsApp Router<T>(
        Color color,
        RouterDelegate<T>? routerDelegate = null,
        RouteInformationParser<T>? routeInformationParser = null,
        RouteInformationProvider? routeInformationProvider = null,
        RouterConfig<T>? routerConfig = null,
        BackButtonDispatcher? backButtonDispatcher = null,
        TransitionBuilder? builder = null,
        string? title = null,
        GenerateAppTitle? onGenerateTitle = null,
        Func<NavigationNotification, bool>? onNavigationNotification = null,
        TextStyle? textStyle = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<Locale>? supportedLocales = null,
        bool showPerformanceOverlay = false,
        bool showSemanticsDebugger = false,
        bool debugShowWidgetInspector = false,
        bool debugShowCheckedModeBanner = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        bool useInheritedMediaQuery = false,
        Key? key = null)
    {
        RouterHost host = CreateRouterHost(
            routerDelegate,
            routeInformationParser,
            routeInformationProvider,
            routerConfig,
            backButtonDispatcher);

        return new WidgetsApp(
            color: color,
            routerHost: host,
            routeInformationProvider: routeInformationProvider,
            backButtonDispatcher: backButtonDispatcher,
            builder: builder,
            title: title,
            onGenerateTitle: onGenerateTitle,
            onNavigationNotification: onNavigationNotification,
            textStyle: textStyle,
            locale: locale,
            localizationsDelegates: localizationsDelegates,
            localeListResolutionCallback: localeListResolutionCallback,
            localeResolutionCallback: localeResolutionCallback,
            supportedLocales: supportedLocales,
            showPerformanceOverlay: showPerformanceOverlay,
            showSemanticsDebugger: showSemanticsDebugger,
            debugShowWidgetInspector: debugShowWidgetInspector,
            debugShowCheckedModeBanner: debugShowCheckedModeBanner,
            shortcuts: shortcuts,
            actions: actions,
            restorationScopeId: restorationScopeId,
            useInheritedMediaQuery: useInheritedMediaQuery,
            key: key);
    }

    /// <summary>
    /// Validates Flutter's <c>WidgetsApp.router</c> assertions and captures the typed router pieces so the
    /// non-generic widget can carry them.
    /// </summary>
    internal static RouterHost CreateRouterHost<T>(
        RouterDelegate<T>? routerDelegate,
        RouteInformationParser<T>? routeInformationParser,
        RouteInformationProvider? routeInformationProvider,
        RouterConfig<T>? routerConfig,
        BackButtonDispatcher? backButtonDispatcher)
    {
        if (routerConfig is not null)
        {
            if (routeInformationProvider is not null
                || routeInformationParser is not null
                || routerDelegate is not null
                || backButtonDispatcher is not null)
            {
                throw new ArgumentException(
                    "If the routerConfig is provided, all the other router delegates must not be provided.",
                    nameof(routerConfig));
            }

            return new RouterHost(
                usesRouterWithDelegates: false,
                hasRouteInformationParser: routerConfig.RouteInformationParser is not null,
                routerDelegate: routerConfig.RouterDelegate,
                routeInformationParser: routerConfig.RouteInformationParser,
                routerConfig: routerConfig,
                build: (_, _) => RouterStatics.WithConfig(routerConfig, restorationScopeId: "router"));
        }

        if (routerDelegate is null)
        {
            throw new ArgumentException(
                "Either one of routerDelegate or routerConfig must be provided.",
                nameof(routerDelegate));
        }

        if (routeInformationProvider is not null && routeInformationParser is null)
        {
            throw new ArgumentException(
                "If routeInformationProvider is provided, routeInformationParser must also be provided.",
                nameof(routeInformationParser));
        }

        return new RouterHost(
            usesRouterWithDelegates: true,
            hasRouteInformationParser: routeInformationParser is not null,
            routerDelegate: routerDelegate,
            routeInformationParser: routeInformationParser,
            routerConfig: null,
            build: (provider, dispatcher) => new Plumix.Widgets.Router<T>(
                routerDelegate: routerDelegate,
                routeInformationProvider: provider,
                routeInformationParser: routeInformationParser,
                backButtonDispatcher: dispatcher,
                restorationScopeId: "router"));
    }

    internal WidgetsApp(
        Color color,
        RouterHost routerHost,
        RouteInformationProvider? routeInformationProvider,
        BackButtonDispatcher? backButtonDispatcher,
        TransitionBuilder? builder,
        string? title,
        GenerateAppTitle? onGenerateTitle,
        Func<NavigationNotification, bool>? onNavigationNotification,
        TextStyle? textStyle,
        Locale? locale,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates,
        LocaleListResolutionCallback? localeListResolutionCallback,
        LocaleResolutionCallback? localeResolutionCallback,
        IReadOnlyList<Locale>? supportedLocales,
        bool showPerformanceOverlay,
        bool showSemanticsDebugger,
        bool debugShowWidgetInspector,
        bool debugShowCheckedModeBanner,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts,
        IReadOnlyDictionary<Type, FlutterAction>? actions,
        string? restorationScopeId,
        bool useInheritedMediaQuery,
        Key? key) : base(key)
    {
        Routes = new Dictionary<string, WidgetBuilder>();
        NavigatorObservers = [];
        SupportedLocales = supportedLocales ?? DefaultSupportedLocalesValue;
        if (SupportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        Color = color;
        RouterHostConfiguration = routerHost;
        RouteInformationProvider = routeInformationProvider;
        BackButtonDispatcher = backButtonDispatcher;
        Builder = builder;
        Title = title;
        OnGenerateTitle = onGenerateTitle;
        OnNavigationNotification = onNavigationNotification;
        TextStyle = textStyle;
        Locale = locale;
        LocalizationsDelegates = localizationsDelegates ?? [];
        LocaleListResolutionCallback = localeListResolutionCallback;
        LocaleResolutionCallback = localeResolutionCallback;
        ShowPerformanceOverlay = showPerformanceOverlay;
        ShowSemanticsDebugger = showSemanticsDebugger;
        DebugShowWidgetInspector = debugShowWidgetInspector;
        DebugShowCheckedModeBanner = debugShowCheckedModeBanner;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
        UseInheritedMediaQuery = useInheritedMediaQuery;
    }

    public Color Color { get; }

    /// <summary>The route-information provider forwarded to the <c>Router</c>, or null outside router mode.</summary>
    public RouteInformationProvider? RouteInformationProvider { get; }

    /// <summary>The back-button dispatcher forwarded to the <c>Router</c>, or null outside router mode.</summary>
    public BackButtonDispatcher? BackButtonDispatcher { get; }

    /// <summary>The router delegate this app was configured with, or null outside router mode.</summary>
    public object? RouterDelegate => RouterHostConfiguration?.RouterDelegate;

    /// <summary>The route-information parser this app was configured with, or null when there is none.</summary>
    public object? RouteInformationParser => RouterHostConfiguration?.RouteInformationParser;

    /// <summary>The router config this app was configured with, or null when delegates were passed directly.</summary>
    public object? RouterConfig => RouterHostConfiguration?.RouterConfig;

    internal RouterHost? RouterHostConfiguration { get; }

    public GlobalKey<NavigatorState>? NavigatorKey { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public AppInitialRouteListFactory? OnGenerateInitialRoutes { get; }

    public RouteFactory? OnUnknownRoute { get; }

    public Func<NavigationNotification, bool>? OnNavigationNotification { get; }

    public IReadOnlyList<NavigatorObserver> NavigatorObservers { get; }

    public string? InitialRoute { get; }

    public PageRouteFactory? PageRouteBuilder { get; }

    public Widget? Home { get; }

    public IReadOnlyDictionary<string, WidgetBuilder> Routes { get; }

    public TransitionBuilder? Builder { get; }

    public string? Title { get; }

    public GenerateAppTitle? OnGenerateTitle { get; }

    public TextStyle? TextStyle { get; }

    public Locale? Locale { get; }

    public IReadOnlyList<LocalizationsDelegate> LocalizationsDelegates { get; }

    public LocaleListResolutionCallback? LocaleListResolutionCallback { get; }

    public LocaleResolutionCallback? LocaleResolutionCallback { get; }

    public IReadOnlyList<Locale> SupportedLocales { get; }

    public bool ShowPerformanceOverlay { get; }

    public bool ShowSemanticsDebugger { get; }

    public bool DebugShowWidgetInspector { get; }

    public bool DebugShowCheckedModeBanner { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent>? Shortcuts { get; }

    public IReadOnlyDictionary<Type, FlutterAction>? Actions { get; }

    public string? RestorationScopeId { get; }

    public bool UseInheritedMediaQuery { get; }

    public static IReadOnlyDictionary<ShortcutActivator, Intent> DefaultShortcuts =>
        DefaultShortcutsValue;

    public static IReadOnlyDictionary<Type, FlutterAction> DefaultActions => DefaultActionsValue;

    public override State CreateState() => new WidgetsAppState();

    private sealed class WidgetsAppState : State, WidgetsBindingObserver
    {
        private readonly GlobalKey<NavigatorState> _navigatorKey =
            new LabeledGlobalKey<NavigatorState>("WidgetsApp navigator");

        private PlatformRouteInformationProvider? _defaultRouteInformationProvider;
        private RootBackButtonDispatcher? _defaultBackButtonDispatcher;

        private WidgetsApp CurrentWidget => (WidgetsApp)StateWidget;

        private RouterHost? RouterHost => CurrentWidget.RouterHostConfiguration;

        private bool UsesRouterWithDelegates => RouterHost?.UsesRouterWithDelegates ?? false;

        private RouteInformationProvider? EffectiveRouteInformationProvider =>
            CurrentWidget.RouteInformationProvider ?? _defaultRouteInformationProvider;

        private BackButtonDispatcher? EffectiveBackButtonDispatcher =>
            CurrentWidget.BackButtonDispatcher ?? _defaultBackButtonDispatcher;

        private string InitialRouteName =>
            !string.Equals(SystemNavigator.DefaultRouteName, Navigator.DefaultRouteName, StringComparison.Ordinal)
                ? SystemNavigator.DefaultRouteName
                : CurrentWidget.InitialRoute ?? SystemNavigator.DefaultRouteName;

        public override void InitState()
        {
            base.InitState();
            WidgetsBinding.Instance.AddObserver(this);
            UpdateRouting();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            UpdateRouting();
        }

        public override void Dispose()
        {
            _ = WidgetsBinding.Instance.RemoveObserver(this);
            _defaultRouteInformationProvider?.Dispose();
            _defaultRouteInformationProvider = null;
            base.Dispose();
        }

        public Task<bool> DidPopRoute()
        {
            // In router mode the back-button dispatcher owns the pop; outside it, Plumix keeps routing the
            // host back button through the navigator handler stack so the innermost navigator still wins.
            return Task.FromResult(false);
        }

        public Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
        {
            ArgumentNullException.ThrowIfNull(routeInformation);
            if (UsesRouterWithDelegates || !UsesNavigator)
            {
                return Task.FromResult(false);
            }

            NavigatorState? navigator = (CurrentWidget.NavigatorKey ?? _navigatorKey).CurrentState;
            if (navigator is null)
            {
                return Task.FromResult(false);
            }

            navigator.PushNamed(LocationOf(routeInformation.Uri));
            return Task.FromResult(true);
        }

        public override Widget Build(BuildContext context)
        {
            Widget? routing = BuildRouting();
            Widget result;
            if (CurrentWidget.Builder != null)
            {
                result = new Builder(
                    builder: builderContext => CurrentWidget.Builder(builderContext, routing));
            }
            else
            {
                result = routing
                         ?? throw new InvalidOperationException(
                             "WidgetsApp requires routing when builder is not specified.");
            }

            if (CurrentWidget.TextStyle != null)
            {
                result = new DefaultTextStyle(CurrentWidget.TextStyle, result);
            }

            if (CurrentWidget.DebugShowCheckedModeBanner)
            {
                result = new CheckedModeBanner(result);
            }

            result = Overlay.Wrap(result);

            result = new Focus(
                canRequestFocus: false,
                onKeyEvent: HandleRootKeyEvent,
                child: result);

            Widget titledResult = CurrentWidget.OnGenerateTitle != null
                ? new Builder(titleContext => new Title(
                    title: CurrentWidget.OnGenerateTitle(titleContext),
                    color: Opaque(CurrentWidget.Color),
                    child: result))
                : new Title(
                    title: CurrentWidget.Title ?? string.Empty,
                    color: Opaque(CurrentWidget.Color),
                    child: result);

            Locale locale = ResolveLocale();
            var delegates = new List<LocalizationsDelegate>(CurrentWidget.LocalizationsDelegates)
            {
                DefaultWidgetsLocalizations.Delegate,
            };

            Widget child = new Localizations(
                locale: locale,
                delegates: delegates,
                isApplicationLevel: true,
                child: titledResult);
            child = new ShortcutRegistrar(child);
            child = new TapRegionSurface(child);
            child = new FocusScope(child);
            child = new FocusTraversalGroup(
                policy: new ReadingOrderTraversalPolicy(),
                child: child);
            child = new Actions(
                actions: CurrentWidget.Actions ?? DefaultActions,
                child: child);
            // Nested inside the app shortcuts so an unmatched key falls through to them.
            child = new DefaultTextEditingShortcuts(child);
            child = new Shortcuts(
                shortcuts: CurrentWidget.Shortcuts ?? DefaultShortcuts,
                debugLabel: "<Default WidgetsApp Shortcuts>",
                child: child);
            child = new NotificationListener<NavigationNotification>(
                onNotification: CurrentWidget.OnNavigationNotification ?? DefaultNavigationNotification,
                child: child);
            child = new SharedAppData(child);
            return new RootRestorationScope(
                restorationId: CurrentWidget.RestorationScopeId,
                child: child);
        }

        private void UpdateRouting()
        {
            if (UsesRouterWithDelegates)
            {
                if (CurrentWidget.RouteInformationProvider is null && RouterHost!.HasRouteInformationParser)
                {
                    _defaultRouteInformationProvider ??= new PlatformRouteInformationProvider(
                        new RouteInformation(new Uri(InitialRouteName, UriKind.RelativeOrAbsolute)));
                }
                else
                {
                    _defaultRouteInformationProvider?.Dispose();
                    _defaultRouteInformationProvider = null;
                }

                if (CurrentWidget.BackButtonDispatcher is null)
                {
                    _defaultBackButtonDispatcher ??= new RootBackButtonDispatcher();
                }

                return;
            }

            _defaultRouteInformationProvider?.Dispose();
            _defaultRouteInformationProvider = null;
            _defaultBackButtonDispatcher = null;
        }

        /// <summary>Flutter's <c>RouteInformation.location</c>: path (or "/"), query and fragment only.</summary>
        private static string LocationOf(Uri uri)
        {
            string path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
            string query = string.Empty;
            string fragment = string.Empty;
            if (uri.IsAbsoluteUri)
            {
                query = uri.Query;
                fragment = uri.Fragment;
            }
            else
            {
                int hashIndex = path.IndexOf('#', StringComparison.Ordinal);
                if (hashIndex >= 0)
                {
                    fragment = path[hashIndex..];
                    path = path[..hashIndex];
                }

                int questionIndex = path.IndexOf('?', StringComparison.Ordinal);
                if (questionIndex >= 0)
                {
                    query = path[questionIndex..];
                    path = path[..questionIndex];
                }
            }

            if (path.Length == 0)
            {
                path = Navigator.DefaultRouteName;
            }

            return Uri.UnescapeDataString(path + query + fragment);
        }

        private Widget? BuildRouting()
        {
            if (RouterHost is not null)
            {
                return RouterHost.Build(EffectiveRouteInformationProvider, EffectiveBackButtonDispatcher);
            }

            if (!UsesNavigator)
            {
                return null;
            }

            NavigatorInitialRouteListFactory? initialRouteFactory =
                CurrentWidget.OnGenerateInitialRoutes == null
                    ? null
                    : (_, routeName) => CurrentWidget.OnGenerateInitialRoutes(routeName);
            return new FocusScope(
                autofocus: true,
                child: new Navigator(
                    onGenerateRoute: GenerateRoute,
                    initialRouteName: InitialRouteName,
                    observers: CurrentWidget.NavigatorObservers,
                    key: CurrentWidget.NavigatorKey ?? _navigatorKey,
                    onGenerateInitialRoutes: initialRouteFactory,
                    onUnknownRoute: CurrentWidget.OnUnknownRoute));
        }

        private bool UsesNavigator =>
            CurrentWidget.Home != null
            || CurrentWidget.Routes.Count > 0
            || CurrentWidget.OnGenerateRoute != null
            || CurrentWidget.OnUnknownRoute != null;

        private Route? GenerateRoute(RouteSettings settings)
        {
            string? routeName = settings.Name;
            WidgetBuilder? builder = null;
            if (string.Equals(routeName, "/", StringComparison.Ordinal) && CurrentWidget.Home != null)
            {
                builder = _ => CurrentWidget.Home;
            }
            else if (routeName != null)
            {
                CurrentWidget.Routes.TryGetValue(routeName, out builder);
            }

            if (builder != null)
            {
                PageRouteFactory pageRouteBuilder = CurrentWidget.PageRouteBuilder
                                                    ?? throw new InvalidOperationException(
                                                        "WidgetsApp requires pageRouteBuilder for home/routes.");
                return pageRouteBuilder(settings, builder);
            }

            return CurrentWidget.OnGenerateRoute?.Invoke(settings);
        }

        private Locale ResolveLocale()
        {
            IReadOnlyList<Locale> preferredLocales = CurrentWidget.Locale != null
                ? [CurrentWidget.Locale]
                : [Locale.FromCultureInfo(CultureInfo.CurrentUICulture)];
            return Localizations.Resolve(
                preferredLocales,
                CurrentWidget.SupportedLocales,
                CurrentWidget.LocaleListResolutionCallback,
                CurrentWidget.LocaleResolutionCallback);
        }

        private static KeyEventResult HandleRootKeyEvent(FocusNode node, UI.KeyEvent @event)
        {
            if (@event is not UI.KeyDownEvent
                || !@event.LogicalKey.Equals(UI.LogicalKeyboardKey.Escape))
            {
                return KeyEventResult.Ignored;
            }

            return RawTooltip.DismissAllToolTips()
                ? KeyEventResult.Handled
                : KeyEventResult.Ignored;
        }

        private static bool DefaultNavigationNotification(NavigationNotification notification) => true;

        private static Color Opaque(Color color)
        {
            return Color.FromArgb(byte.MaxValue, color.R, color.G, color.B);
        }
    }
}

/// <summary>
/// The router configuration a <see cref="WidgetsApp"/> was created with. Dart stores the delegates as
/// <c>Object</c>-typed fields on the widget; C# generics are invariant, so the typed pieces are captured
/// in <see cref="Build"/> and only identity-level references are kept for inspection.
/// </summary>
internal sealed class RouterHost
{
    public RouterHost(
        bool usesRouterWithDelegates,
        bool hasRouteInformationParser,
        object? routerDelegate,
        object? routeInformationParser,
        object? routerConfig,
        Func<RouteInformationProvider?, BackButtonDispatcher?, Widget> build)
    {
        UsesRouterWithDelegates = usesRouterWithDelegates;
        HasRouteInformationParser = hasRouteInformationParser;
        RouterDelegate = routerDelegate;
        RouteInformationParser = routeInformationParser;
        RouterConfig = routerConfig;
        Build = build;
    }

    public bool UsesRouterWithDelegates { get; }

    public bool HasRouteInformationParser { get; }

    public object? RouterDelegate { get; }

    public object? RouteInformationParser { get; }

    public object? RouterConfig { get; }

    public Func<RouteInformationProvider?, BackButtonDispatcher?, Widget> Build { get; }
}
