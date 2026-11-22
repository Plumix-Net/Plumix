using Plumix.Foundation;

// C#-only infrastructure; no Dart parity source. The engine decodes images and reads their pixels back
// with its own codecs; Plumix asks the host's drawing backend through these seams, so the framework
// names no host type (docs/product avalonia-free-core.md, stage 3). Plumix.Avalonia installs the
// Avalonia/Skia implementations (AvaloniaImageBackend, AvaloniaAssetSource).

namespace Plumix.UI;

/// <summary>The drawing backend's image codecs and pixel access.</summary>
internal interface IImageBackend
{
    /// <summary>
    /// dart:ui's <c>instantiateImageCodecWithSize</c>: a codec over <paramref name="buffer"/>, which it
    /// disposes, decoding at the size <paramref name="getTargetSize"/> picks from the intrinsic one.
    /// </summary>
    Task<Codec> InstantiateImageCodecWithSize(ImmutableBuffer buffer, TargetImageSizeCallback? getTargetSize);

    /// <summary>dart:ui's <c>Image.toByteData</c>.</summary>
    Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format);
}

/// <summary>The host's bundled assets (Flutter's asset manifest), addressed by URI.</summary>
internal interface IAssetSource
{
    Stream Open(Uri uri, Uri? baseUri);

    IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri);
}

/// <summary>The image backend and asset source the host installed.</summary>
internal static class ImageBackend
{
    internal static IImageBackend Current { get; set; } = Missing.Instance;

    internal static IAssetSource Assets { get; set; } = Missing.Instance;

    /// <summary>Whether a host installed its asset source.</summary>
    internal static bool HasHostAssets => !ReferenceEquals(Assets, Missing.Instance);

    private sealed class Missing : IImageBackend, IAssetSource
    {
        internal static readonly Missing Instance = new();

        public Task<Codec> InstantiateImageCodecWithSize(
            ImmutableBuffer buffer, TargetImageSizeCallback? getTargetSize) => throw NoHost();

        public Task<ByteData?> ToByteData(RasterImage image, ImageByteFormat format) => throw NoHost();

        public Stream Open(Uri uri, Uri? baseUri) => throw NoHost();

        public IEnumerable<Uri> GetAssets(Uri directory, Uri? baseUri) => throw NoHost();

        private static InvalidOperationException NoHost() => new(
            "No Plumix host is running. Start the app through a host package such as Plumix.Avalonia "
            + "(PlumixApplication, WidgetHost).");
    }
}
