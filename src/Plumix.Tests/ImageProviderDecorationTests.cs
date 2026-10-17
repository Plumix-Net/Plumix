using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;
using IOPath = System.IO.Path;

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
        await EventLoopPump.WaitFor(firstCall.Task);

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
        await EventLoopPump.WaitUntil(() => stream.Completer is not null);
        stream.AddListener(new ImageStreamListener(
            (_, _) => { },
            OnError: (exception, _) => error.TrySetResult(exception)));

        var exception = await EventLoopPump.WaitFor(error.Task);
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
        await EventLoopPump.WaitUntil(() => stream.Completer is not null);
        stream.AddListener(new ImageStreamListener(
            (_, _) => { },
            OnError: (exception, _) =>
            {
                Interlocked.Increment(ref calls);
                error.TrySetResult(exception);
            }));

        Assert.IsType<InvalidOperationException>(await EventLoopPump.WaitFor(error.Task));
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
        Assert.True(EventLoopPump.SpinUntil(() => cache.CurrentSize == 2));
        cache.PutIfAbsent("a", () => a);
        cache.PutIfAbsent("c", () => c);
        Assert.True(EventLoopPump.SpinUntil(() => cache.CurrentSize == 2));

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
        Assert.True(EventLoopPump.SpinUntil(() => cache.StatusForKey("image") == completedStatus));
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

        string file = IOPath.Combine(IOPath.GetTempPath(), "plumix-image.png");
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
        var canvas = new Canvas(new PictureRecorder());

        painter.Paint(canvas, new Rect(0, 0, 40, 20), null, new ImageConfiguration(TextDirection: TextDirection.Rtl));
        Assert.Empty(canvas.DebugCalls);

        completion.SetResult(new ImageInfo(image));
        await EventLoopPump.WaitFor(repaint.Task);
        painter.Paint(canvas, new Rect(0, 0, 40, 20), null, new ImageConfiguration(TextDirection: TextDirection.Rtl));

        Assert.Contains(canvas.DebugCalls, call => call.Method == "drawImageRect");
        Assert.Contains(canvas.DebugCalls, call => call.Method == "scale" && call.Dx == -1.0);
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
        Assert.True(EventLoopPump.SpinUntil(() => first.LastCompleter?.HasListeners == true));

        render.Decoration = new BoxDecoration(Image: new DecorationImage(second));

        Assert.False(first.LastCompleter!.HasListeners);
    }

    private static OneFrameImageStreamCompleter CompletedCompleter(FakeImage image)
    {
        return new OneFrameImageStreamCompleter(Task.FromResult(new ImageInfo(image)));
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
