using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/tab_view.dart

/// <summary>A single tab view with its own navigator state and history.</summary>
public sealed class CupertinoTabView : StatefulWidget
{
    private static readonly IReadOnlyList<NavigatorObserver> DefaultNavigatorObservers =
        Array.Empty<NavigatorObserver>();

    public CupertinoTabView(
        WidgetBuilder? builder = null,
        GlobalKey<NavigatorState>? navigatorKey = null,
        string? defaultTitle = null,
        IReadOnlyDictionary<string, WidgetBuilder>? routes = null,
        RouteFactory? onGenerateRoute = null,
        RouteFactory? onUnknownRoute = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        string? restorationScopeId = null,
        Key? key = null) : base(key)
    {
        Builder = builder;
        NavigatorKey = navigatorKey;
        DefaultTitle = defaultTitle;
        Routes = routes;
        OnGenerateRoute = onGenerateRoute;
        OnUnknownRoute = onUnknownRoute;
        NavigatorObservers = navigatorObservers ?? DefaultNavigatorObservers;
        RestorationScopeId = restorationScopeId;
    }

    public WidgetBuilder? Builder { get; }

    public GlobalKey<NavigatorState>? NavigatorKey { get; }

    public string? DefaultTitle { get; }

    public IReadOnlyDictionary<string, WidgetBuilder>? Routes { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public RouteFactory? OnUnknownRoute { get; }

    public IReadOnlyList<NavigatorObserver> NavigatorObservers { get; }

    public string? RestorationScopeId { get; }

    public override State CreateState() => new CupertinoTabViewState();
}

internal sealed class CupertinoTabViewState : State
{
    private HeroController _heroController = null!;
    private IReadOnlyList<NavigatorObserver> _navigatorObservers = null!;
    private LabeledGlobalKey<NavigatorState>? _ownedNavigatorKey;

    private CupertinoTabView CurrentWidget => (CupertinoTabView)StateWidget;

    private GlobalKey<NavigatorState> NavigatorKey =>
        CurrentWidget.NavigatorKey
        ?? (_ownedNavigatorKey ??= new LabeledGlobalKey<NavigatorState>("CupertinoTabView navigator"));

    private bool IsActive => TickerMode.Of(Context);

    public override void InitState()
    {
        base.InitState();
        _heroController = CupertinoApp.CreateCupertinoHeroController();
        UpdateObservers();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldTabView = (CupertinoTabView)oldWidget;
        if (!Equals(CurrentWidget.NavigatorKey, oldTabView.NavigatorKey)
            || !ReferenceEquals(CurrentWidget.NavigatorObservers, oldTabView.NavigatorObservers))
        {
            UpdateObservers();
        }
    }

    public override void Dispose()
    {
        _heroController.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        GlobalKey<NavigatorState> navigatorKey = NavigatorKey;
        Widget child = new Navigator(
            onGenerateRoute: OnGenerateRoute,
            onUnknownRoute: OnUnknownRoute,
            observers: _navigatorObservers,
            restorationScopeId: CurrentWidget.RestorationScopeId,
            key: navigatorKey);

#pragma warning disable CS0618
        return new NavigatorPopHandler<object?>(
            enabled: IsActive,
            onPop: () => HandlePop(navigatorKey),
            child: child);
#pragma warning restore CS0618
    }

    private void UpdateObservers()
    {
        _navigatorObservers = [.. CurrentWidget.NavigatorObservers, _heroController];
    }

    private void HandlePop(GlobalKey<NavigatorState> navigatorKey)
    {
        if (!IsActive)
        {
            return;
        }

        navigatorKey.CurrentState?.MaybePop();
    }

    private Route? OnGenerateRoute(RouteSettings settings)
    {
        string? name = settings.Name;
        WidgetBuilder? routeBuilder = null;
        string? title = null;
        if (string.Equals(name, Navigator.DefaultRouteName, StringComparison.Ordinal)
            && CurrentWidget.Builder != null)
        {
            routeBuilder = CurrentWidget.Builder;
            title = CurrentWidget.DefaultTitle;
        }
        else if (name != null && CurrentWidget.Routes != null)
        {
            CurrentWidget.Routes.TryGetValue(name, out routeBuilder);
        }

        if (routeBuilder != null)
        {
            return new CupertinoPageRoute<object?>(
                builder: routeBuilder,
                title: title,
                settings: settings);
        }

        return CurrentWidget.OnGenerateRoute?.Invoke(settings);
    }

    private Route? OnUnknownRoute(RouteSettings settings)
    {
        if (CurrentWidget.OnUnknownRoute == null)
        {
            throw new InvalidOperationException(
                $"Could not find a generator for route {settings} in {GetType().Name}. "
                + "Generators are searched in Builder, Routes, OnGenerateRoute, then OnUnknownRoute; "
                + "OnUnknownRoute was not set.");
        }

        Route? result = CurrentWidget.OnUnknownRoute(settings);
        return result ?? throw new InvalidOperationException(
            $"The OnUnknownRoute callback returned null for route {settings} in {GetType().Name}.");
    }
}
