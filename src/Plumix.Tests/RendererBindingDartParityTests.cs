using System.Text.Json;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart
// Mirrors flutter/packages/flutter/test/rendering/binding_test.dart, multi_view_binding_test.dart,
// binding_pipeline_manifold_test.dart, binding_pipeline_manifold_init_test.dart,
// test/widgets/binding_deferred_first_frame_test.dart, reassemble_test.dart and the rendering and
// scheduler cases of test/foundation/service_extensions_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class RendererBindingDartParityTests : IDisposable
{
    private readonly List<(RenderView View, PipelineOwner Owner)> _views = [];
    private readonly Action<Action> _previousTimerRun = PlatformDispatcher.Instance.TimerRun;
    private readonly List<Action> _timers = [];
    private TimeSpan _clock = TimeSpan.FromSeconds(1);

    // Views other test classes left registered in the process-wide binding; Dart's tests get a
    // fresh binding per file, so these are parked for the duration of each test.
    private readonly RenderView[] _parkedViews;

    public RendererBindingDartParityTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        PlatformDispatcher.Instance.TimerRun = _timers.Add;
        _parkedViews = [.. RendererBinding.Instance.RenderViews];
        foreach (RenderView view in _parkedViews)
        {
            RendererBinding.Instance.RemoveRenderView(view);
        }
    }

    public void Dispose()
    {
        foreach ((RenderView view, PipelineOwner owner) in _views)
        {
            RemoveView(view, owner);
        }

        foreach (RenderView view in _parkedViews)
        {
            RendererBinding.Instance.AddRenderView(view);
        }

        PlatformDispatcher.Instance.TimerRun = _previousTimerRun;
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void HandleMetricsChanged_ForcesAFrameOnlyForARegisteredViewWithAChild()
    {
        // binding_test.dart: "handleMetricsChanged does not scheduleForcedFrame unless there a
        // registered renderView with a child".
        RendererBinding binding = RendererBinding.Instance;
        Assert.False(Scheduler.HasScheduledFrame);
        binding.HandleMetricsChanged();
        Assert.False(Scheduler.HasScheduledFrame);

        var renderView = new RenderView(new FlutterView(new Size(800, 600), viewId: 8101));
        binding.AddRenderView(renderView);
        try
        {
            binding.HandleMetricsChanged();
            Assert.False(Scheduler.HasScheduledFrame);

            renderView.Child = new RenderLimitedBox();
            binding.HandleMetricsChanged();
            Assert.True(Scheduler.HasScheduledFrame);
        }
        finally
        {
            binding.RemoveRenderView(renderView);
        }
    }

    [Fact]
    public void DebugDumpSemanticsTree_PrintsTheExplanationWhenSemanticsAreUnavailable()
    {
        // binding_test.dart: "debugDumpSemantics prints explanation when semantics are unavailable".
        var renderView = new RenderView(new FlutterView(new Size(800, 600), viewId: 8102));
        RendererBinding.Instance.AddRenderView(renderView);
        List<string?> printed = [];
        DebugPrintCallback previous = Print.DebugPrint;
        Print.DebugPrint = (message, _) => printed.Add(message);
        try
        {
            RenderingDebug.DebugDumpSemanticsTree();
        }
        finally
        {
            Print.DebugPrint = previous;
            RendererBinding.Instance.RemoveRenderView(renderView);
        }

        string message = Assert.Single(printed)!;
        Assert.StartsWith("Semantics not generated", message);
        Assert.EndsWith(
            "For performance reasons, the framework only generates semantics when asked to do so by the platform.\n"
            + "Usually, platforms only ask for semantics when assistive technologies (like screen readers) are "
            + "running.\n"
            + "To generate semantics, try turning on an assistive technology (like VoiceOver or TalkBack) on your "
            + "device.",
            message);
    }

    [Fact]
    public void DebugDumpSemanticsTree_PrintsTheTreeOfAViewWithSemantics()
    {
        using var handle = new SemanticsScope();
        (RenderView view, _) = AddView(8103, Labelled("dumped", () => { }));
        PumpFrame();
        List<string?> printed = [];
        DebugPrintCallback previous = Print.DebugPrint;
        Print.DebugPrint = (message, _) => printed.Add(message);
        try
        {
            RenderingDebug.DebugDumpSemanticsTree(DebugSemanticsDumpOrder.InverseHitTest);
        }
        finally
        {
            Print.DebugPrint = previous;
        }

        string dump = Assert.Single(printed)!;
        Assert.Equal(view.DebugSemantics!.ToStringDeep(DebugSemanticsDumpOrder.InverseHitTest), dump);
        Assert.Contains("label: \"dumped\"", dump);
    }

    [Fact]
    public void DebugDumpPipelineOwnerTree_PrintsTheRootOwnerTree()
    {
        AddView(8104);
        List<string?> printed = [];
        DebugPrintCallback previous = Print.DebugPrint;
        Print.DebugPrint = (message, _) => printed.Add(message);
        try
        {
            RenderingDebug.DebugDumpPipelineOwnerTree();
        }
        finally
        {
            Print.DebugPrint = previous;
        }

        Assert.Equal(RendererBinding.Instance.RootPipelineOwner.ToStringDeep(), Assert.Single(printed));
    }

    [Fact]
    public void BindingPipelineManifold_RequestsAVisualUpdateForTheOwnerTree()
    {
        // binding_pipeline_manifold_test.dart: "BindingPipelineManifold notifies binding if render
        // object managed by binding's PipelineOwner tree needs visual update".
        var box = new RenderLimitedBox();
        AddView(8105, box);
        PumpFrame();
        Assert.False(Scheduler.HasScheduledFrame);

        box.MarkNeedsLayout();

        Assert.True(Scheduler.HasScheduledFrame);
    }

    [Fact]
    public void PlatformSemanticsEnabled_EnablesSemanticsAndTellsThePlatformTheTreeIsOn()
    {
        // binding_pipeline_manifold_init_test.dart: the platform's semantics state reaches the
        // semantics binding and every owner in the tree.
        var child = new PipelineOwner(onSemanticsUpdate: static _ => { });
        PipelineOwner root = RendererBinding.Instance.RootPipelineOwner;
        root.AdoptChild(child);
        try
        {
            Assert.False(SemanticsBinding.Instance.SemanticsEnabled);
            PlatformDispatcher.Instance.UpdateSemanticsEnabled(true);

            Assert.True(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.Equal(1, SemanticsBinding.Instance.DebugOutstandingSemanticsHandles);
            Assert.True(PlatformDispatcher.Instance.SemanticsTreeEnabled);
            Assert.NotNull(child.SemanticsOwner);

            PlatformDispatcher.Instance.UpdateSemanticsEnabled(false);

            Assert.False(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.False(PlatformDispatcher.Instance.SemanticsTreeEnabled);
            Assert.Null(child.SemanticsOwner);
        }
        finally
        {
            PlatformDispatcher.Instance.UpdateSemanticsEnabled(false);
            root.DropChild(child);
        }
    }

    [Fact]
    public void PerformSemanticsAction_IsPerformedOnTheNamedView()
    {
        // multi_view_binding_test.dart: "semantics actions are performed on the right view".
        using var handle = new SemanticsScope();
        int firstTaps = 0;
        int secondTaps = 0;
        (_, PipelineOwner firstOwner) = AddView(8111, Labelled("first", () => firstTaps += 1));
        (_, PipelineOwner secondOwner) = AddView(8112, Labelled("second", () => secondTaps += 1));
        PumpFrame();
        int firstNode = FindNode(firstOwner, "first");
        int secondNode = FindNode(secondOwner, "second");

        PlatformDispatcher.Instance.DispatchSemanticsActionEvent(
            new SemanticsActionEvent(SemanticsActions.Tap, 8111, firstNode));
        Assert.Equal((1, 0), (firstTaps, secondTaps));

        PlatformDispatcher.Instance.DispatchSemanticsActionEvent(
            new SemanticsActionEvent(SemanticsActions.Tap, 8112, secondNode));
        Assert.Equal((1, 1), (firstTaps, secondTaps));

        // An unknown view ignores the action.
        PlatformDispatcher.Instance.DispatchSemanticsActionEvent(
            new SemanticsActionEvent(SemanticsActions.Tap, 8113, firstNode));
        Assert.Equal((1, 1), (firstTaps, secondTaps));
    }

    [Fact]
    public void GetRectOfSemanticsNodeInViewCoordinates_ResolvesOnlyRegisteredViews()
    {
        using var handle = new SemanticsScope();
        (_, PipelineOwner owner) = AddView(8114, Labelled("rect", () => { }));
        PumpFrame();
        int node = FindNode(owner, "rect");

        Assert.Equal(
            new Rect(0, 0, 100, 100),
            SemanticsBinding.Instance.GetRectOfSemanticsNodeInViewCoordinates(8114, node));
        Assert.Null(SemanticsBinding.Instance.GetRectOfSemanticsNodeInViewCoordinates(8199, node));
        Assert.Null(SemanticsBinding.Instance.GetRectOfSemanticsNodeInViewCoordinates(8114, -1));
    }

    [Fact]
    public void DrawFrame_CompositesEveryRegisteredView()
    {
        // multi_view_binding_test.dart: "all registered renderviews are asked to composite frame".
        (RenderView first, PipelineOwner firstOwner) = AddView(8121);
        (RenderView second, _) = AddView(8122);
        int firstRenders = 0;
        int secondRenders = 0;
        first.FlutterView.RenderRequested += _ => firstRenders += 1;
        second.FlutterView.RenderRequested += _ => secondRenders += 1;

        PumpFrame();
        Assert.Equal((1, 1), (firstRenders, secondRenders));

        RemoveView(first, firstOwner);
        _views.RemoveAll(entry => ReferenceEquals(entry.View, first));
        PumpFrame();
        Assert.Equal((1, 2), (firstRenders, secondRenders));
    }

    [Fact]
    public void HitTestInView_ReachesTheViewThenTheBinding()
    {
        // multi_view_binding_test.dart: "hit-testing reaches the right view". Dart's views are empty
        // and its RenderView always adds itself; Plumix's only adds itself when a child is hit (the
        // RenderView.HitTest row of docs/ai/BACKLOG.md), so each view here carries an opaque child.
        var firstChild = new RenderPointerListener(behavior: HitTestBehavior.Opaque);
        var secondChild = new RenderPointerListener(behavior: HitTestBehavior.Opaque);
        (RenderView first, _) = AddView(8131, firstChild);
        (RenderView second, _) = AddView(8132, secondChild);
        PumpFrame();

        var firstResult = new HitTestResult();
        RendererBinding.Instance.HitTestInView(firstResult, new Point(0, 0), 8131);
        Assert.Equal<object>(
            [firstChild, first, GestureBinding.Instance],
            firstResult.Path.Select(static entry => entry.Target));

        var secondResult = new HitTestResult();
        RendererBinding.Instance.HitTestInView(secondResult, new Point(0, 0), 8132);
        Assert.Equal<object>(
            [secondChild, second, GestureBinding.Instance],
            secondResult.Path.Select(static entry => entry.Target));

        var unknownResult = new HitTestResult();
        RendererBinding.Instance.HitTestInView(unknownResult, new Point(0, 0), 8133);
        Assert.Equal<object>([GestureBinding.Instance], unknownResult.Path.Select(static entry => entry.Target));
    }

    [Fact]
    public void DeferFirstFrame_StopsSendingFramesUntilEveryDeferralIsAllowed()
    {
        // binding_deferred_first_frame_test.dart: "deferFirstFrame/allowFirstFrame stops sending
        // frames to engine" and "Two widgets can defer frames".
        RendererBinding binding = RendererBinding.Instance;
        (RenderView view, _) = AddView(8141);
        int renders = 0;
        view.FlutterView.RenderRequested += _ => renders += 1;
        Assert.True(binding.SendFramesToEngine);

        binding.DeferFirstFrame();
        binding.DeferFirstFrame();
        Assert.False(binding.SendFramesToEngine);
        PumpFrame();
        Assert.Equal(0, renders);

        binding.AllowFirstFrame();
        Assert.False(binding.SendFramesToEngine);
        // Every allowFirstFrame before the first frame schedules a warm-up frame.
        RunTimers();
        Assert.Equal(0, renders);

        binding.AllowFirstFrame();
        Assert.True(binding.SendFramesToEngine);
        RunTimers();
        Assert.Equal(1, renders);

        // Once the first frame was sent, a deferral has no effect.
        binding.DeferFirstFrame();
        Assert.True(binding.SendFramesToEngine);
        binding.AllowFirstFrame();

        binding.ResetFirstFrameSent();
        binding.DeferFirstFrame();
        Assert.False(binding.SendFramesToEngine);
        binding.AllowFirstFrame();
        Assert.True(binding.SendFramesToEngine);
    }

    [Fact]
    public void DeferFirstFrame_FromAWidgetHoldsTheFrameBackUntilItIsAllowed()
    {
        // binding_deferred_first_frame_test.dart, with the deferring widget of that test.
        using var tester = new FrameworkDartTester();
        RendererBinding.Instance.ResetFirstFrameSent();
        var key = new LabeledGlobalKey<DeferringState>("deferring");

        tester.PumpWidget(new DeferringWidget(key));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        tester.Pump();
        Assert.False(RendererBinding.Instance.SendFramesToEngine);

        key.CurrentState!.Allow();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        tester.Pump();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
    }

    [Fact]
    public async Task PerformReassemble_ReassemblesTheViewsAndCompletesAfterTheWarmUpFrame()
    {
        // reassemble_test.dart and service_extensions_test.dart's `hasReassemble`.
        var box = new RenderLimitedBox();
        AddView(8151, box);
        PumpFrame();
        Assert.False(box.DebugNeedsLayout);

        Task reassembled = RendererBinding.Instance.PerformReassemble();

        Assert.True(box.DebugNeedsLayout);
        Assert.False(reassembled.IsCompleted);
        RunTimers();
        await reassembled.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(box.DebugNeedsLayout);
    }

    [Fact]
    public async Task ServiceExtension_DebugPaint_ReadsWritesAndPostsStateChanges()
    {
        List<IReadOnlyDictionary<string, object?>> events = [];
        void OnEvent(string kind, IReadOnlyDictionary<string, object?> data)
        {
            if (kind == "Flutter.ServiceExtensionStateChanged")
            {
                events.Add(data);
            }
        }

        BindingBase.EventPosted += OnEvent;
        try
        {
            Assert.Equal("false", (await Extension("debugPaint"))["enabled"]);
            Assert.Empty(events);

            Assert.Equal("true", (await Extension("debugPaint", ("enabled", "true")))["enabled"]);
            Assert.True(RenderingDebug.PaintSizeEnabled);
            IReadOnlyDictionary<string, object?> changed = Assert.Single(events);
            Assert.Equal("ext.flutter.debugPaint", changed["extension"]);
            Assert.Equal("true", changed["value"]);

            Assert.Equal("true", (await Extension("debugPaint"))["enabled"]);
            Assert.Single(events);

            Assert.Equal("false", (await Extension("debugPaint", ("enabled", "false")))["enabled"]);
            Assert.False(RenderingDebug.PaintSizeEnabled);
            Assert.Equal(2, events.Count);
            Assert.Equal("false", events[1]["value"]);
        }
        finally
        {
            BindingBase.EventPosted -= OnEvent;
            RenderingDebug.PaintSizeEnabled = false;
        }
    }

    [Theory]
    [InlineData("debugPaintBaselinesEnabled")]
    [InlineData("debugDisableClipLayers")]
    [InlineData("debugDisablePhysicalShapeLayers")]
    [InlineData("debugDisableOpacityLayers")]
    public async Task ServiceExtension_RepaintingFlags_ScheduleAFrameOnlyWhenTheValueChanges(string name)
    {
        AddView(8161, new RenderLimitedBox());
        PumpFrame();
        try
        {
            Assert.Equal("false", (await Extension(name))["enabled"]);
            Assert.False(Scheduler.HasScheduledFrame);

            Assert.Equal("true", (await Extension(name, ("enabled", "true")))["enabled"]);
            Assert.True(Scheduler.HasScheduledFrame);
            PumpFrame();

            Assert.Equal("true", (await Extension(name, ("enabled", "true")))["enabled"]);
            Assert.False(Scheduler.HasScheduledFrame);

            Assert.Equal("false", (await Extension(name, ("enabled", "false")))["enabled"]);
            Assert.True(Scheduler.HasScheduledFrame);
            PumpFrame();
        }
        finally
        {
            RenderingDebug.PaintBaselinesEnabled = false;
            RenderingDebug.DisableClipLayers = false;
            RenderingDebug.DisablePhysicalShapeLayers = false;
            RenderingDebug.DisableOpacityLayers = false;
        }
    }

    [Fact]
    public async Task ServiceExtension_RepaintRainbow_RepaintsOnlyWhenTurnedOff()
    {
        AddView(8162, new RenderLimitedBox());
        PumpFrame();
        try
        {
            Assert.Equal("false", (await Extension("repaintRainbow"))["enabled"]);
            Assert.Equal("true", (await Extension("repaintRainbow", ("enabled", "true")))["enabled"]);
            Assert.True(RenderingDebug.RepaintRainbowEnabled);
            Assert.False(Scheduler.HasScheduledFrame);

            Assert.Equal("false", (await Extension("repaintRainbow", ("enabled", "false")))["enabled"]);
            Assert.False(RenderingDebug.RepaintRainbowEnabled);
            Assert.True(Scheduler.HasScheduledFrame);
        }
        finally
        {
            RenderingDebug.RepaintRainbowEnabled = false;
        }
    }

    [Theory]
    [InlineData("profileRenderObjectPaints")]
    [InlineData("profileRenderObjectLayouts")]
    public async Task ServiceExtension_ProfileFlags_NeverScheduleAFrame(string name)
    {
        try
        {
            Assert.Equal("false", (await Extension(name))["enabled"]);
            Assert.Equal("true", (await Extension(name, ("enabled", "true")))["enabled"]);
            Assert.True(name == "profileRenderObjectPaints"
                ? RenderingDebug.ProfilePaintsEnabled
                : RenderingDebug.ProfileLayoutsEnabled);
            Assert.Equal("false", (await Extension(name, ("enabled", "false")))["enabled"]);
            Assert.False(Scheduler.HasScheduledFrame);
        }
        finally
        {
            RenderingDebug.ProfilePaintsEnabled = false;
            RenderingDebug.ProfileLayoutsEnabled = false;
        }
    }

    [Fact]
    public async Task ServiceExtension_TimeDilation_ReadsAndWritesTheSchedulerValue()
    {
        List<IReadOnlyDictionary<string, object?>> events = [];
        void OnEvent(string kind, IReadOnlyDictionary<string, object?> data)
        {
            if (kind == "Flutter.ServiceExtensionStateChanged")
            {
                events.Add(data);
            }
        }

        BindingBase.EventPosted += OnEvent;
        try
        {
            Assert.Equal(1.0, Scheduler.TimeDilation);
            Assert.Equal("1.0", (await Extension("timeDilation"))["timeDilation"]);
            Assert.Empty(events);

            Assert.Equal("100.0", (await Extension("timeDilation", ("timeDilation", "100.0")))["timeDilation"]);
            Assert.Equal(100.0, Scheduler.TimeDilation);
            Assert.Equal("ext.flutter.timeDilation", Assert.Single(events)["extension"]);
            Assert.Equal("100.0", events[0]["value"]);

            Assert.Equal("100.0", (await Extension("timeDilation"))["timeDilation"]);
            Assert.Single(events);

            Assert.Equal("1.0", (await Extension("timeDilation", ("timeDilation", "1.0")))["timeDilation"]);
            Assert.Equal(1.0, Scheduler.TimeDilation);
            Assert.Equal(2, events.Count);
            Assert.False(Scheduler.HasScheduledFrame);
        }
        finally
        {
            BindingBase.EventPosted -= OnEvent;
            Scheduler.TimeDilation = 1.0;
        }
    }

    [Fact]
    public async Task ServiceExtension_Dumps_ReturnTheCollectedTrees()
    {
        (RenderView view, _) = AddView(8171);
        PumpFrame();

        Assert.Equal(view.ToStringDeep(), (await Extension("debugDumpRenderTree"))["data"]);
        Assert.Equal(view.DebugLayer!.ToStringDeep(), (await Extension("debugDumpLayerTree"))["data"]);
        string traversal = (string)(await Extension("debugDumpSemanticsTreeInTraversalOrder"))["data"]!;
        string inverse = (string)(await Extension("debugDumpSemanticsTreeInInverseHitTestOrder"))["data"]!;
        Assert.StartsWith($"Semantics not generated for {view}.\n", traversal);
        Assert.Equal(traversal, inverse);
    }

    [Fact]
    public void ServiceExtensions_RegisterTheRenderingAndSchedulerExtensions()
    {
        string[] expected =
        [
            "timeDilation",
            "debugPaint",
            "debugPaintBaselinesEnabled",
            "repaintRainbow",
            "debugDumpLayerTree",
            "debugDisableClipLayers",
            "debugDisablePhysicalShapeLayers",
            "debugDisableOpacityLayers",
            "debugDumpRenderTree",
            "debugDumpSemanticsTreeInTraversalOrder",
            "debugDumpSemanticsTreeInInverseHitTestOrder",
            "profileRenderObjectPaints",
            "profileRenderObjectLayouts",
        ];

        foreach (string name in expected)
        {
            Assert.Contains($"ext.flutter.{name}", BindingBase.ServiceExtensionMethods);
        }

        Assert.Throws<ArgumentException>(() => BindingBase.RegisterSignalServiceExtension(
            "debugPaint",
            static () => Task.CompletedTask));
    }

    [Fact]
    public async Task InvokeServiceExtension_WrapsTheResultAndReportsErrors()
    {
        PlatformDispatcher.Instance.TimerRun = static callback => callback();
        ServiceExtensionResponse response = await BindingBase.InvokeServiceExtensionAsync("ext.flutter.debugPaint");
        Assert.False(response.IsError);
        using (JsonDocument result = JsonDocument.Parse(response.Result!))
        {
            Assert.Equal("false", result.RootElement.GetProperty("enabled").GetString());
            Assert.Equal("_extensionType", result.RootElement.GetProperty("type").GetString());
            Assert.Equal("ext.flutter.debugPaint", result.RootElement.GetProperty("method").GetString());
        }

        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            ServiceExtensionResponse error = await BindingBase.InvokeServiceExtensionAsync(
                "ext.flutter.timeDilation",
                new Dictionary<string, string> { ["timeDilation"] = "not a number" });
            Assert.True(error.IsError);
            Assert.Equal(ServiceExtensionResponse.ExtensionError, error.ErrorCode);
            using JsonDocument detail = JsonDocument.Parse(error.ErrorDetail!);
            Assert.Equal("ext.flutter.timeDilation", detail.RootElement.GetProperty("method").GetString());
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.IsType<FormatException>(details.Exception);
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        ServiceExtensionResponse unknown = await BindingBase.InvokeServiceExtensionAsync("ext.flutter.nope");
        Assert.Equal(ServiceExtensionResponse.MethodNotFound, unknown.ErrorCode);
    }

    private static async Task<Dictionary<string, object?>> Extension(
        string name,
        params (string Key, string Value)[] arguments)
    {
        var parameters = arguments.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        // flutter_test's harness calls the registered callback directly, bypassing the VM wrapper.
        return await BindingBase.RegisteredCallbacks[name](parameters);
    }

    private (RenderView View, PipelineOwner Owner) AddView(int viewId, RenderBox? child = null)
    {
        var view = new RenderView(new FlutterView(new Size(100, 100), viewId: viewId), child: child);
        var owner = new PipelineOwner(onSemanticsUpdate: static _ => { }) { RootNode = view };
        RendererBinding.Instance.RootPipelineOwner.AdoptChild(owner);
        RendererBinding.Instance.AddRenderView(view);
        view.PrepareInitialFrame();
        _views.Add((view, owner));
        return (view, owner);
    }

    private static void RemoveView(RenderView view, PipelineOwner owner)
    {
        if (RendererBinding.Instance.RenderViews.Contains(view))
        {
            RendererBinding.Instance.RemoveRenderView(view);
        }

        owner.RootNode = null;
        RendererBinding.Instance.RootPipelineOwner.DropChild(owner);
    }

    private void PumpFrame()
    {
        _clock += TimeSpan.FromMilliseconds(16);
        Scheduler.HandleBeginFrame(_clock);
        Scheduler.FlushMicrotasks();
        Scheduler.HandleDrawFrame();
    }

    private void RunTimers()
    {
        while (_timers.Count > 0)
        {
            Action timer = _timers[0];
            _timers.RemoveAt(0);
            timer();
        }
    }

    private static RenderBox Labelled(string label, Action onTap) =>
        new RenderSemanticsAnnotations(
            new SemanticsProperties(label: label, onTap: onTap),
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Expand()),
            textDirection: TextDirection.Ltr);

    private static int FindNode(PipelineOwner owner, string label)
    {
        SemanticsNode? found = null;
        owner.SemanticsOwner!.RootNode!.VisitDescendants(node =>
        {
            if (node.Label == label)
            {
                found = node;
                return false;
            }

            return true;
        });
        return found?.Id ?? (owner.SemanticsOwner.RootNode.Label == label ? owner.SemanticsOwner.RootNode.Id : -1);
    }

    /// <summary>An open <see cref="SemanticsBinding.EnsureSemantics"/> handle for a <c>using</c> block.</summary>
    private sealed class SemanticsScope : IDisposable
    {
        private readonly SemanticsHandle _handle = SemanticsBinding.Instance.EnsureSemantics();

        public void Dispose() => _handle.Dispose();
    }

    private sealed class DeferringWidget(GlobalKey<DeferringState> key) : StatefulWidget(key)
    {
        public override State CreateState() => new DeferringState();
    }

    private sealed class DeferringState : State<DeferringWidget>
    {
        private bool _deferred;

        public override void InitState()
        {
            base.InitState();
            RendererBinding.Instance.DeferFirstFrame();
            _deferred = true;
        }

        public void Allow()
        {
            if (_deferred)
            {
                _deferred = false;
                RendererBinding.Instance.AllowFirstFrame();
            }
        }

        public override Widget Build(BuildContext context) => new SizedBox();
    }
}
