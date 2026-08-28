using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// Parity coverage for `flutter/packages/flutter/lib/src/rendering/flex.dart` and the
/// `Flex`/`Row`/`Column`/`Flexible`/`Expanded`/`Spacer` widgets in `widgets/basic.dart`.
/// Mirrors `flutter/test/rendering/flex_test.dart`, `test/widgets/row_test.dart`,
/// `test/widgets/column_test.dart`, `test/widgets/flex_test.dart` and `test/widgets/spacer_test.dart`.
public sealed class RenderFlexTests
{
    // ---------- Defaults ----------

    [Fact]
    public void RenderFlex_Defaults_MatchFlutter()
    {
        var flex = new RenderFlex();

        Assert.Equal(Axis.Horizontal, flex.Direction);
        Assert.Equal(MainAxisSize.Max, flex.MainAxisSize);
        Assert.Equal(MainAxisAlignment.Start, flex.MainAxisAlignment);
        Assert.Equal(CrossAxisAlignment.Center, flex.CrossAxisAlignment);
        Assert.Null(flex.TextDirection);
        Assert.Equal(VerticalDirection.Down, flex.VerticalDirection);
        Assert.Null(flex.TextBaseline);
        Assert.Equal(Clip.None, flex.ClipBehavior);
        Assert.Equal(0.0, flex.Spacing);
        Assert.False(flex._hasOverflow);
    }

    [Fact]
    public void RenderFlex_NegativeSpacing_IsADebugOnlyAssertion()
    {
        RenderFlex? flex = null;
        Exception? error = Record.Exception(() => flex = new RenderFlex(
            children: [Box(100, 100)],
            textDirection: TextDirection.Ltr,
            spacing: -15.0));

        if (Constants.KDebugMode)
        {
            Assert.IsType<AssertionError>(error);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(-15.0, flex!.Spacing);
        }
    }

    [Fact]
    public void RenderFlex_SpacingSetter_AcceptsNegativeValues()
    {
        var flex = new RenderFlex();

        flex.Spacing = -15.0;

        Assert.Equal(-15.0, flex.Spacing);
    }

    // ---------- Constraint resolution ----------

    [Fact]
    public void RenderFlex_Overconstrained_TakesTheIncomingTightSize()
    {
        var flex = new RenderFlex(children: [Box(0, 0)], textDirection: TextDirection.Ltr);

        Layout(flex, new BoxConstraints(200, 200, 200, 200));

        Assert.Equal(new Size(200, 200), flex.Size);
    }

