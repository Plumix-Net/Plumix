using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/drawer.dart

public sealed class Drawer : StatelessWidget
{
    private const double DefaultWidth = 304.0;
    private const double DefaultM2Elevation = 16.0;
    private const double DefaultM3Elevation = 1.0;
    private const double DefaultM3Radius = 16.0;

    public Drawer(
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        double? width = null,
        Widget? child = null,
        string? semanticLabel = null,
        Clip? clipBehavior = null,
        Key? key = null) : base(key)
    {
        if (elevation.HasValue && (double.IsNaN(elevation.Value) || elevation.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Drawer elevation must be non-negative.");
        }

        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Shape = shape;
        Width = width;
        Child = child;
        SemanticLabel = semanticLabel;
        ClipBehavior = clipBehavior;
    }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public Color? ShadowColor { get; }

    public Color? SurfaceTintColor { get; }

    public ShapeBorder? Shape { get; }

    public double? Width { get; }

    public Widget? Child { get; }

    public string? SemanticLabel { get; }

    public Clip? ClipBehavior { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var drawerTheme = DrawerTheme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        bool isDrawerStart = DrawerController.MaybeOf(context)?.Alignment != DrawerAlignment.End;
        TextDirection direction = Directionality.Of(context);
        Color? effectiveBackground = BackgroundColor
                                     ?? drawerTheme.BackgroundColor
                                     ?? (useMaterial3 ? theme.ColorScheme.SurfaceContainerLow : null);
        double effectiveElevation = Elevation
                                    ?? drawerTheme.Elevation
                                    ?? (useMaterial3 ? DefaultM3Elevation : DefaultM2Elevation);
        double effectiveWidth = Width ?? drawerTheme.Width ?? DefaultWidth;
        Color? effectiveShadowColor = ShadowColor
                                      ?? drawerTheme.ShadowColor
                                      ?? (useMaterial3 ? Colors.Transparent : theme.ShadowColor);
        Color? effectiveSurfaceTintColor = SurfaceTintColor
                                           ?? drawerTheme.SurfaceTintColor
                                           ?? (useMaterial3 ? Colors.Transparent : null);
        ShapeBorder? effectiveShape = Shape
                                      ?? (isDrawerStart ? drawerTheme.Shape : drawerTheme.EndShape)
                                      ?? ResolveDefaultShape(useMaterial3, isDrawerStart, direction);
        Clip effectiveClip = effectiveShape is null
            ? Clip.None
            : ClipBehavior ?? drawerTheme.ClipBehavior ?? Clip.HardEdge;
        string? label = PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? SemanticLabel
            : SemanticLabel ?? MaterialLocalizations.Of(context).DrawerLabel;

        return new Semantics(
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            label: label,
            child: new ConstrainedBox(
                constraints: BoxConstraints.Expand(width: effectiveWidth),
                child: new Material(
                    color: effectiveBackground,
                    elevation: effectiveElevation,
                    shadowColor: effectiveShadowColor,
                    surfaceTintColor: effectiveSurfaceTintColor,
                    shape: effectiveShape,
                    clipBehavior: effectiveClip,
                    child: Child)));
    }

    internal double ResolveEffectiveWidthForScaffold(BuildContext context)
    {
        return Width ?? DrawerTheme.Of(context).Width ?? DefaultWidth;
    }

    private static ShapeBorder? ResolveDefaultShape(
        bool useMaterial3,
        bool isDrawerStart,
        TextDirection direction)
    {
        if (!useMaterial3)
        {
            return null;
        }

        bool isDrawerOnLeft = isDrawerStart == (direction == TextDirection.Ltr);
        BorderRadius radius = isDrawerOnLeft
            ? BorderRadius.Only(topRight: DefaultM3Radius, bottomRight: DefaultM3Radius)
            : BorderRadius.Only(topLeft: DefaultM3Radius, bottomLeft: DefaultM3Radius);
        return new ShapeBorder(radius);
    }
}
