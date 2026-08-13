import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class DebugPaintingDemoPage extends StatefulWidget {
  const DebugPaintingDemoPage({super.key});

  @override
  State<DebugPaintingDemoPage> createState() => _DebugPaintingDemoPageState();
}

class _DebugPaintingDemoPageState extends State<DebugPaintingDemoPage> {
  bool _showPlaceholderChild = false;
  bool _customGrid = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Placeholder + GridPaper',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Placeholder uses fallback dimensions only in unbounded space; '
          'GridPaper paints over its child.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _showPlaceholderChild ? 'Remove child' : 'Add child',
              onTap: () {
                setState(() {
                  _showPlaceholderChild = !_showPlaceholderChild;
                });
              },
            ),
            _buildButton(
              label: _customGrid ? 'Default grid' : 'Custom grid',
              onTap: () {
                setState(() {
                  _customGrid = !_customGrid;
                });
              },
            ),
          ],
        ),
        Expanded(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 16,
            children: <Widget>[
              Expanded(child: _buildPlaceholderProbe()),
              Expanded(child: _buildGridPaperProbe()),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPlaceholderProbe() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          const Text(
            'Unbounded fallback: 160 × 120',
            style: TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Expanded(
            child: Align(
              alignment: Alignment.topLeft,
              child: UnconstrainedBox(
                alignment: Alignment.topLeft,
                child: Placeholder(
                  color: const Color(0xFF455A64),
                  strokeWidth: 2,
                  fallbackWidth: 160,
                  fallbackHeight: 120,
                  child: _showPlaceholderChild
                      ? Container(
                          width: 96,
                          height: 56,
                          color: const Color(0xFFFFE8A3),
                          alignment: Alignment.center,
                          child: const Text(
                            'child',
                            style: TextStyle(fontSize: 14, color: Colors.black),
                          ),
                        )
                      : null,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGridPaperProbe() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Text(
            _customGrid
                ? 'interval=60, divisions=3, subdivisions=2'
                : 'Flutter defaults',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Expanded(
            child: _customGrid
                ? GridPaper(
                    color: const Color(0x7FFF8A65),
                    interval: 60,
                    divisions: 3,
                    subdivisions: 2,
                    child: _buildGridChild(),
                  )
                : GridPaper(child: _buildGridChild()),
          ),
        ],
      ),
    );
  }

  static Widget _buildGridChild() {
    return Container(
      color: const Color(0xFFF2F7FA),
      alignment: Alignment.center,
      child: const Text(
        'foreground grid',
        style: TextStyle(fontSize: 14, color: Colors.black),
      ),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 120,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }
}
