import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ContainerDemoPage extends StatefulWidget {
  const ContainerDemoPage({super.key});

  @override
  State<ContainerDemoPage> createState() => _ContainerDemoPageState();
}

class _ContainerDemoPageState extends State<ContainerDemoPage> {
  Alignment _alignment = Alignment.center;
  bool _withMargin = false;
  bool _clampConstraints = false;
  double _shiftX = 0;

  @override
  Widget build(BuildContext context) {
    final EdgeInsetsGeometry? margin = _withMargin
        ? const EdgeInsets.fromLTRB(12, 8, 12, 8)
        : null;
    final BoxConstraints? constraints = _clampConstraints
        ? const BoxConstraints(
            minWidth: 80,
            maxWidth: 120,
            minHeight: 50,
            maxHeight: 76,
          )
        : null;
    final Matrix4? transform = _shiftX.abs() < 0.001
        ? null
        : Matrix4.translationValues(_shiftX, 0, 0);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Container alignment + margin',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Switch alignment and margin to verify Container composition behavior.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'TopLeft',
              onTap: () => _setAlignment(Alignment.topLeft),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Center',
              onTap: () => _setAlignment(Alignment.center),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'BottomRight',
              onTap: () => _setAlignment(Alignment.bottomRight),
              width: 112,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _withMargin ? 'Margin: on' : 'Margin: off',
              onTap: _toggleMargin,
              width: 120,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Left',
              onTap: () => _move(-20),
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'Right',
              onTap: () => _move(20),
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: _clampConstraints ? 'Clamp: on' : 'Clamp: off',
              onTap: _toggleConstraints,
              width: 120,
              background: const Color(0xFFE8EDF9),
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
          'alignment=${_alignmentLabel(_alignment)}, margin=${_withMargin ? 'on' : 'off'}, clamp=${_clampConstraints ? 'on' : 'off'}, shiftX=${_shiftX.toStringAsFixed(0)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 240,
          height: 160,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: Container(
            margin: margin,
            width: 150,
            height: 90,
            constraints: constraints,
            transform: transform,
            alignment: _alignment,
            decoration: BoxDecoration(
              color: const Color(0xFFCCE3FF),
              border: Border.all(color: const Color(0xFF1D3557), width: 2),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Container(
              width: 32,
              height: 20,
              color: const Color(0xFF1D3557),
              child: const Center(
                child: Text(
                  'C',
                  style: TextStyle(fontSize: 12, color: Colors.white),
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

  void _setAlignment(Alignment alignment) {
    setState(() {
      _alignment = alignment;
    });
  }

  void _toggleMargin() {
    setState(() {
      _withMargin = !_withMargin;
    });
  }

  void _move(double delta) {
    setState(() {
      _shiftX = (_shiftX + delta).clamp(-40.0, 40.0);
    });
  }

  void _toggleConstraints() {
    setState(() {
      _clampConstraints = !_clampConstraints;
    });
  }

  void _reset() {
    setState(() {
      _alignment = Alignment.center;
      _withMargin = false;
      _clampConstraints = false;
      _shiftX = 0;
    });
  }

  static String _alignmentLabel(Alignment alignment) {
    if (alignment == Alignment.topLeft) {
      return 'topLeft';
    }

    if (alignment == Alignment.center) {
      return 'center';
    }

    if (alignment == Alignment.bottomRight) {
      return 'bottomRight';
    }

    return '(${alignment.x.toStringAsFixed(2)},${alignment.y.toStringAsFixed(2)})';
  }
}
