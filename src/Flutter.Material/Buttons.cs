using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/button_style_button.dart; flutter/packages/flutter/lib/src/material/text_button.dart; flutter/packages/flutter/lib/src/material/elevated_button.dart; flutter/packages/flutter/lib/src/material/filled_button.dart; flutter/packages/flutter/lib/src/material/outlined_button.dart (approximate)

public sealed class TextButton : StatelessWidget
{
    public TextButton(
        Widget child,
        Action? onPressed,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        BorderRadius? borderRadius = null,
        double minWidth = 64,
        double minHeight = 40,
        ButtonStyle? style = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        Padding = padding;
        BorderRadius = borderRadius;
        MinWidth = minWidth;
        MinHeight = minHeight;
        Style = style;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public Color? ForegroundColor { get; }

    public Color? BackgroundColor { get; }

    public Thickness? Padding { get; }

    public BorderRadius? BorderRadius { get; }

    public double MinWidth { get; }

    public double MinHeight { get; }

    public ButtonStyle? Style { get; }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? overlayColor = null,
        Color? splashColor = null,
        BorderSide? side = null,
        Thickness? padding = null,
        BorderRadius? shape = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        Alignment? alignment = null,
        TextStyle? textStyle = null)
    {
        return new ButtonStyle(
            ForegroundColor: foregroundColor.HasValue || disabledForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledForegroundColor
                        : foregroundColor)
                : null,
            BackgroundColor: backgroundColor.HasValue || disabledBackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledBackgroundColor
                        : backgroundColor)
                : null,
            OverlayColor: MaterialButtonCore.CreateStyleFromOverlayResolver(foregroundColor, overlayColor),
            SplashColor: MaterialButtonCore.CreateStyleFromSplashResolver(foregroundColor, overlayColor, splashColor),
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
            TextStyle: textStyle is null ? null : MaterialStateProperty<TextStyle?>.All(textStyle));
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var mergedStyle = MaterialButtonCore.ComposeStyles(
            defaults: CreateDefaultStyle(theme, MinWidth, MinHeight),
            themeStyle: TextButtonTheme.Of(context).Style,
            widgetStyle: Style,
            legacyOverrides: CreateLegacyStyleOverrides(theme));

        return new MaterialButtonCore(
            child: Child,
            onPressed: OnPressed,
            style: mergedStyle);
    }

    private static ButtonStyle CreateDefaultStyle(ThemeData theme, double minWidth, double minHeight)
    {
        var stateColor = theme.PrimaryColor;
        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : stateColor),
            BackgroundColor: MaterialStateProperty<Color?>.All(null),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(stateColor),
            SplashColor: null,
            Side: MaterialStateProperty<BorderSide?>.All(null),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(12, 8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(Flutter.Rendering.BorderRadius.Circular(20)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(minWidth, minHeight)));
    }

    private ButtonStyle? CreateLegacyStyleOverrides(ThemeData theme)
    {
        if (!ForegroundColor.HasValue
            && !BackgroundColor.HasValue
            && !Padding.HasValue
            && !BorderRadius.HasValue)
        {
            return null;
        }

        return new ButtonStyle(
            ForegroundColor: ForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                        : ForegroundColor.Value)
                : null,
            BackgroundColor: BackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(BackgroundColor.Value, 0.12)
                        : BackgroundColor.Value)
                : null,
            OverlayColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultOverlayResolver(ForegroundColor.Value)
                : null,
            SplashColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultSplashResolver(ForegroundColor.Value)
                : null,
            Padding: Padding.HasValue
                ? MaterialStateProperty<Thickness?>.All(Padding.Value)
                : null,
            Shape: BorderRadius.HasValue
                ? MaterialStateProperty<BorderRadius?>.All(BorderRadius.Value)
                : null);
    }
}

