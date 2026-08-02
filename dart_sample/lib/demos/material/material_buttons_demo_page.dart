import 'package:flutter/material.dart';

class MaterialButtonsDemoPage extends StatefulWidget {
  const MaterialButtonsDemoPage({super.key});

  @override
  State<MaterialButtonsDemoPage> createState() =>
      _MaterialButtonsDemoPageState();
}

class _MaterialButtonsDemoPageState extends State<MaterialButtonsDemoPage> {
  bool _enabled = true;
  bool _iconButtonSelected = false;
  int _textButtonTaps = 0;
  int _elevatedButtonTaps = 0;
  int _outlinedButtonTaps = 0;
  int _filledButtonTaps = 0;
  int _filledTonalButtonTaps = 0;
  int _iconButtonTaps = 0;
  int _filledIconButtonTaps = 0;
  int _filledTonalIconButtonTaps = 0;
  int _outlinedIconButtonTaps = 0;
  int _materialButtonTaps = 0;
  int _rawMaterialButtonTaps = 0;
  bool _useMaterial3 = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Material buttons baseline',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'TextButton / ElevatedButton / OutlinedButton / FilledButton (+ tonal) / IconButton with enabled/disabled and theme-aware defaults.',
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
              label: 'Reset',
              onTap: _resetCounters,
              width: 88,
              background: const Color(0xFFF3E8D8),
            ),
            _buildControlButton(
              label: _useMaterial3 ? 'Icons M3' : 'Icons M2',
              onTap: _toggleIconMaterialVersion,
              width: 96,
              background: const Color(0xFFE8F5E9),
            ),
          ],
        ),
        Text(
          'enabled=$_enabled, iconsM3=$_useMaterial3, text=$_textButtonTaps, '
          'elevated=$_elevatedButtonTaps, outlined=$_outlinedButtonTaps, '
          'filled=$_filledButtonTaps, tonal=$_filledTonalButtonTaps, '
          'material=$_materialButtonTaps, raw=$_rawMaterialButtonTaps, '
          'icon=$_iconButtonTaps, filledIcon=$_filledIconButtonTaps, '
          'tonalIcon=$_filledTonalIconButtonTaps, '
          'outlinedIcon=$_outlinedIconButtonTaps, '
          'iconSelected=$_iconButtonSelected',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        SizedBox(
          width: 240,
          child: TextButton(
            onPressed: _enabled ? _onTextButtonTap : null,
            child: Text('TextButton taps: $_textButtonTaps'),
          ),
        ),
        _buildTextButtonSchemeProbe(context),
        SizedBox(
          width: 240,
          child: ElevatedButton(
            onPressed: _enabled ? _onElevatedButtonTap : null,
            child: Text('ElevatedButton taps: $_elevatedButtonTaps'),
          ),
        ),
        _buildElevatedButtonSchemeProbe(context),
        SizedBox(
          width: 240,
          child: OutlinedButton(
            onPressed: _enabled ? _onOutlinedButtonTap : null,
            child: Text('OutlinedButton taps: $_outlinedButtonTaps'),
          ),
        ),
        SizedBox(
          width: 240,
          child: FilledButton(
            onPressed: _enabled ? _onFilledButtonTap : null,
            child: Text('FilledButton taps: $_filledButtonTaps'),
          ),
        ),
        SizedBox(
          width: 240,
          child: FilledButton.tonal(
            onPressed: _enabled ? _onFilledTonalButtonTap : null,
            child: Text('FilledButton.tonal taps: $_filledTonalButtonTaps'),
          ),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: MaterialButton(
                onPressed: _enabled ? _onMaterialButtonTap : null,
                color: const Color(0xFFE0E0E0),
                child: Text('Material: $_materialButtonTaps'),
              ),
            ),
            Expanded(
              child: RawMaterialButton(
                onPressed: _enabled ? _onRawMaterialButtonTap : null,
                fillColor: const Color(0xFFDDEBF7),
                hoverColor: const Color(0x1F005E7A),
                highlightColor: const Color(0x33005E7A),
                splashColor: const Color(0x33005E7A),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text('Raw: $_rawMaterialButtonTaps'),
              ),
            ),
          ],
        ),
        _buildIconButtonProbe(context),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: ElevatedButton(
                onPressed: _enabled ? _onElevatedButtonTap : null,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF6A994E),
                  foregroundColor: Colors.white,
                ),
                child: const Text('Custom elevated'),
              ),
            ),
            Expanded(
              child: OutlinedButton(
                onPressed: _enabled ? _onOutlinedButtonTap : null,
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFF7B2CBF),
                  side: const BorderSide(color: Color(0xFF7B2CBF), width: 1),
                ),
                child: const Text('Custom outlined'),
              ),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: FilledButton(
                onPressed: _enabled ? _onFilledButtonTap : null,
                style: FilledButton.styleFrom(
                  foregroundColor: Colors.white,
                  backgroundColor: const Color(0xFF005E7A),
                ),
                child: const Text('Custom filled'),
              ),
            ),
            Expanded(
              child: FilledButton.tonal(
                onPressed: _enabled ? _onFilledTonalButtonTap : null,
                style: FilledButton.styleFrom(
                  foregroundColor: const Color(0xFF42275A),
                  backgroundColor: const Color(0xFFD8CFF8),
                ),
                child: const Text('Custom tonal'),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildTextButtonSchemeProbe(BuildContext context) {
    final ThemeData inherited = Theme.of(context);
    final ColorScheme scheme = inherited.colorScheme.copyWith(
      primary: const Color(0xFF006A6A),
      onSurface: const Color(0xFF4D2A6A),
    );
    final ThemeData probeTheme = inherited.copyWith(
      primaryColor: Colors.deepOrange,
      colorScheme: scheme,
    );

    return Theme(
      data: probeTheme,
      child: Row(
        spacing: 8,
        children: <Widget>[
          Expanded(
            child: TextButton(
              onPressed: _enabled ? _onTextButtonTap : null,
              child: const Text('Scheme primary'),
            ),
          ),
          const Expanded(
            child: TextButton(onPressed: null, child: Text('Scheme disabled')),
          ),
        ],
      ),
    );
  }

  Widget _buildElevatedButtonSchemeProbe(BuildContext context) {
    final ThemeData inherited = Theme.of(context);
    final ColorScheme scheme = inherited.colorScheme.copyWith(
      primary: const Color(0xFF425F2D),
      onPrimary: Colors.white,
      surfaceContainerLow: const Color(0xFFE8F2DD),
      onSurface: const Color(0xFF392E21),
      shadow: const Color(0xFF2F3B26),
    );
    final ThemeData probeTheme = inherited.copyWith(
      primaryColor: Colors.deepOrange,
      colorScheme: scheme,
    );

    return Theme(
      data: probeTheme,
      child: Row(
        spacing: 8,
        children: <Widget>[
          Expanded(
            child: ElevatedButton(
              onPressed: _enabled ? _onElevatedButtonTap : null,
              child: const Text('Scheme elevated'),
            ),
          ),
          const Expanded(
            child: ElevatedButton(
              onPressed: null,
              child: Text('Scheme elevated off'),
            ),
          ),
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

  Widget _buildIconButtonProbe(BuildContext context) {
    final Widget tonalButton = IconButtonTheme(
      data: IconButtonThemeData(
        style: IconButton.styleFrom(foregroundColor: const Color(0xFF6A1B9A)),
      ),
      child: SizedBox(
        width: 56,
        height: 56,
        child: IconButton.filledTonal(
          icon: const Icon(Icons.star),
          visualDensity: VisualDensity.compact,
          tooltip: 'Compact tonal favorite',
          onPressed: _enabled ? _onFilledTonalIconButtonTap : null,
        ),
      ),
    );

    return Theme(
      // ignore: deprecated_member_use
      data: Theme.of(context).copyWith(useMaterial3: _useMaterial3),
      child: Row(
        spacing: 8,
        children: <Widget>[
          SizedBox(
            width: 56,
            height: 56,
            child: IconButton(
              isSelected: _iconButtonSelected,
              icon: const Icon(Icons.star_outline),
              selectedIcon: const Icon(Icons.star),
              tooltip: 'Toggle favorite',
              onPressed: _enabled ? _onIconButtonTap : null,
            ),
          ),
          SizedBox(
            width: 56,
            height: 56,
            child: IconButton.filled(
              icon: const Icon(Icons.add),
              tooltip: 'Add',
              onPressed: _enabled ? _onFilledIconButtonTap : null,
            ),
          ),
          tonalButton,
          SizedBox(
            width: 56,
            height: 56,
            child: IconButton.outlined(
              icon: const Icon(Icons.info_outline),
              tooltip: 'Info',
              onPressed: _enabled ? _onOutlinedIconButtonTap : null,
            ),
          ),
        ],
      ),
    );
  }

  void _toggleEnabled() {
    setState(() {
      _enabled = !_enabled;
    });
  }

  void _toggleIconMaterialVersion() {
    setState(() {
      _useMaterial3 = !_useMaterial3;
    });
  }

  void _resetCounters() {
    setState(() {
      _textButtonTaps = 0;
      _elevatedButtonTaps = 0;
      _outlinedButtonTaps = 0;
      _filledButtonTaps = 0;
      _filledTonalButtonTaps = 0;
      _iconButtonTaps = 0;
      _filledIconButtonTaps = 0;
      _filledTonalIconButtonTaps = 0;
      _outlinedIconButtonTaps = 0;
      _materialButtonTaps = 0;
      _rawMaterialButtonTaps = 0;
      _iconButtonSelected = false;
      _enabled = true;
      _useMaterial3 = true;
    });
  }

  void _onTextButtonTap() {
    setState(() {
      _textButtonTaps += 1;
    });
  }

  void _onElevatedButtonTap() {
    setState(() {
      _elevatedButtonTaps += 1;
    });
  }

  void _onOutlinedButtonTap() {
    setState(() {
      _outlinedButtonTaps += 1;
    });
  }

  void _onFilledButtonTap() {
    setState(() {
      _filledButtonTaps += 1;
    });
  }

  void _onFilledTonalButtonTap() {
    setState(() {
      _filledTonalButtonTaps += 1;
    });
  }

  void _onMaterialButtonTap() {
    setState(() {
      _materialButtonTaps += 1;
    });
  }

  void _onRawMaterialButtonTap() {
    setState(() {
      _rawMaterialButtonTaps += 1;
    });
  }

  void _onIconButtonTap() {
    setState(() {
      _iconButtonTaps += 1;
      _iconButtonSelected = !_iconButtonSelected;
    });
  }

  void _onFilledIconButtonTap() {
    setState(() {
      _filledIconButtonTaps += 1;
    });
  }

  void _onFilledTonalIconButtonTap() {
    setState(() {
      _filledTonalIconButtonTaps += 1;
    });
  }

  void _onOutlinedIconButtonTap() {
    setState(() {
      _outlinedIconButtonTaps += 1;
    });
  }
}
