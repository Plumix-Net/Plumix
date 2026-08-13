using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/spell_check_suggestions_toolbar.dart

/// <summary>The default iOS spell-check suggestions toolbar.</summary>
public sealed class CupertinoSpellCheckSuggestionsToolbar : StatelessWidget
{
    public const int MaxSuggestions = 3;

    public CupertinoSpellCheckSuggestionsToolbar(
        TextSelectionToolbarAnchors anchors,
        IReadOnlyList<ContextMenuButtonItem> buttonItems,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        if (buttonItems.Count > MaxSuggestions)
        {
            throw new ArgumentException("A Cupertino spell-check toolbar supports at most three items.",
                nameof(buttonItems));
        }

        Anchors = anchors;
        ButtonItems = buttonItems;
    }

    public TextSelectionToolbarAnchors Anchors { get; }

    public IReadOnlyList<ContextMenuButtonItem> ButtonItems { get; }

    public static CupertinoSpellCheckSuggestionsToolbar EditableText(
        EditableText.EditableTextState editableTextState,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        return new CupertinoSpellCheckSuggestionsToolbar(
            editableTextState.ContextMenuAnchors,
            BuildButtonItems(editableTextState) ?? [],
            key);
    }

    public static IReadOnlyList<ContextMenuButtonItem>? BuildButtonItems(
        EditableText.EditableTextState editableTextState)
    {
        SuggestionSpan? span = editableTextState.FindSuggestionSpanAtCursorIndex(
            editableTextState.CurrentTextEditingValue.Selection.BaseOffset);
        if (span is null)
        {
            return null;
        }

        if (span.Suggestions.Count == 0)
        {
            CupertinoLocalizations localizations = CupertinoLocalizations.Of(editableTextState.Context);
            return [new ContextMenuButtonItem(null, label: localizations.NoSpellCheckReplacementsLabel)];
        }

        var items = new List<ContextMenuButtonItem>();
        foreach (string suggestion in span.Suggestions.Take(MaxSuggestions))
        {
            string replacement = suggestion;
            items.Add(new ContextMenuButtonItem(
                () =>
                {
                    if (editableTextState.Mounted)
                    {
                        editableTextState.ReplaceText(span.Range, replacement);
                    }
                },
                label: replacement));
        }

        return items;
    }

    public override Widget Build(BuildContext context)
    {
        if (ButtonItems.Count == 0)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        return new CupertinoTextSelectionToolbar(
            anchorAbove: Anchors.PrimaryAnchor,
            anchorBelow: Anchors.SecondaryAnchor ?? Anchors.PrimaryAnchor,
            children: ButtonItems
                .Select(item => (Widget)CupertinoTextSelectionToolbarButton.FromButtonItem(item))
                .ToList());
    }
}
