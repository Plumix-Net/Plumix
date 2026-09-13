using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Widgets;

// C#-only counterpart of the dart:ui paragraph API (flutter/engine/src/flutter/lib/ui/text.dart:
// `ParagraphConstraints`, `GlyphInfo`, `ParagraphStyle`, `StrutStyle`, `TextStyle`, `Paragraph`).
// Dart's `ui.TextStyle` and `ui.StrutStyle` are named `ParagraphTextStyle`/`ParagraphStrutStyle`
// because the painting-level `TextStyle`/`StrutStyle` already own those names in C#.

namespace Plumix.UI;

/// Layout constraints for a <see cref="Paragraph"/>: the width lines are broken against.
public readonly record struct ParagraphConstraints(double Width)
{
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"ParagraphConstraints(width: {Width})");
    }
}

/// The measurements of a character (or a sequence of visually connected characters) in a paragraph.
///
/// `GraphemeClusterLayoutBounds` is not a tight outline: its vertical extent comes from the font
/// metrics and its horizontal extent is the advance.
public sealed record GlyphInfo(
    Rect GraphemeClusterLayoutBounds,
    TextRange GraphemeClusterCodeUnitRange,
    TextDirection WritingDirection)
{
    public override string ToString()
    {
        return $"Glyph({GraphemeClusterLayoutBounds}, textRange: {GraphemeClusterCodeUnitRange}, "
               + $"direction: {WritingDirection})";
    }
}

/// Dart's `ui.TextStyle`: an engine text style whose null fields inherit from the style below it
/// on a <see cref="ParagraphBuilder"/>'s style stack.
///
/// A `Height` of <see cref="TextDefaults.TextHeightNone"/> resets an inherited height multiplier to
/// the font-defined line height.
public sealed record ParagraphTextStyle(
    Color? Color = null,
    TextDecoration? Decoration = null,
    Color? DecorationColor = null,
    TextDecorationStyle? DecorationStyle = null,
    double? DecorationThickness = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    TextBaseline? TextBaseline = null,
    FontFamily? FontFamily = null,
    IReadOnlyList<string>? FontFamilyFallback = null,
    double? FontSize = null,
    double? LetterSpacing = null,
    double? WordSpacing = null,
    double? Height = null,
    TextLeadingDistribution? LeadingDistribution = null,
    string? Locale = null,
    Color? BackgroundColor = null);

/// Dart's `ui.StrutStyle`: the minimum line height a paragraph applies to every line.
public sealed record ParagraphStrutStyle(
    FontFamily? FontFamily = null,
    IReadOnlyList<string>? FontFamilyFallback = null,
    double? FontSize = null,
    double? Height = null,
    TextLeadingDistribution? LeadingDistribution = null,
    double? Leading = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    bool? ForceStrutHeight = null)
{
    /// Whether the strut affects layout.
    ///
    /// Dart enables the strut when any field other than the fallback list is set. Plumix also
    /// treats the painting library's `StrutStyle.disabled` shape (zero height, zero leading, nothing
    /// else) as disabled, which is the documented intent of that constant.
    internal bool IsEnabled =>
        FontFamily is not null
        || FontSize is not null
        || (Height is { } height && height != TextDefaults.TextHeightNone)
        || LeadingDistribution is not null
        || Leading is > 0
        || FontWeight is not null
        || FontStyle is not null
        || ForceStrutHeight is not null;
}

/// Dart's `ui.ParagraphStyle`: the paragraph-wide settings a <see cref="ParagraphBuilder"/> starts
/// from.
public sealed record ParagraphStyle(
    TextAlign? TextAlign = null,
    TextDirection? TextDirection = null,
    int? MaxLines = null,
    FontFamily? FontFamily = null,
    double? FontSize = null,
    double? Height = null,
    TextHeightBehavior? TextHeightBehavior = null,
    FontWeight? FontWeight = null,
    FontStyle? FontStyle = null,
    ParagraphStrutStyle? StrutStyle = null,
    string? Ellipsis = null,
    string? Locale = null)
{
    public override string ToString()
    {
        string maxLinesText = MaxLines is { } lines ? lines.ToString(CultureInfo.InvariantCulture) : "unspecified";
        string heightText = Height is { } h && h != TextDefaults.TextHeightNone
            ? FormatDouble(h) + "x"
            : "unspecified";
        return "ParagraphStyle("
               + $"textAlign: {Enum(TextAlign, "TextAlign")}, "
               + $"textDirection: {Enum(TextDirection, "TextDirection")}, "
               + $"fontWeight: {(FontWeight is { } weight ? $"FontWeight.w{(int)weight}" : "unspecified")}, "
               + $"fontStyle: {Enum(FontStyle, "FontStyle")}, "
               + $"maxLines: {maxLinesText}, "
               + $"textHeightBehavior: {(TextHeightBehavior?.ToString() ?? "unspecified")}, "
               + $"fontFamily: {(FontFamily?.Name ?? "unspecified")}, "
               + $"fontSize: {(FontSize is { } size ? FormatDouble(size) : "unspecified")}, "
               + $"height: {heightText}, "
               + $"strutStyle: {(StrutStyle?.ToString() ?? "unspecified")}, "
               + $"ellipsis: {(Ellipsis is null ? "unspecified" : $"\"{Ellipsis}\"")}, "
               + $"locale: {Locale ?? "unspecified"})";
    }

    private static string Enum<T>(T? value, string typeName)
        where T : struct, System.Enum
    {
        if (value is not { } resolved)
        {
            return "unspecified";
        }

        string name = resolved.ToString();
        return $"{typeName}.{char.ToLowerInvariant(name[0])}{name[1..]}";
    }

    internal static string FormatDouble(double value)
    {
        return value == Math.Floor(value) && Math.Abs(value) < 1e15
            ? value.ToString("0.0", CultureInfo.InvariantCulture)
            : value.ToString("R", CultureInfo.InvariantCulture);
    }
}

