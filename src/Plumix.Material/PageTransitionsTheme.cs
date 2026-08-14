using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// material_ui/lib/src/page.dart
// material_ui/lib/src/page_transitions_theme.dart
// material_ui/lib/src/predictive_back_page_transitions_builder.dart
// cupertino_ui/lib/src/route.dart

public abstract class PageTransitionsBuilder
{
    public virtual DelegatedTransitionBuilder? DelegatedTransition => null;

    public virtual TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(300);

    public virtual TimeSpan ReverseTransitionDuration => TransitionDuration;

    public abstract Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child);
}

public sealed class FadeForwardsPageTransitionsBuilder : PageTransitionsBuilder
{
    public const int TransitionMilliseconds = 450;

    public FadeForwardsPageTransitionsBuilder(Color? backgroundColor = null)
    {
        BackgroundColor = backgroundColor;
    }

    public Color? BackgroundColor { get; }

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(TransitionMilliseconds);

    public override DelegatedTransitionBuilder DelegatedTransition => BuildDelegatedTransition;

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        _ = route;
        Widget delegatedChild = BuildDelegated(
            context,
            secondaryAnimation,
            BackgroundColor,
            child);
        return new DualTransitionBuilder(
            animation,
            forwardBuilder: (_, forwardAnimation, transitionChild) => new FadeTransition(
                opacity: Map(forwardAnimation, value => Interval(value, 0.0, 0.75)),
                child: new SlideTransition(
                    position: Map(
                        forwardAnimation,
                        value => new Vector(
                            0.25 * (1.0 - Curves.EaseInOutCubicEmphasized(value)),
                            0.0)),
                    child: transitionChild)),
            reverseBuilder: (_, reverseAnimation, transitionChild) => new IgnorePointer(
                ignoring: reverseAnimation.Status == AnimationStatus.Forward,
                child: new FadeTransition(
                    opacity: Map(reverseAnimation, value => 1.0 - Interval(value, 0.0, 0.25)),
                    child: new SlideTransition(
                        position: Map(
                            reverseAnimation,
                            value => new Vector(
                                0.25 * Curves.EaseInOutCubicEmphasized(value),
                                0.0)),
                        child: transitionChild))),
            child: delegatedChild);
    }

    private Widget? BuildDelegatedTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        _ = animation;
        _ = allowSnapshotting;
        return BuildDelegated(context, secondaryAnimation, BackgroundColor, child);
    }

    private static Widget BuildDelegated(
        BuildContext context,
        Animation<double> secondaryAnimation,
        Color? backgroundColor,
        Widget? child)
    {
        Widget transition = new DualTransitionBuilder(
            new ReverseAnimation(secondaryAnimation),
            forwardBuilder: (_, animation, transitionChild) => new FadeTransition(
                opacity: Map(animation, value => Interval(value, 0.0, 0.75)),
                child: new SlideTransition(
                    position: Map(
                        animation,
                        value => new Vector(
                            -0.25 * (1.0 - Curves.EaseInOutCubicEmphasized(value)),
                            0.0)),
                    child: transitionChild)),
            reverseBuilder: (_, animation, transitionChild) => new FadeTransition(
                opacity: Map(animation, value => 1.0 - Interval(value, 0.0, 0.25)),
                child: new SlideTransition(
                    position: Map(
                        animation,
                        value => new Vector(
                            -0.25 * Curves.EaseInOutCubicEmphasized(value),
                            0.0)),
                    child: transitionChild)),
            child: child);
        if (!(ModalRoute.OpaqueOf(context) ?? true))
        {
            return transition;
        }

        Color resolvedBackground = backgroundColor ?? Theme.Of(context).ColorScheme.Surface;
        return new AnimatedBuilder(
            secondaryAnimation,
            (_, transitionChild) => new ColoredBox(
                color: secondaryAnimation.Status.IsAnimating()
                    ? resolvedBackground
                    : Transparent(resolvedBackground),
                child: transitionChild),
            transition);
    }

    private static Color Transparent(Color color)
    {
        return Color.FromArgb(0, color.R, color.G, color.B);
    }

    private static Animation<double> Map(Animation<double> parent, Func<double, double> transform)
    {
        return new MappedAnimation<double>(parent, transform);
    }

    private static Animation<Vector> Map(Animation<double> parent, Func<double, Vector> transform)
    {
        return new MappedAnimation<Vector>(parent, transform);
    }

    private static double Interval(double value, double begin, double end)
    {
        return Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
    }
}

/// <summary>
/// Dart's private `_FadeUpwardsPageTransition`: slides the page up from 1/4 screen below the top while
/// fading it in.
/// </summary>
internal sealed class FadeUpwardsPageTransition : StatelessWidget
{
    private static readonly VectorTween BottomUpTween = new(begin: new Vector(0.0, 0.25), end: default(Vector));
    private static readonly CurveTween FastOutSlowInTween = new(Curves.FastOutSlowIn);
    private static readonly CurveTween EaseInTween = new(Curves.EaseIn);

