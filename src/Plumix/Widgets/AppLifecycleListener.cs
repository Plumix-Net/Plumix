using System.Diagnostics;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/app_lifecycle_listener.dart
// flutter/packages/flutter/lib/src/widgets/binding.dart
// flutter/packages/flutter/lib/src/services/binding.dart
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart

public delegate Task<AppExitResponse> AppExitRequestCallback();

/// <summary>Host-reported accessibility animation preferences.</summary>
public readonly record struct AccessibilityFeatures(
    bool ReduceMotion = false,
    bool DisableAnimations = false);

public interface WidgetsBindingObserver
{
    void DidChangeAccessibilityFeatures()
    {
    }

    void DidChangeAppLifecycleState(AppLifecycleState state)
    {
    }

    /// <summary>Called when the application's dimensions change, e.g. a window resize.</summary>
    /// <remarks>Flutter's <c>WidgetsBindingObserver.didChangeMetrics</c>.</remarks>
    void DidChangeMetrics()
    {
    }

    /// <summary>Called when the platform's text scale factor changes.</summary>
    /// <remarks>Flutter's <c>WidgetsBindingObserver.didChangeTextScaleFactor</c>.</remarks>
    void DidChangeTextScaleFactor()
    {
    }

    /// <summary>Called when the platform brightness changes.</summary>
    /// <remarks>Flutter's <c>WidgetsBindingObserver.didChangePlatformBrightness</c>.</remarks>
    void DidChangePlatformBrightness()
    {
    }

    /// <summary>Called when a view gained or lost focus on the platform.</summary>
    /// <remarks>Flutter's <c>WidgetsBindingObserver.didChangeViewFocus</c>.</remarks>
    void DidChangeViewFocus(ViewFocusEvent @event)
    {
        _ = @event;
    }

    /// <summary>
    /// Called when the host reports that the user tapped the status bar. Only iOS and macOS report it;
    /// scaffolds use it to scroll their primary scrollable back to the top.
    /// </summary>
    void HandleStatusBarTap()
    {
    }

    Task<AppExitResponse> DidRequestAppExit()
    {
        return Task.FromResult(AppExitResponse.Exit);
    }

    /// <summary>
    /// Called when the host asks the application to pop the current route. Returning <c>true</c> stops the
    /// dispatch; returning <c>false</c> lets the next observer (and finally the navigator stack) handle it.
    /// </summary>
    Task<bool> DidPopRoute()
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Called when the host pushes a new route location into the application. Returning <c>true</c> stops the
    /// dispatch.
    /// </summary>
    Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
    {
        _ = routeInformation;
        return Task.FromResult(false);
    }

    bool HandleStartBackGesture(PredictiveBackEvent backEvent)
    {
        _ = backEvent;
        return false;
    }

    void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent)
    {
        _ = backEvent;
    }

    void HandleCommitBackGesture()
    {
    }

    void HandleCancelBackGesture()
    {
    }
}

public class WidgetsBinding
{
    private static readonly WidgetsBinding SharedInstance = new();
    private readonly List<WidgetsBindingObserver> _observers = [];
    private readonly List<WidgetsBindingObserver> _backGestureObservers = [];

    static WidgetsBinding()
    {
        // Only the ambient binding owns the platform channels; a locally constructed one (tests do this)
        // must not hijack them.
        SharedInstance.InitInstances();
    }

    public static WidgetsBinding Instance => SharedInstance;

    public AppLifecycleState? LifecycleState { get; private set; }

    public AccessibilityFeatures AccessibilityFeatures { get; private set; }

    public void AddObserver(WidgetsBindingObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
    }

    public bool RemoveObserver(WidgetsBindingObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _observers.Remove(observer);
    }

