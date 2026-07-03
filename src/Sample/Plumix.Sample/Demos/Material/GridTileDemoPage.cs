using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/material/grid_tile_demo_page.dart (exact sample parity)

public sealed class GridTileDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("GridTile + GridTileBar", fontSize: 20, color: Colors.Black),
                new Text(
                    "Header/footer overlays, one/two-line bars, slots, transparent background, and RTL.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(
                    child: GridView.Builder(
                        itemCount: 4,
                        padding: new Thickness(12),
                        gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            mainAxisSpacing: 12,
                            crossAxisSpacing: 12,
                            mainAxisExtent: 150),
                        itemBuilder: (_, index) => BuildTile(index),
                        addAutomaticKeepAlives: false)),
            ]);
    }

    private static Widget BuildTile(int index)
    {
        var colors = new[]
        {
            Color.Parse("#FFD7E3FF"),
            Color.Parse("#FFFFD8E4"),
            Color.Parse("#FFD9F2E6"),
            Color.Parse("#FFFFE2C6"),
        };
        var content = new Container(
            color: colors[index],
            alignment: Alignment.Center,
            child: new Text($"Tile {index + 1}", fontSize: 18, color: Colors.Black));

        Widget tile = index switch
        {
            0 => new GridTile(
                child: content,
                header: new GridTileBar(
                    backgroundColor: Color.Parse("#CC000000"),
                    leading: new Icon(Icons.Star),
                    title: new Text("Header"))),
            1 => new GridTile(
                child: content,
                footer: new GridTileBar(
                    backgroundColor: Color.Parse("#CC000000"),
                    title: new Text("Footer"),
                    subtitle: new Text("Two lines"),
                    trailing: new Icon(Icons.InfoOutline))),
            2 => new GridTile(
                child: content,
                header: new GridTileBar(
                    backgroundColor: Color.Parse("#CC000000"),
                    leading: new Icon(Icons.Menu),
                    title: new Text("RTL")),
                footer: new GridTileBar(
                    backgroundColor: Color.Parse("#99000000"),
                    subtitle: new Text("header + footer"))),
            _ => new GridTile(
                child: content,
                footer: new GridTileBar(
                    title: new Text("Transparent"),
                    trailing: new Icon(Icons.StarOutline))),
        };

        return new Directionality(index == 2 ? TextDirection.Rtl : TextDirection.Ltr, tile);
    }
}
