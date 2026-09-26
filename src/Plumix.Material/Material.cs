using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/material.dart

/// <summary>
/// Signature for the callback used by ink effects to obtain the rectangle for the effect.
/// </summary>
public delegate Rect RectCallback();

/// <summary>
/// The various kinds of material in Material Design. Used to configure the default behavior of
/// <see cref="Material"/> widgets.
/// </summary>
public enum MaterialType
{
    /// <summary>Rectangle using default theme canvas color.</summary>
    Canvas,

    /// <summary>Rounded edges, card theme color.</summary>
    Card,

    /// <summary>A circle, no color by default (used for floating action buttons).</summary>
    Circle,

    /// <summary>Rounded edges, no color by default (used for <c>MaterialButton</c> buttons).</summary>
    Button,

    /// <summary>A transparent piece of material that draws ink splashes and highlights.</summary>
    Transparency,
}

/// <summary>Holds Dart's top-level <c>kMaterialEdges</c>.</summary>
public static class MaterialEdges
{
    /// <summary>
    /// The border radii used by the various kinds of material in Material Design. Dart's
    /// <c>kMaterialEdges</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<MaterialType, BorderRadius?> KMaterialEdges =
        new Dictionary<MaterialType, BorderRadius?>
        {
            [MaterialType.Canvas] = null,
            [MaterialType.Card] = BorderRadius.Circular(2.0),
            [MaterialType.Circle] = null,
            [MaterialType.Button] = BorderRadius.Circular(2.0),
            [MaterialType.Transparency] = null,
        };
}

/// <summary>
/// An interface for creating <c>InkSplash</c>es and <c>InkHighlight</c>s on a <see cref="Material"/>.
/// Typically obtained via <see cref="Material.Of"/>.
/// </summary>
public interface MaterialInkController
{
    /// <summary>The color of the material.</summary>
    Color? Color { get; }

    /// <summary>The ticker provider used by the controller.</summary>
    ITickerProvider Vsync { get; }

    /// <summary>
    /// Add an <see cref="InkFeature"/>, such as an <c>InkSplash</c> or an <c>InkHighlight</c>. The ink
    /// feature will paint as part of this controller.
    /// </summary>
    void AddInkFeature(InkFeature feature);

    /// <summary>Notifies the controller that one of its ink features needs to repaint.</summary>
    void MarkNeedsPaint();
}

/// <summary>
/// A piece of material: clips its subtree (when <see cref="ClipBehavior"/> asks for it), elevates it
/// with a shadow, and shows <see cref="InkFeature"/>s below its children.
/// </summary>
public sealed class Material : StatefulWidget
{
    /// <summary>The default radius of an ink splash in logical pixels.</summary>
    public const double DefaultSplashRadius = 35.0;

