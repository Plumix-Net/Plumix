using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/shape_borders_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ShapeBordersDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ShapeBordersDemoPageState();
    }
}

internal sealed class ShapeBordersDemoPageState : State
{
    private static readonly string[] ShapeNames =
    [
        "RoundedRectangle",
        "Stadium",
        "Circle",
        "Oval",
        "Beveled",
        "Continuous",
        "Star",
        "Polygon",
        "Border",
    ];

    private int _shapeIndex;
    private double _sideWidth = 4;
    private bool _lerpToCircle;

    public override Widget Build(BuildContext context)
    {
        var side = new BorderSide(Color.Parse("#FF1D3557"), _sideWidth);
        ShapeBorder shape = BuildShape(side);
        if (_lerpToCircle)
        {
            shape = ShapeBorder.Lerp(shape, new CircleBorder(side), 0.5)!;
        }

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ShapeBorder hierarchy", fontSize: 20, color: Colors.Black),
                new Text(
                    "Every shape paints its own outline and clips its own path.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Shape <", () => ChangeShape(-1), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Shape >", () => ChangeShape(1), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Side -", () => ChangeSide(-1), width: 78, colorHex: "#FFE9F5EC"),
                        BuildButton("Side +", () => ChangeSide(1), width: 78, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _lerpToCircle ? "Lerp: 50% circle" : "Lerp: off",
                            ToggleLerp,
                            width: 148,
                            colorHex: "#FFF3E8D8"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFE8EDF9"),
                    ]),
                new Text(
                    $"shape={ShapeNames[_shapeIndex]}, side={_sideWidth:0}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 160,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new Center(
                        child: new SizedBox(
                            width: 180,
                            height: 110,
                            child: new DecoratedBox(
                                decoration: new ShapeDecoration(
                                    Shape: shape,
                                    Color: Color.Parse("#FF9DC4FF")),
                                child: new ClipPath(
                                    clipper: new ShapeBorderClipper(shape),
                                    child: new Center(
                                        child: new Text(
                                            "Shaped",
                                            fontSize: 14,
                                            color: Color.Parse("#FF14213D")))))))),
            ]);
    }

    private ShapeBorder BuildShape(BorderSide side)
    {
        return ShapeNames[_shapeIndex] switch
        {
            "RoundedRectangle" => new RoundedRectangleBorder(side, BorderRadius.Circular(18)),
            "Stadium" => new StadiumBorder(side),
            "Circle" => new CircleBorder(side),
            "Oval" => new OvalBorder(side),
            "Beveled" => new BeveledRectangleBorder(side, BorderRadius.Circular(24)),
            "Continuous" => new ContinuousRectangleBorder(side, BorderRadius.Circular(28)),
            "Star" => new StarBorder(side, points: 6, innerRadiusRatio: 0.55, pointRounding: 0.2),
            "Polygon" => StarBorder.Polygon(side, sides: 6),
            _ => new Border(
                top: side,
                right: side.CopyWith(width: side.Width / 2.0),
                bottom: side,
                left: side.CopyWith(width: side.Width / 2.0)),
        };
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

    private void ChangeShape(int delta)
    {
        SetState(() => _shapeIndex = (_shapeIndex + delta + ShapeNames.Length) % ShapeNames.Length);
    }

    private void ChangeSide(double delta)
    {
        SetState(() => _sideWidth = Math.Clamp(_sideWidth + delta, 0, 12));
    }

    private void ToggleLerp()
    {
        SetState(() => _lerpToCircle = !_lerpToCircle);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _shapeIndex = 0;
            _sideWidth = 4;
            _lerpToCircle = false;
        });
    }
}
