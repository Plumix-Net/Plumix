using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/toggle_buttons.dart

public sealed class ToggleButtons : StatelessWidget
{
    private const double DefaultBorderWidth = 1;

    public ToggleButtons(
        IReadOnlyList<Widget> children,
        IReadOnlyList<bool> isSelected,
        Action<int>? onPressed = null,
        MouseCursor? mouseCursor = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TextStyle? textStyle = null,
        BoxConstraints? constraints = null,
        Color? color = null,
        Color? selectedColor = null,
        Color? disabledColor = null,
        Color? fillColor = null,
        Color? focusColor = null,
        Color? highlightColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        IReadOnlyList<FocusNode>? focusNodes = null,
        bool renderBorder = true,
        Color? borderColor = null,
        Color? selectedBorderColor = null,
        Color? disabledBorderColor = null,
        BorderRadius? borderRadius = null,
        double? borderWidth = null,
        Axis direction = Axis.Horizontal,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        Key? key = null) : base(key)
    {
        if (children is null) throw new ArgumentNullException(nameof(children));
        if (isSelected is null) throw new ArgumentNullException(nameof(isSelected));
        if (children.Count != isSelected.Count)
        {
            throw new ArgumentException("children and isSelected must have the same length.", nameof(isSelected));
        }
        if (focusNodes is not null && focusNodes.Count != children.Count)
        {
            throw new ArgumentException("focusNodes and children must have the same length.", nameof(focusNodes));
        }
        if (borderWidth.HasValue && (!double.IsFinite(borderWidth.Value) || borderWidth.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(borderWidth), "Border width must be non-negative and finite.");
        }
        if (constraints.HasValue && !constraints.Value.IsNormalized)
        {
            throw new ArgumentException("Constraints must be normalized.", nameof(constraints));
        }

        Children = children;
        IsSelected = isSelected;
        OnPressed = onPressed;
        MouseCursor = mouseCursor;
        TapTargetSize = tapTargetSize;
        TextStyle = textStyle;
        Constraints = constraints;
        Color = color;
        SelectedColor = selectedColor;
        DisabledColor = disabledColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        HighlightColor = highlightColor;
        HoverColor = hoverColor;
        SplashColor = splashColor;
        FocusNodes = focusNodes;
        RenderBorder = renderBorder;
        BorderColor = borderColor;
        SelectedBorderColor = selectedBorderColor;
        DisabledBorderColor = disabledBorderColor;
        BorderRadius = borderRadius;
        BorderWidth = borderWidth;
        Direction = direction;
        VerticalDirection = verticalDirection;
    }

