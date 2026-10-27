using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/editable_text_demo_page.dart (exact sample parity)

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
        _nameController = new TextEditingController("Flutter!", TextSelection.Collapsed(3));
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

        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("EditableText + Focus/IME",
                        style: new TextStyle(FontSize: 20, Color: MaterialColors.Black)),
                    new Text(
                        "Baseline input + multiline: Enter adds new line in Notes; "
                        + "ArrowUp/ArrowDown moves caret between lines.",
                        style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new SizedBox(
                                width: 120,
                                child: new MenuButton(
                                    _enabled ? "Disable" : "Enable",
                                    () => SetState(() => _enabled = !_enabled),
                                    new Color(0xFFDCE3ED))),
                            new SizedBox(
                                width: 120,
                                child: new MenuButton(
                                    "Clear",
                                    () => SetState(() =>
                                    {
                                        _nameController.Clear();
                                        _notesController.Clear();
                                        _lastChange = "(cleared)";
                                    }),
                                    new Color(0xFFE9F5EC))),
                        ]),
                    new SizedBox(
                        width: 170,
                        child: new MenuButton(
                            "Seed notes",
                            () => SetState(() =>
                            {
                                _notesController.Text = "First line\nSecond line\nThird line";
                                _lastChange = "(seeded notes)";
                            }),
                            new Color(0xFFF3E8D8))),
                    new Text(
                        $"last change: {_lastChange}",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                    new Text(
                        "Name (autofocus preserves the caret at offset 3)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _nameController,
                        "Type your name",
                        value => SetState(() => _lastChange = $"name = {value}"),
                        autofocus: true),
                    new ValueListenableBuilder<TextEditingValue>(
                        valueListenable: _nameController,
                        builder: (_, value, _) => new Text(
                            $"name selection: {value.Selection.BaseOffset}/{value.Selection.ExtentOffset}",
                            style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey))),
                    new Text("Notes (multiline)", style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _notesController,
                        "Type notes (Enter creates new line)",
                        value => SetState(() => _lastChange = $"notes = {EscapeMultiline(value)}"),
                        multiline: true),
                    new Text(
                        $"notes lines: {notesLineCount}",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey)),
                    new Text(
                        "PIN (obscured; mobile shows the last typed character briefly)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _pinController,
                        "Type a PIN",
                        value => SetState(() => _lastChange = $"pin length = {value.Length}"),
                        obscureText: true),
                    new Text(
                        "Caret (width 3, radius 2)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _caretController,
                        "",
                        value => SetState(() => _lastChange = $"caret = {value}"),
                        cursorWidth: 3,
                        cursorRadius: Radius.Circular(2),
                        cursorColor: new Color(0xFFD81B60)),
                    new Text(
                        "Caret (opacity animates)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _animatedCaretController,
                        "",
                        value => SetState(() => _lastChange = $"animated caret = {value}"),
                        cursorOpacityAnimates: true),
                    new Text(
                        "Long line (scrolls to the caret)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    new Align(
                        alignment: Alignment.CenterLeft,
                        child: new SizedBox(
                            width: 220,
                            child: BuildTextField(
                                _longLineController,
                                "",
                                value => SetState(() => _lastChange = $"long line = {value}")))),
                    new Text(
                        "Notes (3 lines, scrolls)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    BuildTextField(
                        _scrollingNotesController,
                        "",
                        value => SetState(() => _lastChange = $"scrolling notes = {EscapeMultiline(value)}"),
                        multiline: true,
                        maxLines: 3),
                    new Text(
                        "Undo history (a formatter upper-cases input)",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black54)),
                    new SizedBox(
                        width: 170,
                        child: new MenuButton(
                            "Seed undo value",
                            () =>
                            {
                                ValueNotifier<TextEditingValue> value = _undoableController;
                                value.Value = new TextEditingValue("SEEDED", TextSelection.Collapsed(6));
                            },
                            new Color(0xFFF3E8D8))),
                    new SizedBox(
                        width: 170,
                        child: new MenuButton(
                            "Reset controller text",
                            () => _undoableController.Text = "RESET",
                            new Color(0xFFF3E8D8))),
                    new ValueListenableBuilder<TextEditingValue>(
                        valueListenable: _undoableController,
                        builder: (_, value, _) => new Text(
                            $"history value: '{value.Text}', "
                            + $"selection: {value.Selection.BaseOffset}/{value.Selection.ExtentOffset}, "
                            + $"composing: {value.Composing.Start}/{value.Composing.End}",
                            style: new TextStyle(FontSize: 12, Color: MaterialColors.BlueGrey))),
                    new ValueListenableBuilder<UndoHistoryValue>(
                        valueListenable: _undoController,
                        builder: (_, value, _) => new Row(
                            spacing: 8,
                            children:
                            [
                                new SizedBox(
                                    width: 120,
                                    child: new MenuButton(
                                        value.CanUndo ? "Undo" : "Undo (-)",
                                        _undoController.Undo,
                                        new Color(0xFFDCE3ED))),
                                new SizedBox(
                                    width: 120,
                                    child: new MenuButton(
                                        value.CanRedo ? "Redo" : "Redo (-)",
                                        _undoController.Redo,
                                        new Color(0xFFDCE3ED))),
                            ])),
                    BuildTextField(
                        _undoableController,
                        "Type, then undo/redo (Ctrl+Z / Ctrl+Shift+Z)",
                        value => SetState(() => _lastChange = $"undoable = {value}"),
                        undoController: _undoController,
                        inputFormatters: [UpperCaseFormatter]),
                    new Text(
                        $"current: name='{_nameController.Text}', notes='{EscapeMultiline(_notesController.Text)}'",
                        style: new TextStyle(FontSize: 12, Color: MaterialColors.Black)),
                ]));
    }

    private Widget BuildTextField(
        TextEditingController controller,
        string placeholder,
        Action<string> onChanged,
        bool autofocus = false,
        bool multiline = false,
        int? maxLines = null,
        bool obscureText = false,
        double cursorWidth = 2.0,
        Radius? cursorRadius = null,
        Color? cursorColor = null,
        bool? cursorOpacityAnimates = null,
        UndoHistoryController? undoController = null,
        IReadOnlyList<TextInputFormatter>? inputFormatters = null)
    {
        return new TextField(
            controller: controller,
            autofocus: autofocus,
            undoController: undoController,
            inputFormatters: inputFormatters,
            enabled: _enabled,
            maxLines: maxLines ?? (multiline ? null : 1),
            obscureText: obscureText,
            cursorWidth: cursorWidth,
            cursorRadius: cursorRadius,
            cursorColor: cursorColor,
            cursorOpacityAnimates: cursorOpacityAnimates,
            onChanged: onChanged,
            decoration: new InputDecoration(
                hintText: placeholder,
                isDense: true,
                filled: true,
                fillColor: _enabled ? new Color(0xFFE8F0FE) : new Color(0xFFF5F5F5),
                border: new OutlineInputBorder()));
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

    private sealed class MenuButton(string label, Action onTap, Color background) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            return new GestureDetector(
                onTap: onTap,
                child: new Container(
                    color: background,
                    padding: EdgeInsets.Symmetric(horizontal: 10, vertical: 8),
                    child: new Text(label, style: new TextStyle(FontSize: 12, Color: MaterialColors.Black))));
        }
    }
}
