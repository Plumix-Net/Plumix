import 'dart:ui' as ui;

import 'package:flutter/rendering.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class SceneCompositingDemoPage extends StatefulWidget {
  const SceneCompositingDemoPage({super.key});

  @override
  State<SceneCompositingDemoPage> createState() =>
      _SceneCompositingDemoPageState();
}

class _SceneCompositingDemoPageState extends State<SceneCompositingDemoPage> {
  static const List<Color> _tints = <Color>[
    Color(0xFF9FC5E8),
    Color(0xFFFFE082),
    Color(0xFFB6D7A8),
  ];

  final GlobalKey _boundaryKey = GlobalKey(
    debugLabel: 'scene compositing boundary',
  );
  bool _showOverlay = false;
  int _tint = 0;
  ui.Image? _capture;

  @override
  void dispose() {
    _capture?.dispose();
    super.dispose();
  }

  Future<void> _captureBoundary() async {
    final boundary =
        _boundaryKey.currentContext!.findRenderObject()!
            as RenderRepaintBoundary;
    final ui.Image image = await boundary.toImage(pixelRatio: 2.0);
    if (!mounted) {
      image.dispose();
      return;
    }
    setState(() {
      _capture?.dispose();
      _capture = image;
    });
  }

  @override
  Widget build(BuildContext context) {
    final ui.Image? capture = _capture;
    final Widget page = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'dart:ui SceneBuilder + Scene',
          style: TextStyle(fontSize: 20, color: Color(0xFF000000)),
        ),
        const Text(
          'Every frame is built into a retained scene: clean layers are '
          're-added with addRetained. RepaintBoundary.toImage rasterizes one '
          'boundary at 2x.',
          style: TextStyle(fontSize: 14, color: Color(0xFF696969)),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            _buildButton(
              _showOverlay
                  ? 'performance overlay: on'
                  : 'performance overlay: off',
              _showOverlay,
              () => setState(() => _showOverlay = !_showOverlay),
            ),
            _buildButton(
              'recolor',
              false,
              () => setState(() => _tint = (_tint + 1) % _tints.length),
            ),
            _buildButton('capture toImage', false, _captureBoundary),
          ],
        ),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 16,
          children: <Widget>[
            RepaintBoundary(
              key: _boundaryKey,
              child: Container(
                width: 160,
                height: 96,
                color: _tints[_tint],
                alignment: Alignment.center,
                child: Text(
                  'boundary #$_tint',
                  style: const TextStyle(
                    fontSize: 16,
                    color: Color(0xFF000000),
                  ),
                ),
              ),
            ),
            if (capture != null)
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                spacing: 4,
                children: <Widget>[
                  RawImage(image: capture, width: 160, height: 96),
                  Text(
                    '${capture.width} x ${capture.height} px',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFF2F4F4F),
                    ),
                  ),
                ],
              )
            else
              const Text(
                'no capture yet',
                style: TextStyle(fontSize: 12, color: Color(0xFF2F4F4F)),
              ),
          ],
        ),
      ],
    );
    return Stack(
      children: <Widget>[
        page,
        if (_showOverlay)
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: PerformanceOverlay.allEnabled(),
          ),
      ],
    );
  }

  Widget _buildButton(String label, bool enabled, VoidCallback onTap) {
    return CounterTapButton(
      label: label,
      onTap: onTap,
      background: enabled ? const Color(0xFF9FC5E8) : const Color(0xFFDCE3ED),
      foreground: const Color(0xFF000000),
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}
