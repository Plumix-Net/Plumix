using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/align_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class AlignDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new AlignDemoPageState();
    }
}

internal sealed class AlignDemoPageState : State
{
    private Alignment _alignment = Alignment.Center;
    private bool _shrinkWrap;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Align + Center", fontSize: 20, color: Colors.Black),
                new Text(
                    "Move a fixed card inside preview using Align; shrink mode applies width/height factors.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("TopLeft", () => SetAlignment(Alignment.TopLeft), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Center", () => SetAlignment(Alignment.Center), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("BottomRight", () => SetAlignment(Alignment.BottomRight), width: 112, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(_shrinkWrap ? "Shrink: on" : "Shrink: off", ToggleShrinkWrap, width: 120, colorHex: "#FFE9F5EC"),
                    ]),
                new Text(
                    $"alignment={AlignmentLabel(_alignment)}, shrink={(_shrinkWrap ? "on" : "off")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new Container(
                        color: Colors.White,
                        child: new Align(
                            alignment: _alignment,
                            widthFactor: _shrinkWrap ? 1.5 : null,
                            heightFactor: _shrinkWrap ? 1.5 : null,
                            child: new Container(
                                width: 64,
                                height: 40,
                                color: Color.Parse("#FF1D3557"),
                                child: new Center(
                                    child: new Text("A", fontSize: 16, color: Colors.White)))))),
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

    private void SetAlignment(Alignment alignment)
    {
        SetState(() => _alignment = alignment);
    }

    private void ToggleShrinkWrap()
    {
        SetState(() => _shrinkWrap = !_shrinkWrap);
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
