using Avalonia;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/widget_inspector_test.dart
//
// The `ext.flutter.inspector.*` tests of widget_inspector_test.dart (and of
// widget_inspector_structure_error_test.dart) that run without `--track-widget-creation`. Golden
// comparisons of the screenshot tests are replaced by their non-image assertions and by the pixel
// sizes the screenshot's pixel-ratio math produces.

namespace Plumix.Tests;

public sealed class WidgetInspectorServiceExtensionsDartParityTests : IDisposable
{
    private readonly WidgetInspectorService _previousService = WidgetInspectorService.Instance;
    private readonly List<FrameworkDartTester> _testers = [];

    public WidgetInspectorServiceExtensionsDartParityTests()
    {
        TestWidgetInspectorService.Restore(Service);
    }

    private static TestWidgetInspectorService Service => TestWidgetInspectorService.Shared;

    public void Dispose()
    {
        foreach (FrameworkDartTester tester in _testers)
        {
            tester.Dispose();
        }

        WidgetsBinding.Instance.DebugOverrideRootForTests(null, null);
        Service.ResetAllState();
        Service.SelectionChangedCallback = null;
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = false;
        WidgetsBinding.Instance.DebugExcludeRootWidgetInspector = false;
        TestWidgetInspectorService.Restore(_previousService);
    }

    private FrameworkDartTester CreateTester()
    {
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        _testers.Add(tester);
        WidgetsBinding.Instance.DebugOverrideRootForTests(tester.Root, tester.Owner);
        return tester;
    }

    private static Widget Ltr(Widget child) => new Directionality(TextDirection.Ltr, child);

    private static Text LtrText(string data) => new(data, textDirection: TextDirection.Ltr);

    private static void PumpWidgetTreeWithABC(FrameworkDartTester tester)
    {
        tester.PumpWidget(Ltr(new Stack(
            children:
            [
                LtrText("a"),
                LtrText("b"),
                LtrText("c"),
                new DisableWidgetInspectorScope(
                    new Column(
                        children:
                        [
                            new InspectorTestHiddenWidget(),
                            new EnableWidgetInspectorScope(new InspectorTestVisibleWidget()),
                        ])),
            ])));
    }

    private static Dictionary<string, object?> Map(object? value) => Assert.IsType<Dictionary<string, object?>>(value);

    private static List<object?> List(object? value) => Assert.IsType<List<object?>>(value);

    [Fact]
    public void RegistersEveryInspectorExtensionWithoutCreationTracking()
    {
        // Dart registers the three creation-tracking extensions only when widget creation is tracked.
        string[] expected =
        {
            "structuredErrors", "show", "disposeAllGroups", "disposeGroup", "isWidgetTreeReady",
            "disposeId", "setPubRootDirectories", "addPubRootDirectories", "removePubRootDirectories",
            "getPubRootDirectories", "setSelectionById", "getParentChain", "getProperties", "getChildren",
            "getChildrenSummaryTree", "getChildrenDetailsSubtree", "getRootWidget", "getRootWidgetSummaryTree",
            "getRootWidgetSummaryTreeWithPreviews", "getRootWidgetTree", "getDetailsSubtree", "getSelectedWidget",
            "getSelectedSummaryWidget", "isWidgetCreationTracked", "screenshot", "getLayoutExplorerNode",
            "setFlexFit", "setFlexFactor", "setFlexProperties",
        };
        Assert.Equal(expected, Service.Extensions.Keys);
        Assert.Equal(false, Service.TestExtension("isWidgetCreationTracked", []));

        // The binding registered the default service's extensions with the VM-service registry.
        foreach (string name in expected)
        {
            Assert.Contains($"ext.flutter.inspector.{name}", BindingBase.ServiceExtensionMethods);
        }
    }

    [Fact]
    public void RegistersTheWidgetsBindingExtensions()
    {
        foreach (string name in new[]
                 {
                     "debugDumpApp", "showPerformanceOverlay", "didSendFirstFrameEvent", "profileWidgetBuilds",
                     "profileUserWidgetBuilds", "debugAllowBanner",
                 })
        {
            Assert.Contains($"ext.flutter.{name}", BindingBase.ServiceExtensionMethods);
        }
    }

    [DebugOnlyFact]
    public async Task DebugAllowBannerExtension_TogglesTheBannerOverride()
    {
        FrameworkDartTester tester = CreateTester();
        Assert.True(WidgetsApp.DebugAllowBannerOverride);
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = static callback => callback();
        try
        {
            Task<ServiceExtensionResponse> pendingResponse = BindingBase.InvokeServiceExtensionAsync(
                "ext.flutter.debugAllowBanner",
                new Dictionary<string, string> { ["enabled"] = "false" });
            // Changing the banner reassembles the root and waits for Scheduler.EndOfFrame.
            tester.Pump();
            ServiceExtensionResponse response = await pendingResponse;
            Assert.Contains("\"enabled\":\"false\"", response.Result);
            Assert.False(WidgetsApp.DebugAllowBannerOverride);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            WidgetsApp.DebugAllowBannerOverride = true;
        }
    }

    [Fact]
    public void DisposeGroupExtension()
    {
        object a = new();
        const string group1 = "group-1";
        const string group2 = "group-2";
        const string group3 = "group-3";
        string? aId = Service.ToId(a, group1);
        Assert.Equal(aId, Service.ToId(a, group2));
        Assert.Equal(aId, Service.ToId(a, group3));
        Service.TestExtension("disposeGroup", new Dictionary<string, string> { ["objectGroup"] = group1 });
        Service.TestExtension("disposeGroup", new Dictionary<string, string> { ["objectGroup"] = group2 });
        Assert.Same(a, Service.ToObject(aId));
        Service.TestExtension("disposeGroup", new Dictionary<string, string> { ["objectGroup"] = group3 });
        Assert.Throws<FlutterError>(() => Service.ToObject(aId));
    }

