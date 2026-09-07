using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart
// (tightening pass: intrinsics, dry layout, alpha semantics, clipping, hit testing)

namespace Plumix.Tests;

public sealed class ProxyBoxParityTests
{
    [Fact]
    public void RenderPointerListener_WithoutAChild_TakesTheBiggestConstraint()
    {
        // Flutter's `RenderPointerListener.computeSizeForNoChild` returns `constraints.biggest`,
        // and the hook feeds both `performLayout` and `computeDryLayout`.
        var listener = new RenderPointerListener();
        var constraints = new BoxConstraints(MaxWidth: 120.0, MaxHeight: 40.0);

        Assert.Equal(new Size(120.0, 40.0), listener.GetDryLayout(constraints));

        LayoutRoot(listener, new Size(120.0, 40.0));
        Assert.Equal(new Size(120.0, 40.0), listener.Size);
    }

    [Fact]
    public void RenderConstrainedBox_IntrinsicsFallThroughForUnboundedAndInfiniteConstraints()
    {
        var child = new DesiredSizeBox(new Size(37.0, 21.0));

        // A tight-but-unbounded width is not `hasBoundedWidth`, so Dart falls through to the child.
        var unbounded = new RenderConstrainedBox(
            new BoxConstraints(
                MinWidth: double.PositiveInfinity,
                MaxWidth: double.PositiveInfinity,
                MinHeight: 0.0,
                MaxHeight: double.PositiveInfinity),
            child);
        Assert.Equal(37.0, unbounded.GetMaxIntrinsicWidth(double.PositiveInfinity));

        // A bounded tight width short-circuits to `minWidth`.
        var tight = new RenderConstrainedBox(
            BoxConstraints.TightFor(width: 55.0),
            new DesiredSizeBox(new Size(37.0, 21.0)));
        Assert.Equal(55.0, tight.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(55.0, tight.GetMinIntrinsicWidth(double.PositiveInfinity));
    }

    [Fact]
    public void RenderLimitedBox_DryLayoutLimitsUnboundedConstraints()
    {
        var child = new DesiredSizeBox(new Size(400.0, 400.0));
        var limited = new RenderLimitedBox(maxWidth: 100.0, maxHeight: 60.0, child: child);

        // Flutter's `computeDryLayout` runs the child under `_limitConstraints`, so the unbounded
        // axes are capped before the child is measured.
        Assert.Equal(new Size(100.0, 60.0), limited.GetDryLayout(BoxConstraints.Unbounded));

        // Bounded incoming constraints win over the limits, as in Dart.
        Assert.Equal(
            new Size(200.0, 200.0),
            limited.GetDryLayout(new BoxConstraints(MaxWidth: 200.0, MaxHeight: 200.0)));
    }

    [DebugOnlyFact]
    public void RenderLimitedBox_SettersUseExactEquality()
    {
        var box = new RenderLimitedBox(maxWidth: 100.0, child: new DesiredSizeBox(new Size(10, 10)));
        LayoutRoot(box, new Size(100, 100));
        Assert.False(box.DebugNeedsLayout);

        box.MaxWidth = 100.00001;

        Assert.Equal(100.00001, box.MaxWidth);
        Assert.True(box.DebugNeedsLayout);
    }

    [Fact]
    public void RenderAspectRatio_IntrinsicsAndDryLayoutApplyTheRatio()
    {
        var aspect = new RenderAspectRatio(2.0, new DesiredSizeBox(new Size(11.0, 13.0)));

        Assert.Equal(60.0, aspect.GetMinIntrinsicWidth(30.0));
        Assert.Equal(60.0, aspect.GetMaxIntrinsicWidth(30.0));
        Assert.Equal(15.0, aspect.GetMinIntrinsicHeight(30.0));
        Assert.Equal(15.0, aspect.GetMaxIntrinsicHeight(30.0));

        // An infinite probe falls through to the child.
        Assert.Equal(11.0, aspect.GetMaxIntrinsicWidth(double.PositiveInfinity));

        Assert.Equal(
            new Size(100.0, 50.0),
            aspect.GetDryLayout(new BoxConstraints(MaxWidth: 100.0, MaxHeight: 200.0)));
    }

    [Fact]
    public void RenderIntrinsicWidth_SnapsIntrinsicsToTheStepsAndReportsMinAsMax()
    {
        var intrinsic = new RenderIntrinsicWidth(
            stepWidth: 56.0,
            stepHeight: 10.0,
            child: new DesiredSizeBox(new Size(70.0, 21.0)));

        Assert.Equal(112.0, intrinsic.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(intrinsic.GetMaxIntrinsicWidth(50.0), intrinsic.GetMinIntrinsicWidth(50.0));
        Assert.Equal(30.0, intrinsic.GetMaxIntrinsicHeight(200.0));

        Assert.Equal(
            new Size(112.0, 30.0),
            intrinsic.GetDryLayout(new BoxConstraints(MaxWidth: 200.0, MaxHeight: 100.0)));
    }

    [Fact]
    public void RenderIntrinsicHeight_SubstitutesTheChildHeightForAnInfiniteProbe()
    {
        var intrinsic = new RenderIntrinsicHeight(new WidthFromHeightBox(intrinsicHeight: 25.0));

        // Dart replaces an infinite height with the child's own max intrinsic height first.
        Assert.Equal(50.0, intrinsic.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(intrinsic.GetMaxIntrinsicHeight(10.0), intrinsic.GetMinIntrinsicHeight(10.0));
    }

    [Fact]
    public void RenderIgnoreBaseline_ReportsNoDryBaselineEither()
    {
        var ignore = new RenderIgnoreBaseline(new BaselineBox(new Size(20, 20), baseline: 12.0));

        Assert.Null(ignore.GetDryBaseline(BoxConstraints.Unbounded, TextBaseline.Alphabetic));
    }

    [Fact]
    public void RenderBaseline_ShiftsTheChildAndReportsTheDryLayout()
    {
        var child = new BaselineBox(new Size(20, 20), baseline: 12.0);
        var baseline = new RenderBaseline(30.0, TextBaseline.Alphabetic, child);

        Assert.Equal(new Size(20.0, 38.0), baseline.GetDryLayout(BoxConstraints.Unbounded));

        LayoutRoot(baseline, new Size(100, 100));
        Assert.Equal(new Point(0.0, 18.0), ((BoxParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderOpacity_UsesTheRoundedAlphaEverywhere()
    {
        var child = new DesiredSizeBox(new Size(10, 10));
        var opacity = new RenderOpacity(1.0, child);
        LayoutRoot(opacity, new Size(10, 10));

        Assert.Equal(1.0, opacity.Opacity);
        Assert.True(opacity.PaintsChild(child));

        // `getAlphaFromOpacity` rounds anything below 0.5/255 down to a fully transparent alpha.
        opacity.Opacity = 0.001;

        Assert.False(opacity.PaintsChild(child));
        Assert.False(opacity.NeedsCompositing);

        int semanticsVisits = 0;
        opacity.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);
    }

    [Fact]
    public void RenderOpacity_PaintsNothingWhenFullyTransparent()
    {
        var child = new PaintCountingBox(new Size(10, 10));
        var opacity = new RenderOpacity(0.0, child);
        LayoutRoot(opacity, new Size(10, 10));

        opacity.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));

        Assert.Equal(0, child.PaintCount);
    }

    [Fact]
    public void RenderAnimatedOpacity_TracksTheAnimationWithoutRelayout()
    {
        var controller = new AnimationValue(1.0);
        var child = new PaintCountingBox(new Size(10, 10));
        var animated = new RenderAnimatedOpacity(controller, child: child);
        LayoutRoot(animated, new Size(10, 10));

        Assert.True(animated.IsRepaintBoundary);
        Assert.True(animated.PaintsChild(child));

        controller.SetValue(0.0);

        Assert.False(animated.IsRepaintBoundary);
        Assert.False(animated.PaintsChild(child));

        animated.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));
        Assert.Equal(0, child.PaintCount);

        Assert.False(animated.NeedsCompositing);
    }

    [Fact]
    public void FadeTransition_CreatesARenderAnimatedOpacity()
    {
        var owner = new BuildOwner();
        var animation = new AnimationValue(0.5);
        var root = new TestRootElement(new FadeTransition(animation, new SizedBox(width: 10, height: 10)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderObject? renderObject = FindFirstRenderObject(root);
        var animated = Assert.IsType<RenderAnimatedOpacity>(renderObject);
        Assert.Same(animation, animated.Opacity);
        Assert.False(animated.AlwaysIncludeSemantics);
    }

    [Fact]
    public void RenderClipRSuperellipse_ClipsToTheResolvedBorderRadiusAndHitTestsTheBoundingBox()
    {
        var child = new HitTestBox(new Size(40, 40));
        var clip = new RenderClipRSuperellipse(
            child,
            borderRadius: BorderRadiusDirectional.Only(topStart: 12.0),
            textDirection: TextDirection.Ltr);
        LayoutRoot(clip, new Size(40, 40));

        // With no clipper Dart never rejects, and with one it rejects on the bounding box only —
        // the corner pixel is inside that box.
        Assert.True(clip.HitTest(new BoxHitTestResult(), new Point(0.5, 0.5)));

        clip.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));
        Assert.Null(clip.DebugLayer);

        clip.ClipBehavior = Clip.None;
        clip.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));
        Assert.Null(clip.DebugLayer);
    }

