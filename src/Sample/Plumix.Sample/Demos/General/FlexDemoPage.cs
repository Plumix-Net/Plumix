using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/flex_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class FlexDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new FlexDemoPageState();
    }
}

internal sealed class FlexDemoPageState : State
{
    private static readonly MainAxisAlignment[] Alignments =
    [
        MainAxisAlignment.Start,
        MainAxisAlignment.End,
        MainAxisAlignment.Center,
        MainAxisAlignment.SpaceBetween,
        MainAxisAlignment.SpaceAround,
        MainAxisAlignment.SpaceEvenly,
    ];

    private static readonly CrossAxisAlignment[] CrossAlignments =
    [
        CrossAxisAlignment.Start,
        CrossAxisAlignment.End,
        CrossAxisAlignment.Center,
        CrossAxisAlignment.Stretch,
    ];

    private int _alignmentIndex;
    private int _crossAlignmentIndex = 2;
    private double _spacing;
    private bool _rightToLeft;
    private bool _bottomToTop;
    private bool _overflow;
    private Clip _clipBehavior = Clip.None;

    public override Widget Build(BuildContext context)
    {
        MainAxisAlignment alignment = Alignments[_alignmentIndex];
        CrossAxisAlignment crossAlignment = CrossAlignments[_crossAlignmentIndex];

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Flex / Row / Column", fontSize: 20, color: Colors.Black),
                new Text(
                    "RenderFlex distributes free space by mainAxisAlignment, inserts `spacing` between "
                    + "children, and flips both axes from textDirection/verticalDirection.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Main axis", CycleAlignment, width: 104, colorHex: "#FFDCE3ED"),
                        BuildButton("Cross axis", CycleCrossAlignment, width: 104, colorHex: "#FFDCE3ED"),
                        BuildButton("Spacing", CycleSpacing, width: 96, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("RTL", ToggleTextDirection, width: 78, colorHex: "#FFE9F5EC"),
                        BuildButton("Up", ToggleVerticalDirection, width: 78, colorHex: "#FFE9F5EC"),
                        BuildButton("Overflow", ToggleOverflow, width: 96, colorHex: "#FFF6E7E7"),
                        BuildButton("Clip", CycleClip, width: 78, colorHex: "#FFF6E7E7"),
                    ]),
                new Text(
                    $"main={alignment}, cross={crossAlignment}, spacing={_spacing:0}, "
                    + $"textDirection={(_rightToLeft ? "Rtl" : "Ltr")}, "
                    + $"verticalDirection={(_bottomToTop ? "Up" : "Down")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Text(
                    $"overflow={(_overflow ? "on" : "off")}, clipBehavior={_clipBehavior}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    height: 120,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new Directionality(
                        _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr,
                        child: new Flex(
                            direction: Axis.Horizontal,
                            mainAxisAlignment: alignment,
                            crossAxisAlignment: crossAlignment,
                            verticalDirection: _bottomToTop ? VerticalDirection.Up : VerticalDirection.Down,
                            spacing: _spacing,
                            clipBehavior: _clipBehavior,
                            children:
                            [
                                Tile("1", "#FF1D3557", _overflow ? 150 : 56, 40),
                                Tile("2", "#FF2A9D8F", _overflow ? 150 : 56, 64),
                                Tile("3", "#FF457B9D", _overflow ? 150 : 56, 48),
                            ]))),
                new Container(
                    height: 150,
                    color: Color.Parse("#FFEFF3E7"),
                    padding: new Thickness(8),
                    child: new Directionality(
                        _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr,
                        child: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Baseline,
                            textBaseline: TextBaseline.Alphabetic,
                            spacing: 12,
                            children:
                            [
                                new Text("Baseline", fontSize: 12, color: Colors.Black),
                                new Text("aligned", fontSize: 22, color: Colors.Black),
                                new Text("row", fontSize: 32, color: Colors.Black),
                            ]))),
            ]);
    }

    private static Widget Tile(string label, string colorHex, double width, double height)
    {
        return new Container(
            width: width,
            height: height,
            color: Color.Parse(colorHex),
            child: new Center(child: new Text(label, fontSize: 12, color: Colors.White)));
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

    private void CycleAlignment()
    {
        SetState(() => _alignmentIndex = (_alignmentIndex + 1) % Alignments.Length);
    }

    private void CycleCrossAlignment()
    {
        SetState(() => _crossAlignmentIndex = (_crossAlignmentIndex + 1) % CrossAlignments.Length);
    }

    private void CycleSpacing()
    {
        SetState(() => _spacing = _spacing >= 24 ? 0 : _spacing + 8);
    }

    private void ToggleTextDirection()
    {
        SetState(() => _rightToLeft = !_rightToLeft);
    }

    private void ToggleVerticalDirection()
    {
        SetState(() => _bottomToTop = !_bottomToTop);
    }

    private void ToggleOverflow()
    {
        SetState(() => _overflow = !_overflow);
    }

    private void CycleClip()
    {
        SetState(() => _clipBehavior = _clipBehavior == Clip.None ? Clip.HardEdge : Clip.None);
    }
}
