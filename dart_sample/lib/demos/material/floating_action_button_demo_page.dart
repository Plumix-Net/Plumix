import 'package:flutter/material.dart';

class FloatingActionButtonDemoPage extends StatefulWidget {
  const FloatingActionButtonDemoPage({super.key});

  @override
  State<FloatingActionButtonDemoPage> createState() =>
      _FloatingActionButtonDemoPageState();
}

class _FloatingActionButtonDemoPageState
    extends State<FloatingActionButtonDemoPage> {
  bool _enabled = true;
  bool _extendedOpen = true;
  int _regularTaps = 0;
  int _smallTaps = 0;
  int _largeTaps = 0;
  int _extendedTaps = 0;
  int _themedTaps = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData themedData = Theme.of(context).copyWith(
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        foregroundColor: Colors.white,
        backgroundColor: Color(0xFF00639B),
        sizeConstraints: BoxConstraints.tightFor(width: 64, height: 64),
        extendedSizeConstraints: BoxConstraints.tightFor(height: 60),
      ),
    );

    return SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'FloatingActionButton baseline',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Regular/small/large/extended FAB defaults, elevation states, and theme overrides.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: _enabled ? 'Enabled' : 'Disabled',
                onTap: _toggleEnabled,
                width: 108,
                background: const Color(0xFFE9F0FF),
              ),
              _buildControlButton(
                label: _extendedOpen ? 'Extended: open' : 'Extended: icon',
                onTap: _toggleExtended,
                width: 146,
                background: const Color(0xFFEAE4FF),
              ),
              _buildControlButton(
                label: 'Reset',
                onTap: _resetCounters,
                width: 88,
                background: const Color(0xFFF3E8D8),
              ),
            ],
          ),
          Text(
            'enabled=$_enabled, extended=${_extendedOpen ? 'open' : 'icon'}, regular=$_regularTaps, small=$_smallTaps, large=$_largeTaps, extendedTaps=$_extendedTaps, themed=$_themedTaps',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          Column(
            mainAxisSize: MainAxisSize.min,
            spacing: 8,
            children: <Widget>[
              _buildProbeCard(
                title: 'Regular',
                subtitle: '56x56',
                fab: FloatingActionButton(
                  onPressed: _enabled ? _onRegularTap : null,
                  child: const Icon(Icons.add),
                ),
              ),
              _buildProbeCard(
                title: 'Small',
                subtitle: '40x40',
                fab: FloatingActionButton.small(
                  onPressed: _enabled ? _onSmallTap : null,
                  child: const Icon(Icons.menu),
                ),
              ),
              _buildProbeCard(
                title: 'Large',
                subtitle: '96x96',
                fab: FloatingActionButton.large(
                  onPressed: _enabled ? _onLargeTap : null,
                  child: const Icon(Icons.star),
                ),
              ),
            ],
          ),
          _buildProbeCard(
            title: 'Extended',
            subtitle: 'label + icon / collapsed icon',
            fab: FloatingActionButton.extended(
              onPressed: _enabled ? _onExtendedTap : null,
              isExtended: _extendedOpen,
              icon: const Icon(Icons.add),
              label: const Text('Create'),
            ),
          ),
          Theme(
            data: themedData,
            child: _buildProbeCard(
              title: 'Theme override',
              subtitle: 'FloatingActionButtonTheme colors + size',
              fab: FloatingActionButton(
                onPressed: _enabled ? _onThemedTap : null,
                child: const Icon(Icons.info_outline),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildProbeCard({
    required String title,
    required String subtitle,
    required Widget fab,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF1F4F9),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFD6DEEA), width: 1),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(fontSize: 13, color: Colors.black),
          ),
          Text(
            subtitle,
            style: const TextStyle(fontSize: 12, color: Colors.black54),
          ),
          SizedBox(height: 112, child: Center(child: fab)),
        ],
      ),
    );
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onTap,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }

  void _toggleEnabled() {
    setState(() {
      _enabled = !_enabled;
    });
  }

  void _toggleExtended() {
    setState(() {
      _extendedOpen = !_extendedOpen;
    });
  }

  void _resetCounters() {
    setState(() {
      _enabled = true;
      _extendedOpen = true;
      _regularTaps = 0;
      _smallTaps = 0;
      _largeTaps = 0;
      _extendedTaps = 0;
      _themedTaps = 0;
    });
  }

  void _onRegularTap() {
    setState(() {
      _regularTaps += 1;
    });
  }

  void _onSmallTap() {
    setState(() {
      _smallTaps += 1;
    });
  }

  void _onLargeTap() {
    setState(() {
      _largeTaps += 1;
    });
  }

  void _onExtendedTap() {
    setState(() {
      _extendedTaps += 1;
    });
  }

  void _onThemedTap() {
    setState(() {
      _themedTaps += 1;
    });
  }
}
