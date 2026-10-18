using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/image_stream.dart

/// <summary>Decodes images on demand and emits animated frames on the framework's frame clock.</summary>
public sealed class MultiFrameImageStreamCompleter : ImageStreamCompleter
{
    private IDisposable? _chunkSubscription;
    private Codec? _codec;
    private readonly double _scale;
    private readonly InformationCollector? _informationCollector;
    private FrameInfo? _nextFrame;
    private ImageInfo? _nextFrameImage;
    private TimeSpan _shownTimestamp;
    private TimeSpan? _frameDuration;
    private int _framesEmitted;
    private GestureTimer? _timer;
    private bool _frameCallbackScheduled;

    public MultiFrameImageStreamCompleter(
        Task<Codec> codec,
        double scale,
        string? debugLabel = null,
        IObservable<ImageChunkEvent>? chunkEvents = null,
        InformationCollector? informationCollector = null)
    {
        ArgumentNullException.ThrowIfNull(codec);
        _scale = scale;
        _informationCollector = informationCollector;
        DebugLabel = debugLabel;
        EventLoop.RunAsync(() => HandleCodecReady(codec));
        _chunkSubscription = chunkEvents?.Subscribe(new ChunkObserver(this));
    }

    private async Task HandleCodecReady(Task<Codec> pendingCodec)
    {
        try
        {
            await Task.Yield();
            Codec codec = await pendingCodec;
            if (IsDisposed)
            {
                codec.Dispose();
                return;
            }

            _codec = codec;
            if (HasListeners)
            {
                EventLoop.RunAsync(DecodeNextFrameAndSchedule);
            }
        }
        catch (Exception exception)
        {
            ReportImageError(exception, "resolving an image codec");
        }
    }

    private void HandleAppFrame(TimeSpan timestamp)
    {
        _frameCallbackScheduled = false;
        if (!HasListeners)
        {
            return;
        }

        FrameInfo nextFrame = _nextFrame!;
        if (_frameDuration is null || timestamp - _shownTimestamp >= _frameDuration)
        {
            EmitFrame(_nextFrameImage!.Clone());
            _shownTimestamp = timestamp;
            _frameDuration = nextFrame.Duration;
            ReleaseNextFrame();
            if (_codec is null)
            {
                return;
            }

            int completedCycles = _framesEmitted / _codec.FrameCount;
            if (_codec.RepetitionCount == -1 || completedCycles <= _codec.RepetitionCount)
            {
                EventLoop.RunAsync(DecodeNextFrameAndSchedule);
                return;
            }

            _codec.Dispose();
            _codec = null;
            return;
        }

        TimeSpan delay = _frameDuration.Value - (timestamp - _shownTimestamp);
        _timer = GestureTimer.Start(delay * Scheduler.TimeDilation, ScheduleAppFrame);
    }

    private async Task DecodeNextFrameAndSchedule()
    {
        ReleaseNextFrame();
        try
        {
            Task<FrameInfo> pendingFrame = _codec!.GetNextFrame();
            await Task.Yield();
            FrameInfo frame = await pendingFrame;
            if (_codec is null)
            {
                // Unlike a Dart image handle, an Avalonia bitmap owns unmanaged memory directly.
                frame.Image.Dispose();
                return;
            }

            _nextFrame = frame;
            _nextFrameImage = new ImageInfo(frame.Image, _scale, DebugLabel);
        }
        catch (Exception exception)
        {
            ReportImageError(exception, "resolving an image frame");
            return;
        }

        if (_codec.FrameCount == 1)
        {
            if (!HasListeners)
            {
                return;
            }

            EmitFrame(_nextFrameImage!.Clone());
            ReleaseNextFrame();
            _codec?.Dispose();
            _codec = null;
            return;
        }

        ScheduleAppFrame();
    }

    private void ScheduleAppFrame()
    {
        if (_frameCallbackScheduled)
        {
            return;
        }

        _frameCallbackScheduled = true;
        SchedulerBinding.Instance.ScheduleFrameCallback(HandleAppFrame);
    }

    private void EmitFrame(ImageInfo imageInfo)
    {
        SetImage(imageInfo);
        _framesEmitted++;
    }

    public override void AddListener(ImageStreamListener listener)
    {
        if (!HasListeners && _codec is not null && (CurrentImage is null || _codec.FrameCount > 1))
        {
            EventLoop.RunAsync(DecodeNextFrameAndSchedule);
        }

        base.AddListener(listener);
    }

    public override void RemoveListener(ImageStreamListener listener)
    {
        base.RemoveListener(listener);
        if (!HasListeners)
        {
            _timer?.Cancel();
            _timer = null;
        }
    }

    protected override void OnDisposed()
    {
        _chunkSubscription?.Dispose();
        _chunkSubscription = null;
        _timer?.Cancel();
        _timer = null;
        _codec?.Dispose();
        _codec = null;
        ReleaseNextFrame();
        base.OnDisposed();
    }

    private void ReleaseNextFrame()
    {
        _nextFrameImage?.Dispose();
        _nextFrameImage = null;
        _nextFrame = null;
    }

    private void ReportImageError(Exception exception, string context) =>
        ReportError(exception, new StackTrace(exception, true), new ErrorDescription(context),
            _informationCollector, silent: true);

    private sealed class ChunkObserver(MultiFrameImageStreamCompleter owner) : IObserver<ImageChunkEvent>
    {
        public void OnNext(ImageChunkEvent value)
        {
            if (!owner.IsDisposed)
            {
                owner.ReportImageChunkEvent(value);
            }
        }

        public void OnError(Exception error) => owner.ReportImageError(error, "loading an image");
        public void OnCompleted() { }
    }
}
