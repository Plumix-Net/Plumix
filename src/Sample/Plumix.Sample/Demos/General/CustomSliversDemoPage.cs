using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
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
                new PinnedHeaderSliver(
                    new Container(
                        height: 88,
                        color: Color.Parse("#FFF8FAFF"),
                        padding: new Thickness(12, 10),
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 4,
                            children:
                            [
                                new Text("PinnedHeaderSliver", fontSize: 20, color: Colors.Black),
                                new Text(
                                    "This measured header remains pinned while the decorated list scrolls behind it.",
                                    fontSize: 14,
                                    color: Colors.DimGray),
                            ]))),
                new DecoratedSliver(
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FFEAF4FF"),
                        Border: new BorderSide(Color.Parse("#FF90CAF9"), 2),
                        BorderRadius: BorderRadius.Circular(18)),
                    sliver: new SliverPadding(
                        padding: new Thickness(12, 10, 12, 8),
                        sliver: SliverFixedExtentList.Builder(
                            childCount: 18,
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
                new SliverPadding(
                    padding: new Thickness(12, 8, 12, 16),
                    sliver: SliverList.Builder(
                        childCount: 8,
                        itemBuilder: (_, index) => new Container(
                            color: Color.Parse("#FFF5F5F5"),
                            padding: new Thickness(10, 10),
                            child: new Text(
                                $"regular sliver row #{index}",
                                fontSize: 13,
                                color: Colors.Black)),
                        addAutomaticKeepAlives: false)),
            ]);
    }
}
