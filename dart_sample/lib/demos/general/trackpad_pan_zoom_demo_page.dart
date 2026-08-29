import 'package:flutter/gestures.dart';
import 'package:flutter/widgets.dart';

class TrackpadPanZoomDemoPage extends StatefulWidget {
  const TrackpadPanZoomDemoPage({super.key});

  @override
  State<TrackpadPanZoomDemoPage> createState() => _TrackpadPanZoomDemoPageState();
}

class _TrackpadPanZoomDemoPageState extends State<TrackpadPanZoomDemoPage> {
  Offset _pan = Offset.zero;
  double _scale = 1.0;
  double _rotation = 0.0;
  int _updates = 0;
  String _phase = 'idle';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Trackpad pan / zoom',
          style: TextStyle(fontSize: 20, color: Color(0xFF000000)),
        ),
        const Text(
          'Pinch or rotate on a trackpad over the panel. The platform reports the gesture '
          'as PointerPanZoom events, which Listener surfaces directly.',
          style: TextStyle(fontSize: 14, color: Color(0xFF696969)),
        ),
        _buildProbe(),
        Text(
          'phase $_phase — pan ${_format(_pan.dx)}, ${_format(_pan.dy)} — '
          'scale ${_format(_scale)} — rotation ${_format(_rotation)} rad — $_updates updates',
          style: const TextStyle(fontSize: 13, color: Color(0xFF696969)),
        ),
      ],
    );
  }

  Widget _buildProbe() {
    return Listener(
      behavior: HitTestBehavior.opaque,
      onPointerPanZoomStart: (PointerPanZoomStartEvent event) => setState(() {
        _phase = 'active';
        _pan = Offset.zero;
        _scale = 1.0;
        _rotation = 0.0;
        _updates = 0;
      }),
      onPointerPanZoomUpdate: (PointerPanZoomUpdateEvent event) => setState(() {
        _pan = event.pan;
        _scale = event.scale;
        _rotation = event.rotation;
        _updates++;
      }),
      onPointerPanZoomEnd: (PointerPanZoomEndEvent event) => setState(() => _phase = 'idle'),
      child: Container(
        height: 220,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: const Color(0xFFF1F3F4),
          border: Border.all(color: const Color(0xFF9AA0A6)),
          borderRadius: BorderRadius.circular(10),
        ),
        child: _buildTarget(),
      ),
    );
  }

  Widget _buildTarget() {
    final Matrix4 transform = Matrix4.translationValues(_pan.dx, _pan.dy, 0.0)
      ..multiply(Matrix4.rotationZ(_rotation))
      ..multiply(Matrix4.diagonal3Values(_scale, _scale, 1.0));

    return Transform(
      transform: transform,
      alignment: Alignment.center,
      child: Container(
        width: 96,
        height: 96,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: const Color(0xFF00796B),
          borderRadius: BorderRadius.circular(12),
        ),
        child: const Text(
          'pinch me',
          style: TextStyle(fontSize: 13, color: Color(0xFFFFFFFF)),
        ),
      ),
    );
  }

  static String _format(double value) => value.toStringAsFixed(2);
}
