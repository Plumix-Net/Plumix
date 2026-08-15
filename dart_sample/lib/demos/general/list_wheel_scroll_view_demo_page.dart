import 'package:material_ui/material_ui.dart';

class ListWheelScrollViewDemoPage extends StatefulWidget {
  const ListWheelScrollViewDemoPage({super.key});

  @override
  State<ListWheelScrollViewDemoPage> createState() => _ListWheelScrollViewDemoPageState();
}

class _ListWheelScrollViewDemoPageState extends State<ListWheelScrollViewDemoPage> {
  static const int _itemCount = 24;

  late final FixedExtentScrollController _controller;
  int _selectedItem = 6;
  bool _useMagnifier = true;

  @override
  void initState() {
    super.initState();
    _controller = FixedExtentScrollController(initialItem: _selectedItem);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ListWheelScrollView',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Children are laid out lazily on a cylinder; FixedExtentScrollPhysics snaps to whole items.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: Row(
            children: <Widget>[
              Expanded(
                child: ListWheelScrollView(
                  controller: _controller,
                  itemExtent: 48,
                  physics: const FixedExtentScrollPhysics(),
                  useMagnifier: _useMagnifier,
                  magnification: 1.3,
                  overAndUnderCenterOpacity: 0.6,
                  onSelectedItemChanged: (int item) => setState(() => _selectedItem = item),
                  children: List<Widget>.generate(_itemCount, (int index) {
                    return Center(
                      child: Text(
                        'item #$index',
                        style: const TextStyle(fontSize: 18, color: Colors.black),
                      ),
                    );
                  }),
                ),
              ),
              Expanded(
                child: ListWheelScrollView.useDelegate(
                  itemExtent: 40,
                  diameterRatio: 1.2,
                  offAxisFraction: -0.5,
                  squeeze: 1.2,
                  childDelegate: ListWheelChildLoopingListDelegate(
                    children: List<Widget>.generate(12, (int index) {
                      return Center(
                        child: Text(
                          'loop $index',
                          style: const TextStyle(fontSize: 16, color: Colors.black54),
                        ),
                      );
                    }),
                  ),
                ),
              ),
            ],
          ),
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          spacing: 12,
          children: <Widget>[
            TextButton(
              onPressed: () => _controller.animateToItem(
                (_selectedItem - 1).clamp(0, _itemCount - 1),
                duration: const Duration(milliseconds: 300),
                curve: Curves.ease,
              ),
              child: const Text('Previous'),
            ),
            Text(
              'selected item $_selectedItem',
              style: const TextStyle(fontSize: 14, color: Colors.black),
            ),
            TextButton(
              onPressed: () => _controller.animateToItem(
                (_selectedItem + 1).clamp(0, _itemCount - 1),
                duration: const Duration(milliseconds: 300),
                curve: Curves.ease,
              ),
              child: const Text('Next'),
            ),
            TextButton(
              onPressed: () => setState(() => _useMagnifier = !_useMagnifier),
              child: Text(_useMagnifier ? 'Magnifier on' : 'Magnifier off'),
            ),
          ],
        ),
      ],
    );
  }
}
