using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_radio_demo_page.dart
// (exact sample parity)

public sealed class CupertinoRadioDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoRadioDemoPageState();
}

internal sealed class CupertinoRadioDemoPageState : State
{
    private string? _groupValue = "lafayette";
    private bool _enabled = true;
    private bool _toggleable;
    private bool _useCheckmarkStyle;
    private bool _dark;
    private int _changes;

    public override Widget Build(BuildContext context)
    {
        return new CupertinoTheme(
            new CupertinoThemeData(brightness: _dark ? PlatformBrightness.Dark : PlatformBrightness.Light),
            new Container(
                color: _dark ? Color.Parse("#FF1C1C1E") : CupertinoColors.White,
                padding: new Thickness(12.0),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12.0,
                    children:
                    [
                        new Text("CupertinoRadio", fontSize: 20.0, color: TitleColor),
                        new Text(
                            "RadioGroup selection, toggleable deselection, checkmark style, "
                            + "disabled and dark-mode painting.",
                            fontSize: 14.0,
                            color: SubtitleColor),
                        new Wrap(
                            spacing: 8.0,
                            runSpacing: 8.0,
                            children:
                            [
                                BuildControl(_enabled ? "Enabled" : "Disabled", () => _enabled = !_enabled),
                                BuildControl(
                                    _toggleable ? "Toggleable" : "No toggle",
                                    () => _toggleable = !_toggleable),
                                BuildControl(
                                    _useCheckmarkStyle ? "Checkmark" : "Dot",
                                    () => _useCheckmarkStyle = !_useCheckmarkStyle),
                                BuildControl(_dark ? "Dark" : "Light", () => _dark = !_dark),
                            ]),
                        new Text(
                            $"value={_groupValue ?? "null"}, changes={_changes}",
                            fontSize: 12.0,
                            color: SubtitleColor),
                        new RadioGroup<string>(
                            groupValue: _groupValue,
                            onChanged: OnChanged,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 8.0,
                                children:
                                [
                                    BuildRow("lafayette", "Lafayette", "default colors"),
                                    BuildRow("jefferson", "Jefferson", "default colors"),
                                    BuildRow(
                                        "custom",
                                        "Custom colors",
                                        "activeColor + inactiveColor + fillColor",
                                        activeColor: CupertinoColors.SystemGreen.Value,
                                        inactiveColor: Color.Parse("#FFEFEFF4"),
                                        fillColor: CupertinoColors.SystemYellow.Value),
                                ])),
                    ])));
    }

    private Color TitleColor => _dark ? CupertinoColors.White : CupertinoColors.Black;

    private Color SubtitleColor => _dark ? Color.Parse("#99FFFFFF") : Color.Parse("#8A000000");

    private Widget BuildRow(
        string value,
        string title,
        string subtitle,
        Color? activeColor = null,
        Color? inactiveColor = null,
        Color? fillColor = null)
    {
        return new Container(
            padding: new Thickness(10.0, 8.0),
            decoration: new BoxDecoration(
                Color: _dark ? Color.Parse("#FF2C2C2E") : Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10.0),
                Border: Border.FromBorderSide(
                    new BorderSide(_dark ? Color.Parse("#FF3A3A3C") : Color.Parse("#FFD6DEEA"), 1.0))),
            child: new Row(
                spacing: 10.0,
                children:
                [
                    new CupertinoRadio<string>(
                        value: value,
                        enabled: _enabled,
                        toggleable: _toggleable,
                        useCheckmarkStyle: _useCheckmarkStyle,
                        activeColor: activeColor,
                        inactiveColor: inactiveColor,
                        fillColor: fillColor),
                    new Expanded(
                        child: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 2.0,
                            children:
                            [
                                new Text(title, fontSize: 13.0, color: TitleColor),
                                new Text(subtitle, fontSize: 12.0, color: SubtitleColor),
                            ])),
                ]));
    }

    private Widget BuildControl(string label, Action onPressed)
    {
        return new CupertinoButton(
            onPressed: () => SetState(onPressed),
            padding: new Thickness(12.0, 6.0),
            child: new Text(label, fontSize: 12.0, color: CupertinoColors.ActiveBlue.Value));
    }

    private void OnChanged(string? value)
    {
        SetState(() =>
        {
            _groupValue = value;
            _changes += 1;
        });
    }
}
