// Dart parity source: flutter/packages/flutter/test/services/hardware_keyboard_test.dart

using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Each Dart `testWidgets(..., variant: KeySimulatorTransitModeVariant.x())` is a theory over the
// variant's values whose body runs inside `KeySimulatorTransitModeVariant.Run`. Tests without a
// variant get flutter_test's per-test keyboard reset from the class constructor/Dispose.
// `tester.binding.keyboard`/`keyEventManager` are `ServicesBinding.Instance.Keyboard`/`KeyEventManager`.
// HardwareKeyboardTests.cs keeps the C#-only unit tests that drive `HardwareKeyboard.HandleKeyEvent`
// directly; every Dart test of this file is ported here through the simulated platform messages.

namespace Plumix.Tests;

#pragma warning disable CS0618 // The transit modes and key messages are deprecated in Flutter.
public sealed class HardwareKeyboardDartParityTests : IDisposable
{
    public HardwareKeyboardDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        KeyboardDebug.DebugPrintKeyboardEvents = false;
        Print.DebugPrint = Print.DebugPrintThrottled;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<KeyDataTransitMode> KeyDataThenRawKeyDataVariant =>
        KeySimulatorTransitModeVariant.KeyDataThenRawKeyData;

    public static TheoryData<KeyDataTransitMode> RawKeyDataVariant => KeySimulatorTransitModeVariant.RawKeyData;

    public static TheoryData<KeyDataTransitMode> AllVariant => KeySimulatorTransitModeVariant.All;

    private static void ExpectState(IReadOnlyDictionary<PhysicalKeyboardKey, LogicalKeyboardKey> expectedKeys)
    {
        Assert.Equal(
            new HashSet<PhysicalKeyboardKey>(expectedKeys.Keys),
            HardwareKeyboard.Instance.PhysicalKeysPressed);
        Assert.Equal(
            new HashSet<LogicalKeyboardKey>(expectedKeys.Values),
            HardwareKeyboard.Instance.LogicalKeysPressed);
    }

