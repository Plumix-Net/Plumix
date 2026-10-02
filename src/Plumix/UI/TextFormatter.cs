using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_formatter.dart

namespace Plumix.UI;

/// <summary>Controls when text longer than a field's maximum length is truncated.</summary>
public enum MaxLengthEnforcement
{
    None,
    Enforced,
    TruncateAfterCompositionEnds,
}

/// <summary>Signature for <see cref="TextInputFormatter.WithFunction"/>.</summary>
public delegate TextEditingValue TextInputFormatFunction(TextEditingValue oldValue, TextEditingValue newValue);

/// <summary>
/// A mechanism for filtering or otherwise transforming the text as the user edits it.
/// </summary>
/// <remarks>
/// Formatters run in the order they appear in <c>EditableText.InputFormatters</c>, each one seeing
/// the value produced by the one before it.
/// </remarks>
public abstract class TextInputFormatter
{
    /// <summary>Transforms an edit from <paramref name="oldValue"/> into the value to apply.</summary>
    public abstract TextEditingValue FormatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue);

    /// <summary>Creates a formatter from a delegate (Dart's <c>TextInputFormatter.withFunction</c>).</summary>
    public static TextInputFormatter WithFunction(TextInputFormatFunction formatFunction) =>
        new SimpleTextInputFormatter(formatFunction);

    private sealed class SimpleTextInputFormatter : TextInputFormatter
    {
        private readonly TextInputFormatFunction _format;

        public SimpleTextInputFormatter(TextInputFormatFunction format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public override TextEditingValue FormatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) =>
            _format(oldValue, newValue);
    }
}

/// <summary>
/// Keeps (<see cref="Allow"/>) or removes (<c>!Allow</c>) every run of characters matching
/// <see cref="FilterPattern"/>, substituting <see cref="ReplacementString"/> for each removed run.
/// </summary>
public class FilteringTextInputFormatter : TextInputFormatter
{
    public FilteringTextInputFormatter(Regex filterPattern, bool allow, string replacementString = "")
    {
        FilterPattern = filterPattern ?? throw new ArgumentNullException(nameof(filterPattern));
        Allow = allow;
        ReplacementString = replacementString ?? string.Empty;
    }

    /// <summary>The pattern selecting the runs of characters this formatter keeps or removes.</summary>
    public Regex FilterPattern { get; }

    /// <summary>Whether matched runs are kept (<c>true</c>) or removed (<c>false</c>).</summary>
    public bool Allow { get; }

    /// <summary>The string substituted for each removed run.</summary>
    public string ReplacementString { get; }

    /// <summary>Dart's <c>FilteringTextInputFormatter.allow</c>.</summary>
    public static FilteringTextInputFormatter AllowPattern(Regex filterPattern, string replacementString = "") =>
        new(filterPattern, allow: true, replacementString);

    /// <summary>Dart's <c>FilteringTextInputFormatter.deny</c>.</summary>
    public static FilteringTextInputFormatter Deny(Regex filterPattern, string replacementString = "") =>
        new(filterPattern, allow: false, replacementString);

    /// <summary>Dart's <c>FilteringTextInputFormatter.digitsOnly</c>.</summary>
    public static FilteringTextInputFormatter DigitsOnly { get; } = AllowPattern(new Regex("[0-9]+"));

    /// <summary>Dart's <c>FilteringTextInputFormatter.singleLineFormatter</c>.</summary>
    public static FilteringTextInputFormatter SingleLineFormatter { get; } = Deny(new Regex("\n"));

    public override TextEditingValue FormatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue)
    {
        string text = newValue.Text;
        var output = new StringBuilder(text.Length);

        // Maps every source index (0..text.Length) onto the index it lands on in the output, so the
        // selection and composing region survive the rewrite the way Dart's accumulator makes them.
        int[] mapping = new int[text.Length + 1];
        int cursor = 0;
        foreach (Match match in FilterPattern.Matches(text))
        {
            AppendRun(text, cursor, match.Index, keep: !Allow, output, mapping);
            AppendRun(text, match.Index, match.Index + match.Length, keep: Allow, output, mapping);
            cursor = match.Index + match.Length;
        }

        AppendRun(text, cursor, text.Length, keep: !Allow, output, mapping);
        mapping[text.Length] = output.Length;

        string formatted = output.ToString();
        if (string.Equals(formatted, text, StringComparison.Ordinal)) return newValue;

        var selection = new TextSelection(
            BaseOffset: mapping[Math.Clamp(newValue.Selection.BaseOffset, 0, text.Length)],
            ExtentOffset: mapping[Math.Clamp(newValue.Selection.ExtentOffset, 0, text.Length)]);
        TextRange? composing = null;
        if (newValue.Composing is { } range)
        {
            composing = new TextRange(
                Start: mapping[Math.Clamp(range.Start, 0, text.Length)],
                End: mapping[Math.Clamp(range.End, 0, text.Length)]);
        }

        return new TextEditingValue(formatted, selection, composing);
    }

    private void AppendRun(string text, int start, int end, bool keep, StringBuilder output, int[] mapping)
    {
        if (end <= start) return;
        if (keep)
        {
            for (int i = start; i < end; i++)
            {
                mapping[i] = output.Length;
                output.Append(text[i]);
            }

            return;
        }

        int replacementStart = output.Length;
        output.Append(ReplacementString);
        for (int i = start; i < end; i++) mapping[i] = replacementStart;
    }
}

