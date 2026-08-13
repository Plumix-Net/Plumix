import 'package:flutter/material.dart';

class TabsDemoPage extends StatefulWidget {
  const TabsDemoPage({super.key});

  @override
  State<TabsDemoPage> createState() => _TabsDemoPageState();
}

class _TabsDemoPageState extends State<TabsDemoPage> {
  bool _useMaterial3 = true;
  bool _isScrollable = false;
  bool _useThemeOverrides = false;
  bool _isPrimary = true;
  bool _useElasticIndicator = true;
  bool _useCenterAlignment = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData ambient = Theme.of(context);
    ThemeData pageTheme = ThemeData(
      colorScheme: ambient.colorScheme,
      textTheme: ambient.textTheme,
      useMaterial3: _useMaterial3,
    );
    if (_useThemeOverrides) {
      pageTheme = pageTheme.copyWith(
        tabBarTheme: const TabBarThemeData(
          indicatorColor: Color(0xFF00695C),
          dividerColor: Color(0xFF80CBC4),
          labelColor: Color(0xFF00695C),
          unselectedLabelColor: Color(0xFF455A64),
          indicatorSize: TabBarIndicatorSize.tab,
        ),
      );
    }

    final TabAlignment alignment = _useCenterAlignment
        ? TabAlignment.center
        : _isScrollable
        ? (_useMaterial3 ? TabAlignment.startOffset : TabAlignment.start)
        : TabAlignment.fill;
    final TabIndicatorAnimation indicatorAnimation = _useElasticIndicator
        ? TabIndicatorAnimation.elastic
        : TabIndicatorAnimation.linear;

    const List<Widget> tabs = <Widget>[
      Tab(text: 'HOME', icon: Icon(Icons.star_outline)),
      Tab(text: 'EXPLORE', icon: Icon(Icons.info_outline)),
      Tab(text: 'SAVED', icon: Icon(Icons.check)),
      Tab(text: 'MORE', icon: Icon(Icons.menu)),
    ];

    return Theme(
      data: pageTheme,
      child: DefaultTabController(
        length: 4,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: <Widget>[
                _controlButton(
                  _useMaterial3 ? 'Material 3' : 'Material 2',
                  () => setState(() => _useMaterial3 = !_useMaterial3),
                ),
                _controlButton(
                  _isScrollable ? 'Scrollable' : 'Fill',
                  () => setState(() => _isScrollable = !_isScrollable),
                ),
                _controlButton(
                  _useThemeOverrides ? 'Theme on' : 'Theme off',
                  () =>
                      setState(() => _useThemeOverrides = !_useThemeOverrides),
                ),
                _controlButton(
                  _isPrimary ? 'Primary' : 'Secondary',
                  () => setState(() => _isPrimary = !_isPrimary),
                ),
                _controlButton(
                  _useElasticIndicator ? 'Elastic' : 'Linear',
                  () => setState(
                    () => _useElasticIndicator = !_useElasticIndicator,
                  ),
                ),
                _controlButton(
                  _useCenterAlignment ? 'Center' : 'Default align',
                  () =>
                      setState(() => _useCenterAlignment = !_useCenterAlignment),
                ),
              ],
            ),
            const SizedBox(height: 10),
            const Text(
              'Tap tabs or swipe pages; the indicator and labels share one TabController.',
              style: TextStyle(fontSize: 13, color: Color(0x8A000000)),
            ),
            const SizedBox(height: 10),
            Center(
              child: TabPageSelector(
                indicatorSize: _useThemeOverrides ? 16 : 12,
                color: _useThemeOverrides ? const Color(0xFFB2DFDB) : null,
                selectedColor: _useThemeOverrides
                    ? const Color(0xFF00695C)
                    : null,
                borderStyle: _useThemeOverrides ? BorderStyle.none : null,
              ),
            ),
            const SizedBox(height: 10),
            const Text(
              'Custom UnderlineTabIndicator: 6px inset, rounded, 5px weight.',
              style: TextStyle(fontSize: 13, color: Color(0x8A000000)),
            ),
            const SizedBox(height: 10),
            const TabBar(
              indicator: UnderlineTabIndicator(
                borderRadius: BorderRadius.all(Radius.circular(5)),
                borderSide: BorderSide(width: 5, color: Color(0xFF6750A4)),
                insets: EdgeInsets.symmetric(horizontal: 6),
              ),
              indicatorSize: TabBarIndicatorSize.label,
              tabs: <Widget>[
                Tab(text: 'ONE'),
                Tab(text: 'TWO'),
                Tab(text: 'THREE'),
                Tab(text: 'FOUR'),
              ],
            ),
            const SizedBox(height: 10),
            Expanded(
              child: Scaffold(
                appBar: AppBar(
                  title: const Text('Tabs preview'),
                  automaticallyImplyLeading: false,
                  bottom: _isPrimary
                      ? TabBar(
                          isScrollable: _isScrollable,
                          tabAlignment: alignment,
                          indicatorAnimation: indicatorAnimation,
                          tabs: tabs,
                        )
                      : TabBar.secondary(
                          isScrollable: _isScrollable,
                          tabAlignment: alignment,
                          indicatorAnimation: indicatorAnimation,
                          tabs: tabs,
                        ),
                ),
                body: const TabBarView(
                  children: <Widget>[
                    _TabPage(
                      title: 'HOME',
                      subtitle: 'Primary filled tab layout',
                      color: Color(0xFFE8DEF8),
                    ),
                    _TabPage(
                      title: 'EXPLORE',
                      subtitle: 'Swipe keeps indicator animation synchronized',
                      color: Color(0xFFD0BCFF),
                    ),
                    _TabPage(
                      title: 'SAVED',
                      subtitle: 'Selected semantics follow the active page',
                      color: Color(0xFFB2DFDB),
                    ),
                    _TabPage(
                      title: 'MORE',
                      subtitle:
                          'Scrollable and themed paths use the same controller',
                      color: Color(0xFFFFD8E4),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  static Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
        textStyle: const TextStyle(fontSize: 12),
      ),
      child: Text(label),
    );
  }
}

class _TabPage extends StatelessWidget {
  const _TabPage({
    required this.title,
    required this.subtitle,
    required this.color,
  });

  final String title;
  final String subtitle;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: color,
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(title, style: const TextStyle(fontSize: 22)),
            const SizedBox(height: 8),
            Text(
              subtitle,
              style: const TextStyle(fontSize: 13, color: Color(0x8A000000)),
            ),
          ],
        ),
      ),
    );
  }
}
