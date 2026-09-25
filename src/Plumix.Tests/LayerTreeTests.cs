using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// - flutter/packages/flutter/test/rendering/layers_test.dart
// - flutter/packages/flutter/test/rendering/layer_annotations_test.dart (LeaderLayer offset)
// - flutter/packages/flutter/test/rendering/object_test.dart (leader layer link moves)
// - flutter/packages/flutter/test/widgets/composited_transform_test.dart (paint order, composite-time transforms)
// - flutter/packages/flutter/test/widgets/debug_test.dart (follower debugCreator)

public sealed class LayerTreeTests
{
    [DebugOnlyFact]
    public void UpdateSubtreeNeedsAddToScene_PropagatesAlwaysNeedsAddToSceneUpTheTree()
    {
        var a = new ContainerLayer();
        var b = new ContainerLayer();
        var c = new ContainerLayer();
        var d = new AlwaysNeedsAddToSceneLayer();
        var e = new ContainerLayer();
        var f = new ContainerLayer();
        // Tree structure:
        //        a
        //       / \
        //      b   c
        //     / \
        // (x)d   e
        //   /
        //  f
        a.Append(b);
        a.Append(c);
        b.Append(d);
        b.Append(e);
        d.Append(f);

        ContainerLayer[] all = [a, b, c, d, e, f];
        foreach (ContainerLayer layer in all)
        {
            layer.DebugMarkClean();
        }

        Assert.All(all, layer => Assert.False(layer.DebugSubtreeNeedsAddToScene));

        a.UpdateSubtreeNeedsAddToScene();
        Assert.True(a.DebugSubtreeNeedsAddToScene);
        Assert.True(b.DebugSubtreeNeedsAddToScene);
        Assert.False(c.DebugSubtreeNeedsAddToScene);
        Assert.True(d.DebugSubtreeNeedsAddToScene);
        Assert.False(e.DebugSubtreeNeedsAddToScene);
        Assert.False(f.DebugSubtreeNeedsAddToScene);
    }

    [DebugOnlyFact]
    public void UpdateSubtreeNeedsAddToScene_PropagatesNeedsAddToSceneUpTheTree()
    {
        var a = new ContainerLayer();
        var b = new ContainerLayer();
        var c = new ContainerLayer();
        var d = new ContainerLayer();
        var e = new ContainerLayer();
        var f = new ContainerLayer();
        var g = new ContainerLayer();
        ContainerLayer[] allLayers = [a, b, c, d, e, f, g];
        // The tree is like the following where b and j are dirty:
        //        a____
        //       /     \
        //   (x)b___    c
        //     / \  \   |
        //    d   e  f  g(x)
        a.Append(b);
        a.Append(c);
        b.Append(d);
        b.Append(e);
        b.Append(f);
        c.Append(g);

        Assert.All(allLayers, layer => Assert.True(layer.DebugSubtreeNeedsAddToScene));
        foreach (ContainerLayer layer in allLayers)
        {
            layer.DebugMarkClean();
        }

        Assert.All(allLayers, layer => Assert.False(layer.DebugSubtreeNeedsAddToScene));

        b.MarkNeedsAddToScene();
        a.UpdateSubtreeNeedsAddToScene();
        Assert.True(a.DebugSubtreeNeedsAddToScene);
        Assert.True(b.DebugSubtreeNeedsAddToScene);
        Assert.False(c.DebugSubtreeNeedsAddToScene);
        Assert.False(d.DebugSubtreeNeedsAddToScene);
        Assert.False(e.DebugSubtreeNeedsAddToScene);
        Assert.False(f.DebugSubtreeNeedsAddToScene);
        Assert.False(g.DebugSubtreeNeedsAddToScene);

        g.MarkNeedsAddToScene();
        a.UpdateSubtreeNeedsAddToScene();
        Assert.True(a.DebugSubtreeNeedsAddToScene);
        Assert.True(b.DebugSubtreeNeedsAddToScene);
        Assert.True(c.DebugSubtreeNeedsAddToScene);
        Assert.False(d.DebugSubtreeNeedsAddToScene);
        Assert.False(e.DebugSubtreeNeedsAddToScene);
        Assert.False(f.DebugSubtreeNeedsAddToScene);
        Assert.True(g.DebugSubtreeNeedsAddToScene);

        a.BuildScene(null);
        Assert.All(allLayers, layer => Assert.False(layer.DebugSubtreeNeedsAddToScene));
    }

    [DebugOnlyFact]
    public void FollowerLayers_AreAlwaysDirty()
    {
        var link = new LayerLink();
        var leaderLayer = new LeaderLayer(link);
        var followerLayer = new FollowerLayer(link);
        leaderLayer.DebugMarkClean();
        followerLayer.DebugMarkClean();
        leaderLayer.UpdateSubtreeNeedsAddToScene();
        followerLayer.UpdateSubtreeNeedsAddToScene();
        Assert.True(followerLayer.DebugSubtreeNeedsAddToScene);
    }

