import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoSearchTextFieldDemoPage extends StatefulWidget {
  const CupertinoSearchTextFieldDemoPage({super.key});

  @override
  State<CupertinoSearchTextFieldDemoPage> createState() =>
      _CupertinoSearchTextFieldDemoPageState();
}

class _CupertinoSearchTextFieldDemoPageState
    extends State<CupertinoSearchTextFieldDemoPage> {
  final TextEditingController _controller = TextEditingController();
  String _query = '';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino search text field',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Localized placeholder, live clear button, custom icon treatment, '
          'and disabled state.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        CupertinoSearchTextField(
          controller: _controller,
          onChanged: (String value) => setState(() => _query = value),
        ),
        Text(
          _query.isEmpty ? 'No query' : 'Query: $_query',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
        const CupertinoSearchTextField(
          placeholder: 'Always-visible action',
          itemColor: CupertinoColors.systemBlue,
          itemSize: 24,
          suffixMode: OverlayVisibilityMode.always,
        ),
        const CupertinoSearchTextField(
          placeholder: 'Disabled search',
          enabled: false,
        ),
      ],
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }
}
