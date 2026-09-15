using Avalonia;
using Plumix.Rendering;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/painting/box_fit_test.dart

namespace Plumix.Tests;

public sealed class BoxFitTests
{
    [Theory]
    [InlineData(BoxFit.ScaleDown, 100, 1000, 200, 2000, 100, 1000, 100, 1000)]
    [InlineData(BoxFit.ScaleDown, 300, 3000, 200, 2000, 300, 3000, 200, 2000)]
    [InlineData(BoxFit.FitWidth, 2000, 400, 1000, 100, 2000, 200, 1000, 100)]
    [InlineData(BoxFit.FitWidth, 2000, 400, 1000, 300, 2000, 400, 1000, 200)]
    [InlineData(BoxFit.FitHeight, 400, 2000, 100, 1000, 200, 2000, 100, 1000)]
    [InlineData(BoxFit.FitHeight, 400, 2000, 300, 1000, 400, 2000, 200, 1000)]
    public void ApplyBoxFit_MatchesFlutterTestSizes(
        BoxFit fit,
        double inputWidth,
        double inputHeight,
        double outputWidth,
        double outputHeight,
        double sourceWidth,
        double sourceHeight,
        double destinationWidth,
        double destinationHeight)
    {
        FittedSizes result = BoxFitUtils.ApplyBoxFit(
            fit,
            new Size(inputWidth, inputHeight),
            new Size(outputWidth, outputHeight));

        Assert.Equal(new Size(sourceWidth, sourceHeight), result.Source);
        Assert.Equal(new Size(destinationWidth, destinationHeight), result.Destination);
    }

    [Theory]
    [InlineData(-400, 2000, 100, 1000)]
    [InlineData(400, -2000, 100, 1000)]
    [InlineData(400, 2000, -100, 1000)]
    [InlineData(400, 2000, 100, -1000)]
    [InlineData(0, 2000, 100, 1000)]
    [InlineData(400, 0, 100, 1000)]
    [InlineData(400, 2000, 0, 1000)]
    [InlineData(400, 2000, 100, 0)]
    public void ApplyBoxFit_NonPositiveDimension_ReturnsTwoZeroSizes(
        double inputWidth,
        double inputHeight,
        double outputWidth,
        double outputHeight)
    {
        foreach (BoxFit fit in Enum.GetValues<BoxFit>())
        {
            FittedSizes result = BoxFitUtils.ApplyBoxFit(
                fit,
                new Size(inputWidth, inputHeight),
                new Size(outputWidth, outputHeight));

            Assert.Equal(new Size(), result.Source);
            Assert.Equal(new Size(), result.Destination);
        }
    }

    [Fact]
    public void FittedSizes_IsAnImmutableReferencePair_LikeDart()
    {
        var first = new FittedSizes(new Size(10, 20), new Size(30, 40));
        var second = new FittedSizes(new Size(10, 20), new Size(30, 40));

        Assert.NotEqual(first, second);
        Assert.Equal(new Size(10, 20), first.Source);
        Assert.Equal(new Size(30, 40), first.Destination);
    }

    [Theory]
    [InlineData(BoxFit.Fill, 200, 100, 100, 200, 200, 100, 100, 200)]
    [InlineData(BoxFit.Contain, 200, 100, 100, 200, 200, 100, 100, 50)]
    [InlineData(BoxFit.Cover, 200, 100, 100, 200, 50, 100, 100, 200)]
    [InlineData(BoxFit.None, 200, 100, 100, 200, 100, 100, 100, 100)]
    public void ApplyBoxFit_OtherBranches_PreserveSourceAndDestinationSemantics(
        BoxFit fit,
        double inputWidth,
        double inputHeight,
        double outputWidth,
        double outputHeight,
        double sourceWidth,
        double sourceHeight,
        double destinationWidth,
        double destinationHeight)
    {
        ApplyBoxFit_MatchesFlutterTestSizes(
            fit,
            inputWidth,
            inputHeight,
            outputWidth,
            outputHeight,
            sourceWidth,
            sourceHeight,
            destinationWidth,
            destinationHeight);
    }
}
