using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/decorated_box_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class DecoratedBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new DecoratedBoxDemoPageState();
    }
}

internal sealed class DecoratedBoxDemoPageState : State
{
    private double _radius = 10;
    private double _borderWidth = 2;
    private bool _accentFill = true;

    public override Widget Build(BuildContext context)
    {
        var fillColor = _accentFill ? Color.Parse("#FF9DC4FF") : Color.Parse("#FFF3F3F3");
        var borderColor = _accentFill ? Color.Parse("#FF1D3557") : Color.Parse("#FF555555");

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("DecoratedBox + Border + Radius", fontSize: 20, color: Colors.Black),
                new Text(
                    "Adjust border width and corner radius; toggle fill theme.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Radius -", () => ChangeRadius(-4), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Radius +", () => ChangeRadius(4), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Border -", () => ChangeBorder(-1), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("Border +", () => ChangeBorder(1), width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(_accentFill ? "Fill: accent" : "Fill: neutral", ToggleFill, width: 128, colorHex: "#FFF3E8D8"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFE8EDF9"),
                    ]),
                new Text(
                    $"radius={_radius:0}, border={_borderWidth:0}, fill={(_accentFill ? "accent" : "neutral")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new Center(
                        child: new SizedBox(
                            width: 140,
                            height: 90,
                            child: new DecoratedBox(
                                decoration: new BoxDecoration(
                                    Color: fillColor,
                                    Border: new BorderSide(borderColor, _borderWidth),
                                    BorderRadius: BorderRadius.Circular(_radius)),
                                child: new Center(
                                    child: new Text("Decorated", fontSize: 14, color: Color.Parse("#FF14213D"))))))),
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

    private void ChangeRadius(double delta)
    {
        SetState(() => _radius = Math.Clamp(_radius + delta, 0, 44));
    }

    private void ChangeBorder(double delta)
    {
        SetState(() => _borderWidth = Math.Clamp(_borderWidth + delta, 0, 12));
    }

    private void ToggleFill()
    {
        SetState(() => _accentFill = !_accentFill);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _radius = 10;
            _borderWidth = 2;
            _accentFill = true;
        });
    }
}
