import 'package:material_ui/material_ui.dart';

class ScrollbarDemoPage extends StatefulWidget {
  const ScrollbarDemoPage({super.key});

  @override
  State<ScrollbarDemoPage> createState() => _ScrollbarDemoPageState();
}

class _ScrollbarDemoPageState extends State<ScrollbarDemoPage> {
  late final ScrollController _materialController;
  late final ScrollController _rawController;

  @override
  void initState() {
    super.initState();
    _materialController = ScrollController();
    _rawController = ScrollController();
  }

  @override
  void dispose() {
    _materialController.dispose();
    _rawController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Scrollbar + RawScrollbar',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Material state theming/fade beside an always-visible raw track; both thumbs are draggable.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: Row(
            spacing: 12,
            children: <Widget>[
              Expanded(
                child: _buildPane(
                  'Material state theme',
                  ScrollbarTheme(
                    data: ScrollbarThemeData(
                      trackVisibility: WidgetStateProperty.resolveWith<bool?>(
                        (Set<WidgetState> states) =>
                            states.contains(WidgetState.hovered),
                      ),
                      thickness: WidgetStateProperty.resolveWith<double?>(
                        (Set<WidgetState> states) =>
                            states.contains(WidgetState.hovered) ? 12 : 8,
                      ),
                      thumbColor: WidgetStateProperty.resolveWith<Color?>(
                        (Set<WidgetState> states) =>
                            states.contains(WidgetState.dragged)
                            ? const Color(0xFF7B1FA2)
                            : const Color(0xFF1565C0),
                      ),
                    ),
                    child: Scrollbar(
                      controller: _materialController,
                      child: _buildList(_materialController, 'material'),
                    ),
                  ),
                ),
              ),
              Expanded(
                child: _buildPane(
                  'Raw + track',
                  RawScrollbar(
                    controller: _rawController,
                    thumbVisibility: true,
                    trackVisibility: true,
                    thickness: 8,
                    radius: const Radius.circular(4),
                    thumbColor: const Color(0xB3005E7A),
                    trackColor: const Color(0x14005E7A),
                    trackBorderColor: const Color(0x33005E7A),
                    child: _buildList(_rawController, 'raw'),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPane(String label, Widget child) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 6,
      children: <Widget>[
        Text(
          label,
          style: const TextStyle(fontSize: 13, color: Colors.black54),
        ),
        Expanded(child: child),
      ],
    );
  }

  Widget _buildList(ScrollController controller, String prefix) {
    return ListView.builder(
      controller: controller,
      itemCount: 70,
      itemExtent: 40,
      padding: const EdgeInsets.all(10),
      itemBuilder: (BuildContext context, int index) {
        return Container(
          color: index.isEven ? Colors.white : const Color(0xFFF4F7FA),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Text(
            '$prefix row #$index',
            style: const TextStyle(fontSize: 13, color: Colors.black),
          ),
        );
      },
    );
  }
}
