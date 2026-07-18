using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class TransitionsTests : IDisposable
{
    public TransitionsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ScaleAndRotationTransition_ExposeFlutterDefaultsAndSharedMatrixSurface()
    {
        var scaleAnimation = new TestAnimation(1.5, AnimationStatus.Completed);
        var scaleChild = new SizedBox(width: 20, height: 10);
        var scale = new ScaleTransition(scaleAnimation, child: scaleChild);

        Assert.Same(scaleAnimation, scale.Scale);
        Assert.Same(scaleAnimation, scale.Animation);
        Assert.Same(scaleAnimation, scale.Listenable);
        Assert.Equal(Alignment.Center, scale.Alignment);
        Assert.Null(scale.FilterQuality);
        Assert.Same(scaleChild, scale.Child);

        var turnsAnimation = new TestAnimation(0.25, AnimationStatus.Completed);
        var rotation = new RotationTransition(turnsAnimation);

        Assert.Same(turnsAnimation, rotation.Turns);
        Assert.Same(turnsAnimation, rotation.Animation);
        Assert.Equal(Alignment.Center, rotation.Alignment);
        Assert.Null(rotation.FilterQuality);
        Assert.Null(rotation.Child);

        Assert.Throws<ArgumentNullException>(() => new ScaleTransition(null!));
        Assert.Throws<ArgumentNullException>(() => new RotationTransition(null!));
        Assert.Throws<ArgumentNullException>(() => new MatrixTransition(
            scaleAnimation,
            onTransform: null!));
    }

    [Fact]
    public void MatrixTransition_UsesCallbackAlignmentAndAnimatedFilterQualityPolicy()
    {
        var animation = new TestAnimation(0.4, AnimationStatus.Dismissed);
        int callbackCount = 0;
        var transition = new MatrixTransition(
            animation: animation,
            onTransform: value =>
            {
                callbackCount++;
                return Matrix.CreateTranslation(value * 10.0, value * 20.0);
            },
            alignment: Alignment.BottomRight,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 24, height: 16));
        var owner = new BuildOwner();
        var root = new TestRootElement(transition);
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(4.0, 8.0), renderTransform.Transform);
        Assert.Equal(Alignment.BottomRight, renderTransform.Alignment);
        Assert.Null(renderTransform.FilterQuality);
        Assert.Equal(1, callbackCount);

        animation.Set(0.6, AnimationStatus.Forward);
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(6.0, 12.0), renderTransform.Transform);
        Assert.Equal(FilterQuality.High, renderTransform.FilterQuality);
        Assert.Equal(2, callbackCount);

        animation.Set(1.0, AnimationStatus.Completed);
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateTranslation(10.0, 20.0), renderTransform.Transform);
        Assert.Null(renderTransform.FilterQuality);
        Assert.Equal(3, callbackCount);

        root.Unmount();
    }

    [Fact]
    public void ScaleTransition_RebuildsFromAnimationAndRebindsWhenAnimationChanges()
    {
        var firstAnimation = new TestAnimation(0.5, AnimationStatus.Forward);
        var secondAnimation = new TestAnimation(1.25, AnimationStatus.Reverse);
        var owner = new BuildOwner();
        var root = new TestRootElement(new ScaleTransition(
            scale: firstAnimation,
            alignment: Alignment.TopLeft,
            filterQuality: FilterQuality.Low,
            child: new SizedBox(width: 30, height: 20)));
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(Matrix.CreateScale(0.5, 0.5), renderTransform.Transform);
        Assert.Equal(Alignment.TopLeft, renderTransform.Alignment);
        Assert.Equal(FilterQuality.Low, renderTransform.FilterQuality);
        Assert.Equal(1, firstAnimation.ListenerCount);

        firstAnimation.Set(0.75, AnimationStatus.Forward);
        owner.FlushBuild();
        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(0.75, 0.75), renderTransform.Transform);

        root.Update(new ScaleTransition(
            scale: secondAnimation,
            alignment: Alignment.TopLeft,
            filterQuality: FilterQuality.Low,
            child: new SizedBox(width: 30, height: 20)));
        owner.FlushBuild();

        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(1.25, 1.25), renderTransform.Transform);
        Assert.Equal(0, firstAnimation.ListenerCount);
        Assert.Equal(1, secondAnimation.ListenerCount);

        firstAnimation.Set(2.0, AnimationStatus.Completed);
        owner.FlushBuild();
        renderTransform = Assert.IsType<RenderTransform>(root.ChildElement.RenderObject);
        Assert.Equal(Matrix.CreateScale(1.25, 1.25), renderTransform.Transform);

        root.Unmount();
        Assert.Equal(0, secondAnimation.ListenerCount);
    }

    [Fact]
    public void MatrixTransition_DropsFilterQualityOnAnimationControllerTerminalFrame()
    {
        using var animation = new AnimationController(TimeSpan.FromMilliseconds(100));
        var owner = new BuildOwner();
        var root = new TestRootElement(new ScaleTransition(
            scale: animation,
            filterQuality: FilterQuality.High,
            child: new SizedBox(width: 20, height: 20)));
        Mount(root, owner);

        animation.Forward(from: 0.0);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(AnimationStatus.Completed, animation.Status);
        Assert.Equal(Matrix.Identity, renderTransform.Transform);
        Assert.Null(renderTransform.FilterQuality);

        root.Unmount();
    }

    [Theory]
    [InlineData(0.0, 1.0, 0.0, 0.0, 1.0)]
    [InlineData(0.25, 0.0, 1.0, -1.0, 0.0)]
    [InlineData(0.5, -1.0, 0.0, 0.0, -1.0)]
    [InlineData(0.75, 0.0, -1.0, 1.0, 0.0)]
    public void RotationTransition_ConvertsTurnsToExactCardinalMatrices(
        double turns,
        double m11,
        double m12,
        double m21,
        double m22)
    {
        var animation = new TestAnimation(turns, AnimationStatus.Completed);
        var transition = new RotationTransition(animation);
        var owner = new BuildOwner();
        var root = new TestRootElement(transition);
        Mount(root, owner);

        var renderTransform = Assert.IsType<RenderTransform>(root.ChildElement!.RenderObject);
        Assert.Equal(new Matrix(m11, m12, m21, m22, 0, 0), renderTransform.Transform);

        root.Unmount();
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestAnimation : Animation<double>
    {
        private readonly List<Action> _listeners = [];
        private readonly List<Action<AnimationStatus>> _statusListeners = [];
        private double _value;
        private AnimationStatus _status;

        public TestAnimation(double value, AnimationStatus status)
        {
            _value = value;
            _status = status;
        }

        public override double Value => _value;

        public override AnimationStatus Status => _status;

        public int ListenerCount => _listeners.Count;

        public override void AddListener(Action listener) => _listeners.Add(listener);

        public override void RemoveListener(Action listener) => _listeners.Remove(listener);

        public override void AddStatusListener(Action<AnimationStatus> listener) => _statusListeners.Add(listener);

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
            _statusListeners.Remove(listener);
        }

        public void Set(double value, AnimationStatus status)
        {
            AnimationStatus previousStatus = _status;
            _value = value;
            _status = status;
            if (previousStatus != status)
            {
                foreach (var listener in _statusListeners.ToArray())
                {
                    listener(status);
                }
            }

            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }
        }
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
