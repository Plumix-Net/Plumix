using Avalonia.Media;
using Plumix.Rendering;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/gradient_test.dart
public sealed class GradientTests
{
    [Fact]
    public void BoxDecoration_LerpsFrameworkLinearGradients()
    {
        var from = new BoxDecoration(Gradient: new LinearGradient(
            [Colors.Black, Colors.Red],
            begin: Alignment.TopLeft,
            end: Alignment.BottomRight));
        var to = new BoxDecoration(Gradient: new LinearGradient(
            [Colors.White, Colors.Blue],
            begin: Alignment.TopRight,
            end: Alignment.BottomLeft));

        BoxDecoration midpoint = Assert.IsType<BoxDecoration>(BoxDecoration.Lerp(from, to, 0.5));
        LinearGradient gradient = Assert.IsType<LinearGradient>(midpoint.Gradient);
        Assert.Equal(new Alignment(0, -1), gradient.Begin);
        Assert.Equal(new Alignment(0, 1), gradient.End);
        Assert.Equal(Color.FromRgb(128, 128, 128), gradient.Colors[0]);
        Assert.Equal(Color.FromRgb(128, 0, 128), gradient.Colors[1]);

        BoxDecoration faded = Assert.IsType<BoxDecoration>(BoxDecoration.Lerp(null, to, 0.5));
        LinearGradient fadedGradient = Assert.IsType<LinearGradient>(faded.Gradient);
        Assert.Equal(Color.FromArgb(128, 255, 255, 255), fadedGradient.Colors[0]);
        Assert.Equal(Color.FromArgb(128, 0, 0, 255), fadedGradient.Colors[1]);
        Assert.Throws<ArgumentException>(() => new LinearGradient([Colors.Red]));
        Assert.Throws<ArgumentException>(() => new LinearGradient(
            [Colors.Red, Colors.Blue],
            stops: [0.0]));
    }
}
