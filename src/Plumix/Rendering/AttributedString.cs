using System.Text;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// A string that carries <see cref="StringAttribute"/>s over ranges of its characters, so that
/// assistive technologies can be told to spell a substring out or read it in another locale.
/// </summary>
/// <remarks>
/// Flutter's <c>AttributedString</c>. Instances are immutable; <see cref="Concat"/> (Dart's
/// <c>operator +</c>) is how two of them are joined, shifting the right-hand attributes by the
/// left-hand string's length.
/// </remarks>
public sealed class AttributedString : IEquatable<AttributedString>
{
    private static readonly IReadOnlyList<StringAttribute> NoAttributes = [];

    /// <summary>Creates an attributed string over <paramref name="value"/>.</summary>
    /// <remarks>
    /// Flutter asserts that an empty string carries no attributes, and that every attribute's range
    /// ends inside the string. Only the upper bound is checked, exactly as Dart does.
    /// </remarks>
    public AttributedString(string value, IReadOnlyList<StringAttribute>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        attributes ??= NoAttributes;
        if (value.Length == 0 && attributes.Count > 0)
        {
            throw new ArgumentException(
                "An empty AttributedString cannot carry attributes.",
                nameof(attributes));
        }

        foreach (StringAttribute attribute in attributes)
        {
            if (value.Length < attribute.Range.Start || value.Length < attribute.Range.End)
            {
                throw new ArgumentException(
                    $"The range in {attribute} is outside of the string {value}",
                    nameof(attributes));
            }
        }

        String = value;
        Attributes = attributes;
    }

    /// <summary>The plain text of this attributed string.</summary>
    public string String { get; }

    /// <summary>The attributes attached to ranges of <see cref="String"/>.</summary>
    /// <remarks>Never mutated after construction; Dart makes the same promise about its list.</remarks>
    public IReadOnlyList<StringAttribute> Attributes { get; }

    /// <summary>The shared empty value Dart writes as <c>AttributedString('')</c>.</summary>
    public static AttributedString Empty { get; } = new(string.Empty);

    /// <summary>
    /// Returns this string followed by <paramref name="other"/>, with <paramref name="other"/>'s
    /// attribute ranges shifted right by this string's length.
    /// </summary>
    /// <remarks>Flutter's <c>AttributedString.operator +</c>.</remarks>
    public AttributedString Concat(AttributedString other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (String.Length == 0)
        {
            return other;
        }

        if (other.String.Length == 0)
        {
            return this;
        }

        string newString = String + other.String;
        List<StringAttribute> newAttributes = [.. Attributes];
        if (other.Attributes.Count > 0)
        {
            int offset = String.Length;
            foreach (StringAttribute attribute in other.Attributes)
            {
                var newRange = new TextRange(attribute.Range.Start + offset, attribute.Range.End + offset);
                newAttributes.Add(attribute.Copy(newRange));
            }
        }

        return new AttributedString(newString, newAttributes);
    }

    /// <summary>Operator form of <see cref="Concat"/>, matching Dart's <c>operator +</c>.</summary>
    public static AttributedString operator +(AttributedString left, AttributedString right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Concat(right);
    }

    /// <inheritdoc />
    public bool Equals(AttributedString? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return String == other.String && Attributes.SequenceEqual(other.Attributes);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as AttributedString);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(String);
        foreach (StringAttribute attribute in Attributes)
        {
            hash.Add(attribute);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(AttributedString? left, AttributedString? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(AttributedString? left, AttributedString? right) => !(left == right);

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder("AttributedString('");
        builder.Append(String);
        builder.Append("', attributes: [");
        for (int i = 0; i < Attributes.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Attributes[i]);
        }

        builder.Append("])");
        return builder.ToString();
    }
}

/// <summary>
/// A <see cref="DiagnosticsProperty{T}"/> for <see cref="AttributedString"/> values that hides an
/// empty string unless <see cref="ShowWhenEmpty"/> is set.
/// </summary>
/// <remarks>Flutter's <c>AttributedStringProperty</c>.</remarks>
public sealed class AttributedStringProperty : DiagnosticsProperty<AttributedString>
{
    public AttributedStringProperty(
        string name,
        AttributedString? value,
        bool showName = true,
        bool showWhenEmpty = false,
        object? defaultValue = null,
        DiagnosticLevel level = DiagnosticLevel.Info,
        string? description = null)
        : base(
            name,
            value,
            showName: showName,
            defaultValue: defaultValue,
            level: level,
            description: description)
    {
        ShowWhenEmpty = showWhenEmpty;
    }

    /// <summary>Whether to show the property even when the string is empty.</summary>
    public bool ShowWhenEmpty { get; }

    /// <inheritdoc />
    public override bool IsInteresting =>
        base.IsInteresting && (ShowWhenEmpty || TypedValue?.String.Length > 0);

    /// <inheritdoc />
    public override string ValueToString(TextTreeConfiguration? parentConfiguration = null)
    {
        AttributedString? value = TypedValue;
        if (value is null)
        {
            return "null";
        }

        string text = value.String;
        if (parentConfiguration is { LineBreakProperties: false })
        {
            text = text.Replace("\n", "\\n", StringComparison.Ordinal);
        }

        if (value.Attributes.Count == 0)
        {
            return $"\"{text}\"";
        }

        return $"\"{text}\" [{string.Join(", ", value.Attributes)}]";
    }
}
