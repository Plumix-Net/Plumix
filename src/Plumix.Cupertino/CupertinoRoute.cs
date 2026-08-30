using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/route.dart

internal interface ICupertinoRouteTransition
{
    string? Title { get; }

    IValueListenable<string?> PreviousTitle { get; }
}

internal static class CupertinoRoutePhysics
{
    public static SpringDescription StandardSpring { get; } = new(
        mass: 1.0,
        stiffness: 522.35,
        damping: 45.7099552);

    public static Tolerance StandardTolerance { get; } = new(velocity: 0.03);
}

/// <summary>Shared iOS page-route behavior corresponding to Dart's route transition mixin.</summary>
public abstract class CupertinoRouteTransitionMixin<T> : PageRoute, ICupertinoRouteTransition
{
    private static readonly Color PageTransitionBarrierColor = Color.FromUInt32(0x18000000);
    private readonly bool _allowSnapshotting;
    private readonly bool _barrierDismissible;
    private ValueNotifier<string?>? _previousTitle;

    protected CupertinoRouteTransitionMixin(
        RouteSettings? settings = null,
        bool fullscreenDialog = false,
        bool maintainState = true,
        bool allowSnapshotting = true,
        bool barrierDismissible = false,
        bool? requestFocus = null) : base(
        settings,
        fullscreenDialog,
        maintainState,
        requestFocus: requestFocus)
    {
        _allowSnapshotting = allowSnapshotting;
        _barrierDismissible = barrierDismissible;
    }

    /// <summary>The title used by Cupertino navigation bars.</summary>
    public abstract string? Title { get; }

    /// <summary>The live title of the preceding Cupertino route.</summary>
    public IValueListenable<string?> PreviousTitle => _previousTitle
        ?? throw new InvalidOperationException(
            "Cannot read the previous title for a route that has not yet been installed.");

    public static TimeSpan CupertinoTransitionDuration { get; } = TimeSpan.FromMilliseconds(500);

    public override TimeSpan TransitionDuration => CupertinoTransitionDuration;

    public override Color? BarrierColor => FullscreenDialog ? null : PageTransitionBarrierColor;

    public override string? BarrierLabel => null;

    public override bool BarrierDismissible => _barrierDismissible;

    public override bool AllowSnapshotting => _allowSnapshotting;

    protected abstract Widget BuildContent(BuildContext context);

    public override bool CanTransitionTo(TransitionRoute nextRoute)
    {
        bool nextRouteIsNotFullscreen = nextRoute is not PageRoute pageRoute || !pageRoute.FullscreenDialog;
        bool nextRouteHasDelegatedTransition = nextRoute is ModalRoute modalRoute
                                                && modalRoute.DelegatedTransition is not null;
        return nextRouteIsNotFullscreen
               && (nextRoute is ICupertinoRouteTransition || nextRouteHasDelegatedTransition);
    }

    public override bool CanTransitionFrom(TransitionRoute previousRoute)
    {
        return previousRoute is PageRoute && !FullscreenDialog;
    }

    public override Widget BuildPage(BuildContext context)
    {
        return new Semantics(
            scopesRoute: true,
            explicitChildNodes: true,
            child: BuildContent(context));
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return BuildPageTransitions(this, context, animation, secondaryAnimation, child);
    }

    public static Widget BuildPageTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        bool linearTransition = route.PopGestureInProgress;
        if (route.FullscreenDialog)
        {
            return new CupertinoFullscreenDialogTransition(
                primaryRouteAnimation: animation,
                secondaryRouteAnimation: secondaryAnimation,
                linearTransition: linearTransition,
                child: child);
        }

        return new CupertinoPageTransition(
            primaryRouteAnimation: animation,
            secondaryRouteAnimation: secondaryAnimation,
            linearTransition: linearTransition,
            child: new CupertinoBackGestureDetector(route, child));
    }

    public override void DidChangePrevious(Route? previousRoute)
    {
        string? previousTitle = (previousRoute as ICupertinoRouteTransition)?.Title;
        if (_previousTitle is null)
        {
            _previousTitle = new ValueNotifier<string?>(previousTitle);
        }
        else
        {
            _previousTitle.Value = previousTitle;
        }

        base.DidChangePrevious(previousRoute);
    }

    public override void Dispose()
    {
        _previousTitle?.Dispose();
        base.Dispose();
    }
}

