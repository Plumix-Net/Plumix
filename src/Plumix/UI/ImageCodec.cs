using Plumix.Foundation;
using Plumix.Rendering;

// C#-only engine adapter for dart:ui/painting.dart's immutable buffers and image codecs. Decoding and
// pixel readback belong to the host's drawing backend (ImageBackend.cs).

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

    internal ReadOnlySpan<byte> Bytes => _bytes ?? throw new ObjectDisposedException(nameof(ImmutableBuffer));

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
public sealed record FrameInfo(TimeSpan Duration, RasterImage Image);

/// <summary>The platform image decoder contract.</summary>
public abstract class Codec : IDisposable
{
    public abstract int FrameCount { get; }
    public abstract int RepetitionCount { get; }
    public abstract Task<FrameInfo> GetNextFrame();
    public abstract void Dispose();

    /// <summary>
    /// Completes a decoded frame the way the engine does: on a later turn of the event loop, where
    /// the engine posts the result of its off-thread decode to the UI task runner
    /// (<c>multi_frame_codec.cc</c>, <c>single_frame_codec.cc</c>).
    /// </summary>
    internal static Task<FrameInfo> CompleteOnLaterTurn(Task<FrameInfo> decoded)
    {
        var completer = new TaskCompletionSource<FrameInfo>();
        EventLoop.PostTask(() =>
        {
            if (decoded.IsCompletedSuccessfully)
            {
                completer.SetResult(decoded.Result);
            }
            else
            {
                completer.SetException(decoded.Exception!.InnerExceptions);
            }
        });
        return completer.Task;
    }
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

    public static Task<Codec> InstantiateImageCodecWithSize(
        ImmutableBuffer buffer,
        TargetImageSizeCallback? getTargetSize = null) =>
        ImageBackend.Current.InstantiateImageCodecWithSize(buffer, getTargetSize);
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
    /// <remarks>Every image the backend produces is 32-bit RGBA, so this is <see cref="RawRgba"/>.</remarks>
    RawUnmodified,

    /// <summary>
    /// Unencoded bytes, in RGBA row-primary form with straight alpha, 32-bit float (IEEE 754 binary32)
    /// per channel.
    /// </summary>
    RawExtendedRgba128,

    /// <summary>PNG format.</summary>
    Png,
}

/// <summary>dart:ui's <c>Image.toByteData</c>.</summary>
public static class ImageByteData
{
    /// <summary>Converts <paramref name="image"/> into a byte array.</summary>
    /// <remarks>
    /// dart:ui's <c>Image.toByteData</c>; channel floats of <see cref="ImageByteFormat.RawExtendedRgba128"/>
    /// are in host byte order, as the engine's are.
    /// </remarks>
    public static Task<ByteData?> ToByteData(this RasterImage image, ImageByteFormat format = ImageByteFormat.RawRgba)
    {
        ArgumentNullException.ThrowIfNull(image);
        return ImageBackend.Current.ToByteData(image, format);
    }
}
