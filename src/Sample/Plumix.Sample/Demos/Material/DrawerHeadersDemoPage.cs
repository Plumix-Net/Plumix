using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/drawer_headers_demo_page.dart

public sealed class DrawerHeadersDemoPage : StatefulWidget
{
    public override State CreateState() => new DrawerHeadersDemoPageState();

    private sealed class DrawerHeadersDemoPageState : State
    {
        private bool _alternateDecoration;
        private int _detailsPressed;

        public override Widget Build(BuildContext context)
        {
            var plainColor = _alternateDecoration ? MaterialColors.BlueGrey : MaterialColors.Indigo;
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text(
                        "DrawerHeader + UserAccountsDrawerHeader",
                        style: new TextStyle(
                            FontSize: 20,
                            Color: Colors.Black)),
                    new Text(
                        "Status-bar padding, animated decoration, ColorScheme primary, pictures, "
                        + "details, and semantics.",
                        style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new TextButton(
                                child: new Text(_alternateDecoration ? "Decoration B" : "Decoration A"),
                                onPressed: () => SetState(() => _alternateDecoration = !_alternateDecoration)),
                            new Text($"details={_detailsPressed}", style: new TextStyle(Color: Colors.Black)),
                        ]),
                    new Expanded(
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            spacing: 12,
                            children:
                            [
                                new Expanded(
                                    child: new DrawerHeader(
                                        decoration: new BoxDecoration(Color: plainColor),
                                        child: new Align(
                                            alignment: Alignment.BottomLeft,
                                            child: new Text(
                                                "Plain header",
                                                style: new TextStyle(
                                                    Color: Colors.White))))),
                                new Expanded(
                                    child: new UserAccountsDrawerHeader(
                                        accountName: new Text("Ada Lovelace"),
                                        accountEmail: new Text("ada@example.test"),
                                        currentAccountPicture: new CircleAvatar(child: new Text("AL")),
                                        otherAccountsPictures:
                                        [
                                            new CircleAvatar(radius: 20, child: new Text("GH")),
                                            new CircleAvatar(radius: 20, child: new Text("CS")),
                                        ],
                                        onDetailsPressed: () => SetState(() => _detailsPressed++))),
                            ])),
                ]);
        }
    }
}
