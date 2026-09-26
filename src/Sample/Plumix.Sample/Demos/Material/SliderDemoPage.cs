using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/material/slider_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class SliderDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new SliderDemoPageState();
    }
}

internal sealed class SliderDemoPageState : State
{
    private bool _enabled = true;
    private bool _discrete;
    private bool _useThemeOverrides;
    private bool _useWidgetColorOverride;
    private bool _showSecondaryTrack = true;
    private bool _useSecondaryColorOverride;
    private bool _useMaterial3 = true;
    private bool _year2023 = true;
    private bool _tapOnly;
    private bool _customShape;
    private double _value = 0.35;
    private double _secondaryTrackValue = 0.7;
    private string _status = "idle";

    public override Widget Build(BuildContext context)
    {
        var baseTheme = Theme.Of(context);
        var themedData = baseTheme with
        {
            UseMaterial3 = _useMaterial3,
            SliderTheme = new SliderThemeData(
                ActiveTrackColor: _useThemeOverrides ? new Color(0xFF1565C0) : null,
                InactiveTrackColor: _useThemeOverrides ? new Color(0xFFC5CAE9) : null,
                ThumbColor: _useThemeOverrides ? new Color(0xFF0D47A1) : null,
                DisabledActiveTrackColor: _useThemeOverrides ? new Color(0x66212121) : null,
                DisabledInactiveTrackColor: _useThemeOverrides ? new Color(0x1F212121) : null,
                DisabledThumbColor: _useThemeOverrides ? new Color(0x66212121) : null,
                TrackHeight: _useThemeOverrides ? 6 : null,
                ThumbShape: _customShape ? new DemoSliderThumbShape() : null)
        };

        return new Theme(
            data: themedData,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Slider baseline", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Continuous/discrete value mapping, drag/tap/keyboard updates, M2/M3 defaults, and theme/widget color precedence.",
                        fontSize: 14,
                        color: new Color(0x8A000000)),
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
                                label: _showSecondaryTrack ? "Secondary on" : "Secondary off",
                                onTap: () => SetState(() => _showSecondaryTrack = !_showSecondaryTrack),
                                width: 132,
                                background: new Color(0xFFE8F6EE)),
                            BuildControlButton(
                                label: "-",
                                onTap: () => SetState(() => _value = Math.Max(0, _value - 0.1)),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            BuildControlButton(
                                label: "+",
                                onTap: () => SetState(() => _value = Math.Min(1, _value + 0.1)),
                                width: 42,
                                background: new Color(0xFFFFF3E0)),
                            new Expanded(
                                child: new Text(
                                    $"value={_value:0.00}, secondary={ResolveSecondaryLabel()}, status={_status}",
                                    fontSize: 12,
                                    color: new Color(0xFF607D8B))),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _useSecondaryColorOverride ? "Secondary color on" : "Secondary color off",
                                onTap: () => SetState(() => _useSecondaryColorOverride = !_useSecondaryColorOverride),
                                width: 164,
                                background: new Color(0xFFE9F0FF)),
                            BuildControlButton(
                                label: "Sec -",
                                onTap: () => SetState(() => _secondaryTrackValue = Math.Max(0, _secondaryTrackValue - 0.1)),
                                width: 56,
                                background: new Color(0xFFFFF3E0)),
                            BuildControlButton(
                                label: "Sec +",
                                onTap: () => SetState(() => _secondaryTrackValue = Math.Min(1, _secondaryTrackValue + 0.1)),
                                width: 56,
                                background: new Color(0xFFFFF3E0)),
                        ]),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildControlButton(
                                label: _year2023 ? "2023 look" : "2024 look",
                                onTap: () => SetState(() => _year2023 = !_year2023),
                                width: 96,
                                background: new Color(0xFFEAF6F7)),
                            BuildControlButton(
                                label: _tapOnly ? "Tap only" : "Tap + slide",
                                onTap: () => SetState(() => _tapOnly = !_tapOnly),
                                width: 104,
                                background: new Color(0xFFF0E8FF)),
                            BuildControlButton(
                                label: _customShape ? "Custom thumb" : "Default thumb",
                                onTap: () => SetState(() => _customShape = !_customShape),
                                width: 112,
                                background: new Color(0xFFE8F6EE)),
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
                                        subtitle: "Left-to-right mapping and keyboard direction",
                                        textDirection: TextDirection.Ltr),
                                    BuildPreviewCard(
                                        title: "RTL",
                                        subtitle: "Right-to-left mapping and keyboard direction",
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
                spacing: 8,
                children:
                [
                    new Text(title, fontSize: 14, color: Colors.Black),
                    new Text(subtitle, fontSize: 12, color: new Color(0x8A000000)),
                    new Directionality(
                        textDirection: textDirection,
                        child: BuildSlider()),
                ]));
    }

    private Widget BuildSlider()
    {
        return new Slider(
            value: _value,
            min: 0,
            max: 1,
            divisions: _discrete ? 5 : null,
            label: $"{Math.Round(_value * 100)}",
            secondaryTrackValue: _showSecondaryTrack ? _secondaryTrackValue : null,
            activeColor: _useWidgetColorOverride ? new Color(0xFFB71C1C) : null,
            inactiveColor: _useWidgetColorOverride ? new Color(0xFFFFCDD2) : null,
            secondaryActiveColor: _useSecondaryColorOverride ? new Color(0xFF1B5E20) : null,
            thumbColor: _useWidgetColorOverride ? new Color(0xFF880E4F) : null,
            allowedInteraction: _tapOnly ? SliderInteraction.TapOnly : SliderInteraction.TapAndSlide,
            showValueIndicator: ShowValueIndicator.OnlyForDiscrete,
            year2023: _year2023,
            onChanged: _enabled ? HandleValueChanged : null,
            onChangeStart: value => SetState(() => _status = $"start {value:0.00}"),
            onChangeEnd: value => SetState(() => _status = $"end {value:0.00}"),
            semanticFormatterCallback: value => $"{Math.Round(value * 100)} percent");
    }

    private void HandleValueChanged(double value)
    {
        SetState(() =>
        {
            _value = value;
            _status = $"change {value:0.00}";
        });
    }

    private string ResolveSecondaryLabel()
    {
        return _showSecondaryTrack ? _secondaryTrackValue.ToString("0.00") : "off";
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

internal sealed class DemoSliderThumbShape : SliderComponentShape
{
    public override Size GetPreferredSize(bool isEnabled, bool isDiscrete) => new(20.0, 20.0);

    public override void Paint(
        PaintingContext context,
        Point center,
        Animation<double> activationAnimation,
        Animation<double> enableAnimation,
        bool isDiscrete,
        TextPainter labelPainter,
        RenderBox parentBox,
        SliderThemeData sliderTheme,
        TextDirection textDirection,
        double value,
        double textScaleFactor,
        Size sizeWithOverflow)
    {
        Color color = enableAnimation.Value >= 0.5
            ? sliderTheme.ThumbColor ?? Colors.Blue
            : sliderTheme.DisabledThumbColor ?? Colors.Gray;
        context.Canvas.DrawRectangle(
            brush: new SolidColorBrush(color),
            pen: null,
            rect: new Rect(center.X - 10.0, center.Y - 10.0, 20.0, 20.0),
            radiusX: 5.0,
            radiusY: 5.0);
    }
}