    /// <summary>Updates accessibility animation preferences and notifies registered widgets.</summary>
    public void HandleAccessibilityFeaturesChanged(AccessibilityFeatures features)
    {
        if (AccessibilityFeatures == features)
        {
            return;
        }

        AccessibilityFeatures = features;
        AnimationController.DisableAnimations = features.DisableAnimations;
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                observer.DidChangeAccessibilityFeatures();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Exception while dispatching {nameof(WidgetsBindingObserver.DidChangeAccessibilityFeatures)}: "
                    + exception);
            }
        }
    }

    /// <summary>
    /// Called when the platform's view metrics changed: the renderer binding reconfigures every
    /// registered <see cref="RenderView"/>, then the observers hear <c>DidChangeMetrics</c>.
    /// </summary>
    /// <remarks>Flutter's <c>WidgetsBinding.handleMetricsChanged</c> over <c>RendererBinding</c>'s.</remarks>
    public void HandleMetricsChanged()
    {
        RendererBinding.Instance.HandleMetricsChanged();
        Dispatch(static observer => observer.DidChangeMetrics(), nameof(WidgetsBindingObserver.DidChangeMetrics));
    }

    /// <summary>Called when the platform's text scale factor changed.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.handleTextScaleFactorChanged</c>.</remarks>
    public void HandleTextScaleFactorChanged()
    {
        Dispatch(
            static observer => observer.DidChangeTextScaleFactor(),
            nameof(WidgetsBindingObserver.DidChangeTextScaleFactor));
    }

    /// <summary>Called when the platform brightness changed.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.handlePlatformBrightnessChanged</c>.</remarks>
    public void HandlePlatformBrightnessChanged()
    {
        Dispatch(
            static observer => observer.DidChangePlatformBrightness(),
            nameof(WidgetsBindingObserver.DidChangePlatformBrightness));
    }

    /// <summary>Called when a view gained or lost focus on the platform.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding._handleViewFocusChanged</c>, which the platform dispatcher feeds.</remarks>
    public void HandleViewFocusChanged(ViewFocusEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        Dispatch(observer => observer.DidChangeViewFocus(@event), nameof(WidgetsBindingObserver.DidChangeViewFocus));
    }

    private void Dispatch(Action<WidgetsBindingObserver> callback, string name)
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                callback(observer);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Exception while dispatching {name}: " + exception);
            }
        }
    }

    public void HandleAppLifecycleStateChanged(AppLifecycleState state)
    {
        foreach (AppLifecycleState generatedState in GenerateStateTransitions(LifecycleState, state))
        {
            LifecycleState = generatedState;
            foreach (WidgetsBindingObserver observer in _observers.ToArray())
            {
                try
                {
                    observer.DidChangeAppLifecycleState(generatedState);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(
                        $"Exception while dispatching {nameof(WidgetsBindingObserver.DidChangeAppLifecycleState)}: "
                        + exception);
                }
            }
        }
    }

    /// <summary>
    /// Dispatches a host status-bar tap to every registered observer. Hosts whose platform reports the
    /// gesture (iOS, macOS) call this; the default adapters never do.
    /// </summary>
    public void HandleStatusBarTap()
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                observer.HandleStatusBarTap();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Exception while dispatching {nameof(WidgetsBindingObserver.HandleStatusBarTap)}: "
                    + exception);
            }
        }
    }

    public async Task<AppExitResponse> HandleRequestAppExit()
    {
        bool didCancel = false;
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                if (await observer.DidRequestAppExit().ConfigureAwait(false) == AppExitResponse.Cancel)
                {
                    didCancel = true;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Exception while dispatching {nameof(WidgetsBindingObserver.DidRequestAppExit)}: "
                    + exception);
            }
        }

        return didCancel ? AppExitResponse.Cancel : AppExitResponse.Exit;
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.initInstances</c>: installs the <c>flutter/navigation</c> handler so a
    /// host can push deep links and back requests through the channel.
    /// </summary>
    public void InitInstances()
    {
        SystemChannels.Navigation.SetMethodCallHandler(HandleNavigationInvocation);
    }

    private Task<object?> HandleNavigationInvocation(MethodCall call)
    {
        switch (call.Method)
        {
            case "popRoute":
                return Task.FromResult<object?>(HandlePopRoute());
            case "pushRoute":
                return Task.FromResult<object?>(HandlePushRoute((string)call.Arguments!));
            case "pushRouteInformation":
                var arguments = (IDictionary<string, object?>)call.Arguments!;
                return Task.FromResult<object?>(HandlePushRouteInformation(new RouteInformation(
                    new Uri((string)arguments["location"]!, UriKind.RelativeOrAbsolute),
                    arguments.TryGetValue("state", out object? state) ? state : null)));
            default:
                return Task.FromResult<object?>(null);
        }
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.handlePushRoute</c>: offers a deep-linked route name to the observers.
    /// </summary>
    public bool HandlePushRoute(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return HandlePushRouteInformation(
            new RouteInformation(new Uri(route, UriKind.RelativeOrAbsolute)));
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.handlePopRoute</c>: offers the pop to every observer in registration order
    /// and stops at the first one that handles it. When no observer handles it, the platform is asked to pop
    /// the application off its own navigation stack.
    /// </summary>
    public bool HandlePopRoute()
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                Task<bool> handled = observer.DidPopRoute();
                if (handled.IsCompletedSuccessfully && handled.Result)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    library: "widgets library",
                    context: new ErrorDescription(
                        "while dispatching notifications for WidgetsBindingObserver.DidPopRoute")));
            }
        }

        _ = ReportPopFailure(SystemNavigator.Pop());
        return false;
    }

    private static async Task ReportPopFailure(Task pop)
    {
        try
        {
            await pop.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                library: "widgets library",
                context: new ErrorDescription("while popping route")));
        }
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.handlePushRouteInformation</c>: offers the location to every observer in
    /// registration order and stops at the first one that handles it.
    /// </summary>
    public bool HandlePushRouteInformation(RouteInformation routeInformation)
    {
        ArgumentNullException.ThrowIfNull(routeInformation);
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                Task<bool> handled = observer.DidPushRouteInformation(routeInformation);
                if (handled.IsCompletedSuccessfully && handled.Result)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    library: "widgets library",
                    context: new ErrorDescription(
                        "while dispatching notifications for WidgetsBindingObserver.DidPushRouteInformation")));
            }
        }

        return false;
    }

    public bool HandleStartBackGesture(PredictiveBackEvent backEvent)
    {
        ArgumentNullException.ThrowIfNull(backEvent);
        _backGestureObservers.Clear();
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                if (observer.HandleStartBackGesture(backEvent))
                {
                    _backGestureObservers.Add(observer);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"Exception while dispatching {nameof(WidgetsBindingObserver.HandleStartBackGesture)}: "
                    + exception);
            }
        }

        return _backGestureObservers.Count > 0;
    }

    public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent)
    {
        ArgumentNullException.ThrowIfNull(backEvent);
        foreach (WidgetsBindingObserver observer in _backGestureObservers.ToArray())
        {
            observer.HandleUpdateBackGestureProgress(backEvent);
        }
    }

    public bool HandleCommitBackGesture()
    {
        WidgetsBindingObserver[] observers = _backGestureObservers.ToArray();
        _backGestureObservers.Clear();
        if (observers.Length == 0)
        {
            return HandlePopRoute();
        }

        foreach (WidgetsBindingObserver observer in observers)
        {
            observer.HandleCommitBackGesture();
        }

        return true;
    }

    public void HandleCancelBackGesture()
    {
        WidgetsBindingObserver[] observers = _backGestureObservers.ToArray();
        _backGestureObservers.Clear();
        foreach (WidgetsBindingObserver observer in observers)
        {
            observer.HandleCancelBackGesture();
        }
    }

    internal void ResetObserversForTests()
    {
        _observers.Clear();
        _backGestureObservers.Clear();
    }

    private static IReadOnlyList<AppLifecycleState> GenerateStateTransitions(
        AppLifecycleState? previousState,
        AppLifecycleState state)
    {
        if (previousState == state)
        {
            return [];
        }

        if (previousState is null)
        {
            return [state];
        }

        var stateChanges = new List<AppLifecycleState>();
        int previousStateIndex = (int)previousState.Value;
        int stateIndex = (int)state;
        if (state == AppLifecycleState.Detached)
        {
            for (int index = previousStateIndex + 1; index < Enum.GetValues<AppLifecycleState>().Length; index++)
            {
                stateChanges.Add((AppLifecycleState)index);
            }

            stateChanges.Add(AppLifecycleState.Detached);
        }
        else if (previousStateIndex > stateIndex)
        {
            for (int index = stateIndex; index < previousStateIndex; index++)
            {
                stateChanges.Insert(0, (AppLifecycleState)index);
            }
        }
        else
        {
            for (int index = previousStateIndex + 1; index <= stateIndex; index++)
            {
                stateChanges.Add((AppLifecycleState)index);
            }
        }

        return stateChanges;
    }
}

