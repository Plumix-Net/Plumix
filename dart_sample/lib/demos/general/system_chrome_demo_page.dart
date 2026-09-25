import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

class SystemChromeDemoPage extends StatefulWidget {
  const SystemChromeDemoPage({super.key});

  @override
  State<SystemChromeDemoPage> createState() => _SystemChromeDemoPageState();
}

class _SystemChromeDemoPageState extends State<SystemChromeDemoPage> {
  bool _alternateTitle = false;
  bool _darkRegion = false;
  String _lastRequest = 'none';
  String _overlaysVisible = 'not reported';

  @override
  void initState() {
    super.initState();
    SystemChrome.setSystemUIChangeCallback(_handleSystemUIChange);
  }

  @override
  void dispose() {
    SystemChrome.setSystemUIChangeCallback(null);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 16,
        children: <Widget>[
          const Text(
            'SystemChrome',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Requests go to the platform over flutter/platform; a platform '
            'that does not implement one ignores it. The window title follows '
            'the Title widget.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          _buildTitleSection(),
          _buildRegionSection(),
          _buildOrientationSection(),
          _buildModeSection(),
          Text(
            'Last request: $_lastRequest',
            style: const TextStyle(color: Color(0xFF31506F)),
          ),
          Text(
            'System overlays visible: $_overlaysVisible',
            style: const TextStyle(color: Color(0xFF31506F)),
          ),
        ],
      ),
    );
  }

  Widget _buildTitleSection() {
    final String label = _alternateTitle
        ? 'System chrome demo'
        : 'Plumix gallery';
    return _buildSection('Application switcher', <Widget>[
      Title(
        title: label,
        color: const Color(0xFF2A9D8F),
        child: Text(
          'Title: $label',
          style: const TextStyle(color: Color(0xFF31506F)),
        ),
      ),
      _buildButton(
        'Toggle title',
        () => setState(() => _alternateTitle = !_alternateTitle),
      ),
    ]);
  }

  Widget _buildRegionSection() {
    final SystemUiOverlayStyle style = _darkRegion
        ? SystemUiOverlayStyle.light
        : SystemUiOverlayStyle.dark;
    return _buildSection('AnnotatedRegion', <Widget>[
      AnnotatedRegion<SystemUiOverlayStyle>(
        value: style,
        child: Container(
          height: 56,
          color: _darkRegion
              ? const Color(0xFF264653)
              : const Color(0xFFE7EDF6),
          alignment: Alignment.center,
          child: Text(
            _darkRegion
                ? 'SystemUiOverlayStyle.light (applies under the status bar)'
                : 'SystemUiOverlayStyle.dark (applies under the status bar)',
            style: TextStyle(color: _darkRegion ? Colors.white : Colors.black),
          ),
        ),
      ),
      _buildButton(
        'Toggle region style',
        () => setState(() => _darkRegion = !_darkRegion),
      ),
    ]);
  }

  Widget _buildOrientationSection() {
    return _buildSection('Preferred orientations', <Widget>[
      Wrap(
        spacing: 8,
        runSpacing: 8,
        children: <Widget>[
          _buildButton(
            'Portrait',
            () => _request(
              'setPreferredOrientations(portraitUp)',
              SystemChrome.setPreferredOrientations(<DeviceOrientation>[
                DeviceOrientation.portraitUp,
              ]),
            ),
          ),
          _buildButton(
            'Landscape',
            () => _request(
              'setPreferredOrientations(landscapeLeft, landscapeRight)',
              SystemChrome.setPreferredOrientations(<DeviceOrientation>[
                DeviceOrientation.landscapeLeft,
                DeviceOrientation.landscapeRight,
              ]),
            ),
          ),
          _buildButton(
            'Any',
            () => _request(
              'setPreferredOrientations([])',
              SystemChrome.setPreferredOrientations(<DeviceOrientation>[]),
            ),
          ),
        ],
      ),
    ]);
  }

  Widget _buildModeSection() {
    return _buildSection('System UI mode', <Widget>[
      Wrap(
        spacing: 8,
        runSpacing: 8,
        children: <Widget>[
          _buildButton(
            'Edge to edge',
            () => _request(
              'setEnabledSystemUIMode(edgeToEdge)',
              SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge),
            ),
          ),
          _buildButton(
            'Immersive sticky',
            () => _request(
              'setEnabledSystemUIMode(immersiveSticky)',
              SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky),
            ),
          ),
          _buildButton(
            'Top bar only',
            () => _request(
              'setEnabledSystemUIMode(manual, [top])',
              SystemChrome.setEnabledSystemUIMode(
                SystemUiMode.manual,
                overlays: <SystemUiOverlay>[SystemUiOverlay.top],
              ),
            ),
          ),
          _buildButton(
            'Restore overlays',
            () => _request(
              'restoreSystemUIOverlays()',
              SystemChrome.restoreSystemUIOverlays(),
            ),
          ),
        ],
      ),
    ]);
  }

  static Widget _buildSection(String title, List<Widget> children) {
    return Container(
      color: const Color(0xFFF4F7FA),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          Text(
            title,
            style: const TextStyle(fontSize: 16, color: Colors.black),
          ),
          ...children,
        ],
      ),
    );
  }

  static Widget _buildButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(backgroundColor: const Color(0xFFDCE3ED)),
      child: Text(label),
    );
  }

  void _request(String description, Future<void> request) {
    setState(() => _lastRequest = description);
  }

  Future<void> _handleSystemUIChange(bool systemOverlaysAreVisible) async {
    if (mounted) {
      setState(
        () => _overlaysVisible = systemOverlaysAreVisible ? 'yes' : 'no',
      );
    }
  }
}
