// ignore_for_file: deprecated_member_use

import 'package:material_ui/material_ui.dart';
import 'package:flutter/services.dart';

class KeyboardListenerDemoPage extends StatefulWidget {
  const KeyboardListenerDemoPage({super.key});

  @override
  State<KeyboardListenerDemoPage> createState() =>
      _KeyboardListenerDemoPageState();
}

class _KeyboardListenerDemoPageState extends State<KeyboardListenerDemoPage> {
  final FocusNode _keyboardFocusNode = FocusNode();
  final FocusNode _rawFocusNode = FocusNode();
  final FocusNode _shortcutFocusNode = FocusNode();
  final FocusNode _excludedFocusNode = FocusNode();
  late final Map<ShortcutActivator, Intent> _shortcuts;
  late final Map<Type, Action<Intent>> _actions;
  String _keyboardEvent = 'none';
  String _rawEvent = 'none';
  int _shortcutCount = 0;
  int _excludedClickCount = 0;
  bool _shortcutFocusHighlight = false;
  bool _shortcutHoverHighlight = false;

  @override
  void initState() {
    super.initState();
    _keyboardFocusNode.addListener(_handleFocusChanged);
    _rawFocusNode.addListener(_handleFocusChanged);
    _shortcutFocusNode.addListener(_handleFocusChanged);
    _excludedFocusNode.addListener(_handleFocusChanged);
    _shortcuts = <ShortcutActivator, Intent>{
      const SingleActivator(LogicalKeyboardKey.keyK, control: true):
          const _CounterShortcutIntent(1),
      const SingleActivator(LogicalKeyboardKey.keyJ, control: true):
          const _CounterShortcutIntent(-1),
    };
    _actions = <Type, Action<Intent>>{
      _CounterShortcutIntent: CallbackAction<_CounterShortcutIntent>(
        onInvoke: (_CounterShortcutIntent intent) {
          setState(() {
            _shortcutCount += intent.delta;
          });
          return _shortcutCount;
        },
      ),
    };
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Keyboard + focus action detectors',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Click a panel or use its button, then press and release keyboard keys.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        _buildKeyboardListenerProbe(),
        _buildRawKeyboardListenerProbe(),
        _buildActionsShortcutsProbe(),
      ],
    );
  }

  @override
  void dispose() {
    _keyboardFocusNode.removeListener(_handleFocusChanged);
    _rawFocusNode.removeListener(_handleFocusChanged);
    _shortcutFocusNode.removeListener(_handleFocusChanged);
    _excludedFocusNode.removeListener(_handleFocusChanged);
    _keyboardFocusNode.dispose();
    _rawFocusNode.dispose();
    _shortcutFocusNode.dispose();
    _excludedFocusNode.dispose();
    super.dispose();
  }

  Widget _buildKeyboardListenerProbe() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        TextButton(
          onPressed: _keyboardFocusNode.requestFocus,
          child: const Text('Focus KeyboardListener'),
        ),
        KeyboardListener(
          focusNode: _keyboardFocusNode,
          onKeyEvent: (KeyEvent event) {
            setState(() {
              _keyboardEvent = _describeKeyEvent(event);
            });
          },
          child: _buildPanel(
            title: 'KeyboardListener',
            detail: _keyboardEvent,
            focused: _keyboardFocusNode.hasFocus,
          ),
        ),
      ],
    );
  }

  Widget _buildRawKeyboardListenerProbe() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        TextButton(
          onPressed: _rawFocusNode.requestFocus,
          child: const Text('Focus RawKeyboardListener'),
        ),
        RawKeyboardListener(
          focusNode: _rawFocusNode,
          onKey: (RawKeyEvent event) {
            setState(() {
              final String phase = event is RawKeyDownEvent ? 'down' : 'up';
              _rawEvent = '${event.logicalKey.debugName} — $phase';
            });
          },
          child: _buildPanel(
            title: 'RawKeyboardListener (deprecated compatibility)',
            detail: _rawEvent,
            focused: _rawFocusNode.hasFocus,
          ),
        ),
      ],
    );
  }

  Widget _buildActionsShortcutsProbe() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        TextButton(
          onPressed: _shortcutFocusNode.requestFocus,
          child: const Text('Focus Actions + Shortcuts'),
        ),
        FocusableActionDetector(
          focusNode: _shortcutFocusNode,
          shortcuts: _shortcuts,
          actions: _actions,
          onShowFocusHighlight: (bool value) {
            setState(() {
              _shortcutFocusHighlight = value;
            });
          },
          onShowHoverHighlight: (bool value) {
            setState(() {
              _shortcutHoverHighlight = value;
            });
          },
          child: _buildPanel(
            title: 'FocusableActionDetector',
            detail:
                'count $_shortcutCount — Ctrl+K / Ctrl+J — focus highlight '
                '${_onOff(_shortcutFocusHighlight)} — hover '
                '${_onOff(_shortcutHoverHighlight)}',
            focused: _shortcutFocusNode.hasFocus,
          ),
        ),
        ExcludeFocusTraversal(
          child: TextButton(
            focusNode: _excludedFocusNode,
            onPressed: () {
              setState(() {
                _excludedClickCount++;
              });
            },
            child: Text(
              'ExcludeFocusTraversal: Tab skips, click works '
              '($_excludedClickCount)',
            ),
          ),
        ),
      ],
    );
  }

  static Widget _buildPanel({
    required String title,
    required String detail,
    required bool focused,
  }) {
    return Container(
      height: 88,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: focused ? const Color(0xFFE0F2F1) : const Color(0xFFF1F3F4),
        border: Border.all(
          color: focused ? const Color(0xFF00796B) : const Color(0xFF9AA0A6),
          width: focused ? 2 : 1,
        ),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.start,
        spacing: 6,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(fontSize: 15, color: Colors.black),
          ),
          Text(
            'Last event: $detail',
            style: const TextStyle(fontSize: 13, color: Colors.black54),
          ),
        ],
      ),
    );
  }

  static String _describeKeyEvent(KeyEvent event) {
    final String phase = switch (event) {
      KeyDownEvent() => 'down',
      KeyRepeatEvent() => 'repeat',
      _ => 'up',
    };
    return '${event.logicalKey.debugName} — $phase — '
        'physical ${event.physicalKey.debugName}';
  }

  static String _onOff(bool value) => value ? 'on' : 'off';

  void _handleFocusChanged() {
    setState(() {});
  }
}

class _CounterShortcutIntent extends Intent {
  const _CounterShortcutIntent(this.delta);

  final int delta;
}
