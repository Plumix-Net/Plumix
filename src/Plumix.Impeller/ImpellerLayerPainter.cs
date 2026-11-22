using NImpeller;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Paints the engine-side retained layer tree (FlowLayers.cs)
// into one Impeller display list, the way the engine's Impeller rasterizer walks a LayerTree. Stage 4
// prototype: colour filters, shader masks, magnifiers, textures and the performance overlay paint their
// children without the effect (reported once each).

/// <summary>Builds an Impeller display list from a composited layer tree.</summary>
internal static class ImpellerLayerPainter
{
    private static readonly ImpellerRect Unbounded = new() { X = -1e9f, Y = -1e9f, Width = 2e9f, Height = 2e9f };

    /// <summary>The layer tree as one display list of <paramref name="width"/> x <paramref name="height"/>.</summary>
    internal static ImpellerDisplayList Build(ContainerFlowLayer root, int width, int height)
    {
        using ImpellerDisplayListBuilder builder = ImpellerDisplayListBuilder.New(
                new ImpellerRect { X = 0, Y = 0, Width = width, Height = height })
            ?? throw new InvalidOperationException("Impeller could not create a display list builder.");
        Paint(builder, root);
        return builder.CreateDisplayListNew()
            ?? throw new InvalidOperationException("Impeller could not build a display list.");
    }

    internal static void Paint(ImpellerDisplayListBuilder builder, FlowLayer layer)
    {
        switch (layer)
        {
            case DisplayListFlowLayer picture:
                ImpellerCanvasPlayer.Play(picture.Picture, builder, picture.Offset);
                break;
            case TransformFlowLayer transform:
                builder.Save();
                builder.Transform(transform.Transform.ToImpeller());
                PaintChildren(builder, transform);
                builder.Restore();
                break;
            case OpacityFlowLayer opacity:
                builder.Save();
                builder.Translate((float)opacity.Offset.Dx, (float)opacity.Offset.Dy);
                if (opacity.Alpha < 255)
                {
                    using ImpellerPaint paint = AlphaPaint(opacity.Alpha);
                    builder.SaveLayer(Unbounded, paint, null);
                    PaintChildren(builder, opacity);
                    builder.Restore();
                }
                else
                {
                    PaintChildren(builder, opacity);
                }

                builder.Restore();
                break;
            case ClipRectFlowLayer clip:
                Clipped(builder, clip, clip.ClipBehavior, b => b.ClipRect(clip.ClipRect.ToImpeller(), Intersect));
                break;
            case ClipRRectFlowLayer clip:
                Clipped(builder, clip, clip.ClipBehavior,
                    b => b.ClipRoundedRect(clip.ClipRRect.OuterRect.ToImpeller(), clip.ClipRRect.Radii(), Intersect));
                break;
            case ClipRSuperellipseFlowLayer clip:
                ImpellerPath superellipse = clip.ClipRSuperellipse.ToPath().ToImpeller();
                Clipped(builder, clip, clip.ClipBehavior, b => b.ClipPath(superellipse, Intersect));
                break;
            case ClipPathFlowLayer clip:
                Clipped(builder, clip, clip.ClipBehavior, b => b.ClipPath(clip.ClipPath.ToImpeller(), Intersect));
                break;
            case ImageFilterFlowLayer filtered:
                builder.Save();
                builder.Translate((float)filtered.Offset.Dx, (float)filtered.Offset.Dy);
                if (CreateFilter(filtered.Filter) is { } imageFilter)
                {
                    using ImpellerPaint paint = ImpellerPaint.New()!;
                    paint.SetImageFilter(imageFilter);
                    builder.SaveLayer(filtered.Bounds?.ToImpeller() ?? Unbounded, paint, null);
                    PaintChildren(builder, filtered);
                    builder.Restore();
                }
                else
                {
                    PaintChildren(builder, filtered);
                }

                builder.Restore();
                break;
            case BackdropFilterFlowLayer backdrop:
                if (CreateFilter(backdrop.Filter) is { } backdropFilter)
                {
                    using ImpellerPaint paint = ImpellerPaint.New()!;
                    paint.SetBlendMode(backdrop.BlendMode.ToImpeller());
                    builder.SaveLayer(Unbounded, paint, backdropFilter);
                    PaintChildren(builder, backdrop);
                    builder.Restore();
                }
                else
                {
                    PaintChildren(builder, backdrop);
                }

                break;
            case ColorFilterFlowLayer colorFilter:
                if (ImpellerPaints.CreateColorFilter(colorFilter.Filter) is { } nativeFilter)
                {
                    using ImpellerPaint paint = ImpellerPaint.New()!;
                    paint.SetColorFilter(nativeFilter);
                    builder.SaveLayer(colorFilter.Bounds?.ToImpeller() ?? Unbounded, paint, null);
                    PaintChildren(builder, colorFilter);
                    builder.Restore();
                }
                else
                {
                    PaintChildren(builder, colorFilter);
                }

                break;
            case ShaderMaskFlowLayer mask:
                // ShaderMaskLayer: the children in a layer, then the shader drawn over the mask rect with the
                // mask's blend mode (dstIn and friends) before the layer composites.
                builder.SaveLayer(Unbounded, null, null);
                PaintChildren(builder, mask);
                using (ImpellerPaint maskPaint = ImpellerPaints.Create(new Paint
                       {
                           Shader = mask.Shader, BlendMode = mask.BlendMode, FilterQuality = mask.FilterQuality,
                       }))
                {
                    builder.DrawRect(mask.MaskRect.ToImpeller(), maskPaint);
                }

                builder.Restore();
                break;
            case ContainerFlowLayer container:
                if (container is MagnifierFlowLayer)
                {
                    ImpellerPaints.Unsupported(container.GetType().Name);
                }

                PaintChildren(builder, container);
                break;
            case PlatformViewFlowLayer:
                break;
            default:
                ImpellerPaints.Unsupported(layer.GetType().Name);
                break;
        }
    }

