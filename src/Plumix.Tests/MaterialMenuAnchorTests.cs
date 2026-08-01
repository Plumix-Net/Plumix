using Avalonia;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialMenuAnchorTests
{
    [Fact]
    public void MenuController_UnattachedOperationsMatchFlutterContract()
    {
        var controller = new MenuController();

        controller.Open(new Vector(12.0, 24.0));
        controller.Close();

        Assert.False(controller.IsOpen);
        Assert.Equal(new Vector(12.0, 24.0), controller.Position);
    }

    [Fact]
    public void MenuAnchor_ExposesFlutterDefaults()
    {
        var anchor = new MenuAnchor(
            menuChildren: [],
            child: new SizedBox());

        Assert.Equal(default, anchor.AlignmentOffset);
        Assert.Null(anchor.ReservedPadding);
        Assert.Equal(Clip.HardEdge, anchor.ClipBehavior);
        Assert.False(anchor.ConsumeOutsideTap);
        Assert.True(anchor.CrossAxisUnconstrained);
        Assert.False(anchor.UseRootOverlay);
        Assert.False(anchor.Animated);
    }

    [Fact]
    public void MenuOverlayLayout_DefaultReservedPaddingAndKeyboardInsetsConstrainPanel()
    {
        var layout = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(20.0, 40.0, 80.0, 40.0),
            alignmentOffset: default,
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Ltr,
            viewInsets: new Thickness(0.0, 0.0, 0.0, 120.0),
            position: null);

        BoxConstraints constraints = layout.GetConstraintsForChild(
            BoxConstraints.Tight(new Size(500.0, 360.0)));

        Assert.Equal(484.0, constraints.MaxWidth);
        Assert.Equal(224.0, constraints.MaxHeight);
    }

    [Fact]
    public void MenuOverlayLayout_FlipsAboveWhenPanelWouldOverflowBottom()
    {
        var layout = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(20.0, 300.0, 80.0, 40.0),
            alignmentOffset: default,
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Ltr,
            viewInsets: default,
            position: null);

        Point position = layout.GetPositionForChild(
            new Size(500.0, 360.0),
            new Size(120.0, 100.0));

        Assert.Equal(new Point(20.0, 200.0), position);
    }

    [Fact]
    public void MenuOverlayLayout_CascadingPlacementMirrorsAndClamps()
    {
        var ltr = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(120.0, 40.0, 80.0, 40.0),
            alignmentOffset: new Vector(4.0, 6.0),
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Horizontal,
            textDirection: TextDirection.Ltr,
            viewInsets: default,
            position: null);
        var rtl = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(120.0, 40.0, 80.0, 40.0),
            alignmentOffset: new Vector(-4.0, 6.0),
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Horizontal,
            textDirection: TextDirection.Rtl,
            viewInsets: default,
            position: null);

        Assert.Equal(
            new Point(204.0, 46.0),
            ltr.GetPositionForChild(new Size(500.0, 360.0), new Size(100.0, 120.0)));
        Assert.Equal(
            new Point(16.0, 46.0),
            rtl.GetPositionForChild(new Size(500.0, 360.0), new Size(100.0, 120.0)));
    }

    [Fact]
    public void MenuOverlayLayout_ExplicitPositionOverridesDirectionalPlacementAndOffset()
    {
        var layout = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(20.0, 40.0, 80.0, 40.0),
            alignmentOffset: new Vector(100.0, 100.0),
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Rtl,
            viewInsets: default,
            position: new Vector(12.0, 24.0));

        Point position = layout.GetPositionForChild(
            new Size(500.0, 360.0),
            new Size(120.0, 100.0));

        Assert.Equal(new Point(32.0, 64.0), position);
    }

    [Fact]
    public void MenuOverlayLayout_SelectsClosestDisplayFeatureSubScreen()
    {
        var layout = new MenuOverlayLayoutDelegate(
            anchorRect: new Rect(360.0, 40.0, 80.0, 40.0),
            alignmentOffset: default,
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Ltr,
            viewInsets: default,
            position: null,
            displayFeatures:
            [
                new DisplayFeature(
                    new Rect(246.0, 8.0, 8.0, 344.0),
                    DisplayFeatureType.Hinge),
            ]);

        BoxConstraints constraints = layout.GetConstraintsForChild(
            BoxConstraints.Tight(new Size(500.0, 360.0)));
        Point position = layout.GetPositionForChild(
            new Size(500.0, 360.0),
            new Size(120.0, 100.0));

        Assert.Equal(238.0, constraints.MaxWidth);
        Assert.Equal(new Point(360.0, 80.0), position);
    }

    [Fact]
    public void MenuAnimationCurvesMatchOpeningOpacityAndHeightCheckpoints()
    {
        Curve opacity = Curves.Interval(0.0, 0.1);
        Curve height = Curves.Cubic(0.3, 0.0, 0.0, 1.0);

        Assert.Equal(0.2, opacity(0.02), 6);
        Assert.Equal(1.0, opacity(0.1), 6);
        Assert.InRange(height(0.2) * 160.0, 59.5, 61.5);
        Assert.InRange(height(0.8) * 160.0, 156.5, 158.5);
    }
}
