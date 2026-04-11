using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/switch_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class SwitchDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new SwitchDemoPageState();
    }
}

internal sealed class SwitchDemoPageState : State
{
    private bool _enabled = true;
    private bool _value = true;
    private bool _shrinkWrapTapTarget;
    private bool _showThumbIcons = true;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var switchTheme = baseTheme with
        {
            MaterialTapTargetSize = _shrinkWrapTapTarget
                ? MaterialTapTargetSize.ShrinkWrap
                : MaterialTapTargetSize.Padded,
            SwitchTheme = new SwitchThemeData(
                ThumbIcon: _showThumbIcons
                    ? MaterialStateProperty<Icon?>.ResolveWith(states =>
                    {
                        return states.HasFlag(MaterialState.Selected)
                            ? new Icon(Icons.Check, size: 14)
                            : new Icon(Icons.Close, size: 14);
                    })
                    : null)
        };

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Switch baseline", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material Switch with value control, drag/tap interaction, thumb icons, and theme/widget color precedence.",
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
                            label: _showThumbIcons ? "Icons: on" : "Icons: off",
                            onTap: ToggleThumbIcons,
                            width: 108,
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
                    $"enabled={(_enabled ? "true" : "false")}, value={(_value ? "true" : "false")}, thumbIcons={(_showThumbIcons ? "true" : "false")}, changes={_changes}, tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Theme(
                    data: switchTheme,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            BuildSwitchRow(
                                toggle: new Switch(
                                    value: _value,
                                    onChanged: _enabled ? OnValueChanged : null),
                                title: "Default switch",
                                subtitle: "Tap or drag thumb to toggle on/off"),
                            BuildSwitchRow(
                                toggle: new Switch(
                                    value: _value,
                                    onChanged: _enabled ? OnValueChanged : null,
                                    thumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.HasFlag(MaterialState.Disabled))
                                        {
                                            return Color.Parse("#6100695C");
                                        }

                                        if (states.HasFlag(MaterialState.Selected))
                                        {
                                            return Color.Parse("#FFE8F5E9");
                                        }

                                        return Color.Parse("#FFB2DFDB");
                                    }),
                                    trackColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.HasFlag(MaterialState.Disabled))
                                        {
                                            return Color.Parse("#3300695C");
                                        }

                                        if (states.HasFlag(MaterialState.Selected))
                                        {
                                            return Color.Parse("#FF00695C");
                                        }

                                        return Color.Parse("#FFB0BEC5");
                                    }),
                                    trackOutlineColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        return states.HasFlag(MaterialState.Selected)
                                            ? Colors.Transparent
                                            : Color.Parse("#FF455A64");
                                    }),
                                    trackOutlineWidth: MaterialStateProperty<double?>.All(2)),
                                title: "Custom colors",
                                subtitle: "thumb/track/outline overrides"),
                        ])),
            ]);
    }

    private Widget BuildSwitchRow(Switch toggle, string title, string subtitle)
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
                    toggle,
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

    private void ToggleThumbIcons()
    {
        SetState(() => _showThumbIcons = !_showThumbIcons);
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
            _value = true;
            _showThumbIcons = true;
            _shrinkWrapTapTarget = false;
            _changes = 0;
        });
    }

    private void OnValueChanged(bool nextValue)
    {
        SetState(() =>
        {
            _value = nextValue;
            _changes += 1;
        });
    }
}
