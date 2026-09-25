using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/rendering/error_test.dart
public sealed class RenderErrorBoxTests
{
    [Fact]
    public void Defaults_MatchTheBuildMode()
    {
        Assert.Equal(new EdgeInsets(64, 96, 64, 12), RenderErrorBox.Padding);
        Assert.Equal(200, RenderErrorBox.MinimumWidth);
        Assert.Equal(new Color(Constants.KDebugMode ? 0xF0900000 : 0xF0C0C0C0),
            RenderErrorBox.BackgroundColor);
        Assert.Equal(new Color(Constants.KDebugMode ? 0xFFFFFF66 : 0xFF303030),
            RenderErrorBox.TextStyle.Color);
        Assert.Equal(Constants.KDebugMode ? "monospace" : "sans-serif", RenderErrorBox.TextStyle.FontFamily!.Name);
        Assert.Equal(Constants.KDebugMode ? 14 : 18, RenderErrorBox.TextStyle.FontSize);
        Assert.Equal(Constants.KDebugMode ? (FontWeight?)FontWeight.Bold : null, RenderErrorBox.TextStyle.FontWeight);
        Assert.Equal(TextDirection.Ltr, RenderErrorBox.ParagraphStyle.TextDirection);
        Assert.Equal(TextAlign.Left, RenderErrorBox.ParagraphStyle.TextAlign);
    }

    [Theory]
    [InlineData(800, 600, 200, 672, 64, 96)]
    [InlineData(100, 600, 200, 100, 0, 96)]
    [InlineData(800, 100, 200, 672, 64, 0)]
    [InlineData(800, 600, 800, 800, 0, 96)]
    [InlineData(328, 122, 200, 328, 0, 0)]
    [InlineData(329, 123, 200, 201, 64, 96)]
    public void Paint_UsesFlutterPaddingThresholds(
        double width, double height, double minimumWidth, double paragraphWidth, double left, double top)
    {
        double previous = RenderErrorBox.MinimumWidth;
        try
        {
            RenderErrorBox.MinimumWidth = minimumWidth;
            var box = new RenderErrorBox("Some error message");
            var spy = new PaintSpyParagraph();
            ParagraphField.SetValue(box, spy);
            box.Layout(BoxConstraints.Tight(new Size(width, height)));
            var root = new ContainerLayer();
            var context = new PaintingContext(root);
            var origin = new Point(7, 11);

            box.Paint(context, origin);
            context.DebugStopRecordingIfNeeded();

            Assert.Equal(paragraphWidth, spy.Width);
            Assert.Equal(origin + new Point(left, top), spy.PaintedOffset);
            Assert.Equal(2, Assert.IsType<PictureLayer>(Assert.Single(root.Children)).Picture!.DrawCommandCount);
            Assert.True(box.HitTest(new BoxHitTestResult(), new Point(1, 1)));
        }
        finally
        {
            RenderErrorBox.MinimumWidth = previous;
        }
    }

    [Fact]
    public void Constructor_BuildsTheParagraphAndSnapshotsAllEngineStyles()
    {
        ParagraphTextStyle previousText = RenderErrorBox.TextStyle;
        ParagraphStyle previousParagraph = RenderErrorBox.ParagraphStyle;
        try
        {
            RenderErrorBox.TextStyle = new ParagraphTextStyle(FontSize: 10, LetterSpacing: 2);
            RenderErrorBox.ParagraphStyle = new ParagraphStyle(MaxLines: 1, TextDirection: TextDirection.Rtl);
            var box = new RenderErrorBox("abcdefghij");
            var paragraph = Assert.IsAssignableFrom<Paragraph>(ParagraphField.GetValue(box));
            RenderErrorBox.TextStyle = new ParagraphTextStyle(FontSize: 30);
            RenderErrorBox.ParagraphStyle = new ParagraphStyle();

            box.Layout(BoxConstraints.Tight(new Size(50, 30)));
            box.Paint(new PaintingContext(new ContainerLayer()), default);
            Assert.Equal(10, paragraph.Height);
            Assert.Equal(1, paragraph.NumberOfLines);
            Assert.True(paragraph.DidExceedMaxLines);
            box.Layout(BoxConstraints.Tight(new Size(800, 600)));
            box.Paint(new PaintingContext(new ContainerLayer()), default);
            Assert.Same(paragraph, ParagraphField.GetValue(box));
            Assert.Equal(672, paragraph.Width);
            Assert.Equal(120, paragraph.LongestLine);
        }
        finally
        {
            RenderErrorBox.TextStyle = previousText;
            RenderErrorBox.ParagraphStyle = previousParagraph;
        }
    }

