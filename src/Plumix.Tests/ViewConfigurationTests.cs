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
