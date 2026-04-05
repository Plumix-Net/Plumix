using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/radio_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class RadioDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new RadioDemoPageState();
    }
}

internal sealed class RadioDemoPageState : State
{
    private bool _enabled = true;
    private bool _toggleable = true;
    private bool _shrinkWrapTapTarget;
    private string? _groupValue = "first";
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var radioTheme = baseTheme with
        {
            MaterialTapTargetSize = _shrinkWrapTapTarget
                ? MaterialTapTargetSize.ShrinkWrap
                : MaterialTapTargetSize.Padded
        };

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Radio baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material Radio with group selection, toggleable mode, and tap-target policy toggle.",
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
                            label: _toggleable ? "Toggleable" : "No toggle",
                            onTap: ToggleToggleable,
                            width: 116,
                            background: Color.Parse("#FFEAE4FF")),
                        BuildControlButton(
                            label: _shrinkWrapTapTarget ? "Tap: shrink" : "Tap: padded",
                            onTap: ToggleTapTargetSize,
                            width: 128,
                            background: Color.Parse("#FFE8F4E8")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: Reset,
                            width: 80,
                            background: Color.Parse("#FFF3E8D8")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, toggleable={(_toggleable ? "true" : "false")}, groupValue={FormatValue(_groupValue)}, changes={_changes}, tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Theme(
                    data: radioTheme,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "first",
                                    groupValue: _groupValue,
                                    onChanged: _enabled ? OnChanged : null,
                                    toggleable: _toggleable),
                                title: "Default radio #1",
                                subtitle: "value: first"),
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "second",
                                    groupValue: _groupValue,
                                    onChanged: _enabled ? OnChanged : null,
                                    toggleable: _toggleable),
                                title: "Default radio #2",
                                subtitle: "value: second"),
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "custom",
                                    groupValue: _groupValue,
                                    onChanged: _enabled ? OnChanged : null,
                                    toggleable: _toggleable,
                                    activeColor: Color.Parse("#FF00695C"),
                                    fillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.HasFlag(MaterialState.Disabled))
                                        {
                                            return Color.Parse("#6100695C");
                                        }

                                        if (states.HasFlag(MaterialState.Selected))
                                        {
                                            return Color.Parse("#FF00695C");
                                        }

                                        return Color.Parse("#FF455A64");
                                    }),
                                    backgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        return states.HasFlag(MaterialState.Selected)
                                            ? Color.Parse("#1400695C")
                                            : Colors.Transparent;
                                    }),
                                    overlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.HasFlag(MaterialState.Pressed))
                                        {
                                            return Color.Parse("#3300695C");
                                        }

                                        if (states.HasFlag(MaterialState.Hovered))
                                        {
                                            return Color.Parse("#2200695C");
                                        }

                                        if (states.HasFlag(MaterialState.Focused))
                                        {
                                            return Color.Parse("#2900695C");
                                        }

                                        return null;
                                    }),
                                    side: new BorderSide(Color.Parse("#FF00695C"), 2),
                                    innerRadius: MaterialStateProperty<double?>.All(5)),
                                title: "Custom colors",
                                subtitle: "fill/overlay/side/background overrides"),
                        ])),
            ]);
    }

    private Widget BuildRadioRow<T>(Radio<T> radio, string title, string subtitle)
    {
        return new Container(
            padding: new Thickness(10, 8),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10),
                Border: new BorderSide(Color.Parse("#FFD6DEEA"), 1)),
            child: new Row(
                spacing: 10,
                children:
                [
                    radio,
                    new Expanded(
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 2,
                            children:
                            [
                                new Text(title, fontSize: 13, color: Colors.Black),
                                new Text(subtitle, fontSize: 12, color: Color.Parse("#8A000000")),
                            ])),
                ]));
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

    private void ToggleToggleable()
    {
        SetState(() => _toggleable = !_toggleable);
    }

    private void ToggleTapTargetSize()
    {
        SetState(() => _shrinkWrapTapTarget = !_shrinkWrapTapTarget);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _enabled = true;
            _toggleable = true;
            _shrinkWrapTapTarget = false;
            _groupValue = "first";
            _changes = 0;
        });
    }

    private void OnChanged(string? value)
    {
        SetState(() =>
        {
            _groupValue = value;
            _changes += 1;
        });
    }

    private static string FormatValue(string? value)
    {
        return value ?? "null";
    }
}
