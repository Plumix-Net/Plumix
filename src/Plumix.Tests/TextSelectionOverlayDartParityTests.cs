using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/text_selection_test.dart (the
// SelectionOverlay / TextSelectionOverlay groups) and the selection-overlay, toolbar, Live Text,
// web clipboard and BrowserContextMenu tests of
// flutter/packages/flutter/test/widgets/editable_text_test.dart.
public sealed class TextSelectionOverlayDartParityTests : IDisposable
{
    private const string MultilineText = "aaaa aaaa\nbbbb bbbb\ncccc cccc";

    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new(debugLabel: "EditableText Node");
    private readonly FlutterExceptionHandler? _previousOnError = FlutterError.OnError;
    private readonly List<FlutterErrorDetails> _errors = [];

    public TextSelectionOverlayDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        FlutterError.OnError = _errors.Add;
    }

    public void Dispose()
    {
        FlutterError.OnError = _previousOnError;
        UI.TextInput.RestorePlatformInputControl();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        PlatformDefaults.DebugIsWebOverride = null;
        BrowserContextMenu.ResetForTests();
        ContextMenuController.RemoveAny();
        FocusManager.Instance.ResetForTests();
    }

    private EditableText Field(
        TextSelectionControls? selectionControls = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        bool showSelectionHandles = true,
        Action? onSelectionHandleTapped = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool obscureText = false,
        bool readOnly = false,
        ToolbarOptions? toolbarOptions = null,
        bool multiline = true) =>
        new(
            controller: _controller,
            focusNode: _focusNode,
            style: new TextStyle(FontSize: 10.0),
            padding: new Thickness(0),
            cursorColor: new Color(0xFF2196F3),
            backgroundCursorColor: new Color(0xFF9E9E9E),
            multiline: multiline,
            maxLines: multiline ? null : 1,
            obscureText: obscureText,
            readOnly: readOnly,
            toolbarOptions: toolbarOptions,
            selectionControls: selectionControls ?? TestTextSelectionHandleControls.Instance,
            contextMenuBuilder: contextMenuBuilder ?? ((_, _) => new SizedBox(width: 10, height: 10)),
            showSelectionHandles: showSelectionHandles,
            onSelectionHandleTapped: onSelectionHandleTapped,
            dragStartBehavior: dragStartBehavior);

    private static Widget Wrap(Widget field, TextScaler? textScaler = null) => new TestWidgetsApp(
        home: new MediaQuery(
            new MediaQueryData(Size: new Size(800, 600), TextScaler: textScaler ?? TextScaler.NoScaling),
            new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(width: 400, height: 300, child: field))));

    private FrameworkDartTester Pump(Widget field, TargetPlatform platform = TargetPlatform.Android, string? text = null)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        _controller.Text = text ?? MultilineText;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(field));
        tester.Pump();
        return tester;
    }

    private static EditableText.EditableTextState State(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>();

    private static RenderEditable Editable(FrameworkDartTester tester) => State(tester).RenderEditableObject;

    // A user selection change: creates the selection overlay and shows the handles.
    private void Select(FrameworkDartTester tester, int baseOffset, int extentOffset)
    {
        State(tester).UserUpdateTextEditingValue(
            new TextEditingValue(_controller.Text, new TextSelection(baseOffset, extentOffset)),
            SelectionChangedCause.Tap);
        tester.Pump();
    }

    private static SelectionHandleOverlay Handle(FrameworkDartTester tester, bool end)
    {
        SelectionOverlay overlay = State(tester).SelectionOverlay!.SelectionOverlay;
        LayerLink link = end ? overlay.EndHandleLayerLink : overlay.StartHandleLayerLink;
        return tester.AllElements()
            .Select(element => element.Widget)
            .OfType<SelectionHandleOverlay>()
            .Single(handle => ReferenceEquals(handle.HandleLayerLink, link));
    }

    private static Point GlobalEndpoint(FrameworkDartTester tester, bool end)
    {
        IReadOnlyList<TextSelectionPoint> endpoints = State(tester).SelectionOverlay!.SelectionOverlay.SelectionEndpoints;
        return Editable(tester).LocalToGlobal(end ? endpoints[^1].Point : endpoints[0].Point);
    }

    private static Point GlobalCaret(FrameworkDartTester tester, int offset)
    {
        RenderEditable editable = Editable(tester);
        Rect caret = editable.GetLocalRectForCaret(new TextPosition(offset));
        return editable.LocalToGlobal(caret.Center);
    }

    private static DragStartDetails Start(Point position) =>
        new(GlobalPosition: position, LocalPosition: position, Kind: PointerDeviceKind.Touch);

    private static DragUpdateDetails Update(Point position) =>
        new(
            GlobalPosition: position,
            LocalPosition: position,
            Delta: default,
            PrimaryDelta: null,
            Kind: PointerDeviceKind.Touch);

    private static DragEndDetails End() => new();

    // --------------------------------------------------------------- memory events

    [Fact]
    public void OverlaysDispatchMemoryEvents()
    {
        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent @event)
        {
            if (@event.Object is SelectionOverlay or TextSelectionOverlay)
            {
                events.Add(@event);
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        try
        {
            using FrameworkDartTester tester = Pump(Field());
            Select(tester, 0, 4);
            TextSelectionOverlay overlay = State(tester).SelectionOverlay!;
            overlay.Dispose();

            Assert.Single(events, @event => @event is ObjectCreated { ClassName: "TextSelectionOverlay" }
                                            && ReferenceEquals(@event.Object, overlay));
            Assert.Single(events, @event => @event is ObjectCreated { ClassName: "SelectionOverlay" }
                                            && ReferenceEquals(@event.Object, overlay.SelectionOverlay));
            Assert.Single(events, @event => @event is ObjectDisposed && ReferenceEquals(@event.Object, overlay));
            Assert.Single(events, @event => @event is ObjectDisposed
                                            && ReferenceEquals(@event.Object, overlay.SelectionOverlay));
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }
    }

    // --------------------------------------------------------------- handle drags

    [Fact]
    public void EndHandleJumpsOnlyAfterAFullLineOfDrag()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 2);
        double lineHeight = Editable(tester).PreferredLineHeight;
        SelectionHandleOverlay handle = Handle(tester, end: true);
        Point start = GlobalEndpoint(tester, end: true);

        handle.OnSelectionHandleDragStart!(Start(start));
        handle.OnSelectionHandleDragUpdate!(Update(start + new Point(0, lineHeight * 0.9)));
        tester.Pump();
        Assert.Equal(new TextSelection(0, 2), _controller.Selection);

        handle.OnSelectionHandleDragUpdate!(Update(start + new Point(0, lineHeight * 1.1)));
        tester.Pump();
        // One line down: the extent is on "bbbb bbbb", right below where it was.
        Assert.Equal(0, _controller.Selection.BaseOffset);
        Assert.Equal(12, _controller.Selection.ExtentOffset);

        // Going back up by less than a line keeps the handle on the second line.
        handle.OnSelectionHandleDragUpdate!(Update(start + new Point(0, lineHeight * 0.2)));
        tester.Pump();
        Assert.Equal(12, _controller.Selection.ExtentOffset);

        handle.OnSelectionHandleDragUpdate!(Update(start + new Point(0, -lineHeight * 0.1)));
        tester.Pump();
        Assert.Equal(2, _controller.Selection.ExtentOffset);
        handle.OnSelectionHandleDragEnd!(End());
    }

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Windows)]
    public void HandlesCannotCrossOnNonApplePlatforms(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(), platform);
        Select(tester, 2, 4);
        SelectionHandleOverlay end = Handle(tester, end: true);
        Point endPosition = GlobalEndpoint(tester, end: true);

        end.OnSelectionHandleDragStart!(Start(endPosition));
        end.OnSelectionHandleDragUpdate!(Update(new Point(GlobalCaret(tester, 1).X, endPosition.Y)));
        tester.Pump();
        Assert.Equal(new TextSelection(2, 4), _controller.Selection);

        SelectionHandleOverlay startHandle = Handle(tester, end: false);
        Point startPosition = GlobalEndpoint(tester, end: false);
        end.OnSelectionHandleDragEnd!(End());
        startHandle.OnSelectionHandleDragStart!(Start(startPosition));
        startHandle.OnSelectionHandleDragUpdate!(Update(new Point(GlobalCaret(tester, 6).X, startPosition.Y)));
        tester.Pump();
        Assert.Equal(new TextSelection(2, 4), _controller.Selection);
        startHandle.OnSelectionHandleDragEnd!(End());
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void DraggingAnEndHandlePastTheOtherSwapsOnApplePlatforms(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(), platform);
        Select(tester, 2, 4);
        SelectionHandleOverlay end = Handle(tester, end: true);
        Point endPosition = GlobalEndpoint(tester, end: true);

        end.OnSelectionHandleDragStart!(Start(endPosition));
        end.OnSelectionHandleDragUpdate!(Update(new Point(GlobalCaret(tester, 1).X, endPosition.Y)));
        tester.Pump();

        // The selection keeps its base where the drag started and follows the handle.
        Assert.Equal(new TextSelection(2, 1), _controller.Selection);
        end.OnSelectionHandleDragEnd!(End());
    }

    [Fact]
    public void DraggingACollapsedHandleMovesTheCaret()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 2, 2);
        SelectionHandleOverlay handle = Handle(tester, end: false);
        Point position = GlobalEndpoint(tester, end: false);

        handle.OnSelectionHandleDragStart!(Start(position));
        handle.OnSelectionHandleDragUpdate!(Update(new Point(GlobalCaret(tester, 7).X, position.Y)));
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(7), _controller.Selection);
        handle.OnSelectionHandleDragEnd!(End());
    }

    [Fact]
    public void DraggingAHandleDoesNotCrashWhenTheLayoutIsDegenerate()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        Point position = GlobalEndpoint(tester, end: true);

        // With a zero text scaler the preferred line height is zero and there is no handle Dy.
        tester.PumpWidget(Wrap(Field(), TextScaler.Linear(0.0)));
        tester.Pump();
        SelectionHandleOverlay handle = Handle(tester, end: true);
        handle.OnSelectionHandleDragStart!(Start(position));
        handle.OnSelectionHandleDragUpdate!(Update(position + new Point(20, 20)));
        tester.Pump();
        handle.OnSelectionHandleDragEnd!(End());

        Assert.Equal(new TextSelection(0, 4), _controller.Selection);
        Assert.Empty(_errors);
    }

    [Fact]
    public void DragEndShowsTheContextMenuForARange()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        TextSelectionOverlay overlay = State(tester).SelectionOverlay!;
        Assert.False(overlay.ToolbarIsVisible);

        SelectionHandleOverlay handle = Handle(tester, end: true);
        Point position = GlobalEndpoint(tester, end: true);
        handle.OnSelectionHandleDragStart!(Start(position));
        handle.OnSelectionHandleDragEnd!(End());
        tester.Pump();

        Assert.True(overlay.ToolbarIsVisible);
    }

    // ------------------------------------------------------------ overlay state

    [Fact]
    public void CanHandleThePartialSelectionOfAMultiCodeUnitGlyph()
    {
        const string family = "\U0001F468‍\U0001F469‍\U0001F466";
        using FrameworkDartTester tester = Pump(Field(), text: family);

        Select(tester, 0, 1);

        Assert.Empty(_errors);
        Assert.True(State(tester).SelectionOverlay!.HandlesAreVisible);
    }

    [Fact]
    public void HandlesAreVisibleFollowsHandlesVisible()
    {
        using FrameworkDartTester tester = Pump(Field(showSelectionHandles: false));
        Select(tester, 0, 4);
        TextSelectionOverlay overlay = State(tester).SelectionOverlay!;

        // The entries are inserted, but the field does not want them shown.
        Assert.True(overlay.SelectionOverlay.HandlesAreInserted);
        Assert.False(overlay.HandlesVisible);
        Assert.False(overlay.HandlesAreVisible);

        overlay.HandlesVisible = true;
        Assert.True(overlay.HandlesAreVisible);
    }

    [Fact]
    public void HandleTypesFollowTheTextDirection()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        SelectionOverlay overlay = State(tester).SelectionOverlay!.SelectionOverlay;
        Assert.Equal(TextSelectionHandleType.Left, overlay.StartHandleType);
        Assert.Equal(TextSelectionHandleType.Right, overlay.EndHandleType);

        Select(tester, 2, 2);
        Assert.Equal(TextSelectionHandleType.Collapsed, overlay.StartHandleType);
        Assert.Equal(TextSelectionHandleType.Collapsed, overlay.EndHandleType);
    }

    [Fact]
    public void MarkNeedsBuildDuringTheBuildPhaseIsDeferredToAPostFrameCallback()
    {
        Action? duringBuild = null;
        Widget Build() => Wrap(new Builder(_ =>
        {
            duringBuild?.Invoke();
            duringBuild = null;
            return Field();
        }));

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        _controller.Text = MultilineText;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Build());
        tester.Pump();
        Select(tester, 0, 4);
        SelectionOverlay overlay = State(tester).SelectionOverlay!.SelectionOverlay;

        SchedulerPhase? phaseDuringBuild = null;
        duringBuild = () =>
        {
            phaseDuringBuild = Scheduler.Phase;
            // Marking the overlay entries dirty now would be too late for this frame; one rebuild
            // is scheduled for after it instead.
            overlay.MarkNeedsBuild();
            overlay.MarkNeedsBuild();
        };
        tester.PumpWidget(Build());

        Assert.Equal(SchedulerPhase.PersistentCallbacks, phaseDuringBuild);
        tester.Pump();
        Assert.True(overlay.HandlesAreInserted);
        Assert.Same(overlay, State(tester).SelectionOverlay!.SelectionOverlay);
    }

    [Fact]
    public void ShowingTheSpellCheckMenuHidesTheHandles()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        TextSelectionOverlay overlay = State(tester).SelectionOverlay!;
        Assert.True(overlay.HandlesAreVisible);

        overlay.ShowSpellCheckSuggestionsToolbar(_ => new SizedBox(width: 10, height: 10));
        tester.Pump();

        Assert.False(overlay.HandlesAreVisible);
        Assert.True(overlay.SpellCheckToolbarIsVisible);
        Assert.True(overlay.ToolbarIsVisible);

        overlay.HideToolbar();
        Assert.False(overlay.SpellCheckToolbarIsVisible);
        Assert.False(overlay.ToolbarIsVisible);
    }

    [Fact]
    public void ToolbarVisibilityFollowsTheSelectionViewport()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        Assert.True(State(tester).ShowToolbar());
        tester.Pump();

        var wrapper = tester.AllElements().Select(element => element.Widget).OfType<SelectionToolbarWrapper>().Single();
        Assert.NotNull(wrapper.Visibility);
        Assert.True(wrapper.Visibility!.Value);
    }

    // ------------------------------------------------ EditableText and the overlay

    [Fact]
    public void ShowToolbarNeedsTheSelectionOverlayAndReturnsFalseWhileShown()
    {
        using FrameworkDartTester tester = Pump(Field());
        EditableText.EditableTextState state = State(tester);
        Assert.False(state.ShowToolbar());

        Select(tester, 0, 4);
        Assert.True(state.ShowToolbar());
        tester.Pump();
        Assert.False(state.ShowToolbar());
        Assert.True(state.SelectionOverlay!.ToolbarIsVisible);
    }

    [Fact]
    public void HideToolbarWithoutHandlesKeepsTheHandles()
    {
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);
        EditableText.EditableTextState state = State(tester);
        Assert.True(state.ShowToolbar());
        tester.Pump();

        state.HideToolbar(hideHandles: false);
        Assert.False(state.SelectionOverlay!.ToolbarIsVisible);
        Assert.True(state.SelectionOverlay.HandlesAreVisible);

        state.HideToolbar();
        Assert.False(state.SelectionOverlay.HandlesAreVisible);
    }

    [Fact]
    public void OnSelectionHandleTappedCanBeUpdated()
    {
        int first = 0;
        int second = 0;
        using FrameworkDartTester tester = Pump(Field(onSelectionHandleTapped: () => first += 1));
        Select(tester, 0, 4);
        Handle(tester, end: false).OnSelectionHandleTapped!();
        Assert.Equal(1, first);

        tester.PumpWidget(Wrap(Field(onSelectionHandleTapped: () => second += 1)));
        tester.Pump();
        Handle(tester, end: false).OnSelectionHandleTapped!();
        Assert.Equal(1, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void DragStartBehaviorCanBeUpdated()
    {
        using FrameworkDartTester tester = Pump(Field(dragStartBehavior: DragStartBehavior.Down));
        Select(tester, 0, 4);
        Assert.Equal(DragStartBehavior.Down, Handle(tester, end: false).DragStartBehavior);

        tester.PumpWidget(Wrap(Field(dragStartBehavior: DragStartBehavior.Start)));
        tester.Pump();
        Assert.Equal(DragStartBehavior.Start, Handle(tester, end: false).DragStartBehavior);
    }

    // Dart: 'the toolbar is disposed when selection changes and there is no selectionControls'.
    [Fact]
    public void TheToolbarIsDisposedWhenTheSelectionControlsGoAway()
    {
        var recording = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(recording);
        try
        {
            using FrameworkDartTester tester = Pump(Field(), text: string.Empty);
            EditableText.EditableTextState state = State(tester);

            // Can't show the toolbar without a selection overlay.
            Assert.False(state.ShowToolbar());

            // Can show the toolbar when focused even though there's no text.
            Editable(tester).SelectWordsInRange(from: default, cause: SelectionChangedCause.Tap);
            tester.Pump();
            Assert.True(state.ShowToolbar());
            tester.PumpAndSettle();
            Assert.Single(tester.AllElements().Select(element => element.Widget).OfType<SelectionToolbarWrapper>());

            // Turn off the selection controls and change the text, which disposes the toolbar.
            tester.PumpWidget(Wrap(new EditableText(
                controller: _controller,
                focusNode: _focusNode,
                style: new TextStyle(FontSize: 10.0),
                cursorColor: new Color(0xFF2196F3),
                backgroundCursorColor: new Color(0xFF9E9E9E),
                enableInteractiveSelection: false,
                multiline: true)));
            tester.Pump();
            state.UpdateEditingValue(new TextEditingValue("abc", TextSelection.Collapsed(3)));
            tester.Pump();

            Assert.Empty(tester.AllElements().Select(element => element.Widget).OfType<SelectionToolbarWrapper>());
        }
        finally
        {
            UI.TextInput.RestorePlatformInputControl();
        }
    }

    // ------------------------------------------------------------- button items

    [Fact]
    public void GetEditableButtonItemsOrdersTheItemsForThePlatform()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        List<ContextMenuButtonItem> android = EditableText.GetEditableButtonItems(
            clipboardStatus: ClipboardStatus.Pasteable,
            onCopy: () => { },
            onCut: () => { },
            onPaste: () => { },
            onSelectAll: () => { },
            onLookUp: () => { },
            onSearchWeb: () => { },
            onShare: () => { },
            onLiveTextInput: () => { });
        Assert.Equal(
            [
                ContextMenuButtonType.Cut, ContextMenuButtonType.Copy, ContextMenuButtonType.Paste,
                ContextMenuButtonType.Share, ContextMenuButtonType.SelectAll, ContextMenuButtonType.LookUp,
                ContextMenuButtonType.SearchWeb, ContextMenuButtonType.LiveTextInput,
            ],
            android.Select(item => item.Type));

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        List<ContextMenuButtonItem> ios = EditableText.GetEditableButtonItems(
            clipboardStatus: ClipboardStatus.Pasteable,
            onCopy: () => { },
            onCut: null,
            onPaste: null,
            onSelectAll: () => { },
            onLookUp: null,
            onSearchWeb: null,
            onShare: () => { },
            onLiveTextInput: null);
        Assert.Equal(
            [ContextMenuButtonType.Copy, ContextMenuButtonType.SelectAll, ContextMenuButtonType.Share],
            ios.Select(item => item.Type));
    }

    [Fact]
    public void GetEditableButtonItemsWaitsForTheClipboardWhenPasteIsPossible()
    {
        List<ContextMenuButtonItem> items = EditableText.GetEditableButtonItems(
            clipboardStatus: ClipboardStatus.Unknown,
            onCopy: () => { },
            onCut: () => { },
            onPaste: () => { },
            onSelectAll: () => { },
            onLookUp: null,
            onSearchWeb: null,
            onShare: null,
            onLiveTextInput: () => { });

        // Only the clipboard-independent Live Text item is offered.
        Assert.Equal([ContextMenuButtonType.LiveTextInput], items.Select(item => item.Type));

        List<ContextMenuButtonItem> noPaste = EditableText.GetEditableButtonItems(
            clipboardStatus: ClipboardStatus.Unknown,
            onCopy: () => { },
            onCut: null,
            onPaste: null,
            onSelectAll: null,
            onLookUp: null,
            onSearchWeb: null,
            onShare: null,
            onLiveTextInput: null);
        Assert.Equal([ContextMenuButtonType.Copy], noPaste.Select(item => item.Type));
    }

    [Fact]
    public void ToolbarOptionsDefaultToEmptyWithHandleControls()
    {
        Assert.Same(ToolbarOptions.Empty, Field().ToolbarOptions);
        Assert.Equal(
            new ToolbarOptions(Copy: true, Cut: true, Paste: true, SelectAll: true),
            new EditableText(_controller).ToolbarOptions);
        Assert.Equal(
            new ToolbarOptions(Paste: true, SelectAll: true),
            new EditableText(_controller, obscureText: true).ToolbarOptions);
        Assert.Equal(
            new ToolbarOptions(Copy: true, SelectAll: true),
            new EditableText(_controller, readOnly: true).ToolbarOptions);
        Assert.Same(ToolbarOptions.Empty, new EditableText(_controller, readOnly: true, obscureText: true).ToolbarOptions);
    }

    [Fact]
    public void ToolbarOptionsItemsReplaceTheEditableItems()
    {
        using var clipboard = new MockClipboardPlatform("pasteable");
        using FrameworkDartTester tester = Pump(Field(toolbarOptions: new ToolbarOptions(Copy: true)));
        Select(tester, 0, 4);

        Assert.Equal(
            [ContextMenuButtonType.Copy],
            State(tester).ContextMenuButtonItems.Select(item => item.Type));
    }

    // ----------------------------------------------------------------- Live Text

    [Fact]
    public void LiveTextButtonIsOfferedWhenThePlatformSupportsIt()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            call => call.Method switch
            {
                "LiveText.isLiveTextInputAvailable" => true,
                "Clipboard.hasStrings" => new Dictionary<string, object?> { ["value"] = true },
                _ => null,
            });
        using FrameworkDartTester tester = Pump(Field(), TargetPlatform.IOS);
        _focusNode.RequestFocus();
        tester.Pump();
        Select(tester, 2, 2);
        Scheduler.FlushMicrotasks();
        tester.Pump();

        EditableText.EditableTextState state = State(tester);
        Assert.True(state.LiveTextInputEnabled);
        Assert.Contains(state.ContextMenuButtonItems, item => item.Type == ContextMenuButtonType.LiveTextInput);

        // A range selection, an obscured or a read-only field offers no Live Text.
        Select(tester, 0, 4);
        Assert.False(state.LiveTextInputEnabled);
        Assert.DoesNotContain(state.ContextMenuButtonItems, item => item.Type == ContextMenuButtonType.LiveTextInput);
    }

    [Fact]
    public void TappingTheLiveTextButtonReportsAnErrorWhenTheChannelFails()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            call => call.Method switch
            {
                "LiveText.isLiveTextInputAvailable" => true,
                "Clipboard.hasStrings" => new Dictionary<string, object?> { ["value"] = true },
                _ => null,
            });
        var recording = new RecordingTextInputControl();
        UI.TextInput.SetInputControl(recording);
        using var textInput = new MockMethodCallHandler(
            SystemChannels.TextInput,
            call => call.Method == "TextInput.startLiveTextInput"
                ? throw new PlatformException(code: "ERROR", message: "Channel failed")
                : null);
        using FrameworkDartTester tester = Pump(
            Field(toolbarOptions: ToolbarOptions.Empty),
            TargetPlatform.IOS);
        _focusNode.RequestFocus();
        tester.Pump();
        Select(tester, 2, 2);
        Scheduler.FlushMicrotasks();
        tester.Pump();

        ContextMenuButtonItem liveText = Assert.Single(
            State(tester).ContextMenuButtonItems,
            item => item.Type == ContextMenuButtonType.LiveTextInput);
        // The tester owns FlutterError.OnError; collect the details here instead.
        FlutterExceptionHandler? testerHandler = FlutterError.OnError;
        FlutterError.OnError = _errors.Add;
        try
        {
            liveText.OnPressed!();
            Scheduler.FlushMicrotasks();
        }
        finally
        {
            FlutterError.OnError = testerHandler;
        }

        FlutterErrorDetails error = Assert.Single(
            _errors,
            details => details.Context?.ToString()?.Contains("while starting Live Text input") == true);
        Assert.Contains("Channel failed", error.Exception.ToString());
        Assert.Equal("widgets library", error.Library);
    }

    // ---------------------------------------------------------------------- web

    [Fact]
    public void WebAvoidsThePastePermissionsPromptByNotCallingHasStrings()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var clipboard = new MockClipboardPlatform("pasteable");
        using FrameworkDartTester tester = Pump(Field(obscureText: true, multiline: false));
        Scheduler.FlushMicrotasks();

        Assert.DoesNotContain(clipboard.Calls, call => call.Method == "Clipboard.hasStrings");
        Assert.Equal(ClipboardStatus.Pasteable, State(tester).ClipboardStatus.Value);

        Select(tester, 0, 2);
        _ = State(tester).ShowToolbar();
        Scheduler.FlushMicrotasks();
        Assert.DoesNotContain(clipboard.Calls, call => call.Method == "Clipboard.hasStrings");
    }

    [Fact]
    public void NativeClipboardStatusIsQueriedWhenTheFieldStarts()
    {
        using var clipboard = new MockClipboardPlatform("pasteable");
        using FrameworkDartTester tester = Pump(Field());
        Scheduler.FlushMicrotasks();

        Assert.Contains(clipboard.Calls, call => call.Method == "Clipboard.hasStrings");
        Assert.Equal(ClipboardStatus.Pasteable, State(tester).ClipboardStatus.Value);
    }

    [Fact]
    public async Task WebShowsTheFlutterContextMenuOnlyWhenTheBrowserMenuIsDisabled()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var contextMenu = new MockMethodCallHandler(SystemChannels.ContextMenu);
        using FrameworkDartTester tester = Pump(Field());
        Select(tester, 0, 4);

        // The browser's own menu is in charge.
        Assert.False(State(tester).ShowToolbar());

        await BrowserContextMenu.DisableContextMenu();
        tester.PumpWidget(Wrap(Field(contextMenuBuilder: (_, _) => new SizedBox(width: 11, height: 11))));
        tester.Pump();
        Select(tester, 0, 5);
        Assert.True(State(tester).ShowToolbar());
        tester.Pump();

        State(tester).HideToolbar();
        Assert.False(State(tester).SelectionOverlay!.ToolbarIsVisible);
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public override void SetEditingState(TextEditingValue value)
        {
        }
    }
}
