// Dart parity source: flutter/packages/flutter/lib/src/gestures/team.dart

namespace Plumix.Gestures;

/// <summary>Gesture arena members that compete as one entry and may elect a captain.</summary>
public sealed class GestureArenaTeam
{
    private readonly Dictionary<int, CombiningGestureArenaMember> _combiners = [];

    /// <summary>The member that wins on behalf of the team, when one is assigned.</summary>
    public IGestureArenaMember? Captain { get; set; }

    internal GestureArenaEntry Add(int pointer, IGestureArenaMember member)
    {
        if (!_combiners.TryGetValue(pointer, out CombiningGestureArenaMember? combiner))
        {
            combiner = new CombiningGestureArenaMember(this, pointer);
            _combiners[pointer] = combiner;
        }

        return combiner.Add(pointer, member);
    }

    private sealed class CombiningGestureArenaMember : IGestureArenaMember
    {
        private readonly GestureArenaTeam _owner;
        private readonly List<IGestureArenaMember> _members = [];
        private readonly int _pointer;
        private bool _resolved;
        private IGestureArenaMember? _winner;
        private GestureArenaEntry? _entry;

        public CombiningGestureArenaMember(GestureArenaTeam owner, int pointer)
        {
            _owner = owner;
            _pointer = pointer;
        }

        public void AcceptGesture(int pointer)
        {
            Close(pointer);
            _winner ??= _owner.Captain ?? _members[0];
            foreach (IGestureArenaMember member in _members)
            {
                if (!ReferenceEquals(member, _winner))
                {
                    member.RejectGesture(pointer);
                }
            }

            _winner.AcceptGesture(pointer);
        }

        public void RejectGesture(int pointer)
        {
            Close(pointer);
            foreach (IGestureArenaMember member in _members)
            {
                member.RejectGesture(pointer);
            }
        }

        public GestureArenaEntry Add(int pointer, IGestureArenaMember member)
        {
            if (_resolved || pointer != _pointer)
            {
                throw new InvalidOperationException("Cannot add a member to a resolved gesture arena team entry.");
            }

            _members.Add(member);
            _entry ??= GestureBinding.Instance.GestureArena.Add(pointer, this);
            return new GestureArenaEntry(disposition => Resolve(member, disposition));
        }

        private void Resolve(IGestureArenaMember member, GestureDisposition disposition)
        {
            if (_resolved)
            {
                return;
            }

            if (disposition == GestureDisposition.Accepted)
            {
                _winner ??= _owner.Captain ?? member;
                _entry!.Value.Resolve(disposition);
                return;
            }

            _members.Remove(member);
            member.RejectGesture(_pointer);
            if (_members.Count == 0)
            {
                _entry!.Value.Resolve(disposition);
            }
        }

        private void Close(int pointer)
        {
            if (_resolved || pointer != _pointer)
            {
                throw new InvalidOperationException("Gesture arena team resolved inconsistently.");
            }

            _resolved = true;
            bool removed = _owner._combiners.Remove(_pointer, out CombiningGestureArenaMember? combiner);
            if (!removed || !ReferenceEquals(combiner, this))
            {
                throw new InvalidOperationException("Gesture arena team lost its active combiner.");
            }
        }
    }
}