    /// <summary>
    /// Creates a piece of material. <paramref name="animationDuration"/> defaults to
    /// <c>kThemeChangeDuration</c> (<see cref="MaterialConstants.ThemeAnimationDuration"/>).
    /// </summary>
    public Material(
        MaterialType type = MaterialType.Canvas,
        double elevation = 0.0,
        Color? color = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        TextStyle? textStyle = null,
        BorderRadiusGeometry? borderRadius = null,
        ShapeBorder? shape = null,
        bool borderOnForeground = true,
        Clip clipBehavior = Clip.None,
        TimeSpan? animationDuration = null,
        Widget? child = null,
        bool animateColor = false,
        Key? key = null) : base(key)
    {
        DebugAssertions.Assert(elevation >= 0.0);
        DebugAssertions.Assert(!(shape is not null && borderRadius is not null));
        DebugAssertions.Assert(!(type == MaterialType.Circle && (borderRadius is not null || shape is not null)));
        Type = type;
        Elevation = elevation;
        Color = color;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        TextStyle = textStyle;
        BorderRadius = borderRadius;
        Shape = shape;
        BorderOnForeground = borderOnForeground;
        ClipBehavior = clipBehavior;
        AnimationDuration = animationDuration ?? MaterialConstants.ThemeAnimationDuration;
        Child = child;
        AnimateColor = animateColor;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget? Child { get; }

    /// <summary>The kind of material to show (e.g., card or canvas).</summary>
    public MaterialType Type { get; }

    /// <summary>Whether the color should be animated.</summary>
    public bool AnimateColor { get; }

    /// <summary>The z-coordinate at which to place this material relative to its parent.</summary>
    public double Elevation { get; }

    /// <summary>The color to paint the material. By default, the color is derived from <see cref="Type"/>.</summary>
    public Color? Color { get; }

    /// <summary>The color to paint the shadow below the material.</summary>
    public Color? ShadowColor { get; }

    /// <summary>The color of the surface tint overlay applied to the material color to indicate elevation.</summary>
    public Color? SurfaceTintColor { get; }

    /// <summary>The typographical style to use for text within this material.</summary>
    public TextStyle? TextStyle { get; }

    /// <summary>Defines the material's shape as well its shadow.</summary>
    public ShapeBorder? Shape { get; }

    /// <summary>Whether to paint the <see cref="Shape"/> border in front of the <see cref="Child"/>.</summary>
    public bool BorderOnForeground { get; }

    /// <summary>How to clip the subtree. Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>
    /// Defines the duration of animated changes for <see cref="Shape"/>, <see cref="Elevation"/>,
    /// <see cref="ShadowColor"/>, <see cref="SurfaceTintColor"/> and the elevation overlay.
    /// </summary>
    public TimeSpan AnimationDuration { get; }

    /// <summary>
    /// If non-null, the corners of this box are rounded by this value. Otherwise, the corners specified
    /// for the current <see cref="Type"/> of material are used. Ignored when <see cref="Shape"/> is non-null.
    /// </summary>
    public BorderRadiusGeometry? BorderRadius { get; }

    /// <summary>
    /// The ink controller from the closest instance of this class that encloses the given context
    /// within the closest <see cref="LookupBoundary"/>.
    /// </summary>
    public static MaterialInkController? MaybeOf(BuildContext context)
    {
        return LookupBoundary.FindAncestorRenderObjectOfType<RenderInkFeatures>(context);
    }

    /// <summary>
    /// The ink controller from the closest instance of <see cref="Material"/> that encloses the given
    /// context within the closest <see cref="LookupBoundary"/>. Throws when there is none.
    /// </summary>
    public static MaterialInkController Of(BuildContext context)
    {
        MaterialInkController? controller = MaybeOf(context);
        if (Constants.KDebugMode && controller is null)
        {
            if (LookupBoundary.DebugIsHidingAncestorRenderObjectOfType<RenderInkFeatures>(context))
            {
                throw new FlutterError(
                    "Material.of() was called with a context that does not have access to a Material widget.\n"
                    + "The context provided to Material.of() does have a Material widget ancestor, but it is "
                    + "hidden by a LookupBoundary. This can happen because you are using a widget that looks "
                    + "for a Material ancestor, but no such ancestor exists within the closest LookupBoundary.\n"
                    + "The context used was:\n"
                    + $"  {context}");
            }

            throw new FlutterError(
                "Material.of() was called with a context that does not contain a Material widget.\n"
                + "No Material widget ancestor could be found starting from the context that was passed to "
                + "Material.of(). This can happen because you are using a widget that looks for a Material "
                + "ancestor, but no such ancestor exists.\n"
                + "The context used was:\n"
                + $"  {context}");
        }

        // Dart's `controller!`.
        return controller ?? throw new InvalidOperationException("Null check operator used on a null value");
    }

    /// <inheritdoc />
    public override State CreateState() => new MaterialState();

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new EnumProperty<MaterialType>("type", Type));
        properties.Add(new DoubleProperty("elevation", Elevation, defaultValue: 0.0));
        properties.Add(new ColorProperty("color", Color, defaultValue: nullDefault));
        properties.Add(new ColorProperty("shadowColor", ShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("surfaceTintColor", SurfaceTintColor, defaultValue: nullDefault));
        TextStyle?.DebugFillProperties(properties, prefix: "textStyle.");
        properties.Add(new DiagnosticsProperty<ShapeBorder>("shape", Shape, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool>(
            "borderOnForeground",
            BorderOnForeground,
            defaultValue: true));
        properties.Add(new DiagnosticsProperty<BorderRadiusGeometry?>(
            "borderRadius",
            BorderRadius,
            defaultValue: nullDefault));
    }

    // Dart's `_MaterialState`.
    private sealed class MaterialState : State<Material>
    {
        private readonly GlobalKey _inkFeatureRenderer = new LabeledGlobalKey<State>("ink renderer");

        public override Widget Build(BuildContext context)
        {
            ThemeData theme = Theme.Of(context);
            Color? backgroundColor = Widget.Color
                ?? Widget.Type switch
                {
                    MaterialType.Canvas => theme.CanvasColor,
                    MaterialType.Card => theme.CardColor,
                    _ => null,
                };
            Color modelShadowColor = Widget.ShadowColor
                ?? (theme.UseMaterial3 ? theme.ColorScheme.Shadow : theme.ShadowColor);
            DebugAssertions.Assert(
                backgroundColor is not null || Widget.Type == MaterialType.Transparency,
                "If Material type is not MaterialType.transparency, a color must "
                + "either be passed in through the `color` property, or be defined "
                + "in the theme (ex. canvasColor != null if type is set to "
                + "MaterialType.canvas)");

            Widget? contents = Widget.Child;
            if (contents is not null)
            {
                contents = new AnimatedDefaultTextStyle(
                    style: Widget.TextStyle ?? Theme.Of(context).TextTheme.BodyMedium,
                    duration: Widget.AnimationDuration,
                    child: contents);
            }

            contents = new NotificationListener<LayoutChangedNotification>(
                onNotification: _ =>
                {
                    var renderer = (RenderInkFeatures)_inkFeatureRenderer.CurrentContext!.FindRenderObject()!;
                    renderer.DidChangeLayout();
                    return false;
                },
                child: new InkFeatures(
                    key: _inkFeatureRenderer,
                    absorbHitTest: Widget.Type != MaterialType.Transparency,
                    color: backgroundColor,
                    vsync: this,
                    child: contents));

            ShapeBorder? shape = Widget.BorderRadius is { } borderRadius
                ? new RoundedRectangleBorder(borderRadius: borderRadius)
                : Widget.Shape;

            // PhysicalModel has a temporary workaround for a performance issue that
            // speeds up rectangular non transparent material (the workaround is to
            // skip the call to ui.Canvas.saveLayer if the border radius is 0).
            // Until the saveLayer performance issue is resolved, we're keeping this
            // special case here for canvas material type that is using the default
            // shape (rectangle). We could go down this fast path for explicitly
            // specified rectangles (e.g shape RoundedRectangleBorder with radius 0, but
            // we choose not to as we want the change from the fast-path to the
            // slow-path to be noticeable in the construction site of Material.
            if (Widget.Type == MaterialType.Canvas && shape is null)
            {
                Color color = theme.UseMaterial3
                    ? ElevationOverlay.ApplySurfaceTint(
                        backgroundColor!,
                        Widget.SurfaceTintColor,
                        Widget.Elevation)
                    : ElevationOverlay.ApplyOverlay(context, backgroundColor!, Widget.Elevation);

                return new AnimatedPhysicalModel(
                    curve: Curves.FastOutSlowIn,
                    duration: Widget.AnimationDuration,
                    clipBehavior: Widget.ClipBehavior,
                    elevation: Widget.Elevation,
                    color: color,
                    shadowColor: modelShadowColor,
                    animateColor: Widget.AnimateColor,
                    child: contents);
            }

            shape ??= Widget.Type switch
            {
                MaterialType.Circle => new CircleBorder(),
                MaterialType.Canvas or MaterialType.Transparency => new RoundedRectangleBorder(),
                _ => new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(2.0)),
            };

            if (Widget.Type == MaterialType.Transparency)
            {
                return new ClipPath(
                    clipper: new ShapeBorderClipper(shape, Directionality.MaybeOf(context)),
                    clipBehavior: Widget.ClipBehavior,
                    child: new ShapeBorderPaint(shape: shape, child: contents));
            }

            return new MaterialInterior(
                curve: Curves.FastOutSlowIn,
                duration: Widget.AnimationDuration,
                shape: shape,
                borderOnForeground: Widget.BorderOnForeground,
                clipBehavior: Widget.ClipBehavior,
                elevation: Widget.Elevation,
                color: backgroundColor!,
                shadowColor: modelShadowColor,
                surfaceTintColor: Widget.SurfaceTintColor,
                child: contents);
        }
    }
}

