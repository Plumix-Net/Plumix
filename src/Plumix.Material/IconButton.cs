using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/icon_button.dart;
// material_ui/lib/src/icon_button_theme.dart

internal enum IconButtonVariant
{
    Standard,
    Filled,
    FilledTonal,
    Outlined,
}

public class IconButton : StatelessWidget
{
    public IconButton(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        VisualDensity? visualDensity = null,
        EdgeInsetsGeometry? padding = null,
        Alignment? alignment = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        Action<bool>? onHover = null,
        Action? onLongPress = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        BoxConstraints? constraints = null,
        ButtonStyle? style = null,
        bool? isSelected = null,
        Widget? selectedIcon = null,
        double? splashRadius = null,
        string? tooltip = null,
        bool? enableFeedback = null,
        MouseCursor? mouseCursor = null,
        MaterialStatesController? statesController = null,
        Key? key = null) : this(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Standard,
            iconSize: iconSize,
            visualDensity: visualDensity,
            padding: padding,
            alignment: alignment,
            color: color,
            disabledColor: disabledColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            splashColor: splashColor,
            onHover: onHover,
            onLongPress: onLongPress,
            focusNode: focusNode,
            autofocus: autofocus,
            constraints: constraints,
            style: style,
            isSelected: isSelected,
            selectedIcon: selectedIcon,
            splashRadius: splashRadius,
            tooltip: tooltip,
            enableFeedback: enableFeedback,
            mouseCursor: mouseCursor,
            statesController: statesController,
            key: key)
    {
    }

