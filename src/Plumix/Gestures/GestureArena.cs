// Dart parity source: flutter/packages/flutter/lib/src/gestures/arena.dart

namespace Plumix.Gestures;

public enum GestureDisposition
{
    Accepted,
    Rejected
}

public interface IGestureArenaMember
{
    void AcceptGesture(int pointer);
    void RejectGesture(int pointer);
}

public readonly struct GestureArenaEntry
{
    private readonly Action<GestureDisposition> _resolve;

    internal GestureArenaEntry(GestureArenaManager manager, int pointer, IGestureArenaMember member)
    {
        _resolve = disposition => manager.Resolve(pointer, member, disposition);
    }

    internal GestureArenaEntry(Action<GestureDisposition> resolve)
    {
        _resolve = resolve;
    }

    public void Resolve(GestureDisposition disposition)
    {
        _resolve(disposition);
    }
}

public sealed class GestureArenaManager
{
    private readonly Dictionary<int, GestureArena> _arenas = [];

    // Dart's `_tryToResolveArena` hands the last-member-standing case to `scheduleMicrotask`, so it
    // runs after the pointer event that emptied the arena has been fully dispatched. C# has no
    // microtask queue, so the deferred resolutions are queued here and drained by
    // `GestureBinding.HandlePointerEvent` once routing and sweeping are done.
    private readonly List<PendingDefaultResolution> _pendingDefaultResolutions = [];

    public GestureArenaEntry Add(int pointer, IGestureArenaMember member)
    {
        if (!_arenas.TryGetValue(pointer, out GestureArena? arena))
        {
            arena = new GestureArena();
            _arenas[pointer] = arena;
            DebugLogDiagnostic(pointer, "\u2605 Opening new gesture arena.");
        }

        arena.Add(member);
        if (GestureDebug.PrintGestureArenaDiagnostics)
        {
            // Dart wraps this in `assert(() { ... }())`, so the member is never stringified unless
            // the diagnostic is on; C# has to guard it explicitly.
            DebugLogDiagnostic(pointer, $"Adding: {member}");
        }
        return new GestureArenaEntry(this, pointer, member);
    }

