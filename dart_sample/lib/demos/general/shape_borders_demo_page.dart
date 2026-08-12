import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ShapeBordersDemoPage extends StatefulWidget {
  const ShapeBordersDemoPage({super.key});

  @override
  State<ShapeBordersDemoPage> createState() => _ShapeBordersDemoPageState();
}

class _ShapeBordersDemoPageState extends State<ShapeBordersDemoPage> {
  static const List<String> _shapeNames = <String>[
    'RoundedRectangle',
    'Stadium',
    'Circle',
    'Oval',
    'Beveled',
    'Continuous',
    'Star',
    'Polygon',
    'Border',
  ];

  int _shapeIndex = 0;
  double _sideWidth = 4;
  bool _lerpToCircle = false;

  @override
  Widget build(BuildContext context) {
    final BorderSide side = BorderSide(
      color: const Color(0xFF1D3557),
      width: _sideWidth,
    );
    ShapeBorder shape = _buildShape(side);
    if (_lerpToCircle) {
      shape = ShapeBorder.lerp(shape, CircleBorder(side: side), 0.5)!;
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ShapeBorder hierarchy',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Every shape paints its own outline and clips its own path.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Shape <',
              onTap: () => _changeShape(-1),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Shape >',
              onTap: () => _changeShape(1),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Side -',
              onTap: () => _changeSide(-1),
              width: 78,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Side +',
              onTap: () => _changeSide(1),
              width: 78,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _lerpToCircle ? 'Lerp: 50% circle' : 'Lerp: off',
              onTap: _toggleLerp,
              width: 148,
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
          'shape=${_shapeNames[_shapeIndex]}, side=${_sideWidth.toStringAsFixed(0)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 160,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: Center(
            child: SizedBox(
              width: 180,
              height: 110,
              child: DecoratedBox(
                decoration: ShapeDecoration(
                  shape: shape,
                  color: const Color(0xFF9DC4FF),
                ),
                child: ClipPath(
                  clipper: ShapeBorderClipper(shape: shape),
                  child: const Center(
                    child: Text(
                      'Shaped',
                      style: TextStyle(fontSize: 14, color: Color(0xFF14213D)),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }

  ShapeBorder _buildShape(BorderSide side) {
    switch (_shapeNames[_shapeIndex]) {
      case 'RoundedRectangle':
        return RoundedRectangleBorder(
          side: side,
          borderRadius: BorderRadius.circular(18),
        );
      case 'Stadium':
        return StadiumBorder(side: side);
      case 'Circle':
        return CircleBorder(side: side);
      case 'Oval':
        return OvalBorder(side: side);
      case 'Beveled':
        return BeveledRectangleBorder(
          side: side,
          borderRadius: BorderRadius.circular(24),
        );
      case 'Continuous':
        return ContinuousRectangleBorder(
          side: side,
          borderRadius: BorderRadius.circular(28),
        );
      case 'Star':
        return StarBorder(
          side: side,
          points: 6,
          innerRadiusRatio: 0.55,
          pointRounding: 0.2,
        );
      case 'Polygon':
        return StarBorder.polygon(side: side, sides: 6);
      default:
        return Border(
          top: side,
          right: side.copyWith(width: side.width / 2.0),
          bottom: side,
          left: side.copyWith(width: side.width / 2.0),
        );
    }
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

  void _changeShape(int delta) {
    setState(() {
      _shapeIndex = (_shapeIndex + delta + _shapeNames.length) % _shapeNames.length;
    });
  }

  void _changeSide(double delta) {
    setState(() {
      _sideWidth = (_sideWidth + delta).clamp(0, 12).toDouble();
    });
  }

  void _toggleLerp() {
    setState(() {
      _lerpToCircle = !_lerpToCircle;
    });
  }

  void _reset() {
    setState(() {
      _shapeIndex = 0;
      _sideWidth = 4;
      _lerpToCircle = false;
    });
  }
}
