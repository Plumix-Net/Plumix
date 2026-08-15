using Plumix;
using Plumix.Rendering;

namespace Plumix.Tests;

/// <summary>
/// A <see cref="ViewportOffset"/> that a test can drive directly, recording the dimensions the
/// viewport applied to it. Flutter's own render tests use <c>ViewportOffset.fixed</c> for this;
/// that offset ignores <c>jumpTo</c>, so the tests here need a scriptable variant.
/// </summary>
internal sealed class TestViewportOffset(double pixels = 0.0) : ViewportOffset
{
    private double _pixels = pixels;

    public override double Pixels => _pixels;

    public override bool HasPixels => true;

    /// <summary>The main-axis extent the viewport last reported.</summary>
    public double ViewportDimension { get; private set; }

    /// <summary>The minimum scroll extent the viewport last reported.</summary>
    public double MinScrollExtent { get; private set; }

    /// <summary>The maximum scroll extent the viewport last reported.</summary>
    public double MaxScrollExtent { get; private set; }

    /// <summary>How many corrections the viewport asked for since the last <see cref="JumpTo"/>.</summary>
    public int CorrectionCount { get; private set; }

    public override bool ApplyViewportDimension(double viewportDimension)
    {
        ViewportDimension = viewportDimension;
        return true;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        MinScrollExtent = minScrollExtent;
        MaxScrollExtent = maxScrollExtent;
        return true;
    }

    public override void CorrectBy(double correction)
    {
        _pixels += correction;
        CorrectionCount++;
    }

    public override void JumpTo(double pixels)
    {
        CorrectionCount = 0;
        if (_pixels == pixels)
        {
            return;
        }

        _pixels = pixels;
        NotifyListeners();
    }

    public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
    {
        JumpTo(to);
        return Task.CompletedTask;
    }

    public override ScrollDirection UserScrollDirection => ScrollDirection.Idle;

    public override bool AllowImplicitScrolling => true;
}
