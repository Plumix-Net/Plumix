using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.UI;

namespace Plumix.Rendering;

// C#-only infrastructure: the raster half of dart:ui's `Canvas.saveLayer(bounds, paint)`. Avalonia's drawing
// context can open an isolated group but composites it only with an alpha, so a layer whose paint carries a
// blend mode, a colour or image filter or inverted colours is drawn into an offscreen image, filtered the way
// Skia applies a layer paint on restore (image filter, then colour filter, then inversion, then the paint's
// alpha) and drawn back under the paint's blend mode.

internal static partial class FilterLayerRasterizer
{
    // The engine's kInvertColorMatrix (display_list/skia/dl_sk_conversions.cc), applied after the paint's
    // colour filter.
    private static readonly double[] InvertColorMatrix =
    [
        -1.0, 0.0, 0.0, 1.0, 0.0,
        0.0, -1.0, 0.0, 1.0, 0.0,
        0.0, 0.0, -1.0, 1.0, 0.0,
        1.0, 1.0, 1.0, 1.0, 0.0,
    ];

    /// <summary>
    /// Draws a save layer's group into an offscreen image of <paramref name="bounds"/> (local units) at
    /// <paramref name="scale"/> device pixels per unit and applies the layer paint to it.
    /// </summary>
    /// <returns>The composited image and where it goes, in local units; a null image for an empty group.</returns>
    internal static (WriteableBitmap? Bitmap, Rect Destination) RasterizeSaveLayer(
        Action<DrawingContext> drawChildren,
        Rect bounds,
        double scale,
        CanvasLayer layer)
    {
        double left = Math.Floor(bounds.Left * scale);
        double top = Math.Floor(bounds.Top * scale);
        double right = Math.Ceiling(bounds.Right * scale);
        double bottom = Math.Ceiling(bounds.Bottom * scale);
        int width = (int)(right - left);
        int height = (int)(bottom - top);
        if (width <= 0 || height <= 0)
        {
            return (null, default);
        }

        if ((long)width * height > PaintRasterizer.MaxRasterPixels)
        {
            throw new NotSupportedException("The layer is too large to rasterize.");
        }

        var pixelSize = new PixelSize(width, height);
        RasterFrame frame;
        using (var target = new RenderTargetBitmap(pixelSize, Dpi))
        {
            using (DrawingContext drawingContext = target.CreateDrawingContext())
            using (drawingContext.PushTransform(Matrix.CreateTranslation(-left, -top)))
            using (drawingContext.PushTransform(Matrix.CreateScale(scale, scale)))
            {
                drawChildren(drawingContext);
            }

            using var readable = new WriteableBitmap(pixelSize, Dpi, PixelFormats.Bgra8888, AlphaFormat.Premul);
            using (ILockedFramebuffer framebuffer = readable.Lock())
            {
                target.CopyPixels(framebuffer);
                frame = new RasterFrame(
                    CopyFromFramebuffer(framebuffer, width, height),
                    width,
                    height,
                    new Rect(left, top, width, height));
            }
        }

        if (layer.ImageFilter is { } imageFilter)
        {
            frame = ApplyImageFilter(frame, ScaleImageFilter(imageFilter, scale));
        }

        if (layer.ColorFilter is { } colorFilter)
        {
            ApplyColorFilter(frame.Pixels, colorFilter);
        }

        if (layer.InvertColors)
        {
            ApplyColorMatrix(frame.Pixels, InvertColorMatrix);
        }

        if (layer.BlendMode == BlendMode.Clear)
        {
            Array.Clear(frame.Pixels);
        }
        else if (layer.Alpha < byte.MaxValue)
        {
            ApplyLayerAlpha(frame.Pixels, layer.Alpha);
        }

        if (frame.Width <= 0 || frame.Height <= 0)
        {
            return (null, default);
        }

        WriteableBitmap bitmap = CreateBitmap(frame);
        var destination = new Rect(
            frame.Bounds.X / scale,
            frame.Bounds.Y / scale,
            frame.Width / scale,
            frame.Height / scale);
        return (bitmap, destination);
    }

