using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/safe_area_demo_page.dart (exact sample parity)

public sealed class SafeAreaDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("SafeArea", fontSize: 20, color: Colors.Black),
                new Text(
                    "The rose surface is the simulated system intrusion. The blue child keeps a minimum " +
                    "8 px inset and preserves the 28 px bottom view padding consumed by a keyboard.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildBoxPreview(),
                new Text(
                    "SliverSafeArea applies the same edge policy in sliver geometry.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(child: BuildSliverPreview()),
            ]);
    }

    private static Widget BuildBoxPreview()
    {
        return new MediaQuery(
            data: new MediaQueryData(
                Padding: new Thickness(24, 18, 32, 0),
                ViewPadding: new Thickness(24, 18, 32, 28)),
            child: new Container(
                height: 170,
                color: Color.Parse("#FFFFCDD2"),
                child: new SafeArea(
                    minimum: EdgeInsets.All(8),
                    maintainBottomViewPadding: true,
                    child: new Container(
                        alignment: Alignment.Center,
                        color: Color.Parse("#FFBBDEFB"),
                        child: new Text(
                            "Safe content\n24 left · 18 top · 32 right · 28 bottom",
                            fontSize: 15,
                            color: Colors.Black,
                            textAlign: TextAlign.Center)))));
    }

    private static Widget BuildSliverPreview()
    {
        return new MediaQuery(
            data: new MediaQueryData(
                Padding: new Thickness(16, 20, 24, 12),
                ViewPadding: new Thickness(16, 20, 24, 12)),
            child: new Container(
                color: Color.Parse("#FFFFE0E0"),
                child: new CustomScrollView(
                    slivers:
                    [
                        new SliverSafeArea(
                            minimum: EdgeInsets.All(8),
                            sliver: SliverFixedExtentList.Builder(
                                childCount: 8,
                                itemExtent: 44,
                                itemBuilder: (_, index) => new Container(
                                    alignment: Alignment.CenterLeft,
                                    color: index % 2 == 0
                                        ? Color.Parse("#FFE3F2FD")
                                        : Color.Parse("#FFFFFFFF"),
                                    padding: new Thickness(12, 8),
                                    child: new Text(
                                        $"safe sliver row #{index}",
                                        fontSize: 14,
                                        color: Colors.Black)),
                                addAutomaticKeepAlives: false)),
                    ])));
    }
}
