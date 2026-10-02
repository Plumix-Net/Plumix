using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Painting;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only infrastructure: the engine side of dart:ui's `SceneBuilder`. A `Scene` holds a tree of these
// retained layers — the counterpart of the engine's `flow/layers` (`ContainerLayer`, `TransformLayer`,
// `ClipRectLayer`, `OpacityLayer`, `DisplayListLayer`, ...) — and `SceneRasterizer` paints that tree
// into an Avalonia drawing context, the way the engine's rasterizer paints a `LayerTree` into a surface.
// Filters, shader masks and backdrops have no native Avalonia layer, so they go through the CPU raster
// backend (`FilterLayerRasterizer`); see docs/ai/DIVERGENCES.md. No Dart source maps to this file.

/// <summary>A layer of the engine's retained layer tree.</summary>
/// <remarks>The engine's <c>flutter::Layer</c>.</remarks>
internal abstract class FlowLayer
{
    /// <summary>Paints this layer: the engine's <c>Layer::Paint</c>.</summary>
    public abstract void Paint(FlowPaintContext context);

    /// <summary>Adds this subtree's backdrop filters to <paramref name="filters"/>, in paint order.</summary>
    internal virtual void CollectBackdropFilters(List<BackdropFilterFlowLayer> filters)
    {
    }

    /// <summary>Whether this subtree contains a magnifier lens.</summary>
    internal virtual bool ContainsMagnifier => false;
}

/// <summary>A layer with child layers: the engine's <c>flutter::ContainerLayer</c>.</summary>
internal class ContainerFlowLayer : FlowLayer
{
    private readonly List<FlowLayer> _layers = [];

    /// <summary>The children, in paint order.</summary>
    public IReadOnlyList<FlowLayer> Layers => _layers;

    /// <summary>Appends <paramref name="layer"/>: the engine's <c>ContainerLayer::Add</c>.</summary>
    public void Add(FlowLayer layer)
    {
        _layers.Add(layer);
    }

    public override void Paint(FlowPaintContext context)
    {
        PaintChildren(context);
    }

    /// <summary>Paints every child in order: the engine's <c>ContainerLayer::PaintChildren</c>.</summary>
    protected void PaintChildren(FlowPaintContext context)
    {
        foreach (FlowLayer layer in _layers)
        {
            if (context.State.Stopped)
            {
                return;
            }

            layer.Paint(context);
        }
    }

    /// <summary>Paints every child under <paramref name="transform"/>, tracking it in the context.</summary>
    protected void PaintChildrenTransformed(FlowPaintContext context, Matrix transform)
    {
        Matrix previous = context.Transform;
        using (context.Canvas.PushTransform(transform))
        {
            context.Transform = transform * previous;
            try
            {
                PaintChildren(context);
            }
            finally
            {
                context.Transform = previous;
            }
        }
    }

    internal override void CollectBackdropFilters(List<BackdropFilterFlowLayer> filters)
    {
        foreach (FlowLayer layer in _layers)
        {
            layer.CollectBackdropFilters(filters);
        }
    }

    internal override bool ContainsMagnifier => _layers.Any(static layer => layer.ContainsMagnifier);
}

/// <summary>
/// The engine's <c>flutter::TransformLayer</c> (what <c>pushTransform</c> and <c>pushOffset</c> add).
/// </summary>
internal sealed class TransformFlowLayer(Matrix4 transform) : ContainerFlowLayer
{
    public Matrix4 Transform { get; } = transform;

    public override void Paint(FlowPaintContext context)
    {
        PaintChildrenTransformed(context, Transform.ToAvaloniaMatrix());
    }
}

/// <summary>The engine's <c>flutter::OpacityLayer</c>: children at an offset, with an alpha.</summary>
internal sealed class OpacityFlowLayer(int alpha, Point offset) : ContainerFlowLayer
{
    public int Alpha { get; } = alpha;

    public Point Offset { get; } = offset;

    public override void Paint(FlowPaintContext context)
    {
        Matrix previous = context.Transform;
        Matrix translation = Matrix.CreateTranslation(Offset.X, Offset.Y);
        using (context.Canvas.PushTransform(translation))
        {
            context.Transform = translation * previous;
            try
            {
                if (Alpha >= 255)
                {
                    PaintChildren(context);
                    return;
                }

                using (context.Canvas.PushOpacity(Math.Clamp(Alpha, 0, 255) / 255.0))
                {
                    PaintChildren(context);
                }
            }
            finally
            {
                context.Transform = previous;
            }
        }
    }
}

/// <summary>The engine's <c>flutter::ClipRectLayer</c>.</summary>
internal sealed class ClipRectFlowLayer(Rect clipRect, Clip clipBehavior) : ContainerFlowLayer
{
    public Rect ClipRect { get; } = clipRect;

