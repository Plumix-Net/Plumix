using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/performance_overlay.dart

namespace Plumix.Rendering;

/// <summary>The options that control whether the performance overlay displays certain aspects of performance.</summary>
/// <remarks>
/// Flutter's <c>PerformanceOverlayOption</c>. These must be in the order needed for their index values
/// to match the constants in the engine's <c>performance_overlay_layer.h</c>.
/// </remarks>
public enum PerformanceOverlayOption
{
    /// <summary>Display the frame time and FPS of the last frame rendered by the rasterizer.</summary>
    DisplayRasterizerStatistics,

    /// <summary>Show the frame time and FPS of the last frames rendered by the rasterizer as a graph.</summary>
    VisualizeRasterizerStatistics,

    /// <summary>Display the frame time and FPS of the last frame built by the UI.</summary>
    DisplayEngineStatistics,

    /// <summary>Show the frame time and FPS of the last frames built by the UI as a graph.</summary>
    VisualizeEngineStatistics,
}

/// <summary>Displays performance statistics.</summary>
/// <remarks>
/// Flutter's <c>RenderPerformanceOverlay</c>. The overlay shows two time series: the time spent in the
/// rasterizer for each frame, and the time spent building each frame. Each series is shown when its
/// display or visualize option is set in <see cref="OptionsMask"/>; each takes 80 logical pixels.
/// </remarks>
public class RenderPerformanceOverlay : RenderBox
{
    private const double KDefaultGraphHeight = 80.0;

    private static readonly int RasterizerMask =
        (1 << (int)PerformanceOverlayOption.DisplayRasterizerStatistics)
        | (1 << (int)PerformanceOverlayOption.VisualizeRasterizerStatistics);

    private static readonly int EngineMask =
        (1 << (int)PerformanceOverlayOption.DisplayEngineStatistics)
        | (1 << (int)PerformanceOverlayOption.VisualizeEngineStatistics);

    private int _optionsMask;

    /// <summary>Creates a performance overlay render object.</summary>
    public RenderPerformanceOverlay(int optionsMask = 0)
    {
        _optionsMask = optionsMask;
    }

    /// <summary>
    /// The mask is created by shifting 1 by the index of the specific <see cref="PerformanceOverlayOption"/>
    /// to enable.
    /// </summary>
    public int OptionsMask
    {
        get => _optionsMask;
        set
        {
            if (value == _optionsMask)
            {
                return;
            }

            _optionsMask = value;
            MarkNeedsPaint();
        }
    }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return 0.0;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return 0.0;
    }

    private double IntrinsicHeight
    {
        get
        {
            double result = 0.0;
            if ((OptionsMask & RasterizerMask) != 0)
            {
                result += KDefaultGraphHeight;
            }

            if ((OptionsMask & EngineMask) != 0)
            {
                result += KDefaultGraphHeight;
            }

            return result;
        }
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return IntrinsicHeight;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return IntrinsicHeight;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Constrain(new Size(double.PositiveInfinity, IntrinsicHeight));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        System.Diagnostics.Debug.Assert(NeedsCompositing);
        context.AddLayer(new PerformanceOverlayLayer(
            overlayRect: new Rect(offset.X, offset.Y, Size.Width, Size.Height),
            optionsMask: OptionsMask));
    }
}
