namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/semantics.dart

public sealed record SemanticsAnnouncement(
    int ViewId,
    string Message,
    TextDirection TextDirection);

public static class SemanticsService
{
    private static Action<SemanticsAnnouncement>? _announcementRequested;
    private static Action<Exception>? _announcementFailed;

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
    }
}
