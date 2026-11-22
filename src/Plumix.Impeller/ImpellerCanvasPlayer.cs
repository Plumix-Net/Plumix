using NImpeller;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Plays a recorded Picture's typed entries (CanvasOps.cs)
// into an Impeller display list builder, the way the engine's DisplayList dispatches its ops to Impeller.
// Stage 4 prototype: the Avalonia/Skia CanvasPlayback is the reference.

/// <summary>Plays recorded canvas entries into an Impeller display list.</summary>
internal static class ImpellerCanvasPlayer
{
    internal static void Play(Picture picture, ImpellerDisplayListBuilder builder, Offset offset)
    {
        bool translated = offset != Offset.Zero;
        if (translated)
        {
            builder.Save();
            builder.Translate((float)offset.Dx, (float)offset.Dy);
        }

        Play(picture.Ops, builder);
        if (translated)
        {
            builder.Restore();
        }
    }

    internal static void Play(IReadOnlyList<CanvasOp> ops, ImpellerDisplayListBuilder builder)
    {
        uint baseCount = builder.GetSaveCount();
        // Open TextOverflow.fade layers: the save count inside each, and the fade to draw before its restore.
        Stack<(uint SaveCount, FadeMaskOp Fade)>? fades = null;
        foreach (CanvasOp op in ops)
        {
            switch (op)
            {
                case SaveOp:
                    builder.Save();
                    break;
                case SaveLayerOp saveLayer:
                    SaveLayer(builder, saveLayer.Layer);
                    break;
                case RestoreOp:
                    if (builder.GetSaveCount() > baseCount)
                    {
                        if (fades is { Count: > 0 } && fades.Peek().SaveCount == builder.GetSaveCount())
                        {
                            DrawFade(builder, fades.Pop().Fade);
                        }

                        builder.Restore();
                    }

                    break;
                case TransformOp transform:
                    Transform(builder, transform);
                    break;
                case ClipRectOp clipRect:
                    builder.ClipRect(clipRect.Rect.ToImpeller(), clipRect.ClipOp == ClipOp.Difference
                        ? ImpellerClipOperation.kImpellerClipOperationDifference
                        : ImpellerClipOperation.kImpellerClipOperationIntersect);
                    break;
                case ClipRRectOp clipRRect:
                    builder.ClipRoundedRect(clipRRect.RRect.OuterRect.ToImpeller(), clipRRect.RRect.Radii(),
                        ImpellerClipOperation.kImpellerClipOperationIntersect);
                    break;
                case ClipPathOp clipPath:
                    builder.ClipPath(clipPath.Path.ToImpeller(), ImpellerClipOperation.kImpellerClipOperationIntersect);
                    break;
                case FadeMaskOp fade:
                    // RenderParagraph's fade: an isolated layer, faded by the gradient drawn before its restore.
                    builder.SaveLayer(fade.Bounds.ToImpeller(), null, null);
                    (fades ??= new()).Push((builder.GetSaveCount(), fade));
                    break;
                case BackendOp:
                    break;
                case FlutterTestTextOp text:
                    ImpellerFlutterTestText.Draw(builder, text);
                    break;
                case DrawPictureOp picture:
                    Play(picture.Picture, builder, picture.Offset);
                    break;
                case DrawShapeOp shape:
                    DrawShape(builder, shape);
                    break;
                case DrawHairlineOp hairline:
                    DrawHairline(builder, hairline);
                    break;
                case DrawPaintOp drawPaint:
                    builder.DrawPaint(PaintOf(drawPaint, drawPaint.Paint));
                    break;
                case DrawShadowOp shadow:
                    builder.DrawShadow(shadow.Path.ToImpeller(), shadow.Color.ToImpeller(), (float)shadow.Elevation,
                        shadow.TransparentOccluder, (float)shadow.DevicePixelRatio);
                    break;
                case DrawImageOp image:
                    DrawImage(builder, image);
                    break;
                default:
                    ImpellerPaints.Unsupported(op.GetType().Name);
                    break;
            }
        }

        while (fades is { Count: > 0 } && builder.GetSaveCount() > baseCount)
        {
            (uint saveCount, FadeMaskOp fade) = fades.Pop();
            builder.RestoreToCount(saveCount);
            DrawFade(builder, fade);
            builder.Restore();
        }

        if (builder.GetSaveCount() > baseCount)
        {
            builder.RestoreToCount(baseCount);
        }
    }

