using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/popup_menu_theme.dart

public enum PopupMenuPosition
{
    Over,
    Under,
}

public sealed record PopupMenuThemeData
{
    public PopupMenuThemeData(
        Color? Color = null,
        ShapeBorder? Shape = null,
        Thickness? MenuPadding = null,
        double? Elevation = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        TextStyle? TextStyle = null,
        MaterialStateProperty<TextStyle?>? LabelTextStyle = null,
        bool? EnableFeedback = null,
        MaterialStateProperty<MouseCursor?>? MouseCursor = null,
        PopupMenuPosition? Position = null,
        Color? IconColor = null,
        double? IconSize = null)
    {
        if (Elevation.HasValue && (!double.IsFinite(Elevation.Value) || Elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(Elevation));
        if (IconSize.HasValue && (!double.IsFinite(IconSize.Value) || IconSize.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(IconSize));
        ValidateInsets(MenuPadding, nameof(MenuPadding));
        this.Color = Color;
        this.Shape = Shape;
        this.MenuPadding = MenuPadding;
        this.Elevation = Elevation;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.TextStyle = TextStyle;
        this.LabelTextStyle = LabelTextStyle;
        this.EnableFeedback = EnableFeedback;
        this.MouseCursor = MouseCursor;
        this.Position = Position;
        this.IconColor = IconColor;
        this.IconSize = IconSize;
    }

    public Color? Color { get; init; }
    public ShapeBorder? Shape { get; init; }
    public Thickness? MenuPadding { get; init; }
    public double? Elevation { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public TextStyle? TextStyle { get; init; }
    public MaterialStateProperty<TextStyle?>? LabelTextStyle { get; init; }
    public bool? EnableFeedback { get; init; }
    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; init; }
    public PopupMenuPosition? Position { get; init; }
    public Color? IconColor { get; init; }
    public double? IconSize { get; init; }

    private static void ValidateInsets(Thickness? value, string parameterName)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public sealed class PopupMenuTheme : InheritedWidget
{
    public PopupMenuTheme(PopupMenuThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public PopupMenuThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((PopupMenuTheme)oldWidget).Data, Data);

    public static PopupMenuThemeData Of(BuildContext context) =>
        context.DependOnInherited<PopupMenuTheme>()?.Data ?? Theme.Of(context).PopupMenuTheme;
}
