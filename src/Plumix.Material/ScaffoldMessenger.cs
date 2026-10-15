using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scaffold.dart

public class ScaffoldFeatureController<TFeature, TClosedReason>
    where TFeature : Widget
{
    private readonly Action _close;
    private readonly TaskCompletionSource<TClosedReason> _closed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ScaffoldFeatureController(TFeature feature, Action close, StateSetter? setState = null)
    {
        Feature = feature;
        _close = close;
        SetState = setState;
    }

    public TFeature Feature { get; }

    /// <summary>Completes when the feature controlled by this object is no longer visible.</summary>
    public Task<TClosedReason> Closed => _closed.Task;

    /// <summary>Marks the feature (a bottom sheet or a snack bar) as needing to rebuild.</summary>
    public StateSetter? SetState { get; }

    /// <summary>Removes the feature from the scaffold.</summary>
    public void Close() => _close();

    internal void Complete(TClosedReason reason) => _closed.TrySetResult(reason);
}

public sealed class ScaffoldMessenger : StatefulWidget
{
    public ScaffoldMessenger(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override State CreateState() => new ScaffoldMessengerState();

    public static ScaffoldMessengerState Of(BuildContext context)
    {
        MaterialDebug.DebugCheckHasScaffoldMessenger(context);
        return context.DependOnInheritedWidgetOfExactType<ScaffoldMessengerScope>()!.Messenger;
    }

    public static ScaffoldMessengerState? MaybeOf(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<ScaffoldMessengerScope>()?.Messenger;
}

internal sealed class ScaffoldMessengerScope : InheritedWidget
{
    public ScaffoldMessengerScope(
        ScaffoldMessengerState messenger,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Messenger = messenger;
    }

    public ScaffoldMessengerState Messenger { get; }
    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldMessengerScope)oldWidget;
        return !ReferenceEquals(Messenger, oldScope.Messenger);
    }
}

public sealed class ScaffoldMessengerState : State<ScaffoldMessenger>
{
    private sealed class MaterialBannerEntry
    {
        public required MaterialBanner Presented { get; init; }
        public required ScaffoldFeatureController<MaterialBanner, MaterialBannerClosedReason> Controller { get; init; }
    }

    private sealed class SnackBarEntry
    {
        public required SnackBar Presented { get; init; }
        public required ScaffoldFeatureController<SnackBar, SnackBarClosedReason> Controller { get; init; }
    }

    private readonly LinkedList<SnackBarEntry> _snackBars = [];
    private readonly Queue<MaterialBannerEntry> _materialBanners = [];
    private readonly List<ScaffoldState> _scaffolds = [];
    private MaterialBannerEntry? _currentMaterialBanner;
    private AnimationController? _snackBarController;
    private AnimationController? _materialBannerAnimation;
    private CancellationTokenSource? _snackBarTimer;
    private bool _accessibleNavigation;
    private bool _disposed;

    private ScaffoldMessenger CurrentWidget => (ScaffoldMessenger)StateWidget;

    internal SnackBar? CurrentSnackBar => _snackBars.First?.Value.Presented;
    internal ScaffoldFeatureController<SnackBar, SnackBarClosedReason>? CurrentSnackBarController =>
        _snackBars.First?.Value.Controller;
    internal ScaffoldFeatureController<MaterialBanner, MaterialBannerClosedReason>? CurrentMaterialBannerController =>
        _currentMaterialBanner?.Controller;

    internal void Register(ScaffoldState scaffold)
    {
        ArgumentNullException.ThrowIfNull(scaffold);
        if (!_scaffolds.Contains(scaffold))
        {
            _scaffolds.Add(scaffold);
        }
        if (IsRoot(scaffold))
        {
            if (_snackBars.Count > 0)
            {
                scaffold.UpdateSnackBar();
            }
            if (_materialBanners.Count > 0)
            {
                scaffold.UpdateMaterialBanner();
            }
        }
    }

