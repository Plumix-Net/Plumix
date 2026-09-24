using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: flutter/packages/flutter/test/widgets/widgets_app_tester.dart
// (`TestWidgetsApp`), the minimal WidgetsApp the Dart widget tests mount their subject in.

namespace Plumix.Tests;

/// <summary>Flutter's <c>TestWidgetsApp</c>: a <see cref="WidgetsApp"/> with no page transition.</summary>
internal sealed class TestWidgetsApp : StatelessWidget
{
    public TestWidgetsApp(
        Widget? home = null,
        GlobalKey<NavigatorState>? navigatorKey = null,
        string? initialRoute = null,
        RouteFactory? onGenerateRoute = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        IReadOnlyDictionary<string, WidgetBuilder>? routes = null,
        Color? color = null,
        TextStyle? textStyle = null,
        TransitionBuilder? builder = null,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        string? restorationScopeId = null,
        Key? key = null) : base(key)
    {
        Home = home;
        NavigatorKey = navigatorKey;
        InitialRoute = initialRoute;
        OnGenerateRoute = onGenerateRoute;
        NavigatorObservers = navigatorObservers ?? [];
        Routes = routes ?? new Dictionary<string, WidgetBuilder>();
        Color = color ?? Color.FromUInt32(0xFFFFFFFF);
        TextStyle = textStyle;
        Builder = builder;
        Shortcuts = shortcuts;
        Actions = actions;
        RestorationScopeId = restorationScopeId;
    }

    public Widget? Home { get; }

    public GlobalKey<NavigatorState>? NavigatorKey { get; }

    public string? InitialRoute { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public IReadOnlyList<NavigatorObserver> NavigatorObservers { get; }

    public IReadOnlyDictionary<string, WidgetBuilder> Routes { get; }

    public Color Color { get; }

    public TextStyle? TextStyle { get; }

    public TransitionBuilder? Builder { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent>? Shortcuts { get; }

    public IReadOnlyDictionary<Type, FlutterAction>? Actions { get; }

    public string? RestorationScopeId { get; }

    public override Widget Build(BuildContext context)
    {
        return new WidgetsApp(
            color: Color,
            textStyle: TextStyle,
            navigatorKey: NavigatorKey,
            navigatorObservers: NavigatorObservers,
            home: Home,
            initialRoute: InitialRoute,
            onGenerateRoute: OnGenerateRoute,
            routes: Routes,
            pageRouteBuilder: DefaultPageRouteBuilder,
            builder: Builder,
            shortcuts: Shortcuts,
            actions: Actions,
            restorationScopeId: RestorationScopeId);
    }

    private static PageRoute DefaultPageRouteBuilder(RouteSettings settings, WidgetBuilder builder)
    {
        return new PageRouteBuilder(
            settings: settings,
            pageBuilder: (context, _, _) => builder(context),
            transitionsBuilder: (_, _, _, child) => child);
    }
}
