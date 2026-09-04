using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/custom_slivers_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CustomSliversDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new CustomScrollView(
            slivers:
            [
                new SliverMainAxisGroup(
                    slivers:
                    [
                        new SliverResizingHeader(
                            minExtentPrototype: new SizedBox(height: 64),
                            maxExtentPrototype: new SizedBox(height: 140),
                            child: new Container(
                                color: Color.Parse("#FFE8EAF6"),
                                padding: new Thickness(16, 12),
                                child: new Column(
                                    mainAxisAlignment: MainAxisAlignment.Center,
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 4,
                                    children:
                                    [
                                        new Text(
                                            "SliverResizingHeader",
                                            fontSize: 20,
                                            color: Colors.Black),
                                        new Text(
                                            "140px → 64px prototype extents",
                                            fontSize: 13,
                                            color: Colors.DimGray),
                                    ]))),
                        SliverFixedExtentList.Builder(
                            itemCount: 5,
                            itemExtent: 44,
                            itemBuilder: (_, index) => BuildExtentCell(
                                $"resizing-header group row #{index}",
                                index % 2 == 0
                                    ? Color.Parse("#FFF5F5F5")
                                    : Color.Parse("#FFFFFFFF")),
                            addAutomaticKeepAlives: false),
                    ]),
                new SliverFloatingHeader(
                    snapMode: FloatingHeaderSnapMode.Overlay,
                    child: new Container(
                        height: 64,
                        color: Color.Parse("#FFFFECB3"),
                        padding: new Thickness(16, 10),
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 2,
                            children:
                            [
                                new Text("SliverFloatingHeader", fontSize: 18, color: Colors.Black),
                                new Text(
                                    "Reverse the scroll direction to reveal and snap it.",
                                    fontSize: 12,
                                    color: Colors.DimGray),
                            ]))),
                new SliverSafeArea(
                    minimum: new EdgeInsets(12, 8, 12, 0),
                    sliver: new SliverLayoutBuilder((_, constraints) =>
                    {
                        bool compact = constraints.CrossAxisExtent < 420;
                        double height = compact ? 104 : 88;
                        string width = constraints.CrossAxisExtent.ToString("0");
                        return new PinnedHeaderSliver(
                            new Container(
                                height: height,
                                color: Color.Parse("#FFF8FAFF"),
                                padding: new Thickness(12, 10),
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 4,
                                    children:
                                    [
                                        new Text(
                                            "SliverLayoutBuilder + SliverSafeArea",
                                            fontSize: 20,
                                            color: Colors.Black),
                                        new Text(
                                            $"{width}px safe cross-axis — {(compact ? "compact" : "wide")} header",
                                            fontSize: 14,
                                            color: Colors.DimGray),
                                    ])));
                    })),
                new SliverFillRemaining(
                    hasScrollBody: false,
                    child: new Container(
                        color: Color.Parse("#FFF3E5F5"),
                        padding: new Thickness(24),
                        alignment: Alignment.Center,
                        child: new Column(
                            mainAxisAlignment: MainAxisAlignment.Center,
                            spacing: 8,
                            children:
                            [
                                new Text("SliverFillRemaining", fontSize: 22, color: Colors.Black),
                                new Text(
                                    "Non-scrollable child fills the first viewport below the pinned header.",
                                    fontSize: 13,
                                    color: Colors.DimGray,
                                    textAlign: TextAlign.Center),
                            ]))),
                new SliverFillViewport(
                    viewportFraction: 0.55,
                    padEnds: true,
                    allowImplicitScrolling: false,
                    @delegate: new SliverChildListDelegate(
                        [
                            BuildViewportPage("viewport page 1", Color.Parse("#FFE3F2FD")),
                            BuildViewportPage("viewport page 2", Color.Parse("#FFE8F5E9")),
                            BuildViewportPage("viewport page 3", Color.Parse("#FFFFF3E0")),
                        ],
                        addAutomaticKeepAlives: false)),
                new DecoratedSliver(
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FFEAF4FF"),
                        Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FF90CAF9"), 2)),
                        BorderRadius: BorderRadius.Circular(18)),
                    sliver: new SliverPadding(
                        padding: new Thickness(12, 10, 12, 8),
                        sliver: SliverFixedExtentList.Builder(
                            itemCount: 8,
                            itemExtent: 42,
                            itemBuilder: (_, index) => new Container(
                                color: index % 2 == 0
                                    ? Color.Parse("#CCFFFFFF")
                                    : Color.Parse("#CCE8F5E9"),
                                padding: new Thickness(10, 8),
                                child: new Text(
                                    $"decorated sliver row #{index}",
                                    fontSize: 13,
                                    color: Colors.Black)),
                            addAutomaticKeepAlives: false))),
                new SliverMainAxisGroup(
                    slivers:
                    [
                        new PinnedHeaderSliver(
                            new Container(
                                height: 56,
                                color: Color.Parse("#FFFFF3E0"),
                                padding: new Thickness(12, 9),
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 2,
                                    children:
                                    [
                                        new Text("SliverMainAxisGroup", fontSize: 17, color: Colors.Black),
                                        new Text(
                                            "This header stops pinning at the end of its group.",
                                            fontSize: 12,
                                            color: Colors.DimGray),
                                    ]))),
                        new SliverCrossAxisGroup(
                            slivers:
                            [
                                SliverFixedExtentList.Builder(
                                    itemCount: 8,
                                    itemExtent: 38,
                                    itemBuilder: (_, index) => BuildGroupCell(
                                        $"1x #{index}",
                                        Color.Parse("#FFE3F2FD")),
                                    addAutomaticKeepAlives: false),
                                new SliverConstrainedCrossAxis(
                                    maxExtent: 96,
                                    sliver: SliverFixedExtentList.Builder(
                                        itemCount: 6,
                                        itemExtent: 46,
                                        itemBuilder: (_, index) => BuildGroupCell(
                                            $"96 #{index}",
                                            Color.Parse("#FFFFF9C4")),
                                        addAutomaticKeepAlives: false)),
                                new SliverCrossAxisExpanded(
                                    flex: 2,
                                    sliver: SliverFixedExtentList.Builder(
                                        itemCount: 10,
                                        itemExtent: 34,
                                        itemBuilder: (_, index) => BuildGroupCell(
                                            $"2x #{index}",
                                            Color.Parse("#FFE8F5E9")),
                                        addAutomaticKeepAlives: false)),
                            ]),
                    ]),
                new SliverPadding(
                    padding: new Thickness(12, 10, 12, 4),
                    sliver: SliverPrototypeExtentList.Builder(
                        itemCount: 4,
                        prototypeItem: new SizedBox(height: 54),
                        itemBuilder: (_, index) => BuildExtentCell(
                            $"prototype extent row #{index}",
                            index % 2 == 0
                                ? Color.Parse("#FFE8EAF6")
                                : Color.Parse("#FFF3E5F5")),
                        addAutomaticKeepAlives: false)),
                new SliverPadding(
                    padding: new Thickness(12, 4, 12, 8),
                    sliver: SliverVariedExtentList.Builder(
                        itemCount: 6,
                        itemExtentBuilder: (index, _) => index % 3 switch
                        {
                            0 => 38,
                            1 => 54,
                            _ => 46,
                        },
                        itemBuilder: (_, index) => BuildExtentCell(
                            $"varied extent row #{index}",
                            index % 2 == 0
                                ? Color.Parse("#FFE0F2F1")
                                : Color.Parse("#FFE8F5E9")),
                        addAutomaticKeepAlives: false)),
                new SliverPadding(
                    padding: new Thickness(12, 8, 12, 4),
                    sliver: SliverList.Builder(
                        itemCount: 8,
                        itemBuilder: (_, index) => new Container(
                            color: Color.Parse("#FFF5F5F5"),
                            padding: new Thickness(10, 10),
                            child: new Text(
                                $"regular sliver row #{index}",
                                fontSize: 13,
                                color: Colors.Black)),
                        addAutomaticKeepAlives: false)),
                new SliverPadding(
                    padding: new Thickness(12, 4, 12, 16),
                    sliver: SliverList.Separated(
                        itemCount: 5,
                        itemBuilder: (_, index) => new Container(
                            color: Color.Parse("#FFFFF3E0"),
                            padding: new Thickness(10, 10),
                            child: new Text(
                                $"separated sliver row #{index}",
                                fontSize: 13,
                                color: Colors.Black)),
                        separatorBuilder: (_, _) => new Container(
                            color: Color.Parse("#FFFFB74D"),
                            height: 2),
                        addAutomaticKeepAlives: false)),
            ]);
    }

    private static Widget BuildGroupCell(string label, Color color)
    {
        return new Container(
            color: color,
            padding: new Thickness(6, 8),
            child: new Text(label, fontSize: 12, color: Colors.Black));
    }

    private static Widget BuildViewportPage(string label, Color color)
    {
        return new Container(
            color: color,
            padding: new Thickness(20),
            alignment: Alignment.Center,
            child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                spacing: 6,
                children:
                [
                    new Text(label, fontSize: 20, color: Colors.Black),
                    new Text(
                        "55% of the viewport · padded ends",
                        fontSize: 13,
                        color: Colors.DimGray),
                ]));
    }

    private static Widget BuildExtentCell(string label, Color color)
    {
        return new Container(
            color: color,
            padding: new Thickness(10, 8),
            alignment: Alignment.CenterLeft,
            child: new Text(label, fontSize: 13, color: Colors.Black));
    }
}
