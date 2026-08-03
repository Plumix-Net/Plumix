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
        private bool _useMaterial3 = true;
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
            ThemeData ambientTheme = Theme.Of(context);
            ColorScheme colorScheme = ambientTheme.ColorScheme.CopyWith(
                surface: Color.Parse("#FFFFF8E1"),
                surfaceContainerLow: Color.Parse("#FFE0F2F1"),
                outlineVariant: Color.Parse("#FF00695C"));
            MaterialBannerThemeData bannerTheme = _useThemeOverrides
                ? new MaterialBannerThemeData(
                    BackgroundColor: Color.Parse("#FFFCE4EC"),
                    DividerColor: Color.Parse("#FFAD1457"),
                    ContentTextStyle: ambientTheme.TextTheme.BodyMedium.CopyWith(
                        color: Color.Parse("#FF880E4F")),
                    Elevation: 2)
                : new MaterialBannerThemeData();
            var theme = new ThemeData(
                colorScheme: colorScheme,
                textTheme: ambientTheme.TextTheme,
                useMaterial3: _useMaterial3,
                bannerTheme: bannerTheme);

            return new Theme(
                theme,
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 14,
                    children:
                    [
                        new Text("Banner + MaterialBanner", fontSize: 20),
                        new Text(
                            "M2/M3 ColorScheme defaults, local theme precedence, and queued presentation.",
                            fontSize: 14,
                            color: Colors.DimGray),
                        new Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children:
                            [
                                ControlButton(
                                    _useMaterial3 ? "Material 3" : "Material 2",
                                    () => SetState(() => _useMaterial3 = !_useMaterial3)),
                                ControlButton(
                                    _forceActionsBelow ? "Actions below" : "Single row",
                                    () => SetState(() => _forceActionsBelow = !_forceActionsBelow)),
                                ControlButton(
                                    _useThemeOverrides ? "Theme on" : "Theme off",
                                    () => SetState(() => _useThemeOverrides = !_useThemeOverrides)),
                                ControlButton("Show through messenger", () => ShowMessengerBanner(context)),
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

        private static void ShowMessengerBanner(BuildContext context)
        {
            ScaffoldMessenger.Of(context).ShowMaterialBanner(new MaterialBanner(
                leading: new Icon(Icons.InfoOutline),
                content: new Text("This banner is queued and presented by ScaffoldMessenger."),
                actions:
                [
                    new TextButton(
                        new Text("DISMISS"),
                        () => ScaffoldMessenger.Of(context).HideCurrentMaterialBanner()),
                ]));
        }

        private static Widget ControlButton(string label, Action onPressed) =>
            new TextButton(new Text(label, fontSize: 12), onPressed);
    }
}
