using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/drawer_theme.dart

public sealed partial record DrawerThemeData(
    Color? BackgroundColor = null,
    Color? ScrimColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    ShapeBorder? Shape = null,
    ShapeBorder? EndShape = null,
    double? Width = null,
    Clip? ClipBehavior = null)
{
    public DrawerThemeData CopyWith(
        Color? backgroundColor = null,
        Color? scrimColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        ShapeBorder? endShape = null,
        double? width = null,
        Clip? clipBehavior = null)
    {
        return new DrawerThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            ScrimColor: scrimColor ?? ScrimColor,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Shape: shape ?? Shape,
            EndShape: endShape ?? EndShape,
            Width: width ?? Width,
            ClipBehavior: clipBehavior ?? ClipBehavior);
    }
}

public sealed class DrawerTheme : InheritedTheme
{
    public DrawerTheme(
        DrawerThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DrawerThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DrawerTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((DrawerTheme)oldWidget).Data, Data);
    }

    public static DrawerThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<DrawerTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).DrawerTheme;
    }
}
