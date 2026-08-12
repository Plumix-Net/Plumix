using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/navigator.dart; flutter/packages/flutter/lib/src/widgets/routes.dart (approximate)

namespace Plumix.Widgets;

public sealed record RouteSettings(string? Name = null, object? Arguments = null);
public delegate Route? RouteFactory(RouteSettings settings);
public delegate IReadOnlyList<Route> NavigatorInitialRouteListFactory(
    NavigatorState navigator,
    string initialRouteName);
public delegate bool RoutePredicate(Route route);
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
    private List<LocalHistoryEntry>? _localHistoryEntries;

    protected Route(RouteSettings? settings = null)
    {
        Settings = settings ?? new RouteSettings();
    }

    public RouteSettings Settings { get; }

    public virtual bool Opaque => true;

    public virtual RoutePopDisposition PopDisposition => Navigator?.IsFirst(this) == false
        ? RoutePopDisposition.Pop
        : RoutePopDisposition.Bubble;

    public bool IsCurrent => Navigator?.CurrentRoute == this;

    public bool IsFirst => Navigator?.IsFirst(this) == true;

    public bool IsActive => Navigator?.IsActive(this) == true;

    public bool WillHandlePopInternally => _localHistoryEntries is { Count: > 0 };

    public virtual bool PopGestureEnabled => false;

    public virtual bool PopGestureInProgress => false;

    internal NavigatorState? Navigator { get; private set; }

    public virtual bool ImpliesAppBarDismissal
    {
        get
        {
            bool hasDismissalLocalHistory = _localHistoryEntries?.Any(entry => entry.ImpliesAppBarDismissal) == true;
            return hasDismissalLocalHistory || (Navigator?.CanPop ?? false);
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

    public virtual void DidPush()
    {
    }

    public virtual void DidPop(Route? previousRoute)
    {
    }

    public virtual void DidComplete(object? result)
    {
    }

    public virtual void OnPopInvoked(bool didPop)
    {
    }

    public virtual void OnPopInvokedWithResult(bool didPop, object? result)
    {
        OnPopInvoked(didPop);
    }

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
    }

    protected void NotifyRouteChanged()
    {
        Navigator?.NotifyRouteChanged();
    }

    public abstract Widget BuildPage(BuildContext context);
}

