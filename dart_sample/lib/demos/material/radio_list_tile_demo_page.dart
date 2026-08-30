import 'package:material_ui/material_ui.dart';

class RadioListTileDemoPage extends StatefulWidget {
  const RadioListTileDemoPage({super.key});

  @override
  State<RadioListTileDemoPage> createState() => _RadioListTileDemoPageState();
}

class _RadioListTileDemoPageState extends State<RadioListTileDemoPage> {
  String? _radioValue = 'standard';
  bool _enabled = true;
  bool _adaptive = false;
  bool _toggleable = true;
  bool _scaled = false;
  ListTileControlAffinity _affinity = ListTileControlAffinity.platform;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'RadioListTile',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          "RadioGroup selection, toggleable clearing, radio scaling, and the affinity rule that puts the radio first on 'platform' — unlike the checkbox and switch tiles.",
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              _enabled ? 'Enabled' : 'Disabled',
              () => setState(() => _enabled = !_enabled),
              104,
              const Color(0xFFE9F0FF),
            ),
            _buildControlButton(
              _affinity == ListTileControlAffinity.trailing
                  ? 'Trailing'
                  : 'Platform',
              () => setState(() {
                _affinity = _affinity == ListTileControlAffinity.trailing
                    ? ListTileControlAffinity.platform
                    : ListTileControlAffinity.trailing;
              }),
              104,
              const Color(0xFFE9F7EF),
            ),
            _buildControlButton(
              _adaptive ? 'Adaptive' : 'Material',
              () => setState(() => _adaptive = !_adaptive),
              104,
              const Color(0xFFF8EFE2),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildControlButton(
              _toggleable ? 'Toggleable' : 'Sticky',
              () => setState(() => _toggleable = !_toggleable),
              104,
              const Color(0xFFF3E5F5),
            ),
            _buildControlButton(
              _scaled ? 'Scale 1.5x' : 'Scale 1.0x',
              () => setState(() => _scaled = !_scaled),
              104,
              const Color(0xFFE0F2F1),
            ),
          ],
        ),
        Text(
          'radio=$_radioValue, affinity=${_affinity.name}, toggleable=$_toggleable, adaptive=$_adaptive',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: ColoredBox(
            color: const Color(0xFFF7F9FC),
            child: RadioGroup<String>(
              groupValue: _radioValue,
              onChanged: (String? value) => setState(() => _radioValue = value),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  _buildRadioTile(
                    'standard',
                    'Standard sync',
                    'Every change, on any network.',
                  ),
                  _buildRadioTile(
                    'metered',
                    'Metered sync',
                    'Only on unmetered connections.',
                  ),
                  _buildRadioTile(
                    'manual',
                    'Manual sync',
                    'Nothing until you ask for it.',
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRadioTile(String value, String title, String subtitle) {
    if (_adaptive) {
      return RadioListTile<String>.adaptive(
        value: value,
        toggleable: _toggleable,
        enabled: _enabled,
        controlAffinity: _affinity,
        title: Text(title),
        subtitle: Text(subtitle),
        secondary: const Icon(Icons.done),
        radioScaleFactor: _scaled ? 1.5 : 1.0,
        selected: _radioValue == value,
      );
    }
    return RadioListTile<String>(
      value: value,
      toggleable: _toggleable,
      enabled: _enabled,
      controlAffinity: _affinity,
      title: Text(title),
      subtitle: Text(subtitle),
      secondary: const Icon(Icons.done),
      radioScaleFactor: _scaled ? 1.5 : 1.0,
      selected: _radioValue == value,
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