    /// <summary>
    /// The pixels of <paramref name="image"/> after a paint's colour filter and colour inversion: what Skia
    /// samples when a <c>drawImage*</c> paint carries either.
    /// </summary>
    internal static WriteableBitmap? FilterImage(IImage image, ColorFilter? colorFilter, bool invertColors)
    {
        var bounds = new Rect(image.Size);
        if (!CanRasterize(bounds))
        {
            return null;
        }

        RasterFrame frame = Rasterize(context => context.DrawImage(image, bounds, bounds), bounds);
        if (colorFilter is not null)
        {
            ApplyColorFilter(frame.Pixels, colorFilter);
        }

        if (invertColors)
        {
            ApplyColorMatrix(frame.Pixels, InvertColorMatrix);
        }

        return CreateBitmap(frame);
    }

    /// <summary>Composites a rasterized save layer under the layer paint's blend mode.</summary>
    internal static void DrawSaveLayer(DrawingContext context, IImage image, Rect destination, BlendMode blendMode)
    {
        using (context.PushRenderOptions(new RenderOptions
               {
                   BitmapBlendingMode = ToBitmapBlendingMode(blendMode),
                   BitmapInterpolationMode = BitmapInterpolationMode.LowQuality,
               }))
        {
            context.DrawImage(image, new Rect(image.Size), destination);
        }
    }

    // An image filter given in local units, expressed in the device pixels of a raster at `scale`.
    private static ImageFilter ScaleImageFilter(ImageFilter filter, double scale)
    {
        if (scale == 1.0)
        {
            return filter;
        }

        return filter switch
        {
            ImageFilter.Blur blur => new ImageFilter.Blur(
                blur.SigmaX * scale,
                blur.SigmaY * scale,
                blur.TileMode,
                blur.Bounds is { } bounds
                    ? new Rect(bounds.X * scale, bounds.Y * scale, bounds.Width * scale, bounds.Height * scale)
                    : null),
            ImageFilter.Matrix matrix => new ImageFilter.Matrix(
                ScaleMatrix(matrix.Values, scale),
                matrix.FilterQuality),
            ImageFilter.Dilate dilate => new ImageFilter.Dilate(dilate.RadiusX * scale, dilate.RadiusY * scale),
            ImageFilter.Erode erode => new ImageFilter.Erode(erode.RadiusX * scale, erode.RadiusY * scale),
            ImageFilter.Compose compose => new ImageFilter.Compose(
                ScaleImageFilter(compose.Outer, scale),
                ScaleImageFilter(compose.Inner, scale)),
            _ => filter,
        };
    }

    // S * M * S^-1 with S = diag(scale, scale, 1, 1), on a column-major 4x4 matrix.
    private static double[] ScaleMatrix(IReadOnlyList<double> values, double scale)
    {
        double[] result = new double[16];
        for (int column = 0; column < 4; column++)
        {
            double columnScale = column < 2 ? scale : 1.0;
            for (int row = 0; row < 4; row++)
            {
                double rowScale = row < 2 ? scale : 1.0;
                result[(column * 4) + row] = values[(column * 4) + row] * rowScale / columnScale;
            }
        }

        return result;
    }

    private static void ApplyLayerAlpha(byte[] pixels, byte alpha)
    {
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = (byte)(((pixels[index] * alpha) + 127) / 255);
        }
    }

    private static WriteableBitmap CreateBitmap(RasterFrame frame)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            Dpi,
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        int rowBytes = checked(frame.Width * 4);
        for (int y = 0; y < frame.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                frame.Pixels,
                y * rowBytes,
                framebuffer.Address + (y * framebuffer.RowBytes),
                rowBytes);
        }

        return bitmap;
    }
}
