using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Painting;
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
    private bool _largeText;
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
                    ActiveTrackColor: new Color(0xFF2E7D32),
                    InactiveTrackColor: new Color(0xFFC8E6C9),
                    ThumbColor: new Color(0xFF1B5E20),
                    DisabledActiveTrackColor: new Color(0x66212121),
                    DisabledInactiveTrackColor: new Color(0x1F212121),
                    DisabledThumbColor: new Color(0x66212121),
                    TrackHeight: 6)
                : new SliderThemeData()
        };

        return new Theme(
            data: themedData,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Text("RangeSlider baseline", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                    new SizedBox(height: 8),
                    new Text(
                        "Two-thumb range mapping, continuous/discrete updates, drag/tap plus Tab focus "
                        + "between the two thumbs, and M2/M3 theme/widget color precedence.",
                        style: new TextStyle(FontSize: 14, Color: new Color(0x8A000000))),
                    new SizedBox(height: 10),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _enabled ? "Enabled" : "Disabled",
                                onTap: () => SetState(() => _enabled = !_enabled),
                                width: 96,
                                background: new Color(0xFFE9F0FF)),
                            BuildControlButton(
                                label: _discrete ? "Discrete" : "Continuous",
                                onTap: () => SetState(() => _discrete = !_discrete),
                                width: 112,
                                background: new Color(0xFFE8F5E9)),
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 76,
                                background: new Color(0xFFFFF8E1)),
                            BuildControlButton(
                                label: _useThemeOverrides ? "Theme on" : "Theme off",
                                onTap: () => SetState(() => _useThemeOverrides = !_useThemeOverrides),
                                width: 112,
                                background: new Color(0xFFEAF6F7)),
                        ]),
                    new SizedBox(height: 8),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useWidgetColorOverride ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetColorOverride = !_useWidgetColorOverride),
                                width: 118,
                                background: new Color(0xFFF0E8FF)),
                            BuildControlButton(
                                label: "S-",
                                onTap: () => NudgeStart(-0.1),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            BuildControlButton(
                                label: "S+",
                                onTap: () => NudgeStart(0.1),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            BuildControlButton(
                                label: "E-",
                                onTap: () => NudgeEnd(-0.1),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            BuildControlButton(
                                label: "E+",
                                onTap: () => NudgeEnd(0.1),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            new Expanded(
                                child: new Text(
                                    $"start={Fixed(_values.Start)}, end={Fixed(_values.End)}, status={_status}",
                                    style: new TextStyle(FontSize: 12, Color: new Color(0xFF607D8B)))),
                        ]),
                    new SizedBox(height: 10),
                    new Row(
                        children:
                        [
                            BuildControlButton(
                                label: _year2023 ? "2023 look" : "2024 look",
                                onTap: () => SetState(() => _year2023 = !_year2023),
                                width: 96,
                                background: new Color(0xFFEAF6F7)),
                        ]),
                    new SizedBox(height: 10),
                    BuildControlButton(
                        label: _largeText ? "Text 2x" : "Text 1x",
                        onTap: () => SetState(() => _largeText = !_largeText),
                        width: 96,
                        background: new Color(0xFFFFF8E1)),
                    new SizedBox(height: 10),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                children:
                                [
                                    BuildPreviewCard(
                                        title: "LTR",
                                        subtitle: "Left-to-right mapping and thumb order",
                                        textDirection: TextDirection.Ltr),
                                    new SizedBox(height: 14),
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
            color: new Color(0xFFF7F9FC),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Text(title, style: new TextStyle(FontSize: 14, Color: Colors.Black)),
                    new SizedBox(height: 4),
                    new Text(subtitle, style: new TextStyle(FontSize: 12, Color: new Color(0x8A000000))),
                    new SizedBox(height: 8),
                    new Directionality(
                        textDirection: textDirection,
                        child: new Builder(context => new MediaQuery(
                            data: MediaQuery.Of(context).CopyWith(
                                textScaler: TextScaler.Linear(_largeText ? 2.0 : 1.0)),
                            child: BuildRangeSlider()))),
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
                $"{RoundAway(_values.Start * 100)}",
                $"{RoundAway(_values.End * 100)}"),
            activeColor: _useWidgetColorOverride ? new Color(0xFFB71C1C) : null,
            inactiveColor: _useWidgetColorOverride ? new Color(0xFFFFCDD2) : null,
            year2023: _year2023,
            onChanged: _enabled ? HandleValuesChanged : null,
            onChangeStart: values => SetState(
                () => _status = $"start {Fixed(values.Start)}-{Fixed(values.End)}"),
            onChangeEnd: values => SetState(
                () => _status = $"end {Fixed(values.Start)}-{Fixed(values.End)}"),
            semanticFormatterCallback: value => $"{RoundAway(value * 100)}%");
    }

    private void HandleValuesChanged(RangeValues values)
    {
        SetState(() =>
        {
            _values = values;
            _status = $"change {Fixed(values.Start)}-{Fixed(values.End)}";
        });
    }

    private void NudgeStart(double delta)
    {
        SetState(() =>
        {
            double nextStart = Math.Min(_values.End, Math.Max(0, _values.Start + delta));
            _values = new RangeValues(nextStart, _values.End);
            _status = $"manual {Fixed(_values.Start)}-{Fixed(_values.End)}";
        });
    }

    private void NudgeEnd(double delta)
    {
        SetState(() =>
        {
            double nextEnd = Math.Max(_values.Start, Math.Min(1, _values.End + delta));
            _values = new RangeValues(_values.Start, nextEnd);
            _status = $"manual {Fixed(_values.Start)}-{Fixed(_values.End)}";
        });
    }

    private static string Fixed(double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static long RoundAway(double value)
    {
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
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
                child: new Text(label, style: new TextStyle(FontSize: 12)),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(0, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }
}
