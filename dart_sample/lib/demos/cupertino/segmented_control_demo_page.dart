import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoSegmentedControlDemoPage extends StatefulWidget {
  const CupertinoSegmentedControlDemoPage({super.key});

  @override
  State<CupertinoSegmentedControlDemoPage> createState() =>
      _CupertinoSegmentedControlDemoPageState();
}

class _CupertinoSegmentedControlDemoPageState
    extends State<CupertinoSegmentedControlDemoPage> {
  String? _selected = 'day';
  bool _disableWeek = false;
  int _changes = 0;

  @override
  Widget build(BuildContext context) {
    const Map<String, Widget> segments = <String, Widget>{
      'day': Text('Day'),
      'week': Text('Week'),
      'month': Text('Month'),
    };
    final Set<String> disabled = _disableWeek ? <String>{'week'} : <String>{};

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 14,
      children: <Widget>[
        const Text(
          'Cupertino segmented control',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Controlled selection, sliding thumb, disabled segments, custom '
          'colors, and arrow-key focus.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        CupertinoSlidingSegmentedControl<String>(
          children: segments,
          groupValue: _selected,
          disabledChildren: disabled,
          proportionalWidth: true,
          onValueChanged: _select,
        ),
        CupertinoSegmentedControl<String>(
          children: segments,
          groupValue: _selected,
          disabledChildren: disabled,
          onValueChanged: _select,
        ),
        CupertinoSegmentedControl<String>(
          children: segments,
          groupValue: _selected,
          disabledChildren: disabled,
          selectedColor: const Color(0xFF00695C),
          unselectedColor: const Color(0xFFF4FBF8),
          borderColor: const Color(0xFF00695C),
          pressedColor: const Color(0x3300695C),
          disabledColor: const Color(0xFFCFD8D5),
          disabledTextColor: const Color(0xFF78908A),
          padding: EdgeInsets.zero,
          onValueChanged: _select,
        ),
        Row(
          spacing: 10,
          children: <Widget>[
            _buildAction(
              _disableWeek ? 'Enable Week' : 'Disable Week',
              () => setState(() => _disableWeek = !_disableWeek),
            ),
            _buildAction('Clear', () => setState(() => _selected = null)),
          ],
        ),
        Text(
          'selected=${_selected ?? 'none'}, changes=$_changes, '
          'weekDisabled=$_disableWeek',
          style: const TextStyle(fontSize: 13, color: Color(0xFF455A64)),
        ),
      ],
    );
  }

  void _select(String? value) {
    setState(() {
      _selected = value;
      _changes += 1;
    });
  }

  Widget _buildAction(String label, VoidCallback onTap) {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: onTap,
      child: Container(
        width: 126,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: const BoxDecoration(
          color: Color(0xFFE8F4F1),
          borderRadius: BorderRadius.all(Radius.circular(8)),
        ),
        child: Center(
          child: Text(label, style: const TextStyle(color: Color(0xFF00695C))),
        ),
      ),
    );
  }
}
