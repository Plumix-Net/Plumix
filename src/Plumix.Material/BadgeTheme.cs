using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/badge_theme.dart

public sealed record BadgeThemeData(
    Color? BackgroundColor = null,
    Color? TextColor = null,
    double? SmallSize = null,
    double? LargeSize = null,
    TextStyle? TextStyle = null,
    Thickness? Padding = null,
    Alignment? Alignment = null,
    Vector? Offset = null);

public sealed class BadgeTheme : InheritedWidget
{
    public BadgeTheme(BadgeThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BadgeThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((BadgeTheme)oldWidget).Data, Data);
    }

    public static BadgeThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<BadgeTheme>()?.Data ?? Theme.Of(context).BadgeTheme;
    }
}
