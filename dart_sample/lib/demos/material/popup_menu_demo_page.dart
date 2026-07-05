import 'package:flutter/material.dart';

class PopupMenuDemoPage extends StatefulWidget {
  const PopupMenuDemoPage({super.key});

  @override
  State<PopupMenuDemoPage> createState() => _PopupMenuDemoPageState();
}

class _PopupMenuDemoPageState extends State<PopupMenuDemoPage> {
  bool _enabled = true;
  bool _under = false;
  bool _useTheme = false;
  String _selected = 'copy';
  String _status = 'idle';

  @override
  Widget build(BuildContext context) {
    final PopupMenuThemeData popupTheme = _useTheme
        ? PopupMenuThemeData(
            color: Colors.orange.shade50,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            menuPadding: const EdgeInsets.all(4),
            iconColor: Colors.orange.shade900,
            labelTextStyle: WidgetStateProperty.resolveWith<TextStyle?>((
              Set<WidgetState> states,
            ) {
              return Theme.of(context).textTheme.labelLarge?.copyWith(
                color: states.contains(WidgetState.disabled)
                    ? Colors.grey.shade600.withValues(alpha: 0.38)
                    : Colors.orange.shade900,
              );
            }),
          )
        : const PopupMenuThemeData();
    return PopupMenuTheme(
      data: popupTheme,
      child: Builder(builder: _buildContent),
    );
  }

  Widget _buildContent(BuildContext context) {
    final PopupMenuPosition position = _under
        ? PopupMenuPosition.under
        : PopupMenuPosition.over;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 14,
      children: <Widget>[
        const Text(
          'PopupMenuButton + PopupMenuItem',
          style: TextStyle(fontSize: 20),
        ),
        const Text(
          'Anchored menu route with selection, cancellation, disabled items, keyboard navigation, and theme precedence.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () => setState(() => _enabled = !_enabled),
              child: Text(_enabled ? 'Enabled' : 'Disabled'),
            ),
            TextButton(
              onPressed: () => setState(() => _under = !_under),
              child: Text(_under ? 'Under' : 'Over'),
            ),
            TextButton(
              onPressed: () => setState(() => _useTheme = !_useTheme),
              child: Text(_useTheme ? 'Theme on' : 'Theme off'),
            ),
          ],
        ),
        Wrap(
          spacing: 16,
          runSpacing: 8,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: <Widget>[
            PopupMenuButton<String>(
              itemBuilder: _buildItems,
              initialValue: _selected,
              onOpened: () => setState(() => _status = 'opened'),
              onSelected: (String value) => setState(() {
                _selected = value;
                _status = 'selected: $value';
              }),
              onCanceled: () => setState(() => _status = 'canceled'),
              enabled: _enabled,
              position: position,
              child: const Padding(
                padding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                child: Text('CHILD MENU'),
              ),
            ),
            PopupMenuButton<String>(
              itemBuilder: _buildItems,
              initialValue: _selected,
              onSelected: (String value) => setState(() {
                _selected = value;
                _status = 'icon selected: $value';
              }),
              onCanceled: () => setState(() => _status = 'icon canceled'),
              enabled: _enabled,
              position: position,
              icon: const Icon(Icons.more_vert),
              tooltip: 'Show commands',
            ),
          ],
        ),
        Text('Selected: $_selected', style: const TextStyle(fontSize: 13)),
        Text('Status: $_status', style: const TextStyle(fontSize: 13)),
      ],
    );
  }

  List<PopupMenuEntry<String>> _buildItems(BuildContext context) {
    return const <PopupMenuEntry<String>>[
      PopupMenuItem<String>(value: 'copy', child: Text('Copy')),
      PopupMenuItem<String>(value: 'rename', child: Text('Rename')),
      PopupMenuItem<String>(
        value: 'archive',
        enabled: false,
        child: Text('Archive (disabled)'),
      ),
      PopupMenuItem<String>(value: 'delete', child: Text('Delete')),
    ];
  }
}
