using Avalonia;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/_window_positioner.dart
// Mirrors flutter/packages/flutter/test/widgets/window_positioner_test.dart, every test.

public sealed class WindowPositionerTests
{
    private static readonly Rect ClientDisplayArea = new(0, 0, 800, 600);
    private static readonly Rect ClientParentRect = new(200, 150, 400, 300);
    private static readonly Size ClientChildSize = new(100, 50);

    private static readonly Rect DisplayArea = new(0, 0, 640, 480);
    private static readonly Rect ParentRect = new(20, 40, 600, 400);
    private static readonly Size ChildSize = new(300, 300);

    private static readonly Rect RectNearRhs = AnchorRectFor(new Rect(590, 20, 10, 20));
    private static readonly Rect RectNearLeft = AnchorRectFor(new Rect(0, 20, 20, 20));
    private static readonly Rect RectNearAll = AnchorRectFor(new Rect(0, 20, 600, 380));
    private static readonly Rect RectNearBottom = AnchorRectFor(new Rect(20, 380, 20, 20));
    private static readonly Rect RectNearBottomRight = AnchorRectFor(new Rect(400, 380, 200, 20));

    public static TheoryData<WindowPositionerAnchor, WindowPositionerAnchor> AnchorPairs()
    {
        var data = new TheoryData<WindowPositionerAnchor, WindowPositionerAnchor>();
        foreach (WindowPositionerAnchor parentAnchor in Enum.GetValues<WindowPositionerAnchor>())
        {
            foreach (WindowPositionerAnchor childAnchor in Enum.GetValues<WindowPositionerAnchor>())
            {
                data.Add(parentAnchor, childAnchor);
            }
        }

        return data;
    }