    [Fact]
    public void DisposeIdExtension()
    {
        object a = new();
        object b = new();
        const string group1 = "group-1";
        const string group2 = "group-2";
        string aId = Service.ToId(a, group1)!;
        string bId = Service.ToId(b, group1)!;
        Assert.Equal(aId, Service.ToId(a, group2));
        Service.TestExtension("disposeId", new Dictionary<string, string> { ["arg"] = bId, ["objectGroup"] = group1 });
        Assert.Throws<FlutterError>(() => Service.ToObject(bId));
        Service.TestExtension("disposeId", new Dictionary<string, string> { ["arg"] = aId, ["objectGroup"] = group1 });
        Assert.Same(a, Service.ToObject(aId));
        Service.TestExtension("disposeId", new Dictionary<string, string> { ["arg"] = aId, ["objectGroup"] = group2 });
        Assert.Throws<FlutterError>(() => Service.ToObject(aId));
    }

    [Fact]
    public void SetSelectionExtension()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Element elementA = tester.FirstElement(Find.Text("a"));
        Element elementB = tester.FirstElement(Find.Text("b"));

        Service.DisposeAllGroups();
        Service.Selection.Clear();
        int selectionChangedCount = 0;
        Service.SelectionChangedCallback = () => selectionChangedCount++;
        Service.SetSelection("invalid selection");
        Assert.Equal(0, selectionChangedCount);
        Assert.Null(Service.Selection.CurrentElement);
        Service.SetSelection(elementA);
        Assert.Equal(1, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
        Assert.Same(elementA.RenderObject, Service.Selection.Current);

        Service.SetSelection(elementB.RenderObject);
        Assert.Equal(2, selectionChangedCount);
        Assert.Same(elementB.RenderObject, Service.Selection.Current);
        Assert.Same(
            ((DebugCreator)elementB.RenderObject!.DebugCreator!).Element,
            Service.Selection.CurrentElement);

        Service.SetSelection("invalid selection");
        Assert.Equal(2, selectionChangedCount);
        Assert.Same(elementB.RenderObject, Service.Selection.Current);

        Assert.Equal(
            true,
            Service.TestExtension(
                "setSelectionById",
                new Dictionary<string, string>
                {
                    ["arg"] = Service.ToId(elementA, "my-group")!,
                    ["objectGroup"] = "my-group",
                }));
        Assert.Equal(3, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
        Assert.Same(elementA.RenderObject, Service.Selection.Current);

        Service.SetSelectionById(Service.ToId(elementA, "my-group"));
        Assert.Equal(3, selectionChangedCount);
        Assert.Same(elementA, Service.Selection.CurrentElement);
    }

    [Fact]
    public void SetSelection_NotifiesToolsWithoutANavigateEventWhenCreationIsUntracked()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Element elementA = tester.FirstElement(Find.Text("a"));
        Service.SetSelection(elementA, "my-group");
        Assert.Equal([elementA], Service.InspectedObjects());
        Assert.Empty(Service.DispatchedEvents("navigate", stream: "ToolEvent"));
    }

