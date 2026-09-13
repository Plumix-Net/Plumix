using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextStyle = Plumix.Widgets.TextStyle;

namespace Plumix.Tests;

// Dart parity source (reference): flutter/engine/src/flutter/testing/dart/paragraph_test.dart,
// paragraph_builder_test.dart and text_test.dart, plus the paragraph-style cases of
// flutter/packages/flutter/test/painting/text_style_test.dart
//
// The headless paragraph engine reproduces Flutter's `FlutterTest` font metrics, so the engine's own
// FlutterTest expectations apply verbatim.

public sealed class ParagraphEngineTests
{
    [Theory]
    [InlineData(10.0)]
    [InlineData(20.0)]
    [InlineData(30.0)]
    [InlineData(40.0)]
    public void SingleLineParagraph_LaysOutPredictably(double fontSize)
    {
        Paragraph paragraph = Build("Test", new ParagraphStyle(FontSize: fontSize));
        paragraph.Layout(new ParagraphConstraints(400.0));

        Assert.Equal(fontSize, paragraph.Height);
        Assert.Equal(400.0, paragraph.Width);
        Assert.Equal(fontSize * 4.0, paragraph.MinIntrinsicWidth);
        Assert.Equal(fontSize * 4.0, paragraph.MaxIntrinsicWidth);
        Assert.Equal(fontSize * 0.75, paragraph.AlphabeticBaseline);
        Assert.Equal(fontSize, paragraph.IdeographicBaseline);
    }

    [Fact]
    public void MultiLineParagraph_LaysOutPredictably()
    {
        const double fontSize = 10.0;
        Paragraph paragraph = Build("Test Ahem", new ParagraphStyle(FontSize: fontSize));
        paragraph.Layout(new ParagraphConstraints(fontSize * 5.0));

        Assert.Equal(fontSize * 2.0, paragraph.Height);
        Assert.Equal(fontSize * 5.0, paragraph.Width);
        Assert.Equal(fontSize * 4.0, paragraph.MinIntrinsicWidth);
        Assert.Equal(fontSize * 9.0, paragraph.MaxIntrinsicWidth);
        Assert.Equal(fontSize * 0.75, paragraph.AlphabeticBaseline);
        Assert.Equal(fontSize, paragraph.IdeographicBaseline);
    }

    [Fact]
    public void GetLineBoundary()
    {
        Paragraph paragraph = Build("Test Ahem", new ParagraphStyle(FontSize: 10.0));
        paragraph.Layout(new ParagraphConstraints(50.0));

        Assert.Equal(new TextRange(5, 9), paragraph.GetLineBoundary(new TextPosition(5)));
        Assert.Equal(new TextRange(0, 5), paragraph.GetLineBoundary(new TextPosition(5, TextAffinity.Upstream)));
        Assert.Equal(new TextRange(0, 5), paragraph.GetLineBoundary(new TextPosition(0)));
        Assert.Equal(new TextRange(5, 9), paragraph.GetLineBoundary(new TextPosition(9)));
    }

    [Fact]
    public void GetLineBoundary_Rtl()
    {
        Paragraph paragraph = Build(
            "\u0627\u0644\u0642\u0627\u0647\u0631\u0629\u0627\u0644\u0642\u0627\u0647\u0631\u0629",
            new ParagraphStyle(FontSize: 10.0, TextDirection: TextDirection.Rtl));
        paragraph.Layout(new ParagraphConstraints(50.0));

        Assert.Equal(3, paragraph.NumberOfLines);
        Assert.Equal(new TextRange(5, 10), paragraph.GetLineBoundary(new TextPosition(5)));
        Assert.Equal(new TextRange(0, 5), paragraph.GetLineBoundary(new TextPosition(5, TextAffinity.Upstream)));
        Assert.Equal(new TextRange(0, 5), paragraph.GetLineBoundary(new TextPosition(0)));
        Assert.Equal(new TextRange(5, 10), paragraph.GetLineBoundary(new TextPosition(9)));
    }