    [Fact]
    public void WindowPositioner_ToString()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopLeft,
            childAnchor: WindowPositionerAnchor.BottomRight,
            offset: new Point(1.2, 3.4),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(
                flipX: true,
                flipY: true,
                slideX: true,
                slideY: true,
                resizeX: true,
                resizeY: true));

        Assert.Equal(
            "WindowPositioner(parentAnchor: WindowPositionerAnchor.topLeft, "
            + "childAnchor: WindowPositionerAnchor.bottomRight, offset: Offset(1.2, 3.4), "
            + "constraintAdjustment: WindowPositionerConstraintAdjustment(flipX: true, flipY: true, "
            + "slideX: true, slideY: true, resizeX: true, resizeY: true))",
            positioner.ToString());
    }

    [Fact]
    public void WindowPositioner_DefaultsAndCopyWith()
    {
        var positioner = new WindowPositioner();
        Assert.Equal(WindowPositionerAnchor.Center, positioner.ParentAnchor);
        Assert.Equal(WindowPositionerAnchor.Center, positioner.ChildAnchor);
        Assert.Equal(default, positioner.Offset);
        Assert.False(positioner.ConstraintAdjustment.FlipX);
        Assert.False(positioner.ConstraintAdjustment.ResizeY);

        WindowPositioner copy = positioner.CopyWith(offset: new Point(3, 4));
        Assert.Equal(new Point(3, 4), copy.Offset);
        Assert.Equal(WindowPositionerAnchor.Center, copy.ParentAnchor);
        Assert.Same(positioner.ConstraintAdjustment, copy.ConstraintAdjustment);
    }

    [Fact]
    public void Client_AnchorsToParentGivenAnchorRectangleRightOfParent()
    {
        Rect anchorRect = RectFromCenter(new Point(595, 300), 10, 10);
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideY: true, resizeX: true));

        Rect placed = positioner.PlaceWindow(ClientChildSize, anchorRect, ClientParentRect, ClientDisplayArea);

        Assert.Equal(anchorRect.TopRight, placed.TopLeft);
        Assert.Equal(ClientChildSize, placed.Size);
    }

    [Fact]
    public void Client_AnchorsToParentGivenAnchorRectangleAboveParent()
    {
        Rect anchorRect = RectFromCenter(new Point(400, 145), 10, 10);
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.BottomRight,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideX: true));

        Rect placed = positioner.PlaceWindow(ClientChildSize, anchorRect, ClientParentRect, ClientDisplayArea);

        Assert.Equal(anchorRect.BottomRight - new Point(100, 50), placed.TopLeft);
        Assert.Equal(ClientChildSize, placed.Size);
    }

    [Fact]
    public void Client_AnchorsToParentGivenOffsetRightOfParent()
    {
        var anchorRect = new Rect(590, 300, 10, 10);
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            offset: new Point(10, 0),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideY: true, resizeX: true));

        Rect placed = positioner.PlaceWindow(ClientChildSize, anchorRect, ClientParentRect, ClientDisplayArea);

        Assert.Equal(anchorRect.TopRight, placed.TopLeft);
        Assert.Equal(ClientChildSize, placed.Size);
    }

    [Fact]
    public void Client_AnchorsToParentGivenOffsetAboveParent()
    {
        var anchorRect = new Rect(400, 150, 10, 10);
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.BottomRight,
            offset: new Point(0, -10),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideX: true));

        Rect placed = positioner.PlaceWindow(ClientChildSize, anchorRect, ClientParentRect, ClientDisplayArea);

        Assert.Equal(new Point(310, 100), placed.TopLeft);
        Assert.Equal(ClientChildSize, placed.Size);
    }

    [Fact]
    public void Client_AnchorsToParentGivenAnchorRectangleAndOffsetBelowLeftParent()
    {
        var anchorRect = new Rect(190, 450, 10, 10);
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomLeft,
            childAnchor: WindowPositionerAnchor.TopRight,
            offset: new Point(-10, 10),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeX: true, resizeY: true));

        Rect placed = positioner.PlaceWindow(ClientChildSize, anchorRect, ClientParentRect, ClientDisplayArea);

        Assert.Equal(ClientParentRect.BottomLeft - new Point(100, 0), placed.TopLeft);
        Assert.Equal(ClientChildSize, placed.Size);
    }

    [Theory]
    [MemberData(nameof(AnchorPairs))]
    public void CanAttachByEveryAnchorGivenNoConstraintAdjustment(
        WindowPositionerAnchor parentAnchor,
        WindowPositionerAnchor childAnchor)
    {
        Rect anchorRect = AnchorRectFor(new Rect(100, 50, 20, 20));
        var positioner = new WindowPositioner(parentAnchor: parentAnchor, childAnchor: childAnchor);

        Rect placed = positioner.PlaceWindow(ChildSize, anchorRect, ParentRect, DisplayArea);

        Assert.Equal(AnchorPositionFor(parentAnchor, anchorRect), AnchorPositionFor(childAnchor, placed));
    }

    [Fact]
    public void PlacementIsFlippedGivenAnchorRectangleNearRightSideAndOffset()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            offset: new Point(42, 13),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(flipX: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearRhs, ParentRect, DisplayArea);

        Assert.Equal(new Point(RectNearRhs.Left - 300 - 42, RectNearRhs.Top + 13), placed.TopLeft);
    }

    [Fact]
    public void PlacementIsFlippedGivenAnchorRectangleNearBottomAndOffset()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomLeft,
            childAnchor: WindowPositionerAnchor.TopLeft,
            offset: new Point(42, 13),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(flipY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearBottom, ParentRect, DisplayArea);

        Assert.Equal(new Point(RectNearBottom.Left + 42, RectNearBottom.Top - 300 - 13), placed.TopLeft);
    }

    [Fact]
    public void PlacementIsFlippedBothWaysGivenAnchorRectangleNearBottomRightAndOffset()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            offset: new Point(42, 13),
            constraintAdjustment: new WindowPositionerConstraintAdjustment(flipX: true, flipY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearBottomRight, ParentRect, DisplayArea);

        Assert.Equal(RectNearBottomRight.TopLeft - new Point(300, 300) - new Point(42, 13), placed.TopLeft);
        Assert.Equal(new Point(78, 107), placed.TopLeft);
    }

    [Fact]
    public void PlacementCanSlideInXGivenAnchorRectangleNearRightSide()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideX: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearRhs, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Right - ChildSize.Width, placed.Left);
        Assert.Equal(new Rect(340, 60, 300, 300), placed);
    }

    [Fact]
    public void PlacementCanSlideInXGivenAnchorRectangleNearLeftSide()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopLeft,
            childAnchor: WindowPositionerAnchor.TopRight,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideX: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearLeft, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Left, placed.Left);
        Assert.Equal(new Rect(0, 60, 300, 300), placed);
    }

    [Fact]
    public void PlacementCanSlideInYGivenAnchorRectangleNearBottom()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomLeft,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearBottom, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Bottom - ChildSize.Height, placed.Top);
        Assert.Equal(new Rect(40, 180, 300, 300), placed);
    }

    [Fact]
    public void PlacementCanSlideInYGivenAnchorRectangleNearTop()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopLeft,
            childAnchor: WindowPositionerAnchor.BottomLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearAll, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Top, placed.Top);
        Assert.Equal(new Rect(20, 0, 300, 300), placed);
    }

    [Fact]
    public void PlacementCanSlideInXAndYGivenAnchorRectangleNearBottomRightAndOffset()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomLeft,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(slideX: true, slideY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearBottomRight, ParentRect, DisplayArea);

        Assert.Equal(
            new Point(DisplayArea.Right - ChildSize.Width, DisplayArea.Bottom - ChildSize.Height),
            placed.TopLeft);
    }

    [Fact]
    public void PlacementCanResizeInXGivenAnchorRectangleNearRightSide()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeX: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearRhs, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Right - RectNearRhs.Right, placed.Width);
        Assert.Equal(new Rect(620, 60, 20, 300), placed);
    }

    [Fact]
    public void PlacementCanResizeInXGivenAnchorRectangleNearLeftSide()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopLeft,
            childAnchor: WindowPositionerAnchor.TopRight,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeX: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearLeft, ParentRect, DisplayArea);

        Assert.Equal(RectNearLeft.Left - DisplayArea.Left, placed.Width);
        Assert.Equal(new Rect(0, 60, 20, 300), placed);
    }

    [Fact]
    public void PlacementCanResizeInYGivenAnchorRectangleNearBottom()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomLeft,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearAll, ParentRect, DisplayArea);

        Assert.Equal(DisplayArea.Bottom - RectNearAll.Bottom, placed.Height);
        Assert.Equal(new Rect(20, 440, 300, 40), placed);
    }

    [Fact]
    public void PlacementCanResizeInYGivenAnchorRectangleNearTop()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.TopLeft,
            childAnchor: WindowPositionerAnchor.BottomLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearAll, ParentRect, DisplayArea);

        Assert.Equal(RectNearAll.Top - DisplayArea.Top, placed.Height);
        Assert.Equal(new Rect(20, 0, 300, 60), placed);
    }

    [Fact]
    public void PlacementCanResizeInXAndYGivenAnchorRectangleNearBottomRightAndOffset()
    {
        var positioner = new WindowPositioner(
            parentAnchor: WindowPositionerAnchor.BottomRight,
            childAnchor: WindowPositionerAnchor.TopLeft,
            constraintAdjustment: new WindowPositionerConstraintAdjustment(resizeX: true, resizeY: true));

        Rect placed = positioner.PlaceWindow(ChildSize, RectNearBottomRight, ParentRect, DisplayArea);

        Assert.Equal(
            new Size(DisplayArea.Right - RectNearBottomRight.Right, DisplayArea.Bottom - RectNearBottomRight.Bottom),
            placed.Size);
        Assert.Equal(new Rect(620, 440, 20, 40), placed);
    }

    private static Rect AnchorRectFor(Rect rect) => rect.Translate(new Vector(20, 40));

    private static Rect RectFromCenter(Point center, double width, double height) =>
        new(center.X - width / 2, center.Y - height / 2, width, height);

    private static Point AnchorPositionFor(WindowPositionerAnchor anchor, Rect rect)
    {
        return anchor switch
        {
            WindowPositionerAnchor.Center => rect.Center,
            WindowPositionerAnchor.Top => new Point(rect.Center.X, rect.Top),
            WindowPositionerAnchor.Bottom => new Point(rect.Center.X, rect.Bottom),
            WindowPositionerAnchor.Left => new Point(rect.Left, rect.Center.Y),
            WindowPositionerAnchor.Right => new Point(rect.Right, rect.Center.Y),
            WindowPositionerAnchor.TopLeft => rect.TopLeft,
            WindowPositionerAnchor.BottomLeft => rect.BottomLeft,
            WindowPositionerAnchor.TopRight => rect.TopRight,
            _ => rect.BottomRight,
        };
    }
}
