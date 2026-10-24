import 'package:dart_sample/counter_app.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('smoke test: menu is rendered', (WidgetTester tester) async {
    await tester.pumpWidget(const CounterApp());

    expect(find.text('Plumix.Sample widget pages'), findsOneWidget);
    expect(find.textContaining('Counter'), findsWidgets);
  });
}
