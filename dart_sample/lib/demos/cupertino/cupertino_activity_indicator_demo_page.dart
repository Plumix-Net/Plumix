import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoActivityIndicatorDemoPage extends StatefulWidget {
  const CupertinoActivityIndicatorDemoPage({super.key});

  @override
  State<CupertinoActivityIndicatorDemoPage> createState() =>
      _CupertinoActivityIndicatorDemoPageState();
}

class _CupertinoActivityIndicatorDemoPageState extends State<CupertinoActivityIndicatorDemoPage> {
  double _progress = 0.6;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino activity indicators',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Spinning ticks, partially revealed ticks and the linear progress '
          'bar.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 24,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            _buildLabeled('Default', const CupertinoActivityIndicator()),
            _buildLabeled(
              'radius 20',
              const CupertinoActivityIndicator(radius: 20),
            ),
            _buildLabeled(
              'Tinted',
              const CupertinoActivityIndicator(
                color: CupertinoColors.activeOrange,
                radius: 20,
              ),
            ),
            _buildLabeled(
              'Paused',
              const CupertinoActivityIndicator(animating: false, radius: 20),
            ),
            _buildLabeled(
              'Partial',
              CupertinoActivityIndicator.partiallyRevealed(
                progress: _progress,
                radius: 20,
              ),
            ),
          ],
        ),
        const Text(
          'Progress for the partial spinner and the bars below:',
          style: TextStyle(fontSize: 14, color: Colors.black),
        ),
        CupertinoSlider(
          value: _progress,
          onChanged: (double value) => setState(() => _progress = value),
        ),
        CupertinoLinearActivityIndicator(progress: _progress),
        CupertinoLinearActivityIndicator(
          progress: _progress,
          height: 10,
          color: CupertinoColors.activeGreen,
        ),
      ],
    );
  }

  Widget _buildLabeled(String label, Widget indicator) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      spacing: 6,
      children: <Widget>[
        SizedBox(width: 44, height: 44, child: Center(child: indicator)),
        Text(label, style: const TextStyle(fontSize: 12, color: Colors.black)),
      ],
    );
  }
}
