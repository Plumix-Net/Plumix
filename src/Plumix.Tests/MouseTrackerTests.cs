using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/rendering/mouse_tracker.dart
// flutter/packages/flutter/lib/src/services/mouse_tracking.dart
// flutter/packages/flutter/lib/src/services/mouse_cursor.dart
// Behaviors mirrored from flutter/packages/flutter/test/rendering/mouse_tracker_test.dart and
// mouse_tracker_cursor_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MouseTrackerTests : IDisposable
{
    private readonly List<string> _log = [];
    private readonly List<Dictionary<string, object?>> _cursorCalls = [];

    public MouseTrackerTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        SystemChannels.MouseCursor.SetPlatformMethodCallHandler(call =>
        {
            var arguments = (System.Collections.IDictionary)call.Arguments!;
            _cursorCalls.Add(new Dictionary<string, object?>
            {
                ["method"] = call.Method,
                ["device"] = arguments["device"],
                ["kind"] = arguments["kind"],
            });
            return Task.FromResult<object?>(null);
        });
    }

    public void Dispose()
    {
        SystemChannels.MouseCursor.SetPlatformMethodCallHandler(null);
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AddedHoverRemoved_ProduceEnterHoverExitAndMouseIsConnectedNotifications()
    {
        using MouseTrackingHarness harness = Build(Region("a"));
        int notifications = 0;
        harness.MouseTracker.AddListener(() => notifications++);

        Assert.False(harness.MouseTracker.MouseIsConnected);

        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        Assert.Equal(["enter:a"], _log);
        Assert.True(harness.MouseTracker.MouseIsConnected);
        Assert.Equal(1, notifications);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(20, 10)));
        Assert.Equal(["hover:a"], _log);
        Assert.Equal(1, notifications);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Removed(new Point(20, 10)));
        Assert.Equal(["exit:a"], _log);
        Assert.False(harness.MouseTracker.MouseIsConnected);
        Assert.Equal(2, notifications);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Added(new Point(30, 10)));
        Assert.Equal(["enter:a"], _log);
        Assert.Equal(3, notifications);
    }

    [Fact]
    public void Stylus_IsTrackedLikeAMouseAndTouchIsIgnored()
    {
        using MouseTrackingHarness harness = Build(Region("a"));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10), kind: PointerDeviceKind.Stylus));
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(12, 10), kind: PointerDeviceKind.Stylus));
        Assert.Equal(["enter:a", "hover:a"], _log);

        _log.Clear();
        // A touch pointer still routes its hover through the hit-test path, but the tracker ignores
        // it, so it produces no enter and no exit.
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(14, 10), device: 9, kind: PointerDeviceKind.Touch));
        Assert.Equal(["hover:a"], _log);
    }

    [Fact]
    public void RemovedEventForAnUnknownDevice_IsANoOp()
    {
        using MouseTrackingHarness harness = Build(Region("a"));

        harness.SendPointer(MouseTrackingHarness.Removed(new Point(10, 10), device: 42));

        Assert.Empty(_log);
        Assert.False(harness.MouseTracker.MouseIsConnected);
    }

    [Fact]
    public void MultipleDevices_KeepSeparateStateAndConnectionIsTheUnionOfThem()
    {
        using MouseTrackingHarness harness = Build(Region("a"));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10), device: 1));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(12, 10), device: 2));
        Assert.Equal(["enter:a", "enter:a"], _log);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Removed(new Point(10, 10), device: 1));
        Assert.Equal(["exit:a"], _log);
        Assert.True(harness.MouseTracker.MouseIsConnected);

        harness.SendPointer(MouseTrackingHarness.Removed(new Point(12, 10), device: 2));
        Assert.False(harness.MouseTracker.MouseIsConnected);
    }

    [Fact]
    public void FrameRecheck_UsesEachDevicesOwnRegisteredView()
    {
        using MouseTrackingHarness first = Build(
            Padding(0, Region("A", cursor: SystemMouseCursors.Text)),
            viewId: 1101);
        using MouseTrackingHarness second = Build(
            Region("B", cursor: SystemMouseCursors.Click),
            viewId: 1102);
        first.SendPointer(MouseTrackingHarness.Added(new Point(10, 10), device: 1, viewId: 1101));
        second.SendPointer(MouseTrackingHarness.Added(new Point(10, 10), device: 2, viewId: 1102));
        Assert.Equal(["enter:A", "enter:B"], _log);

        _log.Clear();
        first.Update(Padding(100, Region("A", cursor: SystemMouseCursors.Text)));

        Assert.Equal(["exit:A"], _log);
    }

    [Fact]
    public void DownMoveAndUp_ProduceNoEnterOrExitWhileThePositionIsUnchanged()
    {
        using MouseTrackingHarness harness = Build(Region("a"));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        _log.Clear();

        var position = new Point(10, 10);
        harness.SendPointer(new PointerDownEvent(1, PointerDeviceKind.Mouse, position, PointerButtons.Primary,
            DateTime.UtcNow));
        harness.SendPointer(new PointerMoveEvent(1, PointerDeviceKind.Mouse, position, PointerButtons.Primary,
            true, DateTime.UtcNow));
        harness.SendPointer(new PointerUpEvent(1, PointerDeviceKind.Mouse, position, PointerButtons.None,
            DateTime.UtcNow));

        Assert.Empty(_log);
    }

    [Fact]
    public void AnnotationAppearingAndDisappearingUnderAStillPointer_FiresOnTheNextFrame()
    {
        using MouseTrackingHarness harness = Build(new SizedBox(width: 200, height: 200));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        Assert.Empty(_log);

        harness.Update(Region("a"));
        Assert.Equal(["enter:a"], _log);

        _log.Clear();
        harness.Update(new SizedBox(width: 200, height: 200));
        // Dart's `MouseRegion` does not fire an exit when it is unmounted: `detach` clears
        // `validForMouseTracker` first, so the tracker skips it.
        Assert.Empty(_log);
    }

    [Fact]
    public void AnnotationMovingOutFromUnderAStillPointer_FiresExitOnTheNextFrame()
    {
        using MouseTrackingHarness harness = Build(Padding(0, Region("a")));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        Assert.Equal(["enter:a"], _log);

        _log.Clear();
        harness.Update(Padding(100, Region("a")));
        Assert.Equal(["exit:a"], _log);

        _log.Clear();
        harness.Update(Padding(0, Region("a")));
        Assert.Equal(["enter:a"], _log);
    }

    [Fact]
    public void SynthesizedEnterAndExit_CarryTheAnnotationTransform()
    {
        Point? enterLocal = null;
        Point? exitLocal = null;
        Widget Tree(double inset) => Padding(
            inset,
            new MouseRegion(
                onEnter: e => enterLocal = e.LocalPosition,
                onExit: e => exitLocal = e.LocalPosition,
                child: new SizedBox(width: 50, height: 50)));

        using MouseTrackingHarness harness = Build(Tree(10));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(30, 25)));
        Assert.Equal(new Point(20, 15), enterLocal);

        harness.SendPointer(MouseTrackingHarness.Removed(new Point(30, 25)));
        Assert.Equal(new Point(20, 15), exitLocal);
    }

    [Fact]
    public void NestedRegions_EnterBackToFrontThenHoverFrontToBackAndExitFrontToBack()
    {
        using MouseTrackingHarness harness = Build(
            new MouseRegion(
                onEnter: _ => _log.Add("enterA"),
                onHover: _ => _log.Add("hoverA"),
                onExit: _ => _log.Add("exitA"),
                child: Padding(
                    10,
                    new MouseRegion(
                        onEnter: _ => _log.Add("enterB"),
                        onHover: _ => _log.Add("hoverB"),
                        onExit: _ => _log.Add("exitB"),
                        child: new SizedBox(width: 50, height: 50)))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(30, 30)));
        Assert.Equal(["enterA", "enterB", "hoverB", "hoverA"], _log);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(400, 400)));
        Assert.Equal(["exitB", "exitA"], _log);
    }

    [Fact]
    public void DisjointSiblings_DispatchEveryExitBeforeAnyEnter()
    {
        using MouseTrackingHarness harness = Build(
            new Stack(
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        child: Region("A", size: 40)),
                    new Positioned(
                        left: 100,
                        top: 0,
                        child: Region("B", size: 40)),
                ]));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Equal(["enter:A", "hover:A"], _log);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(110, 10)));
        Assert.Equal(["exit:A", "enter:B", "hover:B"], _log);

        _log.Clear();
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Equal(["exit:B", "enter:A", "hover:A"], _log);
    }

    [Fact]
    public void ValidForMouseTracker_FollowsAttachmentSoADetachedRegionIsSkipped()
    {
        using MouseTrackingHarness harness = Build(Region("a"));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));

        RenderMouseRegion mounted = Assert.IsType<RenderMouseRegion>(
            FindDescendant<RenderMouseRegion>(harness.RenderView));
        Assert.True(mounted.ValidForMouseTracker);

        _log.Clear();
        harness.Update(new SizedBox(width: 200, height: 200));

        // `detach` clears the flag before the render object leaves the tree, which is what stops the
        // tracker from calling back into a dead node — and why an unmount fires no exit.
        Assert.False(mounted.ValidForMouseTracker);
        Assert.Empty(_log);
    }

    [DebugOnlyFact]
    public void Cursor_UsesTheFirstNonDeferringAnnotationAndFallsBackToBasic()
    {
        using MouseTrackingHarness harness = Build(
            new MouseRegion(
                cursor: SystemMouseCursors.Forbidden,
                child: Padding(
                    10,
                    new MouseRegion(
                        cursor: MouseCursor.Defer,
                        child: new SizedBox(width: 50, height: 50)))));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(30, 30)));
        // The inner region defers, so the outer region's cursor wins.
        Assert.Equal(SystemMouseCursors.Forbidden, harness.MouseTracker.DebugDeviceActiveCursor(1));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(400, 400)));
        Assert.Equal(SystemMouseCursors.Basic, harness.MouseTracker.DebugDeviceActiveCursor(1));
    }

    [Fact]
    public void Cursor_SendsActivateSystemCursorPerChangeAndNothingOnAnUnchangedCursor()
    {
        using MouseTrackingHarness harness = Build(Region("a", cursor: SystemMouseCursors.Grabbing));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        Assert.Equal(
            [Call(1, "grabbing")],
            _cursorCalls);

        // Moving within the same region changes nothing, so no platform call is made.
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(12, 10)));
        Assert.Single(_cursorCalls);

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(400, 400)));
        Assert.Equal([Call(1, "grabbing"), Call(1, "basic")], _cursorCalls);

        // A removal drops the session without activating anything.
        _cursorCalls.Clear();
        harness.SendPointer(MouseTrackingHarness.Removed(new Point(400, 400)));
        Assert.Empty(_cursorCalls);
        Assert.Null(harness.MouseTracker.DebugDeviceActiveCursor(1));
    }

    [DebugOnlyFact]
    public void Cursor_TracksDevicesSeparately()
    {
        using MouseTrackingHarness harness = Build(
            new Stack(
                children:
                [
                    new Positioned(left: 0, top: 0, child: Region("A", size: 40, cursor: SystemMouseCursors.Text)),
                    new Positioned(left: 100, top: 0, child: Region("B", size: 40, cursor: SystemMouseCursors.Click)),
                ]));

        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10), device: 1));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(110, 10), device: 2));

        Assert.Equal(SystemMouseCursors.Text, harness.MouseTracker.DebugDeviceActiveCursor(1));
        Assert.Equal(SystemMouseCursors.Click, harness.MouseTracker.DebugDeviceActiveCursor(2));
        Assert.Equal([Call(1, "text"), Call(2, "click")], _cursorCalls);
    }

    [DebugOnlyFact]
    public void MouseCursorManager_UsesTheFallbackAndSkipsAnEqualCursor()
    {
        var manager = new MouseCursorManager(SystemMouseCursors.Basic);

        manager.HandleDeviceCursorUpdate(3, null, []);
        Assert.Equal(SystemMouseCursors.Basic, manager.DebugDeviceActiveCursor(3));
        Assert.Equal([Call(3, "basic")], _cursorCalls);

        // A distinct-but-equal cursor is the same cursor, so it produces no platform call.
        manager.HandleDeviceCursorUpdate(3, null, [new SystemMouseCursor("basic")]);
        Assert.Single(_cursorCalls);

        manager.HandleDeviceCursorUpdate(3, null, [MouseCursor.Defer, MouseCursor.Defer, SystemMouseCursors.Grab]);
        Assert.Equal([Call(3, "basic"), Call(3, "grab")], _cursorCalls);
    }

    [DebugOnlyFact]
    public void MouseCursorManager_RejectsADeferringFallbackAndToleratesAMissingPlatformHandler()
    {
        Assert.Throws<ArgumentException>(() => new MouseCursorManager(MouseCursor.Defer));

        SystemChannels.MouseCursor.SetPlatformMethodCallHandler(null);
        var manager = new MouseCursorManager(SystemMouseCursors.Basic);
        manager.HandleDeviceCursorUpdate(4, null, [SystemMouseCursors.Click]);
        Assert.Equal(SystemMouseCursors.Click, manager.DebugDeviceActiveCursor(4));
    }

    [Fact]
    public void SystemMouseCursor_EqualityAndDebugDescriptionFollowTheKind()
    {
        Assert.Equal(new SystemMouseCursor("click"), SystemMouseCursors.Click);
        Assert.Equal(SystemMouseCursors.Click.GetHashCode(), new SystemMouseCursor("click").GetHashCode());
        Assert.NotEqual<MouseCursor>(SystemMouseCursors.Click, SystemMouseCursors.Basic);
        Assert.Equal("SystemMouseCursor(click)", SystemMouseCursors.Click.DebugDescription);
        Assert.Equal("SystemMouseCursor(click)", SystemMouseCursors.Click.ToString());
        Assert.Equal("defer", MouseCursor.Defer.ToString());
        Assert.Equal("uncontrolled", MouseCursor.Uncontrolled.ToString());
        Assert.NotEqual(MouseCursor.Defer, MouseCursor.Uncontrolled);
        Assert.Throws<NotSupportedException>(() => MouseCursor.Defer.CreateSession(1));
    }

    [Fact]
    public void SystemMouseCursors_CarryFlutterKindStrings()
    {
        Assert.Equal("none", SystemMouseCursors.None.Kind);
        Assert.Equal("contextMenu", SystemMouseCursors.ContextMenu.Kind);
        Assert.Equal("verticalText", SystemMouseCursors.VerticalText.Kind);
        Assert.Equal("noDrop", SystemMouseCursors.NoDrop.Kind);
        Assert.Equal("allScroll", SystemMouseCursors.AllScroll.Kind);
        Assert.Equal("resizeUpLeftDownRight", SystemMouseCursors.ResizeUpLeftDownRight.Kind);
        Assert.Equal("resizeRow", SystemMouseCursors.ResizeRow.Kind);
        Assert.Equal("zoomOut", SystemMouseCursors.ZoomOut.Kind);
    }

    [DebugOnlyFact]
    public void MouseTrackerAnnotation_ToStringMatchesFlutter()
    {
        var withBoth = new MouseTrackerAnnotation(onEnter: _ => { }, onExit: _ => { });
        var empty = new MouseTrackerAnnotation();
        var withCursor = new MouseTrackerAnnotation(onEnter: _ => { }, cursor: SystemMouseCursors.Grab);

        Assert.Contains("callbacks: [enter, exit]", withBoth.ToString());
        Assert.Contains("callbacks: <none>", empty.ToString());
        Assert.Contains("callbacks: [enter]", withCursor.ToString());
        Assert.Contains("cursor: SystemMouseCursor(grab)", withCursor.ToString());
        // The default cursor is omitted.
        Assert.DoesNotContain("cursor:", withBoth.ToString());
        Assert.Equal(MouseCursor.Defer, empty.Cursor);
        Assert.True(empty.ValidForMouseTracker);
    }

    private static Dictionary<string, object?> Call(int device, string kind) => new()
    {
        ["method"] = "activateSystemCursor",
        ["device"] = device,
        ["kind"] = kind,
    };

    private MouseTrackingHarness Build(Widget widget, int viewId = 0) => new(widget, viewId: viewId);

    private Widget Region(string name, double size = 50, MouseCursor? cursor = null) => new MouseRegion(
        onEnter: _ => _log.Add($"enter:{name}"),
        onHover: _ => _log.Add($"hover:{name}"),
        onExit: _ => _log.Add($"exit:{name}"),
        cursor: cursor,
        child: new SizedBox(width: size, height: size));

    private static Widget Padding(double inset, Widget child) => new Align(
        alignment: Alignment.TopLeft,
        child: new Plumix.Widgets.Padding(insets: new Thickness(inset, inset, 0, 0), child: child));

    private static T? FindDescendant<T>(RenderObject? root)
        where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }
}
