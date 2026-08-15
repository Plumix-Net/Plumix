using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/routes.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

namespace Plumix.Widgets;

public record RouteSettings(string? Name = null, object? Arguments = null);
public delegate Route? RouteFactory(RouteSettings settings);
public delegate IReadOnlyList<Route> NavigatorInitialRouteListFactory(
    NavigatorState navigator,
    string initialRouteName);
public delegate bool RoutePredicate(Route route);
public delegate void DidRemovePageCallback(Page page);
public delegate Widget RoutePageBuilder(
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation);
public delegate Widget RouteTransitionsBuilder(
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation,
    Widget child);
public delegate Widget? DelegatedTransitionBuilder(
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation,
    bool allowSnapshotting,
    Widget? child);

public enum RoutePopDisposition
{
    Pop,
    DoNotPop,
    Bubble,
}

/// <summary>
/// Flutter's <c>_ModalRouteAspect</c>: the piece of route status a <see cref="ModalRoute"/> dependent reads.
/// Depending on one aspect rebuilds the dependent only when that aspect changes.
/// </summary>
public enum ModalRouteAspect
{
    IsCurrent,
    CanPop,
    Settings,
    IsActive,
    IsFirst,
    Opaque,
    PopDisposition,
}

public delegate void PopInvokedCallback(bool didPop);
public delegate void PopInvokedWithResultCallback<T>(bool didPop, T? result);
public delegate void PopResultCallback<T>(T? result);

public interface PopEntry
{
    IValueListenable<bool> CanPopNotifier { get; }

    void OnPopInvoked(bool didPop)
    {
    }

    void OnPopInvokedWithResult(bool didPop, object? result)
    {
        OnPopInvoked(didPop);
    }
}

public sealed class RouteData
{
    private static readonly IReadOnlyDictionary<string, string> EmptyQueryParameters =
        new Dictionary<string, string>();

    public RouteData(
        string name,
        IReadOnlyDictionary<string, string>? queryParameters = null,
        object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        QueryParameters = queryParameters ?? EmptyQueryParameters;
        Arguments = arguments;
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> QueryParameters { get; }

    public object? Arguments { get; }

    public static RouteData FromLocation(string location, object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location cannot be null or whitespace.", nameof(location));
        }

        string normalized = NormalizeLocation(location);
        int queryIndex = normalized.IndexOf('?', StringComparison.Ordinal);
        string path = queryIndex >= 0
            ? normalized[..queryIndex]
            : normalized;
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        string query = queryIndex >= 0 ? normalized[(queryIndex + 1)..] : string.Empty;
        return new RouteData(
            name: path,
            queryParameters: ParseQueryParameters(query),
            arguments: arguments);
    }

    private static string NormalizeLocation(string location)
    {
        if (location.Contains("://", StringComparison.Ordinal)
            && Uri.TryCreate(location, UriKind.Absolute, out var absoluteUri))
        {
            string normalized = absoluteUri.PathAndQuery;
            if (!string.IsNullOrEmpty(absoluteUri.Fragment))
            {
                normalized += absoluteUri.Fragment;
            }

            return normalized;
        }

        return location;
    }

    private static IReadOnlyDictionary<string, string> ParseQueryParameters(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return EmptyQueryParameters;
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            string rawKey = separatorIndex < 0 ? pair : pair[..separatorIndex];
            if (rawKey.Length == 0)
            {
                continue;
            }

            string rawValue = separatorIndex < 0 ? string.Empty : pair[(separatorIndex + 1)..];
            string key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
            string value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            parameters[key] = value;
        }

        return parameters.Count == 0
            ? EmptyQueryParameters
            : parameters;
    }
}

public sealed class LocalHistoryEntry
{
    private bool _removed;

    public LocalHistoryEntry(Action? onRemove = null, bool impliesAppBarDismissal = true)
    {
        OnRemove = onRemove;
        ImpliesAppBarDismissal = impliesAppBarDismissal;
    }

    public Action? OnRemove { get; }

    public bool ImpliesAppBarDismissal { get; }

    internal Route? Owner { get; set; }

    public void Remove()
    {
        if (_removed)
        {
            return;
        }

        var owner = Owner;
        if (owner != null)
        {
            owner.RemoveLocalHistoryEntry(this);
            return;
        }

        MarkRemoved();
    }

    internal void MarkRemoved()
    {
        if (_removed)
        {
            return;
        }

        _removed = true;
        Owner = null;
        OnRemove?.Invoke();
    }
}

public abstract class Route
{
    private readonly TaskCompletionSource<object?> _popCompleter =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ValueNotifier<string?> _restorationScopeId = new(null);
    private readonly bool? _requestFocus;
    private List<LocalHistoryEntry>? _localHistoryEntries;

    protected Route(RouteSettings? settings = null, bool? requestFocus = null)
    {
        Settings = settings ?? new RouteSettings();
        _requestFocus = requestFocus;
    }

    public RouteSettings Settings { get; private set; }

    /// <summary>
    /// Flutter's <c>Route.restorationScopeId</c>: the id the route's subtree stores restoration data under, or
    /// <see langword="null"/> while the route is not restorable.
    /// </summary>
    public IValueListenable<string?> RestorationScopeId => _restorationScopeId;

    /// <summary>Completes with the route's result once it was popped off its navigator.</summary>
    public Task<object?> Popped => _popCompleter.Task;

    /// <summary>Whether this route describes a <see cref="Widgets.Page"/> supplied through the pages API.</summary>
    public bool IsPageBased => Settings is Page;

    /// <summary>Flutter's <c>Route.currentResult</c>: the result used when the route is completed implicitly.</summary>
    public virtual object? CurrentResult => null;

    /// <summary>
    /// Whether the navigator hands focus to this route when it is pushed; falls back to
    /// <see cref="Widgets.Navigator.RequestFocus"/>.
    /// </summary>
    public bool RequestFocus => _requestFocus ?? Navigator?.NavigatorWidget.RequestFocus ?? false;

    internal bool PopCompleted => _popCompleter.Task.IsCompleted;

    internal bool Installed => Navigator is not null;

    internal bool IsInstalledIn(NavigatorState navigator) => ReferenceEquals(Navigator, navigator);

    internal void UpdateRestorationId(string? restorationId) => _restorationScopeId.Value = restorationId;

    /// <summary>Flutter's <c>Route._updateSettings</c>: adopts the new page/settings during a pages update.</summary>
    internal void UpdateSettings(RouteSettings newSettings)
    {
        if (Equals(Settings, newSettings))
        {
            return;
        }

        Settings = newSettings;
        if (Installed)
        {
            ChangedInternalState();
        }
    }

    public virtual bool Opaque => true;

    /// <summary>
    /// The overlay entries this route installs into <see cref="NavigatorState.Overlay"/>. The base route
    /// contributes nothing; <see cref="OverlayRoute"/> fills the list from <c>CreateOverlayEntries</c>.
    /// </summary>
    public virtual IReadOnlyList<OverlayEntry> OverlayEntries => Array.Empty<OverlayEntry>();

    public virtual RoutePopDisposition PopDisposition
    {
        get
        {
            if (Settings is Page { CanPop: false })
            {
                return RoutePopDisposition.DoNotPop;
            }

            return IsFirst ? RoutePopDisposition.Bubble : RoutePopDisposition.Pop;
        }
    }

    public bool IsCurrent => Navigator?.CurrentRoute == this;

    public bool IsFirst => Navigator?.IsFirst(this) == true;

    public bool IsActive => Navigator?.IsActive(this) == true;

    /// <summary>Whether there is at least one active route below this one in the navigator's history.</summary>
    public bool HasActiveRouteBelow => Navigator?.HasActiveRouteBelow(this) == true;

    public bool WillHandlePopInternally => _localHistoryEntries is { Count: > 0 };

    public virtual bool PopGestureEnabled => false;

    public virtual bool PopGestureInProgress => false;

    internal NavigatorState? Navigator { get; private set; }

    public virtual bool ImpliesAppBarDismissal
    {
        get
        {
            bool hasDismissalLocalHistory = _localHistoryEntries?.Any(entry => entry.ImpliesAppBarDismissal) == true;
            return hasDismissalLocalHistory || HasActiveRouteBelow;
        }
    }

    internal void Attach(NavigatorState navigator)
    {
        Navigator = navigator;
        Install();
        OnAttach();
    }

    internal void Detach()
    {
        OnDetach();
        Uninstall();
        Navigator = null;
    }

    protected virtual void Install()
    {
    }

