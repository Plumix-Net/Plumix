using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/filled_button.dart

/// <summary>Dart parity: the private `_FilledButtonVariant`.</summary>
internal enum FilledButtonVariant
{
    Filled,
    Tonal,
}

public sealed class FilledButton : ButtonStyleButton
{
    public FilledButton(
        Widget child,
        Action? onPressed,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Clip clipBehavior = Clip.None,
        MaterialStatesController? statesController = null,
        Key? key = null) : this(
            child: child,
            onPressed: onPressed,
            onLongPress: onLongPress,
            onHover: onHover,
            onFocusChange: onFocusChange,
            style: style,
            focusNode: focusNode,
            autofocus: autofocus,
            clipBehavior: clipBehavior,
            statesController: statesController,
            variant: FilledButtonVariant.Filled,
            addPadding: false,
            key: key)
    {
    }

    private FilledButton(
        Widget? child,
        Action? onPressed,
        Action? onLongPress,
        Action<bool>? onHover,
        Action<bool>? onFocusChange,
        ButtonStyle? style,
        FocusNode? focusNode,
        bool autofocus,
        Clip clipBehavior,
        MaterialStatesController? statesController,
        FilledButtonVariant variant,
        bool addPadding,
        Key? key) : base(
            onPressed: onPressed,
            onLongPress: onLongPress,
            onHover: onHover,
            onFocusChange: onFocusChange,
            style: style,
            focusNode: focusNode,
            autofocus: autofocus,
            clipBehavior: clipBehavior,
            child: child,
            statesController: statesController,
            key: key)
    {
        Variant = variant;
        AddPadding = addPadding;
    }

    internal FilledButtonVariant Variant { get; }

    internal bool AddPadding { get; }

    /// <summary>Dart's `FilledButton.tonal`.</summary>
    public static FilledButton Tonal(
        Widget child,
        Action? onPressed,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Clip clipBehavior = Clip.None,
        MaterialStatesController? statesController = null,
        Key? key = null)
    {
        return new FilledButton(
            child: child,
            onPressed: onPressed,
            onLongPress: onLongPress,
            onHover: onHover,
            onFocusChange: onFocusChange,
            style: style,
            focusNode: focusNode,
            autofocus: autofocus,
            clipBehavior: clipBehavior,
            statesController: statesController,
            variant: FilledButtonVariant.Tonal,
            addPadding: false,
            key: key);
    }

    /// <summary>Dart's `FilledButton.icon`.</summary>
    public static FilledButton Icon(
        Widget label,
        Action? onPressed,
        Widget? icon = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Clip clipBehavior = Clip.None,
        MaterialStatesController? statesController = null,
        IconAlignment? iconAlignment = null,
        Key? key = null)
    {
        return IconVariant(
            label,
            onPressed,
            icon,
            onLongPress,
            onHover,
            onFocusChange,
            style,
            focusNode,
            autofocus,
            clipBehavior,
            statesController,
            iconAlignment,
            FilledButtonVariant.Filled,
            key);
    }

    /// <summary>Dart's `FilledButton.tonalIcon`.</summary>
    public static FilledButton TonalIcon(
        Widget label,
        Action? onPressed,
        Widget? icon = null,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Clip clipBehavior = Clip.None,
        MaterialStatesController? statesController = null,
        IconAlignment? iconAlignment = null,
        Key? key = null)
    {
        return IconVariant(
            label,
            onPressed,
            icon,
            onLongPress,
            onHover,
            onFocusChange,
            style,
            focusNode,
            autofocus,
            clipBehavior,
            statesController,
            iconAlignment,
            FilledButtonVariant.Tonal,
            key);
    }

