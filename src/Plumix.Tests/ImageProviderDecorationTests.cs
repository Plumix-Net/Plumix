using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImageProviderDecorationTests : IDisposable
{
    public ImageProviderDecorationTests()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
        ImageCache.Shared.MaximumSize = 1000;
        ImageCache.Shared.MaximumSizeBytes = 100L << 20;
    }

    public void Dispose()
    {
        ImageCache.Shared.Clear();
        ImageCache.Shared.ClearLiveImages();
    }

    [Fact]
    public void ImageConfiguration_CopyWith_PreservesUnspecifiedFields()
    {
        var bundle = new MemoryAssetBundle();
        var original = new ImageConfiguration(
            Bundle: bundle,
            DevicePixelRatio: 2,
            Locale: System.Globalization.CultureInfo.GetCultureInfo("en-US"),
            TextDirection: TextDirection.Ltr,
            Size: new Size(20, 30),
            Platform: ImageTargetPlatform.Windows);

        var updated = original.CopyWith(devicePixelRatio: 3, textDirection: TextDirection.Rtl);

        Assert.Same(bundle, updated.Bundle);
        Assert.Equal(3, updated.DevicePixelRatio);
        Assert.Equal(TextDirection.Rtl, updated.TextDirection);
        Assert.Equal(original.Locale, updated.Locale);
        Assert.Equal(original.Size, updated.Size);
        Assert.Equal(original.Platform, updated.Platform);
    }

    [Fact]
    public async Task ImageStream_QueuesListenersAndReplaysCompletedImageSynchronously()
    {
        var image = new FakeImage(new Size(12, 8));
        var completion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = new ImageStream();
        var firstCall = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstListener = new ImageStreamListener((info, synchronous) =>
        {
            Assert.False(synchronous);
            Assert.Same(image, info.Image);
            info.Dispose();
            firstCall.TrySetResult(true);
        });
        stream.AddListener(firstListener);
        stream.SetCompleter(new OneFrameImageStreamCompleter(completion.Task));

        completion.SetResult(new ImageInfo(image, scale: 2, debugLabel: "queued"));
        await firstCall.Task.WaitAsync(TimeSpan.FromSeconds(2));

        bool synchronousCall = false;
        var secondListener = new ImageStreamListener((info, synchronous) =>
        {
            synchronousCall = synchronous;
            Assert.Equal(2, info.Scale);
            info.Dispose();
        });
        stream.AddListener(secondListener);

        Assert.True(synchronousCall);
        stream.RemoveListener(firstListener);
        stream.RemoveListener(secondListener);
        Assert.Equal(1, image.DisposeCount);
    }

    [Fact]
    public async Task ImageProvider_UsesGlobalCacheAndEvictionReloadsKey()
    {
        var provider = new TestImageProvider("same", () => Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)))));

        var first = provider.Resolve(ImageConfiguration.Empty);
        var second = provider.Resolve(ImageConfiguration.Empty);

        Assert.Same(first.Completer, second.Completer);
        Assert.Equal(1, provider.LoadCount);
        Assert.True(await provider.Evict());

        var third = provider.Resolve(ImageConfiguration.Empty);
        Assert.NotSame(first.Completer, third.Completer);
        Assert.Equal(2, provider.LoadCount);
    }

    [Fact]
    public async Task ImageProvider_KeyFailure_ReachesListenerAddedAfterResolve()
    {
        var provider = new ThrowingImageProvider();
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stream = provider.Resolve(ImageConfiguration.Empty);
        await Task.Delay(20);
        stream.AddListener(new ImageStreamListener(
            (_, _) => { },
            OnError: (exception, _) => error.TrySetResult(exception)));

        var exception = await error.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task ImageProvider_AsyncLoadFailure_IsReplayableAndRemainsPendingUntilEvicted()
    {
        var provider = new TestImageProvider(
            "failed",
            () => Task.FromException<ImageInfo>(new InvalidOperationException("decode failed")));
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;

        var stream = provider.Resolve(ImageConfiguration.Empty);
        await Task.Delay(20);
        stream.AddListener(new ImageStreamListener(
            (_, _) => { },
            OnError: (exception, _) =>
            {
                Interlocked.Increment(ref calls);
                error.TrySetResult(exception);
            }));

        Assert.IsType<InvalidOperationException>(await error.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(1, calls);
        Assert.True(ImageCache.Shared.StatusForKey("failed").Pending);
    }

    [Fact]
    public void ImageCache_TrimsLeastRecentlyUsedCompletedEntriesByCount()
    {
        var cache = new ImageCache { MaximumSize = 2, MaximumSizeBytes = long.MaxValue };
        var a = CompletedCompleter(new FakeImage(new Size(1, 1)));
        var b = CompletedCompleter(new FakeImage(new Size(1, 1)));
        var c = CompletedCompleter(new FakeImage(new Size(1, 1)));

        cache.PutIfAbsent("a", () => a);
        cache.PutIfAbsent("b", () => b);
        Assert.True(SpinWait.SpinUntil(() => cache.CurrentSize == 2, TimeSpan.FromSeconds(2)));
        cache.PutIfAbsent("a", () => a);
        cache.PutIfAbsent("c", () => c);
        Assert.True(SpinWait.SpinUntil(() => cache.CurrentSize == 2, TimeSpan.FromSeconds(2)));

        Assert.True(cache.ContainsKey("a"));
        Assert.False(cache.ContainsKey("b"));
        Assert.True(cache.ContainsKey("c"));
        cache.Clear();
    }

    [Fact]
    public void ImageCache_TracksPendingKeepAliveAndLiveSeparately()
    {
        var cache = new ImageCache();
        var completion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completer = new OneFrameImageStreamCompleter(completion.Task);
        cache.PutIfAbsent("image", () => completer);

        Assert.Equal(new ImageCacheStatus(Pending: true, KeepAlive: false, Live: true), cache.StatusForKey("image"));

        completion.SetResult(new ImageInfo(new FakeImage(new Size(2, 3))));
        var completedStatus = new ImageCacheStatus(Pending: false, KeepAlive: true, Live: false);
        Assert.True(SpinWait.SpinUntil(
            () => cache.StatusForKey("image") == completedStatus,
            TimeSpan.FromSeconds(2)));
        Assert.Equal(completedStatus, cache.StatusForKey("image"));

        cache.PutIfAbsent("image", () => throw new Exception("must not reload"));
        Assert.True(cache.StatusForKey("image").Live);
        cache.ClearLiveImages();

        Assert.False(cache.StatusForKey("image").Live);
        Assert.True(cache.StatusForKey("image").KeepAlive);
        cache.Clear();
    }

    [Fact]
    public void MemoryFileAndNetworkProviders_MatchFlutterKeyEqualityRules()
    {
        byte[] bytes = new byte[] { 1, 2, 3 };
        Assert.Equal(new MemoryImage(bytes, 2), new MemoryImage(bytes, 2));
        Assert.NotEqual(new MemoryImage(bytes, 2), new MemoryImage([1, 2, 3], 2));

        string file = Path.Combine(Path.GetTempPath(), "plumix-image.png");
        Assert.Equal(new FileImage(file, 1.5), new FileImage(file, 1.5));
        Assert.NotEqual(new FileImage(file, 1), new FileImage(file, 2));

        var headersA = new Dictionary<string, string> { ["Authorization"] = "token" };
        var headersB = new Dictionary<string, string> { ["authorization"] = "token" };
        Assert.NotEqual(new NetworkImage("https://example.com/a.png", headers: headersA),
            new NetworkImage("https://example.com/a.png", headers: headersB));
        Assert.NotEqual(new NetworkImage("https://example.com/a.png"),
            new NetworkImage("https://example.com/b.png"));
    }

    [Fact]
    public async Task ResizeImage_ValidatesDimensionsAndKeysUnderlyingProvider()
    {
        var provider = new TestImageProvider("source", () => Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)))));
        Assert.Same(provider, ResizeImage.ResizeIfNeeded(null, null, provider));
        Assert.Throws<ArgumentException>(() => new ResizeImage(provider));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResizeImage(provider, width: 0));

        var resize = new ResizeImage(provider, width: 40, allowUpscaling: false);
        var first = await resize.ObtainKey(ImageConfiguration.Empty);
        var second = await resize.ObtainKey(new ImageConfiguration(DevicePixelRatio: 3));

        Assert.Equal(first, second);
        Assert.Equal("source", first.ProviderCacheKey);
        Assert.Equal(40, first.Width);
        Assert.False(first.AllowUpscaling);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.25, 2.0)]
    [InlineData(2.25, 2.0)]
    [InlineData(3.8, 4.0)]
    public async Task AssetImage_SelectsFlutterLikeDevicePixelRatioVariant(double dpr, double expectedScale)
    {
        var bundle = new MemoryAssetBundle(
            assets:
            [
                "icons/a.png",
                "icons/2.0x/a.png",
                "icons/3.0x/a.png",
                "icons/4.0x/a.png",
            ]);
        var provider = new AssetImage("icons/a.png", bundle);

        var key = await provider.ObtainKey(new ImageConfiguration(DevicePixelRatio: dpr));

        Assert.Equal(expectedScale, key.Scale);
        string expectedName = expectedScale switch
        {
            1 => "icons/a.png",
            2 => "icons/2.0x/a.png",
            3 => "icons/3.0x/a.png",
            4 => "icons/4.0x/a.png",
            _ => throw new ArgumentOutOfRangeException(nameof(expectedScale)),
        };
        Assert.Equal(expectedName, key.Name);
    }

    [Fact]
    public void DecorationImage_ValidatesAndExcludesOnErrorFromEquality()
    {
        var provider = new TestImageProvider("image", () => Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)))));
        var first = new DecorationImage(provider, onError: (_, _) => { }, opacity: 0.5);
        var second = new DecorationImage(provider, onError: (_, _) => throw new Exception(), opacity: 0.5);

        Assert.Equal(first, second);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecorationImage(provider, scale: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecorationImage(provider, opacity: 2));
        Assert.Throws<ArgumentException>(() => new ColorFilter.Matrix(1, 2, 3));
        Assert.Throws<ArgumentException>(() => new DecorationImage(
            provider,
            fit: BoxFit.Cover,
            centerSlice: new Rect(2, 2, 4, 4)));
    }

    [Fact]
    public void PaintPlan_CoverCropsSourceAndContainCentersDestination()
    {
        var cover = ImagePainting.CreatePaintPlan(
            new Rect(0, 0, 100, 100),
            new Size(100, 50),
            scale: 1,
            fit: BoxFit.Cover,
            alignment: Alignment.Center,
            centerSlice: null,
            repeat: ImageRepeat.NoRepeat,
            flipHorizontally: false);
        var contain = ImagePainting.CreatePaintPlan(
            new Rect(0, 0, 100, 100),
            new Size(100, 50),
            scale: 1,
            fit: BoxFit.Contain,
            alignment: Alignment.Center,
            centerSlice: null,
            repeat: ImageRepeat.NoRepeat,
            flipHorizontally: false);

        Assert.Equal(new Rect(25, 0, 50, 50), cover.SourceRect);
        Assert.Equal(new Rect(0, 0, 100, 100), Assert.Single(cover.DestinationRects));
        Assert.Equal(new Rect(0, 25, 100, 50), Assert.Single(contain.DestinationRects));
    }

    [Fact]
    public void PaintPlan_RepeatAndHorizontalFlipMatchFlutterGeometry()
    {
        var repeated = ImagePainting.CreatePaintPlan(
            new Rect(0, 0, 25, 25),
            new Size(10, 10),
            scale: 1,
            fit: BoxFit.None,
            alignment: Alignment.Center,
            centerSlice: null,
            repeat: ImageRepeat.Repeat,
            flipHorizontally: false);
        var flipped = ImagePainting.CreatePaintPlan(
            new Rect(0, 0, 30, 20),
            new Size(10, 10),
            scale: 1,
            fit: BoxFit.None,
            alignment: Alignment.TopLeft,
            centerSlice: null,
            repeat: ImageRepeat.NoRepeat,
            flipHorizontally: true);

        Assert.Equal(9, repeated.DestinationRects.Count);
        Assert.Equal(new Rect(20, 0, 10, 10), Assert.Single(flipped.DestinationRects));
        Assert.Equal(15, flipped.FlipAxisX);
        Assert.Null(flipped.ClipRect);
    }

    [Fact]
    public void NinePatch_PreservesLogicalEdgeSizeWhenImageScaleIsTwo()
    {
        var patches = ImagePainting.GenerateNinePatchRects(
            new Size(60, 60),
            new Rect(20, 20, 20, 20),
            new Rect(0, 0, 100, 100),
            scale: 2);

        Assert.Equal(9, patches.Count);
        Assert.Equal(new Rect(0, 0, 10, 10), patches[0].Destination);
        Assert.Equal(new Rect(10, 10, 80, 80), patches[4].Destination);
        Assert.Equal(new Rect(90, 90, 10, 10), patches[8].Destination);
    }

    [Fact]
    public void PaintImage_CenterSliceGeneratesNineDrawCommands()
    {
        var root = new ContainerLayer();
        var context = new PaintingContext(root);

        ImagePainting.PaintImage(
            context,
            new Rect(0, 0, 60, 60),
            new FakeImage(new Size(30, 30)),
            fit: BoxFit.Fill,
            centerSlice: new Rect(10, 10, 10, 10));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.Equal(9, CountPictureCommands(picture));
    }

    [Fact]
    public void DecorationImage_LerpPaintsBothImagesDuringTransition()
    {
        var firstCompletion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new TestImageProvider(
            "lerp-first",
            () => firstCompletion.Task);
        var second = new TestImageProvider(
            "lerp-second",
            () => secondCompletion.Task);
        int repaintCount = 0;
        using var painter = DecorationImage.Lerp(
                new DecorationImage(first, fit: BoxFit.Cover),
                new DecorationImage(second, fit: BoxFit.Cover),
                0.25)!
            .CreatePainter(() => Interlocked.Increment(ref repaintCount));

        painter.Paint(
            new PaintingContext(new ContainerLayer()),
            new Rect(0, 0, 20, 20),
            ImageConfiguration.Empty,
            shape: BoxShape.Circle);
        firstCompletion.SetResult(new ImageInfo(new FakeImage(new Size(10, 10))));
        secondCompletion.SetResult(new ImageInfo(new FakeImage(new Size(10, 10))));
        Assert.True(SpinWait.SpinUntil(() => repaintCount >= 2, TimeSpan.FromSeconds(2)));

        var root = new ContainerLayer();
        painter.Paint(
            new PaintingContext(root),
            new Rect(0, 0, 20, 20),
            ImageConfiguration.Empty,
            shape: BoxShape.Circle);

        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.Equal(2, CountPictureCommands(picture));
    }

    [Fact]
    public async Task DecorationImagePainter_RequestsRepaintAfterAsyncImageAndUsesDirectionality()
    {
        var image = new FakeImage(new Size(20, 10));
        var completion = new TaskCompletionSource<ImageInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new TestImageProvider("async", () => completion.Task);
        var repaint = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var painter = new DecorationImage(
            provider,
            fit: BoxFit.None,
            matchTextDirection: true).CreatePainter(() => repaint.TrySetResult(true));
        var root = new ContainerLayer();
        var context = new PaintingContext(root);

        painter.Paint(
            context,
            new Rect(0, 0, 40, 20),
            new ImageConfiguration(TextDirection: TextDirection.Rtl));
        Assert.Empty(root.Children);

        completion.SetResult(new ImageInfo(image));
        await repaint.Task.WaitAsync(TimeSpan.FromSeconds(2));
        painter.Paint(
            context,
            new Rect(0, 0, 40, 20),
            new ImageConfiguration(TextDirection: TextDirection.Rtl));

        Assert.NotEmpty(root.Children);
        Assert.Throws<InvalidOperationException>(() => new DecorationImage(
                provider,
                matchTextDirection: true)
            .CreatePainter(() => { })
            .Paint(new PaintingContext(new ContainerLayer()), new Rect(0, 0, 10, 10), ImageConfiguration.Empty));
    }

    [Fact]
    public void BoxDecoration_ImageUpdateDisposesOldPainterListener()
    {
        var first = new TestImageProvider("first", () => Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)))));
        var second = new TestImageProvider("second", () => Task.FromResult(new ImageInfo(new FakeImage(new Size(10, 10)))));
        var render = new RenderDecoratedBox(
            new BoxDecoration(Image: new DecorationImage(first)),
            configuration: new ImageConfiguration(TextDirection: TextDirection.Ltr));
        render.Layout(BoxConstraints.Tight(new Size(20, 20)));
        render.Paint(new PaintingContext(new ContainerLayer()), new Point());
        Assert.True(SpinWait.SpinUntil(() => first.LastCompleter?.HasListeners == true, TimeSpan.FromSeconds(2)));

        render.Decoration = new BoxDecoration(Image: new DecorationImage(second));

        Assert.False(first.LastCompleter!.HasListeners);
    }

    private static OneFrameImageStreamCompleter CompletedCompleter(FakeImage image)
    {
        return new OneFrameImageStreamCompleter(Task.FromResult(new ImageInfo(image)));
    }

    private static int CountPictureCommands(PictureLayer pictureLayer)
    {
        var commands = typeof(PictureLayer).GetField("_commands", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(pictureLayer) as System.Collections.ICollection;
        return commands!.Count;
    }

    private sealed class FakeImage : IImage, IDisposable
    {
        public FakeImage(Size size) => Size = size;
        public Size Size { get; }
        public int DisposeCount { get; private set; }
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
        public void Dispose() => DisposeCount++;
    }

    private sealed class TestImageProvider : ImageProvider<string>
    {
        private readonly string _key;
        private readonly Func<Task<ImageInfo>> _loader;
        public TestImageProvider(string key, Func<Task<ImageInfo>> loader) { _key = key; _loader = loader; }
        public int LoadCount { get; private set; }
        public ImageStreamCompleter? LastCompleter { get; private set; }
        public override ValueTask<string> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(_key);
        protected override ImageStreamCompleter LoadImage(string key)
        {
            LoadCount++;
            return LastCompleter = new OneFrameImageStreamCompleter(_loader(), debugLabel: key);
        }
    }

    private sealed class ThrowingImageProvider : ImageProvider<string>
    {
        public override ValueTask<string> ObtainKey(ImageConfiguration configuration)
        {
            throw new InvalidOperationException("key failed");
        }
        protected override ImageStreamCompleter LoadImage(string key) => throw new NotSupportedException();
    }

    private sealed class MemoryAssetBundle : AssetBundle
    {
        private readonly IReadOnlyList<string> _assets;
        public MemoryAssetBundle(IReadOnlyList<string>? assets = null) => _assets = assets ?? [];
        public override Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream());
        public override Task<IReadOnlyList<string>> ListAssetsAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_assets);
    }
}
