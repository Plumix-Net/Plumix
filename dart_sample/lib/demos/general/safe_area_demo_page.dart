import 'package:material_ui/material_ui.dart';

/// C# parity source: src/Sample/Plumix.Sample/Demos/General/SafeAreaDemoPage.cs
class SafeAreaDemoPage extends StatelessWidget {
  const SafeAreaDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'SafeArea',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The rose surface is the simulated system intrusion. The blue child '
          'keeps a minimum 8 px inset and preserves the 28 px bottom view '
          'padding consumed by a keyboard.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        _buildBoxPreview(),
        const Text(
          'SliverSafeArea applies the same edge policy in sliver geometry.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(child: _buildSliverPreview()),
      ],
    );
  }

  static Widget _buildBoxPreview() {
    return MediaQuery(
      data: const MediaQueryData(
        padding: EdgeInsets.fromLTRB(24, 18, 32, 0),
        viewPadding: EdgeInsets.fromLTRB(24, 18, 32, 28),
      ),
      child: SizedBox(
        height: 170,
        child: ColoredBox(
          color: const Color(0xFFFFCDD2),
          child: SafeArea(
            minimum: const EdgeInsets.all(8),
            maintainBottomViewPadding: true,
            child: ColoredBox(
              color: const Color(0xFFBBDEFB),
              child: Center(
                child: Text(
                  'Safe content\n24 left · 18 top · 32 right · 28 bottom',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 15, color: Colors.black),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  static Widget _buildSliverPreview() {
    return MediaQuery(
      data: const MediaQueryData(
        padding: EdgeInsets.fromLTRB(16, 20, 24, 12),
        viewPadding: EdgeInsets.fromLTRB(16, 20, 24, 12),
      ),
      child: ColoredBox(
        color: const Color(0xFFFFE0E0),
        child: CustomScrollView(
          slivers: <Widget>[
            SliverSafeArea(
              minimum: const EdgeInsets.all(8),
              sliver: SliverFixedExtentList.builder(
                itemCount: 8,
                itemExtent: 44,
                itemBuilder: (BuildContext context, int index) {
                  return ColoredBox(
                    color: index.isEven
                        ? const Color(0xFFE3F2FD)
                        : const Color(0xFFFFFFFF),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      child: Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          'safe sliver row #$index',
                          style: const TextStyle(
                            fontSize: 14,
                            color: Colors.black,
                          ),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
