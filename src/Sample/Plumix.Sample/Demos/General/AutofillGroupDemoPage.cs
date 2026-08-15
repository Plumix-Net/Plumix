using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/autofill_group_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class AutofillGroupDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AutofillGroupDemoPageState();
    }
}

internal sealed class AutofillGroupDemoPageState : State
{
    private TextEditingController _usernameController = null!;
    private TextEditingController _passwordController = null!;
    private TextEditingController _emailController = null!;
    private AutofillContextAction _onDisposeAction = AutofillContextAction.Commit;
    private bool _emailAutofillEnabled = true;
    private string _lastAction = "(none)";

    public override void InitState()
    {
        _usernameController = new TextEditingController();
        _passwordController = new TextEditingController();
        _emailController = new TextEditingController();
    }

    public override void Dispose()
    {
        _usernameController.Dispose();
        _passwordController.Dispose();
        _emailController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AutofillGroup", fontSize: 20, color: Colors.Black),
                new Text(
                    "Fields sharing the closest AutofillGroup are cross-referenced by the platform. "
                    + "Disposing the topmost group finishes the autofill context.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new SizedBox(
                            width: 160,
                            child: new CounterTapButton(
                                label: _onDisposeAction == AutofillContextAction.Commit
                                    ? "onDispose: commit"
                                    : "onDispose: cancel",
                                onTap: () => SetState(() => _onDisposeAction =
                                    _onDisposeAction == AutofillContextAction.Commit
                                        ? AutofillContextAction.Cancel
                                        : AutofillContextAction.Commit),
                                background: Color.Parse("#FFDCE3ED"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                        new SizedBox(
                            width: 160,
                            child: new CounterTapButton(
                                label: _emailAutofillEnabled ? "Email: autofill on" : "Email: autofill off",
                                onTap: () => SetState(() =>
                                    _emailAutofillEnabled = !_emailAutofillEnabled),
                                background: Color.Parse("#FFE9F5EC"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                    ]),
                new SizedBox(
                    width: 200,
                    child: new CounterTapButton(
                        label: "finishAutofillContext",
                        onTap: () => SetState(() =>
                        {
                            TextInput.FinishAutofillContext();
                            _lastAction = "finishAutofillContext(shouldSave: true)";
                        }),
                        background: Color.Parse("#FFF3E8D8"),
                        foreground: Colors.Black,
                        fontSize: 12,
                        padding: new Thickness(10, 8))),
                new Text($"last action: {_lastAction}", fontSize: 12, color: Colors.DarkSlateGray),
                new AutofillGroup(
                    onDisposeAction: _onDisposeAction,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Username", fontSize: 12, color: Colors.DimGray),
                            new EditableText(
                                controller: _usernameController,
                                placeholder: "username",
                                autofillHints: [AutofillHints.Username]),
                            new Text("Password", fontSize: 12, color: Colors.DimGray),
                            new EditableText(
                                controller: _passwordController,
                                placeholder: "password",
                                obscureText: true,
                                autofillHints: [AutofillHints.Password]),
                            new Text("Email", fontSize: 12, color: Colors.DimGray),
                            new EditableText(
                                controller: _emailController,
                                placeholder: "email",
                                autofillHints: _emailAutofillEnabled
                                    ? [AutofillHints.Email]
                                    : EditableText.AutofillDisabled),
                            new Builder(inner => new Text(
                                $"clients in group: {AutofillGroup.Of(inner).AutofillClients.Count()}",
                                fontSize: 12,
                                color: Colors.Black)),
                        ])),
                new Text(
                    "Keyboard type and autocorrect are inferred from the first hint when they are "
                    + "not given explicitly.",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
            ]);
    }
}
