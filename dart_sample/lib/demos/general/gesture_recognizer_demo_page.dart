import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';

/// Drives [HorizontalDragGestureRecognizer], [LongPressGestureRecognizer] and
/// [ScaleGestureRecognizer] so the drag-start behavior, the accept threshold, the per-button
/// long-press callbacks and pinch/zoom/rotate are all visible.
class GestureRecognizerDemoPage extends StatefulWidget {
  const GestureRecognizerDemoPage({super.key});

  @override
  State<GestureRecognizerDemoPage> createState() =>
      _GestureRecognizerDemoPageState();
}

class _GestureRecognizerDemoPageState extends State<GestureRecognizerDemoPage> {
  static const int _maxLogLines = 8;

  final List<String> _dragLog = <String>[];
  final List<String> _longPressLog = <String>[];
  final List<String> _scaleLog = <String>[];
  DragStartBehavior _dragStartBehavior = DragStartBehavior.start;
  bool _onlyAcceptDragOnThreshold = false;
  bool _trackpadScrollCausesScale = false;
  double _offset = 0.0;
  double _scale = 1.0;
  double _baseScale = 1.0;
  double _rotation = 0.0;
  double _baseRotation = 0.0;
  Offset _translation = Offset.zero;
  Offset _baseTranslation = Offset.zero;
  Offset _startFocalPoint = Offset.zero;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 16,
      children: <Widget>[
        const Text(
          'Drag, long-press and scale recognizers',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'A drag accepts once the pointer travels past the device hit slop (18 logical pixels for '
          'touch, 1 for a mouse). DragStartBehavior decides whether that travelled distance is '
          'reported from the down position or from where the gesture won the arena.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        _buildSwitchRow(
          'DragStartBehavior.down (replay the pending offset)',
          _dragStartBehavior == DragStartBehavior.down,
          (bool value) => setState(() {
            _dragStartBehavior = value
                ? DragStartBehavior.down
                : DragStartBehavior.start;
          }),
        ),
        _buildSwitchRow(
          'onlyAcceptDragOnThreshold (hold the drag back after winning)',
          _onlyAcceptDragOnThreshold,
          (bool value) => setState(() => _onlyAcceptDragOnThreshold = value),
        ),
        _buildDragSurface(),
        _buildLog('Drag events', _dragLog),
        const Text(
          'The long press deadline is 500 ms. Each mouse button dispatches its own callback set; '
          'moving more than the touch slop before the deadline cancels it.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        _buildLongPressSurface(),
        _buildLog('Long-press events', _longPressLog),
        const Text(
          'The scale recognizer tracks every pointer at once: two fingers pinch and rotate, one '
          'finger pans, and a trackpad pan/zoom gesture counts as two pointers. It wins the arena '
          'once the span moves past the scale slop, the focal point past the pan slop, or the '
          'pan/zoom scale differs by more than 5%.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        _buildSwitchRow(
          'trackpadScrollCausesScale (a trackpad scroll zooms instead of panning)',
          _trackpadScrollCausesScale,
          (bool value) => setState(() => _trackpadScrollCausesScale = value),
        ),
        _buildScaleSurface(),
        _buildLog('Scale events', _scaleLog),
      ],
    );
  }

  Widget _buildSwitchRow(
    String label,
    bool value,
    ValueChanged<bool> onChanged,
  ) {
    return Row(
      spacing: 12,
      children: <Widget>[
        Switch(value: value, onChanged: onChanged),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontSize: 14, color: Colors.black),
          ),
        ),
      ],
    );
  }

  Widget _buildDragSurface() {
    return RawGestureDetector(
      behavior: HitTestBehavior.opaque,
      gestures: <Type, GestureRecognizerFactory>{
        HorizontalDragGestureRecognizer:
            GestureRecognizerFactoryWithHandlers<
              HorizontalDragGestureRecognizer
            >(() => HorizontalDragGestureRecognizer(), (
              HorizontalDragGestureRecognizer instance,
            ) {
              instance.dragStartBehavior = _dragStartBehavior;
              instance.onlyAcceptDragOnThreshold = _onlyAcceptDragOnThreshold;
              instance.onDown = (DragDownDetails details) {
                _log(_dragLog, 'down');
              };
              instance.onStart = (DragStartDetails details) {
                _log(
                  _dragLog,
                  'start at ${details.globalPosition.dx.toStringAsFixed(0)}',
                );
              };
              instance.onUpdate = (DragUpdateDetails details) {
                setState(() {
                  _offset = (_offset + (details.primaryDelta ?? 0.0)).clamp(
                    0.0,
                    220.0,
                  );
                  _appendLog(
                    _dragLog,
                    'update ${(details.primaryDelta ?? 0.0).toStringAsFixed(0)}',
                  );
                });
              };
              instance.onEnd = (DragEndDetails details) {
                _log(
                  _dragLog,
                  'end at ${(details.primaryVelocity ?? 0.0).toStringAsFixed(0)} px/s',
                );
              };
              instance.onCancel = () {
                _log(_dragLog, 'cancel');
              };
            }),
      },
      child: Container(
        height: 96,
        color: const Color(0xFFF4F7FA),
        padding: const EdgeInsets.all(8),
        child: Align(
          alignment: Alignment.centerLeft,
          child: Padding(
            padding: EdgeInsets.only(left: _offset),
            child: Container(
              width: 56,
              height: 56,
              color: const Color(0xFF31506F),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLongPressSurface() {
    // The long-press matrix straight off [GestureDetector]: it registers a single
    // [LongPressGestureRecognizer] and wires all 21 primary/secondary/tertiary callbacks.
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onLongPressDown: (LongPressDownDetails details) {
        _log(_longPressLog, 'primary down');
      },
      onLongPressCancel: () {
        _log(_longPressLog, 'primary cancel');
      },
      onLongPress: () {
        _log(_longPressLog, 'primary long press');
      },
      onLongPressMoveUpdate: (LongPressMoveUpdateDetails details) {
        _log(
          _longPressLog,
          'primary move ${details.offsetFromOrigin.dx.toStringAsFixed(0)}, '
          '${details.offsetFromOrigin.dy.toStringAsFixed(0)}',
        );
      },
      onLongPressEnd: (LongPressEndDetails details) {
        _log(_longPressLog, 'primary end');
      },
      onSecondaryLongPressDown: (LongPressDownDetails details) {
        _log(_longPressLog, 'secondary down');
      },
      onSecondaryLongPress: () {
        _log(_longPressLog, 'secondary long press');
      },
      onSecondaryLongPressEnd: (LongPressEndDetails details) {
        _log(_longPressLog, 'secondary end');
      },
      onTertiaryLongPressDown: (LongPressDownDetails details) {
        _log(_longPressLog, 'tertiary down');
      },
      onTertiaryLongPress: () {
        _log(_longPressLog, 'tertiary long press');
      },
      onTertiaryLongPressEnd: (LongPressEndDetails details) {
        _log(_longPressLog, 'tertiary end');
      },
      child: Container(
        height: 96,
        color: const Color(0xFFE7EDF6),
        child: const Center(
          child: Text(
            'Press and hold with any mouse button',
            style: TextStyle(fontSize: 14, color: Color(0xFF31506F)),
          ),
        ),
      ),
    );
  }

  Widget _buildScaleSurface() {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      trackpadScrollCausesScale: _trackpadScrollCausesScale,
      onScaleStart: (ScaleStartDetails details) {
        _baseScale = _scale;
        _baseRotation = _rotation;
        _baseTranslation = _translation;
        _startFocalPoint = details.focalPoint;
        _log(_scaleLog, 'start with ${details.pointerCount} pointer(s)');
      },
      onScaleUpdate: (ScaleUpdateDetails details) {
        setState(() {
          _scale = (_baseScale * details.scale).clamp(0.5, 4.0);
          _rotation = _baseRotation + details.rotation;
          final Offset moved = details.focalPoint - _startFocalPoint;
          _translation = Offset(
            (_baseTranslation.dx + moved.dx).clamp(-96.0, 96.0),
            (_baseTranslation.dy + moved.dy).clamp(-40.0, 40.0),
          );
          _appendLog(
            _scaleLog,
            'scale ${details.scale.toStringAsFixed(2)}, '
            'rotation ${details.rotation.toStringAsFixed(2)}, '
            '${details.pointerCount} pointer(s)',
          );
        });
      },
      onScaleEnd: (ScaleEndDetails details) {
        _log(
          _scaleLog,
          'end at ${details.scaleVelocity.toStringAsFixed(0)} px/s',
        );
      },
      child: Container(
        height: 176,
        color: const Color(0xFFF4F7FA),
        child: Center(
          child: Transform.translate(
            offset: _translation,
            child: Transform.rotate(
              angle: _rotation,
              child: Transform.scale(
                scale: _scale,
                child: Container(
                  width: 72,
                  height: 72,
                  color: const Color(0xFF31506F),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLog(String title, List<String> lines) {
    final List<Widget> children = <Widget>[
      Text(title, style: const TextStyle(fontSize: 14, color: Colors.black)),
    ];
    if (lines.isEmpty) {
      children.add(
        const Text(
          '(no events yet)',
          style: TextStyle(fontSize: 13, color: Colors.grey),
        ),
      );
    } else {
      for (final String line in lines) {
        children.add(
          Text(line, style: const TextStyle(fontSize: 13, color: Colors.grey)),
        );
      }
    }

    return Container(
      color: const Color(0xFFF7F7F7),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        spacing: 4,
        children: children,
      ),
    );
  }

  void _log(List<String> log, String message) {
    setState(() => _appendLog(log, message));
  }

  void _appendLog(List<String> log, String message) {
    log.add(message);
    if (log.length > _maxLogLines) {
      log.removeAt(0);
    }
  }
}
