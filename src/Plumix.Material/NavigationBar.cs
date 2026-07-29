using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_bar.dart

public enum NavigationDestinationLabelBehavior
{
    AlwaysShow,
    AlwaysHide,
    OnlyShowSelected,
}

public sealed class NavigationDestination : StatelessWidget
{
    public NavigationDestination(
        Widget icon,
        string label,
        Widget? selectedIcon = null,
        string? tooltip = null,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        SelectedIcon = selectedIcon;
        Tooltip = tooltip;
        Enabled = enabled;
    }

    public Widget Icon { get; }

    public Widget? SelectedIcon { get; }

    public string Label { get; }

    public string? Tooltip { get; }

    public bool Enabled { get; }

    public override Widget Build(BuildContext context)
    {
        throw new InvalidOperationException("NavigationDestination widgets must be children of NavigationBar.");
    }
}

public sealed class NavigationBar : StatelessWidget
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(500);

    public NavigationBar(
        IReadOnlyList<Widget> destinations,
        TimeSpan? animationDuration = null,
        int selectedIndex = 0,
        Action<int>? onDestinationSelected = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        double? height = null,
        NavigationDestinationLabelBehavior? labelBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        Thickness? labelPadding = null,
        bool maintainBottomViewPadding = false,
        Key? key = null) : base(key)
    {
        if (destinations is null)
        {
            throw new ArgumentNullException(nameof(destinations));
        }

        if (destinations.Count < 2)
        {
            throw new ArgumentException("NavigationBar requires at least two destinations.", nameof(destinations));
        }

        if (selectedIndex < 0 || selectedIndex >= destinations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "Selected index must be within destination range.");
        }

        if (animationDuration.HasValue && animationDuration.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(animationDuration));
        }

        ValidatePositiveFinite(height, nameof(height));
        ValidateNonNegativeFinite(elevation, nameof(elevation));

        Destinations = destinations;
        AnimationDuration = animationDuration;
        SelectedIndex = selectedIndex;
        OnDestinationSelected = onDestinationSelected;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        Height = height;
        LabelBehavior = labelBehavior;
        OverlayColor = overlayColor;
        LabelTextStyle = labelTextStyle;
        LabelPadding = labelPadding;
        MaintainBottomViewPadding = maintainBottomViewPadding;
    }

    public TimeSpan? AnimationDuration { get; }
    public int SelectedIndex { get; }
    public IReadOnlyList<Widget> Destinations { get; }
    public Action<int>? OnDestinationSelected { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public double? Height { get; }
    public NavigationDestinationLabelBehavior? LabelBehavior { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public MaterialStateProperty<TextStyle?>? LabelTextStyle { get; }
    public Thickness? LabelPadding { get; }
    public bool MaintainBottomViewPadding { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var navigationTheme = NavigationBarTheme.Of(context);
        var defaults = ResolveDefaults(theme);
        double effectiveHeight = Height ?? navigationTheme.Height ?? defaults.Height!.Value;
        double effectiveElevation = Elevation ?? navigationTheme.Elevation ?? defaults.Elevation!.Value;
        var effectiveBackground = BackgroundColor ?? navigationTheme.BackgroundColor ?? defaults.BackgroundColor!.Value;
        var effectiveShadow = ShadowColor ?? navigationTheme.ShadowColor ?? defaults.ShadowColor;
        var effectiveSurfaceTint = SurfaceTintColor ?? navigationTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
        var effectiveIndicatorColor = IndicatorColor ?? navigationTheme.IndicatorColor ?? defaults.IndicatorColor!.Value;
        var effectiveIndicatorShape = IndicatorShape ?? navigationTheme.IndicatorShape ?? defaults.IndicatorShape!;
        var effectiveLabelBehavior = LabelBehavior ?? navigationTheme.LabelBehavior ?? defaults.LabelBehavior!.Value;
        var effectiveLabelPadding = LabelPadding ?? navigationTheme.LabelPadding ?? defaults.LabelPadding ?? new Thickness(0, 4, 0, 0);

        var children = new List<Widget>(Destinations.Count);
        for (int index = 0; index < Destinations.Count; index++)
        {
            var destination = Destinations[index];
            int capturedIndex = index;
            Action onTap = OnDestinationSelected is null ? () => { } : () => OnDestinationSelected(capturedIndex);
            string indexLabel = MaterialLocalizations.Of(context).TabLabel(index, Destinations.Count);
            Widget tile = destination is NavigationDestination navigationDestination
                ? new NavigationBarDestinationTile(
                    destination: navigationDestination,
                    selected: index == SelectedIndex,
                    indexLabel: indexLabel,
                    onTap: onTap,
                    duration: AnimationDuration ?? DefaultAnimationDuration,
                    labelBehavior: effectiveLabelBehavior,
                    indicatorColor: effectiveIndicatorColor,
                    indicatorShape: effectiveIndicatorShape,
                    labelTextStyle: ComposeReferenceProperty(LabelTextStyle, navigationTheme.LabelTextStyle, defaults.LabelTextStyle),
                    iconTheme: ComposeReferenceProperty<IconThemeData>(null, navigationTheme.IconTheme, defaults.IconTheme),
                    overlayColor: ComposeColorProperty(OverlayColor, navigationTheme.OverlayColor, defaults.OverlayColor),
                    labelPadding: effectiveLabelPadding,
                    height: effectiveHeight,
                    key: destination.Key ?? new ValueKey<int>(index))
                : new NavigationBarCustomDestinationTile(
                    child: destination,
                    selected: index == SelectedIndex,
                    indexLabel: indexLabel,
                    onTap: onTap,
                    height: effectiveHeight,
                    overlayColor: ComposeColorProperty(OverlayColor, navigationTheme.OverlayColor, defaults.OverlayColor),
                    key: destination.Key ?? new ValueKey<int>(index));
            children.Add(new Expanded(child: tile));
        }

        Widget content = new SizedBox(
            height: effectiveHeight,
            child: new Semantics(
                container: true,
                explicitChildNodes: true,
                child: new Row(children: children)));

        content = new SafeArea(
            left: false,
            top: false,
            right: false,
            maintainBottomViewPadding: MaintainBottomViewPadding,
            child: content);

        return new DecoratedBox(
            decoration: NavigationSurfaceUtilities.CreateDecoration(
                effectiveBackground,
                effectiveElevation,
                effectiveShadow,
                effectiveSurfaceTint,
                theme.UseMaterial3),
            child: content);
    }

    private static NavigationBarThemeData ResolveDefaults(ThemeData theme)
    {
        if (!theme.UseMaterial3)
        {
            return new NavigationBarThemeData(
                Height: 80,
                BackgroundColor: NavigationSurfaceUtilities.Blend(theme.SurfaceColor, theme.OnSurfaceColor, 0.08),
                Elevation: 0,
                IndicatorColor: NavigationSurfaceUtilities.WithOpacity(theme.SecondaryColor, 0.24),
                IndicatorShape: ShapeBorder.RoundedRectangle(16),
                LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                    theme.TextTheme.LabelSmall.CopyWith(color: theme.OnSurfaceColor)),
                IconTheme: MaterialStateProperty<IconThemeData?>.All(
                    new IconThemeData(Color: theme.OnSurfaceColor, Size: 24)),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused)
                        ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.12)
                        : states.HasFlag(MaterialState.Hovered)
                            ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.04)
                            : null),
                LabelBehavior: NavigationDestinationLabelBehavior.AlwaysShow,
                LabelPadding: new Thickness(0, 4, 0, 0));
        }

        return new NavigationBarThemeData(
            Height: 80,
            BackgroundColor: theme.SurfaceContainerColor,
            Elevation: 3,
            ShadowColor: Colors.Transparent,
            SurfaceTintColor: Colors.Transparent,
            IndicatorColor: theme.SecondaryContainerColor,
            IndicatorShape: ShapeBorder.RoundedRectangle(16),
            LabelTextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                theme.TextTheme.LabelMedium.CopyWith(color:
                    states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceVariantColor, 0.38)
                        : states.HasFlag(MaterialState.Selected)
                            ? theme.OnSurfaceColor
                            : theme.OnSurfaceVariantColor)),
            IconTheme: MaterialStateProperty<IconThemeData?>.ResolveWith(states =>
                new IconThemeData(
                    Color: states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceVariantColor, 0.38)
                        : states.HasFlag(MaterialState.Selected)
                            ? theme.OnSecondaryContainerColor
                            : theme.OnSurfaceVariantColor,
                    Size: 24)),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused)
                    ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.10)
                    : states.HasFlag(MaterialState.Hovered)
                        ? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.08)
                        : null),
            LabelBehavior: NavigationDestinationLabelBehavior.AlwaysShow,
            LabelPadding: new Thickness(0, 4, 0, 0));
    }

    private static MaterialStateProperty<T?> ComposeReferenceProperty<T>(
        MaterialStateProperty<T?>? widget,
        MaterialStateProperty<T?>? localTheme,
        MaterialStateProperty<T?>? defaults) where T : class
    {
        return MaterialStateProperty<T?>.ResolveWith(states =>
            widget?.Resolve(states) ?? localTheme?.Resolve(states) ?? defaults?.Resolve(states));
    }

    private static MaterialStateProperty<Color?> ComposeColorProperty(
        MaterialStateProperty<Color?>? widget,
        MaterialStateProperty<Color?>? localTheme,
        MaterialStateProperty<Color?>? defaults)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            var value = widget?.Resolve(states);
            if (value.HasValue) return value;
            value = localTheme?.Resolve(states);
            return value.HasValue ? value : defaults?.Resolve(states);
        });
    }

    private static void ValidatePositiveFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive and finite.");
        }
    }

    private static void ValidateNonNegativeFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative and finite.");
        }
    }
}

