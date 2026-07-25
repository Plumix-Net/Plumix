using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/theme.dart (approximate)

public sealed class Theme : InheritedTheme
{
    public Theme(
        ThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child;
    }

    public ThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new DefaultTextStyle(
            style: Data.TextTheme.BodyMedium,
            child: Child);
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new Theme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((Theme)oldWidget).Data, Data);
    }

    public static ThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<Theme>()?.Data ?? ThemeData.Light;
    }
}
