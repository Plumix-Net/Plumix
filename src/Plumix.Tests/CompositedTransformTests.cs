using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/basic.dart
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart
// - flutter/packages/flutter/lib/src/rendering/layer.dart

public sealed class CompositedTransformTests
{
    [Fact]
    public void Widgets_UseFlutterDefaultsAndUpdateExistingRenderObjects()
    {
        var firstLink = new LayerLink();
        var secondLink = new LayerLink();
        var targetOwner = new BuildOwner();
        var targetRoot = new TestRootElement(
            new CompositedTransformTarget(
                firstLink,
                child: new SizedBox(width: 20, height: 10)));

        targetRoot.Attach(targetOwner);
        targetRoot.Mount(parent: null, newSlot: null);
        targetOwner.FlushBuild();

        RenderLeaderLayer target = RequireRenderObject<RenderLeaderLayer>(targetRoot.ChildElement);
        Assert.Same(firstLink, target.Link);

        targetRoot.Update(new CompositedTransformTarget(
            secondLink,
            child: new SizedBox(width: 20, height: 10)));
        targetOwner.FlushBuild();

        RenderLeaderLayer updatedTarget = RequireRenderObject<RenderLeaderLayer>(targetRoot.ChildElement);
        Assert.Same(target, updatedTarget);
        Assert.Same(secondLink, updatedTarget.Link);

        var owner = new BuildOwner();
        var root = new TestRootElement(
            new CompositedTransformFollower(
                firstLink,
                child: new SizedBox(width: 20, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        RenderFollowerLayer follower = RequireRenderObject<RenderFollowerLayer>(root.ChildElement);
        Assert.Same(firstLink, follower.Link);
        Assert.True(follower.ShowWhenUnlinked);
        Assert.Equal(default, follower.Offset);
        Assert.Equal(Alignment.TopLeft, follower.LeaderAnchor);
        Assert.Equal(Alignment.TopLeft, follower.FollowerAnchor);

        root.Update(new CompositedTransformFollower(
            secondLink,
            showWhenUnlinked: false,
            offset: new Vector(7, -3),
            targetAnchor: Alignment.BottomRight,
            followerAnchor: Alignment.Center,
            child: new SizedBox(width: 20, height: 10)));
        owner.FlushBuild();

        RenderFollowerLayer updated = RequireRenderObject<RenderFollowerLayer>(root.ChildElement);
        Assert.Same(follower, updated);
        Assert.Same(secondLink, updated.Link);
        Assert.False(updated.ShowWhenUnlinked);
        Assert.Equal(new Vector(7, -3), updated.Offset);
        Assert.Equal(Alignment.BottomRight, updated.LeaderAnchor);
        Assert.Equal(Alignment.Center, updated.FollowerAnchor);
    }

    [Fact]
    public void LinkedFollower_AlignsAnchorsAndOffsetInLeaderCoordinates()
    {
        var link = new LayerLink();
        var target = new RenderLeaderLayer(link, new HitTestRenderBox(new Size(40, 20)));
        var followerChild = new HitTestRenderBox(new Size(20, 10), hitTestSelf: true);
        var follower = new RenderFollowerLayer(
            link,
            offset: new Vector(5, 7),
            leaderAnchor: Alignment.BottomRight,
            followerAnchor: Alignment.Center,
            child: followerChild);
        RenderStack stack = CreatePositionedStack(target, follower);
        var renderView = new RenderView { Child = stack };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(new Size(40, 20), link.LeaderSize);
        Assert.NotNull(link.Leader);
        Assert.Equal(Matrix4.TranslationValues(-85, -38, 0.0), follower.GetCurrentTransform());

        var result = new BoxHitTestResult();
        Assert.True(stack.HitTest(result, new Point(75, 67)));
        Assert.Contains(result.Path, entry => ReferenceEquals(entry.Target, followerChild));

        Matrix4 semanticsTransform = Matrix4.Identity();
        follower.ApplyPaintTransform(followerChild, semanticsTransform);
        Assert.Equal(follower.GetCurrentTransform(), semanticsTransform);

        Assert.Single(FindLayers<LeaderLayer>(pipeline.RootLayer));
        Assert.Single(FindLayers<FollowerLayer>(pipeline.RootLayer));
    }

    [Fact]
    public void Follower_UsesIdentityWhenUnlinkedAndCanHidePaintHitTestingAndSemantics()
    {
        var link = new LayerLink();
        var child = new HitTestRenderBox(new Size(20, 10), hitTestSelf: true);
        var follower = new RenderFollowerLayer(link, showWhenUnlinked: false, child: child);
        var renderView = new RenderView { Child = follower };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(20, 10));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Null(link.Leader);
        Assert.Equal(Matrix4.Identity(), follower.GetCurrentTransform());
        Assert.False(follower.HitTest(new BoxHitTestResult(), new Point(5, 5)));

        int semanticsVisits = 0;
        follower.VisitChildrenForSemantics(_ => semanticsVisits++);
        Assert.Equal(0, semanticsVisits);

        FollowerLayer followerLayer = Assert.Single(FindLayers<FollowerLayer>(pipeline.RootLayer));
        Assert.False(followerLayer.ShowWhenUnlinked);
        Assert.Null(followerLayer.GetLastTransform());
    }

    [Fact]
    public void LinkedFollower_ComposesNonCommutativeAncestorTransformsInPaintOrder()
    {
        var link = new LayerLink();
        var target = new RenderLeaderLayer(link, new HitTestRenderBox(new Size(40, 20)));
        var transformedTarget = new RenderTransform(
            Matrix4.Diagonal3Values(1.5, 2, 1.0),
            Alignment.TopLeft,
            target);
        var follower = new RenderFollowerLayer(
            link,
            offset: new Vector(5, 7),
            leaderAnchor: Alignment.BottomRight,
            followerAnchor: Alignment.Center,
            child: new HitTestRenderBox(new Size(20, 10)));
        var transformedFollower = new RenderTransform(
            Matrix4.Diagonal3Values(0.5, 0.75, 1.0),
            Alignment.TopLeft,
            follower);
        var stack = new RenderStack(
            [transformedTarget, transformedFollower],
            clipBehavior: Clip.None,
            textDirection: TextDirection.Ltr);
        Position(transformedTarget, left: 30, top: 40, width: 40, height: 20);
        Position(transformedFollower, left: 150, top: 100, width: 20, height: 10);
        var renderView = new RenderView { Child = stack };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(300, 200));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.True(target.TryGetTransformFromRoot(out Matrix4 leaderToRoot));
        Assert.True(follower.TryGetTransformFromRoot(out Matrix4 followerToRoot));
        Matrix4 followerChildToRoot = Matrix4.Copy(followerToRoot);
        followerChildToRoot.Multiply(follower.GetCurrentTransform());
        Point expectedAnchor = MatrixUtils.TransformPoint(leaderToRoot, new Point(45, 27));
        Point actualAnchor = MatrixUtils.TransformPoint(followerChildToRoot, new Point(10, 5));
        Assert.Equal(expectedAnchor.X, actualAnchor.X, precision: 8);
        Assert.Equal(expectedAnchor.Y, actualAnchor.Y, precision: 8);
    }

    [Fact]
    public void LeaderLinkUpdateTransfersPublishedSizeAndLayerRegistration()
    {
        var firstLink = new LayerLink();
        var secondLink = new LayerLink();
        var target = new RenderLeaderLayer(firstLink, new HitTestRenderBox(new Size(32, 18)));
        var renderView = new RenderView { Child = target };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(32, 18));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(new Size(32, 18), firstLink.LeaderSize);
        Assert.NotNull(firstLink.Leader);

        target.Link = secondLink;
        pipeline.FlushPaint();

        Assert.Null(firstLink.LeaderSize);
        Assert.Null(firstLink.Leader);
        Assert.Equal(new Size(32, 18), secondLink.LeaderSize);
        Assert.NotNull(secondLink.Leader);
    }

