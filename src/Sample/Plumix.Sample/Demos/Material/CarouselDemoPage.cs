using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/carousel_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CarouselDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("CarouselView", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Fixed and weighted Material 3 carousels with item snapping and CarouselViewTheme.",
                    style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                new Text("Fixed extent", style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                new SizedBox(height: 132, child: new CarouselView(
                    itemExtent: 176,
                    itemSnapping: true,
                    elevation: 2,
                    children: BuildItems())),
                new Text("Hero [1, 7, 1] with shrinkExtent", style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                new SizedBox(height: 132, child: CarouselView.Weighted(
                    flexWeights: [1, 7, 1],
                    itemSnapping: true,
                    consumeMaxWeight: false,
                    shrinkExtent: 40,
                    children: BuildItems())),
                new Text("Weighted [1, 6, 1]", style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                new SizedBox(height: 132, child: new CarouselViewTheme(
                    new CarouselViewThemeData(
                        Padding: EdgeInsets.All(6),
                        Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(20))),
                    CarouselView.Weighted(
                        flexWeights: [1, 6, 1],
                        itemSnapping: true,
                        children: BuildItems()))),
            ]);
    }

    private static IReadOnlyList<Widget> BuildItems()
    {
        Color[] colors =
        [
            new Color(0xFF6750A4),
            new Color(0xFF386A20),
            new Color(0xFF006874),
            new Color(0xFF9C4230),
            new Color(0xFF525F7A),
        ];
        List<Widget> items = [];
        for (int index = 0; index < colors.Length; index += 1)
        {
            items.Add(new ColoredBox(
                colors[index],
                child: new Center(new Text(
                    $"Item {index + 1}",
                    style: new TextStyle(
                        FontSize: 18,
                        Color: Colors.White)))));
        }

        return items;
    }
}
