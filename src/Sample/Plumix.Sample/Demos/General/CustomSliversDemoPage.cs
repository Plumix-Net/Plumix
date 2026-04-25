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
                new SliverToBoxAdapter(
                    new Container(
                        color: Colors.White,
                        padding: new Thickness(12),
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 6,
                            children:
                            [
                                new Text("CustomScrollView + Slivers", fontSize: 20, color: Colors.Black),
                                new Text(
                                    "SliverPadding and SliverFixedExtentList are used directly.",
                                    fontSize: 14,
                                    color: Colors.DimGray),
                            ]))),
                new SliverPadding(
                    padding: new Thickness(12, 10, 12, 8),
                    sliver: SliverFixedExtentList.Builder(
                        childCount: 18,
                        itemExtent: 42,
                        itemBuilder: (_, index) => new Container(
                            color: index % 2 == 0 ? Color.Parse("#FFE8F5E9") : Color.Parse("#FFEAF4FF"),
                            padding: new Thickness(10, 8),
                            child: new Text(
                                $"fixed sliver row #{index}",
                                fontSize: 13,
                                color: Colors.Black)),
                        addAutomaticKeepAlives: false)),
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
