using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class InheritedNotifierTests
{
    [Fact]
    public void InheritedNotifier_NotifierUpdates_RebuildDependents()
    {
        InheritedNotifierTracker.Reset();

        var owner = new BuildOwner();
        var notifier = new ValueNotifier<int>(0);
        var stableChildTree = new Row(children: [new NotifierReaderWidget()]);

        var root = new TestRootElement(new IntInheritedNotifier(notifier, stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([0], InheritedNotifierTracker.SeenValues);

        notifier.Value = 1;
        notifier.Value = 2;
        owner.FlushBuild();

        Assert.Equal([0, 2], InheritedNotifierTracker.SeenValues);
    }

    [Fact]
    public void InheritedNotifier_NotifierIdentityChange_UnsubscribesOldAndSubscribesNew()
    {
        InheritedNotifierTracker.Reset();

        var owner = new BuildOwner();
        var notifierA = new ValueNotifier<int>(0);
        var notifierB = new ValueNotifier<int>(10);
        var stableChildTree = new Row(children: [new NotifierReaderWidget()]);

        var root = new TestRootElement(new IntInheritedNotifier(notifierA, stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        root.Update(new IntInheritedNotifier(notifierB, stableChildTree));
        owner.FlushBuild();

        Assert.Equal([0, 10], InheritedNotifierTracker.SeenValues);

        notifierA.Value = 1;
        owner.FlushBuild();
        Assert.Equal([0, 10], InheritedNotifierTracker.SeenValues);

        notifierB.Value = 11;
        owner.FlushBuild();
        Assert.Equal([0, 10, 11], InheritedNotifierTracker.SeenValues);
    }

    [Fact]
    public void InheritedNotifier_NullNotifier_TransitionsSubscribeAndUnsubscribeCorrectly()
    {
        InheritedNotifierTracker.Reset();

        var owner = new BuildOwner();
        var notifier = new ValueNotifier<int>(5);
        var stableChildTree = new Row(children: [new NotifierReaderWidget()]);

        var root = new TestRootElement(new IntInheritedNotifier(notifier: null, stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([-1], InheritedNotifierTracker.SeenValues);

        root.Update(new IntInheritedNotifier(notifier, stableChildTree));
        owner.FlushBuild();
        Assert.Equal([-1, 5], InheritedNotifierTracker.SeenValues);

        notifier.Value = 6;
        owner.FlushBuild();
        Assert.Equal([-1, 5, 6], InheritedNotifierTracker.SeenValues);

        root.Update(new IntInheritedNotifier(notifier: null, stableChildTree));
        owner.FlushBuild();
        Assert.Equal([-1, 5, 6, -1], InheritedNotifierTracker.SeenValues);

        notifier.Value = 7;
        owner.FlushBuild();
        Assert.Equal([-1, 5, 6, -1], InheritedNotifierTracker.SeenValues);
    }

    [Fact]
    public void InheritedNotifier_GlobalKeyReinsertSameWidget_SchedulesBuildAfterInactiveNotification()
    {
        InheritedNotifierTracker.Reset();

        var owner = new BuildOwner();
        var notifier = new CountingIntNotifier(0);
        var globalWidget = new CountingIntInheritedNotifier(
            notifier,
            new CountingNotifierReaderWidget(),
            key: new TestGlobalKey("counting-notifier"));

        var root = new TestRootElement(new SharedGlobalPlacementHost(includeGlobal: true, moveToLeft: true, globalWidget));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([0], InheritedNotifierTracker.CountingSeenValues);
        Assert.Equal(1, notifier.ListenerCount);

        root.Update(new SharedGlobalPlacementHost(includeGlobal: false, moveToLeft: true, globalWidget));

        notifier.SetValue(1);

        root.Update(new SharedGlobalPlacementHost(includeGlobal: true, moveToLeft: false, globalWidget));
        owner.FlushBuild();

        Assert.Equal(0, InheritedNotifierTracker.CountingSeenValues[0]);
        Assert.Equal(1, InheritedNotifierTracker.CountingSeenValues[^1]);
        Assert.Contains(1, InheritedNotifierTracker.CountingSeenValues);
        Assert.Equal(1, notifier.ListenerCount);

        root.Unmount();
        Assert.Equal(0, notifier.ListenerCount);
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
            if (_child != null)
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
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }

    private sealed class IntInheritedNotifier : InheritedNotifier<ValueNotifier<int>>
    {
        public IntInheritedNotifier(ValueNotifier<int>? notifier, Widget child, Key? key = null) : base(notifier, child, key)
        {
        }
    }

    private sealed class CountingIntInheritedNotifier : InheritedNotifier<CountingIntNotifier>
    {
        public CountingIntInheritedNotifier(CountingIntNotifier? notifier, Widget child, Key? key = null) : base(notifier, child, key)
        {
        }
    }

    private sealed class SharedGlobalPlacementHost(bool includeGlobal, bool moveToLeft, Widget globalWidget) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            Widget Left()
            {
                return includeGlobal && moveToLeft ? globalWidget : new SizedBox(width: 1, height: 1);
            }

            Widget Right()
            {
                return includeGlobal && !moveToLeft ? globalWidget : new SizedBox(width: 1, height: 1);
            }

            return new Row(children: [new Expanded(Left()), new Expanded(Right())]);
        }
    }

    private sealed record TestGlobalKey(string Id) : GlobalKey;

    private sealed class CountingIntNotifier : IListenable
    {
        private readonly List<Action> _listeners = [];

        public CountingIntNotifier(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }

        public int ListenerCount => _listeners.Count;

        public void SetValue(int value)
        {
            Value = value;
            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }

        public void AddListener(Action listener)
        {
            _listeners.Add(listener);
        }

        public void RemoveListener(Action listener)
        {
            _ = _listeners.Remove(listener);
        }
    }

    private sealed class CountingNotifierReaderWidget : StatefulWidget
    {
        public override State CreateState() => new CountingNotifierReaderState();
    }

    private sealed class CountingNotifierReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var inherited = context.DependOnInherited<CountingIntInheritedNotifier>()
                            ?? throw new InvalidOperationException("CountingIntInheritedNotifier not found.");
            var value = inherited.Notifier?.Value ?? -1;
            InheritedNotifierTracker.CountingSeenValues.Add(value);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class NotifierReaderWidget : StatefulWidget
    {
        public override State CreateState() => new NotifierReaderState();
    }

    private sealed class NotifierReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var inherited = context.DependOnInherited<IntInheritedNotifier>()
                            ?? throw new InvalidOperationException("IntInheritedNotifier not found.");
            var value = inherited.Notifier?.Value ?? -1;
            InheritedNotifierTracker.SeenValues.Add(value);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class InheritedNotifierTracker
    {
        public static readonly List<int> SeenValues = [];
        public static readonly List<int> CountingSeenValues = [];

        public static void Reset()
        {
            SeenValues.Clear();
            CountingSeenValues.Clear();
        }
    }
}
