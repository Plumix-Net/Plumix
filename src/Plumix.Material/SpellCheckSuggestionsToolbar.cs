using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/spell_check_suggestions_toolbar.dart
// flutter/packages/flutter/lib/src/material/spell_check_suggestions_toolbar_layout_delegate.dart

/// <summary>The default Android Material toolbar for spell-check suggestions.</summary>
public sealed class SpellCheckSuggestionsToolbar : StatelessWidget
{
    internal const double DefaultToolbarHeight = 193.0;
    internal const int MaxSuggestions = 3;
    internal const double ToolbarWidth = 165.0;

    public SpellCheckSuggestionsToolbar(
        Point anchor,
        IReadOnlyList<ContextMenuButtonItem> buttonItems,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        if (buttonItems.Count > MaxSuggestions + 1)
        {
            throw new ArgumentException("A spell-check toolbar supports at most four items.", nameof(buttonItems));
        }

        Anchor = anchor;
        ButtonItems = buttonItems;
    }

    public Point Anchor { get; }

    public IReadOnlyList<ContextMenuButtonItem> ButtonItems { get; }

    public static SpellCheckSuggestionsToolbar EditableText(EditableText.EditableTextState editableTextState)
    {
        return new SpellCheckSuggestionsToolbar(
            GetToolbarAnchor(editableTextState.ContextMenuAnchors),
            BuildButtonItems(editableTextState) ?? []);
    }

    public static IReadOnlyList<ContextMenuButtonItem>? BuildButtonItems(
        EditableText.EditableTextState editableTextState)
    {
        SuggestionSpan? span = editableTextState.FindSuggestionSpanAtCursorIndex(
            editableTextState.CurrentTextEditingValue.Selection.BaseOffset);
        if (span is null) return null;
        var items = new List<ContextMenuButtonItem>();
        foreach (string suggestion in span.Suggestions.Take(MaxSuggestions))
        {
            string replacement = suggestion;
            items.Add(new ContextMenuButtonItem(
                () => editableTextState.ReplaceText(span.Range, replacement),
                label: replacement));
        }
        items.Add(new ContextMenuButtonItem(
            () => editableTextState.ReplaceText(span.Range, string.Empty),
            ContextMenuButtonType.Delete));
        return items;
    }

    public static Point GetToolbarAnchor(TextSelectionToolbarAnchors anchors)
    {
        return anchors.SecondaryAnchor ?? anchors.PrimaryAnchor;
    }

    public override Widget Build(BuildContext context)
    {
        if (ButtonItems.Count == 0)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        double toolbarHeight = DefaultToolbarHeight - (48.0 * (4 - ButtonItems.Count));
        MediaQueryData mediaQuery = MediaQuery.Of(context);
        double paddingAbove = mediaQuery.Padding.Top + TextSelectionToolbar.ToolbarScreenPadding;
        double screenPadding = TextSelectionToolbar.ToolbarScreenPadding;
        var localAdjustment = new Vector(screenPadding, paddingAbove);
        var children = new List<Widget>(ButtonItems.Count);
        foreach (ContextMenuButtonItem item in ButtonItems)
        {
            children.Add(BuildToolbarButton(context, item));
        }

        Widget toolbar = new Material(
            elevation: 2.0,
            type: MaterialType.Card,
            child: new SizedBox(
                width: ToolbarWidth,
                height: toolbarHeight,
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: children)));

        return new Padding(
            new Thickness(
                screenPadding,
                paddingAbove,
                screenPadding,
                screenPadding + mediaQuery.ViewInsets.Bottom),
            new CustomSingleChildLayout(
                new SpellCheckSuggestionsToolbarLayoutDelegate(Anchor - localAdjustment),
                new AnimatedSize(
                    duration: TimeSpan.FromMilliseconds(140),
                    child: toolbar)));
    }

    private static Widget BuildToolbarButton(BuildContext context, ContextMenuButtonItem item)
    {
        Widget button = new TextSelectionToolbarTextButton(
            padding: new Thickness(20.0, 0.0, 0.0, 0.0),
            onPressed: item.OnPressed,
            alignment: Alignment.CenterLeft,
            child: new Text(
                AdaptiveTextSelectionToolbar.GetButtonLabel(context, item),
                color: item.Type == ContextMenuButtonType.Delete
                    ? Color.Parse("#FF2196F3")
                    : null));

        if (item.Type != ContextMenuButtonType.Delete)
        {
            return button;
        }

        return new Stack(
            fit: StackFit.Passthrough,
            clipBehavior: Clip.None,
            children:
            [
                button,
                new Positioned(
                    left: 0.0,
                    top: 0.0,
                    right: 0.0,
                    height: 1.0,
                    child: new ColoredBox(Color.Parse("#FF9E9E9E"))),
            ]);
    }
}

public sealed class SpellCheckSuggestionsToolbarLayoutDelegate : SingleChildLayoutDelegate
{
    public SpellCheckSuggestionsToolbarLayoutDelegate(Point anchor)
    {
        Anchor = anchor;
    }

    public Point Anchor { get; }

    public override BoxConstraints GetConstraintsForChild(BoxConstraints constraints) => constraints.Loosen();

    public override Point GetPositionForChild(Size size, Size childSize)
    {
        return new Point(
            TextSelectionToolbarLayoutDelegate.CenterOn(Anchor.X, childSize.Width, size.Width),
            Anchor.Y + childSize.Height > size.Height
                ? size.Height - childSize.Height
                : Anchor.Y);
    }

    public override bool ShouldRelayout(SingleChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not SpellCheckSuggestionsToolbarLayoutDelegate oldToolbarDelegate
               || oldToolbarDelegate.Anchor != Anchor;
    }
}
