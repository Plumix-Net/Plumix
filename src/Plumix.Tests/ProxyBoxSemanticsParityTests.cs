using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Transform = Plumix.Widgets.Transform;

// Dart parity source: flutter/packages/flutter/test/rendering/proxy_box_test.dart,
// flutter/packages/flutter/test/rendering/offstage_test.dart,
// flutter/packages/flutter/test/rendering/layers_test.dart,
// flutter/packages/flutter/test/rendering/repaint_boundary_test.dart,
// flutter/packages/flutter/test/widgets/repaint_boundary_2_test.dart,
// flutter/packages/flutter/test/widgets/absorb_pointer_test.dart,
// flutter/packages/flutter/test/widgets/annotated_region_test.dart,
// flutter/packages/flutter/test/widgets/composited_transform_test.dart

namespace Plumix.Tests;

public sealed class ProxyBoxSemanticsParityTests
{
    private static readonly Alignment[] Alignments =
    [
        Alignment.TopLeft,
        Alignment.TopRight,
        Alignment.Center,
        Alignment.BottomLeft,
        Alignment.BottomRight,
    ];

    // proxy_box_test.dart

    [Fact]
    public void RenderSemanticsGestureHandler_AddsAndRemovesCorrectSemanticActions()
    {
        Action onTap = () => { };
        var renderObj = new RenderSemanticsGestureHandler(
            onTap: onTap,
            onHorizontalDragUpdate: _ => { });

        var config = new SemanticsConfiguration();
        renderObj.InvokeDescribeSemanticsConfiguration(config);
        Assert.True(config.ActionHandlers.ContainsKey(SemanticsActions.Tap));
        Assert.True(config.ActionHandlers.ContainsKey(SemanticsActions.ScrollLeft));
        Assert.True(config.ActionHandlers.ContainsKey(SemanticsActions.ScrollRight));
        Assert.Same(onTap, config.OnTap);

        config = new SemanticsConfiguration();
        renderObj.ValidActions = SemanticsActions.Tap | SemanticsActions.ScrollLeft;

        renderObj.InvokeDescribeSemanticsConfiguration(config);
        Assert.True(config.ActionHandlers.ContainsKey(SemanticsActions.Tap));
        Assert.True(config.ActionHandlers.ContainsKey(SemanticsActions.ScrollLeft));
        Assert.False(config.ActionHandlers.ContainsKey(SemanticsActions.ScrollRight));
    }

    [Fact]
    public void RenderFollowerLayer_HitTestWithoutALeaderLayerAndShowWhenUnlinkedTrue()
    {
        var follower = new RenderFollowerLayer(new LayerLink(), child: new RenderSizedBox(new Size(1.0, 1.0)));
        Layout(follower, new Size(200.0, 200.0));
        var hitTestResult = new BoxHitTestResult();
        Assert.True(follower.HitTest(hitTestResult, default));
    }

    [Fact]
    public void RenderFollowerLayer_HitTestWithoutALeaderLayerAndShowWhenUnlinkedFalse()
    {
        var follower = new RenderFollowerLayer(
            new LayerLink(),
            showWhenUnlinked: false,
            child: new RenderSizedBox(new Size(1.0, 1.0)));
        Layout(follower, new Size(200.0, 200.0));
        var hitTestResult = new BoxHitTestResult();
        Assert.False(follower.HitTest(hitTestResult, default));
    }

    [Fact]
    public void RenderFollowerLayer_HitTestWithALeaderLayerAndShowWhenUnlinkedTrue()
    {
        // Creates a layer link with a leader.
        var link = new LayerLink();
        var leader = new LeaderLayer(link);
        leader.Attach(new object());

        var follower = new RenderFollowerLayer(link, child: new RenderSizedBox(new Size(1.0, 1.0)));
        LayoutOnly(follower, new Size(200.0, 200.0));
        var hitTestResult = new BoxHitTestResult();
        Assert.True(follower.HitTest(hitTestResult, default));
    }