public abstract class TransitionRoute : Route
{
    private static readonly Animation<double> AlwaysDismissedAnimation =
        new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);
    private readonly ProxyAnimation _secondaryAnimation = new(AlwaysDismissedAnimation);
    private AnimationController? _controller;
    private bool _isPopped;
    private bool _handlingPopGesture;

    protected TransitionRoute(RouteSettings? settings = null) : base(settings)
    {
    }

    public virtual TimeSpan TransitionDuration => TimeSpan.Zero;

    public virtual TimeSpan ReverseTransitionDuration => TransitionDuration;

    public virtual bool AllowSnapshotting => true;

    /// <summary>
    /// Whether the route disposes the controller returned by <see cref="CreateAnimationController"/> when it is
    /// uninstalled. Routes that adopt a caller-supplied controller must return <see langword="false"/>.
    /// </summary>
    protected virtual bool WillDisposeAnimationController => true;

    public Animation<double> Animation => _controller
        ?? throw new InvalidOperationException("The route animation is unavailable before the route is installed.");

    public Animation<double> SecondaryAnimation => _secondaryAnimation;

    protected AnimationController Controller => _controller
        ?? throw new InvalidOperationException("The route controller is unavailable before the route is installed.");

    internal bool IsTransitionDismissed => _controller?.Status == AnimationStatus.Dismissed;

    internal bool EffectiveOpaque => Opaque && _controller?.Status == AnimationStatus.Completed;

    protected override void Install()
    {
        ValidateDuration(TransitionDuration, nameof(TransitionDuration));
        ValidateDuration(ReverseTransitionDuration, nameof(ReverseTransitionDuration));
        _isPopped = false;
        _controller = CreateAnimationController();
        _controller.Changed += HandleAnimationChanged;
        _controller.AddStatusListener(HandleStatusChanged);
        base.Install();
    }

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
        return new AnimationController(NormalizeDuration(TransitionDuration))
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

    public override void DidPush()
    {
        base.DidPush();
        _isPopped = false;
        if (TransitionDuration == TimeSpan.Zero)
        {
            Controller.SetValue(1.0);
            return;
        }

        Controller.Duration = TransitionDuration;
        Controller.Forward(from: 0.0);
    }

    public override void DidPop(Route? previousRoute)
    {
        _isPopped = true;
        if (ReverseTransitionDuration == TimeSpan.Zero)
        {
            Controller.Stop();
            Controller.SetValue(0.0);
        }
        else
        {
            Controller.ReverseDuration = ReverseTransitionDuration;
            Controller.Reverse();
        }

        base.DidPop(previousRoute);
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
            ? transitionRoute.Animation
            : AlwaysDismissedAnimation;
    }

    private void HandleAnimationChanged()
    {
        NotifyRouteChanged();
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        NotifyRouteChanged();
        if (_handlingPopGesture && status is AnimationStatus.Completed or AnimationStatus.Dismissed)
        {
            StopPopGestureIfNeeded();
        }

        if (status == AnimationStatus.Dismissed && _isPopped)
        {
            Navigator?.FinalizeTransitionRoute(this);
        }
    }

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

    protected ModalRoute(RouteSettings? settings = null) : base(settings)
    {
    }

    internal PageStorageBucket StorageBucket => _storageBucket;

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
    }

    public override void DidChangeNext(Route? nextRoute)
    {
        UpdateReceivedTransition(nextRoute);
        base.DidChangeNext(nextRoute);
    }

    public override void DidPopNext(Route nextRoute)
    {
        UpdateReceivedTransition(nextRoute);
        base.DidPopNext(nextRoute);
    }

    /// <summary>
    /// Builds the barrier painted below the route's page. Subclasses override this to create their own barrier.
    /// </summary>
    public virtual Widget BuildModalBarrier()
    {
        if (BarrierColor is { A: not 0 } barrierColor)
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

    /// <summary>The barrier overlay entry: the barrier plus the pointer and semantics wrappers Flutter adds.</summary>
    internal Widget BuildModalBarrierEntry()
    {
        Widget barrier = BuildModalBarrier();
        barrier = new IgnorePointer(
            ignoring: Animation.Status is not (AnimationStatus.Forward or AnimationStatus.Completed),
            child: barrier);
        return SemanticsDismissible && BarrierDismissible
            ? new Semantics(sortKey: new OrdinalSortKey(1.0), child: barrier)
            : barrier;
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

    public static ModalRoute? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<RouteScope>()?.Route as ModalRoute;
    }

    public static bool? IsCurrentOf(BuildContext context)
    {
        RouteScope? scope = context.DependOnInherited<RouteScope>();
        return scope?.Route is ModalRoute ? scope.IsCurrent : null;
    }

    public static bool? OpaqueOf(BuildContext context)
    {
        return context.DependOnInherited<RouteScope>()?.Route is ModalRoute route
            ? route.Opaque
            : null;
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
    protected PageRoute(RouteSettings? settings = null, bool fullscreenDialog = false) : base(settings)
    {
        FullscreenDialog = fullscreenDialog;
    }

    public bool FullscreenDialog { get; }

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
    protected PopupRoute(RouteSettings? settings = null) : base(settings)
    {
    }

    public override bool Opaque => false;

    public override bool AllowSnapshotting => false;
}

public sealed class BuilderPageRoute : PageRoute
{
    private readonly Func<BuildContext, Widget> _builder;

    public BuilderPageRoute(
        Func<BuildContext, Widget> builder,
        RouteSettings? settings = null,
        bool fullscreenDialog = false) : base(settings, fullscreenDialog)
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
        RouteSettings? settings = null) : base(settings, fullscreenDialog)
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
    public Navigator(
        Route initialRoute,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null) : base(key)
    {
        InitialRoute = initialRoute ?? throw new ArgumentNullException(nameof(initialRoute));
        Observers = observers ?? [];
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        RouteData initialRouteData,
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        RouteFactory? onUnknownRoute = null) : base(key)
    {
        OnGenerateRoute = onGenerateRoute ?? throw new ArgumentNullException(nameof(onGenerateRoute));
        InitialRouteData = initialRouteData ?? throw new ArgumentNullException(nameof(initialRouteData));
        Observers = observers ?? [];
        OnUnknownRoute = onUnknownRoute;
    }

    public Navigator(
        RouteFactory onGenerateRoute,
        string initialRouteName = "/",
        IReadOnlyList<NavigatorObserver>? observers = null,
        Key? key = null,
        NavigatorInitialRouteListFactory? onGenerateInitialRoutes = null,
        RouteFactory? onUnknownRoute = null) : base(key)
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
    }

    public Route? InitialRoute { get; }

    public string? InitialRouteName { get; }

    public RouteData? InitialRouteData { get; }

    public RouteFactory? OnGenerateRoute { get; }

    public NavigatorInitialRouteListFactory? OnGenerateInitialRoutes { get; }

    public RouteFactory? OnUnknownRoute { get; }

    public IReadOnlyList<NavigatorObserver> Observers { get; }

    public override State CreateState()
    {
        return new NavigatorState();
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

    public static bool TryHandleBackButton()
    {
        return NavigatorBackButtonDispatcher.DispatchBackButton();
    }
}

public sealed class NavigatorState : State
{
    private const double HeroTransitionDurationMilliseconds = 300;
    private readonly List<Route> _history = [];
    private readonly List<Route> _exitingRoutes = [];
    private readonly Dictionary<Route, Route?> _exitingPreviousRoutes = [];
    private readonly HashSet<Route> _heroDeferredRoutes = [];
    private readonly HashSet<Route> _routesBeingPopped = [];
    private readonly List<NavigatorObserver> _observers = [];
    private readonly Func<bool> _backButtonHandler;
    private readonly HeroTransitionController _heroTransitionController = new();
    private readonly Plumix.AnimationController _heroFlightController;
    private int _userGestureCount;
    private HeroTransitionSession? _heroTransitionSession;
    private bool? _lastCanHandlePop;
    private bool _navigationNotificationPending;

