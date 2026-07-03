using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/toggle_buttons_theme.dart

public sealed record ToggleButtonsThemeData(
    TextStyle? TextStyle = null,
    BoxConstraints? Constraints = null,
    Color? Color = null,
    Color? SelectedColor = null,
    Color? DisabledColor = null,
    Color? FillColor = null,
    Color? FocusColor = null,
    Color? HighlightColor = null,
    Color? HoverColor = null,
    Color? SplashColor = null,
    Color? BorderColor = null,
    Color? SelectedBorderColor = null,
    Color? DisabledBorderColor = null,
    BorderRadius? BorderRadius = null,
    double? BorderWidth = null);

public sealed class ToggleButtonsTheme : InheritedWidget
{
    public ToggleButtonsTheme(ToggleButtonsThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ToggleButtonsThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ToggleButtonsTheme)oldWidget).Data, Data);
    }

    public static ToggleButtonsThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ToggleButtonsTheme>()?.Data
               ?? Theme.Of(context).ToggleButtonsTheme;
    }
}
