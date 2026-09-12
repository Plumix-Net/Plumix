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

            HitTestResult firstResult = binding.HitTestInView(new Point(10, 10), 1011);
            HitTestResult secondResult = binding.HitTestInView(new Point(10, 10), 1012);
            HitTestResult unknownResult = binding.HitTestInView(new Point(10, 10), 1013);

            Assert.Contains(firstResult.Path, entry => ReferenceEquals(entry.Target, firstTarget));
            Assert.DoesNotContain(firstResult.Path, entry => ReferenceEquals(entry.Target, secondTarget));
            Assert.Contains(secondResult.Path, entry => ReferenceEquals(entry.Target, secondTarget));
            Assert.DoesNotContain(secondResult.Path, entry => ReferenceEquals(entry.Target, firstTarget));
            Assert.Empty(unknownResult.Path);
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
    public void SetSemanticsEnabled_CreatesAndDisposesSemanticsOwnersAcrossTheTree()
    {
        var child = new PipelineOwner(onSemanticsUpdate: static _ => { });
        PipelineOwner root = RendererBinding.Instance.RootPipelineOwner;
        root.AdoptChild(child);
        try
        {
            Assert.Null(child.SemanticsOwner);

            RendererBinding.Instance.SetSemanticsEnabled(true);
            Assert.True(RendererBinding.Instance.SemanticsEnabled);
            Assert.NotNull(child.SemanticsOwner);

            RendererBinding.Instance.SetSemanticsEnabled(false);
            Assert.Null(child.SemanticsOwner);
        }
        finally
        {
            RendererBinding.Instance.SetSemanticsEnabled(false);
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
