using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/implicit_animations.dart
// Mirrors flutter/packages/flutter/test/widgets/implicit_animations_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImplicitAnimationsDartParityTests
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan AdditionalDelay = TimeSpan.FromMilliseconds(1);
    private static readonly Key SwitchKey = new ValueKey<string>("switchKey");

    private int _onEndCalled;

    private void OnEnd() => _onEndCalled++;

    private static Widget Wrap(Widget child)
    {
        return new Directionality(textDirection: TextDirection.Ltr, child: new Center(child: child));
    }

    // Flutter: "BoxConstraintsTween control test"
    [Fact]
    public void BoxConstraintsTweenControlTest()
    {
        var tween = new BoxConstraintsTween(
            begin: BoxConstraints.Tight(new Size(20.0, 50.0)),
            end: BoxConstraints.Tight(new Size(10.0, 30.0)));
        BoxConstraints result = tween.Lerp(0.25);
        Assert.Equal(17.5, result.MinWidth);
        Assert.Equal(17.5, result.MaxWidth);
        Assert.Equal(45.0, result.MinHeight);
        Assert.Equal(45.0, result.MaxHeight);
    }

    // Flutter: "DecorationTween control test"
    [Fact]
    public void DecorationTweenControlTest()
    {
        var tween = new DecorationTween(
            begin: new BoxDecoration(Color: new Color(0xFF00FF00)),
            end: new BoxDecoration(Color: new Color(0xFFFFFF00)));
        var result = (BoxDecoration)tween.Lerp(0.25);
        ColorMatchers.AssertSameColorAs(new Color(0xFF3FFF00), result.Color);
    }

    // Flutter: "EdgeInsetsTween control test"
    [Fact]
    public void EdgeInsetsTweenControlTest()
    {
        var tween = new EdgeInsetsTween(
            begin: EdgeInsets.Symmetric(vertical: 50.0),
            end: EdgeInsets.Only(top: 10.0, bottom: 30.0));
        EdgeInsets result = tween.Lerp(0.25);
        Assert.Equal(0.0, result.Left);
        Assert.Equal(0.0, result.Right);
        Assert.Equal(40.0, result.Top);
        Assert.Equal(45.0, result.Bottom);
    }

    // Flutter: "Matrix4Tween control test"
    [Fact]
    public void Matrix4TweenControlTest()
    {
        var tween = new Matrix4Tween(
            begin: Matrix4.TranslationValues(10.0, 20.0, 30.0),
            end: Matrix4.TranslationValues(14.0, 24.0, 34.0));
        Assert.Equal(Matrix4.TranslationValues(11.0, 21.0, 31.0), tween.Lerp(0.25));
    }

    // Flutter: "AnimatedContainer onEnd callback test" ... "TweenAnimationBuilder onEnd callback test"
    [Theory]
    [InlineData(nameof(AnimatedContainer))]
    [InlineData(nameof(AnimatedPadding))]
    [InlineData(nameof(AnimatedAlign))]
    [InlineData(nameof(AnimatedPositioned))]
    [InlineData(nameof(AnimatedPositionedDirectional))]
    [InlineData(nameof(AnimatedSlide))]
    [InlineData(nameof(AnimatedScale))]
    [InlineData(nameof(AnimatedRotation))]
    [InlineData(nameof(AnimatedOpacity))]
    [InlineData(nameof(AnimatedFractionallySizedBox))]
    [InlineData(nameof(SliverAnimatedOpacity))]
    [InlineData(nameof(AnimatedDefaultTextStyle))]
    [InlineData(nameof(AnimatedPhysicalModel))]
    [InlineData("TweenAnimationBuilder")]
    public void OnEndCallbackTest(string widget)
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var testWidget = new TestAnimatedWidget(CreateState(widget), SwitchKey, OnEnd);
        tester.PumpWidget(widget == nameof(SliverAnimatedOpacity) ? testWidget : Wrap(testWidget));

        Element switchElement = tester.ElementsWithKey(SwitchKey).Single();
        tester.Tap(switchElement);
        tester.Pump();
        Assert.Equal(0, _onEndCalled);
        tester.Pump(AnimationDuration);
        Assert.Equal(0, _onEndCalled);
        tester.Pump(AdditionalDelay);
        Assert.Equal(1, _onEndCalled);

        TapTest2And3(tester, switchElement);
    }

    // Flutter: "AnimatedSlide transition test"
    [Fact]
    public void AnimatedSlideTransitionTest()
    {
        RunTransitionTest<SlideTransition, Vector>(
            new TestAnimatedSlideWidgetState(),
            transition => transition.Position,
            [new Vector(0, 0), new Vector(0.5, 0.5), new Vector(0.75, 0.75), new Vector(1, 1)]);
    }

    // Flutter: "AnimatedScale transition test"
    [Fact]
    public void AnimatedScaleTransitionTest()
    {
        RunTransitionTest<ScaleTransition, double>(
            new TestAnimatedScaleWidgetState(),
            transition => transition.Scale,
            [1.0, 1.5, 1.75, 2.0]);
    }

    // Flutter: "AnimatedRotation transition test"
    [Fact]
    public void AnimatedRotationTransitionTest()
    {
        RunTransitionTest<RotationTransition, double>(
            new TestAnimatedRotationWidgetState(),
            transition => transition.Turns,
            [0.0, 0.75, 1.125, 1.5]);
    }

    // Flutter: "AnimatedOpacity transition test"
    [Fact]
    public void AnimatedOpacityTransitionTest()
    {
        RunTransitionTest<FadeTransition, double>(
            new TestAnimatedOpacityWidgetState(),
            transition => transition.Opacity,
            [0.0, 0.5, 0.75, 1.0]);
    }

    // Flutter: "SliverAnimatedOpacity transition test"
    [Fact]
    public void SliverAnimatedOpacityTransitionTest()
    {
        RunTransitionTest<SliverFadeTransition, double>(
            new TestSliverAnimatedOpacityWidgetState(),
            transition => transition.Opacity,
            [0.0, 0.5, 0.75, 1.0]);
    }

    // Flutter: "Ensure CurvedAnimations are disposed on widget change"
    [Fact]
    public void EnsureCurvedAnimationsAreDisposedOnWidgetChange()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<ImplicitlyAnimatedWidgetState<AnimatedOpacity>>(null);
        var curve = new ValueNotifier<Curve>(new Interval(0.0, 0.5));
        tester.PumpWidget(Wrap(new ValueListenableBuilder<Curve>(
            valueListenable: curve,
            builder: (_, currentCurve, _) => new AnimatedOpacity(
                key: key,
                opacity: 1.0,
                duration: TimeSpan.FromSeconds(1),
                curve: currentCurve,
                child: new Container(color: new Color(0xFF00FF00))))));

        ImplicitlyAnimatedWidgetState<AnimatedOpacity>? firstState = key.CurrentState;
        Animation<double>? firstAnimation = firstState?.Animation;
        Assert.NotNull(firstAnimation);
        var firstCurvedAnimation = (CurvedAnimation)firstAnimation;
        Assert.False(firstCurvedAnimation.IsDisposed);

        curve.Value = new Interval(0.0, 0.6);
        tester.PumpAndSettle();

        ImplicitlyAnimatedWidgetState<AnimatedOpacity>? secondState = key.CurrentState;
        Animation<double>? secondAnimation = secondState?.Animation;
        Assert.NotNull(secondAnimation);
        var secondCurvedAnimation = (CurvedAnimation)secondAnimation;
        Assert.Same(firstState, secondState);
        Assert.NotEqual(firstAnimation, secondAnimation);
        Assert.True(firstCurvedAnimation.IsDisposed);
        Assert.False(secondCurvedAnimation.IsDisposed);

        tester.PumpWidget(Wrap(new Offstage()));
        tester.PumpAndSettle();
        Assert.True(secondCurvedAnimation.IsDisposed);
    }

    // Flutter: "Verify that default args match non-animated variants" / "PhysicalModel default args"
    [Fact]
    public void PhysicalModelDefaultArgs()
    {
        var animatedPhysicalModel = new AnimatedPhysicalModel(
            duration: TimeSpan.Zero,
            color: new Color(0x00000000),
            shadowColor: new Color(0x00000000),
            child: SizedBox.Shrink());
        var physicalModel = new PhysicalModel(
            color: new Color(0x00000000),
            shadowColor: new Color(0x00000000),
            child: SizedBox.Shrink());
        Assert.Equal(physicalModel.Shape, animatedPhysicalModel.Shape);
        Assert.Equal(physicalModel.ClipBehavior, animatedPhysicalModel.ClipBehavior);
        Assert.Equal(physicalModel.BorderRadius, animatedPhysicalModel.BorderRadius);
    }

    // Flutter: "AnimatedSlide does not crash at zero area"
    [Fact]
    public void AnimatedSlideDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedSlide>(SizedBox.Shrink(new AnimatedSlide(
            offset: new Vector(100, 0),
            duration: TimeSpan.FromMilliseconds(300))));
    }

    // Flutter: "AnimatedScale does not crash at zero area"
    [Fact]
    public void AnimatedScaleDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedScale>(SizedBox.Shrink(new AnimatedScale(
            scale: 2,
            duration: TimeSpan.FromMilliseconds(300),
            child: new Text("X"))));
    }

    // Flutter: "AnimatedRotation does not crash at zero area"
    [Fact]
    public void AnimatedRotationDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedRotation>(SizedBox.Shrink(new AnimatedRotation(
            turns: 0.75,
            duration: TimeSpan.FromMilliseconds(300),
            child: new Text("X"))));
    }

    // Flutter: "AnimatedOpacity does not crash at zero area"
    [Fact]
    public void AnimatedOpacityDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedOpacity>(new SizedBox(child: new AnimatedOpacity(
            opacity: 0.5,
            duration: TimeSpan.FromMilliseconds(300))));
    }

    // Flutter: "AnimatedDefaultTextStyle does not crash at zero area"
    [Fact]
    public void AnimatedDefaultTextStyleDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedDefaultTextStyle>(SizedBox.Shrink(new AnimatedDefaultTextStyle(
            style: new TextStyle(FontStyle: FontStyle.Italic),
            duration: TimeSpan.FromMilliseconds(300),
            child: new Text("X"))));
    }

    // Flutter: "AnimatedPhysicalModel does not crash at zero area"
    [Fact]
    public void AnimatedPhysicalModelDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedPhysicalModel>(SizedBox.Shrink(new AnimatedPhysicalModel(
            color: new Color(0xFF009688),
            shadowColor: new Color(0xFF64FFDA),
            duration: TimeSpan.FromMilliseconds(300),
            child: new Text("X"))));
    }

    // Flutter: "AnimatedFractionallySizedBox does not crash at zero area"
    [Fact]
    public void AnimatedFractionallySizedBoxDoesNotCrashAtZeroArea()
    {
        AssertZeroArea<AnimatedFractionallySizedBox>(SizedBox.Shrink(new AnimatedFractionallySizedBox(
            duration: TimeSpan.FromMilliseconds(300),
            widthFactor: 0.5,
            heightFactor: 0.5)));
    }

    private static void AssertZeroArea<TWidget>(Widget tree) where TWidget : Widget
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(tree));
        tester.PumpAndSettle();
        Assert.Equal(new Size(0, 0), ImplicitAnimationsDartTestUtils.GetSize(tester.ElementOfType<TWidget>()));
    }

    private void TapTest2And3(FrameworkDartTester tester, Element switchElement)
    {
        tester.Tap(switchElement);
        tester.Pump();
        tester.Pump(AnimationDuration + AdditionalDelay);
        Assert.Equal(2, _onEndCalled);

        tester.Tap(switchElement);
        tester.Pump();
        tester.Pump(AnimationDuration + AdditionalDelay);
        Assert.Equal(3, _onEndCalled);
    }

    private static void RunTransitionTest<TTransition, TValue>(
        TestAnimatedWidgetState state,
        Func<TTransition, Animation<TValue>> animationOf,
        TValue[] values) where TTransition : Widget
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(new TestAnimatedWidget(state, SwitchKey)));
        // Captured once: the animation it holds must report the post-toggle values.
        var transition = (TTransition)tester.ElementsOfType<TTransition>().First().Widget;

        Assert.Equal(1, state.Builds);
        tester.Tap(tester.ElementsWithKey(SwitchKey).Single());
        Assert.Equal(1, state.Builds);
        tester.Pump();
        Assert.Equal(values[0], animationOf(transition).Value);
        Assert.Equal(2, state.Builds);
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(values[1], animationOf(transition).Value);
        Assert.Equal(2, state.Builds);
        tester.Pump(TimeSpan.FromMilliseconds(250));
        Assert.Equal(values[2], animationOf(transition).Value);
        Assert.Equal(2, state.Builds);
        tester.Pump(TimeSpan.FromMilliseconds(250));
        Assert.Equal(values[3], animationOf(transition).Value);
        Assert.Equal(2, state.Builds);
    }

    private static TestAnimatedWidgetState CreateState(string widget)
    {
        return widget switch
        {
            nameof(AnimatedContainer) => new TestAnimatedContainerWidgetState(),
            nameof(AnimatedPadding) => new TestAnimatedPaddingWidgetState(),
            nameof(AnimatedAlign) => new TestAnimatedAlignWidgetState(),
            nameof(AnimatedPositioned) => new TestAnimatedPositionedWidgetState(),
            nameof(AnimatedPositionedDirectional) => new TestAnimatedPositionedDirectionalWidgetState(),
            nameof(AnimatedSlide) => new TestAnimatedSlideWidgetState(),
            nameof(AnimatedScale) => new TestAnimatedScaleWidgetState(),
            nameof(AnimatedRotation) => new TestAnimatedRotationWidgetState(),
            nameof(AnimatedOpacity) => new TestAnimatedOpacityWidgetState(),
            nameof(AnimatedFractionallySizedBox) => new TestAnimatedFractionallySizedBoxWidgetState(),
            nameof(SliverAnimatedOpacity) => new TestSliverAnimatedOpacityWidgetState(),
            nameof(AnimatedDefaultTextStyle) => new TestDefaultTextStyleWidgetState(),
            nameof(AnimatedPhysicalModel) => new TestAnimatedPhysicalModelWidgetState(),
            _ => new TestTweenAnimationBuilderWidgetState(),
        };
    }

    private sealed class TestAnimatedWidget(State state, Key switchKey, Action? callback = null)
        : StatefulWidget
    {
        public Action? Callback { get; } = callback;

        public Key SwitchKey { get; } = switchKey;

        public override State CreateState() => state;
    }

    private abstract class TestAnimatedWidgetState : State<TestAnimatedWidget>
    {
        protected readonly Widget Child = new Placeholder();

        public int Builds { get; private set; }

        protected bool Toggle { get; private set; }

        protected static TimeSpan Duration => AnimationDuration;

        protected abstract Widget GetAnimatedWidget();

        protected Widget BuildToggle()
        {
            return new GestureDetector(
                key: Widget.SwitchKey,
                behavior: HitTestBehavior.Opaque,
                onTap: () => SetState(() => Toggle = !Toggle),
                child: new SizedBox(width: 48.0, height: 48.0));
        }

        protected void CountBuild() => Builds++;

        public override Widget Build(BuildContext context)
        {
            CountBuild();
            return new Stack(children: [GetAnimatedWidget(), BuildToggle()]);
        }
    }

    private sealed class TestAnimatedContainerWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedContainer(
                duration: Duration,
                onEnd: Widget.Callback,
                width: Toggle ? 10 : 20,
                foregroundDecoration: Toggle ? new BoxDecoration() : null,
                child: Child);
        }
    }

    private sealed class TestAnimatedPaddingWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedPadding(
                duration: Duration,
                onEnd: Widget.Callback,
                padding: Toggle ? EdgeInsets.All(8.0) : EdgeInsets.All(16.0),
                child: Child);
        }
    }

    private sealed class TestAnimatedAlignWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedAlign(
                duration: Duration,
                onEnd: Widget.Callback,
                alignment: Toggle ? Alignment.TopLeft : Alignment.BottomRight,
                child: Child);
        }
    }

    private sealed class TestAnimatedPositionedWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedPositioned(
                duration: Duration,
                onEnd: Widget.Callback,
                left: Toggle ? 10 : 20,
                child: Child);
        }
    }

    private sealed class TestAnimatedPositionedDirectionalWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedPositionedDirectional(
                duration: Duration,
                onEnd: Widget.Callback,
                start: Toggle ? 10 : 20,
                child: Child);
        }
    }

    private sealed class TestAnimatedSlideWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedSlide(
                duration: Duration,
                onEnd: Widget.Callback,
                offset: Toggle ? new Vector(1, 1) : new Vector(0, 0),
                child: Child);
        }
    }

    private sealed class TestAnimatedScaleWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedScale(
                duration: Duration,
                onEnd: Widget.Callback,
                scale: Toggle ? 2.0 : 1.0,
                child: Child);
        }
    }

    private sealed class TestAnimatedRotationWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedRotation(
                duration: Duration,
                onEnd: Widget.Callback,
                turns: Toggle ? 1.5 : 0.0,
                child: Child);
        }
    }

    private sealed class TestAnimatedOpacityWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedOpacity(
                duration: Duration,
                onEnd: Widget.Callback,
                opacity: Toggle ? 1.0 : 0.0,
                child: Child);
        }
    }

    private sealed class TestAnimatedFractionallySizedBoxWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedFractionallySizedBox(
                duration: Duration,
                onEnd: Widget.Callback,
                heightFactor: Toggle ? 0.25 : 0.75,
                widthFactor: Toggle ? 0.25 : 0.75,
                child: Child);
        }
    }

    private sealed class TestSliverAnimatedOpacityWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new SliverAnimatedOpacity(
                sliver: new SliverToBoxAdapter(child: Child),
                duration: Duration,
                onEnd: Widget.Callback,
                opacity: Toggle ? 1.0 : 0.0);
        }

        public override Widget Build(BuildContext context)
        {
            CountBuild();
            return new Directionality(
                textDirection: TextDirection.Ltr,
                child: new CustomScrollView(
                    slivers: [GetAnimatedWidget(), new SliverToBoxAdapter(child: BuildToggle())]));
        }
    }

    private sealed class TestDefaultTextStyleWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedDefaultTextStyle(
                duration: Duration,
                onEnd: Widget.Callback,
                style: Toggle
                    ? new TextStyle(FontStyle: FontStyle.Italic)
                    : new TextStyle(FontStyle: FontStyle.Normal),
                child: Child);
        }
    }

    private sealed class TestAnimatedPhysicalModelWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new AnimatedPhysicalModel(
                duration: Duration,
                onEnd: Widget.Callback,
                color: Toggle ? new Color(0xFFFF0000) : new Color(0xFF00FF00),
                shadowColor: new Color(0xFF0000FF),
                child: Child);
        }
    }

    private sealed class TestTweenAnimationBuilderWidgetState : TestAnimatedWidgetState
    {
        protected override Widget GetAnimatedWidget()
        {
            return new TweenAnimationBuilder<double>(
                tween: Toggle ? new DoubleTween(begin: 1, end: 2) : new DoubleTween(begin: 2, end: 1),
                duration: Duration,
                onEnd: Widget.Callback,
                child: Child,
                builder: (_, size, child) => new SizedBox(width: size, height: size, child: child));
        }
    }
}

/// <summary>flutter_test's geometry getters over an element's render box.</summary>
internal static class ImplicitAnimationsDartTestUtils
{
    public static RenderBox Box(Element element) => (RenderBox)element.FindRenderObject()!;

    /// <summary>Dart's <c>tester.getSize</c>.</summary>
    public static Size GetSize(Element element) => Box(element).Size;

    /// <summary>Dart's <c>tester.getTopLeft</c>.</summary>
    public static Point GetTopLeft(Element element) => Box(element).LocalToGlobal(new Point(0, 0));

    /// <summary>Dart's <c>tester.getTopRight</c>.</summary>
    public static Point GetTopRight(Element element)
    {
        RenderBox box = Box(element);
        return box.LocalToGlobal(new Point(box.Size.Width, 0));
    }

    /// <summary>Dart's <c>box.localToGlobal(box.size.center(Offset.zero))</c>.</summary>
    public static Point GetCenter(RenderBox box)
    {
        return box.LocalToGlobal(new Point(box.Size.Width / 2.0, box.Size.Height / 2.0));
    }
}
