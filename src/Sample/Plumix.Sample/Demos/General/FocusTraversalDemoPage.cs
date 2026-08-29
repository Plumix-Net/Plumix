using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/focus_traversal_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class FocusTraversalDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new FocusTraversalDemoPageState();
    }
}

internal enum FocusTraversalDemoPolicy
{
    ReadingOrder,
    WidgetOrder,
    Ordered,
}

internal sealed class FocusTraversalDemoPageState : State
{
    private static readonly string[] TileLabels = ["A", "B", "C", "D", "E", "F"];

    private readonly List<FocusNode> _nodes = [];
    private FocusTraversalDemoPolicy _policy = FocusTraversalDemoPolicy.ReadingOrder;
    private string _focused = "none";

    public override void InitState()
    {
        base.InitState();
        foreach (string label in TileLabels)
        {
            var node = new FocusNode(debugLabel: label);
            _nodes.Add(node);
        }
    }

    public override void Dispose()
    {
        foreach (FocusNode node in _nodes)
        {
            node.Dispose();
        }

        _nodes.Clear();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("FocusTraversalGroup + policies", fontSize: 20, color: Colors.Black),
                new Text(
                    "Tab and Shift+Tab walk the sorted order; the arrow keys use the geometric "
                    + "directional policy. Tile E is excluded from traversal but stays focusable.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: PolicyLabel,
                                onTap: CyclePolicy,
                                background: Colors.SteelBlue,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Next",
                                onTap: () => _ = FocusManager.Instance.PrimaryFocus?.NextFocus(),
                                background: Colors.SeaGreen,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Previous",
                                onTap: () => _ = FocusManager.Instance.PrimaryFocus?.PreviousFocus(),
                                background: Colors.SlateGray,
                                foreground: Colors.White,
                                fontSize: 13)),
                    ]),
                new FocusTraversalGroup(
                    policy: CreatePolicy(),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Row(spacing: 8, children: [Tile(0), Tile(1), Tile(2)]),
                            new Row(spacing: 8, children: [Tile(3), Tile(4), Tile(5)]),
                        ])),
                new Text($"Focused tile: {_focused}", fontSize: 14, color: Colors.Black),
            ]);
    }

    private string PolicyLabel => _policy switch
    {
        FocusTraversalDemoPolicy.WidgetOrder => "Policy: widget order",
        FocusTraversalDemoPolicy.Ordered => "Policy: numeric order",
        _ => "Policy: reading order",
    };

    private FocusTraversalPolicy CreatePolicy() => _policy switch
    {
        FocusTraversalDemoPolicy.WidgetOrder => new WidgetOrderTraversalPolicy(),
        FocusTraversalDemoPolicy.Ordered => new OrderedTraversalPolicy(),
        _ => new ReadingOrderTraversalPolicy(),
    };

    private void CyclePolicy()
    {
        SetState(() =>
        {
            _policy = _policy switch
            {
                FocusTraversalDemoPolicy.ReadingOrder => FocusTraversalDemoPolicy.WidgetOrder,
                FocusTraversalDemoPolicy.WidgetOrder => FocusTraversalDemoPolicy.Ordered,
                _ => FocusTraversalDemoPolicy.ReadingOrder,
            };
        });
    }

    private Widget Tile(int index)
    {
        string label = TileLabels[index];
        FocusNode node = _nodes[index];
        Widget tile = new Expanded(
            child: new Focus(
                focusNode: node,
                autofocus: index == 0,
                onFocusChange: focused => HandleFocusChange(label, focused),
                child: new GestureDetector(
                    onTap: () => node.RequestFocus(),
                    child: new Container(
                        color: node.HasPrimaryFocus ? Colors.SteelBlue : Colors.Gainsboro,
                        padding: new Thickness(14, 18),
                        child: new Text(
                            label,
                            fontSize: 16,
                            color: node.HasPrimaryFocus ? Colors.White : Colors.Black,
                            textAlign: TextAlign.Center)))));

        // Tile E stays focusable by tap or by the arrow keys, but Tab skips it.
        if (label == "E")
        {
            tile = new ExcludeFocusTraversal(child: tile);
        }

        // The ordered policy sorts the bottom row before the top one.
        return new FocusTraversalOrder(
            order: new NumericFocusOrder(index < 3 ? index + 10 : index - 3),
            child: tile);
    }

    private void HandleFocusChange(string label, bool focused)
    {
        SetState(() =>
        {
            if (focused)
            {
                _focused = label;
            }
            else if (_focused == label)
            {
                _focused = "none";
            }
        });
    }
}
