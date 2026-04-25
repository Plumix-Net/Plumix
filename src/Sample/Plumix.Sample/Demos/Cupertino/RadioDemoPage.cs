using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/radio_demo_page.dart (exact sample parity)

namespace Plumix;

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
    private string? _materialGroupValue = "first";
    private int _materialChanges;
    private string? _adaptiveGroupValue = "adaptive-first";
    private int _adaptiveChanges;
    private TargetPlatform _adaptivePlatform = TargetPlatform.IOS;
    private bool _adaptiveUseCheckmarkStyle;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var radioTheme = baseTheme with
        {
            MaterialTapTargetSize = _shrinkWrapTapTarget
                ? MaterialTapTargetSize.ShrinkWrap
                : MaterialTapTargetSize.Padded
        };
        var adaptiveTheme = radioTheme with
        {
            Platform = _adaptivePlatform
        };

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Radio baseline + adaptive", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material Radio plus adaptive Cupertino probe with platform/checkmark toggles.",
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
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildControlButton(
                            label: $"Adaptive: {FormatPlatform(_adaptivePlatform)}",
                            onTap: CycleAdaptivePlatform,
                            width: 156,
                            background: Color.Parse("#FFE7F4FF")),
                        BuildControlButton(
                            label: _adaptiveUseCheckmarkStyle ? "Adaptive: checkmark" : "Adaptive: dot",
                            onTap: ToggleAdaptiveCheckmarkStyle,
                            width: 164,
                            background: Color.Parse("#FFEFE7FF")),
                    ]),
                new Text(
                    $"enabled={(_enabled ? "true" : "false")}, toggleable={(_toggleable ? "true" : "false")}, materialValue={FormatValue(_materialGroupValue)}, materialChanges={_materialChanges}, adaptiveValue={FormatValue(_adaptiveGroupValue)}, adaptiveChanges={_adaptiveChanges}, adaptivePlatform={FormatPlatform(_adaptivePlatform)}, adaptiveStyle={(_adaptiveUseCheckmarkStyle ? "checkmark" : "dot")}, tapTarget={(_shrinkWrapTapTarget ? "shrinkWrap" : "padded")}",
                    fontSize: 12,
                    color: Color.Parse("#FF607D8B")),
                new Theme(
                    data: radioTheme,
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text(
                                "Material path",
                                fontSize: 13,
                                color: Color.Parse("#FF37474F")),
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "first",
                                    groupValue: _materialGroupValue,
                                    onChanged: _enabled ? OnMaterialChanged : null,
                                    toggleable: _toggleable),
                                title: "Default radio #1",
                                subtitle: "value: first"),
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "second",
                                    groupValue: _materialGroupValue,
                                    onChanged: _enabled ? OnMaterialChanged : null,
                                    toggleable: _toggleable),
                                title: "Default radio #2",
                                subtitle: "value: second"),
                            BuildRadioRow(
                                radio: new Radio<string>(
                                    value: "custom",
                                    groupValue: _materialGroupValue,
                                    onChanged: _enabled ? OnMaterialChanged : null,
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
                            new Text(
                                "Adaptive path",
                                fontSize: 13,
                                color: Color.Parse("#FF37474F")),
                            new Theme(
                                data: adaptiveTheme,
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 8,
                                    children:
                                    [
                                        BuildRadioRow(
                                            radio: Radio<string>.Adaptive(
                                                value: "adaptive-first",
                                                groupValue: _adaptiveGroupValue,
                                                onChanged: _enabled ? OnAdaptiveChanged : null,
                                                toggleable: _toggleable),
                                            title: "Adaptive default #1",
                                            subtitle: "value: adaptive-first"),
                                        BuildRadioRow(
                                            radio: Radio<string>.Adaptive(
                                                value: "adaptive-second",
                                                groupValue: _adaptiveGroupValue,
                                                onChanged: _enabled ? OnAdaptiveChanged : null,
                                                toggleable: _toggleable,
                                                activeColor: Color.Parse("#FF00695C"),
                                                fillColor: MaterialStateProperty<Color?>.All(Color.Parse("#FF8E24AA")),
                                                useCupertinoCheckmarkStyle: _adaptiveUseCheckmarkStyle),
                                            title: "Adaptive style probe",
                                            subtitle: "checkmark style + fillColor ignore on iOS/macOS"),
                                    ])),
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

    private void CycleAdaptivePlatform()
    {
        SetState(() =>
        {
            _adaptivePlatform = _adaptivePlatform switch
            {
                TargetPlatform.IOS => TargetPlatform.MacOS,
                TargetPlatform.MacOS => TargetPlatform.Android,
                _ => TargetPlatform.IOS,
            };
        });
    }

    private void ToggleAdaptiveCheckmarkStyle()
    {
        SetState(() => _adaptiveUseCheckmarkStyle = !_adaptiveUseCheckmarkStyle);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _enabled = true;
            _toggleable = true;
            _shrinkWrapTapTarget = false;
            _materialGroupValue = "first";
            _materialChanges = 0;
            _adaptiveGroupValue = "adaptive-first";
            _adaptiveChanges = 0;
            _adaptivePlatform = TargetPlatform.IOS;
            _adaptiveUseCheckmarkStyle = false;
        });
    }

    private void OnMaterialChanged(string? value)
    {
        SetState(() =>
        {
            _materialGroupValue = value;
            _materialChanges += 1;
        });
    }

    private void OnAdaptiveChanged(string? value)
    {
        SetState(() =>
        {
            _adaptiveGroupValue = value;
            _adaptiveChanges += 1;
        });
    }

    private static string FormatPlatform(TargetPlatform platform)
    {
        return platform switch
        {
            TargetPlatform.IOS => "iOS",
            TargetPlatform.MacOS => "macOS",
            TargetPlatform.Android => "Android",
            TargetPlatform.Fuchsia => "Fuchsia",
            TargetPlatform.Linux => "Linux",
            TargetPlatform.Windows => "Windows",
            _ => "Unknown",
        };
    }

    private static string FormatValue(string? value)
    {
        return value ?? "null";
    }
}
