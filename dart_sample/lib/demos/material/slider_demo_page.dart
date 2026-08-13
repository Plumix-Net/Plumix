import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

class SliderDemoPage extends StatefulWidget {
  const SliderDemoPage({super.key});

  @override
  State<SliderDemoPage> createState() => _SliderDemoPageState();
}

class _SliderDemoPageState extends State<SliderDemoPage> {
  bool _enabled = true;
  bool _discrete = false;
  bool _useThemeOverrides = false;
  bool _useWidgetColorOverride = false;
  bool _showSecondaryTrack = true;
  bool _useSecondaryColorOverride = false;
  bool _useMaterial3 = true;
  bool _year2023 = true;
  bool _tapOnly = false;
  bool _customShape = false;
  double _value = 0.35;
  double _secondaryTrackValue = 0.7;
  String _status = 'idle';

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData themedData = baseTheme.copyWith(
      useMaterial3: _useMaterial3,
      sliderTheme: SliderThemeData(
        activeTrackColor:
            _useThemeOverrides ? const Color(0xFF1565C0) : null,
        inactiveTrackColor:
            _useThemeOverrides ? const Color(0xFFC5CAE9) : null,
        thumbColor: _useThemeOverrides ? const Color(0xFF0D47A1) : null,
        disabledActiveTrackColor:
            _useThemeOverrides ? const Color(0x66212121) : null,
        disabledInactiveTrackColor:
            _useThemeOverrides ? const Color(0x1F212121) : null,
        disabledThumbColor:
            _useThemeOverrides ? const Color(0x66212121) : null,
        trackHeight: _useThemeOverrides ? 6 : null,
        thumbShape: _customShape ? const _DemoSliderThumbShape() : null,
      ),
    );

    return Theme(
      data: themedData,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'Slider baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const SizedBox(height: 8),
          const Text(
            'Continuous/discrete value mapping, drag/tap/keyboard updates, M2/M3 defaults, and theme/widget color precedence.',
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
                label: _showSecondaryTrack ? 'Secondary on' : 'Secondary off',
                onTap: () =>
                    setState(() => _showSecondaryTrack = !_showSecondaryTrack),
                width: 132,
                background: const Color(0xFFE8F6EE),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: '-',
                onTap: () => setState(() => _value = math.max(0, _value - 0.1)),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: '+',
                onTap: () => setState(() => _value = math.min(1, _value + 0.1)),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'value=${_value.toStringAsFixed(2)}, secondary=${_resolveSecondaryLabel()}, status=$_status',
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xFF607D8B),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _useSecondaryColorOverride
                    ? 'Secondary color on'
                    : 'Secondary color off',
                onTap: () => setState(
                  () =>
                      _useSecondaryColorOverride = !_useSecondaryColorOverride,
                ),
                width: 164,
                background: const Color(0xFFE9F0FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'Sec -',
                onTap: () => setState(
                  () => _secondaryTrackValue = math.max(
                    0,
                    _secondaryTrackValue - 0.1,
                  ),
                ),
                width: 56,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: 'Sec +',
                onTap: () => setState(
                  () => _secondaryTrackValue = math.min(
                    1,
                    _secondaryTrackValue + 0.1,
                  ),
                ),
                width: 56,
                background: const Color(0xFFFFF3E0),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _year2023 ? '2023 look' : '2024 look',
                onTap: () => setState(() => _year2023 = !_year2023),
                width: 96,
                background: const Color(0xFFEAF6F7),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _tapOnly ? 'Tap only' : 'Tap + slide',
                onTap: () => setState(() => _tapOnly = !_tapOnly),
                width: 104,
                background: const Color(0xFFF0E8FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _customShape ? 'Custom thumb' : 'Default thumb',
                onTap: () => setState(() => _customShape = !_customShape),
                width: 112,
                background: const Color(0xFFE8F6EE),
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
          Directionality(textDirection: textDirection, child: _buildSlider()),
        ],
      ),
    );
  }

  Widget _buildSlider() {
    return Slider(
      value: _value,
      min: 0,
      max: 1,
      divisions: _discrete ? 5 : null,
      label: '${(_value * 100).round()}',
      secondaryTrackValue: _showSecondaryTrack ? _secondaryTrackValue : null,
      activeColor: _useWidgetColorOverride ? const Color(0xFFB71C1C) : null,
      inactiveColor: _useWidgetColorOverride ? const Color(0xFFFFCDD2) : null,
      secondaryActiveColor: _useSecondaryColorOverride
          ? const Color(0xFF1B5E20)
          : null,
      thumbColor: _useWidgetColorOverride ? const Color(0xFF880E4F) : null,
      allowedInteraction: _tapOnly
          ? SliderInteraction.tapOnly
          : SliderInteraction.tapAndSlide,
      showValueIndicator: ShowValueIndicator.onlyForDiscrete,
      year2023: _year2023,
      onChanged: _enabled ? _handleValueChanged : null,
      onChangeStart: (double value) =>
          setState(() => _status = 'start ${value.toStringAsFixed(2)}'),
      onChangeEnd: (double value) =>
          setState(() => _status = 'end ${value.toStringAsFixed(2)}'),
      semanticFormatterCallback: (double value) =>
          '${(value * 100).round()} percent',
    );
  }

  void _handleValueChanged(double value) {
    setState(() {
      _value = value;
      _status = 'change ${value.toStringAsFixed(2)}';
    });
  }

  String _resolveSecondaryLabel() {
    return _showSecondaryTrack
        ? _secondaryTrackValue.toStringAsFixed(2)
        : 'off';
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

class _DemoSliderThumbShape extends SliderComponentShape {
  const _DemoSliderThumbShape();

  @override
  Size getPreferredSize(bool isEnabled, bool isDiscrete) => const Size(20, 20);

  @override
  void paint(
    PaintingContext context,
    Offset center, {
    required Animation<double> activationAnimation,
    required Animation<double> enableAnimation,
    required bool isDiscrete,
    required TextPainter labelPainter,
    required RenderBox parentBox,
    required SliderThemeData sliderTheme,
    required TextDirection textDirection,
    required double value,
    required double textScaleFactor,
    required Size sizeWithOverflow,
  }) {
    final Color color = enableAnimation.value >= 0.5
        ? sliderTheme.thumbColor ?? Colors.blue
        : sliderTheme.disabledThumbColor ?? Colors.grey;
    context.canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromCenter(center: center, width: 20, height: 20),
        const Radius.circular(5),
      ),
      Paint()..color = color,
    );
  }
}
