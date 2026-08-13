using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/radio_theme.dart

public sealed partial record RadioThemeData(
    MaterialStateProperty<MouseCursor?>? MouseCursor = null,
    MaterialStateProperty<Color?>? FillColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    VisualDensity? VisualDensity = null,
    MaterialStateProperty<Color?>? BackgroundColor = null,
    WidgetStateBorderSide? Side = null,
    MaterialStateProperty<double?>? InnerRadius = null)
{
    public RadioThemeData CopyWith(
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        MaterialStateProperty<Color?>? backgroundColor = null,
        WidgetStateBorderSide? side = null,
        MaterialStateProperty<double?>? innerRadius = null)
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
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public RadioThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((RadioTheme)oldWidget).Data, Data);
    }

    public static RadioThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<RadioTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).RadioTheme;
    }
}
