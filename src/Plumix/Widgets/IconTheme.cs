using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/icon_theme_data.dart; flutter/packages/flutter/lib/src/widgets/icon_theme.dart (approximate)

public sealed record IconThemeData(
    Color? Color = null,
    double? Size = null)
{
    internal static IconThemeData Fallback { get; } = new();
}

public sealed class IconTheme : InheritedWidget
{
    public IconTheme(
        IconThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child;
    }

    public IconThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((IconTheme)oldWidget).Data, Data);
    }

    public static IconThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<IconTheme>()?.Data ?? IconThemeData.Fallback;
    }
}
