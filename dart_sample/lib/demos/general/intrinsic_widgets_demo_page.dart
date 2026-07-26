import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class IntrinsicWidgetsDemoPage extends StatefulWidget {
  const IntrinsicWidgetsDemoPage({super.key});

  @override
  State<IntrinsicWidgetsDemoPage> createState() =>
      _IntrinsicWidgetsDemoPageState();
}

class _IntrinsicWidgetsDemoPageState extends State<IntrinsicWidgetsDemoPage> {
  bool _snapWidth = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'IntrinsicWidth + IntrinsicHeight',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'IntrinsicWidth snaps the content width to an optional step. '
          "IntrinsicHeight gives the Row the tallest child's height before "
          'stretch layout.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton('stepWidth: 0', false),
            _buildButton('stepWidth: 56', true),
          ],
        ),
        Container(
          height: 86,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(12),
          child: Align(
            alignment: Alignment.centerLeft,
            child: IntrinsicWidth(
              stepWidth: _snapWidth ? 56 : 0,
              child: Container(
                width: 70,
                color: const Color(0xFFCCE3FF),
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
                child: Text(
                  _snapWidth ? '70 → 112' : '70 px',
                  style: const TextStyle(
                    fontSize: 13,
                    color: Color(0xFF1D3557),
                  ),
                ),
              ),
            ),
          ),
        ),
        const Text(
          "All three tiles below receive the tallest tile's 64 px height.",
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Container(
          color: const Color(0xFFF1F5F9),
          padding: const EdgeInsets.all(12),
          child: IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 10,
              children: <Widget>[
                _buildTile('32', 32, const Color(0xFF457B9D)),
                _buildTile('64', 64, const Color(0xFF2A9D8F)),
                _buildTile('44', 44, const Color(0xFFE76F51)),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildButton(String label, bool enabled) {
    final bool selected = _snapWidth == enabled;
    return SizedBox(
      width: 128,
      child: CounterTapButton(
        label: label,
        onTap: () => setState(() => _snapWidth = enabled),
        background: selected
            ? const Color(0xFF1D3557)
            : const Color(0xFFDCE3ED),
        foreground: selected ? Colors.white : Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  static Widget _buildTile(String label, double height, Color color) {
    return Container(
      width: 70,
      height: height,
      color: color,
      alignment: Alignment.center,
      child: Text(
        label,
        style: const TextStyle(fontSize: 13, color: Colors.white),
      ),
    );
  }
}
