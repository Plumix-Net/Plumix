import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class FittedBoxDemoPage extends StatefulWidget {
  const FittedBoxDemoPage({super.key});

  @override
  State<FittedBoxDemoPage> createState() => _FittedBoxDemoPageState();
}

class _FittedBoxDemoPageState extends State<FittedBoxDemoPage> {
  BoxFit _fit = BoxFit.contain;
  Alignment _alignment = Alignment.center;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'FittedBox',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Change fit/alignment to inspect scaling behavior inside a fixed preview box.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Contain',
              onTap: () => _setFit(BoxFit.contain),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Cover',
              onTap: () => _setFit(BoxFit.cover),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Fill',
              onTap: () => _setFit(BoxFit.fill),
              width: 84,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'None',
              onTap: () => _setFit(BoxFit.none),
              width: 84,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Down',
              onTap: () => _setFit(BoxFit.scaleDown),
              width: 84,
              background: const Color(0xFFDCE3ED),
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
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Center',
              onTap: () => _setAlignment(Alignment.center),
              width: 96,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'BottomRight',
              onTap: () => _setAlignment(Alignment.bottomRight),
              width: 112,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Text(
          'fit=${_fitLabel(_fit)}, alignment=${_alignmentLabel(_alignment)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 190,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Container(
            color: Colors.white,
            child: FittedBox(
              fit: _fit,
              alignment: _alignment,
              child: Container(
                width: 160,
                height: 60,
                decoration: BoxDecoration(
                  color: const Color(0xFFCCE3FF),
                  border: Border.all(color: const Color(0xFF1D3557), width: 2),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  spacing: 8,
                  children: <Widget>[
                    const Text(
                      'WIDE',
                      style: TextStyle(fontSize: 14, color: Colors.black),
                    ),
                    Container(
                      width: 14,
                      height: 14,
                      color: const Color(0xFF1D3557),
                    ),
                  ],
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

  void _setFit(BoxFit value) {
    setState(() {
      _fit = value;
    });
  }

  void _setAlignment(Alignment value) {
    setState(() {
      _alignment = value;
    });
  }

  static String _fitLabel(BoxFit fit) {
    switch (fit) {
      case BoxFit.fill:
        return 'fill';
      case BoxFit.contain:
        return 'contain';
      case BoxFit.cover:
        return 'cover';
      case BoxFit.fitWidth:
        return 'fitWidth';
      case BoxFit.fitHeight:
        return 'fitHeight';
      case BoxFit.none:
        return 'none';
      case BoxFit.scaleDown:
        return 'scaleDown';
    }
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
