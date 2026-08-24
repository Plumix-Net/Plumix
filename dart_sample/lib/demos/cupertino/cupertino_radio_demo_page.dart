import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoRadioDemoPage extends StatefulWidget {
  const CupertinoRadioDemoPage({super.key});

  @override
  State<CupertinoRadioDemoPage> createState() => _CupertinoRadioDemoPageState();
}

class _CupertinoRadioDemoPageState extends State<CupertinoRadioDemoPage> {
  String? _groupValue = 'lafayette';
  bool _enabled = true;
  bool _toggleable = false;
  bool _useCheckmarkStyle = false;
  bool _dark = false;
  int _changes = 0;

  @override
  Widget build(BuildContext context) {
    return CupertinoTheme(
      data: CupertinoThemeData(
        brightness: _dark ? Brightness.dark : Brightness.light,
      ),
      child: Container(
        color: _dark ? const Color(0xFF1C1C1E) : CupertinoColors.white,
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            Text(
              'CupertinoRadio',
              style: TextStyle(fontSize: 20, color: _titleColor),
            ),
            Text(
              'RadioGroup selection, toggleable deselection, checkmark style, '
              'disabled and dark-mode painting.',
              style: TextStyle(fontSize: 14, color: _subtitleColor),
            ),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: <Widget>[
                _buildControl(_enabled ? 'Enabled' : 'Disabled', () {
                  _enabled = !_enabled;
                }),
                _buildControl(_toggleable ? 'Toggleable' : 'No toggle', () {
                  _toggleable = !_toggleable;
                }),
                _buildControl(_useCheckmarkStyle ? 'Checkmark' : 'Dot', () {
                  _useCheckmarkStyle = !_useCheckmarkStyle;
                }),
                _buildControl(_dark ? 'Dark' : 'Light', () {
                  _dark = !_dark;
                }),
              ],
            ),
            Text(
              'value=${_groupValue ?? 'null'}, changes=$_changes',
              style: TextStyle(fontSize: 12, color: _subtitleColor),
            ),
            RadioGroup<String>(
              groupValue: _groupValue,
              onChanged: _onChanged,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                spacing: 8,
                children: <Widget>[
                  _buildRow('lafayette', 'Lafayette', 'default colors'),
                  _buildRow('jefferson', 'Jefferson', 'default colors'),
                  _buildRow(
                    'custom',
                    'Custom colors',
                    'activeColor + inactiveColor + fillColor',
                    activeColor: CupertinoColors.systemGreen,
                    inactiveColor: const Color(0xFFEFEFF4),
                    fillColor: CupertinoColors.systemYellow,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Color get _titleColor =>
      _dark ? CupertinoColors.white : CupertinoColors.black;

  Color get _subtitleColor =>
      _dark ? const Color(0x99FFFFFF) : const Color(0x8A000000);

  Widget _buildRow(
    String value,
    String title,
    String subtitle, {
    Color? activeColor,
    Color? inactiveColor,
    Color? fillColor,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: _dark ? const Color(0xFF2C2C2E) : const Color(0xFFF1F4F9),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: _dark ? const Color(0xFF3A3A3C) : const Color(0xFFD6DEEA),
          width: 1,
        ),
      ),
      child: Row(
        spacing: 10,
        children: <Widget>[
          CupertinoRadio<String>(
            value: value,
            enabled: _enabled,
            toggleable: _toggleable,
            useCheckmarkStyle: _useCheckmarkStyle,
            activeColor: activeColor,
            inactiveColor: inactiveColor,
            fillColor: fillColor,
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              spacing: 2,
              children: <Widget>[
                Text(
                  title,
                  style: TextStyle(fontSize: 13, color: _titleColor),
                ),
                Text(
                  subtitle,
                  style: TextStyle(fontSize: 12, color: _subtitleColor),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildControl(String label, VoidCallback onPressed) {
    return CupertinoButton(
      onPressed: () => setState(onPressed),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Text(
        label,
        style: const TextStyle(
          fontSize: 12,
          color: CupertinoColors.activeBlue,
        ),
      ),
    );
  }

  void _onChanged(String? value) {
    setState(() {
      _groupValue = value;
      _changes += 1;
    });
  }
}
