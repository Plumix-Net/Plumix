using System.Collections;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: flutter/packages/flutter/test/widgets/system_context_menu_test.dart and
// flutter/packages/flutter/test/system_context_menu_utils.dart (`systemContextMenuItemDataFromJson`).
// The web-only test ('asserts when built on web') has no counterpart: Plumix's tests never run on web.
public sealed class SystemContextMenuDartParityTests : IDisposable
{
    public SystemContextMenuDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms => new(Enum.GetValues<TargetPlatform>());

    /// <summary>Dart's <c>_buildSystemContextMenuTestApp</c>.</summary>
    private static Widget BuildApp(Widget child, bool supportsShowingSystemContextMenu = false)
    {
        return new TestWidgetsApp(
            home: child,
            builder: (context, appChild) => new MediaQuery(
                data: (MediaQuery.MaybeOf(context) ?? MediaQueryData.FromView(View.Of(context)))
                    .CopyWith(supportsShowingSystemContextMenu: supportsShowingSystemContextMenu),
                child: appChild ?? SizedBox.Shrink()));
    }

    private static Widget DefaultMenu(BuildContext context, EditableText.EditableTextState state) =>
        SystemContextMenu.EditableText(state);

    private static void ShowMenu(FrameworkDartTester tester)
    {
        tester.Tap(Find.ByType<TestTextField>());
        Assert.True(tester.State<EditableText.EditableTextState>().ShowToolbar());
        tester.Pump();
    }

    /// <summary>flutter_test's <c>handlePlatformMessage('flutter/platform', ...)</c>.</summary>
    internal static void SendPlatformCall(string method, object? arguments = null)
    {
        _ = ServicesBinding.Instance.DefaultBinaryMessenger.HandlePlatformMessage(
            SystemChannels.Platform.Name,
            SystemChannels.Platform.Codec.EncodeMethodCall(new MethodCall(method, arguments)),
            _ => { });
    }

    internal static void SendDismiss() => SendPlatformCall("ContextMenu.onDismissSystemContextMenu");

    internal static void SendCustomAction(string callbackId) =>
        SendPlatformCall("ContextMenu.onPerformCustomAction", new List<object?> { 0, callbackId });

    /// <summary>Dart's <c>systemContextMenuItemDataFromJson</c> (system_context_menu_utils.dart).</summary>
    internal static IOSSystemContextMenuItemData ItemDataFromJson(IDictionary json)
    {
        string? type = json["type"] as string;
        string? title = json.Contains("title") ? json["title"] as string : null;
        return type switch
        {
            "copy" => new IOSSystemContextMenuItemDataCopy(),
            "cut" => new IOSSystemContextMenuItemDataCut(),
            "paste" => new IOSSystemContextMenuItemDataPaste(),
            "selectAll" => new IOSSystemContextMenuItemDataSelectAll(),
            "searchWeb" => new IOSSystemContextMenuItemDataSearchWeb(title: title!),
            "share" => new IOSSystemContextMenuItemDataShare(title: title!),
            "lookUp" => new IOSSystemContextMenuItemDataLookUp(title: title!),
            "captureTextFromCamera" => new IOSSystemContextMenuItemDataLiveText(),
            "custom" => new IOSSystemContextMenuItemDataCustom(title: title!, onPressed: () => { }),
            _ => throw new FlutterError($"Invalid json for IOSSystemContextMenuItem.type {type}."),
        };
    }

    /// <summary>Records the items of every <c>ContextMenu.showSystemContextMenu</c> call.</summary>
    private static MockMethodCallHandler RecordItems(List<List<IOSSystemContextMenuItemData>> itemsReceived)
    {
        return new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method == "ContextMenu.showSystemContextMenu")
            {
                var arguments = (IDictionary)call.Arguments!;
                var items = (IList)arguments["items"]!;
                itemsReceived.Add(items.Cast<IDictionary>().Select(ItemDataFromJson).ToList());
            }

