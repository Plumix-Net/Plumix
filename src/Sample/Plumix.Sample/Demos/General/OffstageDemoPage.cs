using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/offstage_demo_page.dart (exact sample parity)

namespace Plumix;

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
    private bool _visible = true;

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Visibility + SliverVisibility + Offstage", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Compare replacement, maintained layout space, and layout-without-paint behavior.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildButton("visible=true", () => SetVisible(true), width: 104, colorHex: "#FFDCE3ED"),
                            BuildButton(
                                "visible=false",
                                () => SetVisible(false),
                                width: 110,
                                colorHex: "#FFDCE3ED"),
                        ]),
                    new Text(
                        $"state: visible={(_visible ? "true" : "false")}",
                        fontSize: 12,
                        color: Colors.DarkSlateGray),
                    new Text(
                        "maintainState=true keeps the indicator mounted; TickerMode pauses its frame callbacks "
                        + "while hidden.",
                        fontSize: 11,
                        color: Colors.DimGray),
                    new Visibility(
                        visible: _visible,
                        maintainState: true,
                        child: new SizedBox(
                            height: 18,
                            child: new LinearProgressIndicator())),
                    new Container(
                        height: 82,
                        color: Color.Parse("#FFF6F8FB"),
                        padding: new Thickness(8),
                        child: new Row(
                            mainAxisAlignment: MainAxisAlignment.Center,
                            spacing: 8,
                            children:
                            [
                                BuildMarker("L", "#FF1D3557"),
                                Visibility.Maintain(
                                    visible: _visible,
                                    child: new Container(
                                        width: 88,
                                        height: 42,
                                        color: Color.Parse("#FFA8DADC"),
                                        child: new Center(
                                            child: new Text("keeps size", fontSize: 11, color: Colors.Black)))),
                                BuildMarker("R", "#FF457B9D"),
                            ])),
                    new Visibility(
                        visible: _visible,
                        replacement: new Container(
                            height: 42,
                            color: Color.Parse("#FFFFE8CC"),
                            child: new Center(
                                child: new Text("Visibility replacement", fontSize: 11, color: Colors.Black))),
                        child: new Container(
                            height: 42,
                            color: Color.Parse("#FFD8F3DC"),
                            child: new Center(
                                child: new Text("Visibility child", fontSize: 11, color: Colors.Black)))),
                    new SizedBox(
                        height: 150,
                        child: new CustomScrollView(
                            slivers:
                            [
                                new SliverToBoxAdapter(
                                    new Container(
                                        height: 42,
                                        color: Color.Parse("#FFE9ECEF"),
                                        child: new Center(
                                            child: new Text("sliver before", fontSize: 11, color: Colors.Black)))),
                                new SliverVisibility(
                                    visible: _visible,
                                    replacementSliver: new SliverToBoxAdapter(
                                        new Container(
                                            height: 42,
                                            color: Color.Parse("#FFFFE8CC"),
                                            child: new Center(
                                                child: new Text(
                                                    "replacement sliver",
                                                    fontSize: 11,
                                                    color: Colors.Black)))),
                                    sliver: new SliverToBoxAdapter(
                                        new Container(
                                            height: 42,
                                            color: Color.Parse("#FFBDE0FE"),
                                            child: new Center(
                                                child: new Text(
                                                    "SliverVisibility child",
                                                    fontSize: 11,
                                                    color: Colors.Black))))),
                                new SliverToBoxAdapter(
                                    new Container(
                                        height: 42,
                                        color: Color.Parse("#FFE9ECEF"),
                                        child: new Center(
                                            child: new Text("sliver after", fontSize: 11, color: Colors.Black)))),
                            ])),
                new Text(
                    "When offstage=true, child is laid out but not painted/hit-tested and takes no room in parent "
                    + "layout.",
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
                            new Text(
                                "Row layout (middle child disappears from layout when offstage=true)",
                                fontSize: 11,
                                color: Colors.DimGray),
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
                                                    Border: Plumix.Rendering.Border.FromBorderSide(
                                                        new BorderSide(Color.Parse("#FF1D3557"), 2)),
                                                    BorderRadius: BorderRadius.Circular(10)),
                                                child: new Center(
                                                    child: new Text(
                                                        "Offstage child",
                                                        fontSize: 11,
                                                        color: Colors.Black)))),
                                        BuildMarker("R", "#FF457B9D"),
                                    ])),
                            new Text(
                                "Tip: switch state and watch L/R gap change.",
                                fontSize: 11,
                                color: Colors.DimGray),
                        ])),
                ]));
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

    private void SetVisible(bool value)
    {
        SetState(() => _visible = value);
    }
}
