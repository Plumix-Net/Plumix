using System.Collections;
using Avalonia;
using Plumix.Accessibility;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only infrastructure tests: the host-side accessibility tree. The asserted behaviours are the
// engine's accessibility_bridge_unittests.cc and flutter_platform_node_delegate_unittests.cc cases
// (tree shape, roles, states, reparenting, events, actions, bounds), driven through real framework
// semantics updates instead of hand-built engine nodes.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AccessibilityBridgeTests : IDisposable
{
    private readonly AccessibilityBridgeHarness _harness = new();
    private readonly FrameworkDartTester _tester;
    private readonly AccessibilityBridge _bridge;
    private readonly List<AccessibilityTreeUpdate> _updates;

    public AccessibilityBridgeTests()
    {
        _tester = _harness.Tester;
        _bridge = _harness.Bridge;
        _updates = _harness.Updates;
    }

    public void Dispose() => _harness.Dispose();

    private void Pump(Widget widget) => _harness.Pump(widget);

    private AccessibilityNode Find(string label) => _harness.Find(label);

    private static Widget Small(double width = 9) => new SizedBox(width: width, height: 9);

    private static Widget Box(string label, Widget? child = null) =>
        new Semantics(container: true, label: label, child: child ?? new SizedBox(width: 40, height: 20));

    // accessibility_bridge_unittests.cc: BasicTest.
    [Fact]
    public void MirrorsTheTreeInTraversalOrder()
    {
        Pump(new Semantics(
            container: true,
            label: "root",
            identifier: "identifier",
            child: new Column(children: [Box("child 1"), Box("child 2")])));

        AccessibilityNode root = _bridge.Root!;
        Assert.Equal(0, root.Id);
        AccessibilityNode parent = Find("root");
        Assert.Equal("identifier", parent.Identifier);
        Assert.Equal(["child 1", "child 2"], parent.Children.Select(static child => child.Label));
        Assert.All(parent.Children, static child => Assert.Empty(child.Children));
        Assert.All(parent.Children, child => Assert.Same(parent, child.Parent));
        Assert.Null(root.Parent);
    }

    // accessibility_bridge_unittests.cc: AccessibilityRootId — the root is the first commit's root.
    [Fact]
    public void KeepsTheRootOfTheFirstCommit()
    {
        Pump(Box("only"));

        AccessibilityTreeUpdate first = Assert.Single(_updates);
        Assert.True(first.RootChanged);
        Assert.Same(_bridge.Root, _bridge.GetNode(0));

        Pump(Box("renamed"));
        Assert.False(_updates[^1].RootChanged);
        Assert.Same(_bridge.Root, _bridge.GetNode(0));
    }

    // SetRoleFromFlutterUpdate, first match wins (SwitchHasSwitchRole, SliderHasSliderRole,
    // CanSetCheckboxChecked, DoesNotAssignEditableRootToSelectableText).
    [Fact]
    public void DerivesTheDesktopRoleLadder()
    {
        Pump(new Column(children:
        [
            new Semantics(container: true, label: "button", button: true, image: true, child: Leaf()),
            new Semantics(container: true, label: "field", textField: true, child: Leaf()),
            new Semantics(container: true, label: "selectable", textField: true, readOnly: true, child: Leaf()),
            new Semantics(container: true, label: "header", header: true, child: Leaf()),
            new Semantics(container: true, label: "image", image: true, child: Leaf()),
            new Semantics(container: true, label: "link", link: true, child: Leaf()),
            new Semantics(
                container: true,
                label: "radio",
                @checked: true,
                inMutuallyExclusiveGroup: true,
                child: Leaf()),
            new Semantics(container: true, label: "checkbox", @checked: true, child: Leaf()),
            new Semantics(container: true, label: "mixed", @checked: false, mixed: true, child: Leaf()),
            new Semantics(container: true, label: "switch", toggled: false, enabled: true, child: Leaf()),
            new Semantics(container: true, label: "slider", slider: true, child: Leaf()),
            new Semantics(container: true, label: "text", child: Leaf()),
            new Semantics(container: true, label: "group", child: new Column(children: [Box("inner")])),
        ]));

        Assert.Equal(AccessibilityRole.Button, Find("button").Role);
        Assert.Equal(AccessibilityRole.TextField, Find("field").Role);
        Assert.True(Find("field").IsEditableTextField);
        Assert.Equal(AccessibilityRole.StaticText, Find("selectable").Role);
        Assert.False(Find("selectable").IsEditableTextField);
        Assert.Equal(AccessibilityRole.Header, Find("header").Role);
        Assert.Equal(AccessibilityRole.Image, Find("image").Role);
        Assert.Equal(AccessibilityRole.Link, Find("link").Role);
        Assert.Equal(AccessibilityRole.RadioButton, Find("radio").Role);
        Assert.Equal(AccessibilityCheckedState.True, Find("radio").CheckedState);
        Assert.Equal(AccessibilityRole.CheckBox, Find("checkbox").Role);
        Assert.Equal(AccessibilityCheckedState.True, Find("checkbox").CheckedState);
        Assert.Equal(AccessibilityCheckedState.Mixed, Find("mixed").CheckedState);
        Assert.Equal(AccessibilityRole.Switch, Find("switch").Role);
        Assert.Equal(AccessibilityCheckedState.False, Find("switch").CheckedState);
        Assert.Equal(AccessibilityRole.Slider, Find("slider").Role);
        Assert.Equal(AccessibilityRole.StaticText, Find("text").Role);
        Assert.Equal(AccessibilityCheckedState.None, Find("text").CheckedState);
        Assert.Equal(AccessibilityRole.Group, Find("group").Role);

        static Widget Leaf() => new SizedBox(width: 20, height: 20);
    }

    // SetStateFromFlutterUpdate: kIgnored, kSelected (IsSelectedAttribute), kExpanded/kCollapsed.
    [Fact]
    public void DerivesStates()
    {
        Pump(new Column(children:
        [
            new Semantics(container: true, child: new SizedBox(width: 20, height: 20)),
            new Semantics(container: true, label: "selected", selected: true, child: Small()),
            new Semantics(container: true, label: "unselected", selected: false, child: Small()),
            new Semantics(container: true, label: "expanded", expanded: true, child: Small()),
            new Semantics(container: true, label: "collapsed", expanded: false, child: Small()),
            new Semantics(container: true, label: "disabled", enabled: false, child: Small()),
            new Semantics(container: true, label: "plain", child: Small()),
        ]));

        AccessibilityNode empty = _bridge.Root!.Children
            .Single(static node => node.Label.Length == 0 && node.Children.Count == 0);
        Assert.True(empty.IsIgnored);
        Assert.False(Find("plain").IsIgnored);
        Assert.True(Find("selected").IsSelected);
        Assert.False(Find("unselected").IsSelected);
        Assert.False(Find("plain").IsSelected);
        Assert.True(Find("expanded").IsExpanded);
        Assert.False(Find("collapsed").IsExpanded);
        Assert.Null(Find("plain").IsExpanded);
        Assert.False(Find("disabled").IsEnabled);
        Assert.True(Find("plain").IsEnabled);
    }

    // accessibility_bridge_unittests.cc: CanFireChildrenChangedCorrectly.
    [Fact]
    public void AddingAChildReportsChildrenChangedAndSubtreeCreated()
    {
        Pump(Box("root", new Column(children: [Box("child 1")])));
        _updates.Clear();

        Pump(Box("root", new Column(children: [Box("child 1"), Box("child 2")])));

        AccessibilityTreeUpdate update = Assert.Single(_updates);
        Assert.Contains(
            update.Events,
            e => e.Type == AccessibilityEventType.ChildrenChanged && e.Node.Label == "root");
        AccessibilityEvent created = Assert.Single(
            update.Events,
            static e => e.Type == AccessibilityEventType.SubtreeCreated);
        Assert.Equal("child 2", created.Node.Label);
    }

    [Fact]
    public void RemovingAChildDropsItsSubtree()
    {
        Pump(Box("root", new Column(children: [Box("keep"), Box("drop", Box("grandchild"))])));
        AccessibilityNode dropped = Find("drop");
        AccessibilityNode grandchild = Find("grandchild");
        int count = _bridge.NodeCount;
        _updates.Clear();

        Pump(Box("root", new Column(children: [Box("keep")])));

        AccessibilityTreeUpdate update = Assert.Single(_updates);
        Assert.Contains(dropped, update.RemovedNodes);
        Assert.Contains(grandchild, update.RemovedNodes);
        Assert.False(dropped.IsAttached);
        Assert.Null(_bridge.GetNode(dropped.Id));
        Assert.Equal(count - 2, _bridge.NodeCount);
        Assert.True(update.LayoutChanged);
    }

    // accessibility_bridge_unittests.cc: CanReparentNode — a node keyed across parents moves.
    [Fact]
    public void ReparentingKeepsTheMovedNode()
    {
        var key = new GlobalObjectKey<State>("moved");
        Pump(new Column(children:
        [
            Box("first", new Column(children: [Box("moved").WithKey(key)])),
            Box("second", new SizedBox(width: 40, height: 20)),
        ]));
        AccessibilityNode moved = Find("moved");
        _updates.Clear();

        Pump(new Column(children:
        [
            Box("first", new SizedBox(width: 40, height: 20)),
            Box("second", new Column(children: [Box("moved").WithKey(key)])),
        ]));

        AccessibilityNode after = Find("moved");
        Assert.Equal(moved.Id, after.Id);
        Assert.Same(moved, after);
        Assert.Equal("second", after.Parent!.Label);
        Assert.Empty(_updates[^1].RemovedNodes);
        Assert.Equal(
            2,
            _updates[^1].Events.Count(static e => e.Type == AccessibilityEventType.ChildrenChanged
                                                  && e.Node.Label is "first" or "second"));
        Assert.Contains(
            _updates[^1].Events,
            static e => e.Type == AccessibilityEventType.RoleChanged && e.Node.Label == "first");
    }

    [Fact]
    public void DiffsNodeChanges()
    {
        Pump(new Column(children:
        [
            new Semantics(container: true, label: "name", value: "1", hint: "h", child: Small()),
            new Semantics(container: true, label: "check", @checked: false, child: Small()),
            new Semantics(container: true, label: "live", liveRegion: true, child: Small()),
            new Semantics(container: true, label: "size", child: Small()),
        ]));
        _updates.Clear();

        Pump(new Column(children:
        [
            new Semantics(container: true, label: "name", value: "2", hint: "i", child: Small()),
            new Semantics(container: true, label: "check", @checked: true, child: Small()),
            new Semantics(container: true, label: "live", liveRegion: true, child: Small(10)),
            new Semantics(container: true, label: "size", child: new SizedBox(width: 9, height: 19)),
        ]));

        IReadOnlyList<AccessibilityEvent> events = _updates.SelectMany(static u => u.Events).ToList();
        Assert.Contains(events, static e => e.Type == AccessibilityEventType.ValueChanged && e.Node.Label == "name");
        Assert.Contains(events, static e => e.Type == AccessibilityEventType.HintChanged && e.Node.Label == "name");
        Assert.Contains(
            events,
            static e => e.Type == AccessibilityEventType.CheckedStateChanged && e.Node.Label == "check");
        Assert.Contains(events, static e => e.Type == AccessibilityEventType.BoundsChanged && e.Node.Label == "size");
        Assert.DoesNotContain(events, static e => e.Type == AccessibilityEventType.LiveRegionChanged);
        _updates.Clear();

        Pump(new Column(children:
        [
            new Semantics(container: true, label: "renamed", value: "2", hint: "i", child: Small()),
            new Semantics(container: true, label: "check", @checked: true, child: Small()),
            new Semantics(container: true, label: "live 2", liveRegion: true, child: Small(10)),
            new Semantics(container: true, label: "size", child: new SizedBox(width: 9, height: 19)),
        ]));

        events = _updates.SelectMany(static u => u.Events).ToList();
        Assert.Contains(events, static e => e.Type == AccessibilityEventType.NameChanged && e.Node.Label == "renamed");
        Assert.Contains(
            events,
            static e => e.Type == AccessibilityEventType.LiveRegionChanged && e.Node.Label == "live 2");
        Assert.DoesNotContain(events, static e => e.Type == AccessibilityEventType.CheckedStateChanged);
    }

    // SetTreeData: a node reporting focus becomes the tree's focus and fires FOCUS_CHANGED.
    [Fact]
    public void TracksTheFrameworkFocus()
    {
        Pump(new Column(children:
        [
            new Semantics(container: true, label: "a", focusable: true, focused: false, child: Small()),
            new Semantics(container: true, label: "b", focusable: true, focused: false, child: Small()),
        ]));
        Assert.Equal(-1, _bridge.FocusedNodeId);
        _updates.Clear();

        Pump(new Column(children:
        [
            new Semantics(container: true, label: "a", focusable: true, focused: false, child: Small()),
            new Semantics(container: true, label: "b", focusable: true, focused: true, child: Small()),
        ]));

        Assert.Equal(Find("b").Id, _bridge.FocusedNodeId);
        AccessibilityEvent focus = Assert.Single(
            _updates.SelectMany(static u => u.Events),
            static e => e.Type == AccessibilityEventType.FocusChanged);
        Assert.Equal("b", focus.Node.Label);

        Pump(new Column(children:
        [
            new Semantics(container: true, label: "a", focusable: true, focused: false, child: Small()),
            new Semantics(container: true, label: "b", focusable: true, focused: false, child: Small()),
        ]));
        Assert.Equal(-1, _bridge.FocusedNodeId);
    }

    // RelativeToGlobalBounds over the physical-pixel tree: the view's logical bounds are the
    // framework's own getRectOfSemanticsNodeInViewCoordinates.
    [Fact]
    public void ComputesBoundsInPhysicalAndLogicalPixels()
    {
        Pump(new Padding(
            insets: EdgeInsets.Only(left: 10, top: 30),
            child: new Align(
                alignment: Alignment.TopLeft,
                child: new Semantics(container: true, label: "box", child: new SizedBox(width: 50, height: 40)))));

        AccessibilityNode box = Find("box");
        Assert.Equal(new Rect(20, 60, 100, 80), box.GetGlobalRect());
        Assert.Equal(new Rect(10, 30, 50, 40), box.GetBoundsInView());
        Assert.Equal(
            RendererBinding.Instance.GetRectOfSemanticsNodeInViewCoordinates(_bridge.ViewId, box.Id),
            box.GetBoundsInView());
    }

    // flutter_platform_node_delegate_unittests.cc: canPerfomActions.
    [Fact]
    public void DispatchesActionsToTheFramework()
    {
        var log = new List<string>();
        Pump(new Semantics(
            container: true,
            label: "target",
            onTap: () => log.Add("tap"),
            onDidGainAccessibilityFocus: () => log.Add("gain"),
            onDidLoseAccessibilityFocus: () => log.Add("lose"),
            child: new SizedBox(width: 20, height: 20)));
        AccessibilityNode target = Find("target");

        target.PerformAction(SemanticsActions.Tap);
        Assert.Equal(["tap"], log);

        _bridge.SetLastFocusedId(target.Id);
        Assert.Equal(target.Id, _bridge.LastFocusedId);
        Assert.Equal(["tap", "gain"], log);
    }

    // The engine only acts on nodes its tree still holds: a peer that outlived its node, or an
    // unknown id, never reaches the framework's action listeners (which may look the node up, as
    // RenderTapRegionSurface does through getRectOfSemanticsNodeInViewCoordinates).
    [Fact]
    public void DropsActionsForNodesTheFrameworkNoLongerHolds()
    {
        var seen = new List<SemanticsActionEvent>();
        void Listener(SemanticsActionEvent actionEvent) => seen.Add(actionEvent);

        Pump(new Column(children: [Box("stays"), Box("goes")]));
        AccessibilityNode stale = Find("goes");
        Pump(new Column(children: [Box("stays")]));
        Assert.False(stale.IsAttached);

        SemanticsBinding.Instance.AddSemanticsActionListener(Listener);
        try
        {
            stale.PerformAction(SemanticsActions.Tap);
            _bridge.DispatchAction(nodeId: -1, SemanticsActions.Tap);
            Assert.Empty(seen);

            Find("stays").PerformAction(SemanticsActions.Tap);
            Assert.Equal(Find("stays").Id, Assert.Single(seen).NodeId);
        }
        finally
        {
            SemanticsBinding.Instance.RemoveSemanticsActionListener(Listener);
        }
    }

    // SetLastFocusedId: the old node loses accessibility focus before the new one gains it.
    [Fact]
    public void MovingAccessibilityFocusTellsTheOldNodeFirst()
    {
        var log = new List<string>();
        Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "first",
                onDidGainAccessibilityFocus: () => log.Add("gain first"),
                onDidLoseAccessibilityFocus: () => log.Add("lose first"),
                child: new SizedBox(width: 20, height: 20)),
            new Semantics(
                container: true,
                label: "second",
                onDidGainAccessibilityFocus: () => log.Add("gain second"),
                onDidLoseAccessibilityFocus: () => log.Add("lose second"),
                child: new SizedBox(width: 20, height: 20)),
        ]));

        _bridge.SetLastFocusedId(Find("first").Id);
        _bridge.SetLastFocusedId(Find("second").Id);

        Assert.Equal(["gain first", "lose first", "gain second"], log);
        _bridge.ClearLastFocusedId(Find("first").Id);
        Assert.Equal(Find("second").Id, _bridge.LastFocusedId);
        _bridge.ClearLastFocusedId(Find("second").Id);
        Assert.Equal(-1, _bridge.LastFocusedId);
    }

    [Fact]
    public void ActivateTurnsSemanticsOnAndRoutesAccessibilityMessages()
    {
        Pump(Box("target"));
        Assert.False(PlatformDispatcher.Instance.SemanticsEnabled);
        var announcements = new List<AccessibilityAnnouncement>();
        var focusRequests = new List<AccessibilityNode>();
        _bridge.AnnouncementRequested += announcements.Add;
        _bridge.FocusRequested += focusRequests.Add;

        _bridge.Activate();

        Assert.True(_bridge.IsActive);
        Assert.True(PlatformDispatcher.Instance.SemanticsEnabled);
        Task announced = SemanticsService.SendAnnouncement(
            _tester.RenderView.FlutterView,
            "Hello",
            TextDirection.Rtl,
            Assertiveness.Assertive);
        Assert.True(announced.IsCompletedSuccessfully);
        AccessibilityAnnouncement announcement = Assert.Single(announcements);
        Assert.Equal(new AccessibilityAnnouncement("Hello", TextDirection.Rtl, Assertiveness.Assertive), announcement);

        SemanticsService.SendEvent(new FocusSemanticEvent(Find("target").Id));
        Assert.Same(Find("target"), Assert.Single(focusRequests));
    }

    [Fact]
    public void AnnouncementsForAnotherViewAreNotDelivered()
    {
        Pump(Box("target"));
        var announcements = new List<AccessibilityAnnouncement>();
        _bridge.AnnouncementRequested += announcements.Add;
        _bridge.Activate();

        var otherView = new FlutterView(new Size(10, 10), 1.0, _bridge.ViewId + 1000);
        _ = SemanticsService.SendAnnouncement(otherView, "elsewhere", TextDirection.Ltr);

        Assert.Empty(announcements);
    }

    // The iOS bridge's AccessibilityObjectDidBecomeFocused sends didGainFocus, which
    // ServicesBinding._handleAccessibilityMessage records as the accessibility focus.
    [Fact]
    public void ReportingAccessibilityFocusUpdatesTheServicesBinding()
    {
        Pump(Box("target"));
        int id = Find("target").Id;

        _bridge.ReportAccessibilityFocus(id);
        EventLoop.FlushMicrotasks();

        Assert.Equal(id, _bridge.LastFocusedId);
        Assert.Equal(id, ServicesBinding.Instance.AccessibilityFocus.Value);
    }

    [Fact]
    public void ClearDropsTheTree()
    {
        Pump(Box("target"));
        AccessibilityNode target = Find("target");
        int cleared = 0;
        _bridge.Cleared += () => cleared++;

        _bridge.Clear();

        Assert.Equal(1, cleared);
        Assert.Null(_bridge.Root);
        Assert.Equal(0, _bridge.NodeCount);
        Assert.False(target.IsAttached);
    }

    [Fact]
    public void CustomActionsResolveTheirLabels()
    {
        var action = new CustomSemanticsAction(label: "Archive");
        bool archived = false;
        Pump(new Semantics(
            container: true,
            label: "item",
            customSemanticsActions: new Dictionary<CustomSemanticsAction, Action> { [action] = () => archived = true },
            child: new SizedBox(width: 20, height: 20)));
        AccessibilityNode item = Find("item");

        CustomSemanticsActionUpdate custom = Assert.Single(_bridge.GetCustomActions(item));
        Assert.Equal("Archive", custom.Label);
        item.PerformAction(SemanticsActions.CustomAction, custom.Id);
        Assert.True(archived);
    }

    // semantics_event_test.dart: FocusSemanticEvent.toMap.
    [Fact]
    public void FocusSemanticEventToMap()
    {
        Dictionary<string, object?> map = new FocusSemanticEvent().ToMap();
        Assert.Equal("focus", map["type"]);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable>(map["data"]).Cast<object>());
        Assert.False(map.ContainsKey("nodeId"));
        Assert.Equal(123, new FocusSemanticEvent(123).ToMap()["nodeId"]);
    }
}

internal static class AccessibilityBridgeTestWidgets
{
    public static Widget WithKey(this Widget widget, Key key) => new KeyedSubtree(key: key, child: widget);
}
