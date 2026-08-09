using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/chip_theme.dart

public sealed partial record ChipThemeData(
    MaterialStateProperty<Color?>? Color = null,
    Color? BackgroundColor = null,
    Color? DeleteIconColor = null,
    Color? DisabledColor = null,
    Color? SelectedColor = null,
    Color? SecondarySelectedColor = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    Color? SelectedShadowColor = null,
    bool? ShowCheckmark = null,
    Color? CheckmarkColor = null,
    Thickness? LabelPadding = null,
    Thickness? Padding = null,
    BorderSide? Side = null,
    ShapeBorder? Shape = null,
    TextStyle? LabelStyle = null,
    TextStyle? SecondaryLabelStyle = null,
    Brightness? Brightness = null,
    double? Elevation = null,
    double? PressElevation = null,
    IconThemeData? IconTheme = null,
    BoxConstraints? AvatarBoxConstraints = null,
    BoxConstraints? DeleteIconBoxConstraints = null)
{
    public ChipThemeData() : this(Color: null)
    {
    }

    public static ChipThemeData FromDefaults(
        Color secondaryColor,
        TextStyle labelStyle,
        Brightness? brightness = null,
        Color? primaryColor = null)
    {
        if (brightness.HasValue == primaryColor.HasValue)
        {
            throw new ArgumentException("Exactly one of brightness and primaryColor must be provided.");
        }

        Brightness effectiveBrightness = brightness
                                         ?? ThemeData.EstimateBrightnessForColor(primaryColor!.Value);
        Color baseColor = effectiveBrightness == global::Plumix.Material.Brightness.Light
            ? Colors.Black
            : Colors.White;
        return new ChipThemeData(
            BackgroundColor: WithAlpha(baseColor, 0x1f),
            DeleteIconColor: WithAlpha(baseColor, 0xde),
            DisabledColor: WithAlpha(baseColor, 0x0c),
            SelectedColor: WithAlpha(baseColor, 0x3d),
            SecondarySelectedColor: WithAlpha(secondaryColor, 0x3d),
            ShadowColor: Colors.Black,
            SelectedShadowColor: Colors.Black,
            ShowCheckmark: true,
            Padding: new Thickness(4),
            LabelStyle: labelStyle.CopyWith(color: WithAlpha(baseColor, 0xde)),
            SecondaryLabelStyle: labelStyle.CopyWith(color: secondaryColor),
            Brightness: effectiveBrightness,
            Elevation: 0.0,
            PressElevation: 8.0,
            IconTheme: new IconThemeData(Size: 18.0));
    }

    private static Color WithAlpha(Color color, byte alpha)
    {
        return Avalonia.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

public sealed class ChipTheme : InheritedWidget
{
    public ChipTheme(ChipThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ChipThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ChipTheme)oldWidget).Data, Data);
    }

    public static ChipThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ChipTheme>()?.Data ?? Theme.Of(context).ChipTheme;
    }
}
