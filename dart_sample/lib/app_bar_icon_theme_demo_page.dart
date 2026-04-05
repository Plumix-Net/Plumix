import 'package:flutter/material.dart';

import 'counter_widgets.dart';

class AppBarIconThemeDemoPage extends StatefulWidget {
  const AppBarIconThemeDemoPage({super.key});

  @override
  State<AppBarIconThemeDemoPage> createState() =>
      _AppBarIconThemeDemoPageState();
}

class _AppBarIconThemeDemoPageState extends State<AppBarIconThemeDemoPage> {
  static const Color _appBarBackground = Color(0xFF1E3A5F);
  static const Color _foregroundWhite = Colors.white;
  static const Color _foregroundMint = Color(0xFFB8FFF1);
  static const Color _themeIconColor = Color(0xFF2A9D8F);
  static const Color _themeActionsIconColor = Color(0xFFF4A261);
  static const Color _widgetIconColor = Color(0xFF8E44AD);
  static const Color _widgetActionsIconColor = Color(0xFFE63946);

  Color _foregroundColor = _foregroundWhite;
  bool _themeIconEnabled = true;
  bool _themeActionsIconEnabled = false;
  bool _widgetIconOverrideEnabled = false;
  bool _widgetActionsIconOverrideEnabled = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AppBar iconTheme/actionsIconTheme',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Runtime probe for icon-theme precedence: widget override > appBarTheme > iconTheme fallback > foreground fallback.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'fg white',
              onTap: () => _setForeground(_foregroundWhite),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'fg mint',
              onTap: () => _setForeground(_foregroundMint),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'theme icon off',
              onTap: () => _setThemeIconEnabled(false),
              width: 108,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'theme icon on',
              onTap: () => _setThemeIconEnabled(true),
              width: 108,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'theme actions off',
              onTap: () => _setThemeActionsIconEnabled(false),
              width: 118,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'theme actions on',
              onTap: () => _setThemeActionsIconEnabled(true),
              width: 118,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'widget icon off',
              onTap: () => _setWidgetIconOverrideEnabled(false),
              width: 114,
              background: const Color(0xFFE4ECF7),
            ),
            _buildButton(
              label: 'widget icon on',
              onTap: () => _setWidgetIconOverrideEnabled(true),
              width: 114,
              background: const Color(0xFFE4ECF7),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'widget actions off',
              onTap: () => _setWidgetActionsIconOverrideEnabled(false),
              width: 126,
              background: const Color(0xFFF5E8ED),
            ),
            _buildButton(
              label: 'widget actions on',
              onTap: () => _setWidgetActionsIconOverrideEnabled(true),
              width: 126,
              background: const Color(0xFFF5E8ED),
            ),
          ],
        ),
        Text(
          'fg=${_colorLabel(_foregroundColor)}, themeIcon=${_onOff(_themeIconEnabled)}, themeActions=${_onOff(_themeActionsIconEnabled)}, widgetIcon=${_onOff(_widgetIconOverrideEnabled)}, widgetActions=${_onOff(_widgetActionsIconOverrideEnabled)}, expectedLeading=${_colorLabel(_resolveExpectedLeadingColor())}, expectedActions=${_colorLabel(_resolveExpectedActionsColor())}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 320,
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
                'Default app bar reference (no icon theme overrides)',
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
          backgroundColor: _appBarBackground,
          foregroundColor: _foregroundColor,
          iconTheme: _themeIconEnabled
              ? const IconThemeData(color: _themeIconColor, size: 18)
              : null,
          actionsIconTheme: _themeActionsIconEnabled
              ? const IconThemeData(color: _themeActionsIconColor, size: 16)
              : null,
        ),
      ),
      child: AppBar(
        title: const Text('Theme icon chain'),
        iconTheme: _widgetIconOverrideEnabled
            ? const IconThemeData(color: _widgetIconColor, size: 20)
            : null,
        actionsIconTheme: _widgetActionsIconOverrideEnabled
            ? const IconThemeData(color: _widgetActionsIconColor, size: 22)
            : null,
        leading: const Icon(Icons.menu),
        actions: const <Widget>[Icon(Icons.close), Icon(Icons.info_outline)],
      ),
    );
  }

  Widget _buildDefaultReferencePreview() {
    return AppBar(
      title: const Text('Default icon chain'),
      backgroundColor: _appBarBackground,
      foregroundColor: _foregroundColor,
      leading: const Icon(Icons.menu),
      actions: const <Widget>[Icon(Icons.close), Icon(Icons.info_outline)],
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

  void _setForeground(Color value) {
    setState(() {
      _foregroundColor = value;
    });
  }

  void _setThemeIconEnabled(bool value) {
    setState(() {
      _themeIconEnabled = value;
    });
  }

  void _setThemeActionsIconEnabled(bool value) {
    setState(() {
      _themeActionsIconEnabled = value;
    });
  }

  void _setWidgetIconOverrideEnabled(bool value) {
    setState(() {
      _widgetIconOverrideEnabled = value;
    });
  }

  void _setWidgetActionsIconOverrideEnabled(bool value) {
    setState(() {
      _widgetActionsIconOverrideEnabled = value;
    });
  }

  Color _resolveExpectedLeadingColor() {
    if (_widgetIconOverrideEnabled) {
      return _widgetIconColor;
    }

    if (_themeIconEnabled) {
      return _themeIconColor;
    }

    return _foregroundColor;
  }

  Color _resolveExpectedActionsColor() {
    if (_widgetActionsIconOverrideEnabled) {
      return _widgetActionsIconColor;
    }

    if (_themeActionsIconEnabled) {
      return _themeActionsIconColor;
    }

    if (_widgetIconOverrideEnabled) {
      return _widgetIconColor;
    }

    if (_themeIconEnabled) {
      return _themeIconColor;
    }

    return _foregroundColor;
  }

  static String _onOff(bool value) => value ? 'on' : 'off';

  static String _colorLabel(Color color) {
    final int value = color.toARGB32();
    return '#${value.toRadixString(16).toUpperCase().padLeft(8, '0')}';
  }
}
