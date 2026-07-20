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
  double _fractionalShift = 0;
  int _quarterTurns = 0;

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
          'Proxy widgets: transforms + clips',
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
        const Text(
          'FractionalTranslation + RotatedBox',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Shift',
              onTap: _cycleFractionalShift,
              width: 88,
              background: const Color(0xFFE8EDF9),
            ),
            _buildButton(
              label: 'Rotate',
              onTap: _rotateQuarterTurn,
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
            Text(
              'fraction=${_fractionalShift.toStringAsFixed(1)}, turns=$_quarterTurns',
              style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
            ),
          ],
        ),
        Row(
          spacing: 16,
          children: <Widget>[
            Container(
              width: 120,
              height: 80,
              color: const Color(0xFFE7EDF6),
              child: Center(
                child: FractionalTranslation(
                  translation: Offset(_fractionalShift, 0),
                  child: Container(
                    width: 56,
                    height: 32,
                    color: const Color(0xFF6750A4),
                    child: const Center(
                      child: Text(
                        'Shift',
                        style: TextStyle(fontSize: 12, color: Colors.white),
                      ),
                    ),
                  ),
                ),
              ),
            ),
            Container(
              width: 120,
              height: 80,
              color: const Color(0xFFE7EDF6),
              child: Center(
                child: RotatedBox(
                  quarterTurns: _quarterTurns,
                  child: Container(
                    width: 64,
                    height: 28,
                    color: const Color(0xFF386A20),
                    child: const Center(
                      child: Text(
                        'Rotate',
                        style: TextStyle(fontSize: 12, color: Colors.white),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
        const Text(
          'ClipOval + ClipPath',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        Row(
          spacing: 16,
          children: <Widget>[
            SizedBox(
              width: 96,
              height: 72,
              child: ClipOval(
                child: ColoredBox(
                  color: Color(0xFF6750A4),
                  child: Center(
                    child: Text(
                      'Oval',
                      style: TextStyle(fontSize: 13, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ),
            SizedBox(
              width: 96,
              height: 72,
              child: ClipPath(
                clipper: _TrianglePathClipper(),
                child: ColoredBox(
                  color: Color(0xFF386A20),
                  child: Center(
                    child: Text(
                      'Path',
                      style: TextStyle(fontSize: 13, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ),
          ],
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

  void _cycleFractionalShift() {
    setState(() {
      _fractionalShift = _fractionalShift >= 0.5
          ? -0.5
          : _fractionalShift + 0.5;
    });
  }

  void _rotateQuarterTurn() {
    setState(() {
      _quarterTurns = (_quarterTurns + 1) % 4;
    });
  }

  void _reset() {
    setState(() {
      _opacity = 0.8;
      _shiftX = 0;
      _tightClip = true;
      _fractionalShift = 0;
      _quarterTurns = 0;
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

class _TrianglePathClipper extends CustomClipper<Path> {
  const _TrianglePathClipper();

  @override
  Path getClip(Size size) {
    return Path()
      ..moveTo(size.width / 2, 0)
      ..lineTo(size.width, size.height)
      ..lineTo(0, size.height)
      ..close();
  }

  @override
  bool shouldReclip(_TrianglePathClipper oldClipper) => false;
}
