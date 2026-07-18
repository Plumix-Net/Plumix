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
  bool _showMaterialToolbar = false;
  String _lastAction = 'None';

  @override
  Widget build(BuildContext context) {
    final Offset anchor = _nearViewportEdge
        ? const Offset(360, 260)
        : const Offset(24, 24);
    final Widget toolbar = _showMaterialToolbar
        ? _buildMaterialToolbar(context, anchor)
        : _buildDesktopToolbar(context, anchor);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Material text selection toolbars',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Android overflow paging plus the 222px desktop card, anchor clamping, and disabled actions.',
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
            TextButton(
              onPressed: () {
                setState(() {
                  _showMaterialToolbar = !_showMaterialToolbar;
                });
              },
              child: Text(
                _showMaterialToolbar ? 'Show desktop' : 'Show Android',
              ),
            ),
            Text('Last action: $_lastAction'),
          ],
        ),
        Expanded(
          child: ColoredBox(color: const Color(0xFFF3EDF7), child: toolbar),
        ),
      ],
    );
  }

  Widget _buildDesktopToolbar(BuildContext context, Offset anchor) {
    return DesktopTextSelectionToolbar(
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
    );
  }

  Widget _buildMaterialToolbar(BuildContext context, Offset anchor) {
    const List<String> labels = <String>[
      'Cut',
      'Copy',
      'Paste',
      'Select all',
      'Share',
      'Translate',
      'Search web',
    ];
    return TextSelectionToolbar(
      anchorAbove: anchor,
      anchorBelow: anchor + const Offset(0, 20),
      children: List<Widget>.generate(labels.length, (int index) {
        final String label = labels[index];
        return TextSelectionToolbarTextButton(
          padding: TextSelectionToolbarTextButton.getPadding(
            index,
            labels.length,
          ),
          onPressed: label == 'Translate' ? null : () => _setAction(label),
          child: Text(label),
        );
      }),
    );
  }

  void _setAction(String action) {
    setState(() {
      _lastAction = action;
    });
  }
}
