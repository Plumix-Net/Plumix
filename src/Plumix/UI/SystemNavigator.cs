namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_navigator.dart

/// <summary>Controls specific aspects of the system navigation stack.</summary>
public static class SystemNavigator
{
    /// <summary>
    /// The route the host launched the application with. Flutter reads this from
    /// <c>PlatformDispatcher.defaultRouteName</c>; Plumix has no platform dispatcher, so hosts assign it
    /// here before the first frame. See <c>docs/ai/DIVERGENCES.md</c>.
    /// </summary>
    public static string DefaultRouteName { get; set; } = "/";

    /// <summary>
    /// Informs the platform of whether or not the Flutter framework will handle back events.
    /// </summary>
    /// <remarks>Currently, this is used only on Android.</remarks>
    public static Task SetFrameworkHandlesBack(bool frameworkHandlesBack)
    {
        if (PlatformDefaults.IsWeb)
        {
            return Task.CompletedTask;
        }

        return PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.Android => SystemChannels.Platform.InvokeMethod<object>(
                "SystemNavigator.setFrameworkHandlesBack",
                frameworkHandlesBack),
            _ => Task.CompletedTask,
        };
    }

    /// <summary>Instructs the system navigator to remove this activity from the stack and return to the
    /// previous activity.</summary>
    /// <param name="animated">Whether the pop is animated; ignored on every platform except iOS.</param>
    public static Task Pop(bool? animated = null)
    {
        return SystemChannels.Platform.InvokeMethod<object>("SystemNavigator.pop", animated);
    }

    /// <summary>Selects the single-entry history mode.</summary>
    public static Task SelectSingleEntryHistory()
    {
        return SystemChannels.Navigation.InvokeMethod<object>("selectSingleEntryHistory");
    }

    /// <summary>Selects the multiple-entry history mode.</summary>
    public static Task SelectMultiEntryHistory()
    {
        return SystemChannels.Navigation.InvokeMethod<object>("selectMultiEntryHistory");
    }

    /// <summary>Notifies the platform for a route information change.</summary>
    /// <param name="uri">The location the application currently displays.</param>
    /// <param name="state">Opaque state the host stores alongside the history entry.</param>
    /// <param name="replace">Whether the host replaces the current history entry instead of pushing.</param>
    public static Task RouteInformationUpdated(Uri uri, object? state = null, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return SystemChannels.Navigation.InvokeMethod<object>(
            "routeInformationUpdated",
            new Dictionary<string, object?>
            {
                ["uri"] = uri.ToString(),
                ["state"] = state,
                ["replace"] = replace,
            });
    }

    internal static void ResetForTests()
    {
        DefaultRouteName = "/";
    }
}
