// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigator.dart

using System.Collections;
using Plumix.Foundation;

namespace Plumix.Widgets;

/// <summary>
/// Flutter's <c>_History</c>: the navigator's route stack, which notifies its listeners on every
/// structural mutation so <c>NavigatorState._handleHistoryChanged</c> can dispatch a
/// <see cref="NavigationNotification"/>.
/// </summary>
/// <remarks>
/// Dart declares it as <c>class _History extends Iterable&lt;_RouteEntry&gt; with ChangeNotifier</c>.
/// C# has no mixins, so the notifier is the base class and the iterable surface is an interface;
/// which mutations notify is the part that is load-bearing, and it matches Dart exactly.
/// </remarks>
internal sealed class RouteHistory : ChangeNotifier, IReadOnlyList<RouteEntry>
{
    private readonly List<RouteEntry> _entries = [];

    public int Count => _entries.Count;

    public RouteEntry this[int index] => _entries[index];

    public void Add(RouteEntry entry)
    {
        _entries.Add(entry);
        NotifyListeners();
    }

    /// <summary>Dart's <c>_History.addAll</c>, which notifies only for a non-empty addition.</summary>
    public void AddRange(IEnumerable<RouteEntry> entries)
    {
        int before = _entries.Count;
        _entries.AddRange(entries);
        if (_entries.Count != before)
        {
            NotifyListeners();
        }
    }

    /// <summary>Dart's <c>_History.clear</c>, which notifies only when the history was not empty.</summary>
    public void Clear()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _entries.Clear();
        NotifyListeners();
    }

    public void Insert(int index, RouteEntry entry)
    {
        _entries.Insert(index, entry);
        NotifyListeners();
    }

    public RouteEntry RemoveAt(int index)
    {
        RouteEntry entry = _entries[index];
        _entries.RemoveAt(index);
        NotifyListeners();
        return entry;
    }

    public RouteEntry RemoveLast()
    {
        RouteEntry entry = _entries[^1];
        _entries.RemoveAt(_entries.Count - 1);
        NotifyListeners();
        return entry;
    }

    public int IndexOf(RouteEntry entry) => _entries.IndexOf(entry);

    public int FindIndex(Predicate<RouteEntry> match) => _entries.FindIndex(match);

    public IEnumerator<RouteEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
