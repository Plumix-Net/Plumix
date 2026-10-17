using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/painting/debug.dart

namespace Plumix.Painting;

/// <summary>Signature for a method that receives the information about a painted image.</summary>
/// <remarks>Dart's <c>PaintImageCallback</c>; used by <see cref="PaintingDebug.DebugOnPaintImage"/>.</remarks>
public delegate void PaintImageCallback(ImageSizeInfo info);

/// <summary>Tracks the bytes used by a <c>dart:ui</c> image compared to the bytes needed to paint it.</summary>
/// <remarks>Dart's <c>ImageSizeInfo</c>; reported by <c>paintImage</c> to <see cref="PaintingDebug.DebugOnPaintImage"/>
/// and the <c>Flutter.ImageSizesForFrame</c> event.</remarks>
public sealed class ImageSizeInfo : IEquatable<ImageSizeInfo>
{
    /// <summary>Creates an object to track the backing size of a <c>dart:ui</c> image.</summary>
    public ImageSizeInfo(Size displaySize, Size imageSize, string? source = null)
    {
        Source = source;
        DisplaySize = displaySize;
        ImageSize = imageSize;
    }

    /// <summary>A unique identifier for this image, such as its asset path or network URL.</summary>
    public string? Source { get; }

    /// <summary>The size of the area the image will be rendered in.</summary>
    public Size DisplaySize { get; }

    /// <summary>The size the image has been decoded to.</summary>
    public Size ImageSize { get; }

    /// <summary>The number of bytes needed to render the image without scaling it.</summary>
    public int DisplaySizeInBytes => SizeToBytes(DisplaySize);

    /// <summary>The number of bytes used by the image in memory.</summary>
    public int DecodedSizeInBytes => SizeToBytes(ImageSize);

    // Assume 4 bytes per pixel and that mipmapping will be used, which adds 4/3.
    private static int SizeToBytes(Size size) => (int)(size.Width * size.Height * 4 * (4.0 / 3.0));

    /// <summary>Returns a JSON encodable representation of this object.</summary>
    public Dictionary<string, object?> ToJson() => new()
    {
        ["source"] = Source,
        ["displaySize"] = new Dictionary<string, object?>
        {
            ["width"] = DisplaySize.Width,
            ["height"] = DisplaySize.Height,
        },
        ["imageSize"] = new Dictionary<string, object?>
        {
            ["width"] = ImageSize.Width,
            ["height"] = ImageSize.Height,
        },
        ["displaySizeInBytes"] = DisplaySizeInBytes,
        ["decodedSizeInBytes"] = DecodedSizeInBytes,
    };

    /// <inheritdoc />
    public bool Equals(ImageSizeInfo? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.Source == Source
        && other.ImageSize == ImageSize
        && other.DisplaySize == DisplaySize;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ImageSizeInfo);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Source, DisplaySize, ImageSize);

    /// <inheritdoc />
    public override string ToString() =>
        $"ImageSizeInfo({Source ?? "null"}, imageSize: {DartFormat.SizeOf(ImageSize)}, "
        + $"displaySize: {DartFormat.SizeOf(DisplaySize)})";
}

public delegate bool ShaderWarmUpPictureCallback(Picture picture);
public delegate bool ShaderWarmUpImageCallback(Bitmap image);

/// <summary>The painting library's debug variables (Dart's top-level <c>painting/debug.dart</c>).</summary>
public static class PaintingDebug
{
    private const int ImageOverheadAllowanceDefault = 128 * 1024;

    /// <summary>If not null, called when the framework is about to paint an image to a canvas.</summary>
    /// <remarks>Dart's <c>debugOnPaintImage</c>.</remarks>
    public static PaintImageCallback? DebugOnPaintImage { get; set; }

    /// <summary>
    /// If true, the framework will color invert and horizontally flip images that have been decoded to a size
    /// taking at least <see cref="DebugImageOverheadAllowance"/> bytes more than necessary.
    /// </summary>
    /// <remarks>Dart's <c>debugInvertOversizedImages</c>.</remarks>
    public static bool DebugInvertOversizedImages { get; set; }

    /// <summary>
    /// The number of bytes an image must use before it triggers inversion when
    /// <see cref="DebugInvertOversizedImages"/> is true.
    /// </summary>
    /// <remarks>Dart's <c>debugImageOverheadAllowance</c>; defaults to 128KiB.</remarks>
    public static int DebugImageOverheadAllowance { get; set; } = ImageOverheadAllowanceDefault;

    public static ShaderWarmUpPictureCallback DebugCaptureShaderWarmUpPicture { get; set; } = _ => true;

    public static ShaderWarmUpImageCallback DebugCaptureShaderWarmUpImage { get; set; } = _ => true;

    /// <summary>Returns true if none of the painting library debug variables have been changed.</summary>
    /// <remarks>
    /// Dart's <c>debugAssertAllPaintingVarsUnset</c>. <c>debugDisableShadows</c> is
    /// <see cref="RenderingDebug.DisableShadows"/>; Plumix has no <c>debugNetworkImageHttpClientProvider</c>.
    /// </remarks>
    public static bool DebugAssertAllPaintingVarsUnset(string reason, bool debugDisableShadowsOverride = false)
    {
        if (Constants.KDebugMode
            && (RenderingDebug.DisableShadows != debugDisableShadowsOverride
                || DebugOnPaintImage is not null
                || DebugInvertOversizedImages
                || DebugImageOverheadAllowance != ImageOverheadAllowanceDefault))
        {
            throw new FlutterError(reason);
        }

        return true;
    }
}
