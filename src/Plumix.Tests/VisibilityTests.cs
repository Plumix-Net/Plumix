using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class VisibilityTests
{
    [Fact]
    public void Visibility_DefaultsFactoriesAndGuardsMatchFlutter()
    {
        var child = new SizedBox(width: 20, height: 30);
        var visibility = new Visibility(child);

        Assert.Same(child, visibility.Child);
        var replacement = Assert.IsType<SizedBox>(visibility.Replacement);
        Assert.Equal(0, replacement.Width);
        Assert.Equal(0, replacement.Height);
        Assert.True(visibility.Visible);
        Assert.False(visibility.MaintainState);
        Assert.False(visibility.MaintainAnimation);
        Assert.False(visibility.MaintainSize);
        Assert.False(visibility.MaintainSemantics);
        Assert.False(visibility.MaintainInteractivity);
        Assert.False(visibility.MaintainFocusability);

        var maintained = Visibility.Maintain(child, visible: false);
        Assert.False(maintained.Visible);
        Assert.True(maintained.MaintainState);
        Assert.True(maintained.MaintainAnimation);
        Assert.True(maintained.MaintainSize);
        Assert.True(maintained.MaintainSemantics);
        Assert.True(maintained.MaintainInteractivity);
        Assert.True(maintained.MaintainFocusability);

        Assert.Throws<ArgumentException>(() => new Visibility(child, maintainAnimation: true));
        Assert.Throws<ArgumentException>(() => new Visibility(
            child,
            maintainState: true,
            maintainSize: true));
        Assert.Throws<ArgumentException>(() => new Visibility(
            child,
            maintainState: true,
            maintainAnimation: true,
            maintainSemantics: true));
        Assert.Throws<ArgumentException>(() => new Visibility(
            child,
            maintainState: true,
            maintainAnimation: true,
            maintainInteractivity: true));
        Assert.Throws<ArgumentException>(() => new Visibility(child, maintainFocusability: true));
    }

    [Fact]
    public void SliverVisibility_DefaultsFactoriesAndGuardsMatchFlutter()
    {
        var sliver = new SliverToBoxAdapter(new SizedBox(width: 20, height: 30));
        var visibility = new SliverVisibility(sliver);

        Assert.Same(sliver, visibility.Sliver);
        Assert.IsType<SliverToBoxAdapter>(visibility.ReplacementSliver);
        Assert.True(visibility.Visible);
        Assert.False(visibility.MaintainState);
        Assert.False(visibility.MaintainAnimation);
        Assert.False(visibility.MaintainSize);
        Assert.False(visibility.MaintainSemantics);
        Assert.False(visibility.MaintainInteractivity);

        var maintained = SliverVisibility.Maintain(sliver, visible: false);
        Assert.False(maintained.Visible);
        Assert.True(maintained.MaintainState);
        Assert.True(maintained.MaintainAnimation);
        Assert.True(maintained.MaintainSize);
        Assert.True(maintained.MaintainSemantics);
        Assert.True(maintained.MaintainInteractivity);

        Assert.Throws<ArgumentException>(() => new SliverVisibility(sliver, maintainAnimation: true));
        Assert.Throws<ArgumentException>(() => new SliverVisibility(
            sliver,
            maintainState: true,
            maintainSize: true));
        Assert.Throws<ArgumentException>(() => new SliverVisibility(
            sliver,
            maintainState: true,
            maintainAnimation: true,
            maintainSemantics: true));
        Assert.Throws<ArgumentException>(() => new SliverVisibility(
            sliver,
            maintainState: true,
            maintainAnimation: true,
            maintainInteractivity: true));
    }

    [Fact]
    public void Visibility_HiddenWithoutMaintainStateReplacesAndDisposesChild()
    {
        int disposals = 0;
        var key = new LabeledGlobalKey<ProbeState>("visibility-probe");
        var owner = new BuildOwner();
        var root = new TestRootElement(new Visibility(
            visible: true,
            child: new ProbeWidget(key, onDispose: () => disposals++)));
        Mount(root, owner);

        ProbeState initialState = Assert.IsType<ProbeState>(key.CurrentState);
        root.Update(new Visibility(
            visible: false,
            child: new ProbeWidget(key, onDispose: () => disposals++)));
        owner.FlushBuild();

        Assert.Equal(1, disposals);
        Assert.Null(key.CurrentState);

        root.Update(new Visibility(
            visible: true,
            child: new ProbeWidget(key, onDispose: () => disposals++)));
        owner.FlushBuild();

        Assert.NotNull(key.CurrentState);
        Assert.NotSame(initialState, key.CurrentState);
        root.Unmount();
    }

    [Fact]
    public void Visibility_HiddenWithMaintainStateRetainsChildAndUsesOffstageTickerAndFocusPolicy()
    {
        int disposals = 0;
        var key = new LabeledGlobalKey<ProbeState>("maintained-visibility-probe");
        var owner = new BuildOwner();
        var root = new TestRootElement(new Visibility(
            visible: true,
            maintainState: true,
            child: new ProbeWidget(key, onDispose: () => disposals++)));
        Mount(root, owner);

        ProbeState initialState = Assert.IsType<ProbeState>(key.CurrentState);
        root.Update(new Visibility(
            visible: false,
            maintainState: true,
            child: new ProbeWidget(key, onDispose: () => disposals++)));
        owner.FlushBuild();

        Assert.Same(initialState, key.CurrentState);
        Assert.Equal(0, disposals);
        var offstage = FindWidget<Offstage>(root);
        var tickerMode = FindWidget<TickerMode>(root);
        var excludeFocus = FindWidget<ExcludeFocus>(root);
        Assert.True(offstage.IsOffstage);
        Assert.False(tickerMode.Enabled);
        Assert.True(excludeFocus.Excluding);

        root.Unmount();
        Assert.Equal(1, disposals);
    }

    [Fact]
    public void Visibility_MaintainSizePreservesLayoutAndAppliesPaintHitTestAndSemanticsPolicies()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Visibility(
            visible: false,
            maintainState: true,
            maintainAnimation: true,
            maintainSize: true,
            maintainSemantics: false,
            maintainInteractivity: false,
            child: new HitTestWidget(new Size(40, 30))));
        Mount(root, owner);

        var visibility = RequireRenderObject<RenderVisibility>(root.ChildElement);
        visibility.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));

        Assert.Equal(new Size(40, 30), visibility.Size);
        Assert.False(visibility.Visible);
        Assert.False(visibility.MaintainSemantics);
        Assert.False(visibility.HitTest(new BoxHitTestResult(), new Point(10, 10)));

        int semanticsVisits = 0;
        visibility.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);

        root.Update(Visibility.Maintain(
            visible: false,
            child: new HitTestWidget(new Size(40, 30))));
        owner.FlushBuild();

        visibility = RequireRenderObject<RenderVisibility>(root.ChildElement);
        visibility.Layout(new BoxConstraints(MaxWidth: 100, MaxHeight: 100));
        Assert.True(visibility.MaintainSemantics);
        Assert.True(visibility.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        visibility.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);
        root.Unmount();
    }

    [Fact]
    public void VisibilityOf_CombinesNestedAncestorVisibility()
    {
        var probe = new VisibilityProbe();
        var owner = new BuildOwner();
        var root = new TestRootElement(new Visibility(
            visible: false,
            maintainState: true,
            child: new Visibility(
                visible: true,
                maintainState: true,
                child: probe)));
        Mount(root, owner);

        Assert.False(probe.LastVisibility);

        root.Update(new Visibility(
            visible: true,
            maintainState: true,
            child: new Visibility(
                visible: true,
                maintainState: true,
                child: probe)));
        owner.FlushBuild();

        Assert.True(probe.LastVisibility);
        root.Unmount();
    }

    [Fact]
    public void RenderVisibility_SuppressesPaintWithoutForcingCompositing()
    {
        var child = new HitTestRenderBox(new Size(40, 30));
        var visibility = new RenderVisibility(visible: false, maintainSemantics: false, child: child);
        var renderView = new RenderView { Child = visibility };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 80));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(0, child.PaintCount);
        Assert.False(visibility.NeedsCompositing);

        visibility.Visible = true;
        pipeline.FlushPaint();

        Assert.Equal(1, child.PaintCount);
        Assert.False(visibility.NeedsCompositing);
    }

    [Fact]
    public void NestedVisibility_CannotRestoreFocusabilityExcludedByHiddenAncestor()
    {
        var probe = new FocusabilityProbe();
        var owner = new BuildOwner();
        var root = new TestRootElement(new Visibility(
            visible: false,
            maintainState: true,
            child: new Visibility(
                visible: true,
                maintainState: true,
                maintainFocusability: true,
                child: probe)));
        Mount(root, owner);

        Assert.False(probe.DescendantsAreFocusable);
        root.Unmount();
    }

    [Fact]
    public void RenderSliverOffstage_LaysOutChildButReportsZeroGeometryAndSuppressesParticipation()
    {
        var box = new HitTestRenderBox(new Size(100, 80));
        var child = new RenderSliverToBoxAdapter(box);
        var offstage = new RenderSliverOffstage(offstage: true, sliver: child);
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        viewport.Insert(offstage);
        var renderView = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(80, child.Geometry.ScrollExtent);
        Assert.Equal(default, offstage.Geometry);
        Assert.Equal(0, box.PaintCount);
        Assert.False(offstage.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        int semanticsVisits = 0;
        offstage.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);

        offstage.Offstage = false;
        pipeline.FlushLayout(new Size(100, 60));
        Assert.Equal(child.Geometry, offstage.Geometry);
        offstage.Paint(new PaintingContext(new OffsetLayer()), new Point(0, 0));
        Assert.Equal(1, box.PaintCount);
        Assert.True(offstage.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        offstage.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);
    }

    [Fact]
    public void RenderSliverIgnorePointer_PreservesGeometryAndControlsHitTestAndSemantics()
    {
        var child = new RenderSliverToBoxAdapter(new HitTestRenderBox(new Size(100, 80)));
        var ignore = new RenderSliverIgnorePointer(ignoring: true, sliver: child);
        ignore.LayoutWithSliverConstraints(CreateSliverConstraints());

        Assert.Equal(child.Geometry, ignore.Geometry);
        Assert.False(ignore.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        int semanticsVisits = 0;
        ignore.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);

        ignore.IgnoringSemantics = true;
        ignore.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);

        ignore.Ignoring = false;
        Assert.True(ignore.HitTest(new BoxHitTestResult(), new Point(10, 10)));
    }

    [Fact]
    public void SliverVisibility_MaintainSizePreservesGeometryWhileControllingPaintHitTestAndSemantics()
    {
        var box = new HitTestRenderBox(new Size(100, 80));
        var child = new RenderSliverToBoxAdapter(box);
        var ignore = new RenderSliverIgnorePointer(ignoring: true, sliver: child);
        var visibility = new RenderSliverVisibility(
            visible: false,
            maintainSemantics: false,
            sliver: ignore);
        var viewport = new RenderViewport(offset: ViewportOffset.Zero());
        viewport.Insert(visibility);
        var renderView = new RenderView { Child = viewport };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        Assert.Equal(child.Geometry, visibility.Geometry);
        Assert.Equal(80, visibility.Geometry.ScrollExtent);
        Assert.Equal(60, visibility.Geometry.PaintExtent);
        Assert.Equal(0, box.PaintCount);
        Assert.False(visibility.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        int semanticsVisits = 0;
        visibility.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);

        visibility.Visible = true;
        visibility.MaintainSemantics = true;
        ignore.Ignoring = false;
        pipeline.FlushPaint();
        Assert.Equal(1, box.PaintCount);
        Assert.True(visibility.HitTest(new BoxHitTestResult(), new Point(10, 10)));
        visibility.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(1, semanticsVisits);
    }

    private static SliverConstraints CreateSliverConstraints()
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 60,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 60,
            RemainingCacheExtent: 60);
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T FindWidget<T>(Element root) where T : Widget
    {
        T? result = null;
        Visit(root);
        return result ?? throw new InvalidOperationException($"Widget {typeof(T).Name} was not found.");

        void Visit(Element element)
        {
            if (result != null)
            {
                return;
            }

            if (element.Widget is T match)
            {
                result = match;
                return;
            }

            element.VisitChildren(Visit);
        }
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class ProbeWidget : StatefulWidget
    {
        private readonly Action _onDispose;

        public ProbeWidget(Key key, Action onDispose) : base(key)
        {
            _onDispose = onDispose;
        }

        public override State CreateState() => new ProbeState(_onDispose);
    }

    private sealed class ProbeState : State
    {
        private readonly Action _onDispose;

        public ProbeState(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public override Widget Build(BuildContext context) => new SizedBox(width: 10, height: 10);

        public override void Dispose()
        {
            _onDispose();
        }
    }

    private sealed class VisibilityProbe : StatelessWidget
    {
        public bool LastVisibility { get; private set; }

        public override Widget Build(BuildContext context)
        {
            LastVisibility = Visibility.Of(context);
            return new SizedBox();
        }
    }

    private sealed class FocusabilityProbe : StatelessWidget
    {
        public bool DescendantsAreFocusable { get; private set; }

        public override Widget Build(BuildContext context)
        {
            FocusNode? enclosing = Focus.MaybeOf(context, scopeOk: true);
            DescendantsAreFocusable = enclosing == null
                || (enclosing.DescendantsAreFocusable
                    && enclosing.Ancestors.All(ancestor => ancestor.DescendantsAreFocusable));
            return new SizedBox();
        }
    }

    private sealed class HitTestWidget : LeafRenderObjectWidget
    {
        private readonly Size _size;

        public HitTestWidget(Size size)
        {
            _size = size;
        }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new HitTestRenderBox(_size);
        }
    }

    private sealed class HitTestRenderBox : RenderBox
    {
        private readonly Size _size;

        public HitTestRenderBox(Size size)
        {
            _size = size;
        }

        public int PaintCount { get; private set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            PaintCount++;
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
                throw new InvalidOperationException("Test root expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("Test root does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("Test root expects a null slot.");
            }
        }
    }
}
