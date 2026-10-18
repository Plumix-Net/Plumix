// ignore_for_file: deprecated_member_use

import 'dart:async';

import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class NavigatorLifecycleProbe extends StatefulWidget {
  const NavigatorLifecycleProbe({super.key});

  @override
  State<NavigatorLifecycleProbe> createState() => _ProbeState();
}

class _ProbeState extends State<NavigatorLifecycleProbe> {
  final _navigatorKey = GlobalKey<NavigatorState>();
  Completer<bool>? _decision;
  int _page = 0;
  int _localEntries = 0;
  String _lastResult = 'none';

  Future<bool> _confirmBack() {
    if (_decision == null) {
      setState(() => _decision = Completer<bool>());
    }
    return _decision!.future;
  }

  Future<void> _pushPage() async {
    final int number = ++_page;
    final Object? result = await _navigatorKey.currentState!.push<Object?>(
      PageRouteBuilder<Object?>(
        settings: RouteSettings(name: 'page $number'),
        pageBuilder: (_, _, _) => WillPopScope(
          onWillPop: _confirmBack,
          child: _pageBody('Page $number: Back waits for Allow back.'),
        ),
        transitionDuration: const Duration(milliseconds: 300),
      ),
    );
    if (mounted) {
      setState(() => _lastResult = 'page $number: ${result ?? 'null'}');
    }
  }

  static Widget _pageBody(String message) => Container(
    color: const Color(0xFFE8F0FE),
    alignment: Alignment.center,
    child: Text(
      message,
      style: const TextStyle(fontSize: 13, color: Colors.black),
    ),
  );

  @override
  void dispose() {
    if (_decision != null && !_decision!.isCompleted) {
      _decision!.complete(false);
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    spacing: 6,
    children: <Widget>[
      const Text(
        'Route lifecycle',
        style: TextStyle(fontSize: 16, color: Colors.black),
      ),
      Text(
        'Result: $_lastResult; local entries: $_localEntries; '
        '${_decision == null ? 'ready' : 'waiting for Allow back'}',
        style: const TextStyle(fontSize: 12, color: Colors.black),
      ),
      Wrap(
        spacing: 6,
        runSpacing: 6,
        children: <Widget>[
          _action('Push page', _pushPage),
          _action('Back', () {
            if (_decision == null) _navigatorKey.currentState!.maybePop('back');
          }),
          _action('Allow back', () {
            final Completer<bool>? decision = _decision;
            setState(() => _decision = null);
            decision?.complete(true);
          }),
          _action('Add local history', () {
            // The route is owned by this nested navigator, including its initial home page.
            final NavigatorState navigator = _navigatorKey.currentState!;
            Route<dynamic>? current;
            navigator.popUntil((Route<dynamic> route) {
              current = route;
              return true;
            });
            (current! as ModalRoute<dynamic>).addLocalHistoryEntry(
              LocalHistoryEntry(
                onRemove: () {
                  if (mounted) setState(() => _localEntries--);
                },
              ),
            );
            setState(() => _localEntries++);
          }),
          _action(
            'Pop to home with result',
            () => _navigatorKey.currentState!.popUntilWithResult<Object?>(
              (Route<dynamic> route) => route.isFirst,
              'home-result',
            ),
          ),
        ],
      ),
      SizedBox(
        height: 100,
        child: Navigator(
          key: _navigatorKey,
          onGenerateRoute: (_) => PageRouteBuilder<void>(
            pageBuilder: (_, _, _) => _pageBody(
              'Home: push two pages, then pop to home with a result.',
            ),
          ),
        ),
      ),
    ],
  );

  static Widget _action(String label, VoidCallback action) => CounterTapButton(
    label: label,
    onTap: action,
    background: const Color(0xFFE0F2F1),
    foreground: Colors.black,
    fontSize: 12,
    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
  );
}
