using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/checkbox_demo_page.dart (exact sample parity)

namespace Plumix;

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
    private bool _useMaterial3 = true;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var checkboxTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            MaterialTapTargetSize = _shrinkWrapTapTarget
                ? MaterialTapTargetSize.ShrinkWrap
                : MaterialTapTargetSize.Padded
        };
        string status = $"mode={(_useMaterial3 ? "M3" : "M2")}, "
                        + $"enabled={(_enabled ? "true" : "false")}, "
                        + $"checked={(_checked ? "true" : "false")}, "
                        + $"tristate={FormatNullableBool(_tristateValue)}, "
                        + $"changes={_changes}, "
                        + $"tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}";

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Checkbox baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material Checkbox with M2/M3 defaults, tristate values, theme precedence, and tap-target policy.",
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
                            label: _useMaterial3 ? "Material 3" : "Material 2",
                            onTap: ToggleMaterialVersion,
                            width: 104,
                            background: Color.Parse("#FFE8F5E9")),
                        BuildControlButton(
                            label: "Reset",
                            onTap: Reset,
                            width: 80,
                            background: Color.Parse("#FFF3E8D8")),
                    ]),
                new Text(
                    status,
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
                            BuildCheckboxRow(
                                checkbox: new CheckboxTheme(
                                    data: new CheckboxThemeData(
                                        FillColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                            states.HasFlag(MaterialState.Selected)
                                                ? Color.Parse("#FF7B1FA2")
                                                : Colors.Transparent),
                                        CheckColor: MaterialStateProperty<Color?>.All(Colors.White),
                                        Shape: new RoundedRectangleBorder(borderRadius:
                                            Plumix.Rendering.BorderRadius.Circular(6)),
                                        Side: WidgetStateBorderSide.ResolveWith(states =>
                                            new BorderSide(
                                                states.Contains(WidgetState.Error)
                                                    ? Colors.Red
                                                    : Color.Parse("#FF7B1FA2"),
                                                2))),
                                    child: new Checkbox(
                                        value: _checked,
                                        onChanged: _enabled ? OnCheckedChanged : null)),
                                title: "CheckboxTheme",
                                subtitle: "fill/check/shape/stateful side precedence"),
                        ])),
                new Text("CupertinoCheckbox", fontSize: 20, color: Colors.Black),
                new Text(
                    "macOS-style checkbox: dynamic colors, focus ring, press overlay, and the dark-mode gradient.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                BuildCheckboxRow(
                    checkbox: new CupertinoCheckbox(
                        value: _checked,
                        onChanged: _enabled ? OnCheckedChanged : null),
                    title: "Default CupertinoCheckbox",
                    subtitle: "activeBlue fill, grey border when unselected"),
                BuildCheckboxRow(
                    checkbox: new CupertinoCheckbox(
                        value: _tristateValue,
                        tristate: true,
                        onChanged: _enabled ? OnTristateChanged : null),
                    title: "Tristate CupertinoCheckbox",
                    subtitle: "null value paints the dash"),
                BuildCheckboxRow(
                    checkbox: new CupertinoCheckbox(
                        value: _checked,
                        onChanged: _enabled ? OnCheckedChanged : null,
                        activeColor: Color.Parse("#FF00695C"),
                        checkColor: Colors.White,
                        shape: new RoundedRectangleBorder(borderRadius:
                            Plumix.Rendering.BorderRadius.Circular(5)),
                        side: new BorderSide(Color.Parse("#FF00695C"), 2)),
                    title: "Custom CupertinoCheckbox",
                    subtitle: "active/check/shape/side overrides"),
                new Container(
                    padding: new Thickness(10, 8),
                    decoration: new BoxDecoration(
                        Color: Color.Parse("#FF1C1C1E"),
                        BorderRadius: BorderRadius.Circular(10)),
                    child: new CupertinoTheme(
                        new CupertinoThemeData(brightness: PlatformBrightness.Dark),
                        new Row(
                            spacing: 10,
                            children:
                            [
                                new CupertinoCheckbox(
                                    value: _checked,
                                    onChanged: _enabled ? OnCheckedChanged : null),
                                new CupertinoCheckbox(
                                    value: _tristateValue,
                                    tristate: true,
                                    onChanged: _enabled ? OnTristateChanged : null),
                                new Expanded(
                                    child: new Text(
                                        "Dark brightness: gradient fill and dark dynamic colors",
                                        fontSize: 12,
                                        color: Colors.White)),
                            ]))),
            ]);
    }

    private Widget BuildCheckboxRow(Widget checkbox, string title, string subtitle)
    {
        return new Container(
            padding: new Thickness(10, 8),
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10),
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FFD6DEEA"), 1))),
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
                child: new Text(
                    label,
                    fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }

    private void ToggleEnabled()
    {
        SetState(() => _enabled = !_enabled);
    }

    private void ToggleTapTargetSize()
    {
        SetState(() => _shrinkWrapTapTarget = !_shrinkWrapTapTarget);
    }

    private void ToggleMaterialVersion()
    {
        SetState(() => _useMaterial3 = !_useMaterial3);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _enabled = true;
            _checked = false;
            _tristateValue = null;
            _shrinkWrapTapTarget = false;
            _useMaterial3 = true;
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
