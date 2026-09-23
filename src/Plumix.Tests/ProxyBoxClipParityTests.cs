using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/test/rendering/proxy_box_test.dart
// flutter/packages/flutter/test/widgets/clip_test.dart
// flutter/packages/flutter/test/widgets/physical_model_test.dart
// flutter/packages/flutter/test/widgets/backdrop_filter_test.dart
// flutter/packages/flutter/test/widgets/basic_test.dart (PhysicalShape)
// flutter/packages/flutter/test/rendering/box_test.dart (should not have a 0 sized colored Box)

namespace Plumix.Tests;

// The widget tests drive the shared scheduler and renderer binding through FrameworkDartTester.
[Collection(SchedulerTestCollection.Name)]
public sealed class ProxyBoxClipParityTests
{
    private static readonly Color Black = Color.FromUInt32(0xFF000000);

    // ---- proxy_box_test.dart -------------------------------------------------------------------

    [Fact]
    public void RenderPhysicalModel_Compositing()
    {
        var root = new RenderPhysicalModel(color: Color.FromUInt32(0xffff00ff));
        PipelineOwner pipeline = Composite(root, new Size(800, 600));
        Assert.False(root.NeedsCompositing);

        root.Elevation = 1.0;
        Pump(pipeline);
        Assert.False(root.NeedsCompositing);

        root.Elevation = 0.0;
        Pump(pipeline);
        Assert.False(root.NeedsCompositing);
    }

    [DebugOnlyFact]
    public void RenderPhysicalShape_ShapeChangeTriggersRepaint()
    {
        var root = new RenderPhysicalShape(
            clipper: new ShapeBorderClipper(new CircleBorder()),
            color: Color.FromUInt32(0xffff00ff));
        Composite(root, new Size(800, 600));
        Assert.False(root.DebugNeedsPaint);

        // Same shape, no repaint.
        root.Clipper = new ShapeBorderClipper(new CircleBorder());
        Assert.False(root.DebugNeedsPaint);

        // Different shape triggers repaint.
        root.Clipper = new ShapeBorderClipper(new StadiumBorder());
        Assert.True(root.DebugNeedsPaint);
    }

    [Fact]
    public void RenderPhysicalShape_Compositing()
    {
        var root = new RenderPhysicalShape(
            clipper: new ShapeBorderClipper(new CircleBorder()),
            color: Color.FromUInt32(0xffff00ff));
        PipelineOwner pipeline = Composite(root, new Size(800, 600));
        Assert.False(root.NeedsCompositing);

        root.Elevation = 1.0;
        Pump(pipeline);
        Assert.False(root.NeedsCompositing);

        root.Elevation = 0.0;
        Pump(pipeline);
        Assert.False(root.NeedsCompositing);
    }

    [DebugOnlyFact]
    public void RenderShaderMask_ReusesItsLayer()
    {
        TestLayerReuse<ShaderMaskLayer>(new RenderShaderMask(
            shaderCallback: _ => Brushes.Black,
            child: new SizedRenderBox(new Size(1.0, 1.0))));
    }

    [DebugOnlyFact]
    public void RenderBackdropFilter_ReusesItsLayer()
    {
        TestLayerReuse<BackdropFilterLayer>(new RenderBackdropFilter(
            filter: new ImageFilter.Blur(),
            child: new SizedRenderBox(new Size(1.0, 1.0))));
    }