    [Fact]
    public void SwitchingLayerLinkOfAnAttachedLeaderLayer_DoesNotCrash()
    {
        var link = new LayerLink();
        var leaderLayer = new LeaderLayer(link);
        var view = new RenderView(new FlutterView(new Size(800, 600)));
        leaderLayer.Attach(view);
        var link2 = new LayerLink();
        leaderLayer.Link = link2;
        // This should not crash.
        leaderLayer.Detach();
        Assert.Same(link2, leaderLayer.Link);
    }

    [Fact]
    public void LayerLinkAttachDetachOrder_DoesNotCrash()
    {
        var link = new LayerLink();
        var leaderLayer1 = new LeaderLayer(link);
        var leaderLayer2 = new LeaderLayer(link);
        var view = new RenderView(new FlutterView(new Size(800, 600)));
        leaderLayer1.Attach(view);
        leaderLayer2.Attach(view);
        leaderLayer2.Detach();
        leaderLayer1.Detach();
        Assert.Null(link.Leader);
    }

    [DebugOnlyFact]
    public void LeaderLayers_NotDirtyWhenConnectedToFollowerLayer()
    {
        var root = new ContainerLayer();
        root.Attach(new object());
        var link = new LayerLink();
        var leaderLayer = new LeaderLayer(link);
        var followerLayer = new FollowerLayer(link);
        root.Append(leaderLayer);
        root.Append(followerLayer);
        leaderLayer.DebugMarkClean();
        followerLayer.DebugMarkClean();
        leaderLayer.UpdateSubtreeNeedsAddToScene();
        followerLayer.UpdateSubtreeNeedsAddToScene();
        Assert.False(leaderLayer.DebugSubtreeNeedsAddToScene);
    }

    [DebugOnlyFact]
    public void LeaderLayers_AreNotDirtyWhenAllFollowersDisconnect()
    {
        var root = new ContainerLayer();
        root.Attach(new object());
        var link = new LayerLink();
        var leaderLayer = new LeaderLayer(link);
        root.Append(leaderLayer);

        void ExpectClean()
        {
            leaderLayer.DebugMarkClean();
            leaderLayer.UpdateSubtreeNeedsAddToScene();
            Assert.False(leaderLayer.DebugSubtreeNeedsAddToScene);
        }

        // Does not need add to scene when nothing is connected to link.
        ExpectClean();

        // Connecting a follower does not require adding to scene
        var follower1 = new FollowerLayer(link);
        root.Append(follower1);
        ExpectClean();

        var follower2 = new FollowerLayer(link);
        root.Append(follower2);
        ExpectClean();

        // Disconnecting one follower, still does not needs add to scene.
        follower2.Remove();
        ExpectClean();

        // Disconnecting all followers goes back to not requiring add to scene.
        follower1.Remove();
        ExpectClean();
    }

    [Fact]
    public void DepthFirstIterateChildren_VisitsInPaintOrderAndForgetsRemovedSubtrees()
    {
        var a = new ContainerLayer();
        var b = new ContainerLayer();
        var c = new ContainerLayer();
        var d = new ContainerLayer();
        var e = new ContainerLayer();
        var f = new ContainerLayer();
        var g = new ContainerLayer();
        var h = new PictureLayer(default);
        var i = new PictureLayer(default);
        var j = new PictureLayer(default);
        // The tree is like the following:
        //        a____
        //       /     \
        //      b___    c
        //     / \  \   |
        //    d   e  f  g
        //   / \        |
        //  h   i       j
        a.Append(b);
        a.Append(c);
        b.Append(d);
        b.Append(e);
        b.Append(f);
        d.Append(h);
        d.Append(i);
        c.Append(g);
        g.Append(j);
        Assert.Equal<Layer>([b, d, h, i, e, f, c, g, j], a.DepthFirstIterateChildren());

        d.Remove();
        Assert.Equal<Layer>([b, e, f, c, g, j], a.DepthFirstIterateChildren());
    }

    [Fact]
    public void Siblings_AndDepth_FollowTheChildList()
    {
        var root = new ContainerLayer();
        var first = new ContainerLayer();
        var middle = new OffsetLayer();
        var last = new PictureLayer(default);
        var grandChild = new PictureLayer(default);
        Assert.Equal(0, root.Depth);
        Assert.Null(root.FirstChild);
        Assert.Null(root.LastChild);

        root.Append(first);
        root.Append(middle);
        root.Append(last);
        middle.Append(grandChild);

        Assert.Same(first, root.FirstChild);
        Assert.Same(last, root.LastChild);
        Assert.Null(first.PreviousSibling);
        Assert.Same(middle, first.NextSibling);
        Assert.Same(first, middle.PreviousSibling);
        Assert.Same(last, middle.NextSibling);
        Assert.Null(last.NextSibling);
        Assert.Equal(1, first.Depth);
        Assert.Equal(1, middle.Depth);
        Assert.Equal(2, grandChild.Depth);

        middle.Remove();
        Assert.Same(last, first.NextSibling);
        Assert.Same(first, last.PreviousSibling);
        Assert.Null(middle.NextSibling);
        Assert.Null(middle.PreviousSibling);
        Assert.Null(middle.Parent);
        // Depth is only ever raised, including on removal.
        Assert.Equal(1, middle.Depth);
        Assert.Equal<Layer>([first, last], root.Children);

        var deeper = new ContainerLayer();
        var deepest = new ContainerLayer();
        deeper.Append(deepest);
        deepest.Append(new ContainerLayer());
        first.Append(deeper);
        Assert.Equal(2, deeper.Depth);
        Assert.Equal(3, deepest.Depth);
        Assert.Equal(4, ((ContainerLayer)deepest.FirstChild!).Depth);
    }