/// <summary>A page route with the native iOS push, pop, and leading-edge swipe transitions.</summary>
public sealed class CupertinoPageRoute<T> : CupertinoRouteTransitionMixin<T>
{
    private readonly WidgetBuilder _builder;
    private readonly TaskCompletionSource<T?> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CupertinoPageRoute(
        WidgetBuilder builder,
        string? title = null,
        RouteSettings? settings = null,
        bool? requestFocus = null,
        bool maintainState = true,
        bool fullscreenDialog = false,
        bool allowSnapshotting = true,
        bool barrierDismissible = false) : base(
        settings,
        fullscreenDialog,
        maintainState,
        allowSnapshotting,
        barrierDismissible,
        requestFocus)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Title = title;
    }

    public override string? Title { get; }

    public override DelegatedTransitionBuilder DelegatedTransition =>
        CupertinoPageTransition.DelegatedTransition;

    /// <summary>Completes with the typed result supplied when this route is popped.</summary>
    public Task<T?> Completed => _completed.Task;

    protected override Widget BuildContent(BuildContext context) => _builder(context);

    public override void DidComplete(object? result)
    {
        base.DidComplete(result);
        CompleteResult(result);
    }

    public override void Dispose()
    {
        _completed.TrySetResult(default);
        base.Dispose();
    }

    private void CompleteResult(object? result)
    {
        if (result is null)
        {
            _completed.TrySetResult(default);
        }
        else if (result is T typedResult)
        {
            _completed.TrySetResult(typedResult);
        }
        else
        {
            _completed.TrySetException(new InvalidCastException(
                $"Route result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
        }
    }
}

/// <summary>An immutable Navigator page that creates a <see cref="CupertinoPageRoute{T}"/>.</summary>
public sealed record CupertinoPage<T> : Page
{
    public CupertinoPage(
        Widget child,
        bool maintainState = true,
        string? title = null,
        bool fullscreenDialog = false,
        bool allowSnapshotting = true,
        bool canPop = true,
        PopInvokedWithResultCallback<T>? onPopInvoked = null,
        Key? key = null,
        string? name = null,
        object? arguments = null,
        string? restorationId = null) : base(
        key,
        name,
        arguments,
        restorationId,
        canPop,
        AdaptPopCallback(onPopInvoked))
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        MaintainState = maintainState;
        Title = title;
        FullscreenDialog = fullscreenDialog;
        AllowSnapshotting = allowSnapshotting;
    }

    public Widget Child { get; }

    public bool MaintainState { get; }

    public string? Title { get; }

    public bool FullscreenDialog { get; }

    public bool AllowSnapshotting { get; }

    public override Route CreateRoute(BuildContext context)
    {
        _ = context;
        return new PageBasedCupertinoPageRoute<T>(this);
    }

    private static PopInvokedWithResultCallback<object>? AdaptPopCallback(
        PopInvokedWithResultCallback<T>? callback)
    {
        if (callback is null)
        {
            return null;
        }

        return (didPop, result) =>
        {
            if (result is null)
            {
                callback(didPop, default);
                return;
            }

            if (result is T typedResult)
            {
                callback(didPop, typedResult);
                return;
            }

            throw new InvalidCastException(
                $"Route result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}.");
        };
    }
}

internal sealed class PageBasedCupertinoPageRoute<T> : CupertinoRouteTransitionMixin<T>
{
    public PageBasedCupertinoPageRoute(CupertinoPage<T> page) : base(
        settings: page,
        fullscreenDialog: page.FullscreenDialog,
        maintainState: page.MaintainState,
        allowSnapshotting: page.AllowSnapshotting)
    {
    }

    private CupertinoPage<T> Page => (CupertinoPage<T>)Settings;

