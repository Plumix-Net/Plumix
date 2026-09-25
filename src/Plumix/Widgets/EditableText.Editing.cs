using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

public sealed partial class EditableText
{
    public sealed partial class EditableTextState
    {
        private int _batchEditDepth;

        // The last value this state and the platform agreed on; a local change that equals it is not
        // sent again.
        private TextEditingValue? _lastKnownRemoteTextEditingValue;

        private bool _restartConnectionScheduled;

        /// Dart's `_value`: the controller's value.
        private TextEditingValue EditingValue
        {
            get => _controller!.Value;
            set => _controller!.Value = value;
        }

        /// Dart's `_shouldCreateInputConnection`: a read-only field still talks to the platform on the
        /// web and on macOS, where the input connection carries the selection.
        private bool ShouldCreateInputConnection =>
            PlatformDefaults.IsWeb || PlatformDefaults.TargetPlatform == TargetPlatform.MacOS || !Widget.ReadOnly;

        /// <summary>The render object this state configures. Dart's <c>renderEditable</c>.</summary>
        public RenderEditable RenderEditableObject =>
            RenderEditable ?? throw new InvalidOperationException("_Editable must be mounted.");

        // ------------------------------------------------------------- TextInputClient

        /// <inheritdoc/>
        public void UpdateEditingValue(TextEditingValue value)
        {
            // This method handles text editing state updates from the platform text input plugin. The
            // [EditableText] may not have the focus or an open input connection, as autofill can
            // update a disconnected [EditableText].

            // Since we still have to support keyboard select, this is the best place to disable text
            // updating.
            if (!ShouldCreateInputConnection)
            {
                return;
            }

            if (CheckNeedsAdjustAffinity(value))
            {
                value = value.CopyWith(selection: value.Selection with { Affinity = EditingValue.Selection.Affinity });
            }

            if (Widget.ReadOnly)
            {
                // In the read-only case, we only care about selection changes, and reject everything
                // else.
                value = new TextEditingValue(EditingValue.Text, value.Selection);
            }

            _lastKnownRemoteTextEditingValue = value;

            if (value.Equals(EditingValue))
            {
                // This is possible, for example, when the numeric keyboard is input, the engine will
                // notify twice for the same value.
                return;
            }

            if (string.Equals(value.Text, EditingValue.Text, StringComparison.Ordinal)
                && Nullable.Equals(value.Composing, EditingValue.Composing))
            {
                // `selection` is the only change.
                SelectionChangedCause cause = (_textInputConnection?.ScribbleInProgress ?? false)
                    ? SelectionChangedCause.StylusHandwriting
                    : _pointOffsetOrigin is not null
                        ? SelectionChangedCause.ForcePress
                        : SelectionChangedCause.Keyboard;
                HandleSelectionChanged(value.Selection, cause);
            }
            else
            {
                if (!string.Equals(value.Text, EditingValue.Text, StringComparison.Ordinal))
                {
                    // Hide the toolbar if the text was changed, but only hide the toolbar overlay;
                    // the selection handle's visibility will be handled by `_handleSelectionChanged`.
                    HideToolbar(hideHandles: false);
                }

                _currentPromptRectRange = null;
                FormatAndSetValue(value, SelectionChangedCause.Keyboard);
            }

            // Wherever the value is changed by the user, schedule a showCaretOnScreen to make sure
            // the user can see the changes they just made.
            ScheduleShowCaretOnScreen(withAnimation: true);
        }

        private bool CheckNeedsAdjustAffinity(TextEditingValue value)
        {
            // Trust the engine affinity if the text changes or selection changes.
            return string.Equals(value.Text, EditingValue.Text, StringComparison.Ordinal)
                   && value.Selection.IsCollapsed == EditingValue.Selection.IsCollapsed
                   && value.Selection.Start == EditingValue.Selection.Start
                   && value.Selection.Affinity != EditingValue.Selection.Affinity;
        }

        /// <summary>Starts a batch of edits: the platform hears about the value once, when the
        /// outermost batch ends.</summary>
        public void BeginBatchEdit() => _batchEditDepth += 1;

        /// <summary>Ends a batch started by <see cref="BeginBatchEdit"/>.</summary>
        public void EndBatchEdit()
        {
            _batchEditDepth -= 1;
            if (Constants.KDebugMode && _batchEditDepth < 0)
            {
                throw new AssertionError("Unbalanced call to endBatchEdit: beginBatchEdit must be called first.");
            }

            UpdateRemoteEditingValueIfNeeded();
        }