    protected virtual void Uninstall()
    {
    }

    protected virtual void OnAttach()
    {
    }

    protected virtual void OnDetach()
    {
    }

    public virtual bool WillPop()
    {
        if (_localHistoryEntries is { Count: > 0 })
        {
            var entry = _localHistoryEntries[^1];
            _localHistoryEntries.RemoveAt(_localHistoryEntries.Count - 1);
            entry.MarkRemoved();
            OnLocalHistoryChanged();
            return false;
        }

        return true;
    }

    public virtual bool WillPop(object? result) => WillPop();

    /// <summary>
    /// Called when the route is pushed. Flutter returns the entering transition's <c>TickerFuture</c> and hands
    /// focus to the navigator's enclosing scope once it resolves; Plumix reports settling through
    /// <see cref="NotifyPushSettled"/> instead, because the framework has no ticker futures.
    /// </summary>
    public virtual void DidPush()
    {
        if (RequestFocus)
        {
            Navigator?.FocusNode.Scope?.RequestFirstFocus();
        }
    }

    /// <summary>Called when the route is inserted without an entering transition.</summary>
    public virtual void DidAdd()
    {
        if (RequestFocus)
        {
            Navigator?.FocusNode.Scope?.RequestFirstFocus();
        }
    }

    /// <summary>Called when this route replaced <paramref name="oldRoute"/> in place.</summary>
    public virtual void DidReplace(Route? oldRoute)
    {
    }

    /// <summary>
    /// Called when the route is popped off its navigator. Returning <see langword="false"/> vetoes the pop and
    /// returns the route to the idle state.
    /// </summary>
    public virtual bool DidPop(object? result)
    {
        DidComplete(result);
        return true;
    }

    public virtual void DidComplete(object? result)
    {
        _popCompleter.TrySetResult(result ?? CurrentResult);
    }

    public virtual void OnPopInvoked(bool didPop)
    {
    }

    public virtual void OnPopInvokedWithResult(bool didPop, object? result)
    {
        if (Settings is Page page)
        {
            page.OnPopInvoked?.Invoke(didPop, result);
        }

        OnPopInvoked(didPop);
    }

    /// <summary>
    /// Raised when the entering transition settled, mirroring Flutter's <c>didPush</c> ticker future. The
    /// navigator uses it to move the route from <c>pushing</c> to <c>idle</c>.
    /// </summary>
    internal event Action? PushSettled;

    /// <summary>Whether the entering transition settles in the same turn, as it does for non-animated routes.</summary>
    internal virtual bool PushSettlesImmediately => true;

    /// <summary>Reports that the entering transition settled or was cancelled.</summary>
    protected void NotifyPushSettled() => PushSettled?.Invoke();

    public virtual void DidPopNext(Route nextRoute)
    {
    }

    public virtual void DidChangeNext(Route? nextRoute)
    {
    }

    public virtual void DidChangePrevious(Route? previousRoute)
    {
    }

    public virtual void Dispose()
    {
        _restorationScopeId.Dispose();
        _popCompleter.TrySetResult(CurrentResult);
        if (_localHistoryEntries is not { Count: > 0 })
        {
            return;
        }

        foreach (var entry in _localHistoryEntries.ToArray())
        {
            _localHistoryEntries.Remove(entry);
            entry.MarkRemoved();
        }

        OnLocalHistoryChanged();
    }

    public void AddLocalHistoryEntry(LocalHistoryEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry.Owner != null && !ReferenceEquals(entry.Owner, this))
        {
            throw new InvalidOperationException("LocalHistoryEntry is already attached to another route.");
        }

        _localHistoryEntries ??= [];
        if (_localHistoryEntries.Contains(entry))
        {
            return;
        }

        entry.Owner = this;
        _localHistoryEntries.Add(entry);
        OnLocalHistoryChanged();
    }

    internal void RemoveLocalHistoryEntry(LocalHistoryEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (_localHistoryEntries is null)
        {
            return;
        }

        if (_localHistoryEntries.Remove(entry))
        {
            entry.MarkRemoved();
            OnLocalHistoryChanged();
        }
    }

    protected virtual void OnLocalHistoryChanged()
    {
        ChangedInternalState();
    }

    /// <summary>
    /// The hook routes call when their own state changed the page they build. Routes that drive their page
    /// from route-owned animations rely on this, so it maps to <see cref="ChangedExternalState"/>, which
    /// invalidates the cached page; the internal lifecycle hooks use <see cref="ChangedInternalState"/>.
    /// </summary>
    protected void NotifyRouteChanged()
    {
        ChangedExternalState();
    }

    /// <summary>
    /// Called when internal route state that affects the route's own widgets changed. Mirrors Flutter's
    /// <c>Route.changedInternalState</c>.
    /// </summary>
    protected internal virtual void ChangedInternalState()
    {
        Navigator?.NotifyRouteChanged();
    }

    /// <summary>
    /// Called when state external to the route (its position in the history) changed. Mirrors Flutter's
    /// <c>Route.changedExternalState</c>.
    /// </summary>
    protected internal virtual void ChangedExternalState()
    {
        Navigator?.NotifyRouteChanged();
    }

    public abstract Widget BuildPage(BuildContext context);
}

/// <summary>
/// A route that installs one or more <see cref="OverlayEntry"/> objects into the navigator's
/// <see cref="Overlay"/>.
/// </summary>
public abstract class OverlayRoute : Route
{
    private readonly List<OverlayEntry> _overlayEntries = [];

    protected OverlayRoute(RouteSettings? settings = null, bool? requestFocus = null)
        : base(settings, requestFocus)
    {
    }

    public override IReadOnlyList<OverlayEntry> OverlayEntries => _overlayEntries;

    /// <summary>Creates the overlay entries for this route, ordered from bottom-most to top-most.</summary>
    protected abstract IEnumerable<OverlayEntry> CreateOverlayEntries();

    /// <summary>Whether the route is finished (and can be disposed) as soon as it is popped.</summary>
    public virtual bool FinishedWhenPopped => true;

    protected override void Install()
    {
        if (_overlayEntries.Count > 0)
        {
            throw new InvalidOperationException("The route already installed its overlay entries.");
        }

        _overlayEntries.AddRange(CreateOverlayEntries());
        base.Install();
    }

    public override bool DidPop(object? result)
    {
        bool returnValue = base.DidPop(result);
        if (returnValue && FinishedWhenPopped)
        {
            Navigator?.FinalizeRoute(this);
        }

        return returnValue;
    }

    public override void Dispose()
    {
        foreach (OverlayEntry entry in _overlayEntries)
        {
            entry.Dispose();
        }

        _overlayEntries.Clear();
        base.Dispose();
    }
}

