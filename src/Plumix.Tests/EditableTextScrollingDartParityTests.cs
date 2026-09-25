using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

// Dart parity: flutter/packages/flutter/test/widgets/editable_text_show_on_screen_test.dart,
// editable_text_cursor_test.dart (floating cursor) and the scrolling, input-geometry, floating
// cursor, autocorrection and context-menu-on-scroll tests of editable_text_test.dart.
public sealed class EditableTextScrollingDartParityTests : IDisposable
{
    private readonly RecordingTextInputControl _control = new();

    public EditableTextScrollingDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        UI.TextInput.SetInputControl(_control);
    }

    public void Dispose()
    {
        UI.TextInput.RestorePlatformInputControl();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static Widget Ltr(Widget child) => new Directionality(TextDirection.Ltr, child);

    private static EditableText Field(
        TextEditingController controller,
        FocusNode? focusNode = null,
        bool multiline = false,
        int? maxLines = null,
        bool readOnly = false,
        ScrollController? scrollController = null,
        ScrollPhysics? scrollPhysics = null,
        Thickness? scrollPadding = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null,
        Color? autocorrectionTextRectColor = null,
        bool cursorOpacityAnimates = false,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        TextSelectionControls? selectionControls = null,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null) =>
        new(
            controller: controller,
            focusNode: focusNode,
            multiline: multiline,
            maxLines: maxLines,
            readOnly: readOnly,
            padding: new Thickness(0),
            scrollController: scrollController,
            scrollPhysics: scrollPhysics,
            scrollPadding: scrollPadding,
            inputFormatters: inputFormatters,
            autocorrectionTextRectColor: autocorrectionTextRectColor,
            backgroundCursorColor: new Color(0xFFAAAAAA),
            cursorOpacityAnimates: cursorOpacityAnimates,
            contextMenuBuilder: contextMenuBuilder,
            selectionControls: selectionControls,
            onSelectionChanged: onSelectionChanged);

    private static RenderEditable Editable(FrameworkDartTester tester) =>
        tester.AllElements()
            .Select(element => element.RenderObject)
            .OfType<RenderEditable>()
            .Distinct()
            .Single();

    private static EditableText.EditableTextState State(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>();

    private static bool IsCaretOnScreen(FrameworkDartTester tester)
    {
        RenderEditable editable = Editable(tester);
        Rect local = editable.GetLocalRectForCaret(editable.Selection!.Value.Base);
        Rect global = MatrixUtils.TransformRect(editable.GetTransformTo(null), local);
        return global.Left >= 0 && global.Top >= 0 && global.Right <= 800 && global.Bottom <= 600;
    }

    // ------------------------------------------------------------------ show on screen

    [Fact]
    public void SingleLineField_ScrollsCaretIntoViewWhenFocused()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 100));
        controller.Selection = TextSelection.Collapsed(100);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 100,
                child: Field(controller, focusNode, scrollController: scrollController)))));

        Assert.Equal(0.0, scrollController.Offset);
        Assert.True(Editable(tester).MaxScrollExtent > 0);

        focusNode.RequestFocus();
        tester.PumpAndSettle();

        // The caret's right edge lands on the field's right edge (the scroll extent also holds the
        // caret gap, so this is one pixel short of it).
        RenderEditable editable = Editable(tester);
        Rect caret = editable.GetLocalRectForCaret(new TextPosition(100));
        Assert.Equal(100.0, caret.Right, 6);
        Assert.True(scrollController.Offset > 0);
        Assert.True(IsCaretOnScreen(tester));
    }

    [Fact]
    public void FocusedMultilineField_ScrollsCaretBackIntoViewWhenTyping()
    {
        using var tester = new FrameworkDartTester();
        string text = "Start" + new string('\n', 39) + "End";
        var controller = new TextEditingController(text);
        controller.Selection = TextSelection.Collapsed(text.Length);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: new SizedBox(
            height: 300,
            child: new ListView(
                controller: scrollController,
                children: [Field(controller, focusNode, multiline: true)])))));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        scrollController.JumpTo(0.0);
        tester.Pump();
        Assert.True(Editable(tester).Size.Height > 500);

        State(tester).UpdateEditingValue(new TextEditingValue(
            text + " HELLO",
            TextSelection.Collapsed(text.Length + 6)));
        tester.PumpAndSettle();

        Assert.True(scrollController.Offset > 0.0);
    }

    [Fact]
    public void FocusedMultilineField_NonCollapsedSelectionRevealsTheLastSelectionBox()
    {
        using var tester = new FrameworkDartTester();
        string text = "Start" + new string('\n', 39) + "End";
        var controller = new TextEditingController(text);
        controller.Selection = TextSelection.Collapsed(text.Length - 3);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: new SizedBox(
            height: 300,
            child: new ListView(
                controller: scrollController,
                children: [Field(controller, focusNode, multiline: true)])))));

        focusNode.RequestFocus();
        tester.PumpAndSettle();
        scrollController.JumpTo(0.0);
        tester.Pump();

        var selection = new TextSelection(26, 27);
        State(tester).UpdateEditingValue(new TextEditingValue(text, selection));
        tester.PumpAndSettle();

        // The bottom of the selected line plus the default 20px scroll padding, minus the 300px
        // viewport: exactly Dart's 28.0 with 14px lines.
        double boxBottom = Editable(tester).GetBoxesForSelection(selection)[^1].ToRect().Bottom;
        Assert.Equal(boxBottom + 20.0 - 300.0, scrollController.Offset, 6);
        Assert.Equal(28.0, scrollController.Offset, 6);
    }

    [Fact]
    public void EditableWithScrollPadding_IsRevealedWithThePadding()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("hello");
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: new SizedBox(
            height: 300,
            child: new ListView(
                controller: scrollController,
                children:
                [
                    new SizedBox(height: 200),
                    Field(controller, focusNode, scrollPadding: new Thickness(50)),
                    new SizedBox(height: 850),
                ])))));
        double editableHeight = Editable(tester).Size.Height;
        scrollController.JumpTo(200 + editableHeight / 2);
        tester.Pump();

        focusNode.RequestFocus();
        tester.PumpAndSettle();

        // The caret's top minus the 50px padding lands on the viewport's top edge (Dart: ~152.0).
        Assert.Equal(150.0, scrollController.Offset, 6);
    }

    [Fact]
    public void EnteringText_DoesNotScrollWhenPhysicsForbidImplicitScrolling()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        var scrollController = new ScrollController(initialScrollOffset: 100.0);
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: new SizedBox(
            height: 300,
            child: new ListView(
                controller: scrollController,
                physics: new NoImplicitScrollPhysics(),
                children:
                [
                    new SizedBox(height: 350),
                    Field(controller, focusNode),
                    new SizedBox(height: 350),
                ])))));

        focusNode.RequestFocus();
        tester.Pump();
        scrollController.JumpTo(0.0);
        tester.PumpAndSettle();

        State(tester).UpdateEditingValue(new TextEditingValue("Hello", TextSelection.Collapsed(5)));
        tester.PumpAndSettle();

        Assert.Equal(0.0, scrollController.Offset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FocusTriggeredShowCaretOnScreen_RespectsReadOnly(bool readOnly)
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 100));
        controller.Selection = TextSelection.Collapsed(100);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        var editableScrollController = new ScrollController();
        tester.PumpWidget(Ltr(new ListView(
            controller: scrollController,
            cacheExtent: 1000,
            children:
            [
                new SizedBox(height: 599),
                Field(controller, focusNode, readOnly: readOnly, scrollController: editableScrollController),
            ])));

        focusNode.RequestFocus();
        tester.PumpAndSettle();

        Assert.Equal(!readOnly, IsCaretOnScreen(tester));
        Assert.Equal(!readOnly, scrollController.Offset > 0.0);
        Assert.Equal(!readOnly, editableScrollController.Offset > 0.0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionTriggeredShowCaretOnScreen_VirtualKeyboardSkipsReadOnlyFields(bool readOnly)
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 100));
        controller.Selection = TextSelection.Collapsed(80);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        var editableScrollController = new ScrollController();
        tester.PumpWidget(Ltr(new ListView(
            controller: scrollController,
            cacheExtent: 1000,
            children:
            [
                new SizedBox(height: 599),
                Field(controller, focusNode, readOnly: readOnly, scrollController: editableScrollController),
            ])));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        scrollController.JumpTo(0.0);
        editableScrollController.JumpTo(0.0);
        tester.Pump();
        Assert.False(IsCaretOnScreen(tester));

        State(tester).UpdateEditingValue(controller.Value.CopyWith(selection: TextSelection.Collapsed(90)));
        tester.PumpAndSettle();

        Assert.Equal(!readOnly, IsCaretOnScreen(tester));
        Assert.Equal(!readOnly, scrollController.Offset > 0.0);
        Assert.Equal(!readOnly, editableScrollController.Offset > 0.0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionTriggeredShowCaretOnScreen_TextSelectionDelegateShowsEvenReadOnlyAndRejected(
        bool readOnly)
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 100));
        controller.Selection = TextSelection.Collapsed(80);
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        var editableScrollController = new ScrollController();
        TextInputFormatter reject = TextInputFormatter.WithFunction((oldValue, _) => oldValue);

        Widget Build(bool rejectInput) => Ltr(new ListView(
            controller: scrollController,
            cacheExtent: 1000,
            children:
            [
                new SizedBox(height: 599),
                Field(
                    controller,
                    focusNode,
                    readOnly: readOnly,
                    scrollController: editableScrollController,
                    inputFormatters: rejectInput ? [reject] : null),
            ]));

        tester.PumpWidget(Build(rejectInput: false));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        scrollController.JumpTo(0.0);
        editableScrollController.JumpTo(0.0);
        tester.Pump();

        State(tester).UserUpdateTextEditingValue(
            controller.Value.CopyWith(selection: TextSelection.Collapsed(90)),
            null);
        tester.PumpAndSettle();
        Assert.True(IsCaretOnScreen(tester));
        Assert.True(scrollController.Offset > 0.0);
        Assert.True(editableScrollController.Offset > 0.0);

        tester.PumpWidget(Build(rejectInput: true));
        scrollController.JumpTo(0.0);
        editableScrollController.JumpTo(0.0);
        tester.Pump();

        State(tester).UserUpdateTextEditingValue(
            controller.Value.CopyWith(selection: TextSelection.Collapsed(100)),
            null);
        tester.PumpAndSettle();
        Assert.True(IsCaretOnScreen(tester));
        Assert.True(scrollController.Offset > 0.0);
        Assert.True(editableScrollController.Offset > 0.0);
    }

    [Fact]
    public void ShowCaretOnScreen_DoesNotRandomlyTriggerWhenTheCursorBlinks()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 100));
        controller.Selection = TextSelection.Collapsed(0);
        var focusNode = new FocusNode();
        var editableScrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 400,
                child: Field(
                    controller,
                    focusNode,
                    scrollController: editableScrollController,
                    cursorOpacityAnimates: true)))));
        // The blinking cursor never settles, so this test pumps a fixed number of frames.
        focusNode.RequestFocus();
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.True(IsCaretOnScreen(tester));
        Assert.Equal(0.0, editableScrollController.Offset);

        State(tester).UpdateEditingValue(controller.Value.CopyWith(text: new string('a', 101)));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.True(IsCaretOnScreen(tester));
        Assert.Equal(0.0, editableScrollController.Offset);

        editableScrollController.JumpTo(100.0);
        tester.Pump();
        for (int i = 0; i < 5; i++)
        {
            tester.Pump(TimeSpan.FromMilliseconds(500));
        }

        Assert.Equal(100.0, editableScrollController.Offset);
        Assert.False(IsCaretOnScreen(tester));
    }

    [Fact]
    public void BringIntoView_ClampsToTheScrollExtentsWithoutBouncing()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("XXXXX\nXXXXX\nXXXXX");
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 100,
                child: Field(controller, multiline: true, maxLines: 2, scrollController: scrollController)))));

        RenderEditable editable = Editable(tester);
        double maxScrollExtent = editable.MaxScrollExtent;
        Assert.True(maxScrollExtent > 0);
        Assert.Equal(0.0, scrollController.Position.Pixels);

        scrollController.JumpTo(maxScrollExtent / 2);
        tester.Pump();
        State(tester).BringIntoView(new TextPosition(0));
        Assert.Equal(0.0, scrollController.Position.Pixels);

        State(tester).BringIntoView(new TextPosition(13));
        Assert.Equal(maxScrollExtent, scrollController.Position.Pixels, 6);
    }

    [Fact]
    public void BringIntoView_BringsTheCaretIntoViewWhenInAViewport()
    {
        using var tester = new FrameworkDartTester();
        string text = string.Concat(Enumerable.Repeat("Lorem ipsum dolor sit amet. ", 40));
        var controller = new TextEditingController(text);
        var outerController = new ScrollController();
        var innerController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 200,
                height: 200,
                child: new SingleChildScrollView(
                    controller: outerController,
                    child: Field(controller, multiline: true, scrollController: innerController))))));

        State(tester).BringIntoView(new TextPosition(text.Length));
        tester.PumpAndSettle();

        Assert.Equal(outerController.Position.MaxScrollExtent, outerController.Offset, 6);
        Assert.Equal(0.0, innerController.Offset);
    }

    [Fact]
    public void BringIntoView_DoesNothingWhenThePhysicsProhibitImplicitScrolling()
    {
        using var tester = new FrameworkDartTester();
        string text = string.Concat(Enumerable.Repeat("Lorem ipsum dolor sit amet. ", 40));
        var controller = new TextEditingController(text);
        var scrollController = new ScrollController();

        Widget Build(ScrollPhysics? physics) => Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 200,
                height: 200,
                child: Field(
                    controller,
                    multiline: true,
                    maxLines: 8,
                    scrollController: scrollController,
                    scrollPhysics: physics))));

        tester.PumpWidget(Build(null));
        State(tester).BringIntoView(new TextPosition(text.Length));
        tester.PumpAndSettle();
        Assert.Equal(scrollController.Position.MaxScrollExtent, scrollController.Offset, 6);
        Assert.True(scrollController.Offset > 0);

        scrollController.JumpTo(0.0);
        tester.PumpWidget(Build(new NoImplicitScrollPhysics()));
        State(tester).BringIntoView(new TextPosition(text.Length));
        tester.PumpAndSettle();
        Assert.Equal(0.0, scrollController.Offset);
    }

    [Fact]
    public void ScrollController_CanBeChanged()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("text");
        var scrollController1 = new ScrollController();
        var scrollController2 = new ScrollController();

        tester.PumpWidget(Ltr(Field(controller, scrollController: scrollController1)));
        Assert.True(scrollController1.HasClients);
        Assert.Same(scrollController1, State(tester).Widget.ScrollController);

        tester.PumpWidget(Ltr(Field(controller, scrollController: scrollController2)));
        Assert.False(scrollController1.HasClients);
        Assert.True(scrollController2.HasClients);

        tester.PumpWidget(Ltr(Field(controller)));
        Assert.False(scrollController2.HasClients);
        Assert.Null(State(tester).Widget.ScrollController);

        tester.PumpWidget(Ltr(Field(controller, scrollController: scrollController2)));
        Assert.True(scrollController2.HasClients);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(TargetPlatform.IOS, false, false)]
    [InlineData(TargetPlatform.IOS, true, true)]
    [InlineData(TargetPlatform.Android, false, true)]
    public void SingleLineFieldCannotBeUserScrolledOnIOS(
        TargetPlatform platform,
        bool multiline,
        bool allowsUserScrolling)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("XXXXX\nXXXXX\nXXXXX");
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(Field(
            controller,
            multiline: multiline,
            maxLines: multiline ? 2 : null,
            scrollController: scrollController)));

        Assert.Equal(allowsUserScrolling, scrollController.Position.Physics.AllowUserScrolling);
        Assert.True(scrollController.Position.Physics.AllowImplicitScrolling);
    }

    [Fact]
    public void SelectionChangedCause_ScrollsTheSelectionIntoViewOnNonApplePlatforms()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester();
        string text = string.Join('\n', Enumerable.Range(0, 64));
        var controller = new TextEditingController(text);
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 200,
                child: Field(controller, multiline: true, maxLines: 2, scrollController: scrollController)))));
        EditableText.EditableTextState state = State(tester);

        void Reset(bool toMax)
        {
            controller.Value = new TextEditingValue(text, new TextSelection(0, 1));
            tester.Pump();
            scrollController.JumpTo(toMax ? scrollController.Position.MaxScrollExtent : 0.0);
            tester.Pump();
        }

        double max = Editable(tester).MaxScrollExtent;
        Assert.True(max > 0);

        Reset(toMax: true);
        state.CopySelection(SelectionChangedCause.Keyboard);
        Assert.Equal(max, scrollController.Offset, 6);

        Reset(toMax: true);
        state.CopySelection(SelectionChangedCause.Toolbar);
        Assert.Equal(0.0, Math.Round(scrollController.Offset));

        Reset(toMax: true);
        state.CutSelection(SelectionChangedCause.Keyboard);
        tester.Pump();
        Assert.Equal(Editable(tester).MaxScrollExtent, scrollController.Offset, 6);

        Reset(toMax: true);
        state.CutSelection(SelectionChangedCause.Toolbar);
        tester.Pump();
        Assert.Equal(0.0, Math.Round(scrollController.Offset));

        Reset(toMax: false);
        state.SelectAll(SelectionChangedCause.Keyboard);
        Assert.Equal(0.0, scrollController.Offset);

        Reset(toMax: false);
        state.SelectAll(SelectionChangedCause.Toolbar);
        Assert.Equal(Math.Round(Editable(tester).MaxScrollExtent), Math.Round(scrollController.Offset));
    }

    [Fact]
    public void SelectAllFromTheToolbar_DoesNotScrollOnApplePlatforms()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        using var tester = new FrameworkDartTester();
        string text = string.Join('\n', Enumerable.Range(0, 64));
        var controller = new TextEditingController(text);
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 200,
                child: Field(controller, multiline: true, maxLines: 2, scrollController: scrollController)))));
        controller.Selection = new TextSelection(0, 1);
        tester.Pump();

        State(tester).SelectAll(SelectionChangedCause.Toolbar);

        Assert.Equal(0.0, scrollController.Offset);
    }

    // ------------------------------------------------------------------ view metrics

    [Fact]
    public void GrowingKeyboardInset_JumpsTheCaretOnScreenWithoutAnimation()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("I love flutter");
        var focusNode = new FocusNode();
        var scrollController = new ScrollController();
        tester.PumpWidget(Ltr(new SingleChildScrollView(
            controller: scrollController,
            child: new Column(
                children:
                [
                    new SizedBox(height: 1000),
                    new SizedBox(height: 20, child: Field(controller, focusNode)),
                ]))));
        focusNode.RequestFocus();
        tester.PumpAndSettle();
        scrollController.JumpTo(0.0);
        tester.Pump();

        tester.View.UpdateMetrics(viewInsets: new Thickness(0, 0, 0, 500));
        State(tester).DidChangeMetrics();
        tester.Pump();

        double offset = scrollController.Offset;
        Assert.NotEqual(0.0, offset);
        tester.PumpAndSettle();
        Assert.Equal(offset, scrollController.Offset);
    }

    [Fact]
    public void DidChangeMetrics_AfterUnmounting_DoesNotThrow()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("text");
        tester.PumpWidget(Ltr(Field(controller)));
        EditableText.EditableTextState state = State(tester);

        tester.PumpWidget(new SizedBox());
        state.DidChangeMetrics();

        Assert.Null(tester.TakeException());
    }

    // ------------------------------------------------------ input connection geometry

    [Fact]
    public void SizeAndTransform_AreSentWhenTheConnectionOpens()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("text");
        var focusNode = new FocusNode();
        tester.PumpWidget(Ltr(new Column(
            crossAxisAlignment: CrossAxisAlignment.Start,
            children: [new SizedBox(height: 50), Field(controller, focusNode)])));

        focusNode.RequestFocus();
        tester.Pump();

        (Size size, Matrix4 transform) = _control.SizeAndTransforms[^1];
        Assert.Equal(Editable(tester).Size, size);
        Assert.True(transform == Matrix4.TranslationValues(0, 50, 0));
    }

    [Fact]
    public void CompositionCallback_SendsTransformChangesWithoutSkippingFrames()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("text");
        controller.Selection = TextSelection.Collapsed(0);
        var focusNode = new FocusNode();
        Point offset = default;
        StateSetter? setOffset = null;
        tester.PumpWidget(Ltr(new StatefulBuilder((context, setState) =>
        {
            setOffset = setState;
            return Plumix.Widgets.Transform.Translate(
                offset,
                new TickerMode(
                    enabled: false,
                    child: new RepaintBoundary(child: Field(controller, focusNode))));
        })));
        focusNode.RequestFocus();
        tester.Pump();
        _control.SizeAndTransforms.Clear();

        setOffset!(() => offset = new Point(42, 0));
        tester.Pump();

        Assert.Contains(_control.SizeAndTransforms, entry => entry.Transform == Matrix4.TranslationValues(42, 0, 0));
    }

    [Fact]
    public void CompositionCallback_IsDisabledWhileThereIsNoInputConnection()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("text");
        var focusNode = new FocusNode();
        tester.PumpWidget(Ltr(Field(controller, focusNode)));
        RenderEditableTextCompositionCallback callback = tester.AllElements()
            .Select(element => element.RenderObject)
            .OfType<RenderEditableTextCompositionCallback>()
            .Distinct()
            .Single();
        Assert.False(callback.Enabled);

        focusNode.RequestFocus();
        tester.Pump();
        Assert.True(callback.Enabled);

        focusNode.Unfocus();
        tester.Pump();
        Assert.False(callback.Enabled);
    }

    [Theory]
    [InlineData(10, 30)]
    [InlineData(30, 10)]
    public void CaretRect_IsTheSelectionStartAndIsOnlySentWhenItChanges(int baseOffset, int extentOffset)
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController(new string('a', 50));
        var focusNode = new FocusNode();
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: Field(controller, focusNode))));
        focusNode.RequestFocus();
        tester.Pump();
        tester.Pump();
        _control.CaretRects.Clear();
        tester.Pump();
        Assert.Empty(_control.CaretRects);

        controller.Selection = new TextSelection(baseOffset, extentOffset);
        tester.Pump();

        Rect expected = Editable(tester).GetLocalRectForCaret(new TextPosition(10));
        Assert.Equal(expected, _control.CaretRects[^1]);
    }

    [Fact]
    public void ComposingRect_IsSentForTheComposingRange()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        tester.PumpWidget(Ltr(new Align(alignment: Alignment.TopLeft, child: Field(controller, focusNode))));
        focusNode.RequestFocus();
        tester.Pump();

        State(tester).UpdateEditingValue(new TextEditingValue(
            "abcd",
            TextSelection.Collapsed(4),
            composing: new TextRange(1, 3)));
        tester.Pump();
        tester.Pump();

        Rect? expected = Editable(tester).GetRectForComposingRange(new TextRange(1, 3));
        Assert.NotNull(expected);
        Assert.Equal(expected.Value, _control.ComposingRects[^1]);
    }

    // --------------------------------------------------------------- floating cursor

    private const string FloatingCursorText = "hello world this is fun and cool and awesome!";

    private static (FrameworkDartTester Tester, TextEditingController Controller, List<SelectionChangedCause?> Causes)
        PumpFloatingCursorField()
    {
        var tester = new FrameworkDartTester();
        var controller = new TextEditingController(FloatingCursorText);
        var focusNode = new FocusNode();
        var causes = new List<SelectionChangedCause?>();
        tester.PumpWidget(Ltr(new Align(
            alignment: Alignment.TopLeft,
            child: Field(controller, focusNode, onSelectionChanged: (_, cause) => causes.Add(cause)))));
        focusNode.RequestFocus();
        tester.Pump();
        controller.Selection = TextSelection.Collapsed(29);
        tester.Pump();
        causes.Clear();
        return (tester, controller, causes);
    }

    [Fact]
    public void FloatingCursor_UpdateMovesTheCaretOnlyWhenItEnds()
    {
        (FrameworkDartTester tester, TextEditingController controller, List<SelectionChangedCause?> causes) =
            PumpFloatingCursorField();
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            RenderEditable editable = Editable(tester);

            state.UpdateFloatingCursor(new RawFloatingCursorPoint(
                FloatingCursorDragState.Start,
                offset: new Point(20, 20)));
            Assert.True(editable.FloatingCursorOn);
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(
                FloatingCursorDragState.Update,
                offset: new Point(-250, 20)));
            tester.Pump();
            Assert.Equal(29, controller.Selection.BaseOffset);

            // The bounded offset: the start caret's center moved by the drag, less half a line,
            // clamped into the text plus the floating cursor margin.
            double lineHeight = editable.PreferredLineHeight;
            Point startCenter = editable.GetLocalRectForCaret(new TextPosition(29)).Center;
            double boundedX = Math.Max(-editable.FloatingCursorAddedMargin.Left, startCenter.X - 270);
            double boundedY = startCenter.Y - lineHeight / 2;
            int expected = editable
                .GetPositionForPoint(editable.LocalToGlobal(new Point(boundedX, boundedY + lineHeight / 2)))
                .Offset;

            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
            tester.PumpAndSettle();

            Assert.False(editable.FloatingCursorOn);
            // Dart's test font and the headless paragraph both use 14px glyphs, so Dart's offset holds.
            Assert.Equal(10, expected);
            Assert.True(controller.Selection.IsCollapsed);
            Assert.Equal(expected, controller.Selection.BaseOffset);
            Assert.Equal(SelectionChangedCause.ForcePress, causes[^1]);
        }
    }

    [Fact]
    public void FloatingCursor_CanEndWithoutAnUpdate()
    {
        (FrameworkDartTester tester, TextEditingController controller, List<SelectionChangedCause?> causes) =
            PumpFloatingCursorField();
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
            tester.PumpAndSettle();

            Assert.Equal(29, controller.Selection.BaseOffset);
            Assert.Equal(SelectionChangedCause.ForcePress, causes[^1]);
            Assert.Null(tester.TakeException());
        }
    }

    [Fact]
    public void FloatingCursor_StartingDuringTheResetAnimationDoesNotCrash()
    {
        (FrameworkDartTester tester, TextEditingController _, List<SelectionChangedCause?> _) =
            PumpFloatingCursorField();
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start, new Point(20, 20)));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Update, new Point(-250, 20)));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start, new Point(20, 20)));
            tester.PumpAndSettle();
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Update, new Point(-250, 20)));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
            tester.PumpAndSettle();

            Assert.Null(tester.TakeException());
            Assert.False(Editable(tester).FloatingCursorOn);
        }
    }

    [Fact]
    public void FloatingCursor_KeepsASelectionTheKeyboardMadeDuringTheGesture()
    {
        (FrameworkDartTester tester, TextEditingController controller, List<SelectionChangedCause?> causes) =
            PumpFloatingCursorField();
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start, new Point(0, 0)));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Update, new Point(-56, 0)));
            state.UpdateEditingValue(controller.Value.CopyWith(selection: new TextSelection(0, 4)));
            Assert.Equal(SelectionChangedCause.ForcePress, causes[^1]);

            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
            tester.PumpAndSettle();

            Assert.Equal(new TextSelection(0, 4), controller.Selection);
        }
    }

    [Fact]
    public void FloatingCursor_DoesNotLeakItsResetAnimation()
    {
        (FrameworkDartTester tester, TextEditingController _, List<SelectionChangedCause?> _) =
            PumpFloatingCursorField();
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start, new Point(0, 0)));
            tester.Pump(TimeSpan.FromMilliseconds(150));
            tester.Pump(TimeSpan.FromMilliseconds(500));
            state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End, new Point(0, 0)));
            Assert.True(Scheduler.TransientCallbackCount > 0);

            tester.PumpWidget(new SizedBox());
            tester.Pump();
            Assert.Equal(0, Scheduler.TransientCallbackCount);
        }
    }

    // --------------------------------------------------------- autocorrection prompt

    [Fact]
    public void AutocorrectionRect_AppearsOnDemandAndDismissesOnTextChangeOrFocusLoss()
    {
        using var tester = new FrameworkDartTester();
        var controller = new TextEditingController("ABCDEFG");
        var focusNode = new FocusNode();
        Color red = new Color(0xFFFF0000);
        tester.PumpWidget(Ltr(Field(controller, focusNode, autocorrectionTextRectColor: red)));
        focusNode.RequestFocus();
        tester.Pump();

        EditableRenderObjectWidget Editable() =>
            (EditableRenderObjectWidget)tester.ElementsOfType<EditableRenderObjectWidget>().Single().Widget;

        Assert.Null(Editable().PromptRectRange);
        Assert.Equal(red, Editable().PromptRectColor);

        State(tester).ShowAutocorrectionPromptRect(0, 1);
        tester.Pump();
        Assert.Equal(new TextRange(0, 1), Editable().PromptRectRange);

        // A selection-only change keeps it.
        State(tester).UpdateEditingValue(controller.Value.CopyWith(selection: TextSelection.Collapsed(2)));
        tester.Pump();
        Assert.Equal(new TextRange(0, 1), Editable().PromptRectRange);

        State(tester).UpdateEditingValue(new TextEditingValue("12345", TextSelection.Collapsed(5)));
        tester.Pump();
        Assert.Null(Editable().PromptRectRange);

        State(tester).ShowAutocorrectionPromptRect(0, 1);
        tester.Pump();
        Assert.Equal(new TextRange(0, 1), Editable().PromptRectRange);

        focusNode.Unfocus();
        tester.PumpAndSettle();
        Assert.Null(Editable().PromptRectRange);
    }

    // ----------------------------------------------------- context menu on scroll

    private static (FrameworkDartTester Tester, TextEditingController Controller, ScrollController Outer)
        PumpToolbarField(TargetPlatform platform, ScrollController? innerController = null)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester();
        var controller = new TextEditingController(
            string.Concat(Enumerable.Repeat("Atwater Peel Sherbrooke Bonaventure ", 20)));
        controller.Selection = new TextSelection(0, 7);
        var outer = new ScrollController();
        tester.PumpWidget(Ltr(new Overlay(initialEntries:
        [
            new OverlayEntry(_ => new ScrollNotificationObserver(child: new ListView(
                controller: outer,
                children:
                [
                    new SizedBox(height: 100),
                    Field(
                        controller,
                        readOnly: true,
                        scrollController: innerController,
                        contextMenuBuilder: (_, _) => new SizedBox(width: 10, height: 10),
                        selectionControls: TestTextSelectionHandleControls.Instance),
                    new SizedBox(height: 1000),
                ]))),
        ])));

        // Dart long-presses the first word; the selection change creates the selection overlay
        // that `showToolbar` needs.
        RenderEditable editable = State(tester).RenderEditableObject;
        editable.SelectWordsInRange(
            from: editable.LocalToGlobal(new Point(1, 1)),
            cause: SelectionChangedCause.LongPress);
        tester.Pump();
        return (tester, controller, outer);
    }

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.IOS)]
    public void Toolbar_HidesOnParentScrollStartAndReappearsOnScrollEnd(TargetPlatform platform)
    {
        (FrameworkDartTester tester, TextEditingController _, ScrollController outer) = PumpToolbarField(platform);
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            Assert.True(state.ShowToolbar());
            tester.Pump();
            Assert.True(state.ContextMenuIsVisible);

            outer.AnimateTo(10.0, TimeSpan.FromMilliseconds(100));
            tester.Pump();
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.False(state.ContextMenuIsVisible);

            tester.PumpAndSettle();
            Assert.Equal(10.0, outer.Offset);
            Assert.True(state.ContextMenuIsVisible);
        }
    }

    [Fact]
    public void Toolbar_DoesNotReappearWhenTheValueChangesDuringTheScroll()
    {
        (FrameworkDartTester tester, TextEditingController controller, ScrollController outer) =
            PumpToolbarField(TargetPlatform.Android);
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            Assert.True(state.ShowToolbar());
            tester.Pump();

            outer.AnimateTo(10.0, TimeSpan.FromMilliseconds(100));
            tester.Pump();
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.False(state.ContextMenuIsVisible);

            controller.Value = new TextEditingValue("a different value");
            tester.PumpAndSettle();
            Assert.False(state.ContextMenuIsVisible);
        }
    }

    [Fact]
    public void Toolbar_HidesOnTheFieldsOwnScrollAndReappearsWhileTheSelectionIsVisible()
    {
        var inner = new ScrollController();
        (FrameworkDartTester tester, TextEditingController _, ScrollController _) =
            PumpToolbarField(TargetPlatform.Android, inner);
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            Assert.True(state.ShowToolbar());
            tester.Pump();

            inner.AnimateTo(5.0, TimeSpan.FromMilliseconds(100));
            tester.Pump();
            tester.Pump(TimeSpan.FromMilliseconds(50));
            Assert.False(state.ContextMenuIsVisible);

            tester.PumpAndSettle();
            Assert.Equal(5.0, inner.Offset);
            Assert.True(state.ContextMenuIsVisible);
        }
    }

    [Fact]
    public void Toolbar_StaysVisibleOnParentScrollOnDesktop()
    {
        (FrameworkDartTester tester, TextEditingController _, ScrollController outer) =
            PumpToolbarField(TargetPlatform.Windows);
        using (tester)
        {
            EditableText.EditableTextState state = State(tester);
            Assert.True(state.ShowToolbar());
            tester.Pump();

            outer.JumpTo(10);
            tester.PumpAndSettle();
            Assert.True(state.ContextMenuIsVisible);
        }
    }

    /// Dart's `NoImplicitScrollPhysics` from editable_text_show_on_screen_test.dart.
    private sealed class NoImplicitScrollPhysics(ScrollPhysics? parent = null)
        : AlwaysScrollableScrollPhysics(parent)
    {
        public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor) =>
            new NoImplicitScrollPhysics(BuildParent(ancestor));

        public override bool AllowImplicitScrolling => false;
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public List<(Size Size, Matrix4 Transform)> SizeAndTransforms { get; } = [];

        public List<Rect> CaretRects { get; } = [];

        public List<Rect> ComposingRects { get; } = [];

        public override void SetEditableSizeAndTransform(Size editableBoxSize, Matrix4 transform) =>
            SizeAndTransforms.Add((editableBoxSize, transform));

        public override void SetCaretRect(Rect rect) => CaretRects.Add(rect);

        public override void SetComposingRect(Rect rect) => ComposingRects.Add(rect);
    }
}