    public NavigatorState()
    {
        _backButtonHandler = HandleBackButton;
        _heroFlightController = new Plumix.AnimationController(
            TimeSpan.FromMilliseconds(HeroTransitionDurationMilliseconds))
        {
            Curve = Plumix.Curves.EaseInOut,
        };
        _heroFlightController.Changed += HandleHeroFlightTick;
        _heroFlightController.Completed += HandleHeroFlightCompleted;
        _heroFlightController.Dismissed += HandleHeroFlightCompleted;
    }

    private Navigator CurrentWidget => (Navigator)Element.Widget;

    public bool CanPop => _history.Count > 1;

    public Route? CurrentRoute => _history.Count == 0 ? null : _history[^1];

    public bool UserGestureInProgress => _userGestureCount > 0;

    public override void InitState()
    {
        base.InitState();
        SyncObservers(Array.Empty<NavigatorObserver>(), CurrentWidget.Observers);
        PushInitialRoute();
        NavigatorBackButtonDispatcher.AddHandler(_backButtonHandler);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldNavigator = (Navigator)oldWidget;
        SyncObservers(oldNavigator.Observers, CurrentWidget.Observers);
        if (_history.Count == 0)
        {
            PushInitialRoute();
        }
    }

    public override void Activate()
    {
        base.Activate();
        NavigatorBackButtonDispatcher.AddHandler(_backButtonHandler);
    }

    public override void Deactivate()
    {
        NavigatorBackButtonDispatcher.RemoveHandler(_backButtonHandler);
        base.Deactivate();
    }

    public override void Dispose()
    {
        NavigatorBackButtonDispatcher.RemoveHandler(_backButtonHandler);
        StopUserGesture();
        CancelHeroTransition(disposeDetachedRoute: true);
        _heroFlightController.Changed -= HandleHeroFlightTick;
        _heroFlightController.Completed -= HandleHeroFlightCompleted;
        _heroFlightController.Dismissed -= HandleHeroFlightCompleted;
        _heroFlightController.Dispose();

        foreach (var route in _history.Concat(_exitingRoutes).Distinct().ToArray())
        {
            route.Dispose();
            route.Detach();
        }

        _history.Clear();
        _exitingRoutes.Clear();
        _exitingPreviousRoutes.Clear();
        _heroDeferredRoutes.Clear();
        _routesBeingPopped.Clear();
        foreach (var observer in _observers)
        {
            if (ReferenceEquals(observer.Navigator, this))
            {
                observer.Navigator = null;
            }
        }

        _observers.Clear();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        Widget child;
        if (_heroTransitionSession == null)
        {
            var visibleRoutes = VisibleRoutes();
            child = visibleRoutes.Count switch
            {
                0 => new SizedBox(),
                _ => new Stack(
                    fit: StackFit.Expand,
                    children: visibleRoutes.Select(BuildRouteHost).Cast<Widget>().ToArray()),
            };
        }
        else
        {
            child = BuildHeroTransitionHost(_heroTransitionSession);
        }

        bool routeBlocksPop = !CanPop && CurrentRoute?.PopDisposition == RoutePopDisposition.DoNotPop;
        ScheduleNavigationNotification(CanPop || routeBlocksPop);
        return new NavigatorScope(this, child);
    }

    internal void NotifyRouteChanged()
    {
        if (Element.IsActive)
        {
            SetState(() => { });
        }
    }

    internal void FinalizeTransitionRoute(TransitionRoute route)
    {
        TryFinalizePoppedRoute(route);
    }

    internal bool IsFirst(Route route)
    {
        return _history.Count > 0 && ReferenceEquals(_history[0], route);
    }

    internal bool IsActive(Route route)
    {
        return _history.Contains(route);
    }

    private IReadOnlyList<Route> VisibleRoutes()
    {
        var routes = new List<Route>(_history.Count + _exitingRoutes.Count);
        routes.AddRange(_history);
        routes.AddRange(_exitingRoutes);
        if (routes.Count == 0)
        {
            return routes;
        }

        int firstVisible = routes.Count - 1;
        while (firstVisible > 0 && !IsEffectivelyOpaque(routes[firstVisible]))
        {
            firstVisible--;
        }

        return routes.Skip(firstVisible).ToArray();
    }

    private static bool IsEffectivelyOpaque(Route route)
    {
        return route is TransitionRoute transitionRoute
            ? transitionRoute.EffectiveOpaque
            : route.Opaque;
    }

