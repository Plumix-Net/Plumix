using Avalonia;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialFloatingActionButtonLocationTests
{
    private static readonly ScaffoldPrelayoutGeometry Geometry = new(
        ScaffoldSize: new Size(400, 800),
        ContentTop: 80,
        ContentBottom: 720,
        FloatingActionButtonSize: new Size(56, 56),
        BottomSheetSize: new Size(),
        SnackBarSize: new Size(),
        MinInsets: new Thickness(4, 0, 8, 0),
        MinViewPadding: new Thickness(0, 24, 0, 12),
        TextDirection: TextDirection.Ltr);

    [Fact]
    public void StandardLocations_ResolveStartCenterEndAndTopGeometry()
    {
        Assert.Equal(new Point(20, 52), FloatingActionButtonLocation.StartTop.GetOffset(Geometry));
        Assert.Equal(new Point(172, 52), FloatingActionButtonLocation.CenterTop.GetOffset(Geometry));
        Assert.Equal(new Point(320, 52), FloatingActionButtonLocation.EndTop.GetOffset(Geometry));
        Assert.Equal(new Point(172, 648), FloatingActionButtonLocation.CenterFloat.GetOffset(Geometry));
    }

    [Fact]
    public void StandardLocations_RespectRtlInsetsAndMiniAdjustment()
    {
        var rtl = Geometry with { TextDirection = TextDirection.Rtl };

        Assert.Equal(new Point(320, 52), FloatingActionButtonLocation.StartTop.GetOffset(rtl));
        Assert.Equal(new Point(12, 52), FloatingActionButtonLocation.MiniEndTop.GetOffset(rtl));
        Assert.Equal(new Point(328, 656), FloatingActionButtonLocation.MiniEndFloat.GetOffset(Geometry));
    }

    [Fact]
    public void DockedAndContainedLocations_UseBottomNavigationGeometry()
    {
        Assert.Equal(new Point(320, 692), FloatingActionButtonLocation.EndDocked.GetOffset(Geometry));
        Assert.Equal(new Point(320, 726), FloatingActionButtonLocation.EndContained.GetOffset(Geometry));
    }

    [Fact]
    public void FloatingActionButtonAnimator_MatchesScalingAndNoAnimationContracts()
    {
        Point begin = new(10, 20);
        Point end = new(30, 40);

        Assert.Equal(begin, FloatingActionButtonAnimator.Scaling.GetOffset(begin, end, 0.49));
        Assert.Equal(end, FloatingActionButtonAnimator.Scaling.GetOffset(begin, end, 0.5));
        Assert.Equal(0.0, FloatingActionButtonAnimator.Scaling.GetScale(0.5));
        Assert.Equal(0.25, FloatingActionButtonAnimator.Scaling.GetAnimationRestart(0.25));
        Assert.Equal(end, FloatingActionButtonAnimator.NoAnimation.GetOffset(begin, end, 0.0));
        Assert.Equal(1.0, FloatingActionButtonAnimator.NoAnimation.GetScale(0.5));
    }

    [Fact]
    public void Scaffold_UsesFlutterDefaultFabLocationAndAnimator()
    {
        var scaffold = new Scaffold(
            body: new Text("body"),
            floatingActionButton: new FloatingActionButton(child: new Text("+"), onPressed: () => { }));

        Assert.Same(FloatingActionButtonLocation.EndFloat, scaffold.FloatingActionButtonLocation);
        Assert.Same(FloatingActionButtonAnimator.Scaling, scaffold.FloatingActionButtonAnimator);
    }
}
