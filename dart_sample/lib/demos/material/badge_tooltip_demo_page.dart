import 'package:material_ui/material_ui.dart';

class BadgeTooltipDemoPage extends StatefulWidget {
  const BadgeTooltipDemoPage({super.key});

  @override
  State<BadgeTooltipDemoPage> createState() => _BadgeTooltipDemoPageState();
}

class _BadgeTooltipDemoPageState extends State<BadgeTooltipDemoPage> {
  int _count = 7;
  bool _isLabelVisible = true;
  bool _useThemeOverrides = false;
  bool _tooltipsVisible = true;
  bool _useRtl = false;

  @override
  Widget build(BuildContext context) {
    Widget content = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text('Badge + Tooltip', style: TextStyle(fontSize: 20)),
        const SizedBox(height: 14),
        const Text(
          'Badge geometry plus plain/rich tooltips with hover, directional theming, and custom positioning.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            _controlButton('Count +1', () => setState(() => _count++)),
            const SizedBox(width: 8),
            _controlButton(
              _isLabelVisible ? 'Label on' : 'Label off',
              () => setState(() => _isLabelVisible = !_isLabelVisible),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _useThemeOverrides ? 'Theme on' : 'Theme off',
              () => setState(() => _useThemeOverrides = !_useThemeOverrides),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _tooltipsVisible ? 'Tooltips on' : 'Tooltips off',
              () => setState(() => _tooltipsVisible = !_tooltipsVisible),
            ),
          ],
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            _controlButton(
              _useRtl ? 'Direction RTL' : 'Direction LTR',
              () => setState(() => _useRtl = !_useRtl),
            ),
          ],
        ),
        const SizedBox(height: 14),
        Container(
          color: const Color(0xFFF7F2FA),
          padding: const EdgeInsets.all(20),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: <Widget>[
              _probe(
                'Count',
                Badge.count(
                  count: _count,
                  maxCount: 99,
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.info_outline, size: 32),
                ),
              ),
              _probe(
                'Small',
                Badge(
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.star_outline, size: 32),
                ),
              ),
              _probe(
                'Scheme tokens',
                Theme(
                  data: Theme.of(context).copyWith(
                    colorScheme: Theme.of(context).colorScheme.copyWith(
                      error: const Color(0xFF00639B),
                      onError: Colors.white,
                    ),
                  ),
                  child: Badge(
                    label: const Text('M3'),
                    isLabelVisible: _isLabelVisible,
                    child: const Icon(Icons.info_outline, size: 32),
                  ),
                ),
              ),
              _probe(
                'Widget override',
                Badge(
                  backgroundColor: const Color(0xFF00695C),
                  textColor: Colors.white,
                  largeSize: 20,
                  offset: const Offset(7, -7),
                  label: const Text('NEW'),
                  isLabelVisible: _isLabelVisible,
                  child: const Icon(Icons.check, size: 32),
                ),
              ),
              _probe(
                _useRtl ? 'Top end RTL' : 'Top end LTR',
                Directionality(
                  textDirection: _useRtl
                      ? TextDirection.rtl
                      : TextDirection.ltr,
                  child: Badge(
                    alignment: AlignmentDirectional.topEnd,
                    label: const Text('END'),
                    isLabelVisible: _isLabelVisible,
                    child: const Icon(Icons.info_outline, size: 32),
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        const Text(
          'Hover or long-press these controls:',
          style: TextStyle(fontSize: 14),
        ),
        const SizedBox(height: 14),
        TooltipVisibility(
          visible: _tooltipsVisible,
          child: Directionality(
            textDirection: _useRtl ? TextDirection.rtl : TextDirection.ltr,
            child: Wrap(
              spacing: 12,
              runSpacing: 12,
              children: <Widget>[
                Tooltip(
                  message: 'Default tooltip',
                  child: OutlinedButton(
                    onPressed: () {},
                    child: const Text('Default'),
                  ),
                ),
                Tooltip(
                  richMessage: const TextSpan(
                    children: <InlineSpan>[
                      TextSpan(
                        text: 'Rich ',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                      TextSpan(text: 'interactive tooltip'),
                    ],
                  ),
                  decoration: const ShapeDecoration(
                    shape: StadiumBorder(),
                    color: Color(0xFF00695C),
                  ),
                  child: OutlinedButton(
                    onPressed: () {},
                    child: const Text('Rich + shape'),
                  ),
                ),
                Tooltip(
                  message: 'Widget override tooltip',
                  preferBelow: false,
                  verticalOffset: 28,
                  decoration: BoxDecoration(
                    color: const Color(0xFF4527A0),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  textStyle: const TextStyle(color: Colors.white, fontSize: 13),
                  waitDuration: const Duration(milliseconds: 250),
                  child: OutlinedButton(
                    onPressed: () {},
                    child: const Text('Above + delay'),
                  ),
                ),
                Tooltip(
                  message: 'Custom right tooltip',
                  positionDelegate: _positionTooltipRight,
                  child: OutlinedButton(
                    onPressed: _noop,
                    child: const Text('Custom right'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );

    if (!_useThemeOverrides) {
      return content;
    }

    content = BadgeTheme(
      data: const BadgeThemeData(
        backgroundColor: Color(0xFFB3261E),
        textColor: Colors.white,
        largeSize: 18,
        smallSize: 8,
        padding: EdgeInsets.symmetric(horizontal: 5),
        alignment: AlignmentDirectional.bottomEnd,
      ),
      child: TooltipTheme(
        data: TooltipThemeData(
          decoration: BoxDecoration(
            color: const Color(0xFF00695C),
            borderRadius: BorderRadius.circular(6),
          ),
          textStyle: const TextStyle(color: Colors.white, fontSize: 12),
          padding: const EdgeInsetsDirectional.fromSTEB(12, 4, 4, 4),
          waitDuration: const Duration(milliseconds: 150),
          exitDuration: const Duration(milliseconds: 200),
        ),
        child: content,
      ),
    );
    return content;
  }

  Widget _probe(String label, Widget child) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        child,
        const SizedBox(height: 8),
        Text(label, style: const TextStyle(fontSize: 12)),
      ],
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

  static Offset _positionTooltipRight(TooltipPositionContext position) {
    return Offset(
      position.target.dx + position.targetSize.width / 2 + 8,
      position.target.dy - position.tooltipSize.height / 2,
    );
  }

  static void _noop() {}
}