    private ActiveRouteHost BuildRouteHost(Route route)
    {
        return new ActiveRouteHost(
            route: route,
            isCurrent: ReferenceEquals(CurrentRoute, route),
            heroTransitionController: _heroTransitionController,
            key: new ObjectKey(route));
    }

    private Widget BuildHeroTransitionHost(HeroTransitionSession session)
    {
        var children = new List<Widget>();
        if (session.Direction == HeroTransitionDirection.Pop)
        {
            children.Add(BuildRouteHost(session.ToRoute));
            children.Add(BuildRouteHost(session.FromRoute));
        }
        else
        {
            children.Add(BuildRouteHost(session.FromRoute));
            children.Add(BuildRouteHost(session.ToRoute));
        }

        var flights = _heroTransitionController.ActiveFlights;
        if (flights.Count > 0)
        {
            children.Add(
                BuildHeroFlightOverlay(
                    flights,
                    _heroFlightController.Evaluate(),
                    isPushTransition: session.Direction == HeroTransitionDirection.Push));
        }

        return new Stack(children: children);
    }

    private static Widget BuildHeroFlightOverlay(
        IReadOnlyList<HeroFlightManifest> flights,
        double progress,
        bool isPushTransition)
    {
        var overlayChildren = new List<Widget>(flights.Count);
        foreach (var flight in flights)
        {
            var rect = flight.RectTween.Evaluate(progress, flight.FromBounds, flight.ToBounds);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            overlayChildren.Add(
                new Positioned(
                    left: rect.X,
                    top: rect.Y,
                    width: rect.Width,
                    height: rect.Height,
                    child: new SizedBox(
                        width: rect.Width,
                        height: rect.Height,
                        child: flight.BuildShuttle(progress, isPushTransition))));
        }

        if (overlayChildren.Count == 0)
        {
            return new SizedBox();
        }

        return new Stack(children: overlayChildren);
    }

