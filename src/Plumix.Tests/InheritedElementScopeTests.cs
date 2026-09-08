using System.Collections.Immutable;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// (Element._inheritedElements, _updateInheritance, dependOnInheritedWidgetOfExactType)

namespace Plumix.Tests;

/// <summary>
/// Pins the inherited-scope map: every element carries the
/// <c>PersistentHashMap&lt;Type, InheritedElement&gt;</c> its nearest inherited ancestors built, and
/// every lookup that Dart documents as "of exact type" matches the widget's runtime type rather than
/// any assignable type. Flutter has no test for the exact-vs-subtype half (it falls out of the map
/// key being <c>widget.runtimeType</c>), so it is pinned here in both directions.
/// </summary>
public sealed class InheritedElementScopeTests
{
    [Fact]
    public void DependOnInherited_LookingUpTheBaseType_DoesNotFindASubclassScope()
    {
        var probe = new ScopeProbe();
        var owner = new BuildOwner();
        var root = new TestRootElement(new DerivedScope(value: 7, child: probe));
        Mount(root, owner);

        Assert.Null(probe.BaseScope);
        Assert.NotNull(probe.DerivedScope);
        Assert.Equal(7, probe.DerivedScope!.Value);

        root.Unmount();
    }

    [Fact]
    public void DependOnInherited_LookingUpASubclass_DoesNotFindTheBaseTypeScope()
    {
        var probe = new ScopeProbe();
        var owner = new BuildOwner();
        var root = new TestRootElement(new BaseScope(value: 3, child: probe));
        Mount(root, owner);

        Assert.Null(probe.DerivedScope);
        Assert.NotNull(probe.BaseScope);
        Assert.Equal(3, probe.BaseScope!.Value);

        root.Unmount();
    }

    [Fact]
    public void GetInherited_MatchesTheExactTypeAndCreatesNoDependency()
    {
        BaseScope? seenBase = null;
        DerivedScope? seenDerived = null;
        int buildCount = 0;

        var owner = new BuildOwner();
        Widget Body() => new Builder(context =>
        {
            buildCount++;
            seenBase = context.GetInherited<BaseScope>();
            seenDerived = context.GetInherited<DerivedScope>();
            return new SizedBox();
        });

        Widget body = Body();
        var root = new TestRootElement(new DerivedScope(value: 1, child: body));
        Mount(root, owner);

        Assert.Null(seenBase);
        Assert.Equal(1, seenDerived!.Value);
        Assert.Equal(1, buildCount);

        // GetInherited registers no dependency, so a notifying update must not rebuild the reader.
        root.Update(new DerivedScope(value: 2, child: body));
        owner.FlushBuild();

        Assert.Equal(1, buildCount);

        root.Unmount();
    }

    [Fact]
    public void GetElementForInheritedWidgetOfExactType_MatchesTheExactType()
    {
        InheritedElement? baseElement = null;
        InheritedElement? derivedElement = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new DerivedScope(value: 1, child: new Builder(context =>
        {
            baseElement = context.GetElementForInheritedWidgetOfExactType<BaseScope>();
            derivedElement = context.GetElementForInheritedWidgetOfExactType<DerivedScope>();
            return new SizedBox();
        })));
        Mount(root, owner);

        Assert.Null(baseElement);
        Assert.NotNull(derivedElement);
        Assert.IsType<DerivedScope>(derivedElement!.Widget);

