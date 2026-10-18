using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/routes.dart

/// <summary>
/// A general dialog route which allows for customization of the dialog popup: the base of both the
/// Material and the Cupertino dialog routes.
/// </summary>
public class RawDialogRoute<T> : PopupRoute
{
    /// <summary>Dart's `RawDialogRoute` default barrier color, `Color(0x80000000)`.</summary>
    public static readonly Color DefaultBarrierColor = new Color(0x80000000);

    private readonly RoutePageBuilder _pageBuilder;
    private readonly bool _barrierDismissible;
    private readonly string? _barrierLabel;
    private readonly Color? _barrierColor;
    private readonly TimeSpan _transitionDuration;
    private readonly RouteTransitionsBuilder? _transitionBuilder;

    public RawDialogRoute(
        RoutePageBuilder pageBuilder,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        TimeSpan? transitionDuration = null,
        RouteTransitionsBuilder? transitionBuilder = null,
        RouteSettings? settings = null,
        bool? requestFocus = null,
        Point? anchorPoint = null,
        TraversalEdgeBehavior? traversalEdgeBehavior = null,
        TraversalEdgeBehavior? directionalTraversalEdgeBehavior = null,
        bool fullscreenDialog = false) : base(settings, requestFocus: requestFocus)
    {
        _pageBuilder = pageBuilder ?? throw new ArgumentNullException(nameof(pageBuilder));
        _barrierDismissible = barrierDismissible;
        _barrierLabel = barrierLabel;
        // Dart's declared default is `Color(0x80000000)`; a caller wanting no scrim passes a
        // transparent color, which the shared barrier pipeline treats like Dart's explicit null.
        _barrierColor = barrierColor ?? DefaultBarrierColor;
        _transitionDuration = transitionDuration ?? TimeSpan.FromMilliseconds(200);
        _transitionBuilder = transitionBuilder;
        AnchorPoint = anchorPoint;
        TraversalEdgeBehavior = traversalEdgeBehavior;
        DirectionalTraversalEdgeBehavior = directionalTraversalEdgeBehavior;
        FullscreenDialog = fullscreenDialog;
    }

    /// <summary>The anchor used to select a sub-screen when a display feature splits the screen.</summary>
    public Point? AnchorPoint { get; }

    public override bool BarrierDismissible => _barrierDismissible;

    public override string? BarrierLabel => _barrierLabel;

    public override Color? BarrierColor => _barrierColor;

    public override TimeSpan TransitionDuration => _transitionDuration;

    public override bool FullscreenDialog { get; }

    private Task<T?>? _typedCompleted;
    private Task<T?>? _typedPopped;

    /// <summary>Completes after the reverse transition and overlay disposal.</summary>
    public new Task<T?> Completed => _typedCompleted ??= ResultAs<T>(base.Completed);
    public new Task<T?> Popped => _typedPopped ??= ResultAs<T>(base.Popped);

    public override Widget BuildPage(BuildContext context)
    {
        return new Semantics(
            scopesRoute: true,
            explicitChildNodes: true,
            child: new DisplayFeatureSubScreen(
                anchorPoint: AnchorPoint,
                child: _pageBuilder(context, Animation, SecondaryAnimation)));
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        if (_transitionBuilder is null)
        {
            return new FadeTransition(opacity: animation, child: child);
        }

        return _transitionBuilder(context, animation, secondaryAnimation, child);
    }

}

/// <summary>Dart's free function `showGeneralDialog`.</summary>
public static class GeneralDialogs
{
    public static Task<T?> ShowGeneralDialog<T>(
        BuildContext context,
        RoutePageBuilder pageBuilder,
        bool barrierDismissible = false,
        string? barrierLabel = null,
        Color? barrierColor = null,
        TimeSpan? transitionDuration = null,
        RouteTransitionsBuilder? transitionBuilder = null,
        bool useRootNavigator = true,
        bool fullscreenDialog = false,
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null,
        bool? requestFocus = null)
    {
        if (barrierDismissible && barrierLabel is null)
        {
            throw new ArgumentException(
                "A dismissible barrier requires a barrierLabel.", nameof(barrierLabel));
        }

        var route = new RawDialogRoute<T>(
            pageBuilder: pageBuilder,
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            transitionDuration: transitionDuration,
            transitionBuilder: transitionBuilder,
            settings: routeSettings,
            requestFocus: requestFocus,
            anchorPoint: anchorPoint,
            fullscreenDialog: fullscreenDialog);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Popped;
    }
}
