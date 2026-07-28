using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class FilterWidgetsTests
{
    private static readonly IReadOnlyList<double> IdentityImageMatrix =
    [
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0,
    ];

    [Fact]
    public void ImageFilterFactories_ExposeFlutterDefaultsAndValidateArguments()
    {
        var blur = new ImageFilter.Blur();
        Assert.Equal(0.0, blur.SigmaX);
        Assert.Equal(0.0, blur.SigmaY);
        Assert.Equal(Plumix.Rendering.TileMode.Clamp, blur.TileMode);

        var matrix = new ImageFilter.Matrix(IdentityImageMatrix);
        Assert.Equal(IdentityImageMatrix, matrix.Values);
        Assert.Equal(FilterQuality.Low, matrix.FilterQuality);

        var dilate = new ImageFilter.Dilate(2.0, 3.0);
        var erode = new ImageFilter.Erode(4.0, 5.0);
        var compose = new ImageFilter.Compose(dilate, erode);
        Assert.Equal(2.0, dilate.RadiusX);
        Assert.Equal(3.0, dilate.RadiusY);
        Assert.Equal(4.0, erode.RadiusX);
        Assert.Equal(5.0, erode.RadiusY);
        Assert.Same(dilate, compose.Outer);
        Assert.Same(erode, compose.Inner);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFilter.Blur(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFilter.Blur(sigmaY: double.NaN));
        Assert.Throws<ArgumentException>(() => new ImageFilter.Matrix([1.0, 0.0]));
        Assert.Throws<ArgumentException>(() => new ImageFilter.Matrix(
        [
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, double.PositiveInfinity, 0.0,
            0.0, 0.0, 0.0, 1.0,
        ]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFilter.Dilate(radiusX: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFilter.Erode(radiusY: double.NaN));
        Assert.Throws<ArgumentNullException>(() => new ImageFilter.Compose(null!, blur));
        Assert.Throws<ArgumentNullException>(() => new ImageFilter.Compose(blur, null!));
    }

    [Fact]
    public void FilterWidgets_RequireFiltersAndExposeFlutterDefaults()
    {
        var colorFilter = new ColorFilter.Mode(Colors.Red, BitmapBlendingMode.Multiply);
        var dartColorFilter = new ColorFilter.Mode(Colors.Blue, BlendMode.Modulate);
        var imageFilter = new ImageFilter.Blur(4.0, 6.0, Plumix.Rendering.TileMode.Decal);
        var colorWidget = new ColorFiltered(colorFilter);
        var imageWidget = new ImageFiltered(imageFilter);

        Assert.Same(colorFilter, colorWidget.ColorFilter);
        Assert.Equal(BlendMode.Modulate, dartColorFilter.FlutterBlendMode);
        Assert.Equal(BitmapBlendingMode.Multiply, dartColorFilter.BlendMode);
        Assert.Null(colorWidget.Child);
        Assert.Same(imageFilter, imageWidget.ImageFilter);
        Assert.True(imageWidget.Enabled);
        Assert.Null(imageWidget.Child);
        Assert.Throws<ArgumentNullException>(() => new ColorFiltered(null!));
        Assert.Throws<ArgumentNullException>(() => new ImageFiltered(null!));
    }

    [Fact]
    public void FilterWidgets_UpdateExistingRenderObjects()
    {
        var initialColorFilter = new ColorFilter.Mode(Colors.Red, BitmapBlendingMode.SourceIn);
        var updatedColorFilter = new ColorFilter.Matrix(
        [
            1.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0, 0.0,
        ]);
        var owner = new BuildOwner();
        var root = new TestRootElement(new ColorFiltered(
            initialColorFilter,
            new SizedBox(width: 12.0, height: 8.0)));
        Mount(root, owner);

        var colorRenderObject = RequireRenderObject<RenderColorFilter>(root.ChildElement);
        Assert.Same(initialColorFilter, colorRenderObject.ColorFilter);

        root.Update(new ColorFiltered(
            updatedColorFilter,
            new SizedBox(width: 12.0, height: 8.0)));
        owner.FlushBuild();
        var updatedColorRenderObject = RequireRenderObject<RenderColorFilter>(root.ChildElement);
        Assert.Same(colorRenderObject, updatedColorRenderObject);
        Assert.Same(updatedColorFilter, updatedColorRenderObject.ColorFilter);

        var initialImageFilter = new ImageFilter.Blur(2.0, 3.0);
        var updatedImageFilter = new ImageFilter.Matrix(IdentityImageMatrix, FilterQuality.High);
        root.Update(new ImageFiltered(
            initialImageFilter,
            new SizedBox(width: 12.0, height: 8.0)));
        owner.FlushBuild();
        var imageRenderObject = RequireRenderObject<RenderImageFilter>(root.ChildElement);
        Assert.Same(initialImageFilter, imageRenderObject.ImageFilter);
        Assert.True(imageRenderObject.Enabled);

        root.Update(new ImageFiltered(
            updatedImageFilter,
            new SizedBox(width: 12.0, height: 8.0),
            enabled: false));
        owner.FlushBuild();
        var updatedImageRenderObject = RequireRenderObject<RenderImageFilter>(root.ChildElement);
        Assert.Same(imageRenderObject, updatedImageRenderObject);
        Assert.Same(updatedImageFilter, updatedImageRenderObject.ImageFilter);
        Assert.False(updatedImageRenderObject.Enabled);

        root.Unmount();
    }

    [Fact]
    public void ShaderMask_ExposesFlutterDefaultsAndUpdatesExistingRenderObject()
    {
        ShaderCallback initialCallback = _ => Brushes.Red;
        ShaderCallback updatedCallback = _ => Brushes.Blue;
        var widget = new ShaderMask(initialCallback);

        Assert.Same(initialCallback, widget.ShaderCallback);
        Assert.Equal(BlendMode.Modulate, widget.BlendMode);
        Assert.Null(widget.Child);
        Assert.Throws<ArgumentNullException>(() => new ShaderMask(null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(new ShaderMask(
            initialCallback,
            child: new SizedBox(width: 20.0, height: 10.0)));
        Mount(root, owner);

        var renderObject = RequireRenderObject<RenderShaderMask>(root.ChildElement);
        Assert.Same(initialCallback, renderObject.ShaderCallback);
        Assert.Equal(BlendMode.Modulate, renderObject.BlendMode);

        root.Update(new ShaderMask(
            updatedCallback,
            child: new SizedBox(width: 20.0, height: 10.0),
            blendMode: BlendMode.SourceIn));
        owner.FlushBuild();

        var updatedRenderObject = RequireRenderObject<RenderShaderMask>(root.ChildElement);
        Assert.Same(renderObject, updatedRenderObject);
        Assert.Same(updatedCallback, updatedRenderObject.ShaderCallback);
        Assert.Equal(BlendMode.SourceIn, updatedRenderObject.BlendMode);

        root.Unmount();
    }

    [Fact]
    public void RenderColorFilter_RetainsLayerAndRepaintsForFilterChanges()
    {
        var child = new PaintProbeRenderBox();
        var initialFilter = new ColorFilter.Mode(Colors.Red, BitmapBlendingMode.SourceIn);
        var renderObject = new RenderColorFilter(initialFilter, child);
        var renderView = new RenderView { Child = renderObject };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        Pump(pipeline);

        var layer = Assert.IsType<ColorFilterLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Same(initialFilter, layer.ColorFilter);
        Assert.Equal(new Rect(0.0, 0.0, 20.0, 10.0), layer.FilterBounds);
        Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.Equal(1, child.PaintCount);

        var updatedFilter = new ColorFilter.Mode(Colors.Blue, BitmapBlendingMode.Multiply);
        renderObject.ColorFilter = updatedFilter;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Same(layer, Assert.Single(pipeline.RootLayer.Children));
        Assert.Same(updatedFilter, layer.ColorFilter);
        Assert.Equal(2, child.PaintCount);
    }

    [Fact]
    public void RenderImageFilter_UsesLayerUpdateWithoutRepaintingChild()
    {
        var child = new PaintProbeRenderBox();
        var initialFilter = new ImageFilter.Blur(2.0, 3.0);
        var renderObject = new RenderImageFilter(initialFilter, child: child);
        var renderView = new RenderView { Child = renderObject };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        Pump(pipeline);

        var layer = Assert.IsType<ImageFilterLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Same(initialFilter, layer.ImageFilter);
        Assert.Equal(new Rect(0.0, 0.0, 20.0, 10.0), layer.FilterBounds);
        Assert.Equal(1, child.PaintCount);

        var updatedFilter = new ImageFilter.Dilate(2.0, 1.0);
        renderObject.ImageFilter = updatedFilter;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Same(layer, Assert.Single(pipeline.RootLayer.Children));
        Assert.Same(updatedFilter, layer.ImageFilter);
        Assert.Equal(1, child.PaintCount);
    }

    [Fact]
    public void RenderImageFilter_EnabledControlsBoundaryAndLayerPresence()
    {
        var child = new PaintProbeRenderBox();
        var renderObject = new RenderImageFilter(
            new ImageFilter.Blur(2.0, 2.0),
            enabled: false,
            child: child);
        var renderView = new RenderView { Child = renderObject };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        Pump(pipeline);

        Assert.False(renderObject.IsRepaintBoundary);
        Assert.IsType<PictureLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(1, child.PaintCount);

        renderObject.Enabled = true;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(renderObject.IsRepaintBoundary);
        Assert.IsType<ImageFilterLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(2, child.PaintCount);

        renderObject.Enabled = false;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.False(renderObject.IsRepaintBoundary);
        Assert.IsType<PictureLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(3, child.PaintCount);
    }

    [Fact]
    public void RenderShaderMask_UsesOriginBoundsAndRetainsLayerAcrossUpdates()
    {
        Rect callbackBounds = default;
        var child = new PaintProbeRenderBox();
        var renderObject = new RenderShaderMask(
            bounds =>
            {
                callbackBounds = bounds;
                return Brushes.Red;
            },
            child: child);
        var padding = new RenderPadding(new Thickness(7.0, 5.0, 0.0, 0.0))
        {
            Child = renderObject,
        };
        var renderView = new RenderView { Child = padding };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        Pump(pipeline);

        Assert.Equal(new Rect(0.0, 0.0, 20.0, 10.0), callbackBounds);
        var layer = Assert.IsType<ShaderMaskLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Rect(7.0, 5.0, 20.0, 10.0), layer.MaskRect);
        Assert.Equal(BlendMode.Modulate, layer.BlendMode);
        Assert.Same(Brushes.Red, layer.Shader);
        Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.Equal(1, child.PaintCount);

        renderObject.ShaderCallback = _ => Brushes.Blue;
        renderObject.BlendMode = BlendMode.Screen;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Same(layer, Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(BlendMode.Screen, layer.BlendMode);
        Assert.Same(Brushes.Blue, layer.Shader);
        Assert.Equal(2, child.PaintCount);
    }

    [Fact]
    public void ColorFilterRasterization_AppliesMatrixAndFlutterModulate()
    {
        byte[] swapped = FilterLayerRasterizer.ApplyColorFilterForTests(
            [0, 0, 255, 255],
            new ColorFilter.Matrix(
            [
                0.0, 0.0, 1.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0, 0.0,
                1.0, 0.0, 0.0, 0.0, 0.0,
                0.0, 0.0, 0.0, 1.0, 0.0,
            ]));
        Assert.Equal([255, 0, 0, 255], swapped);

        byte[] modulated = FilterLayerRasterizer.ApplyColorFilterForTests(
            [0, 0, 255, 255],
            new ColorFilter.Mode(Colors.Green, BlendMode.Modulate));
        Assert.Equal([0, 0, 0, 255], modulated);
    }

    [Fact]
    public void ShaderMaskRasterization_BlendsShaderAsSourceOverChild()
    {
        byte[] modulated = FilterLayerRasterizer.ApplyShaderMaskForTests(
            childPixels: [0, 0, 255, 255],
            shaderPixels: [0, 255, 0, 255],
            blendMode: BlendMode.Modulate);
        Assert.Equal([0, 0, 0, 255], modulated);

        byte[] sourceIn = FilterLayerRasterizer.ApplyShaderMaskForTests(
            childPixels: [0, 0, 255, 255],
            shaderPixels: [0, 255, 0, 255],
            blendMode: BlendMode.SourceIn);
        Assert.Equal([0, 255, 0, 255], sourceIn);
    }

    [Fact]
    public void ImageFilterRasterization_BlurAndMatrixUpdateOutputGeometry()
    {
        var blur = FilterLayerRasterizer.ApplyImageFilterForTests(
            [0, 0, 255, 255],
            width: 1,
            height: 1,
            imageFilter: new ImageFilter.Blur(1.0, 1.0, Plumix.Rendering.TileMode.Decal));
        Assert.Equal(7, blur.Width);
        Assert.Equal(7, blur.Height);
        Assert.Equal(new Rect(-3.0, -3.0, 7.0, 7.0), blur.Bounds);
        byte centerAlpha = blur.Pixels[(((3 * blur.Width) + 3) * 4) + 3];
        Assert.InRange(centerAlpha, (byte)1, (byte)254);

        var clampedBlur = FilterLayerRasterizer.ApplyImageFilterForTests(
            [0, 0, 255, 255],
            width: 1,
            height: 1,
            imageFilter: new ImageFilter.Blur(1.0, 1.0));
        for (int index = 3; index < clampedBlur.Pixels.Length; index += 4)
        {
            Assert.Equal(byte.MaxValue, clampedBlur.Pixels[index]);
        }

        var translated = FilterLayerRasterizer.ApplyImageFilterForTests(
            [255, 0, 0, 255],
            width: 1,
            height: 1,
            imageFilter: new ImageFilter.Matrix(
            [
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                2.0, 3.0, 0.0, 1.0,
            ],
            FilterQuality.None));
        Assert.Equal(1, translated.Width);
        Assert.Equal(1, translated.Height);
        Assert.Equal(new Rect(2.0, 3.0, 1.0, 1.0), translated.Bounds);
        Assert.Equal([255, 0, 0, 255], translated.Pixels);

        var dilated = FilterLayerRasterizer.ApplyImageFilterForTests(
            [0, 0, 255, 255],
            width: 1,
            height: 1,
            imageFilter: new ImageFilter.Dilate(1.0, 1.0));
        Assert.Equal(new Rect(-1.0, -1.0, 3.0, 3.0), dilated.Bounds);
        for (int index = 3; index < dilated.Pixels.Length; index += 4)
        {
            Assert.Equal(byte.MaxValue, dilated.Pixels[index]);
        }

        byte[] opaqueSquare = Enumerable
            .Range(0, 9)
            .SelectMany(static _ => new byte[] { 0, 0, 255, 255 })
            .ToArray();
        var eroded = FilterLayerRasterizer.ApplyImageFilterForTests(
            opaqueSquare,
            width: 3,
            height: 3,
            imageFilter: new ImageFilter.Erode(1.0, 1.0));
        Assert.Equal(new Rect(0.0, 0.0, 3.0, 3.0), eroded.Bounds);
        Assert.Equal(byte.MaxValue, eroded.Pixels[(((1 * eroded.Width) + 1) * 4) + 3]);
        Assert.Equal(byte.MinValue, eroded.Pixels[3]);
    }

    private static void Pump(PipelineOwner pipeline)
    {
        pipeline.FlushLayout(new Size(40.0, 30.0));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class PaintProbeRenderBox : RenderBox
    {
        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(20.0, 10.0));
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount++;
            ctx.DrawRectangle(Brushes.Red, null, new Rect(offset, Size));
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(child, _child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
