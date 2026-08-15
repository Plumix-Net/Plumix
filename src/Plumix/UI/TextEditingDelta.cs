using System.Collections;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_editing_delta.dart

/// <summary>
/// A structure representing a granular change that has occurred to the editing state as a result of
/// text editing.
/// </summary>
/// <remarks>
/// To opt a client in to receiving deltas, set
/// <see cref="TextInputConfiguration.EnableDeltaModel"/> to <c>true</c>.
/// </remarks>
public abstract class TextEditingDelta : Diagnosticable
{
    /// <summary>Creates a delta for a given change to the editing state.</summary>
    protected TextEditingDelta(string oldText, TextSelection selection, TextRange composing)
    {
        OldText = oldText;
        Selection = selection;
        Composing = composing;
    }

    /// <summary>The old text state before the delta has occurred.</summary>
    public string OldText { get; }

    /// <summary>The range of text that is selected after the delta has been applied.</summary>
    public TextSelection Selection { get; }

    /// <summary>The range of text that is still being composed after the delta has been applied.
    /// </summary>
    public TextRange Composing { get; }

    /// <summary>
    /// Creates an instance from a JSON object by inferring the type of delta from the values sent by
    /// the engine.
    /// </summary>
    public static TextEditingDelta FromJson(IDictionary encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);

        // An insertion delta is one where replacement destination is collapsed.
        //
        // A deletion delta is one where the replacement source is empty.
        //
        // On native platforms when composing text, the entire composing region is replaced on input,
        // rather than reporting character by character insertion/deletion. In those cases the delta
        // is an insertion/deletion when the text inside the original composing region was not
        // modified by the replacement, and a replacement when it was.
        string oldText = ReadString(encoded, "oldText");
        int replacementDestinationStart = ReadInt(encoded, "deltaStart", -1);
        int replacementDestinationEnd = ReadInt(encoded, "deltaEnd", -1);
        string replacementSource = ReadString(encoded, "deltaText");
        const int replacementSourceStart = 0;
        int replacementSourceEnd = replacementSource.Length;

        // This delta is explicitly a non text update.
        bool isNonTextUpdate = replacementDestinationStart == -1
                               && replacementDestinationStart == replacementDestinationEnd;
        var newComposing = new TextRange(
            Start: ReadInt(encoded, "composingBase", -1),
            End: ReadInt(encoded, "composingExtent", -1));
        var newSelection = new TextSelection(
            BaseOffset: ReadInt(encoded, "selectionBase", -1),
            ExtentOffset: ReadInt(encoded, "selectionExtent", -1));

        if (isNonTextUpdate)
        {
            RequireTextRangeIsValid(newSelection.AsTextRange(), oldText, "selection range");
            RequireTextRangeIsValid(newComposing, oldText, "composing range");
            return new TextEditingDeltaNonTextUpdate(oldText, newSelection, newComposing);
        }

        RequireTextRangeIsValid(
            new TextRange(replacementDestinationStart, replacementDestinationEnd),
            oldText,
            "delta range");

        string newText = Replace(
            oldText,
            replacementSource,
            new TextRange(replacementDestinationStart, replacementDestinationEnd));

        RequireTextRangeIsValid(newSelection.AsTextRange(), newText, "selection range");
        RequireTextRangeIsValid(newComposing, newText, "composing range");

        bool isEqual = oldText == newText;

        bool isDeletionGreaterThanOne =
            (replacementDestinationEnd - replacementDestinationStart)
            - (replacementSourceEnd - replacementSourceStart) > 1;
        bool isDeletingByReplacingWithEmpty = replacementSource.Length == 0
                                              && replacementSourceStart == 0
                                              && replacementSourceStart == replacementSourceEnd;

