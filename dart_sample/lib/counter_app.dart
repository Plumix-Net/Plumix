import 'package:material_ui/material_ui.dart';

import 'counter_app_model.dart';
import 'counter_scope.dart';
import 'sample_gallery_screen.dart';

class CounterApp extends StatefulWidget {
  const CounterApp({super.key});

  @override
  State<CounterApp> createState() => _CounterAppState();
}

class _CounterAppState extends State<CounterApp> {
  late final CounterAppModel _model;

  @override
  void initState() {
    super.initState();
    _model = CounterAppModel();
  }

  @override
  void dispose() {
    _model.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Flutter.Net Sample',
      theme: ThemeData(
        textTheme: const TextTheme(
          bodyMedium: TextStyle(
            fontSize: 14,
            height: 1.43,
            letterSpacing: 0.25,
          ),
        ),
      ),
      home: CounterScope(notifier: _model, child: const SampleGalleryScreen()),
    );
  }
}
