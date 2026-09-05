using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/context_menu_controller.dart
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

public delegate Widget EditableTextContextMenuBuilder(
    BuildContext context,
    EditableText.EditableTextState editableTextState);

public delegate Widget SelectableRegionContextMenuBuilder(
    BuildContext context,
    SelectableRegionState selectableRegionState);

/// <summary>Builds and manages the single application-wide context menu in the root overlay.</summary>
public sealed class ContextMenuController
{
    public ContextMenuController(Action? onRemove = null)
    {
        OnRemove = onRemove;
    }

    public Action? OnRemove { get; }

    private static Func<BuildContext, Widget>? _contextMenuBuilder;
    private static ContextMenuController? _shownInstance;
    private static OverlayEntry? _menuOverlayEntry;

    public void Show(
        BuildContext context,
        Func<BuildContext, Widget> contextMenuBuilder,
        Widget? debugRequiredFor = null)
    {
        ArgumentNullException.ThrowIfNull(contextMenuBuilder);
        if (IsShown)
        {
            _contextMenuBuilder = contextMenuBuilder;
            _menuOverlayEntry?.MarkNeedsBuild();
            return;
        }

        RemoveAny();
        OverlayState overlayState = Overlay.Of(context, rootOverlay: true, debugRequiredFor: debugRequiredFor);
        _contextMenuBuilder = contextMenuBuilder;
        _menuOverlayEntry = new OverlayEntry(menuContext =>
        {
            CapturedThemes capturedThemes = InheritedTheme.Capture(
                from: menuContext,
                to: Navigator.MaybeOf(menuContext)?.Context);
            return capturedThemes.Wrap(_contextMenuBuilder!(menuContext));
        });
        _shownInstance = this;
        overlayState.Insert(_menuOverlayEntry);
    }

    public static void RemoveAny()
    {
        _menuOverlayEntry?.Remove();
        _menuOverlayEntry?.Dispose();
        _menuOverlayEntry = null;
        _contextMenuBuilder = null;
        if (_shownInstance is not null)
        {
            _shownInstance.OnRemove?.Invoke();
            _shownInstance = null;
        }
    }

    public bool IsShown => ReferenceEquals(_shownInstance, this);

    public void MarkNeedsBuild()
    {
        if (Constants.KDebugMode && !IsShown)
        {
            throw new AssertionError("The context menu must be shown before marking it dirty.");
        }
        _menuOverlayEntry?.MarkNeedsBuild();
    }

    public void Remove()
    {
        if (!IsShown)
        {
            return;
        }
        RemoveAny();
    }

    /// <summary>Alias for <see cref="Remove"/>.</summary>
    public void Hide() => Remove();
}
