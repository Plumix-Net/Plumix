using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/basic.dart (MetaData, IndexedSemantics)
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderMetaData, RenderIndexedSemantics)

namespace Plumix.Tests;

public sealed class MetaDataIndexedSemanticsTests
{
    [Fact]
    public void MetaData_UsesFlutterDefaultsAndUpdatesItsRenderObject()
    {
        var initial = new MetaData(child: new SizedBox(width: 20, height: 10));
        Assert.Null(initial.Metadata);
        Assert.Equal(HitTestBehavior.DeferToChild, initial.Behavior);

        var owner = new BuildOwner();
        var root = new TestRootElement(initial);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderMetaData = RequireRenderObject<RenderMetaData>(root.ChildElement);
        Assert.Null(renderMetaData.MetaData);
        Assert.Equal(HitTestBehavior.DeferToChild, renderMetaData.Behavior);

        object marker = new object();
        root.Update(new MetaData(
            metaData: marker,
            behavior: HitTestBehavior.Opaque,
            child: new SizedBox(width: 20, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderMetaData>(root.ChildElement);
        Assert.Same(renderMetaData, updated);
        Assert.Same(marker, updated.MetaData);
        Assert.Equal(HitTestBehavior.Opaque, updated.Behavior);
    }

    [Fact]
    public void RenderMetaData_MatchesFlutterHitTestBehaviors()
    {
        var child = new FixedHitTestBox(new Size(40, 24), hitSelf: false);
        var metadata = new RenderMetaData(
            metaData: "probe",
            behavior: HitTestBehavior.DeferToChild,
            child: child);
        metadata.Layout(BoxConstraints.TightFor(width: 40, height: 24));

        var deferred = new BoxHitTestResult();
        Assert.False(metadata.HitTest(deferred, new Point(10, 10)));
        Assert.Empty(deferred.Path);

        metadata.Behavior = HitTestBehavior.Translucent;
        var translucent = new BoxHitTestResult();
        Assert.False(metadata.HitTest(translucent, new Point(10, 10)));
        Assert.Same(metadata, Assert.Single(translucent.Path).Target);

        metadata.Behavior = HitTestBehavior.Opaque;
        var opaque = new BoxHitTestResult();
        Assert.True(metadata.HitTest(opaque, new Point(10, 10)));
        Assert.Same(metadata, Assert.Single(opaque.Path).Target);

        var outside = new BoxHitTestResult();
        Assert.False(metadata.HitTest(outside, new Point(41, 10)));
        Assert.Empty(outside.Path);

        child.HitSelf = true;
        metadata.Behavior = HitTestBehavior.DeferToChild;
        var childHit = new BoxHitTestResult();
        Assert.True(metadata.HitTest(childHit, new Point(10, 10)));
        Assert.Collection(
            childHit.Path,
            entry => Assert.Same(child, entry.Target),
            entry => Assert.Same(metadata, entry.Target));
    }

    [Fact]
    public void IndexedSemantics_UsesAnyIntegerAndUpdatesItsRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new IndexedSemantics(
                index: 3,
                child: new SizedBox(width: 20, height: 10)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var indexed = RequireRenderObject<RenderIndexedSemantics>(root.ChildElement);
        Assert.Equal(3, indexed.Index);

        root.Update(new IndexedSemantics(
            index: -2,
            child: new SizedBox(width: 20, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderIndexedSemantics>(root.ChildElement);
        Assert.Same(indexed, updated);
        Assert.Equal(-2, updated.Index);
    }

    [Fact]
    public void RenderIndexedSemantics_AnnotatesFirstChildNodeAndPreservesItsIdentity()
    {
        var item = new FixedSemanticBox("Item", new Size(40, 20));
        var indexed = new RenderIndexedSemantics(index: 4, child: item);
        var renderView = new RenderView { Child = indexed };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(200, 100));
        pipeline.FlushSemantics();

        var root = Assert.IsType<SemanticsNode>(pipeline.SemanticsOwner.RootNode);
        var first = Assert.Single(root.Children);
        Assert.Equal("Item", first.Label);
        Assert.Equal(4, first.IndexInParent);
        Assert.Empty(first.Children);
        int nodeId = first.Id;

        indexed.Index = 9;
        pipeline.FlushSemantics();

        root = Assert.IsType<SemanticsNode>(pipeline.SemanticsOwner.RootNode);
        var updated = Assert.Single(root.Children);
        Assert.Equal(nodeId, updated.Id);
        Assert.Equal(9, updated.IndexInParent);
        Assert.Contains("indexInParent=9", pipeline.SemanticsOwner.DebugDumpTree());
    }

    [Fact]
    public void RenderIndexedSemantics_DoesNotCreateAStandaloneNodeWithoutSemanticContent()
    {
        var indexed = new RenderIndexedSemantics(
            index: 5,
            child: new FixedHitTestBox(new Size(40, 20), hitSelf: false));
        var renderView = new RenderView { Child = indexed };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(200, 100));
        pipeline.FlushSemantics();

        var root = Assert.IsType<SemanticsNode>(pipeline.SemanticsOwner.RootNode);
        Assert.Empty(root.Children);
        Assert.Null(root.IndexInParent);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class FixedHitTestBox : RenderBox
    {
        private readonly Size _size;

        public FixedHitTestBox(Size size, bool hitSelf)
        {
            _size = size;
            HitSelf = hitSelf;
        }

        public bool HitSelf { get; set; }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return HitSelf;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class FixedSemanticBox : RenderBox
    {
        private readonly string _label;
        private readonly Size _size;

        public FixedSemanticBox(string label, Size size)
        {
            _label = label;
            _size = size;
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
            configuration.IsSemanticBoundary = true;
            configuration.Label = _label;
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
