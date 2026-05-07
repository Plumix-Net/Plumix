using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/circular_progress_indicator_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CircularProgressIndicatorDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CircularProgressIndicatorDemoPageState();
    }
}

internal sealed class CircularProgressIndicatorDemoPageState : State
{
    private static readonly double[] WidgetTrackGapOptions = [0.0, 2.0, 4.0];

    private bool _useMaterial3 = true;
    private bool _useYear2023 = true;
    private bool _determinate = true;
    private bool _useThemeOverrides;
    private bool _useWidgetOverrides;
    private int _widgetTrackGapIndex;
    private double _progress = 0.35;

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var themedData = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            ProgressIndicatorTheme = _useThemeOverrides
                ? new ProgressIndicatorThemeData(
                    Color: Color.Parse("#FF1565C0"),
                    CircularTrackColor: Color.Parse("#FFC5CAE9"),
                    CircularStrokeWidth: 6,
                    CircularStrokeAlign: -1.0,
                    CircularConstraints: new BoxConstraints(MinWidth: 44, MinHeight: 44),
                    CircularStrokeCap: StrokeCap.Round,
                    TrackGap: 7.0,
                    Year2023: _useYear2023)
                : new ProgressIndicatorThemeData(
                    Year2023: _useYear2023)
        };

        return new Theme(
            data: themedData,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("CircularProgressIndicator baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Determinate/indeterminate behavior, M2/M3 defaults, year2023 toggle, theme/widget precedence, and circular trackGap/strokeCap/strokeAlign/constraints.",
                        fontSize: 14,
                        color: Color.Parse("#8A000000")),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useMaterial3 ? "M3" : "M2",
                                onTap: () => SetState(() => _useMaterial3 = !_useMaterial3),
                                width: 80,
                                background: Color.Parse("#FFE9F0FF")),
                            BuildControlButton(
                                label: _useYear2023 ? "2023" : "2024",
                                onTap: () => SetState(() => _useYear2023 = !_useYear2023),
                                width: 82,
                                background: Color.Parse("#FFFFF8E1")),
                            BuildControlButton(
                                label: _determinate ? "Determinate" : "Indeterminate",
                                onTap: () => SetState(() => _determinate = !_determinate),
                                width: 132,
                                background: Color.Parse("#FFE8F5E9")),
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
                                label: _useWidgetOverrides ? "Widget on" : "Widget off",
                                onTap: () => SetState(() => _useWidgetOverrides = !_useWidgetOverrides),
                                width: 118,
                                background: Color.Parse("#FFF0E8FF")),
                            BuildControlButton(
                                label: $"gap={GetWidgetTrackGap():0.#}",
                                onTap: () => SetState(CycleWidgetTrackGap),
                                width: 76,
                                background: Color.Parse("#FFEFF4FF")),
                            BuildControlButton(
                                label: "-",
                                onTap: () => SetState(() => _progress = Math.Max(0, _progress - 0.1)),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            BuildControlButton(
                                label: "+",
                                onTap: () => SetState(() => _progress = Math.Min(1, _progress + 0.1)),
                                width: 42,
                                background: Color.Parse("#FFFFF3E0")),
                            new Expanded(
                                child: new Text(
                                    $"value={_progress:0.00}",
                                    fontSize: 12,
                                    color: Color.Parse("#FF607D8B"))),
                        ]),
                    new Text(
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, year2023={(_useYear2023 ? "true" : "false")}, determinate={(_determinate ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, widget={(_useWidgetOverrides ? "true" : "false")}, gap={GetWidgetTrackGap():0.#}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildPreviewCard(
                                        title: "Indicator",
                                        subtitle: "Default-size indicator preview",
                                        indicator: BuildIndicator()),
                                    BuildPreviewCard(
                                        title: "Larger parent",
                                        subtitle: "Indicator centered in a larger host box",
                                        indicator: new SizedBox(
                                            width: 72,
                                            height: 72,
                                            child: new Align(
                                                alignment: Alignment.Center,
                                                child: BuildIndicator()))),
                                ]))),
                ]));
    }

    private Widget BuildPreviewCard(string title, string subtitle, Widget indicator)
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
                    new SizedBox(
                        height: 92,
                        child: new Align(
                            alignment: Alignment.Center,
                            child: indicator)),
                ]));
    }

    private CircularProgressIndicator BuildIndicator()
    {
        if (_useWidgetOverrides)
        {
            return new CircularProgressIndicator(
                value: _determinate ? _progress : null,
                color: Color.Parse("#FFB71C1C"),
                backgroundColor: Color.Parse("#FFFFCDD2"),
                strokeWidth: 8,
                strokeAlign: 1.0,
                constraints: new BoxConstraints(MinWidth: 56, MinHeight: 56),
                strokeCap: StrokeCap.Square,
                trackGap: GetWidgetTrackGap(),
                year2023: _useYear2023,
                semanticsLabel: "Widget override progress");
        }

        return new CircularProgressIndicator(
            value: _determinate ? _progress : null,
            trackGap: GetWidgetTrackGap(),
            year2023: _useYear2023,
            semanticsLabel: "Baseline progress");
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

    private double GetWidgetTrackGap()
    {
        return WidgetTrackGapOptions[_widgetTrackGapIndex];
    }

    private void CycleWidgetTrackGap()
    {
        _widgetTrackGapIndex = (_widgetTrackGapIndex + 1) % WidgetTrackGapOptions.Length;
    }
}
