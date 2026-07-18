using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using MaterialScrollbar = Plumix.Material.Scrollbar;

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
    private bool _scaled;
    private bool _rotated;
    private bool _positioned;
    private bool _rightToLeft;
    private bool _emphasizedText;
    private bool _raisedSurface;
    private int _switcherValue;
    private bool _showSecondCrossFade;
    private bool _expandedFraction;
    private bool _visibleSliver = true;
    private int _completedAnimations;
    private ScrollController _scrollController = null!;

    public override void InitState()
    {
        _scrollController = new ScrollController();
    }

    public override void Dispose()
    {
        _scrollController.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new MaterialScrollbar(
            controller: _scrollController,
            thumbVisibility: true,
            child: new SingleChildScrollView(
                controller: _scrollController,
                padding: new Thickness(0, 0, 12, 12),
                child: BuildContent()));
    }

    private Widget BuildContent()
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
                new Text("AnimatedScale + AnimatedRotation", fontSize: 20, color: Colors.Black),
                new Text(
                    "Scale and rotate around a bottom-right pivot; transform filtering follows the animated child.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _scaled ? "Scale: 1.6" : "Scale: 1.0",
                            ToggleScale,
                            width: 120,
                            colorHex: "#FFE8E0F4"),
                        BuildButton(
                            _rotated ? "Turns: 0.125" : "Turns: 0",
                            ToggleRotation,
                            width: 128,
                            colorHex: "#FFF7E6CF"),
                    ]),
                new Container(
                    width: 220,
                    height: 130,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new AnimatedRotation(
                            turns: _rotated ? 0.125 : 0,
                            duration: TimeSpan.FromMilliseconds(350),
                            alignment: Alignment.BottomRight,
                            filterQuality: FilterQuality.High,
                            curve: Curves.EaseInOut,
                            onEnd: HandleAnimationEnd,
                            child: new AnimatedScale(
                                scale: _scaled ? 1.6 : 1,
                                duration: TimeSpan.FromMilliseconds(350),
                                alignment: Alignment.BottomRight,
                                filterQuality: FilterQuality.High,
                                curve: Curves.EaseInOut,
                                onEnd: HandleAnimationEnd,
                                child: new Container(
                                    width: 72,
                                    height: 44,
                                    color: Color.Parse("#FFB85C38"),
                                    child: new Center(
                                        child: new Text("turn", fontSize: 14, color: Colors.White))))))),
                new Text("AnimatedPositioned + AnimatedPositionedDirectional", fontSize: 20, color: Colors.Black),
                new Text(
                    "Animate physical and logical Stack insets; switching direction resolves start/end immediately.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _positioned ? "Position: end" : "Position: start",
                            TogglePosition,
                            width: 132,
                            colorHex: "#FFDDEBF7"),
                        BuildButton(
                            _rightToLeft ? "Direction: RTL" : "Direction: LTR",
                            ToggleDirection,
                            width: 132,
                            colorHex: "#FFF4E6C8"),
                    ]),
                new Container(
                    width: 240,
                    height: 140,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Stack(
                        children:
                        [
                            new AnimatedPositioned(
                                left: _positioned ? 154 : 10,
                                top: _positioned ? 18 : 10,
                                width: _positioned ? 70 : 48,
                                height: 40,
                                duration: TimeSpan.FromMilliseconds(350),
                                curve: Curves.EaseInOut,
                                onEnd: HandleAnimationEnd,
                                child: new Container(
                                    color: Color.Parse("#FF2A6F97"),
                                    child: new Center(
                                        child: new Text("left", fontSize: 12, color: Colors.White)))),
                            new Directionality(
                                textDirection: _rightToLeft
                                    ? Plumix.UI.TextDirection.Rtl
                                    : Plumix.UI.TextDirection.Ltr,
                                child: new AnimatedPositionedDirectional(
                                    start: _positioned ? 136 : 10,
                                    top: 86,
                                    width: _positioned ? 88 : 58,
                                    height: 40,
                                    duration: TimeSpan.FromMilliseconds(350),
                                    curve: Curves.EaseInOut,
                                    onEnd: HandleAnimationEnd,
                                    child: new Container(
                                        color: Color.Parse("#FF6A4C93"),
                                        child: new Center(
                                            child: new Text("start", fontSize: 12, color: Colors.White))))),
                        ])),
                new Text("AnimatedDefaultTextStyle + AnimatedPhysicalModel", fontSize: 20, color: Colors.Black),
                new Text(
                    "Animate inherited typography and physical surface radius, elevation, fill, and shadow.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _emphasizedText ? "Text: emphasized" : "Text: normal",
                            ToggleTextStyle,
                            width: 144,
                            colorHex: "#FFE7E0F2"),
                        BuildButton(
                            _raisedSurface ? "Surface: raised" : "Surface: flat",
                            TogglePhysicalModel,
                            width: 136,
                            colorHex: "#FFE2EFE7"),
                    ]),
                new Row(
                    spacing: 18,
                    children:
                    [
                        new SizedBox(
                            width: 150,
                            child: new AnimatedDefaultTextStyle(
                                child: new Text("inherited style"),
                                style: new TextStyle(
                                    FontSize: _emphasizedText ? 22 : 14,
                                    Color: _emphasizedText
                                        ? Color.Parse("#FF6A1B9A")
                                        : Color.Parse("#FF264653"),
                                    FontWeight: _emphasizedText ? FontWeight.Bold : FontWeight.Normal,
                                    LetterSpacing: _emphasizedText ? 1.2 : 0.1),
                                duration: TimeSpan.FromMilliseconds(350),
                                textAlign: Plumix.UI.TextAlign.Center,
                                maxLines: 1,
                                curve: Curves.EaseInOut,
                                onEnd: HandleAnimationEnd)),
                        new AnimatedPhysicalModel(
                            child: new SizedBox(
                                width: 110,
                                height: 64,
                                child: new Center(
                                    child: new Text("surface", fontSize: 13, color: Colors.White))),
                            color: _raisedSurface
                                ? Color.Parse("#FF2A9D8F")
                                : Color.Parse("#FF457B9D"),
                            shadowColor: Color.Parse("#FF1D3557"),
                            duration: TimeSpan.FromMilliseconds(350),
                            clipBehavior: Plumix.UI.Clip.AntiAlias,
                            borderRadius: BorderRadius.Circular(_raisedSurface ? 24 : 4),
                            elevation: _raisedSurface ? 12 : 0,
                            curve: Curves.EaseInOut,
                            onEnd: HandleAnimationEnd),
                    ]),
                new Text("AnimatedSwitcher + AnimatedCrossFade", fontSize: 20, color: Colors.Black),
                new Text(
                    "Rapid keyed replacements keep outgoing switcher children while cross-fade also animates height.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            $"Switcher: {_switcherValue}",
                            AdvanceSwitcher,
                            width: 128,
                            colorHex: "#FFE4EAF4"),
                        BuildButton(
                            _showSecondCrossFade ? "Cross-fade: second" : "Cross-fade: first",
                            ToggleCrossFade,
                            width: 152,
                            colorHex: "#FFF3E4D3"),
                    ]),
                new Container(
                    width: 240,
                    height: 90,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new AnimatedSwitcher(
                            duration: TimeSpan.FromMilliseconds(350),
                            reverseDuration: TimeSpan.FromMilliseconds(220),
                            switchInCurve: Curves.EaseInOut,
                            switchOutCurve: Curves.EaseInOut,
                            child: new Container(
                                key: new ValueKey<int>(_switcherValue),
                                width: 96,
                                height: 48,
                                color: _switcherValue % 2 == 0
                                    ? Color.Parse("#FF315A7D")
                                    : Color.Parse("#FF9C4F63"),
                                child: new Center(
                                    child: new Text(
                                        $"child {_switcherValue}",
                                        fontSize: 13,
                                        color: Colors.White)))))),
                new AnimatedCrossFade(
                    firstChild: new Container(
                        width: 240,
                        height: 54,
                        color: Color.Parse("#FFDCEBF2"),
                        child: new Center(
                            child: new Text("first / 54", fontSize: 13, color: Colors.Black))),
                    secondChild: new Container(
                        width: 240,
                        height: 92,
                        color: Color.Parse("#FFF2D9DF"),
                        child: new Center(
                            child: new Text("second / 92", fontSize: 13, color: Colors.Black))),
                    crossFadeState: _showSecondCrossFade
                        ? CrossFadeState.ShowSecond
                        : CrossFadeState.ShowFirst,
                    duration: TimeSpan.FromMilliseconds(350),
                    reverseDuration: TimeSpan.FromMilliseconds(260),
                    firstCurve: Curves.EaseInOut,
                    secondCurve: Curves.EaseInOut,
                    sizeCurve: Curves.EaseInOut,
                    onEnd: HandleAnimationEnd),
                new Text(
                    "AnimatedFractionallySizedBox + SliverAnimatedOpacity",
                    fontSize: 20,
                    color: Colors.Black),
                new Text(
                    "Animate fractional layout and a sliver paint layer while preserving their child geometry.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _expandedFraction ? "Fraction: 0.8" : "Fraction: 0.4",
                            ToggleFraction,
                            width: 128,
                            colorHex: "#FFDDEAF2"),
                        BuildButton(
                            _visibleSliver ? "Sliver: visible" : "Sliver: faded",
                            ToggleSliverOpacity,
                            width: 132,
                            colorHex: "#FFF0E1EA"),
                    ]),
                new Container(
                    width: 240,
                    height: 120,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new AnimatedFractionallySizedBox(
                        duration: TimeSpan.FromMilliseconds(350),
                        alignment: _expandedFraction ? Alignment.BottomRight : Alignment.TopLeft,
                        widthFactor: _expandedFraction ? 0.8 : 0.4,
                        heightFactor: _expandedFraction ? 0.75 : 0.4,
                        curve: Curves.EaseInOut,
                        onEnd: HandleAnimationEnd,
                        child: new Container(
                            color: Color.Parse("#FF3F7D6B"),
                            child: new Center(
                                child: new Text("fraction", fontSize: 13, color: Colors.White))))),
                new Container(
                    width: 240,
                    height: 100,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new CustomScrollView(
                        slivers:
                        [
                            new SliverAnimatedOpacity(
                                opacity: _visibleSliver ? 1.0 : 0.15,
                                duration: TimeSpan.FromMilliseconds(350),
                                curve: Curves.EaseInOut,
                                onEnd: HandleAnimationEnd,
                                sliver: new SliverToBoxAdapter(
                                    new Container(
                                        height: 84,
                                        color: Color.Parse("#FF8E5572"),
                                        child: new Center(
                                            child: new Text(
                                                "sliver opacity",
                                                fontSize: 13,
                                                color: Colors.White))))),
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

    private void ToggleScale()
    {
        SetState(() => _scaled = !_scaled);
    }

    private void ToggleRotation()
    {
        SetState(() => _rotated = !_rotated);
    }

    private void TogglePosition()
    {
        SetState(() => _positioned = !_positioned);
    }

    private void ToggleDirection()
    {
        SetState(() => _rightToLeft = !_rightToLeft);
    }

    private void ToggleTextStyle()
    {
        SetState(() => _emphasizedText = !_emphasizedText);
    }

    private void TogglePhysicalModel()
    {
        SetState(() => _raisedSurface = !_raisedSurface);
    }

    private void AdvanceSwitcher()
    {
        SetState(() => _switcherValue++);
    }

    private void ToggleCrossFade()
    {
        SetState(() => _showSecondCrossFade = !_showSecondCrossFade);
    }

    private void ToggleFraction()
    {
        SetState(() => _expandedFraction = !_expandedFraction);
    }

    private void ToggleSliverOpacity()
    {
        SetState(() => _visibleSliver = !_visibleSliver);
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