    public FadeUpwardsPageTransition(
        Animation<double> routeAnimation,
        Widget child,
        Foundation.Key? key = null) : base(key)
    {
        PositionAnimation = routeAnimation.Drive(BottomUpTween.Chain(FastOutSlowInTween));
        OpacityAnimation = routeAnimation.Drive(EaseInTween);
        Child = child;
    }

    public Animation<Vector> PositionAnimation { get; }

    public Animation<double> OpacityAnimation { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new SlideTransition(
            position: PositionAnimation,
            child: new FadeTransition(opacity: OpacityAnimation, child: Child));
    }
}

/// <summary>
/// A page transition that fades the incoming page in while sliding it upwards, matching the default on
/// Android O.
/// </summary>
public sealed class FadeUpwardsPageTransitionsBuilder : PageTransitionsBuilder
{
    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        _ = route;
        _ = context;
        _ = secondaryAnimation;
        return BuildTransitions(animation, child);
    }

    /// <summary>
    /// Dart widens this override's `route`, `context` and `secondaryAnimation` to nullable and ignores all
    /// three. C# forbids widening an override's parameter list, so the null-argument form is this overload.
    /// </summary>
    public Widget BuildTransitions(Animation<double> animation, Widget child)
    {
        return new FadeUpwardsPageTransition(animation, child);
    }
}

public sealed class ZoomPageTransitionsBuilder : PageTransitionsBuilder
{
    public ZoomPageTransitionsBuilder(
        bool allowSnapshotting = true,
        bool allowEnterRouteSnapshotting = true,
        Color? backgroundColor = null)
    {
        AllowSnapshotting = allowSnapshotting;
        AllowEnterRouteSnapshotting = allowEnterRouteSnapshotting;
        BackgroundColor = backgroundColor;
    }

    public bool AllowSnapshotting { get; }

    public bool AllowEnterRouteSnapshotting { get; }

    public Color? BackgroundColor { get; }