    private static RenderStack CreatePositionedStack(RenderLeaderLayer target, RenderFollowerLayer follower)
    {
        var stack = new RenderStack(
            [target, follower],
            clipBehavior: Clip.None,
            textDirection: TextDirection.Ltr);
        Position(target, left: 30, top: 40, width: 40, height: 20);
        Position(follower, left: 150, top: 100, width: 20, height: 10);
        return stack;
    }

    private static void Position(RenderBox child, double left, double top, double width, double height)
    {
        var parentData = (StackParentData)child.parentData!;
        parentData.Left = left;
        parentData.Top = top;
        parentData.Width = width;
        parentData.Height = height;
    }

    private static List<T> FindLayers<T>(Layer layer) where T : Layer
    {
        List<T> result = layer is T match ? [match] : [];
        if (layer is not ContainerLayer container)
        {
            return result;
        }

        foreach (Layer child in container.Children)
        {
            result.AddRange(FindLayers<T>(child));
        }

        return result;
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        if (element?.RenderObject is T renderObject)
        {
            return renderObject;
        }

        throw new InvalidOperationException($"Unable to find render object {typeof(T).Name}.");
    }

    private sealed class HitTestRenderBox : RenderBox
    {
        private readonly Size _preferredSize;
        private readonly bool _hitTestSelf;

        public HitTestRenderBox(Size preferredSize, bool hitTestSelf = false)
        {
            _preferredSize = preferredSize;
            _hitTestSelf = hitTestSelf;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_preferredSize);
        }

        protected override bool HitTestSelf(Point position) => _hitTestSelf;

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

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
