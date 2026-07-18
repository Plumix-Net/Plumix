using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
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
    private string _lastAction = "None";

    public override Widget Build(BuildContext context)
    {
        var anchor = _nearViewportEdge ? new Point(360, 260) : new Point(24, 24);
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Desktop text selection toolbar", fontSize: 20, color: Colors.Black),
                new Text(
                    "222px card surface, viewport clamping, full-width actions, disabled state, and desktop cursor.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new TextButton(
                            child: new Text(_nearViewportEdge ? "Move to origin" : "Move near edge"),
                            onPressed: () => SetState(() => _nearViewportEdge = !_nearViewportEdge)),
                        new Text($"Last action: {_lastAction}"),
                    ]),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF3EDF7"),
                        child: new DesktopTextSelectionToolbar(
                            anchor: anchor,
                            children:
                            [
                                DesktopTextSelectionToolbarButton.Text(
                                    context,
                                    () => SetAction("Cut"),
                                    "Cut"),
                                DesktopTextSelectionToolbarButton.Text(
                                    context,
                                    () => SetAction("Copy"),
                                    "Copy"),
                                DesktopTextSelectionToolbarButton.Text(
                                    context,
                                    () => SetAction("Paste"),
                                    "Paste"),
                                DesktopTextSelectionToolbarButton.Text(
                                    context,
                                    null,
                                    "Disabled action"),
                            ]))),
            ]);
    }

    private void SetAction(string action)
    {
        SetState(() => _lastAction = action);
    }
}
