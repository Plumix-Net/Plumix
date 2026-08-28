// Dart parity source: flutter/packages/flutter/lib/src/painting/colors.dart

using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Painting;

/// <summary>
/// A color that has a small table of related colors called a "swatch".
/// </summary>
/// <remarks>
/// Dart's <c>ColorSwatch&lt;T&gt;</c> extends <c>Color</c>. Avalonia's <see cref="Color"/> is a
/// sealed struct, so the swatch instead exposes its primary color through
/// <see cref="Primary"/> and converts implicitly wherever a <see cref="Color"/> is expected.
/// </remarks>
/// <typeparam name="T">The type of the swatch keys.</typeparam>
public class ColorSwatch<T> : IEquatable<ColorSwatch<T>>
    where T : notnull
{
    /// <summary>Creates a color that has a small table of related colors called a "swatch".</summary>
    /// <param name="primary">
    /// The 32 bit ARGB value of one of the values in the swatch, as exposed by <see cref="Value"/>.
    /// This is distinct from the key of any color in the swatch.
    /// </param>
    /// <param name="swatch">The table of related colors.</param>
    public ColorSwatch(uint primary, IReadOnlyDictionary<T, Color> swatch)
    {
        Value = primary;
        Swatch = swatch;
    }

    /// <summary>The 32 bit ARGB value of the swatch's primary color.</summary>
    public uint Value { get; }

    /// <summary>The swatch's primary color.</summary>
    public Color Primary => Color.FromUInt32(Value);

    /// <summary>The table of related colors.</summary>
    protected IReadOnlyDictionary<T, Color> Swatch { get; }

    /// <summary>Returns an element of the swatch table.</summary>
    public Color? this[T key] => Swatch.TryGetValue(key, out Color color) ? color : null;

    /// <summary>Returns the valid keys for accessing the indexer.</summary>
    public IEnumerable<T> Keys => Swatch.Keys;

    public static implicit operator Color(ColorSwatch<T> swatch) => swatch.Primary;

    public static bool operator ==(ColorSwatch<T>? left, ColorSwatch<T>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(ColorSwatch<T>? left, ColorSwatch<T>? right) => !(left == right);

    /// <summary>Linearly interpolates between two <see cref="ColorSwatch{T}"/>es.</summary>
    /// <remarks>
    /// If either swatch is null, this interpolates from a transparent instance of the other one.
    /// </remarks>
    public static ColorSwatch<T>? Lerp(ColorSwatch<T>? a, ColorSwatch<T>? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        Dictionary<T, Color> swatch;
        if (b is null)
        {
            swatch = a!.Swatch.ToDictionary(
                entry => entry.Key,
                entry => LerpColor(entry.Value, null, t)!.Value);
        }
        else if (a is null)
        {
            swatch = b.Swatch.ToDictionary(
                entry => entry.Key,
                entry => LerpColor(null, entry.Value, t)!.Value);
        }
        else
        {
            swatch = a.Swatch.ToDictionary(
                entry => entry.Key,
                entry => LerpColor(entry.Value, b[entry.Key], t)!.Value);
        }

        Color? primary = LerpColor(a?.Primary, b?.Primary, t);
        return new ColorSwatch<T>(primary!.Value.ToUInt32(), swatch);
    }

    public bool Equals(ColorSwatch<T>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }
        if (other.Value != Value || other.Swatch.Count != Swatch.Count)
        {
            return false;
        }
        foreach (KeyValuePair<T, Color> entry in Swatch)
        {
            if (!other.Swatch.TryGetValue(entry.Key, out Color color) || color != entry.Value)
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as ColorSwatch<T>);

    public override int GetHashCode()
    {
        int swatchHash = 0;
        foreach (KeyValuePair<T, Color> entry in Swatch)
        {
            swatchHash ^= HashCode.Combine(entry.Key, entry.Value);
        }
        return HashCode.Combine(GetType(), Value, swatchHash);
    }

    public override string ToString() => $"{GetType().Name}(primary value: {Primary})";

    /// <summary>
    /// Mirrors Dart's <c>Color.lerp</c>: a null endpoint fades the other one out through its own
    /// color with zero alpha.
    /// </summary>
    private static Color? LerpColor(Color? a, Color? b, double t)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return null;
        }
        Color from = a ?? Color.FromArgb(0, b!.Value.R, b.Value.G, b.Value.B);
        Color to = b ?? Color.FromArgb(0, a!.Value.R, a.Value.G, a.Value.B);
        return Color.FromArgb(
            LerpChannel(from.A, to.A, t),
            LerpChannel(from.R, to.R, t),
            LerpChannel(from.G, to.G, t),
            LerpChannel(from.B, to.B, t));
    }

    private static byte LerpChannel(byte a, byte b, double t) =>
        (byte)Math.Clamp((int)(a + ((b - a) * t)), byte.MinValue, byte.MaxValue);
}

/// <summary>
/// [DiagnosticsProperty] that has a [Color] as value.
/// </summary>
public sealed class ColorProperty : DiagnosticsProperty<Color?>
{
    /// Create a diagnostics property for [Color].
    public ColorProperty(
        string name,
        Color? value,
        bool showName = true,
        object? defaultValue = null,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, value, showName: showName, defaultValue: defaultValue, style: style, level: level)
    {
    }

    /// <inheritdoc />
    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (TypedValue is { } color)
        {
            json["valueProperties"] = new Dictionary<string, object>
            {
                ["red"] = color.R,
                ["green"] = color.G,
                ["blue"] = color.B,
                ["alpha"] = color.A,
            };
        }

        return json;
    }
}