        private void UpdateRemoteEditingValueIfNeeded()
        {
            if (_batchEditDepth > 0 || !HasInputConnection)
            {
                return;
            }

            TextEditingValue localValue = EditingValue;
            if (localValue.Equals(_lastKnownRemoteTextEditingValue))
            {
                return;
            }

            _textInputConnection!.SetEditingState(localValue);
            _lastKnownRemoteTextEditingValue = localValue;
        }

        // Only the platform's text input plugin should call this, through `updateEditingValue`, or
        // user interactions through `userUpdateTextEditingValue`.
        private void FormatAndSetValue(
            TextEditingValue value,
            SelectionChangedCause? cause,
            bool userInteraction = false)
        {
            TextEditingValue oldValue = EditingValue;
            bool textChanged = !string.Equals(oldValue.Text, value.Text, StringComparison.Ordinal);
            bool textCommitted = !IsCollapsedComposing(oldValue.Composing) && IsCollapsedComposing(value.Composing);
            bool selectionChanged = !oldValue.Selection.Equals(value.Selection);

            if (textChanged || textCommitted)
            {
                // Only apply input formatters if the text has changed (including uncommitted text in
                // the composing region), or when the user committed the composing text. Gboard is
                // very persistent in restoring the composing region. Applying input formatters on
                // composing-region-only changes (except clearing the current composing region) is
                // very infinite-loop-prone: the formatters will keep trying to modify the composing
                // region while Gboard will keep trying to restore the original composing region.
                try
                {
                    if (Widget.InputFormatters is { } formatters)
                    {
                        foreach (TextInputFormatter formatter in formatters)
                        {
                            value = formatter.FormatEditUpdate(EditingValue, value);
                        }
                    }

                    if (SpellCheckEnabled
                        && value.Text.Length > 0
                        && !string.Equals(EditingValue.Text, value.Text, StringComparison.Ordinal))
                    {
                        string text = value.Text;
                        Scheduler.RunAsync(() => RequestSpellCheckAsync(text));
                    }
                }
                catch (Exception exception)
                {
                    FlutterError.ReportError(new FlutterErrorDetails(
                        exception: exception,
                        stack: exception.StackTrace,
                        library: "widgets",
                        context: new ErrorDescription("while applying input formatters")));
                }
            }

            TextSelection oldTextSelection = TextEditingValue.Selection;

            // Put all optional user callback invocations in a batch edit to prevent sending multiple
            // `TextInput.updateEditingValue` messages.
            BeginBatchEdit();
            EditingValue = value;
            // Changes made by the keyboard can sometimes be "out of band" for listening components,
            // so always send those events, even if we didn't think it changed. Also, the user long
            // pressing should always send a selection change as well.
            if (selectionChanged
                || (userInteraction
                    && cause is SelectionChangedCause.LongPress or SelectionChangedCause.Keyboard))
            {
                HandleSelectionChanged(EditingValue.Selection, cause);
                BringIntoViewBySelectionState(oldTextSelection, value.Selection, cause);
            }

            string currentText = EditingValue.Text;
            if (!string.Equals(oldValue.Text, currentText, StringComparison.Ordinal))
            {
                try
                {
                    Widget.OnChanged?.Invoke(currentText);
                }
                catch (Exception exception)
                {
                    FlutterError.ReportError(new FlutterErrorDetails(
                        exception: exception,
                        stack: exception.StackTrace,
                        library: "widgets",
                        context: new ErrorDescription("while calling onChanged")));
                }
            }

            EndBatchEdit();
        }

        // Dart's `TextRange.isCollapsed` on a composing range: `TextRange.empty` (Plumix's null) is
        // collapsed too.
        private static bool IsCollapsedComposing(TextRange? composing) => composing is not { IsCollapsed: false };

        /// <inheritdoc/>
        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
            // Compare the current TextEditingValue with the pre-format new TextEditingValue value, in
            // case the formatter would reject the change.
            bool shouldShowCaret = Widget.ReadOnly
                ? !EditingValue.Selection.Equals(value.Selection)
                : !EditingValue.Equals(value);
            if (shouldShowCaret)
            {
                ScheduleShowCaretOnScreen(withAnimation: true);
            }

