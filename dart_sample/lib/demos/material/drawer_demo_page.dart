import 'package:flutter/material.dart';

class DrawerDemoPage extends StatefulWidget {
  const DrawerDemoPage({super.key});

  @override
  State<DrawerDemoPage> createState() => _DrawerDemoPageState();
}

class _DrawerDemoPageState extends State<DrawerDemoPage> {
  bool _useMaterial3 = true;
  bool _useThemeOverrides = false;
  bool _useWidgetOverrides = false;
  bool _showEndDrawer = true;
  int _startOpens = 0;
  int _endOpens = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData pageTheme = baseTheme.copyWith(
      // ignore: deprecated_member_use
      useMaterial3: _useMaterial3,
      drawerTheme: _useThemeOverrides
          ? const DrawerThemeData(
              backgroundColor: Color(0xFFF3F7FC),
              scrimColor: Color(0x80123456),
              elevation: 10,
              shadowColor: Color(0xFF345E8B),
              width: 268,
            )
          : const DrawerThemeData(),
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'Drawer baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Scaffold drawer/endDrawer, theme precedence, and widget overrides.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 80,
                background: const Color(0xFFE9F0FF),
              ),
              _buildControlButton(
                label: _useThemeOverrides ? 'Theme on' : 'Theme off',
                onTap: () =>
                    setState(() => _useThemeOverrides = !_useThemeOverrides),
                width: 112,
                background: const Color(0xFFEAF6F7),
              ),
              _buildControlButton(
                label: _useWidgetOverrides ? 'Widget on' : 'Widget off',
                onTap: () =>
                    setState(() => _useWidgetOverrides = !_useWidgetOverrides),
                width: 118,
                background: const Color(0xFFF0E8FF),
              ),
            ],
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: _showEndDrawer ? 'End drawer on' : 'End drawer off',
                onTap: () => setState(() => _showEndDrawer = !_showEndDrawer),
                width: 138,
                background: const Color(0xFFEFF5E8),
              ),
              _buildControlButton(
                label: 'Reset',
                onTap: _reset,
                width: 88,
                background: const Color(0xFFF3E8D8),
              ),
            ],
          ),
          Text(
            'useMaterial3=${_useMaterial3 ? "true" : "false"}, '
            'theme=${_useThemeOverrides ? "true" : "false"}, '
            'widget=${_useWidgetOverrides ? "true" : "false"}, '
            'endDrawer=${_showEndDrawer ? "true" : "false"}, '
            'startOpens=$_startOpens, endOpens=$_endOpens',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: const Color(0xFFFDFEFF),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFD6DEEA), width: 1),
              ),
              child: Scaffold(
                drawerScrimColor: _useWidgetOverrides
                    ? const Color(0x99334455)
                    : null,
                drawer: _buildDrawerPanel(isStartDrawer: true),
                endDrawer: _showEndDrawer
                    ? _buildDrawerPanel(isStartDrawer: false)
                    : null,
                body: Builder(
                  builder: (BuildContext context) => _buildPreviewBody(context),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPreviewBody(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFFE8EEF7),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Text(
              'Use open/close controls to validate start/end drawer choreography and scrim behavior.',
              style: TextStyle(fontSize: 12, color: Color(0xFF30404D)),
            ),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: 'Open start',
                onTap: () => _openStartDrawer(context),
                width: 104,
                background: const Color(0xFFDDEBFF),
              ),
              _buildControlButton(
                label: 'Open end',
                onTap: _showEndDrawer ? () => _openEndDrawer(context) : null,
                width: 98,
                background: const Color(0xFFE6F2FF),
              ),
              _buildControlButton(
                label: 'Close all',
                onTap: () => _closeAllDrawers(context),
                width: 94,
                background: const Color(0xFFF7E9E3),
              ),
            ],
          ),
          const Expanded(
            child: Center(
              child: Text(
                'Drawer preview area',
                style: TextStyle(fontSize: 13, color: Color(0x99000000)),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDrawerPanel({required bool isStartDrawer}) {
    final String title = isStartDrawer ? 'Start drawer' : 'End drawer';
    final Color accent = isStartDrawer
        ? const Color(0xFF0D47A1)
        : const Color(0xFF4A148C);

    return Drawer(
      key: ValueKey<String>(isStartDrawer ? 'drawer-start' : 'drawer-end'),
      backgroundColor: _useWidgetOverrides
          ? (isStartDrawer ? const Color(0xFFEAF2FF) : const Color(0xFFF4ECFF))
          : null,
      elevation: _useWidgetOverrides ? (isStartDrawer ? 6 : 5) : null,
      shadowColor: _useWidgetOverrides
          ? (isStartDrawer ? const Color(0xFF305D8A) : const Color(0xFF5E3F86))
          : null,
      width: _useWidgetOverrides ? (isStartDrawer ? 236 : 228) : null,
      child: Builder(
        builder: (BuildContext context) {
          return Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 8,
              children: <Widget>[
                Text(title, style: TextStyle(fontSize: 16, color: accent)),
                const Text(
                  'Widget/theme/default precedence is visible through color, elevation, and width.',
                  style: TextStyle(fontSize: 12, color: Colors.black54),
                ),
                Text(
                  'DrawerController alignment=${DrawerController.of(context).alignment.name}',
                  style: const TextStyle(fontSize: 11, color: Colors.blueGrey),
                ),
                Row(
                  spacing: 8,
                  children: <Widget>[
                    _buildControlButton(
                      label: 'Close',
                      onTap: isStartDrawer
                          ? () => Scaffold.of(context).closeDrawer()
                          : () => Scaffold.of(context).closeEndDrawer(),
                      width: 84,
                      background: const Color(0xFFE9EEF5),
                    ),
                    _buildControlButton(
                      label: isStartDrawer ? 'Open end' : 'Open start',
                      onTap: isStartDrawer
                          ? (_showEndDrawer
                                ? () => _openEndDrawer(context)
                                : null)
                          : () => _openStartDrawer(context),
                      width: 96,
                      background: const Color(0xFFEFE8F8),
                    ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback? onTap,
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

  void _openStartDrawer(BuildContext context) {
    Scaffold.of(context).openDrawer();
    setState(() => _startOpens += 1);
  }

  void _openEndDrawer(BuildContext context) {
    Scaffold.of(context).openEndDrawer();
    setState(() => _endOpens += 1);
  }

  void _closeAllDrawers(BuildContext context) {
    final ScaffoldState state = Scaffold.of(context);
    state.closeDrawer();
    state.closeEndDrawer();
  }

  void _reset() {
    setState(() {
      _useMaterial3 = true;
      _useThemeOverrides = false;
      _useWidgetOverrides = false;
      _showEndDrawer = true;
      _startOpens = 0;
      _endOpens = 0;
    });
  }
}
