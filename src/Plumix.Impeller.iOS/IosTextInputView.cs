using System.Collections;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterTextInputPlugin.mm
// (FlutterTextInputView).
// The view the keyboard edits: it keeps its own copy of the editing state (text, selection, marked range) as the engine
// does, answers UITextInput from it and from the selection rects the framework sends, and reports every edit to the
// framework through the plugin. Not ported: Scribble (its status stays "none"), and the secure view's UITextField
// masquerade for strong-password autofill.

/// <summary>The engine's FlutterTextInputView: a hidden UITextInput that the system keyboard edits.</summary>
// The bindings declare UITextInput's results non-null where UIKit takes nil (no position, no marked range).
#pragma warning disable CS8764, CS8766

[Register("PlumixTextInputView")]
internal class IosTextInputView : UIView, IUITextInput, IUITextInputTraits
{
    // kInvalidFirstRect: the canonical invalid rect; returned from firstRectForRange it hides the IME candidates.
    internal static readonly CGRect InvalidFirstRect = new(-1, -1, 9999, 9999);

    private const string TextAffinityDownstream = "TextAffinity.downstream";
    private const string TextAffinityUpstream = "TextAffinity.upstream";

    private readonly WeakReference<IosTextInputPlugin> _plugin;
    private readonly List<Dictionary<string, object?>> _pendingDeltas = [];
    private WeakReference<NSObject>? _inputDelegate;
    private FlutterTokenizer? _tokenizer;
    private string _selectionAffinity = TextAffinityUpstream;
    private FlutterTextRange _selectedTextRange = new(0, 0);
    private FlutterTextRange? _markedTextRange;
    private CGRect _markedRect = InvalidFirstRect;
    private CGRect _cachedFirstRect = InvalidFirstRect;
    private CATransform3D _editableTransform;
    private UIInputViewController? _inputViewController;
    private UITextInteraction? _textInteraction;
    private bool _isSystemKeyboardEnabled;
    private bool _isFloatingCursorActive;
    private CGPoint _floatingCursorOffset;
    private bool _enableInteractiveSelection = true;
    private string? _temporarilyDeletedComposedCharacter;
    private NSObject? _editMenuInteraction;
    private CGRect _editMenuTargetRect;
    private IList? _editMenuItems;

    public IosTextInputView(IosTextInputPlugin plugin)
        : base(CGRect.Empty)
    {
        _plugin = new WeakReference<IosTextInputPlugin>(plugin);

        // UITextInputTraits defaults.
        AutocapitalizationType = UITextAutocapitalizationType.Sentences;
        AutocorrectionType = UITextAutocorrectionType.Default;
        SpellCheckingType = UITextSpellCheckingType.Default;
        KeyboardAppearance = UIKeyboardAppearance.Default;
        KeyboardType = UIKeyboardType.Default;
        ReturnKeyType = UIReturnKeyType.Done;
        SmartQuotesType = UITextSmartQuotesType.Yes;
        SmartDashesType = UITextSmartDashesType.Yes;

        if (OperatingSystem.IsIOSVersionAtLeast(16))
        {
            var interaction = new UIEditMenuInteraction(new EditMenuDelegate(this));
            AddInteraction(interaction);
            _editMenuInteraction = interaction;
        }
    }

    /// <summary>The editing state's text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>The attached client's id; 0 when this view does not belong to the current connection.</summary>
    public int TextInputClient { get; private set; }

    public string? AutofillId { get; private set; }

    public bool EnableDeltaModel { get; private set; }

    public bool AccessibilityEnabled { get; set; }

    /// <summary>The engine's preventCursorDismissWhenResignFirstResponder.</summary>
    public bool PreventCursorDismissWhenResignFirstResponder { get; set; }

    /// <summary>The view touches are forwarded to: the Flutter view.</summary>
    public UIView? ViewResponder { get; set; }

    /// <summary>The framework's per-character selection rects, in the view's coordinates.</summary>
    public IReadOnlyList<FlutterTextSelectionRect> SelectionRects { get; set; } = [];

    // UITextInputTraits.
    [Export("autocapitalizationType")]
    public UITextAutocapitalizationType AutocapitalizationType { get; set; }

    [Export("autocorrectionType")]
    public UITextAutocorrectionType AutocorrectionType { get; set; }

    [Export("spellCheckingType")]
    public UITextSpellCheckingType SpellCheckingType { get; set; }

    [Export("smartQuotesType")]
    public UITextSmartQuotesType SmartQuotesType { get; set; }

    [Export("smartDashesType")]
    public UITextSmartDashesType SmartDashesType { get; set; }

    [Export("smartInsertDeleteType")]
    public UITextSmartInsertDeleteType SmartInsertDeleteType { get; set; }

    [Export("enablesReturnKeyAutomatically")]
    public bool EnablesReturnKeyAutomatically { get; set; }

    [Export("keyboardAppearance")]
    public UIKeyboardAppearance KeyboardAppearance { get; set; }

    [Export("keyboardType")]
    public UIKeyboardType KeyboardType { get; set; }

    [Export("returnKeyType")]
    public UIReturnKeyType ReturnKeyType { get; set; }

    [Export("secureTextEntry")]
    public bool SecureTextEntry
    {
        [Export("isSecureTextEntry")] get;
        set;
    }

    [Export("textContentType")]
    public NSString? TextContentType { get; set; }

    [Export("passwordRules")]
    public UITextInputPasswordRules? PasswordRules { get; set; }

    [Export("inlinePredictionType")]
    public UITextInlinePredictionType InlinePredictionType { get; set; }

    // Undocumented UIKit colors that keep its own selection UI invisible; iOS 17 uses the insertion point color as the
    // selected IME candidate's background, so the engine hides that one selector from respondsToSelector: there.
    [Export("selectionBarColor")]
    public UIColor SelectionBarColor => UIColor.Clear;

    [Export("selectionHighlightColor")]
    public UIColor SelectionHighlightColor => UIColor.Clear;

    [Export("insertionPointColor")]
    public UIColor InsertionPointColor => UIColor.Clear;

    public override bool RespondsToSelector(Selector? sel) =>
        !(OperatingSystem.IsIOSVersionAtLeast(17) && sel?.Name == "insertionPointColor")
        && base.RespondsToSelector(sel);

