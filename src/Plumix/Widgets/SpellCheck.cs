using System.Text.RegularExpressions;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/spell_check.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/spell_check.dart

namespace Plumix.Widgets;

public sealed record SuggestionSpan(TextRange Range, IReadOnlyList<string> Suggestions);

public sealed record SpellCheckResults(string SpellCheckedText, IReadOnlyList<SuggestionSpan> SuggestionSpans);

public interface ISpellCheckService
{
    Task<IReadOnlyList<SuggestionSpan>?> FetchSpellCheckSuggestions(Locale locale, string text);
}

public sealed class DefaultSpellCheckService : ISpellCheckService
{
    public static Func<Locale, string, Task<IReadOnlyList<SuggestionSpan>?>>? PlatformHandler { get; set; }

    public SpellCheckResults? LastSavedResults { get; private set; }

    public async Task<IReadOnlyList<SuggestionSpan>?> FetchSpellCheckSuggestions(Locale locale, string text)
    {
        if (PlatformHandler is null) return null;
        IReadOnlyList<SuggestionSpan>? received;
        try
        {
            received = await PlatformHandler(locale, text);
        }
        catch
        {
            return null;
        }

        if (received is null) return null;
        IReadOnlyList<SuggestionSpan> results = LastSavedResults?.SpellCheckedText == text
            ? MergeResults(LastSavedResults.SuggestionSpans, received)
            : received;
        LastSavedResults = new SpellCheckResults(text, results);
        return results;
    }

    public static IReadOnlyList<SuggestionSpan> MergeResults(
        IReadOnlyList<SuggestionSpan> oldResults,
        IReadOnlyList<SuggestionSpan> newResults)
    {
        var merged = new List<SuggestionSpan>(oldResults.Count + newResults.Count);
        int oldIndex = 0;
        int newIndex = 0;
        while (oldIndex < oldResults.Count && newIndex < newResults.Count)
        {
            SuggestionSpan oldSpan = oldResults[oldIndex];
            SuggestionSpan newSpan = newResults[newIndex];
            if (oldSpan.Range.Start <= newSpan.Range.Start)
            {
                merged.Add(oldSpan);
                oldIndex++;
                if (oldSpan.Range.Start == newSpan.Range.Start) newIndex++;
            }
            else
            {
                merged.Add(newSpan);
                newIndex++;
            }
        }
        for (; oldIndex < oldResults.Count; oldIndex++) merged.Add(oldResults[oldIndex]);
        for (; newIndex < newResults.Count; newIndex++) merged.Add(newResults[newIndex]);
        return merged;
    }
}

public sealed record SpellCheckConfiguration
{
    public SpellCheckConfiguration(
        ISpellCheckService? spellCheckService = null,
        Color? misspelledSelectionColor = null,
        TextStyle? misspelledTextStyle = null,
        EditableTextContextMenuBuilder? spellCheckSuggestionsToolbarBuilder = null)
    {
        SpellCheckService = spellCheckService;
        MisspelledSelectionColor = misspelledSelectionColor;
        MisspelledTextStyle = misspelledTextStyle;
        SpellCheckSuggestionsToolbarBuilder = spellCheckSuggestionsToolbarBuilder;
        SpellCheckEnabled = true;
    }

    private SpellCheckConfiguration(bool enabled)
    {
        SpellCheckEnabled = enabled;
    }

    public ISpellCheckService? SpellCheckService { get; init; }
    public Color? MisspelledSelectionColor { get; init; }
    public TextStyle? MisspelledTextStyle { get; init; }
    public EditableTextContextMenuBuilder? SpellCheckSuggestionsToolbarBuilder { get; init; }
    public bool SpellCheckEnabled { get; init; }

    public static SpellCheckConfiguration Disabled { get; } = new(false);

