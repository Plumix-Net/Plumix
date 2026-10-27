using Avalonia;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources: flutter/packages/flutter/test/rendering/paragraph_test.dart,
// flutter/packages/flutter/test/widgets/selectable_region_test.dart,
// engine/src/flutter/lib/web_ui/test/ui/paragraph_test.dart.
namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ParagraphSelectionDartParityTests
{
    [Theory]
    [InlineData("ภาษาไทยภาษาไทย", 2, 0, 4)]
    [InlineData("ภาษาไทยภาษาไทย", 5, 4, 7)]
    [InlineData("ພາສາລາວ", 5, 4, 7)]
    [InlineData("ភាសាខ្មែរ", 4, 0, 9)]
    [InlineData("中文测试", 1, 0, 2)]
    [InlineData("中文测试", 2, 2, 4)]
    [InlineData("日本語の文章", 1, 0, 3)]
    [InlineData("日本語の文章", 4, 4, 6)]
    public void WordBoundaryUsesDictionarySegmentation(string text, int offset, int start, int end)
    {
        using Paragraph paragraph = BuildParagraph(text);
        Assert.Equal(new TextRange(start, end), paragraph.GetWordBoundary(new TextPosition(offset)));
    }

    [Theory]
    [InlineData(-1, TextAffinity.Downstream, 0, 0)]
    [InlineData(0, TextAffinity.Upstream, 0, 0)]
    [InlineData(0, TextAffinity.Downstream, 0, 5)]
    [InlineData(5, TextAffinity.Upstream, 0, 5)]
    [InlineData(5, TextAffinity.Downstream, 5, 6)]
    [InlineData(10, TextAffinity.Downstream, 10, 10)]
    [InlineData(11, TextAffinity.Upstream, 10, 10)]
    public void WordBoundaryPreservesAffinityAndOutsideText(int offset, TextAffinity affinity, int start, int end)
    {
        using Paragraph paragraph = BuildParagraph("Hello team");
        Assert.Equal(new TextRange(start, end), paragraph.GetWordBoundary(new TextPosition(offset, affinity)));
    }

    [Fact]
    public void WordBoundaryRetainsWhitespacePlaceholderAndEmojiSegments()
    {
        using Paragraph spaces = BuildParagraph("   ");
        Assert.Equal(new TextRange(0, 3), spaces.GetWordBoundary(new TextPosition(2)));
        using Paragraph placeholders = BuildParagraph("a\uFFFCb");
        Assert.Equal(new TextRange(1, 2), placeholders.GetWordBoundary(new TextPosition(1)));
        using Paragraph emoji = BuildParagraph("x👨‍👩‍👦y");
        Assert.Equal(new TextRange(1, 9), emoji.GetWordBoundary(new TextPosition(5)));
    }

    [Fact]
    public void ParagraphCachesTheHostWordBoundaries()
    {
        Func<string, int[]?>? previous = TextSegmentation.WordBoundaryProvider;
        int calls = 0;
        try
        {
            TextSegmentation.WordBoundaryProvider = text =>
            {
                calls++;
                Assert.Equal("中文测试", text);
                return [0, 2, 4];
            };
            using Paragraph paragraph = BuildParagraph("中文测试");
            Assert.Equal(new TextRange(0, 2), paragraph.GetWordBoundary(new TextPosition(1)));
            Assert.Equal(new TextRange(2, 4), paragraph.GetWordBoundary(new TextPosition(3)));
            Assert.Equal(1, calls);
        }
        finally
        {
            TextSegmentation.WordBoundaryProvider = previous;
        }
    }

    [Fact]
    public void TripleClickDragBackwardsInvolvingWidgetSpans()
    {
        // Flutter's regression: selectable_region_test.dart, "triple-click-drag backwards
        // involving WidgetSpans". The outer paragraphs contain only placeholders and newlines.
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(home: new SelectionArea(new ListView(children:
        [
            WidgetParagraph("Text A.", "Text B.", "Text C."),
            WidgetParagraph("Text D.", "Text E.", "Text F."),
        ]))));
        tester.PumpAndSettle();
        var paragraphs = tester.ElementsOfType<RichText>()
            .Select(element => (RenderParagraph)element.RenderObject!).ToList();
        RenderParagraph Find(string text) => paragraphs.Single(p => p.Text.ToPlainText() == text);
        RenderParagraph paragraphE = Find("Text E.");
        RenderParagraph paragraphB = Find("Text B.");
        Point start = TextOffsetToPosition(paragraphE, 2);
        TestGesture gesture = tester.StartGesture(start, PointerDeviceKind.Mouse);
        tester.Pump();
        gesture.Up();
        tester.Pump();
        gesture.Down(start);
        tester.Pump();
        gesture.Up();
        tester.Pump();
        gesture.Down(start);
        tester.PumpAndSettle();
        Assert.Equal([new TextSelection(0, 7)], paragraphE.Selections);
        gesture.MoveTo(TextOffsetToPosition(paragraphB, 3));
        tester.PumpAndSettle();
        gesture.Up();
        tester.PumpAndSettle();
        foreach (string text in new[] { "Text B.", "Text C.", "Text D.", "Text E." })
        {
            Assert.Equal([new TextSelection(7, 0)], Find(text).Selections);
        }

        var outer = paragraphs.Where(p => p.Text.ToPlainText() == "\uFFFC\n\uFFFC\n\uFFFC").ToList();
        Assert.Equal(2, outer.Count);
        Assert.Equal([new TextSelection(4, 3)], outer[0].Selections);
        Assert.Equal([new TextSelection(2, 1)], outer[1].Selections);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void NestedOriginEdgesFollowTheEnclosingParagraph(bool isEnd, bool reversed, bool outside)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(home: new SelectionArea(Text.Rich(new TextSpan("head ", children:
        [
            // An intervening render object exercises ancestor traversal, not only the direct parent.
            new WidgetSpan(new Padding(EdgeInsets.All(2), new Text("inner"))),
            new TextSpan(" tail\nnext"),
        ])))));
        var paragraphs = tester.ElementsOfType<RichText>()
            .Select(element => (RenderParagraph)element.RenderObject!).ToList();
        RenderParagraph inner = paragraphs.Single(p => p.Text.ToPlainText() == "inner");
        RenderParagraph outer = paragraphs.Single(p => p.Text.ToPlainText() == "head \uFFFC tail\nnext");
        foreach (SelectableFragment fragment in outer.LastSelectableFragments!)
        {
            fragment.DispatchSelectionEvent(new SelectParagraphSelectionEvent(default, absorb: true));
        }

        SelectableFragment selectable = Assert.Single(inner.LastSelectableFragments!);
        selectable.DispatchSelectionEvent(new SelectParagraphSelectionEvent(default, absorb: true));
        selectable.DebugSetSelection(reversed ? 5 : 0, reversed ? 0 : 5);
        Point target = outside
            ? outer.LocalToGlobal(new Point(0, outer.Size.Height + 100))
            : TextOffsetToPosition(outer, 13);
        SelectionResult result = selectable.DispatchSelectionEvent(isEnd
            ? SelectionEdgeUpdateEvent.ForEnd(target, TextGranularity.Paragraph)
            : SelectionEdgeUpdateEvent.ForStart(target, TextGranularity.Paragraph));
        Assert.Equal(SelectionResult.Next, result);
        Assert.Equal([isEnd ? new TextSelection(0, 5) : new TextSelection(5, 0)], inner.Selections);

        // Cross the other side of the ancestor. Both edge algorithms must keep the whole origin.
        Point before = outer.LocalToGlobal(new Point(0, -100));
        result = selectable.DispatchSelectionEvent(isEnd
            ? SelectionEdgeUpdateEvent.ForEnd(before, TextGranularity.Paragraph)
            : SelectionEdgeUpdateEvent.ForStart(before, TextGranularity.Paragraph));
        Assert.Equal(SelectionResult.Previous, result);
        Assert.Equal([isEnd ? new TextSelection(5, 0) : new TextSelection(0, 5)], inner.Selections);
    }

    private static Widget WidgetParagraph(string first, string second, string third) => Text.Rich(new TextSpan(
        children:
        [
            new WidgetSpan(new Text(first)), new TextSpan("\n"),
            new WidgetSpan(new Text(second)), new TextSpan("\n"),
            new WidgetSpan(new Text(third)),
        ]));

    private static Point TextOffsetToPosition(RenderParagraph paragraph, int offset)
    {
        Point caret = paragraph.GetOffsetForCaret(new TextPosition(offset), new Rect(0, 0, 2, 20));
        return paragraph.LocalToGlobal(caret + new Vector(0, paragraph.PreferredLineHeight - 2));
    }

    private static Paragraph BuildParagraph(string text)
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        builder.AddText(text);
        Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));
        return paragraph;
    }
}