    [Theory]
    [MemberData(nameof(KeyDataThenRawKeyDataVariant))]
    public void HardwareKeyboardRecordsPressedKeysAndEnabledLocks(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            HardwareKeyboard keyboard = HardwareKeyboard.Instance;
            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.NumLock, platform: "windows");
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.NumLock }, keyboard.LogicalKeysPressed);
            Assert.Equal(new HashSet<KeyboardLockMode> { KeyboardLockMode.NumLock }, keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock, PhysicalKeyboardKey.Numpad1 },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(
                new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.NumLock, LogicalKeyboardKey.Numpad1 },
                keyboard.LogicalKeysPressed);
            Assert.Equal(new HashSet<KeyboardLockMode> { KeyboardLockMode.NumLock }, keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyRepeatEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock, PhysicalKeyboardKey.Numpad1 },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(
                new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.NumLock, LogicalKeyboardKey.Numpad1 },
                keyboard.LogicalKeysPressed);
            Assert.Equal(new HashSet<KeyboardLockMode> { KeyboardLockMode.NumLock }, keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.NumLock);
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.Numpad1 },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.Numpad1 }, keyboard.LogicalKeysPressed);
            Assert.Equal(new HashSet<KeyboardLockMode> { KeyboardLockMode.NumLock }, keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.NumLock, platform: "windows");
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock, PhysicalKeyboardKey.Numpad1 },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(
                new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.NumLock, LogicalKeyboardKey.Numpad1 },
                keyboard.LogicalKeysPressed);
            Assert.Empty(keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.Equal(
                new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.NumLock },
                keyboard.PhysicalKeysPressed);
            Assert.Equal(new HashSet<LogicalKeyboardKey> { LogicalKeyboardKey.NumLock }, keyboard.LogicalKeysPressed);
            Assert.Empty(keyboard.LockModesEnabled);

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.NumLock, platform: "windows");
            Assert.Empty(keyboard.PhysicalKeysPressed);
            Assert.Empty(keyboard.LogicalKeysPressed);
            Assert.Empty(keyboard.LockModesEnabled);
        });

    [Theory]
    [MemberData(nameof(KeyDataThenRawKeyDataVariant))]
    public void KeyEventCanTellWhichKeysArePressed(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            tester.PumpWidget(new Focus(autofocus: true, child: new SizedBox()));
            tester.Pump();
            HardwareKeyboard keyboard = HardwareKeyboard.Instance;

            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.NumLock, platform: "windows");

            Assert.True(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.NumLock));
            Assert.True(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.NumLock));

            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.True(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.Numpad1));
            Assert.True(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.Numpad1));

            KeyEventSimulator.SimulateKeyRepeatEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.True(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.Numpad1));
            Assert.True(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.Numpad1));

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.NumLock);
            Assert.True(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.Numpad1));
            Assert.True(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.Numpad1));

            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.NumLock, platform: "windows");
            Assert.True(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.NumLock));
            Assert.True(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.NumLock));

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.Numpad1, platform: "windows");
            Assert.False(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.Numpad1));
            Assert.False(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.Numpad1));

            KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.NumLock, platform: "windows");
            Assert.False(keyboard.IsPhysicalKeyPressed(PhysicalKeyboardKey.NumLock));
            Assert.False(keyboard.IsLogicalKeyPressed(LogicalKeyboardKey.NumLock));
        });

    [Fact]
    public void KeyboardManagerSynthesizesModifierKeysInRawKeyDataMode()
    {
        var events = new List<KeyEvent>();
        HardwareKeyboard.Instance.AddHandler(@event =>
        {
            events.Add(@event);
            return false;
        });
        // While ShiftLeft is held (the event of which was skipped), press keyA.
        Dictionary<string, object?> rawMessage;
        if (Constants.KIsWeb)
        {
            rawMessage = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "web");
            rawMessage["metaState"] = RawKeyEventDataWeb.ModifierShift;
        }
        else
        {
            rawMessage = KeyEventSimulator.GetKeyData(LogicalKeyboardKey.KeyA, platform: "android");
            rawMessage["metaState"] = RawKeyEventDataAndroid.ModifierLeftShift | RawKeyEventDataAndroid.ModifierShift;
        }

        ServicesBinding.Instance.KeyEventManager.HandleRawKeyMessage(rawMessage).GetAwaiter().GetResult();
        Assert.Equal(2, events.Count);
        Assert.Equal(PhysicalKeyboardKey.ShiftLeft, events[0].PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.ShiftLeft, events[0].LogicalKey);
        Assert.True(events[0].Synthesized);
        Assert.Equal(PhysicalKeyboardKey.KeyA, events[1].PhysicalKey);
        Assert.Equal(LogicalKeyboardKey.KeyA, events[1].LogicalKey);
        Assert.False(events[1].Synthesized);
    }

    [Theory]
    [MemberData(nameof(AllVariant))]
    public void DispatchEventsToAllHandlers(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode = new FocusNode();
            var logs = new List<int>();
            try
            {
                tester.PumpWidget(new KeyboardListener(
                    autofocus: true,
                    focusNode: focusNode,
                    child: new Container(),
                    onKeyEvent: _ => logs.Add(1)));

                // Only the Service binding handler.

                Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([1], logs);
                logs.Clear();

                // Add a handler.

                bool handler2Result = false;

                bool Handler2(KeyEvent _)
                {
                    logs.Add(2);
                    return handler2Result;
                }

                HardwareKeyboard.Instance.AddHandler(Handler2);

                Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 1], logs);
                logs.Clear();

                handler2Result = true;

                Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 1], logs);
                logs.Clear();

                // Add another handler.

                handler2Result = false;
                bool handler3Result = false;

                bool Handler3(KeyEvent _)
                {
                    logs.Add(3);
                    return handler3Result;
                }

                HardwareKeyboard.Instance.AddHandler(Handler3);

                Assert.False(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 3, 1], logs);
                logs.Clear();

                handler2Result = true;

                Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 3, 1], logs);
                logs.Clear();

                handler3Result = true;

                Assert.True(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 3, 1], logs);
                logs.Clear();

                // Add handler2 again.

                HardwareKeyboard.Instance.AddHandler(Handler2);

                handler3Result = false;
                handler2Result = false;
                Assert.False(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 3, 2, 1], logs);
                logs.Clear();

                handler2Result = true;
                Assert.True(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([2, 3, 2, 1], logs);
                logs.Clear();

                // Remove handler2 once.

                HardwareKeyboard.Instance.RemoveHandler(Handler2);
                Assert.True(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA));
                Assert.Equal([3, 2, 1], logs);
                logs.Clear();
            }
            finally
            {
                focusNode.Dispose();
            }
        });

    // Regression test for https://github.com/flutter/flutter/issues/99196 .
    //
    // In rawKeyData mode, if a key down event is dispatched but immediately
    // synthesized to be released, the old logic would trigger a Null check
    // _CastError on _hardwareKeyboard.lookUpLayout(key). The original scenario
    // that this is triggered on Android is unknown. Here we make up a scenario
    // where a ShiftLeft key down is dispatched but the modifier bit is not set.
    [Theory]
    [MemberData(nameof(RawKeyDataVariant))]
    public void CorrectlyConvertDownEventsThatAreSynthesizedReleased(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode = new FocusNode();
            var events = new List<KeyEvent>();
            try
            {
                tester.PumpWidget(new KeyboardListener(
                    autofocus: true,
                    focusNode: focusNode,
                    child: new Container(),
                    onKeyEvent: events.Add));

                // Dispatch an arbitrary event to bypass the pressedKeys check.
                KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA, platform: "web");

                // Dispatch an
                Dictionary<string, object?> data2 =
                    KeyEventSimulator.GetKeyData(LogicalKeyboardKey.ShiftLeft, platform: "web");
                data2["metaState"] = 0;
                RawKeyboardDartParityTests.HandlePlatformMessage(data2);

                Assert.Equal(3, events.Count);
                Assert.IsType<KeyDownEvent>(events[1]);
                Assert.Equal(LogicalKeyboardKey.ShiftLeft, events[1].LogicalKey);
                Assert.False(events[1].Synthesized);
                Assert.IsType<KeyUpEvent>(events[2]);
                Assert.Equal(LogicalKeyboardKey.ShiftLeft, events[2].LogicalKey);
                Assert.True(events[2].Synthesized);
                Assert.Equal(
                    new HashSet<PhysicalKeyboardKey> { PhysicalKeyboardKey.KeyA },
                    ServicesBinding.Instance.Keyboard.PhysicalKeysPressed);
            }
            finally
            {
                focusNode.Dispose();
            }
        });

    [Theory]
    [MemberData(nameof(KeyDataThenRawKeyDataVariant))]
    public void InstantlyDispatchSynthesizedKeyEventsWhenTheQueueIsEmpty(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var focusNode = new FocusNode();
            var logs = new List<int>();
            try
            {
                tester.PumpWidget(new KeyboardListener(
                    autofocus: true,
                    focusNode: focusNode,
                    child: new Container(),
                    onKeyEvent: _ => logs.Add(1)));
                ServicesBinding.Instance.Keyboard.AddHandler(_ =>
                {
                    logs.Add(2);
                    return false;
                });

                // Dispatch a solitary synthesized event.
                Assert.False(ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    timeStamp: TimeSpan.Zero,
                    type: KeyEventType.Down,
                    logical: LogicalKeyboardKey.KeyA.KeyId,
                    physical: PhysicalKeyboardKey.KeyA.UsbHidUsage,
                    character: null,
                    synthesized: true)));
                Assert.Equal([2, 1], logs);
                logs.Clear();
            }
            finally
            {
                focusNode.Dispose();
            }
        });

    [Theory]
    [MemberData(nameof(KeyDataThenRawKeyDataVariant))]
    public void PostponeSynthesizedKeyEventsWhenTheQueueIsNotEmpty(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            using var tester = new FrameworkDartTester();
            var keyboardListenerFocusNode = new FocusNode();
            var rawKeyboardListenerFocusNode = new FocusNode();
            var logs = new List<string>();
            try
            {
                tester.PumpWidget(new RawKeyboardListener(
                    focusNode: rawKeyboardListenerFocusNode,
                    onKey: @event => logs.Add(@event.GetType().Name),
                    child: new KeyboardListener(
                        autofocus: true,
                        focusNode: keyboardListenerFocusNode,
                        child: new Container(),
                        onKeyEvent: @event => logs.Add(@event.GetType().Name))));

                // On macOS, a CapsLock tap yields a down event and a synthesized up event.
                Assert.False(ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    timeStamp: TimeSpan.Zero,
                    type: KeyEventType.Down,
                    logical: LogicalKeyboardKey.CapsLock.KeyId,
                    physical: PhysicalKeyboardKey.CapsLock.UsbHidUsage,
                    character: null,
                    synthesized: false)));
                Assert.False(ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
                    timeStamp: TimeSpan.Zero,
                    type: KeyEventType.Up,
                    logical: LogicalKeyboardKey.CapsLock.KeyId,
                    physical: PhysicalKeyboardKey.CapsLock.UsbHidUsage,
                    character: null,
                    synthesized: true)));
                object? reply = ServicesBinding.Instance.KeyEventManager.HandleRawKeyMessage(
                    new Dictionary<string, object?>
                    {
                        ["type"] = "keydown",
                        ["keymap"] = "macos",
                        ["keyCode"] = 0x00000039,
                        ["characters"] = string.Empty,
                        ["charactersIgnoringModifiers"] = string.Empty,
                        ["modifiers"] = 0x10000,
                    }).GetAwaiter().GetResult();
                // equals(<String, dynamic>{'handled': false})
                var replyMap = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(reply);
                Assert.Equal(["handled"], replyMap.Keys);
                Assert.Equal(false, replyMap["handled"]);

                Assert.Equal(["RawKeyDownEvent", "KeyDownEvent", "KeyUpEvent"], logs);
                logs.Clear();
            }
            finally
            {
                keyboardListenerFocusNode.Dispose();
                rawKeyboardListenerFocusNode.Dispose();
            }
        });

    // The first key data received from the engine might be an empty key data.
    // In that case, the key data should not be converted to any [KeyEvent]s,
    // but is only used so that *a* key data comes before the raw key message
    // and makes [KeyEventManager] infer [KeyDataTransitMode.keyDataThenRawKeyData].
    [Fact]
    public void EmptyKeyDataYieldsNoEventButTriggersInference()
    {
        var events = new List<KeyEvent>();
        var rawEvents = new List<RawKeyEvent>();
        ServicesBinding.Instance.Keyboard.AddHandler(@event =>
        {
            events.Add(@event);
            return true;
        });
        RawKeyboard.Instance.AddListener(@event => rawEvents.Add(@event));
        ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
            type: KeyEventType.Down,
            timeStamp: TimeSpan.Zero,
            logical: 0,
            physical: 0,
            character: "a",
            synthesized: false));
        ServicesBinding.Instance.KeyEventManager.HandleRawKeyMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 0x04,
            ["scanCode"] = 0x04,
            ["characterCodePoint"] = 0,
            ["modifiers"] = 0,
        }).GetAwaiter().GetResult();
        Assert.Empty(events);
        Assert.Single(rawEvents);

        // Dispatch another key data to ensure it's in
        // [KeyDataTransitMode.keyDataThenRawKeyData] mode (otherwise assertion
        // will be thrown upon a KeyData).
        ServicesBinding.Instance.KeyEventManager.HandleKeyData(new KeyData(
            type: KeyEventType.Down,
            timeStamp: TimeSpan.Zero,
            logical: 0x22,
            physical: 0x70034,
            character: "\"",
            synthesized: false));
        ServicesBinding.Instance.KeyEventManager.HandleRawKeyMessage(new Dictionary<string, object?>
        {
            ["type"] = "keydown",
            ["keymap"] = "windows",
            ["keyCode"] = 0x04,
            ["scanCode"] = 0x04,
            ["characterCodePoint"] = 0,
            ["modifiers"] = 0,
        }).GetAwaiter().GetResult();
        Assert.Single(events);
        Assert.Equal(2, rawEvents.Count);
    }

    [Fact]
    public void ExceptionsFromKeyMessageHandlerAreCaughtAndReported()
    {
        KeyMessageHandler? oldKeyMessageHandler = ServicesBinding.Instance.KeyEventManager.KeyMessageHandler;
        try
        {
            // Dart throws the integer 1; C# throws a sentinel exception and checks its identity.
            var thrown = new InvalidOperationException("1");

            // When keyMessageHandler throws an error...
            ServicesBinding.Instance.KeyEventManager.KeyMessageHandler = _ => throw thrown;

            // Simulate a key down event.
            FlutterErrorDetails? record = null;
            RawKeyboardDartParityTests.RunWhileOverridingOnError(
                () => KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA),
                onError: details => record = details);

            // ... the error should be caught.
            Assert.NotNull(record);
            Assert.Same(thrown, record.Exception);
            Dictionary<string, DiagnosticsNode> infos =
                RawKeyboardDartParityTests.GroupDiagnosticsByName(record.InformationCollector!());
            Assert.IsType<DiagnosticsProperty<KeyMessage>>(infos["KeyMessage"]);

            // But the exception should not interrupt recording the state.
            // Now the keyMessageHandler no longer throws an error.
            ServicesBinding.Instance.KeyEventManager.KeyMessageHandler = null;
            record = null;

            // Simulate a key up event.
            RawKeyboardDartParityTests.RunWhileOverridingOnError(
                () => KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA),
                onError: details => record = details);
            // If the previous state (key down) wasn't recorded, this key up event will
            // trigger assertions.
            Assert.Null(record);
        }
        finally
        {
            ServicesBinding.Instance.KeyEventManager.KeyMessageHandler = oldKeyMessageHandler;
        }
    }

    [Theory]
    [MemberData(nameof(AllVariant))]
    public void ExceptionsFromHardwareKeyboardHandlersAreCaughtAndReported(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            // Dart throws the integer 1; C# throws a sentinel exception and checks its identity.
            var thrown = new InvalidOperationException("1");

            bool ThrowingCallback(KeyEvent @event)
            {
                throw thrown;
            }

            // When the handler throws an error...
            HardwareKeyboard.Instance.AddHandler(ThrowingCallback);

            // Simulate a key down event.
            FlutterErrorDetails? record = null;
            RawKeyboardDartParityTests.RunWhileOverridingOnError(
                () => KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA),
                onError: details => record = details);

            // ... the error should be caught.
            Assert.NotNull(record);
            Assert.Same(thrown, record.Exception);
            Dictionary<string, DiagnosticsNode> infos =
                RawKeyboardDartParityTests.GroupDiagnosticsByName(record.InformationCollector!());
            Assert.IsType<DiagnosticsProperty<KeyEvent>>(infos["Event"]);

            // But the exception should not interrupt recording the state.
            // Now the key handler no longer throws an error.
            HardwareKeyboard.Instance.RemoveHandler(ThrowingCallback);
            record = null;

            // Simulate a key up event.
            RawKeyboardDartParityTests.RunWhileOverridingOnError(
                () => KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA),
                onError: details => record = details);
            // If the previous state (key down) wasn't recorded, this key up event will
            // trigger assertions.
            Assert.Null(record);
        });

    [Fact]
    public void DebugPrintKeyboardEventsCausesLoggingOfKeyEvents()
    {
        bool oldDebugPrintKeyboardEvents = KeyboardDebug.DebugPrintKeyboardEvents;
        DebugPrintCallback oldDebugPrint = Print.DebugPrint;
        var messages = new System.Text.StringBuilder();
        Print.DebugPrint = (message, _) => messages.AppendLine(message ?? string.Empty);
        KeyboardDebug.DebugPrintKeyboardEvents = true;
        try
        {
            KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA);
        }
        finally
        {
            KeyboardDebug.DebugPrintKeyboardEvents = oldDebugPrintKeyboardEvents;
            Print.DebugPrint = oldDebugPrint;
        }

        string messagesStr = messages.ToString();
        Assert.Contains("KEYBOARD: Key event received: ", messagesStr, StringComparison.Ordinal);
        Assert.Contains("KEYBOARD: Pressed state before processing the event:", messagesStr, StringComparison.Ordinal);
        Assert.Contains("KEYBOARD: Pressed state after processing the event:", messagesStr, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(KeyDataThenRawKeyDataVariant))]
    public void IrregularKeyEventsAreProcessed(KeyDataTransitMode mode) =>
        KeySimulatorTransitModeVariant.Run(mode, () =>
        {
            var logs = new List<string>();

            KeyboardDebug.DebugPrintKeyboardEvents = true;
            DebugPrintCallback oldDebugPrint = Print.DebugPrint;
            Print.DebugPrint = (message, _) =>
            {
                if (message != null)
                {
                    logs.Add(message);
                }
            };

            void ExpectIsRegular(bool simulateEvent, bool isRegular)
            {
                // All simulated events should return a `handled` result of false to
                // indicate that they are indeed dispatched to handlers.
                Assert.False(simulateEvent);

                string log = string.Join("\n", logs);
                bool hasError = log.Contains("ERROR", StringComparison.Ordinal);
                Assert.True(isRegular ? !hasError : hasError, log);
                logs.Clear();
            }

            try
            {
                // 1. Press keyA.
                ExpectIsRegular(KeyEventSimulator.SimulateKeyDownEvent(LogicalKeyboardKey.KeyA), true);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>
                {
                    [PhysicalKeyboardKey.KeyA] = LogicalKeyboardKey.KeyA,
                });
                // Press keyA again with a mismatched logical key, which should affect the
                // state.
                ExpectIsRegular(
                    KeyEventSimulator.SimulateKeyDownEvent(
                        LogicalKeyboardKey.KeyB,
                        physicalKey: PhysicalKeyboardKey.KeyA),
                    false);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>
                {
                    [PhysicalKeyboardKey.KeyA] = LogicalKeyboardKey.KeyB,
                });

                // 2. Release keyA.
                ExpectIsRegular(
                    KeyEventSimulator.SimulateKeyUpEvent(
                        LogicalKeyboardKey.KeyB,
                        physicalKey: PhysicalKeyboardKey.KeyA),
                    true);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>());

                // Release keyA again.
                ExpectIsRegular(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA), false);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>());

                // 3. Send a repeat event for keyA, which should affect the state.
                ExpectIsRegular(KeyEventSimulator.SimulateKeyRepeatEvent(LogicalKeyboardKey.KeyA), false);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>
                {
                    [PhysicalKeyboardKey.KeyA] = LogicalKeyboardKey.KeyA,
                });

                // 4. Send a repeat event with a mismatched logical key, which should
                // affect the state.
                ExpectIsRegular(
                    KeyEventSimulator.SimulateKeyDownEvent(
                        LogicalKeyboardKey.KeyB,
                        physicalKey: PhysicalKeyboardKey.KeyA),
                    false);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>
                {
                    [PhysicalKeyboardKey.KeyA] = LogicalKeyboardKey.KeyB,
                });

                // 5. Send a key up event with a mismatched logical key, which should affect the state.
                ExpectIsRegular(KeyEventSimulator.SimulateKeyUpEvent(LogicalKeyboardKey.KeyA), false);
                ExpectState(new Dictionary<PhysicalKeyboardKey, LogicalKeyboardKey>());
            }
            finally
            {
                KeyboardDebug.DebugPrintKeyboardEvents = false;
                Print.DebugPrint = oldDebugPrint;
            }
        });
}
#pragma warning restore CS0618