    public SpellCheckConfiguration CopyWith(
        ISpellCheckService? spellCheckService = null,
        Color? misspelledSelectionColor = null,
        TextStyle? misspelledTextStyle = null,
        EditableTextContextMenuBuilder? spellCheckSuggestionsToolbarBuilder = null)
    {
        if (!SpellCheckEnabled) return Disabled;
        return new SpellCheckConfiguration(
            spellCheckService ?? SpellCheckService,
            misspelledSelectionColor ?? MisspelledSelectionColor,
            misspelledTextStyle ?? MisspelledTextStyle,
            spellCheckSuggestionsToolbarBuilder ?? SpellCheckSuggestionsToolbarBuilder);
    }
}

/// <summary>
/// Dart's top-level text-span builders of <c>widgets/spell_check.dart</c>: the spans
/// <c>EditableTextState.buildTextSpan</c> draws once spell check results have been received.
/// </summary>
public static class SpellCheckTextSpans
{
    /// <summary>
    /// Adjusts spell check results to correspond to <paramref name="newText"/> if the text has changed
    /// since the spell check was requested. Dart's <c>_correctSpellCheckResults</c>.
    /// </summary>
    internal static List<SuggestionSpan> CorrectSpellCheckResults(
        string newText,
        string resultsText,
        IReadOnlyList<SuggestionSpan> results)
    {
        var correctedSpellCheckResults = new List<SuggestionSpan>();
        int spanPointer = 0;
        int offset = 0;

        // Assumes that the order of spans has not been jumbled for optimization purposes, and will only
        // search since the previously found span.
        int searchStart = 0;

        while (spanPointer < results.Count)
        {
            SuggestionSpan currentSpan = results[spanPointer];
            string currentSpanText = resultsText[currentSpan.Range.Start..currentSpan.Range.End];
            int spanLength = currentSpan.Range.End - currentSpan.Range.Start;

            // Try finding SuggestionSpan from resultsText in new text. Dart's `\b` is an ASCII word
            // boundary, which RegexOptions.ECMAScript reproduces.
            string escapedText = Regex.Escape(currentSpanText);
            var currentSpanTextRegexp = new Regex($"\\b{escapedText}\\b", RegexOptions.ECMAScript);
            Match match = currentSpanTextRegexp.Match(newText[searchStart..]);
            int foundIndex = match.Success ? match.Index : -1;

            // Check whether word was found exactly where expected or elsewhere in the newText.
            bool currentSpanFoundExactly = currentSpan.Range.Start == foundIndex + searchStart;
            bool currentSpanFoundExactlyWithOffset = currentSpan.Range.Start + offset == foundIndex + searchStart;
            bool currentSpanFoundElsewhere = foundIndex >= 0;

            if (currentSpanFoundExactly || currentSpanFoundExactlyWithOffset)
            {
                // currentSpan was found at the same index in newText and resultsText or at the same index
                // with the previously calculated adjustment by the offset value, so apply it to new text by
                // adding it to the list of corrected results.
                var adjustedSpan = new SuggestionSpan(
                    new TextRange(currentSpan.Range.Start + offset, currentSpan.Range.End + offset),
                    currentSpan.Suggestions);

                // Start search for the next misspelled word at the end of currentSpan.
                searchStart = Math.Min(currentSpan.Range.End + 1 + offset, newText.Length);
                correctedSpellCheckResults.Add(adjustedSpan);
            }
            else if (currentSpanFoundElsewhere)
            {
                // Word was pushed forward but not modified.
                int adjustedSpanStart = searchStart + foundIndex;
                int adjustedSpanEnd = adjustedSpanStart + spanLength;
                var adjustedSpan = new SuggestionSpan(
                    new TextRange(adjustedSpanStart, adjustedSpanEnd),
                    currentSpan.Suggestions);

                // Start search for the next misspelled word at the end of the adjusted currentSpan.
                searchStart = Math.Min(adjustedSpanEnd + 1, newText.Length);
                // Adjust offset to reflect the difference between where currentSpan was positioned in
                // resultsText versus in newText.
                offset = adjustedSpanStart - currentSpan.Range.Start;
                correctedSpellCheckResults.Add(adjustedSpan);
            }

            spanPointer++;
        }

        return correctedSpellCheckResults;
    }

