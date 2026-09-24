using System.Text;
using Avalonia.Media;
using Plumix.Painting;

// C#-only counterpart of dart:ui `ParagraphBuilder` (flutter/engine/src/flutter/lib/ui/text.dart). The
// engine's style stack and placeholder encoding become the records the two paragraph backends read.

namespace Plumix.UI;

/// Builds a <see cref="Paragraph"/> from a stack of text styles, text and placeholders.
public sealed class ParagraphBuilder
{
    private readonly ParagraphStyle _style;
    private readonly StringBuilder _text = new();
    private readonly List<ResolvedTextStyle> _styleStack = [];
    private readonly List<StyledRange> _runs = [];
    private readonly List<ParagraphPlaceholder> _placeholders = [];
    private readonly List<double> _placeholderScales = [];
    private bool _built;

    /// Creates a paragraph builder for a paragraph with the given style.
    public ParagraphBuilder(ParagraphStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _style = style;
        _styleStack.Add(ResolvedTextStyle.FromParagraphStyle(style));
    }

    /// The number of placeholders added so far.
    public int PlaceholderCount => _placeholders.Count;

    /// The scale of each placeholder, in the order they were added.
    public IReadOnlyList<double> PlaceholderScales => _placeholderScales;

    /// Applies the given style to the text added until the matching <see cref="Pop"/>.
    public void PushStyle(ParagraphTextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        ThrowIfBuilt();
        _styleStack.Add(_styleStack[^1].Merge(style));
    }

    /// Ends the effect of the most recent <see cref="PushStyle"/> call.
    public void Pop()
    {
        ThrowIfBuilt();
        if (_styleStack.Count > 1)
        {
            _styleStack.RemoveAt(_styleStack.Count - 1);
        }
    }

    /// Adds the given text, styled by the current style stack.
    /// <exception cref="ArgumentException">The text is not well-formed UTF-16.</exception>
    public void AddText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfBuilt();
        if (UnicodeText.HasUnpairedSurrogate(text))
        {
            throw new ArgumentException("string is not well-formed UTF-16", nameof(text));
        }

        Append(text);
    }

    /// Adds an inline placeholder space of the given size, occupying one U+FFFC code unit.
    ///
    /// `baselineOffset` is the distance from the top of the placeholder to its baseline and defaults
    /// to `height`. Width, height and baseline offset are multiplied by `scale`.
    public void AddPlaceholder(
        double width,
        double height,
        PlaceholderAlignment alignment,
        double scale = 1.0,
        double? baselineOffset = null,
        TextBaseline? baseline = null)
    {
        ThrowIfBuilt();
        if (alignment is PlaceholderAlignment.AboveBaseline
                or PlaceholderAlignment.BelowBaseline
                or PlaceholderAlignment.Baseline
            && baseline is null)
        {
            throw new ArgumentException(
                "A baseline must be specified for baseline-relative placeholder alignments.",
                nameof(baseline));
        }

        double effectiveBaselineOffset = baselineOffset ?? height;
        _placeholders.Add(new ParagraphPlaceholder(
            _text.Length,
            width * scale,
            height * scale,
            alignment,
            effectiveBaselineOffset * scale,
            baseline ?? TextBaseline.Alphabetic,
            _styleStack[^1]));
        _placeholderScales.Add(scale);
        Append(UnicodeText.ObjectReplacementCharacter.ToString());
    }

    /// Returns the paragraph described by the calls made so far. The builder cannot be used again.
    public Paragraph Build()
    {
        ThrowIfBuilt();
        _built = true;
        var content = new ParagraphContent(_style, _text.ToString(), _runs, _placeholders, _styleStack[0]);
        return ParagraphBackend.Create(content);
    }

    public override string ToString() => "ParagraphBuilder";

    private void Append(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        ResolvedTextStyle current = _styleStack[^1];
        int start = _text.Length;
        _text.Append(text);
        if (_runs.Count > 0 && _runs[^1].End == start && _runs[^1].Style.Equals(current))
        {
            _runs[^1] = _runs[^1] with { End = _text.Length };
            return;
        }

        _runs.Add(new StyledRange(start, _text.Length, current));
    }

    private void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("The ParagraphBuilder has already been built.");
        }
    }
}

