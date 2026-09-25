using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

/// <summary>
/// A clipboard status notifier for the web, where the clipboard is always considered pasteable.
/// Dart's <c>_WebClipboardStatusNotifier</c>.
/// </summary>
/// <remarks>Calling <c>Clipboard.hasStrings</c> on the web shows a browser permission prompt, so
/// the status is never queried.</remarks>
internal sealed class WebClipboardStatusNotifier : ClipboardStatusNotifier
{
    private ClipboardStatus _value = ClipboardStatus.Pasteable;

    // Dart overrides the field with a plain one: assigning it does not notify.
    public override ClipboardStatus Value
    {
        get => _value;
        set => _value = value;
    }

    public override Task Update() => Task.CompletedTask;
}

public sealed partial class EditableText
{
    /// <summary>
    /// Returns the <see cref="ContextMenuButtonItem"/>s for the given callbacks, in the platform's
    /// order. Dart's <c>EditableText.getEditableButtonItems</c>.
    /// </summary>
    /// <remarks>Nothing depending on the clipboard is returned while <paramref name="onPaste"/> is
    /// given and <paramref name="clipboardStatus"/> is still <see cref="ClipboardStatus.Unknown"/>;
    /// the Live Text button is always last.</remarks>
    public static List<ContextMenuButtonItem> GetEditableButtonItems(
        ClipboardStatus? clipboardStatus,
        Action? onCopy,
        Action? onCut,
        Action? onPaste,
        Action? onSelectAll,
        Action? onLookUp,
        Action? onSearchWeb,
        Action? onShare,
        Action? onLiveTextInput)
    {
        var resultButtonItem = new List<ContextMenuButtonItem>();

        // Configure button items with clipboard.
        if (onPaste is null || clipboardStatus != ClipboardStatus.Unknown)
        {
            // If the paste button is enabled, don't render anything until the state of the
            // clipboard is known, since it's used to determine if paste is shown.

            // On Android, the share button is before the select all button.
            bool showShareBeforeSelectAll = PlatformDefaults.TargetPlatform == TargetPlatform.Android;

            if (onCut is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onCut, ContextMenuButtonType.Cut));
            }

