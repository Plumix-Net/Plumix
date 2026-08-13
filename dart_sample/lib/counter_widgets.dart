import 'dart:math';

import 'package:material_ui/material_ui.dart';

class CounterTapButton extends StatelessWidget {
  const CounterTapButton({
    super.key,
    required this.label,
    required this.onTap,
    required this.background,
    required this.foreground,
    required this.fontSize,
    this.padding = const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
  });

  final String label;
  final VoidCallback? onTap;
  final Color background;
  final Color foreground;
  final double fontSize;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: onTap,
      child: Container(
        color: onTap == null ? _disabledColor(background) : background,
        padding: padding,
        child: Text(
          label,
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: fontSize, color: foreground),
        ),
      ),
    );
  }

  static Color _disabledColor(Color color) {
    return color.withAlpha((0.45 * 255).round());
  }
}

class KeyedListItem extends StatefulWidget {
  const KeyedListItem({required this.id, super.key});

  final int id;

  @override
  State<KeyedListItem> createState() => _KeyedListItemState();
}

class _KeyedListItemState extends State<KeyedListItem> {
  late final int _token = Random().nextInt(9000) + 1000;
  int _taps = 0;

  @override
  Widget build(BuildContext context) {
    return CounterTapButton(
      label: 'id=${widget.id} token=$_token taps=$_taps',
      onTap: () => setState(() => _taps += 1),
      background: const Color(0xFFF5F5F5),
      foreground: Colors.black,
      fontSize: 14,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}

class MovableBadge extends StatefulWidget {
  const MovableBadge({super.key});

  @override
  State<MovableBadge> createState() => MovableBadgeState();
}

class MovableBadgeState extends State<MovableBadge> {
  int _taps = 0;

  @override
  Widget build(BuildContext context) {
    return CounterTapButton(
      label: 'global taps=$_taps',
      onTap: () => setState(() => _taps += 1),
      background: Colors.deepOrange,
      foreground: Colors.white,
      fontSize: 14,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}

class KeepAliveListItem extends StatefulWidget {
  const KeepAliveListItem({
    required this.index,
    required this.keepAlive,
    required this.onTap,
    super.key,
  });

  final int index;
  final bool keepAlive;
  final VoidCallback? onTap;

  @override
  State<KeepAliveListItem> createState() => _KeepAliveListItemState();
}

class _KeepAliveListItemState extends State<KeepAliveListItem>
    with AutomaticKeepAliveClientMixin {
  late final int _token = Random().nextInt(9000) + 1000;
  int _localTaps = 0;

  @override
  bool get wantKeepAlive => widget.keepAlive;

  @override
  Widget build(BuildContext context) {
    super.build(context);

    return CounterTapButton(
      label:
          'row #${widget.index} keepAlive=${widget.keepAlive ? 'on' : 'off'} token=$_token local taps=$_localTaps',
      onTap: () {
        setState(() => _localTaps += 1);
        widget.onTap?.call();
      },
      background: widget.keepAlive ? const Color(0xFFE8F5E9) : Colors.white,
      foreground: Colors.black,
      fontSize: 13,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}