public abstract class TransitionRoute : OverlayRoute
{
    private protected static readonly Animation<double> AlwaysDismissedAnimation =
        new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);
    private protected static readonly Animation<double> AlwaysCompleteAnimation =
        new ConstantAnimation<double>(1.0, AnimationStatus.Completed);
    private readonly ProxyAnimation _secondaryAnimation = new(AlwaysDismissedAnimation);
    private AnimationController? _controller;
    private object? _result;
    private bool _isPopped;
    private bool _popFinalized;
    private bool _handlingPopGesture;

    protected TransitionRoute(RouteSettings? settings = null, bool? requestFocus = null)
        : base(settings, requestFocus)
    {
    }

    public virtual TimeSpan TransitionDuration => TimeSpan.Zero;

    public virtual TimeSpan ReverseTransitionDuration => TransitionDuration;

    public override object? CurrentResult => _result;

    public virtual bool AllowSnapshotting => true;

    /// <summary>
    /// Whether the route disposes the controller returned by <see cref="CreateAnimationController"/> when it is
    /// uninstalled. Routes that adopt a caller-supplied controller must return <see langword="false"/>.
    /// </summary>
    protected virtual bool WillDisposeAnimationController => true;

    public virtual Animation<double> Animation => _controller
        ?? throw new InvalidOperationException("The route animation is unavailable before the route is installed.");

    public virtual Animation<double> SecondaryAnimation => _secondaryAnimation;

    protected AnimationController Controller => _controller
        ?? throw new InvalidOperationException("The route controller is unavailable before the route is installed.");

    public override bool FinishedWhenPopped => IsTransitionDismissed && !_popFinalized;

    internal bool IsTransitionDismissed => _controller?.Status == AnimationStatus.Dismissed;

    internal bool EffectiveOpaque => Opaque && _controller?.Status == AnimationStatus.Completed;

    protected override void Install()
    {
        ValidateDuration(TransitionDuration, nameof(TransitionDuration));
        ValidateDuration(ReverseTransitionDuration, nameof(ReverseTransitionDuration));
        _isPopped = false;
        _popFinalized = false;
        _controller = CreateAnimationController();
        _controller.Changed += HandleAnimationChanged;
        _controller.AddStatusListener(HandleStatusChanged);
        base.Install();
        if (_controller.Status == AnimationStatus.Completed)
        {
            SetPrimaryEntryOpaque(Opaque);
        }
    }

    /// <summary>The raw controller-backed animations, before <see cref="ModalRoute.Offstage"/> proxies them.</summary>
    private protected Animation<double> RawAnimation => _controller ?? AlwaysDismissedAnimation;

    private protected bool HasController => _controller is not null;

    private protected Animation<double> RawSecondaryAnimation => _secondaryAnimation;

    protected override void Uninstall()
    {
        if (_controller is not null)
        {
            _controller.Changed -= HandleAnimationChanged;
            _controller.RemoveStatusListener(HandleStatusChanged);
            if (WillDisposeAnimationController)
            {
                _controller.Dispose();
            }

            _controller = null;
        }

        _secondaryAnimation.Parent = AlwaysDismissedAnimation;
        StopPopGestureIfNeeded();
        base.Uninstall();
    }

    protected virtual AnimationController CreateAnimationController()
    {
        return new AnimationController(duration: NormalizeDuration(TransitionDuration))
        {
            ReverseDuration = NormalizeDuration(ReverseTransitionDuration),
        };
    }

    public virtual bool CanTransitionTo(TransitionRoute nextRoute) => true;

    public virtual bool CanTransitionFrom(TransitionRoute previousRoute) => true;

    public virtual Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return child;
    }

    /// <summary>
    /// Flutter's <c>TransitionRoute.createSimulation</c>: a non-null simulation replaces the
    /// duration/curve drive of the transition controller for the given direction.
    /// </summary>
    protected internal virtual Simulation? CreateSimulation(bool forward) => null;

    internal override bool PushSettlesImmediately => false;

    public override void DidPush()
    {
        base.DidPush();
        _isPopped = false;
        if (CreateSimulation(forward: true) is { } simulation)
        {
            Controller.AnimateWith(simulation);
            return;
        }

        if (TransitionDuration == TimeSpan.Zero)
        {
            Controller.SetValue(1.0);
            NotifyPushSettled();
            return;
        }

        Controller.Duration = TransitionDuration;
        Controller.Forward(from: 0.0);
    }

    public override void DidAdd()
    {
        base.DidAdd();
        _isPopped = false;
        Controller.SetValue(1.0);
    }

    public override void DidReplace(Route? oldRoute)
    {
        _isPopped = false;
        if (oldRoute is TransitionRoute previousTransitionRoute)
        {
            Controller.SetValue(previousTransitionRoute.Controller.Value);
        }

        base.DidReplace(oldRoute);
    }

    public override bool DidPop(object? result)
    {
        _isPopped = true;
        _result = result;
        if (CreateSimulation(forward: false) is { } simulation)
        {
            Controller.AnimateBackWith(simulation);
        }
        else if (ReverseTransitionDuration == TimeSpan.Zero)
        {
            Controller.Stop();
            Controller.SetValue(0.0);
        }
        else
        {
            Controller.ReverseDuration = ReverseTransitionDuration;
            Controller.Reverse();
        }

        return base.DidPop(result);
    }

    public override void DidPopNext(Route nextRoute)
    {
        UpdateSecondaryAnimation(nextRoute);
        base.DidPopNext(nextRoute);
    }

    public override void DidChangeNext(Route? nextRoute)
    {
        UpdateSecondaryAnimation(nextRoute);
        base.DidChangeNext(nextRoute);
    }

    public override void Dispose()
    {
        _secondaryAnimation.Parent = AlwaysDismissedAnimation;
        base.Dispose();
    }

    private void UpdateSecondaryAnimation(Route? nextRoute)
    {
        _secondaryAnimation.Parent = nextRoute is TransitionRoute transitionRoute
                                     && CanTransitionTo(transitionRoute)
                                     && transitionRoute.CanTransitionFrom(this)
            ? transitionRoute.RawAnimation
            : AlwaysDismissedAnimation;
    }

    private void HandleAnimationChanged()
    {
        NotifyRouteChanged();
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        // The bottom-most entry carries the route's opacity: while the route animates it must let the route
        // below build, and once the transition completed an opaque route hides everything under it.
        SetPrimaryEntryOpaque(status == AnimationStatus.Completed && Opaque);

        // Flutter's `didPush` future resolves on completion and on cancellation alike (`whenCompleteOrCancel`).
        if (status is AnimationStatus.Completed or AnimationStatus.Dismissed)
        {
            NotifyPushSettled();
        }

        NotifyRouteChanged();
        if (_handlingPopGesture && status is AnimationStatus.Completed or AnimationStatus.Dismissed)
        {
            StopPopGestureIfNeeded();
        }

        if (status == AnimationStatus.Dismissed && _isPopped)
        {
            _popFinalized = true;
            Navigator?.FinalizeRoute(this);
        }
    }

    private protected void SetPrimaryEntryOpaque(bool opaque)
    {
        if (OverlayEntries.Count == 0)
        {
            return;
        }

        OverlayEntries[0].Opaque = opaque;
    }

    /// <summary>
    /// Keeps the route below onstage while a hero flight runs across a zero-length transition, where the
    /// controller never reports <see cref="AnimationStatus.Forward"/> or <see cref="AnimationStatus.Reverse"/>.
    /// </summary>
    internal void SuspendEntryOpacityForFlight() => SetPrimaryEntryOpaque(false);

    internal void RestoreEntryOpacityAfterFlight() => SetPrimaryEntryOpaque(EffectiveOpaque);

    protected void StartPopGesture(double progress)
    {
        Controller.SetValueForUserGesture(progress);
        if (_handlingPopGesture)
        {
            return;
        }

        _handlingPopGesture = true;
        Navigator?.StartUserGesture();
        NotifyRouteChanged();
    }

    protected void UpdatePopGesture(double progress)
    {
        if (_handlingPopGesture && IsCurrent)
        {
            Controller.SetValue(progress);
        }
    }

    protected void CancelPopGesture()
    {
        if (_handlingPopGesture)
        {
            Controller.Forward();
        }
    }

    protected void CommitPopGesture()
    {
        if (!_handlingPopGesture || Navigator is null)
        {
            return;
        }

        Navigator.Pop();
        if (Controller.Status.IsAnimating())
        {
            Controller.Reverse(from: 1.0);
        }
    }

    private void StopPopGestureIfNeeded()
    {
        if (!_handlingPopGesture)
        {
            return;
        }

        _handlingPopGesture = false;
        Navigator?.StopUserGesture();
        NotifyRouteChanged();
    }

    private static TimeSpan NormalizeDuration(TimeSpan duration)
    {
        return duration == TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : duration;
    }

    private static void ValidateDuration(TimeSpan duration, string propertyName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{propertyName} cannot be negative.");
        }
    }
}

public abstract class ModalRoute : TransitionRoute
{
    private readonly PageStorageBucket _storageBucket = new();
    private readonly List<PopEntry> _popEntries = [];
    private readonly GlobalKey<ModalScopeState> _scopeKey;
    private readonly GlobalKey _subtreeKey;
    private readonly ProxyAnimation _animationProxy = new(AlwaysDismissedAnimation);
    private readonly ProxyAnimation _secondaryAnimationProxy = new(AlwaysDismissedAnimation);
    private OverlayEntry? _modalBarrier;
    private OverlayEntry? _modalScope;
    private Widget? _modalScopeCache;
    private bool _offstage;

    protected ModalRoute(
        RouteSettings? settings = null,
        ImageFilter? filter = null,
        bool? requestFocus = null) : base(settings, requestFocus)
    {
        // Identity-based keys: a label-based GlobalKey is a record and would compare equal across routes.
        _scopeKey = new GlobalObjectKey<ModalScopeState>(this);
        _subtreeKey = new GlobalObjectKey<State>(this);
        Filter = filter;
    }

    internal PageStorageBucket StorageBucket => _storageBucket;

    internal GlobalKey SubtreeKey => _subtreeKey;

    /// <summary>The filter applied to the modal barrier through a <see cref="BackdropFilter"/>.</summary>
    public ImageFilter? Filter { get; }