public sealed class AppLifecycleListener : WidgetsBindingObserver, IDisposable
{
    private bool _isDisposed;
    private AppLifecycleState? _lifecycleState;

    public AppLifecycleListener(
        WidgetsBinding? binding = null,
        Action? onResume = null,
        Action? onInactive = null,
        Action? onHide = null,
        Action? onShow = null,
        Action? onPause = null,
        Action? onRestart = null,
        Action? onDetach = null,
        AppExitRequestCallback? onExitRequested = null,
        Action<AppLifecycleState>? onStateChange = null)
    {
        Binding = binding ?? WidgetsBinding.Instance;
        OnResume = onResume;
        OnInactive = onInactive;
        OnHide = onHide;
        OnShow = onShow;
        OnPause = onPause;
        OnRestart = onRestart;
        OnDetach = onDetach;
        OnExitRequested = onExitRequested;
        OnStateChange = onStateChange;
        _lifecycleState = Binding.LifecycleState;
        Binding.AddObserver(this);
    }

    public WidgetsBinding Binding { get; }
    public Action<AppLifecycleState>? OnStateChange { get; }
    public Action? OnInactive { get; }
    public Action? OnResume { get; }
    public Action? OnHide { get; }
    public Action? OnShow { get; }
    public Action? OnPause { get; }
    public Action? OnRestart { get; }
    public AppExitRequestCallback? OnExitRequested { get; }
    public Action? OnDetach { get; }