    public override DelegatedTransitionBuilder DelegatedTransition => BuildDelegatedTransition;

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        bool allowSnapshotting = AllowSnapshotting && route.AllowSnapshotting;
        Widget delegatedChild = BuildZoomDelegated(
            context,
            animation,
            secondaryAnimation,
            allowSnapshotting,
            AllowEnterRouteSnapshotting,
            BackgroundColor,
            child);
        Color background = BackgroundColor ?? Theme.Of(context).ColorScheme.Surface;
        return new DualTransitionBuilder(
            animation,
            forwardBuilder: (_, forwardAnimation, transitionChild) => BuildEnter(
                context,
                forwardAnimation,
                reverse: false,
                background,
                allowSnapshotting && AllowEnterRouteSnapshotting,
                transitionChild),
            reverseBuilder: (_, reverseAnimation, transitionChild) => BuildExit(
                context,
                reverseAnimation,
                reverse: true,
                allowSnapshotting,
                transitionChild),
            child: delegatedChild);
    }

    private Widget? BuildDelegatedTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        return BuildZoomDelegated(
            context,
            animation,
            secondaryAnimation,
            allowSnapshotting && AllowSnapshotting,
            AllowEnterRouteSnapshotting,
            BackgroundColor,
            child);
    }

    private static Widget BuildZoomDelegated(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        bool allowEnterRouteSnapshotting,
        Color? backgroundColor,
        Widget? child)
    {
        _ = animation;
        Color background = backgroundColor ?? Theme.Of(context).ColorScheme.Surface;
        return new DualTransitionBuilder(
            new ReverseAnimation(secondaryAnimation),
            forwardBuilder: (_, transitionAnimation, transitionChild) => BuildEnter(
                context,
                transitionAnimation,
                reverse: true,
                background,
                allowSnapshotting && allowEnterRouteSnapshotting,
                transitionChild),
            reverseBuilder: (_, transitionAnimation, transitionChild) => BuildExit(
                context,
                transitionAnimation,
                reverse: false,
                allowSnapshotting,
                transitionChild),
            child: child);
    }

    private static Widget BuildEnter(
        BuildContext context,
        Animation<double> animation,
        bool reverse,
        Color background,
        bool allowSnapshotting,
        Widget? child)
    {
        return new AnimatedBuilder(
            animation,
            (_, transitionChild) =>
            {
                double value = animation.Value;
                double opacity = reverse ? 1.0 : Interval(value, 0.125, 0.250);
                double scale = reverse
                    ? Lerp(1.10, 1.00, ZoomScaleCurve(value))
                    : Lerp(0.85, 1.00, ZoomScaleCurve(value));
                double scrimOpacity = reverse || animation.Status == AnimationStatus.Completed
                    ? 0.0
                    : 0.60 * Interval(value, 0.2075, 0.4175);
                Widget content = new ColoredBox(
                    color: WithOpacity(background, scrimOpacity),
                    child: new FadeTransition(
                        new ConstantAnimation<double>(opacity),
                        child: new ScaleTransition(
                            new ConstantAnimation<double>(scale),
                            filterQuality: FilterQuality.Medium,
                            child: transitionChild)));
                return new SnapshotGate(
                    animation,
                    allowSnapshotting,
                    MediaQuery.Of(context).DevicePixelRatio,
                    content);
            },
            child);
    }

    private static Widget BuildExit(
        BuildContext context,
        Animation<double> animation,
        bool reverse,
        bool allowSnapshotting,
        Widget? child)
    {
        return new AnimatedBuilder(
            animation,
            (_, transitionChild) =>
            {
                double value = animation.Value;
                double opacity = reverse ? 1.0 - Interval(value, 0.0825, 0.2075) : 1.0;
                double scale = reverse
                    ? Lerp(1.00, 0.90, ZoomScaleCurve(value))
                    : Lerp(1.00, 1.05, ZoomScaleCurve(value));
                Widget content = new FadeTransition(
                    new ConstantAnimation<double>(opacity),
                    child: new ScaleTransition(
                        new ConstantAnimation<double>(scale),
                        filterQuality: FilterQuality.Medium,
                        child: transitionChild));
                return new SnapshotGate(
                    animation,
                    allowSnapshotting,
                    MediaQuery.Of(context).DevicePixelRatio,
                    content);
            },
            child);
    }

    private static double ZoomScaleCurve(double value)
    {
        const double firstWeight = 0.166666;
        if (value <= firstWeight)
        {
            double local = value / firstWeight;
            return 0.4 * Curves.Cubic(0.05, 0.0, 0.133333, 0.06)(local);
        }

        double second = (value - firstWeight) / (1.0 - firstWeight);
        return 0.4 + (0.6 * Curves.Cubic(0.208333, 0.82, 0.25, 1.0)(second));
    }

    private static double Interval(double value, double begin, double end)
    {
        return Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
    }

    private static double Lerp(double begin, double end, double t)
    {
        return begin + ((end - begin) * Math.Clamp(t, 0.0, 1.0));
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        byte alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * byte.MaxValue);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private sealed class SnapshotGate : StatefulWidget
    {
        public SnapshotGate(
            Animation<double> animation,
            bool allowSnapshotting,
            double pixelRatio,
            Widget? child)
        {
            Animation = animation;
            AllowSnapshotting = allowSnapshotting;
            PixelRatio = pixelRatio;
            Child = child;
        }

        public Animation<double> Animation { get; }

        public bool AllowSnapshotting { get; }

        public double PixelRatio { get; }

        public Widget? Child { get; }

        public override State CreateState()
        {
            return new SnapshotGateState();
        }

        private sealed class SnapshotGateState : State
        {
            private readonly SnapshotController _controller = new();

            private SnapshotGate CurrentWidget => (SnapshotGate)StateWidget;

            public override void InitState()
            {
                CurrentWidget.Animation.AddStatusListener(HandleStatusChanged);
                UpdateController();
            }

            public override void DidUpdateWidget(StatefulWidget oldWidget)
            {
                var oldGate = (SnapshotGate)oldWidget;
                if (!ReferenceEquals(oldGate.Animation, CurrentWidget.Animation))
                {
                    oldGate.Animation.RemoveStatusListener(HandleStatusChanged);
                    CurrentWidget.Animation.AddStatusListener(HandleStatusChanged);
                }

                UpdateController();
            }

            public override Widget Build(BuildContext context)
            {
                return new SnapshotWidget(
                    _controller,
                    mode: SnapshotMode.Permissive,
                    autoresize: true,
                    pixelRatio: CurrentWidget.PixelRatio,
                    child: CurrentWidget.Child);
            }

            public override void Dispose()
            {
                CurrentWidget.Animation.RemoveStatusListener(HandleStatusChanged);
                _controller.Dispose();
            }

            private void HandleStatusChanged(AnimationStatus status)
            {
                _ = status;
                UpdateController();
            }

            private void UpdateController()
            {
                _controller.AllowSnapshotting = CurrentWidget.AllowSnapshotting
                                                   && CurrentWidget.Animation.Status.IsAnimating();
            }
        }
    }
}

public sealed class CupertinoPageTransitionsBuilder : PageTransitionsBuilder
{
    internal const double BackGestureWidth = 20.0;
    internal const double MinFlingVelocity = 1.0;
    internal static readonly TimeSpan DroppedSwipePageAnimationDuration = TimeSpan.FromMilliseconds(350);

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(500);

    public override DelegatedTransitionBuilder DelegatedTransition => BuildDelegatedTransition;

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        bool linearTransition = route.PopGestureInProgress;
        if (route.FullscreenDialog)
        {
            return new SlideTransition(
                position: MapOffset(
                    secondaryAnimation,
                    linearTransition,
                    Curves.LinearToEaseOut,
                    Curves.EaseInToLinear,
                    begin: default,
                    end: new Vector(-1.0 / 3.0, 0.0)),
                textDirection: Directionality.Of(context),
                transformHitTests: false,
                child: new SlideTransition(
                    position: MapOffset(
                        animation,
                        linearTransition: false,
                        Curves.LinearToEaseOut,
                        Curves.Flipped(Curves.LinearToEaseOut),
                        begin: new Vector(0.0, 1.0),
                        end: default),
                    child: child));
        }

