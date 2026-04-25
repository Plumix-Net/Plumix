using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/fitted_box_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class FittedBoxDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new FittedBoxDemoPageState();
    }
}

internal sealed class FittedBoxDemoPageState : State
{
    private BoxFit _fit = BoxFit.Contain;
    private Alignment _alignment = Alignment.Center;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("FittedBox", fontSize: 20, color: Colors.Black),
                new Text(
                    "Change fit/alignment to inspect scaling behavior inside a fixed preview box.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Contain", () => SetFit(BoxFit.Contain), width: 92, colorHex: "#FFDCE3ED"),
                        BuildButton("Cover", () => SetFit(BoxFit.Cover), width: 88, colorHex: "#FFDCE3ED"),
                        BuildButton("Fill", () => SetFit(BoxFit.Fill), width: 84, colorHex: "#FFDCE3ED"),
                        BuildButton("None", () => SetFit(BoxFit.None), width: 84, colorHex: "#FFDCE3ED"),
                        BuildButton("Down", () => SetFit(BoxFit.ScaleDown), width: 84, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("TopLeft", () => SetAlignment(Alignment.TopLeft), width: 96, colorHex: "#FFE9F5EC"),
                        BuildButton("Center", () => SetAlignment(Alignment.Center), width: 96, colorHex: "#FFE9F5EC"),
                        BuildButton("BottomRight", () => SetAlignment(Alignment.BottomRight), width: 112, colorHex: "#FFE9F5EC"),
                    ]),
                new Text(
                    $"fit={FitLabel(_fit)}, alignment={AlignmentLabel(_alignment)}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 190,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Container(
                        color: Colors.White,
                        child: new FittedBox(
                            fit: _fit,
                            alignment: _alignment,
                            child: new Container(
                                width: 160,
                                height: 60,
                                decoration: new BoxDecoration(
                                    Color: Color.Parse("#FFCCE3FF"),
                                    Border: new BorderSide(Color.Parse("#FF1D3557"), 2),
                                    BorderRadius: BorderRadius.Circular(10)),
                                child: new Row(
                                    mainAxisAlignment: MainAxisAlignment.Center,
                                    spacing: 8,
                                    children:
                                    [
                                        new Text("WIDE", fontSize: 14, color: Colors.Black),
                                        new Container(width: 14, height: 14, color: Color.Parse("#FF1D3557")),
                                    ]))))),
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

    private void SetFit(BoxFit value)
    {
        SetState(() => _fit = value);
    }

    private void SetAlignment(Alignment value)
    {
        SetState(() => _alignment = value);
    }

    private static string FitLabel(BoxFit fit)
    {
        return fit switch
        {
            BoxFit.Fill => "fill",
            BoxFit.Contain => "contain",
            BoxFit.Cover => "cover",
            BoxFit.FitWidth => "fitWidth",
            BoxFit.FitHeight => "fitHeight",
            BoxFit.None => "none",
            BoxFit.ScaleDown => "scaleDown",
            _ => fit.ToString()
        };
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