// Dart's `_RenderInkFeatures`.
internal sealed class RenderInkFeatures : RenderProxyBox, MaterialInkController
{
    private List<InkFeature>? _inkFeatures;
    private Dictionary<IMaterialInkFeature, LegacyInkFeature>? _legacyInkFeatures;

    public RenderInkFeatures(
        ITickerProvider vsync,
        bool absorbHitTest,
        Color? color = null,
        RenderBox? child = null) : base(child)
    {
        Vsync = vsync;
        AbsorbHitTest = absorbHitTest;
        Color = color;
    }

    // This class should exist in a 1:1 relationship with a MaterialState object,
    // since there's no current support for dynamically changing the ticker
    // provider.
    public ITickerProvider Vsync { get; }

    // This is here to satisfy the MaterialInkController contract.
    // The actual painting of this color is done by the physical model in the
    // MaterialState build method.
    public Color? Color { get; set; }

    public bool AbsorbHitTest { get; set; }

    /// <summary>Dart's <c>@visibleForTesting debugInkFeatures</c>.</summary>
    public List<InkFeature>? DebugInkFeatures => Constants.KDebugMode ? _inkFeatures : null;

    /// <summary>
    /// C#-only: the painting context of the paint in progress, for the render-object ink effects that
    /// have not been ported to <see cref="InkFeature"/> yet (<see cref="LegacyInkFeature"/>).
    /// </summary>
    internal PaintingContext? LegacyPaintingContext { get; private set; }

