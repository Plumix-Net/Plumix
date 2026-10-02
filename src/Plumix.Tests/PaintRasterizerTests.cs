using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

// C#-only test infrastructure coverage: the raster backend behind dart:ui's `Paint.maskFilter` and
// `Paint.blendMode` (src/Plumix/UI/PaintRasterizer.cs) and the `Paint`-taking canvas draws over it.
public sealed class PaintRasterizerTests
{
    [Fact]
    public void BlurAlpha_ZeroSigma_IsTheIdentity()
    {
        byte[] mask = [0, 10, 255, 30];
        Assert.Equal(mask, PaintRasterizer.BlurAlpha(mask, 2, 2, 0.0));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(3.0)]
    [InlineData(6.5)]
    public void BlurAlpha_IsSymmetricAndPreservesCoverage(double sigma)
    {
        int extent = (int)PaintRasterizer.BlurExtent(sigma);
        int size = (extent * 2) + 5;
        byte[] mask = new byte[size * size];
        int center = size / 2;
        for (int y = center - 2; y <= center + 2; y++)
        {
            for (int x = center - 2; x <= center + 2; x++)
            {
                mask[(y * size) + x] = 255;
            }
        }

        byte[] blurred = PaintRasterizer.BlurAlpha(mask, size, size, sigma);

        Assert.Equal(blurred[(center * size) + center - 3], blurred[(center * size) + center + 3]);
        Assert.Equal(blurred[((center - 3) * size) + center], blurred[((center + 3) * size) + center]);
        Assert.Equal(blurred[(center * size) + center - 3], blurred[((center - 3) * size) + center]);
        long before = mask.Sum(static value => (long)value);
        long after = blurred.Sum(static value => (long)value);
        Assert.InRange(after, before * 0.97, before * 1.03);
        Assert.True(blurred[(center * size) + center] < 255, "the blur softens the center of a small square");
        Assert.True(blurred[center * size] < 3, "the 3-sigma pad holds the blur");
    }

