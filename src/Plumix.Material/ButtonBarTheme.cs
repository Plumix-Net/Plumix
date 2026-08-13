using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/button_bar_theme.dart

public sealed record ButtonBarThemeData
{
    public ButtonBarThemeData(
        MainAxisAlignment? Alignment = null,
        MainAxisSize? MainAxisSize = null,
        ButtonTextTheme? ButtonTextTheme = null,
        double? ButtonMinWidth = null,
        double? ButtonHeight = null,
        EdgeInsetsGeometry? ButtonPadding = null,
        bool? ButtonAlignedDropdown = null,
        ButtonBarLayoutBehavior? LayoutBehavior = null,
        VerticalDirection? OverflowDirection = null)
    {
        ValidateNonNegative(nameof(ButtonMinWidth), ButtonMinWidth);
        ValidateNonNegative(nameof(ButtonHeight), ButtonHeight);
        this.Alignment = Alignment;
        this.MainAxisSize = MainAxisSize;
        this.ButtonTextTheme = ButtonTextTheme;
        this.ButtonMinWidth = ButtonMinWidth;
        this.ButtonHeight = ButtonHeight;
        this.ButtonPadding = ButtonPadding;
        this.ButtonAlignedDropdown = ButtonAlignedDropdown;
        this.LayoutBehavior = LayoutBehavior;
        this.OverflowDirection = OverflowDirection;
    }

    public MainAxisAlignment? Alignment { get; init; }

    public MainAxisSize? MainAxisSize { get; init; }

    public ButtonTextTheme? ButtonTextTheme { get; init; }

    public double? ButtonMinWidth { get; init; }

    public double? ButtonHeight { get; init; }

    public EdgeInsetsGeometry? ButtonPadding { get; init; }

    public bool? ButtonAlignedDropdown { get; init; }

    public ButtonBarLayoutBehavior? LayoutBehavior { get; init; }

    public VerticalDirection? OverflowDirection { get; init; }

    public ButtonBarThemeData CopyWith(
        MainAxisAlignment? alignment = null,
        MainAxisSize? mainAxisSize = null,
        ButtonTextTheme? buttonTextTheme = null,
        double? buttonMinWidth = null,
        double? buttonHeight = null,
        EdgeInsetsGeometry? buttonPadding = null,
        bool? buttonAlignedDropdown = null,
        ButtonBarLayoutBehavior? layoutBehavior = null,
        VerticalDirection? overflowDirection = null)
    {
        return new ButtonBarThemeData(
            Alignment: alignment ?? Alignment,
            MainAxisSize: mainAxisSize ?? MainAxisSize,
            ButtonTextTheme: buttonTextTheme ?? ButtonTextTheme,
            ButtonMinWidth: buttonMinWidth ?? ButtonMinWidth,
            ButtonHeight: buttonHeight ?? ButtonHeight,
            ButtonPadding: buttonPadding ?? ButtonPadding,
            ButtonAlignedDropdown: buttonAlignedDropdown ?? ButtonAlignedDropdown,
            LayoutBehavior: layoutBehavior ?? LayoutBehavior,
            OverflowDirection: overflowDirection ?? OverflowDirection);
    }

    public static ButtonBarThemeData? Lerp(ButtonBarThemeData? a, ButtonBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ButtonBarThemeData(
            Alignment: t < 0.5 ? a?.Alignment : b?.Alignment,
            MainAxisSize: t < 0.5 ? a?.MainAxisSize : b?.MainAxisSize,
            ButtonTextTheme: t < 0.5 ? a?.ButtonTextTheme : b?.ButtonTextTheme,
            ButtonMinWidth: LerpDouble(a?.ButtonMinWidth, b?.ButtonMinWidth, t),
            ButtonHeight: LerpDouble(a?.ButtonHeight, b?.ButtonHeight, t),
            ButtonPadding: EdgeInsetsGeometry.Lerp(a?.ButtonPadding, b?.ButtonPadding, t),
            ButtonAlignedDropdown: t < 0.5 ? a?.ButtonAlignedDropdown : b?.ButtonAlignedDropdown,
            LayoutBehavior: t < 0.5 ? a?.LayoutBehavior : b?.LayoutBehavior,
            OverflowDirection: t < 0.5 ? a?.OverflowDirection : b?.OverflowDirection);
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<MainAxisAlignment?>("alignment", Alignment));
        properties.Add(new DiagnosticsProperty<MainAxisSize?>("mainAxisSize", MainAxisSize));
        properties.Add(new DiagnosticsProperty<ButtonTextTheme?>("textTheme", ButtonTextTheme));
        properties.Add(new DoubleProperty("minWidth", ButtonMinWidth));
        properties.Add(new DoubleProperty("height", ButtonHeight));
        properties.Add(new DiagnosticsProperty<EdgeInsetsGeometry?>("padding", ButtonPadding));
        properties.Add(new FlagProperty(
            "buttonAlignedDropdown",
            ButtonAlignedDropdown,
            "dropdown width matches button"));
        properties.Add(new DiagnosticsProperty<ButtonBarLayoutBehavior?>("layoutBehavior", LayoutBehavior));
        properties.Add(new DiagnosticsProperty<VerticalDirection?>("overflowDirection", OverflowDirection));
    }

    private static double? LerpDouble(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }

        return (a ?? 0) + (((b ?? 0) - (a ?? 0)) * t);
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class ButtonBarTheme : InheritedWidget
{
    public ButtonBarTheme(ButtonBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ButtonBarThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ButtonBarTheme)oldWidget).Data, Data);
    }

    public static ButtonBarThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ButtonBarTheme>()?.Data ?? Theme.Of(context).ButtonBarTheme;
    }
}
