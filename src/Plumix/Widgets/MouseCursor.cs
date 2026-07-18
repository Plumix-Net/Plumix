using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/services/mouse_cursor.dart (approximate)

public abstract record MouseCursor;

public sealed record SystemMouseCursor(string Kind) : MouseCursor;

public static class SystemMouseCursors
{
    public static MouseCursor Basic { get; } = new SystemMouseCursor("basic");

    public static MouseCursor Click { get; } = new SystemMouseCursor("click");

    public static MouseCursor Text { get; } = new SystemMouseCursor("text");

    public static MouseCursor Grab { get; } = new SystemMouseCursor("grab");

    public static MouseCursor Grabbing { get; } = new SystemMouseCursor("grabbing");
}

/// <summary>Changes the host cursor while the pointer is inside its child.</summary>
public sealed class MouseRegion : StatefulWidget
{
    public MouseRegion(
        Widget? child = null,
        MouseCursor? cursor = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Cursor = cursor ?? SystemMouseCursors.Basic;
    }

    public Widget? Child { get; }

    public MouseCursor Cursor { get; }

    public override State CreateState() => new MouseRegionState();

    private sealed class MouseRegionState : State
    {
        private IDisposable? _cursorHandle;

        private MouseRegion CurrentWidget => (MouseRegion)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            if (_cursorHandle is null || Equals(((MouseRegion)oldWidget).Cursor, CurrentWidget.Cursor))
            {
                return;
            }

            _cursorHandle.Dispose();
            _cursorHandle = MouseCursorManager.PushCursor(CurrentWidget.Cursor);
        }

        public override Widget Build(BuildContext context)
        {
            return new Listener(
                onPointerEnter: _ => HandleEnter(),
                onPointerExit: _ => HandleExit(),
                child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            HandleExit();
        }

        private void HandleEnter()
        {
            _cursorHandle ??= MouseCursorManager.PushCursor(CurrentWidget.Cursor);
        }

        private void HandleExit()
        {
            _cursorHandle?.Dispose();
            _cursorHandle = null;
        }
    }
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
        long requestId = Interlocked.Increment(ref _nextRequestId);
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
            bool removed = false;
            for (int i = Requests.Count - 1; i >= 0; i--)
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
            long requestId = Interlocked.Exchange(ref _requestId, 0);
            if (requestId == 0)
            {
                return;
            }

            PopCursor(requestId);
        }
    }
}
