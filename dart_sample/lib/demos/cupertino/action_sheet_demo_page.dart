import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class CupertinoActionSheetDemoPage extends StatefulWidget {
  const CupertinoActionSheetDemoPage({super.key});

  @override
  State<CupertinoActionSheetDemoPage> createState() =>
      _CupertinoActionSheetDemoPageState();
}

class _CupertinoActionSheetDemoPageState
    extends State<CupertinoActionSheetDemoPage> {
  String _lastResult = 'none';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Cupertino action sheet',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Blurred bottom sheet with a title/message section, hairline-separated '
          'actions, a detached cancel button, and slide-to-select.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Text(
          'last result: $_lastResult',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
        _buildAction(
          label: 'Title + message + actions + cancel',
          onTap: () => _showFullSheet(context),
          background: const Color(0xFFE9F0FF),
        ),
        _buildAction(
          label: 'Actions only',
          onTap: () => _showActionsOnly(context),
          background: const Color(0xFFEAE4FF),
        ),
        _buildAction(
          label: 'Message + cancel only',
          onTap: () => _showMessageOnly(context),
          background: const Color(0xFFE8F0FE),
        ),
        _buildAction(
          label: 'Scrollable action list',
          onTap: () => _showScrollableSheet(context),
          background: const Color(0xFFE8F4E8),
        ),
      ],
    );
  }

  void _showFullSheet(BuildContext context) {
    _show(
      context,
      (BuildContext sheetContext) => CupertinoActionSheet(
        title: const Text('Move to trash'),
        message: const Text(
          'This document and every revision of it will be deleted.',
        ),
        actions: <Widget>[
          CupertinoActionSheetAction(
            isDefaultAction: true,
            onPressed: () => _complete(sheetContext, 'keep editing'),
            child: const Text('Keep editing'),
          ),
          CupertinoActionSheetAction(
            onPressed: () => _complete(sheetContext, 'duplicate first'),
            child: const Text('Duplicate first'),
          ),
          CupertinoActionSheetAction(
            isDestructiveAction: true,
            onPressed: () => _complete(sheetContext, 'delete'),
            child: const Text('Delete'),
          ),
        ],
        cancelButton: CupertinoActionSheetAction(
          onPressed: () => _complete(sheetContext, 'cancel'),
          child: const Text('Cancel'),
        ),
      ),
    );
  }

  void _showActionsOnly(BuildContext context) {
    _show(
      context,
      (BuildContext sheetContext) => CupertinoActionSheet(
        actions: <Widget>[
          CupertinoActionSheetAction(
            onPressed: () => _complete(sheetContext, 'copy link'),
            child: const Text('Copy link'),
          ),
          CupertinoActionSheetAction(
            onPressed: () => _complete(sheetContext, 'share'),
            child: const Text('Share'),
          ),
        ],
      ),
    );
  }

  void _showMessageOnly(BuildContext context) {
    _show(
      context,
      (BuildContext sheetContext) => CupertinoActionSheet(
        message: const Text(
          'Signing out removes every downloaded file from this device.',
        ),
        cancelButton: CupertinoActionSheetAction(
          onPressed: () => _complete(sheetContext, 'not now'),
          child: const Text('Not now'),
        ),
      ),
    );
  }

  void _showScrollableSheet(BuildContext context) {
    _show(
      context,
      (BuildContext sheetContext) => CupertinoActionSheet(
        title: const Text('Pick a destination'),
        actions: <Widget>[
          for (int index = 1; index <= 12; index += 1)
            CupertinoActionSheetAction(
              onPressed: () => _complete(sheetContext, 'folder $index'),
              child: Text('Folder $index'),
            ),
        ],
        cancelButton: CupertinoActionSheetAction(
          onPressed: () => _complete(sheetContext, 'cancel'),
          child: const Text('Cancel'),
        ),
      ),
    );
  }

  static void _show(BuildContext context, WidgetBuilder builder) {
    showCupertinoModalPopup<String>(context: context, builder: builder);
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
