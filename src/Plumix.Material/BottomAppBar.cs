using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/bottom_app_bar.dart

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
        if (!double.IsFinite(notchMargin) || notchMargin < 0) throw new ArgumentOutOfRangeException(nameof(notchMargin));
        if (height.HasValue && (!double.IsFinite(height.Value) || height.Value < 0)) throw new ArgumentOutOfRangeException(nameof(height));
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
        return Height ?? barTheme.Height ?? (theme.UseMaterial3 ? 80.0 : 56.0);
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
                        ? theme.SurfaceContainerColor
                        : theme.Brightness == Brightness.Dark ? Color.Parse("#FF424242") : Colors.White);
        var surfaceTint = widget.SurfaceTintColor
                          ?? barTheme.SurfaceTintColor
                          ?? Colors.Transparent;
        var shadowColor = widget.ShadowColor
                          ?? barTheme.ShadowColor
                          ?? (useMaterial3 ? Colors.Transparent : Colors.Black);
        var shape = widget.Shape
                    ?? barTheme.Shape
                    ?? (useMaterial3
                        ? new AutomaticNotchedShape(ShapeBorder.RoundedRectangle(0))
                        : null);
        var padding = widget.Padding
                      ?? barTheme.Padding
                      ?? (useMaterial3 ? new Thickness(16, 12) : default);

        if (useMaterial3 && surfaceTint.A > 0 && elevation > 0)
        {
            color = NavigationSurfaceUtilities.ApplySurfaceTint(color, surfaceTint, elevation);
        }
        else if (!useMaterial3 && theme.Brightness == Brightness.Dark && elevation > 0)
        {
            color = NavigationSurfaceUtilities.ApplySurfaceTint(color, theme.PrimaryColor, elevation);
        }

        Widget child = new SizedBox(
            height: height,
            child: new Padding(
                padding,
                widget.Child ?? new SizedBox()));
        child = new SafeArea(
            left: false,
            top: false,
            right: false,
            child: child);

        var scaffold = Scaffold.MaybeOf(context);
        bool hasFab = scaffold?.HasFloatingActionButton == true;
        var fabSize = scaffold?.FloatingActionButtonSize ?? new Size(56, 56);
        return new BottomAppBarSurface(
            color: color,
            elevation: elevation,
            shadowColor: shadowColor,
            shape: shape,
            notchMargin: widget.NotchMargin,
            clipBehavior: widget.ClipBehavior,
            hasFloatingActionButton: hasFab,
            floatingActionButtonSize: fabSize,
            textDirection: Directionality.Of(context),
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
        Size floatingActionButtonSize,
        TextDirection textDirection,
        Widget child) : base(child)
    {
        Color = color;
        Elevation = elevation;
        ShadowColor = shadowColor;
        Shape = shape;
        NotchMargin = notchMargin;
        ClipBehavior = clipBehavior;
        HasFloatingActionButton = hasFloatingActionButton;
        FloatingActionButtonSize = floatingActionButtonSize;
        TextDirection = textDirection;
    }

    public Color Color { get; }
    public double Elevation { get; }
    public Color ShadowColor { get; }
    public NotchedShape? Shape { get; }
    public double NotchMargin { get; }
    public Clip ClipBehavior { get; }
    public bool HasFloatingActionButton { get; }
    public Size FloatingActionButtonSize { get; }
    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderBottomAppBarSurface(
        Color,
        Elevation,
        ShadowColor,
        Shape,
        NotchMargin,
        ClipBehavior,
        HasFloatingActionButton,
        FloatingActionButtonSize,
        TextDirection);

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
        surface.FloatingActionButtonSize = FloatingActionButtonSize;
        surface.TextDirection = TextDirection;
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
    private Size _floatingActionButtonSize;
    private TextDirection _textDirection;

    public RenderBottomAppBarSurface(
        Color color,
        double elevation,
        Color shadowColor,
        NotchedShape? shape,
        double notchMargin,
        Clip clipBehavior,
        bool hasFloatingActionButton,
        Size floatingActionButtonSize,
        TextDirection textDirection)
    {
        _color = color;
        _elevation = elevation;
        _shadowColor = shadowColor;
        _shape = shape;
        _notchMargin = notchMargin;
        _clipBehavior = clipBehavior;
        _hasFloatingActionButton = hasFloatingActionButton;
        _floatingActionButtonSize = floatingActionButtonSize;
        _textDirection = textDirection;
    }

    public Color Color { get => _color; set { if (_color != value) { _color = value; MarkNeedsPaint(); } } }
    public double Elevation { get => _elevation; set { if (Math.Abs(_elevation - value) > 0.0001) { _elevation = value; MarkNeedsPaint(); } } }
    public Color ShadowColor { get => _shadowColor; set { if (_shadowColor != value) { _shadowColor = value; MarkNeedsPaint(); } } }
    public NotchedShape? Shape { get => _shape; set { if (!Equals(_shape, value)) { _shape = value; MarkNeedsPaint(); } } }
    public double NotchMargin { get => _notchMargin; set { if (Math.Abs(_notchMargin - value) > 0.0001) { _notchMargin = value; MarkNeedsPaint(); } } }
    public Clip ClipBehavior { get => _clipBehavior; set { if (_clipBehavior != value) { _clipBehavior = value; MarkNeedsPaint(); } } }
    public bool HasFloatingActionButton { get => _hasFloatingActionButton; set { if (_hasFloatingActionButton != value) { _hasFloatingActionButton = value; MarkNeedsPaint(); } } }
    public Size FloatingActionButtonSize { get => _floatingActionButtonSize; set { if (_floatingActionButtonSize != value) { _floatingActionButtonSize = value; MarkNeedsPaint(); } } }
    public TextDirection TextDirection { get => _textDirection; set { if (_textDirection != value) { _textDirection = value; MarkNeedsPaint(); } } }

    internal Rect? GuestRect => ResolveGuestRect();

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;
        var host = new Rect(Size);
        var geometry = Shape?.GetOuterPath(host, ResolveGuestRect()) ?? new RectangleGeometry(host);
        var shadows = BuildBoxShadows(ShadowColor, Elevation);
        if (shadows.HasValue)
        {
            context.DrawRectangle(Brushes.Transparent, null, new Rect(offset, Size), boxShadows: shadows.Value);
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
        if (!HasFloatingActionButton || Shape is null) return null;
        double width = Math.Max(0, FloatingActionButtonSize.Width + (NotchMargin * 2));
        double height = Math.Max(0, FloatingActionButtonSize.Height + (NotchMargin * 2));
        double centerX = TextDirection == TextDirection.Ltr
            ? Size.Width - 16 - (FloatingActionButtonSize.Width / 2)
            : 16 + (FloatingActionButtonSize.Width / 2);
        return new Rect(centerX - (width / 2), -(height / 2), width, height);
    }

    private static BoxShadows? BuildBoxShadows(Color color, double elevation)
    {
        if (elevation <= 0 || color.A == 0) return null;
        return new BoxShadows(new BoxShadow
        {
            OffsetY = Math.Max(1, elevation * 0.5),
            Blur = Math.Max(2, elevation * 2.4),
            Color = NavigationSurfaceUtilities.WithOpacity(color, 0.20),
        });
    }
}
