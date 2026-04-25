using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (parity regression tests)

namespace Plumix.Tests;

public sealed class InheritedModelTests
{
    [Fact]
    public void InheritedModel_RebuildsOnlyDependentsForChangedAspect()
    {
        InheritedModelTracker.Reset();

        var owner = new BuildOwner();
        var stableChildTree = new Row(children: [new AspectAReaderWidget(), new AspectBReaderWidget()]);

        var root = new TestRootElement(new ABModel(a: 1, b: 10, child: stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(1, InheritedModelTracker.AspectABuildCount);
        Assert.Equal(1, InheritedModelTracker.AspectBBuildCount);

        root.Update(new ABModel(a: 2, b: 10, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal(2, InheritedModelTracker.AspectABuildCount);
        Assert.Equal(1, InheritedModelTracker.AspectBBuildCount);

        root.Update(new ABModel(a: 2, b: 20, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal(2, InheritedModelTracker.AspectABuildCount);
        Assert.Equal(2, InheritedModelTracker.AspectBBuildCount);
    }

    [Fact]
    public void InheritedModel_UnqualifiedDependency_RebuildsOnAnyModelChange()
    {
        InheritedModelTracker.Reset();

        var owner = new BuildOwner();
        var stableChildTree = new Row(children: [new UnqualifiedReaderWidget()]);

        var root = new TestRootElement(new ABModel(a: 1, b: 10, child: stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(1, InheritedModelTracker.UnqualifiedBuildCount);

        root.Update(new ABModel(a: 2, b: 10, child: stableChildTree));
        owner.FlushBuild();
        root.Update(new ABModel(a: 2, b: 20, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal(3, InheritedModelTracker.UnqualifiedBuildCount);
    }

    [Fact]
    public void InheritedModel_DependentWithMultipleAspects_RebuildsWhenAnyTrackedAspectChanges()
    {
        InheritedModelTracker.Reset();

        var owner = new BuildOwner();
        var stableChildTree = new Row(children: [new MultiAspectReaderWidget()]);

        var root = new TestRootElement(new ABModel(a: 1, b: 10, child: stableChildTree));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(1, InheritedModelTracker.MultiAspectBuildCount);

        root.Update(new ABModel(a: 2, b: 10, child: stableChildTree));
        owner.FlushBuild();
        root.Update(new ABModel(a: 2, b: 20, child: stableChildTree));
        owner.FlushBuild();

        Assert.Equal(3, InheritedModelTracker.MultiAspectBuildCount);
    }

    [Fact]
    public void InheritedModel_Shadowing_UsesNearestSupportingModelForEachAspect()
    {
        InheritedModelTracker.Reset();

        var owner = new BuildOwner();
        var stableChildTree = new Row(children: [new ShadowedAspectAReaderWidget(), new ShadowedAspectBReaderWidget()]);

        Widget BuildTree(int outerA, int outerB, int innerA, int innerB)
        {
            return new ShadowingABModel(
                a: outerA,
                b: outerB,
                supportedAspects: [ABAspect.A, ABAspect.B],
                child: new ShadowingABModel(
                    a: innerA,
                    b: innerB,
                    supportedAspects: [ABAspect.A],
                    child: stableChildTree));
        }

        var root = new TestRootElement(BuildTree(outerA: 10, outerB: 100, innerA: 1, innerB: 1000));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal([1], InheritedModelTracker.ShadowASeenValues);
        Assert.Equal([100], InheritedModelTracker.ShadowBSeenValues);
        Assert.Equal(1, InheritedModelTracker.ShadowABuildCount);
        Assert.Equal(1, InheritedModelTracker.ShadowBBuildCount);

        root.Update(BuildTree(outerA: 11, outerB: 100, innerA: 1, innerB: 1000));
        owner.FlushBuild();

        Assert.Equal(1, InheritedModelTracker.ShadowABuildCount);
        Assert.Equal(1, InheritedModelTracker.ShadowBBuildCount);

        root.Update(BuildTree(outerA: 11, outerB: 100, innerA: 2, innerB: 1000));
        owner.FlushBuild();

        Assert.Equal([1, 2], InheritedModelTracker.ShadowASeenValues);
        Assert.Equal(2, InheritedModelTracker.ShadowABuildCount);
        Assert.Equal(1, InheritedModelTracker.ShadowBBuildCount);

        root.Update(BuildTree(outerA: 11, outerB: 200, innerA: 2, innerB: 1000));
        owner.FlushBuild();

        Assert.Equal([100, 200], InheritedModelTracker.ShadowBSeenValues);
        Assert.Equal(2, InheritedModelTracker.ShadowABuildCount);
        Assert.Equal(2, InheritedModelTracker.ShadowBBuildCount);
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

    private enum ABAspect
    {
        A,
        B
    }

    private sealed class ABModel : InheritedModel<ABAspect>
    {
        public ABModel(int a, int b, Widget child) : base()
        {
            A = a;
            B = b;
            Child = child;
        }

        public int A { get; }

        public int B { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            var oldModel = (ABModel)oldWidget;
            return A != oldModel.A || B != oldModel.B;
        }

        protected internal override bool UpdateShouldNotifyDependent(InheritedModel<ABAspect> oldWidget, IReadOnlySet<ABAspect> dependencies)
        {
            var oldModel = (ABModel)oldWidget;
            return (A != oldModel.A && dependencies.Contains(ABAspect.A))
                || (B != oldModel.B && dependencies.Contains(ABAspect.B));
        }
    }

    private sealed class ShadowingABModel : InheritedModel<ABAspect>
    {
        private readonly HashSet<ABAspect> _supportedAspects;

        public ShadowingABModel(int a, int b, IEnumerable<ABAspect> supportedAspects, Widget child) : base()
        {
            A = a;
            B = b;
            _supportedAspects = [..supportedAspects];
            Child = child;
        }

        public int A { get; }

        public int B { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected internal override bool IsSupportedAspect(object aspect)
        {
            return aspect is ABAspect typedAspect && _supportedAspects.Contains(typedAspect);
        }

        protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            var oldModel = (ShadowingABModel)oldWidget;
            return A != oldModel.A || B != oldModel.B;
        }

        protected internal override bool UpdateShouldNotifyDependent(InheritedModel<ABAspect> oldWidget, IReadOnlySet<ABAspect> dependencies)
        {
            var oldModel = (ShadowingABModel)oldWidget;
            return (A != oldModel.A && dependencies.Contains(ABAspect.A))
                || (B != oldModel.B && dependencies.Contains(ABAspect.B));
        }
    }

    private sealed class AspectAReaderWidget : StatefulWidget
    {
        public override State CreateState() => new AspectAReaderState();
    }

    private sealed class AspectAReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = InheritedModel<ABAspect>.InheritFrom<ABModel>(context, ABAspect.A)
                        ?? throw new InvalidOperationException("ABModel not found.");
            _ = model.A;
            InheritedModelTracker.AspectABuildCount += 1;
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class AspectBReaderWidget : StatefulWidget
    {
        public override State CreateState() => new AspectBReaderState();
    }

    private sealed class AspectBReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = InheritedModel<ABAspect>.InheritFrom<ABModel>(context, ABAspect.B)
                        ?? throw new InvalidOperationException("ABModel not found.");
            _ = model.B;
            InheritedModelTracker.AspectBBuildCount += 1;
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class UnqualifiedReaderWidget : StatefulWidget
    {
        public override State CreateState() => new UnqualifiedReaderState();
    }

    private sealed class UnqualifiedReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = InheritedModel<ABAspect>.InheritFrom<ABModel>(context)
                        ?? throw new InvalidOperationException("ABModel not found.");
            _ = model.A;
            InheritedModelTracker.UnqualifiedBuildCount += 1;
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class MultiAspectReaderWidget : StatefulWidget
    {
        public override State CreateState() => new MultiAspectReaderState();
    }

    private sealed class MultiAspectReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var modelA = InheritedModel<ABAspect>.InheritFrom<ABModel>(context, ABAspect.A)
                         ?? throw new InvalidOperationException("ABModel not found.");
            var modelB = InheritedModel<ABAspect>.InheritFrom<ABModel>(context, ABAspect.B)
                         ?? throw new InvalidOperationException("ABModel not found.");

            _ = modelA.A + modelB.B;
            InheritedModelTracker.MultiAspectBuildCount += 1;
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ShadowedAspectAReaderWidget : StatefulWidget
    {
        public override State CreateState() => new ShadowedAspectAReaderState();
    }

    private sealed class ShadowedAspectAReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = InheritedModel<ABAspect>.InheritFrom<ShadowingABModel>(context, ABAspect.A)
                        ?? throw new InvalidOperationException("ShadowingABModel not found.");
            InheritedModelTracker.ShadowABuildCount += 1;
            InheritedModelTracker.ShadowASeenValues.Add(model.A);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class ShadowedAspectBReaderWidget : StatefulWidget
    {
        public override State CreateState() => new ShadowedAspectBReaderState();
    }

    private sealed class ShadowedAspectBReaderState : State
    {
        public override Widget Build(BuildContext context)
        {
            var model = InheritedModel<ABAspect>.InheritFrom<ShadowingABModel>(context, ABAspect.B)
                        ?? throw new InvalidOperationException("ShadowingABModel not found.");
            InheritedModelTracker.ShadowBBuildCount += 1;
            InheritedModelTracker.ShadowBSeenValues.Add(model.B);
            return new SizedBox(width: 1, height: 1);
        }
    }

    private static class InheritedModelTracker
    {
        public static int AspectABuildCount;
        public static int AspectBBuildCount;
        public static int UnqualifiedBuildCount;
        public static int MultiAspectBuildCount;
        public static int ShadowABuildCount;
        public static int ShadowBBuildCount;
        public static readonly List<int> ShadowASeenValues = [];
        public static readonly List<int> ShadowBSeenValues = [];

        public static void Reset()
        {
            AspectABuildCount = 0;
            AspectBBuildCount = 0;
            UnqualifiedBuildCount = 0;
            MultiAspectBuildCount = 0;
            ShadowABuildCount = 0;
            ShadowBBuildCount = 0;
            ShadowASeenValues.Clear();
            ShadowBSeenValues.Clear();
        }
    }
}
