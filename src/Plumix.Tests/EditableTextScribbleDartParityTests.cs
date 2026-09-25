
using Avalonia;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Color = Avalonia.Media.Color;
using TextDirection = Plumix.UI.TextDirection;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_scribble_test.dart —
// iOS Scribble: the selection rects `EditableText` sends while connected, the scribble cause, the
// scribble element (`_ScribbleFocusable`) and the text placeholders.
public sealed class EditableTextScribbleDartParityTests : IDisposable
{
    private readonly RecordingTextInputControl _control = new();
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new(debugLabel: "EditableText Node");
    private readonly ScrollController _scrollController = new();

    public EditableTextScribbleDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        UI.TextInput.DebugReset();
        UI.TextInput.SetInputControl(_control);
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
    }

    public void Dispose()
    {
        UI.TextInput.RestorePlatformInputControl();
        UI.TextInput.DebugReset();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static readonly SelectionRect[] ExpectedRects =
    [
        new(position: 0, bounds: new Rect(0.0, 0.0, 14.0, 14.0)),
        new(position: 1, bounds: new Rect(14.0, 0.0, 14.0, 14.0)),
        new(position: 2, bounds: new Rect(28.0, 0.0, 14.0, 14.0)),
        new(position: 3, bounds: new Rect(42.0, 0.0, 14.0, 14.0)),
        new(position: 4, bounds: new Rect(56.0, 0.0, 14.0, 14.0)),
    ];

    private Widget Field(
        double? width = null,
        double? height = null,
        TextAlign textAlign = TextAlign.Start,
        int? maxLines = null,
        bool readOnly = false,
        bool stylusHandwritingEnabled = true,
        Action<TextSelection, SelectionChangedCause?>? onSelectionChanged = null) =>
        new MediaQuery(
            new MediaQueryData(Size: new Size(800, 600)),
            new Directionality(
                TextDirection.Ltr,
                new Align(
                    alignment: Alignment.TopLeft,
                    child: new SizedBox(
                        width: width,
                        height: height,
                        child: new EditableText(
                            controller: _controller,
                            textAlign: textAlign,
                            scrollController: _scrollController,
                            multiline: maxLines != 1,
                            maxLines: maxLines,
                            focusNode: _focusNode,
                            cursorWidth: 0,
                            padding: new Thickness(0),
                            style: new TextStyle(),
                            cursorColor: Color.FromUInt32(0xFF0000FF),
                            backgroundCursorColor: Color.FromUInt32(0xFF888888),
                            readOnly: readOnly,
                            stylusHandwritingEnabled: stylusHandwritingEnabled,
                            onSelectionChanged: onSelectionChanged)))));

    private static EditableText.EditableTextState State(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>();

    private static RenderEditable Editable(FrameworkDartTester tester) =>
        tester.AllElements().Select(element => element.RenderObject).OfType<RenderEditable>().Distinct().Single();

    // flutter_test's `showKeyboard`: focus the field and let the frame open its connection.
    private void ShowKeyboard(FrameworkDartTester tester)
    {
        _focusNode.RequestFocus();
        tester.Pump();
        tester.Pump();
    }

    private static void Inbound(string method, params object?[] arguments)
    {
        MethodChannel channel = SystemChannels.TextInput;
        channel.BinaryMessenger.HandlePlatformMessage(
            channel.Name,
            channel.Codec.EncodeMethodCall(new MethodCall(method, arguments.ToList())),
            null);
    }

    [Fact]
    public void SelectionRectsReSentWhenRefocused()
    {
        _controller.Text = "Text1";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field());
        Assert.Empty(_control.SelectionRects);

        ShowKeyboard(tester);
        // First update.
        Assert.Equal(ExpectedRects, Assert.Single(_control.SelectionRects));
        _control.SelectionRects.Clear();

        tester.PumpAndSettle();
        Assert.Empty(_control.SelectionRects);

        _focusNode.Unfocus();
        tester.PumpAndSettle();
        Assert.Empty(_control.SelectionRects);

        _focusNode.RequestFocus();
        tester.PumpAndSettle();
        // Should re-receive the same rects.
        Assert.Equal(ExpectedRects, Assert.Single(_control.SelectionRects));
    }

    [Fact]
    public void SelectionRectsAreNotSentIfStylusHandwritingIsDisabled()
    {
        _controller.Text = "Text1";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(stylusHandwritingEnabled: false));

        ShowKeyboard(tester);
        tester.PumpAndSettle();

        Assert.Empty(_control.SelectionRects);
    }

    [Fact]
    public void SelectionRectsAreSentWhenTheyChange()
    {
        _controller.Text = "Text1";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field());
        ShowKeyboard(tester);
        Assert.Equal(ExpectedRects, Assert.Single(_control.SelectionRects));
        _control.SelectionRects.Clear();

        // Pumping again with the same configuration sends nothing: the cache key matches.
        tester.PumpWidget(Field());
        tester.Pump();
        Assert.Empty(_control.SelectionRects);

        // A width of 20 wraps every character onto its own line.
        tester.PumpWidget(Field(width: 20));
        tester.Pump();
        IReadOnlyList<SelectionRect> narrow = _control.SelectionRects.Last();
        Assert.Equal(5, narrow.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(new SelectionRect(position: i, bounds: new Rect(0, 14.0 * i, 14.0, 14.0)), narrow[i]);
        }
    }

    [Fact]
    public void SelectionChangesDuringScribbleInteractionHaveTheScribbleCause()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        SelectionChangedCause? selectionCause = null;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(onSelectionChanged: (_, cause) => selectionCause = cause));
        ShowKeyboard(tester);
        EditableText.EditableTextState state = State(tester);

        // A normal selection update from the framework has the keyboard cause.
        state.UpdateEditingValue(new TextEditingValue(_controller.Text, new TextSelection(2, 3)));
        Assert.Equal(SelectionChangedCause.Keyboard, selectionCause);

        Inbound("TextInputClient.scribbleInteractionBegan");

        // A selection update during a scribble interaction has the scribble cause.
        state.UpdateEditingValue(new TextEditingValue(_controller.Text, new TextSelection(3, 4)));
        Assert.Equal(SelectionChangedCause.StylusHandwriting, selectionCause);

        Inbound("TextInputClient.scribbleInteractionFinished");
    }

    [Fact]
    public void RequestsFocusAndChangesTheSelectionWhenOnScribbleFocusIsCalled()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        SelectionChangedCause? selectionCause = null;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(onSelectionChanged: (_, cause) => selectionCause = cause));

        string elementIdentifier = UI.TextInput.ScribbleClients.Keys.First();
        Inbound("TextInputClient.focusElement", elementIdentifier, 0.0, 0.0);
        tester.Pump();

        Assert.True(_focusNode.HasFocus);
        Assert.Equal(SelectionChangedCause.StylusHandwriting, selectionCause);
    }

    [Fact]
    public void DeclaresItselfForScribbleIfTheBoundsOverlapTheScribbleRectAndTheWidgetIsTouchable()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(width: 800, height: 600));
        IScribbleClient client = UI.TextInput.ScribbleClients.Values.Single();

        Assert.True(client.IsInScribbleRect(new Rect(0, 0, 1, 1)));
        Assert.Equal(new Rect(0, 0, 800, 600), client.Bounds);
        Assert.False(client.IsInScribbleRect(new Rect(-1, -1, 1, 1)));

        tester.PumpWidget(Field(width: 800, height: 600, readOnly: true));
        Assert.False(UI.TextInput.ScribbleClients.Values.Single().IsInScribbleRect(new Rect(0, 0, 1, 1)));

        // Covered by another widget: the hit test does not reach the editable.
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Stack(children:
        [
            Field(width: 800, height: 600),
            Positioned.Fill(child: new Container(color: Color.FromUInt32(0xFF000000))),
        ])));
        Assert.False(UI.TextInput.ScribbleClients.Values.Single().IsInScribbleRect(new Rect(0, 0, 1, 1)));

        tester.PumpWidget(Field(width: 800, height: 600, stylusHandwritingEnabled: false));
        Assert.Empty(UI.TextInput.ScribbleClients);
    }

    [Fact]
    public void SingleLineScribbleFieldsCanShowAHorizontalPlaceholder()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(maxLines: 1));
        ShowKeyboard(tester);
        _controller.Selection = TextSelection.Collapsed(5);
        tester.Pump();

        State(tester).InsertTextPlaceholder(new Size(25, 25));
        tester.Pump();

        var text = (TextSpan)Editable(tester).Text!;
        Assert.Equal(3, text.Children!.Count);
        Assert.Equal("Lorem", ((TextSpan)text.Children[0]).Text);
        Assert.IsAssignableFrom<WidgetSpan>(text.Children[1]);
        Assert.Equal(" ipsum dolor sit amet", ((TextSpan)text.Children[2]).Text);

        State(tester).RemoveTextPlaceholder();
        tester.Pump();

        text = (TextSpan)Editable(tester).Text!;
        Assert.Null(text.Children);
        Assert.Equal("Lorem ipsum dolor sit amet", text.Text);
    }

    [Fact]
    public void MultilineScribbleFieldsCanShowAVerticalPlaceholder()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(maxLines: 2));
        ShowKeyboard(tester);
        _controller.Selection = TextSelection.Collapsed(5);
        tester.Pump();

        State(tester).InsertTextPlaceholder(new Size(25, 25));
        tester.Pump();

        var text = (TextSpan)Editable(tester).Text!;
        Assert.Equal(4, text.Children!.Count);
        Assert.Equal("Lorem", ((TextSpan)text.Children[0]).Text);
        Assert.IsAssignableFrom<WidgetSpan>(text.Children[1]);
        Assert.IsAssignableFrom<WidgetSpan>(text.Children[2]);
        Assert.Equal(" ipsum dolor sit amet", ((TextSpan)text.Children[3]).Text);

        State(tester).RemoveTextPlaceholder();
        tester.Pump();

        text = (TextSpan)Editable(tester).Text!;
        Assert.Null(text.Children);
        Assert.Equal("Lorem ipsum dolor sit amet", text.Text);
    }

    [Fact]
    public void PlaceholdersAreIgnoredWhenStylusHandwritingIsDisabled()
    {
        _controller.Text = "Lorem ipsum dolor sit amet";
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Field(maxLines: 1, stylusHandwritingEnabled: false));
        ShowKeyboard(tester);
        _controller.Selection = TextSelection.Collapsed(5);
        tester.Pump();

        State(tester).InsertTextPlaceholder(new Size(25, 25));
        tester.Pump();

        var text = (TextSpan)Editable(tester).Text!;
        Assert.Null(text.Children);
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public List<IReadOnlyList<SelectionRect>> SelectionRects { get; } = [];

        public override void SetSelectionRects(IReadOnlyList<SelectionRect> selectionRects) =>
            SelectionRects.Add(selectionRects.ToList());
    }
}
