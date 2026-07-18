using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
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
    private bool _showMaterialToolbar;
    private string _lastAction = "None";

    public override Widget Build(BuildContext context)
    {
        var anchor = _nearViewportEdge ? new Point(360, 260) : new Point(24, 24);
        Widget toolbar = _showMaterialToolbar
            ? BuildMaterialToolbar(context, anchor)
            : BuildDesktopToolbar(context, anchor);
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Material text selection toolbars", fontSize: 20, color: Colors.Black),
                new Text(
                    "Android overflow paging plus the 222px desktop card, anchor clamping, and disabled actions.",
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
                            child: new Text(_showMaterialToolbar ? "Show desktop" : "Show Android"),
                            onPressed: () => SetState(() => _showMaterialToolbar = !_showMaterialToolbar)),
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

    private void SetAction(string action)
    {
        SetState(() => _lastAction = action);
    }
}
