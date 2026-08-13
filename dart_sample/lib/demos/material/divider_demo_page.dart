import 'package:material_ui/material_ui.dart';

class DividerDemoPage extends StatefulWidget {
  const DividerDemoPage({super.key});

  @override
  State<DividerDemoPage> createState() => _DividerDemoPageState();
}

class _DividerDemoPageState extends State<DividerDemoPage> {
  bool _useMaterial3 = true;
  bool _useThemeOverrides = false;
  bool _useWidgetOverrides = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData pageTheme = baseTheme.copyWith(
      useMaterial3: _useMaterial3,
      dividerTheme: _useThemeOverrides
          ? const DividerThemeData(
              color: Color(0xFF00695C),
              space: 28,
              thickness: 3,
              indent: 24,
              endIndent: 12,
              radius: BorderRadius.only(
                topLeft: Radius.circular(1),
                topRight: Radius.circular(4),
                bottomRight: Radius.circular(2),
                bottomLeft: Radius.circular(6),
              ),
            )
          : const DividerThemeData(),
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text('Divider baseline', style: TextStyle(fontSize: 20)),
          const SizedBox(height: 8),
          const Text(
            'M2/M3 tokens, directional indents, asymmetric theme radii, and widget overrides.',
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
                label: _useThemeOverrides ? 'Theme on' : 'Theme off',
                onTap: () =>
                    setState(() => _useThemeOverrides = !_useThemeOverrides),
                width: 112,
                background: const Color(0xFFEAF6F7),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useWidgetOverrides ? 'Widget on' : 'Widget off',
                onTap: () =>
                    setState(() => _useWidgetOverrides = !_useWidgetOverrides),
                width: 118,
                background: const Color(0xFFF0E8FF),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'useMaterial3=${_useMaterial3 ? "true" : "false"}, '
            'theme=${_useThemeOverrides ? "true" : "false"}, '
            'widget=${_useWidgetOverrides ? "true" : "false"}',
            style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: <Widget>[
                  _buildHorizontalPreview(),
                  const SizedBox(height: 14),
                  _buildVerticalPreview(),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHorizontalPreview() {
    final Widget dividerWidget = _useWidgetOverrides
        ? const Divider(
            height: 30,
            thickness: 5,
            indent: 18,
            endIndent: 30,
            color: Color(0xFF1565C0),
            radius: BorderRadius.all(Radius.circular(3)),
          )
        : const Divider();

    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'Horizontal Divider',
            style: TextStyle(fontSize: 14, color: Colors.black),
          ),
          const SizedBox(height: 8),
          Container(
            color: const Color(0xFFE3F2FD),
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            child: const Text(
              'Before divider',
              style: TextStyle(fontSize: 12, color: Color(0xFF0D47A1)),
            ),
          ),
          dividerWidget,
          Container(
            color: const Color(0xFFE8F5E9),
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            child: const Text(
              'After divider',
              style: TextStyle(fontSize: 12, color: Color(0xFF1B5E20)),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildVerticalPreview() {
    final Widget dividerWidget = _useWidgetOverrides
        ? const VerticalDivider(
            width: 30,
            thickness: 5,
            indent: 12,
            endIndent: 20,
            color: Color(0xFF6A1B9A),
            radius: BorderRadius.all(Radius.circular(3)),
          )
        : const VerticalDivider();

    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'Vertical Divider',
            style: TextStyle(fontSize: 14, color: Colors.black),
          ),
          const SizedBox(height: 8),
          SizedBox(
            height: 96,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Expanded(
                  child: Container(
                    color: const Color(0xFFFFF8E1),
                    alignment: Alignment.center,
                    child: const Text(
                      'Start',
                      style: TextStyle(fontSize: 12, color: Color(0xFF5D4037)),
                    ),
                  ),
                ),
                dividerWidget,
                Expanded(
                  child: Container(
                    color: const Color(0xFFFCE4EC),
                    alignment: Alignment.center,
                    child: const Text(
                      'End',
                      style: TextStyle(fontSize: 12, color: Color(0xFF880E4F)),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
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
