using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Foundation;
using Plumix.Rendering;

// C#-only engine adapter for dart:ui/painting.dart's immutable buffers and image codecs.
// Avalonia exposes static Bitmap decoding; animated codecs are tracked in docs/ai/DIVERGENCES.md.

namespace Plumix.UI;

/// <summary>An owned, immutable copy of encoded image bytes.</summary>
public sealed class ImmutableBuffer : IDisposable
{
    private byte[]? _bytes;

    private ImmutableBuffer(byte[] bytes)
    {
        _bytes = bytes;
        Length = bytes.Length;
    }

    public int Length { get; }

    public bool DebugDisposed => Constants.KDebugMode
        ? _bytes is null
        : throw new InvalidOperationException("ImmutableBuffer.debugDisposed requires asserts.");

    public static Task<ImmutableBuffer> FromUint8List(byte[] bytes) =>
        Task.FromResult(new ImmutableBuffer((byte[])bytes.Clone()));

    public static async Task<ImmutableBuffer> FromAsset(string assetKey)
    {
        using Stream stream = await PlatformAssetBundle.Root.LoadAsync(assetKey);
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes);
        return new ImmutableBuffer(bytes.ToArray());
    }

    public static async Task<ImmutableBuffer> FromFilePath(string path) =>
        new(await File.ReadAllBytesAsync(path));

    internal Stream OpenRead() => new MemoryStream(
        _bytes ?? throw new ObjectDisposedException(nameof(ImmutableBuffer)), writable: false);

    public void Dispose()
    {
        if (Constants.KDebugMode && _bytes is null)
        {
            throw new AssertionError("An ImmutableBuffer cannot be disposed more than once.");
        }

        _bytes = null;
    }
}

/// <summary>A requested image decode size; either omitted dimension preserves the aspect ratio.</summary>
public sealed class TargetImageSize
{
    public TargetImageSize(int? width = null, int? height = null)
    {
        if (Constants.KDebugMode && (width <= 0 || height <= 0))
        {
            throw new AssertionError("Target image dimensions must be positive or null.");
        }

        Width = width;
        Height = height;
    }

    public int? Width { get; }
    public int? Height { get; }

    public override string ToString() =>
        $"TargetImageSize({Width?.ToString() ?? "null"} x {Height?.ToString() ?? "null"})";
}

public delegate TargetImageSize TargetImageSizeCallback(int intrinsicWidth, int intrinsicHeight);

/// <summary>A decoded frame. The recipient owns and must dispose its image.</summary>
public sealed record FrameInfo(TimeSpan Duration, Bitmap Image);

/// <summary>The platform image decoder contract.</summary>
public abstract class Codec : IDisposable
{
    public abstract int FrameCount { get; }
    public abstract int RepetitionCount { get; }
    public abstract Task<FrameInfo> GetNextFrame();
    public abstract void Dispose();
}

/// <summary>Engine image decode entry points over the platform's bitmap backend.</summary>
public static class ImageDecoding
{
    public static async Task<Codec> InstantiateImageCodec(
        byte[] bytes,
        int? targetWidth = null,
        int? targetHeight = null,
        bool allowUpscaling = true) =>
        await InstantiateImageCodecFromBuffer(
            await ImmutableBuffer.FromUint8List(bytes), targetWidth, targetHeight, allowUpscaling);

    public static Task<Codec> InstantiateImageCodecFromBuffer(
        ImmutableBuffer buffer,
        int? targetWidth = null,
        int? targetHeight = null,
        bool allowUpscaling = true) =>
        InstantiateImageCodecWithSize(buffer, (width, height) => new TargetImageSize(
            !allowUpscaling && targetWidth > width ? width : targetWidth,
            !allowUpscaling && targetHeight > height ? height : targetHeight));

    public static async Task<Codec> InstantiateImageCodecWithSize(
        ImmutableBuffer buffer,
        TargetImageSizeCallback? getTargetSize = null)
    {
        // Dart creates its encoded descriptor before the buffer-disposal try/finally.
        using Stream stream = buffer.OpenRead();
        using var intrinsic = new Bitmap(stream);
        try
        {
            int width = intrinsic.PixelSize.Width;
            int height = intrinsic.PixelSize.Height;
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

            var image = intrinsic.CreateScaledBitmap(
                new PixelSize(targetWidth.Value, targetHeight.Value), BitmapInterpolationMode.HighQuality);
            return await Task.FromResult<Codec>(new BitmapCodec(image));
        }
        finally
        {
            buffer.Dispose();
        }
    }

    private sealed class BitmapCodec(Bitmap image) : Codec
    {
        private Bitmap? _image = image;

        public override int FrameCount => 1;
        public override int RepetitionCount => 0;

        public override Task<FrameInfo> GetNextFrame()
        {
            Bitmap source = _image ?? throw new ObjectDisposedException(nameof(Codec));
            // Avalonia has no public clone handle: each returned frame owns a separate bitmap.
            return Task.FromResult(new FrameInfo(
                TimeSpan.Zero, source.CreateScaledBitmap(source.PixelSize, BitmapInterpolationMode.HighQuality)));
        }

        public override void Dispose()
        {
            _image?.Dispose();
            _image = null;
        }
    }
}

/// <summary>
/// The format in which image bytes should be returned when using <see cref="ImageByteData.ToByteData"/>.
/// </summary>
/// <remarks>dart:ui's <c>ImageByteFormat</c>.</remarks>
public enum ImageByteFormat
{
    /// <summary>Unencoded bytes, in RGBA row-primary form with premultiplied alpha, 8 bits per channel.</summary>
    RawRgba,

    /// <summary>Unencoded bytes, in RGBA row-primary form with straight alpha, 8 bits per channel.</summary>
    RawStraightRgba,

    /// <summary>Unencoded bytes, in the image's existing format.</summary>
    /// <remarks>
    /// Every Avalonia bitmap the engine side produces is 32-bit RGBA, so this is <see cref="RawRgba"/>.
    /// </remarks>
    RawUnmodified,

    /// <summary>
    /// Unencoded bytes, in RGBA row-primary form with straight alpha, 32-bit float (IEEE 754 binary32)
    /// per channel.
    /// </summary>
    RawExtendedRgba128,

    /// <summary>PNG format.</summary>
    Png,
}

/// <summary>dart:ui's <c>Image.toByteData</c> over the Avalonia bitmaps that stand in for <c>ui.Image</c>.</summary>
public static class ImageByteData
{
    private static readonly Vector Dpi = new(96.0, 96.0);

    /// <summary>Converts <paramref name="image"/> into a byte array.</summary>
    /// <remarks>
    /// dart:ui's <c>Image.toByteData</c>. The raw formats are read row by row through an RGBA framebuffer,
    /// which transcodes the bitmap's own pixel and alpha format; channel floats of
    /// <see cref="ImageByteFormat.RawExtendedRgba128"/> are written in host byte order, as the engine's are.
    /// </remarks>
    public static Task<ByteData?> ToByteData(this Bitmap image, ImageByteFormat format = ImageByteFormat.RawRgba)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (format == ImageByteFormat.Png)
        {
            using var stream = new MemoryStream();
            image.Save(stream);
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
        using var readable = new WriteableBitmap(size, Dpi, PixelFormats.Rgba8888, alphaFormat);
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
