using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/debug.dart

namespace Plumix.Tests;

public sealed class RenderingDebugTests : IDisposable
{
    public void Dispose() => RenderingDebug.ResetForTesting();

    [Fact]
    public void EveryFlag_DefaultsToOff()
    {
        Assert.False(RenderingDebug.PaintSizeEnabled);
        Assert.False(RenderingDebug.PaintBaselinesEnabled);
        Assert.False(RenderingDebug.PaintTextLayoutBoxes);
        Assert.False(RenderingDebug.PaintLayerBordersEnabled);
        Assert.False(RenderingDebug.PaintPointersEnabled);
        Assert.False(RenderingDebug.RepaintRainbowEnabled);
        Assert.False(RenderingDebug.RepaintTextRainbowEnabled);
        Assert.False(RenderingDebug.PrintMarkNeedsLayoutStacks);
        Assert.False(RenderingDebug.PrintMarkNeedsPaintStacks);
        Assert.False(RenderingDebug.PrintLayouts);
        Assert.False(RenderingDebug.CheckIntrinsicSizes);
        Assert.False(RenderingDebug.ProfileLayoutsEnabled);
        Assert.False(RenderingDebug.ProfilePaintsEnabled);
        Assert.False(RenderingDebug.EnhanceLayoutTimelineArguments);
        Assert.False(RenderingDebug.EnhancePaintTimelineArguments);
        Assert.Null(RenderingDebug.OnProfilePaint);
        Assert.False(RenderingDebug.DisableClipLayers);
        Assert.False(RenderingDebug.DisablePhysicalShapeLayers);
        Assert.False(RenderingDebug.DisableOpacityLayers);
    }

    [Fact]
    public void CurrentRepaintColor_DefaultsToTheDartSentinel()
    {
        Assert.Equal(HSVColor.FromAHSV(0.4, 60.0, 1.0, 1.0), RenderingDebug.CurrentRepaintColor);
    }

    [Fact]
    public void AssertAllRenderVarsUnset_PassesWhenNothingWasChanged()
    {
        Assert.True(RenderingDebug.AssertAllRenderVarsUnset("should not throw"));
    }

    [Theory]
    [MemberData(nameof(FlagSetters))]
    public void AssertAllRenderVarsUnset_ThrowsForEveryTrackedVariable(string name, Action set)
    {
        Assert.NotNull(name);
        set();

        FlutterError error = Assert.Throws<FlutterError>(
            () => RenderingDebug.AssertAllRenderVarsUnset("a render var is set"));

        Assert.Contains("a render var is set", error.Message);
    }

    [Fact]
    public void AssertAllRenderVarsUnset_IgnoresTheTimelineArgumentFlags()
    {
        // Dart's debugAssertAllRenderVarsUnset does not list debugEnhance*TimelineArguments.
        RenderingDebug.EnhanceLayoutTimelineArguments = true;
        RenderingDebug.EnhancePaintTimelineArguments = true;

        Assert.True(RenderingDebug.AssertAllRenderVarsUnset("should not throw"));
    }

    [Fact]
    public void AssertAllRenderVarsUnset_HonorsTheCheckIntrinsicSizesOverride()
    {
        RenderingDebug.CheckIntrinsicSizes = true;

        Assert.True(RenderingDebug.AssertAllRenderVarsUnset("ok", checkIntrinsicSizesOverride: true));
        Assert.Throws<FlutterError>(() => RenderingDebug.AssertAllRenderVarsUnset("not ok"));
    }

