using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/annotated_region.dart
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderAnnotatedRegion)
// - flutter/packages/flutter/lib/src/rendering/layer.dart (AnnotatedRegionLayer)

namespace Plumix.Tests;

public sealed class AnnotatedRegionTests
{
    [Fact]
    public void AnnotatedRegion_ExposesDefaultsGuardsArgumentsAndUpdatesRenderObject()
    {
        var child = new SizedBox(width: 20, height: 10);
        var widget = new AnnotatedRegion<string>(
            value: "first",
            child: child);

        Assert.Equal("first", widget.Value);
        Assert.Same(child, widget.Child);
        Assert.True(widget.Sized);
        Assert.Throws<ArgumentNullException>(() => new AnnotatedRegion<string>(
            value: null!,
            child: child));
        Assert.Throws<ArgumentNullException>(() => new AnnotatedRegion<string>(
            value: "value",
            child: null!));

        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        Mount(root, owner);

        var render = RequireRenderObject<RenderAnnotatedRegion<string>>(root.ChildElement);
        Assert.Equal("first", render.Value);
        Assert.True(render.Sized);

        root.Update(new AnnotatedRegion<string>(
            value: "second",
            sized: false,
            child: new SizedBox(width: 30, height: 15)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderAnnotatedRegion<string>>(root.ChildElement);
        Assert.Same(render, updated);
        Assert.Equal("second", updated.Value);
        Assert.False(updated.Sized);
        root.Unmount();
    }

    [Fact]
    public void AnnotatedRegionLayer_FindsFrontToBackWithExactTypesAndLocalPositions()
    {
        var root = new ContainerLayer();
        var background = new AnnotatedRegionLayer<string>(
            value: "background",
            size: new Size(40, 40),
            offset: new Point(0, 0));
        var foreground = new AnnotatedRegionLayer<string>(
            value: "foreground",
            size: new Size(20, 20),
            offset: new Point(5, 6));
        root.Append(background);
        root.Append(foreground);

        Assert.Equal("foreground", root.Find<string>(new Point(8, 10)));
        Assert.Null(root.Find<object>(new Point(8, 10)));

        AnnotationResult<string> result = root.FindAllAnnotations<string>(new Point(8, 10));
        Assert.Collection(
            result.Entries,
            entry =>
            {
                Assert.Equal("foreground", entry.Annotation);
                Assert.Equal(new Point(3, 4), entry.LocalPosition);
            },
            entry =>
            {
                Assert.Equal("background", entry.Annotation);
                Assert.Equal(new Point(8, 10), entry.LocalPosition);
            });
        Assert.Equal(
            ["foreground", "background"],
            result.Annotations);
    }

    [Fact]
    public void AnnotatedRegionLayer_HonorsNestedSpecificityBoundsAndOpacity()
    {
        var root = new ContainerLayer();
        var behind = new AnnotatedRegionLayer<string>("behind");
        var outer = new AnnotatedRegionLayer<string>(
            value: "outer",
            size: new Size(20, 20),
            offset: new Point(5, 5),
            opaque: true);
        var inner = new AnnotatedRegionLayer<string>(
            value: "inner",
            size: new Size(6, 6),
            offset: new Point(7, 8));
        outer.Append(inner);
        root.Append(behind);
        root.Append(outer);

        AnnotationResult<string> inside = root.FindAllAnnotations<string>(new Point(9, 10));
        Assert.Equal(["inner", "outer"], inside.Annotations);
        Assert.Equal(new Point(2, 2), inside.Entries[0].LocalPosition);
        Assert.Equal(new Point(4, 5), inside.Entries[1].LocalPosition);

        AnnotationResult<string> outside = root.FindAllAnnotations<string>(new Point(30, 30));
        Assert.Equal(["behind"], outside.Annotations);
    }

    [Fact]
    public void AnnotatedRegionLayer_DoesNotClipChildrenForUnrelatedTypesOrApplyOpacityOnMiss()
    {
        var root = new ContainerLayer();
        var background = new AnnotatedRegionLayer<int>(1000);
        var unrelatedParent = new AnnotatedRegionLayer<string>(
            value: "parent",
            size: new Size(0, 0),
            opaque: true);
        unrelatedParent.Append(new AnnotatedRegionLayer<int>(1));
        root.Append(background);
        root.Append(unrelatedParent);

        Assert.Equal(1, root.Find<int>(new Point(5, 5)));
        Assert.Equal([1, 1000], root.FindAllAnnotations<int>(new Point(5, 5)).Annotations);
        Assert.Null(root.Find<string>(new Point(0, 0)));
    }

    [Fact]
    public void LayerFind_TransformsCoordinatesAndRespectsClipLayers()
    {
        var root = new ContainerLayer();
        var offset = new OffsetLayer { Offset = new Point(10, 20) };
        var transform = new TransformLayer
        {
            Transform = Matrix.CreateScale(2, 2),
        };
        var clip = new ClipRectLayer
        {
            ClipRect = new Rect(0, 0, 20, 20),
        };
        var annotation = new AnnotatedRegionLayer<string>(
            value: "transformed",
            size: new Size(5, 5),
            offset: new Point(2, 3));
        root.Append(offset);
        offset.Append(transform);
        transform.Append(clip);
        clip.Append(annotation);

        Point scenePosition = new(16, 28);
        Assert.Equal("transformed", root.Find<string>(scenePosition));
        AnnotationEntry<string> entry = Assert.Single(
            root.FindAllAnnotations<string>(scenePosition).Entries);
        Assert.Equal(new Point(1, 1), entry.LocalPosition);

        Assert.Null(root.Find<string>(new Point(52, 62)));
    }

    [Fact]
    public void RenderAnnotatedRegion_PaintsLayerAndUpdatesSizedLookupBehavior()
    {
        var annotated = new RenderAnnotatedRegion<string>(
            value: "first",
            sized: true,
            child: new PaintTrackingRenderBox());
        var renderView = new RenderView { Child = annotated };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 80));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal("first", pipeline.RootLayer.Find<string>(new Point(20, 20)));
        Assert.Null(pipeline.RootLayer.Find<string>(new Point(120, 20)));
        Assert.IsType<AnnotatedRegionLayer<string>>(Assert.Single(pipeline.RootLayer.Children));

        annotated.Value = "second";
        annotated.Sized = false;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal("second", pipeline.RootLayer.Find<string>(new Point(120, 20)));
        Assert.IsType<AnnotatedRegionLayer<string>>(Assert.Single(pipeline.RootLayer.Children));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class PaintTrackingRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Biggest;
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.DrawRectangle(
                Avalonia.Media.Brushes.CadetBlue,
                pen: null,
                rect: new Rect(offset, Size));
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
                throw new InvalidOperationException("TestRootElement does not move render objects.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
