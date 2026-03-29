using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/object.dart (approximate)

namespace Flutter.Rendering;

public sealed class PaintingContext
{
    private readonly ContainerLayer _containerLayer;
    private PictureLayer? _currentPictureLayer;

    public PaintingContext(ContainerLayer containerLayer)
    {
        _containerLayer = containerLayer;
    }

    public void PaintChild(RenderObject child, Point offset)
    {
        if (child.IsRepaintBoundary)
        {
            StopRecordingIfNeeded();

            var oldLayer = child._layer as OffsetLayer;
            var layer = child.EnsureCompositedLayer();
            var shouldRepaint = child.NeedsPaint || oldLayer == null || !ReferenceEquals(oldLayer, layer);
            layer.Offset = offset;
            _containerLayer.Append(layer);
            child._layer = layer;

            if (shouldRepaint)
            {
                child.UpdateCompositedLayerProperties();
                layer.RemoveAllChildren();
                var childContext = new PaintingContext(layer);
                child._paintWithContext(childContext, new Point(0, 0));
            }
            else if (child.NeedsCompositedLayerUpdate)
            {
                child.UpdateCompositedLayerProperties();
            }
        }
        else if (child._wasRepaintBoundary)
        {
            child._layer = null;
            child._paintWithContext(this, offset);
        }
        else
        {
            child._paintWithContext(this, offset);
        }
    }

    public void DrawRectangle(
        IBrush brush,
        IPen? pen,
        Rect rect,
        double radiusX = 0,
        double radiusY = 0,
        BoxShadows boxShadows = default)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedRect = new Rect(rect.Position + sceneOffset, rect.Size);
            drawingContext.DrawRectangle(brush, pen, new RoundedRect(translatedRect, radiusX, radiusY), boxShadows);
        });
    }

    public void DrawCircle(IBrush brush, IPen? pen, Point center, double radius)
    {
        var clampedRadius = Math.Max(0, radius);
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) =>
        {
            var translatedCenter = center + sceneOffset;
            drawingContext.DrawEllipse(brush, pen, translatedCenter, clampedRadius, clampedRadius);
        });
    }

    public void DrawTextLayout(TextLayout layout, Point point)
    {
        var pictureLayer = EnsurePictureLayer();
        pictureLayer.AddDrawCommand((drawingContext, sceneOffset) => layout.Draw(drawingContext, point + sceneOffset));
    }

    public void PushClipRect(Rect clipRect, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new ClipRectLayer
        {
            ClipRect = clipRect
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushClipRRect(Rect clipRect, BorderRadius borderRadius, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new ClipRRectLayer
        {
            ClipRect = clipRect,
            BorderRadius = borderRadius
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushTransform(Matrix transform, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new TransformLayer
        {
            Transform = transform
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    public void PushOpacity(double opacity, Action<PaintingContext> painter)
    {
        StopRecordingIfNeeded();

        var layer = new OpacityLayer
        {
            Opacity = opacity
        };

        _containerLayer.Append(layer);

        var childContext = new PaintingContext(layer);
        painter(childContext);
        childContext.StopRecordingIfNeeded();
    }

    private PictureLayer EnsurePictureLayer()
    {
        if (_currentPictureLayer != null)
        {
            return _currentPictureLayer;
        }

        _currentPictureLayer = new PictureLayer();
        _containerLayer.Append(_currentPictureLayer);
        return _currentPictureLayer;
    }

    private void StopRecordingIfNeeded()
    {
        _currentPictureLayer = null;
    }
}
