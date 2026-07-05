using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/button_theme.dart (dropdown subset)

public sealed record ButtonThemeData(bool AlignedDropdown = false);

public sealed class ButtonTheme : InheritedWidget
{
    public ButtonTheme(ButtonThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ButtonThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ButtonTheme)oldWidget).Data, Data);

    public static ButtonThemeData Of(BuildContext context) =>
        context.DependOnInherited<ButtonTheme>()?.Data ?? Theme.Of(context).ButtonTheme;
}
