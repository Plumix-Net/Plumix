using CoreGraphics;
using Foundation;
using ImageIO;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using UIKit;
using IOPath = System.IO.Path;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The core's image seam (UI/ImageBackend.cs) on ImageIO and
// CoreGraphics: images decode into premultiplied RGBA8888 at the requested size, animated PNG goes through
// ApngCodec (frames decoded here), and the app bundle is the asset source. Prototype: other animated formats show
// their first frame.

/// <summary>Decodes images with ImageIO.</summary>
internal sealed class IosImageBackend : IImageBackend
{
    public Task<Codec> InstantiateImageCodecWithSize(
        ImmutableBuffer buffer,
        TargetImageSizeCallback? getTargetSize)
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

            using CGImage image = Decode(bytes);
            (int width, int height) = DecodedImages.TargetSize((int)image.Width, (int)image.Height, getTargetSize);
            return Task.FromResult(DecodedImages.SingleFrame(Draw(image, width, height), width, height));
        }
        finally
        {
            buffer.Dispose();
        }
    }

    public Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format) =>
        DecodedImages.ToByteData(image, format, EncodePng);

    private static CGImage Decode(byte[] bytes)
    {
        using NSData data = NSData.FromArray(bytes);
        using CGImageSource source = CGImageSource.FromData(data)
            ?? throw new ArgumentException("Invalid image data", nameof(bytes));
        return source.CreateImage(0, new CGImageOptions())
            ?? throw new ArgumentException("Invalid image data", nameof(bytes));
    }

    // Premultiplied RGBA rows, top to bottom, drawn at the given size.
    private static byte[] Draw(CGImage image, int width, int height)
    {
        byte[] rgba = new byte[width * height * 4];
        using CGColorSpace colorSpace = CGColorSpace.CreateSrgb()!;
        using var context = new CGBitmapContext(
            rgba, width, height, 8, width * 4, colorSpace, CGImageAlphaInfo.PremultipliedLast);
        context.InterpolationQuality = CGInterpolationQuality.High;
        context.DrawImage(new CGRect(0, 0, width, height), image);
        return rgba;
    }

    private static CGImage ImageOf(byte[] rgba, int width, int height)
    {
        using CGColorSpace colorSpace = CGColorSpace.CreateSrgb()!;
        using var provider = new CGDataProvider(rgba);
        return new CGImage(
            width, height, 8, 32, width * 4, colorSpace,
            CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrderDefault, provider, null, false,
            CGColorRenderingIntent.Default);
    }

    private static (byte[] Rgba, int Width, int Height) DecodePng(byte[] png)
    {
        using CGImage image = Decode(png);
        return (Draw(image, (int)image.Width, (int)image.Height), (int)image.Width, (int)image.Height);
    }

    private static byte[] Scale(byte[] rgba, int width, int height, int targetWidth, int targetHeight)
    {
        using CGImage image = ImageOf(rgba, width, height);
        return Draw(image, targetWidth, targetHeight);
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height)
    {
        using CGImage image = ImageOf(rgba, width, height);
        using var uiImage = new UIImage(image);
        using NSData png = uiImage.AsPNG() ?? throw new InvalidOperationException("PNG encoding failed.");
        return png.ToArray();
    }
}

/// <summary>
/// The app bundle's resources as the core's asset source: the app's own assets at `&lt;path&gt;`, another
/// package's (a Dart `packages/&lt;package&gt;/&lt;path&gt;` key) at `&lt;package&gt;/&lt;path&gt;`.
/// </summary>
internal sealed class IosAssetSource : IAssetSource
{
    private readonly string _root = NSBundle.MainBundle.ResourcePath ?? NSBundle.MainBundle.BundlePath;

    public Stream Open(Uri uri, Uri? baseUri) =>
        File.OpenRead(IOPath.Combine(_root, AssetPath(uri.IsAbsoluteUri ? uri : new Uri(baseUri!, uri))));

    public IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri)
    {
        Uri absolute = directory.IsAbsoluteUri ? directory : new Uri(baseUri!, directory);
        string path = IOPath.Combine(_root, AssetPath(absolute));
        return Directory.Exists(path)
            ? Directory.EnumerateFileSystemEntries(path).Select(entry => new Uri(absolute, IOPath.GetFileName(entry)))
            : [];
    }

    private static string AssetPath(Uri absolute)
    {
        string path = Uri.UnescapeDataString(absolute.AbsolutePath).TrimStart('/');
        string original = absolute.OriginalString;
        int start = original.IndexOf("://", StringComparison.Ordinal);
        string package = absolute.Host;
        if (start >= 0)
        {
            start += 3;
            int end = original.IndexOf('/', start);
            package = end < 0 ? original[start..] : original[start..end];
        }

        bool ownAsset = package.Length == 0
            || string.Equals(package, PlatformAssetBundle.ApplicationAssemblyName, StringComparison.OrdinalIgnoreCase);
        return ownAsset ? path : IOPath.Combine(package, path);
    }
}
