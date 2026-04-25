namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/services/mouse_cursor.dart (approximate)

public abstract record MouseCursor;

public sealed record SystemMouseCursor(string Kind) : MouseCursor;

public static class SystemMouseCursors
{
    public static MouseCursor Basic { get; } = new SystemMouseCursor("basic");

    public static MouseCursor Click { get; } = new SystemMouseCursor("click");
}

public static class MouseCursorManager
{
    private static readonly object Sync = new();
    private static readonly List<CursorRequest> Requests = [];
    private static Action<MouseCursor>? _cursorChanged;
    private static MouseCursor _currentCursor = SystemMouseCursors.Basic;
    private static long _nextRequestId;

    public static event Action<MouseCursor>? CursorChanged
    {
        add => _cursorChanged += value;
        remove => _cursorChanged -= value;
    }

    public static MouseCursor CurrentCursor
    {
        get
        {
            lock (Sync)
            {
                return _currentCursor;
            }
        }
    }

    public static IDisposable PushCursor(MouseCursor? cursor)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);
        Action<MouseCursor>? listener = null;
        MouseCursor? nextCursor = null;

        lock (Sync)
        {
            Requests.Add(new CursorRequest(requestId, cursor ?? SystemMouseCursors.Basic));
            TryUpdateCurrentCursorLocked(out listener, out nextCursor);
        }

        listener?.Invoke(nextCursor!);
        return new CursorRequestHandle(requestId);
    }

    internal static void ResetForTests()
    {
        lock (Sync)
        {
            Requests.Clear();
            _cursorChanged = null;
            _currentCursor = SystemMouseCursors.Basic;
            _nextRequestId = 0;
        }
    }

    private static void PopCursor(long requestId)
    {
        Action<MouseCursor>? listener = null;
        MouseCursor? nextCursor = null;

        lock (Sync)
        {
            var removed = false;
            for (var i = Requests.Count - 1; i >= 0; i--)
            {
                if (Requests[i].Id != requestId)
                {
                    continue;
                }

                Requests.RemoveAt(i);
                removed = true;
                break;
            }

            if (removed)
            {
                TryUpdateCurrentCursorLocked(out listener, out nextCursor);
            }
        }

        listener?.Invoke(nextCursor!);
    }

    private static bool TryUpdateCurrentCursorLocked(
        out Action<MouseCursor>? listener,
        out MouseCursor? nextCursor)
    {
        var resolvedCursor = Requests.Count > 0
            ? Requests[^1].Cursor
            : SystemMouseCursors.Basic;

        if (Equals(_currentCursor, resolvedCursor))
        {
            listener = null;
            nextCursor = null;
            return false;
        }

        _currentCursor = resolvedCursor;
        listener = _cursorChanged;
        nextCursor = resolvedCursor;
        return true;
    }

    private readonly record struct CursorRequest(long Id, MouseCursor Cursor);

    private sealed class CursorRequestHandle : IDisposable
    {
        private long _requestId;

        public CursorRequestHandle(long requestId)
        {
            _requestId = requestId;
        }

        public void Dispose()
        {
            var requestId = Interlocked.Exchange(ref _requestId, 0);
            if (requestId == 0)
            {
                return;
            }

            PopCursor(requestId);
        }
    }
}
