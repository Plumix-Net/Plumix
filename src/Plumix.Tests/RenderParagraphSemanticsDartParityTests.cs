using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/rendering/paragraph_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/text_semantics_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/text_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/rich_text_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RenderParagraphSemanticsDartParityTests
{
    // paragraph_test.dart: 'Supports gesture recognizer semantics'.
    [Fact]
    public void SupportsGestureRecognizerSemantics()
    {
        int taps = 0;
        int doubleTaps = 0;
        int longPresses = 0;
        using var tap = new TapGestureRecognizer { OnTap = () => taps++ };
        using var doubleTap = new DoubleTapGestureRecognizer { OnDoubleTap = () => doubleTaps++ };
        using var longPress = new LongPressGestureRecognizer { OnLongPress = () => longPresses++ };
        var paragraph = Paragraph(new TextSpan("plain", children:
        [
            new TextSpan("tap", recognizer: tap),
            new TextSpan("hold", recognizer: longPress),
            new TextSpan("twice", recognizer: doubleTap),
        ]));

        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["plain", "tap", "hold", "twice"], node.Children.Select(child => child.Label));
        Assert.Equal(SemanticsActions.None, node.Children[0].Actions);
        Assert.True(node.Children[1].PerformAction(SemanticsActions.Tap));
        Assert.True(node.Children[2].PerformAction(SemanticsActions.LongPress));
        Assert.True(node.Children[3].PerformAction(SemanticsActions.Tap));
        Assert.True(node.Children[1].Flags.HasFlag(SemanticsFlags.IsLink));
        Assert.True(node.Children[3].Flags.HasFlag(SemanticsFlags.IsLink));
        Assert.Equal((1, 1, 1), (taps, longPresses, doubleTaps));
    }

    // rich_text_test.dart: 'RichText with recognizers without handlers does not throw'.
    [Fact]
    public void RecognizersWithoutHandlersHaveNoActions()
    {
        using var tap = new TapGestureRecognizer();
        using var doubleTap = new DoubleTapGestureRecognizer();
        using var longPress = new LongPressGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("root", children:
        [
            new TextSpan("one", recognizer: tap),
            new TextSpan("two", recognizer: doubleTap),
            new TextSpan("three", recognizer: longPress),
        ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["root", "one", "two", "three"], node.Children.Select(child => child.Label));
        Assert.All(node.Children, child =>
        {
            Assert.Equal(SemanticsActions.None, child.Actions);
            Assert.False(child.Flags.HasFlag(SemanticsFlags.IsLink));
        });
    }

    // paragraph_test.dart: 'Supports empty text span with spell out'.
    [Fact]
    public void EmptySpelledOutTextHasNoAttributes()
    {
        var paragraph = Paragraph(new TextSpan(string.Empty, spellOut: true));
        var config = new SemanticsConfiguration();
        paragraph.InvokeDescribeSemanticsConfiguration(config);
        Assert.Equal(string.Empty, config.Label);
        Assert.Empty(config.AttributedLabel.Attributes);
    }

    // paragraph_test.dart: 'Asserts on unsupported gesture recognizer'.
    [DebugOnlyFact]
    public void UnsupportedGestureRecognizerAsserts()
    {
        using var drag = new HorizontalDragGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("drag", recognizer: drag));
        Assert.Contains("not supported", Assert.Throws<AssertionError>(() => Assemble(paragraph)).Message);
    }

    // paragraph_test.dart: text spans and empty WidgetSpans that yield no selection boxes.
    [Fact]
    public void EmptySpansAndEmptyWidgetsDoNotCreateTextNodes()
    {
        using var tap = new TapGestureRecognizer();
        var box = new RenderConstrainedBox(BoxConstraints.Tight(new Size(0, 0)));
        var paragraph = Paragraph(new TextSpan(children:
        [
            new TextSpan(string.Empty, recognizer: tap),
            new TextSpan("one", recognizer: tap),
            new WidgetSpan(SizedBox.Shrink()),
            new TextSpan("two", recognizer: tap),
        ]), [box]);
        Assert.Equal(["one", "two"], Assemble(paragraph).Children.Select(child => child.Label));
    }

    // text_semantics_test.dart: 'SemanticsNode ids are stable'.
    [Fact]
    public void NodeIdsSurviveUpdatesShrinkAndGrowInOrder()
    {
        using var tap = new TapGestureRecognizer();
        InlineSpan Text(int count) => new TextSpan(children: Enumerable.Range(0, count)
            .Select(index => (InlineSpan)new TextSpan($"run {index}", recognizer: tap)).ToList());
        var paragraph = Paragraph(Text(3));
        SemanticsNode first = Assemble(paragraph);
        int[] ids = first.Children.Select(child => child.Id).ToArray();
        SemanticsNode same = Assemble(paragraph);
        Assert.Equal(ids, same.Children.Select(child => child.Id));

        paragraph.Text = Text(2);
        paragraph.Layout(new BoxConstraints(MaxWidth: 800));
        Assert.Equal(ids.Take(2), Assemble(paragraph).Children.Select(child => child.Id));

        paragraph.Text = Text(3);
        paragraph.Layout(new BoxConstraints(MaxWidth: 800));
        SemanticsNode grown = Assemble(paragraph);
        Assert.Equal(ids.Take(2), grown.Children.Take(2).Select(child => child.Id));
        Assert.NotEqual(ids[2], grown.Children[2].Id);

        paragraph.ClearSemantics();
        Assert.DoesNotContain(Assemble(paragraph).Children[0].Id, ids);
    }

    // text_semantics_test.dart: 'SemanticsIdentifier creates a functional SemanticsNode'.
    [Fact]
    public void IdentifierSplitsOrdinaryRunsIntoItsOwnNode()
    {
        var paragraph = Paragraph(new TextSpan("Hello, ", children:
        [
            new TextSpan("a "),
            new TextSpan("new node", semanticsIdentifier: "new_semantics_node"),
            new TextSpan(" has been"),
            new TextSpan(" created"),
        ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["Hello, a ", "new node", " has been created"], node.Children.Select(child => child.Label));
        Assert.Equal(["", "new_semantics_node", ""], node.Children.Select(child => child.Identifier));
    }

    // rich_text_test.dart: 'TextSpan Locale works' and 'TextSpan spellOut works'.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttributesSurviveRecognizerNodeSplitting(bool spellOut)
    {
        using var tap = new TapGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("root", locale: spellOut ? null : "es-MX",
            spellOut: spellOut ? true : null, children:
            [
                new TextSpan("one", recognizer: tap),
                new TextSpan("three"),
            ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["root", "one", "three"], node.Children.Select(child => child.Label));
        foreach (SemanticsNode child in node.Children)
        {
            StringAttribute attribute = Assert.Single(child.AttributedLabel.Attributes);
            Assert.Equal(new TextRange(0, child.Label.Length), attribute.Range);
            if (spellOut)
            {
                Assert.IsType<SpellOutStringAttribute>(attribute);
            }
            else
            {
                Assert.Equal("es-MX", Assert.IsType<LocaleStringAttribute>(attribute).Locale);
            }
        }
    }

    // text_test.dart: 'semanticsLabel can be shorter than text'.
    [Fact]
    public void EmptySemanticsLabelKeepsGeometryAndFollowingOffsets()
    {
        using var tap = new TapGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("Some Text", semanticsLabel: string.Empty, children:
        [
            new TextSpan("Clickable", recognizer: tap),
        ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["", "Clickable"], node.Children.Select(child => child.Label));
        Assert.Equal(new Rect(-4, -4, 98, 18), node.Children[0].Rect);
        Assert.Equal(new Rect(86, -4, 98, 18), node.Children[1].Rect);
    }

    [Fact]
    public void BoundsRoundEdgesAndClampToConstraints()
    {
        using var tap = new TapGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("A", style: new TextStyle(FontSize: 14.8), children:
        [
            new TextSpan("B", recognizer: tap),
        ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(new Rect(10, -4, 24, 23), node.Children[1].Rect);
        paragraph.SoftWrap = false;
        paragraph.Layout(new BoxConstraints(MaxWidth: 5, MaxHeight: 4));
        SemanticsNode clipped = Assemble(paragraph);
        Assert.Equal(new Rect(-4, -4, 13, 12), clipped.Children[0].Rect);
        Assert.Equal(new Rect(10, -4, 14, 12), clipped.Children[1].Rect);
    }

    // text_test.dart: 'recognizers split semantic node - bidi'.
    [Fact]
    public void BidiRunsKeepLogicalOrderAndPreviousRunDirection()
    {
        using var tap = new TapGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("hello ", children:
        [
            new TextSpan("שלום", recognizer: tap),
            new TextSpan(" world"),
        ]));
        SemanticsNode node = Assemble(paragraph);
        Assert.Equal(["hello ", "שלום", " world"], node.Children.Select(child => child.Label));
        Assert.Equal([TextDirection.Ltr, TextDirection.Ltr, TextDirection.Rtl],
            node.Children.Select(child => child.TextDirection));
    }

    // paragraph_test.dart: 'can just update the gesture recognizer'.
    [Fact]
    public void UpdatingOnlyRecognizerReplacesActionAndPreservesNode()
    {
        using var tap = new TapGestureRecognizer { OnTap = () => { } };
        int holds = 0;
        using var hold = new LongPressGestureRecognizer { OnLongPress = () => holds++ };
        var paragraph = Paragraph(new TextSpan("text", recognizer: tap));
        int id = Assert.Single(Assemble(paragraph).Children).Id;
        paragraph.Text = new TextSpan("text", recognizer: hold);
        SemanticsNode changed = Assert.Single(Assemble(paragraph).Children);
        Assert.Equal(id, changed.Id);
        Assert.False(changed.PerformAction(SemanticsActions.Tap));
        Assert.True(changed.PerformAction(SemanticsActions.LongPress));
        Assert.Equal(1, holds);
    }

    // text_test.dart: WidgetSpans with no semantic information are elided, cases 1-4;
    // one WidgetSpan may produce several semantics nodes, interleaved with recognized text.
    [Fact]
    public void PlaceholdersConsumeAllTaggedNodesAndSkipSilentChildren()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        using var tap = new TapGestureRecognizer { OnTap = () => { } };
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new RichText(text: new TextSpan("before ",
            style: new TextStyle(FontSize: 10), children:
            [
                new WidgetSpan(SizedBox.Shrink()),
                new WidgetSpan(new Row(mainAxisSize: MainAxisSize.Min, children:
                [
                    new Semantics(container: true, label: "foo", child: new SizedBox(width: 10, height: 10)),
                    new Semantics(container: true, label: "bar", child: new SizedBox(width: 10, height: 10)),
                ])),
                new TextSpan("link", recognizer: tap),
                new WidgetSpan(new Semantics(container: true, label: "HELLO",
                    child: new SizedBox(width: 10, height: 10))),
                new TextSpan(" after"),
            ]))));
        SemanticsNode node = tester.GetSemantics(Find.ByType<RichText>().First);
        Assert.Equal(["before ", "foo", "bar", "link", "HELLO", " after"],
            node.Children.Select(child => child.Label));
        Assert.True(node.Children[1].IsTagged(new PlaceholderSpanIndexSemanticsTag(1)));
        Assert.True(node.Children[2].IsTagged(new PlaceholderSpanIndexSemanticsTag(1)));
        Assert.True(node.Children[4].IsTagged(new PlaceholderSpanIndexSemanticsTag(2)));
    }

    // text_test.dart: 'inline widgets semantic nodes scale'.
    [Fact]
    public void InlineWidgetSemanticsRetainsLocalRectAndAppliesTextScale()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        using var tap = new TapGestureRecognizer();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new RichText(textScaler: TextScaler.Linear(2),
            text: new TextSpan("a ", style: new TextStyle(FontSize: 10), children:
            [
                new TextSpan("link", recognizer: tap),
                new WidgetSpan(new Semantics(container: true, label: "scaled",
                    child: new SizedBox(width: 20, height: 40))),
            ]))));
        SemanticsNode node = tester.GetSemantics(Find.ByType<RichText>().First);
        SemanticsNode scaled = Assert.Single(node.Children, child => child.Label == "scaled");
        Assert.Equal(new Rect(0, 0, 20, 40), scaled.Rect);
        Assert.NotNull(scaled.Transform);
        Assert.Equal(2.0, scaled.Transform![0]);
        Assert.Equal(2.0, scaled.Transform[5]);
    }

    // paragraph_test.dart: 'Does not include the semantics node of truncated rendering children'.
    [Fact]
    public void TruncatedPlaceholderNodesAreConsumedButNotIncluded()
    {
        using var tap = new TapGestureRecognizer();
        var box = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 20)));
        var paragraph = Paragraph(new TextSpan("too much text ", children:
        [
            new WidgetSpan(SizedBox.Shrink()),
            new TextSpan("link", recognizer: tap),
        ]), [box]);
        paragraph.MaxLines = 1;
        paragraph.Overflow = TextOverflow.Ellipsis;
        paragraph.Layout(new BoxConstraints(MaxWidth: 40));
        var child = new SemanticsNode();
        child.AddTags([new PlaceholderSpanIndexSemanticsTag(0)]);
        child.UpdateWith(new SemanticsConfiguration { Label = "truncated" });
        Assert.DoesNotContain(Assemble(paragraph, [child]).Children, node => node.Label == "truncated");
    }

    [Fact]
    public void OffscreenTextIsHiddenAndShowOnScreenUsesCurrentRunBounds()
    {
        using var tap = new TapGestureRecognizer();
        var paragraph = Paragraph(new TextSpan("visible\nhidden", recognizer: tap));
        var parent = new ShowOnScreenBox(paragraph);
        parent.Layout(new BoxConstraints(MaxWidth: 800));
        SemanticsNode node = Assemble(paragraph, clip: new Rect(0, 100, 800, 20));
        SemanticsNode run = Assert.Single(node.Children);
        Assert.True(run.IsHidden);
        run.ShowOnScreenRequest!();
        Assert.Same(paragraph, parent.Descendant);
        Assert.Equal(run.Rect, parent.RequestedRect);

        paragraph.Text = new TextSpan("short", style: new TextStyle(FontSize: 10), recognizer: tap);
        parent.Layout(new BoxConstraints(MaxWidth: 800));
        SemanticsNode changed = Assert.Single(Assemble(paragraph).Children);
        Assert.Same(run, changed);
        Assert.False(changed.IsHidden);
        run.ShowOnScreenRequest!();
        Assert.Equal(changed.Rect, parent.RequestedRect);
        Assert.Equal(new Rect(-4, -4, 58, 18), parent.RequestedRect);
    }

    // paragraph_test.dart: changing a recognizer causes exactly one semantics notification.
    [Fact]
    public void RecognizerChangeFlushesExactlyOneSemanticsUpdate()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        using var tap = new TapGestureRecognizer { OnTap = () => { } };
        using var hold = new LongPressGestureRecognizer { OnLongPress = () => { } };
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new RichText(text: new TextSpan("text", recognizer: tap))));
        RenderParagraph paragraph = tester.RenderObject<RenderParagraph>(Find.ByType<RichText>());
        SemanticsNode original = Assert.Single(tester.GetSemantics(Find.ByType<RichText>()).Children);
        int updates = 0;
        void OnUpdate() => updates++;
        SemanticsOwner owner = tester.RenderView.Owner!.SemanticsOwner!;
        owner.AddListener(OnUpdate);
        try
        {
            paragraph.Text = new TextSpan("text", recognizer: hold);
            tester.Pump();
            Assert.Equal(1, updates);
            SemanticsNode changed = Assert.Single(tester.GetSemantics(Find.ByType<RichText>()).Children);
            Assert.Same(original, changed);
            Assert.Equal(SemanticsActions.LongPress, changed.Actions);
        }
        finally
        {
            owner.RemoveListener(OnUpdate);
        }
    }

    // text_test.dart: 'semantic nodes of offscreen recognizers are marked hidden'.
    [Fact]
    public void OffscreenRecognizerShowOnScreenScrollsItsRunIntoView()
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        using var tap = new TapGestureRecognizer { OnTap = () => { } };
        var controller = new ScrollController();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Align(alignment: Alignment.TopLeft,
            child: new SizedBox(width: 200, height: 100, child: new SingleChildScrollView(
                controller: controller,
                child: new RichText(text: new TextSpan(string.Concat(Enumerable.Repeat("line\n", 30)),
                    style: new TextStyle(FontSize: 10), children:
                    [
                        new TextSpan("target", recognizer: tap),
                    ])))))));
        SemanticsNode paragraph = tester.GetSemantics(Find.ByType<RichText>());
        SemanticsNode target = Assert.Single(paragraph.Children, child => child.Label == "target");
        Assert.True(target.IsHidden);
        Assert.Equal(0, controller.Offset);
        Assert.True(tester.RenderView.Owner!.SemanticsOwner!.PerformAction(target.Id, SemanticsActions.ShowOnScreen));
        tester.PumpAndSettle();
        Assert.True(controller.Offset > 0);
        Assert.False(target.IsHidden);
    }

    private static RenderParagraph Paragraph(InlineSpan text, List<RenderBox>? children = null)
    {
        var paragraph = new RenderParagraph(new TextSpan(style: new TextStyle(FontSize: 10), children: [text]),
            children);
        paragraph.Layout(new BoxConstraints(MaxWidth: 800));
        return paragraph;
    }

    private static SemanticsNode Assemble(RenderParagraph paragraph,
        IReadOnlyList<SemanticsNode>? children = null, Rect? clip = null)
    {
        var config = new SemanticsConfiguration();
        paragraph.InvokeDescribeSemanticsConfiguration(config);
        var node = new SemanticsNode { ParentPaintClipRect = clip };
        paragraph.InvokeAssembleSemanticsNode(node, config, children ?? []);
        return node;
    }

    private sealed class ShowOnScreenBox(RenderBox child) : RenderProxyBox(child)
    {
        public RenderObject? Descendant { get; private set; }
        public Rect? RequestedRect { get; private set; }

        public override void ShowOnScreen(RenderObject? descendant = null, Rect? rect = null,
            TimeSpan duration = default, Curve? curve = null)
        {
            Descendant = descendant;
            RequestedRect = rect;
        }
    }
}
