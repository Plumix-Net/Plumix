using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ImplicitAnimationsTests : IDisposable
{
    public ImplicitAnimationsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AnimatedOpacity_ValidatesArgumentsAndExposesFlutterDefaults()
    {
        var opacity = new AnimatedOpacity(
            opacity: 0.4,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(0.4, opacity.Opacity);
        Assert.Equal(TimeSpan.FromMilliseconds(200), opacity.Duration);
        Assert.Null(opacity.Child);
        Assert.Equal(Curves.Linear(0.3), opacity.Curve(0.3));
        Assert.Null(opacity.OnEnd);
        Assert.False(opacity.AlwaysIncludeSemantics);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(
            opacity: -0.1,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(
            opacity: 1.1,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(
            opacity: double.NaN,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedOpacity(
            opacity: 0.5,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedSlide_ExposesFlutterDefaultsAndValidatesDuration()
    {
        var slide = new AnimatedSlide(
            offset: new Vector(0.25, -0.5),
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(new Vector(0.25, -0.5), slide.Offset);
        Assert.Equal(TimeSpan.FromMilliseconds(200), slide.Duration);
        Assert.Null(slide.Child);
        Assert.Equal(Curves.Linear(0.3), slide.Curve(0.3));
        Assert.Null(slide.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedSlide(
            offset: default,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedSize_ExposesFlutterDefaultsAndValidatesDurations()
    {
        var animatedSize = new AnimatedSize(duration: TimeSpan.FromMilliseconds(140));

        Assert.Equal(TimeSpan.FromMilliseconds(140), animatedSize.Duration);
        Assert.Null(animatedSize.Child);
        Assert.Equal(Alignment.Center, animatedSize.Alignment);
        Assert.Equal(Curves.Linear(0.3), animatedSize.Curve(0.3));
        Assert.Null(animatedSize.ReverseDuration);
        Assert.Equal(Plumix.UI.Clip.HardEdge, animatedSize.ClipBehavior);
        Assert.Null(animatedSize.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedSize(TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedSize(
            duration: TimeSpan.Zero,
            reverseDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void RenderAnimatedSize_AnimatesStableChildSizeAndUsesReverseDuration()
    {
        using var controller = new AnimationController(TimeSpan.FromMilliseconds(200));
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(10, 10)));
        var animatedSize = new RenderAnimatedSize(
            controller: controller,
            duration: TimeSpan.FromMilliseconds(200),
            reverseDuration: TimeSpan.FromMilliseconds(80),
            alignment: Alignment.Center,
            clipBehavior: Plumix.UI.Clip.HardEdge)
        {
            Child = child,
        };
        var constraints = BoxConstraints.Loose(new Size(100, 100));

        animatedSize.Layout(constraints);
        Assert.Equal(new Size(10, 10), animatedSize.Size);
        Assert.Equal(RenderAnimatedSizeState.Stable, animatedSize.State);

        child.AdditionalConstraints = BoxConstraints.Tight(new Size(30, 30));
        animatedSize.Layout(constraints);
        Assert.Equal(new Size(10, 10), animatedSize.Size);
        Assert.Equal(RenderAnimatedSizeState.Changed, animatedSize.State);

        controller.SetValue(0.5);
        animatedSize.MarkNeedsLayout();
        animatedSize.Layout(constraints);
        Assert.InRange(animatedSize.Size.Width, 10.1, 29.9);

        controller.SetValue(1.0);
        animatedSize.MarkNeedsLayout();
        animatedSize.Layout(constraints);
        Assert.Equal(new Size(30, 30), animatedSize.Size);

        child.AdditionalConstraints = BoxConstraints.Tight(new Size(12, 12));
        animatedSize.Layout(constraints);
        Assert.Equal(TimeSpan.FromMilliseconds(80), controller.Duration);
    }

    [Fact]
    public void AnimatedOpacity_InterpolatesFromCurrentValueAndUpdatesSemanticsPolicyImmediately()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedOpacity(
            opacity: 1.0,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedOpacity(
            opacity: 0.0,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderOpacity>(root.ChildElement);
        Assert.InRange(halfway.Opacity, 0.01, 0.99);
        Assert.False(halfway.AlwaysIncludeSemantics);
        double halfwayOpacity = halfway.Opacity;

        root.Update(new AnimatedOpacity(
            opacity: 0.8,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            alwaysIncludeSemantics: true,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        var interrupted = RequireRenderObject<RenderOpacity>(root.ChildElement);
        Assert.Equal(halfwayOpacity, interrupted.Opacity, precision: 6);
        Assert.True(interrupted.AlwaysIncludeSemantics);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Assert.Equal(0.8, RequireRenderObject<RenderOpacity>(root.ChildElement).Opacity, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedSlide_InterpolatesFractionalOffsetAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedSlide(
            offset: default,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedSlide(
            offset: new Vector(1.0, -0.5),
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderFractionalTranslation>(root.ChildElement);
        Assert.InRange(halfway.Translation.X, 0.01, 0.99);
        Assert.InRange(halfway.Translation.Y, -0.49, -0.01);
        Assert.Equal(0, completed);
        Vector halfwayTranslation = halfway.Translation;

        root.Update(new AnimatedSlide(
            offset: new Vector(-0.5, 0.75),
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        var interrupted = RequireRenderObject<RenderFractionalTranslation>(root.ChildElement);
        Assert.Equal(halfwayTranslation.X, interrupted.Translation.X, precision: 6);
        Assert.Equal(halfwayTranslation.Y, interrupted.Translation.Y, precision: 6);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Vector finished = RequireRenderObject<RenderFractionalTranslation>(root.ChildElement).Translation;
        Assert.Equal(-0.5, finished.X, precision: 6);
        Assert.Equal(0.75, finished.Y, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedScale_ExposesFlutterDefaultsAndValidatesDuration()
    {
        var scale = new AnimatedScale(
            scale: 1.5,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(1.5, scale.Scale);
        Assert.Equal(TimeSpan.FromMilliseconds(200), scale.Duration);
        Assert.Null(scale.Child);
        Assert.Equal(Alignment.Center, scale.Alignment);
        Assert.Null(scale.FilterQuality);
        Assert.Equal(Curves.Linear(0.3), scale.Curve(0.3));
        Assert.Null(scale.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedScale(
            scale: 1,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedRotation_ExposesFlutterDefaultsAndValidatesDuration()
    {
        var rotation = new AnimatedRotation(
            turns: 0.25,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(0.25, rotation.Turns);
        Assert.Equal(TimeSpan.FromMilliseconds(200), rotation.Duration);
        Assert.Null(rotation.Child);
        Assert.Equal(Alignment.Center, rotation.Alignment);
        Assert.Null(rotation.FilterQuality);
        Assert.Equal(Curves.Linear(0.3), rotation.Curve(0.3));
        Assert.Null(rotation.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedRotation(
            turns: 0,
            duration: TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedRotation(
            turns: double.NaN,
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedRotation(
            turns: double.PositiveInfinity,
            duration: TimeSpan.Zero));
    }

    [Fact]
    public void AnimatedScale_InterpolatesFromCurrentValueAndUpdatesTransformOptionsImmediately()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedScale(
            scale: 1,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedScale(
            scale: 2,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            alignment: Alignment.TopLeft,
            filterQuality: FilterQuality.Low,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.InRange(halfway.Transform.M11, 1.01, 1.99);
        Assert.Equal(halfway.Transform.M11, halfway.Transform.M22, precision: 6);
        Assert.Equal(Alignment.TopLeft, halfway.Alignment);
        Assert.Equal(FilterQuality.Low, halfway.FilterQuality);
        double halfwayScale = halfway.Transform.M11;

        root.Update(new AnimatedScale(
            scale: 0.5,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            alignment: Alignment.BottomRight,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        var interrupted = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.Equal(halfwayScale, interrupted.Transform.M11, precision: 6);
        Assert.Equal(Alignment.BottomRight, interrupted.Alignment);
        Assert.Equal(FilterQuality.High, interrupted.FilterQuality);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        var finished = RequireRenderObject<RenderTransform>(root.ChildElement);
        Assert.Equal(0.5, finished.Transform.M11, precision: 6);
        Assert.Equal(0.5, finished.Transform.M22, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedRotation_UsesTurnsAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedRotation(
            turns: 0,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedRotation(
            turns: 0.25,
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        Matrix halfway = RequireRenderObject<RenderTransform>(root.ChildElement).Transform;
        Assert.InRange(halfway.M11, 0.01, 0.99);
        Assert.InRange(halfway.M12, 0.01, 0.99);
        Assert.Equal(-halfway.M12, halfway.M21, precision: 6);
        Assert.Equal(0, completed);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Matrix finished = RequireRenderObject<RenderTransform>(root.ChildElement).Transform;
        Assert.Equal(0, finished.M11, precision: 6);
        Assert.Equal(1, finished.M12, precision: 6);
        Assert.Equal(-1, finished.M21, precision: 6);
        Assert.Equal(0, finished.M22, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedScaleAndRotation_AllowZeroAreaLayout()
    {
        var child = new RenderConstrainedBox(BoxConstraints.TightFor(width: 0, height: 0));
        var scale = new RenderTransform(
            Matrix.CreateScale(2, 2),
            Alignment.Center,
            child);

        scale.Layout(BoxConstraints.TightFor(width: 0, height: 0));
        Assert.Equal(default, scale.Size);

        var rotation = new RenderTransform(
            new Matrix(0, 1, -1, 0, 0, 0),
            Alignment.Center,
            scale);
        rotation.Layout(BoxConstraints.TightFor(width: 0, height: 0));
        Assert.Equal(default, rotation.Size);
    }

    [Fact]
    public void AnimatedPadding_ValidatesArgumentsAndExposesFlutterDefaults()
    {
        var padding = new AnimatedPadding(
            padding: new Thickness(8),
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(new Thickness(8), padding.Padding);
        Assert.Equal(TimeSpan.FromMilliseconds(200), padding.Duration);
        Assert.Null(padding.Child);
        Assert.Equal(Curves.Linear(0.3), padding.Curve(0.3));
        Assert.Null(padding.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedPadding(
            padding: new Thickness(-1),
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedPadding(
            padding: new Thickness(double.NaN),
            duration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedPadding(
            padding: new Thickness(),
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedAlign_ValidatesArgumentsAndExposesFlutterDefaults()
    {
        var align = new AnimatedAlign(
            alignment: Alignment.BottomRight,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(Alignment.BottomRight, align.Alignment);
        Assert.Equal(TimeSpan.FromMilliseconds(200), align.Duration);
        Assert.Null(align.Child);
        Assert.Null(align.WidthFactor);
        Assert.Null(align.HeightFactor);
        Assert.Equal(Curves.Linear(0.3), align.Curve(0.3));
        Assert.Null(align.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.Zero,
            widthFactor: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.Zero,
            heightFactor: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.Zero,
            widthFactor: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedPadding_InterpolatesFromCurrentValueAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedPadding(
            padding: new Thickness(0),
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        Assert.Equal(new Thickness(0), RequireRenderObject<RenderPadding>(root.ChildElement).Padding);

        root.Update(new AnimatedPadding(
            padding: new Thickness(20, 10, 40, 30),
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        Thickness halfway = RequireRenderObject<RenderPadding>(root.ChildElement).Padding;
        Assert.InRange(halfway.Left, 0.1, 19.9);
        Assert.InRange(halfway.Top, 0.1, 9.9);
        Assert.InRange(halfway.Right, 0.1, 39.9);
        Assert.InRange(halfway.Bottom, 0.1, 29.9);
        Assert.Equal(0, completed);

        root.Update(new AnimatedPadding(
            padding: new Thickness(30),
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        Assert.Equal(halfway, RequireRenderObject<RenderPadding>(root.ChildElement).Padding);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1));
        owner.FlushBuild();
        Assert.Equal(new Thickness(30), RequireRenderObject<RenderPadding>(root.ChildElement).Padding);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedAlign_InterpolatesAlignmentAndFactorsAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedAlign(
            alignment: Alignment.TopLeft,
            duration: TimeSpan.FromMilliseconds(200),
            widthFactor: 1,
            heightFactor: 2,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedAlign(
            alignment: Alignment.BottomRight,
            duration: TimeSpan.FromMilliseconds(200),
            widthFactor: 3,
            heightFactor: 4,
            curve: Curves.Linear,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.InRange(halfway.Alignment.X, -0.99, 0.99);
        Assert.InRange(halfway.Alignment.Y, -0.99, 0.99);
        Assert.InRange(halfway.WidthFactor!.Value, 1.01, 2.99);
        Assert.InRange(halfway.HeightFactor!.Value, 2.01, 3.99);
        Assert.Equal(0, completed);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1));
        owner.FlushBuild();
        var finished = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Equal(Alignment.BottomRight, finished.Alignment);
        Assert.Equal(3, finished.WidthFactor);
        Assert.Equal(4, finished.HeightFactor);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedAlign_NullableFactorsSwitchImmediatelyWithoutStartingAnimation()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.FromMilliseconds(200),
            widthFactor: 2,
            heightFactor: 3,
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        var withFactors = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Equal(2, withFactors.WidthFactor);
        Assert.Equal(3, withFactors.HeightFactor);
        Assert.Equal(0, completed);

        root.Update(new AnimatedAlign(
            alignment: Alignment.Center,
            duration: TimeSpan.FromMilliseconds(200),
            child: new SizedBox(width: 10, height: 10),
            onEnd: () => completed++));
        owner.FlushBuild();
        var withoutFactors = RequireRenderObject<RenderAlign>(root.ChildElement);
        Assert.Null(withoutFactors.WidthFactor);
        Assert.Null(withoutFactors.HeightFactor);
        Assert.Equal(0, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedPositioned_ExposesFlutterDefaultsFactoriesAndGuards()
    {
        var child = new SizedBox(width: 10, height: 10);
        var positioned = new AnimatedPositioned(
            child: child,
            duration: TimeSpan.FromMilliseconds(200),
            left: 4,
            top: 6);

        Assert.Same(child, positioned.Child);
        Assert.Equal(4, positioned.Left);
        Assert.Equal(6, positioned.Top);
        Assert.Null(positioned.Right);
        Assert.Null(positioned.Bottom);
        Assert.Null(positioned.Width);
        Assert.Null(positioned.Height);
        Assert.Equal(Curves.Linear(0.3), positioned.Curve(0.3));
        Assert.Null(positioned.OnEnd);

        var fromRect = AnimatedPositioned.FromRect(
            rect: new Rect(3, 5, 40, 20),
            child: child,
            duration: TimeSpan.FromMilliseconds(200));
        Assert.Equal(3, fromRect.Left);
        Assert.Equal(5, fromRect.Top);
        Assert.Equal(40, fromRect.Width);
        Assert.Equal(20, fromRect.Height);
        Assert.Null(fromRect.Right);
        Assert.Null(fromRect.Bottom);

        Assert.Throws<ArgumentException>(() => new AnimatedPositioned(
            child: child,
            duration: TimeSpan.Zero,
            left: 0,
            right: 0,
            width: 10));
        Assert.Throws<ArgumentException>(() => new AnimatedPositioned(
            child: child,
            duration: TimeSpan.Zero,
            top: 0,
            bottom: 0,
            height: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedPositioned(
            child: child,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedPositionedDirectional_ExposesFlutterDefaultsAndGuards()
    {
        var child = new SizedBox(width: 10, height: 10);
        var positioned = new AnimatedPositionedDirectional(
            child: child,
            duration: TimeSpan.FromMilliseconds(200),
            start: 4,
            top: 6);

        Assert.Same(child, positioned.Child);
        Assert.Equal(4, positioned.Start);
        Assert.Equal(6, positioned.Top);
        Assert.Null(positioned.End);
        Assert.Null(positioned.Bottom);
        Assert.Null(positioned.Width);
        Assert.Null(positioned.Height);
        Assert.Equal(Curves.Linear(0.3), positioned.Curve(0.3));
        Assert.Null(positioned.OnEnd);

        Assert.Throws<ArgumentException>(() => new AnimatedPositionedDirectional(
            child: child,
            duration: TimeSpan.Zero,
            start: 0,
            end: 0,
            width: 10));
        Assert.Throws<ArgumentException>(() => new AnimatedPositionedDirectional(
            child: child,
            duration: TimeSpan.Zero,
            top: 0,
            bottom: 0,
            height: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedPositionedDirectional(
            child: child,
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedPositioned_InterpolatesLayoutAndContinuesFromInterruptedValue()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildAnimatedPositioned(
            left: 0,
            top: 0,
            width: 20,
            height: 20,
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(BuildAnimatedPositioned(
            left: 80,
            top: 40,
            width: 40,
            height: 30,
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        StackParentData halfway = GetOnlyStackParentData(root);
        Assert.InRange(halfway.Left!.Value, 0.1, 79.9);
        Assert.InRange(halfway.Top!.Value, 0.1, 39.9);
        Assert.InRange(halfway.Width!.Value, 20.1, 39.9);
        Assert.InRange(halfway.Height!.Value, 20.1, 29.9);
        Assert.Equal(0, completed);
        double halfwayLeft = halfway.Left.Value;
        double halfwayTop = halfway.Top.Value;

        root.Update(BuildAnimatedPositioned(
            left: 20,
            top: 10,
            width: 10,
            height: 12,
            onEnd: () => completed++));
        owner.FlushBuild();
        StackParentData interrupted = GetOnlyStackParentData(root);
        Assert.Equal(halfwayLeft, interrupted.Left!.Value, precision: 6);
        Assert.Equal(halfwayTop, interrupted.Top!.Value, precision: 6);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        StackParentData finished = GetOnlyStackParentData(root);
        Assert.Equal(20, finished.Left);
        Assert.Equal(10, finished.Top);
        Assert.Equal(10, finished.Width);
        Assert.Equal(12, finished.Height);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedPositioned_NullTargetsSwitchImmediatelyWithoutStartingAnimation()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildAnimatedPositioned(
            left: 12,
            top: 8,
            width: null,
            height: null,
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(BuildAnimatedPositioned(
            left: null,
            top: null,
            width: 24,
            height: 18,
            onEnd: () => completed++));
        owner.FlushBuild();

        StackParentData data = GetOnlyStackParentData(root);
        Assert.Null(data.Left);
        Assert.Null(data.Top);
        Assert.Equal(24, data.Width);
        Assert.Equal(18, data.Height);
        Assert.Equal(0, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedPositionedDirectional_AnimatesLogicalInsetsAndResolvesAmbientDirectionImmediately()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildDirectionalPositioned(
            direction: Plumix.UI.TextDirection.Rtl,
            start: 0,
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(BuildDirectionalPositioned(
            direction: Plumix.UI.TextDirection.Rtl,
            start: 80,
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        StackParentData rtlHalfway = GetOnlyStackParentData(root);
        Assert.Null(rtlHalfway.Left);
        Assert.InRange(rtlHalfway.Right!.Value, 0.1, 79.9);
        double halfwayLogicalStart = rtlHalfway.Right.Value;

        root.Update(BuildDirectionalPositioned(
            direction: Plumix.UI.TextDirection.Ltr,
            start: 80,
            onEnd: () => completed++));
        owner.FlushBuild();
        StackParentData ltrHalfway = GetOnlyStackParentData(root);
        Assert.Equal(halfwayLogicalStart, ltrHalfway.Left!.Value, precision: 6);
        Assert.Null(ltrHalfway.Right);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        StackParentData finished = GetOnlyStackParentData(root);
        Assert.Equal(80, finished.Left);
        Assert.Null(finished.Right);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedDefaultTextStyle_ExposesFlutterDefaultsAndValidatesArguments()
    {
        var child = new Text("label");
        var style = new TextStyle(FontSize: 14, Color: Colors.Black);
        var animated = new AnimatedDefaultTextStyle(
            child: child,
            style: style,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Same(child, animated.Child);
        Assert.Same(style, animated.Style);
        Assert.Null(animated.TextAlign);
        Assert.True(animated.SoftWrap);
        Assert.Equal(TextOverflow.Clip, animated.Overflow);
        Assert.Null(animated.MaxLines);
        Assert.Equal(TextWidthBasis.Parent, animated.TextWidthBasis);
        Assert.Null(animated.TextHeightBehavior);
        Assert.Equal(Curves.Linear(0.3), animated.Curve(0.3));
        Assert.Null(animated.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedDefaultTextStyle(
            child,
            style,
            TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedDefaultTextStyle(
            child,
            style,
            TimeSpan.Zero,
            maxLines: 0));
    }

    [Fact]
    public void AnimatedDefaultTextStyle_InterpolatesStyleAndAppliesOtherTextPropertiesImmediately()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedDefaultTextStyle(
            child: new Text("animated text"),
            style: new TextStyle(FontSize: 10, Color: Colors.Red, LetterSpacing: 0),
            duration: TimeSpan.FromMilliseconds(200),
            onEnd: () => completed++));
        Mount(root, owner);

        root.Update(new AnimatedDefaultTextStyle(
            child: new Text("animated text"),
            style: new TextStyle(FontSize: 30, Color: Colors.Blue, LetterSpacing: 4),
            duration: TimeSpan.FromMilliseconds(200),
            textAlign: TextAlign.End,
            softWrap: false,
            overflow: TextOverflow.Ellipsis,
            maxLines: 1,
            textWidthBasis: TextWidthBasis.LongestLine,
            textHeightBehavior: new TextHeightBehavior(false, false),
            curve: Curves.Linear,
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.InRange(halfway.FontSize, 10.1, 29.9);
        Assert.InRange(halfway.LetterSpacing, 0.1, 3.9);
        Assert.Equal(TextAlign.End, halfway.TextAlign);
        Assert.False(halfway.SoftWrap);
        Assert.Equal(TextOverflow.Ellipsis, halfway.Overflow);
        Assert.Equal(1, halfway.MaxLines);
        Assert.Equal(TextWidthBasis.LongestLine, halfway.TextWidthBasis);
        Assert.Equal(new TextHeightBehavior(false, false), halfway.TextHeightBehavior);
        double interruptedFontSize = halfway.FontSize;

        root.Update(new AnimatedDefaultTextStyle(
            child: new Text("animated text"),
            style: new TextStyle(FontSize: 18, Color: Colors.Green, LetterSpacing: 1),
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.Linear,
            onEnd: () => completed++));
        owner.FlushBuild();
        Assert.Equal(
            interruptedFontSize,
            RequireRenderObject<RenderParagraph>(root.ChildElement).FontSize,
            precision: 6);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        var finished = RequireRenderObject<RenderParagraph>(root.ChildElement);
        Assert.Equal(18, finished.FontSize, precision: 6);
        Assert.Equal(Colors.Green, Assert.IsType<SolidColorBrush>(finished.Foreground).Color);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void PhysicalModel_CreatesAndUpdatesSourceShapedRenderObject()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new PhysicalModel(
            color: Colors.Red,
            shape: BoxShape.Rectangle,
            clipBehavior: Clip.HardEdge,
            borderRadius: BorderRadius.Circular(6),
            elevation: 2,
            shadowColor: Colors.Black,
            child: new SizedBox(width: 40, height: 24)));
        Mount(root, owner);

        var physical = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.Equal(BoxShape.Rectangle, physical.Shape);
        Assert.Equal(Clip.HardEdge, physical.ClipBehavior);
        Assert.Equal(BorderRadius.Circular(6), physical.BorderRadius);
        Assert.Equal(2, physical.Elevation);
        Assert.Equal(Colors.Red, physical.Color);
        Assert.Equal(Colors.Black, physical.ShadowColor);

        root.Update(new PhysicalModel(
            color: Colors.Blue,
            shape: BoxShape.Circle,
            clipBehavior: Clip.AntiAlias,
            elevation: 8,
            shadowColor: Colors.Purple,
            child: new SizedBox(width: 40, height: 24)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.Same(physical, updated);
        Assert.Equal(BoxShape.Circle, updated.Shape);
        Assert.Equal(Clip.AntiAlias, updated.ClipBehavior);
        Assert.Equal(8, updated.Elevation);
        Assert.Equal(Colors.Blue, updated.Color);
        Assert.Equal(Colors.Purple, updated.ShadowColor);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalModel(
            color: Colors.Red,
            elevation: -1));

        root.Unmount();
    }

    [Fact]
    public void RenderPhysicalModel_PaintsSurfaceShadowAndShapeAwareClip()
    {
        var physical = new RenderPhysicalModel(
            color: Colors.Orange,
            child: new RenderColoredBox(
                Colors.Green,
                new RenderConstrainedBox(BoxConstraints.TightFor(width: 40, height: 24))),
            shape: BoxShape.Circle,
            clipBehavior: Clip.AntiAlias,
            elevation: 4,
            shadowColor: Colors.Black);
        var renderView = new RenderView { Child = physical };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(40, 24));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();

        Assert.Equal(new Size(40, 24), physical.Size);
        Assert.IsType<PictureLayer>(pipeline.RootLayer.Children[0]);
        var clip = Assert.IsType<ClipGeometryLayer>(pipeline.RootLayer.Children[1]);
        Assert.IsType<EllipseGeometry>(clip.Geometry);
        Assert.IsType<PictureLayer>(Assert.Single(clip.Children));
    }

    [Fact]
    public void AnimatedPhysicalModel_InterpolatesVisualsAndHonorsColorAnimationFlags()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedPhysicalModel(
            child: new SizedBox(width: 40, height: 24),
            color: Colors.Red,
            shadowColor: Colors.Black,
            duration: TimeSpan.FromMilliseconds(200)));
        Mount(root, owner);

        root.Update(new AnimatedPhysicalModel(
            child: new SizedBox(width: 40, height: 24),
            color: Colors.Blue,
            shadowColor: Colors.Purple,
            duration: TimeSpan.FromMilliseconds(200),
            shape: BoxShape.Circle,
            clipBehavior: Clip.HardEdge,
            borderRadius: BorderRadius.Circular(20),
            elevation: 12,
            curve: Curves.Linear,
            onEnd: () => completed++));
        owner.FlushBuild();

        var immediate = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.Equal(BoxShape.Circle, immediate.Shape);
        Assert.Equal(Clip.HardEdge, immediate.ClipBehavior);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        var halfway = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.InRange(halfway.BorderRadius!.Value.Radius, 0.1, 19.9);
        Assert.InRange(halfway.Elevation, 0.1, 11.9);
        Assert.NotEqual(Colors.Red, halfway.Color);
        Assert.NotEqual(Colors.Blue, halfway.Color);
        double interruptedElevation = halfway.Elevation;

        root.Update(new AnimatedPhysicalModel(
            child: new SizedBox(width: 40, height: 24),
            color: Colors.Green,
            shadowColor: Colors.Orange,
            duration: TimeSpan.FromMilliseconds(200),
            borderRadius: BorderRadius.Circular(4),
            elevation: 6,
            animateColor: false,
            animateShadowColor: false,
            curve: Curves.Linear,
            onEnd: () => completed++));
        owner.FlushBuild();
        var interrupted = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.Equal(interruptedElevation, interrupted.Elevation, precision: 6);
        Assert.Equal(Colors.Green, interrupted.Color);
        Assert.Equal(Colors.Orange, interrupted.ShadowColor);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        var finished = RequireRenderObject<RenderPhysicalModel>(root.ChildElement);
        Assert.Equal(4, finished.BorderRadius!.Value.Radius, precision: 6);
        Assert.Equal(6, finished.Elevation, precision: 6);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AlignDemoPage_LeavingRouteUnmountsNestedAnimationsWithoutDoubleDispose()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new AlignDemoPage());
        Mount(root, owner);

        root.Update(new SizedBox(width: 1, height: 1));
        owner.FlushBuild();

        root.Unmount();
    }

    private static Widget BuildAnimatedPositioned(
        double? left,
        double? top,
        double? width,
        double? height,
        Action onEnd)
    {
        return new Stack(children:
        [
            new AnimatedPositioned(
                child: new SizedBox(width: 8, height: 8),
                duration: TimeSpan.FromMilliseconds(200),
                left: left,
                top: top,
                width: width,
                height: height,
                curve: Curves.Linear,
                onEnd: onEnd),
        ]);
    }

    private static Widget BuildDirectionalPositioned(
        Plumix.UI.TextDirection direction,
        double start,
        Action onEnd)
    {
        return new Directionality(
            textDirection: direction,
            child: new Stack(children:
            [
                new AnimatedPositionedDirectional(
                    child: new SizedBox(width: 8, height: 8),
                    duration: TimeSpan.FromMilliseconds(200),
                    start: start,
                    top: 4,
                    width: 16,
                    height: 12,
                    curve: Curves.Linear,
                    onEnd: onEnd),
            ]));
    }

    private static StackParentData GetOnlyStackParentData(TestRootElement root)
    {
        var stack = RequireRenderObject<RenderStack>(root.ChildElement);
        var child = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        return Assert.IsType<StackParentData>(child.parentData);
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null) visitor(_child);
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child)) _child = null;
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null) throw new InvalidOperationException("TestRootElement expects null slot.");
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null) throw new InvalidOperationException("TestRootElement expects null slot.");
        }
    }
}