    public override string? Title => Page.Title;

    public override bool MaintainState => Page.MaintainState;

    public override bool FullscreenDialog => Page.FullscreenDialog;

    public override bool AllowSnapshotting => Page.AllowSnapshotting;

    public override DelegatedTransitionBuilder? DelegatedTransition => FullscreenDialog
        ? null
        : CupertinoPageTransition.DelegatedTransition;

    protected override Widget BuildContent(BuildContext context)
    {
        _ = context;
        return Page.Child;
    }
}

/// <summary>The horizontal iOS page transition with parallax and a directional leading-edge shadow.</summary>
public sealed class CupertinoPageTransition : StatefulWidget
{
    public CupertinoPageTransition(
        Animation<double> primaryRouteAnimation,
        Animation<double> secondaryRouteAnimation,
        Widget child,
        bool linearTransition,
        Key? key = null) : base(key)
    {
        PrimaryRouteAnimation = primaryRouteAnimation
                                ?? throw new ArgumentNullException(nameof(primaryRouteAnimation));
        SecondaryRouteAnimation = secondaryRouteAnimation
                                  ?? throw new ArgumentNullException(nameof(secondaryRouteAnimation));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        LinearTransition = linearTransition;
    }

    public Widget Child { get; }

    public Animation<double> PrimaryRouteAnimation { get; }

    public Animation<double> SecondaryRouteAnimation { get; }

    public bool LinearTransition { get; }

    public static Widget? DelegatedTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        _ = animation;
        _ = allowSnapshotting;
        Animation<double> curve = new DirectionalCurveAnimation(
            secondaryAnimation,
            Curves.LinearToEaseOut,
            Curves.EaseInToLinear);
        Animation<Vector> position = curve.Drive(CupertinoRouteTweens.MiddleLeft);
        return new SlideTransition(
            position: position,
            textDirection: Directionality.Of(context),
            transformHitTests: false,
            child: child);
    }

    public override State CreateState() => new CupertinoPageTransitionState();

    private sealed class CupertinoPageTransitionState : State
    {
        private Animation<Vector> _primaryPositionAnimation = null!;
        private Animation<Vector> _secondaryPositionAnimation = null!;
        private Animation<Decoration> _primaryShadowAnimation = null!;
        private CurvedAnimation? _primaryPositionCurve;
        private CurvedAnimation? _secondaryPositionCurve;
        private CurvedAnimation? _primaryShadowCurve;

        private CupertinoPageTransition CurrentWidget => (CupertinoPageTransition)StateWidget;

        public override void InitState()
        {
            base.InitState();
            SetupAnimation();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldTransition = (CupertinoPageTransition)oldWidget;
            if (!ReferenceEquals(oldTransition.PrimaryRouteAnimation, CurrentWidget.PrimaryRouteAnimation)
                || !ReferenceEquals(oldTransition.SecondaryRouteAnimation, CurrentWidget.SecondaryRouteAnimation)
                || oldTransition.LinearTransition != CurrentWidget.LinearTransition)
            {
                DisposeCurves();
                SetupAnimation();
            }
        }

        public override Widget Build(BuildContext context)
        {
            TextDirection textDirection = Directionality.Of(context);
            return new SlideTransition(
                position: _secondaryPositionAnimation,
                textDirection: textDirection,
                transformHitTests: false,
                child: new SlideTransition(
                    position: _primaryPositionAnimation,
                    textDirection: textDirection,
                    child: new DecoratedBoxTransition(
                        decoration: _primaryShadowAnimation,
                        child: CurrentWidget.Child)));
        }

        public override void Dispose()
        {
            DisposeCurves();
            base.Dispose();
        }

        private void SetupAnimation()
        {
            if (!CurrentWidget.LinearTransition)
            {
                _primaryPositionCurve = new CurvedAnimation(
                    CurrentWidget.PrimaryRouteAnimation,
                    Curves.FastEaseInToSlowEaseOut,
                    Curves.Flipped(Curves.FastEaseInToSlowEaseOut));
                _secondaryPositionCurve = new CurvedAnimation(
                    CurrentWidget.SecondaryRouteAnimation,
                    Curves.LinearToEaseOut,
                    Curves.EaseInToLinear);
                _primaryShadowCurve = new CurvedAnimation(
                    CurrentWidget.PrimaryRouteAnimation,
                    Curves.LinearToEaseOut);
            }

            _primaryPositionAnimation = (_primaryPositionCurve ?? CurrentWidget.PrimaryRouteAnimation)
                .Drive(CupertinoRouteTweens.RightMiddle);
            _secondaryPositionAnimation = (_secondaryPositionCurve ?? CurrentWidget.SecondaryRouteAnimation)
                .Drive(CupertinoRouteTweens.MiddleLeft);
            _primaryShadowAnimation = (_primaryShadowCurve ?? CurrentWidget.PrimaryRouteAnimation)
                .Drive(CupertinoEdgeShadowDecoration.Tween);
        }

        private void DisposeCurves()
        {
            _primaryPositionCurve?.Dispose();
            _secondaryPositionCurve?.Dispose();
            _primaryShadowCurve?.Dispose();
            _primaryPositionCurve = null;
            _secondaryPositionCurve = null;
            _primaryShadowCurve = null;
        }
    }
}

