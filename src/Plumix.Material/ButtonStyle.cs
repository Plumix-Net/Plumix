using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/button_style.dart
// Dart parity source: material_ui/lib/src/material_state.dart

[Flags]
public enum MaterialState
{
    None = 0,
    Hovered = 1 << 0,
    Focused = 1 << 1,
    Pressed = 1 << 2,
    Disabled = 1 << 3,
    Selected = 1 << 4,
    Error = 1 << 5,
    Dragged = 1 << 6
}

public delegate Widget ButtonLayerBuilder(BuildContext context, MaterialState states, Widget? child);

public class MaterialStatesController : ChangeNotifier
{
    private MaterialState _value;

    public MaterialStatesController(MaterialState value = MaterialState.None)
    {
        _value = value;
    }

    public MaterialState Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            NotifyListeners();
        }
    }

    public void Update(MaterialState state, bool add)
    {
        Value = add ? Value | state : Value & ~state;
    }
}

public sealed class WidgetStatesController : MaterialStatesController
{
    public WidgetStatesController(MaterialState value = MaterialState.None) : base(value)
    {
    }
}

/// Bridges Material's `[Flags] MaterialState` onto the core `IReadOnlySet&lt;WidgetState&gt;` the
/// framework's state-resolving values (`WidgetStateColor`, `WidgetStateTextStyle`,
/// `WidgetStateBorderSide`) are declared against. Dart has one `WidgetState` set everywhere.
public static class MaterialStateSet
{
    public static IReadOnlySet<WidgetState> Of(MaterialState states)
    {
        var set = new HashSet<WidgetState>();
        if (states.HasFlag(MaterialState.Hovered)) set.Add(WidgetState.Hovered);
        if (states.HasFlag(MaterialState.Focused)) set.Add(WidgetState.Focused);
        if (states.HasFlag(MaterialState.Pressed)) set.Add(WidgetState.Pressed);
        if (states.HasFlag(MaterialState.Disabled)) set.Add(WidgetState.Disabled);
        if (states.HasFlag(MaterialState.Selected)) set.Add(WidgetState.Selected);
        if (states.HasFlag(MaterialState.Dragged)) set.Add(WidgetState.Dragged);
        if (states.HasFlag(MaterialState.Error)) set.Add(WidgetState.Error);
        return set;
    }

    public static MaterialState Flags(IReadOnlySet<WidgetState> states)
    {
        MaterialState result = MaterialState.None;
        if (states.Contains(WidgetState.Hovered)) result |= MaterialState.Hovered;
        if (states.Contains(WidgetState.Focused)) result |= MaterialState.Focused;
        if (states.Contains(WidgetState.Pressed)) result |= MaterialState.Pressed;
        if (states.Contains(WidgetState.Disabled)) result |= MaterialState.Disabled;
        if (states.Contains(WidgetState.Selected)) result |= MaterialState.Selected;
        if (states.Contains(WidgetState.Dragged)) result |= MaterialState.Dragged;
        if (states.Contains(WidgetState.Error)) result |= MaterialState.Error;
        return result;
    }
}

public abstract class MaterialStateProperty<T> : WidgetStateProperty<T>
{
    public abstract T Resolve(MaterialState states);

    public sealed override T Resolve(IReadOnlySet<WidgetState> states)
    {
        MaterialState materialStates = MaterialState.None;
        foreach (WidgetState state in states)
        {
            materialStates |= state switch
            {
                WidgetState.Hovered => MaterialState.Hovered,
                WidgetState.Focused => MaterialState.Focused,
                WidgetState.Pressed => MaterialState.Pressed,
                WidgetState.Disabled => MaterialState.Disabled,
                WidgetState.Selected => MaterialState.Selected,
                WidgetState.Dragged => MaterialState.Dragged,
                WidgetState.Error => MaterialState.Error,
                _ => MaterialState.None,
            };
        }

        return Resolve(materialStates);
    }