    [DebugOnlyFact]
    public void RenderClipRect_ReusesItsLayer()
    {
        TestLayerReuse<ClipRectLayer>(new RenderClipRect(
            clipper: new TestRectClipper(),
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [DebugOnlyFact]
    public void RenderClipRRect_ReusesItsLayer()
    {
        TestLayerReuse<ClipRRectLayer>(new RenderClipRRect(
            clipper: new TestRRectClipper(),
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [DebugOnlyFact]
    public void RenderClipOval_ReusesItsLayer()
    {
        TestLayerReuse<ClipPathLayer>(new RenderClipOval(
            clipper: new TestRectClipper(),
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [DebugOnlyFact]
    public void RenderClipPath_ReusesItsLayer()
    {
        TestLayerReuse<ClipPathLayer>(new RenderClipPath(
            clipper: new TestPathClipper(),
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [DebugOnlyFact]
    public void RenderPhysicalModel_ReusesItsLayer()
    {
        TestLayerReuse<ClipRRectLayer>(new RenderPhysicalModel(
            clipBehavior: Clip.HardEdge,
            color: Black,
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [DebugOnlyFact]
    public void RenderPhysicalShape_ReusesItsLayer()
    {
        TestLayerReuse<ClipPathLayer>(new RenderPhysicalShape(
            clipper: new TestPathClipper(),
            clipBehavior: Clip.HardEdge,
            color: Black,
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0)))));
    }

    [Fact]
    public void RenderCustomClipExtenders_RespectClipBehaviorWhenAskedToDescribeApproximateClip()
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 200, height: 200));
        var renderClipRect = new RenderClipRect(clipBehavior: Clip.None, child: child);
        LayoutRoot(renderClipRect, new Size(800, 600));
        var bounds = new Rect(new Point(0, 0), renderClipRect.Size);

        Assert.Null(renderClipRect.InvokeDescribeApproximatePaintClip(child));
        renderClipRect.ClipBehavior = Clip.HardEdge;
        Assert.Equal(bounds, renderClipRect.InvokeDescribeApproximatePaintClip(child));
        renderClipRect.ClipBehavior = Clip.AntiAlias;
        Assert.Equal(bounds, renderClipRect.InvokeDescribeApproximatePaintClip(child));
        renderClipRect.ClipBehavior = Clip.AntiAliasWithSaveLayer;
        Assert.Equal(bounds, renderClipRect.InvokeDescribeApproximatePaintClip(child));
    }

    // Plumix records Avalonia draw calls without Dart's op names (`#drawRect`, `#drawParagraph`), so
    // these count every draw: the outline plus the scissors paragraph.
    [DebugOnlyFact]
    public void RenderClipPath_DebugPaintSize_DrawsAPathAndADebugTextWhenClipBehaviorIsNotNone()
    {
        Assert.Equal(2, CountDebugPaintDraws(new RenderClipPath(clipBehavior: Clip.HardEdge, child: Box200())));
        Assert.Equal(0, CountDebugPaintDraws(new RenderClipPath(clipBehavior: Clip.None, child: Box200())));
    }

    [DebugOnlyFact]
    public void RenderClipRect_DebugPaintSize_DrawsARectAndADebugTextWhenClipBehaviorIsNotNone()
    {
        Assert.Equal(2, CountDebugPaintDraws(new RenderClipRect(clipBehavior: Clip.HardEdge, child: Box200())));
        Assert.Equal(0, CountDebugPaintDraws(new RenderClipRect(clipBehavior: Clip.None, child: Box200())));
    }

    [DebugOnlyFact]
    public void RenderClipRRect_DebugPaintSize_DrawsARoundedRectAndADebugTextWhenClipBehaviorIsNotNone()
    {
        Assert.Equal(2, CountDebugPaintDraws(new RenderClipRRect(clipBehavior: Clip.HardEdge, child: Box200())));
        Assert.Equal(0, CountDebugPaintDraws(new RenderClipRRect(clipBehavior: Clip.None, child: Box200())));
    }

    [DebugOnlyFact]
    public void RenderClipOval_DebugPaintSize_DrawsAPathAndADebugTextWhenClipBehaviorIsNotNone()
    {
        Assert.Equal(2, CountDebugPaintDraws(new RenderClipOval(clipBehavior: Clip.HardEdge, child: Box200())));
        Assert.Equal(0, CountDebugPaintDraws(new RenderClipOval(clipBehavior: Clip.None, child: Box200())));
    }

    [DebugOnlyFact]
    public void RenderClipRSuperellipse_DebugPaintSize_DrawsAndSkipsForClipNone()
    {
        Assert.Equal(
            2,
            CountDebugPaintDraws(new RenderClipRSuperellipse(clipBehavior: Clip.HardEdge, child: Box200())));
        Assert.Equal(
            0,
            CountDebugPaintDraws(new RenderClipRSuperellipse(clipBehavior: Clip.None, child: Box200())));
    }

    [DebugOnlyFact]
#pragma warning disable CS0618 // Dart's deprecated `filter` getter is what this test exercises.
    public void RenderBackdropFilter_HandlesMixUsesOfFilterAndFilterConfig()
    {
        ImageFilter filter1 = new ImageFilter.Blur();
        ImageFilter filter2 = new ImageFilter.Matrix(
            [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0]);
        ImageFilter filter3 = new ImageFilter.Compose(outer: filter1, inner: filter2);

        var backdropFilter = new RenderBackdropFilter(filter: filter1);

        Assert.Equal(filter1, backdropFilter.Filter);
        Assert.Equal(new ImageFilterConfig(filter1), backdropFilter.FilterConfig);

        backdropFilter.FilterConfig = new ImageFilterConfig(filter2);

        Assert.Equal(filter2, backdropFilter.Filter);
        Assert.Equal(new ImageFilterConfig(filter2), backdropFilter.FilterConfig);

        backdropFilter.Filter = filter3;

        Assert.Equal(filter3, backdropFilter.Filter);
        Assert.Equal(new ImageFilterConfig(filter3), backdropFilter.FilterConfig);

        var filterConfig1 = new ImageFilterConfig.Blur(sigmaX: 10.0, sigmaY: 10.0);
        backdropFilter.FilterConfig = filterConfig1;

        Assert.Equal(filterConfig1, backdropFilter.FilterConfig);
        Assert.Throws<AssertionError>(() => backdropFilter.Filter);
    }
#pragma warning restore CS0618

    [DebugOnlyFact]
    public void RenderBackdropFilter_AssertsExactlyOneOfFilterAndFilterConfig()
    {
        AssertionError neither = Assert.Throws<AssertionError>(() => new RenderBackdropFilter());
        Assert.Contains("Either filter or filterConfig must be provided.", neither.Message, StringComparison.Ordinal);

        AssertionError both = Assert.Throws<AssertionError>(() => new RenderBackdropFilter(
            filter: new ImageFilter.Blur(),
            filterConfig: new ImageFilterConfig(new ImageFilter.Blur())));
        Assert.Contains("Cannot provide both a filter and a filterConfig.", both.Message, StringComparison.Ordinal);
    }

    // ---- box_test.dart ---------------------------------------------------------------------------

    [DebugOnlyFact]
    public void RenderDecoratedBox_ShouldNotHaveAZeroSizedColoredBox()
    {
        var coloredBox = new RenderDecoratedBox(decoration: new BoxDecoration());

        Assert.Equal(
            "RenderDecoratedBox#00000 NEEDS-LAYOUT NEEDS-PAINT DETACHED\n"
            + "   parentData: MISSING\n"
            + "   constraints: MISSING\n"
            + "   size: MISSING\n"
            + "   decoration: BoxDecoration:\n"
            + "     <no decorations specified>\n"
            + "   configuration: ImageConfiguration()\n",
            FrameworkDartTester.IgnoringHashCodes(coloredBox.ToStringDeep(minLevel: DiagnosticLevel.Info)));

        var paddingBox = new RenderPadding(new Thickness(10.0), coloredBox);
        var root = new RenderDecoratedBox(decoration: new BoxDecoration(), child: paddingBox);
        LayoutRoot(root, new Size(800, 600));
        Assert.Equal(780.0, coloredBox.Size.Width);
        Assert.Equal(580.0, coloredBox.Size.Height);

        // Dart prints `parentData: offset=Offset(10.0, 10.0) (can use size)`, `constraints:
        // BoxConstraints(w=780.0, h=580.0)` and `size: Size(780.0, 580.0)`; those lines are formatted by
        // box.dart's RenderBox/BoxParentData, so only RenderDecoratedBox's own lines are compared here.
        string laidOut = FrameworkDartTester.IgnoringHashCodes(coloredBox.ToStringDeep(minLevel: DiagnosticLevel.Info));
        Assert.StartsWith("RenderDecoratedBox#00000 NEEDS-PAINT\n", laidOut, StringComparison.Ordinal);
        Assert.EndsWith(
            "   decoration: BoxDecoration:\n"
            + "     <no decorations specified>\n"
            + "   configuration: ImageConfiguration()\n",
            laidOut,
            StringComparison.Ordinal);
    }

    // ---- C#-side coverage of Dart behaviors that have no Flutter test --------------------------------

    [DebugOnlyFact]
    public void RenderDecoratedBox_MismatchedSaveCountThrowsDartsFlutterError()
    {
        var box = new RenderDecoratedBox(decoration: new UnbalancedDecoration());
        LayoutRoot(box, new Size(20, 20));

        FlutterError error = Assert.Throws<FlutterError>(
            () => box.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0)));
        string text = error.ToString();
        Assert.Contains("UnbalancedDecoration painter had mismatching save and restore calls.", text);
        Assert.Contains(
            "Before painting the decoration, the canvas save count was 1. After painting it, the canvas save "
            + "count was 2. Every call to save() or saveLayer() must be matched by a call to restore().",
            text.Replace("\n", " ", StringComparison.Ordinal));
        Assert.Contains("The decoration was", text);
        Assert.Contains("The painter was", text);
    }

    [DebugOnlyFact]
    public void RenderDecoratedBox_DetachMarksNeedsPaint()
    {
        var box = new RenderDecoratedBox(decoration: new BoxDecoration(Color: Black));
        var view = new RenderView(new FlutterView(new Size(20, 20))) { Child = box };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        Pump(pipeline);
        Assert.False(box.DebugNeedsPaint);

        view.Child = null;

        Assert.True(box.DebugNeedsPaint);
    }

    [Fact]
    public void RenderClipRect_HitTestUsesDartsHalfOpenRectContains()
    {
        var child = new SizedRenderBox(new Size(80, 80), hitSelf: true);
        var clip = new RenderClipRect(child: child, clipper: new ValueClipper<Rect>("a", new Rect(0, 0, 20, 20)));
        LayoutRoot(clip, new Size(80, 80));

        Assert.True(clip.HitTest(new BoxHitTestResult(), new Point(0, 0)));
        Assert.True(clip.HitTest(new BoxHitTestResult(), new Point(19.9, 19.9)));
        Assert.False(clip.HitTest(new BoxHitTestResult(), new Point(20, 10)));
        Assert.False(clip.HitTest(new BoxHitTestResult(), new Point(10, 20)));
    }

    [Fact]
    public void RenderClipRRect_HitTestUsesDartsRRectContains()
    {
        var child = new SizedRenderBox(new Size(80, 80), hitSelf: true);
        var clip = new RenderClipRRect(
            child: child,
            clipper: new ValueClipper<RRect>("r", RRect.FromRectAndRadius(new Rect(0, 0, 40, 40), 10.0)));
        LayoutRoot(clip, new Size(80, 80));

        Assert.True(clip.HitTest(new BoxHitTestResult(), new Point(20, 20)));
        Assert.False(clip.HitTest(new BoxHitTestResult(), new Point(1, 1)));
        Assert.False(clip.HitTest(new BoxHitTestResult(), new Point(40, 20)));
    }

    [Fact]
    public void RRect_Contains_MatchesDartUi()
    {
        RRect rrect = RRect.FromRectAndRadius(new Rect(10, 10, 100, 50), 20.0);

        Assert.True(rrect.Contains(new Point(10, 35)));
        Assert.True(rrect.Contains(new Point(60, 10)));
        Assert.False(rrect.Contains(new Point(110, 35)));
        Assert.False(rrect.Contains(new Point(60, 60)));
        Assert.False(rrect.Contains(new Point(11, 11)));
        Assert.True(rrect.Contains(new Point(30, 30)));

        // Radii that overflow the rect are scaled down first.
        RRect overflowing = RRect.FromRectAndRadius(new Rect(0, 0, 10, 10), 100.0);
        Assert.True(overflowing.Contains(new Point(5, 5)));
        Assert.False(overflowing.Contains(new Point(0.5, 0.5)));
    }

    [Fact]
    public void BorderRadiusGeometry_ResolveWithoutADirection_ReturnsThePhysicalRadius()
    {
        BorderRadiusGeometry radius = BorderRadius.Circular(4.0);
        Assert.Equal(BorderRadius.Circular(4.0), radius.Resolve(null));
    }

    [Fact]
    public void RenderCustomClip_ClipperSetterUsesEquality()
    {
        var root = new RenderClipRect(child: new SizedRenderBox(new Size(10, 10)), clipper: new EqualClipper(1));
        PipelineOwner pipeline = Composite(root, new Size(10, 10));
        Assert.False(root.DebugNeedsPaint);

        var equal = new EqualClipper(1);
        root.Clipper = equal;

        // Dart's `if (_clipper == newClipper) return;` keeps the old instance when the clippers are equal.
        Assert.NotSame(equal, root.Clipper);
        Assert.False(root.DebugNeedsPaint);
        _ = pipeline;
    }

    [Fact]
    public void RenderCustomClip_FallsBackToTheDefaultClipWhenTheClipperReturnsNull()
    {
        var child = new SizedRenderBox(new Size(40, 40), hitSelf: true);
        var clip = new RenderClipPath(child: child, clipper: new NullPathClipper());
        LayoutRoot(clip, new Size(40, 40));

        Assert.True(clip.HitTest(new BoxHitTestResult(), new Point(39, 39)));
    }

    [DebugOnlyFact]
    public void RenderShaderMaskAndPhysicalModel_StampTheirDebugCreatorOnTheLayer()
    {
        var mask = new RenderShaderMask(
            shaderCallback: _ => Brushes.Black,
            child: new SizedRenderBox(new Size(1.0, 1.0)))
        {
            DebugCreator = "mask",
        };
        Composite(mask, new Size(10, 10));
        Assert.Equal("mask", mask.DebugLayer!.DebugCreator);

        var physical = new RenderPhysicalModel(
            color: Black,
            clipBehavior: Clip.HardEdge,
            child: new RenderRepaintBoundary(child: new SizedRenderBox(new Size(1.0, 1.0))))
        {
            DebugCreator = "physical",
        };
        Composite(physical, new Size(10, 10));
        Assert.Equal("physical", physical.DebugLayer!.DebugCreator);
    }

    [Fact]
    public void CustomClipper_ToStringIsTheRuntimeType()
    {
        string expected = Constants.KDebugMode ? nameof(TestRectClipper) : "CustomClipper";
        Assert.Equal(expected, new TestRectClipper().ToString());
    }

    // ---- clip_test.dart ------------------------------------------------------------------------

    [Fact]
    public void ClipRect_UpdatesClipBehaviorInUpdateRenderObject()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ClipRect());
        var renderClip = RenderOf<RenderClipRect, ClipRect>(tester);
        Assert.Equal(Clip.HardEdge, renderClip.ClipBehavior);

        tester.PumpWidget(new ClipRect(clipBehavior: Clip.AntiAlias));
        Assert.Equal(Clip.AntiAlias, renderClip.ClipBehavior);

        tester.PumpWidget(new ClipRect(clipBehavior: Clip.None));
        Assert.Equal(Clip.None, renderClip.ClipBehavior);
    }

    [Fact]
    public void ClipRRect_UpdatesClipBehaviorInUpdateRenderObject()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ClipRRect());
        var renderClip = RenderOf<RenderClipRRect, ClipRRect>(tester);
        Assert.Equal(Clip.AntiAlias, renderClip.ClipBehavior);

        tester.PumpWidget(new ClipRRect(clipBehavior: Clip.HardEdge));
        Assert.Equal(Clip.HardEdge, renderClip.ClipBehavior);

        tester.PumpWidget(new ClipRRect(clipBehavior: Clip.None));
        Assert.Equal(Clip.None, renderClip.ClipBehavior);
    }

    [Fact]
    public void ClipPath_CallsGetClipOnceAndHitTestsThePath()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ClipPath(
            clipper: new PathClipper(log),
            child: new GestureDetector(behavior: HitTestBehavior.Opaque, onTap: () => log.Add("tap"))));
        Assert.Equal(["getClip"], log);

        TapAt(tester, new Point(10.0, 10.0));
        Assert.Equal(["getClip"], log);
        log.Clear();

        TapAt(tester, new Point(100.0, 100.0));
        Assert.Equal(["tap"], log);
        log.Clear();
    }

    [Fact]
    public void ClipOval_HitTestsTheOval()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ClipOval(
            child: new GestureDetector(behavior: HitTestBehavior.Opaque, onTap: () => log.Add("tap"))));
        Assert.Empty(log);

