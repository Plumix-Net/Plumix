using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImageWidgetTests : IDisposable
{
    public ImageWidgetTests()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
    }

    public void Dispose()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ImageAndRawImage_DefaultsAndFactoriesMatchFlutterSurface()
    {
        byte[] bytes = [1, 2, 3];
        var provider = new MemoryImage(bytes, scale: 2.0);
        var image = new Image(provider);
        var raw = new RawImage();

        Assert.Same(provider, image.ImageProvider);
        Assert.False(image.ExcludeFromSemantics);
        Assert.False(image.MatchTextDirection);
        Assert.False(image.GaplessPlayback);
        Assert.Equal(ImageRepeat.NoRepeat, image.Repeat);
        Assert.Equal(FilterQuality.Medium, image.FilterQuality);
        Assert.Equal((AlignmentGeometry)Alignment.Center, image.Alignment);
        Assert.Equal(1.0, raw.Scale);
        Assert.Equal(ImageRepeat.NoRepeat, raw.Repeat);
        Assert.Equal(FilterQuality.Medium, raw.FilterQuality);

        var network = Image.Network(
            "https://example.com/image.png",
            scale: 1.5,
            headers: new Dictionary<string, string> { ["x-test"] = "yes" },
            cacheWidth: 24);
        var networkResize = Assert.IsType<ResizeImage>(network.ImageProvider);
        var networkProvider = Assert.IsType<NetworkImage>(networkResize.ImageProvider);
        Assert.Equal(24, networkResize.Width);
        Assert.Equal(1.5, networkProvider.Scale);
        Assert.Equal("yes", networkProvider.Headers!["x-test"]);

        Assert.IsType<AssetImage>(Image.Asset("images/a.png").ImageProvider);
        Assert.IsType<ExactAssetImage>(Image.Asset("images/a.png", scale: 3.0).ImageProvider);
        Assert.IsType<MemoryImage>(Image.Memory(bytes).ImageProvider);
        Assert.IsType<FileImage>(Image.File("image.png").ImageProvider);
        Assert.Throws<ArgumentOutOfRangeException>(() => Image.Memory(bytes, cacheWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Image(provider, width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawImage(scale: 0));

        var media = new MediaQueryData(DisableAnimations: true).CopyWith(invertColors: true);
        Assert.True(media.DisableAnimations);
        Assert.True(media.InvertColors);
    }

    [Fact]
    public void RenderImage_PreservesIntrinsicAspectRatioAndHonorsExplicitDimensions()
    {
        var image = new FakeImage(new Size(240, 120));
        var renderImage = new RenderImage(image: image, scale: 2.0);

        renderImage.Layout(new BoxConstraints(MaxWidth: 80, MaxHeight: 80));
        Assert.Equal(new Size(80, 40), renderImage.Size);

        renderImage.Width = 60;
        renderImage.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));
        Assert.Equal(new Size(60, 30), renderImage.Size);

        renderImage.Height = 50;
        renderImage.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));
        Assert.Equal(new Size(60, 50), renderImage.Size);

        renderImage.Image = null;
        renderImage.Width = null;
        renderImage.Height = null;
        renderImage.Layout(new BoxConstraints(MinWidth: 12, MaxWidth: 90, MinHeight: 8, MaxHeight: 70));
        Assert.Equal(new Size(12, 8), renderImage.Size);
    }

    [Fact]
    public void RenderImage_OpacityListenableRequestsPaintAndRtlPaintUsesDirectionalResolution()
    {
        using var opacity = new ValueNotifier<double>(1.0);
        var renderImage = new RenderImage(
            image: new FakeImage(new Size(20, 10)),
            width: 40,
            height: 20,
            opacity: opacity,
            alignment: AlignmentDirectional.CenterStart,
            matchTextDirection: true,
            textDirection: Plumix.UI.TextDirection.Rtl,
            fit: BoxFit.Contain);
        var renderView = new RenderView { Child = renderImage };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(40, 20));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        opacity.Value = 0.4;

        Assert.True(pipeline.NeedsPaint);
        pipeline.FlushPaint();
        Assert.NotEmpty(pipeline.RootLayer.Children);
        var hitTest = new BoxHitTestResult();
        Assert.True(renderImage.HitTest(hitTest, new Point(10, 10)));
    }

    [Fact]
    public async Task Image_ChainsFrameThenLoadingBuildersAndPublishesImageSemantics()
    {
        var completion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TestImageProvider("builders", completion.Task);
        Widget? frameResult = null;
        bool loadingReceivedFrameResult = false;
        int? observedFrame = null;
        bool observedSynchronous = true;
        var image = new Image(
            provider,
            width: 32,
            height: 24,
            semanticLabel: "Sample image",
            frameBuilder: (_, child, frame, synchronous) =>
            {
                observedFrame = frame;
                observedSynchronous = synchronous;
                frameResult = new Padding(new Thickness(1), child);
                return frameResult;
            },
            loadingBuilder: (_, child, _) =>
            {
                loadingReceivedFrameResult = ReferenceEquals(frameResult, child);
                return child;
            });
        using var harness = new WidgetRenderHarness(image);

        Assert.Null(observedFrame);
        Assert.True(loadingReceivedFrameResult);
        completion.SetResult(new ImageInfo(new FakeImage(new Size(16, 12)), scale: 1.0, debugLabel: "test"));
        await PumpUntilAsync(harness, () => observedFrame == 0);

        Assert.False(observedSynchronous);
        Assert.True(loadingReceivedFrameResult);
        harness.Pump(new Size(100, 100));
        harness.Pipeline.FlushSemantics();
        var root = Assert.IsType<SemanticsNode>(harness.Pipeline.SemanticsOwner.RootNode);
        var imageNode = Assert.Single(Flatten(root), node => node.Flags.HasFlag(SemanticsFlags.IsImage));
        Assert.Equal("Sample image", imageNode.Label);
    }

    [Fact]
    public async Task Image_UsesErrorBuilderAndGaplessPlaybackKeepsPreviousFrame()
    {
        var firstCompletion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstImage = new FakeImage(new Size(10, 10));
        var firstProvider = new TestImageProvider("first", firstCompletion.Task);
        var secondProvider = new TestImageProvider("second", secondCompletion.Task);
        bool errorBuilt = false;
        Widget Build(ImageProvider provider, bool gapless)
        {
            return new Image(
                provider,
                gaplessPlayback: gapless,
                errorBuilder: (_, _, _) =>
                {
                    errorBuilt = true;
                    return new SizedBox(width: 7, height: 9);
                });
        }

        using var harness = new WidgetRenderHarness(Build(firstProvider, gapless: true));
        firstCompletion.SetResult(new ImageInfo(firstImage));
        await PumpUntilAsync(harness, () => FindRenderImage(harness.RenderView)?.Image is not null);

        harness.Update(Build(secondProvider, gapless: true));
        Assert.Same(firstImage, FindRenderImage(harness.RenderView)!.Image);

        secondCompletion.SetException(new InvalidOperationException("failed"));
        await PumpUntilAsync(harness, () => errorBuilt);
        Assert.True(errorBuilt);
    }

    [Fact]
    public async Task Image_PausesCompletedStreamWhenAnimationsAreDisabled()
    {
        var completion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TestImageProvider("paused", completion.Task);
        Widget image = new MediaQuery(
            new MediaQueryData(DisableAnimations: true),
            new Image(provider, width: 20, height: 20));
        using var harness = new WidgetRenderHarness(image);

        Assert.True(provider.LastCompleter!.HasListeners);
        completion.SetResult(new ImageInfo(new FakeImage(new Size(20, 20))));
        await PumpUntilAsync(
            harness,
            () => FindRenderImage(harness.RenderView)?.Image is not null
                  && provider.LastCompleter.HasListeners == false);

        Assert.False(provider.LastCompleter.IsDisposed);
    }

    private static async Task PumpUntilAsync(WidgetRenderHarness harness, Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!predicate() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
            harness.FlushBuild();
        }

        Assert.True(predicate());
    }

    private static RenderImage? FindRenderImage(RenderObject root)
    {
        if (root is RenderImage image)
        {
            return image;
        }

        RenderImage? result = null;
        root.VisitChildren(child => result ??= FindRenderImage(child));
        return result;
    }

    private static IEnumerable<SemanticsNode> Flatten(SemanticsNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class TestImageProvider : ImageProvider<string>
    {
        private readonly string _key;
        private readonly Task<ImageInfo> _image;

        public TestImageProvider(string key, Task<ImageInfo> image)
        {
            _key = key;
            _image = image;
        }

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        public ImageStreamCompleter? LastCompleter { get; private set; }

        protected override ImageStreamCompleter LoadImage(string key)
        {
            return LastCompleter = new OneFrameImageStreamCompleter(_image, key);
        }
    }

    private sealed class FakeImage : IImage
    {
        public FakeImage(Size size)
        {
            Size = size;
        }

        public Size Size { get; }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            Pipeline = new PipelineOwner(RenderView);
            Pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public PipelineOwner Pipeline { get; }

        public void FlushBuild() => _owner.FlushBuild();

        public void Update(Widget widget)
        {
            _root.UpdateWidget(widget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            Pipeline.RequestLayout();
            Pipeline.FlushLayout(size);
            Pipeline.FlushCompositingBits();
            Pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _root.Unmount();
            Scheduler.PumpFrameForTests();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            public void UpdateWidget(Widget widget) => Update(widget);

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

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
