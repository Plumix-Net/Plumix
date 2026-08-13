using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source:
// flutter/packages/flutter/lib/src/material/adaptive_text_selection_toolbar.dart

/// <summary>Selects the default Material text-selection toolbar for the ambient platform.</summary>
public sealed class AdaptiveTextSelectionToolbar : StatelessWidget
{
    public AdaptiveTextSelectionToolbar(
        IReadOnlyList<Widget> children,
        TextSelectionToolbarAnchors anchors,
        Key? key = null) : this(children, null, anchors, key)
    {
        ArgumentNullException.ThrowIfNull(children);
    }

    private AdaptiveTextSelectionToolbar(
        IReadOnlyList<Widget>? children,
        IReadOnlyList<ContextMenuButtonItem>? buttonItems,
        TextSelectionToolbarAnchors anchors,
        Key? key) : base(key)
    {
        Children = children;
        ButtonItems = buttonItems;
        Anchors = anchors;
    }

    public IReadOnlyList<ContextMenuButtonItem>? ButtonItems { get; }

    public IReadOnlyList<Widget>? Children { get; }

    public TextSelectionToolbarAnchors Anchors { get; }

    public static AdaptiveTextSelectionToolbar FromButtonItems(
        IReadOnlyList<ContextMenuButtonItem> buttonItems,
        TextSelectionToolbarAnchors anchors,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        return new AdaptiveTextSelectionToolbar(null, buttonItems, anchors, key);
    }

    public static AdaptiveTextSelectionToolbar EditableText(
        EditableText.EditableTextState editableTextState,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        return FromButtonItems(
            editableTextState.ContextMenuButtonItems,
            editableTextState.ContextMenuAnchors,
            key);
    }

    public static AdaptiveTextSelectionToolbar SelectableRegion(
        SelectableRegionState selectableRegionState,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(selectableRegionState);
        return FromButtonItems(
            selectableRegionState.ContextMenuButtonItems,
            selectableRegionState.ContextMenuAnchors,
            key);
    }

    public static AdaptiveTextSelectionToolbar Editable(
        Action? onCopy,
        Action? onCut,
        Action? onPaste,
        Action? onSelectAll,
        Action? onLookUp,
        Action? onSearchWeb,
        Action? onShare,
        Action? onLiveTextInput,
        TextSelectionToolbarAnchors anchors,
        Key? key = null)
    {
        var items = new List<ContextMenuButtonItem>();
        AddItem(items, onCut, ContextMenuButtonType.Cut);
        AddItem(items, onCopy, ContextMenuButtonType.Copy);
        AddItem(items, onPaste, ContextMenuButtonType.Paste);
        AddItem(items, onSelectAll, ContextMenuButtonType.SelectAll);
        AddItem(items, onLookUp, ContextMenuButtonType.LookUp);
        AddItem(items, onSearchWeb, ContextMenuButtonType.SearchWeb);
        AddItem(items, onShare, ContextMenuButtonType.Share);
        AddItem(items, onLiveTextInput, ContextMenuButtonType.LiveTextInput);
        return new AdaptiveTextSelectionToolbar(null, items, anchors, key);
    }

    public static string GetButtonLabel(BuildContext context, ContextMenuButtonItem buttonItem)
    {
        ArgumentNullException.ThrowIfNull(buttonItem);
        string? label = buttonItem.Label;
        if (label is not null)
        {
            return label;
        }

        TargetPlatform platform = Theme.Of(context).Platform;
        if (platform is TargetPlatform.IOS or TargetPlatform.MacOS)
        {
            return CupertinoTextSelectionToolbarButton.GetButtonLabel(context, buttonItem);
        }

        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        return buttonItem.Type switch
        {
            ContextMenuButtonType.Cut => localizations.CutButtonLabel,
            ContextMenuButtonType.Copy => localizations.CopyButtonLabel,
            ContextMenuButtonType.Paste => localizations.PasteButtonLabel,
            ContextMenuButtonType.SelectAll => localizations.SelectAllButtonLabel,
            ContextMenuButtonType.Delete => localizations.DeleteButtonTooltip.ToUpperInvariant(),
            ContextMenuButtonType.LookUp => localizations.LookUpButtonLabel,
            ContextMenuButtonType.SearchWeb => localizations.SearchWebButtonLabel,
            ContextMenuButtonType.Share => localizations.ShareButtonLabel,
            ContextMenuButtonType.LiveTextInput => localizations.ScanTextButtonLabel,
            ContextMenuButtonType.Custom => string.Empty,
            _ => string.Empty,
        };
    }

    public static IReadOnlyList<Widget> GetAdaptiveButtons(
        BuildContext context,
        IReadOnlyList<ContextMenuButtonItem> buttonItems)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        TargetPlatform platform = Theme.Of(context).Platform;
        TextDirection textDirection = Directionality.Of(context);
        var buttons = new List<Widget>(buttonItems.Count);
        for (int index = 0; index < buttonItems.Count; index++)
        {
            ContextMenuButtonItem item = buttonItems[index];
            string label = GetButtonLabel(context, item);
            if (platform == TargetPlatform.IOS)
            {
                buttons.Add(CupertinoTextSelectionToolbarButton.FromButtonItem(item));
                continue;
            }

            if (platform == TargetPlatform.MacOS)
            {
                buttons.Add(CupertinoDesktopTextSelectionToolbarButton.TextButton(item.OnPressed, label));
                continue;
            }

            if (platform is TargetPlatform.Linux or TargetPlatform.Windows)
            {
                buttons.Add(DesktopTextSelectionToolbarButton.Text(context, item.OnPressed, label));
                continue;
            }

            buttons.Add(new TextSelectionToolbarTextButton(
                child: new Text(label),
                padding: TextSelectionToolbarTextButton.GetPadding(index, buttonItems.Count, textDirection),
                onPressed: item.OnPressed,
                alignment: textDirection == TextDirection.Rtl
                    ? Alignment.CenterRight
                    : Alignment.CenterLeft));
        }

        return buttons;
    }

    public override Widget Build(BuildContext context)
    {
        int count = Children?.Count ?? ButtonItems?.Count ?? 0;
        if (count == 0)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        IReadOnlyList<Widget> children = Children ?? GetAdaptiveButtons(context, ButtonItems!);
        TargetPlatform platform = Theme.Of(context).Platform;
        if (platform == TargetPlatform.IOS)
        {
            return new CupertinoTextSelectionToolbar(
                anchorAbove: Anchors.PrimaryAnchor,
                anchorBelow: Anchors.SecondaryAnchor ?? Anchors.PrimaryAnchor,
                children: children);
        }

        if (platform == TargetPlatform.MacOS)
        {
            return new CupertinoDesktopTextSelectionToolbar(
                anchor: Anchors.PrimaryAnchor,
                children: children);
        }

        if (platform is TargetPlatform.Fuchsia
            or TargetPlatform.Linux
            or TargetPlatform.Windows)
        {
            return new DesktopTextSelectionToolbar(
                anchor: Anchors.PrimaryAnchor,
                children: children);
        }

        Point anchorBelow = Anchors.SecondaryAnchor ?? Anchors.PrimaryAnchor;
        return new TextSelectionToolbar(
            anchorAbove: Anchors.PrimaryAnchor,
            anchorBelow: anchorBelow,
            children: children);
    }

    private static void AddItem(
        ICollection<ContextMenuButtonItem> items,
        Action? callback,
        ContextMenuButtonType type)
    {
        if (callback is not null)
        {
            items.Add(new ContextMenuButtonItem(callback, type));
        }
    }
}
