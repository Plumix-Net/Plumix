using System.Globalization;
using Avalonia;
using Plumix.UI;

// C#-only counterpart of the dart:ui text geometry types (flutter/engine/src/flutter/lib/ui/text.dart:
// `TextBox`, `LineMetrics`, `BoxHeightStyle`, `BoxWidthStyle`). They keep the `Plumix` namespace
// they have always lived in.

namespace Plumix;

/// Defines how the bounds of a selection box are extended vertically.
public enum BoxHeightStyle
{
    /// Provide tight bounding boxes that fit heights per run.
    Tight,

    /// The height of the boxes will be the maximum height of all runs in the line.
    Max,

    /// Extends the top and bottom edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingMiddle,

    /// Extends the top edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingTop,

    /// Extends the bottom edge of the bounds to fully cover any line spacing.
    IncludeLineSpacingBottom,

    /// Calculates the height of the boxes as if the text was laid out with the strut.
    Strut,
}

/// Defines how the bounds of a selection box are extended horizontally.
public enum BoxWidthStyle
{
    /// Provide tight bounding boxes that fit widths to the runs.
    Tight,

    /// Adds up to two additional boxes to fill the line's leading and trailing space.
    Max,
}

/// A rectangle enclosing a run of text, with the direction that run flows in.
///
/// `Left` and `Right` are physical edges; <see cref="Start"/> and <see cref="End"/> resolve them
/// against <see cref="Direction"/>.
public readonly record struct TextBox(double Left, double Top, double Right, double Bottom, TextDirection Direction)
{
    /// Dart's `TextBox.fromLTRBD`.
    public static TextBox FromLTRBD(double left, double top, double right, double bottom, TextDirection direction)
    {
        return new TextBox(left, top, right, bottom, direction);
    }

    public static TextBox FromRect(Rect rect, TextDirection direction)
    {
        return new TextBox(rect.Left, rect.Top, rect.Right, rect.Bottom, direction);
    }

    /// The left edge of the box for left-to-right text, the right edge for right-to-left text.
    public double Start => Direction == TextDirection.Ltr ? Left : Right;

    /// The right edge of the box for left-to-right text, the left edge for right-to-left text.
    public double End => Direction == TextDirection.Ltr ? Right : Left;

    public Rect ToRect() => new(new Point(Left, Top), new Point(Right, Bottom));

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"TextBox.fromLTRBD({Left:F1}, {Top:F1}, {Right:F1}, {Bottom:F1}, "
            + $"TextDirection.{(Direction == TextDirection.Ltr ? "ltr" : "rtl")})");
    }
}

/// The metrics of a single laid-out line of text.
///
/// Dart's `ui.LineMetrics`: `Ascent`/`Descent` are the final positive distances above and below
/// <see cref="Baseline"/>, `UnscaledAscent` ignores `TextStyle.height`, `Left` is the left edge of
/// the line, `Width` spans the leftmost to the rightmost glyph, and `HardBreak` is true for lines
/// that end with an explicit break or the end of the paragraph.
public readonly record struct LineMetrics(
    bool HardBreak,
    double Ascent,
    double Descent,
    double UnscaledAscent,
    double Height,
    double Width,
    double Left,
    double Baseline,
    int LineNumber)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"LineMetrics(hardBreak: {(HardBreak ? "true" : "false")}, ascent: {Ascent}, descent: {Descent}, "
            + $"unscaledAscent: {UnscaledAscent}, height: {Height}, width: {Width}, left: {Left}, "
            + $"baseline: {Baseline}, lineNumber: {LineNumber})");
    }
}