/// A fully inherited engine text style: every field resolved against the style stack.
internal sealed record ResolvedTextStyle(
    Color Color,
    TextDecoration Decoration,
    Color? DecorationColor,
    TextDecorationStyle DecorationStyle,
    double? DecorationThickness,
    FontWeight FontWeight,
    FontStyle FontStyle,
    TextBaseline TextBaseline,
    FontFamily FontFamily,
    IReadOnlyList<string>? FontFamilyFallback,
    double FontSize,
    double LetterSpacing,
    double WordSpacing,
    double Height,
    TextLeadingDistribution? LeadingDistribution,
    string? Locale,
    Paint? Background,
    Paint? Foreground,
    IReadOnlyList<Rendering.Shadow>? Shadows,
    IReadOnlyList<FontFeature>? FontFeatures,
    IReadOnlyList<FontVariation>? FontVariations)
{
    /// The default text style the engine derives from a paragraph style.
    ///
    /// The engine's default text color is white; Plumix keeps black, the color every Plumix text
    /// path has always defaulted to.
    internal static ResolvedTextStyle FromParagraphStyle(ParagraphStyle style)
    {
        return new ResolvedTextStyle(
            Colors.Black,
            TextDecoration.None,
            null,
            TextDecorationStyle.Solid,
            null,
            style.FontWeight ?? FontWeight.Normal,
            style.FontStyle ?? FontStyle.Normal,
            TextBaseline.Alphabetic,
            style.FontFamily ?? FontFamily.Default,
            null,
            style.FontSize ?? TextDefaults.DefaultFontSize,
            0.0,
            0.0,
            style.Height ?? TextDefaults.TextHeightNone,
            null,
            style.Locale,
            null,
            null,
            null,
            null,
            null);
    }

    internal ResolvedTextStyle Merge(ParagraphTextStyle style)
    {
        return new ResolvedTextStyle(
            style.Color ?? Color,
            style.Decoration ?? Decoration,
            style.DecorationColor ?? DecorationColor,
            style.DecorationStyle ?? DecorationStyle,
            style.DecorationThickness ?? DecorationThickness,
            style.FontWeight ?? FontWeight,
            style.FontStyle ?? FontStyle,
            style.TextBaseline ?? TextBaseline,
            style.FontFamily ?? FontFamily,
            style.FontFamilyFallback ?? FontFamilyFallback,
            style.FontSize ?? FontSize,
            style.LetterSpacing ?? LetterSpacing,
            style.WordSpacing ?? WordSpacing,
            style.Height ?? Height,
            style.LeadingDistribution ?? LeadingDistribution,
            style.Locale ?? Locale,
            style.Background ?? Background,
            style.Foreground ?? Foreground,
            style.Shadows ?? Shadows,
            style.FontFeatures ?? FontFeatures,
            style.FontVariations ?? FontVariations);
    }
}

/// A range of code units that share one resolved style.
internal readonly record struct StyledRange(int Start, int End, ResolvedTextStyle Style);

/// A placeholder added through <see cref="ParagraphBuilder.AddPlaceholder"/>, already scaled.
internal sealed record ParagraphPlaceholder(
    int Index,
    double Width,
    double Height,
    PlaceholderAlignment Alignment,
    double BaselineOffset,
    TextBaseline Baseline,
    ResolvedTextStyle Style);

/// Everything a paragraph backend needs to lay a built paragraph out.
internal sealed class ParagraphContent
{
    public ParagraphContent(
        ParagraphStyle style,
        string text,
        IReadOnlyList<StyledRange> runs,
        IReadOnlyList<ParagraphPlaceholder> placeholders,
        ResolvedTextStyle defaultStyle)
    {
        Style = style;
        Text = text;
        Runs = runs;
        Placeholders = placeholders;
        DefaultStyle = defaultStyle;
    }

    public ParagraphStyle Style { get; }

    public string Text { get; }

    public IReadOnlyList<StyledRange> Runs { get; }

    public IReadOnlyList<ParagraphPlaceholder> Placeholders { get; }

    /// The style text inherits when no style was pushed: the paragraph style's defaults.
    public ResolvedTextStyle DefaultStyle { get; }

    public TextDirection TextDirection => Style.TextDirection ?? TextDirection.Ltr;

    public TextLeadingDistribution DefaultLeadingDistribution =>
        Style.TextHeightBehavior?.LeadingDistribution ?? TextLeadingDistribution.Proportional;

    /// The resolved style of the code unit at `index`.
    public ResolvedTextStyle StyleAt(int index)
    {
        int run = RunIndexAt(index);
        return run < 0 ? DefaultStyle : Runs[run].Style;
    }

    /// The end of the run containing `index`, or the end of the text.
    public int RunEndAt(int index)
    {
        int run = RunIndexAt(index);
        return run < 0 ? Text.Length : Runs[run].End;
    }

    private int RunIndexAt(int index)
    {
        int low = 0;
        int high = Runs.Count - 1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            StyledRange run = Runs[middle];
            if (index < run.Start)
            {
                high = middle - 1;
            }
            else if (index >= run.End)
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return -1;
    }
}
