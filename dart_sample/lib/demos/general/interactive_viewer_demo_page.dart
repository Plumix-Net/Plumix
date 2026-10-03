import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class InteractiveViewerDemoPage extends StatefulWidget {
  const InteractiveViewerDemoPage({super.key});

  @override
  State<InteractiveViewerDemoPage> createState() =>
      _InteractiveViewerDemoPageState();
}

class _InteractiveViewerDemoPageState extends State<InteractiveViewerDemoPage> {
  static const int _tileColumns = 6;
  static const int _tileRows = 4;
  static const double _tileSize = 100.0;
  static const int _rowCount = 20;
  static const double _rowHeight = 40.0;

  static const List<PanAxis> _panAxes = <PanAxis>[
    PanAxis.free,
    PanAxis.aligned,
    PanAxis.horizontal,
    PanAxis.vertical,
  ];

  static const List<Color> _tileColors = <Color>[
    Color(0xFF1565C0),
    Color(0xFF2E7D32),
    Color(0xFFF57C00),
    Color(0xFF6750A4),
  ];

  final TransformationController _controller = TransformationController();
  int _panAxisIndex = 0;
  bool _infiniteBoundary = false;
  bool _builderMode = false;
  String _lastEvent = 'none';

  @override
  void initState() {
    super.initState();
    _controller.addListener(_handleTransformation);
  }

  @override
  void dispose() {
    _controller.removeListener(_handleTransformation);
    _controller.dispose();
    super.dispose();
  }

  void _handleTransformation() => setState(() {});

  @override
  Widget build(BuildContext context) {
    final Matrix4 matrix = _controller.value;
    final translation = matrix.getTranslation();
    final PanAxis panAxis = _panAxes[_panAxisIndex];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'InteractiveViewer',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Drag to pan, pinch, scroll the mouse wheel or use a trackpad to '
          'zoom. Flings keep moving with inertia and slide along the boundary.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Reset',
              onTap: () => _controller.value = Matrix4.identity(),
            ),
            _buildButton(
              label: 'PanAxis: ${panAxis.name}',
              onTap: () {
                setState(() {
                  _panAxisIndex = (_panAxisIndex + 1) % _panAxes.length;
                });
              },
            ),
            _buildButton(
              label: _infiniteBoundary
                  ? 'Boundary: infinite'
                  : 'Boundary: 40',
              onTap: () {
                setState(() {
                  _infiniteBoundary = !_infiniteBoundary;
                });
              },
            ),
            _buildButton(
              label: _builderMode ? 'Mode: builder' : 'Mode: child',
              onTap: () {
                setState(() {
                  _builderMode = !_builderMode;
                  _controller.value = Matrix4.identity();
                });
              },
            ),
          ],
        ),
        Text(
          'scale=${matrix.getMaxScaleOnAxis().toStringAsFixed(2)}, '
          'translation=(${translation.x.toStringAsFixed(1)}, '
          '${translation.y.toStringAsFixed(1)}), last=$_lastEvent',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Center(
          child: Container(
            width: 360,
            height: 240,
            color: const Color(0xFFF3F6FA),
            child: _builderMode
                ? _buildBuilderViewer(panAxis)
                : _buildChildViewer(panAxis),
          ),
        ),
      ],
    );
  }

  Widget _buildChildViewer(PanAxis panAxis) {
    final List<Widget> rows = <Widget>[];
    for (int row = 0; row < _tileRows; row++) {
      final List<Widget> tiles = <Widget>[];
      for (int column = 0; column < _tileColumns; column++) {
        tiles.add(
          Container(
            width: _tileSize,
            height: _tileSize,
            color: _tileColors[(row + column) % _tileColors.length],
            alignment: Alignment.center,
            child: Text(
              '$row,$column',
              style: const TextStyle(fontSize: 14, color: Colors.white),
            ),
          ),
        );
      }
      rows.add(Row(mainAxisSize: MainAxisSize.min, children: tiles));
    }

    return InteractiveViewer(
      transformationController: _controller,
      constrained: false,
      panAxis: panAxis,
      boundaryMargin: _infiniteBoundary
          ? const EdgeInsets.all(double.infinity)
          : const EdgeInsets.all(40.0),
      minScale: 0.5,
      maxScale: 4.0,
      onInteractionStart: (_) => setState(() => _lastEvent = 'start'),
      onInteractionEnd: (_) => setState(() => _lastEvent = 'end'),
      child: Column(mainAxisSize: MainAxisSize.min, children: rows),
    );
  }

  Widget _buildBuilderViewer(PanAxis panAxis) {
    return InteractiveViewer.builder(
      transformationController: _controller,
      panAxis: panAxis,
      scaleEnabled: false,
      boundaryMargin: const EdgeInsets.all(double.infinity),
      onInteractionStart: (_) => setState(() => _lastEvent = 'start'),
      onInteractionEnd: (_) => setState(() => _lastEvent = 'end'),
      builder: (BuildContext context, viewport) {
        final double top = math.min(
          viewport.point0.y,
          math.min(viewport.point1.y, math.min(viewport.point2.y, viewport.point3.y)),
        );
        final double bottom = math.max(
          viewport.point0.y,
          math.max(viewport.point1.y, math.max(viewport.point2.y, viewport.point3.y)),
        );
        final List<Widget> children = <Widget>[];
        for (int i = 0; i < _rowCount; i++) {
          final double rowTop = i * _rowHeight;
          final double rowBottom = rowTop + _rowHeight;
          final bool visible = rowBottom > top && rowTop < bottom;
          children.add(
            Container(
              width: 360,
              height: _rowHeight,
              color: visible
                  ? const Color(0xFF2E7D32)
                  : const Color(0xFFC62828),
              alignment: Alignment.center,
              child: Text(
                visible ? 'row $i (built visible)' : 'row $i (built hidden)',
                style: const TextStyle(fontSize: 12, color: Colors.white),
              ),
            ),
          );
        }
        return Column(mainAxisSize: MainAxisSize.min, children: children);
      },
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
