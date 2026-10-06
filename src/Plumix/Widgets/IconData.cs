using System.Globalization;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/icon_data.dart

namespace Plumix.Widgets;

/// <summary>A description of a font glyph, including directional mirroring and fallback families.</summary>
public sealed record IconData(
    int CodePoint,
    string? FontFamily = null,
    string? FontPackage = null,
    bool MatchTextDirection = false,
    IReadOnlyList<string>? FontFamilyFallback = null)
{
    public bool Equals(IconData? other) =>
        other is not null
        && other.CodePoint == CodePoint
        && other.FontFamily == FontFamily
        && other.FontPackage == FontPackage
        && other.MatchTextDirection == MatchTextDirection
        && (ReferenceEquals(other.FontFamilyFallback, FontFamilyFallback)
            || (other.FontFamilyFallback is not null
                && FontFamilyFallback is not null
                && other.FontFamilyFallback.SequenceEqual(FontFamilyFallback)));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CodePoint);
        hash.Add(FontFamily);
        hash.Add(FontPackage);
        hash.Add(MatchTextDirection);
        if (FontFamilyFallback is not null)
        {
            foreach (string family in FontFamilyFallback)
            {
                hash.Add(family);
            }
        }
        return hash.ToHashCode();
    }

    public override string ToString() => $"IconData(U+{CodePoint.ToString("X5", CultureInfo.InvariantCulture)})";

    // Preserve the original four-field C# record deconstruction alongside the source's new field.
    public void Deconstruct(out int codePoint, out string? fontFamily, out string? fontPackage,
        out bool matchTextDirection)
    {
        codePoint = CodePoint;
        fontFamily = FontFamily;
        fontPackage = FontPackage;
        matchTextDirection = MatchTextDirection;
    }
}

/// <summary>Diagnostics for an icon, including its code point in serialized inspector data.</summary>
public class IconDataProperty : DiagnosticsProperty<IconData>
{
    public IconDataProperty(
        string name,
        IconData? value,
        string? ifNull = null,
        bool showName = true,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.SingleLine,
        DiagnosticLevel level = DiagnosticLevel.Info)
        : base(name, value, ifNull: ifNull, showName: showName, style: style, level: level)
    {
    }

    public override Dictionary<string, object?> ToJsonMap(DiagnosticsSerializationDelegate serializationDelegate)
    {
        Dictionary<string, object?> json = base.ToJsonMap(serializationDelegate);
        if (Value is IconData value)
        {
            json["valueProperties"] = new Dictionary<string, object?> { ["codePoint"] = value.CodePoint };
        }
        return json;
    }
}
