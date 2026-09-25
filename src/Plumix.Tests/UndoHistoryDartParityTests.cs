using System.Collections;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: flutter/packages/flutter/test/widgets/undo_history_test.dart and
// flutter/packages/flutter/test/services/undo_manager_test.dart. Each Dart
// `TargetPlatformVariant.all()` is a theory row.
public sealed class UndoHistoryDartParityTests : IDisposable
{
    private static readonly TimeSpan Throttle = TimeSpan.FromMilliseconds(500);

    public UndoHistoryDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        UndoManager.Client = null;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        UndoManager.Client = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms => new(Enum.GetValues<TargetPlatform>());

    private static FrameworkDartTester NewTester(TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        return new FrameworkDartTester(fakeGestureTimers: true);
    }

    private static void SendUndoRedo(bool redo = false)
    {
        bool apple = PlatformDefaults.TargetPlatform is TargetPlatform.IOS or TargetPlatform.MacOS;
        KeySim.SendKeyCombination(LogicalKeyboardKey.KeyZ, control: !apple, meta: apple, shift: redo);
    }

    private static void SendUndo() => SendUndoRedo();

    private static void SendRedo() => SendUndoRedo(redo: true);

    [Fact]
    public void RegistersAsGlobalUndoRedoClient()
    {
        using FrameworkDartTester tester = NewTester();
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        var undoHistoryGlobalKey = new LabeledGlobalKey<UndoHistoryState<int>>("undo");
        using var value = new ValueNotifier<int>(0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                key: undoHistoryGlobalKey,
                value: value,
                onTriggered: _ => { },
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        // Initially the UndoHistory doesn't have focus, therefore it should not be the global
        // undo/redo client.
        Assert.Null(UndoManager.Client);

        focusNode.RequestFocus();
        tester.Pump();

        Assert.Same(undoHistoryGlobalKey.CurrentState, UndoManager.Client);
    }

    [Fact]
    public void DeregistersAsGlobalUndoRedoClientWhenItLosesFocus()
    {
        using FrameworkDartTester tester = NewTester();
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        var undoHistoryGlobalKey = new LabeledGlobalKey<UndoHistoryState<int>>("undo");
        using var value = new ValueNotifier<int>(0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                key: undoHistoryGlobalKey,
                value: value,
                onTriggered: _ => { },
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        focusNode.RequestFocus();
        tester.Pump();
        Assert.Same(undoHistoryGlobalKey.CurrentState, UndoManager.Client);

        focusNode.Unfocus();
        tester.Pump();
        Assert.Null(UndoManager.Client);
    }

    [Fact]
    public void DeregistersAsGlobalUndoRedoClientWhenDisposed()
    {
        using FrameworkDartTester tester = NewTester();
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        var undoHistoryGlobalKey = new LabeledGlobalKey<UndoHistoryState<int>>("undo");
        using var value = new ValueNotifier<int>(0);
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                key: undoHistoryGlobalKey,
                value: value,
                onTriggered: _ => { },
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        focusNode.RequestFocus();
        tester.Pump();
        Assert.Same(undoHistoryGlobalKey.CurrentState, UndoManager.Client);

        tester.PumpWidget(new TestWidgetsApp(home: new SizedBox()));
        Assert.Null(UndoManager.Client);
    }

    private static void AssertCan(UndoHistoryController controller, bool canUndo, bool canRedo)
    {
        Assert.Equal(canUndo, controller.Value.CanUndo);
        Assert.Equal(canRedo, controller.Value.CanRedo);
    }

    // The shared body of 'allows undo and redo to be called programmatically from the
    // UndoHistoryController' and 'allows undo and redo to be called using the keyboard'.
    private static void RunUndoRedoSequence(bool useKeyboard, TargetPlatform platform)
    {
        using FrameworkDartTester tester = NewTester(platform);
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        using var value = new ValueNotifier<int>(0);
        using var controller = new UndoHistoryController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                value: value,
                controller: controller,
                onTriggered: newValue => value.Value = newValue,
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        void Undo()
        {
            if (useKeyboard)
            {
                SendUndo();
            }
            else
            {
                controller.Undo();
            }
        }

        void Redo()
        {
            if (useKeyboard)
            {
                SendRedo();
            }
            else
            {
                controller.Redo();
            }
        }

        tester.Pump(Throttle);

        // Undo/redo have no effect if the value has never changed.
        AssertCan(controller, canUndo: false, canRedo: false);
        Undo();
        Assert.Equal(0, value.Value);
        Redo();
        Assert.Equal(0, value.Value);

        focusNode.RequestFocus();
        tester.Pump();
        AssertCan(controller, canUndo: false, canRedo: false);
        Undo();
        Assert.Equal(0, value.Value);
        Redo();
        Assert.Equal(0, value.Value);

        value.Value = 1;
        // Wait for the throttling.
        tester.Pump(Throttle);

        // Can undo/redo a single change.
        AssertCan(controller, canUndo: true, canRedo: false);
        Undo();
        Assert.Equal(0, value.Value);
        AssertCan(controller, canUndo: false, canRedo: true);
        Redo();
        Assert.Equal(1, value.Value);
        AssertCan(controller, canUndo: true, canRedo: false);

        value.Value = 2;
        tester.Pump(Throttle);

        // And can undo/redo multiple changes.
        AssertCan(controller, canUndo: true, canRedo: false);
        Undo();
        Assert.Equal(1, value.Value);
        AssertCan(controller, canUndo: true, canRedo: true);
        Undo();
        Assert.Equal(0, value.Value);
        AssertCan(controller, canUndo: false, canRedo: true);
        Redo();
        Assert.Equal(1, value.Value);
        AssertCan(controller, canUndo: true, canRedo: true);
        Redo();
        Assert.Equal(2, value.Value);
        AssertCan(controller, canUndo: true, canRedo: false);

        // Changing the value again clears the redo stack.
        Undo();
        Assert.Equal(1, value.Value);
        AssertCan(controller, canUndo: true, canRedo: true);
        value.Value = 3;
        tester.Pump(Throttle);
        AssertCan(controller, canUndo: true, canRedo: false);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void AllowsUndoAndRedoToBeCalledProgrammaticallyFromTheController(TargetPlatform platform) =>
        RunUndoRedoSequence(useKeyboard: false, platform);

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void AllowsUndoAndRedoToBeCalledUsingTheKeyboard(TargetPlatform platform) =>
        RunUndoRedoSequence(useKeyboard: true, platform);

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void DuplicateChangesDoNotAffectTheUndoHistory(TargetPlatform platform)
    {
        using FrameworkDartTester tester = NewTester(platform);
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        using var value = new ValueNotifier<int>(0);
        using var controller = new UndoHistoryController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                controller: controller,
                value: value,
                onTriggered: newValue => value.Value = newValue,
                focusNode: focusNode,
                child: new Container())));

        focusNode.RequestFocus();

        // Wait for the throttling.
        tester.Pump(Throttle);

        value.Value = 1;

        // Wait for the throttling.
        tester.Pump(Throttle);

        // Can undo/redo a single change.
        controller.Undo();
        Assert.Equal(0, value.Value);
        controller.Redo();
        Assert.Equal(1, value.Value);

        // Changes that result in the same state won't be saved on the undo stack.
        value.Value = 1;
        tester.Pump(Throttle);
        controller.Undo();
        Assert.Equal(0, value.Value);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void IgnoresValueChangesPushedDuringOnTriggered(TargetPlatform platform)
    {
        using FrameworkDartTester tester = NewTester(platform);
        using var focusNode = new FocusNode(debugLabel: "UndoHistory Node");
        using var value = new ValueNotifier<int>(0);
        using var controller = new UndoHistoryController();
        Func<int, int> valueToUse = newValue => newValue;
        var key = new LabeledGlobalKey<UndoHistoryState<int>>("undo");
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                key: key,
                value: value,
                controller: controller,
                onTriggered: newValue => value.Value = valueToUse(newValue),
                focusNode: focusNode,
                child: new Container())));

        tester.Pump(Throttle);

        // Undo/redo have no effect if the value has never changed.
        controller.Undo();
        Assert.Equal(0, value.Value);
        controller.Redo();
        Assert.Equal(0, value.Value);

        focusNode.RequestFocus();
        tester.Pump();
        controller.Undo();
        Assert.Equal(0, value.Value);
        controller.Redo();
        Assert.Equal(0, value.Value);

        value.Value = 1;

        // Wait for the throttling.
        tester.Pump(Throttle);

        valueToUse = _ => 3;
        Assert.Throws<AssertionError>(() => key.CurrentState!.Undo());
    }

    [Fact]
    public void ChangesSendSetUndoStateToTheUndoManagerOnIOS()
    {
        using FrameworkDartTester tester = NewTester(TargetPlatform.IOS);
        using var undoManager = new MockMethodCallHandler(SystemChannels.UndoManager);
        using var focusNode = new FocusNode();
        using var value = new ValueNotifier<int>(0);
        using var controller = new UndoHistoryController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                controller: controller,
                value: value,
                onTriggered: newValue => value.Value = newValue,
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        tester.Pump();
        focusNode.RequestFocus();
        tester.Pump();

        // Wait for the throttling.
        tester.Pump(Throttle);

        void AssertLastUndoState(bool canUndo, bool canRedo)
        {
            MethodCall methodCall = undoManager.Log.Last(call => call.Method == "UndoManager.setUndoState");
            var arguments = (IDictionary)methodCall.Arguments!;
            Assert.Equal(canUndo, arguments["canUndo"]);
            Assert.Equal(canRedo, arguments["canRedo"]);
        }

        // Undo and redo should both be disabled.
        AssertLastUndoState(canUndo: false, canRedo: false);

        // Making a change should enable undo.
        value.Value = 1;
        tester.Pump(Throttle);
        AssertLastUndoState(canUndo: true, canRedo: false);

        // Undo should remain enabled after another change.
        value.Value = 2;
        tester.Pump(Throttle);
        AssertLastUndoState(canUndo: true, canRedo: false);

        // Undo and redo should be enabled after one undo.
        controller.Undo();
        AssertLastUndoState(canUndo: true, canRedo: true);

        // Only redo should be enabled after a second undo.
        controller.Undo();
        AssertLastUndoState(canUndo: false, canRedo: true);
    }

    [Fact]
    public void HandlePlatformUndoShouldUndoOrRedoAppropriatelyOnIOS()
    {
        using FrameworkDartTester tester = NewTester(TargetPlatform.IOS);
        using var focusNode = new FocusNode();
        using var value = new ValueNotifier<int>(0);
        using var controller = new UndoHistoryController();
        tester.PumpWidget(new TestWidgetsApp(
            home: new UndoHistory<int>(
                controller: controller,
                value: value,
                onTriggered: newValue => value.Value = newValue,
                focusNode: focusNode,
                child: new Focus(focusNode: focusNode, child: new Container()))));

        tester.Pump(Throttle);
        focusNode.RequestFocus();
        tester.Pump();

        // Undo/redo have no effect if the value has never changed.
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Undo);
        Assert.Equal(0, value.Value);
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Redo);
        Assert.Equal(0, value.Value);

        value.Value = 1;

        // Wait for the throttling.
        tester.Pump(Throttle);

        // Can undo/redo a single change.
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Undo);
        Assert.Equal(0, value.Value);
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Redo);
        Assert.Equal(1, value.Value);

        value.Value = 2;
        tester.Pump(Throttle);

        // And can undo/redo multiple changes.
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Undo);
        Assert.Equal(1, value.Value);
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Undo);
        Assert.Equal(0, value.Value);
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Redo);
        Assert.Equal(1, value.Value);
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Redo);
        Assert.Equal(2, value.Value);

        // Changing the value again clears the redo stack.
        UndoManager.Client!.HandlePlatformUndo(UndoDirection.Undo);
        Assert.Equal(1, value.Value);
        value.Value = 3;
        tester.Pump(Throttle);
    }

    [Fact]
    public void UndoHistoryControllerNotifiesOnUndoListenersOnUndo()
    {
        int calls = 0;
        using var controller = new UndoHistoryController();
        controller.OnUndo.AddListener(() => calls++);

        // Does not notify the listener if canUndo is false.
        controller.Undo();
        Assert.Equal(0, calls);

        // Does notify the listener if canUndo is true.
        controller.Value = new UndoHistoryValue(canUndo: true);
        controller.Undo();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void UndoHistoryControllerNotifiesOnRedoListenersOnRedo()
    {
        int calls = 0;
        using var controller = new UndoHistoryController();
        controller.OnRedo.AddListener(() => calls++);

        // Does not notify the listener if canRedo is false.
        controller.Redo();
        Assert.Equal(0, calls);

        // Does notify the listener if canRedo is true.
        controller.Value = new UndoHistoryValue(canRedo: true);
        controller.Redo();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void UndoHistoryControllerNotifiesListenersOnValueChange()
    {
        int calls = 0;
        using var controller = new UndoHistoryController(value: new UndoHistoryValue(canUndo: true));
        controller.AddListener(() => calls++);

        // Does not notify if the value is the same.
        controller.Value = new UndoHistoryValue(canUndo: true);
        Assert.Equal(0, calls);

        // Does notify if the value has changed.
        controller.Value = new UndoHistoryValue(canRedo: true);
        Assert.Equal(1, calls);
    }

    // ------------------------------------------------------------ undo_manager_test.dart

    [Fact]
    public void UndoManagerClientHandleUndo()
    {
        // Assemble an UndoManagerClient so we can verify its change in state.
        var client = new FakeUndoManagerClient();
        UndoManager.Client = client;

        Assert.Empty(client.LatestMethodCall);

        // Send handleUndo message with "undo" as the direction.
        SendPlatformUndo("undo");
        Assert.Equal("handlePlatformUndo(UndoDirection.undo)", client.LatestMethodCall);

        // Send handleUndo message with "redo" as the direction.
        SendPlatformUndo("redo");
        Assert.Equal("handlePlatformUndo(UndoDirection.redo)", client.LatestMethodCall);
    }

    [Fact]
    public void UndoManagerSetUndoStateReportsErrorWhenChannelFails()
    {
        FlutterExceptionHandler? oldOnError = FlutterError.OnError;
        var errors = new List<FlutterErrorDetails>();
        FlutterError.OnError = details => errors.Add(details);
        using var failing = new MockMethodCallHandler(
            SystemChannels.UndoManager,
            _ => throw new PlatformException(code: "UNDO_ERROR", message: "Undo manager unavailable"));
        try
        {
            UndoManager.SetUndoState(canUndo: true, canRedo: false);
            Assert.True(EventLoopPump.SpinUntil(() => errors.Count > 0));

            FlutterErrorDetails error = Assert.Single(errors);
            var exception = Assert.IsType<PlatformException>(error.Exception);
            Assert.Equal("UNDO_ERROR", exception.Code);
            Assert.Equal("services library", error.Library);
            Assert.Equal("while sending the UndoManager.setUndoState event", error.Context!.ToString());
        }
        finally
        {
            FlutterError.OnError = oldOnError;
        }
    }

    private static void SendPlatformUndo(string direction)
    {
        MethodChannel channel = SystemChannels.UndoManager;
        channel.BinaryMessenger.HandlePlatformMessage(
            channel.Name,
            channel.Codec.EncodeMethodCall(
                new MethodCall("UndoManagerClient.handleUndo", new List<object?> { direction })),
            _ => { });
    }

    private sealed class FakeUndoManagerClient : IUndoManagerClient
    {
        public string LatestMethodCall { get; private set; } = string.Empty;

        public void HandlePlatformUndo(UndoDirection direction) =>
            LatestMethodCall = $"handlePlatformUndo(UndoDirection.{direction.ToString().ToLowerInvariant()})";

        public void Undo() => LatestMethodCall = "undo";

        public void Redo() => LatestMethodCall = "redo";

        public bool CanUndo => false;

        public bool CanRedo => false;
    }
}
