using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/segmented_button_theme.dart

public sealed record SegmentedButtonThemeData(
    ButtonStyle? Style = null,
    Widget? SelectedIcon = null);

public sealed class SegmentedButtonTheme : InheritedWidget
{
    public SegmentedButtonTheme(SegmentedButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SegmentedButtonThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SegmentedButtonTheme)oldWidget).Data, Data);
    }

    public static SegmentedButtonThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<SegmentedButtonTheme>()?.Data
               ?? Theme.Of(context).SegmentedButtonTheme;
    }
}