    internal void Unregister(ScaffoldState scaffold)
    {
        bool removed = _scaffolds.Remove(scaffold);
        DebugAssertions.Assert(removed);
    }

    private void UpdateScaffolds()
    {
        foreach (ScaffoldState scaffold in _scaffolds)
        {
            if (IsRoot(scaffold))
            {
                scaffold.UpdateSnackBar();
                scaffold.UpdateMaterialBanner();
            }
        }
    }

    private bool IsRoot(ScaffoldState scaffold)
    {
        ScaffoldState? parent = scaffold.Context.FindAncestorStateOfType<ScaffoldState>();
        return parent is null || !_scaffolds.Contains(parent);
    }

    public ScaffoldFeatureController<SnackBar, SnackBarClosedReason> ShowSnackBar(
        SnackBar snackBar,
        AnimationStyle? snackBarAnimationStyle = null)
    {
        ArgumentNullException.ThrowIfNull(snackBar);
        DebugAssertions.Assert(
            _scaffolds.Count > 0,
            "ScaffoldMessenger.showSnackBar was called, but there are currently no "
            + "descendant Scaffolds to present to.");

        if (snackBarAnimationStyle is not null
            && (_snackBarController?.Duration != snackBarAnimationStyle.Duration
                || _snackBarController?.ReverseDuration != snackBarAnimationStyle.ReverseDuration))
        {
            _snackBarController?.Dispose();
            _snackBarController = null;
        }

        if (_snackBarController is null)
        {
            _snackBarController = SnackBar.CreateAnimationController(
                duration: snackBarAnimationStyle?.Duration,
                reverseDuration: snackBarAnimationStyle?.ReverseDuration);
            _snackBarController.AddStatusListener(HandleSnackBarStatusChanged);
        }

        if (_snackBars.Count == 0)
        {
            DebugAssertions.Assert(_snackBarController.Status == AnimationStatus.Dismissed);
            _snackBarController.Forward();
        }

        SnackBarEntry? entry = null;
        var controller = new ScaffoldFeatureController<SnackBar, SnackBarClosedReason>(
            snackBar.WithAnimation(_snackBarController, new UniqueKey()),
            () =>
            {
                DebugAssertions.Assert(ReferenceEquals(_snackBars.First?.Value, entry));
                HideCurrentSnackBar();
            });
        entry = new SnackBarEntry
        {
            Presented = controller.Feature,
            Controller = controller,
        };

        try
        {
            SetState(() => _snackBars.AddLast(entry));
            UpdateScaffolds();
        }
        catch (FlutterError exception) when (Constants.KDebugMode
            && exception.Diagnostics[0].ToDescription() == "setState() or markNeedsBuild() called during build.")
        {
            throw new FlutterError(
            [
                new ErrorSummary("The showSnackBar() method cannot be called during build."),
                new ErrorDescription(
                    "The showSnackBar() method was called during build, which is "
                    + "prohibited as showing snack bars requires updating state. Updating "
                    + "state is not possible during build."),
                new ErrorHint(
                    "Instead of calling showSnackBar() during build, call it directly "
                    + "in your on tap (and related) callbacks. If you need to immediately "
                    + "show a snack bar, make the call in initState() or "
                    + "didChangeDependencies() instead. Otherwise, you can also schedule a "
                    + "post-frame callback using SchedulerBinding.addPostFrameCallback to "
                    + "show the snack bar after the current frame."),
                Context.DescribeOwnershipChain("The ownership chain for the particular ScaffoldMessenger is"),
            ]);
        }
        return controller;
    }

    public void HideCurrentSnackBar(SnackBarClosedReason reason = SnackBarClosedReason.Hide)
    {
        if (_snackBars.Count == 0 || _snackBarController!.Status == AnimationStatus.Dismissed)
        {
            return;
        }

        SnackBarEntry entry = _snackBars.First!.Value;
        if (_accessibleNavigation)
        {
            _snackBarController!.SetValue(0.0);
            entry.Controller.Complete(reason);
        }
        else
        {
            TickerFuture reverse = _snackBarController!.Reverse();
            EventLoop.RunAsync(async () =>
            {
                await reverse;
                DebugAssertions.Assert(Mounted);
                entry.Controller.Complete(reason);
            });
        }

        CancelSnackBarTimer();
    }

