using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/switch_demo_page.dart (exact sample parity)

namespace Plumix;

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
    private bool _useMaterial3 = true;
    private bool _value = true;
    private bool _shrinkWrapTapTarget;
    private bool _showThumbIcons = true;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var switchTheme = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            ColorScheme = baseTheme.ColorScheme with
            {
                Primary = new Color(0xFF006A60),
                OnPrimary = Colors.White,
                Secondary = new Color(0xFF9C432E),
                SurfaceContainerHighest = new Color(0xFFDCE5E1),
                Outline = new Color(0xFF6F7975)
            },
            MaterialTapTargetSize = _shrinkWrapTapTarget
                ? MaterialTapTargetSize.ShrinkWrap
                : MaterialTapTargetSize.Padded,
            SwitchTheme = new SwitchThemeData(
                ThumbIcon: _showThumbIcons
                    ? WidgetStateProperty<Icon?>.ResolveWith(states =>
                    {
                        return states.Contains(WidgetState.Selected)
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
                    color: new Color(0x8A000000)),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: _enabled ? "Enabled" : "Disabled",
                            onTap: ToggleEnabled,
                            width: 108,
                            background: new Color(0xFFE9F0FF)),
                        BuildControlButton(
                            label: _showThumbIcons ? "Icons: on" : "Icons: off",
                            onTap: ToggleThumbIcons,
                            width: 108,
                            background: new Color(0xFFEAE4FF)),
                        BuildControlButton(
                            label: _shrinkWrapTapTarget ? "Tap: shrink" : "Tap: padded",
                            onTap: ToggleTapTargetSize,
                            width: 128,
                            background: new Color(0xFFE8F4E8)),
                        BuildControlButton(
                            label: "Reset",
                            onTap: Reset,
                            width: 80,
                            background: new Color(0xFFF3E8D8)),
                    ]),
                new Row(
                    children:
                    [
                        BuildControlButton(
                            label: _useMaterial3 ? "Material 3" : "Material 2",
                            onTap: ToggleMaterialVersion,
                            width: 108,
                            background: new Color(0xFFE6F3EF)),
                    ]),
                new Text(
                    $"material={(_useMaterial3 ? "M3" : "M2")}, enabled={(_enabled ? "true" : "false")}, "
                    + $"value={(_value ? "true" : "false")}, thumbIcons={(_showThumbIcons ? "true" : "false")}, "
                    + $"changes={_changes}, tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}",
                    fontSize: 12,
                    color: new Color(0xFF607D8B)),
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
                                    thumbColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.Contains(WidgetState.Disabled))
                                        {
                                            return new Color(0x6100695C);
                                        }

                                        if (states.Contains(WidgetState.Selected))
                                        {
                                            return new Color(0xFFE8F5E9);
                                        }

                                        return new Color(0xFFB2DFDB);
                                    }),
                                    trackColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        if (states.Contains(WidgetState.Disabled))
                                        {
                                            return new Color(0x3300695C);
                                        }

                                        if (states.Contains(WidgetState.Selected))
                                        {
                                            return new Color(0xFF00695C);
                                        }

                                        return new Color(0xFFB0BEC5);
                                    }),
                                    trackOutlineColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                    {
                                        return states.Contains(WidgetState.Selected)
                                            ? Colors.Transparent
                                            : new Color(0xFF455A64);
                                    }),
                                    trackOutlineWidth: WidgetStateProperty<double?>.All(2)),
                                title: "Custom colors",
                                subtitle: "thumb/track/outline overrides"),
                        ])),
                new Text("CupertinoSwitch direct port", fontSize: 16, color: Colors.Black),
                BuildSwitchRow(
                    toggle: new CupertinoSwitch(
                        value: _value,
                        onChanged: _enabled ? OnValueChanged : null),
                    title: "Cupertino default",
                    subtitle: "iOS geometry, drag thresholds, focus ring, and haptics"),
                BuildSwitchRow(
                    toggle: new CupertinoSwitch(
                        value: _value,
                        onChanged: _enabled ? OnValueChanged : null,
                        activeTrackColor: new Color(0xFF00695C),
                        inactiveTrackColor: new Color(0xFFB0BEC5),
                        thumbColor: new Color(0xFFE8F5E9),
                        inactiveThumbColor: new Color(0xFFB2DFDB),
                        trackOutlineColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                            states.Contains(WidgetState.Selected)
                                ? Colors.Transparent
                                : new Color(0xFF455A64)),
                        trackOutlineWidth: WidgetStateProperty<double?>.All(2.0),
                        thumbIcon: _showThumbIcons
                            ? WidgetStateProperty<Icon?>.ResolveWith(states =>
                                states.Contains(WidgetState.Selected)
                                    ? new Icon(Icons.Check, size: 14.0)
                                    : new Icon(Icons.Close, size: 14.0))
                            : null),
                    title: "Cupertino custom",
                    subtitle: "track/thumb/outline/icon state overrides"),
            ]);
    }

    private Widget BuildSwitchRow(Widget toggle, string title, string subtitle)
    {
        return new Container(
            padding: new Thickness(10, 8),
            decoration: new BoxDecoration(
                Color: new Color(0xFFF1F4F9),
                BorderRadius: BorderRadius.Circular(10),
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(new Color(0xFFD6DEEA), 1))),
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
                                new Text(subtitle, fontSize: 12, color: new Color(0x8A000000)),
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

    private void ToggleThumbIcons()
    {
        SetState(() => _showThumbIcons = !_showThumbIcons);
    }

    private void ToggleMaterialVersion()
    {
        SetState(() => _useMaterial3 = !_useMaterial3);
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
            _useMaterial3 = true;
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