    public void AddInkFeature(InkFeature feature)
    {
        DebugAssertions.Assert(!feature.DebugDisposed);
        DebugAssertions.Assert(ReferenceEquals(feature.InternalController, this));
        _inkFeatures ??= [];
        DebugAssertions.Assert(!_inkFeatures.Contains(feature));
        _inkFeatures.Add(feature);
        MarkNeedsPaint();
    }

    internal void RemoveFeature(InkFeature feature)
    {
        DebugAssertions.Assert(_inkFeatures is not null);
        _inkFeatures!.Remove(feature);
        MarkNeedsPaint();
    }

    internal void DidChangeLayout()
    {
        if (_inkFeatures is { Count: > 0 })
        {
            MarkNeedsPaint();
        }
    }

    protected override bool HitTestSelf(Point position) => AbsorbHitTest;

    public override void Paint(PaintingContext context, Point offset)
    {
        List<InkFeature>? inkFeatures = _inkFeatures;
        if (inkFeatures is { Count: > 0 })
        {
            Canvas canvas = context.Canvas;
            canvas.Save();
            canvas.Translate(offset.X, offset.Y);
            canvas.ClipRect(new Rect(new Point(0, 0), Size));
            LegacyPaintingContext = context;
            foreach (InkFeature inkFeature in inkFeatures)
            {
                inkFeature.PaintInternal(canvas);
            }

            LegacyPaintingContext = null;
            canvas.Restore();
        }

        DebugAssertions.Assert(ReferenceEquals(inkFeatures, _inkFeatures));
        base.Paint(context, offset);
    }

    // C#-only bridge for the render-object ink effects (ink_well.dart / ink_decoration.dart are not
    // strict ports yet): each one is registered once, wrapped in a LegacyInkFeature, and removed by
    // disposing that wrapper, so it paints through the same ordered list as a real InkFeature.
    internal void AddLegacyInkFeature(IMaterialInkFeature feature)
    {
        _legacyInkFeatures ??= [];
        if (_legacyInkFeatures.ContainsKey(feature))
        {
            return;
        }

        var inkFeature = new LegacyInkFeature(this, feature);
        _legacyInkFeatures.Add(feature, inkFeature);
        AddInkFeature(inkFeature);
    }

    internal void RemoveLegacyInkFeature(IMaterialInkFeature feature)
    {
        if (_legacyInkFeatures is null || !_legacyInkFeatures.Remove(feature, out LegacyInkFeature? inkFeature))
        {
            return;
        }

        inkFeature.Dispose();
    }
}

// Dart's `_InkFeatures`.
internal sealed class InkFeatures : SingleChildRenderObjectWidget
{
    public InkFeatures(
        ITickerProvider vsync,
        bool absorbHitTest,
        Color? color = null,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Color = color;
        Vsync = vsync;
        AbsorbHitTest = absorbHitTest;
    }

    // This widget must be owned by a MaterialState, which must be provided as the vsync.
    // This relationship must be 1:1 and cannot change for the lifetime of the MaterialState.

    public Color? Color { get; }

    public ITickerProvider Vsync { get; }

    public bool AbsorbHitTest { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInkFeatures(color: Color, absorbHitTest: AbsorbHitTest, vsync: Vsync);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var inkFeatures = (RenderInkFeatures)renderObject;
        inkFeatures.Color = Color;
        inkFeatures.AbsorbHitTest = AbsorbHitTest;
        DebugAssertions.Assert(ReferenceEquals(Vsync, inkFeatures.Vsync));
    }
}