    [Fact]
    public void ClipRSuperellipseWidget_BuildsTheDedicatedRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            TextDirection.Rtl,
            new ClipRSuperellipse(
                borderRadius: BorderRadius.Circular(8.0),
                clipBehavior: Clip.HardEdge,
                child: new SizedBox(width: 20, height: 20))));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var clip = Assert.IsType<RenderClipRSuperellipse>(FindFirstRenderObject(root));
        Assert.Equal(Clip.HardEdge, clip.ClipBehavior);
        Assert.Equal(TextDirection.Rtl, clip.TextDirection);
    }

    [Fact]
    public void RenderClipRect_DefaultsToAntiAliasLikeTheCustomClipBase()
    {
        Assert.Equal(Clip.AntiAlias, new RenderClipRect().ClipBehavior);
        Assert.Equal(Clip.AntiAlias, new RenderClipOval().ClipBehavior);
        Assert.Equal(Clip.AntiAlias, new RenderClipPath().ClipBehavior);
        Assert.Equal(Clip.AntiAlias, new RenderClipRRect().ClipBehavior);
        Assert.Equal(Clip.AntiAlias, new RenderClipRSuperellipse().ClipBehavior);
    }

    [DebugOnlyFact]
    public void RenderCustomClip_ClipBehaviorOnlyRepaints()
    {
        var clip = new RenderClipRect(new DesiredSizeBox(new Size(20, 20)));
        LayoutRoot(clip, new Size(20, 20));
        Assert.False(clip.DebugNeedsLayout);

        clip.ClipBehavior = Clip.HardEdge;

        // Dart's `clipBehavior` setter calls `markNeedsPaint()` only.
        Assert.False(clip.DebugNeedsLayout);
        Assert.True(clip.DebugNeedsPaint);
    }

    [Fact]
    public void RenderMergeSemantics_MarksTheBoundaryAndMergesDescendants()
    {
        var merge = new RenderMergeSemantics(new DesiredSizeBox(new Size(10, 10)));
        var configuration = new SemanticsConfiguration();

        merge.InvokeDescribeSemanticsConfiguration(configuration);

        Assert.True(configuration.IsSemanticBoundary);
        Assert.True(configuration.IsMergingSemanticsOfDescendants);
    }

    [Fact]
    public void MergeSemanticsWidget_BuildsTheDedicatedRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new MergeSemantics(new SizedBox(width: 10, height: 10)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.IsType<RenderMergeSemantics>(FindFirstRenderObject(root));
    }

    [Fact]
    public void RenderSemanticsAnnotations_ExcludeSemanticsAndBlockUserActions()
    {
        var annotations = new RenderSemanticsAnnotations(child: new DesiredSizeBox(new Size(10, 10)))
        {
            ExcludeSemantics = true,
            BlockUserActions = true,
        };

        int visits = 0;
        annotations.VisitChildrenForSemantics(_ => visits++);
        Assert.Equal(0, visits);

        var configuration = new SemanticsConfiguration();
        annotations.InvokeDescribeSemanticsConfiguration(configuration);
        Assert.True(configuration.IsBlockingUserActions);
    }

    [Fact]
    public void RenderDecoratedBox_HitTestsItsDecoration()
    {
        var decorated = new RenderDecoratedBox(
            new BoxDecoration(Color: Colors.Red),
            child: new DesiredSizeBox(new Size(30, 30)));
        LayoutRoot(decorated, new Size(30, 30));

        var result = new BoxHitTestResult();
        Assert.True(decorated.HitTest(result, new Point(10, 10)));
        Assert.Contains(result.Path, entry => ReferenceEquals(entry.Target, decorated));
    }

    [Fact]
    public void RenderFittedBox_PaintsNothingForAnEmptySize()
    {
        var child = new PaintCountingBox(new Size(1, 1));
        var fitted = new RenderFittedBox(child: child);
        LayoutRoot(fitted, new Size(1, 1));
        fitted.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));
        Assert.Equal(1, child.PaintCount);

        var emptyChild = new PaintCountingBox(new Size(0, 0));
        var emptyFitted = new RenderFittedBox(child: emptyChild);
        LayoutRoot(emptyFitted, new Size(0, 0));
        emptyFitted.Paint(new PaintingContext(new ContainerLayer()), new Point(0, 0));
        Assert.Equal(0, emptyChild.PaintCount);
    }

    [DebugOnlyFact]
    public void RenderPhysicalModel_ClipsACircleToAnEllipticalRoundedRect()
    {
        var physical = new RenderPhysicalModel(
            color: Colors.Orange,
            child: new DesiredSizeBox(new Size(40, 24)),
            shape: BoxShape.Circle,
            clipBehavior: Clip.AntiAlias);
        LayoutRoot(physical, new Size(40, 24));
        Assert.False(physical.DebugNeedsLayout);

        // Dart's `_defaultClip` builds `RRect.fromRectXY(rect, width / 2, height / 2)` for a circle,
        // so a non-square box clips to an ellipse rather than a stadium.
        Rect? approximateClip = physical.InvokeDescribeApproximatePaintClip(null);
        Assert.Equal(new Rect(0, 0, 40, 24), approximateClip);

        physical.Shape = BoxShape.Rectangle;
        Assert.True(physical.DebugNeedsPaint);
    }

    private static RenderObject? FindFirstRenderObject(Element element)
    {
        RenderObject? found = null;
        void Visit(Element current)
        {
            if (found is not null)
            {
                return;
            }

            if (current.RenderObject is { } renderObject)
            {
                found = renderObject;
                return;
            }

            current.VisitChildren(Visit);
        }

        element.VisitChildren(Visit);
        return found;
    }

    private static void LayoutRoot(RenderBox render, Size size)
    {
        var view = new RenderView { Child = render };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(size);
    }

    private sealed class AnimationValue : Animation<double>
    {
        private readonly List<Action> _listeners = [];
        private double _value;

        public AnimationValue(double value)
        {
            _value = value;
        }

        public override double Value => _value;

        public override AnimationStatus Status => AnimationStatus.Forward;

        public void SetValue(double value)
        {
            _value = value;
            foreach (Action listener in _listeners.ToArray())
            {
                listener();
            }
        }

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
        }
    }

    private class DesiredSizeBox : RenderBox
    {
        private readonly Size _desiredSize;

        public DesiredSizeBox(Size desiredSize)
        {
            _desiredSize = desiredSize;
        }

        protected override double ComputeMaxIntrinsicWidth(double height) => _desiredSize.Width;

        protected override double ComputeMinIntrinsicWidth(double height) => _desiredSize.Width;

        protected override double ComputeMaxIntrinsicHeight(double width) => _desiredSize.Height;

        protected override double ComputeMinIntrinsicHeight(double width) => _desiredSize.Height;

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override void PerformLayout() => Size = Constraints.Constrain(_desiredSize);

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class PaintCountingBox : RenderBox
    {
        private readonly Size _desiredSize;

        public PaintCountingBox(Size desiredSize)
        {
            _desiredSize = desiredSize;
        }

        public int PaintCount { get; private set; }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override void PerformLayout() => Size = Constraints.Constrain(_desiredSize);

        public override void Paint(PaintingContext context, Point offset) => PaintCount++;
    }

    private sealed class HitTestBox : RenderBox
    {
        private readonly Size _desiredSize;

        public HitTestBox(Size desiredSize)
        {
            _desiredSize = desiredSize;
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override void PerformLayout() => Size = Constraints.Constrain(_desiredSize);

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class BaselineBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly double _baseline;

        public BaselineBox(Size desiredSize, double baseline)
        {
            _desiredSize = desiredSize;
            _baseline = baseline;
        }

        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(_desiredSize);

        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => _baseline;

        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => _baseline;

        protected override void PerformLayout() => Size = Constraints.Constrain(_desiredSize);

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    /// <summary>A child whose intrinsic width is twice the height it is asked about.</summary>
    private sealed class WidthFromHeightBox : RenderBox
    {
        private readonly double _intrinsicHeight;

        public WidthFromHeightBox(double intrinsicHeight)
        {
            _intrinsicHeight = intrinsicHeight;
        }

        protected override double ComputeMaxIntrinsicWidth(double height) => height * 2.0;

        protected override double ComputeMinIntrinsicWidth(double height) => height * 2.0;

        protected override double ComputeMaxIntrinsicHeight(double width) => _intrinsicHeight;

        protected override double ComputeMinIntrinsicHeight(double width) => _intrinsicHeight;

        protected override Size ComputeDryLayout(BoxConstraints constraints) =>
            constraints.Constrain(new Size(_intrinsicHeight * 2.0, _intrinsicHeight));

        protected override void PerformLayout() =>
            Size = Constraints.Constrain(new Size(_intrinsicHeight * 2.0, _intrinsicHeight));

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
