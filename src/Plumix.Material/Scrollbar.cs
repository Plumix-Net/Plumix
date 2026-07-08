using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/scrollbar.dart

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
        var isAndroid = theme.Platform == TargetPlatform.Android;
        var isIos = theme.Platform == TargetPlatform.IOS;
        var onSurface = theme.OnSurfaceColor;

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
            var materialStates = ToMaterialState(state);
            var themed = scrollbarTheme.ThumbColor?.Resolve(materialStates);
            if (themed.HasValue) return themed;

            if (state.HasFlag(ScrollbarInteractionState.Dragged))
            {
                return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.60 : 0.75);
            }

            if (state.HasFlag(ScrollbarInteractionState.Hovered))
            {
                return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.50 : 0.65);
            }

            if (isAndroid) return theme.HighlightColor;
            return ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.10 : 0.30);
        }

        Color? ResolveTrackColor(ScrollbarInteractionState state)
        {
            return scrollbarTheme.TrackColor?.Resolve(ToMaterialState(state))
                   ?? ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.03 : 0.05);
        }

        Color? ResolveTrackBorderColor(ScrollbarInteractionState state)
        {
            return scrollbarTheme.TrackBorderColor?.Resolve(ToMaterialState(state))
                   ?? ApplyOpacity(onSurface, theme.Brightness == Brightness.Light ? 0.10 : 0.25);
        }

        bool? ResolveTrackVisibility(ScrollbarInteractionState state) =>
            TrackVisibility ?? scrollbarTheme.TrackVisibility?.Resolve(ToMaterialState(state)) ?? false;

        double? ResolveThickness(ScrollbarInteractionState state)
        {
            var resolved = Thickness ?? scrollbarTheme.Thickness?.Resolve(ToMaterialState(state));
            if (resolved.HasValue) return resolved;
            return state.HasFlag(ScrollbarInteractionState.Hovered) && ResolveTrackVisibility(state) == true
                ? 12
                : isAndroid ? 4 : 8;
        }

        return new RawScrollbar(
            child: Child,
            controller: Controller,
            thumbVisibility: ThumbVisibility,
            shape: null,
            radius: Radius ?? scrollbarTheme.Radius ?? (isAndroid ? 0 : 8),
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
                ThumbVisibility ?? scrollbarTheme.ThumbVisibility?.Resolve(ToMaterialState(state)) ?? false,
            trackVisibilityResolver: ResolveTrackVisibility,
            trackTapEnabled: true,
            interactionChanged: null);
    }

    private static MaterialState ToMaterialState(ScrollbarInteractionState state)
    {
        var result = MaterialState.None;
        if (state.HasFlag(ScrollbarInteractionState.Hovered)) result |= MaterialState.Hovered;
        if (state.HasFlag(ScrollbarInteractionState.Dragged)) result |= MaterialState.Dragged;
        return result;
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(255 * opacity), 0, 255), color.R, color.G, color.B);
}