/// <summary>The bottom-up iOS transition used by fullscreen dialogs.</summary>
public sealed class CupertinoFullscreenDialogTransition : StatefulWidget
{
    public CupertinoFullscreenDialogTransition(
        Animation<double> primaryRouteAnimation,
        Animation<double> secondaryRouteAnimation,
        Widget child,
        bool linearTransition,
        Key? key = null) : base(key)
    {
        PrimaryRouteAnimation = primaryRouteAnimation
                                ?? throw new ArgumentNullException(nameof(primaryRouteAnimation));
        SecondaryRouteAnimation = secondaryRouteAnimation
                                  ?? throw new ArgumentNullException(nameof(secondaryRouteAnimation));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        LinearTransition = linearTransition;
    }

    public Animation<double> PrimaryRouteAnimation { get; }

    public Animation<double> SecondaryRouteAnimation { get; }

    public bool LinearTransition { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoFullscreenDialogTransitionState();

    private sealed class CupertinoFullscreenDialogTransitionState : State
    {
        private Animation<Vector> _primaryPositionAnimation = null!;
        private Animation<Vector> _secondaryPositionAnimation = null!;
        private CurvedAnimation? _primaryPositionCurve;
        private CurvedAnimation? _secondaryPositionCurve;

        private CupertinoFullscreenDialogTransition CurrentWidget =>
            (CupertinoFullscreenDialogTransition)StateWidget;

        public override void InitState()
        {
            base.InitState();
            SetupAnimation();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldTransition = (CupertinoFullscreenDialogTransition)oldWidget;
            if (!ReferenceEquals(oldTransition.PrimaryRouteAnimation, CurrentWidget.PrimaryRouteAnimation)
                || !ReferenceEquals(oldTransition.SecondaryRouteAnimation, CurrentWidget.SecondaryRouteAnimation)
                || oldTransition.LinearTransition != CurrentWidget.LinearTransition)
            {
                DisposeCurves();
                SetupAnimation();
            }
        }

        public override Widget Build(BuildContext context)
        {
            TextDirection textDirection = Directionality.Of(context);
            return new SlideTransition(
                position: _secondaryPositionAnimation,
                textDirection: textDirection,
                transformHitTests: false,
                child: new SlideTransition(
                    position: _primaryPositionAnimation,
                    child: CurrentWidget.Child));
        }

        public override void Dispose()
        {
            DisposeCurves();
            base.Dispose();
        }

        private void SetupAnimation()
        {
            _primaryPositionCurve = new CurvedAnimation(
                CurrentWidget.PrimaryRouteAnimation,
                Curves.LinearToEaseOut,
                Curves.Flipped(Curves.LinearToEaseOut));
            _primaryPositionAnimation = _primaryPositionCurve.Drive(CupertinoRouteTweens.BottomUp);
            Animation<double> secondaryParent;
            if (CurrentWidget.LinearTransition)
            {
                secondaryParent = CurrentWidget.SecondaryRouteAnimation;
            }
            else
            {
                _secondaryPositionCurve = new CurvedAnimation(
                    CurrentWidget.SecondaryRouteAnimation,
                    Curves.LinearToEaseOut,
                    Curves.EaseInToLinear);
                secondaryParent = _secondaryPositionCurve;
            }

            _secondaryPositionAnimation = secondaryParent.Drive(CupertinoRouteTweens.MiddleLeft);
        }

        private void DisposeCurves()
        {
            _primaryPositionCurve?.Dispose();
            _secondaryPositionCurve?.Dispose();
            _primaryPositionCurve = null;
            _secondaryPositionCurve = null;
        }
    }
}

/// <summary>Standalone Cupertino transition builder used by Material's platform transition theme.</summary>
public sealed class CupertinoPageTransitionsBuilder
{
    internal const double BackGestureWidth = 20.0;
    internal const double MinFlingVelocity = 1.0;
    internal static readonly TimeSpan DroppedSwipePageAnimationDuration = TimeSpan.FromMilliseconds(350);

