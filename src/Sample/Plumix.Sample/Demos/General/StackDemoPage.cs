using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/stack_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class StackDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new StackDemoPageState();
    }
}

internal sealed class StackDemoPageState : State
{
    private double _left = 8;
    private double _top = 8;
    private bool _pinBottomRight;
    private bool _rtl;

    public override Widget Build(BuildContext context)
    {
        Widget badge = new PositionedDirectional(
            start: _pinBottomRight ? null : _left,
            top: _pinBottomRight ? null : _top,
            end: _pinBottomRight ? 8 : null,
            bottom: _pinBottomRight ? 8 : null,
            child: new Container(
                width: 56,
                height: 30,
                color: Color.Parse("#FFD1495B"),
                child: new Center(
                    child: new Text("badge", fontSize: 11, color: Colors.White))));

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Stack + PositionedDirectional", fontSize: 20, color: Colors.Black),
                new Text(
                    "Move with logical start/end insets, toggle pinned mode, and flip LTR/RTL direction.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Left", () => Move(-8, 0), width: 72, colorHex: "#FFDCE3ED"),
                        BuildButton("Right", () => Move(8, 0), width: 72, colorHex: "#FFDCE3ED"),
                        BuildButton("Up", () => Move(0, -8), width: 72, colorHex: "#FFDCE3ED"),
                        BuildButton("Down", () => Move(0, 8), width: 72, colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _pinBottomRight ? "Pin: bottom-end" : "Pin: custom",
                            TogglePin,
                            width: 140,
                            colorHex: "#FFE9F5EC"),
                        BuildButton(_rtl ? "RTL" : "LTR", ToggleDirection, width: 72, colorHex: "#FFE9F5EC"),
                        BuildButton("Reset", Reset, width: 88, colorHex: "#FFF3E8D8"),
                    ]),
                new Text(
                    $"start={_left:0}, top={_top:0}, direction={(_rtl ? "RTL" : "LTR")}, "
                    + $"mode={(_pinBottomRight ? "bottomEnd" : "custom")}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    padding: new Thickness(8),
                    child: new Container(
                        color: Colors.White,
                        child: new Directionality(
                            _rtl ? TextDirection.Rtl : TextDirection.Ltr,
                            new Stack(
                                alignment: Alignment.Center,
                                children:
                                [
                                    new Container(
                                        width: 140,
                                        height: 80,
                                        color: Color.Parse("#FFCCE3FF"),
                                        child: new Center(
                                            child: new Text(
                                                "base",
                                                fontSize: 14,
                                                color: Color.Parse("#FF1D3557")))),
                                    badge,
                                ])))),
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

    private void Move(double dx, double dy)
    {
        if (_pinBottomRight)
        {
            return;
        }

        SetState(() =>
        {
            _left = Math.Clamp(_left + dx, 0, 150);
            _top = Math.Clamp(_top + dy, 0, 90);
        });
    }

    private void TogglePin()
    {
        SetState(() => _pinBottomRight = !_pinBottomRight);
    }

    private void ToggleDirection()
    {
        SetState(() => _rtl = !_rtl);
    }

    private void Reset()
    {
        SetState(() =>
        {
            _left = 8;
            _top = 8;
            _pinBottomRight = false;
            _rtl = false;
        });
    }
}