public sealed class NavigationIndicator : StatelessWidget
{
    public NavigationIndicator(
        double animationValue,
        Color? color = null,
        double width = 64,
        double height = 32,
        BorderRadius? borderRadius = null,
        ShapeBorder? shape = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(animationValue)) throw new ArgumentOutOfRangeException(nameof(animationValue));
        if (!double.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        AnimationValue = Math.Clamp(animationValue, 0, 1);
        Color = color;
        Width = width;
        Height = height;
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(16);
        Shape = shape;
    }

    public double AnimationValue { get; }
    public Color? Color { get; }
    public double Width { get; }
    public double Height { get; }
    public BorderRadius BorderRadius { get; }
    public ShapeBorder? Shape { get; }

    public override Widget Build(BuildContext context)
    {
        double scale = AnimationValue <= 0
            ? 0
            : 0.4 + (0.6 * Curves.EaseInOut(AnimationValue));
        var shape = Shape ?? ShapeBorder.RoundedRectangle(BorderRadius.Radius);
        return new Opacity(
            opacity: AnimationValue,
            child: new SizedBox(
                width: Width * scale,
                height: Height,
                child: new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: Color ?? Theme.Of(context).SecondaryColor,
                        Border: shape.Side,
                        BorderRadius: shape.BorderRadius))));
    }
}

