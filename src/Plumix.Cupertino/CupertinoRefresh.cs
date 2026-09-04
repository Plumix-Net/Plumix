using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/refresh.dart

public enum RefreshIndicatorMode
{
    Inactive,
    Drag,
    Armed,
    Refresh,
    Done,
}

public delegate Widget RefreshControlIndicatorBuilder(
    BuildContext context,
    RefreshIndicatorMode refreshState,
    double pulledExtent,
    double refreshTriggerPullDistance,
    double refreshIndicatorExtent);

public delegate Task RefreshCallback();

internal sealed class CupertinoSliverRefresh : SingleChildRenderObjectWidget
{
    public CupertinoSliverRefresh(
        double refreshIndicatorLayoutExtent = 0.0,
        bool hasLayoutExtent = false,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        if (!double.IsFinite(refreshIndicatorLayoutExtent) || refreshIndicatorLayoutExtent < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshIndicatorLayoutExtent));
        }

        RefreshIndicatorLayoutExtent = refreshIndicatorLayoutExtent;
        HasLayoutExtent = hasLayoutExtent;
    }

    public double RefreshIndicatorLayoutExtent { get; }

    public bool HasLayoutExtent { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoSliverRefresh(
            refreshIndicatorExtent: RefreshIndicatorLayoutExtent,
            hasLayoutExtent: HasLayoutExtent);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var refresh = (RenderCupertinoSliverRefresh)renderObject;
        refresh.RefreshIndicatorLayoutExtent = RefreshIndicatorLayoutExtent;
        refresh.HasLayoutExtent = HasLayoutExtent;
    }
}

internal sealed class RenderCupertinoSliverRefresh : RenderSliverSingleBoxAdapter
{
    private double _refreshIndicatorExtent;
    private bool _hasLayoutExtent;

    public RenderCupertinoSliverRefresh(
        double refreshIndicatorExtent,
        bool hasLayoutExtent,
        RenderBox? child = null)
    {
        if (!double.IsFinite(refreshIndicatorExtent) || refreshIndicatorExtent < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshIndicatorExtent));
        }

        _refreshIndicatorExtent = refreshIndicatorExtent;
        _hasLayoutExtent = hasLayoutExtent;
        Child = child;
    }

    public double RefreshIndicatorLayoutExtent
    {
        get => _refreshIndicatorExtent;
        set
        {
            if (!double.IsFinite(value) || value < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_refreshIndicatorExtent == value)
            {
                return;
            }

            _refreshIndicatorExtent = value;
            MarkNeedsLayout();
        }
    }

    public bool HasLayoutExtent
    {
        get => _hasLayoutExtent;
        set
        {
            if (_hasLayoutExtent == value)
            {
                return;
            }

            _hasLayoutExtent = value;
            MarkNeedsLayout();
        }
    }

    public double LayoutExtentOffsetCompensation { get; private set; }

    protected override void PerformSliverLayout(SliverConstraints constraints)
    {
        if (constraints.AxisDirection != AxisDirection.Down
            || constraints.GrowthDirection != GrowthDirection.Forward)
        {
            throw new InvalidOperationException(
                "CupertinoSliverRefreshControl only supports downward, forward-growing slivers.");
        }

        double layoutExtent = (_hasLayoutExtent ? 1.0 : 0.0) * _refreshIndicatorExtent;
        if (layoutExtent != LayoutExtentOffsetCompensation)
        {
            Geometry = new SliverGeometry(
                ScrollOffsetCorrection: layoutExtent - LayoutExtentOffsetCompensation);
            LayoutExtentOffsetCompensation = layoutExtent;
            return;
        }

        bool active = constraints.Overlap < 0.0 || layoutExtent > 0.0;
        double overscrolledExtent = constraints.Overlap < 0.0 ? Math.Abs(constraints.Overlap) : 0.0;
        if (Child == null)
        {
            Geometry = default;
            return;
        }

        Child.Layout(
            constraints.AsBoxConstraints(maxExtent: layoutExtent + overscrolledExtent),
            parentUsesSize: true);
        if (!active)
        {
            Geometry = default;
            return;
        }

        double childExtent = Math.Max(Child.Size.Height, layoutExtent) - constraints.ScrollOffset;
        double paintExtent = Math.Max(childExtent, 0.0);
        Geometry = new SliverGeometry(
            ScrollExtent: layoutExtent,
            PaintOrigin: -overscrolledExtent - constraints.ScrollOffset,
            PaintExtent: paintExtent,
            MaxPaintExtent: paintExtent,
            LayoutExtent: Math.Max(layoutExtent - constraints.ScrollOffset, 0.0));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child == null)
        {
            return;
        }

        SliverConstraints constraints = ConstraintsForSliver;
        if (constraints.Overlap < 0.0 || constraints.ScrollOffset + Child.Size.Height > 0.0)
        {
            context.PaintChild(Child, offset);
        }
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
    }
}

