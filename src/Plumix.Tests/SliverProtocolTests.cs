using Avalonia;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Flutter parity coverage for the sliver layout and hit-test protocol in `rendering/sliver.dart`:
/// `RenderSliver` as a `RenderObject`, `SliverConstraints`, `SliverHitTestResult`/`SliverHitTestEntry`,
/// and the viewport's entry into it. Ports `test/rendering/slivers_test.dart`,
/// `slivers_helpers_test.dart`, `sliver_max_paint_rect_test.dart` and the hit-test order case of
/// `viewport_test.dart`.
/// </summary>
public sealed class SliverProtocolTests
{
    [Fact]
    public void RenderSliver_IsARenderObjectAndNotARenderBox()
    {
        Assert.False(typeof(RenderBox).IsAssignableFrom(typeof(RenderSliver)));
        Assert.True(typeof(RenderObject).IsAssignableFrom(typeof(RenderSliver)));
    }

    [Theory]
    [InlineData(AxisDirection.Down, 0.0, 400.0, 800.0, 1200.0, 1600.0)]
    [InlineData(AxisDirection.Up, 200.0, -200.0, -600.0, -1000.0, -1400.0)]
    [InlineData(AxisDirection.Right, 0.0, 400.0, 800.0, 1200.0, 1600.0)]
    [InlineData(AxisDirection.Left, 400.0, 0.0, -400.0, -800.0, -1200.0)]
    public void RenderViewport_BasicTest_PositionsBoxesAlongTheAxis(
        AxisDirection axisDirection,
        double a,
        double b,
        double c,
        double d,
        double e)
    {
        (RenderViewport viewport, RenderBox[] boxes, _) = BasicViewport(axisDirection);
        double[] expected = [a, b, c, d, e];
        for (int i = 0; i < boxes.Length; i++)
        {
            Point origin = boxes[i].LocalToGlobal(default);
            Assert.Equal(expected[i], IsVertical(axisDirection) ? origin.Y : origin.X, 6);
            Assert.Equal(0.0, IsVertical(axisDirection) ? origin.X : origin.Y, 6);
        }

        Assert.Same(viewport, boxes[0].Parent!.Parent);
    }

    [Theory]
    [InlineData(AxisDirection.Down, 130.0, 150.0)]
    [InlineData(AxisDirection.Up, 150.0, 350.0)]
    [InlineData(AxisDirection.Right, 150.0, 450.0)]
    [InlineData(AxisDirection.Left, 550.0, 150.0)]
    public void RenderViewport_BasicTest_HitTestLandsOnTheThirdBoxAtOffset900(
        AxisDirection axisDirection,
        double x,
        double y)
    {
        (RenderViewport viewport, RenderBox[] boxes, PipelineOwner pipeline) = BasicViewport(axisDirection);
        viewport.Offset = ViewportOffset.Fixed(900.0);
        pipeline.FlushLayout(new Size(800, 600));

        var result = new BoxHitTestResult();
        Assert.True(viewport.HitTest(result, new Point(x, y)));

        Assert.Same(boxes[2], result.Path[0].Target);
        SliverHitTestEntry sliverEntry = Assert.Single(result.Path.OfType<SliverHitTestEntry>());
        Assert.Same(boxes[2].Parent, sliverEntry.Target);
        Assert.Same(viewport, result.Path[^1].Target);
    }

    [Theory]
    [InlineData(AxisDirection.Down, 130.0, 150.0)]
    [InlineData(AxisDirection.Up, 150.0, 350.0)]
    [InlineData(AxisDirection.Right, 150.0, 450.0)]
    [InlineData(AxisDirection.Left, 550.0, 150.0)]
    public void RenderShrinkWrappingViewport_BasicTest_HitTestLandsOnTheThirdBoxAtOffset900(
        AxisDirection axisDirection,
        double x,
        double y)
    {
        RenderBox[] boxes = Boxes(axisDirection);
        var viewport = new RenderShrinkWrappingViewport(
            offset: ViewportOffset.Fixed(900.0),
            axisDirection: axisDirection,
            children: [.. boxes.Select(box => (RenderSliver)new RenderSliverToBoxAdapter(box))]);
        PipelineOwner pipeline = Attach(viewport);
        pipeline.FlushLayout(new Size(800, 600));

        var result = new BoxHitTestResult();
        Assert.True(viewport.HitTest(result, new Point(x, y)));
        Assert.Same(boxes[2], result.Path[0].Target);
    }

