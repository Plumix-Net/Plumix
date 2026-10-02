using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only infrastructure: the raster backend behind dart:ui's `Paint.maskFilter` and `Paint.blendMode`
// for shape draws. Avalonia's DrawingContext has neither, so a shape drawn with a mask filter or a
// non-srcOver blend mode is rasterized into a coverage mask, blurred and merged per `BlurStyle` the way
// Skia's `SkBlurMaskFilter` does (`SkBlurMask::BoxBlur`'s normal/solid/outer/inner merges), coloured
// with the paint's colour or shader and composited as a bitmap under the paint's blend mode.

/// <summary>A shape draw that needs <see cref="PaintRasterizer"/>: everything the canvas captured.</summary>
internal sealed class RasterizedShapeDraw
{
    private readonly Func<Geometry> _geometrySource;
    private readonly IBrush _brush;
    private readonly Color _color;
    private readonly IPen? _pen;
    private readonly MaskFilter? _maskFilter;
    private readonly BlendMode _blendMode;
    private readonly bool _isAntiAlias;
    private readonly Action<DrawingContext> _fallback;
    private WriteableBitmap? _bitmap;
    private Rect _bitmapBounds;
    private double _bitmapScale;
    private bool _rasterUnavailable;

    internal RasterizedShapeDraw(
        Func<Geometry> geometrySource,
        IBrush brush,
        Color color,
        IPen? pen,
        MaskFilter? maskFilter,
        BlendMode blendMode,
        bool isAntiAlias,
        Action<DrawingContext> fallback)
    {
        _geometrySource = geometrySource;
        _brush = brush;
        _color = color;
        _pen = pen;
        _maskFilter = maskFilter;
        _blendMode = blendMode;
        _isAntiAlias = isAntiAlias;
        _fallback = fallback;
    }

    /// <summary>Plays the draw back, rasterizing it on first use and reusing the bitmap afterwards.</summary>
    internal void Draw(DrawingContext context)
    {
        double scale = PaintRasterizer.RasterScale;
        if (!_rasterUnavailable && (_bitmap is null || _bitmapScale != scale))
        {
            _bitmap?.Dispose();
            _bitmap = null;
            try
            {
                (_bitmap, _bitmapBounds) = Rasterize(scale);
                _bitmapScale = scale;
            }
            catch (InvalidOperationException)
            {
                _rasterUnavailable = true;
            }
            catch (NotSupportedException)
            {
                _rasterUnavailable = true;
            }
        }

        if (_bitmap is null)
        {
            if (_rasterUnavailable)
            {
                _fallback(context);
            }

            return;
        }

        PaintRasterizer.DrawBitmap(context, _bitmap, _bitmapBounds, _blendMode);
    }

    private (WriteableBitmap? Bitmap, Rect Bounds) Rasterize(double scale)
    {
        Geometry geometry = _geometrySource();
        Rect shapeBounds = _pen is null ? geometry.Bounds : geometry.GetRenderBounds(_pen);
        double sigma = _maskFilter?.Sigma ?? 0.0;
        // Skia pads the mask by the blur's 3-sigma extent; one more pixel keeps the anti-aliased edge.
        double padding = PaintRasterizer.BlurExtent(sigma) + 1.0;
        Rect bounds = shapeBounds.Inflate(padding);
        if (!PaintRasterizer.CanRasterize(bounds))
        {
            return (null, default);
        }

        int width = Math.Max(1, (int)Math.Ceiling(bounds.Width * scale));
        int height = Math.Max(1, (int)Math.Ceiling(bounds.Height * scale));
        if ((long)width * height > PaintRasterizer.MaxRasterPixels)
        {
            throw new NotSupportedException("The shape is too large to rasterize.");
        }

        IPen? coveragePen = _pen is null
            ? null
            : new Pen(Brushes.White, _pen.Thickness, _pen.DashStyle, _pen.LineCap, _pen.LineJoin, _pen.MiterLimit);
        byte[] coverage = PaintRasterizer.RasterizeAlpha(
            width,
            height,
            bounds.Position,
            scale,
            drawingContext =>
            {
                using DrawingContext.PushedState edge = drawingContext.PushRenderOptions(new RenderOptions
                {
                    EdgeMode = _isAntiAlias ? EdgeMode.Antialias : EdgeMode.Aliased,
                });
                drawingContext.DrawGeometry(coveragePen is null ? Brushes.White : null, coveragePen, geometry);
            });
        byte[] mask = _maskFilter is { } maskFilter
            ? PaintRasterizer.ApplyBlurStyle(
                coverage,
                PaintRasterizer.BlurAlpha(coverage, width, height, maskFilter.Sigma * scale),
                maskFilter.Style)
            : coverage;
        byte[] pixels = _brush is ISolidColorBrush
            ? PaintRasterizer.ColorizeMask(mask, _color)
            : PaintRasterizer.ShadeMask(
                mask,
                PaintRasterizer.RasterizeBrush(width, height, bounds, scale, _brush));
        return (PaintRasterizer.CreateBitmap(pixels, width, height), new Rect(bounds.Position, new Size(
            width / scale,
            height / scale)));
    }
}

