import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class CenterViewportDemoPage extends StatefulWidget {
  const CenterViewportDemoPage({super.key});

  @override
  State<CenterViewportDemoPage> createState() => _CenterViewportDemoPageState();
}

class _CenterViewportDemoPageState extends State<CenterViewportDemoPage> {
  static const Key _centerKey = ValueKey<String>('center-sliver');

  final ScrollController _controller = ScrollController();
  int _before = 5;
  int _after = 5;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'CustomScrollView center',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Slivers before the center key grow in the reverse direction and live at '
          'negative scroll offsets.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: 'Prepend',
                onTap: () => setState(() => _before++),
                background: Colors.blue,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Append',
                onTap: () => setState(() => _after++),
                background: Colors.green,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Back to center',
                onTap: () => _controller.jumpTo(0),
                background: Colors.blueGrey,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
          ],
        ),
        Expanded(
          child: CustomScrollView(
            controller: _controller,
            center: _centerKey,
            slivers: <Widget>[
              SliverList(
                delegate: SliverChildListDelegate(<Widget>[
                  for (int index = 1; index <= _before; index++)
                    _row(-index, const Color(0xFFFFE0E0)),
                ]),
              ),
              const SliverToBoxAdapter(
                key: _centerKey,
                child: ColoredBox(
                  color: Color(0xFF263238),
                  child: Padding(
                    padding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    child: Text(
                      'center (offset 0)',
                      style: TextStyle(fontSize: 14, color: Colors.white),
                    ),
                  ),
                ),
              ),
              SliverList(
                delegate: SliverChildListDelegate(<Widget>[
                  for (int index = 1; index <= _after; index++)
                    _row(index, const Color(0xFFE0F2E9)),
                ]),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _row(int index, Color background) {
    return Container(
      height: 44,
      color: background,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Text(
        'item $index',
        style: const TextStyle(fontSize: 13, color: Colors.black),
      ),
    );
  }
}
