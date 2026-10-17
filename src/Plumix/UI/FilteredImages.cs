using System.Runtime.CompilerServices;
using Avalonia.Media;
using Plumix.Rendering;

namespace Plumix.UI;

// C#-only infrastructure: the colour-filtered copies of images that `Canvas.drawImage*` paints with a
// `Paint.colorFilter` or `Paint.invertColors` draw. Avalonia's drawing context cannot filter a bitmap
// draw, so the image's pixels are filtered once per (image, filter) and the copy is drawn instead.

/// <summary>A cache of colour-filtered image copies, kept alive as long as the source image.</summary>
internal static class FilteredImages
{
    private static readonly ConditionalWeakTable<IImage, Dictionary<(ColorFilter?, bool), IImage>> Cache = new();

    /// <summary>
    /// <paramref name="image"/> filtered by <paramref name="colorFilter"/> and then inverted when
    /// <paramref name="invertColors"/>; the image itself when no raster backend can filter it.
    /// </summary>
    internal static IImage Get(IImage image, ColorFilter? colorFilter, bool invertColors)
    {
        Dictionary<(ColorFilter?, bool), IImage> entries = Cache.GetOrCreateValue(image);
        (ColorFilter?, bool) key = (colorFilter, invertColors);
        lock (entries)
        {
            if (entries.TryGetValue(key, out IImage? cached))
            {
                return cached;
            }
        }

        IImage filtered;
        try
        {
            filtered = FilterLayerRasterizer.FilterImage(image, colorFilter, invertColors) ?? image;
        }
        catch (InvalidOperationException)
        {
            filtered = image;
        }
        catch (NotSupportedException)
        {
            filtered = image;
        }

        lock (entries)
        {
            entries[key] = filtered;
        }

        return filtered;
    }
}
