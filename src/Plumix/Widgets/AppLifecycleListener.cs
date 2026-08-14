using System.Diagnostics;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/app_lifecycle_listener.dart
// flutter/packages/flutter/lib/src/widgets/binding.dart
// flutter/packages/flutter/lib/src/services/binding.dart
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart

public delegate Task<AppExitResponse> AppExitRequestCallback();

public interface WidgetsBindingObserver
{
    void DidChangeAppLifecycleState(AppLifecycleState state)
    {
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

    public static WidgetsBinding Instance => SharedInstance;

    public AppLifecycleState? LifecycleState { get; private set; }

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
    /// Flutter's <c>WidgetsBinding.handlePopRoute</c>: offers the pop to every observer in registration order
    /// and stops at the first one that handles it.
    /// </summary>
    public bool HandlePopRoute()
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            Task<bool> handled = observer.DidPopRoute();
            if (handled.IsCompletedSuccessfully && handled.Result)
            {
                return true;
            }
        }

        return false;
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
            Task<bool> handled = observer.DidPushRouteInformation(routeInformation);
            if (handled.IsCompletedSuccessfully && handled.Result)
            {
                return true;
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
            return Navigator.TryHandleBackButton();
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
