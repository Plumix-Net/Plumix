using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class AnimatedSwitcherTests : IDisposable
{
    public AnimatedSwitcherTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void AnimatedSwitcher_ExposesFlutterDefaultsAndValidatesDurations()
    {
        var switcher = new AnimatedSwitcher(
            duration: TimeSpan.FromMilliseconds(240),
            child: new Text("child"));

        Assert.Equal(TimeSpan.FromMilliseconds(240), switcher.Duration);
        Assert.Null(switcher.ReverseDuration);
        Assert.Equal(Curves.Linear(0.35), switcher.SwitchInCurve(0.35));
        Assert.Equal(Curves.Linear(0.35), switcher.SwitchOutCurve(0.35));
        Assert.IsType<FadeTransition>(switcher.TransitionBuilder(switcher.Child!, new TestAnimation(0.4)));
        var defaultLayout = Assert.IsType<Stack>(switcher.LayoutBuilder(switcher.Child, []));
        Assert.Equal(Clip.HardEdge, defaultLayout.ClipBehavior);
        Assert.IsType<RenderStack>(CreateRenderObject(defaultLayout));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedSwitcher(
            duration: TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedSwitcher(
            duration: TimeSpan.Zero,
            reverseDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void AnimatedSwitcher_UsesWidgetIdentityAndKeepsRapidOutgoingChildren()
    {
        int previousCount = -1;
        int transitionBuilds = 0;
        AnimatedSwitcherTransitionBuilder transitionBuilder = (child, _) =>
        {
            transitionBuilds++;
            return child;
        };
        AnimatedSwitcherLayoutBuilder layoutBuilder = (current, previous) =>
        {
            previousCount = previous.Count;
            var children = previous.ToList();
            if (current is not null)
            {
                children.Add(current);
            }
            return new Stack(children: children);
        };
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildSwitcher(
            value: "one",
            childKey: new ValueKey<int>(1),
            transitionBuilder,
            layoutBuilder));
        Mount(root, owner);

        Assert.Equal(0, previousCount);
        Assert.Equal(1, transitionBuilds);

        root.Update(BuildSwitcher(
            value: "updated one",
            childKey: new ValueKey<int>(1),
            transitionBuilder,
            layoutBuilder));
        owner.FlushBuild();

        Assert.Equal(0, previousCount);
        Assert.Equal(2, transitionBuilds);

        root.Update(BuildSwitcher(
            value: "two",
            childKey: new ValueKey<int>(2),
            transitionBuilder,
            layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(1, previousCount);

        // A frame has to elapse before the second child can become a *retained* outgoing child: a
        // reverse from a still-dismissed controller has nothing to animate and settles at once.
        AnimationPump.Advance(0.05);
        owner.FlushBuild();

        root.Update(BuildSwitcher(
            value: "three",
            childKey: new ValueKey<int>(3),
            transitionBuilder,
            layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(2, previousCount);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.2));
        owner.FlushBuild();
        Assert.Equal(0, previousCount);

        root.Unmount();
    }

    [Fact]
    public void AnimatedSwitcher_RebuildsAllTransitionsWhenBuilderChanges()
    {
        int firstBuilderCalls = 0;
        int secondBuilderCalls = 0;
        AnimatedSwitcherLayoutBuilder layoutBuilder = (current, previous) =>
        {
            var children = previous.ToList();
            if (current is not null)
            {
                children.Add(current);
            }
            return new Stack(children: children);
        };
        AnimatedSwitcherTransitionBuilder firstBuilder = (child, _) =>
        {
            firstBuilderCalls++;
            return child;
        };
        AnimatedSwitcherTransitionBuilder secondBuilder = (child, _) =>
        {
            secondBuilderCalls++;
            return new Padding(insets: new Thickness(1), child: child);
        };
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildSwitcher(
            value: "one",
            childKey: new ValueKey<int>(1),
            firstBuilder,
            layoutBuilder));
        Mount(root, owner);

        root.Update(BuildSwitcher(
            value: "two",
            childKey: new ValueKey<int>(2),
            firstBuilder,
            layoutBuilder));
        owner.FlushBuild();
        Assert.Equal(2, firstBuilderCalls);

        root.Update(BuildSwitcher(
            value: "two",
            childKey: new ValueKey<int>(2),
            secondBuilder,
            layoutBuilder));
        owner.FlushBuild();

        Assert.Equal(3, secondBuilderCalls);
        root.Unmount();
    }

    [Fact]
    public void AnimatedCrossFade_ExposesDefaultsAndDefaultLayoutGeometry()
    {
        var crossFade = new AnimatedCrossFade(
            firstChild: new SizedBox(width: 20, height: 20),
            secondChild: new SizedBox(width: 30, height: 40),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.FromMilliseconds(200));

        Assert.Equal(CrossFadeState.ShowFirst, crossFade.CrossFadeState);
        Assert.Equal(TimeSpan.FromMilliseconds(200), crossFade.Duration);
        Assert.Null(crossFade.ReverseDuration);
        Assert.Equal((AlignmentGeometry)Alignment.TopCenter, crossFade.Alignment);
        Assert.True(crossFade.ExcludeBottomFocus);
        Assert.Null(crossFade.OnEnd);
        Assert.Equal(Curves.Linear(0.25), crossFade.FirstCurve(0.25));
        Assert.Equal(Curves.Linear(0.25), crossFade.SecondCurve(0.25));
        Assert.Equal(Curves.Linear(0.25), crossFade.SizeCurve(0.25));

        var topKey = new ValueKey<string>("top");
        var bottomKey = new ValueKey<string>("bottom");
        Widget layout = crossFade.LayoutBuilder(
            new Text("top"),
            topKey,
            new Text("bottom"),
            bottomKey);
        var stack = Assert.IsType<Stack>(layout);
        Assert.Equal(Clip.None, stack.ClipBehavior);
        Assert.Equal(2, stack.Children.Count);
        var bottom = Assert.IsType<Positioned>(stack.Children[0]);
        Assert.Equal(bottomKey, bottom.Key);
        Assert.Equal(0.0, bottom.Left);
        Assert.Equal(0.0, bottom.Top);
        Assert.Equal(0.0, bottom.Right);
        var top = Assert.IsType<Positioned>(stack.Children[1]);
        Assert.Equal(topKey, top.Key);

        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedCrossFade(
            firstChild: new SizedBox(),
            secondChild: new SizedBox(),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedCrossFade(
            firstChild: new SizedBox(),
            secondChild: new SizedBox(),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.Zero,
            reverseDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Theory]
    [InlineData(TextDirection.Ltr, 1.0)]
    [InlineData(TextDirection.Rtl, -1.0)]
    public void AnimatedCrossFade_ResolvesDirectionalAlignment(
        TextDirection direction,
        double expectedX)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Directionality(
            direction,
            new AnimatedCrossFade(
                firstChild: new SizedBox(width: 20, height: 20),
                secondChild: new SizedBox(width: 40, height: 40),
                crossFadeState: CrossFadeState.ShowFirst,
                duration: TimeSpan.FromMilliseconds(200),
                alignment: AlignmentDirectional.BottomEnd)));
        Mount(root, owner);

        var animatedSize = FindRenderObject<RenderAnimatedSize>(root.ChildElement!.RenderObject!);
        Assert.NotNull(animatedSize);
        Assert.Equal(new Alignment(expectedX, 1.0), animatedSize!.Alignment);

        root.Unmount();
    }

    [Fact]
    public void AnimatedCrossFade_FadesBothChildrenResizesAndCallsOnEnd()
    {
        int completed = 0;
        var owner = new BuildOwner();
        var root = new TestRootElement(BuildCrossFade(
            CrossFadeState.ShowFirst,
            () => completed++));
        Mount(root, owner);

        var clip = RequireRenderObject<RenderClipRect>(root.ChildElement);
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));
        Assert.Equal(20.0, clip.Size.Height);
        Assert.Equal([0.0, 1.0], FindOpacityValues(clip));

        root.Update(BuildCrossFade(
            CrossFadeState.ShowSecond,
            () => completed++));
        owner.FlushBuild();
        clip = RequireRenderObject<RenderClipRect>(root.ChildElement);
        clip.MarkNeedsLayout();
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 5.0));
        owner.FlushBuild();
        FindRenderObject<RenderAnimatedSize>(clip)!.MarkNeedsLayout();
        clip.MarkNeedsLayout();
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));
        IReadOnlyList<double> halfway = FindOpacityValues(clip);
        Assert.Equal(2, halfway.Count);
        Assert.All(halfway, opacity => Assert.InRange(opacity, 0.01, 0.99));
        Assert.InRange(clip.Size.Height, 20.1, 59.9);
        Assert.Equal(0, completed);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 12.0));
        owner.FlushBuild();
        FindRenderObject<RenderAnimatedSize>(clip)!.MarkNeedsLayout();
        clip.MarkNeedsLayout();
        clip.Layout(BoxConstraints.Loose(new Size(200, 200)));
        Assert.Equal([0.0, 1.0], FindOpacityValues(clip));
        Assert.Equal(60.0, clip.Size.Height);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedCrossFade_UsesReverseDurationAndBottomInteractionPolicies()
    {
        bool? bottomTickerEnabled = null;
        bool? bottomIgnoring = null;
        bool? bottomExcludingSemantics = null;
        bool? bottomExcludingFocus = null;
        AnimatedCrossFadeBuilder builder = (top, _, bottom, _) =>
        {
            var ticker = Assert.IsType<TickerMode>(bottom);
            bottomTickerEnabled = ticker.Enabled;
            var ignorePointer = Assert.IsType<IgnorePointer>(ticker.Child);
            bottomIgnoring = ignorePointer.Ignoring;
            var semantics = Assert.IsType<ExcludeSemantics>(ignorePointer.Child);
            bottomExcludingSemantics = semantics.Excluding;
            var focus = Assert.IsType<ExcludeFocus>(semantics.Child);
            bottomExcludingFocus = focus.Excluding;
            return new Stack(children: [bottom, top]);
        };
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedCrossFade(
            firstChild: new SizedBox(width: 20, height: 20),
            secondChild: new SizedBox(width: 20, height: 20),
            crossFadeState: CrossFadeState.ShowSecond,
            duration: TimeSpan.FromMilliseconds(400),
            reverseDuration: TimeSpan.FromMilliseconds(80),
            excludeBottomFocus: false,
            layoutBuilder: builder));
        Mount(root, owner);

        Assert.False(bottomTickerEnabled);
        Assert.True(bottomIgnoring);
        Assert.True(bottomExcludingSemantics);
        Assert.False(bottomExcludingFocus);

        root.Update(new AnimatedCrossFade(
            firstChild: new SizedBox(width: 20, height: 20),
            secondChild: new SizedBox(width: 20, height: 20),
            crossFadeState: CrossFadeState.ShowFirst,
            duration: TimeSpan.FromMilliseconds(400),
            reverseDuration: TimeSpan.FromMilliseconds(80),
            excludeBottomFocus: false,
            layoutBuilder: builder));
        owner.FlushBuild();
        Assert.True(bottomTickerEnabled);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        owner.FlushBuild();
        Assert.False(bottomTickerEnabled);

        root.Unmount();
    }

    private static AnimatedSwitcher BuildSwitcher(
        string value,
        Key childKey,
        AnimatedSwitcherTransitionBuilder transitionBuilder,
        AnimatedSwitcherLayoutBuilder layoutBuilder)
    {
        return new AnimatedSwitcher(
            duration: TimeSpan.FromMilliseconds(500),
            reverseDuration: TimeSpan.FromMilliseconds(100),
            child: new Text(value, key: childKey),
            transitionBuilder: transitionBuilder,
            layoutBuilder: layoutBuilder);
    }

    private static AnimatedCrossFade BuildCrossFade(CrossFadeState state, Action onEnd)
    {
        return new AnimatedCrossFade(
            firstChild: new SizedBox(width: 40, height: 20),
            secondChild: new SizedBox(width: 40, height: 60),
            crossFadeState: state,
            duration: TimeSpan.FromSeconds(10),
            firstCurve: Curves.Linear,
            secondCurve: Curves.Linear,
            sizeCurve: Curves.Linear,
            onEnd: onEnd);
    }

    private static RenderObject CreateRenderObject(Widget widget)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        Mount(root, owner);
        RenderObject renderObject = root.ChildElement!.RenderObject!;
        root.Unmount();
        return renderObject;
    }

    private static IReadOnlyList<double> FindOpacityValues(RenderObject root)
    {
        var values = new List<double>();
        Visit(root, renderObject =>
        {
            if (renderObject is RenderOpacity opacity)
            {
                values.Add(Math.Round(opacity.Opacity, 6));
            }
        });
        values.Sort();
        return values;
    }

    private static T? FindRenderObject<T>(RenderObject root) where T : RenderObject
    {
        T? result = null;
        Visit(root, renderObject =>
        {
            if (result is null && renderObject is T match)
            {
                result = match;
            }
        });
        return result;
    }

    private static void Visit(RenderObject renderObject, Action<RenderObject> visitor)
    {
        visitor(renderObject);
        renderObject.VisitChildren(child => Visit(child, visitor));
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

    private sealed class TestAnimation : Animation<double>
    {
        public TestAnimation(double value)
        {
            Value = value;
        }

        public override double Value { get; }
        public override AnimationStatus Status => AnimationStatus.Completed;
        public override void AddListener(Action listener) { }
        public override void RemoveListener(Action listener) { }
        public override void AddStatusListener(Action<AnimationStatus> listener) { }
        public override void RemoveStatusListener(Action<AnimationStatus> listener) { }
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
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
