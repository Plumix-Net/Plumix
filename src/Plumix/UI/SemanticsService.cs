using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics_service.dart

/// <summary>
/// Determines the assertiveness level of the accessibility announcement (semantics_event.dart).
/// </summary>
/// <remarks>Only honored by the web engine; it has no effect on other platforms.</remarks>
public enum Assertiveness
{
    /// <summary>The assistive technology will speak changes whenever the user is idle.</summary>
    Polite,

    /// <summary>The assistive technology will interrupt any announcement it is currently making.</summary>
    Assertive,
}

public abstract record SemanticsEvent(string Type, int? NodeId)
{
    public virtual IReadOnlyDictionary<string, object?> GetDataMap() =>
        new Dictionary<string, object?>();

    public Dictionary<string, object?> ToMap()
    {
        var result = new Dictionary<string, object?>
        {
            ["type"] = Type,
            ["data"] = GetDataMap(),
        };
        if (NodeId is int nodeId)
        {
            result["nodeId"] = nodeId;
        }

        return result;
    }
}

/// <summary>
/// An event for a semantic announcement (semantics_event.dart's <c>AnnounceSemanticsEvent</c>).
/// </summary>
/// <remarks>
/// This should be used for announcement that are not seamlessly announced by the system as a result of a UI
/// state change.
/// </remarks>
public sealed record AnnounceSemanticsEvent(
    string Message,
    TextDirection TextDirection,
    int ViewId,
    Assertiveness Assertiveness = Assertiveness.Polite) : SemanticsEvent("announce", NodeId: null)
{
    public override IReadOnlyDictionary<string, object?> GetDataMap()
    {
        var result = new Dictionary<string, object?>
        {
            ["viewId"] = ViewId,
            ["message"] = Message,
            ["textDirection"] = (int)TextDirection,
        };
        if (Assertiveness != Assertiveness.Polite)
        {
            result["assertiveness"] = (int)Assertiveness;
        }

        return result;
    }
}

public sealed record TapSemanticEvent(int? NodeId = null) : SemanticsEvent("tap", NodeId);

public sealed record LongPressSemanticsEvent(int? NodeId = null) : SemanticsEvent("longPress", NodeId);

public sealed record TooltipSemanticEvent(string Message) : SemanticsEvent("tooltip", NodeId: null)
{
    public override IReadOnlyDictionary<string, object?> GetDataMap() =>
        new Dictionary<string, object?> { ["message"] = Message };
}

/// <summary>Allows access to the platform's accessibility services.</summary>
/// <remarks>
/// Events sent by this service are handled by the platform-specific accessibility bridge in the engine. When
/// possible, prefer using mechanisms like <see cref="Semantics"/> to implicitly trigger announcements over
/// using this service.
/// </remarks>
public static class SemanticsService
{
    private static Action<SemanticsEvent>? _semanticsEventRequested;

    public static event Action<SemanticsEvent>? SemanticsEventRequested
    {
        add => _semanticsEventRequested += value;
        remove => _semanticsEventRequested -= value;
    }

    public static void SendEvent(SemanticsEvent semanticsEvent)
    {
        ArgumentNullException.ThrowIfNull(semanticsEvent);
        _semanticsEventRequested?.Invoke(semanticsEvent);
        _ = SystemChannels.Accessibility.Send(semanticsEvent.ToMap());
    }

    public static void Tooltip(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        SendEvent(new TooltipSemanticEvent(message));
    }

    /// <summary>
    /// Sends a semantic announcement for a particular view. One can use <see cref="View.Of"/> to get the
    /// current <see cref="FlutterView"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be used for announcement that are not seamlessly announced by the system as a result of a
    /// UI state change. The assertiveness level is only honored by the web engine. Not all platforms support
    /// announcements; check <see cref="MediaQuery.SupportsAnnounceOf"/> before calling this method.
    /// </para>
    /// <para>
    /// The returned task faults when the accessibility channel fails to deliver the event, so callers attach
    /// Dart's <c>.catchError</c> by awaiting it.
    /// </para>
    /// </remarks>
    public static async Task SendAnnouncement(
        FlutterView view,
        string message,
        TextDirection textDirection,
        Assertiveness assertiveness = Assertiveness.Polite)
    {
        var semanticsEvent = new AnnounceSemanticsEvent(
            message,
            textDirection,
            view.ViewId,
            Assertiveness: assertiveness);
        await SystemChannels.Accessibility.Send(semanticsEvent.ToMap());
    }

    internal static void ResetForTests()
    {
        _semanticsEventRequested = null;
    }
}
