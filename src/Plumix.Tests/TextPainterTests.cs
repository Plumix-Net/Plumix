using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/text_painter_test.dart
// Dart parity source: flutter/packages/flutter/test/painting/text_painter_rtl_test.dart
//
// Headless runs lay text out with the metrics of Flutter's `FlutterTest` font (each glyph one em wide,
// ascent 0.75 em, descent 0.25 em), so every expected value below is Flutter's own.

public sealed class TextPainterTests
{
    private const double SizeOfA = 14.0;

    // -- group('caret') ---------------------------------------------------------------

    [Fact]
    public void Caret_BasicAndTrailingSpaces()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);

        painter.Text = new TextSpan("A");
        painter.Layout();
        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(1), default).X);

        const string emoji = "A\U0001F600";
        painter.Text = new TextSpan(emoji);
        painter.Layout();
        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(emoji.Length), default).X);
        CheckCaretOffsetsLtr(emoji);

        // Trailing white spaces (U+00A0, U+2007 and U+202F are not trailing spaces).
        string[] spaces =
        [
            " ", "\u3000", "\u1680", "\u2000", "\u2001", "\u2002", "\u2003", "\u2004", "\u2005",
            "\u2006", "\u2008", "\u2009", "\u200A", "\u205F",
        ];
        foreach (string space in spaces)
        {
            string text = "A" + space;
            painter.Text = new TextSpan(text);
            painter.Layout();
            Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
            Assert.Equal(14.0, painter.GetOffsetForCaret(new TextPosition(1), default).X);
            Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(text.Length), default).X);

            painter.Layout(maxWidth: 14.0);
            IReadOnlyList<LineMetrics> lines = painter.ComputeLineMetrics();
            Assert.Single(lines);
            Assert.Equal(14.0, lines[0].Width);
        }
    }

    [Fact]
    public void Caret_WithWidgetSpanReachesTheEnd()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);
        painter.Text = new TextSpan(children:
        [
            new TextSpan("before"),
            new WidgetSpan(new Text("widget")),
            new TextSpan("after"),
        ]);
        painter.SetPlaceholderDimensions(
        [
            new PlaceholderDimensions(new Size(50, 30), PlaceholderAlignment.Bottom, baselineOffset: 25),
        ]);
        painter.Layout();
        int length = painter.Text.ToPlainText().Length;
        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(length), default).X);
    }

    [Fact]
    public void Caret_NullTextAndEmptyChildren()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);
        painter.Text = new TextSpan(children: [new TextSpan("B"), new TextSpan("C")]);
        painter.Layout();
        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
        Assert.Equal(painter.Width / 2, painter.GetOffsetForCaret(new TextPosition(1), default).X);
        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(2), default).X);

        painter.Text = new TextSpan(children: []);
        painter.Layout();
        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(1), default).X);
    }

    [Fact]
    public void Caret_Emoji()
    {
        // 👩‍👩‍👦👩‍👩‍👧‍👧👏🏽
        const string text = "\U0001F469\u200D\U0001F469\u200D\U0001F466"
                            + "\U0001F469\u200D\U0001F469\u200D\U0001F467\u200D\U0001F467"
                            + "\U0001F44F\U0001F3FD";
        using var painter = new TextPainter(text: new TextSpan(text), textDirection: TextDirection.Ltr);
        Assert.Equal(23, text.Length);
        painter.Layout(maxWidth: 10000);

        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
        for (int offset = 1; offset <= 8; offset++)
        {
            Assert.Equal(42.0, painter.GetOffsetForCaret(new TextPosition(offset), default).X);
        }

        for (int offset = 9; offset <= 19; offset++)
        {
            Assert.Equal(98.0, painter.GetOffsetForCaret(new TextPosition(offset), default).X);
        }

        for (int offset = 20; offset <= 23; offset++)
        {
            Assert.Equal(126.0, painter.GetOffsetForCaret(new TextPosition(offset), default).X);
        }

        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(23), default).X);
    }

    [Theory]
    [InlineData("\U0001F469\u200D\U0001F680")]
    [InlineData("\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F469")]
    [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466")]
    [InlineData("\U0001F468\U0001F3FE\u200D\U0001F91D\u200D\U0001F468\U0001F3FB")]
    [InlineData("\U0001F468\u200D\U0001F466")]
    [InlineData("\U0001F469\u200D\U0001F466")]
    [InlineData("\U0001F3CC\U0001F3FF\u200D\u2640\uFE0F")]
    [InlineData("\U0001F3CA\u200D\u2640\uFE0F")]
    [InlineData("\U0001F3C4\U0001F3FB\u200D\u2642\uFE0F")]
    [InlineData("\U0001F1FA\U0001F1F3")]
    [InlineData("\U0001F469\u200D\u2764\uFE0F\u200D\U0001F468")]
    public void Caret_SingleLongEmoji(string text)
    {
        CheckCaretOffsetsLtr(text);
    }

    [Theory]
    [InlineData("a\U0001F469\u200D\U0001F680")]
    [InlineData("ab\U0001F469\u200D\U0001F680")]
    [InlineData("abc\U0001F469\u200D\U0001F680")]
    [InlineData("abcd\U0001F469\u200D\U0001F680")]
    public void Caret_LettersThenOneEmojiOfFiveCodeUnits(string text)
    {
        CheckCaretOffsetsLtr(text);
    }

    [Fact]
    public void Caret_Zalgo()
    {
        CheckCaretOffsetsLtr("Z\u0349\u0333\u033A\u0365\u036C\u033Ea\u0334\u0355\u0332\u0312\u0312\u034C\u034B\u036Al"
                             + "\u0328\u034E\u0330\u0318\u0349\u031F\u0364\u0300\u0308\u031A\u035Cg\u0355\u0354\u0324"
                             + "\u0356\u031F\u0312\u035D\u0345o\u0335\u0321\u0321\u033C\u035A\u0310\u036F\u0305\u036A"
                             + "\u0306\u0363\u031A");
    }

    [Fact]
    public void Caret_Devanagari()
    {
        CheckCaretOffsetsLtrFromPieces(
        [
            "\u092A\u094D\u0930\u093E", "\u092A\u094D\u0924", " ", "\u0935", "\u0930\u094D\u0923", "\u0928", " ",
            "\u092A\u094D\u0930", "\u0935\u094D\u0930\u0941", "\u0924\u093F",
        ]);
    }

    [Fact]
    public void Caret_LtrLettersNextToEmojiAsSeparateTextBoxes()
    {
        var bold = new TextStyle(FontWeight: Avalonia.Media.FontWeight.Bold);
        Assert.Equal(
            [0, 28, 28, 28, 28, 28, 42, 56, 70, 84, 98, 112],
            CaretOffsetsForTextSpan(
                TextDirection.Ltr,
                new TextSpan(children:
                [
                    new TextSpan("\U0001F469\u200D\U0001F680", style: new TextStyle()),
                    new TextSpan(" words", style: bold),
                ])));
        Assert.Equal(
            [0, 14, 28, 42, 56, 70, 84, 112, 112, 112, 112, 112],
            CaretOffsetsForTextSpan(
                TextDirection.Ltr,
                new TextSpan(children:
                [
                    new TextSpan("words ", style: bold),
                    new TextSpan("\U0001F469\u200D\U0001F680"),
                ])));
    }

    [Fact]
    public void Caret_RtlLettersNextToEmojiAsSeparateTextBoxes()
    {
        var bold = new TextStyle(FontWeight: Avalonia.Media.FontWeight.Bold);
        Assert.Equal(
            [112, 84, 84, 84, 84, 84, 70, 56, 42, 28, 14, 0],
            CaretOffsetsForTextSpan(
                TextDirection.Rtl,
                new TextSpan(children:
                [
                    new TextSpan("\U0001F469\u200D\U0001F680", style: new TextStyle()),
                    new TextSpan(" \u05DE\u05D9\u05DC\u05D9\u05DD", style: bold),
                ])));
        Assert.Equal(
            [112, 98, 84, 70, 56, 42, 28, 0, 0, 0, 0, 0],
            CaretOffsetsForTextSpan(
                TextDirection.Rtl,
                new TextSpan(children:
                [
                    new TextSpan("\u05DE\u05D9\u05DC\u05D9\u05DD ", style: bold),
                    new TextSpan("\U0001F469\u200D\U0001F680"),
                ])));
    }

    [Fact]
    public void Caret_CenterAlignedTrailingSpaces()
    {
        const string text = "test text with space at end   ";
        using var painter = new TextPainter(
            text: new TextSpan(text),
            textDirection: TextDirection.Ltr,
            textAlign: TextAlign.Center);
        painter.Layout();

        Assert.Equal(21.0, painter.GetOffsetForCaret(new TextPosition(0), default).X);
        // The line end is 441, clamped to the content width.
        Assert.Equal(painter.Width, painter.GetOffsetForCaret(new TextPosition(text.Length), default).X);
        Assert.Equal(35.0, painter.GetOffsetForCaret(new TextPosition(1), default).X);
        Assert.Equal(49.0, painter.GetOffsetForCaret(new TextPosition(2), default).X);
    }

    [Fact]
    public void Caret_HeightFollowsTheStrut()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(Height: 1.0)),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(FontSize: 50.0));
        painter.Layout();
        Assert.Equal(50.0, painter.GetFullHeightForCaret(new TextPosition(0), default));
    }

    [Fact]
    public void Caret_AffinityDoesNotMatterWithinOneBidiRun()
    {
        using var painter = new TextPainter(text: new TextSpan("aa"), textDirection: TextDirection.Ltr);
        painter.Layout();
        var prototype = new Rect(0, 0, 5, 5);
        Assert.Equal(
            painter.GetOffsetForCaret(new TextPosition(1), prototype),
            painter.GetOffsetForCaret(new TextPosition(1, TextAffinity.Upstream), prototype));
    }

    [Fact]
    public void Caret_TrailingSpacesInLtrAndRtl()
    {
        var prototype = new Rect(0, 0, 5, 5);
        using var ltr = new TextPainter(text: new TextSpan("a    "), textDirection: TextDirection.Ltr);
        ltr.Layout(minWidth: 1000, maxWidth: 1000);
        Assert.Equal(SizeOfA * 5, ltr.GetOffsetForCaret(new TextPosition(5), prototype).X);

        using var rtl = new TextPainter(text: new TextSpan("\u0644    "), textDirection: TextDirection.Rtl);
        rtl.Layout(minWidth: 1000, maxWidth: 1000);
        Assert.Equal(1000 - (SizeOfA * 5) - 5, rtl.GetOffsetForCaret(new TextPosition(5), prototype).X);
    }

    [Fact]
    public void Caret_EndOfTextWithAPlusOneBidiLevel()
    {
        var prototype = new Rect(0, 0, 5, 5);
        using var painter = new TextPainter(text: new TextSpan("a\u0644"), textDirection: TextDirection.Ltr);
        painter.Layout(minWidth: 1000, maxWidth: 1000);
        Assert.Equal(0.0, painter.GetOffsetForCaret(new TextPosition(0), prototype).X);
        Assert.Equal((SizeOfA * 2) - 5, painter.GetOffsetForCaret(new TextPosition(1), prototype).X);
        Assert.Equal(SizeOfA * 2, painter.GetOffsetForCaret(new TextPosition(2), prototype).X);
    }

    [Fact]
    public void Caret_HandlesNewlines()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);

        void ExpectCaret(int offset, double dx, double dy)
        {
            Assert.Equal(new Point(dx, dy), painter.GetOffsetForCaret(new TextPosition(offset), default));
            Assert.Equal(
                new Point(dx, dy),
                painter.GetOffsetForCaret(new TextPosition(offset, TextAffinity.Upstream), default));
        }

        painter.Text = new TextSpan("aaa");
        painter.Layout();
        for (int offset = 0; offset <= 3; offset++)
        {
            ExpectCaret(offset, SizeOfA * offset, 0);
        }

        painter.Text = new TextSpan("\n\n");
        painter.Layout();
        ExpectCaret(0, 0, 0);
        ExpectCaret(1, 0, SizeOfA);
        ExpectCaret(2, 0, SizeOfA * 2);

        painter.Text = new TextSpan("\naaa");
        painter.Layout();
        ExpectCaret(0, 0, 0);
        ExpectCaret(1, 0, SizeOfA);

        painter.Text = new TextSpan("aaaaaaaa");
        painter.Layout(maxWidth: 100);
        Assert.Equal(new Point(0, SizeOfA), painter.GetOffsetForCaret(new TextPosition(7), default));
        Assert.Equal(
            new Point(SizeOfA * 7, 0),
            painter.GetOffsetForCaret(new TextPosition(7, TextAffinity.Upstream), default));

        painter.Text = new TextSpan("aaa\n");
        painter.Layout();
        ExpectCaret(4, 0, SizeOfA);

        painter.TextAlign = TextAlign.Right;
        painter.Text = new TextSpan("aaa");
        painter.Layout();
        Assert.Equal(new Point(0, 0), painter.GetOffsetForCaret(new TextPosition(0), default));
        painter.TextAlign = TextAlign.Left;

        painter.Text = new TextSpan("aaa\naaa");
        painter.Layout();
        ExpectCaret(4, 0, SizeOfA);
        ExpectCaret(3, SizeOfA * 3, 0);

        painter.Text = new TextSpan("aaa\n\n\n");
        painter.Layout();
        ExpectCaret(4, 0, SizeOfA);
        ExpectCaret(5, 0, SizeOfA * 2);
        ExpectCaret(6, 0, SizeOfA * 3);

        painter.Text = new TextSpan("\n\n\naaa");
        painter.Layout();
        ExpectCaret(3, 0, SizeOfA * 3);
        ExpectCaret(2, 0, SizeOfA * 2);
        ExpectCaret(1, 0, SizeOfA);
        ExpectCaret(0, 0, 0);
    }

    [Fact]
    public void Caret_HeightReflectsTheRunHeightWhenTheStrutIsDisabled()
    {
        using var painter = new TextPainter(
            text: new TextSpan(
                "M",
                style: new TextStyle(FontSize: 128),
                children:
                [
                    new TextSpan("M", style: new TextStyle(FontSize: 32)),
                    new TextSpan("M", style: new TextStyle(FontSize: 64)),
                ]),
            textDirection: TextDirection.Ltr);
        painter.Layout();

        double Height(int offset, TextAffinity affinity = TextAffinity.Downstream) =>
            painter.GetFullHeightForCaret(new TextPosition(offset, affinity), default);

        Assert.Equal(128.0, Height(0, TextAffinity.Upstream));
        Assert.Equal(128.0, Height(0));
        Assert.Equal(128.0, Height(1, TextAffinity.Upstream));
        Assert.Equal(32.0, Height(1));
        Assert.Equal(32.0, Height(2, TextAffinity.Upstream));
        Assert.Equal(64.0, Height(2));
        Assert.Equal(64.0, Height(3, TextAffinity.Upstream));
        Assert.Equal(128.0, Height(3));
    }

    [Fact]
    public void Caret_FullHeightHandlesADegenerateLayout()
    {
        using var painter = new TextPainter(
            text: new TextSpan("", style: new TextStyle(Height: 1.0, FontSize: 0.0)),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(painter.PreferredLineHeight, painter.GetFullHeightForCaret(new TextPosition(0), default));
    }

    // -- top-level tests -------------------------------------------------------------------

    [Fact]
    public void Paint_BeforeLayoutThrows()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => painter.Paint(new Canvas(new PictureRecorder()), default));
        Assert.Contains("TextPainter.paint called when text geometry was not yet calculated", exception.Message);
    }

    [Fact]
    public void Layout_RequiresATextDirection()
    {
        using var painter = new TextPainter(text: new TextSpan(""));
        Assert.Throws<InvalidOperationException>(() => painter.Layout());

        using var withDirection = new TextPainter(text: new TextSpan(""), textDirection: TextDirection.Rtl);
        withDirection.Layout();
    }

    [Fact]
    public void Size_MatchesTheFontSize()
    {
        using var painter = new TextPainter(
            text: new TextSpan("X", style: new TextStyle(Inherit: false, FontSize: 123.0)),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(new Size(123.0, 123.0), painter.Size);
    }

    [Fact]
    public void TextScaler_ScalesStyledAndUnstyledText()
    {
        using var styled = new TextPainter(
            text: new TextSpan("X", style: new TextStyle(Inherit: false, FontSize: 10.0)),
            textDirection: TextDirection.Ltr,
            textScaler: TextScaler.Linear(2.0));
        styled.Layout();
        Assert.Equal(new Size(20.0, 20.0), styled.Size);

        using var unstyled = new TextPainter(
            text: new TextSpan("X"),
            textDirection: TextDirection.Ltr,
            textScaler: TextScaler.Linear(2.0));
        unstyled.Layout();
        Assert.Equal(new Size(28.0, 28.0), unstyled.Size);
    }

    [Fact]
    public void PreferredLineHeight_DefaultsTo14AndFollowsTheRootStyle()
    {
        using var painter = new TextPainter(text: new TextSpan("x"), textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(14.0, painter.PreferredLineHeight);
        Assert.Equal(new Size(14.0, 14.0), painter.Size);

        using var large = new TextPainter(
            text: new TextSpan("x", style: new TextStyle(FontSize: 100.0)),
            textDirection: TextDirection.Ltr);
        large.Layout();
        Assert.Equal(100.0, large.PreferredLineHeight);
        Assert.Equal(new Size(100.0, 100.0), large.Size);
    }

    [Fact]
    public void WidgetSpan_CaretOffsetsAndPlaceholderBoxes()
    {
        static WidgetSpan Placeholder() => new(new SizedBox(width: 50, height: 30));
        using var painter = new TextPainter(
            text: new TextSpan(
                "test",
                children:
                [
                    Placeholder(), new TextSpan("test"), Placeholder(), Placeholder(), new TextSpan("test"),
                    Placeholder(), Placeholder(), Placeholder(), Placeholder(), Placeholder(), Placeholder(),
                    Placeholder(), Placeholder(), Placeholder(), Placeholder(), Placeholder(),
                ]),
            textDirection: TextDirection.Ltr);
        var dimensions = new List<PlaceholderDimensions>();
        for (int index = 0; index < 14; index++)
        {
            dimensions.Add(new PlaceholderDimensions(
                new Size(index == 12 ? 51 : 50, 30),
                PlaceholderAlignment.Bottom,
                baselineOffset: 25));
        }

        painter.SetPlaceholderDimensions(dimensions);
        painter.Layout(maxWidth: 500);

        (int Offset, double Dx)[] carets =
        [
            (1, 14), (4, 56), (5, 106), (6, 120), (10, 212), (11, 262), (12, 276), (13, 290), (14, 304),
            (15, 318), (16, 368), (17, 418), (18, 0), (19, 50), (23, 250),
        ];
        foreach ((int offset, double dx) in carets)
        {
            Assert.Equal(dx, painter.GetOffsetForCaret(new TextPosition(offset), default).X);
        }

        IReadOnlyList<TextBox> boxes = painter.InlinePlaceholderBoxes!;
        Assert.Equal(14, boxes.Count);
        Assert.Equal(TextBox.FromLTRBD(56, 0, 106, 30, TextDirection.Ltr), boxes[0]);
        Assert.Equal(TextBox.FromLTRBD(212, 0, 262, 30, TextDirection.Ltr), boxes[2]);
        Assert.Equal(TextBox.FromLTRBD(318, 0, 368, 30, TextDirection.Ltr), boxes[3]);
        Assert.Equal(TextBox.FromLTRBD(368, 0, 418, 30, TextDirection.Ltr), boxes[4]);
        Assert.Equal(TextBox.FromLTRBD(418, 0, 468, 30, TextDirection.Ltr), boxes[5]);
        Assert.Equal(TextBox.FromLTRBD(0, 30, 50, 60, TextDirection.Ltr), boxes[6]);
        Assert.Equal(TextBox.FromLTRBD(50, 30, 100, 60, TextDirection.Ltr), boxes[7]);
        Assert.Equal(TextBox.FromLTRBD(200, 30, 250, 60, TextDirection.Ltr), boxes[10]);
        Assert.Equal(TextBox.FromLTRBD(250, 30, 300, 60, TextDirection.Ltr), boxes[11]);
        Assert.Equal(TextBox.FromLTRBD(300, 30, 351, 60, TextDirection.Ltr), boxes[12]);
        Assert.Equal(TextBox.FromLTRBD(351, 30, 401, 60, TextDirection.Ltr), boxes[13]);
    }

    [Fact]
    public void TextHeightBehavior_CanBeSetBackToNull()
    {
        using var painter = new TextPainter();
        painter.TextHeightBehavior = new TextHeightBehavior();
        painter.TextHeightBehavior = null;
    }

    [Fact]
    public void LineMetrics_ForHardAndSoftBreaks()
    {
        const string text = "test1\nhello line two really long for soft break\nfinal line 4";
        using var painter = new TextPainter(text: new TextSpan(text), textDirection: TextDirection.Ltr);
        painter.Layout(maxWidth: 300);

        Assert.Equal(new TextSpan(text), painter.Text);
        Assert.Equal(14.0, painter.PreferredLineHeight);

        IReadOnlyList<LineMetrics> lines = painter.ComputeLineMetrics();
        Assert.Equal(4, lines.Count);
        Assert.Equal([true, false, true, true], lines.Select(line => line.HardBreak));
        Assert.All(lines, line => Assert.Equal(10.5, line.Ascent));
        Assert.All(lines, line => Assert.Equal(3.5, line.Descent));
        Assert.All(lines, line => Assert.Equal(10.5, line.UnscaledAscent));
        Assert.Equal([10.5, 24.5, 38.5, 52.5], lines.Select(line => line.Baseline));
        Assert.All(lines, line => Assert.Equal(14.0, line.Height));
        Assert.Equal([70.0, 294.0, 266.0, 168.0], lines.Select(line => line.Width));
        Assert.All(lines, line => Assert.Equal(0.0, line.Left));
        Assert.Equal([0, 1, 2, 3], lines.Select(line => line.LineNumber));
    }

    [Theory]
    // (height, fontSize, textHeightBehavior trims) -> the glyph sits in the middle of the line.
    [InlineData(20.0, 1.0)]
    [InlineData(0.1, 10.0)]
    public void LeadingDistribution_EvenSplitsTheLeadingAroundTheGlyph(double height, double fontSize)
    {
        using var painter = new TextPainter(
            text: new TextSpan(
                "A",
                style: new TextStyle(
                    Height: height,
                    FontSize: fontSize,
                    LeadingDistribution: TextLeadingDistribution.Even)),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Rect glyphBox = painter.GetBoxesForSelection(new TextSelection(0, 1))[0].ToRect();
        double expected = ((height * fontSize) - fontSize) / 2;
        Assert.Equal(expected, glyphBox.Top, precision: 10);
        Assert.Equal(expected, painter.Size.Height - glyphBox.Bottom, precision: 10);
    }

    [Fact]
    public void LeadingDistribution_TrimmedByTextHeightBehavior()
    {
        using var painter = new TextPainter(
            text: new TextSpan(
                "A",
                style: new TextStyle(Height: 0.1, FontSize: 10, LeadingDistribution: TextLeadingDistribution.Even)),
            textDirection: TextDirection.Ltr,
            textHeightBehavior: new TextHeightBehavior(false, false));
        painter.Layout();
        Rect glyphBox = painter.GetBoxesForSelection(new TextSelection(0, 1))[0].ToRect();
        Assert.Equal(painter.Size, glyphBox.Size);
        Assert.Equal(new Point(0, 0), glyphBox.TopLeft);
    }

    [Fact]
    public void LeadingDistribution_FallsBackToTheParagraphStyle()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(Height: 20, FontSize: 1)),
            textDirection: TextDirection.Ltr,
            textHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even));
        painter.Layout();
        Rect glyphBox = painter.GetBoxesForSelection(new TextSelection(0, 1))[0].ToRect();
        Assert.Equal((20 - 1) / 2.0, glyphBox.Top);
        Assert.Equal((20 - 1) / 2.0, painter.Size.Height - glyphBox.Bottom);
    }

    [Fact]
    public void LeadingDistribution_DoesNothingWithoutAHeightMultiplier()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(FontSize: 1)),
            textDirection: TextDirection.Ltr,
            textHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even));
        painter.Layout();
        Rect evenBox = painter.GetBoxesForSelection(new TextSelection(0, 1))[0].ToRect();

        painter.TextHeightBehavior = new TextHeightBehavior();
        painter.Layout();
        Assert.Equal(evenBox, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].ToRect());
    }

    [Fact]
    public void InvalidUtf16_IsReportedSilentlyAndReplaced()
    {
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterErrorDetails? reported = null;
        FlutterError.OnError = details => reported = details;
        try
        {
            using var painter = new TextPainter(
                text: new TextSpan("Hello\uD83DWorld", style: new TextStyle(FontSize: 20.0)),
                textDirection: TextDirection.Ltr);
            painter.Layout();
            Assert.Equal(20.0, painter.Width);
            Assert.NotNull(reported?.Exception);
            Assert.True(reported!.Silent);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void Diacritic_UpstreamCaretAtTheEndIsTheWidth()
    {
        const string text = "\u0E1F\u0E2B\u0E49";
        using var painter = new TextPainter(text: new TextSpan(text), textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(
            painter.Width,
            painter.GetOffsetForCaret(new TextPosition(text.Length, TextAffinity.Upstream), default).X);
    }

    [Fact]
    public void LineMetrics_UpdateAfterLayout()
    {
        using var painter = new TextPainter(text: new TextSpan("word1 word2 word3"), textDirection: TextDirection.Ltr);
        painter.Layout(maxWidth: 80);
        Assert.Equal(3, painter.ComputeLineMetrics().Count);
        painter.Layout(maxWidth: 1000);
        Assert.Single(painter.ComputeLineMetrics());
    }

    [Fact]
    public void TextLayoutAccess_ThrowsWithTheInvalidatingStack()
    {
        using var painter = new TextPainter(text: new TextSpan("TEXT"), textDirection: TextDirection.Ltr);
        FlutterError neverLaidOut = Assert.Throws<FlutterError>(() => painter.GetPositionForOffset(default));
        Assert.Contains("The TextPainter has never been laid out.", neverLaidOut.ToString());

        painter.Layout();
        painter.GetPositionForOffset(default);

        painter.MarkNeedsLayout();
        FlutterError invalidated = Assert.Throws<FlutterError>(() => painter.GetPositionForOffset(default));
        Assert.Contains("The calls that first invalidated the text layout were:", invalidated.ToString());
    }

    [Fact]
    public void PlaceholderDimensions_ChangeRequiresLayoutBeforePaint()
    {
        using var painter = new TextPainter(
            text: new TextSpan(children:
            [
                new WidgetSpan(new SizedBox()), new WidgetSpan(new SizedBox()), new WidgetSpan(new SizedBox()),
            ]),
            textDirection: TextDirection.Ltr);
        painter.SetPlaceholderDimensions(
        [
            new PlaceholderDimensions(new Size(30, 30), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(40, 30), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(50, 30), PlaceholderAlignment.Bottom),
        ]);
        painter.Layout();

        // Identical dimensions keep the layout.
        painter.SetPlaceholderDimensions(
        [
            new PlaceholderDimensions(new Size(30, 30), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(40, 30), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(50, 30), PlaceholderAlignment.Bottom),
        ]);
        painter.Paint(new Canvas(new PictureRecorder()), default);

        painter.SetPlaceholderDimensions(
        [
            new PlaceholderDimensions(new Size(30, 30), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(40, 20), PlaceholderAlignment.Bottom),
            new PlaceholderDimensions(new Size(50, 30), PlaceholderAlignment.Bottom),
        ]);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => painter.Paint(new Canvas(new PictureRecorder()), default));
        Assert.Contains("TextPainter.paint called when text geometry was not yet calculated", exception.Message);
    }

    [Fact]
    public void Dispose_TracksDisposalAndAssertsOnSecondDispose()
    {
        var painter = new TextPainter();
        Assert.False(painter.DebugDisposed);
        painter.Dispose();
        Assert.True(painter.DebugDisposed);
        Assert.Throws<AssertionError>(() => painter.Dispose());
    }

    [Fact]
    public void ComputeWidth_AndComputeMaxIntrinsicWidth_MatchALaidOutPainter()
    {
        var text = new TextSpan("foobar");
        using var painter = new TextPainter(text: text, textDirection: TextDirection.Ltr);

        painter.Layout();
        Assert.Equal(painter.Width, TextPainter.ComputeWidth(text, TextDirection.Ltr));
        Assert.Equal(painter.MaxIntrinsicWidth, TextPainter.ComputeMaxIntrinsicWidth(text, TextDirection.Ltr));

        painter.Layout(minWidth: 500);
        Assert.Equal(painter.Width, TextPainter.ComputeWidth(text, TextDirection.Ltr, minWidth: 500));
        Assert.Equal(
            painter.MaxIntrinsicWidth,
            TextPainter.ComputeMaxIntrinsicWidth(text, TextDirection.Ltr, minWidth: 500));
    }

    [Fact]
    public void GetWordBoundary_KeepsEmojiZwjSequencesTogether()
    {
        const string family = "\U0001F468\u200D\U0001F469\u200D\U0001F466";
        using var painter = new TextPainter(
            text: new TextSpan(family + family + family),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(new TextRange(8, 16), painter.GetWordBoundary(new TextPosition(8)));
    }

    [Fact]
    public void Strut_WithTextHeightBehaviorOnAnEmptyParagraph()
    {
        var style = new TextStyle(Height: 11, FontSize: 7);
        using var painter = new TextPainter(
            text: new TextSpan("x", style: style),
            textDirection: TextDirection.Ltr,
            strutStyle: StrutStyle.FromTextStyle(style, forceStrutHeight: true),
            textHeightBehavior: new TextHeightBehavior(false, false));
        painter.Layout();
        double height = painter.Height;

        painter.Text = new TextSpan("", style: style);
        painter.Layout();
        Assert.Equal(height, painter.Height);
        Assert.Equal(height, painter.PreferredLineHeight);

        painter.Text = new TextSpan(style: style);
        painter.Layout();
        Assert.Equal(height, painter.Height);
        Assert.Equal(height, painter.PreferredLineHeight);
    }

    [Fact]
    public void PlainText_FlattensPlaceholders()
    {
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);
        Assert.Equal(string.Empty, painter.PlainText);

        painter.Text = new TextSpan(children:
        [
            new TextSpan("before\n"),
            new WidgetSpan(new Text("widget")),
            new TextSpan("after"),
        ]);
        Assert.Equal("before\n\uFFFCafter", painter.PlainText);
        painter.SetPlaceholderDimensions(
            [new PlaceholderDimensions(new Size(50, 30), PlaceholderAlignment.Bottom)]);
        painter.Layout();
        Assert.Equal("before\n\uFFFCafter", painter.PlainText);

        painter.Text = new TextSpan(children:
        [
            new TextSpan("be\nfo\nre\n"),
            new WidgetSpan(new Text("widget")),
            new TextSpan("af\nter"),
        ]);
        Assert.Equal("be\nfo\nre\n\uFFFCaf\nter", painter.PlainText);
        painter.Layout();
        Assert.Equal("be\nfo\nre\n\uFFFCaf\nter", painter.PlainText);
    }

    [Fact]
    public void InfiniteWidth_Centered()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(FontSize: 10)),
            textDirection: TextDirection.Ltr,
            textAlign: TextAlign.Center);

        painter.Layout(minWidth: double.PositiveInfinity);
        Assert.Equal(double.PositiveInfinity, painter.Width);
        Assert.Equal(0, PaintedParagraphs(painter));

        painter.Layout();
        Assert.Equal(10.0, painter.Width);
        Assert.Equal(1, PaintedParagraphs(painter));
        Assert.Equal(0.0, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].Left);

        painter.Layout(minWidth: 100);
        Assert.Equal(100.0, painter.Width);
        Assert.Equal(45.0, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].Left);
    }

    [Fact]
    public void InfiniteWidth_LtrJustified()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(FontSize: 10)),
            textDirection: TextDirection.Ltr,
            textAlign: TextAlign.Justify);

        painter.Layout(minWidth: double.PositiveInfinity);
        Assert.Equal(double.PositiveInfinity, painter.Width);
        Assert.Equal(0.0, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].Left);

        painter.Layout();
        Assert.Equal(10.0, painter.Width);
        Assert.Equal(0.0, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].Left);

        painter.Layout(minWidth: 100);
        Assert.Equal(100.0, painter.Width);
        Assert.Equal(0.0, painter.GetBoxesForSelection(new TextSelection(0, 1))[0].Left);
    }

    [Fact]
    public void LongestLine_RelaysOutWhenMaxWidthChanges()
    {
        using var painter = new TextPainter(
            text: new TextSpan(new string('A', 100), style: new TextStyle(FontSize: 10)),
            textDirection: TextDirection.Ltr,
            textAlign: TextAlign.Justify,
            textWidthBasis: TextWidthBasis.LongestLine);

        painter.Layout(maxWidth: 1000);
        Assert.Equal(1000.0, painter.Width);
        painter.Layout(maxWidth: 100);
        Assert.Equal(100.0, painter.Width);
        painter.Layout(maxWidth: 1000);
        Assert.Equal(1000.0, painter.Width);
    }

    [Fact]
    public void LineBreaking_DoesNotRoundToIntegers()
    {
        using var painter = new TextPainter(
            text: new TextSpan("12345", style: new TextStyle(FontSize: 1.25)),
            textDirection: TextDirection.Ltr);
        painter.Layout(maxWidth: 6.25);
        Assert.Equal(6.25, painter.MaxIntrinsicWidth);
        Assert.Single(painter.ComputeLineMetrics());
        Assert.Equal(6.25, painter.Width);
    }

    [Fact]
    public void Strut_AppliesWhenTheSpanHasNoStyle()
    {
        using var painter = new TextPainter(
            text: new TextSpan(),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(Height: 10, FontSize: 10));
        painter.Layout();
        Assert.Equal(100.0, painter.Height);
    }

    [Fact]
    public void Strut_LeadingIsAFontSizeMultiplier()
    {
        using var painter = new TextPainter(
            text: new TextSpan(),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(Height: 10, FontSize: 10, Leading: 2));
        painter.Layout();
        Assert.Equal(120.0, painter.Height);
        Assert.Equal(10 + (10 * 7.5), painter.ComputeDistanceToActualBaseline(TextBaseline.Alphabetic));
    }

    [Theory]
    [InlineData(false, 60.0, 80.0)]
    [InlineData(true, 37.5, 57.5)]
    public void Strut_ForceStrutHeightAndHalfLeading(bool even, double top, double bottom)
    {
        using var painter = new TextPainter(
            text: new TextSpan("A", style: new TextStyle(FontSize: 20)),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(
                Height: 10,
                FontSize: 10,
                ForceStrutHeight: true,
                LeadingDistribution: even ? TextLeadingDistribution.Even : null));
        painter.Layout();
        Assert.Equal(100.0, painter.Height);
        Assert.Equal(
            [TextBox.FromLTRBD(0, top, 20, bottom, TextDirection.Ltr)],
            painter.GetBoxesForSelection(new TextSelection(0, 1)));
    }

    [Fact]
    public void Strut_ForceStrutHeightAppliesToWidgetSpans()
    {
        using var painter = new TextPainter(
            text: new WidgetSpan(new SizedBox()),
            textDirection: TextDirection.Ltr,
            strutStyle: new StrutStyle(Height: 10, FontSize: 10, ForceStrutHeight: true));
        painter.SetPlaceholderDimensions(
            [new PlaceholderDimensions(new Size(1000, 1000), PlaceholderAlignment.Bottom)]);
        painter.Layout();
        Assert.Equal(100.0, painter.Height);
    }

    [Fact]
    public void GetOffsetForCaret_DoesNotCrashOnDecomposedCharacters()
    {
        using var painter = new TextPainter(
            text: new TextSpan("\u1100\u1161\u11A8", style: new TextStyle(FontSize: 10)),
            textDirection: TextDirection.Ltr);
        painter.Layout(maxWidth: 1);
        painter.GetOffsetForCaret(new TextPosition(0), default);
    }

    [Fact]
    public void TextHeightNone_UnsetsTheHeightMultiplier()
    {
        using var painter = new TextPainter(
            text: new TextSpan(
                style: new TextStyle(FontSize: 10, Height: 1000),
                children: [new TextSpan("A", style: new TextStyle(Height: TextDefaults.TextHeightNone))]),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(10.0, painter.Height);
    }

    [Fact]
    public void DebugPaintTextLayoutBoxes_DrawsOneRectPerRunBeforeTheParagraph()
    {
        using var painter = new TextPainter(
            text: new TextSpan(
                "M",
                style: new TextStyle(FontSize: 128),
                children: [new TextSpan("M", style: new TextStyle(FontSize: 64))]),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        IReadOnlyList<TextBox> boxes = painter.GetBoxesForSelection(new TextSelection(0, 2));
        Assert.Equal(new Rect(0, 0, 128, 128), boxes[0].ToRect());
        Assert.Equal(new Rect(128, 48, 64, 64), boxes[1].ToRect());

        painter.DebugPaintTextLayoutBoxes = true;
        Assert.Equal(3, PaintedDrawCommands(painter));
        painter.DebugPaintTextLayoutBoxes = false;
        Assert.Equal(1, PaintedDrawCommands(painter));
    }

    [Fact]
    public void SurrogateHelpers_AndOffsetsAroundSurrogatePairs()
    {
        Assert.True(TextPainter.IsHighSurrogate(0xD83D));
        Assert.False(TextPainter.IsHighSurrogate(0xDE00));
        Assert.True(TextPainter.IsLowSurrogate(0xDE00));
        Assert.False(TextPainter.IsLowSurrogate(0x0041));

        using var painter = new TextPainter(text: new TextSpan("a\U0001F600b"), textDirection: TextDirection.Ltr);
        Assert.Equal(1, painter.GetOffsetAfter(0));
        Assert.Equal(3, painter.GetOffsetAfter(1));
        Assert.Equal(1, painter.GetOffsetBefore(3));
        Assert.Null(painter.GetOffsetAfter(4));
        Assert.Null(painter.GetOffsetBefore(0));
    }

    [Fact]
    public void WordBoundaries_MoveByWordBoundarySkipsSpacesAndPunctuation()
    {
        using var painter = new TextPainter(text: new TextSpan("how are, you"), textDirection: TextDirection.Ltr);
        painter.Layout();
        TextBoundary boundary = painter.WordBoundaries.MoveByWordBoundary;
        Assert.Equal(3, boundary.GetTrailingTextBoundaryAt(0));
        Assert.Equal(7, boundary.GetTrailingTextBoundaryAt(3));
        Assert.Equal(12, boundary.GetTrailingTextBoundaryAt(7));
        Assert.Equal(9, boundary.GetLeadingTextBoundaryAt(11));
        Assert.Equal(4, boundary.GetLeadingTextBoundaryAt(8));
        Assert.Null(boundary.GetLeadingTextBoundaryAt(-1));
    }

    [Fact]
    public void Dispose_DispatchesMemoryEvents()
    {
        var events = new List<ObjectEvent>();
        void Listener(ObjectEvent @event)
        {
            if (@event.Object is TextPainter)
            {
                lock (events)
                {
                    events.Add(@event);
                }
            }
        }

        FlutterMemoryAllocations.Instance.AddListener(Listener);
        TextPainter painter;
        try
        {
            painter = new TextPainter();
            painter.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(Listener);
        }

        lock (events)
        {
            Assert.Contains(
                events,
                @event => @event is ObjectCreated { ClassName: "TextPainter", Library: "package:flutter/painting.dart" }
                          && ReferenceEquals(@event.Object, painter));
            Assert.Contains(
                events,
                @event => @event is ObjectDisposed && ReferenceEquals(@event.Object, painter));
        }
    }

    // -- text_painter_rtl_test.dart --------------------------------------------------------

    [Fact]
    public void Rtl_BasicWords()
    {
        using var painter = new TextPainter(
            text: new TextSpan("ABC DEF\nGHI", style: new TextStyle(FontSize: 10.0)),
            textDirection: TextDirection.Ltr);
        painter.Layout();
        Assert.Equal(new TextRange(0, 3), painter.GetWordBoundary(new TextPosition(1)));
        Assert.Equal(new TextRange(4, 7), painter.GetWordBoundary(new TextPosition(5)));
        Assert.Equal(new TextRange(8, 11), painter.GetWordBoundary(new TextPosition(9)));
    }

    [Fact]
    public void Rtl_ForcedLineWrappingWithBidi()
    {
        using var painter = new TextPainter(
            text: new TextSpan("A\u05D0", style: new TextStyle(FontSize: 10.0)),
            textDirection: TextDirection.Ltr);
        painter.Layout(maxWidth: 10.0);

        Assert.Equal(new TextRange(0, 2), painter.GetWordBoundary(new TextPosition(0)));
        Assert.Equal(new Point(0, 0), painter.GetOffsetForCaret(new TextPosition(0, TextAffinity.Upstream), default));
        Assert.Equal(new Point(0, 0), painter.GetOffsetForCaret(new TextPosition(0), default));
        Assert.Equal(new Point(10, 0), painter.GetOffsetForCaret(new TextPosition(1, TextAffinity.Upstream), default));
        Assert.Equal(new Point(10, 10), painter.GetOffsetForCaret(new TextPosition(1), default));
        Assert.Equal(new Point(0, 10), painter.GetOffsetForCaret(new TextPosition(2, TextAffinity.Upstream), default));
        Assert.Equal(new Point(10, 10), painter.GetOffsetForCaret(new TextPosition(2), default));

        Assert.Equal(
            [
                TextBox.FromLTRBD(0, 0, 10, 10, TextDirection.Ltr),
                TextBox.FromLTRBD(0, 10, 10, 20, TextDirection.Rtl),
            ],
            painter.GetBoxesForSelection(new TextSelection(0, 2)));
        Assert.Equal(
            [TextBox.FromLTRBD(0, 0, 10, 10, TextDirection.Ltr)],
            painter.GetBoxesForSelection(new TextSelection(0, 1)));
        Assert.Equal(
            [TextBox.FromLTRBD(0, 10, 10, 20, TextDirection.Rtl)],
            painter.GetBoxesForSelection(new TextSelection(1, 2)));
    }

    [Fact]
    public void Rtl_EmptyTextBaseline()
    {
        using var painter = new TextPainter(
            text: new TextSpan("", style: new TextStyle(FontSize: 100.0, Height: 1.0)),
            textDirection: TextDirection.Rtl);
        painter.Layout();
        Assert.Equal(75.0, painter.ComputeDistanceToActualBaseline(TextBaseline.Alphabetic));
    }

    // -- helpers ---------------------------------------------------------------------------

    private static int PaintedParagraphs(TextPainter painter)
    {
        var recorder = new PictureRecorder();
        painter.Paint(new Canvas(recorder), default);
        return recorder.EndRecording().DrawCommandCount;
    }

    private static int PaintedDrawCommands(TextPainter painter) => PaintedParagraphs(painter);

    private static double[] CaretOffsetsForTextSpan(TextDirection textDirection, InlineSpan span)
    {
        using var painter = new TextPainter(text: span, textDirection: textDirection);
        painter.Layout();
        int length = span.ToPlainText().Length;
        double[] result = new double[length + 1];
        for (int offset = 0; offset <= length; offset++)
        {
            result[offset] = painter.GetOffsetForCaret(new TextPosition(offset), default).X;
        }

        Assert.Equal(painter.Width, textDirection == TextDirection.Ltr ? result[length] : result[0]);
        return result;
    }

    private static void CheckCaretOffsetsLtr(string text)
    {
        var graphemes = new List<string>();
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            graphemes.Add((string)enumerator.Current);
        }

        CheckCaretOffsetsLtrFromPieces(graphemes);
    }

    private static void CheckCaretOffsetsLtrFromPieces(IReadOnlyList<string> pieces)
    {
        string text = string.Concat(pieces);
        using var painter = new TextPainter(textDirection: TextDirection.Ltr);

        // The expected caret offset at each piece boundary is the width of the text before it.
        var expectedOffsets = new List<double> { 0.0 };
        for (int index = 0; index < pieces.Count; index++)
        {
            painter.Text = new TextSpan(string.Concat(pieces.Take(index + 1)));
            painter.Layout();
            expectedOffsets.Add(painter.Width);
        }

        painter.Text = new TextSpan(text);
        painter.Layout();
        int codeUnit = 0;
        for (int index = 0; index < pieces.Count; index++)
        {
            Assert.Equal(expectedOffsets[index], painter.GetOffsetForCaret(new TextPosition(codeUnit), default).X);
            codeUnit += pieces[index].Length;
        }

        Assert.Equal(expectedOffsets[^1], painter.GetOffsetForCaret(new TextPosition(codeUnit), default).X);

        double previous = double.NegativeInfinity;
        for (int offset = 0; offset <= text.Length; offset++)
        {
            double caret = painter.GetOffsetForCaret(new TextPosition(offset), default).X;
            Assert.True(caret >= previous, $"The caret at {offset} ({caret}) is left of {previous}.");
            previous = caret;
        }
    }
}