public sealed class ElevatedButton : StatelessWidget
{
    public ElevatedButton(
        Widget child,
        Action? onPressed,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        BorderRadius? borderRadius = null,
        double minWidth = 64,
        double minHeight = 40,
        ButtonStyle? style = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        Padding = padding;
        BorderRadius = borderRadius;
        MinWidth = minWidth;
        MinHeight = minHeight;
        Style = style;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public Color? ForegroundColor { get; }

    public Color? BackgroundColor { get; }

    public Thickness? Padding { get; }

    public BorderRadius? BorderRadius { get; }

    public double MinWidth { get; }

    public double MinHeight { get; }

    public ButtonStyle? Style { get; }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? overlayColor = null,
        Color? splashColor = null,
        BorderSide? side = null,
        Thickness? padding = null,
        BorderRadius? shape = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        Alignment? alignment = null,
        TextStyle? textStyle = null)
    {
        return new ButtonStyle(
            ForegroundColor: foregroundColor.HasValue || disabledForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledForegroundColor
                        : foregroundColor)
                : null,
            BackgroundColor: backgroundColor.HasValue || disabledBackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledBackgroundColor
                        : backgroundColor)
                : null,
            OverlayColor: MaterialButtonCore.CreateStyleFromOverlayResolver(foregroundColor, overlayColor),
            SplashColor: MaterialButtonCore.CreateStyleFromSplashResolver(foregroundColor, overlayColor, splashColor),
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
            TextStyle: textStyle is null ? null : MaterialStateProperty<TextStyle?>.All(textStyle));
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var mergedStyle = MaterialButtonCore.ComposeStyles(
            defaults: CreateDefaultStyle(theme, MinWidth, MinHeight),
            themeStyle: ElevatedButtonTheme.Of(context).Style,
            widgetStyle: Style,
            legacyOverrides: CreateLegacyStyleOverrides(theme));

        return new MaterialButtonCore(
            child: Child,
            onPressed: OnPressed,
            style: mergedStyle);
    }

    private static ButtonStyle CreateDefaultStyle(ThemeData theme, double minWidth, double minHeight)
    {
        var stateColor = theme.PrimaryColor;
        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : stateColor),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                    : theme.SurfaceContainerLowColor),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(stateColor),
            SplashColor: null,
            Side: MaterialStateProperty<BorderSide?>.All(null),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(24, 8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(Flutter.Rendering.BorderRadius.Circular(20)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(minWidth, minHeight)));
    }

    private ButtonStyle? CreateLegacyStyleOverrides(ThemeData theme)
    {
        if (!ForegroundColor.HasValue
            && !BackgroundColor.HasValue
            && !Padding.HasValue
            && !BorderRadius.HasValue)
        {
            return null;
        }

        return new ButtonStyle(
            ForegroundColor: ForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                        : ForegroundColor.Value)
                : null,
            BackgroundColor: BackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                        : BackgroundColor.Value)
                : null,
            OverlayColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultOverlayResolver(ForegroundColor.Value)
                : null,
            SplashColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultSplashResolver(ForegroundColor.Value)
                : null,
            Padding: Padding.HasValue
                ? MaterialStateProperty<Thickness?>.All(Padding.Value)
                : null,
            Shape: BorderRadius.HasValue
                ? MaterialStateProperty<BorderRadius?>.All(BorderRadius.Value)
                : null);
    }
}

public sealed class FilledButton : StatelessWidget
{
    public FilledButton(
        Widget child,
        Action? onPressed,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        BorderRadius? borderRadius = null,
        double minWidth = 64,
        double minHeight = 40,
        ButtonStyle? style = null,
        Key? key = null) : this(
            child: child,
            onPressed: onPressed,
            isTonal: false,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            padding: padding,
            borderRadius: borderRadius,
            minWidth: minWidth,
            minHeight: minHeight,
            style: style,
            key: key)
    {
    }

    private FilledButton(
        Widget child,
        Action? onPressed,
        bool isTonal,
        Color? foregroundColor,
        Color? backgroundColor,
        Thickness? padding,
        BorderRadius? borderRadius,
        double minWidth,
        double minHeight,
        ButtonStyle? style,
        Key? key) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        IsTonal = isTonal;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        Padding = padding;
        BorderRadius = borderRadius;
        MinWidth = minWidth;
        MinHeight = minHeight;
        Style = style;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public bool IsTonal { get; }

    public Color? ForegroundColor { get; }

    public Color? BackgroundColor { get; }

    public Thickness? Padding { get; }

    public BorderRadius? BorderRadius { get; }

    public double MinWidth { get; }

    public double MinHeight { get; }

    public ButtonStyle? Style { get; }

