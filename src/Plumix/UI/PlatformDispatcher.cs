using Avalonia.Threading;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source (reference):
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart
// (views + frame + view-focus + launch-route + locale + semantics/accessibility subset, adapted)

/// <summary>
/// The framework's side of the platform: the engine-level requests the framework can make of a
/// host, independent of any particular view.
/// </summary>
/// <remarks>
/// dart:ui's <c>PlatformDispatcher</c>, limited to launch routes, the frame pipeline and the view-focus request
/// the widget layer makes. Flutter forwards <see cref="RequestViewFocusChange"/> to the engine,
/// which moves native focus to the view's window; Plumix has no engine, so the request is published
/// through <see cref="ViewFocusChangeRequested"/> and the host that owns the named view answers it.
/// The reverse direction — the platform telling the framework that a view gained or lost focus —
/// goes through <c>WidgetsBinding.HandleViewFocusChanged</c>, like every other platform message.
/// The frame pipeline follows the same shape: <see cref="ScheduleFrame"/> publishes
/// <see cref="FrameRequested"/> instead of asking an engine for a vsync, and whatever answers it
/// calls <see cref="OnBeginFrame"/> and then <see cref="OnDrawFrame"/>.
/// </remarks>
public sealed class PlatformDispatcher
{
    private PlatformDispatcher()
    {
    }

    /// <summary>The ambient platform dispatcher.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.instance</c>.</remarks>
    public static PlatformDispatcher Instance { get; } = new();

    /// <summary>The platform's implicit view, when the application is running in single-view mode.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.implicitView</c>. A Plumix host installs its view before
    /// <c>RunApp</c> wraps the application; an explicit <c>RunWidget</c> tree does not need one.
    /// </remarks>
    public FlutterView? ImplicitView { get; private set; }

    private readonly OrderedDictionary<int, FlutterView> _views = [];

    /// <summary>The current list of views, including top level platform windows used by the application.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.views</c>. The engine adds a view when the embedder creates one
    /// (the implicit view, a window of the windowing API) and removes it when the embedder disposes of
    /// it; a Plumix host reports both through <see cref="AddView"/> and <see cref="RemoveView"/>.
    /// </remarks>
    public IEnumerable<FlutterView> Views => _views.Values;

    /// <summary>The <see cref="FlutterView"/> with the given id, if one exists, or <see langword="null"/>.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.view(id:)</c>.</remarks>
    public FlutterView? View(int id) => _views.GetValueOrDefault(id);

    /// <summary>
    /// The engine's <c>_addView</c>: registers a view the embedder created and invokes
    /// <see cref="OnMetricsChanged"/>. Adding a registered view again does nothing.
    /// </summary>
    internal void AddView(FlutterView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (_views.TryGetValue(view.ViewId, out FlutterView? registered) && ReferenceEquals(registered, view))
        {
            return;
        }

        _views[view.ViewId] = view;
        OnMetricsChanged?.Invoke();
    }

    /// <summary>
    /// The engine's <c>_removeView</c>: unregisters a view the embedder disposed of and invokes
    /// <see cref="OnMetricsChanged"/>. Removing an unregistered view does nothing.
    /// </summary>
    internal void RemoveView(FlutterView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!_views.TryGetValue(view.ViewId, out FlutterView? registered) || !ReferenceEquals(registered, view))
        {
            return;
        }

