using Plumix.Developer;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/image_cache.dart

/// <summary>
/// Class for caching images.
/// </summary>
/// <remarks>
/// Dart's <c>ImageCache</c>. Its three maps are Dart's insertion-ordered <c>LinkedHashMap</c>s
/// (<see cref="OrderedDictionary{TKey,TValue}"/>): the first key of <c>_cache</c> is the least
/// recently used image. Like every framework object it is used on the framework thread only.
/// </remarks>
public class ImageCache
{
    private const int KDefaultSize = 1000;
    private const long KDefaultSizeBytes = 100L << 20; // 100 MiB

    private readonly OrderedDictionary<object, PendingImage> _pendingImages = [];
    private readonly OrderedDictionary<object, CachedImage> _cache = [];

    /// <summary>
    /// ImageStreamCompleters with at least one listener. These images may or may not fit into the
    /// <c>_pendingImages</c> or <c>_cache</c> objects.
    /// </summary>
    /// <remarks>Unlike those maps, this map is not limited by the cache size.</remarks>
    private readonly OrderedDictionary<object, LiveImage> _liveImages = [];

    private int _maximumSize = KDefaultSize;
    private long _maximumSizeBytes = KDefaultSizeBytes;
    private long _currentSizeBytes;

    /// <summary>The singleton held by the <see cref="Painting.PaintingBinding"/>.</summary>
    public static ImageCache Shared => Painting.PaintingBinding.Instance.ImageCache;

    /// <summary>Maximum number of entries to store in the cache.</summary>
    public int MaximumSize
    {
        get => _maximumSize;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == MaximumSize)
            {
                return;
            }

