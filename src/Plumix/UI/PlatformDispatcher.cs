using Avalonia.Threading;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.UI;

public delegate void PlatformMessageCallback(
    string name, ByteData? data, PlatformMessageResponseCallback callback);

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

    public ChannelBuffers ChannelBuffers => ServicesBinding.Instance.ChannelBuffers;

    private PlatformMessageCallback? _onPlatformMessage;
    private ExecutionContext? _platformMessageContext;

    [Obsolete("Use ChannelBuffers.SetListener instead.")]
    public PlatformMessageCallback? OnPlatformMessage
    {
        get => _onPlatformMessage;
        set
        {
            _onPlatformMessage = value;
            _platformMessageContext = ExecutionContext.Capture();
        }
    }

    /// <summary>The embedder's channel-listening notifications.</summary>
    public event Action<string, bool>? ChannelUpdate;

    public void SendChannelUpdate(string name, bool listening) => ChannelUpdate?.Invoke(name, listening);

    /// <summary>Delivers an engine message, including the channel-buffer control protocol.</summary>
    public void DispatchPlatformMessage(
        string name, ByteData? data, PlatformMessageResponseCallback callback)
    {
        if (name == ChannelBuffers.KControlChannelName)
        {
            try
            {
                ChannelBuffers.HandleMessage(data!);
            }
            finally
            {
                callback(null);
            }
        }
        else if (_onPlatformMessage is { } handler)
        {
            if (_platformMessageContext is { } context)
            {
                ExecutionContext.Run(context, _ => handler(name, data, callback), null);
            }
            else
            {
                handler(name, data, callback);
            }
        }
        else
        {
            ChannelBuffers.Push(name, data, callback);
        }
    }

    /// <summary>Sends a framework message through the in-process host transport.</summary>
    public void SendPlatformMessage(
        string name, ByteData? data, PlatformMessageResponseCallback? callback)
        => SendPlatformMessage(name, data, callback, ServicesBinding.Instance.DefaultBinaryMessenger,
            exception => FlutterError.ReportError(
                new FlutterErrorDetails(exception: exception, stack: exception.StackTrace)));

    internal void SendPlatformMessage(
        string name, ByteData? data, PlatformMessageResponseCallback? callback,
        BinaryMessenger messenger, Action<Exception> onError)
    {
        ExecutionContext? context = ExecutionContext.Capture();
        Task<ByteData?> result = SendPlatformMessageAsync(name, data, messenger);
        EventLoop.RunAsync(async () =>
        {
            ByteData? reply;
            try
            {
                reply = await result;
            }
            catch (Exception exception)
            {
                onError(exception);
                return;
            }
            if (callback is null)
            {
                return;
            }

            try
            {
                if (context is null)
                {
                    callback(reply);
                }
                else
                {
                    ExecutionContext.Run(context, _ => callback(reply), null);
                }
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(exception: exception, stack: exception.StackTrace));
            }
        });
    }

    internal Task<ByteData?> SendPlatformMessageAsync(string name, ByteData? data, BinaryMessenger messenger) =>
        (messenger is PlatformBinaryMessenger platform
            ? platform.SendToPlatform(name, data)
            : messenger.Send(name, data)) ?? Task.FromResult<ByteData?>(null);

    private string _initialLifecycleState = string.Empty;
    private bool _initialLifecycleStateAccessed;

    public string InitialLifecycleState
    {
        get
        {
            _initialLifecycleStateAccessed = true;
            return _initialLifecycleState;
        }
    }

    public void UpdateInitialLifecycleState(string state)
    {
        if (!_initialLifecycleStateAccessed)
        {
            _initialLifecycleState = state;
        }
    }

    internal void ResetInitialLifecycleStateForTests(string state, bool accessed = false)
    {
        _initialLifecycleState = state;
        _initialLifecycleStateAccessed = accessed;
    }

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

    /// <summary>The most recently committed locale reported by a Localizations scope.</summary>
    public Locale? ApplicationLocale { get; private set; }

    /// <summary>Lets the host consume dart:ui's application-locale notification.</summary>
    public event Action<Locale>? ApplicationLocaleChanged;

    /// <summary>Reports a resolved application locale to the platform.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.setApplicationLocale</c>.</remarks>
    public void SetApplicationLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ApplicationLocale = locale;
        ApplicationLocaleChanged?.Invoke(locale);
    }

    internal void ResetApplicationLocaleForTests(Locale? locale) => ApplicationLocale = locale;

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

    private bool _supportsShowingSystemContextMenu;
    private bool? _supportsShowingSystemContextMenuTestValue;

    /// <summary>Whether the platform can show its own text-selection context menu.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.supportsShowingSystemContextMenu</c>, <see langword="false"/>
    /// until the platform's user settings say otherwise (iOS 16 and above).
    /// </remarks>
    public bool SupportsShowingSystemContextMenu =>
        _supportsShowingSystemContextMenuTestValue ?? _supportsShowingSystemContextMenu;

    /// <summary>Records the <c>supportsShowingSystemContextMenu</c> user setting, as the engine's
    /// <c>_updateUserSettingsData</c> does: a settings message without the key resets it to
    /// <see langword="false"/>.</summary>
    /// <remarks>The key is read with the rest of the user settings, so it notifies through the
    /// platform-configuration change the settings message causes, not on its own. Hosts and tests
    /// call it.</remarks>
    public void UpdateSupportsShowingSystemContextMenu(bool? supportsShowingSystemContextMenu)
    {
        _supportsShowingSystemContextMenu = supportsShowingSystemContextMenu ?? false;
    }

    /// <summary>
    /// flutter_test's <c>TestPlatformDispatcher.supportsShowingSystemContextMenu</c> setter (a value)
    /// and <c>resetSupportsShowingSystemContextMenu</c> (<see langword="null"/>); neither notifies.
    /// </summary>
    internal void SetSupportsShowingSystemContextMenuTestValue(bool? value)
    {
        _supportsShowingSystemContextMenuTestValue = value;
    }

    /// <summary>Whether a test value currently replaces <see cref="SupportsShowingSystemContextMenu"/>.</summary>
    internal bool HasSupportsShowingSystemContextMenuTestValue =>
        _supportsShowingSystemContextMenuTestValue is not null;

    private readonly TextSpacingOverride _lineHeightScaleFactorOverride = new();
    private readonly TextSpacingOverride _letterSpacingOverride = new();
    private readonly TextSpacingOverride _wordSpacingOverride = new();
    private readonly TextSpacingOverride _paragraphSpacingOverride = new();

    /// <summary>
    /// The system-suggested height of the text, as a multiple of the font size; it overrides the
    /// existing value of <c>TextStyle.Height</c> and <c>StrutStyle.Height</c>. <see langword="null"/>
    /// when no override has been set by the system. If this value changes,
    /// <see cref="OnMetricsChanged"/> will be called.
    /// </summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.lineHeightScaleFactorOverride</c>.</remarks>
    public double? LineHeightScaleFactorOverride => _lineHeightScaleFactorOverride.Value;

    /// <summary>
    /// The system-suggested additional space (in logical pixels) to add between each letter; a
    /// negative value brings the letters closer. It overrides <c>TextStyle.LetterSpacing</c>.
    /// <see langword="null"/> when no override has been set by the system. If this value changes,
    /// <see cref="OnMetricsChanged"/> will be called.
    /// </summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.letterSpacingOverride</c>.</remarks>
    public double? LetterSpacingOverride => _letterSpacingOverride.Value;

    /// <summary>
    /// The system-suggested additional space (in logical pixels) to add between each sequence of
    /// white-space (i.e. between each word). It overrides <c>TextStyle.WordSpacing</c>.
    /// <see langword="null"/> when no override has been set by the system. If this value changes,
    /// <see cref="OnMetricsChanged"/> will be called.
    /// </summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.wordSpacingOverride</c>.</remarks>
    public double? WordSpacingOverride => _wordSpacingOverride.Value;

    /// <summary>
    /// The system-suggested additional space (in logical pixels) to add following each paragraph in
    /// text. <see langword="null"/> when no override has been set by the system. If this value
    /// changes, <see cref="OnMetricsChanged"/> will be called.
    /// </summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.paragraphSpacingOverride</c>.</remarks>
    public double? ParagraphSpacingOverride => _paragraphSpacingOverride.Value;

    /// <summary>
    /// Records the platform's text spacing overrides and invokes <see cref="OnMetricsChanged"/> when
    /// any of them changed.
    /// </summary>
    /// <remarks>
    /// The web engine's typography observer (<c>_addTypographySettingsObserver</c>), the only engine
    /// that sets these values: it calls <c>_updateLineHeightScaleFactorOverride</c> and its three
    /// siblings, then <c>invokeOnMetricsChanged</c> when one of them changed. The native engines never
    /// set them, so the values stay <see langword="null"/> unless a host calls this.
    /// </remarks>
    public void UpdateTextSpacingOverrides(
        double? lineHeightScaleFactorOverride,
        double? letterSpacingOverride,
        double? wordSpacingOverride,
        double? paragraphSpacingOverride)
    {
        bool lineHeightChanged = _lineHeightScaleFactorOverride.Update(lineHeightScaleFactorOverride);
        bool letterSpacingChanged = _letterSpacingOverride.Update(letterSpacingOverride);
        bool wordSpacingChanged = _wordSpacingOverride.Update(wordSpacingOverride);
        bool paragraphSpacingChanged = _paragraphSpacingOverride.Update(paragraphSpacingOverride);
        if (lineHeightChanged || letterSpacingChanged || wordSpacingChanged || paragraphSpacingChanged)
        {
            OnMetricsChanged?.Invoke();
        }
    }

    /// <summary>
    /// flutter_test's <c>TestPlatformDispatcher.lineHeightScaleFactorOverrideTestValue</c> setter: a
    /// <see langword="null"/> value forces the override to read <see langword="null"/>. Invokes
    /// <see cref="OnMetricsChanged"/>.
    /// </summary>
    internal void SetLineHeightScaleFactorOverrideTestValue(double? value) =>
        SetTestValue(_lineHeightScaleFactorOverride, value);

    /// <summary>flutter_test's <c>clearLineHeightScaleFactorOverrideTestValue</c>.</summary>
    internal void ClearLineHeightScaleFactorOverrideTestValue() => ClearTestValue(_lineHeightScaleFactorOverride);

    /// <summary>flutter_test's <c>TestPlatformDispatcher.letterSpacingOverrideTestValue</c> setter.</summary>
    internal void SetLetterSpacingOverrideTestValue(double? value) => SetTestValue(_letterSpacingOverride, value);

    /// <summary>flutter_test's <c>clearLetterSpacingOverrideTestValue</c>.</summary>
    internal void ClearLetterSpacingOverrideTestValue() => ClearTestValue(_letterSpacingOverride);

    /// <summary>flutter_test's <c>TestPlatformDispatcher.wordSpacingOverrideTestValue</c> setter.</summary>
    internal void SetWordSpacingOverrideTestValue(double? value) => SetTestValue(_wordSpacingOverride, value);

    /// <summary>flutter_test's <c>clearWordSpacingOverrideTestValue</c>.</summary>
    internal void ClearWordSpacingOverrideTestValue() => ClearTestValue(_wordSpacingOverride);

    /// <summary>flutter_test's <c>TestPlatformDispatcher.paragraphSpacingOverrideTestValue</c> setter.</summary>
    internal void SetParagraphSpacingOverrideTestValue(double? value) =>
        SetTestValue(_paragraphSpacingOverride, value);

    /// <summary>flutter_test's <c>clearParagraphSpacingOverrideTestValue</c>.</summary>
    internal void ClearParagraphSpacingOverrideTestValue() => ClearTestValue(_paragraphSpacingOverride);

    /// <summary>Whether any text spacing override currently carries a test value.</summary>
    internal bool HasTextSpacingOverrideTestValues =>
        _lineHeightScaleFactorOverride.HasTestValue
        || _letterSpacingOverride.HasTestValue
        || _wordSpacingOverride.HasTestValue
        || _paragraphSpacingOverride.HasTestValue;

    private void SetTestValue(TextSpacingOverride spacingOverride, double? value)
    {
        spacingOverride.TestValue = value;
        if (value is null)
        {
            spacingOverride.ForceNull = true;
        }

        OnMetricsChanged?.Invoke();
    }

    private void ClearTestValue(TextSpacingOverride spacingOverride)
    {
        spacingOverride.TestValue = null;
        spacingOverride.ForceNull = false;
        OnMetricsChanged?.Invoke();
    }

    /// <summary>One text spacing override: the platform's value and flutter_test's test value over it.</summary>
    private sealed class TextSpacingOverride
    {
        private double? _platformValue;

        public double? TestValue { get; set; }

        public bool ForceNull { get; set; }

        public bool HasTestValue => TestValue is not null || ForceNull;

        public double? Value => ForceNull ? null : TestValue ?? _platformValue;

        public bool Update(double? value)
        {
            if (_platformValue == value)
            {
                return false;
            }

            _platformValue = value;
            return true;
        }
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

    /// <summary>A callback that is invoked when pointer data is available.</summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onPointerDataPacket</c>; <c>GestureBinding</c> installs its packet
    /// handler here, and an embedder (or a test) reports physical-pixel pointer data by invoking it.
    /// </remarks>
    public Action<PointerDataPacket>? OnPointerDataPacket { get; set; }

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
    /// the default posts to Avalonia's dispatcher at <see cref="EventLoopTaskPriority"/>. A test
    /// harness replaces this to drive the turns itself.
    /// </remarks>
    public Action<Action> TimerRun { get; set; } = DefaultTimerRun;

    /// <summary>
    /// The dispatcher priority of an event-loop task (<see cref="TimerRun"/>): below the
    /// <c>DispatcherPriority.Send</c> that <c>EventLoop.ScheduleMicrotask</c> drains at, which keeps
    /// the microtask-before-next-turn ordering, and above <c>DispatcherPriority.Input</c>.
    /// </summary>
    /// <remarks>
    /// A Dart timer is an ordinary event-loop task, never idle work. Avalonia runs jobs at or below
    /// <c>Input</c> only when the platform reports no pending input; Android's dispatcher runs them
    /// from the looper's idle handler, which never fires while an animation keeps the main thread
    /// busy. At background priority the warm-up frame's draw half never ran behind an indeterminate
    /// progress indicator, and every vsync frame was dropped while that warm-up frame was pending.
    /// </remarks>
    internal static readonly DispatcherPriority EventLoopTaskPriority = DispatcherPriority.Default;

    /// <summary>Posts a job to Avalonia's UI-thread dispatcher; replaced by tests.</summary>
    internal static Action<Action, DispatcherPriority> PostToDispatcher { get; set; } =
        static (callback, priority) => Dispatcher.UIThread.Post(callback, priority);

    internal static void DefaultTimerRun(Action callback) => PostToDispatcher(callback, EventLoopTaskPriority);

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
