using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AsyncBuilderTests : IDisposable
{
    public AsyncBuilderTests()
    {
        Scheduler.ResetForTests();
        FutureBuilder<int>.DebugRethrowError = false;
    }

    public void Dispose()
    {
        FutureBuilder<int>.DebugRethrowError = false;
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AsyncSnapshot_ExposesSourceFactoriesStateRetentionAndRequireData()
    {
        var nothing = AsyncSnapshot<string>.Nothing();
        var waiting = AsyncSnapshot<string>.Waiting();
        var data = AsyncSnapshot<string>.WithData(ConnectionState.Active, "ready");
        var error = new InvalidOperationException("failed");
        var failed = AsyncSnapshot<string>.WithError(ConnectionState.Done, error, "source stack");

        Assert.Equal(ConnectionState.None, nothing.ConnectionState);
        Assert.False(nothing.HasData);
        Assert.False(nothing.HasError);
        Assert.Equal(ConnectionState.Waiting, waiting.ConnectionState);
        Assert.Equal("ready", data.RequireData);
        Assert.True(data.HasData);
        Assert.Same(error, failed.Error);
        Assert.True(failed.HasError);
        Assert.Equal("source stack", failed.StackTrace);
        Assert.Throws<InvalidOperationException>(() => nothing.RequireData);
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => failed.RequireData));

        var disconnected = data.InState(ConnectionState.None);
        Assert.Equal("ready", disconnected.Data);
        Assert.Equal(ConnectionState.None, disconnected.ConnectionState);
        Assert.Equal(disconnected, AsyncSnapshot<string>.WithData(ConnectionState.None, "ready"));
        Assert.Equal(disconnected.GetHashCode(), AsyncSnapshot<string>.WithData(
            ConnectionState.None,
            "ready").GetHashCode());
    }

    [Fact]
    public void Builders_ExposeSourceContractsAndPreserveExplicitValueTypeInitialData()
    {
        AsyncWidgetBuilder<int> builder = (_, snapshot) => new Text(snapshot.ConnectionState.ToString());
        var stream = new ManualObservable<int>();
        var streamBuilder = new StreamBuilder<int>(stream, 0, builder);
        var future = Task.FromResult(4);
        var futureBuilder = new FutureBuilder<int>(future, 0, builder);

        Assert.Same(stream, streamBuilder.Stream);
        Assert.Same(builder, streamBuilder.Builder);
        Assert.Equal(0, streamBuilder.InitialData);
        Assert.True(streamBuilder.Initial().HasData);
        Assert.Equal(0, streamBuilder.Initial().Data);
        Assert.Same(future, futureBuilder.Future);
        Assert.Same(builder, futureBuilder.Builder);
        Assert.Equal(0, futureBuilder.InitialData);
        Assert.Throws<ArgumentNullException>(() => new StreamBuilder<int>(stream, null!));
        Assert.Throws<ArgumentNullException>(() => new FutureBuilder<int>(future, null!));
    }

    [Fact]
    public void StreamBuilder_TracksWaitingActiveAndDoneSnapshots()
    {
        var stream = new ManualObservable<int>();
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(new StreamBuilder<int>(
            stream,
            initialData: 7,
            builder: (_, snapshot) => Capture(snapshot, snapshots)));
        Mount(root, owner);

        AssertSnapshot(snapshots[^1], ConnectionState.Waiting, data: 7);
        Assert.Equal(1, stream.ListenerCount);

        stream.Emit(11);
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Active, data: 11);

        stream.Complete();
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Done, data: 11);

        root.Unmount();
        Assert.Equal(0, stream.ListenerCount);
    }

    [Fact]
    public void StreamBuilder_ReportsErrorsAndRetainsThemWhenDisconnected()
    {
        var stream = new ManualObservable<int>();
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(stream));
        Mount(root, owner);

        var error = new InvalidOperationException("stream failure");
        stream.Fail(error);
        owner.FlushBuild();
        Assert.Equal(ConnectionState.Active, snapshots[^1].ConnectionState);
        Assert.Same(error, snapshots[^1].Error);
        Assert.True(snapshots[^1].HasError);

        root.Update(Build(null));
        owner.FlushBuild();
        Assert.Equal(ConnectionState.None, snapshots[^1].ConnectionState);
        Assert.Same(error, snapshots[^1].Error);
        root.Unmount();

        StreamBuilder<int> Build(IObservable<int>? source)
        {
            return new StreamBuilder<int>(source, (_, snapshot) => Capture(snapshot, snapshots));
        }
    }

    [Fact]
    public void StreamBuilder_RebindsSubscriptionsAndIgnoresDetachedSource()
    {
        var first = new ManualObservable<int>();
        var second = new ManualObservable<int>();
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first));
        Mount(root, owner);

        first.Emit(3);
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Active, data: 3);

        root.Update(Build(second));
        owner.FlushBuild();
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);
        AssertSnapshot(snapshots[^1], ConnectionState.Waiting, data: 3);

        first.EmitIgnoringDisposal(99);
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Waiting, data: 3);

        second.Emit(8);
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Active, data: 8);
        root.Unmount();

        StreamBuilder<int> Build(IObservable<int> source)
        {
            return new StreamBuilder<int>(source, (_, snapshot) => Capture(snapshot, snapshots));
        }
    }

    [Fact]
    public void StreamBuilderBase_FoldsAllSourcePseudoEventsInDartOrder()
    {
        var first = new ManualObservable<int>();
        var second = new ManualObservable<int>();
        var summaries = new List<string>();
        var owner = new BuildOwner();
        var root = new TestRootElement(new FoldingStreamBuilder(first, summaries));
        Mount(root, owner);

        first.Emit(2);
        owner.FlushBuild();
        first.Complete();
        owner.FlushBuild();
        root.Update(new FoldingStreamBuilder(second, summaries));
        owner.FlushBuild();
        second.Fail(new InvalidOperationException("boom"));
        owner.FlushBuild();

        Assert.Equal("initial|connected|data:2|done|disconnected|connected|error:boom", summaries[^1]);
        root.Unmount();
    }

    [Fact]
    public async Task FutureBuilder_TracksWaitingAndSuccessfulCompletion()
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(new FutureBuilder<int>(
            completion.Task,
            initialData: 5,
            builder: (_, snapshot) => Capture(snapshot, snapshots)));
        Mount(root, owner);

        AssertSnapshot(snapshots[^1], ConnectionState.Waiting, data: 5);
        completion.SetResult(12);
        await FlushUntil(owner, () => snapshots[^1].ConnectionState == ConnectionState.Done);
        AssertSnapshot(snapshots[^1], ConnectionState.Done, data: 12);
        root.Unmount();
    }

    [Fact]
    public async Task FutureBuilder_ReportsCompletionErrors()
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(new FutureBuilder<int>(
            completion.Task,
            (_, snapshot) => Capture(snapshot, snapshots)));
        Mount(root, owner);

        var error = new InvalidOperationException("future failure");
        completion.SetException(error);
        await FlushUntil(owner, () => snapshots[^1].HasError);

        Assert.Equal(ConnectionState.Done, snapshots[^1].ConnectionState);
        Assert.Same(error, snapshots[^1].Error);
        Assert.Contains("ObserveFuture", snapshots[^1].StackTrace);
        root.Unmount();
    }

    [Fact]
    public async Task FutureBuilder_RebindsRetainsDataAndIgnoresStaleCompletion()
    {
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first.Task));
        Mount(root, owner);

        first.SetResult(4);
        await FlushUntil(owner, () => snapshots[^1].ConnectionState == ConnectionState.Done);
        AssertSnapshot(snapshots[^1], ConnectionState.Done, data: 4);

        root.Update(Build(second.Task));
        owner.FlushBuild();
        AssertSnapshot(snapshots[^1], ConnectionState.Waiting, data: 4);

        second.SetResult(9);
        await FlushUntil(owner, () => snapshots[^1].Data == 9);
        AssertSnapshot(snapshots[^1], ConnectionState.Done, data: 9);
        root.Unmount();

        FutureBuilder<int> Build(Task<int> future)
        {
            return new FutureBuilder<int>(future, (_, snapshot) => Capture(snapshot, snapshots));
        }
    }

    [Fact]
    public async Task FutureBuilder_IgnoresOldFutureAfterReplacementAndAfterDispose()
    {
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshots = new List<AsyncSnapshot<int>>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first.Task));
        Mount(root, owner);

        root.Update(Build(second.Task));
        owner.FlushBuild();
        first.SetResult(1);
        await Task.Delay(20);
        owner.FlushBuild();
        Assert.DoesNotContain(snapshots, snapshot => snapshot.Data == 1);

        int buildCount = snapshots.Count;
        root.Unmount();
        second.SetResult(2);
        await Task.Delay(20);
        owner.FlushBuild();
        Assert.Equal(buildCount, snapshots.Count);

        FutureBuilder<int> Build(Task<int> future)
        {
            return new FutureBuilder<int>(future, (_, snapshot) => Capture(snapshot, snapshots));
        }
    }

    private static Widget Capture<T>(AsyncSnapshot<T> snapshot, List<AsyncSnapshot<T>> snapshots)
    {
        snapshots.Add(snapshot);
        return new SizedBox(width: 10, height: 10);
    }

    private static void AssertSnapshot(
        AsyncSnapshot<int> snapshot,
        ConnectionState state,
        int data)
    {
        Assert.Equal(state, snapshot.ConnectionState);
        Assert.Equal(data, snapshot.Data);
        Assert.False(snapshot.HasError);
    }

    private static async Task FlushUntil(BuildOwner owner, Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
            owner.FlushBuild();
        }

        Assert.True(condition(), "Timed out waiting for the asynchronous builder snapshot.");
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class FoldingStreamBuilder : StreamBuilderBase<int, string>
    {
        private readonly List<string> _summaries;

        public FoldingStreamBuilder(IObservable<int>? stream, List<string> summaries) : base(stream)
        {
            _summaries = summaries;
        }

        public override string Initial() => "initial";

        public override string AfterConnected(string current) => current + "|connected";

        public override string AfterData(string current, int data) => current + $"|data:{data}";

        public override string AfterError(string current, Exception error, string stackTrace) =>
            current + $"|error:{error.Message}";

        public override string AfterDone(string current) => current + "|done";

        public override string AfterDisconnected(string current) => current + "|disconnected";

        public override Widget Build(BuildContext context, string currentSummary)
        {
            _summaries.Add(currentSummary);
            return new SizedBox(width: 10, height: 10);
        }
    }

    private sealed class ManualObservable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = [];
        private readonly List<IObserver<T>> _allObservers = [];

        public int ListenerCount => _observers.Count;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            _allObservers.Add(observer);
            return new Subscription(this, observer);
        }

        public void Emit(T value)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        public void EmitIgnoringDisposal(T value)
        {
            foreach (var observer in _allObservers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        public void Fail(Exception error)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnError(error);
            }
        }

        public void Complete()
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnCompleted();
            }
        }

        private sealed class Subscription(ManualObservable<T> owner, IObserver<T> observer) : IDisposable
        {
            public void Dispose()
            {
                owner._observers.Remove(observer);
            }
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
