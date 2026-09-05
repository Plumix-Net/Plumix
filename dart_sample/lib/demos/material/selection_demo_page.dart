import 'package:material_ui/material_ui.dart';
import 'package:flutter/rendering.dart'
    show SelectedContent, SelectedContentRange;

class SelectionDemoPage extends StatefulWidget {
  const SelectionDemoPage({super.key});

  @override
  State<SelectionDemoPage> createState() => _SelectionDemoPageState();
}

class _SelectionDemoPageState extends State<SelectionDemoPage> {
  final SelectionListenerNotifier _selectionNotifier =
      SelectionListenerNotifier();
  final ContextMenuController _menuController = ContextMenuController();
  int _menuValue = 0;
  int _pageClicks = 0;
  bool _interactive = true;
  String _singleSelection = 'none';
  String _areaSelection = 'none';
  String _listenerDetails = 'none';

  @override
  void initState() {
    super.initState();
    _selectionNotifier.addListener(_handleSelectionDetailsChanged);
  }

  void _handleSelectionDetailsChanged() {
    final SelectionDetails details = _selectionNotifier.selection;
    final SelectedContentRange? range = details.range;
    setState(() {
      _listenerDetails = range == null
          ? '${details.status.name}: none'
          : '${details.status.name}: ${range.startOffset}..${range.endOffset}';
    });
  }

  @override
  void dispose() {
    _menuController.remove();
    _selectionNotifier.dispose();
    super.dispose();
  }

  void _showMenu() {
    _menuValue++;
    _menuController.show(context: context, contextMenuBuilder: _buildMenu);
  }

  Widget _buildMenu(BuildContext context) {
    return Positioned(
      right: 16,
      top: 80,
      width: 260,
      child: Material(
        elevation: 8,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            spacing: 8,
            children: <Widget>[
              Text(
                'Menu value: $_menuValue',
                style: const TextStyle(fontSize: 18),
              ),
              const Text('The page stays interactive while this menu is open.'),
              TextButton(
                onPressed: _menuController.remove,
                child: const Text('Close menu'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'SelectableText + SelectionArea',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Drag across text, then right-click or long-press for the adaptive context menu. '
            'Double-tap selects a word and triple-tap a paragraph; long press raises the '
            'drag handles and the magnifier. Ctrl/Cmd+A and Ctrl/Cmd+C also work. '
            'The second probe spans several Text widgets.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: () => setState(() => _interactive = !_interactive),
              child: Text('Interactive: $_interactive'),
            ),
          ),
          const Text(
            'ContextMenuController',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          const Text(
            'Show a menu, update its value, and click the page while it stays open.',
          ),
          Wrap(
            spacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: _showMenu,
                child: const Text('Show / replace builder'),
              ),
              TextButton(
                onPressed: () {
                  if (!_menuController.isShown) return;
                  _menuValue++;
                  _menuController.markNeedsBuild();
                },
                child: const Text('Rebuild menu'),
              ),
              TextButton(
                onPressed: () => setState(() => _pageClicks++),
                child: Text('Page clicks: $_pageClicks'),
              ),
            ],
          ),
          const Text(
            'Single selectable run',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              color: const Color(0xFFF7F2FA),
              border: Border.all(color: const Color(0xFFCAC4D0)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: SelectableText(
                'Plumix keeps selectable text in the framework render pipeline.',
                style: const TextStyle(fontSize: 17, height: 1.35),
                showCursor: true,
                enableInteractiveSelection: _interactive,
                onSelectionChanged:
                    (TextSelection selection, SelectionChangedCause? cause) {
                      setState(() {
                        _singleSelection = selection.isCollapsed
                            ? 'none'
                            : '${selection.start}..${selection.end} ($cause)';
                      });
                    },
              ),
            ),
          ),
          Text(
            'Single selection: $_singleSelection',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text(
            'SelectionArea subtree',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          TextSelectionTheme(
            data: const TextSelectionThemeData(
              cursorColor: Colors.green,
              selectionColor: Color(0x66008080),
            ),
            child: SelectionArea(
              onSelectionChanged: (SelectedContent? content) {
                setState(() => _areaSelection = content?.plainText ?? 'none');
              },
              child: SelectionListener(
                selectionNotifier: _selectionNotifier,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: const Color(0xFFF4FBF8),
                    border: Border.all(color: const Color(0xFF80CBC4)),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Padding(
                    padding: EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      spacing: 6,
                      children: <Widget>[
                        Text('SelectionArea coordinates selection across'),
                        Text('multiple Text widgets in one subtree.'),
                        Row(
                          spacing: 8,
                          children: <Widget>[
                            Text('It also works'),
                            Text('across a Row.'),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
          Text(
            'Area selection: $_areaSelection',
            maxLines: 3,
            style: const TextStyle(fontSize: 13),
          ),
          Text(
            'SelectionListener: $_listenerDetails',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text(
            'DefaultSelectionStyle scope',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          const DefaultSelectionStyle(
            cursorColor: Colors.deepOrange,
            selectionColor: Color(0x66FF5722),
            mouseCursor: SystemMouseCursors.click,
            child: SelectableText(
              'Cursor, selection, and mouse cursor inherit from the core selection style.',
            ),
          ),
          const Divider(),
          const Text(
            'TextSelectionTheme on TextField',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          const TextSelectionTheme(
            data: TextSelectionThemeData(
              cursorColor: Color(0xFF7B1FA2),
              selectionColor: Color(0x667B1FA2),
              selectionHandleColor: Color(0xFF7B1FA2),
            ),
            child: TextField(
              decoration: InputDecoration(
                labelText: 'Themed cursor, selection, and handles',
              ),
            ),
          ),
          const TextField(
            cursorColor: Color(0xFF1565C0),
            cursorErrorColor: Color(0xFFB3261E),
            decoration: InputDecoration(
              labelText: 'Explicit cursorColor',
              errorText: 'An errored field paints cursorErrorColor',
            ),
          ),
        ],
      ),
    );
  }
}
