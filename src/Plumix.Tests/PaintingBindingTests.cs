using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/painting/binding.dart
// flutter/packages/flutter/lib/src/painting/shader_warm_up.dart
// Flutter tests: painting/binding_test.dart and painting/shader_warm_up_test.dart.
// Engine prerequisites: flutter/engine/src/flutter/testing/dart/codec_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PaintingBindingTests : IDisposable
{
    private readonly PaintingBinding _painting = PaintingBinding.Instance;
    private readonly ServicesBinding _services = ServicesBinding.Instance;
    private readonly ShaderWarmUp? _warmUp = PaintingBinding.ShaderWarmUp;
    private readonly ShaderWarmUpPictureCallback _capturePicture = PaintingDebug.DebugCaptureShaderWarmUpPicture;
    private readonly ShaderWarmUpImageCallback _captureImage = PaintingDebug.DebugCaptureShaderWarmUpImage;

    public PaintingBindingTests()
    {
        SchedulerBinding.ResetForTests();
        PaintingBinding.ShaderWarmUp = null;
    }

    public void Dispose()
    {
        if (!ReferenceEquals(PaintingBinding.Instance, _painting))
        {
            PaintingBinding.Instance.ImageCache.Clear();
            PaintingBinding.Instance.ImageCache.ClearLiveImages();
        }

        PaintingBinding.Instance = _painting;
        ServicesBinding.Instance = _services;
        PaintingBinding.ShaderWarmUp = _warmUp;
        PaintingDebug.DebugCaptureShaderWarmUpPicture = _capturePicture;
        PaintingDebug.DebugCaptureShaderWarmUpImage = _captureImage;
        SchedulerBinding.ResetForTests();
    }

    [Fact]
    public void BindingCreatesTheSharedCacheAndWarmUpDefaultsToNull()
    {
        var binding = new PaintingBinding();
        Assert.Same(binding, PaintingBinding.Instance);
        Assert.Same(binding.ImageCache, ImageCache.Shared);
        Assert.Equal(1000, binding.ImageCache.MaximumSize);
        Assert.Equal(100L << 20, binding.ImageCache.MaximumSizeBytes);
        Assert.Null(PaintingBinding.ShaderWarmUp);
    }

    [Fact]
    public void EvictClearsAssetCacheThenKeepAliveAndLiveImages()
    {
        var services = new RecordingServicesBinding();
        ServicesBinding.Instance = services;
        var binding = new TestPaintingBinding();
        var cache = Assert.IsType<RecordingImageCache>(binding.ImageCache);
        cache.BeforeClear = () => Assert.Equal("/path/to/asset.png", services.Bundle.Evicted);

        binding.Evict("/path/to/asset.png");

        Assert.Equal(1, cache.ClearCount);
        Assert.Equal(1, cache.LiveClearCount);
        Assert.Equal(0, services.Bundle.ClearCount);
    }

    [Fact]
    public async Task MemoryPressureChannelClearsPendingAndKeepAliveButPreservesLiveImages()
    {
        var services = new RecordingServicesBinding();
        ServicesBinding.Instance = services;
        var binding = new TestPaintingBinding();
        var cache = Assert.IsType<RecordingImageCache>(binding.ImageCache);
        cache.BeforeClear = () => Assert.Equal(1, services.Bundle.ClearCount);
        var completer = new TestCompleter();
        var listener = new ImageStreamListener((image, _) => image.Dispose());
        completer.AddListener(listener);
        cache.PutIfAbsent(1, () => completer);
        completer.Emit(new ImageInfo(NewImage(8, 4)));
        Assert.Equal(1, cache.CurrentSize);
        Assert.True(cache.StatusForKey(1).Live);
        var pending = new TestCompleter();
        cache.PutIfAbsent(2, () => pending);
        Assert.True(cache.StatusForKey(2).Pending);

        await services.DefaultBinaryMessenger.HandlePlatformMessage(
            "flutter/system",
            new JsonMessageCodec().EncodeMessage(new Dictionary<string, object?> { ["type"] = "memoryPressure" }),
            _ => { });

        Assert.Equal(0, cache.CurrentSize);
        Assert.False(cache.StatusForKey(2).Pending);
        Assert.True(cache.StatusForKey(1).Live);
        Assert.Equal(1, cache.ClearCount);
        Assert.Equal(0, cache.LiveClearCount);
        completer.RemoveListener(listener);
        binding.Evict("unused");
        Assert.False(cache.StatusForKey(1).Tracked);
    }

    [Fact]
    public async Task FontsChangeUsesASetAndAwaitsServicesBeforeNotifying()
    {
        var services = new DeferredServicesBinding();
        ServicesBinding.Instance = services;
        var binding = new PaintingBinding();
        int notifications = 0;
        Action listener = () => notifications++;
        binding.SystemFonts.AddListener(listener);
        binding.SystemFonts.AddListener(listener);
        Task change = binding.HandleSystemMessage(new Dictionary<string, object?> { ["type"] = "fontsChange" });
        Assert.Equal(0, notifications);
        services.Completion.SetResult();
        await change;
        Assert.Equal(1, notifications);
        binding.SystemFonts.RemoveListener(listener);
        await binding.HandleSystemMessage(new Dictionary<string, object?> { ["type"] = "fontsChange" });
        Assert.Equal(1, notifications);
        await binding.HandleSystemMessage(new Dictionary<string, object?> { ["type"] = "unknown" });
        Assert.Equal(0, binding.ImageCache.CurrentSize);
    }

    [Fact]
    public async Task ConfiguredWarmUpStartsDuringInitializationAndRasterizesItsPicture()
    {
        var warmUp = new TestWarmUp();
        PaintingBinding.ShaderWarmUp = warmUp;
        Picture? capturedPicture = null;
        int captures = 0;
        PaintingDebug.DebugCaptureShaderWarmUpPicture = picture =>
        {
            capturedPicture = picture;
            Assert.Equal(1, picture.DrawCommandCount);
            return true;
        };
        PaintingDebug.DebugCaptureShaderWarmUpImage = image =>
        {
            Assert.Equal(new PixelSize(11, 7), image.PixelSize);
            Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 2, 2));
            captures++;
            return true;
        };

        _ = new PaintingBinding();
        Assert.True(warmUp.Started);
        Assert.Equal(new Size(100, 100), new DefaultSizeWarmUp().Size);
        warmUp.Completion.SetResult();
        await EventLoopPump.WaitUntil(() => warmUp.Finished);
        if (Constants.KDebugMode)
        {
            Assert.Equal(1, captures);
            Assert.NotNull(capturedPicture);
            Assert.True(capturedPicture.DebugDisposed);
        }
        else
        {
            Assert.Equal(0, captures);
        }
    }

    [Theory]
    [InlineData(null, null, false, 30, 20)]
    [InlineData(15, null, false, 15, 10)]
    [InlineData(null, 7, false, 11, 7)]
    [InlineData(8, null, false, 8, 5)]
    [InlineData(60, null, false, 30, 20)]
    [InlineData(null, 60, false, 30, 20)]
    [InlineData(60, 5, false, 30, 5)]
    [InlineData(60, 40, true, 60, 40)]
    [InlineData(8, 11, false, 8, 11)]
    public async Task DecodeFromBufferUsesDartSizing(
        int? width, int? height, bool upscale, int expectedWidth, int expectedHeight)
    {
        var binding = new PaintingBinding();
        ImmutableBuffer buffer = await ImmutableBuffer.FromUint8List(PngBytes(30, 20));
#pragma warning disable CS0618 // Exercise Dart's retained deprecated entry point.
        using Codec codec = await binding.InstantiateImageCodecFromBuffer(buffer, width, height, upscale);
#pragma warning restore CS0618
        FrameInfo frame = await codec.GetNextFrame();
        using Bitmap image = frame.Image;
        Assert.Equal(new PixelSize(expectedWidth, expectedHeight), image.PixelSize);
        Assert.Equal(TimeSpan.Zero, frame.Duration);
        Assert.Equal(1, codec.FrameCount);
        Assert.Equal(0, codec.RepetitionCount);
        if (Constants.KDebugMode) Assert.True(buffer.DebugDisposed);
    }

    [Fact]
    public async Task SizeCallbackReceivesIntrinsicPixelsAndFramesSurviveCodecDisposal()
    {
        var binding = new PaintingBinding();
        int calls = 0;
        ImmutableBuffer buffer = await ImmutableBuffer.FromUint8List(PngBytes(30, 20));
        Codec codec = await binding.InstantiateImageCodecWithSize(buffer, (width, height) =>
        {
            Assert.Equal(30, width);
            Assert.Equal(20, height);
            calls++;
            return new TargetImageSize(15, 10);
        });
        FrameInfo first = await codec.GetNextFrame();
        FrameInfo second = await codec.GetNextFrame();
        codec.Dispose();
        Assert.Equal(1, calls);
        Assert.Equal(RasterBackend.ReadPixels(first.Image), RasterBackend.ReadPixels(second.Image));
        Assert.Equal(new PixelSize(15, 10), first.Image.PixelSize);
        first.Image.Dispose();
        second.Image.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = codec.GetNextFrame(); });
    }

    [Fact]
    public async Task ImmutableBufferCopiesBytesAndCallbackFailureDisposesIt()
    {
        byte[] bytes = PngBytes(6, 4);
        ImmutableBuffer buffer = await ImmutableBuffer.FromUint8List(bytes);
        Array.Clear(bytes);
        Assert.Equal(bytes.Length, buffer.Length);
        using Codec codec = await PaintingBinding.Instance.InstantiateImageCodecWithSize(buffer);
        FrameInfo frame = await codec.GetNextFrame();
        frame.Image.Dispose();

        ImmutableBuffer failed = await ImmutableBuffer.FromUint8List(PngBytes(6, 4));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PaintingBinding.Instance.InstantiateImageCodecWithSize(failed, (_, _) =>
                throw new InvalidOperationException("target failed")));
        if (Constants.KDebugMode) Assert.True(failed.DebugDisposed);
        ImmutableBuffer invalid = await ImmutableBuffer.FromUint8List([1, 2, 3]);
        await Assert.ThrowsAnyAsync<Exception>(() => PaintingBinding.Instance.InstantiateImageCodecWithSize(invalid));
        if (Constants.KDebugMode) Assert.False(invalid.DebugDisposed);
        invalid.Dispose();
    }

    [DebugOnlyFact]
    public async Task CacheDimensionsAndDisposedBuffersUseDebugAssertions()
    {
        Assert.Throws<AssertionError>(() => new TargetImageSize(width: 0));
        Assert.Throws<AssertionError>(() => new TargetImageSize(height: -1));
        ImmutableBuffer buffer = await ImmutableBuffer.FromUint8List(PngBytes(3, 2));
#pragma warning disable CS0618
        Assert.Throws<AssertionError>(() =>
        {
            _ = PaintingBinding.Instance.InstantiateImageCodecFromBuffer(buffer, cacheWidth: 0);
        });
#pragma warning restore CS0618
        Assert.False(buffer.DebugDisposed);
        buffer.Dispose();
        Assert.Throws<AssertionError>(buffer.Dispose);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => ImageDecoding.InstantiateImageCodecWithSize(buffer));
        Assert.Equal("TargetImageSize(null x null)", new TargetImageSize().ToString());
    }

    [Fact]
    public async Task MemoryImageUsesTheBindingsDecoderAndCache()
    {
        var binding = new DecoderBinding();
        var stream = new MemoryImage(PngBytes(6, 4), scale: 2).Resolve(ImageConfiguration.Empty);
        ImageInfo? decoded = null;
        var listener = new ImageStreamListener((image, _) => decoded = image);
        stream.AddListener(listener);
        await EventLoopPump.WaitUntil(() => decoded is not null);
        Assert.Equal(1, binding.DecodeCalls);
        Assert.Equal(1, binding.ImageCache.CurrentSize);
        Assert.Equal(2, decoded!.Scale);
        Assert.Equal(new PixelSize(6, 4), Assert.IsType<Bitmap>(decoded.Image).PixelSize);
        decoded.Dispose();
        stream.RemoveListener(listener);
    }

    private static Bitmap NewImage(int width, int height) =>
        SceneBuilderTests.DrawPaintPicture(0xFFFF0000).ToImageSync(width, height);

    private static byte[] PngBytes(int width, int height)
    {
        using Bitmap image = NewImage(width, height);
        using var bytes = new MemoryStream();
        image.Save(bytes);
        return bytes.ToArray();
    }

    private sealed class TestPaintingBinding : PaintingBinding
    {
        protected override ImageCache CreateImageCache() => new RecordingImageCache();
    }

    private sealed class RecordingImageCache : ImageCache
    {
        public int ClearCount { get; private set; }
        public int LiveClearCount { get; private set; }
        public Action? BeforeClear { get; set; }

        public override void Clear()
        {
            BeforeClear?.Invoke();
            ClearCount++;
            base.Clear();
        }

        public override void ClearLiveImages()
        {
            LiveClearCount++;
            base.ClearLiveImages();
        }
    }

    private sealed class RecordingServicesBinding : TestServicesBinding
    {
        public RecordingBundle Bundle { get; } = new();
        public override AssetBundle RootBundle => Bundle;
    }

    private sealed class RecordingBundle : AssetBundle
    {
        public string? Evicted { get; private set; }
        public int ClearCount { get; private set; }
        public override Task<Stream> LoadAsync(string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Clear() => ClearCount++;
        public override void Evict(string key) => Evicted = key;
    }

    private sealed class DeferredServicesBinding : TestServicesBinding
    {
        public TaskCompletionSource Completion { get; } = new();
        public override Task HandleSystemMessage(object systemMessage) => Completion.Task;
    }

    private sealed class TestCompleter : ImageStreamCompleter
    {
        public void Emit(ImageInfo image) => SetImage(image);
    }

    private sealed class TestWarmUp : ShaderWarmUp
    {
        public TaskCompletionSource Completion { get; } = new();
        public bool Started { get; private set; }
        public bool Finished { get; private set; }
        public override Size Size => new(10.2, 6.1);

        protected override async Task WarmUpOnCanvas(Canvas canvas)
        {
            Started = true;
            canvas.DrawPaint(new Paint { Color = new Color(0xFFFF0000) });
            await Completion.Task;
            // A microtask after the execution continuation makes trailing disposal observable.
            EventLoop.ScheduleMicrotask(() => Finished = true);
        }
    }

    private sealed class DefaultSizeWarmUp : ShaderWarmUp
    {
        protected override Task WarmUpOnCanvas(Canvas canvas) => Task.CompletedTask;
    }

    private sealed class DecoderBinding : PaintingBinding
    {
        public int DecodeCalls { get; private set; }
        public override Task<Codec> InstantiateImageCodecWithSize(
            ImmutableBuffer buffer, TargetImageSizeCallback? getTargetSize = null)
        {
            DecodeCalls++;
            return base.InstantiateImageCodecWithSize(buffer, getTargetSize);
        }
    }
}
