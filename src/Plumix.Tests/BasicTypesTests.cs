using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/painting/basic_types.dart

namespace Plumix.Tests;

public sealed class BasicTypesTests
{
    [Fact]
    public void EnumOrderMatchesDart()
    {
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<RenderComparison>().Select(value => (int)value));
        Assert.Equal([0, 1], Enum.GetValues<Axis>().Select(value => (int)value));
        Assert.Equal([0, 1], Enum.GetValues<VerticalDirection>().Select(value => (int)value));
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<AxisDirection>().Select(value => (int)value));
    }

    [Theory]
    [InlineData(Axis.Horizontal, Axis.Vertical)]
    [InlineData(Axis.Vertical, Axis.Horizontal)]
    public void FlipAxis_ReturnsOpposite(Axis input, Axis expected)
    {
        Assert.Equal(expected, BasicTypes.FlipAxis(input));
    }

    [Theory]
    [InlineData(AxisDirection.Up, Axis.Vertical, AxisDirection.Down, true)]
    [InlineData(AxisDirection.Right, Axis.Horizontal, AxisDirection.Left, false)]
    [InlineData(AxisDirection.Down, Axis.Vertical, AxisDirection.Up, false)]
    [InlineData(AxisDirection.Left, Axis.Horizontal, AxisDirection.Right, true)]
    public void AxisDirectionHelpers_MatchDart(
        AxisDirection input,
        Axis expectedAxis,
        AxisDirection opposite,
        bool reversed)
    {
        Assert.Equal(expectedAxis, BasicTypes.AxisDirectionToAxis(input));
        Assert.Equal(opposite, BasicTypes.FlipAxisDirection(input));
        Assert.Equal(reversed, BasicTypes.AxisDirectionIsReversed(input));
        Assert.Equal(expectedAxis, ScrollDirectionUtils.AxisDirectionToAxis(input));
        Assert.Equal(reversed, ScrollDirectionUtils.AxisDirectionIsReversed(input));
    }

    [Theory]
    [InlineData(TextDirection.Ltr, AxisDirection.Right)]
    [InlineData(TextDirection.Rtl, AxisDirection.Left)]
    public void TextDirectionToAxisDirection_MatchesReadingOrder(TextDirection input, AxisDirection expected)
    {
        Assert.Equal(expected, BasicTypes.TextDirectionToAxisDirection(input));
    }

    [Fact]
    public void InvalidEnumValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BasicTypes.FlipAxis((Axis)2));
        Assert.Throws<ArgumentOutOfRangeException>(() => BasicTypes.AxisDirectionToAxis((AxisDirection)4));
        Assert.Throws<ArgumentOutOfRangeException>(() => BasicTypes.FlipAxisDirection((AxisDirection)4));
        Assert.Throws<ArgumentOutOfRangeException>(() => BasicTypes.AxisDirectionIsReversed((AxisDirection)4));
        Assert.Throws<ArgumentOutOfRangeException>(() => BasicTypes.TextDirectionToAxisDirection((TextDirection)2));
    }
}
