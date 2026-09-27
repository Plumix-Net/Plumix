using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.ImplicitAnimationsDartTestUtils;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart
// Mirrors flutter/packages/flutter/test/widgets/animated_container_test.dart
// Mirrors flutter/packages/flutter/test/widgets/animated_align_test.dart
// Mirrors flutter/packages/flutter/test/widgets/animated_padding_test.dart
// Mirrors flutter/packages/flutter/test/widgets/animated_positioned_test.dart
// Mirrors flutter/packages/flutter/test/widgets/default_text_style_test.dart (AnimatedDefaultTextStyle case)
// Mirrors flutter/packages/flutter/test/widgets/tween_animation_builder_test.dart
// Mirrors flutter/packages/flutter/test/widgets/implicit_animations_test.dart (SlideTransition zero area)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImplicitAnimationsWidgetsDartParityTests
{
    private static readonly TimeSpan Ms200 = TimeSpan.FromMilliseconds(200);

    // flutter_test's `hasOneLineDescription`.
    private static void AssertOneLineDescription(object value)
    {
        string description = value.ToString()!;
        Assert.NotEmpty(description);
        Assert.DoesNotContain("\n", description);
        Assert.DoesNotContain("Instance of ", description);
        Assert.Equal(description.Trim(), description);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

    // ---- animated_container_test.dart ----

    // Flutter: "AnimatedContainer.debugFillProperties"
    [Fact]
    public void AnimatedContainerDebugFillProperties()
    {
        var container = new AnimatedContainer(
            constraints: BoxConstraints.TightFor(width: 17.0, height: 23.0),
            decoration: new BoxDecoration(Color: new Color(0xFF00FF00)),
            foregroundDecoration: new BoxDecoration(Color: new Color(0x7F0000FF)),
            margin: EdgeInsets.All(10.0),
            padding: EdgeInsets.All(7.0),
            transform: Matrix4.TranslationValues(4.0, 3.0, 0.0),
            width: 50.0,
            height: 75.0,
            curve: Curves.Ease,
            duration: Ms200);
        AssertOneLineDescription(container);
    }

    // Flutter: "AnimatedContainer control test"
    [Fact]
    public void AnimatedContainerControlTest()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        var decorationA = new BoxDecoration(Color: new Color(0xFF00FF00));
        var decorationB = new BoxDecoration(Color: new Color(0xFF0000FF));

        tester.PumpWidget(new AnimatedContainer(key: key, duration: Ms200, decoration: decorationA));
        var box = (RenderDecoratedBox)key.CurrentContext!.FindRenderObject()!;
        Assert.Equal(decorationA.Color, ((BoxDecoration)box.Decoration).Color);

        tester.PumpWidget(new AnimatedContainer(key: key, duration: Ms200, decoration: decorationB));
        Assert.Same(box, key.CurrentContext!.FindRenderObject());
        Assert.Equal(decorationA.Color, ((BoxDecoration)box.Decoration).Color);

        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(decorationB.Color, ((BoxDecoration)box.Decoration).Color);

        // Dart then compares `box.toStringDeep()` with a dump whose `configuration:` line is
        // flutter_test's (`PlatformAssetBundle`, devicePixelRatio 3.0, android); the tree shape is
        // what this port pins.
        string deep = FrameworkDartTester.IgnoringHashCodes(box.ToStringDeep(minLevel: DiagnosticLevel.Info));
        Assert.StartsWith("RenderDecoratedBox#00000\n", deep, StringComparison.Ordinal);
        Assert.Contains(" └─child: RenderPadding#00000\n", deep, StringComparison.Ordinal);
        Assert.Contains("   └─child: RenderLimitedBox#00000\n", deep, StringComparison.Ordinal);
        Assert.Contains("     └─child: RenderConstrainedBox#00000\n", deep, StringComparison.Ordinal);
        Assert.Contains("additionalConstraints: BoxConstraints(biggest)", deep, StringComparison.Ordinal);
    }

    // Flutter: "AnimatedContainer overanimate test"
    [Fact]
    public void AnimatedContainerOveranimateTest()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new AnimatedContainer(duration: Ms200, color: new Color(0xFF00FF00)));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.PumpWidget(new AnimatedContainer(duration: Ms200, color: new Color(0xFF00FF00)));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.PumpWidget(new AnimatedContainer(duration: Ms200, color: new Color(0xFF0000FF)));
        Assert.Equal(1, Scheduler.TransientCallbackCount); // this is the only time an animation should have started!
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
        tester.PumpWidget(new AnimatedContainer(duration: Ms200, color: new Color(0xFF0000FF)));
        Assert.Equal(0, Scheduler.TransientCallbackCount);
    }

    // Flutter: "AnimatedContainer padding visual-to-directional animation"
    [Fact]
    public void AnimatedContainerPaddingVisualToDirectionalAnimation()
    {
        RunPaddingVisualToDirectional((padding, child) => new AnimatedContainer(
            duration: Ms200,
            padding: padding,
            child: child));
    }

    // Flutter: "AnimatedContainer alignment visual-to-directional animation"
    [Fact]
    public void AnimatedContainerAlignmentVisualToDirectionalAnimation()
    {
        RunAlignmentVisualToDirectional((alignment, child) => new AnimatedContainer(
            duration: Ms200,
            alignment: alignment,
            child: child));
    }

    // Flutter: "Animation rerun"
    [Fact]
    public void AnimationRerun()
    {
        using var tester = new FrameworkDartTester();
        Widget Build(double width, double height) => new Center(child: new AnimatedContainer(
            duration: Ms200,
            width: width,
            height: height,
            child: new Text("X", textDirection: TextDirection.Ltr)));
        RenderBox Text() => Box(tester.ElementsWithText("X").Single());

        tester.PumpWidget(Build(100.0, 100.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(100.0, Text().Size.Width);
        Assert.Equal(100.0, Text().Size.Height);
        tester.Pump(TimeSpan.FromMilliseconds(1000));

        tester.PumpWidget(Build(200.0, 200.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.InRange(Text().Size.Width, 110.0001, 189.9999);
        Assert.InRange(Text().Size.Height, 110.0001, 189.9999);
        tester.Pump(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(200.0, Text().Size.Width);
        Assert.Equal(200.0, Text().Size.Height);

        tester.PumpWidget(Build(200.0, 100.0));
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(200.0, Text().Size.Width);
        Assert.InRange(Text().Size.Height, 110.0001, 189.9999);
        tester.Pump(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(200.0, Text().Size.Width);
        Assert.Equal(100.0, Text().Size.Height);
    }

    // Flutter: "AnimatedContainer sets transformAlignment"
    [Fact]
    public void AnimatedContainerSetsTransformAlignment()
    {
        using var tester = new FrameworkDartTester();
        var target = new ValueKey<string>("target");
        Widget Build(Alignment transformAlignment) => new Center(child: new Directionality(
            textDirection: TextDirection.Ltr,
            child: new AnimatedContainer(
                duration: Ms200,
                transform: Matrix4.Diagonal3Values(0.5, 0.5, 1),
                transformAlignment: transformAlignment,
                child: new SizedBox(key: target, width: 100.0, height: 200.0))));
        Element Target() => tester.ElementsWithKey(target).Single();

        tester.PumpWidget(Build(Alignment.TopLeft));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(350.0, 200.0), GetTopLeft(Target()));

        tester.PumpWidget(Build(Alignment.BottomRight));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(350.0, 200.0), GetTopLeft(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(375.0, 250.0), GetTopLeft(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(400.0, 300.0), GetTopLeft(Target()));
    }

    // Flutter: "AnimatedContainer sets clipBehavior"
    [Fact]
    public void AnimatedContainerSetsClipBehavior()
    {
        using var tester = new FrameworkDartTester();
        var decoration = new BoxDecoration(Color: new Color(0xFFED1D7F));
        tester.PumpWidget(new AnimatedContainer(decoration: decoration, duration: Ms200));
        Assert.Equal(Clip.None, ((Container)tester.ElementsOfType<Container>().First().Widget).ClipBehavior);
        tester.PumpWidget(new AnimatedContainer(
            decoration: decoration,
            duration: Ms200,
            clipBehavior: Clip.AntiAlias));
        Assert.Equal(Clip.AntiAlias, ((Container)tester.ElementsOfType<Container>().First().Widget).ClipBehavior);
    }

    // Flutter: "AnimatedContainer does not crash at zero area"
    [Fact]
    public void AnimatedContainerDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(new AnimatedContainer(
                duration: Ms200,
                child: new Text("X"))))));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        tester.PumpAndSettle();
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<AnimatedContainer>()));
    }

    // ---- animated_align_test.dart ----

    // Flutter: "AnimatedAlign.debugFillProperties"
    [Fact]
    public void AnimatedAlignDebugFillProperties()
    {
        AssertOneLineDescription(new AnimatedAlign(
            alignment: Alignment.TopCenter,
            curve: Curves.Ease,
            duration: Ms200));
    }

    // Flutter: "AnimatedAlign alignment visual-to-directional animation"
    [Fact]
    public void AnimatedAlignAlignmentVisualToDirectionalAnimation()
    {
        RunAlignmentVisualToDirectional((alignment, child) => new AnimatedAlign(
            duration: Ms200,
            alignment: alignment,
            child: child));
    }

    // Flutter: "AnimatedAlign widthFactor"
    [Fact]
    public void AnimatedAlignWidthFactor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Row(
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new AnimatedAlign(
                        alignment: Alignment.Center,
                        curve: Curves.Ease,
                        widthFactor: 0.5,
                        duration: Ms200,
                        child: new SizedBox(height: 100.0, width: 100.0)),
                ])));
        Assert.Equal(50.0, GetSize(tester.ElementOfType<AnimatedAlign>()).Width);
    }

    // Flutter: "AnimatedAlign heightFactor"
    [Fact]
    public void AnimatedAlignHeightFactor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Column(
                children:
                [
                    new AnimatedAlign(
                        alignment: Alignment.Center,
                        curve: Curves.Ease,
                        heightFactor: 0.5,
                        duration: Ms200,
                        child: new SizedBox(height: 100.0, width: 100.0)),
                ])));
        Assert.Equal(50.0, GetSize(tester.ElementOfType<AnimatedAlign>()).Height);
    }

    // Flutter: "AnimatedAlign null height factor"
    [Fact]
    public void AnimatedAlignNullHeightFactor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new AnimatedAlign(
                        alignment: Alignment.Center,
                        curve: Curves.Ease,
                        duration: Ms200,
                        child: new SizedBox(height: 100.0, width: 100.0)),
                ])));
        Assert.Equal(new Size(100.0, 100), GetSize(tester.ElementsOfType<SizedBox>().Single()));
    }

    // Flutter: "AnimatedAlign null widthFactor"
    [Fact]
    public void AnimatedAlignNullWidthFactor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: SizedBox.Shrink(new Row(
                mainAxisSize: MainAxisSize.Min,
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new AnimatedAlign(
                        alignment: Alignment.Center,
                        curve: Curves.Ease,
                        duration: Ms200,
                        child: new SizedBox(height: 100.0, width: 100.0)),
                ]))));
        Assert.Equal(new Size(100.0, 100), GetSize(tester.ElementsOfType<SizedBox>().Last()));
    }

    // Flutter: "AnimatedAlign does not crash at zero area"
    [Fact]
    public void AnimatedAlignDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(new AnimatedAlign(
                alignment: Alignment.BottomCenter,
                duration: TimeSpan.FromMilliseconds(50))))));
        tester.PumpAndSettle();
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<AnimatedAlign>()));
    }

    // ---- animated_padding_test.dart ----

    // Flutter: "AnimatedPadding.debugFillProperties"
    [Fact]
    public void AnimatedPaddingDebugFillProperties()
    {
        AssertOneLineDescription(new AnimatedPadding(
            padding: EdgeInsets.All(7.0),
            curve: Curves.Ease,
            duration: Ms200));
    }

    // Flutter: "AnimatedPadding padding visual-to-directional animation"
    [Fact]
    public void AnimatedPaddingPaddingVisualToDirectionalAnimation()
    {
        RunPaddingVisualToDirectional((padding, child) => new AnimatedPadding(
            duration: Ms200,
            padding: padding,
            child: child));
    }

    // Flutter: "AnimatedPadding animated padding clamped to positive values"
    [Fact]
    public void AnimatedPaddingAnimatedPaddingClampedToPositiveValues()
    {
        using var tester = new FrameworkDartTester();
        var target = new ValueKey<string>("target");
        Widget Build(EdgeInsetsGeometry padding) => new Directionality(
            textDirection: TextDirection.Rtl,
            child: new AnimatedPadding(
                curve: Curves.EaseInOutBack,
                duration: Ms200,
                padding: padding,
                child: SizedBox.Expand(key: target)));
        Element Target() => tester.ElementsWithKey(target).Single();

        tester.PumpWidget(Build(EdgeInsets.Only(right: 50.0)));
        Assert.Equal(new Size(750.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(750.0, 0.0), GetTopRight(Target()));

        tester.PumpWidget(Build(EdgeInsets.Zero));
        Assert.Equal(new Size(750.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(750.0, 0.0), GetTopRight(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(128));
        // Curve would take the padding negative; it is clamped to non-negative instead.
        Assert.Equal(new Size(800.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(800.0, 0.0), GetTopRight(Target()));
    }

    // Flutter: "AnimatedPadding does not crash at zero area"
    [Fact]
    public void AnimatedPaddingDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(new AnimatedPadding(
                duration: Ms200,
                padding: EdgeInsets.All(1),
                child: new Text("X"))))));
        tester.Pump(TimeSpan.FromMilliseconds(100));
        tester.PumpAndSettle();
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<AnimatedPadding>()));
    }

    // ---- animated_positioned_test.dart ----

    // Flutter: "AnimatedPositioned.fromRect control test"
    [Fact]
    public void AnimatedPositionedFromRectControlTest()
    {
        var positioned = AnimatedPositioned.FromRect(
            rect: new Rect(7.0, 5.0, 12.0, 16.0),
            duration: Ms200,
            child: new Container());
        Assert.Equal(7.0, positioned.Left);
        Assert.Equal(5.0, positioned.Top);
        Assert.Equal(12.0, positioned.Width);
        Assert.Equal(16.0, positioned.Height);
        AssertOneLineDescription(positioned);
    }

    // Flutter: "AnimatedPositioned - basics (VISUAL)"
    [Fact]
    public void AnimatedPositionedBasics()
    {
        RunPositionedBasics(
            TextDirection.Ltr,
            (key, left, top, width, height) => new AnimatedPositioned(
                left: left,
                top: top,
                width: width,
                height: height,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            new Point(50.0 + (70.0 / 2.0), 30.0 + (110.0 / 2.0)),
            new Point(37.0 + (59.0 / 2.0), 31.0 + (71.0 / 2.0)),
            "left=37.0",
            "Offset(37.0, 31.0)");
    }

    // Flutter: "AnimatedPositionedDirectional - basics (LTR)"
    [Fact]
    public void AnimatedPositionedDirectionalBasicsLtr()
    {
        RunPositionedBasics(
            TextDirection.Ltr,
            (key, start, top, width, height) => new AnimatedPositionedDirectional(
                start: start,
                top: top,
                width: width,
                height: height,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            new Point(50.0 + (70.0 / 2.0), 30.0 + (110.0 / 2.0)),
            new Point(37.0 + (59.0 / 2.0), 31.0 + (71.0 / 2.0)),
            "left=37.0",
            "Offset(37.0, 31.0)");
    }

    // Flutter: "AnimatedPositionedDirectional - basics (RTL)"
    [Fact]
    public void AnimatedPositionedDirectionalBasicsRtl()
    {
        RunPositionedBasics(
            TextDirection.Rtl,
            (key, start, top, width, height) => new AnimatedPositionedDirectional(
                start: start,
                top: top,
                width: width,
                height: height,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            new Point(800.0 - 50.0 - (70.0 / 2.0), 30.0 + (110.0 / 2.0)),
            new Point(800.0 - 37.0 - (59.0 / 2.0), 31.0 + (71.0 / 2.0)),
            "right=37.0",
            "Offset(704.0, 31.0)");
    }

    // Flutter: "AnimatedPositioned - interrupted animation (VISUAL)"
    [Fact]
    public void AnimatedPositionedInterruptedAnimation()
    {
        RunInterrupted(
            TextDirection.Ltr,
            (key, start, top) => new AnimatedPositioned(
                left: start,
                top: top,
                width: 100.0,
                height: 100.0,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            [(50, 50), (50, 50), (50, 50), (100, 100), (100, 100), (150, 150), (200, 200)]);
    }

    // Flutter: "AnimatedPositionedDirectional - interrupted animation (LTR)"
    [Fact]
    public void AnimatedPositionedDirectionalInterruptedAnimationLtr()
    {
        RunInterrupted(
            TextDirection.Ltr,
            (key, start, top) => new AnimatedPositionedDirectional(
                start: start,
                top: top,
                width: 100.0,
                height: 100.0,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            [(50, 50), (50, 50), (50, 50), (100, 100), (100, 100), (150, 150), (200, 200)]);
    }

    // Flutter: "AnimatedPositionedDirectional - interrupted animation (RTL)"
    [Fact]
    public void AnimatedPositionedDirectionalInterruptedAnimationRtl()
    {
        RunInterrupted(
            TextDirection.Rtl,
            (key, start, top) => new AnimatedPositionedDirectional(
                start: start,
                top: top,
                width: 100.0,
                height: 100.0,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            [(750, 50), (750, 50), (750, 50), (700, 100), (700, 100), (650, 150), (600, 200)]);
    }

    // Flutter: "AnimatedPositioned - switching variables (VISUAL)"
    [Fact]
    public void AnimatedPositionedSwitchingVariables()
    {
        RunSwitchingVariables(
            TextDirection.Ltr,
            key => new AnimatedPositioned(
                left: 0.0,
                top: 0.0,
                width: 100.0,
                height: 100.0,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            key => new AnimatedPositioned(
                left: 0.0,
                top: 100.0,
                right: 100.0, // 700.0 from the left
                height: 100.0,
                duration: TimeSpan.FromSeconds(2),
                child: new Container(key: key)),
            [(50, 50), (50, 50), (350, 50), (350, 100), (350, 150)]);
    }

    // Flutter: "AnimatedPositionedDirectional - switching variables (LTR)"
    [Fact]
    public void AnimatedPositionedDirectionalSwitchingVariablesLtr()
    {
        RunSwitchingVariables(
            TextDirection.Ltr,
            DirectionalStart,
            DirectionalSwitched,
            [(50, 50), (50, 50), (350, 50), (350, 100), (350, 150)]);
    }

    // Flutter: "AnimatedPositionedDirectional - switching variables (RTL)"
    [Fact]
    public void AnimatedPositionedDirectionalSwitchingVariablesRtl()
    {
        RunSwitchingVariables(
            TextDirection.Rtl,
            DirectionalStart,
            DirectionalSwitched,
            [(750, 50), (750, 50), (450, 50), (450, 100), (450, 150)]);
    }

    // Flutter: "AnimatedPositionedDirectional does not crash at zero area"
    [Fact]
    public void AnimatedPositionedDirectionalDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: SizedBox.Shrink(new Stack(children:
            [
                new AnimatedPositionedDirectional(
                    duration: TimeSpan.FromMilliseconds(300),
                    child: new Text("X")),
            ])))));
        tester.PumpAndSettle();
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<AnimatedPositionedDirectional>()));
    }

    // ---- default_text_style_test.dart ----

    // Flutter: "AnimatedDefaultTextStyle changes propagate to Text"
    [Fact]
    public void AnimatedDefaultTextStyleChangesPropagateToText()
    {
        using var tester = new FrameworkDartTester();
        var textWidget = new Text("Hello", textDirection: TextDirection.Ltr);
        var s1 = new TextStyle(FontSize: 10.0, FontWeight: (FontWeight)800, Height: 123.0);
        var s2 = new TextStyle(FontSize: 20.0, FontWeight: (FontWeight)200, Height: 1.0);
        RichText FirstRichText() => (RichText)tester.ElementsOfType<RichText>().First().Widget;
        // Plumix's Text fills a missing color with black (DIVERGENCES.md, Text row); Dart passes s1 through.
        TextStyle Resolved(TextStyle style) => style.CopyWith(color: new Color(0xFF000000));

        tester.PumpWidget(new AnimatedDefaultTextStyle(
            style: s1,
            duration: TimeSpan.FromMilliseconds(1000),
            child: textWidget));
        RichText text1 = FirstRichText();
        Assert.Equal(Resolved(s1), text1.Text.Style);
        Assert.Equal(TextAlign.Start, text1.TextAlign);
        Assert.True(text1.SoftWrap);
        Assert.Equal(TextOverflow.Clip, text1.Overflow);
        Assert.Null(text1.MaxLines);
        Assert.Equal(TextWidthBasis.Parent, text1.TextWidthBasis);
        Assert.Null(text1.TextHeightBehavior);

        tester.PumpWidget(new AnimatedDefaultTextStyle(
            style: s2,
            textAlign: TextAlign.Justify,
            softWrap: false,
            overflow: TextOverflow.Fade,
            maxLines: 3,
            textWidthBasis: TextWidthBasis.LongestLine,
            textHeightBehavior: new TextHeightBehavior(ApplyHeightToFirstAscent: false),
            duration: TimeSpan.FromMilliseconds(1000),
            child: textWidget));
        RichText text2 = FirstRichText();
        Assert.Equal(Resolved(s1), text2.Text.Style); // animation hasn't started yet
        Assert.Equal(TextAlign.Justify, text2.TextAlign);
        Assert.False(text2.SoftWrap);
        Assert.Equal(TextOverflow.Fade, text2.Overflow);
        Assert.Equal(3, text2.MaxLines);
        Assert.Equal(TextWidthBasis.LongestLine, text2.TextWidthBasis);
        Assert.Equal(new TextHeightBehavior(ApplyHeightToFirstAscent: false), text2.TextHeightBehavior);

        tester.Pump(TimeSpan.FromMilliseconds(1000));
        RichText text3 = FirstRichText();
        Assert.Equal(Resolved(s2), text3.Text.Style);
        Assert.Equal(TextAlign.Justify, text3.TextAlign);
        Assert.False(text3.SoftWrap);
        Assert.Equal(TextOverflow.Fade, text3.Overflow);
        Assert.Equal(3, text3.MaxLines);
        Assert.Equal(TextWidthBasis.LongestLine, text2.TextWidthBasis);
        Assert.Equal(new TextHeightBehavior(ApplyHeightToFirstAscent: false), text2.TextHeightBehavior);
    }

    // ---- tween_animation_builder_test.dart ----

    private static TweenAnimationBuilder<int> IntBuilder(
        List<int> values,
        IntTween tween,
        TimeSpan? duration = null,
        Curve? curve = null,
        Action? onEnd = null)
    {
        return new TweenAnimationBuilder<int>(
            tween: tween,
            duration: duration ?? TimeSpan.FromSeconds(1),
            curve: curve,
            onEnd: onEnd,
            builder: (_, value, _) =>
            {
                values.Add(value);
                return new Placeholder();
            });
    }

    // Flutter: "Animates forward when built"
    [Fact]
    public void AnimatesForwardWhenBuilt()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        int endCount = 0;
        tester.PumpWidget(IntBuilder(values, new IntTween(10, 110), onEnd: () => endCount++));
        Assert.Equal(0, endCount);
        Assert.Equal([10], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([10, 60], values);
        tester.Pump(TimeSpan.FromMilliseconds(501));
        Assert.Equal(1, endCount);
        Assert.Equal([10, 60, 110], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1, endCount);
        Assert.Equal([10, 60, 110], values);
    }

    // Flutter: "No initial animation when begin=null"
    [Fact]
    public void NoInitialAnimationWhenBeginIsNull()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        int endCount = 0;
        var tween = new IntTween();
        tween.End = 100;
        tester.PumpWidget(IntBuilder(values, tween, onEnd: () => endCount++));
        Assert.Equal(0, endCount);
        Assert.Equal([100], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal(0, endCount);
        Assert.Equal([100], values);
    }

    // Flutter: "No initial animation when begin=end"
    [Fact]
    public void NoInitialAnimationWhenBeginEqualsEnd()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        int endCount = 0;
        tester.PumpWidget(IntBuilder(values, new IntTween(100, 100), onEnd: () => endCount++));
        Assert.Equal(0, endCount);
        Assert.Equal([100], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal(0, endCount);
        Assert.Equal([100], values);
    }

    // Flutter: "Replace tween animates new tween"
    [Fact]
    public void ReplaceTweenAnimatesNewTween()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        Assert.Equal([0], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([0, 100], values);

        tester.PumpWidget(IntBuilder(values, new IntTween(100, 200)));
        Assert.Equal([0, 100, 100], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 100, 100, 150], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 100, 100, 150, 200], values);
    }

    // Flutter: "Curve is respected"
    [Fact]
    public void CurveIsRespected()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100), curve: Curves.EaseInExpo));
        Assert.Equal([0], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.InRange(values.Last(), 1, 49);
        tester.Pump(TimeSpan.FromSeconds(2));

        values.Clear();
        tester.PumpWidget(IntBuilder(values, new IntTween(100, 200), curve: Curves.Linear));
        Assert.Equal([100], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([100, 150], values);
    }

    // Flutter: "Duration is respected"
    [Fact]
    public void DurationIsRespected()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        Assert.Equal([0], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50], values);
        tester.Pump(TimeSpan.FromSeconds(2));

        values.Clear();
        tester.PumpWidget(IntBuilder(values, new IntTween(100, 200), duration: TimeSpan.FromSeconds(2)));
        Assert.Equal([100], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([100, 125], values);
    }

    // Flutter: "Child is integrated into tree"
    [Fact]
    public void ChildIsIntegratedIntoTree()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new TweenAnimationBuilder<int>(
                tween: new IntTween(0, 100),
                duration: TimeSpan.FromSeconds(1),
                child: new Text("Hello World"),
                builder: (_, _, child) => child!)));
        Assert.Single(tester.ElementsWithText("Hello World"));
    }

    // Flutter: "Change tween gapless while" / "running forward"
    [Fact]
    public void ChangeTweenGaplessWhileRunningForward()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        Assert.Equal([0], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50], values);

        tester.PumpWidget(IntBuilder(values, new IntTween(200, 300)));
        Assert.Equal([0, 50, 50], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50, 50, 175], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([0, 50, 50, 175, 300], values);
    }

    // Flutter: "Change tween gapless while" / "running forward and then reverse with same tween instance"
    [Fact]
    public void ChangeTweenGaplessWhileRunningForwardAndThenReverseWithSameTweenInstance()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        var tween1 = new IntTween(0, 100);
        var tween2 = new IntTween(200, 300);
        tester.PumpWidget(IntBuilder(values, tween1));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.PumpWidget(IntBuilder(values, tween2));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([0, 50, 50, 175, 300], values);
    }

    // Flutter: "Changing tween while gapless tween change is in progress"
    [Fact]
    public void ChangingTweenWhileGaplessTweenChangeIsInProgress()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50], values);
        values.Clear();

        tester.PumpWidget(IntBuilder(values, new IntTween(200, 300)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([50, 175], values);
        values.Clear();

        tester.PumpWidget(IntBuilder(values, new IntTween(400, 501)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([175, 338, 501], values);
    }

    // Flutter: "Changing curve while no animation is running does not trigger animation"
    [Fact]
    public void ChangingCurveWhileNoAnimationIsRunningDoesNotTriggerAnimation()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        var tween = new IntTween(0, 100);
        tester.PumpWidget(IntBuilder(values, tween, curve: Curves.Linear));
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([0, 100], values);
        values.Clear();

        tester.PumpWidget(IntBuilder(values, tween, curve: Curves.EaseInExpo));
        Assert.Equal([100], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([100], values);
    }

    // Flutter: "Setting same tween and direction does not trigger animation"
    [Fact]
    public void SettingSameTweenAndDirectionDoesNotTriggerAnimation()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50, 100], values);
        values.Clear();

        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.All(values, value => Assert.Equal(100, value));
    }

    // Flutter: "Setting same tween and direction while gapless animation is in progress works"
    [Fact]
    public void SettingSameTweenAndDirectionWhileGaplessAnimationIsInProgressWorks()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<int>();
        tester.PumpWidget(IntBuilder(values, new IntTween(0, 100)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50], values);

        tester.PumpWidget(IntBuilder(values, new IntTween(200, 300)));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50, 50, 175], values);

        tester.PumpWidget(IntBuilder(values, new IntTween(200, 300)));
        Assert.Equal([0, 50, 50, 175, 175], values);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal([0, 50, 50, 175, 175, 300], values);

        values.Clear();
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.All(values, value => Assert.Equal(300, value));
    }

    // Flutter: "Works with nullable tweens"
    [Fact]
    public void WorksWithNullableTweens()
    {
        using var tester = new FrameworkDartTester();
        var values = new List<Size?>();
        tester.PumpWidget(new TweenAnimationBuilder<Size?>(
            tween: new SizeTween(end: new Size(10, 10)),
            duration: TimeSpan.FromSeconds(1),
            builder: (_, value, _) =>
            {
                values.Add(value);
                return new Placeholder();
            }));
        Assert.Equal([new Size(10, 10)], values);
        tester.Pump(TimeSpan.FromSeconds(2));
        Assert.Equal([new Size(10, 10)], values);
    }

    // Flutter: "TweenAnimationBuilder does not crash at zero area"
    [Fact]
    public void TweenAnimationBuilderDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(0, 0));
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: new TweenAnimationBuilder<Size?>(
                tween: new SizeTween(end: new Size(10, 10)),
                duration: TimeSpan.FromSeconds(1),
                builder: (_, _, _) => new Placeholder()))));
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<TweenAnimationBuilder<Size?>>()));
        tester.PumpAndSettle();
    }

    // implicit_animations_test.dart: "SlideTransition does not crash at zero area"
    [Fact]
    public void SlideTransitionDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(0, 0));
        using var controller = new AnimationController(
            value: 1,
            duration: TimeSpan.FromSeconds(2));
        using var animation = new CurvedAnimation(parent: controller, curve: Curves.Linear);
        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(child: new SlideTransition(
                position: new VectorTween(new Vector(0, 0), new Vector(1.5, 0.0)).Animate(animation),
                child: new Placeholder()))));
        Assert.Equal(new Size(0, 0), GetSize(tester.ElementOfType<SlideTransition>()));
    }

    // ---- shared bodies ----

    private static void RunPaddingVisualToDirectional(Func<EdgeInsetsGeometry, Widget, Widget> build)
    {
        using var tester = new FrameworkDartTester();
        var target = new ValueKey<string>("target");
        Widget Tree(EdgeInsetsGeometry padding) => new Directionality(
            textDirection: TextDirection.Rtl,
            child: build(padding, SizedBox.Expand(key: target)));
        Element Target() => tester.ElementsWithKey(target).Single();

        tester.PumpWidget(Tree(EdgeInsets.Only(right: 50.0)));
        Assert.Equal(new Size(750.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(750.0, 0.0), GetTopRight(Target()));

        tester.PumpWidget(Tree(EdgeInsetsDirectional.Only(start: 100.0)));
        Assert.Equal(new Size(750.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(750.0, 0.0), GetTopRight(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(new Size(725.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(725.0, 0.0), GetTopRight(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Size(700.0, 600.0), GetSize(Target()));
        Assert.Equal(new Point(700.0, 0.0), GetTopRight(Target()));
    }

    private static void RunAlignmentVisualToDirectional(Func<AlignmentGeometry, Widget, Widget> build)
    {
        using var tester = new FrameworkDartTester();
        var target = new ValueKey<string>("target");
        Widget Tree(AlignmentGeometry alignment) => new Directionality(
            textDirection: TextDirection.Rtl,
            child: build(alignment, new SizedBox(key: target, width: 100.0, height: 200.0)));
        Element Target() => tester.ElementsWithKey(target).Single();

        tester.PumpWidget(Tree(Alignment.TopRight));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(800.0, 0.0), GetTopRight(Target()));

        tester.PumpWidget(Tree(AlignmentDirectional.BottomStart));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(800.0, 0.0), GetTopRight(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(800.0, 200.0), GetTopRight(Target()));

        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(new Size(100.0, 200.0), GetSize(Target()));
        Assert.Equal(new Point(800.0, 400.0), GetTopRight(Target()));
    }

    private static void RunPositionedBasics(
        TextDirection direction,
        Func<Key, double, double, double, double, Widget> build,
        Point first,
        Point last,
        string horizontalParentData,
        string offset)
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        Widget Tree(double start, double top, double width, double height) => new Directionality(
            textDirection: direction,
            child: new Stack(children: [build(key, start, top, width, height)]));
        RenderBox Box() => (RenderBox)key.CurrentContext!.FindRenderObject()!;

        tester.PumpWidget(Tree(50.0, 30.0, 70.0, 110.0));
        Assert.Equal(first, GetCenter(Box()));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(first, GetCenter(Box()));

        tester.PumpWidget(Tree(37.0, 31.0, 59.0, 71.0));
        Assert.Equal(first, GetCenter(Box()));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(Lerp(first, last, 0.5), GetCenter(Box()));
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(last, GetCenter(Box()));

        string deep = FrameworkDartTester.IgnoringHashCodes(Box().ToStringDeep(minLevel: DiagnosticLevel.Info));
        Assert.Equal(
            "RenderLimitedBox#00000\n"
            + $" │ parentData: top=31.0; {horizontalParentData}; width=59.0; height=71.0;\n"
            + $" │   offset={offset} (can use size)\n"
            + " │ constraints: BoxConstraints(w=59.0, h=71.0)\n"
            + " │ size: Size(59.0, 71.0)\n"
            + " │ maxWidth: 0.0\n"
            + " │ maxHeight: 0.0\n"
            + " │\n"
            + " └─child: RenderConstrainedBox#00000\n"
            + "     parentData: <none> (can use size)\n"
            + "     constraints: BoxConstraints(w=59.0, h=71.0)\n"
            + "     size: Size(59.0, 71.0)\n"
            + "     additionalConstraints: BoxConstraints(biggest)\n",
            deep);
    }

    private static void RunInterrupted(
        TextDirection direction,
        Func<Key, double, double, Widget> build,
        (double X, double Y)[] centers)
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        Widget Tree(double start, double top) => new Directionality(
            textDirection: direction,
            child: new Stack(children: [build(key, start, top)]));
        Point Center() => GetCenter((RenderBox)key.CurrentContext!.FindRenderObject()!);

        tester.PumpWidget(Tree(0.0, 0.0));
        Assert.Equal(new Point(centers[0].X, centers[0].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[1].X, centers[1].Y), Center());

        tester.PumpWidget(Tree(100.0, 100.0));
        Assert.Equal(new Point(centers[2].X, centers[2].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[3].X, centers[3].Y), Center());

        tester.PumpWidget(Tree(150.0, 150.0));
        Assert.Equal(new Point(centers[4].X, centers[4].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[5].X, centers[5].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[6].X, centers[6].Y), Center());
    }

    private static Widget DirectionalStart(Key key)
    {
        return new AnimatedPositionedDirectional(
            start: 0.0,
            top: 0.0,
            width: 100.0,
            height: 100.0,
            duration: TimeSpan.FromSeconds(2),
            child: new Container(key: key));
    }

    private static Widget DirectionalSwitched(Key key)
    {
        return new AnimatedPositionedDirectional(
            start: 0.0,
            top: 100.0,
            end: 100.0, // 700.0 from the start
            height: 100.0,
            duration: TimeSpan.FromSeconds(2),
            child: new Container(key: key));
    }

    private static void RunSwitchingVariables(
        TextDirection direction,
        Func<Key, Widget> buildStart,
        Func<Key, Widget> buildSwitched,
        (double X, double Y)[] centers)
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        Widget Tree(Widget positioned) => new Directionality(
            textDirection: direction,
            child: new Stack(children: [positioned]));
        Point Center() => GetCenter((RenderBox)key.CurrentContext!.FindRenderObject()!);

        tester.PumpWidget(Tree(buildStart(key)));
        Assert.Equal(new Point(centers[0].X, centers[0].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[1].X, centers[1].Y), Center());

        tester.PumpWidget(Tree(buildSwitched(key)));
        Assert.Equal(new Point(centers[2].X, centers[2].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[3].X, centers[3].Y), Center());
        tester.Pump(TimeSpan.FromSeconds(1));
        Assert.Equal(new Point(centers[4].X, centers[4].Y), Center());
    }
}
