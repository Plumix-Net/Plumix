import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

class PlatformMenuBarDemoPage extends StatefulWidget {
  const PlatformMenuBarDemoPage({super.key});

  @override
  State<PlatformMenuBarDemoPage> createState() =>
      _PlatformMenuBarDemoPageState();
}

class _PlatformMenuBarDemoPageState extends State<PlatformMenuBarDemoPage> {
  bool _enabled = true;
  String _lastEvent = 'none';

  @override
  Widget build(BuildContext context) {
    final List<PlatformMenuItem> menus = <PlatformMenuItem>[
      PlatformMenu(
        label: 'Plumix demo',
        onOpen: () => _record('menu opened'),
        onClose: () => _record('menu closed'),
        menus: <PlatformMenuItem>[
          PlatformMenuItemGroup(
            members: <PlatformMenuItem>[
              PlatformMenuItem(
                label: 'Say hello',
                tooltip: 'Update the message below',
                shortcut: const SingleActivator(
                  LogicalKeyboardKey.keyH,
                  control: true,
                ),
                onSelected: _enabled
                    ? () => _record('Hello from the system menu')
                    : null,
              ),
              const PlatformMenuItem(label: 'Unavailable action'),
            ],
          ),
          PlatformMenu(
            label: 'Messages',
            menus: <PlatformMenuItem>[
              PlatformMenuItem(
                label: 'Welcome',
                onSelected: () => _record('Welcome'),
              ),
              PlatformMenuItem(
                label: 'Goodbye',
                onSelected: () => _record('Goodbye'),
              ),
            ],
          ),
        ],
      ),
    ];
    if (PlatformProvidedMenuItem.hasMenu(PlatformProvidedMenuItemType.about)) {
      menus.add(
        const PlatformMenu(
          label: 'System',
          menus: <PlatformMenuItem>[
            PlatformProvidedMenuItem(type: PlatformProvidedMenuItemType.about),
            PlatformProvidedMenuItem(
              type: PlatformProvidedMenuItemType.servicesSubmenu,
            ),
          ],
        ),
      );
    }
    return PlatformMenuBar(
      menus: menus,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 16,
        children: <Widget>[
          const Text(
            'PlatformMenuBar',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Open the system menu to try grouped actions, a Messages submenu '
            'and Ctrl+H. Native menus require a host with system-menu support.',
            style: TextStyle(color: Colors.black54),
          ),
          Container(
            padding: const EdgeInsets.all(12),
            color: const Color(0xFFF4F7FA),
            child: const Text(
              'Plumix demo\n  Say hello (Ctrl+H)\n  Unavailable action (disabled)\n'
              '  ──────────\n  Messages → Welcome / Goodbye',
              style: TextStyle(color: Colors.black),
            ),
          ),
          Text(
            'Say hello: ${_enabled ? 'enabled' : 'disabled'}',
            style: const TextStyle(color: Colors.black),
          ),
          TextButton(
            onPressed: () => setState(() => _enabled = !_enabled),
            child: const Text('Toggle Say hello'),
          ),
          Text(
            'Last event: $_lastEvent',
            style: const TextStyle(color: Color(0xFF31506F)),
          ),
        ],
      ),
    );
  }

  void _record(String message) => setState(() => _lastEvent = message);
}
