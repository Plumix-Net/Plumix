using System.Runtime.ExceptionServices;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/async.dart

public enum ConnectionState
{
    None,
    Waiting,
    Active,
    Done,
}

public sealed class AsyncSnapshot<T> : IEquatable<AsyncSnapshot<T>>
{
    private readonly ExceptionDispatchInfo? _errorDispatchInfo;
    private readonly bool _hasData;

    private AsyncSnapshot(
        ConnectionState connectionState,
        T? data,
        bool hasData,
        Exception? error,
        string? stackTrace,
        ExceptionDispatchInfo? errorDispatchInfo = null)
    {
        if (hasData && error is not null)
        {
            throw new ArgumentException("An async snapshot cannot contain both data and an error.");
        }

        if (stackTrace is not null && error is null)
        {
            throw new ArgumentException("An async snapshot stack trace requires an error.", nameof(stackTrace));
        }

        ConnectionState = connectionState;
        Data = data;
        _hasData = hasData;
        Error = error;
        StackTrace = stackTrace;
        _errorDispatchInfo = errorDispatchInfo;
    }

    public ConnectionState ConnectionState { get; }

    public T? Data { get; }

    public Exception? Error { get; }

    public string? StackTrace { get; }

    public bool HasData => _hasData;

    public bool HasError => Error is not null;

    public T RequireData
    {
        get
        {
            if (HasData)
            {
                return Data!;
            }

            if (Error is not null)
            {
                _errorDispatchInfo?.Throw();
                ExceptionDispatchInfo.Capture(Error).Throw();
            }

            throw new InvalidOperationException("Snapshot has neither data nor error.");
        }
    }

    public static AsyncSnapshot<T> Nothing()
    {
        return new AsyncSnapshot<T>(ConnectionState.None, default, hasData: false, null, null);
    }

    public static AsyncSnapshot<T> Waiting()
    {
        return new AsyncSnapshot<T>(ConnectionState.Waiting, default, hasData: false, null, null);
    }

    public static AsyncSnapshot<T> WithData(ConnectionState state, T data)
    {
        return new AsyncSnapshot<T>(state, data, hasData: data is not null, null, null);
    }

    public static AsyncSnapshot<T> WithError(
        ConnectionState state,
        Exception error,
        string? stackTrace = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        var dispatchInfo = ExceptionDispatchInfo.Capture(error);
        return new AsyncSnapshot<T>(
            state,
            default,
            hasData: false,
            error,
            stackTrace ?? error.StackTrace ?? string.Empty,
            dispatchInfo);
    }

    public AsyncSnapshot<T> InState(ConnectionState state)
    {
        return new AsyncSnapshot<T>(state, Data, _hasData, Error, StackTrace, _errorDispatchInfo);
    }

    public bool Equals(AsyncSnapshot<T>? other)
    {
        return other is not null
               && ConnectionState == other.ConnectionState
               && _hasData == other._hasData
               && EqualityComparer<T?>.Default.Equals(Data, other.Data)
               && Equals(Error, other.Error)
               && string.Equals(StackTrace, other.StackTrace, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is AsyncSnapshot<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ConnectionState, Data, Error);
    }

    public override string ToString()
    {
        return $"{nameof(AsyncSnapshot<T>)}({ConnectionState}, {Data}, {Error}, {StackTrace})";
    }
}

public delegate Widget AsyncWidgetBuilder<T>(BuildContext context, AsyncSnapshot<T> snapshot);

public abstract class StreamBuilderBase<T, TSummary> : StatefulWidget
{
    protected StreamBuilderBase(IObservable<T>? stream, Key? key = null) : base(key)
    {
        Stream = stream;
    }

    public IObservable<T>? Stream { get; }

    public abstract TSummary Initial();

    public virtual TSummary AfterConnected(TSummary current)
    {
        return current;
    }

    public abstract TSummary AfterData(TSummary current, T data);

    public virtual TSummary AfterError(TSummary current, Exception error, string stackTrace)
    {
        return current;
    }

    public virtual TSummary AfterDone(TSummary current)
    {
        return current;
    }

    public virtual TSummary AfterDisconnected(TSummary current)
    {
        return current;
    }

    public abstract Widget Build(BuildContext context, TSummary currentSummary);

    public override State CreateState()
    {
        return new StreamBuilderBaseState();
    }

    private sealed class StreamBuilderBaseState : State
    {
        private IDisposable? _subscription;
        private object? _subscriptionIdentity;
        private TSummary _summary = default!;

        private StreamBuilderBase<T, TSummary> CurrentWidget =>
            (StreamBuilderBase<T, TSummary>)StateWidget;

        public override void InitState()
        {
            _summary = CurrentWidget.Initial();
            Subscribe();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBuilder = (StreamBuilderBase<T, TSummary>)oldWidget;
            if (ReferenceEquals(oldBuilder.Stream, CurrentWidget.Stream))
            {
                return;
            }

            if (_subscriptionIdentity is not null)
            {
                Unsubscribe();
                _summary = CurrentWidget.AfterDisconnected(_summary);
            }

            Subscribe();
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Build(context, _summary);
        }

        public override void Dispose()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var stream = CurrentWidget.Stream;
            if (stream is null)
            {
                return;
            }

            var identity = new object();
            _subscriptionIdentity = identity;
            _subscription = stream.Subscribe(new StreamObserver(this, identity));
            _summary = CurrentWidget.AfterConnected(_summary);
        }

        private void Unsubscribe()
        {
            _subscriptionIdentity = null;
            _subscription?.Dispose();
            _subscription = null;
        }

        private bool IsCurrent(object identity)
        {
            return Mounted && ReferenceEquals(_subscriptionIdentity, identity);
        }

