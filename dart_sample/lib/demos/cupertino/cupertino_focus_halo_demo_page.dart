import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoFocusHaloDemoPage extends StatefulWidget {
  const CupertinoFocusHaloDemoPage({super.key});

  @override
  State<CupertinoFocusHaloDemoPage> createState() =>
      _CupertinoFocusHaloDemoPageState();
}

class _CupertinoFocusHaloDemoPageState
    extends State<CupertinoFocusHaloDemoPage> {
  final FocusNode _rectFocus = FocusNode();
  final FocusNode _roundedRectFocus = FocusNode();
  final FocusNode _superellipseFocus = FocusNode();

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino focus halo',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Press Tab or click a tile to move focus through the three halo '
          'shapes.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 16,
          runSpacing: 16,
          children: <Widget>[
            CupertinoFocusHalo.withRect(
              child: _buildFocusableTile('Rectangle', _rectFocus),
            ),
            CupertinoFocusHalo.withRRect(
              borderRadius: BorderRadius.circular(12),
              child: _buildFocusableTile(
                'Rounded rectangle',
                _roundedRectFocus,
              ),
            ),
            CupertinoFocusHalo.withRoundedSuperellipse(
              borderRadius: BorderRadius.circular(12),
              child: _buildFocusableTile(
                'Rounded superellipse',
                _superellipseFocus,
              ),
            ),
          ],
        ),
      ],
    );
  }

  @override
  void dispose() {
    _rectFocus.dispose();
    _roundedRectFocus.dispose();
    _superellipseFocus.dispose();
    super.dispose();
  }

  static Widget _buildFocusableTile(String label, FocusNode focusNode) {
    return Focus(
      focusNode: focusNode,
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTap: focusNode.requestFocus,
        child: Container(
          width: 176,
          height: 72,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: const Color(0xFFF2F2F7),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 14, color: Colors.black),
          ),
        ),
      ),
    );
  }
}