    [Fact]
    public void BlurAlpha_ThreeBoxApproximation_TracksTheGaussianEdge()
    {
        // A vertical step edge blurred with sigma 4 follows the Gaussian CDF; the three-box passes Skia
        // (and SVG's feGaussianBlur) use for sigma >= 2 stay within a few levels of it.
        const double sigma = 4.0;
        const int width = 61;
        const int height = 61;
        byte[] mask = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 30; x < width; x++)
            {
                mask[(y * width) + x] = 255;
            }
        }

        // The middle row is far enough from the top and bottom edges to see only the vertical edge.
        byte[] blurred = PaintRasterizer.BlurAlpha(mask, width, height, sigma)[(30 * width)..(31 * width)];
        for (int x = 18; x < 43; x++)
        {
            // Pixel x covers [x, x + 1); the step sits at 30.
            double expected = 255.0 * Phi((x + 0.5 - 30.0) / sigma);
            Assert.InRange(blurred[x], expected - 8.0, expected + 8.0);
        }
    }

    [Fact]
    public void ApplyBlurStyle_MergesLikeSkiaBlurMask()
    {
        byte[] source = [0, 255, 128, 0];
        byte[] blurred = [100, 200, 64, 0];

        Assert.Equal(blurred, PaintRasterizer.ApplyBlurStyle(source, blurred, BlurStyle.Normal));
        // solid: src + blur * (1 - src)
        Assert.Equal(new byte[] { 100, 255, 160, 0 }, PaintRasterizer.ApplyBlurStyle(source, blurred, BlurStyle.Solid));
        // outer: blur * (1 - src)
        Assert.Equal(new byte[] { 100, 0, 32, 0 }, PaintRasterizer.ApplyBlurStyle(source, blurred, BlurStyle.Outer));
        // inner: blur * src
        Assert.Equal(new byte[] { 0, 200, 32, 0 }, PaintRasterizer.ApplyBlurStyle(source, blurred, BlurStyle.Inner));
    }

    [Fact]
    public void ColorizeMask_ProducesPremultipliedBgra()
    {
        byte[] pixels = PaintRasterizer.ColorizeMask([255, 128, 0], new Color(0x80FF4000));

        Assert.Equal(new byte[] { 0, 32, 128, 128 }, pixels[..4]);
        Assert.Equal(new byte[] { 0, 16, 64, 64 }, pixels[4..8]);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, pixels[8..]);
    }

    [Fact]
    public void ShadeMask_ScalesShaderPixelsByCoverage()
    {
        byte[] shader = [200, 100, 50, 255, 200, 100, 50, 255];
        byte[] shaded = PaintRasterizer.ShadeMask([255, 0], shader);
        Assert.Equal(new byte[] { 200, 100, 50, 255, 0, 0, 0, 0 }, shaded);
    }

    [Fact]
    public void Canvas_PaintDraws_RecordTheirPaint()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        var blur = MaskFilter.Blur(BlurStyle.Outer, 3.0);
        canvas.DrawRect(new Rect(0, 0, 10, 10), new Paint { Color = new Color(0xFF112233), MaskFilter = blur });
        canvas.DrawOval(new Rect(0, 0, 10, 20), new Paint { BlendMode = BlendMode.Multiply });
        var path = new Path();
        path.AddRect(new Rect(0, 0, 5, 5));
        canvas.DrawPath(path, new Paint { Style = PaintingStyle.Stroke, StrokeWidth = 2.0 });
        canvas.DrawLine(new Point(0, 0), new Point(5, 5), new Paint { StrokeWidth = 3.0 });
        canvas.DrawDRRect(
            RRect.FromRectAndRadius(new Rect(0, 0, 20, 20), 4.0),
            RRect.FromRectAndRadius(new Rect(5, 5, 10, 10), 2.0),
            new Paint());
        canvas.DrawRSuperellipseBlur(
            RSuperellipse.FromRectAndRadius(new Rect(0, 0, 20, 20), Radius.Circular(5.0)),
            new Color(0x40000000),
            2.0);

        IReadOnlyList<CanvasCall> calls = canvas.DebugCalls;
        Assert.Equal(
            ["drawRect", "drawOval", "drawPath", "drawLine", "drawDRRect", "drawRSuperellipse"],
            calls.Select(call => call.Method));
        Assert.Equal(blur, calls[0].MaskFilter);
        Assert.Equal(new Color(0xFF112233), calls[0].Color);
        Assert.Equal(BlendMode.Multiply, calls[1].BlendMode);
        Assert.Equal(PaintingStyle.Stroke, calls[2].Style);
        Assert.Equal(2.0, calls[2].StrokeWidth);
        // drawLine always strokes, whatever the paint's style.
        Assert.Equal(PaintingStyle.Stroke, calls[3].Style);
        Assert.Equal(3.0, calls[3].StrokeWidth);
        Assert.Equal(MaskFilter.Blur(BlurStyle.Normal, 2.0), calls[5].MaskFilter);
        Assert.Equal(6, recorder.EndRecording().DrawCommandCount);
    }

    [Fact]
    public void Canvas_PaintIsReadWhenTheCallIsMade()
    {
        var canvas = new Canvas(new PictureRecorder());
        var paint = new Paint { Color = new Color(0xFF00FF00) };
        canvas.DrawCircle(new Point(5, 5), 5, paint);
        paint.Color = new Color(0xFFFF0000);
        paint.MaskFilter = MaskFilter.Blur(BlurStyle.Normal, 1.0);

        CanvasCall call = Assert.Single(canvas.DebugCalls);
        Assert.Equal(new Color(0xFF00FF00), call.Color);
        Assert.Null(call.MaskFilter);
    }

    [Fact]
    public void RasterScale_FollowsTheImplicitViewClampedToTheSupportedRange()
    {
        double scale = PaintRasterizer.RasterScale;
        Assert.InRange(scale, 1.0, 4.0);
    }

    [Fact]
    public void BlurExtent_IsSkiasThreeSigmaPad()
    {
        Assert.Equal(0.0, PaintRasterizer.BlurExtent(0.0));
        Assert.Equal(3.0, PaintRasterizer.BlurExtent(1.0));
        Assert.Equal(9.0, PaintRasterizer.BlurExtent(2.9));
    }

    // The standard normal CDF (Abramowitz-Stegun 7.1.26 through erf).
    private static double Phi(double x)
    {
        double z = x / Math.Sqrt(2.0);
        double t = 1.0 / (1.0 + (0.3275911 * Math.Abs(z)));
        double erf = 1.0 - ((((((1.061405429 * t) - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t
                            + 0.254829592) * t * Math.Exp(-z * z);
        return 0.5 * (1.0 + (z >= 0 ? erf : -erf));
    }
}
