using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/desktop_text_selection_toolbar_demo_page.dart

public sealed class DesktopTextSelectionToolbarDemoPage : StatefulWidget
{
    public override State CreateState() => new DesktopTextSelectionToolbarDemoPageState();
}

internal sealed class DesktopTextSelectionToolbarDemoPageState : State
{
    private bool _nearViewportEdge;
    private int _toolbarKind;
    private string _lastAction = "None";

    public override Widget Build(BuildContext context)
    {
        var anchor = _nearViewportEdge ? new Point(360, 260) : new Point(24, 24);
        Widget toolbar = _toolbarKind switch
        {
            1 => BuildMaterialToolbar(context, anchor),
            2 => BuildAdaptiveToolbar(context, anchor),
            3 => BuildSpellCheckToolbar(anchor),
            4 => BuildCupertinoToolbar(anchor),
            5 => BuildCupertinoDesktopToolbar(anchor),
            6 => BuildCupertinoSpellCheckToolbar(anchor),
            7 => BuildCupertinoOverflowToolbar(anchor),
            _ => BuildDesktopToolbar(context, anchor),
        };
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Material text selection toolbars", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material and Cupertino mobile, desktop, adaptive, and spell-check toolbars.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new TextButton(
                            child: new Text(_nearViewportEdge ? "Move to origin" : "Move near edge"),
                            onPressed: () => SetState(() => _nearViewportEdge = !_nearViewportEdge)),
                        new TextButton(
                            child: new Text($"Show {NextToolbarLabel}"),
                            onPressed: () => SetState(() => _toolbarKind = (_toolbarKind + 1) % 8)),
                        new Text($"Last action: {_lastAction}"),
                    ]),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF3EDF7"),
                        child: toolbar)),
            ]);
    }

    private Widget BuildDesktopToolbar(BuildContext context, Point anchor)
    {
        return new DesktopTextSelectionToolbar(
            anchor: anchor,
            children:
            [
                DesktopTextSelectionToolbarButton.Text(context, () => SetAction("Cut"), "Cut"),
                DesktopTextSelectionToolbarButton.Text(context, () => SetAction("Copy"), "Copy"),
                DesktopTextSelectionToolbarButton.Text(context, () => SetAction("Paste"), "Paste"),
                DesktopTextSelectionToolbarButton.Text(context, null, "Disabled action"),
            ]);
    }

    private Widget BuildMaterialToolbar(BuildContext context, Point anchor)
    {
        string[] labels = ["Cut", "Copy", "Paste", "Select all", "Share", "Translate", "Search web"];
        TextDirection direction = Directionality.Of(context);
        var children = new List<Widget>(labels.Length);
        for (int index = 0; index < labels.Length; index++)
        {
            string label = labels[index];
            children.Add(new TextSelectionToolbarTextButton(
                child: new Text(label),
                padding: TextSelectionToolbarTextButton.GetPadding(index, labels.Length, direction),
                onPressed: label == "Translate" ? null : () => SetAction(label)));
        }

        return new TextSelectionToolbar(
            anchorAbove: anchor,
            anchorBelow: anchor + new Vector(0, 20),
            children: children);
    }

    private Widget BuildAdaptiveToolbar(BuildContext context, Point anchor)
    {
        ContextMenuButtonItem[] items =
        [
            new(() => SetAction("Cut"), ContextMenuButtonType.Cut),
            new(() => SetAction("Copy"), ContextMenuButtonType.Copy),
            new(null, ContextMenuButtonType.Paste),
            new(() => SetAction("Select all"), ContextMenuButtonType.SelectAll),
        ];
        ThemeData windowsTheme = Theme.Of(context) with { Platform = TargetPlatform.Windows };
        return new Theme(
            windowsTheme,
            AdaptiveTextSelectionToolbar.FromButtonItems(
                items,
                new TextSelectionToolbarAnchors(anchor, anchor + new Vector(0, 20))));
    }

    private Widget BuildSpellCheckToolbar(Point anchor)
    {
        return new SpellCheckSuggestionsToolbar(
            anchor: anchor,
            buttonItems:
            [
                new ContextMenuButtonItem(() => SetAction("framework"), label: "framework"),
                new ContextMenuButtonItem(() => SetAction("frameworks"), label: "frameworks"),
                new ContextMenuButtonItem(() => SetAction("Delete"), ContextMenuButtonType.Delete),
            ]);
    }

    private Widget BuildCupertinoToolbar(Point anchor)
    {
        return new CupertinoTextSelectionToolbar(
            anchorAbove: anchor,
            anchorBelow: anchor + new Vector(0, 20),
            children:
            [
                CupertinoTextSelectionToolbarButton.TextButton(() => SetAction("Cut"), "Cut"),
                CupertinoTextSelectionToolbarButton.TextButton(() => SetAction("Copy"), "Copy"),
                CupertinoTextSelectionToolbarButton.TextButton(() => SetAction("Paste"), "Paste"),
                CupertinoTextSelectionToolbarButton.TextButton(null, "Disabled"),
            ]);
    }

    private Widget BuildCupertinoOverflowToolbar(Point anchor)
    {
        string[] labels =
        [
            "Cut", "Copy", "Paste", "Select all", "Look up", "Search web", "Share", "Translate",
            "Add to dictionary",
        ];
        var children = new List<Widget>(labels.Length);
        foreach (string label in labels)
        {
            children.Add(CupertinoTextSelectionToolbarButton.TextButton(() => SetAction(label), label));
        }

        return new CupertinoTextSelectionToolbar(
            anchorAbove: anchor,
            anchorBelow: anchor + new Vector(0, 20),
            children: children);
    }

    private Widget BuildCupertinoDesktopToolbar(Point anchor)
    {
        return new CupertinoDesktopTextSelectionToolbar(
            anchor: anchor,
            children:
            [
                CupertinoDesktopTextSelectionToolbarButton.TextButton(() => SetAction("Cut"), "Cut"),
                CupertinoDesktopTextSelectionToolbarButton.TextButton(() => SetAction("Copy"), "Copy"),
                CupertinoDesktopTextSelectionToolbarButton.TextButton(() => SetAction("Paste"), "Paste"),
                CupertinoDesktopTextSelectionToolbarButton.TextButton(null, "Disabled"),
            ]);
    }

    private Widget BuildCupertinoSpellCheckToolbar(Point anchor)
    {
        return new CupertinoSpellCheckSuggestionsToolbar(
            anchors: new TextSelectionToolbarAnchors(anchor, anchor + new Vector(0, 20)),
            buttonItems:
            [
                new ContextMenuButtonItem(() => SetAction("framework"), label: "framework"),
                new ContextMenuButtonItem(() => SetAction("frameworks"), label: "frameworks"),
                new ContextMenuButtonItem(null, label: "No Replacements Found"),
            ]);
    }

    private string NextToolbarLabel => ((_toolbarKind + 1) % 8) switch
    {
        1 => "Android",
        2 => "adaptive",
        3 => "spell check",
        4 => "Cupertino mobile",
        5 => "Cupertino desktop",
        6 => "Cupertino spell check",
        7 => "Cupertino overflow pages",
        _ => "desktop",
    };

    private void SetAction(string action)
    {
        SetState(() => _lastAction = action);
    }
}
