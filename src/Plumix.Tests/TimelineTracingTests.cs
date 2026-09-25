using System.Diagnostics.Tracing;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/timeline.dart
// (mirrors flutter/dev/tracing_tests/test/{timeline,inflate_widget_tracing,inflate_widget_update,
// image_cache_tracing}_test.dart, which read the framework's events back through the VM service; here
// an in-process EventListener on TimelineEventSource plays that part)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TimelineTracingTests : IDisposable
{
    private static readonly HashSet<string> InterestingLabels =
    [
        "BUILD",
        "LAYOUT",
        "UPDATING COMPOSITING BITS",
        "PAINT",
        "COMPOSITING",
        "FINALIZE TREE",
        nameof(Placeholder),
        nameof(CustomPaint),
        nameof(RenderCustomPaint),
    ];

    public TimelineTracingTests()
    {
        ResetDebugFlags();
    }

    public void Dispose()
    {
        ResetDebugFlags();
    }

    private static void ResetDebugFlags()
    {
        Timeline.ResetForTests();
        RenderBox.DebugResetIntrinsicsDepthForTests();
        WidgetsDebug.DebugProfileBuildsEnabled = false;
        WidgetsDebug.DebugProfileBuildsEnabledUserWidgets = false;
        WidgetsDebug.DebugEnhanceBuildTimelineArguments = false;
        RenderingDebug.ProfileLayoutsEnabled = false;
        RenderingDebug.ProfilePaintsEnabled = false;
        RenderingDebug.EnhanceLayoutTimelineArguments = false;
        RenderingDebug.EnhancePaintTimelineArguments = false;
        SchedulerDebug.DebugTracePostFrameCallbacks = false;
    }

    // timeline_test.dart 'Timeline': the unchanged-tree half, where the profiling switches add no
    // events because nothing is rebuilt, laid out or painted.
    [NonReleaseFact]
    public void Timeline_RebuildingAnIdenticalTreeAddsNoPerObjectEvents()
    {
        using var tester = new FrameworkDartTester();
        var root = new TracingTestRoot();
        tester.PumpWidget(root);
        using var recorder = new TimelineRecorder();

        foreach (Action<bool> flag in new Action<bool>[]
                 {
                     value => WidgetsDebug.DebugProfileBuildsEnabled = value,
                     value => RenderingDebug.ProfileLayoutsEnabled = value,
                     value => RenderingDebug.ProfilePaintsEnabled = value,
                 })
        {
            flag(true);
            TracingTestRoot.State!.Rebuild();
            tester.Pump();
            Assert.Equal(
                ["BUILD", "LAYOUT", "UPDATING COMPOSITING BITS", "PAINT", "COMPOSITING", "FINALIZE TREE"],
                recorder.TakeInterestingBeginNames(InterestingLabels));
            flag(false);
        }
    }

    // timeline_test.dart 'Timeline': debugProfileBuildsEnabled + debugEnhanceBuildTimelineArguments.
    [DebugOnlyFact]
    public void Timeline_ProfileBuildsReportsEachRebuiltWidgetWithItsProperties()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        using var recorder = new TimelineRecorder();

        WidgetsDebug.DebugProfileBuildsEnabled = true;
        WidgetsDebug.DebugEnhanceBuildTimelineArguments = true;
        Color white = new Color(0xFFFFFFFF);
        TracingTestRoot.State!.UpdateWidget(new Placeholder(key: new UniqueKey(), color: white));
        tester.Pump();

        List<TimelineRecorder.Event> events = recorder.TakeInterestingBegins(InterestingLabels);
        Assert.Equal(
            [
                "BUILD", "Placeholder", "CustomPaint", "LAYOUT", "UPDATING COMPOSITING BITS", "PAINT",
                "COMPOSITING", "FINALIZE TREE",
            ],
            events.Select(e => e.Name));
        TimelineRecorder.Event placeholder = events.Single(e => e.Name == nameof(Placeholder));
        Assert.Equal(new ColorProperty("color", white).ToDescription(), placeholder.Arguments["color"]);
    }

    // timeline_test.dart 'Timeline': debugProfileBuildsEnabledUserWidgets. Dart reports the
    // app-created Placeholder (but not the framework's CustomPaint); Plumix has no creation-location
    // tracking, so no widget counts as user-created (docs/ai/DIVERGENCES.md).
    [DebugOnlyFact]
    public void Timeline_ProfileUserWidgetBuildsReportsNoWidgetWithoutCreationTracking()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        using var recorder = new TimelineRecorder();

        WidgetsDebug.DebugProfileBuildsEnabledUserWidgets = true;
        WidgetsDebug.DebugEnhanceBuildTimelineArguments = true;
        var placeholder = new Placeholder(key: new UniqueKey(), color: new Color(0xFFFFFFFF));
        Assert.False(WidgetsDebug.DebugIsWidgetLocalCreation(placeholder));
        TracingTestRoot.State!.UpdateWidget(placeholder);
        tester.Pump();

        Assert.Equal(
            ["BUILD", "LAYOUT", "UPDATING COMPOSITING BITS", "PAINT", "COMPOSITING", "FINALIZE TREE"],
            recorder.TakeInterestingBeginNames(InterestingLabels));
    }

    // timeline_test.dart 'Timeline': debugProfileLayoutsEnabled + debugEnhanceLayoutTimelineArguments.
    [DebugOnlyFact]
    public void Timeline_ProfileLayoutsReportsEachLaidOutRenderObjectWithItsProperties()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        using var recorder = new TimelineRecorder();

        RenderingDebug.ProfileLayoutsEnabled = true;
        RenderingDebug.EnhanceLayoutTimelineArguments = true;
        TracingTestRoot.State!.UpdateWidget(new Placeholder(key: new UniqueKey()));
        tester.Pump();

        List<TimelineRecorder.Event> events = recorder.TakeInterestingBegins(InterestingLabels);
        Assert.Equal(
            [
                "BUILD", "LAYOUT", "RenderCustomPaint", "UPDATING COMPOSITING BITS", "PAINT", "COMPOSITING",
                "FINALIZE TREE",
            ],
            events.Select(e => e.Name));
        AssertRenderCustomPaintArguments(events);
    }

    // timeline_test.dart 'Timeline': debugProfilePaintsEnabled + debugEnhancePaintTimelineArguments.
    [DebugOnlyFact]
    public void Timeline_ProfilePaintsReportsEachPaintedRenderObjectWithItsProperties()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        using var recorder = new TimelineRecorder();

        RenderingDebug.ProfilePaintsEnabled = true;
        RenderingDebug.EnhancePaintTimelineArguments = true;
        TracingTestRoot.State!.UpdateWidget(new Placeholder(key: new UniqueKey()));
        tester.Pump();

        List<TimelineRecorder.Event> events = recorder.TakeInterestingBegins(InterestingLabels);
        Assert.Equal(
            [
                "BUILD", "LAYOUT", "UPDATING COMPOSITING BITS", "PAINT", "RenderCustomPaint", "COMPOSITING",
                "FINALIZE TREE",
            ],
            events.Select(e => e.Name));
        AssertRenderCustomPaintArguments(events);
    }

    private static void AssertRenderCustomPaintArguments(List<TimelineRecorder.Event> events)
    {
        IReadOnlyDictionary<string, string?> args = events.Single(e => e.Name == nameof(RenderCustomPaint)).Arguments;
        Assert.StartsWith("CustomPaint", args["creator"]);
        Assert.Contains("Placeholder", args["creator"]);
        Assert.StartsWith("PlaceholderPainter#", args["painter"]);
    }

    // inflate_widget_tracing_test.dart 'Children of MultiChildRenderObjectElement show up in tracing'.
    [NonReleaseFact]
    public void InflateWidget_ChildrenOfMultiChildRenderObjectElementShowUpInTracing()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new InflateTestRoot());
        using var recorder = new TimelineRecorder();

        WidgetsDebug.DebugProfileBuildsEnabled = true;
        InflateTestRoot.State!.ShowRow();
        tester.Pump();

        Assert.Equal(
            ["InflateTestRoot", "Row", "InflateTestChild", "Container", "InflateTestChild", "Container"],
            recorder.TakeInterestingBeginNames(
                new HashSet<string>
                {
                    nameof(Row), nameof(InflateTestRoot), nameof(InflateTestChild), nameof(Container),
                }));
    }

    // inflate_widget_update_test.dart 'Widgets with updated keys produce well formed timelines'.
    [NonReleaseFact]
    public void InflateWidget_WidgetsWithUpdatedKeysProduceWellFormedTimelines()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new KeyedTestRoot());
        using var recorder = new TimelineRecorder();

        WidgetsDebug.DebugProfileBuildsEnabled = true;
        KeyedTestRoot.State!.UpdateKey();
        tester.Pump();

        int buildCount = 0;
        int depth = 0;
        foreach (TimelineRecorder.Event e in recorder.Take())
        {
            if (e.Kind == TimelineEventSource.SyncBeginId)
            {
                depth++;
                if (e.Name == "BUILD")
                {
                    buildCount++;
                }
            }
            else if (e.Kind == TimelineEventSource.SyncEndId)
            {
                depth--;
                Assert.True(depth >= 0);
                if (e.Name == "BUILD")
                {
                    buildCount--;
                }
            }
        }

        Assert.Equal(0, buildCount);
        Assert.Equal(0, depth);
    }

    // image_cache_tracing_test.dart 'Image cache tracing'.
    [NonReleaseFact]
    public void ImageCache_ReportsPutIfAbsentClearAndEvict()
    {
        var cache = new ImageCache();
        var completer1 = new TestImageStreamCompleter();
        var completer2 = new TestImageStreamCompleter();
        using var recorder = new TimelineRecorder();

        cache.PutIfAbsent("Test", () => completer1);
        cache.Clear();

        completer2.TestSetImage(new ImageInfo(new FakeImage(new Size(1, 1))));
        cache.PutIfAbsent("Test2", () => completer2);
        cache.Evict("Test2");

        List<TimelineRecorder.Event> events = recorder.Take();
        ExpectTimelineEvents(events,
        [
            ("ImageCache.putIfAbsent", new Dictionary<string, string?> { ["key"] = "Test", ["parentId"] = null }),
            ("listener", new Dictionary<string, string?> { ["parentId"] = null }),
            ("ImageCache.clear", new Dictionary<string, string?>
            {
                ["pendingImages"] = "1",
                ["keepAliveImages"] = "0",
                ["liveImages"] = "1",
                ["currentSizeInBytes"] = "0",
                ["parentId"] = null,
            }),
            ("ImageCache.putIfAbsent", new Dictionary<string, string?> { ["key"] = "Test2", ["parentId"] = null }),
            ("ImageCache.evict", new Dictionary<string, string?> { ["sizeInBytes"] = "4", ["parentId"] = null }),
        ]);

        // The putIfAbsent task of a synchronously completing image finishes both its operations in
        // the listener: the `listener` one with the call details, the outer one with the cache size.
        TimelineRecorder.Event[] ends = [.. events.Where(e => e.Kind == TimelineEventSource.AsyncEndId)];
        Assert.Contains(ends, e => e.Name == "listener" && e.Arguments.GetValueOrDefault("syncCall") == "true");
        Assert.Contains(
            ends,
            e => e.Name == "ImageCache.putIfAbsent" && e.Arguments.GetValueOrDefault("currentSize") == "1");
        Assert.Contains(events, e => e.Name == "ImageCache.evict" && e.Arguments["type"] == "keepAlive");
    }

    [NonReleaseFact]
    public void ImageCache_ReportsMissAndPendingEvictionsAndTheCacheSizeChecks()
    {
        var cache = new ImageCache();
        using var recorder = new TimelineRecorder();

        Assert.False(cache.Evict("missing"));
        cache.PutIfAbsent("pending", () => new TestImageStreamCompleter());
        Assert.True(cache.Evict("pending"));

        var completer = new TestImageStreamCompleter();
        completer.TestSetImage(new ImageInfo(new FakeImage(new Size(1, 1))));
        cache.PutIfAbsent("cached", () => completer);
        cache.MaximumSize = 0;
        cache.MaximumSize = 0;

        List<TimelineRecorder.Event> events = recorder.Take();
        Assert.Equal(
            ["miss", "pending"],
            events.Where(e => e.Name == "ImageCache.evict").Select(e => e.Arguments["type"]));
        TimelineRecorder.Event check = events.Single(e => e.Name == "checkCacheSize"
                                                          && e.Kind == TimelineEventSource.AsyncEndId);
        Assert.Equal("1", check.Arguments["endCacheSize"]);
        // Setting the same maximum again is a no-op, so only one setMaximumSize task starts.
        Assert.Single(events, e => e.Name == "ImageCache.setMaximumSize"
                                   && e.Kind == TimelineEventSource.AsyncBeginId);
        Assert.Equal("0", events.First(e => e.Name == "ImageCache.setMaximumSize").Arguments["value"]);
    }

    private static void ExpectTimelineEvents(
        List<TimelineRecorder.Event> events,
        List<(string Name, Dictionary<string, string?> Args)> expected)
    {
        foreach (TimelineRecorder.Event e in events)
        {
            for (int index = 0; index < expected.Count; index += 1)
            {
                if (expected[index].Name == e.Name
                    && expected[index].Args.All(pair => e.Arguments.GetValueOrDefault(pair.Key) == pair.Value))
                {
                    expected.RemoveAt(index);
                }
            }
        }

        Assert.True(
            expected.Count == 0,
            $"Timeline did not contain expected events: {string.Join(", ", expected.Select(e => e.Name))}");
    }

    // scheduler/binding.dart: the frame task, the post-frame block, the warm-up frame and event lock.
    [NonReleaseFact]
    public void Scheduler_FrameReportsFrameAndAnimateTasksAndThePostFrameBlock()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new TimelineRecorder();
            Scheduler.AddPostFrameCallback(_ => { });
            Scheduler.HandleBeginFrame(TimeSpan.FromMilliseconds(16));
            Scheduler.HandleDrawFrame();

            List<TimelineRecorder.Event> events = recorder.Take();
            Assert.Equal(
                [
                    (TimelineEventSource.AsyncBeginId, "Frame"),
                    (TimelineEventSource.AsyncBeginId, "Animate"),
                    (TimelineEventSource.AsyncEndId, "Animate"),
                    (TimelineEventSource.SyncBeginId, "POST_FRAME"),
                    (TimelineEventSource.SyncEndId, "POST_FRAME"),
                    (TimelineEventSource.AsyncEndId, "Frame"),
                ],
                events.Select(e => (e.Kind, e.Name)));
            Assert.Single(events.Where(e => e.Name is "Frame" or "Animate").Select(e => e.TaskId).Distinct());
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [NonReleaseFact]
    public void Scheduler_TracePostFrameCallbacksReportsEachCallbackUnderItsDebugLabel()
    {
        Scheduler.ResetForTests();
        try
        {
            using var recorder = new TimelineRecorder();
            Scheduler.AddPostFrameCallback(_ => { }, debugLabel: "untraced");
            SchedulerDebug.DebugTracePostFrameCallbacks = true;
            Scheduler.AddPostFrameCallback(_ => { }, debugLabel: "traced");
            SchedulerDebug.DebugTracePostFrameCallbacks = false;
            Scheduler.HandleBeginFrame(TimeSpan.FromMilliseconds(16));
            Scheduler.HandleDrawFrame();

            List<string> names = [.. recorder.Take()
                .Where(e => e.Kind == TimelineEventSource.SyncBeginId)
                .Select(e => e.Name)];
            if (Constants.KDebugMode)
            {
                Assert.Equal(["POST_FRAME", "traced"], names);
            }
            else
            {
                Assert.Equal(["POST_FRAME"], names);
            }
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [NonReleaseFact]
    public void Scheduler_ScheduledTaskRunsInATimedBlockThatStepsItsFlow()
    {
        Scheduler.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            using var recorder = new TimelineRecorder();
            Developer.Flow flow = Developer.Flow.Begin();
            Scheduler.ScheduleTask(() => 1, Priority.Animation, debugLabel: "labelled", flow: flow);
            Scheduler.ScheduleTask(() => 2, Priority.Animation);
            while (turns.Count > 0)
            {
                Action turn = turns[0];
                turns.RemoveAt(0);
                turn();
            }

            List<TimelineRecorder.Event> events = recorder.Take();
            Assert.Equal(
                ["labelled", "Scheduled Task"],
                events.Where(e => e.Kind == TimelineEventSource.SyncBeginId).Select(e => e.Name));
            TimelineRecorder.Event flowEvent = Assert.Single(events, e => e.Kind == TimelineEventSource.FlowEventId);
            Assert.Equal(flow.Id, flowEvent.FlowId);
            Assert.Equal(10, flowEvent.FlowType); // Flow.step
            Assert.Equal("labelled", flowEvent.Name);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            Scheduler.ResetForTests();
        }
    }

    [NonReleaseFact]
    public void Scheduler_WarmUpFrameAndEventLockAreTimelineTasksThatEndWithTheFrame()
    {
        Scheduler.ResetForTests();
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            using var recorder = new TimelineRecorder();
            Scheduler.ScheduleWarmUpFrame();
            Assert.Equal(
                ["Warm-up frame", "Lock events"],
                recorder.Take().Where(e => e.Kind == TimelineEventSource.AsyncBeginId).Select(e => e.Name));

            turns[0]();
            turns[1]();

            List<string> ended = [.. recorder.Take()
                .Where(e => e.Kind == TimelineEventSource.AsyncEndId)
                .Select(e => e.Name)];
            Assert.Equal(["Animate", "Frame", "Warm-up frame", "Lock events"], ended);
            Assert.False(Scheduler.Locked);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
            Scheduler.ResetForTests();
        }
    }

    // object.dart: the pipeline flushes carry a " (root)" suffix only on the parentless owner, the
    // semantics flush reports its three phases, and the enhanced arguments describe the dirty lists.
    [DebugOnlyFact]
    public void PipelineOwner_FlushesReportRootSuffixSemanticsPhasesAndDirtyLists()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle semantics = SemanticsBinding.Instance.EnsureSemantics();
        try
        {
            tester.PumpWidget(new TracingTestRoot());
            using var recorder = new TimelineRecorder();

            RenderingDebug.EnhanceLayoutTimelineArguments = true;
            RenderingDebug.EnhancePaintTimelineArguments = true;
            TracingTestRoot.State!.UpdateWidget(new Placeholder(key: new UniqueKey()));
            tester.Pump();

            List<TimelineRecorder.Event> begins =
                [.. recorder.Take().Where(e => e.Kind == TimelineEventSource.SyncBeginId)];
            List<string> names = [.. begins.Select(e => e.Name)];
            Assert.Contains("LAYOUT (root)", names);
            Assert.Contains("UPDATING COMPOSITING BITS (root)", names);
            Assert.Contains("PAINT (root)", names);
            Assert.Contains("SEMANTICS (root)", names);
            int semanticsIndex = names.IndexOf("SEMANTICS");
            Assert.True(semanticsIndex >= 0);
            Assert.Equal(
                ["Semantics.updateChildren", "Semantics.ensureGeometry", "Semantics.ensureSemanticsNode"],
                names.Skip(semanticsIndex + 1).Take(3));

            TimelineRecorder.Event layout = begins.First(e => e.Name == "LAYOUT");
            Assert.Equal("1", layout.Arguments["dirty count"]);
            Assert.StartsWith("[", layout.Arguments["dirty list"]);
            TimelineRecorder.Event paint = begins.First(e => e.Name == "PAINT");
            Assert.NotNull(paint.Arguments["dirty count"]);
        }
        finally
        {
            semantics.Dispose();
        }
    }

    [DebugOnlyFact]
    public void BuildOwner_BuildScopeEnhancedArgumentsDescribeTheScope()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        using var recorder = new TimelineRecorder();

        WidgetsDebug.DebugEnhanceBuildTimelineArguments = true;
        TracingTestRoot.State!.Rebuild();
        tester.Pump();

        TimelineRecorder.Event build = recorder.Take().First(e => e.Name == "BUILD");
        Assert.Equal("1", build.Arguments["build scope dirty count"]);
        Assert.Contains("TracingTestRoot", build.Arguments["build scope dirty list"]);
        Assert.Equal("1", build.Arguments["lock level"]);
        Assert.NotNull(build.Arguments["scope context"]);
    }

    [NonReleaseFact]
    public void BuildOwner_ReassembleAndRendererReassembleReportHotReloadBlocks()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new TracingTestRoot());
        List<Action> turns = [];
        Action<Action> previousTimerRun = PlatformDispatcher.Instance.TimerRun;
        PlatformDispatcher.Instance.TimerRun = turns.Add;
        try
        {
            using var recorder = new TimelineRecorder();
            tester.Owner.Reassemble(tester.Root);
            _ = RendererBinding.Instance.PerformReassemble();

            // Run the warm-up frame PerformReassemble schedules here, instead of letting the
            // platform timer run it during a later test.
            while (turns.Count > 0)
            {
                Action turn = turns[0];
                turns.RemoveAt(0);
                turn();
            }

            Scheduler.FlushMicrotasks();
            Assert.False(Scheduler.Locked);
            List<string> names = [.. recorder.Take()
                .Where(e => e.Kind == TimelineEventSource.SyncBeginId)
                .Select(e => e.Name)];
            Assert.Contains("Preparing Hot Reload (widgets)", names);
            Assert.Contains("Preparing Hot Reload (layout)", names);
        }
        finally
        {
            PlatformDispatcher.Instance.TimerRun = previousTimerRun;
        }
    }

    // box.dart `_computeWithTimeline`: only the outermost intrinsic computation is reported, unless
    // debugProfileLayoutsEnabled asks for all of them.
    [DebugOnlyFact]
    public void RenderBox_IntrinsicsReportOnlyTheOutermostComputationUnlessLayoutsAreProfiled()
    {
        var inner = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10, height: 20));
        var outer = new RenderPadding(new Thickness(1), child: inner);
        using var recorder = new TimelineRecorder();

        outer.GetMinIntrinsicWidth(double.PositiveInfinity);
        List<TimelineRecorder.Event> begins =
            [.. recorder.Take().Where(e => e.Kind == TimelineEventSource.SyncBeginId)];
        TimelineRecorder.Event only = Assert.Single(begins);
        Assert.Equal("RenderPadding intrinsics", only.Name);
        Assert.Equal("minWidth", only.Arguments["intrinsics dimension"]);
        Assert.Equal("Infinity", only.Arguments["intrinsics argument"]);

        RenderingDebug.ProfileLayoutsEnabled = true;
        outer.GetMaxIntrinsicHeight(100);
        outer.GetDryLayout(new BoxConstraints(0, 100, 0, 100));
        Assert.Equal(
            [
                "RenderPadding intrinsics", "RenderConstrainedBox intrinsics",
                "RenderPadding.getDryLayout", "RenderConstrainedBox.getDryLayout",
            ],
            recorder.Take().Where(e => e.Kind == TimelineEventSource.SyncBeginId).Select(e => e.Name));
    }

    [NonReleaseFact]
    public void Timeline_EventsAreNotReportedWhileNoListenerIsAttached()
    {
        // A block that starts before any listener attaches stays unreported even if one attaches
        // before it finishes, and the stack stays balanced.
        Timeline.StartSync("before");
        using var recorder = new TimelineRecorder();
        Timeline.FinishSync();
        Timeline.StartSync("after", new Dictionary<string, object?> { ["n"] = 1, ["missing"] = null });
        Timeline.FinishSync();
        Timeline.InstantSync("instant");

        List<TimelineRecorder.Event> events = recorder.Take();
        Assert.Equal(
            [
                (TimelineEventSource.SyncBeginId, "after"),
                (TimelineEventSource.SyncEndId, "after"),
                (TimelineEventSource.InstantId, "instant"),
            ],
            events.Select(e => (e.Kind, e.Name)));
        Assert.Equal("1", events[0].Arguments["n"]);
        Assert.Null(events[0].Arguments["missing"]);
        Assert.Throws<InvalidOperationException>(Timeline.FinishSync);
    }

    [NonReleaseFact]
    public void TimelineTask_ReportsParentAndFilterKeyAndRejectsUnevenCalls()
    {
        using var recorder = new TimelineRecorder();
        var parent = new TimelineTask();
        var task = new TimelineTask(parent: parent, filterKey: "key");
        task.Start("op", new Dictionary<string, object?> { ["a"] = "b" });
        Assert.Throws<InvalidOperationException>(() => task.Pass());
        task.Instant("mark");
        task.Finish();
        Assert.Throws<InvalidOperationException>(() => task.Finish());
        long id = task.Pass();

        List<TimelineRecorder.Event> events = recorder.Take();
        Assert.Equal("b", events[0].Arguments["a"]);
        Assert.Equal("key", events[0].Arguments[TimelineTask.FilterKey]);
        Assert.Equal(
            parent.Pass().ToString("x", System.Globalization.CultureInfo.InvariantCulture),
            events[0].Arguments["parentId"]);
        Assert.Equal("key", events[1].Arguments[TimelineTask.FilterKey]);
        Assert.Equal("key", events[2].Arguments[TimelineTask.FilterKey]);
        Assert.All(events, e => Assert.Equal(id, e.TaskId));
        Assert.Equal(id, TimelineTask.WithTaskId(id).Pass());
    }

    private sealed class TracingTestRoot : StatefulWidget
    {
        public static TracingTestRootState? State { get; set; }

        public override Widgets.State CreateState() => new TracingTestRootState();
    }

    private sealed class TracingTestRootState : State<TracingTestRoot>
    {
        private Widget _widget = new Placeholder();

        public override void InitState()
        {
            base.InitState();
            TracingTestRoot.State = this;
        }

        public void UpdateWidget(Widget newWidget) => SetState(() => _widget = newWidget);

        public void Rebuild() => SetState(() => { });

        public override Widget Build(BuildContext context) => _widget;
    }

    private sealed class InflateTestRoot : StatefulWidget
    {
        public static InflateTestRootState? State { get; set; }

        public override Widgets.State CreateState() => new InflateTestRootState();
    }

    private sealed class InflateTestRootState : State<InflateTestRoot>
    {
        private bool _showRow;

        public override void InitState()
        {
            base.InitState();
            InflateTestRoot.State = this;
        }

        public void ShowRow() => SetState(() => _showRow = true);

        public override Widget Build(BuildContext context)
        {
            // Dart's test has no Directionality either and only logs the RenderFlex assertion; here a
            // reported error would fail the harness, so the Row gets one.
            return _showRow
                ? new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new Row(children: [new InflateTestChild(), new InflateTestChild()]))
                : new Container();
        }
    }

    private sealed class InflateTestChild : StatelessWidget
    {
        public override Widget Build(BuildContext context) => new Container();
    }

    private sealed class KeyedTestRoot : StatefulWidget
    {
        public static KeyedTestRootState? State { get; set; }

        public override Widgets.State CreateState() => new KeyedTestRootState();
    }

    private sealed class KeyedTestRootState : State<KeyedTestRoot>
    {
        private readonly Key _globalKey = new LabeledGlobalKey<State>("global");
        private Key _localKey = new UniqueKey();

        public override void InitState()
        {
            base.InitState();
            KeyedTestRoot.State = this;
        }

        public void UpdateKey() => SetState(() => _localKey = new UniqueKey());

        public override Widget Build(BuildContext context)
        {
            return new Center(
                key: _localKey,
                child: new SizedBox(key: _globalKey, width: 100, height: 100));
        }
    }

    private sealed class FakeImage(Size size) : IImage
    {
        public Size Size { get; } = size;

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }

    private sealed class TestImageStreamCompleter : ImageStreamCompleter
    {
        public void TestSetImage(ImageInfo image) => SetImage(image);
    }
}

