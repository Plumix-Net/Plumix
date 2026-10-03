using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/input_date_picker_form_field.dart

/// <summary>
/// A <see cref="TextFormField"/> configured to accept and validate a date entered by a user.
/// </summary>
/// <remarks>
/// When the field is saved or submitted, the text will be parsed into a <see cref="DateTime"/>
/// according to the ambient locale's compact date format. If the input text doesn't parse into a
/// date, the <see cref="ErrorFormatText"/> message will be displayed under the field.
/// <para>
/// <see cref="FirstDate"/>, <see cref="LastDate"/>, and <see cref="SelectableDayPredicate"/> provide
/// constraints on what days are valid. If the input date isn't in the date range or doesn't pass
/// the given predicate, then the <see cref="ErrorInvalidText"/> message will be displayed under the
/// field.
/// </para>
/// </remarks>
public class InputDatePickerFormField : StatefulWidget
{
    /// <summary>
    /// Creates a <see cref="TextFormField"/> configured to accept and validate a date.
    /// </summary>
    /// <remarks>
    /// If the optional <paramref name="initialDate"/> is provided, then it will be used to populate
    /// the text field. If the <paramref name="fieldHintText"/> is provided, it will be shown.
    /// <para>
    /// If <paramref name="initialDate"/> is provided, it must not be before
    /// <paramref name="firstDate"/> or after <paramref name="lastDate"/>. If
    /// <paramref name="selectableDayPredicate"/> is provided, it must return <c>true</c> for
    /// <paramref name="initialDate"/>. <paramref name="firstDate"/> must be on or before
    /// <paramref name="lastDate"/>.
    /// </para>
    /// </remarks>
    public InputDatePickerFormField(
        DateTime firstDate,
        DateTime lastDate,
        DateTime? initialDate = null,
        Action<DateTime>? onDateSubmitted = null,
        Action<DateTime>? onDateSaved = null,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        TextInputType? keyboardType = null,
        bool autofocus = false,
        bool acceptEmptyDate = false,
        FocusNode? focusNode = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        InitialDate = initialDate != null ? CalendarDelegate.DateOnly(initialDate.Value) : null;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        OnDateSubmitted = onDateSubmitted;
        OnDateSaved = onDateSaved;
        SelectableDayPredicate = selectableDayPredicate;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        FieldHintText = fieldHintText;
        FieldLabelText = fieldLabelText;
        KeyboardType = keyboardType;
        Autofocus = autofocus;
        AcceptEmptyDate = acceptEmptyDate;
        FocusNode = focusNode;

        DebugAssertions.Assert(
            !(LastDate < FirstDate),
            $"lastDate {DateUtils.DartToString(LastDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            initialDate == null || !(InitialDate!.Value < FirstDate),
            $"initialDate {Describe(InitialDate)} must be on or after firstDate "
            + $"{DateUtils.DartToString(FirstDate)}.");
        DebugAssertions.Assert(
            initialDate == null || !(InitialDate!.Value > LastDate),
            $"initialDate {Describe(InitialDate)} must be on or before lastDate "
            + $"{DateUtils.DartToString(LastDate)}.");
        DebugAssertions.Assert(
            SelectableDayPredicate == null
            || initialDate == null
            || SelectableDayPredicate(InitialDate!.Value),
            $"Provided initialDate {Describe(InitialDate)} must satisfy provided selectableDayPredicate.");
    }

    /// <summary>
    /// If provided, it will be used as the default value of the field.
    /// </summary>
    public DateTime? InitialDate { get; }

    /// <summary>The earliest allowable <see cref="DateTime"/> that the user can input.</summary>
    public DateTime FirstDate { get; }

    /// <summary>The latest allowable <see cref="DateTime"/> that the user can input.</summary>
    public DateTime LastDate { get; }

    /// <summary>
    /// An optional method to call when the user indicates they are done editing the text in the
    /// field. Will only be called if the input represents a valid <see cref="DateTime"/>.
    /// </summary>
    public Action<DateTime>? OnDateSubmitted { get; }

    /// <summary>
    /// An optional method to call with the final date when the form is saved via
    /// <see cref="FormState.Save"/>. Will only be called if the input represents a valid
    /// <see cref="DateTime"/>.
    /// </summary>
    public Action<DateTime>? OnDateSaved { get; }

    /// <summary>Function to provide full control over which <see cref="DateTime"/> can be selected.</summary>
    public SelectableDayPredicate? SelectableDayPredicate { get; }

    /// <summary>
    /// The error text displayed if the entered date is not in the correct format.
    /// </summary>
    public string? ErrorFormatText { get; }

    /// <summary>
    /// The error text displayed if the date is not valid.
    /// </summary>
    /// <remarks>
    /// A date is not valid if it is earlier than <see cref="FirstDate"/>, later than
    /// <see cref="LastDate"/>, or doesn't pass the <see cref="SelectableDayPredicate"/>.
    /// </remarks>
    public string? ErrorInvalidText { get; }

    /// <summary>
    /// The hint text displayed in the <see cref="TextField"/>.
    /// </summary>
    /// <remarks>
    /// If this is null, it will default to the date format string. For example, 'mm/dd/yyyy' for
    /// en_US.
    /// </remarks>
    public string? FieldHintText { get; }

    /// <summary>
    /// The label text displayed in the <see cref="TextField"/>.
    /// </summary>
    /// <remarks>If this is null, it will default to the words representing the date format string.
    /// For example, 'Month, Day, Year' for en_US.</remarks>
    public string? FieldLabelText { get; }

