using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class ElementLifecycleTests
{
    [Fact]
    public void UpdateChildren_WithValueKeys_PreservesStateAcrossReorder_AndDisposesRemoved()
    {
        KeyedTracker.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(new KeyedListHost([1, 2, 3]));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initial = KeyedTracker.CurrentStateIdByItem.ToDictionary(static pair => pair.Key, static pair => pair.Value);

        root.Update(new KeyedListHost([3, 1, 2]));
        owner.FlushBuild();

        Assert.Equal(initial[1], KeyedTracker.CurrentStateIdByItem[1]);
        Assert.Equal(initial[2], KeyedTracker.CurrentStateIdByItem[2]);
        Assert.Equal(initial[3], KeyedTracker.CurrentStateIdByItem[3]);
        Assert.Empty(KeyedTracker.DisposedStateIds);

        root.Update(new KeyedListHost([3, 2, 4]));
        owner.FlushBuild();

        Assert.Equal(initial[3], KeyedTracker.CurrentStateIdByItem[3]);
        Assert.Equal(initial[2], KeyedTracker.CurrentStateIdByItem[2]);
        Assert.True(KeyedTracker.CurrentStateIdByItem.ContainsKey(4));
        Assert.False(KeyedTracker.CurrentStateIdByItem.ContainsKey(1));
        Assert.Contains(initial[1], KeyedTracker.DisposedStateIds);
    }

    [Fact]
    public void GlobalKey_Reparenting_TriggersDeactivateThenActivate_WithoutDisposeUntilUnmount()
    {
        LifecycleTracker.Reset();

        var owner = new BuildOwner();
        var identity = new object();

        var root = new TestRootElement(new GlobalReparentHost(moveToLeft: true, identity));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(["init"], LifecycleTracker.Events);

        root.Update(new GlobalReparentHost(moveToLeft: false, identity));
        owner.FlushBuild();

        var deactivateIndex = LifecycleTracker.Events.IndexOf("deactivate");
        var activateIndex = LifecycleTracker.Events.IndexOf("activate");

        Assert.True(deactivateIndex >= 0, "Expected deactivate event during GlobalKey move.");
        Assert.True(activateIndex > deactivateIndex, "Expected activate after deactivate during GlobalKey move.");
        Assert.DoesNotContain("dispose", LifecycleTracker.Events);

        root.Unmount();

        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "dispose"));
    }

    [Fact]
    public void GlobalKey_SameSlotUpdate_DoesNotTriggerDeactivateOrActivate()
    {
        LifecycleTracker.Reset();

        var owner = new BuildOwner();
        var identity = new object();

        var root = new TestRootElement(new GlobalPlacementHost(includeGlobal: true, moveToLeft: true, identity));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(["init"], LifecycleTracker.Events);

        root.Update(new GlobalPlacementHost(includeGlobal: true, moveToLeft: true, identity));
        owner.FlushBuild();

        Assert.Equal(["init"], LifecycleTracker.Events);

        root.Unmount();
        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "dispose"));
    }

    [Fact]
    public void GlobalKey_RemoveThenReinsertBeforeFlush_RetainsState()
    {
        LifecycleTracker.Reset();

        var owner = new BuildOwner();
        var identity = new object();

        var root = new TestRootElement(new GlobalPlacementHost(includeGlobal: true, moveToLeft: true, identity));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        root.Update(new GlobalPlacementHost(includeGlobal: false, moveToLeft: true, identity));
        root.Update(new GlobalPlacementHost(includeGlobal: true, moveToLeft: false, identity));
        owner.FlushBuild();

        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "deactivate"));
        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "activate"));
        Assert.DoesNotContain("dispose", LifecycleTracker.Events);

        root.Unmount();
        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "dispose"));
    }

    [Fact]
    public void GlobalKey_TwoMovesBeforeFlush_ProcessesBothMovesWithoutDispose()
    {
        LifecycleTracker.Reset();

        var owner = new BuildOwner();
        var identity = new object();

        var root = new TestRootElement(new GlobalPlacementHost(includeGlobal: true, moveToLeft: true, identity));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        root.Update(new GlobalPlacementHost(includeGlobal: true, moveToLeft: false, identity));
        root.Update(new GlobalPlacementHost(includeGlobal: true, moveToLeft: true, identity));
        owner.FlushBuild();

        Assert.Equal(2, LifecycleTracker.Events.Count(static eventName => eventName == "deactivate"));
        Assert.Equal(2, LifecycleTracker.Events.Count(static eventName => eventName == "activate"));
        Assert.DoesNotContain("dispose", LifecycleTracker.Events);

        root.Unmount();
        Assert.Equal(1, LifecycleTracker.Events.Count(static eventName => eventName == "dispose"));
    }

    [Fact]
    public void UpdateChildren_MixedKeyedAndUnkeyed_ReusesKeyedAndStableTailDisposesMovedUnkeyed()
    {
        MixedTracker.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(new MixedKeyedUnkeyedHost(reorder: false));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialKeyed = MixedTracker.CurrentKeyedStateIdByName.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value);
        var initialUnkeyed = MixedTracker.CurrentUnkeyedStateIdByName.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value);

        root.Update(new MixedKeyedUnkeyedHost(reorder: true));
        owner.FlushBuild();

        Assert.Equal(initialKeyed["k1"], MixedTracker.CurrentKeyedStateIdByName["k1"]);
        Assert.Equal(initialKeyed["k2"], MixedTracker.CurrentKeyedStateIdByName["k2"]);

        Assert.NotEqual(initialUnkeyed["lead"], MixedTracker.CurrentUnkeyedStateIdByName["lead"]);
        Assert.NotEqual(initialUnkeyed["middle"], MixedTracker.CurrentUnkeyedStateIdByName["middle"]);
        Assert.Equal(initialUnkeyed["tail"], MixedTracker.CurrentUnkeyedStateIdByName["tail"]);
        Assert.Contains(initialUnkeyed["lead"], MixedTracker.DisposedStateIds);
        Assert.Contains(initialUnkeyed["middle"], MixedTracker.DisposedStateIds);
        Assert.DoesNotContain(initialUnkeyed["tail"], MixedTracker.DisposedStateIds);

        root.Unmount();
    }

    [Fact]
    public void UpdateChildren_NestedMixedAcrossParentLevels_RetainsKeyedBranchAndRecreatesMovedUnkeyedBranch()
    {
        MixedTracker.Reset();
        NestedBranchTracker.Reset();

        var owner = new BuildOwner();
        var root = new TestRootElement(new NestedMixedAcrossParentsHost(reorder: false));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var initialKeyedBranchState = NestedBranchTracker.CurrentKeyedStateIdByName["branch-keyed"];
        var initialUnkeyedBranchState = NestedBranchTracker.CurrentUnkeyedStateIdByName["branch-unkeyed"];
        var initialKeyed = MixedTracker.CurrentKeyedStateIdByName.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value);
        var initialUnkeyed = MixedTracker.CurrentUnkeyedStateIdByName.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value);

        root.Update(new NestedMixedAcrossParentsHost(reorder: true));
        owner.FlushBuild();

        Assert.Equal(initialKeyedBranchState, NestedBranchTracker.CurrentKeyedStateIdByName["branch-keyed"]);
        Assert.NotEqual(initialUnkeyedBranchState, NestedBranchTracker.CurrentUnkeyedStateIdByName["branch-unkeyed"]);
        Assert.Contains(initialUnkeyedBranchState, NestedBranchTracker.DisposedStateIds);

        Assert.Equal(initialKeyed["nested-k1"], MixedTracker.CurrentKeyedStateIdByName["nested-k1"]);
        Assert.Equal(initialKeyed["nested-k2"], MixedTracker.CurrentKeyedStateIdByName["nested-k2"]);

        Assert.NotEqual(initialUnkeyed["nested-lead"], MixedTracker.CurrentUnkeyedStateIdByName["nested-lead"]);
        Assert.NotEqual(initialUnkeyed["nested-middle"], MixedTracker.CurrentUnkeyedStateIdByName["nested-middle"]);
        Assert.Equal(initialUnkeyed["nested-tail"], MixedTracker.CurrentUnkeyedStateIdByName["nested-tail"]);
        Assert.Equal(initialUnkeyed["root-tail"], MixedTracker.CurrentUnkeyedStateIdByName["root-tail"]);
        Assert.Contains(initialUnkeyed["nested-lead"], MixedTracker.DisposedStateIds);
        Assert.Contains(initialUnkeyed["nested-middle"], MixedTracker.DisposedStateIds);
        Assert.DoesNotContain(initialUnkeyed["nested-tail"], MixedTracker.DisposedStateIds);
        Assert.DoesNotContain(initialUnkeyed["root-tail"], MixedTracker.DisposedStateIds);

        root.Unmount();
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

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }

    private sealed class KeyedListHost(IReadOnlyList<int> ids) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new Column(children: [..ids.Select(id => new TrackedItemWidget(id, new ValueKey<int>(id)))]);
        }
    }

    private sealed class TrackedItemWidget : StatefulWidget
    {
        public int ItemId { get; }

        public TrackedItemWidget(int itemId, Key key) : base(key)
        {
            ItemId = itemId;
        }

        public override State CreateState() => new TrackedItemState(ItemId);
    }

    private sealed class TrackedItemState(int itemId) : State
    {
        private readonly string _stateId = Guid.NewGuid().ToString("N");

        public override void InitState()
        {
            KeyedTracker.CurrentStateIdByItem[itemId] = _stateId;
        }

        public override void Dispose()
        {
            if (KeyedTracker.CurrentStateIdByItem.TryGetValue(itemId, out var stateId) && stateId == _stateId)
            {
                KeyedTracker.CurrentStateIdByItem.Remove(itemId);
            }

            KeyedTracker.DisposedStateIds.Add(_stateId);
        }

        public override Widget Build(BuildContext context)
        {
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class KeyedTracker
    {
        public static readonly Dictionary<int, string> CurrentStateIdByItem = [];
        public static readonly List<string> DisposedStateIds = [];

        public static void Reset()
        {
            CurrentStateIdByItem.Clear();
            DisposedStateIds.Clear();
        }
    }

    private sealed class GlobalReparentHost(bool moveToLeft, object identity) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            var globalWidget = new LifecycleRecorderWidget(new GlobalObjectKey<LifecycleRecorderState>(identity));

            return new Row(
                children:
                [
                    new Expanded(moveToLeft ? globalWidget : new SizedBox(width: 1, height: 1)),
                    new Expanded(!moveToLeft ? globalWidget : new SizedBox(width: 1, height: 1)),
                ]);
        }
    }

    private sealed class GlobalPlacementHost(bool includeGlobal, bool moveToLeft, object identity) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            var globalWidget = new LifecycleRecorderWidget(new GlobalObjectKey<LifecycleRecorderState>(identity));

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

    private sealed class MixedKeyedUnkeyedHost(bool reorder) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            if (!reorder)
            {
                return new Column(
                    children:
                    [
                        new UnkeyedMixedItemWidget("lead"),
                        new KeyedMixedItemWidget("k1"),
                        new UnkeyedMixedItemWidget("middle"),
                        new KeyedMixedItemWidget("k2"),
                        new UnkeyedMixedItemWidget("tail"),
                    ]);
            }

            return new Column(
                children:
                [
                    new KeyedMixedItemWidget("k2"),
                    new UnkeyedMixedItemWidget("lead"),
                    new KeyedMixedItemWidget("k1"),
                    new UnkeyedMixedItemWidget("middle"),
                    new UnkeyedMixedItemWidget("tail"),
                ]);
        }
    }

    private sealed class NestedMixedAcrossParentsHost(bool reorder) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            if (!reorder)
            {
                return new Column(
                    children:
                    [
                        new KeyedNestedBranchWidget("branch-keyed", reorderInner: false),
                        new UnkeyedNestedBranchWidget("branch-unkeyed"),
                        new UnkeyedMixedItemWidget("root-tail"),
                    ]);
            }

            return new Column(
                children:
                [
                    new UnkeyedNestedBranchWidget("branch-unkeyed"),
                    new KeyedNestedBranchWidget("branch-keyed", reorderInner: true),
                    new UnkeyedMixedItemWidget("root-tail"),
                ]);
        }
    }

    private sealed class KeyedNestedBranchWidget(string name, bool reorderInner) : StatefulWidget(new ValueKey<string>(name))
    {
        public string Name { get; } = name;
        public bool ReorderInner { get; } = reorderInner;

        public override State CreateState() => new KeyedNestedBranchState();
    }

    private sealed class KeyedNestedBranchState : State
    {
        private readonly string _stateId = Guid.NewGuid().ToString("N");
        private KeyedNestedBranchWidget BranchWidget => (KeyedNestedBranchWidget)Element.Widget;

        public override void InitState()
        {
            NestedBranchTracker.CurrentKeyedStateIdByName[BranchWidget.Name] = _stateId;
        }

        public override void Dispose()
        {
            if (NestedBranchTracker.CurrentKeyedStateIdByName.TryGetValue(BranchWidget.Name, out var stateId) &&
                stateId == _stateId)
            {
                NestedBranchTracker.CurrentKeyedStateIdByName.Remove(BranchWidget.Name);
            }

            NestedBranchTracker.DisposedStateIds.Add(_stateId);
        }

        public override Widget Build(BuildContext context)
        {
            if (!BranchWidget.ReorderInner)
            {
                return new Column(
                    children:
                    [
                        new Row(
                            children:
                            [
                                new UnkeyedMixedItemWidget("nested-lead"),
                                new KeyedMixedItemWidget("nested-k1"),
                                new UnkeyedMixedItemWidget("nested-middle"),
                                new KeyedMixedItemWidget("nested-k2"),
                                new UnkeyedMixedItemWidget("nested-tail"),
                            ]),
                    ]);
            }

            return new Column(
                children:
                [
                    new Row(
                        children:
                        [
                            new KeyedMixedItemWidget("nested-k2"),
                            new UnkeyedMixedItemWidget("nested-lead"),
                            new KeyedMixedItemWidget("nested-k1"),
                            new UnkeyedMixedItemWidget("nested-middle"),
                            new UnkeyedMixedItemWidget("nested-tail"),
                        ]),
                ]);
        }
    }

    private sealed class UnkeyedNestedBranchWidget(string name) : StatefulWidget
    {
        public string Name { get; } = name;

        public override State CreateState() => new UnkeyedNestedBranchState();
    }

    private sealed class UnkeyedNestedBranchState : State
    {
        private readonly string _stateId = Guid.NewGuid().ToString("N");
        private UnkeyedNestedBranchWidget BranchWidget => (UnkeyedNestedBranchWidget)Element.Widget;

        public override void InitState()
        {
            NestedBranchTracker.CurrentUnkeyedStateIdByName[BranchWidget.Name] = _stateId;
        }

        public override void Dispose()
        {
            if (NestedBranchTracker.CurrentUnkeyedStateIdByName.TryGetValue(BranchWidget.Name, out var stateId) &&
                stateId == _stateId)
            {
                NestedBranchTracker.CurrentUnkeyedStateIdByName.Remove(BranchWidget.Name);
            }

            NestedBranchTracker.DisposedStateIds.Add(_stateId);
        }

        public override Widget Build(BuildContext context)
        {
            return new Column(children: [new SizedBox(width: 1, height: 1)]);
        }
    }

    private sealed class KeyedMixedItemWidget(string name) : StatefulWidget(new ValueKey<string>(name))
    {
        public override State CreateState() => new MixedItemState(name, isKeyed: true);
    }

    private sealed class UnkeyedMixedItemWidget(string name) : StatefulWidget
    {
        public override State CreateState() => new MixedItemState(name, isKeyed: false);
    }

    private sealed class MixedItemState(string name, bool isKeyed) : State
    {
        private readonly string _stateId = Guid.NewGuid().ToString("N");

        public override void InitState()
        {
            if (isKeyed)
            {
                MixedTracker.CurrentKeyedStateIdByName[name] = _stateId;
            }
            else
            {
                MixedTracker.CurrentUnkeyedStateIdByName[name] = _stateId;
            }
        }

        public override void Dispose()
        {
            if (isKeyed)
            {
                if (MixedTracker.CurrentKeyedStateIdByName.TryGetValue(name, out var stateId) && stateId == _stateId)
                {
                    MixedTracker.CurrentKeyedStateIdByName.Remove(name);
                }
            }
            else
            {
                if (MixedTracker.CurrentUnkeyedStateIdByName.TryGetValue(name, out var stateId) &&
                    stateId == _stateId)
                {
                    MixedTracker.CurrentUnkeyedStateIdByName.Remove(name);
                }
            }

            MixedTracker.DisposedStateIds.Add(_stateId);
        }

        public override Widget Build(BuildContext context)
        {
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class LifecycleRecorderWidget : StatefulWidget
    {
        public LifecycleRecorderWidget(Key key) : base(key)
        {
        }

        public override State CreateState() => new LifecycleRecorderState();
    }

    private sealed class LifecycleRecorderState : State
    {
        public override void InitState()
        {
            LifecycleTracker.Events.Add("init");
        }

        public override void Deactivate()
        {
            LifecycleTracker.Events.Add("deactivate");
        }

        public override void Activate()
        {
            LifecycleTracker.Events.Add("activate");
        }

        public override void Dispose()
        {
            LifecycleTracker.Events.Add("dispose");
        }

        public override Widget Build(BuildContext context)
        {
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class LifecycleTracker
    {
        public static readonly List<string> Events = [];

        public static void Reset()
        {
            Events.Clear();
        }
    }

    private static class MixedTracker
    {
        public static readonly Dictionary<string, string> CurrentKeyedStateIdByName = [];
        public static readonly Dictionary<string, string> CurrentUnkeyedStateIdByName = [];
        public static readonly List<string> DisposedStateIds = [];

        public static void Reset()
        {
            CurrentKeyedStateIdByName.Clear();
            CurrentUnkeyedStateIdByName.Clear();
            DisposedStateIds.Clear();
        }
    }

    private static class NestedBranchTracker
    {
        public static readonly Dictionary<string, string> CurrentKeyedStateIdByName = [];
        public static readonly Dictionary<string, string> CurrentUnkeyedStateIdByName = [];
        public static readonly List<string> DisposedStateIds = [];

        public static void Reset()
        {
            CurrentKeyedStateIdByName.Clear();
            CurrentUnkeyedStateIdByName.Clear();
            DisposedStateIds.Clear();
        }
    }
}