    public Clip ClipBehavior { get; } = clipBehavior;

    public override void Paint(FlowPaintContext context)
    {
        using IDisposable renderOptions = context.Canvas.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.Canvas.PushClip(ClipRect))
        {
            PaintChildren(context);
        }
    }
}

/// <summary>The engine's <c>flutter::ClipRRectLayer</c>.</summary>
internal sealed class ClipRRectFlowLayer(RRect clipRRect, Clip clipBehavior) : ContainerFlowLayer
{
    public RRect ClipRRect { get; } = clipRRect;

    public Clip ClipBehavior { get; } = clipBehavior;

    public override void Paint(FlowPaintContext context)
    {
        using (SceneRasterizer.PushRoundedRectClip(context.Canvas, ClipRRect))
        {
            PaintChildren(context);
        }
    }
}

/// <summary>The engine's <c>flutter::ClipRSuperellipseLayer</c>.</summary>
internal sealed class ClipRSuperellipseFlowLayer(RSuperellipse clipRSuperellipse, Clip clipBehavior)
    : ContainerFlowLayer
{
    public RSuperellipse ClipRSuperellipse { get; } = clipRSuperellipse;

    public Clip ClipBehavior { get; } = clipBehavior;

    public override void Paint(FlowPaintContext context)
    {
        using IDisposable renderOptions = context.Canvas.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.Canvas.PushGeometryClip(ClipRSuperellipse.ToPath().ToGeometry()))
        {
            PaintChildren(context);
        }
    }
}

/// <summary>The engine's <c>flutter::ClipPathLayer</c>.</summary>
internal sealed class ClipPathFlowLayer(Path clipPath, Clip clipBehavior) : ContainerFlowLayer
{
    private Geometry? _geometry;

    public Path ClipPath { get; } = clipPath;

    public Clip ClipBehavior { get; } = clipBehavior;

    /// <summary>
    /// Reuses the geometry <paramref name="oldLayer"/> converted for the same path: the counterpart of the
    /// engine's <c>Layer::AssignOldLayer</c> resource reuse.
    /// </summary>
    internal void AssignOldLayer(ClipPathFlowLayer? oldLayer)
    {
        if (oldLayer != null && ReferenceEquals(oldLayer.ClipPath, ClipPath))
        {
            _geometry ??= oldLayer._geometry;
        }
    }

    public override void Paint(FlowPaintContext context)
    {
        _geometry ??= ClipPath.ToGeometry();
        using IDisposable renderOptions = context.Canvas.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.Canvas.PushGeometryClip(_geometry))
        {
            PaintChildren(context);
        }
    }
}

/// <summary>Plumix-only: a clip by a backend geometry (<see cref="Rendering.ClipGeometryLayer"/>).</summary>
internal sealed class ClipGeometryFlowLayer(Geometry geometry, Point geometryOffset, Clip clipBehavior)
    : ContainerFlowLayer
{
    public Geometry Geometry { get; } = geometry;

    public Point GeometryOffset { get; } = geometryOffset;

    public Clip ClipBehavior { get; } = clipBehavior;

    public override void Paint(FlowPaintContext context)
    {
        using IDisposable renderOptions = context.Canvas.PushRenderOptions(new RenderOptions
        {
            EdgeMode = ClipBehavior == Clip.HardEdge ? EdgeMode.Aliased : EdgeMode.Antialias,
        });
        using (context.Canvas.PushTransform(Matrix.CreateTranslation(GeometryOffset.X, GeometryOffset.Y)))
        using (context.Canvas.PushGeometryClip(Geometry))
        using (context.Canvas.PushTransform(Matrix.CreateTranslation(-GeometryOffset.X, -GeometryOffset.Y)))
        {
            PaintChildren(context);
        }
    }
}

/// <summary>The engine's <c>flutter::ColorFilterLayer</c>, over the CPU filter backend.</summary>
internal sealed class ColorFilterFlowLayer(ColorFilter filter, Rect? bounds) : ContainerFlowLayer
{
    public ColorFilter Filter { get; } = filter;

    /// <summary>The Plumix-only rasterization bounds, in this layer's coordinates.</summary>
    public Rect? Bounds { get; } = bounds;

    public override void Paint(FlowPaintContext context)
    {
        if (Bounds is not { } bounds)
        {
            PaintChildren(context);
            return;
        }

        context.Track(FilterLayerRasterizer.DrawColorFiltered(
            context.Canvas,
            canvas => PaintChildren(context.WithCanvas(canvas, Matrix.Identity)),
            Filter,
            bounds));
    }
}

/// <summary>The engine's <c>flutter::ImageFilterLayer</c>, over the CPU filter backend.</summary>
internal sealed class ImageFilterFlowLayer(ImageFilter filter, Point offset, Rect? bounds) : ContainerFlowLayer
{
    public ImageFilter Filter { get; } = filter;