        private sealed class StreamObserver(StreamBuilderBaseState state, object identity) : IObserver<T>
        {
            public void OnCompleted()
            {
                if (!state.IsCurrent(identity))
                {
                    return;
                }

                state.SetState(() => state._summary = state.CurrentWidget.AfterDone(state._summary));
            }

            public void OnError(Exception error)
            {
                if (!state.IsCurrent(identity))
                {
                    return;
                }

                string stackTrace = error.StackTrace ?? string.Empty;
                state.SetState(() =>
                    state._summary = state.CurrentWidget.AfterError(state._summary, error, stackTrace));
            }

            public void OnNext(T value)
            {
                if (!state.IsCurrent(identity))
                {
                    return;
                }

                state.SetState(() => state._summary = state.CurrentWidget.AfterData(state._summary, value));
            }
        }
    }
}

public sealed class StreamBuilder<T> : StreamBuilderBase<T, AsyncSnapshot<T>>
{
    private readonly bool _hasInitialData;

    public StreamBuilder(
        IObservable<T>? stream,
        AsyncWidgetBuilder<T> builder,
        Key? key = null) : base(stream, key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public StreamBuilder(
        IObservable<T>? stream,
        T initialData,
        AsyncWidgetBuilder<T> builder,
        Key? key = null) : base(stream, key)
    {
        InitialData = initialData;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _hasInitialData = true;
    }

    public AsyncWidgetBuilder<T> Builder { get; }

    public T? InitialData { get; }

    public override AsyncSnapshot<T> Initial()
    {
        return _hasInitialData
            ? AsyncSnapshot<T>.WithData(ConnectionState.None, InitialData!)
            : AsyncSnapshot<T>.Nothing();
    }

    public override AsyncSnapshot<T> AfterConnected(AsyncSnapshot<T> current)
    {
        return current.InState(ConnectionState.Waiting);
    }

    public override AsyncSnapshot<T> AfterData(AsyncSnapshot<T> current, T data)
    {
        return AsyncSnapshot<T>.WithData(ConnectionState.Active, data);
    }

    public override AsyncSnapshot<T> AfterError(
        AsyncSnapshot<T> current,
        Exception error,
        string stackTrace)
    {
        return AsyncSnapshot<T>.WithError(ConnectionState.Active, error, stackTrace);
    }

    public override AsyncSnapshot<T> AfterDone(AsyncSnapshot<T> current)
    {
        return current.InState(ConnectionState.Done);
    }

    public override AsyncSnapshot<T> AfterDisconnected(AsyncSnapshot<T> current)
    {
        return current.InState(ConnectionState.None);
    }

    public override Widget Build(BuildContext context, AsyncSnapshot<T> currentSummary)
    {
        return Builder(context, currentSummary);
    }
}

public sealed class FutureBuilder<T> : StatefulWidget
{
    private readonly bool _hasInitialData;

    public FutureBuilder(
        Task<T>? future,
        AsyncWidgetBuilder<T> builder,
        Key? key = null) : base(key)
    {
        Future = future;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public FutureBuilder(
        Task<T>? future,
        T initialData,
        AsyncWidgetBuilder<T> builder,
        Key? key = null) : base(key)
    {
        Future = future;
        InitialData = initialData;
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _hasInitialData = true;
    }

    public static bool DebugRethrowError { get; set; }

    public Task<T>? Future { get; }

    public AsyncWidgetBuilder<T> Builder { get; }

    public T? InitialData { get; }

    public override State CreateState()
    {
        return new FutureBuilderState();
    }

    private sealed class FutureBuilderState : State
    {
        private object? _activeCallbackIdentity;
        private AsyncSnapshot<T> _snapshot = null!;

        private FutureBuilder<T> CurrentWidget => (FutureBuilder<T>)StateWidget;

        public override void InitState()
        {
            _snapshot = CurrentWidget._hasInitialData
                ? AsyncSnapshot<T>.WithData(ConnectionState.None, CurrentWidget.InitialData!)
                : AsyncSnapshot<T>.Nothing();
            Subscribe();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBuilder = (FutureBuilder<T>)oldWidget;
            if (ReferenceEquals(oldBuilder.Future, CurrentWidget.Future))
            {
                return;
            }

            if (_activeCallbackIdentity is not null)
            {
                Unsubscribe();
                _snapshot = _snapshot.InState(ConnectionState.None);
            }

            Subscribe();
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Builder(context, _snapshot);
        }

        public override void Dispose()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var future = CurrentWidget.Future;
            if (future is null)
            {
                return;
            }

            var callbackIdentity = new object();
            _activeCallbackIdentity = callbackIdentity;
            _ = ObserveFuture(future, callbackIdentity);
            if (_snapshot.ConnectionState != ConnectionState.Done)
            {
                _snapshot = _snapshot.InState(ConnectionState.Waiting);
            }
        }

        private async Task ObserveFuture(Task<T> future, object callbackIdentity)
        {
            await Task.Yield();
            try
            {
                T data = await future;
                if (!Mounted || !ReferenceEquals(_activeCallbackIdentity, callbackIdentity))
                {
                    return;
                }

                SetState(() => _snapshot = AsyncSnapshot<T>.WithData(ConnectionState.Done, data));
            }
            catch (Exception error)
            {
                if (!Mounted || !ReferenceEquals(_activeCallbackIdentity, callbackIdentity))
                {
                    return;
                }

                SetState(() => _snapshot = AsyncSnapshot<T>.WithError(ConnectionState.Done, error));
                if (DebugRethrowError)
                {
                    _ = Task.FromException(error);
                }
            }
        }

        private void Unsubscribe()
        {
            _activeCallbackIdentity = null;
        }
    }
}
