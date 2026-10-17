using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Rendering;
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

    /// <summary>
    /// Called when the system tells the app that the user's locale has changed, for example when the user
    /// changes the system language settings.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBindingObserver.didChangeLocales</c>; this exposes
    /// <see cref="PlatformDispatcher.OnLocaleChanged"/>.
    /// </remarks>
    void DidChangeLocales(IReadOnlyList<Locale>? locales)
    {
        _ = locales;
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

    /// <summary>Called when the host tells the application to push a new route.</summary>
    /// <remarks>
    /// Flutter's deprecated <c>WidgetsBindingObserver.didPushRoute</c>, which the default
    /// <see cref="DidPushRouteInformation"/> still calls with the decoded path, query and fragment.
    /// </remarks>
    [Obsolete("Use DidPushRouteInformation instead. This feature was deprecated after v3.8.0-14.0.pre.")]
    Task<bool> DidPushRoute(string route)
    {
        _ = route;
        return Task.FromResult(false);
    }

    /// <summary>
    /// Called when the host pushes a new route location into the application. Returning <c>true</c> stops the
    /// dispatch.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBindingObserver.didPushRouteInformation</c>. The default calls the deprecated
    /// <see cref="DidPushRoute"/> with <c>Uri.decodeComponent</c> of the location's path (<c>/</c> when
    /// empty), query and fragment.
    /// </remarks>
    Task<bool> DidPushRouteInformation(RouteInformation routeInformation)
    {
        ArgumentNullException.ThrowIfNull(routeInformation);
#pragma warning disable CS0618 // Dart's default still forwards to the deprecated callback.
        return DidPushRoute(DidPushRouteLocation.From(routeInformation.Uri));
#pragma warning restore CS0618
    }

    /// <summary>Called when the system is running low on memory.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBindingObserver.didHaveMemoryPressure</c>: the <c>memoryPressure</c> system
    /// message reaches it after the image cache and the root asset bundle were cleared.
    /// </remarks>
    void DidHaveMemoryPressure()
    {
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

/// <summary>The location the default <see cref="WidgetsBindingObserver.DidPushRouteInformation"/> hands on.</summary>
/// <remarks>
/// Dart's <c>Uri.decodeComponent(Uri(path: uri.path.isEmpty ? '/' : uri.path, queryParameters:
/// uri.queryParametersAll.isEmpty ? null : uri.queryParametersAll, fragment: uri.fragment.isEmpty ? null
/// : uri.fragment).toString())</c>: the scheme and authority are dropped, an empty path becomes <c>/</c>,
/// repeated query keys are grouped in first-seen order, and the whole location is percent-decoded.
/// </remarks>
internal static class DidPushRouteLocation
{
    public static string From(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        string path;
        string query;
        string fragment;
        if (uri.IsAbsoluteUri)
        {
            path = uri.AbsolutePath;
            query = uri.Query.TrimStart('?');
            fragment = uri.Fragment.TrimStart('#');
        }
        else
        {
            string text = uri.OriginalString;
            int hash = text.IndexOf('#', StringComparison.Ordinal);
            fragment = hash >= 0 ? text[(hash + 1)..] : string.Empty;
            text = hash >= 0 ? text[..hash] : text;
            int question = text.IndexOf('?', StringComparison.Ordinal);
            query = question >= 0 ? text[(question + 1)..] : string.Empty;
            path = question >= 0 ? text[..question] : text;
        }

        var location = new System.Text.StringBuilder(path.Length == 0 ? "/" : path);
        if (query.Length > 0)
        {
            // `queryParametersAll` groups the values of a repeated key under its first occurrence.
            var parameters = new List<(string Key, List<string> Values)>();
            foreach (string pair in query.Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                int equals = pair.IndexOf('=', StringComparison.Ordinal);
                string key = equals >= 0 ? pair[..equals] : pair;
                string value = equals >= 0 ? pair[(equals + 1)..] : string.Empty;
                int index = parameters.FindIndex(entry => entry.Key == key);
                if (index < 0)
                {
                    parameters.Add((key, [value]));
                }
                else
                {
                    parameters[index].Values.Add(value);
                }
            }

            if (parameters.Count > 0)
            {
                location.Append('?').Append(string.Join(
                    '&',
                    parameters.SelectMany(static entry => entry.Values.Select(value => $"{entry.Key}={value}"))));
            }
        }

        if (fragment.Length > 0)
        {
            location.Append('#').Append(fragment);
        }

        return Uri.UnescapeDataString(location.ToString());
    }
}

public partial class WidgetsBinding
{
    private static readonly WidgetsBinding SharedInstance;
    private readonly List<WidgetsBindingObserver> _observers = [];
    private readonly List<WidgetsBindingObserver> _backGestureObservers = [];

    // The construction of the ambient binding is Dart's `BindingBase()` constructor: `initInstances`
    // inside a "Framework initialization" timeline block that ends with the
    // `Flutter.FrameworkInitialization` event.
    static WidgetsBinding()
    {
        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.StartSync("Framework initialization");
        }

        SharedInstance = new WidgetsFlutterBinding();
        // Only the ambient binding owns the platform channels; a locally constructed one (tests do this)
        // must not hijack them.
        SharedInstance.FocusManager.ListenToApplicationLifecycleChangesIfSupported();
        SharedInstance.InitInstances();
        // `BindingBase()` runs `initServiceExtensions` after `initInstances`; BindingBase's own link of
        // that chain registers first, the widgets binding's (from InitInstances) follows in Dart.
        BindingBase.InitServiceExtensions();

        if (!Constants.KReleaseMode)
        {
            BindingBase.PostEvent("Flutter.FrameworkInitialization", new Dictionary<string, object?>());
            FlutterTimeline.FinishSync();
        }
    }

    public static WidgetsBinding Instance => SharedInstance;

    /// <summary>
    /// Returns the ambient binding after its static construction has completed. A
    /// <see cref="Widgets.FocusManager"/> created by that construction sees <see langword="null"/>
    /// and is connected by the static constructor immediately afterwards.
    /// </summary>
    internal static WidgetsBinding? MaybeInstance => SharedInstance;

    public AppLifecycleState? LifecycleState { get; private set; }

    /// <summary>The currently active set of accessibility features.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsBinding.accessibilityFeatures</c>, which Dart's widgets binding mixes in;
    /// Plumix's bindings are separate singletons, so this reads <see cref="SemanticsBinding"/>'s.
    /// </remarks>
    public AccessibilityFeatures AccessibilityFeatures => SemanticsBinding.Instance.AccessibilityFeatures;

    public void AddObserver(WidgetsBindingObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
    }

    /// <summary>Unregisters the given observer.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.removeObserver</c>: an observer that took part in the current back
    /// gesture stops hearing it, then the first registration is removed.
    /// </remarks>
    public bool RemoveObserver(WidgetsBindingObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _backGestureObservers.Remove(observer);
        return _observers.Remove(observer);
    }

    /// <summary>Called when the platform accessibility features change.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleAccessibilityFeaturesChanged</c> override: the semantics
    /// binding's part first (Dart's <c>super</c> call), then every observer hears
    /// <c>DidChangeAccessibilityFeatures</c>. <see cref="PlatformDispatcher.OnAccessibilityFeaturesChanged"/>
    /// reaches it through <see cref="SemanticsBinding"/>, which <see cref="InitInstances"/> wires.
    /// </remarks>
    public void HandleAccessibilityFeaturesChanged()
    {
        SemanticsBinding.Instance.HandleAccessibilityFeaturesChanged();
        Dispatch(static observer => observer.DidChangeAccessibilityFeatures(), "didChangeAccessibilityFeatures");
    }

    /// <summary>
    /// Called when the platform's view metrics changed: the renderer binding reconfigures every
    /// registered <see cref="RenderView"/>, then the observers hear <c>DidChangeMetrics</c>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleMetricsChanged</c> over <c>RendererBinding</c>'s, installed as
    /// <see cref="PlatformDispatcher.OnMetricsChanged"/>.
    /// </remarks>
    public void HandleMetricsChanged()
    {
        RendererBinding.Instance.HandleMetricsChanged();
        Dispatch(static observer => observer.DidChangeMetrics(), "didChangeMetrics");
    }

    /// <summary>Called when the platform's text scale factor changed.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleTextScaleFactorChanged</c>, installed as
    /// <see cref="PlatformDispatcher.OnTextScaleFactorChanged"/>.
    /// </remarks>
    public void HandleTextScaleFactorChanged()
    {
        RendererBinding.Instance.HandleTextScaleFactorChanged();
        Dispatch(static observer => observer.DidChangeTextScaleFactor(), "didChangeTextScaleFactor");
    }

    /// <summary>Called when the platform brightness changed.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handlePlatformBrightnessChanged</c>, installed as
    /// <see cref="PlatformDispatcher.OnPlatformBrightnessChanged"/>.
    /// </remarks>
    public void HandlePlatformBrightnessChanged()
    {
        RendererBinding.Instance.HandlePlatformBrightnessChanged();
        Dispatch(static observer => observer.DidChangePlatformBrightness(), "didChangePlatformBrightness");
    }

    /// <summary>Called when the system locale changes.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleLocaleChanged</c>, installed as
    /// <see cref="PlatformDispatcher.OnLocaleChanged"/>: notifies the observers with the dispatcher's
    /// <see cref="PlatformDispatcher.Locales"/>.
    /// </remarks>
    public void HandleLocaleChanged()
    {
        DispatchLocalesChanged(PlatformDispatcher.Instance.Locales);
    }

    /// <summary>Notify all the observers that the locale has changed (using <c>DidChangeLocales</c>).</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.dispatchLocalesChanged</c>.</remarks>
    public void DispatchLocalesChanged(IReadOnlyList<Locale>? locales)
    {
        Dispatch(observer => observer.DidChangeLocales(locales), "didChangeLocales");
    }

    /// <summary>
    /// Performs the platform-native locale resolution: the supported locale the platform's own algorithm
    /// picks for the user's preferences, or <see langword="null"/> when the platform does not resolve
    /// locales.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.computePlatformResolvedLocale</c>. The first of
    /// <paramref name="supportedLocales"/> is the platform's default when nothing matches.
    /// </remarks>
    public Locale? ComputePlatformResolvedLocale(IReadOnlyList<Locale> supportedLocales)
    {
        return PlatformDispatcher.Instance.ComputePlatformResolvedLocale(supportedLocales);
    }

    /// <summary>Called when a view gained or lost focus on the platform.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.handleViewFocusChanged</c>, which the platform dispatcher feeds.</remarks>
    public void HandleViewFocusChanged(ViewFocusEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        Dispatch(observer => observer.DidChangeViewFocus(@event), "didChangeViewFocus");
    }

    /// <summary>Called when the operating system notifies the application of a memory pressure situation.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleMemoryPressure</c>, the most derived override the services
    /// binding's <c>memoryPressure</c> system message reaches: the painting binding clears the image cache
    /// and the root asset bundle first (Dart's <c>super</c>), then every observer hears
    /// <c>DidHaveMemoryPressure</c>.
    /// </remarks>
    public void HandleMemoryPressure()
    {
        Painting.PaintingBinding.Instance.HandleMemoryPressure();
        Dispatch(static observer => observer.DidHaveMemoryPressure(), "didHaveMemoryPressure");
    }

    /// <summary>
    /// Calls <paramref name="callback"/> on a snapshot of the observers, reporting a throwing observer
    /// and continuing with the next one, as every Dart <c>WidgetsBinding</c> dispatch loop does.
    /// </summary>
    private void Dispatch(Action<WidgetsBindingObserver> callback, string dartMethodName)
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                callback(observer);
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, dartMethodName);
            }
        }
    }

    private static void ReportObserverError(Exception exception, string dartMethodName)
    {
        FlutterError.ReportError(new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "widgets library",
            context: new ErrorDescription(
                $"while dispatching notifications for WidgetsBindingObserver.{dartMethodName}")));
    }

    /// <summary>Forgets the lifecycle state, as a fresh test binding starts without one.</summary>
    internal void ResetLifecycleStateForTests() => LifecycleState = null;

    /// <summary>Delivers a platform lifecycle change.</summary>
    /// <remarks>
    /// The <c>flutter/lifecycle</c> path: <c>ServicesBinding._handleLifecycleMessage</c> generates the
    /// intermediate states, and for each one Flutter's <c>WidgetsBinding.handleAppLifecycleStateChanged</c>
    /// lets the scheduler enable or disable frames (its <c>super</c> call) before every observer hears it,
    /// then <c>SystemChrome.handleAppLifecycleStateChanged</c> runs. Plumix's bindings are separate
    /// singletons, so the services message handler and the binding method are one entry point here.
    /// </remarks>
    public void HandleAppLifecycleStateChanged(AppLifecycleState state)
    {
        foreach (AppLifecycleState generatedState in GenerateStateTransitions(LifecycleState, state))
        {
            LifecycleState = generatedState;
            SchedulerBinding.Instance.HandleAppLifecycleStateChanged(generatedState);
            Dispatch(observer => observer.DidChangeAppLifecycleState(generatedState), "didChangeAppLifecycleState");
            SystemChrome.HandleAppLifecycleStateChanged(generatedState);
        }
    }

    /// <summary>Dispatches a host status-bar tap to every registered observer.</summary>
    /// <remarks>
    /// Flutter's private <c>WidgetsBinding._handleStatusBarActions</c> for <c>handleScrollToTop</c> on
    /// <c>flutter/status_bar</c>; only iOS and macOS report the gesture.
    /// </remarks>
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
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "widgets library",
                    context: new ErrorDescription("handling status bar action")));
            }
        }
    }

    private Task<object?> HandleStatusBarActions(MethodCall call)
    {
        DebugAssertions.Assert(call.Method == "handleScrollToTop");
        HandleStatusBarTap();
        return Task.FromResult<object?>(null);
    }

    /// <summary>Asks every observer whether the application may exit.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.handleRequestAppExit</c>: every observer is asked (no early return),
    /// and the exit is cancelled if any of them cancels it.
    /// </remarks>
    public async Task<AppExitResponse> HandleRequestAppExit()
    {
        bool didCancel = false;
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                if (await observer.DidRequestAppExit() == AppExitResponse.Cancel)
                {
                    didCancel = true;
                    // Don't early return. For the case where someone is just using the
                    // observer to know when exit happens, we want to call all the
                    // observers, even if we already know we're going to cancel.
                }
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "didRequestAppExit");
            }
        }

        return didCancel ? AppExitResponse.Cancel : AppExitResponse.Exit;
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.initInstances</c>: adds the debug stack filters, installs the locale
    /// callback and the <c>flutter/navigation</c>, <c>flutter/backgesture</c> and <c>flutter/status_bar</c>
    /// handlers, and registers the persistent widget-build callback.
    /// </summary>
    public void InitInstances()
    {
        if (Constants.KDebugMode)
        {
            DebugAddStackFilters();
        }

        // Dart's `SemanticsBinding.initInstances` installs the dispatcher callback that calls the
        // virtual `handleAccessibilityFeaturesChanged`; this binding's override is the most derived.
        SemanticsBinding.Instance.AccessibilityFeaturesChangedOverride = HandleAccessibilityFeaturesChanged;
        EnsureFrameCallback();
        PlatformDispatcher.Instance.OnLocaleChanged = HandleLocaleChanged;
        SystemChannels.Navigation.SetMethodCallHandler(HandleNavigationInvocation);
        SystemChannels.BackGesture.SetMethodCallHandler(HandleBackGestureInvocation);
        SystemChannels.StatusBar.SetMethodCallHandler(HandleStatusBarActions);
        PlatformMenuDelegate = new DefaultPlatformMenuDelegate();
        _windowingOwner = Windowing.CreateDefaultWindowingOwner();
        if (Constants.KDebugMode)
        {
            FlutterErrorDetails.PropertiesTransformers.Add(WidgetInspectorDebug.DebugTransformDebugCreator);
        }

        InitServiceExtensions();
    }

    /// <summary>
    /// Flutter's private <c>WidgetsBinding._debugAddStackFilters</c>: collapses the frames of normal element
    /// mounting in <see cref="FlutterError.DefaultStackFilter"/> output.
    /// </summary>
    /// <remarks>
    /// The six filters follow Dart's order and shape on Plumix's CLR frames. Dart's
    /// <c>ComponentElement.mount</c> and <c>SingleChildRenderObjectElement.mount</c> are each two frames
    /// here: the subclass's <c>OnMount</c> called by <see cref="Element.Mount"/>.
    /// </remarks>
    private static void DebugAddStackFilters()
    {
        const string package = "dotnet:Plumix/Widgets/";
        var elementInflateWidget = new PartialStackFrame(package, "Element", "InflateWidget");
        var elementUpdateChild = new PartialStackFrame(package, "Element", "UpdateChild");
        var elementRebuild = new PartialStackFrame(package, "Element", "Rebuild");
        var elementMount = new PartialStackFrame(package, "Element", "Mount");
        var componentElementPerformRebuild = new PartialStackFrame(package, "ComponentElement", "PerformRebuild");
        var componentElementFirstBuild = new PartialStackFrame(package, "ComponentElement", "FirstBuild");
        var componentElementMount = new PartialStackFrame(package, "ComponentElement", "OnMount");
        var statefulElementFirstBuild = new PartialStackFrame(package, "StatefulElement", "FirstBuild");
        var singleChildMount = new PartialStackFrame(package, "SingleChildRenderObjectElement", "OnMount");
        var statefulElementRebuild = new PartialStackFrame(package, "StatefulElement", "PerformRebuild");

        const string replacementString = "...     Normal element mounting";

        // ComponentElement variations
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [
                elementInflateWidget,
                elementUpdateChild,
                componentElementPerformRebuild,
                elementRebuild,
                componentElementFirstBuild,
                componentElementMount,
                elementMount,
            ],
            replacementString));
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [
                elementUpdateChild,
                componentElementPerformRebuild,
                elementRebuild,
                componentElementFirstBuild,
                componentElementMount,
                elementMount,
            ],
            replacementString));

        // StatefulElement variations
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [
                elementInflateWidget,
                elementUpdateChild,
                componentElementPerformRebuild,
                statefulElementRebuild,
                elementRebuild,
                componentElementFirstBuild,
                statefulElementFirstBuild,
                componentElementMount,
                elementMount,
            ],
            replacementString));
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [
                elementUpdateChild,
                componentElementPerformRebuild,
                statefulElementRebuild,
                elementRebuild,
                componentElementFirstBuild,
                statefulElementFirstBuild,
                componentElementMount,
                elementMount,
            ],
            replacementString));

        // SingleChildRenderObjectElement variations
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [elementInflateWidget, elementUpdateChild, singleChildMount, elementMount],
            replacementString));
        FlutterError.AddDefaultStackFilter(new RepetitiveStackFrameFilter(
            [elementUpdateChild, singleChildMount, elementMount],
            replacementString));
    }

    private async Task<object?> HandleNavigationInvocation(MethodCall call)
    {
        switch (call.Method)
        {
            case "popRoute":
                return await HandlePopRoute();
            case "pushRoute":
                return await HandlePushRoute((string)call.Arguments!);
            case "pushRouteInformation":
                var arguments = (IDictionary<string, object?>)call.Arguments!;
                return await HandlePushRouteInformation(new RouteInformation(
                    new Uri((string)arguments["location"]!, UriKind.RelativeOrAbsolute),
                    arguments.TryGetValue("state", out object? state) ? state : null));
            default:
                // Return false for unhandled method.
                return false;
        }
    }

    /// <summary>The <c>flutter/backgesture</c> handler.</summary>
    /// <remarks>Flutter's private <c>WidgetsBinding._handleBackGestureInvocation</c>.</remarks>
    private async Task<object?> HandleBackGestureInvocation(MethodCall call)
    {
        IReadOnlyDictionary<string, object?>? arguments = call.Arguments switch
        {
            null => null,
            IReadOnlyDictionary<string, object?> map => map,
            System.Collections.IDictionary map => map.Keys.Cast<object>()
                .ToDictionary(static key => key.ToString()!, key => map[key]),
            _ => throw new InvalidCastException("backGesture arguments must be a map."),
        };
        switch (call.Method)
        {
            case "startBackGesture":
                return HandleStartBackGesture(PredictiveBackEvent.FromMap(arguments!));
            case "updateBackGestureProgress":
                if (_backGestureObservers.Count > 0)
                {
                    HandleUpdateBackGestureProgress(PredictiveBackEvent.FromMap(arguments!));
                }

                return null;
            case "commitBackGesture":
                _ = await HandleCommitBackGesture();
                return null;
            case "cancelBackGesture":
                HandleCancelBackGesture();
                return null;
            default:
                throw new MissingPluginException();
        }
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.handlePushRoute</c>: offers a deep-linked route name to the observers
    /// through <see cref="WidgetsBindingObserver.DidPushRouteInformation"/>, awaiting each in registration
    /// order and completing with <see langword="true"/> at the first one that handles it.
    /// </summary>
    public Task<bool> HandlePushRoute(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return HandlePushRouteInformation(
            new RouteInformation(new Uri(route, UriKind.RelativeOrAbsolute)));
    }

    /// <summary>
    /// Flutter's <c>WidgetsBinding.handlePopRoute</c>: offers the pop to every observer in registration order,
    /// awaiting each answer, and completes with <see langword="true"/> at the first one that handles it.
    /// When no observer handles it, the platform is asked to pop the application off its own navigation
    /// stack and the task completes with <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// An observer that throws, synchronously or through its task, is reported and the next one is asked.
    /// </remarks>
    public async Task<bool> HandlePopRoute()
    {
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                if (await observer.DidPopRoute())
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "didPopRoute");
            }
        }

        EventLoop.RunAsync(() => ReportPopFailure(SystemNavigator.Pop()));
        return false;
    }

    private static async Task ReportPopFailure(Task pop)
    {
        try
        {
            await pop;
        }
        catch (Exception exception)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "widgets library",
                context: new ErrorDescription("while popping route")));
        }
    }

    /// <summary>
    /// Flutter's private <c>WidgetsBinding._handlePushRouteInformation</c>: offers the location to every
    /// observer in registration order, awaiting each answer, and completes with <see langword="true"/> at
    /// the first one that handles it.
    /// </summary>
    public async Task<bool> HandlePushRouteInformation(RouteInformation routeInformation)
    {
        ArgumentNullException.ThrowIfNull(routeInformation);
        foreach (WidgetsBindingObserver observer in _observers.ToArray())
        {
            try
            {
                if (await observer.DidPushRouteInformation(routeInformation))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "didPushRouteInformation");
            }
        }

        return false;
    }

    /// <summary>Starts a predictive back gesture.</summary>
    /// <remarks>
    /// Flutter's private <c>WidgetsBinding._handleStartBackGesture</c>: every observer is asked, and each
    /// one that returns <see langword="true"/> follows the rest of the gesture.
    /// </remarks>
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
                ReportObserverError(exception, "handleStartBackGesture");
            }
        }

        return _backGestureObservers.Count > 0;
    }

    /// <summary>Updates the progress of the current predictive back gesture.</summary>
    /// <remarks>Flutter's private <c>WidgetsBinding._handleUpdateBackGestureProgress</c>.</remarks>
    public void HandleUpdateBackGestureProgress(PredictiveBackEvent backEvent)
    {
        ArgumentNullException.ThrowIfNull(backEvent);
        if (_backGestureObservers.Count == 0)
        {
            return;
        }

        foreach (WidgetsBindingObserver observer in _backGestureObservers.ToArray())
        {
            try
            {
                observer.HandleUpdateBackGestureProgress(backEvent);
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "handleUpdateBackGestureProgress");
            }
        }
    }

    /// <summary>Commits the current predictive back gesture.</summary>
    /// <remarks>
    /// Flutter's private <c>WidgetsBinding._handleCommitBackGesture</c>: without a tracking observer the
    /// commit is a pop (<see cref="HandlePopRoute"/>); otherwise every tracking observer commits. The
    /// tracking list is kept until the next gesture starts. Dart's completes with nothing; this one
    /// completes with whether the gesture was handled, which a host that maps the system back button
    /// onto the gesture needs.
    /// </remarks>
    public async Task<bool> HandleCommitBackGesture()
    {
        if (_backGestureObservers.Count == 0)
        {
            // If the predictive back was not handled, then the route should be popped
            // like a normal, non-predictive back. For example, this will happen if a
            // back gesture occurs but no predictive back route transition exists to
            // handle it. The back gesture should still cause normal pop even if it
            // doesn't cause a predictive transition.
            return await HandlePopRoute();
        }

        foreach (WidgetsBindingObserver observer in _backGestureObservers.ToArray())
        {
            try
            {
                observer.HandleCommitBackGesture();
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "handleCommitBackGesture");
            }
        }

        return true;
    }

    /// <summary>Cancels the current predictive back gesture.</summary>
    /// <remarks>
    /// Flutter's private <c>WidgetsBinding._handleCancelBackGesture</c>; the tracking list is kept until
    /// the next gesture starts.
    /// </remarks>
    public void HandleCancelBackGesture()
    {
        foreach (WidgetsBindingObserver observer in _backGestureObservers.ToArray())
        {
            try
            {
                observer.HandleCancelBackGesture();
            }
            catch (Exception exception)
            {
                ReportObserverError(exception, "handleCancelBackGesture");
            }
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
