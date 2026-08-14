using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/about_demo_page.dart
public sealed class AboutDemoPage : StatelessWidget
{
    private static bool _licensesRegistered;

    public override Widget Build(BuildContext context)
    {
        RegisterDemoLicenses();
        const string name = "Plumix Sample";
        const string version = "1.0.0-demo";
        const string legalese = "© 2026 Plumix contributors";
        var appIcon = new Icon(Icons.InfoOutline, size: 40, color: Color.Parse("#FF6750A4"));
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("AboutDialog + LicensePage", fontSize: 20),
                new Text(
                    "Dialog metadata, adaptive actions, AboutListTile entry, lazy license registry, and the "
                    + "master-detail license page: nested routes below 840px, side-by-side above it.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new ElevatedButton(
                            new Text("SHOW ABOUT"),
                            () => AboutDialogs.ShowAboutDialog(
                                context,
                                applicationName: name,
                                applicationVersion: version,
                                applicationIcon: appIcon,
                                applicationLegalese: legalese,
                                children: [new Text("Flutter-like controls rendered by Plumix.")])),
                        new TextButton(
                            new Text("SHOW ADAPTIVE ABOUT"),
                            () => AboutDialogs.ShowAdaptiveAboutDialog(
                                context,
                                applicationName: name,
                                applicationVersion: version,
                                applicationIcon: appIcon,
                                applicationLegalese: legalese,
                                children: [new Text("Cupertino actions on iOS and macOS.")])),
                        new OutlinedButton(
                            new Text("SHOW LICENSES"),
                            () => AboutDialogs.ShowLicensePage(
                                context,
                                applicationName: name,
                                applicationVersion: version,
                                applicationIcon: appIcon,
                                applicationLegalese: legalese)),
                    ]),
                new Card(
                    child: new AboutListTile(
                        icon: new Icon(Icons.InfoOutline),
                        applicationName: name,
                        applicationVersion: version,
                        applicationIcon: appIcon,
                        applicationLegalese: legalese,
                        aboutBoxChildren: [new Text("Opened from AboutListTile.")]))
            ]);
    }

    private static void RegisterDemoLicenses()
    {
        if (_licensesRegistered) return;
        _licensesRegistered = true;
        LicenseRegistry.AddLicense(() =>
        [
            new LicenseEntryWithLineBreaks(["plumix_sample"], """
                Copyright 2026 Plumix contributors.

                Redistribution and use in source and binary forms are permitted for this demo.
                """),
            new LicenseEntryWithLineBreaks(["plumix_core", "plumix_material"], """
                BSD 3-Clause License

                   Framework porting and rendering components.
                """),
        ]);
    }
}