    public void Push(Route route)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var previousRoute = CurrentRoute;
            InstallRoute(route);
            previousRoute?.DidChangeNext(route);
            route.DidChangePrevious(previousRoute);
            route.DidPush();
            NotifyObserversPush(route, previousRoute);
            TryStartHeroTransition(
                fromRoute: previousRoute,
                toRoute: route,
                direction: HeroTransitionDirection.Push,
                detachFromRouteOnComplete: false,
                isUserGestureTransition: false);
        });
    }

    public void PushAndRemoveUntil(Route newRoute, RoutePredicate predicate)
    {
        if (newRoute == null)
        {
            throw new ArgumentNullException(nameof(newRoute));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var previousRoute = CurrentRoute;
            InstallRoute(newRoute);
            previousRoute?.DidChangeNext(newRoute);
            newRoute.DidChangePrevious(previousRoute);
            newRoute.DidPush();
            NotifyObserversPush(newRoute, previousRoute);
            RemoveRoutesBelowTopUntil(predicate);
        });
    }

    public void PushNamed(string routeName, object? arguments = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        Push(route);
    }

    public void PushNamed(RouteData routeData)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        Push(ResolveRouteData(routeData));
    }

    public void PushNamedAndRemoveUntil(string routeName, RoutePredicate predicate, object? arguments = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        PushAndRemoveUntil(route, predicate);
    }

    public void PushNamedAndRemoveUntil(RouteData routeData, RoutePredicate predicate)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        var route = ResolveRouteData(routeData);
        PushAndRemoveUntil(route, predicate);
    }

    public bool MaybePop(object? result = null)
    {
        var route = CurrentRoute;
        if (route == null)
        {
            return false;
        }

        if (!route.WillPop(result))
        {
            return true;
        }

        switch (route.PopDisposition)
        {
            case RoutePopDisposition.Bubble:
                return false;
            case RoutePopDisposition.DoNotPop:
                route.OnPopInvokedWithResult(didPop: false, result);
                return true;
            case RoutePopDisposition.Pop:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetState(() =>
        {
            PopCurrentRoute(result);
        });
        return true;
    }

    public void Pop(object? result = null)
    {
        var route = CurrentRoute;
        if (route == null)
        {
            throw new InvalidOperationException("Navigator cannot pop without a current route.");
        }

        if (!route.WillPop(result))
        {
            return;
        }

        if (!CanPop)
        {
            throw new InvalidOperationException("Navigator cannot pop the current route.");
        }

        SetState(() => PopCurrentRoute(result));
    }

    public bool MaybePopFromUserGesture(object? result = null)
    {
        StartUserGesture();
        try
        {
            return MaybePop(result);
        }
        finally
        {
            StopUserGesture();
        }
    }

    public void PopUntil(RoutePredicate predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            while (_history.Count > 0)
            {
                var route = _history[^1];
                if (predicate(route))
                {
                    break;
                }

                if (_history.Count <= 1 || !route.WillPop(result: null))
                {
                    break;
                }

                PopCurrentRoute(result: null);
            }
        });
    }

    public void PushReplacement(Route newRoute, object? result = null)
    {
        if (newRoute == null)
        {
            throw new ArgumentNullException(nameof(newRoute));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            var oldRoute = CurrentRoute;
            if (oldRoute == null)
            {
                InstallRoute(newRoute);
                newRoute.DidPush();
                NotifyObserversPush(newRoute, previousRoute: null);
                _ = result;
                return;
            }

            if (ReferenceEquals(oldRoute, newRoute))
            {
                throw new InvalidOperationException("Cannot replace a route with itself.");
            }

            var previousRoute = _history.Count > 1 ? _history[^2] : null;
            _history[^1] = newRoute;
            InstallRoute(newRoute);
            previousRoute?.DidPopNext(oldRoute);
            previousRoute?.DidChangeNext(newRoute);
            newRoute.DidChangePrevious(previousRoute);
            newRoute.DidPush();

            oldRoute.DidPop(previousRoute);
            oldRoute.DidComplete(result);
            oldRoute.Dispose();
            oldRoute.Detach();
            NotifyObserversReplace(newRoute, oldRoute);
            _ = result;
        });
    }

    public void PushReplacementNamed(string routeName, object? arguments = null, object? result = null)
    {
        var route = ResolveRouteLocation(routeName, arguments);
        PushReplacement(route, result);
    }

    public void PushReplacementNamed(RouteData routeData, object? result = null)
    {
        if (routeData == null)
        {
            throw new ArgumentNullException(nameof(routeData));
        }

        PushReplacement(ResolveRouteData(routeData), result);
    }

    public void RemoveRoute(Route route, object? result = null)
    {
        if (route == null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            int index = _history.FindIndex(existing => ReferenceEquals(existing, route));
            if (index < 0)
            {
                throw new InvalidOperationException("Route is not present in Navigator history.");
            }

            if (_history.Count <= 1)
            {
                throw new InvalidOperationException("Navigator cannot remove the last route.");
            }

            RemoveRouteAt(index, result);
        });
    }

    public void RemoveRouteBelow(Route anchorRoute, object? result = null)
    {
        if (anchorRoute == null)
        {
            throw new ArgumentNullException(nameof(anchorRoute));
        }

        SetState(() =>
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            int anchorIndex = _history.FindIndex(existing => ReferenceEquals(existing, anchorRoute));
            if (anchorIndex < 0)
            {
                throw new InvalidOperationException("Anchor route is not present in Navigator history.");
            }

            int removeIndex = anchorIndex - 1;
            if (removeIndex < 0)
            {
                throw new InvalidOperationException("Anchor route does not have a route below it.");
            }

            RemoveRouteAt(removeIndex, result);
        });
    }

    public void StartUserGesture()
    {
        var route = CurrentRoute;
        if (route == null)
        {
            return;
        }

        _userGestureCount += 1;
        if (_userGestureCount > 1)
        {
            return;
        }

        var previousRoute = _history.Count > 1 ? _history[^2] : null;
        NotifyObserversStartUserGesture(route, previousRoute);
    }

    public void StopUserGesture()
    {
        if (_userGestureCount == 0)
        {
            return;
        }

        _userGestureCount -= 1;
        if (_userGestureCount > 0)
        {
            return;
        }

        NotifyObserversStopUserGesture();
    }

    private void TryStartHeroTransition(
        Route? fromRoute,
        Route? toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete,
        bool isUserGestureTransition)
    {
        if (fromRoute == null || toRoute == null)
        {
            return;
        }

        if (TryDivertHeroTransition(fromRoute, toRoute, direction, detachFromRouteOnComplete, isUserGestureTransition))
        {
            return;
        }

        if (!_heroTransitionController.HasHeroes(fromRoute))
        {
            return;
        }

        CancelHeroTransition(disposeDetachedRoute: true);
        _heroTransitionSession = new HeroTransitionSession(
            fromRoute: fromRoute,
            toRoute: toRoute,
            direction: direction,
            detachFromRouteOnComplete: detachFromRouteOnComplete,
            isUserGestureTransition: isUserGestureTransition);

        Scheduler.AddPostFrameCallback(_ => ResolvePendingHeroTransition());
    }

    private bool TryDivertHeroTransition(
        Route fromRoute,
        Route toRoute,
        HeroTransitionDirection direction,
        bool detachFromRouteOnComplete,
        bool isUserGestureTransition)
    {
        var activeSession = _heroTransitionSession;
        if (activeSession == null
            || activeSession.Direction != HeroTransitionDirection.Push
            || direction != HeroTransitionDirection.Pop)
        {
            return false;
        }

        if (!ReferenceEquals(activeSession.FromRoute, toRoute)
            || !ReferenceEquals(activeSession.ToRoute, fromRoute))
        {
            return false;
        }

        if (_heroTransitionController.ActiveFlights.Count == 0)
        {
            return false;
        }

        _heroTransitionSession = new HeroTransitionSession(
            fromRoute: fromRoute,
            toRoute: toRoute,
            direction: direction,
            detachFromRouteOnComplete: detachFromRouteOnComplete,
            isUserGestureTransition: isUserGestureTransition);
        _heroTransitionController.UpdateActiveFlightPlaceholders(isPushTransition: false);
        _heroFlightController.Reverse(from: _heroFlightController.Value);
        return true;
    }

    private void ResolvePendingHeroTransition()
    {
        if (_heroTransitionSession == null || !Element.IsActive)
        {
            return;
        }

        var session = _heroTransitionSession;
        var flights = _heroTransitionController.CreateFlights(
            session.FromRoute,
            session.ToRoute,
            session.IsUserGestureTransition);
        if (flights.Count == 0)
        {
            SetState(() => CancelHeroTransition(disposeDetachedRoute: true));
            return;
        }

        SetState(() =>
        {
            if (_heroTransitionSession == null || !ReferenceEquals(_heroTransitionSession, session))
            {
                return;
            }

            _heroTransitionController.ActivateFlights(
                flights,
                isPushTransition: session.Direction == HeroTransitionDirection.Push);
            _heroFlightController.Forward(from: 0.0);
        });
    }

    private void HandleHeroFlightTick()
    {
        if (_heroTransitionSession == null || _heroTransitionController.ActiveFlights.Count == 0 || !Element.IsActive)
        {
            return;
        }

        SetState(() => { });
    }

    private void HandleHeroFlightCompleted()
    {
        if (_heroTransitionSession == null)
        {
            return;
        }

        if (!Element.IsActive)
        {
            CancelHeroTransition(disposeDetachedRoute: true);
            return;
        }

        SetState(() => CancelHeroTransition(disposeDetachedRoute: true));
    }

    private void CancelHeroTransition(bool disposeDetachedRoute)
    {
        _heroFlightController.Stop();
        _heroTransitionController.ClearFlights();

        var session = _heroTransitionSession;
        _heroTransitionSession = null;

        if (!disposeDetachedRoute || session == null || !session.DetachFromRouteOnComplete)
        {
            return;
        }

        _heroDeferredRoutes.Remove(session.FromRoute);
        TryFinalizePoppedRoute(session.FromRoute);
    }

    private void PushInitialRoute()
    {
        if (_history.Count > 0)
        {
            return;
        }

        IReadOnlyList<Route> initialRoutes = ResolveInitialRoutes();
        Route? previousRoute = null;
        foreach (Route initialRoute in initialRoutes)
        {
            InstallRoute(initialRoute);
            previousRoute?.DidChangeNext(initialRoute);
            initialRoute.DidChangePrevious(previousRoute);
            initialRoute.DidPush();
            NotifyObserversPush(initialRoute, previousRoute);
            previousRoute = initialRoute;
        }
    }

    private void PopCurrentRoute(object? result)
    {
        if (_history.Count == 0)
        {
            return;
        }

        var route = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        var previousRoute = CurrentRoute;
        bool shouldAnimateHero = previousRoute != null && _heroTransitionController.HasHeroes(route);
        _exitingRoutes.Add(route);
        _exitingPreviousRoutes[route] = previousRoute;
        if (shouldAnimateHero)
        {
            _heroDeferredRoutes.Add(route);
        }

        _routesBeingPopped.Add(route);
        try
        {
            previousRoute?.DidPopNext(route);
            route.DidPop(previousRoute);
            route.DidComplete(result);
            route.OnPopInvokedWithResult(didPop: true, result);
        }
        finally
        {
            _routesBeingPopped.Remove(route);
        }

        NotifyObserversPop(route, previousRoute);

        if (shouldAnimateHero)
        {
            TryStartHeroTransition(
                fromRoute: route,
                toRoute: previousRoute,
                direction: HeroTransitionDirection.Pop,
                detachFromRouteOnComplete: true,
                isUserGestureTransition: UserGestureInProgress);
            return;
        }

        CancelHeroTransition(disposeDetachedRoute: true);
        TryFinalizePoppedRoute(route);
    }

    private void TryFinalizePoppedRoute(Route route)
    {
        if (!_exitingRoutes.Contains(route)
            || _heroDeferredRoutes.Contains(route)
            || _routesBeingPopped.Contains(route))
        {
            return;
        }

        if (route is TransitionRoute transitionRoute && !transitionRoute.IsTransitionDismissed)
        {
            return;
        }

        Route? previousRoute = _exitingPreviousRoutes.GetValueOrDefault(route);
        _exitingRoutes.Remove(route);
        _exitingPreviousRoutes.Remove(route);
        route.Dispose();
        route.Detach();
        previousRoute?.DidChangeNext(nextRoute: null);
        if (Element.IsActive)
        {
            SetState(() => { });
        }
    }

    private void ScheduleNavigationNotification(bool canHandlePop)
    {
        if (_lastCanHandlePop == canHandlePop)
        {
            return;
        }

        _lastCanHandlePop = canHandlePop;
        if (_navigationNotificationPending)
        {
            return;
        }

        _navigationNotificationPending = true;
        Scheduler.AddPostFrameCallback(_ =>
        {
            _navigationNotificationPending = false;
            if (!Mounted)
            {
                return;
            }

            new NavigationNotification(_lastCanHandlePop ?? false).Dispatch(Context);
        });
    }

    private void InstallRoute(Route route)
    {
        if (route.Navigator != null && !ReferenceEquals(route.Navigator, this))
        {
            throw new InvalidOperationException("Route is already attached to another Navigator.");
        }

        if (!ReferenceEquals(route.Navigator, this))
        {
            route.Attach(this);
        }

        if (_history.Count == 0 || !ReferenceEquals(_history[^1], route))
        {
            _history.Add(route);
        }
    }

    private void RemoveRoutesBelowTopUntil(RoutePredicate predicate)
    {
        while (_history.Count > 1)
        {
            var routeBelowTop = _history[^2];
            if (predicate(routeBelowTop))
            {
                break;
            }

            RemoveRouteAt(_history.Count - 2, result: null);
        }
    }

    private void RemoveRouteAt(int index, object? result)
    {
        if (index < 0 || index >= _history.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        bool wasTopRoute = index == _history.Count - 1;
        var previousRoute = index > 0 ? _history[index - 1] : null;
        var nextRoute = index + 1 < _history.Count ? _history[index + 1] : null;
        var removedRoute = _history[index];
        _history.RemoveAt(index);

        if (wasTopRoute)
        {
            previousRoute?.DidPopNext(removedRoute);
        }
        removedRoute.DidComplete(result);
        removedRoute.Dispose();
        removedRoute.Detach();

        previousRoute?.DidChangeNext(nextRoute);
        nextRoute?.DidChangePrevious(previousRoute);
        NotifyObserversRemove(removedRoute, previousRoute);
    }

    private IReadOnlyList<Route> ResolveInitialRoutes()
    {
        if (CurrentWidget.InitialRoute != null)
        {
            return [CurrentWidget.InitialRoute];
        }

        if (CurrentWidget.InitialRouteData != null)
        {
            return [ResolveRouteData(CurrentWidget.InitialRouteData)];
        }

        string? routeName = CurrentWidget.InitialRouteName;
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new InvalidOperationException("Navigator requires either InitialRoute or InitialRouteName.");
        }

        if (CurrentWidget.OnGenerateInitialRoutes != null)
        {
            IReadOnlyList<Route> generated = CurrentWidget.OnGenerateInitialRoutes(this, routeName);
            if (generated.Count == 0)
            {
                throw new InvalidOperationException("onGenerateInitialRoutes must return at least one route.");
            }

            return generated;
        }

        if (!routeName.StartsWith("/", StringComparison.Ordinal) || routeName == "/")
        {
            Route? directRoute = TryResolveGeneratedRoute(routeName, arguments: null);
            return directRoute != null
                ? [directRoute]
                : [ResolveDefaultInitialRoute()];
        }

        var routeNames = new List<string> { "/" };
        string[] segments = routeName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string currentPath = string.Empty;
        foreach (string segment in segments)
        {
            currentPath += "/" + segment;
            routeNames.Add(currentPath);
        }

        var routes = new List<Route>(routeNames.Count);
        foreach (string candidateName in routeNames)
        {
            Route? candidate = TryResolveGeneratedRoute(candidateName, arguments: null);
            if (candidate != null)
            {
                routes.Add(candidate);
                continue;
            }

            if (string.Equals(candidateName, routeName, StringComparison.Ordinal))
            {
                foreach (Route generatedRoute in routes)
                {
                    generatedRoute.Dispose();
                }

                return [ResolveDefaultInitialRoute()];
            }
        }

        return routes.Count > 0 ? routes : [ResolveDefaultInitialRoute()];
    }

    private Route ResolveNamedRoute(string routeName, object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            throw new ArgumentException("routeName cannot be null or whitespace.", nameof(routeName));
        }

        var settings = new RouteSettings(Name: routeName, Arguments: arguments);
        Route? route = CurrentWidget.OnGenerateRoute?.Invoke(settings);
        route ??= CurrentWidget.OnUnknownRoute?.Invoke(settings);
        if (route == null)
        {
            throw new InvalidOperationException(
                $"Neither onGenerateRoute nor onUnknownRoute generated route '{routeName}'.");
        }

        return route;
    }

    private Route ResolveDefaultInitialRoute()
    {
        Route? route = TryResolveGeneratedRoute("/", arguments: null);
        return route ?? ResolveNamedRoute("/", arguments: null);
    }

    private Route? TryResolveGeneratedRoute(string routeName, object? arguments)
    {
        return CurrentWidget.OnGenerateRoute?.Invoke(
            new RouteSettings(Name: routeName, Arguments: arguments));
    }

    private Route ResolveRouteData(RouteData routeData)
    {
        return ResolveNamedRoute(routeData.Name, routeData);
    }

    private Route ResolveRouteLocation(string routeName, object? arguments)
    {
        if (routeName.IndexOf('?') >= 0 || routeName.Contains("://", StringComparison.Ordinal))
        {
            return ResolveRouteData(RouteData.FromLocation(routeName, arguments));
        }

        return ResolveNamedRoute(routeName, arguments);
    }

    private void SyncObservers(
        IReadOnlyList<NavigatorObserver> oldObservers,
        IReadOnlyList<NavigatorObserver> newObservers)
    {
        foreach (var oldObserver in oldObservers)
        {
            if (newObservers.Contains(oldObserver))
            {
                continue;
            }

            _observers.Remove(oldObserver);
            if (ReferenceEquals(oldObserver.Navigator, this))
            {
                oldObserver.Navigator = null;
            }
        }

        foreach (var observer in newObservers)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            observer.Navigator = this;
        }
    }

    private void NotifyObserversPush(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidPush(route, previousRoute);
        }
    }

    private void NotifyObserversPop(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidPop(route, previousRoute);
        }
    }

    private void NotifyObserversRemove(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidRemove(route, previousRoute);
        }
    }

    private void NotifyObserversReplace(Route newRoute, Route? oldRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidReplace(newRoute, oldRoute);
        }
    }

    private void NotifyObserversStartUserGesture(Route route, Route? previousRoute)
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidStartUserGesture(route, previousRoute);
        }
    }

    private void NotifyObserversStopUserGesture()
    {
        foreach (var observer in _observers.ToArray())
        {
            observer.DidStopUserGesture();
        }
    }

    private enum HeroTransitionDirection
    {
        Push,
        Pop,
    }

    private sealed class HeroTransitionSession
    {
        public HeroTransitionSession(
            Route fromRoute,
            Route toRoute,
            HeroTransitionDirection direction,
            bool detachFromRouteOnComplete,
            bool isUserGestureTransition)
        {
            FromRoute = fromRoute;
            ToRoute = toRoute;
            Direction = direction;
            DetachFromRouteOnComplete = detachFromRouteOnComplete;
            IsUserGestureTransition = isUserGestureTransition;
        }

        public Route FromRoute { get; }

        public Route ToRoute { get; }

        public HeroTransitionDirection Direction { get; }

        public bool DetachFromRouteOnComplete { get; }

        public bool IsUserGestureTransition { get; }
    }

    private bool HandleBackButton()
    {
        if (!Element.IsActive)
        {
            return false;
        }

        return MaybePop();
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

internal sealed class RouteScope : InheritedWidget
{
    public RouteScope(
        Route route,
        bool isCurrent,
        Widget child,
        Key? key = null) : base(key)
    {
        Route = route;
        IsCurrent = isCurrent;
        Child = child;
    }

    public Route Route { get; }

    public bool IsCurrent { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (RouteScope)oldWidget;
        return !ReferenceEquals(oldScope.Route, Route)
               || oldScope.IsCurrent != IsCurrent;
    }
}

internal sealed class ActiveRouteHost : StatelessWidget
{
    private readonly Route _route;
    private readonly bool _isCurrent;
    private readonly HeroTransitionController _heroTransitionController;

    public ActiveRouteHost(
        Route route,
        bool isCurrent,
        HeroTransitionController heroTransitionController,
        Key? key = null) : base(key)
    {
        _route = route;
        _isCurrent = isCurrent;
        _heroTransitionController = heroTransitionController;
    }

    public override Widget Build(BuildContext context)
    {
        // Flutter's ModalRoute contributes two overlay entries: the barrier below, the modal scope above.
        // The barrier is a sibling of the page, so route transitions never apply to it, and the scope sorts
        // before the barrier in the semantics tree.
        Widget host = _route is ModalRoute modalRoute
            ? new Stack(
                fit: StackFit.Expand,
                children:
                [
                    modalRoute.BuildModalBarrierEntry(),
                    new Semantics(sortKey: new OrdinalSortKey(0.0), child: new RoutePageHost(_route)),
                ])
            : new RoutePageHost(_route);

        return new RouteScope(
            route: _route,
            isCurrent: _isCurrent,
            child: new HeroControllerScope(
                controller: _heroTransitionController,
                route: _route,
                child: host));
    }
}

internal sealed class RoutePageHost : StatelessWidget
{
    private readonly Route _route;

    public RoutePageHost(Route route)
    {
        _route = route;
    }

    public override Widget Build(BuildContext context)
    {
        Widget page = _route.BuildPage(context);
        page = _route is ModalRoute modalRoute
            ? new PageStorage(modalRoute.StorageBucket, page)
            : page;
        return _route is ModalRoute transitionModalRoute
            ? transitionModalRoute.BuildFlexibleTransitions(
                context,
                transitionModalRoute.Animation,
                transitionModalRoute.SecondaryAnimation,
                page)
            : _route is TransitionRoute transitionRoute
            ? transitionRoute.BuildTransitions(
                context,
                transitionRoute.Animation,
                transitionRoute.SecondaryAnimation,
                page)
            : page;
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
