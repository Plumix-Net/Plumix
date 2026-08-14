import 'package:material_ui/material_ui.dart';

class PageViewDemoPage extends StatefulWidget {
  const PageViewDemoPage({super.key});

  @override
  State<PageViewDemoPage> createState() => _PageViewDemoPageState();
}

class _PageViewDemoPageState extends State<PageViewDemoPage> {
  static const List<Color> _pageColors = <Color>[
    Color(0xFFE3F2FD),
    Color(0xFFE8F5E9),
    Color(0xFFFFF3E0),
    Color(0xFFF3E5F5),
    Color(0xFFE0F7FA),
    Color(0xFFFCE4EC),
  ];

  late final PageController _controller;
  int _page = 0;

  @override
  void initState() {
    super.initState();
    _controller = PageController(viewportFraction: 0.85);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'PageView.builder',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Pages are built lazily by a sliver fill viewport; viewportFraction 0.85 pads both ends.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: PageView.builder(
            itemCount: _pageColors.length,
            controller: _controller,
            onPageChanged: (int page) => setState(() => _page = page),
            itemBuilder: (BuildContext context, int index) {
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
                child: Container(
                  color: _pageColors[index],
                  padding: const EdgeInsets.all(16),
                  child: Center(
                    child: Text(
                      'page #$index',
                      style: const TextStyle(fontSize: 24, color: Colors.black),
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          spacing: 12,
          children: <Widget>[
            TextButton(
              onPressed: () => _controller.previousPage(
                duration: const Duration(milliseconds: 300),
                curve: Curves.ease,
              ),
              child: const Text('Previous'),
            ),
            Text(
              'page ${_page + 1} of ${_pageColors.length}',
              style: const TextStyle(fontSize: 14, color: Colors.black),
            ),
            TextButton(
              onPressed: () => _controller.nextPage(
                duration: const Duration(milliseconds: 300),
                curve: Curves.ease,
              ),
              child: const Text('Next'),
            ),
          ],
        ),
      ],
    );
  }
}
