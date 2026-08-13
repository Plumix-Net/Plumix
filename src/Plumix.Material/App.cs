using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/app.dart

public enum ThemeMode
{
    System,
    Light,
    Dark,
}

public sealed class MaterialApp : StatefulWidget
{
    private static readonly IReadOnlyList<Locale> DefaultSupportedLocalesValue =
        [new Locale("en", "US")];

    private static TextStyle ErrorTextStyle { get; } = new(
        FontFamily: new FontFamily("monospace"),
        FontSize: 48.0,
        Color: Avalonia.Media.Color.FromArgb(0xD0, 0xFF, 0x00, 0x00),
        FontWeight: FontWeight.Black);

    public MaterialApp(
        GlobalKey<NavigatorState>? navigatorKey = null,
        GlobalKey<ScaffoldMessengerState>? scaffoldMessengerKey = null,
        Widget? home = null,
        IReadOnlyDictionary<string, WidgetBuilder>? routes = null,
        string? initialRoute = null,
        RouteFactory? onGenerateRoute = null,
        AppInitialRouteListFactory? onGenerateInitialRoutes = null,
        RouteFactory? onUnknownRoute = null,
        Func<NavigationNotification, bool>? onNavigationNotification = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        TransitionBuilder? builder = null,
        string title = "",
        GenerateAppTitle? onGenerateTitle = null,
        Color? color = null,
        ThemeData? theme = null,
        ThemeData? darkTheme = null,
        ThemeData? highContrastTheme = null,
        ThemeData? highContrastDarkTheme = null,
        ThemeMode themeMode = ThemeMode.System,
        TimeSpan? themeAnimationDuration = null,
        Curve? themeAnimationCurve = null,
        Locale? locale = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        LocaleListResolutionCallback? localeListResolutionCallback = null,
        LocaleResolutionCallback? localeResolutionCallback = null,
        IReadOnlyList<Locale>? supportedLocales = null,
        bool debugShowMaterialGrid = false,
        bool showPerformanceOverlay = false,
        bool showSemanticsDebugger = false,
        bool debugShowCheckedModeBanner = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        ScrollBehavior? scrollBehavior = null,
        bool useInheritedMediaQuery = false,
        AnimationStyle? themeAnimationStyle = null,
        Key? key = null) : base(key)
    {
        TimeSpan effectiveThemeAnimationDuration =
            themeAnimationDuration ?? AnimatedTheme.DefaultDuration;
        if (effectiveThemeAnimationDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(themeAnimationDuration));
        }

        IReadOnlyDictionary<string, WidgetBuilder> effectiveRoutes =
            routes ?? new Dictionary<string, WidgetBuilder>();
        IReadOnlyList<NavigatorObserver> effectiveObservers = navigatorObservers ?? [];
        if (home != null && onGenerateInitialRoutes != null)
        {
            throw new ArgumentException(
                "Home and onGenerateInitialRoutes cannot both be specified.",
                nameof(onGenerateInitialRoutes));
        }

        if (home != null && effectiveRoutes.ContainsKey("/"))
        {
            throw new ArgumentException("Home and a '/' route cannot both be specified.", nameof(routes));
        }

        bool hasRoutes = home != null
                         || effectiveRoutes.Count > 0
                         || onGenerateRoute != null
                         || onUnknownRoute != null;
        if (!hasRoutes && builder == null)
        {
            throw new ArgumentException(
                "Either routing properties or builder must provide the application content.");
        }

        if (!hasRoutes
            && (navigatorKey != null || initialRoute != null || effectiveObservers.Count > 0))
        {
            throw new ArgumentException(
                "Navigator properties cannot be used when builder is the only application content.");
        }

