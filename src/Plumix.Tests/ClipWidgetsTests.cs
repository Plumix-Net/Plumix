using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Path = Plumix.UI.Path;

namespace Plumix.Tests;

public sealed class ClipWidgetsTests
{
    [Fact]
    public void ClipOvalAndClipPath_ExposeFlutterDefaultsAndUpdateRenderObjects()
    {
        var ovalClipper = new FixedOvalClipper(new Rect(2, 4, 24, 16));
        var pathClipper = new TriangleClipper();
        var oval = new ClipOval(child: new SizedBox(width: 40, height: 30));
        var path = new ClipPath(child: new SizedBox(width: 40, height: 30));

        Assert.Null(oval.Clipper);
        Assert.Equal(Clip.AntiAlias, oval.ClipBehavior);
        Assert.Null(path.Clipper);
        Assert.Equal(Clip.AntiAlias, path.ClipBehavior);

        var owner = new BuildOwner();
        var root = new TestRootElement(new ClipOval(
            clipper: ovalClipper,
            child: new SizedBox(width: 40, height: 30)));
        Mount(root, owner);

        var renderOval = RequireRenderObject<RenderClipOval>(root.ChildElement);
        Assert.Same(ovalClipper, renderOval.Clipper);
        Assert.Equal(Clip.AntiAlias, renderOval.ClipBehavior);

        root.Update(new ClipOval(
            clipBehavior: Clip.None,
            child: new SizedBox(width: 40, height: 30)));
        owner.FlushBuild();

        var updatedOval = RequireRenderObject<RenderClipOval>(root.ChildElement);
        Assert.Same(renderOval, updatedOval);
        Assert.Null(updatedOval.Clipper);
        Assert.Equal(Clip.None, updatedOval.ClipBehavior);

        root.Update(new ClipPath(
            clipper: pathClipper,
            clipBehavior: Clip.HardEdge,
            child: new SizedBox(width: 40, height: 30)));
        owner.FlushBuild();

        var renderPath = RequireRenderObject<RenderClipPath>(root.ChildElement);
        Assert.NotSame(renderOval, renderPath);
        Assert.Same(pathClipper, renderPath.Clipper);
        Assert.Equal(Clip.HardEdge, renderPath.ClipBehavior);

        root.Unmount();
    }

