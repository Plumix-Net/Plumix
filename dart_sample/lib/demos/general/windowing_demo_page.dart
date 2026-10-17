// The windowing API is experimental and internal to Flutter (`_window.dart` is not exported by
// `widgets.dart`); the demo imports it directly, as Flutter's own windowing examples do.
// ignore_for_file: implementation_imports, invalid_use_of_internal_member

import 'package:flutter/src/foundation/_features.dart' show isWindowingEnabled;
import 'package:flutter/src/widgets/_window.dart';
import 'package:material_ui/material_ui.dart';

class WindowingDemoPage extends StatefulWidget {
  const WindowingDemoPage({super.key});

  @override
  State<WindowingDemoPage> createState() => _WindowingDemoPageState();
}

class _WindowingDemoPageState extends State<WindowingDemoPage> {
  int _openWindows = 0;
  int _nextWindow = 1;
  String _lastEvent = 'none';

  void _record(String message) => setState(() => _lastEvent = message);

  void _openRegularWindow(BuildContext context) {
    final WindowRegistry registry = WindowRegistry.of(context);
    final int number = _nextWindow++;
    late final WindowEntry entry;
    final _DemoWindowDelegate delegate = _DemoWindowDelegate(
      onDestroyed: () {
        registry.unregister(entry);
        entry.controller.dispose();
        setState(() => _openWindows--);
        _record('window $number destroyed');
      },
    );
    final RegularWindowController controller = RegularWindowController(
      size: const Size(360, 220),
      title: 'Plumix window $number',
      delegate: delegate,
    );
    entry = WindowEntry(
      controller: controller,
      builder: (BuildContext context) => _DemoWindowContent(controller: controller),
    );
    registry.register(entry);
    setState(() => _openWindows++);
    _record('window $number opened');
  }

  Future<void> _showDialog(BuildContext context) async {
    final String? result = await showDialog<String>(
      context: context,
      builder: (BuildContext context) => AlertDialog(
        title: const Text('Dialog window'),
        content: const Text(
          'With windowing enabled this dialog is its own window.',
        ),
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.of(context).pop('cancel'),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop('ok'),
            child: const Text('OK'),
          ),
        ],
      ),
    );
    _record('dialog closed: ${result ?? 'dismissed'}');
  }

  @override
  Widget build(BuildContext context) {
    final bool enabled = isWindowingEnabled;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 16,
      children: <Widget>[
        const Text(
          'Windowing',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Regular windows and dialog windows through WindowRegistry, '
          'RegularWindowController and showDialog. '
          'Run with FLUTTER_ENABLED_FEATURE_FLAGS=windowing on a desktop host.',
          style: TextStyle(color: Color(0xFF696969)),
        ),
        Text(
          'Windowing: ${enabled ? 'enabled' : 'disabled'}',
          style: const TextStyle(color: Colors.black),
        ),
        TextButton(
          onPressed: enabled ? () => _openRegularWindow(context) : null,
          child: const Text('Open regular window'),
        ),
        TextButton(
          onPressed: () => _showDialog(context),
          child: const Text('Show dialog'),
        ),
        Text(
          'Open windows: $_openWindows',
          style: const TextStyle(color: Colors.black),
        ),
        Text(
          'Last event: $_lastEvent',
          style: const TextStyle(color: Color(0xFF31506F)),
        ),
      ],
    );
  }
}

class _DemoWindowDelegate extends RegularWindowControllerDelegate {
  _DemoWindowDelegate({required this.onDestroyed});

  final VoidCallback onDestroyed;

  @override
  void onWindowDestroyed() {
    super.onWindowDestroyed();
    onDestroyed();
  }
}

class _DemoWindowContent extends StatelessWidget {
  const _DemoWindowContent({required this.controller});

  final RegularWindowController controller;

  @override
  Widget build(BuildContext context) {
    final Size size = WindowScope.contentSizeOf(context);
    return Container(
      color: const Color(0xFFF4F7FA),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        spacing: 8,
        children: <Widget>[
          Text(
            WindowScope.titleOf(context),
            style: const TextStyle(fontSize: 18, color: Colors.black),
          ),
          Text(
            'Content size: ${size.width.toStringAsFixed(0)} x '
            '${size.height.toStringAsFixed(0)}',
            style: const TextStyle(color: Colors.black),
          ),
          Text(
            'Activated: ${WindowScope.isActivatedOf(context)}',
            style: const TextStyle(color: Colors.black),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: () =>
                    controller.setMaximized(!WindowScope.isMaximizedOf(context)),
                child: const Text('Toggle maximize'),
              ),
              TextButton(
                onPressed: controller.destroy,
                child: const Text('Close'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