/// <summary>
/// A visual reaction on a piece of <see cref="Material"/>. To add an ink feature to a piece of
/// <see cref="Material"/>, obtain the <see cref="MaterialInkController"/> via <see cref="Material.Of"/>
/// and call <see cref="MaterialInkController.AddInkFeature"/>.
/// </summary>
public abstract class InkFeature : IDisposable
{
    private readonly RenderInkFeatures _controller;
    private bool _debugDisposed;

    /// <summary>Initializes fields for subclasses.</summary>
    protected InkFeature(
        MaterialInkController controller,
        RenderBox referenceBox,
        Action? onRemoved = null)
    {
        _controller = (RenderInkFeatures)controller;
        ReferenceBox = referenceBox;
        OnRemoved = onRemoved;
        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchCreated("material", "InkFeature", this));
    }

    /// <summary>The <see cref="MaterialInkController"/> associated with this <see cref="InkFeature"/>.</summary>
    public MaterialInkController Controller => _controller;

    internal RenderInkFeatures InternalController => _controller;

    /// <summary>The render box whose visual position defines the frame of reference for this ink feature.</summary>
    public RenderBox ReferenceBox { get; }

    /// <summary>Called when the ink feature is no longer visible on the material.</summary>
    public Action? OnRemoved { get; }

    internal bool DebugDisposed => _debugDisposed;

    /// <summary>Free up the resources associated with this ink feature. Overrides must call the base.</summary>
    public virtual void Dispose()
    {
        DebugAssertions.Assert(!_debugDisposed);
        if (Constants.KDebugMode)
        {
            _debugDisposed = true;
        }

        DebugAssertions.Assert(FoundationDebug.DebugMaybeDispatchDisposed(this));
        _controller.RemoveFeature(this);
        OnRemoved?.Invoke();
        GC.SuppressFinalize(this);
    }

    // Returns the paint transform that allows `fromRenderObject` to perform paint
    // in `toRenderObject`'s coordinate space.
    //
    // Returns null if either `fromRenderObject` or `toRenderObject` is not in the
    // same render tree, or either of them is in an offscreen subtree (see
    // RenderObject.paintsChild).
    private static Matrix4? GetPaintTransform(RenderObject fromRenderObject, RenderObject toRenderObject)
    {
        // The paths to fromRenderObject and toRenderObject's common ancestor.
        var fromPath = new List<RenderObject> { fromRenderObject };
        var toPath = new List<RenderObject> { toRenderObject };

        RenderObject from = fromRenderObject;
        RenderObject to = toRenderObject;

        while (!ReferenceEquals(from, to))
        {
            int fromDepth = from.Depth;
            int toDepth = to.Depth;

            if (fromDepth >= toDepth)
            {
                RenderObject? fromParent = from.Parent;
                // Return early if the 2 render objects are not in the same render tree,
                // or either of them is offscreen and thus won't get painted.
                if (fromParent is null || !fromParent.PaintsChild(from))
                {
                    return null;
                }

                fromPath.Add(fromParent);
                from = fromParent;
            }

            if (fromDepth <= toDepth)
            {
                RenderObject? toParent = to.Parent;
                if (toParent is null || !toParent.PaintsChild(to))
                {
                    return null;
                }

                toPath.Add(toParent);
                to = toParent;
            }
        }

        DebugAssertions.Assert(ReferenceEquals(from, to));

        var transform = Matrix4.Identity();
        var inverseTransform = Matrix4.Identity();

        for (int index = toPath.Count - 1; index > 0; index -= 1)
        {
            toPath[index].ApplyPaintTransform(toPath[index - 1], transform);
        }

        for (int index = fromPath.Count - 1; index > 0; index -= 1)
        {
            fromPath[index].ApplyPaintTransform(fromPath[index - 1], inverseTransform);
        }

        double det = inverseTransform.Invert();
        if (det == 0)
        {
            return null;
        }

        inverseTransform.Multiply(transform);
        return inverseTransform;
    }

    // Dart's `_paint`.
    internal void PaintInternal(Canvas canvas)
    {
        DebugAssertions.Assert(ReferenceBox.Attached);
        DebugAssertions.Assert(!_debugDisposed);
        // determine the transform that gets our coordinate system to be like theirs
        Matrix4? transform = GetPaintTransform(_controller, ReferenceBox);
        if (transform is not null)
        {
            PaintFeature(canvas, transform);
        }
    }

    /// <summary>
    /// Override this method to paint the ink feature. The transform argument gives the coordinate
    /// conversion from the coordinate system of the canvas to the coordinate system of the
    /// <see cref="ReferenceBox"/>.
    /// </summary>
    protected abstract void PaintFeature(Canvas canvas, Matrix4 transform);

    /// <inheritdoc />
    public override string ToString() => Diagnostics.DescribeIdentity(this);
}

