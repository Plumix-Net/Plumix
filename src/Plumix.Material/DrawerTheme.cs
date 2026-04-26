using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/drawer_theme.dart (approximate)

public sealed record DrawerThemeData(
    Color? BackgroundColor = null,
    Color? ScrimColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    double? Width = null);

public sealed class DrawerTheme : InheritedWidget
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
