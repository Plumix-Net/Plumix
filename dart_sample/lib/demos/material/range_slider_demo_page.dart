import 'dart:math' as math;

import 'package:flutter/material.dart';

class RangeSliderDemoPage extends StatefulWidget {
  const RangeSliderDemoPage({super.key});

  @override
  State<RangeSliderDemoPage> createState() => _RangeSliderDemoPageState();
}

class _RangeSliderDemoPageState extends State<RangeSliderDemoPage> {
  bool _enabled = true;
  bool _discrete = false;
  bool _useThemeOverrides = false;
  bool _useWidgetColorOverride = false;
  bool _useMaterial3 = true;
  RangeValues _values = const RangeValues(0.2, 0.7);
  String _status = 'idle';

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData themedData = baseTheme.copyWith(
      useMaterial3: _useMaterial3,
      sliderTheme: _useThemeOverrides
          ? const SliderThemeData(
              activeTrackColor: Color(0xFF2E7D32),
              inactiveTrackColor: Color(0xFFC8E6C9),
              thumbColor: Color(0xFF1B5E20),
              disabledActiveTrackColor: Color(0x66212121),
              disabledInactiveTrackColor: Color(0x1F212121),
              disabledThumbColor: Color(0x66212121),
              trackHeight: 6,
            )
          : const SliderThemeData(),
    );

    return Theme(
      data: themedData,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'RangeSlider baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const SizedBox(height: 8),
          const Text(
            'Two-thumb range mapping, continuous/discrete updates, drag/tap/keyboard flow, and M2/M3 theme/widget color precedence.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _enabled ? 'Enabled' : 'Disabled',
                onTap: () => setState(() => _enabled = !_enabled),
                width: 96,
                background: const Color(0xFFE9F0FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _discrete ? 'Discrete' : 'Continuous',
                onTap: () => setState(() => _discrete = !_discrete),
                width: 112,
                background: const Color(0xFFE8F5E9),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 76,
                background: const Color(0xFFFFF8E1),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useThemeOverrides ? 'Theme on' : 'Theme off',
                onTap: () =>
                    setState(() => _useThemeOverrides = !_useThemeOverrides),
                width: 112,
                background: const Color(0xFFEAF6F7),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _useWidgetColorOverride ? 'Widget on' : 'Widget off',
                onTap: () => setState(
                  () => _useWidgetColorOverride = !_useWidgetColorOverride,
                ),
                width: 118,
                background: const Color(0xFFF0E8FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'S-',
                onTap: () => _nudgeStart(-0.1),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'S+',
                onTap: () => _nudgeStart(0.1),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'E-',
                onTap: () => _nudgeEnd(-0.1),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'E+',
                onTap: () => _nudgeEnd(0.1),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'start=${_values.start.toStringAsFixed(2)}, end=${_values.end.toStringAsFixed(2)}, status=$_status',
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xFF607D8B),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: <Widget>[
                  _buildPreviewCard(
                    title: 'LTR',
                    subtitle: 'Left-to-right mapping and keyboard direction',
                    textDirection: TextDirection.ltr,
                  ),
                  const SizedBox(height: 14),
                  _buildPreviewCard(
                    title: 'RTL',
                    subtitle: 'Right-to-left mapping and keyboard direction',
                    textDirection: TextDirection.rtl,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPreviewCard({
    required String title,
    required String subtitle,
    required TextDirection textDirection,
  }) {
    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(fontSize: 14, color: Colors.black),
          ),
          const SizedBox(height: 4),
          Text(
            subtitle,
            style: const TextStyle(fontSize: 12, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 8),
          Directionality(
            textDirection: textDirection,
            child: _buildRangeSlider(),
          ),
        ],
      ),
    );
  }

  Widget _buildRangeSlider() {
    return RangeSlider(
      values: _values,
      min: 0,
      max: 1,
      divisions: _discrete ? 5 : null,
      activeColor: _useWidgetColorOverride ? const Color(0xFFB71C1C) : null,
      inactiveColor: _useWidgetColorOverride ? const Color(0xFFFFCDD2) : null,
      onChanged: _enabled ? _handleValuesChanged : null,
      onChangeStart: (RangeValues values) => setState(
        () =>
            _status = 'start ${values.start.toStringAsFixed(2)}-${values.end.toStringAsFixed(2)}',
      ),
      onChangeEnd: (RangeValues values) => setState(
        () =>
            _status = 'end ${values.start.toStringAsFixed(2)}-${values.end.toStringAsFixed(2)}',
      ),
      semanticFormatterCallback: (double value) => '${(value * 100).round()}%',
    );
  }

  void _handleValuesChanged(RangeValues values) {
    setState(() {
      _values = values;
      _status =
          'change ${values.start.toStringAsFixed(2)}-${values.end.toStringAsFixed(2)}';
    });
  }

  void _nudgeStart(double delta) {
    setState(() {
      final double nextStart = math.min(
        _values.end,
        math.max(0, _values.start + delta),
      );
      _values = RangeValues(nextStart, _values.end);
      _status =
          'manual ${_values.start.toStringAsFixed(2)}-${_values.end.toStringAsFixed(2)}';
    });
  }

  void _nudgeEnd(double delta) {
    setState(() {
      final double nextEnd = math.max(
        _values.start,
        math.min(1, _values.end + delta),
      );
      _values = RangeValues(_values.start, nextEnd);
      _status =
          'manual ${_values.start.toStringAsFixed(2)}-${_values.end.toStringAsFixed(2)}';
    });
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onTap,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}
