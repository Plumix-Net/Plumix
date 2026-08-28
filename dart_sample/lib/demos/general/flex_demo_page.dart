import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class FlexDemoPage extends StatefulWidget {
  const FlexDemoPage({super.key});

  @override
  State<FlexDemoPage> createState() => _FlexDemoPageState();
}

class _FlexDemoPageState extends State<FlexDemoPage> {
  static const List<MainAxisAlignment> _alignments = <MainAxisAlignment>[
    MainAxisAlignment.start,
    MainAxisAlignment.end,
    MainAxisAlignment.center,
    MainAxisAlignment.spaceBetween,
    MainAxisAlignment.spaceAround,
    MainAxisAlignment.spaceEvenly,
  ];

  static const List<CrossAxisAlignment> _crossAlignments = <CrossAxisAlignment>[
    CrossAxisAlignment.start,
    CrossAxisAlignment.end,
    CrossAxisAlignment.center,
    CrossAxisAlignment.stretch,
  ];

  int _alignmentIndex = 0;
  int _crossAlignmentIndex = 2;
  double _spacing = 0;
  bool _rightToLeft = false;
  bool _bottomToTop = false;
  bool _overflow = false;
  Clip _clipBehavior = Clip.none;

  @override
  Widget build(BuildContext context) {
    final MainAxisAlignment alignment = _alignments[_alignmentIndex];
    final CrossAxisAlignment crossAlignment =
        _crossAlignments[_crossAlignmentIndex];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Flex / Row / Column',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'RenderFlex distributes free space by mainAxisAlignment, inserts `spacing` between '
          'children, and flips both axes from textDirection/verticalDirection.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Main axis',
              onTap: _cycleAlignment,
              width: 104,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Cross axis',
              onTap: _cycleCrossAlignment,
              width: 104,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Spacing',
              onTap: _cycleSpacing,
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'RTL',
              onTap: _toggleTextDirection,
              width: 78,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Up',
              onTap: _toggleVerticalDirection,
              width: 78,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Overflow',
              onTap: _toggleOverflow,
              width: 96,
              background: const Color(0xFFF6E7E7),
            ),
            _buildButton(
              label: 'Clip',
              onTap: _cycleClip,
              width: 78,
              background: const Color(0xFFF6E7E7),
            ),
          ],
        ),
        Text(
          'main=$alignment, cross=$crossAlignment, '
          'spacing=${_spacing.toStringAsFixed(0)}, '
          'textDirection=${_rightToLeft ? 'Rtl' : 'Ltr'}, '
          'verticalDirection=${_bottomToTop ? 'Up' : 'Down'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Text(
          'overflow=${_overflow ? 'on' : 'off'}, clipBehavior=$_clipBehavior',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          height: 120,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(8),
          child: Directionality(
            textDirection: _rightToLeft ? TextDirection.rtl : TextDirection.ltr,
            child: Flex(
              direction: Axis.horizontal,
              mainAxisAlignment: alignment,
              crossAxisAlignment: crossAlignment,
              verticalDirection: _bottomToTop
                  ? VerticalDirection.up
                  : VerticalDirection.down,
              spacing: _spacing,
              clipBehavior: _clipBehavior,
              children: <Widget>[
                _tile('1', const Color(0xFF1D3557), _overflow ? 150 : 56, 40),
                _tile('2', const Color(0xFF2A9D8F), _overflow ? 150 : 56, 64),
                _tile('3', const Color(0xFF457B9D), _overflow ? 150 : 56, 48),
              ],
            ),
          ),
        ),
        Container(
          height: 150,
          color: const Color(0xFFEFF3E7),
          padding: const EdgeInsets.all(8),
          child: Directionality(
            textDirection: _rightToLeft ? TextDirection.rtl : TextDirection.ltr,
            child: const Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              spacing: 12,
              children: <Widget>[
                Text(
                  'Baseline',
                  style: TextStyle(fontSize: 12, color: Colors.black),
                ),
                Text(
                  'aligned',
                  style: TextStyle(fontSize: 22, color: Colors.black),
                ),
                Text(
                  'row',
                  style: TextStyle(fontSize: 32, color: Colors.black),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  static Widget _tile(String label, Color color, double width, double height) {
    return Container(
      width: width,
      height: height,
      color: color,
      child: Center(
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.white),
        ),
      ),
    );
  }

  Widget _buildButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: background,
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  void _cycleAlignment() {
    setState(() {
      _alignmentIndex = (_alignmentIndex + 1) % _alignments.length;
    });
  }

  void _cycleCrossAlignment() {
    setState(() {
      _crossAlignmentIndex =
          (_crossAlignmentIndex + 1) % _crossAlignments.length;
    });
  }

  void _cycleSpacing() {
    setState(() {
      _spacing = _spacing >= 24 ? 0 : _spacing + 8;
    });
  }

  void _toggleTextDirection() {
    setState(() {
      _rightToLeft = !_rightToLeft;
    });
  }

  void _toggleVerticalDirection() {
    setState(() {
      _bottomToTop = !_bottomToTop;
    });
  }

  void _toggleOverflow() {
    setState(() {
      _overflow = !_overflow;
    });
  }

  void _cycleClip() {
    setState(() {
      _clipBehavior = _clipBehavior == Clip.none ? Clip.hardEdge : Clip.none;
    });
  }
}
