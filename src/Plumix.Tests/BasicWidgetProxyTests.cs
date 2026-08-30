using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class BasicWidgetProxyTests
{
    /// <summary>Dart asserts `opacity >= 0.0 && opacity <= 1.0` in the widget constructor; the render
    /// object still clamps, which is what an out-of-range value set directly on it hits.</summary>
    [DebugOnlyFact]
    public void OpacityWidget_RejectsAnOutOfRangeOpacity()
    {
        Assert.Throws<AssertionError>(() => new Opacity(opacity: 1.5, child: new SizedBox()));
    }

    [Fact]
    public void OpacityWidget_CreatesRenderOpacity_AndUpdatesOpacity()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Opacity(
                opacity: 1.0,
                child: new SizedBox(width: 16, height: 16)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderOpacity = RequireRenderObject<RenderOpacity>(root.ChildElement);
        Assert.Equal(1.0, renderOpacity.Opacity);

        root.Update(new Opacity(
            opacity: 0.25,
            child: new SizedBox(width: 16, height: 16)));
        owner.FlushBuild();

        var updatedRenderOpacity = RequireRenderObject<RenderOpacity>(root.ChildElement);
        Assert.Same(renderOpacity, updatedRenderOpacity);
        Assert.Equal(0.25, updatedRenderOpacity.Opacity);
    }

    [Fact]
    public void OpacityWidget_HidesZeroOpacitySemanticsUnlessAlwaysIncluded()
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 16, height: 16));
        var opacity = new RenderOpacity(opacity: 0.0, child: child);
        int visits = 0;

        opacity.VisitChildrenForSemantics(_ => visits++);
        Assert.Equal(0, visits);

        opacity.AlwaysIncludeSemantics = true;
        opacity.VisitChildrenForSemantics(_ => visits++);
        Assert.Equal(1, visits);
    }

    [Fact]
    public void TransformWidget_CreatesRenderTransform_AndUpdatesTransform()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Transform(
                transform: Matrix4.TranslationValues(12, 6, 0.0),
                alignment: Alignment.TopLeft,
                filterQuality: FilterQuality.Low,
                child: new SizedBox(width: 20, height: 12)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderTransform = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.Equal(Matrix4.TranslationValues(12, 6, 0.0), renderTransform.Transform);
        Assert.Equal(Alignment.TopLeft, renderTransform.Alignment);
        Assert.Equal(FilterQuality.Low, renderTransform.FilterQuality);

        root.Update(new Transform(
            transform: Matrix4.TranslationValues(30, 18, 0.0),
            alignment: Alignment.BottomRight,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 20, height: 12)));
        owner.FlushBuild();

        var updatedRenderTransform = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.Same(renderTransform, updatedRenderTransform);
        Assert.Equal(Matrix4.TranslationValues(30, 18, 0.0), updatedRenderTransform.Transform);
        Assert.Equal(Alignment.BottomRight, updatedRenderTransform.Alignment);
        Assert.Equal(FilterQuality.High, updatedRenderTransform.FilterQuality);
    }

    [Fact]
    public void FractionalTranslationWidget_UsesFlutterDefaultsAndUpdatesRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new FractionalTranslation(
                translation: new Vector(0.25, -0.5),
                child: new SizedBox(width: 20, height: 12)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var translation = RequireRenderObject<RenderFractionalTranslation>(root.ChildElement);
        Assert.Equal(new Vector(0.25, -0.5), translation.Translation);
        Assert.True(translation.TransformHitTests);

        root.Update(new FractionalTranslation(
            translation: new Vector(double.PositiveInfinity, 0.75),
            transformHitTests: false,
            child: new SizedBox(width: 20, height: 12)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderFractionalTranslation>(root.ChildElement);
        Assert.Same(translation, updated);
        Assert.Equal(new Vector(double.PositiveInfinity, 0.75), updated.Translation);
        Assert.False(updated.TransformHitTests);
    }

    [Fact]
    public void RenderFractionalTranslation_HitTestingAndSemanticsFollowPaintOffset()
    {
        var child = new RegionHitTestRenderBox(new Size(20, 10), new Rect(0, 0, 20, 10));
        var translation = new RenderFractionalTranslation(
            translation: new Vector(1, 0),
            transformHitTests: true,
            child: child);

        translation.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        var shiftedResult = new BoxHitTestResult();
        Assert.True(translation.HitTest(shiftedResult, new Point(25, 5)));
        Assert.Contains(shiftedResult.Path, entry => ReferenceEquals(entry.Target, child));

        translation.TransformHitTests = false;
        Assert.True(translation.HitTest(new BoxHitTestResult(), new Point(5, 5)));
        Assert.False(translation.HitTest(new BoxHitTestResult(), new Point(25, 5)));

        Matrix4 semanticsTransform = Matrix4.Identity();
        translation.ApplyPaintTransform(translation.Child!, semanticsTransform);
        Assert.Equal(Matrix4.TranslationValues(20, 0, 0.0), semanticsTransform);
    }

    [Fact]
    public void RotatedBoxWidget_CreatesAndUpdatesRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new RotatedBox(
                quarterTurns: 1,
                child: new SizedBox(width: 80, height: 30)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var rotated = RequireRenderObject<RenderRotatedBox>(root.ChildElement);
        Assert.Equal(1, rotated.QuarterTurns);

        root.Update(new RotatedBox(
            quarterTurns: -2,
            child: new SizedBox(width: 80, height: 30)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderRotatedBox>(root.ChildElement);
        Assert.Same(rotated, updated);
        Assert.Equal(-2, updated.QuarterTurns);
    }

    [Fact]
    public void RenderRotatedBox_QuarterTurnFlipsLayoutAndTransformsHitTestsAndSemantics()
    {
        var child = new RegionHitTestRenderBox(
            new Size(80, 30),
            new Rect(0, 0, 20, 10),
            baseline: 8);
        var rotated = new RenderRotatedBox(quarterTurns: 1, child: child);

        rotated.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 200));

        Assert.Equal(new BoxConstraints(MaxWidth: 200, MaxHeight: 100), child.LastConstraints);
        Assert.Equal(new Size(30, 80), rotated.Size);
        Assert.Null(rotated.GetDistanceToBaseline(Plumix.UI.TextBaseline.Alphabetic, onlyReal: true));
        Assert.True(rotated.HitTest(new BoxHitTestResult(), new Point(25, 10)));
        Assert.False(rotated.HitTest(new BoxHitTestResult(), new Point(10, 60)));

        Matrix4 expectedTransform = Matrix4.TranslationValues(15, 40, 0.0);
        expectedTransform.RotateZ(Math.PI / 2.0);
        expectedTransform.TranslateByDouble(-40, -15, 0, 1);
        Matrix4 semanticsTransform = Matrix4.Identity();
        rotated.ApplyPaintTransform(rotated.Child!, semanticsTransform);
        Assert.Equal(expectedTransform, semanticsTransform);

        var renderView = new RenderView { Child = rotated };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(30, 80));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        // Dart's `pushTransform` records the rotation onto the canvas: no layer, one picture.
        Assert.IsType<PictureLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Null(rotated.DebugLayer);

        rotated.QuarterTurns = 2;
        rotated.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 200));
        Assert.Equal(new Size(80, 30), rotated.Size);
    }

    [Fact]
    public void ClipRectWidget_CreatesRenderClipRect_AndUpdatesClip()
    {
        var owner = new BuildOwner();
        var initialClipper = new FixedRectClipper(new Rect(1, 2, 30, 40));
        var root = new TestRootElement(
            new ClipRect(
                clipper: initialClipper,
                child: new SizedBox(width: 40, height: 50)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderClipRect = RequireRenderObject<RenderClipRect>(root.ChildElement);
        Assert.Same(initialClipper, renderClipRect.Clipper);
        Assert.Equal(Plumix.UI.Clip.HardEdge, renderClipRect.ClipBehavior);

        var updatedClipper = new FixedRectClipper(new Rect(4, 6, 12, 14));
        root.Update(new ClipRect(
            clipper: updatedClipper,
            child: new SizedBox(width: 40, height: 50)));
        owner.FlushBuild();

        var updatedRenderClipRect = RequireRenderObject<RenderClipRect>(root.ChildElement);
        Assert.Same(renderClipRect, updatedRenderClipRect);
        Assert.Same(updatedClipper, updatedRenderClipRect.Clipper);
    }

    [Fact]
    public void ClipRRectWidget_CreatesRenderClipRRect_AndUpdatesBorderRadius()
    {
        var owner = new BuildOwner();
        var initialBorderRadius = BorderRadius.Circular(8);
        var root = new TestRootElement(
            new ClipRRect(
                borderRadius: initialBorderRadius,
                child: new SizedBox(width: 40, height: 50)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderClipRRect = RequireRenderObject<RenderClipRRect>(root.ChildElement);
        Assert.Equal(initialBorderRadius, renderClipRRect.BorderRadius);

        var updatedBorderRadius = BorderRadius.Circular(18);
        root.Update(new ClipRRect(
            borderRadius: updatedBorderRadius,
            child: new SizedBox(width: 40, height: 50)));
        owner.FlushBuild();

        var updatedRenderClipRRect = RequireRenderObject<RenderClipRRect>(root.ChildElement);
        Assert.Same(renderClipRRect, updatedRenderClipRRect);
        Assert.Equal(updatedBorderRadius, updatedRenderClipRRect.BorderRadius);
    }

    [Fact]
    public void IgnorePointerWidget_CreatesRenderIgnorePointer_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new IgnorePointer(
                ignoring: true,
                ignoringSemantics: null,
                child: new SizedBox(width: 16, height: 16)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var ignorePointer = RequireRenderObject<RenderIgnorePointer>(root.ChildElement);
        Assert.True(ignorePointer.Ignoring);
        Assert.Null(ignorePointer.IgnoringSemantics);

        root.Update(new IgnorePointer(
            ignoring: false,
            ignoringSemantics: true,
            child: new SizedBox(width: 16, height: 16)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderIgnorePointer>(root.ChildElement);
        Assert.Same(ignorePointer, updated);
        Assert.False(updated.Ignoring);
        Assert.True(updated.IgnoringSemantics);
    }

    [Fact]
    public void AbsorbPointerWidget_CreatesRenderAbsorbPointer_AndUpdatesProperties()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new AbsorbPointer(
                absorbing: true,
                ignoringSemantics: null,
                child: new SizedBox(width: 16, height: 16)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var absorbPointer = RequireRenderObject<RenderAbsorbPointer>(root.ChildElement);
        Assert.True(absorbPointer.Absorbing);
        Assert.Null(absorbPointer.IgnoringSemantics);

        root.Update(new AbsorbPointer(
            absorbing: false,
            ignoringSemantics: true,
            child: new SizedBox(width: 16, height: 16)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderAbsorbPointer>(root.ChildElement);
        Assert.Same(absorbPointer, updated);
        Assert.False(updated.Absorbing);
        Assert.True(updated.IgnoringSemantics);
    }

    [Fact]
    public void WrapWidget_CreatesRenderWrap_AndUpdatesRunConfiguration()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Wrap(
            spacing: 3,
            runSpacing: 5,
            alignment: WrapAlignment.SpaceAround,
            runAlignment: WrapAlignment.Center,
            crossAxisAlignment: WrapCrossAlignment.End,
            textDirection: Plumix.UI.TextDirection.Rtl,
            children:
            [
                new SizedBox(width: 10, height: 10),
                new SizedBox(width: 20, height: 10),
            ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var wrap = RequireRenderObject<RenderWrap>(root.ChildElement);
        Assert.Equal(3, wrap.Spacing);
        Assert.Equal(5, wrap.RunSpacing);
        Assert.Equal(WrapAlignment.SpaceAround, wrap.Alignment);
        Assert.Equal(WrapCrossAlignment.End, wrap.CrossAxisAlignment);
        Assert.Equal(Plumix.UI.TextDirection.Rtl, wrap.TextDirection);

        root.Update(new Wrap(
            direction: Axis.Vertical,
            spacing: 7,
            runSpacing: 11,
            alignment: WrapAlignment.SpaceEvenly,
            runAlignment: WrapAlignment.End,
            crossAxisAlignment: WrapCrossAlignment.Center,
            textDirection: Plumix.UI.TextDirection.Ltr,
            verticalDirection: Plumix.Painting.VerticalDirection.Up,
            children:
            [
                new SizedBox(width: 10, height: 10),
                new SizedBox(width: 20, height: 10),
            ]));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderWrap>(root.ChildElement);
        Assert.Same(wrap, updated);
        Assert.Equal(Axis.Vertical, updated.Direction);
        Assert.Equal(7, updated.Spacing);
        Assert.Equal(11, updated.RunSpacing);
        Assert.Equal(WrapAlignment.SpaceEvenly, updated.Alignment);
        Assert.Equal(WrapAlignment.End, updated.RunAlignment);
        Assert.Equal(WrapCrossAlignment.Center, updated.CrossAxisAlignment);
        Assert.Equal(Plumix.UI.TextDirection.Ltr, updated.TextDirection);
        Assert.Equal(Plumix.Painting.VerticalDirection.Up, updated.VerticalDirection);
    }

    [Fact]
    public void FlowWidget_WrapsChildrenInRepaintBoundariesAndUpdatesRenderObject()
    {
        var keyedChild = new SizedBox(
            width: 10,
            height: 10,
            key: new ValueKey<string>("keyed"));
        var plainChild = new SizedBox(width: 20, height: 10);
        var initialDelegate = new FlowDelegateProbe();
        var widget = new Flow(
            initialDelegate,
            children: [keyedChild, plainChild]);

        Assert.Equal(Clip.HardEdge, widget.ClipBehavior);
        Assert.All(widget.Children, child => Assert.IsType<RepaintBoundary>(child));
        Assert.Equal(
            new ValueKey<object>(keyedChild.Key!),
            Assert.IsType<RepaintBoundary>(widget.Children[0]).Key);
        Assert.Equal(
            new ValueKey<object>(1),
            Assert.IsType<RepaintBoundary>(widget.Children[1]).Key);

        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderFlow = RequireRenderObject<RenderFlow>(root.ChildElement);
        Assert.Same(initialDelegate, renderFlow.Delegate);
        Assert.Equal(Clip.HardEdge, renderFlow.ClipBehavior);
        Assert.IsType<RenderRepaintBoundary>(renderFlow.FirstChild);

        var updatedDelegate = new FlowDelegateProbe();
        root.Update(new Flow(
            updatedDelegate,
            children: [keyedChild, plainChild],
            clipBehavior: Clip.None));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderFlow>(root.ChildElement);
        Assert.Same(renderFlow, updated);
        Assert.Same(updatedDelegate, updated.Delegate);
        Assert.Equal(Clip.None, updated.ClipBehavior);
    }

    [Fact]
    public void FlowUnwrappedAndRepaintBoundaryWrapHelpersMatchFlutterShape()
    {
        var child = new SizedBox(width: 10, height: 10);
        Flow flow = Flow.Unwrapped(new FlowDelegateProbe(), children: [child]);

        Assert.Same(child, Assert.Single(flow.Children));

        RepaintBoundary wrapped = RepaintBoundary.Wrap(child, 7);
        Assert.Same(child, wrapped.Child);
        Assert.Equal(new ValueKey<object>(7), wrapped.Key);

        IReadOnlyList<RepaintBoundary> wrappedAll = RepaintBoundary.WrapAll([child]);
        Assert.Single(wrappedAll);
        Assert.IsType<RepaintBoundary>(wrappedAll[0]);
    }

    [Fact]
    public void RowWidget_ForwardsAndUpdatesTextBaseline()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Row(
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: Plumix.UI.TextBaseline.Alphabetic,
            children: [new SizedBox(width: 10, height: 10)]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var flex = RequireRenderObject<RenderFlex>(root.ChildElement);
        Assert.Equal(Plumix.UI.TextBaseline.Alphabetic, flex.TextBaseline);

        root.Update(new Row(
            crossAxisAlignment: CrossAxisAlignment.Baseline,
            textBaseline: Plumix.UI.TextBaseline.Ideographic,
            children: [new SizedBox(width: 10, height: 10)]));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderFlex>(root.ChildElement);
        Assert.Same(flex, updated);
        Assert.Equal(Plumix.UI.TextBaseline.Ideographic, updated.TextBaseline);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class RegionHitTestRenderBox : RenderBox
    {
        private readonly Size _desiredSize;
        private readonly Rect _hitRegion;
        private readonly double? _baseline;

        public RegionHitTestRenderBox(Size desiredSize, Rect hitRegion, double? baseline = null)
        {
            _desiredSize = desiredSize;
            _hitRegion = hitRegion;
            _baseline = baseline;
        }

        public BoxConstraints LastConstraints { get; private set; }

        protected override void PerformLayout()
        {
            LastConstraints = Constraints;
            Size = Constraints.Constrain(_desiredSize);
        }

        protected override bool HitTestSelf(Point position)
        {
            return _hitRegion.Contains(position);
        }

        protected override double? ComputeDistanceToActualBaseline(Plumix.UI.TextBaseline baseline)
        {
            return _baseline;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class FlowDelegateProbe : FlowDelegate
    {
        public override void PaintChildren(FlowPaintingContext context)
        {
            for (int index = 0; index < context.ChildCount; index++)
            {
                context.PaintChild(index);
            }
        }

        public override bool ShouldRepaint(FlowDelegate oldDelegate) => false;
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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
