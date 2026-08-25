import 'package:material_ui/material_ui.dart';
import 'package:cupertino_ui/cupertino_ui.dart' as cupertino;

class DesktopTextSelectionToolbarDemoPage extends StatefulWidget {
  const DesktopTextSelectionToolbarDemoPage({super.key});

  @override
  State<DesktopTextSelectionToolbarDemoPage> createState() =>
      _DesktopTextSelectionToolbarDemoPageState();
}

class _DesktopTextSelectionToolbarDemoPageState
    extends State<DesktopTextSelectionToolbarDemoPage> {
  bool _nearViewportEdge = false;
  int _toolbarKind = 0;
  String _lastAction = 'None';

  @override
  Widget build(BuildContext context) {
    final Offset anchor = _nearViewportEdge
        ? const Offset(360, 260)
        : const Offset(24, 24);
    final Widget toolbar = switch (_toolbarKind) {
      1 => _buildMaterialToolbar(context, anchor),
      2 => _buildAdaptiveToolbar(context, anchor),
      3 => _buildSpellCheckToolbar(anchor),
      4 => _buildCupertinoToolbar(anchor),
      5 => _buildCupertinoDesktopToolbar(anchor),
      6 => _buildCupertinoSpellCheckToolbar(anchor),
      7 => _buildCupertinoOverflowToolbar(anchor),
      _ => _buildDesktopToolbar(context, anchor),
    };
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Material text selection toolbars',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material and Cupertino mobile, desktop, adaptive, and spell-check toolbars.',
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
                  _toolbarKind = (_toolbarKind + 1) % 8;
                });
              },
              child: Text('Show $_nextToolbarLabel'),
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

  Widget _buildAdaptiveToolbar(BuildContext context, Offset anchor) {
    final List<ContextMenuButtonItem> items = <ContextMenuButtonItem>[
      ContextMenuButtonItem(
        onPressed: () => _setAction('Cut'),
        type: ContextMenuButtonType.cut,
      ),
      ContextMenuButtonItem(
        onPressed: () => _setAction('Copy'),
        type: ContextMenuButtonType.copy,
      ),
      const ContextMenuButtonItem(
        onPressed: null,
        type: ContextMenuButtonType.paste,
      ),
      ContextMenuButtonItem(
        onPressed: () => _setAction('Select all'),
        type: ContextMenuButtonType.selectAll,
      ),
    ];
    return Theme(
      data: Theme.of(context).copyWith(platform: TargetPlatform.windows),
      child: AdaptiveTextSelectionToolbar.buttonItems(
        buttonItems: items,
        anchors: TextSelectionToolbarAnchors(
          primaryAnchor: anchor,
          secondaryAnchor: anchor + const Offset(0, 20),
        ),
      ),
    );
  }

  Widget _buildSpellCheckToolbar(Offset anchor) {
    return SpellCheckSuggestionsToolbar(
      anchor: anchor,
      buttonItems: <ContextMenuButtonItem>[
        ContextMenuButtonItem(
          onPressed: () => _setAction('framework'),
          label: 'framework',
        ),
        ContextMenuButtonItem(
          onPressed: () => _setAction('frameworks'),
          label: 'frameworks',
        ),
        ContextMenuButtonItem(
          onPressed: () => _setAction('Delete'),
          type: ContextMenuButtonType.delete,
        ),
      ],
    );
  }

  Widget _buildCupertinoToolbar(Offset anchor) {
    return cupertino.CupertinoTextSelectionToolbar(
      anchorAbove: anchor,
      anchorBelow: anchor + const Offset(0, 20),
      children: <Widget>[
        cupertino.CupertinoTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Cut'),
          text: 'Cut',
        ),
        cupertino.CupertinoTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Copy'),
          text: 'Copy',
        ),
        cupertino.CupertinoTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Paste'),
          text: 'Paste',
        ),
        const cupertino.CupertinoTextSelectionToolbarButton.text(
          onPressed: null,
          text: 'Disabled',
        ),
      ],
    );
  }

  Widget _buildCupertinoOverflowToolbar(Offset anchor) {
    const List<String> labels = <String>[
      'Cut',
      'Copy',
      'Paste',
      'Select all',
      'Look up',
      'Search web',
      'Share',
      'Translate',
      'Add to dictionary',
    ];
    return cupertino.CupertinoTextSelectionToolbar(
      anchorAbove: anchor,
      anchorBelow: anchor + const Offset(0, 20),
      children: <Widget>[
        for (final String label in labels)
          cupertino.CupertinoTextSelectionToolbarButton.text(
            onPressed: () => _setAction(label),
            text: label,
          ),
      ],
    );
  }

  Widget _buildCupertinoDesktopToolbar(Offset anchor) {
    return cupertino.CupertinoDesktopTextSelectionToolbar(
      anchor: anchor,
      children: <Widget>[
        cupertino.CupertinoDesktopTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Cut'),
          text: 'Cut',
        ),
        cupertino.CupertinoDesktopTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Copy'),
          text: 'Copy',
        ),
        cupertino.CupertinoDesktopTextSelectionToolbarButton.text(
          onPressed: () => _setAction('Paste'),
          text: 'Paste',
        ),
        const cupertino.CupertinoDesktopTextSelectionToolbarButton.text(
          onPressed: null,
          text: 'Disabled',
        ),
      ],
    );
  }

  Widget _buildCupertinoSpellCheckToolbar(Offset anchor) {
    return cupertino.CupertinoSpellCheckSuggestionsToolbar(
      anchors: TextSelectionToolbarAnchors(
        primaryAnchor: anchor,
        secondaryAnchor: anchor + const Offset(0, 20),
      ),
      buttonItems: <ContextMenuButtonItem>[
        ContextMenuButtonItem(
          onPressed: () => _setAction('framework'),
          label: 'framework',
        ),
        ContextMenuButtonItem(
          onPressed: () => _setAction('frameworks'),
          label: 'frameworks',
        ),
        const ContextMenuButtonItem(
          onPressed: null,
          label: 'No Replacements Found',
        ),
      ],
    );
  }

  String get _nextToolbarLabel => switch ((_toolbarKind + 1) % 8) {
    1 => 'Android',
    2 => 'adaptive',
    3 => 'spell check',
    4 => 'Cupertino mobile',
    5 => 'Cupertino desktop',
    6 => 'Cupertino spell check',
    7 => 'Cupertino overflow pages',
    _ => 'desktop',
  };

  void _setAction(String action) {
    setState(() {
      _lastAction = action;
    });
  }
}
