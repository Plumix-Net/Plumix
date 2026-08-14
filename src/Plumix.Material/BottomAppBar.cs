using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/bottom_app_bar.dart

public sealed class BottomAppBar : StatefulWidget
{
    public BottomAppBar(
        Widget? child = null,
        Color? color = null,
        double? elevation = null,
        NotchedShape? shape = null,
        Clip clipBehavior = Clip.None,
        double notchMargin = 4.0,
        Thickness? padding = null,
        Color? surfaceTintColor = null,
        Color? shadowColor = null,
        double? height = null,
        Key? key = null) : base(key)
    {
        ValidateNonNegative(nameof(elevation), elevation);
        if (!double.IsFinite(notchMargin) || notchMargin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(notchMargin));
        }

        if (height.HasValue && (!double.IsFinite(height.Value) || height.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        Child = child;
        Color = color;
        Elevation = elevation;
        Shape = shape;
        ClipBehavior = clipBehavior;
        NotchMargin = notchMargin;
        Padding = padding;
        SurfaceTintColor = surfaceTintColor;
        ShadowColor = shadowColor;
        Height = height;
    }

    public Widget? Child { get; }
    public Color? Color { get; }
    public double? Elevation { get; }
    public NotchedShape? Shape { get; }
    public Clip ClipBehavior { get; }
    public double NotchMargin { get; }
    public Thickness? Padding { get; }
    public Color? SurfaceTintColor { get; }
    public Color? ShadowColor { get; }
    public double? Height { get; }

    public override State CreateState() => new BottomAppBarState();

    internal double ResolveHeightForScaffold(BuildContext context)
    {
        var theme = Theme.Of(context);
        var barTheme = BottomAppBarTheme.Of(context);
        double contentHeight = Height ?? barTheme.Height ?? (theme.UseMaterial3 ? 80.0 : 56.0);
        double bottomPadding = MediaQuery.MaybeOf(context)?.Padding.Bottom ?? 0.0;
        return contentHeight + bottomPadding;
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

internal sealed class BottomAppBarState : State
{
    private BottomAppBar CurrentWidget => (BottomAppBar)StateWidget;

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var barTheme = BottomAppBarTheme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        double elevation = widget.Elevation ?? barTheme.Elevation ?? (useMaterial3 ? 3.0 : 8.0);
        double? height = widget.Height ?? barTheme.Height ?? (useMaterial3 ? 80.0 : (double?)null);
        var color = widget.Color
                    ?? barTheme.Color
                    ?? (useMaterial3
                        ? theme.ColorScheme.SurfaceContainer
                        : theme.Brightness == Brightness.Dark ? Color.Parse("#FF424242") : Colors.White);
        var surfaceTint = widget.SurfaceTintColor
                          ?? barTheme.SurfaceTintColor
                          ?? (useMaterial3 ? Colors.Transparent : theme.ColorScheme.SurfaceTint);
        var shadowColor = widget.ShadowColor
                          ?? barTheme.ShadowColor
                          ?? (useMaterial3 ? Colors.Transparent : Colors.Black);
        var shape = widget.Shape
                    ?? barTheme.Shape
                    ?? (useMaterial3
                        ? new AutomaticNotchedShape(new RoundedRectangleBorder(borderRadius:
                            Plumix.Rendering.BorderRadius.Circular(0)))
                        : null);
        var padding = widget.Padding
                      ?? barTheme.Padding
                      ?? (useMaterial3 ? new Thickness(16, 12) : default);

        color = useMaterial3
            ? ElevationOverlay.ApplySurfaceTint(color, surfaceTint, elevation)
            : ElevationOverlay.ApplyOverlay(theme, color, elevation);

        Widget child = new SizedBox(
            height: height,
            child: new Padding(
                padding,
                widget.Child ?? new SizedBox()));
        child = new Material(
            type: MaterialType.Transparency,
            child: new SafeArea(
                left: false,
                top: false,
                right: false,
                child: child));

        var scaffold = Scaffold.MaybeOf(context);
        ScaffoldGeometryNotifier? geometryNotifier = Scaffold.GeometryNotifierMaybeOf(context);
        bool hasFab = scaffold?.HasFloatingActionButton == true;
        return new BottomAppBarSurface(
            color: color,
            elevation: elevation,
            shadowColor: shadowColor,
            shape: shape,
            notchMargin: widget.NotchMargin,
            clipBehavior: widget.ClipBehavior,
            hasFloatingActionButton: hasFab,
            geometryNotifier: geometryNotifier,
            child: child);
    }
}

internal sealed class BottomAppBarSurface : SingleChildRenderObjectWidget
{
    public BottomAppBarSurface(
        Color color,
        double elevation,
        Color shadowColor,
        NotchedShape? shape,
        double notchMargin,
        Clip clipBehavior,
        bool hasFloatingActionButton,
        ScaffoldGeometryNotifier? geometryNotifier,
        Widget child) : base(child)
    {
        Color = color;
        Elevation = elevation;
        ShadowColor = shadowColor;
        Shape = shape;
        NotchMargin = notchMargin;
        ClipBehavior = clipBehavior;
        HasFloatingActionButton = hasFloatingActionButton;
        GeometryNotifier = geometryNotifier;
    }

    public Color Color { get; }
    public double Elevation { get; }
    public Color ShadowColor { get; }
    public NotchedShape? Shape { get; }
    public double NotchMargin { get; }
    public Clip ClipBehavior { get; }
    public bool HasFloatingActionButton { get; }
    public ScaffoldGeometryNotifier? GeometryNotifier { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderBottomAppBarSurface(
        Color,
        Elevation,
        ShadowColor,
        Shape,
        NotchMargin,
        ClipBehavior,
        HasFloatingActionButton,
        GeometryNotifier);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var surface = (RenderBottomAppBarSurface)renderObject;
        surface.Color = Color;
        surface.Elevation = Elevation;
        surface.ShadowColor = ShadowColor;
        surface.Shape = Shape;
        surface.NotchMargin = NotchMargin;
        surface.ClipBehavior = ClipBehavior;
        surface.HasFloatingActionButton = HasFloatingActionButton;
        surface.GeometryNotifier = GeometryNotifier;
    }
}

internal sealed class RenderBottomAppBarSurface : RenderProxyBox
{
    private Color _color;
    private double _elevation;
    private Color _shadowColor;
    private NotchedShape? _shape;
    private double _notchMargin;
    private Clip _clipBehavior;
    private bool _hasFloatingActionButton;
    private ScaffoldGeometryNotifier? _geometryNotifier;

    public RenderBottomAppBarSurface(
        Color color,
        double elevation,
        Color shadowColor,
        NotchedShape? shape,
        double notchMargin,
        Clip clipBehavior,
        bool hasFloatingActionButton,
        ScaffoldGeometryNotifier? geometryNotifier)
    {
        _color = color;
        _elevation = elevation;
        _shadowColor = shadowColor;
        _shape = shape;
        _notchMargin = notchMargin;
        _clipBehavior = clipBehavior;
        _hasFloatingActionButton = hasFloatingActionButton;
        _geometryNotifier = geometryNotifier;
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            MarkNeedsPaint();
        }
    }

    public double Elevation
    {
        get => _elevation;
        set
        {
            if (Math.Abs(_elevation - value) <= 0.0001) return;
            _elevation = value;
            MarkNeedsPaint();
        }
    }

    public Color ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor == value) return;
            _shadowColor = value;
            MarkNeedsPaint();
        }
    }

    public NotchedShape? Shape
    {
        get => _shape;
        set
        {
            if (Equals(_shape, value)) return;
            _shape = value;
            MarkNeedsPaint();
        }
    }

    public double NotchMargin
    {
        get => _notchMargin;
        set
        {
            if (Math.Abs(_notchMargin - value) <= 0.0001) return;
            _notchMargin = value;
            MarkNeedsPaint();
        }
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value) return;
            _clipBehavior = value;
            MarkNeedsPaint();
        }
    }

    public bool HasFloatingActionButton
    {
        get => _hasFloatingActionButton;
        set
        {
            if (_hasFloatingActionButton == value) return;
            _hasFloatingActionButton = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>
    /// The live scaffold geometry the notch tracks. Ports the <c>reclip</c> listenable Flutter's
    /// <c>_BottomAppBarClipper</c> subscribes to, so a moving floating action button repaints the notch
    /// without rebuilding the bar.
    /// </summary>
    public ScaffoldGeometryNotifier? GeometryNotifier
    {
        get => _geometryNotifier;
        set
        {
            if (ReferenceEquals(_geometryNotifier, value)) return;
            if (Attached)
            {
                _geometryNotifier?.RemoveListener(MarkNeedsPaint);
                value?.AddListener(MarkNeedsPaint);
            }

            _geometryNotifier = value;
            MarkNeedsPaint();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _geometryNotifier?.AddListener(MarkNeedsPaint);
    }

    protected override void OnDetach()
    {
        _geometryNotifier?.RemoveListener(MarkNeedsPaint);
        base.OnDetach();
    }

    internal Rect? GuestRect => ResolveGuestRect();

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        var host = new Rect(Size);
        if (!ShapeContains(host, ResolveGuestRect(), position))
        {
            return false;
        }

        return base.HitTest(result, position);
    }

    private bool ShapeContains(Rect host, Rect? guest, Point position)
    {
        if (!host.Contains(position))
        {
            return false;
        }

        if (Shape is null)
        {
            return true;
        }

        try
        {
            return Shape.GetOuterPath(host, guest).FillContains(position);
        }
        catch (InvalidOperationException)
        {
            // Host-less tests do not install Avalonia's platform geometry implementation.
            if (!guest.HasValue || !host.Intersects(guest.Value))
            {
                return true;
            }

            if (Shape is AutomaticNotchedShape automaticShape)
            {
                return automaticShape.Guest is null || !guest.Value.Contains(position);
            }

            if (Shape is CircularNotchedRectangle)
            {
                Rect guestRect = guest.Value;
                double radiusX = guestRect.Width / 2.0;
                double radiusY = guestRect.Height / 2.0;
                if (radiusX <= 0.0 || radiusY <= 0.0)
                {
                    return true;
                }

                double normalizedX = (position.X - guestRect.Center.X) / radiusX;
                double normalizedY = (position.Y - guestRect.Center.Y) / radiusY;
                return (normalizedX * normalizedX) + (normalizedY * normalizedY) > 1.0;
            }

            return true;
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;
        var host = new Rect(Size);
        var geometry = Shape?.GetOuterPath(host, ResolveGuestRect()) ?? new RectangleGeometry(host);
        if (Elevation > 0.0 && ShadowColor.A > 0)
        {
            context.DrawShadow(
                geometry,
                ShadowColor,
                Elevation,
                transparentOccluder: Color.A != byte.MaxValue,
                geometryOffset: offset);
        }
        context.PushTransform(
            Matrix.CreateTranslation(offset.X, offset.Y),
            local => local.DrawGeometry(new SolidColorBrush(Color), null, geometry));

        if (Child is null) return;
        var childOffset = ((BoxParentData)Child.parentData!).offset + offset;
        if (ClipBehavior == Clip.None)
        {
            context.PaintChild(Child, childOffset);
            return;
        }

        context.PushTransform(
            Matrix.CreateTranslation(offset.X, offset.Y),
            local => local.PushClipGeometry(
                geometry,
                clipped => clipped.PaintChild(Child, ((BoxParentData)Child.parentData!).offset)));
    }

    private Rect? ResolveGuestRect()
    {
        if (!HasFloatingActionButton || Shape is null || _geometryNotifier is null)
        {
            return null;
        }

        ScaffoldGeometry geometry = _geometryNotifier.ValueForLayout;
        if (geometry.FloatingActionButtonArea is not { } area || geometry.BottomNavigationBarTop is not { } top)
        {
            return null;
        }

        var localArea = new Rect(area.X, area.Y - top, area.Width, area.Height);
        return localArea.Inflate(NotchMargin);
    }
}
