import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class StackDemoPage extends StatefulWidget {
  const StackDemoPage({super.key});

  @override
  State<StackDemoPage> createState() => _StackDemoPageState();
}

class _StackDemoPageState extends State<StackDemoPage> {
  double _left = 8;
  double _top = 8;
  bool _pinBottomRight = false;

  @override
  Widget build(BuildContext context) {
    final Widget badge = Positioned(
      left: _pinBottomRight ? null : _left,
      top: _pinBottomRight ? null : _top,
      right: _pinBottomRight ? 8 : null,
      bottom: _pinBottomRight ? 8 : null,
      child: Container(
        width: 56,
        height: 30,
        color: const Color(0xFFD1495B),
        child: const Center(
          child: Text(
            'badge',
            style: TextStyle(fontSize: 11, color: Colors.white),
          ),
        ),
      ),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Stack + Positioned',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Mix non-positioned and positioned children; toggle pinned mode and nudge offsets.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Left',
              onTap: () => _move(-8, 0),
              width: 72,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Right',
              onTap: () => _move(8, 0),
              width: 72,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Up',
              onTap: () => _move(0, -8),
              width: 72,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Down',
              onTap: () => _move(0, 8),
              width: 72,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _pinBottomRight ? 'Pin: BR' : 'Pin: custom',
              onTap: _togglePin,
              width: 120,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Reset',
              onTap: _reset,
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'left=${_left.toStringAsFixed(0)}, top=${_top.toStringAsFixed(0)}, mode=${_pinBottomRight ? 'bottomRight' : 'custom'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 220,
          height: 140,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: Container(
            color: Colors.white,
            child: Stack(
              alignment: Alignment.center,
              children: <Widget>[
                Container(
                  width: 140,
                  height: 80,
                  color: const Color(0xFFCCE3FF),
                  child: const Center(
                    child: Text(
                      'base',
                      style: TextStyle(fontSize: 14, color: Color(0xFF1D3557)),
                    ),
                  ),
                ),
                badge,
              ],
            ),
          ),
        ),
      ],
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

  void _move(double dx, double dy) {
    if (_pinBottomRight) {
      return;
    }

    setState(() {
      _left = (_left + dx).clamp(0.0, 150.0);
      _top = (_top + dy).clamp(0.0, 90.0);
    });
  }

  void _togglePin() {
    setState(() {
      _pinBottomRight = !_pinBottomRight;
    });
  }

  void _reset() {
    setState(() {
      _left = 8;
      _top = 8;
      _pinBottomRight = false;
    });
  }
}
