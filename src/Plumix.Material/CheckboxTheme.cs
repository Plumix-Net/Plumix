using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/checkbox_theme.dart

public sealed partial record CheckboxThemeData(
    IWidgetStateProperty<MouseCursor?>? MouseCursor = null,
    IWidgetStateProperty<Color?>? FillColor = null,
    IWidgetStateProperty<Color?>? CheckColor = null,
    IWidgetStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    VisualDensity? VisualDensity = null,
    ShapeBorder? Shape = null,
    WidgetStateBorderSide? Side = null)
{
    public CheckboxThemeData CopyWith(
        IWidgetStateProperty<MouseCursor?>? mouseCursor = null,
        IWidgetStateProperty<Color?>? fillColor = null,
        IWidgetStateProperty<Color?>? checkColor = null,
        IWidgetStateProperty<Color?>? overlayColor = null,
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
        Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public CheckboxThemeData Data { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((CheckboxTheme)oldWidget).Data, Data);
    }

    public static CheckboxThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInheritedWidgetOfExactType<CheckboxTheme>();
        return localTheme?.Data ?? Theme.Of(context).CheckboxTheme;
    }
}
