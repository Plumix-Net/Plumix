using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Plumix.Rendering;

internal static class FilterLayerRasterizer
{
    private static readonly Vector Dpi = new(96.0, 96.0);

    internal static byte[] ApplyColorFilterForTests(
        IReadOnlyList<byte> pixels,
        ColorFilter colorFilter)
    {
        byte[] output = pixels.ToArray();
        ApplyColorFilter(output, colorFilter);
        return output;
    }

    internal static FilterRasterResult ApplyImageFilterForTests(
        IReadOnlyList<byte> pixels,
        int width,
        int height,
        ImageFilter imageFilter)
    {
        if (width <= 0 || height <= 0 || pixels.Count != checked(width * height * 4))
        {
            throw new ArgumentException("Pixel data must contain width * height BGRA values.", nameof(pixels));
        }

        RasterFrame output = ApplyImageFilter(
            new RasterFrame(pixels.ToArray(), width, height, new Rect(0.0, 0.0, width, height)),
            imageFilter);
        return new FilterRasterResult(
            output.Pixels,
            output.Width,
            output.Height,
            output.Bounds);
    }

    public static WriteableBitmap? DrawColorFiltered(
        DrawingContext context,
        Action<DrawingContext> drawChildren,
        ColorFilter colorFilter,
        Rect bounds)
    {
        if (!CanRasterize(bounds))
        {
            drawChildren(context);
            return null;
        }

        try
        {
            RasterFrame frame = Rasterize(drawChildren, bounds);
            ApplyColorFilter(frame.Pixels, colorFilter);
            return DrawFrame(context, frame);
        }
        catch (InvalidOperationException)
        {
            drawChildren(context);
            return null;
        }
        catch (NotSupportedException)
        {
            drawChildren(context);
            return null;
        }
    }

    public static WriteableBitmap? DrawImageFiltered(
        DrawingContext context,
        Action<DrawingContext> drawChildren,
        ImageFilter imageFilter,
        Point offset,
        Rect bounds)
    {
        if (!CanRasterize(bounds))
        {
            using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
            {
                drawChildren(context);
            }

            return null;
        }

        try
        {
            RasterFrame frame = ApplyImageFilter(Rasterize(drawChildren, bounds), imageFilter);
            return DrawFrame(
                context,
                frame with { Bounds = new Rect(frame.Bounds.Position + offset, frame.Bounds.Size) });
        }
        catch (InvalidOperationException)
        {
            using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
            {
                drawChildren(context);
            }

            return null;
        }
        catch (NotSupportedException)
        {
            using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
            {
                drawChildren(context);
            }

            return null;
        }
    }

    private static bool CanRasterize(Rect bounds)
    {
        return bounds.Width > 0.0
               && bounds.Height > 0.0
               && double.IsFinite(bounds.X)
               && double.IsFinite(bounds.Y)
               && double.IsFinite(bounds.Width)
               && double.IsFinite(bounds.Height);
    }

