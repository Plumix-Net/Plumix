// Dart parity source: flutter/packages/flutter/lib/src/scheduler/service_extensions.dart

namespace Plumix;

/// <summary>Service extension constants for the scheduler library.</summary>
/// <remarks>
/// These constants will be used when registering service extensions in the framework, and they will
/// also be used by tools and services that call these service extensions.
/// </remarks>
public enum SchedulerServiceExtensions
{
    /// <summary>Name of service extension that, when called, will change the value of
    /// <see cref="Scheduler.TimeDilation"/>, which determines the factor by which to slow down
    /// animations for help in development.</summary>
    TimeDilation,
}

/// <summary>Dart's <c>Enum.name</c> for <see cref="SchedulerServiceExtensions"/>.</summary>
public static class SchedulerServiceExtensionsExtensions
{
    /// <summary>The extension's Dart name, e.g. <c>timeDilation</c>.</summary>
    public static string DartName(this SchedulerServiceExtensions extension) =>
        Foundation.Diagnostics.EnumName(extension);
}
