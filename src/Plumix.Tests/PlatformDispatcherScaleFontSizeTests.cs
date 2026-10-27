using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/engine/src/flutter/lib/ui/platform_dispatcher.dart (scaleFontSize,
// _scaleAndMemoize) and flutter/packages/flutter/lib/src/widgets/media_query.dart (SystemTextScaler).

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PlatformDispatcherScaleFontSizeTests : IDisposable
{
    private readonly PlatformDispatcher _dispatcher = PlatformDispatcher.Instance;
    private readonly List<double> _platformCalls = [];

    public PlatformDispatcherScaleFontSizeTests()
    {
        _dispatcher.ResetScaledFontSizeForTests();
        _dispatcher.UpdateTextScaleFactor(1.0);
    }

    public void Dispose()
    {
        _dispatcher.SetTextScaleFactorTestValue(null);
        _dispatcher.ResetScaledFontSizeForTests();
        _dispatcher.UpdateTextScaleFactor(1.0);
    }

    // An Android-14-like curve: sizes up to 20 double, larger ones grow by 20 plus half the excess.
    private double NonlinearCurve(double fontSize)
    {
        _platformCalls.Add(fontSize);
        return fontSize <= 20 ? fontSize * 2 : 40 + ((fontSize - 20) / 2);
    }

    [Fact]
    public void WithoutAPlatformFunction_ScalesLinearly()
    {
        _dispatcher.UpdateTextScaleFactor(2.0);

        Assert.Equal(28.0, _dispatcher.ScaleFontSize(14.0));
        Assert.Equal(29.0, _dispatcher.ScaleFontSize(14.5));
    }

    [Fact]
    public void AtAFactorOfOne_ReturnsTheUnscaledSizeWithoutAskingThePlatform()
    {
        _dispatcher.UpdateTextScaleFactor(1.0, NonlinearCurve);

        Assert.Equal(32.0, _dispatcher.ScaleFontSize(32.0));
        Assert.Empty(_platformCalls);
    }

    [Fact]
    public void IntegralSizes_AreScaledByThePlatformAndMemoized()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);

        Assert.Equal(28.0, _dispatcher.ScaleFontSize(14.0));
        Assert.Equal(46.0, _dispatcher.ScaleFontSize(32.0));
        Assert.Equal(46.0, _dispatcher.ScaleFontSize(32.0));

        Assert.Equal([14.0, 32.0], _platformCalls);
    }

    [Fact]
    public void FractionalSizes_InterpolateBetweenTheirFloorAndCeiling()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);

        // floor 20 -> 40, ceil 21 -> 40.5.
        Assert.Equal(40.25, _dispatcher.ScaleFontSize(20.5));
        Assert.Equal([20.0, 21.0], _platformCalls);
    }

    [Fact]
    public void ChangingTheFactor_DropsTheMemoizedSizes()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);
        _dispatcher.ScaleFontSize(14.0);

        // A configuration with the same factor keeps the memoized sizes, as Dart's does.
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);
        _dispatcher.ScaleFontSize(14.0);
        Assert.Single(_platformCalls);

        _dispatcher.UpdateTextScaleFactor(1.5, NonlinearCurve);
        _dispatcher.ScaleFontSize(14.0);
        Assert.Equal([14.0, 14.0], _platformCalls);
    }

    [Fact]
    public void AConfigurationWithoutAFunction_KeepsThePreviousOne()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);
        _dispatcher.UpdateTextScaleFactor(3.0);

        Assert.Equal(46.0, _dispatcher.ScaleFontSize(32.0));
    }

    [Fact]
    public void ATestValue_ScalesLinearly()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);
        _dispatcher.SetTextScaleFactorTestValue(3.0);

        Assert.Equal(96.0, _dispatcher.ScaleFontSize(32.0));
        Assert.Empty(_platformCalls);
    }

    [Fact]
    public void ScaleFontSize_RejectsNegativeAndNonFiniteSizes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _dispatcher.ScaleFontSize(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dispatcher.ScaleFontSize(double.PositiveInfinity));
    }

    [Fact]
    public void FromView_UsesASystemTextScalerThatAsksThePlatform()
    {
        _dispatcher.UpdateTextScaleFactor(2.0, NonlinearCurve);
        using var tester = new FrameworkDartTester();

        TextScaler scaler = MediaQueryData.FromView(tester.View).TextScaler;

        SystemTextScaler system = Assert.IsType<SystemTextScaler>(scaler);
        Assert.Equal(2.0, system.TextScaleFactor);
        Assert.Equal(46.0, system.Scale(32.0));
        Assert.Equal(system, MediaQueryData.FromView(tester.View).TextScaler);
        Assert.Equal(system.GetHashCode(), MediaQueryData.FromView(tester.View).TextScaler.GetHashCode());
        Assert.Equal("SystemTextScaler (2x)", system.ToString());

        _dispatcher.UpdateTextScaleFactor(1.0);
        Assert.NotEqual(system, MediaQueryData.FromView(tester.View).TextScaler);
        Assert.Equal("SystemTextScaler (no scaling)", MediaQueryData.FromView(tester.View).TextScaler.ToString());
    }
}
