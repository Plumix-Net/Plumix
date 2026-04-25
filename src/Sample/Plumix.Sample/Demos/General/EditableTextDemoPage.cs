using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/editable_text_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class EditableTextDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new EditableTextDemoPageState();
    }
}

internal sealed class EditableTextDemoPageState : State
{
    private TextEditingController _nameController = null!;
    private TextEditingController _notesController = null!;
    private bool _enabled = true;
    private string _lastChange = "(none)";

    public override void InitState()
    {
        _nameController = new TextEditingController();
        _notesController = new TextEditingController();
    }

    public override void Dispose()
    {
        _nameController.Dispose();
        _notesController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var notesLineCount = CountLines(_notesController.Text);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("EditableText + Focus/IME", fontSize: 20, color: Colors.Black),
                new Text(
                    "Baseline input + multiline: Enter adds new line in Notes; ArrowUp/ArrowDown moves caret between lines.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new SizedBox(
                            width: 120,
                            child: new CounterTapButton(
                                label: _enabled ? "Disable" : "Enable",
                                onTap: () => SetState(() => _enabled = !_enabled),
                                background: Color.Parse("#FFDCE3ED"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                        new SizedBox(
                            width: 120,
                            child: new CounterTapButton(
                                label: "Clear",
                                onTap: () => SetState(() =>
                                {
                                    _nameController.Clear();
                                    _notesController.Clear();
                                    _lastChange = "(cleared)";
                                }),
                                background: Color.Parse("#FFE9F5EC"),
                                foreground: Colors.Black,
                                fontSize: 12,
                                padding: new Thickness(10, 8))),
                    ]),
                new SizedBox(
                    width: 170,
                    child: new CounterTapButton(
                        label: "Seed notes",
                        onTap: () => SetState(() =>
                        {
                            _notesController.Text = "First line\nSecond line\nThird line";
                            _lastChange = "(seeded notes)";
                        }),
                        background: Color.Parse("#FFF3E8D8"),
                        foreground: Colors.Black,
                        fontSize: 12,
                        padding: new Thickness(10, 8))),
                new Text($"last change: {_lastChange}", fontSize: 12, color: Colors.DarkSlateGray),
                new Text("Name", fontSize: 12, color: Colors.DimGray),
                new EditableText(
                    controller: _nameController,
                    enabled: _enabled,
                    placeholder: "Type your name",
                    onChanged: value => SetState(() => _lastChange = $"name = {value}")),
                new Text("Notes (multiline)", fontSize: 12, color: Colors.DimGray),
                new EditableText(
                    controller: _notesController,
                    enabled: _enabled,
                    multiline: true,
                    placeholder: "Type notes (Enter creates new line)",
                    onChanged: value => SetState(() => _lastChange = $"notes = {EscapeMultiline(value)}")),
                new Text(
                    $"notes lines: {notesLineCount}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Text(
                    $"current: name='{_nameController.Text}', notes='{EscapeMultiline(_notesController.Text)}'",
                    fontSize: 12,
                    color: Colors.Black),
            ]);
    }

    private static string EscapeMultiline(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static int CountLines(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 1;
        }

        var normalized = value.Replace("\r", string.Empty, StringComparison.Ordinal);
        var lines = 1;
        foreach (var character in normalized)
        {
            if (character == '\n')
            {
                lines++;
            }
        }

        return lines;
    }
}
