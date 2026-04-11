import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class AppBarActionsPaddingDemoPage extends StatefulWidget {
  const AppBarActionsPaddingDemoPage({super.key});

  @override
  State<AppBarActionsPaddingDemoPage> createState() =>
      _AppBarActionsPaddingDemoPageState();
}

class _AppBarActionsPaddingDemoPageState
    extends State<AppBarActionsPaddingDemoPage> {
  EdgeInsets _themeActionsPadding = const EdgeInsets.fromLTRB(6, 2, 6, 2);
  bool _useWidgetOverride = false;
  EdgeInsets _widgetActionsPadding = const EdgeInsets.fromLTRB(10, 4, 10, 4);

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AppBar theme actionsPadding',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Runtime probe for actions row padding resolution: widget override beats appBarTheme, appBarTheme beats default zero padding.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'theme 0',
              onTap: () => _setThemeActionsPadding(EdgeInsets.zero),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'theme 6/2',
              onTap: () => _setThemeActionsPadding(
                const EdgeInsets.fromLTRB(6, 2, 6, 2),
              ),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'theme 14/6',
              onTap: () => _setThemeActionsPadding(
                const EdgeInsets.fromLTRB(14, 6, 14, 6),
              ),
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
              label: 'widget 4/0',
              onTap: () => _setWidgetActionsPadding(
                const EdgeInsets.fromLTRB(4, 0, 4, 0),
              ),
              width: 92,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'widget 10/4',
              onTap: () => _setWidgetActionsPadding(
                const EdgeInsets.fromLTRB(10, 4, 10, 4),
              ),
              width: 92,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'widget 16/8',
              onTap: () => _setWidgetActionsPadding(
                const EdgeInsets.fromLTRB(16, 8, 16, 8),
              ),
              width: 102,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'theme=${_formatInsets(_themeActionsPadding)}, override=${_useWidgetOverride ? 'on' : 'off'}, widget=${_formatInsets(_widgetActionsPadding)}, effective=${_formatInsets(_effectiveActionsPadding())}',
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
                'Default app bar reference (actionsPadding = 0)',
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
          actionsPadding: _themeActionsPadding,
        ),
      ),
      child: AppBar(
        title: const Text('Theme actions padding'),
        actionsPadding: _useWidgetOverride ? _widgetActionsPadding : null,
        actions: <Widget>[
          _buildActionBadge(Icons.close),
          _buildActionBadge(Icons.menu),
        ],
      ),
    );
  }

  Widget _buildDefaultReferencePreview() {
    return AppBar(
      title: const Text('Default actions padding'),
      backgroundColor: const Color(0xFF1E3A5F),
      foregroundColor: Colors.white,
      actions: <Widget>[
        _buildActionBadge(Icons.close),
        _buildActionBadge(Icons.menu),
      ],
    );
  }

  static Widget _buildActionBadge(IconData icon) {
    return Container(
      width: 28,
      height: 22,
      color: const Color(0xFFFFB703),
      child: Center(child: Icon(icon, size: 14, color: Colors.black)),
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

  void _setThemeActionsPadding(EdgeInsets value) {
    setState(() {
      _themeActionsPadding = value;
    });
  }

  void _setUseWidgetOverride(bool value) {
    setState(() {
      _useWidgetOverride = value;
    });
  }

  void _setWidgetActionsPadding(EdgeInsets value) {
    setState(() {
      _widgetActionsPadding = value;
    });
  }

  EdgeInsets _effectiveActionsPadding() {
    return _useWidgetOverride ? _widgetActionsPadding : _themeActionsPadding;
  }

  static String _formatInsets(EdgeInsets insets) {
    return '${insets.left.toStringAsFixed(0)}/${insets.top.toStringAsFixed(0)}/${insets.right.toStringAsFixed(0)}/${insets.bottom.toStringAsFixed(0)}';
  }
}
