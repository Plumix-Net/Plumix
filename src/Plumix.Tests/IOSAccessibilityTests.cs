using Plumix.Accessibility;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only infrastructure tests: the UIKit-free half of the iOS accessibility bridge. The asserted
// behaviours are the engine's SemanticsObjectTest.mm and accessibility_bridge_test.mm cases, driven
// through real framework semantics updates.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class IOSAccessibilityTests : IDisposable
{
    private readonly AccessibilityBridgeHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private AccessibilityNode Find(string label) => _harness.Find(label);

    private static Widget Leaf() => new SizedBox(width: 30, height: 20);

    // testPlainSemanticsObjectWithLabelHasStaticTextTrait and the three "Intresting" variants.
    [Fact]
    public void StaticTextTraitOnlyForPlainLabelledLeaves()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "plain", child: Leaf()),
            new Semantics(
                container: true,
                label: "parent",
                child: new Column(children: [new Semantics(container: true, label: "child", child: Leaf())])),
            new Semantics(container: true, label: "field", textField: true, readOnly: true, child: Leaf()),
            new Semantics(container: true, label: "button", button: true, child: Leaf()),
        ]));

        Assert.Equal(IOSAccessibilityTraits.StaticText, IOSSemanticsObject.Traits(Find("plain")));
        Assert.Equal(IOSAccessibilityTraits.None, IOSSemanticsObject.Traits(Find("parent")));
        Assert.Equal(IOSAccessibilityTraits.None, IOSSemanticsObject.Traits(Find("field")));
        Assert.Equal(IOSAccessibilityTraits.Button, IOSSemanticsObject.Traits(Find("button")));
    }

    // testPlainSemanticsObjectWithHeadingLevelHasHeaderTrait; the header flag alone sets no trait.
    [Fact]
    public void HeadingLevelGivesTheHeaderTrait()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "heading", headingLevel: 2, child: Leaf()),
            new Semantics(container: true, label: "header", header: true, child: Leaf()),
        ]));

        Assert.True(IOSSemanticsObject.Traits(Find("heading")).HasFlag(IOSAccessibilityTraits.Header));
        Assert.False(IOSSemanticsObject.Traits(Find("header")).HasFlag(IOSAccessibilityTraits.Header));
    }

    [Fact]
    public void MapsFlagsAndActionsToTraits()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "adjustable",
                value: "5",
                onIncrease: static () => { },
                child: Leaf()),
            new Semantics(container: true, label: "selected", selected: true, child: Leaf()),
            new Semantics(container: true, label: "disabled", button: true, enabled: false, child: Leaf()),
            new Semantics(container: true, label: "image", image: true, child: Leaf()),
            new Semantics(container: true, label: "live", liveRegion: true, child: Leaf()),
            new Semantics(container: true, label: "link", link: true, child: Leaf()),
        ]));

        Assert.Equal(IOSAccessibilityTraits.Adjustable, IOSSemanticsObject.Traits(Find("adjustable")));
        Assert.Equal(IOSAccessibilityTraits.Selected, IOSSemanticsObject.Traits(Find("selected")));
        Assert.Equal(
            IOSAccessibilityTraits.Button | IOSAccessibilityTraits.NotEnabled,
            IOSSemanticsObject.Traits(Find("disabled")));
        Assert.Equal(IOSAccessibilityTraits.Image, IOSSemanticsObject.Traits(Find("image")));
        Assert.Equal(IOSAccessibilityTraits.UpdatesFrequently, IOSSemanticsObject.Traits(Find("live")));
        Assert.Equal(IOSAccessibilityTraits.Link, IOSSemanticsObject.Traits(Find("link")));
    }

    // testFlutterSemanticsObjectOfRadioButton, testRadioButtonIsNotSwitchButton.
    [Fact]
    public void RadioButtonIsAPlainButtonWithoutValue()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "radio",
                @checked: false,
                inMutuallyExclusiveGroup: true,
                child: Leaf()),
            new Semantics(container: true, label: "switch", toggled: true, child: Leaf()),
            new Semantics(container: true, label: "check", @checked: false, child: Leaf()),
        ]));

        AccessibilityNode radio = Find("radio");
        Assert.Equal(IOSSemanticsObjectKind.Plain, IOSSemanticsObject.KindOf(radio));
        Assert.Equal(IOSAccessibilityTraits.Button, IOSSemanticsObject.Traits(radio));
        Assert.Null(IOSSemanticsObject.Value(radio));
        Assert.Equal(IOSSemanticsObjectKind.Switch, IOSSemanticsObject.KindOf(Find("switch")));
        Assert.Equal("1", IOSSemanticsObject.Value(Find("switch")));
        Assert.Equal("0", IOSSemanticsObject.Value(Find("check")));
    }

    [Fact]
    public void ChoosesTheObjectKind()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "field", textField: true, child: Leaf()),
            new Semantics(container: true, label: "read only", textField: true, readOnly: true, child: Leaf()),
            new Semantics(container: true, label: "plain", child: Leaf()),
        ]));

        Assert.Equal(IOSSemanticsObjectKind.TextInput, IOSSemanticsObject.KindOf(Find("field")));
        Assert.Equal(IOSSemanticsObjectKind.Plain, IOSSemanticsObject.KindOf(Find("read only")));
        Assert.Equal(IOSSemanticsObjectKind.Plain, IOSSemanticsObject.KindOf(Find("plain")));
    }

    // testFlutterSemanticsObjectMergeTooltipToLabel.
    [Fact]
    public void MergesTheTooltipIntoTheLabel()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "label", tooltip: "tooltip", child: Leaf()),
            new Semantics(container: true, label: "hinted", hint: "hint", child: Leaf()),
        ]));

        AccessibilityNode node = Find("label");
        Assert.Equal("label\ntooltip", IOSSemanticsObject.Label(node));
        Assert.True(IOSSemanticsObject.IsAccessibilityElement(node));
        Assert.Equal("hint", IOSSemanticsObject.Hint(Find("hinted")));
        Assert.Null(IOSSemanticsObject.Hint(node));
    }

    // testSemanticObjectWith(No)AccessibilityFlag..., testLabeledParentAndChildNotInteractive.
    [Fact]
    public void OnlyNodesWithActionsRespondToUserInteraction()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "tap", onTap: static () => { }, child: Leaf()),
            new Semantics(container: true, label: "label", child: Leaf()),
            new Semantics(
                container: true,
                label: "focus only",
                onDidGainAccessibilityFocus: static () => { },
                child: Leaf()),
        ]));

        Assert.True(IOSSemanticsObject.RespondsToUserInteraction(Find("tap")));
        Assert.False(IOSSemanticsObject.RespondsToUserInteraction(Find("label")));
        Assert.False(IOSSemanticsObject.RespondsToUserInteraction(Find("focus only")));
    }

    [Fact]
    public void RouteScopesAreNotElements()
    {
        _harness.Pump(new Semantics(
            container: true,
            label: "route",
            scopesRoute: true,
            explicitChildNodes: true,
            child: new Semantics(container: true, label: "inside", child: Leaf())));

        Assert.False(IOSSemanticsObject.IsAccessibilityElement(Find("route")));
        Assert.True(IOSSemanticsObject.IsAccessibilityElement(Find("inside")));
    }

    // testSliderSemanticsObject: activation without a tap action is swallowed by a slider.
    [Fact]
    public void ActivationTapsOrIsSwallowedBySliders()
    {
        int taps = 0;
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "tap", onTap: () => taps++, child: Leaf()),
            new Semantics(container: true, label: "slider", slider: true, child: Leaf()),
            new Semantics(container: true, label: "plain", child: Leaf()),
        ]));

        Assert.True(IOSSemanticsObject.Activate(Find("tap")));
        Assert.Equal(1, taps);
        Assert.True(IOSSemanticsObject.Activate(Find("slider")));
        Assert.False(IOSSemanticsObject.Activate(Find("plain")));
    }

    [Fact]
    public void MapsScrollDirectionsAndEscape()
    {
        var log = new List<string>();
        _harness.Pump(new Semantics(
            container: true,
            label: "scroller",
            onScrollUp: () => log.Add("up"),
            onScrollLeft: () => log.Add("left"),
            onDismiss: () => log.Add("dismiss"),
            child: Leaf()));
        AccessibilityNode node = Find("scroller");

        static SemanticsActions For(IOSAccessibilityScrollDirection direction) =>
            IOSSemanticsObject.ScrollActionFor(direction);
        Assert.Equal(SemanticsActions.ScrollRight, For(IOSAccessibilityScrollDirection.Right));
        Assert.Equal(SemanticsActions.ScrollRight, For(IOSAccessibilityScrollDirection.Previous));
        Assert.Equal(SemanticsActions.ScrollLeft, For(IOSAccessibilityScrollDirection.Next));
        Assert.Equal(SemanticsActions.ScrollDown, For(IOSAccessibilityScrollDirection.Up));
        Assert.Equal(SemanticsActions.ScrollUp, For(IOSAccessibilityScrollDirection.Down));

        Assert.True(IOSSemanticsObject.Scroll(node, IOSAccessibilityScrollDirection.Down));
        Assert.True(IOSSemanticsObject.Scroll(node, IOSAccessibilityScrollDirection.Next));
        Assert.False(IOSSemanticsObject.Scroll(node, IOSAccessibilityScrollDirection.Up));
        Assert.True(IOSSemanticsObject.PerformEscape(node));
        Assert.Equal(["up", "left", "dismiss"], log);
    }

    // testShouldDispatchShowOnScreenActionForHeader/Hidden, testAccessibilityObjectDidBecomeFocused.
    [Fact]
    public void BecomingFocusedReportsFocusAndShowsHeadersOnScreen()
    {
        var log = new List<string>();
        _harness.Pump(new Semantics(
            container: true,
            label: "header",
            header: true,
            onDidGainAccessibilityFocus: () => log.Add("gain"),
            onDidLoseAccessibilityFocus: () => log.Add("lose"),
            child: Leaf()));
        AccessibilityNode header = Find("header");
        var actions = new List<SemanticsActions>();
        SemanticsBinding.Instance.AddSemanticsActionListener(OnAction);
        try
        {
            IOSSemanticsObject.DidBecomeFocused(header);
            EventLoop.FlushMicrotasks();

            Assert.Equal([SemanticsActions.ShowOnScreen, SemanticsActions.DidGainAccessibilityFocus], actions);
            Assert.Equal(["gain"], log);
            Assert.Equal(header.Id, _harness.Bridge.LastFocusedId);
            Assert.Equal(header.Id, ServicesBinding.Instance.AccessibilityFocus.Value);

            IOSSemanticsObject.DidLoseFocus(header);
            Assert.Equal(-1, _harness.Bridge.LastFocusedId);
            Assert.Equal(["gain", "lose"], log);
        }
        finally
        {
            SemanticsBinding.Instance.RemoveSemanticsActionListener(OnAction);
        }

        void OnAction(SemanticsActionEvent action) => actions.Add(action.Type);
    }

    // testAnnouncesRouteChanges: the namesRoute child of the last new route names the screen.
    [Fact]
    public void AnnouncesRouteChanges()
    {
        var notifier = new IOSAccessibilityNotifier();
        _harness.Pump(Route("node1", child: null));
        IOSAccessibilityNotifications first = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        Assert.True(first.ScreenChanged);
        Assert.Equal("node1", first.RouteName);

        _harness.Pump(Route("node1", child: Route("node3", child: null)));
        IOSAccessibilityNotifications second = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        Assert.True(second.ScreenChanged);
        Assert.Equal("node3", second.RouteName);

        _harness.Pump(Route("node1", child: Route("node3 renamed", child: null)));
        IOSAccessibilityNotifications third = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        Assert.False(third.ScreenChanged);
    }

    private static Widget Route(string name, Widget? child) => new Semantics(
        container: true,
        scopesRoute: true,
        explicitChildNodes: true,
        child: new Column(children:
        [
            new Semantics(container: true, label: name, namesRoute: true, child: Leaf()),
            .. child is null ? Array.Empty<Widget>() : [child],
        ]));

    // testAnnouncesLayoutChangeWithNilIfLastFocusIsRemoved / ...WithTheSameItemFocused.
    [Fact]
    public void LayoutChangesMoveFocusOnlyWhenTheFocusedNodeIsGone()
    {
        var notifier = new IOSAccessibilityNotifier();
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "first", child: Leaf()),
            new Semantics(container: true, label: "focused", child: Leaf()),
        ]));
        _ = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        AccessibilityNode focused = Find("focused");
        _harness.Bridge.ReportAccessibilityFocus(focused.Id);

        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "first", child: new SizedBox(width: 30, height: 25)),
            new Semantics(container: true, label: "focused", child: Leaf()),
        ]));
        IOSAccessibilityNotifications same = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        Assert.True(same.LayoutChanged);
        Assert.Null(same.FocusTarget);

        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "first", child: Leaf()),
        ]));
        IOSAccessibilityNotifications removed = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);
        Assert.True(removed.LayoutChanged);
        Assert.Equal("first", removed.FocusTarget?.Label);
    }

    [Fact]
    public void NothingFocusedMeansNoFocusTarget()
    {
        var notifier = new IOSAccessibilityNotifier();
        _harness.Pump(new Semantics(container: true, label: "only", child: Leaf()));

        IOSAccessibilityNotifications plan = notifier.Plan(_harness.Bridge, _harness.Updates[^1]);

        Assert.True(plan.LayoutChanged);
        Assert.Null(plan.FocusTarget);
        Assert.Empty(plan.Announcements);
    }

    // testShouldTriggerAnnouncement: a new live region, or a new label on one, is announced.
    [Fact]
    public void AnnouncesLiveRegionLabels()
    {
        var notifier = new IOSAccessibilityNotifier();
        _harness.Pump(new Semantics(container: true, label: "Loading", liveRegion: true, child: Leaf()));
        Assert.Equal(["Loading"], notifier.Plan(_harness.Bridge, _harness.Updates[^1]).Announcements);

        _harness.Pump(new Semantics(container: true, label: "Loading", liveRegion: true, child: Leaf()));
        int count = _harness.Updates.Count;

        _harness.Pump(new Semantics(container: true, label: "Done", liveRegion: true, child: Leaf()));
        Assert.True(_harness.Updates.Count > count);
        Assert.Equal(["Done"], notifier.Plan(_harness.Bridge, _harness.Updates[^1]).Announcements);
    }
}
