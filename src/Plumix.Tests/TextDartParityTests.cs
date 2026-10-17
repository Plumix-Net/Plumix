using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextSelection = Plumix.Widgets.TextSelection;

// Ports the Text cases of flutter/packages/flutter/test/widgets/text_test.dart that depend on the
// strict Text build and RenderParagraph semantics ('Text uses TextStyle.overflow', the two 'Mouse
// hovering over selectable Text ...' cases, the five WidgetSpan semantics-order cases),
// text_semantics_test.dart ('... the SemanticsNode contains the correct language tag' with and
// without a SelectionContainer, 'Text with semanticsIdentifier creates its own semantic node'), and
// the selectable_region_test.dart cases that run through `_SelectableTextContainerDelegate`
// ('SelectionListener onSelectionChanged is accurate with WidgetSpans', 'Multiple selectables on a
// single line should be in screen order', 'triple-click-drag backwards involving WidgetSpans').

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TextDartParityTests : IDisposable
{
    private readonly MockClipboardPlatform _clipboard = new("empty");

    public TextDartParityTests()
    {
        // A flutter_test binding starts with no lifecycle state, so a SelectableRegion keeps its
        // selection when it loses focus; do not inherit a state an earlier test left behind.
        SchedulerBinding.Instance.ResetInternalState();
    }

    public void Dispose()
    {
        _clipboard.Dispose();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    // selectable_region_test.dart's top-level `textOffsetToPosition`.
    private static Point TextOffsetToPosition(RenderParagraph paragraph, int offset)
    {
        var caret = new Rect(0.0, 0.0, 2.0, 20.0);
        Point localOffset = paragraph.GetOffsetForCaret(new TextPosition(offset), caret)
                            + new Vector(0.0, paragraph.PreferredLineHeight);
        return paragraph.LocalToGlobal(localOffset) + new Vector(0.0, -2.0);
    }

    // selectable_region_test.dart's `_selectableRegion`.
    private static SelectableRegion SelectableRegion(Widget child, FocusNode? focusNode = null) => new(
        child: child,
        selectionControls: TestTextSelectionHandleControls.Instance,
        focusNode: focusNode,
        contextMenuBuilder: (_, _) => SizedBox.Shrink(),
        magnifierConfiguration: TextMagnifierConfiguration.Disabled);

    private static RenderParagraph ParagraphOf(FrameworkDartTester tester, Finder text) =>
        tester.RenderObject<RenderParagraph>(Find.Descendant(of: text, matching: Find.ByType<RichText>()).First);

    // Flutter: 'text_test.dart: Text uses TextStyle.overflow'
    [Fact]
    public void TextUsesTextStyleOverflow()
    {
        using var tester = new FrameworkDartTester();
        const TextOverflow overflow = TextOverflow.Fade;
        tester.PumpWidget(new Text(
            "Hello World",
            textDirection: TextDirection.Ltr,
            style: new TextStyle(Overflow: overflow)));

        RichText richText = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Equal(overflow, richText.Overflow);
        Assert.Equal(overflow, richText.Text.Style!.Overflow);
    }

    // Flutter: 'text_test.dart: Mouse hovering over selectable Text uses SystemMouseCursor.text'
    [Fact]
    public void MouseHoveringOverSelectableTextUsesSystemMouseCursorText()
    {
        using var tester = new FrameworkDartTester();
        using var focusNode = new FocusNode();
        tester.PumpWidget(new TestWidgetsApp(home: new SelectableRegion(
            selectionControls: EmptyTextSelectionControls.Instance,
            focusNode: focusNode,
            child: new Text("Flutter"))));

        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(Find.Text("Flutter")));
        tester.Pump();

        Assert.Equal(SystemMouseCursors.Text, RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer));
        gesture.RemovePointer();
    }

    // Flutter: 'text_test.dart: Mouse hovering over selectable Text uses default selection style mouse
    // cursor'
    [Fact]
    public void MouseHoveringOverSelectableTextUsesDefaultSelectionStyleMouseCursor()
    {
        using var tester = new FrameworkDartTester();
        using var focusNode = new FocusNode();
        tester.PumpWidget(new TestWidgetsApp(home: new SelectableRegion(
            selectionControls: EmptyTextSelectionControls.Instance,
            focusNode: focusNode,
            child: DefaultSelectionStyle.Merge(
                mouseCursor: SystemMouseCursors.Click,
                child: new Text("Flutter")))));

        TestGesture gesture = tester.CreateGesture(PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(Find.Text("Flutter")));
        tester.Pump();

        Assert.Equal(SystemMouseCursors.Click, RendererBinding.Instance.MouseTracker.DebugDeviceActiveCursor(gesture.Pointer));
        gesture.RemovePointer();
    }

    // The `_SelectableTextContainer` composition: under a SelectionContainer the Text builds
    // MouseRegion > _SelectableTextContainer > SelectionContainer > _RichText > RichText, whose paragraph
    // registers with the inner container; without one it builds the RichText directly.
    [Fact]
    public void TextComposesASelectableTextContainerOnlyUnderASelectionContainer()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Text("Plain", textDirection: TextDirection.Ltr));
        Assert.Empty(tester.ElementsOfType<SelectionContainer>());
        RichText plain = tester.FirstWidget<RichText>(Find.ByType<RichText>());
        Assert.Null(plain.SelectionRegistrar);
        // The non-selectable branch still resolves a selection color.
        Assert.Equal(DefaultSelectionStyle.DefaultColor, plain.SelectionColor);

        using var focusNode = new FocusNode();
        tester.PumpWidget(new TestWidgetsApp(home: new SelectableRegion(
            selectionControls: EmptyTextSelectionControls.Instance,
            focusNode: focusNode,
            child: new Text("Selectable", selectionColor: new Color(0xFF00FF00)))));
        tester.PumpAndSettle();

        Element textElement = tester.ElementsOfType<Text>().Single();
        Element container = tester.ElementsOfType<SelectionContainer>()
            .Single(element => IsDescendant(element, of: textElement));
        RichText richText = tester.Widget<RichText>(Find.Descendant(
            of: Find.ByElement(container),
            matching: Find.ByType<RichText>()));
        Assert.Same(SelectionContainer.MaybeOf(FirstChild(container)), richText.SelectionRegistrar);
        Assert.Equal(new Color(0xFF00FF00), richText.SelectionColor);
        Assert.IsType<GlobalKey>(richText.Key, exactMatch: false);
    }

    private static bool IsDescendant(Element element, Element of)
    {
        bool found = false;
        element.VisitAncestorElements(ancestor =>
        {
            found = ReferenceEquals(ancestor, of);
            return !found;
        });
        return found;
    }

    private static Element FirstChild(Element element)
    {
        Element? child = null;
        element.VisitChildren(candidate => child ??= candidate);
        return child!;
    }

    [Fact]
    public void TextReadsTheDefaultTextHeightBehaviorWhenTheDefaultTextStyleHasNone()
    {
        using var tester = new FrameworkDartTester();
        var behavior = new TextHeightBehavior(ApplyHeightToFirstAscent: false);
        tester.PumpWidget(new DefaultTextHeightBehavior(
            textHeightBehavior: behavior,
            child: new Text("Hello", textDirection: TextDirection.Ltr)));
        Assert.Equal(behavior, tester.FirstWidget<RichText>(Find.ByType<RichText>()).TextHeightBehavior);

        var styleBehavior = new TextHeightBehavior(ApplyHeightToLastDescent: false);
        tester.PumpWidget(new DefaultTextHeightBehavior(
            textHeightBehavior: behavior,
            child: new DefaultTextStyle(
                style: new TextStyle(),
                textHeightBehavior: styleBehavior,
                child: new Text("Hello", textDirection: TextDirection.Ltr))));
        Assert.Equal(styleBehavior, tester.FirstWidget<RichText>(Find.ByType<RichText>()).TextHeightBehavior);
    }

    // Flutter: 'text_semantics_test.dart: GIVEN a Text widget with a locale WHEN semantics are built
    // THEN the SemanticsNode contains the correct language tag'
    [Fact]
    public void TextLocaleReachesTheSemanticsLanguageTag()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        var locale = new Locale("de", "DE");
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Text("Flutter 2050", locale: locale)));

        SemanticsNode node = tester.GetSemantics(Find.ByType<Directionality>());
        var localeStringAttribute = (LocaleStringAttribute)node.AttributedLabel.Attributes[0];
        Assert.Equal("Flutter 2050", node.Label);
        Assert.Equal("de-DE", localeStringAttribute.Locale);        handle.Dispose();
    }

    // Flutter: 'text_semantics_test.dart: GIVEN a Text with a locale is within a SelectionContainer
    // WHEN semantics are built THEN the SemanticsNode contains the correct language tag'
    [Fact]
    public void TextLocaleReachesTheSemanticsLanguageTagInsideASelectionContainer()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        const string text = "Flutter 2050";
        tester.PumpWidget(new TestWidgetsApp(home: new SelectableRegion(
            selectionControls: EmptyTextSelectionControls.Instance,
            child: new Text(text, locale: new Locale("de", "DE")))));
        tester.PumpAndSettle();

        var queue = new List<SemanticsNode> { tester.SemanticsRootNode! };
        SemanticsNode? targetNode = null;
        while (queue.Count > 0)
        {
            SemanticsNode node = queue[0];
            queue.RemoveAt(0);
            if (node.Label == text)
            {
                targetNode = node;
                break;
            }

            queue.AddRange(node.DebugListChildrenInOrder(DebugSemanticsDumpOrder.TraversalOrder));
        }

        var localeStringAttribute = (LocaleStringAttribute)targetNode!.AttributedLabel.Attributes[0];
        Assert.Equal(text, targetNode.Label);
        Assert.Equal("de-DE", localeStringAttribute.Locale);        handle.Dispose();
    }

    // Flutter: 'text_semantics_test.dart: Text with semanticsIdentifier creates its own semantic node'
    [Fact]
    public void TextWithSemanticsIdentifierCreatesItsOwnSemanticNode()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Text("Hello World", semanticsIdentifier: "my-text-identifier")));

        SemanticsNode node = tester.GetSemantics(Find.Text("Hello World"));
        Assert.Equal("my-text-identifier", node.Identifier);
        Assert.Equal("Hello World", node.Label);        handle.Dispose();
    }

    private static WidgetSpan BaselineSpan(Widget child) => new(
        child: child,
        alignment: PlaceholderAlignment.Baseline,
        baseline: TextBaseline.Alphabetic);

    // Pumps the rich text, checks the Text's merged semantics label, then marks the Directionality's
    // render object dirty and checks again: a clean paragraph must reuse its attributed-label cache.
    private static void AssertSemanticsLabelTwice(
        InlineSpan span,
        string label,
        string? childLabel = null)
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, Text.Rich(span)));

        void Check()
        {
            SemanticsNode node = tester.GetSemantics(Find.ByType<Text>().First);
            Assert.Equal(label, node.Label);
            IReadOnlyList<SemanticsNode> children = node.DebugListChildrenInOrder(DebugSemanticsDumpOrder.TraversalOrder);
            if (childLabel is null)
            {
                Assert.Empty(children);
            }
            else
            {
                Assert.Equal(childLabel, Assert.Single(children).Label);
            }
        }

        Check();
        tester.RenderObject<RenderObject>(Find.ByType<Directionality>()).MarkNeedsSemanticsUpdate();
        tester.PumpAndSettle();
        Check();
        handle.Dispose();
    }

    // Flutter: 'text_test.dart: semantics label is in order when uses widget span'
    [Fact]
    public void SemanticsLabelIsInOrderWhenUsesWidgetSpan()
    {
        AssertSemanticsLabelTwice(
            new TextSpan(children:
            [
                new TextSpan(text: "before "),
                BaselineSpan(new Semantics(label: "foo")),
                new TextSpan(text: " after"),
            ]),
            "before \nfoo\n after");
    }

    // Flutter: 'text_test.dart: semantics label is in order with nested rich text widget spans'
    [Fact]
    public void SemanticsLabelIsInOrderWithNestedRichTextWidgetSpans()
    {
        using var tester = new FrameworkDartTester();
        SemanticsHandle handle = tester.EnsureSemantics();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            Text.Rich(new TextSpan(children:
            [
                new WidgetSpan(child: Text.Rich(new TextSpan(children:
                [
                    new WidgetSpan(child: new Text("before")),
                    new TextSpan(text: "foo"),
                    new WidgetSpan(child: new Text("after")),
                ]))),
            ]))));

        Assert.Equal("before\nfoo\nafter", tester.GetSemantics(Find.ByType<Text>().First).Label);
        handle.Dispose();
    }

    // Flutter: 'text_test.dart: semantics can handle some widget spans without semantics'
    [Fact]
    public void SemanticsCanHandleSomeWidgetSpansWithoutSemantics()
    {
        AssertSemanticsLabelTwice(
            new TextSpan(children:
            [
                new TextSpan(text: "before "),
                BaselineSpan(new SizedBox(width: 10.0)),
                new TextSpan(text: " mid"),
                BaselineSpan(new Semantics(label: "foo")),
                new TextSpan(text: " after"),
                BaselineSpan(new SizedBox(width: 10.0)),
            ]),
            "before \n mid\nfoo\n after");
    }

    // Flutter: 'text_test.dart: semantics can handle all widget spans without semantics'
    [Fact]
    public void SemanticsCanHandleAllWidgetSpansWithoutSemantics()
    {
        AssertSemanticsLabelTwice(
            new TextSpan(children:
            [
                new TextSpan(text: "before "),
                BaselineSpan(new SizedBox(width: 10.0)),
                new TextSpan(text: " mid"),
                BaselineSpan(new SizedBox(width: 10.0)),
                new TextSpan(text: " after"),
                BaselineSpan(new SizedBox(width: 10.0)),
            ]),
            "before \n mid\n after");
    }

    // Flutter: 'text_test.dart: semantics can handle widget spans with explicit semantics node'
    [Fact]
    public void SemanticsCanHandleWidgetSpansWithExplicitSemanticsNode()
    {
        AssertSemanticsLabelTwice(
            new TextSpan(children:
            [
                new TextSpan(text: "before "),
                BaselineSpan(new Semantics(
                    label: "inner",
                    container: true,
                    child: new SizedBox(width: 10, height: 10))),
                new TextSpan(text: " after"),
            ]),
            "before \n after",
            childLabel: "inner");
    }

    // Flutter: 'selectable_region_test.dart: SelectionListener onSelectionChanged is accurate with
    // WidgetSpans'
    [Fact]
    public void SelectionListenerOnSelectionChangedIsAccurateWithWidgetSpans()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        string[] dataModel = ["Hello world, ", "how are you today."];
        var selectionNotifier = new SelectionListenerNotifier();

        tester.PumpWidget(new TestWidgetsApp(home: SelectableRegion(new SelectionListener(
            selectionNotifier: selectionNotifier,
            child: new Column(children:
            [
                Text.Rich(new TextSpan(
                    text: dataModel[0],
                    children: [new WidgetSpan(child: new Text(dataModel[1]))])),
            ])))));

        RenderParagraph paragraph1 = ParagraphOf(tester, Find.TextContaining("Hello world"));
        RenderParagraph paragraph2 = ParagraphOf(tester, Find.Text("how are you today."));
        TestGesture mouseGesture = tester.StartGesture(
            TextOffsetToPosition(paragraph1, 0),
            PointerDeviceKind.Mouse);
        tester.Pump();

        void AssertRange(SelectionStatus status, int start, int end)
        {
            Assert.Equal(status, selectionNotifier.Selection.Status);
            SelectedContentRange? selectedRange = selectionNotifier.Selection.Range;
            Assert.NotNull(selectedRange);
            Assert.Equal(start, selectedRange!.StartOffset);
            Assert.Equal(end, selectedRange.EndOffset);
        }

        // Selection on paragraph1.
        mouseGesture.MoveTo(TextOffsetToPosition(paragraph1, 1));
        tester.PumpAndSettle();
        AssertRange(SelectionStatus.Uncollapsed, 0, 1);

        // Selection on paragraph1.
        mouseGesture.MoveTo(TextOffsetToPosition(paragraph1, 10));
        tester.PumpAndSettle();
        AssertRange(SelectionStatus.Uncollapsed, 0, 10);

        // Selection on paragraph1 and paragraph2.
        mouseGesture.MoveTo(TextOffsetToPosition(paragraph2, 10));
        tester.PumpAndSettle();
        AssertRange(SelectionStatus.Uncollapsed, 0, 23);
        mouseGesture.Up();
        tester.Pump();
        AssertRange(SelectionStatus.Uncollapsed, 0, 23);

        // Collapsed selection.
        mouseGesture.Down(TextOffsetToPosition(paragraph2, 3));
        tester.Pump();
        mouseGesture.Up();
        tester.PumpAndSettle(GestureConstants.DoubleTapTimeout);
        AssertRange(SelectionStatus.Collapsed, 16, 16);

        // Backwards selection.
        mouseGesture.Down(TextOffsetToPosition(paragraph2, 4));
        tester.Pump();
        mouseGesture.MoveTo(TextOffsetToPosition(paragraph1, 0));
        tester.PumpAndSettle();
        AssertRange(SelectionStatus.Uncollapsed, 17, 0);
        mouseGesture.Up();
        tester.Pump();
        AssertRange(SelectionStatus.Uncollapsed, 17, 0);

        // Collapsed selection.
        mouseGesture.Down(TextOffsetToPosition(paragraph1, 0));
        tester.PumpAndSettle();
        mouseGesture.Up();
        tester.PumpAndSettle(GestureConstants.DoubleTapTimeout);
        AssertRange(SelectionStatus.Collapsed, 0, 0);
        mouseGesture.RemovePointer();
    }

    // Flutter: 'selectable_region_test.dart: Multiple selectables on a single line should be in screen
    // order'
    [Fact]
    public void MultipleSelectablesOnASingleLineShouldBeInScreenOrder()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var outerText = new UniqueKey();
        var textStyle = new TextStyle(FontSize: 10);

        tester.PumpWidget(new TestWidgetsApp(home: SelectableRegion(new Center(child: Text.Rich(
            new TextSpan(children:
            [
                new TextSpan(text: "Hello my name is ", style: textStyle),
                new WidgetSpan(
                    child: new Text("Dash", style: textStyle),
                    alignment: PlaceholderAlignment.Middle),
                new TextSpan(text: ".", style: textStyle),
            ]),
            key: outerText)))));

        RenderParagraph paragraph1 = ParagraphOf(tester, Find.ByKey(outerText));
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraph1, 0), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();

        // Select all.
        SendControlCombination(tester, LogicalKeyboardKey.KeyA);

        // keyboard copy.
        SendControlCombination(tester, LogicalKeyboardKey.KeyC);

        Assert.Equal("Hello my name is Dash.", _clipboard.Text);
        gesture.RemovePointer();
    }

    // keyboard_utils.dart's `sendKeyCombination(SingleActivator(key, control: true))`.
    private static void SendControlCombination(FrameworkDartTester tester, LogicalKeyboardKey key)
    {
        tester.SendKeyDownEvent(LogicalKeyboardKey.ControlLeft);
        tester.SendKeyEvent(key);
        tester.SendKeyUpEvent(LogicalKeyboardKey.ControlLeft);
        tester.Pump();
    }

    // Flutter: 'selectable_region_test.dart: triple-click-drag backwards involving WidgetSpans'
    [Fact]
    public void TripleClickDragBackwardsInvolvingWidgetSpans()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new TestWidgetsApp(home: new SelectableRegion(
            selectionControls: TestTextSelectionHandleControls.Instance,
            child: new ListView(children:
            [
                Text.Rich(
                    new TextSpan(children:
                    [
                        new WidgetSpan(child: new Text("Text A.")),
                        new TextSpan(text: "\n"),
                        new WidgetSpan(child: new Text("Text B.")),
                        new TextSpan(text: "\n"),
                        new WidgetSpan(child: new Text("Text C.")),
                    ]),
                    key: new ValueKey<string>("rich1")),
                Text.Rich(
                    new TextSpan(children:
                    [
                        new WidgetSpan(child: new Text("Text D.")),
                        new TextSpan(text: "\n"),
                        new WidgetSpan(child: new Text("Text E.")),
                        new TextSpan(text: "\n"),
                        new WidgetSpan(child: new Text("Text F.")),
                    ]),
                    key: new ValueKey<string>("rich2")),
            ]))));
        tester.PumpAndSettle();

        RenderParagraph paragraphE = ParagraphOf(tester, Find.Text("Text E."));
        RenderParagraph paragraphB = ParagraphOf(tester, Find.Text("Text B."));

        // Triple-click on Text E.
        TestGesture gesture = tester.StartGesture(TextOffsetToPosition(paragraphE, 2), PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraphE, 2));
        tester.Pump();
        gesture.Up();
        tester.Pump();

        gesture.Down(TextOffsetToPosition(paragraphE, 2));
        tester.PumpAndSettle();

        // Text E should be selected after triple-click.
        Assert.NotEmpty(paragraphE.Selections);
        Assert.Equal(new TextSelection(BaseOffset: 0, ExtentOffset: 7), paragraphE.Selections[0]);

        // Drag backward to Text B.
        gesture.MoveTo(TextOffsetToPosition(paragraphB, 3));
        tester.PumpAndSettle();

        RenderParagraph paragraphC = ParagraphOf(tester, Find.Text("Text C."));
        RenderParagraph paragraphD = ParagraphOf(tester, Find.Text("Text D."));
        RenderParagraph outerParagraph1 = ParagraphOf(tester, Find.ByKey(new ValueKey<string>("rich1")));
        RenderParagraph outerParagraph2 = ParagraphOf(tester, Find.ByKey(new ValueKey<string>("rich2")));
        gesture.Up();
        tester.PumpAndSettle();

        // When dragging backward from Text E to Text B, all paragraphs between
        // B and E should be fully selected in reverse.
        Assert.NotEmpty(paragraphB.Selections);
        Assert.NotEmpty(paragraphC.Selections);
        Assert.NotEmpty(paragraphD.Selections);
        Assert.NotEmpty(paragraphE.Selections);
        Assert.NotEmpty(outerParagraph1.Selections);
        Assert.NotEmpty(outerParagraph2.Selections);
        Assert.Equal(new TextSelection(BaseOffset: 7, ExtentOffset: 0), paragraphB.Selections[0]);
        Assert.Equal(new TextSelection(BaseOffset: 7, ExtentOffset: 0), paragraphC.Selections[0]);
        Assert.Equal(new TextSelection(BaseOffset: 7, ExtentOffset: 0), paragraphD.Selections[0]);
        Assert.Equal(new TextSelection(BaseOffset: 7, ExtentOffset: 0), paragraphE.Selections[0]);
        Assert.Equal(new TextSelection(BaseOffset: 4, ExtentOffset: 3), outerParagraph1.Selections[0]);
        Assert.Equal(new TextSelection(BaseOffset: 2, ExtentOffset: 1), outerParagraph2.Selections[0]);
        gesture.RemovePointer();
    }
}