    public TimeSpan TransitionDuration => CupertinoRouteTransitionMixin<object>.CupertinoTransitionDuration;

    public DelegatedTransitionBuilder DelegatedTransition => CupertinoPageTransition.DelegatedTransition;

    public Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return CupertinoRouteTransitionMixin<object>.BuildPageTransitions(
            route,
            context,
            animation,
            secondaryAnimation,
            child);
    }
}

/// <summary>A non-opaque iOS popup route that springs up from the bottom edge.</summary>
public sealed class CupertinoModalPopupRoute<T> : PopupRoute
{
    private static readonly VectorTween OffsetTween = new(
        begin: new Vector(0.0, 1.0),
        end: new Vector(0.0, 0.0));

    private readonly WidgetBuilder _builder;
    private readonly bool _barrierDismissible;
    private readonly Color? _barrierColor;
    private readonly bool _usesDefaultBarrierColor;
    private readonly bool _semanticsDismissible;
    private readonly string _barrierLabel;
    private readonly TaskCompletionSource<T?> _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CupertinoModalPopupRoute(
        WidgetBuilder builder,
        string barrierLabel = "Dismiss",
        Color? barrierColor = null,
        bool barrierDismissible = true,
        bool semanticsDismissible = false,
        ImageFilter? filter = null,
        RouteSettings? settings = null,
        bool? requestFocus = null,
        Point? anchorPoint = null) : base(settings, filter, requestFocus)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _barrierLabel = barrierLabel ?? throw new ArgumentNullException(nameof(barrierLabel));
        _barrierColor = barrierColor;
        _usesDefaultBarrierColor = barrierColor is null;
        _barrierDismissible = barrierDismissible;
        _semanticsDismissible = semanticsDismissible;
        AnchorPoint = anchorPoint;
    }

    public override string BarrierLabel => _barrierLabel;

    public override Color? BarrierColor => _usesDefaultBarrierColor
        ? Navigator is null
            ? Color.FromUInt32(0x33000000)
            : CupertinoRouteConstants.ModalBarrierColor.ResolveFrom(Navigator.Context)
        : _barrierColor;

    public override bool BarrierDismissible => _barrierDismissible;

    public override bool SemanticsDismissible => _semanticsDismissible;

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(335);

    public Point? AnchorPoint { get; }

    public Task<T?> Completed => _completed.Task;

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

    public override Widget BuildPage(BuildContext context)
    {
        _ = context;
        return new CupertinoUserInterfaceLevel(
            data: CupertinoUserInterfaceLevelData.Elevated,
            child: new DisplayFeatureSubScreen(
                anchorPoint: AnchorPoint,
                child: new Builder(_builder)));
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        _ = context;
        _ = secondaryAnimation;
        return new Align(
            alignment: Alignment.BottomCenter,
            child: new FractionalTranslation(
                translation: OffsetTween.Transform(animation.Value),
                child: child));
    }

    public override void DidComplete(object? result)
    {
        base.DidComplete(result);
        if (result is null)
        {
            _completed.TrySetResult(default);
        }
        else if (result is T typedResult)
        {
            _completed.TrySetResult(typedResult);
        }
        else
        {
            _completed.TrySetException(new InvalidCastException(
                $"Popup result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
        }
    }

    public override void Dispose()
    {
        _completed.TrySetResult(default);
        base.Dispose();
    }
}

internal static class CupertinoRouteTweens
{
    public static VectorTween RightMiddle { get; } = new(
        begin: new Vector(1.0, 0.0),
        end: new Vector(0.0, 0.0));

    public static VectorTween MiddleLeft { get; } = new(
        begin: new Vector(0.0, 0.0),
        end: new Vector(-1.0 / 3.0, 0.0));

    public static VectorTween BottomUp { get; } = new(
        begin: new Vector(0.0, 1.0),
        end: new Vector(0.0, 0.0));
}

internal sealed class DirectionalCurveAnimation : Animation<double>
{
    private readonly Animation<double> _parent;
    private readonly Curve _forwardCurve;
    private readonly Curve _reverseCurve;

    public DirectionalCurveAnimation(
        Animation<double> parent,
        Curve forwardCurve,
        Curve reverseCurve)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _forwardCurve = forwardCurve ?? throw new ArgumentNullException(nameof(forwardCurve));
        _reverseCurve = reverseCurve ?? throw new ArgumentNullException(nameof(reverseCurve));
    }

    public override double Value => Status == AnimationStatus.Reverse
        ? _reverseCurve(_parent.Value)
        : _forwardCurve(_parent.Value);

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener) => _parent.AddListener(listener);

    public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener) =>
        _parent.AddStatusListener(listener);

    public override void RemoveStatusListener(Action<AnimationStatus> listener) =>
        _parent.RemoveStatusListener(listener);
}