    public static FilledButton Tonal(
        Widget child,
        Action? onPressed,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Thickness? padding = null,
        BorderRadius? borderRadius = null,
        double minWidth = 64,
        double minHeight = 40,
        ButtonStyle? style = null,
        Key? key = null)
    {
        return new FilledButton(
            child: child,
            onPressed: onPressed,
            isTonal: true,
            foregroundColor: foregroundColor,
            backgroundColor: backgroundColor,
            padding: padding,
            borderRadius: borderRadius,
            minWidth: minWidth,
            minHeight: minHeight,
            style: style,
            key: key);
    }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? overlayColor = null,
        Color? splashColor = null,
        BorderSide? side = null,
        Thickness? padding = null,
        BorderRadius? shape = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        Alignment? alignment = null,
        TextStyle? textStyle = null)
    {
        return new ButtonStyle(
            ForegroundColor: foregroundColor.HasValue || disabledForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledForegroundColor
                        : foregroundColor)
                : null,
            BackgroundColor: backgroundColor.HasValue || disabledBackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledBackgroundColor
                        : backgroundColor)
                : null,
            OverlayColor: MaterialButtonCore.CreateStyleFromOverlayResolver(foregroundColor, overlayColor),
            SplashColor: MaterialButtonCore.CreateStyleFromSplashResolver(foregroundColor, overlayColor, splashColor),
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
            TextStyle: textStyle is null ? null : MaterialStateProperty<TextStyle?>.All(textStyle));
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var mergedStyle = MaterialButtonCore.ComposeStyles(
            defaults: CreateDefaultStyle(theme, MinWidth, MinHeight, IsTonal),
            themeStyle: FilledButtonTheme.Of(context).Style,
            widgetStyle: Style,
            legacyOverrides: CreateLegacyStyleOverrides(theme));

        return new MaterialButtonCore(
            child: Child,
            onPressed: OnPressed,
            style: mergedStyle);
    }

    private static ButtonStyle CreateDefaultStyle(
        ThemeData theme,
        double minWidth,
        double minHeight,
        bool isTonal)
    {
        var enabledForeground = isTonal
            ? theme.OnSecondaryContainerColor
            : theme.OnPrimaryColor;
        var enabledBackground = isTonal
            ? theme.SecondaryContainerColor
            : theme.PrimaryColor;

        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : enabledForeground),
            BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                    : enabledBackground),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(enabledForeground),
            SplashColor: null,
            Side: MaterialStateProperty<BorderSide?>.All(null),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(24, 8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(Flutter.Rendering.BorderRadius.Circular(20)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(minWidth, minHeight)));
    }

    private ButtonStyle? CreateLegacyStyleOverrides(ThemeData theme)
    {
        if (!ForegroundColor.HasValue
            && !BackgroundColor.HasValue
            && !Padding.HasValue
            && !BorderRadius.HasValue)
        {
            return null;
        }

        return new ButtonStyle(
            ForegroundColor: ForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                        : ForegroundColor.Value)
                : null,
            BackgroundColor: BackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)
                        : BackgroundColor.Value)
                : null,
            OverlayColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultOverlayResolver(ForegroundColor.Value)
                : null,
            SplashColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultSplashResolver(ForegroundColor.Value)
                : null,
            Padding: Padding.HasValue
                ? MaterialStateProperty<Thickness?>.All(Padding.Value)
                : null,
            Shape: BorderRadius.HasValue
                ? MaterialStateProperty<BorderRadius?>.All(BorderRadius.Value)
                : null);
    }
}

public sealed class OutlinedButton : StatelessWidget
{
    public OutlinedButton(
        Widget child,
        Action? onPressed,
        Color? foregroundColor = null,
        Color? borderColor = null,
        double borderWidth = 1,
        Color? backgroundColor = null,
        Thickness? padding = null,
        BorderRadius? borderRadius = null,
        double minWidth = 64,
        double minHeight = 40,
        ButtonStyle? style = null,
        Key? key = null) : base(key)
    {
        if (double.IsNaN(borderWidth) || double.IsInfinity(borderWidth) || borderWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(borderWidth), "Border width must be non-negative and finite.");
        }

        Child = child;
        OnPressed = onPressed;
        ForegroundColor = foregroundColor;
        BorderColor = borderColor;
        BorderWidth = borderWidth;
        BackgroundColor = backgroundColor;
        Padding = padding;
        BorderRadius = borderRadius;
        MinWidth = minWidth;
        MinHeight = minHeight;
        Style = style;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public Color? ForegroundColor { get; }

    public Color? BorderColor { get; }

    public double BorderWidth { get; }

    public Color? BackgroundColor { get; }

    public Thickness? Padding { get; }

    public BorderRadius? BorderRadius { get; }

    public double MinWidth { get; }

    public double MinHeight { get; }

    public ButtonStyle? Style { get; }

