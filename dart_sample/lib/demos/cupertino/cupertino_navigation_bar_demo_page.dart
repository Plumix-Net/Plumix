import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class CupertinoNavigationBarDemoPage extends StatefulWidget {
  const CupertinoNavigationBarDemoPage({super.key});

  @override
  State<CupertinoNavigationBarDemoPage> createState() =>
      _CupertinoNavigationBarDemoPageState();
}

class _CupertinoNavigationBarDemoPageState extends State<CupertinoNavigationBarDemoPage> {
  bool _searchActive = false;

  @override
  Widget build(BuildContext context) {
    return CupertinoTheme(
      data: const CupertinoThemeData(),
      child: CupertinoPageScaffold(
        child: CustomScrollView(
          slivers: <Widget>[
            CupertinoSliverNavigationBar.search(
              searchField: const CupertinoSearchTextField(),
              largeTitle: const Text('Nav bars'),
              stretch: true,
              onSearchableBottomTap: (bool active) =>
                  setState(() => _searchActive = active),
            ),
            SliverToBoxAdapter(child: Builder(builder: _buildContent)),
          ],
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    final Color secondaryLabel = CupertinoDynamicColor.resolve(
      CupertinoColors.secondaryLabel,
      context,
    );

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10.0,
        children: <Widget>[
          Text(
            _searchActive
                ? 'Search is active — Cancel restores the collapsed bar.'
                : 'Scroll to collapse the large title, overscroll to stretch '
                      'it, or tap the field to search.',
            style: TextStyle(fontSize: 14.0, color: secondaryLabel),
          ),
          _buildAction(
            'Push detail page (auto middle + back label)',
            () => _pushDetail(context),
            const Color(0xFFE9F0FF),
          ),
          _buildAction(
            "Push page with a long title ('Back' fallback)",
            () => _pushLongTitle(context),
            const Color(0xFFEAE4FF),
          ),
          ...List<Widget>.generate(
            20,
            (int index) => Text(
              'Row ${index + 1}',
              style: TextStyle(fontSize: 14.0, color: label),
            ),
          ),
        ],
      ),
    );
  }

  static void _pushDetail(BuildContext context) {
    Navigator.of(context).push(
      CupertinoPageRoute<Object?>(
        title: 'Details',
        builder: (_) => CupertinoPageScaffold(
          navigationBar: const CupertinoNavigationBar(),
          child: Center(
            child: Builder(
              builder: (BuildContext routeContext) => Text(
                'The static bar implies its middle title and the back label '
                'from the route titles.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14.0,
                  color: CupertinoDynamicColor.resolve(
                    CupertinoColors.label,
                    routeContext,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  static void _pushLongTitle(BuildContext context) {
    Navigator.of(context).push(
      CupertinoPageRoute<Object?>(
        title: 'Extended configuration options',
        builder: (_) => CupertinoPageScaffold(
          child: CustomScrollView(
            slivers: <Widget>[
              const CupertinoSliverNavigationBar(),
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Builder(
                    builder: (BuildContext routeContext) => Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      spacing: 10.0,
                      children: <Widget>[
                        Text(
                          'This title is over 12 characters, so the next '
                          "page's back label falls back to 'Back'.",
                          style: TextStyle(
                            fontSize: 14.0,
                            color: CupertinoDynamicColor.resolve(
                              CupertinoColors.label,
                              routeContext,
                            ),
                          ),
                        ),
                        _buildAction(
                          "Push detail from here (shows 'Back')",
                          () => _pushDetail(routeContext),
                          const Color(0xFFE8F4E8),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static Widget _buildAction(String label, VoidCallback onTap, Color background) {
    return CounterTapButton(
      label: label,
      onTap: onTap,
      background: background,
      foreground: Colors.black,
      fontSize: 12.0,
      padding: const EdgeInsets.symmetric(horizontal: 10.0, vertical: 8.0),
    );
  }
}