    /// <summary>Whether the route keeps its state alive while another route covers it.</summary>
    public virtual bool MaintainState => true;

    /// <summary>
    /// How Tab traversal behaves at the edge of this route's focus scope; <see langword="null"/> falls
    /// back to the navigator default (<see cref="Widgets.TraversalEdgeBehavior.ParentScope"/>).
    /// </summary>
    public TraversalEdgeBehavior? TraversalEdgeBehavior { get; set; }

    /// <summary>
    /// How arrow-key traversal behaves at the edge of this route's focus scope; <see langword="null"/>
    /// falls back to the navigator default (<see cref="Widgets.TraversalEdgeBehavior.Stop"/>).
    /// </summary>
    public TraversalEdgeBehavior? DirectionalTraversalEdgeBehavior { get; set; }

    /// <summary>Flutter's <c>fullscreenDialog</c> flag; page routes override it from their constructor.</summary>
    public virtual bool FullscreenDialog => false;

    public override Animation<double> Animation => HasController
        ? _animationProxy
        : throw new InvalidOperationException("The route animation is unavailable before the route is installed.");

    public override Animation<double> SecondaryAnimation => _secondaryAnimationProxy;

    /// <summary>
    /// Whether the route is currently hidden. An offstage route still builds and lays out, but it is not
    /// painted and its animations report their end values. Mirrors Flutter's <c>ModalRoute.offstage</c>.
    /// </summary>
    public bool Offstage
    {
        get => _offstage;
        set
        {
            if (_offstage == value)
            {
                return;
            }

            SetState(() => _offstage = value);
            _animationProxy.Parent = _offstage ? AlwaysCompleteAnimation : RawAnimation;
            _secondaryAnimationProxy.Parent = _offstage ? AlwaysDismissedAnimation : RawSecondaryAnimation;
            ChangedInternalState();
        }
    }

    /// <summary>Whether the route can be popped, mirroring Flutter's <c>ModalRoute.canPop</c>.</summary>
    public bool CanPop => HasActiveRouteBelow || WillHandlePopInternally;

    protected override void Install()
    {
        base.Install();
        _animationProxy.Parent = _offstage ? AlwaysCompleteAnimation : RawAnimation;
        _secondaryAnimationProxy.Parent = _offstage ? AlwaysDismissedAnimation : RawSecondaryAnimation;
    }

    protected override IEnumerable<OverlayEntry> CreateOverlayEntries()
    {
        return
        [
            _modalBarrier = new OverlayEntry(BuildModalBarrierEntry),
            _modalScope = new OverlayEntry(
                BuildModalScopeEntry,
                maintainState: MaintainState,
                canSizeOverlay: Opaque),
        ];
    }

    /// <summary>
    /// Runs <paramref name="mutation"/> and rebuilds the modal scope, mirroring <c>ModalRoute.setState</c>.
    /// </summary>
    protected void SetState(Action mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ModalScopeState? scopeState = _scopeKey.CurrentState;
        if (scopeState is not null)
        {
            scopeState.RouteSetState(mutation);
            return;
        }

        mutation();
    }

    public override void DidPush()
    {
        HandOverFocusToScope();
        base.DidPush();
    }

    public override void DidAdd()
    {
        HandOverFocusToScope();
        base.DidAdd();
    }

    /// <summary>
    /// Flutter's focus hand-over in <c>ModalRoute.didPush</c>/<c>didAdd</c>: the navigator's enclosing scope
    /// makes this route's scope its first focus, so focus lands inside the route rather than on the navigator.
    /// </summary>
    private void HandOverFocusToScope()
    {
        ModalScopeState? scopeState = _scopeKey.CurrentState;
        if (scopeState is null || Navigator?.NavigatorWidget.RequestFocus != true)
        {
            return;
        }

        Navigator.FocusNode.Scope?.SetFirstFocus(scopeState.FocusScopeNode);
    }

    protected internal override void ChangedInternalState()
    {
        base.ChangedInternalState();
        SetState(static () => { });
        _modalBarrier?.MarkNeedsBuild();
        if (_modalScope is not null && _modalScope.Owner is not null)
        {
            _modalScope.MaintainState = MaintainState;
        }
    }

    protected internal override void ChangedExternalState()
    {
        base.ChangedExternalState();
        _modalBarrier?.MarkNeedsBuild();
        _scopeKey.CurrentState?.ForceRebuildPage();
    }

    public override void DidChangePrevious(Route? previousRoute)
    {
        base.DidChangePrevious(previousRoute);
        ChangedInternalState();
    }

    /// <summary>Whether tapping the modal barrier dismisses the route.</summary>
    public virtual bool BarrierDismissible => false;

    /// <summary>Whether the modal barrier can be dismissed through the accessibility layer.</summary>
    public virtual bool SemanticsDismissible => true;

    /// <summary>The color painted by the modal barrier; <see langword="null"/> keeps the barrier invisible.</summary>
    public virtual Color? BarrierColor => null;

    /// <summary>The semantics label announced for the modal barrier.</summary>
    public virtual string? BarrierLabel => null;

    /// <summary>The curve the barrier color follows while the route animates in.</summary>
    public virtual Curve BarrierCurve => Curves.Ease;

    public virtual DelegatedTransitionBuilder? DelegatedTransition => null;

    public DelegatedTransitionBuilder? ReceivedTransition { get; internal set; }

    public override RoutePopDisposition PopDisposition => _popEntries.Any(entry => !entry.CanPopNotifier.Value)
        ? RoutePopDisposition.DoNotPop
        : base.PopDisposition;

    public override void OnPopInvokedWithResult(bool didPop, object? result)
    {
        foreach (var popEntry in _popEntries.ToArray())
        {
            popEntry.OnPopInvokedWithResult(didPop, result);
        }

        base.OnPopInvokedWithResult(didPop, result);
    }

    public void RegisterPopEntry(PopEntry popEntry)
    {
        if (popEntry == null)
        {
            throw new ArgumentNullException(nameof(popEntry));
        }

        if (_popEntries.Contains(popEntry))
        {
            return;
        }

        _popEntries.Add(popEntry);
        popEntry.CanPopNotifier.AddListener(HandlePopEntryChanged);
        HandlePopEntryChanged();
    }

    public void UnregisterPopEntry(PopEntry popEntry)
    {
        if (!_popEntries.Remove(popEntry))
        {
            return;
        }

        popEntry.CanPopNotifier.RemoveListener(HandlePopEntryChanged);
        HandlePopEntryChanged();
    }

    public override void Dispose()
    {
        foreach (var popEntry in _popEntries.ToArray())
        {
            popEntry.CanPopNotifier.RemoveListener(HandlePopEntryChanged);
        }

        _popEntries.Clear();
        base.Dispose();

        // Cleared after `OverlayRoute.Dispose` disposed them, so a late `ChangedInternalState` cannot mark a
        // disposed entry as needing build.
        _modalBarrier = null;
        _modalScope = null;
        _modalScopeCache = null;
    }

    public override void DidChangeNext(Route? nextRoute)
    {
        UpdateReceivedTransition(nextRoute);
        base.DidChangeNext(nextRoute);
        ChangedInternalState();
    }

    public override void DidPopNext(Route nextRoute)
    {
        UpdateReceivedTransition(nextRoute);
        base.DidPopNext(nextRoute);
        ChangedInternalState();
    }

    /// <summary>
    /// Builds the barrier painted below the route's page. Subclasses override this to create their own barrier.
    /// </summary>
    public virtual Widget BuildModalBarrier()
    {
        if (!Offstage && BarrierColor is { A: not 0 } barrierColor)
        {
            return new AnimatedModalBarrier(
                color: CreateBarrierColorAnimation(barrierColor),
                dismissible: BarrierDismissible,
                semanticsLabel: BarrierLabel,
                barrierSemanticsDismissible: SemanticsDismissible);
        }

        return new ModalBarrier(
            dismissible: BarrierDismissible,
            semanticsLabel: BarrierLabel,
            barrierSemanticsDismissible: SemanticsDismissible);
    }

    /// <summary>
    /// Drives <paramref name="barrierColor"/> from fully transparent to opaque through <see cref="BarrierCurve"/>,
    /// mirroring Flutter's <c>animation.drive(ColorTween(...).chain(CurveTween(curve: barrierCurve)))</c>.
    /// </summary>
    protected Animation<Color?> CreateBarrierColorAnimation(Color barrierColor) =>
        new BarrierColorAnimation(Animation, barrierColor, BarrierCurve);