    /// <summary>
    /// Builds the <see cref="TextSpan"/> tree given the current state of the text input and spell check
    /// results. Dart's <c>buildTextSpanWithSpellCheckSuggestions</c>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="value"/> is the current <see cref="TextEditingValue"/> requested to be
    /// rendered by a text input widget. The <paramref name="composingWithinCurrentTextRange"/> value
    /// represents whether or not there is a valid composing region in the text input. The
    /// <paramref name="style"/> is the <see cref="TextStyle"/> to render the text input with. The
    /// <paramref name="misspelledTextStyle"/> is the style used to render misspelled words.
    /// </remarks>
    public static TextSpan BuildTextSpanWithSpellCheckSuggestions(
        TextEditingValue value,
        bool composingWithinCurrentTextRange,
        TextStyle? style,
        TextStyle misspelledTextStyle,
        SpellCheckResults spellCheckResults)
    {
        IReadOnlyList<SuggestionSpan> spellCheckResultsSpans = spellCheckResults.SuggestionSpans;
        string spellCheckResultsText = spellCheckResults.SpellCheckedText;

        if (spellCheckResultsText != value.Text)
        {
            spellCheckResultsSpans = CorrectSpellCheckResults(
                value.Text,
                spellCheckResultsText,
                spellCheckResultsSpans);
        }

        // We will draw the TextSpan tree based on the composing region, if it is available.
        bool shouldConsiderComposingRegion = PlatformDefaults.TargetPlatform == TargetPlatform.Android;
        if (shouldConsiderComposingRegion)
        {
            return new TextSpan(
                style: style,
                children: BuildSubtreesWithComposingRegion(
                    spellCheckResultsSpans,
                    value,
                    style,
                    misspelledTextStyle,
                    composingWithinCurrentTextRange));
        }

        return new TextSpan(
            style: style,
            children: BuildSubtreesWithoutComposingRegion(
                spellCheckResultsSpans,
                value,
                style,
                misspelledTextStyle,
                value.Selection.BaseOffset));
    }

    /// <summary>Builds the text span tree ignoring the composing region. Dart's
    /// <c>_buildSubtreesWithoutComposingRegion</c>.</summary>
    private static List<InlineSpan> BuildSubtreesWithoutComposingRegion(
        IReadOnlyList<SuggestionSpan>? spellCheckSuggestions,
        TextEditingValue value,
        TextStyle? style,
        TextStyle misspelledStyle,
        int cursorIndex)
    {
        var textSpanTreeChildren = new List<InlineSpan>();

        int textPointer = 0;
        int currentSpanPointer = 0;
        int endIndex;
        string text = value.Text;
        TextStyle misspelledJointStyle = style?.Merge(misspelledStyle) ?? misspelledStyle;
        bool cursorInCurrentSpan;

        // Add text interwoven with any misspelled words to the tree.
        if (spellCheckSuggestions is not null)
        {
            while (textPointer < text.Length && currentSpanPointer < spellCheckSuggestions.Count)
            {
                SuggestionSpan currentSpan = spellCheckSuggestions[currentSpanPointer];

                if (currentSpan.Range.Start > textPointer)
                {
                    endIndex = currentSpan.Range.Start < text.Length ? currentSpan.Range.Start : text.Length;
                    textSpanTreeChildren.Add(new TextSpan(style: style, text: text[textPointer..endIndex]));
                    textPointer = endIndex;
                }
                else
                {
                    endIndex = currentSpan.Range.End < text.Length ? currentSpan.Range.End : text.Length;
                    cursorInCurrentSpan = currentSpan.Range.Start <= cursorIndex
                                          && currentSpan.Range.End >= cursorIndex;
                    textSpanTreeChildren.Add(new TextSpan(
                        style: cursorInCurrentSpan ? style : misspelledJointStyle,
                        text: text[currentSpan.Range.Start..endIndex]));

                    textPointer = endIndex;
                    currentSpanPointer++;
                }
            }
        }

        // Add any remaining text to the tree if applicable.
        if (textPointer < text.Length)
        {
            textSpanTreeChildren.Add(new TextSpan(style: style, text: text[textPointer..]));
        }

        return textSpanTreeChildren;
    }

