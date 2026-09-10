using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/scheduler/debug.dart

namespace Plumix;

/// <summary>
/// The debug-only switches of Flutter's <c>scheduler/debug.dart</c> that <see cref="Scheduler"/>
/// reads. Every flag defaults to <see langword="false"/>, and setting one has an effect only in
/// debug builds.
/// </summary>
/// <remarks>
/// Dart's library-level variables live on this static class because C# has no top-level fields, the
/// way <see cref="Plumix.Widgets.WidgetsDebug"/> hosts `widgets/debug.dart`. Any flag added here
/// must be considered in <see cref="DebugAssertAllSchedulerVarsUnset"/>.
/// </remarks>
public static class SchedulerDebug
{
    /// <summary>
    /// Dart's <c>debugPrintBeginFrameBanner</c>: print a banner at the beginning of each frame.
    /// </summary>
    /// <remarks>
    /// Frames triggered by <see cref="Scheduler.ScheduleWarmUpFrame"/> show
    /// <c>(warm-up frame)</c> instead of the frame timestamp.
    /// </remarks>
    public static bool DebugPrintBeginFrameBanner { get; set; }

    /// <summary>Dart's <c>debugPrintEndFrameBanner</c>: print a banner at the end of each frame.</summary>
    public static bool DebugPrintEndFrameBanner { get; set; }

    /// <summary>
    /// Dart's <c>debugPrintScheduleFrameStacks</c>: log the stack of every
    /// <see cref="Scheduler.ScheduleFrame"/> and <see cref="Scheduler.ScheduleForcedFrame"/> call.
    /// </summary>
    public static bool DebugPrintScheduleFrameStacks { get; set; }

    /// <summary>
    /// Dart's <c>debugTracePostFrameCallbacks</c>: record a timeline event for every post-frame
    /// callback, labelled with the <c>debugLabel</c> it was registered with.
    /// </summary>
    public static bool DebugTracePostFrameCallbacks { get; set; }

    /// <summary>
    /// Dart's <c>debugAssertAllSchedulerVarsUnset</c>: throws when either banner flag is still set,
    /// so a test cannot leak a debug switch into the next one.
    /// </summary>
    /// <remarks>
    /// Like Dart, this checks only the two banner flags, not
    /// <see cref="DebugPrintScheduleFrameStacks"/> or <see cref="DebugTracePostFrameCallbacks"/>.
    /// </remarks>
    public static bool DebugAssertAllSchedulerVarsUnset(string reason)
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        if (DebugPrintBeginFrameBanner || DebugPrintEndFrameBanner)
        {
            throw new FlutterError(reason);
        }

        return true;
    }
}