    public static implicit operator MaterialStateProperty<T>(T value)
    {
        return All(value);
    }

    public new static MaterialStateProperty<T> All(T value)
    {
        return new MaterialStatePropertyAll<T>(value);
    }

    public static MaterialStateProperty<T> ResolveWith(Func<MaterialState, T> resolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return new MaterialStatePropertyResolver<T>(resolver);
    }

    public static MaterialStateProperty<T>? Lerp(
        MaterialStateProperty<T>? a,
        MaterialStateProperty<T>? b,
        double t,
        Func<T, T, double, T> lerpFunction)
    {
        ArgumentNullException.ThrowIfNull(lerpFunction);
        if (a is null && b is null)
        {
            return null;
        }

        return ResolveWith(states => lerpFunction(
            a is null ? default! : a.Resolve(states),
            b is null ? default! : b.Resolve(states),
            t));
    }
}

public sealed class MaterialStatePropertyAll<T> : MaterialStateProperty<T>
{
    public MaterialStatePropertyAll(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public override T Resolve(MaterialState states)
    {
        return Value;
    }

    // Dart's `WidgetStatePropertyAll` compares by runtime type and value, so two independently
    // constructed `WidgetStatePropertyAll(3.0)` are equal — theme equality depends on it.
    public override bool Equals(object? obj)
    {
        return obj is MaterialStatePropertyAll<T> other
               && other.GetType() == GetType()
               && EqualityComparer<T>.Default.Equals(other.Value, Value);
    }

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}

internal sealed class MaterialStatePropertyResolver<T> : MaterialStateProperty<T>
{
    private readonly Func<MaterialState, T> _resolver;

    public MaterialStatePropertyResolver(Func<MaterialState, T> resolver)
    {
        _resolver = resolver;
    }

