using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/scrollbar.dart

/// <summary>An iOS style scrollbar.</summary>
/// <remarks>
/// To add a scrollbar to a scroll view, wrap the scroll view widget in a
/// <see cref="CupertinoScrollbar"/>. When dragging the thumb, the thickness and radius animate from
/// <see cref="RawScrollbar.Thickness"/> and <see cref="RawScrollbar.Radius"/> to
/// <see cref="ThicknessWhileDragging"/> and <see cref="RadiusWhileDragging"/>.
/// </remarks>
public sealed class CupertinoScrollbar : RawScrollbar
{
    // All values eyeballed.
    private const double KScrollbarMinLength = 36.0;
    private const double KScrollbarMinOverscrollLength = 8.0;

    // This is the amount of space from the top of a vertical scrollbar to the top edge of the
    // scrollable, measured when the vertical scrollbar overscrolls to the top.
    private const double KScrollbarMainAxisMargin = 3.0;
    private const double KScrollbarCrossAxisMargin = 3.0;

    private static readonly TimeSpan KScrollbarTimeToFade = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan KScrollbarFadeDuration = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan KScrollbarResizeDuration = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan KScrollbarPressDuration = TimeSpan.FromMilliseconds(100);

    // Extracted from iOS 13.1 beta using Debug View Hierarchy.
    internal static readonly CupertinoDynamicColor KScrollbarColor = CupertinoDynamicColor.WithBrightness(
        color: Color.FromArgb(0x59, 0x00, 0x00, 0x00),
        darkColor: Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

    /// <summary>Creates an iOS style scrollbar that wraps the given <paramref name="child"/>.</summary>
    public CupertinoScrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        double thickness = DefaultThickness,
        double thicknessWhileDragging = DefaultThicknessWhileDragging,
        double radius = DefaultRadius,
        double radiusWhileDragging = DefaultRadiusWhileDragging,
        ScrollNotificationPredicate? notificationPredicate = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        double mainAxisMargin = KScrollbarMainAxisMargin,
        Key? key = null) : base(
        child: child,
        controller: controller,
        thumbVisibility: thumbVisibility ?? false,
        radius: radius,
        thickness: thickness,
        fadeDuration: KScrollbarFadeDuration,
        timeToFade: KScrollbarTimeToFade,
        pressDuration: KScrollbarPressDuration,
        notificationPredicate: notificationPredicate ?? DefaultScrollNotificationPredicate,
        scrollbarOrientation: scrollbarOrientation,
        mainAxisMargin: mainAxisMargin,
        key: key)
    {
        if (!double.IsFinite(thicknessWhileDragging) || thicknessWhileDragging <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thicknessWhileDragging));
        }

        if (!double.IsFinite(radiusWhileDragging) || radiusWhileDragging < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusWhileDragging));
        }

        ThicknessWhileDragging = thicknessWhileDragging;
        RadiusWhileDragging = radiusWhileDragging;
    }

    /// <summary>Default value for <see cref="RawScrollbar.Thickness"/>.</summary>
    public const double DefaultThickness = 3;

    /// <summary>Default value for <see cref="ThicknessWhileDragging"/>.</summary>
    public const double DefaultThicknessWhileDragging = 8.0;

    /// <summary>Default value for <see cref="RawScrollbar.Radius"/>.</summary>
    public const double DefaultRadius = 1.5;

    /// <summary>Default value for <see cref="RadiusWhileDragging"/>.</summary>
    public const double DefaultRadiusWhileDragging = 4.0;

    /// <summary>The thickness of the scrollbar while it is being dragged by the user.</summary>
    public double ThicknessWhileDragging { get; }

    /// <summary>The radius of the scrollbar edges while it is being dragged by the user.</summary>
    public double RadiusWhileDragging { get; }

    public override State CreateState() => new CupertinoScrollbarState();

    private sealed class CupertinoScrollbarState : RawScrollbarState<CupertinoScrollbar>
    {
        private AnimationController _thicknessAnimationController = null!;
        private double _pressStartAxisPosition;

        private double Thickness =>
            CurrentWidget.Thickness!.Value +
            (_thicknessAnimationController.Value *
             (CurrentWidget.ThicknessWhileDragging - CurrentWidget.Thickness!.Value));

        private double Radius =>
            CurrentWidget.Radius!.Value +
            (_thicknessAnimationController.Value *
             (CurrentWidget.RadiusWhileDragging - CurrentWidget.Radius!.Value));

        public override void InitState()
        {
            base.InitState();
            _thicknessAnimationController = new AnimationController(
                duration: KScrollbarResizeDuration,
                vsync: this);
            _thicknessAnimationController.Changed += HandleThicknessTick;
        }

        protected override void UpdateScrollbarPainter()
        {
            ScrollbarPainter.Color = CupertinoDynamicColor.Resolve(KScrollbarColor, Context);
            ScrollbarPainter.TextDirection = Directionality.Of(Context);
            ScrollbarPainter.Thickness = Thickness;
            ScrollbarPainter.MainAxisMargin = CurrentWidget.MainAxisMargin;
            ScrollbarPainter.CrossAxisMargin = KScrollbarCrossAxisMargin;
            ScrollbarPainter.Radius = Radius;
            ScrollbarPainter.Padding = MediaQuery.MaybePaddingOf(Context) ?? default;
            ScrollbarPainter.MinLength = KScrollbarMinLength;
            ScrollbarPainter.MinOverscrollLength = KScrollbarMinOverscrollLength;
            ScrollbarPainter.ScrollbarOrientation = CurrentWidget.ScrollbarOrientation;
        }

        // Thumb drag event callbacks handle the gesture where the user presses on the scrollbar
        // thumb and then drags the scrollbar without releasing.

        protected override void HandleThumbPressStart(Point localPosition)
        {
            base.HandleThumbPressStart(localPosition);
            Axis? direction = GetScrollbarDirection();
            if (direction is null)
            {
                return;
            }

            _pressStartAxisPosition = direction == Axis.Vertical ? localPosition.Y : localPosition.X;
        }

        protected override void HandleThumbPress()
        {
            if (GetScrollbarDirection() is null)
            {
                return;
            }

            base.HandleThumbPress();
            _thicknessAnimationController.Forward().WhenComplete(() => HapticFeedback.MediumImpact());
        }

        protected override void HandleThumbPressEnd(Point localPosition, Velocity velocity)
        {
            Axis? direction = GetScrollbarDirection();
            if (direction is null)
            {
                return;
            }

            _thicknessAnimationController.Reverse();
            base.HandleThumbPressEnd(localPosition, velocity);
            double axisPosition = direction == Axis.Horizontal ? localPosition.X : localPosition.Y;
            double axisVelocity = direction == Axis.Horizontal
                ? velocity.PixelsPerSecond.X
                : velocity.PixelsPerSecond.Y;
            if (axisPosition != _pressStartAxisPosition && Math.Abs(axisVelocity) < 10)
            {
                HapticFeedback.MediumImpact();
            }
        }

        protected override void HandleTrackTapDown(PointerDownEvent details)
        {
            // On iOS, tapping the track does not page towards the position of the tap.
            if (ScrollConfiguration.Of(Context).GetPlatform(Context) != TargetPlatform.IOS)
            {
                base.HandleTrackTapDown(details);
            }
        }

        public override void Dispose()
        {
            _thicknessAnimationController.Changed -= HandleThicknessTick;
            _thicknessAnimationController.Dispose();
            base.Dispose();
        }

        private void HandleThicknessTick()
        {
            if (Mounted)
            {
                UpdateScrollbarPainter();
            }
        }
    }
}