        TapAt(tester, new Point(10.0, 10.0));
        Assert.Empty(log);

        TapAt(tester, new Point(400.0, 300.0));
        Assert.Equal(["tap"], log);
    }

    [Fact]
    public void TransparentClipOval_HitTest()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Opacity(
            opacity: 0.0,
            child: new ClipOval(
                child: new GestureDetector(behavior: HitTestBehavior.Opaque, onTap: () => log.Add("tap")))));
        Assert.Empty(log);

        TapAt(tester, new Point(10.0, 10.0));
        Assert.Empty(log);

        TapAt(tester, new Point(400.0, 300.0));
        Assert.Equal(["tap"], log);
    }

    [Fact]
    public void ClipRect_ReclipsOnlyWhenTheSizeOrTheClipperChanges()
    {
        var log = new List<string>();
        using var tester = new FrameworkDartTester();
        Widget Build(double dimension, string message, Rect rect) => new Align(
            alignment: Alignment.TopLeft,
            child: SizedBox.Square(
                dimension: dimension,
                child: new ClipRect(
                    clipper: new ValueClipper<Rect>(message, rect, log),
                    child: new GestureDetector(behavior: HitTestBehavior.Opaque, onTap: () => log.Add("tap")))));

        tester.PumpWidget(Build(100.0, "a", new Rect(5.0, 5.0, 10.0, 10.0)));
        Assert.Equal(["a"], log);

        TapAt(tester, new Point(10.0, 10.0));
        Assert.Equal(["a", "tap"], log);

        TapAt(tester, new Point(100.0, 100.0));
        Assert.Equal(["a", "tap"], log);

        tester.PumpWidget(Build(100.0, "a", new Rect(5.0, 5.0, 10.0, 10.0)));
        Assert.Equal(["a", "tap"], log);

        tester.PumpWidget(Build(200.0, "a", new Rect(5.0, 5.0, 10.0, 10.0)));
        Assert.Equal(["a", "tap", "a"], log);

        tester.PumpWidget(Build(200.0, "a", new Rect(5.0, 5.0, 10.0, 10.0)));
        Assert.Equal(["a", "tap", "a"], log);

        tester.PumpWidget(Build(200.0, "b", new Rect(5.0, 5.0, 10.0, 10.0)));
        Assert.Equal(["a", "tap", "a", "b"], log);

        tester.PumpWidget(Build(200.0, "c", new Rect(25.0, 25.0, 10.0, 10.0)));
        Assert.Equal(["a", "tap", "a", "b", "c"], log);

        TapAt(tester, new Point(30.0, 30.0));
        Assert.Equal(["a", "tap", "a", "b", "c", "tap"], log);

        TapAt(tester, new Point(100.0, 100.0));
        Assert.Equal(["a", "tap", "a", "b", "c", "tap"], log);
    }

    [DebugOnlyFact]
    public void DebugPaintSizeEnabled_ClipRectPaintsItsOutlineAndTheScissors()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new ClipRect(child: new Placeholder()));
        var renderClip = RenderOf<RenderClipRect, ClipRect>(tester);

        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        renderClip.DebugPaintSize(context, new Point(0, 0));
        context.DebugStopRecordingIfNeeded();

        Assert.Equal(2, CountDraws(root));
    }

    [Fact]
    public void ClipRect_DoesNotCrashAtZeroArea()
    {
        using var clip = new ValueNotifier<Rect>(new Rect(50.0, 50.0, 100.0, 100.0));
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<ClipRect>(new ClipRect(clipper: new NotifyClipper<Rect>(clip))));
    }

    [Fact]
    public void ClipRRect_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<ClipRRect>(new ClipRRect(borderRadius: BorderRadius.Circular(8), child: new Placeholder())));
    }

    [Fact]
    public void ClipRSuperellipse_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<ClipRSuperellipse>(new ClipRSuperellipse(
                borderRadius: BorderRadius.Circular(8),
                child: new Placeholder())));
    }

    [Fact]
    public void ClipOval_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(new Size(0, 0), ZeroAreaSize<ClipOval>(new ClipOval(child: new Placeholder())));
    }

    [Fact]
    public void ClipPath_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(new Size(0, 0), ZeroAreaSize<ClipPath>(new ClipPath(child: new Placeholder())));
    }

    // ---- physical_model_test.dart ---------------------------------------------------------------

    [Fact]
    public void PhysicalModel_UpdatesClipBehaviorInUpdateRenderObject()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new PhysicalModel(color: Black));
        var renderPhysicalModel = RenderOf<RenderPhysicalModel, PhysicalModel>(tester);
        Assert.Equal(Clip.None, renderPhysicalModel.ClipBehavior);

        tester.PumpWidget(new PhysicalModel(clipBehavior: Clip.AntiAlias, color: Black));
        Assert.Equal(Clip.AntiAlias, renderPhysicalModel.ClipBehavior);
    }

    [Fact]
    public void PhysicalModel_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<PhysicalModel>(new PhysicalModel(color: Color.FromUInt32(0xAABBCC00))));
    }

    [Fact]
    public void PhysicalShape_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<PhysicalShape>(new PhysicalShape(
                color: Color.FromUInt32(0xAABBCC00),
                clipper: new ShapeBorderClipper(new CircleBorder()))));
    }

    // ---- basic_test.dart ------------------------------------------------------------------------

    [Fact]
    public void PhysicalShape_HitTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new PhysicalShape(
            clipper: new ShapeBorderClipper(new CircleBorder()),
            elevation: 2.0,
            color: Color.FromUInt32(0xFF0000FF),
            shadowColor: Color.FromUInt32(0xFF00FF00),
            child: new Container(color: Color.FromUInt32(0xFF0000FF))));
        var renderPhysicalShape = RenderOf<RenderPhysicalShape, PhysicalShape>(tester);

        // The viewport is 800x600, the CircleBorder is centered and fits the shortest edge, so we get a
        // circle of radius 300, centered at (400, 300). Sample around the left-most point (100, 300).
        Assert.False(Hits(tester, new Point(99.0, 300.0), renderPhysicalShape));
        Assert.True(Hits(tester, new Point(100.0, 300.0), renderPhysicalShape));
        Assert.False(Hits(tester, new Point(100.0, 299.0), renderPhysicalShape));
        Assert.False(Hits(tester, new Point(100.0, 301.0), renderPhysicalShape));
    }

    // ---- backdrop_filter_test.dart --------------------------------------------------------------

    [Fact]
    public void BackdropKey_IsPassedToTheBackdropLayer()
    {
        var backdropKey = new BackdropKey();
        Widget Build(bool enableKeys) => new Directionality(
            textDirection: TextDirection.Ltr,
            child: new ListView(children:
            [
                Item(new BackdropFilter(
                    filter: new ImageFilter.Blur(sigmaX: 40, sigmaY: 40),
                    backdropGroupKey: enableKeys ? backdropKey : null,
                    child: ItemBody())),
                Item(new BackdropFilter(
                    filter: new ImageFilter.Blur(sigmaX: 40, sigmaY: 40),
                    backdropGroupKey: enableKeys ? backdropKey : null,
                    child: ItemBody())),
            ]));

        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Build(enableKeys: true));
        List<BackdropFilterLayer> layers = Layers<BackdropFilterLayer>(tester.RenderView.DebugLayer);
        Assert.Equal(2, layers.Count);
        Assert.Same(backdropKey, layers[0].BackdropKey);
        Assert.Same(backdropKey, layers[1].BackdropKey);

        tester.PumpWidget(Build(enableKeys: false));
        layers = Layers<BackdropFilterLayer>(tester.RenderView.DebugLayer);
        Assert.Equal(2, layers.Count);
        Assert.Null(layers[0].BackdropKey);
        Assert.Null(layers[1].BackdropKey);
    }

    [Fact]
    public void BackdropKey_IsPassedToTheBackdropLayerViaBackdropGroup()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new BackdropGroup(
                child: new ListView(children:
                [
                    Item(BackdropFilter.Grouped(
                        filter: new ImageFilter.Blur(sigmaX: 40, sigmaY: 40),
                        child: ItemBody())),
                    Item(BackdropFilter.Grouped(
                        filter: new ImageFilter.Blur(sigmaX: 40, sigmaY: 40),
                        child: ItemBody())),
                ]))));

        List<BackdropFilterLayer> layers = Layers<BackdropFilterLayer>(tester.RenderView.DebugLayer);
        Assert.Equal(2, layers.Count);
        Assert.NotNull(layers[0].BackdropKey);
        Assert.Same(layers[0].BackdropKey, layers[1].BackdropKey);
    }

    [Fact]
    public void BackdropFilter_DoesNotCrashAtZeroArea()
    {
        Assert.Equal(
            new Size(0, 0),
            ZeroAreaSize<BackdropFilter>(new BackdropFilter(filter: new ImageFilter.Blur(sigmaX: 40, sigmaY: 40))));
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static Widget Item(Widget filter) => new ClipRect(child: filter);

    private static Widget ItemBody() => new Container(
        color: Color.FromUInt32(0x28000000),
        height: 200,
        child: new Text("Item 1"));

    /// <summary>Dart's `_testLayerReuse&lt;L&gt;`.</summary>
    private static void TestLayerReuse<TLayer>(RenderBox renderObject) where TLayer : Layer
    {
        Assert.NotEqual(typeof(Layer), typeof(TLayer));
        Assert.Null(renderObject.DebugLayer);
        PipelineOwner pipeline = Composite(renderObject, new Size(10, 10));
        Layer? layer = renderObject.DebugLayer;
        Assert.IsType<TLayer>(layer);
        Assert.NotNull(layer);

        // Mark for repaint otherwise pumpFrame is a noop.
        renderObject.MarkNeedsPaint();
        Assert.True(renderObject.DebugNeedsPaint);
        Pump(pipeline);
        Assert.False(renderObject.DebugNeedsPaint);
        Assert.Same(layer, renderObject.DebugLayer);
    }

    /// <summary>proxy_box_test.dart's `debugPaint`: paint, then `debugPaintSize`, into one recording.</summary>
    private static int CountDebugPaintDraws(RenderBox renderBox)
    {
        LayoutRoot(renderBox, new Size(800, 600));
        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        renderBox.Paint(context, new Point(0, 0));
        renderBox.DebugPaintSize(context, new Point(0, 0));
        context.DebugStopRecordingIfNeeded();
        return CountDraws(root);
    }

    private static int CountDraws(Layer layer)
    {
        int count = layer is PictureLayer picture ? picture.Picture?.DrawCommandCount ?? 0 : 0;
        if (layer is ContainerLayer container)
        {
            foreach (Layer child in container.Children)
            {
                count += CountDraws(child);
            }
        }

        return count;
    }

    private static List<TLayer> Layers<TLayer>(Layer? layer) where TLayer : Layer
    {
        var found = new List<TLayer>();
        void Visit(Layer? current)
        {
            if (current is TLayer match)
            {
                found.Add(match);
            }

            if (current is ContainerLayer container)
            {
                foreach (Layer child in container.Children)
                {
                    Visit(child);
                }
            }
        }

        Visit(layer);
        return found;
    }

    private static RenderBox Box200() => new RenderConstrainedBox(BoxConstraints.TightFor(width: 200, height: 200));

    private static TRender RenderOf<TRender, TWidget>(FrameworkDartTester tester)
        where TRender : RenderObject
        where TWidget : Widget
        => Assert.IsType<TRender>(tester.ElementOfType<TWidget>().FindRenderObject(), exactMatch: false);

    private static Size ZeroAreaSize<TWidget>(Widget widget) where TWidget : Widget
    {
        using var tester = new FrameworkDartTester();
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: widget)));
        return ((RenderBox)tester.ElementOfType<TWidget>().FindRenderObject()!).Size;
    }

    private static void TapAt(FrameworkDartTester tester, Point location)
    {
        int pointer = tester.StartGesture(location);
        tester.Up(pointer, location);
    }

    private static bool Hits(FrameworkDartTester tester, Point location, RenderObject target)
    {
        var result = new BoxHitTestResult();
        tester.RenderView.HitTest(result, location);
        return result.Path.Any(entry => ReferenceEquals(entry.Target, target));
    }

    /// <summary>The rendering test binding's `layout(box)`: tight constraints of the view's size.</summary>
    private static void LayoutRoot(RenderBox render, Size size)
    {
        var view = new RenderView(new FlutterView(size)) { Child = render };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout();
    }

    private static PipelineOwner Composite(RenderBox render, Size size)
    {
        var view = new RenderView(new FlutterView(size)) { Child = render };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        Pump(pipeline);
        return pipeline;
    }

    /// <summary>`pumpFrame(phase: EnginePhase.paint)` against the view's own tight size.</summary>
    private static void Pump(PipelineOwner pipeline)
    {
        pipeline.FlushLayout();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();
    }

    private sealed class SizedRenderBox(Size size, bool hitSelf = false) : RenderBox
    {
        protected override void PerformLayout() => Size = Constraints.Constrain(size);

        protected override bool HitTestSelf(Point position) => hitSelf;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class TestRectClipper : CustomClipper<Rect>
    {
        public override Rect GetClip(Size size) => default;

        public override Rect GetApproximateClipRect(Size size) => GetClip(size);

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper) => true;
    }

    private sealed class TestRRectClipper : CustomClipper<RRect>
    {
        public override RRect GetClip(Size size) => default;

        public override Rect GetApproximateClipRect(Size size) => GetClip(size).Rect;

        public override bool ShouldReclip(CustomClipper<RRect> oldClipper) => true;
    }

    private sealed class TestPathClipper : CustomClipper<Path>
    {
        public override Path GetClip(Size size)
        {
            var path = new Path();
            path.AddRect(new Rect(50.0, 50.0, 100.0, 100.0));
            return path;
        }

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) => false;
    }

    /// <summary>clip_test.dart's `PathClipper`.</summary>
    private sealed class PathClipper(List<string> log) : CustomClipper<Path>
    {
        public override Path GetClip(Size size)
        {
            log.Add("getClip");
            var path = new Path();
            path.AddRect(new Rect(50.0, 50.0, 100.0, 100.0));
            return path;
        }

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) => false;
    }

    /// <summary>clip_test.dart's `ValueClipper&lt;T&gt;`.</summary>
    private sealed class ValueClipper<T>(string message, T value, List<string>? log = null) : CustomClipper<T>
    {
        public string Message { get; } = message;

        public T Value { get; } = value;

        public override T GetClip(Size size)
        {
            log?.Add(Message);
            return Value;
        }

        public override bool ShouldReclip(CustomClipper<T> oldClipper)
        {
            var old = (ValueClipper<T>)oldClipper;
            return old.Message != Message || !EqualityComparer<T>.Default.Equals(old.Value, Value);
        }
    }

    /// <summary>clip_test.dart's `NotifyClipper&lt;T&gt;`.</summary>
    private sealed class NotifyClipper<T>(ValueNotifier<T> clip) : CustomClipper<T>(reclip: clip)
    {
        public override T GetClip(Size size) => clip.Value;

        public override bool ShouldReclip(CustomClipper<T> oldClipper) =>
            !ReferenceEquals(clip, ((NotifyClipper<T>)oldClipper).Clip);

        private ValueNotifier<T> Clip => clip;
    }

    private sealed class EqualClipper(int id) : CustomClipper<Rect>
    {
        public int Id { get; } = id;

        public override Rect GetClip(Size size) => new(0, 0, size.Width, size.Height);

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper) => true;

        public override bool Equals(object? obj) => obj is EqualClipper other && other.Id == Id;

        public override int GetHashCode() => Id;
    }

    private sealed class NullPathClipper : CustomClipper<Path>
    {
        public override Path GetClip(Size size) => null!;

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) => false;
    }

    private sealed record UnbalancedDecoration : Decoration
    {
        public override BoxPainter CreateBoxPainter(Action? onChanged = null) => new UnbalancedPainter();
    }

    private sealed class UnbalancedPainter : BoxPainter
    {
        public override void Paint(PaintingContext context, Point offset, ImageConfiguration configuration)
        {
            context.Canvas.Save();
        }
    }
}
