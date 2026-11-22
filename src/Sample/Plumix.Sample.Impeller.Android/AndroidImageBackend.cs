using Android.Graphics;
using Java.Nio;
using Plumix.Foundation;
using Plumix.Impeller;
using Plumix.UI;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. The core's image seam (UI/ImageBackend.cs) on the
// platform decoder, the way the engine's Android embedding can hand decoding to android.graphics: images are
// decoded by BitmapFactory into premultiplied RGBA8888 at the requested size, and Image.toByteData reads the
// kept pixels (PNG through Bitmap.compress). Stage 4 prototype: animated images decode their first frame only.

/// <summary>Decodes images with Android's BitmapFactory.</summary>
internal sealed class AndroidImageBackend : IImageBackend
{
    public Task<Codec> InstantiateImageCodecWithSize(ImmutableBuffer buffer, TargetImageSizeCallback? getTargetSize)
    {
        try
        {
            byte[] bytes = buffer.Bytes.ToArray();
            var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, bounds);
            int width = bounds.OutWidth;
            int height = bounds.OutHeight;
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Invalid image data", nameof(buffer));
            }

            (int targetWidth, int targetHeight) = TargetSize(width, height, getTargetSize);
            var options = new BitmapFactory.Options
            {
                InPreferredConfig = Bitmap.Config.Argb8888,
                InPremultiplied = true,
            };
            using Bitmap decoded = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options)
                ?? throw new ArgumentException("Invalid image data", nameof(buffer));
            using Bitmap scaled = targetWidth == decoded.Width && targetHeight == decoded.Height
                ? decoded
                : Bitmap.CreateScaledBitmap(decoded, targetWidth, targetHeight, true)!;
            byte[] rgba = ReadPixels(scaled);
            return Task.FromResult<Codec>(new SingleFrameCodec(rgba, scaled.Width, scaled.Height));
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format)
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
                result = EncodePng(rgba, pixels.Width, pixels.Height);
                break;
        }

        return Task.FromResult<ByteData?>(ByteData.View(result, 0));
    }

    // Dart's `_getDefaultImageSizes`/`ImageDescriptor.instantiateCodec`: a missing dimension keeps the
    // aspect ratio.
    private static (int Width, int Height) TargetSize(int width, int height, TargetImageSizeCallback? getTargetSize)
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

    // ARGB_8888 is RGBA bytes in memory, premultiplied, which is Impeller's RGBA8888.
    private static byte[] ReadPixels(Bitmap bitmap)
    {
        int length = bitmap.ByteCount;
        using ByteBuffer buffer = ByteBuffer.Allocate(length)!;
        bitmap.CopyPixelsToBuffer(buffer);
        buffer.Rewind();
        byte[] rgba = new byte[length];
        buffer.Get(rgba);
        return rgba;
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height)
    {
        using Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!)!;
        using (ByteBuffer pixels = ByteBuffer.Wrap(rgba)!)
        {
            bitmap.CopyPixelsFromBuffer(pixels);
        }

        using var png = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, png);
        return png.ToArray();
    }

    private static byte[] Unpremultiply(byte[] rgba)
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
    }
}

/// <summary>The app's Android assets (the AndroidAsset items) as the core's asset source.</summary>
internal sealed class AndroidAssetSource(global::Android.Content.Res.AssetManager assets) : IAssetSource
{
    public Stream Open(Uri uri, Uri? baseUri)
    {
        Uri absolute = uri.IsAbsoluteUri ? uri : new Uri(baseUri!, uri);
        using Stream asset = assets.Open(Uri.UnescapeDataString(absolute.AbsolutePath).TrimStart('/'));
        var copy = new MemoryStream();
        asset.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    public IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri)
    {
        Uri absolute = directory.IsAbsoluteUri ? directory : new Uri(baseUri!, directory);
        string path = Uri.UnescapeDataString(absolute.AbsolutePath).Trim('/');
        return (assets.List(path) ?? []).Select(name => new Uri(absolute, name));
    }
}
