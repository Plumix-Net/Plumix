using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/date_picker_demo_page.dart

public sealed class DatePickerDemoPage : StatefulWidget
{
    public override State CreateState() => new DatePickerDemoPageState();

    private sealed class DatePickerDemoPageState : State
    {
        private DateTime _selectedDate = new(2026, 3, 12);
        private DateTime _displayedMonth = new(2026, 3, 1);
        private bool _showYearPicker;
        private bool _useMaterial3 = true;
        private bool _useThemeOverride;
        private TimeOfDay _selectedTime = new(14, 30);
        private DateTimeRange<DateTime> _selectedRange = new(new DateTime(2026, 3, 10), new DateTime(2026, 3, 16));
        private readonly LabeledGlobalKey<FormState> _dateFormKey = new("date-input-form");
        private string _formStatus = "not validated";

        public override Widget Build(BuildContext context)
        {
            var baseTheme = Theme.Of(context);
            var pickerTheme = _useThemeOverride
                ? new DatePickerThemeData(
                    DayBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? Color.Parse("#FF006C4C") : null),
                    YearBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? Color.Parse("#FF6750A4") : null),
                    TodayBorder: new BorderSide(Color.Parse("#FF006C4C"), 2),
                    RangeSelectionBackgroundColor: Color.Parse("#FFCDE8DE"))
                : new DatePickerThemeData();
            var timePickerTheme = _useThemeOverride
                ? new TimePickerThemeData(
                    DialBackgroundColor: Color.Parse("#FFE0F2F1"),
                    DialHandColor: Color.Parse("#FF006C4C"),
                    HourMinuteColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? Color.Parse("#FFCDE8DE") : Color.Parse("#FFF2F2F2")))
                : new TimePickerThemeData();

            Widget picker = _showYearPicker
                ? new SizedBox(
                    height: 360,
                    child: new YearPicker(
                        firstDate: new DateTime(2022, 1, 1),
                        lastDate: new DateTime(2032, 12, 31),
                        selectedDate: _selectedDate,
                        currentDate: new DateTime(2026, 3, 12),
                        onChanged: value => SetState(() =>
                        {
                            _selectedDate = value;
                            _displayedMonth = new DateTime(value.Year, value.Month, 1);
                        })))
                : new CalendarDatePicker(
                    initialDate: _selectedDate,
                    firstDate: new DateTime(2022, 1, 1),
                    lastDate: new DateTime(2032, 12, 31),
                    currentDate: new DateTime(2026, 3, 12),
                    selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
                    onDateChanged: value => SetState(() => _selectedDate = value),
                    onDisplayedMonthChanged: value => SetState(() => _displayedMonth = value));

            return new Theme(
                data: baseTheme with
                {
                    UseMaterial3 = _useMaterial3,
                    DatePickerTheme = pickerTheme,
                    TimePickerTheme = timePickerTheme,
                },
                child: new SingleChildScrollView(
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 12,
                        children:
                        [
                            new Text("CalendarDatePicker + YearPicker", fontSize: 20, color: Colors.Black),
                            new Text(
                                "Month paging, day/year modes, weekday predicate, M2/M3 defaults, and DatePickerTheme state overrides.",
                                fontSize: 14,
                                color: Color.Parse("#8A000000")),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildToggle(_showYearPicker ? "Calendar" : "Calendar ✓", () => SetState(() => _showYearPicker = false)),
                                    BuildToggle(_showYearPicker ? "YearPicker ✓" : "YearPicker", () => SetState(() => _showYearPicker = true)),
                                    BuildToggle(_useMaterial3 ? "M3" : "M2", () => SetState(() => _useMaterial3 = !_useMaterial3)),
                                    BuildToggle(_useThemeOverride ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverride = !_useThemeOverride)),
                                ]),
                            new Text(
                                $"Selected: {_selectedDate:yyyy-MM-dd}  |  Displayed: {_displayedMonth:yyyy-MM}",
                                fontSize: 13,
                                color: Color.Parse("#FF455A64")),
                            picker,
                            new Divider(),
                            new Text("InputDatePickerFormField + DatePickerDialog", fontSize: 18),
                            new Form(
                                key: _dateFormKey,
                                child: new InputDatePickerFormField(
                                    initialDate: _selectedDate,
                                    firstDate: new DateTime(2022, 1, 1),
                                    lastDate: new DateTime(2032, 12, 31),
                                    selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
                                    onDateSaved: value => SetState(() =>
                                    {
                                        _selectedDate = value;
                                        _formStatus = $"saved: {value:yyyy-MM-dd}";
                                    }))),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildToggle("Validate", ValidateDateForm),
                                    BuildToggle("Save", SaveDateForm),
                                    BuildToggle("Reset", ResetDateForm),
                                ]),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildToggle("Calendar dialog", () => _ = OpenDatePicker(context, DatePickerEntryMode.Calendar)),
                                    BuildToggle("Input dialog", () => _ = OpenDatePicker(context, DatePickerEntryMode.Input)),
                                ]),
                            new Text($"Form/dialog status: {_formStatus}", fontSize: 13, color: Color.Parse("#FF455A64")),
                            new Divider(),
                            new Text("TimePickerDialog + DateRangePickerDialog", fontSize: 18),
                            new Text(
                                $"Time: {_selectedTime.Hour:00}:{_selectedTime.Minute:00}  |  Range: {_selectedRange.Start:yyyy-MM-dd} – {_selectedRange.End:yyyy-MM-dd}",
                                fontSize: 13,
                                color: Color.Parse("#FF455A64")),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildToggle("Dial time", () => _ = OpenTimePicker(context, TimePickerEntryMode.Dial)),
                                    BuildToggle("Input time", () => _ = OpenTimePicker(context, TimePickerEntryMode.Input)),
                                ]),
                            new Row(
                                spacing: 8,
                                children:
                                [
                                    BuildToggle("Calendar range", () => _ = OpenRangePicker(context, DatePickerEntryMode.Calendar)),
                                    BuildToggle("Input range", () => _ = OpenRangePicker(context, DatePickerEntryMode.Input)),
                                ]),
                        ])));
        }

        private void ValidateDateForm()
        {
            var valid = _dateFormKey.CurrentState?.Validate() == true;
            SetState(() => _formStatus = valid ? "valid" : "invalid");
        }

        private void SaveDateForm()
        {
            var form = _dateFormKey.CurrentState;
            if (form?.Validate() != true)
            {
                SetState(() => _formStatus = "invalid");
                return;
            }
            form.Save();
        }

        private void ResetDateForm()
        {
            _dateFormKey.CurrentState?.Reset();
            SetState(() => _formStatus = "reset");
        }

        private async Task OpenDatePicker(BuildContext context, DatePickerEntryMode entryMode)
        {
            var result = await MaterialDatePickers.ShowDatePicker(
                context,
                initialDate: _selectedDate,
                firstDate: new DateTime(2022, 1, 1),
                lastDate: new DateTime(2032, 12, 31),
                currentDate: new DateTime(2026, 3, 12),
                initialEntryMode: entryMode,
                selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday);
            if (!Mounted) return;
            SetState(() =>
            {
                if (result.HasValue) _selectedDate = result.Value;
                _formStatus = result.HasValue ? $"dialog: {result:yyyy-MM-dd}" : "dialog canceled";
            });
        }

        private async Task OpenTimePicker(BuildContext context, TimePickerEntryMode entryMode)
        {
            var result = await MaterialTimePickers.ShowTimePicker(
                context,
                initialTime: _selectedTime,
                initialEntryMode: entryMode);
            if (!Mounted) return;
            SetState(() =>
            {
                if (result.HasValue) _selectedTime = result.Value;
                _formStatus = result.HasValue ? $"time: {result.Value.Hour:00}:{result.Value.Minute:00}" : "time canceled";
            });
        }

        private async Task OpenRangePicker(BuildContext context, DatePickerEntryMode entryMode)
        {
            var result = await MaterialDatePickers.ShowDateRangePicker(
                context,
                initialDateRange: _selectedRange,
                firstDate: new DateTime(2022, 1, 1),
                lastDate: new DateTime(2032, 12, 31),
                currentDate: new DateTime(2026, 3, 12),
                initialEntryMode: entryMode,
                selectableDayPredicate: (date, _, _) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday);
            if (!Mounted) return;
            SetState(() =>
            {
                if (result is not null) _selectedRange = result;
                _formStatus = result is null
                    ? "range canceled"
                    : $"range: {result.Start:yyyy-MM-dd} – {result.End:yyyy-MM-dd}";
            });
        }

        private static Widget BuildToggle(string label, Action onPressed) => new Expanded(
            child: new OutlinedButton(onPressed: onPressed, child: new Text(label)));
    }
}
