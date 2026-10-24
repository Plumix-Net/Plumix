using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using TextStyle = Plumix.Widgets.TextStyle;

namespace Plumix.Tests;

// Dart parity source: flutter/engine/src/flutter/testing/dart/paragraph_test.dart and text_test.dart;
// flutter/packages/flutter/test/painting/text_painter_test.dart. These tests explicitly use the HOST
// shaper, not the FlutterTest-metric engine selected by the rest of the test assembly.
public sealed class AvaloniaParagraphDartParityTests
{
    private static readonly FontFamily Ahem = new("avares://Plumix.Tests/Fixtures/Fonts#Ahem");
    private static readonly FontFamily Variable = new("avares://Plumix.Tests/Fixtures/Fonts#Roboto Slab");

    private static AvaloniaParagraph Build(
        string text, ParagraphStyle? paragraphStyle = null, ParagraphTextStyle? style = null,
        IReadOnlyList<StyledRange>? runs = null, IReadOnlyList<ParagraphPlaceholder>? placeholders = null)
    {
        ParagraphStyle paragraph = paragraphStyle ?? new ParagraphStyle(FontSize: 20, FontFamily: Ahem);
        ResolvedTextStyle defaults = ResolvedTextStyle.FromParagraphStyle(paragraph);
        ResolvedTextStyle resolved = style is null ? defaults : defaults.Merge(style);
        return new AvaloniaParagraph(new ParagraphContent(paragraph, text,
            runs ?? [new StyledRange(0, text.Length, resolved)], placeholders ?? [], defaults, resolved));
    }

    [Fact]
    public void NativeFont_IsTheBundledAhemFace()
    {
        Assert.Equal("Ahem", new Typeface(Ahem).GlyphTypeface.FamilyName);
        using AvaloniaParagraph paragraph = Build("Test");
        paragraph.Layout(new ParagraphConstraints(400));
        Assert.Equal(80, paragraph.MaxIntrinsicWidth, 5);
        Assert.Equal(20, paragraph.Height);
        Assert.Equal(16, paragraph.AlphabeticBaseline, 5);
        Assert.Equal(20, paragraph.IdeographicBaseline, 5);
    }

