using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only host infrastructure; no Dart parity source. What the native hosts' image backends share once a platform
// decoder has produced premultiplied RGBA8888: the target size dart:ui picks, the engine's single-frame codec and
// Image.toByteData's raw formats (PNG is the host's encoder).

/// <summary>Helpers for images decoded into premultiplied RGBA8888.</summary>
internal static class DecodedImages
{
    /// <summary>The engine's <c>SingleFrameCodec</c> over decoded pixels.</summary>
    public static Codec SingleFrame(byte[] rgba, int width, int height) => new SingleFrameCodec(rgba, width, height);

    /// <summary>
    /// dart:ui's <c>Image.toByteData</c> over decoded pixels; <paramref name="encodePng"/> writes PNG.
    /// </summary>
    public static Task<ByteData?> ToByteData(
        RasterImage image,
        ImageByteFormat format,
        Func<byte[], int, int, byte[]> encodePng)
    {
        if (image.Native is not ImpellerImage pixels)
        {
            throw new NotSupportedException("Only decoded images can be read back.");
        }

        byte[] rgba = pixels.Rgba;
        byte[] result;
        switch (format)
        {
            case ImageByteFormat.RawRgba:
            case ImageByteFormat.RawUnmodified:
                result = (byte[])rgba.Clone();
                break;
            case ImageByteFormat.RawStraightRgba:
                result = Unpremultiply(rgba);
                break;
            case ImageByteFormat.RawExtendedRgba128:
            {
                byte[] straight = Unpremultiply(rgba);
                result = new byte[straight.Length * 4];
                for (int i = 0; i < straight.Length; i++)
                {
                    BitConverter.TryWriteBytes(result.AsSpan(i * 4), straight[i] / 255f);
                }

                break;
            }
            default:
                result = encodePng(rgba, pixels.Width, pixels.Height);
                break;
        }

        return Task.FromResult<ByteData?>(ByteData.View(result, 0));
    }

    // Dart's `_getDefaultImageSizes`/`ImageDescriptor.instantiateCodec`: a missing dimension keeps the
    // aspect ratio.
    public static (int Width, int Height) TargetSize(int width, int height, TargetImageSizeCallback? getTargetSize)
    {
        TargetImageSize target = getTargetSize?.Invoke(width, height) ?? new TargetImageSize();
        int? targetWidth = target.Width > 0 ? target.Width : null;
        int? targetHeight = target.Height > 0 ? target.Height : null;
        double aspectRatio = (double)width / height;
        return (targetWidth, targetHeight) switch
        {
            (null, null) => (width, height),
            (null, int h) => ((int)Math.Round(h * aspectRatio, MidpointRounding.AwayFromZero), h),
            (int w, null) => (w, (int)(w / aspectRatio)),
            (int w, int h) => (w, h),
        };
    }

    public static byte[] Unpremultiply(byte[] rgba)
    {
        byte[] straight = (byte[])rgba.Clone();
        for (int i = 0; i < straight.Length; i += 4)
        {
            int alpha = straight[i + 3];
            if (alpha is > 0 and < 255)
            {
                for (int channel = 0; channel < 3; channel++)
                {
                    straight[i + channel] = (byte)Math.Min(255, ((straight[i + channel] * 255) + (alpha / 2)) / alpha);
                }
            }
        }

        return straight;
    }

    /// <summary>The engine's SingleFrameCodec over decoded pixels.</summary>
    private sealed class SingleFrameCodec(byte[] rgba, int width, int height) : Codec
    {
        private bool _decoded;
        private bool _disposed;

        public override int FrameCount => 1;

        public override int RepetitionCount => 0;

        public override Task<FrameInfo> GetNextFrame()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<FrameInfo> frame =
                Task.FromResult(new FrameInfo(TimeSpan.Zero, ImpellerImage.Wrap(rgba, width, height)));
            // SingleFrameCodec decodes on the first call only; later calls answer from its cached image.
            if (_decoded)
            {
                return frame;
            }

            _decoded = true;
            return Codec.CompleteOnLaterTurn(frame);
        }

        public override void Dispose() => _disposed = true;
    }}
