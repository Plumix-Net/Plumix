using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/proxy_widgets_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ProxyWidgetsDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ProxyWidgetsDemoPageState();
    }
}

internal sealed class ProxyWidgetsDemoPageState : State
{
    private double _opacity = 0.8;
    private double _shiftX;
    private bool _tightClip = true;

    public override Widget Build(BuildContext context)
    {
        var clip = _tightClip
            ? new Rect(0, 0, 120, 80)
            : new Rect(0, 0, 190, 110);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Proxy widgets: Opacity + Transform + ClipRect", fontSize: 20, color: Colors.Black),
                new Text(
                    "Use controls to fade a high-contrast black card over white canvas.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Opacity -", () => ChangeOpacity(-0.3), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Opacity +", () => ChangeOpacity(0.3), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Left", () => Move(-20), width: 88, colorHex: "#FFF3E8D8"),
                        BuildButton("Right", () => Move(20), width: 88, colorHex: "#FFF3E8D8"),
                        BuildButton(_tightClip ? "Clip: tight" : "Clip: wide", ToggleClip, width: 104, colorHex: "#FFE8EDF9"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Opacity 0", () => SetOpacity(0), width: 96, colorHex: "#FFF6E0E0"),
                        BuildButton("Opacity 1", () => SetOpacity(1), width: 96, colorHex: "#FFE0F0E7"),
                    ]),
                new Text(
                    $"opacity={_opacity:0.00}, shiftX={_shiftX:0}, clip={(_tightClip ? "tight" : "wide")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new ClipRect(
                        clipRect: clip,
                        child: new Plumix.Widgets.Transform(
                            transform: Matrix.CreateTranslation(_shiftX, 10),
                            child: new Opacity(
                                opacity: _opacity,
                                child: new Container(
                                    width: 140,
                                    height: 90,
                                    color: Color.Parse("#FF111111"),
                                    padding: new Thickness(8),
                                    child: new Text("Layer", fontSize: 14, color: Colors.White)))))),
            ]);
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private void ChangeOpacity(double delta)
    {
        SetState(() => _opacity = Math.Clamp(_opacity + delta, 0, 1));
    }

    private void SetOpacity(double value)
    {
        SetState(() => _opacity = Math.Clamp(value, 0, 1));
    }

    private void Move(double delta)
    {
        SetState(() => _shiftX = Math.Clamp(_shiftX + delta, -40, 80));
    }

    private void ToggleClip()
    {
        SetState(() => _tightClip = !_tightClip);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _opacity = 0.8;
            _shiftX = 0;
            _tightClip = true;
        });
    }
}
