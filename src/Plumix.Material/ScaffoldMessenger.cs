using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/scaffold.dart

public sealed class ScaffoldFeatureController<TFeature, TClosedReason>
    where TFeature : Widget
{
    private readonly Action _close;
    private readonly TaskCompletionSource<TClosedReason> _closed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ScaffoldFeatureController(TFeature feature, Action close)
    {
        Feature = feature;
        _close = close;
    }

    public TFeature Feature { get; }
    public Task<TClosedReason> Closed => _closed.Task;

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
        public required SnackBar Original { get; init; }
        public required SnackBar Presented { get; init; }
        public required AnimationController Animation { get; init; }
        public required ScaffoldFeatureController<SnackBar, SnackBarClosedReason> Controller { get; init; }
        public SnackBarClosedReason PendingReason { get; set; } = SnackBarClosedReason.Hide;
        public CancellationTokenSource? TimeoutCancellation { get; set; }
    }

    private readonly Queue<SnackBarEntry> _snackBars = [];
    private readonly Queue<MaterialBannerEntry> _materialBanners = [];
    private readonly HashSet<ScaffoldState> _scaffolds = [];
    private SnackBarEntry? _currentSnackBar;
    private MaterialBannerEntry? _currentMaterialBanner;
    private AnimationController? _materialBannerAnimation;
    private bool _disposed;

    private ScaffoldMessenger CurrentWidget => (ScaffoldMessenger)StateWidget;

    internal SnackBar? CurrentSnackBar => _currentSnackBar?.Presented;

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
        return IsRoot(scaffold) ? _currentSnackBar?.Presented : null;
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

    public ScaffoldFeatureController<SnackBar, SnackBarClosedReason> ShowSnackBar(SnackBar snackBar)
    {
        ArgumentNullException.ThrowIfNull(snackBar);

        SnackBarEntry? entry = null;
        var controller = new ScaffoldFeatureController<SnackBar, SnackBarClosedReason>(
            snackBar,
            () => CloseEntry(entry!, SnackBarClosedReason.Hide));
        var animation = SnackBar.CreateAnimationController();
        entry = new SnackBarEntry
        {
            Original = snackBar,
            Presented = snackBar.WithAnimation(animation, snackBar.Key ?? new UniqueKey()),
            Animation = animation,
            Controller = controller,
        };
        animation.Completed += () => BeginTimeout(entry);
        animation.Dismissed += () => CompleteCurrent(entry);

        SetState(() =>
        {
            _snackBars.Enqueue(entry);
            if (_currentSnackBar is null)
            {
                ShowNextSnackBar();
            }
        });
        return controller;
    }

    public void HideCurrentSnackBar(SnackBarClosedReason reason = SnackBarClosedReason.Hide)
    {
        var entry = _currentSnackBar;
        if (entry is null) return;
        CancelTimeout(entry);
        entry.PendingReason = reason;
        if (entry.Animation.Value <= 0)
        {
            CompleteCurrent(entry);
            return;
        }

        entry.Animation.Reverse();
    }

    public void RemoveCurrentSnackBar(SnackBarClosedReason reason = SnackBarClosedReason.Remove)
    {
        var entry = _currentSnackBar;
        if (entry is null) return;
        entry.PendingReason = reason;
        CompleteCurrent(entry);
    }

    public void ClearSnackBars()
    {
        while (_snackBars.Count > 1)
        {
            var queued = _snackBars.Last();
            RemoveQueuedEntry(queued, SnackBarClosedReason.Remove);
        }

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

    public override Widget Build(BuildContext context)
    {
        return new ScaffoldMessengerScope(
            messenger: this,
            snackBar: CurrentSnackBar,
            materialBanner: _currentMaterialBanner?.Presented,
            child: CurrentWidget.Child);
    }

    public override void Dispose()
    {
        _disposed = true;
        foreach (var entry in _snackBars.ToArray())
        {
            CancelTimeout(entry);
            entry.Animation.Dispose();
            entry.Controller.Complete(SnackBarClosedReason.Remove);
        }

        _snackBars.Clear();
        _currentSnackBar = null;
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

    private void CloseEntry(SnackBarEntry entry, SnackBarClosedReason reason)
    {
        if (ReferenceEquals(entry, _currentSnackBar))
        {
            HideCurrentSnackBar(reason);
            return;
        }

        RemoveQueuedEntry(entry, reason);
    }

    private void RemoveQueuedEntry(SnackBarEntry entry, SnackBarClosedReason reason)
    {
        if (!_snackBars.Contains(entry)) return;
        var retained = _snackBars.Where(candidate => !ReferenceEquals(candidate, entry)).ToArray();
        _snackBars.Clear();
        foreach (var candidate in retained) _snackBars.Enqueue(candidate);
        CancelTimeout(entry);
        entry.Animation.Dispose();
        entry.Controller.Complete(reason);
    }

    private void ShowNextSnackBar()
    {
        if (_currentSnackBar is not null || _snackBars.Count == 0) return;
        _currentSnackBar = _snackBars.Peek();
        _currentSnackBar.Animation.Forward(from: 0);
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
        _materialBannerAnimation!.Forward(from: 0.0);
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

    private void CompleteCurrent(SnackBarEntry entry)
    {
        if (!ReferenceEquals(entry, _currentSnackBar)) return;
        CancelTimeout(entry);
        entry.Animation.Dispose();
        if (_snackBars.Count > 0 && ReferenceEquals(_snackBars.Peek(), entry))
        {
            _snackBars.Dequeue();
        }

        _currentSnackBar = null;
        entry.Controller.Complete(entry.PendingReason);
        if (_disposed) return;
        SetState(ShowNextSnackBar);
    }

    private void BeginTimeout(SnackBarEntry entry)
    {
        if (!ReferenceEquals(entry, _currentSnackBar)) return;
        if (entry.Original.Persist) return;
        if (entry.Original.Action is not null
            && MediaQuery.MaybeOf(Context)?.AccessibleNavigation == true)
        {
            return;
        }

        CancelTimeout(entry);
        var cancellation = new CancellationTokenSource();
        entry.TimeoutCancellation = cancellation;
        _ = WaitForTimeout(entry, cancellation.Token);
    }

    private async Task WaitForTimeout(SnackBarEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(entry.Original.Duration, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!cancellationToken.IsCancellationRequested && ReferenceEquals(entry, _currentSnackBar))
                    {
                        HideCurrentSnackBar(SnackBarClosedReason.Timeout);
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void CancelTimeout(SnackBarEntry entry)
    {
        entry.TimeoutCancellation?.Cancel();
        entry.TimeoutCancellation?.Dispose();
        entry.TimeoutCancellation = null;
    }
}