    [Fact]
    public void ClipLayers_PrintClipBehaviorInDebugInfo()
    {
        Assert.Contains("clipBehavior: Clip.hardEdge", GetDebugInfo(new ClipRectLayer()));
        Assert.Contains(
            "clipBehavior: Clip.antiAliasWithSaveLayer",
            GetDebugInfo(new ClipRectLayer { ClipBehavior = Clip.AntiAliasWithSaveLayer }));
        Assert.Contains("clipBehavior: Clip.antiAlias", GetDebugInfo(new ClipRRectLayer()));
        Assert.Contains("clipBehavior: Clip.antiAlias", GetDebugInfo(new ClipRSuperellipseLayer()));
        Assert.Contains("clipBehavior: Clip.antiAlias", GetDebugInfo(new ClipPathLayer()));
    }

    [Fact]
    public void PictureLayer_PrintsPictureAndRasterCacheHintsInDebugInfo()
    {
        Picture picture = new PictureRecorder().EndRecording();
        var layer = new PictureLayer(new Rect(0, 0, 100, 100))
        {
            Picture = picture,
            IsComplexHint = true,
        };
        List<string> info = GetDebugInfo(layer);
        Assert.Contains($"picture: {Diagnostics.DescribeIdentity(picture)}", info);
        Assert.DoesNotContain(info, line => line.StartsWith("engine layer", StringComparison.Ordinal));
        Assert.Contains("raster cache hints: isComplex = true, willChange = false", info);
    }

    [Fact]
    public void Layer_PrintsEngineLayerOnlyWhenItIsNotNull()
    {
        var layer = new ContainerLayer();
        Assert.DoesNotContain(GetDebugInfo(layer), line => line.StartsWith("engine layer", StringComparison.Ordinal));

        var engineLayer = new TestEngineLayer();
        layer.EngineLayer = engineLayer;
        Assert.Contains($"engine layer: {Diagnostics.DescribeIdentity(engineLayer)}", GetDebugInfo(layer));
    }

    [DebugOnlyFact]
    public void MutatingPictureLayerFields_TriggersNeedsAddToScene()
    {
        var pictureLayer = new PictureLayer(new Rect(0, 0, 10, 10));
        CheckNeedsAddToScene(pictureLayer, () => pictureLayer.Picture = new PictureRecorder().EndRecording());

        pictureLayer.IsComplexHint = false;
        CheckNeedsAddToScene(pictureLayer, () => pictureLayer.IsComplexHint = true);

        pictureLayer.WillChangeHint = false;
        CheckNeedsAddToScene(pictureLayer, () => pictureLayer.WillChangeHint = true);
    }

    [DebugOnlyFact]
    public void MutatingOffsetAndOpacityLayerFields_TriggersNeedsAddToScene()
    {
        var offsetLayer = new OffsetLayer(new Point(0, 0));
        CheckNeedsAddToScene(offsetLayer, () => offsetLayer.Offset = new Point(1, 1));

        var opacityLayer = new OpacityLayer(alpha: 0, offset: new Point(0, 0));
        CheckNeedsAddToScene(opacityLayer, () => opacityLayer.Alpha = 1);
        CheckNeedsAddToScene(opacityLayer, () => opacityLayer.Offset = new Point(1, 1));
    }

    [DebugOnlyFact]
    public void MutatingClipLayerFields_TriggersNeedsAddToScene()
    {
        var clipRectLayer = new ClipRectLayer { ClipRect = new Rect(0, 0, 10, 10) };
        CheckNeedsAddToScene(clipRectLayer, () => clipRectLayer.ClipRect = new Rect(1, 1, 10, 10));
        CheckNeedsAddToScene(clipRectLayer, () => clipRectLayer.ClipBehavior = Clip.AntiAlias);

        var clipRRectLayer = new ClipRRectLayer
        {
            ClipRRect = RRect.FromRectAndRadius(new Rect(0, 0, 10, 10), Radius.Zero),
        };
        CheckNeedsAddToScene(
            clipRRectLayer,
            () => clipRRectLayer.ClipRRect = RRect.FromRectAndRadius(new Rect(1, 1, 10, 10), Radius.Zero));
        CheckNeedsAddToScene(clipRRectLayer, () => clipRRectLayer.ClipBehavior = Clip.HardEdge);

        var clipRSuperellipseLayer = new ClipRSuperellipseLayer
        {
            ClipRSuperellipse = RSuperellipse.FromRectAndRadius(new Rect(0, 0, 10, 10), Radius.Zero),
        };
        CheckNeedsAddToScene(
            clipRSuperellipseLayer,
            () => clipRSuperellipseLayer.ClipRSuperellipse =
                RSuperellipse.FromRectAndRadius(new Rect(1, 1, 10, 10), Radius.Zero));
        CheckNeedsAddToScene(clipRSuperellipseLayer, () => clipRSuperellipseLayer.ClipBehavior = Clip.HardEdge);

        var clipPathLayer = new ClipPathLayer { ClipPath = new Plumix.UI.Path() };
        CheckNeedsAddToScene(clipPathLayer, () =>
        {
            var newPath = new Plumix.UI.Path();
            newPath.AddRect(new Rect(0, 0, 10, 10));
            clipPathLayer.ClipPath = newPath;
        });
        CheckNeedsAddToScene(clipPathLayer, () => clipPathLayer.ClipBehavior = Clip.AntiAliasWithSaveLayer);
    }

