import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoTextFieldDemoPage extends StatefulWidget {
  const CupertinoTextFieldDemoPage({super.key});

  @override
  State<CupertinoTextFieldDemoPage> createState() =>
      _CupertinoTextFieldDemoPageState();
}

class _CupertinoTextFieldDemoPageState
    extends State<CupertinoTextFieldDemoPage> {
  final TextEditingController _controller = TextEditingController();
  String _value = '';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Cupertino text fields',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Rounded, borderless, form validation, multiline, disabled, '
          'attachment, and clear-button states.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        CupertinoTextField(
          controller: _controller,
          placeholder: 'Search settings',
          prefix: const Padding(
            padding: EdgeInsets.symmetric(horizontal: 6),
            child: Icon(CupertinoIcons.search, size: 18),
          ),
          clearButtonMode: OverlayVisibilityMode.editing,
          onChanged: (String value) => setState(() => _value = value),
        ),
        Text(
          _value.isEmpty ? 'No query' : 'Query: $_value',
          style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
        ),
        const CupertinoTextField.borderless(
          placeholder: 'Borderless multiline note',
          minLines: 2,
          maxLines: 4,
          decoration: BoxDecoration(
            color: Color(0xFFF2F2F7),
            borderRadius: BorderRadius.all(Radius.circular(8)),
          ),
        ),
        Form(
          child: CupertinoTextFormFieldRow(
            prefix: const Text('Email'),
            placeholder: 'name@example.com',
            keyboardType: TextInputType.emailAddress,
            autovalidateMode: AutovalidateMode.onUserInteraction,
            validator: (String? value) =>
                value?.contains('@') ?? false ? null : 'Enter a valid email',
          ),
        ),
        const CupertinoTextField(enabled: false, placeholder: 'Disabled field'),
      ],
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }
}
