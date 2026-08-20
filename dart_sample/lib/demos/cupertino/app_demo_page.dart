import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoAppDemoPage extends StatelessWidget {
  const CupertinoAppDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return CupertinoApp(
      debugShowCheckedModeBanner: false,
      title: 'CupertinoApp demo',
      theme: const CupertinoThemeData(
        primaryColor: CupertinoColors.systemIndigo,
      ),
      routes: <String, WidgetBuilder>{
        '/': _buildHome,
        '/details': _buildDetails,
      },
    );
  }

  static Widget _buildHome(BuildContext context) {
    final CupertinoThemeData theme = CupertinoTheme.of(context);
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    final Color secondaryLabel = CupertinoDynamicColor.resolve(
      CupertinoColors.secondaryLabel,
      context,
    );
    return _buildPage(
      children: <Widget>[
        Text('CupertinoApp', style: TextStyle(fontSize: 22, color: label)),
        Text(
          'The nested shell supplies Cupertino theme, localization, '
          'selection, scroll, and route defaults.',
          style: TextStyle(fontSize: 14, color: secondaryLabel),
        ),
        Text(
          'locale action: '
          '${CupertinoLocalizations.of(context).selectAllButtonLabel}',
          style: TextStyle(fontSize: 13, color: secondaryLabel),
        ),
        CupertinoButton(
          color: theme.primaryColor,
          onPressed: () => Navigator.of(context).pushNamed('/details'),
          child: Text(
            'Push Cupertino route',
            style: TextStyle(color: theme.primaryContrastingColor),
          ),
        ),
      ],
    );
  }

  static Widget _buildDetails(BuildContext context) {
    final CupertinoThemeData theme = CupertinoTheme.of(context);
    final Color label = CupertinoDynamicColor.resolve(
      CupertinoColors.label,
      context,
    );
    return _buildPage(
      children: <Widget>[
        Text('Details route', style: TextStyle(fontSize: 22, color: label)),
        CupertinoButton(
          color: theme.primaryColor,
          onPressed: () => Navigator.of(context).pop(),
          child: Text(
            'Pop route',
            style: TextStyle(color: theme.primaryContrastingColor),
          ),
        ),
      ],
    );
  }

  static Widget _buildPage({required List<Widget> children}) {
    return CupertinoPageScaffold(
      child: SafeArea(
        minimum: const EdgeInsets.all(20),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            spacing: 14,
            children: children,
          ),
        ),
      ),
    );
  }
}
