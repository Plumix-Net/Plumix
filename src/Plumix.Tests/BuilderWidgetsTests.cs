using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class BuilderWidgetsTests : IDisposable
{
    public BuilderWidgetsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ListenableAndAnimatedBuilder_ExposeSourceContractsAndValidateArguments()
    {
        var listenable = new TrackingListenable();
        var child = new SizedBox(width: 10, height: 10);
        TransitionBuilder builder = (_, passedChild) => passedChild ?? new SizedBox();
        var listenableBuilder = new ListenableBuilder(
            listenable: listenable,
            builder: builder,
            child: child);
        var animatedBuilder = new AnimatedBuilder(
            animation: listenable,
            builder: builder,
            child: child);

        Assert.Same(listenable, listenableBuilder.Listenable);
        Assert.Same(builder, listenableBuilder.Builder);
        Assert.Same(child, listenableBuilder.Child);
        Assert.Same(listenable, animatedBuilder.Animation);
        Assert.Same(listenable, animatedBuilder.Listenable);
        Assert.Same(builder, animatedBuilder.Builder);
        Assert.Same(child, animatedBuilder.Child);
        Assert.Throws<ArgumentNullException>(() => new ListenableBuilder(null!, builder));
        Assert.Throws<ArgumentNullException>(() => new ListenableBuilder(listenable, null!));
        Assert.Throws<ArgumentNullException>(() => new AnimatedBuilder(null!, builder));
        Assert.Throws<ArgumentNullException>(() => new AnimatedBuilder(listenable, null!));
    }

    [Fact]
    public void ListenableBuilder_RebuildsOnNotifyRebindsAndPreservesChildSubtree()
    {
        var first = new TrackingListenable();
        var second = new TrackingListenable();
        var child = new BuildCounterWidget();
        var passedChildren = new List<Widget?>();
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first));
        Mount(root, owner);

        Assert.Equal(1, builderCalls);
        Assert.Equal(1, child.BuildCount);
        Assert.Same(child, Assert.Single(passedChildren));
        Assert.Equal(1, first.ListenerCount);

        owner.FlushBuild();
        Assert.Equal(1, builderCalls);
        Assert.Equal(1, child.BuildCount);

        first.Notify();
        owner.FlushBuild();
        Assert.Equal(2, builderCalls);
        Assert.Equal(1, child.BuildCount);
        Assert.Same(child, passedChildren[^1]);

        root.Update(Build(second));
        owner.FlushBuild();
        Assert.Equal(3, builderCalls);
        Assert.Equal(1, child.BuildCount);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        first.Notify();
        owner.FlushBuild();
        Assert.Equal(3, builderCalls);

        second.Notify();
        owner.FlushBuild();
        Assert.Equal(4, builderCalls);
        Assert.Equal(1, child.BuildCount);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);

        ListenableBuilder Build(IListenable listenable)
        {
            return new ListenableBuilder(
                listenable: listenable,
                child: child,
                builder: (_, passedChild) =>
                {
                    builderCalls++;
                    passedChildren.Add(passedChild);
                    return passedChild ?? new SizedBox();
                });
        }
    }

    [Fact]
    public void AnimatedBuilder_RebuildsOnAnimationNotificationsWithoutRebuildingChild()
    {
        var animation = new TrackingListenable();
        var child = new BuildCounterWidget();
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedBuilder(
            animation: animation,
            child: child,
            builder: (_, passedChild) =>
            {
                builderCalls++;
                return passedChild ?? new SizedBox();
            }));
        Mount(root, owner);

        Assert.Equal(1, builderCalls);
        Assert.Equal(1, child.BuildCount);

        animation.Notify();
        owner.FlushBuild();
        Assert.Equal(2, builderCalls);
        Assert.Equal(1, child.BuildCount);

        owner.FlushBuild();
        Assert.Equal(2, builderCalls);
        Assert.Equal(1, child.BuildCount);

        root.Unmount();
        Assert.Equal(0, animation.ListenerCount);
    }

    [Fact]
    public void ValueListenableBuilder_ExposesSourceContractAndValidatesArguments()
    {
        var listenable = new TrackingValueListenable<int>(4);
        var child = new SizedBox(width: 10, height: 10);
        ValueWidgetBuilder<int> builder = (_, value, _) => new Text(value.ToString());
        var widget = new ValueListenableBuilder<int>(
            valueListenable: listenable,
            builder: builder,
            child: child);

        Assert.Same(listenable, widget.ValueListenable);
        Assert.Same(builder, widget.Builder);
        Assert.Same(child, widget.Child);
        Assert.Throws<ArgumentNullException>(() => new ValueListenableBuilder<int>(null!, builder));
        Assert.Throws<ArgumentNullException>(() => new ValueListenableBuilder<int>(listenable, null!));
    }

    [Fact]
    public void ValueListenableBuilder_TracksValuesRebindsAndPreservesChildIdentity()
    {
        var first = new TrackingValueListenable<int>(4);
        var second = new TrackingValueListenable<int>(9);
        var child = new SizedBox(width: 10, height: 10);
        var values = new List<int>();
        var passedChildren = new List<Widget?>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first));
        Mount(root, owner);

        Assert.Equal([4], values);
        Assert.Same(child, Assert.Single(passedChildren));
        Assert.Equal(1, first.ListenerCount);

        first.SetValue(7);
        owner.FlushBuild();
        Assert.Equal([4, 7], values);
        Assert.Same(child, passedChildren[^1]);

        root.Update(Build(second));
        owner.FlushBuild();
        Assert.Equal([4, 7, 9], values);
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(1, second.ListenerCount);

        first.SetValue(12);
        owner.FlushBuild();
        Assert.Equal([4, 7, 9], values);

        second.SetValue(11);
        owner.FlushBuild();
        Assert.Equal([4, 7, 9, 11], values);

        root.Unmount();
        Assert.Equal(0, second.ListenerCount);

        ValueListenableBuilder<int> Build(IValueListenable<int> listenable)
        {
            return new ValueListenableBuilder<int>(
                valueListenable: listenable,
                child: child,
                builder: (_, value, passedChild) =>
                {
                    values.Add(value);
                    passedChildren.Add(passedChild);
                    return new Text(value.ToString());
                });
        }
    }

    [Fact]
    public void TweenAnimationBuilder_ExposesSourceDefaultsAndValidatesArguments()
    {
        var tween = new DoubleTween(begin: 2.0, end: 8.0);
        var child = new SizedBox(width: 10, height: 10);
        ValueWidgetBuilder<double> builder = (_, value, _) => new Text(value.ToString("F1"));
        var widget = new TweenAnimationBuilder<double>(
            tween: tween,
            duration: TimeSpan.FromMilliseconds(200),
            builder: builder,
            child: child);

        Assert.Same(tween, widget.Tween);
        Assert.Equal(TimeSpan.FromMilliseconds(200), widget.Duration);
        Assert.Equal(Curves.Linear(0.3), widget.Curve(0.3));
        Assert.Same(builder, widget.Builder);
        Assert.Null(widget.OnEnd);
        Assert.Same(child, widget.Child);
        Assert.Equal(2.0, tween.Begin);
        Assert.Equal(8.0, tween.End);
        Assert.Null(new DoubleTween().Begin);
        Assert.Null(new DoubleTween().End);
        Assert.Throws<ArgumentNullException>(() => new TweenAnimationBuilder<double>(
            tween: null!,
            duration: TimeSpan.Zero,
            builder: builder));
        Assert.Throws<ArgumentNullException>(() => new TweenAnimationBuilder<double>(
            tween: tween,
            duration: TimeSpan.Zero,
            builder: null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TweenAnimationBuilder<double>(
            tween: tween,
            duration: TimeSpan.FromMilliseconds(-1),
            builder: builder));
        Assert.Throws<ArgumentException>(() => new TweenAnimationBuilder<double>(
            tween: new DoubleTween(begin: 1.0),
            duration: TimeSpan.Zero,
            builder: builder));
    }

    [Fact]
    public void TweenAnimationBuilder_AnimatesOwnsTweenAndContinuesFromInterruptedValue()
    {
        var firstTween = new DoubleTween(begin: 0.0, end: 100.0);
        var child = new SizedBox(width: 10, height: 10);
        var values = new List<double>();
        var passedChildren = new List<Widget?>();
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(firstTween, Curves.Linear));
        Mount(root, owner);

        Assert.Equal(0.0, Assert.Single(values), precision: 6);
        Assert.Same(child, Assert.Single(passedChildren));

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        double halfway = values[^1];
        Assert.InRange(halfway, 0.01, 99.99);

        var replacement = new DoubleTween(begin: -500.0, end: 200.0);
        root.Update(Build(replacement, Curves.Linear));
        owner.FlushBuild();

        Assert.Equal(halfway, values[^1], precision: 6);
        Assert.Equal(halfway, firstTween.Begin!.Value, precision: 6);
        Assert.Equal(200.0, firstTween.End);
        Assert.Equal(-500.0, replacement.Begin);
        Assert.Equal(200.0, replacement.End);
        Assert.Same(child, passedChildren[^1]);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Assert.Equal(200.0, values[^1], precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();

        TweenAnimationBuilder<double> Build(DoubleTween tween, Curve curve)
        {
            return new TweenAnimationBuilder<double>(
                tween: tween,
                duration: TimeSpan.FromMilliseconds(200),
                curve: curve,
                onEnd: () => completed++,
                child: child,
                builder: (_, value, passedChild) =>
                {
                    values.Add(value);
                    passedChildren.Add(passedChild);
                    return new Text(value.ToString("F1"));
                });
        }
    }

    [Fact]
    public void TweenAnimationBuilder_MissingBeginStartsAtEndWithoutAnimation()
    {
        var tween = new DoubleTween(end: 42.0);
        var values = new List<double>();
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new TweenAnimationBuilder<double>(
            tween: tween,
            duration: TimeSpan.FromMilliseconds(200),
            onEnd: () => completed++,
            builder: (_, value, _) =>
            {
                values.Add(value);
                return new Text(value.ToString("F1"));
            }));
        Mount(root, owner);

        Assert.Equal(42.0, Assert.Single(values));
        Assert.Equal(42.0, tween.Begin);
        Assert.Equal(42.0, tween.End);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Assert.Single(values);
        Assert.Equal(0, completed);

        root.Unmount();
    }

    [Fact]
    public void DualTransitionBuilder_ExposesSourceContractAndValidatesArguments()
    {
        var animation = new TrackingAnimation(0.25, AnimationStatus.Forward);
        var child = new SizedBox(width: 10, height: 10);
        AnimatedTransitionBuilder forwardBuilder = (_, _, passedChild) => passedChild ?? new SizedBox();
        AnimatedTransitionBuilder reverseBuilder = (_, _, passedChild) => passedChild ?? new SizedBox();
        var widget = new DualTransitionBuilder(
            animation: animation,
            forwardBuilder: forwardBuilder,
            reverseBuilder: reverseBuilder,
            child: child);

        Assert.Same(animation, widget.Animation);
        Assert.Same(forwardBuilder, widget.ForwardBuilder);
        Assert.Same(reverseBuilder, widget.ReverseBuilder);
        Assert.Same(child, widget.Child);
        Assert.Throws<ArgumentNullException>(() => new DualTransitionBuilder(
            animation: null!,
            forwardBuilder: forwardBuilder,
            reverseBuilder: reverseBuilder));
        Assert.Throws<ArgumentNullException>(() => new DualTransitionBuilder(
            animation: animation,
            forwardBuilder: null!,
            reverseBuilder: reverseBuilder));
        Assert.Throws<ArgumentNullException>(() => new DualTransitionBuilder(
            animation: animation,
            forwardBuilder: forwardBuilder,
            reverseBuilder: null!));
    }

    [Fact]
    public void ProxyAndReverseAnimation_RelayValuesStatusesAndParentChanges()
    {
        var first = new TrackingAnimation(0.2, AnimationStatus.Forward);
        var second = new TrackingAnimation(0.7, AnimationStatus.Reverse);
        var proxy = new ProxyAnimation(first);
        int valueNotifications = 0;
        var statuses = new List<AnimationStatus>();
        Action valueListener = () => valueNotifications++;
        Action<AnimationStatus> statusListener = status => statuses.Add(status);
        proxy.AddListener(valueListener);
        proxy.AddStatusListener(statusListener);

        Assert.Equal(0.2, proxy.Value, precision: 6);
        Assert.Equal(AnimationStatus.Forward, proxy.Status);
        Assert.Equal(1, first.ListenerCount);
        Assert.Equal(1, first.StatusListenerCount);

        first.Set(0.4, AnimationStatus.Completed);
        Assert.Equal(1, valueNotifications);
        Assert.Equal([AnimationStatus.Completed], statuses);

        proxy.Parent = second;
        Assert.Equal(0, first.ListenerCount);
        Assert.Equal(0, first.StatusListenerCount);
        Assert.Equal(1, second.ListenerCount);
        Assert.Equal(1, second.StatusListenerCount);
        Assert.Equal(0.7, proxy.Value, precision: 6);
        Assert.Equal(AnimationStatus.Reverse, proxy.Status);
        Assert.Equal(2, valueNotifications);
        Assert.Equal([AnimationStatus.Completed, AnimationStatus.Reverse], statuses);

        var reversed = new ReverseAnimation(proxy);
        Assert.Equal(0.3, reversed.Value, precision: 6);
        Assert.Equal(AnimationStatus.Forward, reversed.Status);
        var reversedStatuses = new List<AnimationStatus>();
        Action<AnimationStatus> reversedStatusListener = status => reversedStatuses.Add(status);
        reversed.AddStatusListener(reversedStatusListener);
        second.Set(0.9, AnimationStatus.Forward);
        Assert.Equal(0.1, reversed.Value, precision: 6);
        Assert.Equal(AnimationStatus.Reverse, reversed.Status);
        Assert.Equal([AnimationStatus.Reverse], reversedStatuses);

        reversed.RemoveStatusListener(reversedStatusListener);
        proxy.RemoveListener(valueListener);
        proxy.RemoveStatusListener(statusListener);
        Assert.Equal(0, second.ListenerCount);
        Assert.Equal(0, second.StatusListenerCount);
    }

    [Fact]
    public void DualTransitionBuilder_NestsTransitionsAndKeepsInterruptedDirection()
    {
        var first = new TrackingAnimation(0.25, AnimationStatus.Forward);
        var second = new TrackingAnimation(0.8, AnimationStatus.Completed);
        var child = new SizedBox(width: 10, height: 10);
        var buildOrder = new List<string>();
        Animation<double>? forwardAnimation = null;
        Animation<double>? reverseAnimation = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(first));
        Mount(root, owner);

        Assert.Equal(["reverse", "forward"], buildOrder);
        Assert.NotNull(forwardAnimation);
        Assert.NotNull(reverseAnimation);
        Assert.Equal(0.25, forwardAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Forward, forwardAnimation.Status);
        Assert.Equal(0.0, reverseAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Dismissed, reverseAnimation.Status);
        Assert.Equal(1, first.StatusListenerCount);

        first.Set(0.6, AnimationStatus.Reverse);
        Assert.Equal(0.6, forwardAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Reverse, forwardAnimation.Status);
        Assert.Equal(0.0, reverseAnimation.Value, precision: 6);

        first.Set(1.0, AnimationStatus.Completed);
        Assert.Equal(1.0, forwardAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Completed, forwardAnimation.Status);
        Assert.Equal(0.0, reverseAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Dismissed, reverseAnimation.Status);

        first.Set(0.8, AnimationStatus.Reverse);
        Assert.Equal(1.0, forwardAnimation.Value, precision: 6);
        Assert.Equal(0.2, reverseAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Forward, reverseAnimation.Status);

        first.Set(0.7, AnimationStatus.Forward);
        Assert.Equal(1.0, forwardAnimation.Value, precision: 6);
        Assert.Equal(0.3, reverseAnimation.Value, precision: 6);
        Assert.Equal(AnimationStatus.Reverse, reverseAnimation.Status);

        buildOrder.Clear();
        root.Update(Build(second));
        owner.FlushBuild();
        Assert.Equal(["reverse", "forward"], buildOrder);
        Assert.Equal(0, first.StatusListenerCount);
        Assert.Equal(1, second.StatusListenerCount);
        Assert.Equal(1.0, forwardAnimation.Value, precision: 6);
        Assert.Equal(0.2, reverseAnimation.Value, precision: 6);

        root.Unmount();
        Assert.Equal(0, second.StatusListenerCount);

        DualTransitionBuilder Build(Animation<double> animation)
        {
            return new DualTransitionBuilder(
                animation: animation,
                child: child,
                reverseBuilder: (_, proxy, passedChild) =>
                {
                    buildOrder.Add("reverse");
                    reverseAnimation = proxy;
                    return passedChild ?? new SizedBox();
                },
                forwardBuilder: (_, proxy, reverseTransition) =>
                {
                    buildOrder.Add("forward");
                    forwardAnimation = proxy;
                    return reverseTransition ?? new SizedBox();
                });
        }
    }

    [Fact]
    public void RepeatingAnimationBuilder_ExposesSourceDefaultsAndValidatesArguments()
    {
        var tween = new DoubleTween(begin: 2.0, end: 8.0);
        var child = new SizedBox(width: 10, height: 10);
        ValueWidgetBuilder<double> builder = (_, value, _) => new Text(value.ToString("F1"));
        var widget = new RepeatingAnimationBuilder<double>(
            animatable: tween,
            duration: TimeSpan.FromMilliseconds(300),
            builder: builder,
            child: child);

        Assert.Same(tween, widget.Animatable);
        Assert.Equal(TimeSpan.FromMilliseconds(300), widget.Duration);
        Assert.Equal(Curves.Linear(0.3), widget.Curve(0.3));
        Assert.Same(builder, widget.Builder);
        Assert.Equal(RepeatMode.Restart, widget.RepeatMode);
        Assert.False(widget.Paused);
        Assert.Same(child, widget.Child);
        Assert.Throws<ArgumentNullException>(() => new RepeatingAnimationBuilder<double>(
            animatable: null!,
            duration: TimeSpan.FromMilliseconds(300),
            builder: builder));
        Assert.Throws<ArgumentNullException>(() => new RepeatingAnimationBuilder<double>(
            animatable: tween,
            duration: TimeSpan.FromMilliseconds(300),
            builder: null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeatingAnimationBuilder<double>(
            animatable: tween,
            duration: TimeSpan.Zero,
            builder: builder));
    }

    [Fact]
    public void RepeatingAnimationBuilder_RestartsPausesAndPreservesStableChild()
    {
        var tween = new DoubleTween(begin: 0.0, end: 100.0);
        var child = new BuildCounterWidget();
        var values = new List<double>();
        var passedChildren = new List<Widget?>();
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(paused: false));
        Mount(root, owner);

        Assert.Equal(0.0, Assert.Single(values), precision: 6);
        Assert.Same(child, Assert.Single(passedChildren));
        Assert.Equal(1, child.BuildCount);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        owner.FlushBuild();
        Assert.InRange(values[^1], 20.0, 30.0);
        Assert.Equal(1, child.BuildCount);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.10));
        owner.FlushBuild();
        Assert.InRange(values[^1], 5.0, 15.0);

        root.Update(Build(paused: true));
        owner.FlushBuild();
        int pausedBuildCount = values.Count;
        double pausedValue = values[^1];
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 2.10));
        owner.FlushBuild();
        Assert.Equal(pausedBuildCount, values.Count);
        Assert.Equal(pausedValue, values[^1], precision: 6);
        Assert.Equal(1, child.BuildCount);

        root.Update(Build(paused: false));
        owner.FlushBuild();
        int resumedBuildCount = values.Count;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 2.35));
        owner.FlushBuild();
        Assert.True(values.Count > resumedBuildCount);
        Assert.Equal(1, child.BuildCount);

        root.Unmount();

        RepeatingAnimationBuilder<double> Build(bool paused)
        {
            return new RepeatingAnimationBuilder<double>(
                animatable: tween,
                duration: TimeSpan.FromSeconds(1),
                paused: paused,
                child: child,
                builder: (_, value, passedChild) =>
                {
                    values.Add(value);
                    passedChildren.Add(passedChild);
                    return passedChild ?? new SizedBox();
                });
        }
    }

    [Fact]
    public void RepeatingAnimationBuilder_ReverseModePingPongsAndAppliesCurve()
    {
        var values = new List<double>();
        var owner = new BuildOwner();
        var root = new TestRootElement(new RepeatingAnimationBuilder<double>(
            animatable: new DoubleTween(begin: 0.0, end: 1.0),
            duration: TimeSpan.FromSeconds(1),
            curve: Curves.EaseIn,
            repeatMode: RepeatMode.Reverse,
            builder: (_, value, _) =>
            {
                values.Add(value);
                return new SizedBox(width: 10, height: 10);
            }));
        Mount(root, owner);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.50));
        owner.FlushBuild();
        Assert.InRange(values[^1], 0.30, 0.33);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.25));
        owner.FlushBuild();
        Assert.InRange(values[^1], 0.60, 0.65);

        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.75));
        owner.FlushBuild();
        Assert.InRange(values[^1], 0.05, 0.10);

        root.Unmount();
    }

    [Fact]
    public void RepeatingAnimationBuilder_UpdatesDurationCurveAndRepeatModeInPlace()
    {
        var values = new List<double>();
        var tween = new DoubleTween(begin: 0.0, end: 1.0);
        var owner = new BuildOwner();
        var root = new TestRootElement(Build(
            duration: TimeSpan.FromSeconds(1),
            curve: Curves.Linear,
            repeatMode: RepeatMode.Restart));
        Mount(root, owner);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        owner.FlushBuild();
        Assert.InRange(values[^1], 0.20, 0.30);

        root.Update(Build(
            duration: TimeSpan.FromSeconds(2),
            curve: Curves.EaseIn,
            repeatMode: RepeatMode.Reverse));
        owner.FlushBuild();
        Assert.InRange(values[^1], 0.05, 0.10);

        // The update restarts the repeat, so its ticker needs a frame of its own to start its clock.
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.75));
        owner.FlushBuild();
        // Flutter's repeating simulation keeps the phase the current value implies: 0.75s into a 2s
        // period that started ~0.19s in, so the forward leg is a bit under halfway.
        Assert.InRange(values[^1], 0.44, 0.48);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 4.25));
        owner.FlushBuild();
        // 4.25s plus the same phase offset is two full periods and a bit, so the forward leg restarts.
        Assert.InRange(values[^1], 0.18, 0.26);

        root.Unmount();

        RepeatingAnimationBuilder<double> Build(
            TimeSpan duration,
            Curve curve,
            RepeatMode repeatMode)
        {
            return new RepeatingAnimationBuilder<double>(
                animatable: tween,
                duration: duration,
                curve: curve,
                repeatMode: repeatMode,
                builder: (_, value, _) =>
                {
                    values.Add(value);
                    return new SizedBox(width: 10, height: 10);
                });
        }
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TrackingValueListenable<T> : IValueListenable<T>
    {
        private readonly List<Action> _listeners = [];

        public TrackingValueListenable(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public int ListenerCount => _listeners.Count;

        public void AddListener(Action listener) => _listeners.Add(listener);

        public void RemoveListener(Action listener) => _listeners.Remove(listener);

        public void SetValue(T value)
        {
            Value = value;
            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }
    }

    private sealed class TrackingListenable : IListenable
    {
        private readonly List<Action> _listeners = [];

        public int ListenerCount => _listeners.Count;

        public void AddListener(Action listener) => _listeners.Add(listener);

        public void RemoveListener(Action listener) => _listeners.Remove(listener);

        public void Notify()
        {
            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }
    }

    private sealed class TrackingAnimation : Animation<double>
    {
        private readonly List<Action> _listeners = [];
        private readonly List<Action<AnimationStatus>> _statusListeners = [];
        private double _value;
        private AnimationStatus _status;

        public TrackingAnimation(double value, AnimationStatus status)
        {
            _value = value;
            _status = status;
        }

        public override double Value => _value;

        public override AnimationStatus Status => _status;

        public int StatusListenerCount => _statusListeners.Count;

        public int ListenerCount => _listeners.Count;

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) => _statusListeners.Add(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Remove(listener);
        }

        public void Set(double value, AnimationStatus status)
        {
            AnimationStatus previousStatus = _status;
            _value = value;
            _status = status;
            if (previousStatus != status)
            {
                foreach (var listener in _statusListeners.ToArray())
                {
                    listener(status);
                }
            }

            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }
    }

    private sealed class BuildCounterWidget : StatefulWidget
    {
        public int BuildCount { get; private set; }

        public override State CreateState() => new BuildCounterState();

        private sealed class BuildCounterState : State
        {
            public override Widget Build(BuildContext context)
            {
                var currentWidget = (BuildCounterWidget)StateWidget;
                currentWidget.BuildCount++;
                return new SizedBox(width: 10, height: 10);
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
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
        }
    }
}
