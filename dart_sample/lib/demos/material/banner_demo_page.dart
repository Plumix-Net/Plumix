import 'package:flutter/material.dart';

class BannerDemoPage extends StatefulWidget {
  const BannerDemoPage({super.key});

  @override
  State<BannerDemoPage> createState() => _BannerDemoPageState();
}

class _BannerDemoPageState extends State<BannerDemoPage> {
  bool _forceActionsBelow = false;
  bool _useThemeOverrides = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData ambientTheme = Theme.of(context);
    final ThemeData theme = ambientTheme.copyWith(
      bannerTheme: _useThemeOverrides
          ? MaterialBannerThemeData(
              backgroundColor: Colors.teal.shade50,
              dividerColor: Colors.teal.shade800,
              contentTextStyle: ambientTheme.textTheme.bodyMedium?.copyWith(
                color: Colors.teal.shade900,
              ),
              elevation: 2,
            )
          : const MaterialBannerThemeData(),
    );

    return Theme(
      data: theme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 14,
        children: <Widget>[
          const Text('Banner + MaterialBanner', style: TextStyle(fontSize: 20)),
          const Text(
            'Diagonal ribbon, direct Material layout, and queued ScaffoldMessenger presentation.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: () =>
                    setState(() => _forceActionsBelow = !_forceActionsBelow),
                child: Text(
                  _forceActionsBelow ? 'Actions below' : 'Single row',
                ),
              ),
              TextButton(
                onPressed: () =>
                    setState(() => _useThemeOverrides = !_useThemeOverrides),
                child: Text(_useThemeOverrides ? 'Theme on' : 'Theme off'),
              ),
              TextButton(
                onPressed: () {
                  ScaffoldMessenger.of(context).showMaterialBanner(
                    MaterialBanner(
                      leading: const Icon(Icons.info_outline),
                      content: const Text(
                        'This banner is queued and presented by ScaffoldMessenger.',
                      ),
                      actions: <Widget>[
                        TextButton(
                          onPressed: () => ScaffoldMessenger.of(
                            context,
                          ).hideCurrentMaterialBanner(),
                          child: const Text('DISMISS'),
                        ),
                      ],
                    ),
                  );
                },
                child: const Text('Show through messenger'),
              ),
            ],
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: Banner(
              message: 'BETA',
              location: BannerLocation.topEnd,
              color: const Color(0xCCB3261E),
              child: Container(
                width: 320,
                height: 96,
                color: const Color(0xFFEADDFF),
                alignment: Alignment.center,
                child: const Text('Core diagonal ribbon'),
              ),
            ),
          ),
          MaterialBanner(
            leading: const Icon(Icons.info_outline),
            content: const Text(
              'A Material banner stays visible until the user chooses an action.',
            ),
            forceActionsBelow: _forceActionsBelow,
            actions: <Widget>[
              TextButton(onPressed: () {}, child: const Text('DISMISS')),
              if (_forceActionsBelow)
                TextButton(onPressed: () {}, child: const Text('LEARN MORE')),
            ],
          ),
        ],
      ),
    );
  }
}
