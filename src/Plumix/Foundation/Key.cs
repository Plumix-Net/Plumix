using System.Runtime.CompilerServices;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/key.dart

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
public abstract class Key
{
    /// <summary>
    /// Construct a <see cref="ValueKey{string}"/> with the given <see cref="string"/>.
    ///
    /// This is the simplest way to create keys.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Key Create(string value) => new ValueKey<string>(value);

    public static bool operator ==(Key? left, Key? right) => Equals(left, right);

    public static bool operator !=(Key? left, Key? right) => !Equals(left, right);

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>
/// A key that is not a <see cref="GlobalKey{T}"/>.
/// </summary>
public abstract class LocalKey : Key;

/// <summary>
/// A key that is only equal to itself.
/// </summary>
public sealed class UniqueKey : LocalKey
{
    public override string ToString()
    {
        return $"[#{Diagnostics.ShortHash(this)}]";
    }
}

/// <summary>
/// A key that uses a value of a particular type to identify itself.
/// </summary>
/// <param name="value">The value to which this key delegates its equality.</param>
/// <typeparam name="T"></typeparam>
public class ValueKey<T>(T value) : LocalKey
{
    public T Value { get; } = value;

    public override bool Equals(object? obj)
    {
        return obj is ValueKey<T> other
            && other.GetType() == GetType()
            && ValuesEqual(other.Value, Value);
    }

    /// <summary>
    /// Dart's <c>other.value == value</c>. Dart compares doubles by IEEE rules, so a NaN value is not
    /// equal to itself; the default comparer's <see cref="double.Equals(double)"/> would say it is.
    /// </summary>
    private static bool ValuesEqual(T left, T right)
    {
        // The typeof checks are JIT-time constants, so value-typed keys never box here.
        if (typeof(T) == typeof(double))
        {
            return (double)(object)left! == (double)(object)right!;
        }

        if (typeof(T) == typeof(float))
        {
            return (float)(object)left! == (float)(object)right!;
        }

        if (!typeof(T).IsValueType && left is double leftDouble && right is double rightDouble)
        {
            return leftDouble == rightDouble;
        }

        return EqualityComparer<T>.Default.Equals(left, right);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);

    public override string ToString()
    {
        string valueString = typeof(T) == typeof(string) ? $"<'{Value}'>" : $"<{Diagnostics.DescribeValue(Value)}>";

        // Dart omits the type for a plain `ValueKey<T>` and prints it only for subclasses.
        return GetType() == typeof(ValueKey<T>)
            ? $"[{valueString}]"
            : $"[{DartTypeName(typeof(T))} {valueString}]";
    }

    private static string DartTypeName(Type type)
    {
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null) return $"{DartTypeName(nullableType)}?";
        if (type == typeof(int) || type == typeof(long)) return "int";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(double)) return "double";
        if (type == typeof(string)) return "String";
        if (type == typeof(object)) return "Object";
        return Diagnostics.DescribeType(type);
    }
}
