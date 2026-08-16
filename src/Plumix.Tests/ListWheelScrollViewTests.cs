using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/list_wheel_scroll_view.dart (parity
// regression tests mapped from flutter/packages/flutter/test/widgets/list_wheel_scroll_view_test.dart)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ListWheelScrollViewTests
{
    private static readonly Size Screen = new(800, 600);

    // ------------------------------------------------------------------ API and defaults

    [Fact]
    public void ListWheelScrollView_DefaultsMatchFlutter()
    {
        var view = new ListWheelScrollView(itemExtent: 20.0, children: [new SizedBox()]);

        Assert.Null(view.Controller);
        Assert.Null(view.Physics);
        Assert.Equal(2.0, view.DiameterRatio);
        Assert.Equal(0.003, view.Perspective);
        Assert.Equal(0.0, view.OffAxisFraction);
        Assert.False(view.UseMagnifier);
        Assert.Equal(1.0, view.Magnification);
        Assert.Equal(1.0, view.OverAndUnderCenterOpacity);
        Assert.Equal(20.0, view.ItemExtent);
        Assert.Equal(1.0, view.Squeeze);
        Assert.Null(view.OnSelectedItemChanged);
        Assert.False(view.RenderChildrenOutsideViewport);
        Assert.Equal(Clip.HardEdge, view.ClipBehavior);
        Assert.Equal(HitTestBehavior.Opaque, view.HitTestBehavior);
        Assert.Null(view.RestorationId);
        Assert.Null(view.ScrollBehavior);
        Assert.Equal(DragStartBehavior.Start, view.DragStartBehavior);
        Assert.Equal(ChangeReportingBehavior.OnScrollUpdate, view.ChangeReportingBehavior);
        Assert.IsType<ListWheelChildListDelegate>(view.ChildDelegate);
    }

    [Fact]
    public void ListWheelScrollView_NeedsPositiveDiameterRatio()
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListWheelScrollView(diameterRatio: -2.0, itemExtent: 20.0, children: []));
        Assert.Contains("You can't set a diameterRatio of 0", error.Message);
    }

    [Fact]
    public void ListWheelScrollView_NeedsPositiveMagnification()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListWheelScrollView(
            useMagnifier: true,
            magnification: -1.0,
            itemExtent: 20.0,
            children: [new Container()]));
    }

    [Fact]
    public void ListWheelScrollView_NeedsValidOverAndUnderCenterOpacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListWheelScrollView(
            overAndUnderCenterOpacity: -1, itemExtent: 20.0, children: [new Container()]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListWheelScrollView(
            overAndUnderCenterOpacity: 2, itemExtent: 20.0, children: [new Container()]));
        _ = new ListWheelScrollView(itemExtent: 20.0, children: [new Container()]);
        _ = new ListWheelScrollView(overAndUnderCenterOpacity: 0, itemExtent: 20.0, children: [new Container()]);
    }

    [Fact]
    public void ListWheelScrollView_RejectsRenderingOutsideAClippedViewport()
    {
        Assert.Throws<ArgumentException>(() => new ListWheelScrollView(
            itemExtent: 20.0,
            renderChildrenOutsideViewport: true,
            children: []));
        _ = new ListWheelScrollView(
            itemExtent: 20.0,
            renderChildrenOutsideViewport: true,
            clipBehavior: Clip.None,
            children: []);
    }

    [Fact]
    public void RenderListWheelViewport_ValidatesItsSetters()
    {
        var offset = new ScrollPosition(new ClampingScrollPhysics(), new TestScrollContext());
        var viewport = new RenderListWheelViewport(new NoChildManager(), offset, itemExtent: 10);
        Assert.Equal(Clip.None, viewport.ClipBehavior);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.DiameterRatio = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.Perspective = 0.02);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.Magnification = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.OverAndUnderCenterOpacity = 1.5);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.ItemExtent = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewport.Squeeze = 0);
        Assert.Equal(3, viewport.ScrollOffsetToIndex(35));
        Assert.Equal(-4, viewport.ScrollOffsetToIndex(-35));
        Assert.Equal(70, viewport.IndexToScrollOffset(7));
    }

    [Fact]
    public void FixedExtentScrollController_DebugLabelShowsInToString()
    {
        var controller = new FixedExtentScrollController(debugLabel: "MyCustomWidget");
        Assert.Equal("MyCustomWidget", controller.DebugLabel);
        Assert.Contains("MyCustomWidget", controller.ToString());
        Assert.Contains("no clients", controller.ToString());
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_SelectedItemAssertsWhenUnattached()
    {
        var controller = new FixedExtentScrollController();
        Assert.Equal(0, controller.InitialItem);
        Assert.True(controller.KeepScrollOffset);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => controller.SelectedItem);
        Assert.Equal(
            "FixedExtentScrollController.selectedItem cannot be accessed before a scroll view is built with it.",
            error.Message);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentMetrics_CopyWithKeepsTheItemIndexUnlessOverridden()
    {
        var metrics = new FixedExtentMetrics(0, 900, 300, 600, AxisDirection.Down, 3, 1.0);
        Assert.Equal(3, metrics.ItemIndex);
        Assert.Equal(3, metrics.CopyWith(pixels: 400).ItemIndex);
        Assert.Equal(400, metrics.CopyWith(pixels: 400).Pixels);
        Assert.Equal(7, metrics.CopyWith(itemIndex: 7).ItemIndex);
    }

    [Fact]
    public void ListWheelChildDelegates_FollowDartIndexing()
    {
        Widget[] children = [new SizedBox(), new SizedBox(), new SizedBox()];
        var list = new ListWheelChildListDelegate(children);
        Assert.Equal(3, list.EstimatedChildCount);
        Assert.Equal(5, list.TrueIndexOf(5));
        Assert.False(list.ShouldRebuild(new ListWheelChildListDelegate(children)));
        Assert.True(list.ShouldRebuild(new ListWheelChildListDelegate([new SizedBox()])));

        var looping = new ListWheelChildLoopingListDelegate(children);
        Assert.Null(looping.EstimatedChildCount);
        Assert.Equal(1, looping.TrueIndexOf(-5));
        Assert.Equal(2, looping.TrueIndexOf(5));

        var builder = new ListWheelChildBuilderDelegate((_, index) => index < 0 ? null : new SizedBox());
        Assert.Null(builder.EstimatedChildCount);
        Assert.Null(builder.ChildCount);
        var counted = new ListWheelChildBuilderDelegate((_, _) => new SizedBox(), childCount: 4);
        Assert.Equal(4, counted.EstimatedChildCount);
        Assert.True(counted.ShouldRebuild(builder));
        Assert.False(counted.ShouldRebuild(counted));
    }

    // ------------------------------------------------------------------ layout

    [Fact]
    public void ListWheelScrollView_CanHaveZeroChild()
    {
        using var harness = Harness(new ListWheelScrollView(itemExtent: 50.0, children: []));
        harness.Pump(Screen);
        Assert.Equal(new Size(800.0, 600.0), Viewport(harness).Size);
        Assert.Equal(0, Viewport(harness).ChildCount);
    }

    [Fact]
    public void ListWheelScrollView_TakesParentsSizeWithSmallAndLargeChildren()
    {
        using var small = Harness(new ListWheelScrollView(itemExtent: 50.0, children: [new Container(height: 50.0)]));
        small.Pump(Screen);
        Assert.Equal(new Size(800.0, 600.0), Viewport(small).Size);
        Assert.Equal(default, Viewport(small).GetPaintOffsetToRoot());

        using var large = Harness(new ListWheelScrollView(
            itemExtent: 50.0,
            children: Enumerable.Range(0, 100).Select(_ => (Widget)new Container(height: 50.0)).ToArray()));
        large.Pump(Screen);
        Assert.Equal(new Size(800.0, 600.0), Viewport(large).Size);
    }

    [Fact]
    public void ListWheelScrollView_ChildrenCannotBeBiggerThanItemExtent()
    {
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 50.0,
            children: [new SizedBox(width: 200.0, height: 200.0, child: new ColoredBox(Colors.Red))]));
        harness.Pump(Screen);
        RenderBox child = Assert.Single(Children(Viewport(harness)));
        Assert.Equal(new Size(200.0, 50.0), child.Size);
    }

    [Fact]
    public void ListWheelScrollView_ActiveChildrenAreLaidOutWithCorrectOffset()
    {
        foreach (double width in new[] { 200.0, 100.0, 300.0 })
        {
            using var harness = Harness(new ListWheelScrollView(
                itemExtent: 100.0,
                children: [new SizedBox(width: width, child: new ColoredBox(Colors.Red))]));
            harness.Pump(Screen);
            RenderBox child = Assert.Single(Children(Viewport(harness)));
            Assert.Equal(width, child.Size.Width);
            Assert.Equal(400.0, child.GetPaintOffsetToRoot().X + (child.Size.Width / 2), precision: 6);
        }
    }

    [Fact]
    public void ListWheelScrollView_ZeroSizedHostDoesNotThrow()
    {
        using var harness = Harness(new Center(child: new SizedBox(
            width: 0,
            height: 0,
            child: new ListWheelScrollView(
                itemExtent: 20.0,
                children: Enumerable.Range(0, 20).Select(_ => (Widget)new Container()).ToArray()))));
        harness.Pump(Screen);
        Assert.Equal(new Size(0, 0), Viewport(harness).Size);
    }

    [Fact]
    public void ListWheelScrollView_OnlyVisibleChildrenAreMaintainedAsChildrenOfTheViewport()
    {
        var controller = new FixedExtentScrollController();
        using var harness = Harness(Wheel(controller, 16, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal(4, Viewport(harness).ChildCount);

        controller.JumpToItem(8);
        harness.Pump(Screen);
        Assert.Equal(7, Viewport(harness).ChildCount);

        controller.JumpToItem(15);
        harness.Pump(Screen);
        Assert.Equal(4, Viewport(harness).ChildCount);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ATighterSqueezeLaysOutMoreChildren()
    {
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(Wheel(controller, 20, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal(7, Viewport(harness).ChildCount);

        harness.Replace(Wheel(controller, 20, itemExtent: 100.0, squeeze: 2));
        harness.Pump(Screen);
        Assert.Equal(13, Viewport(harness).ChildCount);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_StartsAndEndsFromTheMiddle()
    {
        var controller = new ScrollController();
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3], PaintedIndices(harness));

        controller.JumpTo(1000.0);
        harness.Pump(Screen);
        Assert.Equal([7, 8, 9, 10, 11, 12, 13], PaintedIndices(harness));

        controller.JumpTo(9900.0);
        harness.Pump(Screen);
        Assert.Equal([96, 97, 98, 99], PaintedIndices(harness));
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_AChildGetsPaintedAsSoonAsItsFirstPixelIsInTheViewport()
    {
        var controller = new ScrollController(initialScrollOffset: 50.0);
        using var harness = Harness(Wheel(controller, 10, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3], PaintedIndices(harness));

        controller.JumpTo(51.0);
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3, 4], PaintedIndices(harness));
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_AChildIsNoLongerPaintedAfterItsLastPixelLeavesTheViewport()
    {
        var controller = new ScrollController(initialScrollOffset: 250.0);
        using var harness = Harness(Wheel(controller, 10, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3, 4, 5], PaintedIndices(harness));

        controller.JumpTo(349.0);
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], PaintedIndices(harness));

        controller.JumpTo(350.0);
        harness.Pump(Screen);
        Assert.Equal([1, 2, 3, 4, 5, 6], PaintedIndices(harness));
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_InfiniteLoopingList()
    {
        var controller = new FixedExtentScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            childDelegate: new ListWheelChildLoopingListDelegate(
                Enumerable.Range(0, 10)
                    .Select(_ => (Widget)new SizedBox(width: 400.0, height: 100.0, child: new ColoredBox(Colors.Red)))
                    .ToArray())));
        harness.Pump(Screen);

        // The wheel is centered on item 0 with the last looped item just above it.
        RenderBox item0 = ChildAt(harness, 0);
        AssertOffset(new Point(200.0, 250.0), item0.GetPaintOffsetToRoot());
        RenderBox item9 = ChildAt(harness, -1);
        Assert.InRange(item9.GetPaintOffsetToRoot().X, 200.0 - 15.0, 200.0 + 15.0);
        Assert.InRange(item9.GetPaintOffsetToRoot().Y, 150.0 - 15.0, 150.0 + 15.0);

        controller.JumpTo(1000.0);
        harness.Pump(Screen);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 10).GetPaintOffsetToRoot());
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_InfiniteChildBuilder()
    {
        var controller = new FixedExtentScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            childDelegate: new ListWheelChildBuilderDelegate(
                (_, _) => new SizedBox(width: 400.0, height: 100.0, child: new ColoredBox(Colors.Red)))));
        harness.Pump(Screen);

        controller.JumpTo(-100000.0);
        harness.Pump(Screen);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, -1000).GetPaintOffsetToRoot());

        controller.JumpTo(100000.0);
        harness.Pump(Screen);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 1000).GetPaintOffsetToRoot());
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ChildBuilderWithLowerAndUpperLimits()
    {
        var controller = new FixedExtentScrollController(initialItem: -10);
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            physics: new FixedExtentScrollPhysics(),
            itemExtent: 100.0,
            childDelegate: new ListWheelChildBuilderDelegate((_, index) =>
                index < -15 || index > -5 ? null : new SizedBox(width: 400.0, height: 100.0))));
        harness.Pump(Screen);
        Assert.Equal([-13, -12, -11, -10, -9, -8, -7], PaintedIndices(harness));

        Fling(harness, new Point(400, 300), new Vector(0, 1000), 1000);
        Settle(harness);
        Assert.Equal(-15, controller.SelectedItem);

        Fling(harness, new Point(400, 300), new Vector(0, -1000), 1000);
        Settle(harness);
        Assert.Equal(-5, controller.SelectedItem);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_HighVelocityFlingsDoNotBreakTheChildLimits()
    {
        var controller = new FixedExtentScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            physics: new FixedExtentScrollPhysics(),
            itemExtent: 400.0,
            childDelegate: new ListWheelChildBuilderDelegate((_, index) =>
                index < 0 || index > 5 ? null : new SizedBox(width: 400.0, height: 400.0))));
        harness.Pump(Screen);
        Assert.Equal([0, 1], LaidOutIndices(harness));
        Assert.Equal(0, controller.SelectedItem);

        Fling(harness, new Point(400, 300), new Vector(0, 40000), 8000);
        Settle(harness);
        Assert.Equal(0, controller.SelectedItem);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ChildDelegateUpdate()
    {
        var controller = new FixedExtentScrollController();
        static Widget Build(FixedExtentScrollController controller, int childCount) => new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            childDelegate: new ListWheelChildBuilderDelegate(
                (_, _) => new SizedBox(width: 400.0, height: 100.0),
                childCount: childCount));

        using var harness = Harness(Build(controller, 1));
        harness.Pump(Screen);
        Assert.Equal([0], LaidOutIndices(harness));

        harness.Replace(Build(controller, 2));
        harness.Pump(Screen);
        Assert.Equal([0, 1], LaidOutIndices(harness));
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ChildDelegateUpdateShrinksAndGrowsTheWindow()
    {
        var controller = new FixedExtentScrollController(initialItem: 2);
        static Widget Build(FixedExtentScrollController controller, int childCount) => new ListWheelScrollView(
            controller: controller,
            itemExtent: 400.0,
            childDelegate: new ListWheelChildBuilderDelegate(
                (_, _) => new SizedBox(width: 400.0, height: 400.0),
                childCount: childCount));

        using var harness = Harness(Build(controller, 5));
        harness.Pump(Screen);
        Assert.Equal([1, 2, 3], LaidOutIndices(harness));

        harness.Replace(Build(controller, 2));
        harness.Pump(Screen);
        Assert.Equal([0, 1], LaidOutIndices(harness));

        harness.Replace(Build(controller, 5));
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2], LaidOutIndices(harness));

        controller.JumpTo(controller.Offset + 1200);
        harness.Pump(Screen);
        Assert.Equal([3, 4], LaidOutIndices(harness));

        harness.Replace(Build(controller, 2));
        harness.Pump(Screen);
        Assert.Equal([0, 1], LaidOutIndices(harness));
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_BuilderIsNeverCalledTwiceForTheSameIndex()
    {
        var builtChildren = new HashSet<int>();
        var controller = new FixedExtentScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            childDelegate: new ListWheelChildBuilderDelegate((_, index) =>
            {
                Assert.True(builtChildren.Add(index));
                return new SizedBox(width: 400.0, height: 100.0);
            })));
        harness.Pump(Screen);

        controller.JumpTo(-10000.0);
        harness.Pump(Screen);
        controller.JumpTo(10000.0);
        harness.Pump(Screen);
        controller.JumpTo(-10000.0);
        harness.Pump(Screen);
        controller.Dispose();
    }

    // ------------------------------------------------------------------ paint

    [Fact]
    public void ListWheelScrollView_RespectsClipBehavior()
    {
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 2000.0,
            children: [new Container(height: 2000.0, width: 2000.0, color: Colors.Red)]));
        harness.Pump(Screen);
        Assert.Equal(Clip.HardEdge, Viewport(harness).ClipBehavior);
        Assert.Equal(Clip.HardEdge, Assert.Single(FindLayers<ClipRectLayer>(harness)).ClipBehavior);

        harness.Replace(new ListWheelScrollView(
            itemExtent: 2000.0,
            clipBehavior: Clip.AntiAlias,
            children: [new Container(height: 2000.0, width: 2000.0, color: Colors.Red)]));
        harness.Pump(Screen);
        Assert.Equal(Clip.AntiAlias, Viewport(harness).ClipBehavior);
        Assert.Equal(Clip.AntiAlias, Assert.Single(FindLayers<ClipRectLayer>(harness)).ClipBehavior);
    }

    [Fact]
    public void ListWheelScrollView_DefaultMiddleTransform()
    {
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 100.0,
            children: [new SizedBox(width: 200.0, child: new ColoredBox(Colors.Red))]));
        harness.Pump(Screen);

        TransformLayer layer = Assert.Single(FindLayers<TransformLayer>(harness));
        AssertMatrix(
            [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, -1.2, -0.9, 1.0, -0.003, 0.0, 0.0, 0.0, 1.0],
            layer.Transform);
    }

    [Fact]
    public void ListWheelScrollView_ScrollingDiameterRatioAndPerspectiveAllChangeTheMatrix()
    {
        var controller = new ScrollController(initialScrollOffset: 200.0);
        using var harness = Harness(Wheel(controller, 1, itemExtent: 100.0));
        harness.Pump(Screen);
        AssertMatrix(
        [
            1.0, 0.0, 0.0, 0.0,
            -0.41042417199080244, 0.6318744917928065, 0.3420201433256687, -0.0010260604299770061,
            -1.12763114494309, -1.1877435020329863, 0.9396926207859084, -0.0028190778623577253,
            166.54856463138663, -62.20844875763376, -138.79047052615562, 1.4163714115784667,
        ], Assert.Single(FindLayers<TransformLayer>(harness)).Transform);

        harness.Replace(Wheel(controller, 1, itemExtent: 100.0, diameterRatio: 3.0));
        harness.Pump(Screen);
        AssertMatrix(
        [
            1.0, 0.0, 0.0, 0.0,
            -0.26954971336161726, 0.7722830529455648, 0.22462476113468105, -0.0006738742834040432,
            -1.1693344055601331, -1.101625565304781, 0.9744453379667777, -0.002923336013900333,
            108.46394900436536, -113.14792465797223, -90.38662417030434, 1.2711598725109134,
        ], Assert.Single(FindLayers<TransformLayer>(harness)).Transform);

        harness.Replace(Wheel(controller, 1, itemExtent: 100.0, perspective: 0.0001));
        harness.Pump(Screen);
        AssertMatrix(
        [
            1.0, 0.0, 0.0, 0.0,
            -0.01368080573302675, 0.9294320164861384, 0.3420201433256687, -0.000034202014332566874,
            -0.03758770483143634, -0.370210921949246, 0.9396926207859084, -0.00009396926207859085,
            5.551618821046304, -182.95615811538906, -138.79047052615562, 1.0138790470526158,
        ], Assert.Single(FindLayers<TransformLayer>(harness)).Transform);

        harness.Replace(Wheel(controller, 1, itemExtent: 100.0));
        controller.JumpTo(300.0);
        harness.Pump(Screen);
        AssertMatrix(
        [
            1.0, 0.0, 0.0, 0.0,
            -0.6, 0.41602540378443875, 0.5, -0.0015,
            -1.0392304845413265, -1.2794228634059948, 0.8660254037844387, -0.0025980762113533163,
            276.46170927520404, -52.46133917892857, -230.38475772933677, 1.69115427318801,
        ], Assert.Single(FindLayers<TransformLayer>(harness)).Transform);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_OffAxisFractionAndMagnificationChangeTheMatrix()
    {
        var controller = new ScrollController(initialScrollOffset: 200.0);
        using var harness = Harness(Wheel(controller, 1, itemExtent: 100.0, offAxisFraction: 0.5));
        harness.Pump(Screen);
        AssertMatrix(
        [
            1.0, 0.0, 0.0, 0.0,
            0.0, 0.6318744917928063, 0.3420201433256688, -0.0010260604299770066,
            0.0, -1.1877435020329863, 0.9396926207859083, -0.002819077862357725,
            0.0, -62.20844875763376, -138.79047052615562, 1.4163714115784667,
        ], Assert.Single(FindLayers<TransformLayer>(harness)).Transform);

        controller.JumpTo(0.0);
        harness.Replace(Wheel(
            controller,
            1,
            itemExtent: 100.0,
            offAxisFraction: 0.5,
            useMagnifier: true,
            magnification: 1.5));
        harness.Pump(Screen);
        // The magnified center pass is painted first, so it is the first transform in paint order.
        AssertMatrix(
            [1.5, 0.0, 0.0, 0.0, 0.0, 1.5, 0.0, 0.0, 0.0, 0.0, 1.5, 0.0, 0.0, -150.0, 0.0, 1.0],
            FindLayers<TransformLayer>(harness).First().Transform);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_CreatesOnlyOneOpacityLayerForAllChildren()
    {
        using var harness = Harness(new ListWheelScrollView(
            overAndUnderCenterOpacity: 0.5,
            itemExtent: 20.0,
            children: Enumerable.Range(0, 20).Select(_ => (Widget)new Container()).ToArray()));
        harness.Pump(Screen);
        OpacityLayer layer = Assert.Single(FindLayers<OpacityLayer>(harness));
        Assert.Equal(128.0 / 255.0, layer.Opacity, precision: 6);
    }

    // ------------------------------------------------------------------ selection reporting

    [Fact]
    public void ListWheelScrollView_NoOnSelectedItemChangedCallbackOnFirstBuild()
    {
        bool callbackCalled = false;
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 100.0,
            onSelectedItemChanged: _ => callbackCalled = true,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);
        Assert.False(callbackCalled);
    }

    [Fact]
    public void ListWheelScrollView_OnSelectedItemChangedWhenANewItemIsClosestToCenter()
    {
        var selectedItems = new List<int>();
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 100.0,
            onSelectedItemChanged: selectedItems.Add,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);

        var gesture = new Gesture(harness, new Point(10.0, 10.0));
        gesture.MoveBy(new Vector(0.0, -49.0));
        harness.Pump(Screen);
        Assert.Empty(selectedItems);

        gesture.MoveBy(new Vector(0.0, -1.0));
        harness.Pump(Screen);
        Assert.Equal([1], selectedItems);

        gesture.MoveBy(new Vector(0.0, -99.0));
        harness.Pump(Screen);
        Assert.Equal([1], selectedItems);

        gesture.MoveBy(new Vector(0.0, -1.0));
        harness.Pump(Screen);
        Assert.Equal([1, 2], selectedItems);

        gesture.MoveBy(new Vector(0.0, 50.0));
        harness.Pump(Screen);
        Assert.Equal([1, 2, 1], selectedItems);
        gesture.Up();
    }

    [Fact]
    public void ListWheelScrollView_OnSelectedItemChangedWithOnScrollEndReporting()
    {
        var selectedItems = new List<int>();
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 100.0,
            changeReportingBehavior: ChangeReportingBehavior.OnScrollEnd,
            onSelectedItemChanged: selectedItems.Add,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);

        var gesture = new Gesture(harness, new Point(10.0, 10.0));
        gesture.MoveBy(new Vector(0.0, -49.0));
        harness.Pump(Screen);
        Assert.Empty(selectedItems);
        gesture.MoveBy(new Vector(0.0, -1.0));
        harness.Pump(Screen);
        Assert.Empty(selectedItems);
        gesture.MoveBy(new Vector(0.0, -99.0));
        harness.Pump(Screen);
        Assert.Empty(selectedItems);
        gesture.MoveBy(new Vector(0.0, -1.0));
        gesture.Up();
        Settle(harness);
        Assert.Equal([2], selectedItems);

        gesture = new Gesture(harness, new Point(10.0, 10.0));
        gesture.MoveBy(new Vector(0.0, 100.0));
        harness.Pump(Screen);
        Assert.Equal([2], selectedItems);
        gesture.Up();
        Settle(harness);
        Assert.Equal([2, 1], selectedItems);
    }

    [Fact]
    public void ListWheelScrollView_OnSelectedItemChangedReportsOnlyInValidRange()
    {
        var selectedItems = new List<int>();
        using var harness = Harness(new ListWheelScrollView(
            itemExtent: 100.0,
            onSelectedItemChanged: selectedItems.Add,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);

        var gesture = new Gesture(harness, new Point(10.0, 10.0));
        // Scroll into overscroll before the start of the list.
        gesture.MoveBy(new Vector(0.0, 70.0));
        harness.Pump(Screen);
        for (double verticalOffset = 0.0; verticalOffset > -2000.0; verticalOffset -= 10.0)
        {
            gesture.MoveTo(new Point(0.0, verticalOffset));
            harness.Pump(Screen);
        }

        // Item 0 was never reported and nothing past the last item was either.
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], selectedItems);
        gesture.Up();
    }

    [Fact]
    public void ListWheelScrollView_OnSelectedItemChangedAndControllerAreInSync()
    {
        var selectedItems = new List<int>();
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            onSelectedItemChanged: selectedItems.Add,
            children: Enumerable.Range(0, 100).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);

        var gesture = new Gesture(harness, new Point(10.0, 10.0));
        gesture.MoveBy(new Vector(0.0, -49.0));
        harness.Pump(Screen);
        Assert.Empty(selectedItems);
        Assert.Equal(10, controller.SelectedItem);

        gesture.MoveBy(new Vector(0.0, -1.0));
        harness.Pump(Screen);
        Assert.Equal([11], selectedItems);
        Assert.Equal(11, controller.SelectedItem);

        gesture.MoveBy(new Vector(0.0, 70.0));
        harness.Pump(Screen);
        Assert.Equal([11, 10], selectedItems);
        Assert.Equal(10, controller.SelectedItem);
        gesture.Up();
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ReportsTrueIndexForLoopingDelegates()
    {
        var selectedItems = new List<int>();
        var controller = new FixedExtentScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            onSelectedItemChanged: selectedItems.Add,
            childDelegate: new ListWheelChildLoopingListDelegate(
                Enumerable.Range(0, 4).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray())));
        harness.Pump(Screen);

        controller.JumpToItem(-3);
        harness.Pump(Screen);
        Assert.Equal([1], selectedItems);
        Assert.Equal(-3, controller.SelectedItem);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_PlainScrollControllerDisablesSelectionReporting()
    {
        var selectedItems = new List<int>();
        var controller = new ScrollController();
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            onSelectedItemChanged: selectedItems.Add,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);
        controller.JumpTo(300.0);
        harness.Pump(Screen);
        Assert.Empty(selectedItems);
        controller.Dispose();
    }

    // ------------------------------------------------------------------ controller

    [Fact]
    public void FixedExtentScrollController_OnAttachAndOnDetach()
    {
        int attach = 0;
        int detach = 0;
        var controller = new FixedExtentScrollController(
            onAttach: _ => attach++,
            onDetach: _ => detach++);
        using var harness = Harness(Wheel(controller, 0, itemExtent: 50.0));
        harness.Pump(Screen);
        Assert.Equal(1, attach);
        Assert.Equal(0, detach);

        harness.Replace(new Container());
        harness.Pump(Screen);
        Assert.Equal(1, attach);
        Assert.Equal(1, detach);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_InitialItem()
    {
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal([7, 8, 9, 10, 11, 12, 13], PaintedIndices(harness));
        Assert.Equal(10, controller.SelectedItem);
        Assert.Equal(1000.0, controller.Offset);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_InitialItem_IsKnownBeforeTheFirstLayout()
    {
        // Dart's _FixedExtentScrollPosition computes `initialPixels: itemExtent * initialItem` in its
        // constructor from the ScrollContext, so the offset is available as soon as the position
        // is attached, before any layout.
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        Assert.True(controller.HasClients);
        Assert.Equal(1000.0, controller.Offset);
        Assert.IsType<FixedExtentScrollableState>(controller.Position.Context);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_RejectsScrollablesThatAreNotListWheels()
    {
        // Flutter: "FixedExtentScrollController can only be used with ListWheelScrollViews".
        var controller = new FixedExtentScrollController();
        Exception error = Assert.ThrowsAny<Exception>(() => Harness(
            ListView.Builder(
                controller: controller,
                itemCount: 3,
                itemExtent: 100,
                itemBuilder: (_, _) => new SizedBox(height: 100))));
        while (error.InnerException is { } inner)
        {
            error = inner;
        }

        Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("can only be used with ListWheelScrollViews", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FixedExtentScrollController_JumpToItem()
    {
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);

        controller.JumpToItem(0);
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3], PaintedIndices(harness));
        Assert.Equal(0, controller.SelectedItem);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_AnimateToItem()
    {
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);

        double clock = Scheduler.CurrentSeconds;
        _ = controller.AnimateToItem(0, TimeSpan.FromSeconds(1), Curves.Linear);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        harness.Pump(Screen);
        Assert.NotEqual(0, controller.SelectedItem);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.5));
        harness.Pump(Screen);
        Assert.InRange(controller.Offset, 1.0, 999.0);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 1.05));
        harness.Pump(Screen);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 1.1));
        harness.Pump(Screen);
        Assert.Equal([0, 1, 2, 3], PaintedIndices(harness));
        Assert.Equal(0, controller.SelectedItem);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_IsHotSwappable()
    {
        using var harness = Harness(Wheel(null, 100, itemExtent: 100.0));
        harness.Pump(Screen);

        var gesture = new Gesture(harness, new Point(10.0, 10.0));
        gesture.MoveBy(new Vector(0.0, -500.0));
        gesture.Up();
        Settle(harness);
        Assert.Equal(5, ((FixedExtentScrollPosition)Viewport(harness).Offset).ItemIndex);

        var controller1 = new FixedExtentScrollController(initialItem: 30);
        harness.Replace(Wheel(controller1, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        // initialItem is ignored: the new position absorbed the old one.
        Assert.Equal(5, controller1.SelectedItem);

        controller1.JumpToItem(50);
        harness.Pump(Screen);
        Assert.Equal(50, controller1.SelectedItem);
        Assert.Equal(5000.0, controller1.Position.Pixels);

        var controller2 = new FixedExtentScrollController(initialItem: 33);
        harness.Replace(Wheel(controller2, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.False(controller1.HasClients);
        Assert.Equal(50, controller2.SelectedItem);

        controller2.JumpToItem(40);
        harness.Pump(Screen);
        Assert.Equal(40, controller2.SelectedItem);
        Assert.Equal(4000.0, controller2.Position.Pixels);

        harness.Replace(Wheel(null, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.False(controller1.HasClients);
        Assert.False(controller2.HasClients);
        controller1.Dispose();
        controller2.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_CanBeReused()
    {
        var controller = new FixedExtentScrollController(initialItem: 3);
        using var harness = Harness(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.Equal(3, controller.SelectedItem);
        Assert.Equal(300.0, controller.Position.Pixels);

        controller.JumpToItem(10);
        harness.Pump(Screen);
        Assert.Equal(10, controller.SelectedItem);
        Assert.Equal(1000.0, controller.Position.Pixels);

        harness.Replace(new Center());
        harness.Pump(Screen);
        Assert.False(controller.HasClients);

        harness.Replace(Wheel(controller, 100, itemExtent: 100.0));
        harness.Pump(Screen);
        Assert.True(controller.HasClients);
        Assert.Equal(3, controller.SelectedItem);
        Assert.Equal(300.0, controller.Position.Pixels);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollController_KeepScrollOffsetRoundTripsThroughPageStorage()
    {
        var bucket = new PageStorageBucket();
        static Widget BuildFrame(PageStorageBucket bucket, ScrollController controller) => new PageStorage(
            bucket: bucket,
            child: new KeyedSubtree(
                key: new PageStorageKey<string>("ListWheelScrollView"),
                child: new ListWheelScrollView(
                    key: new ObjectKey(new object()),
                    itemExtent: 100.0,
                    controller: controller,
                    children: Enumerable.Range(0, 100)
                        .Select(_ => (Widget)new SizedBox(
                            height: 100.0,
                            width: 400.0,
                            child: new ColoredBox(Colors.Red)))
                        .ToArray())));

        var controller = new FixedExtentScrollController(initialItem: 2);
        using var harness = Harness(BuildFrame(bucket, controller));
        harness.Pump(Screen);
        Assert.Equal(2, controller.SelectedItem);
        Assert.Equal(200.0, controller.Offset);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 2).GetPaintOffsetToRoot());

        controller.JumpToItem(20);
        harness.Pump(Screen);
        Assert.Equal(20, controller.SelectedItem);
        Assert.Equal(2000.0, controller.Offset);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 20).GetPaintOffsetToRoot());
        controller.Dispose();

        controller = new FixedExtentScrollController(initialItem: 25);
        harness.Replace(BuildFrame(bucket, controller));
        harness.Pump(Screen);
        Assert.Equal(20, controller.SelectedItem);
        Assert.Equal(2000.0, controller.Offset);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 20).GetPaintOffsetToRoot());
        controller.Dispose();

        controller = new FixedExtentScrollController(keepScrollOffset: false, initialItem: 10);
        harness.Replace(BuildFrame(bucket, controller));
        harness.Pump(Screen);
        Assert.Equal(10, controller.SelectedItem);
        Assert.Equal(1000.0, controller.Offset);
        AssertOffset(new Point(200.0, 250.0), ChildAt(harness, 10).GetPaintOffsetToRoot());
        controller.Dispose();
    }

    // ------------------------------------------------------------------ physics

    [Fact]
    public void FixedExtentScrollPhysics_FlingVelocitiesTooLowSnapBackToTheSameItem()
    {
        var scrolledPositions = new List<double>();
        var controller = new FixedExtentScrollController(initialItem: 40);
        using var harness = Harness(new NotificationListener<ScrollUpdateNotification>(
            onNotification: notification =>
            {
                scrolledPositions.Add(notification.Metrics.Pixels);
                return false;
            },
            child: new ListWheelScrollView(
                controller: controller,
                physics: new FixedExtentScrollPhysics(),
                itemExtent: 1000.0,
                children: Enumerable.Range(0, 100).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray())));
        harness.Pump(Screen);

        Fling(harness, new Point(400, 300), new Vector(0.0, -50.0), 800.0);
        harness.Pump(Screen);
        Assert.Equal(40, controller.SelectedItem);
        Assert.NotEmpty(scrolledPositions);
        // Plumix's recognizer starts the drag on the sample that crosses the slop, so a couple of
        // pixels of the first samples are lost; Flutter lands on exactly +50.
        Assert.Equal((40 * 1000.0) + 50.0, controller.Offset, 3.0);
        int flingCount = scrolledPositions.Count;

        // Flutter's test pumps one frame and then a single one-second frame, which is where the
        // spring's remaining fraction of a pixel goes below the asserted 0.2.
        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.016));
        harness.Pump(Screen);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 1.016));
        harness.Pump(Screen);
        Assert.True(scrolledPositions.Count > flingCount);
        Assert.Equal(40, controller.SelectedItem);
        Assert.Equal(40000.0, scrolledPositions[^1], 0.2);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollPhysics_HighFlingVelocitiesLandExactlyOnItems()
    {
        var scrolledPositions = new List<double>();
        var controller = new FixedExtentScrollController(initialItem: 40);
        using var harness = Harness(new NotificationListener<ScrollUpdateNotification>(
            onNotification: notification =>
            {
                scrolledPositions.Add(notification.Metrics.Pixels);
                return false;
            },
            child: new ListWheelScrollView(
                controller: controller,
                physics: new FixedExtentScrollPhysics(),
                itemExtent: 100.0,
                children: Enumerable.Range(0, 100).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray())));
        harness.Pump(Screen);

        Fling(harness, new Point(400, 300), new Vector(0.0, -567.0), 678.0);
        harness.Pump(Screen);
        Assert.Equal(46, controller.SelectedItem);
        Assert.Equal((40 * 100.0) + 567.0, controller.Offset, 5.0);

        Settle(harness);
        // The friction simulation is tuned to land exactly on an item.
        Assert.Equal(controller.SelectedItem * 100.0, scrolledPositions[^1], 0.3);
        Assert.Equal(controller.SelectedItem * 100.0, controller.Offset, 0.3);
        Assert.True(controller.SelectedItem > 46);
        controller.Dispose();
    }

    [Fact]
    public void FixedExtentScrollPhysics_ScenariosMatchFlutter()
    {
        var controller = new FixedExtentScrollController(initialItem: 3);
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            physics: new FixedExtentScrollPhysics(),
            itemExtent: 100.0,
            children: Enumerable.Range(0, 10).Select(_ => (Widget)new ColoredBox(Colors.Red)).ToArray()));
        harness.Pump(Screen);
        var position = (FixedExtentScrollPosition)controller.Position;
        var physics = new FixedExtentScrollPhysics().ApplyTo(new ClampingScrollPhysics());

        // Scenario 3: at rest on an item, nothing to do.
        Assert.Null(physics.CreateBallisticSimulation(position, 0.0));

        // Scenario 4: too little velocity to leave the item -> spring back to it.
        Simulation? spring = physics.CreateBallisticSimulation(position, 30.0);
        Assert.IsType<SpringSimulation>(spring);
        Assert.Equal(300.0, spring!.X(10), precision: 3);

        // Scenario 5: enough velocity -> friction tuned to land on the settling item.
        Simulation? friction = physics.CreateBallisticSimulation(position, 2000.0);
        Assert.IsType<FrictionSimulation>(friction);
        Assert.True(friction!.X(double.PositiveInfinity) > 300.0);

        // Wrong position type is rejected.
        var plain = new ScrollPosition(new ClampingScrollPhysics(), new TestScrollContext());
        Assert.Throws<InvalidOperationException>(() => physics.CreateBallisticSimulation(plain, 100.0));
        Assert.IsType<FixedExtentScrollPhysics>(physics);
        Assert.IsType<ClampingScrollPhysics>(physics.Parent);
        controller.Dispose();
    }

    // ------------------------------------------------------------------ reveal and hit testing

    [Fact]
    public void ListWheelScrollView_GetOffsetToReveal()
    {
        var innerChildren = new Widget[10];
        var controller = new ScrollController(initialScrollOffset: 300.0);
        using var harness = Harness(new Center(child: new SizedBox(
            height: 500.0,
            width: 300.0,
            child: new ListWheelScrollView(
                controller: controller,
                itemExtent: 100.0,
                children: Enumerable.Range(0, 10).Select(i => (Widget)new Center(
                    child: innerChildren[i] = new SizedBox(
                        width: 50.0,
                        height: 50.0,
                        child: new ColoredBox(Colors.Red))))
                    .ToArray()))));
        harness.Pump(Screen);
        RenderListWheelViewport viewport = Viewport(harness);

        // Direct child of the viewport (its IndexedSemantics wrapper's child).
        RenderObject target = OuterChild(harness, 5);
        RevealedOffset revealed = viewport.GetOffsetToReveal(target, 0.0);
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(0.0, 200.0, 300.0, 100.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 1.0);
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(0.0, 200.0, 300.0, 100.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 0.0, rect: new Rect(40.0, 40.0, 10.0, 10.0));
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(40.0, 240.0, 10.0, 10.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 1.0, rect: new Rect(40.0, 40.0, 10.0, 10.0));
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(40.0, 240.0, 10.0, 10.0), revealed.Rect);

        // Descendant of the viewport, not a direct child.
        target = InnerChild(harness, 5);
        revealed = viewport.GetOffsetToReveal(target, 0.0);
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(125.0, 225.0, 50.0, 50.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 1.0);
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(125.0, 225.0, 50.0, 50.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 0.0, rect: new Rect(40.0, 40.0, 10.0, 10.0));
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(165.0, 265.0, 10.0, 10.0), revealed.Rect);

        revealed = viewport.GetOffsetToReveal(target, 1.0, rect: new Rect(40.0, 40.0, 10.0, 10.0));
        Assert.Equal(500.0, revealed.Offset);
        Assert.Equal(new Rect(165.0, 265.0, 10.0, 10.0), revealed.Rect);

        // A horizontal axis request is not an error for a vertical wheel.
        revealed = viewport.GetOffsetToReveal(target, 0.0, axis: Axis.Horizontal);
        Assert.Equal(500.0, revealed.Offset);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_ShowOnScreen()
    {
        var controller = new ScrollController(initialScrollOffset: 300.0);
        using var harness = Harness(new Center(child: new SizedBox(
            height: 500.0,
            width: 300.0,
            child: new ListWheelScrollView(
                controller: controller,
                itemExtent: 100.0,
                children: Enumerable.Range(0, 10).Select(_ => (Widget)new Center(
                    child: new SizedBox(width: 50.0, height: 50.0, child: new ColoredBox(Colors.Red))))
                    .ToArray()))));
        harness.Pump(Screen);
        Assert.Equal(300.0, controller.Offset);

        OuterChild(harness, 5).ShowOnScreen();
        Settle(harness);
        Assert.Equal(500.0, controller.Offset);

        OuterChild(harness, 7).ShowOnScreen();
        Settle(harness);
        Assert.Equal(700.0, controller.Offset);

        InnerChild(harness, 9).ShowOnScreen();
        Settle(harness);
        Assert.Equal(900.0, controller.Offset);

        double clock = Scheduler.CurrentSeconds;
        OuterChild(harness, 7).ShowOnScreen(duration: TimeSpan.FromSeconds(2));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        harness.Pump(Screen);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 1.0));
        harness.Pump(Screen);
        Assert.InRange(controller.Offset, 700.0 + 1e-6, 900.0 - 1e-6);
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 2.1));
        harness.Pump(Screen);
        Settle(harness);
        Assert.Equal(700.0, controller.Offset, precision: 6);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_AllowsTapsOnItsChildren()
    {
        var tappedChildren = new List<int>();
        var controller = new FixedExtentScrollController(initialItem: 10);
        using var harness = Harness(new ListWheelScrollView(
            controller: controller,
            itemExtent: 100.0,
            children: Enumerable.Range(0, 100).Select(index => (Widget)new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: () => tappedChildren.Add(index),
                child: new SizedBox(width: 100.0, height: 100.0, child: new ColoredBox(Colors.Red))))
                .ToArray()));
        harness.Pump(Screen);
        List<int> painted = PaintedIndices(harness);
        Assert.Equal([7, 8, 9, 10, 11, 12, 13], painted);

        foreach (int index in painted)
        {
            Tap(harness, Center(ChildAt(harness, index)));
        }

        Assert.Equal(painted, tappedChildren);
        controller.Dispose();
    }

    [Fact]
    public void ListWheelScrollView_DoesNotAllowTapsOnChildrenLaidOutButNotPainted()
    {
        var tappedChildren = new List<int>();
        using var harness = Harness(new Center(child: new SizedBox(
            height: 120.0,
            child: new ListWheelScrollView(
                childDelegate: new ListWheelChildListDelegate(
                    Enumerable.Range(0, 100).Select(index => (Widget)new GestureDetector(
                        behavior: HitTestBehavior.Opaque,
                        onTap: () => tappedChildren.Add(index),
                        child: new SizedBox(width: 55.0, height: 55.0, child: new ColoredBox(Colors.Red))))
                        .ToArray()),
                physics: new FixedExtentScrollPhysics(),
                diameterRatio: 0.9,
                itemExtent: 55.0,
                squeeze: 1.45))));
        harness.Pump(Screen);
        Assert.Equal([0, 1], PaintedIndices(harness));
        Assert.Equal([0, 1, 2], LaidOutIndices(harness));

        Tap(harness, Center(ChildAt(harness, 0)));
        Assert.Equal([0], tappedChildren);
        Tap(harness, Center(ChildAt(harness, 1)));
        Assert.Equal([0, 1], tappedChildren);
        // The third child was laid out but sits on the back of the cylinder: never painted, so its
        // paint transform is empty and its "center" resolves next to the viewport origin.
        Tap(harness, Center(ChildAt(harness, 2)));
        Assert.Equal([0, 1], tappedChildren);
    }

    // ------------------------------------------------------------------ sample

    [Fact]
    public void ListWheelScrollViewDemoPage_RendersBothWheelsAtDesktopSize()
    {
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new Material.Theme(Material.ThemeData.Light, new ListWheelScrollViewDemoPage())));
        harness.Pump(new Size(1000, 700));

        var viewports = new List<RenderListWheelViewport>();
        void Visit(RenderObject node)
        {
            if (node is RenderListWheelViewport viewport)
            {
                viewports.Add(viewport);
            }

            node.VisitChildren(Visit);
        }

        Visit(harness.RenderView);
        Assert.Equal(2, viewports.Count);
        Assert.All(viewports, viewport => Assert.True(viewport.ChildCount > 0));
        Assert.True(viewports[0].UseMagnifier);
        Assert.Null(viewports[1].ChildManager.ChildCount);
    }

    // ------------------------------------------------------------------ helpers

    private static Widget Wheel(
        ScrollController? controller,
        int count,
        double itemExtent,
        double squeeze = 1.0,
        double diameterRatio = RenderListWheelViewport.DefaultDiameterRatio,
        double perspective = RenderListWheelViewport.DefaultPerspective,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0)
    {
        return new ListWheelScrollView(
            controller: controller,
            itemExtent: itemExtent,
            squeeze: squeeze,
            diameterRatio: diameterRatio,
            perspective: perspective,
            offAxisFraction: offAxisFraction,
            useMagnifier: useMagnifier,
            magnification: magnification,
            children: Enumerable.Range(0, count)
                .Select(_ => (Widget)new SizedBox(width: 200.0, child: new ColoredBox(Colors.Red)))
                .ToArray());
    }

    private static RenderListWheelViewport Viewport(WidgetRenderHarness harness) =>
        Assert.IsType<RenderListWheelViewport>(FindDescendant<RenderListWheelViewport>(harness.RenderView));

    private static List<RenderBox> Children(RenderListWheelViewport viewport)
    {
        var children = new List<RenderBox>();
        for (RenderBox? child = viewport.FirstChild; child != null; child = viewport.ChildAfter(child))
        {
            children.Add(child);
        }

        return children;
    }

    private static List<int> LaidOutIndices(WidgetRenderHarness harness) =>
        Children(Viewport(harness)).Select(child => ((ListWheelParentData)child.parentData!).Index!.Value).ToList();

    private static List<int> PaintedIndices(WidgetRenderHarness harness) =>
        Children(Viewport(harness))
            .Where(child => ((ListWheelParentData)child.parentData!).Transform != null)
            .Select(child => ((ListWheelParentData)child.parentData!).Index!.Value)
            .ToList();

    private static RenderBox ChildAt(WidgetRenderHarness harness, int index) =>
        Children(Viewport(harness)).Single(child => ((ListWheelParentData)child.parentData!).Index == index);

    /// <summary>The render object of the widget the test handed to the wheel (below the
    /// <see cref="IndexedSemantics"/> wrapper the delegate adds).</summary>
    private static RenderObject OuterChild(WidgetRenderHarness harness, int index)
    {
        RenderObject? result = null;
        ChildAt(harness, index).VisitChildren(child => result ??= child);
        return result!;
    }

    private static RenderObject InnerChild(WidgetRenderHarness harness, int index)
    {
        RenderObject? result = null;
        OuterChild(harness, index).VisitChildren(child => result ??= child);
        return result!;
    }

    /// <summary>Flutter's <c>tester.getCenter</c>: the box's center mapped through its full paint
    /// transform, perspective included.</summary>
    private static Point Center(RenderBox box) =>
        MatrixUtils.TransformPoint(box.GetTransformTo(null), new Point(box.Size.Width / 2, box.Size.Height / 2));

    private static List<T> FindLayers<T>(WidgetRenderHarness harness) where T : Layer
    {
        var result = new List<T>();
        void Visit(Layer layer)
        {
            if (layer is T match)
            {
                result.Add(match);
            }

            if (layer is ContainerLayer container)
            {
                foreach (Layer child in container.Children)
                {
                    Visit(child);
                }
            }
        }

        Visit(harness.RootLayer);
        return result;
    }

    private static void AssertOffset(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 1e-6);
        Assert.Equal(expected.Y, actual.Y, 1e-6);
    }

    private static void AssertMatrix(double[] expected, Matrix4 actual)
    {
        double[] storage = actual.Storage;
        for (int index = 0; index < 16; index++)
        {
            Assert.Equal(expected[index], storage[index], 1e-9);
        }
    }

    private static int _pointer = 200;

    private static void Tap(WidgetRenderHarness harness, Point position)
    {
        int pointer = ++_pointer;
        DateTime now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer, PointerDeviceKind.Touch, position, PointerButtons.None, now.AddMilliseconds(50)));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.5));
        harness.Pump(Screen);
    }

    /// <summary>Flutter's <c>tester.fling</c>: fifty moves spread over <c>|offset| / speed</c>
    /// seconds, then a pointer up.</summary>
    private static void Fling(WidgetRenderHarness harness, Point start, Vector offset, double speed)
    {
        const int moveCount = 50;
        int pointer = ++_pointer;
        DateTime now = DateTime.UtcNow;
        // Same slop compensation as Gesture: Plumix's recognizer swallows the first 18 px.
        offset = offset + new Vector(0.0, Math.Sign(offset.Y) * 18.0);
        double stepMilliseconds = 1000.0 * offset.Length / (moveCount * speed);
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Touch, start, PointerButtons.Primary, now));
        double lastPumpMilliseconds = 0.0;
        for (int index = 1; index <= moveCount; index++)
        {
            Point position = start + (offset * index / moveCount);
            GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
                pointer,
                PointerDeviceKind.Touch,
                position,
                PointerButtons.Primary,
                true,
                now.AddMilliseconds(stepMilliseconds * index)));
            // Flutter's flingFrom pumps a frame whenever a frame interval has elapsed, which is what
            // lets the wheel learn its extents while the finger is still down.
            if ((stepMilliseconds * index) - lastPumpMilliseconds > 16.0)
            {
                lastPumpMilliseconds = stepMilliseconds * index;
                harness.Pump(Screen);
            }
        }

        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer,
            PointerDeviceKind.Touch,
            start + offset,
            PointerButtons.None,
            now.AddMilliseconds(stepMilliseconds * moveCount)));
    }

    private static void Settle(WidgetRenderHarness harness)
    {
        double clock = Scheduler.CurrentSeconds;
        // Flutter's pumpAndSettle pumps every 100 ms; the wheel corrects its extents on every layout,
        // so the ballistic must not skip frames.
        for (double step = 0.016; step < 6.0; step += 0.05)
        {
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + step));
            harness.Pump(Screen);
        }
    }

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
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static WidgetRenderHarness Harness(Widget child) =>
        new(new Directionality(TextDirection.Ltr, child));

    private sealed class NoChildManager : IListWheelChildManager
    {
        public int? ChildCount => 0;

        public bool ChildExistsAt(int index) => false;

        public void CreateChild(int index, RenderBox? after)
        {
        }

        public void RemoveChild(RenderBox child)
        {
        }
    }

    /// <summary>A pointer held down on the wheel, moved in steps like Flutter's
    /// <c>TestGesture</c>.</summary>
    private sealed class Gesture
    {
        /// <summary>
        /// Plumix's drag recognizer consumes the touch slop before it starts a drag even when the arena
        /// resolved in its favour at pointer down (Flutter's sole-member arena starts the drag at the
        /// down event, so a Flutter test gesture loses nothing). The first move therefore carries the
        /// extra slop, so the numbers Flutter's tests assert stay literal.
        /// </summary>
        private const double TouchSlop = 18.0;

        private readonly WidgetRenderHarness _harness;
        private readonly int _pointerId = ++_pointer;
        private Point _logicalPosition;
        private Vector _slopShift;
        private bool _slopConsumed;
        private DateTime _time = DateTime.UtcNow;

        /// <summary>
        /// Like Flutter's <c>TestGesture</c>, every event carries the same timestamp, so a step-wise
        /// drag never turns into a fling.
        /// </summary>
        public Gesture(WidgetRenderHarness harness, Point start)
        {
            _harness = harness;
            _logicalPosition = start;
            GestureBinding.Instance.HandlePointerEvent(_harness.RenderView, new PointerDownEvent(
                _pointerId, PointerDeviceKind.Touch, start, PointerButtons.Primary, _time));
        }

        public void MoveBy(Vector delta) => MoveTo(_logicalPosition + delta);

        public void MoveTo(Point position)
        {
            if (!_slopConsumed)
            {
                _slopConsumed = true;
                Vector delta = position - _logicalPosition;
                _slopShift = new Vector(0.0, Math.Sign(delta.Y) * TouchSlop);
            }

            _logicalPosition = position;
            GestureBinding.Instance.HandlePointerEvent(_harness.RenderView, new PointerMoveEvent(
                _pointerId,
                PointerDeviceKind.Touch,
                _logicalPosition + _slopShift,
                PointerButtons.Primary,
                true,
                _time));
        }

        public void Up()
        {
            GestureBinding.Instance.HandlePointerEvent(_harness.RenderView, new PointerUpEvent(
                _pointerId, PointerDeviceKind.Touch, _logicalPosition + _slopShift, PointerButtons.None, _time));
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public Layer RootLayer => _pipeline.RootLayer;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Replace(Widget child)
        {
            _rootElement.Update(new Directionality(TextDirection.Ltr, child));
            _owner.FlushBuild();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
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
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child != null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