    /// <summary>The barrier overlay entry: the barrier plus the filter, pointer and semantics wrappers.</summary>
    internal Widget BuildModalBarrierEntry(BuildContext context)
    {
        Widget barrier = BuildModalBarrier();
        if (Filter is not null)
        {
            barrier = new BackdropFilter(filter: Filter, child: barrier);
        }

        barrier = new IgnorePointer(
            ignoring: Animation.Status is not (AnimationStatus.Forward or AnimationStatus.Completed),
            child: barrier);
        return SemanticsDismissible && BarrierDismissible
            ? new Semantics(sortKey: new OrdinalSortKey(1.0), child: barrier)
            : barrier;
    }

    /// <summary>The page overlay entry: Flutter's cached <c>_buildModalScope</c>.</summary>
    internal Widget BuildModalScopeEntry(BuildContext context)
    {
        return _modalScopeCache ??= new Semantics(
            sortKey: new OrdinalSortKey(0.0),
            child: new ModalScope(route: this, key: _scopeKey));
    }

    internal Widget BuildFlexibleTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        if (ReceivedTransition is null || secondaryAnimation.Status == AnimationStatus.Dismissed)
        {
            return BuildTransitions(context, animation, secondaryAnimation, child);
        }

        var proxyAnimation = new ProxyAnimation();
        Widget originalTransitions = BuildTransitions(context, animation, proxyAnimation, child);
        return ReceivedTransition(
                   context,
                   animation,
                   secondaryAnimation,
                   AllowSnapshotting,
                   originalTransitions)
               ?? originalTransitions;
    }

    private void HandlePopEntryChanged()
    {
        NotifyRouteChanged();
    }

    private void UpdateReceivedTransition(Route? nextRoute)
    {
        ReceivedTransition = nextRoute is ModalRoute modalRoute
                             && CanTransitionTo(modalRoute)
                             && modalRoute.DelegatedTransition != DelegatedTransition
            ? modalRoute.DelegatedTransition
            : null;
    }

    public static ModalRoute Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("ModalRoute not found in context.");
    }

    /// <summary>
    /// Flutter's private <c>ModalRoute._of</c>: resolves the enclosing modal route, registering a dependency
    /// on <paramref name="aspect"/> only (a <see langword="null"/> aspect depends on the whole status).
    /// </summary>
    private static ModalRoute? ResolveOf(BuildContext context, ModalRouteAspect? aspect)
    {
        RouteScope? scope = aspect is null
            ? context.DependOnInherited<RouteScope>()
            : InheritedModel<ModalRouteAspect>.InheritFrom<RouteScope>(context, aspect.Value);
        return scope?.Route as ModalRoute;
    }

    public static ModalRoute? MaybeOf(BuildContext context)
    {
        return ResolveOf(context, aspect: null);
    }

    public static bool? IsCurrentOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.IsCurrent)?.IsCurrent;
    }

    public static bool? CanPopOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.CanPop)?.CanPop;
    }

    public static RouteSettings? SettingsOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.Settings)?.Settings;
    }

    public static bool? IsActiveOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.IsActive)?.IsActive;
    }

    public static bool? IsFirstOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.IsFirst)?.IsFirst;
    }

    public static bool? OpaqueOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.Opaque)?.Opaque;
    }

    public static RoutePopDisposition? PopDispositionOf(BuildContext context)
    {
        return ResolveOf(context, ModalRouteAspect.PopDisposition)?.PopDisposition;
    }

    private sealed class BarrierColorAnimation : Animation<Color?>
    {
        private readonly Animation<double> _parent;
        private readonly Color _color;
        private readonly Curve _curve;

        public BarrierColorAnimation(Animation<double> parent, Color color, Curve curve)
        {
            _parent = parent;
            _color = color;
            _curve = curve;
        }

        public override Color? Value => Color.FromArgb(
            (byte)Math.Round(_color.A * _curve(Math.Clamp(_parent.Value, 0.0, 1.0))),
            _color.R,
            _color.G,
            _color.B);

        public override AnimationStatus Status => _parent.Status;

        public override void AddListener(Action listener) => _parent.AddListener(listener);

        public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) =>
            _parent.AddStatusListener(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener) =>
            _parent.RemoveStatusListener(listener);
    }
}

public abstract class PageRoute : ModalRoute
{
    protected PageRoute(
        RouteSettings? settings = null,
        bool fullscreenDialog = false,
        bool maintainState = true,
        ImageFilter? filter = null,
        bool? requestFocus = null) : base(settings, filter, requestFocus)
    {
        FullscreenDialog = fullscreenDialog;
        MaintainState = maintainState;
    }

    public override bool FullscreenDialog { get; }

    public override bool MaintainState { get; }

    public override bool PopGestureEnabled => !IsFirst
                                              && !WillHandlePopInternally
                                              && PopDisposition != RoutePopDisposition.DoNotPop
                                              && Animation.Status == AnimationStatus.Completed;

    public override bool PopGestureInProgress => Navigator?.UserGestureInProgress == true;

    public override TimeSpan TransitionDuration => TimeSpan.Zero;

    public void HandleStartBackGesture(double progress = 0.0)
    {
        StartPopGesture(progress);
    }

    public void HandleUpdateBackGestureProgress(double progress)
    {
        UpdatePopGesture(progress);
    }

    public void HandleCancelBackGesture()
    {
        CancelPopGesture();
    }

    public void HandleCommitBackGesture()
    {
        CommitPopGesture();
    }

    internal void HandleSettleBackGesture(bool animateForward, TimeSpan duration, Curve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        if (!PopGestureInProgress)
        {
            return;
        }

        if (animateForward)
        {
            _ = Controller.AnimateTo(1.0, duration, curve);
            return;
        }

        if (IsCurrent)
        {
            Navigator!.Pop();
        }

        _ = Controller.AnimateBack(0.0, duration, curve);
    }
}

/// <summary>
/// A modal route that overlays a widget over the current route, leaving the route below visible.
/// </summary>
public abstract class PopupRoute : ModalRoute
{
    protected PopupRoute(
        RouteSettings? settings = null,
        ImageFilter? filter = null,
        bool? requestFocus = null) : base(settings, filter, requestFocus)
    {
    }

    public override bool Opaque => false;

    public override bool MaintainState => true;

    public override bool AllowSnapshotting => false;
}

public sealed class BuilderPageRoute : PageRoute
{
    private readonly Func<BuildContext, Widget> _builder;

    public BuilderPageRoute(
        Func<BuildContext, Widget> builder,
        RouteSettings? settings = null,
        bool fullscreenDialog = false,
        bool? requestFocus = null) : base(settings, fullscreenDialog, requestFocus: requestFocus)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public override Widget BuildPage(BuildContext context)
    {
        return _builder(context);
    }
}

public sealed class PageRouteBuilder : PageRoute
{
    public PageRouteBuilder(
        RoutePageBuilder pageBuilder,
        RouteTransitionsBuilder? transitionsBuilder = null,
        TimeSpan? transitionDuration = null,
        TimeSpan? reverseTransitionDuration = null,
        bool opaque = true,
        bool fullscreenDialog = false,
        bool allowSnapshotting = true,
        bool maintainState = true,
        RouteSettings? settings = null,
        bool? requestFocus = null) : base(settings, fullscreenDialog, maintainState, requestFocus: requestFocus)
    {
        TimeSpan effectiveTransitionDuration = transitionDuration ?? TimeSpan.FromMilliseconds(300);
        TimeSpan effectiveReverseTransitionDuration =
            reverseTransitionDuration ?? TimeSpan.FromMilliseconds(300);
        if (effectiveTransitionDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionDuration));
        }
        if (effectiveReverseTransitionDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(reverseTransitionDuration));
        }

        PageBuilder = pageBuilder ?? throw new ArgumentNullException(nameof(pageBuilder));
        TransitionsBuilder = transitionsBuilder ?? DefaultTransitionsBuilder;
        TransitionDuration = effectiveTransitionDuration;
        ReverseTransitionDuration = effectiveReverseTransitionDuration;
        Opaque = opaque;
        AllowSnapshotting = allowSnapshotting;
    }

    public RoutePageBuilder PageBuilder { get; }

    public RouteTransitionsBuilder TransitionsBuilder { get; }

    public override TimeSpan TransitionDuration { get; }

    public override TimeSpan ReverseTransitionDuration { get; }

    public override bool Opaque { get; }

    public override bool AllowSnapshotting { get; }

    public override Widget BuildPage(BuildContext context)
    {
        return PageBuilder(context, Animation, SecondaryAnimation);
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return TransitionsBuilder(context, animation, secondaryAnimation, child);
    }

    private static Widget DefaultTransitionsBuilder(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return child;
    }
}

