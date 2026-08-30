using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// <summary>Signature for painting into a <see cref="PaintingContext"/>.</summary>
/// <remarks>Flutter's <c>PaintingContextCallback</c>.</remarks>
public delegate void PaintingContextCallback(PaintingContext context, Point offset);

/// <summary>A place to paint.</summary>
public class PaintingContext : ClipContext
{
    private readonly ContainerLayer _containerLayer;
    private PictureLayer? _currentLayer;
    private PictureRecorder? _recorder;
    private Canvas? _canvas;

    public PaintingContext(ContainerLayer containerLayer, Rect estimatedBounds = default)
    {
        _containerLayer = containerLayer;
        EstimatedBounds = estimatedBounds;
    }

    /// <summary>An estimate of the bounds within which this context's drawing takes place.</summary>
    /// <remarks>Flutter's <c>PaintingContext.estimatedBounds</c>.</remarks>
    public Rect EstimatedBounds { get; }

    /// <summary>Repaints the given render object, which must be a repaint boundary.</summary>
    /// <remarks>Flutter's <c>PaintingContext.repaintCompositedChild</c>.</remarks>
    public static void RepaintCompositedChild(RenderObject child, bool debugAlsoPaintedParent = false)
    {
        ArgumentNullException.ThrowIfNull(child);
        Debug.Assert(child.NeedsPaint);
        RepaintCompositedChildInternal(child, debugAlsoPaintedParent);
    }

    /// <remarks>Flutter's <c>PaintingContext._repaintCompositedChild</c>.</remarks>
    private static void RepaintCompositedChildInternal(
        RenderObject child,
        bool debugAlsoPaintedParent = false,
        PaintingContext? childContext = null)
    {
        Debug.Assert(child.IsRepaintBoundary);
        if (Constants.KDebugMode)
        {
            child.DebugRegisterRepaintBoundaryPaint(
                includedParent: debugAlsoPaintedParent,
                includedChild: true);
        }

        var childLayer = child._layer as OffsetLayer;
        if (childLayer is null)
        {
            Debug.Assert(debugAlsoPaintedParent);
            childLayer = child.UpdateCompositedLayerForRepaint();
        }
        else
        {
            Debug.Assert(debugAlsoPaintedParent || childLayer.Attached);
            Point debugOldOffset = childLayer.Offset;
            childLayer.RemoveAllChildren();
            OffsetLayer updatedLayer = child.UpdateCompositedLayerForRepaint();
            if (!ReferenceEquals(updatedLayer, childLayer))
            {
                throw new AssertionError(
                    $"{child} created a new layer instance {updatedLayer} instead of reusing the existing "
                    + $"layer {childLayer}. See the documentation of RenderObject.UpdateCompositedLayer "
                    + "for more information on how to correctly implement this method.");
            }

            Debug.Assert(debugOldOffset == updatedLayer.Offset);
        }

        if (Constants.KDebugMode)
        {
            childLayer.DebugCreator = child.DebugCreator ?? child.GetType();
        }

        childContext ??= new PaintingContext(childLayer, child.PaintBounds);
        child._paintWithContext(childContext, new Point(0, 0));
        Debug.Assert(ReferenceEquals(childLayer, child._layer));
        childContext.StopRecordingIfNeeded();
    }

    /// <summary>Re-runs the composited-layer update of a clean repaint boundary.</summary>
    /// <remarks>Flutter's <c>PaintingContext.updateLayerProperties</c>.</remarks>
    public static void UpdateLayerProperties(RenderObject child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Debug.Assert(child.IsRepaintBoundary && child._wasRepaintBoundary);
        Debug.Assert(!child.NeedsPaint);
        Debug.Assert(child._layer is not null);

        var childLayer = (OffsetLayer)child._layer!;
        Point debugOldOffset = childLayer.Offset;
        OffsetLayer updatedLayer = child.UpdateCompositedLayerForRepaint();
        if (!ReferenceEquals(updatedLayer, childLayer))
        {
            throw new AssertionError(
                $"{child} created a new layer instance {updatedLayer} instead of reusing the existing "
                + $"layer {childLayer}. See the documentation of RenderObject.UpdateCompositedLayer "
                + "for more information on how to correctly implement this method.");
        }

        Debug.Assert(debugOldOffset == updatedLayer.Offset);
    }

