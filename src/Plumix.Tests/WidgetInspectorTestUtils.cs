using System.Text.Json;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// C#-only test infrastructure: flutter's widget_inspector_test_utils.dart (`TestWidgetInspectorService`,
// `DispatchedEventKey`) and the helpers widget_inspector_test.dart shares between its groups.

namespace Plumix.Tests;

/// <summary>A key for the events <see cref="TestWidgetInspectorService"/> captures.</summary>
/// <remarks>Dart's <c>DispatchedEventKey</c>.</remarks>
internal readonly record struct DispatchedEventKey(string Stream, string EventKind)
{
    public override string ToString() => $"[DispatchedEventKey]({Stream}, {EventKind})";
}

/// <summary>
/// A <see cref="WidgetInspectorService"/> that records the extensions it registers, the events it
/// posts and the objects it inspects instead of handing them to a VM service.
/// </summary>
/// <remarks>Dart's <c>TestWidgetInspectorService</c>.</remarks>
internal class TestWidgetInspectorService : WidgetInspectorService
{
    public TestWidgetInspectorService()
    {
        Selection.AddListener(() => SelectionChangedCallback?.Invoke());
    }

    public Dictionary<string, ServiceExtensionCallback> Extensions { get; } = [];

    public Dictionary<DispatchedEventKey, List<IReadOnlyDictionary<string, object?>>> EventsDispatched { get; } =
        [];

    public List<object?> ObjectsInspected { get; } = [];

    public int RebuildCount { get; set; }

    private static TestWidgetInspectorService? _shared;

    /// <summary>
    /// The service the inspector tests share, created and registered once: Dart's test file installs
    /// one service for all its tests, and the inspector registers its extensions once per process.
    /// </summary>
    public static TestWidgetInspectorService Shared
    {
        get
        {
            if (_shared is null)
            {
                // The binding registers the default service's extensions first, as Dart's does.
                _ = WidgetsBinding.Instance;
                var service = new TestWidgetInspectorService();
                WidgetInspectorService previous = Instance;
                Install(service);
                Restore(previous);
                _shared = service;
            }

            return _shared;
        }
    }

    /// <summary>Makes <paramref name="service"/> the current inspector service and registers its
    /// extensions, the way the test binding's <c>initServiceExtensions</c> does.</summary>
    public static void Install(TestWidgetInspectorService service)
    {
        Instance = service;
        DebugResetServiceExtensionsRegisteredForTests();
        service.InitServiceExtensions((_, _) => { });
    }

    /// <summary>Makes <paramref name="service"/> the current inspector service.</summary>
    public static void Restore(WidgetInspectorService service)
    {
        Instance = service;
    }

    protected override void RegisterServiceExtension(
        string name,
        ServiceExtensionCallback callback,
        RegisterServiceExtensionCallback registerExtension)
    {
        Assert.False(Extensions.ContainsKey(name));
        Extensions[name] = callback;
    }

    protected internal override void PostEvent(
        string eventKind,
        IReadOnlyDictionary<string, object?> eventData,
        string stream = "Extension")
    {
        DispatchedEvents(eventKind, stream).Add(eventData);
    }

    protected internal override void Inspect(object? @object)
    {
        ObjectsInspected.Add(@object);
    }

    public List<IReadOnlyDictionary<string, object?>> DispatchedEvents(string eventKind, string stream = "Extension")
    {
        var key = new DispatchedEventKey(stream, eventKind);
        if (!EventsDispatched.TryGetValue(key, out List<IReadOnlyDictionary<string, object?>>? events))
        {
            events = [];
            EventsDispatched[key] = events;
        }

        return events;
    }

    public List<object?> InspectedObjects() => ObjectsInspected;

    public List<IReadOnlyDictionary<string, object?>> GetServiceExtensionStateChangedEvents(string extensionName) =>
        DispatchedEvents("Flutter.ServiceExtensionStateChanged")
            .Where(@event => Equals(@event["extension"], extensionName))
            .ToList();

    /// <summary>Runs an extension and returns its <c>result</c> after a JSON round trip.</summary>
    public object? TestExtension(string name, Dictionary<string, string> arguments)
    {
        Dictionary<string, object?> result = RunExtension(name, arguments);
        return result["result"];
    }

    /// <summary>Runs a bool extension and returns its <c>enabled</c> string.</summary>
    public string TestBoolExtension(string name, Dictionary<string, string> arguments)
    {
        Dictionary<string, object?> result = RunExtension(name, arguments);
        return (string)result["enabled"]!;
    }

    private Dictionary<string, object?> RunExtension(string name, Dictionary<string, string> arguments)
    {
        Assert.Contains(name, Extensions.Keys);
        Task<Dictionary<string, object?>> task = Extensions[name](arguments);
        Scheduler.FlushMicrotasks();
        Dictionary<string, object?> raw = task.GetAwaiter().GetResult();
        // Emulate the JSON round trip the VM service performs.
        return (Dictionary<string, object?>)JsonRoundTrip.Decode(JsonSerializer.Serialize(raw))!;
    }

