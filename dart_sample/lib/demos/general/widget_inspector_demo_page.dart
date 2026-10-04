import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

/// The on-device widget inspector: select mode wraps the whole app in the root
/// [WidgetInspector], and the page reads the selection back.
class WidgetInspectorDemoPage extends StatefulWidget {
  const WidgetInspectorDemoPage({super.key});

  @override
  State<WidgetInspectorDemoPage> createState() =>
      _WidgetInspectorDemoPageState();
}

class _WidgetInspectorDemoPageState extends State<WidgetInspectorDemoPage> {
  String _selection = 'nothing selected';

  @override
  void initState() {
    super.initState();
    WidgetInspectorService.instance.selection.addListener(
      _handleSelectionChanged,
    );
    WidgetsBinding.instance.debugShowWidgetInspectorOverrideNotifier
        .addListener(_handleSelectionChanged);
  }

  @override
  void dispose() {
    WidgetInspectorService.instance.selection.removeListener(
      _handleSelectionChanged,
    );
    WidgetsBinding.instance.debugShowWidgetInspectorOverrideNotifier
        .removeListener(_handleSelectionChanged);
    super.dispose();
  }

  void _handleSelectionChanged() {
    final Element? element =
        WidgetInspectorService.instance.selection.currentElement;
    final RenderObject? renderObject =
        WidgetInspectorService.instance.selection.current;
    setState(() {
      _selection = element == null
          ? 'nothing selected'
          : '${element.toStringShort()} -> '
                '${renderObject?.runtimeType ?? 'no render object'}';
    });
  }

  @override
  Widget build(BuildContext context) {
    final bool selectMode =
        WidgetsBinding.instance.debugShowWidgetInspectorOverride;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'WidgetInspector',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Select mode wraps the app in the root inspector: tap a widget to '
          'outline it, drag to browse candidates, and use the buttons at the '
          'bottom to exit, move them or let taps reach the app.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            _buildButton(
              label: selectMode ? 'Exit select mode' : 'Select widget',
              onTap: () {
                WidgetsBinding.instance.debugShowWidgetInspectorOverride =
                    !selectMode;
              },
            ),
            _buildButton(
              label: 'Clear selection',
              onTap: () {
                WidgetInspectorService.instance.selection.clear();
              },
            ),
          ],
        ),
        Text(
          'Selected: $_selection',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
        Expanded(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 16,
            children: <Widget>[
              Expanded(
                child: _buildProbe('Blue card', const Color(0xFF90CAF9)),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  spacing: 16,
                  children: <Widget>[
                    Expanded(
                      child: _buildProbe(
                        'Green card',
                        const Color(0xFFA5D6A7),
                      ),
                    ),
                    Expanded(
                      child: DisableWidgetInspectorScope(
                        child: _buildProbe(
                          'Hidden from the tree',
                          const Color(0xFFFFCC80),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  static Widget _buildProbe(String label, Color color) {
    return ColoredBox(
      color: color,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 8,
          children: <Widget>[
            Text(
              label,
              style: const TextStyle(fontSize: 16, color: Colors.black),
            ),
            Container(width: 48, height: 24, color: const Color(0x99000000)),
          ],
        ),
      ),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 140,
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
