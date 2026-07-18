using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/align_demo_page.dart (exact sample parity)

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
    private bool _expandedPadding;
    private bool _faded;
    private bool _shifted;
    private int _completedAnimations;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("AnimatedAlign + AnimatedPadding", fontSize: 20, color: Colors.Black),
                new Text(
                    "Move the card and change its inset; both values transition implicitly with easeInOut.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("TopLeft", () => SetAlignment(Alignment.TopLeft), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton("Center", () => SetAlignment(Alignment.Center), width: 96, colorHex: "#FFDCE3ED"),
                        BuildButton(
                            "BottomRight",
                            () => SetAlignment(Alignment.BottomRight),
                            width: 112,
                            colorHex: "#FFDCE3ED"),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _shrinkWrap ? "Shrink: on" : "Shrink: off",
                            ToggleShrinkWrap,
                            width: 120,
                            colorHex: "#FFE9F5EC"),
                        BuildButton(
                            _expandedPadding ? "Padding: 24" : "Padding: 8",
                            TogglePadding,
                            width: 120,
                            colorHex: "#FFFFE8CC"),
                    ]),
                new Text(
                    $"alignment={AlignmentLabel(_alignment)}, shrink={(_shrinkWrap ? "on" : "off")}, "
                    + $"padding={(_expandedPadding ? 24 : 8)}, completed={_completedAnimations}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Container(
                    width: 220,
                    height: 140,
                    color: Color.Parse("#FFE7EDF6"),
                    child: new AnimatedPadding(
                        padding: new Thickness(_expandedPadding ? 24 : 8),
                        duration: TimeSpan.FromMilliseconds(350),
                        curve: Curves.EaseInOut,
                        onEnd: HandleAnimationEnd,
                        child: new Container(
                            color: Colors.White,
                            child: new AnimatedAlign(
                            alignment: _alignment,
                            duration: TimeSpan.FromMilliseconds(350),
                            curve: Curves.EaseInOut,
                            onEnd: HandleAnimationEnd,
                            widthFactor: _shrinkWrap ? 1.5 : null,
                            heightFactor: _shrinkWrap ? 1.5 : null,
                            child: new Container(
                                width: 64,
                                height: 40,
                                color: Color.Parse("#FF1D3557"),
                                child: new Center(
                                    child: new Text("A", fontSize: 16, color: Colors.White))))))),
                new Text("AnimatedOpacity + AnimatedSlide", fontSize: 20, color: Colors.Black),
                new Text(
                    "Fade and move the same child by a size-relative offset; hit testing follows the slide.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _faded ? "Opacity: 0.2" : "Opacity: 1.0",
                            ToggleOpacity,
                            width: 120,
                            colorHex: "#FFF4E1F0"),
                        BuildButton(
                            _shifted ? "Offset: (0.75,-0.5)" : "Offset: zero",
                            ToggleOffset,
                            width: 160,
                            colorHex: "#FFE1F1F4"),
                    ]),
                new Container(
                    width: 220,
                    height: 110,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new AnimatedSlide(
                            offset: _shifted ? new Vector(0.75, -0.5) : default,
                            duration: TimeSpan.FromMilliseconds(350),
                            curve: Curves.EaseInOut,
                            onEnd: HandleAnimationEnd,
                            child: new AnimatedOpacity(
                                opacity: _faded ? 0.2 : 1.0,
                                duration: TimeSpan.FromMilliseconds(350),
                                curve: Curves.EaseInOut,
                                onEnd: HandleAnimationEnd,
                                child: new Container(
                                    width: 72,
                                    height: 44,
                                    color: Color.Parse("#FF7B2CBF"),
                                    child: new Center(
                                        child: new Text("move", fontSize: 14, color: Colors.White))))))),
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

    private void TogglePadding()
    {
        SetState(() => _expandedPadding = !_expandedPadding);
    }

    private void ToggleOpacity()
    {
        SetState(() => _faded = !_faded);
    }

    private void ToggleOffset()
    {
        SetState(() => _shifted = !_shifted);
    }

    private void HandleAnimationEnd()
    {
        SetState(() => _completedAnimations++);
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
