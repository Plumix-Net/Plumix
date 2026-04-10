using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/material_buttons_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class MaterialButtonsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new MaterialButtonsDemoPageState();
    }
}

internal sealed class MaterialButtonsDemoPageState : State
{
    private bool _enabled = true;
    private int _textButtonTaps;
    private int _elevatedButtonTaps;
    private int _outlinedButtonTaps;
    private int _filledButtonTaps;
    private int _filledTonalButtonTaps;
    private int _iconButtonTaps;
    private int _filledIconButtonTaps;
    private int _outlinedIconButtonTaps;
    private bool _iconButtonSelected;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Material buttons baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "TextButton / ElevatedButton / OutlinedButton / FilledButton (+ tonal) / IconButton with enabled/disabled and theme-aware defaults.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _enabled ? "Enabled" : "Disabled",
                            onTap: ToggleEnabled,
                            width: 108,
                            background: Color.Parse("#FFE9F0FF")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: ResetCounters,
                            width: 88,
                            background: Color.Parse("#FFF3E8D8")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, text={_textButtonTaps}, elevated={_elevatedButtonTaps}, outlined={_outlinedButtonTaps}, filled={_filledButtonTaps}, tonal={_filledTonalButtonTaps}, icon={_iconButtonTaps}, filledIcon={_filledIconButtonTaps}, outlinedIcon={_outlinedIconButtonTaps}, iconSelected={(_iconButtonSelected ? "true" : "false")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new SizedBox(
                    width: 240,
                    child: new TextButton(
                        onPressed: _enabled ? OnTextButtonTap : null,
                        child: new Text($"TextButton taps: {_textButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: new ElevatedButton(
                        onPressed: _enabled ? OnElevatedButtonTap : null,
                        child: new Text($"ElevatedButton taps: {_elevatedButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: new OutlinedButton(
                        onPressed: _enabled ? OnOutlinedButtonTap : null,
                        child: new Text($"OutlinedButton taps: {_outlinedButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: new FilledButton(
                        onPressed: _enabled ? OnFilledButtonTap : null,
                        child: new Text($"FilledButton taps: {_filledButtonTaps}"))),
                new SizedBox(
                    width: 240,
                    child: FilledButton.Tonal(
                        onPressed: _enabled ? OnFilledTonalButtonTap : null,
                        child: new Text($"FilledButton.tonal taps: {_filledTonalButtonTaps}"))),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new SizedBox(
                            width: 56,
                            height: 56,
                            child: new IconButton(
                                icon: new Icon(Icons.StarOutline),
                                selectedIcon: new Icon(Icons.Star),
                                isSelected: _iconButtonSelected,
                                onPressed: _enabled ? OnIconButtonTap : null)),
                        new SizedBox(
                            width: 56,
                            height: 56,
                            child: IconButton.Filled(
                                icon: new Icon(Icons.Add),
                                onPressed: _enabled ? OnFilledIconButtonTap : null)),
                        new SizedBox(
                            width: 56,
                            height: 56,
                            child: IconButton.Outlined(
                                icon: new Icon(Icons.InfoOutline),
                                onPressed: _enabled ? OnOutlinedIconButtonTap : null)),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new ElevatedButton(
                                onPressed: _enabled ? OnElevatedButtonTap : null,
                                backgroundColor: Color.Parse("#FF6A994E"),
                                foregroundColor: Colors.White,
                                child: new Text("Custom elevated"))),
                        new Expanded(
                            child: new OutlinedButton(
                                onPressed: _enabled ? OnOutlinedButtonTap : null,
                                borderColor: Color.Parse("#FF7B2CBF"),
                                foregroundColor: Color.Parse("#FF7B2CBF"),
                                child: new Text("Custom outlined"))),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        new Expanded(
                            child: new FilledButton(
                                onPressed: _enabled ? OnFilledButtonTap : null,
                                foregroundColor: Colors.White,
                                backgroundColor: Color.Parse("#FF005E7A"),
                                child: new Text("Custom filled"))),
                        new Expanded(
                            child: FilledButton.Tonal(
                                onPressed: _enabled ? OnFilledTonalButtonTap : null,
                                foregroundColor: Color.Parse("#FF42275A"),
                                backgroundColor: Color.Parse("#FFD8CFF8"),
                                child: new Text("Custom tonal"))),
                    ]),
            ]);
    }

    private Widget BuildControlButton(
        string label,
        Action onTap,
        double width,
        Color background)
    {
        return new SizedBox(
            width: width,
            child: new TextButton(
                onPressed: onTap,
                backgroundColor: background,
                foregroundColor: Colors.Black,
                minHeight: 36,
                padding: new Thickness(10, 8),
                borderRadius: BorderRadius.Circular(8),
                child: new Text(
                    label,
                    fontSize: 12)));
    }

    private void ToggleEnabled()
    {
        SetState(() => _enabled = !_enabled);
    }

    private void ResetCounters()
    {
        SetState(() =>
        {
            _textButtonTaps = 0;
            _elevatedButtonTaps = 0;
            _outlinedButtonTaps = 0;
            _filledButtonTaps = 0;
            _filledTonalButtonTaps = 0;
            _iconButtonTaps = 0;
            _filledIconButtonTaps = 0;
            _outlinedIconButtonTaps = 0;
            _iconButtonSelected = false;
            _enabled = true;
        });
    }

    private void OnTextButtonTap()
    {
        SetState(() => _textButtonTaps += 1);
    }

    private void OnElevatedButtonTap()
    {
        SetState(() => _elevatedButtonTaps += 1);
    }

    private void OnOutlinedButtonTap()
    {
        SetState(() => _outlinedButtonTaps += 1);
    }

    private void OnFilledButtonTap()
    {
        SetState(() => _filledButtonTaps += 1);
    }

    private void OnFilledTonalButtonTap()
    {
        SetState(() => _filledTonalButtonTaps += 1);
    }

    private void OnIconButtonTap()
    {
        SetState(() =>
        {
            _iconButtonTaps += 1;
            _iconButtonSelected = !_iconButtonSelected;
        });
    }

    private void OnFilledIconButtonTap()
    {
        SetState(() => _filledIconButtonTaps += 1);
    }

    private void OnOutlinedIconButtonTap()
    {
        SetState(() => _outlinedIconButtonTaps += 1);
    }
}