    [DebugOnlyFact]
    public void MutatingEffectLayerFields_TriggersNeedsAddToScene()
    {
        var colorFilterLayer = new ColorFilterLayer
        {
            ColorFilter = new ColorFilter.Mode(Colors.Red, BlendMode.Color),
        };
        CheckNeedsAddToScene(
            colorFilterLayer,
            () => colorFilterLayer.ColorFilter = new ColorFilter.Mode(Colors.Blue, BlendMode.Color));

        var shaderMaskLayer = new ShaderMaskLayer { MaskRect = new Rect(0, 0, 10, 10) };
        CheckNeedsAddToScene(shaderMaskLayer, () => shaderMaskLayer.MaskRect = new Rect(1, 1, 10, 10));
        CheckNeedsAddToScene(shaderMaskLayer, () => shaderMaskLayer.BlendMode = BlendMode.Source);
        CheckNeedsAddToScene(
            shaderMaskLayer,
            () => shaderMaskLayer.Shader = new Avalonia.Media.SolidColorBrush(Colors.Red));

        var backdropFilterLayer = new BackdropFilterLayer { ImageFilter = new ImageFilter.Blur(1.0, 1.0) };
        CheckNeedsAddToScene(
            backdropFilterLayer,
            () => backdropFilterLayer.ImageFilter = new ImageFilter.Blur(2.0, 2.0));
    }

    [Fact]
    public void OpacityLayer_DisposesItsEngineLayerAndSkipsItselfWithoutChildren()
    {
        var layer = new OpacityLayer(alpha: 128);
        var engineLayer = new TestEngineLayer();
        layer.EngineLayer = engineLayer;

        layer.BuildScene(null);

        Assert.Null(layer.EngineLayer);
        Assert.True(engineLayer.Disposed);
    }

    [Fact]
    public void OpacityLayer_DropsItsEngineLayerWhenAlphaCrossesOpaque()
    {
        var layer = new OpacityLayer(alpha: 128);
        layer.EngineLayer = new TestEngineLayer();
        layer.Alpha = 200;
        Assert.NotNull(layer.EngineLayer);

        layer.Alpha = 255;
        Assert.Null(layer.EngineLayer);

        layer.EngineLayer = new TestEngineLayer();
        layer.Alpha = 200;
        Assert.Null(layer.EngineLayer);
    }

    [Fact]
    public void Layers_DescribeClipBounds()
    {
        var bounds = new Rect(10, 10, 20, 20);
        RRect rrBounds = RRect.FromRectAndRadius(bounds, new Radius(2, 2));
        RSuperellipse rseBounds = RSuperellipse.FromRectAndRadius(bounds, new Radius(2, 2));
        var path = new Plumix.UI.Path();
        path.AddRect(bounds);

        Assert.Null(new ContainerLayer().DescribeClipBounds());
        Assert.Equal(bounds, new ClipRectLayer { ClipRect = bounds }.DescribeClipBounds());
        Assert.Equal(rrBounds.Rect, new ClipRRectLayer { ClipRRect = rrBounds }.DescribeClipBounds());
        Assert.Equal(
            rseBounds.Rect,
            new ClipRSuperellipseLayer { ClipRSuperellipse = rseBounds }.DescribeClipBounds());
        Assert.Equal(bounds, new ClipPathLayer { ClipPath = path }.DescribeClipBounds());
    }

    [Fact]
    public void SubtreeHasCompositionCallbacks_FollowsAddedAndRemovedCallbacks()
    {
        var root = new ContainerLayer();
        Assert.False(root.SubtreeHasCompositionCallbacks);

        var cancellationCallbacks = new List<Action> { root.AddCompositionCallback(_ => { }) };
        Assert.True(root.SubtreeHasCompositionCallbacks);

        var a1 = new ContainerLayer();
        var a2 = new ContainerLayer();
        var b1 = new ContainerLayer();
        root.Append(a1);
        root.Append(a2);
        a1.Append(b1);

        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);
        Assert.False(b1.SubtreeHasCompositionCallbacks);

        cancellationCallbacks.Add(b1.AddCompositionCallback(_ => { }));
        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.True(a1.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);
        Assert.True(b1.SubtreeHasCompositionCallbacks);