    public Point Offset { get; } = offset;

    /// <summary>The Plumix-only rasterization bounds, in the children's (offset) coordinates.</summary>
    public Rect? Bounds { get; } = bounds;

    public override void Paint(FlowPaintContext context)
    {
        if (Bounds is not { } bounds)
        {
            PaintChildrenTransformed(context, Matrix.CreateTranslation(Offset.X, Offset.Y));
            return;
        }

        context.Track(FilterLayerRasterizer.DrawImageFiltered(
            context.Canvas,
            canvas => PaintChildren(context.WithCanvas(canvas, Matrix.Identity)),
            Filter,
            Offset,
            bounds));
    }
}

/// <summary>The engine's <c>flutter::ShaderMaskLayer</c>, over the CPU filter backend.</summary>
internal sealed class ShaderMaskFlowLayer(IBrush shader, Rect maskRect, BlendMode blendMode, FilterQuality quality)
    : ContainerFlowLayer
{
    public IBrush Shader { get; } = shader;

    public Rect MaskRect { get; } = maskRect;

    public BlendMode BlendMode { get; } = blendMode;

    public FilterQuality FilterQuality { get; } = quality;

    public override void Paint(FlowPaintContext context)
    {
        context.Track(FilterLayerRasterizer.DrawShaderMasked(
            context.Canvas,
            canvas => PaintChildren(context.WithCanvas(canvas, Matrix.Identity)),
            Shader,
            BlendMode,
            MaskRect));
    }
}

/// <summary>
/// The engine's <c>flutter::BackdropFilterLayer</c>: filters what was painted before it, then paints its
/// children on top.
/// </summary>
/// <remarks>
/// The backdrop is a capture of the scene prefix painted before this layer (<see cref="SceneRasterizer"/>);
/// layers sharing a backdrop id share one capture, the way the engine shares a backdrop-id snapshot.
/// </remarks>
internal sealed class BackdropFilterFlowLayer(ImageFilter filter, BlendMode blendMode, int? backdropId)
    : ContainerFlowLayer
{
    public ImageFilter Filter { get; } = filter;

    public BlendMode BlendMode { get; } = blendMode;

    public int? BackdropId { get; } = backdropId;

    public override void Paint(FlowPaintContext context)
    {
        FlowRasterState state = context.State;
        if (ReferenceEquals(state.StopAt, this))
        {
            state.Stopped = true;
            return;
        }

        if (state.Backdrops.TryGetValue(this, out BackdropCapture? backdrop)
            && context.Transform.TryInvert(out Matrix toBase))
        {
            // The capture is in the target's coordinates; draw it there, under the clips in effect.
            using (context.Canvas.PushTransform(toBase))
            {
                context.Track(FilterLayerRasterizer.DrawBackdropFiltered(
                    context.Canvas,
                    backdrop.Image,
                    backdrop.Bounds,
                    Filter,
                    BlendMode));
            }
        }

        PaintChildren(context);
    }

    internal override void CollectBackdropFilters(List<BackdropFilterFlowLayer> filters)
    {
        filters.Add(this);
        base.CollectBackdropFilters(filters);
    }
}

