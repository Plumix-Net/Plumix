import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoPageScaffoldDemoPage extends StatefulWidget {
  const CupertinoPageScaffoldDemoPage({super.key});

  @override
  State<CupertinoPageScaffoldDemoPage> createState() =>
      _CupertinoPageScaffoldDemoPageState();
}

class _CupertinoPageScaffoldDemoPageState
    extends State<CupertinoPageScaffoldDemoPage> {
  static const CupertinoDynamicColor _pageBackground =
      CupertinoDynamicColor.withBrightness(
        color: Color(0xFFF2F2F7),
        darkColor: Color(0xFF1C1C1E),
      );

  bool _opaqueBar = false;
  bool _showKeyboardInset = false;
  bool _resizeToAvoidBottomInset = true;

  @override
  Widget build(BuildContext context) {
    final MediaQueryData mediaQuery = MediaQuery.of(context);
    final double bottomInset = _showKeyboardInset ? 96.0 : 0.0;
    return MediaQuery(
      data: mediaQuery.copyWith(
        viewInsets: mediaQuery.viewInsets.copyWith(bottom: bottomInset),
      ),
      child: CupertinoTheme(
        data: const CupertinoThemeData(),
        child: CupertinoPageScaffold(
          navigationBar: _DemoNavigationBar(opaque: _opaqueBar),
          backgroundColor: _pageBackground,
          resizeToAvoidBottomInset: _resizeToAvoidBottomInset,
          child: Builder(builder: _buildContent),
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    final MediaQueryData mediaQuery = MediaQuery.of(context);
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    final Color secondaryLabel = CupertinoDynamicColor.resolve(
      CupertinoColors.secondaryLabel,
      context,
    );
    final Color cardColor = CupertinoDynamicColor.resolve(
      CupertinoColors.secondarySystemBackground,
      context,
    );

    return SingleChildScrollView(
      child: Padding(
        padding: EdgeInsets.fromLTRB(16, mediaQuery.padding.top + 16, 16, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 10,
          children: <Widget>[
            Text(
              'CupertinoPageScaffold',
              style: TextStyle(fontSize: 20, color: label),
            ),
            Text(
              'The probe bar switches between translucent overlap guidance '
              'and opaque content offset.',
              style: TextStyle(fontSize: 14, color: secondaryLabel),
            ),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: cardColor,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                spacing: 6,
                children: <Widget>[
                  Text(
                    'bar mode: ${_opaqueBar ? 'opaque' : 'translucent'}',
                    style: TextStyle(fontSize: 13, color: label),
                  ),
                  Text(
                    'child MediaQuery.padding.top: '
                    '${mediaQuery.padding.top.toStringAsFixed(0)}',
                    style: TextStyle(fontSize: 12, color: secondaryLabel),
                  ),
                  Text(
                    'child MediaQuery.viewInsets.bottom: '
                    '${mediaQuery.viewInsets.bottom.toStringAsFixed(0)}',
                    style: TextStyle(fontSize: 12, color: secondaryLabel),
                  ),
                ],
              ),
            ),
            _buildAction(
              _opaqueBar ? 'Use translucent bar' : 'Use opaque bar',
              () => setState(() => _opaqueBar = !_opaqueBar),
              const Color(0xFFE9F0FF),
            ),
            _buildAction(
              _showKeyboardInset
                  ? 'Hide simulated keyboard'
                  : 'Show simulated keyboard',
              () => setState(() => _showKeyboardInset = !_showKeyboardInset),
              const Color(0xFFEAE4FF),
            ),
            _buildAction(
              _resizeToAvoidBottomInset ? 'Resize: on' : 'Resize: off',
              () => setState(
                () => _resizeToAvoidBottomInset = !_resizeToAvoidBottomInset,
              ),
              const Color(0xFFE8F4E8),
            ),
            const SizedBox(
              height: 96,
              child: ColoredBox(
                color: Color(0xFFFFF3E0),
                child: Center(
                  child: Text(
                    'Bottom inset probe',
                    style: TextStyle(fontSize: 13, color: Colors.black),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  static Widget _buildAction(
    String label,
    VoidCallback onTap,
    Color background,
  ) {
    return TextButton(
      onPressed: onTap,
      style: TextButton.styleFrom(
        backgroundColor: background,
        foregroundColor: Colors.black,
        minimumSize: const Size(0, 36),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }
}

class _DemoNavigationBar extends StatelessWidget
    implements ObstructingPreferredSizeWidget {
  const _DemoNavigationBar({required this.opaque});

  static const CupertinoDynamicColor _translucentBackground =
      CupertinoDynamicColor.withBrightness(
        color: Color(0xCCFFFFFF),
        darkColor: Color(0xCC1C1C1E),
      );

  final bool opaque;

  @override
  Size get preferredSize => const Size.fromHeight(52);

  @override
  bool shouldFullyObstruct(BuildContext context) => opaque;

  @override
  Widget build(BuildContext context) {
    final Color background = opaque
        ? CupertinoDynamicColor.resolve(
            CupertinoColors.systemBackground,
            context,
          )
        : CupertinoDynamicColor.resolve(_translucentBackground, context);
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    return Container(
      height: 52,
      color: background,
      alignment: Alignment.center,
      child: Text(
        opaque ? 'Opaque probe bar' : 'Translucent probe bar',
        style: TextStyle(color: label),
      ),
    );
  }
}
