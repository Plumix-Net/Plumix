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

        Assert.False(controller.IsOpen);
        controller.Close();
        Assert.Throws<InvalidOperationException>(() => controller.Open(new Vector(12.0, 24.0)));
        Assert.Throws<InvalidOperationException>(controller.CloseChildren);
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
        Curve opacity = MenuConstants.PanelOpacityForwardCurve;
        Curve height = MenuConstants.PanelHeightForwardCurve;

        Assert.Equal(0.2, opacity(0.02), 6);
        Assert.Equal(1.0, opacity(0.1), 6);
        Assert.InRange(height(0.2) * 160.0, 59.5, 61.5);
        Assert.InRange(height(0.8) * 160.0, 156.5, 158.5);
    }

    [Fact]
    public void MenuPanelClosingCurvesMatchTheSourceFadeOutAndHeightEaseIn()
    {
        Curve opacity = MenuConstants.PanelOpacityReverseCurve;
        Curve height = MenuConstants.PanelHeightReverseCurve;

        // Curves take the controller value, which runs 1 -> 0 while closing. The panel keeps full
        // opacity for the first 100 of 150 ms (value 1 -> 1/3) and fades over the last 50 ms.
        Assert.Equal(1.0, opacity(1.0), 6);
        Assert.Equal(1.0, opacity(1.0 / 3.0), 6);
        Assert.Equal(0.5, opacity(1.0 / 6.0), 6);
        Assert.Equal(0.0, opacity(0.0), 6);

        // `_TweenCurve(0.35, 1)` remaps the output range, so a full reverse still lands on zero.
        Assert.Equal(1.0, height(1.0), 6);
        Assert.Equal(0.35, height(0.0), 6);
        Assert.InRange(height(50.0 / 150.0) * 160.0, 60.0, 130.0);
    }

    [Fact]
    public void MenuTraversalShortcutsMatchTheSourceEightEntryMap()
    {
        IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts = MenuConstants.TraversalShortcuts;

        Assert.Equal(8, shortcuts.Count);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator("GameButtonA")]);
        Assert.IsType<DismissIntent>(shortcuts[new SingleActivator("Escape")]);
        Assert.IsType<NextFocusIntent>(shortcuts[new SingleActivator("Tab")]);
        Assert.IsType<PreviousFocusIntent>(shortcuts[new SingleActivator("Tab", shift: true)]);
        Assert.Equal(
            TraversalDirection.Down,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowDown")]).Direction);
        Assert.Equal(
            TraversalDirection.Up,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowUp")]).Direction);
        Assert.Equal(
            TraversalDirection.Left,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowLeft")]).Direction);
        Assert.Equal(
            TraversalDirection.Right,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowRight")]).Direction);
    }

    [Fact]
    public void MenuItemFadeCurvesStaggerOverTheSourceIntervals()
    {
        // Flutter asserts item opacities of [0.400, 0.0667, 0, 0] at t = 100 ms of a 500 ms open.
        double[] expectedAt100Ms = [0.4, 1.0 / 15.0, 0.0, 0.0];
        for (int index = 0; index < 4; index++)
        {
            (Curve forward, _) = MenuConstants.ItemFadeCurves(index, 4);
            Assert.Equal(expectedAt100Ms[index], forward(0.2), 3);
        }

        // A single item spans the first half of the opening and the first third of the closing.
        (Curve onlyForward, Curve onlyReverse) = MenuConstants.ItemFadeCurves(0, 1);
        Assert.Equal(1.0, onlyForward(0.5), 6);
        Assert.Equal(1.0, onlyReverse(1.0 / 3.0), 6);

        // The last of four items finishes exactly at the end of the opening animation.
        (Curve lastForward, Curve lastReverse) = MenuConstants.ItemFadeCurves(3, 4);
        Assert.Equal(0.0, lastForward(0.5), 6);
        Assert.Equal(1.0, lastForward(1.0), 6);
        Assert.Equal(1.0, lastReverse(2.0 / 3.0), 6);
    }

    [Fact]
    public void MenuStylePaddingIsDirectionalGeometryResolvedAgainstTextDirection()
    {
        var style = new MenuStyle(
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(
                EdgeInsetsGeometry.DirectionalOnly(start: 10, top: 12, end: 11, bottom: 13)));

        EdgeInsetsGeometry padding = style.Padding!.Resolve(MaterialState.None)!.Value;

        Assert.Equal(new Thickness(10, 12, 11, 13), padding.Resolve(TextDirection.Ltr));
        Assert.Equal(new Thickness(11, 12, 10, 13), padding.Resolve(TextDirection.Rtl));
    }

    [Fact]
    public void MenuOverlayLayout_KeepsTheSettledPositionWhileThePanelHeightGrows()
    {
        MenuOverlayLayoutDelegate Layout(double heightFactor) => new(
            anchorRect: new Rect(20.0, 40.0, 80.0, 40.0),
            alignmentOffset: default,
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Ltr,
            viewInsets: default,
            position: null,
            displayFeatures: null,
            heightFactor: heightFactor);

        Point settled = Layout(1.0).GetPositionForChild(new Size(500.0, 360.0), new Size(120.0, 100.0));
        Point half = Layout(0.5).GetPositionForChild(new Size(500.0, 360.0), new Size(120.0, 50.0));

        // A downward-growing panel starts at the anchor's bottom edge and lerps to its settled spot.
        Assert.Equal(new Point(20.0, 80.0), settled);
        Assert.Equal(settled.X, half.X);
        Assert.Equal(80.0, half.Y);
    }

    [Fact]
    public void MenuOverlayLayout_KeepsAnUpwardGrowingPanelPinnedToItsBottomEdge()
    {
        MenuOverlayLayoutDelegate Layout(double heightFactor) => new(
            anchorRect: new Rect(20.0, 300.0, 80.0, 40.0),
            alignmentOffset: default,
            reservedPadding: new Thickness(8.0),
            placementAxis: Axis.Vertical,
            textDirection: TextDirection.Ltr,
            viewInsets: default,
            position: null,
            displayFeatures: null,
            heightFactor: heightFactor);

        Point settled = Layout(1.0).GetPositionForChild(new Size(500.0, 360.0), new Size(120.0, 100.0));
        Point half = Layout(0.5).GetPositionForChild(new Size(500.0, 360.0), new Size(120.0, 50.0));

        Assert.Equal(new Point(20.0, 200.0), settled);
        Assert.Equal(new Point(20.0, 250.0), half);
    }
}