        NavigatorKey = navigatorKey;
        ScaffoldMessengerKey = scaffoldMessengerKey;
        Home = home;
        Routes = effectiveRoutes;
        InitialRoute = initialRoute;
        OnGenerateRoute = onGenerateRoute;
        OnGenerateInitialRoutes = onGenerateInitialRoutes;
        OnUnknownRoute = onUnknownRoute;
        OnNavigationNotification = onNavigationNotification;
        NavigatorObservers = effectiveObservers;
        Builder = builder;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        OnGenerateTitle = onGenerateTitle;
        Color = color;
        Theme = theme;
        DarkTheme = darkTheme;
        HighContrastTheme = highContrastTheme;
        HighContrastDarkTheme = highContrastDarkTheme;
        ThemeMode = themeMode;
        ThemeAnimationDuration = effectiveThemeAnimationDuration;
        ThemeAnimationCurve = themeAnimationCurve ?? Curves.Linear;
        Locale = locale;
        LocalizationsDelegates = localizationsDelegates ?? [];
        LocaleListResolutionCallback = localeListResolutionCallback;
        LocaleResolutionCallback = localeResolutionCallback;
        SupportedLocales = supportedLocales ?? DefaultSupportedLocalesValue;
        if (SupportedLocales.Count == 0)
        {
            throw new ArgumentException("Supported locales cannot be empty.", nameof(supportedLocales));
        }

