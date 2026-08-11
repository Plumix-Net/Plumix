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

    public ChipThemeData CopyWith(
        MaterialStateProperty<Color?>? color = null,
        Color? backgroundColor = null,
        Color? deleteIconColor = null,
        Color? disabledColor = null,
        Color? selectedColor = null,
        Color? secondarySelectedColor = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? selectedShadowColor = null,
        bool? showCheckmark = null,
        Color? checkmarkColor = null,
        Thickness? labelPadding = null,
        Thickness? padding = null,
        BorderSide? side = null,
        ShapeBorder? shape = null,
        TextStyle? labelStyle = null,
        TextStyle? secondaryLabelStyle = null,
        Brightness? brightness = null,
        double? elevation = null,
        double? pressElevation = null,
        IconThemeData? iconTheme = null,
        BoxConstraints? avatarBoxConstraints = null,
        BoxConstraints? deleteIconBoxConstraints = null)
    {
        return new ChipThemeData(
            Color: color ?? Color,
            BackgroundColor: backgroundColor ?? BackgroundColor,
            DeleteIconColor: deleteIconColor ?? DeleteIconColor,
            DisabledColor: disabledColor ?? DisabledColor,
            SelectedColor: selectedColor ?? SelectedColor,
            SecondarySelectedColor: secondarySelectedColor ?? SecondarySelectedColor,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            SelectedShadowColor: selectedShadowColor ?? SelectedShadowColor,
            ShowCheckmark: showCheckmark ?? ShowCheckmark,
            CheckmarkColor: checkmarkColor ?? CheckmarkColor,
            LabelPadding: labelPadding ?? LabelPadding,
            Padding: padding ?? Padding,
            Side: side ?? Side,
            Shape: shape ?? Shape,
            LabelStyle: labelStyle ?? LabelStyle,
            SecondaryLabelStyle: secondaryLabelStyle ?? SecondaryLabelStyle,
            Brightness: brightness ?? Brightness,
            Elevation: elevation ?? Elevation,
            PressElevation: pressElevation ?? PressElevation,
            IconTheme: iconTheme ?? IconTheme,
            AvatarBoxConstraints: avatarBoxConstraints ?? AvatarBoxConstraints,
            DeleteIconBoxConstraints: deleteIconBoxConstraints ?? DeleteIconBoxConstraints);
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
        Color baseColor = primaryColor
                          ?? (effectiveBrightness == global::Plumix.Material.Brightness.Light
                              ? Colors.Black
                              : Colors.White);
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
            SecondaryLabelStyle: labelStyle.CopyWith(color: WithAlpha(secondaryColor, 0xde)),
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

public sealed class ChipTheme : InheritedTheme
{
    public ChipTheme(ChipThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ChipThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ChipTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ChipTheme)oldWidget).Data, Data);
    }

    public static ChipThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ChipTheme>()?.Data ?? Theme.Of(context).ChipTheme;
    }
}