    public void Close(int pointer)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            return;
        }

        arena.IsOpen = false;
        DebugLogDiagnostic(pointer, "Closing", arena);
        TryResolve(pointer, arena);
    }

    public void Sweep(int pointer)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            return;
        }

        if (arena.IsHeld)
        {
            // A long-lived member (double tap) is holding the arena open past the up event.
            arena.HasPendingSweep = true;
            DebugLogDiagnostic(pointer, "Delaying sweep", arena);
            return;
        }

        DebugLogDiagnostic(pointer, "Sweeping", arena);
        _arenas.Remove(pointer);
        if (arena.Members.Count == 0)
        {
            return;
        }

        // First member wins (accepted before the losers hear the bad news, matching Dart's sweep).
        IGestureArenaMember[] snapshot = [.. arena.Members];
        DebugLogDiagnostic(pointer, $"Winner: {snapshot[0]}");
        snapshot[0].AcceptGesture(pointer);
        for (int i = 1; i < snapshot.Length; i++)
        {
            snapshot[i].RejectGesture(pointer);
        }
    }

    /// <summary>Dart's `hold`: prevents the arena from being swept until <see cref="Release"/>.</summary>
    public void Hold(int pointer)
    {
        if (_arenas.TryGetValue(pointer, out GestureArena? arena))
        {
            arena.IsHeld = true;
            DebugLogDiagnostic(pointer, "Holding", arena);
        }
    }

    /// <summary>Dart's `release`: lifts a hold; a sweep attempted while held runs now.</summary>
    public void Release(int pointer)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            return;
        }

        arena.IsHeld = false;
        DebugLogDiagnostic(pointer, "Releasing", arena);
        if (arena.HasPendingSweep)
        {
            Sweep(pointer);
        }
    }

    internal void Resolve(int pointer, IGestureArenaMember member, GestureDisposition disposition)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            return;
        }

        if (!arena.Members.Contains(member))
        {
            return;
        }

        if (disposition == GestureDisposition.Accepted)
        {
            DebugLogDiagnostic(pointer, $"Accepting: {member}");
            if (arena.IsOpen)
            {
                // Dart's `eagerWinner ??=`: the first member to accept while open wins at close.
                arena.EagerWinner ??= member;
            }
            else
            {
                DebugLogDiagnostic(pointer, $"Self-declared winner: {member}");
                ResolveInFavor(pointer, arena, member);
            }

            return;
        }

        DebugLogDiagnostic(pointer, $"Rejecting: {member}");
        arena.Members.Remove(member);
        member.RejectGesture(pointer);

        if (!arena.IsOpen)
        {
            TryResolve(pointer, arena);
        }
    }

    private void TryResolve(int pointer, GestureArena arena)
    {
        if (arena.Members.Count == 1)
        {
            _pendingDefaultResolutions.Add(new PendingDefaultResolution(pointer, arena));
            return;
        }

        if (arena.Members.Count == 0)
        {
            _arenas.Remove(pointer);
            DebugLogDiagnostic(pointer, "Arena empty.");
            return;
        }

        if (arena.EagerWinner != null)
        {
            DebugLogDiagnostic(pointer, $"Eager winner: {arena.EagerWinner}");
            ResolveInFavor(pointer, arena, arena.EagerWinner);
        }
    }

    private void ResolveInFavor(int pointer, GestureArena arena, IGestureArenaMember winner)
    {
        IGestureArenaMember[] snapshot = [.. arena.Members];
        _arenas.Remove(pointer);

        // Every loser is rejected before the winner is accepted, so a recognizer that reacts to
        // winning always observes its competitors already cancelled.
        foreach (var member in snapshot)
        {
            if (!ReferenceEquals(member, winner))
            {
                member.RejectGesture(pointer);
            }
        }

        winner.AcceptGesture(pointer);
    }

    /// <summary>
    /// Runs the deferred single-member resolutions Dart schedules as microtasks. Called by
    /// <see cref="GestureBinding.HandlePointerEvent"/> once the event has been fully dispatched.
    /// </summary>
    internal void FlushDefaultResolutions()
    {
        while (_pendingDefaultResolutions.Count > 0)
        {
            PendingDefaultResolution pending = _pendingDefaultResolutions[0];
            _pendingDefaultResolutions.RemoveAt(0);
            ResolveByDefault(pending.Pointer, pending.Arena);
        }
    }

    private void ResolveByDefault(int pointer, GestureArena arena)
    {
        if (!_arenas.TryGetValue(pointer, out GestureArena? current) || !ReferenceEquals(current, arena))
        {
            // This arena has already resolved.
            return;
        }

        if (arena.Members.Count != 1)
        {
            return;
        }

        _arenas.Remove(pointer);
        DebugLogDiagnostic(pointer, $"Default winner: {arena.Members[0]}");
        arena.Members[0].AcceptGesture(pointer);
    }

    internal void Reset()
    {
        _pendingDefaultResolutions.Clear();
        _arenas.Clear();
    }

    private readonly record struct PendingDefaultResolution(int Pointer, GestureArena Arena);

    private static void DebugLogDiagnostic(int pointer, string message, GestureArena? arena = null)
    {
        if (!GestureDebug.PrintGestureArenaDiagnostics)
        {
            return;
        }

        int? count = arena?.Members.Count;
        string plural = count != 1 ? "s" : string.Empty;
        string suffix = count is null ? string.Empty : $" with {count} member{plural}.";
        GestureDebug.Log($"Gesture arena {pointer.ToString().PadRight(4)} \u2759 {message}{suffix}");
    }

    private sealed class GestureArena
    {
        public List<IGestureArenaMember> Members { get; } = [];
        public bool IsOpen { get; set; } = true;
        public bool IsHeld { get; set; }
        public bool HasPendingSweep { get; set; }
        public IGestureArenaMember? EagerWinner { get; set; }

        public void Add(IGestureArenaMember member)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Cannot add a member to a closed gesture arena.");
            }

            Members.Add(member);
        }

        public override string ToString()
        {
            var buffer = new System.Text.StringBuilder();
            if (Members.Count == 0)
            {
                buffer.Append("<empty>");
            }
            else
            {
                buffer.AppendJoin(
                    ", ",
                    Members.Select(member =>
                        ReferenceEquals(member, EagerWinner) ? $"{member} (eager winner)" : $"{member}"));
            }

            if (IsOpen)
            {
                buffer.Append(" [open]");
            }

            if (IsHeld)
            {
                buffer.Append(" [held]");
            }

            if (HasPendingSweep)
            {
                buffer.Append(" [hasPendingSweep]");
            }

            return buffer.ToString();
        }
    }
}
