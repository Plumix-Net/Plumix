import 'package:flutter/material.dart';

class ActionButtonsDemoPage extends StatefulWidget {
  const ActionButtonsDemoPage({super.key});

  @override
  State<ActionButtonsDemoPage> createState() => _ActionButtonsDemoPageState();
}

class _ActionButtonsDemoPageState extends State<ActionButtonsDemoPage> {
  bool _applePlatform = false;
  bool _customIcons = false;
  int _backCount = 0;
  int _closeCount = 0;
  int _drawerCount = 0;
  int _endDrawerCount = 0;

  @override
  Widget build(BuildContext context) {
    final localTheme = Theme.of(context).copyWith(
      platform: _applePlatform ? TargetPlatform.iOS : TargetPlatform.windows,
    );
    final actionIconTheme = _customIcons
        ? ActionIconThemeData(
            backButtonIconBuilder: (_) => const Icon(Icons.star),
            closeButtonIconBuilder: (_) => const Icon(Icons.cancel),
            drawerButtonIconBuilder: (_) => const Icon(Icons.info_outline),
            endDrawerButtonIconBuilder: (_) => const Icon(Icons.star_outline),
          )
        : const ActionIconThemeData();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Material action buttons',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Back/close/drawer/end-drawer icons, themes, callbacks, tooltips, and style precedence.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _applePlatform = !_applePlatform),
              child: Text(_applePlatform ? 'platform=iOS' : 'platform=Windows'),
            ),
            TextButton(
              onPressed: () => setState(() => _customIcons = !_customIcons),
              child: Text('customIcons=$_customIcons'),
            ),
          ],
        ),
        Theme(
          data: localTheme,
          child: ActionIconTheme(
            data: actionIconTheme,
            child: Row(
              spacing: 16,
              children: <Widget>[
                BackButton(onPressed: () => setState(() => _backCount++)),
                CloseButton(onPressed: () => setState(() => _closeCount++)),
                DrawerButton(onPressed: () => setState(() => _drawerCount++)),
                EndDrawerButton(
                  onPressed: () => setState(() => _endDrawerCount++),
                ),
                const Text('standalone:'),
                const BackButtonIcon(),
                const CloseButtonIcon(),
                const DrawerButtonIcon(),
                const EndDrawerButtonIcon(),
              ],
            ),
          ),
        ),
        Text(
          'back=$_backCount, close=$_closeCount, drawer=$_drawerCount, end=$_endDrawerCount',
          style: const TextStyle(color: Colors.black),
        ),
        const Text(
          'style.iconColor overrides color',
          style: TextStyle(color: Colors.black54),
        ),
        Theme(
          data: localTheme,
          child: BackButton(
            color: Colors.red,
            style: const ButtonStyle(
              iconColor: WidgetStatePropertyAll<Color>(Colors.purple),
            ),
            onPressed: () => setState(() => _backCount++),
          ),
        ),
      ],
    );
  }
}