    /// <summary>Builds the text span tree with the composing region underlined. Dart's
    /// <c>_buildSubtreesWithComposingRegion</c>.</summary>
    private static List<InlineSpan> BuildSubtreesWithComposingRegion(
        IReadOnlyList<SuggestionSpan>? spellCheckSuggestions,
        TextEditingValue value,
        TextStyle? style,
        TextStyle misspelledStyle,
        bool composingWithinCurrentTextRange)
    {
        var textSpanTreeChildren = new List<InlineSpan>();

        int textPointer = 0;
        int currentSpanPointer = 0;
        int endIndex;
        SuggestionSpan currentSpan;
        string text = value.Text;
        TextRange composingRegion = value.Composing ?? TextRange.Empty;
        var underline = new TextStyle(Decoration: Plumix.UI.TextDecoration.Underline);
        TextStyle composingTextStyle = style?.Merge(underline) ?? underline;
        TextStyle misspelledJointStyle = style?.Merge(misspelledStyle) ?? misspelledStyle;
        bool textPointerWithinComposingRegion;
        bool currentSpanIsComposingRegion;

        // Add text interwoven with any misspelled words to the tree.
        if (spellCheckSuggestions is not null)
        {
            while (textPointer < text.Length && currentSpanPointer < spellCheckSuggestions.Count)
            {
                currentSpan = spellCheckSuggestions[currentSpanPointer];

                if (currentSpan.Range.Start > textPointer)
                {
                    endIndex = currentSpan.Range.Start < text.Length ? currentSpan.Range.Start : text.Length;
                    textPointerWithinComposingRegion = composingRegion.Start >= textPointer
                                                       && composingRegion.End <= endIndex
                                                       && !composingWithinCurrentTextRange;

                    if (textPointerWithinComposingRegion)
                    {
                        AddComposingRegionTextSpans(
                            textSpanTreeChildren,
                            text,
                            textPointer,
                            composingRegion,
                            style,
                            composingTextStyle);
                        textSpanTreeChildren.Add(new TextSpan(
                            style: style,
                            text: text[composingRegion.End..endIndex]));
                    }
                    else
                    {
                        textSpanTreeChildren.Add(new TextSpan(style: style, text: text[textPointer..endIndex]));
                    }

                    textPointer = endIndex;
                }
                else
                {
                    endIndex = currentSpan.Range.End < text.Length ? currentSpan.Range.End : text.Length;
                    currentSpanIsComposingRegion = textPointer >= composingRegion.Start
                                                   && endIndex <= composingRegion.End
                                                   && !composingWithinCurrentTextRange;
                    textSpanTreeChildren.Add(new TextSpan(
                        style: currentSpanIsComposingRegion ? composingTextStyle : misspelledJointStyle,
                        text: text[currentSpan.Range.Start..endIndex]));

                    textPointer = endIndex;
                    currentSpanPointer++;
                }
            }
        }

        // Add any remaining text to the tree if applicable.
        if (textPointer < text.Length)
        {
            if (textPointer < composingRegion.Start && !composingWithinCurrentTextRange)
            {
                AddComposingRegionTextSpans(
                    textSpanTreeChildren,
                    text,
                    textPointer,
                    composingRegion,
                    style,
                    composingTextStyle);

                if (composingRegion.End != text.Length)
                {
                    textSpanTreeChildren.Add(new TextSpan(style: style, text: text[composingRegion.End..]));
                }
            }
            else
            {
                textSpanTreeChildren.Add(new TextSpan(style: style, text: text[textPointer..]));
            }
        }

        return textSpanTreeChildren;
    }

    /// <summary>Helper method to create the composing region text spans. Dart's
    /// <c>_addComposingRegionTextSpans</c>.</summary>
    private static void AddComposingRegionTextSpans(
        List<InlineSpan> treeChildren,
        string text,
        int start,
        TextRange composingRegion,
        TextStyle? style,
        TextStyle composingTextStyle)
    {
        treeChildren.Add(new TextSpan(style: style, text: text[start..composingRegion.Start]));
        treeChildren.Add(new TextSpan(
            style: composingTextStyle,
            text: text[composingRegion.Start..composingRegion.End]));
    }
}
