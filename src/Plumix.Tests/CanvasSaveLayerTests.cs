using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: dart:ui Canvas.saveLayer, Canvas.drawImage/drawImageRect/drawImageNine (pixel behavior
// through Avalonia's Skia backend) and the engine's ClipShapeLayer `antiAliasWithSaveLayer` path.

namespace Plumix.Tests;

public sealed class CanvasSaveLayerTests
{
    private static readonly Color Red = new(0xFFFF0000);
    private static readonly Color Green = new(0xFF00FF00);
    private static readonly Color Blue = new(0xFF0000FF);

    private static Bitmap Render(Action<Canvas> paint, int width = 20, int height = 20)
    {
        var recorder = new PictureRecorder();
        paint(new Canvas(recorder));
        return recorder.EndRecording().ToImageSync(width, height);
    }

    [Fact]
    public void SaveLayer_IsolatesItsGroupFromTheBackdrop()
    {
        // A destination-out draw erases what is under it. Inside a layer it erases only the layer's own
        // (empty) content, so the red backdrop survives the restore.
        Bitmap isolated = Render(canvas =>
        {
            canvas.DrawRect(new Rect(0, 0, 20, 20), new Paint { Color = Red });
            canvas.SaveLayer(new Rect(0, 0, 20, 20), new Paint());
            canvas.DrawRect(new Rect(0, 0, 10, 20), new Paint { Color = Blue, BlendMode = BlendMode.DestinationOut });
            canvas.Restore();
        });
        Bitmap direct = Render(canvas =>
        {
            canvas.DrawRect(new Rect(0, 0, 20, 20), new Paint { Color = Red });
            canvas.DrawRect(new Rect(0, 0, 10, 20), new Paint { Color = Blue, BlendMode = BlendMode.DestinationOut });
        });

        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(isolated, 5, 10));
        Assert.Equal(0, RasterBackend.PixelAt(direct, 5, 10).A);
    }

    [Fact]
    public void SaveLayer_AppliesThePaintAlphaToTheWholeGroup()
    {
        // Two overlapping opaque rects in a half-transparent layer: the overlap shows only the top rect at
        // half alpha, as the group is composited once.
        Bitmap image = Render(canvas =>
        {
            canvas.SaveLayer(null, new Paint { Color = Color.FromARGB(128, 0, 0, 0) });
            canvas.DrawRect(new Rect(0, 0, 15, 20), new Paint { Color = Red });
            canvas.DrawRect(new Rect(5, 0, 15, 20), new Paint { Color = Blue });
            canvas.Restore();
        });

        (byte R, byte G, byte B, byte A) overlap = RasterBackend.PixelAt(image, 10, 10);
        Assert.InRange(overlap.A, 126, 130);
        Assert.InRange(overlap.B, 126, 130);
        Assert.Equal(0, overlap.R);
        Assert.Equal(0, overlap.G);
        Assert.InRange(RasterBackend.PixelAt(image, 2, 10).R, 126, 130);
    }

    [Fact]
    public void SaveLayer_ColorFilter_FiltersTheGroup()
    {
        Bitmap image = Render(canvas =>
        {
            canvas.SaveLayer(
                new Rect(0, 0, 20, 20),
                new Paint { ColorFilter = new ColorFilter.Mode(Green, BlendMode.SourceIn) });
            canvas.DrawRect(new Rect(0, 0, 10, 20), new Paint { Color = Red });
            canvas.Restore();
        });

        Assert.Equal(((byte)0, (byte)0xFF, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 5, 10));
        Assert.Equal(0, RasterBackend.PixelAt(image, 15, 10).A);
    }

    [Fact]
    public void SaveLayer_NullBounds_IsBoundedByTheClip()
    {
        Bitmap image = Render(canvas =>
        {
            canvas.ClipRect(new Rect(0, 0, 10, 20));
            canvas.SaveLayer(null, new Paint { InvertColors = true });
            canvas.DrawRect(new Rect(0, 0, 20, 20), new Paint { Color = Red });
            canvas.Restore();
        });

        // invertColors turns opaque red into cyan.
        Assert.Equal(((byte)0, (byte)0xFF, (byte)0xFF, (byte)0xFF), RasterBackend.PixelAt(image, 5, 10));
        Assert.Equal(0, RasterBackend.PixelAt(image, 15, 10).A);
    }

    [Fact]
    public void SaveLayer_BlendMode_CompositesTheGroupUnderIt()
    {
        Bitmap image = Render(canvas =>
        {
            canvas.DrawRect(new Rect(0, 0, 20, 20), new Paint { Color = Red });
            canvas.SaveLayer(new Rect(0, 0, 20, 20), new Paint { BlendMode = BlendMode.Plus });
            canvas.DrawRect(new Rect(0, 0, 10, 20), new Paint { Color = Green });
            canvas.Restore();
        });

        Assert.Equal(((byte)0xFF, (byte)0xFF, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 5, 10));
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 15, 10));
    }

    [Fact]
    public void SaveLayer_ImageFilter_BlursTheGroup()
    {
        Bitmap image = Render(canvas =>
        {
            canvas.SaveLayer(new Rect(0, 0, 20, 20), new Paint { ImageFilter = new ImageFilter.Blur(2.0, 2.0) });
            canvas.DrawRect(new Rect(0, 0, 10, 20), new Paint { Color = Red });
            canvas.Restore();
        });

        byte edge = RasterBackend.PixelAt(image, 10, 10).A;
        Assert.InRange(edge, 40, 215);
        Assert.Equal(0xFF, RasterBackend.PixelAt(image, 2, 10).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 18, 10).A);
    }

    [Fact]
    public void SaveLayer_RecordsItsBoundsAndPaint()
    {
        var canvas = new Canvas(new PictureRecorder());
        canvas.SaveLayer(new Rect(1, 2, 3, 4), new Paint { Color = Red, BlendMode = BlendMode.Multiply });
        canvas.Restore();

        CanvasCall call = canvas.DebugCalls[0];
        Assert.Equal("saveLayer", call.Method);
        Assert.Equal(new Rect(1, 2, 3, 4), call.Rect);
        Assert.Equal(BlendMode.Multiply, call.BlendMode);
        Assert.Equal(Red, call.Color);
    }

    [Fact]
    public void ClipContext_AntiAliasWithSaveLayer_IsolatesThePainter()
    {
        Bitmap image = Render(canvas =>
        {
            canvas.DrawRect(new Rect(0, 0, 20, 20), new Paint { Color = Red });
            new CanvasClipContext(canvas).ClipRectAndPaint(
                new Rect(0, 0, 20, 20),
                Clip.AntiAliasWithSaveLayer,
                new Rect(0, 0, 20, 20),
                () => canvas.DrawRect(
                    new Rect(0, 0, 10, 20),
                    new Paint { Color = Blue, BlendMode = BlendMode.DestinationOut }));
        });

        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 5, 10));
        Assert.Contains(PaintRecordingFor(Clip.AntiAliasWithSaveLayer), call => call.Method == "saveLayer");
        Assert.DoesNotContain(PaintRecordingFor(Clip.AntiAlias), call => call.Method == "saveLayer");
    }

    [Fact]
    public void ClipRectLayer_AntiAliasWithSaveLayer_IsolatesItsChildren()
    {
        static Bitmap Scene(Clip clip)
        {
            var root = new OffsetLayer();
            root.Append(new PictureLayer(default)
            {
                Picture = SceneBuilderTests.DrawRectPicture(new Rect(0, 0, 20, 20), 0xFFFF0000),
            });
            var clipLayer = new ClipRectLayer { ClipRect = new Rect(0, 0, 20, 20), ClipBehavior = clip };
            var recorder = new PictureRecorder();
            new Canvas(recorder).DrawRect(
                new Rect(0, 0, 10, 20),
                new Paint { Color = Blue, BlendMode = BlendMode.DestinationOut });
            clipLayer.Append(new PictureLayer(default) { Picture = recorder.EndRecording() });
            root.Append(clipLayer);
            using Scene scene = root.BuildScene(new SceneBuilder());
            return scene.ToImageSync(20, 20);
        }

        Assert.Equal(0xFF, RasterBackend.PixelAt(Scene(Clip.AntiAliasWithSaveLayer), 5, 10).A);
        Assert.Equal(0, RasterBackend.PixelAt(Scene(Clip.AntiAlias), 5, 10).A);
    }

    [Fact]
    public void DrawImageRect_AppliesThePaintAlphaColorFilterAndInversion()
    {
        WriteableBitmap white = SolidBitmap(4, 4, 0xFFFFFFFF);

        Bitmap faded = Render(canvas => canvas.DrawImageRect(
            white,
            new Rect(0, 0, 4, 4),
            new Rect(0, 0, 20, 20),
            new Paint { Color = Color.FromRGBO(0, 0, 0, 0.5) }));
        Bitmap filtered = Render(canvas => canvas.DrawImageRect(
            white,
            new Rect(0, 0, 4, 4),
            new Rect(0, 0, 20, 20),
            new Paint { ColorFilter = new ColorFilter.Mode(Red, BlendMode.SourceIn) }));
        Bitmap inverted = Render(canvas => canvas.DrawImageRect(
            white,
            new Rect(0, 0, 4, 4),
            new Rect(0, 0, 20, 20),
            new Paint { InvertColors = true }));

        Assert.InRange(RasterBackend.PixelAt(faded, 10, 10).A, 126, 130);
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(filtered, 10, 10));
        Assert.Equal(((byte)0, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(inverted, 10, 10));
    }

    [Fact]
    public void DrawImageRect_PlusBlendInsideALayer_AddsTheImages()
    {
        // The DecorationImage cross-fade: a at (1 - t) source-over, b at t plus-blended, in one layer.
        WriteableBitmap green = SolidBitmap(2, 2, 0xFF00FF00);
        WriteableBitmap red = SolidBitmap(2, 2, 0xFFFF0000);
        Bitmap image = Render(canvas =>
        {
            canvas.SaveLayer(null, new Paint());
            canvas.DrawImageRect(green, new Rect(0, 0, 2, 2), new Rect(0, 0, 20, 20), new Paint
            {
                Color = Color.FromRGBO(0, 0, 0, 0.5),
            });
            canvas.DrawImageRect(red, new Rect(0, 0, 2, 2), new Rect(0, 0, 20, 20), new Paint
            {
                Color = Color.FromRGBO(0, 0, 0, 0.5),
                BlendMode = BlendMode.Plus,
            });
            canvas.Restore();
        });

        (byte R, byte G, byte B, byte A) pixel = RasterBackend.PixelAt(image, 10, 10);
        Assert.InRange(pixel.R, 126, 130);
        Assert.InRange(pixel.G, 126, 130);
        Assert.Equal(0, pixel.B);
        Assert.InRange(pixel.A, 253, 255);
    }

    [Fact]
    public void DrawImageNine_KeepsTheCornersAndStretchesTheMiddle()
    {
        (Rect Source, Rect Destination)[] patches = Canvas.NinePatches(
            new Size(30, 30),
            new Rect(10, 10, 10, 10),
            new Rect(0, 0, 100, 60)).ToArray();

        Assert.Equal(9, patches.Length);
        Assert.Equal((new Rect(0, 0, 10, 10), new Rect(0, 0, 10, 10)), patches[0]);
        Assert.Equal((new Rect(10, 10, 10, 10), new Rect(10, 10, 80, 40)), patches[4]);
        Assert.Equal((new Rect(20, 20, 10, 10), new Rect(90, 50, 10, 10)), patches[8]);
    }

    [Fact]
    public void DrawImageNine_ShrinksTheCornersWhenTheDestinationIsTooSmall()
    {
        (Rect Source, Rect Destination)[] patches = Canvas.NinePatches(
            new Size(30, 30),
            new Rect(10, 10, 10, 10),
            new Rect(0, 0, 10, 10)).ToArray();

        // The middle row and column collapse; each corner takes half the destination.
        Assert.Equal(4, patches.Length);
        Assert.Equal(new Rect(0, 0, 5, 5), patches[0].Destination);
        Assert.Equal(new Rect(5, 5, 5, 5), patches[3].Destination);
    }

    [Fact]
    public void DrawImage_RecordsItsPaint()
    {
        var canvas = new Canvas(new PictureRecorder());
        var paint = new Paint { Color = Color.FromRGBO(0, 0, 0, 0.25), FilterQuality = FilterQuality.High };
        WriteableBitmap image = SolidBitmap(4, 2, 0xFFFFFFFF);

        canvas.DrawImage(image, new Point(3, 4), paint);
        paint.FilterQuality = FilterQuality.None;

        CanvasCall call = Assert.Single(canvas.DebugCalls);
        Assert.Equal("drawImage", call.Method);
        Assert.Equal(new Point(3, 4), call.Offset);
        Assert.Same(image, call.Image);
        Assert.Equal(FilterQuality.High, call.Paint!.FilterQuality);
    }

    private static IReadOnlyList<CanvasCall> PaintRecordingFor(Clip clip)
    {
        var canvas = new Canvas(new PictureRecorder());
        new CanvasClipContext(canvas).ClipRectAndPaint(new Rect(0, 0, 5, 5), clip, new Rect(0, 0, 5, 5), () => { });
        return canvas.DebugCalls;
    }

    private static WriteableBitmap SolidBitmap(int width, int height, uint argb)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        byte[] row = new byte[width * 4];
        for (int x = 0; x < width; x++)
        {
            row[(x * 4) + 0] = (byte)argb;
            row[(x * 4) + 1] = (byte)(argb >> 8);
            row[(x * 4) + 2] = (byte)(argb >> 16);
            row[(x * 4) + 3] = (byte)(argb >> 24);
        }

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(row, 0, framebuffer.Address + (y * framebuffer.RowBytes), row.Length);
        }

        return bitmap;
    }

    private sealed class CanvasClipContext(Canvas canvas) : Plumix.Painting.ClipContext
    {
        public override Canvas Canvas => canvas;
    }
}
