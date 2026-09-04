using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/nested_scroll_view_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class NestedScrollViewDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new NestedScrollView(
            headerSliverBuilder: (headerContext, innerBoxIsScrolled) =>
            [
                new SliverOverlapAbsorber(
                    handle: NestedScrollView.SliverOverlapAbsorberHandleFor(headerContext),
                    sliver: new SliverPersistentHeader(
                        pinned: true,
                        @delegate: new NestedScrollViewHeaderDelegate(innerBoxIsScrolled))),
                new SliverToBoxAdapter(
                    new Container(
                        color: Color.Parse("#FFE8EAF6"),
                        padding: new Thickness(16, 14),
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 4,
                            children:
                            [
                                new Text("Outer header sliver", fontSize: 18, color: Colors.Black),
                                new Text(
                                    "This scrolls away completely before the body starts scrolling.",
                                    fontSize: 13,
                                    color: Colors.DimGray),
                            ]))),
            ],
            body: new Builder(bodyContext => new CustomScrollView(
            [
                new SliverOverlapInjector(
                    NestedScrollView.SliverOverlapAbsorberHandleFor(bodyContext)),
                SliverFixedExtentList.Builder(
                    itemCount: 40,
                    itemExtent: 46,
                    itemBuilder: (_, index) => new Container(
                        color: index % 2 == 0
                            ? Color.Parse("#FFF5F5F5")
                            : Color.Parse("#FFFFFFFF"),
                        padding: new Thickness(16, 12),
                        child: new Text($"body row #{index}", fontSize: 14, color: Colors.Black)),
                    addAutomaticKeepAlives: false),
            ])));
    }
}

/// <summary>The pinned header of the nested scroll view demo, which reacts to the body scrolling.</summary>
internal sealed class NestedScrollViewHeaderDelegate(bool innerBoxIsScrolled) : SliverPersistentHeaderDelegate
{
    public override double MinExtent => 72;

    public override double MaxExtent => 72;

    public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
    {
        return new Container(
            height: 72,
            color: innerBoxIsScrolled
                ? Color.Parse("#FF90CAF9")
                : Color.Parse("#FFBBDEFB"),
            padding: new Thickness(16, 12),
            child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 2,
                children:
                [
                    new Text("NestedScrollView", fontSize: 20, color: Colors.Black),
                    new Text(
                        innerBoxIsScrolled ? "innerBoxIsScrolled: true" : "innerBoxIsScrolled: false",
                        fontSize: 13,
                        color: Colors.DimGray),
                ]));
    }

    public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate)
    {
        return oldDelegate is not NestedScrollViewHeaderDelegate other
               || other.InnerBoxIsScrolled != innerBoxIsScrolled;
    }

    private bool InnerBoxIsScrolled => innerBoxIsScrolled;
}
