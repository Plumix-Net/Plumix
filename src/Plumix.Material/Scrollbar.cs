using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scrollbar.dart

public sealed class Scrollbar : StatelessWidget
{
    public Scrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        bool? trackVisibility = null,
        double? thickness = null,
        double? radius = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        bool? interactive = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        Key? key = null) : base(key)
    {
        if (thickness.HasValue && (!double.IsFinite(thickness.Value) || thickness.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(thickness));
        }

        if (radius.HasValue && (!double.IsFinite(radius.Value) || radius.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        TrackVisibility = trackVisibility;
        Thickness = thickness;
        Radius = radius;
        NotificationPredicate = notificationPredicate;
        Interactive = interactive;
        ScrollbarOrientation = scrollbarOrientation;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public bool? TrackVisibility { get; }
    public double? Thickness { get; }
    public double? Radius { get; }
    public ScrollNotificationPredicate? NotificationPredicate { get; }
    public bool? Interactive { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var scrollbarTheme = ScrollbarTheme.Of(context);
        bool isAndroid = theme.Platform == TargetPlatform.Android;
        bool isIos = theme.Platform == TargetPlatform.IOS;
        Color onSurface = theme.ColorScheme.OnSurface;

        if (isIos)
        {
            return new CupertinoScrollbar(
                child: Child,
                controller: Controller,
                thumbVisibility: ThumbVisibility,
                thickness: Thickness ?? CupertinoScrollbar.DefaultThickness,
                thicknessWhileDragging: Thickness ?? CupertinoScrollbar.DefaultThicknessWhileDragging,
                radius: Radius ?? CupertinoScrollbar.DefaultRadius,
                radiusWhileDragging: Radius ?? CupertinoScrollbar.DefaultRadiusWhileDragging,
                notificationPredicate: NotificationPredicate,
                scrollbarOrientation: ScrollbarOrientation);
        }

        Color? ResolveThumbColor(ScrollbarInteractionState state)
        {
            var widgetStates = ToWidgetStates(state);
            var themed = scrollbarTheme.ThumbColor?.Resolve(widgetStates);
            if (themed.HasValue) return themed;

            if (state.HasFlag(ScrollbarInteractionState.Dragged))
            {
                return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.60 : 0.75);
            }

            if (ResolveTrackVisibility(state) == true || state.HasFlag(ScrollbarInteractionState.Hovered))
            {
                return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.50 : 0.65);
            }

            if (isAndroid) return theme.HighlightColor;
            return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.10 : 0.30);
        }

        Color? ResolveTrackColor(ScrollbarInteractionState state)
        {
            return scrollbarTheme.TrackColor?.Resolve(ToWidgetStates(state))
                   ?? ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.03 : 0.05);
        }

        Color? ResolveTrackBorderColor(ScrollbarInteractionState state)
        {
            return scrollbarTheme.TrackBorderColor?.Resolve(ToWidgetStates(state))
                   ?? ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.10 : 0.25);
        }

        bool? ResolveTrackVisibility(ScrollbarInteractionState state) =>
            TrackVisibility ?? scrollbarTheme.TrackVisibility?.Resolve(ToWidgetStates(state)) ?? false;

        double? ResolveThickness(ScrollbarInteractionState state)
        {
            double? resolved = Thickness ?? scrollbarTheme.Thickness?.Resolve(ToWidgetStates(state));
            if (resolved.HasValue) return resolved;
            return state.HasFlag(ScrollbarInteractionState.Hovered) && ResolveTrackVisibility(state) == true
                ? 12
                : isAndroid ? 4 : 8;
        }

        return new _MaterialScrollbar(
            child: Child,
            controller: Controller,
            thumbVisibility: ThumbVisibility,
            shape: null,
            radius: Radius ?? scrollbarTheme.Radius ?? (isAndroid ? null : 8),
            thickness: Thickness,
            thumbColor: null,
            minThumbLength: scrollbarTheme.MinThumbLength ?? 48,
            minOverscrollLength: null,
            trackVisibility: TrackVisibility,
            trackRadius: null,
            trackColor: null,
            trackBorderColor: null,
            fadeDuration: TimeSpan.FromMilliseconds(300),
            timeToFade: TimeSpan.FromMilliseconds(600),
            pressDuration: TimeSpan.Zero,
            notificationPredicate: NotificationPredicate,
            interactive: Interactive ?? scrollbarTheme.Interactive ?? !isAndroid,
            scrollbarOrientation: ScrollbarOrientation,
            mainAxisMargin: scrollbarTheme.MainAxisMargin ?? 0,
            crossAxisMargin: scrollbarTheme.CrossAxisMargin ?? (isAndroid ? 0 : 2),
            padding: null,
            thumbColorResolver: ResolveThumbColor,
            trackColorResolver: ResolveTrackColor,
            trackBorderColorResolver: ResolveTrackBorderColor,
            thicknessResolver: ResolveThickness,
            radiusResolver: null,
            thumbVisibilityResolver: state =>
                ThumbVisibility ?? scrollbarTheme.ThumbVisibility?.Resolve(ToWidgetStates(state)) ?? false,
            trackVisibilityResolver: ResolveTrackVisibility,
            trackTapEnabled: true,
            interactionChanged: null);
    }

    private sealed class _MaterialScrollbar : RawScrollbar
    {
        public _MaterialScrollbar(
            Widget child,
            ScrollController? controller,
            bool? thumbVisibility,
            ShapeBorder? shape,
            double? radius,
            double? thickness,
            Color? thumbColor,
            double minThumbLength,
            double? minOverscrollLength,
            bool? trackVisibility,
            double? trackRadius,
            Color? trackColor,
            Color? trackBorderColor,
            TimeSpan? fadeDuration,
            TimeSpan? timeToFade,
            TimeSpan? pressDuration,
            ScrollNotificationPredicate? notificationPredicate,
            bool? interactive,
            ScrollbarOrientation? scrollbarOrientation,
            double mainAxisMargin,
            double crossAxisMargin,
            Thickness? padding,
            Func<ScrollbarInteractionState, Color?>? thumbColorResolver,
            Func<ScrollbarInteractionState, Color?>? trackColorResolver,
            Func<ScrollbarInteractionState, Color?>? trackBorderColorResolver,
            Func<ScrollbarInteractionState, double?>? thicknessResolver,
            Func<ScrollbarInteractionState, double?>? radiusResolver,
            Func<ScrollbarInteractionState, bool?>? thumbVisibilityResolver,
            Func<ScrollbarInteractionState, bool?>? trackVisibilityResolver,
            bool trackTapEnabled,
            Action<ScrollbarInteractionState>? interactionChanged,
            Key? key = null) : base(
            child,
            controller,
            thumbVisibility,
            shape,
            radius,
            thickness,
            thumbColor,
            minThumbLength,
            minOverscrollLength,
            trackVisibility,
            trackRadius,
            trackColor,
            trackBorderColor,
            fadeDuration,
            timeToFade,
            pressDuration,
            notificationPredicate,
            interactive,
            scrollbarOrientation,
            mainAxisMargin,
            crossAxisMargin,
            padding,
            thumbColorResolver,
            trackColorResolver,
            trackBorderColorResolver,
            thicknessResolver,
            radiusResolver,
            thumbVisibilityResolver,
            trackVisibilityResolver,
            trackTapEnabled,
            interactionChanged,
            key)
        {
        }

        public override State CreateState() => new _MaterialScrollbarState();
    }

    private sealed class _MaterialScrollbarState : RawScrollbarState<_MaterialScrollbar>
    {
        private AnimationController _hoverAnimationController = null!;

        public override void InitState()
        {
            base.InitState();
            _hoverAnimationController = new AnimationController(duration: TimeSpan.FromMilliseconds(200), vsync: this);
            _hoverAnimationController.Changed += HandleHoverAnimationChanged;
        }

        public override void Dispose()
        {
            _hoverAnimationController.Changed -= HandleHoverAnimationChanged;
            _hoverAnimationController.Dispose();
            base.Dispose();
        }

        protected override Color ResolveThumbColor(ScrollbarInteractionState states)
        {
            if (states.HasFlag(ScrollbarInteractionState.Dragged) || ResolveTrackVisibility(states))
            {
                return base.ResolveThumbColor(states);
            }

            Color idleColor = base.ResolveThumbColor(states & ~ScrollbarInteractionState.Hovered);
            Color hoverColor = base.ResolveThumbColor(states | ScrollbarInteractionState.Hovered);
            return new ColorTween().Evaluate(_hoverAnimationController.Evaluate(), idleColor, hoverColor);
        }

        protected override void InteractionStateChanged(
            ScrollbarInteractionState oldValue,
            ScrollbarInteractionState newValue)
        {
            base.InteractionStateChanged(oldValue, newValue);
            bool wasHovered = oldValue.HasFlag(ScrollbarInteractionState.Hovered);
            bool isHovered = newValue.HasFlag(ScrollbarInteractionState.Hovered);
            if (wasHovered == isHovered)
            {
                return;
            }

            if (isHovered)
            {
                _hoverAnimationController.Forward();
            }
            else
            {
                _hoverAnimationController.Reverse();
            }
        }

        private void HandleHoverAnimationChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }

    private static IReadOnlySet<WidgetState> ToWidgetStates(ScrollbarInteractionState state)
    {
        var result = new HashSet<WidgetState>();
        if (state.HasFlag(ScrollbarInteractionState.Hovered)) result.Add(WidgetState.Hovered);
        if (state.HasFlag(ScrollbarInteractionState.Dragged)) result.Add(WidgetState.Dragged);
        return result;
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(255 * opacity), 0, 255), color.R, color.G, color.B);
}
