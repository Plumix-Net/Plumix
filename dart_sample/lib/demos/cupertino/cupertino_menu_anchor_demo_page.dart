import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoMenuAnchorDemoPage extends StatefulWidget {
  const CupertinoMenuAnchorDemoPage({super.key});

  @override
  State<CupertinoMenuAnchorDemoPage> createState() =>
      _CupertinoMenuAnchorDemoPageState();
}

class _CupertinoMenuAnchorDemoPageState
    extends State<CupertinoMenuAnchorDemoPage> {
  String _lastAction = 'No action selected';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      spacing: 16,
      children: <Widget>[
        const Text(
          'Cupertino menu anchor',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Open the anchored menu by button, keyboard, long press, or swipe. '
          'Items demonstrate leading, subtitle, trailing, disabled, and '
          'destructive states.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        CupertinoMenuAnchor(
          enableLongPressToOpen: true,
          menuChildren: <Widget>[
            CupertinoMenuItem(
              leading: const Icon(CupertinoIcons.pencil),
              trailing: const Text('⌘R'),
              subtitle: const Text('Keep this document in place'),
              onPressed: () => _select('Rename'),
              child: const Text('Rename'),
            ),
            CupertinoMenuItem(
              leading: const Icon(CupertinoIcons.share),
              onPressed: () => _select('Share'),
              child: const Text('Share'),
            ),
            const CupertinoMenuDivider(),
            const CupertinoMenuItem(
              leading: Icon(CupertinoIcons.folder),
              onPressed: null,
              child: Text('Unavailable action'),
            ),
            CupertinoMenuItem(
              leading: const Icon(CupertinoIcons.trash),
              isDestructiveAction: true,
              onPressed: () => _select('Delete'),
              child: const Text('Delete'),
            ),
          ],
          builder: (
            BuildContext context,
            MenuController controller,
            Widget? child,
          ) {
            return CupertinoButton(
              color: const Color(0xFF007AFF),
              onPressed: controller.isOpen ? controller.close : controller.open,
              child: child ??
                  const Row(
                    mainAxisSize: MainAxisSize.min,
                    spacing: 8,
                    children: <Widget>[
                      Text(
                        'Document actions',
                        style: TextStyle(color: Colors.white),
                      ),
                      Icon(
                        CupertinoIcons.chevron_down,
                        color: Colors.white,
                        size: 18,
                      ),
                    ],
                  ),
            );
          },
        ),
        Text(
          _lastAction,
          style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
        ),
      ],
    );
  }

  void _select(String action) {
    setState(() => _lastAction = 'Last action: $action');
  }
}
