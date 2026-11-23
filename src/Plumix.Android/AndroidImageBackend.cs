using Android.Graphics;
using Java.Nio;
using Plumix.Foundation;
using Plumix.Impeller;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Android;

// C#-only host infrastructure; no Dart parity source. The core's image seam (UI/ImageBackend.cs) on the
// platform decoder, the way the engine's Android embedding can hand decoding to android.graphics: images are
// decoded by BitmapFactory into premultiplied RGBA8888 at the requested size, and Image.toByteData reads the
// kept pixels (PNG through Bitmap.compress). Animated PNG goes through ApngCodec (frames decoded here), animated
// GIF and WebP through AImageDecoder from Android 12 (AndroidAnimatedImageCodec); before it they show their
// first frame.

/// <summary>Decodes images with Android's BitmapFactory.</summary>
internal sealed class AndroidImageBackend : IImageBackend
{
    public Task<Codec> InstantiateImageCodecWithSize(ImmutableBuffer buffer, TargetImageSizeCallback? getTargetSize)
    {
        try
        {
            byte[] bytes = buffer.Bytes.ToArray();
            if (ApngCodec.TryCreate(bytes, DecodePng, Scale) is { } apng)
            {
                (apng.TargetWidth, apng.TargetHeight) =
                    DecodedImages.TargetSize(apng.Width, apng.Height, getTargetSize);
                return Task.FromResult<Codec>(apng);
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(31)
                && AndroidAnimatedImageCodec.TryCreate(
                    bytes, (width, height) => DecodedImages.TargetSize(width, height, getTargetSize), Scale)
                    is { } animated)
            {
                return Task.FromResult<Codec>(animated);
            }

            var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, bounds);
            int width = bounds.OutWidth;
            int height = bounds.OutHeight;
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Invalid image data", nameof(buffer));
            }

            (int targetWidth, int targetHeight) = DecodedImages.TargetSize(width, height, getTargetSize);
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
            return Task.FromResult<Codec>(DecodedImages.SingleFrame(rgba, scaled.Width, scaled.Height));
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format) =>
        DecodedImages.ToByteData(image, format, EncodePng);

    // An APNG frame as a standalone PNG.
    private static (byte[] Rgba, int Width, int Height) DecodePng(byte[] png)
    {
        var options = new BitmapFactory.Options { InPreferredConfig = Bitmap.Config.Argb8888, InPremultiplied = true };
        using Bitmap decoded = BitmapFactory.DecodeByteArray(png, 0, png.Length, options)
            ?? throw new InvalidOperationException("Invalid animated PNG frame.");
        return (ReadPixels(decoded), decoded.Width, decoded.Height);
    }

    // Animation frames at the target size, filtered as single images are (Bitmap.createScaledBitmap).
    private static byte[] Scale(byte[] rgba, int width, int height, int targetWidth, int targetHeight)
    {
        using Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!)!;
        using (ByteBuffer pixels = ByteBuffer.Wrap(rgba)!)
        {
            bitmap.CopyPixelsFromBuffer(pixels);
        }

        using Bitmap scaled = Bitmap.CreateScaledBitmap(bitmap, targetWidth, targetHeight, true)!;
        return ReadPixels(scaled);
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


}

/// <summary>
/// The app's Android assets (the AndroidAsset items) as the core's asset source: the app's own assets
/// (`avares://<app>/<path>`) at `<path>`, another package's (`avares://<package>/<path>`, a Dart
/// `packages/<package>/<path>` key) at `<package>/<path>`.
/// </summary>
internal sealed class AndroidAssetSource(global::Android.Content.Res.AssetManager assets) : IAssetSource
{
    public Stream Open(Uri uri, Uri? baseUri)
    {
        using Stream asset = assets.Open(AssetPath(uri.IsAbsoluteUri ? uri : new Uri(baseUri!, uri)));
        var copy = new MemoryStream();
        asset.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    public IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri)
    {
        Uri absolute = directory.IsAbsoluteUri ? directory : new Uri(baseUri!, directory);
        return (assets.List(AssetPath(absolute).TrimEnd('/')) ?? []).Select(name => new Uri(absolute, name));
    }

    private static string AssetPath(Uri absolute)
    {
        string path = Uri.UnescapeDataString(absolute.AbsolutePath).TrimStart('/');
        string package = PackageOf(absolute);
        bool ownAsset = package.Length == 0
            || string.Equals(package, PlatformAssetBundle.ApplicationAssemblyName, StringComparison.OrdinalIgnoreCase);
        return ownAsset ? path : $"{package}/{path}";
    }

    // Uri.Host is lower-cased, and Android asset paths are case-sensitive: the authority as written.
    private static string PackageOf(Uri absolute)
    {
        string original = absolute.OriginalString;
        int start = original.IndexOf("://", StringComparison.Ordinal);
        if (start < 0)
        {
            return absolute.Host;
        }

        start += 3;
        int end = original.IndexOf('/', start);
        return end < 0 ? original[start..] : original[start..end];
    }
}
