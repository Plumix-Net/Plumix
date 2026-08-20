using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/route.dart

/// <summary>Dart's `kCupertinoModalBarrierColor` from `cupertino_ui/lib/src/route.dart`.</summary>
public static class CupertinoRouteConstants
{
    public static CupertinoDynamicColor ModalBarrierColor { get; } = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0x33000000),
        Color.FromUInt32(0x7A000000));
}

/// <summary>A dialog route with iOS-style spring entrance/exit transitions.</summary>
public sealed class CupertinoDialogRoute<T> : RawDialogRoute<T>
{
    private readonly bool _hasCustomTransitionBuilder;

    public CupertinoDialogRoute(
        WidgetBuilder builder,
        BuildContext context,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null,
        TimeSpan? transitionDuration = null,
        RouteTransitionsBuilder? transitionBuilder = null,
        RouteSettings? settings = null,
        bool? requestFocus = null,
        Point? anchorPoint = null) : base(
        pageBuilder: (pageContext, _, _) => builder(pageContext),
        barrierDismissible: barrierDismissible,
        barrierColor: barrierColor ?? CupertinoRouteConstants.ModalBarrierColor.ResolveFrom(context),
        barrierLabel: barrierLabel ?? CupertinoLocalizations.Of(context).ModalBarrierDismissLabel,
        transitionDuration: transitionDuration ?? TimeSpan.FromMilliseconds(250),
        transitionBuilder: transitionBuilder,
        settings: settings,
        requestFocus: requestFocus,
        anchorPoint: anchorPoint)
    {
        _hasCustomTransitionBuilder = transitionBuilder is not null;
    }

    /// <summary>Dart's `_dialogScaleTween`: the dialog enters scaling down from 1.3.</summary>
    private static Animation<double> ScaleTweenOf(Animation<double> animation) =>
        new ScaleTweenAnimation(animation);

    protected internal override Simulation? CreateSimulation(bool forward)
    {
        return new SpringSimulation(
            CupertinoRoutePhysics.StandardSpring,
            Controller.Value,
            forward ? 1.0 : 0.0,
            velocity: 0.0,
            snapToEnd: true,
            tolerance: CupertinoRoutePhysics.StandardTolerance);
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        if (_hasCustomTransitionBuilder)
        {
            return base.BuildTransitions(context, animation, secondaryAnimation, child);
        }

        if (animation.Status == AnimationStatus.Reverse)
        {
            return new FadeTransition(opacity: animation, child: child);
        }

        return new FadeTransition(
            opacity: animation,
            child: new ScaleTransition(scale: ScaleTweenOf(animation), child: child));
    }

    private sealed class ScaleTweenAnimation : Animation<double>
    {
        private readonly Animation<double> _parent;

        public ScaleTweenAnimation(Animation<double> parent)
        {
            _parent = parent;
        }

        public override double Value => 1.3 + ((1.0 - 1.3) * _parent.Value);

        public override AnimationStatus Status => _parent.Status;

        public override void AddListener(Action listener) => _parent.AddListener(listener);

        public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
            _parent.AddStatusListener(listener);
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _parent.RemoveStatusListener(listener);
        }
    }
}

/// <summary>Dart's free function `showCupertinoDialog`.</summary>
public static class CupertinoDialogs
{
    public static Task<T?> ShowCupertinoModalPopup<T>(
        BuildContext context,
        WidgetBuilder builder,
        ImageFilter? filter = null,
        Color? barrierColor = null,
        bool barrierDismissible = true,
        bool useRootNavigator = true,
        bool semanticsDismissible = false,
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null,
        bool? requestFocus = null)
    {
        Color resolvedBarrierColor = barrierColor
                                     ?? CupertinoRouteConstants.ModalBarrierColor.ResolveFrom(context);
        var route = new CupertinoModalPopupRoute<T>(
            builder: builder,
            filter: filter,
            barrierColor: resolvedBarrierColor,
            barrierDismissible: barrierDismissible,
            semanticsDismissible: semanticsDismissible,
            settings: routeSettings,
            anchorPoint: anchorPoint,
            requestFocus: requestFocus);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Completed;
    }

    public static Task<T?> ShowCupertinoDialog<T>(
        BuildContext context,
        WidgetBuilder builder,
        string? barrierLabel = null,
        Color? barrierColor = null,
        bool useRootNavigator = true,
        bool barrierDismissible = false,
        RouteSettings? routeSettings = null,
        Point? anchorPoint = null,
        bool? requestFocus = null)
    {
        var route = new CupertinoDialogRoute<T>(
            builder: builder,
            context: context,
            barrierDismissible: barrierDismissible,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            settings: routeSettings,
            requestFocus: requestFocus,
            anchorPoint: anchorPoint);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Completed;
    }
}
