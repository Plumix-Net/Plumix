import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ProxyWidgetsDemoPage extends StatefulWidget {
  const ProxyWidgetsDemoPage({super.key});

  @override
  State<ProxyWidgetsDemoPage> createState() => _ProxyWidgetsDemoPageState();
}

class _ProxyWidgetsDemoPageState extends State<ProxyWidgetsDemoPage> {
  double _opacity = 0.8;
  double _shiftX = 0;
  bool _tightClip = true;

  @override
  Widget build(BuildContext context) {
    final Rect clip = _tightClip
        ? const Rect.fromLTWH(0, 0, 120, 80)
        : const Rect.fromLTWH(0, 0, 190, 110);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Proxy widgets: Opacity + Transform + ClipRect',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Use controls to fade a high-contrast black card over white canvas.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Opacity -',
              onTap: () => _changeOpacity(-0.3),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Opacity +',
              onTap: () => _changeOpacity(0.3),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Reset',
              onTap: _reset,
              width: 88,
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
              label: _tightClip ? 'Clip: tight' : 'Clip: wide',
              onTap: _toggleClip,
              width: 104,
              background: const Color(0xFFE8EDF9),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Opacity 0',
              onTap: () => _setOpacity(0),
              width: 96,
              background: const Color(0xFFF6E0E0),
            ),
            _buildButton(
              label: 'Opacity 1',
              onTap: () => _setOpacity(1),
              width: 96,
              background: const Color(0xFFE0F0E7),
            ),
          ],
        ),
        Text(
          'opacity=${_opacity.toStringAsFixed(2)}, shiftX=${_shiftX.toStringAsFixed(0)}, clip=${_tightClip ? 'tight' : 'wide'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 220,
          height: 140,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: ClipRect(
            clipper: _FixedRectClipper(clip),
            child: Transform.translate(
              offset: Offset(_shiftX, 10),
              child: Opacity(
                opacity: _opacity,
                child: Container(
                  width: 140,
                  height: 90,
                  color: const Color(0xFF111111),
                  padding: const EdgeInsets.all(8),
                  child: const Text(
                    'Layer',
                    style: TextStyle(fontSize: 14, color: Colors.white),
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

  void _changeOpacity(double delta) {
    setState(() {
      _opacity = (_opacity + delta).clamp(0.0, 1.0);
    });
  }

  void _setOpacity(double value) {
    setState(() {
      _opacity = value.clamp(0.0, 1.0);
    });
  }

  void _move(double delta) {
    setState(() {
      _shiftX = math.max(-40, math.min(80, _shiftX + delta));
    });
  }

  void _toggleClip() {
    setState(() {
      _tightClip = !_tightClip;
    });
  }

  void _reset() {
    setState(() {
      _opacity = 0.8;
      _shiftX = 0;
      _tightClip = true;
    });
  }
}

class _FixedRectClipper extends CustomClipper<Rect> {
  const _FixedRectClipper(this.rect);

  final Rect rect;

  @override
  Rect getClip(Size size) => rect;

  @override
  bool shouldReclip(_FixedRectClipper oldClipper) => oldClipper.rect != rect;
}