    private IconButton(
        Widget icon,
        Action? onPressed,
        IconButtonVariant variant,
        double? iconSize,
        VisualDensity? visualDensity,
        EdgeInsetsGeometry? padding,
        Alignment? alignment,
        Color? color,
        Color? disabledColor,
        Color? focusColor,
        Color? hoverColor,
        Color? highlightColor,
        Color? splashColor,
        Action<bool>? onHover,
        Action? onLongPress,
        FocusNode? focusNode,
        bool autofocus,
        BoxConstraints? constraints,
        ButtonStyle? style,
        bool? isSelected,
        Widget? selectedIcon,
        Key? key,
        double? splashRadius = null,
        string? tooltip = null,
        bool? enableFeedback = null,
        MouseCursor? mouseCursor = null,
        MaterialStatesController? statesController = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        OnPressed = onPressed;
        Variant = variant;
        IconSize = iconSize;
        VisualDensity = visualDensity;
        Padding = padding;
        Alignment = alignment;
        Color = color;
        DisabledColor = disabledColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        HighlightColor = highlightColor;
        SplashColor = splashColor;
        OnHover = onHover;
        OnLongPress = onLongPress;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Constraints = constraints;
        Style = style;
        IsSelected = isSelected;
        SelectedIcon = selectedIcon;
        SplashRadius = splashRadius;
        Tooltip = tooltip;
        EnableFeedback = enableFeedback;
        MouseCursor = mouseCursor;
        StatesController = statesController;

        if (iconSize.HasValue
            && (double.IsNaN(iconSize.Value)
                || double.IsInfinity(iconSize.Value)
                || iconSize.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be finite and positive.");
        }
        if (splashRadius.HasValue && (!double.IsFinite(splashRadius.Value) || splashRadius.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(splashRadius), "Splash radius must be finite and positive.");
        }
    }

    public Widget Icon { get; }

    public Action? OnPressed { get; }

    private IconButtonVariant Variant { get; }

    public double? IconSize { get; }

    public VisualDensity? VisualDensity { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public Alignment? Alignment { get; }

    public Color? Color { get; }

    public Color? DisabledColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public Color? HighlightColor { get; }

    public Color? SplashColor { get; }

    public Action<bool>? OnHover { get; }

    public Action? OnLongPress { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public BoxConstraints? Constraints { get; }

    public ButtonStyle? Style { get; }

    public bool? IsSelected { get; }

    public Widget? SelectedIcon { get; }

    public double? SplashRadius { get; }

    public string? Tooltip { get; }

    public bool? EnableFeedback { get; }

    public MouseCursor? MouseCursor { get; }

    public MaterialStatesController? StatesController { get; }

    public static IconButton Filled(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        VisualDensity? visualDensity = null,
        EdgeInsetsGeometry? padding = null,
        Alignment? alignment = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        Action<bool>? onHover = null,
        Action? onLongPress = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        BoxConstraints? constraints = null,
        ButtonStyle? style = null,
        bool? isSelected = null,
        Widget? selectedIcon = null,
        double? splashRadius = null,
        string? tooltip = null,
        bool? enableFeedback = null,
        MouseCursor? mouseCursor = null,
        MaterialStatesController? statesController = null,
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Filled,
            iconSize: iconSize,
            visualDensity: visualDensity,
            padding: padding,
            alignment: alignment,
            color: color,
            disabledColor: disabledColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            splashColor: splashColor,
            onHover: onHover,
            onLongPress: onLongPress,
            focusNode: focusNode,
            autofocus: autofocus,
            constraints: constraints,
            style: style,
            isSelected: isSelected,
            selectedIcon: selectedIcon,
            splashRadius: splashRadius,
            tooltip: tooltip,
            enableFeedback: enableFeedback,
            mouseCursor: mouseCursor,
            statesController: statesController,
            key: key);
    }

    public static IconButton FilledTonal(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        VisualDensity? visualDensity = null,
        EdgeInsetsGeometry? padding = null,
        Alignment? alignment = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        Action<bool>? onHover = null,
        Action? onLongPress = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        BoxConstraints? constraints = null,
        ButtonStyle? style = null,
        bool? isSelected = null,
        Widget? selectedIcon = null,
        double? splashRadius = null,
        string? tooltip = null,
        bool? enableFeedback = null,
        MouseCursor? mouseCursor = null,
        MaterialStatesController? statesController = null,
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.FilledTonal,
            iconSize: iconSize,
            visualDensity: visualDensity,
            padding: padding,
            alignment: alignment,
            color: color,
            disabledColor: disabledColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            splashColor: splashColor,
            onHover: onHover,
            onLongPress: onLongPress,
            focusNode: focusNode,
            autofocus: autofocus,
            constraints: constraints,
            style: style,
            isSelected: isSelected,
            selectedIcon: selectedIcon,
            splashRadius: splashRadius,
            tooltip: tooltip,
            enableFeedback: enableFeedback,
            mouseCursor: mouseCursor,
            statesController: statesController,
            key: key);
    }

    public static IconButton Outlined(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        VisualDensity? visualDensity = null,
        EdgeInsetsGeometry? padding = null,
        Alignment? alignment = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? splashColor = null,
        Action<bool>? onHover = null,
        Action? onLongPress = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        BoxConstraints? constraints = null,
        ButtonStyle? style = null,
        bool? isSelected = null,
        Widget? selectedIcon = null,
        double? splashRadius = null,
        string? tooltip = null,
        bool? enableFeedback = null,
        MouseCursor? mouseCursor = null,
        MaterialStatesController? statesController = null,
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Outlined,
            iconSize: iconSize,
            visualDensity: visualDensity,
            padding: padding,
            alignment: alignment,
            color: color,
            disabledColor: disabledColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            highlightColor: highlightColor,
            splashColor: splashColor,
            onHover: onHover,
            onLongPress: onLongPress,
            focusNode: focusNode,
            autofocus: autofocus,
            constraints: constraints,
            style: style,
            isSelected: isSelected,
            selectedIcon: selectedIcon,
            splashRadius: splashRadius,
            tooltip: tooltip,
            enableFeedback: enableFeedback,
            mouseCursor: mouseCursor,
            statesController: statesController,
            key: key);
    }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        Color? highlightColor = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? overlayColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        double? elevation = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        double? iconSize = null,
        BorderSide? side = null,
        BorderRadius? shape = null,
        Thickness? padding = null,
        MouseCursor? enabledMouseCursor = null,
        MouseCursor? disabledMouseCursor = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TimeSpan? animationDuration = null,
        bool? enableFeedback = null,
        AlignmentGeometry? alignment = null)
    {
        if (iconSize.HasValue
            && (double.IsNaN(iconSize.Value)
                || double.IsInfinity(iconSize.Value)
                || iconSize.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be finite and positive.");
        }

        return new ButtonStyle(
            ForegroundColor: CreateDefaultColorResolver(foregroundColor, disabledForegroundColor),
            BackgroundColor: CreateDefaultColorResolver(backgroundColor, disabledBackgroundColor),
            ShadowColor: shadowColor.HasValue
                ? MaterialStateProperty<Color?>.All(shadowColor.Value)
                : null,
            SurfaceTintColor: surfaceTintColor.HasValue
                ? MaterialStateProperty<Color?>.All(surfaceTintColor.Value)
                : null,
            OverlayColor: CreateStyleFromOverlayResolver(
                foregroundColor,
                overlayColor,
                focusColor,
                hoverColor,
                highlightColor),
            SplashFactory: splashFactory,
            Elevation: elevation.HasValue
                ? MaterialStateProperty<double?>.All(elevation.Value)
                : null,
            IconSize: iconSize.HasValue
                ? MaterialStateProperty<double?>.All(iconSize.Value)
                : null,
            Side: side.HasValue
                ? MaterialStateProperty<BorderSide?>.All(side.Value)
                : null,
            Padding: padding.HasValue
                ? MaterialStateProperty<EdgeInsetsGeometry?>.All(padding.Value)
                : null,
            Shape: shape.HasValue
                ? MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius: shape.Value))
                : null,
            MinimumSize: minimumSize.HasValue
                ? MaterialStateProperty<Size?>.All(minimumSize.Value)
                : null,
            FixedSize: fixedSize.HasValue
                ? MaterialStateProperty<Size?>.All(fixedSize.Value)
                : null,
            MaximumSize: maximumSize.HasValue
                ? MaterialStateProperty<Size?>.All(maximumSize.Value)
                : null,
            MouseCursor: CreateMouseCursorResolver(
                enabledMouseCursor,
                disabledMouseCursor),
            VisualDensity: visualDensity,
            Alignment: alignment,
            TapTargetSize: tapTargetSize,
            AnimationDuration: animationDuration,
            EnableFeedback: enableFeedback);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        return theme.UseMaterial3
            ? BuildMaterial3(context, theme)
            : BuildMaterial2(context, theme);
    }

    private Widget BuildMaterial3(BuildContext context, ThemeData theme)
    {
        bool isSelected = IsSelected ?? false;
        Size? minimumSize = Constraints is not BoxConstraints explicitConstraints
            ? null
            : new Size(explicitConstraints.MinWidth, explicitConstraints.MinHeight);
        Size? maximumSize = Constraints is not BoxConstraints maxConstraints
            ? null
            : new Size(maxConstraints.MaxWidth, maxConstraints.MaxHeight);

        var adjustedStyle = StyleFrom(
            visualDensity: VisualDensity,
            foregroundColor: Color,
            disabledForegroundColor: DisabledColor,
            focusColor: FocusColor,
            hoverColor: HoverColor,
            highlightColor: HighlightColor,
            padding: Padding?.Resolve(Directionality.Of(context)),
            minimumSize: minimumSize,
            maximumSize: maximumSize,
            iconSize: IconSize,
            alignment: Alignment,
            enabledMouseCursor: MouseCursor,
            disabledMouseCursor: MouseCursor,
            enableFeedback: EnableFeedback);

        if (adjustedStyle.IconColor is null)
        {
            adjustedStyle = adjustedStyle with
            {
                IconColor = adjustedStyle.ForegroundColor
            };
        }

        var effectiveWidgetStyle = Style is null
            ? adjustedStyle
            : Style.Merge(adjustedStyle);

        var effectiveIcon = isSelected && SelectedIcon is not null
            ? SelectedIcon
            : Icon;

        return new SelectableIconButton(
            style: effectiveWidgetStyle,
            onPressed: OnPressed,
            onHover: OnHover,
            onLongPress: OnPressed is null ? null : OnLongPress,
            autofocus: Autofocus,
            focusNode: FocusNode,
            isSelected: IsSelected,
            variant: Variant,
            tooltip: Tooltip,
            statesController: StatesController,
            child: effectiveIcon);
    }

    private Widget BuildMaterial2(BuildContext context, ThemeData theme)
    {
        Color? currentColor = OnPressed is not null
            ? Color
            : DisabledColor ?? theme.DisabledColor;
        VisualDensity effectiveVisualDensity = VisualDensity ?? theme.VisualDensity;
        BoxConstraints unadjustedConstraints = Constraints
                                               ?? new BoxConstraints(
                                                   MinWidth: 48,
                                                   MinHeight: 48);
        BoxConstraints adjustedConstraints = effectiveVisualDensity.EffectiveConstraints(
            unadjustedConstraints);
        var ambientIconTheme = Plumix.Widgets.IconTheme.Of(context);
        double effectiveIconSize = IconSize ?? ambientIconTheme.Size ?? 24.0;
        Thickness effectivePadding = Padding?.Resolve(Directionality.Of(context)) ?? new Thickness(8);
        Plumix.Rendering.Alignment effectiveAlignment = Alignment
                                                         ?? Plumix.Rendering.Alignment.Center;
        bool effectiveEnableFeedback = EnableFeedback ?? true;
        double paddingExtent = Math.Min(
            effectivePadding.Left + effectivePadding.Right,
            effectivePadding.Top + effectivePadding.Bottom);
        double effectiveSplashRadius = SplashRadius
                                       ?? Math.Max(
                                           35.0,
                                           (effectiveIconSize + paddingExtent) * 0.7);

        Widget result = new ConstrainedBox(
            constraints: adjustedConstraints,
            child: new Padding(
                insets: effectivePadding,
                child: new SizedBox(
                    width: effectiveIconSize,
                    height: effectiveIconSize,
                    child: new Align(
                        alignment: effectiveAlignment,
                        child: new Plumix.Widgets.IconTheme(
                            data: new IconThemeData(
                                Color: currentColor ?? ambientIconTheme.Color,
                                Size: effectiveIconSize,
                                Opacity: ambientIconTheme.Opacity),
                            child: Icon)))));

        result = new InkResponse(
            focusNode: FocusNode,
            autofocus: Autofocus,
            canRequestFocus: OnPressed is not null,
            onTap: OnPressed,
            onHover: OnHover,
            onLongPress: OnPressed is null ? null : OnLongPress,
            mouseCursor: MouseCursor ?? ResolveAdaptiveCursor(OnPressed is not null),
            enableFeedback: effectiveEnableFeedback,
            focusColor: FocusColor ?? theme.FocusColor,
            hoverColor: HoverColor ?? theme.HoverColor,
            highlightColor: HighlightColor ?? theme.HighlightColor,
            splashColor: SplashColor ?? theme.SplashColor,
            radius: effectiveSplashRadius,
            child: result);

        if (Tooltip is not null)
        {
            result = new Tooltip(
                message: Tooltip,
                child: result);
        }

        return new Semantics(
            flags: SemanticsFlags.IsButton
                   | (OnPressed is not null
                       ? SemanticsFlags.IsEnabled
                       : SemanticsFlags.None),
            child: result);
    }

    internal static ButtonStyle ResolveThemeStyle(
        BuildContext context,
        IconThemeData iconTheme)
    {
        var theme = Theme.Of(context);
        Color defaultIconColor = theme.Brightness == Brightness.Dark
            ? Colors.White
            : Avalonia.Media.Color.FromArgb(0xDD, 0x00, 0x00, 0x00);
        bool isDefaultColor = iconTheme.Color == defaultIconColor;
        bool isDefaultSize = iconTheme.Size is null;
        var iconThemeStyle = StyleFrom(
            foregroundColor: isDefaultColor ? null : iconTheme.Color,
            iconSize: isDefaultSize ? null : iconTheme.Size);

        var iconButtonThemeStyle = IconButtonTheme.Of(context).Style;
        return iconButtonThemeStyle?.Merge(iconThemeStyle) ?? iconThemeStyle;
    }

    internal static ButtonStyle CreateDefaultStyle(
        ThemeData theme,
        bool isToggleable,
        IconButtonVariant variant)
    {
        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultForegroundColor(theme, variant, isToggleable, states)),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultBackgroundColor(theme, variant, isToggleable, states)),
            ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultOverlayColor(theme, variant, isToggleable, states)),
            Elevation: MaterialStateProperty<double?>.All(0),
            IconSize: MaterialStateProperty<double?>.All(24),
            Side: variant == IconButtonVariant.Outlined
                ? MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                    ResolveOutlinedBorderSide(theme, states))
                : null,
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(new Thickness(8)),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius: 
                Plumix.Rendering.BorderRadius.Circular(9999))),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(40, 40)),
            MaximumSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, double.PositiveInfinity)),
            Alignment: Plumix.Rendering.Alignment.Center,
            TapTargetSize: theme.MaterialTapTargetSize,
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(
                states => ResolveAdaptiveCursor(!states.HasFlag(MaterialState.Disabled))),
            VisualDensity: Plumix.Material.VisualDensity.Standard,
            AnimationDuration: TimeSpan.FromMilliseconds(200),
            EnableFeedback: true,
            SplashFactory: theme.SplashFactory);
    }

    private static Color ResolveDefaultForegroundColor(
        ThemeData theme,
        IconButtonVariant variant,
        bool isToggleable,
        MaterialState states)
    {
        if (states.HasFlag(MaterialState.Disabled))
        {
            return theme.ColorScheme.OnSurface.WithOpacity(0.38);
        }

        bool isSelected = states.HasFlag(MaterialState.Selected);
        return variant switch
        {
            IconButtonVariant.Filled => isSelected
                ? theme.ColorScheme.OnPrimary
                : isToggleable
                    ? theme.ColorScheme.Primary
                    : theme.ColorScheme.OnPrimary,
            IconButtonVariant.FilledTonal => isSelected
                ? theme.ColorScheme.OnSecondaryContainer
                : isToggleable
                    ? theme.ColorScheme.OnSurfaceVariant
                    : theme.ColorScheme.OnSecondaryContainer,
            IconButtonVariant.Outlined => isSelected
                ? theme.ColorScheme.OnInverseSurface
                : theme.ColorScheme.OnSurfaceVariant,
            _ => isSelected
                ? theme.ColorScheme.Primary
                : theme.ColorScheme.OnSurfaceVariant,
        };
    }

    private static Color ResolveDefaultBackgroundColor(
        ThemeData theme,
        IconButtonVariant variant,
        bool isToggleable,
        MaterialState states)
    {
        bool isDisabled = states.HasFlag(MaterialState.Disabled);
        bool isSelected = states.HasFlag(MaterialState.Selected);

        return variant switch
        {
            IconButtonVariant.Filled => isDisabled
                ? theme.ColorScheme.OnSurface.WithOpacity(0.12)
                : isSelected
                    ? theme.ColorScheme.Primary
                    : isToggleable
                        ? theme.ColorScheme.SurfaceContainerHighest
                        : theme.ColorScheme.Primary,
            IconButtonVariant.FilledTonal => isDisabled
                ? theme.ColorScheme.OnSurface.WithOpacity(0.12)
                : isSelected
                    ? theme.ColorScheme.SecondaryContainer
                    : isToggleable
                        ? theme.ColorScheme.SurfaceContainerHighest
                        : theme.ColorScheme.SecondaryContainer,
            IconButtonVariant.Outlined => isDisabled
                ? isSelected
                    ? theme.ColorScheme.OnSurface.WithOpacity(0.12)
                    : Colors.Transparent
                : isSelected
                    ? theme.ColorScheme.InverseSurface
                    : Colors.Transparent,
            _ => Colors.Transparent,
        };
    }

    private static Color ResolveDefaultOverlayColor(
        ThemeData theme,
        IconButtonVariant variant,
        bool isToggleable,
        MaterialState states)
    {
        if (states.HasFlag(MaterialState.Disabled))
        {
            return Colors.Transparent;
        }

        if (states.HasFlag(MaterialState.Selected))
        {
            var selectedOverlay = variant switch
            {
                IconButtonVariant.Filled => theme.ColorScheme.OnPrimary,
                IconButtonVariant.FilledTonal => theme.ColorScheme.OnSecondaryContainer,
                IconButtonVariant.Outlined => theme.ColorScheme.OnInverseSurface,
                _ => theme.ColorScheme.Primary,
            };

            double selectedFocusOpacity = variant == IconButtonVariant.Outlined ? 0.08 : 0.10;
            return ResolveStateLayerColor(selectedOverlay, states, selectedFocusOpacity);
        }

        if (variant == IconButtonVariant.Outlined)
        {
            if (states.HasFlag(MaterialState.Pressed))
            {
                return theme.ColorScheme.OnSurface.WithOpacity(0.10);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08);
            }

            return Colors.Transparent;
        }

        var overlayColor = variant switch
        {
            IconButtonVariant.Filled => isToggleable
                ? theme.ColorScheme.Primary
                : theme.ColorScheme.OnPrimary,
            IconButtonVariant.FilledTonal => isToggleable
                ? theme.ColorScheme.OnSurfaceVariant
                : theme.ColorScheme.OnSecondaryContainer,
            _ => theme.ColorScheme.OnSurfaceVariant,
        };

        return ResolveStateLayerColor(overlayColor, states);
    }

    private static BorderSide? ResolveOutlinedBorderSide(ThemeData theme, MaterialState states)
    {
        if (states.HasFlag(MaterialState.Selected))
        {
            return null;
        }

        if (states.HasFlag(MaterialState.Disabled))
        {
            return new BorderSide(
                theme.ColorScheme.OnSurface.WithOpacity(0.12),
                1);
        }

        return new BorderSide(theme.ColorScheme.Outline, 1);
    }

    private static Color ResolveStateLayerColor(
        Color baseColor,
        MaterialState states,
        double focusedOpacity = 0.10)
    {
        if (states.HasFlag(MaterialState.Pressed))
        {
            return baseColor.WithOpacity(0.10);
        }

        if (states.HasFlag(MaterialState.Hovered))
        {
            return baseColor.WithOpacity(0.08);
        }

        if (states.HasFlag(MaterialState.Focused))
        {
            return baseColor.WithOpacity(focusedOpacity);
        }

        return Colors.Transparent;
    }

    private static MaterialStateProperty<Color?>? CreateDefaultColorResolver(
        Color? enabledColor,
        Color? disabledColor)
    {
        if (!enabledColor.HasValue && !disabledColor.HasValue)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Disabled)
                ? disabledColor
                : enabledColor);
    }

    private static MaterialStateProperty<Color?>? CreateStyleFromOverlayResolver(
        Color? foregroundColor,
        Color? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        Color? highlightColor)
    {
        var overlayFallback = overlayColor ?? foregroundColor;
        if (!overlayFallback.HasValue
            && !focusColor.HasValue
            && !hoverColor.HasValue
            && !highlightColor.HasValue)
        {
            return null;
        }

        if (overlayColor.HasValue && overlayColor.Value.A == 0)
        {
            return MaterialStateProperty<Color?>.All(overlayColor.Value);
        }

        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                if (highlightColor.HasValue)
                {
                    return highlightColor.Value;
                }

                return overlayFallback.HasValue
                    ? overlayFallback.Value.WithOpacity(0.10)
                    : null;
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                if (hoverColor.HasValue)
                {
                    return hoverColor.Value;
                }

                return overlayFallback.HasValue
                    ? overlayFallback.Value.WithOpacity(0.08)
                    : null;
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                if (focusColor.HasValue)
                {
                    return focusColor.Value;
                }

                return overlayFallback.HasValue
                    ? overlayFallback.Value.WithOpacity(0.10)
                    : null;
            }

            return null;
        });
    }

    private static MaterialStateProperty<MouseCursor?>? CreateMouseCursorResolver(
        MouseCursor? enabledMouseCursor,
        MouseCursor? disabledMouseCursor)
    {
        if (enabledMouseCursor is null && disabledMouseCursor is null)
        {
            return null;
        }

        return MaterialStateProperty<MouseCursor?>.ResolveWith(
            states => states.HasFlag(MaterialState.Disabled)
                ? disabledMouseCursor
                : enabledMouseCursor);
    }

    private static MouseCursor ResolveAdaptiveCursor(bool enabled)
    {
        return enabled && OperatingSystem.IsBrowser()
            ? SystemMouseCursors.Click
            : SystemMouseCursors.Basic;
    }
}

