using Plumix.Developer;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/image_cache.dart

public readonly record struct ImageCacheStatus(bool Pending, bool KeepAlive, bool Live)
{
    public bool Tracked => Pending || KeepAlive || Live;
    public bool Untracked => !Tracked;
    public static ImageCacheStatus UntrackedStatus => new(false, false, false);
}

public sealed class ImageCache
{
    private const int DefaultMaximumSize = 1000;
    private const long DefaultMaximumSizeBytes = 100L << 20;
    private readonly object _sync = new();
    private readonly Dictionary<object, PendingImage> _pendingImages = [];
    private readonly Dictionary<object, CachedImage> _cache = [];
    private readonly Dictionary<object, LiveImage> _liveImages = [];
    private readonly LinkedList<object> _lru = [];
    private int _maximumSize = DefaultMaximumSize;
    private long _maximumSizeBytes = DefaultMaximumSizeBytes;
    private long _currentSizeBytes;

    public static ImageCache Shared { get; } = new();

    public int MaximumSize
    {
        get { lock (_sync) return _maximumSize; }
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_sync)
            {
                if (value == _maximumSize) return;
            }

            TimelineTask? debugTimelineTask = null;
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask = new TimelineTask();
                debugTimelineTask.Start(
                    "ImageCache.setMaximumSize",
                    arguments: new Dictionary<string, object?> { ["value"] = value });
            }

            if (value == 0)
            {
                lock (_sync) _maximumSize = value;
                Clear();
            }
            else
            {
                List<CachedImage> evicted;
                lock (_sync)
                {
                    _maximumSize = value;
                    evicted = CheckCacheSizeLocked(debugTimelineTask);
                }
                DisposeAll(evicted);
            }

            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish();
            }
        }
    }

    public long MaximumSizeBytes
    {
        get { lock (_sync) return _maximumSizeBytes; }
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_sync)
            {
                if (value == _maximumSizeBytes) return;
            }

            TimelineTask? debugTimelineTask = null;
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask = new TimelineTask();
                debugTimelineTask.Start(
                    "ImageCache.setMaximumSizeBytes",
                    arguments: new Dictionary<string, object?> { ["value"] = value });
            }

            if (value == 0)
            {
                lock (_sync) _maximumSizeBytes = value;
                Clear();
            }
            else
            {
                List<CachedImage> evicted;
                lock (_sync)
                {
                    _maximumSizeBytes = value;
                    evicted = CheckCacheSizeLocked(debugTimelineTask);
                }
                DisposeAll(evicted);
            }

            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish();
            }
        }
    }

    public int CurrentSize
    {
        get { lock (_sync) return _cache.Count; }
    }

    public int PendingImageCount
    {
        get { lock (_sync) return _pendingImages.Count; }
    }

    public int LiveImageCount
    {
        get { lock (_sync) return _liveImages.Count; }
    }

    public long CurrentSizeBytes
    {
        get { lock (_sync) return _currentSizeBytes; }
    }

    public bool ContainsKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_sync) return _pendingImages.ContainsKey(key) || _cache.ContainsKey(key);
    }

    public ImageCacheStatus StatusForKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_sync)
        {
            return new ImageCacheStatus(
                Pending: _pendingImages.ContainsKey(key),
                KeepAlive: _cache.ContainsKey(key),
                Live: _liveImages.ContainsKey(key));
        }
    }

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

        List<CachedImage> touchEvicted = [];
        CachedImage? touchRejected = null;
        ImageStreamCompleter? liveCompleter = null;
        lock (_sync)
        {
            // Nothing needs to be done because the image hasn't loaded yet.
            if (_pendingImages.TryGetValue(key, out var pending))
            {
                if (!Constants.KReleaseMode)
                {
                    debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "pending" });
                }

                return pending.Completer;
            }

            // Remove the provider from the list so that we can move it to the recently used position
            // below.
            if (_cache.TryGetValue(key, out var cached))
            {
                if (!Constants.KReleaseMode)
                {
                    debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "keepAlive" });
                }

                TouchLocked(cached);
                TrackLiveImageLocked(key, cached.Completer, cached.SizeBytes);
                return cached.Completer;
            }

            if (_liveImages.TryGetValue(key, out var live))
            {
                // Dart's `_touch`: an image of known size that fits is kept alive, then the cache is
                // trimmed back under its limits.
                if (live.SizeBytes.HasValue)
                {
                    var resurrected = new CachedImage(
                        key,
                        live.Completer,
                        live.SizeBytes.Value,
                        _lru.AddLast(key));
                    if (CanCacheLocked(resurrected.SizeBytes))
                    {
                        _cache[key] = resurrected;
                        _currentSizeBytes += resurrected.SizeBytes;
                        touchEvicted = CheckCacheSizeLocked(debugTimelineTask);
                    }
                    else
                    {
                        _lru.Remove(resurrected.Node);
                        touchRejected = resurrected;
                    }
                }

                liveCompleter = live.Completer;
            }
        }

        if (liveCompleter is not null)
        {
            touchRejected?.Dispose();
            DisposeAll(touchEvicted);
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?> { ["result"] = "keepAlive" });
            }

            return liveCompleter;
        }

        ImageStreamCompleter completer;
        try
        {
            completer = loader();
        }
        catch (Exception exception)
        {
            var stackTrace = new System.Diagnostics.StackTrace(exception, true);
            if (!Constants.KReleaseMode)
            {
                debugTimelineTask!.Finish(new Dictionary<string, object?>
                {
                    ["result"] = "error",
                    ["error"] = exception.ToString(),
                    ["stackTrace"] = stackTrace.ToString(),
                });
            }

            if (onError is null) throw;
            onError(exception, stackTrace);
            return null;
        }

        if (!Constants.KReleaseMode)
        {
            debugTimelineTask!.Start("listener");
        }

        bool trackPending = false;
        PendingImage? pendingImage = null;
        ImageStreamListener? listener = null;
        int listenedOnce = 0;
        listener = new ImageStreamListener(
            OnImage: (image, syncCall) =>
            {
                if (Interlocked.Exchange(ref listenedOnce, 1) != 0)
                {
                    image.Dispose();
                    return;
                }

                long sizeBytes = image.SizeBytes;
                CompletePending(key, completer, pendingImage, image, trackPending, debugTimelineTask);
                completer.RemoveListener(listener!);
                image.Dispose();
                if (!Constants.KReleaseMode)
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
            });
        pendingImage = new PendingImage(completer, listener);

        lock (_sync)
        {
            if (_pendingImages.TryGetValue(key, out var racedPending))
            {
                return racedPending.Completer;
            }
            if (_cache.TryGetValue(key, out var racedCached))
            {
                return racedCached.Completer;
            }

            TrackLiveImageLocked(key, completer, sizeBytes: null);
            trackPending = _maximumSize > 0 && _maximumSizeBytes > 0;
            if (trackPending)
            {
                _pendingImages[key] = pendingImage;
            }
        }

        completer.AddListener(listener);
        return completer;
    }

    public bool Evict(object key, bool includeLive = true)
    {
        ArgumentNullException.ThrowIfNull(key);
        LiveImage? live = null;
        PendingImage? pending = null;
        CachedImage? cached = null;
        lock (_sync)
        {
            if (includeLive && _liveImages.Remove(key, out live))
            {
                // Dispose after releasing the cache lock.
            }

            if (_pendingImages.Remove(key, out pending))
            {
                // Pending wins over keepAlive, matching Flutter.
            }
            else if (_cache.Remove(key, out cached))
            {
                _lru.Remove(cached.Node);
                _currentSizeBytes -= cached.SizeBytes;
            }
        }

        live?.Dispose();
        if (pending is not null)
        {
            if (!Constants.KReleaseMode)
            {
                Timeline.InstantSync(
                    "ImageCache.evict",
                    arguments: new Dictionary<string, object?> { ["type"] = "pending" });
            }

            pending.RemoveListener();
            return true;
        }
        if (cached is not null)
        {
            if (!Constants.KReleaseMode)
            {
                Timeline.InstantSync(
                    "ImageCache.evict",
                    arguments: new Dictionary<string, object?>
                    {
                        ["type"] = "keepAlive",
                        ["sizeInBytes"] = cached.SizeBytes,
                    });
            }

            cached.Dispose();
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

    public void Clear()
    {
        PendingImage[] pending;
        CachedImage[] cached;
        lock (_sync)
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

            pending = [.. _pendingImages.Values];
            cached = [.. _cache.Values];
            _pendingImages.Clear();
            _cache.Clear();
            _lru.Clear();
            _currentSizeBytes = 0;
        }

        foreach (var image in pending) image.RemoveListener();
        DisposeAll(cached);
    }

    public void ClearLiveImages()
    {
        LiveImage[] live;
        lock (_sync)
        {
            live = [.. _liveImages.Values];
            _liveImages.Clear();
        }
        DisposeAll(live);
    }

    private void CompletePending(
        object key,
        ImageStreamCompleter completer,
        PendingImage? pending,
        ImageInfo image,
        bool trackPending,
        TimelineTask? timelineTask)
    {
        List<CachedImage> evicted = [];
        CachedImage? rejected = null;
        lock (_sync)
        {
            TrackLiveImageLocked(key, completer, image.SizeBytes);
            if (trackPending)
            {
                var cached = new CachedImage(key, completer, image.SizeBytes, _lru.AddLast(key));
                if (CanCacheLocked(cached.SizeBytes))
                {
                    _cache[key] = cached;
                    _currentSizeBytes += cached.SizeBytes;
                    evicted = CheckCacheSizeLocked(timelineTask);
                }
                else
                {
                    _lru.Remove(cached.Node);
                    rejected = cached;
                }
            }

            if (pending is not null
                && _pendingImages.TryGetValue(key, out var current)
                && ReferenceEquals(current, pending))
            {
                _pendingImages.Remove(key);
            }
        }

        rejected?.Dispose();
        DisposeAll(evicted);
    }

    private void TrackLiveImageLocked(object key, ImageStreamCompleter completer, long? sizeBytes)
    {
        if (_liveImages.TryGetValue(key, out var existing))
        {
            existing.SizeBytes ??= sizeBytes;
            return;
        }

        LiveImage? live = null;
        live = new LiveImage(completer, () => RemoveLiveImage(key, live!));
        live.SizeBytes = sizeBytes;
        _liveImages[key] = live;
    }

    private void RemoveLiveImage(object key, LiveImage live)
    {
        lock (_sync)
        {
            if (!_liveImages.TryGetValue(key, out var current) || !ReferenceEquals(current, live)) return;
            _liveImages.Remove(key);
        }
        live.Dispose();
    }

    private bool CanCacheLocked(long sizeBytes)
    {
        return _maximumSize > 0 && _maximumSizeBytes > 0 && sizeBytes <= _maximumSizeBytes;
    }

    private void TouchLocked(CachedImage image)
    {
        _lru.Remove(image.Node);
        _lru.AddLast(image.Node);
    }

    /// <remarks>
    /// Dart's <c>_checkCacheSize</c>: evicts least recently used images until the cache is back under
    /// its limits. The evicted images are returned so they are disposed outside the lock.
    /// </remarks>
    private List<CachedImage> CheckCacheSizeLocked(TimelineTask? timelineTask)
    {
        var finishArgs = new Dictionary<string, object?>();
        if (!Constants.KReleaseMode)
        {
            timelineTask!.Start("checkCacheSize");
        }

        List<CachedImage> evicted = [];
        while (_cache.Count > _maximumSize || _currentSizeBytes > _maximumSizeBytes)
        {
            object key = _lru.First!.Value;
            var image = _cache[key];
            _cache.Remove(key);
            _lru.RemoveFirst();
            _currentSizeBytes -= image.SizeBytes;
            evicted.Add(image);
            if (!Constants.KReleaseMode)
            {
                finishArgs[key.ToString() ?? "null"] = image.SizeBytes;
            }
        }

        if (!Constants.KReleaseMode)
        {
            finishArgs["endCacheSize"] = _cache.Count;
            finishArgs["endSizeBytes"] = _currentSizeBytes;
            timelineTask!.Finish(finishArgs);
        }

        return evicted;
    }

    private static void DisposeAll<T>(IEnumerable<T> entries) where T : IDisposable
    {
        foreach (var entry in entries) entry.Dispose();
    }

    private sealed class CachedImage : IDisposable
    {
        private ImageStreamCompleterHandle? _handle;

        public CachedImage(object key, ImageStreamCompleter completer, long sizeBytes, LinkedListNode<object> node)
        {
            Key = key;
            Completer = completer;
            SizeBytes = sizeBytes;
            Node = node;
            _handle = completer.KeepAlive();
        }

        public object Key { get; }
        public ImageStreamCompleter Completer { get; }
        public long SizeBytes { get; }
        public LinkedListNode<object> Node { get; }
        public void Dispose() => Interlocked.Exchange(ref _handle, null)?.Dispose();
    }

    private sealed class LiveImage : IDisposable
    {
        private readonly Action _handleRemove;
        private ImageStreamCompleterHandle? _handle;

        public LiveImage(ImageStreamCompleter completer, Action handleRemove)
        {
            Completer = completer;
            _handleRemove = handleRemove;
            _handle = completer.KeepAlive();
            completer.AddOnLastListenerRemovedCallback(_handleRemove);
        }

        public ImageStreamCompleter Completer { get; }
        public long? SizeBytes { get; set; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _handle, null) is not { } handle) return;
            if (!Completer.IsDisposed)
            {
                Completer.RemoveOnLastListenerRemovedCallback(_handleRemove);
            }
            handle.Dispose();
        }
    }

    private sealed record PendingImage(ImageStreamCompleter Completer, ImageStreamListener Listener)
    {
        public void RemoveListener()
        {
            if (!Completer.IsDisposed) Completer.RemoveListener(Listener);
        }
    }
}
