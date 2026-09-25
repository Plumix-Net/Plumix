using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart
// Mirrors flutter/packages/flutter/test/rendering/multi_view_binding_test.dart and
// binding_pipeline_manifold_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class RendererBindingTests
{
    [Fact]
    public void AddRenderView_RegistersAndConfiguresTheView()
    {
        var flutterView = new FlutterView(new Size(400, 600), devicePixelRatio: 2.5, viewId: 1001);
        var renderView = new RenderView(flutterView);
        RendererBinding binding = RendererBinding.Instance;

        Assert.DoesNotContain(renderView, binding.RenderViews);
        binding.AddRenderView(renderView);
        try
        {
            Assert.Contains(renderView, binding.RenderViews);
            Assert.Equal(2.5, renderView.Configuration.DevicePixelRatio);
            Assert.Equal(BoxConstraints.Tight(new Size(160, 240)), renderView.Configuration.LogicalConstraints);
            Assert.Equal(BoxConstraints.Tight(new Size(400, 600)), renderView.Configuration.PhysicalConstraints);
        }
        finally
        {
            binding.RemoveRenderView(renderView);
        }

        Assert.DoesNotContain(renderView, binding.RenderViews);
    }

    [Fact]
    public void AddAndRemoveRenderView_RejectIllegalRegistrations()
    {
        var renderView = new RenderView(new FlutterView(new Size(1, 1), viewId: 1002));
        var sameId = new RenderView(new FlutterView(new Size(1, 1), viewId: 1002));
        var other = new RenderView(new FlutterView(new Size(1, 1), viewId: 1003));
        RendererBinding binding = RendererBinding.Instance;

        Assert.Throws<AssertionError>(() => binding.RemoveRenderView(renderView));
        binding.AddRenderView(renderView);
        try
        {
            Assert.Throws<AssertionError>(() => binding.AddRenderView(renderView));
            Assert.Throws<AssertionError>(() => binding.AddRenderView(sameId));
            Assert.Throws<AssertionError>(() => binding.RemoveRenderView(sameId));
            Assert.Throws<AssertionError>(() => binding.RemoveRenderView(other));
        }
        finally
        {
            binding.RemoveRenderView(renderView);
        }
    }

    [Fact]
    public void HandleMetricsChanged_ReconfiguresTheRegisteredViews()
    {
        var flutterView = new FlutterView(new Size(400, 600), devicePixelRatio: 2.5, viewId: 1004);
        var renderView = new RenderView(flutterView);
        RendererBinding binding = RendererBinding.Instance;
        binding.AddRenderView(renderView);
        try
        {
            Assert.Equal(BoxConstraints.Tight(new Size(160, 240)), renderView.Configuration.LogicalConstraints);

            flutterView.UpdateMetrics(physicalSize: new Size(300, 300), devicePixelRatio: 3.0);
            binding.HandleMetricsChanged();

            Assert.Equal(3.0, renderView.Configuration.DevicePixelRatio);
            Assert.Equal(BoxConstraints.Tight(new Size(100, 100)), renderView.Configuration.LogicalConstraints);
        }
        finally
        {
            binding.RemoveRenderView(renderView);
        }
    }

    [Fact]
    public void HitTestInView_ReachesOnlyTheRegisteredViewWithTheMatchingId()
    {
        var firstTarget = new RenderPointerListener(behavior: HitTestBehavior.Opaque);
        var secondTarget = new RenderPointerListener(behavior: HitTestBehavior.Opaque);
        var firstView = new RenderView(
            new FlutterView(new Size(40, 40), viewId: 1011),
            child: firstTarget);
        var secondView = new RenderView(
            new FlutterView(new Size(40, 40), viewId: 1012),
            child: secondTarget);
        var firstOwner = new PipelineOwner(firstView);
        var secondOwner = new PipelineOwner(secondView);
        RendererBinding binding = RendererBinding.Instance;
        firstOwner.Attach(firstView);
        secondOwner.Attach(secondView);
        binding.AddRenderView(firstView);
        binding.AddRenderView(secondView);
        try
        {
            firstOwner.FlushLayout(new Size(40, 40));
            secondOwner.FlushLayout(new Size(40, 40));

            var firstResult = new HitTestResult();
            var secondResult = new HitTestResult();
            var unknownResult = new HitTestResult();
            binding.HitTestInView(firstResult, new Point(10, 10), 1011);
            binding.HitTestInView(secondResult, new Point(10, 10), 1012);
            binding.HitTestInView(unknownResult, new Point(10, 10), 1013);

            Assert.Contains(firstResult.Path, entry => ReferenceEquals(entry.Target, firstTarget));
            Assert.DoesNotContain(firstResult.Path, entry => ReferenceEquals(entry.Target, secondTarget));
            Assert.Contains(secondResult.Path, entry => ReferenceEquals(entry.Target, secondTarget));
            Assert.DoesNotContain(secondResult.Path, entry => ReferenceEquals(entry.Target, firstTarget));
            Assert.Single(unknownResult.Path);
            Assert.Same(GestureBinding.Instance, unknownResult.Path[0].Target);
        }
        finally
        {
            binding.RemoveRenderView(firstView);
            binding.RemoveRenderView(secondView);
            firstOwner.RootNode = null;
            secondOwner.RootNode = null;
        }
    }

    [DebugOnlyFact]
    public void ScheduleMouseTrackerUpdate_RejectsASecondUpdateInTheSameFrame()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        try
        {
            GestureBinding.Instance.ScheduleMouseTrackerUpdate();

            Assert.Throws<AssertionError>(() => GestureBinding.Instance.ScheduleMouseTrackerUpdate());
        }
        finally
        {
            Scheduler.ResetForTests();
            GestureBinding.Instance.ResetForTests();
        }
    }

    [Fact]
    public void RootPipelineOwner_RejectsARootNode()
    {
        var node = new RenderView(new FlutterView(new Size(1, 1), viewId: 1005));

        FlutterError error = Assert.Throws<FlutterError>(
            () => RendererBinding.Instance.RootPipelineOwner.RootNode = node);

        Assert.StartsWith("Cannot set a rootNode on the default root pipeline owner.", error.Message);
        Assert.Null(RendererBinding.Instance.RootPipelineOwner.RootNode);
    }

    [Fact]
    public void EnsureSemantics_CreatesAndDisposesSemanticsOwnersAcrossTheTree()
    {
        // binding_pipeline_manifold_test.dart: "Turning global semantics on/off creates semantics
        // owners in PipelineOwner tree".
        var child = new PipelineOwner(onSemanticsUpdate: static _ => { });
        PipelineOwner root = RendererBinding.Instance.RootPipelineOwner;
        root.AdoptChild(child);
        SemanticsHandle? handle = null;
        try
        {
            Assert.Null(child.SemanticsOwner);
            Assert.Null(root.SemanticsOwner);

            handle = SemanticsBinding.Instance.EnsureSemantics();
            Assert.True(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.NotNull(child.SemanticsOwner);
            Assert.NotNull(root.SemanticsOwner);

            handle.Dispose();
            handle = null;
            Assert.False(SemanticsBinding.Instance.SemanticsEnabled);
            Assert.Null(child.SemanticsOwner);
            Assert.Null(root.SemanticsOwner);
        }
        finally
        {
            handle?.Dispose();
            root.DropChild(child);
        }
    }

    [Fact]
    public void RootPipelineOwner_FlushesAdoptedChildren()
    {
        var flutterView = new FlutterView(new Size(50, 20), viewId: 1006);
        var renderView = new RenderView(flutterView);
        var child = new PipelineOwner(onSemanticsUpdate: static _ => { });
        PipelineOwner root = RendererBinding.Instance.RootPipelineOwner;
        root.AdoptChild(child);
        try
        {
            child.RootNode = renderView;
            RendererBinding.Instance.AddRenderView(renderView);
            renderView.PrepareInitialFrame();

            root.FlushLayout();

            Assert.Equal(new Size(50, 20), renderView.Size);
        }
        finally
        {
            RendererBinding.Instance.RemoveRenderView(renderView);
            child.RootNode = null;
            root.DropChild(child);
        }
    }
}
