import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class FlowDemoPage extends StatefulWidget {
  const FlowDemoPage({super.key});

  @override
  State<FlowDemoPage> createState() => _FlowDemoPageState();
}

class _FlowDemoPageState extends State<FlowDemoPage> {
  bool _expanded = false;
  int _count = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Flow + RepaintBoundary',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Flow positions children during paint; its default constructor '
          'isolates every child repaint.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _expanded ? 'Collapse' : 'Spread',
              onTap: () {
                setState(() {
                  _expanded = !_expanded;
                });
              },
            ),
            _buildButton(
              label: 'Boundary count: $_count',
              onTap: () {
                setState(() {
                  _count += 1;
                });
              },
            ),
          ],
        ),
        Expanded(
          child: Container(
            color: const Color(0xFFF3F6FA),
            alignment: Alignment.center,
            child: SizedBox(
              width: 300,
              height: 170,
              child: Flow(
                delegate: DemoFlowDelegate(_expanded),
                children: const <Widget>[
                  _FlowTile('0', Color(0xFF1565C0)),
                  _FlowTile('1', Color(0xFF2E7D32)),
                  _FlowTile('2', Color(0xFFF57C00)),
                ],
              ),
            ),
          ),
        ),
        const RepaintBoundary(
          child: ColoredBox(
            color: Colors.white,
            child: Padding(
              padding: EdgeInsets.all(12),
              child: Text(
                'Explicit RepaintBoundary keeps this footer in its own '
                'composited display list.',
                style: TextStyle(fontSize: 12, color: Colors.blueGrey),
              ),
            ),
          ),
        ),
      ],
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

class _FlowTile extends StatelessWidget {
  const _FlowTile(this.label, this.color);

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: color,
      child: Center(
        child: Text(
          label,
          style: const TextStyle(fontSize: 18, color: Colors.white),
        ),
      ),
    );
  }
}

class DemoFlowDelegate extends FlowDelegate {
  const DemoFlowDelegate(this.expanded);

  final bool expanded;

  @override
  Size getSize(BoxConstraints constraints) {
    return constraints.constrain(const Size(300, 170));
  }

  @override
  BoxConstraints getConstraintsForChild(int i, BoxConstraints constraints) {
    return const BoxConstraints.tightFor(width: 72, height: 48);
  }

  @override
  void paintChildren(FlowPaintingContext context) {
    for (int index = 0; index < context.childCount; index += 1) {
      final double x = expanded ? 24 + (index * 92) : 90 + (index * 18);
      final double y = expanded ? 60 : 38 + (index * 24);
      final double opacity = expanded && index == 2 ? 0.55 : 1.0;
      context.paintChild(
        index,
        transform: Matrix4.translationValues(x, y, 0),
        opacity: opacity,
      );
    }
  }

  @override
  bool shouldRepaint(DemoFlowDelegate oldDelegate) {
    return oldDelegate.expanded != expanded;
  }
}
