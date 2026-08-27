import 'package:material_ui/material_ui.dart';

class BottomNavigationBarDemoPage extends StatefulWidget {
  const BottomNavigationBarDemoPage({super.key});

  @override
  State<BottomNavigationBarDemoPage> createState() =>
      _BottomNavigationBarDemoPageState();
}

class _BottomNavigationBarDemoPageState
    extends State<BottomNavigationBarDemoPage> {
  int _currentIndex = 0;
  BottomNavigationBarType? _type;
  BottomNavigationBarLandscapeLayout _landscapeLayout =
      BottomNavigationBarLandscapeLayout.spread;
  bool _showSelectedLabels = true;
  bool _showUnselectedLabels = true;
  bool _customColors = false;
  bool _customIconThemes = false;
  bool _legacyColorScheme = true;
  bool _enableFeedback = true;
  bool _themed = false;
  int _tapCount = 0;

  @override
  Widget build(BuildContext context) {
    Widget bar = _buildBar();
    if (_themed) {
      bar = BottomNavigationBarTheme(
        data: BottomNavigationBarThemeData(
          backgroundColor: const Color(0xFFE8DEF8),
          elevation: 12.0,
          selectedItemColor: const Color(0xFF6750A4),
          unselectedItemColor: const Color(0xFF7A757F),
          selectedLabelStyle: const TextStyle(
            fontSize: 15.0,
            fontWeight: FontWeight.bold,
          ),
          unselectedLabelStyle: const TextStyle(fontSize: 12.0),
          mouseCursor: WidgetStateProperty.resolveWith<MouseCursor?>(
            (Set<WidgetState> states) =>
                states.contains(WidgetState.selected)
                    ? SystemMouseCursors.grab
                    : SystemMouseCursors.click,
          ),
        ),
        child: bar,
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'BottomNavigationBar',
          style: TextStyle(fontSize: 20),
        ),
        const Text(
          'Fixed/shifting types with the animated flex and radial background splash, landscape spread/centered/linear layouts, selected and unselected label visibility, item and label-style colors under both color schemes, theme precedence, feedback, and per-state mouse cursors.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(onPressed: _cycleType, child: Text(_typeLabel())),
            TextButton(
              onPressed: _cycleLandscapeLayout,
              child: Text('Landscape: ${_landscapeLayout.name}'),
            ),
            TextButton(
              onPressed: () => setState(() => _themed = !_themed),
              child: Text(_themed ? 'Theme on' : 'Theme off'),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () =>
                  setState(() => _showSelectedLabels = !_showSelectedLabels),
              child: Text(
                _showSelectedLabels
                    ? 'Selected labels on'
                    : 'Selected labels off',
              ),
            ),
            TextButton(
              onPressed: () => setState(
                () => _showUnselectedLabels = !_showUnselectedLabels,
              ),
              child: Text(
                _showUnselectedLabels
                    ? 'Unselected labels on'
                    : 'Unselected labels off',
              ),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _customColors = !_customColors),
              child: Text(_customColors ? 'Item colors on' : 'Item colors off'),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _customIconThemes = !_customIconThemes),
              child: Text(
                _customIconThemes ? 'Icon themes on' : 'Icon themes off',
              ),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _legacyColorScheme = !_legacyColorScheme),
              child: Text(
                _legacyColorScheme ? 'Legacy colors' : 'Label-style colors',
              ),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () =>
                  setState(() => _enableFeedback = !_enableFeedback),
              child: Text(_enableFeedback ? 'Feedback on' : 'Feedback off'),
            ),
          ],
        ),
        Text(
          'Selected index: $_currentIndex   |   taps: $_tapCount',
          style: const TextStyle(fontSize: 13),
        ),
        bar,
      ],
    );
  }

  Widget _buildBar() {
    return BottomNavigationBar(
      currentIndex: _currentIndex,
      onTap: (int index) {
        setState(() {
          _currentIndex = index;
          _tapCount += 1;
        });
      },
      type: _type,
      landscapeLayout: _landscapeLayout,
      showSelectedLabels: _showSelectedLabels,
      showUnselectedLabels: _showUnselectedLabels,
      useLegacyColorScheme: _legacyColorScheme,
      enableFeedback: _enableFeedback,
      selectedItemColor: _customColors ? const Color(0xFF1B5E20) : null,
      unselectedItemColor: _customColors ? const Color(0xFF8D6E63) : null,
      selectedLabelStyle: _customColors
          ? const TextStyle(color: Color(0xFFB3261E))
          : null,
      unselectedLabelStyle: _customColors
          ? const TextStyle(color: Color(0xFF4A6572))
          : null,
      selectedIconTheme: _customIconThemes
          ? const IconThemeData(color: Color(0xFF0B57D0), size: 30.0)
          : null,
      unselectedIconTheme: _customIconThemes
          ? const IconThemeData(color: Color(0xFF9AA0A6), size: 20.0)
          : null,
      items: const <BottomNavigationBarItem>[
        BottomNavigationBarItem(
          icon: Icon(Icons.star_outline),
          activeIcon: Icon(Icons.star),
          label: 'Favorites',
          tooltip: 'Saved items',
          backgroundColor: Color(0xFF1565C0),
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.menu),
          label: 'Browse',
          backgroundColor: Color(0xFF2E7D32),
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.info_outline),
          label: 'About',
          semanticsLabel: 'About this sample',
          backgroundColor: Color(0xFF6A1B9A),
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.check),
          label: 'Done',
          backgroundColor: Color(0xFFAD1457),
        ),
      ],
    );
  }

  String _typeLabel() {
    return switch (_type) {
      null => 'Type: automatic',
      BottomNavigationBarType.fixed => 'Type: fixed',
      BottomNavigationBarType.shifting => 'Type: shifting',
    };
  }

  void _cycleType() {
    setState(() {
      _type = switch (_type) {
        null => BottomNavigationBarType.fixed,
        BottomNavigationBarType.fixed => BottomNavigationBarType.shifting,
        BottomNavigationBarType.shifting => null,
      };
    });
  }

  void _cycleLandscapeLayout() {
    setState(() {
      _landscapeLayout = switch (_landscapeLayout) {
        BottomNavigationBarLandscapeLayout.spread =>
          BottomNavigationBarLandscapeLayout.centered,
        BottomNavigationBarLandscapeLayout.centered =>
          BottomNavigationBarLandscapeLayout.linear,
        BottomNavigationBarLandscapeLayout.linear =>
          BottomNavigationBarLandscapeLayout.spread,
      };
    });
  }
}
