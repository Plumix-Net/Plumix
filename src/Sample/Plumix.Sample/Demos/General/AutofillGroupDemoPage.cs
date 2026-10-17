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

        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AutofillGroup", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "Fields sharing the closest AutofillGroup are cross-referenced by the platform. "
                    + "Disposing the topmost group finishes the autofill context.",
                    style: new TextStyle(FontSize: 14, Color: Colors.DimGray)),
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
                                background: new Color(0xFFDCE3ED),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                        new SizedBox(
                            width: 160,
                            child: new CounterTapButton(
                                label: _emailAutofillEnabled ? "Email: autofill on" : "Email: autofill off",
                                onTap: () => SetState(() =>
                                    _emailAutofillEnabled = !_emailAutofillEnabled),
                                background: new Color(0xFFE9F5EC),
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
                        background: new Color(0xFFF3E8D8),
                        foreground: Colors.Black,
                        fontSize: 12,
                        padding: new Thickness(10, 8))),
                new Text(
                    $"last action: {_lastAction}",
                    style: new TextStyle(
                        FontSize: 12,
                        Color: Colors.DarkSlateGray)),
                new AutofillGroup(
                    onDisposeAction: _onDisposeAction,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Username", style: new TextStyle(FontSize: 12, Color: Colors.DimGray)),
                            new EditableText(
                                controller: _usernameController,
                                placeholder: "username",
                                autofillHints: [AutofillHints.Username]),
                            new Text("Password", style: new TextStyle(FontSize: 12, Color: Colors.DimGray)),
                            new EditableText(
                                controller: _passwordController,
                                placeholder: "password",
                                obscureText: true,
                                autofillHints: [AutofillHints.Password]),
                            new Text("Email", style: new TextStyle(FontSize: 12, Color: Colors.DimGray)),
                            new EditableText(
                                controller: _emailController,
                                placeholder: "email",
                                autofillHints: _emailAutofillEnabled
                                    ? [AutofillHints.Email]
                                    : EditableText.AutofillDisabled),
                            new Builder(inner => new Text(
                                $"clients in group: {AutofillGroup.Of(inner).AutofillClients.Count()}",
                                style: new TextStyle(FontSize: 12, Color: Colors.Black))),
                        ])),
                new Text(
                    "Keyboard type and autocorrect are inferred from the first hint when they are "
                    + "not given explicitly.",
                    style: new TextStyle(FontSize: 12, Color: Colors.DarkSlateGray)),
            ]);
    }
}