    [Fact]
    public void GetParentChainExtension()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        Element elementB = tester.FirstElement(Find.Text("b"));
        string bId = Service.ToId(elementB, group)!;
        object? jsonList = Service.TestExtension(
            "getParentChain",
            new Dictionary<string, string> { ["arg"] = bId, ["objectGroup"] = group });
        WidgetInspectorDartParityTests.VerifyParentChain(tester, elementB, jsonList);
    }

    [Fact]
    public void GetPropertiesExtension()
    {
        IDiagnosticable diagnosticable = LtrText("a");
        const string group = "group";
        string id = Service.ToId(diagnosticable, group)!;
        List<object?> propertiesJson = List(Service.TestExtension(
            "getProperties",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group }));
        WidgetInspectorDartParityTests.VerifyProperties(diagnosticable, propertiesJson);
    }

    [Fact]
    public void GetChildrenExtension()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        DiagnosticsNode diagnostic = tester.FirstElement(Find.ByType(typeof(Stack))).ToDiagnosticsNode();
        string id = Service.ToId(diagnostic, group)!;
        List<object?> childrenJson = List(Service.TestExtension(
            "getChildren",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group }));
        WidgetInspectorDartParityTests.VerifyChildren(diagnostic, childrenJson);
    }

    [Fact]
    public void GetChildrenDetailsSubtreeExtension()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        Element diagnosticable = tester.FirstElement(Find.ByType(typeof(Stack)));
        string id = Service.ToId(diagnosticable, group)!;
        List<object?> childrenJson = List(Service.TestExtension(
            "getChildrenDetailsSubtree",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group }));
        List<DiagnosticsNode> children = diagnosticable.ToDiagnosticsNode().GetChildren();
        Assert.Equal(4, children.Count);
        Assert.Equal(children.Count, childrenJson.Count);
        VerifyDetailsChildren(children, childrenJson, expectNestedChildren: null);
    }

    private static void VerifyDetailsChildren(
        List<DiagnosticsNode> children,
        List<object?> childrenJson,
        bool? expectNestedChildren)
    {
        for (int i = 0; i < childrenJson.Count; ++i)
        {
            Dictionary<string, object?> childJson = Map(childrenJson[i]);
            Assert.Same(
                GetFirstVisibleNode(children[i])?.Value,
                Service.ToObject((string?)childJson["valueId"]));
            List<object?> propertiesJson = List(childJson["properties"]);
            var element = (Element)Service.ToObject((string?)childJson["valueId"])!;
            List<string> expectedProperties = element.ToDiagnosticsNode()
                .GetProperties()
                .Select(property => property.Value?.ToString() ?? "null")
                .ToList();
            foreach (object? propertyJson in propertiesJson)
            {
                Dictionary<string, object?> property = Map(propertyJson);
                object? propertyValue = Service.ToObject((string?)property["valueId"]);
                Assert.Contains(propertyValue?.ToString() ?? "null", expectedProperties);
                if (expectNestedChildren is { } expected)
                {
                    Assert.Equal(expected, property.ContainsKey("children"));
                }
            }
        }
    }

    private static DiagnosticsNode? GetFirstVisibleNode(DiagnosticsNode node, bool inDisable = false)
    {
        object? value = node.Value;
        if ((value is Element { Widget: DisableWidgetInspectorScope }) || inDisable)
        {
            foreach (DiagnosticsNode child in node.GetChildren())
            {
                DiagnosticsNode? visible = GetFirstVisibleNode(
                    child,
                    ((Element)value!).Widget is not EnableWidgetInspectorScope);
                if (visible is not null)
                {
                    return visible;
                }
            }

            return null;
        }

        return node;
    }

    [Fact]
    public void GetDetailsSubtree()
    {
        FrameworkDartTester tester = CreateTester();
        const string group = "test-group";
        PumpWidgetTreeWithABC(tester);
        Element diagnosticable = tester.FirstElement(Find.ByType(typeof(Stack)));
        string id = Service.ToId(diagnosticable, group)!;
        Dictionary<string, object?> subtreeJson = Map(Service.TestExtension(
            "getDetailsSubtree",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group }));
        Assert.Equal(id, subtreeJson["valueId"]);
        List<object?> childrenJson = List(subtreeJson["children"]);
        List<DiagnosticsNode> children = diagnosticable.ToDiagnosticsNode().GetChildren();
        Assert.Equal(4, children.Count);
        Assert.Equal(children.Count, childrenJson.Count);
        VerifyDetailsChildren(children, childrenJson, expectNestedChildren: false);

        Dictionary<string, object?> deepSubtreeJson = Map(Service.TestExtension(
            "getDetailsSubtree",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group, ["subtreeDepth"] = "3" }));
        List<object?> deepChildrenJson = List(deepSubtreeJson["children"]);
        VerifyDetailsChildren(children, deepChildrenJson, expectNestedChildren: true);
    }

    [Fact]
    public void CyclicDiagnosticsRegressionTest()
    {
        const string group = "test-group";
        var a = new CyclicDiagnostic("a");
        var b = new CyclicDiagnostic("b");
        a.Related = b;
        a.Children.Add(b.ToDiagnosticsNode());
        b.Related = a;

        string id = Service.ToId(a, group)!;
        Dictionary<string, object?> subtreeJson = Map(Service.TestExtension(
            "getDetailsSubtree",
            new Dictionary<string, string> { ["arg"] = id, ["objectGroup"] = group }));
        Assert.Equal(id, subtreeJson["valueId"]);
        Assert.True(subtreeJson.ContainsKey("children"));
        List<object?> propertiesJson = List(subtreeJson["properties"]);
        Assert.Single(propertiesJson);
        Dictionary<string, object?> relatedProperty = Map(propertiesJson[0]);
        Assert.Equal("related", relatedProperty["name"]);
        Assert.Equal("CyclicDiagnostic-b", relatedProperty["description"]);
        Assert.True(relatedProperty.ContainsKey("isDiagnosticableValue"));
        Assert.False(relatedProperty.ContainsKey("children"));
        Assert.True(relatedProperty.ContainsKey("properties"));
        List<object?> relatedWidgetProperties = List(relatedProperty["properties"]);
        Assert.Single(relatedWidgetProperties);
        Dictionary<string, object?> nestedRelatedProperty = Map(relatedWidgetProperties[0]);
        Assert.Equal("related", nestedRelatedProperty["name"]);
        // Make sure we do not include properties or children for diagnostic a
        // as we already included properties and children for it.
        Assert.Equal("CyclicDiagnostic-a", nestedRelatedProperty["description"]);
        Assert.True(nestedRelatedProperty.ContainsKey("isDiagnosticableValue"));
        Assert.False(nestedRelatedProperty.ContainsKey("properties"));
        Assert.False(nestedRelatedProperty.ContainsKey("children"));
    }

    [Fact]
    public void RootWidgetSummaryTree_IncludesEveryNodeWithoutCreationTracking()
    {
        // With creation untracked, `_shouldShowInSummaryTree` keeps every element, and the scopes
        // still hide the subtree below DisableWidgetInspectorScope up to EnableWidgetInspectorScope.
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Dictionary<string, object?> rootJson = Map(Service.TestExtension(
            "getRootWidgetSummaryTree",
            new Dictionary<string, string> { ["objectGroup"] = "test-group" }));
        Assert.Same(WidgetsBinding.Instance.RootElement, Service.ToObject((string?)rootJson["valueId"]));
        var descriptions = new List<string>();
        void Collect(Dictionary<string, object?> node)
        {
            descriptions.Add((string)node["description"]!);
            if (node.TryGetValue("children", out object? children) && children is List<object?> list)
            {
                foreach (object? child in list)
                {
                    Collect(Map(child));
                }
            }
        }

        Collect(rootJson);
        Assert.Contains("InspectorTestVisibleWidget", descriptions);
        Assert.DoesNotContain("InspectorTestHiddenWidget", descriptions);
        Assert.DoesNotContain("DisableWidgetInspectorScope", descriptions);
        Assert.DoesNotContain("EnableWidgetInspectorScope", descriptions);
        Assert.Contains("Stack", descriptions.Select(description => description.Split('-')[0]));

        Dictionary<string, object?> previews = Map(Service.TestExtension(
            "getRootWidgetTree",
            new Dictionary<string, string>
            {
                ["groupName"] = "test-group",
                ["isSummaryTree"] = "true",
                ["withPreviews"] = "true",
            }));
        var textPreviews = new List<string>();
        void CollectPreviews(Dictionary<string, object?> node)
        {
            if (node.TryGetValue("textPreview", out object? preview))
            {
                textPreviews.Add((string)preview!);
            }

            if (node.TryGetValue("children", out object? children) && children is List<object?> list)
            {
                foreach (object? child in list)
                {
                    CollectPreviews(Map(child));
                }
            }
        }

        CollectPreviews(previews);
        // Without creation tracking, the summary tree keeps both the Text element and its RichText,
        // and both render the same paragraph.
        Assert.Equal(new[] { "a", "b", "c" }, textPreviews.Distinct());

        Dictionary<string, object?> withoutDetails = Map(Service.TestExtension(
            "getRootWidgetTree",
            new Dictionary<string, string>
            {
                ["groupName"] = "test-group",
                ["isSummaryTree"] = "false",
                ["withPreviews"] = "false",
                ["fullDetails"] = "false",
            }));
        Assert.False(withoutDetails.ContainsKey("type"));
    }

    [Fact]
    public void GetSelectedSummaryWidget_IsTheSelectedWidgetWithoutCreationTracking()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetTreeWithABC(tester);
        Element richText = tester.FirstElement(Find.Descendant(Find.Text("a"), Find.ByType(typeof(RichText))));
        Service.SetSelection(richText, "my-group");
        Dictionary<string, object?> summary = Map(Service.TestExtension(
            "getSelectedSummaryWidget",
            new Dictionary<string, string> { ["objectGroup"] = "my-group" }));
        Assert.Same(richText, Service.ToObject((string?)summary["valueId"]));
        Dictionary<string, object?> regular = Map(Service.TestExtension(
            "getSelectedWidget",
            new Dictionary<string, string> { ["objectGroup"] = "my-group" }));
        Assert.Same(richText, Service.ToObject((string?)regular["valueId"]));
        Assert.False(regular.ContainsKey("creationLocation"));
        Assert.False(regular.ContainsKey("createdByLocalProject"));
    }

    [Fact]
    public void ShowExtension()
    {
        FrameworkDartTester tester = CreateTester();
        RunShowExtensionTest(
            tester,
            () => WidgetsBinding.Instance.DebugShowWidgetInspectorOverride,
            value => WidgetsBinding.Instance.DebugShowWidgetInspectorOverride = value);
    }

    [Fact]
    public void ShowExtension_ViaWidgetsAppDebugShowWidgetInspectorOverride()
    {
        FrameworkDartTester tester = CreateTester();
#pragma warning disable CS0618 // Dart tests the deprecated accessor too.
        RunShowExtensionTest(
            tester,
            () => WidgetsApp.DebugShowWidgetInspectorOverride,
            value => WidgetsApp.DebugShowWidgetInspectorOverride = value);
#pragma warning restore CS0618
    }

    private static void RunShowExtensionTest(
        FrameworkDartTester tester,
        Func<bool> getOverride,
        Action<bool> setOverride)
    {
        List<IReadOnlyDictionary<string, object?>> ExtensionChangedEvents() =>
            Service.GetServiceExtensionStateChangedEvents("ext.flutter.inspector.show");

        tester.PumpWidget(new WidgetsApp(
            key: new LabeledGlobalKey<State>(null),
            builder: (_, _) => new Placeholder(),
            color: new Color(0xFF123456)));

        var valueListenableBuilder = (ValueListenableBuilder<bool>)tester
            .FirstElement(Find.ByType(typeof(ValueListenableBuilder<bool>)))
            .Widget;
        setOverride(false);
        int debugShowWidgetInspectorOverrideCallCount = 0;
        valueListenableBuilder.ValueListenable.AddListener(() => debugShowWidgetInspectorOverrideCallCount++);

        Service.RebuildCount = 0;
        Assert.Empty(ExtensionChangedEvents());
        Assert.Equal(
            "true",
            Service.TestBoolExtension("show", new Dictionary<string, string> { ["enabled"] = "true" }));
        Assert.Single(ExtensionChangedEvents());
        IReadOnlyDictionary<string, object?> extensionChangedEvent = ExtensionChangedEvents()[^1];
        Assert.Equal("ext.flutter.inspector.show", extensionChangedEvent["extension"]);
        Assert.Equal(true, extensionChangedEvent["value"]);
        Assert.Equal(0, Service.RebuildCount);
        Assert.Equal(1, debugShowWidgetInspectorOverrideCallCount);
        Assert.Equal("true", Service.TestBoolExtension("show", []));
        Assert.True(getOverride());
        Assert.Single(ExtensionChangedEvents());
        Assert.Equal(
            "true",
            Service.TestBoolExtension("show", new Dictionary<string, string> { ["enabled"] = "true" }));
        Assert.Equal(2, ExtensionChangedEvents().Count);
        extensionChangedEvent = ExtensionChangedEvents()[^1];
        Assert.Equal("ext.flutter.inspector.show", extensionChangedEvent["extension"]);
        Assert.Equal(true, extensionChangedEvent["value"]);
        Assert.Equal(0, Service.RebuildCount);
        Assert.Equal(1, debugShowWidgetInspectorOverrideCallCount);
        Assert.Equal(
            "false",
            Service.TestBoolExtension("show", new Dictionary<string, string> { ["enabled"] = "false" }));
        Assert.Equal(3, ExtensionChangedEvents().Count);
        extensionChangedEvent = ExtensionChangedEvents()[^1];
        Assert.Equal("ext.flutter.inspector.show", extensionChangedEvent["extension"]);
        Assert.Equal(false, extensionChangedEvent["value"]);
        Assert.Equal(0, Service.RebuildCount);
        Assert.Equal(2, debugShowWidgetInspectorOverrideCallCount);
        Assert.Equal("false", Service.TestBoolExtension("show", []));
        Assert.Equal(3, ExtensionChangedEvents().Count);
        Assert.False(getOverride());
    }

    private static void PumpWidgetForLayoutExplorer(FrameworkDartTester tester)
    {
        tester.PumpWidget(Ltr(new Center(
            child: new Row(
                children:
                [
                    new Flexible(new ColoredBox(color: new Color(0xFF00FF00), child: new Text("a"))),
                    new Text("b"),
                ]))));
    }

    private static Dictionary<string, object?> LayoutExplorerNode(string id, string group = "test-group")
    {
        return Map(Service.TestExtension(
            "getLayoutExplorerNode",
            new Dictionary<string, string> { ["id"] = id, ["groupName"] = group, ["subtreeDepth"] = "1" }));
    }

    private static string SelectAndId(Element element, string group = "test-group")
    {
        Service.SetSelection(element, group);
        return Service.ToId(element, group)!;
    }

    [Fact]
    public void GetLayoutExplorerNode_ForRenderBoxWithBoxParentData()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Row)))));
        Assert.Equal("Row", result["description"]);

        Dictionary<string, object?> renderObject = Map(result["renderObject"]);
        Assert.StartsWith("RenderFlex", (string)renderObject["description"]!);

        Dictionary<string, object?> parentRenderElement = Map(result["parentRenderElement"]);
        Assert.Equal("Center", parentRenderElement["description"]);

        Dictionary<string, object?> constraints = Map(result["constraints"]);
        Assert.Equal("BoxConstraints", constraints["type"]);
        Assert.Equal("0.0", constraints["minWidth"]);
        Assert.Equal("0.0", constraints["minHeight"]);
        Assert.Equal("800.0", constraints["maxWidth"]);
        Assert.Equal("600.0", constraints["maxHeight"]);

        Assert.Equal(true, result["isBox"]);

        Dictionary<string, object?> size = Map(result["size"]);
        Assert.Equal("800.0", size["width"]);
        Assert.Equal("14.0", size["height"]);

        Assert.False(result.ContainsKey("flexFactor"));
        Assert.False(result.ContainsKey("flexFit"));

        Dictionary<string, object?> parentData = Map(result["parentData"]);
        Assert.Equal("0.0", parentData["offsetX"]);
        Assert.Equal("293.0", parentData["offsetY"]);
    }

    [Fact]
    public void GetLayoutExplorerNode_DoesNotThrowForUnmountedWidget()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        string id = SelectAndId(tester.FirstElement(Find.ByType(typeof(Row))));
        tester.PumpWidget(new Placeholder());
        Dictionary<string, object?> result = LayoutExplorerNode(id);
        Assert.False(result.ContainsKey("renderObject"));
    }

    [Fact]
    public void GetLayoutExplorerNode_OmitsFlexFactorWhenFlexIsNull()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Row(
            children:
            [
                new Align(
                    alignment: Alignment.TopLeft,
                    child: new ColoredBox(
                        color: new Color(0xFF000000),
                        child: new SizedBox(width: 14, height: 14))),
            ])));
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(ColoredBox)))));
        Dictionary<string, object?> parentData = Map(result["parentData"]);
        Assert.False(parentData.ContainsKey("flexFactor"));
        Assert.False(parentData.ContainsKey("flexFit"));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void GetLayoutExplorerNode_ForRenderBoxWithFlexParentData()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Flexible)))));
        Assert.Equal("Flexible", result["description"]);

        Dictionary<string, object?> renderObject = Map(result["renderObject"]);
        Assert.StartsWith("RenderColoredBox", (string)renderObject["description"]!);

        Dictionary<string, object?> parentRenderElement = Map(result["parentRenderElement"]);
        Assert.Equal("Row", parentRenderElement["description"]);

        Dictionary<string, object?> constraints = Map(result["constraints"]);
        Assert.Equal("BoxConstraints", constraints["type"]);
        Assert.Equal("0.0", constraints["minWidth"]);
        Assert.Equal("0.0", constraints["minHeight"]);
        Assert.Equal("786.0", constraints["maxWidth"]);
        Assert.Equal("600.0", constraints["maxHeight"]);

        Assert.Equal(true, result["isBox"]);

        Dictionary<string, object?> size = Map(result["size"]);
        Assert.Equal("14.0", size["width"]);
        Assert.Equal("14.0", size["height"]);

        Assert.Equal(1, result["flexFactor"]);
        Assert.Equal("loose", result["flexFit"]);
    }

    [Fact]
    public void GetLayoutExplorerNode_ForRenderView()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Element? root = null;
        tester.FirstElement(Find.ByType(typeof(Directionality))).VisitAncestorElements(element =>
        {
            root = element;
            return true;
        });
        Dictionary<string, object?> result = LayoutExplorerNode(SelectAndId(root!));
        Assert.Equal("[root]", result["description"]);

        Dictionary<string, object?> renderObject = Map(result["renderObject"]);
        Assert.Contains("RenderView", (string)renderObject["description"]!);

        Assert.False(result.ContainsKey("parentRenderElement"));

        Dictionary<string, object?> constraints = Map(result["constraints"]);
        Assert.Equal("BoxConstraints", constraints["type"]);
        Assert.Equal("800.0", constraints["minWidth"]);
        Assert.Equal("600.0", constraints["minHeight"]);
        Assert.Equal("800.0", constraints["maxWidth"]);
        Assert.Equal("600.0", constraints["maxHeight"]);

        Assert.False(result.ContainsKey("isBox"));

        Dictionary<string, object?> size = Map(result["size"]);
        Assert.Equal("800.0", size["width"]);
        Assert.Equal("600.0", size["height"]);

        Assert.False(result.ContainsKey("flexFactor"));
        Assert.False(result.ContainsKey("flexFit"));
        Assert.False(result.ContainsKey("parentData"));
    }

    [Fact]
    public void SetFlexFitExtension()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Flexible)))));
        Assert.Equal("Flexible", result["description"]);
        Assert.Equal("loose", result["flexFit"]);
        string valueId = (string)result["valueId"]!;

        Assert.Equal(
            true,
            Service.TestExtension(
                "setFlexFit",
                new Dictionary<string, string> { ["id"] = valueId, ["flexFit"] = "FlexFit.tight" }));

        result = LayoutExplorerNode(valueId);
        Assert.Equal("Flexible", result["description"]);
        Assert.Equal("tight", result["flexFit"]);
    }

    [Fact]
    public void SetFlexFactorExtension()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Flexible)))));
        Assert.Equal(1, result["flexFactor"]);
        string valueId = (string)result["valueId"]!;

        Assert.Equal(
            true,
            Service.TestExtension(
                "setFlexFactor",
                new Dictionary<string, string> { ["id"] = valueId, ["flexFactor"] = "3" }));

        result = LayoutExplorerNode(valueId);
        Assert.Equal(3, result["flexFactor"]);
    }

    [Fact]
    public void SetFlexPropertiesExtension()
    {
        FrameworkDartTester tester = CreateTester();
        PumpWidgetForLayoutExplorer(tester);
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Row)))));
        Assert.Equal("Row", result["description"]);

        string AlignmentDescription(Dictionary<string, object?> json, string type) => (string)List(
                Map(json["renderObject"])["properties"])
            .Select(Map)
            .First(property => Equals(property["type"], type))["description"]!;

        Assert.Equal("start", AlignmentDescription(result, "EnumProperty<MainAxisAlignment>"));
        Assert.Equal("center", AlignmentDescription(result, "EnumProperty<CrossAxisAlignment>"));
        string valueId = (string)result["valueId"]!;

        Assert.Equal(
            true,
            Service.TestExtension(
                "setFlexProperties",
                new Dictionary<string, string>
                {
                    ["id"] = valueId,
                    ["mainAxisAlignment"] = "MainAxisAlignment.center",
                    ["crossAxisAlignment"] = "CrossAxisAlignment.start",
                }));

        result = LayoutExplorerNode(valueId);
        Assert.Equal("Row", result["description"]);
        Assert.Equal("center", AlignmentDescription(result, "EnumProperty<MainAxisAlignment>"));
        Assert.Equal("start", AlignmentDescription(result, "EnumProperty<CrossAxisAlignment>"));
    }

    [Fact]
    public void GetLayoutExplorerNode_DoesNotThrowStackOverflowError()
    {
        FrameworkDartTester tester = CreateTester();
        var key = new ValueKey<string>("ColoredBox");
        tester.PumpWidget(Ltr(new ColoredBox(key: key, color: new Color(0xFF0000FF))));
        Element leafElement = tester.FirstElement(Find.ByKey(key));
        Service.SetSelection(leafElement, "test-group");
        string id = Service.ToId(leafElement.ToDiagnosticsNode(), "test-group")!;
        // A DiagnosticsNode is not a Diagnosticable, so the node is not found.
        Assert.Empty(LayoutExplorerNode(id));
    }

    [Fact]
    public void GetLayoutExplorerNode_OnADeeplyNestedWidget_DoesNotThrowStackOverflowError()
    {
        FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(Ltr(new Center(
            child: new Row(
                children:
                [
                    new Flexible(new ColoredBox(
                        color: new Color(0xFF00FF00),
                        child: new SizedBox(
                            child: new Padding(new Thickness(0), child: new Text("a"))))),
                ]))));
        Dictionary<string, object?> result =
            LayoutExplorerNode(SelectAndId(tester.FirstElement(Find.ByType(typeof(Padding)))));
        Assert.Equal("Padding", result["description"]);
    }

    [Fact]
    public void StructuredErrorsExtension()
    {
        CreateTester();
        List<IReadOnlyDictionary<string, object?>> flutterErrorEvents = Service.DispatchedEvents("Flutter.Error");
        Assert.Empty(flutterErrorEvents);

        FlutterExceptionHandler oldHandler = FlutterError.PresentError;
        FlutterExceptionHandler? oldOnError = FlutterError.OnError;

        try
        {
            // flutter_test's handler presents errors through FlutterError.presentError.
            FlutterError.OnError = details => FlutterError.PresentError(details);
            // Enable structured errors.
            Assert.Equal(
                "true",
                Service.TestBoolExtension("structuredErrors", new Dictionary<string, string> { ["enabled"] = "true" }));

            // Create an error.
            FlutterError.ReportError(new FlutterErrorDetails(
                library: "rendering library",
                context: new ErrorDescription("during layout"),
                exception: new InvalidOperationException("stack")));

            // Validate that we received an error.
            Assert.Single(flutterErrorEvents);

            // Validate the error contents.
            IReadOnlyDictionary<string, object?> error = flutterErrorEvents[0];
            Assert.Equal("Exception caught by rendering library", error["description"]);
            Assert.Empty((System.Collections.IList)error["children"]!);

            // Validate that we received an error count.
            Assert.Equal(0, error["errorsSinceReload"]);
            Assert.StartsWith(
                "══╡ EXCEPTION CAUGHT BY RENDERING LIBRARY ╞════════════",
                (string)error["renderedErrorText"]!);

            // Send a second error.
            FlutterError.ReportError(new FlutterErrorDetails(
                library: "rendering library",
                context: new ErrorDescription("also during layout"),
                exception: new InvalidOperationException("stack")));

            // Validate that the error count increased.
            Assert.Equal(2, flutterErrorEvents.Count);
            error = flutterErrorEvents[1];
            Assert.Equal(1, error["errorsSinceReload"]);
            Assert.StartsWith("Another exception was thrown:", (string)error["renderedErrorText"]!);

            // Reloads the app.
            WidgetsBinding.Instance.ReassembleApplication();

            // Send another error.
            FlutterError.ReportError(new FlutterErrorDetails(
                library: "rendering library",
                context: new ErrorDescription("during layout"),
                exception: new InvalidOperationException("stack")));

            // And, validate that the error count has been reset.
            Assert.Equal(3, flutterErrorEvents.Count);
            error = flutterErrorEvents[2];
            Assert.Equal(0, error["errorsSinceReload"]);
        }
        finally
        {
            Service.TestBoolExtension("structuredErrors", new Dictionary<string, string> { ["enabled"] = "false" });
            FlutterError.PresentError = oldHandler;
            FlutterError.OnError = oldOnError;
        }
    }

    [Fact]
    public void StructuredErrors_CustomFlutterErrorOnError()
    {
        // widget_inspector_structure_error_test.dart: structured errors are presented only through
        // FlutterError.presentError, and the extension restores the original handler. Plumix turns
        // structured errors on by default only when the `flutter.inspector.structuredErrors` switch
        // is set, so the test sets it, as Dart's default (`!kIsWeb`) would.
        AppContext.SetSwitch("flutter.inspector.structuredErrors", true);
        FlutterExceptionHandler oldHandler = FlutterError.PresentError;
        FlutterExceptionHandler? oldOnError = FlutterError.OnError;
        var service = new TestWidgetInspectorService();
        WidgetInspectorService previous = WidgetInspectorService.Instance;
        try
        {
            TestWidgetInspectorService.Install(service);
            Assert.Empty(service.DispatchedEvents("Flutter.Error"));

            // Set callback that doesn't call presentError.
            bool onErrorCalled = false;
            FlutterError.OnError = _ => onErrorCalled = true;

            var expectedError = new FlutterErrorDetails(
                library: "rendering library",
                context: new ErrorDescription("during layout"),
                exception: new InvalidOperationException("stack"));
            FlutterError.ReportError(expectedError);

            // Verify structured errors are not shown.
            Assert.True(onErrorCalled);
            Assert.Empty(service.DispatchedEvents("Flutter.Error"));

            // Set callback that calls presentError.
            onErrorCalled = false;
            FlutterError.OnError = details =>
            {
                FlutterError.PresentError(details);
                onErrorCalled = true;
            };

            FlutterError.ReportError(expectedError);

            // Verify structured errors are shown.
            Assert.True(onErrorCalled);
            Assert.Single(service.DispatchedEvents("Flutter.Error"));

            // Verify that the extension can enable/disable structured errors.
            Assert.Equal(
                "true",
                service.TestBoolExtension("structuredErrors", new Dictionary<string, string> { ["enabled"] = "true" }));
            Assert.NotEqual(oldHandler, FlutterError.PresentError);

            Assert.Equal(
                "false",
                service.TestBoolExtension(
                    "structuredErrors",
                    new Dictionary<string, string> { ["enabled"] = "false" }));
            Assert.Equal(oldHandler, FlutterError.PresentError);
        }
        finally
        {
            FlutterError.OnError = oldOnError;
            FlutterError.PresentError = oldHandler;
            AppContext.SetSwitch("flutter.inspector.structuredErrors", false);
            TestWidgetInspectorService.Restore(previous);
        }
    }

    [Fact]
    public void StructuredErrors_AreOffByDefaultWithoutTheSwitch()
    {
        Assert.False(new TestWidgetInspectorService().IsStructuredErrorsEnabled());
    }

    private static int GetChildLayerCount(OffsetLayer layer)
    {
        Layer? child = layer.FirstChild;
        int count = 0;
        while (child is not null)
        {
            count++;
            child = child.NextSibling;
        }

        return count;
    }

    [Fact]
    public void ScreenshotExtension()
    {
        FrameworkDartTester tester = CreateTester();
        var outerContainerKey = new LabeledGlobalKey<State>(null);
        var paddingKey = new LabeledGlobalKey<State>(null);
        var redContainerKey = new LabeledGlobalKey<State>(null);
        var whiteContainerKey = new LabeledGlobalKey<State>(null);
        var sizedBoxKey = new LabeledGlobalKey<State>(null);

        // Complex widget tree intended to exercise features such as children
        // with rotational transforms and clipping without introducing platform
        // specific behavior as text rendering would.
        tester.PumpWidget(new Center(
            child: new RepaintBoundaryWithDebugPaint(
                child: new ColoredBox(
                    key: outerContainerKey,
                    color: new Color(0xFFFFFFFF),
                    child: new Padding(
                        new Thickness(100.0),
                        key: paddingKey,
                        child: new SizedBox(
                            key: sizedBoxKey,
                            height: 100.0,
                            width: 100.0,
                            child: Transform.Rotate(
                                angle: 1.0, // radians
                                child: new ClipRRect(
                                    borderRadius: new BorderRadius(
                                        topLeft: Radius.Elliptical(10.0, 20.0),
                                        topRight: Radius.Elliptical(5.0, 30.0),
                                        bottomLeft: Radius.Elliptical(2.5, 12.0),
                                        bottomRight: Radius.Elliptical(15.0, 6.0)),
                                    child: new ColoredBox(
                                        key: redContainerKey,
                                        color: new Color(0xFFF44336),
                                        child: new ColoredBox(
                                            key: whiteContainerKey,
                                            color: new Color(0xFFFFFFFF),
                                            child: new RepaintBoundary(
                                                child: new Center(
                                                    child: new Container(
                                                        color: new Color(0xFF000000),
                                                        height: 10.0,
                                                        width: 10.0)))))))))))));

        Element repaintBoundary = tester.FirstElement(Find.ByType(typeof(RepaintBoundaryWithDebugPaint)));
        var renderObject = (RenderRepaintBoundary)repaintBoundary.RenderObject!;

        var layer = (OffsetLayer)renderObject.DebugLayer!;
        Assert.Equal(2, GetChildLayerCount(layer));
        ContainerLayer? layerParent = layer.Parent;
        Assert.NotNull(layerParent);
        Assert.NotNull(layer.FirstChild);

        Bitmap screenshot1 = Screenshot(repaintBoundary, width: 300.0, height: 300.0);
        Assert.Equal(new PixelSize(300, 300), screenshot1.PixelSize);
        // Verify calling the screenshot method still results in a valid layer tree.
        Assert.Same(layer, renderObject.DebugLayer);
        Assert.Equal(2, GetChildLayerCount(layer));

        Bitmap screenshot2 = Screenshot(repaintBoundary, width: 500.0, height: 500.0, margin: 50.0);
        Assert.Equal(new PixelSize(400, 400), screenshot2.PixelSize);
        Assert.Same(layer, renderObject.DebugLayer);
        Assert.Equal(2, GetChildLayerCount(layer));
        Assert.Same(layerParent, layer.Parent);

        Bitmap screenshot3 = Screenshot(repaintBoundary, width: 300.0, height: 300.0, debugPaint: true);
        Assert.Equal(new PixelSize(300, 300), screenshot3.PixelSize);
        Assert.Equal(2, GetChildLayerCount(layer));
        Assert.Same(layer, renderObject.DebugLayer);
        Assert.True(layer.Attached);
        Assert.False(RenderingDebug.PaintSizeEnabled);

        Element outerContainer = tester.FirstElement(Find.ByKey(outerContainerKey));
        Bitmap screenshot4 = Screenshot(outerContainer, width: 100.0, height: 100.0);
        Assert.Equal(new PixelSize(100, 100), screenshot4.PixelSize);
        Bitmap screenshot5 = Screenshot(outerContainer, width: 100.0, height: 100.0, debugPaint: true);
        Assert.Equal(new PixelSize(100, 100), screenshot5.PixelSize);

        RenderObject container = outerContainer.RenderObject!;
        container.MarkNeedsLayout();
        container.MarkNeedsPaint();
        Assert.True(container.DebugNeedsLayout);
        Bitmap screenshot6 = Screenshot(outerContainer, width: 100.0, height: 100.0, debugPaint: true);
        Assert.Equal(new PixelSize(100, 100), screenshot6.PixelSize);
        Assert.False(container.DebugNeedsLayout);

        Bitmap screenshot7 = Screenshot(outerContainer, width: 50.0, height: 100.0);
        Assert.Equal(new PixelSize(50, 50), screenshot7.PixelSize);
        Bitmap screenshot8 = Screenshot(outerContainer, width: 400.0, height: 400.0, maxPixelRatio: 3.0);
        Assert.Equal(new PixelSize(400, 400), screenshot8.PixelSize);

        Element clipRect = tester.FirstElement(Find.ByType(typeof(ClipRRect)));
        Bitmap screenshot9 = Screenshot(clipRect, width: 100.0, height: 100.0, debugPaint: true);
        Assert.Equal(new PixelSize(100, 100), screenshot9.PixelSize);
        Bitmap clipRectScreenshot = Screenshot(clipRect, width: 100.0, height: 100.0, margin: 20.0, debugPaint: true);
        Assert.Equal(new PixelSize(100, 100), clipRectScreenshot.PixelSize);

        // Verify we get the same image if we go through the service extension
        // instead of invoking the screenshot method directly.
        string base64Screenshot = (string)Service.TestExtension(
            "screenshot",
            new Dictionary<string, string>
            {
                ["id"] = Service.ToId(clipRect, "group")!,
                ["width"] = "100.0",
                ["height"] = "100.0",
                ["margin"] = "20.0",
                ["debugPaint"] = "true",
            })!;
        using var stream = new MemoryStream(Convert.FromBase64String(base64Screenshot));
        using var screenshotFromExtension = new Bitmap(stream);
        Assert.Equal(clipRectScreenshot.PixelSize, screenshotFromExtension.PixelSize);
        Assert.Equal(RasterBackend.ReadPixels(clipRectScreenshot), RasterBackend.ReadPixels(screenshotFromExtension));

        Bitmap screenshot10 = Screenshot(
            tester.FirstElement(Find.ByKey(paddingKey)),
            width: 300.0,
            height: 300.0,
            debugPaint: true);
        Assert.Equal(new PixelSize(300, 300), screenshot10.PixelSize);

        Element sizedBox = tester.FirstElement(Find.ByKey(sizedBoxKey));
        Bitmap screenshot11 = Screenshot(sizedBox, width: 300.0, height: 300.0, debugPaint: true);
        Bitmap screenshot12 = Screenshot(sizedBox, width: 300.0, height: 300.0, margin: 50.0, debugPaint: true);
        // The rotated child grows the box's subtree bounds; the margin grows them further.
        Assert.True(screenshot12.PixelSize.Width >= screenshot11.PixelSize.Width);

        // The white outer container fills the top-left pixel of its own screenshot.
        (byte r, byte g, byte b, byte a) = RasterBackend.PixelAt(screenshot4, 1, 1);
        Assert.Equal((255, 255, 255, 255), (r, g, b, a));

        foreach (Bitmap image in new[]
                 {
                     screenshot1, screenshot2, screenshot3, screenshot4, screenshot5, screenshot6, screenshot7,
                     screenshot8, screenshot9, clipRectScreenshot, screenshot10, screenshot11, screenshot12,
                 })
        {
            image.Dispose();
        }
    }

    private static Bitmap Screenshot(
        object target,
        double width,
        double height,
        double margin = 0.0,
        double maxPixelRatio = 1.0,
        bool debugPaint = false)
    {
        Task<Bitmap?> task = Service.Screenshot(
            target,
            width: width,
            height: height,
            margin: margin,
            maxPixelRatio: maxPixelRatio,
            debugPaint: debugPaint);
        Scheduler.FlushMicrotasks();
        return task.GetAwaiter().GetResult()!;
    }

    [Fact]
    public void ScreenshotOfCompositedTransforms_WithRotations_KeepsTheTree()
    {
        FrameworkDartTester tester = CreateTester();
        var link = new LayerLink();
        var key1 = new LabeledGlobalKey<State>(null);
        var key2 = new LabeledGlobalKey<State>(null);
        var mainStackKey = new LabeledGlobalKey<State>(null);
        var stackWithTransformTarget = new LabeledGlobalKey<State>(null);
        var stackWithTransformFollower = new LabeledGlobalKey<State>(null);

        tester.PumpWidget(Ltr(new Stack(
            key: mainStackKey,
            children:
            [
                new Stack(
                    key: stackWithTransformTarget,
                    children:
                    [
                        new Positioned(
                            top: 123.0,
                            left: 456.0,
                            child: Transform.Rotate(
                                angle: 1.0, // radians
                                child: new CompositedTransformTarget(
                                    link: link,
                                    child: new Container(
                                        key: key1,
                                        height: 20.0,
                                        width: 20.0,
                                        color: Color.FromARGB(128, 255, 0, 0)))))
                    ]),
                new Positioned(
                    top: 487.0,
                    left: 243.0,
                    child: new Stack(
                        key: stackWithTransformFollower,
                        children:
                        [
                            new Container(height: 15.0, width: 15.0, color: Color.FromARGB(128, 0, 0, 255)),
                            Transform.Rotate(
                                angle: -0.3, // radians
                                child: new CompositedTransformFollower(
                                    link: link,
                                    child: new Container(
                                        key: key2,
                                        height: 10.0,
                                        width: 10.0,
                                        color: Color.FromARGB(128, 0, 255, 0)))),
                        ])),
            ])));
        var box1 = (RenderBox)key1.CurrentContext!.FindRenderObject()!;
        var box2 = (RenderBox)key2.CurrentContext!.FindRenderObject()!;
        // Snapshot the positions of the two relevant boxes to ensure that taking
        // screenshots doesn't impact their positions.
        Point position1 = box1.LocalToGlobal(new Point(0, 0));
        Point position2 = box2.LocalToGlobal(new Point(0, 0));
        Assert.Equal(position1.X, position2.X, 1e-10);
        Assert.Equal(position1.Y, position2.Y, 1e-10);

        Screenshot(tester.FirstElement(Find.ByKey(mainStackKey)), width: 500.0, height: 500.0).Dispose();
        Screenshot(tester.FirstElement(Find.ByKey(stackWithTransformTarget)), width: 500.0, height: 500.0).Dispose();
        Screenshot(tester.FirstElement(Find.ByKey(stackWithTransformFollower)), width: 500.0, height: 500.0).Dispose();

        // Make sure taking screenshots hasn't modified the positions of the
        // TransformTarget or TransformFollower layers.
        Assert.Same(box1, key1.CurrentContext!.FindRenderObject());
        Assert.Same(box2, key2.CurrentContext!.FindRenderObject());
        Assert.Equal(position1, box1.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(position2, box2.LocalToGlobal(new Point(0, 0)));
    }
}
