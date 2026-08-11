using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/toggle_buttons_theme.dart

public sealed partial record ToggleButtonsThemeData(
    TextStyle? TextStyle = null,
    BoxConstraints? Constraints = null,
    Color? Color = null,
    Color? SelectedColor = null,
    Color? DisabledColor = null,
    MaterialStateProperty<Color?>? FillColor = null,
    Color? FocusColor = null,
    Color? HighlightColor = null,
    Color? HoverColor = null,
    Color? SplashColor = null,
    Color? BorderColor = null,
    Color? SelectedBorderColor = null,
    Color? DisabledBorderColor = null,
    BorderRadius? BorderRadius = null,
    double? BorderWidth = null)
{
    public ToggleButtonsThemeData CopyWith(
        TextStyle? textStyle = null,
        BoxConstraints? constraints = null,
        Color? color = null,
        Color? selectedColor = null,
        Color? disabledColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? focusColor = null,
        Color? highlightColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        Color? borderColor = null,
        Color? selectedBorderColor = null,
        Color? disabledBorderColor = null,
        BorderRadius? borderRadius = null,
        double? borderWidth = null)
    {
        return new ToggleButtonsThemeData(
            TextStyle: textStyle ?? TextStyle,
            Constraints: constraints ?? Constraints,
            Color: color ?? Color,
            SelectedColor: selectedColor ?? SelectedColor,
            DisabledColor: disabledColor ?? DisabledColor,
            FillColor: fillColor ?? FillColor,
            FocusColor: focusColor ?? FocusColor,
            HighlightColor: highlightColor ?? HighlightColor,
            HoverColor: hoverColor ?? HoverColor,
            SplashColor: splashColor ?? SplashColor,
            BorderColor: borderColor ?? BorderColor,
            SelectedBorderColor: selectedBorderColor ?? SelectedBorderColor,
            DisabledBorderColor: disabledBorderColor ?? DisabledBorderColor,
            BorderRadius: borderRadius ?? BorderRadius,
            BorderWidth: borderWidth ?? BorderWidth);
    }
}

public sealed class ToggleButtonsTheme : InheritedTheme
{
    public ToggleButtonsTheme(ToggleButtonsThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ToggleButtonsThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ToggleButtonsTheme(Data, child);
    }

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
