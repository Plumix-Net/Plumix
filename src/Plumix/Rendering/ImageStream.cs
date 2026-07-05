using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/image_stream.dart

public delegate void ImageListener(ImageInfo image, bool synchronousCall);
public delegate void ImageChunkListener(ImageChunkEvent @event);
public delegate void ImageErrorListener(Exception exception, StackTrace? stackTrace);

public sealed record ImageChunkEvent
{
    public ImageChunkEvent(long cumulativeBytesLoaded, long? expectedTotalBytes)
    {
        if (cumulativeBytesLoaded < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cumulativeBytesLoaded));
        }

        if (expectedTotalBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedTotalBytes));
        }

        CumulativeBytesLoaded = cumulativeBytesLoaded;
        ExpectedTotalBytes = expectedTotalBytes;
    }

    public long CumulativeBytesLoaded { get; }
    public long? ExpectedTotalBytes { get; }
}

public sealed record ImageStreamListener(
    ImageListener OnImage,
    ImageChunkListener? OnChunk = null,
    ImageErrorListener? OnError = null);

public sealed class ImageInfo : IDisposable
{
    private readonly SharedImage _sharedImage;
    private int _disposed;

    public ImageInfo(IImage image, double scale = 1.0, string? debugLabel = null)
        : this(new SharedImage(image ?? throw new ArgumentNullException(nameof(image))), scale, debugLabel)
    {
    }

