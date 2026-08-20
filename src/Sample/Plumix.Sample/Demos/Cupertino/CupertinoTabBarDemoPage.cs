using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_tab_bar_demo_page.dart
// (exact sample parity)

public sealed class CupertinoTabBarDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoTabBarDemoPageState();
}

internal sealed class CupertinoTabBarDemoPageState : State
{
    private static readonly string[] Titles = ["Home", "Favorites", "Profile"];

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
                new Text("CupertinoTabScaffold", fontSize: 20.0, color: label),
                new Text(
                    "Tap a destination to probe lazy tab bodies, retained state, active icons, and blur.",
                    fontSize: 14.0,
                    color: secondaryLabel),
                new ClipRRect(
                    BorderRadius.Circular(14.0),
                    child: new Container(
                        height: 260.0,
                        decoration: new BoxDecoration(
                            Color: panel,
                            BorderRadius: BorderRadius.Circular(14.0)),
                        child: new CupertinoTabScaffold(
                            backgroundColor: CupertinoColors.SecondarySystemBackground,
                            tabBar: new CupertinoTabBar(
                                items:
                                [
                                    new BottomNavigationBarItem(
                                        icon: new Icon(CupertinoIcons.Home),
                                        label: "Home"),
                                    new BottomNavigationBarItem(
                                        icon: new Icon(CupertinoIcons.Heart),
                                        activeIcon: new Icon(CupertinoIcons.HeartFill),
                                        label: "Favorites"),
                                    new BottomNavigationBarItem(
                                        icon: new Icon(CupertinoIcons.Person),
                                        activeIcon: new Icon(CupertinoIcons.PersonFill),
                                        label: "Profile"),
                                ]),
                            tabBuilder: (_, index) => new Center(
                                child: new Text(
                                    $"Selected: {Titles[index]}",
                                    fontSize: 18.0,
                                    color: label))))),
            ]);
    }
}
