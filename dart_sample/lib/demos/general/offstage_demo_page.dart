import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class OffstageDemoPage extends StatefulWidget {
  const OffstageDemoPage({super.key});

  @override
  State<OffstageDemoPage> createState() => _OffstageDemoPageState();
}

class _OffstageDemoPageState extends State<OffstageDemoPage> {
  bool _offstage = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Offstage',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'When offstage=true, child is laid out but not painted/hit-tested and takes no room in parent layout.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'offstage=true',
              onTap: () => _setOffstage(true),
              width: 112,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'offstage=false',
              onTap: () => _setOffstage(false),
              width: 118,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Text(
          'state: offstage=${_offstage ? 'true' : 'false'}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 190,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'Row layout (middle child disappears from layout when offstage=true)',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 72,
                color: Colors.white,
                padding: const EdgeInsets.fromLTRB(8, 10, 8, 10),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  spacing: 8,
                  children: <Widget>[
                    _buildMarker('L', const Color(0xFF1D3557)),
                    Offstage(
                      offstage: _offstage,
                      child: Container(
                        width: 120,
                        height: 44,
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
                            'Offstage child',
                            style: TextStyle(fontSize: 11, color: Colors.black),
                          ),
                        ),
                      ),
                    ),
                    _buildMarker('R', const Color(0xFF457B9D)),
                  ],
                ),
              ),
              const Text(
                'Tip: switch state and watch L/R gap change.',
                style: TextStyle(fontSize: 11, color: Colors.black54),
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

  static Widget _buildMarker(String label, Color color) {
    return Container(
      width: 34,
      height: 34,
      color: color,
      child: Center(
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.white),
        ),
      ),
    );
  }

  void _setOffstage(bool value) {
    setState(() {
      _offstage = value;
    });
  }
}
