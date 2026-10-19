using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>Creates on-screen test owners that share the binding's process-global focus manager.</summary>
internal static class TestBuildOwner
{
    // Reading an already-pumped result must never block the framework thread.
    internal static T Complete<T>(Task<T> future)
    {
        EventLoop.FlushMicrotasks();
        Xunit.Assert.True(future.IsCompleted, "The future requires more than the current microtask turn.");
        return future.GetAwaiter().GetResult();
    }

    internal static BuildOwner Create() => new(focusManager: FocusManager.Instance);

    /// <summary>Runs an isolated owner's build and optional render pipeline in a real test frame.</summary>
    internal static void PumpFrame(this BuildOwner owner) => FrameworkDartTester.PumpFrame(owner);

    internal static void PumpFrame(this BuildOwner owner, Action render)
        => FrameworkDartTester.PumpFrame(owner, render);

    internal static void PumpFrame(this BuildOwner owner, PipelineOwner pipeline, Size size, bool semantics = false)
        => owner.PumpFrame(() =>
        {
            pipeline.RequestLayout();
            pipeline.FlushLayout(size);
            pipeline.FlushCompositingBits();
            pipeline.FlushPaint();
            pipeline.CompositeFrame();
            if (semantics)
            {
                pipeline.RequestSemanticsUpdate();
                pipeline.FlushSemantics();
            }
        });
}