    [Fact]
    public void ClipPathShape_UsesShapeBorderClipperAndAmbientDirection()
    {
        var shape = ShapeBorder.Circle(new BorderSide(Colors.Red, 2));
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            TextDirection.Rtl,
            ClipPath.Shape(
                shape,
                clipBehavior: Clip.HardEdge,
                child: new SizedBox(width: 60, height: 40))));
        Mount(root, owner);

        var renderPath = RequireRenderObject<RenderClipPath>(root.ChildElement);
        var shapeClipper = Assert.IsType<ShapeBorderClipper>(renderPath.Clipper);
        Assert.Equal(shape, shapeClipper.Shape);
        Assert.Equal(TextDirection.Rtl, shapeClipper.TextDirection);
        Assert.True(shapeClipper.GetClip(new Size(60, 40)).Contains(new Point(30, 20)));
        Assert.False(shapeClipper.GetClip(new Size(60, 40)).Contains(new Point(1, 1)));
        Assert.Equal(Clip.HardEdge, renderPath.ClipBehavior);

        var rounded = new ShapeBorderClipper(ShapeBorder.RoundedRectangle(12));
        Assert.True(rounded.GetClip(new Size(60, 40)).Contains(new Point(30, 20)));
        Assert.False(rounded.GetClip(new Size(60, 40)).Contains(new Point(1, 1)));
        Assert.False(rounded.ShouldReclip(new ShapeBorderClipper(ShapeBorder.RoundedRectangle(12))));
        Assert.True(rounded.ShouldReclip(new ShapeBorderClipper(ShapeBorder.RoundedRectangle(8))));

        root.Unmount();
    }

    [Fact]
    public void RenderClipOval_HitTestingUsesExactEllipseEvenWhenPaintClipIsDisabled()
    {
        var child = new HitTestBox(new Size(100, 60));
        var oval = new RenderClipOval(
            child: child,
            clipBehavior: Clip.None);
        var pipeline = BuildPipeline(oval, new Size(100, 60));

        Assert.True(oval.HitTest(new BoxHitTestResult(), new Point(50, 30)));
        Assert.True(oval.HitTest(new BoxHitTestResult(), new Point(95, 30)));
        Assert.False(oval.HitTest(new BoxHitTestResult(), new Point(5, 5)));
        Assert.Null(oval.InvokeDescribeApproximatePaintClip(child));

        oval.ClipBehavior = Clip.AntiAlias;
        Assert.Equal(new Rect(0, 0, 100, 60), oval.InvokeDescribeApproximatePaintClip(child));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
    }

    [Fact]
    public void RenderClipPath_CustomGeometryDrivesHitTestingAndApproximateSemanticsClip()
    {
        var clipper = new TriangleClipper(new Rect(0, 0, 70, 50));
        var child = new HitTestBox(new Size(100, 60));
        var path = new RenderClipPath(
            child: child,
            clipper: clipper);
        _ = BuildPipeline(path, new Size(100, 60));

        Assert.True(path.HitTest(new BoxHitTestResult(), new Point(50, 20)));
        Assert.False(path.HitTest(new BoxHitTestResult(), new Point(5, 50)));
        Assert.Equal(new Rect(0, 0, 70, 50), path.InvokeDescribeApproximatePaintClip(child));
    }

    [Fact]
    public void Path_ContainsShapesAndHonorsNonZeroAndEvenOddFillRules()
    {
        var path = new Path();
        path.AddRect(new Rect(0, 0, 80, 60));
        path.AddRect(new Rect(20, 15, 40, 30));

        Assert.True(path.Contains(new Point(40, 30)));

        path.FillType = PathFillType.EvenOdd;
        Assert.False(path.Contains(new Point(40, 30)));
        Assert.True(path.Contains(new Point(10, 10)));

        var oval = new Path();
        oval.AddOval(new Rect(0, 0, 80, 40));
        Assert.True(oval.Contains(new Point(40, 20)));
        Assert.False(oval.Contains(new Point(2, 2)));

        var curved = new Path();
        curved.MoveTo(0, 40);
        curved.QuadraticBezierTo(40, -10, 80, 40);
        curved.LineTo(80, 60);
        curved.LineTo(0, 60);
        Assert.True(curved.Contains(new Point(40, 30)));
    }

    [Fact]
    public void CustomClipperReclip_InvalidatesCachedClipAndUnsubscribesOnDetach()
    {
        var reclip = new TrackingListenable();
        var clipper = new ListeningOvalClipper(reclip);
        var child = new HitTestBox(new Size(80, 50));
        var oval = new RenderClipOval(child: child, clipper: clipper);
        var root = new RenderView { Child = oval };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(80, 50));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(1, reclip.ListenerCount);
        Assert.True(oval.HitTest(new BoxHitTestResult(), new Point(40, 25)));
        Assert.True(oval.HitTest(new BoxHitTestResult(), new Point(40, 25)));
        Assert.Equal(1, clipper.GetClipCalls);

        reclip.Notify();
        Assert.True(oval.NeedsPaint);
        Assert.True(oval.HitTest(new BoxHitTestResult(), new Point(40, 25)));
        Assert.Equal(2, clipper.GetClipCalls);

        root.Child = null;
        Assert.Equal(0, reclip.ListenerCount);
    }

    [Theory]
    [InlineData(Clip.HardEdge)]
    [InlineData(Clip.AntiAlias)]
    [InlineData(Clip.AntiAliasWithSaveLayer)]
    public void RenderClipPath_PaintsGeometryLayerWithClipQualityAndOffset(Clip clipBehavior)
    {
        var clip = new RenderClipPath(
            child: new PaintBox(),
            clipper: new RectPathClipper(),
            clipBehavior: clipBehavior);
        var padding = new RenderPadding(new Thickness(12, 8, 0, 0))
        {
            Child = clip,
        };
        var root = new RenderView { Child = padding };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        var layer = Assert.IsType<ClipGeometryLayer>(Assert.Single(pipeline.RootLayer.Children));
        Assert.Equal(new Point(12, 8), layer.GeometryOffset);
        Assert.Equal(clipBehavior, layer.ClipBehavior);
        Assert.IsType<PictureLayer>(Assert.Single(layer.Children));
    }

    [Fact]
    public void RenderClipPath_NonePaintsChildWithoutGeometryLayer()
    {
        var clip = new RenderClipPath(
            child: new PaintBox(),
            clipper: new TriangleClipper(),
            clipBehavior: Clip.None);
        var pipeline = BuildPipeline(clip, new Size(100, 60));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.IsType<PictureLayer>(Assert.Single(pipeline.RootLayer.Children));
    }

    [Fact]
    public void PhysicalShape_ExposesFlutterDefaultsAndUpdatesExistingRenderObject()
    {
        var initialClipper = new TriangleClipper();
        var updatedClipper = new RectPathClipper();
        var widget = new PhysicalShape(
            clipper: initialClipper,
            color: Colors.Orange);

        Assert.Same(initialClipper, widget.Clipper);
        Assert.Equal(Clip.None, widget.ClipBehavior);
        Assert.Equal(0.0, widget.Elevation);
        Assert.Equal(Colors.Orange, widget.Color);
        Assert.Equal(Colors.Black, widget.ShadowColor);
        Assert.Null(widget.Child);
        Assert.Throws<ArgumentNullException>(() => new PhysicalShape(null!, Colors.Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalShape(
            initialClipper,
            Colors.Red,
            elevation: -1.0));

        var owner = new BuildOwner();
        var root = new TestRootElement(new PhysicalShape(
            clipper: initialClipper,
            color: Colors.Orange,
            child: new SizedBox(width: 100.0, height: 60.0)));
        Mount(root, owner);

        var renderObject = RequireRenderObject<RenderPhysicalShape>(root.ChildElement);
        Assert.Same(initialClipper, renderObject.Clipper);
        Assert.Equal(Clip.None, renderObject.ClipBehavior);

        root.Update(new PhysicalShape(
            clipper: updatedClipper,
            color: Colors.Blue,
            child: new SizedBox(width: 100.0, height: 60.0),
            clipBehavior: Clip.AntiAlias,
            elevation: 6.0,
            shadowColor: Colors.Purple));
        owner.FlushBuild();

        var updatedRenderObject = RequireRenderObject<RenderPhysicalShape>(root.ChildElement);
        Assert.Same(renderObject, updatedRenderObject);
        Assert.Same(updatedClipper, updatedRenderObject.Clipper);
        Assert.Equal(Clip.AntiAlias, updatedRenderObject.ClipBehavior);
        Assert.Equal(6.0, updatedRenderObject.Elevation);
        Assert.Equal(Colors.Blue, updatedRenderObject.Color);
        Assert.Equal(Colors.Purple, updatedRenderObject.ShadowColor);

        root.Unmount();
    }

    [Fact]
    public void RenderPhysicalShape_UsesPathForHitTestingSurfaceShadowAndClip()
    {
        var clipRect = new Rect(10.0, 5.0, 80.0, 50.0);
        var physicalShape = new RenderPhysicalShape(
            clipper: new FixedRectPathClipper(clipRect),
            color: Colors.Orange,
            child: new PaintBox(),
            clipBehavior: Clip.AntiAlias,
            elevation: 5.0,
            shadowColor: Colors.Black);
        var pipeline = BuildPipeline(physicalShape, new Size(100.0, 60.0));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(physicalShape.HitTest(new BoxHitTestResult(), new Point(50.0, 20.0)));
        Assert.False(physicalShape.HitTest(new BoxHitTestResult(), new Point(5.0, 50.0)));
        Assert.Equal(clipRect, physicalShape.InvokeDescribeApproximatePaintClip(physicalShape.Child));

        Assert.Equal(2, pipeline.RootLayer.Children.Count);
        Assert.IsType<PictureLayer>(pipeline.RootLayer.Children[0]);
        var clipLayer = Assert.IsType<ClipGeometryLayer>(pipeline.RootLayer.Children[1]);
        Assert.Equal(Clip.AntiAlias, clipLayer.ClipBehavior);
        Assert.Equal(clipRect, Assert.IsType<RectangleGeometry>(clipLayer.Geometry).Rect);
        Assert.IsType<PictureLayer>(Assert.Single(clipLayer.Children));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static PipelineOwner BuildPipeline(RenderBox child, Size size)
    {
        var root = new RenderView { Child = child };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(size);
        return pipeline;
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class FixedOvalClipper : CustomClipper<Rect>
    {
        public FixedOvalClipper(Rect rect)
        {
            Rect = rect;
        }

        public Rect Rect { get; }

        public override Rect GetClip(Size size) => Rect;

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper)
        {
            return oldClipper is not FixedOvalClipper old || old.Rect != Rect;
        }
    }

    private sealed class TriangleClipper : CustomClipper<Path>
    {
        private readonly Rect _approximateClip;

        public TriangleClipper(Rect? approximateClip = null)
        {
            _approximateClip = approximateClip ?? new Rect(0, 0, 100, 60);
        }

        public override Path GetClip(Size size)
        {
            var path = new Path();
            path.MoveTo(size.Width / 2.0, 0);
            path.LineTo(size.Width, size.Height);
            path.LineTo(0, size.Height);
            path.Close();
            return path;
        }

        public override Rect GetApproximateClipRect(Size size) => _approximateClip;

        public override bool ShouldReclip(CustomClipper<Path> oldClipper)
        {
            return oldClipper is not TriangleClipper old || old._approximateClip != _approximateClip;
        }
    }

    private sealed class RectPathClipper : CustomClipper<Path>
    {
        public override Path GetClip(Size size)
        {
            var path = new Path();
            path.AddRect(new Rect(new Point(0, 0), size));
            return path;
        }

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) => false;
    }

    private sealed class FixedRectPathClipper : CustomClipper<Path>
    {
        public FixedRectPathClipper(Rect rect)
        {
            Rect = rect;
        }

        public Rect Rect { get; }

        public override Path GetClip(Size size)
        {
            var path = new Path();
            path.AddRect(Rect);
            return path;
        }

        public override Rect GetApproximateClipRect(Size size) => Rect;

        public override bool ShouldReclip(CustomClipper<Path> oldClipper)
        {
            return oldClipper is not FixedRectPathClipper old || old.Rect != Rect;
        }
    }

    private sealed class ListeningOvalClipper : CustomClipper<Rect>
    {
        public ListeningOvalClipper(IListenable reclip) : base(reclip)
        {
        }

        public int GetClipCalls { get; private set; }

        public override Rect GetClip(Size size)
        {
            GetClipCalls += 1;
            return new Rect(new Point(0, 0), size);
        }

        public override bool ShouldReclip(CustomClipper<Rect> oldClipper) => false;
    }

    private sealed class TrackingListenable : IListenable
    {
        private readonly List<Action> _listeners = [];

        public int ListenerCount => _listeners.Count;

        public void AddListener(Action listener) => _listeners.Add(listener);

        public void RemoveListener(Action listener) => _listeners.Remove(listener);

        public void Notify()
        {
            foreach (Action listener in _listeners.ToArray())
            {
                listener();
            }
        }
    }

    private sealed class HitTestBox : RenderBox
    {
        private readonly Size _size;

        public HitTestBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class PaintBox : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(new Size(100, 60));
        }

        protected override bool HitTestSelf(Point position) => true;

        public override void Paint(PaintingContext ctx, Point offset)
        {
            ctx.DrawRectangle(Brushes.Red, null, new Rect(offset, Size));
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
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(child, _child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }
}
