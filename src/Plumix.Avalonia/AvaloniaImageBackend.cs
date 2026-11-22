using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Foundation;
using Plumix.UI;
using SkiaSharp;

// C#-only engine adapter for dart:ui/painting.dart's image codecs and Image.toByteData over Avalonia bitmaps
// and Skia's codecs (IImageBackend, src/Plumix/UI/ImageBackend.cs). SKCodec supplies the same frame
// dependencies, disposal and repetition metadata as Flutter's engine.

namespace Plumix;

/// <summary>Decodes images with Avalonia and Skia, and reads Avalonia bitmaps back.</summary>
internal sealed class AvaloniaImageBackend : IImageBackend
{
    internal static readonly AvaloniaImageBackend Instance = new();

    private AvaloniaImageBackend()
    {
    }

    public async Task<Codec> InstantiateImageCodecWithSize(
        ImmutableBuffer buffer,
        TargetImageSizeCallback? getTargetSize)
    {
        // Dart creates its encoded descriptor before the buffer-disposal try/finally.
        using Stream stream = buffer.OpenRead();
        using SKData encoded = SKData.Create(stream);
        ApngImageCodec? animatedPng = ApngImageCodec.TryCreate(buffer.Bytes);
        SKCodec? descriptor = animatedPng is null ? SKCodec.Create(encoded) : null;
        stream.Position = 0;
        using Bitmap? intrinsic = animatedPng is null && (descriptor is null || descriptor.FrameCount <= 1)
            ? new Bitmap(stream)
            : null;
        try
        {
            int width = animatedPng?.Width ?? intrinsic?.PixelSize.Width ?? descriptor!.Info.Width;
            int height = animatedPng?.Height ?? intrinsic?.PixelSize.Height ?? descriptor!.Info.Height;
            TargetImageSize target = getTargetSize?.Invoke(width, height) ?? new TargetImageSize();
            if (Constants.KDebugMode && (target.Width <= 0 || target.Height <= 0))
            {
                throw new AssertionError("Target image dimensions must be positive or null.");
            }

            int? targetWidth = target.Width > 0 ? target.Width : null;
            int? targetHeight = target.Height > 0 ? target.Height : null;
            double aspectRatio = (double)width / height;
            if (targetWidth is null && targetHeight is null)
            {
                targetWidth = width;
                targetHeight = height;
            }
            else if (targetWidth is null)
            {
                targetWidth = (int)Math.Round(targetHeight!.Value * aspectRatio, MidpointRounding.AwayFromZero);
            }
            else if (targetHeight is null)
            {
                targetHeight = (int)(targetWidth.Value / aspectRatio);
            }

            var targetSize = new PixelSize(targetWidth.Value, targetHeight.Value);
            if (animatedPng is not null)
            {
                animatedPng.TargetSize = targetSize;
                ApngImageCodec result = animatedPng;
                animatedPng = null;
                return result;
            }

            if (intrinsic is not null)
            {
                return new BitmapCodec(intrinsic.CreateScaledBitmap(targetSize, BitmapInterpolationMode.HighQuality));
            }

            var codec = new SkiaImageCodec(descriptor!, targetSize);
            descriptor = null; // Ownership moves to the codec; frame decoding outlives the buffer.
            return await Task.FromResult<Codec>(codec);
        }
        finally
        {
            descriptor?.Dispose();
            animatedPng?.Dispose();
            buffer.Dispose();
        }
    }

    private sealed class BitmapCodec(Bitmap image) : Codec
    {
        private Bitmap? _image = image;
        private bool _decoded;
        public override int FrameCount => 1;
        public override int RepetitionCount => 0;

        public override Task<FrameInfo> GetNextFrame()
        {
            Bitmap source = _image ?? throw new ObjectDisposedException(nameof(Codec));
            Task<FrameInfo> frame = Task.FromResult(new FrameInfo(TimeSpan.Zero,
                AvaloniaImages.Wrap(source.CreateScaledBitmap(source.PixelSize, BitmapInterpolationMode.HighQuality))));
            // SingleFrameCodec decodes on the first call only; later calls answer from its cached image.
            if (_decoded)
            {
                return frame;
            }

            _decoded = true;
            return Codec.CompleteOnLaterTurn(frame);
        }

        public override void Dispose()
        {
            _image?.Dispose();
            _image = null;
        }
    }

    private static readonly Offset Dpi = new(96.0, 96.0);

    /// <summary>Converts <paramref name="image"/> into a byte array.</summary>
    /// <remarks>
    /// dart:ui's <c>Image.toByteData</c>. The raw formats are read row by row through an RGBA framebuffer,
    /// which transcodes the bitmap's own pixel and alpha format; channel floats of
    /// <see cref="ImageByteFormat.RawExtendedRgba128"/> are written in host byte order, as the engine's are.
    /// </remarks>
    public Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format)
    {
        ArgumentNullException.ThrowIfNull(image);
        return image.Native is Bitmap bitmap
            ? ToByteData(bitmap, format)
            : throw new NotSupportedException("Only bitmap-backed images can be read back.");
    }

    internal static Task<ByteData?> ToByteData(Bitmap image, ImageByteFormat format = ImageByteFormat.RawRgba)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (format == ImageByteFormat.Png)
        {
            using var stream = new MemoryStream();
            image.Save(stream, PngBitmapEncoderOptions.Default);
            return Task.FromResult<ByteData?>(ByteData.SublistView(stream.ToArray()));
        }

        bool premultiplied = format is ImageByteFormat.RawRgba or ImageByteFormat.RawUnmodified;
        byte[] pixels = ReadRgba(image, premultiplied ? AlphaFormat.Premul : AlphaFormat.Unpremul);
        if (format != ImageByteFormat.RawExtendedRgba128)
        {
            return Task.FromResult<ByteData?>(ByteData.SublistView(pixels));
        }

        byte[] floats = new byte[checked(pixels.Length * sizeof(float))];
        for (int index = 0; index < pixels.Length; index++)
        {
            BitConverter.TryWriteBytes(floats.AsSpan(index * sizeof(float)), pixels[index] / 255.0f);
        }

        return Task.FromResult<ByteData?>(ByteData.SublistView(floats));
    }

    private static byte[] ReadRgba(Bitmap image, AlphaFormat alphaFormat)
    {
        PixelSize size = image.PixelSize;
        using var readable = new WriteableBitmap(size, Dpi.ToAvaloniaVector(), PixelFormats.Rgba8888, alphaFormat);
        using ILockedFramebuffer framebuffer = readable.Lock();
        image.CopyPixels(framebuffer);
        int rowBytes = checked(size.Width * 4);
        byte[] pixels = new byte[checked(rowBytes * size.Height)];
        for (int y = 0; y < size.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                framebuffer.Address + (y * framebuffer.RowBytes),
                pixels,
                y * rowBytes,
                rowBytes);
        }

        return pixels;
    }
}
