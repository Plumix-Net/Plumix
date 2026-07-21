import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class DismissibleSizeChangedLayoutDemoPage extends StatefulWidget {
  const DismissibleSizeChangedLayoutDemoPage({super.key});

  @override
  State<DismissibleSizeChangedLayoutDemoPage> createState() =>
      _DismissibleSizeChangedLayoutDemoPageState();
}

class _DismissibleSizeChangedLayoutDemoPageState
    extends State<DismissibleSizeChangedLayoutDemoPage> {
  final List<int> _items = <int>[1, 2, 3];
  int _sizeNotifications = 0;
  bool _expanded = false;
  bool _rightToLeft = false;

  @override
  Widget build(BuildContext context) {
    final TextDirection textDirection = _rightToLeft
        ? TextDirection.rtl
        : TextDirection.ltr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Dismissible + SizeChangedLayoutNotifier',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Swipe rows in either direction. The resize probe reports '
          'notifications only after its established layout size changes.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _rightToLeft ? 'Direction: RTL' : 'Direction: LTR',
              onTap: () {
                setState(() {
                  _rightToLeft = !_rightToLeft;
                });
              },
            ),
            _buildButton(
              label: _expanded ? 'Shrink probe' : 'Grow probe',
              onTap: () {
                setState(() {
                  _expanded = !_expanded;
                });
              },
            ),
            _buildButton(
              label: 'Reset rows',
              onTap: () {
                setState(() {
                  _items
                    ..clear()
                    ..addAll(<int>[1, 2, 3]);
                });
              },
            ),
          ],
        ),
        NotificationListener<SizeChangedLayoutNotification>(
          onNotification: (SizeChangedLayoutNotification notification) {
            WidgetsBinding.instance.addPostFrameCallback((Duration _) {
              if (mounted) {
                setState(() {
                  _sizeNotifications++;
                });
              }
            });
            return false;
          },
          child: Align(
            alignment: Alignment.centerLeft,
            child: SizeChangedLayoutNotifier(
              child: Container(
                width: _expanded ? 320 : 190,
                height: _expanded ? 64 : 44,
                color: const Color(0xFFDCEAF7),
                alignment: Alignment.center,
                child: Text(
                  'layout notifications: $_sizeNotifications',
                  style: const TextStyle(color: Color(0xFF174A72)),
                ),
              ),
            ),
          ),
        ),
        Expanded(
          child: Directionality(
            textDirection: textDirection,
            child: ListView(
              children: _items.map(_buildDismissibleRow).toList(),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildDismissibleRow(int item) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Dismissible(
        key: ValueKey<int>(item),
        background: _buildRowBackground(
          'START →',
          Alignment.centerLeft,
          const Color(0xFF2E7D32),
        ),
        secondaryBackground: _buildRowBackground(
          '← END',
          Alignment.centerRight,
          const Color(0xFFC62828),
        ),
        crossAxisEndOffset: 0.08,
        onDismissed: (DismissDirection direction) {
          setState(() {
            _items.remove(item);
          });
        },
        child: _buildRowSurface('Swipe row $item', const Color(0xFFF4F6F8)),
      ),
    );
  }

  static Widget _buildRowSurface(String label, Color color) {
    return Container(
      height: 58,
      color: color,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      alignment: Alignment.centerLeft,
      child: Text(label, style: const TextStyle(color: Colors.black)),
    );
  }

  static Widget _buildRowBackground(
    String label,
    Alignment alignment,
    Color color,
  ) {
    return Container(
      height: 58,
      color: color,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      alignment: alignment,
      child: Text(label, style: const TextStyle(color: Colors.white)),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 130,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 7),
      ),
    );
  }
}