    public void RemoveCurrentSnackBar(SnackBarClosedReason reason = SnackBarClosedReason.Remove)
    {
        if (_snackBars.Count == 0)
        {
            return;
        }

        SnackBarEntry entry = _snackBars.First!.Value;
        entry.Controller.Complete(reason);
        CancelSnackBarTimer();
        // This will trigger the animation's status callback, which removes the entry.
        _snackBarController!.SetValue(0.0);
    }

    public void ClearSnackBars()
    {
        if (_snackBars.Count == 0 || _snackBarController!.Status == AnimationStatus.Dismissed)
        {
            return;
        }

        SnackBarEntry current = _snackBars.First!.Value;
        _snackBars.Clear();
        _snackBars.AddLast(current);
        HideCurrentSnackBar();
    }

    public ScaffoldFeatureController<MaterialBanner, MaterialBannerClosedReason> ShowMaterialBanner(
        MaterialBanner materialBanner)
    {
        ArgumentNullException.ThrowIfNull(materialBanner);
        DebugAssertions.Assert(
            _scaffolds.Count > 0,
            "ScaffoldMessenger.showMaterialBanner was called, but there are currently no "
            + "descendant Scaffolds to present to.");

        MaterialBannerEntry? entry = null;
        if (_materialBannerAnimation is null)
        {
            _materialBannerAnimation = MaterialBanner.CreateAnimationController();
            _materialBannerAnimation.AddStatusListener(HandleMaterialBannerAnimationStatusChanged);
        }
        if (_materialBanners.Count == 0)
        {
            DebugAssertions.Assert(_materialBannerAnimation.Status == AnimationStatus.Dismissed);
            _materialBannerAnimation.Forward();
        }
        MaterialBanner presented = materialBanner.WithAnimation(
            _materialBannerAnimation,
            materialBanner.Key ?? new UniqueKey());
        var controller = new ScaffoldFeatureController<MaterialBanner, MaterialBannerClosedReason>(
            presented,
            () => CloseMaterialBannerEntry(entry!));
        entry = new MaterialBannerEntry
        {
            Presented = presented,
            Controller = controller,
        };

        SetState(() =>
        {
            _materialBanners.Enqueue(entry);
            if (_currentMaterialBanner is null)
            {
                _currentMaterialBanner = _materialBanners.Peek();
            }
        });
        UpdateScaffolds();
        return controller;
    }

    public void HideCurrentMaterialBanner(
        MaterialBannerClosedReason reason = MaterialBannerClosedReason.Hide)
    {
        MaterialBannerEntry? entry = _currentMaterialBanner;
        if (entry is null || _materialBannerAnimation!.Status == AnimationStatus.Dismissed)
        {
            return;
        }

        if (_accessibleNavigation)
        {
            _materialBannerAnimation!.SetValue(0.0);
            entry.Controller.Complete(reason);
        }
        else
        {
            TickerFuture reverse = _materialBannerAnimation!.Reverse();
            EventLoop.RunAsync(async () =>
            {
                await reverse;
                DebugAssertions.Assert(Mounted);
                entry.Controller.Complete(reason);
            });
        }
    }

    public void RemoveCurrentMaterialBanner(
        MaterialBannerClosedReason reason = MaterialBannerClosedReason.Remove)
    {
        MaterialBannerEntry? entry = _currentMaterialBanner;
        if (entry is null)
        {
            return;
        }

        entry.Controller.Complete(reason);
        _materialBannerAnimation!.SetValue(0.0);
    }

