using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/overflow_box_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class OverflowBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new OverflowBoxDemoPageState();
    }
}

internal sealed class OverflowBoxDemoPageState : State
{
    private OverflowBoxFit _fit = OverflowBoxFit.Max;
    private Alignment _alignment = Alignment.Center;
    private double? _maxWidth = 140;
    private double? _maxHeight = 70;
    private Size _requestedSize = new Size(90, 44);

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("OverflowBox + SizedOverflowBox", fontSize: 20, color: Colors.Black),
                new Text(
                    "OverflowBox overrides child constraints and can stay parent-sized; SizedOverflowBox keeps fixed own size while child may overflow.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Fit max", () => SetFit(OverflowBoxFit.Max), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("Fit child", () => SetFit(OverflowBoxFit.DeferToChild), width: 94, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("TopLeft", () => SetAlignment(Alignment.TopLeft), width: 92, colorHex: "#FFE9F5EC"),
                        BuildButton("Center", () => SetAlignment(Alignment.Center), width: 92, colorHex: "#FFE9F5EC"),
                        BuildButton("BottomRight", () => SetAlignment(Alignment.BottomRight), width: 112, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("max=parent", () => SetMax(null, null), width: 98, colorHex: "#FFF3E8D8"),
                        BuildButton("max=120x56", () => SetMax(120, 56), width: 98, colorHex: "#FFF3E8D8"),
                        BuildButton("max=160x92", () => SetMax(160, 92), width: 98, colorHex: "#FFF3E8D8"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("req 70x36", () => SetRequestedSize(new Size(70, 36)), width: 92, colorHex: "#FFE4ECF7"),
                        BuildButton("req 90x44", () => SetRequestedSize(new Size(90, 44)), width: 92, colorHex: "#FFE4ECF7"),
                        BuildButton("req 130x60", () => SetRequestedSize(new Size(130, 60)), width: 102, colorHex: "#FFE4ECF7"),
                    ]),
                new Text(
                    $"fit={FitLabel(_fit)}, alignment={AlignmentLabel(_alignment)}, max={MaxLabel(_maxWidth, _maxHeight)}, requested={_requestedSize.Width:0}x{_requestedSize.Height:0}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 230,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("OverflowBox preview", fontSize: 11, color: Colors.DimGray),
                            new Container(
                                height: 72,
                                color: Colors.White,
                                padding: new Thickness(6),
                                child: new Center(
                                    child: new OverflowBox(
                                        alignment: _alignment,
                                        maxWidth: _maxWidth,
                                        maxHeight: _maxHeight,
                                        fit: _fit,
                                        child: BuildProbeCard()))),
                            new Text("SizedOverflowBox preview", fontSize: 11, color: Colors.DimGray),
                            new Container(
                                height: 86,
                                color: Colors.White,
                                padding: new Thickness(6),
                                child: new Center(
                                    child: new SizedOverflowBox(
                                        size: _requestedSize,
                                        alignment: _alignment,
                                        child: BuildProbeCard()))),
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
                child: new Text("child 190x86", fontSize: 12, color: Colors.Black)));
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

    private void SetFit(OverflowBoxFit value)
    {
        SetState(() => _fit = value);
    }

    private void SetAlignment(Alignment value)
    {
        SetState(() => _alignment = value);
    }

    private void SetMax(double? maxWidth, double? maxHeight)
    {
        SetState(() =>
        {
            _maxWidth = maxWidth;
            _maxHeight = maxHeight;
        });
    }

    private void SetRequestedSize(Size value)
    {
        SetState(() => _requestedSize = value);
    }

    private static string FitLabel(OverflowBoxFit fit)
    {
        return fit == OverflowBoxFit.Max ? "max" : "deferToChild";
    }

    private static string AlignmentLabel(Alignment alignment)
    {
        if (alignment == Alignment.TopLeft)
        {
            return "topLeft";
        }

        if (alignment == Alignment.Center)
        {
            return "center";
        }

        if (alignment == Alignment.BottomRight)
        {
            return "bottomRight";
        }

        return $"({alignment.X:0.##},{alignment.Y:0.##})";
    }

    private static string MaxLabel(double? maxWidth, double? maxHeight)
    {
        if (!maxWidth.HasValue && !maxHeight.HasValue)
        {
            return "parent";
        }

        return $"{maxWidth:0}x{maxHeight:0}";
    }
}
