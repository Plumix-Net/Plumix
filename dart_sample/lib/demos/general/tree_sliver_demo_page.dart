import 'package:flutter/rendering.dart' show TreeSliverIndentationType;
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class TreeSliverDemoPage extends StatefulWidget {
  const TreeSliverDemoPage({super.key});

  @override
  State<TreeSliverDemoPage> createState() => _TreeSliverDemoPageState();
}

class _TreeSliverDemoPageState extends State<TreeSliverDemoPage> {
  static const List<String> _indentationNames = <String>[
    'standard',
    'none',
    'custom(32)',
  ];

  static final List<TreeSliverIndentationType> _indentations =
      <TreeSliverIndentationType>[
        TreeSliverIndentationType.standard,
        TreeSliverIndentationType.none,
        TreeSliverIndentationType.custom(32.0),
      ];

  final TreeSliverController _controller = TreeSliverController();
  final List<TreeSliverNode<String>> _tree = _buildTree();
  int _indentationIndex = 0;
  bool _animate = true;
  bool _rowToggles = false;
  int _addedCount = 0;
  String _lastToggled = 'none';

  static List<TreeSliverNode<String>> _buildTree() => <TreeSliverNode<String>>[
    TreeSliverNode<String>('README.md'),
    TreeSliverNode<String>(
      'lib',
      expanded: true,
      children: <TreeSliverNode<String>>[
        TreeSliverNode<String>('main.dart'),
        TreeSliverNode<String>(
          'src',
          children: <TreeSliverNode<String>>[
            TreeSliverNode<String>(
              'widgets',
              children: <TreeSliverNode<String>>[
                TreeSliverNode<String>('tree_sliver.dart'),
                TreeSliverNode<String>('sliver.dart'),
              ],
            ),
            TreeSliverNode<String>(
              'rendering',
              children: <TreeSliverNode<String>>[
                TreeSliverNode<String>('sliver_tree.dart'),
              ],
            ),
          ],
        ),
      ],
    ),
    TreeSliverNode<String>(
      'test',
      children: <TreeSliverNode<String>>[
        TreeSliverNode<String>('sliver_tree_test.dart'),
      ],
    ),
    TreeSliverNode<String>('pubspec.yaml'),
  ];

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'TreeSliver',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Tap an arrow to expand or collapse a node. Children slide out from '
          'under their parent while rows below it move with them.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Expand all',
              onTap: () => _controller.expandAll(),
            ),
            _buildButton(
              label: 'Collapse all',
              onTap: () => _controller.collapseAll(),
            ),
            _buildButton(
              label: 'Indent: ${_indentationNames[_indentationIndex]}',
              onTap: () {
                setState(() {
                  _indentationIndex =
                      (_indentationIndex + 1) % _indentations.length;
                });
              },
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _animate ? 'Animation: on' : 'Animation: off',
              onTap: () => setState(() => _animate = !_animate),
            ),
            _buildButton(
              label: _rowToggles ? 'Tap: whole row' : 'Tap: arrow',
              onTap: () => setState(() => _rowToggles = !_rowToggles),
            ),
            _buildButton(
              label: 'Add to lib',
              onTap: () {
                setState(() {
                  _addedCount++;
                  _tree[1].children.add(
                    TreeSliverNode<String>('added_$_addedCount.dart'),
                  );
                });
              },
            ),
          ],
        ),
        Text(
          'last toggled=$_lastToggled',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Center(
          child: Container(
            width: 360,
            height: 280,
            color: const Color(0xFFF3F6FA),
            child: CustomScrollView(
              slivers: <Widget>[
                TreeSliver<String>(
                  tree: _tree,
                  controller: _controller,
                  indentation: _indentations[_indentationIndex],
                  toggleAnimationStyle: _animate
                      ? null
                      : AnimationStyle.noAnimation,
                  treeNodeBuilder: _rowToggles
                      ? _buildToggleRow
                      : TreeSliver.defaultTreeNodeBuilder,
                  onNodeToggle: (TreeSliverNode<Object?> node) {
                    setState(() {
                      _lastToggled =
                          '${node.content} '
                          '(${node.isExpanded ? 'expanded' : 'collapsed'})';
                    });
                  },
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  static Widget _buildToggleRow(
    BuildContext context,
    TreeSliverNode<Object?> node,
    AnimationStyle animationStyle,
  ) {
    return TreeSliver.wrapChildToToggleNode(
      node: node,
      child: TreeSliver.defaultTreeNodeBuilder(context, node, animationStyle),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 140,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }
}