        bool isReplacedByShorter = isDeletionGreaterThanOne
                                   && replacementSourceEnd - replacementSourceStart
                                   < replacementDestinationEnd - replacementDestinationStart;
        bool isReplacedByLonger = replacementSourceEnd - replacementSourceStart
                                  > replacementDestinationEnd - replacementDestinationStart;
        bool isReplacedBySame = replacementSourceEnd - replacementSourceStart
                                == replacementDestinationEnd - replacementDestinationStart;

        bool isInsertingInsideComposingRegion =
            replacementDestinationStart + replacementSourceEnd > replacementDestinationEnd;
        bool isDeletingInsideComposingRegion =
            !isReplacedByShorter
            && !isDeletingByReplacingWithEmpty
            && replacementDestinationStart + replacementSourceEnd < replacementDestinationEnd;

        string newComposingText;
        string originalComposingText;

        if (isDeletingByReplacingWithEmpty || isDeletingInsideComposingRegion || isReplacedByShorter)
        {
            newComposingText = replacementSource[replacementSourceStart..replacementSourceEnd];
            originalComposingText = oldText[
                replacementDestinationStart..(replacementDestinationStart + replacementSourceEnd)];
        }
        else
        {
            newComposingText = replacementSource[
                replacementSourceStart..(replacementSourceStart
                                         + (replacementDestinationEnd - replacementDestinationStart))];
            originalComposingText = oldText[replacementDestinationStart..replacementDestinationEnd];
        }

        bool isOriginalComposingRegionTextChanged = originalComposingText != newComposingText;
        bool isReplaced = isOriginalComposingRegionTextChanged
                          || isReplacedByLonger
                          || isReplacedByShorter
                          || isReplacedBySame;

        if (isEqual)
        {
            return new TextEditingDeltaNonTextUpdate(oldText, newSelection, newComposing);
        }

        if ((isDeletingByReplacingWithEmpty || isDeletingInsideComposingRegion)
            && !isOriginalComposingRegionTextChanged)
        {
            // Deletion.
            int actualStart = replacementDestinationStart;
            if (!isDeletionGreaterThanOne)
            {
                actualStart = replacementDestinationEnd - 1;
            }

            return new TextEditingDeltaDeletion(
                oldText,
                new TextRange(actualStart, replacementDestinationEnd),
                newSelection,
                newComposing);
        }

        if ((replacementDestinationStart == replacementDestinationEnd || isInsertingInsideComposingRegion)
            && !isOriginalComposingRegionTextChanged)
        {
            // Insertion.
            int insertedStart = replacementDestinationEnd - replacementDestinationStart;
            int insertedEnd = insertedStart + (replacementSource.Length - insertedStart);
            return new TextEditingDeltaInsertion(
                oldText,
                replacementSource[insertedStart..insertedEnd],
                replacementDestinationEnd,
                newSelection,
                newComposing);
        }

        if (isReplaced)
        {
            // Replacement.
            return new TextEditingDeltaReplacement(
                oldText,
                replacementSource,
                new TextRange(replacementDestinationStart, replacementDestinationEnd),
                newSelection,
                newComposing);
        }

