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
  bool _use24HourTime = false;
  TimeOfDay _selectedTime = const TimeOfDay(hour: 14, minute: 30);
  DateTimeRange _selectedRange = DateTimeRange(
    start: DateTime(2026, 3, 10),
    end: DateTime(2026, 3, 16),
  );
  final GlobalKey<FormState> _dateFormKey = GlobalKey<FormState>();
  String _formStatus = 'not validated';

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
            rangeSelectionBackgroundColor: const Color(0xFFCDE8DE),
          )
        : const DatePickerThemeData();
    final TimePickerThemeData timePickerTheme = _useThemeOverride
        ? TimePickerThemeData(
            dialBackgroundColor: const Color(0xFFE0F2F1),
            dialHandColor: const Color(0xFF006C4C),
            hourMinuteColor: WidgetStateColor.resolveWith(
              (Set<WidgetState> states) => states.contains(WidgetState.selected)
                  ? const Color(0xFFCDE8DE)
                  : const Color(0xFFF2F2F2),
            ),
          )
        : const TimePickerThemeData();

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
      ).copyWith(
        datePickerTheme: pickerTheme,
        timePickerTheme: timePickerTheme,
      ),
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
            const Divider(),
            const Text(
              'InputDatePickerFormField + DatePickerDialog',
              style: TextStyle(fontSize: 18),
            ),
            Form(
              key: _dateFormKey,
              child: InputDatePickerFormField(
                initialDate: _selectedDate,
                firstDate: DateTime(2022),
                lastDate: DateTime(2032, 12, 31),
                selectableDayPredicate: (DateTime date) =>
                    date.weekday != DateTime.saturday &&
                    date.weekday != DateTime.sunday,
                onDateSaved: (DateTime value) => setState(() {
                  _selectedDate = value;
                  _formStatus = 'saved: ${_formatDay(value)}';
                }),
              ),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle('Validate', _validateDateForm),
                _buildToggle('Save', _saveDateForm),
                _buildToggle('Reset', _resetDateForm),
              ],
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle(
                  'Calendar dialog',
                  () => _openDatePicker(context, DatePickerEntryMode.calendar),
                ),
                _buildToggle(
                  'Input dialog',
                  () => _openDatePicker(context, DatePickerEntryMode.input),
                ),
              ],
            ),
            Text(
              'Form/dialog status: $_formStatus',
              style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
            ),
            const Divider(),
            const Text(
              'TimePickerDialog + DateRangePickerDialog',
              style: TextStyle(fontSize: 18),
            ),
            Text(
              'Time: ${_selectedTime.hour.toString().padLeft(2, '0')}:${_selectedTime.minute.toString().padLeft(2, '0')}  |  Range: ${_formatDay(_selectedRange.start)} – ${_formatDay(_selectedRange.end)}',
              style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle(
                  'Dial time',
                  () => _openTimePicker(context, TimePickerEntryMode.dial),
                ),
                _buildToggle(
                  'Input time',
                  () => _openTimePicker(context, TimePickerEntryMode.input),
                ),
              ],
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle(
                  'Dial only',
                  () => _openTimePicker(context, TimePickerEntryMode.dialOnly),
                ),
                _buildToggle(
                  'Input only',
                  () => _openTimePicker(context, TimePickerEntryMode.inputOnly),
                ),
                _buildToggle(
                  _use24HourTime ? '24h ✓' : '24h',
                  () => setState(() => _use24HourTime = !_use24HourTime),
                ),
              ],
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildToggle(
                  'Calendar range',
                  () => _openRangePicker(context, DatePickerEntryMode.calendar),
                ),
                _buildToggle(
                  'Input range',
                  () => _openRangePicker(context, DatePickerEntryMode.input),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  void _validateDateForm() {
    final bool valid = _dateFormKey.currentState?.validate() ?? false;
    setState(() => _formStatus = valid ? 'valid' : 'invalid');
  }

  void _saveDateForm() {
    final FormState? form = _dateFormKey.currentState;
    if (form?.validate() != true) {
      setState(() => _formStatus = 'invalid');
      return;
    }
    form!.save();
  }

  void _resetDateForm() {
    _dateFormKey.currentState?.reset();
    setState(() => _formStatus = 'reset');
  }

  Future<void> _openDatePicker(
    BuildContext context,
    DatePickerEntryMode entryMode,
  ) async {
    final DateTime? result = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(2022),
      lastDate: DateTime(2032, 12, 31),
      currentDate: DateTime(2026, 3, 12),
      initialEntryMode: entryMode,
      selectableDayPredicate: (DateTime date) =>
          date.weekday != DateTime.saturday && date.weekday != DateTime.sunday,
    );
    if (!mounted) return;
    setState(() {
      if (result != null) _selectedDate = result;
      _formStatus = result == null
          ? 'dialog canceled'
          : 'dialog: ${_formatDay(result)}';
    });
  }

  Future<void> _openTimePicker(
    BuildContext context,
    TimePickerEntryMode entryMode,
  ) async {
    final TimeOfDay? result = await showTimePicker(
      context: context,
      initialTime: _selectedTime,
      initialEntryMode: entryMode,
      builder: (BuildContext dialogContext, Widget? child) => MediaQuery(
        data: MediaQuery.of(
          dialogContext,
        ).copyWith(alwaysUse24HourFormat: _use24HourTime),
        child: child!,
      ),
    );
    if (!mounted) return;
    setState(() {
      if (result != null) _selectedTime = result;
      _formStatus = result == null
          ? 'time canceled'
          : 'time: ${result.hour.toString().padLeft(2, '0')}:${result.minute.toString().padLeft(2, '0')}';
    });
  }

  Future<void> _openRangePicker(
    BuildContext context,
    DatePickerEntryMode entryMode,
  ) async {
    final DateTimeRange? result = await showDateRangePicker(
      context: context,
      initialDateRange: _selectedRange,
      firstDate: DateTime(2022),
      lastDate: DateTime(2032, 12, 31),
      currentDate: DateTime(2026, 3, 12),
      initialEntryMode: entryMode,
      selectableDayPredicate: (DateTime date, DateTime? start, DateTime? end) =>
          date.weekday != DateTime.saturday && date.weekday != DateTime.sunday,
    );
    if (!mounted) return;
    setState(() {
      if (result != null) _selectedRange = result;
      _formStatus = result == null
          ? 'range canceled'
          : 'range: ${_formatDay(result.start)} – ${_formatDay(result.end)}';
    });
  }

  Widget _buildToggle(String label, VoidCallback onPressed) => Expanded(
    child: OutlinedButton(onPressed: onPressed, child: Text(label)),
  );

  String _formatDay(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  String _formatMonth(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}';
}