        DebugShowMaterialGrid = debugShowMaterialGrid;
        ShowPerformanceOverlay = showPerformanceOverlay;
        ShowSemanticsDebugger = showSemanticsDebugger;
        DebugShowCheckedModeBanner = debugShowCheckedModeBanner;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
        ScrollBehavior = scrollBehavior;
        UseInheritedMediaQuery = useInheritedMediaQuery;
        ThemeAnimationStyle = themeAnimationStyle;
    }

    public GlobalKey<NavigatorState>? NavigatorKey { get; }

    public GlobalKey<ScaffoldMessengerState>? ScaffoldMessengerKey { get; }

    public Widget? Home { get; }

    public IReadOnlyDictionary<string, WidgetBuilder> Routes { get; }

    public string? InitialRoute { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public AppInitialRouteListFactory? OnGenerateInitialRoutes { get; }

    public RouteFactory? OnUnknownRoute { get; }

    public Func<NavigationNotification, bool>? OnNavigationNotification { get; }

    public IReadOnlyList<NavigatorObserver> NavigatorObservers { get; }

    public TransitionBuilder? Builder { get; }

    public string Title { get; }

    public GenerateAppTitle? OnGenerateTitle { get; }

    public Color? Color { get; }

    public ThemeData? Theme { get; }

    public ThemeData? DarkTheme { get; }

    public ThemeData? HighContrastTheme { get; }

    public ThemeData? HighContrastDarkTheme { get; }

    public ThemeMode ThemeMode { get; }

    public TimeSpan ThemeAnimationDuration { get; }

    public Curve ThemeAnimationCurve { get; }

    public Locale? Locale { get; }

    public IReadOnlyList<LocalizationsDelegate> LocalizationsDelegates { get; }

    public LocaleListResolutionCallback? LocaleListResolutionCallback { get; }

    public LocaleResolutionCallback? LocaleResolutionCallback { get; }

    public IReadOnlyList<Locale> SupportedLocales { get; }

    public bool DebugShowMaterialGrid { get; }

    public bool ShowPerformanceOverlay { get; }

    public bool ShowSemanticsDebugger { get; }

    public bool DebugShowCheckedModeBanner { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent>? Shortcuts { get; }

    public IReadOnlyDictionary<Type, FlutterAction>? Actions { get; }

    public string? RestorationScopeId { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    public bool UseInheritedMediaQuery { get; }

    public AnimationStyle? ThemeAnimationStyle { get; }

    public override State CreateState() => new MaterialAppState();

    private sealed class MaterialAppState : State
    {
        private MaterialApp CurrentWidget => (MaterialApp)StateWidget;

        public override Widget Build(BuildContext context)
        {
            Color materialColor = CurrentWidget.Color
                                  ?? CurrentWidget.Theme?.PrimaryColor
                                  ?? Colors.Blue;
            var delegates = new List<LocalizationsDelegate>(CurrentWidget.LocalizationsDelegates)
            {
                DefaultMaterialLocalizations.Delegate,
                DefaultCupertinoLocalizations.Delegate,
            };

            Widget result = new WidgetsApp(
                navigatorKey: CurrentWidget.NavigatorKey,
                navigatorObservers: CurrentWidget.NavigatorObservers,
                pageRouteBuilder: (settings, builder) => new MaterialPageRoute(
                    builder: builder,
                    settings: settings),
                home: CurrentWidget.Home,
                routes: CurrentWidget.Routes,
                initialRoute: CurrentWidget.InitialRoute,
                onGenerateRoute: CurrentWidget.OnGenerateRoute,
                onGenerateInitialRoutes: CurrentWidget.OnGenerateInitialRoutes,
                onUnknownRoute: CurrentWidget.OnUnknownRoute,
                onNavigationNotification: CurrentWidget.OnNavigationNotification,
                builder: BuildMaterialShell,
                title: CurrentWidget.Title,
                onGenerateTitle: CurrentWidget.OnGenerateTitle,
                textStyle: ErrorTextStyle,
                color: materialColor,
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

            if (CurrentWidget.DebugShowMaterialGrid)
            {
                result = new GridPaper(
                    color: Avalonia.Media.Color.FromArgb(0xE0, 0xF9, 0xBB, 0xE0),
                    interval: 8.0,
                    subdivisions: 1,
                    child: result);
            }

            return new ScrollConfiguration(
                behavior: CurrentWidget.ScrollBehavior ?? new MaterialScrollBehavior(),
                child: result);
        }

        private Widget BuildMaterialShell(BuildContext context, Widget? child)
        {
            ThemeData theme = ResolveTheme(context);
            TextSelectionThemeData selectionTheme = theme.TextSelectionTheme;
            Color effectiveSelectionColor = selectionTheme.SelectionColor
                                            ?? WithOpacity(theme.PrimaryColor, 0.40);
            Color effectiveCursorColor = selectionTheme.CursorColor ?? theme.PrimaryColor;

            Widget childWidget = child ?? new SizedBox();
            if (CurrentWidget.Builder != null)
            {
                childWidget = new Builder(
                    builder: builderContext => CurrentWidget.Builder(builderContext, child));
            }

            childWidget = new ScaffoldMessenger(
                key: CurrentWidget.ScaffoldMessengerKey,
                child: new DefaultSelectionStyle(
                    selectionColor: effectiveSelectionColor,
                    cursorColor: effectiveCursorColor,
                    child: childWidget));

            if (!Equals(CurrentWidget.ThemeAnimationStyle, AnimationStyle.NoAnimation))
            {
                childWidget = new AnimatedTheme(
                    data: theme,
                    duration: CurrentWidget.ThemeAnimationStyle?.Duration
                              ?? CurrentWidget.ThemeAnimationDuration,
                    curve: CurrentWidget.ThemeAnimationStyle?.Curve
                           ?? CurrentWidget.ThemeAnimationCurve,
                    child: childWidget);
            }
            else
            {
                childWidget = new Theme(theme, childWidget);
            }

            return childWidget;
        }

        private ThemeData ResolveTheme(BuildContext context)
        {
            MediaQueryData? mediaQuery = MediaQuery.MaybeOf(context);
            bool useDarkTheme = CurrentWidget.ThemeMode == ThemeMode.Dark
                                || (CurrentWidget.ThemeMode == ThemeMode.System
                                    && mediaQuery?.PlatformBrightness == PlatformBrightness.Dark);
            bool highContrast = mediaQuery?.HighContrast ?? false;
            ThemeData? theme = null;
            if (useDarkTheme && highContrast)
            {
                theme = CurrentWidget.HighContrastDarkTheme;
            }

            if (useDarkTheme)
            {
                theme ??= CurrentWidget.DarkTheme;
            }
            else if (highContrast)
            {
                theme = CurrentWidget.HighContrastTheme;
            }

            theme ??= CurrentWidget.Theme ?? new ThemeData();
            SystemUiIconBrightness iconBrightness = theme.Brightness == Brightness.Dark
                ? SystemUiIconBrightness.Light
                : SystemUiIconBrightness.Dark;
            SystemChrome.SetSystemUiOverlayStyle(
                new SystemUiOverlayStyle(
                    StatusBarIconBrightness: iconBrightness,
                    NavigationBarIconBrightness: iconBrightness));
            return theme;
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            byte alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * byte.MaxValue);
            return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
