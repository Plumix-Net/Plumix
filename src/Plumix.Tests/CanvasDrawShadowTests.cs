using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.UI;
using SkiaSharp;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Canvas.drawShadow) as the engine's Skia
// backend renders it: DlSkCanvasDispatcher::DrawShadow over SkShadowUtils (tonal colours, ambient and directional
// spot meshes). Expected values are computed from the Skia formulas, not read back from the implementation.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class CanvasDrawShadowTests
{
    private static readonly Rect Occluder = new(20, 20, 60, 60);

    private static Bitmap Render(Action<Canvas> paint, double engineDevicePixelRatio = 1.0)
    {
        double? previous = PlatformDispatcher.Instance.EngineDevicePixelRatioOverride;
        PlatformDispatcher.Instance.EngineDevicePixelRatioOverride = engineDevicePixelRatio;
        try
        {
            var recorder = new PictureRecorder();
            paint(new Canvas(recorder));
            return recorder.EndRecording().ToImageSync(100, 100);
        }
        finally
        {
            PlatformDispatcher.Instance.EngineDevicePixelRatioOverride = previous;
        }
    }

    private static UI.Path RectPath(Rect rect)
    {
        var path = new UI.Path();
        path.AddRect(rect);
        return path;
    }

    [Fact]
    public void TonalColors_KeepABlackShadowGreyscale()
    {
        // DlSkCanvasDispatcher::DrawShadow: 0.039 and 0.25 of an opaque colour's alpha, truncated to 9 and 63.
        (SKColor ambient, SKColor spot) = ShadowDraw.ComputeTonalColors(
            new SKColor(0, 0, 0, 9), new SKColor(0, 0, 0, 63));

        Assert.Equal(new SKColor(0, 0, 0, 9), ambient);
        Assert.Equal(new SKColor(0, 0, 0, 63), spot);
    }

    [Fact]
    public void TonalColors_TintTheSpotShadowByLuminance()
    {
        // Pure red: luminance 0.5, so colorAlpha 0.4178 under alphaAdjust(0.247) and greyscaleAlpha 0.1976,
        // giving tonal alpha 0.5329 (136) and an unpremultiplied red of 0.6291 * 255 (160). Ambient drops the hue.
        (SKColor ambient, SKColor spot) = ShadowDraw.ComputeTonalColors(
            new SKColor(255, 0, 0, 9), new SKColor(255, 0, 0, 63));

        Assert.Equal(new SKColor(0, 0, 0, 9), ambient);
        Assert.Equal(new SKColor(160, 0, 0, 136), spot);
    }

    [Fact]
    public void AmbientMesh_OutsetsByHalfTheHeightAroundACentroidFan()
    {
        using var path = new SKPath();
        path.AddRect(new SKRect(0, 0, 100, 100));

        ShadowVertices? mesh = ShadowTessellator.MakeAmbient(
            path, SKMatrix.Identity, new ShadowPoint3(0, 0, 6), transparent: true);

        // AmbientBlurRadius(6) = 6 / 128 * 64 = 3; a transparent occluder fans its umbra from the centroid.
        Assert.NotNull(mesh);
        Assert.Equal(new SKPoint(50, 50), mesh.Positions[0]);
        Assert.Equal(255, mesh.Colors[0].Alpha);
        Assert.Equal(-3, mesh.Positions.Min(point => point.X), 4);
        Assert.Equal(103, mesh.Positions.Max(point => point.Y), 4);
        Assert.All(mesh.Colors, color => Assert.Contains(color.Alpha, new byte[] { 0, 255 }));
        Assert.Equal(0, mesh.Indices.Length % 3);
    }

    [Fact]
    public void SpotMesh_FollowsTheDirectionalLightDownward()
    {
        using var path = new SKPath();
        path.AddRect(new SKRect(0, 0, 100, 100));

        ShadowVertices? mesh = ShadowTessellator.MakeSpot(
            path,
            SKMatrix.Identity,
            new ShadowPoint3(0, 0, 6),
            new ShadowPoint3(0, -1, 1),
            800f / 600f,
            transparent: false,
            directional: true);

        // The light at (0, -1, 1) shifts the umbra down by z = 6 and blurs it by 4/3 * z = 8 on every side.
        Assert.NotNull(mesh);
        Assert.Equal(-8, mesh.Positions.Min(point => point.X), 3);
        Assert.Equal(108, mesh.Positions.Max(point => point.X), 3);
        Assert.Equal(-2, mesh.Positions.Min(point => point.Y), 3);
        Assert.Equal(114, mesh.Positions.Max(point => point.Y), 3);
    }

    [Fact]
    public void ConcaveOutlines_FallBackToTheBlurredPath()
    {
        using var path = new SKPath();
        path.MoveTo(0, 0);
        path.LineTo(100, 0);
        path.LineTo(100, 40);
        path.LineTo(40, 40);
        path.LineTo(40, 100);
        path.LineTo(0, 100);
        path.Close();

        Assert.Null(ShadowTessellator.MakeAmbient(path, SKMatrix.Identity, new ShadowPoint3(0, 0, 6), true));

        var occluder = new UI.Path();
        occluder.MoveTo(20, 20);
        occluder.LineTo(80, 20);
        occluder.LineTo(80, 40);
        occluder.LineTo(40, 40);
        occluder.LineTo(40, 80);
        occluder.LineTo(20, 80);
        occluder.Close();
        Bitmap image = Render(canvas => canvas.DrawShadow(occluder, Colors.Black, 4, transparentOccluder: false));

        Assert.True(RasterBackend.PixelAt(image, 30, 83).A > 0);
    }

    [Fact]
    public void TransparentOccluder_FillsTheUmbraWithBothShadows()
    {
        Bitmap image = Render(canvas =>
            canvas.DrawShadow(RectPath(Occluder), Colors.Black, 4, transparentOccluder: true));

        // Ambient 9/255 and spot 63/255 composite to about 70/255 in the middle of the umbra.
        Assert.InRange(RasterBackend.PixelAt(image, 50, 50).A, 69, 71);
    }

    [Fact]
    public void OpaqueOccluder_LeavesTheHiddenUmbraEmpty()
    {
        Bitmap image = Render(canvas =>
            canvas.DrawShadow(RectPath(Occluder), Colors.Black, 4, transparentOccluder: false));

        Assert.Equal(0, RasterBackend.PixelAt(image, 50, 50).A);
        // The spot shadow is pushed down 4 and blurred 16/3: it reaches y = 89.3 below but only 18.7 above.
        Assert.True(RasterBackend.PixelAt(image, 50, 85).A > RasterBackend.PixelAt(image, 50, 18).A);
        Assert.True(RasterBackend.PixelAt(image, 50, 88).A > 0);
        Assert.Equal(0, RasterBackend.PixelAt(image, 50, 92).A);
        Assert.Equal(0, RasterBackend.PixelAt(image, 50, 16).A);
        // Symmetric across the vertical axis.
        Assert.Equal(RasterBackend.PixelAt(image, 16, 50), RasterBackend.PixelAt(image, 83, 50));
    }

    [Fact]
    public void Elevation_IsScaledByTheEngineDevicePixelRatio()
    {
        // Canvas::drawShadow multiplies the elevation by view 0's device pixel ratio, not by the canvas transform.
        Bitmap scaled = Render(
            canvas => canvas.DrawShadow(RectPath(Occluder), Colors.Black, 2, transparentOccluder: false),
            engineDevicePixelRatio: 3.0);
        Bitmap direct = Render(
            canvas => canvas.DrawShadow(RectPath(Occluder), Colors.Black, 6, transparentOccluder: false));

        Assert.Equal(RasterBackend.ReadPixels(direct), RasterBackend.ReadPixels(scaled));
    }

    [Fact]
    public void EngineDevicePixelRatio_DefaultsToTheImplicitViewOrOne()
    {
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        double? previous = dispatcher.EngineDevicePixelRatioOverride;
        try
        {
            dispatcher.EngineDevicePixelRatioOverride = null;
            Assert.Equal(dispatcher.ImplicitView?.DevicePixelRatio ?? 1.0, dispatcher.EngineDevicePixelRatio);
            dispatcher.EngineDevicePixelRatioOverride = 2.5;
            Assert.Equal(2.5, dispatcher.EngineDevicePixelRatio);
        }
        finally
        {
            dispatcher.EngineDevicePixelRatioOverride = previous;
        }
    }

    [Fact]
    public void RectangleGeometry_DrawsTheSameShadowAsItsPath()
    {
        Bitmap fromGeometry = Render(canvas => canvas.DrawShadow(
            new RectangleGeometry(Occluder, 4, 4), Colors.Black, 3, transparentOccluder: true));
        Bitmap fromPath = Render(canvas =>
        {
            var path = new UI.Path();
            path.AddRRect(RRect.FromRectXY(Occluder, 4, 4));
            canvas.DrawShadow(path, Colors.Black, 3, transparentOccluder: true);
        });

        Assert.Equal(RasterBackend.ReadPixels(fromPath), RasterBackend.ReadPixels(fromGeometry));
    }
}
