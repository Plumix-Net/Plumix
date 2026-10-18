// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

namespace Plumix.Widgets;

public sealed partial class Navigator
{
    public static void Replace(BuildContext context, Route oldRoute, Route newRoute) =>
        Of(context).Replace(oldRoute, newRoute);

    public static void ReplaceRouteBelow(BuildContext context, Route anchorRoute, Route newRoute) =>
        Of(context).ReplaceRouteBelow(anchorRoute, newRoute);

    public static string RestorablePushNamed(BuildContext context, string routeName, object? arguments = null) =>
        Of(context).RestorablePushNamed(routeName, arguments);

    public static string RestorablePushReplacementNamed(
        BuildContext context,
        string routeName,
        object? result = null,
        object? arguments = null) => Of(context).RestorablePushReplacementNamed(routeName, arguments, result);

    public static string RestorablePopAndPushNamed(
        BuildContext context,
        string routeName,
        object? result = null,
        object? arguments = null) => Of(context).RestorablePopAndPushNamed(routeName, result, arguments);

    public static string RestorablePushNamedAndRemoveUntil(
        BuildContext context,
        string routeName,
        RoutePredicate predicate,
        object? arguments = null) => Of(context).RestorablePushNamedAndRemoveUntil(routeName, predicate, arguments);

    public static string RestorablePush(
        BuildContext context,
        RestorableRouteBuilder routeBuilder,
        object? arguments = null) => Of(context).RestorablePush(routeBuilder, arguments);

    public static string RestorablePushReplacement(
        BuildContext context,
        RestorableRouteBuilder routeBuilder,
        object? result = null,
        object? arguments = null) => Of(context).RestorablePushReplacement(routeBuilder, arguments, result);

    public static string RestorablePushAndRemoveUntil(
        BuildContext context,
        RestorableRouteBuilder routeBuilder,
        RoutePredicate predicate,
        object? arguments = null) => Of(context).RestorablePushAndRemoveUntil(routeBuilder, predicate, arguments);

    public static string RestorableReplace(
        BuildContext context,
        Route oldRoute,
        RestorableRouteBuilder routeBuilder,
        object? arguments = null) => Of(context).RestorableReplace(oldRoute, routeBuilder, arguments);

    public static string RestorableReplaceRouteBelow(
        BuildContext context,
        Route anchorRoute,
        RestorableRouteBuilder routeBuilder,
        object? arguments = null) => Of(context).RestorableReplaceRouteBelow(anchorRoute, routeBuilder, arguments);
}
