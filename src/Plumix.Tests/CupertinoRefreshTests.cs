using Avalonia;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/refresh_test.dart

public sealed class CupertinoRefreshTests
{
    private static readonly Size Viewport = new(320.0, 480.0);

    [Fact]
    public void CupertinoSliverRefreshControl_ExposesSourceDefaultsNullBuilderAndGuards()
    {
        var refresh = new CupertinoSliverRefreshControl();

        Assert.Equal(100.0, refresh.RefreshTriggerPullDistance);
        Assert.Equal(60.0, refresh.RefreshIndicatorExtent);
        Assert.NotNull(refresh.Builder);
        Assert.Null(refresh.OnRefresh);
        Assert.Null(new CupertinoSliverRefreshControl(builder: null).Builder);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CupertinoSliverRefreshControl(refreshTriggerPullDistance: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CupertinoSliverRefreshControl(refreshIndicatorExtent: -1.0));
        Assert.Throws<ArgumentException>(() =>
            new CupertinoSliverRefreshControl(
                refreshTriggerPullDistance: 50.0,
                refreshIndicatorExtent: 60.0));
    }

    [Fact]
    public void BuildRefreshIndicator_MatchesDragArmedRefreshDoneAndInactiveComposition()
    {
        var drag = Assert.IsType<Center>(CupertinoSliverRefreshControl.BuildRefreshIndicator(
            default,
            RefreshIndicatorMode.Drag,
            pulledExtent: 50.0,
            refreshTriggerPullDistance: 100.0,
            refreshIndicatorExtent: 60.0));
        var dragStack = Assert.IsType<Stack>(drag.Child);
        Assert.Equal(Clip.None, dragStack.ClipBehavior);
        var dragPosition = Assert.IsType<Positioned>(Assert.Single(dragStack.Children));
        Assert.Equal(16.0, dragPosition.Top);
        Assert.Equal(0.0, dragPosition.Left);
        Assert.Equal(0.0, dragPosition.Right);
        var dragOpacity = Assert.IsType<Opacity>(dragPosition.Child);
        Assert.Equal(Curves.Interval(0.0, 0.35, Curves.EaseInOut)(0.5), dragOpacity.Value, 8);
        var partial = Assert.IsType<CupertinoActivityIndicator>(dragOpacity.Child);
        Assert.False(partial.Animating);
        Assert.Equal(14.0, partial.Radius);
        Assert.Equal(0.5, partial.Progress);

        var armed = IndicatorFor(RefreshIndicatorMode.Armed, pulledExtent: 100.0);
        var refreshing = IndicatorFor(RefreshIndicatorMode.Refresh, pulledExtent: 100.0);
        Assert.True(Assert.IsType<CupertinoActivityIndicator>(armed).Animating);
        Assert.True(Assert.IsType<CupertinoActivityIndicator>(refreshing).Animating);
        Assert.Equal(7.0, Assert.IsType<CupertinoActivityIndicator>(
            IndicatorFor(RefreshIndicatorMode.Done, pulledExtent: 50.0)).Radius);
        Assert.Equal(1.0, Assert.IsType<CupertinoActivityIndicator>(Assert.IsType<Opacity>(
            IndicatorFor(RefreshIndicatorMode.Drag, pulledExtent: 200.0)).Child).Progress);
        Assert.IsType<SizedBox>(IndicatorFor(RefreshIndicatorMode.Inactive, pulledExtent: 0.0));
    }

    [Fact]
    public void RenderCupertinoSliverRefresh_UsesOverscrollAndCompensatedLayoutExtentGeometry()
    {
        using var renderErrors = RenderErrorRethrowScope.Enter();
        var child = new ExpandingRenderBox();
        var sliver = new RenderCupertinoSliverRefresh(
            refreshIndicatorExtent: 60.0,
            hasLayoutExtent: false,
            child: child);

        sliver.LayoutWithSliverConstraints(Constraints(overlap: -40.0));
        Assert.Equal(new Size(320.0, 40.0), child.Size);
        Assert.Equal(0.0, sliver.Geometry.ScrollExtent);
        Assert.Equal(-40.0, sliver.Geometry.PaintOrigin);
        Assert.Equal(40.0, sliver.Geometry.PaintExtent);
        Assert.Equal(40.0, sliver.Geometry.MaxPaintExtent);
        Assert.Equal(0.0, sliver.Geometry.LayoutExtent);

        sliver.HasLayoutExtent = true;
        sliver.LayoutWithSliverConstraints(Constraints(remainingPaintExtent: 479.0));
        Assert.Equal(60.0, sliver.Geometry.ScrollOffsetCorrection);
        sliver.LayoutWithSliverConstraints(Constraints());
        Assert.Equal(new Size(320.0, 60.0), child.Size);
        Assert.Equal(60.0, sliver.Geometry.ScrollExtent);
        Assert.Equal(60.0, sliver.Geometry.LayoutExtent);

        sliver.HasLayoutExtent = false;
        sliver.LayoutWithSliverConstraints(Constraints());
        Assert.Equal(-60.0, sliver.Geometry.ScrollOffsetCorrection);

        var paintOverflow = new RenderCupertinoSliverRefresh(
            refreshIndicatorExtent: 60.0,
            hasLayoutExtent: false,
            child: new ExpandingRenderBox());
        paintOverflow.LayoutWithSliverConstraints(Constraints(
            overlap: -80.0,
            remainingPaintExtent: 20.0));
        Assert.Equal(80.0, paintOverflow.Geometry.PaintExtent);
        Assert.Throws<InvalidOperationException>(() =>
            sliver.LayoutWithSliverConstraints(Constraints(axisDirection: AxisDirection.Up)));
    }

