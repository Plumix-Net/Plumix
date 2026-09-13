namespace Plumix.UI;

/// A horizontal line used for aligning text.
public enum TextBaseline
{
    /// The horizontal line used to align the bottom of glyphs for alphabetic characters.
    Alphabetic,

    /// The horizontal line used to align ideographic characters.
    Ideographic,
}

/// How extra line-height space is distributed above and below glyphs.
public enum TextLeadingDistribution
{
    Proportional,
    Even,
}

/// Decorations painted through text glyphs.
[Flags]
public enum TextDecoration
{
    None = 0,
    Underline = 1 << 0,
    Overline = 1 << 1,
    LineThrough = 1 << 2,
}

/// Stroke style used to paint a text decoration.
public enum TextDecorationStyle
{
    Solid,
    Double,
    Dotted,
    Dashed,
    Wavy,
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

/// How overflowing text should be handled.
public enum TextOverflow
{
    /// Clip the overflowing text to fix its container.
    Clip,

    /// Fade the overflowing text to transparent.
    Fade,

    /// Use an ellipsis to indicate that the text has overflowed.
    Ellipsis,

    /// Render overflowing text outside of its container.
    Visible,
}

/// The horizontal extent used to compute the width of a paragraph.
public enum TextWidthBasis
{
    Parent,
    LongestLine,
}

/// Defines how to apply `TextStyle.height` over and under text.
///
/// `ApplyHeightToFirstAscent` and `ApplyHeightToLastDescent` only matter when a height multiplier
/// is set; `LeadingDistribution` is the paragraph-level default for how that extra space is split.
public readonly record struct TextHeightBehavior(
    bool ApplyHeightToFirstAscent = true,
    bool ApplyHeightToLastDescent = true,
    TextLeadingDistribution LeadingDistribution = TextLeadingDistribution.Proportional)
{
    /// Creates the default behavior: height applies to the first ascent and the last descent, and
    /// the leading is distributed proportionally.
    public TextHeightBehavior()
        : this(true, true, TextLeadingDistribution.Proportional)
    {
    }

    public override string ToString()
    {
        string distribution = LeadingDistribution == TextLeadingDistribution.Even ? "even" : "proportional";
        return $"TextHeightBehavior(applyHeightToFirstAscent: {(ApplyHeightToFirstAscent ? "true" : "false")}, "
               + $"applyHeightToLastDescent: {(ApplyHeightToLastDescent ? "true" : "false")}, "
               + $"leadingDistribution: TextLeadingDistribution.{distribution})";
    }
}

// Dart parity source (reference): flutter/engine/src/flutter/lib/ui/text.dart (engine parity, approximate)
