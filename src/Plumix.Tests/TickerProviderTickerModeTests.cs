using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/ticker_mode_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TickerModeTests : IDisposable
{
    public TickerModeTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void TickerMode_UpdatesTickerWithoutRebuildingItsOwner()
    {
        var key = new LabeledGlobalKey<TickingState>("ticker");
        var child = new TickingWidget(key);
        var owner = new BuildOwner();
        var root = new TestRootElement(new TickerMode(child: child, enabled: true));
        Mount(root, owner);

        TickingState state = Assert.IsType<TickingState>(key.CurrentState);
        Assert.True(state.Ticker.IsTicking);
        Assert.Equal(1, state.BuildCount);

        root.Update(new TickerMode(child: child, enabled: false));
        owner.FlushBuild();

        Assert.False(state.Ticker.IsTicking);
        Assert.True(state.Ticker.IsActive);
        Assert.True(state.Ticker.Muted);
        Assert.Equal(1, state.BuildCount);

        root.Update(new TickerMode(child: child, enabled: true));
        owner.FlushBuild();

        Assert.True(state.Ticker.IsTicking);
        Assert.Equal(1, state.BuildCount);
        root.Unmount();
    }

    [Fact]
    public void TickerMode_NestedEnabledAndForceFramesUseFlutterSemantics()
    {
        var outerKey = new LabeledGlobalKey<TickingState>("outer");
        var innerKey = new LabeledGlobalKey<TickingState>("inner");
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildNestedModes(
            outerKey,
            innerKey,
            outerEnabled: false,
            innerEnabled: true,
            outerForceFrames: true,
            innerForceFrames: false));
        Mount(root, owner);

        TickingState outerState = Assert.IsType<TickingState>(outerKey.CurrentState);
        TickingState innerState = Assert.IsType<TickingState>(innerKey.CurrentState);
        Assert.False(outerState.Ticker.IsTicking);
        Assert.False(innerState.Ticker.IsTicking);
        Assert.True(outerState.Ticker.ForceFrames);
        Assert.True(innerState.Ticker.ForceFrames);

        root.Update(BuildNestedModes(
            outerKey,
            innerKey,
            outerEnabled: true,
            innerEnabled: false,
            outerForceFrames: false,
            innerForceFrames: true));
        owner.FlushBuild();

        Assert.True(outerState.Ticker.IsTicking);
        Assert.False(innerState.Ticker.IsTicking);
        Assert.False(outerState.Ticker.ForceFrames);
        Assert.True(innerState.Ticker.ForceFrames);
        root.Unmount();
    }

    [Fact]
    public void TickerMode_MergeValuesAndNotifierMatchFlutter()
    {
        IValueListenable<TickerModeData>? notifier = null;
        TickerModeData? values = null;
        var probe = new Builder(context =>
        {
            values = TickerMode.ValuesOf(context);
            notifier ??= TickerMode.GetValuesNotifier(context);
            return new SizedBox();
        });
        Widget merged = TickerMode.Merge(child: probe, enabled: true, forceFrames: true);
        var owner = new BuildOwner();
        var root = new TestRootElement(new TickerMode(child: merged, enabled: false));
        Mount(root, owner);

        Assert.Equal(new TickerModeData(Enabled: false, ForceFrames: true), values);
        Assert.NotNull(notifier);
        Assert.Equal(values, notifier!.Value);

        var notifications = new List<TickerModeData>();
        notifier.AddListener(() => notifications.Add(notifier.Value));
        root.Update(new TickerMode(
            child: TickerMode.Merge(child: probe, enabled: true, forceFrames: false),
            enabled: true));
        owner.FlushBuild();

        Assert.Equal(new TickerModeData(Enabled: true, ForceFrames: false), values);
        Assert.Contains(new TickerModeData(Enabled: true, ForceFrames: false), notifications);
        root.Unmount();
    }

    [Fact]
    public void TickerMode_FallbackAndDeprecatedAccessorsMatchFlutter()
    {
        TickerModeData? values = null;
        bool? enabled = null;
        IValueListenable<TickerModeData>? valuesNotifier = null;
        IValueListenable<bool>? enabledNotifier = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Builder(context =>
        {
            values = TickerMode.ValuesOf(context);
            enabled = TickerMode.Of(context);
            valuesNotifier = TickerMode.GetValuesNotifier(context);
            enabledNotifier = TickerMode.GetNotifier(context);
            return new SizedBox();
        }));
        Mount(root, owner);

        Assert.Equal(TickerModeData.Fallback, values);
        Assert.True(enabled);
        Assert.Equal(TickerModeData.Fallback, valuesNotifier!.Value);
        Assert.True(enabledNotifier!.Value);
        Assert.Equal(TickerModeData.Fallback, new TickerModeData(Enabled: true, ForceFrames: false));
        root.Unmount();
    }

    [Fact]
    public void TickerMode_MutesCallbacksWhileElapsedTimeContinues()
    {
        var key = new LabeledGlobalKey<TickingState>("ticker");
        var child = new TickingWidget(key);
        var owner = new BuildOwner();
        var root = new TestRootElement(new TickerMode(child: child, enabled: false));
        Mount(root, owner);
        TickingState state = Assert.IsType<TickingState>(key.CurrentState);
        double now = Scheduler.CurrentSeconds;

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        Assert.Equal(0, state.TickCount);

        root.Update(new TickerMode(child: child, enabled: true));
        owner.FlushBuild();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 2.0));

        Assert.Equal(1, state.TickCount);
        Assert.True(state.LastElapsed >= TimeSpan.FromSeconds(1.9));
        root.Unmount();
    }

    [Fact]
    public void TickerMode_MutesDescendantAnimationController()
    {
        int builds = 0;
        var animation = new RepeatingAnimationBuilder<double>(
            duration: TimeSpan.FromSeconds(1),
            animatable: new DoubleTween(begin: 0.0, end: 1.0),
            builder: (_, _, _) =>
            {
                builds++;
                return new SizedBox();
            });
        var owner = new BuildOwner();
        var root = new TestRootElement(new TickerMode(child: animation, enabled: false));
        Mount(root, owner);
        int initialBuilds = builds;
        double now = Scheduler.CurrentSeconds;

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        owner.FlushBuild();
        Assert.Equal(initialBuilds, builds);

        root.Update(new TickerMode(child: animation, enabled: true));
        owner.FlushBuild();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        owner.FlushBuild();

        Assert.True(builds > initialBuilds);
        root.Unmount();
    }

    [Fact]
    public void TickerMode_ReparentedStateSubscribesToNewAncestor()
    {
        var childKey = new LabeledGlobalKey<TickingState>("ticker");
        var child = new TickingWidget(childKey);
        var owner = new BuildOwner();
        var root = new TestRootElement(new TickerMode(
            child: child,
            enabled: true,
            key: new ValueKey<int>(1)));
        Mount(root, owner);
        TickingState state = Assert.IsType<TickingState>(childKey.CurrentState);

        root.Update(new TickerMode(
            child: child,
            enabled: false,
            key: new ValueKey<int>(2)));
        owner.FlushBuild();

        Assert.Same(state, childKey.CurrentState);
        Assert.False(state.Ticker.IsTicking);
        root.Unmount();
    }

    private static Widget BuildNestedModes(
        GlobalKey outerKey,
        GlobalKey innerKey,
        bool outerEnabled,
        bool innerEnabled,
        bool outerForceFrames,
        bool innerForceFrames)
    {
        return new TickerMode(
            enabled: outerEnabled,
            forceFrames: outerForceFrames,
            child: new Row(children:
            [
                new TickingWidget(outerKey),
                new TickerMode(
                    child: new TickingWidget(innerKey),
                    enabled: innerEnabled,
                    forceFrames: innerForceFrames)
            ]));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TickingWidget : StatefulWidget
    {
        public TickingWidget(Key key) : base(key)
        {
        }

        public override State CreateState() => new TickingState();
    }

    private sealed class TickingState : State
    {
        public Ticker Ticker { get; private set; } = null!;

        public int BuildCount { get; private set; }

        public int TickCount { get; private set; }

        public TimeSpan LastElapsed { get; private set; }

        public override void InitState()
        {
            base.InitState();
            Ticker = CreateTicker(elapsed =>
            {
                TickCount++;
                LastElapsed = elapsed;
            });
            Ticker.Start();
        }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            return new SizedBox();
        }

        public override void Dispose()
        {
            Ticker.Dispose();
            base.Dispose();
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

        internal override void Unmount()
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