    private const ImpellerClipOperation Intersect = ImpellerClipOperation.kImpellerClipOperationIntersect;

    private static void PaintChildren(ImpellerDisplayListBuilder builder, ContainerFlowLayer container)
    {
        foreach (FlowLayer child in container.Layers)
        {
            Paint(builder, child);
        }
    }

    private static void Clipped(ImpellerDisplayListBuilder builder, ContainerFlowLayer layer, Clip behavior,
        Action<ImpellerDisplayListBuilder> clip)
    {
        builder.Save();
        if (behavior != Clip.None)
        {
            clip(builder);
        }

        if (behavior == Clip.AntiAliasWithSaveLayer)
        {
            using ImpellerPaint paint = AlphaPaint(255);
            builder.SaveLayer(Unbounded, paint, null);
            PaintChildren(builder, layer);
            builder.Restore();
        }
        else
        {
            PaintChildren(builder, layer);
        }

        builder.Restore();
    }

    private static ImpellerPaint AlphaPaint(int alpha)
    {
        ImpellerPaint paint = ImpellerPaint.New() ?? throw new InvalidOperationException("No Impeller paint.");
        paint.SetColor(new ImpellerColor { Alpha = alpha / 255f, Red = 0, Green = 0, Blue = 0 });
        return paint;
    }

    internal static ImpellerImageFilter? CreateFilter(ImageFilter filter)
    {
        switch (filter)
        {
            case ImageFilter.Blur blur:
                return ImpellerImageFilter.CreateBlurNew((float)blur.SigmaX, (float)blur.SigmaY,
                    blur.TileMode.ToImpeller());
            case ImageFilter.Dilate dilate:
                return ImpellerImageFilter.CreateDilateNew((float)dilate.RadiusX, (float)dilate.RadiusY);
            case ImageFilter.Erode erode:
                return ImpellerImageFilter.CreateErodeNew((float)erode.RadiusX, (float)erode.RadiusY);
            case ImageFilter.Matrix matrix:
            {
                // dart:ui's 16 column-major values, which Matrix4x4's fields hold in the same order.
                IReadOnlyList<double> m = matrix.Values;
                var transform = new System.Numerics.Matrix4x4(
                    (float)m[0], (float)m[1], (float)m[2], (float)m[3],
                    (float)m[4], (float)m[5], (float)m[6], (float)m[7],
                    (float)m[8], (float)m[9], (float)m[10], (float)m[11],
                    (float)m[12], (float)m[13], (float)m[14], (float)m[15]);
                return ImpellerImageFilter.CreateMatrixNew(transform, matrix.FilterQuality.ToSampling());
            }

            case ImageFilter.Compose compose:
                ImpellerImageFilter? outer = CreateFilter(compose.Outer);
                ImpellerImageFilter? inner = CreateFilter(compose.Inner);
                return outer is null ? inner : inner is null ? outer : outer.CreateComposeNew(inner);
            default:
                ImpellerPaints.Unsupported($"image filter {filter.GetType().Name}");
                return null;
        }
    }
}
