using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>Creates a separate composited display list for its child.</summary>
public sealed class RenderRepaintBoundary : RenderProxyBox
{
    private int _debugSymmetricPaintCount;
    private int _debugAsymmetricPaintCount;

    public RenderRepaintBoundary(RenderBox? child = null)
    {
        Child = child;
    }

    public override bool IsRepaintBoundary => true;

    /// <summary>
    /// The number of times that this render object repainted at the same time as its parent.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderRepaintBoundary.debugSymmetricPaintCount</c>. Repaint boundaries are only
    /// useful when the parent and child paint at different times; when both paint together the
    /// boundary is redundant and may be making performance worse. Only valid in debug builds; in
    /// release builds this always returns zero. Reset with <see cref="DebugResetMetrics"/>.
    /// </remarks>
    public int DebugSymmetricPaintCount => _debugSymmetricPaintCount;

    /// <summary>
    /// The number of times that either this render object repainted without the parent being
    /// painted, or the parent repainted without this object being painted.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderRepaintBoundary.debugAsymmetricPaintCount</c>. Only valid in debug builds;
    /// in release builds this always returns zero. Reset with <see cref="DebugResetMetrics"/>.
    /// </remarks>
    public int DebugAsymmetricPaintCount => _debugAsymmetricPaintCount;

    /// <summary>Resets both repaint-boundary paint counts to zero.</summary>
    /// <remarks>
    /// Flutter's <c>RenderRepaintBoundary.debugResetMetrics</c>; does nothing in release builds.
    /// </remarks>
    public void DebugResetMetrics()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        _debugSymmetricPaintCount = 0;
        _debugAsymmetricPaintCount = 0;
    }

    /// <inheritdoc />
    public override void DebugRegisterRepaintBoundaryPaint(bool includedParent = true, bool includedChild = false)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (includedParent && includedChild)
        {
            _debugSymmetricPaintCount += 1;
        }
        else
        {
            _debugAsymmetricPaintCount += 1;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        if (!Constants.KDebugMode)
        {
            properties.Add(DiagnosticsNode.Message("(run in debug mode to collect repaint boundary statistics)"));
            return;
        }

        int totalPaints = DebugSymmetricPaintCount + DebugAsymmetricPaintCount;
        if (totalPaints == 0)
        {
            properties.Add(new MessageProperty("usefulness ratio", "no metrics collected yet (never painted)"));
            return;
        }

        double fraction = DebugAsymmetricPaintCount / (double)totalPaints;
        string diagnosis = fraction switch
        {
            _ when totalPaints < 5 => "insufficient data to draw conclusion (less than five repaints)",
            > 0.9 => "this is an outstandingly useful repaint boundary and should definitely be kept",
            > 0.5 => "this is a useful repaint boundary and should be kept",
            > 0.3 => "this repaint boundary is probably useful, but maybe it would be more useful in "
                     + "tandem with adding more repaint boundaries elsewhere",
            > 0.1 => "this repaint boundary does sometimes show value, though currently not that often",
            _ when DebugAsymmetricPaintCount > 0 =>
                "this repaint boundary is not very effective and should probably be removed",
            _ => "this repaint boundary is astoundingly ineffectual and should be removed",
        };

        properties.Add(new PercentProperty(
            "metrics",
            fraction,
            unit: "useful",
            tooltip: $"{DebugSymmetricPaintCount} bad vs {DebugAsymmetricPaintCount} good"));
        properties.Add(new MessageProperty("diagnosis", diagnosis));
    }
}
