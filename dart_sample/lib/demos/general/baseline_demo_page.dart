import 'package:material_ui/material_ui.dart';

class BaselineDemoPage extends StatelessWidget {
  const BaselineDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Baseline + IgnoreBaseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The guide is 48 px from the top. Text uses its real alphabetic baseline; '
          'the box falls back to its bottom edge.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        _buildBaselinePreview(),
        const Text(
          'IgnoreBaseline keeps the tall middle child out of Row baseline calculations.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Container(
          color: const Color(0xFFF1F5F9),
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            spacing: 12,
            children: <Widget>[
              const Text(
                'Aa',
                style: TextStyle(fontSize: 34, color: Color(0xFF1D3557)),
              ),
              IgnoreBaseline(
                child: Container(
                  width: 32,
                  height: 52,
                  color: const Color(0xFFE9C46A),
                ),
              ),
              const Text(
                'baseline',
                style: TextStyle(fontSize: 16, color: Color(0xFF2A9D8F)),
              ),
            ],
          ),
        ),
      ],
    );
  }

  static Widget _buildBaselinePreview() {
    return Container(
      height: 118,
      color: const Color(0xFFE7EDF6),
      padding: const EdgeInsets.all(12),
      child: Stack(
        clipBehavior: Clip.none,
        children: <Widget>[
          Positioned(
            left: 0,
            right: 0,
            top: 48,
            height: 1,
            child: Container(color: const Color(0xFFE63946)),
          ),
          const Baseline(
            baseline: 48,
            baselineType: TextBaseline.alphabetic,
            child: Text(
              'Plumix',
              style: TextStyle(fontSize: 36, color: Color(0xFF1D3557)),
            ),
          ),
          Positioned(
            left: 150,
            child: Baseline(
              baseline: 48,
              baselineType: TextBaseline.alphabetic,
              child: Container(
                width: 54,
                height: 28,
                color: const Color(0xFF2A9D8F),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
