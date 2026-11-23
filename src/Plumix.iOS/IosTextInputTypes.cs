using System.Collections;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Plumix.iOS;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterTextInputPlugin.mm (FlutterTextPosition,
// FlutterTextRange, FlutterTokenizer, FlutterTextSelectionRect, FlutterTextPlaceholder and the static configuration
// functions) and flutter/engine/src/flutter/fml/platform/darwin/string_range_sanitization.mm. Indices are UTF-16 code
// units, as in NSString and C# strings.

/// <summary>A UTF-16 index with UIKit's storage-direction affinity.</summary>
[Register("PlumixTextPosition")]
internal sealed class FlutterTextPosition : UITextPosition
{
    public FlutterTextPosition(int index, UITextStorageDirection affinity = UITextStorageDirection.Forward)
    {
        Index = index;
        Affinity = affinity;
    }

    public int Index { get; }

    public UITextStorageDirection Affinity { get; }
}

/// <summary>A UTF-16 range; its start has forward affinity, its end backward.</summary>
[Register("PlumixTextRange")]
internal sealed class FlutterTextRange : UITextRange
{
    public FlutterTextRange(NSRange range)
    {
        Range = range;
    }

    public FlutterTextRange(int location, int length)
        : this(new NSRange(location, length))
    {
    }

    public NSRange Range { get; }

    public int Location => (int)Range.Location;

    public int Length => (int)Range.Length;

    public override UITextPosition Start => new FlutterTextPosition(Location, UITextStorageDirection.Forward);

    public override UITextPosition End =>
        new FlutterTextPosition(Location + Length, UITextStorageDirection.Backward);

    public override bool IsEmpty => Range.Length == 0;

    [Export("copyWithZone:")]
    [return: Release]
    public NSObject CopyWithZone(IntPtr zone) => new FlutterTextRange(Range);
}

/// <summary>A selection rect the framework reported for one character position.</summary>
[Register("PlumixTextSelectionRect")]
internal sealed class FlutterTextSelectionRect : UITextSelectionRect
{
    private readonly CGRect _rect;
    private readonly NSWritingDirection _writingDirection;
    private readonly bool _containsStart;
    private readonly bool _containsEnd;
    private readonly bool _isVertical;

    public FlutterTextSelectionRect(
        CGRect rect,
        int position,
        NSWritingDirection writingDirection = NSWritingDirection.Natural,
        bool containsStart = false,
        bool containsEnd = false,
        bool isVertical = false)
    {
        _rect = rect;
        Position = position;
        _writingDirection = writingDirection;
        _containsStart = containsStart;
        _containsEnd = containsEnd;
        _isVertical = isVertical;
    }

    public int Position { get; }

    public bool IsRtl => _writingDirection == NSWritingDirection.RightToLeft;

    public override CGRect Rect => _rect;

    public override NSWritingDirection WritingDirection => _writingDirection;

    public override bool ContainsStart => _containsStart;

    public override bool ContainsEnd => _containsEnd;

    public override bool IsVertical => _isVertical;
}

/// <summary>
/// UIKit's string tokenizer with the engine's own line granularity, which the default tokenizer gets wrong.
/// </summary>
[Register("PlumixTokenizer")]
internal sealed class FlutterTokenizer : UITextInputStringTokenizer
{
    private readonly WeakReference<IosTextInputView> _textInputView;

    public FlutterTokenizer(IosTextInputView textInput)
        : base(textInput)
    {
        _textInputView = new WeakReference<IosTextInputView>(textInput);
    }

#pragma warning disable CS8764 // UIKit takes nil: no enclosing range.
    public override UITextRange? GetRangeEnclosingPosition(
        UITextPosition position, UITextGranularity granularity, UITextDirection direction) =>
        granularity == UITextGranularity.Line
            ? LineEnclosingPosition(position, direction)
            : base.GetRangeEnclosingPosition(position, granularity, direction);
#pragma warning restore CS8764

    private UITextRange? LineEnclosingPosition(UITextPosition position, UITextDirection direction)
    {
        if (!_textInputView.TryGetTarget(out IosTextInputView? view))
        {
            return null;
        }

        var flutterPosition = (FlutterTextPosition)position;
        if (OperatingSystem.IsIOSVersionAtLeast(17)
            && (flutterPosition.Index > view.Text.Length
                || (flutterPosition.Index == view.Text.Length
                    && direction == (UITextDirection)UITextStorageDirection.Forward)))
        {
            // At a boundary, a position is enclosed only if the next position in the direction is.
            return null;
        }

        string textAfter = view.TextInRange(view.GetTextRange(position, view.EndOfDocument)) ?? string.Empty;
        int offsetToLineBreak = textAfter.Split('\n')[0].Length;
        UITextPosition lineBreakAfter = view.GetPosition(position, offsetToLineBreak) ?? view.EndOfDocument;
        string textBefore = view.TextInRange(view.GetTextRange(view.BeginningOfDocument, position)) ?? string.Empty;
        string[] linesBefore = textBefore.Split('\n');
        int offsetFromLineBreak = linesBefore[^1].Length;
        UITextPosition lineBreakBefore = view.GetPosition(position, -offsetFromLineBreak) ?? view.BeginningOfDocument;
        return view.GetTextRange(lineBreakBefore, lineBreakAfter);
    }
}

/// <summary>The engine's text placeholder: no rects, which keeps PencilKit from looping.</summary>
[Register("PlumixTextPlaceholder")]
internal sealed class FlutterTextPlaceholder : UITextPlaceholder
{
    public override UITextSelectionRect[] Rects => [];
}

/// <summary>The engine's static configuration functions and NSString range helpers.</summary>
internal static class TextInputConfiguration
{
    // ICU's UCHAR_EMOJI.
    private const int UCharEmoji = 57;

    /// <summary>fml::RangeForCharacterAtIndex: the composed character sequence at the index.</summary>
    public static NSRange RangeForCharacterAtIndex(string text, int index)
    {
        if (index < 0 || index > text.Length)
        {
            return new NSRange(NSRange.NotFound, 0);
        }

        if (index < text.Length)
        {
            using var nsText = new NSString(text);
            return RangeOfComposedCharacterSequenceAtIndex(
                nsText.Handle, Selector.GetHandle("rangeOfComposedCharacterSequenceAtIndex:"), (nuint)index);
        }

        return new NSRange(index, 0);
    }

    /// <summary>
    /// fml::RangeForCharactersInRange: the range's start snapped to a composed sequence, length kept.
    /// </summary>
    public static NSRange RangeForCharactersInRange(string text, NSRange range)
    {
        if (range.Location < 0 || range.Location + range.Length > text.Length)
        {
            return new NSRange(NSRange.NotFound, 0);
        }

        using var nsText = new NSString(text);
        NSRange sanitized = RangeOfComposedCharacterSequencesForRange(
            nsText.Handle, Selector.GetHandle("rangeOfComposedCharacterSequencesForRange:"), range);
        return new NSRange(sanitized.Location, range.Length);
    }

    /// <summary>IsEmoji: whether the range's first code point has ICU's Emoji property.</summary>
    public static bool IsEmoji(string text, NSRange range)
    {
        if (range.Location < 0 || range.Location >= text.Length || range.Length <= 0)
        {
            return false;
        }

        int index = (int)range.Location;
        int codePoint;
        if (char.IsHighSurrogate(text[index]))
        {
            // A lone surrogate is no code point: NSString's UTF-32 conversion fails.
            if (range.Length < 2 || index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
            {
                return false;
            }

            codePoint = char.ConvertToUtf32(text[index], text[index + 1]);
        }
        else if (char.IsLowSurrogate(text[index]))
        {
            return false;
        }
        else
        {
            codePoint = text[index];
        }

        return u_hasBinaryProperty(codePoint, UCharEmoji) != 0;
    }

    /// <summary>ShouldShowSystemKeyboard: "TextInputType.none" is the in-app keyboard's made-up type.</summary>
    public static bool ShouldShowSystemKeyboard(IDictionary? type) =>
        type?["name"] as string != "TextInputType.none";

    public static UIKeyboardType ToUIKeyboardType(IDictionary? type) =>
        (type?["name"] as string) switch
        {
            "TextInputType.datetime" => UIKeyboardType.NumbersAndPunctuation,
            "TextInputType.emailAddress" => UIKeyboardType.EmailAddress,
            "TextInputType.name" => UIKeyboardType.NamePhonePad,
            "TextInputType.number" when type!["signed"] is true => UIKeyboardType.NumbersAndPunctuation,
            "TextInputType.number" when type!["decimal"] is true => UIKeyboardType.DecimalPad,
            "TextInputType.number" => UIKeyboardType.NumberPad,
            "TextInputType.phone" => UIKeyboardType.PhonePad,
            "TextInputType.url" => UIKeyboardType.Url,
            "TextInputType.visiblePassword" => UIKeyboardType.AsciiCapable,
            "TextInputType.webSearch" => UIKeyboardType.WebSearch,
            "TextInputType.twitter" => UIKeyboardType.Twitter,
            _ => UIKeyboardType.Default,
        };

    public static UITextAutocapitalizationType ToUITextAutoCapitalizationType(IDictionary configuration) =>
        (configuration["textCapitalization"] as string) switch
        {
            "TextCapitalization.characters" => UITextAutocapitalizationType.AllCharacters,
            "TextCapitalization.sentences" => UITextAutocapitalizationType.Sentences,
            "TextCapitalization.words" => UITextAutocapitalizationType.Words,
            _ => UITextAutocapitalizationType.None,
        };

    public static UIReturnKeyType ToUIReturnKeyType(string? inputAction) =>
        inputAction switch
        {
            "TextInputAction.done" => UIReturnKeyType.Done,
            "TextInputAction.go" => UIReturnKeyType.Go,
            "TextInputAction.send" => UIReturnKeyType.Send,
            "TextInputAction.search" => UIReturnKeyType.Search,
            "TextInputAction.next" => UIReturnKeyType.Next,
            "TextInputAction.continueAction" => UIReturnKeyType.Continue,
            "TextInputAction.join" => UIReturnKeyType.Join,
            "TextInputAction.route" => UIReturnKeyType.Route,
            "TextInputAction.emergencyCall" => UIReturnKeyType.EmergencyCall,
            _ => UIReturnKeyType.Default,
        };

    /// <summary>ToUITextContentType: the first hint as UIKit's content type; unknown hints pass through.</summary>
    public static NSString? ToUITextContentType(IList? hints)
    {
        if (hints is null || hints.Count == 0 || hints[0] is not string hint)
        {
            return null;
        }

        return hint switch
        {
            "addressCityAndState" => UITextContentType.AddressCityAndState,
            "addressState" => UITextContentType.AddressState,
            "addressCity" => UITextContentType.AddressCity,
            "sublocality" => UITextContentType.Sublocality,
            "streetAddressLine1" => UITextContentType.StreetAddressLine1,
            "streetAddressLine2" => UITextContentType.StreetAddressLine2,
            "countryName" => UITextContentType.CountryName,
            "fullStreetAddress" => UITextContentType.FullStreetAddress,
            "postalCode" => UITextContentType.PostalCode,
            "location" => UITextContentType.Location,
            "creditCardNumber" => UITextContentType.CreditCardNumber,
            "email" => UITextContentType.EmailAddress,
            "jobTitle" => UITextContentType.JobTitle,
            "givenName" => UITextContentType.GivenName,
            "middleName" => UITextContentType.MiddleName,
            "familyName" => UITextContentType.FamilyName,
            "name" => UITextContentType.Name,
            "namePrefix" => UITextContentType.NamePrefix,
            "nameSuffix" => UITextContentType.NameSuffix,
            "nickname" => UITextContentType.Nickname,
            "organizationName" => UITextContentType.OrganizationName,
            "telephoneNumber" => UITextContentType.TelephoneNumber,
            "password" => UITextContentType.Password,
            "oneTimeCode" => UITextContentType.OneTimeCode,
            "newPassword" => UITextContentType.NewPassword,
            _ => new NSString(hint),
        };
    }

    /// <summary>AutofillIdFromDictionary: the autofill id, or "password" for a secure field without autofill.</summary>
    public static string? AutofillIdFromDictionary(IDictionary dictionary) =>
        dictionary["autofill"] is IDictionary autofill
            ? autofill["uniqueIdentifier"] as string
            : dictionary["obscureText"] is true ? "password" : null;

    private static bool IsFieldPasswordRelated(IDictionary configuration)
    {
        if (AutofillIdFromDictionary(configuration) is null)
        {
            return false;
        }

        if (configuration["obscureText"] is true)
        {
            return true;
        }

        NSString? contentType = ToUITextContentType((configuration["autofill"] as IDictionary)?["hints"] as IList);
        return contentType is not null
               && (contentType == UITextContentType.Password || contentType == UITextContentType.Username
                   || contentType == UITextContentType.NewPassword);
    }

    /// <summary>AutofillTypeOf.</summary>
    public static AutofillType AutofillTypeOf(IDictionary configuration)
    {
        if (configuration["fields"] is IList fields)
        {
            foreach (object? field in fields)
            {
                if (field is IDictionary dictionary && IsFieldPasswordRelated(dictionary))
                {
                    return AutofillType.Password;
                }
            }
        }

        if (IsFieldPasswordRelated(configuration))
        {
            return AutofillType.Password;
        }

        var autofill = configuration["autofill"] as IDictionary;
        NSString? contentType = ToUITextContentType(autofill?["hints"] as IList);
        return autofill is null || contentType?.ToString() == string.Empty ? AutofillType.None : AutofillType.Regular;
    }

    /// <summary>IsSelectionRectBoundaryCloserToPoint, the floating cursor's empirically tuned comparison.</summary>
    public static bool IsSelectionRectBoundaryCloserToPoint(
        CGPoint point,
        CGRect selectionRect,
        bool selectionRectIsRtl,
        bool useTrailingBoundaryOfSelectionRect,
        CGRect otherSelectionRect,
        bool otherSelectionRectIsRtl,
        double verticalPrecision)
    {
        var halfRect = new CGRect(
            selectionRect.X + ((useTrailingBoundaryOfSelectionRect ^ selectionRectIsRtl)
                ? 0.5 * selectionRect.Width
                : 0),
            selectionRect.Y, 0.5 * selectionRect.Width, selectionRect.Height);
        if (halfRect.Contains(point))
        {
            return true;
        }

        var pointForSelectionRect = new CGPoint(
            selectionRect.X + (selectionRectIsRtl ^ useTrailingBoundaryOfSelectionRect ? selectionRect.Width : 0),
            selectionRect.Y + (selectionRect.Height * 0.5));
        float yDist = (float)Math.Abs(pointForSelectionRect.Y - point.Y);
        float xDist = (float)Math.Abs(pointForSelectionRect.X - point.X);
        var pointForOtherSelectionRect = new CGPoint(
            otherSelectionRect.X + (otherSelectionRectIsRtl ? otherSelectionRect.Width : 0),
            otherSelectionRect.Y + (otherSelectionRect.Height * 0.5));
        float yDistOther = (float)Math.Abs(pointForOtherSelectionRect.Y - point.Y);
        float xDistOther = (float)Math.Abs(pointForOtherSelectionRect.X - point.X);

        bool isCloserVertically = yDist < yDistOther - verticalPrecision;
        bool isEqualVertically = Math.Abs(yDist - yDistOther) <= (float)verticalPrecision;
        bool isAboveBottomOfLine = point.Y <= selectionRect.Y + selectionRect.Height;
        bool isCloserHorizontally = xDist < xDistOther;
        bool isBelowBottomOfLine = point.Y > selectionRect.Y + selectionRect.Height;
        bool isFarther = selectionRectIsRtl
            ? selectionRect.X < otherSelectionRect.X
            : selectionRect.X + (useTrailingBoundaryOfSelectionRect ? selectionRect.Width : 0)
              > otherSelectionRect.X;
        return isCloserVertically
               || (isEqualVertically
                   && ((isAboveBottomOfLine && isCloserHorizontally) || (isBelowBottomOfLine && isFarther)));
    }

    // NSString's composed-sequence methods have no binding; NSRange returns in registers on arm64 and x64.
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern NSRange RangeOfComposedCharacterSequenceAtIndex(
        IntPtr receiver, IntPtr selector, nuint index);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern NSRange RangeOfComposedCharacterSequencesForRange(
        IntPtr receiver, IntPtr selector, NSRange range);

    [DllImport("/usr/lib/libicucore.dylib")]
    private static extern sbyte u_hasBinaryProperty(int codePoint, int property);
}

/// <summary>FlutterAutofillType.</summary>
internal enum AutofillType
{
    None,
    Regular,
    Password,
}
