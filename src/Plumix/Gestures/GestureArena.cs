// Dart parity source (reference): flutter/packages/flutter/lib/src/gestures/arena.dart (approximate)

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

    public GestureArenaEntry Add(int pointer, IGestureArenaMember member)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            arena = new GestureArena();
            _arenas[pointer] = arena;
        }

        arena.Members.Add(member);
        return new GestureArenaEntry(this, pointer, member);
    }

    public void Close(int pointer)
    {
        if (!_arenas.TryGetValue(pointer, out var arena))
        {
            return;
        }

        arena.IsOpen = false;
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
            return;
        }

        _arenas.Remove(pointer);
        if (arena.Members.Count == 0)
        {
            return;
        }

        // First member wins (accepted before the losers hear the bad news, matching Dart's sweep).
        var snapshot = arena.Members.ToArray();
        snapshot[0].AcceptGesture(pointer);
        for (int i = 1; i < snapshot.Length; i++)
        {
            snapshot[i].RejectGesture(pointer);
        }
    }

    /// <summary>Dart's `hold`: prevents the arena from being swept until <see cref="Release"/>.</summary>
    public void Hold(int pointer)
    {
        if (_arenas.TryGetValue(pointer, out var arena))
        {
            arena.IsHeld = true;
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
            if (arena.IsOpen)
            {
                // Dart's `eagerWinner ??=`: the first member to accept while open wins at close.
                arena.EagerWinner ??= member;
            }
            else
            {
                ResolveInFavor(pointer, arena, member);
            }

            return;
        }

        arena.Members.Remove(member);
        member.RejectGesture(pointer);

        if (arena.Members.Count == 0)
        {
            _arenas.Remove(pointer);
            return;
        }

        if (!arena.IsOpen)
        {
            TryResolve(pointer, arena);
        }
    }

    private void TryResolve(int pointer, GestureArena arena)
    {
        if (arena.Members.Count == 1)
        {
            // Dart defers this to a microtask (`_resolveByDefault`); Plumix resolves synchronously.
            ResolveInFavor(pointer, arena, arena.Members[0]);
            return;
        }

        if (arena.Members.Count == 0)
        {
            _arenas.Remove(pointer);
            return;
        }

        if (arena.EagerWinner != null)
        {
            ResolveInFavor(pointer, arena, arena.EagerWinner);
        }
    }

    private void ResolveInFavor(int pointer, GestureArena arena, IGestureArenaMember winner)
    {
        var snapshot = arena.Members.ToArray();
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

    internal void Reset()
    {
        _arenas.Clear();
    }

    private sealed class GestureArena
    {
        public List<IGestureArenaMember> Members { get; } = [];
        public bool IsOpen { get; set; } = true;
        public bool IsHeld { get; set; }
        public bool HasPendingSweep { get; set; }
        public IGestureArenaMember? EagerWinner { get; set; }
    }
}
