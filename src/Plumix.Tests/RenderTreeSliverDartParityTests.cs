using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.TreeSliverFixtures;

// Ports flutter/packages/flutter/test/rendering/sliver_tree_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RenderTreeSliverDartParityTests
{
    // Flutter: 'sliver_tree_test.dart (rendering): asserts proper axis directions'
    [DebugOnlyFact]
    public void AssertsProperAxisDirections()
    {
        using var tester = new FrameworkDartTester();
        var exceptions = new List<object>();
        FlutterExceptionHandler? oldHandler = FlutterError.OnError;
        FlutterError.OnError = details => exceptions.Add(details.Exception);
        try
        {
            (Axis Direction, bool Reverse)[] cases =
            [
                (Axis.Vertical, true), // AxisDirection.up
                (Axis.Horizontal, true), // AxisDirection.left
                (Axis.Horizontal, false), // AxisDirection.right
            ];
            foreach ((Axis direction, bool reverse) in cases)
            {
                tester.PumpWidget(BuildTestWidget(new CustomScrollView(
                    scrollDirection: direction,
                    reverse: reverse,
                    slivers: [new TreeSliver<string>(tree: SetUpNodes())])));

                Assert.NotEmpty(exceptions);
                Assert.Contains(
                    "TreeSliver is only supported in Viewports with an AxisDirection.down.",
                    exceptions[0].ToString(),
                    StringComparison.Ordinal);
                exceptions.Clear();
                tester.PumpWidget(new Container());
            }
        }
        finally
        {
            FlutterError.OnError = oldHandler;
        }
    }

    // Flutter: 'sliver_tree_test.dart (rendering): Basic layout'
    [Fact]
    public void BasicLayout()
    {
        using var tester = new FrameworkDartTester();
        Finder treeSliver = Find.ByType<TreeSliver<string>>();

        // Default layout, custom indentation values, row extents.
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers: [new TreeSliver<string>(tree: SetUpNodes())])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        ExpectRect(tester, "First", 46.0, 8.0, 286.0, 32.0);
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        ExpectRect(tester, "Third", 46.0, 88.0, 286.0, 112.0);
        ExpectRect(tester, "gamma", 56.0, 128.0, 296.0, 152.0);
        ExpectRect(tester, "delta", 56.0, 168.0, 296.0, 192.0);
        ExpectRect(tester, "epsilon", 56.0, 208.0, 392.0, 232.0);
        ExpectRect(tester, "Fourth", 46.0, 248.0, 334.0, 272.0);

        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers:
            [
                new TreeSliver<string>(tree: SetUpNodes(), indentation: TreeSliverIndentationType.None),
            ])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (46, 128), (46, 168), (46, 208), (46, 248)));
        ExpectRect(tester, "First", 46.0, 8.0, 286.0, 32.0);
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        ExpectRect(tester, "Third", 46.0, 88.0, 286.0, 112.0);
        ExpectRect(tester, "gamma", 46.0, 128.0, 286.0, 152.0);
        ExpectRect(tester, "delta", 46.0, 168.0, 286.0, 192.0);
        ExpectRect(tester, "epsilon", 46.0, 208.0, 382.0, 232.0);
        ExpectRect(tester, "Fourth", 46.0, 248.0, 334.0, 272.0);

        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers:
            [
                new TreeSliver<string>(
                    tree: SetUpNodes(),
                    indentation: TreeSliverIndentationType.Custom(50.0)),
            ])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (96, 128), (96, 168), (96, 208), (46, 248)));
        ExpectRect(tester, "First", 46.0, 8.0, 286.0, 32.0);
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        ExpectRect(tester, "Third", 46.0, 88.0, 286.0, 112.0);
        ExpectRect(tester, "gamma", 96.0, 128.0, 336.0, 152.0);
        ExpectRect(tester, "delta", 96.0, 168.0, 336.0, 192.0);
        ExpectRect(tester, "epsilon", 96.0, 208.0, 432.0, 232.0);
        ExpectRect(tester, "Fourth", 46.0, 248.0, 334.0, 272.0);

        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers:
            [
                new TreeSliver<string>(tree: SetUpNodes(), treeRowExtentBuilder: (_, _) => 100),
            ])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 26), null, (46, 126), null, (46, 226), (56, 326), (56, 426), (56, 526)));
        ExpectRect(tester, "First", 46.0, 26.0, 286.0, 74.0);
        ExpectRect(tester, "Second", 46.0, 126.0, 334.0, 174.0);
        ExpectRect(tester, "Third", 46.0, 226.0, 286.0, 274.0);
        ExpectRect(tester, "gamma", 56.0, 326.0, 296.0, 374.0);
        ExpectRect(tester, "delta", 56.0, 426.0, 296.0, 474.0);
        ExpectRect(tester, "epsilon", 56.0, 526.0, 392.0, 574.0);
        Finds.Nothing(Find.Text("Fourth"));
    }

    // Flutter: 'sliver_tree_test.dart (rendering): Animating node segment'
    [Fact]
    public void AnimatingNodeSegment()
    {
        using var tester = new FrameworkDartTester();
        Finder treeSliver = Find.ByType<TreeSliver<string>>();
        List<TreeSliverNode<string>> treeNodes = SetUpNodes();
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers: [new TreeSliver<string>(tree: treeNodes)])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        Finds.Nothing(Find.Text("alpha"));
        tester.Tap(Find.ByType<Icon>().First);
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs(
                (46, 8), null, (46, 48),
                (56, 8), // beta animating in
                (56, 48), // kappa animating in
                // Remaining are unchanged
                null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        // New nodes have been inserted into the tree, alpha is not visible yet.
        Finds.Nothing(Find.Text("alpha"));
        Finds.OneWidget(Find.Text("beta"));
        Finds.OneWidget(Find.Text("kappa"));
        ExpectRect(tester, "beta", 56.0, 8.0, 248.0, 32.0);
        ExpectRect(tester, "kappa", 56.0, 48.0, 296.0, 72.0);
        // Progress the animation.
        tester.Pump(TimeSpan.FromMilliseconds(50));
        PaintAssert.Paints(
            treeSliver,
            Paragraphs(
                (46, 8), null, (46, 48),
                null, // Icon of alpha animating in
                (56, 8), // alpha animating in
                (56, 48), // beta animating in
                (56, 88), // kappa animating in
                // Remaining are unchanged
                null, (46, 128), (56, 168), (56, 208), (56, 248), (46, 288)));
        Assert.Equal(8.0, Math.Floor(tester.GetRect(Find.Text("alpha")).Top));
        Assert.Equal(48.0, Math.Floor(tester.GetRect(Find.Text("beta")).Top));
        Assert.Equal(88.0, Math.Floor(tester.GetRect(Find.Text("kappa")).Top));
        // Complete the animation
        tester.PumpAndSettle();
        PaintAssert.Paints(treeSliver, SettledExpandedSecond());
        // Time to animate
        ExpectRect(tester, "alpha", 56.0, 88.0, 296.0, 112.0);
        ExpectRect(tester, "beta", 56.0, 128.0, 248.0, 152.0);
        ExpectRect(tester, "kappa", 56.0, 168.0, 296.0, 192.0);

        // Customize the animation
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers:
            [
                new TreeSliver<string>(
                    tree: treeNodes,
                    toggleAnimationStyle: new AnimationStyle(
                        Duration: TimeSpan.FromMilliseconds(500),
                        Curve: Curves.BounceIn)),
            ])));
        tester.Pump();
        PaintAssert.Paints(treeSliver, SettledExpandedSecond());
        // Still visible from earlier.
        ExpectRect(tester, "alpha", 56.0, 88.0, 296.0, 112.0);
        // Collapse the node now
        tester.Tap(Find.ByType<Icon>().First);
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Finds.OneWidget(Find.Text("alpha"));
        Finds.OneWidget(Find.Text("beta"));
        Finds.OneWidget(Find.Text("kappa"));
        Assert.Equal(-22.0, Math.Floor(tester.GetRect(Find.Text("alpha")).Top));
        Assert.Equal(18.0, Math.Floor(tester.GetRect(Find.Text("beta")).Top));
        Assert.Equal(58.0, Math.Floor(tester.GetRect(Find.Text("kappa")).Top));
        // Progress the animation.
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(-25.0, Math.Floor(tester.GetRect(Find.Text("alpha")).Top));
        Assert.Equal(15.0, Math.Floor(tester.GetRect(Find.Text("beta")).Top));
        Assert.Equal(55.0, Math.Floor(tester.GetRect(Find.Text("kappa")).Top));
        // Complete the animation
        tester.PumpAndSettle();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        Finds.Nothing(Find.Text("alpha"));

        // Disable the animation
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers:
            [
                new TreeSliver<string>(tree: treeNodes, toggleAnimationStyle: AnimationStyle.NoAnimation),
            ])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        // Not in the tree.
        Finds.Nothing(Find.Text("alpha"));
        // Collapse the node now
        tester.Tap(Find.ByType<Icon>().First);
        tester.Pump();
        // No animating, straight to positions.
        PaintAssert.Paints(treeSliver, SettledExpandedSecond());
        // Still visible from earlier.
        ExpectRect(tester, "alpha", 56.0, 88.0, 296.0, 112.0);
        ExpectRect(tester, "beta", 56.0, 128.0, 248.0, 152.0);
        ExpectRect(tester, "kappa", 56.0, 168.0, 296.0, 192.0);
    }

    // Flutter: 'sliver_tree_test.dart (rendering): Multiple animating node segments'
    [Fact]
    public void MultipleAnimatingNodeSegments()
    {
        using var tester = new FrameworkDartTester();
        Finder treeSliver = Find.ByType<TreeSliver<string>>();
        List<TreeSliverNode<string>> treeNodes = SetUpNodes();
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            slivers: [new TreeSliver<string>(tree: treeNodes)])));
        tester.Pump();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        Finds.Nothing(Find.Text("alpha")); // Second is collapsed
        Finds.OneWidget(Find.Text("gamma")); // Third is expanded

        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        ExpectRect(tester, "Third", 46.0, 88.0, 286.0, 112.0);
        ExpectRect(tester, "gamma", 56.0, 128.0, 296.0, 152.0);

        // Trigger two animations to run together.
        // Collapse Third
        tester.Tap(Find.ByType<Icon>().Last);
        // Expand Second
        tester.Tap(Find.ByType<Icon>().First);
        tester.Pump(TimeSpan.FromMilliseconds(15));
        PaintAssert.Paints(
            treeSliver,
            Paragraphs(
                (46, 8), null, (46, 48),
                (56, 8), // beta animating in
                (56, 48), // kappa animating in
                null, (46, 88), (56, 128), (56, 168), (56, 208), (46, 248)));
        // Third is collapsing
        ExpectRect(tester, "Third", 46.0, 88.0, 286.0, 112.0);
        ExpectRect(tester, "gamma", 56.0, 128.0, 296.0, 152.0);
        // Second is expanding
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        // beta has been added and is animating into view.
        Assert.Equal(8.0, Math.Floor(tester.GetRect(Find.Text("beta")).Top));
        tester.Pump(TimeSpan.FromMilliseconds(15));
        // Third is still collapsing. Third is sliding down as Seconds's children slide in, gamma is still
        // exiting.
        PaintAssert.Paints(
            treeSliver,
            Paragraphs(
                (46, 8), null, (46, 48), null,
                (56, -20), // alpha animating in
                (56, 20), // beta animating in
                (56, 60), // kappa animating in
                null,
                (46, 100), // Third animating down
                (56, 128), (56, 168), (56, 208), (46, 248)));
        // Third is collapsing
        Assert.Equal(100.0, Math.Floor(tester.GetRect(Find.Text("Third")).Top));
        ExpectRect(tester, "gamma", 56.0, 128.0, 296.0, 152.0);
        // Second is expanding
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        // alpha has been added and is animating into view.
        Assert.Equal(-20.0, Math.Floor(tester.GetRect(Find.Text("alpha")).Top));
        tester.Pump(TimeSpan.FromMilliseconds(15));
        // Third is still collapsing. Third is sliding down as Seconds's children slide in, gamma is still
        // exiting.
        PaintAssert.Paints(
            treeSliver,
            Paragraphs(
                (46, 8), null, (46, 48), null,
                (56, -8), // alpha animating in
                (56, 32), // beta animating in
                (56, 72), // kappa animating in
                null,
                (46, 112), // Third animating down
                (56, 128), (56, 168), (56, 208), (46, 248)));
        // Third is collapsing
        Assert.Equal(112.0, Math.Floor(tester.GetRect(Find.Text("Third")).Top));
        ExpectRect(tester, "gamma", 56.0, 128.0, 296.0, 152.0);
        // Second is expanding
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        // alpha is still animating into view.
        Assert.Equal(-8.0, Math.Floor(tester.GetRect(Find.Text("alpha")).Top));
        // Complete the animations
        tester.PumpAndSettle();
        PaintAssert.Paints(
            treeSliver,
            Paragraphs((46, 8), null, (46, 48), null, (56, 88), (56, 128), (56, 168), null, (46, 208), (46, 248)));
        ExpectRect(tester, "Third", 46.0, 208.0, 286.0, 232.0);
        // gamma has left the building
        Finds.Nothing(Find.Text("gamma"));
        ExpectRect(tester, "Second", 46.0, 48.0, 334.0, 72.0);
        // alpha is in place.
        ExpectRect(tester, "alpha", 56.0, 88.0, 296.0, 112.0);
    }

    // Flutter: 'sliver_tree_test.dart (rendering): only paints visible rows'
    [Fact]
    public void OnlyPaintsVisibleRows()
    {
        using var tester = new FrameworkDartTester();
        var scrollController = new ScrollController();
        List<TreeSliverNode<string>> treeNodes = SetUpNodes();
        tester.PumpWidget(BuildTestWidget(new CustomScrollView(
            controller: scrollController,
            slivers: [new TreeSliver<string>(treeRowExtentBuilder: (_, _) => 200, tree: treeNodes)])));
        tester.Pump();

        Assert.Equal(0.0, scrollController.Position.Pixels);
        Assert.Equal(800.0, scrollController.Position.MaxScrollExtent);
        bool rowNeedsPaint(string row) => tester.RenderObject<RenderParagraph>(Find.Text(row)).DebugNeedsPaint;

        Assert.False(rowNeedsPaint("First"));
        Assert.False(rowNeedsPaint("Second"));
        Assert.False(rowNeedsPaint("Third"));
        Finds.Nothing(Find.Text("gamma")); // Not visible

        // Change the scroll offset
        scrollController.JumpTo(200);
        tester.Pump();
        Finds.Nothing(Find.Text("First"));
        Assert.False(rowNeedsPaint("Second"));
        Assert.False(rowNeedsPaint("Third"));
        Assert.False(rowNeedsPaint("gamma")); // Now visible
        scrollController.Dispose();
    }
}

