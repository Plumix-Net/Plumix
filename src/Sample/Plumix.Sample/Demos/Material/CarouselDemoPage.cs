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
                new Text("CarouselView", fontSize: 20, color: Colors.Black),
                new Text(
                    "Fixed and weighted Material 3 carousels with item snapping and CarouselViewTheme.",
                    fontSize: 14,
                    color: new Color(0x8A000000)),
                new Text("Fixed extent", fontSize: 14, color: Colors.Black),
                new SizedBox(height: 132, child: new CarouselView(
                    itemExtent: 176,
                    itemSnapping: true,
                    elevation: 2,
                    children: BuildItems())),
                new Text("Hero [1, 7, 1] with shrinkExtent", fontSize: 14, color: Colors.Black),
                new SizedBox(height: 132, child: CarouselView.Weighted(
                    flexWeights: [1, 7, 1],
                    itemSnapping: true,
                    consumeMaxWeight: false,
                    shrinkExtent: 40,
                    children: BuildItems())),
                new Text("Weighted [1, 6, 1]", fontSize: 14, color: Colors.Black),
                new SizedBox(height: 132, child: new CarouselViewTheme(
                    new CarouselViewThemeData(
                        Padding: new Thickness(6),
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
                child: new Center(new Text($"Item {index + 1}", fontSize: 18, color: Colors.White))));
        }

        return items;
    }
}