    private static ImpellerPaint PaintOf(CanvasOp op, Paint paint) =>
        (ImpellerPaint)(op.BackendState ??= ImpellerPaints.Create(paint));

    private static void SaveLayer(ImpellerDisplayListBuilder builder, CanvasLayer layer)
    {
        ImpellerPaint paint = ImpellerPaint.New() ?? throw new InvalidOperationException("No Impeller paint.");
        paint.SetColor(new ImpellerColor { Alpha = layer.Alpha / 255f, Red = 0, Green = 0, Blue = 0 });
        paint.SetBlendMode(layer.BlendMode.ToImpeller());
        if (ImpellerPaints.CreateColorFilter(layer.ColorFilter, layer.InvertColors) is { } native)
        {
            paint.SetColorFilter(native);
        }

        if (layer.ImageFilter is { } imageFilter && ImpellerLayerPainter.CreateFilter(imageFilter) is { } filter)
        {
            paint.SetImageFilter(filter);
        }

        ImpellerRect bounds = layer.Bounds is { } rect
            ? rect.ToImpeller()
            : new ImpellerRect { X = -1e9f, Y = -1e9f, Width = 2e9f, Height = 2e9f };
        builder.SaveLayer(bounds, paint, null);
    }

    private static void Transform(ImpellerDisplayListBuilder builder, TransformOp transform)
    {
        switch (transform.Kind)
        {
            case TransformKind.Translate:
                builder.Translate((float)transform.X, (float)transform.Y);
                break;
            case TransformKind.Scale:
                builder.Scale((float)transform.X, (float)transform.Y);
                break;
            case TransformKind.Rotate:
                builder.Rotate((float)(transform.X * 180.0 / Math.PI));
                break;
            case TransformKind.Matrix:
                builder.Transform(transform.Matrix!.ToImpeller());
                break;
        }
    }

    private static void DrawShape(ImpellerDisplayListBuilder builder, DrawShapeOp shape)
    {
        ImpellerPaint paint = PaintOf(shape, shape.Paint);
        switch (shape.Kind)
        {
            case CanvasShapeKind.Rect:
                builder.DrawRect(shape.Rect.ToImpeller(), paint);
                break;
            case CanvasShapeKind.RRect:
                builder.DrawRoundedRect(shape.RRect.OuterRect.ToImpeller(), shape.RRect.Radii(), paint);
                break;
            case CanvasShapeKind.DRRect:
                builder.DrawRoundedRectDifference(shape.RRect.OuterRect.ToImpeller(), shape.RRect.Radii(),
                    shape.Inner.OuterRect.ToImpeller(), shape.Inner.Radii(), paint);
                break;
            case CanvasShapeKind.Oval:
                builder.DrawOval(shape.Rect.ToImpeller(), paint);
                break;
            case CanvasShapeKind.Circle:
                builder.DrawOval(Rect.FromCircle(shape.Start, shape.Radius).ToImpeller(), paint);
                break;
            case CanvasShapeKind.Line:
                builder.DrawLine(shape.Start.ToImpeller(), shape.End.ToImpeller(), paint);
                break;
            case CanvasShapeKind.Path:
                builder.DrawPath(shape.Path!.ToImpeller(), paint);
                break;
            default:
                ImpellerPaints.Unsupported($"shape {shape.Kind}");
                break;
        }
    }

    private static void DrawHairline(ImpellerDisplayListBuilder builder, DrawHairlineOp hairline)
    {
        var paint = (ImpellerPaint)(hairline.BackendState ??= CreateHairlinePaint(hairline));
        builder.DrawLine(hairline.Start.ToImpeller(), hairline.End.ToImpeller(), paint);
    }

