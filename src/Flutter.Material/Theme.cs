using Flutter.Foundation;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/theme.dart (approximate)

public sealed class Theme : InheritedWidget
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

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((Theme)oldWidget).Data, Data);
    }

    public static ThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<Theme>()?.Data ?? ThemeData.Light;
    }
}