    /// <summary>Dart's `FilledButton.styleFrom` (shared by the filled and tonal variants).</summary>
    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? iconColor = null,
        double? iconSize = null,
        IconAlignment? iconAlignment = null,
        Color? disabledIconColor = null,
        Color? overlayColor = null,
        double? elevation = null,
        TextStyle? textStyle = null,
        EdgeInsetsGeometry? padding = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        BorderSide? side = null,
        OutlinedBorder? shape = null,
        MouseCursor? enabledMouseCursor = null,
        MouseCursor? disabledMouseCursor = null,
        VisualDensity? visualDensity = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TimeSpan? animationDuration = null,
        bool? enableFeedback = null,
        AlignmentGeometry? alignment = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        ButtonLayerBuilder? backgroundBuilder = null,
        ButtonLayerBuilder? foregroundBuilder = null)
    {
        return new ButtonStyle(
            TextStyle: MaterialStateProperty<TextStyle?>.All(textStyle),
            BackgroundColor: DefaultColor(backgroundColor, disabledBackgroundColor),
            ForegroundColor: DefaultColor(foregroundColor, disabledForegroundColor),
            OverlayColor: DefaultOverlayColor(foregroundColor, overlayColor),
            ShadowColor: AllOrNullValue(shadowColor),
            SurfaceTintColor: AllOrNullValue(surfaceTintColor),
            Elevation: AllOrNullValue(elevation),
            Padding: AllOrNullValue(padding),
            MinimumSize: AllOrNullValue(minimumSize),
            FixedSize: AllOrNullValue(fixedSize),
            MaximumSize: AllOrNullValue(maximumSize),
            IconColor: DefaultColor(iconColor, disabledIconColor),
            IconSize: AllOrNullValue(iconSize),
            IconAlignment: iconAlignment,
            Side: AllOrNullValue(side),
            Shape: AllOrNull(shape),
            MouseCursor: StyleFromMouseCursor(enabledMouseCursor, disabledMouseCursor),
            VisualDensity: visualDensity,
            TapTargetSize: tapTargetSize,
            AnimationDuration: animationDuration,
            EnableFeedback: enableFeedback,
            Alignment: alignment,
            SplashFactory: splashFactory,
            BackgroundBuilder: backgroundBuilder,
            ForegroundBuilder: foregroundBuilder);
    }

    protected internal override ButtonStyle? ThemeStyleOf(BuildContext context)
    {
        return FilledButtonTheme.Of(context).Style;
    }

    protected internal override ButtonStyle DefaultStyleOf(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        ColorScheme colors = theme.ColorScheme;
        ButtonStyle buttonStyle = Variant == FilledButtonVariant.Tonal
            ? DefaultsM3(
                context,
                theme,
                colors,
                background: colors.SecondaryContainer,
                foreground: colors.OnSecondaryContainer)
            : DefaultsM3(context, theme, colors, background: colors.Primary, foreground: colors.OnPrimary);

        if (!AddPadding)
        {
            return buttonStyle;
        }

        double defaultFontSize = buttonStyle.TextStyle?.Resolve(MaterialState.None)?.FontSize ?? 14.0;
        double effectiveTextScale = EffectiveTextScale(context, defaultFontSize);
        EdgeInsetsGeometry iconPadding = theme.UseMaterial3
            ? ScaledPadding(
                EdgeInsetsGeometry.DirectionalOnly(start: 16, end: 24),
                EdgeInsetsGeometry.DirectionalOnly(start: 8, end: 12),
                EdgeInsetsGeometry.DirectionalOnly(start: 4, end: 6),
                effectiveTextScale)
            : ScaledPadding(
                EdgeInsetsGeometry.DirectionalOnly(start: 12, end: 16),
                EdgeInsetsGeometry.Symmetric(horizontal: 8),
                EdgeInsetsGeometry.DirectionalOnly(start: 8, end: 4),
                effectiveTextScale);
        return buttonStyle.CopyWith(padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(iconPadding));
    }

    private static FilledButton IconVariant(
        Widget label,
        Action? onPressed,
        Widget? icon,
        Action? onLongPress,
        Action<bool>? onHover,
        Action<bool>? onFocusChange,
        ButtonStyle? style,
        FocusNode? focusNode,
        bool autofocus,
        Clip clipBehavior,
        MaterialStatesController? statesController,
        IconAlignment? iconAlignment,
        FilledButtonVariant variant,
        Key? key)
    {
        return new FilledButton(
            child: icon is null
                ? label
                : new FilledButtonWithIconChild(
                    label: label,
                    icon: icon,
                    buttonStyle: style,
                    iconAlignment: iconAlignment),
            onPressed: onPressed,
            onLongPress: onLongPress,
            onHover: onHover,
            onFocusChange: onFocusChange,
            style: style,
            focusNode: focusNode,
            autofocus: autofocus,
            clipBehavior: clipBehavior,
            statesController: statesController,
            variant: variant,
            addPadding: icon is not null,
            key: key);
    }

    private static EdgeInsetsGeometry ScaledPaddingOf(BuildContext context, ThemeData theme)
    {
        double effectiveTextScale = EffectiveTextScale(context, theme.TextTheme.LabelLarge.FontSize);
        double padding1x = theme.UseMaterial3 ? 24.0 : 16.0;
        return ScaledPadding(
            EdgeInsetsGeometry.Symmetric(horizontal: padding1x),
            EdgeInsetsGeometry.Symmetric(horizontal: padding1x / 2),
            EdgeInsetsGeometry.Symmetric(horizontal: padding1x / 2 / 2),
            effectiveTextScale);
    }

    /// Dart's `_FilledButtonDefaultsM3` / `_FilledTonalButtonDefaultsM3`: identical except for the
    /// background, foreground, overlay and icon colours.
    private static ButtonStyle DefaultsM3(
        BuildContext context,
        ThemeData theme,
        ColorScheme colors,
        Color background,
        Color foreground)
    {
        return new ButtonStyle(
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? WithOpacity(colors.OnSurface, 0.12) : background),
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? WithOpacity(colors.OnSurface, 0.38) : foreground),
            OverlayColor: StateOverlay(foreground),
            ShadowColor: MaterialStateProperty<Color?>.All(colors.Shadow),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            Elevation: MaterialStateProperty<double?>.ResolveWith(states =>
                !states.HasFlag(MaterialState.Disabled) && states.HasFlag(MaterialState.Hovered) ? 1.0 : 0.0),
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(ScaledPaddingOf(context, theme)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64.0, 40.0)),
            MaximumSize: MaterialStateProperty<Size?>.All(InfiniteSize),
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? WithOpacity(colors.OnSurface, 0.38) : foreground),
            IconSize: MaterialStateProperty<double?>.All(18.0),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new StadiumBorder()),
            MouseCursor: AdaptiveClickableCursor,
            VisualDensity: theme.VisualDensity,
            TapTargetSize: theme.MaterialTapTargetSize,
            AnimationDuration: ButtonStyleState.ThemeChangeDuration,
            EnableFeedback: true,
            Alignment: Alignment.Center,
            SplashFactory: theme.SplashFactory);
    }
}

/// <summary>Dart parity: `_FilledButtonWithIconChild`.</summary>
internal sealed class FilledButtonWithIconChild : StatelessWidget
{
    public FilledButtonWithIconChild(
        Widget label,
        Widget icon,
        ButtonStyle? buttonStyle,
        IconAlignment? iconAlignment,
        Key? key = null) : base(key)
    {
        Label = label;
        Icon = icon;
        ButtonStyle = buttonStyle;
        IconAlignment = iconAlignment;
    }

    public Widget Label { get; }

    public Widget Icon { get; }

    public ButtonStyle? ButtonStyle { get; }

    public IconAlignment? IconAlignment { get; }

    public override Widget Build(BuildContext context)
    {
        IconAlignment effectiveIconAlignment = IconAlignment
                                               ?? FilledButtonTheme.Of(context).Style?.IconAlignment
                                               ?? ButtonStyle?.IconAlignment
                                               ?? ButtonStyleButton.DefaultIconAlignment;
        return ButtonStyleButton.BuildIconChild(context, ButtonStyle, Icon, Label, effectiveIconAlignment);
    }
}