internal sealed record CupertinoEdgeShadowDecoration : Decoration
{
    private static readonly IReadOnlyList<Color> ShadowColors =
    [
        Color.FromUInt32(0x04000000),
        Colors.Transparent,
    ];

    private readonly IReadOnlyList<Color>? _colors;

    private CupertinoEdgeShadowDecoration(IReadOnlyList<Color>? colors = null)
    {
        _colors = colors;
    }

    public static DecorationTween Tween { get; } = new(
        begin: new CupertinoEdgeShadowDecoration(),
        end: new CupertinoEdgeShadowDecoration(ShadowColors));

    public static CupertinoEdgeShadowDecoration? Lerp(
        CupertinoEdgeShadowDecoration? a,
        CupertinoEdgeShadowDecoration? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        IReadOnlyList<Color>? aColors = a?._colors;
        IReadOnlyList<Color>? bColors = b?._colors;
        if (aColors is null && bColors is null)
        {
            return b ?? a;
        }

        int colorCount = bColors?.Count ?? aColors!.Count;
        if (aColors is not null && bColors is not null && aColors.Count != bColors.Count)
        {
            throw new InvalidOperationException("Cupertino edge-shadow gradients must have matching lengths.");
        }

        var colors = new List<Color>(colorCount);
        for (int index = 0; index < colorCount; index += 1)
        {
            colors.Add(LerpColor(aColors?[index], bColors?[index], t));
        }

        return new CupertinoEdgeShadowDecoration(colors);
    }

    public override Decoration? LerpFrom(Decoration? a, double t)
    {
        return a is CupertinoEdgeShadowDecoration decoration
            ? Lerp(decoration, this, t)
            : Lerp(null, this, t);
    }

    public override Decoration? LerpTo(Decoration? b, double t)
    {
        return b is CupertinoEdgeShadowDecoration decoration
            ? Lerp(this, decoration, t)
            : Lerp(this, null, t);
    }

    public override BoxPainter CreateBoxPainter(Action? onChanged = null)
    {
        return new CupertinoEdgeShadowPainter(this, onChanged);
    }

