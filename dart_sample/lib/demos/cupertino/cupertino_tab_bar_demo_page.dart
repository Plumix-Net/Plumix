import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoTabBarDemoPage extends StatefulWidget {
  const CupertinoTabBarDemoPage({super.key});

  @override
  State<CupertinoTabBarDemoPage> createState() =>
      _CupertinoTabBarDemoPageState();
}

class _CupertinoTabBarDemoPageState extends State<CupertinoTabBarDemoPage> {
  static const List<String> _titles = <String>['Home', 'Favorites', 'Profile'];

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
        Text('CupertinoTabScaffold', style: TextStyle(fontSize: 20, color: label)),
        Text(
          'Tap a destination to probe lazy tab bodies, retained state, active '
          'icons, and blur.',
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
            child: CupertinoTabScaffold(
              backgroundColor: CupertinoColors.secondarySystemBackground,
              tabBar: CupertinoTabBar(
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
              ),
              tabBuilder: (BuildContext context, int index) {
                return Center(
                  child: Text(
                    'Selected: ${_titles[index]}',
                    style: TextStyle(fontSize: 18, color: label),
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }
}