    public static TheoryData<string, Action> FlagSetters() =>
        new()
        {
            { "PaintSizeEnabled", () => RenderingDebug.PaintSizeEnabled = true },
            { "PaintBaselinesEnabled", () => RenderingDebug.PaintBaselinesEnabled = true },
            { "PaintLayerBordersEnabled", () => RenderingDebug.PaintLayerBordersEnabled = true },
            { "PaintTextLayoutBoxes", () => RenderingDebug.PaintTextLayoutBoxes = true },
            { "PaintPointersEnabled", () => RenderingDebug.PaintPointersEnabled = true },
            { "RepaintRainbowEnabled", () => RenderingDebug.RepaintRainbowEnabled = true },
            { "RepaintTextRainbowEnabled", () => RenderingDebug.RepaintTextRainbowEnabled = true },
            {
                "CurrentRepaintColor",
                () => RenderingDebug.CurrentRepaintColor = RenderingDebug.CurrentRepaintColor.WithHue(62.0)
            },
            { "PrintMarkNeedsLayoutStacks", () => RenderingDebug.PrintMarkNeedsLayoutStacks = true },
            { "PrintMarkNeedsPaintStacks", () => RenderingDebug.PrintMarkNeedsPaintStacks = true },
            { "PrintLayouts", () => RenderingDebug.PrintLayouts = true },
            { "CheckIntrinsicSizes", () => RenderingDebug.CheckIntrinsicSizes = true },
            { "ProfileLayoutsEnabled", () => RenderingDebug.ProfileLayoutsEnabled = true },
            { "ProfilePaintsEnabled", () => RenderingDebug.ProfilePaintsEnabled = true },
            { "OnProfilePaint", () => RenderingDebug.OnProfilePaint = _ => { } },
            { "DisableClipLayers", () => RenderingDebug.DisableClipLayers = true },
            { "DisablePhysicalShapeLayers", () => RenderingDebug.DisablePhysicalShapeLayers = true },
            { "DisableOpacityLayers", () => RenderingDebug.DisableOpacityLayers = true },
        };

    [Fact]
    public void CheckHasBoundedAxis_PassesWhenBothAxesAreBounded()
    {
        var constraints = new BoxConstraints(MaxWidth: 100.0, MaxHeight: 100.0);

        Assert.True(RenderingDebug.CheckHasBoundedAxis(Axis.Vertical, constraints));
        Assert.True(RenderingDebug.CheckHasBoundedAxis(Axis.Horizontal, constraints));
    }

    [Fact]
    public void CheckHasBoundedAxis_Vertical_ReportsUnboundedHeightFirst()
    {
        var constraints = new BoxConstraints(
            MaxWidth: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity);

        FlutterError error = Assert.Throws<FlutterError>(
            () => RenderingDebug.CheckHasBoundedAxis(Axis.Vertical, constraints));

        Assert.Contains("Vertical viewport was given unbounded height.", error.Message);
        Assert.Contains("Column or Wrap instead", error.Message);
    }

    [Fact]
    public void CheckHasBoundedAxis_Vertical_ReportsUnboundedWidth()
    {
        var constraints = new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: 100.0);

        FlutterError error = Assert.Throws<FlutterError>(
            () => RenderingDebug.CheckHasBoundedAxis(Axis.Vertical, constraints));

