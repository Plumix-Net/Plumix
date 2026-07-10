import 'package:flutter/material.dart';

class SearchDemoPage extends StatefulWidget {
  const SearchDemoPage({super.key});

  @override
  State<SearchDemoPage> createState() => _SearchDemoPageState();
}

class _SearchDemoPageState extends State<SearchDemoPage> {
  static const List<String> _searchTerms = <String>[
    'Widget',
    'Element',
    'RenderObject',
    'SearchBar',
    'SearchAnchor',
    'Navigator',
    'Material',
    'TextField',
    'ThemeData',
    'Plumix',
  ];

  final SearchController _controller = SearchController();
  final TextEditingController _standaloneController = TextEditingController(
    text: 'Standalone',
  );
  bool _enabled = true;
  bool _useFullScreen = false;
  bool _useThemeOverrides = false;
  String _selected = 'none';
  String _status = 'idle';

  @override
  Widget build(BuildContext context) {
    final ThemeData baseTheme = Theme.of(context);
    final ThemeData pageTheme = baseTheme.copyWith(
      searchBarTheme: _useThemeOverrides
          ? SearchBarThemeData(
              backgroundColor: const WidgetStatePropertyAll<Color?>(
                Color(0xFFEAF6F7),
              ),
              elevation: const WidgetStatePropertyAll<double?>(1),
              shape: WidgetStatePropertyAll<OutlinedBorder?>(
                RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(18),
                  side: const BorderSide(color: Color(0xFF00695C)),
                ),
              ),
              padding: const WidgetStatePropertyAll<EdgeInsetsGeometry?>(
                EdgeInsets.symmetric(horizontal: 12),
              ),
              constraints: const BoxConstraints(
                minWidth: 280,
                maxWidth: 520,
                minHeight: 52,
              ),
              hintStyle: const WidgetStatePropertyAll<TextStyle?>(
                TextStyle(color: Color(0xFF00695C)),
              ),
            )
          : const SearchBarThemeData(),
      searchViewTheme: _useThemeOverrides
          ? SearchViewThemeData(
              backgroundColor: const Color(0xFFF5FBFA),
              elevation: 0,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(22),
                side: const BorderSide(color: Color(0xFF80CBC4)),
              ),
              headerHeight: 64,
              barPadding: const EdgeInsets.symmetric(horizontal: 12),
              dividerColor: const Color(0xFF80CBC4),
              constraints: const BoxConstraints(
                minWidth: 360,
                minHeight: 260,
                maxWidth: 560,
                maxHeight: 420,
              ),
              padding: const EdgeInsets.all(16),
            )
          : const SearchViewThemeData(),
    );

    return Theme(
      data: pageTheme,
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            const Text(
              'SearchBar + SearchAnchor + SearchDelegate',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Controller-backed search view with suggestions, open/close callbacks, M3 defaults, and theme precedence probes.',
              style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _controlButton(
                  _enabled ? 'Enabled' : 'Disabled',
                  () => setState(() => _enabled = !_enabled),
                ),
                _controlButton(
                  _useFullScreen ? 'Full screen' : 'Docked view',
                  () => setState(() => _useFullScreen = !_useFullScreen),
                ),
                _controlButton(
                  _useThemeOverrides ? 'Theme on' : 'Theme off',
                  () =>
                      setState(() => _useThemeOverrides = !_useThemeOverrides),
                ),
                _controlButton(
                  'Legacy route',
                  () => _openLegacySearch(context),
                ),
              ],
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _controlButton('Open', () => _controller.openView()),
                _controlButton(
                  'Clear',
                  () => setState(() {
                    _controller.clear();
                    _selected = 'none';
                    _status = 'cleared';
                  }),
                ),
              ],
            ),
            Align(
              alignment: Alignment.centerLeft,
              child: SearchAnchor.bar(
                searchController: _controller,
                barHintText: 'Search framework terms',
                viewHintText: 'Type a Plumix concept',
                isFullScreen: _useFullScreen,
                enabled: _enabled,
                shrinkWrap: true,
                constraints: const BoxConstraints(
                  minWidth: 320,
                  maxWidth: 560,
                  minHeight: 56,
                ),
                viewConstraints: const BoxConstraints(
                  minWidth: 360,
                  minHeight: 260,
                  maxWidth: 560,
                  maxHeight: 420,
                ),
                barTrailing: <Widget>[
                  IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: () => setState(() {
                      _controller.clear();
                      _status = 'bar cleared';
                    }),
                  ),
                ],
                onOpen: () => setState(() => _status = 'opened'),
                onClose: () => setState(() => _status = 'closed'),
                onChanged: (String value) =>
                    setState(() => _status = 'changed: ${_formatEmpty(value)}'),
                onSubmitted: (String value) => setState(
                  () => _status = 'submitted: ${_formatEmpty(value)}',
                ),
                suggestionsBuilder: _buildSuggestions,
              ),
            ),
            Text('Selected: $_selected', style: const TextStyle(fontSize: 13)),
            Text(
              'Controller text: ${_formatEmpty(_controller.text)}',
              style: const TextStyle(fontSize: 13),
            ),
            Text('Status: $_status', style: const TextStyle(fontSize: 13)),
            const Divider(),
            const Text(
              'Standalone SearchBar',
              style: TextStyle(fontSize: 18, color: Colors.black),
            ),
            SearchBar(
              controller: _standaloneController,
              hintText: 'Filter inside a page',
              leading: const Icon(Icons.search),
              trailing: <Widget>[
                IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () => setState(_standaloneController.clear),
                ),
              ],
              onTap: () => setState(() => _status = 'standalone tapped'),
              onChanged: (String value) => setState(
                () => _status = 'standalone: ${_formatEmpty(value)}',
              ),
              onSubmitted: (String value) => setState(
                () => _status = 'standalone submitted: ${_formatEmpty(value)}',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Iterable<Widget> _buildSuggestions(
    BuildContext context,
    SearchController controller,
  ) {
    final String query = controller.text.trim();
    final List<Widget> suggestions = <Widget>[];
    for (final String term in _searchTerms) {
      if (query.isNotEmpty &&
          !term.toLowerCase().contains(query.toLowerCase())) {
        continue;
      }

      suggestions.add(
        ListTile(
          leading: const Icon(Icons.search),
          title: Text(term),
          subtitle: Text('Select $term'),
          onTap: () => setState(() {
            _selected = term;
            _status = 'selected: $term';
            controller.closeView(term);
          }),
        ),
      );
    }

    if (suggestions.isEmpty) {
      suggestions.add(
        Padding(
          padding: const EdgeInsets.all(16),
          child: Text(
            'No results for $query',
            style: const TextStyle(fontSize: 13, color: Colors.grey),
          ),
        ),
      );
    }

    return suggestions;
  }

  @override
  void dispose() {
    _controller.dispose();
    _standaloneController.dispose();
    super.dispose();
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(onPressed: onPressed, child: Text(label));
  }

  static String _formatEmpty(String value) {
    return value.isEmpty ? 'empty' : value;
  }

  Future<void> _openLegacySearch(BuildContext context) async {
    final String? result = await showSearch<String>(
      context: context,
      query: _controller.text,
      delegate: _TermSearchDelegate(_searchTerms, (String value) {
        setState(() {
          _selected = value;
          _status = 'legacy selected: $value';
        });
      }),
    );
    if (result != null && mounted) {
      setState(() => _status = 'legacy closed: $result');
    }
  }
}