/// <summary>
/// An interpolation between two <see cref="ShapeBorder"/>s: specializes <see cref="Tween{T}"/> to
/// use <see cref="ShapeBorder.Lerp"/>.
/// </summary>
public sealed class ShapeBorderTween : Tween<ShapeBorder?>
{
    /// <summary>
    /// Creates a <see cref="ShapeBorder"/> tween. The begin and end properties may be null; see
    /// <see cref="ShapeBorder.Lerp"/> for the null handling semantics.
    /// </summary>
    public ShapeBorderTween(ShapeBorder? begin = null, ShapeBorder? end = null)
    {
        Begin = begin;
        End = end;
    }

    /// <summary>Returns the value this tween has at the given animation clock value.</summary>
    public override ShapeBorder? Lerp(ShapeBorder? a, ShapeBorder? b, double t)
    {
        return ShapeBorder.Lerp(a, b, t);
    }

    /// <inheritdoc />
    /// <remarks>Dart's <c>Tween.transform</c>: exactly <c>begin</c> at 0, <c>end</c> at 1, else the lerp.</remarks>
    public override ShapeBorder? Evaluate(double t)
    {
        if (t == 0.0)
        {
            return Begin;
        }

        if (t == 1.0)
        {
            return End;
        }

        return Lerp(Begin, End, t);
    }
}

// Dart's `_MaterialInterior`: the interior of non-transparent material. Animates elevation,
// shadowColor, and shape.
internal sealed class MaterialInterior : ImplicitlyAnimatedWidget
{
    public MaterialInterior(
        Widget child,
        ShapeBorder shape,
        double elevation,
        Color color,
        Color shadowColor,
        Color? surfaceTintColor,
        TimeSpan duration,
        bool borderOnForeground = true,
        Clip clipBehavior = Clip.None,
        Curve? curve = null) : base(duration, curve)
    {
        DebugAssertions.Assert(elevation >= 0.0);
        Child = child;
        Shape = shape;
        BorderOnForeground = borderOnForeground;
        ClipBehavior = clipBehavior;
        Elevation = elevation;
        Color = color;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    /// <summary>
    /// The border of the widget. This border will be painted, and in addition the outer path of the
    /// border determines the physical shape.
    /// </summary>
    public ShapeBorder Shape { get; }

    /// <summary>Whether to paint the border in front of the child.</summary>
    public bool BorderOnForeground { get; }

    /// <summary>Defaults to <see cref="Clip.None"/>.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>The target z-coordinate at which to place this physical object relative to its parent.</summary>
    public double Elevation { get; }

    /// <summary>The target background color.</summary>
    public Color Color { get; }

    /// <summary>The target shadow color.</summary>
    public Color ShadowColor { get; }

    /// <summary>The target surface tint color.</summary>
    public Color? SurfaceTintColor { get; }

    public override State CreateState() => new MaterialInteriorState();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder description)
    {
        base.DebugFillProperties(description);
        description.Add(new DiagnosticsProperty<ShapeBorder>("shape", Shape));
        description.Add(new DoubleProperty("elevation", Elevation));
        description.Add(new ColorProperty("color", Color));
        description.Add(new ColorProperty("shadowColor", ShadowColor));
    }
}

// Dart's `_MaterialInteriorState`.
internal sealed class MaterialInteriorState : AnimatedWidgetBaseState<MaterialInterior>
{
    private Tween<double>? _elevation;
    private ColorTween? _surfaceTintColor;
    private ColorTween? _shadowColor;
    private ShapeBorderTween? _border;