    protected override Task ForceRebuild()
    {
        RebuildCount++;
        WidgetsBinding binding = WidgetsBinding.Instance;
        if (binding.RootElement is { } rootElement)
        {
            binding.BuildOwner.Reassemble(rootElement);
        }

        return Task.CompletedTask;
    }

    protected internal override void ResetAllState()
    {
        base.ResetAllState();
        EventsDispatched.Clear();
        ObjectsInspected.Clear();
        RebuildCount = 0;
    }

    /// <summary>Dart's <c>currentPubRootDirectories</c> test extension getter.</summary>
    public List<string> CurrentPubRootDirectories
    {
        get
        {
            Dictionary<string, object?> result = PubRootDirectories(new Dictionary<string, string>())
                .GetAwaiter()
                .GetResult();
            return ((List<string>)result["result"]!).ToList();
        }
    }
}

/// <summary>Decodes JSON into the dictionaries, lists and primitives Dart's <c>json.decode</c> yields.</summary>
internal static class JsonRoundTrip
{
    public static object? Decode(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return Convert(document.RootElement);
    }

    private static object? Convert(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(property => property.Name, property => Convert(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(Convert).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt32(out int value) ? (object)value : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };
}

/// <summary>Dart's <c>button_tester.dart</c> <c>TestButton</c>.</summary>
internal sealed class InspectorTestButton : StatelessWidget
{
    public InspectorTestButton(
        Widget child,
        Action? onPressed,
        HitTestBehavior? behavior = null,
        Key? key = null) : base(key)
    {
        Child = child;
        OnPressed = onPressed;
        Behavior = behavior;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public HitTestBehavior? Behavior { get; }

    public override Widget Build(BuildContext context)
    {
        return new Semantics(
            label: "button",
            button: true,
            enabled: OnPressed is not null,
            onTap: OnPressed,
            child: new GestureDetector(behavior: Behavior, onTap: OnPressed, child: Child));
    }
}

/// <summary>Dart's <c>TestHiddenWidget</c>.</summary>
internal sealed class InspectorTestHiddenWidget : StatelessWidget
{
    public override Widget Build(BuildContext context) => new Container();
}

/// <summary>Dart's <c>TestVisibleWidget</c>.</summary>
internal sealed class InspectorTestVisibleWidget : StatelessWidget
{
    public override Widget Build(BuildContext context) => new Container();
}

/// <summary>Dart's <c>CyclicDiagnostic</c>.</summary>
internal sealed class CyclicDiagnostic(string name) : DiagnosticableTree
{
    public CyclicDiagnostic? Related { get; set; }

    public List<DiagnosticsNode> Children { get; } = [];

    public string Name { get; } = name;

    public override string ToStringShort() => $"CyclicDiagnostic-{Name}";

    public override string ToString() => ToStringShort();

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<CyclicDiagnostic>("related", Related));
    }

    public override List<DiagnosticsNode> DebugDescribeChildren() => Children;
}

/// <summary>Dart's <c>RenderRepaintBoundaryWithDebugPaint</c>.</summary>
internal sealed class RenderRepaintBoundaryWithDebugPaint : RenderRepaintBoundary
{
    protected internal override void DebugPaintSize(PaintingContext context, Point offset)
    {
        base.DebugPaintSize(context, offset);
        var paint = new Paint { Style = PaintingStyle.Stroke, StrokeWidth = 1.0, Color = new Color(0xFFFF0000) };
        var first = new OffsetLayer(offset);
        first.Append(PictureLayerWithCircle(new Point(0, 0), paint));
        context.AddLayer(first);
        context.Canvas.DrawLine(offset, new Point(offset.X + Size.Width, offset.Y + Size.Height), paint);
        var second = new OffsetLayer(offset);
        second.Append(PictureLayerWithCircle(new Point(20.0, 20.0), paint));
        context.AddLayer(second);
        var bluePaint = new Paint { Style = PaintingStyle.Stroke, StrokeWidth = 1.0, Color = new Color(0xFF0000FF) };
        context.Canvas.DrawLine(
            offset,
            new Point(offset.X + Size.Width * 0.5, offset.Y + Size.Height * 0.5),
            bluePaint);
    }

    private PictureLayer PictureLayerWithCircle(Point center, Paint paint)
    {
        var recorder = new PictureRecorder();
        var canvas = new Plumix.UI.Canvas(recorder);
        canvas.DrawCircle(center, 20.0, paint);
        return new PictureLayer(new Rect(Size)) { Picture = recorder.EndRecording() };
    }
}

/// <summary>Dart's <c>RepaintBoundaryWithDebugPaint</c>.</summary>
internal sealed class RepaintBoundaryWithDebugPaint(Widget? child = null, Key? key = null)
    : SingleChildRenderObjectWidget(child, key)
{
    public override RenderObject CreateRenderObject(BuildContext context) => new RenderRepaintBoundaryWithDebugPaint();
}
