using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class InheritedWidgetTests
{
    [Fact]
    public void InheritedWidget_NotifiesOnlyRegisteredDependents()
    {
        InheritedTracker.Reset();

        var owner = new BuildOwner();
        var dependent = new IntScopeDependentProbeWidget();
        var passive = new PassiveProbeWidget();
        var stableChildTree = new Row(children: [dependent, passive]);

        var root = new TestRootElement(new IntScope(value: 1, child: stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([1], InheritedTracker.DependentBuildValues);
        Assert.Equal(1, InheritedTracker.DependentDidChangeCount);
        Assert.Equal(1, InheritedTracker.PassiveBuildCount);

        root.Update(new IntScope(value: 2, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal([1, 2], InheritedTracker.DependentBuildValues);
        Assert.Equal(2, InheritedTracker.DependentDidChangeCount);
        Assert.Equal(1, InheritedTracker.PassiveBuildCount);
    }

    [Fact]
    public void InheritedWidget_UpdateShouldNotifyFalse_DoesNotTriggerDidChangeDependencies()
    {
        InheritedTracker.Reset();

        var owner = new BuildOwner();
        var dependent = new ConditionalScopeDependentProbeWidget();
        var passive = new PassiveProbeWidget();
        var stableChildTree = new Row(children: [dependent, passive]);

        var root = new TestRootElement(new ConditionalScope(value: 1, shouldNotify: true, child: stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        root.Update(new ConditionalScope(value: 2, shouldNotify: false, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal([1], InheritedTracker.DependentBuildValues);
        Assert.Equal(1, InheritedTracker.DependentDidChangeCount);
        Assert.Equal(1, InheritedTracker.PassiveBuildCount);

        root.Update(new ConditionalScope(value: 3, shouldNotify: true, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal([1, 3], InheritedTracker.DependentBuildValues);
        Assert.Equal(2, InheritedTracker.DependentDidChangeCount);
        Assert.Equal(1, InheritedTracker.PassiveBuildCount);
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

    private sealed class IntScope : InheritedWidget
    {
        public IntScope(int value, Widget child) : base()
        {
            Value = value;
            Child = child;
        }

        public int Value { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Value != ((IntScope)oldWidget).Value;
        }
    }

    private sealed class ConditionalScope : InheritedWidget
    {
        public ConditionalScope(int value, bool shouldNotify, Widget child) : base()
        {
            Value = value;
            ShouldNotify = shouldNotify;
            Child = child;
        }

        public int Value { get; }

        public bool ShouldNotify { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return ShouldNotify;
        }
    }

    private sealed class IntScopeDependentProbeWidget : StatefulWidget
    {
        public override State CreateState() => new IntScopeDependentProbeState();
    }

    private sealed class IntScopeDependentProbeState : State
    {
        public override void DidChangeDependencies()
        {
            InheritedTracker.DependentDidChangeCount += 1;
        }

        public override Widget Build(BuildContext context)
        {
            var scope = context.DependOnInherited<IntScope>() ?? throw new InvalidOperationException("Expected IntScope.");
            InheritedTracker.DependentBuildValues.Add(scope.Value);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ConditionalScopeDependentProbeWidget : StatefulWidget
    {
        public override State CreateState() => new ConditionalScopeDependentProbeState();
    }

    private sealed class ConditionalScopeDependentProbeState : State
    {
        public override void DidChangeDependencies()
        {
            InheritedTracker.DependentDidChangeCount += 1;
        }

        public override Widget Build(BuildContext context)
        {
            var scope = context.DependOnInherited<ConditionalScope>() ?? throw new InvalidOperationException("Expected ConditionalScope.");
            InheritedTracker.DependentBuildValues.Add(scope.Value);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class PassiveProbeWidget : StatefulWidget
    {
        public override State CreateState() => new PassiveProbeState();
    }

    private sealed class PassiveProbeState : State
    {
        public override Widget Build(BuildContext context)
        {
            InheritedTracker.PassiveBuildCount += 1;
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class InheritedTracker
    {
        public static readonly List<int> DependentBuildValues = [];
        public static int DependentDidChangeCount;
        public static int PassiveBuildCount;

        public static void Reset()
        {
            DependentBuildValues.Clear();
            DependentDidChangeCount = 0;
            PassiveBuildCount = 0;
        }
    }
}
