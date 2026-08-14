namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/system_navigator.dart

/// <summary>
/// How the browser/host history should treat the route information the framework reports.
/// Flutter selects this through the <c>SystemNavigator.selectSingleEntryHistory</c> /
/// <c>selectMultiEntryHistory</c> platform messages.
/// </summary>
public enum BrowserHistoryMode
{
    /// <summary>The host keeps a single history entry and replaces it on every report.</summary>
    SingleEntry,

    /// <summary>The host keeps one history entry per reported route information.</summary>
    MultiEntry,
}

/// <summary>The payload of a route-information report the framework sends to the host.</summary>
/// <param name="Uri">The location the application currently displays.</param>
/// <param name="State">Opaque state the host should store alongside the history entry.</param>
/// <param name="Replace">
/// Whether the host should replace the current history entry instead of pushing a new one.
/// </param>
public readonly record struct RouteInformationUpdate(Uri Uri, object? State, bool Replace);

/// <summary>
/// Plumix has no platform message channels, so Flutter's <c>SystemNavigator</c> messages are exposed as
/// host hooks: hosts that own a browser-style history subscribe to the events, and everything else is a
/// no-op. See <c>docs/ai/DIVERGENCES.md</c>.
/// </summary>
public static class SystemNavigator
{
    private static Action<BrowserHistoryMode>? _historyModeSelected;
    private static Action<RouteInformationUpdate>? _routeInformationUpdated;
    private static Action? _popRequested;

    /// <summary>Raised when the framework asks the host to switch its history mode.</summary>
    public static event Action<BrowserHistoryMode>? HistoryModeSelected
    {
        add => _historyModeSelected += value;
        remove => _historyModeSelected -= value;
    }

    /// <summary>Raised when the framework reports new route information to the host.</summary>
    public static event Action<RouteInformationUpdate>? RouteInformationReported
    {
        add => _routeInformationUpdated += value;
        remove => _routeInformationUpdated -= value;
    }

    /// <summary>Raised when the framework asks the host to close the application.</summary>
    public static event Action? PopRequested
    {
        add => _popRequested += value;
        remove => _popRequested -= value;
    }

    /// <summary>
    /// The route the host launched the application with. Flutter reads this from
    /// <c>PlatformDispatcher.defaultRouteName</c>; Plumix has no platform dispatcher, so hosts assign it
    /// here before the first frame.
    /// </summary>
    public static string DefaultRouteName { get; set; } = "/";

    /// <summary>The last history mode the framework selected, or <c>null</c> if it never selected one.</summary>
    public static BrowserHistoryMode? SelectedHistoryMode { get; private set; }

    /// <summary>The last route information the framework reported, or <c>null</c> if it never reported any.</summary>
    public static RouteInformationUpdate? LastReportedRouteInformation { get; private set; }

    public static void SelectSingleEntryHistory()
    {
        SelectHistoryMode(BrowserHistoryMode.SingleEntry);
    }

    public static void SelectMultiEntryHistory()
    {
        SelectHistoryMode(BrowserHistoryMode.MultiEntry);
    }

    public static void RouteInformationUpdated(Uri uri, object? state = null, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var update = new RouteInformationUpdate(uri, state, replace);
        LastReportedRouteInformation = update;
        _routeInformationUpdated?.Invoke(update);
    }

    public static void Pop()
    {
        _popRequested?.Invoke();
    }

    private static void SelectHistoryMode(BrowserHistoryMode mode)
    {
        SelectedHistoryMode = mode;
        _historyModeSelected?.Invoke(mode);
    }

    internal static void ResetForTests()
    {
        _historyModeSelected = null;
        _routeInformationUpdated = null;
        _popRequested = null;
        SelectedHistoryMode = null;
        LastReportedRouteInformation = null;
        DefaultRouteName = "/";
    }
}
