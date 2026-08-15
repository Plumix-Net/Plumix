import 'package:material_ui/material_ui.dart';

class SnackBarDemoPage extends StatefulWidget {
  const SnackBarDemoPage({super.key});

  @override
  State<SnackBarDemoPage> createState() => _SnackBarDemoPageState();
}

class _SnackBarDemoPageState extends State<SnackBarDemoPage> {
  bool _floating = false;
  bool _showAction = true;
  bool _showCloseIcon = false;
  bool _customColors = false;
  bool _swipeUp = false;
  bool _fixedWidth = false;
  int _actionCount = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 14,
      children: <Widget>[
        const Text('SnackBar + SnackBarAction', style: TextStyle(fontSize: 20)),
        const Text(
          'ScaffoldMessenger queue, fixed/floating placement, one-shot action, close icon, overflow, and theme/widget precedence.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _floating = !_floating),
              child: Text(_floating ? 'Floating' : 'Fixed'),
            ),
            TextButton(
              onPressed: () => setState(() => _showAction = !_showAction),
              child: Text(_showAction ? 'Action on' : 'Action off'),
            ),
            TextButton(
              onPressed: () => setState(() => _showCloseIcon = !_showCloseIcon),
              child: Text(_showCloseIcon ? 'Close on' : 'Close off'),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _customColors = !_customColors),
              child: Text(_customColors ? 'Colors on' : 'Colors off'),
            ),
            TextButton(
              onPressed: () => setState(() => _swipeUp = !_swipeUp),
              child: Text(_swipeUp ? 'Swipe up' : 'Swipe down'),
            ),
            TextButton(
              onPressed: () => setState(() => _fixedWidth = !_fixedWidth),
              child: Text(_fixedWidth ? 'Width 320' : 'Full width'),
            ),
          ],
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            ElevatedButton(
              onPressed: () => _showSnackBar(context),
              child: const Text('SHOW SNACKBAR'),
            ),
          ],
        ),
        Text(
          'Action presses: $_actionCount',
          style: const TextStyle(fontSize: 13),
        ),
        const Text(
          'Narrow the window to move a wide action below the message; repeated SHOW calls '
          'show queue order. Drag the bar in the swipe direction to dismiss it; the width '
          'probe needs floating behavior.',
          style: TextStyle(fontSize: 13, color: Colors.black54),
        ),
      ],
    );
  }

  void _showSnackBar(BuildContext context) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Text('A queued Material snackbar from Plumix.'),
        behavior: _floating
            ? SnackBarBehavior.floating
            : SnackBarBehavior.fixed,
        backgroundColor: _customColors ? const Color(0xFF004D40) : null,
        closeIconColor: _customColors ? const Color(0xFFFFD54F) : null,
        showCloseIcon: _showCloseIcon,
        persist: _showAction || _showCloseIcon,
        dismissDirection: _swipeUp
            ? DismissDirection.up
            : DismissDirection.down,
        width: _floating && _fixedWidth ? 320 : null,
        action: _showAction
            ? SnackBarAction(
                label: 'UNDO LONG ACTION',
                textColor: _customColors ? const Color(0xFFFFD54F) : null,
                onPressed: () => setState(() => _actionCount++),
              )
            : null,
      ),
    );
  }
}
