using Avalonia;
using Avalonia.Media.Imaging;
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
