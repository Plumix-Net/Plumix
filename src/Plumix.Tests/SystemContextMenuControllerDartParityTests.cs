using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: flutter/packages/flutter/test/services/system_context_menu_controller_test.dart
// and the SystemContextMenu tests of flutter/packages/flutter/test/services/text_input_test.dart.
public sealed class SystemContextMenuControllerDartParityTests : IDisposable
{
    private readonly MockClipboardPlatform _clipboard;
    private readonly List<IDictionary> _targetRects = [];
    private readonly List<IReadOnlyList<IOSSystemContextMenuItemData>> _itemsReceived = [];
    private TextInputConnection? _connection;
    private int _hideCount;

    public SystemContextMenuControllerDartParityTests()
    {
        _clipboard = new MockClipboardPlatform(text: "Clipboard data");
        SystemChannels.Platform.SetPlatformMethodCallHandler(call =>
        {
            switch (call.Method)
            {
                case "ContextMenu.showSystemContextMenu":
                    var arguments = (IDictionary)call.Arguments!;
                    _targetRects.Add((IDictionary)arguments["targetRect"]!);
                    if (arguments.Contains("items"))
                    {
                        _itemsReceived.Add(((IList)arguments["items"]!)
                            .Cast<IDictionary>()
                            .Select(SystemContextMenuDartParityTests.ItemDataFromJson)
                            .ToList());
                    }

                    break;
                case "ContextMenu.hideSystemContextMenu":
                    _hideCount += 1;
                    break;
            }

            return Task.FromResult<object?>(null);
        });
    }

