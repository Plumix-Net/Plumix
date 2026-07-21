using Avalonia;
using Plumix;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/test/widgets/sliver_resizing_header_test.dart
// flutter/packages/flutter/test/widgets/sliver_floating_header_test.dart

public sealed class SliverHeaderTests
{
    [Fact]
    public void HeaderWidgets_ExposeSourceDefaultsCompositionAndGuards()
    {
        var resizing = new SliverResizingHeader();

        Assert.Null(resizing.MinExtentPrototype);
        Assert.Null(resizing.MaxExtentPrototype);
        Assert.Null(resizing.Child);
        var resizingRenderWidget = Assert.IsType<SliverResizingHeaderRenderObjectWidget>(
            resizing.Build(default));
        Assert.Null(resizingRenderWidget.MinExtentPrototype);
        Assert.Null(resizingRenderWidget.MaxExtentPrototype);
        Assert.IsType<SizedBox>(resizingRenderWidget.Child);

        var minPrototype = new SizedBox(height: 40);
        var maxPrototype = new SizedBox(height: 120);
        var child = new SizedBox(height: 120);
        resizingRenderWidget = Assert.IsType<SliverResizingHeaderRenderObjectWidget>(
            new SliverResizingHeader(minPrototype, maxPrototype, child).Build(default));
        Assert.Same(minPrototype, Assert.IsType<ExcludeFocus>(resizingRenderWidget.MinExtentPrototype).Child);
        Assert.Same(maxPrototype, Assert.IsType<ExcludeFocus>(resizingRenderWidget.MaxExtentPrototype).Child);
        Assert.Same(child, resizingRenderWidget.Child);

        var floating = new SliverFloatingHeader(child);
        Assert.Same(child, floating.Child);
        Assert.Null(floating.AnimationStyle);
        Assert.Null(floating.SnapMode);
        Assert.Throws<ArgumentNullException>(() => new SliverFloatingHeader(null!));
    }