        TextDirection textDirection = Directionality.Of(context);
        Animation<double> shadowAnimation = MapCurve(
            animation,
            linearTransition,
            Curves.LinearToEaseOut,
            Curves.LinearToEaseOut);
        Widget transition = new SlideTransition(
            position: MapOffset(
                secondaryAnimation,
                linearTransition,
                Curves.LinearToEaseOut,
                Curves.EaseInToLinear,
                begin: default,
                end: new Vector(-1.0 / 3.0, 0.0)),
            textDirection: textDirection,
            transformHitTests: false,
            child: new SlideTransition(
                position: MapOffset(
                    animation,
                    linearTransition,
                    Curves.FastEaseInToSlowEaseOut,
                    Curves.Flipped(Curves.FastEaseInToSlowEaseOut),
                    begin: new Vector(1.0, 0.0),
                    end: default),
                textDirection: textDirection,
                child: new CustomPaint(
                    painter: new CupertinoEdgeShadowPainter(shadowAnimation, textDirection),
                    child: child)));
        return new CupertinoBackGestureDetector(route, transition);
    }

    private static Animation<double> MapCurve(
        Animation<double> animation,
        bool linearTransition,
        Curve forwardCurve,
        Curve reverseCurve)
    {
        return new MappedAnimation<double>(
            animation,
            value => linearTransition
                ? value
                : animation.Status == AnimationStatus.Reverse
                    ? reverseCurve(value)
                    : forwardCurve(value));
    }

    private static Animation<Vector> MapOffset(
        Animation<double> animation,
        bool linearTransition,
        Curve forwardCurve,
        Curve reverseCurve,
        Vector begin,
        Vector end)
    {
        Animation<double> curvedAnimation = MapCurve(
            animation,
            linearTransition,
            forwardCurve,
            reverseCurve);
        return new MappedAnimation<Vector>(
            curvedAnimation,
            value => new Vector(
                begin.X + ((end.X - begin.X) * value),
                begin.Y + ((end.Y - begin.Y) * value)));
    }

    private sealed class CupertinoEdgeShadowPainter : CustomPainter
    {
        private readonly Animation<double> _animation;
        private readonly TextDirection _textDirection;

        public CupertinoEdgeShadowPainter(
            Animation<double> animation,
            TextDirection textDirection) : base(animation)
        {
            _animation = animation;
            _textDirection = textDirection;
        }

        public override void Paint(PaintingContext context, Size size)
        {
            double opacity = Math.Clamp(_animation.Value, 0.0, 1.0);
            if (opacity <= 0.0 || size.Width <= 0.0 || size.Height <= 0.0)
            {
                return;
            }

            double shadowWidth = 0.05 * size.Width;
            double direction = _textDirection == TextDirection.Ltr ? -1.0 : 1.0;
            double start = _textDirection == TextDirection.Ltr ? 0.0 : size.Width;
            for (double dx = 0.0; dx < shadowWidth; dx += 1.0)
            {
                double gradient = 1.0 - (dx / shadowWidth);
                byte alpha = (byte)Math.Round(4.0 * opacity * gradient);
                double x = start + (direction * dx);
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)),
                    null,
                    new Rect(x - 1.0, 0.0, 1.0, size.Height));
            }
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate)
        {
            return oldDelegate is not CupertinoEdgeShadowPainter oldPainter
                   || !ReferenceEquals(oldPainter._animation, _animation)
                   || oldPainter._textDirection != _textDirection;
        }
    }

    private sealed class CupertinoBackGestureDetector : StatefulWidget
    {
        public CupertinoBackGestureDetector(PageRoute route, Widget child)
        {
            Route = route;
            Child = child;
        }

        public PageRoute Route { get; }

        public Widget Child { get; }

        public override State CreateState()
        {
            return new CupertinoBackGestureDetectorState();
        }

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
                double dragAreaWidth = Math.Max(inset, BackGestureWidth);
                Widget stack = new Stack(
                    fit: StackFit.Passthrough,
                    children:
                    [
                        new IgnorePointer(
                            ignoring: CurrentWidget.Route.PopGestureInProgress,
                            child: CurrentWidget.Child),
                        new PositionedDirectional(
                            start: 0.0,
                            width: dragAreaWidth,
                            top: 0.0,
                            bottom: 0.0,
                            child: new Listener(
                                onPointerDown: HandlePointerDown,
                                behavior: HitTestBehavior.Translucent)),
                    ]);
                return new CupertinoPageSizeObserver(
                    size => _pageWidth = size.Width,
                    stack);
            }

            public override void Dispose()
            {
                if (_gestureStarted && CurrentWidget.Route.PopGestureInProgress)
                {
                    CurrentWidget.Route.HandleSettleBackGesture(
                        animateForward: true,
                        DroppedSwipePageAnimationDuration,
                        Curves.FastEaseInToSlowEaseOut);
                }

                _recognizer?.Dispose();
                _recognizer = null;
            }

            private void HandlePointerDown(PointerDownEvent @event)
            {
                if (CurrentWidget.Route.PopGestureEnabled)
                {
                    _recognizer!.AddPointer(@event);
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

                double delta = details.PrimaryDelta / _pageWidth;
                if (_textDirection == TextDirection.Rtl)
                {
                    delta = -delta;
                }

                CurrentWidget.Route.HandleUpdateBackGestureProgress(
                    CurrentWidget.Route.Animation.Value - delta);
            }

            private void HandleDragEnd(DragEndDetails details)
            {
                double velocity = _pageWidth <= 0.0 ? 0.0 : details.PrimaryVelocity / _pageWidth;
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
                else if (Math.Abs(velocity) >= MinFlingVelocity)
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
                    DroppedSwipePageAnimationDuration,
                    Curves.FastEaseInToSlowEaseOut);
            }
        }
    }

    private sealed class CupertinoPageSizeObserver : SingleChildRenderObjectWidget
    {
        public CupertinoPageSizeObserver(Action<Size> onSizeChanged, Widget child) : base(child)
        {
            OnSizeChanged = onSizeChanged;
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

    private sealed class RenderCupertinoPageSizeObserver : RenderProxyBox
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

    private static Widget? BuildDelegatedTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        _ = animation;
        _ = allowSnapshotting;
        return new SlideTransition(
            position: MapOffset(
                secondaryAnimation,
                linearTransition: false,
                Curves.LinearToEaseOut,
                Curves.EaseInToLinear,
                begin: default,
                end: new Vector(-1.0 / 3.0, 0.0)),
            textDirection: Directionality.Of(context),
            transformHitTests: false,
            child: child);
    }
}

