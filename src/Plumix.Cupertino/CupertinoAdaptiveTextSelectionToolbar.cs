using Avalonia;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/adaptive_text_selection_toolbar.dart

/// <summary>Selects the mobile or desktop Cupertino text-selection toolbar for the platform.</summary>
public sealed class CupertinoAdaptiveTextSelectionToolbar : StatelessWidget
{
    public CupertinoAdaptiveTextSelectionToolbar(
        IReadOnlyList<Widget> children,
        TextSelectionToolbarAnchors anchors,
        Key? key = null) : this(children, null, anchors, key)
    {
        ArgumentNullException.ThrowIfNull(children);
    }

    private CupertinoAdaptiveTextSelectionToolbar(
        IReadOnlyList<Widget>? children,
        IReadOnlyList<ContextMenuButtonItem>? buttonItems,
        TextSelectionToolbarAnchors anchors,
        Key? key) : base(key)
    {
        Children = children;
        ButtonItems = buttonItems;
        Anchors = anchors;
    }

    public TextSelectionToolbarAnchors Anchors { get; }

    public IReadOnlyList<Widget>? Children { get; }

    public IReadOnlyList<ContextMenuButtonItem>? ButtonItems { get; }

    public static CupertinoAdaptiveTextSelectionToolbar FromButtonItems(
        IReadOnlyList<ContextMenuButtonItem> buttonItems,
        TextSelectionToolbarAnchors anchors,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        return new CupertinoAdaptiveTextSelectionToolbar(null, buttonItems, anchors, key);
    }

    public static CupertinoAdaptiveTextSelectionToolbar EditableText(
        EditableText.EditableTextState editableTextState,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        return FromButtonItems(
            editableTextState.ContextMenuButtonItems,
            editableTextState.ContextMenuAnchors,
            key);
    }

    public static CupertinoAdaptiveTextSelectionToolbar SelectableRegion(
        SelectableRegionState selectableRegionState,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(selectableRegionState);
        return FromButtonItems(
            selectableRegionState.ContextMenuButtonItems,
            selectableRegionState.ContextMenuAnchors,
            key);
    }

    public static CupertinoAdaptiveTextSelectionToolbar Editable(
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
        return new CupertinoAdaptiveTextSelectionToolbar(null, items, anchors, key);
    }

    public static IReadOnlyList<Widget> GetAdaptiveButtons(
        BuildContext context,
        IReadOnlyList<ContextMenuButtonItem> buttonItems)
    {
        ArgumentNullException.ThrowIfNull(buttonItems);
        TargetPlatform platform = PlatformDefaults.TargetPlatform;
        return platform is TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS
            ? buttonItems.Select(item => (Widget)CupertinoTextSelectionToolbarButton.FromButtonItem(item)).ToList()
            : buttonItems
                .Select(item => (Widget)CupertinoDesktopTextSelectionToolbarButton.FromButtonItem(item))
                .ToList();
    }

    public override Widget Build(BuildContext context)
    {
        int count = Children?.Count ?? ButtonItems?.Count ?? 0;
        if (count == 0)
        {
            return new SizedBox(width: 0.0, height: 0.0);
        }

        IReadOnlyList<Widget> children = Children ?? GetAdaptiveButtons(context, ButtonItems!);
        TargetPlatform platform = PlatformDefaults.TargetPlatform;
        if (platform is TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS)
        {
            return new CupertinoTextSelectionToolbar(
                anchorAbove: Anchors.PrimaryAnchor,
                anchorBelow: Anchors.SecondaryAnchor ?? Anchors.PrimaryAnchor,
                children: children);
        }

        return new CupertinoDesktopTextSelectionToolbar(
            anchor: Anchors.PrimaryAnchor,
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