            TimelineTask? debugTimelineTask = null;
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask = new TimelineTask();
                debugTimelineTask.Start(
                    "ImageCache.setMaximumSize",
                    arguments: new Dictionary<string, object?> { ["value"] = value });
            }

            _maximumSize = value;
            if (MaximumSize == 0)
            {
                Clear();
            }
            else
            {
                CheckCacheSize(debugTimelineTask);
            }

            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish();
            }
        }
    }

    /// <summary>The current number of cached entries.</summary>
    public int CurrentSize => _cache.Count;

    /// <summary>Maximum size of entries to store in the cache in bytes.</summary>
    public long MaximumSizeBytes
    {
        get => _maximumSizeBytes;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == _maximumSizeBytes)
            {
                return;
            }

            TimelineTask? debugTimelineTask = null;
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask = new TimelineTask();
                debugTimelineTask.Start(
                    "ImageCache.setMaximumSizeBytes",
                    arguments: new Dictionary<string, object?> { ["value"] = value });
            }

            _maximumSizeBytes = value;
            if (_maximumSizeBytes == 0)
            {
                Clear();
            }
            else
            {
                CheckCacheSize(debugTimelineTask);
            }

            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish();
            }
        }
    }

    /// <summary>The current size of cached entries in bytes.</summary>
    public long CurrentSizeBytes => _currentSizeBytes;

    /// <summary>
    /// Evicts all pending and keepAlive entries from the cache.
    /// </summary>
    /// <remarks>Live images are not affected; <see cref="ClearLiveImages"/> releases them.</remarks>
    public virtual void Clear()
    {
        if (!Constants.KReleaseMode)
        {
            Timeline.InstantSync(
                "ImageCache.clear",
                arguments: new Dictionary<string, object?>
                {
                    ["pendingImages"] = _pendingImages.Count,
                    ["keepAliveImages"] = _cache.Count,
                    ["liveImages"] = _liveImages.Count,
                    ["currentSizeInBytes"] = _currentSizeBytes,
                });
        }

        foreach (CachedImage image in _cache.Values)
        {
            image.Dispose();
        }

        _cache.Clear();
        foreach (PendingImage pendingImage in _pendingImages.Values)
        {
            pendingImage.RemoveListener();
        }

        _pendingImages.Clear();
        _currentSizeBytes = 0;
    }

    /// <summary>
    /// Evicts a single entry from the cache, returning true if successful.
    /// </summary>
    /// <remarks>
    /// The <paramref name="key"/> must be equal to an object used to cache an image in
    /// <see cref="PutIfAbsent"/>. When <paramref name="includeLive"/> is true the image is also
    /// removed from the live images.
    /// </remarks>
    public bool Evict(object key, bool includeLive = true)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (includeLive)
        {
            // Remove from live images - the cache will not be able to mark
            // it as complete, and it might be getting evicted because it
            // will never complete, e.g. it was loaded in a FakeAsync zone.
            // In such a case, we need to make sure subsequent calls to
            // putIfAbsent don't return this image that may never complete.
            _liveImages.Remove(key, out LiveImage? image);
            image?.Dispose();
        }

        if (_pendingImages.Remove(key, out PendingImage? pendingImage))
        {
            if (!Constants.KReleaseMode)
            {
                Timeline.InstantSync(
                    "ImageCache.evict",
                    arguments: new Dictionary<string, object?> { ["type"] = "pending" });
            }

            pendingImage.RemoveListener();
            return true;
        }

        if (_cache.Remove(key, out CachedImage? cachedImage))
        {
            if (!Constants.KReleaseMode)
            {
                Timeline.InstantSync(
                    "ImageCache.evict",
                    arguments: new Dictionary<string, object?>
                    {
                        ["type"] = "keepAlive",
                        ["sizeInBytes"] = cachedImage.SizeBytes,
                    });
            }

            _currentSizeBytes -= cachedImage.SizeBytes!.Value;
            cachedImage.Dispose();
            return true;
        }

        if (!Constants.KReleaseMode)
        {
            Timeline.InstantSync(
                "ImageCache.evict",
                arguments: new Dictionary<string, object?> { ["type"] = "miss" });
        }

        return false;
    }

    /// <summary>
    /// Updates the least recently used image cache with this image, if it is less than the
    /// <see cref="MaximumSizeBytes"/> of this cache.
    /// </summary>
    /// <remarks>
    /// Resizes the cache as appropriate to maintain the constraints of <see cref="MaximumSize"/> and
    /// <see cref="MaximumSizeBytes"/>.
    /// </remarks>
    private void Touch(object key, CachedImage image, TimelineTask? timelineTask)
    {
        if (image.SizeBytes is { } sizeBytes && sizeBytes <= MaximumSizeBytes && MaximumSize > 0)
        {
            _currentSizeBytes += sizeBytes;
            _cache[key] = image;
            CheckCacheSize(timelineTask);
        }
        else
        {
            image.Dispose();
        }
    }

    private void TrackLiveImage(object key, ImageStreamCompleter completer, long? sizeBytes)
    {
        // Avoid adding unnecessary callbacks to the completer.
        if (!_liveImages.TryGetValue(key, out LiveImage? liveImage))
        {
            // Even if no callers to ImageProvider.resolve have listened to the stream,
            // the cache is listening to the stream and will remove itself once the
            // image completes to move it from pending to keepAlive.
            // Even if the cache size is 0, we still add this tracker, which will add
            // a keep alive handle to the stream.
            liveImage = new LiveImage(completer, () => _liveImages.Remove(key));
            _liveImages[key] = liveImage;
        }

        liveImage.SizeBytes ??= sizeBytes;
    }

    /// <summary>
    /// Returns the previously cached <see cref="ImageStream"/> for the given key, if available; if
    /// not, calls the given callback to obtain it first. In either case, the key is moved to the
    /// "most recently used" position.
    /// </summary>
    /// <remarks>
    /// In the event that the loader throws an exception, it will be caught only if
    /// <paramref name="onError"/> is also provided. When an exception is caught resolving an image,
    /// no completers are cached and null is returned instead of a new completer.
    /// </remarks>
    public ImageStreamCompleter? PutIfAbsent(
        object key,
        Func<ImageStreamCompleter> loader,
        ImageErrorListener? onError = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(loader);
        TimelineTask? debugTimelineTask = null;
        if (!Constants.KReleaseMode)
        {
            debugTimelineTask = new TimelineTask();
            debugTimelineTask.Start(
                "ImageCache.putIfAbsent",
                arguments: new Dictionary<string, object?> { ["key"] = key.ToString() });
        }

        ImageStreamCompleter? result = _pendingImages.TryGetValue(key, out PendingImage? pending)
            ? pending.Completer
            : null;
        // Nothing needs to be done because the image hasn't loaded yet.
        if (result is not null)
        {
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "pending" });
            }

            return result;
        }

        // Remove the provider from the list so that we can move it to the
        // recently used position below.
        // Don't use _touch here, which would trigger a check on cache size that is
        // not needed since this is just moving an existing cache entry to the head.
        if (_cache.Remove(key, out CachedImage? image))
        {
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "keepAlive" });
            }

            // The image might have been keptAlive but had no listeners (so not live).
            // Make sure the cache starts tracking it as live again.
            TrackLiveImage(key, image.Completer, image.SizeBytes);
            _cache[key] = image;
            return image.Completer;
        }

        if (_liveImages.TryGetValue(key, out LiveImage? liveImage))
        {
            Touch(key, new CachedImage(liveImage.Completer, sizeBytes: liveImage.SizeBytes), debugTimelineTask);
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "keepAlive" });
            }

            return liveImage.Completer;
        }

        try
        {
            result = loader();
            TrackLiveImage(key, result, null);
        }
        catch (Exception error)
        {
            var stackTrace = new System.Diagnostics.StackTrace(error, true);
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?>
                {
                    ["result"] = "error",
                    ["error"] = error.ToString(),
                    ["stackTrace"] = stackTrace.ToString(),
                });
            }

            if (onError is not null)
            {
                onError(error, stackTrace);
                return null;
            }

            throw;
        }

        if (!Constants.KReleaseMode)
        {
            debugTimelineTask!.Start("listener");
        }

        // A multi-frame provider may call the listener more than once. We need do make
        // sure that some cleanup works won't run multiple times, such as finishing the
        // tracing task or removing the listeners
        bool listenedOnce = false;

        // We shouldn't use the _pendingImages map if the cache is disabled, but we
        // will have to listen to the image at least once so we don't leak it in
        // the live image tracking.
        bool trackPendingImage = MaximumSize > 0 && MaximumSizeBytes > 0;
        PendingImage? pendingImage = null;
        ImageStreamCompleter completer = result;
        void Listener(ImageInfo? info, bool syncCall)
        {
            long? sizeBytes = null;
            if (info is not null)
            {
                sizeBytes = info.SizeBytes;
                info.Dispose();
            }

            var cachedImage = new CachedImage(completer, sizeBytes: sizeBytes);

            TrackLiveImage(key, completer, sizeBytes);

            // Only touch if the cache was enabled when resolve was initially called.
            if (trackPendingImage)
            {
                Touch(key, cachedImage, debugTimelineTask);
            }
            else
            {
                cachedImage.Dispose();
            }

            _pendingImages.Remove(key);
            if (!listenedOnce)
            {
                pendingImage!.RemoveListener();
            }

            if (!Constants.KReleaseMode && !listenedOnce)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?>
                {
                    ["syncCall"] = syncCall ? "true" : "false",
                    ["sizeInBytes"] = sizeBytes,
                });
                debugTimelineTask.Finish(new Dictionary<string, object?>
                {
                    ["currentSizeBytes"] = CurrentSizeBytes,
                    ["currentSize"] = CurrentSize,
                });
            }

            listenedOnce = true;
        }

        var streamListener = new ImageStreamListener(Listener);
        pendingImage = new PendingImage(completer, streamListener);
        if (trackPendingImage)
        {
            _pendingImages[key] = pendingImage;
        }

        // Listener is removed in [_PendingImage.removeListener].
        completer.AddListener(streamListener);
        return completer;
    }

    /// <summary>
    /// The <see cref="ImageCacheStatus"/> information for the given <paramref name="key"/>.
    /// </summary>
    public ImageCacheStatus StatusForKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new ImageCacheStatus(
            Pending: _pendingImages.ContainsKey(key),
            KeepAlive: _cache.ContainsKey(key),
            Live: _liveImages.ContainsKey(key));
    }

    /// <summary>
    /// Returns whether this <paramref name="key"/> has been previously added by <see cref="PutIfAbsent"/>.
    /// </summary>
    public bool ContainsKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _pendingImages.ContainsKey(key) || _cache.ContainsKey(key);
    }

    /// <summary>The number of live images being held by the <see cref="ImageCache"/>.</summary>
    public int LiveImageCount => _liveImages.Count;

    /// <summary>The number of images being tracked as pending in the <see cref="ImageCache"/>.</summary>
    public int PendingImageCount => _pendingImages.Count;

    /// <summary>
    /// Clears any live references to images in this cache.
    /// </summary>
    /// <remarks>
    /// An image is considered live if its <see cref="ImageStreamCompleter"/> has never hit zero
    /// listeners after adding at least one listener. The
    /// <see cref="ImageStreamCompleter.AddOnLastListenerRemovedCallback"/> is used to determine when
    /// this has happened.
    /// </remarks>
    public virtual void ClearLiveImages()
    {
        foreach (LiveImage image in _liveImages.Values)
        {
            image.Dispose();
        }

        _liveImages.Clear();
    }

    // Remove images from the cache until both the length and bytes are below
    // maximum, or the cache is empty.
    private void CheckCacheSize(TimelineTask? timelineTask)
    {
        var finishArgs = new Dictionary<string, object?>();
        List<string>? evictedKeys = null;
        if (!Constants.KReleaseMode)
        {
            timelineTask!.Start("checkCacheSize");
            evictedKeys = [];
            finishArgs["evictedKeys"] = evictedKeys;
            finishArgs["currentSize"] = CurrentSize;
            finishArgs["currentSizeBytes"] = CurrentSizeBytes;
        }

        while (_currentSizeBytes > _maximumSizeBytes || _cache.Count > _maximumSize)
        {
            object key = _cache.GetAt(0).Key;
            CachedImage image = _cache[key];
            _currentSizeBytes -= image.SizeBytes!.Value;
            image.Dispose();
            _cache.Remove(key);
            if (!Constants.KReleaseMode)
            {
                evictedKeys!.Add(key.ToString() ?? "null");
            }
        }

        if (!Constants.KReleaseMode)
        {
            finishArgs["endSize"] = CurrentSize;
            finishArgs["endSizeBytes"] = CurrentSizeBytes;
            timelineTask!.Finish(finishArgs);
        }

        if (Constants.KDebugMode
            && (_currentSizeBytes < 0 || _cache.Count > MaximumSize || _currentSizeBytes > MaximumSizeBytes))
        {
            throw new AssertionError("The image cache is over its limits after trimming.");
        }
    }

    private abstract class CachedImageBase
    {
        protected CachedImageBase(ImageStreamCompleter completer, long? sizeBytes = null)
        {
            Completer = completer;
            SizeBytes = sizeBytes;
            Handle = completer.KeepAlive();
            if (Constants.KDebugMode)
            {
                FoundationDebug.DebugMaybeDispatchCreated("painting", "_CachedImageBase", this);
            }
        }

        public ImageStreamCompleter Completer { get; }

        public long? SizeBytes { get; set; }

        public ImageStreamCompleterHandle? Handle { get; private set; }

        public virtual void Dispose()
        {
            if (Constants.KDebugMode)
            {
                if (Handle is null)
                {
                    throw new AssertionError("A cached image was disposed after its handle was released.");
                }

                FoundationDebug.DebugMaybeDispatchDisposed(this);
            }

            // Give any interested parties a chance to listen to the stream before we
            // potentially dispose it.
            SchedulerBinding.Instance.AddPostFrameCallback(
                _ =>
                {
                    if (Constants.KDebugMode && Handle is null)
                    {
                        throw new AssertionError("A cached image handle was released twice.");
                    }

                    Handle?.Dispose();
                    Handle = null;
                },
                debugLabel: "CachedImage.disposeHandle");
        }
    }

    private sealed class CachedImage(ImageStreamCompleter completer, long? sizeBytes = null)
        : CachedImageBase(completer, sizeBytes);

    private sealed class LiveImage : CachedImageBase
    {
        private readonly Action _handleRemove;

        public LiveImage(ImageStreamCompleter completer, Action handleRemove) : base(completer)
        {
            _handleRemove = () =>
            {
                handleRemove();
                Dispose();
            };
            completer.AddOnLastListenerRemovedCallback(_handleRemove);
        }

        public override void Dispose()
        {
            Completer.RemoveOnLastListenerRemovedCallback(_handleRemove);
            base.Dispose();
        }

        public override string ToString() => Diagnostics.DescribeIdentity(this);
    }

    private sealed class PendingImage(ImageStreamCompleter completer, ImageStreamListener listener)
    {
        public ImageStreamCompleter Completer { get; } = completer;

        public ImageStreamListener Listener { get; } = listener;

        public void RemoveListener()
        {
            Completer.RemoveListener(Listener);
        }
    }
}

