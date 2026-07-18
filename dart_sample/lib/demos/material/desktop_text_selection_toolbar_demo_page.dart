import 'package:flutter/material.dart';

class DesktopTextSelectionToolbarDemoPage extends StatefulWidget {
  const DesktopTextSelectionToolbarDemoPage({super.key});

  @override
  State<DesktopTextSelectionToolbarDemoPage> createState() =>
      _DesktopTextSelectionToolbarDemoPageState();
}

class _DesktopTextSelectionToolbarDemoPageState
    extends State<DesktopTextSelectionToolbarDemoPage> {
  bool _nearViewportEdge = false;
  String _lastAction = 'None';

  @override
  Widget build(BuildContext context) {
    final Offset anchor = _nearViewportEdge
        ? const Offset(360, 260)
        : const Offset(24, 24);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Desktop text selection toolbar',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          '222px card surface, viewport clamping, full-width actions, disabled state, and desktop cursor.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () {
                setState(() {
                  _nearViewportEdge = !_nearViewportEdge;
                });
              },
              child: Text(
                _nearViewportEdge ? 'Move to origin' : 'Move near edge',
              ),
            ),
            Text('Last action: $_lastAction'),
          ],
        ),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF3EDF7),
            child: DesktopTextSelectionToolbar(
              anchor: anchor,
              children: <Widget>[
                DesktopTextSelectionToolbarButton.text(
                  context: context,
                  onPressed: () => _setAction('Cut'),
                  text: 'Cut',
                ),
                DesktopTextSelectionToolbarButton.text(
                  context: context,
                  onPressed: () => _setAction('Copy'),
                  text: 'Copy',
                ),
                DesktopTextSelectionToolbarButton.text(
                  context: context,
                  onPressed: () => _setAction('Paste'),
                  text: 'Paste',
                ),
                DesktopTextSelectionToolbarButton.text(
                  context: context,
                  onPressed: null,
                  text: 'Disabled action',
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  void _setAction(String action) {
    setState(() {
      _lastAction = action;
    });
  }
}