public abstract class NavigatorObserver
{
    public NavigatorState? Navigator { get; internal set; }

    public virtual void DidPush(Route route, Route? previousRoute)
    {
    }

    public virtual void DidPop(Route route, Route? previousRoute)
    {
    }

    public virtual void DidRemove(Route route, Route? previousRoute)
    {
    }

    public virtual void DidReplace(Route newRoute, Route? oldRoute)
    {
    }

    /// <summary>Called when the top-most route of the navigator changed.</summary>
    public virtual void DidChangeTop(Route topRoute, Route? previousTopRoute)
    {
    }

    public virtual void DidStartUserGesture(Route route, Route? previousRoute)
    {
    }

    public virtual void DidStopUserGesture()
    {
    }
}

public interface RouteAware
{
    void DidPush();

    void DidPop();

    void DidPopNext();

    void DidPushNext();
}

public class RouteObserver<TRoute> : NavigatorObserver where TRoute : Route
{
    private readonly Dictionary<TRoute, HashSet<RouteAware>> _listeners = [];

    public void Subscribe(RouteAware routeAware, TRoute route)
    {
        if (routeAware == null)
        {
            throw new ArgumentNullException(nameof(routeAware));
        }

        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (!_listeners.TryGetValue(route, out var subscribers))
        {
            subscribers = [];
            _listeners[route] = subscribers;
        }

        if (subscribers.Add(routeAware))
        {
            routeAware.DidPush();
        }
    }

    public void Unsubscribe(RouteAware routeAware)
    {
        if (routeAware == null)
        {
            throw new ArgumentNullException(nameof(routeAware));
        }

        foreach (var entry in _listeners.ToArray())
        {
            var subscribers = entry.Value;
            subscribers.Remove(routeAware);
            if (subscribers.Count == 0)
            {
                _listeners.Remove(entry.Key);
            }
        }
    }

    public override void DidPush(Route route, Route? previousRoute)
    {
        if (previousRoute is not TRoute previousTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(previousTypedRoute, out var subscribers))
        {
            return;
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.DidPushNext();
        }
    }

    public override void DidPop(Route route, Route? previousRoute)
    {
        if (previousRoute is TRoute previousTypedRoute
            && _listeners.TryGetValue(previousTypedRoute, out var previousSubscribers))
        {
            foreach (var subscriber in previousSubscribers.ToArray())
            {
                subscriber.DidPopNext();
            }
        }

        if (route is not TRoute typedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(typedRoute, out var subscribers))
        {
            return;
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.DidPop();
        }
    }

    public override void DidReplace(Route newRoute, Route? oldRoute)
    {
        if (oldRoute is not TRoute oldTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(oldTypedRoute, out var oldSubscribers))
        {
            return;
        }

        _listeners.Remove(oldTypedRoute);

        if (newRoute is not TRoute newTypedRoute)
        {
            return;
        }

        if (!_listeners.TryGetValue(newTypedRoute, out var newSubscribers))
        {
            _listeners[newTypedRoute] = oldSubscribers;
            return;
        }

        foreach (var subscriber in oldSubscribers)
        {
            newSubscribers.Add(subscriber);
        }
    }
}

public sealed class Navigator : StatefulWidget
{
    /// <summary>Flutter's <c>kDefaultRouteTraversalEdgeBehavior</c>.</summary>
    public const TraversalEdgeBehavior DefaultRouteTraversalEdgeBehavior = TraversalEdgeBehavior.ParentScope;

    /// <summary>Flutter's <c>kDefaultRouteDirectionalTraversalEdgeBehavior</c>.</summary>
    public const TraversalEdgeBehavior DefaultRouteDirectionalTraversalEdgeBehavior = TraversalEdgeBehavior.Stop;

    /// <summary>Flutter's <c>Navigator.defaultRouteName</c>.</summary>
    public const string DefaultRouteName = "/";

    public Navigator(
        Route initialRoute,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        TransitionDelegate? transitionDelegate = null,
        bool requestFocus = true,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationScopeId = null,
        TraversalEdgeBehavior routeTraversalEdgeBehavior = DefaultRouteTraversalEdgeBehavior,
        TraversalEdgeBehavior routeDirectionalTraversalEdgeBehavior = DefaultRouteDirectionalTraversalEdgeBehavior,
        bool reportsRouteUpdateToEngine = false) : base(key)
    {
        InitialRoute = initialRoute ?? throw new ArgumentNullException(nameof(initialRoute));
        Observers = observers ?? [];
        ApplyOptions(
            transitionDelegate,
            requestFocus,
            clipBehavior,
            restorationScopeId,
            routeTraversalEdgeBehavior,
            routeDirectionalTraversalEdgeBehavior,
            reportsRouteUpdateToEngine);
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        RouteData initialRouteData,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        RouteFactory? onUnknownRoute = null,
        TransitionDelegate? transitionDelegate = null,
        bool requestFocus = true,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationScopeId = null,
        TraversalEdgeBehavior routeTraversalEdgeBehavior = DefaultRouteTraversalEdgeBehavior,
        TraversalEdgeBehavior routeDirectionalTraversalEdgeBehavior = DefaultRouteDirectionalTraversalEdgeBehavior,
        bool reportsRouteUpdateToEngine = false) : base(key)
    {
        OnGenerateRoute = onGenerateRoute ?? throw new ArgumentNullException(nameof(onGenerateRoute));
        InitialRouteData = initialRouteData ?? throw new ArgumentNullException(nameof(initialRouteData));
        Observers = observers ?? [];
        OnUnknownRoute = onUnknownRoute;
        ApplyOptions(
            transitionDelegate,
            requestFocus,
            clipBehavior,
            restorationScopeId,
            routeTraversalEdgeBehavior,
            routeDirectionalTraversalEdgeBehavior,
            reportsRouteUpdateToEngine);
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        string initialRouteName = DefaultRouteName,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        NavigatorInitialRouteListFactory? onGenerateInitialRoutes = null,
        RouteFactory? onUnknownRoute = null,
        TransitionDelegate? transitionDelegate = null,
        bool requestFocus = true,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationScopeId = null,
        TraversalEdgeBehavior routeTraversalEdgeBehavior = DefaultRouteTraversalEdgeBehavior,
        TraversalEdgeBehavior routeDirectionalTraversalEdgeBehavior = DefaultRouteDirectionalTraversalEdgeBehavior,
        bool reportsRouteUpdateToEngine = false) : base(key)
    {
        if (string.IsNullOrWhiteSpace(initialRouteName))
        {
            throw new ArgumentException("initialRouteName cannot be null or whitespace.", nameof(initialRouteName));
        }

        OnGenerateRoute = onGenerateRoute ?? throw new ArgumentNullException(nameof(onGenerateRoute));
        InitialRouteName = initialRouteName;
        Observers = observers ?? [];
        OnGenerateInitialRoutes = onGenerateInitialRoutes;
        OnUnknownRoute = onUnknownRoute;
        ApplyOptions(
            transitionDelegate,
            requestFocus,
            clipBehavior,
            restorationScopeId,
            routeTraversalEdgeBehavior,
            routeDirectionalTraversalEdgeBehavior,
            reportsRouteUpdateToEngine);
    }

    /// <summary>
    /// Flutter's declarative <c>Navigator.pages</c> constructor: the page list owns the history, and
    /// <paramref name="onDidRemovePage"/> tells the owner to drop a page whose route left the navigator.
    /// </summary>
    public Navigator(
        IReadOnlyList<Page> pages,
        DidRemovePageCallback onDidRemovePage,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        RouteFactory? onGenerateRoute = null,
        RouteFactory? onUnknownRoute = null,
        TransitionDelegate? transitionDelegate = null,
        bool requestFocus = true,
        Clip clipBehavior = Clip.HardEdge,
        string? restorationScopeId = null,
        TraversalEdgeBehavior routeTraversalEdgeBehavior = DefaultRouteTraversalEdgeBehavior,
        TraversalEdgeBehavior routeDirectionalTraversalEdgeBehavior = DefaultRouteDirectionalTraversalEdgeBehavior,
        bool reportsRouteUpdateToEngine = false) : base(key)
    {
        Pages = pages ?? throw new ArgumentNullException(nameof(pages));
        OnDidRemovePage = onDidRemovePage ?? throw new ArgumentNullException(nameof(onDidRemovePage));
        Observers = observers ?? [];
        OnGenerateRoute = onGenerateRoute;
        OnUnknownRoute = onUnknownRoute;
        ApplyOptions(
            transitionDelegate,
            requestFocus,
            clipBehavior,
            restorationScopeId,
            routeTraversalEdgeBehavior,
            routeDirectionalTraversalEdgeBehavior,
            reportsRouteUpdateToEngine);
    }

