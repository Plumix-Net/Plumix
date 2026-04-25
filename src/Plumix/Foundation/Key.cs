using Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/foundation/key.dart (approximate)

namespace Plumix.Foundation;

/// <summary>
/// A [Key] is an identifier for [Widget]s, [Element]s and [SemanticsNode]s.
///
/// A new widget will only be used to update an existing element if its key is
/// the same as the key of the current widget associated with the element.
///
/// {@youtube 560 315 https://www.youtube.com/watch?v=kn0EOS-ZiIc}
///
/// Keys must be unique amongst the [Element]s with the same parent.
///
/// Subclasses of [Key] should either subclass [LocalKey] or [GlobalKey].
/// </summary>
public abstract record Key
{
    /// <summary>
    /// Construct a <see cref="ValueKey{string}"/> with the given <see cref="string"/>.
    ///
    /// This is the simplest way to create keys.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Key Create(string value) => new ValueKey<string>(value);
}

/// <summary>
/// A key that is not a <see cref="GlobalKey{T}"/>.
/// </summary>
public abstract record LocalKey : Key;

/// <summary>
/// A key that is only equal to itself.
/// </summary>
public sealed record UniqueKey : LocalKey
{
    public override string ToString()
    {
        return $"[#{Diagnostics.ShortHash(this)}]";
    }
}

/// <summary>
/// A key that uses a value of a particular type to identify itself.
/// </summary>
/// <param name="Value">The value to which this key delegates its</param>
/// <typeparam name="T"></typeparam>
public sealed record ValueKey<T>(T Value) : LocalKey
{
    public override string ToString()
    {
        string valueString = typeof(T) == typeof(string) ? $"<'{Value}'>" : $"<{Value}>";

        return $"[{typeof(T)} {valueString}]";
    }
}
