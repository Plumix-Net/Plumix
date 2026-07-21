using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/page_storage.dart (exact structure)

namespace Plumix.Widgets;

public record PageStorageKey<T>(T Value) : ValueKey<T>(Value);

public sealed class PageStorageBucket
{
    private Dictionary<object, object?>? _storage;

    public void WriteState(BuildContext context, object? data, object? identifier = null)
    {
        _storage ??= [];
        if (identifier != null)
        {
            _storage[identifier] = data;
            return;
        }

        var contextIdentifier = ComputeIdentifier(context);
        if (contextIdentifier.IsNotEmpty)
        {
            _storage[contextIdentifier] = data;
        }
    }

    public object? ReadState(BuildContext context, object? identifier = null)
    {
        if (_storage == null)
        {
            return null;
        }

        if (identifier != null)
        {
            return _storage.GetValueOrDefault(identifier);
        }

        var contextIdentifier = ComputeIdentifier(context);
        return contextIdentifier.IsNotEmpty
            ? _storage.GetValueOrDefault(contextIdentifier)
            : null;
    }

    private static StorageEntryIdentifier ComputeIdentifier(BuildContext context)
    {
        var keys = new List<Key>();
        if (MaybeAddKey(context.Owner.Widget, keys))
        {
            context.VisitAncestorElements(element => MaybeAddKey(element.Widget, keys));
        }

        return new StorageEntryIdentifier(keys);
    }

    private static bool MaybeAddKey(Widget widget, List<Key> keys)
    {
        if (IsPageStorageKey(widget.Key))
        {
            keys.Add(widget.Key!);
        }

        return widget is not PageStorage;
    }

    private static bool IsPageStorageKey(Key? key)
    {
        Type? type = key?.GetType();
        return type?.IsGenericType == true
               && type.GetGenericTypeDefinition() == typeof(PageStorageKey<>);
    }

    private sealed class StorageEntryIdentifier : IEquatable<StorageEntryIdentifier>
    {
        private readonly IReadOnlyList<Key> _keys;

        public StorageEntryIdentifier(IReadOnlyList<Key> keys)
        {
            _keys = [..keys];
        }

        public bool IsNotEmpty => _keys.Count > 0;

        public bool Equals(StorageEntryIdentifier? other)
        {
            return other != null && _keys.SequenceEqual(other._keys);
        }

        public override bool Equals(object? obj)
        {
            return obj is StorageEntryIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (Key key in _keys)
            {
                hash.Add(key);
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"StorageEntryIdentifier({string.Join(":", _keys)})";
        }
    }
}

public sealed class PageStorage : StatelessWidget
{
    public PageStorage(PageStorageBucket bucket, Widget child, Key? key = null) : base(key)
    {
        Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public PageStorageBucket Bucket { get; }

    public Widget Child { get; }

    public static PageStorageBucket? MaybeOf(BuildContext context)
    {
        return context.FindAncestorWidgetOfExactType<PageStorage>()?.Bucket;
    }

    public static PageStorageBucket Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "PageStorage.Of() was called with a context that does not contain a PageStorage widget.");
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }
}