        root.Unmount();
    }

    [Fact]
    public void FindAncestorWidgetOfExactType_MatchesTheExactTypeNotASubclass()
    {
        BaseHolder? seenBase = null;
        DerivedHolder? seenDerived = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new DerivedHolder(new Builder(context =>
        {
            seenBase = context.FindAncestorWidgetOfExactType<BaseHolder>();
            seenDerived = context.FindAncestorWidgetOfExactType<DerivedHolder>();
            return new SizedBox();
        })));
        Mount(root, owner);

        Assert.Null(seenBase);
        Assert.NotNull(seenDerived);

        root.Unmount();
    }

    [Fact]
    public void InheritedScope_NearestScopeOfATypeShadowsTheOuterOne()
    {
        int? inner = null;
        int? outer = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new BaseScope(
            value: 1,
            child: new Builder(context =>
            {
                outer = context.DependOnInherited<BaseScope>()!.Value;
                return new BaseScope(
                    value: 2,
                    child: new Builder(innerContext =>
                    {
                        inner = innerContext.DependOnInherited<BaseScope>()!.Value;
                        return new SizedBox();
                    }));
            })));
        Mount(root, owner);

        Assert.Equal(1, outer);
        Assert.Equal(2, inner);

        root.Unmount();
    }

    [Fact]
    public void InheritedScope_IsVisibleOnlyBelowTheInheritedElement()
    {
        BaseScope? aboveScope = null;
        BaseScope? belowScope = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new Builder(above =>
        {
            aboveScope = above.DependOnInherited<BaseScope>();
            return new BaseScope(
                value: 5,
                child: new Builder(below =>
                {
                    belowScope = below.DependOnInherited<BaseScope>();
                    return new SizedBox();
                }));
        }));
        Mount(root, owner);

        Assert.Null(aboveScope);
        Assert.Equal(5, belowScope!.Value);

        root.Unmount();
    }

    [Fact]
    public void InheritedScope_MapIsSharedWithDescendantsUntilANewScopeIsIntroduced()
    {
        Element? outerReader = null;
        Element? innerReader = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new BaseScope(
            value: 1,
            child: new Builder(context =>
            {
                outerReader = (Element)context;
                return new BaseScope(
                    value: 2,
                    child: new Builder(innerContext =>
                    {
                        innerReader = (Element)innerContext;
                        return new SizedBox();
                    }));
            })));
        Mount(root, owner);

        ImmutableDictionary<Type, InheritedElement> outerMap = outerReader!.InheritedElements!;
        ImmutableDictionary<Type, InheritedElement> innerMap = innerReader!.InheritedElements!;

        // A plain element shares its parent's map instance verbatim; only an InheritedElement adds
        // an entry, and the outer map keeps observing the outer scope after that.
        Assert.Same(outerMap, outerReader.Parent!.InheritedElements);
        Assert.NotSame(outerMap, innerMap);
        Assert.Equal(1, ((BaseScope)outerMap[typeof(BaseScope)].Widget).Value);
        Assert.Equal(2, ((BaseScope)innerMap[typeof(BaseScope)].Widget).Value);

        root.Unmount();
    }

    [DebugOnlyFact]
    public void InheritedScope_IsDroppedWhenTheElementLeavesTheTree()
    {
        Element? reader = null;

        var owner = new BuildOwner();
        var root = new TestRootElement(new BaseScope(value: 1, child: new Builder(context =>
        {
            reader = (Element)context;
            return new SizedBox();
        })));
        Mount(root, owner);

        Assert.NotNull(reader!.InheritedElements);

        root.Update(new BaseScope(value: 1, child: new SizedBox()));
        owner.FlushBuild();

        Assert.Null(reader.InheritedElements);
        Assert.Throws<FlutterError>(() => reader.DependOnInherited<BaseScope>());
        Assert.Throws<FlutterError>(
            () => reader.GetElementForInheritedWidgetOfExactType<BaseScope>());

        root.Unmount();
    }

    [Fact]
    public void InheritedScope_IsRebuiltFromTheNewParentWhenAnElementIsReparented()
    {
        var probeKey = new LabeledGlobalKey<ScopeProbeState>("probe");
        var probe = new ScopeProbe(key: probeKey);
        var owner = new BuildOwner();
        var root = new TestRootElement(new BaseScope(value: 1, child: new Column(children: [
            new BaseScope(value: 2, child: probe),
            new SizedBox()
        ])));
        Mount(root, owner);

        Assert.Equal(2, probe.BaseScope!.Value);

        // The probe keeps its element through the global key, so its scope map must be rebuilt from
        // the new parent chain on activation rather than kept from the old one.
        root.Update(new BaseScope(value: 1, child: new Column(children: [
            new SizedBox(),
            new BaseScope(value: 3, child: probe)
        ])));
        owner.FlushBuild();

        Assert.Equal(3, probe.BaseScope!.Value);

        root.Unmount();
    }

    [Fact]
    public void DependOnInherited_WhenUnsatisfied_RebuildsOnceTheScopeAppears()
    {
        var probeKey = new LabeledGlobalKey<ScopeProbeState>("probe");
        var probe = new ScopeProbe(key: probeKey);
        var owner = new BuildOwner();
        var root = new TestRootElement(new Column(children: [probe]));
        Mount(root, owner);

        Assert.Null(probe.BaseScope);
        Assert.Equal(1, probe.BuildCount);

        root.Update(new BaseScope(value: 9, child: new Column(children: [probe])));
        owner.FlushBuild();

        Assert.Equal(9, probe.BaseScope!.Value);

        root.Unmount();
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();
    }

    private class BaseScope : InheritedWidget
    {
        public BaseScope(int value, Widget child, Key? key = null) : base(child, key)
        {
            Value = value;
        }

        public int Value { get; }

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Value != ((BaseScope)oldWidget).Value;
        }
    }

    private sealed class DerivedScope : BaseScope
    {
        public DerivedScope(int value, Widget child, Key? key = null) : base(value, child, key)
        {
        }
    }

    private class BaseHolder : StatelessWidget
    {
        public BaseHolder(Widget child, Key? key = null) : base(key)
        {
            Child = child;
        }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;
    }

    private sealed class DerivedHolder : BaseHolder
    {
        public DerivedHolder(Widget child, Key? key = null) : base(child, key)
        {
        }
    }

    private sealed class ScopeProbe : StatefulWidget
    {
        public ScopeProbe(Key? key = null) : base(key)
        {
        }

        public BaseScope? BaseScope { get; set; }

        public DerivedScope? DerivedScope { get; set; }

        public int BuildCount { get; set; }

        public override State CreateState() => new ScopeProbeState();
    }

    private sealed class ScopeProbeState : State
    {
        public override Widget Build(BuildContext context)
        {
            var probe = (ScopeProbe)StateWidget;
            probe.BuildCount++;
            probe.BaseScope = context.DependOnInherited<BaseScope>();
            probe.DerivedScope = context.DependOnInherited<DerivedScope>();
            return new SizedBox();
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
