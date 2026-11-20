using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;
using Plumix.UI;

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

        var owner = TestBuildOwner.Create();
        var root = new TestRootElement(widget);
        Mount(root, owner);

        var render = RequireRenderObject<RenderAnnotatedRegion<string>>(root.ChildElement);
        Assert.Equal("first", render.Value);
        Assert.True(render.Sized);

        root.Update(new AnnotatedRegion<string>(
            value: "second",
            sized: false,
            child: new SizedBox(width: 30, height: 15)));
        owner.PumpFrame();

        var updated = RequireRenderObject<RenderAnnotatedRegion<string>>(root.ChildElement);
        Assert.Same(render, updated);
        Assert.Equal("second", updated.Value);
        Assert.False(updated.Sized);
        root.UnmountRoot();
    }

    [Fact]
    public void AnnotatedRegionLayer_FindsFrontToBackWithExactTypesAndLocalPositions()
    {
        var root = new ContainerLayer();
        var background = new AnnotatedRegionLayer<string>(
            value: "background",
            size: new Size(40, 40),
            offset: new Offset(0, 0));
        var foreground = new AnnotatedRegionLayer<string>(
            value: "foreground",
            size: new Size(20, 20),
            offset: new Offset(5, 6));
        root.Append(background);
        root.Append(foreground);

        Assert.Equal("foreground", root.Find<string>(new Offset(8, 10)));
        Assert.Null(root.Find<object>(new Offset(8, 10)));

        AnnotationResult<string> result = root.FindAllAnnotations<string>(new Offset(8, 10));
        Assert.Collection(
            result.Entries,
            entry =>
            {
                Assert.Equal("foreground", entry.Annotation);
                Assert.Equal(new Offset(3, 4), entry.LocalPosition);
            },
            entry =>
            {
                Assert.Equal("background", entry.Annotation);
                Assert.Equal(new Offset(8, 10), entry.LocalPosition);
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
            offset: new Offset(5, 5),
            opaque: true);
        var inner = new AnnotatedRegionLayer<string>(
            value: "inner",
            size: new Size(6, 6),
            offset: new Offset(7, 8));
        outer.Append(inner);
        root.Append(behind);
        root.Append(outer);

        AnnotationResult<string> inside = root.FindAllAnnotations<string>(new Offset(9, 10));
        Assert.Equal(["inner", "outer"], inside.Annotations);
        Assert.Equal(new Offset(2, 2), inside.Entries[0].LocalPosition);
        Assert.Equal(new Offset(4, 5), inside.Entries[1].LocalPosition);

        AnnotationResult<string> outside = root.FindAllAnnotations<string>(new Offset(30, 30));
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

        Assert.Equal(1, root.Find<int>(new Offset(5, 5)));
        Assert.Equal([1, 1000], root.FindAllAnnotations<int>(new Offset(5, 5)).Annotations);
        Assert.Null(root.Find<string>(new Offset(0, 0)));
    }

    [Fact]
    public void LayerFind_TransformsCoordinatesAndRespectsClipLayers()
    {
        var root = new ContainerLayer();
        var offset = new OffsetLayer { Offset = new Offset(10, 20) };
        var transform = new TransformLayer
        {
            Transform = Matrix4.Diagonal3Values(2, 2, 1.0),
        };
        var clip = new ClipRectLayer
        {
            ClipRect = Rect.FromLTWH(0, 0, 20, 20),
        };
        var annotation = new AnnotatedRegionLayer<string>(
            value: "transformed",
            size: new Size(5, 5),
            offset: new Offset(2, 3));
        root.Append(offset);
        offset.Append(transform);
        transform.Append(clip);
        clip.Append(annotation);

        Offset scenePosition = new(16, 28);
        Assert.Equal("transformed", root.Find<string>(scenePosition));
        AnnotationEntry<string> entry = Assert.Single(
            root.FindAllAnnotations<string>(scenePosition).Entries);
        Assert.Equal(new Offset(1, 1), entry.LocalPosition);

        Assert.Null(root.Find<string>(new Offset(52, 62)));
    }

    [Fact]
    public void RenderAnnotatedRegion_PaintsLayerAndUpdatesSizedLookupBehavior()
    {
        var annotated = new RenderAnnotatedRegion<string>(
            value: "first",
            sized: true,
            child: new PaintTrackingRenderBox());
        var renderView = new RenderView(new FlutterView(new Size(800, 600))) { Child = annotated };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 80));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();

        Assert.Equal("first", pipeline.RootLayer.Find<string>(new Offset(20, 20)));
        Assert.Null(pipeline.RootLayer.Find<string>(new Offset(120, 20)));
        Assert.IsType<AnnotatedRegionLayer<string>>(Assert.Single(pipeline.RootLayer.Children));

        annotated.Value = "second";
        annotated.Sized = false;
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();

        Assert.Equal("second", pipeline.RootLayer.Find<string>(new Offset(120, 20)));
        Assert.IsType<AnnotatedRegionLayer<string>>(Assert.Single(pipeline.RootLayer.Children));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.PumpFrame();
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

        public override void Paint(PaintingContext ctx, Offset offset)
        {
            ctx.Canvas.DrawRectangle(
                Avalonia.Media.Brushes.CadetBlue,
                pen: null,
                rect: (offset & Size));
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
            Owner!.BuildScope(this, () => Rebuild(force: true));
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
