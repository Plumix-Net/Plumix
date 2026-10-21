import 'dart:convert';

import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';
import 'image_demo_page.dart';

class DecoratedBoxDemoPage extends StatefulWidget {
  const DecoratedBoxDemoPage({super.key});

  @override
  State<DecoratedBoxDemoPage> createState() => _DecoratedBoxDemoPageState();
}

class _DecoratedBoxDemoPageState extends State<DecoratedBoxDemoPage> {
  double _radius = 10;
  double _borderWidth = 2;
  bool _accentFill = true;
  BlurStyle _blurStyle = BlurStyle.normal;
  bool _multiply = false;
  double _fade = 0.5;
  bool _invert = false;
  bool _outerProbe = false;
  final MemoryImage _image = MemoryImage(base64Decode(demoSamplePng));

  @override
  Widget build(BuildContext context) {
    final Color fillColor = _accentFill
        ? const Color(0xFF9DC4FF)
        : const Color(0xFFF3F3F3);
    final Color borderColor = _accentFill
        ? const Color(0xFF1D3557)
        : const Color(0xFF555555);

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'DecoratedBox + Border + Radius',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Adjust border width and corner radius; toggle fill theme, shadow blur style and blend mode.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
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
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _buildButton(
                label: _accentFill ? 'Fill: accent' : 'Fill: neutral',
                onTap: _toggleFill,
                width: 128,
                background: const Color(0xFFF3E8D8),
              ),
              _buildButton(
                label: 'Shadow: ${_blurStyle.name}',
                onTap: _cycleBlurStyle,
                width: 128,
                background: const Color(0xFFE6E0F5),
              ),
              _buildButton(
                label: _multiply ? 'Blend: multiply' : 'Blend: none',
                onTap: _toggleBlend,
                width: 128,
                background: const Color(0xFFF5E0E6),
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
            'radius=${_radius.toStringAsFixed(0)}, border=${_borderWidth.toStringAsFixed(0)}, '
            'fill=${_accentFill ? 'accent' : 'neutral'}, shadow=${_blurStyle.name}, '
            'blend=${_multiply ? 'multiply' : 'none'}',
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
                    boxShadow: <BoxShadow>[
                      BoxShadow(
                        color: const Color(0x99000000),
                        offset: const Offset(0, 4),
                        blurRadius: 12,
                        spreadRadius: 1,
                        blurStyle: _blurStyle,
                      ),
                    ],
                    backgroundBlendMode: _multiply ? BlendMode.multiply : null,
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
          const Text(
            'Rounded corner hit testing',
            style: TextStyle(fontSize: 16, color: Colors.black),
          ),
          _buildButton(
            label: _outerProbe ? 'Point: outer' : 'Point: inner',
            onTap: () => setState(() => _outerProbe = !_outerProbe),
            width: 128,
            background: const Color(0xFFE0F0F5),
          ),
          Row(
            spacing: 16,
            children: [_buildGeometryProbe(false), _buildGeometryProbe(true)],
          ),
          const Text(
            'DecorationImage cross-fade',
            style: TextStyle(fontSize: 16, color: Colors.black),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildButton(
                label: 'Fade: ${(_fade * 100).toStringAsFixed(0)}%',
                onTap: _cycleFade,
                width: 128,
                background: const Color(0xFFE0F0F5),
              ),
              _buildButton(
                label: _invert ? 'Invert: on' : 'Invert: off',
                onTap: _toggleInvert,
                width: 128,
                background: const Color(0xFFF0E8F5),
              ),
            ],
          ),
          Text(
            'fade=${_fade.toStringAsFixed(2)}, invert=${_invert ? 'on' : 'off'}',
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
                    borderRadius: BorderRadius.circular(12),
                    image: DecorationImage.lerp(
                      DecorationImage(
                        image: _image,
                        fit: BoxFit.cover,
                        invertColors: _invert,
                      ),
                      DecorationImage(
                        image: _image,
                        repeat: ImageRepeat.repeat,
                        scale: 2,
                        colorFilter: const ColorFilter.mode(
                          Color(0xFF2A9D8F),
                          BlendMode.color,
                        ),
                      ),
                      _fade,
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGeometryProbe(bool continuous) {
    final contains = _GeometryProbePainter.contains(continuous, _outerProbe);
    return Column(
      children: [
        Text(
          '${continuous ? 'Continuous' : 'Circular'}: ${contains ? 'inside' : 'outside'}',
        ),
        CustomPaint(
          size: const Size(110, 110),
          painter: _GeometryProbePainter(continuous, _outerProbe),
        ),
      ],
    );
  }

  void _cycleFade() {
    setState(() {
      _fade = _fade >= 1.0 ? 0.0 : _fade + 0.25;
    });
  }

  void _toggleInvert() {
    setState(() {
      _invert = !_invert;
    });
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

  void _cycleBlurStyle() {
    setState(() {
      _blurStyle =
          BlurStyle.values[(_blurStyle.index + 1) % BlurStyle.values.length];
    });
  }

  void _toggleBlend() {
    setState(() {
      _multiply = !_multiply;
    });
  }

  void _reset() {
    setState(() {
      _radius = 10;
      _borderWidth = 2;
      _accentFill = true;
      _blurStyle = BlurStyle.normal;
      _multiply = false;
      _fade = 0.5;
      _invert = false;
      _outerProbe = false;
    });
  }
}

class _GeometryProbePainter extends CustomPainter {
  const _GeometryProbePainter(this.continuous, this.outer);
  final bool continuous;
  final bool outer;

  static Offset probe(bool outer) =>
      outer ? const Offset(95.74, 99.89) : const Offset(95.72, 99.87);

  static bool contains(bool continuous, bool outer) => continuous
      ? const RSuperellipse.fromLTRBXY(
          0,
          0,
          100,
          100,
          5,
          5,
        ).contains(probe(outer))
      : const RRect.fromLTRBXY(0, 0, 100, 100, 5, 5).contains(probe(outer));

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..color = const Color(0xFF9DC4FF);
    if (continuous) {
      canvas.drawRSuperellipse(
        const RSuperellipse.fromLTRBXY(5, 5, 105, 105, 5, 5),
        paint,
      );
    } else {
      canvas.drawRRect(const RRect.fromLTRBXY(5, 5, 105, 105, 5, 5), paint);
    }
    canvas.drawCircle(
      probe(outer) + const Offset(5, 5),
      2,
      Paint()
        ..color = contains(continuous, outer)
            ? const Color(0xFF1B5E20)
            : const Color(0xFFB71C1C),
    );
  }

  @override
  bool shouldRepaint(covariant _GeometryProbePainter oldDelegate) => true;
}
