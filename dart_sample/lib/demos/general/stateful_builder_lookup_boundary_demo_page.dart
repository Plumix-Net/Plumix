import 'package:material_ui/material_ui.dart';

class StatefulBuilderLookupBoundaryDemoPage extends StatelessWidget {
  const StatefulBuilderLookupBoundaryDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    int count = 0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 16,
      children: <Widget>[
        const Text(
          'StatefulBuilder + LookupBoundary',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'StatefulBuilder owns a local rebuild. LookupBoundary hides ancestors '
          'only from its bounded static lookup helpers.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Container(
          color: const Color(0xFFF4F7FA),
          padding: const EdgeInsets.all(12),
          child: StatefulBuilder(
            builder: (BuildContext context, StateSetter setState) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                spacing: 8,
                children: <Widget>[
                  Text(
                    'local count: $count',
                    style: const TextStyle(color: Color(0xFF31506F)),
                  ),
                  TextButton(
                    onPressed: () => setState(() => count += 1),
                    child: const Text('Increment local state'),
                  ),
                ],
              );
            },
          ),
        ),
        _DemoLookupScope(
          label: 'outer scope',
          child: LookupBoundary(
            child: Builder(
              builder: (BuildContext context) {
                final String bounded =
                    LookupBoundary.getInheritedWidgetOfExactType<
                          _DemoLookupScope
                        >(context)
                        ?.label ??
                    'hidden';
                final String regular =
                    context
                        .dependOnInheritedWidgetOfExactType<_DemoLookupScope>()
                        ?.label ??
                    'missing';
                return Container(
                  color: const Color(0xFFE7EDF6),
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    spacing: 6,
                    children: <Widget>[
                      Text('bounded lookup: $bounded'),
                      Text('regular context lookup: $regular'),
                    ],
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }
}

class _DemoLookupScope extends InheritedWidget {
  const _DemoLookupScope({required this.label, required super.child});

  final String label;

  @override
  bool updateShouldNotify(_DemoLookupScope oldWidget) {
    return label != oldWidget.label;
  }
}
