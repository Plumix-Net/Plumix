import 'package:flutter/material.dart';

class BottomSheetDemoPage extends StatefulWidget {
  const BottomSheetDemoPage({super.key});

  @override
  State<BottomSheetDemoPage> createState() => _BottomSheetDemoPageState();
}

class _BottomSheetDemoPageState extends State<BottomSheetDemoPage> {
  bool _showDragHandle = true;
  bool _scrollControlled = false;
  bool _customTheme = false;
  bool _anchorEnd = false;
  String _lastResult = 'none';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'BottomSheet + ModalBottomSheet',
          style: TextStyle(fontSize: 20),
        ),
        const Text(
          'Persistent LocalHistory/controller flow, modal scrim/result, drag handle, 9/16 height cap, SafeArea, display-feature anchoring, and theme precedence.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () =>
                  setState(() => _showDragHandle = !_showDragHandle),
              child: Text(_showDragHandle ? 'Handle on' : 'Handle off'),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _scrollControlled = !_scrollControlled),
              child: Text(_scrollControlled ? 'Full height' : '9/16 cap'),
            ),
            TextButton(
              onPressed: () => setState(() => _customTheme = !_customTheme),
              child: Text(_customTheme ? 'Theme on' : 'Theme off'),
            ),
            TextButton(
              onPressed: () => setState(() => _anchorEnd = !_anchorEnd),
              child: Text(_anchorEnd ? 'Anchor end' : 'Anchor start'),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            ElevatedButton(
              onPressed: () => _showPersistent(context),
              child: const Text('SHOW PERSISTENT'),
            ),
            FilledButton(
              onPressed: () => _showModal(context),
              child: const Text('SHOW MODAL'),
            ),
          ],
        ),
        Text(
          'Last modal result: $_lastResult',
          style: const TextStyle(fontSize: 13),
        ),
      ],
    );
  }

  void _showPersistent(BuildContext context) {
    late PersistentBottomSheetController controller;
    controller = showBottomSheet(
      context: context,
      showDragHandle: _showDragHandle,
      backgroundColor: _customTheme ? const Color(0xFFFFF0F5) : null,
      shape: _customTheme
          ? RoundedRectangleBorder(borderRadius: BorderRadius.circular(18))
          : null,
      builder: (BuildContext context) =>
          _sheetContent('Persistent sheet', controller.close),
    );
  }

  Future<void> _showModal(BuildContext context) async {
    final String? result = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: _showDragHandle,
      isScrollControlled: _scrollControlled,
      useSafeArea: true,
      anchorPoint: _anchorEnd ? const Offset(double.maxFinite, 0) : null,
      backgroundColor: _customTheme ? const Color(0xFFE8DEF8) : null,
      shape: _customTheme
          ? RoundedRectangleBorder(borderRadius: BorderRadius.circular(18))
          : null,
      builder: (BuildContext context) => _sheetContent(
        'Modal sheet',
        () => Navigator.pop(context, 'accepted'),
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }

  Widget _sheetContent(String title, VoidCallback close) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          Text(title, style: const TextStyle(fontSize: 18)),
          const Text(
            'Drag downward or use the action below.',
            style: TextStyle(color: Colors.black54),
          ),
          TextButton(onPressed: close, child: const Text('CLOSE')),
        ],
      ),
    );
  }
}
