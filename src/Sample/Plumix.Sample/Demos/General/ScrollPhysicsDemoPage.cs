using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/scroll_physics_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ScrollPhysicsDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scroll physics", fontSize: 20, color: Colors.Black),
                new Text(
                    "Drag past either end: bouncing physics rubber-band and spring back, "
                    + "clamping physics stop at the edge, never-scrollable ignores the drag.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Text("Single child: horizontal strip with directional padding", fontSize: 14),
                new SizedBox(
                    height: 64,
                    child: new SingleChildScrollView(
                        scrollDirection: Axis.Horizontal,
                        padding: EdgeInsetsGeometry.DirectionalOnly(start: 24, end: 8),
                        clipBehavior: Plumix.UI.Clip.AntiAlias,
                        child: new Row(
                            spacing: 8,
                            children: Enumerable.Range(1, 12).Select(index => (Widget)new Container(
                                width: 100,
                                height: 56,
                                color: Color.Parse("#FFE3F2FD"),
                                alignment: Alignment.Center,
                                child: new Text($"item #{index}", fontSize: 14))).ToArray()))),
                new Expanded(
                    child: new Row(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 12,
                        children:
                        [
                            new Expanded(
                                child: BuildList(
                                    "Bouncing (iOS)",
                                    Color.Parse("#FFE8F5E9"),
                                    new BouncingScrollPhysics(
                                        parent: new RangeMaintainingScrollPhysics()))),
                            new Expanded(
                                child: BuildList(
                                    "Bouncing (fast)",
                                    Color.Parse("#FFFFF3E0"),
                                    new BouncingScrollPhysics(
                                        decelerationRate: ScrollDecelerationRate.Fast,
                                        parent: new RangeMaintainingScrollPhysics()))),
                            new Expanded(
                                child: BuildList(
                                    "Clamping (Android)",
                                    Color.Parse("#FFE3F2FD"),
                                    new ClampingScrollPhysics(
                                        parent: new RangeMaintainingScrollPhysics()))),
                            new Expanded(
                                child: BuildList(
                                    "Never (locked)",
                                    Color.Parse("#FFF3E5F5"),
                                    new NeverScrollableScrollPhysics())),
                        ])),
            ]);
    }

    private static Widget BuildList(string title, Color color, ScrollPhysics physics)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 6,
            children:
            [
                new Text(title, fontSize: 14, color: Colors.Black),
                new Expanded(
                    child: ListView.Builder(
                        itemCount: 24,
                        itemExtent: 44,
                        physics: physics,
                        itemBuilder: (_, index) => new Container(
                            color: index % 2 == 0 ? color : Colors.White,
                            padding: new Thickness(10, 8),
                            child: new Text($"row #{index}", fontSize: 13, color: Colors.Black)),
                        addAutomaticKeepAlives: false)),
            ]);
    }
}
