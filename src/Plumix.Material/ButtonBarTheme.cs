using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/button_bar_theme.dart

public sealed record ButtonBarThemeData(
    MainAxisAlignment? Alignment = null,
    MainAxisSize? MainAxisSize = null,
    ButtonTextTheme? ButtonTextTheme = null,
    double? ButtonMinWidth = null,
    double? ButtonHeight = null,
    Thickness? ButtonPadding = null,
    bool? ButtonAlignedDropdown = null,
    ButtonBarLayoutBehavior? LayoutBehavior = null,
    VerticalDirection? OverflowDirection = null)
{
    public static ButtonBarThemeData? Lerp(ButtonBarThemeData? a, ButtonBarThemeData? b, double t)
    {
        if (ReferenceEquals(a, b)) return a;
        t = Math.Clamp(t, 0.0, 1.0);
        return new ButtonBarThemeData(
            Alignment: t < 0.5 ? a?.Alignment : b?.Alignment,
            MainAxisSize: t < 0.5 ? a?.MainAxisSize : b?.MainAxisSize,
            ButtonTextTheme: t < 0.5 ? a?.ButtonTextTheme : b?.ButtonTextTheme,
            ButtonMinWidth: LerpNullable(a?.ButtonMinWidth, b?.ButtonMinWidth, t),
            ButtonHeight: LerpNullable(a?.ButtonHeight, b?.ButtonHeight, t),
            ButtonPadding: LerpThickness(a?.ButtonPadding, b?.ButtonPadding, t),
            ButtonAlignedDropdown: t < 0.5 ? a?.ButtonAlignedDropdown : b?.ButtonAlignedDropdown,
            LayoutBehavior: t < 0.5 ? a?.LayoutBehavior : b?.LayoutBehavior,
            OverflowDirection: t < 0.5 ? a?.OverflowDirection : b?.OverflowDirection);
    }

    private static double? LerpNullable(double? a, double? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        return (a ?? 0) + (((b ?? 0) - (a ?? 0)) * t);
    }

    private static Thickness? LerpThickness(Thickness? a, Thickness? b, double t)
    {
        if (!a.HasValue && !b.HasValue) return null;
        var from = a ?? default;
        var to = b ?? default;
        return new Thickness(
            from.Left + ((to.Left - from.Left) * t),
            from.Top + ((to.Top - from.Top) * t),
            from.Right + ((to.Right - from.Right) * t),
            from.Bottom + ((to.Bottom - from.Bottom) * t));
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

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((ButtonBarTheme)oldWidget).Data, Data);

    public static ButtonBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<ButtonBarTheme>()?.Data ?? Theme.Of(context).ButtonBarTheme;
}
