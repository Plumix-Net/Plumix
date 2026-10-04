using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/tree_sliver_demo_page.dart (exact sample parity)

public sealed class TreeSliverDemoPage : StatefulWidget
{
    public override State CreateState() => new TreeSliverDemoPageState();
}

internal sealed class TreeSliverDemoPageState : State
{
    private static readonly string[] IndentationNames = ["standard", "none", "custom(32)"];

    private static readonly TreeSliverIndentationType[] Indentations =
    [
        TreeSliverIndentationType.Standard,
        TreeSliverIndentationType.None,
        TreeSliverIndentationType.Custom(32.0),
    ];

    private readonly TreeSliverController _controller = new();
    private readonly List<TreeSliverNode<string>> _tree = BuildTree();
    private int _indentationIndex;
    private bool _animate = true;
    private bool _rowToggles;
    private int _addedCount;
    private string _lastToggled = "none";

    private static List<TreeSliverNode<string>> BuildTree() =>
    [
        new("README.md"),
        new(
            "lib",
            expanded: true,
            children:
            [
                new("main.dart"),
                new(
                    "src",
                    children:
                    [
                        new("widgets", children: [new("tree_sliver.dart"), new("sliver.dart")]),
                        new("rendering", children: [new("sliver_tree.dart")]),
                    ]),
            ]),
        new("test", children: [new("sliver_tree_test.dart")]),
        new("pubspec.yaml"),
    ];

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("TreeSliver", fontSize: 20, color: Colors.Black),
                new Text(
                    "Tap an arrow to expand or collapse a node. Children slide out from under their "
                    + "parent while rows below it move with them.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Expand all", () => _controller.ExpandAll()),
                        BuildButton("Collapse all", () => _controller.CollapseAll()),
                        BuildButton(
                            $"Indent: {IndentationNames[_indentationIndex]}",
                            () => SetState(() =>
                                _indentationIndex = (_indentationIndex + 1) % Indentations.Length)),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _animate ? "Animation: on" : "Animation: off",
                            () => SetState(() => _animate = !_animate)),
                        BuildButton(
                            _rowToggles ? "Tap: whole row" : "Tap: arrow",
                            () => SetState(() => _rowToggles = !_rowToggles)),
                        BuildButton(
                            "Add to lib",
                            () => SetState(() =>
                            {
                                _addedCount++;
                                _tree[1].Children.Add(new TreeSliverNode<string>($"added_{_addedCount}.dart"));
                            })),
                    ]),
                new Text(
                    $"last toggled={_lastToggled}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Center(child: new Container(
                    width: 360,
                    height: 280,
                    color: new Color(0xFFF3F6FA),
                    child: new CustomScrollView(
                        slivers:
                        [
                            new TreeSliver<string>(
                                tree: _tree,
                                controller: _controller,
                                indentation: Indentations[_indentationIndex],
                                toggleAnimationStyle: _animate ? null : AnimationStyle.NoAnimation,
                                treeNodeBuilder: _rowToggles ? BuildToggleRow : TreeSliver.DefaultTreeNodeBuilder,
                                onNodeToggle: node => SetState(() =>
                                    _lastToggled = $"{node.Content} ({(node.IsExpanded ? "expanded" : "collapsed")})")),
                        ]))),
            ]);
    }

    private static Widget BuildToggleRow(BuildContext context, TreeSliverNode node, AnimationStyle animationStyle)
    {
        return TreeSliver.WrapChildToToggleNode(
            node,
            TreeSliver.DefaultTreeNodeBuilder(context, node, animationStyle));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 140,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: new Color(0xFFDCE3ED),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}