    public void Dispose()
    {
        _connection?.Close();
        SystemChannels.Platform.SetPlatformMethodCallHandler(null);
        _clipboard.Dispose();
        SystemContextMenuController.ResetForTests();
        ServicesBinding.SystemContextMenuClient = null;
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    private void Attach(string text = "test1")
    {
        var client = new FakeTextInputClient(new TextEditingValue(text));
        _connection = UI.TextInput.Attach(client, new TextInputConfiguration());
    }

    private static readonly Rect Rect1 = new(0, 0, 100, 100);
    private static readonly Rect Rect2 = new(1, 1, 200, 200);
    private static readonly IReadOnlyList<IOSSystemContextMenuItemData> CopyItems =
        [new IOSSystemContextMenuItemDataCopy()];

    private static void AssertRect(IDictionary json, Rect rect)
    {
        Assert.Equal(rect.Left, Convert.ToDouble(json["x"]));
        Assert.Equal(rect.Top, Convert.ToDouble(json["y"]));
        Assert.Equal(rect.Width, Convert.ToDouble(json["width"]));
        Assert.Equal(rect.Height, Convert.ToDouble(json["height"]));
    }

    [Fact]
    public void ShowingAndHidingOneController()
    {
        Attach();
        var controller = new SystemContextMenuController();
        Assert.Empty(_targetRects);
        Assert.Equal(0, _hideCount);
        Assert.False(controller.IsVisible);

        _ = controller.ShowWithItems(Rect1, CopyItems);
        AssertRect(Assert.Single(_targetRects), Rect1);

        // Showing the same thing again does nothing.
        _ = controller.ShowWithItems(Rect1, CopyItems);
        Assert.True(controller.IsVisible);
        Assert.Single(_targetRects);

        // Showing a new rect calls the platform.
        _ = controller.ShowWithItems(Rect2, CopyItems);
        Assert.Equal(2, _targetRects.Count);
        AssertRect(_targetRects[^1], Rect2);

        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Equal(1, _hideCount);

        // Hiding again does nothing.
        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Equal(1, _hideCount);

        // Showing the last shown rect calls the platform.
        _ = controller.ShowWithItems(Rect2, CopyItems);
        Assert.True(controller.IsVisible);
        Assert.Equal(3, _targetRects.Count);
        AssertRect(_targetRects[^1], Rect2);

        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Equal(2, _hideCount);
        controller.Dispose();
    }

    [Fact]
    public async Task TheSystemCanHideTheMenuWithHandleSystemHide()
    {
        Attach();
        int systemHideCount = 0;
        var controller = new SystemContextMenuController(onSystemHide: () => systemHideCount += 1);
        Assert.False(controller.IsVisible);

        await controller.ShowWithItems(Rect1, CopyItems);
        Assert.True(controller.IsVisible);
        AssertRect(Assert.Single(_targetRects), Rect1);
        Assert.Equal(0, _hideCount);
        Assert.Equal(0, systemHideCount);

        // If the system hides the menu, onSystemHide is called.
        SystemContextMenuDartParityTests.SendDismiss();
        Assert.False(controller.IsVisible);
        Assert.Equal(0, _hideCount);
        Assert.Equal(1, systemHideCount);

        // Hiding does not call the platform, since the menu was already hidden.
        await controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Equal(0, _hideCount);
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void ShowingASecondControllerWhileOneIsVisibleIsAnError()
    {
        Attach();
        var controller1 = new SystemContextMenuController();
        _ = controller1.ShowWithItems(Rect1, CopyItems);
        Assert.True(controller1.IsVisible);

        var controller2 = new SystemContextMenuController();
        Assert.True(controller1.IsVisible);
        Assert.False(controller2.IsVisible);
        Assert.Throws<AssertionError>(() => { _ = controller2.ShowWithItems(Rect2, CopyItems); });
        Assert.True(controller1.IsVisible);
        Assert.False(controller2.IsVisible);

        _ = controller1.Hide();
        Assert.False(controller1.IsVisible);
        Assert.False(controller2.IsVisible);
        controller1.Dispose();
        controller2.Dispose();
    }

    [Fact]
    public void ShowingAndHidingTwoControllers()
    {
        Attach();
        var controller1 = new SystemContextMenuController();
        _ = controller1.ShowWithItems(Rect1, CopyItems);
        Assert.True(controller1.IsVisible);
        Assert.Equal(Rect1.Left, Convert.ToDouble(Assert.Single(_targetRects)["x"]));

        _ = controller1.Hide();
        Assert.False(controller1.IsVisible);
        Assert.Equal(1, _hideCount);

        var controller2 = new SystemContextMenuController();
        Assert.False(controller2.IsVisible);
        _ = controller2.ShowWithItems(Rect2, CopyItems);
        Assert.False(controller1.IsVisible);
        Assert.True(controller2.IsVisible);
        Assert.Equal(2, _targetRects.Count);
        AssertRect(_targetRects[^1], Rect2);

        // Hiding a controller that isn't shown does nothing.
        _ = controller1.Hide();
        Assert.False(controller1.IsVisible);
        Assert.True(controller2.IsVisible);
        Assert.Equal(1, _hideCount);

        _ = controller2.Hide();
        Assert.False(controller1.IsVisible);
        Assert.False(controller2.IsVisible);
        Assert.Equal(2, _hideCount);
        controller1.Dispose();
        controller2.Dispose();
    }

    [Fact]
    public void ShowingAControllerWithCustomItems()
    {
        Attach();
        var controller = new SystemContextMenuController();
        IReadOnlyList<IOSSystemContextMenuItemData> items1 =
        [
            new IOSSystemContextMenuItemDataCut(),
            new IOSSystemContextMenuItemDataCopy(),
            new IOSSystemContextMenuItemDataPaste(),
            new IOSSystemContextMenuItemDataSelectAll(),
            new IOSSystemContextMenuItemDataSearchWeb(title: "Special Search"),
        ];
        _ = controller.ShowWithItems(Rect1, items1);
        Assert.True(controller.IsVisible);
        IReadOnlyList<IOSSystemContextMenuItemData> received = Assert.Single(_itemsReceived);
        Assert.Equal(items1.Count, received.Count);
        Assert.Equal(items1, received);

        // Showing the same thing again does nothing.
        _ = controller.ShowWithItems(Rect1, items1);
        Assert.Single(_itemsReceived);

        // Showing new items calls the platform.
        IReadOnlyList<IOSSystemContextMenuItemData> items2 = [new IOSSystemContextMenuItemDataCut()];
        _ = controller.ShowWithItems(Rect1, items2);
        Assert.Equal(2, _itemsReceived.Count);
        Assert.Equal(items2, _itemsReceived[^1]);

        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void ShowingAControllerWithEmptyItems()
    {
        Attach();
        var controller = new SystemContextMenuController();
        Assert.Throws<AssertionError>(() => { _ = controller.ShowWithItems(Rect1, []); });
        Assert.False(controller.IsVisible);
        Assert.Empty(_targetRects);
        controller.Dispose();
    }

    [Fact]
    public async Task ShowingAControllerForAnEditableText()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        FocusManager.Instance.ResetForTests();
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("test");
        var focusNode = new FocusNode();
        tester.PumpWidget(new TestWidgetsApp(home: new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 400,
                child: new EditableText(
                    controller,
                    maxLines: 10,
                    showSelectionHandles: true,
                    autofocus: true,
                    focusNode: focusNode,
                    style: new TextStyle(),
                    cursorColor: new Color(0xFF0000FF),
                    backgroundCursorColor: new Color(0xFF808080),
                    keyboardType: TextInputType.Text,
                    textAlign: TextAlign.Right,
                    selectionControls: EmptyTextSelectionControls.Instance)))));
        tester.Pump();

        var state = tester.State<EditableText.EditableTextState>();
        IReadOnlyList<IOSSystemContextMenuItem> defaultItems = SystemContextMenu.GetDefaultItems(state);
        Assert.Equal(
            new IOSSystemContextMenuItem[]
            {
                new IOSSystemContextMenuItemCut(),
                new IOSSystemContextMenuItemCopy(),
                new IOSSystemContextMenuItemPaste(),
                new IOSSystemContextMenuItemSelectAll(),
            },
            defaultItems);

        (double startGlyphHeight, double endGlyphHeight) = state.GetGlyphHeights();
        RenderEditable renderEditable = state.RenderEditableObject;
        Rect anchor = TextSelectionToolbarAnchors.GetSelectionRect(
            renderEditable,
            startGlyphHeight,
            endGlyphHeight,
            renderEditable.GetEndpointsForSelection(state.TextEditingValue.Selection));
        List<IOSSystemContextMenuItemData> itemDatas =
            defaultItems.Select(item => item.GetData(new DefaultWidgetsLocalizations())).ToList();
        Assert.NotEmpty(itemDatas);

        var systemController = new SystemContextMenuController();
        Assert.False(systemController.IsVisible);
        await systemController.ShowWithItems(anchor, itemDatas);
        Assert.True(systemController.IsVisible);
        systemController.Dispose();
        focusNode.Dispose();
    }