    public static ButtonStyle StyleFrom(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? disabledForegroundColor = null,
        Color? disabledBackgroundColor = null,
        Color? overlayColor = null,
        Color? splashColor = null,
        BorderSide? side = null,
        Thickness? padding = null,
        BorderRadius? shape = null,
        Size? minimumSize = null,
        Size? fixedSize = null,
        Size? maximumSize = null,
        Alignment? alignment = null,
        TextStyle? textStyle = null)
    {
        return new ButtonStyle(
            ForegroundColor: foregroundColor.HasValue || disabledForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledForegroundColor
                        : foregroundColor)
                : null,
            BackgroundColor: backgroundColor.HasValue || disabledBackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? disabledBackgroundColor
                        : backgroundColor)
                : null,
            OverlayColor: MaterialButtonCore.CreateStyleFromOverlayResolver(foregroundColor, overlayColor),
            SplashColor: MaterialButtonCore.CreateStyleFromSplashResolver(foregroundColor, overlayColor, splashColor),
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
            TextStyle: textStyle is null ? null : MaterialStateProperty<TextStyle?>.All(textStyle));
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var mergedStyle = MaterialButtonCore.ComposeStyles(
            defaults: CreateDefaultStyle(theme, MinWidth, MinHeight),
            themeStyle: OutlinedButtonTheme.Of(context).Style,
            widgetStyle: Style,
            legacyOverrides: CreateLegacyStyleOverrides(theme));

        return new MaterialButtonCore(
            child: Child,
            onPressed: OnPressed,
            style: mergedStyle);
    }

    private static ButtonStyle CreateDefaultStyle(ThemeData theme, double minWidth, double minHeight)
    {
        var stateColor = theme.PrimaryColor;
        return new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : stateColor),
            BackgroundColor: MaterialStateProperty<Color?>.All(null),
            OverlayColor: MaterialButtonCore.CreateDefaultOverlayResolver(stateColor),
            SplashColor: null,
            Side: MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? new BorderSide(MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12), 1)
                    : new BorderSide(theme.OutlineColor, 1)),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(24, 8)),
            Shape: MaterialStateProperty<BorderRadius?>.All(Flutter.Rendering.BorderRadius.Circular(20)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(minWidth, minHeight)));
    }

    private ButtonStyle? CreateLegacyStyleOverrides(ThemeData theme)
    {
        var hasSideOverride = BorderColor.HasValue || Math.Abs(BorderWidth - 1) > 0.0001;
        if (!ForegroundColor.HasValue
            && !BackgroundColor.HasValue
            && !Padding.HasValue
            && !BorderRadius.HasValue
            && !hasSideOverride)
        {
            return null;
        }

        var activeSideColor = BorderColor ?? theme.OutlineColor;
        return new ButtonStyle(
            ForegroundColor: ForegroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                        : ForegroundColor.Value)
                : null,
            BackgroundColor: BackgroundColor.HasValue
                ? MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(BackgroundColor.Value, 0.12)
                        : BackgroundColor.Value)
                : null,
            OverlayColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultOverlayResolver(ForegroundColor.Value)
                : null,
            SplashColor: ForegroundColor.HasValue
                ? MaterialButtonCore.CreateDefaultSplashResolver(ForegroundColor.Value)
                : null,
            Side: hasSideOverride
                ? MaterialStateProperty<BorderSide?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? new BorderSide(MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12), BorderWidth)
                        : new BorderSide(activeSideColor, BorderWidth))
                : null,
            Padding: Padding.HasValue
                ? MaterialStateProperty<Thickness?>.All(Padding.Value)
                : null,
            Shape: BorderRadius.HasValue
                ? MaterialStateProperty<BorderRadius?>.All(BorderRadius.Value)
                : null);
    }
}

