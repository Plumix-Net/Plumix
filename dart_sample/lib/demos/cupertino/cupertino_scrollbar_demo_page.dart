import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoScrollbarDemoPage extends StatefulWidget {
  const CupertinoScrollbarDemoPage({super.key});

  @override
  State<CupertinoScrollbarDemoPage> createState() =>
      _CupertinoScrollbarDemoPageState();
}

class _CupertinoScrollbarDemoPageState
    extends State<CupertinoScrollbarDemoPage> {
  late final ScrollController _fadingController;
  late final ScrollController _alwaysVisibleController;
  late final ScrollController _leftController;
  bool _dark = false;
  bool _rightToLeft = false;

  @override
  void initState() {
    super.initState();
    _fadingController = ScrollController();
    _alwaysVisibleController = ScrollController();
    _leftController = ScrollController();
  }

  @override
  void dispose() {
    _fadingController.dispose();
    _alwaysVisibleController.dispose();
    _leftController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CupertinoTheme(
      data: CupertinoThemeData(
        brightness: _dark ? Brightness.dark : Brightness.light,
      ),
      child: Directionality(
        textDirection: _rightToLeft ? TextDirection.rtl : TextDirection.ltr,
        child: Container(
          color: _dark ? const Color(0xFF1C1C1E) : CupertinoColors.white,
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 12,
            children: <Widget>[
              Text(
                'CupertinoScrollbar',
                style: TextStyle(fontSize: 20, color: _titleColor),
              ),
              Text(
                'Press and hold the thumb to grow it from 3 to 8 logical pixels, then '
                'drag. Tapping the track never pages on iOS.',
                style: TextStyle(fontSize: 14, color: _subtitleColor),
              ),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: <Widget>[
                  _buildControl(_dark ? 'Dark' : 'Light', () {
                    _dark = !_dark;
                  }),
                  _buildControl(_rightToLeft ? 'RTL' : 'LTR', () {
                    _rightToLeft = !_rightToLeft;
                  }),
                ],
              ),
              Expanded(
                child: Row(
                  spacing: 12,
                  children: <Widget>[
                    Expanded(
                      child: _buildPane(
                        'Fading',
                        'default thumbVisibility: fades in while scrolling',
                        CupertinoScrollbar(
                          controller: _fadingController,
                          child: _buildList(_fadingController),
                        ),
                      ),
                    ),
                    Expanded(
                      child: _buildPane(
                        'Always visible',
                        'thumbVisibility: true, thicker while dragging',
                        CupertinoScrollbar(
                          controller: _alwaysVisibleController,
                          thumbVisibility: true,
                          thickness: 6,
                          thicknessWhileDragging: 14,
                          radius: const Radius.circular(3),
                          radiusWhileDragging: const Radius.circular(7),
                          child: _buildList(_alwaysVisibleController),
                        ),
                      ),
                    ),
                    Expanded(
                      child: _buildPane(
                        'Left rail',
                        'scrollbarOrientation: Left, mainAxisMargin: 12',
                        CupertinoScrollbar(
                          controller: _leftController,
                          thumbVisibility: true,
                          scrollbarOrientation: ScrollbarOrientation.left,
                          mainAxisMargin: 12,
                          child: _buildList(_leftController),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Color get _titleColor => _dark ? CupertinoColors.white : CupertinoColors.black;

  Color get _subtitleColor =>
      _dark ? const Color(0x99FFFFFF) : const Color(0x8A000000);

  Widget _buildPane(String title, String subtitle, Widget scrollbar) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: _dark ? const Color(0xFF2C2C2E) : const Color(0xFFF1F4F9),
        borderRadius: BorderRadius.circular(10),
        border: Border.fromBorderSide(
          BorderSide(
            color: _dark ? const Color(0xFF3A3A3C) : const Color(0xFFD6DEEA),
          ),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 6,
        children: <Widget>[
          Text(title, style: TextStyle(fontSize: 13, color: _titleColor)),
          Text(subtitle, style: TextStyle(fontSize: 12, color: _subtitleColor)),
          Expanded(child: scrollbar),
        ],
      ),
    );
  }

  Widget _buildList(ScrollController controller) {
    return ListView.builder(
      controller: controller,
      itemCount: 40,
      itemExtent: 34,
      itemBuilder: (BuildContext context, int index) => Align(
        alignment: Alignment.centerLeft,
        child: Text(
          'row $index',
          style: TextStyle(fontSize: 13, color: _titleColor),
        ),
      ),
    );
  }

  Widget _buildControl(String label, VoidCallback onPressed) {
    return CupertinoButton(
      onPressed: () => setState(onPressed),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Text(
        label,
        style: const TextStyle(fontSize: 12, color: CupertinoColors.activeBlue),
      ),
    );
  }
}
