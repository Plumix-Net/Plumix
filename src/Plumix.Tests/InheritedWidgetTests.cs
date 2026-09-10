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
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
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
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
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

    [Fact]
    public void InheritedWidget_IsAProxyWidgetThatCarriesItsChild()
    {
        // Dart's `InheritedWidget extends ProxyWidget`, so the child is the `ProxyWidget.child`
        // field rather than something the widget builds.
        var child = new SizedBox(width: 1, height: 1);
        var scope = new IntScope(value: 1, child: child);

        Assert.IsAssignableFrom<ProxyWidget>(scope);
        Assert.Same(child, scope.Child);
        Assert.Same(child, ((ProxyWidget)scope).Child);
    }

    [Fact]
    public void InheritedElement_IsAProxyElement()
    {
        // Dart's `InheritedElement extends ProxyElement`; `ParentDataElement` is the sibling that
        // shares the same base.
        Element element = new IntScope(value: 1, child: new SizedBox()).CreateElement();

        Assert.IsType<InheritedElement>(element);
        Assert.IsAssignableFrom<ProxyElement>(element);
        Assert.IsAssignableFrom<ComponentElement>(element);
    }

    [Fact]
    public void InheritedWidget_ChildOverride_IsWhatTheElementInflates()
    {
        // Dart's `TextSelectionTheme` overrides the `child` getter to slip a wrapper into the
        // subtree without changing its constructor; `ProxyElement.build` reads the getter, so the
        // wrapper is what gets inflated.
        var owner = new BuildOwner();
        var leaf = new SizedBox(width: 3, height: 4);

        var root = new TestRootElement(new WrappingScope(value: 1, child: leaf));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        var scopeElement = (InheritedElement)FirstChildOf(root)!;
        Element wrapper = FirstChildOf(scopeElement)!;
        Assert.IsType<Center>(wrapper.Widget);
        Assert.Same(leaf, FirstChildOf(wrapper)!.Widget);

        root.UnmountRoot();
    }

    [Fact]
    public void InheritedWidget_UpdateShouldNotifyFalse_StillRebuildsTheChild()
    {
        // `ProxyElement.update` calls `rebuild(force: true)` whether or not `updated` notified the
        // dependents, so a new child instance is inflated even when nothing depends on the scope.
        var owner = new BuildOwner();
        var firstChild = new SizedBox(width: 1, height: 1);

        var root = new TestRootElement(new ConditionalScope(value: 1, shouldNotify: false, child: firstChild));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        var scopeElement = (InheritedElement)FirstChildOf(root)!;
        Assert.Same(firstChild, FirstChildOf(scopeElement)!.Widget);

        var secondChild = new SizedBox(width: 2, height: 2);
        root.Update(new ConditionalScope(value: 2, shouldNotify: false, child: secondChild));
        owner.FlushBuild();

        Assert.Same(secondChild, FirstChildOf(scopeElement)!.Widget);

        root.UnmountRoot();
    }

    private static Element? FirstChildOf(Element element)
    {
        Element? first = null;
        element.VisitChildren(child => first ??= child);
        return first;
    }

    private sealed class WrappingScope : InheritedWidget
    {
        private readonly Widget _child;

        public WrappingScope(int value, Widget child) : base(child)
        {
            Value = value;
            _child = child;
        }

        public int Value { get; }

        public override Widget Child => new Center(child: _child);

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Value != ((WrappingScope)oldWidget).Value;
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
        public IntScope(int value, Widget child) : base(child)
        {
            Value = value;
        }

        public int Value { get; }

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Value != ((IntScope)oldWidget).Value;
        }
    }

    private sealed class ConditionalScope : InheritedWidget
    {
        public ConditionalScope(int value, bool shouldNotify, Widget child) : base(child)
        {
            Value = value;
            ShouldNotify = shouldNotify;
        }

        public int Value { get; }

        public bool ShouldNotify { get; }

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
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
