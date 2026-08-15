using System.Globalization;
using System.Text;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

namespace Plumix.Foundation;

/// <summary>
/// Module-level helpers from Flutter's `foundation/diagnostics.dart` (plus `objectRuntimeType`
/// from `foundation/object.dart`, which the diagnostics layer is the only real consumer of).
/// </summary>
public static class Diagnostics
{
    /// Returns a 5 character long hexadecimal string generated from
    /// [Object.hashCode]'s 20 least-significant bits.
    public static string ShortHash(object? obj)
    {
        uint hash = unchecked((uint)(obj?.GetHashCode() ?? 0));

        return (hash & 0xFFFFFu).ToString("x5", CultureInfo.InvariantCulture);
    }

    /// Returns a summary of the runtime type and hash code of `object`.
    ///
    /// See also:
    ///
    ///  * [Object.hashCode], a value used when placing an object in a [Map] or
    ///    other similar data structure, and which is also used in debug output to
    ///    distinguish instances of the same class (hash collisions are
    ///    possible, but rare enough that its use in debug output is useful).
    ///  * [Object.runtimeType], the [Type] of an object.
    public static string DescribeIdentity(object? obj) => $"{ObjectRuntimeType(obj)}#{ShortHash(obj)}";

    /// Framework counterpart of Dart's `objectRuntimeType(object, optimizedValue)`.
    ///
    /// Dart returns `optimizedValue` in release builds; Plumix has no build-mode concept, so the
    /// real type name is always returned (see `docs/ai/DIVERGENCES.md`).
    public static string ObjectRuntimeType(object? obj)
    {
        return obj is null ? "Null" : DescribeType(obj.GetType());
    }

    /// Renders `type` the way Dart renders `runtimeType`: the bare name plus angle-bracketed
    /// generic arguments, with no CLR arity suffix and no namespace.
    public static string DescribeType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name;
        int arity = name.IndexOf('`', StringComparison.Ordinal);
        if (arity >= 0)
        {
            name = name[..arity];
        }

        var builder = new StringBuilder(name);
        builder.Append('<');
        Type[] arguments = type.GetGenericArguments();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(DescribeType(arguments[i]));
        }

        builder.Append('>');
        return builder.ToString();
    }

    /// Returns the name of an enum value the way Dart's `Enum.name` does: the declared member name
    /// in lower camel case, so serialized diagnostics stay wire-compatible with Dart's.
    public static string EnumName(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return ToLowerCamelCase(value.ToString());
    }

    /// Returns a short description of an enum value.
    ///
    /// Strips off the enum name from the `enumEntry.toString()`.
    [Obsolete("Use the Name getter on enums instead. This feature was deprecated after v3.14.0-2.0.pre.")]
    public static string DescribeEnum(object enumEntry)
    {
        ArgumentNullException.ThrowIfNull(enumEntry);

        if (enumEntry is Enum value)
        {
            return EnumName(value);
        }

        string description = enumEntry.ToString() ?? string.Empty;
        int indexOfDot = description.IndexOf('.', StringComparison.Ordinal);
        if (indexOfDot == -1 || indexOfDot >= description.Length - 1)
        {
            throw new ArgumentException(
                $"The provided object \"{enumEntry}\" is not an enum.",
                nameof(enumEntry));
        }

        return description[(indexOfDot + 1)..];
    }

    internal static bool IsSingleLine(DiagnosticsTreeStyle? style) => style == DiagnosticsTreeStyle.SingleLine;

    /// Renders `value` the way Dart's `Object.toString()` would.
    ///
    /// `bool` prints lower case and numbers print with the invariant culture, so diagnostics read
    /// the same regardless of the ambient culture.
    internal static string DescribeValue(object? value)
    {
        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            string text => text,
            IFormattable formattable when value is not Enum =>
                formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string ToLowerCamelCase(string name)
    {
        if (name.Length == 0 || !char.IsUpper(name[0]))
        {
            return name;
        }

        return string.Create(
            name.Length,
            name,
            static (span, source) =>
            {
                source.AsSpan().CopyTo(span);
                span[0] = char.ToLowerInvariant(span[0]);
            });
    }
}

/// Marker type for the `kNoDefaultValue` sentinel; see <see cref="DiagnosticsDefaults"/>.
internal sealed class NoDefaultValueMarker
{
    internal static readonly NoDefaultValueMarker Instance = new();

    private NoDefaultValueMarker()
    {
    }

    public override string ToString() => "no default value";
}

/// Marker type for an explicitly null default value; see <see cref="DiagnosticsDefaults"/>.
internal sealed class NullDefaultValueMarker
{
    internal static readonly NullDefaultValueMarker Instance = new();

    private NullDefaultValueMarker()
    {
    }

    public override string ToString() => "null";
}

/// <summary>
/// Hosts the two markers a property's `defaultValue` slot can carry.
///
/// Dart defaults the parameter to the `kNoDefaultValue` sentinel and lets `defaultValue: null`
/// mean "null is the boring value". C# forbids a non-constant default parameter value, so an
/// omitted argument means [NoDefaultValue] and Dart's explicit `defaultValue: null` is written as
/// [NullValue].
/// </summary>
public static class DiagnosticsDefaults
{
    /// Marker object indicating that a [DiagnosticsNode] has no default value.
    public static object NoDefaultValue => NoDefaultValueMarker.Instance;

    /// Marker object indicating that a [DiagnosticsNode]'s default value is null.
    public static object NullValue => NullDefaultValueMarker.Instance;
}