public sealed class CupertinoSliverRefreshControl : StatefulWidget
{
    private const double DefaultRefreshTriggerPullDistance = 100.0;
    private const double DefaultRefreshIndicatorExtent = 60.0;
    private const double ActivityIndicatorRadius = 14.0;
    private const double ActivityIndicatorMargin = 16.0;

    public CupertinoSliverRefreshControl(
        double refreshTriggerPullDistance = DefaultRefreshTriggerPullDistance,
        double refreshIndicatorExtent = DefaultRefreshIndicatorExtent,
        RefreshCallback? onRefresh = null,
        Key? key = null) : this(
        refreshTriggerPullDistance,
        refreshIndicatorExtent,
        BuildRefreshIndicator,
        onRefresh,
        key)
    {
    }

    public CupertinoSliverRefreshControl(
        RefreshControlIndicatorBuilder? builder,
        double refreshTriggerPullDistance = DefaultRefreshTriggerPullDistance,
        double refreshIndicatorExtent = DefaultRefreshIndicatorExtent,
        RefreshCallback? onRefresh = null,
        Key? key = null) : this(
        refreshTriggerPullDistance,
        refreshIndicatorExtent,
        builder,
        onRefresh,
        key)
    {
    }

    private CupertinoSliverRefreshControl(
        double refreshTriggerPullDistance,
        double refreshIndicatorExtent,
        RefreshControlIndicatorBuilder? builder,
        RefreshCallback? onRefresh,
        Key? key) : base(key)
    {
        if (!double.IsFinite(refreshTriggerPullDistance) || refreshTriggerPullDistance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshTriggerPullDistance));
        }

        if (!double.IsFinite(refreshIndicatorExtent) || refreshIndicatorExtent < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshIndicatorExtent));
        }

        if (refreshTriggerPullDistance < refreshIndicatorExtent)
        {
            throw new ArgumentException(
                "The refresh indicator cannot take more space in its final state than the amount "
                + "initially created by overscrolling.",
                nameof(refreshIndicatorExtent));
        }

        RefreshTriggerPullDistance = refreshTriggerPullDistance;
        RefreshIndicatorExtent = refreshIndicatorExtent;
        Builder = builder;
        OnRefresh = onRefresh;
    }

    public double RefreshTriggerPullDistance { get; }

    public double RefreshIndicatorExtent { get; }

    public RefreshControlIndicatorBuilder? Builder { get; }

    public RefreshCallback? OnRefresh { get; }

    public static RefreshIndicatorMode State(BuildContext context)
    {
        return context.FindAncestorStateOfType<CupertinoSliverRefreshControlState>()?.RefreshState
               ?? throw new InvalidOperationException("No CupertinoSliverRefreshControl ancestor was found.");
    }

    public static Widget BuildRefreshIndicator(
        BuildContext context,
        RefreshIndicatorMode refreshState,
        double pulledExtent,
        double refreshTriggerPullDistance,
        double refreshIndicatorExtent)
    {
        double percentageComplete = Math.Clamp(pulledExtent / refreshTriggerPullDistance, 0.0, 1.0);
        return new Center(
            child: new Stack(
                clipBehavior: Clip.None,
                children:
                [
                    new Positioned(
                        top: ActivityIndicatorMargin,
                        left: 0.0,
                        right: 0.0,
                        child: BuildIndicatorForRefreshState(
                            refreshState,
                            ActivityIndicatorRadius,
                            percentageComplete)),
                ]));
    }

    public override State CreateState()
    {
        return new CupertinoSliverRefreshControlState();
    }

    private static Widget BuildIndicatorForRefreshState(
        RefreshIndicatorMode refreshState,
        double radius,
        double percentageComplete)
    {
        switch (refreshState)
        {
            case RefreshIndicatorMode.Drag:
                Curve opacityCurve = Curves.Interval(0.0, 0.35, Curves.EaseInOut);
                return new Opacity(
                    opacity: opacityCurve(percentageComplete),
                    child: CupertinoActivityIndicator.PartiallyRevealed(
                        radius: radius,
                        progress: percentageComplete));
            case RefreshIndicatorMode.Armed:
            case RefreshIndicatorMode.Refresh:
                return new CupertinoActivityIndicator(radius: radius);
            case RefreshIndicatorMode.Done:
                return new CupertinoActivityIndicator(radius: radius * percentageComplete);
            case RefreshIndicatorMode.Inactive:
                return new SizedBox();
            default:
                throw new ArgumentOutOfRangeException(nameof(refreshState));
        }
    }
}

internal sealed class CupertinoSliverRefreshControlState : State
{
    private const double InactiveResetOverscrollFraction = 0.1;

