using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/flexible_space_bar.dart

public enum CollapseMode
{
    Parallax,
    Pin,
    None,
}

public enum StretchMode
{
    ZoomBackground,
    BlurBackground,
    FadeTitle,
}

public sealed class FlexibleSpaceBar : StatelessWidget
{
    public FlexibleSpaceBar(
        Widget? title = null,
        Widget? background = null,
        bool? centerTitle = null,
        Thickness? titlePadding = null,
        CollapseMode collapseMode = CollapseMode.Parallax,
        IReadOnlyList<StretchMode>? stretchModes = null,
        double expandedTitleScale = 1.5,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(expandedTitleScale) || expandedTitleScale < 1)
            throw new ArgumentOutOfRangeException(nameof(expandedTitleScale));
        ValidateInsets(titlePadding, nameof(titlePadding));
        Title = title;
        Background = background;
        CenterTitle = centerTitle;
        TitlePadding = titlePadding;
        CollapseMode = collapseMode;
        StretchModes = stretchModes ?? [StretchMode.ZoomBackground];
        ExpandedTitleScale = expandedTitleScale;
    }

    public Widget? Title { get; }
    public Widget? Background { get; }
    public bool? CenterTitle { get; }
    public Thickness? TitlePadding { get; }
    public CollapseMode CollapseMode { get; }
    public IReadOnlyList<StretchMode> StretchModes { get; }
    public double ExpandedTitleScale { get; }

    public static Widget CreateSettings(
        double currentExtent,
        Widget child,
        double? toolbarOpacity = null,
        double? minExtent = null,
        double? maxExtent = null,
        bool? isScrolledUnder = null,
        bool? hasLeading = null) => new FlexibleSpaceBarSettings(
        toolbarOpacity ?? 1,
        minExtent ?? currentExtent,
        maxExtent ?? currentExtent,
        currentExtent,
        isScrolledUnder,
        hasLeading,
        child);

    public override Widget Build(BuildContext context)
    {
        var settings = FlexibleSpaceBarSettings.Of(context);
        var deltaExtent = Math.Max(0, settings.MaxExtent - settings.MinExtent);
        var t = deltaExtent <= 0.0001
            ? 0
            : Math.Clamp(1 - ((settings.CurrentExtent - settings.MinExtent) / deltaExtent), 0, 1);
        var children = new List<Widget>();

        if (Background is not null)
        {
            var fadeStart = deltaExtent <= 0.0001 ? 1 : Math.Max(0, 1 - (56 / deltaExtent));
            var fadeProgress = t <= fadeStart ? 0 : Math.Clamp((t - fadeStart) / Math.Max(0.0001, 1 - fadeStart), 0, 1);
            var opacity = deltaExtent <= 0.0001 ? 1 : 1 - fadeProgress;
            var height = Math.Max(settings.MaxExtent, settings.CurrentExtent);
            var top = CollapseMode switch
            {
                CollapseMode.Pin => -(settings.MaxExtent - settings.CurrentExtent),
                CollapseMode.None => 0,
                _ => -(deltaExtent * t / 4),
            };
            children.Add(new Positioned(
                top: top,
                left: 0,
                right: 0,
                height: height,
                child: new Opacity(opacity, Background)));
        }

        if (Title is not null && settings.ToolbarOpacity > 0)
        {
            var theme = Theme.Of(context);
            var center = CenterTitle ?? theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
            var direction = Directionality.Of(context);
            var alignment = center
                ? Alignment.BottomCenter
                : direction == TextDirection.Ltr ? Alignment.BottomLeft : Alignment.BottomRight;
            var leadingPadding = settings.HasLeading ?? true ? 72.0 : 0.0;
            var padding = TitlePadding ?? (center
                ? new Thickness(0, 0, 0, 16)
                : direction == TextDirection.Ltr
                    ? new Thickness(leadingPadding, 0, 0, 16)
                    : new Thickness(0, 0, leadingPadding, 16));
            var style = (theme.UseMaterial3 ? theme.TextTheme.TitleLarge : theme.PrimaryTextTheme.TitleLarge)
                .CopyWith(color: ApplyOpacity(
                    (theme.UseMaterial3 ? theme.TextTheme.TitleLarge : theme.PrimaryTextTheme.TitleLarge).Color
                    ?? theme.OnSurfaceColor,
                    settings.ToolbarOpacity));
            var scale = ExpandedTitleScale + ((1 - ExpandedTitleScale) * t);
            Widget title = new Semantics(
                namesRoute: theme.Platform is not (TargetPlatform.IOS or TargetPlatform.MacOS),
                child: new DefaultTextStyle(style, Title));
            if (StretchModes.Contains(StretchMode.FadeTitle) && settings.CurrentExtent > settings.MaxExtent)
            {
                title = new Opacity(
                    1 - Math.Clamp((settings.CurrentExtent - settings.MaxExtent) / 100, 0, 1),
                    title);
            }
            title = new Plumix.Widgets.Transform(
                Matrix.CreateScale(scale, scale),
                alignment: alignment,
                child: new Align(alignment: alignment, child: title));
            children.Add(new Positioned(
                left: padding.Left,
                top: padding.Top,
                right: padding.Right,
                bottom: padding.Bottom,
                child: title));
        }

        return new ClipRect(child: new Stack(fit: StackFit.Expand, children: children));
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var v = value.Value;
        if (!double.IsFinite(v.Left) || !double.IsFinite(v.Top) || !double.IsFinite(v.Right) || !double.IsFinite(v.Bottom)
            || v.Left < 0 || v.Top < 0 || v.Right < 0 || v.Bottom < 0)
            throw new ArgumentOutOfRangeException(name);
    }
}

