import 'package:material_ui/material_ui.dart';

class SegmentedButtonsDemoPage extends StatefulWidget {
  const SegmentedButtonsDemoPage({super.key});

  @override
  State<SegmentedButtonsDemoPage> createState() =>
      _SegmentedButtonsDemoPageState();
}

class _SegmentedButtonsDemoPageState extends State<SegmentedButtonsDemoPage> {
  final List<bool> _toggleSelection = <bool>[true, false, false];
  Set<int> _segmentSelection = <int>{0};
  bool _multiSelection = false;
  bool _emptySelection = false;
  bool _vertical = false;
  bool _showSelectedIcon = true;
  bool _useThemeOverrides = false;
  bool _useWidgetStyle = false;
  bool _useStatefulFill = false;

  @override
  Widget build(BuildContext context) {
    final Color? toggleFill = _useStatefulFill
        ? WidgetStateColor.resolveWith(
            (Set<WidgetState> states) => states.contains(WidgetState.selected)
                ? Colors.teal
                : Colors.lightBlue,
          )
        : _useWidgetStyle
        ? Colors.deepPurple
        : null;
    final ThemeData ambientTheme = Theme.of(context);
    final ThemeData theme = ambientTheme.copyWith(
      toggleButtonsTheme: _useThemeOverrides
          ? const ToggleButtonsThemeData(
              color: Colors.blueGrey,
              selectedColor: Colors.white,
              fillColor: Colors.teal,
              borderColor: Colors.teal,
              selectedBorderColor: Colors.teal,
              borderRadius: BorderRadius.all(Radius.circular(12)),
            )
          : const ToggleButtonsThemeData(),
      segmentedButtonTheme: _useThemeOverrides
          ? SegmentedButtonThemeData(
              style: SegmentedButton.styleFrom(
                foregroundColor: Colors.blueGrey,
                selectedForegroundColor: Colors.white,
                selectedBackgroundColor: Colors.teal,
                side: const BorderSide(color: Colors.teal),
                shape: const StadiumBorder(),
              ),
              selectedIcon: const Icon(Icons.star),
            )
          : const SegmentedButtonThemeData(),
    );

    return Theme(
      data: theme,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'ToggleButtons + SegmentedButton',
            style: TextStyle(fontSize: 20),
          ),
          const Text(
            'Legacy bool-list toggles and Material 3 value-set segments with selection, orientation, themes, and widget styles.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              _controlButton(
                _multiSelection ? 'Multi' : 'Single',
                _toggleMultiSelection,
              ),
              _controlButton(
                _emptySelection ? 'Empty allowed' : 'Selection required',
                _toggleEmptySelection,
              ),
              _controlButton(
                _vertical ? 'Vertical' : 'Horizontal',
                () => setState(() => _vertical = !_vertical),
              ),
              _controlButton(
                _showSelectedIcon ? 'Check on' : 'Check off',
                () => setState(() => _showSelectedIcon = !_showSelectedIcon),
              ),
              _controlButton(
                _useThemeOverrides ? 'Theme on' : 'Theme off',
                () => setState(() => _useThemeOverrides = !_useThemeOverrides),
              ),
              _controlButton(
                _useWidgetStyle ? 'Widget style on' : 'Widget style off',
                () => setState(() => _useWidgetStyle = !_useWidgetStyle),
              ),
              _controlButton(
                _useStatefulFill ? 'State fill on' : 'State fill off',
                () => setState(() => _useStatefulFill = !_useStatefulFill),
              ),
            ],
          ),
          Text(
            'ToggleButtons selection: ${_toggleSelection.map((bool value) => value ? '1' : '0').join(',')}',
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: ToggleButtons(
              isSelected: _toggleSelection,
              onPressed: (int index) => setState(
                () => _toggleSelection[index] = !_toggleSelection[index],
              ),
              direction: _vertical ? Axis.vertical : Axis.horizontal,
              borderRadius: _useWidgetStyle
                  ? const BorderRadius.all(Radius.circular(20))
                  : null,
              selectedColor: _useWidgetStyle ? Colors.white : null,
              fillColor: toggleFill,
              children: const <Widget>[
                Icon(Icons.star_outline),
                Icon(Icons.info_outline),
                Icon(Icons.menu),
              ],
            ),
          ),
          Text(
            _segmentSelection.isEmpty
                ? 'Segmented selection: none'
                : 'Segmented selection: ${(_segmentSelection.toList()..sort()).join(',')}',
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: SegmentedButton<int>(
              selected: _segmentSelection,
              onSelectionChanged: (Set<int> selection) =>
                  setState(() => _segmentSelection = selection),
              multiSelectionEnabled: _multiSelection,
              emptySelectionAllowed: _emptySelection,
              direction: _vertical ? Axis.vertical : Axis.horizontal,
              showSelectedIcon: _showSelectedIcon,
              style: _useWidgetStyle
                  ? SegmentedButton.styleFrom(
                      selectedForegroundColor: Colors.white,
                      selectedBackgroundColor: Colors.deepPurple,
                      side: const BorderSide(color: Colors.deepPurple),
                      shape: const StadiumBorder(),
                    )
                  : null,
              segments: const <ButtonSegment<int>>[
                ButtonSegment<int>(
                  value: 0,
                  icon: Icon(Icons.star_outline),
                  label: Text('Favorites'),
                  tooltip: 'Favorites segment',
                ),
                ButtonSegment<int>(
                  value: 1,
                  icon: Icon(Icons.info_outline),
                  label: Text('Explore'),
                ),
                ButtonSegment<int>(
                  value: 2,
                  icon: Icon(Icons.menu),
                  label: Text('Disabled'),
                  enabled: false,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  void _toggleMultiSelection() {
    setState(() {
      _multiSelection = !_multiSelection;
      if (!_multiSelection && _segmentSelection.length > 1) {
        _segmentSelection = <int>{
          _segmentSelection.reduce((int a, int b) => a < b ? a : b),
        };
      }
    });
  }

  void _toggleEmptySelection() {
    setState(() {
      _emptySelection = !_emptySelection;
      if (!_emptySelection && _segmentSelection.isEmpty) {
        _segmentSelection = <int>{0};
      }
    });
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(onPressed: onPressed, child: Text(label));
  }
}