        return new TextEditingDeltaNonTextUpdate(oldText, newSelection, newComposing);
    }

    /// <summary>Applies this delta to <paramref name="value"/> and returns the result.</summary>
    public abstract TextEditingValue Apply(TextEditingValue value);

    /// <summary>Replaces a range of text in the original string with the replacement string.
    /// </summary>
    private protected static string Replace(
        string originalText,
        string replacementText,
        TextRange replacementRange)
    {
        return string.Concat(
            originalText[..replacementRange.Start],
            replacementText,
            originalText[replacementRange.End..]);
    }

    /// <summary>Builds the editing value a delta's <c>apply</c> returns.</summary>
    /// <remarks>Plumix's <see cref="TextEditingValue"/> models "not composing" as a <c>null</c>
    /// composing range where Dart uses <c>TextRange.empty</c>.</remarks>
    private protected static TextEditingValue Compose(
        string text,
        TextSelection selection,
        TextRange composing)
    {
        return new TextEditingValue(
            text: text,
            selection: selection,
            composing: composing.IsValid ? composing : null);
    }

    /// <summary>Verifies that the given range is within the text.</summary>
    /// <remarks>Dart guards this with <c>assert</c>; C# has no assert elision, so the check throws in
    /// every build (the `AutofillScope` precedent).</remarks>
    private protected static void RequireTextRangeIsValid(TextRange range, string text, string what)
    {
        if (!range.IsValid)
        {
            return;
        }

        if (range.Start >= 0 && range.Start <= text.Length && range.End >= 0 && range.End <= text.Length)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            nameof(range),
            $"The {what}: {range} is not within the bounds of text: {text} of length: {text.Length}");
    }

    private static string ReadString(IDictionary json, string key)
    {
        object? value = json.Contains(key) ? json[key] : null;
        return value as string ?? string.Empty;
    }

    private static int ReadInt(IDictionary json, string key, int fallback)
    {
        object? value = json.Contains(key) ? json[key] : null;
        return value is null ? fallback : Convert.ToInt32(value);
    }
}

/// <summary>
/// A structure representing an insertion of a single or contiguous sequence of characters at some
/// offset of an editing state.
/// </summary>
public sealed class TextEditingDeltaInsertion : TextEditingDelta
{
    /// <summary>Creates an insertion delta for a given change to the editing state.</summary>
    public TextEditingDeltaInsertion(
        string oldText,
        string textInserted,
        int insertionOffset,
        TextSelection selection,
        TextRange composing)
        : base(oldText, selection, composing)
    {
        TextInserted = textInserted;
        InsertionOffset = insertionOffset;
    }

    /// <summary>The text that is being inserted into <see cref="TextEditingDelta.OldText"/>.
    /// </summary>
    public string TextInserted { get; }

    /// <summary>The offset in <see cref="TextEditingDelta.OldText"/> where the insertion begins.
    /// </summary>
    public int InsertionOffset { get; }

    /// <inheritdoc/>
    public override TextEditingValue Apply(TextEditingValue value)
    {
        // To stay inline with the plain text model a delta follows a last-write-wins policy and is
        // applied to `oldText`, because the connection to the platform text input plugin is
        // asynchronous.
        string newText = OldText;
        RequireTextRangeIsValid(TextRange.Collapsed(InsertionOffset), newText, "insertionOffset");
        newText = Replace(newText, TextInserted, TextRange.Collapsed(InsertionOffset));
        RequireTextRangeIsValid(Selection.AsTextRange(), newText, "selection range");
        RequireTextRangeIsValid(Composing, newText, "composing range");
        return Compose(newText, Selection, Composing);
    }

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("oldText", OldText));
        properties.Add(new DiagnosticsProperty<string>("textInserted", TextInserted));
        properties.Add(new DiagnosticsProperty<int>("insertionOffset", InsertionOffset));
        properties.Add(new DiagnosticsProperty<TextSelection>("selection", Selection));
        properties.Add(new DiagnosticsProperty<TextRange>("composing", Composing));
    }
}

/// <summary>
/// A structure representing the deletion of a single or contiguous sequence of characters in an
/// editing state.
/// </summary>
public sealed class TextEditingDeltaDeletion : TextEditingDelta
{
    /// <summary>Creates a deletion delta for a given change to the editing state.</summary>
    public TextEditingDeltaDeletion(
        string oldText,
        TextRange deletedRange,
        TextSelection selection,
        TextRange composing)
        : base(oldText, selection, composing)
    {
        DeletedRange = deletedRange;
    }

    /// <summary>The range in <see cref="TextEditingDelta.OldText"/> that is being deleted.</summary>
    public TextRange DeletedRange { get; }

    /// <summary>The text from <see cref="TextEditingDelta.OldText"/> that is being deleted.</summary>
    public string TextDeleted => OldText[DeletedRange.Start..DeletedRange.End];

