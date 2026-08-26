using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors cupertino_ui/test/text_selection_toolbar_test.dart.
[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTextSelectionToolbarTests : IDisposable
{
    private const double ChildWidth = 100.0;
    private const double ChildHeight = 44.0;

    public CupertinoTextSelectionToolbarTests()
    {
        Scheduler.ResetForTests();
        MouseCursorManager.ResetForTests();
    }

    public void Dispose()
    {
        MouseCursorManager.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Toolbar_RejectsEmptyChildrenAndExposesScreenPadding()
    {
        Assert.Throws<ArgumentException>(() => new CupertinoTextSelectionToolbar(default, default, []));
        Assert.Equal(8.0, CupertinoTextSelectionToolbar.ToolbarScreenPadding);
        Assert.Equal(new Size(14.0, 7.0), CupertinoTextSelectionToolbar.ToolbarArrowSize);
        Assert.Equal(Radius.Circular(8.0), CupertinoTextSelectionToolbar.ToolbarBorderRadius);
        Assert.Equal(26.0, CupertinoTextSelectionToolbar.ArrowScreenPadding);
        Assert.Equal(10.0, CupertinoTextSelectionToolbar.ToolbarChevronSize);
        Assert.Equal(2.0, CupertinoTextSelectionToolbar.ToolbarChevronThickness);
        Assert.Equal(TimeSpan.FromMilliseconds(125), CupertinoTextSelectionToolbar.ToolbarTransitionDuration);
    }

    [Fact]
    public void Toolbar_ResolvesBackgroundDividerAndTextColorsForBothBrightnesses()
    {
        Assert.Equal(Color.FromUInt32(0xFFF6F6F6), CupertinoTextSelectionToolbar.ToolbarBackgroundColor.Color);
        Assert.Equal(
            Color.FromUInt32(0xFF222222),
            CupertinoTextSelectionToolbar.ToolbarBackgroundColor.DarkColor);
        Assert.Equal(Color.FromUInt32(0xFFD6D6D6), CupertinoTextSelectionToolbar.ToolbarDividerColor.Color);
        Assert.Equal(Color.FromUInt32(0xFF424242), CupertinoTextSelectionToolbar.ToolbarDividerColor.DarkColor);
        Assert.Equal(CupertinoColors.Black, CupertinoTextSelectionToolbar.ToolbarTextColor.Color);
        Assert.Equal(CupertinoColors.White, CupertinoTextSelectionToolbar.ToolbarTextColor.DarkColor);
    }

    [Fact]
    public void Toolbar_AdjustsAnchorsByContentDistanceAndClampsToArrowScreenPadding()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(100000.0, 0.0),
                anchorBelow: new Point(-100000.0, 500.0),
                children: [Child(), Child(), Child()]),
            padding: new Thickness(0.0, 12.0, 0.0, 0.0));
        harness.Pump(new Size(800.0, 600.0));

        CustomSingleChildLayout layout = Assert.Single(harness.FindWidgets<CustomSingleChildLayout>());
        var toolbarDelegate = Assert.IsType<TextSelectionToolbarLayoutDelegate>(layout.LayoutDelegate);

        // paddingAbove = padding.top + kToolbarScreenPadding.
        Assert.Equal(500.0 + 8.0 - 20.0, toolbarDelegate.AnchorBelow.Y);
        Assert.Equal(0.0 - 8.0 - 20.0, toolbarDelegate.AnchorAbove.Y);

        // Both anchors clamp into [_kArrowScreenPadding, width - _kArrowScreenPadding].
        Assert.Equal(800.0 - 26.0, toolbarDelegate.AnchorAbove.X);
        Assert.Equal(26.0, toolbarDelegate.AnchorBelow.X);
    }

    [Fact]
    public void Toolbar_PositionsItselfAboveOnlyWhenTheChildFits()
    {
        // Barely doesn't fit above.
        using (var below = CreateHarness(
                   new CupertinoTextSelectionToolbar(
                       anchorAbove: new Point(100.0, 70.0),
                       anchorBelow: new Point(100.0, 500.0),
                       children: [Child(50.0, 50.0), Child(50.0, 50.0), Child(50.0, 50.0)]),
                   padding: new Thickness(0.0, 12.0, 0.0, 0.0)))
        {
            below.Pump(new Size(800.0, 600.0));
            RenderBox shape = Assert.Single(
                FindDescendants<RenderCupertinoTextSelectionToolbarShape>(below.RenderView));
            Assert.Equal(500.0 + 8.0, shape.LocalToGlobal(new Point(0.0, 0.0)).Y);
        }

        // Fits above: the toolbar bottom lands one arrow height below the anchor.
        using var above = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(100.0, 80.0),
                anchorBelow: new Point(100.0, 500.0),
                children: [Child(50.0, 50.0), Child(50.0, 50.0), Child(50.0, 50.0)]),
            padding: new Thickness(0.0, 12.0, 0.0, 0.0));
        above.Pump(new Size(800.0, 600.0));
        RenderBox aboveShape = Assert.Single(
            FindDescendants<RenderCupertinoTextSelectionToolbarShape>(above.RenderView));
        Assert.Equal(80.0 - 50.0 + 7.0 - 8.0, aboveShape.LocalToGlobal(new Point(0.0, 0.0)).Y);
    }

    [Fact]
    public void Toolbar_ShowsOnlyTheNextChevronOnTheFirstPageAndBothInTheMiddle()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: Enumerable.Range(0, 15).Select(_ => Child()).ToList()));
        harness.Pump(new Size(800.0, 600.0));

        Assert.Empty(FindPainters<LeftCupertinoChevronPainter>(harness));
        Assert.Single(FindPainters<RightCupertinoChevronPainter>(harness));

        RenderCupertinoTextSelectionToolbarItems items = Items(harness);
        Assert.True(items.HasNextPage);
        Assert.False(items.HasPreviousPage);

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);

        Assert.Single(FindPainters<LeftCupertinoChevronPainter>(harness));
        Assert.Single(FindPainters<RightCupertinoChevronPainter>(harness));
        Assert.True(Items(harness).HasPreviousPage);

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);

        Assert.Single(FindPainters<LeftCupertinoChevronPainter>(harness));
        Assert.Empty(FindPainters<RightCupertinoChevronPainter>(harness));
        Assert.False(Items(harness).HasNextPage);
    }

    [Fact]
    public void Toolbar_PaginatesChildrenAndReturnsToThePreviousPage()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: Enumerable.Range(0, 8).Select(_ => Child()).ToList()));
        harness.Pump(new Size(800.0, 600.0));

        // Seven of the eight children fit next to the next-page chevron.
        Assert.Equal(7, VisibleItemCount(harness));
        Assert.Single(FindPainters<RightCupertinoChevronPainter>(harness));
        Assert.Empty(FindPainters<LeftCupertinoChevronPainter>(harness));

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);
        Assert.Equal(1, VisibleItemCount(harness));
        Assert.Empty(FindPainters<RightCupertinoChevronPainter>(harness));
        Assert.Single(FindPainters<LeftCupertinoChevronPainter>(harness));

        TapChevron(harness, next: false);
        AdvancePageTransition(harness);
        Assert.Equal(7, VisibleItemCount(harness));
        Assert.Single(FindPainters<RightCupertinoChevronPainter>(harness));
        Assert.Empty(FindPainters<LeftCupertinoChevronPainter>(harness));
    }

    [Fact]
    public void Toolbar_FitsSixChildrenOnAPageThatAlsoShowsTheBackChevron()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: Enumerable.Range(0, 14).Select(_ => Child()).ToList()));
        harness.Pump(new Size(800.0, 600.0));
        Assert.Equal(7, VisibleItemCount(harness));

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);

        // With the back button, only six children fit on this page.
        Assert.Equal(6, VisibleItemCount(harness));

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);
        Assert.Equal(1, VisibleItemCount(harness));
    }

    [Fact]
    public void ToolbarItems_LaysOutPagesGreedilyAndSizesLargeChildren()
    {
        RenderConstrainedBox back = FixedRenderBox(48.0, ChildHeight);
        RenderConstrainedBox next = FixedRenderBox(48.0, ChildHeight);
        RenderConstrainedBox first = FixedRenderBox(ChildWidth, ChildHeight);
        RenderConstrainedBox second = FixedRenderBox(300.0, ChildHeight);
        RenderConstrainedBox third = FixedRenderBox(ChildWidth, ChildHeight);
        RenderConstrainedBox fourth = FixedRenderBox(ChildWidth, ChildHeight);
        var items = new RenderCupertinoTextSelectionToolbarItems(Colors.Gray, 1.0, 0)
        {
            BackButton = back,
            NextButton = next,
        };
        items.AddAll([first, second, third, fourth]);

        items.Layout(BoxConstraints.Loose(new Size(420.0, 300.0)));
        Assert.True(ShouldPaint(first));
        Assert.False(ShouldPaint(second));
        Assert.True(items.HasNextPage);
        Assert.False(items.HasPreviousPage);

        items.Page = 1;
        items.Layout(BoxConstraints.Loose(new Size(420.0, 300.0)));
        Assert.False(ShouldPaint(first));
        Assert.True(ShouldPaint(second));
        Assert.False(ShouldPaint(third));

        // A page's width never constrains the pages after it.
        Assert.Equal(300.0, second.Size.Width);
        Assert.True(items.HasNextPage);
        Assert.True(items.HasPreviousPage);

        items.Page = 2;
        items.Layout(BoxConstraints.Loose(new Size(420.0, 300.0)));
        Assert.True(ShouldPaint(third));
        Assert.True(ShouldPaint(fourth));
        Assert.False(items.HasNextPage);
        Assert.True(items.HasPreviousPage);

        // Later pages start after the back button plus a divider.
        Assert.Equal(new Point(49.0, 0.0), Offset(third));
        Assert.Equal(new Point(150.0, 0.0), Offset(fourth));
        Assert.True(ShouldPaint(back));
        Assert.False(ShouldPaint(next));
        Assert.Equal(default, Offset(back));
    }

    [Fact]
    public void ToolbarItems_DoesNotPaginateWhenTheChildrenFitWithZeroMargin()
    {
        const double dividerWidth = 1.0 / 3.0;
        var items = new RenderCupertinoTextSelectionToolbarItems(Colors.Gray, dividerWidth, 0)
        {
            BackButton = FixedRenderBox(48.0, ChildHeight),
            NextButton = FixedRenderBox(48.0, ChildHeight),
        };
        List<RenderBox> children = Enumerable.Range(0, 7)
            .Select(_ => (RenderBox)FixedRenderBox(ChildWidth, ChildHeight))
            .ToList();
        items.AddAll(children);

        items.Layout(BoxConstraints.Loose(new Size((7.0 * ChildWidth) + (6.0 * dividerWidth), 300.0)));

        Assert.All(children, child => Assert.True(ShouldPaint(child)));
        Assert.False(items.HasNextPage);
        Assert.False(items.HasPreviousPage);
        Assert.Equal((7.0 * ChildWidth) + (6.0 * dividerWidth), items.Size.Width);
    }

    [Fact]
    public void ToolbarItems_HidesOffPageChildrenFromPaintHitTestAndSemantics()
    {
        var items = new RenderCupertinoTextSelectionToolbarItems(Colors.Gray, 1.0, 0)
        {
            BackButton = FixedRenderBox(48.0, ChildHeight),
            NextButton = FixedRenderBox(48.0, ChildHeight),
        };
        var first = new FixedHitRenderBox(new Size(ChildWidth, ChildHeight));
        var second = new FixedHitRenderBox(new Size(ChildWidth, ChildHeight));
        items.AddAll([first, second]);
        items.Layout(BoxConstraints.Loose(new Size(150.0, 300.0)));

        Assert.True(ShouldPaint(first));
        Assert.False(ShouldPaint(second));
        Assert.True(items.HitTest(new BoxHitTestResult(), new Point(10.0, 10.0)));

        var semanticsChildren = new List<RenderObject>();
        items.VisitChildrenForSemantics(semanticsChildren.Add);
        Assert.DoesNotContain(second, semanticsChildren);
        Assert.Contains(first, semanticsChildren);
    }

    [Fact]
    public void ToolbarShape_SizesToTheChildMinusOneArrowAndOffsetsWhenAbove()
    {
        var above = new RenderCupertinoTextSelectionToolbarShape(
            anchorAbove: new Point(50.0, 100.0),
            anchorBelow: new Point(50.0, 120.0),
            shadowColor: Colors.Black)
        {
            Child = new FixedHitRenderBox(new Size(100.0, 51.0)),
        };
        above.Layout(BoxConstraints.Loose(new Size(200.0, 200.0)));

        Assert.Equal(new Size(100.0, 44.0), above.Size);
        Assert.Equal(new Point(0.0, -7.0), ((BoxParentData)above.Child!.parentData!).offset);
        Assert.True(above.IsRepaintBoundary);

        // The arrow band is not hit-testable; the rounded body is.
        Assert.False(above.HitTest(new BoxHitTestResult(), new Point(50.0, 43.0)));
        Assert.True(above.HitTest(new BoxHitTestResult(), new Point(50.0, 20.0)));

        var below = new RenderCupertinoTextSelectionToolbarShape(
            anchorAbove: new Point(50.0, 10.0),
            anchorBelow: new Point(50.0, 40.0),
            shadowColor: null)
        {
            Child = new FixedHitRenderBox(new Size(100.0, 51.0)),
        };
        below.Layout(BoxConstraints.Loose(new Size(200.0, 200.0)));
        Assert.Equal(default, ((BoxParentData)below.Child!.parentData!).offset);
        Assert.Equal(new Size(100.0, 44.0), below.Size);
    }

    [Fact]
    public void ToolbarShape_EnforcesAMinimumWidthThatFitsTheArrowAndCorners()
    {
        var shape = new RenderCupertinoTextSelectionToolbarShape(
            anchorAbove: new Point(5.0, 5.0),
            anchorBelow: new Point(5.0, 40.0),
            shadowColor: null)
        {
            Child = new FixedHitRenderBox(new Size(4.0, 30.0)),
        };
        shape.Layout(BoxConstraints.Loose(new Size(200.0, 200.0)));

        Assert.Equal(30.0, shape.Child!.Size.Width);
    }

    [Fact]
    public void ToolbarShape_PutsTheArrowAtTheBottomWhenTheToolbarIsAboveTheAnchor()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 400.0),
                anchorBelow: new Point(400.0, 590.0),
                children: [Child(100.0, 50.0)]));
        harness.Pump(new Size(800.0, 600.0));

        RenderCupertinoTextSelectionToolbarShape shape = Shape(harness);
        RenderBox child = shape.Child!;
        Assert.Equal(new Point(0.0, -7.0), ((BoxParentData)child.parentData!).offset);
        Plumix.UI.Path path = shape.ClipPath(child, shape.ShapeRRect(child));
        double arrowTipX = shape.GlobalToLocal(new Point(400.0, 0.0)).X;

        // The arrow sits at the bottom, pointing down at the anchor; the top strip is clipped away.
        Assert.True(path.Contains(new Point(arrowTipX, child.Size.Height - 1.0)));
        Assert.False(path.Contains(new Point(arrowTipX, 2.0)));
        Assert.False(path.Contains(new Point(2.0, child.Size.Height - 1.0)));
    }

    [Fact]
    public void ToolbarShape_PutsTheArrowAtTheTopWhenTheToolbarIsBelowTheAnchor()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 20.0),
                anchorBelow: new Point(400.0, 40.0),
                children: [Child(100.0, 50.0)]));
        harness.Pump(new Size(800.0, 600.0));

        RenderCupertinoTextSelectionToolbarShape shape = Shape(harness);
        RenderBox child = shape.Child!;
        Assert.Equal(default, ((BoxParentData)child.parentData!).offset);
        Assert.Equal(new Rect(0.0, 7.0, 100.0, 36.0), shape.ShapeRRect(child).Rect);

        Plumix.UI.Path path = shape.ClipPath(child, shape.ShapeRRect(child));
        double arrowTipX = shape.GlobalToLocal(new Point(400.0, 0.0)).X;
        Assert.True(path.Contains(new Point(arrowTipX, 2.0)));
        Assert.False(path.Contains(new Point(arrowTipX, child.Size.Height - 1.0)));
        Assert.False(path.Contains(new Point(2.0, 2.0)));
    }

    [Fact]
    public void ToolbarShape_DropsTheArrowWhenTheToolbarIsTooNarrow()
    {
        var shape = new RenderCupertinoTextSelectionToolbarShape(
            anchorAbove: new Point(15.0, 100.0),
            anchorBelow: new Point(15.0, 140.0),
            shadowColor: null)
        {
            Child = new FixedHitRenderBox(new Size(60.0, 56.0)),
        };
        shape.Layout(BoxConstraints.Tight(new Size(20.0, 49.0)));

        Plumix.UI.Path path = shape.ClipPath(shape.Child!, shape.ShapeRRect(shape.Child!));
        Assert.Equal(shape.ShapeRRect(shape.Child!).Rect, path.GetBounds());
    }

    [Fact]
    public void Chevrons_PointToTheCorrectSide()
    {
        var left = new LeftCupertinoChevronPainter(Colors.Black);
        var right = new RightCupertinoChevronPainter(Colors.Black);
        Assert.True(left.IsLeft);
        Assert.False(right.IsLeft);

        Assert.Equal(
            (new Point(7.5, 0.0), new Point(2.5, 5.0), new Point(7.5, 10.0)),
            left.ChevronPoints(new Size(10.0, 10.0)));
        Assert.Equal(
            (new Point(2.5, 0.0), new Point(7.5, 5.0), new Point(2.5, 10.0)),
            right.ChevronPoints(new Size(10.0, 10.0)));

        Assert.True(left.ShouldRepaint(new LeftCupertinoChevronPainter(Colors.White)));
        Assert.True(left.ShouldRepaint(right));
        Assert.False(left.ShouldRepaint(new LeftCupertinoChevronPainter(Colors.Black)));
        Assert.Throws<ArgumentException>(() => left.ChevronPoints(new Size(10.0, 12.0)));
    }

    [Fact]
    public void Toolbar_ComposesFadeAnimatedSizeAndDragPaging()
    {
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: Enumerable.Range(0, 15).Select(_ => Child()).ToList()));
        harness.Pump(new Size(800.0, 600.0));

        Assert.Single(harness.FindWidgets<FadeTransition>().Where(fade => fade.Child is AnimatedSize));
        AnimatedSize animatedSize = Assert.Single(harness.FindWidgets<AnimatedSize>());
        Assert.Equal(TimeSpan.FromMilliseconds(125), animatedSize.Duration);
        Assert.Equal(0.75, animatedSize.Curve(0.5));

        GestureDetector detector = Assert.Single(
            harness.FindWidgets<GestureDetector>().Where(value => value.OnHorizontalDragEnd is not null));

        // A fling to the left advances a page; a fling to the right goes back.
        detector.OnHorizontalDragEnd!(new DragEndDetails(-500.0));
        AdvancePageTransition(harness);
        Assert.True(Items(harness).HasPreviousPage);

        detector = Assert.Single(
            harness.FindWidgets<GestureDetector>().Where(value => value.OnHorizontalDragEnd is not null));
        detector.OnHorizontalDragEnd!(new DragEndDetails(500.0));
        AdvancePageTransition(harness);
        Assert.False(Items(harness).HasPreviousPage);
    }

    [Fact]
    public void Toolbar_ResetsToTheFirstPageWhenTheChildrenListChanges()
    {
        List<Widget> first = Enumerable.Range(0, 15).Select(_ => Child()).ToList();
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: first));
        harness.Pump(new Size(800.0, 600.0));

        TapChevron(harness, next: true);
        AdvancePageTransition(harness);
        Assert.True(Items(harness).HasPreviousPage);

        harness.PumpWidget(WrapForHarness(new CupertinoTextSelectionToolbar(
            anchorAbove: new Point(400.0, 100.0),
            anchorBelow: new Point(400.0, 140.0),
            children: Enumerable.Range(0, 15).Select(_ => Child()).ToList())));
        harness.Pump(new Size(800.0, 600.0));

        Assert.False(Items(harness).HasPreviousPage);
    }

    [Fact]
    public void Toolbar_UsesDarkSurfaceColorsInDarkMode()
    {
        using var light = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: [Child()]));
        light.Pump(new Size(800.0, 600.0));
        Assert.Equal(
            Color.FromUInt32(0xFFF6F6F6),
            Assert.Single(light.FindWidgets<ColoredBox>()).Color);
        Assert.Equal(
            Color.FromArgb(0x33, 0x00, 0x00, 0x00),
            Assert.Single(FindDescendants<RenderCupertinoTextSelectionToolbarShape>(light.RenderView))
                .ShadowColor);
        Assert.Equal(Color.FromUInt32(0xFFD6D6D6), Items(light).DividerColor);

        using var dark = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: [Child()]),
            brightness: PlatformBrightness.Dark);
        dark.Pump(new Size(800.0, 600.0));
        Assert.Equal(
            Color.FromUInt32(0xFF222222),
            Assert.Single(dark.FindWidgets<ColoredBox>()).Color);
        Assert.Null(
            Assert.Single(FindDescendants<RenderCupertinoTextSelectionToolbarShape>(dark.RenderView))
                .ShadowColor);
        Assert.Equal(Color.FromUInt32(0xFF424242), Items(dark).DividerColor);
    }

    [Fact]
    public void Toolbar_UsesTheCustomToolbarBuilderInsteadOfTheDefaultSurface()
    {
        int builderCalls = 0;
        using var harness = CreateHarness(
            new CupertinoTextSelectionToolbar(
                anchorAbove: new Point(400.0, 100.0),
                anchorBelow: new Point(400.0, 140.0),
                children: [Child()],
                toolbarBuilder: (context, anchorAbove, anchorBelow, child) =>
                {
                    builderCalls++;
                    return new ColoredBox(Colors.Red, child: child);
                }));
        harness.Pump(new Size(800.0, 600.0));

        Assert.Equal(1, builderCalls);
        Assert.Empty(FindDescendants<RenderCupertinoTextSelectionToolbarShape>(harness.RenderView));
        Assert.Equal(Colors.Red, Assert.Single(harness.FindWidgets<ColoredBox>()).Color);
    }

    private static Widget Child(double width = ChildWidth, double height = ChildHeight)
    {
        return new SizedBox(width: width, height: height);
    }

    private static RenderConstrainedBox FixedRenderBox(double width, double height)
    {
        return new RenderConstrainedBox(BoxConstraints.Tight(new Size(width, height)));
    }

    private static bool ShouldPaint(RenderBox child)
    {
        return ((ToolbarItemsParentData)child.parentData!).ShouldPaint;
    }

    private static Point Offset(RenderBox child)
    {
        return ((ToolbarItemsParentData)child.parentData!).offset;
    }

    private static RenderCupertinoTextSelectionToolbarShape Shape(CupertinoThemeTestHarness harness)
    {
        return Assert.Single(FindDescendants<RenderCupertinoTextSelectionToolbarShape>(harness.RenderView));
    }

    private static RenderCupertinoTextSelectionToolbarItems Items(CupertinoThemeTestHarness harness)
    {
        return Assert.Single(FindDescendants<RenderCupertinoTextSelectionToolbarItems>(harness.RenderView));
    }

    private static int VisibleItemCount(CupertinoThemeTestHarness harness)
    {
        RenderCupertinoTextSelectionToolbarItems items = Items(harness);
        int count = 0;
        for (RenderBox? child = items.FirstChild; child is not null; child = items.ChildAfter(child))
        {
            if (ShouldPaint(child))
            {
                count++;
            }
        }

        return count;
    }

    private static IReadOnlyList<T> FindPainters<T>(CupertinoThemeTestHarness harness) where T : CustomPainter
    {
        return harness.FindWidgets<CustomPaint>()
            .Where(paint => paint.Painter is T)
            .Where(paint => IsVisibleChevron(harness, paint))
            .Select(paint => (T)paint.Painter!)
            .ToList();
    }

    private static bool IsVisibleChevron(CupertinoThemeTestHarness harness, CustomPaint paint)
    {
        RenderCupertinoTextSelectionToolbarItems items = Items(harness);
        bool isLeft = paint.Painter is LeftCupertinoChevronPainter;
        RenderBox? button = isLeft
            ? items.SlottedChildren[CupertinoTextSelectionToolbarItemsSlot.BackButton]
            : items.SlottedChildren[CupertinoTextSelectionToolbarItemsSlot.NextButton];
        return ShouldPaint(button);
    }

    private static void TapChevron(CupertinoThemeTestHarness harness, bool next)
    {
        CupertinoTextSelectionToolbarButton button = harness
            .FindWidgets<CupertinoTextSelectionToolbarButton>()
            .Single(candidate => candidate.Child is IgnorePointer { Child: CustomPaint paint }
                                 && (paint.Painter is RightCupertinoChevronPainter) == next);
        button.OnPressed!();
    }

    private static void AdvancePageTransition(CupertinoThemeTestHarness harness)
    {
        AnimationPump.Advance(0.2);
        harness.Pump(new Size(800.0, 600.0));
        AnimationPump.Advance(0.2);
        harness.Pump(new Size(800.0, 600.0));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static Widget WrapForHarness(
        Widget child,
        Thickness padding = default,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new CupertinoTheme(
            new CupertinoThemeData(brightness: brightness),
            new MediaQuery(
                new MediaQueryData(
                    Size: new Size(800.0, 600.0),
                    Padding: padding,
                    PlatformBrightness: brightness,
                    DevicePixelRatio: 3.0),
                new Localizations(
                    locale: new Locale("en"),
                    delegates: [DefaultWidgetsLocalizations.Delegate, DefaultCupertinoLocalizations.Delegate],
                    child: new Directionality(TextDirection.Ltr, child))));
    }

    private static CupertinoThemeTestHarness CreateHarness(
        Widget child,
        Thickness padding = default,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new CupertinoThemeTestHarness(WrapForHarness(child, padding, brightness));
    }

    private sealed class FixedHitRenderBox : RenderBox
    {
        private readonly Size _preferredSize;

        public FixedHitRenderBox(Size preferredSize)
        {
            _preferredSize = preferredSize;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_preferredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }

        protected override bool HitTestSelf(Point position) => true;
    }
}