public sealed class PredictiveBackPageTransitionsBuilder : PageTransitionsBuilder
{
    public PredictiveBackPageTransitionsBuilder(Color? fallbackColor = null)
    {
        FallbackColor = fallbackColor;
    }

    public Color? FallbackColor { get; }

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(
        FadeForwardsPageTransitionsBuilder.TransitionMilliseconds);

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return new PredictiveBackGestureDetector(
            route,
            animation,
            secondaryAnimation,
            child,
            fullscreen: false,
            FallbackColor);
    }
}

public sealed class PredictiveBackFullscreenPageTransitionsBuilder : PageTransitionsBuilder
{
    public PredictiveBackFullscreenPageTransitionsBuilder(Color? fallbackColor = null)
    {
        FallbackColor = fallbackColor;
    }

    public Color? FallbackColor { get; }

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return new PredictiveBackGestureDetector(
            route,
            animation,
            secondaryAnimation,
            child,
            fullscreen: true,
            FallbackColor);
    }
}

public sealed class PageTransitionsTheme : IEquatable<PageTransitionsTheme>
{
    private static readonly IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> DefaultBuilders =
        new Dictionary<TargetPlatform, PageTransitionsBuilder>
        {
            [TargetPlatform.Android] = new PredictiveBackPageTransitionsBuilder(),
            [TargetPlatform.IOS] = new CupertinoPageTransitionsBuilder(),
            [TargetPlatform.MacOS] = new CupertinoPageTransitionsBuilder(),
            [TargetPlatform.Windows] = new ZoomPageTransitionsBuilder(),
            [TargetPlatform.Linux] = new ZoomPageTransitionsBuilder(),
        };

    public PageTransitionsTheme(IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder>? builders = null)
    {
        Builders = builders ?? DefaultBuilders;
    }

    public IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> Builders { get; }

    public PageTransitionsBuilder Resolve(TargetPlatform platform)
    {
        if (Builders.TryGetValue(platform, out PageTransitionsBuilder? builder))
        {
            return builder;
        }

        return platform == TargetPlatform.IOS
            ? new CupertinoPageTransitionsBuilder()
            : new ZoomPageTransitionsBuilder();
    }

    public DelegatedTransitionBuilder? DelegatedTransition(TargetPlatform platform)
    {
        return Builders.TryGetValue(platform, out PageTransitionsBuilder? builder)
            ? builder.DelegatedTransition
            : new ZoomPageTransitionsBuilder().DelegatedTransition;
    }

