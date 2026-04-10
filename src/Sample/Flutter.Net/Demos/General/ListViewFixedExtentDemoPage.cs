using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/list_view_fixed_extent_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class ListViewFixedExtentDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListView with itemExtent", fontSize: 20, color: Colors.Black),
                new Text(
                    "Each row has fixed paint/layout extent and list has sliver padding.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(
                    child: ListView.Builder(
                        itemCount: 50,
                        itemExtent: 52,
                        padding: new Thickness(16, 12, 16, 20),
                        itemBuilder: (_, index) => new Container(
                            color: index % 2 == 0 ? Color.Parse("#FFE8F5E9") : Color.Parse("#FFE3F2FD"),
                            padding: new Thickness(12, 8),
                            child: new Text(
                                $"fixed row #{index} (itemExtent=52)",
                                fontSize: 13,
                                color: Colors.Black)),
                        addAutomaticKeepAlives: false)),
            ]);
    }
}
