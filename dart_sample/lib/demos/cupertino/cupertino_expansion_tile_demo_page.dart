import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoExpansionTileDemoPage extends StatefulWidget {
  const CupertinoExpansionTileDemoPage({super.key});

  @override
  State<CupertinoExpansionTileDemoPage> createState() =>
      _CupertinoExpansionTileDemoPageState();
}

class _CupertinoExpansionTileDemoPageState
    extends State<CupertinoExpansionTileDemoPage> {
  final ExpansibleController _fadeController = ExpansibleController();
  final ExpansibleController _scrollController = ExpansibleController();

  @override
  void dispose() {
    _fadeController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final Color label = CupertinoColors.label.resolveFrom(context);
    final Color secondaryLabel = CupertinoColors.secondaryLabel.resolveFrom(
      context,
    );
    final Color panel = CupertinoColors.secondarySystemBackground.resolveFrom(
      context,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12.0,
      children: <Widget>[
        Text(
          'Cupertino list + expansion tiles',
          style: TextStyle(fontSize: 20.0, color: label),
        ),
        Text(
          'Compare base/notched rows, async activation, chevrons, and both '
          'expansion transitions.',
          style: TextStyle(fontSize: 14.0, color: secondaryLabel),
        ),
        ClipRRect(
          borderRadius: BorderRadius.circular(14.0),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              const CupertinoListTile(
                title: Text('Base list tile'),
                subtitle: Text('Subtitle and additional info'),
                additionalInfo: Text('Connected'),
                leading: Icon(CupertinoIcons.wifi),
                trailing: CupertinoListTileChevron(),
                backgroundColor: CupertinoColors.systemBackground,
                onTap: _completeImmediately,
              ),
              CupertinoListTile.notched(
                title: const Text('Notched list tile'),
                subtitle: const Text('Inset-grouped geometry'),
                trailing: const CupertinoListTileChevron(),
                backgroundColor: panel,
                onTap: _completeImmediately,
              ),
            ],
          ),
        ),
        _buildExpansionTile(
          'Fade transition',
          'The fully extended body fades over the height animation.',
          _fadeController,
          ExpansionTileTransitionMode.fade,
          panel,
        ),
        _buildExpansionTile(
          'Scroll transition',
          'The body scrolls out from under the 44 px header.',
          _scrollController,
          ExpansionTileTransitionMode.scroll,
          panel,
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          children: <Widget>[
            CupertinoButton(
              onPressed: _fadeController.toggle,
              child: const Text('Toggle fade'),
            ),
            CupertinoButton(
              onPressed: _scrollController.toggle,
              child: const Text('Toggle scroll'),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildExpansionTile(
    String title,
    String body,
    ExpansibleController controller,
    ExpansionTileTransitionMode mode,
    Color panel,
  ) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(14.0),
      child: ColoredBox(
        color: panel,
        child: CupertinoExpansionTile(
          title: Text(title),
          controller: controller,
          transitionMode: mode,
          child: Padding(
            padding: const EdgeInsets.all(
              14.0,
            ).copyWith(left: 20.0, right: 20.0),
            child: Text(body),
          ),
        ),
      ),
    );
  }
}

Future<void> _completeImmediately() async {}
