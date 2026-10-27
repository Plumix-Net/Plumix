// Dart parity source: flutter/packages/flutter/lib/src/painting/image_cache.dart
// Mirrors flutter/packages/flutter/test/painting/image_cache_test.dart with the mocks of
// mocks_for_image_cache.dart. Adaptations: ImageInfo is sealed, so TestImageInfo's `value` travels
// in the debug label; the engine's per-image handle stack traces become the test image's disposal
// count plus the completer's disposal; Dart's `await null` / `await extractOneFrame` drain the
// microtask queue; `endOfFrame` is a pumped frame.
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImageCacheDartParityTests : IDisposable
{
    private static ImageCache ImageCache => Plumix.Rendering.ImageCache.Shared;

    public ImageCacheDartParityTests()
    {
        ImageCache.Clear();
        ImageCache.ClearLiveImages();
        ImageCache.MaximumSize = 1000;
        ImageCache.MaximumSizeBytes = 10485760;
    }

    // Dart's tearDown.
    public void Dispose()
    {
        ImageCache.Clear();
        ImageCache.ClearLiveImages();
        ImageCache.MaximumSize = 1000;
        ImageCache.MaximumSizeBytes = 10485760;
    }

    // Flutter: "maintains cache size"
    [Fact]
    public void MaintainsCacheSize()
    {
        ImageCache.MaximumSize = 3;

        ImageInfo a = ExtractOneFrame(new TestImageProvider(1, 1, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(a));
        ImageInfo b = ExtractOneFrame(new TestImageProvider(1, 2, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(b));
        ImageInfo c = ExtractOneFrame(new TestImageProvider(1, 3, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(c));
        ImageInfo d = ExtractOneFrame(new TestImageProvider(1, 4, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(d));
        ImageInfo e = ExtractOneFrame(new TestImageProvider(1, 5, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(e));
        ImageInfo f = ExtractOneFrame(new TestImageProvider(1, 6, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(f));

        // TestImageInfo's == : same value, a clone of the same image, same scale and label.
        Assert.True(f.IsCloneOf(a));

        // cache still only has one entry in it: 1(1)

        ImageInfo g = ExtractOneFrame(new TestImageProvider(2, 7, image: CreateTestImage()));
        Assert.Equal(7, ValueOf(g));

        // cache has two entries in it: 1(1), 2(7)

        ImageInfo h = ExtractOneFrame(new TestImageProvider(1, 8, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(h));

        // cache still has two entries in it: 2(7), 1(1)

        ImageInfo i = ExtractOneFrame(new TestImageProvider(3, 9, image: CreateTestImage()));
        Assert.Equal(9, ValueOf(i));

        // cache has three entries in it: 2(7), 1(1), 3(9)

        ImageInfo j = ExtractOneFrame(new TestImageProvider(1, 10, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(j));

        // cache still has three entries in it: 2(7), 3(9), 1(1)

        ImageInfo k = ExtractOneFrame(new TestImageProvider(4, 11, image: CreateTestImage()));
        Assert.Equal(11, ValueOf(k));

        // cache has three entries: 3(9), 1(1), 4(11)

        ImageInfo l = ExtractOneFrame(new TestImageProvider(1, 12, image: CreateTestImage()));
        Assert.Equal(1, ValueOf(l));

        // cache has three entries: 3(9), 4(11), 1(1)

        ImageInfo m = ExtractOneFrame(new TestImageProvider(2, 13, image: CreateTestImage()));
        Assert.Equal(13, ValueOf(m));

        // cache has three entries: 4(11), 1(1), 2(13)

        ImageInfo n = ExtractOneFrame(new TestImageProvider(3, 14, image: CreateTestImage()));
        Assert.Equal(14, ValueOf(n));

        // cache has three entries: 1(1), 2(13), 3(14)

        ImageInfo o = ExtractOneFrame(new TestImageProvider(4, 15, image: CreateTestImage()));
        Assert.Equal(15, ValueOf(o));

        // cache has three entries: 2(13), 3(14), 4(15)

        ImageInfo p = ExtractOneFrame(new TestImageProvider(1, 16, image: CreateTestImage()));
        Assert.Equal(16, ValueOf(p));

        // cache has three entries: 3(14), 4(15), 1(16)
    }

    // Flutter: "clear removes all images and resets cache size"
    [Fact]
    public void ClearRemovesAllImagesAndResetsCacheSize()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        Assert.Equal(0, ImageCache.CurrentSize);
        Assert.Equal(0, ImageCache.CurrentSizeBytes);

        ExtractOneFrame(new TestImageProvider(1, 1, image: testImage));
        ExtractOneFrame(new TestImageProvider(2, 2, image: testImage));

        Assert.Equal(2, ImageCache.CurrentSize);
        Assert.Equal(256 * 2, ImageCache.CurrentSizeBytes);

        ImageCache.Clear();

        Assert.Equal(0, ImageCache.CurrentSize);
        Assert.Equal(0, ImageCache.CurrentSizeBytes);
    }

    // Flutter: "evicts individual images"
    [Fact]
    public void EvictsIndividualImages()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);
        ExtractOneFrame(new TestImageProvider(1, 1, image: testImage));
        ExtractOneFrame(new TestImageProvider(2, 2, image: testImage));

        Assert.Equal(2, ImageCache.CurrentSize);
        Assert.Equal(256 * 2, ImageCache.CurrentSizeBytes);
        Assert.True(ImageCache.Evict(1));
        Assert.Equal(1, ImageCache.CurrentSize);
        Assert.Equal(256, ImageCache.CurrentSizeBytes);
    }

    // Flutter: "Do not cache large images"
    [Fact]
    public void DoNotCacheLargeImages()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        ImageCache.MaximumSizeBytes = 1;
        ExtractOneFrame(new TestImageProvider(1, 1, image: testImage));
        Assert.Equal(0, ImageCache.CurrentSize);
        Assert.Equal(0, ImageCache.CurrentSizeBytes);
        Assert.Equal(1, ImageCache.MaximumSizeBytes);
    }

    // Flutter: "Returns null if an error is caught resolving an image"
    [Fact]
    public void ReturnsNullIfAnErrorIsCaughtResolvingAnImage()
    {
        var errorImage = new ErrorImageProvider();
        Assert.ThrowsAny<Exception>(() => ImageCache.PutIfAbsent(errorImage, errorImage.LoadThrowing));
        bool caughtError = false;
        ImageStreamCompleter? result = ImageCache.PutIfAbsent(
            errorImage,
            errorImage.LoadThrowing,
            onError: (_, _) => caughtError = true);
        Assert.Null(result);
        Assert.True(caughtError);
    }

    // Flutter: "already pending image is returned when it is put into the cache again"
    [Fact]
    public void AlreadyPendingImageIsReturnedWhenItIsPutIntoTheCacheAgain()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();
        var completer2 = new TestImageStreamCompleter();

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);
        ImageStreamCompleter? resultingCompleter2 = ImageCache.PutIfAbsent(testImage, () => completer2);

        Assert.Same(completer1, resultingCompleter1);
        Assert.Same(completer1, resultingCompleter2);
    }

    // Flutter: "pending image is removed when cache is cleared"
    [Fact]
    public void PendingImageIsRemovedWhenCacheIsCleared()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();
        var completer2 = new TestImageStreamCompleter();

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);

        // Make the image seem live.
        var listener = new ImageStreamListener((_, _) => { });
        completer1.AddListener(listener);

        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        ImageCache.Clear();
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        ImageCache.ClearLiveImages();
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.False(ImageCache.StatusForKey(testImage).Live);

        ImageStreamCompleter? resultingCompleter2 = ImageCache.PutIfAbsent(testImage, () => completer2);

        Assert.Same(completer1, resultingCompleter1);
        Assert.Same(completer2, resultingCompleter2);
    }

    // Flutter: "pending image is removed when image is evicted"
    [Fact]
    public void PendingImageIsRemovedWhenImageIsEvicted()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();
        var completer2 = new TestImageStreamCompleter();

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);

        ImageCache.Evict(testImage);

        ImageStreamCompleter? resultingCompleter2 = ImageCache.PutIfAbsent(testImage, () => completer2);

        Assert.Same(completer1, resultingCompleter1);
        Assert.Same(completer2, resultingCompleter2);
    }

    // Flutter: "failed image can successfully be removed from the cache's pending images"
    [Fact]
    public void FailedImageCanSuccessfullyBeRemovedFromTheCachesPendingImages()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);
        bool? evictionResult = null;

        new FailingTestImageProvider(1, 1, image: testImage)
            .Resolve(ImageConfiguration.Empty)
            .AddListener(new ImageStreamListener(
                (_, _) => Assert.Fail("Image should not complete successfully"),
                OnError: (_, _) => evictionResult = ImageCache.Evict(1)));
        // yield an event turn so that async work can complete.
        Assert.True(EventLoopPump.SpinUntil(() => evictionResult is not null));

        Assert.True(evictionResult);
    }

    // Flutter: "containsKey - pending"
    [Fact]
    public void ContainsKeyPending()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);

        Assert.Same(completer1, resultingCompleter1);
        Assert.True(ImageCache.ContainsKey(testImage));
    }

    // Flutter: "containsKey - completed"
    [Fact]
    public void ContainsKeyCompleted()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);

        // Mark as complete
        completer1.TestSetImage(testImage);

        Assert.Same(completer1, resultingCompleter1);
        Assert.True(ImageCache.ContainsKey(testImage));
    }

    // Flutter: "putIfAbsent updates LRU properties of a live image"
    [Fact]
    public void PutIfAbsentUpdatesLruPropertiesOfALiveImage()
    {
        ImageCache.MaximumSize = 1;
        TestImage testImage = CreateTestImage(width: 8, height: 8);
        TestImage testImage2 = CreateTestImage(width: 10, height: 10);

        var completer1 = new TestImageStreamCompleter();
        completer1.TestSetImage(testImage);
        var completer2 = new TestImageStreamCompleter();
        completer2.TestSetImage(testImage2);

        completer1.AddListener(new ImageStreamListener((_, _) => { }));

        ImageStreamCompleter? resultingCompleter1 = ImageCache.PutIfAbsent(testImage, () => completer1);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage2).Untracked);
        ImageStreamCompleter? resultingCompleter2 = ImageCache.PutIfAbsent(testImage2, () => completer2);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive); // evicted
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage2).Pending);
        Assert.True(ImageCache.StatusForKey(testImage2).KeepAlive); // took the LRU spot.
        Assert.False(ImageCache.StatusForKey(testImage2).Live); // no listeners

        Assert.Same(completer1, resultingCompleter1);
        Assert.Same(completer2, resultingCompleter2);
    }

    // Flutter: "Live image cache avoids leaks of unlistened streams"
    [Fact]
    public void LiveImageCacheAvoidsLeaksOfUnlistenedStreams()
    {
        ImageCache.MaximumSize = 3;

        for (int index = 1; index <= 6; index += 1)
        {
            new TestImageProvider(index, index, image: CreateTestImage()).Resolve(ImageConfiguration.Empty);
        }

        // wait an event loop to let image resolution process.
        Assert.True(EventLoopPump.SpinUntil(() => ImageCache.PendingImageCount == 0));

        Assert.Equal(3, ImageCache.CurrentSize);
        Assert.Equal(0, ImageCache.LiveImageCount);
    }

    // Flutter: "Disabled image cache does not leak live images"
    [Fact]
    public void DisabledImageCacheDoesNotLeakLiveImages()
    {
        ImageCache.MaximumSize = 0;

        for (int index = 1; index <= 6; index += 1)
        {
            new TestImageProvider(index, index, image: CreateTestImage()).Resolve(ImageConfiguration.Empty);
        }

        // wait an event loop to let image resolution process.
        Assert.True(EventLoopPump.SpinUntil(() => ImageCache.LiveImageCount == 0));

        Assert.Equal(0, ImageCache.CurrentSize);
        Assert.Equal(0, ImageCache.LiveImageCount);
    }

    // Flutter: "Clearing image cache does not leak live images"
    [Fact]
    public void ClearingImageCacheDoesNotLeakLiveImages()
    {
        ImageCache.MaximumSize = 1;

        TestImage testImage1 = CreateTestImage(width: 8, height: 8);
        TestImage testImage2 = CreateTestImage(width: 10, height: 10);

        var completer1 = new TestImageStreamCompleter();
        var completer2 = new TestImageStreamCompleter();
        completer2.TestSetImage(testImage2);

        ImageCache.PutIfAbsent(testImage1, () => completer1);
        Assert.True(ImageCache.StatusForKey(testImage1).Pending);
        Assert.True(ImageCache.StatusForKey(testImage1).Live);

        ImageCache.Clear();
        Assert.False(ImageCache.StatusForKey(testImage1).Pending);
        Assert.False(ImageCache.StatusForKey(testImage1).Live);

        completer1.TestSetImage(testImage1);
        Assert.False(ImageCache.StatusForKey(testImage1).KeepAlive);
        Assert.False(ImageCache.StatusForKey(testImage1).Live);

        ImageCache.PutIfAbsent(testImage2, () => completer2);
        Assert.False(ImageCache.StatusForKey(testImage1).Tracked); // evicted
        Assert.True(ImageCache.StatusForKey(testImage2).Tracked);
    }

    // Flutter: "Evicting a pending image clears the live image by default"
    [Fact]
    public void EvictingAPendingImageClearsTheLiveImageByDefault()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Evict(testImage);
        Assert.True(ImageCache.StatusForKey(testImage).Untracked);
    }

    // Flutter: "Evicting a pending image does clear the live image when includeLive is false and only
    // cache listening"
    [Fact]
    public void EvictingAPendingImageDoesClearTheLiveImageWhenIncludeLiveIsFalseAndOnlyCacheListening()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Evict(testImage, includeLive: false);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.False(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
    }

    // Flutter: "Evicting a pending image does clear the live image when includeLive is false and some
    // other listener"
    [Fact]
    public void EvictingAPendingImageDoesClearTheLiveImageWhenIncludeLiveIsFalseAndSomeOtherListener()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);

        completer1.AddListener(new ImageStreamListener((_, _) => { }));
        ImageCache.Evict(testImage, includeLive: false);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
    }

    // Flutter: "Evicting a completed image does clear the live image by default"
    [Fact]
    public void EvictingACompletedImageDoesClearTheLiveImageByDefault()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();
        completer1.TestSetImage(testImage);
        completer1.AddListener(new ImageStreamListener((_, _) => { }));

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Evict(testImage);
        Assert.True(ImageCache.StatusForKey(testImage).Untracked);
    }

    // Flutter: "Evicting a completed image does not clear the live image when includeLive is set to
    // false"
    [Fact]
    public void EvictingACompletedImageDoesNotClearTheLiveImageWhenIncludeLiveIsSetToFalse()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();
        completer1.TestSetImage(testImage);
        completer1.AddListener(new ImageStreamListener((_, _) => { }));

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Evict(testImage, includeLive: false);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
    }

    // Flutter: "Clearing liveImages removes callbacks"
    [Fact]
    public void ClearingLiveImagesRemovesCallbacks()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var listener = new ImageStreamListener((_, _) => { });

        var completer1 = new TestImageStreamCompleter();
        completer1.TestSetImage(testImage);
        completer1.AddListener(listener);

        var completer2 = new TestImageStreamCompleter();
        completer2.TestSetImage(testImage);
        completer2.AddListener(listener);

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Clear();
        ImageCache.ClearLiveImages();
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.False(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.PutIfAbsent(testImage, () => completer2);
        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);

        completer1.RemoveListener(listener);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);
    }

    // Flutter: "Live image gets size updated"
    [Fact]
    public void LiveImageGetsSizeUpdated()
    {
        // Add an image to the cache in pending state
        // Complete it once it is in there as live
        // Evict it but leave the live one.
        // Add it again.
        // If the live image did not track the size properly, the last line of
        // this test will fail.

        TestImage testImage = CreateTestImage(width: 8, height: 8);
        const int testImageSize = 8 * 8 * 4;

        var listener = new ImageStreamListener((_, _) => { });

        var completer1 = new TestImageStreamCompleter();
        completer1.AddListener(listener);

        ImageCache.PutIfAbsent(testImage, () => completer1);
        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
        Assert.Equal(0, ImageCache.CurrentSizeBytes);

        completer1.TestSetImage(testImage);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);
        Assert.Equal(testImageSize, ImageCache.CurrentSizeBytes);

        ImageCache.Evict(testImage, includeLive: false);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
        Assert.Equal(0, ImageCache.CurrentSizeBytes);

        ImageCache.PutIfAbsent(testImage, () => completer1);

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.True(ImageCache.StatusForKey(testImage).KeepAlive);
        Assert.Equal(testImageSize, ImageCache.CurrentSizeBytes);
    }

    // Flutter: "Image is obtained and disposed of properly for cache"
    [Fact]
    public void ImageIsObtainedAndDisposedOfProperlyForCache()
    {
        using var tester = new FrameworkDartTester();
        const int key = 1;
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        ImageInfo? imageInfo = null;
        var listener = new ImageStreamListener((info, _) => imageInfo = info);

        var completer = new TestImageStreamCompleter();

        completer.AddListener(listener);
        ImageCache.PutIfAbsent(key, () => completer);

        // This should cause keepAlive to be set to true.
        completer.TestSetImage(testImage);
        Assert.NotNull(imageInfo);

        completer.RemoveListener(listener);

        // Force us to the end of the frame.
        SchedulerBinding.Instance.ScheduleFrame();
        tester.Pump();

        // The cache's keep-alive handle holds the completer, and with it its image.
        Assert.False(completer.IsDisposed);
        Assert.Equal(0, testImage.DisposeCount);

        Assert.True(ImageCache.Evict(key));

        // Force us to the end of the frame.
        SchedulerBinding.Instance.ScheduleFrame();
        tester.Pump();

        // -1 _CachedImage
        // -1 ImageStreamCompleter
        Assert.True(completer.IsDisposed);
        Assert.Equal(0, testImage.DisposeCount);

        imageInfo!.Dispose();
        Assert.Equal(1, testImage.DisposeCount);
    }

    // Flutter: "Image is obtained and disposed of properly for cache when listener is still active"
    [Fact]
    public void ImageIsObtainedAndDisposedOfProperlyForCacheWhenListenerIsStillActive()
    {
        using var tester = new FrameworkDartTester();
        const int key = 1;
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        ImageInfo? imageInfo = null;
        var listener = new ImageStreamListener((info, _) => imageInfo = info);

        var completer = new TestImageStreamCompleter();

        completer.AddListener(listener);
        ImageCache.PutIfAbsent(key, () => completer);

        // This should cause keepAlive to be set to true.
        completer.TestSetImage(testImage);
        Assert.NotNull(imageInfo);

        Assert.True(ImageCache.Evict(key));

        // Force us to the end of the frame.
        SchedulerBinding.Instance.ScheduleFrame();
        tester.Pump();

        // Live image still around since there's still a listener, and the listener
        // should be holding a handle.
        Assert.False(completer.IsDisposed);
        completer.RemoveListener(listener);

        Assert.True(completer.IsDisposed);
        Assert.Equal(0, testImage.DisposeCount);
        imageInfo!.Dispose();
        Assert.Equal(1, testImage.DisposeCount);
    }

    // Flutter: "clear does not leave pending images stuck"
    [Fact]
    public void ClearDoesNotLeavePendingImagesStuck()
    {
        TestImage testImage = CreateTestImage(width: 8, height: 8);

        var completer1 = new TestImageStreamCompleter();

        ImageCache.PutIfAbsent(testImage, () => completer1);

        Assert.True(ImageCache.StatusForKey(testImage).Pending);
        Assert.True(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);

        ImageCache.Clear();

        // No one else is listening to the completer. It should not be considered
        // live anymore.

        Assert.False(ImageCache.StatusForKey(testImage).Pending);
        Assert.False(ImageCache.StatusForKey(testImage).Live);
        Assert.False(ImageCache.StatusForKey(testImage).KeepAlive);
    }

    // image_cache.dart `ImageCacheStatus.toString`.
    [Fact]
    public void ImageCacheStatusToStringMatchesDart()
    {
        Assert.Equal(
            "ImageCacheStatus(pending: true, live: true, keepAlive: false)",
            new ImageCacheStatus(Pending: true, Live: true).ToString());
        Assert.True(ImageCache.StatusForKey(new object()).Untracked);
    }

    // flutter_test's createTestImage: a width x height image.
    private static TestImage CreateTestImage(int width = 1, int height = 1) => new(new Size(width, height));

    // TestImageInfo.value.
    private static int ValueOf(ImageInfo info) =>
        int.Parse(info.DebugLabel!, System.Globalization.CultureInfo.InvariantCulture);

    // mocks_for_image_cache.dart `extractOneFrame`.
    private static ImageInfo ExtractOneFrame(ImageProvider provider)
    {
        ImageStream stream = provider.Resolve(ImageConfiguration.Empty);
        ImageInfo? result = null;
        ImageStreamListener? listener = null;
        listener = new ImageStreamListener((image, _) =>
        {
            result = image;
            stream.RemoveListener(listener!);
        });
        stream.AddListener(listener);
        Assert.True(EventLoopPump.SpinUntil(() => result is not null));
        return result!;
    }

    private sealed class TestImage(Size size) : IImage, IDisposable
    {
        public Size Size { get; } = size;

        public int DisposeCount { get; private set; }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }

        public void Dispose() => DisposeCount++;
    }

    // mocks_for_image_cache.dart `TestImageProvider`.
    private class TestImageProvider(int key, int imageValue, TestImage image) : ImageProvider<int>
    {
        protected TestImage Image { get; } = image;

        public override ValueTask<int> ObtainKey(ImageConfiguration configuration) => ValueTask.FromResult(key);

        protected override ImageStreamCompleter LoadImage(int key, ImageDecoderCallback decode)
        {
            // TestImageInfo(imageValue, image: image.clone()).
            return new OneFrameImageStreamCompleter(Task.FromResult(new ImageInfo(
                Image,
                debugLabel: imageValue.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        }

        public override string ToString() => $"TestImageProvider({key}, {imageValue})";
    }

    // mocks_for_image_cache.dart `FailingTestImageProvider`.
    private sealed class FailingTestImageProvider(int key, int imageValue, TestImage image)
        : TestImageProvider(key, imageValue, image)
    {
        protected override ImageStreamCompleter LoadImage(int key, ImageDecoderCallback decode)
        {
            return new OneFrameImageStreamCompleter(
                Task.FromException<ImageInfo>(new InvalidOperationException("loading failed!")));
        }
    }

    // mocks_for_image_cache.dart `ErrorImageProvider`: its loadBuffer throws.
    private sealed class ErrorImageProvider : ImageProvider<ErrorImageProvider>
    {
        public ImageStreamCompleter LoadThrowing() => throw new UnreachableException();

        public override ValueTask<ErrorImageProvider> ObtainKey(ImageConfiguration configuration) =>
            ValueTask.FromResult(this);

        protected override ImageStreamCompleter LoadImage(ErrorImageProvider key, ImageDecoderCallback decode) =>
            throw new UnreachableException();
    }

    // mocks_for_image_cache.dart `TestImageStreamCompleter`.
    private sealed class TestImageStreamCompleter : ImageStreamCompleter
    {
        public void TestSetImage(TestImage image) => SetImage(new ImageInfo(image));
    }
}
