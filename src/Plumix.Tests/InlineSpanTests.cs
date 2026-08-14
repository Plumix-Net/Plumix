using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class InlineSpanTests
{
    [Fact]
    public void TextSpan_ResolvesDefaultMouseCursorFromRecognizer()
    {
        var plain = new TextSpan(text: "plain");
        Assert.Equal(MouseCursor.Defer, plain.MouseCursor);
        Assert.True(plain.ValidForMouseTracker);

        using var recognizer = new TapGestureRecognizer();
        var tappable = new TextSpan(text: "link", recognizer: recognizer);
        Assert.Equal(SystemMouseCursors.Click, tappable.MouseCursor);

        var custom = new TextSpan(text: "link", recognizer: recognizer, mouseCursor: SystemMouseCursors.Text);
        Assert.Equal(SystemMouseCursors.Text, custom.MouseCursor);
    }

    [Fact]
    public void TextSpan_RejectsSemanticsLabelWithoutText()
    {
        Assert.Throws<ArgumentException>(() => new TextSpan(semanticsLabel: "label"));
    }

    [Fact]
    public void ToPlainText_HonoursSemanticsLabelsAndPlaceholders()
    {
        var span = new TextSpan(
            text: "$$",
            semanticsLabel: "Double dollars",
            children:
            [
                new TextSpan(text: " and "),
                new WidgetSpan(new SizedBox()),
            ]);

        Assert.Equal("Double dollars and ￼", span.ToPlainText());
        Assert.Equal("$$ and ￼", span.ToPlainText(includeSemanticsLabels: false));
        Assert.Equal("$$ and ", span.ToPlainText(includeSemanticsLabels: false, includePlaceholders: false));
    }

    [Fact]
    public void CodeUnitAt_WalksTextAndPlaceholders()
    {
        var widgetSpan = new WidgetSpan(new SizedBox());
        Assert.Null(widgetSpan.CodeUnitAt(-1));
        Assert.Equal(PlaceholderSpan.PlaceholderCodeUnit, widgetSpan.CodeUnitAt(0));
        Assert.Null(widgetSpan.CodeUnitAt(1));

        var span = new TextSpan(text: "AAA", children: [widgetSpan, widgetSpan]);
        Assert.Equal('A', span.CodeUnitAt(0));
        Assert.Equal('A', span.CodeUnitAt(2));
        Assert.Equal(PlaceholderSpan.PlaceholderCodeUnit, span.CodeUnitAt(3));
        Assert.Equal(PlaceholderSpan.PlaceholderCodeUnit, span.CodeUnitAt(4));
        Assert.Null(span.CodeUnitAt(5));
    }

    [Fact]
    public void GetSpanForPosition_FollowsAffinityRules()
    {
        var first = new TextSpan(text: "abc");
        var second = new TextSpan(text: "def");
        var root = new TextSpan(children: [first, second]);

        Assert.Same(first, root.GetSpanForPosition(new TextPosition(0)));
        Assert.Same(first, root.GetSpanForPosition(new TextPosition(1)));
        Assert.Same(first, root.GetSpanForPosition(new TextPosition(3, TextAffinity.Upstream)));
        Assert.Same(second, root.GetSpanForPosition(new TextPosition(3)));
        Assert.Same(second, root.GetSpanForPosition(new TextPosition(4)));

        // A WidgetSpan never reports a containing position.
        Assert.Null(new WidgetSpan(new SizedBox()).GetSpanForPosition(new TextPosition(0)));
    }

    [Fact]
    public void CompareTo_ReportsTheCheapestSufficientRepaint()
    {
        var a = new TextSpan(text: "one", style: new TextStyle(Color: Colors.Red));
        Assert.Equal(RenderComparison.Identical, a.CompareTo(a));
        Assert.Equal(
            RenderComparison.Identical,
            a.CompareTo(new TextSpan(text: "one", style: new TextStyle(Color: Colors.Red))));
        Assert.Equal(
            RenderComparison.Paint,
            a.CompareTo(new TextSpan(text: "one", style: new TextStyle(Color: Colors.Blue))));
        Assert.Equal(
            RenderComparison.Layout,
            a.CompareTo(new TextSpan(text: "two", style: new TextStyle(Color: Colors.Red))));
        Assert.Equal(
            RenderComparison.Layout,
            a.CompareTo(new TextSpan(text: "one", style: new TextStyle(Color: Colors.Red, FontSize: 20))));
        Assert.Equal(RenderComparison.Layout, a.CompareTo(new WidgetSpan(new SizedBox())));

        using var recognizer = new TapGestureRecognizer();
        var plain = new TextSpan(text: "one");
        Assert.Equal(RenderComparison.Metadata, plain.CompareTo(new TextSpan(text: "one", recognizer: recognizer)));
    }

    [Fact]
    public void ComputeSemanticsInformation_InheritsLocaleAndSpellOutThroughTextSpans()
    {
        var span = new TextSpan(
            text: "outer",
            locale: "es_MX",
            spellOut: true,
            children: [new TextSpan(text: "inner")]);

        List<InlineSpanSemanticsInformation> info = span.GetSemanticsInformation();
        Assert.Equal(2, info.Count);
        foreach (InlineSpanSemanticsInformation entry in info)
        {
            Assert.Contains(entry.StringAttributes, attribute => attribute is SpellOutStringAttribute);
            Assert.Contains(
                entry.StringAttributes,
                attribute => attribute is LocaleStringAttribute { Locale: "es_MX" });
        }

        Assert.Equal(new TextRange(0, 5), info[0].StringAttributes[0].Range);
    }

    [Fact]
    public void ComputeSemanticsInformation_SkipsAttributesForEmptyText()
    {
        List<InlineSpanSemanticsInformation> info =
            new TextSpan(text: string.Empty, spellOut: true).GetSemanticsInformation();
        InlineSpanSemanticsInformation only = Assert.Single(info);
        Assert.Equal(string.Empty, only.Text);
        Assert.Empty(only.StringAttributes);
    }

    [Fact]
    public void CombineSemanticsInfo_SplitsOnNodesThatNeedTheirOwn()
    {
        using var recognizer = new TapGestureRecognizer();
        var span = new TextSpan(
            text: "hello ",
            children:
            [
                new TextSpan(text: "world", recognizer: recognizer),
                new TextSpan(text: " this is"),
                new TextSpan(text: " a cat-astrophe"),
            ]);

        List<InlineSpanSemanticsInformation> combined =
            InlineSpan.CombineSemanticsInfo(span.GetSemanticsInformation());

        Assert.Equal(3, combined.Count);
        Assert.Equal("hello ", combined[0].SemanticsLabel);
        Assert.Equal("world", combined[1].Text);
        Assert.True(combined[1].RequiresOwnNode);
        Assert.Equal(" this is a cat-astrophe", combined[2].SemanticsLabel);
    }

    [Fact]
    public void CombineSemanticsInfo_ShiftsAttributeRangesByTheAccumulatedLabel()
    {
        var span = new TextSpan(
            children:
            [
                new TextSpan(text: "abc"),
                new TextSpan(text: "de", spellOut: true),
            ]);

        InlineSpanSemanticsInformation combined =
            Assert.Single(InlineSpan.CombineSemanticsInfo(span.GetSemanticsInformation()));
        Assert.Equal("abcde", combined.SemanticsLabel);
        Assert.Equal(new TextRange(3, 5), Assert.Single(combined.StringAttributes).Range);
    }

    [Fact]
    public void WidgetSpan_RequiresABaselineForBaselineRelativeAlignments()
    {
        foreach (PlaceholderAlignment alignment in new[]
                 {
                     PlaceholderAlignment.Baseline,
                     PlaceholderAlignment.AboveBaseline,
                     PlaceholderAlignment.BelowBaseline,
                 })
        {
            Assert.Throws<ArgumentException>(() => new WidgetSpan(new SizedBox(), alignment));
        }

        Assert.NotNull(new WidgetSpan(new SizedBox(), PlaceholderAlignment.Middle));
        Assert.NotNull(new WidgetSpan(new SizedBox(), PlaceholderAlignment.Baseline, TextBaseline.Alphabetic));
    }

    [Fact]
    public void ExtractFromInlineSpan_AppliesTheNearestAncestorFontSize()
    {
        var scaler = new SquareTextScaler();
        var span = new TextSpan(
            children:
            [
                new WidgetSpan(new SizedBox(), style: new TextStyle(FontSize: 0)),
                new WidgetSpan(new SizedBox(), style: new TextStyle(FontSize: 10)),
                new TextSpan(
                    style: new TextStyle(FontSize: 20),
                    children: [new WidgetSpan(new SizedBox())]),
                new WidgetSpan(new SizedBox()),
            ]);

        List<Widget> widgets = WidgetSpan.ExtractFromInlineSpan(span, scaler);
        Assert.Equal(4, widgets.Count);
        Assert.Equal(
            [0.0, 10.0, 20.0, 14.0],
            widgets.Select(widget => ScaleFactorOf(widget)).ToArray());
    }

    [Fact]
    public void RichText_ValidatesMaxLinesAndSelectionColor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RichText(new TextSpan(text: "a"), maxLines: 0));
        Assert.Throws<ArgumentException>(() => new RichText(
            new TextSpan(text: "a"),
            selectionRegistrar: new RecordingSelectionRegistrar()));
    }

    [Fact]
    public void RichText_ExposesEveryWidgetSpanAsAParagraphChild()
    {
        using var harness = new SpanHarness(new Directionality(
            TextDirection.Ltr,
            new RichText(new TextSpan(
                text: "before ",
                children:
                [
                    new WidgetSpan(new SizedBox(width: 12, height: 8)),
                    new TextSpan(text: " after "),
                    new WidgetSpan(new SizedBox(width: 4, height: 6)),
                ]))));
        harness.Pump(new Size(400, 100));

        RenderParagraph paragraph = harness.RequireParagraph();
        Assert.Equal(2, paragraph.ChildCount);
        Assert.Equal("before ￼ after ￼", paragraph.PlainText);
        for (RenderBox? child = paragraph.FirstChild; child is not null; child = paragraph.ChildAfter(child))
        {
            var parentData = Assert.IsType<TextParentData>(child.parentData);
            Assert.NotNull(parentData.Span);
            Assert.NotNull(parentData.InlineOffset);
        }
    }

    [Fact]
    public void RenderParagraph_TextSetterUsesTheCheapestSufficientInvalidation()
    {
        using var harness = new SpanHarness(new Directionality(
            TextDirection.Ltr,
            new RichText(new TextSpan(text: "abc", style: new TextStyle(Color: Colors.Red)))));
        harness.Pump(new Size(200, 100));
        RenderParagraph paragraph = harness.RequireParagraph();

        paragraph.Text = new TextSpan(text: "abc", style: new TextStyle(Color: Colors.Red));
        Assert.False(paragraph.NeedsLayout);
        Assert.False(paragraph.NeedsPaint);

        paragraph.Text = new TextSpan(text: "abc", style: new TextStyle(Color: Colors.Blue));
        Assert.False(paragraph.NeedsLayout);
        Assert.True(paragraph.NeedsPaint);

        harness.Pump(new Size(200, 100));
        paragraph.Text = new TextSpan(text: "abcd", style: new TextStyle(Color: Colors.Blue));
        Assert.True(paragraph.NeedsLayout);
    }

    [Fact]
    public void RenderParagraph_ProjectsTheRootStyleThroughTheLegacyAccessors()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            text: "abc",
            style: new TextStyle(FontSize: 18, Color: Colors.Red, FontWeight: FontWeight.Bold)));

        Assert.Equal(18, paragraph.FontSize);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
        Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);

        paragraph.FontSize = 24;
        Assert.Equal(24, paragraph.FontSize);
        Assert.Equal("abc", paragraph.PlainText);
        Assert.Equal(FontWeight.Bold, paragraph.FontWeight);
    }

    [Fact]
    public void Text_RichNestsTheSuppliedSpanUnderTheResolvedStyle()
    {
        using var harness = new SpanHarness(new Directionality(
            TextDirection.Ltr,
            new DefaultTextStyle(
                new TextStyle(FontSize: 20, Color: Colors.Green),
                Text.Rich(new TextSpan(text: "rich")))));
        harness.Pump(new Size(200, 100));

        RenderParagraph paragraph = harness.RequireParagraph();
        var root = Assert.IsType<TextSpan>(paragraph.Text);
        Assert.Null(root.Text);
        Assert.Equal(20, root.Style!.FontSize);
        Assert.Equal(Colors.Green, root.Style.Color);
        Assert.Equal("rich", Assert.IsType<TextSpan>(Assert.Single(root.Children!)).Text);
        Assert.Equal("rich", paragraph.PlainText);
    }

    [Fact]
    public void RenderParagraph_BuildsOneSemanticsChildPerRecognizerRun()
    {
        using var tap = new TapGestureRecognizer();
        using var longPress = new LongPressGestureRecognizer();
        var paragraph = new RenderParagraph(new TextSpan(
            text: "plain ",
            children:
            [
                new TextSpan(text: "tap", recognizer: tap),
                new TextSpan(text: "hold", recognizer: longPress),
            ]));

        var configuration = new SemanticsConfiguration();
        paragraph.InvokeDescribeSemanticsConfiguration(configuration);
        Assert.True(configuration.IsSemanticBoundary);
        Assert.True(configuration.ExplicitChildNodes);
    }

    [Fact]
    public void RenderParagraph_FlattensTheSpanTreeIntoOneLabelWithoutRecognizers()
    {
        var paragraph = new RenderParagraph(new TextSpan(
            text: "one ",
            children: [new TextSpan(text: "two", semanticsLabel: "second")]));

        var configuration = new SemanticsConfiguration();
        paragraph.InvokeDescribeSemanticsConfiguration(configuration);
        Assert.False(configuration.IsSemanticBoundary);
        Assert.Equal("one second", configuration.Label);
    }

    private static double ScaleFactorOf(Widget widget)
    {
        var parentData = Assert.IsType<WidgetSpanParentData>(widget);
        return Assert.IsType<AutoScaleInlineWidget>(parentData.Child).TextScaleFactor;
    }

    /// A scaler whose output is not proportional, so the per-span font size the
    /// extraction pushed is observable in the resulting factor.
    private sealed record SquareTextScaler : TextScaler
    {
        public override double Scale(double fontSize) => fontSize * fontSize;

        public override double TextScaleFactor => 1.0;
    }

    private sealed class SpanHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public SpanHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public RenderParagraph RequireParagraph()
        {
            RenderParagraph? found = FindDescendant<RenderParagraph>(RenderView);
            Assert.NotNull(found);
            return found!;
        }

        public void Dispose() => _root.Unmount();

        private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
        {
            if (root is null)
            {
                return null;
            }

            if (root is T match)
            {
                return match;
            }

            T? result = null;
            root.VisitChildren(child =>
            {
                result ??= FindDescendant<T>(child);
            });
            return result;
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public HarnessRootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, null);
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot) => _view.Child = null;
        }
    }

    private sealed class RecordingSelectionRegistrar : ISelectionRegistrar
    {
        public List<ISelectable> Selectables { get; } = [];

        public void Add(ISelectable selectable) => Selectables.Add(selectable);

        public void Remove(ISelectable selectable) => Selectables.Remove(selectable);
    }
}
