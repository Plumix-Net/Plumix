using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/material_icons_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class MaterialIconsDemoPage : StatelessWidget
{
    private static readonly (string Name, IconData Icon)[] Variants =
    [
        ("alarm", Icons.Alarm),
        ("alarm_outlined", Icons.AlarmOutlined),
        ("alarm_rounded", Icons.AlarmRounded),
        ("alarm_sharp", Icons.AlarmSharp),
    ];

    private static readonly (string Name, IconData Icon)[] Samples =
    [
        ("home", Icons.Home),
        ("favorite", Icons.Favorite),
        ("shopping_cart_outlined", Icons.ShoppingCartOutlined),
        ("cloud_upload_rounded", Icons.CloudUploadRounded),
        ("rocket_launch", Icons.RocketLaunch),
        ("bookmark_outline", Icons.BookmarkOutline),
        ("zoom_out_map_rounded", Icons.ZoomOutMapRounded),
        ("auto_awesome_rounded", Icons.AutoAwesomeRounded),
    ];

    private static readonly (string Name, IconData Icon)[] Adaptive =
    [
        ("adaptive.arrow_back", Icons.Adaptive.ArrowBack),
        ("adaptive.flip_camera", Icons.Adaptive.FlipCamera),
        ("adaptive.more", Icons.Adaptive.More),
        ("adaptive.share", Icons.Adaptive.Share),
    ];

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Material icons", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "The full material_ui catalog: base, outlined, rounded, and sharp variants, "
                    + "aliases, high-range glyphs, and directional mirroring.",
                    fontSize: 14.0,
                    color: Color.Parse("#8A000000")),
                new Text("Style variants of one icon", fontSize: 14.0, color: Colors.Black),
                BuildRow(Variants),
                new Text("Catalog samples", fontSize: 14.0, color: Colors.Black),
                BuildRow(Samples),
                new Text($"Icons.adaptive on {PlatformDefaults.TargetPlatform}", fontSize: 14.0, color: Colors.Black),
                BuildRow(Adaptive),
                new Text("arrow_back mirrors with text direction", fontSize: 14.0, color: Colors.Black),
                new Row(
                    spacing: 12.0,
                    children:
                    [
                        new Directionality(
                            textDirection: TextDirection.Ltr,
                            child: BuildTile("ltr", Icons.ArrowBack)),
                        new Directionality(
                            textDirection: TextDirection.Rtl,
                            child: BuildTile("rtl", Icons.ArrowBack)),
                    ]),
            ]);
    }

    private static Widget BuildRow((string Name, IconData Icon)[] icons)
    {
        return new Wrap(
            spacing: 12.0,
            runSpacing: 12.0,
            children: [.. icons.Select(icon => BuildTile(icon.Name, icon.Icon))]);
    }

    private static Widget BuildTile(string name, IconData icon)
    {
        return new Container(
            width: 148.0,
            padding: new Thickness(12.0),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFEDE7F6"),
                BorderRadius: BorderRadius.Circular(12.0)),
            child: new Column(
                spacing: 8.0,
                children:
                [
                    new Icon(icon, size: 34.0, color: Color.Parse("#FF6200EE")),
                    new Text(name, fontSize: 11.0, color: Colors.Black, textAlign: TextAlign.Center),
                ]));
    }
}
