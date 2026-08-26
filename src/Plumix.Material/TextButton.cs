using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/text_button.dart

public sealed class TextButton : ButtonStyleButton
{
    public TextButton(
        Widget child,
        Action? onPressed,
        Action? onLongPress = null,
        Action<bool>? onHover = null,
        Action<bool>? onFocusChange = null,
        ButtonStyle? style = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Clip? clipBehavior = null,
        MaterialStatesController? statesController = null,
        bool? isSemanticButton = true,
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
            isSemanticButton: isSemanticButton,
            addPadding: false,
            key: key)
    {
    }

    private TextButton(
        Widget? child,
        Action? onPressed,
        Action? onLongPress,
        Action<bool>? onHover,
        Action<bool>? onFocusChange,
        ButtonStyle? style,
        FocusNode? focusNode,
        bool autofocus,
        Clip? clipBehavior,
        MaterialStatesController? statesController,
        bool? isSemanticButton,
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
            isSemanticButton: isSemanticButton,
            key: key)
    {
        AddPadding = addPadding;
    }

    /// Dart's private `_addPadding`: true when built through `TextButton.icon` with a non-null icon,
    /// which swaps in the icon-specific padding profile.
    internal bool AddPadding { get; }

    /// <summary>Dart's `TextButton.icon`.</summary>
    public static TextButton Icon(
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
        return new TextButton(
            child: icon is null
                ? label
                : new TextButtonWithIconChild(
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
            isSemanticButton: true,
            addPadding: icon is not null,
            key: key);
    }

    /// <summary>Dart's `TextButton.styleFrom`.</summary>
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
            TextStyle: AllOrNull(textStyle),
            BackgroundColor: SingleValueOrDefaultColor(backgroundColor, disabledBackgroundColor),
            ForegroundColor: DefaultColor(foregroundColor, disabledForegroundColor),
            OverlayColor: DefaultOverlayColor(foregroundColor, overlayColor),
            ShadowColor: AllOrNullValue(shadowColor),
            SurfaceTintColor: AllOrNullValue(surfaceTintColor),
            Elevation: AllOrNullValue(elevation),
            Padding: AllOrNullValue(padding),
            MinimumSize: AllOrNullValue(minimumSize),
            FixedSize: AllOrNullValue(fixedSize),
            MaximumSize: AllOrNullValue(maximumSize),
            IconColor: SingleValueOrDefaultColor(iconColor, disabledIconColor),
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
        return TextButtonTheme.Of(context).Style;
    }

    protected internal override ButtonStyle DefaultStyleOf(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        ColorScheme colors = theme.ColorScheme;
        ButtonStyle buttonStyle = theme.UseMaterial3
            ? TextButtonDefaultsM3(context, theme, colors)
            : StyleFrom(
                foregroundColor: colors.Primary,
                disabledForegroundColor: WithOpacity(colors.OnSurface, 0.38),
                backgroundColor: Colors.Transparent,
                disabledBackgroundColor: Colors.Transparent,
                shadowColor: theme.ShadowColor,
                elevation: 0,
                textStyle: theme.TextTheme.LabelLarge,
                padding: ScaledPaddingOf(context, theme),
                minimumSize: new Size(64, 36),
                maximumSize: InfiniteSize,
                shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4)),
                enabledMouseCursor: PlatformDefaults.IsWeb ? SystemMouseCursors.Click : SystemMouseCursors.Basic,
                disabledMouseCursor: SystemMouseCursors.Basic,
                visualDensity: theme.VisualDensity,
                tapTargetSize: theme.MaterialTapTargetSize,
                animationDuration: ButtonStyleState.ThemeChangeDuration,
                enableFeedback: true,
                alignment: Alignment.Center,
                splashFactory: InkRipple.SplashFactory);

        if (!AddPadding)
        {
            return buttonStyle;
        }

        double defaultFontSize = buttonStyle.TextStyle?.Resolve(MaterialState.None)?.FontSize ?? 14.0;
        double effectiveTextScale = EffectiveTextScale(context, defaultFontSize);
        EdgeInsetsGeometry iconPadding = ScaledPadding(
            theme.UseMaterial3
                ? EdgeInsetsGeometry.DirectionalOnly(start: 12, top: 8, end: 16, bottom: 8)
                : EdgeInsetsGeometry.All(8),
            EdgeInsetsGeometry.Symmetric(horizontal: 4),
            EdgeInsetsGeometry.Symmetric(horizontal: 4),
            effectiveTextScale);
        return buttonStyle.CopyWith(padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(iconPadding));
    }

    private static EdgeInsetsGeometry ScaledPaddingOf(BuildContext context, ThemeData theme)
    {
        double effectiveTextScale = EffectiveTextScale(context, theme.TextTheme.LabelLarge.FontSize);
        return ScaledPadding(
            theme.UseMaterial3
                ? EdgeInsetsGeometry.Symmetric(horizontal: 12, vertical: 8)
                : EdgeInsetsGeometry.All(8),
            EdgeInsetsGeometry.Symmetric(horizontal: 8),
            EdgeInsetsGeometry.Symmetric(horizontal: 4),
            effectiveTextScale);
    }

    /// Dart's `_TextButtonDefaultsM3`. `ButtonStyle` is a sealed record in C#, so the token table is
    /// materialized as a value rather than as a lazily-resolving subclass; the values are identical.
    private static ButtonStyle TextButtonDefaultsM3(BuildContext context, ThemeData theme, ColorScheme colors)
    {
        return new ButtonStyle(
            TextStyle: MaterialStateProperty<TextStyle?>.All(theme.TextTheme.LabelLarge),
            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? WithOpacity(colors.OnSurface, 0.38) : colors.Primary),
            OverlayColor: StateOverlay(colors.Primary),
            ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            Elevation: MaterialStateProperty<double?>.All(0.0),
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(ScaledPaddingOf(context, theme)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(64.0, 40.0)),
            MaximumSize: MaterialStateProperty<Size?>.All(InfiniteSize),
            IconColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled) ? WithOpacity(colors.OnSurface, 0.38) : colors.Primary),
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

/// <summary>Dart parity: `_TextButtonWithIconChild`.</summary>
internal sealed class TextButtonWithIconChild : StatelessWidget
{
    public TextButtonWithIconChild(
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
                                               ?? TextButtonTheme.Of(context).Style?.IconAlignment
                                               ?? ButtonStyle?.IconAlignment
                                               ?? ButtonStyleButton.DefaultIconAlignment;
        return ButtonStyleButton.BuildIconChild(context, ButtonStyle, Icon, Label, effectiveIconAlignment);
    }
}
