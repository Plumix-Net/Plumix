using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/layout_builder.dart
// flutter/packages/flutter/lib/src/widgets/orientation_builder.dart
// flutter/packages/flutter/lib/src/widgets/sliver_layout_builder.dart

public sealed class LayoutBuilderTests
{
    [Fact]
    public void SliverLayoutBuilder_ExposesSourceContractAndValidatesBuilder()
    {
        SliverLayoutWidgetBuilder builder = (_, _) => new SliverToBoxAdapter();
        var widget = new SliverLayoutBuilder(builder);

        Assert.Same(builder, widget.Builder);
        Assert.Throws<ArgumentNullException>(() => new SliverLayoutBuilder(null!));
    }

    [Fact]
    public void SliverLayoutBuilder_DefersBuildUntilLayoutAndForwardsConstraintsAndGeometry()
    {
        int builderCalls = 0;
        SliverConstraints? receivedConstraints = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverLayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            receivedConstraints = constraints;
            return new SliverToBoxAdapter(new SizedBox(height: 140));
        }));
        Mount(root, owner);

        Assert.Equal(0, builderCalls);
        var renderObject = Assert.IsType<RenderSliverLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 20,
            RemainingPaintExtent: 80,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 80,
            RemainingCacheExtent: 100);

        renderObject.LayoutWithSliverConstraints(constraints);

        Assert.Equal(1, builderCalls);
        Assert.Equal(constraints, receivedConstraints);
        Assert.NotNull(renderObject.Child);
        Assert.Equal(renderObject.Child!.Geometry, renderObject.Geometry);
        Assert.Equal(140, renderObject.Geometry.ScrollExtent);
        Assert.Equal(80, renderObject.Geometry.PaintExtent);
        Assert.Equal(new Size(100, 80), renderObject.Size);
    }

    [Fact]
    public void SliverLayoutBuilder_RebuildsForChangedConstraintsButSkipsEquivalentLayoutInfo()
    {
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverLayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            return new SliverToBoxAdapter(
                new SizedBox(height: Math.Max(1.0, constraints.RemainingPaintExtent)));
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderSliverLayoutBuilder>(root.ChildElement!.RenderObject);
        var firstConstraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 80,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 80,
            RemainingCacheExtent: 80);
        renderObject.LayoutWithSliverConstraints(firstConstraints);
        Assert.Equal(1, builderCalls);

        renderObject.ScheduleLayoutCallback();
        renderObject.LayoutWithSliverConstraints(firstConstraints);
        Assert.Equal(1, builderCalls);

        var secondConstraints = firstConstraints with { RemainingPaintExtent = 120 };
        renderObject.LayoutWithSliverConstraints(secondConstraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal(120, renderObject.Geometry.ScrollExtent);
    }

    [Fact]
    public void SliverLayoutBuilder_WidgetUpdateUsesNewBuilderAtNextLayout()
    {
        int firstBuilderCalls = 0;
        int secondBuilderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverLayoutBuilder((_, _) =>
        {
            firstBuilderCalls++;
            return new SliverToBoxAdapter(new SizedBox(height: 20));
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderSliverLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 100,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 100);
        renderObject.LayoutWithSliverConstraints(constraints);

        root.Update(new SliverLayoutBuilder((_, _) =>
        {
            secondBuilderCalls++;
            return new SliverToBoxAdapter(new SizedBox(height: 40));
        }));

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(0, secondBuilderCalls);
        Assert.Same(renderObject, root.ChildElement!.RenderObject);

        renderObject.LayoutWithSliverConstraints(constraints);

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(1, secondBuilderCalls);
        Assert.Equal(40, renderObject.Geometry.ScrollExtent);
    }

    [Fact]
    public void SliverLayoutBuilder_RebuildsAtLayoutWhenInheritedDependencyChanges()
    {
        int builderCalls = 0;
        var values = new List<int>();
        var layoutBuilder = new SliverLayoutBuilder((context, _) =>
        {
            builderCalls++;
            values.Add(context.DependOnInherited<TestInheritedValue>()!.Value);
            return new SliverToBoxAdapter(new SizedBox(height: 20));
        });
        var owner = new BuildOwner();
        var root = new TestRootElement(new TestInheritedValue(1, layoutBuilder));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderSliverLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 100,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 100);
        renderObject.LayoutWithSliverConstraints(constraints);
        Assert.Equal([1], values);

        root.Update(new TestInheritedValue(2, layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(1, builderCalls);

        renderObject.LayoutWithSliverConstraints(constraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal([1, 2], values);
    }

    [Fact]
    public void LayoutBuilder_ExposesSourceContractAndValidatesBuilder()
    {
        LayoutWidgetBuilder builder = (_, _) => new SizedBox();
        var widget = new LayoutBuilder(builder);

        Assert.Same(builder, widget.Builder);
        Assert.Throws<ArgumentNullException>(() => new LayoutBuilder(null!));
    }

    [Fact]
    public void LayoutBuilder_DefersBuildUntilLayoutAndForwardsConstraints()
    {
        int builderCalls = 0;
        BoxConstraints? receivedConstraints = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            receivedConstraints = constraints;
            return new SizedBox(width: 36, height: 18);
        }));
        Mount(root, owner);

        Assert.Equal(0, builderCalls);
        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 120, MaxHeight: 80);

        renderObject.Layout(constraints);

        Assert.Equal(1, builderCalls);
        Assert.Equal(constraints, receivedConstraints);
        Assert.Equal(new Size(36, 18), renderObject.Size);
        Assert.Equal(new Size(36, 18), renderObject.Child!.Size);
    }

    [Fact]
    public void LayoutBuilder_RebuildsForChangedConstraintsButSkipsEquivalentLayoutInfo()
    {
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, constraints) =>
        {
            builderCalls++;
            return new SizedBox(width: constraints.MaxWidth / 2.0, height: 20);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var firstConstraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 60);
        renderObject.Layout(firstConstraints);
        Assert.Equal(1, builderCalls);
        Assert.Equal(new Size(50, 20), renderObject.Size);

        renderObject.ScheduleLayoutCallback();
        renderObject.Layout(firstConstraints);
        Assert.Equal(1, builderCalls);

        var secondConstraints = new BoxConstraints(MaxWidth: 160, MaxHeight: 60);
        renderObject.Layout(secondConstraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal(new Size(80, 20), renderObject.Size);
    }

    [Fact]
    public void LayoutBuilder_WidgetUpdateUsesNewBuilderAtNextLayout()
    {
        int firstBuilderCalls = 0;
        int secondBuilderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            firstBuilderCalls++;
            return new SizedBox(width: 20, height: 10);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);

        root.Update(new LayoutBuilder((_, _) =>
        {
            secondBuilderCalls++;
            return new SizedBox(width: 40, height: 30);
        }));

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(0, secondBuilderCalls);
        Assert.Same(renderObject, root.ChildElement!.RenderObject);

        renderObject.Layout(constraints);

        Assert.Equal(1, firstBuilderCalls);
        Assert.Equal(1, secondBuilderCalls);
        Assert.Equal(new Size(40, 30), renderObject.Size);
    }

    [Fact]
    public void LayoutBuilder_MarkNeedsBuildRebuildsWithLastConstraintsDuringNextLayout()
    {
        int builderCalls = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            builderCalls++;
            return new SizedBox(width: 20, height: 10);
        }));
        Mount(root, owner);

        var element = Assert.IsType<LayoutBuilderElement>(root.ChildElement);
        var renderObject = Assert.IsType<RenderLayoutBuilder>(element.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);

        element.MarkNeedsBuild();
        owner.FlushBuild();
        Assert.Equal(1, builderCalls);

        renderObject.Layout(constraints);
        Assert.Equal(2, builderCalls);
    }

    [Fact]
    public void LayoutBuilder_RebuildsAtLayoutWhenInheritedDependencyChanges()
    {
        int builderCalls = 0;
        var values = new List<int>();
        var layoutBuilder = new LayoutBuilder((context, _) =>
        {
            builderCalls++;
            values.Add(context.DependOnInherited<TestInheritedValue>()!.Value);
            return new SizedBox(width: 20, height: 10);
        });
        var owner = new BuildOwner();
        var root = new TestRootElement(new TestInheritedValue(1, layoutBuilder));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 100);
        renderObject.Layout(constraints);
        Assert.Equal([1], values);

        root.Update(new TestInheritedValue(2, layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(1, builderCalls);

        renderObject.Layout(constraints);
        Assert.Equal(2, builderCalls);
        Assert.Equal([1, 2], values);
    }

    [Theory]
    [InlineData(120, 80, Orientation.Landscape)]
    [InlineData(80, 120, Orientation.Portrait)]
    [InlineData(100, 100, Orientation.Portrait)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, Orientation.Portrait)]
    public void OrientationBuilder_UsesConstraintOrientation(
        double maxWidth,
        double maxHeight,
        Orientation expected)
    {
        Orientation? received = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new OrientationBuilder((_, orientation) =>
        {
            received = orientation;
            return new SizedBox(width: 10, height: 10);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        renderObject.Layout(new BoxConstraints(MaxWidth: maxWidth, MaxHeight: maxHeight));

        Assert.Equal(expected, received);
        Assert.Equal(new Size(10, 10), renderObject.Size);
    }

    [Fact]
    public void OrientationBuilder_ExposesSourceContractAndValidatesBuilder()
    {
        OrientationWidgetBuilder builder = (_, _) => new SizedBox();
        var widget = new OrientationBuilder(builder);

        Assert.Same(builder, widget.Builder);
        Assert.Throws<ArgumentNullException>(() => new OrientationBuilder(null!));
    }

    [Fact]
    public void LayoutBuilder_RunsScheduledCallbackWhenAnAncestorSkipsLayingOutTheSubtree()
    {
        int builds = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SkipLayoutHost(
            new LayoutBuilder((_, _) =>
            {
                builds++;
                return new SizedBox(width: 10, height: 10);
            })));
        Mount(root, owner);

        var host = Assert.IsType<RenderSkipLayoutHost>(root.ChildElement!.RenderObject);
        var renderView = new RenderView { Child = host };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(new Size(200, 100));
        Assert.Equal(1, builds);

        // The layout builder is not a relayout boundary here: the host passes loose constraints and
        // uses the child's size.
        RenderLayoutBuilder layoutBuilder = FindRenderObject<RenderLayoutBuilder>(renderView);
        Assert.False(layoutBuilder.IsRelayoutBoundary);

        host.SkipChildLayout = true;
        pipeline.FlushLayout(new Size(200, 100));
        Assert.Equal(1, builds);

        // Flutter's `RenderObjectWithLayoutCallbackMixin.scheduleLayoutCallback` registers the node with
        // the pipeline owner itself, so the callback still runs when the ancestor declines to lay this
        // subtree out - which is what keeps global keys in the deferred subtree unique.
        FindElement<LayoutBuilderElement>(root).MarkNeedsBuild();
        owner.FlushBuild();
        pipeline.FlushLayout(new Size(200, 100));
        Assert.Equal(2, builds);
    }

    [Fact]
    public void SliverLayoutBuilder_RunsScheduledCallbackWhenAnAncestorSkipsLayingOutTheSubtree()
    {
        int builds = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new SkipLayoutHost(
            new SliverLayoutBuilder((_, _) =>
            {
                builds++;
                return new SliverToBoxAdapter(new SizedBox(height: 10));
            })));
        Mount(root, owner);

        var host = Assert.IsType<RenderSkipLayoutHost>(root.ChildElement!.RenderObject);
        var renderView = new RenderView { Child = host };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        RenderSliverLayoutBuilder layoutBuilder = FindRenderObject<RenderSliverLayoutBuilder>(renderView);
        layoutBuilder.LayoutWithSliverConstraints(new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 100,
            CrossAxisExtent: 200,
            ViewportMainAxisExtent: 100,
            RemainingCacheExtent: 100));
        Assert.Equal(1, builds);

        host.SkipChildLayout = true;
        FindElement<SliverLayoutBuilderElement>(root).MarkNeedsBuild();
        owner.FlushBuild();
        pipeline.FlushLayout(new Size(200, 100));
        Assert.Equal(2, builds);
    }

    [Fact]
    public void LayoutBuilder_RepeatedScheduleLayoutCallbackBeforeLayoutIsANoOp()
    {
        int builds = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            builds++;
            return new SizedBox(width: 10, height: 10);
        }));
        Mount(root, owner);

        var renderObject = Assert.IsType<RenderLayoutBuilder>(root.ChildElement!.RenderObject);
        renderObject.Layout(BoxConstraints.Loose(new Size(200, 100)));
        Assert.Equal(1, builds);

        LayoutBuilderElement element = FindElement<LayoutBuilderElement>(root);
        element.MarkNeedsBuild();
        element.MarkNeedsBuild();
        element.MarkNeedsBuild();
        Assert.True(renderObject.NeedsLayout);

        renderObject.Layout(BoxConstraints.Loose(new Size(200, 100)));
        Assert.Equal(2, builds);
    }

    private static T FindElement<T>(Element root) where T : Element
    {
        T? result = null;
        Visit(root);
        return Assert.IsType<T>(result);

        void Visit(Element element)
        {
            if (result is not null)
            {
                return;
            }

            if (element is T typed)
            {
                result = typed;
                return;
            }

            element.VisitChildren(Visit);
        }
    }

    private static T FindRenderObject<T>(RenderObject root) where T : RenderObject
    {
        T? result = null;
        Visit(root);
        return Assert.IsType<T>(result);

        void Visit(RenderObject renderObject)
        {
            if (result is not null)
            {
                return;
            }

            if (renderObject is T typed)
            {
                result = typed;
                return;
            }

            renderObject.VisitChildren(Visit);
        }
    }

    /// <summary>
    /// A parent that stops laying its child out, standing in for Flutter's obstructed
    /// <c>OverlayEntry</c> with <c>maintainState: true</c>.
    /// </summary>
    private sealed class SkipLayoutHost : SingleChildRenderObjectWidget
    {
        public SkipLayoutHost(Widget child) : base(child)
        {
        }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderSkipLayoutHost();
        }
    }

    private sealed class RenderSkipLayoutHost : RenderProxyBox
    {
        private bool _skipChildLayout;

        public bool SkipChildLayout
        {
            get => _skipChildLayout;
            set
            {
                if (_skipChildLayout == value)
                {
                    return;
                }

                _skipChildLayout = value;
                MarkNeedsLayout();
            }
        }

        protected override void PerformLayout()
        {
            BoxConstraints constraints = Constraints;
            if (Child is null)
            {
                Size = constraints.Smallest;
                return;
            }

            if (_skipChildLayout)
            {
                Size = constraints.Constrain(Child.HasSize ? Child.Size : new Size());
                return;
            }

            Child.Layout(BoxConstraints.Loose(constraints.Biggest), parentUsesSize: true);
            Size = constraints.Constrain(Child.Size);
        }
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestInheritedValue : InheritedWidget
    {
        private readonly Widget _child;

        public TestInheritedValue(int value, Widget child)
        {
            Value = value;
            _child = child;
        }

        public int Value { get; }

        public override Widget Build(BuildContext context) => _child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
            Value != ((TestInheritedValue)oldWidget).Value;
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support child moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
