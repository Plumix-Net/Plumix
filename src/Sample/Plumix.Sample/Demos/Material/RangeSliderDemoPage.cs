using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/range_slider_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class RangeSliderDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new RangeSliderDemoPageState();
    }
}

internal sealed class RangeSliderDemoPageState : State
{
    private bool _enabled = true;
    private bool _discrete;
    private bool _useThemeOverrides;
    private bool _useWidgetColorOverride;
    private bool _useMaterial3 = true;
    private bool _year2023 = true;
    private RangeValues _values = new(0.2, 0.7);
    private string _status = "idle";

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var themedData = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            SliderTheme = _useThemeOverrides
                ? new SliderThemeData(
                    ActiveTrackColor: Color.Parse("#FF2E7D32"),
                    InactiveTrackColor: Color.Parse("#FFC8E6C9"),
                    ThumbColor: Color.Parse("#FF1B5E20"),
                    DisabledActiveTrackColor: Color.Parse("#66212121"),
                    DisabledInactiveTrackColor: Color.Parse("#1F212121"),
                    DisabledThumbColor: Color.Parse("#66212121"),
                    TrackHeight: 6,
                    ThumbRadius: 11)
                : new SliderThemeData()
        };

        return new Theme(
            data: themedData,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("RangeSlider baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Two-thumb range mapping, continuous/discrete updates, drag/tap plus Tab focus "
                        + "between the two thumbs, and M2/M3 theme/widget color precedence.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _enabled ? "Enabled" : "Disabled",
                                onTap: () => SetState(() => _enabled = !_enabled),
                                width: 96,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _discrete ? "Discrete" : "Continuous",
                                onTap: () => SetState(() => _discrete = !_discrete),
                                width: 112,
                                background: Color.Parse("#FFE8F5E9")),
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 76,
                                background: Color.Parse("#FFFFF8E1")),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: Color.Parse("#FFEAF6F7")),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useWidgetColorOverride ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetColorOverride = !_useWidgetColorOverride),
                                width: 118,
                                background: Color.Parse("#FFF0E8FF")),
                            BuildControlButton(
                                label: "S-",
                                onTap: () => NudgeStart(-0.1),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            BuildControlButton(
                                label: "S+",
                                onTap: () => NudgeStart(0.1),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            BuildControlButton(
                                label: "E-",
                                onTap: () => NudgeEnd(-0.1),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            BuildControlButton(
                                label: "E+",
                                onTap: () => NudgeEnd(0.1),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            new Expanded(
                                child: new Text(
                                    $"start={_values.Start:0.00}, end={_values.End:0.00}, status={_status}",
                                    fontSize: 12,
                                    color: Color.Parse("#FF607D8B"))),
                        ]),
                    new Row(
                        children:
                        [
                            BuildControlButton(
                                label: _year2023 ? "2023 look" : "2024 look",
                                onTap: () => SetState(() => _year2023 = !_year2023),
                                width: 96,
                                background: Color.Parse("#FFEAF6F7")),
                        ]),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildPreviewCard(
                                        title: "LTR",
                                        subtitle: "Left-to-right mapping and thumb order",
                                        textDirection: TextDirection.Ltr),
                                    BuildPreviewCard(
                                        title: "RTL",
                                        subtitle: "Right-to-left mapping and thumb order",
                                        textDirection: TextDirection.Rtl),
                                ]))),
                ]));
    }

    private Widget BuildPreviewCard(string title, string subtitle, TextDirection textDirection)
    {
        return new Container(
            color: Color.Parse("#FFF7F9FC"),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(title, fontSize: 14, color: Colors.Black),
                    new Text(subtitle, fontSize: 12, color: Color.Parse("#8A000000")),
                    new Directionality(
                        textDirection: textDirection,
                        child: BuildRangeSlider()),
                ]));
    }

    private Widget BuildRangeSlider()
    {
        return new RangeSlider(
            values: _values,
            min: 0,
            max: 1,
            divisions: _discrete ? 5 : null,
            labels: new RangeLabels(
                $"{Math.Round(_values.Start * 100)}",
                $"{Math.Round(_values.End * 100)}"),
            activeColor: _useWidgetColorOverride ? Color.Parse("#FFB71C1C") : null,
            inactiveColor: _useWidgetColorOverride ? Color.Parse("#FFFFCDD2") : null,
            year2023: _year2023,
            onChanged: _enabled ? HandleValuesChanged : null,
            onChangeStart: values => SetState(() => _status = $"start {values.Start:0.00}-{values.End:0.00}"),
            onChangeEnd: values => SetState(() => _status = $"end {values.Start:0.00}-{values.End:0.00}"),
            semanticFormatterCallback: value => $"{Math.Round(value * 100):0}%");
    }

    private void HandleValuesChanged(RangeValues values)
    {
        SetState(() =>
        {
            _values = values;
            _status = $"change {values.Start:0.00}-{values.End:0.00}";
        });
    }

    private void NudgeStart(double delta)
    {
        SetState(() =>
        {
            double nextStart = Math.Clamp(_values.Start + delta, 0, _values.End);
            _values = new RangeValues(nextStart, _values.End);
            _status = $"manual {nextStart:0.00}-{_values.End:0.00}";
        });
    }

    private void NudgeEnd(double delta)
    {
        SetState(() =>
        {
            double nextEnd = Math.Clamp(_values.End + delta, _values.Start, 1);
            _values = new RangeValues(_values.Start, nextEnd);
            _status = $"manual {_values.Start:0.00}-{nextEnd:0.00}";
        });
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
                child: new Text(label, fontSize: 12)));
    }
}
