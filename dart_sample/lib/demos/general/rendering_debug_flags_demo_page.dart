import 'package:flutter/rendering.dart';
import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class RenderingDebugFlagsDemoPage extends StatefulWidget {
  const RenderingDebugFlagsDemoPage({super.key});

  @override
  State<RenderingDebugFlagsDemoPage> createState() =>
      _RenderingDebugFlagsDemoPageState();
}

class _RenderingDebugFlagsDemoPageState
    extends State<RenderingDebugFlagsDemoPage> {
  int _repaintToken = 0;

  @override
  void dispose() {
    debugPaintSizeEnabled = false;
    debugPaintBaselinesEnabled = false;
    debugPaintPointersEnabled = false;
    debugPaintLayerBordersEnabled = false;
    debugDisableClipLayers = false;
    debugDisableOpacityLayers = false;
    debugDisablePhysicalShapeLayers = false;
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'rendering/debug.dart flags',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Each toggle flips the matching library-level debug variable and '
          'rebuilds the probe below.',
          style: TextStyle(fontSize: 14, color: Color(0xFF696969)),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            _buildToggle(
              'paint size',
              debugPaintSizeEnabled,
              () => debugPaintSizeEnabled = !debugPaintSizeEnabled,
            ),
            _buildToggle(
              'baselines',
              debugPaintBaselinesEnabled,
              () => debugPaintBaselinesEnabled = !debugPaintBaselinesEnabled,
            ),
            _buildToggle(
              'pointers',
              debugPaintPointersEnabled,
              () => debugPaintPointersEnabled = !debugPaintPointersEnabled,
            ),
            _buildToggle(
              'layer borders',
              debugPaintLayerBordersEnabled,
              () =>
                  debugPaintLayerBordersEnabled = !debugPaintLayerBordersEnabled,
            ),
            _buildToggle(
              'no clips',
              debugDisableClipLayers,
              () => debugDisableClipLayers = !debugDisableClipLayers,
            ),
            _buildToggle(
              'no opacity',
              debugDisableOpacityLayers,
              () => debugDisableOpacityLayers = !debugDisableOpacityLayers,
            ),
            _buildToggle(
              'no shadows',
              debugDisablePhysicalShapeLayers,
              () => debugDisablePhysicalShapeLayers =
                  !debugDisablePhysicalShapeLayers,
            ),
          ],
        ),
        Expanded(child: _buildProbe()),
      ],
    );
  }

  Widget _buildProbe() {
    return KeyedSubtree(
      key: ValueKey<int>(_repaintToken),
      child: Container(
        color: Colors.white,
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 16,
          children: <Widget>[
            const Text(
              'Padding draws its construction lines, clips get a scissors '
              'marker.',
              style: TextStyle(fontSize: 12, color: Color(0xFF2F4F4F)),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Container(
                  width: 200,
                  height: 64,
                  color: const Color(0xFFB3E5FC),
                  alignment: Alignment.center,
                  child: const Text(
                    'clipped',
                    style: TextStyle(fontSize: 16, color: Colors.black),
                  ),
                ),
              ),
            ),
            Opacity(
              opacity: 0.45,
              child: PhysicalModel(
                color: const Color(0xFFFFE082),
                elevation: 8,
                borderRadius: BorderRadius.circular(8),
                child: const SizedBox(
                  width: 200,
                  height: 56,
                  child: Center(
                    child: Text(
                      'elevated + 45% opacity',
                      style: TextStyle(fontSize: 14, color: Colors.black),
                    ),
                  ),
                ),
              ),
            ),
            Listener(
              behavior: HitTestBehavior.opaque,
              child: Container(
                width: 200,
                height: 44,
                color: const Color(0xFFDCE3ED),
                alignment: Alignment.center,
                child: const Text(
                  'press and hold me',
                  style: TextStyle(fontSize: 14, color: Colors.black),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildToggle(String label, bool enabled, VoidCallback toggle) {
    return CounterTapButton(
      label: enabled ? '$label: on' : '$label: off',
      onTap: () => setState(() {
        toggle();
        _repaintToken += 1;
      }),
      background: enabled ? const Color(0xFF9FC5E8) : const Color(0xFFDCE3ED),
      foreground: Colors.black,
      fontSize: 12,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}