/// <summary>Truncates the edited text so it never exceeds <see cref="MaxLength"/> characters.</summary>
public class LengthLimitingTextInputFormatter : TextInputFormatter
{
    /// <summary>Dart's <c>LengthLimitingTextInputFormatter.maxLengthEnforced</c> sentinel.</summary>
    public const int NoMaxLength = -1;

    public LengthLimitingTextInputFormatter(
        int? maxLength,
        MaxLengthEnforcement? maxLengthEnforcement = null)
    {
        if (maxLength.HasValue && maxLength.Value != NoMaxLength && maxLength.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        MaxLength = maxLength;
        MaxLengthEnforcement = maxLengthEnforcement ?? GetDefaultMaxLengthEnforcement();
    }

    public int? MaxLength { get; }

    public MaxLengthEnforcement MaxLengthEnforcement { get; }

    public static MaxLengthEnforcement GetDefaultMaxLengthEnforcement()
    {
        if (OperatingSystem.IsBrowser())
        {
            return MaxLengthEnforcement.TruncateAfterCompositionEnds;
        }

        return PlatformDefaults.TargetPlatform is TargetPlatform.Android or TargetPlatform.Windows
            ? MaxLengthEnforcement.Enforced
            : MaxLengthEnforcement.TruncateAfterCompositionEnds;
    }

    /// <summary>Truncates <paramref name="value"/> to <paramref name="maxLength"/> user-perceived
    /// characters. Dart parity source: <c>LengthLimitingTextInputFormatter.truncate</c>.</summary>
    public static TextEditingValue Truncate(TextEditingValue value, int maxLength)
    {
        int[] elements = StringInfo.ParseCombiningCharacters(value.Text);
        string truncated = elements.Length > maxLength ? value.Text[..elements[maxLength]] : value.Text;
        return new TextEditingValue(
            text: truncated,
            selection: value.Selection with
            {
                BaseOffset = Math.Min(value.Selection.Start, truncated.Length),
                ExtentOffset = Math.Min(value.Selection.End, truncated.Length),
            },
            composing: value.Composing is { IsCollapsed: false } composing && truncated.Length > composing.Start
                ? new TextRange(composing.Start, Math.Min(composing.End, truncated.Length))
                : null);
    }

    /// <summary>Dart parity source: <c>LengthLimitingTextInputFormatter.formatEditUpdate</c>.</summary>
    public override TextEditingValue FormatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue)
    {
        if (MaxLength is not { } maxLength
            || maxLength == NoMaxLength
            || CharactersLength(newValue.Text) <= maxLength)
        {
            return newValue;
        }

        switch (MaxLengthEnforcement)
        {
            case MaxLengthEnforcement.None:
                return newValue;
            case MaxLengthEnforcement.Enforced:
                // If already at the maximum and tried to enter even more, and has no selection, keep
                // the old value.
                if (CharactersLength(oldValue.Text) == maxLength && oldValue.Selection.IsCollapsed)
                {
                    return oldValue;
                }

                // Enforced to return a truncated value.
                return Truncate(newValue, maxLength);
            default:
                // If already at the maximum and tried to enter even more, and the old value is not
                // composing, keep the old value.
                if (CharactersLength(oldValue.Text) == maxLength && oldValue.Composing is not { IsValid: true })
                {
                    return oldValue;
                }

                // Temporarily exempt `newValue` from the maxLength limit if it has a composing text
                // going and no enforcement to the composing value, until the composing is finished.
                if (newValue.Composing is { IsValid: true })
                {
                    return newValue;
                }

                return Truncate(newValue, maxLength);
        }
    }

    /// Dart's `text.characters.length`: the number of user-perceived characters.
    private static int CharactersLength(string text) => StringInfo.ParseCombiningCharacters(text).Length;
}