    public IReadOnlyList<Widget> Children { get; }
    public IReadOnlyList<bool> IsSelected { get; }
    public Action<int>? OnPressed { get; }
    public MouseCursor? MouseCursor { get; }
    public MaterialTapTargetSize? TapTargetSize { get; }
    public TextStyle? TextStyle { get; }
    public BoxConstraints? Constraints { get; }
    public Color? Color { get; }
    public Color? SelectedColor { get; }
    public Color? DisabledColor { get; }
    public Color? FillColor { get; }
    public Color? FocusColor { get; }
    public Color? HighlightColor { get; }
    public Color? HoverColor { get; }
    public Color? SplashColor { get; }
    public IReadOnlyList<FocusNode>? FocusNodes { get; }
    public bool RenderBorder { get; }
    public Color? BorderColor { get; }
    public Color? SelectedBorderColor { get; }
    public Color? DisabledBorderColor { get; }
    public BorderRadius? BorderRadius { get; }
    public double? BorderWidth { get; }
    public Axis Direction { get; }
    public VerticalDirection VerticalDirection { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var toggleTheme = ToggleButtonsTheme.Of(context);
        var effectiveConstraints = Constraints ?? toggleTheme.Constraints;
        var minimumSize = effectiveConstraints?.Smallest ?? new Size(48, 48);
        var maximumSize = effectiveConstraints?.Biggest;
        var effectiveTapTarget = TapTargetSize ?? theme.MaterialTapTargetSize;
        var effectiveTextStyle = TextStyle ?? toggleTheme.TextStyle ?? theme.TextTheme.BodyMedium;
        double effectiveBorderWidth = BorderWidth ?? toggleTheme.BorderWidth ?? DefaultBorderWidth;
        var effectiveRadius = BorderRadius ?? toggleTheme.BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;
        var textDirection = Directionality.Of(context);

        var indexedButtons = new List<(int Index, Widget Button)>(Children.Count);
        for (int index = 0; index < Children.Count; index++)
        {
            bool selected = IsSelected[index];
            bool enabled = OnPressed is not null;
            var foreground = !enabled
                ? DisabledColor ?? toggleTheme.DisabledColor
                    ?? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.38)
                : selected
                    ? SelectedColor ?? toggleTheme.SelectedColor ?? theme.PrimaryColor
                    : Color ?? toggleTheme.Color
                        ?? NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.87);
            var background = enabled && selected
                ? FillColor ?? toggleTheme.FillColor
                    ?? NavigationSurfaceUtilities.WithOpacity(theme.PrimaryColor, 0.12)
                : Colors.Transparent;
            var border = ResolveBorderSide(theme, toggleTheme, selected, enabled, effectiveBorderWidth);
            int capturedIndex = index;
            var overlay = MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveOverlayColor(theme, toggleTheme, selected, enabled, states));
            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(foreground),
                BackgroundColor: MaterialStateProperty<Color?>.All(background),
                OverlayColor: overlay,
                SplashColor: overlay,
                Elevation: MaterialStateProperty<double?>.All(0),
                IconColor: MaterialStateProperty<Color?>.All(foreground),
                IconSize: MaterialStateProperty<double?>.All(24),
                Side: RenderBorder ? MaterialStateProperty<BorderSide?>.All(border) : null,
                Padding: MaterialStateProperty<Thickness?>.All(default),
                Shape: MaterialStateProperty<BorderRadius?>.All(Plumix.Rendering.BorderRadius.Zero),
                MinimumSize: MaterialStateProperty<Size?>.All(minimumSize),
                MaximumSize: maximumSize.HasValue
                    ? MaterialStateProperty<Size?>.All(maximumSize.Value)
                    : null,
                Alignment: Alignment.Center,
                TapTargetSize: effectiveTapTarget,
                TextStyle: MaterialStateProperty<TextStyle?>.All(
                    effectiveTextStyle.CopyWith(color: foreground)));

            Widget button = new MaterialButtonCore(
                child: Children[index],
                onPressed: enabled ? () => OnPressed!(capturedIndex) : null,
                style: style,
                focusNode: FocusNodes?[index],
                isSelected: selected,
                includeSemanticSelected: false,
                isSemanticButton: true,
                isSemanticChecked: selected,
                mouseCursor: MouseCursor,
                clipBehavior: Clip.HardEdge);
            indexedButtons.Add((index, button));
        }

        Widget group = new SegmentedControlLayout(
            children: indexedButtons.Select(entry => entry.Button).ToList(),
            direction: Direction,
            textDirection: textDirection,
            verticalDirection: VerticalDirection);

        if (RenderBorder && effectiveRadius.Radius > 0)
        {
            group = new ClipRRect(effectiveRadius, group);
        }

        return group;
    }

    private BorderSide ResolveBorderSide(
        ThemeData theme,
        ToggleButtonsThemeData toggleTheme,
        bool selected,
        bool enabled,
        double width)
    {
        var color = !enabled
            ? DisabledBorderColor ?? toggleTheme.DisabledBorderColor
            : selected
                ? SelectedBorderColor ?? toggleTheme.SelectedBorderColor
                : BorderColor ?? toggleTheme.BorderColor;
        color ??= NavigationSurfaceUtilities.WithOpacity(theme.OnSurfaceColor, 0.12);
        return new BorderSide(color.Value, width);
    }

    private Color? ResolveOverlayColor(
        ThemeData theme,
        ToggleButtonsThemeData toggleTheme,
        bool selected,
        bool enabled,
        MaterialState states)
    {
        if (!enabled) return null;
        var stateColor = selected ? theme.PrimaryColor : theme.OnSurfaceColor;
        if (states.HasFlag(MaterialState.Pressed))
        {
            return SplashColor ?? toggleTheme.SplashColor
                ?? (!selected ? HighlightColor ?? toggleTheme.HighlightColor : null)
                ?? NavigationSurfaceUtilities.WithOpacity(stateColor, 0.16);
        }
        if (states.HasFlag(MaterialState.Hovered))
        {
            return HoverColor ?? toggleTheme.HoverColor
                ?? NavigationSurfaceUtilities.WithOpacity(stateColor, 0.04);
        }
        if (states.HasFlag(MaterialState.Focused))
        {
            return FocusColor ?? toggleTheme.FocusColor
                ?? NavigationSurfaceUtilities.WithOpacity(stateColor, 0.12);
        }
        return null;
    }
}
