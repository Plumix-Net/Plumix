using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/framework.dart (BuildScope, BuildOwner.buildScope,
//   BuildOwner.scheduleBuildFor, Element.buildScope/_updateBuildScopeRecursively/_sort)
// flutter/packages/flutter/lib/src/widgets/layout_builder.dart (_LayoutBuilderElement)
// Mirrors flutter/packages/flutter/test/widgets/build_scope_test.dart, framework_test.dart and
// layout_builder_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class BuildScopeTests
{
    [Fact]
    public void BuildScope_ExposesItsScheduleRebuildCallback()
    {
        Assert.Null(new BuildScope().ScheduleRebuild);

        int calls = 0;
        Action rebuild = () => calls++;
        var scope = new BuildScope(rebuild);

        Assert.Same(rebuild, scope.ScheduleRebuild);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Element_BuildScope_IsTheOwnerRootScopeAtTheRootAndInheritedByEveryDescendant()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Probe("a"));
        Mount(root, owner);

        Assert.Same(owner.RootBuildScope, root.BuildScope);
        Assert.Same(owner.RootBuildScope, root.ChildElement!.BuildScope);
        Assert.Same(owner.RootBuildScope, root.ChildElement!.RenderObjectAttachingChild!.BuildScope);
    }

    [Fact]
    public void BuildScope_SegregatesDirtyElementsFromTheAmbientBuild()
    {
        var scope = new BuildScope();
        var owner = new BuildOwner();
        ProbeState? inner = null;
        var root = new TestRootElement(new Scoped(scope, new Probe("inner", state => inner = state)));
        Mount(root, owner);

        Assert.Equal(1, inner!.Builds);
        Assert.Same(scope, inner.Element.BuildScope);

        inner.Bump();
        Assert.True(inner.Element.Dirty);

        // The ambient build pass owns a different scope, so it must not touch this element.
        owner.FlushBuild();
        Assert.True(inner.Element.Dirty);
        Assert.Equal(1, inner.Builds);

        owner.BuildScope(inner.Element);
        Assert.False(inner.Element.Dirty);
        Assert.Equal(2, inner.Builds);
    }

    [Fact]
    public void ScheduleRebuild_FiresOnceWhileTheScopeIsIdleAndNeverWhileItIsBuilding()
    {
        int scheduled = 0;
        var scope = new BuildScope(() => scheduled++);
        var owner = new BuildOwner();
        ProbeState? first = null;
        ProbeState? second = null;
        var root = new TestRootElement(new Scoped(
            scope,
            new Pair(
                new Probe("first", state => first = state),
                new Probe("second", state => second = state))));
        Mount(root, owner);

        Assert.Equal(0, scheduled);

        first!.Bump();
        second!.Bump();
        Assert.Equal(1, scheduled);

        // Cleared by the flush, so the next dirty element asks again.
        owner.BuildScope(root.ChildElement!);
        first.Bump();
        Assert.Equal(2, scheduled);
    }

    [Fact]
    public void BuildScope_RebuildsParentsBeforeChildrenAndEachDirtyElementExactlyOnce()
    {
        var owner = new BuildOwner();
        var order = new List<string>();
        ProbeState? outer = null;
        ProbeState? middle = null;
        ProbeState? deep = null;
        var root = new TestRootElement(new Probe(
            "outer",
            state => outer = state,
            order,
            new Probe("middle", state => middle = state, order, new Probe("deep", state => deep = state, order))));
        Mount(root, owner);
        order.Clear();

        // Dirtied deepest first; the flush must still walk them shallowest first.
        deep!.Bump();
        middle!.Bump();
        outer!.Bump();
        owner.FlushBuild();

        Assert.Equal(["outer", "middle", "deep"], order);
        Assert.Equal(2, outer.Builds);
        Assert.Equal(2, middle.Builds);
        Assert.Equal(2, deep.Builds);
    }

    [Fact]
    public void BuildScope_ReSortsAndRewindsWhenAnAncestorDirtiesADescendantDuringTheFlush()
    {
        var owner = new BuildOwner();
        var order = new List<string>();
        ProbeState? parent = null;
        ProbeState? child = null;
        var root = new TestRootElement(new Probe(
            "parent",
            state => parent = state,
            order,
            new Probe("child", state => child = state, order)));
        Mount(root, owner);
        order.Clear();

        // Only the parent is queued; it dirties the child while it is being rebuilt, which lands
        // behind the cursor and is only reached because the flush re-sorts and rewinds.
        parent!.OnBuild = () => child!.Bump();
        parent.Bump();
        owner.FlushBuild();

        Assert.Equal(["parent", "child"], order);
        Assert.False(child!.Element.Dirty);
    }

    [Fact]
    public void BuildScope_ReportsAThrowingDirtyElementAndContinuesTheFlush()
    {
        var owner = new BuildOwner();
        ThrowAfterBuildElement? failing = null;
        ProbeState? later = null;
        var root = new TestRootElement(new ThrowAfterBuild(
            element => failing = element,
            new Probe("later", state => later = state)));
        Mount(root, owner);

        var failure = new InvalidOperationException("dirty rebuild failed");
        failing!.Failure = failure;
        failing.MarkNeedsBuild();
        later!.Bump();

        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            owner.FlushBuild();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(reported);
        Assert.Same(failure, details.Exception);
        Assert.Equal("widgets library", details.Library);
        Assert.Equal("while rebuilding dirty elements", details.Context!.ToString());

        List<DiagnosticsNode> information = [.. details.InformationCollector!()];
        if (Constants.KDebugMode)
        {
            var creator = Assert.IsType<DiagnosticsDebugCreator>(information[0]);
            Assert.Same(failing, Assert.IsType<DebugCreator>(creator.Value).Element);
        }

        Assert.Contains(
            information,
            node => node.ToString().Contains("The element being rebuilt at the time was", StringComparison.Ordinal));
        Assert.Equal(2, later.Builds);
        Assert.False(later.Element.Dirty);
        Assert.Empty(owner.RootBuildScope.DirtyElements);
    }

    [Fact]
    public void BuildScope_WithNoCallbackAndAnEmptyDirtyList_ReturnsWithoutBuilding()
    {
        var owner = new BuildOwner();
        ProbeState? probe = null;
        var root = new TestRootElement(new Probe("a", state => probe = state));
        Mount(root, owner);

        int builds = probe!.Builds;
        owner.BuildScope(root);

        Assert.Equal(builds, probe.Builds);
        Assert.False(owner.DebugBuilding);
    }

    [Fact]
    public void BuildScope_IsNotReentrant()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Probe("a"));
        Mount(root, owner);

        Assert.Throws<InvalidOperationException>(
            () => owner.BuildScope(root, () => owner.BuildScope(root, () => { })));
    }

    [Fact]
    public void ScheduleBuild_OnAnElementThatIsNotDirty_ReportsTheDartError()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new Probe("a"));
        Mount(root, owner);

        Element child = root.ChildElement!;
        Assert.False(child.Dirty);

        FlutterError error = Assert.Throws<FlutterError>(() => owner.ScheduleBuild(child));
        Assert.Contains("scheduleBuildFor() called for a widget that is not marked as dirty.", error.Message);
        Assert.Contains("Make sure to set the dirty flag before calling scheduleBuildFor().", error.Message);
    }

    [Fact]
    public void BuildScope_DirtyElementOutsideTheScopeRoot_ReportsTheDartError()
    {
        var owner = new BuildOwner();
        ProbeState? first = null;
        ProbeState? second = null;
        var root = new TestRootElement(new Pair(
            new Probe("first", state => first = state),
            new Probe("second", state => second = state)));
        Mount(root, owner);

        // `second` shares the root scope but is not under `first`, so a build scope rooted at `first`
        // has a dirty element it cannot legally rebuild.
        second!.Bump();

        FlutterError error = Assert.Throws<FlutterError>(() => owner.BuildScope(first!.Element));
        Assert.Contains("Tried to build dirty widget in the wrong build scope.", error.Message);
        Assert.Contains("The root of the build scope was", error.Message);
        Assert.Contains("does not appear to be a descendant of the root of the build scope", error.Message);
    }

    [Fact]
    public void MarkNeedsBuild_DuringBuild_RejectsAnElementOutsideTheSubtreeBeingBuilt()
    {
        var owner = new BuildOwner();
        ProbeState? middle = null;
        ProbeState? deep = null;
        var root = new TestRootElement(new Probe(
            "outer",
            _ => { },
            child: new Probe(
                "middle",
                state => middle = state,
                child: new Probe("deep", state => deep = state))));
        Mount(root, owner);

        // Dirtying a shallower element from a deeper element's build is what Dart's markNeedsBuild
        // check forbids: the cursor has already cleaned `deep`, so the rewind stops at it and never
        // reaches `middle` again. Without the check this is exactly how the closing
        // `buildScope missed some dirty elements` assert would be reached.
        bool once = true;
        deep!.OnBuild = () =>
        {
            if (once)
            {
                once = false;
                middle!.Bump();
            }
        };
        deep.Bump();

        // `ComponentElement.PerformRebuild` reports a failing build rather than letting it escape.
        FlutterError error = BuildErrors.Throws<FlutterError>(owner.FlushBuild);
        Assert.Contains("setState() or markNeedsBuild() called during build.", error.Message);
        Assert.Contains("the framework is already in the process of building widgets", error.Message);
        Assert.Contains("The widget on which setState() or markNeedsBuild() was called was", error.Message);
        Assert.Contains(
            "The widget which was currently being built when the offending call was made was",
            error.Message);
    }

    [Fact]
    public void MarkNeedsBuild_DuringBuild_AllowsADescendantOfTheElementBeingBuilt()
    {
        var owner = new BuildOwner();
        ProbeState? outer = null;
        ProbeState? deep = null;
        var root = new TestRootElement(new Probe(
            "outer",
            state => outer = state,
            child: new Probe(
                "middle",
                _ => { },
                child: new Probe("deep", state => deep = state))));
        Mount(root, owner);

        int deepBuildsBefore = deep!.Builds;
        bool once = true;
        outer!.OnBuild = () =>
        {
            if (once)
            {
                once = false;
                deep!.Bump();
            }
        };
        outer.Bump();
        owner.FlushBuild();

        // The dirty descendant is reached by the same build, so no error and it did rebuild.
        Assert.True(deep!.Builds > deepBuildsBefore);
    }

    [Fact]
    public void ReparentingADirtyElementIntoAnotherBuildScope_LeavesItForThatScope()
    {
        var scope = new BuildScope();
        var owner = new BuildOwner();
        ProbeState? moved = null;
        var key = new GlobalObjectKey<ProbeState>(new object());
        var root = new TestRootElement(new Mover(key, scope, insideScope: false, state => moved = state));
        Mount(root, owner);

        Assert.Same(owner.RootBuildScope, moved!.Element.BuildScope);
        moved.Bump();
        Assert.Contains(moved.Element, owner.RootBuildScope.DirtyElements);

        // Dart's _updateBuildScopeRecursively runs before activate(), so the pending rebuild lands in
        // the scope the element is moving into and the root scope keeps only a skipped tombstone.
        ProbeState before = moved;
        root.Update(new Mover(key, scope, insideScope: true, state => moved = state));

        Assert.Same(before, moved);
        Assert.Same(scope, moved.Element.BuildScope);
        Assert.Contains(moved.Element, scope.DirtyElements);

        // The reparenting rebuild cleaned the element, but its entry stays in the new scope's list
        // until that scope is flushed. Dirtying it again has to happen inside a build scope: Dart's
        // scheduleBuildFor rejects re-queueing an already-listed element from outside one.
        int builds = moved.Builds;
        owner.BuildScope(root, moved.Bump);
        owner.FlushBuild();
        Assert.Equal(builds, moved.Builds);
        Assert.True(moved.Element.Dirty, "still dirty after the ambient flush");

        Element scopeOwner = moved.Element.Parent!;
        while (!ReferenceEquals(scopeOwner.BuildScope, scope) || ReferenceEquals(scopeOwner.Parent?.BuildScope, scope))
        {
            scopeOwner = scopeOwner.Parent!;
        }

        owner.BuildScope(scopeOwner);
        Assert.Equal(builds + 1, moved.Builds);
        Assert.False(moved.Element.Dirty);
    }

    [Fact]
    public void LayoutBuilder_OwnsItsBuildScopeAndKeepsDescendantsOutOfTheAmbientBuild()
    {
        var owner = new BuildOwner();
        ProbeState? inner = null;
        var root = new TestRootElement(new LayoutBuilder((_, _) => new Probe("inner", state => inner = state)));
        Mount(root, owner);

        var renderObject = (RenderLayoutBuilder)root.ChildElement!.RenderObject!;
        LayoutOnce(renderObject, BoxConstraints.Tight(new Size(100, 100)));

        Element builderElement = root.ChildElement!;
        Assert.NotSame(owner.RootBuildScope, builderElement.BuildScope);
        Assert.Same(builderElement.BuildScope, inner!.Element.BuildScope);
        Assert.Equal(1, inner.Builds);

        inner.Bump();
        owner.FlushBuild();
        Assert.Equal(1, inner.Builds);
    }

    [Fact]
    public void SliverLayoutBuilder_OwnsItsBuildScopeAndFlushesItFromLayout()
    {
        var owner = new BuildOwner();
        ProbeState? inner = null;
        var root = new TestRootElement(new SliverLayoutBuilder((_, _) =>
            new SliverToBoxAdapter(new Probe("inner", state => inner = state))));
        Mount(root, owner);

        var renderObject = (RenderSliverLayoutBuilder)root.ChildElement!.RenderObject!;
        var constraints = new SliverConstraints(
            Axis: Axis.Vertical,
            ScrollOffset: 0,
            RemainingPaintExtent: 80,
            CrossAxisExtent: 100,
            ViewportMainAxisExtent: 80,
            RemainingCacheExtent: 80);
        renderObject.LayoutWithSliverConstraints(constraints);

        Assert.NotSame(owner.RootBuildScope, root.ChildElement!.BuildScope);
        Assert.Same(root.ChildElement!.BuildScope, inner!.Element.BuildScope);
        Assert.Equal(1, inner.Builds);

        owner.BuildScope(root, inner.Bump);
        owner.FlushBuild();
        Assert.Equal(1, inner.Builds);

        renderObject.LayoutWithSliverConstraints(constraints);
        Assert.Equal(2, inner.Builds);
    }

    [Fact]
    public void LayoutBuilder_FlushesADescendantDirtiedBetweenLayoutsEvenWhenTheConstraintsAreUnchanged()
    {
        var owner = new BuildOwner();
        int builderCalls = 0;
        ProbeState? inner = null;
        var root = new TestRootElement(new LayoutBuilder((_, _) =>
        {
            builderCalls++;
            return new Probe("inner", state => inner = state);
        }));
        Mount(root, owner);

        var renderObject = (RenderLayoutBuilder)root.ChildElement!.RenderObject!;
        BoxConstraints constraints = BoxConstraints.Tight(new Size(100, 100));
        LayoutOnce(renderObject, constraints);
        Assert.Equal(1, builderCalls);
        Assert.Equal(1, inner!.Builds);

        owner.BuildScope(root, inner.Bump);
        Assert.True(renderObject.DebugNeedsLayout);
        LayoutOnce(renderObject, constraints);

        // Dart enters `owner.buildScope(this, null)` even when the builder itself has nothing to do,
        // which is the only thing that flushes this scope's dirty list.
        Assert.Equal(1, builderCalls);
        Assert.Equal(2, inner.Builds);
        Assert.False(inner.Element.Dirty);
    }

    [Fact]
    public void LayoutBuilder_RebuildsADescendantOnceWhenBothTheConstraintsAndAnInheritedValueChange()
    {
        var owner = new BuildOwner();
        ProbeState? inner = null;
        var root = new TestRootElement(new Inherited(
            1,
            new LayoutBuilder((_, _) => new Probe("inner", state => inner = state, dependsOnInherited: true))));
        Mount(root, owner);

        RenderLayoutBuilder renderObject = FindLayoutBuilderRenderObject(root);
        LayoutOnce(renderObject, BoxConstraints.Tight(new Size(100, 100)));
        Assert.Equal(1, inner!.Builds);

        root.Update(new Inherited(
            2,
            new LayoutBuilder((_, _) => new Probe("inner", state => inner = state, dependsOnInherited: true))));
        LayoutOnce(renderObject, BoxConstraints.Tight(new Size(120, 100)));

        // Regression for flutter/flutter#146379: the inherited notification and the constraint change
        // must collapse into a single rebuild of the descendant.
        Assert.Equal(2, inner.Builds);
    }

    [Fact]
    public void LayoutBuilder_MarkNeedsBuildOnADescendantWhileIdle_DefersInsteadOfDirtyingTheRenderTree()
    {
        var owner = new BuildOwner();
        ProbeState? inner = null;
        var root = new TestRootElement(new LayoutBuilder((_, _) => new Probe("inner", state => inner = state)));
        Mount(root, owner);

        var renderObject = (RenderLayoutBuilder)root.ChildElement!.RenderObject!;
        LayoutOnce(renderObject, BoxConstraints.Tight(new Size(100, 100)));
        Assert.False(renderObject.DebugNeedsLayout);
        Assert.Equal(SchedulerPhase.Idle, Scheduler.Phase);

        try
        {
            // Pausing the app disables frames, which keeps `ScheduleFrame` from arming the real
            // dispatcher timer; the transient callback is still queued, which is what this test is
            // about.
            Scheduler.HandleAppLifecycleStateChanged(AppLifecycleState.Paused);
            int before = Scheduler.TransientCallbackCount;
            inner!.Bump();

            // Dart's _scheduleRebuild hands the request to the next frame rather than dirtying layout
            // from the idle phase, so a `markNeedsBuild` at rest never marks the render tree.
            Assert.False(renderObject.DebugNeedsLayout);
            Assert.Equal(before + 1, Scheduler.TransientCallbackCount);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeactivationAndUnmount_PreserveDirtyStateUntilTheScopeDropsMembership(bool dirty)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox(width: 1, height: 1));
        Mount(root, owner);
        Element child = root.ChildElement!;
        if (dirty)
        {
            child.MarkNeedsBuild();
        }

        root.DeactivateChild(child);
        Assert.Equal(dirty, child.Dirty);
        Assert.Equal(dirty, child.InDirtyList);
        child.Rebuild(force: true);
        Assert.Equal(dirty, child.Dirty);

        child.UnmountRoot();
        Assert.Equal(dirty, child.Dirty);
        Assert.Equal(dirty, child.InDirtyList);
        owner.FlushBuild();
        Assert.Equal(dirty, child.Dirty);
        Assert.False(child.InDirtyList);
        Assert.Empty(owner.RootBuildScope.DirtyElements);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReactivationInTheSameScope_PreservesCleanElementsAndReusesDirtyEntries(bool dirty)
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new SizedBox(width: 1, height: 1));
        Mount(root, owner);
        Element child = root.ChildElement!;
        if (dirty)
        {
            child.MarkNeedsBuild();
        }

        owner.BuildScope(root, () =>
        {
            root.DeactivateChild(child);
            Assert.Equal(dirty, child.Dirty);
            child.ActivateWithParent(root, null);
            Assert.Equal(dirty, child.Dirty);
            Assert.Equal(dirty, child.InDirtyList);
            Assert.Equal(dirty ? 1 : 0, owner.RootBuildScope.DirtyElements.Count);
        });

        Assert.False(child.Dirty);
        Assert.False(child.InDirtyList);
    }

    [Fact]
    public void InactiveDirtyElement_IsNotRebuiltAndIsQueuedAgainWhenReactivatedAfterAFlush()
    {
        var owner = new BuildOwner();
        ProbeState? probe = null;
        var root = new TestRootElement(new Probe("probe", state => probe = state));
        Mount(root, owner);
        Element child = root.ChildElement!;
        probe!.Bump();
        root.DeactivateChild(child);
        owner.RootBuildScope.FlushDirtyElements(root);

        Assert.Equal(1, probe.Builds);
        Assert.True(child.Dirty);
        Assert.False(child.InDirtyList);

        owner.BuildScope(root, () =>
        {
            child.ActivateWithParent(root, null);
            Assert.Single(owner.RootBuildScope.DirtyElements);
        });
        Assert.Equal(2, probe.Builds);
        Assert.False(child.Dirty);
    }

    [Fact]
    public void Reactivation_NotifiesInheritedDependenciesBeforeStateAndChildrenActivate()
    {
        var owner = new BuildOwner();
        ProbeState? parent = null;
        ProbeState? child = null;
        var root = new TestRootElement(new Inherited(
            1,
            new Probe(
                "parent",
                state => parent = state,
                child: new Probe("child", state => child = state),
                dependsOnInherited: true)));
        Mount(root, owner);
        Element element = parent!.Element;
        Element oldParent = element.Parent!;
        var order = new List<string>();
        parent.OnActivation = () =>
        {
            Assert.True(element.Dirty);
            Assert.True(element.InDirtyList);
            order.Add("parent");
        };
        child!.OnActivation = () => order.Add("child");

        owner.BuildScope(root, () =>
        {
            oldParent.DeactivateChild(element);
            element.ActivateWithParent(oldParent, null);
        });

        Assert.Equal(["parent", "child"], order);
        Assert.Equal(2, parent.Builds);
        Assert.Equal(2, child.Builds);
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();
    }

    private static void LayoutOnce(RenderBox renderObject, BoxConstraints constraints)
    {
        renderObject.Layout(constraints, parentUsesSize: true);
    }

    private static RenderLayoutBuilder FindLayoutBuilderRenderObject(Element root)
    {
        RenderLayoutBuilder? found = null;
        void Visit(Element element)
        {
            found ??= element.RenderObject as RenderLayoutBuilder;
            if (found is null)
            {
                element.VisitChildren(Visit);
            }
        }

        Visit(root);
        return found!;
    }

    private sealed class Probe : StatefulWidget
    {
        public Probe(
            string name,
            Action<ProbeState>? report = null,
            List<string>? order = null,
            Widget? child = null,
            bool dependsOnInherited = false,
            Key? key = null)
            : base(key)
        {
            Name = name;
            Report = report;
            Order = order;
            Child = child;
            DependsOnInherited = dependsOnInherited;
        }

        public string Name { get; }
        public Action<ProbeState>? Report { get; }
        public List<string>? Order { get; }
        public Widget? Child { get; }
        public bool DependsOnInherited { get; }

        public override State CreateState() => new ProbeState();
    }

    private sealed class ProbeState : State
    {
        private int _generation;

        public int Builds { get; private set; }

        public Action? OnBuild { get; set; }

        public Action? OnActivation { get; set; }

        public override void Activate()
        {
            base.Activate();
            OnActivation?.Invoke();
        }

        public void Bump() => SetState(() => _generation++);

        public override Widget Build(BuildContext context)
        {
            var probe = (Probe)StateWidget;
            Builds++;
            probe.Order?.Add(probe.Name);
            probe.Report?.Invoke(this);
            if (probe.DependsOnInherited)
            {
                _ = context.DependOnInherited<InheritedValue>();
            }

            OnBuild?.Invoke();
            return probe.Child ?? new SizedBox(width: 1 + _generation, height: 1);
        }
    }

    private sealed class Scoped(BuildScope scope, Widget child, Key? key = null) : Widget(key)
    {
        public BuildScope Scope { get; } = scope;
        public Widget Child { get; } = child;

        public override Element CreateElement() => new ScopedElement(this);
    }

    private sealed class ScopedElement(Scoped widget) : ComponentElement(widget)
    {
        public override BuildScope BuildScope => ((Scoped)Widget).Scope;

        protected override Widget Build() => ((Scoped)Widget).Child;
    }

    private sealed class ThrowAfterBuild(
        Action<ThrowAfterBuildElement> report,
        Widget child,
        Key? key = null) : Widget(key)
    {
        public Widget Child { get; } = child;

        public override Element CreateElement()
        {
            var element = new ThrowAfterBuildElement(this);
            report(element);
            return element;
        }
    }

    private sealed class ThrowAfterBuildElement(ThrowAfterBuild widget) : ComponentElement(widget)
    {
        public Exception? Failure { get; set; }

        protected override Widget Build() => ((ThrowAfterBuild)Widget).Child;

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            if (Failure is not null)
            {
                Exception failure = Failure;
                Failure = null;
                throw failure;
            }
        }
    }

    private sealed class Pair(Widget first, Widget second, Key? key = null) : StatelessWidget(key)
    {
        public override Widget Build(BuildContext context) =>
            new Column(children: [first, second]);
    }

    private sealed class InheritedValue(int value, Widget child) : InheritedWidget(child)
    {
        public int Value { get; } = value;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
            Value != ((InheritedValue)oldWidget).Value;
    }

    private sealed class Inherited(int value, Widget child, Key? key = null) : StatelessWidget(key)
    {
        public override Widget Build(BuildContext context) => new InheritedValue(value, child);
    }

    /// <summary>Moves one global-keyed child between the ambient tree and a scope-owning subtree.</summary>
    private sealed class Mover(
        GlobalObjectKey<ProbeState> key,
        BuildScope scope,
        bool insideScope,
        Action<ProbeState> report)
        : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            var probe = new Probe("moved", report, key: key);
            return insideScope
                ? new Column(children: [new SizedBox(width: 1, height: 1), new Scoped(scope, probe)])
                : new Column(children: [probe, new SizedBox(width: 1, height: 1)]);
        }
    }

    private sealed class TestRootElement(Widget widget) : Element(widget), IRenderObjectHost
    {
        private Element? _child;

        public Element? ChildElement => _child;

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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
