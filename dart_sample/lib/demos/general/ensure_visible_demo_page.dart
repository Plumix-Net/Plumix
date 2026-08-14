import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class EnsureVisibleDemoPage extends StatefulWidget {
  const EnsureVisibleDemoPage({super.key});

  @override
  State<EnsureVisibleDemoPage> createState() => _EnsureVisibleDemoPageState();
}

class _EnsureVisibleDemoPageState extends State<EnsureVisibleDemoPage> {
  static const int itemCount = 40;
  static const double itemExtent = 56.0;

  final ScrollController _outerController = ScrollController();
  final ScrollController _innerController = ScrollController();
  final Map<int, BuildContext> _itemContexts = <int, BuildContext>{};
  double _alignment = 0.0;
  ScrollPositionAlignmentPolicy _policy = ScrollPositionAlignmentPolicy.explicit;
  String _status = 'Pick a row to reveal.';

  @override
  void dispose() {
    _outerController.dispose();
    _innerController.dispose();
    super.dispose();
  }

  String get _policyLabel => switch (_policy) {
    ScrollPositionAlignmentPolicy.keepVisibleAtStart => 'Keep at start',
    ScrollPositionAlignmentPolicy.keepVisibleAtEnd => 'Keep at end',
    ScrollPositionAlignmentPolicy.explicit => 'Explicit',
  };

  void _cycleAlignment() {
    setState(() {
      _alignment = switch (_alignment) {
        < 0.25 => 0.5,
        < 0.75 => 1.0,
        _ => 0.0,
      };
    });
  }

  void _cyclePolicy() {
    setState(() {
      _policy = switch (_policy) {
        ScrollPositionAlignmentPolicy.explicit => ScrollPositionAlignmentPolicy.keepVisibleAtStart,
        ScrollPositionAlignmentPolicy.keepVisibleAtStart => ScrollPositionAlignmentPolicy.keepVisibleAtEnd,
        ScrollPositionAlignmentPolicy.keepVisibleAtEnd => ScrollPositionAlignmentPolicy.explicit,
      };
    });
  }

  void _reveal(int index) {
    final BuildContext? itemContext = _itemContexts[index];
    if (itemContext == null) {
      setState(() => _status = 'Row $index is not built yet; scroll closer to it first.');
      return;
    }

    Scrollable.ensureVisible(
      itemContext,
      alignment: _alignment,
      duration: const Duration(milliseconds: 400),
      alignmentPolicy: _policy,
    );
    setState(
      () => _status = 'Revealed row $index at alignment ${_alignment.toStringAsFixed(1)} ($_policyLabel).',
    );
  }

  Widget _buildOuterSection(BuildContext context, int index) {
    if (index != 1) {
      return Container(
        height: 160,
        color: index == 0 ? Colors.white70 : const Color(0xFFDCDCDC),
        alignment: Alignment.center,
        child: Text(
          index == 0 ? 'Outer header' : 'Outer footer',
          style: const TextStyle(fontSize: 15, color: Colors.grey),
        ),
      );
    }

    return Container(
      height: 240,
      color: Colors.white,
      child: ListView.builder(
        controller: _innerController,
        itemCount: itemCount,
        itemExtent: itemExtent,
        itemBuilder: _buildRow,
      ),
    );
  }

  Widget _buildRow(BuildContext context, int index) {
    _itemContexts[index] = context;
    return Container(
      color: index % 2 == 0 ? Colors.white : const Color(0xFFF0F8FF),
      alignment: Alignment.centerLeft,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: Text('Row $index', style: const TextStyle(fontSize: 14, color: Colors.black)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text('Scrollable.ensureVisible', style: TextStyle(fontSize: 20, color: Colors.black)),
        const Text(
          'The inner list is nested in an outer scroller, so a reveal walks both viewports.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: 'Alignment ${_alignment.toStringAsFixed(1)}',
                onTap: _cycleAlignment,
                background: Colors.blue,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: _policyLabel,
                onTap: _cyclePolicy,
                background: Colors.blueGrey,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: 'Reveal row 8',
                onTap: () => _reveal(8),
                background: Colors.green,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Reveal row 30',
                onTap: () => _reveal(30),
                background: Colors.orange,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
          ],
        ),
        Text(_status, style: const TextStyle(fontSize: 13, color: Colors.black)),
        Expanded(
          child: ListView.builder(
            controller: _outerController,
            itemCount: 3,
            itemBuilder: _buildOuterSection,
          ),
        ),
      ],
    );
  }
}