/// <summary>
/// Records the <see cref="TimelineEventSource"/> events written on the creating thread, the way
/// Flutter's tracing tests read the VM timeline back.
/// </summary>
internal sealed class TimelineRecorder : EventListener
{
    private readonly List<Event> _events = [];
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private EventSource? _source;

    public TimelineRecorder()
    {
        // Events only flow once the constructor has run, so a base-constructor callback that found
        // the source early is enabled here.
        if (_source is not null)
        {
            EnableEvents(_source, EventLevel.Verbose);
        }
    }

    public List<Event> Take()
    {
        lock (_events)
        {
            List<Event> taken = [.. _events];
            _events.Clear();
            return taken;
        }
    }

    public List<Event> TakeInterestingBegins(IReadOnlySet<string> labels) =>
        [.. Take().Where(e => e.Kind == TimelineEventSource.SyncBeginId && labels.Contains(e.Name))];

    public List<string> TakeInterestingBeginNames(IReadOnlySet<string> labels) =>
        [.. TakeInterestingBegins(labels).Select(e => e.Name)];

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Plumix-Timeline")
        {
            _source = eventSource;
            if (_events is not null)
            {
                EnableEvents(eventSource, EventLevel.Verbose);
            }
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (Environment.CurrentManagedThreadId != _threadId || eventData.Payload is null)
        {
            return;
        }

        IReadOnlyList<object?> payload = eventData.Payload;
        var e = eventData.EventId switch
        {
            TimelineEventSource.SyncBeginId or TimelineEventSource.AsyncBeginId
                or TimelineEventSource.AsyncInstantId or TimelineEventSource.AsyncEndId
                => new Event(eventData.EventId, (string)payload[1]!, Parse((string?)payload[2]), (long)payload[0]!),
            TimelineEventSource.SyncEndId =>
                new Event(eventData.EventId, (string)payload[1]!, Parse(null), (long)payload[0]!),
            TimelineEventSource.InstantId =>
                new Event(eventData.EventId, (string)payload[0]!, Parse((string?)payload[1]), 0),
            TimelineEventSource.FlowEventId => new Event(
                eventData.EventId,
                (string)payload[3]!,
                Parse(null),
                (long)payload[2]!,
                FlowId: (long)payload[0]!,
                FlowType: (int)payload[1]!),
            _ => null,
        };
        if (e is not null)
        {
            lock (_events)
            {
                _events.Add(e);
            }
        }
    }

    private static Dictionary<string, string?> Parse(string? json)
    {
        var result = new Dictionary<string, string?>();
        if (json is null)
        {
            return result;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
        }

        return result;
    }

    internal sealed record Event(
        int Kind,
        string Name,
        IReadOnlyDictionary<string, string?> Arguments,
        long TaskId,
        long FlowId = 0,
        int FlowType = 0);
}
