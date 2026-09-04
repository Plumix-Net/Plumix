using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/rendering/object.dart
// - flutter/packages/flutter/lib/src/rendering/layer.dart

namespace Plumix.Tests;

public sealed class RenderObjectLifecycleTests
{
    [Fact]
    public void RenderObject_Dispose_ReleasesItsLayerAndMarksItDisposed()
    {
        var renderObject = new TestRenderBox();
        var layer = new OffsetLayer();
        renderObject._layer = layer;

        Assert.False(renderObject.DebugDisposed);
        Assert.Same(layer, renderObject.DebugLayer);
        Assert.Equal(1, layer.DebugHandleCount);

        renderObject.Dispose();

        Assert.True(renderObject.DebugDisposed);
        Assert.Null(renderObject.DebugLayer);
        Assert.True(layer.DebugDisposed);
        Assert.Contains("DISPOSED", renderObject.ToStringShort(), StringComparison.Ordinal);
        Assert.Throws<AssertionError>(renderObject.Dispose);
    }

    [Fact]
    public void RenderObject_Detach_RetainsItsLayer()
    {
        var renderObject = new TestRenderBox();
        var root = new RenderView();
        var owner = new PipelineOwner(root);
        var layer = new OffsetLayer();
        renderObject._layer = layer;

        renderObject.Attach(owner);
        renderObject.Detach();

        Assert.Same(layer, renderObject.DebugLayer);
        Assert.False(layer.DebugDisposed);

        renderObject.Dispose();
    }

