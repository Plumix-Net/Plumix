import 'package:material_ui/material_ui.dart';
import 'package:cupertino_ui/cupertino_ui.dart';

class RestorationDemoPage extends StatefulWidget {
  const RestorationDemoPage({super.key});

  @override
  State<RestorationDemoPage> createState() => _RestorationDemoPageState();
}

class _RestorationDemoPageState extends State<RestorationDemoPage>
    with RestorationMixin {
  final RestorableInt _counter = RestorableInt(0);
  final RestorableTextEditingController _note =
      RestorableTextEditingController();

  @override
  String? get restorationId => 'restoration-demo';

  @override
  void restoreState(RestorationBucket? oldBucket, bool initialRestore) {
    registerForRestoration(_counter, 'counter');
    registerForRestoration(_note, 'note');
  }

  @override
  Widget build(BuildContext context) {
    return UnmanagedRestorationScope(
      bucket: bucket,
      child: SingleChildScrollView(
        restorationId: 'scroll',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            Text(
              bucket == null
                  ? 'State restoration: unavailable'
                  : 'State restoration: enabled',
            ),
            const Text(
              "Increment the counter, write a note and scroll. On Android, enable Don't keep "
              'activities, background the app and reopen it. On iOS, background the app, '
              'terminate it through the debugger and reopen it. The page, values and scroll '
              'position are restored when the OS supplies saved state.',
            ),
            Text('Restorable counter: ${_counter.value}'),
            OutlinedButton(
              onPressed: () => setState(() => _counter.value++),
              child: const Text('Increment'),
            ),
            TextField(
              controller: _note.value,
              decoration: const InputDecoration(labelText: 'Restorable note'),
            ),
            const TextField(
              restorationId: 'material-note',
              maxLines: 3,
              decoration: InputDecoration(labelText: 'Owned Material note'),
            ),
            TextFormField(
              restorationId: 'material-form',
              initialValue: 'Material seed',
              maxLines: 3,
              autovalidateMode: AutovalidateMode.onUserInteraction,
              validator: (String? value) =>
                  value == null || value.isEmpty ? 'Required' : null,
              decoration: const InputDecoration(
                labelText: 'Restorable Material form',
              ),
            ),
            const CupertinoTextField(
              restorationId: 'cupertino-note',
              maxLines: 3,
              placeholder: 'Owned Cupertino note',
            ),
            CupertinoTextFormFieldRow(
              restorationId: 'cupertino-form',
              initialValue: 'Cupertino seed',
              maxLines: 3,
              autovalidateMode: AutovalidateMode.onUserInteraction,
              validator: (String? value) =>
                  value == null || value.isEmpty ? 'Required' : null,
              prefix: const Text('Restorable Cupertino form'),
            ),
            for (int index = 1; index <= 24; index++)
              Container(
                height: 48,
                alignment: Alignment.centerLeft,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Text('Restorable row $index'),
              ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _counter.dispose();
    _note.dispose();
    super.dispose();
  }
}