    public override UIInputViewController? InputViewController
    {
        get
        {
            if (_isSystemKeyboardEnabled)
            {
                return null;
            }

            return _inputViewController ??= new UIInputViewController();
        }
    }

    private IosTextInputPlugin? Plugin => _plugin.TryGetTarget(out IosTextInputPlugin? plugin) ? plugin : null;

    private NSObject? InputDelegateObject =>
        _inputDelegate is not null && _inputDelegate.TryGetTarget(out NSObject? target) ? target : null;

    private IUITextInputDelegate? InputDelegate => InputDelegateObject as IUITextInputDelegate;

    // UITextInput.
    [Export("inputDelegate", ArgumentSemantic.Weak)]
    public NSObject? WeakInputDelegate
    {
        get => InputDelegateObject;
        set => _inputDelegate = value is null ? null : new WeakReference<NSObject>(value);
    }

    [Export("tokenizer")]
    public NSObject WeakTokenizer => _tokenizer ??= new FlutterTokenizer(this);

    [Export("selectedTextRange")]
    public UITextRange? SelectedTextRange
    {
        get => new FlutterTextRange(_selectedTextRange.Range);
        set
        {
            if (!_enableInteractiveSelection || value is not FlutterTextRange range)
            {
                return;
            }

            SetSelectedTextRangeLocal(range);
            if (EnableDeltaModel)
            {
                UpdateEditingStateWithDelta(Text, -1, -1, string.Empty);
            }
            else
            {
                UpdateEditingState();
            }
        }
    }

    [Export("markedTextRange")]
    public UITextRange? MarkedTextRange => _markedTextRange;

    [Export("markedTextStyle")]
    public NSDictionary? MarkedTextStyle { get; set; }

    [Export("beginningOfDocument")]
    public UITextPosition BeginningOfDocument => new FlutterTextPosition(0, UITextStorageDirection.Forward);

    [Export("endOfDocument")]
    public UITextPosition EndOfDocument => new FlutterTextPosition(Text.Length, UITextStorageDirection.Backward);

    [Export("hasText")]
    public bool HasText => Text.Length > 0;

    public override bool CanBecomeFirstResponder => TextInputClient != 0;

    [Export("accessibilityElementsHidden")]
    public override bool AccessibilityElementsHidden
    {
        get => !AccessibilityEnabled;
        set { }
    }

    private bool IsScribbleAvailable => UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;

    /// <summary>setTextInputClient:.</summary>
    public void SetTextInputClient(int client) => TextInputClient = client;

