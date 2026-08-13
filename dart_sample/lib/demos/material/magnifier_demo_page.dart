import 'package:material_ui/material_ui.dart';

class MagnifierDemoPage extends StatefulWidget {
  const MagnifierDemoPage({super.key});

  @override
  State<MagnifierDemoPage> createState() => _MagnifierDemoPageState();
}

class _MagnifierDemoPageState extends State<MagnifierDemoPage> {
  double _focusX = 180;
  bool _showFilm = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text(
          'RawMagnifier + Material Magnifier',
          style: TextStyle(fontSize: 20),
        ),
        const SizedBox(height: 12),
        const Text(
          'Move both lenses across high-contrast text and stripes to compare source geometry and styling.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 12),
        Row(
          children: <Widget>[
            _controlButton(
              'Focus left',
              () => setState(() => _focusX = (_focusX - 36).clamp(90, 310)),
            ),
            const SizedBox(width: 8),
            _controlButton(
              'Focus right',
              () => setState(() => _focusX = (_focusX + 36).clamp(90, 310)),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _showFilm ? 'Film on' : 'Film off',
              () => setState(() => _showFilm = !_showFilm),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F2FA),
            child: Stack(
              clipBehavior: Clip.none,
              children: <Widget>[
                Positioned(
                  left: 24,
                  top: 28,
                  right: 24,
                  height: 48,
                  child: _stripeRow(),
                ),
                const Positioned(
                  left: 24,
                  top: 168,
                  right: 24,
                  child: Center(
                    child: Text(
                      'MAGNIFY 0123456789',
                      style: TextStyle(fontSize: 24, color: Color(0xFF1D192B)),
                    ),
                  ),
                ),
                Positioned(
                  left: _focusX - 50,
                  top: 82,
                  child: RawMagnifier(
                    size: const Size(100, 54),
                    magnificationScale: 1.8,
                    focalPointOffset: const Offset(0, 74),
                    decoration: MagnifierDecoration(
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                        side: const BorderSide(
                          color: Color(0xFF006A6A),
                          width: 2,
                        ),
                      ),
                      shadows: Magnifier().shadows,
                    ),
                    clipBehavior: Clip.hardEdge,
                    child: ColoredBox(
                      color: _showFilm
                          ? const Color.fromARGB(10, 0, 105, 105)
                          : Colors.transparent,
                    ),
                  ),
                ),
                Positioned(
                  left: _focusX - Magnifier.kDefaultMagnifierSize.width / 2,
                  top: 116,
                  child: Magnifier(
                    filmColor: _showFilm
                        ? const Color.fromARGB(8, 158, 158, 158)
                        : Colors.transparent,
                  ),
                ),
                Positioned(
                  left: 24,
                  bottom: 18,
                  child: Text(
                    'focusX=${_focusX.toStringAsFixed(0)}, raw scale=1.8, material scale=1.25',
                    style: const TextStyle(fontSize: 12, color: Color(0xFF6750A4)),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _stripeRow() {
    const List<Color> colors = <Color>[
      Color(0xFF6750A4),
      Color(0xFFFFD8E4),
      Color(0xFF006A6A),
      Color(0xFFFFDDB3),
      Color(0xFF386A20),
    ];
    return Row(
      children: colors
          .map((Color color) => Expanded(child: ColoredBox(color: color)))
          .toList(),
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
