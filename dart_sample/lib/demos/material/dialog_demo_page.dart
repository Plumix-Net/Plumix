import 'package:cupertino_ui/cupertino_ui.dart' as cupertino;
import 'package:material_ui/material_ui.dart';

class DialogDemoPage extends StatefulWidget {
  const DialogDemoPage({super.key});

  @override
  State<DialogDemoPage> createState() => _DialogDemoPageState();
}

class _DialogDemoPageState extends State<DialogDemoPage> {
  bool _scrollable = false;
  bool _barrierDismissible = true;
  bool _useThemeOverrides = false;
  bool _appleAdaptive = false;
  String _lastResult = 'none';

  @override
  Widget build(BuildContext context) {
    final DialogThemeData dialogTheme = _useThemeOverrides
        ? DialogThemeData(
            backgroundColor: Colors.teal.shade50,
            iconColor: Colors.teal.shade800,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
            titleTextStyle: Theme.of(
              context,
            ).textTheme.headlineSmall?.copyWith(color: Colors.teal.shade900),
            barrierColor: const Color(0x99004D40),
          )
        : const DialogThemeData();
    return Theme(
      data: Theme.of(context).copyWith(
        dialogTheme: dialogTheme,
        platform: _appleAdaptive ? TargetPlatform.iOS : null,
      ),
      child: Builder(builder: _buildContent),
    );
  }

  Widget _buildContent(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 14,
      children: <Widget>[
        const Text('Dialog family', style: TextStyle(fontSize: 20)),
        const Text(
          'Dialog, AlertDialog, SimpleDialog, typed results, intrinsic width, actions overflow, and scrollable choices.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _scrollable = !_scrollable),
              child: Text(_scrollable ? 'Scrollable' : 'Static'),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _barrierDismissible = !_barrierDismissible),
              child: Text(
                _barrierDismissible ? 'Barrier closes' : 'Barrier locked',
              ),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _useThemeOverrides = !_useThemeOverrides),
              child: Text(_useThemeOverrides ? 'Theme on' : 'Theme off'),
            ),
            TextButton(
              onPressed: () =>
                  setState(() => _appleAdaptive = !_appleAdaptive),
              child: Text(_appleAdaptive ? 'Apple platform' : 'Host platform'),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            ElevatedButton(
              onPressed: () => _showAlert(context),
              child: const Text('SHOW ALERT'),
            ),
            OutlinedButton(
              onPressed: () => _showPlainDialog(context),
              child: const Text('SHOW DIALOG'),
            ),
            FilledButton(
              onPressed: () => _showSimpleDialog(context),
              child: const Text('SHOW SIMPLE'),
            ),
            TextButton(
              onPressed: () => _showAdaptive(context),
              child: const Text('SHOW ADAPTIVE'),
            ),
          ],
        ),
        Text('Last result: $_lastResult', style: const TextStyle(fontSize: 13)),
      ],
    );
  }

  Future<void> _showAlert(BuildContext context) async {
    final String? result = await showDialog<String>(
      context: context,
      barrierDismissible: _barrierDismissible,
      builder: (BuildContext routeContext) => AlertDialog(
        icon: const Icon(Icons.info_outline),
        title: const Text('Delete draft?'),
        content: _scrollable
            ? const Column(
                children: <Widget>[
                  Text(
                    'This dialog keeps actions visible while the message scrolls.',
                  ),
                  SizedBox(height: 180),
                  Text('End of the scrollable content.'),
                ],
              )
            : const Text('The draft can be restored later from history.'),
        scrollable: _scrollable,
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.pop(routeContext, 'cancel'),
            child: const Text('CANCEL'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(routeContext, 'delete'),
            child: const Text('DELETE'),
          ),
        ],
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }

  Future<void> _showPlainDialog(BuildContext context) async {
    final String? result = await showDialog<String>(
      context: context,
      barrierDismissible: _barrierDismissible,
      builder: (BuildContext routeContext) => Dialog(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            spacing: 16,
            children: <Widget>[
              const Text('Base Dialog', style: TextStyle(fontSize: 20)),
              const Text(
                'This uses the same themed Material surface and route barrier.',
              ),
              TextButton(
                onPressed: () => Navigator.pop(routeContext, 'closed'),
                child: const Text('CLOSE'),
              ),
            ],
          ),
        ),
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }

  Future<void> _showSimpleDialog(BuildContext context) async {
    final String? result = await showDialog<String>(
      context: context,
      barrierDismissible: _barrierDismissible,
      builder: (BuildContext routeContext) => SimpleDialog(
        title: const Text('Select workspace'),
        children: <Widget>[
          SimpleDialogOption(
            onPressed: () => Navigator.pop(routeContext, 'personal'),
            child: const Text('Personal workspace'),
          ),
          SimpleDialogOption(
            onPressed: () => Navigator.pop(routeContext, 'team'),
            child: const Text('Team workspace'),
          ),
          SimpleDialogOption(
            onPressed: () => Navigator.pop(routeContext, 'guest'),
            child: const Text('Guest workspace'),
          ),
        ],
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }

  Future<void> _showAdaptive(BuildContext context) async {
    final bool apple = switch (Theme.of(context).platform) {
      TargetPlatform.iOS || TargetPlatform.macOS => true,
      _ => false,
    };
    final String? result = await showAdaptiveDialog<String>(
      context: context,
      barrierDismissible: _barrierDismissible,
      builder: (BuildContext routeContext) => AlertDialog.adaptive(
        title: const Text('Adaptive alert'),
        content: const Text(
          'Material on desktop platforms, Cupertino on Apple platforms.',
        ),
        actions: apple
            ? <Widget>[
                cupertino.CupertinoDialogAction(
                  onPressed: () => Navigator.pop(routeContext, 'cancel'),
                  child: const Text('Cancel'),
                ),
                cupertino.CupertinoDialogAction(
                  isDefaultAction: true,
                  onPressed: () => Navigator.pop(routeContext, 'ok'),
                  child: const Text('OK'),
                ),
              ]
            : <Widget>[
                TextButton(
                  onPressed: () => Navigator.pop(routeContext, 'cancel'),
                  child: const Text('CANCEL'),
                ),
                TextButton(
                  onPressed: () => Navigator.pop(routeContext, 'ok'),
                  child: const Text('OK'),
                ),
              ],
      ),
    );
    if (mounted) setState(() => _lastResult = result ?? 'dismissed');
  }
}
