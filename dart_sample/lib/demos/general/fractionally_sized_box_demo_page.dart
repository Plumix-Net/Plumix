import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class FractionallySizedBoxDemoPage extends StatefulWidget {
  const FractionallySizedBoxDemoPage({super.key});

  @override
  State<FractionallySizedBoxDemoPage> createState() =>
      _FractionallySizedBoxDemoPageState();
}

class _FractionallySizedBoxDemoPageState
    extends State<FractionallySizedBoxDemoPage> {
  Alignment _alignment = Alignment.center;
  double? _widthFactor = 0.7;
  double? _heightFactor = 0.55;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'FractionallySizedBox',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Configure width/height factors and alignment; null factor means pass-through on that axis.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'W pass',
              onTap: () => _setWidthFactor(null),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'W 0.5',
              onTap: () => _setWidthFactor(0.5),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'W 0.8',
              onTap: () => _setWidthFactor(0.8),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'H pass',
              onTap: () => _setHeightFactor(null),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'H 0.4',
              onTap: () => _setHeightFactor(0.4),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'H 0.7',
              onTap: () => _setHeightFactor(0.7),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'TopLeft',
              onTap: () => _setAlignment(Alignment.topLeft),
              width: 96,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'Center',
              onTap: () => _setAlignment(Alignment.center),
              width: 96,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'BottomRight',
              onTap: () => _setAlignment(Alignment.bottomRight),
              width: 112,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'widthFactor=${_formatFactor(_widthFactor)}, heightFactor=${_formatFactor(_heightFactor)}, alignment=${_alignmentLabel(_alignment)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 190,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: FractionallySizedBox(
            alignment: _alignment,
            widthFactor: _widthFactor,
            heightFactor: _heightFactor,
            child: Container(
              decoration: BoxDecoration(
                color: const Color(0xFFCCE3FF),
                border: Border.all(color: const Color(0xFF1D3557), width: 2),
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Center(
                child: Text(
                  'fraction child',
                  style: TextStyle(fontSize: 14, color: Colors.black),
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

  void _setWidthFactor(double? value) {
    setState(() {
      _widthFactor = value;
    });
  }

  void _setHeightFactor(double? value) {
    setState(() {
      _heightFactor = value;
    });
  }

  void _setAlignment(Alignment value) {
    setState(() {
      _alignment = value;
    });
  }

  static String _formatFactor(double? value) {
    return value == null ? 'pass' : value.toStringAsFixed(2);
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

    return 'custom';
  }
}
