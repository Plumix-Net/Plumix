using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/widgets/sliver_persistent_header_test.dart,
// flutter/packages/flutter/test/rendering/sliver_persistent_header_test.dart and the persistent-header
// showOnScreen cases of flutter/packages/flutter/test/rendering/viewport_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SliverPersistentHeaderDartParityTests
{
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    // ------------------------------------------------- widgets/sliver_persistent_header_test.dart

    private static void VerifyPaintPosition(GlobalKey key, Point ideal, bool? visible = null)
    {
        var target = (RenderSliver)key.CurrentContext!.FindRenderObject()!;
        Assert.IsType<RenderViewport>(target.Parent);
        var parentData = (SliverPhysicalParentData)target.parentData!;
        Point actual = parentData.PaintOffset;
        Assert.Equal(ideal, actual);

        if (visible != null)
        {
            SliverGeometry geometry = target.Geometry!;
            Assert.Equal(visible, geometry.Visible);
        }
    }

    private static void VerifyActualBoxPosition(FrameworkDartTester tester, int index, Rect ideal)
    {
        // `tester.renderObjectList<RenderBox>(find.byType(Container)).elementAt(index)`.
        Element container = tester.OnstageElements()
            .Where(static element => element.Widget.GetType() == typeof(Container))
            .ElementAt(index);
        Assert.Equal(ideal, tester.GetRect(container));
    }

    // sliver_test_utils.dart: verifySliverGeometry.
    private static void VerifySliverGeometry(GlobalKey key, bool visible, double paintExtent)
    {
        var target = (RenderSliver)key.CurrentContext!.FindRenderObject()!;
        SliverGeometry geometry = target.Geometry!;
        Assert.Equal(visible, geometry.Visible);
        Assert.Equal(paintExtent, geometry.PaintExtent);
    }

    private static ScrollPosition Position(FrameworkDartTester tester) =>
        tester.State<ScrollableState>().Position;

    private static GlobalKey NewKey() => new LabeledGlobalKey<State>(null);

    private static Widget Ltr(Widget child) => new Directionality(TextDirection.Ltr, child);

    // Flutter: '_SliverScrollingPersistentHeader should update stretchConfiguration'
    [Fact]
    public void SliverScrollingPersistentHeaderShouldUpdateStretchConfiguration()
    {
        using var tester = new FrameworkDartTester();
        foreach (double stretchTriggerOffset in new[] { 10.0, 20.0 })
        {
            tester.PumpWidget(new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverPersistentHeader(
                            @delegate: new TestDelegate(
                                stretchConfiguration: new OverScrollHeaderStretchConfiguration(
                                    stretchTriggerOffset: stretchTriggerOffset))),
                    ])));
        }

        Assert.Contains(tester.AllElements(), static element => element.Widget is SliverScrollingPersistentHeader);

        RenderSliverScrollingPersistentHeader render = AllRenderObjects(tester)
            .OfType<RenderSliverScrollingPersistentHeader>()
            .First();
        Assert.Equal(20, render.StretchConfiguration?.StretchTriggerOffset);
    }

    // Flutter: '_SliverPinnedPersistentHeader should update stretchConfiguration'
    [Fact]
    public void SliverPinnedPersistentHeaderShouldUpdateStretchConfiguration()
    {
        using var tester = new FrameworkDartTester();
        foreach (double stretchTriggerOffset in new[] { 10.0, 20.0 })
        {
            tester.PumpWidget(new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverPersistentHeader(
                            pinned: true,
                            @delegate: new TestDelegate(
                                stretchConfiguration: new OverScrollHeaderStretchConfiguration(
                                    stretchTriggerOffset: stretchTriggerOffset))),
                    ])));
        }

        Assert.Contains(tester.AllElements(), static element => element.Widget is SliverPinnedPersistentHeader);

        RenderSliverPinnedPersistentHeader render = AllRenderObjects(tester)
            .OfType<RenderSliverPinnedPersistentHeader>()
            .First();
        Assert.Equal(20, render.StretchConfiguration?.StretchTriggerOffset);
    }

    // Flutter: '_SliverPinnedPersistentHeader should update showOnScreenConfiguration'
    [Fact]
    public void SliverPinnedPersistentHeaderShouldUpdateShowOnScreenConfiguration()
    {
        using var tester = new FrameworkDartTester();
        foreach (double maxShowOnScreenExtent in new[] { 1000.0, 2000.0 })
        {
            tester.PumpWidget(new TestWidgetsApp(
                home: new CustomScrollView(
                    slivers:
                    [
                        new SliverPersistentHeader(
                            pinned: true,
                            @delegate: new TestDelegate(
                                showOnScreenConfiguration: new PersistentHeaderShowOnScreenConfiguration(
                                    maxShowOnScreenExtent: maxShowOnScreenExtent))),
                    ])));
        }

        Assert.Contains(tester.AllElements(), static element => element.Widget is SliverPinnedPersistentHeader);

        RenderSliverPinnedPersistentHeader render = AllRenderObjects(tester)
            .OfType<RenderSliverPinnedPersistentHeader>()
            .First();
        Assert.Equal(2000, render.ShowOnScreenConfiguration?.MaxShowOnScreenExtent);
    }

    // Flutter: "SliverPersistentHeader - floating - scroll offset doesn't change"
    [Fact]
    public void FloatingScrollOffsetDoesNotChange()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 1000.0;
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(height: bigHeight),
                new SliverPersistentHeader(@delegate: new TestDelegate(), floating: true),
                new BigSliver(height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);
        // 600 is the height of the test viewport.
        double max = bigHeight * 2.0 + new TestDelegate().MaxExtent - 600.0;
        Assert.True(max < 10000.0);
        Assert.Equal(1600.0, max);
        Assert.Equal(0.0, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        _ = position.AnimateTo(10000.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(50));
        Assert.Equal(max, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
    }

    // Flutter: 'SliverPersistentHeader - floating - normal behavior works'
    [Fact]
    public void FloatingNormalBehaviorWorks()
    {
        using var tester = new FrameworkDartTester();
        var @delegate = new TestDelegate2();
        const double bigHeight = 1000.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: @delegate, floating: true),
                new BigSliver(key: key3, height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);

        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 1000.0), false);
        VerifyPaintPosition(key3, new Point(0.0, 1200.0), false);

        _ = position.AnimateTo(bigHeight - 600.0 + @delegate.MaxExtent, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 600.0 - @delegate.MaxExtent), true);
        VerifyActualBoxPosition(
            tester,
            0,
            new Rect(0.0, 600.0 - @delegate.MaxExtent, 800.0, @delegate.MaxExtent));
        VerifyPaintPosition(key3, new Point(0.0, 600.0), false);

        Assert.True(@delegate.MaxExtent * 2.0 < 600.0); // make sure this fits on the test screen...
        _ = position.AnimateTo(
            bigHeight - 600.0 + @delegate.MaxExtent * 2.0,
            curve: Curves.Linear,
            duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 600.0 - @delegate.MaxExtent * 2.0), true);
        VerifyActualBoxPosition(
            tester,
            0,
            new Rect(0.0, 600.0 - @delegate.MaxExtent * 2.0, 800.0, @delegate.MaxExtent));
        VerifyPaintPosition(key3, new Point(0.0, 600.0 - @delegate.MaxExtent), true);

        _ = position.AnimateTo(bigHeight, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyActualBoxPosition(tester, 0, new Rect(0.0, 0.0, 800.0, @delegate.MaxExtent));
        VerifyPaintPosition(key3, new Point(0.0, @delegate.MaxExtent), true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 0.1, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyActualBoxPosition(tester, 0, new Rect(0.0, 0.0, 800.0, @delegate.MaxExtent * 0.9));
        VerifyPaintPosition(key3, new Point(0.0, @delegate.MaxExtent * 0.9), true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 0.5, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyActualBoxPosition(tester, 0, new Rect(0.0, 0.0, 800.0, @delegate.MaxExtent * 0.5));
        VerifyPaintPosition(key3, new Point(0.0, @delegate.MaxExtent * 0.5), true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 0.9, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyActualBoxPosition(
            tester,
            0,
            new Rect(0.0, -@delegate.MaxExtent * 0.4, 800.0, @delegate.MaxExtent * 0.5));
        VerifyPaintPosition(key3, new Point(0.0, @delegate.MaxExtent * 0.1), true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 2.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, false);
        VerifyPaintPosition(key3, default, true);
    }

    // Flutter: 'SliverPersistentHeader - floating - no floating behavior when animating'
    [Fact]
    public void FloatingNoFloatingBehaviorWhenAnimating()
    {
        using var tester = new FrameworkDartTester();
        var @delegate = new TestDelegate();
        const double bigHeight = 1000.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: @delegate, floating: true),
                new BigSliver(key: key3, height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);

        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 1000.0), false);
        VerifyPaintPosition(key3, new Point(0.0, 1200.0), false);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 2.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, false);
        VerifyPaintPosition(key3, default, true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 1.9, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, false);
        VerifyPaintPosition(key3, default, true);
    }

    // Flutter: 'SliverPersistentHeader - floating - floating behavior when dragging down'
    [Fact]
    public void FloatingFloatingBehaviorWhenDraggingDown()
    {
        using var tester = new FrameworkDartTester();
        var @delegate = new TestDelegate2();
        const double bigHeight = 1000.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: @delegate, floating: true),
                new BigSliver(key: key3, height: bigHeight),
            ])));
        var position = (ScrollPositionWithSingleContext)Position(tester);

        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 1000.0), false);
        VerifyPaintPosition(key3, new Point(0.0, 1200.0), false);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 2.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, false);
        VerifyPaintPosition(key3, default, true);

        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent * 1.9, curve: Curves.Linear, duration: OneMinute);
        position.UpdateUserScrollDirection(ScrollDirection.Forward);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyActualBoxPosition(
            tester,
            0,
            new Rect(0.0, -@delegate.MaxExtent * 0.4, 800.0, @delegate.MaxExtent * 0.5));
        VerifyPaintPosition(key3, default, true);
    }

    // Flutter: 'SliverPersistentHeader - floating - overscroll gap is below header'
    [Fact]
    public void FloatingOverscrollGapIsBelowHeader()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Ltr(new CustomScrollView(
            physics: new BouncingScrollPhysics(),
            slivers:
            [
                new SliverPersistentHeader(@delegate: new TestDelegate(), floating: true),
                SliverList.FromChildren([new SizedBox(height: 300.0, child: new Text("X"))]),
            ])));

        Assert.Equal(default, tester.GetTopLeft(SizedBoxAncestorOfHeaderText(tester)));
        Assert.Equal(new Point(0.0, 200.0), tester.GetTopLeft(Text(tester, "X")));

        ScrollPosition position = Position(tester);
        position.JumpTo(-50.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(SizedBoxAncestorOfHeaderText(tester)));
        Assert.Equal(new Point(0.0, 250.0), tester.GetTopLeft(Text(tester, "X")));
    }

    // Flutter: 'SliverPersistentHeader - pinned'
    [Fact]
    public void Pinned()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 550.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey(), key4 = NewKey(), key5 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: new TestDelegate2(), pinned: true),
                new SliverPersistentHeader(key: key3, @delegate: new TestDelegate2(), pinned: true),
                new BigSliver(key: key4, height: bigHeight),
                new BigSliver(key: key5, height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);
        // 600 is the height of the test viewport.
        double max = bigHeight * 3.0 + new TestDelegate().MaxExtent * 2.0 - 600.0;
        Assert.True(max < 10000.0);
        Assert.Equal(1450.0, max);
        Assert.Equal(0.0, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        _ = position.AnimateTo(10000.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(10));
        Assert.Equal(max, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key4, default, true);
        VerifyPaintPosition(key5, new Point(0.0, 50.0), true);
    }

    // Flutter: 'SliverPersistentHeader - toStringDeep of maxExtent that throws'
    [Fact]
    public void ToStringDeepOfMaxExtentThatThrows()
    {
        using var tester = new FrameworkDartTester();
        var delegateThatCanThrow = new TestDelegateThatCanThrow();
        GlobalKey key = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new SliverPersistentHeader(key: key, @delegate: delegateThatCanThrow, pinned: true),
            ])));
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(10));

        RenderObject renderObject = key.CurrentContext!.FindRenderObject()!;
        // The delegate must only start throwing immediately before calling toStringDeep to avoid
        // triggering spurious exceptions.
        delegateThatCanThrow.ShouldThrow = true;
        Assert.Equal(
            "RenderSliverPinnedPersistentHeaderForWidgets#00000 relayoutBoundary=up1\n"
            + " │ parentData: paintOffset=Offset(0.0, 0.0) (can use size)\n"
            + " │ constraints: SliverConstraints(AxisDirection.down,\n"
            + " │   GrowthDirection.forward, ScrollDirection.idle, scrollOffset:\n"
            + " │   0.0, precedingScrollExtent: 0.0, remainingPaintExtent: 600.0,\n"
            + " │   crossAxisExtent: 800.0, crossAxisDirection:\n"
            + " │   AxisDirection.right, viewportMainAxisExtent: 600.0,\n"
            + " │   remainingCacheExtent: 850.0, cacheOrigin: 0.0)\n"
            + " │ geometry: SliverGeometry(scrollExtent: 200.0, paintExtent: 200.0,\n"
            + " │   maxPaintExtent: 200.0, hasVisualOverflow: true, cacheExtent:\n"
            + " │   200.0)\n"
            + " │ maxExtent: EXCEPTION (FlutterError)\n"
            + " │ child position: 0.0\n"
            + " │\n"
            + " └─child: RenderConstrainedBox#00000 relayoutBoundary=up2\n"
            + "   │ parentData: <none> (can use size)\n"
            + "   │ constraints: BoxConstraints(w=800.0, 0.0<=h<=200.0)\n"
            + "   │ size: Size(800.0, 200.0)\n"
            + "   │ additionalConstraints: BoxConstraints(0.0<=w<=Infinity,\n"
            + "   │   100.0<=h<=200.0)\n"
            + "   │\n"
            + "   └─child: RenderLimitedBox#00000 relayoutBoundary=up3\n"
            + "     │ parentData: <none> (can use size)\n"
            + "     │ constraints: BoxConstraints(w=800.0, 100.0<=h<=200.0)\n"
            + "     │ size: Size(800.0, 200.0)\n"
            + "     │ maxWidth: 0.0\n"
            + "     │ maxHeight: 0.0\n"
            + "     │\n"
            + "     └─child: RenderConstrainedBox#00000 relayoutBoundary=up4\n"
            + "         parentData: <none> (can use size)\n"
            + "         constraints: BoxConstraints(w=800.0, 100.0<=h<=200.0)\n"
            + "         size: Size(800.0, 200.0)\n"
            + "         additionalConstraints: BoxConstraints(biggest)\n",
            FrameworkDartTester.IgnoringHashCodes(renderObject.ToStringDeep(minLevel: DiagnosticLevel.Info)));
    }

    // Flutter: 'SliverPersistentHeader - pinned with slow scroll'
    [Fact]
    public void PinnedWithSlowScroll()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 550.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey(), key4 = NewKey(), key5 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: new TestDelegate2(), pinned: true),
                new SliverPersistentHeader(key: key3, @delegate: new TestDelegate2(), pinned: true),
                new BigSliver(key: key4, height: bigHeight),
                new BigSliver(key: key5, height: bigHeight),
            ])));

        ScrollPosition position = Position(tester);
        VerifyPaintPosition(key1, default, true);
        VerifyPaintPosition(key2, new Point(0.0, 550.0), true);
        VerifyPaintPosition(key3, new Point(0.0, 750.0), false);
        VerifyPaintPosition(key4, new Point(0.0, 950.0), false);
        VerifyPaintPosition(key5, new Point(0.0, 1500.0), false);
        _ = position.AnimateTo(550.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle();
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 200.0), true);
        VerifyPaintPosition(key4, new Point(0.0, 400.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 950.0), false);
        _ = position.AnimateTo(600.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(200));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 150.0), true);
        VerifyPaintPosition(key4, new Point(0.0, 350.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 900.0), false);
        _ = position.AnimateTo(650.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(300));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyActualBoxPosition(tester, 1, new Rect(0.0, 100.0, 800.0, 200.0));
        VerifyPaintPosition(key4, new Point(0.0, 300.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 850.0), false);
        _ = position.AnimateTo(700.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(400));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyActualBoxPosition(tester, 1, new Rect(0.0, 100.0, 800.0, 200.0));
        VerifyPaintPosition(key4, new Point(0.0, 250.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 800.0), false);
        _ = position.AnimateTo(750.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(500));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyActualBoxPosition(tester, 1, new Rect(0.0, 100.0, 800.0, 200.0));
        VerifyPaintPosition(key4, new Point(0.0, 200.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 750.0), false);
        _ = position.AnimateTo(800.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(60));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key4, new Point(0.0, 150.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 700.0), false);
        _ = position.AnimateTo(850.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(70));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key4, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 650.0), false);
        _ = position.AnimateTo(900.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(80));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key4, new Point(0.0, 50.0), true);
        VerifyPaintPosition(key5, new Point(0.0, 600.0), false);
        _ = position.AnimateTo(950.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(90));
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyActualBoxPosition(tester, 1, new Rect(0.0, 100.0, 800.0, 100.0));
        VerifyPaintPosition(key4, default, true);
        VerifyPaintPosition(key5, new Point(0.0, 550.0), true);
    }

    // Flutter: 'SliverPersistentHeader - pinned with less overlap'
    [Fact]
    public void PinnedWithLessOverlap()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 650.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey(), key4 = NewKey(), key5 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: new TestDelegate2(), pinned: true),
                new SliverPersistentHeader(key: key3, @delegate: new TestDelegate2(), pinned: true),
                new BigSliver(key: key4, height: bigHeight),
                new BigSliver(key: key5, height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);
        // 600 is the height of the test viewport.
        double max = bigHeight * 3.0 + new TestDelegate2().MaxExtent * 2.0 - 600.0;
        Assert.True(max < 10000.0);
        Assert.Equal(1750.0, max);
        Assert.Equal(0.0, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        _ = position.AnimateTo(10000.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(10));
        Assert.Equal(max, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        VerifyPaintPosition(key1, default, false);
        VerifyPaintPosition(key2, default, true);
        VerifyPaintPosition(key3, new Point(0.0, 100.0), true);
        VerifyPaintPosition(key4, default, false);
        VerifyPaintPosition(key5, default, true);
    }

    // Flutter: 'SliverPersistentHeader - overscroll gap is below header'
    [Fact]
    public void PinnedOverscrollGapIsBelowHeader()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Ltr(new CustomScrollView(
            physics: new BouncingScrollPhysics(),
            slivers:
            [
                new SliverPersistentHeader(@delegate: new TestDelegate2(), pinned: true),
                SliverList.FromChildren([new SizedBox(height: 300.0, child: new Text("X"))]),
            ])));

        Assert.Equal(default, tester.GetTopLeft(tester.ElementOfType<Container>()));
        Assert.Equal(new Point(0.0, 200.0), tester.GetTopLeft(Text(tester, "X")));

        ScrollPosition position = Position(tester);
        position.JumpTo(-50.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(tester.ElementOfType<Container>()));
        Assert.Equal(new Point(0.0, 250.0), tester.GetTopLeft(Text(tester, "X")));

        position.JumpTo(50.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(tester.ElementOfType<Container>()));
        Assert.Equal(new Point(0.0, 150.0), tester.GetTopLeft(Text(tester, "X")));

        position.JumpTo(150.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(tester.ElementOfType<Container>()));
        Assert.Equal(new Point(0.0, 50.0), tester.GetTopLeft(Text(tester, "X")));
    }

    // Flutter: 'SliverPersistentHeader pointer scrolled floating'
    [Fact]
    public void PointerScrolledFloating()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey headerKey = NewKey();
        tester.PumpWidget(new TestWidgetsApp(
            home: new CustomScrollView(
                slivers:
                [
                    new SliverPersistentHeader(key: headerKey, floating: true, @delegate: new TestDelegate3()),
                    new SliverFixedExtentList(
                        itemExtent: 50.0,
                        @delegate: new SliverChildBuilderDelegate(
                            (_, index) => new Text($"Item {index}"),
                            childCount: 30)),
                ])));

        Assert.Single(tester.OnstageElementsWithText("Test Title"));
        Assert.Single(tester.OnstageElementsWithText("Item 1"));
        Assert.Single(tester.OnstageElementsWithText("Item 5"));
        VerifySliverGeometry(headerKey, visible: true, paintExtent: 56.0);

        // Pointer scroll the app bar away, we will scroll back less to validate the app bar floats
        // back in.
        Point point1 = tester.GetCenter(Text(tester, "Item 5"));
        tester.SendPointerScroll(point1, new Vector(0.0, 300.0));
        tester.Pump();
        Assert.Empty(tester.OnstageElementsWithText("Test Title"));
        Assert.Empty(tester.OnstageElementsWithText("Item 1"));
        Assert.Single(tester.OnstageElementsWithText("Item 5"));
        VerifySliverGeometry(headerKey, paintExtent: 0.0, visible: false);

        // Scroll back to float in appbar
        tester.SendPointerScroll(point1, new Vector(0.0, -50.0));
        tester.Pump();
        Assert.Single(tester.OnstageElementsWithText("Test Title"));
        Assert.Empty(tester.OnstageElementsWithText("Item 1"));
        Assert.Single(tester.OnstageElementsWithText("Item 5"));
        VerifySliverGeometry(headerKey, paintExtent: 50.0, visible: true);

        // Float the rest of the way in.
        tester.SendPointerScroll(point1, new Vector(0.0, -250.0));
        tester.Pump();
        Assert.Single(tester.OnstageElementsWithText("Test Title"));
        Assert.Single(tester.OnstageElementsWithText("Item 1"));
        Assert.Single(tester.OnstageElementsWithText("Item 5"));
        VerifySliverGeometry(headerKey, paintExtent: 56.0, visible: true);
    }

    // Flutter: 'SliverPersistentHeader - scrolling'
    [Fact]
    public void Scrolling()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 550.0;
        GlobalKey key1 = NewKey(), key2 = NewKey(), key3 = NewKey(), key4 = NewKey(), key5 = NewKey();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(key: key1, height: bigHeight),
                new SliverPersistentHeader(key: key2, @delegate: new TestDelegate()),
                new SliverPersistentHeader(key: key3, @delegate: new TestDelegate()),
                new BigSliver(key: key4, height: bigHeight),
                new BigSliver(key: key5, height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);
        // 600 is the height of the test viewport.
        double max = bigHeight * 3.0 + new TestDelegate().MaxExtent * 2.0 - 600.0;
        Assert.True(max < 10000.0);
        Assert.Equal(1450.0, max);
        Assert.Equal(0.0, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        _ = position.AnimateTo(10000.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(10));
        Assert.Equal(max, position.Pixels);
        Assert.Equal(0.0, position.MinScrollExtent);
        Assert.Equal(max, position.MaxScrollExtent);
        VerifyPaintPosition(key1, default);
        VerifyPaintPosition(key2, default);
        VerifyPaintPosition(key3, default);
        VerifyPaintPosition(key4, default);
        VerifyPaintPosition(key5, new Point(0.0, 50.0));
    }

    // Flutter: 'SliverPersistentHeader - scrolling off screen'
    [Fact]
    public void ScrollingOffScreen()
    {
        using var tester = new FrameworkDartTester();
        const double bigHeight = 550.0;
        GlobalKey key = NewKey();
        var @delegate = new TestDelegate();
        tester.PumpWidget(Ltr(new CustomScrollView(
            slivers:
            [
                new BigSliver(height: bigHeight),
                new SliverPersistentHeader(key: key, @delegate: @delegate),
                new BigSliver(height: bigHeight),
                new BigSliver(height: bigHeight),
            ])));
        ScrollPosition position = Position(tester);
        _ = position.AnimateTo(bigHeight + @delegate.MaxExtent - 5.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(new Rect(0.0, -195.0, 800.0, 200.0), tester.GetRect(Text(tester, "Sliver Persistent Header")));
    }

    // Flutter: 'SliverPersistentHeader - scrolling - overscroll gap is below header'
    [Fact]
    public void ScrollingOverscrollGapIsBelowHeader()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Ltr(new CustomScrollView(
            physics: new BouncingScrollPhysics(),
            slivers:
            [
                new SliverPersistentHeader(@delegate: new TestDelegate()),
                SliverList.FromChildren([new SizedBox(height: 300.0, child: new Text("X"))]),
            ])));

        Assert.Equal(default, tester.GetTopLeft(Text(tester, "Sliver Persistent Header")));
        Assert.Equal(new Point(0.0, 200.0), tester.GetTopLeft(Text(tester, "X")));

        ScrollPosition position = Position(tester);
        position.JumpTo(-50.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(Text(tester, "Sliver Persistent Header")));
        Assert.Equal(new Point(0.0, 250.0), tester.GetTopLeft(Text(tester, "X")));
    }

    // Flutter: 'Sliver SliverPersistentHeader const child delegate - scrolling - overscroll gap is
    // below header'
    [Fact]
    public void ConstChildDelegateScrollingOverscrollGapIsBelowHeader()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Ltr(new CustomScrollView(
            physics: new BouncingScrollPhysics(),
            slivers:
            [
                new SliverPersistentHeader(@delegate: new TestDelegate()),
                new SliverList(
                    @delegate: SliverChildListDelegate.Fixed([new SizedBox(height: 300.0, child: new Text("X"))])),
            ])));

        Assert.Equal(default, tester.GetTopLeft(Text(tester, "Sliver Persistent Header")));
        Assert.Equal(new Point(0.0, 200.0), tester.GetTopLeft(Text(tester, "X")));

        ScrollPosition position = Position(tester);
        position.JumpTo(-50.0);
        tester.Pump();

        Assert.Equal(default, tester.GetTopLeft(Text(tester, "Sliver Persistent Header")));
        Assert.Equal(new Point(0.0, 250.0), tester.GetTopLeft(Text(tester, "X")));
    }

    // Flutter: 'has correct semantics when within viewport'
    [Fact]
    public void HasCorrectSemanticsWhenWithinViewport()
    {
        using var tester = new FrameworkDartTester();
        const double cacheExtent = 250;
        using SemanticsHandleScope handle = EnsureSemantics(tester);

        tester.PumpWidget(Ltr(new CustomScrollView(
            cacheExtent: cacheExtent,
            physics: new BouncingScrollPhysics(),
            slivers:
            [
                new SliverPersistentHeader(@delegate: new TestDelegate()),
                new SliverList(
                    @delegate: SliverChildListDelegate.Fixed([new SizedBox(height: 300.0, child: new Text("X"))])),
            ])));
        FlushSemantics(tester);

        SemanticsNode sliver = Assert.Single(SemanticsNodesWithLabel(tester, "Sliver Persistent Header"));
        Assert.False(sliver.GetSemanticsData().HasFlag(SemanticsFlags.IsHidden));
    }

    // Flutter: 'has correct semantics when partially scrolling off screen'
    [Fact]
    public void HasCorrectSemanticsWhenPartiallyScrollingOffScreen()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key = NewKey();
        var @delegate = new TestDelegate();
        using SemanticsHandleScope handle = EnsureSemantics(tester);
        const double cacheExtent = 250;
        tester.PumpWidget(Ltr(new CustomScrollView(
            cacheExtent: cacheExtent,
            slivers:
            [
                new SliverPersistentHeader(key: key, @delegate: @delegate),
                new BigSliver(height: 550.0),
                new BigSliver(height: 550.0),
            ])));
        ScrollPosition position = Position(tester);
        _ = position.AnimateTo(@delegate.MaxExtent - 20.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        FlushSemantics(tester);
        Assert.Equal(new Rect(0.0, -180.0, 800.0, 200.0), tester.GetRect(Text(tester, "Sliver Persistent Header")));

        SemanticsNode sliver = Assert.Single(SemanticsNodesWithLabel(tester, "Sliver Persistent Header"));
        Assert.False(sliver.GetSemanticsData().HasFlag(SemanticsFlags.IsHidden));
    }

    // Flutter: 'has correct semantics when completely scrolling off screen but within cache extent'
    [Fact]
    public void HasCorrectSemanticsWhenCompletelyScrollingOffScreenButWithinCacheExtent()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key = NewKey();
        var @delegate = new TestDelegate();
        using SemanticsHandleScope handle = EnsureSemantics(tester);
        const double cacheExtent = 250;
        tester.PumpWidget(Ltr(new CustomScrollView(
            cacheExtent: cacheExtent,
            slivers:
            [
                new SliverPersistentHeader(key: key, @delegate: @delegate),
                new BigSliver(height: 550.0),
                new BigSliver(height: 550.0),
            ])));
        ScrollPosition position = Position(tester);
        _ = position.AnimateTo(@delegate.MaxExtent + 20.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        FlushSemantics(tester);

        SemanticsNode sliver = Assert.Single(SemanticsNodesWithLabel(tester, "Sliver Persistent Header"));
        Assert.True(sliver.GetSemanticsData().HasFlag(SemanticsFlags.IsHidden));
    }

    // Flutter: 'has correct semantics when completely scrolling off screen and not within cache
    // extent'
    [Fact]
    public void HasCorrectSemanticsWhenCompletelyScrollingOffScreenAndNotWithinCacheExtent()
    {
        using var tester = new FrameworkDartTester();
        GlobalKey key = NewKey();
        var @delegate = new TestDelegate();
        using SemanticsHandleScope handle = EnsureSemantics(tester);
        const double cacheExtent = 250;
        tester.PumpWidget(Ltr(new CustomScrollView(
            cacheExtent: cacheExtent,
            slivers:
            [
                new SliverPersistentHeader(key: key, @delegate: @delegate),
                new BigSliver(height: 550.0),
                new BigSliver(height: 550.0),
            ])));
        ScrollPosition position = Position(tester);
        _ = position.AnimateTo(@delegate.MaxExtent + 300.0, curve: Curves.Linear, duration: OneMinute);
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(1000));
        FlushSemantics(tester);

        Assert.Empty(SemanticsNodesWithLabel(tester, "Sliver Persistent Header"));
    }

    // ---------------------------------------------- rendering/sliver_persistent_header_test.dart

    // Flutter: 'RenderSliverFloatingPersistentHeader maxScrollObstructionExtent is 0'
    [Fact]
    public void RenderSliverFloatingPersistentHeaderMaxScrollObstructionExtentIsZero()
    {
        var header = new TestRenderSliverFloatingPersistentHeader(new RenderSizedBox(new Size(400.0, 100.0)));
        var root = new RenderViewport(
            crossAxisDirection: AxisDirection.Right,
            offset: ViewportOffset.Zero(),
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [header]);
        Layout(root);

        Assert.Equal(0, header.Geometry!.MaxScrollObstructionExtent);
    }

    // Flutter: 'RenderSliverFloatingPinnedPersistentHeader maxScrollObstructionExtent is minExtent'
    [Fact]
    public void RenderSliverFloatingPinnedPersistentHeaderMaxScrollObstructionExtentIsMinExtent()
    {
        var header = new TestRenderSliverFloatingPinnedPersistentHeader(
            new RenderSizedBox(new Size(400.0, 100.0)));
        var root = new RenderViewport(
            crossAxisDirection: AxisDirection.Right,
            offset: ViewportOffset.Zero(),
            scrollCacheExtent: ScrollCacheExtent.Pixels(0),
            children: [header]);
        Layout(root);

        Assert.Equal(100.0, header.Geometry!.MaxScrollObstructionExtent);
    }

    // ---------------------------------------------------------- rendering/viewport_test.dart

    // Flutter: 'Viewport showOnScreen should not scroll if the rect is already visible, even if it
    // does not scroll linearly'
    [Fact]
    public void ViewportShowOnScreenShouldNotScrollIfTheRectIsAlreadyVisible()
    {
        using var tester = new FrameworkDartTester();
        var controller = new ScrollController(initialScrollOffset: 300.0);
        Key headerKey = Key.Create("header");
        List<Widget> children = Enumerable.Range(0, 20).Select(Widget (i) => i == 10
                ? new SliverPersistentHeader(
                    pinned: true,
                    @delegate: new ViewportTestHeaderDelegate(minExtent: 100, maxExtent: 300, key: headerKey))
                : new SliverToBoxAdapter(child: new SizedBox(height: 300.0, child: new Text($"Tile {i}"))))
            .ToList();
        tester.PumpWidget(Ltr(new Center(
            child: new SizedBox(
                height: 600.0,
                child: new CustomScrollView(controller: controller, slivers: children)))));

        controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();

        // find.descendant(of: find.byWidget(children[10]), matching: find.byKey(headerKey)).
        Element header = tester.ElementOfWidget(children[10]);
        Element pinnedHeaderContent = tester.OnstageElementsWithKey(headerKey)
            .Single(element => IsDescendantOf(element, header));

        // The persistent header is pinned to the leading edge thus still visible, the viewport
        // should not scroll.
        pinnedHeaderContent.RenderObject!.ShowOnScreen();
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, controller.Offset);

        // The 11th child will be partially obstructed by the persistent header, the viewport should
        // scroll to reveal it.
        controller.JumpTo(
            11 * 300.0 // Preceding headers
            + 200.0 // Shrinks the pinned header to minExtent
            + 100.0); // Obstructs the leading 100 pixels of the 11th header
        tester.PumpAndSettle();

        tester.ElementOfWidget(children[11]).RenderObject!.ShowOnScreen();
        tester.PumpAndSettle();
        Assert.True(controller.Offset < 11 * 300.0 + 200.0 + 100.0, $"{controller.Offset}");
        controller.Dispose();
    }

    // Flutter: 'Floating header showOnScreen' group: testFloatingHeaderShowOnScreen().
    private sealed class FloatingHeaderShowOnScreenFixture
    {
        public static readonly Key HeaderKey = Key.Create("header");

        private readonly Axis _axis;

        public FloatingHeaderShowOnScreenFixture(Axis axis, bool animated = true)
        {
            _axis = axis;
            Vsync = animated ? new TestVSync() : null;
        }

        public ITickerProvider? Vsync { get; }

        public ScrollController Controller { get; } = new(initialScrollOffset: 300.0);

        public List<Widget> Children { get; private set; } = [];

        public Widget BuildList(SliverPersistentHeader floatingHeader, bool reversed = false)
        {
            Children = Enumerable.Range(0, 20).Select(Widget (i) => i == 10
                    ? floatingHeader
                    : new SliverToBoxAdapter(
                        key: i == 19 ? Key.Create("19") : null,
                        child: new SizedBox(height: 300.0, width: 300, child: new Text($"Tile {i}"))))
                .ToList();
            return Ltr(new Center(
                child: SizedBox.Square(
                    dimension: 400.0,
                    child: new CustomScrollView(
                        scrollDirection: _axis,
                        center: reversed ? Key.Create("19") : null,
                        controller: Controller,
                        slivers: Children))));
        }

        public double MainAxisExtent(Element element)
        {
            RenderObject renderObject = element.FindRenderObject()!;
            if (renderObject is RenderSliver sliver)
            {
                return sliver.Geometry!.PaintExtent;
            }

            var renderBox = (RenderBox)renderObject;
            return _axis switch
            {
                Axis.Horizontal => renderBox.Size.Width,
                _ => renderBox.Size.Height,
            };
        }

        public SliverPersistentHeader Header(
            PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null,
            bool keyOnHeader = false)
        {
            return new SliverPersistentHeader(
                key: keyOnHeader ? HeaderKey : null,
                pinned: true,
                floating: true,
                @delegate: new ViewportTestHeaderDelegate(
                    minExtent: 100,
                    maxExtent: 300,
                    key: keyOnHeader ? null : HeaderKey,
                    vsync: Vsync,
                    showOnScreenConfiguration: showOnScreenConfiguration));
        }
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen'
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreen(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(floatingHeader: fixture.Header()));

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        fixture.Controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();
        Assert.True(fixture.MainAxisExtent(PinnedHeaderContent()) < 300);

        // The persistent header is pinned to the leading edge thus still visible, the viewport
        // should not scroll.
        RenderObject content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(0, 0, 300, 300));
        tester.PumpAndSettle();
        // The header expands but doesn't move.
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(300, fixture.MainAxisExtent(PinnedHeaderContent()));

        // The rect specifies that the persistent header needs to be 1 pixel away from the leading
        // edge of the viewport. Ignore the 1 pixel, the viewport should not scroll.
        //
        // See: https://github.com/flutter/flutter/issues/25507.
        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(-1, -1, 300, 300));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(300, fixture.MainAxisExtent(PinnedHeaderContent()));
        fixture.Controller.Dispose();
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen twice almost
    // instantly'
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreenTwiceAlmostInstantly(Axis axis)
    {
        // Regression test for https://github.com/flutter/flutter/issues/137901
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(floatingHeader: fixture.Header()));

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        fixture.Controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();
        Assert.True(fixture.MainAxisExtent(PinnedHeaderContent()) < 300);

        RenderObject content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(
            descendant: content,
            // Adding different rect to check if the second showOnScreen call leads to a different
            // result. When the animation has forward status and the second showOnScreen is called,
            // the new animation won't start.
            rect: new Rect(0, 0, 150, 150),
            duration: TimeSpan.FromSeconds(3));
        tester.Pump(TimeSpan.FromSeconds(1));

        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(0, 0, 300, 300));

        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(150, fixture.MainAxisExtent(PinnedHeaderContent()));
        fixture.Controller.Dispose();
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen but no child'
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreenButNoChild(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(floatingHeader: fixture.Header(keyOnHeader: true)));

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        fixture.Controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();
        Assert.True(fixture.MainAxisExtent(PinnedHeaderContent()) < 300);

        // The persistent header is pinned to the leading edge thus still visible, the viewport
        // should not scroll.
        PinnedHeaderContent().FindRenderObject()!.ShowOnScreen(rect: new Rect(0, 0, 300, 300));
        tester.PumpAndSettle();
        // The header expands but doesn't move.
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(300, fixture.MainAxisExtent(PinnedHeaderContent()));

        // The rect specifies that the persistent header needs to be 1 pixel away from the leading
        // edge of the viewport. Ignore the 1 pixel, the viewport should not scroll.
        //
        // See: https://github.com/flutter/flutter/issues/25507.
        PinnedHeaderContent().FindRenderObject()!.ShowOnScreen(rect: new Rect(-1, -1, 300, 300));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(300, fixture.MainAxisExtent(PinnedHeaderContent()));
        fixture.Controller.Dispose();
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen with
    // maxShowOnScreenExtent '
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreenWithMaxShowOnScreenExtent(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(
            floatingHeader: fixture.Header(
                showOnScreenConfiguration: new PersistentHeaderShowOnScreenConfiguration(
                    maxShowOnScreenExtent: 200))));

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        fixture.Controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();
        // childExtent was initially 100.
        Assert.Equal(100, fixture.MainAxisExtent(PinnedHeaderContent()));

        RenderObject content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(0, 0, 300, 300));
        tester.PumpAndSettle();
        // The header doesn't move. It would have expanded to 300 but maxShowOnScreenExtent is 200,
        // preventing it from doing so.
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(200, fixture.MainAxisExtent(PinnedHeaderContent()));

        // ignoreLeading still works.
        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(-1, -1, 300, 300));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(200, fixture.MainAxisExtent(PinnedHeaderContent()));

        // Move the viewport so that its childExtent reaches 250.
        fixture.Controller.JumpTo(300.0 * 10 + 50.0);
        tester.PumpAndSettle();
        Assert.Equal(250, fixture.MainAxisExtent(PinnedHeaderContent()));

        // Doesn't move, doesn't expand or shrink, leading still ignored.
        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(-1, -1, 300, 300));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 10 + 50.0, fixture.Controller.Offset);
        Assert.Equal(250, fixture.MainAxisExtent(PinnedHeaderContent()));
        fixture.Controller.Dispose();
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen with
    // minShowOnScreenExtent '
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreenWithMinShowOnScreenExtent(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(
            floatingHeader: fixture.Header(
                showOnScreenConfiguration: new PersistentHeaderShowOnScreenConfiguration(
                    minShowOnScreenExtent: 200))));

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        fixture.Controller.JumpTo(300.0 * 15);
        tester.PumpAndSettle();
        // childExtent was initially 100.
        Assert.Equal(100, fixture.MainAxisExtent(PinnedHeaderContent()));

        RenderObject content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(0, 0, 110, 110));
        tester.PumpAndSettle();
        // The header doesn't move. It would have expanded to 110 but minShowOnScreenExtent is 200,
        // preventing it from doing so.
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(200, fixture.MainAxisExtent(PinnedHeaderContent()));

        // ignoreLeading still works.
        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(-1, -1, 110, 110));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 15, fixture.Controller.Offset);
        Assert.Equal(200, fixture.MainAxisExtent(PinnedHeaderContent()));

        // Move the viewport so that its childExtent reaches 250.
        fixture.Controller.JumpTo(300.0 * 10 + 50.0);
        tester.PumpAndSettle();
        Assert.Equal(250, fixture.MainAxisExtent(PinnedHeaderContent()));

        // Doesn't move, doesn't expand or shrink, leading still ignored.
        content = PinnedHeaderContent().RenderObject!;
        content.ShowOnScreen(descendant: content, rect: new Rect(-1, -1, 110, 110));
        tester.PumpAndSettle();
        Assert.Equal(300.0 * 10 + 50.0, fixture.Controller.Offset);
        Assert.Equal(250, fixture.MainAxisExtent(PinnedHeaderContent()));
        fixture.Controller.Dispose();
    }

    // Flutter: 'animated: true, scrollDirection: $axis RenderViewportBase.showOnScreen should not
    // scroll if the rect is already visible, even if it does not scroll linearly (reversed order
    // version)'
    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void FloatingHeaderShowOnScreenShouldNotScrollIfTheRectIsAlreadyVisibleReversed(Axis axis)
    {
        using var tester = new FrameworkDartTester();
        var fixture = new FloatingHeaderShowOnScreenFixture(axis);
        tester.PumpWidget(fixture.BuildList(floatingHeader: fixture.Header(), reversed: true));

        fixture.Controller.JumpTo(-300.0 * 15);
        tester.PumpAndSettle();

        Element PinnedHeaderContent() => tester.ElementsWithKey(FloatingHeaderShowOnScreenFixture.HeaderKey).Single();

        // The persistent header is pinned to the leading edge thus still visible, the viewport
        // should not scroll.
        PinnedHeaderContent().RenderObject!.ShowOnScreen();
        tester.PumpAndSettle();
        Assert.Equal(-300.0 * 15, fixture.Controller.Offset);

        // children[9] will be partially obstructed by the persistent header, the viewport should
        // scroll to reveal it.
        fixture.Controller.JumpTo(
            -8 * 300.0 // Preceding headers 11 - 18, children[11]'s top edge is aligned to the leading edge.
            - 400.0 // Viewport height. children[10] (the pinned header) becomes pinned at the bottom of the screen.
            - 200.0 // Shrinks the pinned header to minExtent (100).
            - 100.0); // Obstructs the leading 100 pixels of the 11th header
        tester.PumpAndSettle();

        tester.ElementOfWidget(fixture.Children[9]).RenderObject!.ShowOnScreen();
        tester.PumpAndSettle();
        Assert.Equal(-8 * 300.0 - 400.0 - 200.0, fixture.Controller.Offset);
        fixture.Controller.Dispose();
    }

    // -------------------------------------------------------------------------------- helpers

    // rendering_tester.dart's `layout(root)`: an 800x600 render view laid out once.
    private static void Layout(RenderBox root)
    {
        var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = root };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(800, 600));
    }

    private static IReadOnlyList<RenderObject> AllRenderObjects(FrameworkDartTester tester)
    {
        var result = new List<RenderObject>();
        void Visit(RenderObject renderObject)
        {
            result.Add(renderObject);
            renderObject.VisitChildren(Visit);
        }

        Visit(tester.RenderView);
        return result;
    }

    private static Element Text(FrameworkDartTester tester, string text) =>
        tester.OnstageElementsWithText(text).Single();

    // find.ancestor(of: find.text('Sliver Persistent Header'), matching: find.byType(SizedBox)).
    private static Element SizedBoxAncestorOfHeaderText(FrameworkDartTester tester)
    {
        Element? result = null;
        Text(tester, "Sliver Persistent Header").VisitAncestorElements(ancestor =>
        {
            if (ancestor.Widget is SizedBox)
            {
                result = ancestor;
                return false;
            }

            return true;
        });
        return result!;
    }

    private static bool IsDescendantOf(Element element, Element ancestor)
    {
        bool found = false;
        element.VisitAncestorElements(candidate =>
        {
            found = ReferenceEquals(candidate, ancestor);
            return !found;
        });
        return found;
    }

    // tester.ensureSemantics(); flutter_test's view clears its semantics cache the way
    // semantics_tester.dart's SemanticsTester does.
    private static SemanticsHandleScope EnsureSemantics(FrameworkDartTester tester)
    {
        PipelineOwner owner = tester.RenderView.Owner!;
        bool createsOwner = owner.SemanticsOwner is null;
        SemanticsHandle handle = owner.EnsureSemantics();
        if (createsOwner)
        {
            tester.RenderView.ClearSemantics();
            tester.RenderView.ScheduleInitialSemantics();
        }

        return new SemanticsHandleScope(handle);
    }

    // The Dart tests end with `handle.dispose()`.
    private sealed class SemanticsHandleScope(SemanticsHandle handle) : IDisposable
    {
        public void Dispose() => handle.Dispose();
    }

    // flutter_test's frame runs `flushSemantics` after compositing; the C# tester's frame stops at
    // composite.
    private static void FlushSemantics(FrameworkDartTester tester) => tester.RenderView.Owner!.FlushSemantics();

    // find.semantics.byLabel(label).
    private static List<SemanticsNode> SemanticsNodesWithLabel(FrameworkDartTester tester, string label)
    {
        var result = new List<SemanticsNode>();
        void Visit(SemanticsNode node)
        {
            if (node.Label == label)
            {
                result.Add(node);
            }

            foreach (SemanticsNode child in node.Children)
            {
                Visit(child);
            }
        }

        Visit(tester.RenderView.Owner!.SemanticsOwner!.RootNode!);
        return result;
    }

    private sealed class TestDelegate(
        OverScrollHeaderStretchConfiguration? stretchConfiguration = null,
        PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        : SliverPersistentHeaderDelegate
    {
        public override double MaxExtent => 200.0;

        public override double MinExtent => 200.0;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new SizedBox(height: MaxExtent, child: new Text("Sliver Persistent Header"));
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;

        public override OverScrollHeaderStretchConfiguration? StretchConfiguration => stretchConfiguration;

        public override PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration =>
            showOnScreenConfiguration;
    }

    private sealed class TestDelegate2 : SliverPersistentHeaderDelegate
    {
        public override double MaxExtent => 200.0;

        public override double MinExtent => 100.0;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new Container(constraints: new BoxConstraints(MinHeight: MinExtent, MaxHeight: MaxExtent));
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    private sealed class TestDelegate3 : SliverPersistentHeaderDelegate
    {
        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new Container(height: 56, color: new Color(0xFFFF0000), child: new Text("Test Title"));
        }

        public override double MaxExtent => 56;

        public override double MinExtent => 56;

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    private sealed class TestDelegateThatCanThrow : SliverPersistentHeaderDelegate
    {
        public bool ShouldThrow { get; set; }

        public override double MaxExtent => ShouldThrow ? throw new FlutterError("Unavailable maxExtent") : 200.0;

        public override double MinExtent => ShouldThrow ? throw new FlutterError("Unavailable minExtent") : 100.0;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            return new Container(constraints: new BoxConstraints(MinHeight: MinExtent, MaxHeight: MaxExtent));
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => false;
    }

    private sealed class RenderBigSliver(double height) : RenderSliver
    {
        private double _height = height;

        public double Height
        {
            get => _height;
            set
            {
                if (value == _height)
                {
                    return;
                }

                _height = value;
                MarkNeedsLayout();
            }
        }

        private double PaintExtent =>
            Math.Clamp(Height - Constraints.ScrollOffset, 0.0, Constraints.RemainingPaintExtent);

        protected override void PerformLayout()
        {
            Geometry = new SliverGeometry(
                ScrollExtent: Height,
                PaintExtent: PaintExtent,
                MaxPaintExtent: Height);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class BigSliver(double height, Key? key = null) : LeafRenderObjectWidget(key)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RenderBigSliver(height);

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderBigSliver)renderObject).Height = height;
        }
    }

    // rendering/sliver_persistent_header_test.dart's test subclasses.
    private sealed class TestRenderSliverFloatingPersistentHeader(RenderBox child)
        : RenderSliverFloatingPersistentHeader(null, child: child, vsync: null)
    {
        public override double MaxExtent => 200;

        public override double MinExtent => 100;
    }

    private sealed class TestRenderSliverFloatingPinnedPersistentHeader(RenderBox child)
        : RenderSliverFloatingPinnedPersistentHeader(child: child, vsync: null, showOnScreenConfiguration: null)
    {
        public override double MaxExtent => 200;

        public override double MinExtent => 100;
    }

    private sealed class RenderSizedBox(Size size) : RenderBox
    {
        protected override bool SizedByParent => true;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(size);

        protected override void PerformResize()
        {
            Size = Constraints.Constrain(size);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    // viewport_test.dart's _TestSliverPersistentHeaderDelegate (its vsync defaults to TestVSync).
    private sealed class ViewportTestHeaderDelegate : SliverPersistentHeaderDelegate
    {
        private readonly Key? _key;

        public ViewportTestHeaderDelegate(
            double minExtent,
            double maxExtent,
            Key? key = null,
            ITickerProvider? vsync = null,
            PersistentHeaderShowOnScreenConfiguration? showOnScreenConfiguration = null)
        {
            _key = key;
            MinExtent = minExtent;
            MaxExtent = maxExtent;
            Vsync = vsync ?? new TestVSync();
            ShowOnScreenConfiguration = showOnScreenConfiguration ?? new PersistentHeaderShowOnScreenConfiguration();
        }

        public override double MaxExtent { get; }

        public override double MinExtent { get; }

        public override ITickerProvider? Vsync { get; }

        public override PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration { get; }

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent) =>
            SizedBox.Expand(key: _key);

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => true;
    }
}