    protected override void ForEachTween(TweenVisitor visitor)
    {
        _elevation = visitor.Visit(
            _elevation,
            Widget.Elevation,
            value => new DoubleTween(begin: value));
        _shadowColor = visitor.Visit<Color>(
            _shadowColor,
            Widget.ShadowColor,
            value => new ColorTween(begin: value)) as ColorTween;
        _surfaceTintColor = Widget.SurfaceTintColor is not null
            ? visitor.Visit<Color>(
                _surfaceTintColor,
                Widget.SurfaceTintColor,
                value => new ColorTween(begin: value)) as ColorTween
            : null;
        _border = visitor.Visit<ShapeBorder?>(
            _border,
            Widget.Shape,
            value => new ShapeBorderTween(begin: value)) as ShapeBorderTween;
    }

    public override Widget Build(BuildContext context)
    {
        ShapeBorder shape = _border!.Evaluate(Animation)!;
        double elevation = _elevation!.Evaluate(Animation);
        Color color = Theme.Of(context).UseMaterial3
            ? ElevationOverlay.ApplySurfaceTint(
                Widget.Color,
                _surfaceTintColor?.Evaluate(Animation),
                elevation)
            : ElevationOverlay.ApplyOverlay(context, Widget.Color, elevation);
        Color shadowColor = _shadowColor!.Evaluate(Animation)!;

        return new PhysicalShape(
            clipper: new ShapeBorderClipper(shape, Directionality.MaybeOf(context)),
            clipBehavior: Widget.ClipBehavior,
            elevation: elevation,
            color: color,
            shadowColor: shadowColor,
            child: new ShapeBorderPaint(
                shape: shape,
                borderOnForeground: Widget.BorderOnForeground,
                child: Widget.Child));
    }
}

// Dart's `_ShapeBorderPaint`.
internal sealed class ShapeBorderPaint : StatelessWidget
{
    public ShapeBorderPaint(Widget child, ShapeBorder shape, bool borderOnForeground = true)
    {
        Child = child;
        Shape = shape;
        BorderOnForeground = borderOnForeground;
    }

    public Widget Child { get; }

    public ShapeBorder Shape { get; }

    public bool BorderOnForeground { get; }

    public override Widget Build(BuildContext context)
    {
        return new CustomPaint(
            painter: BorderOnForeground
                ? null
                : new ShapeBorderPainter(Shape, Directionality.MaybeOf(context)),
            foregroundPainter: BorderOnForeground
                ? new ShapeBorderPainter(Shape, Directionality.MaybeOf(context))
                : null,
            child: Child);
    }
}

// Dart's `_ShapeBorderPainter`.
internal sealed class ShapeBorderPainter(ShapeBorder border, TextDirection? textDirection) : CustomPainter
{
    public ShapeBorder Border { get; } = border;

    public TextDirection? TextDirection { get; } = textDirection;

    public override void Paint(PaintingContext context, Size size)
    {
        Border.Paint(context, new Rect(new Point(0, 0), size), textDirection: TextDirection);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return ((ShapeBorderPainter)oldDelegate).Border != Border;
    }
}

// C#-only: the render-object ink effects of ink_well.dart / ink_decoration.dart, which are not strict
// ports of InkFeature subclasses yet, register through this interface. See docs/ai/BACKLOG.md.
internal interface IMaterialInkFeature
{
    RenderBox ReferenceBox { get; }

    void PaintFeature(PaintingContext context);
}

// C#-only: the `AddInkFeature`/`RemoveInkFeature` spelling those render objects use.
internal static class MaterialInkControllerLegacyExtensions
{
    public static void AddInkFeature(this MaterialInkController controller, IMaterialInkFeature feature)
    {
        ((RenderInkFeatures)controller).AddLegacyInkFeature(feature);
    }

    public static void RemoveInkFeature(this MaterialInkController controller, IMaterialInkFeature feature)
    {
        ((RenderInkFeatures)controller).RemoveLegacyInkFeature(feature);
    }
}

// C#-only: wraps an IMaterialInkFeature as an InkFeature. Dart's InkSplash/InkHighlight apply the
// transform themselves (`canvas.save(); canvas.transform(...)`); this does the same around the
// wrapped render object's paint.
internal sealed class LegacyInkFeature(RenderInkFeatures controller, IMaterialInkFeature feature)
    : InkFeature(controller, feature.ReferenceBox)
{
    public IMaterialInkFeature Feature { get; } = feature;

    protected override void PaintFeature(Canvas canvas, Matrix4 transform)
    {
        PaintingContext context = InternalController.LegacyPaintingContext!;
        canvas.Save();
        canvas.Transform(transform);
        Feature.PaintFeature(context);
        canvas.Restore();
    }
}