/// <summary>
/// Plumix-only: the lens of a <see cref="Rendering.MagnifierLayer"/>, which draws a magnified copy of the
/// scene captured without any lens (docs/ai/DIVERGENCES.md).
/// </summary>
internal sealed class MagnifierFlowLayer(
    Rect lensRect,
    Point focalPointOffset,
    double magnificationScale,
    MagnifierDecoration decoration,
    Clip clipBehavior) : ContainerFlowLayer
{
    public Rect LensRect { get; } = lensRect;

    public Point FocalPointOffset { get; } = focalPointOffset;

    public double MagnificationScale { get; } = magnificationScale;

    public MagnifierDecoration Decoration { get; } = decoration;

    public Clip ClipBehavior { get; } = clipBehavior;

    internal override bool ContainsMagnifier => true;

    public override void Paint(FlowPaintContext context)
    {
        FlowRasterState state = context.State;
        if (state.CapturingMagnifier || state.StopAt != null)
        {
            return;
        }

        Matrix toTarget = context.Transform;
        if (!toTarget.TryInvert(out Matrix toBase))
        {
            return;
        }

        Rect lensRect = LensRect.TransformToAABB(toTarget);
        if (lensRect.Width <= 0 || lensRect.Height <= 0)
        {
            return;
        }

        var focalPointOffset = new Point(
            (FocalPointOffset.X * toTarget.M11) + (FocalPointOffset.Y * toTarget.M21),
            (FocalPointOffset.X * toTarget.M12) + (FocalPointOffset.Y * toTarget.M22));
        BorderRadius borderRadius = ResolveBorderRadius(lensRect);
        using (context.Canvas.PushTransform(toBase))
        using (context.Canvas.PushOpacity(Math.Clamp(Decoration.Opacity, 0.0, 1.0)))
        {
            using (SceneRasterizer.PushRoundedRectClip(context.Canvas, lensRect, borderRadius))
            {
                DrawMagnifiedBackdrop(context.Canvas, state.MagnifierBackdrop, lensRect, focalPointOffset);
                using (context.Canvas.PushTransform(toTarget))
                {
                    PaintChildren(context);
                }
            }

            DrawDecoration(context.Canvas, lensRect, borderRadius);
        }
    }

    /// <summary>
    /// Resolves the decoration shape to the per-corner radii the lens is clipped and stroked with.
    /// Each corner keeps its own (possibly elliptical) radius, clamped to half the lens so that
    /// neighbouring corners cannot overlap.
    /// </summary>
    private BorderRadius ResolveBorderRadius(Rect lensRect)
    {
        double maxX = lensRect.Width / 2.0;
        double maxY = lensRect.Height / 2.0;
        switch (Decoration.Shape)
        {
            case CircleBorder or StadiumBorder:
                return BorderRadius.Circular(Math.Min(maxX, maxY));
            case RoundedRectangleBorder rounded:
                BorderRadius resolved = rounded.BorderRadius.Resolve(TextDirection.Ltr);
                return new BorderRadius(
                    Layer.ClampRadius(resolved.TopLeftRadius, maxX, maxY),
                    Layer.ClampRadius(resolved.TopRightRadius, maxX, maxY),
                    Layer.ClampRadius(resolved.BottomRightRadius, maxX, maxY),
                    Layer.ClampRadius(resolved.BottomLeftRadius, maxX, maxY));
            default:
                return BorderRadius.Zero;
        }
    }

    private void DrawMagnifiedBackdrop(
        DrawingContext context,
        BackdropCapture? backdrop,
        Rect lensRect,
        Point focalPointOffset)
    {
        if (backdrop == null)
        {
            return;
        }

        double scale = MagnificationScale;
        double absoluteScale = Math.Abs(scale);
        if (absoluteScale <= double.Epsilon)
        {
            return;
        }

        Point focalPoint = lensRect.Center + focalPointOffset;
        var sourceSize = new Size(lensRect.Width / absoluteScale, lensRect.Height / absoluteScale);
        var sourceRect = new Rect(
            focalPoint.X - (sourceSize.Width / 2.0),
            focalPoint.Y - (sourceSize.Height / 2.0),
            sourceSize.Width,
            sourceSize.Height);
        if (scale > 0)
        {
            context.DrawImage(backdrop.Image, sourceRect, lensRect);
            return;
        }

        using (context.PushTransform(
                   Matrix.CreateTranslation(lensRect.Center.X, lensRect.Center.Y)
                   * Matrix.CreateScale(-1, -1)
                   * Matrix.CreateTranslation(-lensRect.Center.X, -lensRect.Center.Y)))
        {
            context.DrawImage(backdrop.Image, sourceRect, lensRect);
        }
    }

    private void DrawDecoration(DrawingContext context, Rect lensRect, BorderRadius borderRadius)
    {
        BoxShadows shadows = Decoration.Shadows.ToAvalonia();
        BorderSide side = Decoration.Shape is OutlinedBorder outlined ? outlined.Side : BorderSide.None;
        IPen? pen = side is { Style: BorderStyle.Solid, Width: > 0 }
            ? new Pen(new SolidColorBrush(side.Color), side.Width)
            : null;

        if (shadows.Count == 0 && pen == null)
        {
            return;
        }

        DrawingContext.PushedState? clip = null;
        try
        {
            if (ClipBehavior != Clip.None)
            {
                double inset = pen?.Thickness ?? 0.0;
                var outer = lensRect.Inflate(Math.Max(lensRect.Width, lensRect.Height));
                var geometry = new CombinedGeometry(
                    GeometryCombineMode.Exclude,
                    new RectangleGeometry(outer),
                    new RectangleGeometry(
                        new Rect(
                            lensRect.X + inset,
                            lensRect.Y + inset,
                            Math.Max(0, lensRect.Width - (inset * 2)),
                            Math.Max(0, lensRect.Height - (inset * 2))),
                        Math.Max(0, LargestRadiusX(borderRadius) - inset),
                        Math.Max(0, LargestRadiusY(borderRadius) - inset)));
                clip = context.PushGeometryClip(geometry);
            }

            context.DrawRectangle(Brushes.Transparent, pen, ToRoundedRect(lensRect, borderRadius), shadows);
        }
        finally
        {
            clip?.Dispose();
        }
    }

    private static RoundedRect ToRoundedRect(Rect rect, BorderRadius borderRadius)
    {
        return new RoundedRect(
            rect,
            new Vector(borderRadius.TopLeftRadius.X, borderRadius.TopLeftRadius.Y),
            new Vector(borderRadius.TopRightRadius.X, borderRadius.TopRightRadius.Y),
            new Vector(borderRadius.BottomRightRadius.X, borderRadius.BottomRightRadius.Y),
            new Vector(borderRadius.BottomLeftRadius.X, borderRadius.BottomLeftRadius.Y));
    }

    private static double LargestRadiusX(BorderRadius borderRadius)
    {
        return Math.Max(
            Math.Max(borderRadius.TopLeftRadius.X, borderRadius.TopRightRadius.X),
            Math.Max(borderRadius.BottomRightRadius.X, borderRadius.BottomLeftRadius.X));
    }

    private static double LargestRadiusY(BorderRadius borderRadius)
    {
        return Math.Max(
            Math.Max(borderRadius.TopLeftRadius.Y, borderRadius.TopRightRadius.Y),
            Math.Max(borderRadius.BottomRightRadius.Y, borderRadius.BottomLeftRadius.Y));
    }
}

