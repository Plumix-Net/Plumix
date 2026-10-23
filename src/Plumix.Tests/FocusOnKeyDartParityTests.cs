// Dart parity source: flutter/packages/flutter/test/widgets/focus_manager_test.dart, focus_scope_test.dart,
// raw_keyboard_listener_test.dart (onKey/RawKeyboardListener tests)

using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Every Dart test that exercises the deprecated raw-key focus callbacks (`onKey`) or
// `RawKeyboardListener`. Also ports focus_traversal_test.dart's `RawKeyboardListener` group.
//
// Dart's `tester.sendKeyEvent(key, platform:)` is `simulateKeyDownEvent` + `simulateKeyUpEvent`
// (controller.dart); `FrameworkDartTester.SendKeyEvent` routes straight into the focus manager and
// so never builds a raw event, which is why these tests go through `KeyEventSimulator` instead (see
// `SendKeyEvent` below). `tester.idle()` is a microtask flush. Tests with
// `variant: KeySimulatorTransitModeVariant.all()` are theories whose body runs inside
// `KeySimulatorTransitModeVariant.Run`; the rest run in the default transit mode and get
// flutter_test's per-test keyboard reset from the constructor/Dispose.
//
// No test here is web-only (`skip: !isBrowser`); 'Fuchsia key event' is skipped only *on* the web
// (`skip: isBrowser`), so it runs. None of raw_keyboard_listener_test.dart was already ported 1:1 in
// KeyboardListenerTests.cs (that file keeps C#-only RawKeyboardListener regression tests).

namespace Plumix.Tests;

#pragma warning disable CS0618 // onKey, RawKeyEvent and RawKeyboardListener are deprecated in Flutter.
public sealed class FocusOnKeyDartParityTests : IDisposable
{
    public FocusOnKeyDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<KeyDataTransitMode> AllVariant => KeySimulatorTransitModeVariant.All;

    // controller.dart's `WidgetController.sendKeyEvent`.
    private static bool SendKeyEvent(LogicalKeyboardKey key, string? platform = null)
    {
        bool handled = KeyEventSimulator.SimulateKeyDownEvent(key, platform: platform);
        KeyEventSimulator.SimulateKeyUpEvent(key, platform: platform);
        EventLoop.FlushMicrotasks();
        return handled;
    }

