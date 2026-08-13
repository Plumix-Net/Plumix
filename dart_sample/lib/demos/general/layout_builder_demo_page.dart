import 'package:material_ui/material_ui.dart';

class LayoutBuilderDemoPage extends StatelessWidget {
  const LayoutBuilderDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'LayoutBuilder + OrientationBuilder',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          "LayoutBuilder receives its parent's live constraints. "
          'OrientationBuilder reduces those constraints to landscape or portrait.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Container(
          height: 96,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(12),
          child: LayoutBuilder(
            builder: (BuildContext context, BoxConstraints constraints) {
              final bool isWide = constraints.maxWidth >= 420;
              final String width = constraints.maxWidth.toStringAsFixed(0);
              final String height = constraints.maxHeight.toStringAsFixed(0);
              return Container(
                color: isWide
                    ? const Color(0xFF2A9D8F)
                    : const Color(0xFFE76F51),
                alignment: Alignment.center,
                child: Text(
                  '$width × $height — ${isWide ? 'wide' : 'compact'}',
                  style: const TextStyle(fontSize: 16, color: Colors.white),
                ),
              );
            },
          ),
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.start,
          spacing: 16,
          children: <Widget>[
            _buildOrientationProbe(width: 180, height: 80),
            _buildOrientationProbe(width: 100, height: 150),
          ],
        ),
      ],
    );
  }

  static Widget _buildOrientationProbe({
    required double width,
    required double height,
  }) {
    return SizedBox(
      width: width,
      height: height,
      child: OrientationBuilder(
        builder: (BuildContext context, Orientation orientation) {
          final bool isLandscape = orientation == Orientation.landscape;
          return Container(
            color: isLandscape
                ? const Color(0xFF264653)
                : const Color(0xFF457B9D),
            alignment: Alignment.center,
            child: Text(
              isLandscape ? 'landscape' : 'portrait',
              style: const TextStyle(fontSize: 14, color: Colors.white),
            ),
          );
        },
      ),
    );
  }
}
