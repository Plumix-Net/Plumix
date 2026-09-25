using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextStyle = Plumix.Widgets.TextStyle;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/rendering/editable_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/editable_intrinsics_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/editable_gesture_test.dart
//
// Headless layout uses Flutter's `FlutterTest` font metrics (every glyph one em wide, ascent 0.75 em),
// so the expected geometry is Flutter's own. The `paints` matcher is reproduced over the canvas's debug
// call log (`CanvasCall`), painting through a `TestRecordingPaintingContext` exactly as flutter_test
// does. The render-level semantics tests at the end restate the RenderEditable behaviors that
// `editable_text_test.dart` asserts through the widget.

public sealed class RenderEditableDartParityTests
{
    private static readonly Color Black = new Color(0xFF000000);
    private static readonly Color Blue = new Color(0xFF0000FF);
    private static readonly Color Grey = new Color(0xFF9E9E9E);
    private static readonly Color Red = new Color(0xFFFF0000);

    private static double CaretMarginOf(RenderEditable renderEditable) => renderEditable.CursorWidth + 1.0;

    // -- editable_test.dart -------------------------------------------------------------------------

    [Fact]
    public void RenderEditable_RespectsClipBehavior()
    {
        const double viewportHeight = 100.0;
        const double maxWidth = 1.0;
        Clip?[] clips = [null, Clip.None, Clip.HardEdge, Clip.AntiAlias, Clip.AntiAliasWithSaveLayer];
        foreach (Clip? clip in clips)
        {
            var context = new TestClipPaintingContext();
            RenderEditable editable = clip is null
                ? NewEditable(
                    text: new TextSpan(text: new string('a', 10000)),
                    selection: new TextSelection(0, 0))
                : NewEditable(
                    text: new TextSpan(text: new string('a', 10000)),
                    selection: new TextSelection(0, 0),
                    clipBehavior: clip.Value);
            using var tester = new RenderingTester();
            tester.Layout(
                editable,
                new BoxConstraints(MaxHeight: viewportHeight, MaxWidth: maxWidth),
                EnginePhase.Composite);
            context.PaintChild(editable, default);
            Assert.Equal(clip ?? Clip.HardEdge, context.ClipBehavior);
        }
    }

