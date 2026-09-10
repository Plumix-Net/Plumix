using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/button_style.dart

public delegate Widget ButtonLayerBuilder(
    BuildContext context,
    IReadOnlySet<WidgetState> states,
    Widget? child);

/// <summary>
/// Dart parity: `ButtonStyle`. Field order, `CopyWith`, `Merge`, `Lerp`, `==`/`GetHashCode` and
/// `DebugFillProperties` follow `material_ui/lib/src/button_style.dart` exactly.
/// </summary>
public sealed record ButtonStyle(
    WidgetStateProperty<TextStyle?>? TextStyle = null,
    WidgetStateProperty<Color?>? BackgroundColor = null,
    WidgetStateProperty<Color?>? ForegroundColor = null,
    WidgetStateProperty<Color?>? OverlayColor = null,
    WidgetStateProperty<Color?>? ShadowColor = null,
    WidgetStateProperty<Color?>? SurfaceTintColor = null,
    WidgetStateProperty<double?>? Elevation = null,
    WidgetStateProperty<EdgeInsetsGeometry?>? Padding = null,
    WidgetStateProperty<Size?>? MinimumSize = null,
    WidgetStateProperty<Size?>? FixedSize = null,
    WidgetStateProperty<Size?>? MaximumSize = null,
    WidgetStateProperty<Color?>? IconColor = null,
    WidgetStateProperty<double?>? IconSize = null,
    IconAlignment? IconAlignment = null,
    WidgetStateProperty<BorderSide?>? Side = null,
    WidgetStateProperty<OutlinedBorder?>? Shape = null,
    WidgetStateProperty<MouseCursor?>? MouseCursor = null,
    VisualDensity? VisualDensity = null,
    MaterialTapTargetSize? TapTargetSize = null,
    TimeSpan? AnimationDuration = null,
    bool? EnableFeedback = null,
    AlignmentGeometry? Alignment = null,
    InteractiveInkFeatureFactory? SplashFactory = null,
    ButtonLayerBuilder? BackgroundBuilder = null,
    ButtonLayerBuilder? ForegroundBuilder = null) : IDiagnosticable
{
    /// Dart's `ButtonStyle.copyWith`: a null argument keeps the current value, it never clears one.
    public ButtonStyle CopyWith(
        WidgetStateProperty<TextStyle?>? textStyle = null,
        WidgetStateProperty<Color?>? backgroundColor = null,
        WidgetStateProperty<Color?>? foregroundColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        WidgetStateProperty<Color?>? shadowColor = null,
        WidgetStateProperty<Color?>? surfaceTintColor = null,
        WidgetStateProperty<double?>? elevation = null,
        WidgetStateProperty<EdgeInsetsGeometry?>? padding = null,
        WidgetStateProperty<Size?>? minimumSize = null,
        WidgetStateProperty<Size?>? fixedSize = null,
        WidgetStateProperty<Size?>? maximumSize = null,
        WidgetStateProperty<Color?>? iconColor = null,
        WidgetStateProperty<double?>? iconSize = null,
        IconAlignment? iconAlignment = null,
        WidgetStateProperty<BorderSide?>? side = null,
        WidgetStateProperty<OutlinedBorder?>? shape = null,
        WidgetStateProperty<MouseCursor?>? mouseCursor = null,
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
            TextStyle: textStyle ?? TextStyle,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            ForegroundColor: foregroundColor ?? ForegroundColor,
            OverlayColor: overlayColor ?? OverlayColor,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Elevation: elevation ?? Elevation,
            Padding: padding ?? Padding,
            MinimumSize: minimumSize ?? MinimumSize,
            FixedSize: fixedSize ?? FixedSize,
            MaximumSize: maximumSize ?? MaximumSize,
            IconColor: iconColor ?? IconColor,
            IconSize: iconSize ?? IconSize,
            IconAlignment: iconAlignment ?? IconAlignment,
            Side: side ?? Side,
            Shape: shape ?? Shape,
            MouseCursor: mouseCursor ?? MouseCursor,
            VisualDensity: visualDensity ?? VisualDensity,
            TapTargetSize: tapTargetSize ?? TapTargetSize,
            AnimationDuration: animationDuration ?? AnimationDuration,
            EnableFeedback: enableFeedback ?? EnableFeedback,
            Alignment: alignment ?? Alignment,
            SplashFactory: splashFactory ?? SplashFactory,
            BackgroundBuilder: backgroundBuilder ?? BackgroundBuilder,
            ForegroundBuilder: foregroundBuilder ?? ForegroundBuilder);
    }

    /// Dart's `ButtonStyle.merge`: this style wins on every field, `style` only fills its nulls.
    /// A null argument returns this same instance, exactly as Dart does.
    public ButtonStyle Merge(ButtonStyle? style)
    {
        if (style is null)
        {
            return this;
        }

        return CopyWith(
            textStyle: TextStyle ?? style.TextStyle,
            backgroundColor: BackgroundColor ?? style.BackgroundColor,
            foregroundColor: ForegroundColor ?? style.ForegroundColor,
            overlayColor: OverlayColor ?? style.OverlayColor,
            shadowColor: ShadowColor ?? style.ShadowColor,
            surfaceTintColor: SurfaceTintColor ?? style.SurfaceTintColor,
            elevation: Elevation ?? style.Elevation,
            padding: Padding ?? style.Padding,
            minimumSize: MinimumSize ?? style.MinimumSize,
            fixedSize: FixedSize ?? style.FixedSize,
            maximumSize: MaximumSize ?? style.MaximumSize,
            iconColor: IconColor ?? style.IconColor,
            iconSize: IconSize ?? style.IconSize,
            iconAlignment: IconAlignment ?? style.IconAlignment,
            side: Side ?? style.Side,
            shape: Shape ?? style.Shape,
            mouseCursor: MouseCursor ?? style.MouseCursor,
            visualDensity: VisualDensity ?? style.VisualDensity,
            tapTargetSize: TapTargetSize ?? style.TapTargetSize,
            animationDuration: AnimationDuration ?? style.AnimationDuration,
            enableFeedback: EnableFeedback ?? style.EnableFeedback,
            alignment: Alignment ?? style.Alignment,
            splashFactory: SplashFactory ?? style.SplashFactory,
            backgroundBuilder: BackgroundBuilder ?? style.BackgroundBuilder,
            foregroundBuilder: ForegroundBuilder ?? style.ForegroundBuilder);
    }

    /// Dart's `ButtonStyle.lerp`. `t` is not clamped, and `identical(a, b)` returns `a` — including
    /// the null/null case, which is how `lerp(null, null, t)` yields null.
    public static ButtonStyle? Lerp(ButtonStyle? a, ButtonStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ButtonStyle(
            TextStyle: WidgetStateProperty<TextStyle?>.Lerp(
                a?.TextStyle,
                b?.TextStyle,
                t,
                MaterialThemeLerp.TextStyle),
            BackgroundColor: LerpColor(a?.BackgroundColor, b?.BackgroundColor, t),
            ForegroundColor: LerpColor(a?.ForegroundColor, b?.ForegroundColor, t),
            OverlayColor: LerpColor(a?.OverlayColor, b?.OverlayColor, t),
            ShadowColor: LerpColor(a?.ShadowColor, b?.ShadowColor, t),
            SurfaceTintColor: LerpColor(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            Elevation: LerpDouble(a?.Elevation, b?.Elevation, t),
            Padding: WidgetStateProperty<EdgeInsetsGeometry?>.Lerp(
                a?.Padding,
                b?.Padding,
                t,
                MaterialThemeLerp.EdgeInsets),
            MinimumSize: LerpSize(a?.MinimumSize, b?.MinimumSize, t),
            FixedSize: LerpSize(a?.FixedSize, b?.FixedSize, t),
            MaximumSize: LerpSize(a?.MaximumSize, b?.MaximumSize, t),
            IconColor: LerpColor(a?.IconColor, b?.IconColor, t),
            IconSize: LerpDouble(a?.IconSize, b?.IconSize, t),
            IconAlignment: t < 0.5 ? a?.IconAlignment : b?.IconAlignment,
            Side: WidgetStateBorderSideLerp.Lerp(a?.Side, b?.Side, t),
            Shape: WidgetStateProperty<OutlinedBorder?>.Lerp(
                a?.Shape,
                b?.Shape,
                t,
                OutlinedBorder.Lerp),
            MouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            VisualDensity: t < 0.5 ? a?.VisualDensity : b?.VisualDensity,
            TapTargetSize: t < 0.5 ? a?.TapTargetSize : b?.TapTargetSize,
            AnimationDuration: t < 0.5 ? a?.AnimationDuration : b?.AnimationDuration,
            EnableFeedback: t < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            Alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, t),
            SplashFactory: t < 0.5 ? a?.SplashFactory : b?.SplashFactory,
            BackgroundBuilder: t < 0.5 ? a?.BackgroundBuilder : b?.BackgroundBuilder,
            ForegroundBuilder: t < 0.5 ? a?.ForegroundBuilder : b?.ForegroundBuilder);
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<TextStyle?>?>(
            "textStyle",
            TextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "backgroundColor",
            BackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "foregroundColor",
            ForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "overlayColor",
            OverlayColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "shadowColor",
            ShadowColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "surfaceTintColor",
            SurfaceTintColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<double?>?>(
            "elevation",
            Elevation,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<EdgeInsetsGeometry?>?>(
            "padding",
            Padding,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Size?>?>(
            "minimumSize",
            MinimumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Size?>?>(
            "fixedSize",
            FixedSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Size?>?>(
            "maximumSize",
            MaximumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>?>(
            "iconColor",
            IconColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<double?>?>(
            "iconSize",
            IconSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<IconAlignment?>(
            "iconAlignment",
            IconAlignment,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<BorderSide?>?>(
            "side",
            Side,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<OutlinedBorder?>?>(
            "shape",
            Shape,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<MouseCursor?>?>(
            "mouseCursor",
            MouseCursor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<VisualDensity?>(
            "visualDensity",
            VisualDensity,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialTapTargetSize?>(
            "tapTargetSize",
            TapTargetSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TimeSpan?>(
            "animationDuration",
            AnimationDuration,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>(
            "enableFeedback",
            EnableFeedback,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>(
            "alignment",
            Alignment,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ButtonLayerBuilder?>(
            "backgroundBuilder",
            BackgroundBuilder,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ButtonLayerBuilder?>(
            "foregroundBuilder",
            ForegroundBuilder,
            defaultValue: nullDefault));
    }

    internal Color? ResolveForegroundColor(IReadOnlySet<WidgetState> states) => ForegroundColor?.Resolve(states);

    internal Color? ResolveBackgroundColor(IReadOnlySet<WidgetState> states) => BackgroundColor?.Resolve(states);

    internal Color? ResolveOverlayColor(IReadOnlySet<WidgetState> states) => OverlayColor?.Resolve(states);

    internal Color? ResolveShadowColor(IReadOnlySet<WidgetState> states) => ShadowColor?.Resolve(states);

    internal Color? ResolveSurfaceTintColor(IReadOnlySet<WidgetState> states) => SurfaceTintColor?.Resolve(states);

    internal double? ResolveElevation(IReadOnlySet<WidgetState> states) => Elevation?.Resolve(states);

    internal Color? ResolveIconColor(IReadOnlySet<WidgetState> states) => IconColor?.Resolve(states);

    internal double? ResolveIconSize(IReadOnlySet<WidgetState> states) => IconSize?.Resolve(states);

    internal BorderSide? ResolveSide(IReadOnlySet<WidgetState> states) => Side?.Resolve(states);

    internal EdgeInsetsGeometry? ResolvePadding(IReadOnlySet<WidgetState> states) => Padding?.Resolve(states);

    internal OutlinedBorder? ResolveShape(IReadOnlySet<WidgetState> states) => Shape?.Resolve(states);

    internal Size? ResolveMinimumSize(IReadOnlySet<WidgetState> states) => MinimumSize?.Resolve(states);

    internal Size? ResolveFixedSize(IReadOnlySet<WidgetState> states) => FixedSize?.Resolve(states);

    internal Size? ResolveMaximumSize(IReadOnlySet<WidgetState> states) => MaximumSize?.Resolve(states);

    internal MaterialTapTargetSize? ResolveTapTargetSize() => TapTargetSize;

    internal IconAlignment? ResolveIconAlignment() => IconAlignment;

    internal TextStyle? ResolveTextStyle(IReadOnlySet<WidgetState> states) => TextStyle?.Resolve(states);

    internal MouseCursor? ResolveMouseCursor(IReadOnlySet<WidgetState> states) => MouseCursor?.Resolve(states);

    private static WidgetStateProperty<Color?>? LerpColor(
        WidgetStateProperty<Color?>? a,
        WidgetStateProperty<Color?>? b,
        double t)
    {
        return WidgetStateProperty<Color?>.Lerp(a, b, t, MaterialThemeLerp.Color);
    }

    private static WidgetStateProperty<double?>? LerpDouble(
        WidgetStateProperty<double?>? a,
        WidgetStateProperty<double?>? b,
        double t)
    {
        return WidgetStateProperty<double?>.Lerp(a, b, t, MaterialThemeLerp.Double);
    }

    private static WidgetStateProperty<Size?>? LerpSize(
        WidgetStateProperty<Size?>? a,
        WidgetStateProperty<Size?>? b,
        double t)
    {
        return WidgetStateProperty<Size?>.Lerp(a, b, t, MaterialThemeLerp.Size);
    }
}

/// <summary>
/// Dart parity: `WidgetStateBorderSide.lerp` and its private `_LerpSides` resolver
/// (`flutter/packages/flutter/lib/src/widgets/widget_state.dart`). A side that resolves to null on
/// one end is synthesized as a zero-width, zero-alpha side of the other end's color, so a border
/// fades in or out instead of snapping.
/// </summary>
internal static class WidgetStateBorderSideLerp
{
    public static WidgetStateProperty<BorderSide?>? Lerp(
        WidgetStateProperty<BorderSide?>? a,
        WidgetStateProperty<BorderSide?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return WidgetStateProperty<BorderSide?>.ResolveWith(states =>
        {
            BorderSide? resolvedA = a?.Resolve(states);
            BorderSide? resolvedB = b?.Resolve(states);
            if (!resolvedA.HasValue && !resolvedB.HasValue)
            {
                return null;
            }

            if (!resolvedA.HasValue)
            {
                return BorderSide.Lerp(Faded(resolvedB!.Value), resolvedB.Value, t);
            }

            if (!resolvedB.HasValue)
            {
                return BorderSide.Lerp(resolvedA.Value, Faded(resolvedA.Value), t);
            }

            return BorderSide.Lerp(resolvedA.Value, resolvedB.Value, t);
        });
    }

    private static BorderSide Faded(BorderSide side)
    {
        Color color = side.Color;
        return new BorderSide(Color.FromArgb(0, color.R, color.G, color.B), 0.0);
    }
}