        Assert.Contains("Vertical viewport was given unbounded width.", error.Message);
    }

    [Fact]
    public void CheckHasBoundedAxis_Horizontal_ReportsUnboundedWidthFirst()
    {
        var constraints = new BoxConstraints(
            MaxWidth: double.PositiveInfinity,
            MaxHeight: double.PositiveInfinity);

        FlutterError error = Assert.Throws<FlutterError>(
            () => RenderingDebug.CheckHasBoundedAxis(Axis.Horizontal, constraints));

        Assert.Contains("Horizontal viewport was given unbounded width.", error.Message);
        Assert.Contains("Row or Wrap instead", error.Message);
    }

    [Fact]
    public void CheckHasBoundedAxis_Horizontal_ReportsUnboundedHeight()
    {
        var constraints = new BoxConstraints(MaxWidth: 100.0, MaxHeight: double.PositiveInfinity);

        FlutterError error = Assert.Throws<FlutterError>(
            () => RenderingDebug.CheckHasBoundedAxis(Axis.Horizontal, constraints));

        Assert.Contains("Horizontal viewport was given unbounded height.", error.Message);
    }

    [Fact]
    public void PaintPadding_WithoutInnerRect_FillsTheWholeOuterRectAsSpacing()
    {
        var probe = new PaintCallbackRenderBox(
            new Size(60.0, 40.0),
            (context, offset) => RenderingDebug.PaintPadding(context, new Rect(offset, new Size(60.0, 40.0)), null));

        OffsetLayer layer = Paint(probe, new Size(60.0, 40.0));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void PaintPadding_WithInnerRect_BuildsAnEvenOddRingOverTheOuterRect()
    {
        var outer = new Rect(0.0, 0.0, 60.0, 40.0);
        var inner = new Rect(10.0, 10.0, 40.0, 20.0);

        Plumix.UI.Path ring = RenderingDebug.BuildDoubleRectPath(outer, inner);

        Assert.Equal(PathFillType.EvenOdd, ring.FillType);
        Assert.Equal(outer, ring.GetBounds());
    }

    [Fact]
    public void PaintPadding_WithEmptyInnerRect_FallsBackToTheSpacingFill()
    {
        var probe = new PaintCallbackRenderBox(
            new Size(60.0, 40.0),
            (context, offset) => RenderingDebug.PaintPadding(
                context,
                new Rect(offset, new Size(60.0, 40.0)),
                default(Rect)));

        OffsetLayer layer = Paint(probe, new Size(60.0, 40.0));

        // The spacing branch draws a plain rectangle, so the picture layer exists and is non-empty.
        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void DebugPaint_IsNotCalledWhenEveryPaintFlagIsOff()
    {
        var probe = new DebugPaintProbeRenderBox();

        Paint(probe, new Size(40.0, 40.0));

        Assert.Equal(0, probe.DebugPaintSizeCount);
        Assert.Equal(0, probe.DebugPaintBaselinesCount);
        Assert.Equal(0, probe.DebugPaintPointersCount);
    }

    [Fact]
    public void PaintSizeEnabled_RoutesEveryRenderBoxThroughDebugPaintSize()
    {
        RenderingDebug.PaintSizeEnabled = true;
        var probe = new DebugPaintProbeRenderBox();

        Paint(probe, new Size(40.0, 40.0));

        Assert.Equal(1, probe.DebugPaintSizeCount);
        Assert.Equal(0, probe.DebugPaintBaselinesCount);
    }

    [Fact]
    public void PaintBaselinesEnabled_RoutesEveryRenderBoxThroughDebugPaintBaselines()
    {
        RenderingDebug.PaintBaselinesEnabled = true;
        var probe = new DebugPaintProbeRenderBox();

        Paint(probe, new Size(40.0, 40.0));

        Assert.Equal(0, probe.DebugPaintSizeCount);
        Assert.Equal(1, probe.DebugPaintBaselinesCount);
    }

    [Fact]
    public void PaintPointersEnabled_RoutesEveryRenderBoxThroughDebugPaintPointers()
    {
        RenderingDebug.PaintPointersEnabled = true;
        var probe = new DebugPaintProbeRenderBox();

        Paint(probe, new Size(40.0, 40.0));

        Assert.Equal(1, probe.DebugPaintPointersCount);
    }

    [Fact]
    public void DebugHandleEvent_CountsPointersAndRepaintsOnlyWhilePaintPointersIsEnabled()
    {
        var probe = new DebugPaintProbeRenderBox();
        Paint(probe, new Size(40.0, 40.0));
        var entry = new BoxHitTestEntry(probe, new Point(1.0, 1.0));

        probe.DebugHandleEvent(PointerDownAt(1), entry);
        Assert.False(probe.DebugNeedsPaint);

        RenderingDebug.PaintPointersEnabled = true;
        probe.DebugHandleEvent(PointerDownAt(2), entry);

        Assert.True(probe.DebugNeedsPaint);
    }

    [Fact]
    public void DebugPaintPointers_PaintsOnlyWhileAPointerIsDown()
    {
        RenderingDebug.PaintPointersEnabled = true;
        var probe = new DebugPaintProbeRenderBox();
        var renderView = new RenderView { Child = probe };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        Repaint(pipeline, new Size(40.0, 40.0));

        Assert.False(probe.LastPaintPointersDrew);

        var entry = new BoxHitTestEntry(probe, new Point(1.0, 1.0));
        probe.DebugHandleEvent(PointerDownAt(1), entry);
        Repaint(pipeline, new Size(40.0, 40.0));
        Assert.True(probe.LastPaintPointersDrew);

        probe.DebugHandleEvent(PointerUpAt(1), entry);
        Repaint(pipeline, new Size(40.0, 40.0));
        Assert.False(probe.LastPaintPointersDrew);
    }

    [Fact]
    public void DebugHandleEvent_TreatsACancelLikeAnUp()
    {
        RenderingDebug.PaintPointersEnabled = true;
        var probe = new DebugPaintProbeRenderBox();
        var entry = new BoxHitTestEntry(probe, new Point(1.0, 1.0));

        probe.DebugHandleEvent(PointerDownAt(1), entry);
        probe.DebugHandleEvent(PointerCancelAt(1), entry);
        Paint(probe, new Size(40.0, 40.0));

        Assert.Equal(1, probe.DebugPaintPointersCount);
        Assert.False(probe.LastPaintPointersDrew);
    }

    [Fact]
    public void OnProfilePaint_IsInvokedForEveryPaintedChild()
    {
        var painted = new List<RenderObject>();
        RenderingDebug.OnProfilePaint = renderObject => painted.Add(renderObject);
        var leaf = new DebugPaintProbeRenderBox();
        var padding = new RenderPadding(new Thickness(4.0), leaf);

        Paint(padding, new Size(40.0, 40.0));

        Assert.Contains(padding, painted);
        Assert.Contains(leaf, painted);
    }

    [Fact]
    public void PrintLayouts_LogsEveryLaidOutRenderObject()
    {
        List<string> messages = CaptureDebugPrint(() =>
        {
            RenderingDebug.PrintLayouts = true;
            Paint(new DebugPaintProbeRenderBox(), new Size(40.0, 40.0));
        });

        Assert.Contains(messages, message => message.StartsWith("Laying out (", StringComparison.Ordinal));
    }

    [Fact]
    public void PrintMarkNeedsLayoutStacks_LogsTheStackOfTheDirtyingCall()
    {
        var probe = new DebugPaintProbeRenderBox();
        OffsetLayer layer = Paint(probe, new Size(40.0, 40.0));
        Assert.NotNull(layer);

        List<string> messages = CaptureDebugPrint(() =>
        {
            RenderingDebug.PrintMarkNeedsLayoutStacks = true;
            probe.MarkNeedsLayout();
        });

        Assert.Contains(messages, message => message.Contains("MarkNeedsLayout() called for", StringComparison.Ordinal));
    }

    [Fact]
    public void PrintMarkNeedsPaintStacks_LogsTheStackOfTheDirtyingCall()
    {
        var probe = new DebugPaintProbeRenderBox();
        Paint(probe, new Size(40.0, 40.0));

        List<string> messages = CaptureDebugPrint(() =>
        {
            RenderingDebug.PrintMarkNeedsPaintStacks = true;
            probe.MarkNeedsPaint();
        });

        Assert.Contains(messages, message => message.Contains("MarkNeedsPaint() called for", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckIntrinsicSizes_CatchesAnIntrinsicProtocolViolation()
    {
        RenderingDebug.CheckIntrinsicSizes = true;

        List<FlutterErrorDetails> errors = CaptureRenderErrors(
            () => Paint(new BadIntrinsicsRenderBox(), new Size(40.0, 40.0)));

        FlutterErrorDetails failure = Assert.Single(errors);
        Assert.Contains("violate the intrinsic protocol contract", ((Exception)failure.Exception!).Message);
        Assert.Contains("returned a negative value", ((Exception)failure.Exception!).Message);
    }

    [Fact]
    public void CheckIntrinsicSizes_CatchesADryLayoutThatDisagreesWithPerformLayout()
    {
        RenderingDebug.CheckIntrinsicSizes = true;

        List<FlutterErrorDetails> errors = CaptureRenderErrors(
            () => Paint(new InconsistentDryLayoutRenderBox(), new Size(40.0, 40.0)));

        // The view's own dry layout defers to the child's, so the mismatch is reported twice.
        Assert.Contains(
            errors,
            failure => ((Exception)failure.Exception!).Message.Contains(
                "The size given to the InconsistentDryLayoutRenderBox class differs from the size computed",
                StringComparison.Ordinal));
    }

    [Fact]
    public void CheckIntrinsicSizes_AcceptsAConsistentRenderBox()
    {
        RenderingDebug.CheckIntrinsicSizes = true;

        List<FlutterErrorDetails> errors = CaptureRenderErrors(
            () => Paint(new DebugPaintProbeRenderBox(), new Size(40.0, 40.0)));

        Assert.Empty(errors);
    }

    [Fact]
    public void CheckIntrinsicSizes_CatchesADryBaselineThatDisagreesWithTheRealOne()
    {
        RenderingDebug.CheckIntrinsicSizes = true;

        List<FlutterErrorDetails> errors = CaptureRenderErrors(
            () => Paint(new InconsistentBaselineRenderBox(), new Size(40.0, 40.0)));

        Assert.Contains(
            errors,
            failure => ((Exception)failure.Exception!).Message.Contains(
                "differs from the baseline location computed by ComputeDryBaseline",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RepaintBoundaryMetrics_CountASharedPaintAsSymmetric()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());

        Paint(boundary, new Size(40.0, 40.0));

        Assert.Equal(1, boundary.DebugSymmetricPaintCount);
        Assert.Equal(0, boundary.DebugAsymmetricPaintCount);
    }

    [Fact]
    public void RepaintBoundaryMetrics_CountAStandaloneRepaintAsAsymmetric()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());
        var renderView = new RenderView { Child = boundary };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(40.0, 40.0));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Equal(1, boundary.DebugSymmetricPaintCount);

        boundary.MarkNeedsPaint();
        pipeline.FlushPaint();

        Assert.Equal(1, boundary.DebugAsymmetricPaintCount);
    }

    [Fact]
    public void RepaintBoundary_DebugResetMetrics_ZeroesBothCounts()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());
        Paint(boundary, new Size(40.0, 40.0));

        boundary.DebugResetMetrics();

        Assert.Equal(0, boundary.DebugSymmetricPaintCount);
        Assert.Equal(0, boundary.DebugAsymmetricPaintCount);
    }

    [Fact]
    public void RepaintBoundary_DebugFillProperties_ReportsNoMetricsBeforeTheFirstPaint()
    {
        var boundary = new RenderRepaintBoundary();
        var properties = new DiagnosticPropertiesBuilder();

        boundary.DebugFillProperties(properties);

        Assert.Contains(
            properties.Properties,
            property => property.Name == "usefulness ratio");
    }

    [Fact]
    public void RepaintBoundary_DebugFillProperties_DiagnosesTooLittleData()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());
        Paint(boundary, new Size(40.0, 40.0));
        var properties = new DiagnosticPropertiesBuilder();

        boundary.DebugFillProperties(properties);

        DiagnosticsNode diagnosis = Assert.Single(
            properties.Properties,
            property => property.Name == "diagnosis");
        Assert.Contains("insufficient data", diagnosis.ToDescription());
        Assert.Contains(properties.Properties, property => property.Name == "metrics");
    }

    [Fact]
    public void RepaintBoundary_DebugFillProperties_DiagnosesAnIneffectualBoundary()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());
        for (int index = 0; index < 6; index += 1)
        {
            boundary.DebugRegisterRepaintBoundaryPaint(includedParent: true, includedChild: true);
        }

        var properties = new DiagnosticPropertiesBuilder();
        boundary.DebugFillProperties(properties);

        DiagnosticsNode diagnosis = Assert.Single(
            properties.Properties,
            property => property.Name == "diagnosis");
        Assert.Contains("astoundingly ineffectual", diagnosis.ToDescription());
    }

    [Fact]
    public void RepaintBoundary_DebugFillProperties_DiagnosesAnOutstandingBoundary()
    {
        var boundary = new RenderRepaintBoundary(new DebugPaintProbeRenderBox());
        for (int index = 0; index < 10; index += 1)
        {
            boundary.DebugRegisterRepaintBoundaryPaint(includedParent: true, includedChild: false);
        }

        var properties = new DiagnosticPropertiesBuilder();
        boundary.DebugFillProperties(properties);

        DiagnosticsNode diagnosis = Assert.Single(
            properties.Properties,
            property => property.Name == "diagnosis");
        Assert.Contains("outstandingly useful", diagnosis.ToDescription());
    }

    [Fact]
    public void RepaintRainbowEnabled_AdvancesTheRepaintColorByTwoDegreesPerFrame()
    {
        RenderingDebug.RepaintRainbowEnabled = true;

        RenderingDebug.AdvanceRepaintColorForFrame();

        Assert.Equal(62.0, RenderingDebug.CurrentRepaintColor.Hue);
    }

    [Fact]
    public void RepaintRainbow_WrapsTheHueBackToZeroAfter360Degrees()
    {
        RenderingDebug.RepaintRainbowEnabled = true;
        RenderingDebug.CurrentRepaintColor = RenderingDebug.CurrentRepaintColor.WithHue(359.0);

        RenderingDebug.AdvanceRepaintColorForFrame();

        Assert.Equal(1.0, RenderingDebug.CurrentRepaintColor.Hue);
    }

    [Fact]
    public void CompositeFrame_LeavesTheRepaintColorAloneWhileTheRainbowsAreOff()
    {
        RenderingDebug.AdvanceRepaintColorForFrame();

        Assert.Equal(60.0, RenderingDebug.CurrentRepaintColor.Hue);
    }

    [Fact]
    public void RepaintTextRainbowEnabled_AlsoAdvancesTheRepaintColor()
    {
        RenderingDebug.RepaintTextRainbowEnabled = true;

        RenderingDebug.AdvanceRepaintColorForFrame();

        Assert.Equal(62.0, RenderingDebug.CurrentRepaintColor.Hue);
    }

    [Fact]
    public void PaintLayerBordersEnabled_DrawsIntoAnOtherwiseEmptyPictureLayer()
    {
        var emptyProbe = new PaintCallbackRenderBox(new Size(40.0, 40.0), (_, _) => { });
        Assert.Empty(Paint(emptyProbe, new Size(40.0, 40.0)).Children);

        RenderingDebug.PaintLayerBordersEnabled = true;
        var probe = new PaintCallbackRenderBox(
            new Size(40.0, 40.0),
            (context, offset) => context.DrawRectangle(Brushes.Red, null, new Rect(offset, new Size(4.0, 4.0))));

        OffsetLayer layer = Paint(probe, new Size(40.0, 40.0));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void PaintSizeEnabled_MakesRenderPaddingDrawItsConstructionLines()
    {
        RenderingDebug.PaintSizeEnabled = true;
        var padding = new RenderPadding(new Thickness(6.0), new DebugPaintProbeRenderBox());

        OffsetLayer layer = Paint(padding, new Size(60.0, 60.0));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void PaintSizeEnabled_MakesAnEmptyRenderConstrainedBoxDrawSpacing()
    {
        RenderingDebug.PaintSizeEnabled = true;
        var constrained = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20.0, 20.0)));

        OffsetLayer layer = Paint(constrained, new Size(40.0, 40.0));

        var picture = Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
        Assert.False(picture.IsEmpty);
    }

    [Fact]
    public void PaintZigZag_RejectsANonPositiveZigCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PaintUtilities.BuildZigZagPath(
                new Point(0.0, 0.0),
                new Point(10.0, 0.0),
                zigs: 0,
                width: 4.0));
    }

    [Fact]
    public void PaintZigZag_CrossesTheDirectLineZigsMinusOneTimes()
    {
        Plumix.UI.Path path = PaintUtilities.BuildZigZagPath(
            new Point(0.0, 0.0),
            new Point(20.0, 0.0),
            zigs: 4,
            width: 3.0);

        Rect bounds = path.GetBounds();
        Assert.Equal(0.0, bounds.X, 6);
        Assert.Equal(20.0, bounds.Right, 6);
        Assert.Equal(-3.0, bounds.Y, 6);
        Assert.Equal(3.0, bounds.Bottom, 6);
    }

    [Fact]
    public void PaintZigZag_RotatesTheZigZagOntoTheStartToEndLine()
    {
        Plumix.UI.Path path = PaintUtilities.BuildZigZagPath(
            new Point(5.0, 5.0),
            new Point(5.0, 25.0),
            zigs: 2,
            width: 4.0);

        Rect bounds = path.GetBounds();
        Assert.Equal(5.0, bounds.Y, 6);
        Assert.Equal(25.0, bounds.Bottom, 6);
        Assert.Equal(1.0, bounds.X, 6);
        Assert.Equal(9.0, bounds.Right, 6);
    }

    private static PointerDownEvent PointerDownAt(int pointer) => new(
        pointer,
        PointerDeviceKind.Touch,
        new Point(1.0, 1.0),
        PointerButtons.Primary,
        DateTime.UnixEpoch);

    private static PointerUpEvent PointerUpAt(int pointer) => new(
        pointer,
        PointerDeviceKind.Touch,
        new Point(1.0, 1.0),
        PointerButtons.None,
        DateTime.UnixEpoch);

    private static PointerCancelEvent PointerCancelAt(int pointer) => new(
        pointer,
        PointerDeviceKind.Touch,
        new Point(1.0, 1.0),
        PointerButtons.None,
        DateTime.UnixEpoch);

    private static List<FlutterErrorDetails> CaptureRenderErrors(Action body)
    {
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => errors.Add(details);
        try
        {
            body();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        return errors;
    }

    private static List<string> CaptureDebugPrint(Action body)
    {
        var messages = new List<string>();
        DebugPrintCallback previous = Print.DebugPrint;
        Print.DebugPrint = (message, _) => messages.Add(message ?? string.Empty);
        try
        {
            body();
        }
        finally
        {
            Print.DebugPrint = previous;
        }

        return messages;
    }

    private static void Repaint(PipelineOwner pipeline, Size size)
    {
        pipeline.FlushLayout(size);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
    }

    private static OffsetLayer Paint(RenderBox root, Size size)
    {
        var renderView = new RenderView { Child = root };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(size);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        return pipeline.RootLayer;
    }

    private sealed class PaintCallbackRenderBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly Action<PaintingContext, Point> _paint;

        public PaintCallbackRenderBox(Size desiredSize, Action<PaintingContext, Point> paint)
        {
            _desiredSize = desiredSize;
            _paint = paint;
        }

        protected override void PerformLayout() => Size = Constraints.Constrain(_desiredSize);

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        public override void Paint(PaintingContext ctx, Point offset) => _paint(ctx, offset);
    }

    private sealed class DebugPaintProbeRenderBox : RenderBox
    {
        public int DebugPaintSizeCount { get; private set; }

        public int DebugPaintBaselinesCount { get; private set; }

        public int DebugPaintPointersCount { get; private set; }

        public bool LastPaintPointersDrew { get; private set; }

        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(32.0, 32.0));

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(new Size(32.0, 32.0));

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected internal override void DebugPaintSize(PaintingContext context, Point offset)
        {
            DebugPaintSizeCount += 1;
            base.DebugPaintSize(context, offset);
        }

        protected override void DebugPaintBaselines(PaintingContext context, Point offset)
        {
            DebugPaintBaselinesCount += 1;
            base.DebugPaintBaselines(context, offset);
        }

        protected override void DebugPaintPointers(PaintingContext context, Point offset)
        {
            DebugPaintPointersCount += 1;
            var container = new ContainerLayer();
            base.DebugPaintPointers(new PaintingContext(container, context.EstimatedBounds), offset);
            LastPaintPointersDrew = container.Children.Count > 0;
        }
    }

    private sealed class BadIntrinsicsRenderBox : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(20.0, 20.0));

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(new Size(20.0, 20.0));

        protected override double ComputeMinIntrinsicWidth(double height) => -1.0;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class InconsistentDryLayoutRenderBox : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(20.0, 20.0));

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(new Size(10.0, 10.0));

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class InconsistentBaselineRenderBox : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(20.0, 20.0));

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(new Size(20.0, 20.0));

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => 4.0;

        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => 9.0;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }
}
