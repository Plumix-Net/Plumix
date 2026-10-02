using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Flutter's engine `testing/dart/compositing_test.dart` (the dart:ui `SceneBuilder`/`Scene`/`EngineLayer`
// contract) and the scene-building cases of `rendering/layers_test.dart`.
public sealed class SceneBuilderTests
{
    [Fact]
    public void SceneToImageSync_Succeeds()
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.DrawPaint(new Paint { Color = new Plumix.UI.Color(0xFF123456) });
        Picture picture = recorder.EndRecording();
        var builder = new SceneBuilder();
        builder.PushOffset(10, 10);
        builder.AddPicture(new Point(5, 5), picture);
        Scene scene = builder.Build();

        Bitmap image = scene.ToImageSync(6, 8);
        picture.Dispose();
        scene.Dispose();

        Assert.Equal(6, image.PixelSize.Width);
        Assert.Equal(8, image.PixelSize.Height);
        Assert.Equal(6 * 8 * 4, RasterBackend.ReadPixels(image).Length);
        Assert.Equal(((byte)0x12, (byte)0x34, (byte)0x56, (byte)0xFF), RasterBackend.PixelAt(image, 0, 0));
    }

    [Fact]
    public void SceneToImageSync_SucceedsWithTextureLayer()
    {
        var builder = new SceneBuilder();
        builder.PushOffset(10, 10);
        builder.AddTexture(0, width: 10, height: 10);
        Scene scene = builder.Build();

        Bitmap image = scene.ToImageSync(20, 20);
        scene.Dispose();

        Assert.Equal(new PixelSize(20, 20), image.PixelSize);
        Assert.Equal(20 * 20 * 4, RasterBackend.ReadPixels(image).Length);
        Assert.Equal(((byte)0, (byte)0, (byte)0, (byte)0), RasterBackend.PixelAt(image, 0, 0));
    }

    [Fact]
    public void SceneToImage_RejectsInvalidDimensionsAndDisposedScenes()
    {
        Scene scene = new SceneBuilder().Build();
        Assert.Throws<ArgumentException>(() => scene.ToImageSync(0, 1));
        Assert.Throws<ArgumentException>(() => { _ = scene.ToImage(1, -1); });

        scene.Dispose();
        PictureRasterizationException exception =
            Assert.Throws<PictureRasterizationException>(() => scene.ToImageSync(1, 1));
        Assert.Equal("Failed to rasterize a picture: Scene has been disposed..", exception.ToString());
        Assert.Throws<InvalidOperationException>(() => { _ = scene.ToImage(1, 1); });
        Assert.Equal("Scene", scene.ToString());
        Assert.Equal("SceneBuilder", new SceneBuilder().ToString());
    }

    [Fact]
    public async Task SceneToImage_CompletesWithTheRasterizedImage()
    {
        var builder = new SceneBuilder();
        builder.AddPicture(default, DrawPaintPicture(0xFF00FF00));
        using Scene scene = builder.Build();

        Bitmap image = await scene.ToImage(3, 2);

        Assert.Equal(new PixelSize(3, 2), image.PixelSize);
        Assert.Equal(((byte)0, (byte)0xFF, (byte)0, (byte)0xFF), RasterBackend.PixelAt(image, 2, 1));
    }

    [DebugOnlyFact]
    public void AddPicture_WithDisposedPictureDoesNotCrash()
    {
        Picture picture = new PictureRecorder().EndRecording();
        picture.Dispose();
        Assert.True(picture.DebugDisposed);

        var builder = new SceneBuilder();
        Assert.Throws<AssertionError>(() => builder.AddPicture(default, picture));
        builder.Build().Dispose();
    }

    [DebugOnlyFact]
    public void PushTransform_ValidatesTheMatrix()
    {
        var builder = new SceneBuilder();
        Assert.NotNull(builder.PushTransform(Matrix4.Identity().Storage));
        Assert.Throws<AssertionError>(() => builder.PushTransform(new double[14]));
        double[] nan = Matrix4.Identity().Storage.ToArray();
        nan[3] = double.NaN;
        Assert.Throws<AssertionError>(() => builder.PushTransform(nan));
        double[] infinity = Matrix4.Identity().Storage.ToArray();
        infinity[3] = double.PositiveInfinity;
        Assert.Throws<AssertionError>(() => builder.PushTransform(infinity));
    }

    [DebugOnlyFact]
    public void ClipPushes_RejectClipNone()
    {
        var builder = new SceneBuilder();
        Assert.Throws<AssertionError>(() => builder.PushClipRect(default, clipBehavior: Clip.None));
        Assert.Throws<AssertionError>(() => builder.PushClipRRect(default, clipBehavior: Clip.None));
        Assert.Throws<AssertionError>(() => builder.PushClipPath(new Plumix.UI.Path(), clipBehavior: Clip.None));
    }

    [Fact]
    public void SceneBuilder_AcceptsTypedLayers()
    {
        var builder1 = new SceneBuilder();
        OpacityEngineLayer opacity1 = builder1.PushOpacity(100);
        Assert.NotNull(opacity1);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        OpacityEngineLayer opacity2 = builder2.PushOpacity(200, oldLayer: opacity1);
        Assert.NotNull(opacity2);
        builder2.Pop();
        builder2.Build().Dispose();
    }

    [Fact]
    public void Pop_NeverPopsTheRootContainer()
    {
        var builder = new SceneBuilder();
        builder.Pop();
        builder.Pop();
        builder.AddPicture(default, new PictureRecorder().EndRecording());
        using Scene scene = builder.Build();
        Assert.IsType<DisplayListFlowLayer>(Assert.Single(scene.RootLayer!.Layers));
    }

    [Fact]
    public void Build_MakesTheBuilderUnusable()
    {
        var builder = new SceneBuilder();
        builder.Build().Dispose();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.PushOffset(0, 0));
    }

    [Fact]
    public void AddRetained_ReaddsTheEngineLayerSubtree()
    {
        var builder1 = new SceneBuilder();
        OffsetEngineLayer offset = builder1.PushOffset(3, 4);
        builder1.AddPicture(default, new PictureRecorder().EndRecording());
        builder1.Pop();
        using Scene scene1 = builder1.Build();
        FlowLayer retained = Assert.Single(scene1.RootLayer!.Layers);

        var builder2 = new SceneBuilder();
        builder2.AddRetained(offset);
        using Scene scene2 = builder2.Build();

        Assert.Same(retained, Assert.Single(scene2.RootLayer!.Layers));
        var transform = Assert.IsType<TransformFlowLayer>(retained);
        Assert.Equal(Matrix4.TranslationValues(3, 4, 0), transform.Transform);
    }

    [DebugOnlyFact]
    public void AddRetained_RejectsLayersNoSceneBuilderCreated()
    {
        Assert.Throws<AssertionError>(() => new SceneBuilder().AddRetained(new FakeOffsetEngineLayer()));
    }

    [DebugOnlyFact]
    public void EngineLayer_DisposeTwiceAsserts()
    {
        var builder = new SceneBuilder();
        OffsetEngineLayer layer = builder.PushOffset(0, 0);
        builder.Build().Dispose();
        layer.Dispose();
        Assert.Throws<AssertionError>(() => layer.Dispose());
    }

    public static TheoryData<string> PushTypes => new()
    {
        "offset", "transform", "clipRect", "clipRRect", "clipPath", "opacity", "backdropFilter",
        "shaderMask", "colorFilter", "imageFilter",
    };

    // compositing_test.dart: "SceneBuilder does not share a layer between addRetained and push*".
    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void PushThenIllegalRetain(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        Push(type, builder2, layer);
        builder2.Pop();
        AssertionError error = Assert.Throws<AssertionError>(() => builder2.AddRetained(layer));
        Assert.Contains("The layer is already being used", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void AddRetainedThenIllegalPush(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        builder2.AddRetained(layer);
        AssertionError error = Assert.Throws<AssertionError>(() => Push(type, builder2, layer));
        Assert.Contains("The layer is already being used", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void DoubleAddRetained(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        builder2.AddRetained(layer);
        AssertionError error = Assert.Throws<AssertionError>(() => builder2.AddRetained(layer));
        Assert.Contains("The layer is already being used", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void PushOldLayerTwice(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer oldLayer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        Push(type, builder2, oldLayer);
        builder2.Pop();
        AssertionError error = Assert.Throws<AssertionError>(() => Push(type, builder2, oldLayer));
        Assert.Contains("was previously used as oldLayer", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void PushChildLayerOfRetainedLayer(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        OpacityEngineLayer childLayer = builder1.PushOpacity(123);
        builder1.Pop();
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        builder2.AddRetained(layer);
        AssertionError error = Assert.Throws<AssertionError>(() => builder2.PushOpacity(321, oldLayer: childLayer));
        Assert.Contains("The layer is already being used", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void RetainParentLayerOfPushedChild(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        OpacityEngineLayer childLayer = builder1.PushOpacity(123);
        builder1.Pop();
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        builder2.PushOpacity(234, oldLayer: childLayer);
        builder2.Pop();
        AssertionError error = Assert.Throws<AssertionError>(() => builder2.AddRetained(layer));
        Assert.Contains("The layer is already being used", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void RetainOldLayer(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        Push(type, builder2, layer);
        builder2.Pop();
        builder2.Build().Dispose();

        var builder3 = new SceneBuilder();
        AssertionError error = Assert.Throws<AssertionError>(() => builder3.AddRetained(layer));
        Assert.Contains("was previously used as oldLayer", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void PushOldLayer(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer layer = Push(type, builder1, null);
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        Push(type, builder2, layer);
        builder2.Pop();
        builder2.Build().Dispose();

        var builder3 = new SceneBuilder();
        AssertionError error = Assert.Throws<AssertionError>(() => Push(type, builder3, layer));
        Assert.Contains("was previously used as oldLayer", error.Message, StringComparison.Ordinal);
    }

    [DebugOnlyTheory]
    [MemberData(nameof(PushTypes))]
    public void RetainsParentOfOldLayer(string type)
    {
        var builder1 = new SceneBuilder();
        EngineLayer parentLayer = Push(type, builder1, null);
        OpacityEngineLayer childLayer = builder1.PushOpacity(123);
        builder1.Pop();
        builder1.Pop();
        builder1.Build().Dispose();

        var builder2 = new SceneBuilder();
        builder2.PushOpacity(321, oldLayer: childLayer);
        builder2.Pop();
        builder2.Build().Dispose();

        var builder3 = new SceneBuilder();
        AssertionError error = Assert.Throws<AssertionError>(() => builder3.AddRetained(parentLayer));
        Assert.Contains("was previously used as oldLayer", error.Message, StringComparison.Ordinal);
    }

    // layers_test.dart: "OpacityLayer does not push an OffsetLayer if there are no children".
    [Fact]
    public void OpacityLayer_DoesNotPushAnOffsetLayerIfThereAreNoChildren()
    {
        var layer = new OpacityLayer(alpha: 128);
        var builder = new FakeSceneBuilder();
        layer.AddToScene(builder);
        Assert.False(builder.PushedOpacity);
        Assert.False(builder.PushedOffset);
        Assert.False(builder.AddedPicture);
        Assert.Null(layer.EngineLayer);

        layer.Append(new PictureLayer(new Rect(-1e9, -1e9, 2e9, 2e9))
        {
            Picture = new PictureRecorder().EndRecording(),
        });
        builder.Reset();
        layer.AddToScene(builder);
        Assert.True(builder.PushedOpacity);
        Assert.False(builder.PushedOffset);
        Assert.True(builder.AddedPicture);
        Assert.IsType<FakeOpacityEngineLayer>(layer.EngineLayer);

        builder.Reset();
        layer.Alpha = 200;
        Assert.IsType<FakeOpacityEngineLayer>(layer.EngineLayer);

        layer.Alpha = 255;
        Assert.Null(layer.EngineLayer);

        builder.Reset();
        layer.AddToScene(builder);
        Assert.False(builder.PushedOpacity);
        Assert.True(builder.PushedOffset);
        Assert.True(builder.AddedPicture);
        Assert.IsType<FakeOffsetEngineLayer>(layer.EngineLayer);

        layer.Alpha = 200;
        Assert.Null(layer.EngineLayer);

        builder.Reset();
        layer.AddToScene(builder);
        Assert.True(builder.PushedOpacity);
        Assert.False(builder.PushedOffset);
        Assert.True(builder.AddedPicture);
        Assert.IsType<FakeOpacityEngineLayer>(layer.EngineLayer);
    }

    // layers_test.dart: "OpacityLayer dispose its engineLayer if there are no children".
    [Fact]
    public void OpacityLayer_DisposesItsEngineLayerIfThereAreNoChildren()
    {
        var layer = new OpacityLayer(alpha: 128);
        var builder = new FakeSceneBuilder();
        layer.AddToScene(builder);
        Assert.Null(layer.EngineLayer);

        layer.Append(new PictureLayer(new Rect(-1e9, -1e9, 2e9, 2e9))
        {
            Picture = new PictureRecorder().EndRecording(),
        });
        layer.AddToScene(builder);
        Assert.IsType<FakeOpacityEngineLayer>(layer.EngineLayer);

        layer.RemoveAllChildren();
        layer.AddToScene(builder);
        Assert.Null(layer.EngineLayer);
    }

    // layers_test.dart: "PictureLayer.picture (set) disposes the picture".
    [DebugOnlyFact]
    public void PictureLayer_PictureSetterDisposesThePicture()
    {
        var layer = new PictureLayer(default);
        Picture picture = new PictureRecorder().EndRecording();
        layer.Picture = picture;
        Assert.False(picture.DebugDisposed);
        layer.Picture = null;
        Assert.True(picture.DebugDisposed);
    }

    // layers_test.dart: "PictureLayer disposes the picture".
    [DebugOnlyFact]
    public void PictureLayer_DisposesThePicture()
    {
        var layer = new PictureLayer(default);
        Picture picture = new PictureRecorder().EndRecording();
        layer.Picture = picture;
        Assert.False(picture.DebugDisposed);
        layer.Dispose();
        Assert.True(picture.DebugDisposed);
    }

    // layers_test.dart: "mutating PerformanceOverlayLayer fields triggers needsAddToScene".
    [DebugOnlyFact]
    public void MutatingPerformanceOverlayLayerFields_TriggersNeedsAddToScene()
    {
        var layer = new PerformanceOverlayLayer(overlayRect: default, optionsMask: 0);
        layer.DebugMarkClean();
        layer.UpdateSubtreeNeedsAddToScene();
        Assert.False(layer.DebugSubtreeNeedsAddToScene);
        layer.OverlayRect = new Rect(0, 0, 1, 1);
        layer.UpdateSubtreeNeedsAddToScene();
        Assert.True(layer.DebugSubtreeNeedsAddToScene);
    }

    // layers_test.dart: "ContainerLayer.toImage can render interior layer".
    [Fact]
    public async Task ContainerLayerToImage_CanRenderInteriorLayer()
    {
        var parent = new OffsetLayer();
        var child = new OffsetLayer();
        var grandChild = new OffsetLayer();
        child.Append(grandChild);
        parent.Append(child);

        // This renders the layers and generates engine layers.
        parent.BuildScene(new SceneBuilder()).Dispose();

        // Causes grandChild to pass its engine layer as `oldLayer`.
        Bitmap image = await grandChild.ToImage(new Rect(0, 0, 10, 10));
        Assert.Equal(new PixelSize(10, 10), image.PixelSize);

        // Ensure we can render the same scene again after rendering an interior layer.
        parent.BuildScene(new SceneBuilder()).Dispose();
    }

    // layers_test.dart: "ContainerLayer.toImageSync can render interior layer".
    [Fact]
    public void ContainerLayerToImageSync_CanRenderInteriorLayer()
    {
        var parent = new OffsetLayer();
        var child = new OffsetLayer();
        var grandChild = new OffsetLayer();
        child.Append(grandChild);
        parent.Append(child);

        parent.BuildScene(new SceneBuilder()).Dispose();
        Bitmap image = grandChild.ToImageSync(new Rect(0, 0, 10, 10));
        Assert.Equal(new PixelSize(10, 10), image.PixelSize);
        parent.BuildScene(new SceneBuilder()).Dispose();
    }

    [Fact]
    public void OffsetLayerToImageSync_CropsToTheBoundsAtThePixelRatio()
    {
        var layer = new OffsetLayer(new Point(100, 100));
        var picture = new PictureLayer(new Rect(0, 0, 20, 20)) { Picture = DrawRectPicture(new Rect(5, 5, 5, 5)) };
        layer.Append(picture);

        Bitmap image = layer.ToImageSync(new Rect(5, 5, 10, 10), pixelRatio: 2.0);

        Assert.Equal(new PixelSize(20, 20), image.PixelSize);
        // The rect starts at the bounds' origin: the top-left image pixels are covered...
        Assert.Equal((byte)0xFF, RasterBackend.PixelAt(image, 1, 1).A);
        Assert.Equal((byte)0xFF, RasterBackend.PixelAt(image, 9, 9).A);
        // ...and it ends 5 logical (10 image) pixels in.
        Assert.Equal((byte)0, RasterBackend.PixelAt(image, 11, 11).A);
    }

    [Fact]
    public void BuildScene_AddsACleanSubtreeRetained()
    {
        var root = new OffsetLayer();
        var clean = new OffsetLayer(new Point(1, 2));
        clean.Append(new PictureLayer(default) { Picture = new PictureRecorder().EndRecording() });
        root.Append(clean);

        using Scene first = root.BuildScene(new SceneBuilder());
        ContainerFlowLayer firstRoot = first.RootLayer!;
        var firstOffset = (ContainerFlowLayer)Assert.Single(firstRoot.Layers);
        FlowLayer firstClean = Assert.Single(firstOffset.Layers);
        EngineLayer? cleanEngineLayer = clean.EngineLayer;
        Assert.IsType<OffsetEngineLayer>(cleanEngineLayer);

        root.MarkNeedsAddToScene();
        using Scene second = root.BuildScene(new SceneBuilder());
        var secondOffset = (ContainerFlowLayer)Assert.Single(second.RootLayer!.Layers);
        Assert.NotSame(firstOffset, secondOffset);
        // The clean child was not re-added: its engine layer subtree is shared with the first scene.
        Assert.Same(firstClean, Assert.Single(secondOffset.Layers));
        Assert.Same(cleanEngineLayer, clean.EngineLayer);
    }

    private static EngineLayer Push(string type, SceneBuilder builder, EngineLayer? oldLayer)
    {
        var filter = new ImageFilter.Blur(1.0, 1.0);
        return type switch
        {
            "offset" => builder.PushOffset(0, 0, oldLayer: (OffsetEngineLayer?)oldLayer),
            "transform" => builder.PushTransform(new double[16], oldLayer: (TransformEngineLayer?)oldLayer),
            "clipRect" => builder.PushClipRect(default, oldLayer: (ClipRectEngineLayer?)oldLayer),
            "clipRRect" => builder.PushClipRRect(default, oldLayer: (ClipRRectEngineLayer?)oldLayer),
            "clipPath" => builder.PushClipPath(new Plumix.UI.Path(), oldLayer: (ClipPathEngineLayer?)oldLayer),
            "opacity" => builder.PushOpacity(100, oldLayer: (OpacityEngineLayer?)oldLayer),
            "backdropFilter" => builder.PushBackdropFilter(filter, oldLayer: (BackdropFilterEngineLayer?)oldLayer),
            "shaderMask" => builder.PushShaderMask(
                new Avalonia.Media.RadialGradientBrush(),
                default,
                BlendMode.Color,
                oldLayer: (ShaderMaskEngineLayer?)oldLayer),
            "colorFilter" => builder.PushColorFilter(
                new ColorFilter.Mode(new Plumix.UI.Color(0xFFFF0000), BlendMode.Color),
                oldLayer: (ColorFilterEngineLayer?)oldLayer),
            "imageFilter" => builder.PushImageFilter(filter, oldLayer: (ImageFilterEngineLayer?)oldLayer),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    internal static Picture DrawPaintPicture(uint argb)
    {
        var recorder = new PictureRecorder();
        new Canvas(recorder).DrawPaint(new Paint { Color = new Plumix.UI.Color(argb) });
        return recorder.EndRecording();
    }

    internal static Picture DrawRectPicture(Rect rect, uint argb = 0xFF000000)
    {
        var recorder = new PictureRecorder();
        new Canvas(recorder).DrawRect(rect, new Paint { Color = new Plumix.UI.Color(argb) });
        return recorder.EndRecording();
    }

    // layers_test.dart's FakeSceneBuilder: records the pushes the framework makes.
    private sealed class FakeSceneBuilder : SceneBuilder
    {
        public bool PushedOpacity { get; private set; }

        public bool PushedOffset { get; private set; }

        public bool AddedPicture { get; private set; }

        public void Reset()
        {
            PushedOpacity = false;
            PushedOffset = false;
            AddedPicture = false;
        }

        public override OpacityEngineLayer PushOpacity(
            int alpha,
            Point offset = default,
            OpacityEngineLayer? oldLayer = null)
        {
            PushedOpacity = true;
            return new FakeOpacityEngineLayer();
        }

        public override OffsetEngineLayer PushOffset(double dx, double dy, OffsetEngineLayer? oldLayer = null)
        {
            PushedOffset = true;
            return new FakeOffsetEngineLayer();
        }

        public override void AddPicture(
            Point offset,
            Picture picture,
            bool isComplexHint = false,
            bool willChangeHint = false)
        {
            AddedPicture = true;
        }

        public override void Pop()
        {
        }
    }

    private sealed class FakeOpacityEngineLayer : OpacityEngineLayer
    {
        private bool _disposed;

        public override void Dispose()
        {
            Assert.False(_disposed);
            _disposed = true;
        }
    }

    private sealed class FakeOffsetEngineLayer : OffsetEngineLayer
    {
        private bool _disposed;

        public override void Dispose()
        {
            Assert.False(_disposed);
            _disposed = true;
        }
    }
}
