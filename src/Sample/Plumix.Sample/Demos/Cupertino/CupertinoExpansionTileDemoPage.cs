using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_expansion_tile_demo_page.dart
// (exact sample parity)

public sealed class CupertinoExpansionTileDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoExpansionTileDemoPageState();
}

internal sealed class CupertinoExpansionTileDemoPageState : State
{
    private readonly ExpansibleController _fadeController = new();
    private readonly ExpansibleController _scrollController = new();

    public override void Dispose()
    {
        _fadeController.Dispose();
        _scrollController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color secondaryLabel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);
        Color panel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondarySystemBackground, context);
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino list + expansion tiles", fontSize: 20.0, color: label),
                new Text(
                    "Compare base/notched rows, async activation, chevrons, and both expansion transitions.",
                    fontSize: 14.0,
                    color: secondaryLabel),
                new ClipRRect(
                    BorderRadius.Circular(14.0),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        children:
                        [
                            new CupertinoListTile(
                                title: new Text("Base list tile"),
                                subtitle: new Text("Subtitle and additional info"),
                                additionalInfo: new Text("Connected"),
                                leading: new Icon(CupertinoIcons.Wifi),
                                trailing: new CupertinoListTileChevron(),
                                backgroundColor: CupertinoColors.SystemBackground,
                                onTap: () => Task.CompletedTask),
                            CupertinoListTile.Notched(
                                title: new Text("Notched list tile"),
                                subtitle: new Text("Inset-grouped geometry"),
                                trailing: new CupertinoListTileChevron(),
                                backgroundColor: panel,
                                onTap: () => Task.CompletedTask),
                        ])),
                BuildExpansionTile(
                    "Fade transition",
                    "The fully extended body fades over the height animation.",
                    _fadeController,
                    ExpansionTileTransitionMode.Fade,
                    panel),
                BuildExpansionTile(
                    "Scroll transition",
                    "The body scrolls out from under the 44 px header.",
                    _scrollController,
                    ExpansionTileTransitionMode.Scroll,
                    panel),
                new Row(
                    mainAxisAlignment: MainAxisAlignment.SpaceEvenly,
                    children:
                    [
                        new CupertinoButton(
                            child: new Text("Toggle fade"),
                            onPressed: _fadeController.Toggle),
                        new CupertinoButton(
                            child: new Text("Toggle scroll"),
                            onPressed: _scrollController.Toggle),
                    ]),
            ]);
    }

    private static Widget BuildExpansionTile(
        string title,
        string body,
        ExpansibleController controller,
        ExpansionTileTransitionMode mode,
        Color panel)
    {
        return new ClipRRect(
            BorderRadius.Circular(14.0),
            child: new ColoredBox(
                color: panel,
                child: new CupertinoExpansionTile(
                    title: new Text(title),
                    controller: controller,
                    transitionMode: mode,
                    child: new Padding(
                        insets: new Thickness(20.0, 14.0),
                        child: new Text(body)))));
    }
}