    /// <inheritdoc/>
    public override TextEditingValue Apply(TextEditingValue value)
    {
        string newText = OldText;
        RequireTextRangeIsValid(DeletedRange, newText, "deletedRange");
        newText = Replace(newText, string.Empty, DeletedRange);
        RequireTextRangeIsValid(Selection.AsTextRange(), newText, "selection range");
        RequireTextRangeIsValid(Composing, newText, "composing range");
        return Compose(newText, Selection, Composing);
    }

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("oldText", OldText));
        properties.Add(new DiagnosticsProperty<string>("textDeleted", TextDeleted));
        properties.Add(new DiagnosticsProperty<TextRange>("deletedRange", DeletedRange));
        properties.Add(new DiagnosticsProperty<TextSelection>("selection", Selection));
        properties.Add(new DiagnosticsProperty<TextRange>("composing", Composing));
    }
}

/// <summary>
/// A structure representing a replacement of a range of characters with a new sequence of text.
/// </summary>
public sealed class TextEditingDeltaReplacement : TextEditingDelta
{
    /// <summary>Creates a replacement delta for a given change to the editing state.</summary>
    public TextEditingDeltaReplacement(
        string oldText,
        string replacementText,
        TextRange replacedRange,
        TextSelection selection,
        TextRange composing)
        : base(oldText, selection, composing)
    {
        ReplacementText = replacementText;
        ReplacedRange = replacedRange;
    }

    /// <summary>The new text replacing <see cref="ReplacedRange"/> in
    /// <see cref="TextEditingDelta.OldText"/>.</summary>
    public string ReplacementText { get; }

    /// <summary>The range in <see cref="TextEditingDelta.OldText"/> that is being replaced.</summary>
    public TextRange ReplacedRange { get; }

    /// <summary>The original text that is being replaced in <see cref="TextEditingDelta.OldText"/>.
    /// </summary>
    public string TextReplaced => OldText[ReplacedRange.Start..ReplacedRange.End];

    /// <inheritdoc/>
    public override TextEditingValue Apply(TextEditingValue value)
    {
        string newText = OldText;
        RequireTextRangeIsValid(ReplacedRange, newText, "replacedRange");
        newText = Replace(newText, ReplacementText, ReplacedRange);
        RequireTextRangeIsValid(Selection.AsTextRange(), newText, "selection range");
        RequireTextRangeIsValid(Composing, newText, "composing range");
        return Compose(newText, Selection, Composing);
    }

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("oldText", OldText));
        properties.Add(new DiagnosticsProperty<string>("textReplaced", TextReplaced));
        properties.Add(new DiagnosticsProperty<string>("replacementText", ReplacementText));
        properties.Add(new DiagnosticsProperty<TextRange>("replacedRange", ReplacedRange));
        properties.Add(new DiagnosticsProperty<TextSelection>("selection", Selection));
        properties.Add(new DiagnosticsProperty<TextRange>("composing", Composing));
    }
}

/// <summary>
/// A structure representing changes to the selection and/or composing regions of an editing state
/// with no changes to the text value.
/// </summary>
public sealed class TextEditingDeltaNonTextUpdate : TextEditingDelta
{
    /// <summary>Creates a delta representing no updates to the text value.</summary>
    public TextEditingDeltaNonTextUpdate(string oldText, TextSelection selection, TextRange composing)
        : base(oldText, selection, composing)
    {
    }

    /// <inheritdoc/>
    public override TextEditingValue Apply(TextEditingValue value)
    {
        RequireTextRangeIsValid(Selection.AsTextRange(), OldText, "selection range");
        RequireTextRangeIsValid(Composing, OldText, "composing region");
        return Compose(OldText, Selection, Composing);
    }

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<string>("oldText", OldText));
        properties.Add(new DiagnosticsProperty<TextSelection>("selection", Selection));
        properties.Add(new DiagnosticsProperty<TextRange>("composing", Composing));
    }
}