    [Fact]
    public void RenderViewport_HitTest_ReportsTheSliverMainAndCrossAxisPositions()
    {
        (RenderViewport viewport, RenderBox[] boxes, PipelineOwner pipeline) = BasicViewport(AxisDirection.Down);
        viewport.Offset = ViewportOffset.Fixed(900.0);
        pipeline.FlushLayout(new Size(800, 600));

        var result = new BoxHitTestResult();
        viewport.HitTest(result, new Point(130, 150));

        // The third sliver starts at scroll offset 800, so at offset 900 it is painted from the top
        // of the viewport and the main-axis position is the viewport-local y coordinate.
        SliverHitTestEntry entry = Assert.Single(result.Path.OfType<SliverHitTestEntry>());
        Assert.Equal(150.0, entry.MainAxisPosition);
        Assert.Equal(130.0, entry.CrossAxisPosition);
        Assert.Equal("RenderSliverToBoxAdapter@(mainAxis: 150.0, crossAxis: 130.0)", entry.ToString());
    }

    [Theory]
    [InlineData(SliverPaintOrder.FirstIsTop, new[] { 0, 1, 2, 3, 4 })]
    [InlineData(SliverPaintOrder.LastIsTop, new[] { 4, 3, 2, 1, 0 })]
    public void Viewport_HitTestOrder_FollowsThePaintOrder(SliverPaintOrder paintOrder, int[] expected)
    {
        RenderSliver[] slivers = [.. Enumerable.Range(0, 5).Select(id => (RenderSliver)new AllOverlapSliver(id))];
        var viewport = new RenderViewport(
            offset: ViewportOffset.Zero(),
            crossAxisDirection: AxisDirection.Right,
            paintOrder: paintOrder,
            children: slivers);
        PipelineOwner pipeline = Attach(viewport);
        pipeline.FlushLayout(new Size(800, 600));

        var result = new BoxHitTestResult();
        viewport.HitTest(result, new Point(400, 300));

        Assert.Equal(
            expected,
            result.Path.OfType<SliverHitTestEntry>().Select(entry => ((AllOverlapSliver)entry.Target).Id));
    }

    [Fact]
    public void SliverHitTestResult_WrappingSharesThePathAndTheTransform()
    {
        var wrapped = new PublicHitTestResult();
        Matrix4 transform = Matrix4.TranslationValues(40, 150, 0);
        wrapped.PublicPushTransform(transform);
        var wrapping = SliverHitTestResult.Wrap(wrapped);
        Assert.Same(wrapped.Path, wrapping.Path);

        var target1 = new ProbeSliver();
        var entry1 = new SliverHitTestEntry(target1, mainAxisPosition: 1, crossAxisPosition: 2);
        wrapped.Add(entry1);
        Assert.Equal([entry1], wrapping.Path);
        Assert.Equal(transform.Storage, entry1.Transform!.Storage);

        var target2 = new ProbeSliver();
        var entry2 = new SliverHitTestEntry(target2, mainAxisPosition: 3, crossAxisPosition: 4);
        wrapping.Add(entry2);
        Assert.Equal([entry1, entry2], wrapped.Path);
        Assert.Equal(transform.Storage, entry2.Transform!.Storage);
    }

