using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/fractionally_sized_box_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class FractionallySizedBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new FractionallySizedBoxDemoPageState();
    }
}

internal sealed class FractionallySizedBoxDemoPageState : State
{
    private Alignment _alignment = Alignment.Center;
    private double? _widthFactor = 0.7;
    private double? _heightFactor = 0.55;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("FractionallySizedBox", fontSize: 20, color: Colors.Black),
                new Text(
                    "Configure width/height factors and alignment; null factor means pass-through on that axis.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("W pass", () => SetWidthFactor(null), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("W 0.5", () => SetWidthFactor(0.5), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("W 0.8", () => SetWidthFactor(0.8), width: 88, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("H pass", () => SetHeightFactor(null), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("H 0.4", () => SetHeightFactor(0.4), width: 88, colorHex: "#FFE9F5EC"),
                        BuildButton("H 0.7", () => SetHeightFactor(0.7), width: 88, colorHex: "#FFE9F5EC"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("TopLeft", () => SetAlignment(Alignment.TopLeft), width: 96, colorHex: "#FFF3E8D8"),
                        BuildButton("Center", () => SetAlignment(Alignment.Center), width: 96, colorHex: "#FFF3E8D8"),
                        BuildButton("BottomRight", () => SetAlignment(Alignment.BottomRight), width: 112, colorHex: "#FFF3E8D8"),
                    ]),
                new Text(
                    $"widthFactor={FormatFactor(_widthFactor)}, heightFactor={FormatFactor(_heightFactor)}, alignment={AlignmentLabel(_alignment)}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 190,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new FractionallySizedBox(
                        alignment: _alignment,
                        widthFactor: _widthFactor,
                        heightFactor: _heightFactor,
                        child: new Container(
                            decoration: new BoxDecoration(
                                Color: Color.Parse("#FFCCE3FF"),
                                Border: new BorderSide(Color.Parse("#FF1D3557"), 2),
                                BorderRadius: BorderRadius.Circular(10)),
                            child: new Center(
                                child: new Text("fraction child", fontSize: 14, color: Colors.Black))))),
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

    private void SetWidthFactor(double? value)
    {
        SetState(() => _widthFactor = value);
    }

    private void SetHeightFactor(double? value)
    {
        SetState(() => _heightFactor = value);
    }

    private void SetAlignment(Alignment alignment)
    {
        SetState(() => _alignment = alignment);
    }

    private static string FormatFactor(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.##") : "pass";
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
}
