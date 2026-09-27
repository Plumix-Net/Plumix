using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/test/animation/tween_test.dart
// flutter/packages/flutter/test/animation/animations_test.dart (TweenSequence and CurvedAnimation cases)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TweenDartParityTests : IDisposable
{
    private const string ApiDocsLink =
        "See \"Types with special considerations\" at https://api.flutter.dev/flutter/animation/Tween-class.html "
        + "for more information.";

    private readonly long _baseTicks;

    public TweenDartParityTests()
    {
        Scheduler.ResetForTests();
        _baseTicks = Scheduler.CurrentSystemFrameTimeStamp.Ticks;
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    // Flutter's `tick(Duration)` helper: an absolute frame timestamp.
    private void Tick(int milliseconds)
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromTicks(_baseTicks + TimeSpan.FromMilliseconds(milliseconds).Ticks));
    }

    // flutter_test's `hasOneLineDescription`.
    private static void AssertOneLineDescription(object value)
    {
        string description = value.ToString()!;
        Assert.NotEmpty(description);
        Assert.DoesNotContain("\n", description);
        Assert.DoesNotContain("Instance of ", description);
        Assert.Equal(description.Trim(), description);
    }

    private static List<string> Diagnostics(FlutterError error)
    {
        return error.Diagnostics.Select(node => node.ToString()).ToList();
    }

    [DebugOnlyFact]
    public void ThrowsFlutterErrorWhenTweeningTypesThatDoNotSatisfyTweenRequirements_Object()
    {
        var objectTween = new Tween<object>(new object(), new object());

        FlutterError error = Assert.Throws<FlutterError>(() => objectTween.Transform(0.1));
        Assert.Equal(
            [
                "Cannot lerp between \"Instance of 'Object'\" and \"Instance of 'Object'\".",
                $"The type Object might not fully implement `+`, `-`, and/or `*`. {ApiDocsLink}",
                "There may be a dedicated \"ObjectTween\" for this type, or you may need to create one.",
            ],
            Diagnostics(error));
    }

    [DebugOnlyFact]
    public void ThrowsFlutterErrorWhenTweeningTypesThatDoNotSatisfyTweenRequirements_Color()
    {
        var colorTween = new Tween<Color>(new Color(0xFF000000), new Color(0xFFFFFFFF));

        FlutterError error = Assert.Throws<FlutterError>(() => colorTween.Transform(0.1));
        Assert.Equal(
            [
                $"Cannot lerp between \"{new Color(0xff000000)}\" and \"{new Color(0xffffffff)}\".",
                $"The type Color might not fully implement `+`, `-`, and/or `*`. {ApiDocsLink}",
                "To lerp colors, consider ColorTween instead.",
            ],
            Diagnostics(error));
    }

    [DebugOnlyFact]
    public void ThrowsFlutterErrorWhenTweeningTypesThatDoNotSatisfyTweenRequirements_Rect()
    {
        var rectTween = new Tween<Rect>(new Rect(0, 0, 10, 10), new Rect(2, 2, 2, 2));

        FlutterError error = Assert.Throws<FlutterError>(() => rectTween.Transform(0.1));
        Assert.Equal(
            [
                "Cannot lerp between \"Rect.fromLTRB(0.0, 0.0, 10.0, 10.0)\" and "
                + "\"Rect.fromLTRB(2.0, 2.0, 4.0, 4.0)\".",
                $"The type Rect might not fully implement `+`, `-`, and/or `*`. {ApiDocsLink}",
                "To lerp rects, consider RectTween instead.",
            ],
            Diagnostics(error));
    }

    [DebugOnlyFact]
    public void ThrowsFlutterErrorWhenTweeningTypesThatDoNotSatisfyTweenRequirements_Int()
    {
        var intTween = new Tween<int>(0, 1);

        FlutterError error = Assert.Throws<FlutterError>(() => intTween.Transform(0.1));
        Assert.Equal(
            [
                "Cannot lerp between \"0\" and \"1\".",
                $"The type int returned a double after multiplication with a double value. {ApiDocsLink}",
                "To lerp int values, consider IntTween or StepTween instead.",
            ],
            Diagnostics(error));
    }

    [Fact]
    public void CanChainTweens()
    {
        var tween = new Tween<double>(0.30, 0.50);
        AssertOneLineDescription(tween);
        Animatable<double> chain = tween.Chain(new Tween<double>(0.50, 1.0));
        using var controller = new AnimationController();
        Assert.Equal(0.40, chain.Evaluate(controller));
        AssertOneLineDescription(chain);
    }

    [Fact]
    public void CanAnimateTweens()
    {
        var tween = new Tween<double>(0.30, 0.50);
        using var controller = new AnimationController();
        Animation<double> animation = tween.Animate(controller);
        controller.SetValue(0.50);
        Assert.Equal(0.40, animation.Value);
        AssertOneLineDescription(animation);
    }

    [Fact]
    public void CanDriveTweens()
    {
        var tween = new Tween<double>(0.30, 0.50);
        using var controller = new AnimationController();
        Animation<double> animation = controller.Drive(tween);
        controller.SetValue(0.50);
        Assert.Equal(0.40, animation.Value);
        AssertOneLineDescription(animation);
    }

    [Fact]
    public void SizeTween()
    {
        var tween = new SizeTween(begin: new Size(0, 0), end: new Size(20.0, 30.0));
        Assert.Equal(new Size(10.0, 15.0), tween.Lerp(0.5));
        AssertOneLineDescription(tween);
    }

    [Fact]
    public void IntTween()
    {
        var tween = new IntTween(5, 9);
        Assert.Equal(7, tween.Lerp(0.5));
        Assert.Equal(8, tween.Lerp(0.7));
    }

    [Fact]
    public void RectTween()
    {
        var a = new Rect(5.0, 3.0, 7.0, 11.0);
        var b = new Rect(8.0, 12.0, 14.0, 18.0);
        var tween = new RectTween(begin: a, end: b);
        Assert.Equal(new Rect(6.5, 7.5, 10.5, 14.5), tween.Lerp(0.5));
        AssertOneLineDescription(tween);
    }

    [Fact]
    public void Matrix4Tween()
    {
        Matrix4 a = Matrix4.Identity();
        Matrix4 b = Matrix4.Copy(a);
        b.TranslateByDouble(6.0, -8.0, 0.0, 1);
        b.ScaleByDouble(0.5, 1.0, 5.0, 1);
        var tween = new Matrix4Tween(begin: a, end: b);
        Assert.Equal(a, tween.Lerp(0.0));
        Assert.Equal(b, tween.Lerp(1.0));
    }

    [Fact]
    public void ConstantTween()
    {
        var tween = new ConstantTween<double>(100.0);
        Assert.Equal(100.0, tween.Begin);
        Assert.Equal(100.0, tween.End);
        Assert.Equal(100.0, tween.Lerp(0.0));
        Assert.Equal(100.0, tween.Lerp(0.5));
        Assert.Equal(100.0, tween.Lerp(1.0));
        Assert.EndsWith("(value: 100)", tween.ToString());
    }

    [Fact]
    public void ReverseTween()
    {
        var tween = new ReverseTween<int>(new IntTween(5, 9));
        Assert.Equal(7, tween.Lerp(0.5));
        Assert.Equal(6, tween.Lerp(0.7));
        Assert.Equal(9, tween.Begin);
        Assert.Equal(5, tween.End);
    }

    [Fact]
    public void ColorTween()
    {
        var tween = new ColorTween(begin: new Color(0xff000000), end: new Color(0xffffffff));
        Assert.Equal(new Color(0xff000000), tween.Lerp(0.0));
        AssertSameColorAs(new Color(0xff7f7f7f), tween.Lerp(0.5)!);
        AssertSameColorAs(new Color(0xffb2b2b2), tween.Lerp(0.7)!);
        Assert.Equal(new Color(0xffffffff), tween.Lerp(1.0));
    }

    // flutter_test's `isSameColorAs`: every channel within 0.004 of the expected one.
    private static void AssertSameColorAs(Color expected, Color actual)
    {
        const double threshold = 0.004;
        Assert.InRange(actual.A, expected.A - threshold, expected.A + threshold);
        Assert.InRange(actual.R, expected.R - threshold, expected.R + threshold);
        Assert.InRange(actual.G, expected.G - threshold, expected.G + threshold);
        Assert.InRange(actual.B, expected.B - threshold, expected.B + threshold);
    }

    [Fact]
    public void StepTween()
    {
        var tween = new StepTween(5, 9);
        Assert.Equal(7, tween.Lerp(0.5));
        Assert.Equal(7, tween.Lerp(0.7));
    }

    [Fact]
    public void CurveTween()
    {
        var tween = new CurveTween(Curves.EaseIn);
        Assert.Equal(0.0, tween.Transform(0.0));
        Assert.Equal(0.31640625, tween.Transform(0.5));
        Assert.Equal(1.0, tween.Transform(1.0));
    }

    [Fact]
    public void Transform_ReturnsTheEndsExactlyAndExtrapolatesWithoutClamping()
    {
        var tween = new Tween<double>(10.0, 20.0);
        Assert.Equal(10.0, tween.Transform(0.0));
        Assert.Equal(20.0, tween.Transform(1.0));
        Assert.Equal(25.0, tween.Transform(1.5));
        Assert.Equal(5.0, tween.Transform(-0.5));

        var offsets = new Tween<Point>(new Point(0, 10), new Point(10, 30));
        Assert.Equal(new Point(2.5, 15), offsets.Transform(0.25));
    }

    [Fact]
    public void ColorTween_LerpsFromANullEndLikeColorLerp()
    {
        var tween = new ColorTween(begin: new Color(0xFF000000));
        Assert.Null(tween.End);
        Assert.Equal(new Color(0xFF000000), tween.Transform(0.0));
        Assert.Null(tween.Transform(1.0));
        Assert.Equal(0.5, tween.Transform(0.5)!.A, 6);

        Assert.Null(new ColorTween().Lerp(0.5));
    }

    [Fact]
    public void SizeAndRectTweens_ScaleFromANullEnd()
    {
        Assert.Equal(new Size(5, 10), new SizeTween(end: new Size(10, 20)).Lerp(0.5));
        Assert.Equal(new Size(5, 10), new SizeTween(begin: new Size(10, 20)).Lerp(0.5));
        Assert.Null(new SizeTween().Lerp(0.5));
        Assert.Equal(new Rect(1, 2, 3, 4), new RectTween(end: new Rect(2, 4, 6, 8)).Lerp(0.5));
        Assert.Null(new RectTween().Transform(0.0));
    }

    [Fact]
    public void Transform_OfAnUnsetValueTypeEndFailsDartsCast()
    {
        var tween = new Tween<double> { End = 1.0 };
        Assert.Throws<InvalidCastException>(() => tween.Transform(0.0));
    }

    [Fact]
    public void AnimatableFromCallback()
    {
        Animatable<string> animatable = Animatable<string>.FromCallback(
            t => "t=" + t.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("t=0.25", animatable.Transform(0.25));
    }

    [Fact]
    public void ToString_MatchesDartFormats()
    {
        Assert.Equal("CurveTween(curve: Cubic(0.42, 0.00, 1.00, 1.00))", new CurveTween(Curves.EaseIn).ToString());
        Assert.Equal("TweenSequence(1 items)", new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(0.0, 1.0), 1.0),
        ]).ToString());
        Assert.Contains(
            "(Offset(0.0, 1.0) → Offset(2.0, 3.0))",
            new Tween<Point>(new Point(0, 1), new Point(2, 3)).ToString());
    }

    [Fact]
    public void TweenSequence()
    {
        using var controller = new AnimationController();
        Animation<double> animation = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(5.0, 10.0), 4.0),
            new TweenSequenceItem<double>(new ConstantTween<double>(10.0), 2.0),
            new TweenSequenceItem<double>(new Tween<double>(10.0, 5.0), 4.0),
        ]).Animate(controller);

        Assert.Equal(5.0, animation.Value);
        controller.SetValue(0.2);
        Assert.Equal(7.5, animation.Value);
        controller.SetValue(0.4);
        Assert.Equal(10.0, animation.Value);
        controller.SetValue(0.6);
        Assert.Equal(10.0, animation.Value);
        controller.SetValue(0.8);
        Assert.Equal(7.5, animation.Value);
        controller.SetValue(1.0);
        Assert.Equal(5.0, animation.Value);
    }

    [Fact]
    public void TweenSequenceWithCurves()
    {
        using var controller = new AnimationController();
        Animation<double> animation = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(
                new Tween<double>(5.0, 10.0).Chain(new CurveTween(new Interval(0.5, 1.0))),
                4.0),
            new TweenSequenceItem<double>(new ConstantTween<double>(10.0).Chain(new CurveTween(Curves.Linear)), 2.0),
            new TweenSequenceItem<double>(
                new Tween<double>(10.0, 5.0).Chain(new CurveTween(new Interval(0.0, 0.5))),
                4.0),
        ]).Animate(controller);

        Assert.Equal(5.0, animation.Value);
        controller.SetValue(0.2);
        Assert.Equal(5.0, animation.Value);
        controller.SetValue(0.4);
        Assert.Equal(10.0, animation.Value);
        controller.SetValue(0.6);
        Assert.Equal(10.0, animation.Value);
        controller.SetValue(0.8);
        Assert.Equal(5.0, animation.Value);
        controller.SetValue(1.0);
        Assert.Equal(5.0, animation.Value);
    }

    [Fact]
    public void TweenSequenceOneTween()
    {
        using var controller = new AnimationController();
        Animation<double> animation = new TweenSequence<double>(
        [
            new TweenSequenceItem<double>(new Tween<double>(5.0, 10.0), 1.0),
        ]).Animate(controller);

        Assert.Equal(5.0, animation.Value);
        controller.SetValue(0.5);
        Assert.Equal(7.5, animation.Value);
        controller.SetValue(1.0);
        Assert.Equal(10.0, animation.Value);
    }

    [Fact]
    public void FlippedTweenSequence_EvaluatesTheSequenceMirrored()
    {
        var sequence = new FlippedTweenSequence(
        [
            new TweenSequenceItem<double>(new Tween<double>(0.0, 0.2), 1.0),
            new TweenSequenceItem<double>(new Tween<double>(0.2, 1.0), 1.0),
        ]);
        Assert.Equal(0.0, sequence.Transform(0.0));
        Assert.Equal(1.0, sequence.Transform(1.0));
        Assert.Equal(1.0 - 0.6, sequence.Transform(0.25), 10);
    }

    private sealed class BogusCurve : Curve
    {
        public override double Transform(double t) => 100.0;
    }

    [DebugOnlyFact]
    public void CurvedAnimationWithBogusCurve()
    {
        using var controller = new AnimationController();
        var curved = new CurvedAnimation(controller, new BogusCurve());

        FlutterError error = Assert.Throws<FlutterError>(() => curved.Value);
        Assert.Matches(
            @"^Invalid curve endpoint at \d+(\.\d*)?\.\nCurves must map 0\.0 to near zero and 1\.0 to near one but "
            + @"BogusCurve mapped \d+(\.\d*)? to \d+(\.\d*)?, which is near \d+(\.\d*)?\.$",
            error.Message);
    }

    [Fact]
    public void CurvedAnimationRunningWithDifferentForwardAndReverseDurations()
    {
        using var controller = new AnimationController(
            duration: TimeSpan.FromMilliseconds(100),
            reverseDuration: TimeSpan.FromMilliseconds(50));
        var curved = new CurvedAnimation(controller, Curves.Linear, reverseCurve: Curves.Linear);

        controller.Forward();
        Tick(0);
        double[] forward = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0];
        for (int i = 0; i < forward.Length; i++)
        {
            Tick((i + 1) * 10);
            Assert.Equal(forward[i], curved.Value, 6);
        }

        controller.Reverse();
        double[] reverse = [1.0, 0.8, 0.6, 0.4, 0.2, 0.0];
        for (int i = 0; i < reverse.Length; i++)
        {
            Tick(110 + (i * 10));
            Assert.Equal(reverse[i], curved.Value, 6);
        }
    }

    [Fact]
    public void CurvedAnimationStopsListeningToParentWhenDisposed()
    {
        var forwardCurve = new Interval(0.0, 0.5);
        var reverseCurve = new Interval(0.5, 1.0);

        using var controller = new AnimationController(
            duration: TimeSpan.FromMilliseconds(100),
            reverseDuration: TimeSpan.FromMilliseconds(100));
        var curved = new CurvedAnimation(controller, forwardCurve, reverseCurve: reverseCurve);

        Assert.Equal(1.0, forwardCurve.Transform(0.5));
        Assert.Equal(0.0, reverseCurve.Transform(0.5));

        controller.Forward(from: 0.5);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        Assert.Equal(1.0, curved.Value);

        controller.SetValue(1.0);
        Assert.Equal(AnimationStatus.Completed, controller.Status);

        controller.Reverse(from: 0.5);
        Assert.Equal(AnimationStatus.Reverse, controller.Status);
        Assert.Equal(0.0, curved.Value);

        Assert.False(curved.IsDisposed);
        curved.Dispose();
        Assert.True(curved.IsDisposed);

        controller.SetValue(0.0);
        Assert.Equal(AnimationStatus.Dismissed, controller.Status);

        controller.Forward(from: 0.5);
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        Assert.Equal(0.0, curved.Value);
    }

    [Fact]
    public void CurvedAnimation_KeepsTheDirectionItStartedInUntilTheAnimationSettles()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromMilliseconds(100));
        var curved = new CurvedAnimation(controller, Curves.Linear, reverseCurve: new Threshold(0.5));

        controller.Reverse(from: 0.4);
        Assert.Equal(0.0, curved.Value);

        // Turning around mid-flight keeps the reverse curve, as Dart's `_curveDirection ?? status` does.
        controller.Forward();
        Assert.Equal(AnimationStatus.Forward, controller.Status);
        Assert.Equal(0.0, curved.Value);
        Assert.Contains("ₒₙ", curved.ToString());
    }

    [Fact]
    public void Easing_MatchesTheMaterialMotionTokens()
    {
        Assert.Equal("Cubic(0.30, 0.00, 0.80, 0.15)", Plumix.Material.Easing.EmphasizedAccelerate.ToString());
        Assert.Equal("Cubic(0.00, 0.00, 0.20, 1.00)", Plumix.Material.Easing.LegacyDecelerate.ToString());
        Assert.Equal("Cubic(0.40, 0.00, 0.20, 1.00)", Plumix.Material.Easing.Legacy.ToString());
        Assert.Equal(TimeSpan.FromMilliseconds(300), Plumix.Material.Durations.Medium2);
    }
}
