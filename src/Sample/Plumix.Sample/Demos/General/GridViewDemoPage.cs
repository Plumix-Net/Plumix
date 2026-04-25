using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/grid_view_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class GridViewDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("GridView + SliverGrid", fontSize: 20, color: Colors.Black),
                new Text(
                    "GridView uses SliverGrid with fixed-cross-axis delegate.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(
                    child: GridView.Builder(
                        itemCount: 60,
                        padding: new Thickness(12),
                        gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 3,
                            mainAxisSpacing: 8,
                            crossAxisSpacing: 8,
                            mainAxisExtent: 84),
                        itemBuilder: (_, index) => new GridTileItem(index),
                        addAutomaticKeepAlives: true)),
                new SizedBox(
                    height: 170,
                    child: new CustomScrollView(
                        slivers:
                        [
                            new SliverPadding(
                                padding: new Thickness(12, 0, 12, 10),
                                sliver: SliverGrid.Builder(
                                    childCount: 12,
                                    gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
                                        maxCrossAxisExtent: 140,
                                        crossAxisSpacing: 8,
                                        mainAxisSpacing: 8,
                                        childAspectRatio: 1.8),
                                    itemBuilder: (_, index) => new Container(
                                        color: index % 2 == 0 ? Color.Parse("#FFEAF4FF") : Color.Parse("#FFE8F5E9"),
                                        padding: new Thickness(8, 6),
                                        child: new Text($"sliver tile #{index}", fontSize: 12, color: Colors.Black)),
                                    addAutomaticKeepAlives: false)),
                        ])),
            ]);
    }
}

internal sealed class GridTileItem : StatefulWidget
{
    public GridTileItem(int index, Key? key = null) : base(key)
    {
        Index = index;
    }

    public int Index { get; }

    public override State CreateState()
    {
        return new GridTileItemState(Index);
    }
}

internal sealed class GridTileItemState : State
{
    private readonly int _index;
    private readonly int _token = Random.Shared.Next(1000, 9999);
    private int _taps;

    public GridTileItemState(int index)
    {
        _index = index;
    }

    public override Widget Build(BuildContext context)
    {
        var background = _index % 2 == 0 ? Color.Parse("#FFEAF4FF") : Colors.White;
        return new CounterTapButton(
            label: $"tile {_index}\nstate={_token}\ntaps={_taps}",
            onTap: () => SetState(() => _taps += 1),
            background: background,
            foreground: Colors.Black,
            fontSize: 12,
            padding: new Thickness(8, 6));
    }
}
