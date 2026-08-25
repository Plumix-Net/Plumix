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

    public static ScaffoldMessengerState Of(BuildContext context) =>
        MaybeOf(context) ?? throw new InvalidOperationException("ScaffoldMessenger not found in context.");

    public static ScaffoldMessengerState? MaybeOf(BuildContext context) =>
        context.DependOnInherited<ScaffoldMessengerScope>()?.Messenger;
}

internal sealed class ScaffoldMessengerScope : InheritedWidget
{
    public ScaffoldMessengerScope(
        ScaffoldMessengerState messenger,
        SnackBar? snackBar,
        MaterialBanner? materialBanner,
        Widget child,
        Key? key = null) : base(key)
    {
        Messenger = messenger;
        SnackBar = snackBar;
        MaterialBanner = materialBanner;
        Child = child;
    }

    public ScaffoldMessengerState Messenger { get; }
    public SnackBar? SnackBar { get; }
    public MaterialBanner? MaterialBanner { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ScaffoldMessengerScope)oldWidget;
        return !ReferenceEquals(Messenger, oldScope.Messenger)
               || !ReferenceEquals(SnackBar, oldScope.SnackBar)
               || !ReferenceEquals(MaterialBanner, oldScope.MaterialBanner);
    }
}

public sealed class ScaffoldMessengerState : State
{
    private sealed class MaterialBannerEntry
    {
        public required MaterialBanner Presented { get; init; }
        public required ScaffoldFeatureController<MaterialBanner, MaterialBannerClosedReason> Controller { get; init; }
        public MaterialBannerClosedReason PendingReason { get; set; } = MaterialBannerClosedReason.Hide;
    }

    private sealed class SnackBarEntry
    {
        public required SnackBar Presented { get; init; }
        public required ScaffoldFeatureController<SnackBar, SnackBarClosedReason> Controller { get; init; }
    }

    private readonly LinkedList<SnackBarEntry> _snackBars = [];
    private readonly Queue<MaterialBannerEntry> _materialBanners = [];
    private readonly HashSet<ScaffoldState> _scaffolds = [];
    private MaterialBannerEntry? _currentMaterialBanner;
    private AnimationController? _snackBarController;
    private AnimationController? _materialBannerAnimation;
    private CancellationTokenSource? _snackBarTimer;
    private bool _accessibleNavigation;
    private bool _disposed;

    private ScaffoldMessenger CurrentWidget => (ScaffoldMessenger)StateWidget;

    internal SnackBar? CurrentSnackBar => _snackBars.First?.Value.Presented;

    internal void Register(ScaffoldState scaffold)
    {
        ArgumentNullException.ThrowIfNull(scaffold);
        _scaffolds.Add(scaffold);
    }

    internal void Unregister(ScaffoldState scaffold)
    {
        _scaffolds.Remove(scaffold);
    }

    internal SnackBar? SnackBarFor(ScaffoldState scaffold)
    {
        return IsRoot(scaffold) ? CurrentSnackBar : null;
    }

    internal MaterialBanner? MaterialBannerFor(ScaffoldState scaffold)
    {
        return IsRoot(scaffold) ? _currentMaterialBanner?.Presented : null;
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
        if (_scaffolds.Count == 0)
        {
            throw new InvalidOperationException(
                "ScaffoldMessenger.ShowSnackBar was called, but there are currently no "
                + "descendant Scaffolds to present to.");
        }

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
            _snackBarController.Forward();
        }

        SnackBarEntry? entry = null;
        var controller = new ScaffoldFeatureController<SnackBar, SnackBarClosedReason>(
            snackBar.WithAnimation(_snackBarController, new UniqueKey()),
            () =>
            {
                // The Dart close callback asserts the entry is still the head of the queue.
                if (ReferenceEquals(_snackBars.First?.Value, entry))
                {
                    HideCurrentSnackBar();
                }
            });
        entry = new SnackBarEntry
        {
            Presented = controller.Feature,
            Controller = controller,
        };

        SetState(() => _snackBars.AddLast(entry));
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
            _snackBarController!.Reverse().WhenCompleteOrCancel(() => entry.Controller.Complete(reason));
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
        if (_scaffolds.Count == 0)
        {
            throw new InvalidOperationException(
                "ScaffoldMessenger.ShowMaterialBanner was called, but there are currently no "
                + "descendant Scaffolds to present to.");
        }

