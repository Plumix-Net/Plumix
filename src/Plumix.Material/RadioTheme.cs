using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/radio_theme.dart

public sealed partial record RadioThemeData(
    IWidgetStateProperty<MouseCursor?>? MouseCursor = null,
    IWidgetStateProperty<Color?>? FillColor = null,
    IWidgetStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    VisualDensity? VisualDensity = null,
    IWidgetStateProperty<Color?>? BackgroundColor = null,
    WidgetStateBorderSide? Side = null,
    WidgetStateProperty<double?>? InnerRadius = null)
{
    public RadioThemeData CopyWith(
        IWidgetStateProperty<MouseCursor?>? mouseCursor = null,
        IWidgetStateProperty<Color?>? fillColor = null,
        IWidgetStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        IWidgetStateProperty<Color?>? backgroundColor = null,
        WidgetStateBorderSide? side = null,
        WidgetStateProperty<double?>? innerRadius = null)
    {
        return new RadioThemeData(
            MouseCursor: mouseCursor ?? MouseCursor,
            FillColor: fillColor ?? FillColor,
            OverlayColor: overlayColor ?? OverlayColor,
            SplashRadius: splashRadius ?? SplashRadius,
            MaterialTapTargetSize: materialTapTargetSize ?? MaterialTapTargetSize,
            VisualDensity: visualDensity ?? VisualDensity,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Side: side ?? Side,
            InnerRadius: innerRadius ?? InnerRadius);
    }
}

public sealed class RadioTheme : InheritedWidget
{
    public RadioTheme(
        RadioThemeData data,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public RadioThemeData Data { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((RadioTheme)oldWidget).Data, Data);
    }

    public static RadioThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInheritedWidgetOfExactType<RadioTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).RadioTheme;
    }
}
