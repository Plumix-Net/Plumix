import 'package:material_ui/material_ui.dart';

class NavigationDrawerDemoPage extends StatefulWidget {
  const NavigationDrawerDemoPage({super.key});

  @override
  State<NavigationDrawerDemoPage> createState() =>
      _NavigationDrawerDemoPageState();
}

class _NavigationDrawerDemoPageState extends State<NavigationDrawerDemoPage> {
  int? _selectedIndex;
  bool _thirdEnabled = true;
  bool _useThemeOverrides = false;
  bool _useWidgetOverrides = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData ambientTheme = Theme.of(context);
    final ThemeData pageTheme = ambientTheme.copyWith(
      navigationDrawerTheme: _useThemeOverrides
          ? const NavigationDrawerThemeData(
              backgroundColor: Color(0xFFF3E5F5),
              indicatorColor: Color(0xFFB2DFDB),
              tileHeight: 60,
              indicatorSize: Size(270, 48),
              labelTextStyle: WidgetStatePropertyAll<TextStyle?>(
                TextStyle(color: Color(0xFF4A148C), fontSize: 13),
              ),
              iconTheme: WidgetStatePropertyAll<IconThemeData?>(
                IconThemeData(color: Color(0xFF00695C), size: 22),
              ),
            )
          : const NavigationDrawerThemeData(),
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'NavigationDrawer + NavigationDrawerDestination',
            style: TextStyle(fontSize: 20),
          ),
          const Text(
            'Header/footer slots, custom children, destination indexing, selection, disabled state, and theme precedence.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _controlButton(
                _useThemeOverrides ? 'Theme on' : 'Theme off',
                () => setState(() => _useThemeOverrides = !_useThemeOverrides),
              ),
              _controlButton(
                _useWidgetOverrides ? 'Widget on' : 'Widget off',
                () =>
                    setState(() => _useWidgetOverrides = !_useWidgetOverrides),
              ),
              _controlButton(
                _thirdEnabled ? 'Disable third' : 'Enable third',
                () => setState(() => _thirdEnabled = !_thirdEnabled),
              ),
              _controlButton(
                _selectedIndex != null ? 'Clear selection' : 'Select first',
                () => setState(
                  () => _selectedIndex = _selectedIndex != null ? null : 0,
                ),
              ),
            ],
          ),
          Text(
            _selectedIndex != null
                ? 'Selected destination: ${_selectedIndex! + 1}'
                : 'Selected destination: none',
          ),
          Expanded(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 16,
              children: <Widget>[
                NavigationDrawer(
                  selectedIndex: _selectedIndex,
                  onDestinationSelected: (int index) =>
                      setState(() => _selectedIndex = index),
                  backgroundColor: _useWidgetOverrides
                      ? const Color(0xFFFFF8E1)
                      : null,
                  indicatorColor: _useWidgetOverrides
                      ? const Color(0xFFFFCC80)
                      : null,
                  tilePadding: EdgeInsets.symmetric(
                    horizontal: _useWidgetOverrides ? 18 : 12,
                  ),
                  header: const Padding(
                    padding: EdgeInsets.fromLTRB(28, 20, 16, 12),
                    child: Text('Destinations', style: TextStyle(fontSize: 16)),
                  ),
                  footer: const Padding(
                    padding: EdgeInsets.fromLTRB(28, 12, 16, 20),
                    child: Text(
                      'Navigation footer',
                      style: TextStyle(fontSize: 12, color: Colors.black54),
                    ),
                  ),
                  children: <Widget>[
                    const Padding(
                      padding: EdgeInsets.symmetric(
                        horizontal: 28,
                        vertical: 8,
                      ),
                      child: Text(
                        'Primary',
                        style: TextStyle(fontSize: 12, color: Colors.black54),
                      ),
                    ),
                    const NavigationDrawerDestination(
                      icon: Icon(Icons.star_outline),
                      selectedIcon: Icon(Icons.star),
                      label: Text('Favorites'),
                    ),
                    const NavigationDrawerDestination(
                      icon: Icon(Icons.info_outline),
                      label: Text('Explore'),
                    ),
                    const Divider(indent: 28, endIndent: 28),
                    NavigationDrawerDestination(
                      icon: const Icon(Icons.menu),
                      label: const Text('Downloads'),
                      enabled: _thirdEnabled,
                    ),
                  ],
                ),
                const Expanded(
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      color: Color(0xFFF7F2FA),
                      borderRadius: BorderRadius.all(Radius.circular(12)),
                    ),
                    child: Center(
                      child: Text(
                        'The drawer keeps destination indices independent from custom children.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: Colors.black54),
                      ),
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

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(onPressed: onPressed, child: Text(label));
  }
}