/// <summary>The pixel work behind <see cref="RasterizedShapeDraw"/>; the pure parts are testable.</summary>
internal static class PaintRasterizer
{
    private static readonly Vector Dpi = new(96.0, 96.0);

    // A shape this large (in device pixels) is not rasterized; the canvas falls back to its backend draw.
    internal const long MaxRasterPixels = 16L * 1024 * 1024;

    /// <summary>
    /// The device pixels per logical pixel a mask is rasterized at: the implicit view's ratio, so a
    /// blurred edge is as sharp as the backend's own drawing.
    /// </summary>
    internal static double RasterScale
    {
        get
        {
            double ratio = PlatformDispatcher.Instance.ImplicitView?.DevicePixelRatio ?? 1.0;
            return double.IsFinite(ratio) && ratio > 0.0 ? Math.Clamp(ratio, 1.0, 4.0) : 1.0;
        }
    }

    /// <summary>How far a blur of <paramref name="sigma"/> spreads past the shape (Skia's 3-sigma pad).</summary>
    internal static double BlurExtent(double sigma) => sigma > 0.0 ? Math.Ceiling(sigma * 3.0) : 0.0;

    internal static bool CanRasterize(Rect bounds)
    {
        return bounds.Width > 0.0
               && bounds.Height > 0.0
               && double.IsFinite(bounds.X)
               && double.IsFinite(bounds.Y)
               && double.IsFinite(bounds.Width)
               && double.IsFinite(bounds.Height);
    }

    /// <summary>
    /// Blurs an 8-bit coverage mask with a Gaussian of <paramref name="sigma"/> device pixels; the mask
    /// keeps its size, so callers pad it by <see cref="BlurExtent"/> first.
    /// </summary>
    /// <remarks>
    /// Like Skia's <c>SkMaskBlurFilter</c>, a small sigma uses the exact Gaussian kernel and a larger one
    /// the three-box approximation (box sizes from the SVG <c>feGaussianBlur</c> formula Skia shares).
    /// </remarks>
    internal static byte[] BlurAlpha(IReadOnlyList<byte> alpha, int width, int height, double sigma)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        if (alpha.Count != checked(width * height))
        {
            throw new ArgumentException("The mask must contain width * height values.", nameof(alpha));
        }