    [DebugOnlyFact]
    public void RenderObject_MutationAfterDispose_ThrowsFlutterError()
    {
        var renderObject = new TestRenderBox();
        renderObject.Dispose();

        FlutterError error = Assert.Throws<FlutterError>(renderObject.MarkNeedsLayout);

        Assert.Contains("A disposed RenderObject was mutated.", error.Message, StringComparison.Ordinal);
        Assert.Contains("The disposed RenderObject was:", error.Message, StringComparison.Ordinal);
        Assert.Contains(renderObject.ToStringShort(), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LayerHandle_ReplacesAndDisposesTheLastReferencedLayer()
    {
        var first = new PictureLayer();
        var second = new PictureLayer();
        var handle = new LayerHandle<PictureLayer>(first);

        Assert.Equal(1, first.DebugHandleCount);
        handle.Layer = first;
        Assert.Equal(1, first.DebugHandleCount);

        handle.Layer = second;

        Assert.True(first.DebugDisposed);
        Assert.Equal(1, second.DebugHandleCount);

        handle.Layer = null;

        Assert.True(second.DebugDisposed);
        Assert.Equal("LayerHandle(DISPOSED)", handle.ToString());
        Assert.Throws<AssertionError>(() => handle.Layer = first);
    }

    [Fact]
    public void ContainerLayer_AppendAndRemove_OwnsAndAttachesItsChild()
    {
        object owner = new();
        var parent = new ContainerLayer();
        var child = new PictureLayer();
        parent.Attach(owner);

        parent.Append(child);

        Assert.Same(parent, child.Parent);
        Assert.Same(owner, child.Owner);
        Assert.True(child.Attached);
        Assert.Equal(1, child.DebugHandleCount);

        parent.Remove(child);

        Assert.Null(child.Parent);
        Assert.Null(child.Owner);
        Assert.False(child.Attached);
        Assert.True(child.DebugDisposed);
    }

    [Fact]
    public void ContainerLayer_Remove_PreservesAChildHeldByAnotherHandle()
    {
        var parent = new ContainerLayer();
        var child = new PictureLayer();
        var retained = new LayerHandle<PictureLayer>(child);
        parent.Append(child);

        Assert.Equal(2, child.DebugHandleCount);

        parent.Remove(child);

        Assert.False(child.DebugDisposed);
        Assert.Equal(1, child.DebugHandleCount);
        Assert.Null(child.Parent);

        retained.Layer = null;
        Assert.True(child.DebugDisposed);
    }

    [Fact]
    public void Layer_Dispose_ReleasesItsEngineLayerAndReportsDiagnostics()
    {
        object owner = new();
        var engineLayer = new TestEngineLayer();
        var layer = new PictureLayer
        {
            DebugCreator = "creator",
            EngineLayer = engineLayer,
        };
        var handle = new LayerHandle<PictureLayer>(layer);

        layer.Attach(owner);

        // `toStringDeep` and the `DETACHED` marker come from `debugFillProperties`/`toStringShort`,
        // which Dart strips outside a debug build; the dispose contract below holds in every build.
        if (Constants.KDebugMode)
        {
            string dump = layer.ToStringDeep(minLevel: DiagnosticLevel.Debug);
            Assert.Contains("owner: System.Object", dump, StringComparison.Ordinal);
            Assert.Contains("creator: creator", dump, StringComparison.Ordinal);
            Assert.Contains("engine layer: TestEngineLayer", dump, StringComparison.Ordinal);
            Assert.Contains("handles: 1", dump, StringComparison.Ordinal);
            Assert.DoesNotContain("DETACHED", layer.ToStringShort(), StringComparison.Ordinal);
        }

        layer.Detach();
        handle.Layer = null;

        Assert.True(layer.DebugDisposed);
        Assert.True(engineLayer.Disposed);
        Assert.Throws<AssertionError>(layer.Dispose);
    }

    [Fact]
    public void RenderObjectElement_Unmount_DisposesItsRenderObject()
    {
        var widget = new TrackingRenderObjectWidget();
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        TestRenderBox renderObject = Assert.IsType<TestRenderBox>(widget.CreatedRenderObject);
        Assert.False(renderObject.DebugDisposed);

        root.Unmount();

        Assert.True(renderObject.DebugDisposed);
        Assert.True(widget.DidUnmountCalled);
        Assert.False(widget.WasDisposedDuringDidUnmount);
    }

    [Fact]
    public void RenderObjectElement_Deactivation_KeepsTheDetachedRenderTreeIntact()
    {
        var innerWidget = new TrackingProxyRenderObjectWidget();
        var outerWidget = new TrackingProxyRenderObjectWidget(innerWidget);
        var owner = new BuildOwner();
        var root = new TestRootElement(outerWidget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        TrackingRenderBox outer = Assert.IsType<TrackingRenderBox>(outerWidget.CreatedRenderObject);
        TrackingRenderBox inner = Assert.IsType<TrackingRenderBox>(innerWidget.CreatedRenderObject);
        Assert.True(outer.Attached);
        Assert.True(inner.Attached);
        Assert.Same(outer, inner.Parent);

        root.Update(new SizedBox(width: 1, height: 1));

        Assert.False(outer.Attached);
        Assert.False(inner.Attached);
        Assert.Same(outer, inner.Parent);
        Assert.Same(inner, outer.Child);

        owner.FlushBuild();
        Assert.True(outer.DebugDisposed);
        Assert.True(inner.DebugDisposed);
    }

    private sealed class TestRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Smallest;
        }

        public override void Paint(PaintingContext ctx, Avalonia.Point offset)
        {
        }
    }

    private sealed class TestEngineLayer : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public override string ToString() => nameof(TestEngineLayer);
    }

    private sealed class TrackingRenderObjectWidget : LeafRenderObjectWidget
    {
        public RenderObject? CreatedRenderObject { get; private set; }
        public bool DidUnmountCalled { get; private set; }
        public bool WasDisposedDuringDidUnmount { get; private set; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            CreatedRenderObject = new TestRenderBox();
            return CreatedRenderObject;
        }

        public override void DidUnmountRenderObject(RenderObject renderObject)
        {
            DidUnmountCalled = true;
            WasDisposedDuringDidUnmount = renderObject.DebugDisposed;
        }
    }

    private sealed class TrackingProxyRenderObjectWidget : SingleChildRenderObjectWidget
    {
        public TrackingProxyRenderObjectWidget(Widget? child = null) : base(child)
        {
        }

        public RenderObject? CreatedRenderObject { get; private set; }

        public override RenderObject CreateRenderObject(BuildContext context)
        {
            CreatedRenderObject = new TrackingRenderBox();
            return CreatedRenderObject;
        }
    }

    private sealed class TrackingRenderBox : RenderProxyBox
    {
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;
        private readonly RenderView _renderView = new();

        public TestRootElement(Widget childWidget) : base(childWidget)
        {
            var pipelineOwner = new PipelineOwner(_renderView);
            pipelineOwner.Attach(_renderView);
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, null);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(child, _child))
            {
                _child = null;
            }
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }

            _renderView.Child = (RenderBox)child;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child))
            {
                _renderView.Child = null;
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
    }
}
