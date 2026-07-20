using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/layout_builder_demo_page.dart (exact sample parity)

public sealed class LayoutBuilderDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("LayoutBuilder + OrientationBuilder", fontSize: 20, color: Colors.Black),
                new Text(
                    "LayoutBuilder receives its parent's live constraints. OrientationBuilder reduces those " +
                    "constraints to landscape or portrait.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    height: 96,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(12),
                    child: new LayoutBuilder((_, constraints) =>
                    {
                        bool isWide = constraints.MaxWidth >= 420;
                        string width = constraints.MaxWidth.ToString("0");
                        string height = constraints.MaxHeight.ToString("0");
                        return new Container(
                            color: isWide ? Color.Parse("#FF2A9D8F") : Color.Parse("#FFE76F51"),
                            alignment: Alignment.Center,
                            child: new Text(
                                $"{width} × {height} — {(isWide ? "wide" : "compact")}",
                                fontSize: 16,
                                color: Colors.White));
                    })),
                new Row(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 16,
                    children:
                    [
                        BuildOrientationProbe(width: 180, height: 80),
                        BuildOrientationProbe(width: 100, height: 150),
                    ]),
            ]);
    }

    private static Widget BuildOrientationProbe(double width, double height)
    {
        return new SizedBox(
            width: width,
            height: height,
            child: new OrientationBuilder((_, orientation) =>
            {
                bool isLandscape = orientation == Orientation.Landscape;
                return new Container(
                    color: isLandscape ? Color.Parse("#FF264653") : Color.Parse("#FF457B9D"),
                    alignment: Alignment.Center,
                    child: new Text(
                        isLandscape ? "landscape" : "portrait",
                        fontSize: 14,
                        color: Colors.White));
            }));
    }
}