    [Fact]
    public void GetLineBoundary_EmptyLine()
    {
        Paragraph paragraph = Build("Test\n\nAhem", new ParagraphStyle(FontSize: 10.0));
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));

        Assert.Equal(new TextRange(5, 5), paragraph.GetLineBoundary(new TextPosition(5)));
        Assert.Equal(new TextRange(5, 5), paragraph.GetLineBoundary(new TextPosition(5, TextAffinity.Upstream)));
        Assert.Equal(new TextRange(0, 4), paragraph.GetLineBoundary(new TextPosition(4)));
        Assert.Equal(new TextRange(6, 10), paragraph.GetLineBoundary(new TextPosition(6)));
    }

    [Fact]
    public void GetLineMetricsAt()
    {
        Paragraph paragraph = Build(
            "Test\npppp",
            new ParagraphStyle(FontSize: 10, TextDirection: TextDirection.Rtl, Height: 2.0));
        paragraph.Layout(new ParagraphConstraints(100.0));

        LineMetrics line = paragraph.GetLineMetricsAt(1)!.Value;
        Assert.True(line.HardBreak);
        Assert.Equal(15.0, line.Ascent);
        Assert.Equal(5.0, line.Descent);
        Assert.Equal(20.0, line.Height);
        Assert.Equal(40.0, line.Width);
        Assert.Equal(60.0, line.Left);
        Assert.Equal(35.0, line.Baseline);
        Assert.Equal(1, line.LineNumber);
    }

    [Fact]
    public void LineNumber_CountsLineFeedsAsTheEndOfTheirLine()
    {
        Paragraph paragraph = Build("Test\n\nTest", new ParagraphStyle(FontSize: 10.0));
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));

        Assert.Equal(3, paragraph.NumberOfLines);
        Assert.Equal(0, paragraph.GetLineNumberAt(4));
        Assert.Equal(1, paragraph.GetLineNumberAt(5));
        Assert.Equal(2, paragraph.GetLineNumberAt(6));
    }

    [Fact]
    public void EmptyParagraph_HasNoGlyphsOrLines()
    {
        Paragraph paragraph = new ParagraphBuilder(new ParagraphStyle()).Build();
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));

        Assert.Null(paragraph.GetClosestGlyphInfoForOffset(default));
        Assert.Null(paragraph.GetGlyphInfoAt(0));
        Assert.Null(paragraph.GetLineMetricsAt(0));
        Assert.Null(paragraph.GetLineNumberAt(0));
        Assert.Equal(0, paragraph.NumberOfLines);
    }

    [Fact]
    public void OutOfBoundsIndices_ReturnNull()
    {
        Paragraph paragraph = Build(
            new string('A', 100),
            new ParagraphStyle(FontSize: 10, MaxLines: 1, Ellipsis: "BBB"));
        paragraph.Layout(new ParagraphConstraints(100));

        Assert.Equal(1, paragraph.NumberOfLines);
        Assert.Null(paragraph.GetLineMetricsAt(-1));
        Assert.Equal(0, paragraph.GetLineMetricsAt(0)!.Value.LineNumber);
        Assert.Null(paragraph.GetLineMetricsAt(1));
        Assert.Null(paragraph.GetLineMetricsAt(7));

        Assert.Null(paragraph.GetLineNumberAt(-1));
        Assert.Equal(0, paragraph.GetLineNumberAt(0));
        Assert.Equal(0, paragraph.GetLineNumberAt(6));

        Assert.Null(paragraph.GetGlyphInfoAt(-1));
        Assert.Equal(new TextRange(0, 1), paragraph.GetGlyphInfoAt(0)!.GraphemeClusterCodeUnitRange);
        Assert.Equal(new TextRange(6, 7), paragraph.GetGlyphInfoAt(6)!.GraphemeClusterCodeUnitRange);
        Assert.Null(paragraph.GetGlyphInfoAt(7));
        Assert.Null(paragraph.GetGlyphInfoAt(200));
        Assert.True(paragraph.DidExceedMaxLines);
    }

    [Fact]
    public void GlyphInfo_Queries()
    {
        Paragraph paragraph = Build("Test\nTest", new ParagraphStyle(FontSize: 10.0));
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));

        GlyphInfo bottomRight = paragraph.GetClosestGlyphInfoForOffset(new Point(99.0, 99.0))!;
        GlyphInfo last = paragraph.GetGlyphInfoAt(8)!;
        Assert.Equal(last, bottomRight);
        Assert.NotEqual(paragraph.GetGlyphInfoAt(0), bottomRight);

        Assert.Equal(new Rect(30, 10, 10, 10), last.GraphemeClusterLayoutBounds);
        Assert.Equal(new TextRange(8, 9), last.GraphemeClusterCodeUnitRange);
        Assert.Equal(TextDirection.Ltr, last.WritingDirection);
    }

    [Fact]
    public void LineBreaking_DoesNotRoundToIntegers()
    {
        Paragraph paragraph = Build("12345", new ParagraphStyle(FontSize: 1.25));
        paragraph.Layout(new ParagraphConstraints(6.25));

        Assert.Equal(6.25, paragraph.MaxIntrinsicWidth);
        IReadOnlyList<LineMetrics> lines = paragraph.ComputeLineMetrics();
        Assert.Single(lines);
        Assert.Equal(6.25, lines[0].Width);
    }

    [Fact]
    public void TextHeightNone_UnsetsTheHeightMultiplier()
    {
        var builder = new ParagraphBuilder(new ParagraphStyle(FontSize: 10, Height: 10));
        builder.PushStyle(new ParagraphTextStyle(Height: TextDefaults.TextHeightNone));
        builder.AddText("A");
        Paragraph pushed = builder.Build();
        pushed.Layout(new ParagraphConstraints(1000));
        Assert.Equal(10.0, pushed.Height);

        Paragraph paragraphStyle = Build("A", new ParagraphStyle(FontSize: 10, Height: TextDefaults.TextHeightNone));
        paragraphStyle.Layout(new ParagraphConstraints(1000));
        Assert.Equal(10.0, paragraphStyle.Height);

        Paragraph strut = Build(
            "A",
            new ParagraphStyle(
                FontSize: 100,
                StrutStyle: new ParagraphStrutStyle(
                    ForceStrutHeight: true,
                    Height: TextDefaults.TextHeightNone,
                    FontSize: 10)));
        strut.Layout(new ParagraphConstraints(1000));
        Assert.Equal(10.0, strut.Height);
    }

    [Fact]
    public void ParagraphBuilder_DefaultStyleBoxesAndLineMetrics()
    {
        Paragraph paragraph = Build("Hello", new ParagraphStyle());
        paragraph.Layout(new ParagraphConstraints(800.0));

        Assert.NotEqual(0.0, paragraph.Width);
        Assert.NotEqual(0.0, paragraph.Height);
        Assert.Equal(
            [TextBox.FromLTRBD(0, 0, 42, 14, TextDirection.Ltr)],
            paragraph.GetBoxesForRange(0, 3));

        IReadOnlyList<LineMetrics> lines = paragraph.ComputeLineMetrics();
        LineMetrics line = Assert.Single(lines);
        Assert.Equal(new LineMetrics(true, 10.5, 3.5, 10.5, 14, 70, 0, 10.5, 0), line);
    }

    [Fact]
    public void ParagraphBuilder_CannotBeUsedAfterBuild()
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.PushStyle(new ParagraphTextStyle()));
    }

    [Fact]
    public void ParagraphBuilder_PlaceholdersScaleAndCount()
    {
        var builder = new ParagraphBuilder(new ParagraphStyle(FontSize: 10));
        builder.AddText("A");
        builder.AddPlaceholder(10, 20, PlaceholderAlignment.Bottom, scale: 2.0);
        Assert.Equal(1, builder.PlaceholderCount);
        Assert.Equal([2.0], builder.PlaceholderScales);
        Assert.Throws<ArgumentException>(() => builder.AddPlaceholder(1, 1, PlaceholderAlignment.Baseline));

        Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(1000));
        Assert.Equal(
            [TextBox.FromLTRBD(10, 0, 30, 40, TextDirection.Ltr)],
            paragraph.GetBoxesForPlaceholders());
    }

    [Fact]
    public void ParagraphBuilder_RejectsMalformedUtf16()
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        Assert.Throws<ArgumentException>(() => builder.AddText("Hello\uD83DWorld"));
    }

    [Fact]
    public void Dispose_TwiceThrowsInDebug()
    {
        Paragraph paragraph = Build("A", new ParagraphStyle());
        Assert.False(paragraph.DebugDisposed);
        paragraph.Dispose();
        Assert.True(paragraph.DebugDisposed);
        Assert.Throws<InvalidOperationException>(() => paragraph.Dispose());
    }

    [Fact]
    public void ParagraphStyle_ToStringMatchesDart()
    {
        Assert.Equal(
            "ParagraphStyle(textAlign: unspecified, textDirection: TextDirection.ltr, fontWeight: unspecified, "
            + "fontStyle: unspecified, maxLines: unspecified, textHeightBehavior: unspecified, "
            + "fontFamily: unspecified, fontSize: 14.0, height: unspecified, strutStyle: unspecified, "
            + "ellipsis: unspecified, locale: unspecified)",
            new ParagraphStyle(TextDirection: TextDirection.Ltr, FontSize: 14.0).ToString());
        Assert.Contains("height: 100.0x", new ParagraphStyle(Height: 100.0).ToString());
        Assert.Contains("height: unspecified", new ParagraphStyle(Height: TextDefaults.TextHeightNone).ToString());
    }

    [Fact]
    public void TextHeightBehavior_DefaultsAndToString()
    {
        var behavior = new TextHeightBehavior();
        Assert.True(behavior.ApplyHeightToFirstAscent);
        Assert.True(behavior.ApplyHeightToLastDescent);
        Assert.Equal(TextLeadingDistribution.Proportional, behavior.LeadingDistribution);
        Assert.Equal(
            "TextHeightBehavior(applyHeightToFirstAscent: true, applyHeightToLastDescent: true, "
            + "leadingDistribution: TextLeadingDistribution.proportional)",
            behavior.ToString());
    }

    [Fact]
    public void GetWordBoundary_UsesTheAffinity()
    {
        Paragraph paragraph = Build("Hello team", new ParagraphStyle(FontSize: 40));
        paragraph.Layout(new ParagraphConstraints(double.PositiveInfinity));
        Assert.Equal(new TextRange(0, 5), paragraph.GetWordBoundary(new TextPosition(5, TextAffinity.Upstream)));
        Assert.Equal(new TextRange(5, 6), paragraph.GetWordBoundary(new TextPosition(5)));
    }

    [Fact]
    public void GlyphInfo_AndTextBox_StoreTheirFields()
    {
        var info = new GlyphInfo(new Rect(1, 2, 3, 4), new TextRange(5, 6), TextDirection.Rtl);
        Assert.Equal(new Rect(1, 2, 3, 4), info.GraphemeClusterLayoutBounds);
        Assert.Equal(new TextRange(5, 6), info.GraphemeClusterCodeUnitRange);
        Assert.Equal(TextDirection.Rtl, info.WritingDirection);

        TextBox box = TextBox.FromLTRBD(1, 2, 3, 4, TextDirection.Rtl);
        Assert.Equal(3.0, box.Start);
        Assert.Equal(1.0, box.End);
        Assert.Equal("TextBox.fromLTRBD(1.0, 2.0, 3.0, 4.0, TextDirection.rtl)", box.ToString());
    }

    // -- text_style_test.dart: getTextStyle / getParagraphStyle ---------------------------------

    [Fact]
    public void TextStyle_GetTextStyleAndGetParagraphStyle()
    {
        var s5 = new TextStyle(FontWeight: FontWeight.Bold, FontSize: 12.0, Height: 123.0);
        Assert.Equal(
            new ParagraphTextStyle(FontWeight: FontWeight.Bold, FontSize: 12.0, Height: 123.0),
            s5.GetTextStyle());
        Assert.Equal(
            new ParagraphStyle(FontWeight: FontWeight.Bold, FontSize: 12.0, Height: 123.0),
            s5.GetParagraphStyle());

        var s2 = new TextStyle(
            Color: Color.FromUInt32(0xFF00FF00),
            FontWeight: FontWeight.ExtraBold,
            FontSize: 10.0,
            Height: 100.0,
            LeadingDistribution: TextLeadingDistribution.Even);
        Assert.Equal(
            new ParagraphTextStyle(
                Color: Color.FromUInt32(0xFF00FF00),
                FontWeight: FontWeight.ExtraBold,
                FontSize: 10.0,
                Height: 100.0,
                LeadingDistribution: TextLeadingDistribution.Even),
            s2.GetTextStyle());
        Assert.Equal(
            new ParagraphStyle(
                TextAlign: TextAlign.Center,
                FontWeight: FontWeight.ExtraBold,
                FontSize: 10.0,
                Height: 100.0,
                TextHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even)),
            s2.GetParagraphStyle(textAlign: TextAlign.Center));
    }

    [Fact]
    public void TextStyle_GetParagraphStyleCarriesTheDirectionAndDefaultFontSize()
    {
        Assert.Equal(
            new ParagraphStyle(TextDirection: TextDirection.Ltr, FontSize: 14.0),
            new TextStyle().GetParagraphStyle(textDirection: TextDirection.Ltr));
        Assert.Equal(
            new ParagraphStyle(TextDirection: TextDirection.Rtl, FontSize: 14.0),
            new TextStyle().GetParagraphStyle(textDirection: TextDirection.Rtl));
    }

    [Fact]
    public void TextStyle_GetParagraphStyleScalesTheFontSizeAndStrut()
    {
        Assert.Equal(
            new TextStyle(FontSize: 25).GetParagraphStyle(),
            new TextStyle(FontSize: 10).GetParagraphStyle(textScaler: TextScaler.Linear(2.5)));

        ParagraphStyle style = new TextStyle().GetParagraphStyle(
            textScaler: TextScaler.Linear(2.0),
            strutStyle: new StrutStyle(FontSize: 10, Height: 2, Leading: 1, ForceStrutHeight: true));
        Assert.Equal(
            new ParagraphStrutStyle(FontSize: 20, Height: 2, Leading: 1, ForceStrutHeight: true),
            style.StrutStyle);
    }

    private static Paragraph Build(string text, ParagraphStyle style)
    {
        var builder = new ParagraphBuilder(style);
        builder.AddText(text);
        return builder.Build();
    }
}
