import 'package:flutter/material.dart';

class SelectionHandlesDemoPage extends StatefulWidget {
  const SelectionHandlesDemoPage({super.key});

  @override
  State<SelectionHandlesDemoPage> createState() =>
      _SelectionHandlesDemoPageState();
}

class _SelectionHandlesDemoPageState extends State<SelectionHandlesDemoPage> {
  static const double _lineTop = 96;
  static const double _lineHeight = 24;

  final LayerLink _startHandleLink = LayerLink();
  final LayerLink _endHandleLink = LayerLink();
  final LayerLink _toolbarLink = LayerLink();
  final TextEditingController _fieldController = TextEditingController(
    text: 'Long press this real text field, then drag either selection handle.',
  );

  SelectionOverlay? _overlay;
  double _startX = 48;
  double _endX = 232;
  bool _handlesVisible = false;
  bool _collapsed = false;

  @override
  void dispose() {
    _overlay?.dispose();
    _overlay = null;
    _fieldController.dispose();
    super.dispose();
  }

  double get _effectiveStartX => _collapsed ? _endX : _startX;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text(
          'SelectionOverlay + Material handles',
          style: TextStyle(fontSize: 20),
        ),
        const SizedBox(height: 12),
        const Text(
          'Drag either handle to move its endpoint. Collapsed mode keeps a single upward handle.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _fieldController,
          maxLines: 2,
          decoration: const InputDecoration(
            labelText: 'RenderEditable-backed handles',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: <Widget>[
            _controlButton(
              _handlesVisible ? 'Hide handles' : 'Show handles',
              _toggleHandles,
            ),
            const SizedBox(width: 8),
            _controlButton(_collapsed ? 'Ranged' : 'Collapsed', _toggleCollapsed),
            const SizedBox(width: 8),
            _controlButton('Reset', _resetEndpoints),
          ],
        ),
        const SizedBox(height: 12),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F2FA),
            child: Stack(
              clipBehavior: Clip.none,
              children: <Widget>[
                const Positioned(
                  left: 24,
                  top: _lineTop - _lineHeight,
                  right: 24,
                  child: Text(
                    'Drag the handles across this line',
                    style: TextStyle(fontSize: 18, color: Color(0xFF1D192B)),
                  ),
                ),
                Positioned(
                  left: _effectiveStartX,
                  top: _lineTop,
                  child: CompositedTransformTarget(
                    link: _startHandleLink,
                    child: const SizedBox.shrink(),
                  ),
                ),
                Positioned(
                  left: _endX,
                  top: _lineTop,
                  child: CompositedTransformTarget(
                    link: _endHandleLink,
                    child: const SizedBox.shrink(),
                  ),
                ),
                Positioned(
                  left: 24,
                  top: _lineTop + 24,
                  child: CompositedTransformTarget(
                    link: _toolbarLink,
                    child: const SizedBox.shrink(),
                  ),
                ),
                Positioned(
                  left: 24,
                  bottom: 18,
                  child: Text(
                    'startX=${_effectiveStartX.toStringAsFixed(0)}, '
                    'endX=${_endX.toStringAsFixed(0)}, '
                    'handles=${_handlesVisible ? 'on' : 'off'}',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFF6750A4),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  void _toggleHandles() {
    final SelectionOverlay overlay = _ensureOverlay();
    if (_handlesVisible) {
      overlay.hideHandles();
    } else {
      overlay.showHandles();
    }
    setState(() => _handlesVisible = !_handlesVisible);
  }

  void _toggleCollapsed() {
    setState(() => _collapsed = !_collapsed);
    _syncOverlay();
  }

  void _resetEndpoints() {
    setState(() {
      _startX = 48;
      _endX = 232;
    });
    _syncOverlay();
  }

  SelectionOverlay _ensureOverlay() {
    final SelectionOverlay? existing = _overlay;
    if (existing != null) {
      return existing;
    }

    final SelectionOverlay overlay = SelectionOverlay(
      context: context,
      startHandleType: TextSelectionHandleType.left,
      lineHeightAtStart: _lineHeight,
      endHandleType: TextSelectionHandleType.right,
      lineHeightAtEnd: _lineHeight,
      selectionEndpoints: _buildEndpoints(),
      selectionControls: materialTextSelectionHandleControls,
      selectionDelegate: null,
      clipboardStatus: null,
      startHandleLayerLink: _startHandleLink,
      endHandleLayerLink: _endHandleLink,
      toolbarLayerLink: _toolbarLink,
      onStartHandleDragUpdate: (DragUpdateDetails details) =>
          _moveStart(details.delta.dx),
      onEndHandleDragUpdate: (DragUpdateDetails details) =>
          _moveEnd(details.delta.dx),
    );
    _overlay = overlay;
    _syncOverlay();
    return overlay;
  }

  void _moveStart(double delta) {
    setState(() => _startX = (_startX + delta).clamp(24, _endX));
    _syncOverlay();
  }

  void _moveEnd(double delta) {
    setState(() => _endX = (_endX + delta).clamp(_effectiveStartX, 320));
    _syncOverlay();
  }

  void _syncOverlay() {
    final SelectionOverlay? overlay = _overlay;
    if (overlay == null) {
      return;
    }

    overlay
      ..startHandleType = _collapsed
          ? TextSelectionHandleType.collapsed
          : TextSelectionHandleType.left
      ..endHandleType = _collapsed
          ? TextSelectionHandleType.collapsed
          : TextSelectionHandleType.right
      ..selectionEndpoints = _buildEndpoints()
      ..markNeedsBuild();
  }

  List<TextSelectionPoint> _buildEndpoints() {
    return <TextSelectionPoint>[
      TextSelectionPoint(
        Offset(_effectiveStartX, _lineTop),
        TextDirection.ltr,
      ),
      TextSelectionPoint(Offset(_endX, _lineTop), TextDirection.ltr),
    ];
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }
}
