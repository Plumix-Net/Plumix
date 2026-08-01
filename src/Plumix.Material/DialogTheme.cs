using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/dialog_theme.dart

public sealed partial record DialogThemeData
{
    public DialogThemeData(
        Color? BackgroundColor = null,
        double? Elevation = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        ShapeBorder? Shape = null,
        Alignment? Alignment = null,
        Color? IconColor = null,
        TextStyle? TitleTextStyle = null,
        TextStyle? ContentTextStyle = null,
        Thickness? ActionsPadding = null,
        Color? BarrierColor = null,
        Thickness? InsetPadding = null,
        Clip? ClipBehavior = null,
        BoxConstraints? Constraints = null)
    {
        if (Elevation.HasValue && (!double.IsFinite(Elevation.Value) || Elevation.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(Elevation));
        ValidateInsets(ActionsPadding, nameof(ActionsPadding));
        ValidateInsets(InsetPadding, nameof(InsetPadding));
        this.BackgroundColor = BackgroundColor;
        this.Elevation = Elevation;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.Shape = Shape;
        this.Alignment = Alignment;
        this.IconColor = IconColor;
        this.TitleTextStyle = TitleTextStyle;
        this.ContentTextStyle = ContentTextStyle;
        this.ActionsPadding = ActionsPadding;
        this.BarrierColor = BarrierColor;
        this.InsetPadding = InsetPadding;
        this.ClipBehavior = ClipBehavior;
        this.Constraints = Constraints;
    }

    public Color? BackgroundColor { get; init; }
    public double? Elevation { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public ShapeBorder? Shape { get; init; }
    public Alignment? Alignment { get; init; }
    public Color? IconColor { get; init; }
    public TextStyle? TitleTextStyle { get; init; }
    public TextStyle? ContentTextStyle { get; init; }
    public Thickness? ActionsPadding { get; init; }
    public Color? BarrierColor { get; init; }
    public Thickness? InsetPadding { get; init; }
    public Clip? ClipBehavior { get; init; }
    public BoxConstraints? Constraints { get; init; }

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

public sealed class DialogTheme : InheritedWidget
{
    public DialogTheme(DialogThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DialogThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((DialogTheme)oldWidget).Data, Data);

    public static DialogThemeData Of(BuildContext context) =>
        context.DependOnInherited<DialogTheme>()?.Data ?? Theme.Of(context).DialogTheme;
}