    [DebugOnlyFact]
    public void CustomActionCallbacksAreProperlyManaged()
    {
        Attach();
        var controller = new SystemContextMenuController();
        bool action1Called = false;
        bool action2Called = false;
        var item1 = new IOSSystemContextMenuItemDataCustom(title: "Action 1", onPressed: () => action1Called = true);
        var item2 = new IOSSystemContextMenuItemDataCustom(title: "Action 2", onPressed: () => action2Called = true);
        _ = controller.ShowWithItems(Rect1, [item1, item2]);
        Assert.True(controller.IsVisible);

        controller.HandleCustomContextMenuAction(item1.CallbackId);
        Assert.True(action1Called);
        Assert.False(action2Called);

        controller.HandleCustomContextMenuAction(item2.CallbackId);
        Assert.True(action1Called);
        Assert.True(action2Called);

        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Throws<AssertionError>(() => controller.HandleCustomContextMenuAction(item1.CallbackId));
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void MultipleControllersHandleCallbacksIndependently()
    {
        Attach();
        var controller1 = new SystemContextMenuController();
        var controller2 = new SystemContextMenuController();
        bool controller1ActionCalled = false;
        bool controller2ActionCalled = false;
        var item1 = new IOSSystemContextMenuItemDataCustom(
            title: "Controller 1 Action",
            onPressed: () => controller1ActionCalled = true);
        var item2 = new IOSSystemContextMenuItemDataCustom(
            title: "Controller 2 Action",
            onPressed: () => controller2ActionCalled = true);

        _ = controller1.ShowWithItems(Rect1, [item1]);
        Assert.True(controller1.IsVisible);
        _ = controller1.Hide();
        Assert.False(controller1.IsVisible);

        _ = controller2.ShowWithItems(new Rect(100, 100, 100, 100), [item2]);
        Assert.True(controller2.IsVisible);

        controller2.HandleCustomContextMenuAction(item2.CallbackId);
        Assert.True(controller2ActionCalled);
        Assert.False(controller1ActionCalled);

        Assert.Throws<AssertionError>(() => controller1.HandleCustomContextMenuAction(item1.CallbackId));
        controller1.Dispose();
        controller2.Dispose();
    }

    [DebugOnlyFact]
    public void PlatformDismissalClearsCallbacks()
    {
        Attach();
        var controller = new SystemContextMenuController();
        bool actionCalled = false;
        var item = new IOSSystemContextMenuItemDataCustom(title: "Test Action", onPressed: () => actionCalled = true);
        _ = controller.ShowWithItems(Rect1, [item]);
        Assert.True(controller.IsVisible);

        controller.HandleSystemHide();
        Assert.False(controller.IsVisible);
        Assert.Throws<AssertionError>(() => controller.HandleCustomContextMenuAction(item.CallbackId));
        Assert.False(actionCalled);
        controller.Dispose();
    }

    [Fact]
    public void CallingHandleCustomContextMenuActionWithNoSystemContextMenuClient()
    {
        ServicesBinding.SystemContextMenuClient = null;
        Exception? exception = Record.Exception(
            () => SystemContextMenuDartParityTests.SendCustomAction("test-id"));
        Assert.Null(exception);
    }

    [DebugOnlyFact]
    public void HandleCustomContextMenuActionWithNonExistentCallbackId()
    {
        Attach();
        var controller = new SystemContextMenuController();
        _ = controller.ShowWithItems(
            Rect1,
            [new IOSSystemContextMenuItemDataCustom(title: "Test", onPressed: () => { })]);
        Assert.True(controller.IsVisible);
        Assert.Throws<AssertionError>(() => controller.HandleCustomContextMenuAction("non-existent-id"));
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void HandleCustomContextMenuActionAfterHideClearsCallbacks()
    {
        Attach();
        var controller = new SystemContextMenuController();
        bool actionCalled = false;
        var item = new IOSSystemContextMenuItemDataCustom(title: "Test Action", onPressed: () => actionCalled = true);
        _ = controller.ShowWithItems(Rect1, [item]);
        Assert.True(controller.IsVisible);

        controller.HandleCustomContextMenuAction(item.CallbackId);
        Assert.True(actionCalled);
        actionCalled = false;

        _ = controller.Hide();
        Assert.False(controller.IsVisible);
        Assert.Throws<AssertionError>(() => controller.HandleCustomContextMenuAction(item.CallbackId));
        Assert.False(actionCalled);
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void ShowingNewMenuInvalidatesOldMenuCallbacks()
    {
        Attach();
        var controller = new SystemContextMenuController();
        bool oldActionCalled = false;
        bool newActionCalled = false;
        var oldItem = new IOSSystemContextMenuItemDataCustom(
            title: "Old Action",
            onPressed: () => oldActionCalled = true);
        var newItem = new IOSSystemContextMenuItemDataCustom(
            title: "New Action",
            onPressed: () => newActionCalled = true);

        _ = controller.ShowWithItems(Rect1, [oldItem]);
        Assert.True(controller.IsVisible);
        _ = controller.ShowWithItems(new Rect(100, 100, 100, 100), [newItem]);
        Assert.True(controller.IsVisible);

        Assert.Throws<AssertionError>(() => controller.HandleCustomContextMenuAction(oldItem.CallbackId));
        Assert.False(oldActionCalled);

        controller.HandleCustomContextMenuAction(newItem.CallbackId);
        Assert.True(newActionCalled);
        Assert.False(oldActionCalled);
        controller.Dispose();
    }

    [DebugOnlyFact]
    public void ShowAssertsWithoutATextInputConnection()
    {
        var controller = new SystemContextMenuController();
        AssertionError error = Assert.Throws<AssertionError>(() => { _ = controller.ShowWithItems(Rect1, CopyItems); });
        Assert.Equal(
            "Currently, the system context menu can only be shown for an active text input connection",
            error.Message);
        Assert.False(controller.IsVisible);
        controller.Dispose();
    }

    [Fact]
    public void DeprecatedShowSendsNoItems()
    {
        Attach();
        var controller = new SystemContextMenuController();
#pragma warning disable CS0618 // Dart's deprecated `show` is ported for parity.
        _ = controller.Show(Rect1);
        _ = controller.Show(Rect1);
#pragma warning restore CS0618
        AssertRect(Assert.Single(_targetRects), Rect1);
        Assert.Empty(_itemsReceived);
        Assert.True(controller.IsVisible);
        controller.Dispose();
        Assert.Equal(1, _hideCount);
    }

    // text_input_test.dart

    [DebugOnlyFact]
    public void SystemContextMenuControllerDebugFillProperties()
    {
        IDiagnosticable controller = new SystemContextMenuController(onSystemHide: () => { });
        List<DiagnosticsNode> properties = controller.ToDiagnosticsNode().GetProperties();
        Assert.Equal(4, properties.Count);
        Assert.Equal("isVisible", properties[0].Name);
        Assert.Equal(false, properties[0].Value);
        Assert.Equal("onSystemHide", properties[1].Name);
        Assert.Equal(true, properties[1].Value);
        Assert.Equal("_hiddenBySystem", properties[2].Name);
        Assert.Equal(false, properties[2].Value);
        Assert.Equal("_isDisposed", properties[3].Name);
        Assert.Equal(false, properties[3].Value);
    }

    [DebugOnlyFact]
    public void LookUpDataDebugFillProperties()
    {
        SystemContextMenuDartParityTests.AssertTitleProperty(new IOSSystemContextMenuItemDataLookUp(title: "my title"));
    }

    [DebugOnlyFact]
    public void SearchWebDataDebugFillProperties()
    {
        SystemContextMenuDartParityTests.AssertTitleProperty(
            new IOSSystemContextMenuItemDataSearchWeb(title: "my title"));
    }

    [DebugOnlyFact]
    public void ShareDataDebugFillProperties()
    {
        SystemContextMenuDartParityTests.AssertTitleProperty(new IOSSystemContextMenuItemDataShare(title: "my title"));
    }

    private sealed class FakeTextInputClient(TextEditingValue value) : ITextInputClient
    {
        public TextEditingValue? CurrentTextEditingValue { get; } = value;

        public IAutofillScope? CurrentAutofillScope => null;

        public void UpdateEditingValue(TextEditingValue value)
        {
        }

        public void PerformAction(TextInputActionType action)
        {
        }

        public void PerformPrivateCommand(string action, IDictionary data)
        {
        }

        public void UpdateFloatingCursor(RawFloatingCursorPoint point)
        {
        }

        public void ShowAutocorrectionPromptRect(int start, int end)
        {
        }

        public void ConnectionClosed()
        {
        }
    }
}