    /// <summary>
    /// The keyboard type of the <see cref="TextField"/>.
    /// </summary>
    /// <remarks>If this is null, it will default to <see cref="TextInputType.Datetime"/>.</remarks>
    public TextInputType? KeyboardType { get; }

    /// <summary>Whether the field should be focused when it is first displayed.</summary>
    public bool Autofocus { get; }

    /// <summary>Determines if an empty date would show a <see cref="ErrorFormatText"/> or not.</summary>
    /// <remarks>
    /// Defaults to false. If true, <see cref="ErrorFormatText"/> is not shown when the date input
    /// field is empty.
    /// </remarks>
    public bool AcceptEmptyDate { get; }

    /// <summary>The focus node used by the text field.</summary>
    public FocusNode? FocusNode { get; }

    /// <summary>The calendar system the field interprets and formats dates with.</summary>
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new InputDatePickerFormFieldState();

    // Dart interpolates the nullable `this.initialDate`, i.e. `DateTime.toString` or `null`.
    private static string Describe(DateTime? date) => date is { } value ? DateUtils.DartToString(value) : "null";
}

/// <summary>Dart's private <c>_InputDatePickerFormFieldState</c>.</summary>
internal sealed class InputDatePickerFormFieldState : State<InputDatePickerFormField>
{
    private readonly TextEditingController _controller = new();
    private DateTime? _selectedDate;
    private string? _inputText;
    private bool _autoSelected;

    public override void InitState()
    {
        base.InitState();
        _selectedDate = Widget.InitialDate;
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        UpdateValueForSelectedDate();
    }

    public override void DidUpdateWidget(InputDatePickerFormField oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (Widget.InitialDate != oldWidget.InitialDate)
        {
            // Can't update the form field in the middle of a build, so do it next frame
            Scheduler.AddPostFrameCallback(
                _ =>
                {
                    SetState(() =>
                    {
                        _selectedDate = Widget.InitialDate;
                        UpdateValueForSelectedDate();
                    });
                },
                debugLabel: "InputDatePickerFormField.update");
        }
    }

    private void UpdateValueForSelectedDate()
    {
        if (_selectedDate != null)
        {
            MaterialLocalizations localizations = MaterialLocalizations.Of(Context);
            _inputText = Widget.CalendarDelegate.FormatCompactDate(_selectedDate.Value, localizations);
            var textEditingValue = new TextEditingValue(text: _inputText);
            // Select the new text if we are auto focused and haven't selected the text before.
            if (Widget.Autofocus && !_autoSelected)
            {
                textEditingValue = textEditingValue.CopyWith(
                    selection: new TextSelection(BaseOffset: 0, ExtentOffset: _inputText.Length));
                _autoSelected = true;
            }

            _controller.Value = textEditingValue;
        }
        else
        {
            _inputText = string.Empty;
            _controller.Value = new TextEditingValue(text: _inputText);
        }
    }

    private DateTime? ParseDate(string? text)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(Context);
        return Widget.CalendarDelegate.ParseCompactDate(text, localizations);
    }

    private bool IsValidAcceptableDate(DateTime? date)
    {
        return date != null
            && !(date.Value < Widget.FirstDate)
            && !(date.Value > Widget.LastDate)
            && (Widget.SelectableDayPredicate == null || Widget.SelectableDayPredicate(date.Value));
    }

    private string? ValidateDate(string? text)
    {
        if ((text == null || text.Length == 0) && Widget.AcceptEmptyDate)
        {
            return null;
        }

        DateTime? date = ParseDate(text);
        if (date == null)
        {
            return Widget.ErrorFormatText ?? MaterialLocalizations.Of(Context).InvalidDateFormatLabel;
        }
        else if (!IsValidAcceptableDate(date))
        {
            return Widget.ErrorInvalidText ?? MaterialLocalizations.Of(Context).DateOutOfRangeLabel;
        }

        return null;
    }

    private void UpdateDate(string? text, Action<DateTime>? callback)
    {
        DateTime? date = ParseDate(text);
        if (IsValidAcceptableDate(date))
        {
            _selectedDate = date;
            _inputText = text;
            callback?.Invoke(_selectedDate!.Value);
        }
    }

    private void HandleSaved(string? text)
    {
        UpdateDate(text, Widget.OnDateSaved);
    }

    private void HandleSubmitted(string text)
    {
        UpdateDate(text, Widget.OnDateSubmitted);
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        bool useMaterial3 = theme.UseMaterial3;
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        DatePickerThemeData datePickerTheme = theme.DatePickerTheme;
        InputDecorationThemeData inputTheme = InputDecorationTheme.Of(context);
        InputBorder effectiveInputBorder = datePickerTheme.InputDecorationTheme?.Border
            ?? inputTheme.Border
            ?? (useMaterial3 ? new OutlineInputBorder() : new UnderlineInputBorder());

        return new Semantics(
            container: true,
            child: new TextFormField(
                decoration: new InputDecoration(
                        hintText: Widget.FieldHintText ?? Widget.CalendarDelegate.DateHelpText(localizations),
                        labelText: Widget.FieldLabelText ?? localizations.DateInputLabel)
                    .ApplyDefaults(
                        inputTheme
                            .Merge(datePickerTheme.InputDecorationTheme)
                            .CopyWith(border: effectiveInputBorder)),
                validator: ValidateDate,
                keyboardType: Widget.KeyboardType ?? TextInputType.Datetime,
                onSaved: HandleSaved,
                onFieldSubmitted: HandleSubmitted,
                autofocus: Widget.Autofocus,
                controller: _controller,
                focusNode: Widget.FocusNode));
    }
}
