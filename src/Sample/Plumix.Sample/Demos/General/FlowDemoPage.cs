using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Plumix.UI;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/flow_demo_page.dart (exact sample parity)

public sealed class FlowDemoPage : StatefulWidget
{
    public override State CreateState() => new FlowDemoPageState();
}

internal sealed class FlowDemoPageState : State
{
    private bool _expanded;
    private int _count;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Flow + RepaintBoundary", fontSize: 20, color: Colors.Black),
                new Text(
                    "Flow positions children during paint; its default constructor isolates every child repaint.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _expanded ? "Collapse" : "Spread",
                            () => SetState(() => _expanded = !_expanded)),
                        BuildButton(
                            $"Boundary count: {_count}",
                            () => SetState(() => _count++)),
                    ]),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF3F6FA"),
                        alignment: Alignment.Center,
                        child: new SizedBox(
                            width: 300,
                            height: 170,
                            child: new Flow(
                                new DemoFlowDelegate(_expanded),
                                children:
                                [
                                    BuildTile("0", Color.Parse("#FF1565C0")),
                                    BuildTile("1", Color.Parse("#FF2E7D32")),
                                    BuildTile("2", Color.Parse("#FFF57C00")),
                                ])))),
                new RepaintBoundary(
                    child: new Container(
                        color: Colors.White,
                        padding: new Thickness(12),
                        child: new Text(
                            "Explicit RepaintBoundary keeps this footer in its own composited display list.",
                            fontSize: 12,
                            color: Colors.DarkSlateGray))),
            ]);
    }

    private static Widget BuildTile(string label, Color color)
    {
        return new Container(
            color: color,
            alignment: Alignment.Center,
            child: new Text(label, fontSize: 18, color: Colors.White));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 140,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}

internal sealed class DemoFlowDelegate(bool expanded) : FlowDelegate
{
    public bool Expanded { get; } = expanded;

    public override Size GetSize(BoxConstraints constraints)
    {
        return constraints.Constrain(new Size(300, 170));
    }

    public override BoxConstraints GetConstraintsForChild(int index, BoxConstraints constraints)
    {
        return BoxConstraints.TightFor(width: 72, height: 48);
    }

    public override void PaintChildren(FlowPaintingContext context)
    {
        for (int index = 0; index < context.ChildCount; index++)
        {
            double x = Expanded ? 24 + (index * 92) : 90 + (index * 18);
            double y = Expanded ? 60 : 38 + (index * 24);
            double opacity = Expanded && index == 2 ? 0.55 : 1.0;
            context.PaintChild(index, Matrix4.TranslationValues(x, y, 0.0), opacity);
        }
    }

    public override bool ShouldRepaint(FlowDelegate oldDelegate)
    {
        return oldDelegate is not DemoFlowDelegate previous || previous.Expanded != Expanded;
    }
}
