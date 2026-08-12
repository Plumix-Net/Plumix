using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using MaterialScrollbar = Plumix.Material.Scrollbar;
using RelativeRect = Plumix.Rendering.RelativeRect;

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
    private double _builderExtent = 72;
    private bool _explicitTransitionsForward;
    private bool _repeatingPaused;
    private bool _repeatingReverse;
    private int _completedAnimations;
    private ValueNotifier<int> _builderCounter = null!;
    private ScrollController _scrollController = null!;
    private AnimationController _explicitTransitionsController = null!;
    private Animation<Vector> _explicitSlideAnimation = null!;
    private Animation<RelativeRect> _explicitPositionAnimation = null!;
    private Animation<Rect?> _explicitRelativePositionAnimation = null!;
    private Animation<AlignmentGeometry> _explicitAlignmentAnimation = null!;
    private Animation<TextStyle> _explicitTextStyleAnimation = null!;
    private Animation<Decoration> _explicitDecorationAnimation = null!;

    public override void InitState()
    {
        _builderCounter = new ValueNotifier<int>(0);
        _scrollController = new ScrollController();
        _explicitTransitionsController = new AnimationController(TimeSpan.FromMilliseconds(800), this);
        _explicitTransitionsController.SetValue(0.25);
        _explicitSlideAnimation = new DerivedAnimation<Vector>(
            _explicitTransitionsController,
            value => new Vector(value * 0.75, value * -0.5));
        var positionTween = new RelativeRectTween(
            begin: new RelativeRect(10, 12, 160, 78),
            end: new RelativeRect(160, 72, 10, 18));
        _explicitPositionAnimation = new DerivedAnimation<RelativeRect>(
            _explicitTransitionsController,
            positionTween.Evaluate);
        var rectTween = new RectTween();
        _explicitRelativePositionAnimation = new DerivedAnimation<Rect?>(
            _explicitTransitionsController,
            value => rectTween.Evaluate(
                value,
                new Rect(12, 74, 70, 40),
                new Rect(158, 14, 70, 40)));
        _explicitAlignmentAnimation = new DerivedAnimation<AlignmentGeometry>(
            _explicitTransitionsController,
            value => new Alignment(-1.0 + (2.0 * value), -1.0 + (2.0 * value)));
        var textStyleBegin = new TextStyle(
            FontSize: 12,
            Color: Color.Parse("#FF315A7D"),
            FontWeight: FontWeight.Normal,
            LetterSpacing: 0);
        var textStyleEnd = new TextStyle(
            FontSize: 20,
            Color: Color.Parse("#FF9C4F63"),
            FontWeight: FontWeight.Bold,
            LetterSpacing: 1.5);
        _explicitTextStyleAnimation = new DerivedAnimation<TextStyle>(
            _explicitTransitionsController,
            value => TextStyle.Lerp(textStyleBegin, textStyleEnd, value));
        _explicitDecorationAnimation = new DecorationTween(
            begin: new BoxDecoration(
                Color: Color.Parse("#FF315A7D"),
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FF9EC5E5"), 2)),
                BorderRadius: BorderRadius.Circular(4)),
            end: new BoxDecoration(
                Color: Color.Parse("#FF9C4F63"),
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(Color.Parse("#FFF4B6C2"), 6)),
                BorderRadius: BorderRadius.Circular(24))).Animate(_explicitTransitionsController);
    }

    public override void Dispose()
    {
        _builderCounter.Dispose();
        _explicitTransitionsController.Dispose();
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
                new Text("ScaleTransition + RotationTransition", fontSize: 20, color: Colors.Black),
                new Text(
                    "Drive explicit scale and turn animations from one controller; filtering is active only in motion.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildButton(
                    _explicitTransitionsForward ? "Reverse transitions" : "Forward transitions",
                    ToggleExplicitTransitions,
                    width: 152,
                    colorHex: "#FFDCEAF4"),
                new Container(
                    width: 220,
                    height: 130,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new RotationTransition(
                            turns: _explicitTransitionsController,
                            alignment: Alignment.BottomRight,
                            filterQuality: FilterQuality.High,
                            child: new ScaleTransition(
                                scale: _explicitTransitionsController,
                                alignment: Alignment.BottomRight,
                                filterQuality: FilterQuality.High,
                                child: new Container(
                                    width: 72,
                                    height: 44,
                                    color: Color.Parse("#FF356A82"),
                                    child: new Center(
                                        child: new Text("explicit", fontSize: 13, color: Colors.White))))))),
                new Text("DecoratedBoxTransition", fontSize: 20, color: Colors.Black),
                new Text(
                    "Interpolate fill, border width/color, and radius through DecorationTween.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    width: 220,
                    height: 100,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new DecoratedBoxTransition(
                            decoration: _explicitDecorationAnimation,
                            position: DecorationPosition.Foreground,
                            child: new SizedBox(
                                width: 112,
                                height: 56,
                                child: new Center(
                                    child: new Text(
                                        "decoration",
                                        fontSize: 13,
                                        color: Colors.White)))))),
                new Text("SlideTransition + SizeTransition", fontSize: 20, color: Colors.Black),
                new Text(
                    "Move in reading direction and reveal clipped size from the bottom-right alignment.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _rightToLeft ? "Direction: RTL" : "Direction: LTR",
                            ToggleDirection,
                            width: 132,
                            colorHex: "#FFF4E6C8"),
                        new Text(
                            "factor follows the explicit controller",
                            fontSize: 12,
                            color: Colors.DarkSlateGray),
                    ]),
                new Row(
                    spacing: 12,
                    children:
                    [
                        new Container(
                            width: 110,
                            height: 90,
                            color: Color.Parse("#FFF3F5F8"),
                            child: new Center(
                                child: new SlideTransition(
                                    position: _explicitSlideAnimation,
                                    textDirection: _rightToLeft
                                        ? Plumix.UI.TextDirection.Rtl
                                        : Plumix.UI.TextDirection.Ltr,
                                    child: new Container(
                                        width: 58,
                                        height: 40,
                                        color: Color.Parse("#FF2A6F97"),
                                        child: new Center(
                                            child: new Text("slide", fontSize: 12, color: Colors.White)))))),
                        new Container(
                            width: 110,
                            height: 90,
                            color: Color.Parse("#FFF3F5F8"),
                            child: new Center(
                                child: new SizeTransition(
                                    sizeFactor: _explicitTransitionsController,
                                    axis: Axis.Vertical,
                                    alignment: Alignment.BottomRight,
                                    fixedCrossAxisSizeFactor: 0.75,
                                    child: new Container(
                                        width: 72,
                                        height: 60,
                                        color: Color.Parse("#FF9C4F63"),
                                        child: new Center(
                                            child: new Text("size", fontSize: 12, color: Colors.White)))))),
                    ]),
                new Text(
                    "PositionedTransition + RelativePositionedTransition",
                    fontSize: 20,
                    color: Colors.Black),
                new Text(
                    "The same explicit controller drives Stack insets and a Rect relative to a declared box size.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    width: 240,
                    height: 130,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Stack(
                        children:
                        [
                            new PositionedTransition(
                                rect: _explicitPositionAnimation,
                                child: new Container(
                                    color: Color.Parse("#FF315A7D"),
                                    child: new Center(
                                        child: new Text("insets", fontSize: 12, color: Colors.White)))),
                            new RelativePositionedTransition(
                                rect: _explicitRelativePositionAnimation,
                                size: new Size(240, 130),
                                child: new Container(
                                    color: Color.Parse("#FF9C4F63"),
                                    child: new Center(
                                        child: new Text("rect", fontSize: 12, color: Colors.White)))),
                        ])),
                new Text("AlignTransition + DefaultTextStyleTransition", fontSize: 20, color: Colors.Black),
                new Text(
                    "Animate alignment geometry and inherited text style from the same explicit controller.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 12,
                    children:
                    [
                        new Container(
                            width: 110,
                            height: 90,
                            color: Color.Parse("#FFF3F5F8"),
                            child: new AlignTransition(
                                alignment: _explicitAlignmentAnimation,
                                widthFactor: 1.6,
                                heightFactor: 2.0,
                                child: new Container(
                                    width: 48,
                                    height: 28,
                                    color: Color.Parse("#FF356A82")))),
                        new Container(
                            width: 180,
                            height: 90,
                            color: Color.Parse("#FFF3F5F8"),
                            child: new Center(
                                child: new DefaultTextStyleTransition(
                                    style: _explicitTextStyleAnimation,
                                    textAlign: Plumix.UI.TextAlign.Center,
                                    maxLines: 1,
                                    child: new Text("animated text")))),
                    ]),
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
                    "Rapid keyed replacements keep outgoing switcher children while cross-fade animates height "
                    + "from the logical bottom-end edge.",
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
                new Directionality(
                    textDirection: _rightToLeft
                        ? Plumix.UI.TextDirection.Rtl
                        : Plumix.UI.TextDirection.Ltr,
                    child: new AnimatedCrossFade(
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
                        alignment: AlignmentDirectional.BottomEnd,
                        onEnd: HandleAnimationEnd)),
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
                new Text("ValueListenableBuilder + TweenAnimationBuilder", fontSize: 20, color: Colors.Black),
                new Text(
                    "The notifier rebuilds only its builder; the tween owns and retargets its animation.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            "Increment notifier",
                            () => _builderCounter.Value++,
                            width: 140,
                            colorHex: "#FFDDEAF2"),
                        BuildButton(
                            _builderExtent > 72 ? "Tween: 72" : "Tween: 180",
                            ToggleBuilderExtent,
                            width: 112,
                            colorHex: "#FFF0E1EA"),
                    ]),
                new ValueListenableBuilder<int>(
                    valueListenable: _builderCounter,
                    child: new Container(
                        width: 84,
                        height: 28,
                        color: Color.Parse("#FFE4E8EE"),
                        child: new Center(
                            child: new Text("stable child", fontSize: 11, color: Colors.DarkSlateGray))),
                    builder: (_, value, child) => new Row(
                        spacing: 8,
                        children:
                        [
                            new Text($"notifier value={value}", fontSize: 13, color: Colors.Black),
                            child ?? new SizedBox(),
                        ])),
                new Container(
                    width: 240,
                    height: 56,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Align(
                        alignment: Alignment.CenterLeft,
                        child: new TweenAnimationBuilder<double>(
                            tween: new DoubleTween(end: _builderExtent),
                            duration: TimeSpan.FromMilliseconds(450),
                            curve: Curves.EaseInOut,
                            onEnd: HandleAnimationEnd,
                            child: new Center(
                                child: new Text("tween child", fontSize: 12, color: Colors.White)),
                            builder: (_, value, child) => new Container(
                                width: value,
                                height: 36,
                                color: Color.Parse("#FF356A82"),
                                child: child)))),
                new Text("ListenableBuilder + AnimatedBuilder", fontSize: 20, color: Colors.Black),
                new Text(
                    "The generic listenable and animation aliases share listener lifecycle and stable-child reuse.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            "Notify listenable",
                            () => _builderCounter.Value++,
                            width: 132,
                            colorHex: "#FFE5EDF5"),
                        BuildButton(
                            _explicitTransitionsForward ? "Reverse builder" : "Forward builder",
                            ToggleExplicitTransitions,
                            width: 132,
                            colorHex: "#FFF1E5D8"),
                    ]),
                new ListenableBuilder(
                    listenable: _builderCounter,
                    child: new Container(
                        width: 84,
                        height: 28,
                        color: Color.Parse("#FFE4E8EE"),
                        child: new Center(
                            child: new Text("stable child", fontSize: 11, color: Colors.DarkSlateGray))),
                    builder: (_, child) => new Row(
                        spacing: 8,
                        children:
                        [
                            new Text(
                                $"listenable notifications={_builderCounter.Value}",
                                fontSize: 13,
                                color: Colors.Black),
                            child ?? new SizedBox(),
                        ])),
                new Container(
                    width: 240,
                    height: 56,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Align(
                        alignment: Alignment.CenterLeft,
                        child: new AnimatedBuilder(
                            animation: _explicitTransitionsController,
                            child: new Center(
                                child: new Text("animated child", fontSize: 12, color: Colors.White)),
                            builder: (_, child) => new Container(
                                width: 72 + (_explicitTransitionsController.Value * 108),
                                height: 36,
                                color: Color.Parse("#FF6B5B95"),
                                child: child)))),
                new Text("DualTransitionBuilder + RepeatingAnimationBuilder", fontSize: 20, color: Colors.Black),
                new Text(
                    "Enter/exit builders stay nested while a reusable animatable loops, reverses, and pauses.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _explicitTransitionsForward ? "Dual: reverse" : "Dual: forward",
                            ToggleExplicitTransitions,
                            width: 124,
                            colorHex: "#FFE5EDF5"),
                        BuildButton(
                            _repeatingPaused ? "Repeat: resume" : "Repeat: pause",
                            ToggleRepeatingPaused,
                            width: 124,
                            colorHex: "#FFF1E5D8"),
                        BuildButton(
                            _repeatingReverse ? "Mode: reverse" : "Mode: restart",
                            ToggleRepeatingMode,
                            width: 124,
                            colorHex: "#FFE6F0E2"),
                    ]),
                new Container(
                    width: 240,
                    height: 72,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Center(
                        child: new DualTransitionBuilder(
                            animation: _explicitTransitionsController,
                            forwardBuilder: (_, animation, child) => new ScaleTransition(
                                scale: animation,
                                child: child),
                            reverseBuilder: (_, animation, child) => new RotationTransition(
                                turns: animation,
                                child: child),
                            child: new Container(
                                width: 64,
                                height: 40,
                                color: Color.Parse("#FF356A82"),
                                child: new Center(
                                    child: new Text("dual", fontSize: 12, color: Colors.White)))))),
                new Container(
                    width: 240,
                    height: 56,
                    color: Color.Parse("#FFF3F5F8"),
                    child: new Align(
                        alignment: Alignment.CenterLeft,
                        child: new RepeatingAnimationBuilder<double>(
                            animatable: new DoubleTween(begin: 64, end: 220),
                            duration: TimeSpan.FromMilliseconds(1400),
                            curve: Curves.EaseInOut,
                            repeatMode: _repeatingReverse ? RepeatMode.Reverse : RepeatMode.Restart,
                            paused: _repeatingPaused,
                            child: new Center(
                                child: new Text("repeat child", fontSize: 12, color: Colors.White)),
                            builder: (_, value, child) => new Container(
                                width: value,
                                height: 36,
                                color: Color.Parse("#FF6D7F47"),
                                child: child)))),
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

    private void ToggleExplicitTransitions()
    {
        SetState(() => _explicitTransitionsForward = !_explicitTransitionsForward);
        if (_explicitTransitionsForward)
        {
            _explicitTransitionsController.Forward();
        }
        else
        {
            _explicitTransitionsController.Reverse();
        }
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

    private void ToggleBuilderExtent()
    {
        SetState(() => _builderExtent = _builderExtent > 72 ? 72 : 180);
    }

    private void ToggleRepeatingPaused()
    {
        SetState(() => _repeatingPaused = !_repeatingPaused);
    }

    private void ToggleRepeatingMode()
    {
        SetState(() => _repeatingReverse = !_repeatingReverse);
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

    private sealed class DerivedAnimation<T> : Animation<T>
    {
        private readonly Animation<double> _parent;
        private readonly Func<double, T> _transform;

        public DerivedAnimation(Animation<double> parent, Func<double, T> transform)
        {
            _parent = parent;
            _transform = transform;
        }

        public override T Value => _transform(_parent.Value);

        public override AnimationStatus Status => _parent.Status;

        public override void AddListener(Action listener) => _parent.AddListener(listener);

        public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
            _parent.AddStatusListener(listener);
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _parent.RemoveStatusListener(listener);
        }
    }
}
