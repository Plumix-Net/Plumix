using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/view.dart

namespace Plumix.Tests;

public sealed class ViewConfigurationTests
{
    [Fact]
    public void ViewConfiguration_DefaultsToZeroSizedConstraintsAndUnitPixelRatio()
    {
        var configuration = new ViewConfiguration();

        Assert.Equal(new BoxConstraints(MaxWidth: 0, MaxHeight: 0), configuration.LogicalConstraints);
        Assert.Equal(new BoxConstraints(MaxWidth: 0, MaxHeight: 0), configuration.PhysicalConstraints);
        Assert.Equal(1.0, configuration.DevicePixelRatio);
    }

    [Fact]
    public void ViewConfiguration_FromView_DividesThePhysicalConstraintsByTheDevicePixelRatio()
    {
        var view = new FlutterView(new Size(800, 600), devicePixelRatio: 2.0, viewId: 7);
        ViewConfiguration configuration = ViewConfiguration.FromView(view);

        Assert.Equal(BoxConstraints.Tight(new Size(800, 600)), configuration.PhysicalConstraints);
        Assert.Equal(BoxConstraints.Tight(new Size(400, 300)), configuration.LogicalConstraints);
        Assert.Equal(2.0, configuration.DevicePixelRatio);
        Assert.Equal(new Size(800, 600), configuration.ToPhysicalSize(new Size(400, 300)));
        Assert.Equal(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), configuration.ToMatrix());
    }

    [Fact]
    public void ViewConfiguration_ShouldUpdateMatrix_TracksOnlyTheDevicePixelRatio()
    {
        var baseline = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(100, 100)),
            logicalConstraints: BoxConstraints.Tight(new Size(100, 100)),
            devicePixelRatio: 1.0);
        var sameRatio = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(200, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(200, 200)),
            devicePixelRatio: 1.0);
        var otherRatio = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(100, 100)),
            logicalConstraints: BoxConstraints.Tight(new Size(100, 100)),
            devicePixelRatio: 3.0);

        Assert.False(sameRatio.ShouldUpdateMatrix(baseline));
        Assert.True(otherRatio.ShouldUpdateMatrix(baseline));
    }

    [Fact]
    public void ViewConfiguration_EqualityAndToStringFollowDart()
    {
        var left = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(2, 4)),
            logicalConstraints: BoxConstraints.Tight(new Size(1, 2)),
            devicePixelRatio: 2.0);
        var right = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(2, 4)),
            logicalConstraints: BoxConstraints.Tight(new Size(1, 2)),
            devicePixelRatio: 2.0);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.EndsWith(" at 2.0x", left.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RenderView_TakesItsRootConstraintsFromTheConfigurationTheOwnerWrites()
    {
        var view = new RenderView(new FlutterView(new Size(400, 200), devicePixelRatio: 2.0));
        Assert.False(view.HasConfiguration);
        Assert.Throws<InvalidOperationException>(() => view.Configuration);

        // The single-view owner configures the view from its FlutterView so the first frame can be
        // prepared; a host then hands every frame its own size.
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        Assert.True(view.HasConfiguration);
        Assert.Equal(BoxConstraints.Tight(new Size(200, 100)), view.Configuration.LogicalConstraints);

        pipeline.FlushLayout(new Size(320, 240));

        Assert.Equal(new BoxConstraints(0, 320, 0, 240), view.Configuration.LogicalConstraints);
        Assert.Equal(new BoxConstraints(0, 640, 0, 480), view.Configuration.PhysicalConstraints);
        Assert.Equal(2.0, view.Configuration.DevicePixelRatio);
        Assert.Equal(new BoxConstraints(0, 320, 0, 240), view.Constraints);
    }

    [Fact]
    public void RenderView_ConfigurationCanBeSetBeforePrepareInitialFrame()
    {
        var flutterView = new FlutterView(new Size(400, 200), devicePixelRatio: 2.0);
        var view = new RenderView(flutterView);
        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(200, 100)),
            devicePixelRatio: 2.0);
        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(100, 50)),
            devicePixelRatio: 4.0);
        Assert.Null(view.DebugLayer);

        var pipeline = new PipelineOwner(onSemanticsUpdate: static _ => { });
        pipeline.RootNode = view;
        view.PrepareInitialFrame();
        pipeline.FlushLayout();

        Assert.NotNull(view.DebugLayer);
        Assert.Equal(new Size(100, 50), view.Size);
    }

    [Fact]
    public void RenderView_PrepareInitialFrame_RequiresAnOwnerAndAConfigurationAndRunsOnce()
    {
        var view = new RenderView(new FlutterView(new Size(400, 200)));
        Assert.Throws<AssertionError>(view.PrepareInitialFrame);

        var pipeline = new PipelineOwner(onSemanticsUpdate: static _ => { });
        pipeline.RootNode = view;
        Assert.Throws<AssertionError>(view.PrepareInitialFrame);

        view.Configuration = ViewConfiguration.FromView(view.FlutterView);
        view.PrepareInitialFrame();
        Assert.Throws<AssertionError>(view.PrepareInitialFrame);
    }

    [Fact]
    public void RenderView_ReplacesTheRootLayerOnlyWhenTheDevicePixelRatioChanges()
    {
        var view = new RenderView(new FlutterView(new Size(400, 200), devicePixelRatio: 2.0));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout();
        Layer? rootLayer = view.DebugLayer;
        Assert.NotNull(rootLayer);

        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(200, 100)),
            devicePixelRatio: 2.0);
        Assert.Same(rootLayer, view.DebugLayer);

        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(800, 400)),
            logicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            devicePixelRatio: 2.0);
        Assert.Same(rootLayer, view.DebugLayer);
        pipeline.FlushLayout();
        Assert.Equal(new Size(400, 200), view.Size);

        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(80, 40)),
            devicePixelRatio: 5.0);
        Assert.NotSame(rootLayer, view.DebugLayer);
        Assert.Same(view.DebugLayer, pipeline.RootLayer);
    }

    [Fact]
    public void RenderView_InvokesDebugPaintCallbacksUntilTheyAreRemoved()
    {
        var view = new RenderView(new FlutterView(new Size(40, 20)));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        int calls = 0;
        DebugPaintCallback callback = (_, _, renderView) =>
        {
            Assert.Same(view, renderView);
            calls += 1;
        };

        RenderView.DebugAddPaintCallback(callback);
        try
        {
            pipeline.FlushLayout();
            pipeline.FlushCompositingBits();
            pipeline.FlushPaint();
            pipeline.CompositeFrame();
            Assert.Equal(1, calls);
        }
        finally
        {
            RenderView.DebugRemovePaintCallback(callback);
        }

        view.MarkNeedsPaint();
        pipeline.FlushLayout();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.CompositeFrame();
        Assert.Equal(1, calls);
    }

    [Fact]
    public void RenderView_SizesItselfFromItsConfigurationAndChild()
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 30, height: 60));
        var view = new RenderView(
            new FlutterView(new Size(300, 600), devicePixelRatio: 3.0),
            child: child);
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        pipeline.FlushLayout();
        Assert.Equal(new Size(100, 200), view.Size);
        Assert.False(child.DebugCanParentUseSize);

        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Unbounded,
            logicalConstraints: BoxConstraints.Unbounded,
            devicePixelRatio: 3.0);
        pipeline.FlushLayout();
        Assert.Equal(new Size(30, 60), view.Size);
        Assert.True(child.DebugCanParentUseSize);

        view.Child = null;
        pipeline.FlushLayout();
        Assert.Equal(new Size(0, 0), view.Size);
    }

    [Fact]
    public void RenderView_AccountsForDevicePixelRatioInPaintBounds()
    {
        // view_test.dart: "accounts for device pixel ratio in paintBounds".
        var view = new RenderView(
            new FlutterView(new Size(800, 600), devicePixelRatio: 2.0),
            child: new RenderAspectRatio(aspectRatio: 1.0));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout();

        Size logicalSize = view.Size;
        double devicePixelRatio = view.Configuration.DevicePixelRatio;
        Assert.Equal(
            new Rect(0, 0, logicalSize.Width * devicePixelRatio, logicalSize.Height * devicePixelRatio),
            view.PaintBounds);
    }

    [Fact]
    public void RenderView_ConstraintsAreDerivedFromConfiguration()
    {
        // view_test.dart: "Constraints are derived from configuration".
        var constraints = new BoxConstraints(MinWidth: 1, MaxWidth: 2, MinHeight: 3, MaxHeight: 4);
        const double devicePixelRatio = 3.0;
        var config = new ViewConfiguration(
            logicalConstraints: constraints,
            physicalConstraints: constraints * devicePixelRatio,
            devicePixelRatio: devicePixelRatio);

        // Configuration set via setter.
        var view = new RenderView(new FlutterView(new Size(800, 600)));
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => view.Constraints);
        Assert.Contains("RenderView has not been given a configuration yet", error.Message);
        view.Configuration = config;
        Assert.Equal(constraints, view.Constraints);

        // Configuration set in constructor.
        var view2 = new RenderView(new FlutterView(new Size(800, 600)), configuration: config);
        Assert.Equal(constraints, view2.Constraints);
    }

    [Fact]
    public void RenderView_RootLayerIsATransformLayerCarryingTheDevicePixelRatio()
    {
        var view = new RenderView(new FlutterView(new Size(400, 200), devicePixelRatio: 2.0));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        TransformLayer rootLayer = Assert.IsType<TransformLayer>(view.DebugLayer);
        Assert.Equal(Matrix4.Diagonal3Values(2.0, 2.0, 1.0), rootLayer.Transform);
        Assert.Same(rootLayer, pipeline.RootLayer);

        view.Configuration = new ViewConfiguration(
            physicalConstraints: BoxConstraints.Tight(new Size(400, 200)),
            logicalConstraints: BoxConstraints.Tight(new Size(100, 50)),
            devicePixelRatio: 4.0);
        TransformLayer replaced = Assert.IsType<TransformLayer>(view.DebugLayer);
        Assert.NotSame(rootLayer, replaced);
        Assert.Equal(Matrix4.Diagonal3Values(4.0, 4.0, 1.0), replaced.Transform);
    }

    [Fact]
    public void RenderView_PaintsItsChildUnderTheRootTransformButKeepsGlobalCoordinatesLogical()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(30, 20)));
        var center = new RenderPositionedBox(child: child);
        var view = new RenderView(new FlutterView(new Size(200, 100), devicePixelRatio: 2.0), child: center);
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout();

        // The view gives its child plain parent data: it is a bare RenderObject, not a RenderBox.
        Assert.IsNotType<BoxParentData>(center.parentData);
        // Dart's `semanticBounds`: the logical box under the root transform.
        Assert.Equal(new Rect(0, 0, 200, 100), view.SemanticBoundsForSemantics);

        Matrix4 toView = child.GetTransformTo(view);
        Assert.Equal(new Point(70, 30), MatrixUtils.TransformPoint(toView, new Point(0, 0)));
        // A null target stops below the root, so global coordinates stay in logical pixels.
        Assert.Equal(new Point(35, 15), MatrixUtils.TransformPoint(child.GetTransformTo(null), new Point(0, 0)));
        Assert.Equal(new Point(35, 15), child.LocalToGlobal(new Point(0, 0)));
        Assert.Equal(new Point(0, 0), child.GlobalToLocal(new Point(35, 15)));
    }

    [Fact]
    public void RenderView_HitTest_AlwaysAddsItselfAfterItsChild()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(30, 20)));
        var view = new RenderView(
            new FlutterView(new Size(200, 100), devicePixelRatio: 2.0),
            child: new RenderPointerListener(behavior: HitTestBehavior.Opaque, child: child));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout();

        // Positions are logical: (60, 40) is inside the 100x50 logical view.
        var inside = new HitTestResult();
        Assert.True(view.HitTest(inside, new Point(60, 40)));
        Assert.Equal(2, inside.Path.Count);
        Assert.IsType<RenderPointerListener>(inside.Path[0].Target);
        Assert.Same(view, inside.Path[1].Target);

        // Dart's `hitTest` has no bounds check: a miss still reports the view and returns true.
        var outside = new HitTestResult();
        Assert.True(view.HitTest(outside, new Point(500, 500)));
        Assert.Same(view, Assert.Single(outside.Path).Target);

        var empty = new RenderView(new FlutterView(new Size(10, 10)));
        var emptyResult = new HitTestResult();
        Assert.True(empty.HitTest(emptyResult, new Point(-1, -1)));
        Assert.Same(empty, Assert.Single(emptyResult.Path).Target);
    }

    [DebugOnlyFact]
    public void RenderView_DebugFillProperties_ReportsTheViewMetricsAndConfiguration()
    {
        var view = new RenderView(new FlutterView(new Size(400, 200), devicePixelRatio: 2.0));
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(200, 100));

        var properties = new DiagnosticPropertiesBuilder();
        view.DebugFillProperties(properties);
        List<string> names = [.. properties.Properties.Select(property => property.Name ?? string.Empty)];

        Assert.Contains("view size", names);
        Assert.Contains("device pixel ratio", names);
        Assert.Contains("configuration", names);
        Assert.Contains(properties.Properties, property => property.ToDescription().Contains("debug mode enabled"));
    }
}