    private static RasterFrame Rasterize(Action<DrawingContext> drawChildren, Rect bounds)
    {
        int width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)Math.Ceiling(bounds.Height));
        var pixelSize = new PixelSize(width, height);
        using var target = new RenderTargetBitmap(pixelSize, Dpi);
        using (DrawingContext drawingContext = target.CreateDrawingContext())
        using (drawingContext.PushTransform(Matrix.CreateTranslation(-bounds.X, -bounds.Y)))
        {
            drawChildren(drawingContext);
        }

        using var readable = new WriteableBitmap(
            pixelSize,
            Dpi,
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using (ILockedFramebuffer framebuffer = readable.Lock())
        {
            target.CopyPixels(framebuffer);
            byte[] pixels = CopyFromFramebuffer(framebuffer, width, height);
            return new RasterFrame(pixels, width, height, new Rect(bounds.Position, new Size(width, height)));
        }
    }

    private static byte[] CopyFromFramebuffer(ILockedFramebuffer framebuffer, int width, int height)
    {
        int rowBytes = checked(width * 4);
        byte[] pixels = new byte[checked(rowBytes * height)];
        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(framebuffer.Address + (y * framebuffer.RowBytes), pixels, y * rowBytes, rowBytes);
        }

        return pixels;
    }

    private static WriteableBitmap? DrawFrame(DrawingContext context, RasterFrame frame)
    {
        if (frame.Width <= 0 || frame.Height <= 0)
        {
            return null;
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            Dpi,
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using (ILockedFramebuffer framebuffer = bitmap.Lock())
        {
            int rowBytes = checked(frame.Width * 4);
            for (int y = 0; y < frame.Height; y++)
            {
                Marshal.Copy(frame.Pixels, y * rowBytes, framebuffer.Address + (y * framebuffer.RowBytes), rowBytes);
            }
        }

        context.DrawImage(
            bitmap,
            new Rect(0.0, 0.0, frame.Width, frame.Height),
            frame.Bounds);
        return bitmap;
    }

    private static void ApplyColorFilter(byte[] pixels, ColorFilter colorFilter)
    {
        switch (colorFilter)
        {
            case ColorFilter.Matrix matrix:
                ApplyColorMatrix(pixels, matrix.Values);
                break;
            case ColorFilter.Mode mode:
                ApplyColorMode(pixels, mode.Color, mode.FlutterBlendMode);
                break;
            default:
                throw new NotSupportedException($"Unsupported color filter: {colorFilter.GetType().Name}.");
        }
    }

    private static void ApplyColorMatrix(byte[] pixels, IReadOnlyList<double> matrix)
    {
        for (int index = 0; index < pixels.Length; index += 4)
        {
            double alpha = pixels[index + 3] / 255.0;
            double blue = Unpremultiply(pixels[index], alpha);
            double green = Unpremultiply(pixels[index + 1], alpha);
            double red = Unpremultiply(pixels[index + 2], alpha);
            double outputRed = ClampByte(
                (matrix[0] * red) + (matrix[1] * green) + (matrix[2] * blue) + (matrix[3] * pixels[index + 3])
                + matrix[4]);
            double outputGreen = ClampByte(
                (matrix[5] * red) + (matrix[6] * green) + (matrix[7] * blue) + (matrix[8] * pixels[index + 3])
                + matrix[9]);
            double outputBlue = ClampByte(
                (matrix[10] * red) + (matrix[11] * green) + (matrix[12] * blue)
                + (matrix[13] * pixels[index + 3])
                + matrix[14]);
            double outputAlpha = ClampByte(
                (matrix[15] * red) + (matrix[16] * green) + (matrix[17] * blue)
                + (matrix[18] * pixels[index + 3])
                + matrix[19]);
            double outputAlphaFactor = outputAlpha / 255.0;
            pixels[index] = ToByte(outputBlue * outputAlphaFactor);
            pixels[index + 1] = ToByte(outputGreen * outputAlphaFactor);
            pixels[index + 2] = ToByte(outputRed * outputAlphaFactor);
            pixels[index + 3] = ToByte(outputAlpha);
        }
    }

    private static void ApplyColorMode(
        byte[] pixels,
        Color color,
        BlendMode blendMode)
    {
        var source = new Pixel(
            color.R / 255.0,
            color.G / 255.0,
            color.B / 255.0,
            color.A / 255.0);
        for (int index = 0; index < pixels.Length; index += 4)
        {
            double destinationAlpha = pixels[index + 3] / 255.0;
            var destination = new Pixel(
                Unpremultiply(pixels[index + 2], destinationAlpha) / 255.0,
                Unpremultiply(pixels[index + 1], destinationAlpha) / 255.0,
                Unpremultiply(pixels[index], destinationAlpha) / 255.0,
                destinationAlpha);
            Pixel output = Blend(source, destination, blendMode);
            pixels[index] = ToByte(output.Blue * output.Alpha * 255.0);
            pixels[index + 1] = ToByte(output.Green * output.Alpha * 255.0);
            pixels[index + 2] = ToByte(output.Red * output.Alpha * 255.0);
            pixels[index + 3] = ToByte(output.Alpha * 255.0);
        }
    }

    private static Pixel Blend(Pixel source, Pixel destination, BlendMode mode)
    {
        return mode switch
        {
            BlendMode.Clear => default,
            BlendMode.Source => source,
            BlendMode.Destination => destination,
            BlendMode.DestinationOver => PorterDuff(source, destination, 1.0 - destination.Alpha, 1.0),
            BlendMode.SourceIn => PorterDuff(source, destination, destination.Alpha, 0.0),
            BlendMode.DestinationIn => PorterDuff(source, destination, 0.0, source.Alpha),
            BlendMode.SourceOut => PorterDuff(source, destination, 1.0 - destination.Alpha, 0.0),
            BlendMode.DestinationOut => PorterDuff(source, destination, 0.0, 1.0 - source.Alpha),
            BlendMode.SourceAtop => PorterDuff(
                source,
                destination,
                destination.Alpha,
                1.0 - source.Alpha),
            BlendMode.DestinationAtop => PorterDuff(
                source,
                destination,
                1.0 - destination.Alpha,
                source.Alpha),
            BlendMode.Xor => PorterDuff(
                source,
                destination,
                1.0 - destination.Alpha,
                1.0 - source.Alpha),
            BlendMode.Plus => Plus(source, destination),
            BlendMode.SourceOver => PorterDuff(
                source,
                destination,
                1.0,
                1.0 - source.Alpha),
            BlendMode.Modulate => new Pixel(
                source.Red * destination.Red,
                source.Green * destination.Green,
                source.Blue * destination.Blue,
                source.Alpha * destination.Alpha),
            _ => BlendSeparable(source, destination, mode),
        };
    }

    private static Pixel PorterDuff(Pixel source, Pixel destination, double sourceFactor, double destinationFactor)
    {
        double sourceWeight = source.Alpha * sourceFactor;
        double destinationWeight = destination.Alpha * destinationFactor;
        double alpha = Math.Clamp(sourceWeight + destinationWeight, 0.0, 1.0);
        if (alpha <= 0.0)
        {
            return default;
        }

        return new Pixel(
            ((source.Red * sourceWeight) + (destination.Red * destinationWeight)) / alpha,
            ((source.Green * sourceWeight) + (destination.Green * destinationWeight)) / alpha,
            ((source.Blue * sourceWeight) + (destination.Blue * destinationWeight)) / alpha,
            alpha).Clamp();
    }

    private static Pixel Plus(Pixel source, Pixel destination)
    {
        double red = Math.Min(1.0, (source.Red * source.Alpha) + (destination.Red * destination.Alpha));
        double green = Math.Min(1.0, (source.Green * source.Alpha) + (destination.Green * destination.Alpha));
        double blue = Math.Min(1.0, (source.Blue * source.Alpha) + (destination.Blue * destination.Alpha));
        double alpha = Math.Min(1.0, source.Alpha + destination.Alpha);
        return alpha <= 0.0
            ? default
            : new Pixel(red / alpha, green / alpha, blue / alpha, alpha).Clamp();
    }

    private static Pixel BlendSeparable(Pixel source, Pixel destination, BlendMode mode)
    {
        var blended = mode switch
        {
            BlendMode.Hue => SetLum(SetSat(source, Saturation(destination)), Luminosity(destination)),
            BlendMode.Saturation => SetLum(
                SetSat(destination, Saturation(source)),
                Luminosity(destination)),
            BlendMode.Color => SetLum(source, Luminosity(destination)),
            BlendMode.Luminosity => SetLum(destination, Luminosity(source)),
            _ => new Pixel(
                BlendChannel(source.Red, destination.Red, mode),
                BlendChannel(source.Green, destination.Green, mode),
                BlendChannel(source.Blue, destination.Blue, mode),
                1.0),
        };
        double alpha = source.Alpha + destination.Alpha - (source.Alpha * destination.Alpha);
        if (alpha <= 0.0)
        {
            return default;
        }

        double red = ((1.0 - source.Alpha) * destination.Red * destination.Alpha)
                     + ((1.0 - destination.Alpha) * source.Red * source.Alpha)
                     + (source.Alpha * destination.Alpha * blended.Red);
        double green = ((1.0 - source.Alpha) * destination.Green * destination.Alpha)
                       + ((1.0 - destination.Alpha) * source.Green * source.Alpha)
                       + (source.Alpha * destination.Alpha * blended.Green);
        double blue = ((1.0 - source.Alpha) * destination.Blue * destination.Alpha)
                      + ((1.0 - destination.Alpha) * source.Blue * source.Alpha)
                      + (source.Alpha * destination.Alpha * blended.Blue);
        return new Pixel(red / alpha, green / alpha, blue / alpha, alpha).Clamp();
    }

    private static double BlendChannel(double source, double destination, BlendMode mode)
    {
        return mode switch
        {
            BlendMode.Screen => source + destination - (source * destination),
            BlendMode.Overlay => destination <= 0.5
                ? 2.0 * source * destination
                : 1.0 - (2.0 * (1.0 - source) * (1.0 - destination)),
            BlendMode.Darken => Math.Min(source, destination),
            BlendMode.Lighten => Math.Max(source, destination),
            BlendMode.ColorDodge => source >= 1.0
                ? 1.0
                : Math.Min(1.0, destination / (1.0 - source)),
            BlendMode.ColorBurn => source <= 0.0
                ? 0.0
                : 1.0 - Math.Min(1.0, (1.0 - destination) / source),
            BlendMode.HardLight => source <= 0.5
                ? 2.0 * source * destination
                : 1.0 - (2.0 * (1.0 - source) * (1.0 - destination)),
            BlendMode.SoftLight => SoftLight(source, destination),
            BlendMode.Difference => Math.Abs(destination - source),
            BlendMode.Exclusion => source + destination - (2.0 * source * destination),
            BlendMode.Multiply => source * destination,
            _ => source,
        };
    }

    private static double SoftLight(double source, double destination)
    {
        if (source <= 0.5)
        {
            return destination - ((1.0 - (2.0 * source)) * destination * (1.0 - destination));
        }

        double delta = destination <= 0.25
            ? (((16.0 * destination - 12.0) * destination + 4.0) * destination)
            : Math.Sqrt(destination);
        return destination + (((2.0 * source) - 1.0) * (delta - destination));
    }

    private static double Luminosity(Pixel pixel)
    {
        return (0.30 * pixel.Red) + (0.59 * pixel.Green) + (0.11 * pixel.Blue);
    }

    private static double Saturation(Pixel pixel)
    {
        return Math.Max(pixel.Red, Math.Max(pixel.Green, pixel.Blue))
               - Math.Min(pixel.Red, Math.Min(pixel.Green, pixel.Blue));
    }

    private static Pixel SetLum(Pixel color, double luminosity)
    {
        double delta = luminosity - Luminosity(color);
        return ClipColor(new Pixel(color.Red + delta, color.Green + delta, color.Blue + delta, color.Alpha));
    }

    private static Pixel SetSat(Pixel color, double saturation)
    {
        double[] channels = [color.Red, color.Green, color.Blue];
        int minimum = Array.IndexOf(channels, channels.Min());
        int maximum = Array.IndexOf(channels, channels.Max());
        int middle = 3 - minimum - maximum;
        if (channels[maximum] > channels[minimum])
        {
            channels[middle] = ((channels[middle] - channels[minimum]) * saturation)
                               / (channels[maximum] - channels[minimum]);
            channels[maximum] = saturation;
        }
        else
        {
            channels[middle] = 0.0;
            channels[maximum] = 0.0;
        }

        channels[minimum] = 0.0;
        return new Pixel(channels[0], channels[1], channels[2], color.Alpha);
    }

    private static Pixel ClipColor(Pixel color)
    {
        double luminosity = Luminosity(color);
        double minimum = Math.Min(color.Red, Math.Min(color.Green, color.Blue));
        double maximum = Math.Max(color.Red, Math.Max(color.Green, color.Blue));
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        if (minimum < 0.0)
        {
            red = luminosity + (((red - luminosity) * luminosity) / (luminosity - minimum));
            green = luminosity + (((green - luminosity) * luminosity) / (luminosity - minimum));
            blue = luminosity + (((blue - luminosity) * luminosity) / (luminosity - minimum));
        }

        if (maximum > 1.0)
        {
            red = luminosity + (((red - luminosity) * (1.0 - luminosity)) / (maximum - luminosity));
            green = luminosity + (((green - luminosity) * (1.0 - luminosity)) / (maximum - luminosity));
            blue = luminosity + (((blue - luminosity) * (1.0 - luminosity)) / (maximum - luminosity));
        }

        return new Pixel(red, green, blue, color.Alpha).Clamp();
    }

    private static RasterFrame ApplyImageFilter(RasterFrame frame, ImageFilter imageFilter)
    {
        return imageFilter switch
        {
            ImageFilter.Blur blur => ApplyBlur(frame, blur),
            ImageFilter.Matrix matrix => ApplyMatrix(frame, matrix),
            ImageFilter.Dilate dilate => ApplyMorphology(frame, dilate.RadiusX, dilate.RadiusY, dilate: true),
            ImageFilter.Erode erode => ApplyMorphology(frame, erode.RadiusX, erode.RadiusY, dilate: false),
            ImageFilter.Compose compose => ApplyImageFilter(
                ApplyImageFilter(frame, compose.Inner),
                compose.Outer),
            _ => throw new NotSupportedException($"Unsupported image filter: {imageFilter.GetType().Name}."),
        };
    }

    private static RasterFrame ApplyBlur(RasterFrame frame, ImageFilter.Blur blur)
    {
        int radiusX = (int)Math.Ceiling(blur.SigmaX * 3.0);
        int radiusY = (int)Math.Ceiling(blur.SigmaY * 3.0);
        if (radiusX == 0 && radiusY == 0)
        {
            return frame;
        }

        int width = checked(frame.Width + (radiusX * 2));
        int height = checked(frame.Height + (radiusY * 2));
        int horizontalHeight = checked(frame.Height + (radiusY * 4));
        byte[] horizontal = new byte[checked(width * horizontalHeight * 4)];
        byte[] output = new byte[checked(width * height * 4)];
        double[] kernelX = GaussianKernel(radiusX, blur.SigmaX);
        double[] kernelY = GaussianKernel(radiusY, blur.SigmaY);
        for (int y = 0; y < horizontalHeight; y++)
        {
            int sourceY = y - (radiusY * 2);
            for (int x = 0; x < width; x++)
            {
                int sourceX = x - radiusX;
                AccumulateKernel(
                    frame.Pixels,
                    frame.Width,
                    frame.Height,
                    sourceX,
                    sourceY,
                    kernelX,
                    horizontal,
                    width,
                    x,
                    y,
                    horizontalAxis: true,
                    blur.TileMode);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                AccumulateKernel(
                    horizontal,
                    width,
                    horizontalHeight,
                    x,
                    y + radiusY,
                    kernelY,
                    output,
                    width,
                    x,
                    y,
                    horizontalAxis: false,
                    TileMode.Decal);
            }
        }

        return new RasterFrame(
            output,
            width,
            height,
            new Rect(
                frame.Bounds.X - radiusX,
                frame.Bounds.Y - radiusY,
                width,
                height));
    }

    private static double[] GaussianKernel(int radius, double sigma)
    {
        if (radius == 0 || sigma == 0.0)
        {
            return [1.0];
        }

        var kernel = new double[(radius * 2) + 1];
        double sum = 0.0;
        for (int index = -radius; index <= radius; index++)
        {
            double weight = Math.Exp(-(index * index) / (2.0 * sigma * sigma));
            kernel[index + radius] = weight;
            sum += weight;
        }

        for (int index = 0; index < kernel.Length; index++)
        {
            kernel[index] /= sum;
        }

        return kernel;
    }

    private static void AccumulateKernel(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceX,
        int sourceY,
        IReadOnlyList<double> kernel,
        byte[] destination,
        int destinationWidth,
        int destinationX,
        int destinationY,
        bool horizontalAxis,
        TileMode tileMode)
    {
        int radius = kernel.Count / 2;
        double blue = 0.0;
        double green = 0.0;
        double red = 0.0;
        double alpha = 0.0;
        for (int kernelIndex = -radius; kernelIndex <= radius; kernelIndex++)
        {
            int x = horizontalAxis ? sourceX + kernelIndex : sourceX;
            int y = horizontalAxis ? sourceY : sourceY + kernelIndex;
            if (!TryMapCoordinate(x, sourceWidth, tileMode, out x)
                || !TryMapCoordinate(y, sourceHeight, tileMode, out y))
            {
                continue;
            }

            int sourceIndex = ((y * sourceWidth) + x) * 4;
            double weight = kernel[kernelIndex + radius];
            blue += source[sourceIndex] * weight;
            green += source[sourceIndex + 1] * weight;
            red += source[sourceIndex + 2] * weight;
            alpha += source[sourceIndex + 3] * weight;
        }

        int destinationIndex = ((destinationY * destinationWidth) + destinationX) * 4;
        destination[destinationIndex] = ToByte(blue);
        destination[destinationIndex + 1] = ToByte(green);
        destination[destinationIndex + 2] = ToByte(red);
        destination[destinationIndex + 3] = ToByte(alpha);
    }

    private static bool TryMapCoordinate(int coordinate, int extent, TileMode tileMode, out int mapped)
    {
        if (coordinate >= 0 && coordinate < extent)
        {
            mapped = coordinate;
            return true;
        }

        switch (tileMode)
        {
            case TileMode.Clamp:
                mapped = Math.Clamp(coordinate, 0, extent - 1);
                return true;
            case TileMode.Repeated:
                mapped = ((coordinate % extent) + extent) % extent;
                return true;
            case TileMode.Mirror:
                int period = extent * 2;
                int value = ((coordinate % period) + period) % period;
                mapped = value < extent ? value : period - value - 1;
                return true;
            default:
                mapped = 0;
                return false;
        }
    }

    private static RasterFrame ApplyMorphology(
        RasterFrame frame,
        double radiusX,
        double radiusY,
        bool dilate)
    {
        int horizontalRadius = (int)Math.Ceiling(radiusX);
        int verticalRadius = (int)Math.Ceiling(radiusY);
        if (horizontalRadius == 0 && verticalRadius == 0)
        {
            return frame;
        }

        int width = dilate
            ? checked(frame.Width + (horizontalRadius * 2))
            : frame.Width;
        int height = dilate
            ? checked(frame.Height + (verticalRadius * 2))
            : frame.Height;
        int horizontalOffset = dilate ? horizontalRadius : 0;
        int verticalOffset = dilate ? verticalRadius : 0;
        byte[] output = new byte[checked(width * height * 4)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int outputIndex = ((y * width) + x) * 4;
                for (int channel = 0; channel < 4; channel++)
                {
                    byte value = dilate ? byte.MinValue : byte.MaxValue;
                    for (int sampleY = -verticalRadius; sampleY <= verticalRadius; sampleY++)
                    {
                        int sourceY = y - verticalOffset + sampleY;
                        for (int sampleX = -horizontalRadius; sampleX <= horizontalRadius; sampleX++)
                        {
                            int sourceX = x - horizontalOffset + sampleX;
                            byte sample = sourceX >= 0
                                          && sourceX < frame.Width
                                          && sourceY >= 0
                                          && sourceY < frame.Height
                                ? frame.Pixels[((sourceY * frame.Width) + sourceX) * 4 + channel]
                                : byte.MinValue;
                            value = dilate ? Math.Max(value, sample) : Math.Min(value, sample);
                        }
                    }

                    output[outputIndex + channel] = value;
                }
            }
        }

        return new RasterFrame(
            output,
            width,
            height,
            new Rect(
                frame.Bounds.X - horizontalOffset,
                frame.Bounds.Y - verticalOffset,
                width,
                height));
    }

    private static RasterFrame ApplyMatrix(RasterFrame frame, ImageFilter.Matrix matrixFilter)
    {
        IReadOnlyList<double> values = matrixFilter.Values;
        var transform = new Homography(
            values[0],
            values[4],
            values[12],
            values[1],
            values[5],
            values[13],
            values[3],
            values[7],
            values[15]);
        if (!transform.TryInvert(out Homography inverse))
        {
            return new RasterFrame([], 0, 0, default);
        }

        Point[] corners =
        [
            transform.Transform(default),
            transform.Transform(new Point(frame.Width, 0.0)),
            transform.Transform(new Point(0.0, frame.Height)),
            transform.Transform(new Point(frame.Width, frame.Height)),
        ];
        double minimumX = corners.Min(static point => point.X);
        double minimumY = corners.Min(static point => point.Y);
        double maximumX = corners.Max(static point => point.X);
        double maximumY = corners.Max(static point => point.Y);
        int width = Math.Max(1, (int)Math.Ceiling(maximumX - minimumX));
        int height = Math.Max(1, (int)Math.Ceiling(maximumY - minimumY));
        byte[] output = new byte[checked(width * height * 4)];
        bool interpolate = matrixFilter.FilterQuality != FilterQuality.None;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Point source = inverse.Transform(new Point(x + minimumX + 0.5, y + minimumY + 0.5));
                int outputIndex = ((y * width) + x) * 4;
                if (interpolate)
                {
                    SampleBilinear(frame, source.X - 0.5, source.Y - 0.5, output, outputIndex);
                }
                else
                {
                    SampleNearest(frame, source.X - 0.5, source.Y - 0.5, output, outputIndex);
                }
            }
        }

        return new RasterFrame(
            output,
            width,
            height,
            new Rect(
                frame.Bounds.X + minimumX,
                frame.Bounds.Y + minimumY,
                width,
                height));
    }

    private static void SampleNearest(
        RasterFrame frame,
        double sourceX,
        double sourceY,
        byte[] output,
        int outputIndex)
    {
        int x = (int)Math.Round(sourceX);
        int y = (int)Math.Round(sourceY);
        if (x < 0 || x >= frame.Width || y < 0 || y >= frame.Height)
        {
            return;
        }

        int sourceIndex = ((y * frame.Width) + x) * 4;
        Array.Copy(frame.Pixels, sourceIndex, output, outputIndex, 4);
    }

    private static void SampleBilinear(
        RasterFrame frame,
        double sourceX,
        double sourceY,
        byte[] output,
        int outputIndex)
    {
        int left = (int)Math.Floor(sourceX);
        int top = (int)Math.Floor(sourceY);
        double horizontal = sourceX - left;
        double vertical = sourceY - top;
        for (int channel = 0; channel < 4; channel++)
        {
            double topValue = (Sample(frame, left, top, channel) * (1.0 - horizontal))
                              + (Sample(frame, left + 1, top, channel) * horizontal);
            double bottomValue = (Sample(frame, left, top + 1, channel) * (1.0 - horizontal))
                                 + (Sample(frame, left + 1, top + 1, channel) * horizontal);
            output[outputIndex + channel] = ToByte(
                (topValue * (1.0 - vertical)) + (bottomValue * vertical));
        }
    }

    private static byte Sample(RasterFrame frame, int x, int y, int channel)
    {
        return x >= 0 && x < frame.Width && y >= 0 && y < frame.Height
            ? frame.Pixels[((y * frame.Width) + x) * 4 + channel]
            : (byte)0;
    }

    private static double Unpremultiply(byte value, double alpha)
    {
        return alpha <= 0.0 ? 0.0 : Math.Clamp(value / alpha, 0.0, 255.0);
    }

    private static double ClampByte(double value)
    {
        return Math.Clamp(value, 0.0, 255.0);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private readonly record struct RasterFrame(
        byte[] Pixels,
        int Width,
        int Height,
        Rect Bounds);

    private readonly record struct Pixel(
        double Red,
        double Green,
        double Blue,
        double Alpha)
    {
        public Pixel Clamp()
        {
            return new Pixel(
                Math.Clamp(Red, 0.0, 1.0),
                Math.Clamp(Green, 0.0, 1.0),
                Math.Clamp(Blue, 0.0, 1.0),
                Math.Clamp(Alpha, 0.0, 1.0));
        }
    }

    private readonly record struct Homography(
        double M00,
        double M01,
        double M02,
        double M10,
        double M11,
        double M12,
        double M20,
        double M21,
        double M22)
    {
        public Point Transform(Point point)
        {
            double denominator = (M20 * point.X) + (M21 * point.Y) + M22;
            if (Math.Abs(denominator) < 0.0000001)
            {
                return new Point(double.NaN, double.NaN);
            }

            return new Point(
                ((M00 * point.X) + (M01 * point.Y) + M02) / denominator,
                ((M10 * point.X) + (M11 * point.Y) + M12) / denominator);
        }

        public bool TryInvert(out Homography inverse)
        {
            double determinant = (M00 * ((M11 * M22) - (M12 * M21)))
                                 - (M01 * ((M10 * M22) - (M12 * M20)))
                                 + (M02 * ((M10 * M21) - (M11 * M20)));
            if (Math.Abs(determinant) < 0.0000001)
            {
                inverse = default;
                return false;
            }

            double reciprocal = 1.0 / determinant;
            inverse = new Homography(
                ((M11 * M22) - (M12 * M21)) * reciprocal,
                ((M02 * M21) - (M01 * M22)) * reciprocal,
                ((M01 * M12) - (M02 * M11)) * reciprocal,
                ((M12 * M20) - (M10 * M22)) * reciprocal,
                ((M00 * M22) - (M02 * M20)) * reciprocal,
                ((M02 * M10) - (M00 * M12)) * reciprocal,
                ((M10 * M21) - (M11 * M20)) * reciprocal,
                ((M01 * M20) - (M00 * M21)) * reciprocal,
                ((M00 * M11) - (M01 * M10)) * reciprocal);
            return true;
        }
    }
}

internal readonly record struct FilterRasterResult(
    byte[] Pixels,
    int Width,
    int Height,
    Rect Bounds);
