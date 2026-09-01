import 'package:material_ui/material_ui.dart';

class NavigationSurfacesDemoPage extends StatefulWidget {
  const NavigationSurfacesDemoPage({super.key});

  @override
  State<NavigationSurfacesDemoPage> createState() =>
      _NavigationSurfacesDemoPageState();
}

class _NavigationSurfacesDemoPageState
    extends State<NavigationSurfacesDemoPage> {
  int _selectedIndex = 0;
  bool _useMaterial3 = true;
  bool _extended = false;
  bool _useThemeOverrides = false;
  bool _useSeedScheme = false;
  NavigationDestinationLabelBehavior _barLabelBehavior =
      NavigationDestinationLabelBehavior.alwaysShow;
  NavigationRailLabelType _railLabelType = NavigationRailLabelType.all;

  @override
  Widget build(BuildContext context) {
    final ThemeData ambientTheme = Theme.of(context);
    final ColorScheme colorScheme = _useSeedScheme
        ? ColorScheme.fromSeed(seedColor: const Color(0xFF006495))
        : ambientTheme.colorScheme;
    ThemeData pageTheme = ThemeData(
      colorScheme: colorScheme,
      useMaterial3: _useMaterial3,
    );
    if (_useThemeOverrides) {
      pageTheme = pageTheme.copyWith(
        navigationBarTheme: const NavigationBarThemeData(
          backgroundColor: Color(0xFFE0F2F1),
          indicatorColor: Color(0xFF00695C),
          height: 76,
        ),
        navigationRailTheme: const NavigationRailThemeData(
          backgroundColor: Color(0xFFF3E5F5),
          indicatorColor: Color(0xFF6A1B9A),
          minWidth: 76,
          minExtendedWidth: 220,
        ),
      );
    }

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'NavigationBar + NavigationRail',
            style: TextStyle(fontSize: 20),
          ),
          const SizedBox(height: 14),
          const Text(
            'Seed-generated ColorScheme, Material 2021 typography, navigation defaults, '
            'theme precedence, and icon-scoped hover/press ripples.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 14),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _controlButton(
                _useMaterial3 ? 'Material 3' : 'Material 2',
                () => setState(() => _useMaterial3 = !_useMaterial3),
              ),
              _controlButton(
                _useSeedScheme ? 'Seed scheme' : 'Baseline scheme',
                () => setState(() => _useSeedScheme = !_useSeedScheme),
              ),
              _controlButton(
                _useThemeOverrides ? 'Theme on' : 'Theme off',
                () => setState(() => _useThemeOverrides = !_useThemeOverrides),
              ),
              _controlButton(
                _extended ? 'Rail extended' : 'Rail compact',
                () => setState(() => _extended = !_extended),
              ),
              _controlButton('Bar: ${_barLabelBehavior.name}', _cycleBarLabels),
              _controlButton('Rail: ${_railLabelType.name}', _cycleRailLabels),
            ],
          ),
          const SizedBox(height: 14),
          Text('Selected destination: ${_selectedIndex + 1}'),
          const SizedBox(height: 14),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _paletteChip(
                'primary',
                colorScheme.primary,
                colorScheme.onPrimary,
              ),
              _paletteChip(
                'secondary',
                colorScheme.secondary,
                colorScheme.onSecondary,
              ),
              _paletteChip(
                'tertiary',
                colorScheme.tertiary,
                colorScheme.onTertiary,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'titleMedium · ${pageTheme.textTheme.titleMedium?.fontSize?.toStringAsFixed(0)}px',
            style: pageTheme.textTheme.titleMedium,
          ),
          const SizedBox(height: 14),
          DecoratedBox(
            decoration: BoxDecoration(
              border: Border.all(color: const Color(0x33000000)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: NavigationBar(
              selectedIndex: _selectedIndex,
              onDestinationSelected: (int index) =>
                  setState(() => _selectedIndex = index),
              labelBehavior: _barLabelBehavior,
              destinations: const <Widget>[
                NavigationDestination(
                  icon: Icon(Icons.star_outline),
                  selectedIcon: Icon(Icons.star),
                  label: 'Favorites',
                ),
                NavigationDestination(
                  icon: Icon(Icons.info_outline),
                  label: 'Explore',
                ),
                NavigationDestination(
                  icon: Icon(Icons.menu),
                  label: 'Disabled',
                  enabled: false,
                ),
              ],
            ),
          ),
          const SizedBox(height: 14),
          SizedBox(
            height: 280,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                NavigationRail(
                  selectedIndex: _selectedIndex,
                  onDestinationSelected: (int index) =>
                      setState(() => _selectedIndex = index),
                  extended: _extended,
                  labelType: _extended
                      ? NavigationRailLabelType.none
                      : _railLabelType,
                  destinations: const <NavigationRailDestination>[
                    NavigationRailDestination(
                      icon: Icon(Icons.star_outline),
                      selectedIcon: Icon(Icons.star),
                      label: Text('Favorites'),
                    ),
                    NavigationRailDestination(
                      icon: Icon(Icons.info_outline),
                      label: Text('Explore'),
                    ),
                    NavigationRailDestination(
                      icon: Icon(Icons.menu),
                      label: Text('Disabled'),
                      disabled: true,
                    ),
                  ],
                ),
                const Expanded(
                  child: ColoredBox(
                    color: Color(0xFFF7F2FA),
                    child: Center(
                      child: Text(
                        'Rail content area',
                        style: TextStyle(color: Color(0x8A000000)),
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

  void _cycleBarLabels() {
    setState(() {
      _barLabelBehavior = switch (_barLabelBehavior) {
        NavigationDestinationLabelBehavior.alwaysShow =>
          NavigationDestinationLabelBehavior.onlyShowSelected,
        NavigationDestinationLabelBehavior.onlyShowSelected =>
          NavigationDestinationLabelBehavior.alwaysHide,
        NavigationDestinationLabelBehavior.alwaysHide =>
          NavigationDestinationLabelBehavior.alwaysShow,
      };
    });
  }

  void _cycleRailLabels() {
    setState(() {
      _railLabelType = switch (_railLabelType) {
        NavigationRailLabelType.all => NavigationRailLabelType.selected,
        NavigationRailLabelType.selected => NavigationRailLabelType.none,
        NavigationRailLabelType.none => NavigationRailLabelType.all,
      };
    });
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }

  Widget _paletteChip(String label, Color color, Color onColor) {
    return Container(
      width: 104,
      height: 48,
      alignment: Alignment.center,
      color: color,
      child: Text(label, style: TextStyle(fontSize: 11, color: onColor)),
    );
  }
}