/// <summary>
/// Dart parity: `_SelectableIconButton`. Owns the states controller so the selected state reaches
/// <see cref="IconButtonM3"/>'s style resolution.
/// </summary>
internal sealed class SelectableIconButton : StatefulWidget
{
    public SelectableIconButton(
        Widget child,
        IconButtonVariant variant,
        bool autofocus,
        Action? onPressed,
        bool? isSelected = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        MaterialStatesController? statesController = null,
        string? tooltip = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Variant = variant;
        Autofocus = autofocus;
        OnPressed = onPressed;
        IsSelected = isSelected;
        Style = style;
        FocusNode = focusNode;
        OnLongPress = onLongPress;
        OnHover = onHover;
        StatesController = statesController;
        Tooltip = tooltip;
    }

    public Widget Child { get; }
    public IconButtonVariant Variant { get; }
    public bool Autofocus { get; }
    public Action? OnPressed { get; }
    public bool? IsSelected { get; }
    public ButtonStyle? Style { get; }
    public FocusNode? FocusNode { get; }
    public Action? OnLongPress { get; }
    public Action<bool>? OnHover { get; }
    public MaterialStatesController? StatesController { get; }
    public string? Tooltip { get; }

    public override State CreateState() => new SelectableIconButtonState();

    private sealed class SelectableIconButtonState : State
    {
        private MaterialStatesController? _internalStatesController;

