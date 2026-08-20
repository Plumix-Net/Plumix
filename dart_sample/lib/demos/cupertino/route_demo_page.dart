import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class CupertinoRouteDemoPage extends StatefulWidget {
  const CupertinoRouteDemoPage({super.key});

  @override
  State<CupertinoRouteDemoPage> createState() => _CupertinoRouteDemoPageState();
}

class _CupertinoRouteDemoPageState extends State<CupertinoRouteDemoPage> {
  String _lastResult = 'none';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Cupertino routes',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Page transitions, modal popups, and a CupertinoTabView with '
          'independent history.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Text(
          'last result: $_lastResult',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
        _buildAction(
          label: 'Push standard CupertinoPageRoute',
          onTap: () => _pushPage(context, fullscreenDialog: false),
          background: const Color(0xFFE9F0FF),
        ),
        _buildAction(
          label: 'Push fullscreen CupertinoPageRoute',
          onTap: () => _pushPage(context, fullscreenDialog: true),
          background: const Color(0xFFEAE4FF),
        ),
        _buildAction(
          label: 'Open independent CupertinoTabView',
          onTap: () => _pushTabView(context),
          background: const Color(0xFFE8F0FE),
        ),
        _buildAction(
          label: 'Show Cupertino modal popup',
          onTap: () => _showPopup(context),
          background: const Color(0xFFE8F4E8),
        ),
      ],
    );
  }

  void _pushPage(BuildContext context, {required bool fullscreenDialog}) {
    final String routeKind = fullscreenDialog ? 'fullscreen' : 'standard';
    Navigator.of(context).push<String>(
      CupertinoPageRoute<String>(
        title: fullscreenDialog ? 'Fullscreen' : 'Details',
        fullscreenDialog: fullscreenDialog,
        builder: (BuildContext routeContext) => Center(
          child: Container(
            color: Colors.white,
            padding: const EdgeInsets.all(20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              spacing: 12,
              children: <Widget>[
                Text(
                  fullscreenDialog
                      ? 'Bottom-up fullscreen transition'
                      : 'Swipe from the leading edge to go back',
                  style: const TextStyle(fontSize: 16, color: Colors.black),
                ),
                _buildAction(
                  label: 'Pop with result',
                  onTap: () => _complete(routeContext, '$routeKind page'),
                  background: const Color(0xFFFFF3E0),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  static void _pushTabView(BuildContext context) {
    Navigator.of(context).push<void>(
      CupertinoPageRoute<void>(
        title: 'Tab history',
        builder: (_) => CupertinoTabView(
          defaultTitle: 'Tab root',
          builder: (BuildContext tabContext) => _buildTabPage(
            title: 'Independent tab root',
            actionLabel: 'Push a named route inside this tab',
            onTap: () => Navigator.of(tabContext).pushNamed('/details'),
          ),
          routes: <String, WidgetBuilder>{
            '/details': (BuildContext tabContext) => _buildTabPage(
              title: 'Named tab route',
              actionLabel: 'Pop back to the tab root',
              onTap: () => Navigator.of(tabContext).pop(),
            ),
          },
        ),
      ),
    );
  }

  static Widget _buildTabPage({
    required String title,
    required String actionLabel,
    required VoidCallback onTap,
  }) {
    return Center(
      child: Container(
        color: Colors.white,
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          spacing: 12,
          children: <Widget>[
            Text(
              title,
              style: const TextStyle(fontSize: 16, color: Colors.black),
            ),
            _buildAction(
              label: actionLabel,
              onTap: onTap,
              background: const Color(0xFFE8F0FE),
            ),
          ],
        ),
      ),
    );
  }

  void _showPopup(BuildContext context) {
    showCupertinoModalPopup<String>(
      context: context,
      builder: (BuildContext popupContext) => Container(
        color: Colors.white,
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          spacing: 12,
          children: <Widget>[
            const Text(
              'Spring-driven bottom popup',
              style: TextStyle(fontSize: 16, color: Colors.black),
            ),
            _buildAction(
              label: 'Close popup',
              onTap: () => _complete(popupContext, 'modal popup'),
              background: const Color(0xFFE0F2F1),
            ),
          ],
        ),
      ),
    );
  }

  void _complete(BuildContext context, String result) {
    setState(() => _lastResult = result);
    Navigator.of(context).pop<String>(result);
  }

  static Widget _buildAction({
    required String label,
    required VoidCallback onTap,
    required Color background,
  }) {
    return CounterTapButton(
      label: label,
      onTap: onTap,
      background: background,
      foreground: Colors.black,
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}