        cancellationCallbacks[0]();
        cancellationCallbacks.RemoveAt(0);
        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.True(a1.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);
        Assert.True(b1.SubtreeHasCompositionCallbacks);

        cancellationCallbacks[0]();
        cancellationCallbacks.RemoveAt(0);
        Assert.False(root.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);
        Assert.False(b1.SubtreeHasCompositionCallbacks);
    }

    [Fact]
    public void SubtreeHasCompositionCallbacks_RemoveChild()
    {
        var root = new ContainerLayer();
        var a1 = new ContainerLayer();
        var a2 = new ContainerLayer();
        var b1 = new ContainerLayer();
        root.Append(a1);
        root.Append(a2);
        a1.Append(b1);
        Assert.False(b1.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);
        Assert.False(root.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);

        b1.AddCompositionCallback(_ => { });
        Assert.True(b1.SubtreeHasCompositionCallbacks);
        Assert.True(a1.SubtreeHasCompositionCallbacks);
        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);

        b1.Remove();
        Assert.True(b1.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);
        Assert.False(root.SubtreeHasCompositionCallbacks);
        Assert.False(a2.SubtreeHasCompositionCallbacks);
    }

    [Fact]
    public void CompositionCallback_IsNotCalledOnceRemoved()
    {
        (ContainerLayer root, _, ContainerLayer b1) = CreateCallbackTree();
        // Add and immediately remove the callback.
        b1.AddCompositionCallback(_ => Assert.Fail("Should not have called back"))();
        root.BuildScene(null);
    }

    [Fact]
    public void CompositionCallback_ObservesTheComposite()
    {
        (ContainerLayer root, _, ContainerLayer b1) = CreateCallbackTree();
        bool compositedB1 = false;
        b1.AddCompositionCallback(layer =>
        {
            Assert.Same(b1, layer);
            compositedB1 = true;
        });
        Assert.False(compositedB1);
        root.BuildScene(null);
        Assert.True(compositedB1);
    }

    [Fact]
    public void CompositionCallback_ObservesTheCompositeOfACleanLayerWithAnEngineLayer()
    {
        (ContainerLayer root, _, ContainerLayer b1) = CreateCallbackTree();
        b1.EngineLayer = new TestEngineLayer();
        b1.DebugMarkClean();
        bool compositedB1 = false;
        b1.AddCompositionCallback(layer =>
        {
            Assert.Same(b1, layer);
            compositedB1 = true;
        });
        Assert.False(compositedB1);
        root.BuildScene(null);
        Assert.True(compositedB1);
    }

    [DebugOnlyFact]
    public void CompositionCallback_AssertsOnMutation()
    {
        (ContainerLayer root, _, ContainerLayer b1) = CreateCallbackTree();
        bool compositedB1 = false;
        b1.AddCompositionCallback(layer =>
        {
            Assert.Same(b1, layer);
            Assert.Throws<AssertionError>(() => layer.Remove());
            Assert.Throws<AssertionError>(() => layer.Dispose());
            Assert.Throws<AssertionError>(() => layer.MarkNeedsAddToScene());
            Assert.Throws<AssertionError>(() => layer.DebugMarkClean());
            Assert.Throws<AssertionError>(() => layer.UpdateSubtreeNeedsAddToScene());
            Assert.Throws<AssertionError>(() => layer.Remove());
            Assert.Throws<AssertionError>(() => ((ContainerLayer)layer).Append(new ContainerLayer()));
            Assert.Throws<AssertionError>(() => layer.EngineLayer = null);
            compositedB1 = true;
        });
        Assert.False(compositedB1);
        root.BuildScene(null);
        Assert.True(compositedB1);
    }

    [Fact]
    public void CompositionCallback_DetachTriggersTheCallback()
    {
        (ContainerLayer root, _, ContainerLayer b1) = CreateCallbackTree();
        bool compositedB1 = false;
        b1.AddCompositionCallback(layer =>
        {
            Assert.Same(b1, layer);
            compositedB1 = true;
        });
        root.Attach(new object());
        Assert.False(compositedB1);
        root.Detach();
        Assert.True(compositedB1);
    }

    [Fact]
    public void CompositionCallback_ObserverCountIsCorrectlyMaintained()
    {
        var root = new ContainerLayer();
        var a1 = new ContainerLayer();
        root.Append(a1);
        Assert.False(root.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);

        Action remover1 = a1.AddCompositionCallback(_ => { });
        Action remover2 = a1.AddCompositionCallback(_ => { });
        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.True(a1.SubtreeHasCompositionCallbacks);

        remover1();
        Assert.True(root.SubtreeHasCompositionCallbacks);
        Assert.True(a1.SubtreeHasCompositionCallbacks);

        remover2();
        Assert.False(root.SubtreeHasCompositionCallbacks);
        Assert.False(a1.SubtreeHasCompositionCallbacks);
    }

    [DebugOnlyFact]
    public void CompositionCallback_DoubleRemovingThrows()
    {
        var root = new ContainerLayer();
        Action callback = root.AddCompositionCallback(_ => { });
        callback();
        Assert.Throws<AssertionError>(callback);
    }

    [Fact]
    public void CompositionCallback_RemovingOnADisposedLayerDoesNotThrow()
    {
        var root = new ContainerLayer();
        Action callback = root.AddCompositionCallback(_ => { });
        root.Dispose();
        callback();
    }

    [Fact]
    public void PaintingContext_AddCompositionCallback_ObservesItsContainerLayer()
    {
        var root = new OffsetLayer();
        var context = new PaintingContext(root, new Rect(0, 0, 10, 10));
        Layer? observed = null;
        context.AddCompositionCallback(layer => observed = layer);
        root.BuildScene(null);
        Assert.Same(root, observed);
    }

    [Fact]
    public void LayerTypes_ThatSupportRasterization()
    {
        Assert.True(new OffsetLayer().SupportsRasterization());
        Assert.True(new OpacityLayer().SupportsRasterization());
        Assert.True(new ClipRectLayer().SupportsRasterization());
        Assert.True(new ClipRRectLayer().SupportsRasterization());
        Assert.True(new ClipRSuperellipseLayer().SupportsRasterization());
        Assert.True(new ImageFilterLayer().SupportsRasterization());
        Assert.True(new BackdropFilterLayer().SupportsRasterization());
        Assert.True(new ColorFilterLayer().SupportsRasterization());
        Assert.True(new ShaderMaskLayer().SupportsRasterization());

        var container = new ContainerLayer();
        container.Append(new OffsetLayer());
        container.Append(new NoRasterizationLayer());
        Assert.False(container.SupportsRasterization());
    }

    [Fact]
    public void ApplyTransform_MatchesEachLayersCompositedTransform()
    {
        var child = new ContainerLayer();

        Matrix4 offsetTransform = Matrix4.Identity();
        new OffsetLayer(new Point(3, 4)).ApplyTransform(child, offsetTransform);
        Assert.Equal(Matrix4.TranslationValues(3, 4, 0), offsetTransform);

        Matrix4 clipTransform = Matrix4.Identity();
        new ClipRectLayer { ClipRect = new Rect(0, 0, 1, 1) }.ApplyTransform(child, clipTransform);
        Assert.Equal(Matrix4.Identity(), clipTransform);

        // Before the first composite a TransformLayer applies its transform without its offset; after it,
        // the effective transform T(offset) * transform.
        var transformLayer = new TransformLayer(Matrix4.Diagonal3Values(2, 3, 1), new Point(5, 7));
        transformLayer.Append(new ContainerLayer());
        Matrix4 beforeComposite = Matrix4.Identity();
        transformLayer.ApplyTransform(child, beforeComposite);
        Assert.Equal(Matrix4.Diagonal3Values(2, 3, 1), beforeComposite);

        transformLayer.BuildScene(null);
        Matrix4 afterComposite = Matrix4.Identity();
        transformLayer.ApplyTransform(child, afterComposite);
        Matrix4 expected = Matrix4.TranslationValues(5, 7, 0);
        expected.Multiply(Matrix4.Diagonal3Values(2, 3, 1));
        Assert.Equal(expected, afterComposite);

        Matrix4 leaderTransform = Matrix4.Identity();
        new LeaderLayer(new LayerLink(), new Point(9, 11)).ApplyTransform(null, leaderTransform);
        Assert.Equal(Matrix4.TranslationValues(9, 11, 0), leaderTransform);
    }

    [Fact]
    public void LeaderLayer_FindAllAnnotations_RespectsOffset()
    {
        const int insideLayer = 1;
        const int outsideLayer = 1000;
        var root = new ContainerLayer();
        root.Append(new AnnotatedRegionLayer<int>(outsideLayer));
        var leader = new LeaderLayer(new LayerLink(), new Point(-10, 0));
        leader.Append(new AnnotatedRegionLayer<int>(insideLayer, size: new Size(10, 10), opaque: true));
        root.Append(leader);

        AnnotationEntry<int> inside = Assert.Single(root.FindAllAnnotations<int>(new Point(-5, 5)).Entries);
        Assert.Equal(new AnnotationEntry<int>(insideLayer, new Point(5, 5)), inside);
        AnnotationEntry<int> outside = Assert.Single(root.FindAllAnnotations<int>(new Point(5, 5)).Entries);
        Assert.Equal(new AnnotationEntry<int>(outsideLayer, new Point(5, 5)), outside);
    }

    [Fact]
    public void FollowerLayer_EstablishesItsTransformFromTheLayerChainWhileCompositing()
    {
        var link = new LayerLink();
        var root = new OffsetLayer();
        root.Attach(new object());
        var leaderParent = new TransformLayer(Matrix4.Diagonal3Values(2, 2, 1), new Point(10, 20));
        var leader = new LeaderLayer(link, new Point(3, 4));
        leaderParent.Append(leader);
        root.Append(leaderParent);
        var followerParent = new OffsetLayer(new Point(100, 200));
        var follower = new FollowerLayer(link, unlinkedOffset: new Point(7, 9), linkedOffset: new Point(1, 1));
        followerParent.Append(follower);
        root.Append(followerParent);

        Assert.Null(follower.GetLastTransform());

        root.BuildScene(null);

        // Forward chain: T(10, 20) * S(2) * T(3, 4) * T(1, 1); inverse chain: T(100, 200).
        Matrix4 forward = Matrix4.TranslationValues(10, 20, 0);
        forward.Multiply(Matrix4.Diagonal3Values(2, 2, 1));
        forward.TranslateByDouble(3, 4, 0, 1);
        forward.TranslateByDouble(1, 1, 0, 1);
        Matrix4 expected = Matrix4.TranslationValues(-100, -200, 0);
        expected.Multiply(forward);
        Matrix4 lastTransform = Matrix4.TranslationValues(-7, -9, 0);
        lastTransform.Multiply(expected);
        Assert.Equal(lastTransform, follower.GetLastTransform());

        Matrix4 applied = Matrix4.Identity();
        follower.ApplyTransform(new ContainerLayer(), applied);
        Assert.Equal(expected, applied);

        // Unlinked: the follower composites at its unlinked offset and reports no transform.
        leader.Remove();
        root.BuildScene(null);
        Assert.Null(follower.GetLastTransform());
        Matrix4 unlinked = Matrix4.Identity();
        follower.ApplyTransform(new ContainerLayer(), unlinked);
        Assert.Equal(Matrix4.TranslationValues(7, 9, 0), unlinked);
    }

    [Fact]
    public void FollowerLayer_HiddenWhenUnlinkedSkipsItsChildren()
    {
        var root = new ContainerLayer();
        root.Attach(new object());
        var follower = new FollowerLayer(new LayerLink(), showWhenUnlinked: false);
        var child = new ContainerLayer();
        bool childComposited = false;
        child.AddCompositionCallback(_ => childComposited = true);
        follower.Append(child);
        root.Append(follower);
        var engineLayer = new TestEngineLayer();
        follower.EngineLayer = engineLayer;

        root.AddToScene(null, default);

        Assert.False(childComposited);
        Assert.True(engineLayer.Disposed);
        Assert.Null(follower.GetLastTransform());
    }

    [Fact]
    public void FollowerLayer_TracksALeaderThatMovesWithoutRepaintingTheFollower()
    {
        var link = new LayerLink();
        var target = new RenderLeaderLayer(link, new SizedRenderBox(new Size(10, 10)));
        var followerChild = new SizedRenderBox(new Size(5, 5));
        var follower = new RenderFollowerLayer(link, child: followerChild);
        var followerBoundary = new CountingRepaintBoundary(follower);
        var stack = new RenderStack(
            [target, followerBoundary],
            clipBehavior: Clip.None,
            textDirection: TextDirection.Ltr);
        var targetData = (StackParentData)target.parentData!;
        targetData.Left = 20;
        targetData.Top = 30;
        var renderView = new RenderView(new FlutterView(new Size(800, 600))) { Child = stack };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        PumpFrame(pipeline);

        Assert.Equal(new Point(20, 30), followerChild.LocalToGlobal(default));
        int followerPaints = followerBoundary.PaintCount;

        targetData.Left = 50;
        targetData.Top = 60;
        stack.MarkNeedsLayout();
        PumpFrame(pipeline);

        // The follower's repaint boundary was not repainted, yet the composite moved it with the leader.
        Assert.Equal(followerPaints, followerBoundary.PaintCount);
        Assert.Equal(new Point(50, 60), followerChild.LocalToGlobal(default));
    }

    [DebugOnlyFact]
    public void LeaderAfterFollower_Asserts()
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        tester.PumpWidget(new CompositedTransformFollower(
            link,
            child: new CompositedTransformTarget(link, child: new SizedBox(height: 20, width: 20))));

        var error = Assert.IsType<AssertionError>(tester.TakeException());
        Assert.Contains("LeaderLayer anchor must come before FollowerLayer in paint order", error.Message);
    }

    [Fact]
    public void FollowerLayer_TransformQueriesDuringCompositeDoNotCrash()
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        tester.PumpWidget(new CompositedTransformTarget(
            link,
            child: new CompositedTransformFollower(link, child: new GlobalToLocalWhileCompositingWidget())));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void FollowerLayer_DebugCreatorIsNotNull()
    {
        using var tester = new FrameworkDartTester();
        var link = new LayerLink();
        var follower = new CompositedTransformFollower(link, child: new SizedBox(width: 10, height: 10));
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new CompositedTransformTarget(link, child: new SizedBox(width: 10, height: 10))));
        tester.PumpWidget(new Directionality(TextDirection.Ltr, follower));

        var renderObject = (RenderFollowerLayer)tester.ElementOfWidget(follower).RenderObject!;
        Assert.NotNull(renderObject.DebugLayer?.DebugCreator);
    }

    [Fact]
    public void LeaderLayer_CanSwitchToADifferentRenderObjectWithinOneFrame()
    {
        using var tester = new FrameworkDartTester();
        var layerLink = new LayerLink();
        Widget Build(LayerLink? first, LayerLink? second) => new Directionality(
            TextDirection.Ltr,
            new Stack(children: [new LeaderLayerWidget(first), new LeaderLayerWidget(second)]));

        tester.PumpWidget(Build(layerLink, null));
        // Swap the layer link to the second render object in the same frame, then back.
        tester.PumpWidget(Build(null, layerLink));
        tester.Pump();
        tester.PumpWidget(Build(layerLink, null));
        tester.Pump();

        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void LeaderLayer_AppendedByTwoRenderObjectsReportsAnErrorAtTheEndOfTheFrame()
    {
        using var tester = new FrameworkDartTester();
        var layerLink = new LayerLink();
        tester.PumpWidget(new Directionality(
            TextDirection.Ltr,
            new Stack(children: [new LeaderLayerWidget(layerLink), new LeaderLayerWidget(layerLink)])));

        Assert.NotNull(tester.TakeException());
    }

    private static (ContainerLayer Root, ContainerLayer A1, ContainerLayer B1) CreateCallbackTree()
    {
        var root = new ContainerLayer();
        var a1 = new ContainerLayer();
        var a2 = new ContainerLayer();
        var b1 = new ContainerLayer();
        root.Append(a1);
        root.Append(a2);
        a1.Append(b1);
        return (root, a1, b1);
    }

    private static void CheckNeedsAddToScene(Layer layer, Action mutateCallback)
    {
        layer.DebugMarkClean();
        layer.UpdateSubtreeNeedsAddToScene();
        Assert.False(layer.DebugSubtreeNeedsAddToScene);
        mutateCallback();
        layer.UpdateSubtreeNeedsAddToScene();
        Assert.True(layer.DebugSubtreeNeedsAddToScene);
    }

    private static List<string> GetDebugInfo(Layer layer)
    {
        var builder = new DiagnosticPropertiesBuilder();
        layer.DebugFillProperties(builder);
        return builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();
    }

    private static void PumpFrame(PipelineOwner pipeline)
    {
        pipeline.FlushLayout(new Size(800, 600));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();
    }

    private sealed class AlwaysNeedsAddToSceneLayer : ContainerLayer
    {
        protected internal override bool AlwaysNeedsAddToScene => true;
    }

    private sealed class NoRasterizationLayer : Layer
    {
        public override bool SupportsRasterization() => false;

        internal override void AddToScene(Avalonia.Media.DrawingContext? context, Point offset)
        {
        }
    }

    private sealed class TestEngineLayer : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class SizedRenderBox(Size preferredSize) : RenderBox
    {
        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(preferredSize);
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }

    private sealed class CountingRepaintBoundary(RenderBox child) : RenderProxyBox(child)
    {
        public int PaintCount { get; private set; }

        public override bool IsRepaintBoundary => true;

        public override void Paint(PaintingContext context, Point offset)
        {
            PaintCount += 1;
            base.Paint(context, offset);
        }
    }

    private sealed class GlobalToLocalWhileCompositingWidget() : SingleChildRenderObjectWidget(null)
    {
        public override RenderObject CreateRenderObject(BuildContext context)
            => new GlobalToLocalWhileCompositingRenderObject();

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
        }
    }

    private sealed class GlobalToLocalWhileCompositingRenderObject : RenderProxyBox
    {
        public override void Paint(PaintingContext context, Point offset)
        {
            if (Layer == null)
            {
                Layer = new CallbackLayer(ComputeSomething);
            }
            else
            {
                ((CallbackLayer)Layer).ComputeSomething = ComputeSomething;
            }

            context.PushLayer((ContainerLayer)Layer, base.Paint, default);
        }

        private void ComputeSomething()
        {
            // Indeed, use GlobalToLocal to compute some useful data.
            GlobalToLocal(default);
        }
    }

    private sealed class CallbackLayer(Action computeSomething) : ContainerLayer
    {
        public Action ComputeSomething { get; set; } = computeSomething;

        internal override void AddToScene(Avalonia.Media.DrawingContext? context, Point offset)
        {
            // Indeed, need to use the result of this function.
            ComputeSomething();
            base.AddToScene(context, offset);
        }
    }

    // object_test.dart's LeaderLayerRenderObject: a repaint boundary that pushes a fresh LeaderLayer
    // whenever it has a link.
    private sealed class LeaderLayerWidget(LayerLink? layerLink) : SingleChildRenderObjectWidget(null)
    {
        public LayerLink? LayerLink { get; } = layerLink;

        public override RenderObject CreateRenderObject(BuildContext context)
            => new LeaderLayerRenderObject { LayerLink = LayerLink };

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            var leader = (LeaderLayerRenderObject)renderObject;
            leader.LayerLink = LayerLink;
            leader.MarkNeedsPaint();
        }
    }

    private sealed class LeaderLayerRenderObject : RenderProxyBox
    {
        public LayerLink? LayerLink { get; set; }

        public override bool IsRepaintBoundary => true;

        public override void Paint(PaintingContext context, Point offset)
        {
            if (LayerLink != null)
            {
                context.PushLayer(new LeaderLayer(LayerLink), base.Paint, offset);
            }
        }
    }
}