    [Theory]
    [InlineData(20, 20.0, 9.5)]
    [InlineData(0.1, 1.0, -4.5)]
    public void HalfLeading_PreservesTightGlyphHeight(double height, double expectedHeight, double inset)
    {
        double size = height == 20 ? 1 : 10;
        using AvaloniaParagraph paragraph = Build("A",
            new ParagraphStyle(FontFamily: Ahem, FontSize: size, Height: height,
                TextHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even)));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(expectedHeight, paragraph.Height);
        TextBox box = Assert.Single(paragraph.GetBoxesForRange(0, 1));
        Assert.Equal(inset, box.Top, 5);
        Assert.Equal(size, box.Bottom - box.Top, 5);
        Assert.Equal(box.ToRect(), paragraph.GetGlyphInfoAt(0)!.GraphemeClusterLayoutBounds);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(10.0)]
    public void HeightTrim_RestoresNativeFirstAscentAndLastDescent(double height)
    {
        using AvaloniaParagraph paragraph = Build("A", new ParagraphStyle(FontFamily: Ahem, FontSize: 10,
            Height: height, TextHeightBehavior: new TextHeightBehavior(false, false, TextLeadingDistribution.Even)));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(10, paragraph.Height);
        Assert.Equal(8, paragraph.AlphabeticBaseline, 5);
        Assert.Equal(0, Assert.Single(paragraph.GetBoxesForRange(0, 1)).Top, 5);
    }

    [Theory]
    [InlineData(TextLeadingDistribution.Proportional, 80)]
    [InlineData(TextLeadingDistribution.Even, 53)]
    public void ForcedStrut_UsesItsOwnMetricsAndKeepsGlyphBounds(
        TextLeadingDistribution distribution, double baseline)
    {
        using AvaloniaParagraph paragraph = Build("A", new ParagraphStyle(FontFamily: Ahem, FontSize: 20,
            StrutStyle: new ParagraphStrutStyle(FontFamily: Ahem, FontSize: 10, Height: 10,
                LeadingDistribution: distribution, ForceStrutHeight: true)));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(100, paragraph.Height);
        Assert.Equal(baseline, paragraph.AlphabeticBaseline, 5);
        TextBox tight = Assert.Single(paragraph.GetBoxesForRange(0, 1));
        Assert.Equal(baseline - 16, tight.Top, 5);
        Assert.Equal(20, tight.Bottom - tight.Top, 5);
        TextBox strut = Assert.Single(paragraph.GetBoxesForRange(0, 1, BoxHeightStyle.Strut));
        Assert.Equal(0, strut.Top, 5);
        Assert.Equal(100, strut.Bottom, 5);
    }

    [Fact]
    public void ForcedStrut_AppliesToEmptyParagraphAndTallPlaceholders()
    {
        var paragraphStyle = new ParagraphStyle(FontFamily: Ahem, FontSize: 20,
            TextHeightBehavior: new TextHeightBehavior(false, false),
            StrutStyle: new ParagraphStrutStyle(FontFamily: Ahem, FontSize: 10, Height: 10,
                ForceStrutHeight: true));
        using AvaloniaParagraph empty = Build("", paragraphStyle);
        empty.Layout(new ParagraphConstraints(100));
        Assert.Equal(100, empty.Height);
        Assert.Equal(0, empty.NumberOfLines);
        Assert.Null(empty.GetGlyphInfoAt(0));
        ResolvedTextStyle style = ResolvedTextStyle.FromParagraphStyle(paragraphStyle);
        var placeholder = new ParagraphPlaceholder(0, 1000, 1000, PlaceholderAlignment.Baseline,
            1000, TextBaseline.Alphabetic, style);
        using AvaloniaParagraph huge = Build("\uFFFC", paragraphStyle, placeholders: [placeholder]);
        huge.Layout(new ParagraphConstraints(2000));
        Assert.Equal(100, huge.Height);
        Assert.Equal(1000, Assert.Single(huge.GetBoxesForPlaceholders()).ToRect().Height);
    }

    [Fact]
    public void PerRunHeightAndLetterSpacing_AffectWrappingMetricsAndPaintingTogether()
    {
        var paragraphStyle = new ParagraphStyle(FontFamily: Ahem, FontSize: 10);
        ResolvedTextStyle plain = ResolvedTextStyle.FromParagraphStyle(paragraphStyle);
        ResolvedTextStyle tall = plain.Merge(new ParagraphTextStyle(Height: 3, LetterSpacing: 5));
        using AvaloniaParagraph paragraph = Build("AABB", paragraphStyle,
            runs: [new StyledRange(0, 2, plain), new StyledRange(2, 4, tall)]);
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(50, paragraph.MaxIntrinsicWidth, 5);
        Assert.Equal(30, paragraph.Height);
        Assert.Equal(24, paragraph.AlphabeticBaseline, 5);
        paragraph.Layout(new ParagraphConstraints(35));
        Assert.Equal(2, paragraph.NumberOfLines);
        Assert.Equal(30, paragraph.GetLineMetricsAt(1)!.Value.Height);
        Assert.Equal(1, paragraph.GetLineNumberAt(3));
    }

    [Fact]
    public void WordSpacing_IsAppliedBeforeLineBreaking()
    {
        using AvaloniaParagraph paragraph = Build("A A", style: new ParagraphTextStyle(WordSpacing: 10));
        paragraph.Layout(new ParagraphConstraints(200));
        Assert.Equal(70, paragraph.MaxIntrinsicWidth, 5);
    }

    [Fact]
    public void WordSpacing_ReadsTheSourceTextOfARunThatStartsMidParagraph()
    {
        // Avalonia 12.1 hands back slice-relative glyph clusters; spacing must still test the
        // run's own characters, not the paragraph's first ones ("AAAA" has no space to widen).
        var paragraphStyle = new ParagraphStyle(FontFamily: Ahem, FontSize: 10);
        ResolvedTextStyle plain = ResolvedTextStyle.FromParagraphStyle(paragraphStyle);
        ResolvedTextStyle spaced = plain.Merge(new ParagraphTextStyle(WordSpacing: 10));
        using AvaloniaParagraph paragraph = Build("AAAA A A", paragraphStyle,
            runs: [new StyledRange(0, 4, plain), new StyledRange(4, 8, spaced)]);
        paragraph.Layout(new ParagraphConstraints(500));
        Assert.Equal(100, paragraph.MaxIntrinsicWidth, 5);
    }

    [Fact]
    public void WrappedRightToLeftRuns_SplitAtTheirOwnClusters()
    {
        const string word = "\u0645\u0631\u062D\u0628\u0627";
        const string text = "Hello " + word + " " + word + " " + word;
        using AvaloniaParagraph paragraph = Build(text);
        paragraph.Layout(new ParagraphConstraints(paragraph.MaxIntrinsicWidth));
        double width = paragraph.MaxIntrinsicWidth;
        paragraph.Layout(new ParagraphConstraints(width * 0.45));
        Assert.True(paragraph.NumberOfLines > 1);
        for (int index = 6; index < text.Length; index++)
        {
            if (text[index] == ' ')
            {
                continue;
            }

            GlyphInfo glyph = paragraph.GetGlyphInfoAt(index)!;
            Assert.Equal(TextDirection.Rtl, glyph.WritingDirection);
            Assert.True(glyph.GraphemeClusterCodeUnitRange.Start <= index
                        && index < glyph.GraphemeClusterCodeUnitRange.End);
        }

        double covered = paragraph.GetBoxesForRange(0, text.Length).Sum(box => box.Right - box.Left);
        Assert.Equal(width, covered, 0);
    }

    [Theory]
    [InlineData(BoxHeightStyle.IncludeLineSpacingTop)]
    [InlineData(BoxHeightStyle.IncludeLineSpacingMiddle)]
    [InlineData(BoxHeightStyle.IncludeLineSpacingBottom)]
    public void SpacingBoxes_JoinAdjacentLines(BoxHeightStyle heightStyle)
    {
        using AvaloniaParagraph paragraph = Build(
            "A\nB", new ParagraphStyle(FontFamily: Ahem, FontSize: 10, Height: 3));
        paragraph.Layout(new ParagraphConstraints(100));
        TextBox first = paragraph.GetBoxesForRange(0, 1, heightStyle)[0];
        TextBox second = paragraph.GetBoxesForRange(2, 3, heightStyle)[0];
        Assert.Equal(first.Bottom, second.Top, 5);
        TextBox tight = paragraph.GetBoxesForRange(0, 1)[0];
        TextBox max = paragraph.GetBoxesForRange(0, 1, BoxHeightStyle.Max)[0];
        Assert.Equal(10, tight.Bottom - tight.Top, 5);
        Assert.Equal(0, max.Top, 5);
        Assert.Equal(30, max.Bottom, 5);
    }

    [Fact]
    public void ShapedBidiRuns_ExposeTheirOwnDirectionsAndAdvanceBounds()
    {
        using AvaloniaParagraph paragraph = Build("Hello مرحبا");
        paragraph.Layout(new ParagraphConstraints(1000));
        Assert.Equal(TextDirection.Ltr, paragraph.GetGlyphInfoAt(0)!.WritingDirection);
        Assert.Equal(TextDirection.Rtl, paragraph.GetGlyphInfoAt(6)!.WritingDirection);
        Assert.Contains(paragraph.GetBoxesForRange(0, 11), box => box.Direction == TextDirection.Rtl);
    }

    [Fact]
    public void Ellipsis_UsesTheRequestedSymbolAndHidesRemovedGlyphs()
    {
        using AvaloniaParagraph paragraph = Build(new string('A', 100),
            new ParagraphStyle(FontFamily: Ahem, FontSize: 10, MaxLines: 1, Ellipsis: "BBB"));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.True(paragraph.DidExceedMaxLines);
        Assert.NotNull(paragraph.GetGlyphInfoAt(6));
        Assert.Null(paragraph.GetGlyphInfoAt(7));
        Assert.Null(paragraph.GetLineNumberAt(7));
        Assert.Null(paragraph.GetGlyphInfoAt(200));
    }

    [Fact]
    public void VariableAxes_ChangeBothAdvancesAndPixels_AndExplicitWeightWins()
    {
        using AvaloniaParagraph light = VariableParagraph(100);
        using AvaloniaParagraph heavy = VariableParagraph(900);
        using AvaloniaParagraph explicitAxis = VariableParagraph(100, 900);
        light.Layout(new ParagraphConstraints(500));
        heavy.Layout(new ParagraphConstraints(500));
        explicitAxis.Layout(new ParagraphConstraints(500));
        Assert.True(heavy.MaxIntrinsicWidth > light.MaxIntrinsicWidth);
        Assert.Equal(heavy.MaxIntrinsicWidth, explicitAxis.MaxIntrinsicWidth, 5);
        using Bitmap lightImage = Render(light);
        using Bitmap heavyImage = Render(heavy);
        using Bitmap explicitImage = Render(explicitAxis);
        Assert.NotEqual(RasterBackend.ReadPixels(lightImage), RasterBackend.ReadPixels(heavyImage));
        Assert.Equal(RasterBackend.ReadPixels(heavyImage), RasterBackend.ReadPixels(explicitImage));
    }

    [Fact]
    public void VariableInstances_ShareOneFacePerTypeface_AndOutliveEachOther()
    {
        // A face copies every table it reads; macOS's variable system font carries megabytes of them,
        // so a face per paragraph turned each laid-out paragraph into a multi-megabyte copy.
        GlyphTypeface typeface = new Typeface(Variable).GlyphTypeface;
        ResolvedTextStyle style = ResolvedTextStyle.FromParagraphStyle(
            new ParagraphStyle(FontFamily: Variable, FontSize: 40, FontWeight: (FontWeight)900));
        Assert.True(VariableFont.IsVariable(typeface));
        var first = new VariableFont(typeface, style);
        using var second = new VariableFont(typeface, style);
        Assert.Same(first.FaceForTests, second.FaceForTests);

        double before = VariableParagraphWidth();
        first.Dispose();
        Assert.Equal(before, VariableParagraphWidth(), 5);

        static double VariableParagraphWidth()
        {
            using AvaloniaParagraph paragraph = VariableParagraph(900);
            paragraph.Layout(new ParagraphConstraints(500));
            return paragraph.MaxIntrinsicWidth;
        }
    }

    private static AvaloniaParagraph VariableParagraph(int weight, double? axis = null) =>
        Build("Hello", new ParagraphStyle(FontFamily: Variable, FontSize: 40, FontWeight: (FontWeight)weight),
            new ParagraphTextStyle(FontVariations: axis is double value ? [new FontVariation("wght", value)] : null));

    [Fact]
    public void Shadows_PaintUnderGlyphsAtTheirOffset()
    {
        using AvaloniaParagraph paragraph = Build("A", style: new ParagraphTextStyle(Color: new Color(0xFFFF0000),
            Shadows: [new Shadow(new Color(0xFF0000FF), new Point(30, 0))]));
        paragraph.Layout(new ParagraphConstraints(100));
        using Bitmap image = Render(paragraph);
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 10, 10));
        Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), RasterBackend.PixelAt(image, 40, 10));
    }

    [Fact]
    public void ForegroundPaint_PreservesColorFilterAndBlendMode()
    {
        using AvaloniaParagraph filtered = Build("A", style: new ParagraphTextStyle(Foreground: new Paint
        {
            Color = new Color(0xFFFF0000),
            ColorFilter = new ColorFilter.Mode(new Color(0xFF00FF00), BlendMode.SourceIn),
        }));
        filtered.Layout(new ParagraphConstraints(100));
        using Bitmap image = Render(filtered);
        Assert.Equal(((byte)0, (byte)255, (byte)0, (byte)255), RasterBackend.PixelAt(image, 10, 10));
        using AvaloniaParagraph erase = Build("A", style: new ParagraphTextStyle(Foreground: new Paint
        {
            BlendMode = BlendMode.DestinationOut,
        }));
        erase.Layout(new ParagraphConstraints(100));
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        canvas.DrawRect(new Rect(0, 0, 100, 100), new Paint { Color = new Color(0xFFFF0000) });
        canvas.DrawParagraph(erase, default);
        using Bitmap erased = recorder.EndRecording().ToImageSync(100, 100);
        Assert.Equal(0, RasterBackend.PixelAt(erased, 10, 10).A);
    }

    [Fact]
    public void RecordedParagraph_SurvivesRelayoutAndDisposal()
    {
        AvaloniaParagraph paragraph = Build("A");
        paragraph.Layout(new ParagraphConstraints(100));
        var recorder = new PictureRecorder();
        new Canvas(recorder).DrawParagraph(paragraph, default);
        Picture picture = recorder.EndRecording();
        paragraph.Layout(new ParagraphConstraints(20));
        paragraph.Dispose();
        using Bitmap image = picture.ToImageSync(100, 100);
        Assert.Equal(255, RasterBackend.PixelAt(image, 10, 10).A);
    }

    [Fact]
    public void GraphemeQueries_RejectPartialSelectionsAndKeepUtf16Ranges()
    {
        using AvaloniaParagraph paragraph = Build("A\u0301B");
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(new TextRange(0, 2), paragraph.GetGlyphInfoAt(1)!.GraphemeClusterCodeUnitRange);
        Assert.Equal(paragraph.GetGlyphInfoAt(0), paragraph.GetGlyphInfoAt(1));
        Assert.Empty(paragraph.GetBoxesForRange(0, 1));
        Assert.Single(paragraph.GetBoxesForRange(0, 2));
    }

    [Fact]
    public void HitTesting_UsesFrameworkLinePositionsAfterLeadingAndStrut()
    {
        using AvaloniaParagraph paragraph = Build("A\nB",
            new ParagraphStyle(FontFamily: Ahem, FontSize: 10, Height: 4));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(2, paragraph.GetPositionForOffset(new Point(0, 45)).Offset);
        GlyphInfo glyph = paragraph.GetClosestGlyphInfoForOffset(new Point(1000, 1000))!;
        Assert.Equal(new TextRange(2, 3), glyph.GraphemeClusterCodeUnitRange);
        Assert.Equal(64, glyph.GraphemeClusterLayoutBounds.Top, 5);
    }

    [Fact]
    public void MaxWidthBoxes_FillBothEdgesOfShortCenteredMiddleLine()
    {
        using AvaloniaParagraph paragraph = Build("AAAA\nB\nAAAA",
            new ParagraphStyle(FontFamily: Ahem, FontSize: 10, TextAlign: TextAlign.Center));
        paragraph.Layout(new ParagraphConstraints(100));
        TextBox[] middle = paragraph.GetBoxesForRange(0, 11, boxWidthStyle: BoxWidthStyle.Max)
            .Where(box => box.Top == 10).ToArray();
        Assert.Equal(3, middle.Length);
        Assert.Equal(30, middle[0].Left, 5);
        Assert.Equal(70, middle[^1].Right, 5);
    }

    [Fact]
    public void ForegroundStrokeAndBlur_AffectPixelsWithoutChangingMetrics()
    {
        using AvaloniaParagraph stroke = Build("A", style: new ParagraphTextStyle(Foreground: new Paint
        {
            Color = new Color(0xFFFF0000), Style = PaintingStyle.Stroke, StrokeWidth = 2,
        }));
        stroke.Layout(new ParagraphConstraints(100));
        using Bitmap stroked = Render(stroke);
        Assert.Equal(0, RasterBackend.PixelAt(stroked, 10, 10).A);
        Assert.True(RasterBackend.PixelAt(stroked, 0, 10).A > 0);
        using AvaloniaParagraph blur = Build("A", style: new ParagraphTextStyle(Foreground: new Paint
        {
            MaskFilter = MaskFilter.Blur(BlurStyle.Normal, 3),
        }));
        blur.Layout(new ParagraphConstraints(100));
        using Bitmap blurred = Render(blur);
        Assert.True(RasterBackend.PixelAt(blurred, 22, 10).A > 0);
        Assert.Equal(stroke.Height, blur.Height);
    }

    [Fact]
    public void Tab_IsOneSpaceAsInSkParagraph()
    {
        using AvaloniaParagraph paragraph = Build("A\tA");
        paragraph.Layout(new ParagraphConstraints(200));
        Assert.Equal(60, paragraph.MaxIntrinsicWidth, 5);
    }

    [Fact]
    public void DecorationThickness_MultipliesTheNativeFontThickness()
    {
        using AvaloniaParagraph thin = Build("A", style: new ParagraphTextStyle(Color: new Color(0),
            Decoration: Plumix.UI.TextDecoration.Underline, DecorationColor: new Color(0xFF000000),
            DecorationThickness: 1));
        using AvaloniaParagraph thick = Build("A", style: new ParagraphTextStyle(Color: new Color(0),
            Decoration: Plumix.UI.TextDecoration.Underline, DecorationColor: new Color(0xFF000000),
            DecorationThickness: 4));
        thin.Layout(new ParagraphConstraints(100));
        thick.Layout(new ParagraphConstraints(100));
        using Bitmap thinImage = Render(thin);
        using Bitmap thickImage = Render(thick);
        int thinCoverage = RasterBackend.ReadPixels(thinImage).Where((_, index) => index % 4 == 3).Sum(value => value);
        int thickCoverage = RasterBackend.ReadPixels(thickImage)
            .Where((_, index) => index % 4 == 3).Sum(value => value);
        Assert.True(thinCoverage > 0);
        Assert.True(thickCoverage > thinCoverage * 2);
        Assert.Equal(thin.Height, thick.Height);
    }

    [Fact]
    public void GraphemeAcrossPaintStyles_RemainsQueryableAsOneUtf16Cluster()
    {
        var paragraphStyle = new ParagraphStyle(FontFamily: Ahem, FontSize: 20);
        ResolvedTextStyle first = ResolvedTextStyle.FromParagraphStyle(paragraphStyle);
        ResolvedTextStyle second = first.Merge(new ParagraphTextStyle(Color: new Color(0xFFFF0000)));
        using AvaloniaParagraph paragraph = Build("A\u0301", paragraphStyle,
            runs: [new StyledRange(0, 1, first), new StyledRange(1, 2, second)]);
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(new TextRange(0, 2), paragraph.GetGlyphInfoAt(1)!.GraphemeClusterCodeUnitRange);
        Assert.Single(paragraph.GetBoxesForRange(0, 2));
        Assert.Empty(paragraph.GetBoxesForRange(-1, 2));
    }

    [Fact]
    public void AdjacentRunShadow_IsBelowEveryForegroundInTheLine()
    {
        var paragraphStyle = new ParagraphStyle(FontFamily: Ahem, FontSize: 20);
        ResolvedTextStyle defaults = ResolvedTextStyle.FromParagraphStyle(paragraphStyle);
        ResolvedTextStyle first = defaults.Merge(new ParagraphTextStyle(Color: new Color(0xFFFF0000)));
        ResolvedTextStyle second = defaults.Merge(new ParagraphTextStyle(Color: new Color(0xFF0000FF),
            Shadows: [new Shadow(new Color(0xFF00FF00), new Point(-20, 0))]));
        using AvaloniaParagraph paragraph = Build("AA", paragraphStyle,
            runs: [new StyledRange(0, 1, first), new StyledRange(1, 2, second)]);
        paragraph.Layout(new ParagraphConstraints(100));
        using Bitmap image = Render(paragraph);
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), RasterBackend.PixelAt(image, 10, 10));
        Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), RasterBackend.PixelAt(image, 30, 10));
    }

    [Theory]
    [InlineData(true, 100.0)]
    [InlineData(false, 90.0)]
    public void NativeLineMetrics_KeepRunMetricsSeparateFromStrutLeading(bool force, double reportedBaseline)
    {
        using AvaloniaParagraph paragraph = Build("A", new ParagraphStyle(FontFamily: Ahem, FontSize: 20,
            StrutStyle: new ParagraphStrutStyle(FontFamily: Ahem, FontSize: 10, Height: 10,
                Leading: 2, ForceStrutHeight: force)));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(120, paragraph.Height);
        Assert.Equal(90, paragraph.AlphabeticBaseline, 5);
        Assert.Equal(120, paragraph.IdeographicBaseline, 5);
        LineMetrics metrics = paragraph.GetLineMetricsAt(0)!.Value;
        Assert.Equal(16, metrics.Ascent, 5);
        Assert.Equal(4, metrics.Descent, 5);
        Assert.Equal(metrics.Ascent, metrics.UnscaledAscent);
        Assert.Equal(20, metrics.Height);
        Assert.Equal(reportedBaseline, metrics.Baseline, 5);
    }

    [Fact]
    public void EmptyParagraph_KeepsTheCurrentPushedStyle()
    {
        using AvaloniaParagraph paragraph = Build("", style: new ParagraphTextStyle(FontSize: 40));
        paragraph.Layout(new ParagraphConstraints(100));
        Assert.Equal(40, paragraph.Height);
        Assert.Equal(0, paragraph.NumberOfLines);
    }

    private static Bitmap Render(Paragraph paragraph)
    {
        var recorder = new PictureRecorder();
        new Canvas(recorder).DrawParagraph(paragraph, default);
        return recorder.EndRecording().ToImageSync(240, 100);
    }
}
