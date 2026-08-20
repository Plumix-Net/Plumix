import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoTextSelectionControlsDemoPage extends StatelessWidget {
  const CupertinoTextSelectionControlsDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Builder(
      builder: (BuildContext builderContext) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'Cupertino text selection',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Line-height-aware iOS handles and handle-free macOS selection controls.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFFF2F2F7),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: <Widget>[
                _buildHandleProbe(builderContext, '14 px line', 14),
                _buildHandleProbe(builderContext, '32 px line', 32),
                const Column(
                  spacing: 6,
                  children: <Widget>[
                    Text('macOS', style: TextStyle(fontSize: 13)),
                    Text(
                      'no handles',
                      style: TextStyle(fontSize: 12, color: Colors.black54),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const Text(
            'Selection toolbars use the existing Cupertino mobile and desktop '
            'surfaces; Material TextField and SelectableText choose these '
            'handle controls on Apple platforms.',
            style: TextStyle(fontSize: 13, color: Colors.black54),
          ),
        ],
      ),
    );
  }

  static Widget _buildHandleProbe(
    BuildContext context,
    String label,
    double lineHeight,
  ) {
    return Column(
      spacing: 6,
      children: <Widget>[
        Text(label, style: const TextStyle(fontSize: 13)),
        Row(
          spacing: 12,
          children: <Widget>[
            cupertinoTextSelectionControls.buildHandle(
              context,
              TextSelectionHandleType.left,
              lineHeight,
            ),
            cupertinoTextSelectionControls.buildHandle(
              context,
              TextSelectionHandleType.right,
              lineHeight,
            ),
          ],
        ),
      ],
    );
  }
}

