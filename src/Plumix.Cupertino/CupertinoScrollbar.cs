using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/scrollbar.dart

public sealed class CupertinoScrollbar : StatelessWidget
{
    public const double DefaultThickness = 3;
    public const double DefaultThicknessWhileDragging = 8;
    public const double DefaultRadius = 1.5;
    public const double DefaultRadiusWhileDragging = 4;

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
        double mainAxisMargin = 3,
        Key? key = null) : base(key)
    {
        ValidatePositive(nameof(thickness), thickness);
        ValidatePositive(nameof(thicknessWhileDragging), thicknessWhileDragging);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(radiusWhileDragging), radiusWhileDragging);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        Thickness = thickness;
        ThicknessWhileDragging = thicknessWhileDragging;
        Radius = radius;
        RadiusWhileDragging = radiusWhileDragging;
        NotificationPredicate = notificationPredicate;
        ScrollbarOrientation = scrollbarOrientation;
        MainAxisMargin = mainAxisMargin;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public double Thickness { get; }
    public double ThicknessWhileDragging { get; }
    public double Radius { get; }
    public double RadiusWhileDragging { get; }
    public ScrollNotificationPredicate? NotificationPredicate { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }
    public double MainAxisMargin { get; }

    public override Widget Build(BuildContext context)
    {
        PlatformBrightness brightness = MediaQuery.MaybeOf(context)?.PlatformBrightness
                                        ?? PlatformBrightness.Light;
        Color thumbColor = brightness == PlatformBrightness.Dark
            ? Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x59, 0, 0, 0);
        return new _CupertinoScrollbar(
            child: Child,
            controller: Controller,
            thumbVisibility: ThumbVisibility ?? false,
            thickness: Thickness,
            thicknessWhileDragging: ThicknessWhileDragging,
            radius: Radius,
            radiusWhileDragging: RadiusWhileDragging,
            thumbColor: thumbColor,
            notificationPredicate: NotificationPredicate,
            scrollbarOrientation: ScrollbarOrientation,
            mainAxisMargin: MainAxisMargin);
    }

    private sealed class _CupertinoScrollbar : RawScrollbar
    {
        public _CupertinoScrollbar(
            Widget child,
            ScrollController? controller,
            bool thumbVisibility,
            double thickness,
            double thicknessWhileDragging,
            double radius,
            double radiusWhileDragging,
            Color thumbColor,
            ScrollNotificationPredicate? notificationPredicate,
            ScrollbarOrientation? scrollbarOrientation,
            double mainAxisMargin) : base(
            child: child,
            controller: controller,
            thumbVisibility: thumbVisibility,
            shape: null,
            radius: null,
            thickness: null,
            thumbColor: thumbColor,
            minThumbLength: 36,
            minOverscrollLength: 8,
            trackVisibility: false,
            trackRadius: null,
            trackColor: null,
            trackBorderColor: null,
            fadeDuration: TimeSpan.FromMilliseconds(250),
            timeToFade: TimeSpan.FromMilliseconds(1200),
            pressDuration: TimeSpan.FromMilliseconds(100),
            notificationPredicate: notificationPredicate,
            interactive: true,
            scrollbarOrientation: scrollbarOrientation,
            mainAxisMargin: mainAxisMargin,
            crossAxisMargin: 3,
            padding: null,
            thumbColorResolver: null,
            trackColorResolver: null,
            trackBorderColorResolver: null,
            thicknessResolver: null,
            radiusResolver: null,
            thumbVisibilityResolver: null,
            trackVisibilityResolver: null,
            trackTapEnabled: false,
            interactionChanged: null)
        {
            IdleThickness = thickness;
            DragThickness = thicknessWhileDragging;
            IdleRadius = radius;
            DragRadius = radiusWhileDragging;
        }

        public double IdleThickness { get; }

        public double DragThickness { get; }

        public double IdleRadius { get; }

        public double DragRadius { get; }

        public override State CreateState() => new _CupertinoScrollbarState();
    }

    private sealed class _CupertinoScrollbarState : RawScrollbarState<_CupertinoScrollbar>
    {
        private AnimationController _resizeController = null!;

        public override void InitState()
        {
            base.InitState();
            _resizeController = new AnimationController(TimeSpan.FromMilliseconds(100), this);
            _resizeController.Changed += HandleResizeChanged;
        }

        public override void Dispose()
        {
            _resizeController.Changed -= HandleResizeChanged;
            _resizeController.Dispose();
            base.Dispose();
        }

        protected override double ResolveThickness(ScrollbarInteractionState states)
        {
            return Lerp(CurrentWidget.IdleThickness, CurrentWidget.DragThickness, _resizeController.Evaluate());
        }

        protected override double ResolveRadius(ScrollbarInteractionState states)
        {
            return Lerp(CurrentWidget.IdleRadius, CurrentWidget.DragRadius, _resizeController.Evaluate());
        }

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
                HapticFeedback.MediumImpact();
                global::Plumix.Scheduler.AddPostFrameCallback(_ =>
                {
                    if (Mounted && IsDragged)
                    {
                        _resizeController.Forward(0);
                    }
                });
            }
            else
            {
                _resizeController.Reverse();
            }
        }

        protected override void ThumbDragEnded(bool didDrag, double primaryVelocity)
        {
            base.ThumbDragEnded(didDrag, primaryVelocity);
            if (didDrag && Math.Abs(primaryVelocity) < 10)
            {
                HapticFeedback.MediumImpact();
            }
        }

        private void HandleResizeChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
    }

    private static void ValidatePositive(string name, double value)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateNonNegative(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}
