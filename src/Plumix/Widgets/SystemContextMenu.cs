using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/system_context_menu.dart

namespace Plumix.Widgets;

/// <summary>Displays the system context menu on top of the Flutter view.</summary>
/// <remarks>
/// <para>
/// Dart's <c>SystemContextMenu</c>. Currently, only supports iOS 16.0 and above and displays nothing
/// on other platforms: check <see cref="IsSupported"/> (or <see cref="IsSupportedByField"/>) before
/// building it, otherwise it asserts. The menu itself is drawn by the platform; this widget renders
/// an empty box and drives a <see cref="SystemContextMenuController"/>.
/// </para>
/// <para>
/// Just like a Flutter-drawn context menu, the system context menu should be removed from the tree
/// when it is hidden; <see cref="OnSystemHide"/> is called when the system hides it, for example
/// when the user taps outside of it.
/// </para>
/// </remarks>
public sealed class SystemContextMenu : StatefulWidget
{
    private SystemContextMenu(
        Rect anchor,
        IReadOnlyList<IOSSystemContextMenuItem> items,
        Action? onSystemHide,
        Key? key) : base(key)
    {
        Anchor = anchor;
        Items = items ?? throw new ArgumentNullException(nameof(items));
        OnSystemHide = onSystemHide;
    }

    /// <summary>
    /// Creates an instance of <see cref="SystemContextMenu"/> for the field indicated by the given
    /// <see cref="EditableText.EditableTextState"/>.
    /// </summary>
    /// <remarks>
    /// Dart's <c>SystemContextMenu.editableText</c> factory: the anchor is the selection rect, the
    /// items default to <see cref="GetDefaultItems"/>, and a system hide hides the toolbar but keeps
    /// the selection handles.
    /// </remarks>
    public static SystemContextMenu EditableText(
        EditableText.EditableTextState editableTextState,
        IReadOnlyList<IOSSystemContextMenuItem>? items = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        (double startGlyphHeight, double endGlyphHeight) = editableTextState.GetGlyphHeights();
        RenderEditable renderEditable = editableTextState.RenderEditableObject;
        return new SystemContextMenu(
            anchor: TextSelectionToolbarAnchors.GetSelectionRect(
                renderEditable,
                startGlyphHeight,
                endGlyphHeight,
                renderEditable.GetEndpointsForSelection(editableTextState.TextEditingValue.Selection)),
            items: items ?? GetDefaultItems(editableTextState),
            onSystemHide: () => editableTextState.HideToolbar(false),
            key: key);
    }

    /// <summary>The <see cref="Rect"/> that the context menu should point to.</summary>
    public Rect Anchor { get; }

    /// <summary>
    /// A list of the items to be displayed in the system context menu.
    /// </summary>
    /// <remarks>
    /// When passed, items will be shown regardless of the state of text input. For example, a copy
    /// item will be shown even if there is no selection. When empty, nothing is shown.
    /// </remarks>
    public IReadOnlyList<IOSSystemContextMenuItem> Items { get; }

    /// <summary>Called when the system hides this context menu.</summary>
    /// <remarks>
    /// For example, tapping outside of the context menu typically causes the system to hide the
    /// menu. Not called when showing a new system context menu causes another to be hidden.
    /// </remarks>
    public Action? OnSystemHide { get; }

    /// <summary>Whether the current device supports showing the system context menu.</summary>
    /// <remarks>Currently, this is only supported on newer versions of iOS.</remarks>
    public static bool IsSupported(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return PlatformDefaults.TargetPlatform == TargetPlatform.IOS
               && (MediaQuery.MaybeSupportsShowingSystemContextMenu(context) ?? false);
    }