    private static ImpellerPaint CreateHairlinePaint(DrawHairlineOp hairline)
    {
        ImpellerPaint paint = ImpellerPaints.Solid(hairline.Color);
        paint.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        paint.SetStrokeWidth(0);
        paint.SetStrokeCap(hairline.Cap switch
        {
            StrokeCap.Round => ImpellerStrokeCap.kImpellerStrokeCapRound,
            StrokeCap.Square => ImpellerStrokeCap.kImpellerStrokeCapSquare,
            _ => ImpellerStrokeCap.kImpellerStrokeCapButt,
        });
        return paint;
    }

    private static void DrawImage(ImpellerDisplayListBuilder builder, DrawImageOp image)
    {
        ImpellerTexture? texture = image.Image.Native switch
        {
            ImpellerImage pixels => pixels.Texture,
            ImpellerTexture native => native,
            _ => null,
        };
        if (texture is null)
        {
            ImpellerPaints.Unsupported($"image {image.Image.Native.GetType().Name}");
            return;
        }

        ImpellerPaint paint = PaintOf(image, image.Paint);
        ImpellerTextureSampling sampling = image.Paint.FilterQuality.ToSampling();
        if (image.Kind == ImageDrawKind.ImageNine)
        {
            DrawImageNine(builder, texture, image, sampling, paint);
            return;
        }

        builder.DrawTextureRect(texture, image.Source.ToImpeller(), image.Destination.ToImpeller(), sampling, paint);
    }

    // Skia's drawImageNine (SkLatticeIter::Nine): the corners keep their size, the edges stretch along one axis and
    // the centre in both; corners that do not fit the destination shrink in proportion.
    // The white-to-transparent gradient Dart's RenderParagraph modulates its text with, over the fade's bounds.
    private static void DrawFade(ImpellerDisplayListBuilder builder, FadeMaskOp fade)
    {
        Offset origin = fade.Bounds.TopLeft;
        var paint = new Paint
        {
            BlendMode = BlendMode.Modulate,
            Shader = GradientShader.Linear(origin + fade.From, origin + fade.To,
                [new Color(0xFFFFFFFF), new Color(0x00FFFFFF)], [0.0, 1.0], TileMode.Clamp, null),
        };
        builder.DrawRect(fade.Bounds.ToImpeller(), ImpellerPaints.Create(paint));
    }

    private static void DrawImageNine(ImpellerDisplayListBuilder builder, ImpellerTexture texture, DrawImageOp image,
        ImpellerTextureSampling sampling, ImpellerPaint paint)
    {
        double width = image.Image.Width;
        double height = image.Image.Height;
        Rect center = image.Source;
        Rect destination = image.Destination;
        double[] sourceX = [0, center.Left, center.Right, width];
        double[] sourceY = [0, center.Top, center.Bottom, height];
        double[] destinationX = Divisions(destination.Left, destination.Right, center.Left, width - center.Right);
        double[] destinationY = Divisions(destination.Top, destination.Bottom, center.Top, height - center.Bottom);
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Rect source = Rect.FromLTRB(sourceX[column], sourceY[row], sourceX[column + 1], sourceY[row + 1]);
                Rect target = Rect.FromLTRB(destinationX[column], destinationY[row], destinationX[column + 1],
                    destinationY[row + 1]);
                if (source.Width > 0 && source.Height > 0 && target.Width > 0 && target.Height > 0)
                {
                    builder.DrawTextureRect(texture, source.ToImpeller(), target.ToImpeller(), sampling, paint);
                }
            }
        }
    }

    private static double[] Divisions(double start, double end, double first, double last)
    {
        double available = end - start;
        double fixedSize = first + last;
        double scale = fixedSize > available && fixedSize > 0 ? available / fixedSize : 1.0;
        return [start, start + (first * scale), end - (last * scale), end];
    }
}