    public void DidChangeAppLifecycleState(AppLifecycleState state)
    {
        ThrowIfDisposed();
        AppLifecycleState? previousState = _lifecycleState;
        if (state == previousState)
        {
            return;
        }

        _lifecycleState = state;
        switch (state)
        {
            case AppLifecycleState.Resumed:
                OnResume?.Invoke();
                break;
            case AppLifecycleState.Inactive:
                if (previousState == AppLifecycleState.Hidden)
                {
                    OnShow?.Invoke();
                }
                else if (previousState is null or AppLifecycleState.Resumed)
                {
                    OnInactive?.Invoke();
                }

                break;
            case AppLifecycleState.Hidden:
                if (previousState == AppLifecycleState.Paused)
                {
                    OnRestart?.Invoke();
                }
                else if (previousState is null or AppLifecycleState.Inactive)
                {
                    OnHide?.Invoke();
                }

                break;
            case AppLifecycleState.Paused:
                if (previousState is null or AppLifecycleState.Hidden)
                {
                    OnPause?.Invoke();
                }

                break;
            case AppLifecycleState.Detached:
                OnDetach?.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

        OnStateChange?.Invoke(state);
    }

    public Task<AppExitResponse> DidRequestAppExit()
    {
        ThrowIfDisposed();
        return OnExitRequested?.Invoke() ?? Task.FromResult(AppExitResponse.Exit);
    }

    public void Dispose()
    {
        ThrowIfDisposed();
        Binding.RemoveObserver(this);
        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
