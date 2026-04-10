import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class AppBarTextStylesDemoPage extends StatefulWidget {
  const AppBarTextStylesDemoPage({super.key});

  @override
  State<AppBarTextStylesDemoPage> createState() =>
      _AppBarTextStylesDemoPageState();
}

class _AppBarTextStylesDemoPageState extends State<AppBarTextStylesDemoPage> {
  static const Color _appBarBackground = Color(0xFF1E3A5F);
  static const Color _foregroundWhite = Colors.white;
  static const Color _foregroundMint = Color(0xFFB8FFF1);
  static const TextStyle _themeTitleStyle = TextStyle(
    fontSize: 22,
    color: Color(0xFF90E0EF),
    fontWeight: FontWeight.bold,
  );
  static const TextStyle _themeToolbarStyle = TextStyle(
    fontSize: 14,
    color: Color(0xFFF4A261),
    fontWeight: FontWeight.w600,
  );
  static const TextStyle _widgetTitleStyle = TextStyle(
    fontSize: 19,
    color: Color(0xFF8E44AD),
    fontWeight: FontWeight.normal,
  );
  static const TextStyle _widgetToolbarStyle = TextStyle(
    fontSize: 16,
    color: Color(0xFFE63946),
    fontWeight: FontWeight.bold,
  );

  Color _foregroundColor = _foregroundWhite;
  bool _themeTitleStyleEnabled = true;
  bool _themeToolbarStyleEnabled = true;
  bool _widgetTitleOverrideEnabled = false;
  bool _widgetToolbarOverrideEnabled = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AppBar title/toolbar text styles',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Runtime probe for titleTextStyle and toolbarTextStyle precedence: widget override > appBarTheme > foreground fallback.',
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
              label: 'theme title off',
              onTap: () => _setThemeTitleStyleEnabled(false),
              width: 114,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'theme title on',
              onTap: () => _setThemeTitleStyleEnabled(true),
              width: 114,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'theme toolbar off',
              onTap: () => _setThemeToolbarStyleEnabled(false),
              width: 126,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'theme toolbar on',
              onTap: () => _setThemeToolbarStyleEnabled(true),
              width: 126,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'widget title off',
              onTap: () => _setWidgetTitleOverrideEnabled(false),
              width: 118,
              background: const Color(0xFFE4ECF7),
            ),
            _buildButton(
              label: 'widget title on',
              onTap: () => _setWidgetTitleOverrideEnabled(true),
              width: 118,
              background: const Color(0xFFE4ECF7),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'widget toolbar off',
              onTap: () => _setWidgetToolbarOverrideEnabled(false),
              width: 130,
              background: const Color(0xFFF5E8ED),
            ),
            _buildButton(
              label: 'widget toolbar on',
              onTap: () => _setWidgetToolbarOverrideEnabled(true),
              width: 130,
              background: const Color(0xFFF5E8ED),
            ),
          ],
        ),
        Text(
          'fg=${_colorLabel(_foregroundColor)}, themeTitle=${_onOff(_themeTitleStyleEnabled)}, themeToolbar=${_onOff(_themeToolbarStyleEnabled)}, widgetTitle=${_onOff(_widgetTitleOverrideEnabled)}, widgetToolbar=${_onOff(_widgetToolbarOverrideEnabled)}, expectedTitle=${_describeStyle(_resolveExpectedTitleStyle(), _foregroundColor)}, expectedActions=${_describeStyle(_resolveExpectedToolbarStyle(), _foregroundColor)}',
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
                'Default app bar reference (no text style overrides)',
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
          titleTextStyle: _themeTitleStyleEnabled ? _themeTitleStyle : null,
          toolbarTextStyle: _themeToolbarStyleEnabled
              ? _themeToolbarStyle
              : null,
        ),
      ),
      child: AppBar(
        title: const Text('Theme text styles'),
        titleTextStyle: _widgetTitleOverrideEnabled ? _widgetTitleStyle : null,
        toolbarTextStyle: _widgetToolbarOverrideEnabled
            ? _widgetToolbarStyle
            : null,
        actions: const <Widget>[Text('A1'), Text('A2')],
      ),
    );
  }

  Widget _buildDefaultReferencePreview() {
    return AppBar(
      title: const Text('Default text styles'),
      backgroundColor: _appBarBackground,
      foregroundColor: _foregroundColor,
      actions: const <Widget>[Text('A1'), Text('A2')],
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

  void _setThemeTitleStyleEnabled(bool value) {
    setState(() {
      _themeTitleStyleEnabled = value;
    });
  }

  void _setThemeToolbarStyleEnabled(bool value) {
    setState(() {
      _themeToolbarStyleEnabled = value;
    });
  }

  void _setWidgetTitleOverrideEnabled(bool value) {
    setState(() {
      _widgetTitleOverrideEnabled = value;
    });
  }

  void _setWidgetToolbarOverrideEnabled(bool value) {
    setState(() {
      _widgetToolbarOverrideEnabled = value;
    });
  }

  TextStyle? _resolveExpectedTitleStyle() {
    if (_widgetTitleOverrideEnabled) {
      return _widgetTitleStyle;
    }

    if (_themeTitleStyleEnabled) {
      return _themeTitleStyle;
    }

    return null;
  }

  TextStyle? _resolveExpectedToolbarStyle() {
    if (_widgetToolbarOverrideEnabled) {
      return _widgetToolbarStyle;
    }

    if (_themeToolbarStyleEnabled) {
      return _themeToolbarStyle;
    }

    return null;
  }

  static String _describeStyle(TextStyle? style, Color fallbackColor) {
    final Color color = style?.color ?? fallbackColor;
    final String sizeLabel = style?.fontSize?.toStringAsFixed(0) ?? 'base';
    return '${_colorLabel(color)}:$sizeLabel';
  }

  static String _onOff(bool value) => value ? 'on' : 'off';

  static String _colorLabel(Color color) {
    final int value = color.toARGB32();
    return '#${value.toRadixString(16).toUpperCase().padLeft(8, '0')}';
  }
}
