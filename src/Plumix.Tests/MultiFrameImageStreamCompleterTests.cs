using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/image_stream_test.dart
[Collection(SchedulerTestCollection.Name)]
public sealed class MultiFrameImageStreamCompleterTests : IDisposable
{
    private readonly FakeGestureTimers _timers;
    private readonly FlutterExceptionHandler? _previousError;
    private readonly List<FlutterErrorDetails> _errors = [];
    private TimeSpan _clock;

    public MultiFrameImageStreamCompleterTests()
    {
        SchedulerBinding.ResetForTests();
        _timers = new FakeGestureTimers();
        _previousError = FlutterError.OnError;
        FlutterError.OnError = _errors.Add;
    }

    public void Dispose()
    {
        _timers.Dispose();
        SchedulerBinding.ResetForTests();
        FlutterError.OnError = _previousError;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecodeStartsOnlyWhenBothCodecAndListenerAreReady(bool codecFirst)
    {
        var pending = new TaskCompletionSource<Codec>();
        var codec = new FakeCodec();
        var completer = new MultiFrameImageStreamCompleter(pending.Task, 2, "animation");
        var listener = DisposingListener();
        if (codecFirst)
        {
            pending.SetResult(codec);
            Drain();
            Assert.Equal(0, codec.Requests);
            completer.AddListener(listener);
        }
        else
        {
            completer.AddListener(listener);
            Drain();
            Assert.Equal(0, codec.Requests);
            pending.SetResult(codec);
        }

        Assert.True(EventLoopPump.SpinUntil(() => codec.Requests == 1));
        Assert.False(codec.Disposed);
        completer.RemoveListener(listener);
        Assert.True(codec.Disposed);
    }

    [Fact]
    public void FirstAnimatedFrameWaitsForAppFrameAndNextWaitsForCurrentDuration()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        List<ImageInfo> images = [];
        var listener = new ImageStreamListener((image, sync) =>
        {
            Assert.False(sync);
            images.Add(image);
        });
        completer.AddListener(listener);
        codec.Complete(200);
        Drain();
        Assert.Empty(images);
        Pump();
        Assert.Single(images);
        Assert.Equal(2, images[0].Scale);
        Assert.Equal("animation", images[0].DebugLabel);
        Assert.Equal(2, codec.Requests);
        codec.Complete(400);
        Drain();
        Pump(100);
        Assert.Single(images);
        Pump(100);
        Assert.Equal(2, images.Count);
        codec.Complete(200);
        Drain();
        Pump(399);
        Assert.Equal(2, images.Count);
        Pump(1);
        Assert.Equal(3, images.Count);
        completer.RemoveListener(listener);
        Assert.True(codec.Disposed);
        Assert.All(images, image => image.Dispose());
        Assert.Empty(_errors);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 4)]
    [InlineData(2, 6)]
    public void AnimationDoesNotRepeatMoreThanSpecified(int repeats, int frames)
    {
        var codec = new FakeCodec(repetitions: repeats);
        var completer = Ready(codec);
        int delivered = 0;
        var listener = new ImageStreamListener((image, _) => { delivered++; image.Dispose(); });
        completer.AddListener(listener);
        for (int index = 0; index < frames; index++)
        {
            codec.Complete(10);
            Drain();
            Pump(10);
        }

        Assert.Equal(frames, delivered);
        Assert.Equal(frames, codec.Requests);
        Assert.True(codec.Disposed);
        Pump(1000);
        Assert.Equal(frames, delivered);
        completer.RemoveListener(listener);
    }

    [Fact]
    public void ZeroDurationStillEmitsAtMostOneImagePerAppFrame()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        int count = 0;
        var listener = new ImageStreamListener((image, _) => { count++; image.Dispose(); });
        completer.AddListener(listener);
        codec.Complete(0);
        Drain();
        Assert.Equal(0, count);
        Pump();
        codec.Complete(0);
        Drain();
        Assert.Equal(1, count);
        Pump();
        Assert.Equal(2, count);
        completer.RemoveListener(listener);
    }

    [Fact]
    public void KeepAliveDoesNotDriveFramesAndLastListenerCallbackStillRuns()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        using ImageStreamCompleterHandle handle = completer.KeepAlive();
        Assert.Equal(0, codec.Requests);
        bool removed = false;
        completer.AddOnLastListenerRemovedCallback(() => removed = true);
        var listener = DisposingListener();
        completer.AddListener(listener);
        codec.Complete(200);
        Drain();
        Pump();
        Assert.Equal(2, codec.Requests);
        completer.RemoveListener(listener);
        Assert.True(removed);
        Assert.False(codec.Disposed);
        codec.Complete(200);
        Drain();
        Pump(1000);
        Assert.Equal(2, codec.Requests);
        Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
        completer.AddListener(listener);
        Assert.Equal(3, codec.Requests);
        handle.Dispose();
        Assert.False(codec.Disposed);
        completer.RemoveListener(listener);
        Assert.True(codec.Disposed);
    }

    [Fact]
    public void RemovingLastListenerCancelsFrameTimerEvenWithKeepAlive()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        using ImageStreamCompleterHandle handle = completer.KeepAlive();
        var listener = DisposingListener();
        completer.AddListener(listener);
        codec.Complete(200);
        Drain();
        Pump();
        codec.Complete(200);
        Drain();
        Pump(10);
        completer.RemoveListener(listener);
        _timers.Elapse(TimeSpan.FromSeconds(1));
        Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
        Assert.Equal(2, codec.Requests);
    }

    [Fact]
    public void TimeDilationMultipliesRemainingFrameTimerDelay()
    {
        Scheduler.TimeDilation = 2;
        var codec = new FakeCodec();
        var completer = Ready(codec);
        var listener = DisposingListener();
        completer.AddListener(listener);
        codec.Complete(200);
        Drain();
        Pump();
        codec.Complete(400);
        Drain();
        Pump();
        Pump(200);
        Assert.Equal(2, codec.Requests);
        Pump(200);
        Assert.Equal(3, codec.Requests);
        completer.RemoveListener(listener);
    }

    [Fact]
    public void StaticImageEmitsWithoutAppFrameAndOnlyDecodesOnce()
    {
        var codec = new FakeCodec(frames: 1);
        var completer = Ready(codec);
        using ImageStreamCompleterHandle handle = completer.KeepAlive();
        int count = 0;
        var listener = new ImageStreamListener((image, _) => { count++; image.Dispose(); });
        completer.AddListener(listener);
        codec.Complete(0);
        Drain();
        Assert.Equal(1, count);
        Assert.True(codec.Disposed);
        Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
        completer.RemoveListener(listener);
        completer.AddListener(listener);
        Assert.Equal(2, count);
        Assert.Equal(1, codec.Requests);
        completer.RemoveListener(listener);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ImageCallbackCanRemoveItsOwnLastListener(int frames)
    {
        var codec = new FakeCodec(frames: frames);
        var completer = Ready(codec);
        ImageStreamListener? listener = null;
        listener = new ImageStreamListener((image, _) =>
        {
            completer.RemoveListener(listener!);
            Assert.NotEmpty(RasterBackend.ReadPixels((Bitmap)image.Image));
            image.Dispose();
        });
        completer.AddListener(listener);
        codec.Complete(200);
        Drain();
        Pump();
        Assert.True(codec.Disposed);
        Assert.Empty(_errors);
    }

    [Fact]
    public void DisposalWhileCodecOrFrameIsPendingReleasesLateResources()
    {
        var pendingCodec = new TaskCompletionSource<Codec>();
        var completer = new MultiFrameImageStreamCompleter(pendingCodec.Task, 1);
        completer.MaybeDispose();
        var codec = new FakeCodec();
        pendingCodec.SetResult(codec);
        Assert.True(EventLoopPump.SpinUntil(() => codec.Disposed));
        Assert.Equal(0, codec.Requests);

        var decoding = new FakeCodec();
        var second = Ready(decoding);
        var listener = DisposingListener();
        second.AddListener(listener);
        second.RemoveListener(listener);
        decoding.Complete(20);
        Drain();
        Assert.True(decoding.Disposed);
        Assert.False(SchedulerBinding.Instance.HasScheduledFrame);
        Assert.Empty(_errors);
    }

    [Fact]
    public void MultipleAndDuplicateListenersRemainIndependent()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        int first = 0;
        int second = 0;
        var listener = new ImageStreamListener((image, _) => { first++; image.Dispose(); });
        var other = new ImageStreamListener((image, _) => { second++; image.Dispose(); });
        completer.AddListener(listener);
        completer.AddListener(listener);
        completer.AddListener(other);
        completer.RemoveListener(listener);
        codec.Complete(20);
        Drain();
        Pump();
        Assert.Equal(1, first);
        Assert.Equal(1, second);
        completer.RemoveListener(listener);
        codec.Complete(20);
        Drain();
        Pump(20);
        Assert.Equal(1, first);
        Assert.Equal(2, second);
        completer.RemoveListener(other);
        Assert.True(codec.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingStreamListenersAreAsynchronousAndNewListenersAreSynchronous(bool setFirst)
    {
        var completer = new TestCompleter();
        completer.Emit();
        var stream = new ImageStream();
        bool? synchronous = null;
        var listener = new ImageStreamListener((image, sync) => { synchronous = sync; image.Dispose(); });
        if (setFirst) stream.SetCompleter(completer);
        stream.AddListener(listener);
        if (!setFirst) stream.SetCompleter(completer);
        Assert.Equal(setFirst, synchronous);
        stream.RemoveListener(listener);
    }

    [Fact]
    public void CodecAndFrameErrorsCarryTheirContextAndCanBeIntercepted()
    {
        var exception = new InvalidOperationException("codec failure");
        var failed = new MultiFrameImageStreamCompleter(Task.FromException<Codec>(exception), 1);
        Drain();
        Assert.Same(exception, Assert.Single(_errors).Exception);
        Assert.Equal("resolving an image codec", _errors[0].Context!.ToDescription());
        Assert.True(_errors[0].Silent);
        failed.MaybeDispose();
        _errors.Clear();

        var codec = new FakeCodec();
        var completer = Ready(codec);
        Exception? intercepted = null;
        var listener = new ImageStreamListener((_, _) => { }, OnError: (error, _) => intercepted = error);
        completer.AddListener(listener);
        codec.Fail(exception);
        Assert.True(EventLoopPump.SpinUntil(() => intercepted is not null));
        Assert.Same(exception, intercepted);
        Assert.Empty(_errors);
        Assert.False(codec.Disposed);
        completer.RemoveListener(listener);
    }

    [Fact]
    public void RethrownErrorsReachFrameworkAndBadImageListenerDoesNotStopOtherListeners()
    {
        var completer = new TestCompleter();
        var exception = new InvalidOperationException("failure");
        var first = new ImageStreamListener((image, _) => { image.Dispose(); throw exception; },
            OnError: (error, _) => throw error);
        bool delivered = false;
        var second = new ImageStreamListener((image, _) => { delivered = true; image.Dispose(); });
        completer.AddListener(first);
        completer.AddListener(second);
        completer.Emit();
        Assert.True(delivered);
        Assert.Same(exception, Assert.Single(_errors).Exception);
        completer.RemoveListener(first);
        completer.RemoveListener(second);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void ReportErrorsPolicySurvivesListenerRemoval(bool report, int expected)
    {
        var completer = new TestCompleter();
        var listener = new ImageStreamListener((image, _) => image.Dispose(), ReportErrors: report);
        completer.AddListener(listener);
        completer.RemoveListener(listener);
        completer.ReportError(new InvalidOperationException("late error"));
        Assert.Equal(expected, _errors.Count);
    }

    [Fact]
    public void ChunkEventsAreNotBufferedErrorsAreReportedAndDisposalUnsubscribes()
    {
        var chunks = new ChunkSource();
        var codec = new FakeCodec();
        var completer = new MultiFrameImageStreamCompleter(Task.FromResult<Codec>(codec), 1,
            chunkEvents: chunks);
        Assert.NotNull(chunks.Observer);
        chunks.Emit(1);
        List<long> events = [];
        var listener = new ImageStreamListener((image, _) => image.Dispose(),
            OnChunk: chunk => events.Add(chunk.CumulativeBytesLoaded));
        completer.AddListener(listener);
        chunks.Emit(2);
        chunks.Observer!.OnError(new InvalidOperationException("chunk error"));
        chunks.Emit(3);
        Assert.Equal([2L, 3L], events);
        Assert.Equal("loading an image", Assert.Single(_errors).Context!.ToDescription());
        completer.RemoveListener(listener);
        Assert.Null(chunks.Observer);
        chunks.Emit(4);
        Assert.Equal([2L, 3L], events);
        Drain();
        Assert.True(codec.Disposed);
    }

    [Fact]
    public void ReadyStreamThatNeverHadListenersCanBeDisposed()
    {
        var codec = new FakeCodec();
        var completer = Ready(codec);
        completer.MaybeDispose();
        Assert.True(codec.Disposed);
        Assert.Equal(0, codec.Requests);
    }

    [Fact]
    public void ImageInfoAndKeepAliveHandlesDispatchMemoryEventsAndClonesRetainPixels()
    {
        List<ObjectEvent> events = [];
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        ImageInfo image;
        ImageInfo clone;
        ImageStreamCompleterHandle handle;
        try
        {
            image = new ImageInfo(NewBitmap(), 2, "test");
            clone = image.Clone();
            Assert.True(image.IsCloneOf(clone));
            image.Dispose();
            Assert.NotEmpty(RasterBackend.ReadPixels((Bitmap)clone.Image));
            clone.Dispose();
            var completer = new TestCompleter();
            handle = completer.KeepAlive();
            handle.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }

        if (Constants.KDebugMode)
        {
            Assert.Collection(events,
                created => Assert.Same(image, Assert.IsType<ObjectCreated>(created).Object),
                created => Assert.Same(clone, Assert.IsType<ObjectCreated>(created).Object),
                disposed => Assert.Same(image, Assert.IsType<ObjectDisposed>(disposed).Object),
                disposed => Assert.Same(clone, Assert.IsType<ObjectDisposed>(disposed).Object),
                created => Assert.Same(handle, Assert.IsType<ObjectCreated>(created).Object),
                disposed => Assert.Same(handle, Assert.IsType<ObjectDisposed>(disposed).Object));
        }
    }

    private MultiFrameImageStreamCompleter Ready(FakeCodec codec)
    {
        var completer = new MultiFrameImageStreamCompleter(Task.FromResult<Codec>(codec), 2, "animation");
        Drain();
        return completer;
    }

    private void Pump(int milliseconds = 0)
    {
        TimeSpan elapsed = TimeSpan.FromMilliseconds(milliseconds);
        _clock += elapsed;
        _timers.Elapse(elapsed);
        SchedulerBinding.PumpFrameForTests(_clock);
        Drain();
    }

    private static void Drain() => EventLoop.FlushMicrotasks();
    private static ImageStreamListener DisposingListener() => new((image, _) => image.Dispose());

    private sealed class FakeCodec(int frames = 2, int repetitions = -1) : Codec
    {
        private readonly Queue<TaskCompletionSource<FrameInfo>> _pending = [];
        public int Requests { get; private set; }
        public bool Disposed { get; private set; }
        public override int FrameCount => frames;
        public override int RepetitionCount => repetitions;

        public override Task<FrameInfo> GetNextFrame()
        {
            Assert.False(Disposed);
            Requests++;
            var result = new TaskCompletionSource<FrameInfo>();
            _pending.Enqueue(result);
            return result.Task;
        }

        public void Complete(int milliseconds) => _pending.Dequeue().SetResult(new FrameInfo(
            TimeSpan.FromMilliseconds(milliseconds), NewBitmap()));
        public void Fail(Exception exception) => _pending.Dequeue().SetException(exception);
        public override void Dispose() => Disposed = true;
    }

    private static Bitmap NewBitmap() => new(AnimatedImageCodecTests.Fixture("four_frame_with_reuse_end.png"));

    private sealed class TestCompleter : ImageStreamCompleter
    {
        public void Emit() => SetImage(new ImageInfo(NewBitmap()));
    }

    private sealed class ChunkSource : IObservable<ImageChunkEvent>, IDisposable
    {
        public IObserver<ImageChunkEvent>? Observer { get; private set; }
        public IDisposable Subscribe(IObserver<ImageChunkEvent> observer)
        {
            Observer = observer;
            return this;
        }

        public void Emit(int loaded) => Observer?.OnNext(new ImageChunkEvent(loaded, 4));
        public void Dispose() => Observer = null;
    }
}
