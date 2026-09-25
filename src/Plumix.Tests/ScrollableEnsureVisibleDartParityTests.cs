using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/ensure_visible_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ScrollableEnsureVisibleDartParityTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(1020);

    // Dart's findKey(i): find.byKey(ValueKey<int>(i), skipOffstage: false).
    private static Element FindKey(FrameworkDartTester tester, int i) =>
        tester.ElementsWithKey(new ValueKey<int>(i)).Single();

    private static Element FindKey(FrameworkDartTester tester, string coordinate) =>
        tester.ElementsWithKey(new ValueKey<string>(coordinate)).Single();

    private static Element FindKey(FrameworkDartTester tester, ChildVicinity vicinity) =>
        tester.ElementsWithKey(new ValueKey<ChildVicinity>(vicinity)).Single();

    // Dart's tester.getBottomLeft.
    private static Point GetBottomLeft(FrameworkDartTester tester, Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(0.0, box.Size.Height));
    }

    private static void EnsureVisible(
        FrameworkDartTester tester,
        int i,
        double alignment = 0.0,
        TimeSpan? duration = null,
        ScrollPositionAlignmentPolicy alignmentPolicy = ScrollPositionAlignmentPolicy.Explicit)
    {
        _ = Scrollable.EnsureVisible(
            FindKey(tester, i),
            alignment: alignment,
            duration: duration,
            alignmentPolicy: alignmentPolicy);
    }

    private static void Prepare(FrameworkDartTester tester, double offset)
    {
        tester.State<ScrollableState>().Position.JumpTo(offset);
        tester.Pump();
    }

    private static Widget BuildSingleChildScrollView(Axis scrollDirection, bool reverse = false)
    {
        return new Directionality(
            TextDirection.Ltr,
            new Center(
                child: new SizedBox(
                    width: 600.0,
                    height: 400.0,
                    child: new SingleChildScrollView(
                        scrollDirection: scrollDirection,
                        reverse: reverse,
                        child: new ListBody(
                            mainAxis: scrollDirection,
                            children: KeyedBoxes(0, 7))))));
    }

    private static Widget BuildListView(Axis scrollDirection, bool reverse = false, bool shrinkWrap = false)
    {
        return new Directionality(
            TextDirection.Ltr,
            new Center(
                child: new SizedBox(
                    width: 600.0,
                    height: 400.0,
                    child: new ListView(
                        scrollDirection: scrollDirection,
                        reverse: reverse,
                        addSemanticIndexes: false,
                        shrinkWrap: shrinkWrap,
                        children: KeyedBoxes(0, 7)))));
    }

    private static List<Widget> KeyedBoxes(int start, int count)
    {
        var children = new List<Widget>(count);
        for (int i = start; i < start + count; i++)
        {
            children.Add(new SizedBox(key: new ValueKey<int>(i), width: 200.0, height: 200.0));
        }

        return children;
    }

    private static List<Widget> RotatedChildren()
    {
        return
            [
                new SizedBox(height: 200.0),
                new SizedBox(height: 200.0),
                new SizedBox(height: 200.0),
                new SizedBox(
                    height: 200.0,
                    child: new Center(
                        child: new Plumix.Widgets.Transform(
                            Matrix4.RotationZ(Math.PI),
                            child: new Container(
                                key: new ValueKey<int>(0),
                                width: 100.0,
                                height: 100.0,
                                color: new Color(0xFFFFFFFF))))),
                new SizedBox(height: 200.0),
                new SizedBox(height: 200.0),
                new SizedBox(height: 200.0),
            ];
    }

    // two_dimensional_utils.dart: simpleBuilderTest.
    private static Widget SimpleBuilderTest(
        ScrollableDetails? verticalDetails = null,
        ScrollableDetails? horizontalDetails = null,
        bool useCacheExtent = false)
    {
        return new TestWidgetsApp(
            home: new Align(
                alignment: Alignment.Center,
                child: new SimpleBuilderTableView(
                    TwoDimensionalHarness.BuilderDelegate(),
                    mainAxis: Axis.Vertical,
                    verticalDetails: verticalDetails ?? ScrollableDetails.Vertical(),
                    horizontalDetails: horizontalDetails ?? ScrollableDetails.Horizontal(),
                    useCacheExtent: useCacheExtent,
                    diagonalDragBehavior: DiagonalDragBehavior.None,
                    clipBehavior: Clip.HardEdge)));
    }

    // Flutter: 'SingleChildScrollView SingleChildScrollView ensureVisible Axis.vertical'
    [Fact]
    public void SingleChildScrollViewEnsureVisibleAxisVertical()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildSingleChildScrollView(Axis.Vertical));

        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);

        EnsureVisible(tester, 6);
        tester.Pump();
        Assert.Equal(300.0, tester.GetTopLeft(FindKey(tester, 6)).Y);

        EnsureVisible(tester, 4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 4)).Y);

        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 0)).Y);

        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);
    }

    // Flutter: 'SingleChildScrollView SingleChildScrollView ensureVisible Axis.horizontal'
    [Fact]
    public void SingleChildScrollViewEnsureVisibleAxisHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildSingleChildScrollView(Axis.Horizontal));

        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).X);

        EnsureVisible(tester, 6);
        tester.Pump();
        Assert.Equal(500.0, tester.GetTopLeft(FindKey(tester, 6)).X);

        EnsureVisible(tester, 4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 4)).X);

        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 0)).X);

        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).X);
    }

    // Flutter: 'SingleChildScrollView SingleChildScrollView ensureVisible Axis.vertical reverse'
    [Fact]
    public void SingleChildScrollViewEnsureVisibleAxisVerticalReverse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildSingleChildScrollView(Axis.Vertical, reverse: true));

        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 3)).Y);

        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.Equal(300.0, tester.GetBottomRight(FindKey(tester, 0)).Y);

        EnsureVisible(tester, 2, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 2)).Y);

        EnsureVisible(tester, 6, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 6)).Y);

        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 3)).Y);

        // Regression test for https://github.com/flutter/flutter/issues/128749
        // Reset to zero position.
        tester.State<ScrollableState>().Position.JumpTo(0.0);
        tester.Pump();
        // 4 is not currently visible as the SingleChildScrollView is contained
        // within a centered SizedBox.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 4)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).Y);
        EnsureVisible(tester, 6, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 5, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // 5 and 6 are already visible beyond the top edge, so no change.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 4)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).Y);
        EnsureVisible(tester, 4, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // Since it is reversed, 4 should have come into view at the top
        // edge of the scrollable, matching the alignment expectation.
        Assert.Equal(300.0, GetBottomLeft(tester, FindKey(tester, 4)).Y);
        Assert.Equal(700.0, GetBottomLeft(tester, FindKey(tester, 6)).Y);

        // Bring 6 back into view at the trailing edge, checking the other
        // alignment.
        EnsureVisible(tester, 6, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 4)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).Y);
    }

    // Flutter: 'SingleChildScrollView SingleChildScrollView ensureVisible Axis.horizontal reverse'
    [Fact]
    public void SingleChildScrollViewEnsureVisibleAxisHorizontalReverse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildSingleChildScrollView(Axis.Horizontal, reverse: true));

        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 3)).X);

        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.Equal(300.0, tester.GetBottomRight(FindKey(tester, 0)).X);

        EnsureVisible(tester, 2, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 2)).X);

        EnsureVisible(tester, 6, alignment: 1.0);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 6)).X);

        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 3)).X);

        // Regression test for https://github.com/flutter/flutter/issues/128749
        // Reset to zero position.
        tester.State<ScrollableState>().Position.JumpTo(0.0);
        tester.Pump();
        // 4 is not currently visible as the SingleChildScrollView is contained
        // within a centered SizedBox.
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).X);
        EnsureVisible(tester, 6, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 5, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 4, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // 4, 5 and 6 are already visible beyond the left edge, so no change.
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).X);
        EnsureVisible(tester, 3, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // Since it is reversed, 3 should have come into view at the leading
        // edge of the scrollable, matching the alignment expectation.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(700.0, GetBottomLeft(tester, FindKey(tester, 6)).X);

        // Bring 6 back into view at the trailing edge, checking the other
        // alignment.
        EnsureVisible(tester, 6, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 6)).X);
    }

    // Flutter: 'SingleChildScrollView SingleChildScrollView ensureVisible rotated child'
    [Fact]
    public void SingleChildScrollViewEnsureVisibleRotatedChild()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(
            new Center(
                child: new SizedBox(
                    width: 600.0,
                    height: 400.0,
                    child: new SingleChildScrollView(child: new ListBody(children: RotatedChildren())))));

        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.InRange(tester.GetBottomRight(FindKey(tester, 0)).Y, 100.0 - 0.1, 100.0 + 0.1);

        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.InRange(tester.GetTopLeft(FindKey(tester, 0)).Y, 500.0 - 0.1, 500.0 + 0.1);
    }

    // Flutter: 'SingleChildScrollView Nested SingleChildScrollView ensureVisible behavior test'
    [Fact]
    public void NestedSingleChildScrollViewEnsureVisibleBehaviorTest()
    {
        // Regressing test for https://github.com/flutter/flutter/issues/65100
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);

        void EnsureVisibleAt(string coordinate, double alignment = 0.0) =>
            _ = Scrollable.EnsureVisible(FindKey(tester, coordinate), alignment: alignment);

        Point TopLeft(string coordinate) => tester.GetTopLeft(FindKey(tester, coordinate));

        var rows = new List<Widget>(7);
        for (int y = 0; y < 7; y++)
        {
            var cells = new List<Widget>(7);
            for (int x = 0; x < 7; x++)
            {
                cells.Add(new SizedBox(key: new ValueKey<string>($"{x}, {y}"), width: 200.0, height: 200.0));
            }

            rows.Add(new Row(children: cells));
        }

        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        width: 600.0,
                        height: 400.0,
                        child: new SingleChildScrollView(
                            scrollDirection: Axis.Horizontal,
                            child: new SingleChildScrollView(child: new Column(children: rows)))))));

        //      Items: 7 * 7 Container(width: 200.0, height: 200.0)
        //      viewport: Size(width: 600.0, height: 400.0)
        //
        //               0                       600
        //                 +----------------------+
        //                 |0,0    |1,0    |2,0   |
        //                 |       |       |      |
        //                 +----------------------+
        //                 |0,1    |1,1    |2,1   |
        //                 |       |       |      |
        //             400 +----------------------+

        EnsureVisibleAt("0, 0");
        tester.Pump();
        Assert.Equal(new Point(100.0, 100.0), TopLeft("0, 0"));

        EnsureVisibleAt("3, 0");
        tester.Pump();
        Assert.Equal(new Point(100.0, 100.0), TopLeft("3, 0"));

        EnsureVisibleAt("3, 0", alignment: 0.5);
        tester.Pump();
        Assert.Equal(new Point(300.0, 100.0), TopLeft("3, 0"));

        EnsureVisibleAt("6, 0");
        tester.Pump();
        Assert.Equal(new Point(500.0, 100.0), TopLeft("6, 0"));

        EnsureVisibleAt("0, 2");
        tester.Pump();
        Assert.Equal(new Point(100.0, 100.0), TopLeft("0, 2"));

        EnsureVisibleAt("3, 2");
        tester.Pump();
        Assert.Equal(new Point(100.0, 100.0), TopLeft("3, 2"));

        // It should be at the center of the screen.
        EnsureVisibleAt("3, 2", alignment: 0.5);
        tester.Pump();
        Assert.Equal(new Point(300.0, 200.0), TopLeft("3, 2"));
    }

    private static void RunListViewEnsureVisibleAxisVertical(bool shrinkWrap)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildListView(Axis.Vertical, shrinkWrap: shrinkWrap));

        Prepare(tester, 480.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);

        Prepare(tester, 1083.0);
        EnsureVisible(tester, 6);
        tester.Pump();
        Assert.Equal(300.0, tester.GetTopLeft(FindKey(tester, 6)).Y);

        Prepare(tester, 735.0);
        EnsureVisible(tester, 4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 4)).Y);

        Prepare(tester, 123.0);
        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 0)).Y);

        Prepare(tester, 523.0);
        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);
    }

    private static void RunListViewEnsureVisibleAxisHorizontal(bool shrinkWrap)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildListView(Axis.Horizontal, shrinkWrap: shrinkWrap));

        Prepare(tester, 23.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).X);

        Prepare(tester, 843.0);
        EnsureVisible(tester, 6);
        tester.Pump();
        Assert.Equal(500.0, tester.GetTopLeft(FindKey(tester, 6)).X);

        Prepare(tester, 415.0);
        EnsureVisible(tester, 4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 4)).X);

        Prepare(tester, 46.0);
        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 0)).X);

        Prepare(tester, 211.0);
        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).X);
    }

    private static void RunListViewEnsureVisibleAxisVerticalReverse(bool shrinkWrap)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildListView(Axis.Vertical, reverse: true, shrinkWrap: shrinkWrap));

        Prepare(tester, 211.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 3)).Y);

        Prepare(tester, 23.0);
        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 0)).Y);

        Prepare(tester, 230.0);
        EnsureVisible(tester, 2, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 2)).Y);

        Prepare(tester, 1083.0);
        EnsureVisible(tester, 6, alignment: 1.0);
        tester.Pump();
        Assert.Equal(300.0, tester.GetBottomRight(FindKey(tester, 6)).Y);

        Prepare(tester, 345.0);
        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 3)).Y);

        // Regression test for https://github.com/flutter/flutter/issues/128749
        // Reset to zero position.
        Prepare(tester, 0.0);
        // 2 is not currently visible as the ListView is contained
        // within a centered SizedBox.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 2)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).Y);
        EnsureVisible(tester, 0, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 1, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // 0 and 1 are already visible beyond the top edge, so no change.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 2)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).Y);
        EnsureVisible(tester, 2, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // Since it is reversed, 2 should have come into view at the top
        // edge of the scrollable, matching the alignment expectation.
        Assert.Equal(300.0, GetBottomLeft(tester, FindKey(tester, 2)).Y);
        Assert.Equal(700.0, GetBottomLeft(tester, FindKey(tester, 0)).Y);

        // Bring 0 back into view at the trailing edge, checking the other
        // alignment.
        EnsureVisible(tester, 0, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 2)).Y);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).Y);
    }

    private static void RunListViewEnsureVisibleAxisHorizontalReverse(bool shrinkWrap)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(BuildListView(Axis.Horizontal, reverse: true, shrinkWrap: shrinkWrap));

        Prepare(tester, 211.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 3)).X);

        Prepare(tester, 23.0);
        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 0)).X);

        Prepare(tester, 230.0);
        EnsureVisible(tester, 2, alignment: 1.0);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 2)).X);

        Prepare(tester, 1083.0);
        EnsureVisible(tester, 6, alignment: 1.0);
        tester.Pump();
        Assert.Equal(300.0, tester.GetBottomRight(FindKey(tester, 6)).X);

        Prepare(tester, 345.0);
        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(700.0, tester.GetBottomRight(FindKey(tester, 3)).X);

        // Regression test for https://github.com/flutter/flutter/issues/128749
        // Reset to zero position.
        Prepare(tester, 0.0);
        // 3 is not currently visible as the ListView is contained
        // within a centered SizedBox.
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).X);
        EnsureVisible(tester, 0, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 1, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        EnsureVisible(tester, 2, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // 0, 1 and 2 are already visible beyond the left edge, so no change.
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).X);
        EnsureVisible(tester, 3, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtStart);
        tester.Pump();
        // Since it is reversed, 3 should have come into view at the leading
        // edge of the scrollable, matching the alignment expectation.
        Assert.Equal(100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(700.0, GetBottomLeft(tester, FindKey(tester, 0)).X);

        // Bring 0 back into view at the trailing edge, checking the other
        // alignment.
        EnsureVisible(tester, 0, alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        Assert.Equal(-100.0, GetBottomLeft(tester, FindKey(tester, 3)).X);
        Assert.Equal(500.0, GetBottomLeft(tester, FindKey(tester, 0)).X);
    }

    // Flutter: 'ListView ListView ensureVisible Axis.vertical'
    [Fact]
    public void ListViewEnsureVisibleAxisVertical() => RunListViewEnsureVisibleAxisVertical(shrinkWrap: false);

    // Flutter: 'ListView ListView ensureVisible Axis.horizontal'
    [Fact]
    public void ListViewEnsureVisibleAxisHorizontal() => RunListViewEnsureVisibleAxisHorizontal(shrinkWrap: false);

    // Flutter: 'ListView ListView ensureVisible Axis.vertical reverse'
    [Fact]
    public void ListViewEnsureVisibleAxisVerticalReverse() =>
        RunListViewEnsureVisibleAxisVerticalReverse(shrinkWrap: false);

    // Flutter: 'ListView ListView ensureVisible Axis.horizontal reverse'
    [Fact]
    public void ListViewEnsureVisibleAxisHorizontalReverse() =>
        RunListViewEnsureVisibleAxisHorizontalReverse(shrinkWrap: false);

    // Flutter: 'ListView ListView ensureVisible negative child'
    [Fact]
    public void ListViewEnsureVisibleNegativeChild()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);

        double GetOffset() => tester.State<ScrollableState>().Position.Pixels;

        static Widget BuildSliver(int i) =>
            new SliverToBoxAdapter(key: new ValueKey<int>(i), child: new SizedBox(width: 200.0, height: 200.0));

        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        width: 600.0,
                        height: 400.0,
                        child: new Scrollable(
                            viewportBuilder: (_, offset) => new Viewport(
                                offset: offset,
                                center: new ValueKey<int>(4),
                                slivers:
                                [
                                    BuildSliver(0),
                                    BuildSliver(1),
                                    BuildSliver(2),
                                    BuildSliver(3),
                                    BuildSliver(4),
                                    BuildSliver(5),
                                    BuildSliver(6),
                                ]))))));

        Prepare(tester, -125.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(-200.0, GetOffset());

        Prepare(tester, -225.0);
        EnsureVisible(tester, 2);
        tester.Pump();
        Assert.Equal(-400.0, GetOffset());
    }

    // Flutter: 'ListView ListView ensureVisible rotated child'
    [Fact]
    public void ListViewEnsureVisibleRotatedChild()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        width: 600.0,
                        height: 400.0,
                        child: new ListView(children: RotatedChildren())))));

        Prepare(tester, 321.0);
        EnsureVisible(tester, 0);
        tester.Pump();
        Assert.InRange(tester.GetBottomRight(FindKey(tester, 0)).Y, 100.0 - 0.1, 100.0 + 0.1);

        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.InRange(tester.GetTopLeft(FindKey(tester, 0)).Y, 500.0 - 0.1, 500.0 + 0.1);
    }

    // Flutter: 'ListView shrinkWrap ListView ensureVisible Axis.vertical'
    [Fact]
    public void ShrinkWrapListViewEnsureVisibleAxisVertical() => RunListViewEnsureVisibleAxisVertical(shrinkWrap: true);

    // Flutter: 'ListView shrinkWrap ListView ensureVisible Axis.horizontal'
    [Fact]
    public void ShrinkWrapListViewEnsureVisibleAxisHorizontal() =>
        RunListViewEnsureVisibleAxisHorizontal(shrinkWrap: true);

    // Flutter: 'ListView shrinkWrap ListView ensureVisible Axis.vertical reverse'
    [Fact]
    public void ShrinkWrapListViewEnsureVisibleAxisVerticalReverse() =>
        RunListViewEnsureVisibleAxisVerticalReverse(shrinkWrap: true);

    // Flutter: 'ListView shrinkWrap ListView ensureVisible Axis.horizontal reverse'
    [Fact]
    public void ShrinkWrapListViewEnsureVisibleAxisHorizontalReverse() =>
        RunListViewEnsureVisibleAxisHorizontalReverse(shrinkWrap: true);

    // Flutter: 'Scrollable with center ensureVisible'
    [Fact]
    public void ScrollableWithCenterEnsureVisible()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        var slivers = new List<Widget>();
        for (int i = -6; i <= 6; i++)
        {
            slivers.Add(new SliverToBoxAdapter(
                key: i == 0 ? new ValueKey<string>("center") : null,
                child: new SizedBox(key: new ValueKey<int>(i), width: 200.0, height: 200.0)));
        }

        tester.PumpWidget(
            new Directionality(
                TextDirection.Ltr,
                new Center(
                    child: new SizedBox(
                        width: 600.0,
                        height: 400.0,
                        child: new Scrollable(
                            viewportBuilder: (_, offset) => new Viewport(
                                offset: offset,
                                center: new ValueKey<string>("center"),
                                slivers: slivers))))));

        Prepare(tester, 480.0);
        EnsureVisible(tester, 3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);

        Prepare(tester, 1083.0);
        EnsureVisible(tester, 6);
        tester.Pump();
        Assert.Equal(300.0, tester.GetTopLeft(FindKey(tester, 6)).Y);

        Prepare(tester, 735.0);
        EnsureVisible(tester, 4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 4)).Y);

        Prepare(tester, 123.0);
        EnsureVisible(tester, 0, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, 0)).Y);

        Prepare(tester, 523.0);
        EnsureVisible(tester, 3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, 3)).Y);

        Prepare(tester, -480.0);
        EnsureVisible(tester, -3);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, -3)).Y);

        Prepare(tester, -1083.0);
        EnsureVisible(tester, -6);
        tester.Pump();
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, -6)).Y);

        Prepare(tester, -735.0);
        EnsureVisible(tester, -4, alignment: 1.0);
        tester.Pump();
        Assert.Equal(500.0, tester.GetBottomRight(FindKey(tester, -4)).Y);

        Prepare(tester, -523.0);
        EnsureVisible(tester, -3, duration: OneSecond);
        tester.Pump();
        tester.Pump(Settle);
        Assert.Equal(100.0, tester.GetTopLeft(FindKey(tester, -3)).Y);
    }

    private static void EnsureVisible(
        FrameworkDartTester tester,
        ChildVicinity vicinity,
        double alignment = 0.0,
        ScrollPositionAlignmentPolicy alignmentPolicy = ScrollPositionAlignmentPolicy.Explicit)
    {
        _ = Scrollable.EnsureVisible(
            FindKey(tester, vicinity),
            alignment: alignment,
            alignmentPolicy: alignmentPolicy);
    }

    private static Rect Ltrb(double left, double top, double right, double bottom) =>
        new(new Point(left, top), new Point(right, bottom));

    // Flutter: 'TwoDimensionalViewport ensureVisible Axis.vertical'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleAxisVertical()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(SimpleBuilderTest(useCacheExtent: true));

        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 0));
        tester.Pump();
        Assert.Equal(0.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 0))).Y);
        // (0, 3) is in the cache extent, and will be brought into view next
        Assert.Equal(600.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 3));
        tester.Pump();
        // Now in view at top edge of viewport
        Assert.Equal(0.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);

        // If already visible, no change
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 3));
        tester.Pump();
        Assert.Equal(0.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);
    }

    // Flutter: 'TwoDimensionalViewport ensureVisible Axis.horizontal'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleAxisHorizontal()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(SimpleBuilderTest(useCacheExtent: true));

        EnsureVisible(tester, new ChildVicinity(xIndex: 1, yIndex: 0));
        tester.Pump();
        Assert.Equal(0.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 1, yIndex: 0))).X);
        // (5, 0) is now in the cache extent, and will be brought into view next
        Assert.Equal(800.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 0))).X);
        EnsureVisible(
            tester,
            new ChildVicinity(xIndex: 5, yIndex: 0),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        // Now in view at trailing edge of viewport
        Assert.Equal(600.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 0))).X);

        // If already in position, no change
        EnsureVisible(
            tester,
            new ChildVicinity(xIndex: 5, yIndex: 0),
            alignmentPolicy: ScrollPositionAlignmentPolicy.KeepVisibleAtEnd);
        tester.Pump();
        Assert.Equal(600.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 0))).X);
    }

    // Flutter: 'TwoDimensionalViewport ensureVisible both axes'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleBothAxes()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(SimpleBuilderTest(useCacheExtent: true));

        EnsureVisible(tester, new ChildVicinity(xIndex: 1, yIndex: 1));
        tester.Pump();
        Assert.Equal(
            Ltrb(0.0, 0.0, 200.0, 200.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 1, yIndex: 1))));
        // (5, 4) is in the cache extent, and will be brought into view next
        Assert.Equal(
            Ltrb(800.0, 600.0, 1000.0, 800.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));
        EnsureVisible(
            tester,
            new ChildVicinity(xIndex: 5, yIndex: 4),
            alignment: 1.0); // Same as ScrollAlignmentPolicy.keepVisibleAtEnd
        tester.Pump();
        // Now in view at bottom trailing corner of viewport
        Assert.Equal(
            Ltrb(600.0, 400.0, 800.0, 600.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));

        // If already visible, no change
        EnsureVisible(tester, new ChildVicinity(xIndex: 5, yIndex: 4), alignment: 1.0);
        tester.Pump();
        Assert.Equal(
            Ltrb(600.0, 400.0, 800.0, 600.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));
    }

    // Flutter: 'TwoDimensionalViewport ensureVisible Axis.vertical reverse'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleAxisVerticalReverse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(
            SimpleBuilderTest(
                verticalDetails: ScrollableDetails.Vertical(reverse: true),
                useCacheExtent: true));

        Assert.Equal(400.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 0))).Y);
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 0));
        tester.Pump();
        // Already visible so no change.
        Assert.Equal(400.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 0))).Y);
        // (0, 3) is in the cache extent, and will be brought into view next
        Assert.Equal(-200.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 3));
        tester.Pump();
        // Now in view at bottom edge of viewport since we are reversed
        Assert.Equal(400.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);

        // If already visible, no change
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 3));
        tester.Pump();
        Assert.Equal(400.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 3))).Y);
    }

    // Flutter: 'TwoDimensionalViewport ensureVisible Axis.horizontal reverse'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleAxisHorizontalReverse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(
            SimpleBuilderTest(
                horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
                useCacheExtent: true));

        Assert.Equal(600.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 0))).X);
        EnsureVisible(tester, new ChildVicinity(xIndex: 0, yIndex: 0));
        tester.Pump();
        // Already visible so no change.
        Assert.Equal(600.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 0, yIndex: 0))).X);
        // (4, 0) is in the cache extent, and will be brought into view next
        Assert.Equal(-200.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 4, yIndex: 0))).X);
        EnsureVisible(tester, new ChildVicinity(xIndex: 4, yIndex: 0));
        tester.Pump();
        Assert.Equal(200.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 4, yIndex: 0))).X);

        // If already visible, no change
        EnsureVisible(tester, new ChildVicinity(xIndex: 4, yIndex: 0));
        tester.Pump();
        Assert.Equal(200.0, tester.GetTopLeft(FindKey(tester, new ChildVicinity(xIndex: 4, yIndex: 0))).X);
    }

    // Flutter: 'TwoDimensionalViewport ensureVisible both axes reverse'
    [Fact]
    public void TwoDimensionalViewportEnsureVisibleBothAxesReverse()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, devicePixelRatio: 3.0);
        tester.PumpWidget(
            SimpleBuilderTest(
                verticalDetails: ScrollableDetails.Vertical(reverse: true),
                horizontalDetails: ScrollableDetails.Horizontal(reverse: true),
                useCacheExtent: true));

        EnsureVisible(tester, new ChildVicinity(xIndex: 1, yIndex: 1));
        tester.Pump();
        Assert.Equal(
            Ltrb(600.0, 400.0, 800.0, 600.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 1, yIndex: 1))));
        // (5, 4) is in the cache extent, and will be brought into view next
        Assert.Equal(
            Ltrb(-200.0, -200.0, 0.0, 0.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));
        EnsureVisible(
            tester,
            new ChildVicinity(xIndex: 5, yIndex: 4),
            alignment: 1.0); // Same as ScrollAlignmentPolicy.keepVisibleAtEnd
        tester.Pump();
        // Now in view at trailing corner of viewport
        Assert.Equal(
            Ltrb(0.0, 0.0, 200.0, 200.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));

        // If already visible, no change
        EnsureVisible(tester, new ChildVicinity(xIndex: 5, yIndex: 4), alignment: 1.0);
        tester.Pump();
        Assert.Equal(
            Ltrb(0.0, 0.0, 200.0, 200.0),
            tester.GetRect(FindKey(tester, new ChildVicinity(xIndex: 5, yIndex: 4))));
    }
}
