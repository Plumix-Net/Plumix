import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class OverflowIndicatorDemoPage extends StatefulWidget {
  const OverflowIndicatorDemoPage({super.key});

  @override
  State<OverflowIndicatorDemoPage> createState() =>
      _OverflowIndicatorDemoPageState();
}

class _OverflowIndicatorDemoPageState extends State<OverflowIndicatorDemoPage> {
  bool _horizontal = false;
  int _itemCount = 6;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'RenderFlex overflow indicator',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Intentionally overflows a Flex container to show Flutter-like yellow/black debug stripes and overflow labels.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Vertical',
              onTap: () => _setHorizontal(false),
              width: 90,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Horizontal',
              onTap: () => _setHorizontal(true),
              width: 94,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: '- items',
              onTap: _decreaseItems,
              width: 84,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: '+ items',
              onTap: _increaseItems,
              width: 84,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Text(
          'axis=${_horizontal ? 'horizontal' : 'vertical'}, itemCount=$_itemCount (overflow expected)',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 280,
          height: 220,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Center(child: _buildProbe()),
        ),
      ],
    );
  }

  Widget _buildProbe() {
    if (_horizontal) {
      return Container(
        width: 240,
        height: 120,
        color: Colors.white,
        padding: const EdgeInsets.all(8),
        child: Row(
          spacing: 8,
          children: List<Widget>.generate(_itemCount, _buildHorizontalTile),
        ),
      );
    }

    return Container(
      width: 240,
      height: 120,
      color: Colors.white,
      padding: const EdgeInsets.all(8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: List<Widget>.generate(_itemCount, _buildVerticalTile),
      ),
    );
  }

  Widget _buildHorizontalTile(int index) {
    return Container(
      width: 72,
      height: 86,
      color: index.isEven ? const Color(0xFFBBDEFB) : const Color(0xFFC8E6C9),
      child: Center(
        child: Text(
          'tile $index',
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }

  Widget _buildVerticalTile(int index) {
    return Container(
      height: 34,
      color: index.isEven ? const Color(0xFFBBDEFB) : const Color(0xFFC8E6C9),
      child: Center(
        child: Text(
          'row $index',
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }

  Widget _buildButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: background,
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  void _setHorizontal(bool value) {
    setState(() {
      _horizontal = value;
    });
  }

  void _increaseItems() {
    setState(() {
      _itemCount = (_itemCount + 1).clamp(3, 12);
    });
  }

  void _decreaseItems() {
    setState(() {
      _itemCount = (_itemCount - 1).clamp(3, 12);
    });
  }
}
