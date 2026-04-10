using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/checkbox_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class CheckboxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CheckboxDemoPageState();
    }
}

internal sealed class CheckboxDemoPageState : State
{
    private bool _enabled = true;
    private bool _checked;
    private bool? _tristateValue;
    private bool _shrinkWrapTapTarget;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var checkboxTheme = baseTheme with
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
                new Text("Checkbox baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material Checkbox with bool and bool? (tristate) values, enabled/disabled flow, and tap-target policy toggle.",
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
                            label: _shrinkWrapTapTarget ? "Tap: shrink" : "Tap: padded",
                            onTap: ToggleTapTargetSize,
                            width: 128,
                            background: Color.Parse("#FFEAE4FF")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: Reset,
                            width: 80,
                            background: Color.Parse("#FFF3E8D8")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, checked={(_checked ? "true" : "false")}, tristate={FormatNullableBool(_tristateValue)}, changes={_changes}, tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Theme(
                    data: checkboxTheme,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            BuildCheckboxRow(
                                checkbox: new Checkbox(
                                    value: _checked,
                                    onChanged: _enabled ? OnCheckedChanged : null),
                                title: "Default checkbox",
                                subtitle: "value: false/true"),
                            BuildCheckboxRow(
                                checkbox: new Checkbox(
                                    value: _tristateValue,
                                    tristate: true,
                                    onChanged: _enabled ? OnTristateChanged : null),
                                title: "Tristate checkbox",
                                subtitle: "cycle: false -> true -> null -> false"),
                            BuildCheckboxRow(
                                checkbox: new Checkbox(
                                    value: _checked,
                                    onChanged: _enabled ? OnCheckedChanged : null,
                                    activeColor: Color.Parse("#FF00695C"),
                                    checkColor: Colors.White,
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

                                        return Colors.Transparent;
                                    }),
                                    side: new BorderSide(Color.Parse("#FF00695C"), 2)),
                                title: "Custom colors",
                                subtitle: "active/check/fill/side overrides"),
                        ])),
            ]);
    }

    private Widget BuildCheckboxRow(Checkbox checkbox, string title, string subtitle)
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
                    checkbox,
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

    private void ToggleTapTargetSize()
    {
        SetState(() => _shrinkWrapTapTarget = !_shrinkWrapTapTarget);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _enabled = true;
            _checked = false;
            _tristateValue = null;
            _shrinkWrapTapTarget = false;
            _changes = 0;
        });
    }

    private void OnCheckedChanged(bool? nextValue)
    {
        SetState(() =>
        {
            _checked = nextValue ?? false;
            _changes += 1;
        });
    }

    private void OnTristateChanged(bool? nextValue)
    {
        SetState(() =>
        {
            _tristateValue = nextValue;
            _changes += 1;
        });
    }

    private static string FormatNullableBool(bool? value)
    {
        return value switch
        {
            true => "true",
            false => "false",
            _ => "null"
        };
    }
}
