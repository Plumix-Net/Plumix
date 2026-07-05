import 'package:flutter/material.dart';

class BadgeTooltipDemoPage extends StatefulWidget {
  const BadgeTooltipDemoPage({super.key});

  @override
  State<BadgeTooltipDemoPage> createState() => _BadgeTooltipDemoPageState();
}

class _BadgeTooltipDemoPageState extends State<BadgeTooltipDemoPage> {
  int _count = 7;
  bool _isLabelVisible = true;
  bool _useThemeOverrides = false;

  @override
  Widget build(BuildContext context) {
    Widget content = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text('Badge + Tooltip', style: TextStyle(fontSize: 20)),
        const SizedBox(height: 14),
        const Text(
          'Count/stadium/small badge geometry plus hover, long-press, timing, and theme precedence.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            _controlButton('Count +1', () => setState(() => _count++)),
            const SizedBox(width: 8),
            _controlButton(
              _isLabelVisible ? 'Label on' : 'Label off',
              () => setState(() => _isLabelVisible = !_isLabelVisible),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _useThemeOverrides ? 'Theme on' : 'Theme off',
              () => setState(() => _useThemeOverrides = !_useThemeOverrides),
            ),
          ],
        ),
        const SizedBox(height: 14),
        Container(
          color: const Color(0xFFF7F2FA),
          padding: const EdgeInsets.all(20),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: <Widget>[
              _probe(
                'Count',
                Badge.count(
                  count: _count,
                  maxCount: 99,
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.info_outline, size: 32),
                ),
              ),
              _probe(
                'Small',
                Badge(
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.star_outline, size: 32),
                ),
              ),
              _probe(
                'Widget override',
                Badge(
                  backgroundColor: const Color(0xFF00695C),
                  textColor: Colors.white,
                  largeSize: 20,
                  offset: const Offset(7, -7),
                  label: const Text('NEW'),
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.check, size: 32),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        const Text(
          'Hover or long-press these controls:',
          style: TextStyle(fontSize: 14),
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            Tooltip(
              message: 'Default tooltip',
              child: OutlinedButton(
                onPressed: () {},
                child: const Text('Default'),
              ),
            ),
            const SizedBox(width: 12),
            Tooltip(
              message: 'Widget override tooltip',
              preferBelow: false,
              verticalOffset: 28,
              decoration: BoxDecoration(
                color: const Color(0xFF4527A0),
                borderRadius: BorderRadius.circular(8),
              ),
              textStyle: const TextStyle(color: Colors.white, fontSize: 13),
              waitDuration: const Duration(milliseconds: 250),
              child: OutlinedButton(
                onPressed: () {},
                child: const Text('Above + delay'),
              ),
            ),
          ],
        ),
      ],
    );

    if (!_useThemeOverrides) {
      return content;
    }

    content = BadgeTheme(
      data: const BadgeThemeData(
        backgroundColor: Color(0xFFB3261E),
        textColor: Colors.white,
        largeSize: 18,
        smallSize: 8,
        padding: EdgeInsets.symmetric(horizontal: 5),
      ),
      child: TooltipTheme(
        data: TooltipThemeData(
          decoration: BoxDecoration(
            color: const Color(0xFF00695C),
            borderRadius: BorderRadius.circular(6),
          ),
          textStyle: const TextStyle(color: Colors.white, fontSize: 12),
          waitDuration: const Duration(milliseconds: 150),
          exitDuration: const Duration(milliseconds: 200),
        ),
        child: content,
      ),
    );
    return content;
  }

  Widget _probe(String label, Widget child) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        child,
        const SizedBox(height: 8),
        Text(label, style: const TextStyle(fontSize: 12)),
      ],
    );
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }
}
