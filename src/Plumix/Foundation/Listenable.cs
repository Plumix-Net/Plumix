// Dart parity source: flutter/packages/flutter/lib/src/foundation/change_notifier.dart

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
    public static IListenable Merge(params IListenable?[] listenables) =>
        new MergingListenable(listenables);

    public static IListenable Merge(IEnumerable<IListenable?> listenables) =>
        new MergingListenable(listenables);

    private sealed class MergingListenable : IListenable
    {
        private readonly IEnumerable<IListenable?> _children;

        public MergingListenable(IEnumerable<IListenable?> children)
        {
            ArgumentNullException.ThrowIfNull(children);
            _children = children;
        }

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

        public override string ToString() =>
            $"Listenable.merge([{string.Join(", ", _children.Select(DescribeChild))}])";

        private static string DescribeChild(IListenable? child)
        {
            if (child is null)
            {
                return "null";
            }

            string? description = child.ToString();
            return child.GetType().GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType == typeof(object)
                ? $"Instance of '{Diagnostics.DescribeType(child.GetType())}'"
                : description ?? $"Instance of '{Diagnostics.DescribeType(child.GetType())}'";
        }
    }
}

public class ChangeNotifier : IListenable, IDisposable
{
    private static readonly Action?[] EmptyListeners = [];

    private int _count;
    private Action?[] _listeners = EmptyListeners;
    private int _notificationCallStackDepth;
    private int _reentrantlyRemovedListeners;
    private bool _debugDisposed;
    private bool _debugCreationDispatched;

    public static bool DebugAssertNotDisposed(ChangeNotifier notifier)
    {
        ArgumentNullException.ThrowIfNull(notifier);

        if (Constants.KDebugMode && notifier._debugDisposed)
        {
            string type = Diagnostics.DescribeType(notifier.GetType());
            throw new FlutterError(
                $"A {type} was used after being disposed.\n"
                + $"Once you have called Dispose() on a {type}, it can no longer be used.");
        }

        return true;
    }

    protected bool HasListeners => _count > 0;

    protected static void MaybeDispatchObjectCreation(ChangeNotifier @object)
    {
        ArgumentNullException.ThrowIfNull(@object);

        if (Constants.KDebugMode && !@object._debugCreationDispatched)
        {
            FoundationDebug.DebugMaybeDispatchCreated("foundation", "ChangeNotifier", @object);
            @object._debugCreationDispatched = true;
        }
    }

    public virtual void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _ = DebugAssertNotDisposed(this);

        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            MaybeDispatchObjectCreation(this);
        }

        if (_count == _listeners.Length)
        {
            int newLength = _count == 0 ? 1 : _listeners.Length * 2;
            var newListeners = new Action?[newLength];
            Array.Copy(_listeners, newListeners, _count);
            _listeners = newListeners;
        }

        _listeners[_count++] = listener;
    }

    private void RemoveAt(int index)
    {
        _count -= 1;
        if (_count * 2 <= _listeners.Length)
        {
            var newListeners = new Action?[_count];
            Array.Copy(_listeners, 0, newListeners, 0, index);
            Array.Copy(_listeners, index + 1, newListeners, index, _count - index);
            _listeners = newListeners;
        }
        else
        {
            Array.Copy(_listeners, index + 1, _listeners, index, _count - index);
            _listeners[_count] = null;
        }
    }

    public virtual void RemoveListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        for (int index = 0; index < _count; index += 1)
        {
            if (_listeners[index] != listener)
            {
                continue;
            }

            if (_notificationCallStackDepth > 0)
            {
                _listeners[index] = null;
                _reentrantlyRemovedListeners += 1;
            }
            else
            {
                RemoveAt(index);
            }

            break;
        }
    }

    public virtual void Dispose()
    {
        _ = DebugAssertNotDisposed(this);
        if (Constants.KDebugMode && _notificationCallStackDepth != 0)
        {
            throw new AssertionError(
                $"The Dispose() method on {this} was called during NotifyListeners(). This is likely to cause "
                + "errors since it modifies the list of listeners while the list is being used.");
        }

        if (Constants.KDebugMode)
        {
            _debugDisposed = true;
            if (_debugCreationDispatched)
            {
                _ = FoundationDebug.DebugMaybeDispatchDisposed(this);
            }
        }

        _listeners = EmptyListeners;
        _count = 0;
    }

    public virtual void NotifyListeners()
    {
        _ = DebugAssertNotDisposed(this);
        if (_count == 0)
        {
            return;
        }

        _notificationCallStackDepth += 1;
        int end = _count;
        for (int index = 0; index < end; index += 1)
        {
            try
            {
                _listeners[index]?.Invoke();
            }
            catch (Exception exception)
            {
                string type = Diagnostics.DescribeType(GetType());
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "foundation library",
                    context: new ErrorDescription($"while dispatching notifications for {type}"),
                    informationCollector: () =>
                    [
                        new DiagnosticsProperty<ChangeNotifier>(
                            $"The {type} sending notification was",
                            this,
                            style: DiagnosticsTreeStyle.ErrorProperty),
                    ]));
            }
        }

        _notificationCallStackDepth -= 1;
        if (_notificationCallStackDepth == 0 && _reentrantlyRemovedListeners > 0)
        {
            DefragmentListeners();
        }
    }

    private void DefragmentListeners()
    {
        int newLength = _count - _reentrantlyRemovedListeners;
        if (newLength * 2 <= _listeners.Length)
        {
            var newListeners = new Action?[newLength];
            int newIndex = 0;
            for (int index = 0; index < _count; index += 1)
            {
                Action? listener = _listeners[index];
                if (listener is not null)
                {
                    newListeners[newIndex++] = listener;
                }
            }

            _listeners = newListeners;
        }
        else
        {
            for (int index = 0; index < newLength; index += 1)
            {
                if (_listeners[index] is not null)
                {
                    continue;
                }

                int swapIndex = index + 1;
                while (_listeners[swapIndex] is null)
                {
                    swapIndex += 1;
                }

                _listeners[index] = _listeners[swapIndex];
                _listeners[swapIndex] = null;
            }
        }

        _reentrantlyRemovedListeners = 0;
        _count = newLength;
    }
}

public class ValueNotifier<T> : ChangeNotifier, IValueListenable<T>
{
    private T _value;

    public ValueNotifier(T value)
    {
        _value = value;
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            MaybeDispatchObjectCreation(this);
        }
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

    public override string ToString() => $"{Diagnostics.DescribeIdentity(this)}({Value})";
}