    public override T Resolve(MaterialState states)
    {
        return _resolver(states);
    }
}

/// <summary>
/// Dart parity: `ButtonStyle`. Field order, `CopyWith`, `Merge`, `Lerp`, `==`/`GetHashCode` and
/// `DebugFillProperties` follow `material_ui/lib/src/button_style.dart` exactly.
/// </summary>
public sealed record ButtonStyle(
    MaterialStateProperty<TextStyle?>? TextStyle = null,
    MaterialStateProperty<Color?>? BackgroundColor = null,
    MaterialStateProperty<Color?>? ForegroundColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialStateProperty<Color?>? ShadowColor = null,
    MaterialStateProperty<Color?>? SurfaceTintColor = null,
    MaterialStateProperty<double?>? Elevation = null,
    MaterialStateProperty<EdgeInsetsGeometry?>? Padding = null,
    MaterialStateProperty<Size?>? MinimumSize = null,
    MaterialStateProperty<Size?>? FixedSize = null,
    MaterialStateProperty<Size?>? MaximumSize = null,
    MaterialStateProperty<Color?>? IconColor = null,
    MaterialStateProperty<double?>? IconSize = null,
    IconAlignment? IconAlignment = null,
    MaterialStateProperty<BorderSide?>? Side = null,
    MaterialStateProperty<OutlinedBorder?>? Shape = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
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
        MaterialStateProperty<TextStyle?>? textStyle = null,
        MaterialStateProperty<Color?>? backgroundColor = null,
        MaterialStateProperty<Color?>? foregroundColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<Color?>? shadowColor = null,
        MaterialStateProperty<Color?>? surfaceTintColor = null,
        MaterialStateProperty<double?>? elevation = null,
        MaterialStateProperty<EdgeInsetsGeometry?>? padding = null,
        MaterialStateProperty<Size?>? minimumSize = null,
        MaterialStateProperty<Size?>? fixedSize = null,
        MaterialStateProperty<Size?>? maximumSize = null,
        MaterialStateProperty<Color?>? iconColor = null,
        MaterialStateProperty<double?>? iconSize = null,
        IconAlignment? iconAlignment = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<OutlinedBorder?>? shape = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
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
            TextStyle: MaterialStateProperty<TextStyle?>.Lerp(
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
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.Lerp(
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
            Shape: MaterialStateProperty<OutlinedBorder?>.Lerp(
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
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<TextStyle?>?>(
            "textStyle",
            TextStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "backgroundColor",
            BackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "foregroundColor",
            ForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "overlayColor",
            OverlayColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "shadowColor",
            ShadowColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "surfaceTintColor",
            SurfaceTintColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<double?>?>(
            "elevation",
            Elevation,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<EdgeInsetsGeometry?>?>(
            "padding",
            Padding,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "minimumSize",
            MinimumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "fixedSize",
            FixedSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "maximumSize",
            MaximumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "iconColor",
            IconColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<double?>?>(
            "iconSize",
            IconSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<IconAlignment?>(
            "iconAlignment",
            IconAlignment,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<BorderSide?>?>(
            "side",
            Side,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<OutlinedBorder?>?>(
            "shape",
            Shape,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<MouseCursor?>?>(
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

    internal Color? ResolveForegroundColor(MaterialState states) => ForegroundColor?.Resolve(states);

    internal Color? ResolveBackgroundColor(MaterialState states) => BackgroundColor?.Resolve(states);

    internal Color? ResolveOverlayColor(MaterialState states) => OverlayColor?.Resolve(states);

    internal Color? ResolveShadowColor(MaterialState states) => ShadowColor?.Resolve(states);

    internal Color? ResolveSurfaceTintColor(MaterialState states) => SurfaceTintColor?.Resolve(states);

    internal double? ResolveElevation(MaterialState states) => Elevation?.Resolve(states);

    internal Color? ResolveIconColor(MaterialState states) => IconColor?.Resolve(states);

    internal double? ResolveIconSize(MaterialState states) => IconSize?.Resolve(states);

    internal BorderSide? ResolveSide(MaterialState states) => Side?.Resolve(states);

    internal EdgeInsetsGeometry? ResolvePadding(MaterialState states) => Padding?.Resolve(states);

    internal OutlinedBorder? ResolveShape(MaterialState states) => Shape?.Resolve(states);

    internal Size? ResolveMinimumSize(MaterialState states) => MinimumSize?.Resolve(states);

    internal Size? ResolveFixedSize(MaterialState states) => FixedSize?.Resolve(states);

    internal Size? ResolveMaximumSize(MaterialState states) => MaximumSize?.Resolve(states);

    internal MaterialTapTargetSize? ResolveTapTargetSize() => TapTargetSize;

    internal IconAlignment? ResolveIconAlignment() => IconAlignment;

    internal TextStyle? ResolveTextStyle(MaterialState states) => TextStyle?.Resolve(states);

    internal MouseCursor? ResolveMouseCursor(MaterialState states) => MouseCursor?.Resolve(states);

    private static MaterialStateProperty<Color?>? LerpColor(
        MaterialStateProperty<Color?>? a,
        MaterialStateProperty<Color?>? b,
        double t)
    {
        return MaterialStateProperty<Color?>.Lerp(a, b, t, MaterialThemeLerp.Color);
    }

    private static MaterialStateProperty<double?>? LerpDouble(
        MaterialStateProperty<double?>? a,
        MaterialStateProperty<double?>? b,
        double t)
    {
        return MaterialStateProperty<double?>.Lerp(a, b, t, MaterialThemeLerp.Double);
    }

    private static MaterialStateProperty<Size?>? LerpSize(
        MaterialStateProperty<Size?>? a,
        MaterialStateProperty<Size?>? b,
        double t)
    {
        return MaterialStateProperty<Size?>.Lerp(a, b, t, MaterialThemeLerp.Size);
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
    public static MaterialStateProperty<BorderSide?>? Lerp(
        MaterialStateProperty<BorderSide?>? a,
        MaterialStateProperty<BorderSide?>? b,
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

        return MaterialStateProperty<BorderSide?>.ResolveWith(states =>
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
