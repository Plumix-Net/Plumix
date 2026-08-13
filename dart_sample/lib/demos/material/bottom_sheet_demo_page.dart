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
    Widget buildContent(BuildContext sheetContext) => Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'BottomSheet + ModalBottomSheet',
          style: TextStyle(fontSize: 20),
        ),
        const Text(
          'Persistent LocalHistory/controller flow, modal scrim/result, drag handle, 9/16 height cap, SafeArea, display-feature anchoring, theme precedence, and a draggable-scrollable child that closes the sheet at its minimum extent.',
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
              onPressed: () => _showPersistent(sheetContext),
              child: const Text('SHOW PERSISTENT'),
            ),
            FilledButton(
              onPressed: () => _showModal(sheetContext),
              child: const Text('SHOW MODAL'),
            ),
            OutlinedButton(
              onPressed: () => _showDraggable(sheetContext),
              child: const Text('SHOW DRAGGABLE'),
            ),
          ],
        ),
        Text(
          'Last modal result: $_lastResult',
          style: const TextStyle(fontSize: 13),
        ),
      ],
    );

    if (!_customTheme) {
      return buildContent(context);
    }

    return Theme(
      data: Theme.of(context).copyWith(
        bottomSheetTheme: BottomSheetThemeData(
          backgroundColor: const Color(0xFFE8DEF8),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
          ),
          showDragHandle: true,
          dragHandleColor: WidgetStateColor.resolveWith(
            (Set<WidgetState> states) => states.contains(WidgetState.hovered)
                ? const Color(0xFFB3261E)
                : const Color(0xFF6750A4),
          ),
        ),
      ),
      child: Builder(builder: buildContent),
    );
  }

  void _showPersistent(BuildContext context) {
    late PersistentBottomSheetController controller;
    controller = showBottomSheet(
      context: context,
      showDragHandle: _showDragHandle,
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
      builder: (BuildContext context) => _sheetContent(
        'Modal sheet',
        () => Navigator.pop(context, 'accepted'),
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }

  Future<void> _showDraggable(BuildContext context) async {
    final String? result = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: _showDragHandle,
      builder: (BuildContext context) => DraggableScrollableSheet(
        initialChildSize: 0.5,
        minChildSize: 0.25,
        maxChildSize: 0.95,
        expand: false,
        builder: (BuildContext context, ScrollController scrollController) =>
            ColoredBox(
              color: const Color(0xFFF7F2FA),
              child: ListView.builder(
                controller: scrollController,
                itemExtent: 48,
                itemCount: 40,
                itemBuilder: (BuildContext context, int index) => Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 12,
                  ),
                  child: Text('Draggable row $index'),
                ),
              ),
            ),
      ),
    );
    if (mounted) {
      setState(() => _lastResult = result ?? 'dragged to minimum');
    }
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
