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
        var view = new RenderView { FlutterView = new FlutterView(new Size(400, 200), devicePixelRatio: 2.0) };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);

        Assert.False(view.HasConfiguration);
        Assert.Throws<InvalidOperationException>(() => view.Configuration);

        pipeline.FlushLayout(new Size(320, 240));

        Assert.True(view.HasConfiguration);
        Assert.Equal(new BoxConstraints(0, 320, 0, 240), view.Configuration.LogicalConstraints);
        Assert.Equal(new BoxConstraints(0, 640, 0, 480), view.Configuration.PhysicalConstraints);
        Assert.Equal(2.0, view.Configuration.DevicePixelRatio);
    }

    [DebugOnlyFact]
    public void RenderView_DebugFillProperties_ReportsTheViewMetricsAndConfiguration()
    {
        var view = new RenderView { FlutterView = new FlutterView(new Size(400, 200), devicePixelRatio: 2.0) };
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