        private SelectableIconButton CurrentWidget => (SelectableIconButton)StateWidget;

        private MaterialStatesController StatesController =>
            CurrentWidget.StatesController ?? _internalStatesController!;

        private bool IsSelected => CurrentWidget.IsSelected ?? false;

        public override void InitState()
        {
            base.InitState();
            if (CurrentWidget.StatesController is null)
            {
                _internalStatesController = new MaterialStatesController();
            }

            StatesController.Update(MaterialState.Selected, IsSelected);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var old = (SelectableIconButton)oldWidget;
            if (!ReferenceEquals(old.StatesController, CurrentWidget.StatesController))
            {
                if (CurrentWidget.StatesController is not null)
                {
                    _internalStatesController?.Dispose();
                    _internalStatesController = null;
                }

                InitStatesController();
            }

            if (old.IsSelected != CurrentWidget.IsSelected)
            {
                StatesController.Update(MaterialState.Selected, IsSelected);
            }
        }

        public override void Dispose()
        {
            _internalStatesController?.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            SelectableIconButton widget = CurrentWidget;
            bool toggleable = widget.IsSelected is not null;

            return new IconButtonM3(
                statesController: StatesController,
                style: widget.Style,
                autofocus: widget.Autofocus,
                focusNode: widget.FocusNode,
                onPressed: widget.OnPressed,
                onHover: widget.OnHover,
                onLongPress: widget.OnPressed is null ? null : widget.OnLongPress,
                variant: widget.Variant,
                toggleable: toggleable,
                tooltip: widget.Tooltip,
                child: new Semantics(selected: widget.IsSelected, child: widget.Child));
        }

