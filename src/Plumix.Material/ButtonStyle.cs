using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/button_style.dart (approximate)

[Flags]
public enum MaterialState
{
    None = 0,
    Hovered = 1 << 0,
    Focused = 1 << 1,
    Pressed = 1 << 2,
    Disabled = 1 << 3,
    Selected = 1 << 4,
    Error = 1 << 5
}

public enum IconAlignment
{
    Start,
    End
}

public abstract class MaterialStateProperty<T>
{
    public abstract T Resolve(MaterialState states);

    public static MaterialStateProperty<T> All(T value)
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
    MaterialStateProperty<BorderRadius?>? Shape = null,
    MaterialStateProperty<Size?>? MinimumSize = null,
    MaterialStateProperty<Size?>? FixedSize = null,
    MaterialStateProperty<Size?>? MaximumSize = null,
    Alignment? Alignment = null,
    IconAlignment? IconAlignment = null,
    MaterialTapTargetSize? TapTargetSize = null,
    MaterialStateProperty<TextStyle?>? TextStyle = null)
{
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
            TextStyle = TextStyle ?? style.TextStyle
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

    internal BorderRadius? ResolveShape(MaterialState states)
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
}
