import 'package:flutter/material.dart';

class DatePickerDemoPage extends StatefulWidget {
  const DatePickerDemoPage({super.key});

  @override
  State<DatePickerDemoPage> createState() => _DatePickerDemoPageState();
}

class _DatePickerDemoPageState extends State<DatePickerDemoPage> {
  DateTime _selectedDate = DateTime(2026, 3, 12);
  DateTime _displayedMonth = DateTime(2026, 3);
  bool _showYearPicker = false;
  bool _useMaterial3 = true;
  bool _useThemeOverride = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final DatePickerThemeData pickerTheme = _useThemeOverride
        ? DatePickerThemeData(
            dayBackgroundColor: WidgetStateProperty.resolveWith<Color?>(
              (Set<WidgetState> states) => states.contains(WidgetState.selected)
                  ? const Color(0xFF006C4C)
                  : null,
            ),
            yearBackgroundColor: WidgetStateProperty.resolveWith<Color?>(
              (Set<WidgetState> states) => states.contains(WidgetState.selected)
                  ? const Color(0xFF6750A4)
                  : null,
            ),
            todayBorder: const BorderSide(color: Color(0xFF006C4C), width: 2),
          )
        : const DatePickerThemeData();

    final Widget picker = _showYearPicker
        ? SizedBox(
            height: 360,
            child: YearPicker(
              firstDate: DateTime(2022),
              lastDate: DateTime(2032, 12, 31),
              selectedDate: _selectedDate,
              currentDate: DateTime(2026, 3, 12),
              onChanged: (DateTime value) => setState(() {
                _selectedDate = value;
                _displayedMonth = DateTime(value.year, value.month);
              }),
            ),
          )
        : CalendarDatePicker(
            initialDate: _selectedDate,
            firstDate: DateTime(2022),
            lastDate: DateTime(2032, 12, 31),
            currentDate: DateTime(2026, 3, 12),
            selectableDayPredicate: (DateTime date) =>
                date.weekday != DateTime.saturday &&
                date.weekday != DateTime.sunday,
            onDateChanged: (DateTime value) =>
                setState(() => _selectedDate = value),
            onDisplayedMonthChanged: (DateTime value) =>
                setState(() => _displayedMonth = value),
          );

    return Theme(
      data: ThemeData.from(
        colorScheme: baseTheme.colorScheme,
        textTheme: baseTheme.textTheme,
        useMaterial3: _useMaterial3,
      ).copyWith(datePickerTheme: pickerTheme),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            const Text(
              'CalendarDatePicker + YearPicker',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Month paging, day/year modes, weekday predicate, M2/M3 defaults, and DatePickerTheme state overrides.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle(
                  _showYearPicker ? 'Calendar' : 'Calendar ✓',
                  () => setState(() => _showYearPicker = false),
                ),
                _buildToggle(
                  _showYearPicker ? 'YearPicker ✓' : 'YearPicker',
                  () => setState(() => _showYearPicker = true),
                ),
                _buildToggle(
                  _useMaterial3 ? 'M3' : 'M2',
                  () => setState(() => _useMaterial3 = !_useMaterial3),
                ),
                _buildToggle(
                  _useThemeOverride ? 'Theme on' : 'Theme off',
                  () => setState(() => _useThemeOverride = !_useThemeOverride),
                ),
              ],
            ),
            Text(
              'Selected: ${_formatDay(_selectedDate)}  |  Displayed: ${_formatMonth(_displayedMonth)}',
              style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
            ),
            picker,
          ],
        ),
      ),
    );
  }

  Widget _buildToggle(String label, VoidCallback onPressed) => Expanded(
    child: OutlinedButton(onPressed: onPressed, child: Text(label)),
  );

  String _formatDay(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  String _formatMonth(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}';
}