    private Task? _refreshTask;
    private double _latestIndicatorBoxExtent;
    private bool _hasSliverLayoutExtent;

    private CupertinoSliverRefreshControl CurrentWidget =>
        (CupertinoSliverRefreshControl)StateWidget;

    internal RefreshIndicatorMode RefreshState { get; private set; }

    public override void InitState()
    {
        RefreshState = RefreshIndicatorMode.Inactive;
    }

    public override Widget Build(BuildContext context)
    {
        return new CupertinoSliverRefresh(
            refreshIndicatorLayoutExtent: CurrentWidget.RefreshIndicatorExtent,
            hasLayoutExtent: _hasSliverLayoutExtent,
            child: new LayoutBuilder((builderContext, constraints) =>
            {
                _latestIndicatorBoxExtent = constraints.MaxHeight;
                RefreshState = TransitionNextState();
                if (CurrentWidget.Builder != null && _latestIndicatorBoxExtent > 0.0)
                {
                    return CurrentWidget.Builder(
                        builderContext,
                        RefreshState,
                        _latestIndicatorBoxExtent,
                        CurrentWidget.RefreshTriggerPullDistance,
                        CurrentWidget.RefreshIndicatorExtent);
                }

                return new LimitedBox(
                    maxWidth: 0.0,
                    maxHeight: 0.0,
                    child: new ConstrainedBox(BoxConstraints.Expand()));
            }));
    }

    private RefreshIndicatorMode TransitionNextState()
    {
        RefreshIndicatorMode state = RefreshState;
        while (true)
        {
            switch (state)
            {
                case RefreshIndicatorMode.Inactive:
                    if (_latestIndicatorBoxExtent <= 0.0)
                    {
                        return RefreshIndicatorMode.Inactive;
                    }

                    state = RefreshIndicatorMode.Drag;
                    continue;
                case RefreshIndicatorMode.Drag:
                    if (_latestIndicatorBoxExtent == 0.0)
                    {
                        return RefreshIndicatorMode.Inactive;
                    }

                    if (_latestIndicatorBoxExtent < CurrentWidget.RefreshTriggerPullDistance)
                    {
                        return RefreshIndicatorMode.Drag;
                    }

                    if (CurrentWidget.OnRefresh != null)
                    {
                        _ = HapticFeedback.MediumImpact();
                        Scheduler.AddPostFrameCallback(
                            _ => StartRefreshTask(CurrentWidget.OnRefresh),
                            scheduleFrame: false);
                    }

                    return RefreshIndicatorMode.Armed;
                case RefreshIndicatorMode.Armed:
                    if (RefreshState == RefreshIndicatorMode.Armed && _refreshTask == null)
                    {
                        GoToDone();
                        state = RefreshIndicatorMode.Done;
                        continue;
                    }

                    if (_latestIndicatorBoxExtent > CurrentWidget.RefreshIndicatorExtent)
                    {
                        return RefreshIndicatorMode.Armed;
                    }

                    state = RefreshIndicatorMode.Refresh;
                    continue;
                case RefreshIndicatorMode.Refresh:
                    if (_refreshTask != null)
                    {
                        return RefreshIndicatorMode.Refresh;
                    }

                    GoToDone();
                    state = RefreshIndicatorMode.Done;
                    continue;
                case RefreshIndicatorMode.Done:
                    return _latestIndicatorBoxExtent
                           > CurrentWidget.RefreshTriggerPullDistance * InactiveResetOverscrollFraction
                        ? RefreshIndicatorMode.Done
                        : RefreshIndicatorMode.Inactive;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void GoToDone()
    {
        if (Scheduler.Phase == SchedulerPhase.Idle)
        {
            SetState(() => _hasSliverLayoutExtent = false);
            return;
        }

        Scheduler.AddPostFrameCallback(
            _ =>
            {
                if (Mounted)
                {
                    SetState(() => _hasSliverLayoutExtent = false);
                }
            },
            scheduleFrame: false);
    }

    private void StartRefreshTask(RefreshCallback callback)
    {
        if (!Mounted)
        {
            return;
        }

        _refreshTask = callback();
        _ = CompleteRefreshTaskAsync(_refreshTask);
        SetState(() => _hasSliverLayoutExtent = true);
    }

    private async Task CompleteRefreshTaskAsync(Task refreshTask)
    {
        await Task.Yield();
        try
        {
            await refreshTask;
        }
        catch
        {
            // Dart's whenComplete transition runs for both successful and failed refresh futures.
        }

        if (!Mounted || !ReferenceEquals(_refreshTask, refreshTask))
        {
            return;
        }

        SetState(() => _refreshTask = null);
        RefreshState = TransitionNextState();
    }
}
