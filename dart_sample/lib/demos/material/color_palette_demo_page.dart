import 'package:material_ui/material_ui.dart';

class ColorPaletteDemoPage extends StatefulWidget {
  const ColorPaletteDemoPage({super.key});

  @override
  State<ColorPaletteDemoPage> createState() => _ColorPaletteDemoPageState();
}

class _ColorPaletteDemoPageState extends State<ColorPaletteDemoPage> {
  static const List<(String, MaterialColor)> _swatches = <(String, MaterialColor)>[
    ('blue', Colors.blue),
    ('green', Colors.green),
    ('deepOrange', Colors.deepOrange),
    ('grey', Colors.grey),
  ];

  static const List<int> _shades = <int>[
    50,
    100,
    200,
    300,
    400,
    500,
    600,
    700,
    800,
    900,
  ];

  int _swatchIndex = 0;
  bool _useMaterial3 = false;

  @override
  Widget build(BuildContext context) {
    final (String name, MaterialColor swatch) = _swatches[_swatchIndex];
    final ThemeData pageTheme = ThemeData(
      useMaterial3: _useMaterial3,
      primarySwatch: swatch,
    );

    return Theme(
      data: pageTheme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text('Colors + primarySwatch', style: TextStyle(fontSize: 20)),
          const SizedBox(height: 8),
          const Text(
            'MaterialColor shades, ColorScheme.fromSwatch and the '
            'primarySwatch-derived theme colors.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildControlButton(
                label: 'swatch: $name',
                onTap: () => setState(
                  () => _swatchIndex = (_swatchIndex + 1) % _swatches.length,
                ),
                width: 150,
                background: const Color(0xFFE9F0FF),
              ),
              const SizedBox(width: 8),
              _buildControlButton(
                label: _useMaterial3 ? 'M3' : 'M2',
                onTap: () => setState(() => _useMaterial3 = !_useMaterial3),
                width: 80,
                background: const Color(0xFFEAF6F7),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                children: <Widget>[
                  _buildShadeStrip(name, swatch),
                  const SizedBox(height: 14),
                  _buildThemeProbe(pageTheme),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildShadeStrip(String name, MaterialColor swatch) {
    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            'Colors.$name shades',
            style: const TextStyle(fontSize: 14, color: Colors.black),
          ),
          const SizedBox(height: 8),
          Row(
            children: <Widget>[
              for (final int shade in _shades)
                Expanded(
                  child: Container(
                    height: 56,
                    color: swatch[shade],
                    alignment: Alignment.center,
                    child: Text(
                      '$shade',
                      style: TextStyle(
                        fontSize: 11,
                        color:
                            ThemeData.estimateBrightnessForColor(
                                  swatch[shade]!,
                                ) ==
                                Brightness.dark
                            ? Colors.white
                            : Colors.black,
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildThemeProbe(ThemeData theme) {
    return Container(
      color: const Color(0xFFF7F9FC),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'Theme colors',
            style: TextStyle(fontSize: 14, color: Colors.black),
          ),
          const SizedBox(height: 8),
          _buildSwatchRow('colorScheme.primary', theme.colorScheme.primary),
          _buildSwatchRow('colorScheme.secondary', theme.colorScheme.secondary),
          _buildSwatchRow('primaryColor', theme.primaryColor),
          _buildSwatchRow('primaryColorLight', theme.primaryColorLight),
          _buildSwatchRow('primaryColorDark', theme.primaryColorDark),
          _buildSwatchRow('canvasColor', theme.canvasColor),
          _buildSwatchRow('cardColor', theme.cardColor),
        ],
      ),
    );
  }

  Widget _buildSwatchRow(String label, Color color) {
    return Row(
      children: <Widget>[
        Container(width: 28, height: 20, color: color),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontSize: 12, color: Colors.black),
          ),
        ),
        Text(
          '#${color.value.toRadixString(16).toUpperCase().padLeft(8, '0')}',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
      ],
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
}
