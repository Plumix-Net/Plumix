using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only host infrastructure (Plumix.Avalonia): the Avalonia/Skia backend that plays a Picture's typed
// entries (src/Plumix/UI/CanvasOps.cs) onto an Avalonia DrawingContext, the counterpart of the engine's
// DlSkCanvasDispatcher. Everything a draw needs from Avalonia or Skia — brushes, pens, geometry,
// rasterized layers and mask-filter draws — is built here on first playback and kept on the entry for
// the next one.

/// <summary>Plays recorded canvas entries onto an Avalonia drawing context.</summary>
internal static class CanvasPlayback
{
    // Dart's Rect.largest: what drawPaint fills, since Avalonia exposes no clip-bounds query.
    private static readonly Rect DrawPaintBounds = Rect.FromLTWH(-1.0e9, -1.0e9, 2.0e9, 2.0e9);

    /// <summary>
    /// Replays <paramref name="ops"/>, translated by <paramref name="offset"/>, rasterizing any layer that
    /// needs it at <paramref name="deviceScale"/> device pixels per picture unit.
    /// </summary>
    internal static void Play(IReadOnlyList<CanvasOp> ops, DrawingContext context, Offset offset, double deviceScale)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (ops.Count == 0)
        {
            return;
        }

        DrawingContext.PushedState? translation = null;
        try
        {
            if (offset.Dx != 0.0 || offset.Dy != 0.0)
            {
                translation = context.PushTransform(Matrix.CreateTranslation(offset.Dx, offset.Dy));
            }

            PlayRange(ops, context, 0, ops.Count, deviceScale);
        }
        finally
        {
            translation?.Dispose();
        }
    }

    /// <summary>
    /// Replays <paramref name="picture"/>, translated by <paramref name="offset"/>, rasterizing any layer that
    /// needs it at the implicit view's device pixel ratio.
    /// </summary>
    internal static void Play(Picture picture, DrawingContext context, Offset offset) =>
        Play(picture.Ops, context, offset, PaintRasterizer.RasterScale);

    // Replays ops [start, end): a balanced range, so every save in it is restored in it (or at its end).
    private static void PlayRange(IReadOnlyList<CanvasOp> ops, DrawingContext context, int start, int end,
        double deviceScale)
    {
        var pushed = new List<DrawingContext.PushedState>();
        var saves = new Stack<int>();
        try
        {
            for (int index = start; index < end; index++)
            {
                switch (ops[index])
                {
                    case SaveOp:
                        saves.Push(pushed.Count);
                        break;
                    case SaveLayerOp saveLayer:
                        CanvasLayer layer = saveLayer.Layer;
                        int restore = Math.Min(layer.RestoreIndex < 0 ? end : layer.RestoreIndex, end);
                        if (layer.NeedsRaster
                            && TryDrawRasterizedLayer(ops, context, saveLayer, index + 1, restore, deviceScale))
                        {
                            // The group, and its restore, were composited as one image.
                            index = restore;
                            break;
                        }

                        saves.Push(pushed.Count);
                        pushed.Add(PushIsolatedGroup(context, layer));
                        break;
                    case RestoreOp:
                        Unwind(pushed, saves.Count > 0 ? saves.Pop() : 0);
                        break;
                    case TransformOp transform:
                        pushed.Add(context.PushTransform(ToMatrix(transform)));
                        break;
                    case ClipRectOp clipRect:
                        pushed.Add(PushClipRect(context, clipRect));
                        break;
                    case ClipRRectOp clipRRect:
                        pushed.Add(SceneRasterizer.PushRoundedRectClip(context, clipRRect.RRect));
                        break;
                    case ClipPathOp clipPath:
                        var clipGeometry = (Geometry)(clipPath.BackendState ??= clipPath.Path.ToGeometry());
                        pushed.Add(context.PushGeometryClip(clipGeometry));
                        break;
                    case FadeMaskOp fade:
                        pushed.Add(context.PushOpacityMask(FadeBrush(fade), fade.Bounds.ToAvalonia()));
                        break;
                    case BackendOp { Payload: Action<DrawingContext> draw }:
                        draw(context);
                        break;
                    case BackendOp nothing when ReferenceEquals(nothing.Payload, BackendOp.NothingToDraw):
                        break;
                    case FlutterTestTextOp text:
                        DrawFlutterTestText(context, text);
                        break;
                    case DrawPictureOp picture:
                        Play(picture.Picture, context, picture.Offset);
                        break;
                    case DrawShapeOp shape:
                        DrawShape(context, shape);
                        break;
                    case DrawHairlineOp hairline:
                        context.Custom((HairlineDraw)(hairline.BackendState ??= new HairlineDraw(
                            hairline.Start, hairline.End, hairline.Color, hairline.IsAntiAlias, hairline.Cap)));
                        break;
                    case DrawPaintOp drawPaint:
                        DrawPaint(context, drawPaint);
                        break;
                    case DrawShadowOp shadow:
                        context.Custom((ShadowDraw)(shadow.BackendState ??= new ShadowDraw(
                            shadow.Path,
                            shadow.Color,
                            shadow.Elevation,
                            shadow.TransparentOccluder,
                            shadow.DevicePixelRatio)));
                        break;
                    case DrawImageOp image:
                        DrawImage(context, image);
                        break;
                    default:
                        throw new NotSupportedException($"Unknown canvas entry {ops[index].GetType().Name}.");
                }
            }
        }
        finally
        {
            Unwind(pushed, 0);
        }
    }

    // A headless paragraph's steps in order: a FlutterTestGlyphDraw per glyph batch, over the op's bounds, and
    // each shader-painted glyph as its FlutterTest outline filled with the shader's brush. The draws are built
    // on first playback and kept on the entry.
    private static void DrawFlutterTestText(DrawingContext context, FlutterTestTextOp text)
    {
        var draws = (Action<DrawingContext>[])(text.BackendState ??= text.Steps
            .Select(step => FlutterTestTextStepDraw(step, text.Bounds))
            .ToArray());
        foreach (Action<DrawingContext> draw in draws)
        {
            draw(context);
        }
    }

    private static Action<DrawingContext> FlutterTestTextStepDraw(FlutterTestText.Step step, Rect bounds) =>
        step switch
        {
            FlutterTestText.GlyphBatch batch => CustomDraw(new FlutterTestGlyphDraw(batch.Operations, bounds)),
            FlutterTestText.ShaderGlyph glyph => GeometryDraw(
                FlutterTestGlyphDraw.FlutterTestGlyph(
                    glyph.Shape, glyph.X, glyph.Width, glyph.Baseline, glyph.FontSize),
                SkiaShaders.ToBrush(glyph.Shader)),
        };

    private static Action<DrawingContext> CustomDraw(FlutterTestGlyphDraw draw) => context => context.Custom(draw);

    private static Action<DrawingContext> GeometryDraw(Geometry shape, IBrush brush) =>
        context => context.DrawGeometry(brush, null, shape);

    // The white-to-transparent gradient Dart builds for TextOverflow.fade, relative to the fade's box.
    private static IBrush FadeBrush(FadeMaskOp fade)
    {
        if (fade.BackendState is IBrush cached)
        {
            return cached;
        }

        double width = Math.Max(fade.Size.Width, double.Epsilon);
        double height = Math.Max(fade.Size.Height, double.Epsilon);
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(fade.From.Dx / width, fade.From.Dy / height, RelativeUnit.Relative),
            EndPoint = new RelativePoint(fade.To.Dx / width, fade.To.Dy / height, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(new Color(0xFFFFFFFF).ToAvalonia(), 0),
                new GradientStop(new Color(0x00FFFFFF).ToAvalonia(), 1),
            },
        };
        fade.BackendState = brush;
        return brush;
    }

    private static Matrix ToMatrix(TransformOp transform)
    {
        if (transform.BackendState is Matrix cached)
        {
            return cached;
        }

        Matrix matrix = transform.Kind switch
        {
            TransformKind.Translate => Matrix.CreateTranslation(transform.X, transform.Y),
            TransformKind.Scale => Matrix.CreateScale(transform.X, transform.Y),
            TransformKind.Rotate => Matrix.CreateRotation(transform.X),
            _ => transform.Matrix!.ToAvaloniaMatrix(),
        };
        transform.BackendState = matrix;
        return matrix;
    }

    private static DrawingContext.PushedState PushClipRect(DrawingContext context, ClipRectOp clip)
    {
        if (clip.ClipOp == ClipOp.Intersect)
        {
            return context.PushClip(clip.Rect.ToAvalonia());
        }

        // A difference clip keeps everything but the rect.
        var difference = (Geometry)(clip.BackendState ??= new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(Canvas.LargestRect.ToAvalonia()),
            new RectangleGeometry(clip.Rect.ToAvalonia())));
        return context.PushGeometryClip(difference);
    }

    // An alpha-only layer: an opacity mask of the paint's alpha opens an isolated offscreen group on the
    // backend (Skia's saveLayer), which is composited with that alpha when the group is popped.
    private static DrawingContext.PushedState PushIsolatedGroup(DrawingContext context, CanvasLayer layer)
    {
        var mask = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(
            Avalonia.Media.Color.FromArgb(layer.Alpha, 0xFF, 0xFF, 0xFF));
        return context.PushOpacityMask(mask, ClampLayerBounds(layer.Bounds).ToAvalonia());
    }

    // Avalonia needs finite group bounds; an unbounded layer is bounded by the largest finite rect instead.
    private static Rect ClampLayerBounds(Rect? bounds)
    {
        Rect largest = Canvas.LargestRect;
        if (bounds is not { } rect || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height))
        {
            return largest;
        }

        return rect.Intersect(largest);
    }

    /// <summary>The rasterized group of a save layer and the scale it was rasterized at.</summary>
    private sealed class LayerRaster
    {
        internal (WriteableBitmap? Bitmap, Rect Destination, double Scale)? Raster { get; set; }

        /// <summary>Whether rasterizing failed once (no raster backend), so playback falls back for good.</summary>
        internal bool Unavailable { get; set; }
    }

    private static bool TryDrawRasterizedLayer(
        IReadOnlyList<CanvasOp> ops,
        DrawingContext context,
        SaveLayerOp saveLayer,
        int start,
        int end,
        double deviceScale)
    {
        CanvasLayer layer = saveLayer.Layer;
        var state = (LayerRaster)(saveLayer.BackendState ??= new LayerRaster());
        if (state.Unavailable)
        {
            return false;
        }

        double scale = Math.Clamp(deviceScale * layer.Transform.GetMaxScaleOnAxis(), 0.25, 8.0);
        if (state.Raster is not { } raster || raster.Scale != scale)
        {
            if (layer.Bounds is not { } bounds || !PaintRasterizer.CanRasterize(bounds))
            {
                return false;
            }

            try
            {
                (WriteableBitmap? bitmap, Rect destination) = FilterLayerRasterizer.RasterizeSaveLayer(
                    drawingContext => PlayRange(ops, drawingContext, start, end, deviceScale),
                    bounds,
                    scale,
                    layer);
                raster = (bitmap, destination, scale);
                state.Raster = raster;
            }
            catch (InvalidOperationException)
            {
                state.Unavailable = true;
                return false;
            }
            catch (NotSupportedException)
            {
                state.Unavailable = true;
                return false;
            }
        }

        if (raster.Bitmap is { } image)
        {
            FilterLayerRasterizer.DrawSaveLayer(context, image, raster.Destination, layer.BlendMode);
        }

        return true;
    }

    private static void Unwind(List<DrawingContext.PushedState> pushed, int target)
    {
        for (int index = pushed.Count - 1; index >= target; index--)
        {
            pushed[index].Dispose();
            pushed.RemoveAt(index);
        }
    }

    /// <summary>The paint's backend brush: its shader, or a brush of its colour.</summary>
    internal static IBrush BrushOf(Paint paint) =>
        paint.Shader is { } shader ? SkiaShaders.ToBrush(shader) : new SolidColorBrush(paint.Color.ToAvalonia());

    internal static Pen CreatePen(Paint paint, IBrush brush) => new(
        brush,
        paint.StrokeWidth,
        lineCap: paint.StrokeCap switch
        {
            StrokeCap.Round => PenLineCap.Round,
            StrokeCap.Square => PenLineCap.Square,
            _ => PenLineCap.Flat,
        },
        lineJoin: paint.StrokeJoin switch
        {
            StrokeJoin.Round => PenLineJoin.Round,
            StrokeJoin.Bevel => PenLineJoin.Bevel,
            _ => PenLineJoin.Miter,
        },
        miterLimit: paint.StrokeMiterLimit);

    private static void DrawPaint(DrawingContext context, DrawPaintOp op)
    {
        var brush = (IBrush)(op.BackendState ??= BrushOf(op.Paint));
        if (brush is INativeShader shader)
        {
            ShaderDrawing.Fill(context, shader, DrawPaintBounds, op.Paint, fillCanvas: true);
            return;
        }

        ShaderDrawing.DrawRectangle(context, brush, null, DrawPaintBounds);
    }

    /// <summary>What a shape draw built on its first playback.</summary>
    private sealed class ShapeState
    {
        internal required IBrush Brush { get; init; }

        internal IBrush? Fill { get; init; }

        internal IPen? Pen { get; init; }

        internal INativeShader? Native { get; init; }

        internal Path? NativePath { get; init; }

        internal Geometry? Geometry { get; set; }

        internal RasterizedShapeDraw? Rasterized { get; set; }
    }

    /// <remarks>
    /// A plain paint draws the shape's backend geometry with the paint's brush or pen; a solid colour or
    /// engine shader on a curved shape goes to Skia as the exact path. A paint with a
    /// <see cref="MaskFilter"/> or a blend mode other than <see cref="BlendMode.SourceOver"/> goes through
    /// <see cref="RasterizedShapeDraw"/>, the raster backend for both (Avalonia's drawing context has
    /// neither); where no raster backend exists the shape is drawn plainly, with a Gaussian-ring blur
    /// approximation.
    /// </remarks>
    private static void DrawShape(DrawingContext context, DrawShapeOp op)
    {
        Paint paint = op.Paint;
        var state = (ShapeState?)op.BackendState;
        if (state is null)
        {
            IBrush brush = BrushOf(paint);
            bool stroke = paint.Style == PaintingStyle.Stroke;
            INativeShader? native = null;
            Path? nativePath = null;
            if (paint.MaskFilter is null && paint.BlendMode == BlendMode.SourceOver)
            {
                native = brush as INativeShader
                    ?? (op.NativeSolid && paint.Shader is null ? new SolidColorNativeShader(paint.Color) : null);
                nativePath = native is null ? null : op.ToPath();
            }

            state = new ShapeState
            {
                Brush = brush,
                Pen = stroke ? CreatePen(paint, brush) : null,
                Fill = stroke ? null : brush,
                Native = native,
                NativePath = nativePath,
            };
            op.BackendState = state;
        }

        if (state.Native is { } shader && state.NativePath is { } path)
        {
            ShaderDrawing.DrawPath(context, shader, path, paint);
            return;
        }

        if (paint.MaskFilter is null && paint.BlendMode == BlendMode.SourceOver)
        {
            DrawPlain(context, op, state);
            return;
        }

        state.Rasterized ??= new RasterizedShapeDraw(
            () => GeometryOf(op, state),
            state.Brush,
            paint.Color,
            state.Pen,
            paint.MaskFilter,
            paint.BlendMode,
            paint.IsAntiAlias,
            drawingContext =>
            {
                if (paint.MaskFilter is not { } maskFilter || paint.Style == PaintingStyle.Stroke)
                {
                    DrawPlain(drawingContext, op, state);
                    return;
                }

                DrawBlurApproximation(drawingContext, GeometryOf(op, state), paint.Color, maskFilter,
                    plainContext => DrawPlain(plainContext, op, state));
            });
        state.Rasterized.Draw(context);
    }

    private static void DrawPlain(DrawingContext context, DrawShapeOp op, ShapeState state)
    {
        Paint paint = op.Paint;
        using DrawingContext.PushedState alpha = state.Brush is INativeShader
            ? context.PushOpacity(paint.Color.A) : default;
        using DrawingContext.PushedState edge = context.PushRenderOptions(new RenderOptions
        {
            EdgeMode = paint.IsAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
        });
        switch (op.Kind)
        {
            case CanvasShapeKind.Rect:
                ShaderDrawing.DrawRectangle(context, state.Fill, state.Pen, op.Rect);
                break;
            case CanvasShapeKind.Oval:
                ShaderDrawing.DrawEllipse(
                    context, state.Fill, state.Pen, op.Rect.Center, op.Rect.Width / 2.0, op.Rect.Height / 2.0);
                break;
            case CanvasShapeKind.Circle:
                ShaderDrawing.DrawEllipse(context, state.Fill, state.Pen, op.Start, op.Radius, op.Radius);
                break;
            case CanvasShapeKind.Line:
                ShaderDrawing.DrawLine(context, state.Pen!, op.Start, op.End);
                break;
            default:
                ShaderDrawing.DrawGeometry(context, state.Fill, state.Pen, GeometryOf(op, state));
                break;
        }
    }

    // The shape as Avalonia geometry, for a plain draw and for the raster backend's coverage mask.
    private static Geometry GeometryOf(DrawShapeOp op, ShapeState state) => state.Geometry ??= op.Kind switch
    {
        CanvasShapeKind.Rect => new RectangleGeometry(op.Rect.ToAvalonia()),
        CanvasShapeKind.Oval => new EllipseGeometry(op.Rect.ToAvalonia()),
        CanvasShapeKind.Circle => new EllipseGeometry(op.CircleBounds.ToAvalonia()),
        CanvasShapeKind.Line => new LineGeometry(op.Start.ToAvalonia(), op.End.ToAvalonia()),
        CanvasShapeKind.RRect => op.RRect.ToPath().ToGeometry(),
        CanvasShapeKind.DRRect => new CombinedGeometry(
            GeometryCombineMode.Exclude,
            op.RRect.ToPath().ToGeometry(),
            op.Inner.ToPath().ToGeometry()),
        CanvasShapeKind.Path => op.Path!.ToGeometry(),
        _ => (Geometry)op.BackendShape!,
    };

    // The no-raster-backend stand-in for a blur mask filter: concentric strokes sampled from one Gaussian
    // falloff keep the contour exact; the shape itself is filled last unless the style is outer.
    private static void DrawBlurApproximation(
        DrawingContext context,
        Geometry geometry,
        Color color,
        MaskFilter maskFilter,
        Action<DrawingContext> drawShape)
    {
        double blurSigma = maskFilter.Sigma;
        if (blurSigma > 0.0 && maskFilter.Style != BlurStyle.Inner)
        {
            double outerRadius = blurSigma * 3.0;
            int steps = Math.Max(2, (int)Math.Ceiling(outerRadius));
            double previousOpacity = 0.0;
            for (int step = 0; step < steps; step++)
            {
                double radius = outerRadius * (steps - step) / steps;
                double targetOpacity = Math.Exp(-(radius * radius) / (2.0 * blurSigma * blurSigma));
                double layerOpacity = 1.0 - ((1.0 - targetOpacity) / (1.0 - previousOpacity));
                previousOpacity = targetOpacity;
                byte layerAlpha = (byte)Math.Clamp(
                    (int)Math.Round(color.Alpha * layerOpacity),
                    0,
                    byte.MaxValue);
                if (layerAlpha > 0)
                {
                    Color layerColor = Color.FromARGB(layerAlpha, color.Red, color.Green, color.Blue);
                    ShaderDrawing.DrawGeometry(context, null,
                        new Pen(new SolidColorBrush(layerColor.ToAvalonia()), radius * 2.0), geometry);
                }
            }
        }

        if (maskFilter.Style != BlurStyle.Outer)
        {
            drawShape(context);
        }
    }

    private static void DrawImage(DrawingContext context, DrawImageOp op)
    {
        Paint paint = op.Paint;
        double opacity = paint.Color.Opacity;
        BitmapBlendingMode blendMode = FilterLayerRasterizer.ToBitmapBlendingMode(paint.BlendMode);
        if (opacity <= 0.0 && blendMode == BitmapBlendingMode.SourceOver)
        {
            return;
        }

        IImage image = op.Image.NativeImage;
        IImage source = paint.ColorFilter is null && !paint.InvertColors
            ? image
            : FilteredImages.Get(image, paint.ColorFilter, paint.InvertColors);
        DrawingContext.PushedState? alpha = opacity < 1.0 ? context.PushOpacity(opacity) : null;
        try
        {
            using DrawingContext.PushedState options = context.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = paint.FilterQuality switch
                {
                    FilterQuality.None => BitmapInterpolationMode.None,
                    FilterQuality.Low => BitmapInterpolationMode.LowQuality,
                    FilterQuality.High => BitmapInterpolationMode.HighQuality,
                    _ => BitmapInterpolationMode.MediumQuality,
                },
                EdgeMode = paint.IsAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
                BitmapBlendingMode = blendMode,
            });
            Rect dst = op.Destination;
            switch (op.Kind)
            {
                case ImageDrawKind.Image:
                    context.DrawImage(
                        source,
                        (Offset.Zero & source.Size.ToPlumix()).ToAvalonia(),
                        dst.ToAvalonia());
                    break;
                case ImageDrawKind.ImageRect:
                    Rect src = op.Source;
                    if (src.Width <= 0.0 || src.Height <= 0.0 || dst.Width <= 0.0 || dst.Height <= 0.0)
                    {
                        return;
                    }

                    context.DrawImage(source, src.ToAvalonia(), dst.ToAvalonia());
                    break;
                default:
                    foreach ((Rect patchSource, Rect patchDestination) in Canvas.NinePatches(
                                 Canvas.ImageSize(op.Image), op.Source, dst))
                    {
                        context.DrawImage(source, patchSource.ToAvalonia(), patchDestination.ToAvalonia());
                    }

                    break;
            }
        }
        finally
        {
            alpha?.Dispose();
        }
    }
}