    /// <summary>
    /// Whether the given field supports showing the system context menu.
    /// </summary>
    /// <remarks>
    /// Currently <see cref="SystemContextMenu"/> is only supported with an active
    /// <see cref="TextInputConnection"/>, so a read-only field never shows it.
    /// </remarks>
    public static bool IsSupportedByField(EditableText.EditableTextState editableTextState)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        return !editableTextState.Widget.ReadOnly && IsSupported(editableTextState.Context);
    }

    /// <summary>
    /// The default <see cref="Items"/> for the given <see cref="EditableText.EditableTextState"/>.
    /// </summary>
    /// <remarks>
    /// For <see cref="IOSSystemContextMenuItem"/>s that support it, the platform localizes the title;
    /// the order follows <see cref="EditableText.EditableTextState.ContextMenuButtonItems"/>, and
    /// delete and custom buttons (including the text processing actions) are left out.
    /// </remarks>
    public static IReadOnlyList<IOSSystemContextMenuItem> GetDefaultItems(
        EditableText.EditableTextState editableTextState)
    {
        ArgumentNullException.ThrowIfNull(editableTextState);
        var items = new List<IOSSystemContextMenuItem>();
        foreach (ContextMenuButtonItem button in editableTextState.ContextMenuButtonItems)
        {
            IOSSystemContextMenuItem? item = button.Type switch
            {
                ContextMenuButtonType.Copy => new IOSSystemContextMenuItemCopy(),
                ContextMenuButtonType.Cut => new IOSSystemContextMenuItemCut(),
                ContextMenuButtonType.Paste => new IOSSystemContextMenuItemPaste(),
                ContextMenuButtonType.SelectAll => new IOSSystemContextMenuItemSelectAll(),
                ContextMenuButtonType.LookUp => new IOSSystemContextMenuItemLookUp(),
                ContextMenuButtonType.SearchWeb => new IOSSystemContextMenuItemSearchWeb(),
                ContextMenuButtonType.Share => new IOSSystemContextMenuItemShare(),
                ContextMenuButtonType.LiveTextInput => new IOSSystemContextMenuItemLiveText(),
                ContextMenuButtonType.Delete or ContextMenuButtonType.Custom => null,
                _ => null,
            };
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public override State CreateState() => new SystemContextMenuState();

    private sealed class SystemContextMenuState : State<SystemContextMenu>
    {
        private SystemContextMenuController _systemContextMenuController = null!;

        public override void InitState()
        {
            base.InitState();
            _systemContextMenuController = new SystemContextMenuController(onSystemHide: Widget.OnSystemHide);
        }

        public override void Dispose()
        {
            _systemContextMenuController.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            if (Constants.KDebugMode && !IsSupported(context))
            {
                throw new AssertionError();
            }

            if (Widget.Items.Count > 0)
            {
                WidgetsLocalizations localizations = WidgetsLocalizations.Of(context);
                List<IOSSystemContextMenuItemData> itemDatas =
                    Widget.Items.Select(item => item.GetData(localizations)).ToList();
                _ = _systemContextMenuController.ShowWithItems(Widget.Anchor, itemDatas);
            }

            return SizedBox.Shrink();
        }
    }
}

/// <summary>Describes a context menu button that will be rendered in the iOS system context menu.</summary>
/// <remarks>
/// <para>
/// Dart's <c>sealed class IOSSystemContextMenuItem</c>; C# closes the hierarchy with an internal
/// constructor (only <see cref="IOSSystemContextMenuItemCustom"/> is open, as in Dart).
/// </para>
/// <para>
/// By default, <see cref="SystemContextMenu"/> will display the relevant items, but they can be
/// customized by passing <see cref="SystemContextMenu"/> items explicitly.
/// </para>
/// </remarks>
public abstract class IOSSystemContextMenuItem
{
    private protected IOSSystemContextMenuItem()
    {
    }

    /// <summary>The text to display to the user.</summary>
    /// <remarks>Not exposed for some built-in menu items whose title is always set by the platform.</remarks>
    public virtual string? Title => null;

    /// <summary>
    /// Returns the representation of this class used by method channels.
    /// </summary>
    public abstract IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations);

    public override int GetHashCode() => Title?.GetHashCode() ?? 0;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is IOSSystemContextMenuItem other && other.Title == Title;
    }

    /// <summary>Dart's <c>Diagnosticable.toString</c> for the classes that mix it in.</summary>
    private protected static string DiagnosticableToString(IDiagnosticable self)
    {
        return Constants.KDebugMode
            ? self.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, DiagnosticLevel.Info)
            : self.ToStringShort();
    }
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in copy button.
/// </summary>
/// <remarks>Should only appear when there is a selection that can be copied. The title and action are
/// both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemCopy : IOSSystemContextMenuItem
{
    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataCopy();
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in cut button.
/// </summary>
/// <remarks>Should only appear when there is a selection that can be cut. The title and action are
/// both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemCut : IOSSystemContextMenuItem
{
    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataCut();
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in paste button.
/// </summary>
/// <remarks>Should only appear when the field can receive pasted content. The title and action are
/// both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemPaste : IOSSystemContextMenuItem
{
    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataPaste();
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in select all
/// button.
/// </summary>
/// <remarks>Should only appear when the field can have its selection changed. The title and action
/// are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemSelectAll : IOSSystemContextMenuItem
{
    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataSelectAll();
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in look up
/// button.
/// </summary>
/// <remarks>Should only appear when content is selected. The action is handled by the platform; the
/// title defaults to <see cref="WidgetsLocalizations.LookUpButtonLabel"/>.</remarks>
public sealed class IOSSystemContextMenuItemLookUp : IOSSystemContextMenuItem, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemLookUp"/>.</summary>
    public IOSSystemContextMenuItemLookUp(string? title = null)
    {
        Title = title;
    }

    /// <inheritdoc />
    public override string? Title { get; }

    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations)
    {
        ArgumentNullException.ThrowIfNull(localizations);
        return new IOSSystemContextMenuItemDataLookUp(title: Title ?? localizations.LookUpButtonLabel);
    }

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<string>("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in search web
/// button.
/// </summary>
/// <remarks>Should only appear when content is selected. The action is handled by the platform; the
/// title defaults to <see cref="WidgetsLocalizations.SearchWebButtonLabel"/>.</remarks>
public sealed class IOSSystemContextMenuItemSearchWeb : IOSSystemContextMenuItem, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemSearchWeb"/>.</summary>
    public IOSSystemContextMenuItemSearchWeb(string? title = null)
    {
        Title = title;
    }

    /// <inheritdoc />
    public override string? Title { get; }

    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations)
    {
        ArgumentNullException.ThrowIfNull(localizations);
        return new IOSSystemContextMenuItemDataSearchWeb(title: Title ?? localizations.SearchWebButtonLabel);
    }

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<string>("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in share
/// button.
/// </summary>
/// <remarks>Opens the system share dialog. Should only appear when content is selected. The action is
/// handled by the platform; the title defaults to <see cref="WidgetsLocalizations.ShareButtonLabel"/>.
/// </remarks>
public sealed class IOSSystemContextMenuItemShare : IOSSystemContextMenuItem, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemShare"/>.</summary>
    public IOSSystemContextMenuItemShare(string? title = null)
    {
        Title = title;
    }

    /// <inheritdoc />
    public override string? Title { get; }

    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations)
    {
        ArgumentNullException.ThrowIfNull(localizations);
        return new IOSSystemContextMenuItemDataShare(title: Title ?? localizations.ShareButtonLabel);
    }

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for the system's built-in Live Text
/// button.
/// </summary>
/// <remarks>Should only appear when the field can receive text input. The title and action are both
/// handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemLiveText : IOSSystemContextMenuItem
{
    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataLiveText();
}

/// <summary>
/// Creates an instance of <see cref="IOSSystemContextMenuItem"/> for custom action buttons defined by
/// the developer.
/// </summary>
/// <remarks>
/// Only available on iOS 16.0 and later. The title and action are both defined by the developer; the
/// item's hash code (as a string) is the callback id the platform sends back, matching
/// <see cref="IOSSystemContextMenuItemDataCustom.CallbackId"/>.
/// </remarks>
public class IOSSystemContextMenuItemCustom : IOSSystemContextMenuItem, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemCustom"/>.</summary>
    public IOSSystemContextMenuItemCustom(string title, Action onPressed)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
    }

    /// <summary>The text to display to the user.</summary>
    public override string Title { get; }

    /// <summary>The callback to be called when the menu item is pressed.</summary>
    public Action OnPressed { get; }

    public override IOSSystemContextMenuItemData GetData(WidgetsLocalizations localizations) =>
        new IOSSystemContextMenuItemDataCustom(title: Title, onPressed: OnPressed);

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
        properties.Add(ObjectFlagProperty<Action>.Has("onPressed", OnPressed));
    }

    public override int GetHashCode() => IOSSystemContextMenuItemData.HashTitleAndCallback(Title, OnPressed);

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj)
               || (obj is IOSSystemContextMenuItemCustom other
                   && other.Title == Title
                   && Equals(other.OnPressed, OnPressed));
    }

    public override string ToString() => DiagnosticableToString(this);
}
