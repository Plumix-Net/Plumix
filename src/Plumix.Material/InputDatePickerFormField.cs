using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/input_date_picker_form_field.dart

public sealed class InputDatePickerFormField : StatefulWidget
{
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
        bool autofocus = false,
        bool acceptEmptyDate = false,
        FocusNode? focusNode = null,
        CalendarDelegate<DateTime>? calendarDelegate = null,
        Key? key = null) : base(key)
    {
        CalendarDelegate = calendarDelegate ?? GregorianCalendarDelegate.Instance;
        InitialDate = initialDate.HasValue ? CalendarDelegate.DateOnly(initialDate.Value) : null;
        FirstDate = CalendarDelegate.DateOnly(firstDate);
        LastDate = CalendarDelegate.DateOnly(lastDate);
        CalendarDatePicker.ValidateDates(InitialDate, FirstDate, LastDate, selectableDayPredicate);
        OnDateSubmitted = onDateSubmitted;
        OnDateSaved = onDateSaved;
        SelectableDayPredicate = selectableDayPredicate;
        ErrorFormatText = errorFormatText;
        ErrorInvalidText = errorInvalidText;
        FieldHintText = fieldHintText;
        FieldLabelText = fieldLabelText;
        Autofocus = autofocus;
        AcceptEmptyDate = acceptEmptyDate;
        FocusNode = focusNode;
    }

    public DateTime? InitialDate { get; }
    public DateTime FirstDate { get; }
    public DateTime LastDate { get; }
    public Action<DateTime>? OnDateSubmitted { get; }
    public Action<DateTime>? OnDateSaved { get; }
    public SelectableDayPredicate? SelectableDayPredicate { get; }
    public string? ErrorFormatText { get; }
    public string? ErrorInvalidText { get; }
    public string? FieldHintText { get; }
    public string? FieldLabelText { get; }
    public bool Autofocus { get; }
    public bool AcceptEmptyDate { get; }
    public FocusNode? FocusNode { get; }
    public CalendarDelegate<DateTime> CalendarDelegate { get; }

    public override State CreateState() => new InputDatePickerFormFieldState();
}

internal sealed class InputDatePickerFormFieldState : State
{
    private readonly TextEditingController _controller = new();
    private DateTime? _selectedDate;
    private string _inputText = string.Empty;
    private bool _autoSelected;

    private InputDatePickerFormField Current => (InputDatePickerFormField)StateWidget;

    public override void InitState()
    {
        _selectedDate = Current.InitialDate;
    }

    public override void DidChangeDependencies() => UpdateValueForSelectedDate();

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (InputDatePickerFormField)oldWidget;
        if (Nullable.Equals(old.InitialDate, Current.InitialDate)) return;
        Scheduler.AddPostFrameCallback(_ =>
        {
            if (!Mounted) return;
            SetState(() =>
            {
                _selectedDate = Current.InitialDate;
                UpdateValueForSelectedDate();
            });
        });
    }

    public override void Dispose() => _controller.Dispose();

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var localizations = MaterialLocalizations.Of(context);
        var datePickerTheme = DatePickerTheme.Of(context);
        var inputTheme = InputDecorationTheme.Of(context);
        var dateInputTheme = datePickerTheme.InputDecorationTheme ?? new InputDecorationThemeData();
        var effectiveBorder = dateInputTheme.Border
                              ?? inputTheme.Border
                              ?? (theme.UseMaterial3 ? new OutlineInputBorder() : new UnderlineInputBorder());
        var decoration = new InputDecoration(
                hintText: Current.FieldHintText ?? Current.CalendarDelegate.DateHelpText(localizations),
                labelText: Current.FieldLabelText ?? localizations.DateInputLabel,
                border: effectiveBorder)
            .ApplyDefaults(dateInputTheme)
            .ApplyDefaults(inputTheme);

        return new Semantics(
            container: true,
            child: new TextFormField(
                decoration: decoration,
                validator: ValidateDate,
                onSaved: HandleSaved,
                onFieldSubmitted: HandleSubmitted,
                autofocus: Current.Autofocus,
                controller: _controller,
                focusNode: Current.FocusNode));
    }

    private void UpdateValueForSelectedDate()
    {
        if (_selectedDate is { } selected)
        {
            _inputText = Current.CalendarDelegate.FormatCompactDate(
                selected, MaterialLocalizations.Of(Context));
            _controller.Text = _inputText;
            if (Current.Autofocus && !_autoSelected)
            {
                _controller.Selection = new TextSelection(0, _inputText.Length);
                _autoSelected = true;
            }
        }
        else
        {
            _inputText = string.Empty;
            _controller.Text = string.Empty;
        }
    }

    private DateTime? ParseDate(string? text) => Current.CalendarDelegate.ParseCompactDate(
        text, MaterialLocalizations.Of(Context));

    private bool IsValidAcceptableDate(DateTime? date) => date is { } candidate
        && candidate >= Current.FirstDate
        && candidate <= Current.LastDate
        && (Current.SelectableDayPredicate is null || Current.SelectableDayPredicate(candidate));

    private string? ValidateDate(string? text)
    {
        if (string.IsNullOrEmpty(text) && Current.AcceptEmptyDate) return null;
        var date = ParseDate(text);
        if (date is null)
            return Current.ErrorFormatText ?? MaterialLocalizations.Of(Context).InvalidDateFormatLabel;
        if (!IsValidAcceptableDate(date))
            return Current.ErrorInvalidText ?? MaterialLocalizations.Of(Context).DateOutOfRangeLabel;
        return null;
    }

    private void UpdateDate(string? text, Action<DateTime>? callback)
    {
        var date = ParseDate(text);
        if (!IsValidAcceptableDate(date)) return;
        _selectedDate = date;
        _inputText = text ?? string.Empty;
        callback?.Invoke(date!.Value);
    }

    private void HandleSaved(string? text) => UpdateDate(text, Current.OnDateSaved);

    private void HandleSubmitted(string text) => UpdateDate(text, Current.OnDateSubmitted);
}