    /// <remarks>Flutter's <c>PaintingContext.debugInstrumentRepaintCompositedChild</c>.</remarks>
    public static void DebugInstrumentRepaintCompositedChild(
        RenderObject child,
        PaintingContext customContext,
        bool debugAlsoPaintedParent = false)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        RepaintCompositedChildInternal(child, debugAlsoPaintedParent, customContext);
    }

    public void PaintChild(RenderObject child, Point offset)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (Constants.KDebugMode)
        {
            RenderingDebug.OnProfilePaint?.Invoke(child);
        }

        if (child.IsRepaintBoundary)
        {
            StopRecordingIfNeeded();
            CompositeChild(child, offset);
        }
        else if (child._wasRepaintBoundary)
        {
            Debug.Assert(child._layer is OffsetLayer);
            child._layer = null;
            child._paintWithContext(this, offset);
        }
        else
        {
            child._paintWithContext(this, offset);
        }
    }

    /// <remarks>Flutter's <c>PaintingContext._compositeChild</c>.</remarks>
    private void CompositeChild(RenderObject child, Point offset)
    {
        Debug.Assert(!IsRecording);
        Debug.Assert(child.IsRepaintBoundary);

        // Create a layer for our child, and paint the child into it.
        if (child.NeedsPaint || !child._wasRepaintBoundary)
        {
            RepaintCompositedChild(child, debugAlsoPaintedParent: true);
        }
        else
        {
            if (child.NeedsCompositedLayerUpdate)
            {
                UpdateLayerProperties(child);
            }

            if (Constants.KDebugMode)
            {
                child.DebugRegisterRepaintBoundaryPaint();
                if (child._layer is { } childDebugLayer)
                {
                    childDebugLayer.DebugCreator = child.DebugCreator ?? child;
                }
            }
        }

        Debug.Assert(child._layer is OffsetLayer);
        var childOffsetLayer = (OffsetLayer)child._layer!;
        childOffsetLayer.Offset = offset;
        AppendLayer(childOffsetLayer);
    }

    /// <summary>Adds a composited leaf layer to the recording.</summary>
    /// <remarks>Flutter's <c>PaintingContext.addLayer</c>.</remarks>
    public void AddLayer(Layer layer)
    {
        StopRecordingIfNeeded();
        AppendLayer(layer);
    }

    /// <summary>Appends the given layer, detaching it from any previous parent first.</summary>
    /// <remarks>Flutter's <c>PaintingContext.appendLayer</c>.</remarks>
    protected virtual void AppendLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        Debug.Assert(!IsRecording);
        layer.Remove();
        _containerLayer.Append(layer);
    }

    /// <remarks>Flutter's <c>PaintingContext._isRecording</c>.</remarks>
    private bool IsRecording => _canvas is not null;

    /// <summary>The recorder the current picture is being recorded into.</summary>
    /// <remarks>Flutter's <c>PaintingContext.recorder</c>.</remarks>
    public PictureRecorder Recorder
    {
        get
        {
            if (_recorder is null)
            {
                StartRecording();
            }

            return _recorder!;
        }
    }

    /// <summary>The canvas on which to paint.</summary>
    /// <remarks>Flutter's <c>PaintingContext.canvas</c>.</remarks>
    public override Canvas Canvas
    {
        get
        {
            if (_canvas is null)
            {
                StartRecording();
            }

            return _canvas!;
        }
    }

    /// <remarks>Flutter's <c>PaintingContext._startRecording</c>.</remarks>
    private void StartRecording()
    {
        Debug.Assert(!IsRecording);
        _currentLayer = new PictureLayer(EstimatedBounds);
        _recorder = new PictureRecorder();
        _canvas = new Canvas(_recorder);
        _containerLayer.Append(_currentLayer);
    }

    /// <summary>Hints that the painting in the current layer is complex enough to benefit from caching.</summary>
    /// <remarks>Flutter's <c>PaintingContext.setIsComplexHint</c>.</remarks>
    public void SetIsComplexHint()
    {
        if (_currentLayer is null)
        {
            StartRecording();
        }

        _currentLayer!.IsComplexHint = true;
    }

    /// <summary>Hints that the painting in the current layer is likely to change next frame.</summary>
    /// <remarks>Flutter's <c>PaintingContext.setWillChangeHint</c>.</remarks>
    public void SetWillChangeHint()
    {
        if (_currentLayer is null)
        {
            StartRecording();
        }

        _currentLayer!.WillChangeHint = true;
    }

    /// <summary>Ends the current picture recording, if one is in progress.</summary>
    /// <remarks>
    /// Flutter's <c>PaintingContext.stopRecordingIfNeeded</c>. Dart's <c>@protected</c> is advisory, so
    /// its own tests call this directly; <see cref="DebugStopRecordingIfNeeded"/> is the C# equivalent.
    /// </remarks>
    protected virtual void StopRecordingIfNeeded()
    {
        if (!IsRecording)
        {
            return;
        }

        if (Constants.KDebugMode)
        {
            if (RenderingDebug.RepaintRainbowEnabled)
            {
                var pen = new Pen(new SolidColorBrush(RenderingDebug.CurrentRepaintColor.ToColor()), 6.0);
                Canvas.DrawGeometry(null, pen, new RectangleGeometry(DeflateRect(EstimatedBounds, 3.0)));
            }

            if (RenderingDebug.PaintLayerBordersEnabled)
            {
                var pen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFFFF9800)), 1.0);
                Canvas.DrawGeometry(null, pen, new RectangleGeometry(EstimatedBounds));
            }
        }

        _currentLayer!.Picture = _recorder!.EndRecording();
        _currentLayer = null;
        _recorder = null;
        _canvas = null;
    }

    /// <summary>Test-only entry point to <see cref="StopRecordingIfNeeded"/>.</summary>
    internal void DebugStopRecordingIfNeeded() => StopRecordingIfNeeded();

    /// <summary>Adds a composited layer and paints into it via <paramref name="painter"/>.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushLayer</c>.</remarks>
    public void PushLayer(
        ContainerLayer childLayer,
        PaintingContextCallback painter,
        Point offset,
        Rect? childPaintBounds = null)
    {
        ArgumentNullException.ThrowIfNull(childLayer);
        ArgumentNullException.ThrowIfNull(painter);

        // If a layer is being reused it may already have children; remove them so `painter` can add
        // the children that are relevant for this frame.
        if (childLayer.HasChildren)
        {
            childLayer.RemoveAllChildren();
        }

        StopRecordingIfNeeded();
        AppendLayer(childLayer);

        PaintingContext childContext = CreateChildContext(childLayer, childPaintBounds ?? EstimatedBounds);
        painter(childContext, offset);
        childContext.StopRecordingIfNeeded();
    }

    /// <summary>Creates a painting context for a child layer.</summary>
    /// <remarks>Flutter's <c>PaintingContext.createChildContext</c>.</remarks>
    protected virtual PaintingContext CreateChildContext(ContainerLayer childLayer, Rect bounds)
    {
        return new PaintingContext(childLayer, bounds);
    }

    /// <summary>Clips using a rectangle, then paints.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushClipRect</c>.</remarks>
    public ClipRectLayer? PushClipRect(
        bool needsCompositing,
        Point offset,
        Rect clipRect,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.HardEdge,
        ClipRectLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(painter);
        if (clipBehavior == Clip.None)
        {
            painter(this, offset);
            return null;
        }

        Rect offsetClipRect = ShiftRect(clipRect, offset);
        if (needsCompositing)
        {
            ClipRectLayer layer = oldLayer ?? new ClipRectLayer();
            layer.ClipRect = offsetClipRect;
            layer.ClipBehavior = clipBehavior;
            PushLayer(layer, painter, offset, offsetClipRect);
            return layer;
        }

        ClipRectAndPaint(offsetClipRect, clipBehavior, offsetClipRect, () => painter(this, offset));
        return null;
    }

    /// <summary>Clips using a rounded rectangle, then paints.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushClipRRect</c>.</remarks>
    public ClipRRectLayer? PushClipRRect(
        bool needsCompositing,
        Point offset,
        Rect bounds,
        RRect clipRRect,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.AntiAlias,
        ClipRRectLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(painter);
        if (clipBehavior == Clip.None)
        {
            painter(this, offset);
            return null;
        }

        Rect offsetBounds = ShiftRect(bounds, offset);
        RRect offsetClipRRect = clipRRect.Shift(offset);
        if (needsCompositing)
        {
            ClipRRectLayer layer = oldLayer ?? new ClipRRectLayer();
            layer.ClipRRect = offsetClipRRect;
            layer.ClipBehavior = clipBehavior;
            PushLayer(layer, painter, offset, offsetBounds);
            return layer;
        }

        ClipRRectAndPaint(offsetClipRRect, clipBehavior, offsetBounds, () => painter(this, offset));
        return null;
    }

    /// <summary>Clips using a rounded superellipse, then paints.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushClipRSuperellipse</c>.</remarks>
    public ClipRSuperellipseLayer? PushClipRSuperellipse(
        bool needsCompositing,
        Point offset,
        Rect bounds,
        RSuperellipse clipRSuperellipse,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.AntiAlias,
        ClipRSuperellipseLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(painter);
        if (clipBehavior == Clip.None)
        {
            painter(this, offset);
            return null;
        }

        Rect offsetBounds = ShiftRect(bounds, offset);
        RSuperellipse offsetShape = clipRSuperellipse.Shift(offset);
        if (needsCompositing)
        {
            ClipRSuperellipseLayer layer = oldLayer ?? new ClipRSuperellipseLayer();
            layer.ClipRSuperellipse = offsetShape;
            layer.ClipBehavior = clipBehavior;
            PushLayer(layer, painter, offset, offsetBounds);
            return layer;
        }

        ClipRSuperellipseAndPaint(offsetShape, clipBehavior, offsetBounds, () => painter(this, offset));
        return null;
    }

    /// <summary>Clips using a path, then paints.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushClipPath</c>.</remarks>
    public ClipPathLayer? PushClipPath(
        bool needsCompositing,
        Point offset,
        Rect bounds,
        Path clipPath,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.AntiAlias,
        ClipPathLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(clipPath);
        ArgumentNullException.ThrowIfNull(painter);
        if (clipBehavior == Clip.None)
        {
            painter(this, offset);
            return null;
        }

        Rect offsetBounds = ShiftRect(bounds, offset);
        Path offsetClipPath = clipPath.Shift(offset);
        if (needsCompositing)
        {
            ClipPathLayer layer = oldLayer ?? new ClipPathLayer();
            layer.ClipPath = offsetClipPath;
            layer.ClipBehavior = clipBehavior;
            PushLayer(layer, painter, offset, offsetBounds);
            return layer;
        }

        ClipPathAndPaint(offsetClipPath, clipBehavior, offsetBounds, () => painter(this, offset));
        return null;
    }

    /// <summary>Plumix-only: clips using a backend geometry, then paints.</summary>
    /// <remarks>
    /// Dart's clip family goes through <c>Path</c> only; a handful of Plumix shapes (notched app bars,
    /// decoration outlines) arrive as an Avalonia <c>Geometry</c> that cannot be shifted, so the offset
    /// travels alongside it. Everything else matches <see cref="PushClipPath"/>.
    /// </remarks>
    public ClipGeometryLayer? PushClipGeometry(
        bool needsCompositing,
        Point offset,
        Rect bounds,
        Geometry geometry,
        PaintingContextCallback painter,
        Clip clipBehavior = Clip.AntiAlias,
        ClipGeometryLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(painter);
        if (clipBehavior == Clip.None)
        {
            painter(this, offset);
            return null;
        }

        Rect offsetBounds = ShiftRect(bounds, offset);
        if (needsCompositing)
        {
            ClipGeometryLayer layer = oldLayer ?? new ClipGeometryLayer();
            layer.Geometry = geometry;
            layer.GeometryOffset = offset;
            layer.ClipBehavior = clipBehavior;
            PushLayer(layer, painter, offset, offsetBounds);
            return layer;
        }

        ClipGeometryAndPaint(geometry, offset, clipBehavior, offsetBounds, () => painter(this, offset));
        return null;
    }

    /// <summary>Blends the painting with a color filter, in its own composited layer.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushColorFilter</c>.</remarks>
    public ColorFilterLayer PushColorFilter(
        Point offset,
        ColorFilter colorFilter,
        PaintingContextCallback painter,
        ColorFilterLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(colorFilter);
        ArgumentNullException.ThrowIfNull(painter);
        ColorFilterLayer layer = oldLayer ?? new ColorFilterLayer();
        layer.ColorFilter = colorFilter;
        PushLayer(layer, painter, offset);
        return layer;
    }

    /// <summary>Transforms the painting, then paints.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushTransform</c>.</remarks>
    public TransformLayer? PushTransform(
        bool needsCompositing,
        Point offset,
        Matrix4 transform,
        PaintingContextCallback painter,
        TransformLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(transform);
        ArgumentNullException.ThrowIfNull(painter);
        Matrix4 effectiveTransform = Matrix4.TranslationValues(offset.X, offset.Y, 0.0);
        effectiveTransform.Multiply(transform);
        effectiveTransform.TranslateByDouble(-offset.X, -offset.Y, 0.0, 1.0);
        if (needsCompositing)
        {
            TransformLayer layer = oldLayer ?? new TransformLayer();
            layer.Transform = effectiveTransform;
            PushLayer(
                layer,
                painter,
                offset,
                MatrixUtils.InverseTransformRect(effectiveTransform, EstimatedBounds));
            return layer;
        }

        Canvas.Save();
        Canvas.Transform(effectiveTransform);
        painter(this, offset);
        Canvas.Restore();
        return null;
    }

    /// <summary>Blends the painting with an alpha value, in its own composited layer.</summary>
    /// <remarks>Flutter's <c>PaintingContext.pushOpacity</c>.</remarks>
    public OpacityLayer PushOpacity(
        Point offset,
        int alpha,
        PaintingContextCallback painter,
        OpacityLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(painter);
        OpacityLayer layer = oldLayer ?? new OpacityLayer();
        layer.Alpha = alpha;
        layer.Offset = offset;
        PushLayer(layer, painter, new Point(0, 0));
        return layer;
    }

    /// <summary>Plumix-only: paints through a magnifier layer, which has no Flutter counterpart.</summary>
    public MagnifierLayer PushMagnifier(
        Rect lensRect,
        Point focalPointOffset,
        double magnificationScale,
        MagnifierDecoration decoration,
        Clip clipBehavior,
        PaintingContextCallback painter,
        MagnifierLayer? oldLayer = null)
    {
        ArgumentNullException.ThrowIfNull(decoration);
        ArgumentNullException.ThrowIfNull(painter);
        MagnifierLayer layer = oldLayer ?? new MagnifierLayer();
        layer.LensRect = lensRect;
        layer.FocalPointOffset = focalPointOffset;
        layer.MagnificationScale = magnificationScale;
        layer.Decoration = decoration;
        layer.ClipBehavior = clipBehavior;
        PushLayer(layer, painter, new Point(0, 0));
        return layer;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"PaintingContext#{GetHashCode()}(layer: {_containerLayer}, canvas bounds: {EstimatedBounds})";

    /// <remarks>Dart's <c>Rect.shift</c>.</remarks>
    private static Rect ShiftRect(Rect rect, Point offset) => new(rect.Position + offset, rect.Size);

    /// <remarks>Dart's <c>Rect.deflate</c>.</remarks>
    private static Rect DeflateRect(Rect rect, double delta)
    {
        return new Rect(
            rect.X + delta,
            rect.Y + delta,
            Math.Max(0.0, rect.Width - (delta * 2.0)),
            Math.Max(0.0, rect.Height - (delta * 2.0)));
    }
}