    [Fact]
    public void EmptyMessageOrFailedParagraphBuild_StillPaintsTheBackground()
    {
        // Construct invalid UTF-16 here: xUnit's InlineData serialization replaces lone surrogates.
        foreach (string message in new[] { string.Empty, new string((char)0xD800, 1) })
        {
            var box = new RenderErrorBox(message);
            Assert.Null(ParagraphField.GetValue(box));
            box.Layout(BoxConstraints.Tight(new Size(80, 40)));
            var root = new ContainerLayer();
            var context = new PaintingContext(root);
            box.Paint(context, default);
            context.DebugStopRecordingIfNeeded();
            Assert.Equal(1, Assert.IsType<PictureLayer>(Assert.Single(root.Children)).Picture!.DrawCommandCount);
        }
    }

    [Fact]
    public void BackgroundColor_IsReadAtEveryPaintAndCoversTheWholeBox()
    {
        Color previous = RenderErrorBox.BackgroundColor;
        try
        {
            var box = new RenderErrorBox();
            box.Layout(BoxConstraints.Tight(new Size(800, 600)));
            foreach (Color color in new[] { previous, new Color(0xFF112233) })
            {
                RenderErrorBox.BackgroundColor = color;
                var root = new ContainerLayer();
                var context = new PaintingContext(root);
                box.Paint(context, new Point(7, 11));
                context.DebugStopRecordingIfNeeded();
                Picture picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children)).Picture!;
                var commands = (IReadOnlyList<CanvasCommand>)typeof(Picture)
                    .GetField("_commands", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(picture)!;
                object closure = Assert.Single(commands).Draw!.Target!;
                object?[] captured = closure.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(field => field.GetValue(closure)).ToArray();
                Assert.Equal(new Rect(7, 11, 800, 600), Assert.Single(captured.OfType<Rect>()));
                Assert.Equal(color, Assert.Single(captured.OfType<SolidColorBrush>()).Color);
            }
        }
        finally
        {
            RenderErrorBox.BackgroundColor = previous;
        }
    }

    [Fact]
    public void PaintFailure_IsSwallowedAfterPaintingTheBackground()
    {
        var box = new RenderErrorBox("error");
        ParagraphField.SetValue(box, new PaintSpyParagraph { ThrowOnLayout = true });
        box.Layout(BoxConstraints.Tight(new Size(80, 40)));
        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        box.Paint(context, default);
        context.DebugStopRecordingIfNeeded();
        Assert.Equal(1, Assert.IsType<PictureLayer>(Assert.Single(root.Children)).Picture!.DrawCommandCount);
    }

    // The headless backend records no glyphs. Substitute only the engine paragraph to observe
    // Canvas.drawParagraph's offset while exercising the real render object's layout/paint path.
    private static FieldInfo ParagraphField => typeof(RenderErrorBox)
        .GetField("_paragraph", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private sealed class PaintSpyParagraph() : Paragraph("error")
    {
        private double _width;
        public bool ThrowOnLayout { get; init; }
        public Point? PaintedOffset { get; private set; }
        public override double Width => _width;
        public override double Height => 14;
        public override double LongestLine => 14;
        public override double MinIntrinsicWidth => 14;
        public override double MaxIntrinsicWidth => 14;
        public override double AlphabeticBaseline => 10;
        public override double IdeographicBaseline => 14;
        public override bool DidExceedMaxLines => false;
        public override int NumberOfLines => 1;
        public override void Layout(ParagraphConstraints constraints)
        {
            if (ThrowOnLayout)
            {
                throw new InvalidOperationException("failed layout");
            }
            _width = constraints.Width;
        }
        public override IReadOnlyList<TextBox> GetBoxesForRange(
            int start, int end, BoxHeightStyle boxHeightStyle, BoxWidthStyle boxWidthStyle) => [];
        public override IReadOnlyList<TextBox> GetBoxesForPlaceholders() => [];
        public override TextPosition GetPositionForOffset(Point offset) => new(0);
        public override GlyphInfo? GetGlyphInfoAt(int codeUnitOffset) => null;
        public override GlyphInfo? GetClosestGlyphInfoForOffset(Point offset) => null;
        private protected override TextRange GetLineRange(int offset) => TextRange.Empty;
        public override IReadOnlyList<LineMetrics> ComputeLineMetrics() => [];
        public override LineMetrics? GetLineMetricsAt(int lineNumber) => null;
        public override int? GetLineNumberAt(int codeUnitOffset) => null;
        internal override Action<DrawingContext>? CreateDrawAction(Point offset)
        {
            PaintedOffset = offset;
            return static _ => { };
        }
    }
}
