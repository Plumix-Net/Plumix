using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/checkbox_theme.dart

public abstract record WidgetStateMouseCursor : MouseCursor
{
    public abstract MouseCursor? Resolve(MaterialState states);

    public static WidgetStateMouseCursor ResolveWith(Func<MaterialState, MouseCursor?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new ResolverWidgetStateMouseCursor(resolver);
    }

    private sealed record ResolverWidgetStateMouseCursor(
        Func<MaterialState, MouseCursor?> Resolver) : WidgetStateMouseCursor
    {
        public override MouseCursor? Resolve(MaterialState states) => Resolver(states);
    }
}

public abstract class WidgetStateBorderSide
{
    public abstract BorderSide? Resolve(MaterialState states);

    internal abstract bool IsStateful { get; }

    public static WidgetStateBorderSide All(BorderSide? side)
    {
        return new StatefulBorderSide(_ => side);
    }

    public static WidgetStateBorderSide ResolveWith(Func<MaterialState, BorderSide?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return new StatefulBorderSide(resolver);
    }

    public static implicit operator WidgetStateBorderSide(BorderSide side)
    {
        return new FixedBorderSide(side);
    }

    internal static WidgetStateBorderSide? Lerp(
        WidgetStateBorderSide? a,
        WidgetStateBorderSide? b,
        double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        BorderSide? start = a?.Resolve(MaterialState.None);
        BorderSide? end = b?.Resolve(MaterialState.None);
        BorderSide? side = MaterialThemeLerp.BorderSide(start, end, t);
        return side.HasValue ? new FixedBorderSide(side.Value) : null;
    }

    private sealed class FixedBorderSide(BorderSide side) : WidgetStateBorderSide
    {
        public override BorderSide? Resolve(MaterialState states)
        {
            return states.HasFlag(MaterialState.Selected) ? null : side;
        }

        internal override bool IsStateful => false;
    }

    private sealed class StatefulBorderSide(Func<MaterialState, BorderSide?> resolver) : WidgetStateBorderSide
    {
        public override BorderSide? Resolve(MaterialState states) => resolver(states);

        internal override bool IsStateful => true;
    }
}

public sealed partial record CheckboxThemeData(
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    MaterialStateProperty<Color?>? FillColor = null,
    MaterialStateProperty<Color?>? CheckColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    VisualDensity? VisualDensity = null,
    ShapeBorder? Shape = null,
    WidgetStateBorderSide? Side = null)
{
    public CheckboxThemeData CopyWith(
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        MaterialStateProperty<Color?>? checkColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        ShapeBorder? shape = null,
        WidgetStateBorderSide? side = null)
    {
        return new CheckboxThemeData(
            MouseCursor: mouseCursor ?? MouseCursor,
            FillColor: fillColor ?? FillColor,
            CheckColor: checkColor ?? CheckColor,
            OverlayColor: overlayColor ?? OverlayColor,
            SplashRadius: splashRadius ?? SplashRadius,
            MaterialTapTargetSize: materialTapTargetSize ?? MaterialTapTargetSize,
            VisualDensity: visualDensity ?? VisualDensity,
            Shape: shape ?? Shape,
            Side: side ?? Side);
    }
}

public sealed class CheckboxTheme : InheritedWidget
{
    public CheckboxTheme(
        CheckboxThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CheckboxThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((CheckboxTheme)oldWidget).Data, Data);
    }

    public static CheckboxThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<CheckboxTheme>();
        return localTheme?.Data ?? Theme.Of(context).CheckboxTheme;
    }
}
