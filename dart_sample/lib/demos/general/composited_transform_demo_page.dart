import 'package:material_ui/material_ui.dart';

class CompositedTransformDemoPage extends StatefulWidget {
  const CompositedTransformDemoPage({super.key});

  @override
  State<CompositedTransformDemoPage> createState() =>
      _CompositedTransformDemoPageState();
}

class _CompositedTransformDemoPageState
    extends State<CompositedTransformDemoPage> {
  final LayerLink _link = LayerLink();
  double _targetLeft = 48;
  bool _showTarget = true;
  bool _showWhenUnlinked = true;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'CompositedTransformTarget + Follower',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          "The blue follower is painted in a separate composited layer. Its top-center stays 12 px "
          "below the orange target's bottom-center. Both labels also expose typed layer annotations.",
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        _buildPreview(),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            OutlinedButton(
              onPressed: () => setState(
                () => _targetLeft = (_targetLeft - 36).clamp(16, 224),
              ),
              child: const Text('Move left'),
            ),
            OutlinedButton(
              onPressed: () => setState(
                () => _targetLeft = (_targetLeft + 36).clamp(16, 224),
              ),
              child: const Text('Move right'),
            ),
            OutlinedButton(
              onPressed: () => setState(() => _showTarget = !_showTarget),
              child: Text(_showTarget ? 'Remove target' : 'Restore target'),
            ),
            OutlinedButton(
              onPressed: () =>
                  setState(() => _showWhenUnlinked = !_showWhenUnlinked),
              child: Text(
                _showWhenUnlinked ? 'Unlinked: visible' : 'Unlinked: hidden',
              ),
            ),
          ],
        ),
        Text(
          'target=${_showTarget ? 'x=${_targetLeft.toStringAsFixed(0)}' : 'removed'}; '
          'showWhenUnlinked=$_showWhenUnlinked',
          style: const TextStyle(fontSize: 13, color: Color(0xFF334155)),
        ),
      ],
    );
  }

  Widget _buildPreview() {
    return Container(
      height: 190,
      color: const Color(0xFFF1F5F9),
      child: Stack(
        clipBehavior: Clip.none,
        children: <Widget>[
          if (_showTarget)
            Positioned(
              left: _targetLeft,
              top: 36,
              width: 88,
              height: 52,
              child: CompositedTransformTarget(
                link: _link,
                child: AnnotatedRegion<String>(
                  value: 'target',
                  child: _buildLabel('TARGET', const Color(0xFFF59E0B)),
                ),
              ),
            ),
          Positioned(
            left: 0,
            top: 0,
            width: 120,
            height: 48,
            child: CompositedTransformFollower(
              link: _link,
              showWhenUnlinked: _showWhenUnlinked,
              offset: const Offset(0, 12),
              targetAnchor: Alignment.bottomCenter,
              followerAnchor: Alignment.topCenter,
              child: AnnotatedRegion<String>(
                value: 'follower',
                child: _buildLabel(
                  'FOLLOWER',
                  const Color(0xFF2563EB),
                  Colors.white,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  static Widget _buildLabel(String label, Color color, [Color? textColor]) {
    return Container(
      color: color,
      alignment: Alignment.center,
      child: Text(
        label,
        style: TextStyle(fontSize: 13, color: textColor ?? Colors.black),
      ),
    );
  }
}