            if (onCopy is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onCopy, ContextMenuButtonType.Copy));
            }

            if (onPaste is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onPaste, ContextMenuButtonType.Paste));
            }

            if (onShare is not null && showShareBeforeSelectAll)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onShare, ContextMenuButtonType.Share));
            }

            if (onSelectAll is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onSelectAll, ContextMenuButtonType.SelectAll));
            }

            if (onLookUp is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onLookUp, ContextMenuButtonType.LookUp));
            }

            if (onSearchWeb is not null)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onSearchWeb, ContextMenuButtonType.SearchWeb));
            }

            if (onShare is not null && !showShareBeforeSelectAll)
            {
                resultButtonItem.Add(new ContextMenuButtonItem(onShare, ContextMenuButtonType.Share));
            }
        }

        // Config button items with Live Text.
        if (onLiveTextInput is not null)
        {
            resultButtonItem.Add(new ContextMenuButtonItem(onLiveTextInput, ContextMenuButtonType.LiveTextInput));
        }

        return resultButtonItem;
    }

    public sealed partial class EditableTextState
    {
        private readonly LiveTextInputStatusNotifier? _liveTextInputStatus =
            PlatformDefaults.IsWeb ? null : new LiveTextInputStatusNotifier();

        // The text processing service used to retrieve the native text processing actions.
        private readonly IProcessTextService _processTextService = new DefaultProcessTextService();

        // The list of native text processing actions provided by the engine.
        private readonly List<ProcessTextAction> _processTextActions = [];

        private AppLifecycleListener? _appLifecycleListener;
        private bool _justResumed;

        /// <summary>Detects whether the clipboard can paste. On the web it is always
        /// <see cref="Widgets.ClipboardStatus.Pasteable"/>.</summary>
        public ClipboardStatusNotifier ClipboardStatus { get; } =
            PlatformDefaults.IsWeb ? new WebClipboardStatusNotifier() : new ClipboardStatusNotifier();

        // The browser's own context menu is used on the web unless the app disabled it.
        private static bool WebContextMenuEnabled => PlatformDefaults.IsWeb && BrowserContextMenu.Enabled;

        private bool UsesHandleControls => Widget.SelectionControls is ITextSelectionHandleControls;

        /// <inheritdoc/>
        public bool CutEnabled
        {
            get
            {
                if (!UsesHandleControls)
                {
                    return Widget.ToolbarOptions.Cut && !Widget.ReadOnly && !Widget.ObscureText;
                }

                return !Widget.ReadOnly && !Widget.ObscureText && !TextEditingValue.Selection.IsCollapsed;
            }
        }

        /// <inheritdoc/>
        public bool CopyEnabled
        {
            get
            {
                if (!UsesHandleControls)
                {
                    return Widget.ToolbarOptions.Copy && !Widget.ObscureText;
                }

                return !Widget.ObscureText && !TextEditingValue.Selection.IsCollapsed;
            }
        }

        /// <inheritdoc/>
        public bool PasteEnabled
        {
            get
            {
                if (!UsesHandleControls)
                {
                    return Widget.ToolbarOptions.Paste && !Widget.ReadOnly;
                }

                return !Widget.ReadOnly && ClipboardStatus.Value == Widgets.ClipboardStatus.Pasteable;
            }
        }

        /// <inheritdoc/>
        public bool SelectAllEnabled
        {
            get
            {
                if (!UsesHandleControls)
                {
                    return Widget.ToolbarOptions.SelectAll
                           && (!Widget.ReadOnly || !Widget.ObscureText)
                           && Widget.EnableInteractiveSelection;
                }

                if (!Widget.EnableInteractiveSelection || (Widget.ReadOnly && Widget.ObscureText))
                {
                    return false;
                }

                TextEditingValue value = TextEditingValue;
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.MacOS:
                        return false;
                    case TargetPlatform.IOS:
                        return value.Text.Length > 0 && value.Selection.IsCollapsed;
                    default:
                        return value.Text.Length > 0
                               && !(value.Selection.Start == 0 && value.Selection.End == value.Text.Length);
                }
            }
        }

        /// <inheritdoc/>
        public bool LookUpEnabled
        {
            get
            {
                if (PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
                {
                    return false;
                }

                return !Widget.ObscureText
                       && !TextEditingValue.Selection.IsCollapsed
                       && TextEditingValue.Selection.TextInside(TextEditingValue.Text).Trim().Length > 0;
            }
        }

        /// <inheritdoc/>
        public bool SearchWebEnabled
        {
            get
            {
                if (PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
                {
                    return false;
                }

                return !Widget.ObscureText
                       && !TextEditingValue.Selection.IsCollapsed
                       && TextEditingValue.Selection.TextInside(TextEditingValue.Text).Trim().Length > 0;
            }
        }

        /// <inheritdoc/>
        public bool ShareEnabled
        {
            get
            {
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.IOS:
                        return !Widget.ObscureText
                               && !TextEditingValue.Selection.IsCollapsed
                               && TextEditingValue.Selection.TextInside(TextEditingValue.Text).Trim().Length > 0;
                    default:
                        return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool LiveTextInputEnabled =>
            _liveTextInputStatus?.Value == LiveTextInputStatus.Enabled
            && !Widget.ObscureText
            && !Widget.ReadOnly
            && TextEditingValue.Selection.IsCollapsed;

        private void OnChangedClipboardStatus()
        {
            // Inform the widget that the value of clipboardStatus has changed.
            SetState(static () => { });
        }

        private void OnChangedLiveTextInputStatus()
        {
            // Inform the widget that the value of liveTextInputStatus has changed.
            SetState(static () => { });
        }

        private void StartLiveTextInput(SelectionChangedCause cause)
        {
            if (!LiveTextInputEnabled)
            {
                return;
            }

            if (HasInputConnection)
            {
                Scheduler.RunAsync(async () =>
                {
                    try
                    {
                        await LiveText.StartLiveTextInput();
                    }
                    catch (Exception error)
                    {
                        FlutterError.ReportError(new FlutterErrorDetails(
                            exception: error,
                            stack: error.StackTrace,
                            library: "widgets library",
                            context: new ErrorDescription("while starting Live Text input")));
                    }
                });
            }

            if (cause == SelectionChangedCause.Toolbar)
            {
                HideToolbar();
            }
        }

        private void OnResume()
        {
            _justResumed = true;
            // To prevent adding multiple listeners, remove any existing one first.
            FocusManager.Instance.RemoveListener(ResetJustResumed);
            // Reset _justResumed as soon as there is a focus change.
            FocusManager.Instance.AddListener(ResetJustResumed);
        }

        private void ResetJustResumed()
        {
            _justResumed = false;
            FocusManager.Instance.RemoveListener(ResetJustResumed);
        }

        /// <summary>
        /// Returns the <see cref="ContextMenuButtonItem"/>s representing the buttons in this
        /// platform's default selection menu, for the deprecated <see cref="ToolbarOptions"/>, or
        /// null when <see cref="EditableText.ToolbarOptions"/> is
        /// <see cref="Widgets.ToolbarOptions.Empty"/>.
        /// </summary>
        public List<ContextMenuButtonItem>? ButtonItemsForToolbarOptions(TargetPlatform? targetPlatform = null)
        {
            ToolbarOptions toolbarOptions = Widget.ToolbarOptions;
            if (toolbarOptions == ToolbarOptions.Empty)
            {
                return null;
            }

            var items = new List<ContextMenuButtonItem>();
            if (toolbarOptions.Cut && CutEnabled)
            {
                items.Add(new ContextMenuButtonItem(
                    () => CutSelection(SelectionChangedCause.Toolbar),
                    ContextMenuButtonType.Cut));
            }

            if (toolbarOptions.Copy && CopyEnabled)
            {
                items.Add(new ContextMenuButtonItem(
                    () => CopySelection(SelectionChangedCause.Toolbar),
                    ContextMenuButtonType.Copy));
            }

            if (toolbarOptions.Paste && PasteEnabled)
            {
                items.Add(new ContextMenuButtonItem(
                    () => PasteTextWithReportingForActions(SelectionChangedCause.Toolbar),
                    ContextMenuButtonType.Paste));
            }

            if (toolbarOptions.SelectAll && SelectAllEnabled)
            {
                items.Add(new ContextMenuButtonItem(
                    () => SelectAll(SelectionChangedCause.Toolbar),
                    ContextMenuButtonType.SelectAll));
            }

            return items;
        }

        /// <summary>Looks up the current selection, as in the "Look Up" edit menu button on iOS.
        /// Dart's <c>lookUpSelection</c>.</summary>
        /// <remarks>Currently this is only implemented for iOS. Throws an error if the selection is
        /// empty or collapsed.</remarks>
        public async Task LookUpSelection(SelectionChangedCause cause)
        {
            if (Constants.KDebugMode && Widget.ObscureText)
            {
                throw new AssertionError("'!widget.obscureText': is not true.");
            }

            string text = TextEditingValue.Selection.TextInside(TextEditingValue.Text);
            if (Widget.ObscureText || text.Length == 0)
            {
                return;
            }

            await SystemChannels.Platform.InvokeMethod<object>("LookUp.invoke", text);
        }

        /// <summary>Launches a web search on the current selection, as in the "Search Web" edit menu
        /// button on iOS. Dart's <c>searchWebForSelection</c>.</summary>
        /// <remarks>Currently this is only implemented for iOS. When <c>cause</c> is
        /// <see cref="SelectionChangedCause.Toolbar"/>, the toolbar is left shown, as in Dart.
        /// </remarks>
        public async Task SearchWebForSelection(SelectionChangedCause cause)
        {
            if (Constants.KDebugMode && Widget.ObscureText)
            {
                throw new AssertionError("'!widget.obscureText': is not true.");
            }

            if (Widget.ObscureText)
            {
                return;
            }

            string text = TextEditingValue.Selection.TextInside(TextEditingValue.Text);
            if (text.Length > 0)
            {
                await SystemChannels.Platform.InvokeMethod<object>("SearchWeb.invoke", text);
            }
        }

        /// <summary>Launches the share interface for the current selection, as in the "Share..."
        /// edit menu button on iOS. Dart's <c>shareSelection</c>.</summary>
        /// <remarks>Currently this is only implemented for iOS and Android.</remarks>
        public async Task ShareSelection(SelectionChangedCause cause)
        {
            if (Constants.KDebugMode && Widget.ObscureText)
            {
                throw new AssertionError("'!widget.obscureText': is not true.");
            }

            if (Widget.ObscureText)
            {
                return;
            }

            string text = TextEditingValue.Selection.TextInside(TextEditingValue.Text);
            if (text.Length > 0)
            {
                await SystemChannels.Platform.InvokeMethod<object>("Share.invoke", text);
            }
        }

        /// <summary>Gets the line heights at the start and end of the selection for the given
        /// editable text state. Dart's <c>getGlyphHeights</c>.</summary>
        /// <remarks>When the text changed since the last layout, or the selection is invalid or
        /// collapsed, both heights are <see cref="RenderEditable.PreferredLineHeight"/>.</remarks>
        public (double StartGlyphHeight, double EndGlyphHeight) GetGlyphHeights()
        {
            TextSelection selection = TextEditingValue.Selection;
            RenderEditable renderEditable = RenderEditableObject;

            // Only calculate handle rects if the text in the previous frame is the same as the text
            // in the current frame. This is done because widget.renderObject contains the
            // renderEditable from the previous frame. If the text changed between the current and
            // previous frames then widget.renderObject.getRectForComposingRange might fail. In cases
            // where the current frame is different from the previous we fall back to
            // renderObject.preferredLineHeight.
            InlineSpan span = renderEditable.Text!;
            string prevText = span.ToPlainText();
            string currText = TextEditingValue.Text;
            if (prevText != currText || !selection.IsValid || selection.IsCollapsed)
            {
                return (renderEditable.PreferredLineHeight, renderEditable.PreferredLineHeight);
            }

            string selectedGraphemes = selection.TextInside(currText);
            int firstSelectedGraphemeExtent = StringInfo.GetNextTextElementLength(selectedGraphemes);
            Rect? startCharacterRect = renderEditable.GetRectForComposingRange(
                new TextRange(selection.Start, selection.Start + firstSelectedGraphemeExtent));
            int[] graphemeStarts = StringInfo.ParseCombiningCharacters(selectedGraphemes);
            int lastSelectedGraphemeExtent = selectedGraphemes.Length - graphemeStarts[^1];
            Rect? endCharacterRect = renderEditable.GetRectForComposingRange(
                new TextRange(selection.End - lastSelectedGraphemeExtent, selection.End));
            return (
                startCharacterRect?.Height ?? renderEditable.PreferredLineHeight,
                endCharacterRect?.Height ?? renderEditable.PreferredLineHeight);
        }

        /// <summary>Returns the anchor points for the default context menu. Dart's
        /// <c>contextMenuAnchors</c>.</summary>
        /// <remarks>A secondary tap (right click) wins and yields its position as the only anchor;
        /// otherwise the anchors come from
        /// <see cref="TextSelectionToolbarAnchors.FromSelection"/>.</remarks>
        public TextSelectionToolbarAnchors ContextMenuAnchors
        {
            get
            {
                RenderEditable renderEditable = RenderEditableObject;
                if (renderEditable.LastSecondaryTapDownPosition is { } lastSecondaryTapDownPosition)
                {
                    return new TextSelectionToolbarAnchors(lastSecondaryTapDownPosition);
                }

                (double startGlyphHeight, double endGlyphHeight) = GetGlyphHeights();
                TextSelection selection = TextEditingValue.Selection;
                IReadOnlyList<TextSelectionPoint> points = renderEditable.GetEndpointsForSelection(selection);
                return TextSelectionToolbarAnchors.FromSelection(
                    renderBox: renderEditable,
                    startGlyphHeight: startGlyphHeight,
                    endGlyphHeight: endGlyphHeight,
                    selectionEndpoints: points);
            }
        }

        /// <summary>
        /// Returns the <see cref="ContextMenuButtonItem"/>s for this field's context menu: the
        /// deprecated <see cref="ToolbarOptions"/> items when set, the platform's editable items
        /// otherwise, followed by the platform's text processing actions. Dart's
        /// <c>contextMenuButtonItems</c>.
        /// </summary>
        public IReadOnlyList<ContextMenuButtonItem> ContextMenuButtonItems =>
            (ButtonItemsForToolbarOptions()
            ?? GetEditableButtonItems(
                clipboardStatus: ClipboardStatus.Value,
                onCopy: CopyEnabled ? () => CopySelection(SelectionChangedCause.Toolbar) : null,
                onCut: CutEnabled ? () => CutSelection(SelectionChangedCause.Toolbar) : null,
                onPaste: PasteEnabled
                    ? () => PasteTextWithReportingForActions(SelectionChangedCause.Toolbar)
                    : null,
                onSelectAll: SelectAllEnabled ? () => SelectAll(SelectionChangedCause.Toolbar) : null,
                onLookUp: LookUpEnabled
                    ? () => Scheduler.RunAsync(() => LookUpSelection(SelectionChangedCause.Toolbar))
                    : null,
                onSearchWeb: SearchWebEnabled
                    ? () => Scheduler.RunAsync(() => SearchWebForSelection(SelectionChangedCause.Toolbar))
                    : null,
                onShare: ShareEnabled
                    ? () => Scheduler.RunAsync(() => ShareSelection(SelectionChangedCause.Toolbar))
                    : null,
                onLiveTextInput: LiveTextInputEnabled
                    ? () => StartLiveTextInput(SelectionChangedCause.Toolbar)
                    : null))
            .Concat(TextProcessingActionButtonItems)
            .ToList();

        // Query the engine to initialize the list of text processing actions to show in the text
        // selection toolbar.
        private async Task InitProcessTextActions()
        {
            _processTextActions.Clear();
            _processTextActions.AddRange(await _processTextService.QueryTextActions());
        }

        private List<ContextMenuButtonItem> TextProcessingActionButtonItems
        {
            get
            {
                var buttonItems = new List<ContextMenuButtonItem>();
                TextSelection selection = TextEditingValue.Selection;
                if (Widget.ObscureText || !selection.IsValid || selection.IsCollapsed)
                {
                    return buttonItems;
                }

                foreach (ProcessTextAction action in _processTextActions)
                {
                    buttonItems.Add(new ContextMenuButtonItem(
                        label: action.Label,
                        onPressed: () => Scheduler.RunAsync(async () =>
                        {
                            string selectedText = selection.TextInside(TextEditingValue.Text);
                            if (selectedText.Length > 0)
                            {
                                string? processedText = await _processTextService.ProcessTextAction(
                                    action.Id,
                                    selectedText,
                                    Widget.ReadOnly);
                                // If an activity does not return a modified version, just hide the
                                // toolbar. Otherwise use the result to replace the selected text.
                                if (processedText is not null && AllowPaste)
                                {
                                    PasteTextCore(SelectionChangedCause.Toolbar, processedText);
                                }
                                else
                                {
                                    HideToolbar();
                                }
                            }
                        })));
                }

                return buttonItems;
            }
        }

#pragma warning disable CS0618 // Dart calls the deprecated legacy controls here too.
        private Action? SemanticsOnCopy(TextSelectionControls? controls)
        {
            return Widget.SelectionEnabled
                   && _focusNode!.HasFocus
                   && (UsesHandleControls
                       ? CopyEnabled
                       : CopyEnabled && (Widget.SelectionControls?.CanCopy(this) ?? false))
                ? () =>
                {
                    controls?.HandleCopy(this);
                    CopySelection(SelectionChangedCause.Toolbar);
                }
                : null;
        }

        private Action? SemanticsOnCut(TextSelectionControls? controls)
        {
            return Widget.SelectionEnabled
                   && _focusNode!.HasFocus
                   && (UsesHandleControls
                       ? CutEnabled
                       : CutEnabled && (Widget.SelectionControls?.CanCut(this) ?? false))
                ? () =>
                {
                    controls?.HandleCut(this);
                    CutSelection(SelectionChangedCause.Toolbar);
                }
                : null;
        }

        private Action? SemanticsOnPaste(TextSelectionControls? controls)
        {
            return Widget.SelectionEnabled
                   && _focusNode!.HasFocus
                   && (UsesHandleControls
                       ? PasteEnabled
                       : PasteEnabled && (Widget.SelectionControls?.CanPaste(this) ?? false))
                   && ClipboardStatus.Value == Widgets.ClipboardStatus.Pasteable
                ? () =>
                {
                    controls?.HandlePaste(this);
                    PasteTextWithReportingForActions(SelectionChangedCause.Toolbar);
                }
                : null;
        }

        /// The tail of Dart's `didUpdateWidget`: refresh the clipboard status when pasting is
        /// possible.
        private void UpdateClipboardStatusAfterWidgetUpdate()
        {
            bool canPaste = UsesHandleControls
                ? PasteEnabled
                : Widget.SelectionControls?.CanPaste(this) ?? false;
            if (Widget.SelectionEnabled && PasteEnabled && canPaste)
            {
                Scheduler.RunAsync(ClipboardStatus.Update);
            }
        }
#pragma warning restore CS0618

        /// <summary>
        /// Shows the selection toolbar at the location of the current cursor. Returns false if a
        /// toolbar couldn't be shown, such as when the toolbar is already shown, or when no text
        /// selection currently exists.
        /// </summary>
        public bool ShowToolbar()
        {
            // Web is using native dom elements to enable clipboard functionality of the context
            // menu: copy, paste, select, cut. It might also provide additional functionality
            // depending on the browser (such as translate). Due to this, we should not show a
            // Flutter toolbar for the editable text elements unless the browser's context menu is
            // explicitly disabled.
            if (WebContextMenuEnabled)
            {
                return false;
            }

            if (_selectionOverlay is null)
            {
                return false;
            }

            if (_selectionOverlay.ToolbarIsVisible)
            {
                return false;
            }

            if (_liveTextInputStatus is not null)
            {
                Scheduler.RunAsync(_liveTextInputStatus.Update);
            }

            Scheduler.RunAsync(ClipboardStatus.Update);
            _selectionOverlay.ShowToolbar();
            // Listen to parent scroll events when the toolbar is visible so it can be hidden during
            // a scroll on supported platforms.
            ListenToParentScrollsIfNeeded();
            return true;
        }

        /// <summary>Hides the toolbar, and the handles too when
        /// <paramref name="hideHandles"/> is true.</summary>
        public void HideToolbar(bool hideHandles = true)
        {
            // Stop listening to parent scroll events when toolbar is hidden.
            DisposeScrollNotificationObserver();
            if (hideHandles)
            {
                // Hide the handles and the toolbar.
                _selectionOverlay?.Hide();
            }
            else if (_selectionOverlay?.ToolbarIsVisible ?? false)
            {
                // Hide only the toolbar but not the handles.
                _selectionOverlay?.HideToolbar();
            }
        }

        /// <summary>Toggles the visibility of the toolbar. Dart's <c>toggleToolbar</c>.</summary>
        public void ToggleToolbar(bool hideHandles = true)
        {
            TextSelectionOverlay selectionOverlay = _selectionOverlay ??= CreateSelectionOverlay();
            if (selectionOverlay.ToolbarIsVisible)
            {
                HideToolbar(hideHandles);
            }
            else
            {
                _ = ShowToolbar();
            }
        }

        /// <summary>
        /// Shows the toolbar with spell check suggestions of misspelled words that are available
        /// for click-and-replace. Dart's <c>showSpellCheckSuggestionsToolbar</c>; returns whether
        /// it was shown.
        /// </summary>
        public bool ShowSpellCheckSuggestionsToolbar()
        {
            // Spell check suggestions toolbars are intended to be shown on non-web platforms.
            if (!SpellCheckEnabled
                || WebContextMenuEnabled
                || Widget.ReadOnly
                || _selectionOverlay is null
                || _spellCheckResults is null
                || FindSuggestionSpanAtCursorIndex(TextEditingValue.Selection.ExtentOffset) is null)
            {
                // Only attempt to show the spell check suggestions toolbar if there is a toolbar
                // specified and spell check suggestions available to show.
                return false;
            }

            if (Widget.SpellCheckConfiguration?.SpellCheckSuggestionsToolbarBuilder is not { } builder)
            {
                throw new AssertionError(
                    "spellCheckSuggestionsToolbarBuilder must be defined in SpellCheckConfiguration "
                    + "to show a toolbar with spell check suggestions");
            }

            _selectionOverlay.ShowSpellCheckSuggestionsToolbar(context => builder(context, this));
            return true;
        }

        /// <summary>Shows the magnifier at <paramref name="positionToShow"/> if no magnifier
        /// exists, or moves the existing one there. Does nothing without a selection overlay.
        /// </summary>
        public void ShowMagnifier(Point positionToShow)
        {
            if (_selectionOverlay is null)
            {
                return;
            }

            if (_selectionOverlay.MagnifierExists)
            {
                _selectionOverlay.UpdateMagnifier(positionToShow);
            }
            else
            {
                _selectionOverlay.ShowMagnifier(positionToShow);
            }
        }

        /// <summary>Hides the magnifier.</summary>
        public void HideMagnifier()
        {
            if (_selectionOverlay is null)
            {
                return;
            }

            _selectionOverlay.HideMagnifier();
        }

        private Widget ContextMenuBuilderForOverlay(BuildContext context)
        {
            return Widget.ContextMenuBuilder!(context, this);
        }

        private TextSelectionOverlay CreateSelectionOverlay()
        {
            return new TextSelectionOverlay(
                clipboardStatus: ClipboardStatus,
                context: Context,
                value: EditingValue,
                debugRequiredFor: Widget,
                toolbarLayerLink: _toolbarLayerLink,
                startHandleLayerLink: _startHandleLayerLink,
                endHandleLayerLink: _endHandleLayerLink,
                renderObject: RenderEditableObject,
                selectionControls: Widget.SelectionControls,
                selectionDelegate: this,
                dragStartBehavior: Widget.DragStartBehavior,
                onSelectionHandleTapped: Widget.OnSelectionHandleTapped,
                contextMenuBuilder: Widget.ContextMenuBuilder is null || WebContextMenuEnabled
                    ? null
                    : ContextMenuBuilderForOverlay,
                magnifierConfiguration: Widget.MagnifierConfiguration);
        }

        /// The selection-overlay part of Dart's `didUpdateWidget`.
        private void DidUpdateWidgetForSelectionOverlay(EditableText oldWidget)
        {
            // If only the identity of the context menu builder closure changed (e.g. an inline
            // lambda on every rebuild), the TextSelectionOverlay does not need to be recreated.
            //
            // We just need to trigger a rebuild of the currently-shown toolbar so its overlay entry
            // picks up the new closure.
            TextSelectionOverlay? selectionOverlay = _selectionOverlay;
            if (selectionOverlay is { ToolbarIsVisible: true }
                && Widget.ContextMenuBuilder != oldWidget.ContextMenuBuilder
                && (Widget.ContextMenuBuilder is null) == (oldWidget.ContextMenuBuilder is null))
            {
                // Deferred to the next frame because ShowToolbar() calls renderBox.LocalToGlobal(),
                // which requires a fully laid-out render tree, and DidUpdateWidget is called before
                // layout.
                Scheduler.AddPostFrameCallback(_ =>
                {
                    if (Mounted && (_selectionOverlay?.ToolbarIsVisible ?? false))
                    {
                        _selectionOverlay!.ShowToolbar();
                    }
                });
            }

            if (_selectionOverlay is not null
                && ((Widget.ContextMenuBuilder is null) != (oldWidget.ContextMenuBuilder is null)
                    || !ReferenceEquals(Widget.SelectionControls, oldWidget.SelectionControls)
                    || Widget.OnSelectionHandleTapped != oldWidget.OnSelectionHandleTapped
                    || Widget.DragStartBehavior != oldWidget.DragStartBehavior
                    || !ReferenceEquals(Widget.MagnifierConfiguration, oldWidget.MagnifierConfiguration)))
            {
                bool shouldShowToolbar = _selectionOverlay.ToolbarIsVisible;
                bool shouldShowHandles = _selectionOverlay.HandlesVisible;
                _selectionOverlay.Dispose();
                _selectionOverlay = CreateSelectionOverlay();
                if (shouldShowToolbar || shouldShowHandles)
                {
                    Scheduler.AddPostFrameCallback(_ =>
                    {
                        if (shouldShowToolbar)
                        {
                            _selectionOverlay!.ShowToolbar();
                        }

                        if (shouldShowHandles)
                        {
                            _selectionOverlay!.ShowHandles();
                        }
                    });
                }
            }
            else if (!oldWidget.Controller.Selection.Equals(Widget.Controller.Selection))
            {
                _selectionOverlay?.Update(EditingValue);
            }

            if (_selectionOverlay is not null)
            {
                _selectionOverlay.HandlesVisible = Widget.ShowSelectionHandles;
            }
        }
    }
}
