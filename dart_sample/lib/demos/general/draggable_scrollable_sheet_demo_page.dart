import 'package:flutter/material.dart';

class DraggableScrollableSheetDemoPage extends StatefulWidget {
  const DraggableScrollableSheetDemoPage({super.key});

  @override
  State<DraggableScrollableSheetDemoPage> createState() =>
      _DraggableScrollableSheetDemoPageState();
}

class _DraggableScrollableSheetDemoPageState
    extends State<DraggableScrollableSheetDemoPage> {
  final DraggableScrollableController _controller =
      DraggableScrollableController();
  bool _snap = true;
  double _extent = 0.5;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_handleSizeChanged);
  }

  @override
  void dispose() {
    _controller.removeListener(_handleSizeChanged);
    _controller.dispose();
    super.dispose();
  }

  void _handleSizeChanged() {
    setState(() => _extent = _controller.size);
  }

  void _animateToTop() {
    _controller.animateTo(
      1.0,
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeOut,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'DraggableScrollableSheet',
          style: TextStyle(fontSize: 20),
        ),
        const Text(
          'Drag the sheet to resize it, keep dragging to scroll its list, and release to snap. '
          'The controller reports and drives the extent; the actuator resets it.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _snap = !_snap),
              child: Text(_snap ? 'Snap on' : 'Snap off'),
            ),
            TextButton(
              onPressed: () => _controller.jumpTo(0.4),
              child: const Text('Jump to 0.4'),
            ),
            TextButton(
              onPressed: _animateToTop,
              child: const Text('Animate to 1.0'),
            ),
            TextButton(
              onPressed: () => DraggableScrollableActuator.reset(context),
              child: const Text('Reset'),
            ),
          ],
        ),
        Text(
          'extent: ${_extent.toStringAsFixed(2)}',
          style: const TextStyle(fontSize: 13),
        ),
        SizedBox(
          height: 320,
          child: ColoredBox(
            color: const Color(0xFFE7EDF6),
            child: DraggableScrollableActuator(
              child: DraggableScrollableSheet(
                initialChildSize: 0.5,
                minChildSize: 0.25,
                maxChildSize: 1.0,
                snap: _snap,
                snapSizes: const <double>[0.5],
                controller: _controller,
                builder:
                    (BuildContext context, ScrollController scrollController) {
                      return ColoredBox(
                        color: Colors.white,
                        child: ListView.builder(
                          controller: scrollController,
                          itemExtent: 44.0,
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 8,
                          ),
                          itemCount: 24,
                          itemBuilder: (BuildContext context, int index) {
                            return Align(
                              alignment: Alignment.centerLeft,
                              child: Text(
                                'Item $index',
                                style: const TextStyle(fontSize: 15),
                              ),
                            );
                          },
                        ),
                      );
                    },
              ),
            ),
          ),
        ),
      ],
    );
  }
}
