using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/bottom_sheet_theme.dart

public sealed record BottomSheetThemeData
{
    public BottomSheetThemeData(
        Color? BackgroundColor = null,
        Color? SurfaceTintColor = null,
        double? Elevation = null,
        Color? ModalBackgroundColor = null,
        Color? ModalBarrierColor = null,
        Color? ShadowColor = null,
        double? ModalElevation = null,
        ShapeBorder? Shape = null,
        bool? ShowDragHandle = null,
        MaterialStateProperty<Color?>? DragHandleColor = null,
        Size? DragHandleSize = null,
        Clip? ClipBehavior = null,
        BoxConstraints? Constraints = null)
    {
        ValidateElevation(Elevation, nameof(Elevation));
        ValidateElevation(ModalElevation, nameof(ModalElevation));
        if (DragHandleSize.HasValue
            && (!double.IsFinite(DragHandleSize.Value.Width)
                || !double.IsFinite(DragHandleSize.Value.Height)
                || DragHandleSize.Value.Width < 0
                || DragHandleSize.Value.Height < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(DragHandleSize));
        }

        this.BackgroundColor = BackgroundColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.Elevation = Elevation;
        this.ModalBackgroundColor = ModalBackgroundColor;
        this.ModalBarrierColor = ModalBarrierColor;
        this.ShadowColor = ShadowColor;
        this.ModalElevation = ModalElevation;
        this.Shape = Shape;
        this.ShowDragHandle = ShowDragHandle;
        this.DragHandleColor = DragHandleColor;
        this.DragHandleSize = DragHandleSize;
        this.ClipBehavior = ClipBehavior;
        this.Constraints = Constraints;
    }

    public Color? BackgroundColor { get; init; }
    public Color? SurfaceTintColor { get; init; }
    public double? Elevation { get; init; }
    public Color? ModalBackgroundColor { get; init; }
    public Color? ModalBarrierColor { get; init; }
    public Color? ShadowColor { get; init; }
    public double? ModalElevation { get; init; }
    public ShapeBorder? Shape { get; init; }
    public bool? ShowDragHandle { get; init; }
    public MaterialStateProperty<Color?>? DragHandleColor { get; init; }
    public Size? DragHandleSize { get; init; }
    public Clip? ClipBehavior { get; init; }
    public BoxConstraints? Constraints { get; init; }

    private static void ValidateElevation(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class BottomSheetTheme : InheritedWidget
{
    public BottomSheetTheme(BottomSheetThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BottomSheetThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((BottomSheetTheme)oldWidget).Data, Data);

    public static BottomSheetThemeData Of(BuildContext context) =>
        context.DependOnInherited<BottomSheetTheme>()?.Data ?? Theme.Of(context).BottomSheetTheme;
}