/// <summary>
/// Information about how the <see cref="ImageCache"/> is tracking an image.
/// </summary>
/// <remarks>
/// A <see cref="Pending"/> image is one that has not completed yet. It may also be tracked as
/// <see cref="Live"/> because something is listening to it. A <see cref="KeepAlive"/> image is being
/// held in the cache. It may also be <see cref="Live"/>. A <see cref="Live"/> image is being held
/// until its <see cref="ImageStreamCompleter"/> has no more listeners. It may also be
/// <see cref="Pending"/> or <see cref="KeepAlive"/>. An <see cref="Untracked"/> image is not being
/// cached. To obtain an <see cref="ImageCacheStatus"/>, use <see cref="ImageCache.StatusForKey"/> or
/// <see cref="ImageProvider.ObtainCacheStatus"/>.
/// </remarks>
public readonly record struct ImageCacheStatus
{
    /// <summary>Dart's private <c>ImageCacheStatus._</c>.</summary>
    internal ImageCacheStatus(bool Pending = false, bool KeepAlive = false, bool Live = false)
    {
        if (Constants.KDebugMode && Pending && KeepAlive)
        {
            throw new AssertionError("An image cannot be both pending and kept alive.");
        }

        this.Pending = Pending;
        this.KeepAlive = KeepAlive;
        this.Live = Live;
    }

    /// <summary>An image that has been submitted to <see cref="ImageCache.PutIfAbsent"/>, but not yet
    /// completed.</summary>
    public bool Pending { get; }

    /// <summary>An image that has been submitted to <see cref="ImageCache.PutIfAbsent"/>, has completed,
    /// fits based on the sizing rules of the cache, and has not been evicted.</summary>
    public bool KeepAlive { get; }

    /// <summary>An image that has been submitted to <see cref="ImageCache.PutIfAbsent"/> and has at least
    /// one listener on its <see cref="ImageStreamCompleter"/>.</summary>
    public bool Live { get; }

    /// <summary>An image that is tracked in some way by the <see cref="ImageCache"/>.</summary>
    public bool Tracked => Pending || KeepAlive || Live;

    /// <summary>An image that either has not been submitted to <see cref="ImageCache.PutIfAbsent"/> or has
    /// otherwise been evicted from the keepAlive and live caches.</summary>
    public bool Untracked => !Pending && !KeepAlive && !Live;

    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "ImageCacheStatus")}"
        + $"(pending: {DartBool(Pending)}, live: {DartBool(Live)}, keepAlive: {DartBool(KeepAlive)})";

    private static string DartBool(bool value) => value ? "true" : "false";
}
