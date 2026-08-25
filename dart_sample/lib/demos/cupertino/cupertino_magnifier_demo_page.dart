import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoMagnifierDemoPage extends StatefulWidget {
  const CupertinoMagnifierDemoPage({super.key});

  @override
  State<CupertinoMagnifierDemoPage> createState() =>
      _CupertinoMagnifierDemoPageState();
}

class _CupertinoMagnifierDemoPageState
    extends State<CupertinoMagnifierDemoPage> {
  static const List<double> _scales = <double>[1.0, 1.5, 2.0];

  final MagnifierController _controller = MagnifierController();
  final ValueNotifier<MagnifierInfo> _magnifierInfo =
      ValueNotifier<MagnifierInfo>(
        MagnifierInfo(
          globalGesturePosition: Offset.zero,
          caretRect: Rect.zero,
          fieldBounds: Rect.zero,
          currentLineBoundaries: Rect.zero,
        ),
      );

  BuildContext? _panelContext;
  double _magnificationScale = 1.5;
  Offset _lastGesturePosition = Offset.zero;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino magnifier',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Drag over the stripes: the text magnifier follows the gesture, '
          'stays inside the 10pt screen padding and resists downward drag.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: _scales.map(_scaleButton).toList(),
        ),
        Expanded(
          child: Builder(
            builder: (BuildContext panelContext) {
              _panelContext = panelContext;
              return _buildPanel();
            },
          ),
        ),
      ],
    );
  }

  @override
  void dispose() {
    _controller.hide();
    _magnifierInfo.dispose();
    super.dispose();
  }

  Widget _buildPanel() {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onPanStart: (DragStartDetails details) =>
          _showMagnifier(details.globalPosition),
      onPanUpdate: (DragUpdateDetails details) =>
          _updateMagnifierInfo(details.globalPosition),
      onPanEnd: (DragEndDetails details) => _hideMagnifier(),
      onPanCancel: _hideMagnifier,
      child: ColoredBox(
        color: const Color(0xFFF2F2F7),
        child: Stack(
          clipBehavior: Clip.none,
          children: <Widget>[
            Positioned(
              left: 16,
              top: 24,
              right: 16,
              height: 44,
              child: _stripeRow(),
            ),
            const Positioned(
              left: 16,
              top: 96,
              right: 16,
              child: Center(
                child: Text(
                  'MAGNIFY 0123456789',
                  style: TextStyle(fontSize: 24, color: Color(0xFF1C1C1E)),
                ),
              ),
            ),
            Positioned(
              left: 16,
              top: 148,
              child: CupertinoMagnifier(magnificationScale: _magnificationScale),
            ),
            Positioned(
              left: 16,
              bottom: 16,
              child: Text(
                'gesture=(${_lastGesturePosition.dx.toStringAsFixed(0)}, '
                '${_lastGesturePosition.dy.toStringAsFixed(0)}), '
                'scale=${_magnificationScale.toStringAsFixed(1)}',
                style: const TextStyle(
                  fontSize: 12,
                  color: Color(0xFF007AFF),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _scaleButton(double scale) {
    final bool selected = (_magnificationScale - scale).abs() < 0.001;
    return CupertinoButton(
      color: selected
          ? CupertinoColors.activeBlue
          : CupertinoColors.systemGrey5,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      onPressed: () => setState(() => _magnificationScale = scale),
      child: Text(
        'x${scale.toStringAsFixed(1)}',
        style: TextStyle(
          fontSize: 13,
          color: selected ? Colors.white : const Color(0xFF1C1C1E),
        ),
      ),
    );
  }

  static Widget _stripeRow() {
    const List<Color> colors = <Color>[
      Color(0xFF007AFF),
      Color(0xFFFFCC00),
      Color(0xFF34C759),
      Color(0xFFFF3B30),
      Color(0xFF5856D6),
    ];
    return Row(
      children: colors
          .map((Color color) => Expanded(child: ColoredBox(color: color)))
          .toList(),
    );
  }

  void _showMagnifier(Offset globalPosition) {
    _updateMagnifierInfo(globalPosition);
    final BuildContext? panelContext = _panelContext;
    if (_controller.overlayEntry != null || panelContext == null) {
      return;
    }

    _controller.show(
      context: panelContext,
      builder: (BuildContext context) => CupertinoTextMagnifier(
        controller: _controller,
        magnifierInfo: _magnifierInfo,
      ),
    );
  }

  void _hideMagnifier() {
    _controller.hide();
  }

  void _updateMagnifierInfo(Offset globalPosition) {
    final Rect lineBounds = _currentLineBounds(globalPosition);
    setState(() => _lastGesturePosition = globalPosition);
    _magnifierInfo.value = MagnifierInfo(
      globalGesturePosition: globalPosition,
      caretRect: lineBounds,
      fieldBounds: _panelBounds() ?? lineBounds,
      currentLineBoundaries: lineBounds,
    );
  }

  /// The stripe band the magnifier treats as the "line" the lens stays level with.
  Rect _currentLineBounds(Offset globalPosition) {
    final Rect? bounds = _panelBounds();
    if (bounds == null) {
      return Rect.fromLTWH(globalPosition.dx, globalPosition.dy, 1, 1);
    }

    return Rect.fromLTWH(
      bounds.left + 16,
      bounds.top + 24,
      (bounds.width - 32).clamp(0, double.infinity),
      44,
    );
  }

  Rect? _panelBounds() {
    final RenderObject? box = _panelContext?.findRenderObject();
    if (box is! RenderBox || !box.hasSize) {
      return null;
    }

    return box.localToGlobal(Offset.zero) & box.size;
  }
}
