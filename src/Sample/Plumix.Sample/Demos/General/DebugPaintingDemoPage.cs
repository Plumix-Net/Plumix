using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/debug_painting_demo_page.dart (exact sample parity)

public sealed class DebugPaintingDemoPage : StatefulWidget
{
    public override State CreateState() => new DebugPaintingDemoPageState();
}

internal sealed class DebugPaintingDemoPageState : State
{
    private bool _showPlaceholderChild;
    private bool _customGrid;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Placeholder + GridPaper", fontSize: 20, color: Colors.Black),
                new Text(
                    "Placeholder uses fallback dimensions only in unbounded space; GridPaper paints over its child.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _showPlaceholderChild ? "Remove child" : "Add child",
                            () => SetState(() => _showPlaceholderChild = !_showPlaceholderChild)),
                        BuildButton(
                            _customGrid ? "Default grid" : "Custom grid",
                            () => SetState(() => _customGrid = !_customGrid)),
                    ]),
                new Expanded(
                    child: new Row(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 16,
                        children:
                        [
                            new Expanded(child: BuildPlaceholderProbe()),
                            new Expanded(child: BuildGridPaperProbe()),
                        ])),
            ]);
    }

    private Widget BuildPlaceholderProbe()
    {
        return new Container(
            color: Colors.White,
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text("Unbounded fallback: 160 × 120", fontSize: 12, color: Colors.DarkSlateGray),
                    new Expanded(
                        child: new Align(
                            alignment: Alignment.TopLeft,
                            child: new UnconstrainedBox(
                                alignment: Alignment.TopLeft,
                                child: new Placeholder(
                                    color: Color.Parse("#FF455A64"),
                                    strokeWidth: 2,
                                    fallbackWidth: 160,
                                    fallbackHeight: 120,
                                    child: _showPlaceholderChild
                                        ? new Container(
                                            width: 96,
                                            height: 56,
                                            color: Color.Parse("#FFFFE8A3"),
                                            alignment: Alignment.Center,
                                            child: new Text("child", fontSize: 14, color: Colors.Black))
                                        : null)))),
                ]));
    }

    private Widget BuildGridPaperProbe()
    {
        return new Container(
            color: Colors.White,
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(
                        _customGrid ? "interval=60, divisions=3, subdivisions=2" : "Flutter defaults",
                        fontSize: 12,
                        color: Colors.DarkSlateGray),
                    new Expanded(
                        child: new GridPaper(
                            color: _customGrid ? Color.Parse("#7FFF8A65") : null,
                            interval: _customGrid ? 60 : 100,
                            divisions: _customGrid ? 3 : 2,
                            subdivisions: _customGrid ? 2 : 5,
                            child: new Container(
                                color: Color.Parse("#FFF2F7FA"),
                                alignment: Alignment.Center,
                                child: new Text("foreground grid", fontSize: 14, color: Colors.Black)))),
                ]));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 120,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}
