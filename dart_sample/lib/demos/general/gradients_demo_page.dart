import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class GradientsDemoPage extends StatefulWidget {
  const GradientsDemoPage({super.key});

  @override
  State<GradientsDemoPage> createState() => _GradientsDemoPageState();
}

class _GradientsDemoPageState extends State<GradientsDemoPage> {
  static const List<TileMode> _tileModes = <TileMode>[
    TileMode.clamp,
    TileMode.repeated,
    TileMode.mirror,
    TileMode.decal,
  ];

  double _rotation = 0;
  int _tileModeIndex = 0;
  bool _blended = false;

  TileMode get _currentTileMode => _tileModes[_tileModeIndex];

  GradientTransform? get _currentTransform =>
      _rotation == 0 ? null : GradientRotation(_rotation);

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Gradients + BoxShadow lerp',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Linear, radial and sweep gradients share stops, tile modes and a rotation transform.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Rotate -',
              onTap: () => _changeRotation(-math.pi / 8),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Rotate +',
              onTap: () => _changeRotation(math.pi / 8),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Tile: ${_currentTileMode.name}',
              onTap: _cycleTileMode,
              width: 132,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: _blended ? 'Blend: B' : 'Blend: A',
              onTap: _toggleBlend,
              width: 108,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'rotation=${_rotation.toStringAsFixed(2)} rad, tileMode=${_currentTileMode.name}, '
          'target=${_blended ? 'B' : 'A'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Row(
          spacing: 12,
          children: <Widget>[
            _buildSwatch('Linear', _buildLinearGradient()),
            _buildSwatch('Radial', _buildRadialGradient()),
            _buildSwatch('Sweep', _buildSweepGradient()),
          ],
        ),
        const Text(
          'The card below animates its gradient colors and its shadow list at the same time.',
          style: TextStyle(fontSize: 12, color: Colors.black54),
        ),
        Center(
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 450),
            width: 240,
            height: 96,
            decoration: _buildAnimatedDecoration(),
            child: const Center(
              child: Text(
                'AnimatedContainer',
                style: TextStyle(fontSize: 14, color: Colors.white),
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSwatch(String label, Gradient gradient) {
    return Column(
      spacing: 6,
      children: <Widget>[
        SizedBox(
          width: 96,
          height: 96,
          child: DecoratedBox(decoration: BoxDecoration(gradient: gradient)),
        ),
        Text(label, style: const TextStyle(fontSize: 12, color: Colors.blueGrey)),
      ],
    );
  }

  Gradient _buildLinearGradient() {
    return LinearGradient(
      colors: const <Color>[
        Color(0xFF1D3557),
        Color(0xFF9DC4FF),
        Color(0xFFF3E8D8),
      ],
      begin: Alignment.topLeft,
      end: Alignment.bottomRight,
      stops: const <double>[0.0, 0.35, 0.7],
      tileMode: _currentTileMode,
      transform: _currentTransform,
    );
  }

  Gradient _buildRadialGradient() {
    return RadialGradient(
      colors: const <Color>[
        Color(0xFFFFF1D0),
        Color(0xFFE76F51),
        Color(0xFF1D3557),
      ],
      center: Alignment.center,
      radius: 0.35,
      stops: const <double>[0.0, 0.55, 1.0],
      tileMode: _currentTileMode,
      focal: Alignment.topLeft,
      transform: _currentTransform,
    );
  }

  Gradient _buildSweepGradient() {
    return SweepGradient(
      colors: const <Color>[
        Color(0xFF2A9D8F),
        Color(0xFFE9C46A),
        Color(0xFF264653),
      ],
      center: Alignment.center,
      startAngle: 0.0,
      endAngle: math.pi * 1.5,
      tileMode: _currentTileMode,
      transform: _currentTransform,
    );
  }

  BoxDecoration _buildAnimatedDecoration() {
    final List<BoxShadow> shadows = _blended
        ? const <BoxShadow>[
            BoxShadow(
              color: Color(0x5A1D3557),
              offset: Offset(0, 10),
              blurRadius: 18,
            ),
            BoxShadow(
              color: Color(0x321D3557),
              offset: Offset(0, 2),
              blurRadius: 4,
            ),
          ]
        : const <BoxShadow>[
            BoxShadow(
              color: Color(0x28000000),
              offset: Offset(0, 2),
              blurRadius: 6,
            ),
          ];

    return BoxDecoration(
      gradient: LinearGradient(
        colors: _blended
            ? const <Color>[Color(0xFF264653), Color(0xFF2A9D8F)]
            : const <Color>[Color(0xFFE76F51), Color(0xFFF4A261)],
        begin: _blended ? Alignment.topLeft : Alignment.bottomLeft,
        end: _blended ? Alignment.bottomRight : Alignment.topRight,
      ),
      borderRadius: BorderRadius.circular(16),
      boxShadow: shadows,
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

  void _changeRotation(double delta) {
    setState(() {
      _rotation = (_rotation + delta).clamp(-math.pi, math.pi);
    });
  }

  void _cycleTileMode() {
    setState(() {
      _tileModeIndex = (_tileModeIndex + 1) % _tileModes.length;
    });
  }

  void _toggleBlend() {
    setState(() {
      _blended = !_blended;
    });
  }
}
