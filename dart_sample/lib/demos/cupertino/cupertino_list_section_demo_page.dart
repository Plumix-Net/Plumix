import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoListSectionDemoPage extends StatelessWidget {
  const CupertinoListSectionDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    final Color label = CupertinoColors.label.resolveFrom(context);
    final Color secondaryLabel = CupertinoColors.secondaryLabel.resolveFrom(
      context,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12.0,
      children: <Widget>[
        Text(
          'Cupertino list + form sections',
          style: TextStyle(fontSize: 20.0, color: label),
        ),
        Text(
          'Compare list rows with split form rows, helper/error content, and '
          'inset-grouped decoration.',
          style: TextStyle(fontSize: 14.0, color: secondaryLabel),
        ),
        CupertinoListSection(
          header: const Text('CONNECTIVITY'),
          footer: const Text(
            'The base section draws full-width borders above and below its '
            'rows.',
          ),
          children: const <Widget>[
            CupertinoListTile(
              title: Text('Wi-Fi'),
              additionalInfo: Text('Studio'),
              leading: Icon(CupertinoIcons.wifi),
              trailing: CupertinoListTileChevron(),
              onTap: _completeImmediately,
            ),
            CupertinoListTile(
              title: Text('Bluetooth'),
              additionalInfo: Text('On'),
              leading: Icon(CupertinoIcons.bluetooth),
              trailing: CupertinoListTileChevron(),
              onTap: _completeImmediately,
            ),
          ],
        ),
        CupertinoListSection.insetGrouped(
          header: const Text('Account'),
          footer: const Text(
            'Inset groups clip their rows to a 10 px rounded superellipse.',
          ),
          hasLeading: false,
          separatorColor: CupertinoColors.systemGrey4,
          children: const <Widget>[
            CupertinoListTile.notched(
              title: Text('Profile'),
              additionalInfo: Text('Egor'),
              trailing: CupertinoListTileChevron(),
              onTap: _completeImmediately,
            ),
            CupertinoListTile.notched(
              title: Text('Subscriptions'),
              trailing: CupertinoListTileChevron(),
              onTap: _completeImmediately,
            ),
          ],
        ),
        CupertinoFormSection.insetGrouped(
          header: const Text('PROFILE'),
          footer: const Text(
            'Form rows keep values trailing-aligned and supporting text below '
            'the row.',
          ),
          children: const <Widget>[
            CupertinoFormRow(
              prefix: Text('Name'),
              helper: Text('Shown on shared pages.'),
              child: Text('Egor'),
            ),
            CupertinoFormRow(
              prefix: Text('Email'),
              error: Text('Address has not been verified.'),
              child: Text('egor@example.com'),
            ),
          ],
        ),
      ],
    );
  }
}

Future<void> _completeImmediately() async {}