    public Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return new PageTransitionsThemeTransitions(
            Builders,
            route,
            animation,
            secondaryAnimation,
            child);
    }

    public bool Equals(PageTransitionsTheme? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other) || ReferenceEquals(Builders, other.Builders))
        {
            return true;
        }

        foreach (TargetPlatform platform in Enum.GetValues<TargetPlatform>())
        {
            Builders.TryGetValue(platform, out PageTransitionsBuilder? left);
            other.Builders.TryGetValue(platform, out PageTransitionsBuilder? right);
            if (!Equals(left, right))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is PageTransitionsTheme other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (TargetPlatform platform in Enum.GetValues<TargetPlatform>())
        {
            Builders.TryGetValue(platform, out PageTransitionsBuilder? builder);
            hash.Add(builder);
        }

        return hash.ToHashCode();
    }

    private sealed class PageTransitionsThemeTransitions : StatefulWidget
    {
        public PageTransitionsThemeTransitions(
            IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> builders,
            PageRoute route,
            Animation<double> animation,
            Animation<double> secondaryAnimation,
            Widget child)
        {
            Builders = builders;
            Route = route;
            Animation = animation;
            SecondaryAnimation = secondaryAnimation;
            Child = child;
        }

        public IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> Builders { get; }

        public PageRoute Route { get; }

        public Animation<double> Animation { get; }

        public Animation<double> SecondaryAnimation { get; }

        public Widget Child { get; }

        public override State CreateState()
        {
            return new PageTransitionsThemeTransitionsState();
        }

        private sealed class PageTransitionsThemeTransitionsState : State
        {
            private TargetPlatform? _transitionPlatform;

            private PageTransitionsThemeTransitions CurrentWidget =>
                (PageTransitionsThemeTransitions)StateWidget;

            public override Widget Build(BuildContext context)
            {
                TargetPlatform platform = Theme.Of(context).Platform;
                if (CurrentWidget.Route.PopGestureInProgress)
                {
                    _transitionPlatform ??= platform;
                    platform = _transitionPlatform.Value;
                }
                else
                {
                    _transitionPlatform = null;
                }

                PageTransitionsBuilder builder = CurrentWidget.Builders.TryGetValue(
                    platform,
                    out PageTransitionsBuilder? matchingBuilder)
                    ? matchingBuilder
                    : platform == TargetPlatform.IOS
                        ? new CupertinoPageTransitionsBuilder()
                        : new ZoomPageTransitionsBuilder();
                return builder.BuildTransitions(
                    CurrentWidget.Route,
                    context,
                    CurrentWidget.Animation,
                    CurrentWidget.SecondaryAnimation,
                    CurrentWidget.Child);
            }
        }
    }
}

public sealed class MaterialPageRoute : PageRoute
{
    private readonly WidgetBuilder _builder;

    public MaterialPageRoute(
        WidgetBuilder builder,
        RouteSettings? settings = null,
        bool maintainState = true,
        bool fullscreenDialog = false,
        bool allowSnapshotting = true) : base(settings, fullscreenDialog)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        MaintainState = maintainState;
        AllowSnapshotting = allowSnapshotting;
    }

    public bool MaintainState { get; }

    public override bool AllowSnapshotting { get; }

    public override TimeSpan TransitionDuration => ResolveBuilder().TransitionDuration;

    public override TimeSpan ReverseTransitionDuration => ResolveBuilder().ReverseTransitionDuration;

    public override DelegatedTransitionBuilder DelegatedTransition => BuildDelegatedTransition;

    public override bool CanTransitionTo(TransitionRoute nextRoute)
    {
        bool nextRouteIsNotFullscreen = nextRoute is not PageRoute pageRoute || !pageRoute.FullscreenDialog;
        bool nextHasDelegatedTransition = nextRoute is ModalRoute modalRoute
                                          && modalRoute.DelegatedTransition is not null;
        return nextRouteIsNotFullscreen
               && (nextRoute is MaterialPageRoute || nextHasDelegatedTransition);
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
            child: _builder(context));
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return Theme.Of(context).PageTransitionsTheme.BuildTransitions(
            this,
            context,
            animation,
            secondaryAnimation,
            child);
    }

    private static Widget? BuildDelegatedTransition(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        bool allowSnapshotting,
        Widget? child)
    {
        DelegatedTransitionBuilder? transition = Theme.Of(context)
            .PageTransitionsTheme
            .DelegatedTransition(Theme.Of(context).Platform);
        return transition?.Invoke(context, animation, secondaryAnimation, allowSnapshotting, child);
    }

    private PageTransitionsBuilder ResolveBuilder()
    {
        NavigatorState? navigator = Navigator;
        ThemeData theme = navigator is null ? ThemeData.Light : Theme.Of(navigator.Context);
        return theme.PageTransitionsTheme.Resolve(theme.Platform);
    }
}

internal sealed class PredictiveBackGestureDetector : StatefulWidget
{
    public PredictiveBackGestureDetector(
        PageRoute route,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child,
        bool fullscreen,
        Color? fallbackColor)
    {
        Route = route;
        Animation = animation;
        SecondaryAnimation = secondaryAnimation;
        Child = child;
        Fullscreen = fullscreen;
        FallbackColor = fallbackColor;
    }

    public PageRoute Route { get; }

    public Animation<double> Animation { get; }

    public Animation<double> SecondaryAnimation { get; }

    public Widget Child { get; }

    public bool Fullscreen { get; }

    public Color? FallbackColor { get; }

    public override State CreateState()
    {
        return new PredictiveBackGestureDetectorState();
    }

