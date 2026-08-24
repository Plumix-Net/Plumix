using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/checkbox_theme.dart

// `WidgetStateMouseCursor` and `WidgetStateBorderSide` moved to the core widgets library
// (`src/Plumix/Widgets/WidgetState.cs`), where Flutter declares them; these extensions keep
// Material's `[Flags] MaterialState` call sites working against the core set-based resolvers.
public static class WidgetStateMaterialExtensions
{
    public static BorderSide? Resolve(this WidgetStateBorderSide side, MaterialState states)
    {
        ArgumentNullException.ThrowIfNull(side);
        return side.Resolve(MaterialStateSet.Of(states));
    }

    public static MouseCursor? Resolve(this WidgetStateMouseCursor cursor, MaterialState states)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        return cursor.Resolve(MaterialStateSet.Of(states));
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
