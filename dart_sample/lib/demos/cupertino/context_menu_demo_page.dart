import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoContextMenuDemoPage extends StatefulWidget {
  const CupertinoContextMenuDemoPage({super.key});

  @override
  State<CupertinoContextMenuDemoPage> createState() =>
      _CupertinoContextMenuDemoPageState();
}

class _CupertinoContextMenuDemoPageState
    extends State<CupertinoContextMenuDemoPage> {
  String _lastAction = 'none';

  @override
  Widget build(BuildContext context) {
    final Widget preview = Container(
      width: 180,
      height: 120,
      decoration: const BoxDecoration(
        color: Color(0xFF5E5CE6),
        borderRadius: BorderRadius.all(Radius.circular(16)),
      ),
      alignment: Alignment.center,
      child: const Text(
        'Press and hold',
        style: TextStyle(color: Colors.white, fontSize: 18),
      ),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 18,
      children: <Widget>[
        const Text(
          'Cupertino context menu',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Hold the preview, choose an action, or drag the open preview '
          'down to dismiss.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Center(
          child: CupertinoContextMenu(
            enableHapticFeedback: true,
            actions: <Widget>[
              _buildAction(context, 'Copy', CupertinoIcons.doc_on_doc, 'copy'),
              _buildAction(context, 'Share', CupertinoIcons.share, 'share'),
              _buildAction(
                context,
                'Delete',
                CupertinoIcons.delete,
                'delete',
                destructive: true,
              ),
            ],
            child: preview,
          ),
        ),
        Text(
          'last action: $_lastAction',
          style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
        ),
      ],
    );
  }

  Widget _buildAction(
    BuildContext context,
    String label,
    IconData icon,
    String result, {
    bool destructive = false,
  }) {
    return CupertinoContextMenuAction(
      trailingIcon: icon,
      isDestructiveAction: destructive,
      onPressed: () {
        Navigator.pop(context);
        setState(() => _lastAction = result);
      },
      child: Text(label),
    );
  }
}