    [Fact]
    public async Task StateMachine_ArmsRefreshesAndEntersDoneAsSoonAsTheTaskCompletes()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<(RefreshIndicatorMode Mode, double Extent)>();
        int refreshCalls = 0;
        var refresh = new CupertinoSliverRefreshControl(
            builder: (_, mode, extent, _, _) =>
            {
                calls.Add((mode, extent));
                return new SizedBox();
            },
            onRefresh: () =>
            {
                refreshCalls++;
                return gate.Task;
            });
        using var harness = new CupertinoThemeTestHarness(refresh);
        var sliver = Assert.IsType<RenderCupertinoSliverRefresh>(harness.RenderView.Child);
        var state = harness.FindState<CupertinoSliverRefreshControlState>();

        sliver.LayoutWithSliverConstraints(Constraints());
        Assert.Equal(RefreshIndicatorMode.Inactive, state.RefreshState);
        Assert.Empty(calls);

        sliver.LayoutWithSliverConstraints(Constraints(overlap: -40.0));
        Assert.Equal(RefreshIndicatorMode.Drag, state.RefreshState);
        Assert.Equal((RefreshIndicatorMode.Drag, 40.0), calls[^1]);

        sliver.LayoutWithSliverConstraints(Constraints(overlap: -100.0));
        Assert.Equal(RefreshIndicatorMode.Armed, state.RefreshState);
        MethodCall haptic = Assert.Single(platform.Log);
        Assert.Equal("HapticFeedback.vibrate", haptic.Method);
        Assert.Equal("HapticFeedbackType.mediumImpact", haptic.Arguments);
        Scheduler.ScheduleFrame();
        Scheduler.PumpFrameForTests();
        await WaitUntilAsync(() => refreshCalls == 1);

        harness.Layout(Viewport);
        sliver.LayoutWithSliverConstraints(Constraints());
        Assert.Equal(RefreshIndicatorMode.Refresh, state.RefreshState);
        Assert.True(sliver.HasLayoutExtent);

        gate.SetResult();
        await WaitUntilAsync(() => state.RefreshState == RefreshIndicatorMode.Done);
        sliver.LayoutWithSliverConstraints(Constraints(overlap: -101.0));
        Assert.Equal(1, refreshCalls);
        harness.Layout(Viewport);
        Assert.Equal(RefreshIndicatorMode.Done, state.RefreshState);
        Assert.False(sliver.HasLayoutExtent);
    }

    [Fact]
    public void StateMachine_WithoutRefreshCallbackShowsArmedForOneLayoutThenRetracts()
    {
        var calls = new List<RefreshIndicatorMode>();
        using var harness = new CupertinoThemeTestHarness(new CupertinoSliverRefreshControl(
            builder: (_, mode, _, _, _) =>
            {
                calls.Add(mode);
                return new SizedBox();
            }));
        var sliver = Assert.IsType<RenderCupertinoSliverRefresh>(harness.RenderView.Child);
        var state = harness.FindState<CupertinoSliverRefreshControlState>();

        sliver.LayoutWithSliverConstraints(Constraints(overlap: -100.0));
        Assert.Equal(RefreshIndicatorMode.Armed, state.RefreshState);
        sliver.LayoutWithSliverConstraints(Constraints(overlap: -99.0));
        Assert.Equal(RefreshIndicatorMode.Done, state.RefreshState);
        sliver.LayoutWithSliverConstraints(Constraints(overlap: -11.0));
        Assert.Equal(RefreshIndicatorMode.Done, state.RefreshState);
        sliver.LayoutWithSliverConstraints(Constraints(overlap: -9.0));
        Assert.Equal(RefreshIndicatorMode.Inactive, state.RefreshState);
        Assert.Equal(
            [
                RefreshIndicatorMode.Armed,
                RefreshIndicatorMode.Done,
                RefreshIndicatorMode.Done,
                RefreshIndicatorMode.Inactive,
            ],
            calls);
    }

    private static Widget IndicatorFor(RefreshIndicatorMode mode, double pulledExtent)
    {
        var center = Assert.IsType<Center>(CupertinoSliverRefreshControl.BuildRefreshIndicator(
            default,
            mode,
            pulledExtent,
            refreshTriggerPullDistance: 100.0,
            refreshIndicatorExtent: 60.0));
        var stack = Assert.IsType<Stack>(center.Child);
        return Assert.IsType<Positioned>(Assert.Single(stack.Children)).Child;
    }

    private static SliverConstraints Constraints(
        double overlap = 0.0,
        AxisDirection axisDirection = AxisDirection.Down,
        double remainingPaintExtent = 480.0)
    {
        return new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0.0,
            RemainingPaintExtent: remainingPaintExtent,
            CrossAxisExtent: 320.0,
            ViewportMainAxisExtent: 480.0,
            RemainingCacheExtent: 480.0,
            AxisDirection: axisDirection,
            GrowthDirection: GrowthDirection.Forward,
            Overlap: overlap);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        for (int attempt = 0; attempt < 500 && !predicate(); attempt++)
        {
            await Task.Delay(2);
        }

        Assert.True(predicate());
    }

    private sealed class ExpandingRenderBox : RenderBox
    {
        protected override void PerformLayout()
        {
            double height = double.IsFinite(Constraints.MaxHeight) ? Constraints.MaxHeight : 0.0;
            Size = Constraints.Constrain(new Size(Constraints.MaxWidth, height));
        }

        public override void Paint(PaintingContext context, Point offset)
        {
        }
    }
}
