using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/segmented_button_theme.dart

public sealed partial record SegmentedButtonThemeData(
    ButtonStyle? Style = null,
    Widget? SelectedIcon = null)
{
    public SegmentedButtonThemeData CopyWith(
        ButtonStyle? style = null,
        Widget? selectedIcon = null)
    {
        return new SegmentedButtonThemeData(
            Style: style ?? Style,
            SelectedIcon: selectedIcon ?? SelectedIcon);
    }
}

public sealed class SegmentedButtonTheme : InheritedTheme
{
    public SegmentedButtonTheme(SegmentedButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SegmentedButtonThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new SegmentedButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SegmentedButtonTheme)oldWidget).Data, Data);
    }

    public static SegmentedButtonThemeData Of(BuildContext context)
    {
        return MaybeOf(context) ?? Theme.Of(context).SegmentedButtonTheme;
    }

    public static SegmentedButtonThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<SegmentedButtonTheme>()?.Data;
}
