using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/banner_theme.dart

public sealed partial record MaterialBannerThemeData
{
    public MaterialBannerThemeData(
        Color? BackgroundColor = null,
        Color? SurfaceTintColor = null,
        Color? ShadowColor = null,
        Color? DividerColor = null,
        TextStyle? ContentTextStyle = null,
        double? Elevation = null,
        Thickness? Padding = null,
        Thickness? LeadingPadding = null)
    {
        if (Elevation.HasValue && (!double.IsFinite(Elevation.Value) || Elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(Elevation), "Elevation must be non-negative and finite.");
        }
        this.BackgroundColor = BackgroundColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.ShadowColor = ShadowColor;
        this.DividerColor = DividerColor;
        this.ContentTextStyle = ContentTextStyle;
        this.Elevation = Elevation;
        this.Padding = Padding;
        this.LeadingPadding = LeadingPadding;
    }

    public Color? BackgroundColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? DividerColor { get; init; }
    public TextStyle? ContentTextStyle { get; init; }
    public double? Elevation { get; init; }
    public Thickness? Padding { get; init; }
    public Thickness? LeadingPadding { get; init; }

    public MaterialBannerThemeData CopyWith(
        Color? backgroundColor = null,
        Color? surfaceTintColor = null,
        Color? shadowColor = null,
        Color? dividerColor = null,
        TextStyle? contentTextStyle = null,
        double? elevation = null,
        Thickness? padding = null,
        Thickness? leadingPadding = null)
    {
        return new MaterialBannerThemeData(
            backgroundColor ?? BackgroundColor,
            surfaceTintColor ?? SurfaceTintColor,
            shadowColor ?? ShadowColor,
            dividerColor ?? DividerColor,
            contentTextStyle ?? ContentTextStyle,
            elevation ?? Elevation,
            padding ?? Padding,
            leadingPadding ?? LeadingPadding);
    }
}

public sealed class MaterialBannerTheme : InheritedWidget
{
    public MaterialBannerTheme(MaterialBannerThemeData? data, Widget child, Key? key = null) : base(key)
    {
        Data = data;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public MaterialBannerThemeData? Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((MaterialBannerTheme)oldWidget).Data, Data);

    public static MaterialBannerThemeData Of(BuildContext context) =>
        context.DependOnInherited<MaterialBannerTheme>()?.Data ?? Theme.Of(context).BannerTheme;
}
