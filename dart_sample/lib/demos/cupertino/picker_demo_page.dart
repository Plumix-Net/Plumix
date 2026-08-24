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
  int _fruitIndex = 1;
  int _sizeIndex = 1;

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
          'Scroll or tap a row. The left wheel loops; the right wheel uses '
          'the lazy builder API.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Text(
          'Fruit: ${_fruits[_fruitIndex]} · Size: ${_sizes[_sizeIndex]}',
          style: const TextStyle(fontSize: 13, color: Colors.blueGrey),
        ),
        Row(
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
                  selectionOverlay:
                      const CupertinoPickerDefaultSelectionOverlay(
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
}