    public Route? InitialRoute { get; }

    public string? InitialRouteName { get; }

    public RouteData? InitialRouteData { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public NavigatorInitialRouteListFactory? OnGenerateInitialRoutes { get; }

    public RouteFactory? OnUnknownRoute { get; }

    public IReadOnlyList<NavigatorObserver> Observers { get; }

    /// <summary>The declarative page list, or <see langword="null"/> when the imperative API is used.</summary>
    public IReadOnlyList<Page>? Pages { get; }

    /// <summary>Called when a page-based route left the navigator and its page must be dropped.</summary>
    public DidRemovePageCallback? OnDidRemovePage { get; }

    /// <summary>Decides how routes enter and leave when <see cref="Pages"/> changes.</summary>
    public TransitionDelegate TransitionDelegate { get; private set; } = new DefaultTransitionDelegate();

    /// <summary>Whether pushed routes take focus. Defaults to <see langword="true"/>.</summary>
    public bool RequestFocus { get; private set; } = true;

    /// <summary>The clip applied by the navigator's overlay. Defaults to <see cref="Clip.HardEdge"/>.</summary>
    public Clip ClipBehavior { get; private set; } = Clip.HardEdge;

    /// <summary>The restoration id this navigator stores its history under.</summary>
    public string? RestorationScopeId { get; private set; }

    /// <summary>How Tab traversal behaves at a route's focus-scope edge.</summary>
    public TraversalEdgeBehavior RouteTraversalEdgeBehavior { get; private set; } =
        DefaultRouteTraversalEdgeBehavior;

    /// <summary>How arrow-key traversal behaves at a route's focus-scope edge.</summary>
    public TraversalEdgeBehavior RouteDirectionalTraversalEdgeBehavior { get; private set; } =
        DefaultRouteDirectionalTraversalEdgeBehavior;

    /// <summary>Whether the top route's name is reported to the host as the current location.</summary>
    public bool ReportsRouteUpdateToEngine { get; private set; }

    /// <summary>Whether this navigator's history is owned by <see cref="Pages"/>.</summary>
    internal bool UsingPagesApi => Pages is not null;

    public override State CreateState()
    {
        return new NavigatorState();
    }

    private void ApplyOptions(
        TransitionDelegate? transitionDelegate,
        bool requestFocus,
        Clip clipBehavior,
        string? restorationScopeId,
        TraversalEdgeBehavior routeTraversalEdgeBehavior,
        TraversalEdgeBehavior routeDirectionalTraversalEdgeBehavior,
        bool reportsRouteUpdateToEngine)
    {
        TransitionDelegate = transitionDelegate ?? TransitionDelegate;
        RequestFocus = requestFocus;
        ClipBehavior = clipBehavior;
        RestorationScopeId = restorationScopeId;
        RouteTraversalEdgeBehavior = routeTraversalEdgeBehavior;
        RouteDirectionalTraversalEdgeBehavior = routeDirectionalTraversalEdgeBehavior;
        ReportsRouteUpdateToEngine = reportsRouteUpdateToEngine;
    }

    public static NavigatorState Of(BuildContext context, bool rootNavigator = false)
    {
        return MaybeOf(context, rootNavigator)
               ?? throw new InvalidOperationException("Navigator not found in context.");
    }

    public static NavigatorState? MaybeOf(BuildContext context, bool rootNavigator = false)
    {
        if (!rootNavigator)
        {
            return context.DependOnInherited<NavigatorScope>()?.Navigator;
        }

        NavigatorState? result = null;
        for (var ancestor = context.Owner; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor.Widget is NavigatorScope scope) result = scope.Navigator;
        }

        return result;
    }

    public static bool CanPop(BuildContext context)
    {
        return MaybeOf(context)?.CanPop ?? false;
    }

    public static bool MaybePop(BuildContext context, object? result = null)
    {
        var navigator = MaybeOf(context);
        if (navigator == null)
        {
            return false;
        }

        return navigator.MaybePop(result);
    }

    public static void Pop(BuildContext context, object? result = null)
    {
        Of(context).Pop(result);
    }

    public static void PopUntil(BuildContext context, RoutePredicate predicate)
    {
        Of(context).PopUntil(predicate);
    }

    public static void Push(BuildContext context, Route route)
    {
        Of(context).Push(route);
    }

    public static void PushAndRemoveUntil(BuildContext context, Route newRoute, RoutePredicate predicate)
    {
        Of(context).PushAndRemoveUntil(newRoute, predicate);
    }

    public static void PushNamed(BuildContext context, string routeName, object? arguments = null)
    {
        Of(context).PushNamed(routeName, arguments);
    }

    public static void PushNamed(BuildContext context, RouteData routeData)
    {
        Of(context).PushNamed(routeData);
    }

    public static void PushNamedAndRemoveUntil(
        BuildContext context,
        string routeName,
        RoutePredicate predicate,
        object? arguments = null)
    {
        Of(context).PushNamedAndRemoveUntil(routeName, predicate, arguments);
    }

    public static void PushNamedAndRemoveUntil(BuildContext context, RouteData routeData, RoutePredicate predicate)
    {
        Of(context).PushNamedAndRemoveUntil(routeData, predicate);
    }

    public static void PushReplacement(BuildContext context, Route newRoute, object? result = null)
    {
        Of(context).PushReplacement(newRoute, result);
    }

    public static void PushReplacementNamed(
        BuildContext context,
        string routeName,
        object? arguments = null,
        object? result = null)
    {
        Of(context).PushReplacementNamed(routeName, arguments, result);
    }

    public static void PushReplacementNamed(BuildContext context, RouteData routeData, object? result = null)
    {
        Of(context).PushReplacementNamed(routeData, result);
    }

    public static void RemoveRoute(BuildContext context, Route route, object? result = null)
    {
        Of(context).RemoveRoute(route, result);
    }

    public static void RemoveRouteBelow(BuildContext context, Route anchorRoute, object? result = null)
    {
        Of(context).RemoveRouteBelow(anchorRoute, result);
    }

    public static void StartUserGesture(BuildContext context)
    {
        Of(context).StartUserGesture();
    }

    public static void StopUserGesture(BuildContext context)
    {
        Of(context).StopUserGesture();
    }

    public static bool MaybePopFromUserGesture(BuildContext context, object? result = null)
    {
        return Of(context).MaybePopFromUserGesture(result);
    }

    /// <summary>
    /// Flutter routes the host back button through <c>WidgetsBinding.handlePopRoute</c>; Plumix additionally
    /// keeps the navigator handler stack, so binding observers (where <see cref="RootBackButtonDispatcher"/>
    /// registers) get the first refusal and the innermost navigator handles the rest.
    /// </summary>
    public static bool TryHandleBackButton()
    {
        return WidgetsBinding.Instance.HandlePopRoute() || NavigatorBackButtonDispatcher.DispatchBackButton();
    }
}

internal sealed class NavigatorScope : InheritedWidget
{
    public NavigatorScope(
        NavigatorState navigator,
        Widget child,
        Key? key = null) : base(key)
    {
        Navigator = navigator;
        Child = child;
    }

    public NavigatorState Navigator { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((NavigatorScope)oldWidget).Navigator, Navigator);
    }
}

/// <summary>Flutter's <c>_ModalScopeStatus</c>: the route status the page subtree depends on.</summary>
internal sealed class RouteScope : InheritedModel<ModalRouteAspect>
{
    public RouteScope(
        Route route,
        bool isCurrent,
        bool canPop,
        bool impliesAppBarDismissal,
        bool opaque,
        Widget child,
        Key? key = null) : base(key)
    {
        Route = route;
        IsCurrent = isCurrent;
        CanPop = canPop;
        ImpliesAppBarDismissal = impliesAppBarDismissal;
        Opaque = opaque;
        Child = child;
    }

    public Route Route { get; }

    public bool IsCurrent { get; }

    public bool CanPop { get; }

    public bool ImpliesAppBarDismissal { get; }