class _TermSearchDelegate extends SearchDelegate<String> {
  _TermSearchDelegate(this._terms, this._onSelected)
    : super(searchFieldLabel: 'Search framework terms');

  final List<String> _terms;
  final ValueChanged<String> _onSelected;

  @override
  List<Widget>? buildActions(BuildContext context) {
    return <Widget>[
      IconButton(
        icon: const Icon(Icons.clear),
        tooltip: MaterialLocalizations.of(context).clearButtonTooltip,
        onPressed: () {
          query = '';
          showSuggestions(context);
        },
      ),
    ];
  }

  @override
  Widget? buildLeading(BuildContext context) {
    return BackButton(onPressed: () => close(context, null));
  }

  @override
  Widget buildResults(BuildContext context) => _buildTerms(context, 'Results');

  @override
  Widget buildSuggestions(BuildContext context) =>
      _buildTerms(context, 'Suggestions');

  Widget _buildTerms(BuildContext context, String label) {
    final String normalizedQuery = query.trim();
    return ListView(
      children: <Widget>[
        Text(
          '$label for ${_SearchDemoPageState._formatEmpty(normalizedQuery)}',
        ),
        for (final String term in _terms)
          if (normalizedQuery.isEmpty ||
              term.toLowerCase().contains(normalizedQuery.toLowerCase()))
            ListTile(
              leading: const Icon(Icons.search),
              title: Text(term),
              onTap: () {
                if (label == 'Suggestions') {
                  query = term;
                  showResults(context);
                } else {
                  _onSelected(term);
                  close(context, term);
                }
              },
            ),
      ],
    );
  }
}
