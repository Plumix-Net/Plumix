using System.Globalization;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/input_decorator.dart (InputDecoration.toString).

public sealed partial record InputDecoration
{
    public override string ToString()
    {
        var description = new List<string>();
        if (Icon != null)
        {
            description.Add($"icon: {FormatDecorationValue(Icon)}");
        }

        if (IconColor != null)
        {
            description.Add($"iconColor: {FormatDecorationValue(IconColor)}");
        }

        if (Label != null)
        {
            description.Add($"label: {FormatDecorationValue(Label)}");
        }

        if (LabelText != null)
        {
            description.Add($"labelText: \"{FormatDecorationValue(LabelText)}\"");
        }

        if (FloatingLabelStyle != null)
        {
            description.Add($"floatingLabelStyle: \"{FormatDecorationValue(FloatingLabelStyle)}\"");
        }

        if (Helper != null)
        {
            description.Add($"helper: \"{FormatDecorationValue(Helper)}\"");
        }

        if (HelperText != null)
        {
            description.Add($"helperText: \"{FormatDecorationValue(HelperText)}\"");
        }

        if (HelperMaxLines != null)
        {
            description.Add($"helperMaxLines: \"{FormatDecorationValue(HelperMaxLines)}\"");
        }

        if (HintText != null)
        {
            description.Add($"hintText: \"{FormatDecorationValue(HintText)}\"");
        }

        if (Hint != null)
        {
            description.Add($"hint: {FormatDecorationValue(Hint)}");
        }

        if (HintMaxLines != null)
        {
            description.Add($"hintMaxLines: \"{FormatDecorationValue(HintMaxLines)}\"");
        }

        if (HintFadeDuration != null)
        {
            description.Add($"hintFadeDuration: \"{FormatDecorationValue(HintFadeDuration)}\"");
        }

        if (!_maintainHintHeight)
        {
            description.Add("maintainHintHeight: false");
        }

        if (!MaintainHintSize)
        {
            description.Add("maintainHintSize: false");
        }

        if (MaintainLabelSize)
        {
            description.Add("maintainLabelSize: true");
        }

        if (Error != null)
        {
            description.Add($"error: \"{FormatDecorationValue(Error)}\"");
        }

        if (ErrorText != null)
        {
            description.Add($"errorText: \"{FormatDecorationValue(ErrorText)}\"");
        }

        if (ErrorStyle != null)
        {
            description.Add($"errorStyle: \"{FormatDecorationValue(ErrorStyle)}\"");
        }

        if (ErrorMaxLines != null)
        {
            description.Add($"errorMaxLines: \"{FormatDecorationValue(ErrorMaxLines)}\"");
        }

        if (FloatingLabelBehavior != null)
        {
            description.Add($"floatingLabelBehavior: {FormatDecorationValue(FloatingLabelBehavior)}");
        }

        if (FloatingLabelAlignment != null)
        {
            description.Add($"floatingLabelAlignment: {FormatDecorationValue(FloatingLabelAlignment)}");
        }

        if (IsDense ?? false)
        {
            description.Add($"isDense: {FormatDecorationValue(IsDense)}");
        }

        if (ContentPadding != null)
        {
            description.Add($"contentPadding: {FormatDecorationValue(ContentPadding)}");
        }

        if (IsCollapsed ?? false)
        {
            description.Add($"isCollapsed: {FormatDecorationValue(IsCollapsed)}");
        }

        if (PrefixIcon != null)
        {
            description.Add($"prefixIcon: {FormatDecorationValue(PrefixIcon)}");
        }

        if (PrefixIconColor != null)
        {
            description.Add($"prefixIconColor: {FormatDecorationValue(PrefixIconColor)}");
        }

        if (Prefix != null)
        {
            description.Add($"prefix: {FormatDecorationValue(Prefix)}");
        }

        if (PrefixText != null)
        {
            description.Add($"prefixText: {FormatDecorationValue(PrefixText)}");
        }

        if (PrefixStyle != null)
        {
            description.Add($"prefixStyle: {FormatDecorationValue(PrefixStyle)}");
        }

        if (PrefixIconConstraints != null)
        {
            description.Add($"prefixIconConstraints: {FormatDecorationValue(PrefixIconConstraints)}");
        }

        if (SuffixIcon != null)
        {
            description.Add($"suffixIcon: {FormatDecorationValue(SuffixIcon)}");
        }

        if (SuffixIconColor != null)
        {
            description.Add($"suffixIconColor: {FormatDecorationValue(SuffixIconColor)}");
        }

        if (Suffix != null)
        {
            description.Add($"suffix: {FormatDecorationValue(Suffix)}");
        }

        if (SuffixText != null)
        {
            description.Add($"suffixText: {FormatDecorationValue(SuffixText)}");
        }

        if (SuffixStyle != null)
        {
            description.Add($"suffixStyle: {FormatDecorationValue(SuffixStyle)}");
        }

        if (SuffixIconConstraints != null)
        {
            description.Add($"suffixIconConstraints: {FormatDecorationValue(SuffixIconConstraints)}");
        }

        if (Counter != null)
        {
            description.Add($"counter: {FormatDecorationValue(Counter)}");
        }

        if (CounterText != null)
        {
            description.Add($"counterText: {FormatDecorationValue(CounterText)}");
        }

        if (CounterStyle != null)
        {
            description.Add($"counterStyle: {FormatDecorationValue(CounterStyle)}");
        }

        if (Filled ?? false)
        {
            description.Add("filled: true");
        }

        if (FillColor != null)
        {
            description.Add($"fillColor: {FormatDecorationValue(FillColor)}");
        }

        if (FocusColor != null)
        {
            description.Add($"focusColor: {FormatDecorationValue(FocusColor)}");
        }

        if (HoverColor != null)
        {
            description.Add($"hoverColor: {FormatDecorationValue(HoverColor)}");
        }

        if (ErrorBorder != null)
        {
            description.Add($"errorBorder: {FormatDecorationValue(ErrorBorder)}");
        }

        if (FocusedBorder != null)
        {
            description.Add($"focusedBorder: {FormatDecorationValue(FocusedBorder)}");
        }

        if (FocusedErrorBorder != null)
        {
            description.Add($"focusedErrorBorder: {FormatDecorationValue(FocusedErrorBorder)}");
        }

        if (DisabledBorder != null)
        {
            description.Add($"disabledBorder: {FormatDecorationValue(DisabledBorder)}");
        }

        if (EnabledBorder != null)
        {
            description.Add($"enabledBorder: {FormatDecorationValue(EnabledBorder)}");
        }

        if (Border != null)
        {
            description.Add($"border: {FormatDecorationValue(Border)}");
        }

        if (!Enabled)
        {
            description.Add("enabled: false");
        }

        if (SemanticCounterText != null)
        {
            description.Add($"semanticCounterText: {FormatDecorationValue(SemanticCounterText)}");
        }

        if (AlignLabelWithHint != null)
        {
            description.Add($"alignLabelWithHint: {FormatDecorationValue(AlignLabelWithHint)}");
        }

        if (Constraints != null)
        {
            description.Add($"constraints: {FormatDecorationValue(Constraints)}");
        }

        if (VisualDensity != null)
        {
            description.Add($"visualDensity: {FormatDecorationValue(VisualDensity)}");
        }

        return $"InputDecoration({string.Join(", ", description)})";
    }

    private static string FormatDecorationValue(object? value) => value switch
    {
        null => "null",
        bool flag => flag ? "true" : "false",
        FloatingLabelBehavior behavior => $"FloatingLabelBehavior.{behavior.ToString().ToLowerInvariant()}",
        FloatingLabelAlignment alignment => $"FloatingLabelAlignment.{alignment.ToString().ToLowerInvariant()}",
        TimeSpan duration => FormatDuration(duration),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static string FormatDuration(TimeSpan duration)
    {
        long microseconds = Math.Abs(duration.Ticks / 10);
        long seconds = microseconds / 1_000_000;
        string sign = duration.Ticks < 0 ? "-" : string.Empty;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{sign}{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}.{microseconds % 1_000_000:000000}");
    }
}