    [Fact]
    public void SliverHitTestResult_AddWithAxisOffset_SubtractsTheOffsetsFromThePositions()
    {
        var result = new SliverHitTestResult();
        double recordedMain = 0;
        double recordedCross = 0;
        bool Record(SliverHitTestResult hit, double mainAxisPosition, double crossAxisPosition)
        {
            Assert.Same(result, hit);
            recordedMain = mainAxisPosition;
            recordedCross = crossAxisPosition;
            return true;
        }

        Assert.True(result.AddWithAxisOffset(null, 5, 6, 10, 20, Record));
        Assert.Equal((5.0, 14.0), (recordedMain, recordedCross));

        Assert.False(result.AddWithAxisOffset(null, -5, -6, 10, 20, (hit, main, cross) =>
        {
            Record(hit, main, cross);
            return false;
        }));
        Assert.Equal((15.0, 26.0), (recordedMain, recordedCross));

        Assert.True(result.AddWithAxisOffset(null, 0, 0, 0, 0, Record));
        Assert.Equal((0.0, 0.0), (recordedMain, recordedCross));
    }

    [Fact]
    public void SliverHitTestResult_AddWithAxisOffset_PushesTheNegatedPaintOffset()
    {
        var result = new SliverHitTestResult();
        var entry = new SliverHitTestEntry(new ProbeSliver(), mainAxisPosition: 0, crossAxisPosition: 0);
        result.AddWithAxisOffset(new Point(7, 11), 0, 0, 0, 0, (hit, _, _) =>
        {
            hit.Add(entry);
            return true;
        });

        Matrix4 transform = entry.Transform!.Clone();
        transform.TranslateByDouble(7, 11, 0, 1);
        Assert.True(transform.IsIdentity());

        // The pushed offset is popped again: a later entry sees the identity.
        var later = new SliverHitTestEntry(new ProbeSliver(), mainAxisPosition: 0, crossAxisPosition: 0);
        result.Add(later);
        Assert.True(later.Transform!.IsIdentity());
    }

