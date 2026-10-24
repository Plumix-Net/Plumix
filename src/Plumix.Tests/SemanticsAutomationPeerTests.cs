using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Plumix.Accessibility;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only infrastructure tests: the Avalonia automation peers a PlumixHost exposes to UI Automation,
// the macOS accessibility API, AT-SPI and Android. Activation on the first children query follows the
// Windows embedder's WM_GETOBJECT rule; the action mapping follows flutter_platform_node_delegate.cc.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SemanticsAutomationPeerTests : IDisposable
{
    private static readonly Size Viewport = new(320, 480);
    private readonly WidgetHost _host = new();

    public void Dispose()
    {
        _host.RootWidget = null;
        SchedulerBinding.PumpFrameForTests();
        AccessibilityBridge.ResetForTests();
        PlatformDispatcher.Instance.UpdateSemanticsEnabled(false);
    }

    private PlumixHostAutomationPeer Show(Widget widget)
    {
        _host.RootWidget = new Directionality(TextDirection.Ltr, widget);
        _host.PumpFrameForTests(Viewport);
        var peer = Assert.IsType<PlumixHostAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(_host));
        _ = peer.GetChildren();
        _host.PumpFrameForTests(Viewport);
        return peer;
    }

    private void Update(Widget widget)
    {
        _host.RootWidget = new Directionality(TextDirection.Ltr, widget);
        _host.PumpFrameForTests(Viewport);
    }

    private static SemanticsNodeAutomationPeer Find(AutomationPeer root, string name)
    {
        SemanticsNodeAutomationPeer? found = FindOrNull(root, name);
        Assert.NotNull(found);
        return found!;
    }

    private static SemanticsNodeAutomationPeer? FindOrNull(AutomationPeer root, string name)
    {
        foreach (AutomationPeer child in root.GetChildren())
        {
            if (child is SemanticsNodeAutomationPeer node && node.GetName() == name)
            {
                return node;
            }

            if (FindOrNull(child, name) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private static Widget Leaf() => new SizedBox(width: 40, height: 20);

    [Fact]
    public void FirstChildrenQueryTurnsSemanticsOn()
    {
        _host.RootWidget = new Directionality(
            TextDirection.Ltr,
            new Semantics(container: true, label: "hello", child: Leaf()));
        _host.PumpFrameForTests(Viewport);
        Assert.False(PlatformDispatcher.Instance.SemanticsEnabled);
        Assert.Null(_host.SemanticsRoot);

        var peer = (PlumixHostAutomationPeer)ControlAutomationPeer.CreatePeerForElement(_host);
        int childrenChanged = 0;
        peer.ChildrenChanged += (_, _) => childrenChanged++;
        Assert.Empty(peer.GetChildren());

        Assert.True(PlatformDispatcher.Instance.SemanticsEnabled);
        Assert.True(_host.AccessibilityBridge.IsActive);
        _host.PumpFrameForTests(Viewport);

        Assert.True(childrenChanged > 0);
        AutomationPeer root = Assert.Single(peer.GetChildren());
        Assert.Same(peer, root.GetParent());
        Assert.Equal("hello", Find(peer, "hello").GetName());
        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
    }

    [Fact]
    public void ButtonInvokesTap()
    {
        int taps = 0;
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            button: true,
            label: "Send",
            hint: "Sends the message",
            identifier: "send-button",
            onTap: () => taps++,
            child: Leaf()));

        SemanticsNodeAutomationPeer button = Find(host, "Send");
        Assert.Equal(AutomationControlType.Button, button.GetAutomationControlType());
        Assert.Equal("Sends the message", button.GetHelpText());
        Assert.Equal("send-button", button.GetAutomationId());
        Assert.Null(button.GetProvider<IToggleProvider>());
        Assert.True(button.IsEnabled());

        button.GetProvider<IInvokeProvider>()!.Invoke();

        Assert.Equal(1, taps);
    }

    [Fact]
    public void DisabledButtonRejectsInvoke()
    {
        int taps = 0;
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            button: true,
            enabled: false,
            label: "Off",
            onTap: () => taps++,
            child: Leaf()));

        SemanticsNodeAutomationPeer button = Find(host, "Off");
        Assert.False(button.IsEnabled());
        Assert.Throws<ElementNotEnabledException>(() => button.GetProvider<IInvokeProvider>()!.Invoke());
        Assert.Equal(0, taps);
    }

    [Fact]
    public void CheckboxAndSwitchToggle()
    {
        bool value = false;
        int toggles = 0;
        Widget Build() => new Column(children:
        [
            new Semantics(container: true, label: "Check", @checked: value, onTap: () => toggles++, child: Leaf()),
            new Semantics(container: true, label: "Switch", toggled: value, onTap: () => toggles++, child: Leaf()),
        ]);
        PlumixHostAutomationPeer host = Show(Build());

        SemanticsNodeAutomationPeer check = Find(host, "Check");
        SemanticsNodeAutomationPeer toggle = Find(host, "Switch");
        Assert.Equal(AutomationControlType.CheckBox, check.GetAutomationControlType());
        Assert.Equal(AutomationControlType.Button, toggle.GetAutomationControlType());
        Assert.Null(check.GetProvider<IInvokeProvider>());
        Assert.Equal(ToggleState.Off, check.GetProvider<IToggleProvider>()!.ToggleState);

        var changed = new List<AutomationProperty>();
        check.PropertyChanged += (_, e) => changed.Add(e.Property);
        check.GetProvider<IToggleProvider>()!.Toggle();
        toggle.GetProvider<IToggleProvider>()!.Toggle();
        Assert.Equal(2, toggles);

        value = true;
        Update(Build());
        Assert.Equal(ToggleState.On, Find(host, "Check").GetProvider<IToggleProvider>()!.ToggleState);
        Assert.Equal(ToggleState.On, Find(host, "Switch").GetProvider<IToggleProvider>()!.ToggleState);
        Assert.Contains(TogglePatternIdentifiers.ToggleStateProperty, changed);
    }

    [Fact]
    public void RadioButtonIsASelectionItem()
    {
        int taps = 0;
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "Option",
            @checked: false,
            inMutuallyExclusiveGroup: true,
            onTap: () => taps++,
            child: Leaf()));

        SemanticsNodeAutomationPeer radio = Find(host, "Option");
        Assert.Equal(AutomationControlType.RadioButton, radio.GetAutomationControlType());
        ISelectionItemProvider selection = radio.GetProvider<ISelectionItemProvider>()!;
        Assert.False(selection.IsSelected);
        selection.Select();
        Assert.Equal(1, taps);
        selection.RemoveFromSelection();
        Assert.Equal(1, taps);
    }

    [Fact]
    public void SelectedStateIsReportedWithoutATapAction()
    {
        int taps = 0;
        PlumixHostAutomationPeer host = Show(new Column(children:
        [
            new Semantics(container: true, label: "Current", selected: true, child: Leaf()),
            new Semantics(container: true, label: "Other", selected: false, onTap: () => taps++, child: Leaf()),
        ]));

        SemanticsNodeAutomationPeer current = Find(host, "Current");
        Assert.True(current.GetProvider<ISelectionItemProvider>()!.IsSelected);
        Assert.Null(current.GetProvider<IInvokeProvider>());

        // A tappable selectable item stays invocable, which is what makes it clickable on Android.
        SemanticsNodeAutomationPeer other = Find(host, "Other");
        Assert.False(other.GetProvider<ISelectionItemProvider>()!.IsSelected);
        other.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, taps);
    }

    [Fact]
    public void SliderMovesOneStepTowardTheRequestedValue()
    {
        var log = new List<string>();
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "Volume",
            slider: true,
            value: "50%",
            increasedValue: "60%",
            decreasedValue: "40%",
            onIncrease: () => log.Add("increase"),
            onDecrease: () => log.Add("decrease"),
            child: Leaf()));

        SemanticsNodeAutomationPeer slider = Find(host, "Volume");
        Assert.Equal(AutomationControlType.Slider, slider.GetAutomationControlType());
        IRangeValueProvider range = slider.GetProvider<IRangeValueProvider>()!;
        Assert.Equal(0, range.Minimum);
        Assert.Equal(100, range.Maximum);
        Assert.Equal(50, range.Value);
        Assert.Equal(10, range.SmallChange);
        Assert.False(range.IsReadOnly);

        range.SetValue(90);
        range.SetValue(10);
        range.SetValue(50);

        Assert.Equal(["increase", "decrease"], log);
    }

    [Fact]
    public void TextFieldSetsText()
    {
        string? text = null;
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "Name",
            textField: true,
            value: "Ada",
            onSetText: value => text = value,
            child: Leaf()));

        SemanticsNodeAutomationPeer field = Find(host, "Name");
        Assert.Equal(AutomationControlType.Edit, field.GetAutomationControlType());
        IValueProvider value = field.GetProvider<IValueProvider>()!;
        Assert.Equal("Ada", value.Value);
        Assert.False(value.IsReadOnly);

        value.SetValue("Grace");

        Assert.Equal("Grace", text);
    }

    [Fact]
    public void ExpandCollapseUsesExpansionActionsBeforeTap()
    {
        var log = new List<string>();
        PlumixHostAutomationPeer host = Show(new Column(children:
        [
            new Semantics(
                container: true,
                label: "Tapped",
                expanded: false,
                onTap: () => log.Add("tap"),
                child: Leaf()),
            new Semantics(
                container: true,
                label: "Actions",
                expanded: true,
                onTap: () => log.Add("actions tap"),
                onCollapse: () => log.Add("collapse"),
                child: Leaf()),
        ]));

        IExpandCollapseProvider tapped = Find(host, "Tapped").GetProvider<IExpandCollapseProvider>()!;
        IExpandCollapseProvider actions = Find(host, "Actions").GetProvider<IExpandCollapseProvider>()!;
        Assert.Equal(ExpandCollapseState.Collapsed, tapped.ExpandCollapseState);
        Assert.Equal(ExpandCollapseState.Expanded, actions.ExpandCollapseState);
        Assert.NotNull(Find(host, "Tapped").GetProvider<IInvokeProvider>());
        Assert.NotNull(Find(host, "Actions").GetProvider<IInvokeProvider>());

        tapped.Collapse();
        tapped.Expand();
        actions.Expand();
        actions.Collapse();

        Assert.Equal(["tap", "collapse"], log);
    }

    [Fact]
    public void ScrollProviderScrollsTowardTheEnd()
    {
        var controller = new ScrollController();
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "List",
            child: new SizedBox(
                height: 200,
                child: ListView.Builder(
                    controller: controller,
                    itemCount: 50,
                    itemExtent: 40,
                    itemBuilder: (_, index) => new Text($"Item {index}")))));

        SemanticsNodeAutomationPeer? scrollable = FindScrollable(host);
        Assert.NotNull(scrollable);
        IScrollProvider scroll = scrollable!.GetProvider<IScrollProvider>()!;
        Assert.True(scroll.VerticallyScrollable);
        Assert.False(scroll.HorizontallyScrollable);
        Assert.Equal(0, scroll.VerticalScrollPercent);

        scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
        _host.PumpFrameForTests(Viewport);
        for (int i = 0; i < 30; i++)
        {
            _host.PumpFrameForTests(Viewport);
        }

        Assert.True(controller.Offset > 0);
    }

    private static SemanticsNodeAutomationPeer? FindScrollable(AutomationPeer root)
    {
        foreach (AutomationPeer child in root.GetChildren())
        {
            if (child is SemanticsNodeAutomationPeer node && node.GetProvider<IScrollProvider>() is not null)
            {
                return node;
            }

            if (FindScrollable(child) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    [Fact]
    public void GainingAnActionReplacesThePeerWithOneOfTheNewShape()
    {
        PlumixHostAutomationPeer host = Show(new Semantics(container: true, label: "Item", child: Leaf()));
        SemanticsNodeAutomationPeer before = Find(host, "Item");
        Assert.Null(before.GetProvider<IInvokeProvider>());

        Update(new Semantics(container: true, label: "Item", onTap: static () => { }, child: Leaf()));

        SemanticsNodeAutomationPeer after = Find(host, "Item");
        Assert.NotSame(before, after);
        Assert.NotNull(after.GetProvider<IInvokeProvider>());
        Assert.Same(before.Node, after.Node);
    }

    [Fact]
    public void LabelChangesRaiseNamePropertyChanged()
    {
        PlumixHostAutomationPeer host = Show(new Column(children:
        [
            new Semantics(container: true, label: "Before", child: Leaf()),
        ]));
        SemanticsNodeAutomationPeer peer = Find(host, "Before");
        var changed = new List<AutomationProperty>();
        peer.PropertyChanged += (_, e) => changed.Add(e.Property);

        Update(new Column(children: [new Semantics(container: true, label: "After", child: Leaf())]));

        Assert.Contains(AutomationElementIdentifiers.NameProperty, changed);
        Assert.Equal("After", peer.GetName());
    }

    [Fact]
    public void ReportsNamesHeadingsLandmarksAndBounds()
    {
        PlumixHostAutomationPeer host = Show(new Column(children:
        [
            new Semantics(container: true, label: "Title", header: true, child: Leaf()),
            new Semantics(container: true, label: "Level 3", headingLevel: 3, child: Leaf()),
            new Semantics(container: true, label: "Tip", tooltip: "More", child: Leaf()),
            new Semantics(container: true, label: "Main", role: SemanticsRole.Main, child: Leaf()),
            new Semantics(
                container: true, label: "Tab", role: SemanticsRole.Tab, selected: false,
                onTap: static () => { }, child: Leaf()),
            new Semantics(container: true, label: "Live", liveRegion: true, child: Leaf()),
        ]));

        Assert.Equal(2, Find(host, "Title").GetHeadingLevel());
        Assert.Equal(AutomationControlType.Text, Find(host, "Title").GetAutomationControlType());
        Assert.Equal(3, Find(host, "Level 3").GetHeadingLevel());
        Assert.NotNull(FindOrNull(host, "Tip\nMore"));
        Assert.Equal(AutomationLandmarkType.Main, Find(host, "Main").GetLandmarkType());
        Assert.Equal(AutomationControlType.TabItem, Find(host, "Tab").GetAutomationControlType());
        Assert.Equal(AutomationLiveSetting.Polite, Find(host, "Live").GetLiveSetting());
        Assert.Equal(AutomationLiveSetting.Off, Find(host, "Title").GetLiveSetting());

        Rect bounds = Find(host, "Level 3").GetBoundingRectangle();
        Assert.Equal(new Rect(140, 20, 40, 20), bounds);
        Assert.False(Find(host, "Level 3").IsOffscreen());
    }

    [Fact]
    public void FocusAndBringIntoViewAreActions()
    {
        var log = new List<string>();
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "Target",
            focusable: true,
            focused: true,
            onDidGainAccessibilityFocus: () => log.Add("gain"),
            child: Leaf()));

        SemanticsNodeAutomationPeer target = Find(host, "Target");
        Assert.True(target.HasKeyboardFocus());
        Assert.True(target.IsKeyboardFocusable());

        target.SetFocus();
        Assert.Equal(["gain"], log);
        Assert.Equal(target.Node.Id, _host.AccessibilityBridge.LastFocusedId);
    }

    [Fact]
    public void AnnouncementsGoThroughAHiddenLiveRegion()
    {
        PlumixHostAutomationPeer host = Show(new Semantics(container: true, label: "Content", child: Leaf()));
        int childrenChanged = 0;
        host.ChildrenChanged += (_, _) => childrenChanged++;

        _ = SemanticsService.SendAnnouncement(_host.RootFlutterView, "Saved", TextDirection.Ltr);

        Assert.True(childrenChanged > 0);
        AnnouncementAutomationPeer announcement =
            Assert.Single(host.GetChildren().OfType<AnnouncementAutomationPeer>());
        Assert.Equal("Saved", announcement.GetName());
        Assert.Equal(AutomationLiveSetting.Polite, announcement.GetLiveSetting());
        Assert.True(announcement.IsOffscreen());
    }

    [Fact]
    public void IgnoredNodesAreLeftOut()
    {
        PlumixHostAutomationPeer host = Show(new Semantics(
            container: true,
            label: "Parent",
            child: new Column(children:
            [
                new Semantics(container: true, child: Leaf()),
                new Semantics(container: true, label: "Visible", child: Leaf()),
            ])));

        SemanticsNodeAutomationPeer parent = Find(host, "Parent");
        AutomationPeer child = Assert.Single(parent.GetChildren());
        Assert.Equal("Visible", child.GetName());
    }
}