internal sealed class NavigationBarDestinationTile : StatefulWidget
{
    public NavigationBarDestinationTile(
        NavigationDestination destination,
        bool selected,
        string indexLabel,
        Action? onTap,
        TimeSpan duration,
        NavigationDestinationLabelBehavior labelBehavior,
        Color indicatorColor,
        ShapeBorder indicatorShape,
        MaterialStateProperty<TextStyle?> labelTextStyle,
        MaterialStateProperty<IconThemeData?> iconTheme,
        MaterialStateProperty<Color?> overlayColor,
        Thickness labelPadding,
        double height,
        Key? key = null) : base(key)
    {
        Destination = destination;
        Selected = selected;
        IndexLabel = indexLabel;
        OnTap = onTap;
        Duration = duration;
        LabelBehavior = labelBehavior;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        LabelTextStyle = labelTextStyle;
        IconTheme = iconTheme;
        OverlayColor = overlayColor;
        LabelPadding = labelPadding;
        Height = height;
    }

    public NavigationDestination Destination { get; }
    public bool Selected { get; }
    public string IndexLabel { get; }
    public Action? OnTap { get; }
    public TimeSpan Duration { get; }
    public NavigationDestinationLabelBehavior LabelBehavior { get; }
    public Color IndicatorColor { get; }
    public ShapeBorder IndicatorShape { get; }
    public MaterialStateProperty<TextStyle?> LabelTextStyle { get; }
    public MaterialStateProperty<IconThemeData?> IconTheme { get; }
    public MaterialStateProperty<Color?> OverlayColor { get; }
    public Thickness LabelPadding { get; }
    public double Height { get; }

