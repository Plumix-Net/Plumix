import 'package:flutter/material.dart';

class NavigationPopDemoPage extends StatefulWidget {
  const NavigationPopDemoPage({super.key});

  @override
  State<NavigationPopDemoPage> createState() => _NavigationPopDemoPageState();
}

class _NavigationPopDemoPageState extends State<NavigationPopDemoPage> {
  final GlobalKey<NavigatorState> _nestedNavigatorKey =
      GlobalKey<NavigatorState>();
  bool _canLeave = true;
  int _nestedPage = 1;
  String _status = 'No pop attempted';

  @override
  Widget build(BuildContext context) {
    return PopScope<Object?>(
      canPop: _canLeave,
      onPopInvokedWithResult: (bool didPop, Object? result) {
        if (mounted) {
          setState(() {
            _status = didPop ? 'Route popped' : 'Pop handled or blocked';
          });
        }
      },
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'PopScope + NavigatorPopHandler',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Push a nested page, then simulate a parent Back. The handler consumes it in the nested '
            'navigator. Nested routes use PageTransitionsTheme, including Android predictive-back '
            'peek/commit/cancel and iOS/macOS leading-edge swipe. Disable route pop to probe PopScope '
            'veto behavior.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: _toggleCanLeave,
                child: Text(
                  _canLeave ? 'Disable route pop' : 'Enable route pop',
                ),
              ),
              TextButton(
                onPressed: _pushNestedPage,
                child: const Text('Push nested page'),
              ),
              TextButton(
                onPressed: () =>
                    Navigator.of(context).maybePop<Object?>('demo-result'),
                child: const Text('Simulate parent Back'),
              ),
            ],
          ),
          Text(
            'Status: $_status',
            style: const TextStyle(color: Color(0xFF31506F)),
          ),
          Expanded(
            child: NavigatorPopHandler<Object?>(
              onPopWithResult: (Object? result) {
                _nestedNavigatorKey.currentState?.maybePop<Object?>(result);
                if (mounted) {
                  setState(() {
                    _status = 'NavigatorPopHandler popped the nested route';
                  });
                }
              },
              child: Navigator(
                key: _nestedNavigatorKey,
                onGenerateRoute: (RouteSettings settings) {
                  return MaterialPageRoute<void>(
                    settings: settings,
                    builder: (_) => _buildNestedPage(0),
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _toggleCanLeave() {
    setState(() {
      _canLeave = !_canLeave;
      _status = _canLeave ? 'Route pop enabled' : 'Route pop disabled';
    });
  }

  void _pushNestedPage() {
    final int page = _nestedPage++;
    _nestedNavigatorKey.currentState?.push<void>(
      MaterialPageRoute<void>(
        settings: RouteSettings(name: 'nested-$page'),
        builder: (_) => _buildNestedPage(page),
      ),
    );
    setState(() {
      _status = 'Nested page $page pushed';
    });
  }

  static Widget _buildNestedPage(int page) {
    return Container(
      color: page == 0 ? const Color(0xFFE8F0FE) : const Color(0xFFE6F4EA),
      padding: const EdgeInsets.all(16),
      alignment: Alignment.center,
      child: Text(
        page == 0 ? 'Nested root' : 'Nested page $page',
        style: const TextStyle(fontSize: 18, color: Colors.black),
      ),
    );
  }
}
