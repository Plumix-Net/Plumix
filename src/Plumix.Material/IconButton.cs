using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/icon_button.dart; flutter/packages/flutter/lib/src/material/icon_button_theme.dart (approximate)

internal enum IconButtonVariant
{
    Standard,
    Filled,
    FilledTonal,
    Outlined,
}

public sealed class IconButton : StatelessWidget
{
    public IconButton(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        Thickness? padding = null,
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
        Key? key = null) : this(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Standard,
            iconSize: iconSize,
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
            key: key)
    {
    }

    private IconButton(
        Widget icon,
        Action? onPressed,
        IconButtonVariant variant,
        double? iconSize,
        Thickness? padding,
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
        Key? key) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        OnPressed = onPressed;
        Variant = variant;
        IconSize = iconSize;
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

        if (iconSize.HasValue && (double.IsNaN(iconSize.Value) || double.IsInfinity(iconSize.Value) || iconSize.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be finite and positive.");
        }
    }

    public Widget Icon { get; }

    public Action? OnPressed { get; }

    private IconButtonVariant Variant { get; }

    public double? IconSize { get; }

    public Thickness? Padding { get; }

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

    public static IconButton Filled(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        Thickness? padding = null,
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
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Filled,
            iconSize: iconSize,
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
            key: key);
    }

    public static IconButton FilledTonal(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        Thickness? padding = null,
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
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.FilledTonal,
            iconSize: iconSize,
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
            key: key);
    }

    public static IconButton Outlined(
        Widget icon,
        Action? onPressed,
        double? iconSize = null,
        Thickness? padding = null,
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
        Key? key = null)
    {
        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            variant: IconButtonVariant.Outlined,
            iconSize: iconSize,
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
        Color? splashColor = null,
        double? elevation = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        double? iconSize = null,
        BorderSide? side = null,
        BorderRadius? shape = null,
        Thickness? padding = null,
        MaterialTapTargetSize? tapTargetSize = null,
        Alignment? alignment = null)
    {
        if (iconSize.HasValue && (double.IsNaN(iconSize.Value) || double.IsInfinity(iconSize.Value) || iconSize.Value <= 0))
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
            SplashColor: splashColor.HasValue
                ? MaterialButtonCore.CreateExplicitSplashResolver(splashColor.Value)
                : null,
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
                ? MaterialStateProperty<Thickness?>.All(padding.Value)
                : null,
            Shape: shape.HasValue
                ? MaterialStateProperty<BorderRadius?>.All(shape.Value)
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
            Alignment: alignment,
            TapTargetSize: tapTargetSize);
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var iconThemeData = Plumix.Widgets.IconTheme.Of(context);
        var canToggle = theme.UseMaterial3 && IsSelected.HasValue;
        var isSelected = canToggle && IsSelected!.Value;
        Size? minimumSize = Constraints is not BoxConstraints explicitConstraints
            ? null
            : new Size(explicitConstraints.MinWidth, explicitConstraints.MinHeight);
        Size? maximumSize = Constraints is not BoxConstraints maxConstraints
            ? null
            : new Size(maxConstraints.MaxWidth, maxConstraints.MaxHeight);

        var adjustedStyle = StyleFrom(
            foregroundColor: Color,
            disabledForegroundColor: DisabledColor,
            focusColor: FocusColor,
            hoverColor: HoverColor,
            highlightColor: HighlightColor,
            padding: Padding,
            minimumSize: minimumSize,
            maximumSize: maximumSize,
            iconSize: IconSize,
            alignment: Alignment);

        if (SplashColor.HasValue)
        {
            adjustedStyle = adjustedStyle with
            {
                SplashColor = MaterialButtonCore.CreateExplicitSplashResolver(SplashColor.Value)
            };
        }

        var effectiveWidgetStyle = Style is null
            ? adjustedStyle
            : Style.Merge(adjustedStyle);

        var themeStyle = ResolveThemeStyle(context, iconThemeData);

        var mergedStyle = MaterialButtonCore.ComposeStyles(
            defaults: CreateDefaultStyle(theme, canToggle, Variant),
            themeStyle: themeStyle,
            widgetStyle: effectiveWidgetStyle,
            legacyOverrides: null);

        var effectiveIcon = isSelected && SelectedIcon is not null
            ? SelectedIcon
            : Icon;

        return new MaterialButtonCore(
            child: effectiveIcon,
            onPressed: OnPressed,
            onLongPress: OnPressed is null ? null : OnLongPress,
            onHoverChanged: OnHover,
            style: mergedStyle,
            focusNode: FocusNode,
            isSelected: isSelected,
            autofocus: Autofocus);
    }

    private static ButtonStyle ResolveThemeStyle(BuildContext context, IconThemeData iconTheme)
    {
        var iconThemeStyle = StyleFrom(
            foregroundColor: iconTheme.Color,
            iconSize: iconTheme.Size);

        var iconButtonThemeStyle = IconButtonTheme.Of(context).Style;
        return iconButtonThemeStyle?.Merge(iconThemeStyle) ?? iconThemeStyle;
    }

    private static ButtonStyle CreateDefaultStyle(
        ThemeData theme,
        bool isToggleable,
        IconButtonVariant variant)
    {
        var minDimension = theme.UseMaterial3 ? 40.0 : 48.0;
        var borderRadius = Plumix.Rendering.BorderRadius.Circular(theme.UseMaterial3 ? 20 : 24);

        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultForegroundColor(theme, variant, isToggleable, states)),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultBackgroundColor(theme, variant, isToggleable, states)),
            ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                ResolveDefaultOverlayColor(theme, variant, isToggleable, states)),
            SplashColor: null,
            Elevation: MaterialStateProperty<double?>.All(0),
            IconSize: MaterialStateProperty<double?>.All(24),
            Side: variant == IconButtonVariant.Outlined
                ? MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                    ResolveOutlinedBorderSide(theme, states))
                : MaterialStateProperty<BorderSide?>.All(null),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(borderRadius),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(minDimension, minDimension)),
            MaximumSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, double.PositiveInfinity)),
            Alignment: Plumix.Rendering.Alignment.Center,
            TapTargetSize: theme.MaterialTapTargetSize);
    }

    private static Color ResolveDefaultForegroundColor(
        ThemeData theme,
        IconButtonVariant variant,
        bool isToggleable,
        MaterialState states)
    {
        if (states.HasFlag(MaterialState.Disabled))
        {
            return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
        }

        var isSelected = states.HasFlag(MaterialState.Selected);
        return variant switch
        {
            IconButtonVariant.Filled => isSelected
                ? theme.OnPrimaryColor
                : isToggleable
                    ? theme.PrimaryColor
                    : theme.OnPrimaryColor,
            IconButtonVariant.FilledTonal => isSelected
                ? theme.OnSecondaryContainerColor
                : isToggleable
                    ? theme.OnSurfaceVariantColor
                    : theme.OnSecondaryContainerColor,
            IconButtonVariant.Outlined => isSelected
                ? theme.OnInverseSurfaceColor
                : theme.OnSurfaceVariantColor,
            _ => isSelected
                ? theme.PrimaryColor
                : theme.OnSurfaceVariantColor,
        };
    }

    private static Color ResolveDefaultBackgroundColor(
        ThemeData theme,
        IconButtonVariant variant,
        bool isToggleable,
        MaterialState states)
    {
        var isDisabled = states.HasFlag(MaterialState.Disabled);
        var isSelected = states.HasFlag(MaterialState.Selected);

        return variant switch
        {
            IconButtonVariant.Filled => isDisabled
                ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                : isSelected
                    ? theme.PrimaryColor
                    : isToggleable
                        ? theme.SurfaceContainerHighestColor
                        : theme.PrimaryColor,
            IconButtonVariant.FilledTonal => isDisabled
                ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                : isSelected
                    ? theme.SecondaryContainerColor
                    : isToggleable
                        ? theme.SurfaceContainerHighestColor
                        : theme.SecondaryContainerColor,
            IconButtonVariant.Outlined => isDisabled
                ? isSelected
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                    : Colors.Transparent
                : isSelected
                    ? theme.InverseSurfaceColor
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
                IconButtonVariant.Filled => theme.OnPrimaryColor,
                IconButtonVariant.FilledTonal => theme.OnSecondaryContainerColor,
                IconButtonVariant.Outlined => theme.OnInverseSurfaceColor,
                _ => theme.PrimaryColor,
            };

            var selectedFocusOpacity = variant == IconButtonVariant.Outlined ? 0.08 : 0.10;
            return ResolveStateLayerColor(selectedOverlay, states, selectedFocusOpacity);
        }

        if (variant == IconButtonVariant.Outlined)
        {
            if (states.HasFlag(MaterialState.Pressed))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.10);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceVariantColor, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceVariantColor, 0.08);
            }

            return Colors.Transparent;
        }

        var overlayColor = variant switch
        {
            IconButtonVariant.Filled => isToggleable ? theme.PrimaryColor : theme.OnPrimaryColor,
            IconButtonVariant.FilledTonal => isToggleable ? theme.OnSurfaceVariantColor : theme.OnSecondaryContainerColor,
            _ => theme.OnSurfaceVariantColor,
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
                MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12),
                1);
        }

        return new BorderSide(theme.OutlineColor, 1);
    }

    private static Color ResolveStateLayerColor(
        Color baseColor,
        MaterialState states,
        double focusedOpacity = 0.10)
    {
        if (states.HasFlag(MaterialState.Pressed))
        {
            return MaterialButtonCore.ApplyOpacity(baseColor, 0.10);
        }

        if (states.HasFlag(MaterialState.Hovered))
        {
            return MaterialButtonCore.ApplyOpacity(baseColor, 0.08);
        }

        if (states.HasFlag(MaterialState.Focused))
        {
            return MaterialButtonCore.ApplyOpacity(baseColor, focusedOpacity);
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
                ? disabledColor ?? enabledColor
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
                    ? MaterialButtonCore.ApplyOpacity(overlayFallback.Value, 0.10)
                    : null;
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                if (hoverColor.HasValue)
                {
                    return hoverColor.Value;
                }

                return overlayFallback.HasValue
                    ? MaterialButtonCore.ApplyOpacity(overlayFallback.Value, 0.08)
                    : null;
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                if (focusColor.HasValue)
                {
                    return focusColor.Value;
                }

                return overlayFallback.HasValue
                    ? MaterialButtonCore.ApplyOpacity(overlayFallback.Value, 0.10)
                    : null;
            }

            return null;
        });
    }
}
