import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoButtonDemoPage extends StatefulWidget {
  const CupertinoButtonDemoPage({super.key});

  @override
  State<CupertinoButtonDemoPage> createState() => _CupertinoButtonDemoPageState();
}

class _CupertinoButtonDemoPageState extends State<CupertinoButtonDemoPage> {
  int _taps = 0;
  int _longPresses = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino buttons',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Plain, tinted and filled styles across the three size styles.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            CupertinoButton(
              onPressed: () => setState(() => _taps++),
              child: const Text('Plain'),
            ),
            CupertinoButton.tinted(
              onPressed: () => setState(() => _taps++),
              child: const Text('Tinted'),
            ),
            CupertinoButton.filled(
              onPressed: () => setState(() => _taps++),
              child: const Text('Filled'),
            ),
            const CupertinoButton(onPressed: null, child: Text('Disabled')),
            const CupertinoButton.filled(
              onPressed: null,
              child: Text('Disabled filled'),
            ),
          ],
        ),
        const Text('Size styles', style: TextStyle(fontSize: 14, color: Colors.black)),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            CupertinoButton.filled(
              sizeStyle: CupertinoButtonSize.small,
              onPressed: () => setState(() => _taps++),
              child: const Text('Small'),
            ),
            CupertinoButton.filled(
              sizeStyle: CupertinoButtonSize.medium,
              onPressed: () => setState(() => _taps++),
              child: const Text('Medium'),
            ),
            CupertinoButton.filled(
              onPressed: () => setState(() => _taps++),
              child: const Text('Large'),
            ),
          ],
        ),
        const Text('Customisation', style: TextStyle(fontSize: 14, color: Colors.black)),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            CupertinoButton.tinted(
              color: CupertinoColors.systemGrey,
              onPressed: () => setState(() => _taps++),
              child: const Text('Grey tint'),
            ),
            CupertinoButton.filled(
              color: CupertinoColors.systemRed,
              borderRadius: BorderRadius.circular(4),
              onPressed: () => setState(() => _taps++),
              child: const Text('Custom radius'),
            ),
            CupertinoButton(
              foregroundColor: CupertinoColors.systemPink,
              onPressed: () => setState(() => _taps++),
              child: const Icon(CupertinoIcons.heart),
            ),
            CupertinoButton(
              onPressed: null,
              onLongPress: () => setState(() => _longPresses++),
              child: const Text('Long press me'),
            ),
          ],
        ),
        Text(
          'Taps: $_taps   Long presses: $_longPresses',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
      ],
    );
  }
}
