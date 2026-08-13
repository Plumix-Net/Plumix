import 'package:material_ui/material_ui.dart';

class AnimatedIconDemoPage extends StatefulWidget {
  const AnimatedIconDemoPage({super.key});

  @override
  State<AnimatedIconDemoPage> createState() => _AnimatedIconDemoPageState();
}

class _AnimatedIconDemoPageState extends State<AnimatedIconDemoPage>
    with SingleTickerProviderStateMixin {
  static const List<(String, AnimatedIconData)> _catalog =
      <(String, AnimatedIconData)>[
        ('add_event', AnimatedIcons.add_event),
        ('arrow_menu', AnimatedIcons.arrow_menu),
        ('close_menu', AnimatedIcons.close_menu),
        ('ellipsis_search', AnimatedIcons.ellipsis_search),
        ('event_add', AnimatedIcons.event_add),
        ('home_menu', AnimatedIcons.home_menu),
        ('list_view', AnimatedIcons.list_view),
        ('menu_arrow', AnimatedIcons.menu_arrow),
        ('menu_close', AnimatedIcons.menu_close),
        ('menu_home', AnimatedIcons.menu_home),
        ('pause_play', AnimatedIcons.pause_play),
        ('play_pause', AnimatedIcons.play_pause),
        ('search_ellipsis', AnimatedIcons.search_ellipsis),
        ('view_list', AnimatedIcons.view_list),
      ];

  late final AnimationController _controller;
  bool _forward = false;
  bool _rightToLeft = false;
  bool _large = false;
  bool _muted = false;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      duration: const Duration(milliseconds: 700),
      vsync: this,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.only(right: 12, bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'AnimatedIcon + AnimatedIcons',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Complete Flutter catalog with frame interpolation, IconTheme defaults, and RTL mirroring.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              FilledButton(
                onPressed: _toggleDirection,
                child: Text(_forward ? 'Reverse' : 'Forward'),
              ),
              OutlinedButton(
                onPressed: _toggleTextDirection,
                child: Text(_rightToLeft ? 'RTL: on' : 'RTL: off'),
              ),
              OutlinedButton(
                onPressed: _toggleSize,
                child: Text(_large ? 'Size: 48' : 'Size: 36'),
              ),
              TextButton(
                onPressed: _toggleOpacity,
                child: Text(_muted ? 'Opacity: 0.45' : 'Opacity: 1.0'),
              ),
            ],
          ),
          Text(
            'direction=${_forward ? 'forward' : 'reverse'}, '
            'textDirection=${_rightToLeft ? 'rtl' : 'ltr'}',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Directionality(
            textDirection: _rightToLeft ? TextDirection.rtl : TextDirection.ltr,
            child: IconTheme(
              data: IconThemeData(
                color: const Color(0xFF315A7D),
                size: _large ? 48 : 36,
                opacity: _muted ? 0.45 : 1,
              ),
              child: Wrap(
                spacing: 10,
                runSpacing: 10,
                children: _catalog.map(_buildCatalogTile).toList(),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCatalogTile((String, AnimatedIconData) entry) {
    return Container(
      width: 132,
      height: 100,
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: const Color(0xFFF1F4F8),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        spacing: 6,
        children: <Widget>[
          AnimatedIcon(
            icon: entry.$2,
            progress: _controller,
            semanticLabel: entry.$1,
          ),
          Text(
            entry.$1,
            style: const TextStyle(fontSize: 11, color: Colors.black),
          ),
        ],
      ),
    );
  }

  void _toggleDirection() {
    setState(() => _forward = !_forward);
    if (_forward) {
      _controller.forward();
    } else {
      _controller.reverse();
    }
  }

  void _toggleTextDirection() {
    setState(() => _rightToLeft = !_rightToLeft);
  }

  void _toggleSize() {
    setState(() => _large = !_large);
  }

  void _toggleOpacity() {
    setState(() => _muted = !_muted);
  }
}
