using Plumix.Widgets;

// C#-only test infrastructure: shared frame driver for isolated widget and render test trees.

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    /// <summary>
    /// Pumps one explicit frame for an isolated owner. Rendering and finalization run in the same
    /// persistent callback; animation time advances only when the caller advances its test clock.
    /// </summary>
    internal static void PumpFrame(BuildOwner owner, Action? render = null)
    {
        RendererBinding binding = RendererBinding.Instance;
        binding.EnsurePersistentFrameCallback();
        Action? previous = binding.DrawFrameOverrideForTests;
        binding.DrawFrameOverrideForTests = () =>
        {
            if (owner.RootElement is { } root)
            {
                owner.BuildScope(root);
            }

            render?.Invoke();
            owner.FinalizeTree();
        };
        try
        {
            EventLoop.FlushMicrotasks();
            TimeSpan timestamp = SchedulerBinding.Instance.CurrentSystemFrameTimeStamp;
            SchedulerBinding.Instance.HandleFrame(timestamp);
        }
        finally
        {
            binding.DrawFrameOverrideForTests = previous;
        }
    }
}
