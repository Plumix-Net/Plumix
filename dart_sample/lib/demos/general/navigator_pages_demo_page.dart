import 'package:flutter/material.dart';

/// C# parity source: src/Sample/Plumix.Sample/Demos/General/NavigatorPagesDemoPage.cs
class NavigatorPagesDemoPage extends StatefulWidget {
  const NavigatorPagesDemoPage({super.key});

  @override
  State<NavigatorPagesDemoPage> createState() => _NavigatorPagesDemoPageState();
}

class _NavigatorPagesDemoPageState extends State<NavigatorPagesDemoPage> {
  final List<Page<dynamic>> _pages = <Page<dynamic>>[
    _SampleDeclarativePage(label: 'Home', index: 0),
  ];
  int _nextIndex = 1;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        const Text(
          'Navigator.pages demo',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The page list owns the history: pushing appends a page, popping asks onDidRemovePage '
          'to drop it.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Text(
          'pages: ${_pages.map((Page<dynamic> page) => page.name).join(', ')}',
          style: const TextStyle(fontSize: 12),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildAction('Add page', _addPage, const Color(0xFFE8F5E9)),
            _buildAction('Remove top page', _removeTopPage, const Color(0xFFFFF3E0)),
          ],
        ),
        SizedBox(
          height: 220,
          child: ColoredBox(
            color: const Color(0xFFFAFAFA),
            child: Navigator(
              pages: List<Page<dynamic>>.of(_pages),
              onDidRemovePage: _handleDidRemovePage,
            ),
          ),
        ),
      ],
    );
  }

  void _addPage() {
    setState(() {
      _pages.add(_SampleDeclarativePage(label: 'Page $_nextIndex', index: _nextIndex));
      _nextIndex += 1;
    });
  }

  void _removeTopPage() {
    if (_pages.length <= 1) {
      return;
    }

    setState(() => _pages.removeLast());
  }

  void _handleDidRemovePage(Page<Object?> page) {
    setState(() => _pages.remove(page));
  }

  static Widget _buildAction(String label, VoidCallback onTap, Color background) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        color: background,
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }
}

/// One entry of the declarative page list; its key keeps the same route across list updates.
class _SampleDeclarativePage extends Page<void> {
  _SampleDeclarativePage({required String label, required this.index})
    : super(key: ValueKey<int>(index), name: label);

  final int index;

  @override
  Route<void> createRoute(BuildContext context) => _SampleDeclarativePageRoute(this, index);
}

class _SampleDeclarativePageRoute extends PageRoute<void> {
  _SampleDeclarativePageRoute(_SampleDeclarativePage page, this.index) : super(settings: page);

  final int index;

  @override
  Color? get barrierColor => null;

  @override
  String? get barrierLabel => null;

  @override
  bool get maintainState => true;

  @override
  Duration get transitionDuration => const Duration(milliseconds: 300);

  @override
  Widget buildPage(
    BuildContext context,
    Animation<double> animation,
    Animation<double> secondaryAnimation,
  ) {
    return ColoredBox(
      color: index.isEven ? const Color(0xFFE3F2FD) : const Color(0xFFF1F8E9),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          spacing: 8,
          children: <Widget>[
            Text(
              settings.name ?? '',
              style: const TextStyle(fontSize: 18, color: Colors.black),
            ),
            Text(
              'canPop: ${ModalRoute.canPopOf(context)}',
              style: const TextStyle(fontSize: 12, color: Colors.grey),
            ),
            GestureDetector(
              onTap: () => Navigator.of(context).maybePop(),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                color: const Color(0xFFFFFFFF),
                child: const Text(
                  'Pop this page',
                  style: TextStyle(fontSize: 12, color: Colors.black),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
