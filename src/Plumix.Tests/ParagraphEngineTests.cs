using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextDecoration = Plumix.UI.TextDecoration;
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

    [Theory]
    [InlineData(true, 95.0)]
    [InlineData(false, 85.0)]
    public void LineMetrics_KeepNativeRunMetricsSeparateFromStrut(bool force, double reportedBaseline)
    {
        Paragraph paragraph = Build("A", new ParagraphStyle(FontSize: 20,
            StrutStyle: new ParagraphStrutStyle(FontSize: 10, Height: 10, Leading: 2, ForceStrutHeight: force)));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(120, paragraph.Height);
        Assert.Equal(85, paragraph.AlphabeticBaseline);
        Assert.Equal(120, paragraph.IdeographicBaseline);
        LineMetrics metrics = paragraph.GetLineMetricsAt(0)!.Value;
        Assert.Equal(15, metrics.Ascent);
        Assert.Equal(5, metrics.Descent);
        Assert.Equal(15, metrics.UnscaledAscent);
        Assert.Equal(20, metrics.Height);
        Assert.Equal(reportedBaseline, metrics.Baseline);
    }

    [Fact]
    public void EmptyParagraph_UsesCurrentPushedStyleForMetrics()
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        builder.PushStyle(new ParagraphTextStyle(FontSize: 40));
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(40, paragraph.Height);
        Assert.Equal(0, paragraph.NumberOfLines);
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

    [DebugOnlyFact]
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
            Color: new Color(0xFF00FF00),
            FontWeight: FontWeight.ExtraBold,
            FontSize: 10.0,
            Height: 100.0,
            LeadingDistribution: TextLeadingDistribution.Even);
        Assert.Equal(
            new ParagraphTextStyle(
                Color: new Color(0xFF00FF00),
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

    // C#-only: flutter_tester's SkParagraph placeholder boxes under a 1.43 line height (FlutterTest, 16 px,
    // 40x20 placeholder with baseline offset 15). Top/bottom/middle align against the run's unscaled font
    // metrics, and every box hangs from the line's rounded baseline.
    [Theory]
    [InlineData(PlaceholderAlignment.Baseline, 23.0, 2.28)]
    [InlineData(PlaceholderAlignment.AboveBaseline, 26.0, 0.28)]
    [InlineData(PlaceholderAlignment.BelowBaseline, 37.0, 17.0)]
    [InlineData(PlaceholderAlignment.Top, 25.0, 5.0)]
    [InlineData(PlaceholderAlignment.Bottom, 23.0, 1.28)]
    [InlineData(PlaceholderAlignment.Middle, 23.0, 3.0)]
    public void Placeholder_WithLineHeight_AlignsLikeSkParagraph(
        PlaceholderAlignment alignment,
        double height,
        double top)
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        builder.PushStyle(new ParagraphTextStyle(FontSize: 16, Height: 1.43));
        builder.AddText("Inline ");
        builder.AddPlaceholder(40, 20, alignment, baselineOffset: 15, baseline: TextBaseline.Alphabetic);
        builder.AddText(" widgets");
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(720));

        Assert.Equal(height, paragraph.Height, 6);
        Assert.Equal(top, paragraph.GetBoxesForPlaceholders()[0].Top, 6);
    }

    // C#-only: flutter_tester pixels (FlutterTest, captured with `matchesGoldenFile`). SkParagraph's
    // OneLineShaper emboldens a regular typeface for w600+ requests, so the 40 px glyph at x = 10.3 bleeds
    // into column 9 only when bold. flutter_tester and macOS Skia paint that edge pixel 207; Linux Skia's
    // anti-aliasing paints it 188, so the bleed is asserted, not the exact coverage.
    [Fact]
    public void Paint_SemiboldAndHeavier_FakeBoldsTheGlyph()
    {
        Assert.Equal(255, PaintedPixel("X", new ParagraphTextStyle(FontSize: 40), new Point(10.3, 0.3), 9, 20).R);
        Assert.InRange(PaintedPixel(
            "X",
            new ParagraphTextStyle(FontSize: 40, FontWeight: FontWeight.SemiBold),
            new Point(10.3, 0.3),
            9,
            20).R, (byte)160, (byte)230);
    }

    // C#-only: flutter_tester pixels. TextLine::paintBackground fills the run's CSS-height clip, which a
    // 1.43 line height extends above and below the glyph box.
    [Fact]
    public void Paint_Background_FillsTheRunClip()
    {
        var style = new ParagraphTextStyle(
            FontSize: 16,
            Height: 1.43,
            Color: new Color(0xFF000000),
            Background: new Paint { Color = new Color(0xFFFFE8A3) });

        Assert.Equal((255, 232, 163, 255), PaintedPixel("XX", style, default, 10, 1));
        Assert.Equal((255, 232, 163, 255), PaintedPixel("XX", style, default, 10, 21));
        Assert.Equal((0, 0, 0, 255), PaintedPixel("XX", style, default, 10, 10));
    }

    // C#-only: flutter_tester pixels. TextLine::paintShadow draws the glyphs offset in the shadow color
    // before the text.
    [Fact]
    public void Paint_Shadow_DrawsOffsetGlyphsBelowTheText()
    {
        var style = new ParagraphTextStyle(
            FontSize: 16,
            Color: new Color(0xFF000000),
            Shadows: [new Rendering.Shadow(new Color(0xFF0000FF), new Point(3, 3))]);

        Assert.Equal((0, 0, 255, 255), PaintedPixel("XX", style, default, 20, 18));
        Assert.Equal((0, 0, 0, 255), PaintedPixel("XX", style, default, 20, 10));
    }

    // C#-only: Decorations::paint draws after the glyphs and across the run's whole clip, spaces included;
    // FlutterTest's thin underline metrics tint one or two rows.
    [Theory]
    [InlineData(TextDecoration.Underline)]
    [InlineData(TextDecoration.Overline)]
    [InlineData(TextDecoration.LineThrough)]
    public void Paint_Decoration_SpansTheSpaceBetweenGlyphs(TextDecoration decoration)
    {
        var style = new ParagraphTextStyle(
            FontSize: 20,
            Color: new Color(0xFF000000),
            Decoration: decoration,
            DecorationColor: new Color(0xFFFF0000));
        int redRows = Enumerable.Range(0, 40)
            .Count(y => PaintedPixel("X X", style, new Point(0, 10), 30, y) is { R: > 200, G: < 230 });

        Assert.InRange(redRows, 1, 3);
    }

    private static (byte R, byte G, byte B, byte A) PaintedPixel(
        string text,
        ParagraphTextStyle style,
        Point offset,
        int x,
        int y)
    {
        var builder = new ParagraphBuilder(new ParagraphStyle());
        builder.PushStyle(style);
        builder.AddText(text);
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(400));
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.DrawRect(new Rect(0, 0, 80, 60), new Paint { Color = new Color(0xFFFFFFFF) });
        canvas.DrawParagraph(paragraph, offset);
        using Avalonia.Media.Imaging.Bitmap image = recorder.EndRecording().ToImageSync(80, 60);
        return RasterBackend.PixelAt(image, x, y);
    }

    // C#-only: the expected numbers are flutter_tester's (FlutterTest font, dart:ui ParagraphBuilder with
    // `TextStyle(fontSize: 14, height: 1.43, leadingDistribution: even).getParagraphStyle()` and its
    // `getTextStyle()` pushed), the M3 bodyMedium a `Text.rich` of `WidgetSpan`s lays out with.
    private static ParagraphBuilder BodyMediumBuilder(double letterSpacing = 0.0)
    {
        var builder = new ParagraphBuilder(new ParagraphStyle(
            FontSize: 14,
            Height: 1.43,
            TextHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even)));
        builder.PushStyle(new ParagraphTextStyle(
            FontSize: 14,
            Height: 1.43,
            LeadingDistribution: TextLeadingDistribution.Even,
            LetterSpacing: letterSpacing));
        return builder;
    }

    // A line holding only placeholders and a hard break sizes from the paragraph style's empty metrics
    // (height applied proportionally: 15.015 / 5.005) and the placeholders, not from the newline's run:
    // 22 px lines where the newline's even-leading metrics would give 23.
    [Fact]
    public void PlaceholdersSeparatedByHardBreaks_LineHeightsSkipTheNewlineRun()
    {
        ParagraphBuilder builder = BodyMediumBuilder();
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        builder.AddText("\n");
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        builder.AddText("\n");
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(744));

        Assert.Equal(66.0, paragraph.Height, 6);
        Assert.Equal(16.5, paragraph.AlphabeticBaseline, 3);
        Assert.Equal(
            [0.495, 22.495, 44.495],
            paragraph.GetBoxesForPlaceholders().Select(box => Math.Round(box.Top, 3)));
        IReadOnlyList<LineMetrics> lines = paragraph.ComputeLineMetrics();
        Assert.Equal(3, lines.Count);
        // LineMetrics report every run of the line (TextLine::fMaxRunMetrics), the newline's included.
        Assert.Equal(16.5, lines[0].Ascent, 3);
        Assert.Equal(6.51, lines[0].Descent, 3);
        Assert.Equal(23.0, lines[0].Height, 6);
        Assert.Equal(16.995, lines[0].Baseline, 3);
        Assert.Equal(3.5, lines[2].Descent, 3);
        Assert.Equal(20.0, lines[2].Height, 6);
        Assert.Equal(60.995, lines[2].Baseline, 3);
    }

    [Fact]
    public void PlaceholderOnlyLine_StartsFromTheParagraphStylesEmptyMetrics()
    {
        ParagraphBuilder builder = BodyMediumBuilder();
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        using Paragraph single = builder.Build();
        single.Layout(new ParagraphConstraints(744));

        Assert.Equal(22.0, single.Height, 6);
        LineMetrics line = single.ComputeLineMetrics()[0];
        Assert.Equal(16.5, line.Ascent, 3);
        Assert.Equal(3.5, line.Descent, 3);
        Assert.Equal(16.995, line.Baseline, 3);

        // The paragraph style (20 px, height 2) rather than the pushed text style sets the empty metrics.
        var mixed = new ParagraphBuilder(new ParagraphStyle(FontSize: 20, Height: 2));
        mixed.PushStyle(new ParagraphTextStyle(
            FontSize: 14,
            Height: 1.43,
            LeadingDistribution: TextLeadingDistribution.Even));
        mixed.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        using Paragraph paragraph = mixed.Build();
        paragraph.Layout(new ParagraphConstraints(744));

        Assert.Equal(40.0, paragraph.Height, 6);
        Assert.Equal(30.0, paragraph.ComputeLineMetrics()[0].Baseline, 3);
        Assert.Equal(13.5, paragraph.GetBoxesForPlaceholders()[0].Top, 3);
    }

    // TextWrapper keeps `fEndLine` for the line after a trailing hard break, so it repeats the previous
    // line's metrics instead of taking the newline's style.
    [Fact]
    public void LineAfterTrailingHardBreak_RepeatsThePreviousLinesMetrics()
    {
        ParagraphBuilder builder = BodyMediumBuilder();
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        builder.AddText("\n");
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(744));

        Assert.Equal(44.0, paragraph.Height, 6);
        LineMetrics trailing = paragraph.ComputeLineMetrics()[1];
        Assert.Equal(16.5, trailing.Ascent, 3);
        Assert.Equal(5.005, trailing.Descent, 3);
        Assert.Equal(22.0, trailing.Height, 6);
        Assert.Equal(38.995, trailing.Baseline, 3);
    }

    [Fact]
    public void HardBreakInALargerStyle_DoesNotGrowTheLineItEnds()
    {
        ParagraphBuilder builder = BodyMediumBuilder();
        builder.AddText("X");
        builder.PushStyle(new ParagraphTextStyle(FontSize: 30));
        builder.AddText("\n");
        builder.Pop();
        builder.AddText("Y");
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(744));

        Assert.Equal(40.0, paragraph.Height, 6);
        LineMetrics first = paragraph.ComputeLineMetrics()[0];
        Assert.Equal(28.95, first.Ascent, 3);
        Assert.Equal(13.95, first.Descent, 3);
        Assert.Equal(43.0, first.Height, 6);
        Assert.Equal(13.49, first.Baseline, 3);
    }

    // `TextLine`'s half-letter-spacing shift comes from the first non-placeholder cluster, here the
    // newline; the last line holds only a placeholder and is not shifted.
    [Fact]
    public void LetterSpacedPlaceholderLines_ShiftByTheNewlinesHalfSpacing()
    {
        ParagraphBuilder builder = BodyMediumBuilder(letterSpacing: 0.25);
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        builder.AddText("\n");
        builder.AddPlaceholder(99.75, 20, PlaceholderAlignment.Bottom);
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(744));

        Assert.Equal([0.125, 0.0], paragraph.GetBoxesForPlaceholders().Select(box => box.Left));
    }

    // FlutterTest has no Thai, Lao or Khmer glyphs: every code point shapes to a one-em `.notdef`, the
    // Khmer coeng and vowel signs included, and letter spacing applies per glyph (52 × 14.25).
    [Fact]
    public void ComplexScriptLine_KeepsEveryGlyphAdvanceAndSpacing()
    {
        ParagraphBuilder builder = BodyMediumBuilder(letterSpacing: 0.25);
        builder.AddText(
            "ภาษาไทยภาษาไทย · "
            + "ພາສາລາວ · "
            + "ភាសាខ្មែរ · "
            + "中文测试 · 日本語の文章");
        using Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(800));

        Assert.Equal(741.0, paragraph.MaxIntrinsicWidth, 6);
        Assert.Equal(20.0, paragraph.Height, 6);
    }

    // HarfBuzz with FlutterTest: a mark composes when the font maps the composition (Ç, Å), the default,
    // Arabic and Hebrew shapers zero other non-spacing marks unless no base precedes them, the Indic,
    // Thai and Khmer shapers keep them, spacing and enclosing marks are full glyphs, ZWJ is a hidden
    // glyph, and SARA AM splits in two. Widths at 10 px without and with 1 px letter spacing.
    [Theory]
    [InlineData("q́", 10.0, 12.0)]
    [InlineData("Ç", 10.0, 11.0)]
    [InlineData("Å", 10.0, 11.0)]
    [InlineData("Ḉ", 10.0, 12.0)]
    [InlineData("́", 10.0, 11.0)]
    [InlineData("́́", 20.0, 22.0)]
    [InlineData(" ́", 10.0, 12.0)]
    [InlineData("שָ", 10.0, 12.0)]
    [InlineData("يَّ", 10.0, 10.0)]
    [InlineData("क्ष", 30.0, 33.0)]
    [InlineData("ก́", 20.0, 22.0)]
    [InlineData("ខ្", 20.0, 22.0)]
    [InlineData("ำ", 20.0, 22.0)]
    [InlineData("a⃝", 20.0, 22.0)]
    [InlineData("aः", 20.0, 22.0)]
    [InlineData("a‍b", 20.0, 23.0)]
    [InlineData("឴", 0.0, 1.0)]
    public void CombiningMarks_ShapeLikeHarfBuzzWithFlutterTest(string text, double width, double spacedWidth)
    {
        foreach ((double letterSpacing, double expected) in new[] { (0.0, width), (1.0, spacedWidth) })
        {
            var builder = new ParagraphBuilder(new ParagraphStyle());
            builder.PushStyle(new ParagraphTextStyle(FontSize: 10, LetterSpacing: letterSpacing));
            builder.AddText(text);
            using Paragraph paragraph = builder.Build();
            paragraph.Layout(new ParagraphConstraints(2000));

            Assert.Equal(expected, paragraph.MaxIntrinsicWidth, 6);
        }
    }

    private static Paragraph Build(string text, ParagraphStyle style)
    {
        var builder = new ParagraphBuilder(style);
        builder.AddText(text);
        return builder.Build();
    }
}
