using System;
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

        public override Widget Build(BuildContext context)
        {
            var baseTheme = Theme.Of(context);
            var pickerTheme = _useThemeOverride
                ? new DatePickerThemeData(
                    DayBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? Color.Parse("#FF006C4C") : null),
                    YearBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                        states.HasFlag(MaterialState.Selected) ? Color.Parse("#FF6750A4") : null),
                    TodayBorder: new BorderSide(Color.Parse("#FF006C4C"), 2))
                : new DatePickerThemeData();

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
                data: baseTheme with { UseMaterial3 = _useMaterial3, DatePickerTheme = pickerTheme },
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
                        ])));
        }

        private static Widget BuildToggle(string label, Action onPressed) => new Expanded(
            child: new OutlinedButton(onPressed: onPressed, child: new Text(label)));
    }
}
