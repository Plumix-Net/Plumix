import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

class AutofillGroupDemoPage extends StatefulWidget {
  const AutofillGroupDemoPage({super.key});

  @override
  State<AutofillGroupDemoPage> createState() => _AutofillGroupDemoPageState();
}

class _AutofillGroupDemoPageState extends State<AutofillGroupDemoPage> {
  late final TextEditingController _usernameController;
  late final TextEditingController _passwordController;
  late final TextEditingController _emailController;
  AutofillContextAction _onDisposeAction = AutofillContextAction.commit;
  bool _emailAutofillEnabled = true;
  String _lastAction = '(none)';

  @override
  void initState() {
    super.initState();
    _usernameController = TextEditingController();
    _passwordController = TextEditingController();
    _emailController = TextEditingController();
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AutofillGroup',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Fields sharing the closest AutofillGroup are cross-referenced by the '
          'platform. Disposing the topmost group finishes the autofill context.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            SizedBox(
              width: 160,
              child: _MenuButton(
                label: _onDisposeAction == AutofillContextAction.commit
                    ? 'onDispose: commit'
                    : 'onDispose: cancel',
                onTap: () => setState(() {
                  _onDisposeAction =
                      _onDisposeAction == AutofillContextAction.commit
                      ? AutofillContextAction.cancel
                      : AutofillContextAction.commit;
                }),
                background: const Color(0xFFDCE3ED),
              ),
            ),
            SizedBox(
              width: 160,
              child: _MenuButton(
                label: _emailAutofillEnabled
                    ? 'Email: autofill on'
                    : 'Email: autofill off',
                onTap: () => setState(
                  () => _emailAutofillEnabled = !_emailAutofillEnabled,
                ),
                background: const Color(0xFFE9F5EC),
              ),
            ),
          ],
        ),
        SizedBox(
          width: 200,
          child: _MenuButton(
            label: 'finishAutofillContext',
            onTap: () {
              setState(() {
                TextInput.finishAutofillContext();
                _lastAction = 'finishAutofillContext(shouldSave: true)';
              });
            },
            background: const Color(0xFFF3E8D8),
          ),
        ),
        Text(
          'last action: $_lastAction',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        AutofillGroup(
          onDisposeAction: _onDisposeAction,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'Username',
                style: TextStyle(fontSize: 12, color: Colors.black54),
              ),
              _buildTextField(
                controller: _usernameController,
                placeholder: 'username',
                autofillHints: const <String>[AutofillHints.username],
              ),
              const Text(
                'Password',
                style: TextStyle(fontSize: 12, color: Colors.black54),
              ),
              _buildTextField(
                controller: _passwordController,
                placeholder: 'password',
                obscureText: true,
                autofillHints: const <String>[AutofillHints.password],
              ),
              const Text(
                'Email',
                style: TextStyle(fontSize: 12, color: Colors.black54),
              ),
              _buildTextField(
                controller: _emailController,
                placeholder: 'email',
                autofillHints: _emailAutofillEnabled
                    ? const <String>[AutofillHints.email]
                    : null,
              ),
              Builder(
                builder: (BuildContext inner) => Text(
                  'clients in group: '
                  '${AutofillGroup.of(inner).autofillClients.length}',
                  style: const TextStyle(fontSize: 12, color: Colors.black),
                ),
              ),
            ],
          ),
        ),
        const Text(
          'Keyboard type and autocorrect are inferred from the first hint when '
          'they are not given explicitly.',
          style: TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
      ],
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String placeholder,
    required List<String>? autofillHints,
    bool obscureText = false,
  }) {
    return TextField(
      controller: controller,
      obscureText: obscureText,
      autofillHints: autofillHints,
      decoration: InputDecoration(
        hintText: placeholder,
        isDense: true,
        filled: true,
        fillColor: const Color(0xFFE8F0FE),
        border: const OutlineInputBorder(),
      ),
    );
  }
}

class _MenuButton extends StatelessWidget {
  const _MenuButton({
    required this.label,
    required this.onTap,
    required this.background,
  });

  final String label;
  final VoidCallback onTap;
  final Color background;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        color: background,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }
}
