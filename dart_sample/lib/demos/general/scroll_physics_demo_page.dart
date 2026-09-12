import 'package:material_ui/material_ui.dart';

class ScrollPhysicsDemoPage extends StatelessWidget {
  const ScrollPhysicsDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Scroll physics',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Drag past either end: bouncing physics rubber-band and spring back, '
          'clamping physics stop at the edge, never-scrollable ignores the drag.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        const Text(
          'Single child: horizontal strip with directional padding',
          style: TextStyle(fontSize: 14),
        ),
        SizedBox(
          height: 64,
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsetsDirectional.only(start: 24, end: 8),
            clipBehavior: Clip.antiAlias,
            child: Row(
              spacing: 8,
              children: List<Widget>.generate(
                12,
                (int index) => Container(
                  width: 100,
                  height: 56,
                  color: const Color(0xFFE3F2FD),
                  alignment: Alignment.center,
                  child: Text(
                    'item #${index + 1}',
                    style: const TextStyle(fontSize: 14),
                  ),
                ),
              ),
            ),
          ),
        ),
        Expanded(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 12,
            children: <Widget>[
              Expanded(
                child: _buildList(
                  'Bouncing (iOS)',
                  const Color(0xFFE8F5E9),
                  const BouncingScrollPhysics(
                    parent: RangeMaintainingScrollPhysics(),
                  ),
                ),
              ),
              Expanded(
                child: _buildList(
                  'Bouncing (fast)',
                  const Color(0xFFFFF3E0),
                  const BouncingScrollPhysics(
                    decelerationRate: ScrollDecelerationRate.fast,
                    parent: RangeMaintainingScrollPhysics(),
                  ),
                ),
              ),
              Expanded(
                child: _buildList(
                  'Clamping (Android)',
                  const Color(0xFFE3F2FD),
                  const ClampingScrollPhysics(
                    parent: RangeMaintainingScrollPhysics(),
                  ),
                ),
              ),
              Expanded(
                child: _buildList(
                  'Never (locked)',
                  const Color(0xFFF3E5F5),
                  const NeverScrollableScrollPhysics(),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildList(String title, Color color, ScrollPhysics physics) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 6,
      children: <Widget>[
        Text(title, style: const TextStyle(fontSize: 14, color: Colors.black)),
        Expanded(
          child: ListView.builder(
            itemCount: 24,
            itemExtent: 44,
            physics: physics,
            addAutomaticKeepAlives: false,
            itemBuilder: (BuildContext context, int index) {
              return Container(
                color: index.isEven ? color : Colors.white,
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
                child: Text(
                  'row #$index',
                  style: const TextStyle(fontSize: 13, color: Colors.black),
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
