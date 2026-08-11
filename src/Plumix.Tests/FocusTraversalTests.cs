using Avalonia;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class FocusTraversalTests
{
    [Fact]
    public void ReadingOrderTraversalPolicy_SortsTopBandsBeforeRegistrationOrder()
    {
        var bottom = new FocusNode
        {
            TraversalRect = new Rect(0, 80, 20, 20),
        };
        var middleRight = new FocusNode
        {
            TraversalRect = new Rect(40, 40, 20, 20),
        };
        var middleLeft = new FocusNode
        {
            TraversalRect = new Rect(0, 40, 20, 20),
        };
        var top = new FocusNode
        {
            TraversalRect = new Rect(0, 0, 20, 20),
        };

        IReadOnlyList<FocusNode> sorted = ReadingOrderTraversalPolicy.Sort(
            [bottom, middleRight, middleLeft, top]);

        Assert.Equal([top, middleLeft, middleRight, bottom], sorted);
    }

    [Fact]
    public void ReadingOrderTraversalPolicy_PreservesDegenerateNodeOrderWithoutCrashing()
    {
        var first = new FocusNode
        {
            TraversalRect = default(Rect),
        };
        var second = new FocusNode
        {
            TraversalRect = default(Rect),
        };

        IReadOnlyList<FocusNode> sorted = ReadingOrderTraversalPolicy.Sort([first, second]);

        Assert.Equal([first, second], sorted);
    }
}