    /// <summary>configureWithDictionary:.</summary>
    public void Configure(IDictionary configuration)
    {
        IDictionary? inputType = configuration["inputType"] as IDictionary;
        string? keyboardAppearance = configuration["keyboardAppearance"] as string;
        IDictionary? autofill = configuration["autofill"] as IDictionary;

        SecureTextEntry = configuration["obscureText"] is true;
        EnableDeltaModel = configuration["enableDeltaModel"] is true;
        _isSystemKeyboardEnabled = TextInputConfiguration.ShouldShowSystemKeyboard(inputType);
        KeyboardType = TextInputConfiguration.ToUIKeyboardType(inputType);
        ReturnKeyType = TextInputConfiguration.ToUIReturnKeyType(configuration["inputAction"] as string);
        AutocapitalizationType = TextInputConfiguration.ToUITextAutoCapitalizationType(configuration);
        _enableInteractiveSelection = configuration["enableInteractiveSelection"] is true;
        // The indices come from the framework's SmartDashesType and SmartQuotesType enums.
        SmartDashesType = configuration["smartDashesType"] as string == "0"
            ? UITextSmartDashesType.No
            : UITextSmartDashesType.Yes;
        SmartQuotesType = configuration["smartQuotesType"] as string == "0"
            ? UITextSmartQuotesType.No
            : UITextSmartQuotesType.Yes;
        KeyboardAppearance = keyboardAppearance switch
        {
            "Brightness.dark" => UIKeyboardAppearance.Dark,
            "Brightness.light" => UIKeyboardAppearance.Light,
            _ => UIKeyboardAppearance.Default,
        };
        bool autocorrectIsDisabled = configuration.Contains("autocorrect") && configuration["autocorrect"] is false;
        AutocorrectionType = autocorrectIsDisabled ? UITextAutocorrectionType.No : UITextAutocorrectionType.Default;
        SpellCheckingType = autocorrectIsDisabled ? UITextSpellCheckingType.No : UITextSpellCheckingType.Default;
        AutofillId = TextInputConfiguration.AutofillIdFromDictionary(configuration);
        if (autofill is null)
        {
            TextContentType = new NSString(string.Empty);
        }
        else
        {
            TextContentType = TextInputConfiguration.ToUITextContentType(autofill["hints"] as IList);
            if (autofill["editingValue"] is IDictionary editingValue)
            {
                SetTextInputState(editingValue);
            }
        }

        // The input field must be visible for the system autofill to find it.
        IsVisibleToAutofill = autofill is not null || SecureTextEntry;
        if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            InlinePredictionType = configuration["enableInlinePrediction"] is true
                ? UITextInlinePredictionType.Yes
                : UITextInlinePredictionType.No;
        }
    }

    /// <summary>The engine's isVisibleToAutofill: a 1x1 frame is visible, a zero frame is not.</summary>
    public bool IsVisibleToAutofill
    {
        get => Frame.Width > 0 && Frame.Height > 0;
        set => Frame = value ? new CGRect(0, 0, 1, 1) : CGRect.Empty;
    }

    /// <summary>setTextInputState: the framework's editing state, with the keyboard told about each change.</summary>
    public void SetTextInputState(IDictionary state)
    {
        // UITextInteraction's willMoveToView: sometimes clears the input delegate (a UIKit bug); a temporary
        // interaction restores one so the keyboard still hears about the change.
        if (InputDelegateObject is null && IsFirstResponder)
        {
            _textInteraction ??= UITextInteraction.Create(UITextInteractionMode.Editable);
            _textInteraction.TextInput = this;
            AddInteraction(_textInteraction);
        }

        string newText = state["text"] as string ?? string.Empty;
        bool textChanged = Text != newText;
        if (textChanged)
        {
            InputDelegate?.TextWillChange(this);
            Text = newText;
        }

        int composingBase = ToInt(state["composingBase"]);
        int composingExtent = ToInt(state["composingExtent"]);
        NSRange composingRange = ClampSelection(
            new NSRange(Math.Min(composingBase, composingExtent), Math.Abs(composingBase - composingExtent)), Text);
        _markedTextRange = composingRange.Length > 0 ? new FlutterTextRange(composingRange) : null;

        NSRange selectedRange = ClampSelectionFromBase(
            ToInt(state["selectionBase"]), ToInt(state["selectionExtent"]), Text);
        if (selectedRange.Location != _selectedTextRange.Range.Location
            || selectedRange.Length != _selectedTextRange.Range.Length)
        {
            InputDelegate?.SelectionWillChange(this);
            SetSelectedTextRangeLocal(new FlutterTextRange(selectedRange));
            _selectionAffinity = state["selectionAffinity"] as string == TextAffinityUpstream
                ? TextAffinityUpstream
                : TextAffinityDownstream;
            InputDelegate?.SelectionDidChange(this);
        }

        if (textChanged)
        {
            InputDelegate?.TextDidChange(this);
        }

        if (_textInteraction is not null)
        {
            RemoveInteraction(_textInteraction);
        }
    }

    /// <summary>setMarkedRect:.</summary>
    public CGRect MarkedRect
    {
        set
        {
            _markedRect = value;
            _cachedFirstRect = InvalidFirstRect;
        }
    }

    /// <summary>setEditableTransform: a 4x4 matrix in column-major order.</summary>
    public void SetEditableTransform(IList matrix)
    {
        nfloat M(int index) =>
            (nfloat)Convert.ToDouble(matrix[index], System.Globalization.CultureInfo.InvariantCulture);
        _editableTransform = new CATransform3D
        {
            M11 = M(0), M12 = M(1), M13 = M(2), M14 = M(3),
            M21 = M(4), M22 = M(5), M23 = M(6), M24 = M(7),
            M31 = M(8), M32 = M(9), M33 = M(10), M34 = M(11),
            M41 = M(12), M42 = M(13), M43 = M(14), M44 = M(15),
        };
        _cachedFirstRect = InvalidFirstRect;
    }

    /// <summary>
    /// localRectFromFrameworkTransform: the bounding rect of the transformed rect, in this view's space.
    /// </summary>
    public CGRect LocalRectFromFrameworkTransform(CGRect incomingRect)
    {
        CGPoint[] points =
        [
            incomingRect.Location,
            new(incomingRect.X, incomingRect.Y + incomingRect.Height),
            new(incomingRect.X + incomingRect.Width, incomingRect.Y),
            new(incomingRect.X + incomingRect.Width, incomingRect.Y + incomingRect.Height),
        ];
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        CATransform3D t = _editableTransform;
        foreach (CGPoint point in points)
        {
            double x = (t.M11 * point.X) + (t.M21 * point.Y) + t.M41;
            double y = (t.M12 * point.X) + (t.M22 * point.Y) + t.M42;
            double w = (t.M14 * point.X) + (t.M24 * point.Y) + t.M44;
            if (w == 0.0)
            {
                return InvalidFirstRect;
            }

            if (w != 1.0)
            {
                x /= w;
                y /= w;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        return new CGRect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// replaceRangeLocal:withText: without telling the framework; the selection collapses after the text.
    /// </summary>
    public void ReplaceRangeLocal(NSRange range, string text)
    {
        NSRange clamped = ClampSelection(range, Text);
        Text = Text.Remove((int)clamped.Location, (int)clamped.Length).Insert((int)clamped.Location, text);
        NSRange newSelection = ClampSelection(new NSRange(range.Location + text.Length, 0), Text);
        SetSelectedTextRangeLocal(new FlutterTextRange(newSelection));
        _markedTextRange = null;
    }

    // UIResponder.
    public override bool ResignFirstResponder()
    {
        bool success = base.ResignFirstResponder();
        if (success && !PreventCursorDismissWhenResignFirstResponder)
        {
            Plugin?.DidResignFirstResponder(TextInputClient);
        }

        return success;
    }

    public override bool CanPerform(Selector action, NSObject? withSender) =>
        action.Name switch
        {
            // Pasting images, memojis and other non-string content is forbidden.
            "paste:" => UIPasteboard.General.HasStrings,
            "copy:" or "cut:" or "delete:" => (TextInRange(_selectedTextRange) ?? string.Empty).Length > 0,
            "selectAll:" => HasText,
            "captureTextFromCamera:" => true,
            _ => base.CanPerform(action, withSender),
        };

    public override void Cut(NSObject? sender)
    {
        UIPasteboard.General.String = TextInRange(_selectedTextRange);
        ReplaceText(_selectedTextRange, string.Empty);
    }

    public override void Copy(NSObject? sender) => UIPasteboard.General.String = TextInRange(_selectedTextRange);

    public override void Paste(NSObject? sender)
    {
        if (UIPasteboard.General.String is { } pasteboardString)
        {
            InsertText(pasteboardString);
        }
    }

    public override void Delete(NSObject? sender) => ReplaceText(_selectedTextRange, string.Empty);

    public override void SelectAll(NSObject? sender) =>
        SelectedTextRange = GetTextRange(BeginningOfDocument, EndOfDocument);

    // Touches go to the Flutter view, so taps inside the field work as usual.
    public override void TouchesBegan(NSSet touches, UIEvent? evt) => ViewResponder?.TouchesBegan(touches, evt);

    public override void TouchesMoved(NSSet touches, UIEvent? evt) => ViewResponder?.TouchesMoved(touches, evt);

    public override void TouchesEnded(NSSet touches, UIEvent? evt) => ViewResponder?.TouchesEnded(touches, evt);

    public override void TouchesCancelled(NSSet touches, UIEvent? evt) => ViewResponder?.TouchesCancelled(touches, evt);

    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt) =>
        Plugin?.ViewController?.PressesBegan(presses, evt);

    public override void PressesChanged(NSSet<UIPress> presses, UIPressesEvent evt) =>
        Plugin?.ViewController?.PressesChanged(presses, evt);

    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt) =>
        Plugin?.ViewController?.PressesEnded(presses, evt);

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt) =>
        Plugin?.ViewController?.PressesCancelled(presses, evt);

    // UITextInput.
    [Export("textInRange:")]
    public string? TextInRange(UITextRange range)
    {
        if (range is not FlutterTextRange flutterRange || flutterRange.Range.Location == NSRange.NotFound)
        {
            return null;
        }

        int location = Math.Min(flutterRange.Location, Text.Length);
        int length = Math.Min(Text.Length - location, flutterRange.Length);
        return Text.Substring(location, length);
    }

    [Export("replaceRange:withText:")]
    public void ReplaceText(UITextRange range, string text)
    {
        string textBeforeChange = Text;
        NSRange replaceRange = ((FlutterTextRange)range).Range;
        ReplaceRangeLocal(replaceRange, text);
        if (EnableDeltaModel)
        {
            NSRange next = ClampSelection(replaceRange, textBeforeChange);
            UpdateEditingStateWithDelta(
                textBeforeChange, (int)next.Location, (int)(next.Location + next.Length), text);
        }
        else
        {
            UpdateEditingState();
        }
    }

    [Export("shouldChangeTextInRange:replacementText:")]
    public bool ShouldChangeTextInRange(UITextRange inRange, string replacementText)
    {
        // The temporarily deleted composed character only lives through one text change.
        _temporarilyDeletedComposedCharacter = null;
        if (replacementText != "\n")
        {
            return true;
        }

        if (ReturnKeyType == UIReturnKeyType.Default)
        {
            Plugin?.PerformAction("TextInputAction.newline", TextInputClient);
            return true;
        }

        string action = ReturnKeyType switch
        {
            UIReturnKeyType.Done => "TextInputAction.done",
            UIReturnKeyType.Go => "TextInputAction.go",
            UIReturnKeyType.Send => "TextInputAction.send",
            UIReturnKeyType.Search or UIReturnKeyType.Google or UIReturnKeyType.Yahoo => "TextInputAction.search",
            UIReturnKeyType.Next => "TextInputAction.next",
            UIReturnKeyType.Continue => "TextInputAction.continueAction",
            UIReturnKeyType.Join => "TextInputAction.join",
            UIReturnKeyType.Route => "TextInputAction.route",
            UIReturnKeyType.EmergencyCall => "TextInputAction.emergencyCall",
            _ => "TextInputAction.unspecified",
        };
        Plugin?.PerformAction(action, TextInputClient);
        return false;
    }

    /// <summary>
    /// setMarkedText:selectedRange: replaces the marked text, or the selection when nothing is marked.
    /// </summary>
    [Export("setMarkedText:selectedRange:")]
    public void SetMarkedText(string? markedText, NSRange selectedRange)
    {
        string textBeforeChange = Text;
        markedText ??= string.Empty;
        NSRange actualReplacedRange = _markedTextRange is { IsEmpty: false } marked
            ? marked.Range
            : _selectedTextRange.Range;
        NSRange clampedReplace = ClampSelection(actualReplacedRange, Text);
        Text = Text.Remove((int)clampedReplace.Location, (int)clampedReplace.Length)
            .Insert((int)clampedReplace.Location, markedText);

        var newMarkedRange = new NSRange(actualReplacedRange.Location, markedText.Length);
        _markedTextRange = newMarkedRange.Length > 0 ? new FlutterTextRange(newMarkedRange) : null;
        SetSelectedTextRangeLocal(new FlutterTextRange(ClampSelection(
            new NSRange(selectedRange.Location + newMarkedRange.Location, selectedRange.Length), Text)));
        if (EnableDeltaModel)
        {
            NSRange next = ClampSelection(actualReplacedRange, textBeforeChange);
            UpdateEditingStateWithDelta(
                textBeforeChange, (int)next.Location, (int)(next.Location + next.Length), markedText);
        }
        else
        {
            UpdateEditingState();
        }
    }

    // iOS 17 inline prediction marks attributed text; only its string matters.
    [Export("setAttributedMarkedText:selectedRange:")]
    public void SetAttributedMarkedText(NSAttributedString? attributedString, NSRange selectedRange) =>
        SetMarkedText(attributedString?.Value ?? string.Empty, selectedRange);

    [Export("unmarkText")]
    public void UnmarkText()
    {
        if (_markedTextRange is null)
        {
            return;
        }

        _markedTextRange = null;
        if (EnableDeltaModel)
        {
            UpdateEditingStateWithDelta(Text, -1, -1, string.Empty);
        }
        else
        {
            UpdateEditingState();
        }
    }

    [Export("textRangeFromPosition:toPosition:")]
    public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
    {
        int fromIndex = IndexOf(fromPosition);
        int toIndex = IndexOf(toPosition);
        // toIndex can precede fromIndex: UITextInputStringTokenizer mishandles some CJK text; native text views
        // swap them.
        return toIndex >= fromIndex
            ? new FlutterTextRange(fromIndex, toIndex - fromIndex)
            : new FlutterTextRange(toIndex, fromIndex - toIndex);
    }

    [Export("positionFromPosition:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, nint offset)
    {
        int offsetPosition = IndexOf(fromPosition);
        long newLocation = offsetPosition + (long)offset;
        if (newLocation < 0 || newLocation > Text.Length)
        {
            return null;
        }

        if (offset >= 0)
        {
            for (long i = 0; i < offset && offsetPosition < Text.Length; ++i)
            {
                offsetPosition = IncrementOffsetPosition(offsetPosition);
            }
        }
        else
        {
            for (long i = 0; i < -offset && offsetPosition > 0; ++i)
            {
                offsetPosition = DecrementOffsetPosition(offsetPosition);
            }
        }

        return new FlutterTextPosition(offsetPosition);
    }

    // positionFromPosition:inDirection:offset: (the engine has no RTL handling yet, and moves right/down by one).
    [Export("positionFromPosition:inDirection:offset:")]
    public UITextPosition? GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset) =>
        inDirection is UITextLayoutDirection.Left or UITextLayoutDirection.Up
            ? GetPosition(fromPosition, -offset)
            : GetPosition(fromPosition, 1);

    [Export("comparePosition:toPosition:")]
    public NSComparisonResult ComparePosition(UITextPosition first, UITextPosition second)
    {
        int positionIndex = IndexOf(first);
        int otherIndex = IndexOf(second);
        if (positionIndex < otherIndex)
        {
            return NSComparisonResult.Ascending;
        }

        if (positionIndex > otherIndex)
        {
            return NSComparisonResult.Descending;
        }

        UITextStorageDirection positionAffinity = AffinityOf(first);
        UITextStorageDirection otherAffinity = AffinityOf(second);
        if (positionAffinity == otherAffinity)
        {
            return NSComparisonResult.Same;
        }

        return positionAffinity == UITextStorageDirection.Backward
            ? NSComparisonResult.Ascending
            : NSComparisonResult.Descending;
    }

    [Export("offsetFromPosition:toPosition:")]
    public nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition) =>
        IndexOf(toPosition) - IndexOf(fromPosition);

    [Export("positionWithinRange:farthestInDirection:")]
    public UITextPosition? GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction) =>
        direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
            ? new FlutterTextPosition(IndexOf(range.Start), UITextStorageDirection.Forward)
            : new FlutterTextPosition(IndexOf(range.End), UITextStorageDirection.Backward);

    [Export("characterRangeByExtendingPosition:inDirection:")]
    public UITextRange? GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
    {
        int positionIndex = IndexOf(byExtendingPosition);
        (int start, int end) = direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
            ? (DecrementOffsetPosition(positionIndex), positionIndex)
            : (positionIndex, IncrementOffsetPosition(positionIndex));
        return new FlutterTextRange(start, end - start);
    }

    [Export("baseWritingDirectionForPosition:inDirection:")]
    public NSWritingDirection GetBaseWritingDirection(UITextPosition forPosition, UITextStorageDirection direction) =>
        NSWritingDirection.Natural;

    [Export("setBaseWritingDirection:forRange:")]
    public void SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range)
    {
    }

    /// <summary>
    /// firstRectForRange: the marked text rect for the IME candidates while composing; otherwise the rect spanning
    /// the range's first line of selection rects.
    /// </summary>
    [Export("firstRectForRange:")]
    public CGRect GetFirstRectForRange(UITextRange range)
    {
        int start = IndexOf(range.Start);
        int end = IndexOf(range.End);
        if (_markedTextRange is not null)
        {
            // The candidates view cannot show before the framework sent the first caret rect.
            if (_markedRect == InvalidFirstRect)
            {
                return InvalidFirstRect;
            }

            if (_cachedFirstRect == InvalidFirstRect)
            {
                // A too-narrow rect is the caret rect, not the marked text rect: widen it so the candidates show.
                CGRect rect = _markedRect;
                if (rect.IsEmpty)
                {
                    rect = rect.Inset(-0.1f, 0);
                }

                _cachedFirstRect = LocalRectFromFrameworkTransform(rect);
            }

            UIView? hostView = Plugin?.HostView;
            return hostView is not null ? hostView.ConvertRectToView(_cachedFirstRect, this) : _cachedFirstRect;
        }

        if (!OperatingSystem.IsIOSVersionAtLeast(17))
        {
            // The only hint of the auto-correction highlight; iOS 17 asks for every character of the word instead.
            Plugin?.ShowAutocorrectionPromptRect(start, end, TextInputClient);
            // iOS 16's highlight ignores the returned height, so it is hidden unless Scribble needs a width.
            if (!IsScribbleAvailable)
            {
                return CGRect.Empty;
            }
        }

        int first = end < start ? end : start;
        CGRect startSelectionRect = CGRect.Null;
        CGRect endSelectionRect = CGRect.Null;
        double minY = double.MaxValue;
        double maxY = double.MinValue;
        NSRange textRange = TextInputConfiguration.RangeForCharactersInRange(Text, new NSRange(0, Text.Length));
        IReadOnlyList<FlutterTextSelectionRect> rects = SelectionRects;
        for (int i = 0; i < rects.Count; i++)
        {
            bool startsOnOrBeforeStartOfRange = rects[i].Position <= first;
            bool isLastSelectionRect = i + 1 == rects.Count;
            bool endOfTextIsAfterStartOfRange = isLastSelectionRect && textRange.Length > first;
            bool nextSelectionRectIsAfterStartOfRange = !isLastSelectionRect && rects[i + 1].Position > first;
            if (startsOnOrBeforeStartOfRange && (endOfTextIsAfterStartOfRange || nextSelectionRectIsAfterStartOfRange))
            {
                if (!OperatingSystem.IsIOSVersionAtLeast(17))
                {
                    return rects[i].Rect;
                }

                startSelectionRect = rects[i].Rect;
            }

            if (!startSelectionRect.IsNull())
            {
                minY = Math.Min(minY, rects[i].Rect.GetMinY());
                maxY = Math.Max(maxY, rects[i].Rect.GetMaxY());
                bool endsOnOrAfterEndOfRange = rects[i].Position >= end - 1;
                bool nextSelectionRectIsOnNextLine =
                    !isLastSelectionRect && rects[i + 1].Rect.GetMidY() > rects[i].Rect.GetMaxY();
                if (endsOnOrAfterEndOfRange || isLastSelectionRect || nextSelectionRectIsOnNextLine)
                {
                    endSelectionRect = rects[i].Rect;
                    break;
                }
            }
        }

        if (startSelectionRect.IsNull() || endSelectionRect.IsNull())
        {
            return CGRect.Empty;
        }

        double minX = Math.Min(startSelectionRect.GetMinX(), endSelectionRect.GetMinX());
        double maxX = Math.Max(startSelectionRect.GetMaxX(), endSelectionRect.GetMaxX());
        return new CGRect(minX, minY, maxX - minX, maxY - minY);
    }

    [Export("caretRectForPosition:")]
    public CGRect GetCaretRectForPosition(UITextPosition? position)
    {
        if (position is null)
        {
            return CGRect.Empty;
        }

        int index = IndexOf(position);
        UITextStorageDirection affinity = AffinityOf(position);
        // The selection rects of the characters before and after the caret.
        UITextSelectionRect[] rects = GetSelectionRects(new FlutterTextRange(
            TextInputConfiguration.RangeForCharactersInRange(
                Text, new NSRange(Math.Max(0, index - 1), index >= Text.Length ? 1 : 2))));
        if (rects.Length == 0)
        {
            return CGRect.Empty;
        }

        if (index == 0 || (rects.Length == 2 && affinity == UITextStorageDirection.Forward))
        {
            // Along the upstream edge of the character after the caret.
            UITextSelectionRect after = index == 0 ? rects[0] : rects[1];
            CGRect rect = after.Rect;
            return after is FlutterTextSelectionRect { IsRtl: true }
                ? new CGRect(rect.X + rect.Width, rect.Y, 0, rect.Height)
                : new CGRect(rect.X, rect.Y, 0, rect.Height);
        }

        // Along the downstream edge of the character before the caret.
        CGRect before = rects[0].Rect;
        return rects[0] is FlutterTextSelectionRect { IsRtl: true }
            ? new CGRect(before.X, before.Y, 0, before.Height)
            : new CGRect(before.X + before.Width, before.Y, 0, before.Height);
    }

    [Export("closestPositionToPoint:")]
    public UITextPosition GetClosestPositionToPoint(CGPoint point)
    {
        if (SelectionRects.Count == 0)
        {
            return new FlutterTextPosition(_selectedTextRange.Location, UITextStorageDirection.Forward);
        }

        var range = new FlutterTextRange(
            TextInputConfiguration.RangeForCharactersInRange(Text, new NSRange(0, Text.Length)));
        return GetClosestPositionToPoint(point, range);
    }

    [Export("selectionRectsForRange:")]
    public UITextSelectionRect[] GetSelectionRects(UITextRange range)
    {
        // Swapping to the Japanese keyboard asks with UIKit's own positions.
        if (range.Start is not FlutterTextPosition)
        {
            return [];
        }

        int start = IndexOf(range.Start);
        int end = IndexOf(range.End);
        int composedLength =
            (int)TextInputConfiguration.RangeForCharactersInRange(Text, new NSRange(0, Text.Length)).Length;
        List<UITextSelectionRect> result = [];
        IReadOnlyList<FlutterTextSelectionRect> rects = SelectionRects;
        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i].Position >= start
                && (rects[i].Position < end || (start == end && rects[i].Position <= end)))
            {
                CGRect rect = rects[i].Rect;
                result.Add(new FlutterTextSelectionRect(
                    new CGRect(rect.X, rect.Y, start == end ? 0 : rect.Width, rect.Height),
                    rects[i].Position,
                    NSWritingDirection.Natural,
                    containsStart: i == 0,
                    containsEnd: i == composedLength));
            }
        }

        return [.. result];
    }

    [Export("closestPositionToPoint:withinRange:")]
    public UITextPosition GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
    {
        int start = IndexOf(withinRange.Start);
        int end = IndexOf(withinRange.End);
        // The floating cursor is less precise than the pencil: allow more vertical deviation.
        double verticalPrecision = _isFloatingCursorActive ? 10 : 1;
        IReadOnlyList<FlutterTextSelectionRect> rects = SelectionRects;
        bool isFirst = true;
        int closestRectIndex = 0;
        for (int i = 0; i < rects.Count; i++)
        {
            int position = rects[i].Position;
            if (position >= start && position <= end
                && (isFirst || TextInputConfiguration.IsSelectionRectBoundaryCloserToPoint(
                    point, rects[i].Rect, rects[i].IsRtl, false, rects[closestRectIndex].Rect,
                    rects[closestRectIndex].IsRtl, verticalPrecision)))
            {
                isFirst = false;
                closestRectIndex = i;
            }
        }

        var closestPosition = new FlutterTextPosition(
            rects.Count > 0 ? rects[closestRectIndex].Position : 0, UITextStorageDirection.Forward);
        // The far side of the closest rect or its neighbors may fit better (tapping the end of a line).
        for (int i = Math.Max(0, closestRectIndex - 1); i < Math.Min(closestRectIndex + 2, rects.Count); i++)
        {
            int position = rects[i].Position + 1;
            if (position >= start && position <= end
                && TextInputConfiguration.IsSelectionRectBoundaryCloserToPoint(
                    point, rects[i].Rect, rects[i].IsRtl, true, rects[closestRectIndex].Rect,
                    rects[closestRectIndex].IsRtl, verticalPrecision))
            {
                closestPosition = new FlutterTextPosition(position, UITextStorageDirection.Backward);
            }
        }

        return closestPosition;
    }

    [Export("characterRangeAtPoint:")]
    public UITextRange GetCharacterRangeAtPoint(CGPoint point) =>
        new FlutterTextRange(TextInputConfiguration.RangeForCharacterAtIndex(Text, _selectedTextRange.Location));

    [Export("insertDictationResultPlaceholder")]
    public NSObject InsertDictationResultPlaceholder() => new NSString(string.Empty);

    [Export("removeDictationResultPlaceholder:willInsertResult:")]
    public void RemoveDictationResultPlaceholder(NSObject placeholder, bool willInsertResult)
    {
    }

    // The floating cursor: the point relative to where the force press began goes to the framework, which animates
    // the cursor and moves the selection when the gesture ends.
    [Export("beginFloatingCursorAtPoint:")]
    public void BeginFloatingCursor(CGPoint point)
    {
        _isFloatingCursorActive = true;
        _floatingCursorOffset = point;
        Plugin?.UpdateFloatingCursor("FloatingCursorDragState.start", TextInputClient, 0, 0);
    }

    [Export("updateFloatingCursorAtPoint:")]
    public void UpdateFloatingCursor(CGPoint point) =>
        Plugin?.UpdateFloatingCursor(
            "FloatingCursorDragState.update", TextInputClient, point.X - _floatingCursorOffset.X,
            point.Y - _floatingCursorOffset.Y);

    [Export("endFloatingCursor")]
    public void EndFloatingCursor()
    {
        _isFloatingCursorActive = false;
        Plugin?.UpdateFloatingCursor("FloatingCursorDragState.end", TextInputClient, 0, 0);
    }

    [Export("insertTextPlaceholderWithSize:")]
    public UITextPlaceholder InsertTextPlaceholder(CGSize size)
    {
        Plugin?.InsertTextPlaceholder(size, TextInputClient);
        return new FlutterTextPlaceholder();
    }

    [Export("removeTextPlaceholder:")]
    public void RemoveTextPlaceholder(UITextPlaceholder textPlaceholder) =>
        Plugin?.RemoveTextPlaceholder(TextInputClient);

    // UIKeyInput.
    [Export("insertText:")]
    public void InsertText(string text)
    {
        if (_temporarilyDeletedComposedCharacter is { Length: > 0 } deleted && text.Length == 1
            && char.IsSurrogate(text[0]) && text[0] == deleted[0])
        {
            // iOS corrupts the next insertion after deleting an emoji (flutter/flutter#111494).
            text = deleted;
            _temporarilyDeletedComposedCharacter = null;
        }

        int insertPosition = _selectedTextRange.Location;
        List<FlutterTextSelectionRect> copiedRects = new(SelectionRects.Count);
        foreach (FlutterTextSelectionRect rect in SelectionRects)
        {
            int rectPosition = rect.Position;
            if (rectPosition == insertPosition)
            {
                for (int j = 0; j <= text.Length; j++)
                {
                    copiedRects.Add(new FlutterTextSelectionRect(rect.Rect, rectPosition + j, rect.WritingDirection));
                }
            }
            else
            {
                if (rectPosition > insertPosition)
                {
                    rectPosition += text.Length;
                }

                copiedRects.Add(new FlutterTextSelectionRect(rect.Rect, rectPosition, rect.WritingDirection));
            }
        }

        SelectionRects = copiedRects;
        _selectionAffinity = TextAffinityDownstream;
        ReplaceText(_selectedTextRange, text);
    }

    [Export("deleteBackward")]
    public void DeleteBackward()
    {
        _selectionAffinity = TextAffinityDownstream;
        // A Thai vowel deletes only part of its grapheme cluster; an emoji must go whole so the text stays valid.
        if (_selectedTextRange.IsEmpty && HasText)
        {
            NSRange oldRange = _selectedTextRange.Range;
            if (oldRange.Location > 0)
            {
                var newRange = new NSRange(oldRange.Location - 1, 1);
                NSRange charRange = TextInputConfiguration.RangeForCharacterAtIndex(Text, (int)oldRange.Location - 1);
                if (TextInputConfiguration.IsEmoji(Text, charRange))
                {
                    newRange = new NSRange(charRange.Location, oldRange.Location - charRange.Location);
                }

                _selectedTextRange = new FlutterTextRange(newRange);
            }
        }

        if (!_selectedTextRange.IsEmpty)
        {
            // The deleted emoji's first composed character, for the next insertion's workaround.
            if (TextInputConfiguration.IsEmoji(Text, _selectedTextRange.Range))
            {
                string deletedText = Text.Substring(_selectedTextRange.Location, _selectedTextRange.Length);
                NSRange firstCharacter = TextInputConfiguration.RangeForCharacterAtIndex(deletedText, 0);
                _temporarilyDeletedComposedCharacter =
                    deletedText.Substring((int)firstCharacter.Location, (int)firstCharacter.Length);
            }

            ReplaceText(_selectedTextRange, string.Empty);
        }
    }

    /// <summary>showEditMenuWithTargetRect:items: (iOS 16).</summary>
    public void ShowEditMenu(CGRect targetRect, IList? items)
    {
        _editMenuTargetRect = targetRect;
        _editMenuItems = items;
        if (OperatingSystem.IsIOSVersionAtLeast(16) && _editMenuInteraction is UIEditMenuInteraction interaction)
        {
            interaction.PresentEditMenu(UIEditMenuConfiguration.Create(null, CGPoint.Empty));
        }
    }

    public void HideEditMenu()
    {
        if (OperatingSystem.IsIOSVersionAtLeast(16) && _editMenuInteraction is UIEditMenuInteraction interaction)
        {
            interaction.DismissMenu();
        }
    }

    /// <summary>The selection's rect in this view (the share sheet's popover source on iPad).</summary>
    public CGRect SelectionRectInView()
    {
        CGRect first = LocalRectFromFrameworkTransform(GetCaretRectForPosition(_selectedTextRange.Start));
        CGRect last = LocalRectFromFrameworkTransform(GetCaretRectForPosition(_selectedTextRange.End));
        return new CGRect(Math.Min(first.X, last.X), first.Y, Math.Abs(last.X - first.X), first.Height);
    }

    [Export("handleSearchWebAction")]
    public void HandleSearchWebAction() => Plugin?.SearchWeb(TextInRange(_selectedTextRange) ?? string.Empty);

    [Export("handleLookUpAction")]
    public void HandleLookUpAction() => Plugin?.LookUp(TextInRange(_selectedTextRange) ?? string.Empty);

    [Export("handleShareAction")]
    public void HandleShareAction() => Plugin?.Share(TextInRange(_selectedTextRange) ?? string.Empty);

    /// <summary>updateEditingState: the whole editing state to the framework.</summary>
    private void UpdateEditingState()
    {
        Dictionary<string, object?> state = EditingStateFields();
        state["text"] = Text;
        if (TextInputClient == 0 && AutofillId is not null)
        {
            Plugin?.UpdateEditingClient(TextInputClient, state, AutofillId);
        }
        else
        {
            Plugin?.UpdateEditingClient(TextInputClient, state, null);
        }
    }

    /// <summary>updateEditingStateWithDelta: deltas batch until the next main-queue turn.</summary>
    private void UpdateEditingStateWithDelta(string oldText, int deltaStart, int deltaEnd, string deltaText)
    {
        Dictionary<string, object?> delta = EditingStateFields();
        delta["oldText"] = oldText;
        delta["deltaText"] = deltaText;
        delta["deltaStart"] = deltaStart;
        delta["deltaEnd"] = deltaEnd;
        _pendingDeltas.Add(delta);
        if (_pendingDeltas.Count == 1)
        {
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                if (_pendingDeltas.Count > 0)
                {
                    Plugin?.UpdateEditingClientWithDeltas(TextInputClient, [.. _pendingDeltas]);
                    _pendingDeltas.Clear();
                }
            });
        }
    }

    private Dictionary<string, object?> EditingStateFields()
    {
        // An empty composing range is the framework's TextRange.empty.
        int composingBase = _markedTextRange?.Location ?? -1;
        int composingExtent = _markedTextRange is { } marked ? marked.Location + marked.Length : -1;
        return new Dictionary<string, object?>
        {
            ["selectionBase"] = _selectedTextRange.Location,
            ["selectionExtent"] = _selectedTextRange.Location + _selectedTextRange.Length,
            ["selectionAffinity"] = _selectionAffinity,
            ["selectionIsDirectional"] = false,
            ["composingBase"] = composingBase,
            ["composingExtent"] = composingExtent,
        };
    }

    /// <summary>setSelectedTextRangeLocal: snapped to composed character sequences when there is text.</summary>
    private void SetSelectedTextRangeLocal(FlutterTextRange range) =>
        _selectedTextRange = HasText
            ? new FlutterTextRange(TextInputConfiguration.RangeForCharactersInRange(Text, range.Range))
            : new FlutterTextRange(range.Range);

    private int DecrementOffsetPosition(int position) =>
        (int)TextInputConfiguration.RangeForCharacterAtIndex(Text, Math.Max(0, position - 1)).Location;

    private int IncrementOffsetPosition(int position)
    {
        NSRange charRange = TextInputConfiguration.RangeForCharacterAtIndex(Text, position);
        return (int)Math.Min(position + charRange.Length, Text.Length);
    }

    // clampSelectionFromBase:extent:forText: an invalid selection becomes (0, 0).
    private static NSRange ClampSelectionFromBase(int selectionBase, int selectionExtent, string text)
    {
        int location = Math.Min(selectionBase, selectionExtent);
        int length = Math.Abs(selectionExtent - selectionBase);
        return location < 0 ? new NSRange(0, 0) : ClampSelection(new NSRange(location, length), text);
    }

    private static NSRange ClampSelection(NSRange range, string text)
    {
        nint start = Math.Min(Math.Max(range.Location, 0), text.Length);
        nint length = Math.Min(range.Length, text.Length - start);
        return new NSRange(start, length);
    }

    private static int IndexOf(UITextPosition position) => (position as FlutterTextPosition)?.Index ?? 0;

    private static UITextStorageDirection AffinityOf(UITextPosition position) =>
        (position as FlutterTextPosition)?.Affinity ?? UITextStorageDirection.Forward;

    private static int ToInt(object? value) =>
        value is null ? 0 : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

    // The system edit menu (iOS 16): the framework's items, mapped to the suggested UIKit commands.
    private sealed class EditMenuDelegate(IosTextInputView owner) : UIEditMenuInteractionDelegate
    {
        public override UIMenu? GetMenu(
            UIEditMenuInteraction interaction, UIEditMenuConfiguration configuration, UIMenuElement[] suggestedActions)
        {
            UIMenu suggestedMenu = UIMenu.Create(suggestedActions);
            if (owner._editMenuItems is not { } encodedItems)
            {
                return suggestedMenu;
            }

            List<UIMenuElement> items = [];
            foreach (object? item in encodedItems)
            {
                if (item is not IDictionary encodedItem)
                {
                    continue;
                }

                string? type = encodedItem["type"] as string;
                switch (type)
                {
                    case "copy":
                        AddBasic(items, type, "copy:", suggestedMenu);
                        break;
                    case "paste":
                        AddBasic(items, type, "paste:", suggestedMenu);
                        break;
                    case "cut":
                        AddBasic(items, type, "cut:", suggestedMenu);
                        break;
                    case "delete":
                        AddBasic(items, type, "delete:", suggestedMenu);
                        break;
                    case "selectAll":
                        AddBasic(items, type, "selectAll:", suggestedMenu);
                        break;
                    case "captureTextFromCamera":
                        AddBasic(items, type, "captureTextFromCamera:", suggestedMenu);
                        break;
                    case "searchWeb":
                        AddAdditional(items, type, "handleSearchWebAction", encodedItem);
                        break;
                    case "share":
                        AddAdditional(items, type, "handleShareAction", encodedItem);
                        break;
                    case "lookUp":
                        AddAdditional(items, type, "handleLookUpAction", encodedItem);
                        break;
                    case "custom":
                        if (encodedItem["id"] is string callbackId && encodedItem["title"] is string title)
                        {
                            items.Add(UIAction.Create(title, null, null, _ =>
                                owner.Plugin?.PerformContextMenuCustomAction(callbackId, owner.TextInputClient)));
                        }

                        break;
                }
            }

            return UIMenu.Create(items.ToArray());
        }

        public override void WillDismissMenu(
            UIEditMenuInteraction interaction,
            UIEditMenuConfiguration configuration,
            IUIEditMenuInteractionAnimating? animator) =>
            owner.Plugin?.WillDismissEditMenu(owner.TextInputClient);

        public override CGRect GetTargetRect(
            UIEditMenuInteraction interaction, UIEditMenuConfiguration configuration) =>
            owner._editMenuTargetRect;

        private static void AddBasic(List<UIMenuElement> items, string type, string selector, UIMenu suggestedMenu)
        {
            if (SearchCommand(new Selector(selector), suggestedMenu) is { } command)
            {
                items.Add(command);
            }
            else
            {
                Console.Error.WriteLine($"Cannot find context menu item of type \"{type}\".");
            }
        }

        private static void AddAdditional(List<UIMenuElement> items, string type, string selector, IDictionary item)
        {
            if (item["title"] is string title)
            {
                items.Add(UICommand.Create(title, null, new Selector(selector), null));
            }
            else
            {
                Console.Error.WriteLine($"Missing title for context menu item of type \"{type}\".");
            }
        }

        // searchCommandWithSelector:element: depth-first through the menu tree.
        private static UICommand? SearchCommand(Selector selector, UIMenuElement element)
        {
            if (element is UICommand command)
            {
                return command.Action?.Handle == selector.Handle ? command : null;
            }

            if (element is UIMenu menu)
            {
                foreach (UIMenuElement child in menu.Children)
                {
                    if (SearchCommand(selector, child) is { } result)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
