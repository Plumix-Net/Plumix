using System.Text.Json;
using Plumix.Accessibility;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// C#-only infrastructure tests: the DOM description the browser accessibility bridge renders. The
// asserted behaviours are the web engine's semantics tests (semantics_test.dart, tappable_test.dart,
// text_field_test.dart, scrollable_test.dart), driven through real framework semantics updates.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class WebSemanticsTests : IDisposable
{
    private readonly AccessibilityBridgeHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private WebSemanticsElement Describe(string label) => WebSemantics.Describe(_harness.Find(label));

    private static Widget Leaf() => new SizedBox(width: 30, height: 20);

    // 'can switch role': link + button is a link; 'link': an <a> with href only when a URL is set.
    [Fact]
    public void ChoosesRolesInTheWebEngineOrder()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "link", link: true, button: true, child: Leaf()),
            new Semantics(
                container: true,
                label: "url",
                link: true,
                linkUrl: new Uri("https://flutter.dev"),
                child: Leaf()),
            new Semantics(container: true, label: "tappable", onTap: static () => { }, child: Leaf()),
            new Semantics(container: true, label: "field", textField: true, child: Leaf()),
            new Semantics(container: true, label: "slider", onIncrease: static () => { }, child: Leaf()),
            new Semantics(container: true, label: "image", image: true, child: Leaf()),
            new Semantics(container: true, label: "image button", image: true, button: true, child: Leaf()),
            new Semantics(container: true, label: "tab", role: SemanticsRole.Tab, button: true, child: Leaf()),
            new Semantics(container: true, label: "tooltip role", role: SemanticsRole.Tooltip, child: Leaf()),
        ]));

        WebSemanticsElement link = Describe("link");
        Assert.Equal(WebSemanticsRole.Link, link.Role);
        Assert.Equal("a", link.Tag);
        Assert.False(link.Attributes.ContainsKey("href"));
        Assert.Equal("link", link.Text);
        Assert.Equal("https://flutter.dev", Describe("url").Attributes["href"]);
        Assert.Equal(WebSemanticsRole.Button, Describe("tappable").Role);
        Assert.Equal(WebSemanticsRole.TextField, Describe("field").Role);
        Assert.Equal(WebSemanticsRole.Incrementable, Describe("slider").Role);
        Assert.Equal(WebSemanticsRole.Image, Describe("image").Role);
        Assert.Equal(WebSemanticsRole.Button, Describe("image button").Role);
        Assert.Equal(WebSemanticsRole.Tab, Describe("tab").Role);
        Assert.Equal("tab", Describe("tab").Attributes["role"]);
        Assert.Equal(WebSemanticsRole.Generic, Describe("tooltip role").Role);
    }

    // Header tests and the heading test.
    [Fact]
    public void RendersHeadingsAndHeaders()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "heading", headingLevel: 3, child: Leaf()),
            new Semantics(container: true, label: "labelled header", header: true, child: Leaf()),
            new Semantics(
                container: true,
                header: true,
                label: "header group",
                child: new Column(children: [new Semantics(container: true, label: "inside", child: Leaf())])),
        ]));

        WebSemanticsElement heading = Describe("heading");
        Assert.Equal("h3", heading.Tag);
        Assert.Equal("heading", heading.Text);
        Assert.False(heading.TextAsSpan);
        Assert.Equal("h2", Describe("labelled header").Tag);
        WebSemanticsElement header = Describe("header group");
        Assert.Equal("header", header.Tag);
        Assert.Equal("header group", header.Attributes["aria-label"]);
        Assert.Null(header.Text);
    }

    // 'produces a label', 'tooltip is part of label', LabelAndValue representations.
    [Fact]
    public void RendersLabels()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "Hello", child: Leaf()),
            new Semantics(container: true, label: "label", value: "value", tooltip: "tooltip", child: Leaf()),
            new Semantics(
                container: true,
                label: "group",
                child: new Column(children: [new Semantics(container: true, label: "child", child: Leaf())])),
            new Semantics(container: true, label: "described", hint: "a hint", child: Leaf()),
        ]));

        WebSemanticsElement hello = Describe("Hello");
        Assert.Equal("flt-semantics", hello.Tag);
        Assert.Equal("Hello", hello.Text);
        Assert.True(hello.TextAsSpan);
        Assert.False(hello.Attributes.ContainsKey("role"));
        Assert.Equal("tooltip\nlabel value", Describe("label").Text);
        WebSemanticsElement group = Describe("group");
        Assert.Equal("group", group.Attributes["role"]);
        Assert.Equal("group", group.Attributes["aria-label"]);
        Assert.Equal("a hint", Describe("described").Attributes["aria-description"]);
        Assert.Equal(
            "flt-semantic-node-" + _harness.Find("Hello").Id,
            hello.Attributes["id"]);
        Assert.Null(WebSemantics.ComputeDomLabel(_harness.Bridge.Root!, WebSemanticsRole.Generic));
    }

    // Checkable tests: role, aria-checked, flt-tappable only when enabled, aria-disabled when disabled.
    [Fact]
    public void RendersCheckables()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "check", @checked: true, onTap: static () => { }, child: Leaf()),
            new Semantics(container: true, label: "switch", toggled: false, onTap: static () => { }, child: Leaf()),
            new Semantics(
                container: true,
                label: "radio",
                @checked: false,
                inMutuallyExclusiveGroup: true,
                child: Leaf()),
            new Semantics(
                container: true,
                label: "disabled",
                @checked: false,
                enabled: false,
                onTap: static () => { },
                child: Leaf()),
        ]));

        WebSemanticsElement check = Describe("check");
        Assert.Equal("checkbox", check.Attributes["role"]);
        Assert.Equal("true", check.Attributes["aria-checked"]);
        Assert.True(check.Tappable);
        Assert.True(check.Attributes.ContainsKey("flt-tappable"));
        Assert.Equal("switch", Describe("switch").Attributes["role"]);
        Assert.Equal("false", Describe("switch").Attributes["aria-checked"]);
        Assert.Equal("radio", Describe("radio").Attributes["role"]);
        WebSemanticsElement disabled = Describe("disabled");
        Assert.Equal("true", disabled.Attributes["aria-disabled"]);
        Assert.False(disabled.Tappable);
        Assert.False(disabled.Attributes.ContainsKey("flt-tappable"));
    }

    // Tappable tests: role=button, flt-tappable, focusable buttons, aria-disabled when disabled.
    [Fact]
    public void RendersButtons()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "button",
                button: true,
                focusable: true,
                focused: true,
                onTap: static () => { },
                child: Leaf()),
            new Semantics(
                container: true,
                label: "off",
                button: true,
                enabled: false,
                onTap: static () => { },
                child: Leaf()),
        ]));

        WebSemanticsElement button = Describe("button");
        Assert.Equal("button", button.Attributes["role"]);
        Assert.Equal("button", button.Text);
        Assert.True(button.Tappable);
        Assert.True(button.Focusable);
        Assert.True(button.Focused);
        WebSemanticsElement off = Describe("off");
        Assert.Equal("true", off.Attributes["aria-disabled"]);
        Assert.False(off.Tappable);
    }

    // Selectable tests: rows and tabs use aria-selected, others aria-current; a checkable wins.
    [Fact]
    public void RendersSelectionAndExpansion()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "tab", role: SemanticsRole.Tab, selected: true, child: Leaf()),
            new Semantics(container: true, label: "item", selected: false, child: Leaf()),
            new Semantics(container: true, label: "checked", selected: true, @checked: true, child: Leaf()),
            new Semantics(container: true, label: "expander", expanded: true, child: Leaf()),
            new Semantics(container: true, label: "required", isRequired: true, child: Leaf()),
            new Semantics(
                container: true,
                label: "invalid",
                validationResult: SemanticsValidationResult.Invalid,
                child: Leaf()),
        ]));

        Assert.Equal("true", Describe("tab").Attributes["aria-selected"]);
        Assert.Equal("false", Describe("item").Attributes["aria-current"]);
        Assert.False(Describe("checked").Attributes.ContainsKey("aria-current"));
        Assert.False(Describe("checked").Attributes.ContainsKey("aria-selected"));
        Assert.Equal("true", Describe("expander").Attributes["aria-expanded"]);
        Assert.Equal("true", Describe("required").Attributes["aria-required"]);
        Assert.Equal("true", Describe("invalid").Attributes["aria-invalid"]);
    }

    // Image tests: a visual-only leaf is role=img; with children the label moves to an image child.
    [Fact]
    public void RendersImages()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "leaf image", image: true, child: Leaf()),
            new Semantics(
                container: true,
                label: "image parent",
                image: true,
                child: new Column(children: [new Semantics(container: true, label: "caption", child: Leaf())])),
        ]));

        WebSemanticsElement leaf = Describe("leaf image");
        Assert.Equal("img", leaf.Attributes["role"]);
        Assert.Equal("leaf image", leaf.Attributes["aria-label"]);
        WebSemanticsElement parent = Describe("image parent");
        Assert.False(parent.Attributes.ContainsKey("role"));
        Assert.Equal("image parent", parent.ImageLabel);
    }

    // Route and dialog tests.
    [Fact]
    public void RendersRoutesAndDialogs()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "route",
                scopesRoute: true,
                namesRoute: true,
                explicitChildNodes: true,
                child: Leaf()),
            new Semantics(container: true, label: "dialog", role: SemanticsRole.Dialog, child: Leaf()),
        ]));

        WebSemanticsElement route = Describe("route");
        Assert.Equal(WebSemanticsRole.Route, route.Role);
        Assert.Equal("route", route.Attributes["aria-label"]);
        Assert.False(route.Attributes.ContainsKey("role"));
        WebSemanticsElement dialog = Describe("dialog");
        Assert.Equal("dialog", dialog.Attributes["role"]);
        Assert.Equal("true", dialog.Attributes["aria-modal"]);
    }

    [Fact]
    public void RendersProgressBars()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(
                container: true,
                label: "percent",
                value: "25%",
                minValue: "0",
                maxValue: "200",
                role: SemanticsRole.ProgressBar,
                child: Leaf()),
            new Semantics(
                container: true,
                label: "number",
                value: "0.5",
                role: SemanticsRole.ProgressBar,
                child: Leaf()),
        ]));

        WebSemanticsElement percent = Describe("percent");
        Assert.Equal("progressbar", percent.Attributes["role"]);
        Assert.Equal("50", percent.Attributes["aria-valuenow"]);
        Assert.Equal("25%", percent.Attributes["aria-valuetext"]);
        Assert.Equal("0.5", Describe("number").Attributes["aria-valuenow"]);
    }

    [Fact]
    public void WritesTheUpdateAsJson()
    {
        _harness.Pump(new Column(children:
        [
            new Semantics(container: true, label: "first", child: Leaf()),
            new Semantics(
                container: true,
                label: "slider",
                value: "5",
                increasedValue: "6",
                onIncrease: static () => { },
                child: Leaf()),
            new Semantics(
                container: true,
                label: "list",
                child: new SizedBox(
                    height: 100,
                    child: ListView.Builder(
                        itemCount: 20,
                        itemExtent: 40,
                        itemBuilder: (_, index) => new Text($"Item {index}")))),
        ]));
        AccessibilityBridge bridge = _harness.Bridge;

        string json = WebSemantics.WriteUpdate(bridge, WebSemantics.AllNodes(bridge), [99]);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        Assert.Equal(bridge.Root!.Id, root.GetProperty("root").GetInt32());
        Assert.Equal(2.0, root.GetProperty("devicePixelRatio").GetDouble());
        Assert.Equal(99, Assert.Single(root.GetProperty("removed").EnumerateArray()).GetInt32());
        JsonElement[] nodes = root.GetProperty("nodes").EnumerateArray().ToArray();
        Assert.Equal(bridge.NodeCount, nodes.Length);
        Assert.Equal(bridge.Root.Id, nodes[0].GetProperty("id").GetInt32());

        JsonElement first = nodes.Single(n => n.GetProperty("id").GetInt32() == _harness.Find("first").Id);
        Assert.Equal("flt-semantics", first.GetProperty("tag").GetString());
        Assert.Equal("generic", first.GetProperty("kind").GetString());
        Assert.Equal("first", first.GetProperty("text").GetString());
        Assert.Equal(4, first.GetProperty("rect").GetArrayLength());

        JsonElement slider = nodes.Single(n => n.GetProperty("id").GetInt32() == _harness.Find("slider").Id);
        Assert.Equal("incrementable", slider.GetProperty("kind").GetString());
        Assert.Equal("5", slider.GetProperty("range").GetProperty("value").GetString());
        Assert.True(slider.GetProperty("range").GetProperty("canIncrease").GetBoolean());
        Assert.False(slider.GetProperty("range").GetProperty("canDecrease").GetBoolean());

        JsonElement scrollable = nodes.Single(n => n.GetProperty("kind").GetString() == "scrollable");
        Assert.Equal("vertical", scrollable.GetProperty("scroll").GetProperty("axis").GetString());
        Assert.Equal("group", scrollable.GetProperty("attrs").GetProperty("role").GetString());
        Assert.True(scrollable.GetProperty("scroll").GetProperty("extentMax").GetDouble() > 0);

        // The root's rect is the view in physical pixels; its children carry the device-pixel-ratio scale.
        JsonElement rootNode = nodes[0];
        Assert.Equal(1600, rootNode.GetProperty("rect")[2].GetDouble());
        JsonElement scaled = nodes.First(n => n.TryGetProperty("transform", out _));
        JsonElement transform = scaled.GetProperty("transform");
        Assert.Equal(16, transform.GetArrayLength());
        Assert.Equal(2.0, transform[0].GetDouble());
    }

    [Fact]
    public void ParsesTheScriptsActions()
    {
        Assert.Equal((SemanticsActions.Tap, (object?)null), WebSemantics.ParseAction("tap", ""));
        Assert.Equal(SemanticsActions.Focus, WebSemantics.ParseAction("focus", "")!.Value.Action);
        Assert.Equal(SemanticsActions.Increase, WebSemantics.ParseAction("increase", "")!.Value.Action);
        Assert.Equal(SemanticsActions.Decrease, WebSemantics.ParseAction("decrease", "")!.Value.Action);
        (SemanticsActions action, object? arguments) = WebSemantics.ParseAction("scrollToOffset", "0,20")!.Value;
        Assert.Equal(SemanticsActions.ScrollToOffset, action);
        Assert.Equal([0.0, 20.0], Assert.IsType<double[]>(arguments));
        Assert.Null(WebSemantics.ParseAction("unknown", ""));
    }
}
