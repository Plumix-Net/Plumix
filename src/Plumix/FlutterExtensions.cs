using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart (runApp, runWidget)

namespace Plumix;

public static class PlumixExtensions
{
    /// <summary>Runs an application in the platform's implicit view.</summary>
    /// <remarks>Flutter's top-level <c>runApp</c>.</remarks>
    public static void RunApp(Widget application)
    {
        ArgumentNullException.ThrowIfNull(application);
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        RunWidgetCore(binding.WrapWithDefaultView(application), binding, "runApp");
    }

    /// <summary>Runs a caller-owned view tree without adding an implicit <see cref="View"/>.</summary>
    /// <remarks>Flutter's top-level <c>runWidget</c>.</remarks>
    public static void RunWidget(Widget application)
    {
        ArgumentNullException.ThrowIfNull(application);
        WidgetsBinding binding = WidgetsFlutterBinding.EnsureInitialized();
        RunWidgetCore(application, binding, "runWidget");
    }

    // Dart's private `_runWidget`.
    private static void RunWidgetCore(Widget application, WidgetsBinding binding, string debugEntryPoint)
    {
        System.Diagnostics.Debug.Assert(binding.DebugCheckZone(debugEntryPoint));
        binding.ScheduleAttachRootWidget(application);
        SchedulerBinding.Instance.ScheduleWarmUpFrame();
    }
}
