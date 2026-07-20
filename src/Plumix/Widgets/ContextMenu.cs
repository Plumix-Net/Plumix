using Avalonia;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/context_menu_button_item.dart
// flutter/packages/flutter/lib/src/widgets/text_selection_toolbar_anchors.dart

public enum ContextMenuButtonType
{
    Cut,
    Copy,
    Paste,
    SelectAll,
    Delete,
    LookUp,
    SearchWeb,
    Share,
    LiveTextInput,
    Custom,
}

public sealed class ContextMenuButtonItem
{
    public ContextMenuButtonItem(
        Action? onPressed,
        ContextMenuButtonType type = ContextMenuButtonType.Custom,
        string? label = null)
    {
        OnPressed = onPressed;
        Type = type;
        Label = label;
    }

    public Action? OnPressed { get; }

    public ContextMenuButtonType Type { get; }

    public string? Label { get; }
}

public readonly record struct TextSelectionToolbarAnchors(
    Point PrimaryAnchor,
    Point? SecondaryAnchor = null);
