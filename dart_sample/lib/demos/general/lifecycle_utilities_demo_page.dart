import 'dart:ui' as ui;

import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

class LifecycleUtilitiesDemoPage extends StatefulWidget {
  const LifecycleUtilitiesDemoPage({super.key});

  @override
  State<LifecycleUtilitiesDemoPage> createState() =>
      _LifecycleUtilitiesDemoPageState();
}

class _LifecycleUtilitiesDemoPageState extends State<LifecycleUtilitiesDemoPage>
    with SingleTickerProviderStateMixin, WidgetsBindingObserver {
  late final AnimationController _controller;
  late final AppLifecycleListener _lifecycleListener;
  late final DisposableBuildContext<_LifecycleUtilitiesDemoPageState>
  _disposableContext;
  AppLifecycleState? _lastLifecycleState;
  int _statusBuildCount = 0;
  int _metricsChanges = 0;
  int _brightnessChanges = 0;
  int _memoryPressureEvents = 0;
  final ui.ChannelBuffers _messageBuffers = ui.ChannelBuffers();
  static const String _demoChannel = 'plumix/lifecycle-buffer-demo';
  int _messageNumber = 0;
  String _lastMessage = 'none';
  bool _listening = false;

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
    WidgetsBinding.instance.addObserver(this);
    WidgetsBinding.instance.waitUntilFirstFrameRasterized.then((_) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void didChangeMetrics() {
    setState(() => _metricsChanges++);
  }

  @override
  void didChangePlatformBrightness() {
    setState(() => _brightnessChanges++);
  }

  @override
  void didHaveMemoryPressure() {
    setState(() => _memoryPressureEvents++);
  }

  @override
  void dispose() {
    _messageBuffers.clearListener(_demoChannel);
    WidgetsBinding.instance.removeObserver(this);
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
            const Text(
              'Binding events',
              style: TextStyle(fontSize: 16, color: Colors.black),
            ),
            const Text(
              'Resize the window or switch the system theme: WidgetsBindingObserver '
              'hears didChangeMetrics and didChangePlatformBrightness from the platform.',
              style: TextStyle(color: Colors.black54),
            ),
            Container(
              color: const Color(0xFFF4F7FA),
              padding: const EdgeInsets.all(12),
              child: Text(
                'Metrics changes: $_metricsChanges\n'
                'Platform brightness changes: $_brightnessChanges '
                '(now ${MediaQuery.platformBrightnessOf(context).name})\n'
                'Memory pressure events: $_memoryPressureEvents\n'
                'First frame rasterized: '
                '${WidgetsBinding.instance.firstFrameRasterized}',
              ),
            ),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: <Widget>[
                TextButton(
                  onPressed: () =>
                      PaintingBinding.instance.handleMemoryPressure(),
                  child: const Text('Simulate memory pressure'),
                ),
              ],
            ),
            const Text(
              'Messages before listening',
              style: TextStyle(fontSize: 16),
            ),
            const Text(
              'Queue several messages, then start listening. The default buffer '
              'keeps the latest message; new messages arrive immediately while listening.',
            ),
            Text(
              'Sent: $_messageNumber · Received: $_lastMessage · Listening: $_listening',
            ),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: <Widget>[
                TextButton(
                  onPressed: _queueMessage,
                  child: const Text('Queue message'),
                ),
                TextButton(
                  onPressed: _listening ? null : _startListening,
                  child: const Text('Start listening'),
                ),
                TextButton(
                  onPressed: _listening ? _stopListening : null,
                  child: const Text('Stop listening'),
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

  void _queueMessage() {
    setState(() => _messageNumber++);
    _messageBuffers.push(
      _demoChannel,
      const StringCodec().encodeMessage('Message $_messageNumber'),
      (_) {},
    );
  }

  void _startListening() {
    setState(() => _listening = true);
    _messageBuffers.setListener(_demoChannel, (data, reply) {
      if (mounted) {
        setState(() {
          _lastMessage = const StringCodec().decodeMessage(data) ?? 'empty';
        });
      }
      reply(null);
    });
  }

  void _stopListening() {
    _messageBuffers.clearListener(_demoChannel);
    setState(() => _listening = false);
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
