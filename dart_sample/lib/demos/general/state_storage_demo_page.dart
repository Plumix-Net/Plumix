import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';

class StateStorageDemoPage extends StatefulWidget {
  const StateStorageDemoPage({super.key});

  @override
  State<StateStorageDemoPage> createState() => _StateStorageDemoPageState();
}

class _StateStorageDemoPageState extends State<StateStorageDemoPage> {
  static const String _sharedCounterKey = 'shared-counter';
  final PageStorageBucket _bucket = PageStorageBucket();
  bool _showScrollable = true;

  @override
  Widget build(BuildContext context) {
    return PageStorage(
      bucket: _bucket,
      child: SharedAppData(child: Builder(builder: _buildContent)),
    );
  }

  Widget _buildContent(BuildContext context) {
    final int sharedCounter = SharedAppData.getValue<String, int>(
      context,
      _sharedCounterKey,
      () => 0,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'PageStorage + SharedAppData',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Jump the list, unmount it, then restore it. The same PageStorageKey '
          'restores the offset; the shared counter rebuilds only its keyed dependent. '
          'The list inherits its controller through PrimaryScrollController and '
          'its desktop chrome through ScrollConfiguration. Drag past an edge to '
          'compare Flutter glow and stretch indicators.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            SizedBox(
              width: 160,
              child: OutlinedButton(
                onPressed: () => SharedAppData.setValue<String, int>(
                  context,
                  _sharedCounterKey,
                  sharedCounter + 1,
                ),
                child: Text('Shared value: $sharedCounter'),
              ),
            ),
            SizedBox(
              width: 160,
              child: OutlinedButton(
                onPressed: () => setState(() {
                  _showScrollable = !_showScrollable;
                }),
                child: Text(_showScrollable ? 'Unmount list' : 'Restore list'),
              ),
            ),
          ],
        ),
        Expanded(
          child: _showScrollable
              ? const _RestorableStorageList()
              : Container(
                  color: const Color(0xFFE8EEF6),
                  alignment: Alignment.center,
                  child: const Text(
                    'List is unmounted. Restore it to verify the saved offset.',
                    style: TextStyle(color: Color(0xFF31506F)),
                  ),
                ),
        ),
      ],
    );
  }
}

class _RestorableStorageList extends StatefulWidget {
  const _RestorableStorageList();

  @override
  State<_RestorableStorageList> createState() => _RestorableStorageListState();
}

class _RestorableStorageListState extends State<_RestorableStorageList> {
  final ScrollController _controller = ScrollController();
  bool _showScrollbar = true;
  bool _useStretch = true;

  @override
  Widget build(BuildContext context) {
    final Widget scrollView = SingleChildScrollView(
      key: const PageStorageKey<String>('state-storage-list'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 6,
        children: List<Widget>.generate(18, _buildRow),
      ),
    );
    final Widget indicatedScrollView = _useStretch
        ? StretchingOverscrollIndicator(
            axisDirection: AxisDirection.down,
            child: scrollView,
          )
        : GlowingOverscrollIndicator(
            axisDirection: AxisDirection.down,
            color: const Color(0xFF625B71),
            child: scrollView,
          );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            SizedBox(
              width: 180,
              child: FilledButton(
                onPressed: () => _controller.jumpTo(240),
                child: const Text('Jump to offset 240'),
              ),
            ),
            SizedBox(
              width: 180,
              child: FilledButton(
                onPressed: () => setState(() {
                  _showScrollbar = !_showScrollbar;
                }),
                child: Text(
                  _showScrollbar
                      ? 'Hide config scrollbar'
                      : 'Show config scrollbar',
                ),
              ),
            ),
            SizedBox(
              width: 180,
              child: FilledButton(
                onPressed: () => setState(() {
                  _useStretch = !_useStretch;
                }),
                child: Text(_useStretch ? 'Effect: stretch' : 'Effect: glow'),
              ),
            ),
          ],
        ),
        Expanded(
          child: ScrollConfiguration(
            behavior: const _DesktopDemoScrollBehavior().copyWith(
              scrollbars: _showScrollbar,
              dragDevices: <PointerDeviceKind>{
                PointerDeviceKind.touch,
                PointerDeviceKind.mouse,
                PointerDeviceKind.trackpad,
              },
            ),
            child: PrimaryScrollController(
              automaticallyInheritForPlatforms: const <TargetPlatform>{
                TargetPlatform.windows,
              },
              controller: _controller,
              child: indicatedScrollView,
            ),
          ),
        ),
      ],
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  static Widget _buildRow(int index) {
    return Container(
      height: 44,
      color: index.isEven ? const Color(0xFFF4F7FA) : const Color(0xFFE6EDF5),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      alignment: Alignment.centerLeft,
      child: Text('Stored row ${index + 1}'),
    );
  }
}

class _DesktopDemoScrollBehavior extends ScrollBehavior {
  const _DesktopDemoScrollBehavior();

  @override
  TargetPlatform getPlatform(BuildContext context) => TargetPlatform.windows;
}
