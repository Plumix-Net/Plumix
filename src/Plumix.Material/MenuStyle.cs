using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/menu_style.dart

/// The visual properties that menus have in common.
///
/// Flutter declares this as an ordinary class so `_MenuBarDefaultsM3`/`_MenuDefaultsM3` can extend it
/// and override individual getters against the ambient `ColorScheme`; Plumix keeps that shape, which
/// is why the members are `virtual` rather than record properties.
public class MenuStyle : IDiagnosticable
{
    public MenuStyle(
        MaterialStateProperty<Color?>? backgroundColor = null,
        MaterialStateProperty<Color?>? shadowColor = null,
        MaterialStateProperty<Color?>? surfaceTintColor = null,
        MaterialStateProperty<double?>? elevation = null,
        MaterialStateProperty<EdgeInsetsGeometry?>? padding = null,
        MaterialStateProperty<Size?>? minimumSize = null,
        MaterialStateProperty<Size?>? fixedSize = null,
        MaterialStateProperty<Size?>? maximumSize = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<OutlinedBorder?>? shape = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        VisualDensity? visualDensity = null,
        AlignmentGeometry? alignment = null)
    {
        BackgroundColor = backgroundColor;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Elevation = elevation;
        Padding = padding;
        MinimumSize = minimumSize;
        FixedSize = fixedSize;
        MaximumSize = maximumSize;
        Side = side;
        Shape = shape;
        MouseCursor = mouseCursor;
        VisualDensity = visualDensity;
        Alignment = alignment;
    }

    /// <summary>The menu's background fill color.</summary>
    public virtual MaterialStateProperty<Color?>? BackgroundColor { get; }

    /// <summary>The shadow color of the menu's <see cref="Material"/>.</summary>
    public virtual MaterialStateProperty<Color?>? ShadowColor { get; }

    /// <summary>The surface tint color of the menu's <see cref="Material"/>.</summary>
    public virtual MaterialStateProperty<Color?>? SurfaceTintColor { get; }

    /// <summary>The elevation of the menu's <see cref="Material"/>.</summary>
    public virtual MaterialStateProperty<double?>? Elevation { get; }

    /// <summary>The padding between the menu's boundary and its child.</summary>
    public virtual MaterialStateProperty<EdgeInsetsGeometry?>? Padding { get; }

    /// <summary>The minimum size of the menu itself.</summary>
    public virtual MaterialStateProperty<Size?>? MinimumSize { get; }

    /// <summary>The menu's size; infinite dimensions are ignored.</summary>
    public virtual MaterialStateProperty<Size?>? FixedSize { get; }

    /// <summary>The maximum size of the menu itself.</summary>
    public virtual MaterialStateProperty<Size?>? MaximumSize { get; }

    /// <summary>The color and weight of the menu's outline, combined with <see cref="Shape"/>.</summary>
    public virtual MaterialStateProperty<BorderSide?>? Side { get; }

    /// <summary>The shape of the menu's underlying <see cref="Material"/>.</summary>
    public virtual MaterialStateProperty<OutlinedBorder?>? Shape { get; }

    /// <summary>The cursor for a mouse pointer hovering over this menu's ink well.</summary>
    public virtual MaterialStateProperty<MouseCursor?>? MouseCursor { get; }

    /// <summary>Defines how compact the menu's layout will be.</summary>
    public virtual VisualDensity? VisualDensity { get; }

    /// <summary>The desired alignment of the submenu relative to the button that opens it.</summary>
    public virtual AlignmentGeometry? Alignment { get; }

    /// <summary>Returns a copy of this style with the given fields replaced by the new values.</summary>
    public MenuStyle CopyWith(
        MaterialStateProperty<Color?>? backgroundColor = null,
        MaterialStateProperty<Color?>? shadowColor = null,
        MaterialStateProperty<Color?>? surfaceTintColor = null,
        MaterialStateProperty<double?>? elevation = null,
        MaterialStateProperty<EdgeInsetsGeometry?>? padding = null,
        MaterialStateProperty<Size?>? minimumSize = null,
        MaterialStateProperty<Size?>? fixedSize = null,
        MaterialStateProperty<Size?>? maximumSize = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<OutlinedBorder?>? shape = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        VisualDensity? visualDensity = null,
        AlignmentGeometry? alignment = null)
    {
        return new MenuStyle(
            backgroundColor: backgroundColor ?? BackgroundColor,
            shadowColor: shadowColor ?? ShadowColor,
            surfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            elevation: elevation ?? Elevation,
            padding: padding ?? Padding,
            minimumSize: minimumSize ?? MinimumSize,
            fixedSize: fixedSize ?? FixedSize,
            maximumSize: maximumSize ?? MaximumSize,
            side: side ?? Side,
            shape: shape ?? Shape,
            mouseCursor: mouseCursor ?? MouseCursor,
            visualDensity: visualDensity ?? VisualDensity,
            alignment: alignment ?? Alignment);
    }