    [DebugOnlyFact]
    public void SliverConstraints_ReportsNaNOnEveryDoubleProperty()
    {
        SliverConstraints constraints = new(
            AxisDirection: AxisDirection.Down,
            GrowthDirection: GrowthDirection.Forward,
            UserScrollDirection: ScrollDirection.Idle,
            ScrollOffset: double.NaN,
            PrecedingScrollExtent: double.NaN,
            Overlap: double.NaN,
            RemainingPaintExtent: double.NaN,
            CrossAxisExtent: double.NaN,
            CrossAxisDirection: AxisDirection.Left,
            ViewportMainAxisExtent: double.NaN,
            RemainingCacheExtent: double.NaN,
            CacheOrigin: double.NaN);

        FlutterError error = Assert.Throws<FlutterError>(() => constraints.DebugAssertIsValid());

        Assert.Contains(
            "SliverConstraints is not valid:",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            "  The \"scrollOffset\" is NaN.\n"
            + "  The \"overlap\" is NaN.\n"
            + "  The \"crossAxisExtent\" is NaN.\n"
            + "  The \"scrollOffset\" is NaN, expected greater than or equal to zero.\n"
            + "  The \"viewportMainAxisExtent\" is NaN, expected greater than or equal to zero.\n"
            + "  The \"remainingPaintExtent\" is NaN, expected greater than or equal to zero.\n"
            + "  The \"remainingCacheExtent\" is NaN, expected greater than or equal to zero.\n"
            + "  The \"cacheOrigin\" is NaN, expected less than or equal to zero.\n"
            + "  The \"precedingScrollExtent\" is NaN, expected greater than or equal to zero.\n"
            + "  The constraints are not normalized.",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains("The offending constraints were:", error.Message, StringComparison.Ordinal);
        Assert.Equal(
            "SliverConstraints(AxisDirection.down, GrowthDirection.forward, ScrollDirection.idle, "
            + "scrollOffset: NaN, precedingScrollExtent: NaN, remainingPaintExtent: NaN, overlap: NaN, "
            + "crossAxisExtent: NaN, crossAxisDirection: AxisDirection.left, viewportMainAxisExtent: NaN, "
            + "remainingCacheExtent: NaN, cacheOrigin: NaN)",
            constraints.ToString());
    }

    [DebugOnlyFact]
    public void SliverConstraints_ReportsTheSignOfRelevantDoubleProperties()
    {
        SliverConstraints constraints = new(
            AxisDirection: AxisDirection.Down,
            GrowthDirection: GrowthDirection.Forward,
            UserScrollDirection: ScrollDirection.Idle,
            ScrollOffset: -1.0,
            PrecedingScrollExtent: -1.0,
            Overlap: 0.0,
            RemainingPaintExtent: -1.0,
            CrossAxisExtent: 0.0,
            CrossAxisDirection: AxisDirection.Left,
            ViewportMainAxisExtent: 0.0,
            RemainingCacheExtent: -1.0,
            CacheOrigin: 1.0);

        FlutterError error = Assert.Throws<FlutterError>(() => constraints.DebugAssertIsValid());

        Assert.Contains(
            "  The \"scrollOffset\" is negative.\n"
            + "  The \"remainingPaintExtent\" is negative.\n"
            + "  The \"remainingCacheExtent\" is negative.\n"
            + "  The \"cacheOrigin\" is positive.\n"
            + "  The \"precedingScrollExtent\" is negative.\n"
            + "  The constraints are not normalized.",
            error.Message,
            StringComparison.Ordinal);
        Assert.Equal(
            "SliverConstraints(AxisDirection.down, GrowthDirection.forward, ScrollDirection.idle, "
            + "scrollOffset: -1.0, precedingScrollExtent: -1.0, remainingPaintExtent: -1.0, "
            + "crossAxisExtent: 0.0, crossAxisDirection: AxisDirection.left, viewportMainAxisExtent: 0.0, "
            + "remainingCacheExtent: -1.0, cacheOrigin: 1.0)",
            constraints.ToString());
    }

    [DebugOnlyFact]
    public void SliverConstraints_RejectsAxisAndCrossAxisDirectionsAlongTheSameAxis()
    {
        SliverConstraints constraints = TestSliverConstraints.Create(
            AxisDirection: AxisDirection.Down,
            CrossAxisDirection: AxisDirection.Up);

        Assert.False(constraints.IsNormalized);
        FlutterError error = Assert.Throws<FlutterError>(() => constraints.DebugAssertIsValid());
        Assert.Contains(
            "The \"axisDirection\" and the \"crossAxisDirection\" are along the same axis.",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SliverConstraints_AreTheSameWhenCopied()
    {
        SliverConstraints a = TestSliverConstraints.Create(
            ScrollOffset: 0.0,
            RemainingPaintExtent: 0.0,
            CrossAxisExtent: 0.0,
            ViewportMainAxisExtent: 0.0);
        SliverConstraints copy = a.CopyWith();

        Assert.Equal(a, copy);
        Assert.Equal(a.GetHashCode(), copy.GetHashCode());
        Assert.Equal(a.ToString(), copy.ToString());
        Assert.DoesNotContain('\n', a.ToString());
        Assert.Equal(GrowthDirection.Forward, a.NormalizedGrowthDirection);
        Assert.Equal(Axis.Vertical, a.Axis);
        Assert.False(a.IsTight);

        SliverConstraints changed = a.CopyWith(scrollOffset: 10.0, axisDirection: AxisDirection.Up);
        Assert.NotEqual(a, changed);
        Assert.Equal(10.0, changed.ScrollOffset);
        Assert.Equal(Axis.Vertical, changed.Axis);
    }

    [Theory]
    [InlineData(AxisDirection.Up, GrowthDirection.Reverse, GrowthDirection.Forward)]
    [InlineData(AxisDirection.Right, GrowthDirection.Reverse, GrowthDirection.Reverse)]
    [InlineData(AxisDirection.Left, GrowthDirection.Reverse, GrowthDirection.Forward)]
    [InlineData(AxisDirection.Up, GrowthDirection.Forward, GrowthDirection.Reverse)]
    [InlineData(AxisDirection.Down, GrowthDirection.Forward, GrowthDirection.Forward)]
    public void SliverConstraints_NormalizedGrowthDirectionIsInferredFromTheAxisDirection(
        AxisDirection axisDirection,
        GrowthDirection growthDirection,
        GrowthDirection expected)
    {
        SliverConstraints constraints = TestSliverConstraints.Create(
            AxisDirection: axisDirection,
            GrowthDirection: growthDirection);

        Assert.Equal(expected, constraints.NormalizedGrowthDirection);
        Assert.Equal(ScrollDirectionUtils.AxisDirectionToAxis(axisDirection), constraints.Axis);
    }

    [Fact]
    public void SliverConstraints_AsBoxConstraintsIsTightInTheCrossAxis()
    {
        SliverConstraints vertical = TestSliverConstraints.Create(CrossAxisExtent: 80.0);
        SliverConstraints horizontal = TestSliverConstraints.Create(Axis: Axis.Horizontal, CrossAxisExtent: 80.0);

        Assert.Equal(
            new BoxConstraints(MinWidth: 80, MaxWidth: 80, MinHeight: 10, MaxHeight: 20),
            vertical.AsBoxConstraints(minExtent: 10, maxExtent: 20));
        Assert.Equal(
            new BoxConstraints(MinWidth: 0, MaxWidth: double.PositiveInfinity, MinHeight: 30, MaxHeight: 30),
            horizontal.AsBoxConstraints(crossAxisExtent: 30));
    }

    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void Sliver_PaintBoundsAndSemanticBoundsSpanThePaintExtentAndTheCrossAxis(Axis axis)
    {
        var box = new SizedRenderBox(new Size(150, 150));
        var sliver = new RenderSliverToBoxAdapter(box);
        var viewport = new RenderViewport(
            offset: ViewportOffset.Zero(),
            axisDirection: axis == Axis.Vertical ? AxisDirection.Down : AxisDirection.Right,
            children: [sliver]);
        PipelineOwner pipeline = Attach(viewport);
        pipeline.FlushLayout(new Size(800, 600));

        Rect expected = axis == Axis.Vertical ? new Rect(0, 0, 800, 150) : new Rect(0, 0, 150, 600);
        Assert.Equal(expected, sliver.PaintBounds);
        Assert.Equal(expected, sliver.SemanticBoundsForSemantics);
    }

    [Fact]
    public void PrecedingScrollExtent_AccumulatesOverSliversAndIgnoresTheScrollOffset()
    {
        RenderSliverToBoxAdapter[] slivers =
        [
            new(new SizedRenderBox(new Size(100, 150))),
            new(new SizedRenderBox(new Size(100, 150))),
            new(new SizedRenderBox(new Size(100, 150))),
        ];
        var viewport = new RenderViewport(offset: ViewportOffset.Zero(), children: slivers);
        PipelineOwner pipeline = Attach(viewport);
        pipeline.FlushLayout(new Size(800, 600));

        Assert.Equal(0.0, slivers[0].Constraints.PrecedingScrollExtent);
        Assert.Equal(300.0, slivers[2].Constraints.PrecedingScrollExtent);

        viewport.Offset = ViewportOffset.Fixed(100.0);
        pipeline.FlushLayout(new Size(800, 600));
        Assert.Equal(300.0, slivers[2].Constraints.PrecedingScrollExtent);
    }

    [Theory]
    [MemberData(nameof(MaxPaintRectCases))]
    public void GetMaxPaintRect_MatchesFlutter(
        SliverGeometry geometry,
        AxisDirection axisDirection,
        GrowthDirection growthDirection,
        double scrollOffset,
        double remainingPaintExtent,
        double remainingCacheExtent,
        double cacheOrigin,
        Rect expected)
    {
        var sliver = new ProbeSliver(geometry);
        sliver.Layout(
            TestSliverConstraints.Create(
                AxisDirection: axisDirection,
                GrowthDirection: growthDirection,
                ScrollOffset: scrollOffset,
                RemainingPaintExtent: remainingPaintExtent,
                CrossAxisExtent: 100.0,
                ViewportMainAxisExtent: 100.0,
                RemainingCacheExtent: remainingCacheExtent,
                CacheOrigin: cacheOrigin),
            parentUsesSize: false);

        Assert.Equal(expected, sliver.MaxPaintRect);
    }

    [Fact]
    public void GetMaxPaintRect_BeforeLayoutIsEmpty()
    {
        var sliver = new ProbeSliver();

        Assert.Null(sliver.Geometry);
        Assert.Equal(new Rect(0, 0, 0, 0), sliver.MaxPaintRect);
    }

    public static TheoryData<SliverGeometry, AxisDirection, GrowthDirection, double, double, double, double, Rect>
        MaxPaintRectCases()
    {
        var full = new SliverGeometry(ScrollExtent: 100, PaintExtent: 100, MaxPaintExtent: 100);
        var half = new SliverGeometry(ScrollExtent: 100, PaintExtent: 50, MaxPaintExtent: 100);
        var forward = GrowthDirection.Forward;
        return new()
        {
            { SliverGeometry.Zero, AxisDirection.Down, forward, 0, 300, 100, 0, new Rect(0, 0, 0, 0) },
            { new SliverGeometry(), AxisDirection.Down, forward, 0, 300, 100, 0, new Rect(0, 0, 100, 0) },
            { full, AxisDirection.Down, forward, 0, 300, 100, 0, new Rect(0, 0, 100, 100) },
            { full, AxisDirection.Down, GrowthDirection.Reverse, 0, 300, 100, 0, new Rect(0, 0, 100, 100) },
            { half, AxisDirection.Down, forward, 50, 50, 100, 0, new Rect(0, -50, 100, 100) },
            { full, AxisDirection.Up, forward, 0, 300, 100, 0, new Rect(0, 0, 100, 100) },
            { half, AxisDirection.Up, forward, 50, 50, 100, 0, new Rect(0, 0, 100, 100) },
            { full, AxisDirection.Right, forward, 0, 300, 100, 0, new Rect(0, 0, 100, 100) },
            { half, AxisDirection.Right, forward, 50, 50, 100, 0, new Rect(-50, 0, 100, 100) },
            { full, AxisDirection.Left, forward, 0, 300, 100, 0, new Rect(0, 0, 100, 100) },
            { half, AxisDirection.Left, forward, 50, 50, 100, 0, new Rect(0, 0, 100, 100) },
            {
                new SliverGeometry(
                    ScrollExtent: 100,
                    PaintExtent: 10,
                    MaxPaintExtent: 100,
                    MaxScrollObstructionExtent: 10),
                AxisDirection.Down, forward, 95, 10, 100, 0, new Rect(0, -90, 100, 100)
            },
            {
                new SliverGeometry(
                    ScrollExtent: double.PositiveInfinity,
                    PaintExtent: 100,
                    MaxPaintExtent: double.PositiveInfinity,
                    CacheExtent: 150),
                AxisDirection.Down, forward, 50, 300, 200, -50, new Rect(0, -50, 100, 150)
            },
            {
                new SliverGeometry(ScrollExtent: 100, PaintExtent: 100, MaxPaintExtent: 100, CrossAxisExtent: 50),
                AxisDirection.Down, forward, 0, 300, 100, 0, new Rect(0, 0, 50, 100)
            },
            {
                new SliverGeometry(ScrollExtent: 200, PaintExtent: 100, MaxPaintExtent: 150),
                AxisDirection.Down, forward, 0, 100, 100, 0, new Rect(0, 0, 100, 150)
            },
        };
    }

    [Theory]
    [InlineData(AxisDirection.Down, GrowthDirection.Forward, 100.0, 40.0)]
    [InlineData(AxisDirection.Up, GrowthDirection.Forward, 100.0, -40.0)]
    [InlineData(AxisDirection.Right, GrowthDirection.Forward, 40.0, 100.0)]
    [InlineData(AxisDirection.Left, GrowthDirection.Forward, -40.0, 100.0)]
    [InlineData(AxisDirection.Down, GrowthDirection.Reverse, 100.0, -40.0)]
    public void GetAbsoluteSize_FollowsTheAxisAndGrowthDirections(
        AxisDirection axisDirection,
        GrowthDirection growthDirection,
        double relativeWidth,
        double relativeHeight)
    {
        var sliver = new ProbeSliver(new SliverGeometry(ScrollExtent: 40, PaintExtent: 40, MaxPaintExtent: 40));
        sliver.Layout(
            TestSliverConstraints.Create(
                AxisDirection: axisDirection,
                GrowthDirection: growthDirection,
                RemainingPaintExtent: 100,
                CrossAxisExtent: 100,
                ViewportMainAxisExtent: 100),
            parentUsesSize: true);

        Assert.Equal(new Size(relativeWidth, relativeHeight), sliver.AbsoluteSizeRelativeToOrigin);
        Assert.Equal(new Size(Math.Abs(relativeWidth), Math.Abs(relativeHeight)), sliver.AbsoluteSize);
    }

    [DebugOnlyFact]
    public void DebugAssertDoesMeetConstraints_RejectsPaintingBeyondTheRemainingPaintExtent()
    {
        var sliver = new ProbeSliver(new SliverGeometry(
            ScrollExtent: 100,
            PaintExtent: 60,
            PaintOrigin: 50,
            LayoutExtent: 0,
            MaxPaintExtent: 100));
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => errors.Add(details);
        try
        {
            sliver.Layout(
                TestSliverConstraints.Create(
                    RemainingPaintExtent: 100,
                    CrossAxisExtent: 100,
                    ViewportMainAxisExtent: 100),
                parentUsesSize: true);
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(errors);
        string message = details.Exception.ToString()!;
        Assert.Contains(
            "SliverGeometry has a paintOffset that exceeds the remainingPaintExtent from the constraints.",
            message,
            StringComparison.Ordinal);
        Assert.Contains(
            "The remainingPaintExtent is 100.0, but the paintOrigin + paintExtent is 110.0.",
            message,
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void RenderSliver_BaseApplyPaintTransformReportsTheMissingOverride()
    {
        var sliver = new ProbeSliver();
        FlutterError error = Assert.Throws<FlutterError>(
            () => sliver.ApplyPaintTransform(new ProbeSliver(), Matrix4.Identity()));
        Assert.Equal("ProbeSliver does not implement applyPaintTransform.", error.Message);
    }

    [Fact]
    public void RenderSliver_RelayoutsWhenOnlyTheSliverConstraintsChange()
    {
        var sliver = new ProbeSliver(new SliverGeometry(ScrollExtent: 10, PaintExtent: 10, MaxPaintExtent: 10));
        SliverConstraints constraints = TestSliverConstraints.Create(
            RemainingPaintExtent: 100,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 100);

        sliver.Layout(constraints, parentUsesSize: true);
        sliver.Layout(constraints, parentUsesSize: true);
        Assert.Equal(1, sliver.LayoutCount);

        // Only the user scroll direction differs; no box constraints could tell the two apart.
        sliver.Layout(constraints with { UserScrollDirection = ScrollDirection.Forward }, parentUsesSize: true);
        Assert.Equal(2, sliver.LayoutCount);
    }

    [Fact]
    public void RenderSliver_IsARelayoutBoundaryOnlyWhenItsParentDoesNotUseItsGeometry()
    {
        SliverConstraints constraints = TestSliverConstraints.Create(
            RemainingPaintExtent: 100,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 100);
        var parentUsesSize = new ProbeSliver();
        var ownBoundary = new ProbeSliver();
        var viewport = new RenderViewport(offset: ViewportOffset.Zero(), children: [parentUsesSize, ownBoundary]);
        Attach(viewport).FlushLayout(new Size(800, 600));

        // A viewport always reads its slivers' geometry, and `SliverConstraints.isTight` is false.
        Assert.False(parentUsesSize.IsRelayoutBoundary);
        ownBoundary.Layout(constraints, parentUsesSize: false);
        Assert.True(ownBoundary.IsRelayoutBoundary);
    }

    [Fact]
    public void SliverMultiBoxAdaptorParentData_ToStringMatchesFlutter()
    {
        var candidate = new SliverMultiBoxAdaptorParentData();
        Assert.Equal("index=null; layoutOffset=None", candidate.ToString());
        candidate.KeepAlive = true;
        Assert.Equal("index=null; keepAlive; layoutOffset=None", candidate.ToString());
        candidate.KeepAlive = false;
        candidate.Index = 0;
        Assert.Equal("index=0; layoutOffset=None", candidate.ToString());
        candidate.Index = 1;
        Assert.Equal("index=1; layoutOffset=None", candidate.ToString());
        candidate.Index = -1;
        Assert.Equal("index=-1; layoutOffset=None", candidate.ToString());
        candidate.LayoutOffset = 100.0;
        Assert.Equal("index=-1; layoutOffset=100.0", candidate.ToString());
    }

    private static bool IsVertical(AxisDirection axisDirection) =>
        axisDirection is AxisDirection.Down or AxisDirection.Up;

    private static RenderBox[] Boxes(AxisDirection axisDirection)
    {
        Size size = IsVertical(axisDirection) ? new Size(100, 400) : new Size(400, 100);
        return [.. Enumerable.Range(0, 5).Select(_ => (RenderBox)new SizedRenderBox(size))];
    }

    private static (RenderViewport Viewport, RenderBox[] Boxes, PipelineOwner Pipeline) BasicViewport(
        AxisDirection axisDirection)
    {
        RenderBox[] boxes = Boxes(axisDirection);
        var viewport = new RenderViewport(
            offset: ViewportOffset.Zero(),
            axisDirection: axisDirection,
            children: [.. boxes.Select(box => (RenderSliver)new RenderSliverToBoxAdapter(box))]);
        PipelineOwner pipeline = Attach(viewport);
        pipeline.FlushLayout(new Size(800, 600));
        return (viewport, boxes, pipeline);
    }

    private static PipelineOwner Attach(RenderBox viewport)
    {
        var root = new RenderView(new FlutterView(new Size(800, 600))) { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        return pipeline;
    }

    private sealed class PublicHitTestResult : HitTestResult
    {
        public void PublicPushTransform(Matrix4 transform) => PushTransform(transform);
    }

    /// <summary>Flutter's `RenderSizedBox` test helper: a box that takes its size and is hittable.</summary>
    private sealed class SizedRenderBox(Size size) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// <summary>Flutter's `_TestRenderSliver`: reports a fixed geometry.</summary>
    private sealed class ProbeSliver(SliverGeometry? geometry = null) : RenderSliver
    {
        public int LayoutCount { get; private set; }

        public Rect MaxPaintRect => GetMaxPaintRect();

        public Size AbsoluteSize => GetAbsoluteSize();

        public Size AbsoluteSizeRelativeToOrigin => GetAbsoluteSizeRelativeToOrigin();

        protected override void PerformLayout()
        {
            LayoutCount++;
            Geometry = geometry;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// <summary>
    /// `viewport_test.dart`'s `_RenderAllOverlapSliver`: every sliver covers the whole viewport and
    /// records a hit without claiming it, so the viewport visits all of them.
    /// </summary>
    private sealed class AllOverlapSliver(int id) : RenderSliver
    {
        public int Id { get; } = id;

        protected override void PerformLayout()
        {
            Geometry = new SliverGeometry(
                PaintExtent: Constraints.RemainingPaintExtent,
                MaxPaintExtent: Constraints.RemainingPaintExtent,
                LayoutExtent: 0.0);
        }

        public override bool HitTest(SliverHitTestResult result, double mainAxisPosition, double crossAxisPosition)
        {
            if (mainAxisPosition >= 0.0
                && mainAxisPosition < Geometry!.HitTestExtent
                && crossAxisPosition >= 0.0
                && crossAxisPosition < Constraints.CrossAxisExtent)
            {
                result.Add(new SliverHitTestEntry(this, mainAxisPosition, crossAxisPosition));
            }

            return false;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
