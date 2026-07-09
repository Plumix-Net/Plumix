import 'package:flutter/material.dart';

class AutocompleteDemoPage extends StatefulWidget {
  const AutocompleteDemoPage({super.key});

  @override
  State<AutocompleteDemoPage> createState() => _AutocompleteDemoPageState();
}

class _AutocompleteDemoPageState extends State<AutocompleteDemoPage> {
  static const List<String> _frameworkTerms = <String>[
    'Widget',
    'Element',
    'RenderObject',
    'BuildContext',
    'StatefulWidget',
    'InheritedWidget',
    'Navigator',
    'Autocomplete',
    'RawAutocomplete',
  ];

  final TextEditingController _materialController = TextEditingController();
  final FocusNode _materialFocusNode = FocusNode();
  final TextEditingController _rawController = TextEditingController();
  final FocusNode _rawFocusNode = FocusNode();
  OptionsViewOpenDirection _openDirection = OptionsViewOpenDirection.down;
  String _materialSelection = 'none';
  String _rawSelection = 'none';

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text(
            'Autocomplete + RawAutocomplete',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Material defaults and a custom raw field/options view with shared filtering, keyboard highlighting, and anchored direction probes.',
            style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              onPressed: () => setState(() {
                _openDirection = _nextDirection(_openDirection);
              }),
              child: Text('Open: ${_formatDirection(_openDirection)}'),
            ),
          ),
          const Text(
            'Material Autocomplete',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          Autocomplete<String>(
            optionsBuilder: _filterTerms,
            textEditingController: _materialController,
            focusNode: _materialFocusNode,
            optionsViewOpenDirection: _openDirection,
            optionsMaxHeight: 160,
            onSelected: (String value) {
              setState(() => _materialSelection = value);
            },
          ),
          Text(
            'Selected: $_materialSelection',
            style: const TextStyle(fontSize: 13),
          ),
          const Divider(),
          const Text(
            'RawAutocomplete',
            style: TextStyle(fontSize: 18, color: Colors.black),
          ),
          RawAutocomplete<String>(
            textEditingController: _rawController,
            focusNode: _rawFocusNode,
            optionsBuilder: _filterTerms,
            optionsViewOpenDirection: _openDirection,
            displayStringForOption: (String value) => value,
            fieldViewBuilder:
                (
                  BuildContext context,
                  TextEditingController controller,
                  FocusNode focusNode,
                  VoidCallback onSubmitted,
                ) {
                  return TextField(
                    controller: controller,
                    focusNode: focusNode,
                    decoration: const InputDecoration(
                      labelText: 'Framework concept',
                      hintText: 'Type to filter',
                    ),
                    onSubmitted: (String value) => onSubmitted(),
                  );
                },
            optionsViewBuilder:
                (
                  BuildContext context,
                  AutocompleteOnSelected<String> onSelected,
                  Iterable<String> options,
                ) {
                  final int highlightedIndex = AutocompleteHighlightedOption.of(
                    context,
                  );
                  final List<String> materialized = options.toList();
                  return Material(
                    elevation: 4,
                    borderRadius: BorderRadius.circular(12),
                    clipBehavior: Clip.antiAlias,
                    child: ListView(
                      padding: EdgeInsets.zero,
                      shrinkWrap: true,
                      children: <Widget>[
                        for (
                          int index = 0;
                          index < materialized.length;
                          index += 1
                        )
                          InkWell(
                            onTap: () => onSelected(materialized[index]),
                            child: Container(
                              color: index == highlightedIndex
                                  ? Theme.of(context).focusColor
                                  : null,
                              padding: const EdgeInsets.symmetric(
                                horizontal: 16,
                                vertical: 12,
                              ),
                              child: Text(materialized[index]),
                            ),
                          ),
                      ],
                    ),
                  );
                },
            onSelected: (String value) {
              setState(() => _rawSelection = value);
            },
          ),
          Text(
            'Selected: $_rawSelection',
            style: const TextStyle(fontSize: 13),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _materialController.dispose();
    _materialFocusNode.dispose();
    _rawController.dispose();
    _rawFocusNode.dispose();
    super.dispose();
  }

  Iterable<String> _filterTerms(TextEditingValue value) {
    final String query = value.text.trim().toLowerCase();
    return _frameworkTerms.where(
      (String term) => query.isEmpty || term.toLowerCase().contains(query),
    );
  }

  static OptionsViewOpenDirection _nextDirection(
    OptionsViewOpenDirection value,
  ) {
    return switch (value) {
      OptionsViewOpenDirection.down => OptionsViewOpenDirection.up,
      OptionsViewOpenDirection.up => OptionsViewOpenDirection.mostSpace,
      OptionsViewOpenDirection.mostSpace => OptionsViewOpenDirection.down,
    };
  }

  static String _formatDirection(OptionsViewOpenDirection value) {
    return switch (value) {
      OptionsViewOpenDirection.down => 'down',
      OptionsViewOpenDirection.up => 'up',
      OptionsViewOpenDirection.mostSpace => 'mostSpace',
    };
  }
}
