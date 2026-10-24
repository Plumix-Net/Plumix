// Renders every sample route to a PNG for the screen-by-screen comparison with the C# sample
// (src/Plumix.Tests/SampleScreenCaptureTests.cs runs the same pump sequence). Opt-in:
//   SCREEN_CAPTURE_DIR=/abs/dir flutter test test/screen_capture_test.dart --update-goldens
import 'dart:io';

import 'package:dart_sample/counter_app.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final String? outputDir = Platform.environment['SCREEN_CAPTURE_DIR'];
  final List<String> routes = File('lib/sample_routes.dart')
      .readAsLinesSync()
      .map(
        (String line) => RegExp(r"'(/[a-z0-9/-]*)'").firstMatch(line)?.group(1),
      )
      .whereType<String>()
      .toList();

  for (final String route in routes) {
    testWidgets('capture $route', (WidgetTester tester) async {
      tester.view.physicalSize = const Size(800, 600);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);
      final List<String> errors = <String>[];
      final FlutterExceptionHandler? previousOnError = FlutterError.onError;
      FlutterError.onError = (FlutterErrorDetails details) => errors.add(
        '${details.library}: ${details.exceptionAsString().split('\n').first}',
      );

      await tester.pumpWidget(const CounterApp());
      await tester.pump();
      if (route != '/') {
        tester
            .state<NavigatorState>(find.byType(Navigator).last)
            .restorablePushNamed(route);
      }
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));
      await tester.pump(const Duration(seconds: 1));

      final String name = route == '/'
          ? 'menu'
          : route.substring(1).replaceAll('/', '_');
      File('$outputDir/$name.texts.txt').writeAsStringSync(
        find
            .byType(RichText, skipOffstage: false)
            .evaluate()
            .map(
              (Element e) => (e.widget as RichText).text
                  .toPlainText()
                  .replaceAll('\n', r'\n'),
            )
            .join('\n'),
      );
      File('$outputDir/$name.layout.txt').writeAsStringSync(
        find
            .byType(RichText, skipOffstage: false)
            .evaluate()
            .map((Element e) {
              final RenderBox box = e.renderObject! as RenderBox;
              if (!box.attached || !box.hasSize) {
                return '-';
              }
              final Offset o = box.localToGlobal(Offset.zero);
              return '${o.dx.toStringAsFixed(4)} ${o.dy.toStringAsFixed(4)} '
                  '${box.size.width.toStringAsFixed(4)} ${box.size.height.toStringAsFixed(4)}';
            })
            .join('\n'),
      );
      await expectLater(
        find.byType(CounterApp),
        matchesGoldenFile(Uri.file('$outputDir/$name.png')),
      );
      // Pages with endless animations or pending timers must not fail the capture run.
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(minutes: 1));
      FlutterError.onError = previousOnError;
      File('$outputDir/$name.errors.txt').writeAsStringSync(errors.join('\n'));
    }, skip: outputDir == null);
  }
}
