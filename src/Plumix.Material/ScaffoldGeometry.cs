using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scaffold.dart

/// <summary>
/// Geometry information for <see cref="Scaffold"/> components after layout is finalized.
/// </summary>
public sealed class ScaffoldGeometry
{
    public ScaffoldGeometry(double? bottomNavigationBarTop = null, Rect? floatingActionButtonArea = null)
    {
        BottomNavigationBarTop = bottomNavigationBarTop;
        FloatingActionButtonArea = floatingActionButtonArea;
    }

    /// <summary>The distance from the <see cref="Scaffold"/>'s top edge to the top edge of the rectangle in
    /// which the <see cref="Scaffold.BottomNavigationBar"/> is being laid out.</summary>
    public double? BottomNavigationBarTop { get; }

    /// <summary>The <see cref="Scaffold.FloatingActionButton"/>'s bounding rectangle, in coordinates relative
    /// to the <see cref="Scaffold"/>'s origin.</summary>
    public Rect? FloatingActionButtonArea { get; }

    internal ScaffoldGeometry ScaleFloatingActionButton(double scaleFactor)
    {
        if (scaleFactor == 1.0)
        {
            return this;
        }

        if (scaleFactor == 0.0)
        {
            return new ScaffoldGeometry(bottomNavigationBarTop: BottomNavigationBarTop);
        }

        Rect area = FloatingActionButtonArea!.Value;
        var collapsed = new Rect(area.Center, default(Size));
        var scaledButton = new Rect(
            LerpDouble(collapsed.Left, area.Left, scaleFactor),
            LerpDouble(collapsed.Top, area.Top, scaleFactor),
            LerpDouble(collapsed.Width, area.Width, scaleFactor),
            LerpDouble(collapsed.Height, area.Height, scaleFactor));
        return CopyWith(floatingActionButtonArea: scaledButton);
    }

    public ScaffoldGeometry CopyWith(double? bottomNavigationBarTop = null, Rect? floatingActionButtonArea = null)
    {
        return new ScaffoldGeometry(
            bottomNavigationBarTop: bottomNavigationBarTop ?? BottomNavigationBarTop,
            floatingActionButtonArea: floatingActionButtonArea ?? FloatingActionButtonArea);
    }

    private static double LerpDouble(double a, double b, double t) => a + ((b - a) * t);
}

/// <summary>
/// Ports Flutter's private <c>_ScaffoldGeometryNotifier</c>: the live <see cref="ScaffoldGeometry"/> a
/// <see cref="Scaffold"/> publishes to its descendants (notably <see cref="BottomAppBar"/>).
/// </summary>
internal sealed class ScaffoldGeometryNotifier : ChangeNotifier, IValueListenable<ScaffoldGeometry>
{
    private readonly BuildContext _context;

    public ScaffoldGeometryNotifier(ScaffoldGeometry geometry, BuildContext context)
    {
        Geometry = geometry;
        _context = context;
    }

    public ScaffoldGeometry Geometry { get; private set; }

    public double? FloatingActionButtonScale { get; private set; }

    public ScaffoldGeometry Value
    {
        get
        {
            RenderObject? renderObject = _context.FindRenderObject();
            if (renderObject is null || renderObject.Owner?.DebugDoingPaint != true)
            {
                throw new InvalidOperationException(
                    "Scaffold.geometryOf() must only be accessed during the paint phase.\n"
                    + "The ScaffoldGeometry is only available during the paint phase, because its value is "
                    + "computed during the animation and layout phases prior to painting.");
            }

            return Geometry.ScaleFloatingActionButton(FloatingActionButtonScale!.Value);
        }
    }

    /// <summary>Reads the geometry without the paint-phase assertion; used by the scaffold's own layout.</summary>
    internal ScaffoldGeometry ValueForLayout =>
        Geometry.ScaleFloatingActionButton(FloatingActionButtonScale ?? 1.0);

    internal void UpdateWith(
        double? bottomNavigationBarTop = null,
        Rect? floatingActionButtonArea = null,
        double? floatingActionButtonScale = null)
    {
        FloatingActionButtonScale = floatingActionButtonScale ?? FloatingActionButtonScale;
        Geometry = Geometry.CopyWith(
            bottomNavigationBarTop: bottomNavigationBarTop,
            floatingActionButtonArea: floatingActionButtonArea);
        NotifyListeners();
    }
}