/// <summary>
/// The raster snapshot a <see cref="Rendering.SnapshotOffsetLayer"/> keeps between frames: its children
/// rasterized once at <see cref="PixelRatio"/>, redrawn from the image until the layer clears it.
/// </summary>
internal sealed class SnapshotRasterCache
{
    /// <summary>The cached image, null until the first rasterization (and after <see cref="Clear"/>).</summary>
    public RenderTargetBitmap? Image { get; set; }

    /// <summary>The logical size the children are drawn into.</summary>
    public Size Size { get; set; }

    /// <summary>The number of image pixels per logical pixel.</summary>
    public double PixelRatio { get; set; } = 1.0;

    /// <summary>Whether a failed rasterization falls back to drawing the children directly.</summary>
    public bool Permissive { get; set; }

    public void Clear()
    {
        Image?.Dispose();
        Image = null;
    }
}

/// <summary>Plumix-only: draws its children from a <see cref="SnapshotRasterCache"/>.</summary>
internal sealed class SnapshotFlowLayer(SnapshotRasterCache cache, Point offset) : ContainerFlowLayer
{
    public SnapshotRasterCache Cache { get; } = cache;

    public Point Offset { get; } = offset;

    public override void Paint(FlowPaintContext context)
    {
        try
        {
            Cache.Image ??= RasterizeChildren(context);
        }
        catch when (Cache.Permissive)
        {
            Cache.Clear();
            PaintChildrenTransformed(context, Matrix.CreateTranslation(Offset.X, Offset.Y));
            return;
        }

        RenderTargetBitmap snapshot = Cache.Image;
        var source = new Rect(0.0, 0.0, snapshot.PixelSize.Width, snapshot.PixelSize.Height);
        var destination = new Rect(Offset, Cache.Size);
        using (context.Canvas.PushRenderOptions(new RenderOptions
               {
                   BitmapInterpolationMode = BitmapInterpolationMode.MediumQuality,
               }))
        {
            context.Canvas.DrawImage(snapshot, source, destination);
        }
    }