    [Fact]
    public void RenderFollowerLayer_HitTestWithALeaderLayerAndShowWhenUnlinkedFalse()
    {
        // Creates a layer link with a leader.
        var link = new LayerLink();
        var leader = new LeaderLayer(link);
        leader.Attach(new object());

        var follower = new RenderFollowerLayer(
            link,
            showWhenUnlinked: false,
            child: new RenderSizedBox(new Size(1.0, 1.0)));
        LayoutOnly(follower, new Size(200.0, 200.0));
        var hitTestResult = new BoxHitTestResult();
        // The follower is still hit testable because there is a leader layer.
        Assert.True(follower.HitTest(hitTestResult, default));
    }

    [Fact]
    public void Offstage_ImplementsPaintsChildCorrectly()
    {
        var box = new RenderConstrainedBox(BoxConstraints.TightFor(width: 20));
        var parent = new RenderConstrainedBox(BoxConstraints.TightFor(width: 20));
        var offstage = new RenderOffstage(offstage: false, child: box);
        parent.Child = offstage;

        Assert.True(offstage.PaintsChild(box));

        offstage.Offstage = true;

        Assert.False(offstage.PaintsChild(box));
    }

    [Fact]
    public void LayerLink_ToString_DescribesWhetherItIsLinked()
    {
        var link = new LayerLink();
        Assert.EndsWith("(<dangling>)", link.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("LayerLink#", link.ToString(), StringComparison.Ordinal);

        new LeaderLayer(link).Attach(new object());
        Assert.EndsWith("(<linked>)", link.ToString(), StringComparison.Ordinal);
    }

    // offstage_test.dart

    [Fact]
    public void Offstage_LaysOutButDoesNotPaintItsChild()
    {
        bool painted = false;
        // incoming constraints are tight 800x600
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 10.0, height: 10.0));
        var root = new RenderPositionedBox(
            child: new RenderConstrainedBox(
                BoxConstraints.TightFor(width: 800.0),
                child: new RenderOffstage(
                    child: new RenderCustomPaint(
                        painter: new CallbackPainter(() => painted = true),
                        child: child))));
        Assert.False(child.HasSize);
        Assert.False(painted);
        Layout(root, new Size(800.0, 600.0));
        Assert.True(child.HasSize);
        Assert.False(painted);
        Assert.Equal(new Size(800.0, 10.0), child.Size);
    }

    // layers_test.dart

    [Fact]
    public void NonPaintedLayersAreDetached()
    {
        RenderObject boundary;
        RenderObject inner;
        var root = new RenderOpacity(
            child: (RenderBox)(boundary = new RenderRepaintBoundary(
                child: (RenderBox)(inner = new RenderDecoratedBox(new BoxDecoration())))));
        PipelineOwner pipeline = Layout(root, new Size(800.0, 600.0));
        Assert.False(inner.IsRepaintBoundary);
        Assert.Null(inner.DebugLayer);
        Assert.True(boundary.IsRepaintBoundary);
        Assert.NotNull(boundary.DebugLayer);
        Assert.True(boundary.DebugLayer!.Attached); // this time it painted...

        root.Opacity = 0.0;
        PumpFrame(pipeline, new Size(800.0, 600.0));
        Assert.False(inner.IsRepaintBoundary);
        Assert.Null(inner.DebugLayer);
        Assert.True(boundary.IsRepaintBoundary);
        Assert.NotNull(boundary.DebugLayer);
        Assert.False(boundary.DebugLayer!.Attached); // this time it did not.

        root.Opacity = 0.5;
        PumpFrame(pipeline, new Size(800.0, 600.0));
        Assert.False(inner.IsRepaintBoundary);
        Assert.Null(inner.DebugLayer);
        Assert.True(boundary.IsRepaintBoundary);
        Assert.NotNull(boundary.DebugLayer);
        Assert.True(boundary.DebugLayer!.Attached); // this time it did again!
    }

    [Fact]
    public void LeaderLayerApplyTransform_CanBeCalledAfterRetainedRendering()
    {
        static void ExpectTransform(RenderObject leader)
        {
            var leaderLayer = (LeaderLayer)leader.DebugLayer!;
            Matrix4 expected = Matrix4.TranslationValues(leaderLayer.Offset.X, leaderLayer.Offset.Y, 0.0);
            Matrix4 transformed = Matrix4.Identity();
            leaderLayer.ApplyTransform(null, transformed);
            Assert.Equal(expected, transformed);
        }

        var link = new LayerLink();
        RenderLeaderLayer leader;
        var root = new RenderRepaintBoundary(
            child: new RenderRepaintBoundary(child: leader = new RenderLeaderLayer(link)));
        PipelineOwner pipeline = Layout(root, new Size(800.0, 600.0));

        ExpectTransform(leader);

        // Causes a repaint, but the LeaderLayer of RenderLeaderLayer is retained.
        root.MarkNeedsPaint();
        PumpFrame(pipeline, new Size(800.0, 600.0));

        // The LeaderLayer.ApplyTransform call shouldn't crash.
        ExpectTransform(leader);
    }

    // repaint_boundary_test.dart

    [Fact]
    public void NestedRepaintBoundaries_SmokeTest()
    {
        RenderOpacity b;
        RenderOpacity c;
        var a = new RenderOpacity(
            child: new RenderRepaintBoundary(
                child: b = new RenderOpacity(child: new RenderRepaintBoundary(child: c = new RenderOpacity()))));
        PipelineOwner pipeline = Layout(a, new Size(800.0, 600.0));
        pipeline.FlushSemantics();
        c.Opacity = 0.9;
        PumpFrame(pipeline, new Size(800.0, 600.0));
        a.Opacity = 0.8;
        c.Opacity = 0.8;
        PumpFrame(pipeline, new Size(800.0, 600.0));
        a.Opacity = 0.7;
        b.Opacity = 0.7;
        c.Opacity = 0.7;
        PumpFrame(pipeline, new Size(800.0, 600.0));
    }

    [Fact]
    public void RepaintBoundary_CanGetNewParentAfterMarkNeedsCompositingBitsUpdate()
    {
        // Regression test for https://github.com/flutter/flutter/issues/24029.
        var repaintBoundary = new RenderRepaintBoundary();
        PipelineOwner pipeline = Layout(repaintBoundary, new Size(800.0, 600.0));

        repaintBoundary.MarkNeedsCompositingBitsUpdate();

        var renderView = (RenderView)pipeline.Root;
        renderView.Child = null;
        var padding = new RenderPadding(EdgeInsets.All(50));
        renderView.Child = padding;
        padding.Child = repaintBoundary;
        PumpFrame(pipeline, new Size(800.0, 600.0));
    }

    // repaint_boundary_2_test.dart

    [Fact]
    public void RepaintBoundary_WithConstraintChanges()
    {
        // Regression test for https://github.com/flutter/flutter/issues/39151.
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new RelayoutBoundariesCrash());
        tester.State<RelayoutBoundariesCrashState>().ToggleMode();
        tester.Pump();
    }

    // absorb_pointer_test.dart

    [Fact]
    public void AbsorbPointers_DoNotBlockSiblings()
    {
        using var tester = new FrameworkDartTester();
        bool tapped = false;
        tester.PumpWidget(new Column(
            children:
            [
                new Expanded(child: new GestureDetector(onTap: () => tapped = true)),
                new Expanded(child: new AbsorbPointer()),
            ]));
        tester.Tap(tester.ElementOfType<GestureDetector>());
        Assert.True(tapped);
    }

    [Fact]
    public void AbsorbPointerSemantics_DoesNotChangeSemanticsWhenNotAbsorbing()
    {
        var absorbPointer = new RenderAbsorbPointer(
            absorbing: false,
            child: new ActionSemanticBox("button", new Size(40, 20), static () => { }));
        PipelineOwner pipeline = Layout(absorbPointer, new Size(220, 120));
        pipeline.FlushSemantics();

        SemanticsNode? node = FindNodeByLabel(pipeline.SemanticsOwner!.RootNode, "button");
        Assert.NotNull(node);
        Assert.True(node!.Actions.HasFlag(SemanticsActions.Tap));
    }

    // annotated_region_test.dart

    [Fact]
    public void AnnotatedRegion_ProvidesAValueToTheLayerTreeInAParticularRegion()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Transform.Translate(
            new Point(25.0, 25.0),
            child: new AnnotatedRegion<int>(value: 1, child: new SizedBox(width: 100.0, height: 100.0))));
        double devicePixelRatio = tester.View.DevicePixelRatio;
        Layer rootLayer = tester.RenderView.DebugLayer!;
        Assert.Empty(rootLayer.FindAllAnnotations<int>(
            new Point(10.0 * devicePixelRatio, 10.0 * devicePixelRatio)).Entries);
        Assert.Equal(1, Assert.Single(rootLayer.FindAllAnnotations<int>(
            new Point(50.0 * devicePixelRatio, 50.0 * devicePixelRatio)).Entries).Annotation);
    }

    // composited_transform_test.dart

    [Fact]
    public void CompositedTransform_ChangeLinkDuringLayout()
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        GlobalKey key = new LabeledGlobalKey<State>(null);

        Widget Build(LayerLink? linkToUse = null)
        {
            return new Directionality(
                TextDirection.Ltr,
                // The LayoutBuilder forces the CompositedTransformTarget widget to access its own size
                // while a layout is active.
                new LayoutBuilder((context, constraints) => new Stack(
                    children:
                    [
                        new Positioned(
                            left: 123.0,
                            top: 456.0,
                            child: new CompositedTransformTarget(
                                linkToUse ?? link,
                                child: new SizedBox(height: 10.0, width: 10.0))),
                        new Positioned(
                            left: 787.0,
                            top: 343.0,
                            child: new CompositedTransformFollower(
                                linkToUse ?? link,
                                targetAnchor: Alignment.Center,
                                followerAnchor: Alignment.Center,
                                child: new SizedBox(key: key, height: 20.0, width: 20.0))),
                    ])));
        }

        tester.PumpWidget(Build());
        var box = (RenderBox)key.CurrentContext!.FindRenderObject()!;
        AssertOffset(new Point(118.0, 451.0), box.LocalToGlobal(default));

        tester.PumpWidget(Build(linkToUse: new LayerLink()));
        AssertOffset(new Point(118.0, 451.0), box.LocalToGlobal(default));
    }

    [Fact]
    public void CompositedTransform_LeaderLayerShouldNotCauseError()
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();

        Widget BuildWidget(double paddingLeft, Color? siblingColor = null)
        {
            return new Directionality(
                TextDirection.Ltr,
                new Stack(
                    children:
                    [
                        new Padding(
                            EdgeInsets.Only(left: paddingLeft),
                            child: new CompositedTransformTarget(
                                link,
                                child: new RepaintBoundary(
                                    child: new ClipRect(
                                        child: new Container(color: new Color(0x00ff0000)))))),
                        Positioned.Fill(
                            child: new RepaintBoundary(
                                child: new ColoredBox(siblingColor ?? new Color(0xff000000)))),
                    ]));
        }

        tester.PumpWidget(BuildWidget(paddingLeft: 10));
        tester.PumpWidget(BuildWidget(paddingLeft: 0));
        tester.PumpWidget(BuildWidget(paddingLeft: 0, siblingColor: new Color(0x0000ff00)));
    }

    [Theory]
    [InlineData(0, 0, 123.0, 456.0)]
    [InlineData(2, 2, 118.0, 451.0)]
    [InlineData(4, 1, 113.0, 466.0)]
    public void CompositedTransforms_OnlyOffsets(
        int targetAlignment,
        int followerAlignment,
        double expectedX,
        double expectedY)
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        GlobalKey key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Stack(
                children:
                [
                    new Positioned(
                        left: 123.0,
                        top: 456.0,
                        child: new CompositedTransformTarget(link, child: new SizedBox(height: 10.0, width: 10.0))),
                    new Positioned(
                        left: 787.0,
                        top: 343.0,
                        child: new CompositedTransformFollower(
                            link,
                            targetAnchor: Alignments[targetAlignment],
                            followerAnchor: Alignments[followerAlignment],
                            child: new SizedBox(key: key, height: 20.0, width: 20.0))),
                ])));
        var box = (RenderBox)key.CurrentContext!.FindRenderObject()!;
        AssertOffset(new Point(expectedX, expectedY), box.LocalToGlobal(default));
    }

    [Theory]
    [InlineData(0, 0, 0.0, 0.0, 0.0, 0.0)]
    [InlineData(2, 2, 40.0, 5.0, 20.0, 10.0)]
    [InlineData(4, 1, 80.0, 10.0, 40.0, 0.0)]
    public void CompositedTransforms_WithRotations(
        int targetAlignment,
        int followerAlignment,
        double targetX,
        double targetY,
        double followerX,
        double followerY)
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        GlobalKey key1 = new LabeledGlobalKey<State>(null);
        GlobalKey key2 = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Stack(
                children:
                [
                    new Positioned(
                        top: 123.0,
                        left: 456.0,
                        child: Transform.Rotate(
                            1.0, // radians
                            child: new CompositedTransformTarget(
                                link,
                                child: new SizedBox(key: key1, width: 80.0, height: 10.0)))),
                    new Positioned(
                        top: 787.0,
                        left: 343.0,
                        child: Transform.Rotate(
                            -0.3, // radians
                            child: new CompositedTransformFollower(
                                link,
                                targetAnchor: Alignments[targetAlignment],
                                followerAnchor: Alignments[followerAlignment],
                                child: new SizedBox(key: key2, width: 40.0, height: 20.0)))),
                ])));
        var box1 = (RenderBox)key1.CurrentContext!.FindRenderObject()!;
        var box2 = (RenderBox)key2.CurrentContext!.FindRenderObject()!;
        AssertOffset(
            box2.LocalToGlobal(new Point(followerX, followerY)),
            box1.LocalToGlobal(new Point(targetX, targetY)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    [InlineData(4, 1)]
    public void CompositedTransforms_Nested(int targetAlignment, int followerAlignment)
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        GlobalKey key1 = new LabeledGlobalKey<State>(null);
        GlobalKey key2 = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Stack(
                children:
                [
                    new Positioned(
                        top: 123.0,
                        left: 456.0,
                        child: Transform.Rotate(
                            1.0, // radians
                            child: new CompositedTransformTarget(
                                link,
                                child: new SizedBox(key: key1, width: 80.0, height: 10.0)))),
                    new Positioned(
                        top: 787.0,
                        left: 343.0,
                        child: Transform.Rotate(
                            -0.3, // radians
                            child: new Padding(
                                EdgeInsets.All(20.0),
                                child: new CompositedTransformFollower(
                                    new LayerLink(),
                                    child: new Transform(
                                        Matrix4.Skew(0.9, 1.1),
                                        child: new Padding(
                                            EdgeInsets.All(20.0),
                                            child: new CompositedTransformFollower(
                                                link,
                                                targetAnchor: Alignments[targetAlignment],
                                                followerAnchor: Alignments[followerAlignment],
                                                child: new SizedBox(key: key2, width: 40.0, height: 20.0)))))))),
                ])));
        var box1 = (RenderBox)key1.CurrentContext!.FindRenderObject()!;
        var box2 = (RenderBox)key2.CurrentContext!.FindRenderObject()!;
        Point position1 = box1.LocalToGlobal(Alignments[targetAlignment].AlongSize(new Size(80, 10)));
        Point position2 = box2.LocalToGlobal(Alignments[followerAlignment].AlongSize(new Size(40, 20)));
        AssertOffset(position2, position1);
    }

    [Fact]
    public void CompositedTransforms_HitTesting()
    {
        foreach (Alignment targetAlignment in Alignments)
        {
            foreach (Alignment followerAlignment in Alignments)
            {
                using var tester = new FrameworkDartTester();
                var link = new LayerLink();
                GlobalKey key1 = new LabeledGlobalKey<State>(null);
                GlobalKey key2 = new LabeledGlobalKey<State>(null);
                GlobalKey key3 = new LabeledGlobalKey<State>(null);
                bool tapped = false;
                tester.PumpWidget(new Directionality(
                    TextDirection.Ltr,
                    new Stack(
                        children:
                        [
                            new Positioned(
                                left: 123.0,
                                top: 456.0,
                                child: new CompositedTransformTarget(
                                    link,
                                    child: new SizedBox(key: key1, height: 10.0, width: 10.0))),
                            new CompositedTransformFollower(
                                link,
                                child: new GestureDetector(
                                    key: key2,
                                    behavior: HitTestBehavior.Opaque,
                                    onTap: () => tapped = true,
                                    child: new SizedBox(key: key3, height: 2.0, width: 2.0))),
                        ])));
                var box2 = (RenderBox)key2.CurrentContext!.FindRenderObject()!;
                Assert.Equal(new Size(2.0, 2.0), box2.Size);
                Assert.False(tapped, $"{targetAlignment} - {followerAlignment}");
                tester.Tap(tester.ElementsWithKey(key3).Single());
                Assert.True(tapped, $"{targetAlignment} - {followerAlignment}");
            }
        }
    }

    [Fact]
    public void CompositedTransformTargetAndFollower_DoNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.View.UpdateMetrics(physicalSize: new Size(0, 0));
        RendererBinding.Instance.HandleMetricsChanged();
        var link = new LayerLink();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Center(
                child: new CompositedTransformTarget(
                    link,
                    child: new CompositedTransformFollower(link)))));
        Assert.Equal(
            new Size(0, 0),
            ((RenderBox)tester.ElementOfType<CompositedTransformTarget>().FindRenderObject()!).Size);
        Assert.Equal(
            new Size(0, 0),
            ((RenderBox)tester.ElementOfType<CompositedTransformFollower>().FindRenderObject()!).Size);
    }

    private static PipelineOwner Layout(RenderBox root, Size size)
    {
        var renderView = new RenderView(new FlutterView(new Size(800, 600))) { Child = root };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        PumpFrame(pipeline, size);
        return pipeline;
    }

    // rendering_tester.dart's `layout` pumps only to `EnginePhase.layout`.
    private static void LayoutOnly(RenderBox root, Size size)
    {
        var renderView = new RenderView(new FlutterView(new Size(800, 600))) { Child = root };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(size);
    }

    private static void PumpFrame(PipelineOwner pipeline, Size size)
    {
        pipeline.FlushLayout(size);
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();
    }

    private static void AssertOffset(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 8);
        Assert.Equal(expected.Y, actual.Y, precision: 8);
    }

    private static SemanticsNode? FindNodeByLabel(SemanticsNode? node, string label)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Label == label)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? match = FindNodeByLabel(child, label);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>Flutter's <c>RenderSizedBox</c> from <c>rendering_tester.dart</c>.</summary>
    private sealed class RenderSizedBox : RenderBox
    {
        private readonly Size _size;

        public RenderSizedBox(Size size)
        {
            _size = size;
        }

        protected override double ComputeMinIntrinsicWidth(double height) => _size.Width;

        protected override double ComputeMaxIntrinsicWidth(double height) => _size.Width;

        protected override double ComputeMinIntrinsicHeight(double width) => _size.Height;

        protected override double ComputeMaxIntrinsicHeight(double width) => _size.Height;

        protected override bool SizedByParent => true;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_size);

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class CallbackPainter : CustomPainter
    {
        private readonly Action _onPaint;

        public CallbackPainter(Action onPaint)
        {
            _onPaint = onPaint;
        }

        public override void Paint(PaintingContext context, Size size) => _onPaint();

        public override bool ShouldRepaint(CustomPainter oldDelegate) => true;
    }

    private sealed class ActionSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;
        private readonly Action _onTap;

        public ActionSemanticBox(string label, Size size, Action onTap)
        {
            _label = label;
            _size = size;
            _onTap = onTap;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            configuration.Label = _label;
            configuration.TextDirection = TextDirection.Ltr;
            configuration.OnTap = _onTap;
        }
    }

    private sealed class RelayoutBoundariesCrash : StatefulWidget
    {
        public override State CreateState() => new RelayoutBoundariesCrashState();
    }

    private sealed class RelayoutBoundariesCrashState : State<RelayoutBoundariesCrash>
    {
        private bool _mode = true;

        public void ToggleMode()
        {
            SetState(() => _mode = !_mode);
        }

        public override Widget Build(BuildContext context)
        {
            return new Center(
                child: new SizedBox(
                    // when _mode is true, constraints are tight, otherwise constraints are loose
                    width: !_mode ? 100.0 : null,
                    height: !_mode ? 100.0 : null,
                    child: new LayoutBuilder((innerContext, constraints) =>
                    {
                        // Make the outer SizedBoxes relayout without making the Placeholders relayout.
                        double dimension = !_mode ? 10.0 : 20.0;
                        return new Column(
                            children:
                            [
                                new SizedBox(width: dimension, height: dimension, child: new Placeholder()),
                                new SizedBox(width: dimension, height: dimension, child: new Placeholder()),
                            ]);
                    })));
        }
    }
}
