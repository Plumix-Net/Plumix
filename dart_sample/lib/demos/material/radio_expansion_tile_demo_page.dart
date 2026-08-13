import 'package:material_ui/material_ui.dart';

class RadioExpansionTileDemoPage extends StatefulWidget {
  const RadioExpansionTileDemoPage({super.key});

  @override
  State<RadioExpansionTileDemoPage> createState() =>
      _RadioExpansionTileDemoPageState();
}

class _RadioExpansionTileDemoPageState
    extends State<RadioExpansionTileDemoPage> {
  final ExpansibleController _expansionController = ExpansibleController();
  String? _selectedSchedule = 'daily';
  bool _toggleable = false;
  bool _adaptive = false;
  bool _maintainState = false;
  bool _useMaterial3 = true;
  bool _expanded = false;
  ListTileControlAffinity _affinity = ListTileControlAffinity.leading;

  @override
  void dispose() {
    _expansionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'RadioListTile + ExpansionTile',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'RadioGroup selection and controller-driven expansion with animated arrow/body/theme transitions.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              _toggleable ? 'Toggleable' : 'Single select',
              () => setState(() => _toggleable = !_toggleable),
              116,
              const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              _adaptive ? 'Adaptive' : 'Material',
              () => setState(() => _adaptive = !_adaptive),
              104,
              const Color(0xFFE9F7EF),
            ),
            _buildControlButton(
              _affinity == ListTileControlAffinity.leading
                  ? 'Leading'
                  : 'Trailing',
              () => setState(() {
                _affinity = _affinity == ListTileControlAffinity.leading
                    ? ListTileControlAffinity.trailing
                    : ListTileControlAffinity.leading;
              }),
              104,
              const Color(0xFFF8EFE2),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              _expanded ? 'Collapse' : 'Expand',
              _expansionController.toggle,
              100,
              const Color(0xFFF0E8FF),
            ),
            _buildControlButton(
              _maintainState ? 'Maintain on' : 'Maintain off',
              () => setState(() => _maintainState = !_maintainState),
              112,
              const Color(0xFFEAF6F7),
            ),
            _buildControlButton(
              _useMaterial3 ? 'Material 3' : 'Material 2',
              () => setState(() => _useMaterial3 = !_useMaterial3),
              104,
              const Color(0xFFFFF2CC),
            ),
          ],
        ),
        Text(
          'selected=$_selectedSchedule, expanded=$_expanded, '
          'affinity=${_affinity.name}, adaptive=$_adaptive, '
          'material3=$_useMaterial3',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F9FC),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                _buildRadioGroup(),
                Theme(
                  data: _buildControlTheme(),
                  child: ExpansionTile(
                    title: const Text('Advanced schedule options'),
                    subtitle: const Text(
                      'Tap row or use the controller button.',
                    ),
                    leading: const Icon(Icons.info_outline),
                    controller: _expansionController,
                    controlAffinity: _affinity,
                    maintainState: _maintainState,
                    backgroundColor: const Color(0xFFF0E8FF),
                    collapsedBackgroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    collapsedShape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(4),
                    ),
                    onExpansionChanged: (bool value) =>
                        setState(() => _expanded = value),
                    childrenPadding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
                    children: const <Widget>[
                      Text(
                        'Sync only while charging',
                        style: TextStyle(fontSize: 13),
                      ),
                      Text(
                        'Retry window: 15 minutes',
                        style: TextStyle(fontSize: 13),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRadioGroup() {
    return Theme(
      data: _buildControlTheme(),
      child: RadioGroup<String>(
        groupValue: _selectedSchedule,
        onChanged: (String? value) => setState(() => _selectedSchedule = value),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            _buildRadioTile('daily', 'Daily', Icons.star),
            _buildRadioTile('weekly', 'Weekly', Icons.star_outline),
            _buildRadioTile(
              'paused',
              'Paused (disabled)',
              Icons.info_outline,
              enabled: false,
            ),
          ],
        ),
      ),
    );
  }

  ThemeData _buildControlTheme() {
    final ThemeData ambient = Theme.of(context);
    final ColorScheme controlScheme = ambient.colorScheme.copyWith(
      primary: const Color(0xFF6750A4),
      secondary: const Color(0xFF006C4C),
      onSurface: const Color(0xFF1D1B20),
      onSurfaceVariant: const Color(0xFF49454F),
    );
    return ThemeData.from(
      colorScheme: controlScheme,
      textTheme: ambient.textTheme,
      useMaterial3: _useMaterial3,
    );
  }

  Widget _buildRadioTile(
    String value,
    String label,
    IconData icon, {
    bool? enabled,
  }) {
    if (_adaptive) {
      return RadioListTile<String>.adaptive(
        value: value,
        toggleable: _toggleable,
        title: Text(label),
        secondary: Icon(icon),
        selected: _selectedSchedule == value,
        enabled: enabled,
        controlAffinity: _affinity,
        useCupertinoCheckmarkStyle: true,
      );
    }
    return RadioListTile<String>(
      value: value,
      toggleable: _toggleable,
      title: Text(label),
      secondary: Icon(icon),
      selected: _selectedSchedule == value,
      enabled: enabled,
      controlAffinity: _affinity,
      selectedTileColor: const Color(0xFFE8DEF8),
    );
  }

  Widget _buildControlButton(
    String label,
    VoidCallback onPressed,
    double width,
    Color background,
  ) {
    return SizedBox(
      width: width,
      child: TextButton(
        onPressed: onPressed,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 36),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}
