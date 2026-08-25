using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_slider_demo_page.dart
// (exact sample parity)

public sealed class CupertinoSliderDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoSliderDemoPageState();
}

internal sealed class CupertinoSliderDemoPageState : State
{
    private double _value = 0.35;
    private double _rangedValue = 30.0;
    private double _discreteValue = 2.0;
    private bool _enabled = true;
    private bool _dark;
    private bool _rightToLeft;
    private int _changes;
    private string _lifecycle = "idle";

    public override Widget Build(BuildContext context)
    {
        return new CupertinoTheme(
            new CupertinoThemeData(brightness: _dark ? PlatformBrightness.Dark : PlatformBrightness.Light),
            new Directionality(
                _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr,
                new Container(
                    color: _dark ? Color.Parse("#FF1C1C1E") : CupertinoColors.White,
                    padding: new Thickness(12.0),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 12.0,
                        children:
                        [
                            new Text("CupertinoSlider", fontSize: 20.0, color: TitleColor),
                            new Text(
                                "Continuous and discrete values, min/max ranges, thumb and active "
                                + "colors, disabled state, and LTR/RTL dragging.",
                                fontSize: 14.0,
                                color: SubtitleColor),
                            new Wrap(
                                spacing: 8.0,
                                runSpacing: 8.0,
                                children:
                                [
                                    BuildControl(_enabled ? "Enabled" : "Disabled", () => _enabled = !_enabled),
                                    BuildControl(_dark ? "Dark" : "Light", () => _dark = !_dark),
                                    BuildControl(
                                        _rightToLeft ? "RTL" : "LTR",
                                        () => _rightToLeft = !_rightToLeft),
                                    BuildControl("Reset", Reset),
                                ]),
                            new Text(
                                $"value={Format(_value)}, ranged={Format(_rangedValue)}, "
                                + $"discrete={Format(_discreteValue)}, changes={_changes}, "
                                + $"lifecycle={_lifecycle}",
                                fontSize: 12.0,
                                color: SubtitleColor),
                            BuildRow(
                                new CupertinoSlider(
                                    value: _value,
                                    onChanged: _enabled ? OnValueChanged : null,
                                    onChangeStart: _ => SetState(() => _lifecycle = "dragging"),
                                    onChangeEnd: _ => SetState(() => _lifecycle = "idle")),
                                "Continuous",
                                "0.0 to 1.0, theme primary color"),
                            BuildRow(
                                new CupertinoSlider(
                                    value: _rangedValue,
                                    onChanged: _enabled ? OnRangedChanged : null,
                                    min: 10.0,
                                    max: 90.0,
                                    activeColor: CupertinoColors.SystemGreen),
                                "Ranged",
                                "min 10, max 90, activeColor override"),
                            BuildRow(
                                new CupertinoSlider(
                                    value: _discreteValue,
                                    onChanged: _enabled ? OnDiscreteChanged : null,
                                    min: 0.0,
                                    max: 5.0,
                                    divisions: 5,
                                    activeColor: CupertinoColors.SystemPurple,
                                    thumbColor: CupertinoColors.SystemYellow),
                                "Discrete",
                                "5 divisions, animated track, custom thumb"),
                        ]))));
    }

    private Color TitleColor => _dark ? CupertinoColors.White : CupertinoColors.Black;

    private Color SubtitleColor => _dark ? Color.Parse("#99FFFFFF") : Color.Parse("#8A000000");

    private Widget BuildRow(Widget slider, string title, string subtitle)
    {
        return new Container(
            padding: new Thickness(10.0, 8.0),
            decoration: new BoxDecoration(
                Color: _dark ? Color.Parse("#FF2C2C2E") : Color.Parse("#FFF1F4F9"),
                BorderRadius: BorderRadius.Circular(10.0),
                Border: Border.FromBorderSide(
                    new BorderSide(_dark ? Color.Parse("#FF3A3A3C") : Color.Parse("#FFD6DEEA"), 1.0))),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 6.0,
                children:
                [
                    new Text(title, fontSize: 13.0, color: TitleColor),
                    new Text(subtitle, fontSize: 12.0, color: SubtitleColor),
                    new Align(alignment: Alignment.CenterLeft, child: slider),
                ]));
    }

    private Widget BuildControl(string label, Action onPressed)
    {
        return new CupertinoButton(
            onPressed: () => SetState(onPressed),
            padding: new Thickness(12.0, 6.0),
            child: new Text(label, fontSize: 12.0, color: CupertinoColors.ActiveBlue.Value));
    }

    private static string Format(double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void Reset()
    {
        _value = 0.35;
        _rangedValue = 30.0;
        _discreteValue = 2.0;
        _changes = 0;
        _lifecycle = "idle";
    }

    private void OnValueChanged(double value)
    {
        SetState(() =>
        {
            _value = value;
            _changes += 1;
        });
    }

    private void OnRangedChanged(double value)
    {
        SetState(() =>
        {
            _rangedValue = value;
            _changes += 1;
        });
    }

    private void OnDiscreteChanged(double value)
    {
        SetState(() =>
        {
            _discreteValue = value;
            _changes += 1;
        });
    }
}
