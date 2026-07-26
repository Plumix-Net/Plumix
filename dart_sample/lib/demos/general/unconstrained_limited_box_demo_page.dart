import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class UnconstrainedLimitedBoxDemoPage extends StatefulWidget {
  const UnconstrainedLimitedBoxDemoPage({super.key});

  @override
  State<UnconstrainedLimitedBoxDemoPage> createState() =>
      _UnconstrainedLimitedBoxDemoPageState();
}

class _UnconstrainedLimitedBoxDemoPageState
    extends State<UnconstrainedLimitedBoxDemoPage> {
  Axis? _constrainedAxis;
  double _maxWidth = 120;
  double _maxHeight = 64;
  bool _clipTransformOverflow = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ConstraintsTransformBox + UnconstrainedBox',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'UnconstrainedBox delegates axis removal to ConstraintsTransformBox; '
          'the direct probe removes maxWidth and toggles clipping.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Axis none',
              onTap: () => _setConstrainedAxis(null),
              width: 94,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Axis H',
              onTap: () => _setConstrainedAxis(Axis.horizontal),
              width: 86,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Axis V',
              onTap: () => _setConstrainedAxis(Axis.vertical),
              width: 86,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'maxW 80',
              onTap: () => _setMaxWidth(80),
              width: 86,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'maxW 120',
              onTap: () => _setMaxWidth(120),
              width: 96,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'maxW 170',
              onTap: () => _setMaxWidth(170),
              width: 96,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'maxH 44',
              onTap: () => _setMaxHeight(44),
              width: 86,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'maxH 64',
              onTap: () => _setMaxHeight(64),
              width: 96,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'maxH 88',
              onTap: () => _setMaxHeight(88),
              width: 96,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'axis=${_axisLabel(_constrainedAxis)}, '
          'maxWidth=${_maxWidth.toStringAsFixed(0)}, '
          'maxHeight=${_maxHeight.toStringAsFixed(0)}, '
          'transformClip=$_clipTransformOverflow',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        _buildButton(
          label: _clipTransformOverflow
              ? 'Transform clip: antiAlias'
              : 'Transform clip: none',
          onTap: () {
            setState(() {
              _clipTransformOverflow = !_clipTransformOverflow;
            });
          },
          width: 180,
          background: const Color(0xFFE8DFF5),
        ),
        Container(
          width: 260,
          height: 304,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'Bounded parent: LimitedBox ignores maxWidth/maxHeight.',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 60,
                color: Colors.white,
                padding: const EdgeInsets.all(6),
                child: Center(
                  child: LimitedBox(
                    maxWidth: _maxWidth,
                    maxHeight: _maxHeight,
                    child: _buildProbeCard(),
                  ),
                ),
              ),
              const Text(
                'Inside UnconstrainedBox: unbounded axes use LimitedBox max values.',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 92,
                color: Colors.white,
                padding: const EdgeInsets.all(6),
                child: Center(
                  child: UnconstrainedBox(
                    alignment: Alignment.center,
                    constrainedAxis: _constrainedAxis,
                    child: LimitedBox(
                      maxWidth: _maxWidth,
                      maxHeight: _maxHeight,
                      child: _buildProbeCard(),
                    ),
                  ),
                ),
              ),
              const Text(
                'Direct ConstraintsTransformBox: maxWidth removed.',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 58,
                color: Colors.white,
                padding: const EdgeInsets.all(6),
                child: ConstraintsTransformBox(
                  constraintsTransform:
                      ConstraintsTransformBox.maxWidthUnconstrained,
                  textDirection: TextDirection.ltr,
                  alignment: AlignmentDirectional.centerStart,
                  clipBehavior: _clipTransformOverflow
                      ? Clip.antiAlias
                      : Clip.none,
                  child: Container(
                    width: 280,
                    height: 42,
                    color: const Color(0xFFFFD7A8),
                    alignment: Alignment.center,
                    child: const Text(
                      'natural width 280',
                      style: TextStyle(fontSize: 11, color: Colors.black),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildProbeCard() {
    return Container(
      width: 190,
      height: 86,
      decoration: BoxDecoration(
        color: const Color(0xFFCCE3FF),
        border: Border.all(color: const Color(0xFF1D3557), width: 2),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Center(
        child: Text(
          'child 190x86\nlimited to ${_maxWidth.toStringAsFixed(0)}x${_maxHeight.toStringAsFixed(0)}',
          style: const TextStyle(fontSize: 11, color: Colors.black),
          textAlign: TextAlign.center,
        ),
      ),
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

  void _setConstrainedAxis(Axis? value) {
    setState(() {
      _constrainedAxis = value;
    });
  }

  void _setMaxWidth(double value) {
    setState(() {
      _maxWidth = value;
    });
  }

  void _setMaxHeight(double value) {
    setState(() {
      _maxHeight = value;
    });
  }

  static String _axisLabel(Axis? axis) {
    if (axis == Axis.horizontal) {
      return 'horizontal';
    }

    if (axis == Axis.vertical) {
      return 'vertical';
    }

    return 'none';
  }
}
