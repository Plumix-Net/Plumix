using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
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
    private TextEditingController _pinController = null!;
    private TextEditingController _caretController = null!;
    private TextEditingController _animatedCaretController = null!;
    private TextEditingController _longLineController = null!;
    private TextEditingController _scrollingNotesController = null!;
    private TextEditingController _undoableController = null!;
    private UndoHistoryController _undoController = null!;
    private bool _enabled = true;
    private string _lastChange = "(none)";

    public override void InitState()
    {
        _nameController = new TextEditingController();
        _notesController = new TextEditingController();
        _pinController = new TextEditingController();
        _caretController = new TextEditingController("Wide rounded caret");
        _animatedCaretController = new TextEditingController("iOS-style fading caret");
        _longLineController = new TextEditingController(
            "This single line is far wider than its field, so typing at the end scrolls to the caret");
        _scrollingNotesController = new TextEditingController(
            "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8");
        _undoableController = new TextEditingController();
        _undoController = new UndoHistoryController();
    }

    public override void Dispose()
    {
        _nameController.Dispose();
        _notesController.Dispose();
        _pinController.Dispose();
        _caretController.Dispose();
        _animatedCaretController.Dispose();
        _longLineController.Dispose();
        _scrollingNotesController.Dispose();
        _undoableController.Dispose();
        _undoController.Dispose();

        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        int notesLineCount = CountLines(_notesController.Text);

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
                                background: new Color(0xFFDCE3ED),
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
                                background: new Color(0xFFE9F5EC),
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
                        background: new Color(0xFFF3E8D8),
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
                    "PIN (obscured; mobile shows the last typed character briefly)",
                    fontSize: 12,
                    color: Colors.DimGray),
                new EditableText(
                    controller: _pinController,
                    enabled: _enabled,
                    obscureText: true,
                    placeholder: "Type a PIN",
                    onChanged: value => SetState(() => _lastChange = $"pin length = {value.Length}")),
                new Text("Caret (width 3, radius 2)", fontSize: 12, color: Colors.DimGray),
                new EditableText(
                    controller: _caretController,
                    enabled: _enabled,
                    cursorWidth: 3,
                    cursorRadius: Radius.Circular(2),
                    cursorColor: new Color(0xFFD81B60),
                    onChanged: value => SetState(() => _lastChange = $"caret = {value}")),
                new Text("Caret (opacity animates)", fontSize: 12, color: Colors.DimGray),
                new EditableText(
                    controller: _animatedCaretController,
                    enabled: _enabled,
                    cursorOpacityAnimates: true,
                    onChanged: value => SetState(() => _lastChange = $"animated caret = {value}")),
                new Text("Long line (scrolls to the caret)", fontSize: 12, color: Colors.DimGray),
                new Align(
                    alignment: Alignment.CenterLeft,
                    child: new SizedBox(
                        width: 220,
                        child: new EditableText(
                            controller: _longLineController,
                            enabled: _enabled,
                            onChanged: value => SetState(() => _lastChange = $"long line = {value}")))),
                new Text("Notes (3 lines, scrolls)", fontSize: 12, color: Colors.DimGray),
                new EditableText(
                    controller: _scrollingNotesController,
                    enabled: _enabled,
                    multiline: true,
                    maxLines: 3,
                    onChanged: value => SetState(() => _lastChange = $"scrolling notes = {EscapeMultiline(value)}")),
                new Text("Undo history (a formatter upper-cases input)", fontSize: 12, color: Colors.DimGray),
                new ValueListenableBuilder<UndoHistoryValue>(
                    valueListenable: _undoController,
                    builder: (_, value, _) => new Row(
                        spacing: 8,
                        children:
                        [
                            new SizedBox(
                                width: 120,
                                child: new CounterTapButton(
                                    label: value.CanUndo ? "Undo" : "Undo (-)",
                                    onTap: _undoController.Undo,
                                    background: new Color(0xFFDCE3ED),
                                    foreground: Colors.Black,
                                    fontSize: 12,
                                    padding: new Thickness(10, 8))),
                            new SizedBox(
                                width: 120,
                                child: new CounterTapButton(
                                    label: value.CanRedo ? "Redo" : "Redo (-)",
                                    onTap: _undoController.Redo,
                                    background: new Color(0xFFDCE3ED),
                                    foreground: Colors.Black,
                                    fontSize: 12,
                                    padding: new Thickness(10, 8))),
                        ])),
                new EditableText(
                    controller: _undoableController,
                    enabled: _enabled,
                    undoController: _undoController,
                    inputFormatters: [UpperCaseFormatter],
                    placeholder: "Type, then undo/redo (Ctrl+Z / Ctrl+Shift+Z)",
                    onChanged: value => SetState(() => _lastChange = $"undoable = {value}")),
                new Text(
                    $"current: name='{_nameController.Text}', notes='{EscapeMultiline(_notesController.Text)}'",
                    fontSize: 12,
                    color: Colors.Black),
            ]);
    }

    private static readonly TextInputFormatter UpperCaseFormatter = TextInputFormatter.WithFunction(
        (_, newValue) => newValue.CopyWith(text: newValue.Text.ToUpperInvariant()));

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

        string normalized = value.Replace("\r", string.Empty, StringComparison.Ordinal);
        int lines = 1;
        foreach (char character in normalized)
        {
            if (character == '\n')
            {
                lines++;
            }
        }

        return lines;
    }
}