    [Fact]
    public void ReportsTheRealHeightWhenMaxLinesIs1()
    {
        var text = new TextSpan(
            style: new TextStyle(FontSize: 10),
            children: [new TextSpan(text: "TALL", style: new TextStyle(FontSize: 100))]);
        RenderEditable editable = NewEditable(text: text);
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(600, 600)));
        Assert.Equal(100, editable.Size.Height);
    }

    [Fact]
    public void HasDefaultSemanticsInputType()
    {
        var editable = new TestRenderEditable(text: new TextSpan(text: "text"));
        var config = new SemanticsConfiguration();
        editable.InvokeDescribeSemanticsConfiguration(config);
        Assert.Equal(SemanticsInputType.Text, config.InputType);
    }

    [Fact]
    public void ReportsTheHeightOfTheFirstLineWhenMaxLinesIs1()
    {
        var text = new TextSpan(
            text: string.Concat(Enumerable.Repeat("liiiiines\n", 10)),
            style: new TextStyle(FontSize: 10));
        RenderEditable editable = NewEditable(text: text);
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(600, 600)));
        Assert.Equal(10, editable.Size.Height);
    }

    [Fact]
    public void Editable_RespectsClipBehaviorInDescribeApproximatePaintClip()
    {
        var editable = new TestRenderEditable(
            text: new TextSpan(text: new string('a', 10000)),
            selection: new TextSelection(0, 0),
            clipBehavior: Clip.None);
        using var tester = new RenderingTester();
        tester.Layout(editable);

        bool visited = false;
        editable.VisitChildren(child =>
        {
            visited = true;
            Assert.Null(editable.InvokeDescribeApproximatePaintClip(child));
        });
        Assert.True(visited);
    }

    [Fact]
    public void Paint_RespectsTheOffsetArgument()
    {
        var context = new TestPushLayerPaintingContext();
        RenderEditable editable = NewEditable(
            text: new TextSpan(text: "text", style: new TextStyle(FontSize: 20.0, Height: 1.0)),
            selection: new TextSelection(0, 0));
        using var tester = new RenderingTester();
        tester.Layout(editable, new BoxConstraints(MaxHeight: 1000.0, MaxWidth: 1000.0), EnginePhase.Composite);

        var paintOffset = new Point(100, 200);
        const double fontSize = 20.0;
        var endpoint = new Point(0.0, fontSize);

        editable.Paint(context, paintOffset);

        List<LeaderLayer> leaderLayers = context.PushedLayers.OfType<LeaderLayer>().ToList();
        Assert.Equal(2, leaderLayers.Count);
        // Collapsed selection: both handles are anchored at the caret's bottom.
        Assert.Equal(endpoint + paintOffset, leaderLayers[0].Offset);
        Assert.Equal(endpoint + paintOffset, leaderLayers[1].Offset);
    }

    [Fact]
    public void CorrectClipping()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), text: "A"),
            locale: "en_US",
            offset: ViewportOffset.Fixed(10.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);
        Assert.True(Paints(PaintEditable(editable), ClipRectStep(FromLTRB(0.0, 0.0, 500.0, 10.0))));
    }

    [Fact]
    public void CanChangeCursorColorRadiusVisibility()
    {
        var showCursor = new ValueNotifier<bool>(true);
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            cursorColor: Color.FromARGB(0xFF, 0xFF, 0x00, 0x00),
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: TextSelection.Collapsed(4, TextAffinity.Upstream));
        AssertCursorColorRadiusVisibility(editable, showCursor, BoxConstraints.Loose(new Size(100, 100)), false);
    }

    [Fact]
    public void CanChangeTextAlign()
    {
        RenderEditable editable = NewEditable(text: new TextSpan(text: "test"));
        using var tester = new RenderingTester();
        tester.Layout(editable);
        editable.Layout(BoxConstraints.Loose(new Size(100, 100)));
        Assert.Equal(TextAlign.Start, editable.TextAlign);
        Assert.False(editable.DebugNeedsLayout);

        editable.TextAlign = TextAlign.Center;
        Assert.Equal(TextAlign.Center, editable.TextAlign);
        Assert.True(editable.DebugNeedsLayout);
    }

    [Fact]
    public void CanReadPlainText()
    {
        RenderEditable editable = NewEditable(maxLines: null);
        Assert.Equal(string.Empty, editable.PlainText);

        editable.Text = new TextSpan(text: "123");
        Assert.Equal("123", editable.PlainText);

        editable.Text = new TextSpan(
            children:
            [
                new TextSpan(text: "abc", style: new TextStyle(FontSize: 12)),
                new TextSpan(text: "def", style: new TextStyle(FontSize: 10)),
            ]);
        Assert.Equal("abcdef", editable.PlainText);

        editable.Layout(BoxConstraints.TightFor(width: 200));
        Assert.Equal("abcdef", editable.PlainText);
    }

    [Fact]
    public void CursorWithIdeographicScript()
    {
        var showCursor = new ValueNotifier<bool>(true);
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            cursorColor: Color.FromARGB(0xFF, 0xFF, 0x00, 0x00),
            text: new TextSpan(
                text: "中文测试文本是否正确",
                style: new TextStyle(FontSize: 10.0, FontFamily: new FontFamily("FlutterTest"))),
            selection: TextSelection.Collapsed(4, TextAffinity.Upstream));
        AssertCursorColorRadiusVisibility(editable, showCursor, BoxConstraints.Loose(new Size(100, 100)), true);
    }

    private static void AssertCursorColorRadiusVisibility(
        RenderEditable editable,
        ValueNotifier<bool> showCursor,
        BoxConstraints constraints,
        bool layoutWithConstraints)
    {
        using var tester = new RenderingTester();
        if (layoutWithConstraints)
        {
            tester.Layout(editable, constraints);
        }
        else
        {
            tester.Layout(editable);
            editable.Layout(constraints);
        }

        tester.PumpFrame(EnginePhase.CompositingBits);

        // Don't paint the cursor unless showCursor says so.
        Assert.Equal(0, CountCalls(PaintEditable(editable), "drawRect"));

        editable.ShowCursor = showCursor;
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(Color.FromARGB(0xFF, 0xFF, 0x00, 0x00), new Rect(40, 0, 1, 10))));

        // Now change to a rounded caret.
        editable.CursorColor = Color.FromARGB(0xFF, 0x00, 0x00, 0xFF);
        editable.CursorWidth = 4;
        editable.CursorRadius = Radius.Circular(3);
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.True(Paints(
            PaintEditable(editable),
            RRectStep(
                Color.FromARGB(0xFF, 0x00, 0x00, 0xFF),
                RRect.FromRectAndRadius(new Rect(40, 0, 4, 10), Radius.Circular(3)))));

        editable.TextScaler = TextScaler.Linear(2.0);
        tester.PumpFrame(EnginePhase.CompositingBits);

        // Now the caret height is much bigger due to the bigger font scale.
        Assert.True(Paints(
            PaintEditable(editable),
            RRectStep(
                Color.FromARGB(0xFF, 0x00, 0x00, 0xFF),
                RRect.FromRectAndRadius(new Rect(80, 0, 4, 20), Radius.Circular(3)))));

        // Can turn off caret.
        showCursor.Value = false;
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.Equal(0, CountCalls(PaintEditable(editable), "drawRRect"));
    }

    [Fact]
    public void TextIsPaintedAboveSelection()
    {
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: new TextSelection(0, 3, TextAffinity.Upstream));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        IReadOnlyList<CanvasCall> calls = PaintEditable(editable);
        // Check that it's the black selection box, not the red cursor.
        Assert.True(Paints(calls, RectStep(Black), ParagraphStep()));
        // There is exactly one rect paint (1 selection, 0 cursor).
        Assert.Equal(1, CountCalls(calls, "drawRect"));
    }

    [Fact]
    public void CursorCanPaintAboveOrBelowTheText()
    {
        var showCursor = new ValueNotifier<bool>(true);
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            paintCursorAboveText: true,
            showCursor: showCursor,
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: TextSelection.Collapsed(2, TextAffinity.Upstream));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        IReadOnlyList<CanvasCall> calls = PaintEditable(editable);
        Assert.True(Paints(calls, ParagraphStep(), RectStep(Red)));
        // There is exactly one rect paint (0 selection, 1 cursor).
        Assert.Equal(1, CountCalls(calls, "drawRect"));

        editable.PaintCursorAboveText = false;
        tester.PumpFrame(EnginePhase.CompositingBits);

        calls = PaintEditable(editable);
        // The paint order is now flipped.
        Assert.True(Paints(calls, RectStep(Red), ParagraphStep()));
        Assert.Equal(1, CountCalls(calls, "drawRect"));
    }

    [Fact]
    public void DoesNotPaintTheCaretWhenSelectionIsNullOrInvalid()
    {
        var showCursor = new ValueNotifier<bool>(true);
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            paintCursorAboveText: true,
            showCursor: showCursor,
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: TextSelection.Collapsed(2, TextAffinity.Upstream));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        Assert.True(Paints(PaintEditable(editable), ParagraphStep(), RectStep(Red)));

        // Let the RenderEditable paint again. Setting the selection to null should prevent the caret
        // from being painted.
        editable.Selection = null;
        // Still paints the paragraph.
        Assert.True(Paints(PaintEditable(editable), ParagraphStep()));
        // No longer paints the caret.
        Assert.False(Paints(PaintEditable(editable), RectStep(Red)));

        // Reset.
        editable.Selection = TextSelection.Collapsed(0);
        Assert.True(Paints(PaintEditable(editable), ParagraphStep()));
        Assert.True(Paints(PaintEditable(editable), RectStep(Red)));

        // Invalid cursor position.
        editable.Selection = TextSelection.Collapsed(-1);
        // Still paints the paragraph.
        Assert.True(Paints(PaintEditable(editable), ParagraphStep()));
        // No longer paints the caret.
        Assert.False(Paints(PaintEditable(editable), RectStep(Red)));
    }

    [Fact]
    public void SelectsCorrectPlaceWithOffsets()
    {
        const string text = "test\ntest";
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        ViewportOffset viewportOffset = ViewportOffset.Zero();
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            offset: viewportOffset,
            textSelectionDelegate: @delegate,
            // This makes the scroll axis vertical.
            maxLines: 2,
            text: new TextSpan(text: text, style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: TextSelection.Collapsed(4));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        Assert.True(Paints(PaintEditable(editable), ParagraphStep(offset: default)));

        editable.SelectPositionAt(from: new Point(0, 2), cause: SelectionChangedCause.Tap);
        tester.PumpFrame();

        Assert.True(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(0, @delegate.Selection.Value.BaseOffset);

        viewportOffset.CorrectBy(10);

        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.True(Paints(PaintEditable(editable), ParagraphStep(offset: new Point(0, -10))));

        // Tap the same place. But because the offset is scrolled up, the second line gets tapped
        // instead.
        editable.SelectPositionAt(from: new Point(0, 2), cause: SelectionChangedCause.Tap);
        tester.PumpFrame();

        Assert.True(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(5, @delegate.Selection.Value.BaseOffset);

        // Test the other selection methods.
        // Move over by one character.
        editable.HandleTapDown(new TapDownDetails(globalPosition: new Point(10, 2)));
        tester.PumpFrame();
        editable.SelectPosition(cause: SelectionChangedCause.Tap);
        tester.PumpFrame();
        Assert.True(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(6, @delegate.Selection.Value.BaseOffset);

        editable.HandleTapDown(new TapDownDetails(globalPosition: new Point(20, 2)));
        tester.PumpFrame();
        editable.SelectWord(cause: SelectionChangedCause.LongPress);
        tester.PumpFrame();
        Assert.False(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(5, @delegate.Selection.Value.BaseOffset);
        Assert.Equal(9, @delegate.Selection.Value.ExtentOffset);

        // Select one more character down but since it's still part of the same word, the same word
        // is selected.
        editable.SelectWordsInRange(from: new Point(30, 2), cause: SelectionChangedCause.LongPress);
        tester.PumpFrame();
        Assert.False(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(5, @delegate.Selection.Value.BaseOffset);
        Assert.Equal(9, @delegate.Selection.Value.ExtentOffset);
    }

    [Fact]
    public void SelectsReadonlyRenderEditableMatchesNativeBehaviorForAndroid()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        try
        {
            (FakeEditableTextState @delegate, RenderEditable editable, RenderingTester tester) =
                WhitespaceEditable("  test", readOnly: true);
            using (tester)
            {
                editable.SelectWordsInRange(from: new Point(10, 2), cause: SelectionChangedCause.LongPress);
                tester.PumpFrame();
                Assert.False(@delegate.Selection!.Value.IsCollapsed);
                Assert.Equal(1, @delegate.Selection.Value.BaseOffset);
                Assert.Equal(2, @delegate.Selection.Value.ExtentOffset);
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void SelectsRenderEditableMatchesNativeBehaviorForIOSCase1()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        try
        {
            // Regression test for https://github.com/flutter/flutter/issues/78564
            (FakeEditableTextState @delegate, RenderEditable editable, RenderingTester tester) =
                WhitespaceEditable("  test", readOnly: false);
            using (tester)
            {
                editable.SelectWordsInRange(from: new Point(10, 2), cause: SelectionChangedCause.LongPress);
                tester.PumpFrame();
                Assert.False(@delegate.Selection!.Value.IsCollapsed);
                Assert.Equal(1, @delegate.Selection.Value.BaseOffset);
                Assert.Equal(6, @delegate.Selection.Value.ExtentOffset);
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void SelectsRenderEditableMatchesNativeBehaviorForIOSCase2()
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        try
        {
            // Regression test for https://github.com/flutter/flutter/issues/78564
            (FakeEditableTextState @delegate, RenderEditable editable, RenderingTester tester) =
                WhitespaceEditable("   ", readOnly: false);
            using (tester)
            {
                editable.SelectWordsInRange(from: new Point(10, 2), cause: SelectionChangedCause.LongPress);
                tester.PumpFrame();
                Assert.True(@delegate.Selection!.Value.IsCollapsed);
                Assert.Equal(1, @delegate.Selection.Value.BaseOffset);
                Assert.Equal(1, @delegate.Selection.Value.ExtentOffset);
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    private static (FakeEditableTextState, RenderEditable, RenderingTester) WhitespaceEditable(
        string text,
        bool readOnly)
    {
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            readOnly: readOnly,
            textSelectionDelegate: @delegate,
            text: new TextSpan(text: text, style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: TextSelection.Collapsed(4));
        var tester = new RenderingTester();
        tester.Layout(editable);
        return (@delegate, editable, tester);
    }

    [Fact]
    public void SelectsCorrectPlaceWhenOffsetsAreFlipped()
    {
        const string text = "abc def ghi";
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            textSelectionDelegate: @delegate,
            text: new TextSpan(text: text, style: new TextStyle(Height: 1.0, FontSize: 10.0)));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        editable.SelectPositionAt(from: new Point(30, 2), to: new Point(10, 2), cause: SelectionChangedCause.Drag);
        tester.PumpFrame();

        Assert.False(@delegate.Selection!.Value.IsCollapsed);
        Assert.Equal(3, @delegate.Selection.Value.BaseOffset);
        Assert.Equal(1, @delegate.Selection.Value.ExtentOffset);
    }

    [Fact]
    public void PromptRectDisappearsWhenPromptRectColorIsSetToNull()
    {
        Color promptRectColor = new Color(0x12345678);
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), text: "ABCDEFG"),
            locale: "en_US",
            offset: ViewportOffset.Fixed(10.0),
            selection: TextSelection.Collapsed(0),
            promptRectColor: promptRectColor,
            promptRectRange: new TextRange(0, 1));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(1000.0, 1000.0)));
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.True(Paints(PaintEditable(editable), RectStep(promptRectColor)));

        editable.PromptRectColor = null;

        editable.Layout(BoxConstraints.Loose(new Size(1000.0, 1000.0)));
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.Null(editable.PromptRectColor);
        Assert.False(Paints(PaintEditable(editable), RectStep(promptRectColor)));
    }

    [Fact]
    public void EditableHasFocusCorrectlyInitialized()
    {
        // Regression test for https://github.com/flutter/flutter/issues/21640
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), text: "12345"),
            locale: "en_US",
            hasFocus: true);

        Assert.True(editable.HasFocus);
        editable.HasFocus = false;
        Assert.False(editable.HasFocus);
    }

    [Fact]
    public void HasCorrectMaxScrollExtent()
    {
        RenderEditable editable = NewEditable(
            maxLines: 2,
            backgroundCursorColor: Grey,
            cursorColor: Color.FromARGB(0xFF, 0xFF, 0x00, 0x00),
            text: new TextSpan(
                text: "撒地方加咖啡哈金凤凰卡号方式剪坏算法发挥福建垃\nasfjafjajfjaslfjaskjflasjfksajf撒分开建安路口附近拉设\n计费可使肌肤撒附近埃里克圾房卡设计费\"",
                style: new TextStyle(Height: 1.0, FontSize: 10.0, FontFamily: new FontFamily("Roboto"))),
            selection: TextSelection.Collapsed(4, TextAffinity.Upstream));

        editable.Layout(BoxConstraints.Loose(new Size(100.0, 1000.0)));
        Assert.Equal(new Size(100, 20), editable.Size);
        Assert.Equal(2, editable.MaxLines);
        Assert.Equal(90, editable.MaxScrollExtent);

        editable.Layout(BoxConstraints.Loose(new Size(150.0, 1000.0)));
        Assert.Equal(50, editable.MaxScrollExtent);

        editable.Layout(BoxConstraints.Loose(new Size(200.0, 1000.0)));
        Assert.Equal(40, editable.MaxScrollExtent);

        editable.Layout(BoxConstraints.Loose(new Size(500.0, 1000.0)));
        Assert.Equal(10, editable.MaxScrollExtent);

        editable.Layout(BoxConstraints.Loose(new Size(1000.0, 1000.0)));
        Assert.Equal(10, editable.MaxScrollExtent);
    }

    [Fact]
    public void GetEndpointsForSelectionHandlesEmptyCharacters()
    {
        RenderEditable editable = NewEditable(
            // This is a Unicode left-to-right mark character that will not render any glyphs.
            text: new TextSpan(text: "‎"));
        editable.Layout(BoxConstraints.Loose(new Size(100, 100)));
        IReadOnlyList<TextSelectionPoint> endpoints = editable.GetEndpointsForSelection(new TextSelection(0, 1));
        Assert.Equal(0, endpoints[0].Point.X);
    }

    [Fact]
    public void TextSelectionPointCanCompare()
    {
        // ignore: prefer_const_constructors
        var first = new TextSelectionPoint(new Point(1, 2), TextDirection.Ltr);
        // ignore: prefer_const_constructors
        var second = new TextSelectionPoint(new Point(1, 2), TextDirection.Ltr);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        // ignore: prefer_const_constructors
        var different = new TextSelectionPoint(new Point(2, 2), TextDirection.Ltr);
        Assert.False(first == different);
        Assert.NotEqual(first.GetHashCode(), different.GetHashCode());
    }

    [Fact]
    public void GetRectForComposingRange_ReturnsNullWhenNoComposingRange()
    {
        RenderEditable editable = NewEditable(maxLines: null);
        editable.Text = new TextSpan(text: "123");
        editable.Layout(BoxConstraints.TightFor(width: 200));

        // Invalid range.
        Assert.Null(editable.GetRectForComposingRange(new TextRange(-1, 2)));

        // Collapsed range.
        Assert.Null(editable.GetRectForComposingRange(TextRange.Collapsed(2)));

        // Empty Editable.
        editable.Text = new TextSpan(text: "‎");
        editable.Layout(BoxConstraints.TightFor(width: 200));

        Rect? rect = editable.GetRectForComposingRange(new TextRange(0, 1));
        // On web these evaluate to a zero-width Rect.
        Assert.True(rect is null || rect.Value.Width == 0);
    }

    [Fact]
    public void GetRectForComposingRange_MoreThan1RunOnTheSameLine()
    {
        RenderEditable editable = NewEditable(maxLines: null);
        var tinyText = new TextSpan(text: "A", style: new TextStyle(FontSize: 1));
        var normalText = new TextSpan(text: new string('A', 20), style: new TextStyle(FontSize: 10));
        editable.Text = new TextSpan(children: [normalText, tinyText, normalText]);
        editable.Layout(BoxConstraints.TightFor(width: 200));

        IReadOnlyList<LineMetrics> lineMetrics = editable.TextPainter.ComputeLineMetrics();
        // Just FYI.
        Assert.Equal([10.0, 10.0, 10.0], lineMetrics.Select(metrics => metrics.Height));

        Rect? composingRect = editable.GetRectForComposingRange(new TextRange(0, 20 + 2));
        Assert.True(composingRect!.Value.Width > 200 - 10);
    }

    // -- custom painters ------------------------------------------------------------------------------

    [Fact]
    public void CustomPainters_PaintInTheCorrectOrder()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        Color color = new Color(0x12345678);
        Rect onePixel = FromLTRB(1, 1, 1, 1);
        editable.ForegroundPainter = new TestRenderEditablePainter();
        tester.PumpFrame(EnginePhase.CompositingBits);
        Assert.True(Paints(PaintEditable(editable), ParagraphStep(), RectStep(color, onePixel)));

        editable.ForegroundPainter = null;
        editable.Painter = new TestRenderEditablePainter();
        Assert.True(Paints(PaintEditable(editable), RectStep(color, onePixel), ParagraphStep()));

        editable.ForegroundPainter = new TestRenderEditablePainter();
        editable.Painter = new TestRenderEditablePainter();
        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(color, onePixel),
            ParagraphStep(),
            RectStep(color, onePixel)));
    }

    [Fact]
    public void CustomPainters_ChangingForegroundPainter()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.ForegroundPainter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(1, painter.PaintCount);

        painter = new TestRenderEditablePainter { Repaint = false };
        editable.ForegroundPainter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(0, painter.PaintCount);

        painter = new TestRenderEditablePainter { Repaint = true };
        editable.ForegroundPainter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(1, painter.PaintCount);
    }

    [Fact]
    public void CustomPainters_ChangingBackgroundPainter()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.Painter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(1, painter.PaintCount);

        painter = new TestRenderEditablePainter { Repaint = false };
        editable.Painter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(0, painter.PaintCount);

        painter = new TestRenderEditablePainter { Repaint = true };
        editable.Painter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(1, painter.PaintCount);
    }

    [Fact]
    public void CustomPainters_SwappingPainters()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter1 = new TestRenderEditablePainter(new Color(0x01234567));
        var painter2 = new TestRenderEditablePainter(new Color(0x76543210));
        Rect onePixel = FromLTRB(1, 1, 1, 1);

        editable.Painter = painter1;
        editable.ForegroundPainter = painter2;
        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(painter1.Color, onePixel),
            ParagraphStep(),
            RectStep(painter2.Color, onePixel)));

        editable.Painter = painter2;
        editable.ForegroundPainter = painter1;
        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(painter2.Color, onePixel),
            ParagraphStep(),
            RectStep(painter1.Color, onePixel)));
    }

    [Fact]
    public void CustomPainters_ReusingTheSamePainter()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.Painter = painter;
        editable.ForegroundPainter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(2, painter.PaintCount);

        Color color = new Color(0x12345678);
        Rect onePixel = FromLTRB(1, 1, 1, 1);
        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(color, onePixel),
            ParagraphStep(),
            RectStep(color, onePixel)));
    }

    [Fact]
    public void CustomPainters_DoesNotRepaintTheRenderEditableWhenCustomPaintersNeedRepaint()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.Painter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        editable.PaintCount = 0;
        painter.PaintCount = 0;

        painter.MarkNeedsPaint();

        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(0, editable.PaintCount);
        Assert.Equal(1, painter.PaintCount);
    }

    [Fact]
    public void CustomPainters_RepaintsWhenItsRenderEditableRepaints()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.Painter = painter;
        tester.PumpFrame(EnginePhase.Paint);
        editable.PaintCount = 0;
        painter.PaintCount = 0;

        editable.MarkNeedsPaint();

        tester.PumpFrame(EnginePhase.Paint);
        Assert.Equal(1, editable.PaintCount);
        Assert.Equal(1, painter.PaintCount);
    }

    [Fact]
    public void CustomPainters_CorrectCoordinateSpace()
    {
        TestRenderEditable editable = NewPainterEditable();
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(100, 100)));

        var painter = new TestRenderEditablePainter();
        editable.Painter = painter;
        editable.Offset = ViewportOffset.Fixed(1000);

        tester.PumpFrame(EnginePhase.CompositingBits);
        Assert.True(Paints(
            PaintEditable(editable),
            RectStep(new Color(0x12345678), FromLTRB(1, 1, 1, 1)),
            ParagraphStep()));
    }

    private static TestRenderEditable NewPainterEditable() => new(
        text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
        selection: TextSelection.Collapsed(4, TextAffinity.Upstream));

    // -- hit testing ----------------------------------------------------------------------------------

    [Fact]
    public void HitTesting_BasicTextSpanHitTesting()
    {
        var textSpanA = new TextSpan(text: new string('A', 10));
        var textSpanBC = new TextSpan(text: "BC", style: new TextStyle(LetterSpacing: 26.0));

        var text = new TextSpan(
            text: string.Empty,
            style: new TextStyle(FontSize: 10.0),
            children: [textSpanA, textSpanBC]);

        RenderEditable renderEditable = NewEditable(
            maxLines: null,
            text: text,
            offset: ViewportOffset.Fixed(0.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(renderEditable, BoxConstraints.TightFor(width: 100.0 + CaretMarginOf(renderEditable)));

        // Prepare for painting after layout. Hit-testing the A span.
        AssertHitSpans(renderEditable, new Point(5.0, 5.0), true, textSpanA);
        AssertHitSpans(renderEditable, new Point(95.0, 5.0), true, textSpanA);
        AssertHitSpans(renderEditable, new Point(200.0, 5.0), false);

        // Hit-testing the BC span. The first character 'B' is at x = 13.0 (letterSpacing / 2).
        AssertHitSpans(renderEditable, new Point(18.0, 15.0), true, textSpanBC);
        // Between B and C, with large letter-spacing.
        AssertHitSpans(renderEditable, new Point(31.0, 15.0), true, textSpanBC);
        // On C.
        AssertHitSpans(renderEditable, new Point(54.0, 15.0), true, textSpanBC);
        // After C.
        AssertHitSpans(renderEditable, new Point(100.0, 15.0), true);
        AssertHitSpans(renderEditable, new Point(9999.0, 9999.0), false);
    }

    [Fact]
    public void HitTesting_TextSpanHitTestingWithTextJustification()
    {
        var textSpanA = new TextSpan(text: "A ");
        var textSpanB = new TextSpan(text: "B​");
        var textSpanC = new TextSpan(text: new string('C', 10));

        var text = new TextSpan(
            text: string.Empty,
            style: new TextStyle(FontSize: 10.0),
            children: [textSpanA, textSpanB, textSpanC]);

        RenderEditable renderEditable = NewEditable(
            maxLines: null,
            text: text,
            textAlign: TextAlign.Justify,
            offset: ViewportOffset.Fixed(0.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(renderEditable, BoxConstraints.TightFor(width: 100.0 + CaretMarginOf(renderEditable)));

        // Expected layout: "A        B" / "CCCCCCCCCC".
        AssertHitSpans(renderEditable, new Point(5.0, 5.0), true, textSpanA);
        AssertHitSpans(renderEditable, new Point(50.0, 5.0), true, textSpanA);
        AssertHitSpans(renderEditable, new Point(95.0, 5.0), true, textSpanB);
    }

    [Fact]
    public void HitTesting_HitsCorrectTextSpanWhenNotScrolled()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(
                style: new TextStyle(Height: 1.0, FontSize: 10.0),
                children: [new TextSpan(text: "A"), new TextSpan(text: "B")]),
            offset: ViewportOffset.Fixed(0.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        BoxHitTestResult result = HitTest(editable, default);
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("A", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);

        result = HitTest(editable, new Point(15.0, 0.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("B", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
    }

    [Fact]
    public void HitTesting_HitsCorrectTextSpanWhenScrolledVertically()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(
                style: new TextStyle(Height: 1.0, FontSize: 10.0),
                children: [new TextSpan(text: "A"), new TextSpan(text: "B\n"), new TextSpan(text: "C")]),
            maxLines: null,
            offset: ViewportOffset.Fixed(5.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        BoxHitTestResult result = HitTest(editable, default);
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("A", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);

        result = HitTest(editable, new Point(15.0, 0.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("B\n", Assert.IsType<TextSpan>(result.Path[0].Target).Text);

        result = HitTest(editable, new Point(0.0, 6.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("C", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
    }

    [Fact]
    public void HitTesting_HitsCorrectTextSpanWhenScrolledHorizontally()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(
                style: new TextStyle(Height: 1.0, FontSize: 10.0),
                children: [new TextSpan(text: "A"), new TextSpan(text: "B")]),
            offset: ViewportOffset.Fixed(5.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        BoxHitTestResult result = HitTest(editable, new Point(6.0, 0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal("B", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);
    }

    private static BoxHitTestResult HitTest(RenderBox box, Point position)
    {
        var result = new BoxHitTestResult();
        box.HitTest(result, position);
        return result;
    }

    private static void AssertHitSpans(RenderEditable editable, Point position, bool hit, params TextSpan[] spans)
    {
        var result = new BoxHitTestResult();
        Assert.Equal(hit, editable.HitTest(result, position));
        Assert.Equal(spans, result.Path.Select(entry => entry.Target).OfType<TextSpan>());
    }

    // -- WidgetSpan support ---------------------------------------------------------------------------

    [Fact]
    public void WidgetSpan_AbleToRenderBasicWidgetSpan()
    {
        RenderEditable editable = WidgetSpanEditable(
            [new TextSpan(text: "test"), BlueBox()],
            ["b"]);
        using var tester = new RenderingTester();
        tester.Layout(editable);
        editable.HasFocus = true;
        tester.PumpFrame();

        Rect composingRect = editable.GetRectForComposingRange(new TextRange(4, 5))!.Value;
        Assert.Equal(FromLTRB(40.0, 0.0, 54.0, 14.0), composingRect);
    }

    [Fact]
    public void WidgetSpan_AbleToRenderMultipleWidgetSpans()
    {
        RenderEditable editable = WidgetSpanEditable(
            [new TextSpan(text: "test"), BlueBox(), BlueBox(), BlueBox()],
            ["b", "c", "d"]);
        using var tester = new RenderingTester();
        tester.Layout(editable);
        editable.HasFocus = true;
        tester.PumpFrame();

        Rect composingRect = editable.GetRectForComposingRange(new TextRange(4, 7))!.Value;
        Assert.Equal(FromLTRB(40.0, 0.0, 82.0, 14.0), composingRect);
    }

    [Fact]
    public void WidgetSpan_AbleToRenderWidgetSpansWithLineWrap()
    {
        RenderEditable editable = WidgetSpanEditable(
            [new TextSpan(text: "test"), TextWidgetSpan("b"), TextWidgetSpan("c"), TextWidgetSpan("d")],
            ["b", "c", "d"],
            maxLines: 2,
            minLines: 2);
        using var tester = new RenderingTester();
        // Force a line wrap.
        tester.Layout(editable, new BoxConstraints(MaxWidth: 75));
        editable.HasFocus = true;
        tester.PumpFrame();

        Rect composingRect = editable.GetRectForComposingRange(new TextRange(4, 6))!.Value;
        Assert.Equal(FromLTRB(40.0, 0.0, 68.0, 14.0), composingRect);
        composingRect = editable.GetRectForComposingRange(new TextRange(6, 7))!.Value;
        Assert.Equal(FromLTRB(0.0, 14.0, 14.0, 28.0), composingRect);
    }

    [Fact]
    public void WidgetSpan_AbleToRenderWidgetSpansWithLineWrapAlternatingSpans()
    {
        RenderEditable editable = WidgetSpanEditable(
            [
                new TextSpan(text: "test"),
                TextWidgetSpan("b"),
                TextWidgetSpan("c"),
                TextWidgetSpan("d"),
                new TextSpan(text: "HI"),
                TextWidgetSpan("e"),
            ],
            ["b", "c", "d", "e"],
            maxLines: 2,
            minLines: 2);
        using var tester = new RenderingTester();
        // Force a line wrap.
        tester.Layout(editable, new BoxConstraints(MaxWidth: 75));
        editable.HasFocus = true;
        tester.PumpFrame();

        Assert.Equal(FromLTRB(40.0, 0.0, 68.0, 14.0), editable.GetRectForComposingRange(new TextRange(4, 6)));
        Assert.Equal(FromLTRB(0.0, 14.0, 14.0, 28.0), editable.GetRectForComposingRange(new TextRange(6, 7)));
        // H.
        Assert.Equal(FromLTRB(14.0, 14.0, 24.0, 28.0), editable.GetRectForComposingRange(new TextRange(7, 8)));
        // I.
        Assert.Equal(FromLTRB(24.0, 14.0, 34.0, 28.0), editable.GetRectForComposingRange(new TextRange(8, 9)));
        Assert.Equal(FromLTRB(34.0, 14.0, 48.0, 28.0), editable.GetRectForComposingRange(new TextRange(9, 10)));
    }

    [Fact]
    public void WidgetSpan_AbleToRenderWidgetSpansNestedSpans()
    {
        RenderEditable editable = WidgetSpanEditable(
            [
                new TextSpan(text: "test"),
                TextWidgetSpan("a"),
                new TextSpan(children: [TextWidgetSpan("b"), TextWidgetSpan("c")]),
            ],
            ["a", "b", "c"],
            maxLines: 2,
            minLines: 2);
        using var tester = new RenderingTester();
        // Force a line wrap.
        tester.Layout(editable, new BoxConstraints(MaxWidth: 75));
        editable.HasFocus = true;
        tester.PumpFrame();

        Assert.Equal(FromLTRB(40.0, 0.0, 54.0, 14.0), editable.GetRectForComposingRange(new TextRange(4, 5)));
        Assert.Equal(FromLTRB(54.0, 0.0, 68.0, 14.0), editable.GetRectForComposingRange(new TextRange(5, 6)));
        Assert.Equal(FromLTRB(0.0, 14.0, 14.0, 28.0), editable.GetRectForComposingRange(new TextRange(6, 7)));
        Assert.Null(editable.GetRectForComposingRange(new TextRange(7, 8)));
    }

    [Fact]
    public void WidgetSpan_RenderBoxIsPaintedAtCorrectOffsetWhenScrolled()
    {
        RenderEditable editable = WidgetSpanEditable(
            [new TextSpan(text: "test"), BlueBox()],
            ["b"],
            offset: ViewportOffset.Fixed(100.0),
            maxLines: null);
        using var tester = new RenderingTester();
        tester.Layout(editable);
        editable.HasFocus = true;
        tester.PumpFrame();

        Rect composingRect = editable.GetRectForComposingRange(new TextRange(4, 5))!.Value;
        Assert.Equal(FromLTRB(40.0, -100.0, 54.0, -86.0), composingRect);
    }

    [Fact]
    public void WidgetSpan_CanComputeIntrinsicWidthForWidgetSpans()
    {
        // Regression test for https://github.com/flutter/flutter/issues/59316
        const double screenWidth = 1000.0;
        const double fixedHeight = 1000.0;
        const string sentence = "one two";
        var editable = new TestRenderEditable(
            cursorWidth: 0.0,
            text: new TextSpan(
                style: new TextStyle(Height: 1.0, FontSize: 10.0),
                children: [new TextSpan(text: "test"), TextWidgetSpan("a")]),
            children: [new RenderParagraph(new TextSpan(text: sentence))],
            maxLines: 2,
            minLines: 2,
            textScaler: TextScaler.Linear(2.0));
        ApplyParentData(editable);

        // Intrinsics can be computed without doing layout.
        Assert.Equal(
            (2.0 * 10.0 * 4) + (14.0 * 7) + 1.0,
            editable.InvokeComputeMaxIntrinsicWidth(fixedHeight));
        Assert.Equal(
            Math.Max(Math.Max(2.0 * 10.0 * 4, 14.0 * 3), 14.0 * 3),
            editable.InvokeComputeMinIntrinsicWidth(fixedHeight));
        Assert.Equal(40.0, editable.InvokeComputeMaxIntrinsicHeight(fixedHeight));
        Assert.Equal(40.0, editable.InvokeComputeMinIntrinsicHeight(fixedHeight));

        using var tester = new RenderingTester();
        tester.Layout(editable, new BoxConstraints(MaxWidth: screenWidth));
        // Intrinsics can be computed after layout.
        Assert.Equal(
            (2.0 * 10.0 * 4) + (14.0 * 7) + 1.0,
            editable.InvokeComputeMaxIntrinsicWidth(fixedHeight));
    }

    [Fact]
    public void WidgetSpan_HitsCorrectWidgetSpanWhenNotScrolled()
    {
        RenderEditable editable = WidgetSpanEditable(
            [
                new TextSpan(text: "test"),
                TextWidgetSpan("a"),
                new TextSpan(children: [TextWidgetSpan("b"), TextWidgetSpan("c")]),
            ],
            ["a", "b", "c"],
            offset: ViewportOffset.Fixed(0.0),
            selection: TextSelection.Collapsed(0));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        AssertWidgetSpanHits(editable, "test");
    }

    [Fact]
    public void WidgetSpan_HitsCorrectWidgetSpanWhenScrolled()
    {
        string text = new string('\n', 10) + "test";
        RenderEditable editable = WidgetSpanEditable(
            [
                new TextSpan(text: text),
                TextWidgetSpan("a"),
                new TextSpan(children: [TextWidgetSpan("b"), TextWidgetSpan("c")]),
            ],
            ["a", "b", "c"],
            offset: ViewportOffset.Fixed(100.0),
            selection: TextSelection.Collapsed(0),
            maxLines: null,
            delegateText: text,
            delegateSelection: TextSelection.Collapsed(13));
        using var tester = new RenderingTester();
        tester.Layout(editable, BoxConstraints.Loose(new Size(500.0, 500.0)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        BoxHitTestResult result = HitTest(editable, new Point(0.0, 4.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(text, Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);
        result = HitTest(editable, new Point(15.0, 4.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(text, Assert.IsType<TextSpan>(result.Path[0].Target).Text);

        AssertPlaceholderHits(editable);

        // Only the RenderEditable itself: nothing is laid out below the last line.
        Assert.Single(HitTest(editable, new Point(5.0, 15.0)).Path);
    }

    private static void AssertWidgetSpanHits(RenderEditable editable, string text)
    {
        BoxHitTestResult result = HitTest(editable, new Point(1.0, 5.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(text, Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);
        result = HitTest(editable, new Point(15.0, 5.0));
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(text, Assert.IsType<TextSpan>(result.Path[0].Target).Text);

        AssertPlaceholderHits(editable);

        Assert.Empty(HitTest(editable, new Point(5.0, 15.0)).Path);
    }

    private static void AssertPlaceholderHits(RenderEditable editable)
    {
        // The hit on a placeholder goes through the child paragraph: its span, the paragraph, then the
        // editable.
        BoxHitTestResult result = HitTest(editable, new Point(41.0, 0.0));
        Assert.Equal(3, result.Path.Count);
        Assert.Equal("a", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
        Assert.IsAssignableFrom<RenderEditable>(result.Path[^1].Target);

        result = HitTest(editable, new Point(55.0, 0.0));
        Assert.Equal(3, result.Path.Count);
        Assert.Equal("b", Assert.IsType<TextSpan>(result.Path[0].Target).Text);

        result = HitTest(editable, new Point(69.0, 5.0));
        Assert.Equal(3, result.Path.Count);
        Assert.Equal("c", Assert.IsType<TextSpan>(result.Path[0].Target).Text);
    }

    private static WidgetSpan BlueBox() => new(new Container(width: 10, height: 10, color: Blue));

    private static WidgetSpan TextWidgetSpan(string text) => new(new Text(text));

    private static RenderEditable WidgetSpanEditable(
        IReadOnlyList<InlineSpan> children,
        IReadOnlyList<string> boxTexts,
        ViewportOffset? offset = null,
        TextSelection? selection = null,
        int? maxLines = 1,
        int? minLines = null,
        string delegateText = "test",
        TextSelection? delegateSelection = null)
    {
        var @delegate = new FakeEditableTextState
        {
            TextEditingValue = new TextEditingValue(delegateText, delegateSelection ?? TextSelection.Collapsed(3)),
        };
        List<RenderBox> renderBoxes = boxTexts
            .Select(text => (RenderBox)new RenderParagraph(new TextSpan(text: text)))
            .ToList();
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            offset: offset ?? ViewportOffset.Zero(),
            textSelectionDelegate: @delegate,
            maxLines: maxLines,
            minLines: minLines,
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), children: children),
            selection: selection ?? TextSelection.Collapsed(3),
            children: renderBoxes);
        ApplyParentData(editable);
        return editable;
    }

    // Dart's `_applyParentData`: hands every inline child the WidgetSpan it stands for.
    private static void ApplyParentData(RenderEditable editable)
    {
        var spans = new List<PlaceholderSpan>();
        editable.Text!.VisitChildren(span =>
        {
            if (span is WidgetSpan widgetSpan)
            {
                spans.Add(widgetSpan);
            }

            return true;
        });

        int index = 0;
        for (RenderBox? child = editable.FirstChild; child is not null; child = editable.ChildAfter(child))
        {
            ((TextParentData)child.parentData!).Span = spans[index++];
        }
    }

    // -- trailing editable_test.dart cases --------------------------------------------------------------

    [Fact]
    public void DoesNotSkipTextPainterLayoutBecauseOfInvalidCache()
    {
        // Regression test for https://github.com/flutter/flutter/issues/84896.
        var constraints = new BoxConstraints(MinWidth: 100, MaxWidth: 500);
        var editable = new TestRenderEditable(
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), text: "A"),
            locale: "en_US",
            offset: ViewportOffset.Fixed(10.0),
            selection: TextSelection.Collapsed(0),
            cursorColor: new Color(0xFFFFFFFF),
            showCursor: new ValueNotifier<bool>(true));
        using var tester = new RenderingTester();
        tester.Layout(editable, constraints);

        double initialWidth = editable.InvokeComputeDryLayout(constraints).Width;
        Assert.Equal(500, initialWidth);

        // Turn off forceLine. Now the width should be significantly smaller.
        editable.ForceLine = false;
        Assert.True(editable.InvokeComputeDryLayout(constraints).Width < initialWidth);
    }

    [Fact]
    public void FloatingCursorPositionIsIndependentOfViewportOffset()
    {
        var showCursor = new ValueNotifier<bool>(true);
        Color cursorColor = Color.FromARGB(0xFF, 0xFF, 0x00, 0x00);
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            cursorColor: cursorColor,
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            maxLines: 3,
            selection: TextSelection.Collapsed(4, TextAffinity.Upstream));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        editable.Layout(BoxConstraints.Loose(new Size(100, 100)));
        // Prepare for painting after layout.
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.Equal(0, CountCalls(PaintEditable(editable), "drawRect"));

        editable.ShowCursor = showCursor;
        editable.SetFloatingCursor(
            FloatingCursorDragState.Start,
            new Point(50, 50),
            new TextPosition(4, TextAffinity.Upstream));
        tester.PumpFrame(EnginePhase.CompositingBits);

        RRect expectedRRect = RRect.FromRectAndRadius(new Rect(49.5, 51, 2, 8), Radius.Circular(1));

        Assert.True(Paints(PaintEditable(editable), RRectStep(cursorColor.WithOpacity(0.75), expectedRRect)));

        // Change the text viewport offset.
        editable.Offset = ViewportOffset.Fixed(200);

        // Floating cursor should be drawn in the same position.
        editable.SetFloatingCursor(
            FloatingCursorDragState.Start,
            new Point(50, 50),
            new TextPosition(4, TextAffinity.Upstream));
        tester.PumpFrame(EnginePhase.CompositingBits);

        Assert.True(Paints(PaintEditable(editable), RRectStep(cursorColor.WithOpacity(0.75), expectedRRect)));
    }

    [Fact]
    public void GetWordAtOffsetWithANegativePosition()
    {
        const string text = "abc";
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        var editable = new TestRenderEditable(
            textSelectionDelegate: @delegate,
            text: new TextSpan(text: text, style: new TextStyle(Height: 1.0, FontSize: 10.0)));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        // Cause text metrics to be computed.
        editable.InvokeComputeDistanceToActualBaseline(TextBaseline.Alphabetic);

        TextSelection selection;
        try
        {
            selection = editable.GetWordAtOffset(new TextPosition(-1, TextAffinity.Upstream));
        }
        catch (AssertionError)
        {
            // Debug mode will throw an assertion error.
            return;
        }

        Assert.Equal(TextSelection.Collapsed(3), selection);
    }

    // -- editable_intrinsics_test.dart ------------------------------------------------------------------

    [Fact]
    public void Intrinsics_EditableIntrinsics()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(Height: 1.0, FontSize: 10.0), text: "12345"),
            locale: "ja_JP");
        Assert.Equal(50.0, editable.GetMinIntrinsicWidth(double.PositiveInfinity));
        // The width includes the width of the cursor (1.0).
        Assert.Equal(52.0, editable.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(10.0, editable.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(10.0, editable.GetMaxIntrinsicHeight(double.PositiveInfinity));

        Assert.Equal(
            "RenderEditable#00000 NEEDS-LAYOUT NEEDS-PAINT NEEDS-COMPOSITING-BITS-UPDATE DETACHED\n"
            + " │ parentData: MISSING\n"
            + " │ constraints: MISSING\n"
            + " │ size: MISSING\n"
            + " │ cursorColor: null\n"
            + " │ showCursor: ValueNotifier<bool>#00000(false)\n"
            + " │ maxLines: 1\n"
            + " │ minLines: null\n"
            + " │ selectionColor: null\n"
            + " │ locale: ja_JP\n"
            + " │ selection: null\n"
            // Dart's private `_FixedViewportOffset` is the internal `FixedViewportOffset` class here.
            + " │ offset: FixedViewportOffset#00000(offset: 0.0)\n"
            + " ╘═╦══ text ═══\n"
            + "   ║ TextSpan:\n"
            + "   ║   inherit: true\n"
            + "   ║   size: 10.0\n"
            + "   ║   height: 1.0x\n"
            + "   ║   \"12345\"\n"
            + "   ╚═══════════\n",
            FrameworkDartTester.IgnoringHashCodes(editable.ToStringDeep(minLevel: DiagnosticLevel.Info)));
    }

    [Fact]
    public void Intrinsics_TextScalerAffectsIntrinsics()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(FontSize: 10), text: "Hello World"));

        Assert.Equal(110 + 2, editable.GetMaxIntrinsicWidth(double.PositiveInfinity));

        editable.TextScaler = TextScaler.Linear(2);
        Assert.Equal(220 + 2, editable.GetMaxIntrinsicWidth(double.PositiveInfinity));
    }

    [Fact]
    public void Intrinsics_MaxLinesAffectsIntrinsics()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(FontSize: 10), text: string.Join("\n", Enumerable.Repeat("A", 5))),
            maxLines: null);

        Assert.Equal(50, editable.GetMaxIntrinsicHeight(double.PositiveInfinity));

        editable.MaxLines = 1;
        Assert.Equal(10, editable.GetMaxIntrinsicHeight(double.PositiveInfinity));
    }

    [Fact]
    public void Intrinsics_StrutStyleAffectsIntrinsics()
    {
        RenderEditable editable = NewEditable(
            text: new TextSpan(style: new TextStyle(FontSize: 10), text: "Hello World"));

        Assert.Equal(10, editable.GetMaxIntrinsicHeight(double.PositiveInfinity));

        editable.StrutStyle = new StrutStyle(FontSize: 100, ForceStrutHeight: true);
        Assert.Equal(100, editable.GetMaxIntrinsicHeight(double.PositiveInfinity));
    }

    // -- editable_gesture_test.dart ---------------------------------------------------------------------

    [Fact]
    public void AttachAndDetachCorrectlyHandleGesture()
    {
        const int pointer = 90210;
        ViewportOffset offset = ViewportOffset.Zero();
        RenderEditable editable = NewEditable(
            backgroundCursorColor: Grey,
            selectionColor: Black,
            cursorColor: Red,
            offset: offset,
            text: new TextSpan(text: "test", style: new TextStyle(Height: 1.0, FontSize: 10.0)),
            selection: new TextSelection(0, 3, TextAffinity.Upstream));
        try
        {
            editable.Layout(BoxConstraints.Loose(new Size(1000.0, 1000.0)));
            var owner = new PipelineOwner();
            editable.Attach(owner);

            PointerRouter router = GestureBinding.Instance.PointerRouter;
            var down = new PointerDownEvent(pointer: pointer);
            editable.HandleEvent(down, new BoxHitTestEntry(editable, new Point(10, 10)));
            router.Route(down);
            // The recognizers registered pointer routes for the down event.
            Assert.True(router.DebugRouteCountFor(pointer) > 0);

            editable.Detach();
            // Detaching disposed both recognizers, which removed every route they added.
            Assert.Equal(0, router.DebugRouteCountFor(pointer));
        }
        finally
        {
            GestureBinding.Instance.GestureArena.Sweep(pointer);
            editable.Dispose();
            offset.Dispose();
        }
    }

    // -- RenderEditable semantics (asserted through the widget in editable_text_test.dart) --------------

    [Fact]
    public void Semantics_ExposesCorrectCursorMovementSemantics()
    {
        const string text = "test";
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        var editable = new TestRenderEditable(
            text: new TextSpan(text: text),
            textSelectionDelegate: @delegate,
            hasFocus: true,
            selection: TextSelection.Collapsed(text.Length));
        using var tester = new RenderingTester();
        tester.Layout(editable);

        SemanticsConfiguration config = editable.Describe();
        Assert.Null(config.OnMoveCursorForwardByCharacter);
        Assert.NotNull(config.OnMoveCursorBackwardByCharacter);
        Assert.Null(config.OnMoveCursorForwardByWord);
        Assert.NotNull(config.OnMoveCursorBackwardByWord);
        Assert.NotNull(config.OnSetSelection);
        Assert.NotNull(config.OnSetText);

        editable.Selection = TextSelection.Collapsed(text.Length / 2);
        config = editable.Describe();
        Assert.NotNull(config.OnMoveCursorForwardByCharacter);
        Assert.NotNull(config.OnMoveCursorBackwardByCharacter);
        Assert.NotNull(config.OnMoveCursorForwardByWord);
        Assert.NotNull(config.OnMoveCursorBackwardByWord);

        editable.Selection = TextSelection.Collapsed(0);
        config = editable.Describe();
        Assert.NotNull(config.OnMoveCursorForwardByCharacter);
        Assert.Null(config.OnMoveCursorBackwardByCharacter);
        Assert.NotNull(config.OnMoveCursorForwardByWord);
        Assert.Null(config.OnMoveCursorBackwardByWord);
    }

    [Fact]
    public void Semantics_CanMoveCursorWithA11yMeansCharacter()
    {
        (TestRenderEditable editable, RenderingTester tester) = A11yEditable("test");
        using (tester)
        {
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, false);
            AssertSelection(editable, 3, 3);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, false);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, false);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, false);
            AssertSelection(editable, 0, 0);
            MoveBy(editable, config => config.OnMoveCursorForwardByCharacter!, false);
            AssertSelection(editable, 1, 1);
        }
    }

    [Fact]
    public void Semantics_CanMoveCursorWithA11yMeansWord()
    {
        (TestRenderEditable editable, RenderingTester tester) = A11yEditable("test for words");
        using (tester)
        {
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, false);
            AssertSelection(editable, 9, 9);
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, false);
            AssertSelection(editable, 5, 5);
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, false);
            AssertSelection(editable, 0, 0);
            MoveBy(editable, config => config.OnMoveCursorForwardByWord!, false);
            AssertSelection(editable, 5, 5);
            MoveBy(editable, config => config.OnMoveCursorForwardByWord!, false);
            AssertSelection(editable, 9, 9);
        }
    }

    [Fact]
    public void Semantics_CanExtendSelectionWithA11yMeansCharacter()
    {
        (TestRenderEditable editable, RenderingTester tester) = A11yEditable("test");
        using (tester)
        {
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, true);
            AssertSelection(editable, 4, 3);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, true);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, true);
            MoveBy(editable, config => config.OnMoveCursorBackwardByCharacter!, true);
            AssertSelection(editable, 4, 0);
            MoveBy(editable, config => config.OnMoveCursorForwardByCharacter!, false);
            AssertSelection(editable, 1, 1);
            MoveBy(editable, config => config.OnMoveCursorForwardByCharacter!, true);
            AssertSelection(editable, 1, 2);
        }
    }

    [Fact]
    public void Semantics_CanExtendSelectionWithA11yMeansWord()
    {
        (TestRenderEditable editable, RenderingTester tester) = A11yEditable("test for words");
        using (tester)
        {
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, true);
            AssertSelection(editable, 14, 9);
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, true);
            AssertSelection(editable, 14, 5);
            MoveBy(editable, config => config.OnMoveCursorBackwardByWord!, true);
            AssertSelection(editable, 14, 0);
            MoveBy(editable, config => config.OnMoveCursorForwardByWord!, false);
            AssertSelection(editable, 5, 5);
            MoveBy(editable, config => config.OnMoveCursorForwardByWord!, true);
            AssertSelection(editable, 5, 9);
        }
    }

    [Fact]
    public void Semantics_PasswordFieldsHaveCorrectSemantics()
    {
        const string value = "password";
        var editable = new TestRenderEditable(text: new TextSpan(text: value), obscureText: true);
        SemanticsConfiguration config = editable.Describe();
        Assert.Equal(new string('•', value.Length), config.Value);
        Assert.True(config.IsTextField);
        Assert.True(config.IsFocusable);
        Assert.True(config.IsObscured);

        // Changing obscureText updates the value and the flags.
        editable.ObscureText = false;
        config = editable.Describe();
        Assert.Equal(value, config.Value);
        Assert.False(config.IsObscured);

        // A custom obscuring character is used for the value.
        editable.ObscureText = true;
        editable.ObscuringCharacter = "#";
        editable.Text = new TextSpan(text: value + "!");
        config = editable.Describe();
        Assert.Equal(new string('#', value.Length + 1), config.Value);
    }

    [Fact]
    public void Semantics_RichTextWithGestureRecognizerIsASemanticBoundaryWithExplicitChildren()
    {
        var recognizer = new TapGestureRecognizer { OnTap = () => { } };
        var editable = new TestRenderEditable(
            text: new TextSpan(
                children: [new TextSpan(text: "text"), new TextSpan(text: "link", recognizer: recognizer)]),
            readOnly: true);

        SemanticsConfiguration config = editable.Describe();
        Assert.True(config.IsSemanticBoundary);
        Assert.True(config.ExplicitChildNodes);
        Assert.False(config.IsTextField);

        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        try
        {
            // macOS has no API for selections across nodes, so the text field configuration stays.
            config = editable.Describe();
            Assert.False(config.ExplicitChildNodes);
            Assert.True(config.IsTextField);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
            recognizer.Dispose();
        }
    }

    private static (TestRenderEditable, RenderingTester) A11yEditable(string text)
    {
        var @delegate = new FakeEditableTextState { TextEditingValue = new TextEditingValue(text) };
        var editable = new TestRenderEditable(
            text: new TextSpan(text: text),
            textSelectionDelegate: @delegate,
            hasFocus: true,
            selection: TextSelection.Collapsed(text.Length));
        @delegate.OnUpdate = value => editable.Selection = value.Selection;
        var tester = new RenderingTester();
        tester.Layout(editable);
        return (editable, tester);
    }

    private static void MoveBy(
        TestRenderEditable editable,
        Func<SemanticsConfiguration, MoveCursorHandler> handler,
        bool extendSelection)
    {
        handler(editable.Describe())(extendSelection);
    }

    private static void AssertSelection(RenderEditable editable, int baseOffset, int extentOffset)
    {
        Assert.Equal(baseOffset, editable.Selection!.Value.BaseOffset);
        Assert.Equal(extentOffset, editable.Selection.Value.ExtentOffset);
    }

    // -- harness ----------------------------------------------------------------------------------------

    private static RenderEditable NewEditable(
        InlineSpan? text = null,
        TextDirection textDirection = TextDirection.Ltr,
        ViewportOffset? offset = null,
        ITextSelectionDelegate? textSelectionDelegate = null,
        TextSelection? selection = null,
        Clip clipBehavior = Clip.HardEdge,
        Color? cursorColor = null,
        Color? backgroundCursorColor = null,
        Color? selectionColor = null,
        ValueNotifier<bool>? showCursor = null,
        bool paintCursorAboveText = false,
        bool? hasFocus = null,
        bool readOnly = false,
        int? maxLines = 1,
        int? minLines = null,
        string? locale = null,
        TextAlign textAlign = TextAlign.Start,
        Color? promptRectColor = null,
        TextRange? promptRectRange = null,
        List<RenderBox>? children = null)
    {
        return new RenderEditable(
            textDirection: textDirection,
            startHandleLayerLink: new LayerLink(),
            endHandleLayerLink: new LayerLink(),
            offset: offset ?? ViewportOffset.Zero(),
            textSelectionDelegate: textSelectionDelegate ?? new FakeEditableTextState(),
            text: text,
            selection: selection,
            clipBehavior: clipBehavior,
            cursorColor: cursorColor,
            backgroundCursorColor: backgroundCursorColor,
            selectionColor: selectionColor,
            showCursor: showCursor,
            paintCursorAboveText: paintCursorAboveText,
            hasFocus: hasFocus,
            readOnly: readOnly,
            maxLines: maxLines,
            minLines: minLines,
            locale: locale,
            textAlign: textAlign,
            promptRectColor: promptRectColor,
            promptRectRange: promptRectRange,
            children: children);
    }

    private static Rect FromLTRB(double left, double top, double right, double bottom) =>
        new(left, top, right - left, bottom - top);

    private static IReadOnlyList<CanvasCall> PaintEditable(RenderEditable editable)
    {
        var context = new TestRecordingPaintingContext();
        editable.Paint(context, default);
        return context.Calls;
    }

    private static int CountCalls(IReadOnlyList<CanvasCall> calls, string method) =>
        calls.Count(call => call.Method == method);

    /// flutter_test's `paints` pattern: each step matches the next call of its kind, which must then
    /// carry the step's arguments; other calls in between are skipped.
    private static bool Paints(IReadOnlyList<CanvasCall> calls, params PaintStep[] steps)
    {
        if (calls.Count == 0)
        {
            return false;
        }

        int index = 0;
        foreach (PaintStep step in steps)
        {
            while (index < calls.Count && calls[index].Method != step.Method)
            {
                index++;
            }

            if (index >= calls.Count || !step.Matches(calls[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    private static PaintStep RectStep(Color? color = null, Rect? rect = null) => new(
        "drawRect",
        call => (color is null || call.Color == color) && (rect is null || call.Rect == rect));

    private static PaintStep RRectStep(Color? color = null, RRect? rrect = null) => new(
        "drawRRect",
        call => (color is null || call.Color == color) && (rrect is null || call.RRect == rrect));

    private static PaintStep ParagraphStep(Point? offset = null) => new(
        "drawParagraph",
        call => offset is null || call.Offset == offset);

    private static PaintStep ClipRectStep(Rect? rect = null) => new(
        "clipRect",
        call => rect is null || call.Rect == rect);

    private readonly record struct PaintStep(string Method, Func<CanvasCall, bool> Matches);

    private enum EnginePhase
    {
        Layout,
        CompositingBits,
        Paint,
        Composite,
    }

    /// rendering_tester.dart's `layout`/`pumpFrame` over an 800x600 render view.
    private sealed class RenderingTester : IDisposable
    {
        private readonly RenderView _view;
        private readonly PipelineOwner _owner;

        public RenderingTester()
        {
            _view = new RenderView(new FlutterView(new Size(800, 600)));
            _owner = new PipelineOwner(_view);
            _owner.Attach(_view);
        }

        /// Dart's `layout(box, constraints:, phase:)`: with constraints, the box is centered inside a
        /// constrained box that applies them.
        public void Layout(RenderBox box, BoxConstraints? constraints = null, EnginePhase phase = EnginePhase.Layout)
        {
            _view.Child = constraints is { } additionalConstraints
                ? new RenderPositionedBox(
                    alignment: Alignment.Center,
                    child: new RenderConstrainedBox(additionalConstraints, box))
                : box;
            PumpFrame(phase);
        }

        public void PumpFrame(EnginePhase phase = EnginePhase.Layout)
        {
            _owner.FlushLayout();
            if (phase == EnginePhase.Layout)
            {
                return;
            }

            _owner.FlushCompositingBits();
            if (phase == EnginePhase.CompositingBits)
            {
                return;
            }

            _owner.FlushPaint();
            if (phase == EnginePhase.Paint)
            {
                return;
            }

            _owner.CompositeFrame();
        }

        public void Dispose()
        {
            _view.Child = null;
        }
    }

    /// flutter_test's `TestRecordingPaintingContext`: children paint inline, layers are not pushed, and
    /// clips go straight to the recording canvas.
    private sealed class TestRecordingPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        private readonly Canvas _canvas = new(new PictureRecorder());

        public override Canvas Canvas => _canvas;

        public IReadOnlyList<CanvasCall> Calls => _canvas.DebugCalls;

        public override void PaintChild(RenderObject child, Point offset) => child.Paint(this, offset);

        public override void PushLayer(
            ContainerLayer childLayer,
            PaintingContextCallback painter,
            Point offset,
            Rect? childPaintBounds = null)
        {
            painter(this, offset);
        }

        public override ClipRectLayer? PushClipRect(
            bool needsCompositing,
            Point offset,
            Rect clipRect,
            PaintingContextCallback painter,
            Clip clipBehavior = Clip.HardEdge,
            ClipRectLayer? oldLayer = null)
        {
            Rect shifted = clipRect.Translate(new Vector(offset.X, offset.Y));
            ClipRectAndPaint(shifted, clipBehavior, shifted, () => painter(this, offset));
            return null;
        }
    }

    /// rendering_tester.dart's `TestClipPaintingContext`: records the clip behavior and paints nothing.
    private sealed class TestClipPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        public Clip ClipBehavior { get; private set; } = Clip.None;

        public override ClipRectLayer? PushClipRect(
            bool needsCompositing,
            Point offset,
            Rect clipRect,
            PaintingContextCallback painter,
            Clip clipBehavior = Clip.HardEdge,
            ClipRectLayer? oldLayer = null)
        {
            ClipBehavior = clipBehavior;
            return null;
        }
    }

    /// rendering_tester.dart's `TestPushLayerPaintingContext`: records every pushed layer.
    private sealed class TestPushLayerPaintingContext() : PaintingContext(new OffsetLayer(), default)
    {
        public List<ContainerLayer> PushedLayers { get; } = [];

        public override void PushLayer(
            ContainerLayer childLayer,
            PaintingContextCallback painter,
            Point offset,
            Rect? childPaintBounds = null)
        {
            PushedLayers.Add(childLayer);
            base.PushLayer(childLayer, painter, offset, childPaintBounds);
        }
    }

    private sealed class FakeEditableTextState : ITextSelectionDelegate
    {
        public TextSelection? Selection { get; private set; }

        public Action<TextEditingValue>? OnUpdate { get; set; }

        public TextEditingValue TextEditingValue { get; set; } = new();

        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
            Selection = value.Selection;
            OnUpdate?.Invoke(value);
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

    private sealed class TestRenderEditable(
        InlineSpan? text = null,
        TextSelection? selection = null,
        ITextSelectionDelegate? textSelectionDelegate = null,
        ViewportOffset? offset = null,
        Clip clipBehavior = Clip.HardEdge,
        bool? hasFocus = null,
        bool readOnly = false,
        bool obscureText = false,
        double cursorWidth = 1.0,
        int? maxLines = 1,
        int? minLines = null,
        TextScaler? textScaler = null,
        string? locale = null,
        Color? cursorColor = null,
        ValueNotifier<bool>? showCursor = null,
        List<RenderBox>? children = null)
        : RenderEditable(
            textDirection: TextDirection.Ltr,
            startHandleLayerLink: new LayerLink(),
            endHandleLayerLink: new LayerLink(),
            offset: offset ?? ViewportOffset.Zero(),
            textSelectionDelegate: textSelectionDelegate ?? new FakeEditableTextState(),
            text: text,
            selection: selection,
            clipBehavior: clipBehavior,
            hasFocus: hasFocus,
            readOnly: readOnly,
            obscureText: obscureText,
            cursorWidth: cursorWidth,
            maxLines: maxLines,
            minLines: minLines,
            textScaler: textScaler,
            locale: locale,
            cursorColor: cursorColor,
            showCursor: showCursor,
            children: children)
    {
        public int PaintCount { get; set; }

        public override void Paint(PaintingContext context, Point offset)
        {
            base.Paint(context, offset);
            PaintCount += 1;
        }

        public void InvokeDescribeSemanticsConfiguration(SemanticsConfiguration config) =>
            DescribeSemanticsConfiguration(config);

        public SemanticsConfiguration Describe()
        {
            var config = new SemanticsConfiguration();
            DescribeSemanticsConfiguration(config);
            return config;
        }

        public Rect? InvokeDescribeApproximatePaintClip(RenderObject child) => DescribeApproximatePaintClip(child);

        public double InvokeComputeMaxIntrinsicWidth(double height) => ComputeMaxIntrinsicWidth(height);

        public double InvokeComputeMinIntrinsicWidth(double height) => ComputeMinIntrinsicWidth(height);

        public double InvokeComputeMaxIntrinsicHeight(double width) => ComputeMaxIntrinsicHeight(width);

        public double InvokeComputeMinIntrinsicHeight(double width) => ComputeMinIntrinsicHeight(width);

        public Size InvokeComputeDryLayout(BoxConstraints constraints) => ComputeDryLayout(constraints);

        public double? InvokeComputeDistanceToActualBaseline(TextBaseline baseline) =>
            ComputeDistanceToActualBaseline(baseline);
    }

    private sealed class TestRenderEditablePainter(Color? color = null) : RenderEditablePainter
    {
        public Color Color { get; } = color ?? new Color(0x12345678);

        public bool Repaint { get; set; } = true;

        public int PaintCount { get; set; }

        public override void Paint(Canvas canvas, Size size, RenderEditable renderEditable)
        {
            PaintCount += 1;
            canvas.DrawRectangle(new SolidColorBrush(Color), null, new Rect(1, 1, 0, 0));
        }

        public override bool ShouldRepaint(RenderEditablePainter? oldDelegate) => Repaint;

        public void MarkNeedsPaint() => NotifyListeners();
    }
}
