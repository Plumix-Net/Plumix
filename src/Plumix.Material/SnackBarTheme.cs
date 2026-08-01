using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/snack_bar_theme.dart

public enum SnackBarBehavior
{
    Fixed,
    Floating,
}

public enum DismissDirection
{
    Vertical,
    Horizontal,
    EndToStart,
    StartToEnd,
    Up,
    Down,
    None,
}

public sealed partial record SnackBarThemeData
{
    public SnackBarThemeData(
        Color? BackgroundColor = null,
        Color? ActionTextColor = null,
        Color? DisabledActionTextColor = null,
        TextStyle? ContentTextStyle = null,
        double? Elevation = null,
        ShapeBorder? Shape = null,
        SnackBarBehavior? Behavior = null,
        double? Width = null,
        Thickness? InsetPadding = null,
        bool? ShowCloseIcon = null,
        Color? CloseIconColor = null,
        double? ActionOverflowThreshold = null,
        Color? ActionBackgroundColor = null,
        Color? DisabledActionBackgroundColor = null,
        DismissDirection? DismissDirection = null)
    {
        ValidateNonNegativeFinite(Elevation, nameof(Elevation));
        ValidatePositiveFinite(Width, nameof(Width));
        ValidateUnitInterval(ActionOverflowThreshold, nameof(ActionOverflowThreshold));
        ValidateInsets(InsetPadding, nameof(InsetPadding));
        if (Width.HasValue && Behavior != SnackBarBehavior.Floating)
        {
            throw new ArgumentException("SnackBar theme width requires floating behavior.", nameof(Width));
        }

        this.BackgroundColor = BackgroundColor;
        this.ActionTextColor = ActionTextColor;
        this.DisabledActionTextColor = DisabledActionTextColor;
        this.ContentTextStyle = ContentTextStyle;
        this.Elevation = Elevation;
        this.Shape = Shape;
        this.Behavior = Behavior;
        this.Width = Width;
        this.InsetPadding = InsetPadding;
        this.ShowCloseIcon = ShowCloseIcon;
        this.CloseIconColor = CloseIconColor;
        this.ActionOverflowThreshold = ActionOverflowThreshold;
        this.ActionBackgroundColor = ActionBackgroundColor;
        this.DisabledActionBackgroundColor = DisabledActionBackgroundColor;
        this.DismissDirection = DismissDirection;
    }

    public Color? BackgroundColor { get; init; }
    public Color? ActionTextColor { get; init; }
    public Color? DisabledActionTextColor { get; init; }
    public TextStyle? ContentTextStyle { get; init; }
    public double? Elevation { get; init; }
    public ShapeBorder? Shape { get; init; }
    public SnackBarBehavior? Behavior { get; init; }
    public double? Width { get; init; }
    public Thickness? InsetPadding { get; init; }
    public bool? ShowCloseIcon { get; init; }
    public Color? CloseIconColor { get; init; }
    public double? ActionOverflowThreshold { get; init; }
    public Color? ActionBackgroundColor { get; init; }
    public Color? DisabledActionBackgroundColor { get; init; }
    public DismissDirection? DismissDirection { get; init; }

    private static void ValidateNonNegativeFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative and finite.");
        }
    }

    private static void ValidatePositiveFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive and finite.");
        }
    }

    private static void ValidateUnitInterval(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0 || value.Value > 1))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be between zero and one.");
        }
    }

    private static void ValidateInsets(Thickness? value, string name)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Insets must be non-negative and finite.");
        }
    }
}

public sealed class SnackBarTheme : InheritedWidget
{
    public SnackBarTheme(SnackBarThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SnackBarThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((SnackBarTheme)oldWidget).Data, Data);

    public static SnackBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<SnackBarTheme>()?.Data ?? Theme.Of(context).SnackBarTheme;
}