        private void InitStatesController()
        {
            if (CurrentWidget.StatesController is null)
            {
                _internalStatesController = new MaterialStatesController();
            }

            StatesController.Update(MaterialState.Selected, IsSelected);
        }
    }
}

/// <summary>Dart parity: `_IconButtonM3`.</summary>
internal sealed class IconButtonM3 : ButtonStyleButton
{
    public IconButtonM3(
        Widget child,
        Action? onPressed,
        IconButtonVariant variant,
        bool toggleable,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        Action<bool>? onHover = null,
        Action? onLongPress = null,
        bool autofocus = false,
        MaterialStatesController? statesController = null,
        string? tooltip = null,
        Key? key = null) : base(
            onPressed: onPressed,
            onLongPress: onLongPress,
            onHover: onHover,
            onFocusChange: null,
            style: style,
            focusNode: focusNode,
            autofocus: autofocus,
            clipBehavior: Plumix.UI.Clip.None,
            child: child,
            statesController: statesController,
            tooltip: tooltip,
            key: key)
    {
        Variant = variant;
        Toggleable = toggleable;
    }

    private IconButtonVariant Variant { get; }

    private bool Toggleable { get; }

    protected internal override ButtonStyle DefaultStyleOf(BuildContext context)
    {
        return IconButton.CreateDefaultStyle(Theme.Of(context), Toggleable, Variant);
    }

    /// <summary>
    /// Dart's `_IconButtonM3.themeStyleOf`: the `IconButtonTheme` style, backfilled from the ambient
    /// `IconTheme` for the colour and size it does not set itself.
    /// </summary>
    protected internal override ButtonStyle? ThemeStyleOf(BuildContext context)
    {
        return IconButton.ResolveThemeStyle(context, Plumix.Widgets.IconTheme.Of(context));
    }
}
