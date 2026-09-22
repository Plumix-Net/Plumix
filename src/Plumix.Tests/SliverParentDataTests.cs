using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/sliver.dart

namespace Plumix.Tests;

public class SliverParentDataTests
{
    [Fact]
    public void PlainData_HasFlutterDefaults_AndNoBoxOrSiblingState()
    {
        var logical = new SliverLogicalParentData();
        var physical = new SliverPhysicalParentData();

        Assert.Null(logical.LayoutOffset);
        Assert.Equal(default, physical.PaintOffset);
        Assert.Null(physical.CrossAxisFlex);
        Assert.Equal(typeof(ParentData), typeof(SliverPhysicalParentData).BaseType);
        Assert.False(typeof(IContainerParentDataMixin<RenderSliver>).IsAssignableFrom(physical.GetType()));
        Assert.Equal("layoutOffset=None", logical.ToString());
        Assert.Equal("paintOffset=0, 0", physical.ToString());
    }

    [Fact]
    public void ContainerData_InheritsItsPositionAndDiagnosticsFromThePlainBase()
    {
        SliverLogicalParentData logical = new SliverLogicalContainerParentData();
        SliverPhysicalParentData physical = new SliverPhysicalContainerParentData();
        logical.LayoutOffset = 12.5;
        physical.PaintOffset = new Point(3, 7);
        physical.CrossAxisFlex = 2;

        Assert.Equal("layoutOffset=12.5", logical.ToString());
        Assert.Equal("paintOffset=3, 7", physical.ToString());
        Assert.Equal(2, physical.CrossAxisFlex);
        Assert.Equal(physical.PaintOffset, physical.PaintOffset);
        physical.PaintOffset = new Point(9, 11);
        Assert.Equal(new Point(9, 11), physical.PaintOffset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhysicalTransform_PostMultipliesTheStoredOffset(bool container)
    {
        SliverPhysicalParentData data = container
            ? new SliverPhysicalContainerParentData()
            : new SliverPhysicalParentData();
        data.PaintOffset = new Point(3, 7);
        var transform = Matrix4.Diagonal3Values(2, 4, 1);
        data.ApplyPaintTransform(transform);

        Assert.Equal(new Point(6, 28), MatrixUtils.TransformPoint(transform, default));
        Assert.NotEqual(0, transform.Determinant());
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void ContainerDetach_RequiresBothSiblingPointersCleared(bool physical, bool previous, bool next)
    {
        IContainerParentDataMixin<RenderSliver> data = physical
            ? new SliverPhysicalContainerParentData()
            : new SliverLogicalContainerParentData();
        data.previousSibling = previous ? new RenderSliverToBoxAdapter() : null;
        data.nextSibling = next ? new RenderSliverToBoxAdapter() : null;

        if (Constants.KDebugMode && (previous || next))
        {
            AssertionError error = Assert.Throws<AssertionError>(data.Detach);
            Assert.Contains("Pointers to siblings must be nulled", error.Message);
        }
        else
        {
            data.Detach();
        }
    }

    [Fact]
    public void RegularViewport_UsesPhysicalContainerLifecycle()
    {
        CheckContainer<SliverPhysicalContainerParentData>(new RenderViewport(ViewportOffset.Zero()));
    }

    [Fact]
    public void ShrinkWrappingViewport_UsesLogicalContainerLifecycle()
    {
        CheckContainer<SliverLogicalContainerParentData>(new RenderShrinkWrappingViewport(ViewportOffset.Zero()));
    }

    [Fact]
    public void CrossAxisGroup_UsesPhysicalContainerLifecycle()
    {
        CheckContainer<SliverPhysicalContainerParentData>(new RenderSliverCrossAxisGroup());
    }

    [Fact]
    public void MainAxisGroup_UsesPhysicalContainerLifecycle()
    {
        CheckContainer<SliverPhysicalContainerParentData>(new RenderSliverMainAxisGroup());
    }

    [Fact]
    public void CrossAxisSetup_OnlyDefaultsFlexWhenReplacingIncompatibleData()
    {
        var group = new RenderSliverCrossAxisGroup();
        var child = new RenderSliverToBoxAdapter();
        child.parentData = new SliverPhysicalParentData { CrossAxisFlex = 9 };
        group.SetupParentData(child);
        var data = Assert.IsType<SliverPhysicalContainerParentData>(child.parentData);
        Assert.Equal(1, data.CrossAxisFlex);

        data.CrossAxisFlex = null;
        data.PaintOffset = new Point(4, 8);
        group.SetupParentData(child);
        Assert.Same(data, child.parentData);
        Assert.Null(data.CrossAxisFlex);
        Assert.Equal(new Point(4, 8), data.PaintOffset);
    }

    [Fact]
    public void Reparenting_ReplacesPhysicalContainerDataWithLogicalContainerData()
    {
        var physical = new RenderViewport(ViewportOffset.Zero());
        var logical = new RenderShrinkWrappingViewport(ViewportOffset.Zero());
        var child = new RenderSliverToBoxAdapter();
        physical.Add(child);
        var oldData = Assert.IsType<SliverPhysicalContainerParentData>(child.parentData);
        physical.Remove(child);
        logical.Add(child);

        Assert.Null(oldData.previousSibling);
        Assert.Null(oldData.nextSibling);
        Assert.IsType<SliverLogicalContainerParentData>(child.parentData);
        Assert.Same(logical, child.Parent);
        logical.RemoveAll();
    }

    private static RenderViewport HostSliver(RenderSliver sliver)
    {
        var viewport = new RenderViewport(ViewportOffset.Zero());
        viewport.Insert(sliver);
        return viewport;
    }

    private static void CheckContainer<TData>(RenderObject renderObject)
        where TData : class, IContainerParentDataMixin<RenderSliver>
    {
        var container = Assert.IsAssignableFrom<IContainerRenderObjectMixin<RenderSliver, TData>>(renderObject);
        var first = new RenderSliverToBoxAdapter();
        var middle = new RenderSliverToBoxAdapter();
        var last = new RenderSliverToBoxAdapter();
        container.AddAll(null);
        container.AddAll([first, last]);
        container.Insert(middle, after: first);
        Assert.Equal(3, container.ChildCount);
        Assert.Same(first, container.FirstChild);
        Assert.Same(last, container.LastChild);
        Assert.Same(middle, container.ChildAfter(first));
        Assert.Same(middle, container.ChildBefore(last));
        TData data = Assert.IsType<TData>(middle.parentData);

        RenderBox host = renderObject as RenderBox ?? HostSliver((RenderSliver)renderObject);
        var root = new RenderView(new FlutterView(new Size(800, 600))) { Child = host };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        Assert.Same(pipeline, middle.Owner);
        Assert.True(middle.Depth > renderObject.Depth);
        container.Move(middle);
        Assert.Same(data, middle.parentData);
        Assert.Same(middle, container.FirstChild);
        Assert.Same(first, container.ChildAfter(middle));
        Assert.Same(pipeline, middle.Owner);
        container.Move(middle, after: last);
        Assert.Same(middle, container.LastChild);
        Assert.Same(last, container.ChildBefore(middle));

        root.Detach();
        Assert.False(first.Attached);
        Assert.False(middle.Attached);
        Assert.Same(data, middle.parentData);
        Assert.Same(last, data.previousSibling);
        root.Attach(pipeline);
        Assert.Same(pipeline, middle.Owner);

        container.Remove(middle);
        Assert.Null(data.previousSibling);
        Assert.Null(data.nextSibling);
        Assert.Null(middle.parentData);
        Assert.Null(middle.Parent);
        Assert.False(middle.Attached);
        Assert.Equal(2, container.ChildCount);
        container.RemoveAll();
        Assert.Equal(0, container.ChildCount);
        Assert.Null(container.FirstChild);
        Assert.Null(container.LastChild);
        Assert.Null(first.Parent);
        Assert.Null(last.Parent);
        Assert.False(last.Attached);
        if (renderObject is RenderViewport viewport)
        {
            Assert.Null(viewport.Center);
            viewport.Add(middle);
            Assert.Same(middle, viewport.Center);
            viewport.RemoveAll();
        }
        root.Detach();
    }
}
