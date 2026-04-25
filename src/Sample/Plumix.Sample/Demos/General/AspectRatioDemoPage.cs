using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/aspect_ratio_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class AspectRatioDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AspectRatioDemoPageState();
    }
}

internal sealed class AspectRatioDemoPageState : State
{
    private double _aspectRatio = 16.0 / 9.0;
    private int _spacerFlex = 1;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AspectRatio + Spacer", fontSize: 20, color: Colors.Black),
                new Text(
                    "AspectRatio enforces a tight ratio box; Spacer reserves remaining Row space by flex.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("16:9", () => SetAspectRatio(16.0 / 9.0), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("4:3", () => SetAspectRatio(4.0 / 3.0), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("1:1", () => SetAspectRatio(1.0), width: 88, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Flex 1", () => SetSpacerFlex(1), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("Flex 2", () => SetSpacerFlex(2), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("Flex 3", () => SetSpacerFlex(3), width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Text(
                    $"ratio={FormatRatio(_aspectRatio)}, spacerFlex={_spacerFlex} (second spacer fixed=1)",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 190,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 10,
                        children:
                        [
                            new AspectRatio(
                                aspectRatio: _aspectRatio,
                                child: new Container(
                                    decoration: new BoxDecoration(
                                        Color: Color.Parse("#FFCCE3FF"),
                                        Border: new BorderSide(Color.Parse("#FF1D3557"), 2),
                                        BorderRadius: BorderRadius.Circular(10)),
                                    child: new Center(
                                        child: new Text("Aspect preview", fontSize: 14, color: Colors.Black)))),
                            new Container(
                                height: 44,
                                color: Colors.White,
                                padding: new Thickness(8, 6, 8, 6),
                                child: new Row(
                                    children:
                                    [
                                        new Container(
                                            width: 42,
                                            height: 24,
                                            color: Color.Parse("#FF1D3557"),
                                            child: new Center(
                                                child: new Text("L", fontSize: 12, color: Colors.White))),
                                        new Spacer(flex: _spacerFlex),
                                        new Container(
                                            width: 28,
                                            height: 24,
                                            color: Color.Parse("#FF2A9D8F"),
                                            child: new Center(
                                                child: new Text("M", fontSize: 12, color: Colors.White))),
                                        new Spacer(),
                                        new Container(
                                            width: 54,
                                            height: 24,
                                            color: Color.Parse("#FF457B9D"),
                                            child: new Center(
                                                child: new Text("R", fontSize: 12, color: Colors.White))),
                                    ])),
                        ])),
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

    private void SetAspectRatio(double value)
    {
        SetState(() => _aspectRatio = value);
    }

    private void SetSpacerFlex(int value)
    {
        SetState(() => _spacerFlex = value);
    }

    private static string FormatRatio(double ratio)
    {
        if (Math.Abs(ratio - (16.0 / 9.0)) < 0.001)
        {
            return "16:9";
        }

        if (Math.Abs(ratio - (4.0 / 3.0)) < 0.001)
        {
            return "4:3";
        }

        if (Math.Abs(ratio - 1.0) < 0.001)
        {
            return "1:1";
        }

        return ratio.ToString("0.##");
    }
}
