using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.UI;

namespace Plumix.Tests;

// C#-only test infrastructure: installs Avalonia's Skia render interface for the whole test assembly, so
// scene rasterization (`Scene.ToImageSync`, `OffsetLayer.ToImage`, the scene rasterizer) runs headlessly
// the way flutter_test rasterizes with the engine. Text keeps the headless `FlutterTest` paragraph engine:
// the paragraph backend probe runs before Skia registers its font manager.
internal static class RasterBackend
{
    private static readonly Vector Dpi = new(96.0, 96.0);

    [ModuleInitializer]
    internal static void Initialize()
    {
        ParagraphBackend.EnsureProbed();
        Avalonia.Skia.SkiaPlatform.Initialize();
    }

    /// <summary>The premultiplied BGRA bytes of <paramref name="bitmap"/>, row by row, whatever its format.</summary>
    public static byte[] ReadPixels(Bitmap bitmap)
    {
        PixelSize size = bitmap.PixelSize;
        int rowBytes = size.Width * 4;
        byte[] pixels = new byte[rowBytes * size.Height];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(
            pixels,
            System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, rowBytes);
        }
        finally
        {
            handle.Free();
        }

        if (bitmap.Format == Avalonia.Platform.PixelFormat.Rgba8888)
        {
            for (int index = 0; index < pixels.Length; index += 4)
            {
                (pixels[index], pixels[index + 2]) = (pixels[index + 2], pixels[index]);
            }
        }

        return pixels;
    }

    /// <summary>The (R, G, B, A) of the pixel at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static (byte R, byte G, byte B, byte A) PixelAt(Bitmap bitmap, int x, int y)
    {
        byte[] pixels = ReadPixels(bitmap);
        int index = ((y * bitmap.PixelSize.Width) + x) * 4;
        return (pixels[index + 2], pixels[index + 1], pixels[index], pixels[index + 3]);
    }
}