    public bool Opaque { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (RouteScope)oldWidget;
        return !ReferenceEquals(oldScope.Route, Route)
               || oldScope.IsCurrent != IsCurrent
               || oldScope.CanPop != CanPop
               || oldScope.ImpliesAppBarDismissal != ImpliesAppBarDismissal
               || oldScope.Opaque != Opaque;
    }

    protected override bool UpdateShouldNotifyDependent(
        InheritedModel<ModalRouteAspect> oldWidget,
        IReadOnlySet<ModalRouteAspect> dependencies)
    {
        var oldScope = (RouteScope)oldWidget;
        return dependencies.Any(dependency => dependency switch
        {
            ModalRouteAspect.IsCurrent => IsCurrent != oldScope.IsCurrent,
            ModalRouteAspect.CanPop => CanPop != oldScope.CanPop,
            ModalRouteAspect.Settings => !Equals(Route.Settings, oldScope.Route.Settings),
            ModalRouteAspect.IsActive => Route.IsActive != oldScope.Route.IsActive,
            ModalRouteAspect.IsFirst => Route.IsFirst != oldScope.Route.IsFirst,
            ModalRouteAspect.Opaque => Opaque != oldScope.Opaque,
            ModalRouteAspect.PopDisposition => Route.PopDisposition != oldScope.Route.PopDisposition,
            _ => false,
        });
    }
}

/// <summary>Flutter's <c>_ModalScope</c>: the page half of a <see cref="ModalRoute"/>'s overlay entries.</summary>
internal sealed class ModalScope : StatefulWidget
{
    public ModalScope(ModalRoute route, Key? key = null) : base(key)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
    }

    public ModalRoute Route { get; }

    public override State CreateState() => new ModalScopeState();
}

internal sealed class ModalScopeState : State
{
    private readonly ScrollController _primaryScrollController = new();
    private Animation<double> _animation = new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);
    private Animation<double> _secondaryAnimation = new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);
    private IListenable _listenable = Listenable.Merge();
    private Widget? _page;

    /// <summary>The scope the route hands focus to; owned by this state, as it is in Flutter.</summary>
    internal FocusScopeNode FocusScopeNode { get; } = new();

    private ModalScope CurrentWidget => (ModalScope)StateWidget;

    private bool ShouldIgnoreFocusRequest =>
        _animation.Status == AnimationStatus.Reverse
        || CurrentWidget.Route.Navigator?.UserGestureInProgress == true;

    private bool ShouldRequestFocus => CurrentWidget.Route.RequestFocus;

    public override void InitState()
    {
        base.InitState();
        ModalRoute route = CurrentWidget.Route;
        // The proxies outlive the route's controller, so they are captured once: an entry can still be
        // mounted for a frame after the route was uninstalled.
        _animation = route.Animation;
        _secondaryAnimation = route.SecondaryAnimation;
        _listenable = Listenable.Merge(_animation, _secondaryAnimation);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        UpdateFocusScopeNode();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _page = null;
        UpdateFocusScopeNode();
    }

    public override void Dispose()
    {
        FocusScopeNode.Dispose();
        _primaryScrollController.Dispose();
        base.Dispose();
    }

    internal void ForceRebuildPage()
    {
        SetState(() => _page = null);
    }

    /// <summary>
    /// Flutter's <c>_ModalScopeState._routeSetState</c>: wraps every change to the route's
    /// <c>isCurrent</c>/<c>canPop</c>/<c>offstage</c> so a newly current route takes focus first.
    /// </summary>
    internal void RouteSetState(Action mutation)
    {
        ModalRoute route = CurrentWidget.Route;
        if (route.IsCurrent && !ShouldIgnoreFocusRequest && ShouldRequestFocus)
        {
            route.Navigator?.FocusNode.Scope?.SetFirstFocus(FocusScopeNode);
        }

        SetState(mutation);
    }

    /// <summary>
    /// Flutter's <c>_ModalScopeState._updateFocusScopeNode</c>: applies the route's traversal edge
    /// behaviors (falling back to the navigator defaults) and hands focus to a current route's scope.
    /// </summary>
    private void UpdateFocusScopeNode()
    {
        ModalRoute route = CurrentWidget.Route;
        NavigatorState? navigator = route.Navigator;
        FocusScopeNode.TraversalEdgeBehavior =
            route.TraversalEdgeBehavior
            ?? navigator?.NavigatorWidget.RouteTraversalEdgeBehavior
            ?? Navigator.DefaultRouteTraversalEdgeBehavior;
        FocusScopeNode.DirectionalTraversalEdgeBehavior =
            route.DirectionalTraversalEdgeBehavior
            ?? navigator?.NavigatorWidget.RouteDirectionalTraversalEdgeBehavior
            ?? Navigator.DefaultRouteDirectionalTraversalEdgeBehavior;
        if (route.IsCurrent && ShouldRequestFocus)
        {
            navigator?.FocusNode.Scope?.SetFirstFocus(FocusScopeNode);
        }
    }

    public override Widget Build(BuildContext context)
    {
        ModalRoute route = CurrentWidget.Route;

        // Only the top-most route participates in focus traversal.
        FocusScopeNode.SkipTraversal = !route.IsCurrent;

        return new ListenableBuilder(
            listenable: route.RestorationScopeId,
            builder: (_, child) => new RestorationScope(
                restorationId: route.RestorationScopeId.Value,
                child: child!),
            child: new RouteScope(
                route: route,
                isCurrent: route.IsCurrent,
                canPop: route.CanPop,
                impliesAppBarDismissal: route.ImpliesAppBarDismissal,
                opaque: route.Opaque,
                child: new Offstage(
                    offstage: route.Offstage,
                    child: new PageStorage(
                        route.StorageBucket,
                        new Builder(BuildScopeBody)))));
    }

    private Widget BuildScopeBody(BuildContext context)
    {
        ModalRoute route = CurrentWidget.Route;
        var actions = new Dictionary<Type, FlutterAction>
        {
            [typeof(DismissIntent)] = new DismissModalAction(context),
        };

        return new Actions(
            actions: actions,
            child: new PrimaryScrollController(
                controller: _primaryScrollController,
                child: new HeroControllerScope(
                    controller: route.Navigator!.HeroTransitionController,
                    route: route,
                    child: FocusScope.WithExternalFocusNode(
                        focusScopeNode: FocusScopeNode,
                        child: new RepaintBoundary(
                            child: new ListenableBuilder(
                                listenable: _listenable,
                                builder: BuildFlexibleTransitions,
                                child: _page ??= new RepaintBoundary(
                                    key: route.SubtreeKey,
                                    child: new Builder(route.BuildPage))))))));
    }

    private Widget BuildFlexibleTransitions(BuildContext context, Widget? child)
    {
        ModalRoute route = CurrentWidget.Route;
        return route.BuildFlexibleTransitions(
            context,
            _animation,
            _secondaryAnimation,
            new ListenableBuilder(
                listenable: route.Navigator?.UserGestureInProgressNotifier ?? Listenable.Merge(),
                builder: (_, pointerChild) =>
                {
                    bool ignoreEvents = ShouldIgnoreFocusRequest;
                    FocusScopeNode.CanRequestFocus = !ignoreEvents;
                    return new IgnorePointer(ignoring: ignoreEvents, child: pointerChild);
                },
                child: child));
    }
}

/// <summary>Flutter's <c>_DismissModalAction</c>: Escape pops a dismissible modal route.</summary>
internal sealed class DismissModalAction : DismissAction
{
    private readonly BuildContext _context;

    public DismissModalAction(BuildContext context)
    {
        _context = context;
    }

    public override bool IsEnabled(DismissIntent intent)
    {
        return ModalRoute.MaybeOf(_context)?.BarrierDismissible == true;
    }

    public override object? Invoke(DismissIntent intent)
    {
        Navigator.Of(_context).MaybePop();
        return null;
    }
}

internal static class NavigatorBackButtonDispatcher
{
    private static readonly List<Func<bool>> Handlers = [];

    public static void AddHandler(Func<bool> handler)
    {
        RemoveHandler(handler);
        Handlers.Add(handler);
    }

    public static void RemoveHandler(Func<bool> handler)
    {
        Handlers.RemoveAll(existing => ReferenceEquals(existing, handler));
    }

    public static bool DispatchBackButton()
    {
        for (int index = Handlers.Count - 1; index >= 0; index -= 1)
        {
            if (Handlers[index]())
            {
                return true;
            }
        }

        return false;
    }

    internal static void ResetForTests()
    {
        Handlers.Clear();
    }
}