    private ImageInfo(SharedImage sharedImage, double scale, string? debugLabel)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Image scale must be finite and positive.");
        }

        _sharedImage = sharedImage;
        Scale = scale;
        DebugLabel = debugLabel;
    }

    public IImage Image => _sharedImage.Image;
    public double Scale { get; }
    public string? DebugLabel { get; }
    public long SizeBytes => Image is Bitmap bitmap
        ? checked((long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4L)
        : checked((long)Math.Ceiling(Image.Size.Width) * (long)Math.Ceiling(Image.Size.Height) * 4L);

    public ImageInfo Clone()
    {
        _sharedImage.AddReference();
        return new ImageInfo(_sharedImage, Scale, DebugLabel);
    }

    public bool IsCloneOf(ImageInfo other)
    {
        return ReferenceEquals(Image, other.Image)
               && Scale.Equals(other.Scale)
               && string.Equals(DebugLabel, other.DebugLabel, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _sharedImage.Release();
        }
    }

    public override string ToString() => $"{DebugLabel ?? Image.ToString()} @ {Scale:0.0}x";

    private sealed class SharedImage
    {
        private int _references = 1;

        public SharedImage(IImage image)
        {
            Image = image;
        }

        public IImage Image { get; }

        public void AddReference()
        {
            if (Interlocked.Increment(ref _references) <= 1)
            {
                throw new ObjectDisposedException(nameof(ImageInfo));
            }
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref _references) == 0 && Image is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

public sealed class ImageStream
{
    private ImageStreamCompleter? _completer;
    private List<ImageStreamListener>? _listeners;

    public ImageStreamCompleter? Completer => _completer;
    public object Key => (object?)_completer ?? this;

    public void SetCompleter(ImageStreamCompleter value)
    {
        if (_completer is not null)
        {
            throw new InvalidOperationException("An ImageStream completer can only be assigned once.");
        }

        _completer = value ?? throw new ArgumentNullException(nameof(value));
        if (_listeners is null)
        {
            return;
        }

        var initialListeners = _listeners;
        _listeners = null;
        foreach (var listener in initialListeners)
        {
            _completer.AddListener(listener);
        }
    }

    public void AddListener(ImageStreamListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (_completer is not null)
        {
            _completer.AddListener(listener);
            return;
        }

        _listeners ??= [];
        _listeners.Add(listener);
    }

    public void RemoveListener(ImageStreamListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (_completer is not null)
        {
            _completer.RemoveListener(listener);
            return;
        }

        _listeners?.Remove(listener);
    }
}

public sealed class ImageStreamCompleterHandle : IDisposable
{
    private ImageStreamCompleter? _completer;

    internal ImageStreamCompleterHandle(ImageStreamCompleter completer)
    {
        _completer = completer;
        completer.AddKeepAliveHandle();
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _completer, null)?.RemoveKeepAliveHandle();
    }
}

public abstract class ImageStreamCompleter
{
    private readonly List<ImageStreamListener> _listeners = [];
    private readonly List<ImageErrorListener> _ephemeralErrorListeners = [];
    private readonly List<Action> _lastListenerRemovedCallbacks = [];
    private ImageInfo? _currentImage;
    private (Exception Exception, StackTrace? Stack)? _currentError;
    private int _keepAliveHandles;
    private bool _disposed;

    public static event ImageErrorListener? UnhandledError;

    public string? DebugLabel { get; protected set; }
    public bool HasListeners => _listeners.Count > 0;
    public bool IsDisposed => _disposed;

    public void AddListener(ImageStreamListener listener)
    {
        CheckNotDisposed();
        _listeners.Add(listener);
        if (_currentImage is not null)
        {
            listener.OnImage(_currentImage.Clone(), synchronousCall: true);
        }

        if (_currentError is { } error && listener.OnError is not null)
        {
            listener.OnError(error.Exception, error.Stack);
        }
    }

    public void RemoveListener(ImageStreamListener listener)
    {
        CheckNotDisposed();
        _listeners.Remove(listener);
        if (_listeners.Count != 0)
        {
            return;
        }

        var callbacks = _lastListenerRemovedCallbacks.ToArray();
        _lastListenerRemovedCallbacks.Clear();
        foreach (var callback in callbacks)
        {
            callback();
        }

        MaybeDispose();
    }

    public void AddEphemeralErrorListener(ImageErrorListener listener)
    {
        CheckNotDisposed();
        if (_currentError is { } error)
        {
            listener(error.Exception, error.Stack);
        }
        else if (_currentImage is null)
        {
            _ephemeralErrorListeners.Add(listener);
        }
    }

    public ImageStreamCompleterHandle KeepAlive()
    {
        CheckNotDisposed();
        return new ImageStreamCompleterHandle(this);
    }

    public void AddOnLastListenerRemovedCallback(Action callback)
    {
        CheckNotDisposed();
        _lastListenerRemovedCallbacks.Add(callback);
    }

    public void RemoveOnLastListenerRemovedCallback(Action callback)
    {
        CheckNotDisposed();
        _lastListenerRemovedCallbacks.Remove(callback);
    }

    public void MaybeDispose()
    {
        if (_disposed || _listeners.Count > 0 || _keepAliveHandles > 0)
        {
            return;
        }

        _ephemeralErrorListeners.Clear();
        _currentImage?.Dispose();
        _currentImage = null;
        _disposed = true;
        OnDisposed();
    }

    protected virtual void OnDisposed()
    {
    }

    protected void SetImage(ImageInfo image)
    {
        CheckNotDisposed();
        _currentImage?.Dispose();
        _currentImage = image;
        _currentError = null;
        _ephemeralErrorListeners.Clear();

        foreach (var listener in _listeners.ToArray())
        {
            listener.OnImage(image.Clone(), synchronousCall: false);
        }
    }

    public void ReportError(Exception exception, StackTrace? stackTrace = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _currentError = (exception, stackTrace);
        var errorListeners = _listeners
            .Select(listener => listener.OnError)
            .Where(listener => listener is not null)
            .Cast<ImageErrorListener>()
            .Concat(_ephemeralErrorListeners)
            .ToArray();
        _ephemeralErrorListeners.Clear();

        foreach (var listener in errorListeners)
        {
            listener(exception, stackTrace);
        }

        if (errorListeners.Length == 0)
        {
            UnhandledError?.Invoke(exception, stackTrace);
        }
    }

    public void ReportImageChunkEvent(ImageChunkEvent @event)
    {
        CheckNotDisposed();
        foreach (var listener in _listeners.ToArray())
        {
            listener.OnChunk?.Invoke(@event);
        }
    }

    internal void AddKeepAliveHandle()
    {
        CheckNotDisposed();
        _keepAliveHandles++;
    }

    internal void RemoveKeepAliveHandle()
    {
        if (_keepAliveHandles <= 0)
        {
            return;
        }

        _keepAliveHandles--;
        MaybeDispose();
    }

    private void CheckNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                GetType().Name,
                "An ImageStreamCompleter is disposed after its last listener and keep-alive handle are removed.");
        }
    }
}

public sealed class OneFrameImageStreamCompleter : ImageStreamCompleter
{
    public OneFrameImageStreamCompleter(Task<ImageInfo> image, string? debugLabel = null)
    {
        DebugLabel = debugLabel;
        _ = CompleteAsync(image ?? throw new ArgumentNullException(nameof(image)));
    }

    private async Task CompleteAsync(Task<ImageInfo> image)
    {
        await Task.Yield();
        try
        {
            SetImage(await image.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            ReportError(exception, new StackTrace(exception, fNeedFileInfo: true));
        }
    }
}