    private RenderTargetBitmap RasterizeChildren(FlowPaintContext context)
    {
        int width = Math.Max(1, (int)Math.Ceiling(Cache.Size.Width * Cache.PixelRatio));
        int height = Math.Max(1, (int)Math.Ceiling(Cache.Size.Height * Cache.PixelRatio));
        var bitmap = new RenderTargetBitmap(
            new PixelSize(width, height),
            new Vector(96.0 * Cache.PixelRatio, 96.0 * Cache.PixelRatio));
        try
        {
            using DrawingContext drawingContext = bitmap.CreateDrawingContext();
            PaintChildren(context.WithCanvas(drawingContext, Matrix.Identity));
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}

/// <summary>The engine's <c>flutter::DisplayListLayer</c>: a recorded picture at an offset.</summary>
internal sealed class DisplayListFlowLayer(Point offset, Picture picture, bool isComplex, bool willChange)
    : FlowLayer
{
    public Point Offset { get; } = offset;

    public Picture Picture { get; } = picture;

    public bool IsComplex { get; } = isComplex;

    public bool WillChange { get; } = willChange;

    public override void Paint(FlowPaintContext context)
    {
        Picture.Playback(context.Canvas, Offset);
    }
}

/// <summary>The engine's <c>flutter::TextureLayer</c>: a registered backend texture.</summary>
internal sealed class TextureFlowLayer(Point offset, Size size, int textureId, bool freeze, FilterQuality quality)
    : FlowLayer
{
    public Point Offset { get; } = offset;

    public Size Size { get; } = size;

    public int TextureId { get; } = textureId;

    public bool Freeze { get; } = freeze;

    public FilterQuality FilterQuality { get; } = quality;

    public override void Paint(FlowPaintContext context)
    {
        EngineTexture? texture = context.State.Textures?.GetTexture(TextureId);
        if (texture == null)
        {
            // An unknown texture paints nothing: the engine's "null texture" trace.
            return;
        }

        texture.Paint(context.Canvas, new Rect(Offset, Size), Freeze, FilterQuality);
    }
}

/// <summary>
/// The engine's <c>flutter::PlatformViewLayer</c>. No Plumix host embeds platform views, so it paints
/// nothing (docs/ai/DIVERGENCES.md).
/// </summary>
internal sealed class PlatformViewFlowLayer(Point offset, Size size, int viewId) : FlowLayer
{
    public Point Offset { get; } = offset;

    public Size Size { get; } = size;

    public int ViewId { get; } = viewId;

    public override void Paint(FlowPaintContext context)
    {
    }
}

/// <summary>The engine's <c>flutter::PerformanceOverlayLayer</c>.</summary>
internal sealed class PerformanceOverlayFlowLayer(int options, Rect bounds) : FlowLayer
{
    /// <summary>The engine's <c>kDisplayRasterizerStatistics</c>.</summary>
    public const int DisplayRasterizerStatistics = 1 << 0;

    /// <summary>The engine's <c>kVisualizeRasterizerStatistics</c>.</summary>
    public const int VisualizeRasterizerStatistics = 1 << 1;

    /// <summary>The engine's <c>kDisplayEngineStatistics</c>.</summary>
    public const int DisplayEngineStatistics = 1 << 2;

    /// <summary>The engine's <c>kVisualizeEngineStatistics</c>.</summary>
    public const int VisualizeEngineStatistics = 1 << 3;

    public int Options { get; } = options;

    public Rect Bounds { get; } = bounds;

    /// <summary>
    /// The statistics line: the engine's <c>PerformanceOverlayLayer::MakeStatisticsText</c>.
    /// </summary>
    internal static string MakeStatisticsText(FrameStopwatch stopwatch, string labelPrefix)
    {
        double maxMillisecondsPerFrame = stopwatch.MaxDelta().TotalMilliseconds;
        double averageMillisecondsPerFrame = stopwatch.AverageDelta().TotalMilliseconds;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{labelPrefix}  max {maxMillisecondsPerFrame:F1} ms/frame, avg {averageMillisecondsPerFrame:F1} ms/frame");
    }

    public override void Paint(FlowPaintContext context)
    {
        const int padding = 8;
        if (Options == 0)
        {
            return;
        }

        CompositorContext compositor = context.State.Compositor;
        double x = Bounds.X + padding;
        double y = Bounds.Y + padding;
        double width = Bounds.Width - (padding * 2);
        double height = Bounds.Height / 2;
        VisualizeStopwatch(
            context.Canvas,
            compositor.RasterTime,
            x,
            y,
            width,
            height - padding,
            (Options & VisualizeRasterizerStatistics) != 0,
            (Options & DisplayRasterizerStatistics) != 0,
            "Raster");
        VisualizeStopwatch(
            context.Canvas,
            compositor.UiTime,
            x,
            y + height,
            width,
            height - padding,
            (Options & VisualizeEngineStatistics) != 0,
            (Options & DisplayEngineStatistics) != 0,
            "UI");
    }

    /// <remarks>The engine's file-local <c>VisualizeStopWatch</c>.</remarks>
    private static void VisualizeStopwatch(
        DrawingContext canvas,
        FrameStopwatch stopwatch,
        double x,
        double y,
        double width,
        double height,
        bool showGraph,
        bool showLabels,
        string labelPrefix)
    {
        const int labelX = 8; // distance from x
        const int labelY = -10; // distance from y+height
        if (showGraph && width > 0 && height > 0)
        {
            stopwatch.Visualize(canvas, new Rect(x, y, width, height));
        }

        if (!showLabels)
        {
            return;
        }

        FormattedText? text = SceneRasterizer.TryMakeStatisticsText(MakeStatisticsText(stopwatch, labelPrefix));
        if (text == null)
        {
            return;
        }

        // The engine draws the text with its baseline at (x + labelX, y + height + labelY).
        canvas.DrawText(text, new Point(x + labelX, y + height + labelY - text.Baseline));
    }
}

/// <summary>A backdrop input: an image of the scene prefix painted before a backdrop filter.</summary>
internal sealed class BackdropCapture : IDisposable
{
    private readonly bool _ownsImage;

    public BackdropCapture(IImage image, Rect bounds, bool ownsImage = false)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        Bounds = bounds;
        _ownsImage = ownsImage;
    }

    public IImage Image { get; }

    public Rect Bounds { get; }

    public void Dispose()
    {
        if (_ownsImage && Image is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>The state one rasterization pass shares across every drawing context it paints into.</summary>
internal sealed class FlowRasterState(Size targetSize, CompositorContext compositor, TextureRegistry? textures)
{
    /// <summary>The size of the surface, in the drawing context's own (target) units.</summary>
    public Size TargetSize { get; } = targetSize;

    public CompositorContext Compositor { get; } = compositor;

    public TextureRegistry? Textures { get; } = textures;

    /// <summary>The backdrop filter a prefix capture stops at; null for a full paint.</summary>
    public BackdropFilterFlowLayer? StopAt { get; init; }

    /// <summary>Whether the prefix capture reached <see cref="StopAt"/>.</summary>
    public bool Stopped { get; set; }

    /// <summary>Whether this pass captures the magnifier input, which paints no lens.</summary>
    public bool CapturingMagnifier { get; init; }

    /// <summary>The captured backdrop of every backdrop filter, by layer.</summary>
    public Dictionary<BackdropFilterFlowLayer, BackdropCapture> Backdrops { get; init; } =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>The captured scene a magnifier lens draws from.</summary>
    public BackdropCapture? MagnifierBackdrop { get; set; }

    /// <summary>The raster outputs the drawing context draws from, kept alive until the next frame.</summary>
    public List<IDisposable> Resources { get; init; } = [];
}

/// <summary>
/// One drawing context of a rasterization pass: the engine's <c>PaintContext</c> (its canvas and state
/// stack). <see cref="Transform"/> maps the current local coordinates to the target's.
/// </summary>
internal sealed class FlowPaintContext(DrawingContext canvas, Matrix transform, FlowRasterState state)
{
    public DrawingContext Canvas { get; } = canvas;

    public Matrix Transform { get; set; } = transform;

    public FlowRasterState State { get; } = state;

    /// <summary>A context drawing into <paramref name="canvas"/>, an offscreen of this one.</summary>
    public FlowPaintContext WithCanvas(DrawingContext canvas, Matrix transform) => new(canvas, transform, State);

    /// <summary>Keeps a raster output alive until the next frame of the same surface.</summary>
    public void Track(IDisposable? resource)
    {
        if (resource != null)
        {
            State.Resources.Add(resource);
        }
    }
}

/// <summary>Paints a scene's layer tree: the engine's rasterizer, over Avalonia's drawing context.</summary>
internal static class SceneRasterizer
{
    private static readonly Vector Dpi = new(96.0, 96.0);

    [ThreadStatic]
    private static int t_captureDepth;

    /// <summary>Whether the current thread is painting a backdrop or magnifier capture.</summary>
    internal static bool Capturing => t_captureDepth > 0;

    /// <summary>
    /// Paints <paramref name="root"/> into <paramref name="context"/> under <paramref name="rootTransform"/>,
    /// which maps the scene's (physical) pixels to the context's units.
    /// </summary>
    /// <returns>The raster outputs the context draws from; the caller disposes them after the next frame.</returns>
    public static List<IDisposable> Draw(
        DrawingContext context,
        ContainerFlowLayer root,
        Size targetSize,
        Matrix rootTransform,
        CompositorContext compositor,
        TextureRegistry? textures)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(root);
        compositor.RasterTime.Start();
        var state = new FlowRasterState(targetSize, compositor, textures);
        try
        {
            PrepareBackdropCaptures(root, state, filter => CaptureScene(root, state, rootTransform, filter, false));
            if (root.ContainsMagnifier)
            {
                state.MagnifierBackdrop = CaptureScene(root, state, rootTransform, null, true);
            }

            using (context.PushTransform(rootTransform))
            {
                root.Paint(new FlowPaintContext(context, rootTransform, state));
            }
        }
        finally
        {
            foreach (BackdropCapture capture in state.Backdrops.Values.Distinct())
            {
                capture.Dispose();
            }

            state.MagnifierBackdrop?.Dispose();
            compositor.RasterTime.Stop();
        }

        return state.Resources;
    }

    /// <summary>
    /// Rasterizes <paramref name="root"/> into a new <paramref name="width"/> by <paramref name="height"/>
    /// bitmap, one scene pixel per image pixel: the engine's <c>Scene::toImageSync</c>.
    /// </summary>
    public static Bitmap RasterizeToImage(ContainerFlowLayer root, int width, int height)
    {
        var image = new RenderTargetBitmap(new PixelSize(width, height), Dpi);
        List<IDisposable> resources = [];
        try
        {
            using (DrawingContext context = image.CreateDrawingContext())
            {
                resources = Draw(
                    context,
                    root,
                    new Size(width, height),
                    Matrix.Identity,
                    new CompositorContext(),
                    TextureRegistry.Instance);
            }

            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
        finally
        {
            foreach (IDisposable resource in resources)
            {
                resource.Dispose();
            }
        }
    }

    /// <summary>
    /// Assigns every backdrop filter its captured input, capturing once per backdrop id.
    /// </summary>
    internal static void PrepareBackdropCaptures(
        ContainerFlowLayer root,
        FlowRasterState state,
        Func<BackdropFilterFlowLayer, BackdropCapture> capture)
    {
        var filters = new List<BackdropFilterFlowLayer>();
        root.CollectBackdropFilters(filters);
        var groupedBackdrops = new Dictionary<int, BackdropCapture>();
        foreach (BackdropFilterFlowLayer filter in filters)
        {
            if (filter.BackdropId is { } id && groupedBackdrops.TryGetValue(id, out BackdropCapture? grouped))
            {
                state.Backdrops[filter] = grouped;
                continue;
            }

            BackdropCapture backdrop = capture(filter)
                ?? throw new InvalidOperationException("Backdrop capture must return an image.");
            state.Backdrops[filter] = backdrop;
            if (filter.BackdropId is { } key)
            {
                groupedBackdrops[key] = backdrop;
            }
        }
    }

    private static BackdropCapture CaptureScene(
        ContainerFlowLayer root,
        FlowRasterState state,
        Matrix rootTransform,
        BackdropFilterFlowLayer? stopAt,
        bool capturingMagnifier)
    {
        int width = Math.Max(1, (int)Math.Ceiling(state.TargetSize.Width));
        int height = Math.Max(1, (int)Math.Ceiling(state.TargetSize.Height));
        var bounds = new Rect(0.0, 0.0, width, height);
        var image = new RenderTargetBitmap(new PixelSize(width, height), Dpi);
        t_captureDepth++;
        try
        {
            var captureState = new FlowRasterState(state.TargetSize, state.Compositor, state.Textures)
            {
                StopAt = stopAt,
                CapturingMagnifier = capturingMagnifier,
                Backdrops = state.Backdrops,
                Resources = state.Resources,
            };
            using (DrawingContext captureContext = image.CreateDrawingContext())
            using (captureContext.PushTransform(rootTransform))
            {
                root.Paint(new FlowPaintContext(captureContext, rootTransform, captureState));
            }

            return new BackdropCapture(image, bounds, ownsImage: true);
        }
        catch
        {
            image.Dispose();
            throw;
        }
        finally
        {
            t_captureDepth--;
        }
    }

    /// <summary>The statistics label of the performance overlay, or null without a font backend.</summary>
    internal static FormattedText? TryMakeStatisticsText(string text)
    {
        try
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                15,
                // Historically SK_ColorGRAY (== 0xFF888888) was used here.
                new SolidColorBrush(Avalonia.Media.Color.FromUInt32(0xFF888888)));
            // Lays the text out now, so a missing font backend surfaces here rather than in the draw.
            _ = formatted.Baseline;
            return formatted;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(DrawingContext context, RRect rrect)
    {
        return PushRoundedRectClip(context, rrect.Rect, rrect.Radii);
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        double radius)
    {
        double clampedRadius = Math.Min(Math.Max(0, radius), Math.Min(rect.Width, rect.Height) / 2.0);
        if (Capturing)
        {
            // Avalonia's recording context does not implement PushClip(RoundedRect) in every backend, but
            // its geometry-clip path records the equivalent rounded rectangle correctly.
            return context.PushGeometryClip(new RectangleGeometry(rect, clampedRadius, clampedRadius));
        }

        return context.PushClip(new RoundedRect(rect, clampedRadius));
    }

    internal static DrawingContext.PushedState PushRoundedRectClip(
        DrawingContext context,
        Rect rect,
        BorderRadius borderRadius)
    {
        double maxX = Math.Max(0.0, rect.Width / 2.0);
        double maxY = Math.Max(0.0, rect.Height / 2.0);
        Radius topLeft = Layer.ClampRadius(borderRadius.TopLeftRadius, maxX, maxY);
        Radius topRight = Layer.ClampRadius(borderRadius.TopRightRadius, maxX, maxY);
        Radius bottomRight = Layer.ClampRadius(borderRadius.BottomRightRadius, maxX, maxY);
        Radius bottomLeft = Layer.ClampRadius(borderRadius.BottomLeftRadius, maxX, maxY);
        if (Capturing)
        {
            double fallbackX = Math.Max(
                Math.Max(topLeft.X, topRight.X),
                Math.Max(bottomRight.X, bottomLeft.X));
            double fallbackY = Math.Max(
                Math.Max(topLeft.Y, topRight.Y),
                Math.Max(bottomRight.Y, bottomLeft.Y));
            return context.PushGeometryClip(new RectangleGeometry(rect, fallbackX, fallbackY));
        }

        return context.PushClip(new RoundedRect(
            rect,
            new Vector(topLeft.X, topLeft.Y),
            new Vector(topRight.X, topRight.Y),
            new Vector(bottomRight.X, bottomRight.Y),
            new Vector(bottomLeft.X, bottomLeft.Y)));
    }
}
