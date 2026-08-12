using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class ModalBarrierTests
{
    [Fact]
    public void ModalBarrier_DefaultsAndAnimatedDefaultsMatchFlutter()
    {
        var barrier = new ModalBarrier();

        Assert.Null(barrier.Color);
        Assert.True(barrier.Dismissible);
        Assert.Null(barrier.OnDismiss);
        Assert.Null(barrier.SemanticsLabel);
        Assert.True(barrier.BarrierSemanticsDismissible);
        Assert.Null(barrier.ClipDetailsNotifier);
        Assert.Null(barrier.SemanticsOnTapHint);

        var animation = new ManualAnimation<Color?>(Colors.Red);
        var animated = new AnimatedModalBarrier(animation);

        Assert.Same(animation, animated.Color);
        Assert.True(animated.Dismissible);
        Assert.Null(animated.SemanticsLabel);
        Assert.Null(animated.BarrierSemanticsDismissible);
        Assert.Null(animated.OnDismiss);
        Assert.Null(animated.ClipDetailsNotifier);
        Assert.Null(animated.SemanticsOnTapHint);
        Assert.Throws<ArgumentNullException>(() => new AnimatedModalBarrier(null!));
    }

    [Fact]
    public void ModalBarrier_PlatformSemanticsPolicyMatchesFlutter()
    {
        Assert.True(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.Android));
        Assert.True(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.IOS));
        Assert.True(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.MacOS));
        Assert.False(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.Fuchsia));
        Assert.False(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.Linux));
        Assert.False(ModalBarrier.PlatformSupportsDismissingBarrier(TargetPlatform.Windows));
    }

    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Fuchsia, false)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void ModalBarrier_BuildsSourceShapedBlockingOpaqueSurface(TargetPlatform platform, bool semanticsSupported)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            int dismissCount = 0;
            var owner = new BuildOwner();
            var root = new TestRootElement(new Directionality(
                TextDirection.Rtl,
                new ModalBarrier(
                    color: Colors.SlateBlue,
                    semanticsLabel: "Close overlay",
                    semanticsOnTapHint: "Close the overlay",
                    onDismiss: () => dismissCount++)));
            Mount(root, owner);

            var block = FindWidget<BlockSemantics>(root);
            var exclude = FindWidget<ExcludeSemantics>(root);
            var gesture = FindWidget<GestureDetector>(root);
            var semantics = FindWidget<Semantics>(root);
            var constraints = FindWidget<ConstrainedBox>(root);
            var coloredBox = FindWidget<ColoredBox>(root);

            Assert.True(block.Blocking);
            Assert.Equal(HitTestBehavior.Opaque, gesture.Behavior);
            Assert.NotNull(gesture.OnTap);
            Assert.NotNull(gesture.OnSecondaryTap);
            Assert.Equal(BoxConstraints.Expand(), constraints.Constraints);
            Assert.Equal(Colors.SlateBlue, coloredBox.Color);
            Assert.Equal("Close the overlay", semantics.OnTapHint);

            // Only platforms that support dismissing the barrier expose it to semantics.
            Assert.Equal(!semanticsSupported, exclude.Excluding);
            Assert.Equal(semanticsSupported ? "Close overlay" : null, semantics.Label);

            gesture.OnTap!();
            gesture.OnSecondaryTap!();
            Assert.Equal(2, dismissCount);
            root.Unmount();
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void ModalBarrier_NonDismissibleTapEmitsAlertFeedback()
    {
        int alertCount = 0;
        Action<SystemSoundType> listener = type =>
        {
            if (type == SystemSoundType.Alert)
            {
                alertCount++;
            }
        };

        SystemSound.SoundRequested += listener;
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new ModalBarrier(dismissible: false));
            Mount(root, owner);

            FindWidget<GestureDetector>(root).OnTap!();

            Assert.Equal(1, alertCount);
            root.Unmount();
        }
        finally
        {
            SystemSound.SoundRequested -= listener;
        }
    }

    [Fact]
    public void AnimatedModalBarrier_RebuildsWithCurrentAnimationColor()
    {
        var animation = new ManualAnimation<Color?>(Colors.Red);
        var owner = new BuildOwner();
        var root = new TestRootElement(new AnimatedModalBarrier(animation));
        Mount(root, owner);

        Assert.Equal(Colors.Red, FindWidget<ModalBarrier>(root).Color);

        animation.SetValue(Colors.Blue);
        owner.FlushBuild();

        Assert.Equal(Colors.Blue, FindWidget<ModalBarrier>(root).Color);
        root.Unmount();
    }

    [Fact]
    public void SemanticsClipper_TracksNotifierAndUpdatesBarrierFocusBounds()
    {
        var notifier = new ValueNotifier<Thickness>(new Thickness(10, 5, 20, 15));
        var label = new RenderSemanticsAnnotations(
            label: "Barrier",
            child: new RenderConstrainedBox(BoxConstraints.Expand()));
        var clipper = new RenderSemanticsClipper(notifier, label);
        var renderView = new RenderView { Child = clipper };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 80));
        pipeline.FlushSemantics();

        var root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        var clipperNode = Assert.Single(root!.Children);
        Assert.Equal(new Rect(10, 5, 70, 60), clipperNode.Rect);
        Assert.Equal("Barrier", clipperNode.Label);
        Assert.Empty(clipperNode.Children);

        notifier.Value = new Thickness(4, 3, 6, 7);
        pipeline.FlushSemantics();

        root = pipeline.SemanticsOwner.RootNode;
        Assert.NotNull(root);
        clipperNode = Assert.Single(root!.Children);
        Assert.Equal(new Rect(4, 3, 90, 70), clipperNode.Rect);
        notifier.Dispose();
    }

    [Fact]
    public void BlockSemantics_DropsPreviouslyPaintedSiblingSemantics()
    {
        var behind = new RenderSemanticsAnnotations(
            label: "Behind",
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 30, height: 20)));
        var foreground = new RenderSemanticsAnnotations(
            label: "Foreground",
            child: new RenderConstrainedBox(BoxConstraints.TightFor(width: 30, height: 20)));
        var block = new RenderBlockSemantics(child: foreground);
        var stack = new RenderStack(children: [behind, block]);
        var renderView = new RenderView { Child = stack };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        pipeline.FlushLayout(new Size(100, 80));
        pipeline.FlushSemantics();

        var labels = new List<string>();
        Visit(pipeline.SemanticsOwner.RootNode);
        Assert.DoesNotContain("Behind", labels);
        Assert.Contains("Foreground", labels);

        block.Blocking = false;
        pipeline.FlushSemantics();
        labels.Clear();
        Visit(pipeline.SemanticsOwner.RootNode);
        Assert.Contains("Behind", labels);
        Assert.Contains("Foreground", labels);
        return;

        void Visit(SemanticsNode? node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Label != null)
            {
                labels.Add(node.Label);
            }

            foreach (var child in node.Children)
            {
                Visit(child);
            }
        }
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private static T FindWidget<T>(Element root) where T : Widget
    {
        T? result = null;
        Visit(root);
        return result ?? throw new InvalidOperationException($"Widget {typeof(T).Name} was not found.");

        void Visit(Element element)
        {
            if (result != null)
            {
                return;
            }

            if (element.Widget is T match)
            {
                result = match;
                return;
            }

            element.VisitChildren(Visit);
        }
    }

    private sealed class ManualAnimation<T> : Animation<T>
    {
        private readonly List<Action> _listeners = [];
        private readonly List<Action<AnimationStatus>> _statusListeners = [];
        private T _value;

        public ManualAnimation(T value)
        {
            _value = value;
        }

        public override T Value => _value;

        public override AnimationStatus Status => AnimationStatus.Completed;

        public void SetValue(T value)
        {
            _value = value;
            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Add(listener);
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Remove(listener);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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
            if (_child != null)
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
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
