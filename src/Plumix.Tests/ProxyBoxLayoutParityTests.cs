using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/rendering/intrinsic_width_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/aspect_ratio_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/limited_box_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/proxy_box_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/proxy_getters_and_setters_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/semantics_and_children_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/repaint_boundary_test.dart
// Dart parity source: flutter/packages/flutter/test/rendering/flex_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/constrained_box_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/aspect_ratio_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/intrinsic_width_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/opacity_repaint_test.dart
// Dart parity source: flutter/packages/flutter/test/widgets/animated_opacity_repaint_test.dart

namespace Plumix.Tests;

public sealed class ProxyBoxLayoutParityTests
{
    private static readonly BoxConstraints ProbeConstraints =
        new(MinWidth: 5.0, MaxWidth: 500.0, MinHeight: 8.0, MaxHeight: 800.0);

    private static readonly double[] Probes = [0.0, 10.0, 80.0, double.PositiveInfinity];

    private static RenderTestBox NewTestChild() =>
        new(new BoxConstraints(MinWidth: 10.0, MaxWidth: 100.0, MinHeight: 20.0, MaxHeight: 200.0));

    // ---- rendering/intrinsic_width_test.dart ----

    [Fact]
    public void ShrinkWrappingWidth()
    {
        RenderTestBox child = NewTestChild();
        var parent = new RenderIntrinsicWidth(child: child);
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(100.0, 110.0), parent.Size);
        Assert.Equal(new Size(100.0, 110.0), child.Size);
        AssertIntrinsics(parent, 100.0, 100.0, 20.0, 200.0);
    }

    [Fact]
    public void IntrinsicWidthWithoutAChild()
    {
        var parent = new RenderIntrinsicWidth();
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(5.0, 8.0), parent.Size);
        AssertIntrinsics(parent, 0.0, 0.0, 0.0, 0.0);
    }

    [Fact]
    public void ShrinkWrappingWidth_SteppedWidth()
    {
        RenderTestBox child = NewTestChild();
        var parent = new RenderIntrinsicWidth(stepWidth: 47.0, child: child);
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(3.0 * 47.0, 110.0), parent.Size);
        Assert.Equal(new Size(3.0 * 47.0, 110.0), child.Size);
        AssertIntrinsics(parent, 3.0 * 47.0, 3.0 * 47.0, 20.0, 200.0);
    }

    [Fact]
    public void ShrinkWrappingWidth_SteppedHeight()
    {
        var parent = new RenderIntrinsicWidth(stepHeight: 47.0, child: NewTestChild());
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(100.0, 235.0), parent.Size);
        AssertIntrinsics(parent, 100.0, 100.0, 1.0 * 47.0, 5.0 * 47.0);
    }

    [Fact]
    public void ShrinkWrappingWidth_SteppedEverything()
    {
        var parent = new RenderIntrinsicWidth(stepWidth: 37.0, stepHeight: 47.0, child: NewTestChild());
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(3.0 * 37.0, 235.0), parent.Size);
        AssertIntrinsics(parent, 3.0 * 37.0, 3.0 * 37.0, 1.0 * 47.0, 5.0 * 47.0);
    }

    [Theory]
    [InlineData(50.0, 70.0, 70.0)]
    [InlineData(500.0, 500.0, 500.0)]
    [InlineData(50.0, 50.0, 50.0)]
    public void RenderIntrinsicWidth_ParentWidthConstraints(double minWidth, double maxWidth, double expectedWidth)
    {
        RenderTestBox child = NewTestChild();
        var parent = new RenderIntrinsicWidth(child: child);
        _ = new RenderingHarness(
            parent,
            new BoxConstraints(MinWidth: minWidth, MaxWidth: maxWidth, MinHeight: 8.0, MaxHeight: 800.0));

        Assert.Equal(new Size(expectedWidth, 110.0), parent.Size);
        Assert.Equal(new Size(expectedWidth, 110.0), child.Size);
    }

    [Fact]
    public void ShrinkWrappingHeight()
    {
        var parent = new RenderIntrinsicHeight(child: NewTestChild());
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(55.0, 200.0), parent.Size);
        AssertIntrinsics(parent, 10.0, 100.0, 200.0, 200.0);
    }

    [Fact]
    public void IntrinsicHeightWithoutAChild()
    {
        var parent = new RenderIntrinsicHeight();
        _ = new RenderingHarness(parent, ProbeConstraints);

        Assert.Equal(new Size(5.0, 8.0), parent.Size);
        AssertIntrinsics(parent, 0.0, 0.0, 0.0, 0.0);
    }

    [Theory]
    [InlineData(8.0, 80.0, 80.0)]
    [InlineData(400.0, 400.0, 400.0)]
    [InlineData(80.0, 80.0, 80.0)]
    public void RenderIntrinsicHeight_ParentHeightConstraints(
        double minHeight,
        double maxHeight,
        double expectedHeight)
    {
        RenderTestBox child = NewTestChild();
        var parent = new RenderIntrinsicHeight(child: child);
        _ = new RenderingHarness(
            parent,
            new BoxConstraints(MinWidth: 5.0, MaxWidth: 500.0, MinHeight: minHeight, MaxHeight: maxHeight));

        Assert.Equal(new Size(55.0, expectedHeight), parent.Size);
        Assert.Equal(new Size(55.0, expectedHeight), child.Size);
    }

    [Fact]
    public void PaddingAndInterestingIntrinsics()
    {
        var box = new RenderPadding(EdgeInsets.All(15.0), new RenderAspectRatio(aspectRatio: 1.0));

        foreach (double probe in new[] { 0.0, 10.0, double.PositiveInfinity })
        {
            Assert.Equal(30.0, box.GetMinIntrinsicWidth(probe));
            Assert.Equal(30.0, box.GetMaxIntrinsicWidth(probe));
            Assert.Equal(30.0, box.GetMinIntrinsicHeight(probe));
            Assert.Equal(30.0, box.GetMaxIntrinsicHeight(probe));
        }

        Assert.Equal(80.0, box.GetMinIntrinsicWidth(80.0));
        Assert.Equal(80.0, box.GetMaxIntrinsicWidth(80.0));
        Assert.Equal(80.0, box.GetMinIntrinsicHeight(80.0));
        Assert.Equal(80.0, box.GetMaxIntrinsicHeight(80.0));

        _ = new RenderingHarness(box, BoxConstraints.Tight(new Size(10.0, 10.0)));
    }

    // ---- rendering/aspect_ratio_test.dart ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RenderAspectRatio_IntrinsicSizing(bool withChild)
    {
        var wide = new RenderAspectRatio(
            aspectRatio: 2.0,
            child: withChild ? new RenderSizedBox(new Size(90.0, 70.0)) : null);
        Assert.Equal(400.0, wide.GetMinIntrinsicWidth(200.0));
        Assert.Equal(800.0, wide.GetMinIntrinsicWidth(400.0));
        Assert.Equal(400.0, wide.GetMaxIntrinsicWidth(200.0));
        Assert.Equal(800.0, wide.GetMaxIntrinsicWidth(400.0));
        Assert.Equal(100.0, wide.GetMinIntrinsicHeight(200.0));
        Assert.Equal(200.0, wide.GetMinIntrinsicHeight(400.0));
        Assert.Equal(100.0, wide.GetMaxIntrinsicHeight(200.0));
        Assert.Equal(200.0, wide.GetMaxIntrinsicHeight(400.0));
        AssertInfiniteProbe(wide, withChild);

        var tall = new RenderAspectRatio(
            aspectRatio: 0.5,
            child: withChild ? new RenderSizedBox(new Size(90.0, 70.0)) : null);
        Assert.Equal(100.0, tall.GetMinIntrinsicWidth(200.0));
        Assert.Equal(200.0, tall.GetMinIntrinsicWidth(400.0));
        Assert.Equal(100.0, tall.GetMaxIntrinsicWidth(200.0));
        Assert.Equal(200.0, tall.GetMaxIntrinsicWidth(400.0));
        Assert.Equal(400.0, tall.GetMinIntrinsicHeight(200.0));
        Assert.Equal(800.0, tall.GetMinIntrinsicHeight(400.0));
        Assert.Equal(400.0, tall.GetMaxIntrinsicHeight(200.0));
        Assert.Equal(800.0, tall.GetMaxIntrinsicHeight(400.0));
        AssertInfiniteProbe(tall, withChild);

        static void AssertInfiniteProbe(RenderBox box, bool withChild)
        {
            Assert.Equal(withChild ? 90.0 : 0.0, box.GetMinIntrinsicWidth(double.PositiveInfinity));
            Assert.Equal(withChild ? 90.0 : 0.0, box.GetMaxIntrinsicWidth(double.PositiveInfinity));
            Assert.Equal(withChild ? 70.0 : 0.0, box.GetMinIntrinsicHeight(double.PositiveInfinity));
            Assert.Equal(withChild ? 70.0 : 0.0, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
        }
    }

    [DebugOnlyFact]
    public void RenderAspectRatio_Unbounded()
    {
        var box = new RenderConstrainedOverflowBox(
            maxWidth: double.PositiveInfinity,
            maxHeight: double.PositiveInfinity,
            child: new RenderAspectRatio(aspectRatio: 0.5, child: new RenderSizedBox(new Size(90.0, 70.0))));

        List<FlutterErrorDetails> errors = CollectErrors(() => _ = new RenderingHarness(box));

        Assert.NotEmpty(errors);
        var error = Assert.IsType<FlutterError>(errors[0].Exception);
        Assert.Equal(
            "FlutterError\n"
            + "   RenderAspectRatio has unbounded constraints.\n"
            + "   This RenderAspectRatio was given an aspect ratio of 0.5 but was\n"
            + "   given both unbounded width and unbounded height constraints.\n"
            + "   Because both constraints were unbounded, this render object\n"
            + "   doesn't know how much size to consume.\n",
            error.ToStringDeep());
    }

    [DebugOnlyFact]
    public void RenderAspectRatio_ChecksUnboundedBeforeTight()
    {
        // Dart reports the unbounded error first, so tight-but-infinite constraints still throw.
        var box = new RenderAspectRatio(aspectRatio: 1.0);
        var infinite = new BoxConstraints(
            MinWidth: double.PositiveInfinity,
            MaxWidth: double.PositiveInfinity,
            MinHeight: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity);

        Exception? error = Record.Exception(() => box.GetDryLayout(infinite));

        Assert.IsType<FlutterError>(error);
    }

    [Fact]
    public void RenderAspectRatio_Sizing()
    {
        var inside = new RenderAspectRatio(aspectRatio: 1.0);
        var outside = new RenderConstrainedOverflowBox(child: inside);
        var harness = new RenderingHarness(outside);
        Assert.Equal(new Size(800.0, 600.0), inside.Size);
        outside.MinWidth = 0.0;
        outside.MinHeight = 0.0;

        void Check(double maxWidth, double maxHeight, Size expected)
        {
            outside.MaxWidth = maxWidth;
            outside.MaxHeight = maxHeight;
            harness.PumpFrame();
            Assert.Equal(expected, inside.Size);
        }

        const double inf = double.PositiveInfinity;
        Check(100.0, 90.0, new Size(90.0, 90.0));
        Check(90.0, 100.0, new Size(90.0, 90.0));
        Check(inf, 90.0, new Size(90.0, 90.0));
        Check(90.0, inf, new Size(90.0, 90.0));

        inside.AspectRatio = 2.0;
        Check(100.0, 90.0, new Size(100.0, 50.0));
        Check(90.0, 100.0, new Size(90.0, 45.0));
        Check(inf, 90.0, new Size(180.0, 90.0));
        Check(90.0, inf, new Size(90.0, 45.0));

        outside.MinWidth = 80.0;
        outside.MinHeight = 80.0;
        Check(100.0, 90.0, new Size(100.0, 80.0));
        Check(90.0, 100.0, new Size(90.0, 80.0));
        Check(inf, 90.0, new Size(180.0, 90.0));
        Check(90.0, inf, new Size(90.0, 80.0));
    }

    // ---- rendering/limited_box_test.dart ----

    [Theory]
    [InlineData(0.0, double.PositiveInfinity, 0.0, double.PositiveInfinity, 100.0, 200.0)]
    [InlineData(0.0, double.PositiveInfinity, 500.0, 500.0, 100.0, 500.0)]
    [InlineData(500.0, 500.0, 0.0, double.PositiveInfinity, 500.0, 200.0)]
    public void LimitedBox_ParentMaxSizeIsUnconstrained(
        double minWidth,
        double maxWidth,
        double minHeight,
        double maxHeight,
        double expectedWidth,
        double expectedHeight)
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 300.0, height: 400.0));
        var parent = new RenderConstrainedOverflowBox(
            minWidth: minWidth,
            maxWidth: maxWidth,
            minHeight: minHeight,
            maxHeight: maxHeight,
            child: new RenderLimitedBox(maxWidth: 100.0, maxHeight: 200.0, child: child));
        _ = new RenderingHarness(parent);

        Assert.Equal(new Size(expectedWidth, expectedHeight), child.Size);
    }

    [Fact]
    public void LimitedBox_DumpShowsLimits()
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 300.0, height: 400.0));
        var limited = new RenderLimitedBox(maxWidth: 100.0, maxHeight: 200.0, child: child);
        var parent = new RenderConstrainedOverflowBox(
            minWidth: 0.0,
            maxWidth: double.PositiveInfinity,
            minHeight: 0.0,
            maxHeight: double.PositiveInfinity,
            child: limited);
        _ = new RenderingHarness(parent);

        string dump = FrameworkDartTester.IgnoringHashCodes(limited.ToStringDeep(minLevel: DiagnosticLevel.Info));

        // Dart's dump of the overflow box, from its `└─child: RenderLimitedBox` line down, re-prefixed.
        Assert.Equal(
            "RenderLimitedBox#00000 relayoutBoundary=up1 NEEDS-PAINT NEEDS-COMPOSITING-BITS-UPDATE\n"
            + " │ parentData: offset=Offset(350.0, 200.0) (can use size)\n"
            + " │ constraints: BoxConstraints(unconstrained)\n"
            + " │ size: Size(100.0, 200.0)\n"
            + " │ maxWidth: 100.0\n"
            + " │ maxHeight: 200.0\n"
            + " │\n"
            + " └─child: RenderConstrainedBox#00000 relayoutBoundary=up2 NEEDS-PAINT\n"
            + "     parentData: <none> (can use size)\n"
            + "     constraints: BoxConstraints(0.0<=w<=100.0, 0.0<=h<=200.0)\n"
            + "     size: Size(100.0, 200.0)\n"
            + "     additionalConstraints: BoxConstraints(w=300.0, h=400.0)\n",
            dump);
    }

    [Fact]
    public void LimitedBox_NoChild()
    {
        var box = new RenderLimitedBox(maxWidth: 100.0, maxHeight: 200.0);
        var parent = new RenderConstrainedOverflowBox(
            minWidth: 10.0,
            maxWidth: 500.0,
            minHeight: 0.0,
            maxHeight: double.PositiveInfinity,
            child: box);
        _ = new RenderingHarness(parent);

        Assert.Equal(new Size(10.0, 0.0), box.Size);
    }

    [Fact]
    public void LimitedBox_NoChildUseParent()
    {
        var box = new RenderLimitedBox(maxWidth: 100.0, maxHeight: 200.0);
        var parent = new RenderConstrainedOverflowBox(minWidth: 10.0, child: box);
        _ = new RenderingHarness(parent);

        Assert.Equal(new Size(10.0, 600.0), box.Size);
        Assert.Equal(new BoxConstraints(MinWidth: 10.0, MaxWidth: 800.0, MinHeight: 600.0, MaxHeight: 600.0),
            box.Constraints);
    }

    // ---- rendering/flex_test.dart ----

    [Fact]
    public void Flex_VerticalFlippedConstraints()
    {
        var flex = new RenderFlex(
            children: [new RenderAspectRatio(aspectRatio: 1.0)],
            direction: Axis.Vertical);
        _ = new RenderingHarness(flex, new BoxConstraints(MaxWidth: 1000.0, MaxHeight: 200.0));

        Assert.Equal(0.0, flex.GetMaxIntrinsicWidth(200.0));
    }

    [Fact]
    public void Flex_MainAxisIntrinsicsWithRenderAspectRatio()
    {
        BoxConstraints square = BoxConstraints.TightFor(width: 100.0, height: 100.0);
        var box1 = new RenderConstrainedBox(square);
        var box2 = new RenderConstrainedBox(square);
        var box3 = new RenderAspectRatio(aspectRatio: 1.0, child: new RenderConstrainedBox(square));
        var flex = new RenderFlex(children: [box1, box2, box3], textDirection: TextDirection.Ltr);
        var box2ParentData = (FlexParentData)box2.parentData!;
        box2ParentData.flex = 1;
        box2ParentData.fit = FlexFit.Tight;

        Assert.Equal(300.0, flex.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(300.0, flex.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(200.0 + 300.0, flex.GetMinIntrinsicWidth(300.0));
        Assert.Equal(200.0 + 300.0, flex.GetMaxIntrinsicWidth(300.0));
        Assert.Equal(200.0 + 500.0, flex.GetMinIntrinsicWidth(500.0));
        Assert.Equal(200.0 + 500.0, flex.GetMaxIntrinsicWidth(500.0));
    }

    // ---- rendering/proxy_getters_and_setters_test.dart ----

    [Fact]
    public void GettersAndSetters()
    {
        var constrained = new RenderConstrainedBox(BoxConstraints.TightFor(height: 10.0));
        Assert.Equal(new BoxConstraints(MinHeight: 10.0, MaxHeight: 10.0), constrained.AdditionalConstraints);
        constrained.AdditionalConstraints = BoxConstraints.TightFor(width: 10.0);
        Assert.Equal(new BoxConstraints(MinWidth: 10.0, MaxWidth: 10.0), constrained.AdditionalConstraints);

        var limited = new RenderLimitedBox();
        Assert.Equal(double.PositiveInfinity, limited.MaxWidth);
        Assert.Equal(double.PositiveInfinity, limited.MaxHeight);
        limited.MaxWidth = 0.0;
        limited.MaxHeight = 1.0;
        Assert.Equal(0.0, limited.MaxWidth);
        Assert.Equal(1.0, limited.MaxHeight);

        var aspectRatio = new RenderAspectRatio(aspectRatio: 1.0);
        Assert.Equal(1.0, aspectRatio.AspectRatio);
        aspectRatio.AspectRatio = 0.2;
        Assert.Equal(0.2, aspectRatio.AspectRatio);
        aspectRatio.AspectRatio = 1.2;
        Assert.Equal(1.2, aspectRatio.AspectRatio);

        var intrinsicWidth = new RenderIntrinsicWidth();
        Assert.Null(intrinsicWidth.StepWidth);
        Assert.Null(intrinsicWidth.StepHeight);
        intrinsicWidth.StepWidth = 10.0;
        intrinsicWidth.StepHeight = 10.0;
        Assert.Equal(10.0, intrinsicWidth.StepWidth);
        Assert.Equal(10.0, intrinsicWidth.StepHeight);

        var opacity = new RenderOpacity();
        Assert.Equal(1.0, opacity.Opacity);
        opacity.Opacity = 0.0;
        Assert.Equal(0.0, opacity.Opacity);
    }

    [Fact]
    public void RenderConstrainedBox_TightBoundedAxisDoesNotQueryTheChild()
    {
        var child = new RenderSizedBox(new Size(37.0, 21.0));
        var box = new RenderConstrainedBox(BoxConstraints.TightFor(width: 55.0, height: 44.0), child);

        Assert.Equal(55.0, box.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(55.0, box.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(44.0, box.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(44.0, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(0, child.IntrinsicQueries);
    }

    // ---- rendering/proxy_box_test.dart (opacity) ----

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    [InlineData(0.1, true)]
    public void RenderOpacity_CompositesUnlessTransparent(double value, bool expected)
    {
        var opacity = new RenderOpacity(opacity: value, child: new RenderSizedBox(new Size(1.0, 1.0)));
        new RenderingHarness(opacity).PumpFrame(paint: true);

        Assert.Equal(expected, opacity.NeedsCompositing);
    }

    [Fact]
    public void RenderOpacity_ReusesItsLayer()
    {
        TestLayerReuse(new RenderOpacity(
            opacity: 0.5,
            child: new RenderRepaintBoundary(child: new RenderSizedBox(new Size(1.0, 1.0)))));
    }

    [Fact]
    public void RenderOpacity_ImplementsPaintsChild()
    {
        var box = new RenderSizedBox(new Size(1.0, 1.0));
        var opacity = new RenderOpacity(child: box);

        Assert.True(opacity.PaintsChild(box));
        opacity.Opacity = 0;
        Assert.False(opacity.PaintsChild(box));
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    [InlineData(0.5, true)]
    public void RenderAnimatedOpacity_CompositesUnlessTransparent(double value, bool expected)
    {
        using var animation = new AnimationController(value: value);
        var opacity = new RenderAnimatedOpacity(opacity: animation, child: new RenderSizedBox(new Size(1.0, 1.0)));
        new RenderingHarness(opacity).PumpFrame(paint: true);

        Assert.Equal(expected, opacity.NeedsCompositing);
    }

    [Fact]
    public void RenderAnimatedOpacity_ReusesItsLayer()
    {
        using var animation = new AnimationController(value: 0.5);
        TestLayerReuse(new RenderAnimatedOpacity(
            opacity: animation,
            child: new RenderSizedBox(new Size(1.0, 1.0))));
    }

    [Fact]
    public void RenderAnimatedOpacity_PaintsChildTracksTheAnimation()
    {
        var box = new RenderSizedBox(new Size(1.0, 1.0));
        using var animation = new AnimationController(value: 1.0);
        var opacity = new RenderAnimatedOpacity(opacity: animation, child: box);
        _ = new RenderingHarness(opacity);

        Assert.True(opacity.PaintsChild(box));
        animation.SetValue(0.0);
        Assert.False(opacity.PaintsChild(box));
    }

    // ---- rendering/semantics_and_children_test.dart ----

    [Fact]
    public void RenderOpacity_ChildrenAndSemantics()
    {
        var box = new RenderOpacity(child: new RenderSizedBox(new Size(1.0, 1.0)));
        Assert.Equal(1, CountSemanticsChildren(box));
        box.Opacity = 0.5;
        Assert.Equal(1, CountSemanticsChildren(box));
        box.Opacity = 0.25;
        Assert.Equal(1, CountSemanticsChildren(box));
        box.Opacity = 0.125;
        Assert.Equal(1, CountSemanticsChildren(box));
        box.Opacity = 0.0;
        Assert.Equal(0, CountSemanticsChildren(box));
        box.Opacity = 0.125;
        Assert.Equal(1, CountSemanticsChildren(box));
        box.Opacity = 0.0;
        Assert.Equal(0, CountSemanticsChildren(box));
    }

    [Fact]
    public void RenderAnimatedOpacity_ChildrenAndSemantics()
    {
        using var controller = new AnimationController();
        var box = new RenderAnimatedOpacity(opacity: controller, child: new RenderSizedBox(new Size(1.0, 1.0)));
        Assert.Equal(0, CountSemanticsChildren(box)); // controller defaults to 0.0
        controller.SetValue(0.2); // has no effect, box isn't subscribed yet
        Assert.Equal(0, CountSemanticsChildren(box));
        controller.SetValue(1.0); // ditto
        Assert.Equal(0, CountSemanticsChildren(box)); // alpha is still 0
        _ = new RenderingHarness(box); // this causes the box to attach, which makes it subscribe
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(1.0);
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(0.5);
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(0.25);
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(0.125);
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(0.0);
        Assert.Equal(0, CountSemanticsChildren(box));
        controller.SetValue(0.125);
        Assert.Equal(1, CountSemanticsChildren(box));
        controller.SetValue(0.0);
        Assert.Equal(0, CountSemanticsChildren(box));
    }

    // ---- rendering/repaint_boundary_test.dart (RenderOpacity as the parent) ----

    [Fact]
    public void NestedRepaintBoundaries_SmokeTest()
    {
        RenderOpacity c = new();
        RenderOpacity b = new(child: new RenderRepaintBoundary(child: c));
        RenderOpacity a = new(child: new RenderRepaintBoundary(child: b));
        var harness = new RenderingHarness(a);
        harness.PumpFrame(paint: true, semantics: true);
        c.Opacity = 0.9;
        harness.PumpFrame(paint: true, semantics: true);
        a.Opacity = 0.8;
        c.Opacity = 0.8;
        harness.PumpFrame(paint: true, semantics: true);
        a.Opacity = 0.7;
        b.Opacity = 0.7;
        c.Opacity = 0.7;
        harness.PumpFrame(paint: true, semantics: true);
    }

    [Fact]
    public void Framework_CreatesAnOffsetLayerOnlyForARepaintBoundaryChild()
    {
        var repaintBoundary = new TestBox(isRepaintBoundary: true, setsOwnLayer: false);
        new RenderingHarness(new RenderOpacity(child: repaintBoundary)).PumpFrame(paint: true, semantics: true);
        Assert.IsType<OffsetLayer>(repaintBoundary.DebugLayer);

        var nonComposited = new TestBox(isRepaintBoundary: false, setsOwnLayer: false);
        new RenderingHarness(new RenderOpacity(child: nonComposited)).PumpFrame(paint: true, semantics: true);
        Assert.Null(nonComposited.DebugLayer);

        var composited = new TestBox(isRepaintBoundary: false, setsOwnLayer: true);
        new RenderingHarness(new RenderOpacity(child: composited)).PumpFrame(paint: true, semantics: true);
        Assert.IsType<OpacityLayer>(composited.DebugLayer);
    }

    // ---- widgets/constrained_box_test.dart ----

    [Theory]
    [InlineData(0.0, double.PositiveInfinity, 20.0, double.PositiveInfinity, 0.0, 20.0)]
    [InlineData(20.0, double.PositiveInfinity, 0.0, double.PositiveInfinity, 20.0, 0.0)]
    [InlineData(0.0, double.PositiveInfinity, 0.0, 20.0, 0.0, 0.0)]
    [InlineData(0.0, 20.0, 0.0, double.PositiveInfinity, 0.0, 0.0)]
    [InlineData(10.0, 10.0, 30.0, 30.0, 10.0, 30.0)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 20.0, double.PositiveInfinity, 0.0, 20.0)]
    [InlineData(20.0, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 20.0, 0.0)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity,
        double.PositiveInfinity, 0.0, 0.0)]
    public void ConstrainedBox_Intrinsics(
        double minWidth,
        double maxWidth,
        double minHeight,
        double maxHeight,
        double expectedWidth,
        double expectedHeight)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ConstrainedBox(
            constraints: new BoxConstraints(
                MinWidth: minWidth,
                MaxWidth: maxWidth,
                MinHeight: minHeight,
                MaxHeight: maxHeight),
            child: new Placeholder()));

        var box = (RenderBox)tester.ElementOfType<ConstrainedBox>().RenderObject!;
        Assert.Equal(expectedWidth, box.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(expectedWidth, box.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(expectedHeight, box.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(expectedHeight, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
    }

    [Fact]
    public void Placeholder_Intrinsics()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Placeholder());

        var box = (RenderBox)tester.ElementOfType<Placeholder>().RenderObject!;
        Assert.Equal(0.0, box.GetMinIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(0.0, box.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(0.0, box.GetMinIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(0.0, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
    }

    [Fact]
    public void ConstrainedBox_DoesNotCrashAtZeroArea()
    {
        AssertZeroArea<ConstrainedBox>(new ConstrainedBox(new BoxConstraints(MinWidth: 200, MaxWidth: 400)));
    }

    // ---- widgets/aspect_ratio_test.dart and widgets/intrinsic_width_test.dart ----

    [Fact]
    public void AspectRatio_ControlTest()
    {
        Assert.Equal(new Size(500.0, 250.0), GetAspectRatioChildSize(BoxConstraints.Loose(new Size(500, 500)), 2.0));
        Assert.Equal(new Size(250.0, 500.0), GetAspectRatioChildSize(BoxConstraints.Loose(new Size(500, 500)), 0.5));
    }

    [Fact]
    public void AspectRatio_InfiniteWidth()
    {
        using var tester = new FrameworkDartTester();
        var childKey = new UniqueKey();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(child: new SingleChildScrollView(
                scrollDirection: Axis.Horizontal,
                child: new AspectRatio(2.0, new Container(key: childKey))))));

        var box = (RenderBox)tester.ElementsWithKey(childKey).Single().RenderObject!;
        Assert.Equal(new Size(1200.0, 600.0), box.Size);
    }

    [Fact]
    public void AspectRatio_DoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AspectRatio>(new AspectRatio(2.0, new Placeholder()));
    }

    [Fact]
    public void IntrinsicWidth_DoesNotCrashAtZeroArea()
    {
        AssertZeroArea<IntrinsicWidth>(new IntrinsicWidth(child: new Placeholder()));
    }

    // ---- widgets/opacity_repaint_test.dart ----

    [Fact]
    public void RenderOpacity_AvoidsRepaintingAndDoesNotDropLayerAtFullyOpaque()
    {
        using var tester = new FrameworkDartTester();
        var counter = new PaintCounter();

        tester.PumpWidget(OpacityTree(0.0, counter));
        Assert.Equal(0, counter.Count);

        tester.PumpWidget(OpacityTree(0.1, counter));
        Assert.Equal(1, counter.Count);

        tester.PumpWidget(OpacityTree(1.0, counter));
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public void RenderOpacity_AllowsOpacityLayerToBeDroppedAtZeroOpacity()
    {
        using var tester = new FrameworkDartTester();
        var counter = new PaintCounter();

        tester.PumpWidget(OpacityTree(0.5, counter));
        Assert.Equal(1, counter.Count);

        tester.PumpWidget(OpacityTree(0.0, counter));
        Assert.Equal(1, counter.Count);
        Assert.DoesNotContain(AllLayers(tester), layer => layer is OpacityLayer);
    }

    // ---- widgets/animated_opacity_repaint_test.dart ----

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.99)]
    public void RenderAnimatedOpacityMixin_AvoidsRepaintingChildAsItAnimates(double end)
    {
        using var tester = new FrameworkDartTester();
        var counter = new PaintCounter();
        using var controller = new AnimationController(
            duration: TimeSpan.FromSeconds(1),
            vsync: new TestTickerProvider());
        tester.PumpWidget(FadeTree(controller.Drive(new DoubleTween(begin: 0.0, end: end)), counter));

        Assert.Equal(0, counter.Count);
        controller.Forward();

        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1, counter.Count);

        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1, counter.Count);

        controller.Stop();
        tester.Pump();
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public void RenderAnimatedOpacityMixin_AllowsOpacityLayerToBeDisposedWhenAnimatingToZero()
    {
        using var tester = new FrameworkDartTester();
        var counter = new PaintCounter();
        using var controller = new AnimationController(
            duration: TimeSpan.FromSeconds(1),
            vsync: new TestTickerProvider());
        tester.PumpWidget(FadeTree(controller.Drive(new DoubleTween(begin: 0.99, end: 0.0)), counter));

        Assert.Equal(1, counter.Count);
        Assert.Contains(AllLayers(tester), layer => layer is OpacityLayer);
        controller.Forward();

        tester.Pump();
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal(1, counter.Count);

        controller.Stop();
        tester.Pump();
        Assert.DoesNotContain(AllLayers(tester), layer => layer is OpacityLayer);
    }

    // ---- helpers ----

    private static void AssertIntrinsics(RenderBox box, double minWidth, double maxWidth, double minHeight,
        double maxHeight)
    {
        foreach (double probe in Probes)
        {
            Assert.Equal(minWidth, box.GetMinIntrinsicWidth(probe));
            Assert.Equal(maxWidth, box.GetMaxIntrinsicWidth(probe));
            Assert.Equal(minHeight, box.GetMinIntrinsicHeight(probe));
            Assert.Equal(maxHeight, box.GetMaxIntrinsicHeight(probe));
        }
    }

    /// <summary>Dart's <c>_testLayerReuse</c>: a layer is created on the first frame and reused on the
    /// second.</summary>
    private static void TestLayerReuse(RenderBox renderObject)
    {
        Assert.Null(renderObject.DebugLayer);
        var harness = new RenderingHarness(renderObject, BoxConstraints.Tight(new Size(10, 10)));
        harness.PumpFrame(paint: true);
        Layer? layer = renderObject.DebugLayer;
        Assert.IsType<OpacityLayer>(layer);

        // Mark for repaint otherwise pumpFrame is a noop.
        renderObject.MarkNeedsPaint();
        Assert.True(renderObject.DebugNeedsPaint);
        harness.PumpFrame(paint: true);
        Assert.False(renderObject.DebugNeedsPaint);
        Assert.Same(layer, renderObject.DebugLayer);
    }

    private static int CountSemanticsChildren(RenderObject renderObject)
    {
        int count = 0;
        renderObject.VisitChildrenForSemantics(_ => count += 1);
        return count;
    }

    private static List<FlutterErrorDetails> CollectErrors(Action action)
    {
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = errors.Add;
        try
        {
            action();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        return errors;
    }

    private static Size GetAspectRatioChildSize(BoxConstraints constraints, double aspectRatio)
    {
        using var tester = new FrameworkDartTester();
        var childKey = new UniqueKey();
        tester.PumpWidget(new Center(child: new ConstrainedBox(
            constraints: constraints,
            child: new AspectRatio(aspectRatio, new Container(key: childKey)))));
        return ((RenderBox)tester.ElementsWithKey(childKey).Single().RenderObject!).Size;
    }

    private static void AssertZeroArea<TWidget>(Widget widget) where TWidget : Widget
    {
        using var tester = new FrameworkDartTester();
        // Dart's `tester.view.physicalSize = Size.zero`: new metrics, then the binding's metrics hook.
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Center(child: widget)));

        var box = (RenderBox)tester.ElementOfType<TWidget>().RenderObject!;
        Assert.Equal(new Size(0, 0), box.Size);
    }

    private static Widget OpacityTree(double opacity, PaintCounter counter) =>
        new ColoredBox(new Color(0xFFFF0000), child: new Opacity(opacity, new TestWidget(counter)));

    private static Widget FadeTree(Animation<double> opacity, PaintCounter counter) =>
        new ColoredBox(new Color(0xFFFF0000), child: new FadeTransition(opacity, new TestWidget(counter)));

    private static List<Layer> AllLayers(FrameworkDartTester tester)
    {
        var layers = new List<Layer>();
        void Visit(Layer layer)
        {
            layers.Add(layer);
            if (layer is ContainerLayer container)
            {
                foreach (Layer child in container.Children)
                {
                    Visit(child);
                }
            }
        }

        if (tester.RenderView.DebugLayer is { } root)
        {
            Visit(root);
        }

        return layers;
    }

    /// <summary>A layout driver in the shape of Flutter's <c>rendering_tester.dart</c> <c>layout</c> and
    /// <c>pumpFrame</c>.</summary>
    private sealed class RenderingHarness
    {
        private readonly PipelineOwner _pipeline;

        public RenderingHarness(RenderBox box, BoxConstraints? constraints = null)
        {
            RenderBox root = box;
            if (constraints is BoxConstraints additional)
            {
                root = new RenderPositionedBox(
                    alignment: Alignment.TopLeft,
                    child: new RenderConstrainedBox(additional, box));
            }

            var view = new RenderView(new FlutterView(new Size(800, 600))) { Child = root };
            _pipeline = new PipelineOwner(view);
            _pipeline.Attach(view);
            PumpFrame();
        }

        public void PumpFrame(bool paint = false, bool semantics = false)
        {
            // No root size: the owner keeps `ViewConfiguration.FromView`, whose tight 800x600 constraints
            // match the rendering_tester's render view (`FlushLayout(Size)` would loosen them).
            _pipeline.FlushLayout();
            if (!paint)
            {
                return;
            }

            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            _pipeline.CompositeFrame();
            if (semantics)
            {
                _pipeline.FlushSemantics();
            }
        }
    }

    /// <summary>The <c>RenderTestBox</c> of intrinsic_width_test.dart.</summary>
    private sealed class RenderTestBox(BoxConstraints intrinsicDimensions) : RenderBox
    {
        protected override double ComputeMinIntrinsicWidth(double height) => intrinsicDimensions.MinWidth;

        protected override double ComputeMaxIntrinsicWidth(double height) => intrinsicDimensions.MaxWidth;

        protected override double ComputeMinIntrinsicHeight(double width) => intrinsicDimensions.MinHeight;

        protected override double ComputeMaxIntrinsicHeight(double width) => intrinsicDimensions.MaxHeight;

        protected override bool SizedByParent => true;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(new Size(
            intrinsicDimensions.MinWidth + ((intrinsicDimensions.MaxWidth - intrinsicDimensions.MinWidth) / 2.0),
            intrinsicDimensions.MinHeight + ((intrinsicDimensions.MaxHeight - intrinsicDimensions.MinHeight) / 2.0)));

        protected override void PerformResize()
        {
            Size = ComputeDryLayout(Constraints);
        }

        protected override void PerformLayout()
        {
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// <summary>The <c>RenderSizedBox</c> of rendering_tester.dart, counting intrinsic queries.</summary>
    private sealed class RenderSizedBox(Size size) : RenderBox
    {
        public int IntrinsicQueries { get; private set; }

        protected override double ComputeMinIntrinsicWidth(double height) => Query(size.Width);

        protected override double ComputeMaxIntrinsicWidth(double height) => Query(size.Width);

        protected override double ComputeMinIntrinsicHeight(double width) => Query(size.Height);

        protected override double ComputeMaxIntrinsicHeight(double width) => Query(size.Height);

        protected override bool SizedByParent => true;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(size);

        protected override void PerformResize()
        {
            Size = Constraints.Constrain(size);
        }

        protected override void PerformLayout()
        {
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
        }

        private double Query(double value)
        {
            IntrinsicQueries += 1;
            return value;
        }
    }

    /// <summary>The <c>_TestRepaintBoundary</c>/<c>_TestNonCompositedBox</c>/<c>_TestCompositedBox</c> of
    /// repaint_boundary_test.dart.</summary>
    private sealed class TestBox(bool isRepaintBoundary, bool setsOwnLayer) : RenderBox
    {
        public override bool IsRepaintBoundary => isRepaintBoundary;

        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext context, Point offset)
        {
            if (setsOwnLayer)
            {
                Layer = new OpacityLayer { Alpha = 50 };
            }
        }
    }

    private sealed class PaintCounter
    {
        public int Count { get; set; }
    }

    /// <summary>The <c>TestWidget</c>/<c>RenderTestObject</c> pair of the opacity repaint tests.</summary>
    private sealed class TestWidget(PaintCounter counter) : SingleChildRenderObjectWidget(null, null)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RenderTestObject(counter);
    }

    private sealed class RenderTestObject(PaintCounter counter) : RenderProxyBox
    {
        public override void Paint(PaintingContext context, Point offset)
        {
            counter.Count += 1;
            base.Paint(context, offset);
        }
    }

    private sealed class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(TickerCallback onTick) => new(onTick);
    }
}
