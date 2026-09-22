using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/page_storage.dart

namespace Plumix.Widgets;

internal interface IPageStorageKey;

public class PageStorageKey<T>(T value) : ValueKey<T>(value), IPageStorageKey;

internal sealed class StorageEntryIdentifier : IEquatable<StorageEntryIdentifier>
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

public sealed class PageStorageBucket
{
    private static bool MaybeAddKey(BuildContext context, List<Key> keys)
    {
        Widget widget = context.Widget;
        Key? key = widget.Key;
        if (key is IPageStorageKey)
        {
            keys.Add(key);
        }

        return widget is not PageStorage;
    }

    private static List<Key> AllKeys(BuildContext context)
    {
        var keys = new List<Key>();
        if (MaybeAddKey(context, keys))
        {
            context.VisitAncestorElements(element => MaybeAddKey(element, keys));
        }

        return keys;
    }

    private static StorageEntryIdentifier ComputeIdentifier(BuildContext context)
    {
        return new StorageEntryIdentifier(AllKeys(context));
    }

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
        PageStorageBucket? bucket = MaybeOf(context);
        if (Constants.KDebugMode && bucket == null)
        {
            throw new FlutterError(
                "PageStorage.Of() was called with a context that does not contain a "
                + "PageStorage widget.\n"
                + "No PageStorage widget ancestor could be found starting from the "
                + "context that was passed to PageStorage.Of(). This can happen "
                + "because you are using a widget that looks for a PageStorage "
                + "ancestor, but no such ancestor exists.\n"
                + "The context used was:\n"
                + $"  {context}");
        }

        return bucket ?? throw new NullReferenceException("Null check operator used on a null value");
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }
}
