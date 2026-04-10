import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class DecoratedBoxDemoPage extends StatefulWidget {
  const DecoratedBoxDemoPage({super.key});

  @override
  State<DecoratedBoxDemoPage> createState() => _DecoratedBoxDemoPageState();
}

class _DecoratedBoxDemoPageState extends State<DecoratedBoxDemoPage> {
  double _radius = 10;
  double _borderWidth = 2;
  bool _accentFill = true;

  @override
  Widget build(BuildContext context) {
    final Color fillColor = _accentFill
        ? const Color(0xFF9DC4FF)
        : const Color(0xFFF3F3F3);
    final Color borderColor = _accentFill
        ? const Color(0xFF1D3557)
        : const Color(0xFF555555);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'DecoratedBox + Border + Radius',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Adjust border width and corner radius; toggle fill theme.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Radius -',
              onTap: () => _changeRadius(-4),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Radius +',
              onTap: () => _changeRadius(4),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Border -',
              onTap: () => _changeBorder(-1),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Border +',
              onTap: () => _changeBorder(1),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _accentFill ? 'Fill: accent' : 'Fill: neutral',
              onTap: _toggleFill,
              width: 128,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'Reset',
              onTap: _reset,
              width: 88,
              background: const Color(0xFFE8EDF9),
            ),
          ],
        ),
        Text(
          'radius=${_radius.toStringAsFixed(0)}, border=${_borderWidth.toStringAsFixed(0)}, fill=${_accentFill ? 'accent' : 'neutral'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 220,
          height: 140,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: Center(
            child: SizedBox(
              width: 140,
              height: 90,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: fillColor,
                  border: Border.all(color: borderColor, width: _borderWidth),
                  borderRadius: BorderRadius.circular(_radius),
                ),
                child: const Center(
                  child: Text(
                    'Decorated',
                    style: TextStyle(fontSize: 14, color: Color(0xFF14213D)),
                  ),
                ),
              ),
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

  void _changeRadius(double delta) {
    setState(() {
      _radius = (_radius + delta).clamp(0.0, 44.0);
    });
  }

  void _changeBorder(double delta) {
    setState(() {
      _borderWidth = (_borderWidth + delta).clamp(0.0, 12.0);
    });
  }

  void _toggleFill() {
    setState(() {
      _accentFill = !_accentFill;
    });
  }

  void _reset() {
    setState(() {
      _radius = 10;
      _borderWidth = 2;
      _accentFill = true;
    });
  }
}