        double[] values = new double[alpha.Count];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = alpha[index];
        }

        if (sigma > 0.0 && width > 0 && height > 0)
        {
            double[] scratch = new double[Math.Max(width, height)];
            double[] line = new double[Math.Max(width, height)];
            for (int y = 0; y < height; y++)
            {
                BlurLine(values, y * width, 1, width, sigma, line, scratch);
            }

            for (int x = 0; x < width; x++)
            {
                BlurLine(values, x, width, height, sigma, line, scratch);
            }
        }

        byte[] result = new byte[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            result[index] = (byte)Math.Clamp((int)Math.Round(values[index]), 0, byte.MaxValue);
        }

        return result;
    }

    /// <summary>
    /// Merges the blurred mask with the source coverage per <paramref name="style"/>, as
    /// <c>SkBlurMask::BoxBlur</c> does: normal keeps the blur, solid adds the source on top, outer keeps
    /// the blur outside the source only and inner keeps it inside the source only.
    /// </summary>
    internal static byte[] ApplyBlurStyle(IReadOnlyList<byte> source, IReadOnlyList<byte> blurred, BlurStyle style)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(blurred);
        if (source.Count != blurred.Count)
        {
            throw new ArgumentException("The source and blurred masks must have the same size.", nameof(blurred));
        }

        byte[] result = new byte[source.Count];
        for (int index = 0; index < result.Length; index++)
        {
            int src = source[index];
            int blur = blurred[index];
            result[index] = style switch
            {
                BlurStyle.Solid => (byte)(src + MultiplyAlpha(blur, 255 - src)),
                BlurStyle.Outer => MultiplyAlpha(blur, 255 - src),
                BlurStyle.Inner => MultiplyAlpha(blur, src),
                _ => (byte)blur,
            };
        }

        return result;
    }

    /// <summary>Premultiplied BGRA pixels of <paramref name="color"/> under the coverage mask.</summary>
    internal static byte[] ColorizeMask(IReadOnlyList<byte> mask, Color color)
    {
        ArgumentNullException.ThrowIfNull(mask);
        byte[] pixels = new byte[checked(mask.Count * 4)];
        for (int index = 0; index < mask.Count; index++)
        {
            int alpha = MultiplyAlpha(color.Alpha, mask[index]);
            int offset = index * 4;
            pixels[offset] = MultiplyAlpha(color.Blue, alpha);
            pixels[offset + 1] = MultiplyAlpha(color.Green, alpha);
            pixels[offset + 2] = MultiplyAlpha(color.Red, alpha);
            pixels[offset + 3] = (byte)alpha;
        }

        return pixels;
    }

    /// <summary>Scales premultiplied BGRA shader pixels by the coverage mask.</summary>
    internal static byte[] ShadeMask(IReadOnlyList<byte> mask, byte[] shaderPixels)
    {
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentNullException.ThrowIfNull(shaderPixels);
        for (int index = 0; index < mask.Count; index++)
        {
            int coverage = mask[index];
            int offset = index * 4;
            for (int channel = 0; channel < 4; channel++)
            {
                shaderPixels[offset + channel] = MultiplyAlpha(shaderPixels[offset + channel], coverage);
            }
        }

        return shaderPixels;
    }

    /// <summary>Rasterizes <paramref name="draw"/> and returns its alpha channel.</summary>
    internal static byte[] RasterizeAlpha(
        int width,
        int height,
        Point origin,
        double scale,
        Action<DrawingContext> draw)
    {
        byte[] pixels = Rasterize(width, height, origin, scale, draw);
        byte[] alpha = new byte[checked(width * height)];
        for (int index = 0; index < alpha.Length; index++)
        {
            alpha[index] = pixels[(index * 4) + 3];
        }

        return alpha;
    }

    /// <summary>Rasterizes <paramref name="brush"/> over <paramref name="bounds"/> (premultiplied BGRA).</summary>
    internal static byte[] RasterizeBrush(int width, int height, Rect bounds, double scale, IBrush brush) =>
        Rasterize(width, height, bounds.Position, scale, context => context.DrawRectangle(brush, null, bounds));

    internal static WriteableBitmap CreateBitmap(byte[] pixels, int width, int height)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            Dpi,
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        int rowBytes = checked(width * 4);
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(pixels, y * rowBytes, framebuffer.Address + (y * framebuffer.RowBytes), rowBytes);
        }

        return bitmap;
    }

    internal static void DrawBitmap(DrawingContext context, IImage bitmap, Rect bounds, BlendMode blendMode)
    {
        using DrawingContext.PushedState options = context.PushRenderOptions(new RenderOptions
        {
            BitmapBlendingMode = FilterLayerRasterizer.ToBitmapBlendingMode(blendMode),
            BitmapInterpolationMode = BitmapInterpolationMode.LowQuality,
        });
        context.DrawImage(bitmap, new Rect(bitmap.Size), bounds);
    }

    private static byte[] Rasterize(
        int width,
        int height,
        Point origin,
        double scale,
        Action<DrawingContext> draw)
    {
        var pixelSize = new PixelSize(width, height);
        using var target = new RenderTargetBitmap(pixelSize, Dpi);
        using (DrawingContext drawingContext = target.CreateDrawingContext())
        using (drawingContext.PushTransform(
                   Matrix.CreateTranslation(-origin.X, -origin.Y) * Matrix.CreateScale(scale, scale)))
        {
            draw(drawingContext);
        }

        using var readable = new WriteableBitmap(pixelSize, Dpi, PixelFormats.Bgra8888, AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = readable.Lock();
        target.CopyPixels(framebuffer);
        int rowBytes = checked(width * 4);
        byte[] pixels = new byte[checked(rowBytes * height)];
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(framebuffer.Address + (y * framebuffer.RowBytes), pixels, y * rowBytes, rowBytes);
        }

        return pixels;
    }

    // Blurs one row or column of `values` in place (`stride` apart), treating the outside as transparent.
    private static void BlurLine(
        double[] values,
        int start,
        int stride,
        int count,
        double sigma,
        double[] line,
        double[] scratch)
    {
        for (int index = 0; index < count; index++)
        {
            line[index] = values[start + (index * stride)];
        }

        if (sigma < 2.0)
        {
            GaussianPass(line, scratch, count, sigma);
            Array.Copy(scratch, line, count);
        }
        else
        {
            // SVG feGaussianBlur: d = floor(s * 3 * sqrt(2 * pi) / 4 + 0.5).
            int box = (int)Math.Floor((sigma * 3.0 * Math.Sqrt(2.0 * Math.PI) / 4.0) + 0.5);
            if (box % 2 == 1)
            {
                BoxPass(line, scratch, count, box, box / 2);
                BoxPass(scratch, line, count, box, box / 2);
                BoxPass(line, scratch, count, box, box / 2);
            }
            else
            {
                BoxPass(line, scratch, count, box, box / 2);
                BoxPass(scratch, line, count, box, (box / 2) - 1);
                BoxPass(line, scratch, count, box + 1, box / 2);
            }

            Array.Copy(scratch, line, count);
        }

        for (int index = 0; index < count; index++)
        {
            values[start + (index * stride)] = line[index];
        }
    }

    // A box of `size` samples whose window starts `leftReach` samples left of the output position.
    private static void BoxPass(double[] source, double[] target, int count, int size, int leftReach)
    {
        double sum = 0.0;
        int windowStart = -leftReach;
        for (int index = windowStart; index < windowStart + size; index++)
        {
            sum += Sample(source, count, index);
        }

        for (int index = 0; index < count; index++)
        {
            target[index] = sum / size;
            sum += Sample(source, count, windowStart + index + size) - Sample(source, count, windowStart + index);
        }
    }

    private static void GaussianPass(double[] source, double[] target, int count, double sigma)
    {
        int radius = (int)Math.Ceiling(sigma * 3.0);
        double[] kernel = new double[(radius * 2) + 1];
        double sum = 0.0;
        for (int index = -radius; index <= radius; index++)
        {
            double weight = Math.Exp(-(index * index) / (2.0 * sigma * sigma));
            kernel[index + radius] = weight;
            sum += weight;
        }

        for (int index = 0; index < count; index++)
        {
            double value = 0.0;
            for (int tap = -radius; tap <= radius; tap++)
            {
                value += Sample(source, count, index + tap) * kernel[tap + radius];
            }

            target[index] = value / sum;
        }
    }

    private static double Sample(double[] source, int count, int index) =>
        index >= 0 && index < count ? source[index] : 0.0;

    // Skia's `SkAlphaMul(value, SkAlpha255To256(alpha))`.
    private static byte MultiplyAlpha(int value, int alpha) => (byte)((value * (alpha + 1)) >> 8);
}