    // focus_manager_test.dart's `setupWidget`.
    private static BuildContext SetupWidget(FrameworkDartTester tester)
    {
        GlobalKey widgetKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: widgetKey));
        return widgetKey.CurrentContext!;
    }

    // ---------------------------------------------------------------------------------------------
    // focus_manager_test.dart
    // ---------------------------------------------------------------------------------------------

    // 'onKeyEvent and onKey correctly cooperate'
    [Theory]
    [MemberData(nameof(AllVariant))]
    public void OnKeyEventAndOnKeyCorrectlyCooperate(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode1 = new FocusNode(debugLabel: "Test Node 1");
            var focusNode2 = new FocusNode(debugLabel: "Test Node 2");
            var focusNode3 = new FocusNode(debugLabel: "Test Node 3");
            KeyEventResult[][] results =
            [
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
            ];
            var logs = new List<int>();

            tester.PumpWidget(new Focus(
                focusNode: focusNode1,
                onKeyEvent: (_, _) =>
                {
                    logs.Add(0);
                    return results[0][0];
                },
                onKey: (_, _) =>
                {
                    logs.Add(1);
                    return results[0][1];
                },
                child: new Focus(
                    focusNode: focusNode2,
                    onKeyEvent: (_, _) =>
                    {
                        logs.Add(10);
                        return results[1][0];
                    },
                    onKey: (_, _) =>
                    {
                        logs.Add(11);
                        return results[1][1];
                    },
                    child: new Focus(
                        focusNode: focusNode3,
                        onKeyEvent: (_, _) =>
                        {
                            logs.Add(20);
                            return results[2][0];
                        },
                        onKey: (_, _) =>
                        {
                            logs.Add(21);
                            return results[2][1];
                        },
                        child: new SizedBox(width: 200, height: 100)))));
            focusNode3.RequestFocus();
            tester.Pump();

            // All ignored.
            results =
            [
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
            ];
            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([20, 21, 10, 11, 0, 1], logs);
            logs.Clear();

            // The onKeyEvent should be able to stop propagation.
            results =
            [
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Handled, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
            ];
            Assert.True(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([20, 21, 10, 11], logs);
            logs.Clear();

            // The onKey should be able to stop propagation.
            results =
            [
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Handled],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
            ];
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([20, 21, 10, 11], logs);
            logs.Clear();

            // KeyEventResult.skipRemainingHandlers works.
            results =
            [
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
                [KeyEventResult.SkipRemainingHandlers, KeyEventResult.Ignored],
                [KeyEventResult.Ignored, KeyEventResult.Ignored],
            ];
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([20, 21, 10, 11], logs);
            logs.Clear();

            focusNode1.Dispose();
            focusNode2.Dispose();
            focusNode3.Dispose();
        });

    // 'Key handling bubbles up and terminates when handled.'
    [Theory]
    [MemberData(nameof(AllVariant))]
    public void KeyHandlingBubblesUpAndTerminatesWhenHandled(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var receivedAnEvent = new HashSet<FocusNode>();
            var shouldHandle = new HashSet<FocusNode>();
            KeyEventResult HandleEvent(FocusNode node, RawKeyEvent @event)
            {
                if (shouldHandle.Contains(node))
                {
                    receivedAnEvent.Add(node);
                    return KeyEventResult.Handled;
                }

                return KeyEventResult.Ignored;
            }

            void SendEvent()
            {
                receivedAnEvent.Clear();
                SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "fuchsia");
            }

            BuildContext context = SetupWidget(tester);
            var scope1 = new FocusScopeNode(debugLabel: "Scope 1");
            FocusAttachment scope1Attachment = scope1.Attach(context, onKey: HandleEvent);
            var scope2 = new FocusScopeNode(debugLabel: "Scope 2");
            FocusAttachment scope2Attachment = scope2.Attach(context, onKey: HandleEvent);
            var parent1 = new FocusNode(debugLabel: "Parent 1", onKey: HandleEvent);
            FocusAttachment parent1Attachment = parent1.Attach(context);
            var parent2 = new FocusNode(debugLabel: "Parent 2", onKey: HandleEvent);
            FocusAttachment parent2Attachment = parent2.Attach(context);
            var child1 = new FocusNode(debugLabel: "Child 1");
            FocusAttachment child1Attachment = child1.Attach(context, onKey: HandleEvent);
            var child2 = new FocusNode(debugLabel: "Child 2");
            FocusAttachment child2Attachment = child2.Attach(context, onKey: HandleEvent);
            var child3 = new FocusNode(debugLabel: "Child 3");
            FocusAttachment child3Attachment = child3.Attach(context, onKey: HandleEvent);
            var child4 = new FocusNode(debugLabel: "Child 4");
            FocusAttachment child4Attachment = child4.Attach(context, onKey: HandleEvent);
            scope1Attachment.Reparent(parent: FocusManager.Instance.RootScope);
            scope2Attachment.Reparent(parent: FocusManager.Instance.RootScope);
            parent1Attachment.Reparent(parent: scope1);
            parent2Attachment.Reparent(parent: scope2);
            child1Attachment.Reparent(parent: parent1);
            child2Attachment.Reparent(parent: parent1);
            child3Attachment.Reparent(parent: parent2);
            child4Attachment.Reparent(parent: parent2);
            child4.RequestFocus();
            tester.Pump();
            shouldHandle.UnionWith([scope2, parent2, child2, child4]);
            SendEvent();
            Assert.Equal(new HashSet<FocusNode> { child4 }, receivedAnEvent);
            shouldHandle.Remove(child4);
            SendEvent();
            Assert.Equal(new HashSet<FocusNode> { parent2 }, receivedAnEvent);
            shouldHandle.Remove(parent2);
            SendEvent();
            Assert.Equal(new HashSet<FocusNode> { scope2 }, receivedAnEvent);
            shouldHandle.Clear();
            SendEvent();
            Assert.Empty(receivedAnEvent);
            child1.RequestFocus();
            tester.Pump();
            shouldHandle.UnionWith([scope2, parent2, child2, child4]);
            SendEvent();
            // Since none of the focused nodes handle this event, nothing should
            // receive it.
            Assert.Empty(receivedAnEvent);

            foreach (FocusNode node in new FocusNode[]
                     {
                         scope1, scope2, parent1, parent2, child1, child2, child3, child4,
                     })
            {
                node.Dispose();
            }
        });

    // 'FocusManager.addEarlyKeyEventHandler works'
    [Theory]
    [MemberData(nameof(AllVariant))]
    public void FocusManagerAddEarlyKeyEventHandlerWorks(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode1 = new FocusNode(debugLabel: "Test Node 1");
            var logs = new List<int>();
            KeyEventResult earlyResult = KeyEventResult.Ignored;
            KeyEventResult focusResult = KeyEventResult.Ignored;

            tester.PumpWidget(new Focus(
                focusNode: focusNode1,
                onKeyEvent: (_, @event) =>
                {
                    logs.Add(0);
                    if (@event is KeyDownEvent)
                    {
                        return focusResult;
                    }

                    return KeyEventResult.Ignored;
                },
                onKey: (_, @event) =>
                {
                    logs.Add(1);
                    // Dart's `event is KeyDownEvent` on a RawKeyEvent, which is never true.
                    if ((object)@event is KeyDownEvent)
                    {
                        return focusResult;
                    }

                    return KeyEventResult.Ignored;
                },
                child: new SizedBox()));
            focusNode1.RequestFocus();
            tester.Pump();

            OnKeyEventCallback earlyHandler = @event =>
            {
                if (@event is KeyDownEvent)
                {
                    return earlyResult;
                }

                return KeyEventResult.Ignored;
            };

            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            FocusManager.Instance.AddEarlyKeyEventHandler(earlyHandler);
            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            earlyResult = KeyEventResult.Handled;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            earlyResult = KeyEventResult.Ignored;
            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Handled;
            earlyResult = KeyEventResult.Ignored;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            FocusManager.Instance.RemoveEarlyKeyEventHandler(earlyHandler);
            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            earlyResult = KeyEventResult.Handled;
            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Handled;
            earlyResult = KeyEventResult.Ignored;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            focusNode1.Dispose();
        });

    // 'FocusManager.addLateKeyEventHandler works'
    [Theory]
    [MemberData(nameof(AllVariant))]
    public void FocusManagerAddLateKeyEventHandlerWorks(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode1 = new FocusNode(debugLabel: "Test Node 1");
            var logs = new List<int>();
            KeyEventResult lateResult = KeyEventResult.Ignored;
            KeyEventResult focusResult = KeyEventResult.Ignored;

            tester.PumpWidget(new Focus(
                focusNode: focusNode1,
                onKeyEvent: (_, @event) =>
                {
                    logs.Add(0);
                    if (@event is KeyDownEvent)
                    {
                        return focusResult;
                    }

                    return KeyEventResult.Ignored;
                },
                onKey: (_, @event) =>
                {
                    logs.Add(1);
                    // Dart's `event is KeyDownEvent` on a RawKeyEvent, which is never true.
                    if ((object)@event is KeyDownEvent)
                    {
                        return focusResult;
                    }

                    return KeyEventResult.Ignored;
                },
                child: new SizedBox()));
            focusNode1.RequestFocus();
            tester.Pump();

            OnKeyEventCallback lateHandler = @event =>
            {
                if (@event is KeyDownEvent)
                {
                    return lateResult;
                }

                return KeyEventResult.Ignored;
            };

            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            FocusManager.Instance.AddLateKeyEventHandler(lateHandler);
            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            lateResult = KeyEventResult.Handled;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            lateResult = KeyEventResult.Ignored;
            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Handled;
            lateResult = KeyEventResult.Ignored;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            FocusManager.Instance.RemoveLateKeyEventHandler(lateHandler);
            logs.Clear();
            focusResult = KeyEventResult.Ignored;
            lateResult = KeyEventResult.Handled;
            Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            logs.Clear();
            focusResult = KeyEventResult.Handled;
            lateResult = KeyEventResult.Ignored;
            Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Digit1));
            Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Digit1));
            Assert.Equal([0, 1, 0, 1], logs);

            focusNode1.Dispose();
        });

    // ---------------------------------------------------------------------------------------------
    // focus_scope_test.dart
    // ---------------------------------------------------------------------------------------------

    // "FocusScope doesn't update the focusNode attributes when the widget updates if
    // withExternalFocusNode is used"
    [Fact]
    public void FocusScopeDoesNotUpdateTheFocusNodeAttributesWhenTheWidgetUpdatesWithExternalFocusNode()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key1 = new LabeledGlobalKey<State>("1");
        var focusScopeNode = new FocusScopeNode();
        bool? keyEventHandled = null;
        FocusOnKeyCallback handleCallback = (_, _) =>
        {
            keyEventHandled = true;
            return KeyEventResult.Handled;
        };
        FocusOnKeyEventCallback handleEventCallback = (_, _) =>
        {
            keyEventHandled = true;
            return KeyEventResult.Handled;
        };
        FocusOnKeyCallback ignoreCallback = (_, _) => KeyEventResult.Ignored;
        FocusOnKeyEventCallback ignoreEventCallback = (_, _) => KeyEventResult.Ignored;
        focusScopeNode.OnKey = ignoreCallback;
        focusScopeNode.OnKeyEvent = ignoreEventCallback;
        focusScopeNode.DescendantsAreFocusable = false;
        focusScopeNode.DescendantsAreTraversable = false;
        focusScopeNode.SkipTraversal = false;
        focusScopeNode.CanRequestFocus = true;
        FocusScope focusScopeWidget = FocusScope.WithExternalFocusNode(
            focusScopeNode: focusScopeNode,
            child: new Container(key: key1));
        tester.PumpWidget(focusScopeWidget);
        Assert.Equal(ignoreCallback, focusScopeNode.OnKey);
        Assert.Equal(ignoreEventCallback, focusScopeNode.OnKeyEvent);
        Assert.False(focusScopeNode.DescendantsAreFocusable);
        Assert.False(focusScopeNode.DescendantsAreTraversable);
        Assert.False(focusScopeNode.SkipTraversal);
        Assert.True(focusScopeNode.CanRequestFocus);
        Assert.Equal(focusScopeNode.OnKey, focusScopeWidget.OnKey);
        Assert.Equal(focusScopeNode.OnKeyEvent, focusScopeWidget.OnKeyEvent);
        Assert.Equal(focusScopeNode.DescendantsAreFocusable, focusScopeWidget.DescendantsAreFocusable);
        Assert.Equal(focusScopeNode.DescendantsAreTraversable, focusScopeWidget.DescendantsAreTraversable);
        Assert.Equal(focusScopeNode.SkipTraversal, focusScopeWidget.SkipTraversal);
        Assert.Equal(focusScopeNode.CanRequestFocus, focusScopeWidget.CanRequestFocus);

        FocusScope.Of(key1.CurrentContext!).RequestFocus();
        tester.Pump();
        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.Null(keyEventHandled);

        focusScopeNode.OnKey = handleCallback;
        focusScopeNode.OnKeyEvent = handleEventCallback;
        focusScopeNode.DescendantsAreFocusable = true;
        focusScopeNode.DescendantsAreTraversable = true;
        focusScopeWidget = FocusScope.WithExternalFocusNode(
            focusScopeNode: focusScopeNode,
            child: new Container(key: key1));
        tester.PumpWidget(focusScopeWidget);
        Assert.Equal(handleCallback, focusScopeNode.OnKey);
        Assert.Equal(handleEventCallback, focusScopeNode.OnKeyEvent);
        Assert.True(focusScopeNode.DescendantsAreFocusable);
        Assert.True(focusScopeNode.DescendantsAreTraversable);
        Assert.False(focusScopeNode.SkipTraversal);
        Assert.True(focusScopeNode.CanRequestFocus);
        Assert.Equal(focusScopeNode.OnKey, focusScopeWidget.OnKey);
        Assert.Equal(focusScopeNode.OnKeyEvent, focusScopeWidget.OnKeyEvent);
        Assert.Equal(focusScopeNode.DescendantsAreFocusable, focusScopeWidget.DescendantsAreFocusable);
        Assert.Equal(focusScopeNode.DescendantsAreTraversable, focusScopeWidget.DescendantsAreTraversable);
        Assert.Equal(focusScopeNode.SkipTraversal, focusScopeWidget.SkipTraversal);
        Assert.Equal(focusScopeNode.CanRequestFocus, focusScopeWidget.CanRequestFocus);

        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.True(keyEventHandled);

        focusScopeNode.Dispose();
    }

    // 'Focus updates the onKey handler when the widget updates'
    [Fact]
    public void FocusUpdatesTheOnKeyHandlerWhenTheWidgetUpdates()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key1 = new LabeledGlobalKey<State>("1");
        var focusNode = new FocusNode();
        bool? keyEventHandled = null;
        FocusOnKeyCallback handleCallback = (_, _) =>
        {
            keyEventHandled = true;
            return KeyEventResult.Handled;
        };
        FocusOnKeyCallback ignoreCallback = (_, _) => KeyEventResult.Ignored;
        var focusWidget = new Focus(
            onKey: ignoreCallback, // This one does nothing.
            focusNode: focusNode,
            skipTraversal: true,
            canRequestFocus: true,
            child: new Container(key: key1));
        focusNode.OnKeyEvent = null;
        tester.PumpWidget(focusWidget);
        Assert.Equal(ignoreCallback, focusNode.OnKey);
        Assert.Equal(focusNode.OnKey, focusWidget.OnKey);
        Assert.Equal(focusNode.OnKeyEvent, focusWidget.OnKeyEvent);
        Assert.Equal(focusNode.DescendantsAreFocusable, focusWidget.DescendantsAreFocusable);
        Assert.Equal(focusNode.SkipTraversal, focusWidget.SkipTraversal);
        Assert.Equal(focusNode.CanRequestFocus, focusWidget.CanRequestFocus);

        Focus.Of(key1.CurrentContext!).RequestFocus();
        tester.Pump();
        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.Null(keyEventHandled);

        focusWidget = new Focus(
            onKey: handleCallback,
            focusNode: focusNode,
            skipTraversal: true,
            canRequestFocus: true,
            child: new Container(key: key1));
        tester.PumpWidget(focusWidget);
        Assert.Equal(handleCallback, focusNode.OnKey);
        Assert.Equal(focusNode.OnKey, focusWidget.OnKey);
        Assert.Equal(focusNode.OnKeyEvent, focusWidget.OnKeyEvent);
        Assert.Equal(focusNode.DescendantsAreFocusable, focusWidget.DescendantsAreFocusable);
        Assert.Equal(focusNode.SkipTraversal, focusWidget.SkipTraversal);
        Assert.Equal(focusNode.CanRequestFocus, focusWidget.CanRequestFocus);

        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.True(keyEventHandled);

        focusNode.Dispose();
    }

    // "Focus doesn't update the focusNode attributes when the widget updates if withExternalFocusNode
    // is used"
    [Fact]
    public void FocusDoesNotUpdateTheFocusNodeAttributesWhenTheWidgetUpdatesWithExternalFocusNode()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key1 = new LabeledGlobalKey<State>("1");
        var focusNode = new FocusNode();
        bool? keyEventHandled = null;
        FocusOnKeyCallback handleCallback = (_, _) =>
        {
            keyEventHandled = true;
            return KeyEventResult.Handled;
        };
        FocusOnKeyEventCallback handleEventCallback = (_, _) =>
        {
            keyEventHandled = true;
            return KeyEventResult.Handled;
        };
        FocusOnKeyCallback ignoreCallback = (_, _) => KeyEventResult.Ignored;
        FocusOnKeyEventCallback ignoreEventCallback = (_, _) => KeyEventResult.Ignored;
        focusNode.OnKey = ignoreCallback;
        focusNode.OnKeyEvent = ignoreEventCallback;
        focusNode.DescendantsAreFocusable = false;
        focusNode.DescendantsAreTraversable = false;
        focusNode.SkipTraversal = false;
        focusNode.CanRequestFocus = true;
        Focus focusWidget = Focus.WithExternalFocusNode(focusNode: focusNode, child: new Container(key: key1));
        tester.PumpWidget(focusWidget);
        Assert.Equal(ignoreCallback, focusNode.OnKey);
        Assert.Equal(ignoreEventCallback, focusNode.OnKeyEvent);
        Assert.False(focusNode.DescendantsAreFocusable);
        Assert.False(focusNode.DescendantsAreTraversable);
        Assert.False(focusNode.SkipTraversal);
        Assert.True(focusNode.CanRequestFocus);
        Assert.Equal(focusNode.OnKey, focusWidget.OnKey);
        Assert.Equal(focusNode.OnKeyEvent, focusWidget.OnKeyEvent);
        Assert.Equal(focusNode.DescendantsAreFocusable, focusWidget.DescendantsAreFocusable);
        Assert.Equal(focusNode.DescendantsAreTraversable, focusWidget.DescendantsAreTraversable);
        Assert.Equal(focusNode.SkipTraversal, focusWidget.SkipTraversal);
        Assert.Equal(focusNode.CanRequestFocus, focusWidget.CanRequestFocus);

        Focus.Of(key1.CurrentContext!).RequestFocus();
        tester.Pump();
        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.Null(keyEventHandled);

        focusNode.OnKey = handleCallback;
        focusNode.OnKeyEvent = handleEventCallback;
        focusNode.DescendantsAreFocusable = true;
        focusNode.DescendantsAreTraversable = true;
        focusWidget = Focus.WithExternalFocusNode(focusNode: focusNode, child: new Container(key: key1));
        tester.PumpWidget(focusWidget);
        Assert.Equal(handleCallback, focusNode.OnKey);
        Assert.Equal(handleEventCallback, focusNode.OnKeyEvent);
        Assert.True(focusNode.DescendantsAreFocusable);
        Assert.True(focusNode.DescendantsAreTraversable);
        Assert.False(focusNode.SkipTraversal);
        Assert.True(focusNode.CanRequestFocus);
        Assert.Equal(focusNode.OnKey, focusWidget.OnKey);
        Assert.Equal(focusNode.OnKeyEvent, focusWidget.OnKeyEvent);
        Assert.Equal(focusNode.DescendantsAreFocusable, focusWidget.DescendantsAreFocusable);
        Assert.Equal(focusNode.DescendantsAreTraversable, focusWidget.DescendantsAreTraversable);
        Assert.Equal(focusNode.SkipTraversal, focusWidget.SkipTraversal);
        Assert.Equal(focusNode.CanRequestFocus, focusWidget.CanRequestFocus);

        SendKeyEvent(LogicalKeyboardKey.Enter);
        Assert.True(keyEventHandled);

        focusNode.Dispose();
    }

    // ---------------------------------------------------------------------------------------------
    // raw_keyboard_listener_test.dart
    // ---------------------------------------------------------------------------------------------

    // 'Can dispose without keyboard'
    [Fact]
    public void CanDisposeWithoutKeyboard()
    {
        using var tester = new FrameworkDartTester();
        var focusNode = new FocusNode();
        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, child: new Container()));
        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, child: new Container()));
        tester.PumpWidget(new Container());
        focusNode.Dispose();
    }

    // 'Fuchsia key event' (`skip: isBrowser`: it runs everywhere but the web)
    [Fact]
    public void FuchsiaKeyEvent()
    {
        using var tester = new FrameworkDartTester();
        var events = new List<RawKeyEvent>();
        var focusNode = new FocusNode();

        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, onKey: events.Add, child: new Container()));

        focusNode.RequestFocus();
        EventLoop.FlushMicrotasks();

        SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "fuchsia");
        EventLoop.FlushMicrotasks();

        Assert.Equal(2, events.Count);
        Assert.Equal(typeof(RawKeyDownEvent), events[0].GetType());
        Assert.Equal(typeof(RawKeyEventDataFuchsia), events[0].Data.GetType());
        var typedData = (RawKeyEventDataFuchsia)events[0].Data;
        Assert.Equal(0x700e3, typedData.HidUsage);
        Assert.Equal(0x0, typedData.CodePoint);
        Assert.Equal(RawKeyEventDataFuchsia.ModifierLeftMeta, typedData.Modifiers);
        Assert.True(typedData.IsModifierPressed(ModifierKey.MetaModifier, side: KeyboardSide.Left));

        tester.PumpWidget(new Container());
        focusNode.Dispose();
    }

    // 'Web key event'
    [Fact]
    public void WebKeyEvent()
    {
        using var tester = new FrameworkDartTester();
        var events = new List<RawKeyEvent>();
        var focusNode = new FocusNode();

        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, onKey: events.Add, child: new Container()));

        focusNode.RequestFocus();
        EventLoop.FlushMicrotasks();

        SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "web");
        EventLoop.FlushMicrotasks();

        Assert.Equal(2, events.Count);
        Assert.Equal(typeof(RawKeyDownEvent), events[0].GetType());
        Assert.IsAssignableFrom<RawKeyEventDataWeb>(events[0].Data);
        var typedData = (RawKeyEventDataWeb)events[0].Data;
        Assert.Equal("MetaLeft", typedData.Code);
        Assert.Equal(RawKeyEventDataWeb.ModifierMeta, typedData.MetaState);
        Assert.True(typedData.IsModifierPressed(ModifierKey.MetaModifier, side: KeyboardSide.Left));

        tester.PumpWidget(new Container());
        focusNode.Dispose();
    }

    // 'Defunct listeners do not receive events'
    [Fact]
    public void DefunctListenersDoNotReceiveEvents()
    {
        using var tester = new FrameworkDartTester();
        var events = new List<RawKeyEvent>();
        var focusNode = new FocusNode();

        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, onKey: events.Add, child: new Container()));

        focusNode.RequestFocus();
        EventLoop.FlushMicrotasks();

        SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "fuchsia");
        EventLoop.FlushMicrotasks();

        Assert.Equal(2, events.Count);
        events.Clear();

        tester.PumpWidget(new Container());

        SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "fuchsia");

        EventLoop.FlushMicrotasks();

        Assert.Empty(events);

        tester.PumpWidget(new Container());
        focusNode.Dispose();
    }

    // ---------------------------------------------------------------------------------------------
    // focus_traversal_test.dart, group(RawKeyboardListener)
    // ---------------------------------------------------------------------------------------------

    // 'Raw keyboard listener introduces a Semantics node by default'
    [Fact]
    public void RawKeyboardListenerIntroducesASemanticsNodeByDefault()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        var focusNode = new FocusNode();

        tester.PumpWidget(new RawKeyboardListener(focusNode: focusNode, child: new Container()));
        // hasSemantics(TestSemantics.root(children: [TestSemantics.rootChild(flags: [isFocusable],
        // actions: [focus])]), ignoreId: true, ignoreRect: true, ignoreTransform: true)
        ExpectRootChildren(
            semantics,
            child =>
            {
                SemanticsData data = child.GetSemanticsData();
                Assert.Equal(SemanticsFlags.IsFocusable, data.Flags);
                Assert.Equal(SemanticsActions.Focus, data.Actions);
                Assert.Empty(child.Children);
            });
        focusNode.Dispose();
    }

    // "Raw keyboard listener doesn't introduce a Semantics node when specified"
    [Fact]
    public void RawKeyboardListenerDoesNotIntroduceASemanticsNodeWhenSpecified()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        var focusNode = new FocusNode();

        tester.PumpWidget(new RawKeyboardListener(
            focusNode: focusNode,
            includeSemantics: false,
            child: new Container()));
        // hasSemantics(TestSemantics.root())
        ExpectRootChildren(semantics);
        focusNode.Dispose();
    }

    public static TheoryData<TargetPlatform> AllPlatforms =>
    [
        TargetPlatform.Android,
        TargetPlatform.Fuchsia,
        TargetPlatform.IOS,
        TargetPlatform.Linux,
        TargetPlatform.MacOS,
        TargetPlatform.Windows,
    ];

    public static TheoryData<TargetPlatform> MobilePlatforms =>
        [TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.IOS];

    public static TheoryData<TargetPlatform> DesktopPlatforms =>
        [TargetPlatform.Linux, TargetPlatform.MacOS, TargetPlatform.Windows];

    // flutter_test's TargetPlatformVariant: the platform override for one test run.
    private static void WithTargetPlatform(TargetPlatform platform, Action body)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            body();
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    // "Initial highlight mode guesses correctly."
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void InitialHighlightModeGuessesCorrectly(TargetPlatform platform)
    {
        WithTargetPlatform(platform, () =>
        {
            using var tester = new FrameworkDartTester();
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.Automatic;
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.Fuchsia:
                case TargetPlatform.Android:
                case TargetPlatform.IOS:
                    Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
                    break;
                case TargetPlatform.Linux:
                case TargetPlatform.MacOS:
                case TargetPlatform.Windows:
                    Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
                    break;
            }
        });
    }

    // "Mouse events change initial focus highlight mode on mobile."
    [Theory]
    [MemberData(nameof(MobilePlatforms))]
    public void MouseEventsChangeInitialFocusHighlightModeOnMobile(TargetPlatform platform)
    {
        WithTargetPlatform(platform, () =>
        {
            using var tester = new FrameworkDartTester();
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            // Clear out the mouse state.
            RendererBinding.Instance.InitMouseTracker();
            TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
            gesture.MoveTo(default);
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
            gesture.RemovePointer();
        });
    }

    // "Mouse events change initial focus highlight mode on desktop."
    [Theory]
    [MemberData(nameof(DesktopPlatforms))]
    public void MouseEventsChangeInitialFocusHighlightModeOnDesktop(TargetPlatform platform)
    {
        WithTargetPlatform(platform, () =>
        {
            using var tester = new FrameworkDartTester();
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
            // Clear out the mouse state.
            RendererBinding.Instance.InitMouseTracker();
            TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
            gesture.MoveTo(default);
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
            gesture.RemovePointer();
        });
    }

    // "Keyboard events change initial focus highlight mode."
    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void KeyboardEventsChangeInitialFocusHighlightMode(TargetPlatform platform)
    {
        WithTargetPlatform(platform, () =>
        {
            using var tester = new FrameworkDartTester();
            tester.SendKeyEvent(LogicalKeyboardKey.Enter);
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
        });
    }

    // "Soft keyboard key events do not change focus highlight mode on Android."
    [Fact]
    public void SoftKeyboardKeyEventsDoNotChangeFocusHighlightModeOnAndroid()
    {
        WithTargetPlatform(TargetPlatform.Android, () =>
        {
            using var tester = new FrameworkDartTester();
            SetupWidget(tester);

            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.Automatic;
            tester.Tap(Find.ByType<Container>());
            tester.Pump();

            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);

            // Simulates a soft keyboard key event.
            const int kAndroidSoftKeyboardFlag = 0x00000002;
            const int kAndroidVirtualKeyboardDeviceId = -1;
            Dictionary<string, object?> data = KeyEventSimulator.GetKeyData(
                LogicalKeyboardKey.Backspace,
                platform: "android");
            data["flags"] = kAndroidSoftKeyboardFlag;
            data["deviceId"] = kAndroidVirtualKeyboardDeviceId;
            data["source"] = 0x00000101;
            data["repeatCount"] = 0;

            Task delivery = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
                SystemChannels.KeyEvent.Name,
                SystemChannels.KeyEvent.Codec.EncodeMessage(data),
                _ => { });
            EventLoop.FlushMicrotasks();
            Assert.True(delivery.IsCompleted);

            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            HardwareKeyboard.Instance.ClearState();
            RawKeyboard.Instance.ClearKeysPressed();
        });
    }

    // "Events change focus highlight mode."
    [Fact]
    public void EventsChangeFocusHighlightMode()
    {
        using var tester = new FrameworkDartTester();
        SetupWidget(tester);
        int callCount = 0;
        FocusHighlightMode? lastMode = null;
        void HandleModeChange(FocusHighlightMode mode)
        {
            lastMode = mode;
            callCount++;
        }

        FocusManager.Instance.AddHighlightModeListener(HandleModeChange);
        try
        {
            Assert.Equal(0, callCount);
            Assert.Null(lastMode);
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.Automatic;
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            SendKeyEvent(LogicalKeyboardKey.MetaLeft, platform: "fuchsia");
            Assert.Equal(1, callCount);
            Assert.Equal(FocusHighlightMode.Traditional, lastMode);
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
            tester.Tap(Find.ByType<Container>());
            Assert.Equal(2, callCount);
            Assert.Equal(FocusHighlightMode.Touch, lastMode);
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            TestGesture gesture = tester.StartGesture(default, kind: PointerDeviceKind.Mouse);
            gesture.Up();
            Assert.Equal(2, callCount);
            Assert.Equal(FocusHighlightMode.Touch, lastMode);
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            tester.Tap(Find.ByType<Container>());
            Assert.Equal(2, callCount);
            Assert.Equal(FocusHighlightMode.Touch, lastMode);
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
            Assert.Equal(3, callCount);
            Assert.Equal(FocusHighlightMode.Traditional, lastMode);
            Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
            FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
            Assert.Equal(4, callCount);
            Assert.Equal(FocusHighlightMode.Touch, lastMode);
            Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
        }
        finally
        {
            FocusManager.Instance.RemoveHighlightModeListener(HandleModeChange);
        }
    }

    // The part of `hasSemantics` these two tests use: the root node, with no flags/actions of its own,
    // has exactly the given children.
    private static void ExpectRootChildren(SemanticsTester semantics, params Action<SemanticsNode>[] children)
    {
        SemanticsNode root = semantics.RootNode;
        SemanticsData rootData = root.GetSemanticsData();
        Assert.Equal(default, rootData.Flags);
        Assert.Equal(default, rootData.Actions);
        Assert.Collection(root.Children, children);
    }
}
#pragma warning restore CS0618
