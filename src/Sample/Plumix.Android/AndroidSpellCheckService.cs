using Android.Content;
using Android.Views.TextService;
using Java.Util;
using Plumix.Widgets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plumix.Android;

// Dart parity source (host implementation):
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/SpellCheckPlugin.java

internal sealed class AndroidSpellCheckService : Java.Lang.Object,
    SpellCheckerSession.ISpellCheckerSessionListener
{
    private const int MaxSuggestions = 5;
    private readonly TextServicesManager _textServicesManager;
    private SpellCheckerSession? _session;
    private TaskCompletionSource<IReadOnlyList<SuggestionSpan>?>? _pendingResult;

    public AndroidSpellCheckService(Context context)
    {
        _textServicesManager = (TextServicesManager)context.GetSystemService(Context.TextServicesManagerService)!;
        Handler = FetchSpellCheckSuggestions;
    }

    public Func<Plumix.Widgets.Locale, string, Task<IReadOnlyList<SuggestionSpan>?>> Handler { get; }

    public Task<IReadOnlyList<SuggestionSpan>?> FetchSpellCheckSuggestions(
        Plumix.Widgets.Locale locale,
        string text)
    {
        if (_pendingResult is not null)
        {
            return Task.FromResult<IReadOnlyList<SuggestionSpan>?>(null);
        }

        _pendingResult = new TaskCompletionSource<IReadOnlyList<SuggestionSpan>?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _session ??= _textServicesManager.NewSpellCheckerSession(
            null,
            Java.Util.Locale.ForLanguageTag(locale.Name),
            this,
            referToSpellCheckerLanguageSettings: true);
        if (_session is null)
        {
            Complete(null);
            return Task.FromResult<IReadOnlyList<SuggestionSpan>?>(null);
        }

        Task<IReadOnlyList<SuggestionSpan>?> task = _pendingResult.Task;
        _session.GetSentenceSuggestions([new TextInfo(text)], MaxSuggestions);
        return task;
    }

    public void OnGetSentenceSuggestions(SentenceSuggestionsInfo[]? results)
    {
        if (results is not { Length: > 0 } || results[0] is null)
        {
            Complete([]);
            return;
        }

        SentenceSuggestionsInfo result = results[0];
        var spans = new List<SuggestionSpan>();
        for (int index = 0; index < result.SuggestionsCount; index++)
        {
            SuggestionsInfo? suggestionsInfo = result.GetSuggestionsInfoAt(index);
            if (suggestionsInfo is null)
            {
                continue;
            }
            var suggestions = new List<string>();
            for (int suggestionIndex = 0;
                 suggestionIndex < suggestionsInfo.SuggestionsCount;
                 suggestionIndex++)
            {
                string? suggestion = suggestionsInfo.GetSuggestionAt(suggestionIndex);
                if (!string.IsNullOrEmpty(suggestion))
                {
                    suggestions.Add(suggestion);
                }
            }

            if (suggestions.Count == 0)
            {
                continue;
            }

            int start = result.GetOffsetAt(index);
            spans.Add(new SuggestionSpan(
                new TextRange(start, start + result.GetLengthAt(index)),
                suggestions));
        }

        Complete(spans);
    }

    public void OnGetSuggestions(SuggestionsInfo[]? results)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session?.Close();
            _session?.Dispose();
            _session = null;
            Complete(null);
        }

        base.Dispose(disposing);
    }

    private void Complete(IReadOnlyList<SuggestionSpan>? result)
    {
        TaskCompletionSource<IReadOnlyList<SuggestionSpan>?>? pending = _pendingResult;
        _pendingResult = null;
        pending?.TrySetResult(result);
    }
}
