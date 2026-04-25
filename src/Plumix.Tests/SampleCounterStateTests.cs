using Plumix;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): dart_sample/lib/counter_app_model.dart; dart_sample/lib/counter_scope.dart (sample parity regression tests)

namespace Plumix.Tests;

public sealed class SampleCounterStateTests
{
    [Fact]
    public void CounterAppModel_Mutations_UpdateStateAndNotifyListeners()
    {
        var model = new CounterAppModel();
        var notifications = 0;
        model.AddListener(() => notifications += 1);

        Assert.Equal(0, model.Count);
        Assert.Equal([1, 2, 3, 4], model.Items);
        Assert.True(model.PlaceGlobalOnLeft);

        model.Increment();
        model.Decrement();
        model.ReverseItems();
        model.InsertHead();
        model.RemoveTail();
        model.ToggleGlobalPlacement();

        Assert.Equal(0, model.Count);
        Assert.Equal([5, 4, 3, 2], model.Items);
        Assert.False(model.PlaceGlobalOnLeft);
        Assert.Equal(6, notifications);

        while (model.Items.Count > 0)
        {
            model.RemoveTail();
        }

        Assert.Empty(model.Items);
        var notificationsBeforeNoOpRemove = notifications;
        model.RemoveTail();
        Assert.Equal(notificationsBeforeNoOpRemove, notifications);
    }

    [Fact]
    public void CounterScope_RebuildsDependent_WhenModelChanges()
    {
        CounterScopeTracker.Reset();

        var owner = new BuildOwner();
        var model = new CounterAppModel();

        var root = new TestRootElement(new CounterScope(model, new CounterCountProbeWidget()));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([0], CounterScopeTracker.SeenCounts);

        model.Increment();
        model.Increment();
        owner.FlushBuild();

        model.Decrement();
        owner.FlushBuild();

        Assert.Equal([0, 2, 1], CounterScopeTracker.SeenCounts);
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

    private sealed class CounterCountProbeWidget : StatefulWidget
    {
        public override State CreateState() => new CounterCountProbeState();
    }

    private sealed class CounterCountProbeState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = CounterScope.Of(context);
            CounterScopeTracker.SeenCounts.Add(model.Count);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class CounterScopeTracker
    {
        public static readonly List<int> SeenCounts = [];

        public static void Reset()
        {
            SeenCounts.Clear();
        }
    }
}
