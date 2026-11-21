using Plumix.Painting;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only infrastructure: the engine side of dart:ui's `SceneBuilder`. A `Scene` holds a tree of these
// retained layers — the counterpart of the engine's `flow/layers` (`ContainerLayer`, `TransformLayer`,
// `ClipRectLayer`, `OpacityLayer`, `DisplayListLayer`, ...). They are framework data only: the host's
// rasterizer paints the tree (Plumix.Avalonia's SceneRasterizer and FlowLayerPainter, the way the engine's
// rasterizer paints a `LayerTree` into a surface), and Scene.toImage reaches it through RenderBackend.
// No Dart source maps to this file.

/// <summary>A layer of the engine's retained layer tree.</summary>
/// <remarks>The engine's <c>flutter::Layer</c>.</remarks>
internal abstract class FlowLayer
{
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

    internal override void CollectBackdropFilters(List<BackdropFilterFlowLayer> filters)
    {
        foreach (FlowLayer layer in _layers)
        {
            layer.CollectBackdropFilters(filters);
        }
    }

    internal override bool ContainsMagnifier => _layers.Any(static layer => layer.ContainsMagnifier);

    /// <summary>
    /// Whether <paramref name="next"/> paints exactly what <paramref name="previous"/> paints because it only
    /// re-adds the previous tree's retained layers: a frame in which nothing repainted re-pushes just the root
    /// transform around the layers <c>addRetained</c> handed back.
    /// </summary>
    /// <remarks>
    /// Conservative: plain containers, transforms and pictures (a picture layer has no engine layer to retain,
    /// so it re-adds its unchanged picture) are compared by value, every other layer must be the same retained
    /// object.
    /// </remarks>
    internal static bool PaintsSameAs(FlowLayer next, FlowLayer previous)
    {
        if (ReferenceEquals(next, previous))
        {
            return true;
        }

        if (next is DisplayListFlowLayer nextPicture && previous is DisplayListFlowLayer previousPicture)
        {
            return ReferenceEquals(nextPicture.Picture, previousPicture.Picture)
                && nextPicture.Offset == previousPicture.Offset;
        }

        if (next is TransformFlowLayer nextTransform && previous is TransformFlowLayer previousTransform)
        {
            if (nextTransform.Transform != previousTransform.Transform)
            {
                return false;
            }
        }
        else if (next.GetType() != typeof(ContainerFlowLayer) || previous.GetType() != typeof(ContainerFlowLayer))
        {
            return false;
        }

        IReadOnlyList<FlowLayer> nextLayers = ((ContainerFlowLayer)next).Layers;
        IReadOnlyList<FlowLayer> previousLayers = ((ContainerFlowLayer)previous).Layers;
        if (nextLayers.Count != previousLayers.Count)
        {
            return false;
        }

        for (int i = 0; i < nextLayers.Count; i++)
        {
            if (!PaintsSameAs(nextLayers[i], previousLayers[i]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// The engine's <c>flutter::TransformLayer</c> (what <c>pushTransform</c> and <c>pushOffset</c> add).
/// </summary>
internal sealed class TransformFlowLayer(Matrix4 transform) : ContainerFlowLayer
{
    public Matrix4 Transform { get; } = transform;
}

/// <summary>The engine's <c>flutter::OpacityLayer</c>: children at an offset, with an alpha.</summary>
internal sealed class OpacityFlowLayer(int alpha, Offset offset) : ContainerFlowLayer
{
    public int Alpha { get; } = alpha;

    public Offset Offset { get; } = offset;
}

/// <summary>The engine's <c>flutter::ClipRectLayer</c>.</summary>
internal sealed class ClipRectFlowLayer(Rect clipRect, Clip clipBehavior) : ContainerFlowLayer
{
    public Rect ClipRect { get; } = clipRect;

    public Clip ClipBehavior { get; } = clipBehavior;
}

/// <summary>The engine's <c>flutter::ClipRRectLayer</c>.</summary>
internal sealed class ClipRRectFlowLayer(RRect clipRRect, Clip clipBehavior) : ContainerFlowLayer
{
    public RRect ClipRRect { get; } = clipRRect;

    public Clip ClipBehavior { get; } = clipBehavior;
}

/// <summary>The engine's <c>flutter::ClipRSuperellipseLayer</c>.</summary>
internal sealed class ClipRSuperellipseFlowLayer(RSuperellipse clipRSuperellipse, Clip clipBehavior)
    : ContainerFlowLayer
{
    public RSuperellipse ClipRSuperellipse { get; } = clipRSuperellipse;

    public Clip ClipBehavior { get; } = clipBehavior;
}

/// <summary>The engine's <c>flutter::ClipPathLayer</c>.</summary>
internal sealed class ClipPathFlowLayer(Path clipPath, Clip clipBehavior) : ContainerFlowLayer
{
    public Path ClipPath { get; } = clipPath;

    public Clip ClipBehavior { get; } = clipBehavior;

    /// <summary>The clip geometry the host's rasterizer built from <see cref="ClipPath"/> on first paint.</summary>
    internal object? BackendGeometry { get; set; }

    /// <summary>
    /// Reuses the geometry <paramref name="oldLayer"/> converted for the same path: the counterpart of the
    /// engine's <c>Layer::AssignOldLayer</c> resource reuse.
    /// </summary>
    internal void AssignOldLayer(ClipPathFlowLayer? oldLayer)
    {
        if (oldLayer != null && ReferenceEquals(oldLayer.ClipPath, ClipPath))
        {
            BackendGeometry ??= oldLayer.BackendGeometry;
        }
    }
}

/// <summary>The engine's <c>flutter::ColorFilterLayer</c>, over the CPU filter backend.</summary>
internal sealed class ColorFilterFlowLayer(ColorFilter filter, Rect? bounds) : ContainerFlowLayer
{
    public ColorFilter Filter { get; } = filter;

    /// <summary>The Plumix-only rasterization bounds, in this layer's coordinates.</summary>
    public Rect? Bounds { get; } = bounds;
}

/// <summary>The engine's <c>flutter::ImageFilterLayer</c>, over the CPU filter backend.</summary>
internal sealed class ImageFilterFlowLayer(ImageFilter filter, Offset offset, Rect? bounds) : ContainerFlowLayer
{
    public ImageFilter Filter { get; } = filter;

    public Offset Offset { get; } = offset;

    /// <summary>The Plumix-only rasterization bounds, in the children's (offset) coordinates.</summary>
    public Rect? Bounds { get; } = bounds;
}

/// <summary>The engine's <c>flutter::ShaderMaskLayer</c>, over the CPU filter backend.</summary>
internal sealed class ShaderMaskFlowLayer(Shader shader, Rect maskRect, BlendMode blendMode, FilterQuality quality)
    : ContainerFlowLayer
{
    public Shader Shader { get; } = shader;

    public Rect MaskRect { get; } = maskRect;

    public BlendMode BlendMode { get; } = blendMode;

    public FilterQuality FilterQuality { get; } = quality;
}

/// <summary>
/// The engine's <c>flutter::BackdropFilterLayer</c>: filters what was painted before it, then paints its
/// children on top.
/// </summary>
/// <remarks>
/// The backdrop is a capture of the scene prefix painted before this layer (the host's rasterizer);
/// layers sharing a backdrop id share one capture, the way the engine shares a backdrop-id snapshot.
/// </remarks>
internal sealed class BackdropFilterFlowLayer(ImageFilter filter, BlendMode blendMode, int? backdropId)
    : ContainerFlowLayer
{
    public ImageFilter Filter { get; } = filter;

    public BlendMode BlendMode { get; } = blendMode;

    public int? BackdropId { get; } = backdropId;

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
    Offset focalPointOffset,
    double magnificationScale,
    MagnifierDecoration decoration,
    Clip clipBehavior) : ContainerFlowLayer
{
    public Rect LensRect { get; } = lensRect;

    public Offset FocalPointOffset { get; } = focalPointOffset;

    public double MagnificationScale { get; } = magnificationScale;

    public MagnifierDecoration Decoration { get; } = decoration;

    public Clip ClipBehavior { get; } = clipBehavior;

    internal override bool ContainsMagnifier => true;
}

/// <summary>The engine's <c>flutter::DisplayListLayer</c>: a recorded picture at an offset.</summary>
internal sealed class DisplayListFlowLayer(Offset offset, Picture picture, bool isComplex, bool willChange)
    : FlowLayer
{
    public Offset Offset { get; } = offset;

    public Picture Picture { get; } = picture;

    public bool IsComplex { get; } = isComplex;

    public bool WillChange { get; } = willChange;
}

/// <summary>The engine's <c>flutter::TextureLayer</c>: a registered backend texture.</summary>
internal sealed class TextureFlowLayer(Offset offset, Size size, int textureId, bool freeze, FilterQuality quality)
    : FlowLayer
{
    public Offset Offset { get; } = offset;

    public Size Size { get; } = size;

    public int TextureId { get; } = textureId;

    public bool Freeze { get; } = freeze;

    public FilterQuality FilterQuality { get; } = quality;
}

/// <summary>
/// The engine's <c>flutter::PlatformViewLayer</c>. No Plumix host embeds platform views, so it paints
/// nothing (docs/ai/DIVERGENCES.md).
/// </summary>
internal sealed class PlatformViewFlowLayer(Offset offset, Size size, int viewId) : FlowLayer
{
    public Offset Offset { get; } = offset;

    public Size Size { get; } = size;

    public int ViewId { get; } = viewId;
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
}
