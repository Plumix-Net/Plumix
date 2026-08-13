import 'package:material_ui/material_ui.dart';

class BannerDemoPage extends StatefulWidget {
  const BannerDemoPage({super.key});

  @override
  State<BannerDemoPage> createState() => _BannerDemoPageState();
}

class _BannerDemoPageState extends State<BannerDemoPage> {
  bool _forceActionsBelow = false;
  bool _useMaterial3 = true;
  bool _useThemeOverrides = false;

  @override
  Widget build(BuildContext context) {
    final ThemeData ambientTheme = Theme.of(context);
    final ColorScheme colorScheme = ambientTheme.colorScheme.copyWith(
      surface: const Color(0xFFFFF8E1),
      surfaceContainerLow: const Color(0xFFE0F2F1),
      outlineVariant: const Color(0xFF00695C),
    );
    final MaterialBannerThemeData bannerTheme = _useThemeOverrides
        ? MaterialBannerThemeData(
            backgroundColor: const Color(0xFFFCE4EC),
            dividerColor: const Color(0xFFAD1457),
            contentTextStyle: ambientTheme.textTheme.bodyMedium?.copyWith(
              color: const Color(0xFF880E4F),
            ),
            elevation: 2,
          )
        : const MaterialBannerThemeData();
    final ThemeData theme = ThemeData(
      useMaterial3: _useMaterial3,
      colorScheme: colorScheme,
      textTheme: ambientTheme.textTheme,
      bannerTheme: bannerTheme,
    );

    return Theme(
      data: theme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 14,
        children: <Widget>[
          const Text('Banner + MaterialBanner', style: TextStyle(fontSize: 20)),
          const Text(
            'M2/M3 ColorScheme defaults, local theme precedence, and queued presentation.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: () => setState(() => _useMaterial3 = !_useMaterial3),
                child: Text(_useMaterial3 ? 'Material 3' : 'Material 2'),
              ),
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
