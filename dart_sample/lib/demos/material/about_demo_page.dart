import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';

bool _licensesRegistered = false;

class AboutDemoPage extends StatelessWidget {
  const AboutDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    _registerDemoLicenses();
    const String name = 'Plumix Sample';
    const String version = '1.0.0-demo';
    const String legalese = '© 2026 Plumix contributors';
    const Widget appIcon = Icon(
      Icons.info_outline,
      size: 40,
      color: Color(0xFF6750A4),
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text('AboutDialog + LicensePage', style: TextStyle(fontSize: 20)),
        const Text(
          'Dialog metadata, AboutListTile entry, lazy license registry, package grouping, and license detail routes.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            ElevatedButton(
              onPressed: () => showAboutDialog(
                context: context,
                applicationName: name,
                applicationVersion: version,
                applicationIcon: appIcon,
                applicationLegalese: legalese,
                children: const <Widget>[
                  Text('Flutter-like controls rendered by Plumix.'),
                ],
              ),
              child: const Text('SHOW ABOUT'),
            ),
            OutlinedButton(
              onPressed: () => showLicensePage(
                context: context,
                applicationName: name,
                applicationVersion: version,
                applicationIcon: appIcon,
                applicationLegalese: legalese,
              ),
              child: const Text('SHOW LICENSES'),
            ),
          ],
        ),
        const Card(
          child: AboutListTile(
            icon: Icon(Icons.info_outline),
            applicationName: name,
            applicationVersion: version,
            applicationIcon: appIcon,
            applicationLegalese: legalese,
            aboutBoxChildren: <Widget>[Text('Opened from AboutListTile.')],
          ),
        ),
      ],
    );
  }

  void _registerDemoLicenses() {
    if (_licensesRegistered) return;
    _licensesRegistered = true;
    LicenseRegistry.addLicense(() async* {
      yield const LicenseEntryWithLineBreaks(
        <String>['plumix_sample'],
        '''
Copyright 2026 Plumix contributors.

Redistribution and use in source and binary forms are permitted for this demo.''',
      );
      yield const LicenseEntryWithLineBreaks(
        <String>['plumix_core', 'plumix_material'],
        '''
BSD 3-Clause License

   Framework porting and rendering components.''',
      );
    });
  }
}
