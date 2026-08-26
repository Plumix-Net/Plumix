using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/linear_progress_indicator_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class LinearProgressIndicatorDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new LinearProgressIndicatorDemoPageState();
    }
}

internal sealed class LinearProgressIndicatorDemoPageState : State
{
    private bool _useMaterial3 = true;
    private bool _useYear2023 = true;
    private bool _determinate = true;
    private bool _useThemeOverrides;
    private bool _useWidgetOverrides;
    private bool _useValueColorOverride;
    private double _progress = 0.35;
    private static readonly AlwaysStoppedAnimation<Color?> ValueColorOverride = new(Color.Parse("#FF1B5E20"));

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var themedData = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            ProgressIndicatorTheme = _useThemeOverrides
                ? new ProgressIndicatorThemeData(
                    Color: Color.Parse("#FF1565C0"),
                    LinearTrackColor: Color.Parse("#FFC5CAE9"),
                    LinearMinHeight: 6,
                    BorderRadius: BorderRadius.Circular(3),
                    StopIndicatorColor: Color.Parse("#FF0D47A1"),
                    StopIndicatorRadius: 3,
                    TrackGap: 6,
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
                    new Text("LinearProgressIndicator baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Determinate/indeterminate behavior, M2/M3 defaults, year2023 toggle, theme/widget precedence, valueColor priority, track-gap/stop-dot styling, and RTL paint direction.",
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
                                label: _useValueColorOverride ? "valueColor on" : "valueColor off",
                                onTap: () => SetState(() => _useValueColorOverride = !_useValueColorOverride),
                                width: 126,
                                background: Color.Parse("#FFE4F7E8")),
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
                        $"useMaterial3={(_useMaterial3 ? "true" : "false")}, year2023={(_useYear2023 ? "true" : "false")}, determinate={(_determinate ? "true" : "false")}, theme={(_useThemeOverrides ? "true" : "false")}, widget={(_useWidgetOverrides ? "true" : "false")}, valueColor={(_useValueColorOverride ? "true" : "false")}",
                        fontSize: 12,
                        color: Color.Parse("#FF607D8B")),
                    new Expanded(
                        child: new SingleChildScrollView(
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 14,
                                children:
                                [
                                    BuildLtrPreview(),
                                    BuildRtlPreview(),
                                ]))),
                ]));
    }

    private Widget BuildLtrPreview()
    {
        var indicator = BuildIndicator();
        return BuildPreviewCard(
            title: "LTR",
            subtitle: "Left-to-right fill and indeterminate movement",
            indicator: indicator);
    }

    private Widget BuildRtlPreview()
    {
        var indicator = new Directionality(
            textDirection: TextDirection.Rtl,
            child: BuildIndicator());

        return BuildPreviewCard(
            title: "RTL",
            subtitle: "Right-to-left fill and indeterminate movement",
            indicator: indicator);
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
                        height: 28,
                        child: new Align(
                            alignment: Alignment.Center,
                            child: indicator)),
                ]));
    }

    private LinearProgressIndicator BuildIndicator()
    {
        if (_useWidgetOverrides)
        {
            return new LinearProgressIndicator(
                value: _determinate ? _progress : null,
                color: Color.Parse("#FFB71C1C"),
                backgroundColor: Color.Parse("#FFFFCDD2"),
                minHeight: 8,
                borderRadius: BorderRadius.Circular(4),
                stopIndicatorColor: Color.Parse("#FF880E4F"),
                stopIndicatorRadius: 3.5,
                trackGap: 8,
                valueColor: _useValueColorOverride ? ValueColorOverride : null,
                year2023: _useYear2023,
                semanticsLabel: "Widget override progress");
        }

        return new LinearProgressIndicator(
            value: _determinate ? _progress : null,
            valueColor: _useValueColorOverride ? ValueColorOverride : null,
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
                child: new Text(label, fontSize: 12),
                style: TextButton.StyleFrom(
                    foregroundColor: Colors.Black,
                    backgroundColor: background,
                    padding: new Thickness(10, 8),
                    minimumSize: new Size(64, 36),
                    shape: new RoundedRectangleBorder(
                        borderRadius: BorderRadius.Circular(8)))));
    }
}
