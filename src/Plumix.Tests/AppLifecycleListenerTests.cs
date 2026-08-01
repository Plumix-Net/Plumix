using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class AppLifecycleListenerTests
{
    [Fact]
    public void Listener_DispatchesSourceStateMachineCallbacksInOrder()
    {
        var binding = new WidgetsBinding();
        var events = new List<string>();
        using var listener = new AppLifecycleListener(
            binding: binding,
            onResume: () => events.Add("resume"),
            onInactive: () => events.Add("inactive"),
            onHide: () => events.Add("hide"),
            onShow: () => events.Add("show"),
            onPause: () => events.Add("pause"),
            onRestart: () => events.Add("restart"),
            onDetach: () => events.Add("detach"),
            onStateChange: state => events.Add($"state:{state}"));

        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Detached);

        Assert.Equal(
            [
                "resume",
                "state:Resumed",
                "inactive",
                "state:Inactive",
                "hide",
                "state:Hidden",
                "pause",
                "state:Paused",
                "restart",
                "state:Hidden",
                "show",
                "state:Inactive",
                "resume",
                "state:Resumed",
                "inactive",
                "state:Inactive",
                "hide",
                "state:Hidden",
                "pause",
                "state:Paused",
                "detach",
                "state:Detached",
            ],
            events);
    }

    [Fact]
    public void Binding_SuppressesDuplicateStatesAndListenerDisposeStopsNotifications()
    {
        var binding = new WidgetsBinding();
        int changes = 0;
        var listener = new AppLifecycleListener(
            binding: binding,
            onStateChange: _ => changes++);

        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        Assert.Equal(1, changes);

        listener.Dispose();
        binding.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
        Assert.Equal(1, changes);
        Assert.Throws<ObjectDisposedException>(listener.Dispose);
    }

    [Fact]
    public async Task ExitRequest_AsksEveryObserverAndAnyCancelResponseWins()
    {
        var binding = new WidgetsBinding();
        int exitRequests = 0;
        using var cancelingListener = new AppLifecycleListener(
            binding: binding,
            onExitRequested: () =>
            {
                exitRequests++;
                return Task.FromResult(AppExitResponse.Cancel);
            });
        using var exitingListener = new AppLifecycleListener(
            binding: binding,
            onExitRequested: () =>
            {
                exitRequests++;
                return Task.FromResult(AppExitResponse.Exit);
            });

        AppExitResponse response = await binding.HandleRequestAppExit();

        Assert.Equal(AppExitResponse.Cancel, response);
        Assert.Equal(2, exitRequests);
    }

    [Fact]
    public void AndroidLifecycleChannel_CombinesActivityStateAndWindowFocusLikeFlutterEngine()
    {
        var states = new List<AppLifecycleState>();
        var channel = new AndroidLifecycleChannel(states.Add);

        channel.AppIsResumed();
        channel.NoWindowsAreFocused();
        channel.AWindowIsFocused();
        channel.AppIsInactive();
        channel.NoWindowsAreFocused();
        channel.AppIsResumed();
        channel.AppIsPaused();
        channel.AWindowIsFocused();
        channel.NoWindowsAreFocused();
        channel.AppIsResumed();
        channel.AWindowIsFocused();
        channel.AppIsDetached();

        Assert.Equal(
            [
                AppLifecycleState.Resumed,
                AppLifecycleState.Inactive,
                AppLifecycleState.Resumed,
                AppLifecycleState.Inactive,
                AppLifecycleState.Paused,
                AppLifecycleState.Inactive,
                AppLifecycleState.Resumed,
                AppLifecycleState.Detached,
            ],
            states);
    }

    [Fact]
    public void DisposableBuildContext_ReturnsContextUntilExplicitlyDisposed()
    {
        var widget = new ContextOwnerWidget(disposeHandle: true);
        var root = Mount(widget);
        var state = Assert.IsType<ContextOwnerState>(widget.CreatedState);

        Assert.True(state.Mounted);
        Assert.NotNull(state.Handle!.Context);

        root.Unmount();

        Assert.False(state.Mounted);
        Assert.Null(state.Handle.Context);
        Assert.Throws<InvalidOperationException>(() => _ = state.Context);
    }

    [Fact]
    public void DisposableBuildContext_DetectsOwnerThatFailedToDisposeIt()
    {
        var widget = new ContextOwnerWidget(disposeHandle: false);
        var root = Mount(widget);
        var state = Assert.IsType<ContextOwnerState>(widget.CreatedState);

        root.Unmount();

        Assert.Throws<InvalidOperationException>(() => _ = state.Handle!.Context);
        state.Handle!.Dispose();
        Assert.Null(state.Handle.Context);
    }

    [Fact]
    public void DisposableBuildContext_RequiresMountedState()
    {
        var state = new ContextOwnerState(disposeHandle: true);

        Assert.Throws<ArgumentException>(() => new DisposableBuildContext<ContextOwnerState>(state));
    }

    [Fact]
    public void StatusTransitionWidget_RebuildsOnlyForStatusChangesAndRebindsAnimation()
    {
        var firstAnimation = new TrackingAnimation();
        var secondAnimation = new TrackingAnimation();
        int buildCount = 0;
        var root = Mount(new TestStatusTransition(firstAnimation, () => buildCount++));

        Assert.Equal(1, buildCount);
        Assert.Equal(1, firstAnimation.StatusListenerCount);

        firstAnimation.SetValue(0.5);
        Assert.Equal(1, buildCount);

        firstAnimation.SetStatus(AnimationStatus.Forward);
        root.OwnerForTest.FlushBuild();
        Assert.Equal(2, buildCount);

        root.Update(new TestStatusTransition(secondAnimation, () => buildCount++));
        root.OwnerForTest.FlushBuild();
        Assert.Equal(0, firstAnimation.StatusListenerCount);
        Assert.Equal(1, secondAnimation.StatusListenerCount);

        firstAnimation.SetStatus(AnimationStatus.Completed);
        root.OwnerForTest.FlushBuild();
        Assert.Equal(3, buildCount);

        secondAnimation.SetStatus(AnimationStatus.Reverse);
        root.OwnerForTest.FlushBuild();
        Assert.Equal(4, buildCount);

        root.Unmount();
        Assert.Equal(0, secondAnimation.StatusListenerCount);
    }

    private static TestRootElement Mount(Widget widget)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return root;
    }

    private sealed class ContextOwnerWidget(bool disposeHandle) : StatefulWidget
    {
        public State? CreatedState { get; private set; }

        public override State CreateState()
        {
            CreatedState = new ContextOwnerState(disposeHandle);
            return CreatedState;
        }
    }

    private sealed class ContextOwnerState(bool disposeHandle) : State
    {
        public DisposableBuildContext<ContextOwnerState>? Handle { get; private set; }

        public override void InitState()
        {
            Handle = new DisposableBuildContext<ContextOwnerState>(this);
        }

        public override void Dispose()
        {
            if (disposeHandle)
            {
                Handle?.Dispose();
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class TestStatusTransition(
        Animation<double> animation,
        Action onBuild) : StatusTransitionWidget(animation)
    {
        public override Widget Build(BuildContext context)
        {
            onBuild();
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class TrackingAnimation : Animation<double>
    {
        private event Action? ValueChanged;
        private event Action<AnimationStatus>? StatusChanged;
        private double _value;
        private AnimationStatus _status = AnimationStatus.Dismissed;

        public override double Value => _value;
        public override AnimationStatus Status => _status;
        public int StatusListenerCount => StatusChanged?.GetInvocationList().Length ?? 0;

        public void SetValue(double value)
        {
            _value = value;
            ValueChanged?.Invoke();
        }

        public void SetStatus(AnimationStatus status)
        {
            _status = status;
            StatusChanged?.Invoke(status);
        }

        public override void AddListener(Action listener)
        {
            ValueChanged += listener;
        }

        public override void RemoveListener(Action listener)
        {
            ValueChanged -= listener;
        }

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
            StatusChanged += listener;
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            StatusChanged -= listener;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;
        public BuildOwner OwnerForTest { get; private set; } = null!;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            OwnerForTest = Owner!;
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
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

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
