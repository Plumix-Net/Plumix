using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Painting;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only host infrastructure: the paint half of the engine's `flow/layers` (`Layer::Paint` of
// `ContainerLayer`, `TransformLayer`, `ClipRectLayer`, `OpacityLayer`, `DisplayListLayer`, ...) over
// Avalonia's drawing context. The layers themselves are framework data (src/Plumix/UI/FlowLayers.cs);
// this painter dispatches on the layer type. Filters, shader masks and backdrops have no native Avalonia
// layer, so they go through the CPU raster backend (`FilterLayerRasterizer`); see docs/ai/DIVERGENCES.md.
// No Dart source maps to this file.

/// <summary>Paints the engine's retained layers: every <c>Layer::Paint</c>, by layer type.</summary>
internal static class FlowLayerPainter
{
    // The engine's ClipShapeLayer: `Clip.antiAliasWithSaveLayer` paints the children into a save layer
    // (an isolated backend group) bounded by the clip, so their edges blend once against the clip.
    private static readonly IBrush SaveLayerMask = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Colors.White);

    /// <summary>Paints <paramref name="layer"/>: the engine's <c>Layer::Paint</c>.</summary>
    internal static void Paint(FlowLayer layer, FlowPaintContext context)
    {
        switch (layer)
        {
            case TransformFlowLayer transform:
                PaintChildrenTransformed(transform, context, transform.Transform.ToAvaloniaMatrix());
                break;
            case OpacityFlowLayer opacity:
                PaintOpacity(opacity, context);
                break;
            case ClipRectFlowLayer clipRect:
                PaintClipRect(clipRect, context);
                break;
            case ClipRRectFlowLayer clipRRect:
                PaintClipRRect(clipRRect, context);
                break;
            case ClipRSuperellipseFlowLayer clipRSuperellipse:
                PaintClipRSuperellipse(clipRSuperellipse, context);
                break;
            case ClipPathFlowLayer clipPath:
                PaintClipPath(clipPath, context);
                break;
            case ColorFilterFlowLayer colorFilter:
                PaintColorFilter(colorFilter, context);
                break;
            case ImageFilterFlowLayer imageFilter:
                PaintImageFilter(imageFilter, context);
                break;
            case ShaderMaskFlowLayer shaderMask:
                PaintShaderMask(shaderMask, context);
                break;
            case BackdropFilterFlowLayer backdropFilter:
                PaintBackdropFilter(backdropFilter, context);
                break;
            case MagnifierFlowLayer magnifier:
                PaintMagnifier(magnifier, context);
                break;
            case ContainerFlowLayer container:
                PaintChildren(container, context);
                break;
            case DisplayListFlowLayer displayList:
                CanvasPlayback.Play(displayList.Picture, context.Canvas, displayList.Offset);
                break;
            case TextureFlowLayer texture:
                PaintTexture(texture, context);
                break;
            case PlatformViewFlowLayer:
                break;
            case PerformanceOverlayFlowLayer performanceOverlay:
                PaintPerformanceOverlay(performanceOverlay, context);
                break;
            case HostFlowLayer host:
                host.Paint(context);
                break;
            default:
                throw new NotSupportedException($"Unknown flow layer {layer.GetType().Name}.");
        }
    }

    private static IDisposable? SaveLayerIfNeeded(FlowPaintContext context, Clip clipBehavior, Rect bounds)
    {
        if (clipBehavior != Clip.AntiAliasWithSaveLayer)
        {
            return null;
        }

        return context.Canvas.PushOpacityMask(SaveLayerMask, bounds.ToAvalonia());
    }

    /// <summary>Paints every child in order: the engine's <c>ContainerLayer::PaintChildren</c>.</summary>
    internal static void PaintChildren(ContainerFlowLayer container, FlowPaintContext context)
    {
        foreach (FlowLayer layer in container.Layers)
        {
            if (context.State.Stopped)
            {
                return;
            }

            Paint(layer, context);
        }
    }

    /// <summary>Paints every child under <paramref name="transform"/>, tracking it in the context.</summary>
    private static void PaintChildrenTransformed(ContainerFlowLayer container, FlowPaintContext context,
        Matrix transform)
    {
        Matrix previous = context.Transform;
        using (context.Canvas.PushTransform(transform))
        {
            context.Transform = transform * previous;
            try
            {
                PaintChildren(container, context);
            }
            finally
            {
                context.Transform = previous;
            }
        }
    }

    private static void PaintOpacity(OpacityFlowLayer layer, FlowPaintContext context)
    {
        Matrix previous = context.Transform;
        Matrix translation = Matrix.CreateTranslation(layer.Offset.Dx, layer.Offset.Dy);
        using (context.Canvas.PushTransform(translation))
        {
            context.Transform = translation * previous;
            try
            {
                if (layer.Alpha >= 255)
                {
                    PaintChildren(layer, context);
                    return;
                }

                // The engine's opacity layer is a save-layer: overlapping children blend with each other
                // first and the alpha applies once. Avalonia folds opacity into every draw unless asked.
                using (context.Canvas.PushRenderOptions(new RenderOptions { RequiresFullOpacityHandling = true }))
                using (context.Canvas.PushOpacity(Math.Clamp(layer.Alpha, 0, 255) / 255.0))
                {
                    PaintChildren(layer, context);
                }
            }
            finally
            {
                context.Transform = previous;
            }
        }
    }

    private static void PaintClipRect(ClipRectFlowLayer layer, FlowPaintContext context)
    {
        using (context.Canvas.PushClip(layer.ClipRect.ToAvalonia()))
        using (SaveLayerIfNeeded(context, layer.ClipBehavior, layer.ClipRect))
        {
            PaintChildren(layer, context);
        }
    }

    private static void PaintClipRRect(ClipRRectFlowLayer layer, FlowPaintContext context)
    {
        using (SceneRasterizer.PushRoundedRectClip(context.Canvas, layer.ClipRRect))
        using (SaveLayerIfNeeded(context, layer.ClipBehavior, layer.ClipRRect.Rect))
        {
            PaintChildren(layer, context);
        }
    }

    private static void PaintClipRSuperellipse(ClipRSuperellipseFlowLayer layer, FlowPaintContext context)
    {
        using (context.Canvas.PushGeometryClip(layer.ClipRSuperellipse.ToPath().ToGeometry()))
        using (SaveLayerIfNeeded(context, layer.ClipBehavior, layer.ClipRSuperellipse.OuterRect))
        {
            PaintChildren(layer, context);
        }
    }

    private static void PaintClipPath(ClipPathFlowLayer layer, FlowPaintContext context)
    {
        var geometry = (Geometry)(layer.BackendGeometry ??= layer.ClipPath.ToGeometry());
        using (context.Canvas.PushGeometryClip(geometry))
        using (SaveLayerIfNeeded(context, layer.ClipBehavior, layer.ClipPath.GetBounds()))
        {
            PaintChildren(layer, context);
        }
    }

    private static void PaintColorFilter(ColorFilterFlowLayer layer, FlowPaintContext context)
    {
        if (layer.Bounds is not { } bounds)
        {
            PaintChildren(layer, context);
            return;
        }

        context.Track(FilterLayerRasterizer.DrawColorFiltered(
            context.Canvas,
            canvas => PaintChildren(layer, context.WithCanvas(canvas, Matrix.Identity)),
            layer.Filter,
            bounds));
    }

    private static void PaintImageFilter(ImageFilterFlowLayer layer, FlowPaintContext context)
    {
        if (layer.Filter is ImageFilter.Matrix matrix)
        {
            // A matrix filter only moves pixels, so the children are drawn through the matrix at device
            // resolution instead of through the CPU backend, which samples a 1 px-per-logical-pixel raster:
            // the stretch overscroll (Transform with a filter quality) would otherwise blur all text by the
            // device pixel ratio. Vectors stay sharp where Skia resamples the layer (DIVERGENCES.md).
            IReadOnlyList<double> m = matrix.Values;
            var transform = new Matrix(m[0], m[1], m[3], m[4], m[5], m[7], m[12], m[13], m[15]);
            PaintChildrenTransformed(
                layer, context, transform * Matrix.CreateTranslation(layer.Offset.Dx, layer.Offset.Dy));
            return;
        }

        if (layer.Bounds is not { } bounds)
        {
            PaintChildrenTransformed(layer, context, Matrix.CreateTranslation(layer.Offset.Dx, layer.Offset.Dy));
            return;
        }

        context.Track(FilterLayerRasterizer.DrawImageFiltered(
            context.Canvas,
            canvas => PaintChildren(layer, context.WithCanvas(canvas, Matrix.Identity)),
            layer.Filter,
            layer.Offset,
            bounds));
    }

    private static void PaintShaderMask(ShaderMaskFlowLayer layer, FlowPaintContext context)
    {
        context.Track(FilterLayerRasterizer.DrawShaderMasked(
            context.Canvas,
            canvas => PaintChildren(layer, context.WithCanvas(canvas, Matrix.Identity)),
            SkiaShaders.ToBrush(layer.Shader),
            layer.BlendMode,
            layer.MaskRect));
    }

    private static void PaintBackdropFilter(BackdropFilterFlowLayer layer, FlowPaintContext context)
    {
        FlowRasterState state = context.State;
        if (ReferenceEquals(state.StopAt, layer))
        {
            state.Stopped = true;
            return;
        }

        if (state.Backdrops.TryGetValue(layer, out BackdropCapture? backdrop)
            && context.Transform.TryInvert(out Matrix toBase))
        {
            // The capture is in the target's coordinates; draw it there, under the clips in effect.
            using (context.Canvas.PushTransform(toBase))
            {
                context.Track(FilterLayerRasterizer.DrawBackdropFiltered(
                    context.Canvas,
                    backdrop.Image,
                    backdrop.Bounds,
                    layer.Filter,
                    layer.BlendMode));
            }
        }

        PaintChildren(layer, context);
    }

    private static void PaintMagnifier(MagnifierFlowLayer layer, FlowPaintContext context)
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

        Rect lensRect = layer.LensRect.ToAvalonia().TransformToAABB(toTarget).ToPlumix();
        if (lensRect.Width <= 0 || lensRect.Height <= 0)
        {
            return;
        }

        var focalPointOffset = new Offset(
            (layer.FocalPointOffset.Dx * toTarget.M11) + (layer.FocalPointOffset.Dy * toTarget.M21),
            (layer.FocalPointOffset.Dx * toTarget.M12) + (layer.FocalPointOffset.Dy * toTarget.M22));
        BorderRadius borderRadius = ResolveBorderRadius(layer, lensRect);
        double opacity = Math.Clamp(layer.Decoration.Opacity, 0.0, 1.0);
        using (context.Canvas.PushTransform(toBase))
        using (context.Canvas.PushOpacity(opacity))
        using (SceneRasterizer.PushRoundedRectClip(context.Canvas, lensRect, borderRadius))
        {
            DrawMagnifiedBackdrop(layer, context.Canvas, state.MagnifierBackdrop, lensRect, focalPointOffset);
            using (context.Canvas.PushTransform(toTarget))
            {
                PaintChildren(layer, context);
            }
        }

        // RawMagnifier.build's younger sibling has its own Opacity, so the decoration fades
        // separately from the magnified image.
        using (context.Canvas.PushOpacity(opacity))
        {
            DrawMagnifierDecoration(layer, context.Canvas);
        }
    }

    /// <summary>
    /// Resolves the decoration shape to the per-corner radii the lens is clipped and stroked with.
    /// Radii too large for the lens shrink together by one factor, the way Dart's RRect does when
    /// it is drawn, so an oversized elliptical radius gives a stadium rather than an ellipse.
    /// </summary>
    private static BorderRadius ResolveBorderRadius(MagnifierFlowLayer layer, Rect lensRect)
    {
        double maxX = lensRect.Width / 2.0;
        double maxY = lensRect.Height / 2.0;
        switch (layer.Decoration.Shape)
        {
            case CircleBorder or StadiumBorder:
                return BorderRadius.Circular(Math.Min(maxX, maxY));
            case RoundedRectangleBorder rounded:
                RRect scaled = rounded.BorderRadius.Resolve(TextDirection.Ltr).ToRRect(lensRect).NormalizeForPaint();
                return new BorderRadius(scaled.TopLeft, scaled.TopRight, scaled.BottomRight, scaled.BottomLeft);
            default:
                return BorderRadius.Zero;
        }
    }

    private static void DrawMagnifiedBackdrop(
        MagnifierFlowLayer layer,
        DrawingContext context,
        BackdropCapture? backdrop,
        Rect lensRect,
        Offset focalPointOffset)
    {
        if (backdrop == null)
        {
            return;
        }

        double scale = layer.MagnificationScale;
        double absoluteScale = Math.Abs(scale);
        if (absoluteScale <= double.Epsilon)
        {
            return;
        }

        Offset focalPoint = lensRect.Center + focalPointOffset;
        var sourceSize = new Size(lensRect.Width / absoluteScale, lensRect.Height / absoluteScale);
        var sourceRect = Rect.FromLTWH(
            focalPoint.Dx - (sourceSize.Width / 2.0),
            focalPoint.Dy - (sourceSize.Height / 2.0),
            sourceSize.Width,
            sourceSize.Height);
        if (scale > 0)
        {
            context.DrawImage(backdrop.Image, sourceRect.ToAvalonia(), lensRect.ToAvalonia());
            return;
        }

        using (context.PushTransform(
                   Matrix.CreateTranslation(lensRect.Center.Dx, lensRect.Center.Dy)
                   * Matrix.CreateScale(-1, -1)
                   * Matrix.CreateTranslation(-lensRect.Center.Dx, -lensRect.Center.Dy)))
        {
            context.DrawImage(backdrop.Image, sourceRect.ToAvalonia(), lensRect.ToAvalonia());
        }
    }

    /// <summary>
    /// RawMagnifier.build's decoration sibling, in the layer's local coordinates:
    /// <c>ClipPath(clipBehavior, clipper: _NegativeClip(shape))</c> over a <c>DecoratedBox</c> of
    /// <c>ShapeDecoration(shape, shadows)</c> the size of the lens, drawn through the strict
    /// decoration painter (mask-filtered shadows, then the shape's border).
    /// </summary>
    private static void DrawMagnifierDecoration(MagnifierFlowLayer layer, DrawingContext context)
    {
        Rect rect = layer.LensRect;
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var paintingContext = new CanvasPaintingContext(canvas);
        var decoration = new ShapeDecoration(Shape: layer.Decoration.Shape, Shadows: layer.Decoration.Shadows);
        using BoxPainter painter = decoration.CreateBoxPainter();
        var configuration = new ImageConfiguration(Size: rect.Size, TextDirection: TextDirection.Ltr);

        void PaintDecoration() => painter.Paint(paintingContext, rect.TopLeft, configuration);

        if (layer.ClipBehavior == Clip.None)
        {
            PaintDecoration();
        }
        else
        {
            // _NegativeClip.getClip: everything except the inside of the shape.
            var clip = new Path { FillType = PathFillType.EvenOdd };
            clip.AddRect(ShapeDecorationPainter.LargestRect);
            clip.AddPath(layer.Decoration.Shape.GetInnerPath(rect, TextDirection.Ltr));
            paintingContext.ClipPathAndPaint(clip, layer.ClipBehavior, rect, PaintDecoration);
        }

        Picture picture = recorder.EndRecording();
        CanvasPlayback.Play(picture, context, default);
        picture.Dispose();
    }

    private static void PaintTexture(TextureFlowLayer layer, FlowPaintContext context)
    {
        EngineTexture? texture = context.State.Textures?.GetTexture(layer.TextureId);
        if (texture == null)
        {
            // An unknown texture paints nothing: the engine's "null texture" trace.
            return;
        }

        PaintTextureFrame(texture, context.Canvas, (layer.Offset & layer.Size), layer.Freeze, layer.FilterQuality);
    }

    /// <summary>
    /// Paints the texture's current frame into <paramref name="bounds"/>; a <paramref name="freeze"/>d
    /// texture keeps painting the frame it last painted.
    /// </summary>
    /// <remarks>The engine's <c>Texture::Paint</c>.</remarks>
    internal static void PaintTextureFrame(
        EngineTexture texture,
        DrawingContext context,
        Rect bounds,
        bool freeze,
        FilterQuality filterQuality)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (texture.FrameToPaint(freeze) is not { } frame || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        IImage image = frame.NativeImage;
        Size size = image.Size.ToPlumix();
        using (context.PushRenderOptions(new RenderOptions
               {
                   BitmapInterpolationMode = filterQuality switch
                   {
                       FilterQuality.None => BitmapInterpolationMode.None,
                       FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                       FilterQuality.Medium => BitmapInterpolationMode.MediumQuality,
                       _ => BitmapInterpolationMode.HighQuality,
                   },
               }))
        {
            context.DrawImage(image, (Offset.Zero & size).ToAvalonia(), bounds.ToAvalonia());
        }
    }

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

    /// <remarks>The engine's <c>PerformanceOverlayLayer::Paint</c>.</remarks>
    private static void PaintPerformanceOverlay(PerformanceOverlayFlowLayer layer, FlowPaintContext context)
    {
        const int padding = 8;
        if (layer.Options == 0)
        {
            return;
        }

        CompositorContext compositor = context.State.Compositor;
        double x = layer.Bounds.Left + padding;
        double y = layer.Bounds.Top + padding;
        double width = layer.Bounds.Width - (padding * 2);
        double height = layer.Bounds.Height / 2;
        VisualizeStopwatch(
            context.Canvas,
            compositor.RasterTime,
            x,
            y,
            width,
            height - padding,
            (layer.Options & PerformanceOverlayFlowLayer.VisualizeRasterizerStatistics) != 0,
            (layer.Options & PerformanceOverlayFlowLayer.DisplayRasterizerStatistics) != 0,
            "Raster");
        VisualizeStopwatch(
            context.Canvas,
            compositor.UiTime,
            x,
            y + height,
            width,
            height - padding,
            (layer.Options & PerformanceOverlayFlowLayer.VisualizeEngineStatistics) != 0,
            (layer.Options & PerformanceOverlayFlowLayer.DisplayEngineStatistics) != 0,
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
            stopwatch.Visualize(canvas, Rect.FromLTWH(x, y, width, height));
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
        canvas.DrawText(text, new Offset(x + labelX, y + height + labelY - text.Baseline).ToAvalonia());
    }
}
