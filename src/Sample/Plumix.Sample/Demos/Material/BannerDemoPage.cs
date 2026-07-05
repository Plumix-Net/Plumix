using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/banner_demo_page.dart

public sealed class BannerDemoPage : StatefulWidget
{
    public override State CreateState() => new BannerDemoPageState();

    private sealed class BannerDemoPageState : State
    {
        private bool _forceActionsBelow;
        private bool _useThemeOverrides;

        public override Widget Build(BuildContext context)
        {
            IReadOnlyList<Widget> actions = _forceActionsBelow
                ?
                [
                    new TextButton(new Text("DISMISS"), () => { }),
                    new TextButton(new Text("LEARN MORE"), () => { }),
                ]
                : [new TextButton(new Text("DISMISS"), () => { })];
            var theme = Theme.Of(context) with
            {
                BannerTheme = _useThemeOverrides
                    ? new MaterialBannerThemeData(
                        BackgroundColor: Color.Parse("#FFE0F2F1"),
                        DividerColor: Color.Parse("#FF00695C"),
                        ContentTextStyle: Theme.Of(context).TextTheme.BodyMedium.CopyWith(color: Color.Parse("#FF004D40")),
                        Elevation: 2)
                    : new MaterialBannerThemeData(),
            };

            return new Theme(
                theme,
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 14,
                    children:
                    [
                        new Text("Banner + MaterialBanner", fontSize: 20),
                        new Text(
                            "Diagonal core ribbon and persistent Material message with leading content, actions, overflow, and theme precedence.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Row(
                            spacing: 8,
                            children:
                            [
                                ControlButton(_forceActionsBelow ? "Actions below" : "Single row", () => SetState(() => _forceActionsBelow = !_forceActionsBelow)),
                                ControlButton(_useThemeOverrides ? "Theme on" : "Theme off", () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                            ]),
                        new Align(
                            alignment: Alignment.CenterLeft,
                            child: new Banner(
                                message: "BETA",
                                location: BannerLocation.TopEnd,
                                color: Color.Parse("#CCB3261E"),
                                child: new Container(
                                    width: 320,
                                    height: 96,
                                    color: Color.Parse("#FFEADDFF"),
                                    alignment: Alignment.Center,
                                    child: new Text("Core diagonal ribbon")))),
                        new MaterialBanner(
                            leading: new Icon(Icons.InfoOutline),
                            content: new Text("A Material banner stays visible until the user chooses an action."),
                            forceActionsBelow: _forceActionsBelow,
                            actions: actions),
                    ]));
        }

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
