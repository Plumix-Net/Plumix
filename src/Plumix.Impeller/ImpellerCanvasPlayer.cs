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
                case FadeMaskOp:
                    // The fade is an alpha mask until the matching restore; a plain save keeps the nesting.
                    ImpellerPaints.Unsupported("TextOverflow.fade mask");
                    builder.Save();
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
        if (layer.ColorFilter is not null || layer.ImageFilter is not null || layer.InvertColors)
        {
            ImpellerPaints.Unsupported("saveLayer filters");
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
            ImpellerPaints.Unsupported("drawImageNine");
        }

        builder.DrawTextureRect(texture, image.Source.ToImpeller(), image.Destination.ToImpeller(), sampling, paint);
    }
}
