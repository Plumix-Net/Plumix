import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoPickerDemoPage extends StatefulWidget {
  const CupertinoPickerDemoPage({super.key});

  @override
  State<CupertinoPickerDemoPage> createState() =>
      _CupertinoPickerDemoPageState();
}

class _CupertinoPickerDemoPageState extends State<CupertinoPickerDemoPage> {
  static const List<String> _fruits = <String>[
    'Apple',
    'Banana',
    'Cherry',
    'Pear',
  ];
  static const List<String> _sizes = <String>[
    'Small',
    'Medium',
    'Large',
    'Extra large',
  ];

  final FixedExtentScrollController _fruitController =
      FixedExtentScrollController(initialItem: 1);
  final FixedExtentScrollController _sizeController =
      FixedExtentScrollController(initialItem: 1);
  int _demoIndex = 0;
  int _fruitIndex = 1;
  int _sizeIndex = 1;
  DateTime _selectedDateTime = DateTime(2025, 6, 16, 10, 30);
  Duration _selectedDuration = const Duration(
    hours: 1,
    minutes: 20,
    seconds: 30,
  );

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino picker',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Compare the base wheel, bounded date/time, and duration picker '
          'APIs.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(child: _buildModeButton('Wheel', 0)),
            Expanded(child: _buildModeButton('Date + time', 1)),
            Expanded(child: _buildModeButton('Timer', 2)),
          ],
        ),
        Text(
          _buildSummary(),
          style: const TextStyle(fontSize: 13, color: Colors.blueGrey),
        ),
        _buildActivePicker(),
      ],
    );
  }

  @override
  void dispose() {
    _fruitController.dispose();
    _sizeController.dispose();
    super.dispose();
  }

  Widget _buildWheelColumn(String label, Widget picker) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 6,
      children: <Widget>[
        Text(
          label,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 13, color: Color(0xFF37474F)),
        ),
        SizedBox(height: 180, child: picker),
      ],
    );
  }

  Widget _buildModeButton(String label, int index) {
    return CupertinoButton(
      color: _demoIndex == index
          ? CupertinoColors.activeBlue
          : CupertinoColors.systemGrey5,
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      onPressed: () => setState(() => _demoIndex = index),
      child: Text(label, textAlign: TextAlign.center),
    );
  }

  Widget _buildActivePicker() {
    return switch (_demoIndex) {
      1 => SizedBox(
        height: 216,
        child: CupertinoDatePicker(
          initialDateTime: _selectedDateTime,
          minimumDate: DateTime(2025, 6, 13, 8),
          maximumDate: DateTime(2025, 6, 20, 18),
          minuteInterval: 5,
          showTimeSeparator: true,
          selectableDayPredicate: (DateTime date) =>
              date.weekday != DateTime.saturday &&
              date.weekday != DateTime.sunday,
          onDateTimeChanged: _selectDateTime,
        ),
      ),
      2 => SizedBox(
        height: 216,
        child: CupertinoTimerPicker(
          initialTimerDuration: _selectedDuration,
          minuteInterval: 5,
          secondInterval: 10,
          onTimerDurationChanged: _selectDuration,
        ),
      ),
      _ => Row(
        spacing: 16,
        children: <Widget>[
          Expanded(
            child: _buildWheelColumn(
              'Looping list',
              CupertinoPicker(
                itemExtent: 40,
                onSelectedItemChanged: _selectFruit,
                scrollController: _fruitController,
                looping: true,
                children: _fruits
                    .map(
                      (String fruit) =>
                          Text(fruit, textAlign: TextAlign.center),
                    )
                    .toList(),
              ),
            ),
          ),
          Expanded(
            child: _buildWheelColumn(
              'Builder + magnifier',
              CupertinoPicker.builder(
                itemExtent: 40,
                onSelectedItemChanged: _selectSize,
                itemBuilder: (BuildContext context, int index) {
                  return Text(_sizes[index], textAlign: TextAlign.center);
                },
                selectionOverlay: const CupertinoPickerDefaultSelectionOverlay(
                  capStartEdge: false,
                ),
                childCount: _sizes.length,
                useMagnifier: true,
                magnification: 1.12,
                scrollController: _sizeController,
              ),
            ),
          ),
        ],
      ),
    };
  }

  String _buildSummary() {
    return switch (_demoIndex) {
      1 => 'Selected: ${_selectedDateTime.toLocal()}',
      2 => 'Duration: ${_selectedDuration.toString().split('.').first}',
      _ => 'Fruit: ${_fruits[_fruitIndex]} · Size: ${_sizes[_sizeIndex]}',
    };
  }

  void _selectFruit(int index) {
    setState(() {
      _fruitIndex =
          ((index % _fruits.length) + _fruits.length) % _fruits.length;
    });
  }

  void _selectSize(int index) {
    setState(() {
      _sizeIndex = index;
    });
  }

  void _selectDateTime(DateTime value) {
    setState(() => _selectedDateTime = value);
  }

  void _selectDuration(Duration value) {
    setState(() => _selectedDuration = value);
  }
}
