using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/unconstrained_limited_box_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class UnconstrainedLimitedBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new UnconstrainedLimitedBoxDemoPageState();
    }
}

internal sealed class UnconstrainedLimitedBoxDemoPageState : State
{
    private Axis? _constrainedAxis;
    private double _maxWidth = 120;
    private double _maxHeight = 64;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("UnconstrainedBox + LimitedBox", fontSize: 20, color: Colors.Black),
                new Text(
                    "LimitedBox max values apply only on unbounded axes; UnconstrainedBox controls which axes become unbounded.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Axis none", () => SetConstrainedAxis(null), width: 94, colorHex: "#FFDCE3ED"),
                        BuildButton("Axis H", () => SetConstrainedAxis(Axis.Horizontal), width: 86, colorHex: "#FFDCE3ED"),
                        BuildButton("Axis V", () => SetConstrainedAxis(Axis.Vertical), width: 86, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("maxW 80", () => SetMaxWidth(80), width: 86, colorHex: "#FFE9F5EC"),
                        BuildButton("maxW 120", () => SetMaxWidth(120), width: 96, colorHex: "#FFE9F5EC"),
                        BuildButton("maxW 170", () => SetMaxWidth(170), width: 96, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("maxH 44", () => SetMaxHeight(44), width: 86, colorHex: "#FFF3E8D8"),
                        BuildButton("maxH 64", () => SetMaxHeight(64), width: 96, colorHex: "#FFF3E8D8"),
                        BuildButton("maxH 88", () => SetMaxHeight(88), width: 96, colorHex: "#FFF3E8D8"),
                    ]),
                new Text(
                    $"axis={AxisLabel(_constrainedAxis)}, maxWidth={_maxWidth:0}, maxHeight={_maxHeight:0}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 220,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Bounded parent: LimitedBox ignores maxWidth/maxHeight.", fontSize: 11, color: Colors.DimGray),
                            new Container(
                                height: 60,
                                color: Colors.White,
                                padding: new Thickness(6),
                                child: new Center(
                                    child: new LimitedBox(
                                        maxWidth: _maxWidth,
                                        maxHeight: _maxHeight,
                                        child: BuildProbeCard()))),
                            new Text("Inside UnconstrainedBox: unbounded axes use LimitedBox max values.", fontSize: 11, color: Colors.DimGray),
                            new Container(
                                height: 92,
                                color: Colors.White,
                                padding: new Thickness(6),
                                child: new Center(
                                    child: new UnconstrainedBox(
                                        alignment: Alignment.Center,
                                        constrainedAxis: _constrainedAxis,
                                        child: new LimitedBox(
                                            maxWidth: _maxWidth,
                                            maxHeight: _maxHeight,
                                            child: BuildProbeCard())))),
                        ])),
            ]);
    }

    private Widget BuildProbeCard()
    {
        return new Container(
            width: 190,
            height: 86,
            decoration: new BoxDecoration(
                Color: Color.Parse("#FFCCE3FF"),
                Border: new BorderSide(Color.Parse("#FF1D3557"), 2),
                BorderRadius: BorderRadius.Circular(10)),
            child: new Center(
                child: new Text(
                    $"child 190x86\nlimited to {_maxWidth:0}x{_maxHeight:0}",
                    fontSize: 11,
                    color: Colors.Black,
                    textAlign: TextAlign.Center)));
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

    private void SetConstrainedAxis(Axis? value)
    {
        SetState(() => _constrainedAxis = value);
    }

    private void SetMaxWidth(double value)
    {
        SetState(() => _maxWidth = value);
    }

    private void SetMaxHeight(double value)
    {
        SetState(() => _maxHeight = value);
    }

    private static string AxisLabel(Axis? axis)
    {
        return axis switch
        {
            Axis.Horizontal => "horizontal",
            Axis.Vertical => "vertical",
            null => "none",
            _ => axis?.ToString() ?? "none"
        };
    }
}