public sealed class FlexibleSpaceBarSettings : InheritedWidget
{
    public FlexibleSpaceBarSettings(
        double toolbarOpacity,
        double minExtent,
        double maxExtent,
        double currentExtent,
        bool? isScrolledUnder,
        bool? hasLeading,
        Widget child,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(toolbarOpacity) || toolbarOpacity < 0 || toolbarOpacity > 1)
            throw new ArgumentOutOfRangeException(nameof(toolbarOpacity));
        if (!double.IsFinite(minExtent) || minExtent < 0) throw new ArgumentOutOfRangeException(nameof(minExtent));
        if (!double.IsFinite(maxExtent) || maxExtent < minExtent) throw new ArgumentOutOfRangeException(nameof(maxExtent));
        if (!double.IsFinite(currentExtent) || currentExtent < 0) throw new ArgumentOutOfRangeException(nameof(currentExtent));
        ToolbarOpacity = toolbarOpacity;
        MinExtent = minExtent;
        MaxExtent = maxExtent;
        CurrentExtent = currentExtent;
        IsScrolledUnder = isScrolledUnder;
        HasLeading = hasLeading;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public double ToolbarOpacity { get; }
    public double MinExtent { get; }
    public double MaxExtent { get; }
    public double CurrentExtent { get; }
    public bool? IsScrolledUnder { get; }
    public bool? HasLeading { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => !EqualsSettings((FlexibleSpaceBarSettings)oldWidget);
    public static FlexibleSpaceBarSettings Of(BuildContext context) =>
        context.DependOnInherited<FlexibleSpaceBarSettings>()
        ?? throw new InvalidOperationException("FlexibleSpaceBar requires FlexibleSpaceBarSettings.");

    private bool EqualsSettings(FlexibleSpaceBarSettings other) =>
        ToolbarOpacity.Equals(other.ToolbarOpacity)
        && MinExtent.Equals(other.MinExtent)
        && MaxExtent.Equals(other.MaxExtent)
        && CurrentExtent.Equals(other.CurrentExtent)
        && IsScrolledUnder == other.IsScrolledUnder
        && HasLeading == other.HasLeading;
}