internal sealed class MaterialButtonCore : StatefulWidget
{
    public MaterialButtonCore(
        Widget child,
        Action? onPressed,
        ButtonStyle style,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public ButtonStyle Style { get; }

    public override State CreateState()
    {
        return new MaterialButtonCoreState();
    }

    internal static ButtonStyle ComposeStyles(
        ButtonStyle? defaults,
        ButtonStyle? themeStyle,
        ButtonStyle? widgetStyle,
        ButtonStyle? legacyOverrides)
    {
        return new ButtonStyle(
            ForegroundColor: ComposeStateProperty<Color?>(
                legacyOverrides?.ForegroundColor,
                widgetStyle?.ForegroundColor,
                themeStyle?.ForegroundColor,
                defaults?.ForegroundColor),
            BackgroundColor: ComposeStateProperty<Color?>(
                legacyOverrides?.BackgroundColor,
                widgetStyle?.BackgroundColor,
                themeStyle?.BackgroundColor,
                defaults?.BackgroundColor),
            OverlayColor: ComposeStateProperty<Color?>(
                legacyOverrides?.OverlayColor,
                widgetStyle?.OverlayColor,
                themeStyle?.OverlayColor,
                defaults?.OverlayColor),
            SplashColor: ComposeStateProperty<Color?>(
                legacyOverrides?.SplashColor,
                widgetStyle?.SplashColor,
                themeStyle?.SplashColor,
                defaults?.SplashColor),
            Side: ComposeStateProperty<BorderSide?>(
                legacyOverrides?.Side,
                widgetStyle?.Side,
                themeStyle?.Side,
                defaults?.Side),
            Padding: ComposeStateProperty<Thickness?>(
                legacyOverrides?.Padding,
                widgetStyle?.Padding,
                themeStyle?.Padding,
                defaults?.Padding),
            Shape: ComposeStateProperty<BorderRadius?>(
                legacyOverrides?.Shape,
                widgetStyle?.Shape,
                themeStyle?.Shape,
                defaults?.Shape),
            MinimumSize: ComposeStateProperty<Size?>(
                legacyOverrides?.MinimumSize,
                widgetStyle?.MinimumSize,
                themeStyle?.MinimumSize,
                defaults?.MinimumSize),
            FixedSize: ComposeStateProperty<Size?>(
                legacyOverrides?.FixedSize,
                widgetStyle?.FixedSize,
                themeStyle?.FixedSize,
                defaults?.FixedSize),
            MaximumSize: ComposeStateProperty<Size?>(
                legacyOverrides?.MaximumSize,
                widgetStyle?.MaximumSize,
                themeStyle?.MaximumSize,
                defaults?.MaximumSize),
            Alignment: legacyOverrides?.Alignment
                       ?? widgetStyle?.Alignment
                       ?? themeStyle?.Alignment
                       ?? defaults?.Alignment,
            TextStyle: ComposeStateProperty<TextStyle?>(
                legacyOverrides?.TextStyle,
                widgetStyle?.TextStyle,
                themeStyle?.TextStyle,
                defaults?.TextStyle));
    }

    private static MaterialStateProperty<T>? ComposeStateProperty<T>(
        params MaterialStateProperty<T>?[] layers)
    {
        var hasAny = false;
        foreach (var layer in layers)
        {
            if (layer is not null)
            {
                hasAny = true;
                break;
            }
        }

        if (!hasAny)
        {
            return null;
        }

        return MaterialStateProperty<T>.ResolveWith(states =>
        {
            foreach (var layer in layers)
            {
                if (layer is null)
                {
                    continue;
                }

                var resolved = layer.Resolve(states);
                if (resolved is not null)
                {
                    return resolved;
                }
            }

            return default!;
        });
    }

    internal static MaterialStateProperty<Color?> CreateDefaultOverlayResolver(Color stateColor)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return ApplyOpacity(stateColor, 0.10);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return ApplyOpacity(stateColor, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return ApplyOpacity(stateColor, 0.10);
            }

            return null;
        });
    }

    internal static MaterialStateProperty<Color?> CreateExplicitOverlayResolver(Color overlayColor)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            if (states.HasFlag(MaterialState.Pressed)
                || states.HasFlag(MaterialState.Focused)
                || states.HasFlag(MaterialState.Hovered))
            {
                return overlayColor;
            }

            return null;
        });
    }

    internal static MaterialStateProperty<Color?>? CreateStyleFromOverlayResolver(
        Color? foregroundColor,
        Color? overlayColor)
    {
        if (overlayColor.HasValue)
        {
            if (overlayColor.Value.A == 0)
            {
                return MaterialStateProperty<Color?>.All(overlayColor.Value);
            }

            return CreateDefaultOverlayResolver(overlayColor.Value);
        }

        return foregroundColor.HasValue
            ? CreateDefaultOverlayResolver(foregroundColor.Value)
            : null;
    }

    internal static MaterialStateProperty<Color?> CreateDefaultSplashResolver(Color stateColor)
    {
        return CreateDefaultOverlayResolver(stateColor);
    }

    internal static MaterialStateProperty<Color?> CreateExplicitSplashResolver(Color splashColor)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            return splashColor;
        });
    }

    internal static MaterialStateProperty<Color?>? CreateStyleFromSplashResolver(
        Color? foregroundColor,
        Color? overlayColor,
        Color? splashColor)
    {
        if (splashColor.HasValue)
        {
            return CreateExplicitSplashResolver(splashColor.Value);
        }

        if (overlayColor.HasValue)
        {
            if (overlayColor.Value.A == 0)
            {
                return MaterialStateProperty<Color?>.All(overlayColor.Value);
            }

            return CreateDefaultOverlayResolver(overlayColor.Value);
        }

        return foregroundColor.HasValue
            ? CreateDefaultOverlayResolver(foregroundColor.Value)
            : null;
    }

    private sealed class MaterialButtonCoreState : State
    {
        private static readonly Point CenterSplashOrigin = new(double.NaN, double.NaN);

        private bool _isPressed;
        private bool _hasFocus;
        private bool _isHovered;
        private bool _suppressFocusOverlay;
        private bool _isKeyboardPressed;
        private bool _isSplashActive;
        private double _splashProgress;
        private Point _splashOrigin = CenterSplashOrigin;
        private Color? _splashBaseColor;
        private FocusNode? _focusNode;
        private AnimationController? _splashController;
        private AnimationController? _keyboardPressController;

        private MaterialButtonCore CurrentWidget => (MaterialButtonCore)StateWidget;

        private bool Enabled => CurrentWidget.OnPressed != null;

        public override void InitState()
        {
            _focusNode = new FocusNode();
            _focusNode.AddListener(HandleFocusChanged);
            _hasFocus = _focusNode.HasFocus;

            _splashController = new AnimationController(TimeSpan.FromMilliseconds(225))
            {
                Curve = Curves.EaseOut
            };
            _splashController.Changed += HandleSplashTick;
            _splashController.Completed += HandleSplashCompleted;

            _keyboardPressController = new AnimationController(TimeSpan.FromMilliseconds(100));
            _keyboardPressController.Completed += HandleKeyboardPressCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            if (!Enabled && _isPressed)
            {
                _isPressed = false;
            }

            if (!Enabled && _isHovered)
            {
                _isHovered = false;
            }

            if (!Enabled && _suppressFocusOverlay)
            {
                _suppressFocusOverlay = false;
            }

            if (!Enabled && _isSplashActive)
            {
                _isSplashActive = false;
                _splashProgress = 0;
                _splashBaseColor = null;
                _splashController?.Stop();
            }

            if (!Enabled && _isKeyboardPressed)
            {
                _isKeyboardPressed = false;
                _keyboardPressController?.Stop();
            }

            if (!Enabled && _focusNode != null && _focusNode.HasFocus)
            {
                _focusNode.Unfocus();
            }
        }

        public override void Dispose()
        {
            if (_focusNode != null)
            {
                _focusNode.RemoveListener(HandleFocusChanged);
                _focusNode.Dispose();
                _focusNode = null;
            }

            if (_splashController != null)
            {
                _splashController.Changed -= HandleSplashTick;
                _splashController.Completed -= HandleSplashCompleted;
                _splashController.Dispose();
                _splashController = null;
            }

            if (_keyboardPressController != null)
            {
                _keyboardPressController.Completed -= HandleKeyboardPressCompleted;
                _keyboardPressController.Dispose();
                _keyboardPressController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var enabled = Enabled;
            var style = widget.Style;
            var baseStates = BuildMaterialStates(enabled, includeFocus: true);
            var overlayStates = BuildMaterialStates(enabled, includeFocus: !_suppressFocusOverlay);

            var foreground = ResolveForegroundColor(style, baseStates);
            var background = ResolveBackgroundColor(style, baseStates, overlayStates);
            var splashColor = ResolveSplashColor();
            var border = style.ResolveSide(baseStates);
            var padding = style.ResolvePadding(baseStates) ?? default;
            var borderRadius = style.ResolveShape(baseStates) ?? Flutter.Rendering.BorderRadius.Zero;
            var minimumSize = style.ResolveMinimumSize(baseStates) ?? new Size(64, 40);
            ValidateMinimumSize(minimumSize);
            var maximumSize = style.ResolveMaximumSize(baseStates) ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
            ValidateMaximumSize(maximumSize);
            var fixedSize = style.ResolveFixedSize(baseStates);
            ValidateFixedSize(fixedSize);
            var effectiveConstraints = CreateEffectiveConstraints(minimumSize, maximumSize, fixedSize);
            var alignment = style.Alignment ?? Alignment.Center;
            var resolvedTextStyle = style.ResolveTextStyle(baseStates);
            var textStyle = MergeTextStyle(
                new TextStyle(
                    Color: foreground,
                    FontSize: 14,
                    FontWeight: FontWeight.Medium),
                resolvedTextStyle);

            Widget content = new DefaultTextStyle(
                style: textStyle,
                child: new Align(
                    alignment: alignment,
                    widthFactor: 1,
                    heightFactor: 1,
                    child: widget.Child));

            content = new Container(
                padding: padding,
                child: content);

            content = new ConstrainedBox(
                constraints: effectiveConstraints,
                child: content);

            content = new InkSplash(
                splashColor: splashColor,
                splashOrigin: _splashOrigin,
                splashProgress: _splashProgress,
                clipToBounds: false,
                child: content);

            content = new ClipRRect(
                borderRadius: borderRadius,
                child: content);

            content = new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: background,
                    Border: border,
                    BorderRadius: borderRadius),
                child: content);

            if (!enabled)
            {
                return content;
            }

            content = new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: widget.OnPressed,
                child: content);

            content = new Listener(
                behavior: HitTestBehavior.Opaque,
                onPointerDown: HandlePointerDown,
                onPointerUp: HandlePointerUp,
                onPointerCancel: HandlePointerCancel,
                onPointerEnter: _ => SetHovered(true),
                onPointerExit: _ => SetHovered(false),
                child: content);

            return new Focus(
                focusNode: _focusNode,
                canRequestFocus: true,
                onKeyEvent: HandleKeyEvent,
                child: content);
        }

        private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
        {
            if (!IsActivateKey(@event))
            {
                return KeyEventResult.Ignored;
            }

            if (!Enabled)
            {
                return KeyEventResult.Handled;
            }

            if (@event.IsDown)
            {
                SetFocusOverlaySuppressed(false);
                StartKeyboardPress();
                StartSplash(CenterSplashOrigin);
                CurrentWidget.OnPressed?.Invoke();
            }

            return KeyEventResult.Handled;
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            SetPressed(true, suppressFocusOverlay: true);
            StartSplash(@event.LocalPosition);
        }

        private void HandlePointerUp(PointerUpEvent @event)
        {
            SetPressed(false);
        }

        private void HandlePointerCancel(PointerCancelEvent @event)
        {
            SetPressed(false);
        }

        private void HandleFocusChanged()
        {
            var hasFocus = _focusNode?.HasFocus ?? false;
            var shouldClearFocusSuppression = !hasFocus && _suppressFocusOverlay;
            if (_hasFocus == hasFocus && !shouldClearFocusSuppression)
            {
                return;
            }

            SetState(() =>
            {
                _hasFocus = hasFocus;
                if (!hasFocus)
                {
                    _suppressFocusOverlay = false;
                }
            });
        }

        private void SetPressed(bool value, bool suppressFocusOverlay = false)
        {
            if (!Enabled)
            {
                return;
            }

            var nextSuppressFocusOverlay = _suppressFocusOverlay || suppressFocusOverlay;
            if (_isPressed == value && _suppressFocusOverlay == nextSuppressFocusOverlay)
            {
                return;
            }

            SetState(() =>
            {
                _isPressed = value;
                _suppressFocusOverlay = nextSuppressFocusOverlay;
            });
        }

        private void SetHovered(bool value)
        {
            if (!Enabled || _isHovered == value)
            {
                return;
            }

            SetState(() => _isHovered = value);
        }

        private void SetFocusOverlaySuppressed(bool value)
        {
            if (_suppressFocusOverlay == value)
            {
                return;
            }

            SetState(() => _suppressFocusOverlay = value);
        }

        private void StartKeyboardPress()
        {
            if (!Enabled || _keyboardPressController is null)
            {
                return;
            }

            if (!_isKeyboardPressed)
            {
                SetState(() => _isKeyboardPressed = true);
            }

            _keyboardPressController.Forward(0);
        }

        private void StartSplash(Point origin)
        {
            if (!Enabled || _splashController is null)
            {
                return;
            }

            var splashStates = BuildMaterialStates(enabled: true, includeFocus: !_suppressFocusOverlay);
            var style = CurrentWidget.Style;
            var splashBaseColor = style.ResolveSplashColor(splashStates)
                                  ?? style.ResolveOverlayColor(splashStates);

            SetState(() =>
            {
                _isSplashActive = true;
                _splashProgress = 0;
                _splashOrigin = origin;
                _splashBaseColor = splashBaseColor;
            });

            _splashController.Forward(0);
        }

        private MaterialState BuildMaterialStates(bool enabled, bool includeFocus)
        {
            if (!enabled)
            {
                return MaterialState.Disabled;
            }

            var states = MaterialState.None;
            if (_isPressed)
            {
                states |= MaterialState.Pressed;
            }

            if (_isKeyboardPressed)
            {
                states |= MaterialState.Pressed;
            }

            if (_isHovered)
            {
                states |= MaterialState.Hovered;
            }

            if (includeFocus && _hasFocus)
            {
                states |= MaterialState.Focused;
            }

            return states;
        }

        private Color ResolveForegroundColor(ButtonStyle style, MaterialState states)
        {
            var color = style.ResolveForegroundColor(states);
            if (!color.HasValue && states.HasFlag(MaterialState.Disabled))
            {
                color = style.ResolveForegroundColor(MaterialState.None);
            }

            return color ?? Colors.Black;
        }

        private static Color? ResolveBackgroundColor(
            ButtonStyle style,
            MaterialState baseStates,
            MaterialState overlayStates)
        {
            var background = style.ResolveBackgroundColor(baseStates);
            var overlay = HasOverlayState(overlayStates)
                ? style.ResolveOverlayColor(overlayStates)
                : null;

            if (!background.HasValue)
            {
                return overlay;
            }

            if (!overlay.HasValue)
            {
                return background;
            }

            return BlendColorOverlay(background.Value, overlay.Value);
        }

        private static bool HasOverlayState(MaterialState states)
        {
            return states.HasFlag(MaterialState.Pressed)
                   || states.HasFlag(MaterialState.Hovered)
                   || states.HasFlag(MaterialState.Focused);
        }

        private Color? ResolveSplashColor()
        {
            if (!_isSplashActive)
            {
                return null;
            }

            if (!_splashBaseColor.HasValue)
            {
                return null;
            }

            var fade = ResolveSplashFade(_splashProgress);
            var opacity = Math.Clamp((_splashBaseColor.Value.A / 255.0) * fade, 0, 1);
            if (opacity <= 0.001)
            {
                return null;
            }

            var alpha = (byte)Math.Clamp((int)(opacity * 255), 0, 255);
            return Color.FromArgb(alpha, _splashBaseColor.Value.R, _splashBaseColor.Value.G, _splashBaseColor.Value.B);
        }

        private static void ValidateMinimumSize(Size minimumSize)
        {
            if (double.IsNaN(minimumSize.Width) || double.IsInfinity(minimumSize.Width) || minimumSize.Width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSize), "Minimum width must be non-negative and finite.");
            }

            if (double.IsNaN(minimumSize.Height) || double.IsInfinity(minimumSize.Height) || minimumSize.Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSize), "Minimum height must be non-negative and finite.");
            }
        }

        private static void ValidateMaximumSize(Size maximumSize)
        {
            if (double.IsNaN(maximumSize.Width) || maximumSize.Width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSize), "Maximum width must be non-negative and not NaN.");
            }

            if (double.IsNaN(maximumSize.Height) || maximumSize.Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSize), "Maximum height must be non-negative and not NaN.");
            }
        }

        private static void ValidateFixedSize(Size? fixedSize)
        {
            if (!fixedSize.HasValue)
            {
                return;
            }

            var value = fixedSize.Value;
            if (double.IsNaN(value.Width) || value.Width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedSize), "Fixed width must be non-negative and not NaN.");
            }

            if (double.IsNaN(value.Height) || value.Height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedSize), "Fixed height must be non-negative and not NaN.");
            }
        }

        private static BoxConstraints CreateEffectiveConstraints(
            Size minimumSize,
            Size maximumSize,
            Size? fixedSize)
        {
            var normalizedMaxWidth = double.IsPositiveInfinity(maximumSize.Width)
                ? double.PositiveInfinity
                : Math.Max(maximumSize.Width, minimumSize.Width);
            var normalizedMaxHeight = double.IsPositiveInfinity(maximumSize.Height)
                ? double.PositiveInfinity
                : Math.Max(maximumSize.Height, minimumSize.Height);

            var effectiveConstraints = new BoxConstraints(
                MinWidth: minimumSize.Width,
                MaxWidth: normalizedMaxWidth,
                MinHeight: minimumSize.Height,
                MaxHeight: normalizedMaxHeight);

            if (!fixedSize.HasValue)
            {
                return effectiveConstraints;
            }

            var constrainedFixedSize = effectiveConstraints.Constrain(fixedSize.Value);
            if (double.IsFinite(constrainedFixedSize.Width))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinWidth = constrainedFixedSize.Width,
                    MaxWidth = constrainedFixedSize.Width
                };
            }

            if (double.IsFinite(constrainedFixedSize.Height))
            {
                effectiveConstraints = effectiveConstraints with
                {
                    MinHeight = constrainedFixedSize.Height,
                    MaxHeight = constrainedFixedSize.Height
                };
            }

            return effectiveConstraints;
        }

        private static TextStyle MergeTextStyle(TextStyle baseStyle, TextStyle? style)
        {
            if (style is null)
            {
                return baseStyle;
            }

            return new TextStyle(
                FontFamily: style.FontFamily ?? baseStyle.FontFamily,
                FontSize: style.FontSize ?? baseStyle.FontSize,
                Color: style.Color ?? baseStyle.Color,
                FontWeight: style.FontWeight ?? baseStyle.FontWeight,
                FontStyle: style.FontStyle ?? baseStyle.FontStyle,
                Height: style.Height ?? baseStyle.Height,
                LetterSpacing: style.LetterSpacing ?? baseStyle.LetterSpacing);
        }

        private static double ResolveSplashFade(double progress)
        {
            var clamped = Math.Clamp(progress, 0, 1);
            const double fadeStart = 0.72;
            if (clamped <= fadeStart)
            {
                return 1;
            }

            var tailProgress = (clamped - fadeStart) / (1 - fadeStart);
            return Math.Clamp(1 - tailProgress, 0, 1);
        }

        private void HandleSplashTick()
        {
            if (!_isSplashActive || _splashController is null)
            {
                return;
            }

            SetState(() =>
            {
                _splashProgress = Math.Clamp(_splashController.Evaluate(), 0, 1);
            });
        }

        private void HandleSplashCompleted()
        {
            if (!_isSplashActive)
            {
                return;
            }

            SetState(() =>
            {
                _isSplashActive = false;
                _splashProgress = 0;
                _splashOrigin = CenterSplashOrigin;
                _splashBaseColor = null;
            });
        }

        private void HandleKeyboardPressCompleted()
        {
            if (!_isKeyboardPressed)
            {
                return;
            }

            SetState(() => _isKeyboardPressed = false);
        }

        private static bool IsActivateKey(string key)
        {
            return string.Equals(key, "Enter", StringComparison.Ordinal)
                   || string.Equals(key, "Return", StringComparison.Ordinal)
                   || string.Equals(key, "NumPadEnter", StringComparison.Ordinal)
                   || string.Equals(key, "NumpadEnter", StringComparison.Ordinal)
                   || string.Equals(key, "Space", StringComparison.Ordinal)
                   || string.Equals(key, "Spacebar", StringComparison.Ordinal);
        }

        private static bool IsActivateKey(KeyEvent @event)
        {
            if (@event.IsShiftPressed
                || @event.IsControlPressed
                || @event.IsAltPressed
                || @event.IsMetaPressed)
            {
                return false;
            }

            return IsActivateKey(@event.Key);
        }
    }

    private static Color BlendColorOverlay(Color baseColor, Color overlayColor)
    {
        static byte Blend(byte from, byte to, double t)
        {
            return (byte)Math.Clamp((int)(from + ((to - from) * t)), 0, 255);
        }

        var clampedOpacity = Math.Clamp(overlayColor.A / 255.0, 0, 1);
        return Color.FromArgb(
            baseColor.A,
            Blend(baseColor.R, overlayColor.R, clampedOpacity),
            Blend(baseColor.G, overlayColor.G, clampedOpacity),
            Blend(baseColor.B, overlayColor.B, clampedOpacity));
    }

    internal static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp((int)(255 * opacity), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
