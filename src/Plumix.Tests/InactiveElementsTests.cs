using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart (_InactiveElements)

namespace Plumix.Tests;

/// <summary>
/// The teardown behavior Dart's private <c>_InactiveElements</c> pins: a deactivated subtree is
/// parked until the end of the frame, unmounted deepest-first, and skipped entirely when a
/// <see cref="GlobalKey"/> grafts it somewhere else first.
/// </summary>
public sealed class InactiveElementsTests
{
    [Fact]
    public void UnmountAll_UnmountsChildrenBeforeTheirParent()
    {
        List<string> log = [];
        var owner = new BuildOwner();
        var root = new TestRootElement(new Marker(log, "outer", new Marker(log, "inner")));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        root.Update(new SizedBox());
        owner.FlushBuild();

        // Dart's _unmount recurses through visitChildren *before* unmounting the element itself.
        Assert.Equal(["inner", "outer"], log);
    }

    [Fact]
    public void UnmountAll_ReachesEveryChildOfAMultiChildElement()
    {
        List<string> log = [];
        var owner = new BuildOwner();
        var root = new TestRootElement(new Marker(
            log,
            "parent",
            new Column([new Marker(log, "first"), new Marker(log, "second")])));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        root.Update(new SizedBox());
        owner.FlushBuild();

        // Only the subtree root is parked; every descendant is reached through visitChildren, so a
        // multi-child element in the middle no longer strands its children in the inactive state.
        Assert.Equal(3, log.Count);
        Assert.Contains("first", log);
        Assert.Contains("second", log);
        Assert.Equal("parent", log[^1]);
    }

    [Fact]
    public void UnmountAll_UnmountsTheDeeperDeactivationRootFirst()
    {
        List<string> log = [];
        var owner = new BuildOwner();
        var toggle = new ToggleWidget(log);
        var root = new TestRootElement(toggle);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        ToggleState state = Assert.IsType<ToggleState>(toggle.CreatedState);
        state.Hide();
        owner.FlushBuild();

        // Both markers are removed in the same build, the shallow one first. Dart sorts the inactive
        // set by depth and walks it in reverse, so the deeper subtree is torn down first.
        Assert.Equal(["deep", "shallow"], log);
    }

    [Fact]
    public void UnmountAll_SkipsASubtreeThatAGlobalKeyReactivatedInTheSameFrame()
    {
        List<string> log = [];
        var key = new LabeledGlobalKey<State>("kept");
        var owner = new BuildOwner();
        var root = new TestRootElement(new Column([new Marker(log, "kept", key: key)]));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        root.Update(new Column([new SizedBox(), new Marker(log, "kept", key: key)]));
        owner.FlushBuild();

        Assert.Empty(log);
    }

    [Fact]
    public void DeactivatedElement_StaysMountedUntilTheInactiveListIsFlushed()
    {
        List<string> log = [];
        var owner = new BuildOwner();
        var root = new TestRootElement(new Marker(log, "child"));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        Element child = ChildOf(root);
        owner.BuildScope(root, () => root.Update(new SizedBox()));

        Assert.True(child.Mounted);
        Assert.False(child.IsActive);
        Assert.Empty(log);

        owner.FinalizeTree();

        Assert.False(child.Mounted);
        Assert.Equal(["child"], log);
    }

    private static Element ChildOf(Element element)
    {
        Element? found = null;
        element.VisitChildren(child => found ??= child);
        return found ?? throw new InvalidOperationException("The element has no child.");
    }

    private sealed class Marker : StatefulWidget
    {
        public Marker(List<string> log, string label, Widget? child = null, Key? key = null) : base(key)
        {
            Log = log;
            Label = label;
            Child = child;
        }

        public List<string> Log { get; }

        public string Label { get; }

        public Widget? Child { get; }

        public override State CreateState() => new MarkerState();
    }

    private sealed class MarkerState : State
    {
        private Marker Current => (Marker)StateWidget;

        public override Widget Build(BuildContext context) => Current.Child ?? new SizedBox();

        public override void Dispose()
        {
            Current.Log.Add(Current.Label);
            base.Dispose();
        }
    }

    /// <summary>Puts two markers at different depths under one parent and removes both at once.</summary>
    private sealed class ToggleWidget : StatefulWidget
    {
        public ToggleWidget(List<string> log)
        {
            Log = log;
        }

        public List<string> Log { get; }

        public ToggleState? CreatedState { get; private set; }

        public override State CreateState()
        {
            var state = new ToggleState();
            CreatedState = state;
            return state;
        }
    }

    private sealed class ToggleState : State
    {
        private bool _visible = true;

        private ToggleWidget Current => (ToggleWidget)StateWidget;

        public void Hide() => SetState(() => _visible = false);

        public override Widget Build(BuildContext context)
        {
            return new Column(
            [
                _visible ? new Marker(Current.Log, "shallow") : new SizedBox(),
                new Padding(
                    EdgeInsets.Zero,
                    _visible ? new Marker(Current.Log, "deep") : new SizedBox()),
            ]);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public override Element? RenderObjectAttachingChild => _child;

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
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            base.ForgetChild(child);
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
    }
}
