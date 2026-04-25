using System.Collections.Generic;
using System.Linq;
using Plumix.Foundation;

// Dart parity source (reference): dart_sample/lib/counter_app_model.dart (exact sample parity)

namespace Plumix;

public sealed class CounterAppModel : ChangeNotifier
{
    private int _count;
    private int _nextId = 5;
    private bool _placeGlobalOnLeft = true;
    private List<int> _items = [1, 2, 3, 4];

    public int Count => _count;

    public IReadOnlyList<int> Items => _items;

    public bool PlaceGlobalOnLeft => _placeGlobalOnLeft;

    public object GlobalBadgeIdentity { get; } = new();

    public void Increment()
    {
        _count += 1;
        NotifyListeners();
    }

    public void Decrement()
    {
        _count -= 1;
        NotifyListeners();
    }

    public void ReverseItems()
    {
        _items = [.._items.AsEnumerable().Reverse()];
        NotifyListeners();
    }

    public void InsertHead()
    {
        _items.Insert(0, _nextId++);
        NotifyListeners();
    }

    public void RemoveTail()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.RemoveAt(_items.Count - 1);
        NotifyListeners();
    }

    public void ToggleGlobalPlacement()
    {
        _placeGlobalOnLeft = !_placeGlobalOnLeft;
        NotifyListeners();
    }
}
