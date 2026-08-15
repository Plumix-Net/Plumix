using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/center_viewport_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CenterViewportDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CenterViewportDemoPageState();
    }
}

internal sealed class CenterViewportDemoPageState : State
{
    private static readonly Key CenterKey = new ValueKey<string>("center-sliver");

    private readonly ScrollController _controller = new();
    private int _before = 5;
    private int _after = 5;

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("CustomScrollView center", fontSize: 20, color: Colors.Black),
                new Text(
                    "Slivers before the center key grow in the reverse direction and live at "
                    + "negative scroll offsets.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Prepend",
                                onTap: () => SetState(() => _before++),
                                background: Colors.SteelBlue,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Append",
                                onTap: () => SetState(() => _after++),
                                background: Colors.SeaGreen,
                                foreground: Colors.White,
                                fontSize: 13)),
                        new Expanded(
                            child: new CounterTapButton(
                                label: "Back to center",
                                onTap: () => _controller.JumpTo(0),
                                background: Colors.DarkSlateGray,
                                foreground: Colors.White,
                                fontSize: 13)),
                    ]),
                new Expanded(
                    child: new CustomScrollView(
                        controller: _controller,
                        center: CenterKey,
                        slivers:
                        [
                            new SliverList(
                                new SliverChildListDelegate(
                                    Enumerable.Range(1, _before)
                                        .Select(index => Row(-index, Color.Parse("#FFFFE0E0")))
                                        .ToList())),
                            new SliverToBoxAdapter(
                                key: CenterKey,
                                child: new Container(
                                    color: Color.Parse("#FF263238"),
                                    padding: new Thickness(12, 10),
                                    child: new Text("center (offset 0)", fontSize: 14, color: Colors.White))),
                            new SliverList(
                                new SliverChildListDelegate(
                                    Enumerable.Range(1, _after)
                                        .Select(index => Row(index, Color.Parse("#FFE0F2E9")))
                                        .ToList())),
                        ])),
            ]);
    }

    private static Widget Row(int index, Color background)
    {
        return new Container(
            height: 44,
            color: background,
            padding: new Thickness(12, 8),
            child: new Text($"item {index}", fontSize: 13, color: Colors.Black));
    }
}
