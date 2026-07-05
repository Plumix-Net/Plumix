using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/chip_theme.dart

public sealed record ChipThemeData(
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