            // Even if the value doesn't change, it may be necessary to focus and build the selection
            // overlay. For example, this happens when right clicking an unfocused field that
            // previously had a selection in the same spot.
            if (value.Equals(TextEditingValue))
            {
                if (!_focusNode!.HasFocus)
                {
                    FlagInternalFocus();
                    _focusNode.RequestFocus();
                    if (RenderEditable is { HasSize: true })
                    {
                        _ = EnsureSelectionOverlay();
                    }
                }

                return;
            }

            FormatAndSetValue(value, cause, userInteraction: true);
        }

        /// Dart's `_handleSelectionChanged`.
        private void HandleSelectionChanged(TextSelection selection, SelectionChangedCause? cause)
        {
            // We return early if the selection is not valid. This can happen when the text of
            // [EditableText] is updated at the same time as the selection is changed by a gesture
            // event.
            string text = EditingValue.Text;
            if (text.Length < selection.End || text.Length < selection.Start)
            {
                return;
            }

            _controller!.Selection = selection;

            // This will show the keyboard for all selection changes on the EditableText except for
            // those triggered by a keyboard input. Typically EditableText shouldn't take user keyboard
            // input if it's not focused already. If the EditableText is being autofilled it shouldn't
            // request focus.
            switch (cause)
            {
                case null:
                case SelectionChangedCause.DoubleTap:
                case SelectionChangedCause.Drag:
                case SelectionChangedCause.ForcePress:
                case SelectionChangedCause.LongPress:
                case SelectionChangedCause.StylusHandwriting:
                case SelectionChangedCause.Tap:
                case SelectionChangedCause.Toolbar:
                    RequestKeyboard();
                    break;
                case SelectionChangedCause.Keyboard:
                    break;
            }

            if (Widget.SelectionControls is null && Widget.ContextMenuBuilder is null)
            {
                _selectionOverlay?.Dispose();
                _selectionOverlay = null;
            }
            else if (RenderEditable is { HasSize: true })
            {
                if (_selectionOverlay is null)
                {
                    _ = EnsureSelectionOverlay();
                }
                else
                {
                    _selectionOverlay.Update(EditingValue);
                }

                _selectionOverlay!.HandlesVisible = Widget.ShowSelectionHandles;
                _selectionOverlay.ShowHandles();
            }

            // TODO(chunhtai): we should make sure selection actually changed before we call the
            // onSelectionChanged.
            // https://github.com/flutter/flutter/issues/76349.
            try
            {
                Widget.OnSelectionChanged?.Invoke(selection, cause);
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "widgets",
                    context: new ErrorDescription($"while calling onSelectionChanged for {cause}")));
            }
        }

        /// Dart's `_didChangeTextEditingValue`: the controller listener.
        private void DidChangeTextEditingValue()
        {
            PushValueToRenderEditable();
            UpdateRemoteEditingValueIfNeeded();
            UpdateOrDisposeSelectionOverlayIfNeeded();
            // TODO(abarth): Teach RenderEditable about ValueNotifier<TextEditingValue> to avoid this
            // setState().
            SetState(static () => { });
            VerticalSelectionUpdateAction.StopCurrentVerticalRunIfSelectionChanges();
        }

        /// The selection overlay reads the editable's geometry before the rebuild reaches the render
        /// object, so the new value is pushed ahead of `UpdateRenderObject`, which then sees no
        /// change. Dart never has new text in the render object between frames, so the painter is
        /// laid out right away: a hover hit test or an IME geometry query can arrive before the next
        /// layout and read it.
        private void PushValueToRenderEditable()
        {
            if (RenderEditable is not { HasSize: true } renderEditable)
            {
                return;
            }

            renderEditable.Text = BuildTextSpan(ShowPlaceholder);
            renderEditable.Selection = ShowPlaceholder ? TextSelection.Collapsed(0) : EditingValue.Selection;
            if (renderEditable.Attached)
            {
                renderEditable.ComputeTextMetricsIfNeeded();
            }
        }

        private void UpdateOrDisposeSelectionOverlayIfNeeded()
        {
            if (_selectionOverlay is null)
            {
                return;
            }

            if (_focusNode!.HasFocus)
            {
                _selectionOverlay.Update(EditingValue);
            }
            else
            {
                _selectionOverlay.Dispose();
                _selectionOverlay = null;
            }
        }

        // ----------------------------------------------------------------- clipboard

        /// <summary>Copies the selected text to the clipboard. Dart's <c>copySelection</c>.</summary>
        public void CopySelection(SelectionChangedCause cause)
        {
            TextSelection selection = TextEditingValue.Selection;
            if (selection.IsCollapsed || Widget.ObscureText)
            {
                return;
            }

            string text = TextEditingValue.Text;
            SetClipboard(selection.AsTextRange().TextInside(text), "while copying selection to clipboard");
            if (cause == SelectionChangedCause.Toolbar)
            {
                BringIntoView(TextEditingValue.Selection.Extent);
                HideToolbar(hideHandles: false);

                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.MacOS:
                    case TargetPlatform.IOS:
                    case TargetPlatform.Linux:
                    case TargetPlatform.Windows:
                        break;
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                        // Collapse the selection and hide the toolbar and handles.
                        UserUpdateTextEditingValue(
                            new TextEditingValue(
                                TextEditingValue.Text,
                                TextSelection.Collapsed(TextEditingValue.Selection.End)),
                            SelectionChangedCause.Toolbar);
                        break;
                }
            }

            Scheduler.RunAsync(_clipboardStatus.Update);
        }

        /// <summary>Cuts the selected text to the clipboard. Dart's <c>cutSelection</c>.</summary>
        public void CutSelection(SelectionChangedCause cause)
        {
            if (Widget.ReadOnly || Widget.ObscureText)
            {
                return;
            }

            TextSelection selection = TextEditingValue.Selection;
            string text = TextEditingValue.Text;
            if (selection.IsCollapsed)
            {
                return;
            }

            SetClipboard(selection.AsTextRange().TextInside(text), "while cutting selection to clipboard");
            ReplaceTextCore(new ReplaceTextIntent(TextEditingValue, string.Empty, selection.AsTextRange(), cause));
            if (cause == SelectionChangedCause.Toolbar)
            {
                // Schedule a call to bringIntoView() after renderEditable updates.
                Scheduler.AddPostFrameCallback(_ =>
                {
                    if (Mounted)
                    {
                        BringIntoView(TextEditingValue.Selection.Extent);
                    }
                }, "EditableText.bringSelectionIntoView");
                HideToolbar();
            }

            Scheduler.RunAsync(_clipboardStatus.Update);
        }

        private void SetClipboard(string text, string errorContext)
        {
            Scheduler.RunAsync(async () =>
            {
                try
                {
                    await Clipboard.SetData(new ClipboardData(text));
                }
                catch (Exception exception)
                {
                    ReportClipboardError(exception, errorContext);
                }
            });
        }

        private static void ReportClipboardError(Exception exception, string context)
        {
            FlutterError.ReportError(new FlutterErrorDetails(
                exception: exception,
                stack: exception.StackTrace,
                library: "widgets library",
                context: new ErrorDescription(context)));
        }

        private bool AllowPaste => !Widget.ReadOnly && TextEditingValue.Selection.IsValid;

        /// <inheritdoc/>
        /// <remarks>Dart's <c>pasteText</c> returns a future; this interface member is
        /// fire-and-forget, so it runs <see cref="PasteTextAsync"/> the way the paste action does,
        /// reporting a failed clipboard read to <see cref="FlutterError"/>.</remarks>
        public void PasteText(SelectionChangedCause cause) =>
            Scheduler.RunAsync(() => PasteTextWithReportingAsync(cause));

        /// <summary>Pastes the clipboard's text over the selection. Dart's <c>pasteText</c>.</summary>
        public async Task PasteTextAsync(SelectionChangedCause cause)
        {
            if (!AllowPaste)
            {
                return;
            }

            // Snapshot the input before using `await`.
            // See https://github.com/flutter/flutter/issues/11427
            ClipboardData? data = await Clipboard.GetData(Clipboard.KTextPlain);
            if (data is null || !Mounted)
            {
                return;
            }

            PasteTextCore(cause, data.Text!);
        }

        private void PasteTextCore(SelectionChangedCause cause, string text)
        {
            if (!AllowPaste)
            {
                return;
            }

            TextEditingValue value = TextEditingValue;
            TextSelection selection = value.Selection;
            // After the paste, the cursor should be collapsed and located after the pasted content.
            int lastSelectionIndex = Math.Max(selection.BaseOffset, selection.ExtentOffset);
            TextEditingValue collapsedTextEditingValue = value.CopyWith(
                selection: TextSelection.Collapsed(lastSelectionIndex));

            UserUpdateTextEditingValue(
                collapsedTextEditingValue.Replaced(selection.AsTextRange(), LimitInsertion(text)),
                cause);
            if (cause == SelectionChangedCause.Toolbar)
            {
                // Schedule a call to bringIntoView() after renderEditable updates.
                Scheduler.AddPostFrameCallback(_ =>
                {
                    if (Mounted)
                    {
                        BringIntoView(TextEditingValue.Selection.Extent);
                    }
                }, "EditableText.bringSelectionIntoView");
                HideToolbar();
            }
        }

        private async Task PasteTextWithReportingAsync(SelectionChangedCause cause)
        {
            try
            {
                await PasteTextAsync(cause);
            }
            catch (Exception exception)
            {
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: exception,
                    stack: exception.StackTrace,
                    library: "widgets",
                    context: new ErrorDescription("while pasting text to EditableText")));
            }
        }

        /// <summary>Selects the entire text value. Dart's <c>selectAll</c>.</summary>
        public void SelectAll(SelectionChangedCause cause)
        {
            if (Widget.ReadOnly && Widget.ObscureText)
            {
                // If we can't modify it, and we can't copy it, there's no point in selecting it.
                return;
            }

            UserUpdateTextEditingValue(
                TextEditingValue.CopyWith(selection: new TextSelection(0, TextEditingValue.Text.Length)),
                cause);

            if (cause == SelectionChangedCause.Toolbar)
            {
                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.IOS:
                    case TargetPlatform.Fuchsia:
                        break;
                    case TargetPlatform.MacOS:
                    case TargetPlatform.Linux:
                    case TargetPlatform.Windows:
                        HideToolbar();
                        break;
                }

                switch (PlatformDefaults.TargetPlatform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.Linux:
                    case TargetPlatform.Windows:
                        BringIntoView(TextEditingValue.Selection.Extent);
                        break;
                    case TargetPlatform.MacOS:
                    case TargetPlatform.IOS:
                        break;
                }
            }
        }

        // ------------------------------------------------------------------ toolbar

        /// <summary>Toggles the visibility of the toolbar. Dart's <c>toggleToolbar</c>.</summary>
        public void ToggleToolbar(bool hideHandles = true)
        {
            TextSelectionOverlay selectionOverlay = EnsureSelectionOverlay();
            if (selectionOverlay.ToolbarIsVisible)
            {
                HideToolbar(hideHandles);
            }
            else
            {
                _ = ShowToolbar();
            }
        }

        /// <summary>Shows the magnifier at the position given by <paramref name="positionToShow"/>,
        /// if there is no magnifier visible; otherwise moves it.</summary>
        public void ShowMagnifier(Point positionToShow)
        {
            if (_selectionOverlay is null)
            {
                return;
            }

            if (_selectionOverlay.MagnifierIsVisible)
            {
                _selectionOverlay.UpdateMagnifier(positionToShow);
            }
            else
            {
                _selectionOverlay.ShowMagnifier(positionToShow);
            }
        }

        /// <summary>Hides the magnifier if it is visible.</summary>
        public void HideMagnifier() => _selectionOverlay?.HideMagnifier();

        /// <summary>
        /// Shows the spell check suggestions toolbar for the misspelled word under the caret. Dart's
        /// <c>showSpellCheckSuggestionsToolbar</c>; returns whether it was shown.
        /// </summary>
        public bool ShowSpellCheckSuggestionsToolbar()
        {
            if (!SpellCheckEnabled
                || Widget.ReadOnly
                || _selectionOverlay is null
                || _spellCheckResults is null
                || FindSuggestionSpanAtCursorIndex(TextEditingValue.Selection.ExtentOffset) is null
                || Widget.SpellCheckConfiguration?.SpellCheckSuggestionsToolbarBuilder is not { } builder)
            {
                // Only attempt to show the spell check suggestions toolbar if there is a toolbar
                // specified and spell check suggestions available to show.
                return false;
            }

            _selectionOverlay.ShowSpellCheckSuggestionsToolbar(context => builder(context, this));
            return true;
        }

        /// <inheritdoc/>
        /// <remarks>Dart's <c>performSelector</c>: the macOS selector's intent is invoked from the
        /// primary focus, like a shortcut.</remarks>
        public void PerformSelector(string selectorName)
        {
            Intent? intent = MacOsSelectors.IntentForMacOsSelector(selectorName);
            if (intent is null)
            {
                return;
            }

            BuildContext? primaryContext = FocusManager.Instance.PrimaryFocus?.Context;
            if (primaryContext is not null)
            {
                _ = Actions.Invoke(primaryContext, intent);
            }
        }
    }
}
