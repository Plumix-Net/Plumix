import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class AspectRatioDemoPage extends StatefulWidget {
  const AspectRatioDemoPage({super.key});

  @override
  State<AspectRatioDemoPage> createState() => _AspectRatioDemoPageState();
}

class _AspectRatioDemoPageState extends State<AspectRatioDemoPage> {
  double _aspectRatio = 16 / 9;
  int _spacerFlex = 1;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AspectRatio + Spacer',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'AspectRatio enforces a tight ratio box; Spacer reserves remaining Row space by flex.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: '16:9',
              onTap: () => _setAspectRatio(16 / 9),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: '4:3',
              onTap: () => _setAspectRatio(4 / 3),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: '1:1',
              onTap: () => _setAspectRatio(1),
              width: 88,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Flex 1',
              onTap: () => _setSpacerFlex(1),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Flex 2',
              onTap: () => _setSpacerFlex(2),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Flex 3',
              onTap: () => _setSpacerFlex(3),
              width: 88,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Text(
          'ratio=${_formatRatio(_aspectRatio)}, spacerFlex=$_spacerFlex (second spacer fixed=1)',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 190,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 10,
            children: <Widget>[
              AspectRatio(
                aspectRatio: _aspectRatio,
                child: Container(
                  decoration: BoxDecoration(
                    color: const Color(0xFFCCE3FF),
                    border: Border.all(
                      color: const Color(0xFF1D3557),
                      width: 2,
                    ),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Center(
                    child: Text(
                      'Aspect preview',
                      style: TextStyle(fontSize: 14, color: Colors.black),
                    ),
                  ),
                ),
              ),
              Container(
                height: 44,
                color: Colors.white,
                padding: const EdgeInsets.fromLTRB(8, 6, 8, 6),
                child: Row(
                  children: <Widget>[
                    Container(
                      width: 42,
                      height: 24,
                      color: const Color(0xFF1D3557),
                      child: const Center(
                        child: Text(
                          'L',
                          style: TextStyle(fontSize: 12, color: Colors.white),
                        ),
                      ),
                    ),
                    Spacer(flex: _spacerFlex),
                    Container(
                      width: 28,
                      height: 24,
                      color: const Color(0xFF2A9D8F),
                      child: const Center(
                        child: Text(
                          'M',
                          style: TextStyle(fontSize: 12, color: Colors.white),
                        ),
                      ),
                    ),
                    const Spacer(),
                    Container(
                      width: 54,
                      height: 24,
                      color: const Color(0xFF457B9D),
                      child: const Center(
                        child: Text(
                          'R',
                          style: TextStyle(fontSize: 12, color: Colors.white),
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

  void _setAspectRatio(double value) {
    setState(() {
      _aspectRatio = value;
    });
  }

  void _setSpacerFlex(int value) {
    setState(() {
      _spacerFlex = value;
    });
  }

  static String _formatRatio(double ratio) {
    if ((ratio - (16 / 9)).abs() < 0.001) {
      return '16:9';
    }

    if ((ratio - (4 / 3)).abs() < 0.001) {
      return '4:3';
    }

    if ((ratio - 1).abs() < 0.001) {
      return '1:1';
    }

    return ratio.toStringAsFixed(2);
  }
}
