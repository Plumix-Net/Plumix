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

/// <summary>Owns the route-backed presentation of a Flutter-style context menu.</summary>
public sealed class ContextMenuController
{
    private static ContextMenuController? _shownInstance;
    private ContextMenuRoute? _route;
    private NavigatorState? _navigator;

    public ContextMenuController(Action? onRemove = null)
    {
        OnRemove = onRemove;
    }

    public Action? OnRemove { get; }

    public bool IsShown => ReferenceEquals(_shownInstance, this);

    public bool Show(BuildContext context, Func<BuildContext, Widget> contextMenuBuilder)
    {
        ArgumentNullException.ThrowIfNull(contextMenuBuilder);
        RemoveAny();

        NavigatorState? navigator = Navigator.MaybeOf(context, rootNavigator: true);
        if (navigator is null)
        {
            return false;
        }

        _navigator = navigator;
        _route = new ContextMenuRoute(
            contextMenuBuilder,
            onDismiss: Hide,
            onRemoved: HandleRouteRemoved);
        _shownInstance = this;
        navigator.Push(_route);
        return true;
    }

    public void Hide()
    {
        Remove();
    }

    public void Remove()
    {
        if (!IsShown)
        {
            return;
        }

        RemoveRoute();
    }

    public static void RemoveAny()
    {
        _shownInstance?.RemoveRoute();
    }

    private void RemoveRoute()
    {
        ContextMenuRoute? route = _route;
        NavigatorState? navigator = _navigator;
        _route = null;
        _navigator = null;
        if (ReferenceEquals(_shownInstance, this))
        {
            _shownInstance = null;
        }
        if (route is not null && navigator is not null)
        {
            navigator.RemoveRoute(route);
        }
        OnRemove?.Invoke();
    }

    private void HandleRouteRemoved(ContextMenuRoute route)
    {
        if (!ReferenceEquals(_route, route))
        {
            return;
        }

        _route = null;
        _navigator = null;
        if (ReferenceEquals(_shownInstance, this))
        {
            _shownInstance = null;
        }
        OnRemove?.Invoke();
    }

    private sealed class ContextMenuRoute : PageRoute
    {
        private readonly Func<BuildContext, Widget> _builder;
        private readonly Action _onDismiss;
        private readonly Action<ContextMenuRoute> _onRemoved;
        private bool _removed;

        public ContextMenuRoute(
            Func<BuildContext, Widget> builder,
            Action onDismiss,
            Action<ContextMenuRoute> onRemoved)
        {
            _builder = builder;
            _onDismiss = onDismiss;
            _onRemoved = onRemoved;
        }

        public override bool Opaque => false;

        public override Widget BuildPage(BuildContext context)
        {
            return new GestureDetector(
                behavior: HitTestBehavior.Translucent,
                onTap: _onDismiss,
                child: new Stack(
                    fit: StackFit.Expand,
                    clipBehavior: Clip.None,
                    children: [_builder(context)]));
        }

        public override void DidPop(Route? previousRoute)
        {
            base.DidPop(previousRoute);
            NotifyRemoved();
        }

        public override void Dispose()
        {
            NotifyRemoved();
            base.Dispose();
        }

        private void NotifyRemoved()
        {
            if (_removed)
            {
                return;
            }

            _removed = true;
            _onRemoved(this);
        }
    }
}