        _views.Remove(view.ViewId);
        OnMetricsChanged?.Invoke();
    }

    /// <summary>Platform-wide media data associated with <see cref="ImplicitView"/>.</summary>
    internal MediaQueryData? ImplicitViewPlatformData { get; private set; }

    /// <summary>Installs the view and platform data supplied by the current single-view host.</summary>
    internal void SetImplicitView(FlutterView view, MediaQueryData platformData)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (ImplicitView is { } previous && !ReferenceEquals(previous, view))
        {
            _views.Remove(previous.ViewId);
        }

        ImplicitView = view;
        ImplicitViewPlatformData = platformData;
        _views[view.ViewId] = view;
    }

    /// <summary>Refreshes platform data for the current implicit view.</summary>
    internal void UpdateImplicitViewPlatformData(FlutterView view, MediaQueryData platformData)
    {
        if (ReferenceEquals(ImplicitView, view))
        {
            ImplicitViewPlatformData = platformData;
        }
    }

    /// <summary>Clears the implicit-view contract for an isolated test binding.</summary>
    internal void ClearImplicitViewForTests()
    {
        if (ImplicitView is { } implicitView)
        {
            _views.Remove(implicitView.ViewId);
        }

        ImplicitView = null;
        ImplicitViewPlatformData = null;
    }

    /// <summary>The setting indicating the current brightness mode of the host platform.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.platformBrightness</c>: the implicit view's platform data, or
    /// <see cref="Widgets.PlatformBrightness.Light"/> before a host reported any.
    /// </remarks>
    public PlatformBrightness PlatformBrightness =>
        ImplicitViewPlatformData?.PlatformBrightness ?? PlatformBrightness.Light;

    /// <summary>
    /// A callback invoked whenever the metrics of any view (its size, device pixel ratio, insets)
    /// change.
    /// </summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onMetricsChanged</c>; <c>RendererBinding</c> installs the composed
    /// binding's <c>handleMetricsChanged</c> here, and a host invokes it after it updated a view's metrics.
    /// </remarks>
    public Action? OnMetricsChanged { get; set; }

    /// <summary>A callback invoked whenever the platform's text scale factor changes.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onTextScaleFactorChanged</c>; <c>RendererBinding</c> installs the
    /// composed binding's <c>handleTextScaleFactorChanged</c> here.
    /// </remarks>
    public Action? OnTextScaleFactorChanged { get; set; }

    /// <summary>A callback invoked whenever <see cref="PlatformBrightness"/> changes.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onPlatformBrightnessChanged</c>; <c>RendererBinding</c> installs
    /// the composed binding's <c>handlePlatformBrightnessChanged</c> here.
    /// </remarks>
    public Action? OnPlatformBrightnessChanged { get; set; }

    private IReadOnlyList<Locale> _locales = [];
    private IReadOnlyList<Locale>? _localesTestValue;

    /// <summary>The primary locale selected by the user, or <c>und</c> while <see cref="Locales"/> is empty.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.locale</c>: the first of <see cref="Locales"/>.</remarks>
    public Locale Locale => Locales.Count == 0 ? new Locale("und") : Locales[0];

    /// <summary>The full system-reported supported locales of the device, in the user's order of preference.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.locales</c>, empty until the platform reports any. The engine owns the
    /// value; a Plumix host reports it through <see cref="UpdateLocales"/>.
    /// </remarks>
    public IReadOnlyList<Locale> Locales => _localesTestValue ?? _locales;

    /// <summary>A callback invoked whenever <see cref="Locale"/> changes value.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onLocaleChanged</c>; <c>WidgetsBinding</c> installs its
    /// <c>HandleLocaleChanged</c> here.
    /// </remarks>
    public Action? OnLocaleChanged { get; set; }

    /// <summary>
    /// The platform's own resolution of the supported locales against the user's preferences, which
    /// <see cref="ComputePlatformResolvedLocale"/> asks.
    /// </summary>
    /// <remarks>
    /// Stands in for the engine's native <c>ComputePlatformResolvedLocale</c>, which only Android and iOS
    /// answer. A host whose platform resolves locales installs it; without one the result is
    /// <see langword="null"/>, as the desktop and web engines answer.
    /// </remarks>
    public Func<IReadOnlyList<Locale>, Locale?>? PlatformLocaleResolver { get; set; }

    /// <summary>
    /// Performs the platform-native locale resolution, or returns <see langword="null"/> when the platform
    /// does not resolve locales.
    /// </summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.computePlatformResolvedLocale</c>.</remarks>
    public Locale? ComputePlatformResolvedLocale(IReadOnlyList<Locale> supportedLocales)
    {
        ArgumentNullException.ThrowIfNull(supportedLocales);
        return PlatformLocaleResolver?.Invoke(supportedLocales);
    }

    /// <summary>
    /// Records the platform's preferred locales, as the engine does, and invokes
    /// <see cref="OnLocaleChanged"/> when they changed.
    /// </summary>
    /// <remarks>
    /// The engine-side <c>_updateLocales</c>; hosts call it. An unchanged list (same locales in the same
    /// order) notifies nobody.
    /// </remarks>
    public void UpdateLocales(IReadOnlyList<Locale> locales)
    {
        ArgumentNullException.ThrowIfNull(locales);
        if (locales.SequenceEqual(_locales))
        {
            return;
        }

        _locales = [.. locales];
        OnLocaleChanged?.Invoke();
    }

    /// <summary>
    /// flutter_test's <c>TestPlatformDispatcher.localesTestValue</c> setter (a value) and
    /// <c>clearLocalesTestValue</c> (<see langword="null"/>): both invoke <see cref="OnLocaleChanged"/>.
    /// </summary>
    internal void SetLocalesTestValue(IReadOnlyList<Locale>? value)
    {
        _localesTestValue = value is null ? null : [.. value];
        OnLocaleChanged?.Invoke();
    }

    /// <summary>The route requested by the platform when the application was launched, or <c>/</c>.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.defaultRouteName</c>. A host sets this before mounting the app;
    /// subsequent navigation requests arrive through <c>SystemChannels.Navigation</c>.
    /// Flutter reads this from its engine; the setter supplies that value in an Avalonia host.
    /// </remarks>
    public string DefaultRouteName { get; set; } = "/";

    /// <summary>
    /// Raised by <see cref="RequestViewFocusChange"/>. A host that owns the view named by the event
    /// gives it native focus; a test harness can record the requests.
    /// </summary>
    public event Action<ViewFocusEvent>? ViewFocusChangeRequested;

    /// <summary>Requests that the platform move focus to, or away from, the view <paramref name="viewId"/>.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.requestViewFocusChange</c>.</remarks>
    public void RequestViewFocusChange(int viewId, ViewFocusState state, ViewFocusDirection direction)
    {
        ViewFocusChangeRequested?.Invoke(new ViewFocusEvent(viewId, state, direction));
    }

    /// <summary>
    /// A callback invoked when any view begins a frame, with the raw timestamp of the frame.
    /// </summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onBeginFrame</c>.
    /// <c>SchedulerBinding.EnsureFrameCallbacksRegistered</c>
    /// installs the framework's handler here, and only when the slot is still null.
    /// </remarks>
    public Action<TimeSpan>? OnBeginFrame { get; set; }

    /// <summary>Additional data available on each flutter frame.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.frameData</c>. The engine fills it before each frame; no Plumix
    /// host reports a frame number yet, so it keeps the engine's initial value.
    /// </remarks>
    public FrameData FrameData { get; internal set; } = new(frameNumber: -1);

    /// <summary>A callback invoked for each frame after <see cref="OnBeginFrame"/> has completed.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.onDrawFrame</c>.</remarks>
    public Action? OnDrawFrame { get; set; }

    /// <summary>
    /// A callback that reports the <see cref="FrameTiming"/> of recently rasterized frames.
    /// </summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onReportTimings</c>. It is a single slot, not an event:
    /// <c>SchedulerBinding.Instance.AddTimingsCallback</c> owns it and multiplexes to the callbacks it holds.
    /// </remarks>
    public Action<IReadOnlyList<FrameTiming>>? OnReportTimings { get; set; }

    /// <summary>A callback that is invoked for each key data that is sent by the platform.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onKeyData</c>; <c>ServicesBinding</c> installs
    /// <c>KeyEventManager.HandleKeyData</c> here. The engine delivers key data over the
    /// <c>flutter/keydata</c> channel buffer and answers the platform with the callback's result; a
    /// Plumix host calls <see cref="DispatchKeyData"/>, which returns that result directly.
    /// </remarks>
    public KeyDataCallback? OnKeyData { get; set; }

    /// <summary>
    /// Delivers a <see cref="KeyData"/> packet to <see cref="OnKeyData"/>, as the engine's
    /// <c>_keyDataListener</c> does; false when no callback is installed.
    /// </summary>
    public bool DispatchKeyData(KeyData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return OnKeyData?.Invoke(data) ?? false;
    }

    /// <summary>Whether the user has requested that semantics be enabled for the application.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.semanticsEnabled</c>: typically set while an assistive technology
    /// is running. The engine owns the value; a Plumix host reports it through
    /// <see cref="UpdateSemanticsEnabled"/>.
    /// </remarks>
    public bool SemanticsEnabled { get; private set; }

    /// <summary>A callback invoked when <see cref="SemanticsEnabled"/> changes.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onSemanticsEnabledChanged</c>; owned by <c>SemanticsBinding</c>.
    /// </remarks>
    public Action? OnSemanticsEnabledChanged { get; set; }

    /// <summary>A callback invoked when the platform asks the framework to perform a semantics action.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onSemanticsActionEvent</c>; owned by <c>SemanticsBinding</c>.
    /// </remarks>
    public Action<SemanticsActionEvent>? OnSemanticsActionEvent { get; set; }

    /// <summary>The last value passed to <see cref="SetSemanticsTreeEnabled"/>.</summary>
    /// <remarks>Plumix-only observability; dart:ui hands the value straight to the engine.</remarks>
    public bool SemanticsTreeEnabled { get; private set; }

    /// <summary>Tells the platform whether the framework is producing a semantics tree.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.setSemanticsTreeEnabled</c>.</remarks>
    public void SetSemanticsTreeEnabled(bool enabled)
    {
        SemanticsTreeEnabled = enabled;
    }

    /// <summary>
    /// Records the platform's accessibility state, as the engine does, and invokes
    /// <see cref="OnSemanticsEnabledChanged"/> when it changed.
    /// </summary>
    /// <remarks>The engine-side write of <c>semanticsEnabled</c>; hosts and tests call it.</remarks>
    public void UpdateSemanticsEnabled(bool enabled)
    {
        if (SemanticsEnabled == enabled)
        {
            return;
        }

        SemanticsEnabled = enabled;
        OnSemanticsEnabledChanged?.Invoke();
    }

    private AccessibilityFeatures _accessibilityFeatures = new(0);
    private AccessibilityFeatures? _accessibilityFeaturesTestValue;

    /// <summary>Additional accessibility features that may be enabled by the platform.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.accessibilityFeatures</c>. The engine owns the value; a Plumix
    /// host reports it through <see cref="UpdateAccessibilityFeatures"/>. Before any report every
    /// flag is clear, so the inverted ones (<see cref="AccessibilityFeatures.SupportsAnnounce"/>,
    /// <see cref="AccessibilityFeatures.AutoPlayAnimatedImages"/>,
    /// <see cref="AccessibilityFeatures.AutoPlayVideos"/>) read <see langword="true"/>.
    /// </remarks>
    public AccessibilityFeatures AccessibilityFeatures => _accessibilityFeaturesTestValue ?? _accessibilityFeatures;

    /// <summary>A callback invoked when the value of <see cref="AccessibilityFeatures"/> changes.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onAccessibilityFeaturesChanged</c>; owned by
    /// <c>SemanticsBinding</c>.
    /// </remarks>
    public Action? OnAccessibilityFeaturesChanged { get; set; }

    /// <summary>
    /// Records the platform's accessibility feature bitfield, as the engine does, and invokes
    /// <see cref="OnAccessibilityFeaturesChanged"/> when it changed.
    /// </summary>
    /// <remarks>
    /// The engine-side <c>_updateAccessibilityFeatures(int values)</c>; hosts call it. The bits follow
    /// the embedder's <c>FlutterAccessibilityFeature</c> layout, by bit number: 0 accessible
    /// navigation, 1 invert colors, 2 disable animations, 3 bold text, 4 reduce motion, 5 high
    /// contrast, 6 on/off switch labels, 7 no announce, 8 no auto-play animated images, 9 no auto-play
    /// videos, 10 deterministic cursor.
    /// </remarks>
    public void UpdateAccessibilityFeatures(int values)
    {
        var newFeatures = new AccessibilityFeatures(values);
        if (newFeatures == _accessibilityFeatures)
        {
            return;
        }

        _accessibilityFeatures = newFeatures;
        OnAccessibilityFeaturesChanged?.Invoke();
    }

    /// <summary>
    /// flutter_test's <c>TestPlatformDispatcher.accessibilityFeaturesTestValue</c> setter (a value) and
    /// <c>clearAccessibilityFeaturesTestValue</c> (<see langword="null"/>): both invoke
    /// <see cref="OnAccessibilityFeaturesChanged"/>.
    /// </summary>
    /// <remarks>
    /// flutter_test wraps the dispatcher in a <c>TestPlatformDispatcher</c> that every view reports
    /// as its <c>platformDispatcher</c>. Plumix's views all report <see cref="Instance"/>, so the
    /// test value lives here, behind an internal entry point the test harness uses.
    /// </remarks>
    internal void SetAccessibilityFeaturesTestValue(AccessibilityFeatures? value)
    {
        _accessibilityFeaturesTestValue = value;
        OnAccessibilityFeaturesChanged?.Invoke();
    }

    /// <summary>
    /// The bitfield last passed to <see cref="UpdateAccessibilityFeatures"/>, ignoring any test value.
    /// </summary>
    internal int ReportedAccessibilityFeatures => _accessibilityFeatures.Index;

    /// <summary>Whether a test value currently replaces <see cref="AccessibilityFeatures"/>.</summary>
    internal bool HasAccessibilityFeaturesTestValue => _accessibilityFeaturesTestValue is not null;

    /// <summary>Whether the user has requested that obscured text fields briefly show the most
    /// recently typed character.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.brieflyShowPassword</c>, <see langword="true"/> until the
    /// platform's user settings say otherwise (Android's "Show passwords" setting).
    /// </remarks>
    public bool BrieflyShowPassword { get; private set; } = true;

    /// <summary>Records the <c>brieflyShowPassword</c> user setting, as the engine's
    /// <c>_updateUserSettingsData</c> does when the settings message carries the key.</summary>
    /// <remarks>Like the engine, a change of this key alone notifies nobody; the next reader sees
    /// it. Hosts and tests call it.</remarks>
    public void UpdateBrieflyShowPassword(bool brieflyShowPassword)
    {
        BrieflyShowPassword = brieflyShowPassword;
    }

    /// <summary>Delivers a platform semantics action to <see cref="OnSemanticsActionEvent"/>.</summary>
    /// <remarks>The engine-side dispatch of <c>onSemanticsActionEvent</c>; hosts and tests call it.</remarks>
    public void DispatchSemanticsActionEvent(SemanticsActionEvent action)
    {
        ArgumentNullException.ThrowIfNull(action);
        OnSemanticsActionEvent?.Invoke(action);
    }

    /// <summary>Engine-style query for whether a hit-test path contains a native platform view.</summary>
    public Func<HitTestRequest, HitTestResponse>? OnHitTest { get; set; }

    /// <summary>Raised by <see cref="ScheduleFrame"/>; whoever drives frames arms itself here.</summary>
    /// <remarks>
    /// Flutter asks the engine for a vsync signal. Plumix has no engine, so <see cref="FrameClock"/>
    /// answers the request, and the request is also published for a host that drives frames itself.
    /// </remarks>
    public event Action? FrameRequested;

    /// <summary>Raised by <see cref="RequestDartPerformanceMode"/>.</summary>
    public event Action<DartPerformanceMode>? PerformanceModeRequested;

    /// <summary>The last mode passed to <see cref="RequestDartPerformanceMode"/>, if any.</summary>
    /// <remarks>Plumix-only observability; dart:ui hands the request straight to the VM.</remarks>
    public DartPerformanceMode? LastRequestedPerformanceMode { get; private set; }

    /// <summary>
    /// Runs <paramref name="callback"/> on the next turn of the event loop, after the microtasks
    /// queued by the current turn have drained.
    /// </summary>
    /// <remarks>
    /// dart:async's <c>Timer.run</c>, which dart:ui's <c>scheduleWarmUpFrame</c> and Flutter's
    /// <c>SchedulerBinding._ensureEventLoopCallback</c> both use. Plumix has no Dart event loop, so
    /// the default posts to Avalonia's dispatcher at background priority — below the
    /// <c>DispatcherPriority.Send</c> that <c>EventLoop.ScheduleMicrotask</c> drains at, which is
    /// what keeps the microtask-before-next-turn ordering. A test harness replaces this to drive the
    /// turns itself.
    /// </remarks>
    public Action<Action> TimerRun { get; set; } =
        static callback => Dispatcher.UIThread.Post(callback, DispatcherPriority.Background);

    /// <summary>Requests that the operating system schedule a new frame.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.scheduleFrame</c>.</remarks>
    public void ScheduleFrame()
    {
        FrameClock.RequestFrame();
        FrameRequested?.Invoke();
    }

    /// <summary>
    /// Schedules a frame as soon as possible, without waiting for the platform's vsync signal.
    /// </summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.scheduleWarmUpFrame</c>: two separate event-loop turns, so
    /// the microtasks queued during <paramref name="beginFrame"/> drain before
    /// <paramref name="drawFrame"/> runs.
    /// </remarks>
    public void ScheduleWarmUpFrame(Action beginFrame, Action drawFrame)
    {
        ArgumentNullException.ThrowIfNull(beginFrame);
        ArgumentNullException.ThrowIfNull(drawFrame);

        // Each timer callback ends its event-loop turn, and Dart drains the microtask queue there.
        TimerRun(() =>
        {
            beginFrame();
            EventLoop.FlushMicrotasks();
        });
        TimerRun(() =>
        {
            EventLoop.FlushMicrotasks();
            drawFrame();
            EventLoop.FlushMicrotasks();
        });
    }

    /// <summary>Requests a performance mode from the Dart runtime.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.requestDartPerformanceMode</c>.</remarks>
    public void RequestDartPerformanceMode(DartPerformanceMode mode)
    {
        LastRequestedPerformanceMode = mode;
        PerformanceModeRequested?.Invoke(mode);
    }

    /// <summary>Reports the timings of recently rasterized frames to <see cref="OnReportTimings"/>.</summary>
    /// <remarks>
    /// The engine calls <c>onReportTimings</c> directly; Plumix hosts have no engine, so this is the
    /// entry point they use.
    /// </remarks>
    public void ReportTimings(IReadOnlyList<FrameTiming> timings)
    {
        ArgumentNullException.ThrowIfNull(timings);
        OnReportTimings?.Invoke(timings);
    }
}

/// <summary>Data for a frame provided by the engine.</summary>
/// <remarks>dart:ui's <c>FrameData</c>.</remarks>
public sealed class FrameData
{
    internal FrameData(int frameNumber)
    {
        FrameNumber = frameNumber;
    }

    /// <summary>The number of the current frame, or -1 when the engine does not report one.</summary>
    /// <remarks>Incremented by the engine on each frame; it can be used to identify a frame.</remarks>
    public int FrameNumber { get; }
}