    [Fact]
    public void RenderSliverResizingHeader_ResizesBetweenMeasuredPrototypeExtents()
    {
        var minPrototype = new NaturalSizeBox(new Size(100, 100));
        var maxPrototype = new NaturalSizeBox(new Size(100, 300));
        var child = new NaturalSizeBox(new Size(100, 350));
        var header = new RenderSliverResizingHeader
        {
            MinExtentPrototype = minPrototype,
            MaxExtentPrototype = maxPrototype,
            Child = child
        };

        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 0.0));

        Assert.Equal(new Size(100, 100), minPrototype.Size);
        Assert.Equal(new Size(100, 300), maxPrototype.Size);
        Assert.Equal(new Size(100, 300), child.Size);
        Assert.Equal(300.0, header.Geometry.ScrollExtent);
        Assert.Equal(300.0, header.Geometry.PaintExtent);
        Assert.Equal(100.0, header.Geometry.MaxScrollObstructionExtent);

        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 200.0));

        Assert.Equal(new Size(100, 100), child.Size);
        Assert.Equal(100.0, header.Geometry.PaintExtent);
        Assert.Equal(100.0, header.Geometry.LayoutExtent);
        Assert.Equal(default, ((BoxParentData)child.parentData!).offset);

        var semanticChildren = new List<RenderObject>();
        header.VisitChildrenForSemantics((renderObject, _, _) => semanticChildren.Add(renderObject));
        Assert.Equal([child], semanticChildren);
    }

    [Fact]
    public void RenderSliverResizingHeader_UsesZeroMinimumAndChildMaximumByDefault()
    {
        var child = new NaturalSizeBox(new Size(100, 300));
        var header = new RenderSliverResizingHeader { Child = child };

        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 299.0));

        Assert.Equal(300.0, header.Geometry.ScrollExtent);
        Assert.Equal(0.0, header.Geometry.MaxScrollObstructionExtent);
        Assert.Equal(new Size(100, 1), child.Size);
        Assert.Equal(1.0, header.Geometry.PaintExtent);

        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 300.0));
        Assert.Equal(0.0, child.Size.Height);
        Assert.Equal(0.0, header.Geometry.PaintExtent);
    }

    [Fact]
    public void SliverResizingHeader_ElementOwnsThreeIndependentRenderSlots()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SliverResizingHeader(
            minExtentPrototype: new SizedBox(height: 40),
            maxExtentPrototype: new SizedBox(height: 120),
            child: new SizedBox(height: 200)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var header = Assert.IsType<RenderSliverResizingHeader>(root.ChildElement!.RenderObject);
        Assert.NotNull(header.MinExtentPrototype);
        Assert.NotNull(header.MaxExtentPrototype);
        Assert.NotNull(header.Child);
        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 80.0));
        Assert.Equal(40.0, header.Child!.Size.Height);

        root.Unmount();
    }

    [Fact]
    public void ScrollPosition_ReportsUserDirectionAndScrollingTransitions()
    {
        var position = new ScrollPosition();
        position.ApplyViewportDimension(100.0);
        position.ApplyContentDimensions(0.0, 1000.0);
        position.JumpTo(200.0);
        var values = new List<bool>();
        position.IsScrollingNotifier.AddListener(() => values.Add(position.IsScrollingNotifier.Value));

        position.BeginDrag();
        position.ApplyUserOffset(25.0);

        Assert.Equal(ScrollDirection.Forward, position.UserScrollDirection);
        Assert.Equal(175.0, position.Pixels);
        position.EndDrag(0.0);
        Assert.Equal([true, false], values);

        position.BeginDrag();
        position.ApplyUserOffset(-25.0);
        Assert.Equal(ScrollDirection.Reverse, position.UserScrollDirection);
        position.EndDrag(0.0);

        values.Clear();
        position.ApplyPointerScrollDelta(-25.0);
        Assert.Equal(ScrollDirection.Forward, position.UserScrollDirection);
        Assert.Equal([true, false], values);
        Assert.InRange(Curves.EaseInOut(0.25), 0.1290, 0.1293);
        position.Dispose();
    }

    [Fact]
    public void RenderSliverFloatingHeader_RevealsAndHidesFromUserScrollDirection()
    {
        var child = new NaturalSizeBox(new Size(100, 200));
        var header = new RenderSliverFloatingHeader(child: child);

        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 0.0));
        Assert.Equal(200.0, header.Geometry.PaintExtent);
        Assert.Equal(200.0, header.Geometry.LayoutExtent);

        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 200.0,
            userScrollDirection: ScrollDirection.Reverse));
        Assert.Equal(0.0, header.Geometry.PaintExtent);

        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 175.0,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(25.0, header.Geometry.PaintExtent);
        Assert.Equal(25.0, header.Geometry.LayoutExtent);
        Assert.Equal(new Point(0, -175), ((BoxParentData)child.parentData!).offset);

        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 150.0,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(50.0, header.Geometry.PaintExtent);

        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 175.0,
            userScrollDirection: ScrollDirection.Reverse));
        Assert.Equal(25.0, header.Geometry.PaintExtent);
    }

    [Fact]
    public void RenderSliverFloatingHeader_SnapsWithOverlayOrScrollLayoutExtent()
    {
        var position = new ScrollPosition();
        position.ApplyViewportDimension(100.0);
        position.ApplyContentDimensions(0.0, 1000.0);
        position.JumpTo(200.0);
        position.BeginDrag();
        position.ApplyUserOffset(25.0);
        position.EndDrag(0.0);

        var overlay = CreatePartiallyVisibleFloatingHeader(FloatingHeaderSnapMode.Overlay);
        overlay.IsScrollingUpdate(position);
        overlay.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 175.0,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(200.0, overlay.Geometry.PaintExtent);
        Assert.Equal(25.0, overlay.Geometry.LayoutExtent);

        var scroll = CreatePartiallyVisibleFloatingHeader(FloatingHeaderSnapMode.Scroll);
        scroll.IsScrollingUpdate(position);
        scroll.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 175.0,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(200.0, scroll.Geometry.PaintExtent);
        Assert.Equal(200.0, scroll.Geometry.LayoutExtent);
        position.Dispose();
    }

    private static RenderSliverFloatingHeader CreatePartiallyVisibleFloatingHeader(FloatingHeaderSnapMode snapMode)
    {
        var header = new RenderSliverFloatingHeader(
            animationStyle: AnimationStyle.NoAnimation,
            snapMode: snapMode,
            child: new NaturalSizeBox(new Size(100, 200)));
        header.LayoutWithSliverConstraints(CreateConstraints(scrollOffset: 0.0));
        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 200.0,
            userScrollDirection: ScrollDirection.Reverse));
        header.LayoutWithSliverConstraints(CreateConstraints(
            scrollOffset: 175.0,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(25.0, header.Geometry.PaintExtent);
        return header;
    }

    private static SliverConstraints CreateConstraints(
        double scrollOffset,
        ScrollDirection userScrollDirection = ScrollDirection.Idle)
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: scrollOffset,
            RemainingPaintExtent: 400.0,
            CrossAxisExtent: 100.0,
            ViewportMainAxisExtent: 400.0,
            RemainingCacheExtent: 400.0,
            UserScrollDirection: userScrollDirection);
    }

    private sealed class NaturalSizeBox(Size naturalSize) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(naturalSize);
        }

        public override void Paint(PaintingContext ctx, Point offset)
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
                throw new InvalidOperationException("TestRootElement expects a null slot.");
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
        }
    }
}
