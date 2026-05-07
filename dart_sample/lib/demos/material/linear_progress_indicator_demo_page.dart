import 'dart:math' as math;

import 'package:flutter/material.dart';

class LinearProgressIndicatorDemoPage extends StatefulWidget {
  const LinearProgressIndicatorDemoPage({super.key});

  @override
  State<LinearProgressIndicatorDemoPage> createState() =>
      _LinearProgressIndicatorDemoPageState();
}

class _LinearProgressIndicatorDemoPageState
    extends State<LinearProgressIndicatorDemoPage> {
  bool _useMaterial3 = true;
  bool _useYear2023 = true;
  bool _determinate = true;
  bool _useThemeOverrides = false;
  bool _useWidgetOverrides = false;
  bool _useValueColorOverride = false;
  double _progress = 0.35;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData pageTheme = baseTheme.copyWith(
      useMaterial3: _useMaterial3,
      progressIndicatorTheme: _useThemeOverrides
          ? const ProgressIndicatorThemeData(
              color: Color(0xFF1565C0),
              linearTrackColor: Color(0xFFC5CAE9),
              linearMinHeight: 6,
              borderRadius: BorderRadius.all(Radius.circular(3)),
              stopIndicatorColor: Color(0xFF0D47A1),
              stopIndicatorRadius: 3,
              trackGap: 6,
              year2023: _useYear2023,
            )
          : ProgressIndicatorThemeData(year2023: _useYear2023),
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'LinearProgressIndicator baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const SizedBox(height: 8),
          const Text(
            'Determinate/indeterminate behavior, M2/M3 defaults, year2023 toggle, theme/widget precedence, valueColor priority, track-gap/stop-dot styling, and RTL paint direction.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 80,
                background: const Color(0xFFE9F0FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useYear2023 ? '2023' : '2024',
                onTap: () => setState(() => _useYear2023 = !_useYear2023),
                width: 82,
                background: const Color(0xFFFFF8E1),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _determinate ? 'Determinate' : 'Indeterminate',
                onTap: () => setState(() => _determinate = !_determinate),
                width: 132,
                background: const Color(0xFFE8F5E9),
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
                label: _useWidgetOverrides ? 'Widget on' : 'Widget off',
                onTap: () =>
                    setState(() => _useWidgetOverrides = !_useWidgetOverrides),
                width: 118,
                background: const Color(0xFFF0E8FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useValueColorOverride
                    ? 'valueColor on'
                    : 'valueColor off',
                onTap: () => setState(
                  () => _useValueColorOverride = !_useValueColorOverride,
                ),
                width: 126,
                background: const Color(0xFFE4F7E8),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: '-',
                onTap: () =>
                    setState(() => _progress = math.max(0, _progress - 0.1)),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: '+',
                onTap: () =>
                    setState(() => _progress = math.min(1, _progress + 0.1)),
                width: 42,
                background: const Color(0xFFFFF3E0),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'value=${_progress.toStringAsFixed(2)}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xFF607D8B),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'useMaterial3=${_useMaterial3 ? "true" : "false"}, '
            'year2023=${_useYear2023 ? "true" : "false"}, '
            'determinate=${_determinate ? "true" : "false"}, '
            'theme=${_useThemeOverrides ? "true" : "false"}, '
            'widget=${_useWidgetOverrides ? "true" : "false"}, '
            'valueColor=${_useValueColorOverride ? "true" : "false"}',
            style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: <Widget>[
                  _buildPreviewCard(
                    title: 'LTR',
                    subtitle: 'Left-to-right fill and indeterminate movement',
                    indicator: _buildIndicator(),
                  ),
                  const SizedBox(height: 14),
                  _buildPreviewCard(
                    title: 'RTL',
                    subtitle: 'Right-to-left fill and indeterminate movement',
                    indicator: Directionality(
                      textDirection: TextDirection.rtl,
                      child: _buildIndicator(),
                    ),
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
    required Widget indicator,
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
          SizedBox(
            height: 28,
            child: Align(alignment: Alignment.center, child: indicator),
          ),
        ],
      ),
    );
  }

  Widget _buildIndicator() {
    if (_useWidgetOverrides) {
      return LinearProgressIndicator(
        value: _determinate ? _progress : null,
        color: const Color(0xFFB71C1C),
        backgroundColor: const Color(0xFFFFCDD2),
        minHeight: 8,
        borderRadius: const BorderRadius.all(Radius.circular(4)),
        stopIndicatorColor: const Color(0xFF880E4F),
        stopIndicatorRadius: 3.5,
        trackGap: 8,
        valueColor: _useValueColorOverride
            ? const AlwaysStoppedAnimation<Color>(Color(0xFF1B5E20))
            : null,
        year2023: _useYear2023,
        semanticsLabel: 'Widget override progress',
      );
    }

    return LinearProgressIndicator(
      value: _determinate ? _progress : null,
      valueColor: _useValueColorOverride
          ? const AlwaysStoppedAnimation<Color>(Color(0xFF1B5E20))
          : null,
      year2023: _useYear2023,
      semanticsLabel: 'Baseline progress',
    );
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
