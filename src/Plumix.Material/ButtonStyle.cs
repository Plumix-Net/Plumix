using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): material_ui/lib/src/button_style.dart (approximate)

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

public enum IconAlignment
{
    Start,
    End
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

public sealed record ButtonStyle(
    MaterialStateProperty<Color?>? ForegroundColor = null,
    MaterialStateProperty<Color?>? BackgroundColor = null,
    MaterialStateProperty<Color?>? ShadowColor = null,
    MaterialStateProperty<Color?>? SurfaceTintColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialStateProperty<Color?>? SplashColor = null,
    MaterialStateProperty<double?>? Elevation = null,
    MaterialStateProperty<Color?>? IconColor = null,
    MaterialStateProperty<double?>? IconSize = null,
    MaterialStateProperty<BorderSide?>? Side = null,
    MaterialStateProperty<Thickness?>? Padding = null,
    MaterialStateProperty<OutlinedBorder?>? Shape = null,
    MaterialStateProperty<Size?>? MinimumSize = null,
    MaterialStateProperty<Size?>? FixedSize = null,
    MaterialStateProperty<Size?>? MaximumSize = null,
    AlignmentGeometry? Alignment = null,
    IconAlignment? IconAlignment = null,
    MaterialTapTargetSize? TapTargetSize = null,
    MaterialStateProperty<TextStyle?>? TextStyle = null,
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    VisualDensity? VisualDensity = null,
    TimeSpan? AnimationDuration = null,
    bool? EnableFeedback = null,
    InteractiveInkFeatureFactory? SplashFactory = null,
    ButtonLayerBuilder? BackgroundBuilder = null,
    ButtonLayerBuilder? ForegroundBuilder = null)
{
    public static ButtonStyle? Lerp(ButtonStyle? a, ButtonStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }
        if (a is null && b is null)
        {
            return null;
        }

        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new ButtonStyle(
            ForegroundColor: LerpColorProperty(a?.ForegroundColor, b?.ForegroundColor, clampedT),
            BackgroundColor: LerpColorProperty(a?.BackgroundColor, b?.BackgroundColor, clampedT),
            ShadowColor: LerpColorProperty(a?.ShadowColor, b?.ShadowColor, clampedT),
            SurfaceTintColor: LerpColorProperty(a?.SurfaceTintColor, b?.SurfaceTintColor, clampedT),
            OverlayColor: LerpColorProperty(a?.OverlayColor, b?.OverlayColor, clampedT),
            SplashColor: LerpColorProperty(a?.SplashColor, b?.SplashColor, clampedT),
            Elevation: LerpDoubleProperty(a?.Elevation, b?.Elevation, clampedT),
            IconColor: LerpColorProperty(a?.IconColor, b?.IconColor, clampedT),
            IconSize: LerpDoubleProperty(a?.IconSize, b?.IconSize, clampedT),
            Side: LerpBorderSideProperty(a?.Side, b?.Side, clampedT),
            Padding: LerpThicknessProperty(a?.Padding, b?.Padding, clampedT),
            Shape: LerpBorderRadiusProperty(a?.Shape, b?.Shape, clampedT),
            MinimumSize: LerpSizeProperty(a?.MinimumSize, b?.MinimumSize, clampedT),
            FixedSize: LerpSizeProperty(a?.FixedSize, b?.FixedSize, clampedT),
            MaximumSize: LerpSizeProperty(a?.MaximumSize, b?.MaximumSize, clampedT),
            Alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, clampedT),
            IconAlignment: clampedT < 0.5 ? a?.IconAlignment : b?.IconAlignment,
            TapTargetSize: clampedT < 0.5 ? a?.TapTargetSize : b?.TapTargetSize,
            TextStyle: LerpTextStyleProperty(a?.TextStyle, b?.TextStyle, clampedT),
            MouseCursor: clampedT < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            VisualDensity: clampedT < 0.5 ? a?.VisualDensity : b?.VisualDensity,
            AnimationDuration: clampedT < 0.5
                ? a?.AnimationDuration
                : b?.AnimationDuration,
            EnableFeedback: clampedT < 0.5 ? a?.EnableFeedback : b?.EnableFeedback,
            SplashFactory: clampedT < 0.5 ? a?.SplashFactory : b?.SplashFactory,
            BackgroundBuilder: clampedT < 0.5 ? a?.BackgroundBuilder : b?.BackgroundBuilder,
            ForegroundBuilder: clampedT < 0.5 ? a?.ForegroundBuilder : b?.ForegroundBuilder);
    }

    public ButtonStyle Merge(ButtonStyle? style)
    {
        if (style is null)
        {
            return this;
        }

        return this with
        {
            ForegroundColor = ForegroundColor ?? style.ForegroundColor,
            BackgroundColor = BackgroundColor ?? style.BackgroundColor,
            ShadowColor = ShadowColor ?? style.ShadowColor,
            SurfaceTintColor = SurfaceTintColor ?? style.SurfaceTintColor,
            OverlayColor = OverlayColor ?? style.OverlayColor,
            SplashColor = SplashColor ?? style.SplashColor,
            Elevation = Elevation ?? style.Elevation,
            IconColor = IconColor ?? style.IconColor,
            IconSize = IconSize ?? style.IconSize,
            Side = Side ?? style.Side,
            Padding = Padding ?? style.Padding,
            Shape = Shape ?? style.Shape,
            MinimumSize = MinimumSize ?? style.MinimumSize,
            FixedSize = FixedSize ?? style.FixedSize,
            MaximumSize = MaximumSize ?? style.MaximumSize,
            Alignment = Alignment ?? style.Alignment,
            IconAlignment = IconAlignment ?? style.IconAlignment,
            TapTargetSize = TapTargetSize ?? style.TapTargetSize,
            TextStyle = TextStyle ?? style.TextStyle,
            MouseCursor = MouseCursor ?? style.MouseCursor,
            VisualDensity = VisualDensity ?? style.VisualDensity,
            AnimationDuration = AnimationDuration ?? style.AnimationDuration,
            EnableFeedback = EnableFeedback ?? style.EnableFeedback,
            SplashFactory = SplashFactory ?? style.SplashFactory,
            BackgroundBuilder = BackgroundBuilder ?? style.BackgroundBuilder,
            ForegroundBuilder = ForegroundBuilder ?? style.ForegroundBuilder
        };
    }

    internal Color? ResolveForegroundColor(MaterialState states)
    {
        return ForegroundColor?.Resolve(states);
    }

    internal Color? ResolveBackgroundColor(MaterialState states)
    {
        return BackgroundColor?.Resolve(states);
    }

    internal Color? ResolveOverlayColor(MaterialState states)
    {
        return OverlayColor?.Resolve(states);
    }

    internal Color? ResolveShadowColor(MaterialState states)
    {
        return ShadowColor?.Resolve(states);
    }

    internal Color? ResolveSurfaceTintColor(MaterialState states)
    {
        return SurfaceTintColor?.Resolve(states);
    }

    internal Color? ResolveSplashColor(MaterialState states)
    {
        return SplashColor?.Resolve(states);
    }

    internal double? ResolveElevation(MaterialState states)
    {
        return Elevation?.Resolve(states);
    }

    internal Color? ResolveIconColor(MaterialState states)
    {
        return IconColor?.Resolve(states);
    }

    internal double? ResolveIconSize(MaterialState states)
    {
        return IconSize?.Resolve(states);
    }

    internal BorderSide? ResolveSide(MaterialState states)
    {
        return Side?.Resolve(states);
    }

    internal Thickness? ResolvePadding(MaterialState states)
    {
        return Padding?.Resolve(states);
    }

    internal OutlinedBorder? ResolveShape(MaterialState states)
    {
        return Shape?.Resolve(states);
    }

    internal Size? ResolveMinimumSize(MaterialState states)
    {
        return MinimumSize?.Resolve(states);
    }

    internal Size? ResolveFixedSize(MaterialState states)
    {
        return FixedSize?.Resolve(states);
    }

    internal Size? ResolveMaximumSize(MaterialState states)
    {
        return MaximumSize?.Resolve(states);
    }

    internal MaterialTapTargetSize? ResolveTapTargetSize()
    {
        return TapTargetSize;
    }

    internal IconAlignment? ResolveIconAlignment()
    {
        return IconAlignment;
    }

    internal TextStyle? ResolveTextStyle(MaterialState states)
    {
        return TextStyle?.Resolve(states);
    }

    internal MouseCursor? ResolveMouseCursor(MaterialState states)
    {
        return MouseCursor?.Resolve(states);
    }

    private static MaterialStateProperty<Color?>? LerpColorProperty(
        MaterialStateProperty<Color?>? a,
        MaterialStateProperty<Color?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<Color?>.ResolveWith(
            states => MaterialThemeLerp.Color(
                a?.Resolve(states),
                b?.Resolve(states),
                t));
    }

    private static MaterialStateProperty<double?>? LerpDoubleProperty(
        MaterialStateProperty<double?>? a,
        MaterialStateProperty<double?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<double?>.ResolveWith(
            states => MaterialThemeLerp.Double(
                a?.Resolve(states),
                b?.Resolve(states),
                t));
    }

    private static MaterialStateProperty<BorderSide?>? LerpBorderSideProperty(
        MaterialStateProperty<BorderSide?>? a,
        MaterialStateProperty<BorderSide?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<BorderSide?>.ResolveWith(states =>
        {
            BorderSide? from = a?.Resolve(states);
            BorderSide? to = b?.Resolve(states);
            if (!from.HasValue && !to.HasValue)
            {
                return null;
            }

            BorderSide start = from ?? TransparentSide(to!.Value);
            BorderSide end = to ?? TransparentSide(from!.Value);
            return new BorderSide(
                MaterialThemeLerp.Color(start.Color, end.Color, t)!.Value,
                start.Width + ((end.Width - start.Width) * t),
                t < 0.5 ? start.Style : end.Style);
        });
    }

    private static MaterialStateProperty<Thickness?>? LerpThicknessProperty(
        MaterialStateProperty<Thickness?>? a,
        MaterialStateProperty<Thickness?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<Thickness?>.ResolveWith(
            states => MaterialThemeLerp.Thickness(
                a?.Resolve(states),
                b?.Resolve(states),
                t));
    }

    private static MaterialStateProperty<OutlinedBorder?>? LerpBorderRadiusProperty(
        MaterialStateProperty<OutlinedBorder?>? a,
        MaterialStateProperty<OutlinedBorder?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<OutlinedBorder?>.ResolveWith(states =>
            (OutlinedBorder?)ShapeBorder.Lerp(a?.Resolve(states), b?.Resolve(states), t));
    }

    private static MaterialStateProperty<Size?>? LerpSizeProperty(
        MaterialStateProperty<Size?>? a,
        MaterialStateProperty<Size?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<Size?>.ResolveWith(
            states => MaterialThemeLerp.Size(
                a?.Resolve(states),
                b?.Resolve(states),
                t));
    }

    private static MaterialStateProperty<TextStyle?>? LerpTextStyleProperty(
        MaterialStateProperty<TextStyle?>? a,
        MaterialStateProperty<TextStyle?>? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return MaterialStateProperty<TextStyle?>.ResolveWith(
            states => MaterialThemeLerp.TextStyle(
                a?.Resolve(states),
                b?.Resolve(states),
                t));
    }

    private static BorderSide TransparentSide(BorderSide source)
    {
        return new BorderSide(
            Avalonia.Media.Color.FromArgb(
                0,
                source.Color.R,
                source.Color.G,
                source.Color.B),
            0.0,
            source.Style);
    }
}
