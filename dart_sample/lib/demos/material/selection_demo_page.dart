import 'package:material_ui/material_ui.dart';
import 'package:flutter/rendering.dart' show SelectedContent;

class SelectionDemoPage extends StatefulWidget {
  const SelectionDemoPage({super.key});

  @override
  State<SelectionDemoPage> createState() => _SelectionDemoPageState();
}

class _SelectionDemoPageState extends State<SelectionDemoPage> {
  bool _interactive = true;
  String _singleSelection = 'none';
  String _areaSelection = 'none';

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
          Text(
            'Area selection: $_areaSelection',
            maxLines: 3,
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
        ],
      ),
    );
  }
}
