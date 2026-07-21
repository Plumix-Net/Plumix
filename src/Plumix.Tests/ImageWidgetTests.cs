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
    public void FadeInImage_DefaultsFactoriesAndGuardsMatchFlutterSurface()
    {
        byte[] bytes = [1, 2, 3];
        var placeholder = new MemoryImage(bytes);
        var target = new NetworkImage("https://example.com/target.png");
        var fade = new FadeInImage(placeholder, target);

        Assert.Same(placeholder, fade.Placeholder);
        Assert.Same(target, fade.Image);
        Assert.Equal(TimeSpan.FromMilliseconds(300), fade.FadeOutDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(700), fade.FadeInDuration);
        Assert.Equal(0.684643, fade.FadeOutCurve(0.5), 6);
        Assert.Equal(0.315357, fade.FadeInCurve(0.5), 6);
        Assert.Equal(FilterQuality.Medium, fade.FilterQuality);
        Assert.Null(fade.PlaceholderFilterQuality);
        Assert.Equal((AlignmentGeometry)Alignment.Center, fade.Alignment);
        Assert.Equal(ImageRepeat.NoRepeat, fade.Repeat);
        Assert.False(fade.ExcludeFromSemantics);
        Assert.False(fade.MatchTextDirection);

        var memoryNetwork = FadeInImage.MemoryNetwork(
            bytes,
            "https://example.com/memory.png",
            placeholderScale: 2.0,
            imageScale: 3.0,
            placeholderCacheWidth: 12,
            imageCacheHeight: 18);
        var placeholderResize = Assert.IsType<ResizeImage>(memoryNetwork.Placeholder);
        var memoryProvider = Assert.IsType<MemoryImage>(placeholderResize.ImageProvider);
        var imageResize = Assert.IsType<ResizeImage>(memoryNetwork.Image);
        var networkProvider = Assert.IsType<NetworkImage>(imageResize.ImageProvider);
        Assert.Equal(12, placeholderResize.Width);
        Assert.Equal(2.0, memoryProvider.Scale);
        Assert.Equal(18, imageResize.Height);
        Assert.Equal(3.0, networkProvider.Scale);

        var assetNetwork = FadeInImage.AssetNetwork(
            "images/placeholder.png",
            "https://example.com/asset.png",
            placeholderScale: 2.0);
        Assert.IsType<ExactAssetImage>(assetNetwork.Placeholder);
        Assert.IsType<NetworkImage>(assetNetwork.Image);
        Assert.IsType<AssetImage>(FadeInImage.AssetNetwork(
            "images/placeholder.png",
            "https://example.com/asset.png").Placeholder);

        Assert.Throws<ArgumentOutOfRangeException>(() => new FadeInImage(
            placeholder,
            target,
            fadeOutDuration: TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FadeInImage(placeholder, target, width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FadeInImage.MemoryNetwork(
            bytes,
            "https://example.com/image.png",
            imageCacheWidth: 0));
    }

    [Fact]
    public void ImageIcon_UsesIconThemeSizeColorOpacityAndSemantics()
    {
        byte[] bytes = [1, 2, 3];
        var provider = new MemoryImage(bytes);
        var icon = new ImageIcon(provider, semanticLabel: "Photo icon");
        Widget widget = new IconTheme(
            new IconThemeData(Color: Colors.Red, Size: 36, Opacity: 0.5),
            icon);
        using var harness = new WidgetRenderHarness(widget);

        Assert.Same(provider, icon.Image);
        var image = Assert.IsType<Image>(harness.FindWidget<Image>());
        Assert.Same(provider, image.ImageProvider);
        Assert.Equal(36, image.Width);
        Assert.Equal(36, image.Height);
        Assert.Equal(BoxFit.ScaleDown, image.Fit);
        Assert.True(image.ExcludeFromSemantics);
        Assert.Equal(Color.FromArgb(128, 255, 0, 0), image.Color);

        var semantics = Assert.IsType<Semantics>(harness.FindWidget<Semantics>());
        Assert.Equal("Photo icon", semantics.Label);
        Assert.False(semantics.Flags.HasFlag(SemanticsFlags.IsImage));

        using var emptyHarness = new WidgetRenderHarness(new ImageIcon(null, semanticLabel: "Empty icon"));
        var box = Assert.IsType<SizedBox>(emptyHarness.FindWidget<SizedBox>());
        Assert.Equal(24, box.Width);
        Assert.Equal(24, box.Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageIcon(provider, size: -1));
    }

    [Fact]
    public async Task FadeInImage_FadesPlaceholderThenTargetAndPublishesSingleImageSemantics()
    {
        var targetCompletion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var placeholderImage = new FakeImage(new Size(10, 10));
        var targetImage = new FakeImage(new Size(20, 20));
        var placeholderProvider = new SynchronousImageProvider("fade-placeholder", placeholderImage);
        var targetProvider = new TestImageProvider("fade-target", targetCompletion.Task);
        var fade = new FadeInImage(
            placeholderProvider,
            targetProvider,
            imageSemanticLabel: "Fading image",
            fadeOutDuration: TimeSpan.FromMilliseconds(300),
            fadeInDuration: TimeSpan.FromMilliseconds(700),
            width: 40,
            height: 40);
        using var harness = new WidgetRenderHarness(fade);

        await PumpUntilAsync(
            harness,
            () => FindRenderImages(harness.RenderView).Any(image => ReferenceEquals(image.Image, placeholderImage)));
        IReadOnlyList<RenderImage> initialImages = FindRenderImages(harness.RenderView);
        RenderImage initialTarget = Assert.Single(initialImages, image => image.Image is null);
        RenderImage placeholder = Assert.Single(
            initialImages,
            image => ReferenceEquals(image.Image, placeholderImage));
        Assert.Equal(0.0, initialTarget.Opacity!.Value, 6);
        Assert.Equal(1.0, placeholder.Opacity!.Value, 6);

        targetCompletion.SetResult(new ImageInfo(targetImage));
        await PumpUntilAsync(
            harness,
            () => FindRenderImages(harness.RenderView).Any(image => ReferenceEquals(image.Image, targetImage)));
        double start = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start + 0.15));
        harness.FlushBuild();

        IReadOnlyList<RenderImage> fadeOutImages = FindRenderImages(harness.RenderView);
        RenderImage fadingTarget = Assert.Single(
            fadeOutImages,
            image => ReferenceEquals(image.Image, targetImage));
        RenderImage fadingPlaceholder = Assert.Single(
            fadeOutImages,
            image => ReferenceEquals(image.Image, placeholderImage));
        Assert.Equal(0.0, fadingTarget.Opacity!.Value, 6);
        Assert.InRange(fadingPlaceholder.Opacity!.Value, 0.27, 0.35);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start + 0.65));
        harness.FlushBuild();
        IReadOnlyList<RenderImage> fadeInImages = FindRenderImages(harness.RenderView);
        fadingTarget = Assert.Single(fadeInImages, image => ReferenceEquals(image.Image, targetImage));
        fadingPlaceholder = Assert.Single(
            fadeInImages,
            image => ReferenceEquals(image.Image, placeholderImage));
        Assert.InRange(fadingTarget.Opacity!.Value, 0.27, 0.35);
        Assert.Equal(0.0, fadingPlaceholder.Opacity!.Value, 6);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(start + 1.1));
        harness.FlushBuild();
        RenderImage finalImage = Assert.Single(FindRenderImages(harness.RenderView));
        Assert.Same(targetImage, finalImage.Image);
        Assert.Equal(1.0, finalImage.Opacity!.Value, 6);

        harness.Pump(new Size(80, 80));
        harness.Pipeline.FlushSemantics();
        var root = Assert.IsType<SemanticsNode>(harness.Pipeline.SemanticsOwner.RootNode);
        var imageNode = Assert.Single(Flatten(root), node => node.Flags.HasFlag(SemanticsFlags.IsImage));
        Assert.Equal("Fading image", imageNode.Label);
    }

    [Fact]
    public void FadeInImage_SynchronouslyLoadedTargetSkipsPlaceholder()
    {
        var targetImage = new FakeImage(new Size(20, 20));
        var placeholderImage = new FakeImage(new Size(10, 10));
        var fade = new FadeInImage(
            new SynchronousImageProvider("sync-placeholder", placeholderImage),
            new SynchronousImageProvider("sync-target", targetImage));
        using var harness = new WidgetRenderHarness(fade);

        RenderImage image = Assert.Single(FindRenderImages(harness.RenderView));
        Assert.Same(targetImage, image.Image);
        Assert.Equal(1.0, image.Opacity!.Value, 6);
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

    private static IReadOnlyList<RenderImage> FindRenderImages(RenderObject root)
    {
        var result = new List<RenderImage>();
        void Visit(RenderObject current)
        {
            if (current is RenderImage image)
            {
                result.Add(image);
            }

            current.VisitChildren(Visit);
        }

        Visit(root);
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

    private sealed class SynchronousImageProvider : ImageProvider<string>
    {
        private readonly string _key;
        private readonly IImage _image;

        public SynchronousImageProvider(string key, IImage image)
        {
            _key = key;
            _image = image;
        }

        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);

        protected override ImageStreamCompleter LoadImage(string key) => new SynchronousImageCompleter(_image, key);
    }

    private sealed class SynchronousImageCompleter : ImageStreamCompleter
    {
        public SynchronousImageCompleter(IImage image, string debugLabel)
        {
            DebugLabel = debugLabel;
            SetImage(new ImageInfo(image, debugLabel: debugLabel));
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

        public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root);

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

        private static T? FindWidget<T>(Element element) where T : Widget
        {
            if (element.Widget is T widget)
            {
                return widget;
            }

            T? result = null;
            element.VisitChildren(child => result ??= FindWidget<T>(child));
            return result;
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
