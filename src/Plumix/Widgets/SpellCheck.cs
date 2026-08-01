using Avalonia.Media;

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
            received = await PlatformHandler(locale, text).ConfigureAwait(false);
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
