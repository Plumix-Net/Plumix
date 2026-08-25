using Avalonia.Media;
using Plumix.Foundation;
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
        minThumbLength: KScrollbarMinLength,
        minOverscrollLength: KScrollbarMinOverscrollLength,
        fadeDuration: KScrollbarFadeDuration,
        timeToFade: KScrollbarTimeToFade,
        pressDuration: KScrollbarPressDuration,
        notificationPredicate: notificationPredicate ?? DefaultScrollNotificationPredicate,
        scrollbarOrientation: scrollbarOrientation,
        mainAxisMargin: mainAxisMargin,
        crossAxisMargin: KScrollbarCrossAxisMargin,
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

        private double Thickness =>
            CurrentWidget.Thickness!.Value +
            (_thicknessAnimationController.Evaluate() *
             (CurrentWidget.ThicknessWhileDragging - CurrentWidget.Thickness!.Value));

        private double Radius =>
            CurrentWidget.Radius!.Value +
            (_thicknessAnimationController.Evaluate() *
             (CurrentWidget.RadiusWhileDragging - CurrentWidget.Radius!.Value));

        public override void InitState()
        {
            base.InitState();
            _thicknessAnimationController = new AnimationController(
                duration: KScrollbarResizeDuration,
                vsync: this);
            _thicknessAnimationController.Changed += UpdateScrollbarPainter;
        }

        public override void Dispose()
        {
            _thicknessAnimationController.Changed -= UpdateScrollbarPainter;
            _thicknessAnimationController.Dispose();
            base.Dispose();
        }

        protected override double ResolveThickness(ScrollbarInteractionState states) => Thickness;

        protected override double ResolveRadius(ScrollbarInteractionState states) => Radius;

        protected override Color ResolveThumbColor(ScrollbarInteractionState states) =>
            CupertinoDynamicColor.Resolve(KScrollbarColor, Context);

        // On iOS, tapping the track does not page towards the position of the tap.
        protected override bool ResolveTrackTapEnabled() =>
            ScrollConfiguration.Of(Context).GetPlatform(Context) != TargetPlatform.IOS;

        protected override void InteractionStateChanged(
            ScrollbarInteractionState oldValue,
            ScrollbarInteractionState newValue)
        {
            base.InteractionStateChanged(oldValue, newValue);
            bool wasDragged = oldValue.HasFlag(ScrollbarInteractionState.Dragged);
            bool isDragged = newValue.HasFlag(ScrollbarInteractionState.Dragged);
            if (wasDragged == isDragged)
            {
                return;
            }

            if (isDragged)
            {
                // HandleThumbPress: grow to the dragging thickness, then buzz once.
                _thicknessAnimationController.Forward(0).WhenComplete(() => HapticFeedback.MediumImpact());
            }
            else
            {
                // HandleThumbPressEnd: shrink back before the drag is handed to the scroll position.
                _thicknessAnimationController.Reverse();
            }
        }

        protected override void ThumbDragEnded(bool didDrag, double primaryVelocity)
        {
            base.ThumbDragEnded(didDrag, primaryVelocity);
            if (LastPointerAxisOffset != ThumbPressStartAxisOffset && Math.Abs(primaryVelocity) < 10)
            {
                HapticFeedback.MediumImpact();
            }
        }

        private void UpdateScrollbarPainter()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }
}
