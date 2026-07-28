import 'package:flutter/material.dart';

class LifecycleUtilitiesDemoPage extends StatefulWidget {
  const LifecycleUtilitiesDemoPage({super.key});

  @override
  State<LifecycleUtilitiesDemoPage> createState() =>
      _LifecycleUtilitiesDemoPageState();
}

class _LifecycleUtilitiesDemoPageState extends State<LifecycleUtilitiesDemoPage>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final AppLifecycleListener _lifecycleListener;
  late final DisposableBuildContext<_LifecycleUtilitiesDemoPageState>
  _disposableContext;
  AppLifecycleState? _lastLifecycleState;
  int _statusBuildCount = 0;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 600),
      vsync: this,
    );
    _disposableContext =
        DisposableBuildContext<_LifecycleUtilitiesDemoPageState>(this);
    _lifecycleListener = AppLifecycleListener(
      onStateChange: _handleLifecycleStateChanged,
    );
  }

  @override
  void dispose() {
    _lifecycleListener.dispose();
    _disposableContext.dispose();
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            const Text(
              'Lifecycle listener controls',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Change window focus or minimize/restore the app to exercise '
              'AppLifecycleListener. The animation probe rebuilds only when '
              'AnimationStatus changes.',
              style: TextStyle(color: Colors.black54),
            ),
            Container(
              color: const Color(0xFFF4F7FA),
              padding: const EdgeInsets.all(12),
              child: Text(
                'Last app state: ${_lastLifecycleState?.name ?? 'waiting'}\n'
                'Disposable context available: '
                '${_disposableContext.context != null}',
              ),
            ),
            _DemoStatusTransition(
              animation: _controller,
              builder: _buildStatusReadout,
            ),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: <Widget>[
                TextButton(
                  onPressed: () => _controller.forward(from: 0),
                  child: const Text('Forward'),
                ),
                TextButton(
                  onPressed: () => _controller.reverse(from: 1),
                  child: const Text('Reverse'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusReadout(BuildContext context) {
    _statusBuildCount++;
    return Container(
      color: const Color(0xFFE7F0FA),
      padding: const EdgeInsets.all(12),
      child: Text(
        'Animation status: ${_controller.status.name}\n'
        'StatusTransitionWidget builds: $_statusBuildCount',
      ),
    );
  }

  void _handleLifecycleStateChanged(AppLifecycleState state) {
    if (_disposableContext.context == null) {
      return;
    }
    setState(() {
      _lastLifecycleState = state;
    });
  }
}

class _DemoStatusTransition extends StatusTransitionWidget {
  const _DemoStatusTransition({
    required super.animation,
    required this.builder,
  });

  final WidgetBuilder builder;

  @override
  Widget build(BuildContext context) => builder(context);
}
