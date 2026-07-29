using System.Globalization;
using Avalonia.Media;
using Plumix.Foundation;

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

    public Color Color { get; }

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

    private sealed class WidgetsAppState : State
    {
        private readonly GlobalKey<NavigatorState> _navigatorKey =
            new LabeledGlobalKey<NavigatorState>("WidgetsApp navigator");

        private WidgetsApp CurrentWidget => (WidgetsApp)StateWidget;

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
            child = new Actions(
                actions: CurrentWidget.Actions ?? DefaultActions,
                child: child);
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

        private Widget? BuildRouting()
        {
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
                    initialRouteName: CurrentWidget.InitialRoute ?? "/",
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
            if (!@event.IsDown
                || !string.Equals(@event.Key, "Escape", StringComparison.OrdinalIgnoreCase))
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
