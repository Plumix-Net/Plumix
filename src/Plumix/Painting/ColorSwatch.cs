// Dart parity source: flutter/packages/flutter/lib/src/painting/colors.dart

using Plumix.Foundation;

namespace Plumix.Painting;

/// <summary>
/// A color that has a small table of related colors called a "swatch".
/// </summary>
/// <remarks>
/// Like Dart's, the swatch <em>is</em> its primary color: it derives from <see cref="Color"/>, so it
/// can be stored in any color slot and recovered with a runtime type test. Dart hashes the swatch
/// map by identity while <c>==</c> compares its contents; Plumix hashes the contents so equal
/// swatches hash equally.
/// </remarks>
/// <typeparam name="T">The type of the swatch keys.</typeparam>
public class ColorSwatch<T> : Color
    where T : notnull
{
    /// <summary>Creates a color that has a small table of related colors called a "swatch".</summary>
    /// <param name="primary">
    /// The 32 bit ARGB value of one of the values in the swatch, as exposed by <see cref="Color.Value"/>.
    /// This is distinct from the key of any color in the swatch.
    /// </param>
    /// <param name="swatch">The table of related colors.</param>
    public ColorSwatch(uint primary, IReadOnlyDictionary<T, Color> swatch)
        : base(primary)
    {
        Swatch = swatch;
    }

    /// <summary>The table of related colors.</summary>
    protected IReadOnlyDictionary<T, Color> Swatch { get; }

    /// <summary>Returns an element of the swatch table.</summary>
    public Color? this[T key] => Swatch.TryGetValue(key, out Color? color) ? color : null;

    /// <summary>Returns the valid keys for accessing the indexer.</summary>
    public IEnumerable<T> Keys => Swatch.Keys;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return base.Equals(obj) && obj is ColorSwatch<T> other && MapEquals(other.Swatch, Swatch);
    }

    public override int GetHashCode()
    {
        int swatchHash = 0;
        foreach (KeyValuePair<T, Color> entry in Swatch)
        {
            swatchHash ^= HashCode.Combine(entry.Key, entry.Value);
        }

        return HashCode.Combine(GetType(), Value, swatchHash);
    }

    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "ColorSwatch")}(primary value: {base.ToString()})";

    /// <summary>Linearly interpolate between two <see cref="ColorSwatch{T}"/>es.</summary>
    /// <remarks>
    /// It delegates to <see cref="Color.Lerp"/> to interpolate the different colors of the swatch.
    /// If either color is null, this function linearly interpolates from a transparent instance of
    /// the other color.
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
            swatch = a!.Swatch.ToDictionary(entry => entry.Key, entry => Lerp(entry.Value, null, t)!);
        }
        else if (a is null)
        {
            swatch = b.Swatch.ToDictionary(entry => entry.Key, entry => Lerp(null, entry.Value, t)!);
        }
        else
        {
            swatch = a.Swatch.ToDictionary(entry => entry.Key, entry => Lerp(entry.Value, b[entry.Key], t)!);
        }

        return new ColorSwatch<T>(Lerp((Color?)a, b, t)!.Value, swatch);
    }

    // foundation's `mapEquals<T, Color>`.
    private static bool MapEquals(IReadOnlyDictionary<T, Color> a, IReadOnlyDictionary<T, Color> b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (KeyValuePair<T, Color> entry in a)
        {
            if (!b.TryGetValue(entry.Key, out Color? color) || color != entry.Value)
            {
                return false;
            }
        }

        return true;
    }
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
                ["red"] = color.Red,
                ["green"] = color.Green,
                ["blue"] = color.Blue,
                ["alpha"] = color.Alpha,
            };
        }

        return json;
    }
}
