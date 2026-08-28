using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Plumix.Painting;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/bottom_sheet_theme.dart

public sealed partial record BottomSheetThemeData
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
        WidgetStateColor? DragHandleColor = null,
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
    public WidgetStateColor? DragHandleColor { get; init; }
    public Size? DragHandleSize { get; init; }
    public Clip? ClipBehavior { get; init; }
    public BoxConstraints? Constraints { get; init; }

    public BottomSheetThemeData CopyWith(
        Color? backgroundColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        Color? modalBackgroundColor = null,
        Color? modalBarrierColor = null,
        Color? shadowColor = null,
        double? modalElevation = null,
        ShapeBorder? shape = null,
        bool? showDragHandle = null,
        WidgetStateColor? dragHandleColor = null,
        Size? dragHandleSize = null,
        Clip? clipBehavior = null,
        BoxConstraints? constraints = null)
    {
        return new BottomSheetThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Elevation: elevation ?? Elevation,
            ModalBackgroundColor: modalBackgroundColor ?? ModalBackgroundColor,
            ModalBarrierColor: modalBarrierColor ?? ModalBarrierColor,
            ShadowColor: shadowColor ?? ShadowColor,
            ModalElevation: modalElevation ?? ModalElevation,
            Shape: shape ?? Shape,
            ShowDragHandle: showDragHandle ?? ShowDragHandle,
            DragHandleColor: dragHandleColor ?? DragHandleColor,
            DragHandleSize: dragHandleSize ?? DragHandleSize,
            ClipBehavior: clipBehavior ?? ClipBehavior,
            Constraints: constraints ?? Constraints);
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new ColorProperty(
            "backgroundColor",
            BackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "surfaceTintColor",
            SurfaceTintColor,
            defaultValue: nullDefault));
        properties.Add(new DoubleProperty("elevation", Elevation, defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "modalBackgroundColor",
            ModalBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty("shadowColor", ShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "modalBarrierColor",
            ModalBarrierColor,
            defaultValue: nullDefault));
        properties.Add(new DoubleProperty("modalElevation", ModalElevation, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ShapeBorder?>("shape", Shape, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("showDragHandle", ShowDragHandle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateColor?>(
            "dragHandleColor",
            DragHandleColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Size?>("dragHandleSize", DragHandleSize, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Clip?>("clipBehavior", ClipBehavior, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BoxConstraints?>(
            "constraints",
            Constraints,
            defaultValue: nullDefault));
    }

    private static void ValidateElevation(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

public sealed class BottomSheetTheme : InheritedTheme
{
    public BottomSheetTheme(BottomSheetThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BottomSheetThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new BottomSheetTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((BottomSheetTheme)oldWidget).Data, Data);

    public static BottomSheetThemeData Of(BuildContext context) =>
        context.DependOnInherited<BottomSheetTheme>()?.Data ?? Theme.Of(context).BottomSheetTheme;
}
