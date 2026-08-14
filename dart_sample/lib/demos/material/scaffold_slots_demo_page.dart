import 'package:material_ui/material_ui.dart';

class ScaffoldSlotsDemoPage extends StatefulWidget {
  const ScaffoldSlotsDemoPage({super.key});

  @override
  State<ScaffoldSlotsDemoPage> createState() => _ScaffoldSlotsDemoPageState();
}

class _ScaffoldSlotsDemoPageState extends State<ScaffoldSlotsDemoPage> {
  static const List<AlignmentDirectional> _footerAlignments =
      <AlignmentDirectional>[
        AlignmentDirectional.centerStart,
        AlignmentDirectional.center,
        AlignmentDirectional.centerEnd,
      ];

  static const List<String> _footerAlignmentLabels = <String>[
    'start',
    'center',
    'end',
  ];

  bool _showFooter = true;
  int _footerAlignmentIndex = 2;
  bool _useFooterDecoration = false;
  bool _extendBody = false;
  bool _extendBodyBehindAppBar = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Scaffold slots',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'persistentFooterButtons, the extendBody padding restoration, and drawer paint order.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _showFooter ? 'Footer on' : 'Footer off',
              onTap: () => setState(() => _showFooter = !_showFooter),
              width: 96,
              background: const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              label: 'Align ${_footerAlignmentLabels[_footerAlignmentIndex]}',
              onTap: () => setState(
                () => _footerAlignmentIndex =
                    (_footerAlignmentIndex + 1) % _footerAlignments.length,
              ),
              width: 106,
              background: const Color(0xFFEFE8F8),
            ),
            _buildControlButton(
              label: _useFooterDecoration ? 'Decoration' : 'Divider',
              onTap: () =>
                  setState(() => _useFooterDecoration = !_useFooterDecoration),
              width: 106,
              background: const Color(0xFFF7E9E3),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              label: _extendBody ? 'extendBody on' : 'extendBody off',
              onTap: () => setState(() => _extendBody = !_extendBody),
              width: 132,
              background: const Color(0xFFE8F5E9),
            ),
            _buildControlButton(
              label: _extendBodyBehindAppBar
                  ? 'behind bar on'
                  : 'behind bar off',
              onTap: () => setState(
                () => _extendBodyBehindAppBar = !_extendBodyBehindAppBar,
              ),
              width: 132,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Text(
          'footer=${_showFooter ? "true" : "false"}, '
          'alignment=${_footerAlignmentLabels[_footerAlignmentIndex]}, '
          'decoration=${_useFooterDecoration ? "true" : "false"}, '
          'extendBody=${_extendBody ? "true" : "false"}, '
          'extendBodyBehindAppBar=${_extendBodyBehindAppBar ? "true" : "false"}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: Container(
            decoration: BoxDecoration(
              color: const Color(0xFFFDFEFF),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFFD6DEEA), width: 1),
            ),
            child: Scaffold(
              appBar: AppBar(title: const Text('Slots preview')),
              extendBody: _extendBody,
              extendBodyBehindAppBar: _extendBodyBehindAppBar,
              drawer: _buildDrawerPanel(isStartDrawer: true),
              endDrawer: _buildDrawerPanel(isStartDrawer: false),
              persistentFooterAlignment:
                  _footerAlignments[_footerAlignmentIndex],
              persistentFooterDecoration: _useFooterDecoration
                  ? const BoxDecoration(color: Color(0xFFEFF4FF))
                  : null,
              persistentFooterButtons: _showFooter
                  ? <Widget>[
                      _buildFooterButton('Reset', _reset),
                      _buildFooterButton('Save', () {}),
                    ]
                  : null,
              bottomNavigationBar: Container(
                color: const Color(0xFFE3ECFB),
                height: 48,
                child: const Center(
                  child: Text(
                    'bottomNavigationBar (48pt)',
                    style: TextStyle(fontSize: 12, color: Color(0xFF30404D)),
                  ),
                ),
              ),
              body: Builder(
                builder: (BuildContext context) => _buildPreviewBody(context),
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildPreviewBody(BuildContext context) {
    final EdgeInsets padding = MediaQuery.paddingOf(context);

    return Container(
      color: const Color(0xFFF2F6FF),
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          const Text(
            'The body reports the padding _BodyBuilder restores for the slots it extends behind.',
            style: TextStyle(fontSize: 12, color: Color(0xFF30404D)),
          ),
          Text(
            'body MediaQuery padding: top=${padding.top.toStringAsFixed(1)}, '
            'bottom=${padding.bottom.toStringAsFixed(1)}',
            style: const TextStyle(fontSize: 12, color: Color(0xFF0D47A1)),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildControlButton(
                label: 'Open start',
                onTap: () => Scaffold.of(context).openDrawer(),
                width: 100,
                background: const Color(0xFFE9EEF5),
              ),
              _buildControlButton(
                label: 'Open end',
                onTap: () => Scaffold.of(context).openEndDrawer(),
                width: 100,
                background: const Color(0xFFEFE8F8),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildDrawerPanel({required bool isStartDrawer}) {
    final String title = isStartDrawer ? 'Start drawer' : 'End drawer';
    final Color accent = isStartDrawer
        ? const Color(0xFF0D47A1)
        : const Color(0xFF4A148C);

    return Drawer(
      child: Builder(
        builder: (BuildContext context) => Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              Text(title, style: TextStyle(fontSize: 16, color: accent)),
              const Text(
                'The opened end drawer is appended last, so it paints over the start drawer.',
                style: TextStyle(fontSize: 12, color: Colors.black54),
              ),
              _buildControlButton(
                label: 'Close',
                onTap: isStartDrawer
                    ? () => Scaffold.of(context).closeDrawer()
                    : () => Scaffold.of(context).closeEndDrawer(),
                width: 84,
                background: const Color(0xFFE9EEF5),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildFooterButton(String label, VoidCallback onTap) {
    return TextButton(
      onPressed: onTap,
      style: TextButton.styleFrom(
        foregroundColor: const Color(0xFF0D47A1),
        minimumSize: const Size(0, 36),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }

  Widget _buildControlButton({
    required String label,
    required VoidCallback? onTap,
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

  void _reset() {
    setState(() {
      _showFooter = true;
      _footerAlignmentIndex = 2;
      _useFooterDecoration = false;
      _extendBody = false;
      _extendBodyBehindAppBar = false;
    });
  }
}
