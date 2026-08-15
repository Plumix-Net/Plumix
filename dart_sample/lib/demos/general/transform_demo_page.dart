import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class TransformDemoPage extends StatefulWidget {
  const TransformDemoPage({super.key});

  @override
  State<TransformDemoPage> createState() => _TransformDemoPageState();
}

class _TransformDemoPageState extends State<TransformDemoPage> {
  double _turns = 0;
  double _scale = 1.0;
  double _perspectiveTurns = 0;
  bool _flipX = false;
  bool _flipY = false;
  bool _alignTopLeft = false;

  @override
  Widget build(BuildContext context) {
    final Alignment alignment = _alignTopLeft
        ? Alignment.topLeft
        : Alignment.center;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Transform + Matrix4',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Transform carries a full 4x4 matrix, so rotations about the X/Y '
          'axes and a perspective row render and hit-test alike.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Rotate',
              onTap: () {
                setState(() {
                  _turns += 0.125;
                });
              },
            ),
            _buildButton(
              label: 'Scale',
              onTap: () {
                setState(() {
                  _scale = _scale >= 1.5 ? 0.5 : _scale + 0.25;
                });
              },
            ),
            _buildButton(
              label: _alignTopLeft ? 'Anchor: top left' : 'Anchor: center',
              onTap: () {
                setState(() {
                  _alignTopLeft = !_alignTopLeft;
                });
              },
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _flipX ? 'Flip X: on' : 'Flip X: off',
              onTap: () {
                setState(() {
                  _flipX = !_flipX;
                });
              },
            ),
            _buildButton(
              label: _flipY ? 'Flip Y: on' : 'Flip Y: off',
              onTap: () {
                setState(() {
                  _flipY = !_flipY;
                });
              },
            ),
            _buildButton(
              label: 'Perspective',
              onTap: () {
                setState(() {
                  _perspectiveTurns += 0.08;
                });
              },
            ),
          ],
        ),
        Text(
          'turns=${_turns.toStringAsFixed(3)}, '
          'scale=${_scale.toStringAsFixed(2)}, '
          'perspective=${_perspectiveTurns.toStringAsFixed(2)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Row(
          spacing: 16,
          children: <Widget>[
            _buildStage(
              'rotate + scale',
              Transform.rotate(
                angle: _turns * math.pi * 2.0,
                alignment: alignment,
                child: Transform.scale(
                  scale: _scale,
                  alignment: alignment,
                  child: _buildCard('Card', const Color(0xFF1565C0)),
                ),
              ),
            ),
            _buildStage(
              'flip',
              Transform.flip(
                flipX: _flipX,
                flipY: _flipY,
                child: _buildCard('Flip', const Color(0xFF2E7D32)),
              ),
            ),
          ],
        ),
        Row(
          spacing: 16,
          children: <Widget>[
            _buildStage(
              'perspective rotateY',
              Transform(
                transform: _buildPerspectiveTransform(),
                alignment: Alignment.center,
                child: _buildCard('3D', const Color(0xFFF57C00)),
              ),
            ),
            _buildStage(
              'translate',
              Transform.translate(
                offset: Offset(_turns * 40.0, 0.0),
                child: _buildCard('Move', const Color(0xFF6750A4)),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Matrix4 _buildPerspectiveTransform() {
    return Matrix4.identity()
      ..setEntry(3, 2, 0.002)
      ..rotateY(_perspectiveTurns * math.pi * 2.0);
  }

  static Widget _buildStage(String label, Widget child) {
    return Column(
      spacing: 6,
      children: <Widget>[
        Text(label, style: const TextStyle(fontSize: 12, color: Colors.blueGrey)),
        Container(
          width: 150,
          height: 120,
          color: const Color(0xFFF3F6FA),
          alignment: Alignment.center,
          child: child,
        ),
      ],
    );
  }

  static Widget _buildCard(String label, Color color) {
    return Container(
      width: 90,
      height: 56,
      color: color,
      alignment: Alignment.center,
      child: Text(
        label,
        style: const TextStyle(fontSize: 14, color: Colors.white),
      ),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 140,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }
}