    public void ClearMaterialBanners()
    {
        MaterialBannerEntry? current = _currentMaterialBanner;
        if (current is null || _materialBannerAnimation!.Status == AnimationStatus.Dismissed)
        {
            return;
        }

        _materialBanners.Clear();
        _materialBanners.Enqueue(current);
        HideCurrentMaterialBanner();
    }

    public override void DidChangeDependencies()
    {
        _accessibleNavigation = MediaQuery.AccessibleNavigationOf(Context);
        base.DidChangeDependencies();
    }

    public override Widget Build(BuildContext context)
    {
        WidgetsDebug.DebugCheckHasMediaQuery(context);
        _accessibleNavigation = MediaQuery.AccessibleNavigationOf(context);
        if (_snackBars.Count > 0)
        {
            ModalRoute? route = ModalRoute.MaybeOf(context);
            if (route is null || route.IsCurrent)
            {
                if (_snackBarController!.Status == AnimationStatus.Completed && _snackBarTimer is null)
                {
                    SnackBar snackBar = _snackBars.First!.Value.Presented;
                    var cancellation = new CancellationTokenSource();
                    _snackBarTimer = cancellation;
                    EventLoop.RunAsync(() => RunSnackBarTimer(snackBar, cancellation.Token));
                }
            }
        }

        return new ScaffoldMessengerScope(
            messenger: this,
            child: CurrentWidget.Child);
    }

    public override void Dispose()
    {
        _disposed = true;
        if (_materialBannerAnimation is not null)
        {
            _materialBannerAnimation.RemoveStatusListener(HandleMaterialBannerAnimationStatusChanged);
            _materialBannerAnimation.Dispose();
        }
        if (_snackBarController is not null)
        {
            _snackBarController.RemoveStatusListener(HandleSnackBarStatusChanged);
            _snackBarController.Dispose();
        }

        CancelSnackBarTimer();

        base.Dispose();
    }

    private void HandleSnackBarStatusChanged(AnimationStatus status)
    {
        switch (status)
        {
            case AnimationStatus.Dismissed:
                if (_disposed)
                {
                    return;
                }
                DebugAssertions.Assert(_snackBars.Count > 0);
                SetState(() => _snackBars.RemoveFirst());
                UpdateScaffolds();
                if (_snackBars.Count > 0)
                {
                    _snackBarController!.Forward();
                }

                break;
            case AnimationStatus.Completed:
                if (_disposed)
                {
                    return;
                }

                SetState(() => DebugAssertions.Assert(_snackBarTimer is null));
                UpdateScaffolds();
                break;
        }
    }

    private async Task RunSnackBarTimer(SnackBar snackBar, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(snackBar.Duration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                return;
            }

            DebugAssertions.Assert(_snackBarController!.Status.IsForwardOrCompleted());
            if (snackBar.Persist)
            {
                return;
            }

            HideCurrentSnackBar(SnackBarClosedReason.Timeout);
        });
    }

    private void CancelSnackBarTimer()
    {
        _snackBarTimer?.Cancel();
        _snackBarTimer?.Dispose();
        _snackBarTimer = null;
    }

    private void CloseMaterialBannerEntry(MaterialBannerEntry entry)
    {
        DebugAssertions.Assert(ReferenceEquals(entry, _currentMaterialBanner));
        HideCurrentMaterialBanner();
    }

    private void HandleMaterialBannerAnimationStatusChanged(AnimationStatus status)
    {
        if (_disposed)
        {
            return;
        }
        if (status == AnimationStatus.Dismissed)
        {
            DebugAssertions.Assert(_materialBanners.Count > 0);
            SetState(() =>
            {
                _materialBanners.Dequeue();
                _currentMaterialBanner = _materialBanners.Count > 0 ? _materialBanners.Peek() : null;
            });
            UpdateScaffolds();
            if (_materialBanners.Count > 0)
            {
                _materialBannerAnimation!.Forward();
            }
        }
        else if (status == AnimationStatus.Completed)
        {
            UpdateScaffolds();
        }
    }

}
