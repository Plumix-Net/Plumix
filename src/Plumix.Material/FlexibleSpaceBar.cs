using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/flexible_space_bar.dart

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

public sealed class FlexibleSpaceBar : StatefulWidget
{
    public FlexibleSpaceBar(
        Widget? title = null,
        Widget? background = null,
        bool? centerTitle = null,
        EdgeInsetsGeometry? titlePadding = null,
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
    public EdgeInsetsGeometry? TitlePadding { get; }
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

    public override State CreateState() => new FlexibleSpaceBarState();

    private static void ValidateInsets(EdgeInsetsGeometry? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        EdgeInsetsGeometry insets = value.Value;
        double[] values =
        [
            insets.Left,
            insets.Top,
            insets.Right,
            insets.Bottom,
            insets.Start,
            insets.End,
        ];
        if (values.Any(static inset => !double.IsFinite(inset) || inset < 0.0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

internal sealed class FlexibleSpaceBarState : State
{
    private FlexibleSpaceBar CurrentWidget => (FlexibleSpaceBar)StateWidget;

    public override Widget Build(BuildContext context)
    {
        return new LayoutBuilder((builderContext, constraints) => BuildWithConstraints(builderContext, constraints));
    }

    private Widget BuildWithConstraints(BuildContext context, BoxConstraints constraints)
    {
        FlexibleSpaceBar widget = CurrentWidget;
        var settings = FlexibleSpaceBarSettings.Of(context);
        double deltaExtent = Math.Max(0, settings.MaxExtent - settings.MinExtent);
        double t = deltaExtent <= 0.0001
            ? 0
            : Math.Clamp(1 - ((settings.CurrentExtent - settings.MinExtent) / deltaExtent), 0, 1);
        var children = new List<Widget>();

        if (widget.Background is not null)
        {
            double fadeStart = deltaExtent <= 0.0001 ? 1.0 : Math.Max(0.0, 1.0 - (56.0 / deltaExtent));
            double opacity = settings.MaxExtent == settings.MinExtent
                ? 1.0
                : 1.0 - TransformInterval(fadeStart, 1.0, t);
            double height = settings.MaxExtent;
            if (widget.StretchModes.Contains(StretchMode.ZoomBackground)
                && constraints.MaxHeight > height)
            {
                height = constraints.MaxHeight;
            }

            double top = GetCollapsePadding(t, settings, widget.CollapseMode);
            children.Add(new Positioned(
                top: top,
                left: 0,
                right: 0,
                height: height,
                child: new FlexibleSpaceHeaderOpacity(
                    opacity: opacity,
                    alwaysIncludeSemantics: true,
                    child: widget.Background)));

            if (widget.StretchModes.Contains(StretchMode.BlurBackground)
                && constraints.MaxHeight > settings.MaxExtent)
            {
                double blurAmount = (constraints.MaxHeight - settings.MaxExtent) / 10.0;
                children.Add(new Positioned(
                    left: 0.0,
                    top: 0.0,
                    right: 0.0,
                    bottom: 0.0,
                    child: new BackdropFilter(
                        filter: new ImageFilter.Blur(sigmaX: blurAmount, sigmaY: blurAmount),
                        child: new ColoredBox(Colors.Transparent))));
            }
        }

        if (widget.Title is not null)
        {
            var theme = Theme.Of(context);
            Widget title = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? widget.Title
                : new Semantics(namesRoute: true, child: widget.Title);
            if (widget.StretchModes.Contains(StretchMode.FadeTitle)
                && constraints.MaxHeight > settings.MaxExtent)
            {
                double stretchOpacity = 1.0 - Math.Clamp(
                    (constraints.MaxHeight - settings.MaxExtent) / 100.0,
                    0.0,
                    1.0);
                title = new Opacity(
                    opacity: stretchOpacity,
                    child: title);
            }

            double opacity = settings.ToolbarOpacity;
            if (opacity > 0.0)
            {
                TextStyle baseStyle = theme.UseMaterial3
                    ? theme.TextTheme.TitleLarge
                    : theme.PrimaryTextTheme.TitleLarge;
                Color titleColor = baseStyle.Color
                    ?? throw new InvalidOperationException("FlexibleSpaceBar title style requires a color.");
                TextStyle titleStyle = baseStyle.CopyWith(color: ApplyOpacity(titleColor, opacity));
                bool effectiveCenterTitle = GetEffectiveCenterTitle(widget, theme);
                double leadingPadding = (settings.HasLeading ?? true) ? 72.0 : 0.0;
                EdgeInsetsGeometry padding = widget.TitlePadding
                    ?? EdgeInsetsGeometry.DirectionalOnly(
                        start: effectiveCenterTitle ? 0.0 : leadingPadding,
                        bottom: 16.0);
                double scaleValue = widget.ExpandedTitleScale
                                    + ((1.0 - widget.ExpandedTitleScale) * t);
                Alignment titleAlignment = GetTitleAlignment(context, effectiveCenterTitle);
                Widget constrainedTitle = new DefaultTextStyle(
                    style: titleStyle,
                    child: new LayoutBuilder((_, titleConstraints) => new SizedBox(
                        width: titleConstraints.MaxWidth / scaleValue,
                        child: new Align(
                            alignment: titleAlignment,
                            child: title))));
                children.Add(new Padding(
                    insets: padding,
                    child: new Plumix.Widgets.Transform(
                        transform: Matrix4.Diagonal3Values(scaleValue, scaleValue, 1.0),
                        alignment: titleAlignment,
                        child: new Align(
                            alignment: titleAlignment,
                            child: constrainedTitle))));
            }
        }

        return new ClipRect(child: new Stack(children: children));
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

    private static bool GetEffectiveCenterTitle(FlexibleSpaceBar widget, ThemeData theme)
    {
        return widget.CenterTitle ?? theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
    }

    private static Alignment GetTitleAlignment(BuildContext context, bool effectiveCenterTitle)
    {
        if (effectiveCenterTitle)
        {
            return Alignment.BottomCenter;
        }

        return Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.BottomRight
            : Alignment.BottomLeft;
    }

    private static double GetCollapsePadding(
        double t,
        FlexibleSpaceBarSettings settings,
        CollapseMode collapseMode)
    {
        return collapseMode switch
        {
            CollapseMode.Pin => -(settings.MaxExtent - settings.CurrentExtent),
            CollapseMode.None => 0.0,
            CollapseMode.Parallax => -((settings.MaxExtent - settings.MinExtent) * t / 4.0),
            _ => throw new ArgumentOutOfRangeException(nameof(collapseMode)),
        };
    }

    private static double TransformInterval(double begin, double end, double t)
    {
        if (t <= begin)
        {
            return 0.0;
        }

        if (t >= end)
        {
            return 1.0;
        }

        return (t - begin) / (end - begin);
    }
}

internal sealed class FlexibleSpaceHeaderOpacity : SingleChildRenderObjectWidget
{
    public FlexibleSpaceHeaderOpacity(
        double opacity,
        bool alwaysIncludeSemantics,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity { get; }

    public bool AlwaysIncludeSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFlexibleSpaceHeaderOpacity(Opacity, AlwaysIncludeSemantics);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderFlexibleSpaceHeaderOpacity)renderObject;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
        opacity.Opacity = Opacity;
    }
}

internal sealed class RenderFlexibleSpaceHeaderOpacity : RenderOpacity
{
    public RenderFlexibleSpaceHeaderOpacity(
        double opacity,
        bool alwaysIncludeSemantics) : base(opacity, alwaysIncludeSemantics)
    {
    }

    public override bool IsRepaintBoundary => false;

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        int alpha = (int)Math.Round(Opacity * 255.0, MidpointRounding.AwayFromZero);
        if (alpha <= 0)
        {
            return;
        }

        Layer = context.PushOpacity(offset, alpha, base.Paint, Layer as OpacityLayer);
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
        if (!double.IsFinite(currentExtent) || currentExtent < minExtent || currentExtent > maxExtent)
        {
            throw new ArgumentOutOfRangeException(nameof(currentExtent));
        }
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
