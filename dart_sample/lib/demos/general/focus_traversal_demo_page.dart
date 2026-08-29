import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

enum FocusTraversalDemoPolicy { readingOrder, widgetOrder, ordered }

class FocusTraversalDemoPage extends StatefulWidget {
  const FocusTraversalDemoPage({super.key});

  @override
  State<FocusTraversalDemoPage> createState() => _FocusTraversalDemoPageState();
}

class _FocusTraversalDemoPageState extends State<FocusTraversalDemoPage> {
  static const List<String> tileLabels = <String>['A', 'B', 'C', 'D', 'E', 'F'];

  final List<FocusNode> _nodes = <FocusNode>[];
  FocusTraversalDemoPolicy _policy = FocusTraversalDemoPolicy.readingOrder;
  String _focused = 'none';

  @override
  void initState() {
    super.initState();
    for (final String label in tileLabels) {
      _nodes.add(FocusNode(debugLabel: label));
    }
  }

  @override
  void dispose() {
    for (final FocusNode node in _nodes) {
      node.dispose();
    }
    _nodes.clear();
    super.dispose();
  }

  String get _policyLabel => switch (_policy) {
    FocusTraversalDemoPolicy.widgetOrder => 'Policy: widget order',
    FocusTraversalDemoPolicy.ordered => 'Policy: numeric order',
    FocusTraversalDemoPolicy.readingOrder => 'Policy: reading order',
  };

  FocusTraversalPolicy _createPolicy() => switch (_policy) {
    FocusTraversalDemoPolicy.widgetOrder => WidgetOrderTraversalPolicy(),
    FocusTraversalDemoPolicy.ordered => OrderedTraversalPolicy(),
    FocusTraversalDemoPolicy.readingOrder => ReadingOrderTraversalPolicy(),
  };

  void _cyclePolicy() {
    setState(() {
      _policy = switch (_policy) {
        FocusTraversalDemoPolicy.readingOrder => FocusTraversalDemoPolicy.widgetOrder,
        FocusTraversalDemoPolicy.widgetOrder => FocusTraversalDemoPolicy.ordered,
        FocusTraversalDemoPolicy.ordered => FocusTraversalDemoPolicy.readingOrder,
      };
    });
  }

  void _handleFocusChange(String label, bool focused) {
    setState(() {
      if (focused) {
        _focused = label;
      } else if (_focused == label) {
        _focused = 'none';
      }
    });
  }

  Widget _tile(int index) {
    final String label = tileLabels[index];
    final FocusNode node = _nodes[index];
    Widget tile = Expanded(
      child: Focus(
        focusNode: node,
        autofocus: index == 0,
        onFocusChange: (bool focused) => _handleFocusChange(label, focused),
        child: GestureDetector(
          onTap: node.requestFocus,
          child: Container(
            color: node.hasPrimaryFocus ? Colors.blue : Colors.grey.shade300,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 18),
            child: Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 16,
                color: node.hasPrimaryFocus ? Colors.white : Colors.black,
              ),
            ),
          ),
        ),
      ),
    );

    // Tile E stays focusable by tap or by the arrow keys, but Tab skips it.
    if (label == 'E') {
      tile = ExcludeFocusTraversal(child: tile);
    }

    // The ordered policy sorts the bottom row before the top one.
    return FocusTraversalOrder(
      order: NumericFocusOrder(index < 3 ? index + 10 : (index - 3).toDouble()),
      child: tile,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'FocusTraversalGroup + policies',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Tab and Shift+Tab walk the sorted order; the arrow keys use the geometric '
          'directional policy. Tile E is excluded from traversal but stays focusable.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: _policyLabel,
                onTap: _cyclePolicy,
                background: Colors.blue,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Next',
                onTap: () => FocusManager.instance.primaryFocus?.nextFocus(),
                background: Colors.green,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Previous',
                onTap: () => FocusManager.instance.primaryFocus?.previousFocus(),
                background: Colors.blueGrey,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
          ],
        ),
        FocusTraversalGroup(
          policy: _createPolicy(),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              Row(spacing: 8, children: <Widget>[_tile(0), _tile(1), _tile(2)]),
              Row(spacing: 8, children: <Widget>[_tile(3), _tile(4), _tile(5)]),
            ],
          ),
        ),
        Text(
          'Focused tile: $_focused',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
      ],
    );
  }
}
