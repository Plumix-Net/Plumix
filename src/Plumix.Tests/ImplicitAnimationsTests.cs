using Avalonia;
using Plumix.Rendering;
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
