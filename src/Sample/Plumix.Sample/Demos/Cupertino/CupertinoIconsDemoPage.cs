using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_icons_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CupertinoIconsDemoPage : StatelessWidget
{
    private static readonly (string Name, IconData Icon)[] Samples =
    [
        ("heart_fill", CupertinoIcons.HeartFill),
        ("bell_fill", CupertinoIcons.BellFill),
        ("camera", CupertinoIcons.Camera),
        ("person_2_fill", CupertinoIcons.Person2Fill),
        ("map_fill", CupertinoIcons.MapFill),
        ("gear", CupertinoIcons.Gear),
        ("search", CupertinoIcons.Search),
        ("plus_circle", CupertinoIcons.PlusCircle),
        ("chevron_back", CupertinoIcons.ChevronBack),
        ("arrow_left_right", CupertinoIcons.ArrowLeftRight),
        ("waveform_path_ecg", CupertinoIcons.WaveformPathEcg),
        ("videocam_circle_fill", CupertinoIcons.VideocamCircleFill),
    ];

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino icons", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Representative legacy, SF Symbols, directional, alias, and high-range glyphs.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Wrap(
                    spacing: 12.0,
                    runSpacing: 12.0,
                    children:
                    [
                        .. Samples.Select(sample => BuildTile(sample.Name, sample.Icon)),
                    ]),
            ]);
    }

    private static Widget BuildTile(string name, IconData icon)
    {
        return new Container(
            width: 132.0,
            padding: new Thickness(12.0),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF2F2F7"),
                BorderRadius: BorderRadius.Circular(12.0)),
            child: new Column(
                spacing: 8.0,
                children:
                [
                    new Icon(icon, size: 34.0, color: Color.Parse("#FF007AFF")),
                    new Text(name, fontSize: 11.0, color: Colors.Black, textAlign: TextAlign.Center),
                ]));
    }
}
