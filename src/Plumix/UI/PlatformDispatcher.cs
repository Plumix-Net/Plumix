using Avalonia.Threading;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source (reference):
// flutter/bin/cache/pkg/sky_engine/lib/ui/platform_dispatcher.dart (frame + view-focus + launch-route subset, adapted)

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

    /// <summary>Platform-wide media data associated with <see cref="ImplicitView"/>.</summary>
    internal MediaQueryData? ImplicitViewPlatformData { get; private set; }

    /// <summary>Installs the view and platform data supplied by the current single-view host.</summary>
    internal void SetImplicitView(FlutterView view, MediaQueryData platformData)
    {
        ArgumentNullException.ThrowIfNull(view);
        ImplicitView = view;
        ImplicitViewPlatformData = platformData;
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
        ImplicitView = null;
        ImplicitViewPlatformData = null;
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
    /// dart:ui's <c>PlatformDispatcher.onBeginFrame</c>. <c>Scheduler.EnsureFrameCallbacksRegistered</c>
    /// installs the framework's handler here, and only when the slot is still null.
    /// </remarks>
    public Action<TimeSpan>? OnBeginFrame { get; set; }

    /// <summary>A callback invoked for each frame after <see cref="OnBeginFrame"/> has completed.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.onDrawFrame</c>.</remarks>
    public Action? OnDrawFrame { get; set; }

    /// <summary>
    /// A callback that reports the <see cref="FrameTiming"/> of recently rasterized frames.
    /// </summary>
    /// <remarks>
    /// dart:ui's <c>PlatformDispatcher.onReportTimings</c>. It is a single slot, not an event:
    /// <c>Scheduler.AddTimingsCallback</c> owns it and multiplexes to the callbacks it holds.
    /// </remarks>
    public Action<IReadOnlyList<FrameTiming>>? OnReportTimings { get; set; }

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
    /// Flutter asks the engine for a vsync signal. Plumix has no engine, so the request is published
    /// and <see cref="Scheduler"/> (or a host that drives frames itself) answers it.
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
    /// <c>DispatcherPriority.Send</c> that <c>Scheduler.ScheduleMicrotask</c> drains at, which is
    /// what keeps the microtask-before-next-turn ordering. A test harness replaces this to drive the
    /// turns itself.
    /// </remarks>
    public Action<Action> TimerRun { get; set; } =
        static callback => Dispatcher.UIThread.Post(callback, DispatcherPriority.Background);

    /// <summary>Requests that the operating system schedule a new frame.</summary>
    /// <remarks>dart:ui's <c>PlatformDispatcher.scheduleFrame</c>.</remarks>
    public void ScheduleFrame() => FrameRequested?.Invoke();

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

        TimerRun(beginFrame);
        TimerRun(() =>
        {
            Scheduler.FlushMicrotasks();
            drawFrame();
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
