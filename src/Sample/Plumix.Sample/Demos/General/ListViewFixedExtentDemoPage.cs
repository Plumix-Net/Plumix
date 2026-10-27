using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/list_view_fixed_extent_demo_page.dart
// (exact sample parity)

namespace Plumix;

public sealed class ListViewFixedExtentDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ListView with itemExtent", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Each row has fixed paint/layout extent and list has sliver padding.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                new Expanded(
                    child: ListView.Builder(
                        itemCount: 50,
                        itemExtent: 52,
                        padding: EdgeInsets.FromLTRB(16, 12, 16, 20),
                        itemBuilder: (_, index) => new Container(
                            color: index % 2 == 0 ? new Color(0xFFE8F5E9) : new Color(0xFFE3F2FD),
                            padding: EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                            child: new Text(
                                $"fixed row #{index} (itemExtent=52)",
                                style: new TextStyle(FontSize: 13, Color: Colors.Black))),
                        addAutomaticKeepAlives: false)),
            ]);
    }
}