        MaterialBannerEntry? entry = null;
        if (_materialBannerAnimation is null)
        {
            _materialBannerAnimation = MaterialBanner.CreateAnimationController();
            _materialBannerAnimation.AddStatusListener(HandleMaterialBannerAnimationStatusChanged);
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
                ShowNextMaterialBanner();
            }
        });
        return controller;
    }

    public void HideCurrentMaterialBanner(
        MaterialBannerClosedReason reason = MaterialBannerClosedReason.Hide)
    {
        MaterialBannerEntry? entry = _currentMaterialBanner;
        if (entry is null)
        {
            return;
        }

        entry.PendingReason = reason;
        bool accessibleNavigation = MediaQuery.MaybeOf(Context)?.AccessibleNavigation == true;
        if (accessibleNavigation || _materialBannerAnimation!.Value <= 0.0)
        {
            CompleteCurrentMaterialBanner(entry);
            return;
        }

        _materialBannerAnimation.Reverse();
    }

    public void RemoveCurrentMaterialBanner(
        MaterialBannerClosedReason reason = MaterialBannerClosedReason.Remove)
    {
        MaterialBannerEntry? entry = _currentMaterialBanner;
        if (entry is null)
        {
            return;
        }

        entry.PendingReason = reason;
        CompleteCurrentMaterialBanner(entry);
    }

    public void ClearMaterialBanners()
    {
        MaterialBannerEntry? current = _currentMaterialBanner;
        if (current is null)
        {
            return;
        }

        foreach (MaterialBannerEntry queued in _materialBanners.Skip(1).ToArray())
        {
            RemoveQueuedMaterialBanner(queued);
        }
        HideCurrentMaterialBanner();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        bool accessibleNavigation = MediaQuery.MaybeOf(Context)?.AccessibleNavigation == true;
        // If we transition from accessible navigation to non-accessible navigation and there is a
        // SnackBar that would have timed out that has already completed its timer, dismiss that
        // SnackBar. If the timer hasn't finished yet, let it timeout as normal.
        if (_accessibleNavigation
            && !accessibleNavigation
            && _snackBars.Count > 0
            && _snackBarTimer is null)
        {
            HideCurrentSnackBar(SnackBarClosedReason.Timeout);
        }

        _accessibleNavigation = accessibleNavigation;
    }

    public override Widget Build(BuildContext context)
    {
        _accessibleNavigation = MediaQuery.MaybeOf(context)?.AccessibleNavigation == true;
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
                    _ = RunSnackBarTimer(snackBar, cancellation.Token);
                }
            }
        }

        return new ScaffoldMessengerScope(
            messenger: this,
            snackBar: CurrentSnackBar,
            materialBanner: _currentMaterialBanner?.Presented,
            child: CurrentWidget.Child);
    }

    public override void Dispose()
    {
        _disposed = true;
        foreach (SnackBarEntry entry in _snackBars)
        {
            entry.Controller.Complete(SnackBarClosedReason.Remove);
        }

        _snackBars.Clear();
        if (_snackBarController is not null)
        {
            _snackBarController.RemoveStatusListener(HandleSnackBarStatusChanged);
            _snackBarController.Dispose();
            _snackBarController = null;
        }

        CancelSnackBarTimer();
        _materialBanners.Clear();
        _currentMaterialBanner = null;
        if (_materialBannerAnimation is not null)
        {
            _materialBannerAnimation.RemoveStatusListener(HandleMaterialBannerAnimationStatusChanged);
            _materialBannerAnimation.Dispose();
            _materialBannerAnimation = null;
        }
        _scaffolds.Clear();
    }

    private void HandleSnackBarStatusChanged(AnimationStatus status)
    {
        switch (status)
        {
            case AnimationStatus.Dismissed:
                if (_snackBars.Count == 0 || _disposed)
                {
                    return;
                }

                SetState(() => _snackBars.RemoveFirst());
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

                SetState(() => { });
                break;
        }
    }

    private async Task RunSnackBarTimer(SnackBar snackBar, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(snackBar.Duration, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (cancellationToken.IsCancellationRequested || _disposed || snackBar.Persist)
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
        if (!ReferenceEquals(entry, _currentMaterialBanner))
        {
            throw new InvalidOperationException("Only the current MaterialBanner can be closed.");
        }

        HideCurrentMaterialBanner();
    }

    private void ShowNextMaterialBanner()
    {
        if (_currentMaterialBanner is not null || _materialBanners.Count == 0)
        {
            return;
        }

        _currentMaterialBanner = _materialBanners.Peek();
        // Flutter forwards without resetting the value: the controller is dismissed at this point, and
        // a `from: 0.0` would report `dismissed` again and re-enter the status handler.
        _materialBannerAnimation!.Forward();
    }

    private void CompleteCurrentMaterialBanner(MaterialBannerEntry entry)
    {
        if (!ReferenceEquals(entry, _currentMaterialBanner))
        {
            return;
        }

        entry.Controller.Complete(entry.PendingReason);
        if (_materialBanners.Count > 0 && ReferenceEquals(_materialBanners.Peek(), entry))
        {
            _materialBanners.Dequeue();
        }

        _currentMaterialBanner = null;
        if (_disposed)
        {
            return;
        }

        SetState(ShowNextMaterialBanner);
    }

    private void RemoveQueuedMaterialBanner(MaterialBannerEntry entry)
    {
        if (!_materialBanners.Contains(entry))
        {
            return;
        }

        MaterialBannerEntry[] retained = _materialBanners
            .Where(candidate => !ReferenceEquals(candidate, entry))
            .ToArray();
        _materialBanners.Clear();
        foreach (MaterialBannerEntry candidate in retained)
        {
            _materialBanners.Enqueue(candidate);
        }
    }

    private void HandleMaterialBannerAnimationStatusChanged(AnimationStatus status)
    {
        if (status == AnimationStatus.Dismissed && _currentMaterialBanner is not null)
        {
            CompleteCurrentMaterialBanner(_currentMaterialBanner);
        }
    }

}
