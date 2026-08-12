// Dart parity source (reference): flutter/packages/flutter/lib/src/foundation/change_notifier.dart (approximate)

namespace Plumix.Foundation;

public interface IListenable
{
    void AddListener(Action listener);
    void RemoveListener(Action listener);
}

public interface IValueListenable<out T> : IListenable
{
    T Value { get; }
}

public static class Listenable
{
    public static IListenable Merge(params IListenable?[] listenables) => new MergingListenable(listenables);

    private sealed class MergingListenable : IListenable
    {
        private readonly IListenable?[] _children;

        public MergingListenable(IListenable?[] children) => _children = children;

        public void AddListener(Action listener)
        {
            foreach (IListenable? child in _children)
            {
                child?.AddListener(listener);
            }
        }

        public void RemoveListener(Action listener)
        {
            foreach (IListenable? child in _children)
            {
                child?.RemoveListener(listener);
            }
        }
    }
}

public class ChangeNotifier : IListenable, IDisposable
{
    private readonly List<Action> _listeners = [];
    private bool _disposed;

    public virtual void AddListener(Action listener)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        _listeners.Add(listener);
    }

    public virtual void RemoveListener(Action listener)
    {
        if (_disposed)
        {
            return;
        }

        _ = _listeners.Remove(listener);
    }

    /// <summary>Whether any listeners are currently registered.</summary>
    protected bool HasListeners => _listeners.Count > 0;

    public void NotifyListeners()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var listener in _listeners.ToArray())
        {
            listener();
        }
    }

    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _listeners.Clear();
        _disposed = true;
    }
}

public sealed class ValueNotifier<T> : ChangeNotifier, IValueListenable<T>
{
    private T _value;

    public ValueNotifier(T value)
    {
        _value = value;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            NotifyListeners();
        }
    }
}

public sealed class AlwaysStoppedAnimation<T> : ChangeNotifier, IValueListenable<T>
{
    public AlwaysStoppedAnimation(T value)
    {
        Value = value;
    }

    public T Value { get; }
}
