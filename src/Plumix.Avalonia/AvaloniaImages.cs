using Avalonia.Media;
using Avalonia.Media.Imaging;

// C#-only host infrastructure: the Avalonia backend's view of a RasterImage (an Avalonia IImage, a Bitmap
// for every image Plumix decodes or rasterizes). The framework only carries the handle (RasterImage.Native);
// Plumix.Avalonia wraps and unwraps it (docs/product avalonia-free-core.md, stage 3).

namespace Plumix.UI;

/// <summary>Wraps Avalonia images as <see cref="RasterImage"/>s and unwraps them.</summary>
internal static class AvaloniaImages
{
    /// <summary>A handle to <paramref name="image"/>, measured the way Avalonia measures it.</summary>
    internal static RasterImage Wrap(IImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        int width = image is Bitmap bitmap ? bitmap.PixelSize.Width : (int)Math.Ceiling(image.Size.Width);
        int height = image is Bitmap sized ? sized.PixelSize.Height : (int)Math.Ceiling(image.Size.Height);
        // Decoded bitmaps are 96 DPI: one canvas unit per pixel, as RenderImage and ImageInfo measure it.
        return new RasterImage(image, width, height, image.Size.ToPlumix());
    }

    extension(RasterImage image)
    {
        /// <summary>The backend image the pixels live in.</summary>
        internal IImage NativeImage => (IImage)image.Native;

        /// <summary>The backend image as a bitmap; images Plumix decodes or rasterizes always are.</summary>
        internal Bitmap NativeBitmap => (Bitmap)image.Native;
    }
}
