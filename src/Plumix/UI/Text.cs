namespace Plumix.UI;

/// A horizontal line used for aligning text.
public enum TextBaseline
{
    /// The horizontal line used to align the bottom of glyphs for alphabetic characters.
    Alphabetic,

    /// The horizontal line used to align ideographic characters.
    Ideographic,
}

public enum TextDirection
{
    /// The text flows from right to left (e.g. Arabic, Hebrew).
    Rtl,

    /// The text flows from left to right (e.g., English, French).
    Ltr
}

/// How the horizontal alignment of text should be handled.
public enum TextAlign
{
    Left,
    Right,
    Center,
    Justify,
    Start,
    End
}

/// Visual overflow handling for laid out text.
public enum TextOverflow
{
    Clip,
    Ellipsis,
    Fade
}

/// The horizontal extent used to compute the width of a paragraph.
public enum TextWidthBasis
{
    Parent,
    LongestLine,
}

/// Controls whether the first ascent and last descent include TextStyle height adjustments.
public readonly record struct TextHeightBehavior(
    bool ApplyHeightToFirstAscent = true,
    bool ApplyHeightToLastDescent = true);

// Dart parity source (reference): flutter/engine/src/flutter/lib/ui/text.dart (engine parity, approximate)
