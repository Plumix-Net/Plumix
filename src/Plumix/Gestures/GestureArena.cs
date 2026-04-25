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
    private readonly GestureArenaManager _manager;
    private readonly int _pointer;
    private readonly IGestureArenaMember _member;

    internal GestureArenaEntry(GestureArenaManager manager, int pointer, IGestureArenaMember member)
    {
        _manager = manager;
        _pointer = pointer;
        _member = member;
    }

    public void Resolve(GestureDisposition disposition)
    {
        _manager.Resolve(_pointer, _member, disposition);
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

        if (arena.Winner != null)
        {
            var winner = arena.Winner;
            ResolveInFavor(pointer, arena, winner);
            return;
        }

        if (arena.Members.Count > 0)
        {
            ResolveInFavor(pointer, arena, arena.Members[0]);
            return;
        }

        _arenas.Remove(pointer);
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
            arena.Winner = member;
            if (!arena.IsOpen)
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

        TryResolve(pointer, arena);
    }

    private void TryResolve(int pointer, GestureArena arena)
    {
        if (arena.IsOpen)
        {
            return;
        }

        if (arena.Winner != null)
        {
            ResolveInFavor(pointer, arena, arena.Winner);
            return;
        }

        if (arena.Members.Count == 1)
        {
            ResolveInFavor(pointer, arena, arena.Members[0]);
        }
    }

    private void ResolveInFavor(int pointer, GestureArena arena, IGestureArenaMember winner)
    {
        var snapshot = arena.Members.ToArray();
        _arenas.Remove(pointer);

        foreach (var member in snapshot)
        {
            if (ReferenceEquals(member, winner))
            {
                member.AcceptGesture(pointer);
                continue;
            }

            member.RejectGesture(pointer);
        }
    }

    internal void Reset()
    {
        _arenas.Clear();
    }

    private sealed class GestureArena
    {
        public List<IGestureArenaMember> Members { get; } = [];
        public bool IsOpen { get; set; } = true;
        public IGestureArenaMember? Winner { get; set; }
    }
}
