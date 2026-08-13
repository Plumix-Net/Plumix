import 'package:material_ui/material_ui.dart';

class CarouselDemoPage extends StatelessWidget {
  const CarouselDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text('CarouselView', style: TextStyle(fontSize: 20, color: Colors.black)),
        const Text(
          'Fixed and weighted Material 3 carousels with item snapping and CarouselViewTheme.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        const Text('Fixed extent', style: TextStyle(fontSize: 14, color: Colors.black)),
        SizedBox(
          height: 132,
          child: CarouselView(itemExtent: 176, itemSnapping: true, children: _items()),
        ),
        const Text('Weighted [1, 6, 1]', style: TextStyle(fontSize: 14, color: Colors.black)),
        SizedBox(
          height: 132,
          child: CarouselViewTheme(
            data: const CarouselViewThemeData(
              padding: EdgeInsets.all(6),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(20))),
            ),
            child: CarouselView.weighted(
              flexWeights: const <int>[1, 6, 1],
              itemSnapping: true,
              children: _items(),
            ),
          ),
        ),
      ],
    );
  }

  static List<Widget> _items() {
    const List<Color> colors = <Color>[Color(0xFF6750A4), Color(0xFF386A20), Color(0xFF006874), Color(0xFF9C4230), Color(0xFF525F7A)];
    return List<Widget>.generate(
      colors.length,
      (int index) => ColoredBox(
        color: colors[index],
        child: Center(child: Text('Item ${index + 1}', style: const TextStyle(fontSize: 18, color: Colors.white))),
      ),
    );
  }
}
