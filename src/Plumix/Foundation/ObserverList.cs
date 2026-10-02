using System.Collections;

namespace Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/observer_list.dart

/// <summary>
/// A list optimized for the observer pattern when there are small numbers of observers: it keeps
/// insertion order, allows duplicates, and switches <see cref="Contains"/> to a hash lookup once it
/// holds three or more items.
/// </summary>
public class ObserverList<T> : IEnumerable<T>
{
    private readonly List<T> _list = [];
    private bool _isDirty;
    private HashSet<T>? _set;

    /// <summary>Adds an item to the end of this list.</summary>
    public void Add(T item)
    {
        _isDirty = true;
        _list.Add(item);
    }

    /// <summary>Removes the first occurrence of <paramref name="item"/>; returns whether it was present.</summary>
    public bool Remove(T item)
    {
        bool removed = _list.Remove(item);
        if (removed)
        {
            _isDirty = true;
            _set?.Clear(); // Clear the set so that we don't leak items.
        }

        return removed;
    }

    /// <summary>Removes all items from the list.</summary>
    public void Clear()
    {
        _isDirty = false;
        _list.Clear();
        _set?.Clear();
    }

    public bool Contains(T element)
    {
        if (_list.Count < 3)
        {
            return _list.Contains(element);
        }

        _set ??= [];
        if (_isDirty)
        {
            _set.UnionWith(_list);
            _isDirty = false;
        }

        return _set.Contains(element);
    }

    public bool IsEmpty => _list.Count == 0;

    public bool IsNotEmpty => _list.Count != 0;

    public List<T> ToList() => [.. _list];

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// A list optimized for the observer pattern, but for larger numbers of observers: a counted
/// multiset whose iteration order is insertion order of the distinct items.
/// </summary>
public class HashedObserverList<T> : IEnumerable<T>
    where T : notnull
{
    private readonly Dictionary<T, int> _map = [];
    private readonly List<T> _order = [];

    /// <summary>Adds an item to the end of this list.</summary>
    public void Add(T item)
    {
        if (_map.TryGetValue(item, out int count))
        {
            _map[item] = count + 1;
            return;
        }

        _map[item] = 1;
        _order.Add(item);
    }

    /// <summary>Removes one occurrence of <paramref name="item"/>; returns whether it was present.</summary>
    public bool Remove(T item)
    {
        if (!_map.TryGetValue(item, out int value))
        {
            return false;
        }

        if (value == 1)
        {
            _map.Remove(item);
            _order.Remove(item);
        }
        else
        {
            _map[item] = value - 1;
        }

        return true;
    }

    /// <summary>Removes all items from the list.</summary>
    public void Clear()
    {
        _map.Clear();
        _order.Clear();
    }

    public bool Contains(T element) => _map.ContainsKey(element);

    public bool IsEmpty => _map.Count == 0;

    public bool IsNotEmpty => _map.Count != 0;

    public List<T> ToList() => [.. _order];

    // Dart iterates `_map.keys`, which a `LinkedHashMap` yields in insertion order.
    public IEnumerator<T> GetEnumerator() => _order.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