    private static Color LerpColor(Color? a, Color? b, double t)
    {
        if (a is null)
        {
            Color color = b!.Value;
            return Color.FromArgb(
                (byte)Math.Round(color.A * t),
                color.R,
                color.G,
                color.B);
        }

        if (b is null)
        {
            Color color = a.Value;
            return Color.FromArgb(
                (byte)Math.Round(color.A * (1.0 - t)),
                color.R,
                color.G,
                color.B);
        }

        byte LerpChannel(byte start, byte end) => (byte)Math.Round(start + ((end - start) * t));
        return Color.FromArgb(
            LerpChannel(a.Value.A, b.Value.A),
            LerpChannel(a.Value.R, b.Value.R),
            LerpChannel(a.Value.G, b.Value.G),
            LerpChannel(a.Value.B, b.Value.B));
    }

    private sealed class CupertinoEdgeShadowPainter : BoxPainter
    {
        private readonly CupertinoEdgeShadowDecoration _decoration;

        public CupertinoEdgeShadowPainter(
            CupertinoEdgeShadowDecoration decoration,
            Action? onChanged) : base(onChanged)
        {
            _decoration = decoration;
        }

        public override void Paint(
            PaintingContext context,
            Point offset,
            ImageConfiguration configuration)
        {
            IReadOnlyList<Color>? colors = _decoration._colors;
            if (colors is null || configuration.Size is not { } size)
            {
                return;
            }

            double shadowWidth = 0.05 * size.Width;
            double shadowHeight = size.Height;
            double bandWidth = shadowWidth / (colors.Count - 1);
            TextDirection textDirection = configuration.TextDirection
                ?? throw new InvalidOperationException("The Cupertino page shadow requires a text direction.");
            double shadowDirection = textDirection == TextDirection.Rtl ? 1.0 : -1.0;
            double start = textDirection == TextDirection.Rtl ? offset.X + size.Width : offset.X;

            for (double dx = 0.0; dx < shadowWidth; dx += 1.0)
            {
                int bandColorIndex = Math.Min((int)(dx / bandWidth), colors.Count - 2);
                double localT = (dx % bandWidth) / bandWidth;
                Color color = LerpColor(colors[bandColorIndex], colors[bandColorIndex + 1], localT);
                double x = start + (shadowDirection * dx);
                context.Canvas.DrawRectangle(
                    new SolidColorBrush(color),
                    null,
                    new Rect(x - 1.0, offset.Y, 1.0, shadowHeight));
            }
        }
    }
}

internal sealed class CupertinoBackGestureDetector : StatefulWidget
{
    public CupertinoBackGestureDetector(PageRoute route, Widget child)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public PageRoute Route { get; }

    public Widget Child { get; }

    public override State CreateState() => new CupertinoBackGestureDetectorState();

    private sealed class CupertinoBackGestureDetectorState : State
    {
        private HorizontalDragGestureRecognizer? _recognizer;
        private bool _gestureStarted;
        private double _pageWidth;
        private TextDirection _textDirection;

        private CupertinoBackGestureDetector CurrentWidget =>
            (CupertinoBackGestureDetector)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _recognizer = new HorizontalDragGestureRecognizer
            {
                OnStart = HandleDragStart,
                OnUpdate = HandleDragUpdate,
                OnEnd = HandleDragEnd,
                OnCancel = HandleDragCancel,
            };
        }

        public override Widget Build(BuildContext context)
        {
            _textDirection = Directionality.Of(context);
            _pageWidth = MediaQuery.Of(context).Size.Width;
            Thickness padding = MediaQuery.MaybePaddingOf(context) ?? default;
            double inset = _textDirection == TextDirection.Ltr ? padding.Left : padding.Right;
            double dragAreaWidth = Math.Max(
                inset,
                CupertinoPageTransitionsBuilder.BackGestureWidth);
            Widget stack = new Stack(
                fit: StackFit.Passthrough,
                children:
                [
                    CurrentWidget.Child,
                    new PositionedDirectional(
                        start: 0.0,
                        width: dragAreaWidth,
                        top: 0.0,
                        bottom: 0.0,
                        child: new Listener(
                            onPointerDown: HandlePointerDown,
                            behavior: HitTestBehavior.Translucent)),
                ]);
            return new CupertinoPageSizeObserver(size => _pageWidth = size.Width, stack);
        }

