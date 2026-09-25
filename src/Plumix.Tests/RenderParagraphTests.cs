using Avalonia;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/rendering/paragraph_test.dart
//
// The cases that exercise RenderParagraph's text painter: caret, box and position queries, overflow,
// max lines, inline placeholders and span hit testing. Headless layout uses Flutter's `FlutterTest`
// font metrics, so the expected values are Flutter's own.

public sealed class RenderParagraphTests
{
    private const string KText =
        "I polished up that handle so carefullee\nThat now I am the Ruler of the Queen's Navee!";

    [Fact]
    public void GetOffsetForCaret_ControlTest()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText));
        LayOut(paragraph);

        var caret = new Rect(0.0, 0.0, 2.0, 20.0);
        Point offset5 = paragraph.GetOffsetForCaret(new TextPosition(5), caret);
        Assert.True(offset5.X > 0.0);

        Point offset25 = paragraph.GetOffsetForCaret(new TextPosition(25), caret);
        Assert.True(offset25.X > offset5.X);

        Point offset50 = paragraph.GetOffsetForCaret(new TextPosition(50), caret);
        Assert.True(offset50.Y > offset5.Y);
    }

    [Fact]
    public void GetFullHeightForCaret_ControlTest()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText, style: new TextStyle(FontSize: 10.0)));
        LayOut(paragraph);
        Assert.Equal(10.0, paragraph.GetFullHeightForCaret(new TextPosition(5)));
    }

    [Fact]
    public void GetPositionForOffset_ControlTest()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText));
        LayOut(paragraph);

        TextPosition position20 = paragraph.GetPositionForOffset(new Point(20.0, 5.0));
        Assert.True(position20.Offset > 0);

        TextPosition position40 = paragraph.GetPositionForOffset(new Point(40.0, 5.0));
        Assert.True(position40.Offset > position20.Offset);

        TextPosition positionBelow = paragraph.GetPositionForOffset(new Point(5.0, 20.0));
        Assert.True(positionBelow.Offset > position40.Offset);
    }

    [Fact]
    public void GetBoxesForSelection_ControlTest()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText, style: new TextStyle(FontSize: 10.0)));
        LayOut(paragraph);

        Assert.Single(paragraph.GetBoxesForSelection(new TextSelection(5, 25)));

        IReadOnlyList<TextBox> boxes = paragraph.GetBoxesForSelection(new TextSelection(25, 50));
        Assert.Contains(boxes, box => box.Left == 250.0 && box.Top == 0.0);
        Assert.Contains(boxes, box => box.Right == 100.0 && box.Top == 10.0);
    }

    [Fact]
    public void GetBoxesForSelection_WithMultipleTextSpansAndLines()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            "First ",
            style: new TextStyle(FontSize: 10.0),
            children:
            [
                new TextSpan("smallsecond ", style: new TextStyle(FontSize: 5.0)),
                new TextSpan("third fourth fifth"),
            ]));
        LayOut(paragraph, new BoxConstraints(MaxWidth: 140.0));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0.0, 0.0, 60.0, 10.0, TextDirection.Ltr),
                TextBox.FromLTRBD(60.0, 3.75, 120.0, 8.75, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 10.0, 130.0, 20.0, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 20.0, 50.0, 30.0, TextDirection.Ltr),
            ],
            paragraph.GetBoxesForSelection(new TextSelection(0, 36)));
    }

    [Fact]
    public void GetBoxesForSelection_WithMaxHeightAndWidthStyles()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            "First ",
            style: new TextStyle(FontSize: 10.0),
            children:
            [
                new TextSpan("smallsecond ", style: new TextStyle(FontSize: 8.0)),
                new TextSpan("third fourth fifth"),
            ]));
        LayOut(paragraph, new BoxConstraints(MaxWidth: 160.0));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0.0, 0.0, 60.0, 10.0, TextDirection.Ltr),
                TextBox.FromLTRBD(60.0, 0.0, 156.0, 10.0, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 10.0, 130.0, 20.0, TextDirection.Ltr),
                TextBox.FromLTRBD(130.0, 10.0, 156.0, 20.0, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 20.0, 50.0, 30.0, TextDirection.Ltr),
            ],
            paragraph.GetBoxesForSelection(
                new TextSelection(0, 36),
                BoxHeightStyle.Max,
                BoxWidthStyle.Max));
    }

    [Fact]
    public void GetWordBoundary_ControlTest()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText));
        LayOut(paragraph);

        Assert.Equal("polished", paragraph.GetWordBoundary(new TextPosition(5)).TextInside(KText));
        Assert.Equal(" ", paragraph.GetWordBoundary(new TextPosition(50)).TextInside(KText));
        Assert.Equal("Queen's", paragraph.GetWordBoundary(new TextPosition(75)).TextInside(KText));
    }

    [Fact]
    public void Overflow_Test()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            "This\n"
            + "is a wrapping test. It should wrap at manual newlines, and if softWrap is true, also at spaces.",
            style: new TextStyle(FontSize: 10.0)))
        {
            MaxLines = 1,
            SoftWrap = true,
        };
        var constraints = new BoxConstraints(MaxWidth: 50.0);
        LayOut(paragraph, constraints);
        double lineHeight = paragraph.Size.Height;

        void RelayoutWith(int? maxLines, bool softWrap, TextOverflow overflow)
        {
            paragraph.MaxLines = maxLines;
            paragraph.SoftWrap = softWrap;
            paragraph.Overflow = overflow;
            paragraph.Layout(constraints, parentUsesSize: true);
        }

        RelayoutWith(3, true, TextOverflow.Clip);
        Assert.Equal(lineHeight * 3.0, paragraph.Size.Height);

        RelayoutWith(null, true, TextOverflow.Clip);
        Assert.True(paragraph.Size.Height > lineHeight * 5.0);

        RelayoutWith(1, true, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight, paragraph.Size.Height);

        RelayoutWith(3, true, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight * 3.0, paragraph.Size.Height);

        RelayoutWith(null, true, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight * 2.0, paragraph.Size.Height);

        RelayoutWith(1, false, TextOverflow.Clip);
        Assert.Equal(lineHeight, paragraph.Size.Height);

        RelayoutWith(3, false, TextOverflow.Clip);
        Assert.Equal(lineHeight * 2.0, paragraph.Size.Height);

        RelayoutWith(null, false, TextOverflow.Clip);
        Assert.Equal(lineHeight * 2.0, paragraph.Size.Height);

        RelayoutWith(1, false, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight, paragraph.Size.Height);

        RelayoutWith(3, false, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight * 3.0, paragraph.Size.Height);

        RelayoutWith(null, false, TextOverflow.Ellipsis);
        Assert.Equal(lineHeight * 2.0, paragraph.Size.Height);

        RelayoutWith(3, true, TextOverflow.Fade);
        Assert.True(paragraph.DebugHasOverflowShader);

        RelayoutWith(3, true, TextOverflow.Ellipsis);
        Assert.False(paragraph.DebugHasOverflowShader);

        RelayoutWith(100, true, TextOverflow.Fade);
        Assert.False(paragraph.DebugHasOverflowShader);
    }

    [Fact]
    public void MaxLines_LimitTheHeight()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            "How do you write like you're running out of time? Write day and night like you're running out of time?",
            style: new TextStyle(FontSize: 10.0)));
        var constraints = new BoxConstraints(MaxWidth: 100.0);
        LayOut(paragraph, constraints);

        void LayoutAt(int? maxLines)
        {
            paragraph.MaxLines = maxLines;
            paragraph.Layout(constraints, parentUsesSize: true);
        }

        LayoutAt(null);
        Assert.Equal(130.0, paragraph.Size.Height);

        LayoutAt(1);
        Assert.Equal(10.0, paragraph.Size.Height);

        LayoutAt(2);
        Assert.Equal(20.0, paragraph.Size.Height);

        LayoutAt(3);
        Assert.Equal(30.0, paragraph.Size.Height);
    }

    [Fact]
    public void TextAlign_TriggersTextPainterRelayoutInThePaintMethod()
    {
        var paragraph = new RenderParagraph(new TextSpan("A", style: new TextStyle(FontSize: 10.0)))
        {
            TextAlign = TextAlign.Left,
        };

        Rect RectForA() => paragraph.GetBoxesForSelection(new TextSelection(0, 1)).Single().ToRect();

        LayOut(paragraph, new BoxConstraints(MinWidth: 100.0, MaxWidth: 100.0));
        Assert.Equal(new Rect(0, 0, 10, 10), RectForA());

        paragraph.TextAlign = TextAlign.Right;
        Assert.False(paragraph.DebugNeedsLayout);
        Assert.True(paragraph.DebugNeedsPaint);

        paragraph.Paint(new PaintingContext(new ContainerLayer()), default);
        Assert.Equal(new Rect(90, 0, 10, 10), RectForA());
    }

    [Fact]
    public void DevicePixelRatio_DoesNotInvalidateLayoutOrPaint()
    {
        var paragraph = new RenderParagraph(new TextSpan("Hello"));
        LayOut(paragraph);
        Assert.Equal(1.0, paragraph.DevicePixelRatio);

        paragraph.DevicePixelRatio = 2.0;
        Assert.Equal(2.0, paragraph.DevicePixelRatio);
        Assert.False(paragraph.DebugNeedsLayout);
    }

    [Fact]
    public void DidExceedMaxLines_ReportsTruncation()
    {
        RenderParagraph Create(int? maxLines = null, TextOverflow overflow = TextOverflow.Clip)
        {
            return new RenderParagraph(new TextSpan(
                "Here is a long text, maybe exceed maxlines",
                style: new TextStyle(FontSize: 10.0)))
            {
                MaxLines = maxLines,
                Overflow = overflow,
            };
        }

        var constraints = new BoxConstraints(MaxWidth: 100.0);
        RenderParagraph unlimited = Create();
        LayOut(unlimited, constraints);
        Assert.False(unlimited.DidExceedMaxLines);

        RenderParagraph oneLine = Create(maxLines: 1);
        LayOut(oneLine, constraints);
        Assert.True(oneLine.DidExceedMaxLines);

        RenderParagraph ellipsis = Create(overflow: TextOverflow.Ellipsis);
        LayOut(ellipsis, constraints);
        Assert.True(ellipsis.DidExceedMaxLines);
    }

    [Fact]
    public void ChangingColor_DoesNotRequireLayout()
    {
        var paragraph = new RenderParagraph(
            new TextSpan("Hello", style: new TextStyle(Color: Colors.Black)));
        var constraints = new BoxConstraints(MaxWidth: 100.0);
        LayOut(paragraph, constraints);

        paragraph.Text = new TextSpan("Hello World", style: new TextStyle(Color: Colors.Black));
        Assert.True(paragraph.DebugNeedsLayout);
        paragraph.Layout(constraints, parentUsesSize: true);

        paragraph.Text = new TextSpan("Hello World", style: new TextStyle(Color: Colors.Red));
        Assert.False(paragraph.DebugNeedsLayout);
        Assert.True(paragraph.DebugNeedsPaint);
    }

    [Fact]
    public void NestedTextSpans_HandleALinearTextScaler()
    {
        var testSpan = new TextSpan(
            "a",
            style: new TextStyle(FontSize: 10.0),
            children:
            [
                new TextSpan("b", style: new TextStyle(FontSize: 20.0), children: [new TextSpan("c")]),
                new TextSpan("d"),
            ]);
        var paragraph = new RenderParagraph(testSpan)
        {
            TextScaler = TextScaler.Linear(1.3),
        };
        // `new BoxConstraints()` on the record struct zero-initializes; Dart's default is unbounded.
        LayOut(paragraph, new BoxConstraints(MinWidth: 0.0));
        Assert.Equal(78.0, paragraph.Size.Width, precision: 10);
        Assert.Equal(26.0, paragraph.Size.Height, precision: 10);

        int length = testSpan.ToPlainText().Length;
        var boxes = new List<TextBox>();
        for (int index = 0; index < length; index++)
        {
            boxes.AddRange(paragraph.GetBoxesForSelection(new TextSelection(index, index + 1)));
        }

        Assert.Equal(4, boxes.Count);
        double[] expected = [13.0, 26.0, 26.0, 13.0];
        for (int index = 0; index < 4; index++)
        {
            Assert.Equal(expected[index], boxes[index].ToRect().Width, precision: 10);
            Assert.Equal(expected[index], boxes[index].ToRect().Height, precision: 10);
        }
    }

    [Fact]
    public void Locale_Setter()
    {
        var paragraph = new RenderParagraph(new TextSpan(KText))
        {
            Locale = "zh_HK",
        };
        Assert.Equal("zh_HK", paragraph.Locale);

        paragraph.Locale = "ja_JP";
        Assert.Equal("ja_JP", paragraph.Locale);
    }

    [Fact]
    public void InlineWidgets_Boxes()
    {
        var text = new TextSpan(
            "a",
            style: new TextStyle(FontSize: 10.0),
            children:
            [
                new WidgetSpan(new SizedBox(width: 21, height: 21)),
                new WidgetSpan(new SizedBox(width: 21, height: 21)),
                new TextSpan("a"),
                new WidgetSpan(new SizedBox(width: 21, height: 21)),
            ]);
        RenderParagraph paragraph = CreateWithInlineChildren(text, 3);
        LayOut(paragraph, new BoxConstraints(MaxWidth: 100.0));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0.0, 4.0, 10.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(10.0, 0.0, 24.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(24.0, 0.0, 38.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(38.0, 4.0, 48.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(48.0, 0.0, 62.0, 14.0, TextDirection.Ltr),
            ],
            paragraph.GetBoxesForSelection(new TextSelection(0, 8)));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0.0, 0.0, 10.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(10.0, 0.0, 24.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(24.0, 0.0, 38.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(38.0, 0.0, 48.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(48.0, 0.0, 62.0, 14.0, TextDirection.Ltr),
            ],
            paragraph.GetBoxesForSelection(new TextSelection(0, 8), BoxHeightStyle.Max));
    }

    [Fact]
    public void InlineWidgets_Multiline()
    {
        var widget = new SizedBox(width: 21, height: 21);
        var text = new TextSpan(
            "a",
            style: new TextStyle(FontSize: 10.0),
            children:
            [
                new WidgetSpan(widget), new WidgetSpan(widget), new TextSpan("a"), new WidgetSpan(widget),
                new WidgetSpan(widget), new WidgetSpan(widget), new WidgetSpan(widget), new WidgetSpan(widget),
            ]);
        RenderParagraph paragraph = CreateWithInlineChildren(text, 7);
        LayOut(paragraph, new BoxConstraints(MaxWidth: 50.0));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0.0, 4.0, 10.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(10.0, 0.0, 24.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(24.0, 0.0, 38.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(38.0, 4.0, 48.0, 14.0, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 14.0, 14.0, 28.0, TextDirection.Ltr),
                TextBox.FromLTRBD(14.0, 14.0, 28.0, 28.0, TextDirection.Ltr),
                TextBox.FromLTRBD(28.0, 14.0, 42.0, 28.0, TextDirection.Ltr),
                TextBox.FromLTRBD(0.0, 28.0, 14.0, 42.0, TextDirection.Ltr),
                TextBox.FromLTRBD(14.0, 28.0, 28.0, 42.0, TextDirection.Ltr),
            ],
            paragraph.GetBoxesForSelection(new TextSelection(0, 12)));
    }

    [Fact]
    public void HitTesting_BasicTextSpans()
    {
        var textSpanA = new TextSpan(new string('A', 10));
        var textSpanBC = new TextSpan("BC", style: new TextStyle(LetterSpacing: 26.0));
        var paragraph = new RenderParagraph(new TextSpan(
            style: new TextStyle(FontSize: 10.0),
            children: [textSpanA, textSpanBC]));
        LayOut(paragraph, new BoxConstraints(MinWidth: 100.0, MaxWidth: 100.0));

        AssertHit(paragraph, new Point(5.0, 5.0), textSpanA);
        AssertHit(paragraph, new Point(95.0, 5.0), textSpanA);
        AssertHit(paragraph, new Point(200.0, 5.0), null);
        AssertHit(paragraph, new Point(18.0, 15.0), textSpanBC);
        AssertHit(paragraph, new Point(31.0, 15.0), textSpanBC);
        AssertHit(paragraph, new Point(54.0, 15.0), textSpanBC);
        AssertHit(paragraph, new Point(100.0, 15.0), null);
        AssertHit(paragraph, new Point(9999.0, 9999.0), null);
    }

    [Fact]
    public void HitTesting_WithTextJustification()
    {
        var textSpanA = new TextSpan("A ");
        var textSpanB = new TextSpan("B\u200B");
        var textSpanC = new TextSpan(new string('C', 10));
        var paragraph = new RenderParagraph(new TextSpan(
            "",
            style: new TextStyle(FontSize: 10.0),
            children: [textSpanA, textSpanB, textSpanC]))
        {
            TextAlign = TextAlign.Justify,
        };
        LayOut(paragraph, new BoxConstraints(MinWidth: 100.0, MaxWidth: 100.0));

        AssertHit(paragraph, new Point(5.0, 5.0), textSpanA);
        AssertHit(paragraph, new Point(50.0, 5.0), textSpanA);
        AssertHit(paragraph, new Point(95.0, 5.0), textSpanB);
    }

    [Fact]
    public void Selection_GetPositionForOffsetAndHighlightBoxes()
    {
        var paragraph = new RenderParagraph(new TextSpan("1234567"));
        LayOut(paragraph);

        Assert.Equal(new TextPosition(3), paragraph.GetPositionForOffset(new Point(42.0, 14.0)));
        Assert.Equal(
            new Rect(14, 0, 56, 14),
            paragraph.GetBoxesForSelection(new TextSelection(1, 5)).Single().ToRect());
        Assert.Equal(
            new Rect(28, 0, 28, 14),
            paragraph.GetBoxesForSelection(new TextSelection(2, 4)).Single().ToRect());
    }

    private static RenderParagraph CreateWithInlineChildren(InlineSpan text, int childCount)
    {
        var children = new List<RenderBox>();
        for (int index = 0; index < childCount; index++)
        {
            children.Add(new RenderParagraph(new TextSpan("b")));
        }

        var paragraph = new RenderParagraph(text, children);
        int placeholderIndex = 0;
        text.VisitChildren(span =>
        {
            if (span is PlaceholderSpan placeholder)
            {
                ((TextParentData)children[placeholderIndex].parentData!).Span = placeholder;
                placeholderIndex++;
            }

            return true;
        });
        return paragraph;
    }

    private static void AssertHit(RenderParagraph paragraph, Point position, TextSpan? expected)
    {
        var result = new BoxHitTestResult();
        bool hit = paragraph.HitTest(result, position);
        Assert.Equal(expected is not null, hit);
        TextSpan[] spans = result.Path.Select(entry => entry.Target).OfType<TextSpan>().ToArray();
        if (expected is null)
        {
            Assert.Empty(spans);
        }
        else
        {
            Assert.Same(expected, Assert.Single(spans));
        }
    }

    // Dart's `layout(box)` puts the box under an 800x600 render view with loose constraints.
    private static void LayOut(RenderBox box, BoxConstraints? constraints = null)
    {
        var root = new RenderView(new FlutterView(new Size(800, 600))) { Child = box };
        var owner = new PipelineOwner(root);
        owner.Attach(root);
        box.Layout(constraints ?? new BoxConstraints(MaxWidth: 800, MaxHeight: 600), parentUsesSize: true);
    }
}
