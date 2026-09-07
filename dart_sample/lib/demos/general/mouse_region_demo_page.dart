import 'package:flutter/gestures.dart';
import 'package:flutter/widgets.dart';

class MouseRegionDemoPage extends StatefulWidget {
  const MouseRegionDemoPage({super.key});

  @override
  State<MouseRegionDemoPage> createState() => _MouseRegionDemoPageState();
}

class _MouseRegionDemoPageState extends State<MouseRegionDemoPage> {
  final List<String> _log = <String>[];
  bool _opaque = true;
  Offset? _hoverPosition;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'MouseRegion + MouseTracker',
          style: TextStyle(fontSize: 20, color: Color(0xFF000000)),
        ),
        const Text(
          "Enter and exit come from the mouse tracker's per-device annotation diff, so "
          'they also fire when a region appears, moves or disappears under a still '
          'pointer. The cursor is the first non-deferring region in hit-test order.',
          style: TextStyle(fontSize: 14, color: Color(0xFF696969)),
        ),
        _buildCursorRow(),
        _buildStackedRegions(),
        _buildToggle(),
        Text(
          _hoverPosition == null
              ? 'hover — outside'
              : 'hover — ${_format(_hoverPosition!.dx)}, ${_format(_hoverPosition!.dy)}',
          style: const TextStyle(fontSize: 13, color: Color(0xFF696969)),
        ),
        Text(
          _log.isEmpty ? 'no enter/exit yet' : _log.join('  '),
          style: const TextStyle(fontSize: 13, color: Color(0xFF696969)),
        ),
      ],
    );
  }

  Widget _buildCursorRow() {
    return Row(
      spacing: 12,
      children: <Widget>[
        _buildCursorSwatch('text', SystemMouseCursors.text, const Color(0xFF00796B)),
        _buildCursorSwatch('click', SystemMouseCursors.click, const Color(0xFF1A73E8)),
        _buildCursorSwatch('grab', SystemMouseCursors.grab, const Color(0xFF8E24AA)),
        _buildCursorSwatch('forbidden', SystemMouseCursors.forbidden, const Color(0xFFD93025)),
      ],
    );
  }

  Widget _buildCursorSwatch(String label, MouseCursor cursor, Color color) {
    return Expanded(
      child: MouseRegion(
        cursor: cursor,
        onEnter: (PointerEnterEvent event) => _record('enter $label'),
        onExit: (PointerExitEvent event) => _record('exit $label'),
        child: Container(
          height: 64,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: Color(0xFFFFFFFF)),
          ),
        ),
      ),
    );
  }

  Widget _buildStackedRegions() {
    return Container(
      height: 180,
      decoration: BoxDecoration(
        color: const Color(0xFFF1F3F4),
        border: Border.all(color: const Color(0xFF9AA0A6)),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Stack(
        children: <Widget>[
          // The outer region always sees the pointer; the inner one sits on top of it and
          // only lets it through when `opaque` is false.
          Positioned.fill(
            child: MouseRegion(
              onEnter: (PointerEnterEvent event) => _record('enter outer'),
              onExit: (PointerExitEvent event) => _record('exit outer'),
              onHover: (PointerHoverEvent event) =>
                  setState(() => _hoverPosition = event.localPosition),
              child: const SizedBox(),
            ),
          ),
          Positioned(
            left: 40,
            top: 40,
            width: 160,
            height: 100,
            child: MouseRegion(
              opaque: _opaque,
              cursor: SystemMouseCursors.precise,
              onEnter: (PointerEnterEvent event) => _record('enter inner'),
              onExit: (PointerExitEvent event) => _record('exit inner'),
              child: Container(
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: const Color(0xFFFFCC80),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  _opaque ? 'opaque: true' : 'opaque: false',
                  style: const TextStyle(fontSize: 13, color: Color(0xFF000000)),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildToggle() {
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () => setState(() => _opaque = !_opaque),
        behavior: HitTestBehavior.opaque,
        child: Container(
          height: 40,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: const Color(0xFF3C4043),
            borderRadius: BorderRadius.circular(8),
          ),
          child: const Text(
            "toggle the inner region's opaque flag",
            style: TextStyle(fontSize: 13, color: Color(0xFFFFFFFF)),
          ),
        ),
      ),
    );
  }

  void _record(String entry) {
    setState(() {
      _log.add(entry);
      if (_log.length > 6) {
        _log.removeAt(0);
      }
    });
  }

  static String _format(double value) => value.toStringAsFixed(1);
}
