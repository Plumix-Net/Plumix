import 'package:flutter/material.dart';

class SliverAppBarDemoPage extends StatefulWidget {
  const SliverAppBarDemoPage({super.key});

  @override
  State<SliverAppBarDemoPage> createState() => _SliverAppBarDemoPageState();
}

class _SliverAppBarDemoPageState extends State<SliverAppBarDemoPage> {
  int _variant = 0;
  bool _pinned = true;
  bool _floating = false;
  bool _snap = false;
  bool _stretch = false;

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: <Widget>[
        _buildAppBar(),
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 8,
              children: <Widget>[
                const Text(
                  'SliverAppBar + FlexibleSpaceBar',
                  style: TextStyle(fontSize: 20),
                ),
                const Text(
                  'Collapse, parallax, floating reveal, pinned extent, snap contract, and Material 3 medium/large variants.',
                  style: TextStyle(fontSize: 14, color: Colors.black54),
                ),
                Wrap(
                  spacing: 6,
                  runSpacing: 6,
                  children: <Widget>[
                    TextButton(
                      onPressed: () =>
                          setState(() => _variant = (_variant + 1) % 3),
                      child: Text(_variantLabel),
                    ),
                    TextButton(
                      onPressed: () => setState(() => _pinned = !_pinned),
                      child: Text(_pinned ? 'Pinned on' : 'Pinned off'),
                    ),
                    TextButton(
                      onPressed: _toggleFloating,
                      child: Text(_floating ? 'Floating on' : 'Floating off'),
                    ),
                    TextButton(
                      onPressed: _toggleSnap,
                      child: Text(_snap ? 'Snap on' : 'Snap off'),
                    ),
                    TextButton(
                      onPressed: () => setState(() => _stretch = !_stretch),
                      child: Text(_stretch ? 'Stretch on' : 'Stretch off'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(12, 0, 12, 20),
          sliver: SliverList.builder(
            itemCount: 24,
            itemBuilder: (BuildContext context, int index) {
              return Container(
                margin: const EdgeInsets.only(bottom: 6),
                padding: const EdgeInsets.all(14),
                color: index.isEven
                    ? const Color(0xFFF3EDF7)
                    : const Color(0xFFE8DEF8),
                child: Text('Scrollable row #${index + 1}'),
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildAppBar() {
    final title = Text(
      _variant == 0
          ? 'Flexible space'
          : _variant == 1
          ? 'Medium app bar'
          : 'Large app bar',
    );
    final flexible = FlexibleSpaceBar(
      title: title,
      stretchModes: const <StretchMode>[
        StretchMode.zoomBackground,
        StretchMode.fadeTitle,
      ],
      background: const ColoredBox(
        color: Color(0xFF6750A4),
        child: Center(
          child: Text(
            'PARALLAX',
            style: TextStyle(fontSize: 30, color: Colors.white),
          ),
        ),
      ),
    );

    return switch (_variant) {
      1 => SliverAppBar.medium(
        title: title,
        pinned: _pinned,
        floating: _floating,
        snap: _snap,
        stretch: _stretch,
      ),
      2 => SliverAppBar.large(
        title: title,
        pinned: _pinned,
        floating: _floating,
        snap: _snap,
        stretch: _stretch,
      ),
      _ => SliverAppBar(
        pinned: _pinned,
        floating: _floating,
        snap: _snap,
        stretch: _stretch,
        expandedHeight: 220,
        flexibleSpace: flexible,
      ),
    };
  }

  String get _variantLabel => switch (_variant) {
    1 => 'Medium',
    2 => 'Large',
    _ => 'Regular',
  };

  void _toggleFloating() => setState(() {
    _floating = !_floating;
    if (!_floating) _snap = false;
  });

  void _toggleSnap() => setState(() {
    if (!_floating) _floating = true;
    _snap = !_snap;
  });
}
