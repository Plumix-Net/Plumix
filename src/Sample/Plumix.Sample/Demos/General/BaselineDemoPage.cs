using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/baseline_demo_page.dart (exact sample parity)

public sealed class BaselineDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Baseline + IgnoreBaseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "The guide is 48 px from the top. Text uses its real alphabetic baseline; the box falls back " +
                    "to its bottom edge.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildBaselinePreview(),
                new Text(
                    "IgnoreBaseline keeps the tall middle child out of Row baseline calculations.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    color: Color.Parse("#FFF1F5F9"),
                    padding: new Thickness(12),
                    child: new Row(
                        crossAxisAlignment: CrossAxisAlignment.Baseline,
                        textBaseline: TextBaseline.Alphabetic,
                        spacing: 12,
                        children:
                        [
                            new Text("Aa", fontSize: 34, color: Color.Parse("#FF1D3557")),
                            new IgnoreBaseline(
                                child: new Container(
                                    width: 32,
                                    height: 52,
                                    color: Color.Parse("#FFE9C46A"))),
                            new Text("baseline", fontSize: 16, color: Color.Parse("#FF2A9D8F")),
                        ])),
            ]);
    }

    private static Widget BuildBaselinePreview()
    {
        return new Container(
            height: 118,
            color: Color.Parse("#FFE7EDF6"),
            padding: new Thickness(12),
            child: new Stack(
                clipBehavior: Clip.None,
                children:
                [
                    new Positioned(
                        left: 0,
                        right: 0,
                        top: 48,
                        height: 1,
                        child: new Container(color: Color.Parse("#FFE63946"))),
                    new Baseline(
                        baseline: 48,
                        baselineType: TextBaseline.Alphabetic,
                        child: new Text("Plumix", fontSize: 36, color: Color.Parse("#FF1D3557"))),
                    new Positioned(
                        left: 150,
                        child: new Baseline(
                            baseline: 48,
                            baselineType: TextBaseline.Alphabetic,
                            child: new Container(
                                width: 54,
                                height: 28,
                                color: Color.Parse("#FF2A9D8F")))),
                ]));
    }
}
