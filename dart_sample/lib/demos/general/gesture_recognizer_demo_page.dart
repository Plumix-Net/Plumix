import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';

/// Drives [HorizontalDragGestureRecognizer] and [LongPressGestureRecognizer] through
/// [RawGestureDetector] so the drag-start behavior, the accept threshold and the per-button
/// long-press callbacks are all visible.
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
  DragStartBehavior _dragStartBehavior = DragStartBehavior.start;
  bool _onlyAcceptDragOnThreshold = false;
  double _offset = 0.0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 16,
      children: <Widget>[
        const Text(
          'Drag and long-press recognizers',
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
    return RawGestureDetector(
      behavior: HitTestBehavior.opaque,
      gestures: <Type, GestureRecognizerFactory>{
        LongPressGestureRecognizer:
            GestureRecognizerFactoryWithHandlers<
              LongPressGestureRecognizer
            >(() => LongPressGestureRecognizer(), (
              LongPressGestureRecognizer instance,
            ) {
              instance.onLongPressDown = (LongPressDownDetails details) {
                _log(_longPressLog, 'primary down');
              };
              instance.onLongPressCancel = () {
                _log(_longPressLog, 'primary cancel');
              };
              instance.onLongPress = () {
                _log(_longPressLog, 'primary long press');
              };
              instance
                  .onLongPressMoveUpdate = (LongPressMoveUpdateDetails details) {
                _log(
                  _longPressLog,
                  'primary move ${details.offsetFromOrigin.dx.toStringAsFixed(0)}, '
                  '${details.offsetFromOrigin.dy.toStringAsFixed(0)}',
                );
              };
              instance.onLongPressEnd = (LongPressEndDetails details) {
                _log(_longPressLog, 'primary end');
              };
              instance.onSecondaryLongPressDown =
                  (LongPressDownDetails details) {
                    _log(_longPressLog, 'secondary down');
                  };
              instance.onSecondaryLongPress = () {
                _log(_longPressLog, 'secondary long press');
              };
              instance.onSecondaryLongPressEnd = (LongPressEndDetails details) {
                _log(_longPressLog, 'secondary end');
              };
              instance.onTertiaryLongPressDown =
                  (LongPressDownDetails details) {
                    _log(_longPressLog, 'tertiary down');
                  };
              instance.onTertiaryLongPress = () {
                _log(_longPressLog, 'tertiary long press');
              };
              instance.onTertiaryLongPressEnd = (LongPressEndDetails details) {
                _log(_longPressLog, 'tertiary end');
              };
            }),
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