    public override State CreateState() => new NavigationBarDestinationTileState();
}

internal sealed class NavigationBarDestinationTileState : State
{
    private AnimationController? _controller;

    private NavigationBarDestinationTile CurrentWidget => (NavigationBarDestinationTile)StateWidget;

    public override void InitState()
    {
        CreateController(CurrentWidget.Selected ? 1 : 0);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (NavigationBarDestinationTile)oldWidget;
        if (old.Duration != CurrentWidget.Duration)
        {
            double value = _controller?.Value ?? (CurrentWidget.Selected ? 1 : 0);
            DisposeController();
            CreateController(value);
        }

        if (old.Selected != CurrentWidget.Selected)
        {
            if (CurrentWidget.Selected) _controller!.Forward(); else _controller!.Reverse();
        }
    }

    public override void Dispose()
    {
        DisposeController();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var destination = widget.Destination;
        double progress = _controller?.Evaluate() ?? (widget.Selected ? 1 : 0);
        var icon = widget.Selected && destination.SelectedIcon is not null
            ? destination.SelectedIcon
            : destination.Icon;

        Widget iconWithIndicator = new Stack(
            alignment: Alignment.Center,
            children:
            [
                new NavigationIndicator(
                    animationValue: progress,
                    color: widget.IndicatorColor,
                    shape: widget.IndicatorShape),
                icon
            ]);

        Widget content;
        if (widget.LabelBehavior == NavigationDestinationLabelBehavior.AlwaysHide)
        {
            content = iconWithIndicator;
        }
        else
        {
            double labelOpacity = widget.LabelBehavior == NavigationDestinationLabelBehavior.AlwaysShow ? 1 : progress;
            var label = new Opacity(
                opacity: labelOpacity,
                child: new Padding(
                    insets: widget.LabelPadding,
                    child: new Text(destination.Label, softWrap: false, maxLines: 1, overflow: TextOverflow.Ellipsis)));
            content = new Column(
                mainAxisSize: MainAxisSize.Min,
                mainAxisAlignment: MainAxisAlignment.Center,
                children: [iconWithIndicator, label]);
        }

        var buttonStyle = new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states => widget.LabelTextStyle.Resolve(states)?.Color),
            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: widget.OverlayColor,
            SplashColor: widget.OverlayColor,
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states => widget.IconTheme.Resolve(states)?.Color),
            IconSize: MaterialStateProperty<double?>.ResolveWith(states => widget.IconTheme.Resolve(states)?.Size),
            TextStyle: widget.LabelTextStyle,
            Padding: MaterialStateProperty<Thickness?>.All(default),
            Shape: MaterialStateProperty<BorderRadius?>.All(widget.IndicatorShape.BorderRadius),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, widget.Height)),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            Alignment: Alignment.Center);

        Widget result = new MaterialButtonCore(
            child: content,
            onPressed: destination.Enabled ? widget.OnTap : null,
            style: buttonStyle,
            isSelected: widget.Selected,
            isSemanticButton: true,
            semanticLabel: $"{destination.Label}, {widget.IndexLabel}",
            clipBehavior: Clip.None);

        string tooltipMessage = destination.Tooltip ?? destination.Label;
        if (tooltipMessage.Length > 0)
        {
            result = new Tooltip(
                message: tooltipMessage,
                verticalOffset: 42,
                preferBelow: false,
                excludeFromSemantics: true,
                child: result);
        }

        return new MergeSemantics(result);
    }

    private void CreateController(double value)
    {
        _controller = new AnimationController(CurrentWidget.Duration)
        {
            Curve = Curves.EaseInOut
        };
        _controller.Changed += HandleAnimationChanged;
        if (value >= 1) _controller.Forward(1); else if (value > 0) _controller.Forward(value);
    }

    private void HandleAnimationChanged()
    {
        SetState(() => { });
    }

    private void DisposeController()
    {
        if (_controller is null) return;
        _controller.Changed -= HandleAnimationChanged;
        _controller.Dispose();
        _controller = null;
    }
}

