namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics_service.dart

public sealed record SemanticsAnnouncement(
    int ViewId,
    string Message,
    TextDirection TextDirection);

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

public sealed record TapSemanticEvent(int? NodeId = null) : SemanticsEvent("tap", NodeId);

public sealed record LongPressSemanticsEvent(int? NodeId = null) : SemanticsEvent("longPress", NodeId);

public sealed record TooltipSemanticEvent(string Message) : SemanticsEvent("tooltip", NodeId: null)
{
    public override IReadOnlyDictionary<string, object?> GetDataMap() =>
        new Dictionary<string, object?> { ["message"] = Message };
}

public static class SemanticsService
{
    private static Action<SemanticsAnnouncement>? _announcementRequested;
    private static Action<Exception>? _announcementFailed;
    private static Action<SemanticsEvent>? _semanticsEventRequested;

    public static Func<SemanticsAnnouncement, Task>? PlatformHandler { get; set; }

    public static event Action<SemanticsAnnouncement>? AnnouncementRequested
    {
        add => _announcementRequested += value;
        remove => _announcementRequested -= value;
    }

    public static event Action<Exception>? AnnouncementFailed
    {
        add => _announcementFailed += value;
        remove => _announcementFailed -= value;
    }

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

    public static async Task SendAnnouncement(
        int viewId,
        string message,
        TextDirection textDirection)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        var announcement = new SemanticsAnnouncement(viewId, message, textDirection);
        try
        {
            _announcementRequested?.Invoke(announcement);
            if (PlatformHandler is not null)
            {
                await PlatformHandler(announcement);
            }
        }
        catch (Exception exception)
        {
            _announcementFailed?.Invoke(exception);
        }
    }

    internal static void ResetForTests()
    {
        PlatformHandler = null;
        _announcementRequested = null;
        _announcementFailed = null;
        _semanticsEventRequested = null;
    }
}