        public override void Dispose()
        {
            if (_gestureStarted && CurrentWidget.Route.PopGestureInProgress)
            {
                CurrentWidget.Route.HandleSettleBackGesture(
                    animateForward: true,
                    CupertinoPageTransitionsBuilder.DroppedSwipePageAnimationDuration,
                    Curves.FastEaseInToSlowEaseOut);
            }

            _recognizer?.Dispose();
            _recognizer = null;
            base.Dispose();
        }

        private void HandlePointerDown(PointerDownEvent pointerEvent)
        {
            if (CurrentWidget.Route.PopGestureEnabled)
            {
                _recognizer!.AddPointer(pointerEvent);
            }
        }

        private void HandleDragStart(DragStartDetails details)
        {
            _ = details;
            if (!CurrentWidget.Route.PopGestureEnabled)
            {
                return;
            }

            _gestureStarted = true;
            CurrentWidget.Route.HandleStartBackGesture(CurrentWidget.Route.Animation.Value);
        }

        private void HandleDragUpdate(DragUpdateDetails details)
        {
            if (!_gestureStarted || _pageWidth <= 0.0)
            {
                return;
            }

            double delta = (details.PrimaryDelta ?? 0.0) / _pageWidth;
            if (_textDirection == TextDirection.Rtl)
            {
                delta = -delta;
            }

            CurrentWidget.Route.HandleUpdateBackGestureProgress(
                CurrentWidget.Route.Animation.Value - delta);
        }

        private void HandleDragEnd(DragEndDetails details)
        {
            double velocity = _pageWidth <= 0.0 ? 0.0 : (details.PrimaryVelocity ?? 0.0) / _pageWidth;
            if (_textDirection == TextDirection.Rtl)
            {
                velocity = -velocity;
            }

            EndGesture(velocity);
        }

        private void HandleDragCancel()
        {
            EndGesture(0.0);
        }

        private void EndGesture(double velocity)
        {
            if (!_gestureStarted)
            {
                return;
            }

            PageRoute route = CurrentWidget.Route;
            bool animateForward;
            if (!route.IsCurrent)
            {
                animateForward = route.IsActive;
            }
            else if (Math.Abs(velocity) >= CupertinoPageTransitionsBuilder.MinFlingVelocity)
            {
                animateForward = velocity <= 0.0;
            }
            else
            {
                animateForward = route.Animation.Value > 0.5;
            }

            _gestureStarted = false;
            route.HandleSettleBackGesture(
                animateForward,
                CupertinoPageTransitionsBuilder.DroppedSwipePageAnimationDuration,
                Curves.FastEaseInToSlowEaseOut);
        }
    }
}

internal sealed class CupertinoPageSizeObserver : SingleChildRenderObjectWidget
{
    public CupertinoPageSizeObserver(Action<Size> onSizeChanged, Widget child) : base(child)
    {
        OnSizeChanged = onSizeChanged ?? throw new ArgumentNullException(nameof(onSizeChanged));
    }

    public Action<Size> OnSizeChanged { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        _ = context;
        return new RenderCupertinoPageSizeObserver(OnSizeChanged);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        _ = context;
        ((RenderCupertinoPageSizeObserver)renderObject).OnSizeChanged = OnSizeChanged;
    }
}

internal sealed class RenderCupertinoPageSizeObserver : RenderProxyBox
{
    private Size _lastSize = new(double.NaN, double.NaN);

    public RenderCupertinoPageSizeObserver(Action<Size> onSizeChanged)
    {
        OnSizeChanged = onSizeChanged;
    }

    public Action<Size> OnSizeChanged { get; set; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (_lastSize == Size)
        {
            return;
        }

        _lastSize = Size;
        OnSizeChanged(Size);
    }
}
