import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class HeroDemoPage extends StatefulWidget {
  const HeroDemoPage({super.key});

  @override
  State<HeroDemoPage> createState() => _HeroDemoPageState();
}

class _HeroDemoPageState extends State<HeroDemoPage> {
  bool _heroModeEnabled = true;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'Hero',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Tap a tile to push a detail route. The tile flies between the two routes along the '
            'MaterialRectArcTween MaterialApp installs on its HeroController.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _buildButton(
                label: 'HeroMode enabled',
                onTap: () => _setHeroMode(true),
                width: 140,
                background: const Color(0xFFDCE3ED),
              ),
              _buildButton(
                label: 'HeroMode disabled',
                onTap: () => _setHeroMode(false),
                width: 146,
                background: const Color(0xFFDCE3ED),
              ),
            ],
          ),
          Text(
            'state: heroMode=${_heroModeEnabled ? 'enabled' : 'disabled'}',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
          const Text(
            'A disabled HeroMode hides the subtree from Hero._allHeroesFor, so the route transition '
            'runs without a flight.',
            style: TextStyle(fontSize: 11, color: Colors.black54),
          ),
          _buildTile(
            context,
            tag: 'hero-demo-plain',
            label: 'Default flight',
            color: const Color(0xFF1D3557),
            useShuttleBuilder: false,
          ),
          _buildTile(
            context,
            tag: 'hero-demo-shuttle',
            label: 'Custom shuttle + placeholder',
            color: const Color(0xFFE07A5F),
            useShuttleBuilder: true,
          ),
          const Text(
            'The second tile supplies a flightShuttleBuilder (what the overlay paints while the hero '
            'is in the air) and a placeholderBuilder (what each route shows in its place).',
            style: TextStyle(fontSize: 11, color: Colors.black54),
          ),
        ],
      ),
    );
  }

  Widget _buildTile(
    BuildContext context, {
    required String tag,
    required String label,
    required Color color,
    required bool useShuttleBuilder,
  }) {
    Widget hero = Hero(
      tag: tag,
      flightShuttleBuilder: useShuttleBuilder ? _buildShuttle : null,
      placeholderBuilder: useShuttleBuilder ? _buildPlaceholder : null,
      child: buildHeroCard(
        label: label,
        color: color,
        width: 150,
        height: 84,
        fontSize: 12,
      ),
    );

    if (!_heroModeEnabled) {
      hero = HeroMode(enabled: false, child: hero);
    }

    return Row(
      children: <Widget>[
        GestureDetector(
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute<void>(
              builder: (BuildContext context) => HeroDetailPage(
                tag: tag,
                label: label,
                color: color,
                useShuttleBuilder: useShuttleBuilder,
              ),
              settings: RouteSettings(name: '/hero/$tag'),
            ),
          ),
          child: hero,
        ),
      ],
    );
  }

  static Widget _buildShuttle(
    BuildContext flightContext,
    Animation<double> animation,
    HeroFlightDirection flightDirection,
    BuildContext fromHeroContext,
    BuildContext toHeroContext,
  ) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xFF264653),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Center(
        child: Text(
          flightDirection == HeroFlightDirection.push ? 'flying →' : 'flying ←',
          style: const TextStyle(fontSize: 12, color: Colors.white),
        ),
      ),
    );
  }

  static Widget _buildPlaceholder(
    BuildContext context,
    Size heroSize,
    Widget child,
  ) {
    return SizedBox(
      width: heroSize.width,
      height: heroSize.height,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: const Color(0xFFF1F1F1),
          border: Border.all(color: const Color(0xFFBDBDBD)),
          borderRadius: BorderRadius.circular(14),
        ),
        child: const Center(
          child: Text(
            'placeholder',
            style: TextStyle(fontSize: 11, color: Colors.black54),
          ),
        ),
      ),
    );
  }

  Widget _buildButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: background,
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  void _setHeroMode(bool value) {
    setState(() {
      _heroModeEnabled = value;
    });
  }
}

Widget buildHeroCard({
  required String label,
  required Color color,
  required double width,
  required double height,
  required double fontSize,
}) {
  return SizedBox(
    width: width,
    height: height,
    child: DecoratedBox(
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Center(
        child: Text(
          label,
          style: TextStyle(fontSize: fontSize, color: Colors.white),
        ),
      ),
    ),
  );
}

class HeroDetailPage extends StatelessWidget {
  const HeroDetailPage({
    super.key,
    required this.tag,
    required this.label,
    required this.color,
    required this.useShuttleBuilder,
  });

  final String tag;
  final String label;
  final Color color;
  final bool useShuttleBuilder;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xFFFDFDFD),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'Hero detail',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          Hero(
            tag: tag,
            child: buildHeroCard(
              label: label,
              color: color,
              width: 288,
              height: 176,
              fontSize: 16,
            ),
          ),
          const Text(
            "The destination hero owns the flight: its createRectTween, curve and flightShuttleBuilder "
            "win over the source hero's.",
            style: TextStyle(fontSize: 12, color: Colors.black54),
          ),
          CounterTapButton(
            label: 'Pop',
            onTap: () => Navigator.of(context).pop(),
            background: const Color(0xFFDCE3ED),
            foreground: Colors.black,
            fontSize: 12,
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          ),
        ],
      ),
    );
  }
}