/// <summary>Fixtures shared by the widgets and rendering <c>sliver_tree_test.dart</c> ports.</summary>
internal static class TreeSliverFixtures
{
    // rendering/sliver_tree_test.dart: _setUpNodes.
    internal static List<TreeSliverNode<string>> SetUpNodes() =>
    [
        new("First"),
        new(
            "Second",
            children:
            [
                new("alpha", children: [new("uno"), new("dos"), new("tres")]),
                new("beta"),
                new("kappa"),
            ]),
        new("Third", expanded: true, children: [new("gamma"), new("delta"), new("epsilon")]),
        new("Fourth"),
    ];

    // rendering/sliver_tree_test.dart: _buildTestWidget.
    internal static Widget BuildTestWidget(Widget child) =>
        new Directionality(
            TextDirection.Ltr,
            new DefaultTextStyle(
                new TextStyle(
                    Color: new Color(0xFF000000),
                    FontFamily: new FontFamily("monospace"),
                    FontSize: 48.0,
                    FontWeight: FontWeight.Black,
                    Decoration: Plumix.UI.TextDecoration.Underline,
                    DecorationColor: new Color(0xFFFFFF00),
                    DecorationStyle: TextDecorationStyle.Double,
                    DebugLabel: "fallback style"),
                child));

    // The rendering tests' settled paint sequence once 'Second' is expanded.
    internal static PaintPattern SettledExpandedSecond() =>
        Paragraphs(
            (46, 8), null, (46, 48), null,
            (56, 88), // alpha
            (56, 128), // beta
            (56, 168), // kappa
            null, (46, 208), (56, 248), (56, 288), (56, 328), (46, 368));

    // `paints..paragraph(offset: ...)..paragraph()...`: a null entry matches any paragraph (an icon).
    internal static PaintPattern Paragraphs(params (double X, double Y)?[] offsets)
    {
        PaintPattern pattern = PaintPattern.Paints;
        foreach ((double X, double Y)? offset in offsets)
        {
            pattern = offset is { } point ? pattern.Paragraph(new Point(point.X, point.Y)) : pattern.Paragraph();
        }

        return pattern;
    }

    internal static void ExpectRect(
        FrameworkDartTester tester,
        string text,
        double left,
        double top,
        double right,
        double bottom)
    {
        Assert.Equal(new Rect(left, top, right - left, bottom - top), tester.GetRect(Find.Text(text)));
    }
}
