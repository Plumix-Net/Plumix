import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoTabBarDemoPage extends StatefulWidget {
  const CupertinoTabBarDemoPage({super.key});

  @override
  State<CupertinoTabBarDemoPage> createState() =>
      _CupertinoTabBarDemoPageState();
}

class _CupertinoTabBarDemoPageState extends State<CupertinoTabBarDemoPage> {
  static const List<String> _titles = <String>['Home', 'Favorites', 'Profile'];

  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    final Color secondaryLabel = CupertinoDynamicColor.resolve(
      CupertinoColors.secondaryLabel,
      context,
    );
    final Color panel = CupertinoDynamicColor.resolve(
      CupertinoColors.secondarySystemBackground,
      context,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        Text('CupertinoTabBar', style: TextStyle(fontSize: 20, color: label)),
        Text(
          'Tap a destination to probe selection, active icons, labels, and '
          'the translucent blur.',
          style: TextStyle(fontSize: 14, color: secondaryLabel),
        ),
        ClipRRect(
          borderRadius: BorderRadius.circular(14),
          child: Container(
            height: 260,
            decoration: BoxDecoration(
              color: panel,
              borderRadius: BorderRadius.circular(14),
            ),
            child: Column(
              children: <Widget>[
                Expanded(
                  child: Center(
                    child: Text(
                      'Selected: ${_titles[_currentIndex]}',
                      style: TextStyle(fontSize: 18, color: label),
                    ),
                  ),
                ),
                CupertinoTabBar(
                  items: const <BottomNavigationBarItem>[
                    BottomNavigationBarItem(
                      icon: Icon(CupertinoIcons.home),
                      label: 'Home',
                    ),
                    BottomNavigationBarItem(
                      icon: Icon(CupertinoIcons.heart),
                      activeIcon: Icon(CupertinoIcons.heart_fill),
                      label: 'Favorites',
                    ),
                    BottomNavigationBarItem(
                      icon: Icon(CupertinoIcons.person),
                      activeIcon: Icon(CupertinoIcons.person_fill),
                      label: 'Profile',
                    ),
                  ],
                  currentIndex: _currentIndex,
                  onTap: (int index) => setState(() => _currentIndex = index),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