/// A paragraph of text: Dart's `ui.Paragraph`.
///
/// Created only by <see cref="ParagraphBuilder.Build"/>. Every metric and query is valid only after
/// <see cref="Layout"/>. Plumix has two engines behind this type: the Avalonia text formatter when a
/// font manager is available, and a headless engine with the metrics of Flutter's `FlutterTest`
/// font everywhere else (unit tests, hosts with no platform).
public abstract class Paragraph : IDisposable
{
    private bool _disposed;

    private protected Paragraph(string text)
    {
        Text = text;
    }

    /// The text this paragraph was built from, one U+FFFC per placeholder.
    internal string Text { get; }

    /// The amount of horizontal space this paragraph occupies: the width it was laid out at.
    public abstract double Width { get; }

    /// The amount of vertical space this paragraph occupies.
    public abstract double Height { get; }

    /// The distance from the left edge of the leftmost glyph to the right edge of the rightmost glyph.
    public abstract double LongestLine { get; }

    /// The minimum width beyond which the paragraph can paint its contents within itself.
    public abstract double MinIntrinsicWidth { get; }

    /// Returns the smallest width beyond which increasing the width never decreases the height.
    public abstract double MaxIntrinsicWidth { get; }

    /// The distance from the top of the paragraph to the alphabetic baseline of the first line.
    public abstract double AlphabeticBaseline { get; }

    /// The distance from the top of the paragraph to the ideographic baseline of the first line.
    public abstract double IdeographicBaseline { get; }

    /// Whether the content was truncated by `MaxLines` or by an ellipsis.
    public abstract bool DidExceedMaxLines { get; }

    /// The number of visible lines; zero for an empty paragraph.
    public abstract int NumberOfLines { get; }

    /// Computes the size and position of each glyph in the paragraph.
    public abstract void Layout(ParagraphConstraints constraints);

    /// Returns boxes that enclose the characters in `[start, end)`, relative to the paragraph origin.
    public abstract IReadOnlyList<TextBox> GetBoxesForRange(
        int start,
        int end,
        BoxHeightStyle boxHeightStyle = BoxHeightStyle.Tight,
        BoxWidthStyle boxWidthStyle = BoxWidthStyle.Tight);

    /// Returns one box per placeholder, in the order the placeholders were added.
    public abstract IReadOnlyList<TextBox> GetBoxesForPlaceholders();

    /// Returns the text position closest to the given offset.
    public abstract TextPosition GetPositionForOffset(Point offset);

    /// Returns the glyph at the given code unit offset, or null when it is not laid out.
    public abstract GlyphInfo? GetGlyphInfoAt(int codeUnitOffset);

    /// Returns the glyph closest to the given offset, or null when the paragraph shows no glyphs.
    public abstract GlyphInfo? GetClosestGlyphInfoForOffset(Point offset);

    /// Returns the UAX #29 word boundary range enclosing the character at `position`.
    ///
    /// The character is the one before the offset for an upstream position and the one at the
    /// offset for a downstream position.
    public TextRange GetWordBoundary(TextPosition position)
    {
        int characterPosition = position.Affinity == TextAffinity.Upstream ? position.Offset - 1 : position.Offset;
        (int start, int end) = UnicodeText.GetWordRange(Text, characterPosition);
        return new TextRange(start, end);
    }

    /// Returns the line range enclosing `position`, including trailing spaces but not the line break.
    public TextRange GetLineBoundary(TextPosition position)
    {
        TextRange line = GetLineRange(position.Offset);
        TextRange nextLine = GetLineRange(position.Offset + 1);
        if (!nextLine.IsValid)
        {
            return line;
        }

        if (position.Affinity == TextAffinity.Downstream
            && line != nextLine
            && position.Offset == line.End
            && line.End == nextLine.Start)
        {
            return nextLine;
        }

        return line;
    }

    /// The engine-side line lookup: the first line whose content range contains `offset`, treating
    /// the offset as upstream, or <see cref="TextRange.Empty"/>.
    private protected abstract TextRange GetLineRange(int offset);

    /// Returns the metrics of every laid-out line.
    public abstract IReadOnlyList<LineMetrics> ComputeLineMetrics();

    /// Returns the metrics of the given line, or null when it does not exist.
    public abstract LineMetrics? GetLineMetricsAt(int lineNumber);

    /// Returns the line a code unit is on, or null when it is not on a visible line.
    public abstract int? GetLineNumberAt(int codeUnitOffset);

    /// Whether this paragraph has been disposed. Available only in debug builds.
    public bool DebugDisposed
    {
        get
        {
            if (!Constants.KDebugMode)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.debugDisposed is only available when asserts are enabled.");
            }

            return _disposed;
        }
    }

    /// Releases the resources this paragraph holds.
    public void Dispose()
    {
        if (Constants.KDebugMode && _disposed)
        {
            throw new InvalidOperationException("Paragraph.dispose() was called more than once.");
        }

        _disposed = true;
        DisposeCore();
    }

    private protected virtual void DisposeCore()
    {
    }

    /// Captures the current layout as a draw call with its origin at `offset`, or null when this
    /// paragraph has nothing a backend can draw.
    internal abstract Action<DrawingContext>? CreateDrawAction(Point offset);

    public override string ToString() => _disposed ? "Paragraph(DISPOSED)" : "Paragraph()";
}