    private sealed class PredictiveBackGestureDetectorState : State, WidgetsBindingObserver
    {
        private PredictiveBackPhase _phase;
        private PredictiveBackEvent? _startEvent;
        private PredictiveBackEvent? _currentEvent;
        private SwipeEdge _swipeEdge = SwipeEdge.Left;
        private double _lastGestureProgress;
        private Vector _lastGestureOffset;

        private PredictiveBackGestureDetector CurrentWidget =>
            (PredictiveBackGestureDetector)StateWidget;

        public override void InitState()
        {
            WidgetsBinding.Instance.AddObserver(this);
        }

        public bool HandleStartBackGesture(PredictiveBackEvent backEvent)
        {
            SetState(() => _phase = PredictiveBackPhase.Start);
            if (backEvent.IsButtonEvent
                || !CurrentWidget.Route.IsCurrent
                || !CurrentWidget.Route.PopGestureEnabled)
            {
                return false;
            }

            CurrentWidget.Route.HandleStartBackGesture(1.0 - backEvent.Progress);
            SetState(() =>
            {
                _startEvent = backEvent;
                _currentEvent = backEvent;
                _swipeEdge = backEvent.SwipeEdge;
                _lastGestureProgress = 0.0;
                _lastGestureOffset = default;
            });
            return true;
        }

        public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent)
        {
            CurrentWidget.Route.HandleUpdateBackGestureProgress(1.0 - backEvent.Progress);
            SetState(() =>
            {
                _phase = PredictiveBackPhase.Update;
                _currentEvent = backEvent;
                _swipeEdge = backEvent.SwipeEdge;
            });
        }

        public void HandleCommitBackGesture()
        {
            SaveGestureState();
            SetState(() =>
            {
                _phase = PredictiveBackPhase.Commit;
                _startEvent = null;
                _currentEvent = null;
            });
            CurrentWidget.Route.HandleCommitBackGesture();
        }

        public void HandleCancelBackGesture()
        {
            SaveGestureState();
            SetState(() =>
            {
                _phase = PredictiveBackPhase.Cancel;
                _startEvent = null;
                _currentEvent = null;
            });
            CurrentWidget.Route.HandleCancelBackGesture();
        }

        public override Widget Build(BuildContext context)
        {
            if (!CurrentWidget.Route.PopGestureInProgress)
            {
                _phase = PredictiveBackPhase.Idle;
                return CurrentWidget.Fullscreen
                    ? new ZoomPageTransitionsBuilder(backgroundColor: CurrentWidget.FallbackColor).BuildTransitions(
                        CurrentWidget.Route,
                        context,
                        CurrentWidget.Animation,
                        CurrentWidget.SecondaryAnimation,
                        CurrentWidget.Child)
                    : new FadeForwardsPageTransitionsBuilder(CurrentWidget.FallbackColor).BuildTransitions(
                        CurrentWidget.Route,
                        context,
                        CurrentWidget.Animation,
                        CurrentWidget.SecondaryAnimation,
                        CurrentWidget.Child);
            }

            return CurrentWidget.Fullscreen
                ? BuildFullscreenTransition(context)
                : BuildSharedElementTransition(context);
        }

        public override void Dispose()
        {
            WidgetsBinding.Instance.RemoveObserver(this);
        }

        private Widget BuildSharedElementTransition(BuildContext context)
        {
            Size screenSize = MediaQuery.SizeOf(context);
            BorderRadius displayRadius = MediaQuery.DisplayCornerRadiiOf(context) ?? BorderRadius.Zero;
            return new AnimatedBuilder(
                CurrentWidget.Animation,
                (_, child) =>
                {
                    double animationProgress = 1.0 - CurrentWidget.Animation.Value;
                    double progress;
                    double scale;
                    double opacity;
                    double radius;
                    Vector offset;
                    if (_phase == PredictiveBackPhase.Commit)
                    {
                        progress = Curves.EaseInOutCubicEmphasized(
                            Math.Clamp(animationProgress / (400.0 / 450.0), 0.0, 1.0));
                        double startScale = 1.0 - (0.10 * Curves.EaseInOutCubicEmphasized(
                            _lastGestureProgress));
                        scale = Lerp(startScale, 1.0, progress);
                        opacity = 1.0 - progress;
                        radius = Lerp(32.0 * _lastGestureProgress, 0.0, progress);
                        offset = Lerp(_lastGestureOffset, new Vector(screenSize.Height * 0.1, 0.0), progress);
                    }
                    else
                    {
                        progress = Curves.EaseInOutCubicEmphasized(animationProgress);
                        scale = Lerp(1.0, 0.90, progress);
                        opacity = 1.0;
                        radius = Lerp(0.0, 32.0, progress);
                        offset = CalculateGestureOffset(screenSize, progress);
                    }

                    BorderRadius effectiveRadius = displayRadius == BorderRadius.Zero
                        ? BorderRadius.Circular(radius)
                        : displayRadius;
                    Widget clipped = new ClipRRect(effectiveRadius, child: child);
                    Widget faded = new Opacity(opacity, clipped);
                    Widget translated = new Plumix.Widgets.Transform(
                        Matrix.CreateTranslation(offset.X, offset.Y),
                        child: faded);
                    return new ScaleTransition(
                        new ConstantAnimation<double>(scale),
                        child: translated);
                },
                CurrentWidget.Child);
        }

