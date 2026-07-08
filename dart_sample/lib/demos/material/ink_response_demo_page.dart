import 'package:flutter/material.dart';

class InkResponseDemoPage extends StatefulWidget {
  const InkResponseDemoPage({super.key});

  @override
  State<InkResponseDemoPage> createState() => _InkResponseDemoPageState();
}

class _InkResponseDemoPageState extends State<InkResponseDemoPage> {
  bool _enabled = true;
  bool _customOverlay = true;
  int _responseTaps = 0;
  int _wellTaps = 0;
  int _secondaryTaps = 0;
  String _interaction = 'Ready';

  @override
  Widget build(BuildContext context) {
    final WidgetStateProperty<Color?>? overlay = _customOverlay
        ? WidgetStateProperty.resolveWith<Color?>((Set<WidgetState> states) {
            if (states.contains(WidgetState.pressed)) {
              return const Color(0x556750A4);
            }
            if (states.contains(WidgetState.hovered)) {
              return const Color(0x336750A4);
            }
            if (states.contains(WidgetState.focused)) {
              return const Color(0x446750A4);
            }
            return null;
          })
        : null;

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 14,
        children: <Widget>[
          const Text(
            'InkResponse + InkWell',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Circle/uncontained versus rectangle/contained ink, primary + secondary gestures, hover/focus, and overlay states.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: <Widget>[
              _buildInkResponse(overlay),
              _buildInkWell(overlay),
            ],
          ),
          Text(
            'InkResponse taps: $_responseTaps  |  InkWell taps: $_wellTaps  |  secondary: $_secondaryTaps',
            style: const TextStyle(fontSize: 14, color: Colors.black),
          ),
          Text(
            'Interaction: $_interaction',
            style: const TextStyle(fontSize: 13, color: Colors.black54),
          ),
          Row(
            spacing: 10,
            children: <Widget>[
              Expanded(
                child: FilledButton(
                  onPressed: () => setState(() => _enabled = !_enabled),
                  child: Text(_enabled ? 'Disable ink' : 'Enable ink'),
                ),
              ),
              Expanded(
                child: OutlinedButton(
                  onPressed: () =>
                      setState(() => _customOverlay = !_customOverlay),
                  child: Text(
                    _customOverlay ? 'Use theme colors' : 'Use custom overlay',
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInkResponse(WidgetStateProperty<Color?>? overlay) {
    return Column(
      spacing: 8,
      children: <Widget>[
        const Text(
          'InkResponse',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        Container(
          width: 112,
          height: 112,
          decoration: const BoxDecoration(
            color: Color(0xFFEADDFF),
            shape: BoxShape.circle,
          ),
          child: InkResponse(
            onTap: _enabled ? () => setState(() => _responseTaps += 1) : null,
            onSecondaryTap: _enabled ? _handleSecondaryTap : null,
            onHover: (bool value) =>
                setState(() => _interaction = 'InkResponse hover: $value'),
            onHighlightChanged: (bool value) =>
                setState(() => _interaction = 'InkResponse pressed: $value'),
            overlayColor: overlay,
            radius: 58,
            child: const Center(
              child: Icon(Icons.star, size: 32, color: Color(0xFF6750A4)),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildInkWell(WidgetStateProperty<Color?>? overlay) {
    return Column(
      spacing: 8,
      children: <Widget>[
        const Text(
          'InkWell',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        Container(
          width: 150,
          height: 96,
          decoration: BoxDecoration(
            color: const Color(0xFFD7E3FF),
            borderRadius: BorderRadius.circular(18),
          ),
          child: InkWell(
            onTap: _enabled ? () => setState(() => _wellTaps += 1) : null,
            onLongPress: _enabled
                ? () => setState(() => _interaction = 'InkWell long press')
                : null,
            onSecondaryTap: _enabled ? _handleSecondaryTap : null,
            onHover: (bool value) =>
                setState(() => _interaction = 'InkWell hover: $value'),
            onHighlightChanged: (bool value) =>
                setState(() => _interaction = 'InkWell pressed: $value'),
            overlayColor: overlay,
            borderRadius: BorderRadius.circular(18),
            child: const Center(
              child: Text(
                'Tap / hold',
                style: TextStyle(fontSize: 15, color: Colors.black),
              ),
            ),
          ),
        ),
      ],
    );
  }

  void _handleSecondaryTap() {
    setState(() {
      _secondaryTaps += 1;
      _interaction = 'Secondary tap';
    });
  }
}
