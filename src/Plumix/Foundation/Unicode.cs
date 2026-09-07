// Dart parity source: flutter/packages/flutter/lib/src/foundation/unicode.dart

namespace Plumix.Foundation;

/// <summary>
/// Constants for useful Unicode characters, mostly the bidirectional formatting marks used to force
/// a run of text into a reading direction.
/// </summary>
/// <remarks>
/// Flutter's <c>Unicode</c> class. Renamed here because <c>Unicode</c> reads as a namespace in C#,
/// and the members carry their spelt-out names instead of the Unicode abbreviations.
/// </remarks>
public static class UnicodeMarks
{
    /// <summary>U+202A LEFT-TO-RIGHT EMBEDDING (<c>LRE</c>).</summary>
    public const string LeftToRightEmbedding = "\u202A";

    /// <summary>U+202B RIGHT-TO-LEFT EMBEDDING (<c>RLE</c>).</summary>
    public const string RightToLeftEmbedding = "\u202B";

    /// <summary>U+202C POP DIRECTIONAL FORMATTING (<c>PDF</c>).</summary>
    public const string PopDirectionalFormatting = "\u202C";

    /// <summary>U+202D LEFT-TO-RIGHT OVERRIDE (<c>LRO</c>).</summary>
    public const string LeftToRightOverride = "\u202D";

    /// <summary>U+202E RIGHT-TO-LEFT OVERRIDE (<c>RLO</c>).</summary>
    public const string RightToLeftOverride = "\u202E";

    /// <summary>U+2066 LEFT-TO-RIGHT ISOLATE (<c>LRI</c>).</summary>
    public const string LeftToRightIsolate = "\u2066";

    /// <summary>U+2067 RIGHT-TO-LEFT ISOLATE (<c>RLI</c>).</summary>
    public const string RightToLeftIsolate = "\u2067";

    /// <summary>U+2068 FIRST STRONG ISOLATE (<c>FSI</c>).</summary>
    public const string FirstStrongIsolate = "\u2068";

    /// <summary>U+2069 POP DIRECTIONAL ISOLATE (<c>PDI</c>).</summary>
    public const string PopDirectionalIsolate = "\u2069";

    /// <summary>U+200E LEFT-TO-RIGHT MARK (<c>LRM</c>).</summary>
    public const string LeftToRightMark = "\u200E";

    /// <summary>U+200F RIGHT-TO-LEFT MARK (<c>RLM</c>).</summary>
    public const string RightToLeftMark = "\u200F";

    /// <summary>U+061C ARABIC LETTER MARK (<c>ALM</c>).</summary>
    public const string ArabicLetterMark = "\u061C";
}
