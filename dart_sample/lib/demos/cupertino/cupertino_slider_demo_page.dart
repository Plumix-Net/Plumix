import 'package:cupertino_ui/cupertino_ui.dart';

class CupertinoSliderDemoPage extends StatefulWidget {
  const CupertinoSliderDemoPage({super.key});

  @override
  State<CupertinoSliderDemoPage> createState() =>
      _CupertinoSliderDemoPageState();
}

class _CupertinoSliderDemoPageState extends State<CupertinoSliderDemoPage> {
  double _value = 0.35;
  double _rangedValue = 30;
  double _discreteValue = 2;
  bool _enabled = true;
  bool _dark = false;
  bool _rightToLeft = false;
  int _changes = 0;
  String _lifecycle = 'idle';

  @override
  Widget build(BuildContext context) {
    return CupertinoTheme(
      data: CupertinoThemeData(
        brightness: _dark ? Brightness.dark : Brightness.light,
      ),
      child: Directionality(
        textDirection: _rightToLeft ? TextDirection.rtl : TextDirection.ltr,
        child: Container(
          color: _dark ? const Color(0xFF1C1C1E) : CupertinoColors.white,
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 12,
            children: <Widget>[
              Text(
                'CupertinoSlider',
                style: TextStyle(fontSize: 20, color: _titleColor),
              ),
              Text(
                'Continuous and discrete values, min/max ranges, thumb and active '
                'colors, disabled state, and LTR/RTL dragging.',
                style: TextStyle(fontSize: 14, color: _subtitleColor),
              ),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: <Widget>[
                  _buildControl(_enabled ? 'Enabled' : 'Disabled', () {
                    _enabled = !_enabled;
                  }),
                  _buildControl(_dark ? 'Dark' : 'Light', () {
                    _dark = !_dark;
                  }),
                  _buildControl(_rightToLeft ? 'RTL' : 'LTR', () {
                    _rightToLeft = !_rightToLeft;
                  }),
                  _buildControl('Reset', _reset),
                ],
              ),
              Text(
                'value=${_format(_value)}, ranged=${_format(_rangedValue)}, '
                'discrete=${_format(_discreteValue)}, changes=$_changes, '
                'lifecycle=$_lifecycle',
                style: TextStyle(fontSize: 12, color: _subtitleColor),
              ),
              _buildRow(
                CupertinoSlider(
                  value: _value,
                  onChanged: _enabled ? _onValueChanged : null,
                  onChangeStart: (double value) {
                    setState(() {
                      _lifecycle = 'dragging';
                    });
                  },
                  onChangeEnd: (double value) {
                    setState(() {
                      _lifecycle = 'idle';
                    });
                  },
                ),
                'Continuous',
                '0.0 to 1.0, theme primary color',
              ),
              _buildRow(
                CupertinoSlider(
                  value: _rangedValue,
                  onChanged: _enabled ? _onRangedChanged : null,
                  min: 10,
                  max: 90,
                  activeColor: CupertinoColors.systemGreen,
                ),
                'Ranged',
                'min 10, max 90, activeColor override',
              ),
              _buildRow(
                CupertinoSlider(
                  value: _discreteValue,
                  onChanged: _enabled ? _onDiscreteChanged : null,
                  min: 0,
                  max: 5,
                  divisions: 5,
                  activeColor: CupertinoColors.systemPurple,
                  thumbColor: CupertinoColors.systemYellow,
                ),
                'Discrete',
                '5 divisions, animated track, custom thumb',
              ),
            ],
          ),
        ),
      ),
    );
  }

  Color get _titleColor =>
      _dark ? CupertinoColors.white : CupertinoColors.black;

  Color get _subtitleColor =>
      _dark ? const Color(0x99FFFFFF) : const Color(0x8A000000);

  Widget _buildRow(Widget slider, String title, String subtitle) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: _dark ? const Color(0xFF2C2C2E) : const Color(0xFFF1F4F9),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: _dark ? const Color(0xFF3A3A3C) : const Color(0xFFD6DEEA),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 6,
        children: <Widget>[
          Text(title, style: TextStyle(fontSize: 13, color: _titleColor)),
          Text(subtitle, style: TextStyle(fontSize: 12, color: _subtitleColor)),
          Align(alignment: Alignment.centerLeft, child: slider),
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
        style: TextStyle(fontSize: 12, color: CupertinoColors.activeBlue),
      ),
    );
  }

  static String _format(double value) => value.toStringAsFixed(2);

  void _reset() {
    _value = 0.35;
    _rangedValue = 30;
    _discreteValue = 2;
    _changes = 0;
    _lifecycle = 'idle';
  }

  void _onValueChanged(double value) {
    setState(() {
      _value = value;
      _changes += 1;
    });
  }

  void _onRangedChanged(double value) {
    setState(() {
      _rangedValue = value;
      _changes += 1;
    });
  }

  void _onDiscreteChanged(double value) {
    setState(() {
      _discreteValue = value;
      _changes += 1;
    });
  }
}
