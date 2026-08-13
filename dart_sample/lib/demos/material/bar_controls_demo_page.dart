import 'package:material_ui/material_ui.dart';

class BarControlsDemoPage extends StatefulWidget {
  const BarControlsDemoPage({super.key});

  @override
  State<BarControlsDemoPage> createState() => _BarControlsDemoPageState();
}

class _BarControlsDemoPageState extends State<BarControlsDemoPage> {
  bool _useMaterial3 = true;
  bool _useThemeOverrides = false;
  bool _showNotch = true;
  bool _narrowButtonBar = false;
  bool _overflowUp = false;
  bool _useRtl = false;
  int _actionCount = 0;

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData theme = baseTheme.copyWith(
      // ignore: deprecated_member_use
      useMaterial3: _useMaterial3,
      bottomAppBarTheme: _useThemeOverrides
          ? const BottomAppBarThemeData(
              color: Color(0xFFE8F5E9),
              elevation: 6,
              height: 72,
              shadowColor: Color(0x66000000),
              padding: EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            )
          : const BottomAppBarThemeData(),
      // ignore: deprecated_member_use
      buttonBarTheme: _useThemeOverrides
          // ignore: deprecated_member_use
          ? ButtonBarThemeData(
              alignment: MainAxisAlignment.center,
              buttonMinWidth: 72,
              buttonHeight: 40,
              buttonPadding: const EdgeInsetsDirectional.fromSTEB(14, 2, 6, 2),
              layoutBehavior: ButtonBarLayoutBehavior.constrained,
              overflowDirection: _overflowUp
                  ? VerticalDirection.up
                  : VerticalDirection.down,
            )
          // ignore: deprecated_member_use
          : ButtonBarThemeData(
              overflowDirection: _overflowUp
                  ? VerticalDirection.up
                  : VerticalDirection.down,
            ),
    );

    return Theme(
      data: theme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const Text(
            'BottomAppBar + ButtonBar',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const SizedBox(height: 8),
          const Text(
            'M2/M3 bottom-surface defaults, FAB notch geometry, SafeArea, theme precedence, and legacy ButtonBar row-to-column overflow.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          const SizedBox(height: 10),
          Row(
            children: <Widget>[
              _buildToggle(_useMaterial3 ? 'M3' : 'M2', () {
                _useMaterial3 = !_useMaterial3;
              }, 68),
              const SizedBox(width: 8),
              _buildToggle(
                _useThemeOverrides ? 'theme=on' : 'theme=off',
                () => _useThemeOverrides = !_useThemeOverrides,
                104,
              ),
              const SizedBox(width: 8),
              _buildToggle(
                _showNotch ? 'notch=on' : 'notch=off',
                () => _showNotch = !_showNotch,
                104,
              ),
              const SizedBox(width: 8),
              _buildToggle(
                _narrowButtonBar ? 'bar=narrow' : 'bar=wide',
                () => _narrowButtonBar = !_narrowButtonBar,
                106,
              ),
              const SizedBox(width: 8),
              _buildToggle(
                _overflowUp ? 'overflow=up' : 'overflow=down',
                () => _overflowUp = !_overflowUp,
                118,
              ),
              const SizedBox(width: 8),
              _buildToggle(_useRtl ? 'RTL' : 'LTR', () {
                _useRtl = !_useRtl;
              }, 68),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'actionCount=$_actionCount',
            style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
          ),
          const SizedBox(height: 10),
          Align(
            alignment: Alignment.centerLeft,
            child: SizedBox(
              width: _narrowButtonBar ? 190 : 520,
              child: ColoredBox(
                color: const Color(0xFFF7F9FC),
                // ignore: deprecated_member_use
                child: Directionality(
                  textDirection: _useRtl
                      ? TextDirection.rtl
                      : TextDirection.ltr,
                  child: ButtonBar(
                    overflowButtonSpacing: 8,
                    children: <Widget>[
                      _buildAction('CANCEL'),
                      _buildAction('LATER'),
                      _buildAction('CONFIRM'),
                    ],
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: Scaffold(
              backgroundColor: const Color(0xFFF4F6FA),
              body: const Center(
                child: Text(
                  'The FAB and notch share Scaffold geometry',
                  style: TextStyle(fontSize: 14, color: Colors.grey),
                ),
              ),
              floatingActionButton: FloatingActionButton(
                onPressed: () => setState(() => _actionCount += 1),
                child: const Icon(Icons.add),
              ),
              floatingActionButtonLocation:
                  FloatingActionButtonLocation.centerDocked,
              bottomNavigationBar: BottomAppBar(
                shape: _showNotch ? const CircularNotchedRectangle() : null,
                notchMargin: 4,
                child: Row(
                  children: <Widget>[
                    IconButton(
                      onPressed: () => setState(() => _actionCount += 1),
                      icon: const Icon(Icons.menu),
                    ),
                    const Spacer(),
                    IconButton(
                      onPressed: () => setState(() => _actionCount += 1),
                      icon: const Icon(Icons.info_outline),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAction(String label) {
    return TextButton(
      onPressed: () => setState(() => _actionCount += 1),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }

  Widget _buildToggle(String label, VoidCallback update, double width) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: () => setState(update),
        style: TextButton.styleFrom(
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          backgroundColor: const Color(0xFFE9F0FF),
          foregroundColor: Colors.black,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 11)),
      ),
    );
  }
}