    [Fact]
    public void RenderFlex_HorizontalWithMultipleChildren_RequiresTextDirection()
    {
        var flex = new RenderFlex(children: [Box(10, 10), Box(10, 10)]);

        Exception? error = Record.Exception(() => Layout(flex, BoxConstraints.Loose(new Size(100, 100))));

        if (Constants.KDebugMode)
        {
            var flutterError = Assert.IsType<FlutterError>(error);
            Assert.Equal(
                "Horizontal RenderFlex with multiple children has a null textDirection, "
                + "so the layout order is undefined.",
                flutterError.Message);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Fact]
    public void RenderFlex_HorizontalStartAlignment_RequiresTextDirection()
    {
        var flex = new RenderFlex(children: [Box(10, 10)], mainAxisAlignment: MainAxisAlignment.Start);

        Exception? error = Record.Exception(() => Layout(flex, BoxConstraints.Loose(new Size(100, 100))));

        if (Constants.KDebugMode)
        {
            Assert.IsType<FlutterError>(error);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Fact]
    public void RenderFlex_VerticalCrossStartAlignment_RequiresTextDirection()
    {
        var flex = new RenderFlex(
            children: [Box(10, 10)],
            direction: Axis.Vertical,
            mainAxisAlignment: MainAxisAlignment.Center,
            crossAxisAlignment: CrossAxisAlignment.Start);

        Exception? error = Record.Exception(() => Layout(flex, BoxConstraints.Loose(new Size(100, 100))));

        if (Constants.KDebugMode)
        {
            Assert.IsType<FlutterError>(error);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Fact]
    public void RenderFlex_VerticalWithMultipleChildren_DoesNotRequireTextDirection()
    {
        var flex = new RenderFlex(
            children: [Box(10, 10), Box(10, 10)],
            direction: Axis.Vertical,
            mainAxisAlignment: MainAxisAlignment.Center);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.Equal(new Size(10, 100), flex.Size);
    }

    [Fact]
    public void RenderFlex_BaselineWithoutTextBaseline_Throws()
    {
        var flex = new RenderFlex(
            children: [Box(10, 10)],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: MainAxisAlignment.Center,
            crossAxisAlignment: CrossAxisAlignment.Baseline);

        FlutterError error = Assert.Throws<FlutterError>(
            () => Layout(flex, BoxConstraints.Loose(new Size(100, 100))));
        Assert.Contains("textBaseline", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderFlex_TextBaselineSetter_UsesADebugOnlyAssertion()
    {
        var flex = new RenderFlex(
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);

        Exception? error = Record.Exception(() => flex.TextBaseline = null);

        if (Constants.KDebugMode)
        {
            Assert.IsType<AssertionError>(error);
            Assert.Equal(TextBaseline.Alphabetic, flex.TextBaseline);
        }
        else
        {
            Assert.Null(error);
            Assert.Null(flex.TextBaseline);
        }
    }

    // ---------- Unbounded main axis ----------

    [Fact]
    public void RenderFlex_MinSizeUnbounded_LooseFlexChild_ShrinkWraps()
    {
        RenderBox first = Box(100, 100);
        RenderBox second = Box(100, 100);
        RenderBox third = Box(100, 100);
        var flex = new RenderFlex(
            children: [first, second, third],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min);
        SetFlex(second, 1, FlexFit.Loose);

        Layout(flex, new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: 400));

        Assert.Equal(new Size(300, 100), flex.Size);
    }

    [Fact]
    public void RenderFlex_MinSizeUnbounded_TightFlexChild_Throws()
    {
        RenderBox child = Box(100, 100);
        var flex = new RenderFlex(
            children: [child],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min);
        SetFlex(child, 1, FlexFit.Tight);

        Exception? error = Record.Exception(() =>
            Layout(flex, new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: 400)));

        if (Constants.KDebugMode)
        {
            var flutterError = Assert.IsType<FlutterError>(error);
            Assert.Contains(
                "non-zero flex but incoming width constraints are unbounded",
                flutterError.Message,
                StringComparison.Ordinal);
            Assert.Contains("\nSee also:", flutterError.Message, StringComparison.Ordinal);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Fact]
    public void RenderFlex_MaxSizeUnbounded_LooseFlexChild_Throws()
    {
        RenderBox child = Box(100, 100);
        var flex = new RenderFlex(
            children: [child],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Max);
        SetFlex(child, 1, FlexFit.Loose);

        Exception? error = Record.Exception(() =>
            Layout(flex, new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: 400)));

        if (Constants.KDebugMode)
        {
            Assert.IsType<FlutterError>(error);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Fact]
    public void RenderFlex_MinSizeUnbounded_TracksTheParentsBound()
    {
        RenderBox first = Box(100, 100);
        RenderBox second = Box(100, 100);
        RenderBox third = Box(100, 100);
        var flex = new RenderFlex(
            children: [first, second, third],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min);
        SetFlex(second, 1, FlexFit.Loose);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(new Size(300, 100), flex.Size);

        flex.MainAxisSize = MainAxisSize.Max;
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(new Size(500, 100), flex.Size);

        flex.MainAxisSize = MainAxisSize.Min;
        SetFlex(second, 1, FlexFit.Tight);
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(new Size(500, 100), flex.Size);

        Layout(flex, new BoxConstraints(MaxWidth: 505, MaxHeight: 400));
        Assert.Equal(new Size(505, 100), flex.Size);
    }

    // ---------- Overflow ----------

    [Fact]
    public void RenderFlex_VerticalOverflow_ZeroesTheFlexChildAndKeepsIntrinsics()
    {
        RenderBox fixedChild = Box(0, 200);
        RenderBox flexChild = Box(0, 0);
        var flex = new RenderFlex(children: [fixedChild, flexChild], direction: Axis.Vertical);
        SetFlex(flexChild, 1, FlexFit.Tight);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.Equal(0.0, flexChild.Size.Height);
        Assert.True(flex._hasOverflow);
        Assert.Equal(200.0, flex.GetMinIntrinsicHeight(100));
        Assert.Equal(200.0, flex.GetMaxIntrinsicHeight(100));
        Assert.Equal(0.0, flex.GetMinIntrinsicWidth(100));
        Assert.Equal(0.0, flex.GetMaxIntrinsicWidth(100));
    }

    [Fact]
    public void RenderFlex_Overflow_IsReportedThroughFlutterErrorOncePerRenderObject()
    {
        var flex = new RenderFlex(
            children: [Box(200, 100)],
            direction: Axis.Vertical,
            textDirection: TextDirection.Ltr);
        Layout(flex, BoxConstraints.Tight(new Size(100, 50)));
        Assert.True(flex._hasOverflow);

        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            flex.UpdateCompositingBits();
            flex.Paint(new PaintingContext(new ContainerLayer()), default);
            flex.Paint(new PaintingContext(new ContainerLayer()), default);
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        if (!Constants.KDebugMode)
        {
            Assert.Empty(reported);
            return;
        }

        // Dart reports on the first occurrence only, and never again for the same render object.
        FlutterErrorDetails details = Assert.Single(reported);
        Assert.Equal("rendering library", details.Library);
        Assert.Equal("during layout", details.Context?.ToString());
        var error = Assert.IsType<FlutterError>(details.Exception);
        Assert.Equal("A RenderFlex overflowed by 50 pixels on the bottom.", error.Diagnostics[0].ToString());
        Assert.Contains(
            "The overflowing RenderFlex has an orientation of Vertical.",
            details.InformationCollector!().Select(node => node.ToString()));
        Assert.Contains(
            "Consider applying a flex factor (e.g. using an Expanded widget) to "
            + "force the children of the RenderFlex to fit within the available "
            + "space instead of being sized to their natural size.",
            details.InformationCollector!().Select(node => node.ToString()));
    }

    [Fact]
    public void RenderFlex_VerticalOverflowWithSpacing_AddsTheGapToTheMainIntrinsic()
    {
        RenderBox fixedChild = Box(0, 200);
        RenderBox flexChild = Box(0, 0);
        var flex = new RenderFlex(
            children: [fixedChild, flexChild],
            direction: Axis.Vertical,
            spacing: 16.0);
        SetFlex(flexChild, 1, FlexFit.Tight);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.Equal(0.0, flexChild.Size.Height);
        Assert.Equal(216.0, flex.GetMinIntrinsicHeight(100));
        Assert.Equal(216.0, flex.GetMaxIntrinsicHeight(100));
        Assert.Equal(0.0, flex.GetMinIntrinsicWidth(100));
    }

    [Fact]
    public void RenderFlex_HorizontalOverflowWithSpacing_AddsTheGapToTheMainIntrinsic()
    {
        RenderBox fixedChild = Box(200, 0);
        RenderBox flexChild = Box(0, 0);
        var flex = new RenderFlex(
            children: [fixedChild, flexChild],
            textDirection: TextDirection.Ltr,
            spacing: 12.0);
        SetFlex(flexChild, 1, FlexFit.Tight);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.Equal(0.0, flexChild.Size.Width);
        Assert.Equal(212.0, flex.GetMinIntrinsicWidth(100));
        Assert.Equal(212.0, flex.GetMaxIntrinsicWidth(100));
        Assert.Equal(0.0, flex.GetMinIntrinsicHeight(100));
    }

    [Fact]
    public void RenderFlex_SubEpsilonOverflow_IsNotReportedAsOverflow()
    {
        const double width = 438.85714285714283;
        RenderBox child = Box(438.8571428571429, 100);
        var flex = new RenderFlex(
            children: [child],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min);

        Layout(flex, new BoxConstraints(MaxWidth: width, MaxHeight: 400));

        Assert.Equal(width, flex.Size.Width, 9);
        Assert.Equal(100.0, flex.Size.Height);
        Assert.False(flex._hasOverflow);
    }

    [Fact]
    public void RenderFlex_ClipBehavior_IsSettableAndOnlyRepaints()
    {
        var flex = new RenderFlex(children: [Box(10, 10)], textDirection: TextDirection.Ltr);
        Assert.Equal(Clip.None, flex.ClipBehavior);
        Assert.Null(flex.InvokeDescribeApproximatePaintClip(null));

        flex.ClipBehavior = Clip.AntiAlias;

        Assert.Equal(Clip.AntiAlias, flex.ClipBehavior);
    }

    [Fact]
    public void RenderFlex_DescribeApproximatePaintClip_FollowsOverflowAndClipBehavior()
    {
        RenderBox fixedChild = Box(0, 200);
        var flex = new RenderFlex(
            children: [fixedChild],
            direction: Axis.Vertical,
            clipBehavior: Clip.HardEdge);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.True(flex._hasOverflow);
        Assert.Equal(new Rect(new Point(), flex.Size), flex.InvokeDescribeApproximatePaintClip(fixedChild));

        flex.ClipBehavior = Clip.None;
        Assert.Null(flex.InvokeDescribeApproximatePaintClip(fixedChild));
    }

    // ---------- Cross axis ----------

    [Fact]
    public void RenderFlex_Stretch_TightensTheCrossAxis()
    {
        RenderBox first = ZeroIntrinsicBox();
        RenderBox second = ZeroIntrinsicBox();
        var flex = new RenderFlex(children: [first, second], textDirection: TextDirection.Ltr);
        SetFlex(second, 2, FlexFit.Tight);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));
        Assert.Equal(new Size(0, 0), first.Size);
        Assert.Equal(new Size(100, 0), second.Size);

        flex.CrossAxisAlignment = CrossAxisAlignment.Stretch;
        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));
        Assert.Equal(new Size(0, 100), first.Size);
        Assert.Equal(new Size(100, 100), second.Size);

        flex.Direction = Axis.Vertical;
        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));
        Assert.Equal(new Size(100, 0), first.Size);
        Assert.Equal(new Size(100, 100), second.Size);
    }

    [Fact]
    public void RenderFlex_ParentDataFlex_ExpandsTheChild()
    {
        RenderBox first = ZeroIntrinsicBox();
        RenderBox second = ZeroIntrinsicBox();
        var flex = new RenderFlex(children: [first, second], textDirection: TextDirection.Ltr);

        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));
        Assert.Equal(new Size(0, 0), first.Size);
        Assert.Equal(new Size(0, 0), second.Size);

        ((FlexParentData)second.parentData!).flex = 1;
        flex.MarkNeedsLayout();
        Layout(flex, BoxConstraints.Loose(new Size(100, 100)));

        Assert.Equal(new Size(0, 0), first.Size);
        Assert.Equal(new Size(100, 0), second.Size);
    }

    // ---------- Main axis alignment ----------

    [Fact]
    public void RenderFlex_SpaceEvenly_DistributesLeadingAndBetweenSpace()
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(
            children: [.. children],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: MainAxisAlignment.SpaceEvenly);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([50.0, 200.0, 350.0], children.Select(c => Offset(c).X));

        flex.Direction = Axis.Vertical;
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([25.0, 150.0, 275.0], children.Select(c => Offset(c).Y));
    }

    [Theory]
    [InlineData(MainAxisAlignment.Start, new[] { 0.0, 114.0, 228.0 }, new[] { 0.0, 114.0, 228.0 })]
    [InlineData(MainAxisAlignment.End, new[] { 172.0, 286.0, 400.0 }, new[] { 72.0, 186.0, 300.0 })]
    [InlineData(MainAxisAlignment.Center, new[] { 86.0, 200.0, 314.0 }, new[] { 36.0, 150.0, 264.0 })]
    [InlineData(MainAxisAlignment.SpaceBetween, new[] { 0.0, 200.0, 400.0 }, new[] { 0.0, 150.0, 300.0 })]
    public void RenderFlex_SpacingWithAlignment_MatchesFlutter(
        MainAxisAlignment alignment,
        double[] horizontal,
        double[] vertical)
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(
            children: [.. children],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: alignment,
            spacing: 14.0);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(horizontal, children.Select(c => Offset(c).X));

        flex.Direction = Axis.Vertical;
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(vertical, children.Select(c => Offset(c).Y));
    }

    [Fact]
    public void RenderFlex_SpaceEvenlyWithSpacing_MatchesFlutter()
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(
            children: [.. children],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: MainAxisAlignment.SpaceEvenly,
            spacing: 14.0);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([43.0, 200.0, 357.0], children.Select(c => Offset(c).X));

        flex.Direction = Axis.Vertical;
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([18.0, 150.0, 282.0], children.Select(c => Offset(c).Y));
    }

    [Fact]
    public void RenderFlex_SpaceAroundWithSpacing_MatchesFlutter()
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(
            children: [.. children],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: MainAxisAlignment.SpaceAround,
            spacing: 14.0);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        // free = 500 - (300 + 2 * 14) = 172; leading = 172 / 3 / 2, between = 172 / 3 + 14.
        Assert.Equal(172.0 / 3.0 / 2.0, Offset(children[0]).X, 9);
        Assert.Equal(200.0, Offset(children[1]).X, 9);
        Assert.Equal(500.0 - 100.0 - 172.0 / 3.0 / 2.0, Offset(children[2]).X, 9);

        flex.Direction = Axis.Vertical;
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([12.0, 150.0, 288.0], children.Select(c => Offset(c).Y));
    }

    // ---------- Flexible fits ----------

    [Fact]
    public void RenderFlex_LooseFit_ClampsTheChildToItsFlexAllotment()
    {
        RenderBox first = Box(100, 100);
        RenderBox second = Box(100, 100);
        RenderBox third = Box(100, 100);
        var flex = new RenderFlex(
            children: [first, second, third],
            textDirection: TextDirection.Ltr,
            mainAxisAlignment: MainAxisAlignment.SpaceBetween);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([0.0, 200.0, 400.0], new[] { first, second, third }.Select(c => Offset(c).X));

        SetFlex(first, 1, FlexFit.Loose);
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(100.0, first.Size.Width);
        Assert.Equal([0.0, 200.0, 400.0], new[] { first, second, third }.Select(c => Offset(c).X));

        ((FixedSizeBox)first).PreferredSize = new Size(1000, 100);
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal(300.0, first.Size.Width);
        Assert.Equal(0.0, Offset(first).X);
        Assert.Equal(300.0, Offset(second).X);
        Assert.Equal(400.0, Offset(third).X);
    }

    [Fact]
    public void RenderFlex_FlexibleWithMainAxisSizeMin_OnlyGrowsForTightFits()
    {
        RenderBox first = Box(100, 100);
        RenderBox second = Box(100, 100);
        RenderBox third = Box(100, 100);
        var flex = new RenderFlex(
            children: [first, second, third],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min,
            mainAxisAlignment: MainAxisAlignment.SpaceBetween);

        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([0.0, 100.0, 200.0], new[] { first, second, third }.Select(c => Offset(c).X));
        Assert.Equal(300.0, flex.Size.Width);

        SetFlex(first, 1, FlexFit.Tight);
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([0.0, 300.0, 400.0], new[] { first, second, third }.Select(c => Offset(c).X));
        Assert.Equal(300.0, first.Size.Width);
        Assert.Equal(500.0, flex.Size.Width);

        SetFlex(first, 1, FlexFit.Loose);
        Layout(flex, new BoxConstraints(MaxWidth: 500, MaxHeight: 400));
        Assert.Equal([0.0, 100.0, 200.0], new[] { first, second, third }.Select(c => Offset(c).X));
        Assert.Equal(100.0, first.Size.Width);
        Assert.Equal(300.0, flex.Size.Width);
    }

    // ---------- Directional flips ----------

    [Fact]
    public void RenderFlex_DirectionalFlips_MatchFlutter()
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(children: [.. children], textDirection: TextDirection.Ltr);
        var constraints = new BoxConstraints(800, 800, 600, 600);

        void Expect(params (double X, double Y)[] expected)
        {
            Layout(flex, constraints);
            Assert.Equal(
                expected.Select(e => new Point(e.X, e.Y)).ToArray(),
                children.Select(Offset).ToArray());
        }

        Expect((0, 250), (100, 250), (200, 250));

        flex.MainAxisAlignment = MainAxisAlignment.End;
        Expect((500, 250), (600, 250), (700, 250));

        flex.TextDirection = TextDirection.Rtl;
        Expect((200, 250), (100, 250), (0, 250));

        flex.MainAxisAlignment = MainAxisAlignment.Start;
        Expect((700, 250), (600, 250), (500, 250));

        flex.CrossAxisAlignment = CrossAxisAlignment.Start;
        Expect((700, 0), (600, 0), (500, 0));

        flex.CrossAxisAlignment = CrossAxisAlignment.End;
        Expect((700, 500), (600, 500), (500, 500));

        flex.VerticalDirection = VerticalDirection.Up;
        Expect((700, 0), (600, 0), (500, 0));

        flex.CrossAxisAlignment = CrossAxisAlignment.Start;
        Expect((700, 500), (600, 500), (500, 500));

        flex.Direction = Axis.Vertical;
        Expect((700, 500), (700, 400), (700, 300));

        flex.CrossAxisAlignment = CrossAxisAlignment.End;
        Expect((0, 500), (0, 400), (0, 300));

        flex.CrossAxisAlignment = CrossAxisAlignment.Stretch;
        Layout(flex, constraints);
        Assert.Equal([new Point(0, 500), new Point(0, 400), new Point(0, 300)], children.Select(Offset));
        Assert.All(children, child => Assert.Equal(new Size(800, 100), child.Size));

        flex.TextDirection = TextDirection.Ltr;
        Layout(flex, constraints);
        Assert.Equal([new Point(0, 500), new Point(0, 400), new Point(0, 300)], children.Select(Offset));

        flex.CrossAxisAlignment = CrossAxisAlignment.Start;
        Layout(flex, constraints);
        Assert.Equal([new Point(0, 500), new Point(0, 400), new Point(0, 300)], children.Select(Offset));
        Assert.All(children, child => Assert.Equal(new Size(100, 100), child.Size));

        flex.CrossAxisAlignment = CrossAxisAlignment.End;
        Expect((700, 500), (700, 400), (700, 300));

        flex.VerticalDirection = VerticalDirection.Down;
        Expect((700, 0), (700, 100), (700, 200));

        flex.MainAxisAlignment = MainAxisAlignment.End;
        Expect((700, 300), (700, 400), (700, 500));
    }

    // ---------- Baselines ----------

    [Fact]
    public void RenderFlex_BaselineAlignment_AlignsBaselinesAndSizesTheCrossAxis()
    {
        RenderBox first = BaselineBox(50, 30, 20);
        RenderBox second = BaselineBox(50, 40, 10);
        var flex = new RenderFlex(
            children: [first, second],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);

        Layout(flex, BoxConstraints.Loose(new Size(400, 400)));

        // ascent = max(20, 10) = 20, descent = max(10, 30) = 30 => cross extent 50.
        Assert.Equal(new Size(100, 50), flex.Size);
        Assert.Equal(0.0, Offset(first).Y);
        Assert.Equal(10.0, Offset(second).Y);
        Assert.Equal(20.0, flex.GetDryBaseline(BoxConstraints.Loose(new Size(400, 400)), TextBaseline.Alphabetic));
    }

    [Fact]
    public void RenderFlex_BaselineAlignment_ChildrenWithoutBaselinesAreTopAligned()
    {
        RenderBox withBaseline = BaselineBox(50, 30, 20);
        RenderBox withoutBaseline = Box(50, 100);
        var flex = new RenderFlex(
            children: [withBaseline, withoutBaseline],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic,
            verticalDirection: VerticalDirection.Up);

        Layout(flex, BoxConstraints.Loose(new Size(400, 400)));
        Assert.Equal(0.0, Offset(withoutBaseline).Y);

        flex.VerticalDirection = VerticalDirection.Down;
        Layout(flex, BoxConstraints.Loose(new Size(400, 400)));
        Assert.Equal(0.0, Offset(withoutBaseline).Y);
    }

    [Fact]
    public void RenderFlex_HorizontalBaseline_UsesTheHighestChildBaseline()
    {
        RenderBox first = BaselineBox(50, 40, 30);
        RenderBox second = BaselineBox(50, 40, 10);
        var flex = new RenderFlex(
            children: [first, second],
            textDirection: TextDirection.Ltr,
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Center);

        Layout(flex, BoxConstraints.Loose(new Size(400, 400)));

        // Both children are centred in a 40-tall flex, so their offsets are zero and the
        // highest (smallest) baseline wins.
        Assert.Equal(10.0, flex.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
    }

    [Fact]
    public void RenderFlex_VerticalBaseline_UsesTheFirstChildWithABaseline()
    {
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 600);
        RenderBox box1 = BaselineBox(100, 100, 10);
        RenderBox box2 = BaselineBox(100, 100, 10);
        var flex = new RenderFlex(
            children: [Box(100, 100), box1, Box(100, 100), box2, Box(100, 100)],
            direction: Axis.Vertical,
            mainAxisAlignment: MainAxisAlignment.Start);

        Assert.Equal(110.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));

        flex.MainAxisAlignment = MainAxisAlignment.End;
        Assert.Equal(210.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));

        flex.VerticalDirection = VerticalDirection.Up;
        Assert.Equal(310.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));

        flex.MainAxisAlignment = MainAxisAlignment.Start;
        Assert.Equal(410.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));
    }

    [Fact]
    public void RenderFlex_VerticalBaseline_LaidOutMatchesTheDryBaseline()
    {
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 600);
        RenderBox box1 = BaselineBox(100, 100, 10);
        var flex = new RenderFlex(
            children: [Box(100, 100), box1, Box(100, 100)],
            direction: Axis.Vertical,
            mainAxisAlignment: MainAxisAlignment.Start);

        Layout(flex, constraints);

        Assert.Equal(110.0, flex.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
    }

    // ---------- Intrinsics ----------

    [Fact]
    public void RenderFlex_MainAxisIntrinsics_ScaleTheLargestFlexFraction()
    {
        RenderBox first = Box(100, 100);
        RenderBox second = Box(100, 100);
        var flex = new RenderFlex(children: [first, second], textDirection: TextDirection.Ltr);
        SetFlex(second, 1, FlexFit.Tight);

        // maxFlexFraction = 100/1, totalFlex = 1, inflexible = 100.
        Assert.Equal(200.0, flex.GetMaxIntrinsicWidth(double.PositiveInfinity));

        SetFlex(second, 2, FlexFit.Tight);
        Assert.Equal(200.0, flex.GetMaxIntrinsicWidth(double.PositiveInfinity));
    }

    [Fact]
    public void RenderFlex_IntrinsicsWithSpacing_AreDirectionSensitive()
    {
        RenderBox[] children = [Box(100, 100), Box(100, 100), Box(100, 100)];
        var flex = new RenderFlex(
            children: [.. children],
            textDirection: TextDirection.Ltr,
            spacing: 16.0);

        foreach (double extent in new[] { double.PositiveInfinity, 300.0, 500.0 })
        {
            Assert.Equal(332.0, flex.GetMinIntrinsicWidth(extent));
            Assert.Equal(332.0, flex.GetMaxIntrinsicWidth(extent));
            Assert.Equal(100.0, flex.GetMinIntrinsicHeight(extent));
            Assert.Equal(100.0, flex.GetMaxIntrinsicHeight(extent));
        }

        flex.Direction = Axis.Vertical;
        foreach (double extent in new[] { double.PositiveInfinity, 300.0, 500.0 })
        {
            Assert.Equal(100.0, flex.GetMinIntrinsicWidth(extent));
            Assert.Equal(332.0, flex.GetMinIntrinsicHeight(extent));
            Assert.Equal(332.0, flex.GetMaxIntrinsicHeight(extent));
        }
    }

    [Fact]
    public void RenderFlex_CrossAxisIntrinsics_UseAscendingFlexFlowLayout()
    {
        var flex = new RenderFlex(
            children: [Box(5, 5), Segments(3), Segments(3)],
            textDirection: TextDirection.Ltr);
        SetFlex(flex.LastChild!, 2, FlexFit.Tight);
        SetFlex(flex.ChildBefore(flex.LastChild!)!, 1, FlexFit.Tight);

        // Max intrinsic width: maxFlexFraction = max(30/1, 30/2) = 30, totalFlex = 3, inflexible 5.
        Assert.Equal(95.0, flex.GetMaxIntrinsicWidth(double.PositiveInfinity));

        Assert.Equal(10.0, flex.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(10.0, flex.GetMinIntrinsicHeight(95.0));
        Assert.Equal(20.0, flex.GetMinIntrinsicHeight(94.0));
        Assert.Equal(20.0, flex.GetMinIntrinsicHeight(65.0));
        Assert.Equal(30.0, flex.GetMinIntrinsicHeight(35.0));
    }

    [Fact]
    public void RenderFlex_CrossAxisIntrinsics_UseDescendingFlexFlowLayout()
    {
        var flex = new RenderFlex(
            children: [Box(5, 5), Segments(3), Segments(3)],
            textDirection: TextDirection.Ltr);
        SetFlex(flex.ChildBefore(flex.LastChild!)!, 2, FlexFit.Tight);
        SetFlex(flex.LastChild!, 1, FlexFit.Tight);

        Assert.Equal(10.0, flex.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(10.0, flex.GetMinIntrinsicHeight(95.0));
        Assert.Equal(20.0, flex.GetMinIntrinsicHeight(65.0));
        Assert.Equal(30.0, flex.GetMinIntrinsicHeight(35.0));
    }

    [Fact]
    public void RenderFlex_BaselineAligned_DryLayoutAndDryBaselineFollowTheFlexAllotment()
    {
        RenderBox top = BaselineBox(10, 10, 0);
        RenderBox bottom = BaselineBox(10, 10, 10);
        var flex = new RenderFlex(
            children: [top, bottom],
            textDirection: TextDirection.Ltr,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);
        SetFlex(top, 2, FlexFit.Tight);
        SetFlex(bottom, 1, FlexFit.Loose);

        var constraints = BoxConstraints.Loose(new Size(200, 100));

        // ascent = max(0, 10) = 10, descent = max(10, 0) = 10 => cross extent 20.
        Assert.Equal(new Size(200, 20), flex.GetDryLayout(constraints));
        Assert.Equal(10.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));
        Assert.Equal(20.0, flex.GetMinIntrinsicHeight(200));
        Assert.Equal(20.0, flex.GetMaxIntrinsicHeight(200));
    }

    [Fact]
    public void RenderFlex_ChildrenWithoutBaselines_DoNotMoveTheBaseline()
    {
        RenderBox withBaseline = BaselineBox(10, 10, 10);
        RenderBox withoutBaseline = Box(10, 10);
        var flex = new RenderFlex(
            children: [withBaseline, withoutBaseline],
            textDirection: TextDirection.Ltr,
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: TextBaseline.Alphabetic);
        SetFlex(withBaseline, 2, FlexFit.Tight);
        SetFlex(withoutBaseline, 1, FlexFit.Loose);

        var constraints = BoxConstraints.Loose(new Size(200, 100));

        Assert.Equal(new Size(200, 10), flex.GetDryLayout(constraints));
        Assert.Equal(10.0, flex.GetDryBaseline(constraints, TextBaseline.Alphabetic));
    }

    // ---------- Widget layer ----------

    [Fact]
    public void Flex_BaselineWithoutTextBaseline_IsADebugOnlyAssertion()
    {
        Flex? flex = null;
        Exception? error = Record.Exception(() => flex = new Flex(
            direction: Axis.Horizontal,
            crossAxisAlignment: CrossAxisAlignment.Baseline));

        if (Constants.KDebugMode)
        {
            var assertion = Assert.IsType<AssertionError>(error);
            Assert.Equal(
                "textBaseline is required if you specify the crossAxisAlignment with "
                + "CrossAxisAlignment.Baseline",
                assertion.Message);
        }
        else
        {
            Assert.Null(error);
            Assert.NotNull(flex);
        }
    }

    [Fact]
    public void Flex_ClipBehavior_DefaultsToNoneAndUpdates()
    {
        using var harness = new FlexHarness(new Flex(direction: Axis.Vertical));
        harness.Pump(new Size(800, 600));

        var flex = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal(Clip.None, flex.ClipBehavior);

        harness.Update(new Flex(direction: Axis.Vertical, clipBehavior: Clip.AntiAlias));
        harness.Pump(new Size(800, 600));
        Assert.Equal(Clip.AntiAlias, flex.ClipBehavior);
    }

    [Fact]
    public void Flex_EffectiveTextDirection_IsOnlyResolvedWhenNeeded()
    {
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Rtl,
                child: new Column(crossAxisAlignment: CrossAxisAlignment.Center)));
        harness.Pump(new Size(800, 600));

        var column = (RenderFlex)harness.RenderView.Child!;
        Assert.Null(column.TextDirection);

        harness.Update(
            new Directionality(
                TextDirection.Rtl,
                child: new Column(crossAxisAlignment: CrossAxisAlignment.Start)));
        harness.Pump(new Size(800, 600));
        Assert.Equal(TextDirection.Rtl, column.TextDirection);
    }

    [Fact]
    public void Row_WithoutDirectionality_Throws()
    {
        using var harness = new FlexHarness(
            new Row(children: [new SizedBox(width: 100, height: 100), new SizedBox(width: 100, height: 100)]));

        Exception? error = Record.Exception(() => harness.Pump(new Size(800, 600)));

        if (Constants.KDebugMode)
        {
            Assert.IsType<FlutterError>(error);
        }
        else
        {
            Assert.Null(error);
        }
    }

    [Theory]
    [InlineData(TextDirection.Ltr, new[] { 0.0, 100.0, 200.0 })]
    [InlineData(TextDirection.Rtl, new[] { 700.0, 600.0, 500.0 })]
    public void Row_DefaultMainAxisParameters_PositionChildren(TextDirection direction, double[] expected)
    {
        using var harness = new FlexHarness(
            new Directionality(
                direction,
                child: new Row(children: [Sized(), Sized(), Sized()])));
        harness.Pump(new Size(800, 600));

        var row = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal(new Size(800, 100), row.Size);
        Assert.Equal(expected, Children(row).Select(c => Offset(c).X));
    }

    [Fact]
    public void Row_WithOneFlexibleChild_GivesItTheRemainingSpace()
    {
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Ltr,
                child: new Row(children:
                [
                    Sized(),
                    new Expanded(child: Sized()),
                    Sized(),
                ])));
        harness.Pump(new Size(800, 600));

        var row = (RenderFlex)harness.RenderView.Child!;
        List<RenderBox> children = Children(row);
        Assert.Equal([0.0, 100.0, 700.0], children.Select(c => Offset(c).X));
        Assert.Equal(600.0, children[1].Size.Width);
    }

    [Fact]
    public void Flexible_DefaultsToLooseFit()
    {
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Ltr,
                child: new Row(children:
                [
                    new Flexible(child: new SizedBox(width: 100, height: 200)),
                ])));
        harness.Pump(new Size(800, 600));

        var row = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal(100.0, row.FirstChild!.Size.Width);
    }

    [Fact]
    public void Column_SpaceAround_PositionsFourChildren()
    {
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Ltr,
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.SpaceAround,
                    children: [Sized(), Sized(), Sized(), Sized()])));
        harness.Pump(new Size(800, 600));

        var column = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal([25.0, 175.0, 325.0, 475.0], Children(column).Select(c => Offset(c).Y));
    }

    [Fact]
    public void Column_VerticalDirectionUp_ReversesTheChildren()
    {
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Ltr,
                child: new Column(
                    verticalDirection: VerticalDirection.Up,
                    children: [Sized(), Sized(), Sized()])));
        harness.Pump(new Size(800, 600));

        var column = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal([500.0, 400.0, 300.0], Children(column).Select(c => Offset(c).Y));
    }

    [Fact]
    public void Column_Spacing_DefaultsToZeroAndUpdates()
    {
        Assert.Equal(0.0, new Column().Spacing);

        using var harness = new FlexHarness(BuildSpacedColumn(8.0));
        harness.Pump(new Size(800, 600));

        var column = (RenderFlex)harness.RenderView.Child!;
        Assert.Equal(8.0, column.Spacing);
        Assert.Equal(new Size(100, 316), column.Size);

        harness.Update(BuildSpacedColumn(18.0));
        harness.Pump(new Size(800, 600));
        Assert.Equal(18.0, column.Spacing);
        Assert.Equal(new Size(100, 336), column.Size);
    }

    [Fact]
    public void Spacer_TakesUpSpaceProportionalToFlex()
    {
        var firstSpacer = new Spacer();
        var secondSpacer = new Spacer(flex: 2);
        using var harness = new FlexHarness(
            new Directionality(
                TextDirection.Ltr,
                child: new Row(children:
                [
                    new SizedBox(width: 10, height: 10),
                    firstSpacer,
                    secondSpacer,
                    new SizedBox(width: 10, height: 10),
                ])));
        harness.Pump(new Size(800, 600));

        var row = (RenderFlex)harness.RenderView.Child!;
        List<RenderBox> children = Children(row);
        // 780 free logical pixels split 1:2.
        Assert.Equal(260.0, children[1].Size.Width, 6);
        Assert.Equal(520.0, children[2].Size.Width, 6);
        Assert.Equal(0.0, children[1].Size.Height);
    }

    [Fact]
    public void Spacer_RequiresAPositiveFlex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spacer(flex: 0));
    }

    // ---------- Helpers ----------

    private static Widget BuildSpacedColumn(double spacing) => new Directionality(
        TextDirection.Ltr,
        child: new Column(
            mainAxisSize: MainAxisSize.Min,
            spacing: spacing,
            children: [Sized(), Sized(), Sized()]));

    private static Widget Sized() => new SizedBox(width: 100, height: 100);

    private static List<RenderBox> Children(RenderFlex flex)
    {
        var result = new List<RenderBox>();
        for (RenderBox? child = flex.FirstChild; child != null; child = flex.ChildAfter(child))
        {
            result.Add(child);
        }

        return result;
    }

    private static Point Offset(RenderBox child) => ((FlexParentData)child.parentData!).offset;

    private static void SetFlex(RenderBox child, int flex, FlexFit fit)
    {
        var parentData = (FlexParentData)child.parentData!;
        parentData.flex = flex;
        parentData.fit = fit;
        child.Parent?.MarkNeedsLayout();
    }

    private static RenderBox Box(double width, double height) => new FixedSizeBox(new Size(width, height));

    private static RenderBox ZeroIntrinsicBox() => new FixedSizeBox(new Size(0, 0));

    private static RenderBox BaselineBox(double width, double height, double baseline) =>
        new FixedSizeBox(new Size(width, height)) { Baseline = baseline };

    private static RenderBox Segments(int count) => new SegmentedBox(count);

    private readonly Dictionary<RenderBox, ConstraintsHost> _hosts = [];

    private void Layout(RenderBox box, BoxConstraints constraints)
    {
        if (!_hosts.TryGetValue(box, out ConstraintsHost? host))
        {
            host = new ConstraintsHost(box);
            _hosts[box] = host;
        }

        host.LayoutChild(constraints);
    }

    /// Hosts a render box under a `RenderView` and re-lays it out with arbitrary
    /// constraints, the way Flutter's `layout(box, constraints: ...)` test helper does.
    private sealed class ConstraintsHost : RenderProxyBox
    {
        private readonly RenderView _root;
        private readonly PipelineOwner _pipeline;
        private BoxConstraints _childConstraints;

        public ConstraintsHost(RenderBox child)
        {
            Child = child;
            _root = new RenderView { Child = this };
            _pipeline = new PipelineOwner(_root);
            _pipeline.Attach(_root);
        }

        public void LayoutChild(BoxConstraints constraints)
        {
            _childConstraints = constraints;
            MarkNeedsLayout();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(new Size(2000, 2000));
        }

        protected override void PerformLayout()
        {
            Child!.Layout(_childConstraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child.Size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// A fixed-size box that optionally reports an alphabetic baseline, standing in for
    /// Flutter's `RenderSizedBox`/`_RenderBaselineTestBox` test doubles.
    private sealed class FixedSizeBox : RenderBox
    {
        public FixedSizeBox(Size size)
        {
            PreferredSize = size;
        }

        private Size _preferredSize;

        public Size PreferredSize
        {
            get => _preferredSize;
            set
            {
                if (_preferredSize == value)
                {
                    return;
                }

                _preferredSize = value;
                MarkNeedsLayout();
            }
        }

        public double? Baseline { get; init; }

        protected override double ComputeMinIntrinsicWidth(double height) => PreferredSize.Width;

        protected override double ComputeMaxIntrinsicWidth(double height) => PreferredSize.Width;

        protected override double ComputeMinIntrinsicHeight(double width) => PreferredSize.Height;

        protected override double ComputeMaxIntrinsicHeight(double width) => PreferredSize.Height;

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(PreferredSize);

        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => Baseline;

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => Baseline;

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(PreferredSize);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    /// A box whose intrinsic height depends on how many fixed-width segments fit on a line,
    /// standing in for the wrapping paragraphs Flutter's cross-axis intrinsic tests use.
    private sealed class SegmentedBox : RenderBox
    {
        private const double SegmentWidth = 10.0;
        private const double LineHeight = 10.0;
        private readonly int _segmentCount;

        public SegmentedBox(int segmentCount)
        {
            _segmentCount = segmentCount;
        }

        protected override double ComputeMinIntrinsicWidth(double height) => SegmentWidth;

        protected override double ComputeMaxIntrinsicWidth(double height) => SegmentWidth * _segmentCount;

        protected override double ComputeMinIntrinsicHeight(double width) => HeightForWidth(width);

        protected override double ComputeMaxIntrinsicHeight(double width) => HeightForWidth(width);

        protected override Size ComputeDryLayout(BoxConstraints constraints)
        {
            double width = double.IsFinite(constraints.MaxWidth)
                ? constraints.MaxWidth
                : SegmentWidth * _segmentCount;
            return constraints.Constrain(new Size(width, HeightForWidth(width)));
        }

        protected override void PerformLayout()
        {
            Size = ComputeDryLayout(Constraints);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        private double HeightForWidth(double width)
        {
            if (!double.IsFinite(width))
            {
                return LineHeight;
            }

            int perLine = Math.Max(1, (int)Math.Floor(width / SegmentWidth));
            return LineHeight * Math.Ceiling(_segmentCount / (double)perLine);
        }
    }

    private sealed class FlexHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public FlexHarness(Widget rootWidget)
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

        public void Update(Widget rootWidget)
        {
            _rootElement.Update(rootWidget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

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
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is not RenderBox renderBox)
                {
                    throw new InvalidOperationException("FlexHarness can host only RenderBox.");
                }

                _renderView.Child = renderBox;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
