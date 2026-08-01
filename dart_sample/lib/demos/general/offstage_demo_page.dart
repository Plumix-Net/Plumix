import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class OffstageDemoPage extends StatefulWidget {
  const OffstageDemoPage({super.key});

  @override
  State<OffstageDemoPage> createState() => _OffstageDemoPageState();
}

class _OffstageDemoPageState extends State<OffstageDemoPage> {
  bool _offstage = true;
  bool _visible = true;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'Visibility + SliverVisibility + Offstage',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Compare replacement, maintained layout space, and layout-without-paint behavior.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildButton(
                label: 'visible=true',
                onTap: () => _setVisible(true),
                width: 104,
                background: const Color(0xFFDCE3ED),
              ),
              _buildButton(
                label: 'visible=false',
                onTap: () => _setVisible(false),
                width: 110,
                background: const Color(0xFFDCE3ED),
              ),
            ],
          ),
          Text(
            'state: visible=${_visible ? 'true' : 'false'}',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          const Text(
            'maintainState=true keeps the indicator mounted; TickerMode pauses its frame callbacks while hidden.',
            style: TextStyle(fontSize: 11, color: Colors.black54),
          ),
          Visibility(
            visible: _visible,
            maintainState: true,
            child: const SizedBox(
              height: 18,
              child: LinearProgressIndicator(),
            ),
          ),
          Container(
            height: 82,
            color: const Color(0xFFF6F8FB),
            padding: const EdgeInsets.all(8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              spacing: 8,
              children: <Widget>[
                _buildMarker('L', const Color(0xFF1D3557)),
                Visibility.maintain(
                  visible: _visible,
                  child: Container(
                    width: 88,
                    height: 42,
                    color: const Color(0xFFA8DADC),
                    child: const Center(
                      child: Text(
                        'keeps size',
                        style: TextStyle(fontSize: 11, color: Colors.black),
                      ),
                    ),
                  ),
                ),
                _buildMarker('R', const Color(0xFF457B9D)),
              ],
            ),
          ),
          Visibility(
            visible: _visible,
            replacement: Container(
              height: 42,
              color: const Color(0xFFFFE8CC),
              child: const Center(
                child: Text(
                  'Visibility replacement',
                  style: TextStyle(fontSize: 11, color: Colors.black),
                ),
              ),
            ),
            child: Container(
              height: 42,
              color: const Color(0xFFD8F3DC),
              child: const Center(
                child: Text(
                  'Visibility child',
                  style: TextStyle(fontSize: 11, color: Colors.black),
                ),
              ),
            ),
          ),
          SizedBox(
            height: 150,
            child: CustomScrollView(
              slivers: <Widget>[
                SliverToBoxAdapter(
                  child: Container(
                    height: 42,
                    color: const Color(0xFFE9ECEF),
                    child: const Center(
                      child: Text(
                        'sliver before',
                        style: TextStyle(fontSize: 11, color: Colors.black),
                      ),
                    ),
                  ),
                ),
                SliverVisibility(
                  visible: _visible,
                  replacementSliver: SliverToBoxAdapter(
                    child: Container(
                      height: 42,
                      color: const Color(0xFFFFE8CC),
                      child: const Center(
                        child: Text(
                          'replacement sliver',
                          style: TextStyle(fontSize: 11, color: Colors.black),
                        ),
                      ),
                    ),
                  ),
                  sliver: SliverToBoxAdapter(
                    child: Container(
                      height: 42,
                      color: const Color(0xFFBDE0FE),
                      child: const Center(
                        child: Text(
                          'SliverVisibility child',
                          style: TextStyle(fontSize: 11, color: Colors.black),
                        ),
                      ),
                    ),
                  ),
                ),
                SliverToBoxAdapter(
                  child: Container(
                    height: 42,
                    color: const Color(0xFFE9ECEF),
                    child: const Center(
                      child: Text(
                        'sliver after',
                        style: TextStyle(fontSize: 11, color: Colors.black),
                      ),
                    ),
                  ),
                ),
              ],
            ),
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
                              style: TextStyle(
                                fontSize: 11,
                                color: Colors.black,
                              ),
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

  void _setVisible(bool value) {
    setState(() {
      _visible = value;
    });
  }
}
