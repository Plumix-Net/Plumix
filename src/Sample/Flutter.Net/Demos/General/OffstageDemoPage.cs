using System;
using Avalonia;
using Avalonia.Media;
using Flutter.Rendering;
using Flutter.Widgets;

// Dart parity source (reference): dart_sample/lib/offstage_demo_page.dart (exact sample parity)

namespace Flutter.Net;

public sealed class OffstageDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new OffstageDemoPageState();
    }
}

internal sealed class OffstageDemoPageState : State
{
    private bool _offstage = true;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Offstage", fontSize: 20, color: Colors.Black),
                new Text(
                    "When offstage=true, child is laid out but not painted/hit-tested and takes no room in parent layout.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("offstage=true", () => SetOffstage(true), width: 112, colorHex: "#FFDCE3ED"),
                        BuildButton("offstage=false", () => SetOffstage(false), width: 118, colorHex: "#FFDCE3ED"),
                    ]),
                new Text(
                    $"state: offstage={(_offstage ? "true" : "false")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 260,
                    height: 190,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(10),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Row layout (middle child disappears from layout when offstage=true)", fontSize: 11, color: Colors.DimGray),
                            new Container(
                                height: 72,
                                color: Colors.White,
                                padding: new Thickness(8, 10, 8, 10),
                                child: new Row(
                                    mainAxisAlignment: MainAxisAlignment.Center,
                                    spacing: 8,
                                    children:
                                    [
                                        BuildMarker("L", "#FF1D3557"),
                                        new Offstage(
                                            offstage: _offstage,
                                            child: new Container(
                                                width: 120,
                                                height: 44,
                                                decoration: new BoxDecoration(
                                                    Color: Color.Parse("#FFCCE3FF"),
                                                    Border: new BorderSide(Color.Parse("#FF1D3557"), 2),
                                                    BorderRadius: BorderRadius.Circular(10)),
                                                child: new Center(
                                                    child: new Text("Offstage child", fontSize: 11, color: Colors.Black)))),
                                        BuildMarker("R", "#FF457B9D"),
                                    ])),
                            new Text("Tip: switch state and watch L/R gap change.", fontSize: 11, color: Colors.DimGray),
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

    private static Widget BuildMarker(string label, string colorHex)
    {
        return new Container(
            width: 34,
            height: 34,
            color: Color.Parse(colorHex),
            child: new Center(
                child: new Text(label, fontSize: 12, color: Colors.White)));
    }

    private void SetOffstage(bool value)
    {
        SetState(() => _offstage = value);
    }
}