    /// <summary>
    /// Returns a copy of this style where the non-null fields in <paramref name="style"/> have
    /// replaced the corresponding null fields in this style.
    /// </summary>
    public MenuStyle Merge(MenuStyle? style)
    {
        if (style is null)
        {
            return this;
        }

        return CopyWith(
            backgroundColor: BackgroundColor ?? style.BackgroundColor,
            shadowColor: ShadowColor ?? style.ShadowColor,
            surfaceTintColor: SurfaceTintColor ?? style.SurfaceTintColor,
            elevation: Elevation ?? style.Elevation,
            padding: Padding ?? style.Padding,
            minimumSize: MinimumSize ?? style.MinimumSize,
            fixedSize: FixedSize ?? style.FixedSize,
            maximumSize: MaximumSize ?? style.MaximumSize,
            side: Side ?? style.Side,
            shape: Shape ?? style.Shape,
            mouseCursor: MouseCursor ?? style.MouseCursor,
            visualDensity: VisualDensity ?? style.VisualDensity,
            alignment: Alignment ?? style.Alignment);
    }

    /// <summary>Linearly interpolates between two <see cref="MenuStyle"/>s.</summary>
    public static MenuStyle? Lerp(MenuStyle? a, MenuStyle? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new MenuStyle(
            backgroundColor: MaterialThemeLerp.ColorStateProperty(a?.BackgroundColor, b?.BackgroundColor, t),
            shadowColor: MaterialThemeLerp.ColorStateProperty(a?.ShadowColor, b?.ShadowColor, t),
            surfaceTintColor: MaterialThemeLerp.ColorStateProperty(
                a?.SurfaceTintColor,
                b?.SurfaceTintColor,
                t),
            elevation: MaterialThemeLerp.DoubleStateProperty(a?.Elevation, b?.Elevation, t),
            padding: MaterialThemeLerp.EdgeInsetsStateProperty(a?.Padding, b?.Padding, t),
            minimumSize: MaterialThemeLerp.SizeStateProperty(a?.MinimumSize, b?.MinimumSize, t),
            fixedSize: MaterialThemeLerp.SizeStateProperty(a?.FixedSize, b?.FixedSize, t),
            maximumSize: MaterialThemeLerp.SizeStateProperty(a?.MaximumSize, b?.MaximumSize, t),
            side: MaterialThemeLerp.BorderSideStateProperty(a?.Side, b?.Side, t),
            shape: MaterialThemeLerp.OutlinedBorderStateProperty(a?.Shape, b?.Shape, t),
            mouseCursor: t < 0.5 ? a?.MouseCursor : b?.MouseCursor,
            visualDensity: t < 0.5 ? a?.VisualDensity : b?.VisualDensity,
            alignment: AlignmentGeometry.Lerp(a?.Alignment, b?.Alignment, t));
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        // Dart's `operator ==` starts with a runtimeType check, so a defaults subclass is never equal
        // to a plain MenuStyle carrying the same values.
        if (obj is not MenuStyle other || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(BackgroundColor, other.BackgroundColor)
               && Equals(ShadowColor, other.ShadowColor)
               && Equals(SurfaceTintColor, other.SurfaceTintColor)
               && Equals(Elevation, other.Elevation)
               && Equals(Padding, other.Padding)
               && Equals(MinimumSize, other.MinimumSize)
               && Equals(FixedSize, other.FixedSize)
               && Equals(MaximumSize, other.MaximumSize)
               && Equals(Side, other.Side)
               && Equals(Shape, other.Shape)
               && Equals(MouseCursor, other.MouseCursor)
               && Equals(VisualDensity, other.VisualDensity)
               && Equals(Alignment, other.Alignment);
    }

    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "backgroundColor",
            BackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "shadowColor",
            ShadowColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Color?>?>(
            "surfaceTintColor",
            SurfaceTintColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<double?>?>(
            "elevation",
            Elevation,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<EdgeInsetsGeometry?>?>(
            "padding",
            Padding,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "minimumSize",
            MinimumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "fixedSize",
            FixedSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<Size?>?>(
            "maximumSize",
            MaximumSize,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<BorderSide?>?>(
            "side",
            Side,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<OutlinedBorder?>?>(
            "shape",
            Shape,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<MaterialStateProperty<MouseCursor?>?>(
            "mouseCursor",
            MouseCursor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<VisualDensity?>(
            "visualDensity",
            VisualDensity,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<AlignmentGeometry?>(
            "alignment",
            Alignment,
            defaultValue: nullDefault));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BackgroundColor);
        hash.Add(ShadowColor);
        hash.Add(SurfaceTintColor);
        hash.Add(Elevation);
        hash.Add(Padding);
        hash.Add(MinimumSize);
        hash.Add(FixedSize);
        hash.Add(MaximumSize);
        hash.Add(Side);
        hash.Add(Shape);
        hash.Add(MouseCursor);
        hash.Add(VisualDensity);
        hash.Add(Alignment);
        return hash.ToHashCode();
    }
}
