using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/performance_overlay.dart

namespace Plumix.Widgets;

/// <summary>Displays performance statistics.</summary>
/// <remarks>
/// Flutter's <c>PerformanceOverlay</c>. The overlay shows two time series: the rasterizer time of each
/// frame (the time the host spent drawing the scene) and the UI time of each frame (the time spent
/// building it). The green bar marks the current frame and turns red when the frame took longer than one
/// 60 Hz frame budget.
/// </remarks>
public class PerformanceOverlay : LeafRenderObjectWidget
{
    /// <summary>Create a performance overlay that only displays specific statistics.</summary>
    /// <remarks>
    /// The mask is created by shifting 1 by the index of the specific <see cref="PerformanceOverlayOption"/>
    /// to enable.
    /// </remarks>
    public PerformanceOverlay(int optionsMask = 0, Plumix.Foundation.Key? key = null) : base(key)
    {
        OptionsMask = optionsMask;
    }

    /// <summary>Create a performance overlay that displays all available statistics.</summary>
    /// <remarks>Flutter's <c>PerformanceOverlay.allEnabled</c>.</remarks>
    public static PerformanceOverlay AllEnabled(Plumix.Foundation.Key? key = null) => new(
        optionsMask: (1 << (int)PerformanceOverlayOption.DisplayRasterizerStatistics)
                     | (1 << (int)PerformanceOverlayOption.VisualizeRasterizerStatistics)
                     | (1 << (int)PerformanceOverlayOption.DisplayEngineStatistics)
                     | (1 << (int)PerformanceOverlayOption.VisualizeEngineStatistics),
        key: key);

    /// <summary>
    /// The mask is created by shifting 1 by the index of the specific <see cref="PerformanceOverlayOption"/>
    /// to enable.
    /// </summary>
    public int OptionsMask { get; }

    public override RenderObject CreateRenderObject(BuildContext context) =>
        new RenderPerformanceOverlay(optionsMask: OptionsMask);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderPerformanceOverlay)renderObject).OptionsMask = OptionsMask;
    }
}
