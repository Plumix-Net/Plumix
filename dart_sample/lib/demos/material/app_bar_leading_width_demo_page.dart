import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class AppBarLeadingWidthDemoPage extends StatefulWidget {
  const AppBarLeadingWidthDemoPage({super.key});

  @override
  State<AppBarLeadingWidthDemoPage> createState() =>
      _AppBarLeadingWidthDemoPageState();
}

class _AppBarLeadingWidthDemoPageState
    extends State<AppBarLeadingWidthDemoPage> {
  double _themeLeadingWidth = 88;
  bool _useWidgetOverride = false;
  double _widgetLeadingWidth = 120;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AppBar theme leadingWidth',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Runtime probe for AppBar leading slot width resolution: widget override beats appBarTheme, appBarTheme beats default 56.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'theme 56',
              onTap: () => _setThemeLeadingWidth(56),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'theme 88',
              onTap: () => _setThemeLeadingWidth(88),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'theme 120',
              onTap: () => _setThemeLeadingWidth(120),
              width: 102,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'override off',
              onTap: () => _setUseWidgetOverride(false),
              width: 108,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'override on',
              onTap: () => _setUseWidgetOverride(true),
              width: 108,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'widget 72',
              onTap: () => _setWidgetLeadingWidth(72),
              width: 92,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'widget 96',
              onTap: () => _setWidgetLeadingWidth(96),
              width: 92,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'widget 120',
              onTap: () => _setWidgetLeadingWidth(120),
              width: 102,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'themeLeadingWidth=${_themeLeadingWidth.toStringAsFixed(0)}, override=${_useWidgetOverride ? 'on' : 'off'}, widgetLeadingWidth=${_widgetLeadingWidth.toStringAsFixed(0)}, effective=${_effectiveLeadingWidthLabel()}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 300,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'Themed app bar preview',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              _buildThemedPreview(),
              const Text(
                'Default app bar reference (leadingWidth = 56)',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              _buildDefaultReferencePreview(),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildThemedPreview() {
    return Theme(
      data: ThemeData.light().copyWith(
        appBarTheme: AppBarTheme(
          backgroundColor: const Color(0xFF1E3A5F),
          foregroundColor: Colors.white,
          leadingWidth: _themeLeadingWidth,
        ),
      ),
      child: AppBar(
        title: const Text('Theme leading width'),
        leadingWidth: _useWidgetOverride ? _widgetLeadingWidth : null,
        leading: _buildLeadingProbe(),
        actions: const <Widget>[Icon(Icons.close)],
      ),
    );
  }

  Widget _buildDefaultReferencePreview() {
    return AppBar(
      title: const Text('Default leading width'),
      backgroundColor: const Color(0xFF1E3A5F),
      foregroundColor: Colors.white,
      leading: _buildLeadingProbe(),
      actions: const <Widget>[Icon(Icons.close)],
    );
  }

  static Widget _buildLeadingProbe() {
    return Container(
      width: 24,
      height: 24,
      color: const Color(0xFFFFB703),
      child: const Center(
        child: Icon(Icons.menu, size: 14, color: Colors.black),
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

  void _setThemeLeadingWidth(double value) {
    setState(() {
      _themeLeadingWidth = value;
    });
  }

  void _setUseWidgetOverride(bool value) {
    setState(() {
      _useWidgetOverride = value;
    });
  }

  void _setWidgetLeadingWidth(double value) {
    setState(() {
      _widgetLeadingWidth = value;
    });
  }

  String _effectiveLeadingWidthLabel() {
    final double effective = _useWidgetOverride
        ? _widgetLeadingWidth
        : _themeLeadingWidth;
    return effective.toStringAsFixed(0);
  }
}
