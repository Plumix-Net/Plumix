using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// The engine side of the scene: the Plumix scene rasterizer (the engine's `flow/layers` painting) through
// Avalonia's Skia backend, `RenderRepaintBoundary.toImage`, textures, the performance overlay and the
// view composite (`RenderView.compositeFrame` handing a built scene to `FlutterView.render`).
public sealed class SceneRasterizerTests
{
    private static readonly uint Red = 0xFFFF0000;

    [Fact]
    public void TransformAndOffsetLayers_PushRealTransforms()
    {
        var root = new TransformLayer(Matrix4.Diagonal3Values(2.0, 2.0, 1.0));
        var offset = new OffsetLayer(new Point(10, 10));
        offset.Append(new PictureLayer(default)
        {
            Picture = SceneBuilderTests.DrawRectPicture(new Rect(0, 0, 5, 5), Red),
        });
        root.Append(offset);

        Bitmap image = ToImage(root, 40, 40);

        // (10, 10)..(15, 15) scaled by 2.
        Assert.Equal(0, RasterBackend.PixelAt(image, 19, 19).A);
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 21, 21));
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 29, 29));
        Assert.Equal(0, RasterBackend.PixelAt(image, 31, 31).A);
    }

    [Fact]
    public void ClipRectLayer_ClipsItsChildren()
    {
        var root = new OffsetLayer();
        var clip = new ClipRectLayer { ClipRect = new Rect(0, 0, 4, 4) };
        clip.Append(new PictureLayer(default) { Picture = SceneBuilderTests.DrawPaintPicture(Red) });
        root.Append(clip);

        Bitmap image = ToImage(root, 8, 8);

        Assert.Equal((byte)0xFF, RasterBackend.PixelAt(image, 2, 2).A);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 6, 6).A);
    }

    [Fact]
    public void OpacityLayer_MultipliesItsAlpha()
    {
        var root = new OffsetLayer();
        var opacity = new OpacityLayer(alpha: 128);
        opacity.Append(new PictureLayer(default) { Picture = SceneBuilderTests.DrawPaintPicture(0xFFFFFFFF) });
        root.Append(opacity);

        Bitmap image = ToImage(root, 4, 4);

        Assert.InRange(RasterBackend.PixelAt(image, 1, 1).A, 126, 130);
    }

    [Fact]
    public void BackdropFilterLayer_FiltersThePaintedPrefixInTargetCoordinates()
    {
        // The left half is red; a backdrop filter under an offset layer on the right half shifts the
        // backdrop 10 pixels right, so the right half shows the red the left half painted.
        var root = new OffsetLayer();
        root.Append(new PictureLayer(default)
        {
            Picture = SceneBuilderTests.DrawRectPicture(new Rect(0, 0, 10, 10), Red),
        });
        var offset = new OffsetLayer(new Point(10, 0));
        var clip = new ClipRectLayer { ClipRect = new Rect(0, 0, 10, 10) };
        Matrix4 shift = Matrix4.TranslationValues(10, 0, 0);
        clip.Append(new BackdropFilterLayer(new ImageFilter.Matrix(shift.Storage)));
        offset.Append(clip);
        root.Append(offset);

        Bitmap image = RasterizeScene(root, 20, 10);

        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 5, 5));
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 15, 5));
    }

    [Fact]
    public void MagnifierLayer_SamplesTheSceneBehindItsLensInTargetCoordinates()
    {
        var root = new OffsetLayer();
        root.Append(new PictureLayer(default)
        {
            Picture = SceneBuilderTests.DrawRectPicture(new Rect(0, 0, 20, 20), Red),
        });
        var offset = new OffsetLayer(new Point(30, 0));
        // The lens is centred on (35, 5) in the target; its focal point (10, 10) lies in the red square.
        offset.Append(new MagnifierLayer
        {
            LensRect = new Rect(0, 0, 10, 10),
            FocalPointOffset = new Point(-25, 5),
            MagnificationScale = 1.0,
            Decoration = new MagnifierDecoration(opacity: 1.0),
        });
        root.Append(offset);

        Bitmap image = RasterizeScene(root, 50, 20);

        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 35, 5));
        Assert.Equal(0, RasterBackend.PixelAt(image, 45, 5).A);
    }

    [Fact]
    public void RetainedSubtree_RastersLikeTheFrameThatBuiltIt()
    {
        var root = new OffsetLayer();
        var retained = new OffsetLayer(new Point(2, 2));
        retained.Append(new PictureLayer(default)
        {
            Picture = SceneBuilderTests.DrawRectPicture(new Rect(0, 0, 2, 2), Red),
        });
        root.Append(retained);
        root.BuildScene(new SceneBuilder()).Dispose();

        root.MarkNeedsAddToScene();
        using Scene scene = root.BuildScene(new SceneBuilder());
        Bitmap image = scene.ToImageSync(6, 6);

        Assert.Equal((byte)0xFF, RasterBackend.PixelAt(image, 3, 3).A);
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 1, 1).A);
    }

    [Fact]
    public void RenderRepaintBoundaryToImageSync_CapturesTheBoundaryAtThePixelRatio()
    {
        var boundary = new RenderRepaintBoundary(new RenderConstrainedBox(
            BoxConstraints.Tight(new Size(10, 6)),
            new RenderColoredBox(new Plumix.UI.Color(0xFFFF0000))));
        var renderView = new RenderView(new FlutterView(new Size(100, 100)))
        {
            Child = new RenderPositionedBoxProbe(boundary),
        };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();

        Bitmap image = boundary.ToImageSync(pixelRatio: 2.0);

        Assert.Equal(new PixelSize(20, 12), image.PixelSize);
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 0, 0));
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 19, 11));
    }

    [Fact]
    public async Task RenderRepaintBoundaryToImage_CompletesWithTheCapture()
    {
        var boundary = new RenderRepaintBoundary(new RenderConstrainedBox(
            BoxConstraints.Tight(new Size(4, 4)),
            new RenderColoredBox(new Plumix.UI.Color(0xFF0000FF))));
        var renderView = new RenderView(new FlutterView(new Size(10, 10)))
        {
            Child = new RenderPositionedBoxProbe(boundary),
        };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(10, 10));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Bitmap image = await boundary.ToImage();

        Assert.Equal(new PixelSize(4, 4), image.PixelSize);
        Assert.Equal(((byte)0, (byte)0, (byte)0xFF, (byte)0xFF), RasterBackend.PixelAt(image, 2, 2));
    }

    [Fact]
    public void TextureLayer_PaintsTheRegisteredTextureAndHonorsFreeze()
    {
        const int textureId = 4242;
        var texture = new ImageTexture(textureId);
        TextureRegistry.Instance.RegisterTexture(texture);
        try
        {
            texture.SetImage(SolidImage(Red));
            var red = ((byte)0xFF, (byte)0, (byte)0, (byte)0xFF);
            Assert.Equal(red, RasterBackend.PixelAt(TextureScene(textureId, false), 5, 5));

            texture.SetImage(SolidImage(0xFF0000FF));
            Assert.Equal(red, RasterBackend.PixelAt(TextureScene(textureId, true), 5, 5));
            var blue = ((byte)0, (byte)0, (byte)0xFF, (byte)0xFF);
            Assert.Equal(blue, RasterBackend.PixelAt(TextureScene(textureId, false), 5, 5));
        }
        finally
        {
            TextureRegistry.Instance.UnregisterTexture(textureId);
        }

        Assert.Null(TextureRegistry.Instance.GetTexture(textureId));
        Assert.Equal(0, RasterBackend.PixelAt(TextureScene(textureId, false), 5, 5).A);
    }

    [Fact]
    public void TextureRegistry_ReportsNewFrames()
    {
        const int textureId = 4243;
        var texture = new ImageTexture(textureId);
        var reported = new List<long>();
        void Handler(long id) => reported.Add(id);
        TextureRegistry.Instance.TextureFrameAvailable += Handler;
        try
        {
            TextureRegistry.Instance.RegisterTexture(texture);
            texture.SetImage(SolidImage(Red));
            TextureRegistry.Instance.MarkTextureFrameAvailable(textureId);
        }
        finally
        {
            TextureRegistry.Instance.TextureFrameAvailable -= Handler;
            TextureRegistry.Instance.UnregisterTexture(textureId);
        }

        Assert.Equal([textureId, textureId], reported.Where(id => id == textureId));
    }

    [Fact]
    public void PerformanceOverlay_DrawsTheStatisticsGraphsOnlyWhenEnabled()
    {
        var builder = new SceneBuilder();
        builder.AddPerformanceOverlay(0x0F, new Rect(0, 0, 200, 160));
        using Scene enabled = builder.Build();
        Bitmap image = enabled.ToImageSync(200, 160);
        // The graph background is the engine's semi-transparent white.
        Assert.InRange(RasterBackend.PixelAt(image, 10, 10).A, 0x90, 0xA0);

        var emptyBuilder = new SceneBuilder();
        emptyBuilder.AddPerformanceOverlay(0, new Rect(0, 0, 200, 160));
        using Scene disabled = emptyBuilder.Build();
        Assert.Equal(0, RasterBackend.PixelAt(disabled.ToImageSync(200, 160), 10, 10).A);
    }

    [Fact]
    public void FrameStopwatch_KeepsTheLastLapsLikeTheEngineStopwatch()
    {
        // The engine's stopwatch_unittests.cc: laps wrap around after kMaxSamples, and the average counts
        // every slot.
        var stopwatch = new FrameStopwatch();
        Assert.Equal(FrameStopwatch.MaxSamples - 1, stopwatch.CurrentSample);
        stopwatch.SetLapTime(TimeSpan.FromMilliseconds(4));
        Assert.Equal(0, stopwatch.CurrentSample);
        Assert.Equal(TimeSpan.FromMilliseconds(4), stopwatch.LastLap);
        stopwatch.SetLapTime(TimeSpan.FromMilliseconds(16));
        Assert.Equal(TimeSpan.FromMilliseconds(16), stopwatch.MaxDelta());
        Assert.Equal(TimeSpan.FromMilliseconds(20) / FrameStopwatch.MaxSamples, stopwatch.AverageDelta());
        for (int i = 0; i < FrameStopwatch.MaxSamples; i++)
        {
            stopwatch.SetLapTime(TimeSpan.FromMilliseconds(1));
        }

        Assert.Equal(1, stopwatch.CurrentSample);
        Assert.Equal(TimeSpan.FromMilliseconds(1), stopwatch.MaxDelta());
        Assert.Equal(
            "Raster  max 1.0 ms/frame, avg 1.0 ms/frame",
            PerformanceOverlayFlowLayer.MakeStatisticsText(stopwatch, "Raster"));
    }

    [Fact]
    public void RenderViewCompositeFrame_RendersTheBuiltSceneAtThePhysicalSize()
    {
        using var tester = new FrameworkDartTester(devicePixelRatio: 2.0, logicalSize: new Size(50, 40));
        Scene? rendered = null;
        Size? renderedSize = null;
        int? layerCount = null;
        void Handler(Scene scene, Size? size)
        {
            rendered = scene;
            renderedSize = size;
            layerCount = scene.RootLayer?.Layers.Count;
            Assert.IsType<TransformFlowLayer>(scene.RootLayer!.Layers[0]);
        }

        tester.View.RenderRequested += Handler;
        try
        {
            tester.PumpWidget(new ColoredBox(new Plumix.UI.Color(0xFFFF0000)));
        }
        finally
        {
            tester.View.RenderRequested -= Handler;
        }

        Assert.NotNull(rendered);
        Assert.Equal(new Size(100, 80), renderedSize);
        Assert.Equal(1, layerCount);
        // `compositeFrame` disposes the scene once the view has it.
        Assert.Null(rendered!.RootLayer);
    }

    [Fact]
    public void RenderViewCompositeFrame_AdvancesTheRepaintRainbow()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ColoredBox(new Plumix.UI.Color(0xFFFF0000)));
        RenderingDebug.RepaintRainbowEnabled = true;
        Plumix.Painting.HSVColor before = RenderingDebug.CurrentRepaintColor;
        try
        {
            tester.RenderView.CompositeFrame();
            Assert.Equal((before.Hue + 2.0) % 360.0, RenderingDebug.CurrentRepaintColor.Hue, 6);
        }
        finally
        {
            RenderingDebug.RepaintRainbowEnabled = false;
            RenderingDebug.CurrentRepaintColor = before;
        }
    }

    [Fact]
    public void HostRasterization_DrawsTheViewSceneAtLogicalSizeOnAHighDensityView()
    {
        // What PlumixHost.Render does: the view's scene is in physical pixels (its root transform layer
        // scales by the device pixel ratio) and is drawn into a logical-pixel surface under the inverse.
        using var tester = new FrameworkDartTester(devicePixelRatio: 2.0, logicalSize: new Size(100, 60));
        ContainerFlowLayer? layerTree = null;
        void Handler(Scene scene, Size? size) => layerTree = scene.TakeLayerTree();
        tester.View.RenderRequested += Handler;
        try
        {
            tester.PumpWidget(new Directionality(
                Plumix.UI.TextDirection.Ltr,
                new Stack(
                    children:
                    [
                        new Positioned(
                            left: 10,
                            top: 20,
                            child: new RepaintBoundary(child: new SizedBox(
                                width: 30,
                                height: 30,
                                child: new ColoredBox(new Plumix.UI.Color(0xFFFF0000))))),
                        new Positioned(
                            left: 50,
                            top: 20,
                            child: new Opacity(0.5, child: new SizedBox(
                                width: 30,
                                height: 30,
                                child: new ColoredBox(new Plumix.UI.Color(0xFF0000FF))))),
                    ])));
        }
        finally
        {
            tester.View.RenderRequested -= Handler;
        }

        Assert.NotNull(layerTree);
        var image = new RenderTargetBitmap(new PixelSize(100, 60), new Vector(96, 96));
        List<IDisposable> resources;
        using (Avalonia.Media.DrawingContext context = image.CreateDrawingContext())
        {
            resources = SceneRasterizer.Draw(
                context,
                layerTree!,
                new Size(100, 60),
                Matrix.CreateScale(0.5, 0.5),
                new CompositorContext(),
                TextureRegistry.Instance);
        }

        resources.ForEach(static resource => resource.Dispose());
        Assert.Equal(((byte)0xFF, (byte)0, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 25, 35));
        Assert.Equal(0, RasterBackend.PixelAt(image, 5, 35).A);
        (byte r, byte g, byte b, byte a) = RasterBackend.PixelAt(image, 65, 35);
        Assert.Equal((byte)0, r);
        Assert.InRange(a, 126, 130);
        Assert.InRange(b, 126, 130);
        Assert.Equal((byte)0, g);
    }

    private static Bitmap ToImage(ContainerLayer root, int width, int height)
    {
        using Scene scene = root.BuildScene(new SceneBuilder());
        return scene.ToImageSync(width, height);
    }

    // Draws through the host path: the backdrop and magnifier captures need a target-sized surface.
    private static Bitmap RasterizeScene(ContainerLayer root, int width, int height)
    {
        using Scene scene = root.BuildScene(new SceneBuilder());
        var image = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        List<IDisposable> resources;
        using (Avalonia.Media.DrawingContext context = image.CreateDrawingContext())
        {
            resources = SceneRasterizer.Draw(
                context,
                scene.RootLayer!,
                new Size(width, height),
                Matrix.Identity,
                new CompositorContext(),
                TextureRegistry.Instance);
        }

        foreach (IDisposable resource in resources)
        {
            resource.Dispose();
        }

        return image;
    }

    private static Bitmap TextureScene(int textureId, bool freeze)
    {
        var builder = new SceneBuilder();
        builder.AddTexture(textureId, width: 10, height: 10, freeze: freeze);
        using Scene scene = builder.Build();
        return scene.ToImageSync(10, 10);
    }

    private static RenderTargetBitmap SolidImage(uint argb)
    {
        var image = new RenderTargetBitmap(new PixelSize(2, 2), new Vector(96, 96));
        using (Avalonia.Media.DrawingContext context = image.CreateDrawingContext())
        {
            context.DrawRectangle(
                new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromUInt32(argb)),
                null,
                new Rect(0, 0, 2, 2));
        }

        return image;
    }

    // Lays its child out loosely at the origin, so a tight-sized boundary keeps its own size.
    private sealed class RenderPositionedBoxProbe(RenderBox child) : RenderProxyBox(child)
    {
        protected override void PerformLayout()
        {
            Child!.Layout(Constraints.Loosen(), parentUsesSize: true);
            Size = Constraints.Biggest;
        }
    }
}