internal sealed class NavigationBarCustomDestinationTile : StatelessWidget
{
    public NavigationBarCustomDestinationTile(
        Widget child,
        bool selected,
        string indexLabel,
        Action onTap,
        double height,
        MaterialStateProperty<Color?> overlayColor,
        Key? key = null) : base(key)
    {
        Child = child;
        Selected = selected;
        IndexLabel = indexLabel;
        OnTap = onTap;
        Height = height;
        OverlayColor = overlayColor;
    }

    public Widget Child { get; }
    public bool Selected { get; }
    public string IndexLabel { get; }
    public Action OnTap { get; }
    public double Height { get; }
    public MaterialStateProperty<Color?> OverlayColor { get; }

    public override Widget Build(BuildContext context)
    {
        return new MaterialButtonCore(
            child: Child,
            onPressed: OnTap,
            style: new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: OverlayColor,
                SplashColor: OverlayColor,
                Padding: MaterialStateProperty<Thickness?>.All(default),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, Height)),
                TapTargetSize: MaterialTapTargetSize.ShrinkWrap,
                Alignment: Alignment.Center),
            isSelected: Selected,
            isSemanticButton: true,
            semanticLabel: IndexLabel,
            clipBehavior: Clip.None);
    }
}

internal static class NavigationSurfaceUtilities
{
    public static Color ApplySurfaceTint(Color background, Color surfaceTint, double elevation)
    {
        return ElevationOverlay.ApplySurfaceTint(background, surfaceTint, elevation);
    }

    public static BoxDecoration CreateDecoration(
        Color background,
        double elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        bool useMaterial3)
    {
        if (useMaterial3)
        {
            background = ElevationOverlay.ApplySurfaceTint(background, surfaceTintColor, elevation);
        }

        BoxShadows? shadows = null;
        if (elevation > 0 && shadowColor.HasValue && shadowColor.Value.A > 0)
        {
            shadows = new BoxShadows(new BoxShadow
            {
                OffsetY = Math.Max(1, elevation * 0.5),
                Blur = Math.Max(2, elevation * 2.4),
                Color = WithOpacity(shadowColor.Value, 0.20),
            });
        }

        return new BoxDecoration(Color: background, BoxShadows: shadows);
    }

    public static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), 0, 255),
            color.R,
            color.G,
            color.B);
    }

    public static Color Blend(Color background, Color foreground, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1) * (foreground.A / 255.0);
        byte Mix(byte a, byte b) => (byte)Math.Clamp((int)Math.Round(a + ((b - a) * opacity)), 0, 255);
        return Color.FromArgb(background.A, Mix(background.R, foreground.R), Mix(background.G, foreground.G), Mix(background.B, foreground.B));
    }
}
