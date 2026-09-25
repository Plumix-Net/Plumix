using Avalonia;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: material_ui/test/text_field_test.dart (the Look Up / Search Web / Share tests
// and the text processing group), cupertino_ui/test/text_field_test.dart (Look Up / Search Web /
// Share), flutter/packages/flutter/test/widgets/process_text_utils.dart (the mock handler), and the
// context-menu members of widgets/editable_text.dart, widgets/selectable_region.dart and
// widgets/text_selection_toolbar_anchors.dart that those tests reach.
public sealed class EditableTextContextMenuDartParityTests : IDisposable
{
    private const string FakeAction1Id = "fakeActivity.fakeAction1";
    private const string FakeAction2Id = "fakeActivity.fakeAction2";
    private const string FakeAction1Label = "Action1";
    private const string FakeAction2Label = "Action2";

    public EditableTextContextMenuDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms => new(Enum.GetValues<TargetPlatform>());

    public static TheoryData<TargetPlatform> IOSAndAndroid => new(TargetPlatform.IOS, TargetPlatform.Android);

    /// <summary>Dart's <c>MockProcessTextHandler</c> (process_text_utils.dart).</summary>
    private sealed class MockProcessTextHandler : IDisposable
    {
        private readonly MockMethodCallHandler _handler;

        public MockProcessTextHandler()
        {
            _handler = new MockMethodCallHandler(SystemChannels.ProcessText, HandleMethodCall);
        }

        public string? LastCalledActionId { get; private set; }

        public string? LastTextToProcess { get; private set; }

        private object? HandleMethodCall(MethodCall call)
        {
            if (call.Method == "ProcessText.queryTextActions")
            {
                if (PlatformDefaults.TargetPlatform == TargetPlatform.Android)
                {
                    return new Dictionary<string, string>
                    {
                        [FakeAction1Id] = FakeAction1Label,
                        [FakeAction2Id] = FakeAction2Label,
                    };
                }

                return null;
            }

            if (call.Method == "ProcessText.processTextAction")
            {
                var args = (System.Collections.IList)call.Arguments!;
                string actionId = (string)args[0]!;
                string textToProcess = (string)args[1]!;
                LastCalledActionId = actionId;
                LastTextToProcess = textToProcess;

                if (actionId == FakeAction1Id)
                {
                    // Simulates an action that returns a transformed text.
                    return textToProcess + "!!!";
                }

                // Simulates an action that failed or does not transform text.
                return null;
            }

            return null;
        }

        public void Dispose() => _handler.Dispose();
    }

    private static FrameworkDartTester PumpMaterial(
        TargetPlatform platform,
        TextEditingController controller,
        bool obscureText = false,
        bool readOnly = false)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Material.Material(
                child: new TextField(controller: controller, obscureText: obscureText, readOnly: readOnly))));
        return tester;
    }

    private static FrameworkDartTester PumpCupertino(TargetPlatform platform, TextEditingController controller)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new CupertinoApp(
            home: new Center(child: new CupertinoTextField(controller: controller))));
        return tester;
    }

    private static RenderEditable FindRenderEditable(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>().RenderEditableObject;

    private static Point TextOffsetToPosition(FrameworkDartTester tester, int offset)
    {
        RenderEditable editable = FindRenderEditable(tester);
        IReadOnlyList<TextSelectionPoint> endpoints =
            editable.GetEndpointsForSelection(TextSelection.Collapsed(offset));
        Assert.Single(endpoints);
        Point global = editable.LocalToGlobal(endpoints[0].Point);
        return new Point(global.X, global.Y - 2.0);
    }

    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    // flutter_test's `longPressAt`: down, wait out the long-press timeout, up.
    private static void LongPressAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromSeconds(1));
        gesture.Up();
    }

    // Long press to put the cursor at `index`, then double tap there to select the word around it.
    private static void SelectWordWithLongPressThenDoubleTap(FrameworkDartTester tester, int index)
    {
        LongPressAt(tester, TextOffsetToPosition(tester, index));
        tester.Pump();

        TapAt(tester, TextOffsetToPosition(tester, index));
        tester.Pump(TimeSpan.FromMilliseconds(50));
        TapAt(tester, TextOffsetToPosition(tester, index));
        tester.PumpAndSettle();
    }

    // text_field_test.dart's `showSelectionMenuAt` followed by `skipPastScrollingAnimation`.
    private static void ShowSelectionMenuAt(FrameworkDartTester tester, TextEditingController controller, int index)
    {
        TapAt(tester, tester.GetCenter(tester.ElementOfType<EditableText>()));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Empty(tester.ElementsWithText("Select all"));

        TapAt(tester, TextOffsetToPosition(tester, index));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        RenderEditable renderEditable = FindRenderEditable(tester);
        Point endpoint = renderEditable.LocalToGlobal(
            renderEditable.GetEndpointsForSelection(controller.Selection)[0].Point);
        // Tapping on the part of the handle's GestureDetector where it overlaps with the text itself
        // does not show the menu, so add a small vertical offset to tap below the text.
        TapAt(tester, endpoint + new Vector(1.0, 13.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));

        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
    }

    private static void TapText(FrameworkDartTester tester, string text) =>
        tester.Tap(Assert.Single(tester.ElementsWithText(text)));

    // ------------------------------------------------------------- Look Up / Search Web / Share

    [Theory]
    [MemberData(nameof(IOSAndAndroid))]
    public void LookUpShowsUpOnIOSOnly(TargetPlatform platform)
    {
        string? lastLookUp = null;
        using var platformChannel = new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method == "LookUp.invoke")
            {
                lastLookUp = Assert.IsType<string>(call.Arguments);
            }

            return null;
        });
        var controller = new TextEditingController("Test ");
        using FrameworkDartTester tester = PumpMaterial(platform, controller);

        bool isTargetPlatformIOS = platform == TargetPlatform.IOS;
        SelectWordWithLongPressThenDoubleTap(tester, 3);

        Assert.Equal(new TextSelection(0, 4), controller.Selection);
        Assert.Equal(isTargetPlatformIOS ? 1 : 0, tester.ElementsWithText("Look Up").Count);

        if (isTargetPlatformIOS)
        {
            TapText(tester, "Look Up");
            Assert.Equal("Test", lastLookUp);
        }
    }

    [Theory]
    [MemberData(nameof(IOSAndAndroid))]
    public void SearchWebShowsUpOnIOSOnly(TargetPlatform platform)
    {
        string? lastSearch = null;
        using var platformChannel = new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method == "SearchWeb.invoke")
            {
                lastSearch = Assert.IsType<string>(call.Arguments);
            }

            return null;
        });
        var controller = new TextEditingController("Test ");
        using FrameworkDartTester tester = PumpMaterial(platform, controller);

        bool isTargetPlatformIOS = platform == TargetPlatform.IOS;
        SelectWordWithLongPressThenDoubleTap(tester, 3);

        Assert.Equal(new TextSelection(0, 4), controller.Selection);
        Assert.Equal(isTargetPlatformIOS ? 1 : 0, tester.ElementsWithText("Search Web").Count);

        if (isTargetPlatformIOS)
        {
            TapText(tester, "Search Web");
            Assert.Equal("Test", lastSearch);
        }
    }

    [Theory]
    [MemberData(nameof(IOSAndAndroid))]
    public void ShareShowsUpOnIOSAndAndroid(TargetPlatform platform)
    {
        string? lastShare = null;
        using var platformChannel = new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method == "Share.invoke")
            {
                lastShare = Assert.IsType<string>(call.Arguments);
            }

            return null;
        });
        var controller = new TextEditingController("Test ");
        using FrameworkDartTester tester = PumpMaterial(platform, controller);

        SelectWordWithLongPressThenDoubleTap(tester, 3);

        Assert.Equal(new TextSelection(0, 4), controller.Selection);
        string shareLabel = platform == TargetPlatform.IOS ? "Share..." : "Share";
        Assert.Single(tester.ElementsWithText(shareLabel));

        TapText(tester, shareLabel);
        Assert.Equal("Test", lastShare);
    }

    [Theory]
    [MemberData(nameof(IOSAndAndroid))]
    public void CupertinoTextFieldOffersLookUpSearchWebAndShareLikeDart(TargetPlatform platform)
    {
        var calls = new List<(string Method, object? Arguments)>();
        using var platformChannel = new MockMethodCallHandler(SystemChannels.Platform, call =>
        {
            if (call.Method is "LookUp.invoke" or "SearchWeb.invoke" or "Share.invoke")
            {
                calls.Add((call.Method, call.Arguments));
            }

            return null;
        });
        var controller = new TextEditingController("Test ");
        using FrameworkDartTester tester = PumpCupertino(platform, controller);

        SelectWordWithLongPressThenDoubleTap(tester, 3);

        Assert.Equal(new TextSelection(0, 4), controller.Selection);
        bool isTargetPlatformIOS = platform == TargetPlatform.IOS;
        Assert.Equal(isTargetPlatformIOS ? 1 : 0, tester.ElementsWithText("Look Up").Count);
        Assert.Equal(isTargetPlatformIOS ? 1 : 0, tester.ElementsWithText("Search Web").Count);
        // CupertinoLocalizations spells it "Share..." on every platform.
        Assert.Single(tester.ElementsWithText("Share..."));

        TapText(tester, "Share...");
        Assert.Equal([("Share.invoke", (object?)"Test")], calls);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LookUpSearchWebAndShareGatesFollowDart(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var controller = new TextEditingController("Test  ");
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Material.Material(child: new TextField(controller: controller))));
        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();

        bool iOS = platform == TargetPlatform.IOS;
        bool share = platform is TargetPlatform.IOS or TargetPlatform.Android;

        controller.Selection = new TextSelection(0, 4);
        Assert.Equal(iOS, state.LookUpEnabled);
        Assert.Equal(iOS, state.SearchWebEnabled);
        Assert.Equal(share, state.ShareEnabled);

        // A collapsed selection, or one of only whitespace, disables all three.
        controller.Selection = TextSelection.Collapsed(2);
        Assert.False(state.LookUpEnabled || state.SearchWebEnabled || state.ShareEnabled);
        controller.Selection = new TextSelection(4, 6);
        Assert.False(state.LookUpEnabled || state.SearchWebEnabled || state.ShareEnabled);

        // The interface defaults are true, as in Dart's `TextSelectionDelegate`.
        ITextSelectionDelegate defaults = new DefaultsDelegate();
        Assert.True(defaults.LookUpEnabled && defaults.SearchWebEnabled && defaults.ShareEnabled);
    }

    [Fact]
    public void LookUpSearchWebAndShareAreNeverOfferedForObscuredText()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        var controller = new TextEditingController("Test Test");
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Material.Material(child: new TextField(controller: controller, obscureText: true))));
        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();

        controller.Selection = new TextSelection(0, 4);
        Assert.False(state.LookUpEnabled || state.SearchWebEnabled || state.ShareEnabled);
        Assert.DoesNotContain(
            state.ContextMenuButtonItems,
            item => item.Type is ContextMenuButtonType.LookUp
                or ContextMenuButtonType.SearchWeb
                or ContextMenuButtonType.Share);
    }

    [Fact]
    public void EditableButtonItemsOrderShareBeforeSelectAllOnAndroidOnly()
    {
        static List<ContextMenuButtonType> Types() => EditableText.GetEditableButtonItems(
            clipboardStatus: ClipboardStatus.Pasteable,
            onCopy: () => { },
            onCut: () => { },
            onPaste: () => { },
            onSelectAll: () => { },
            onLookUp: () => { },
            onSearchWeb: () => { },
            onShare: () => { },
            onLiveTextInput: () => { }).ConvertAll(item => item.Type);

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        Assert.Equal(
            [
                ContextMenuButtonType.Cut, ContextMenuButtonType.Copy, ContextMenuButtonType.Paste,
                ContextMenuButtonType.Share, ContextMenuButtonType.SelectAll, ContextMenuButtonType.LookUp,
                ContextMenuButtonType.SearchWeb, ContextMenuButtonType.LiveTextInput,
            ],
            Types());

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        Assert.Equal(
            [
                ContextMenuButtonType.Cut, ContextMenuButtonType.Copy, ContextMenuButtonType.Paste,
                ContextMenuButtonType.SelectAll, ContextMenuButtonType.LookUp, ContextMenuButtonType.SearchWeb,
                ContextMenuButtonType.Share, ContextMenuButtonType.LiveTextInput,
            ],
            Types());
    }

    private sealed class DefaultsDelegate : ITextSelectionDelegate
    {
        public TextEditingValue TextEditingValue => new();

        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
        }

        public void CutSelection(SelectionChangedCause cause)
        {
        }

        public void CopySelection(SelectionChangedCause cause)
        {
        }

        public void PasteText(SelectionChangedCause cause)
        {
        }

        public void SelectAll(SelectionChangedCause cause)
        {
        }

        public void HideToolbar(bool hideHandles = true)
        {
        }
    }

    // ------------------------------------------------------------- text processing

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void TextProcessingActionsAreAddedToTheToolbar(TargetPlatform platform)
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(platform, controller);

        SelectWordWithLongPressThenDoubleTap(tester, initialText.IndexOf('F', StringComparison.Ordinal));
        Assert.Equal(new TextSelection(7, 14), controller.Selection);

        // The toolbar is visible and the text processing actions are visible on Android.
        bool areTextActionsSupported = platform == TargetPlatform.Android;
        Assert.Single(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
        Assert.Equal(areTextActionsSupported ? 1 : 0, tester.ElementsWithText(FakeAction1Label).Count);
        Assert.Equal(areTextActionsSupported ? 1 : 0, tester.ElementsWithText(FakeAction2Label).Count);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void TextProcessingActionsAreNotAddedToTheToolbarForObscuredText(TargetPlatform platform)
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(platform, controller, obscureText: true);

        SelectWordWithLongPressThenDoubleTap(tester, initialText.IndexOf('F', StringComparison.Ordinal));
        // Obscured text is selected as a whole.
        Assert.Equal(new TextSelection(0, 14), controller.Selection);

        // The toolbar is visible but does not contain the text processing actions.
        Assert.Single(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
        Assert.Empty(tester.ElementsWithText(FakeAction1Label));
        Assert.Empty(tester.ElementsWithText(FakeAction2Label));
    }

    [Fact]
    public void TextProcessingActionsAreNotAddedToTheToolbarIfSelectionIsCollapsed()
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(TargetPlatform.Android, controller);

        // Open the text selection toolbar.
        ShowSelectionMenuAt(tester, controller, initialText.IndexOf('F', StringComparison.Ordinal));

        // The toolbar is visible but does not contain the text processing actions.
        Assert.Single(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
        Assert.True(controller.Selection.IsCollapsed);

        Assert.Empty(tester.ElementsWithText(FakeAction1Label));
        Assert.Empty(tester.ElementsWithText(FakeAction2Label));
    }

    [Fact]
    public void InvokeATextProcessingActionThatDoesNotReturnAValue()
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(TargetPlatform.Android, controller);

        SelectWordWithLongPressThenDoubleTap(tester, initialText.IndexOf('F', StringComparison.Ordinal));
        Assert.Equal(new TextSelection(7, 14), controller.Selection);

        TapText(tester, FakeAction2Label);
        tester.Pump(TimeSpan.FromMilliseconds(200));

        // This action does not return a value, so the selection is not replaced.
        Assert.Equal(FakeAction2Id, mockProcessTextHandler.LastCalledActionId);
        Assert.Equal("Flutter", mockProcessTextHandler.LastTextToProcess);
        Assert.Equal(initialText, controller.Text);

        // The toolbar is no longer visible.
        Assert.Empty(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void InvokingATextProcessingActionThatReturnsAValueReplacesTheSelection()
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(TargetPlatform.Android, controller);

        SelectWordWithLongPressThenDoubleTap(tester, initialText.IndexOf('F', StringComparison.Ordinal));
        Assert.Equal(new TextSelection(7, 14), controller.Selection);

        TapText(tester, FakeAction1Label);
        tester.Pump(TimeSpan.FromMilliseconds(200));

        Assert.Equal(FakeAction1Id, mockProcessTextHandler.LastCalledActionId);
        Assert.Equal("Flutter", mockProcessTextHandler.LastTextToProcess);
        // This action returns a transformed text which replaces the selection, and the caret lands
        // after it.
        Assert.Equal("I love Flutter!!!", controller.Text);
        Assert.Equal(TextSelection.Collapsed(17), controller.Selection);

        // The toolbar is no longer visible.
        Assert.Empty(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void ATextProcessingActionResultDoesNotReplaceTheSelectionOfAReadOnlyField()
    {
        const string initialText = "I love Flutter";
        var controller = new TextEditingController(initialText);
        using var mockProcessTextHandler = new MockProcessTextHandler();
        using FrameworkDartTester tester = PumpMaterial(TargetPlatform.Android, controller, readOnly: true);

        SelectWordWithLongPressThenDoubleTap(tester, initialText.IndexOf('F', StringComparison.Ordinal));
        Assert.Equal(new TextSelection(7, 14), controller.Selection);

        TapText(tester, FakeAction1Label);
        tester.Pump(TimeSpan.FromMilliseconds(200));

        // The action returns a value, but the field is read-only.
        Assert.Equal(FakeAction1Id, mockProcessTextHandler.LastCalledActionId);
        Assert.Equal("Flutter", mockProcessTextHandler.LastTextToProcess);
        Assert.Equal(initialText, controller.Text);

        // The toolbar is no longer visible.
        Assert.Empty(tester.ElementsOfType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void TextProcessingItemsFollowTheDeprecatedToolbarOptionsItemsAndSendReadOnly()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var processText = new MockMethodCallHandler(SystemChannels.ProcessText, call =>
            call.Method == "ProcessText.queryTextActions"
                ? new Dictionary<string, string> { [FakeAction1Id] = FakeAction1Label }
                : null);
        var controller = new TextEditingController("alpha beta", new TextSelection(0, 5));
        var key = new LabeledGlobalKey<EditableText.EditableTextState>("processing");
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
#pragma warning disable CS0618 // Dart's cascade appends the processing items to the deprecated list too.
        tester.PumpWidget(new TestWidgetsApp(home: new EditableText(
            key: key,
            controller: controller,
            focusNode: new FocusNode(),
            style: new TextStyle(FontSize: 10.0),
            cursorColor: Colors.Blue,
            backgroundCursorColor: Colors.Black,
            readOnly: true,
            toolbarOptions: new ToolbarOptions(Copy: true))));
#pragma warning restore CS0618
        EditableText.EditableTextState state = key.CurrentState!;

        IReadOnlyList<ContextMenuButtonItem> items = state.ContextMenuButtonItems;
        Assert.Equal([ContextMenuButtonType.Copy, ContextMenuButtonType.Custom], items.Select(item => item.Type));
        Assert.Equal(FakeAction1Label, items[1].Label);

        items[1].OnPressed!();
        tester.Pump();
        MethodCall call = processText.Log[^1];
        Assert.Equal("ProcessText.processTextAction", call.Method);
        Assert.Equal(
            new object?[] { FakeAction1Id, "alpha", true },
            ((System.Collections.IEnumerable)call.Arguments!).Cast<object?>());
        Assert.Equal("alpha beta", controller.Text);
    }

    // ------------------------------------------------------------- anchors and glyph heights

    [Fact]
    public void ContextMenuAnchorsComeFromTheSelectionInGlobalCoordinates()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        var controller = new TextEditingController("alpha beta", new TextSelection(6, 10));
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new TestWidgetsApp(home: new Padding(
            EdgeInsetsGeometry.FromLTRB(30, 40, 0, 0),
            child: new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(
                    width: 300,
                    child: new EditableText(
                        controller: controller,
                        focusNode: new FocusNode(),
                        style: new TextStyle(FontSize: 10.0),
                        cursorColor: Colors.Blue,
                        backgroundCursorColor: Colors.Black))))));
        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();
        RenderEditable renderEditable = state.RenderEditableObject;

        (double startGlyphHeight, double endGlyphHeight) = state.GetGlyphHeights();
        Assert.Equal(10.0, startGlyphHeight);
        Assert.Equal(10.0, endGlyphHeight);

        IReadOnlyList<TextSelectionPoint> points = renderEditable.GetEndpointsForSelection(controller.Selection);
        Point origin = renderEditable.LocalToGlobal(default);
        TextSelectionToolbarAnchors anchors = state.ContextMenuAnchors;
        double centerX = origin.X + (points[0].Point.X + points[^1].Point.X) / 2;
        Assert.Equal(centerX, anchors.PrimaryAnchor.X, 6);
        Assert.Equal(origin.Y + points[0].Point.Y - startGlyphHeight, anchors.PrimaryAnchor.Y, 6);
        Assert.Equal(centerX, anchors.SecondaryAnchor!.Value.X, 6);
        Assert.Equal(origin.Y + points[^1].Point.Y, anchors.SecondaryAnchor!.Value.Y, 6);

        // A collapsed selection falls back to the preferred line height.
        controller.Selection = TextSelection.Collapsed(3);
        Assert.Equal((renderEditable.PreferredLineHeight, renderEditable.PreferredLineHeight), state.GetGlyphHeights());
    }

    [Fact]
    public void ContextMenuAnchorsPreferTheLastSecondaryTapDownPosition()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        var controller = new TextEditingController("alpha beta");
        using FrameworkDartTester tester = PumpMaterial(TargetPlatform.MacOS, controller);
        Point location = TextOffsetToPosition(tester, 2);

        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Mouse, PointerButtons.Secondary);
        gesture.Up();
        tester.PumpAndSettle();

        TextSelectionToolbarAnchors anchors = tester.State<EditableText.EditableTextState>().ContextMenuAnchors;
        Assert.Equal(location, anchors.PrimaryAnchor);
        Assert.Null(anchors.SecondaryAnchor);
    }

    // ------------------------------------------------------------- SelectableRegion

    private static FrameworkDartTester PumpSelectionArea(
        TargetPlatform platform,
        LabeledGlobalKey<SelectionAreaState> key)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new SelectionArea(key: key, child: new Text("share me"))));
        return tester;
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void SelectableRegionOffersShareOnAndroidOnlyAndClearsTheSelection(TargetPlatform platform)
    {
        using var platformChannel = new MockMethodCallHandler(SystemChannels.Platform);
        var key = new LabeledGlobalKey<SelectionAreaState>("share-area");
        using FrameworkDartTester tester = PumpSelectionArea(platform, key);
        SelectableRegionState state = key.CurrentState!.SelectableRegion;

        state.SelectAll();
        tester.Pump();
        ContextMenuButtonItem? share = state.ContextMenuButtonItems
            .SingleOrDefault(item => item.Type == ContextMenuButtonType.Share);
        if (platform != TargetPlatform.Android || Foundation.Constants.KIsWeb)
        {
            Assert.Null(share);
            return;
        }

        share!.OnPressed!();
        tester.Pump();
        MethodCall call = Assert.Single(platformChannel.Log, c => c.Method == "Share.invoke");
        Assert.Equal("share me", call.Arguments);
        Assert.Null(state.SelectedContent);
    }

    [Fact]
    public void SelectableRegionAppendsTextProcessingActionsAndSendsReadOnly()
    {
        using var mockProcessTextHandler = new MockProcessTextHandler();
        var key = new LabeledGlobalKey<SelectionAreaState>("process-area");
        using FrameworkDartTester tester = PumpSelectionArea(TargetPlatform.Android, key);
        SelectableRegionState state = key.CurrentState!.SelectableRegion;

        // Nothing selected: no content, so no text processing items.
        Assert.DoesNotContain(state.ContextMenuButtonItems, item => item.Type == ContextMenuButtonType.Custom);

        state.SelectAll();
        tester.Pump();
        IReadOnlyList<ContextMenuButtonItem> items = state.ContextMenuButtonItems;
        Assert.Equal(
            [
                ContextMenuButtonType.Copy, ContextMenuButtonType.Share, ContextMenuButtonType.SelectAll,
                ContextMenuButtonType.Custom, ContextMenuButtonType.Custom,
            ],
            items.Select(item => item.Type));
        Assert.Equal([FakeAction1Label, FakeAction2Label], items.Skip(3).Select(item => item.Label));

        items[3].OnPressed!();
        tester.Pump();
        Assert.Equal(FakeAction1Id, mockProcessTextHandler.LastCalledActionId);
        Assert.Equal("share me", mockProcessTextHandler.LastTextToProcess);
        Assert.False(state.ContextMenuIsVisible);
    }
}