        private Widget BuildFullscreenTransition(BuildContext context)
        {
            double xShift = MediaQuery.WidthOf(context) / 20.0 - 8.0;
            BorderRadius radius = MediaQuery.DisplayCornerRadiiOf(context) ?? BorderRadius.Circular(32.0);
            Animation<double> listenable = CurrentWidget.Route.IsCurrent
                ? CurrentWidget.Animation
                : CurrentWidget.SecondaryAnimation;
            return new AnimatedBuilder(
                listenable,
                (_, child) =>
                {
                    bool isCurrent = CurrentWidget.Route.IsCurrent;
                    double value = listenable.Value;
                    double positionX;
                    double scale;
                    double opacity;
                    if (isCurrent)
                    {
                        double postCommit = Math.Clamp((value - 0.65) / 0.35, 0.0, 1.0);
                        positionX = Lerp(xShift, 0.0, postCommit);
                        scale = Lerp(0.95, 1.0, postCommit);
                        opacity = Lerp(0.95, 1.0, value);
                    }
                    else
                    {
                        positionX = Lerp(xShift, 0.0, value);
                        double preCommit = Math.Clamp(value / 0.65, 0.0, 1.0);
                        scale = Lerp(0.95, 1.0, preCommit);
                        opacity = value <= 0.65 ? Lerp(1.0, 0.95, preCommit) : 1.0;
                    }

                    double animatedOpacity = _phase == PredictiveBackPhase.Commit
                        || (isCurrent && value < 0.65)
                        ? 0.0
                        : 1.0;
                    Widget result = new ClipRRect(radius, child: child);
                    result = new AnimatedOpacity(
                        animatedOpacity,
                        TimeSpan.FromMilliseconds(100),
                        child: result);
                    result = new Opacity(opacity, result);
                    result = new ScaleTransition(new ConstantAnimation<double>(scale), child: result);
                    return new Plumix.Widgets.Transform(
                        Matrix.CreateTranslation(positionX, 0.0),
                        child: result);
                },
                CurrentWidget.Child);
        }

        private void SaveGestureState()
        {
            Size size = MediaQuery.SizeOf(Context);
            _lastGestureProgress = Math.Clamp(1.0 - CurrentWidget.Animation.Value, 0.0, 1.0);
            _lastGestureOffset = CalculateGestureOffset(
                size,
                Curves.EaseInOutCubicEmphasized(_lastGestureProgress));
        }

        private Vector CalculateGestureOffset(Size screenSize, double progress)
        {
            double xShift = screenSize.Width / 20.0 - 8.0;
            double x = (_swipeEdge == SwipeEdge.Left ? 1.0 : -1.0) * xShift * progress;
            if (_startEvent?.TouchOffset is not Point start
                || _currentEvent?.TouchOffset is not Point current
                || screenSize.Height <= 0.0)
            {
                if (_phase == PredictiveBackPhase.Cancel && _lastGestureProgress > 0.0)
                {
                    double factor = Math.Clamp(progress / _lastGestureProgress, 0.0, 1.0);
                    return new Vector(_lastGestureOffset.X * factor, _lastGestureOffset.Y * factor);
                }

                return new Vector(x, 0.0);
            }

            double yShiftMax = screenSize.Height / 20.0 - 8.0;
            double rawYShift = current.Y - start.Y;
            double normalized = Math.Clamp(Math.Abs(rawYShift) / screenSize.Height, 0.0, 1.0);
            double y = Curves.EaseOut(normalized) * Math.Sign(rawYShift) * yShiftMax;
            return new Vector(x, Math.Clamp(y, -yShiftMax, yShiftMax));
        }

        private static double Lerp(double begin, double end, double t)
        {
            return begin + ((end - begin) * Math.Clamp(t, 0.0, 1.0));
        }

        private static Vector Lerp(Vector begin, Vector end, double t)
        {
            double clamped = Math.Clamp(t, 0.0, 1.0);
            return new Vector(
                begin.X + ((end.X - begin.X) * clamped),
                begin.Y + ((end.Y - begin.Y) * clamped));
        }
    }

    private enum PredictiveBackPhase
    {
        Idle,
        Start,
        Update,
        Commit,
        Cancel,
    }
}

internal sealed class MappedAnimation<T> : Animation<T>
{
    private readonly Animation<double> _parent;
    private readonly Func<double, T> _transform;

    public MappedAnimation(Animation<double> parent, Func<double, T> transform)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    public override T Value => _transform(_parent.Value);

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener)
    {
        _parent.AddListener(listener);
    }

    public override void RemoveListener(Action listener)
    {
        _parent.RemoveListener(listener);
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        _parent.AddStatusListener(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _parent.RemoveStatusListener(listener);
    }
}
