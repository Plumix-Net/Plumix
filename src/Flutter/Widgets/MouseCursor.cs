namespace Flutter.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/services/mouse_cursor.dart (approximate)

public abstract record MouseCursor;

public sealed record SystemMouseCursor(string Kind) : MouseCursor;

public static class SystemMouseCursors
{
    public static MouseCursor Basic { get; } = new SystemMouseCursor("basic");

    public static MouseCursor Click { get; } = new SystemMouseCursor("click");
}
