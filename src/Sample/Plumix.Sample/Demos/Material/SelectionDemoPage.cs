using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/selection_demo_page.dart

public sealed class SelectionDemoPage : StatefulWidget
{
    public override State CreateState() => new SelectionDemoPageState();
}

internal sealed class SelectionDemoPageState : State
{
    private bool _interactive = true;
    private string _singleSelection = "none";
    private string _areaSelection = "none";

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("SelectableText + SelectionArea", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Drag across text, then right-click or long-press for the adaptive context menu. "
                        + "Double-tap selects a word and triple-tap a paragraph; long press raises the "
                        + "drag handles and the magnifier. Ctrl/Cmd+A and Ctrl/Cmd+C also work. "
                        + "The second probe spans several Text widgets.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new TextButton(
                            new Text($"Interactive: {_interactive}"),
                            () => SetState(() => _interactive = !_interactive))),
                    new Text("Single selectable run", fontSize: 18, color: Colors.Black),
                    new DecoratedBox(
                        decoration: new BoxDecoration(
                            Color: Color.Parse("#FFF7F2FA"),
                            Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FFCAC4D0"))),
                            BorderRadius: BorderRadius.Circular(12)),
                        child: new Padding(
                            insets: new Thickness(16),
                            child: new SelectableText(
                                "Plumix keeps selectable text in the framework render pipeline.",
                                style: new TextStyle(FontSize: 17, Height: 1.35),
                                showCursor: true,
                                enableInteractiveSelection: _interactive,
                                onSelectionChanged: (selection, cause) => SetState(() =>
                                    _singleSelection = selection.IsCollapsed
                                        ? "none"
                                        : $"{selection.Start}..{selection.End} ({cause})")))),
                    new Text($"Single selection: {_singleSelection}", fontSize: 13),
                    new Divider(),
                    new Text("SelectionArea subtree", fontSize: 18, color: Colors.Black),
                    new TextSelectionTheme(
                        data: new TextSelectionThemeData(
                            CursorColor: Colors.DarkGreen,
                            SelectionColor: Color.FromArgb(0x66, 0x00, 0x80, 0x80)),
                        child: new SelectionArea(
                            onSelectionChanged: content => SetState(() =>
                                _areaSelection = content?.PlainText ?? "none"),
                            child: new DecoratedBox(
                                decoration: new BoxDecoration(
                                    Color: Color.Parse("#FFF4FBF8"),
                                    Border: Plumix.Rendering.Border.FromBorderSide(
                                        new BorderSide(Color.Parse("#FF80CBC4"))),
                                    BorderRadius: BorderRadius.Circular(12)),
                                child: new Padding(
                                    insets: new Thickness(16),
                                    child: new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Start,
                                        spacing: 6,
                                        children:
                                        [
                                            new Text("SelectionArea coordinates selection across"),
                                            new Text("multiple Text widgets in one subtree."),
                                            new Row(
                                                spacing: 8,
                                                children:
                                                [
                                                    new Text("It also works"),
                                                    new Text("across a Row."),
                                                ]),
                                        ]))))),
                    new Text($"Area selection: {_areaSelection}", fontSize: 13, maxLines: 3),
                    new Divider(),
                    new Text("DefaultSelectionStyle scope", fontSize: 18, color: Colors.Black),
                    new DefaultSelectionStyle(
                        cursorColor: Color.Parse("#FFFF5722"),
                        selectionColor: Color.FromArgb(0x66, 0xFF, 0x57, 0x22),
                        mouseCursor: SystemMouseCursors.Click,
                        child: new SelectableText(
                            "Cursor, selection, and mouse cursor inherit from the core selection style.")),
                ]));
    }
}