            return null;
        });
    }

    [DebugOnlyTheory]
    [MemberData(nameof(AllPlatforms))]
    public void AssertsWhenBuiltOnAnUnsupportedDevice(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu)));

        ShowMenu(tester);

        Assert.IsType<AssertionError>(tester.TakeException());
    }

    [Fact]
    public void WhenSupportsShowingSystemContextMenuIsFalseIsSupportedIsFalse()
    {
        using var tester = new FrameworkDartTester();
        BuildContext? buildContext = null;
        tester.PumpWidget(BuildApp(new Builder(context =>
        {
            buildContext = context;
            return SizedBox.Shrink();
        })));

        Assert.False(SystemContextMenu.IsSupported(buildContext!));
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void WhenSupportedAndThePlatformIsIOSIsSupportedIsTrue(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester();
        BuildContext? buildContext = null;
        tester.PumpWidget(BuildApp(
            new Builder(context =>
            {
                buildContext = context;
                return SizedBox.Shrink();
            }),
            supportsShowingSystemContextMenu: true));

        Assert.Equal(platform == TargetPlatform.IOS, SystemContextMenu.IsSupported(buildContext!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadOnlyFieldsDoNotSupportTheSystemContextMenu(bool readOnly)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(controller: controller, readOnly: readOnly, contextMenuBuilder: DefaultMenu),
            supportsShowingSystemContextMenu: true));

        var state = tester.State<EditableText.EditableTextState>();
        Assert.Equal(!readOnly, SystemContextMenu.IsSupportedByField(state));
    }

    [DebugOnlyFact]
    public void WhenSupportsShowingSystemContextMenuIsFalseSystemContextMenuThrows()
    {
        // Regression test for https://github.com/flutter/flutter/issues/170521. Dart runs this one on
        // the default test platform, which is Android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu)));

        ShowMenu(tester);

        Assert.IsType<AssertionError>(tester.TakeException());
    }

    [Fact]
    public void CanBeShownAndHiddenLikeANormalContextMenu()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu),
            supportsShowingSystemContextMenu: true));

        Finds.Nothing(Find.ByType<SystemContextMenu>());
        ShowMenu(tester);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());

        tester.State<EditableText.EditableTextState>().HideToolbar();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void CanCustomizeTheMenuItems()
    {
        var itemsReceived = new List<List<IOSSystemContextMenuItemData>>();
        using MockMethodCallHandler platform = RecordItems(itemsReceived);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        IReadOnlyList<IOSSystemContextMenuItem> items =
        [
            new IOSSystemContextMenuItemCopy(),
            new IOSSystemContextMenuItemShare(title: "My Share Title"),
            new IOSSystemContextMenuItemLiveText(),
        ];
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: items)),
            supportsShowingSystemContextMenu: true));

        Finds.Nothing(Find.ByType<SystemContextMenu>());
        Assert.Empty(itemsReceived);

        ShowMenu(tester);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        List<IOSSystemContextMenuItemData> received = Assert.Single(itemsReceived);
        Assert.Equal(3, received.Count);
        Assert.Equal(new IOSSystemContextMenuItemDataCopy(), received[0]);
        Assert.Equal(new IOSSystemContextMenuItemDataShare(title: "My Share Title"), received[1]);
        Assert.Equal(new IOSSystemContextMenuItemDataLiveText(), received[2]);

        tester.State<EditableText.EditableTextState>().HideToolbar();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void PassingEmptyItemsBuildsTheWidgetButDoesNotShowTheSystemContextMenu()
    {
        var itemsReceived = new List<List<IOSSystemContextMenuItemData>>();
        using MockMethodCallHandler platform = RecordItems(itemsReceived);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: [])),
            supportsShowingSystemContextMenu: true));
        Assert.Null(tester.TakeException());

        Finds.Nothing(Find.ByType<SystemContextMenu>());
        Assert.Empty(itemsReceived);

        tester.Tap(Find.ByType<TestTextField>());
        Assert.True(tester.State<EditableText.EditableTextState>().ShowToolbar());
        Assert.Null(tester.TakeException());
        tester.Pump();
        Assert.Null(tester.TakeException());

        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        Assert.Empty(itemsReceived);
    }

    [Fact]
    public void ItemsReceiveADefaultTitle()
    {
        var itemsReceived = new List<List<IOSSystemContextMenuItemData>>();
        using MockMethodCallHandler platform = RecordItems(itemsReceived);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        IReadOnlyList<IOSSystemContextMenuItem> items =
        [
            new IOSSystemContextMenuItemCopy(),
            new IOSSystemContextMenuItemShare(),
        ];
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: items)),
            supportsShowingSystemContextMenu: true));

        ShowMenu(tester);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        List<IOSSystemContextMenuItemData> received = Assert.Single(itemsReceived);
        Assert.Equal(2, received.Count);
        Assert.Equal(new IOSSystemContextMenuItemDataCopy(), received[0]);
        Assert.Equal(
            new IOSSystemContextMenuItemDataShare(title: new DefaultWidgetsLocalizations().ShareButtonLabel),
            received[1]);

        tester.State<EditableText.EditableTextState>().HideToolbar();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void CanBeUpdated()
    {
        var targetRects = new List<IDictionary>();
        using var platform = new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method == "ContextMenu.showSystemContextMenu")
            {
                targetRects.Add((IDictionary)((IDictionary)call.Arguments!)["targetRect"]!);
            }

            return null;
        });
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(BuildApp(
            new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu),
            supportsShowingSystemContextMenu: true));

        Assert.Empty(targetRects);

        ShowMenu(tester);
        IDictionary first = Assert.Single(targetRects);
        Assert.Equal(0.0, Assert.IsType<double>(first["width"]));

        controller.Selection = new TextSelection(4, 7);
        tester.PumpAndSettle();

        Assert.Equal(2, targetRects.Count);
        Assert.True(Convert.ToDouble(targetRects[^1]["width"]) > 0.0);
    }

    [Fact]
    public void CanBeRebuilt()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("one two three");
        StateSetter? setState = null;
        tester.PumpWidget(BuildApp(
            new StatefulBuilder((context, localSetState) =>
            {
                setState = localSetState;
                return new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu);
            }),
            supportsShowingSystemContextMenu: true));

        ShowMenu(tester);

        setState!(() => { });
        tester.PumpAndSettle();

        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void CanHandleMultipleInstances()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller1 = new TextEditingController("one two three");
        var controller2 = new TextEditingController("four five six");
        var field1Key = new LabeledGlobalKey<State>("field1");
        var field2Key = new LabeledGlobalKey<State>("field2");
        var menu1Key = new LabeledGlobalKey<State>("menu1");
        var menu2Key = new LabeledGlobalKey<State>("menu2");
        tester.PumpWidget(BuildApp(
            new Column(children:
            [
                new TestTextField(
                    key: field1Key,
                    controller: controller1,
                    contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, key: menu1Key)),
                new TestTextField(
                    key: field2Key,
                    controller: controller2,
                    contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, key: menu2Key)),
            ]),
            supportsShowingSystemContextMenu: true));

        Finds.Nothing(Find.ByType<SystemContextMenu>());

        tester.Tap(Find.ByKey(field1Key));
        EditableText.EditableTextState state1 = tester.State<EditableText.EditableTextState>(
            Find.Descendant(Find.ByKey(field1Key), Find.ByType<EditableText>()));
        Assert.True(state1.ShowToolbar());
        tester.Pump();
        Finds.OneWidget(Find.ByKey(menu1Key));
        Finds.Nothing(Find.ByKey(menu2Key));

        // In a real app, this message is sent by iOS when the user taps anywhere outside of the
        // system context menu.
        SendDismiss();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());

        tester.Tap(Find.ByKey(field2Key));
        EditableText.EditableTextState state2 = tester.State<EditableText.EditableTextState>(
            Find.Descendant(Find.ByKey(field2Key), Find.ByType<EditableText>()));
        Assert.True(state2.ShowToolbar());
        tester.Pump();
        Finds.Nothing(Find.ByKey(menu1Key));
        Finds.OneWidget(Find.ByKey(menu2Key));
    }

    [Fact]
    public void CanGetTheDataRepresentationOfEveryItem()
    {
        WidgetsLocalizations localizations = new DefaultWidgetsLocalizations();
        Assert.Equal(new IOSSystemContextMenuItemDataCopy(), new IOSSystemContextMenuItemCopy().GetData(localizations));
        Assert.Equal(new IOSSystemContextMenuItemDataCut(), new IOSSystemContextMenuItemCut().GetData(localizations));
        Assert.Equal(
            new IOSSystemContextMenuItemDataPaste(),
            new IOSSystemContextMenuItemPaste().GetData(localizations));
        Assert.Equal(
            new IOSSystemContextMenuItemDataSelectAll(),
            new IOSSystemContextMenuItemSelectAll().GetData(localizations));
        Assert.Equal(
            new IOSSystemContextMenuItemDataLookUp(title: localizations.LookUpButtonLabel),
            new IOSSystemContextMenuItemLookUp().GetData(localizations));
        Assert.Equal(
            new IOSSystemContextMenuItemDataSearchWeb(title: localizations.SearchWebButtonLabel),
            new IOSSystemContextMenuItemSearchWeb().GetData(localizations));
        Assert.Equal(
            new IOSSystemContextMenuItemDataShare(title: localizations.ShareButtonLabel),
            new IOSSystemContextMenuItemShare().GetData(localizations));
        Assert.IsType<IOSSystemContextMenuItemDataLiveText>(
            new IOSSystemContextMenuItemLiveText().GetData(localizations));
    }

    [Fact]
    public void ItemDataEqualityComparesTypeAndTitle()
    {
        Assert.NotEqual<IOSSystemContextMenuItemData>(
            new IOSSystemContextMenuItemDataCopy(),
            new IOSSystemContextMenuItemDataCut());
        Assert.NotEqual(
            new IOSSystemContextMenuItemDataShare(title: "a"),
            new IOSSystemContextMenuItemDataShare(title: "b"));
        Assert.NotEqual<IOSSystemContextMenuItem>(
            new IOSSystemContextMenuItemCopy(),
            new IOSSystemContextMenuItemCut());
        Assert.Equal(new IOSSystemContextMenuItemShare(title: "a"), new IOSSystemContextMenuItemShare(title: "a"));
    }

    [Fact]
    public void ItemDataFromJsonHandlesLiveText()
    {
        var json = new Dictionary<string, object?> { ["type"] = "captureTextFromCamera" };
        Assert.IsType<IOSSystemContextMenuItemDataLiveText>(ItemDataFromJson(json));
    }

    [Fact]
    public void ItemDataJsonMatchesTheChannelFormat()
    {
        Assert.Equal(
            new Dictionary<string, object?> { ["callbackId"] = 0, ["type"] = "copy" },
            new IOSSystemContextMenuItemDataCopy().Json);
        Assert.Equal("captureTextFromCamera", new IOSSystemContextMenuItemDataLiveText().Json["type"]);
        var share = new IOSSystemContextMenuItemDataShare(title: "Share");
        Assert.Equal(new[] { "callbackId", "title", "type" }, share.Json.Keys);
        Assert.Equal(share.GetHashCode(), share.Json["callbackId"]);
        Action onPressed = () => { };
        var custom = new IOSSystemContextMenuItemDataCustom(title: "Custom", onPressed: onPressed);
        Assert.Equal(
            new Dictionary<string, object?> { ["id"] = custom.CallbackId, ["title"] = "Custom", ["type"] = "custom" },
            custom.Json);
        Assert.Equal(
            new IOSSystemContextMenuItemCustom(title: "Custom", onPressed: onPressed).GetHashCode().ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            custom.CallbackId);
    }

    [DebugOnlyFact]
    public void LookUpDebugFillProperties()
    {
        // Regression test for https://github.com/flutter/flutter/issues/169696.
        AssertTitleProperty(new IOSSystemContextMenuItemLookUp(title: "my title"));
    }

    [DebugOnlyFact]
    public void SearchWebDebugFillProperties()
    {
        AssertTitleProperty(new IOSSystemContextMenuItemSearchWeb(title: "my title"));
    }

    [DebugOnlyFact]
    public void ShareDebugFillProperties()
    {
        AssertTitleProperty(new IOSSystemContextMenuItemShare(title: "my title"));
    }

    internal static void AssertTitleProperty(IDiagnosticable item)
    {
        List<DiagnosticsNode> properties = item.ToDiagnosticsNode().GetProperties();
        DiagnosticsNode property = Assert.Single(properties);
        Assert.Equal("title", property.Name);
        Assert.Equal("my title", property.Value);
    }

    [Fact]
    public void CanUseCustomMenuItems()
    {
        var itemsReceived = new List<List<IOSSystemContextMenuItemData>>();
        using MockMethodCallHandler platform = RecordItems(itemsReceived);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        bool customAction1Called = false;
        bool customAction2Called = false;
        var customItem1 = new IOSSystemContextMenuItemCustom(
            title: "Custom Action 1",
            onPressed: () => customAction1Called = true);
        var customItem2 = new IOSSystemContextMenuItemCustom(
            title: "Custom Action 2",
            onPressed: () => customAction2Called = true);
        IReadOnlyList<IOSSystemContextMenuItem> items = [new IOSSystemContextMenuItemCopy(), customItem1, customItem2];
        var controller = new TextEditingController("test text");
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: items)),
            supportsShowingSystemContextMenu: true));

        ShowMenu(tester);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        List<IOSSystemContextMenuItemData> received = Assert.Single(itemsReceived);
        Assert.Equal(3, received.Count);
        Assert.Equal(new IOSSystemContextMenuItemDataCopy(), received[0]);
        Assert.Equal("Custom Action 1", Assert.IsType<IOSSystemContextMenuItemDataCustom>(received[1]).Title);
        Assert.Equal("Custom Action 2", Assert.IsType<IOSSystemContextMenuItemDataCustom>(received[2]).Title);

        SendCustomAction(CallbackIdOf(customItem1));
        Assert.True(customAction1Called);
        Assert.False(customAction2Called);

        SendCustomAction(CallbackIdOf(customItem2));
        Assert.True(customAction2Called);

        tester.State<EditableText.EditableTextState>().HideToolbar();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    private static string CallbackIdOf(IOSSystemContextMenuItem item) =>
        item.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void SystemContextMenuAutoClosesAfterCustomAction()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        bool customActionCalled = false;
        var controller = new TextEditingController("test text");
        IReadOnlyList<IOSSystemContextMenuItem> items =
        [
            new IOSSystemContextMenuItemCustom(title: "Test Action", onPressed: () => customActionCalled = true),
        ];
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: items)),
            supportsShowingSystemContextMenu: true));

        ShowMenu(tester);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());

        var menu = tester.Widget<SystemContextMenu>(Find.ByType<SystemContextMenu>());
        SendCustomAction(CallbackIdOf(menu.Items[0]));
        Assert.True(customActionCalled);

        SendDismiss();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void CanTriggerCustomMenuActionThroughPlatformChannelMessage()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        bool customActionCalled = false;
        var controller = new TextEditingController("test text");
        IReadOnlyList<IOSSystemContextMenuItem> items =
        [
            new IOSSystemContextMenuItemCut(),
            new IOSSystemContextMenuItemCustom(title: "Test Action", onPressed: () => customActionCalled = true),
        ];
        tester.PumpWidget(BuildApp(
            new TestTextField(
                controller: controller,
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: items)),
            supportsShowingSystemContextMenu: true));

        tester.Tap(Find.ByType<TestTextField>());
        tester.PumpAndSettle();
        controller.Selection = new TextSelection(0, 3);
        tester.LongPress(Find.ByType<TestTextField>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByType<SystemContextMenu>());

        SendCustomAction(CallbackIdOf(items[1]));
        Assert.True(customActionCalled);

        SendDismiss();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void TwoTextFieldsCanHaveDifferentCustomSystemContextMenuItems()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        bool field1ActionCalled = false;
        bool field2ActionCalled = false;
        var item1 = new IOSSystemContextMenuItemCustom(
            title: "Field 1 Action",
            onPressed: () => field1ActionCalled = true);
        var item2 = new IOSSystemContextMenuItemCustom(
            title: "Field 2 Action",
            onPressed: () => field2ActionCalled = true);
        tester.PumpWidget(BuildApp(
            new Column(children:
            [
                new TestTextField(
                    controller: new TextEditingController("Field 1 text"),
                    contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: [item1])),
                new TestTextField(
                    controller: new TextEditingController("Field 2 text"),
                    contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state, items: [item2])),
            ]),
            supportsShowingSystemContextMenu: true));

        tester.LongPress(Find.ByType<TestTextField>().First);
        tester.Pump();
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        var menu1 = tester.Widget<SystemContextMenu>(Find.ByType<SystemContextMenu>());
        SendCustomAction(CallbackIdOf(menu1.Items[0]));
        Assert.True(field1ActionCalled);
        Assert.False(field2ActionCalled);

        SendDismiss();
        tester.Pump();
        field1ActionCalled = false;

        tester.LongPress(Find.ByType<TestTextField>().Last);
        tester.Pump();
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
        SendCustomAction(CallbackIdOf(item2));
        Assert.False(field1ActionCalled);
        Assert.True(field2ActionCalled);

        SendDismiss();
        tester.Pump();
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void DefaultIOSSystemContextMenuIncludesShareForNonEmptySelection()
    {
        var itemsReceived = new List<List<IOSSystemContextMenuItemData>>();
        using MockMethodCallHandler platform = RecordItems(itemsReceived);
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var controller = new TextEditingController("Hello world");
        tester.PumpWidget(BuildApp(
            new TestTextField(controller: controller, contextMenuBuilder: DefaultMenu),
            supportsShowingSystemContextMenu: true));

        tester.Tap(Find.ByType<TestTextField>());
        tester.Pump();
        controller.Selection = new TextSelection(0, 5);
        tester.Pump();
        Assert.Empty(itemsReceived);

        Assert.True(tester.State<EditableText.EditableTextState>().ShowToolbar());
        tester.Pump();

        Assert.NotEmpty(itemsReceived);
        Assert.Contains(itemsReceived[^1], item => item is IOSSystemContextMenuItemDataShare);
    }
}
