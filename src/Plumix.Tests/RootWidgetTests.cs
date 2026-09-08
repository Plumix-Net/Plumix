using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/binding.dart (RootWidget, RootElement,
//   WidgetsBinding.attachRootWidget/attachToBuildOwner)
// flutter/packages/flutter/lib/src/widgets/framework.dart (RootElementMixin.assignOwner)
// Mirrors flutter/packages/flutter/test/widgets/binding_test.dart and root_widget_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class RootWidgetTests
{
    [Fact]
    public void RootWidget_Attach_MountsTheElementAndBuildsItsChild()
    {
        var owner = new BuildOwner();
        var probe = new Probe("a");
        RootElement root = new RootWidget(child: probe).Attach(owner);

        Assert.Same(owner, root.Owner);
        Assert.Null(root.Parent);
        Assert.True(root.IsActive);
        Assert.False(root.Dirty);
        Assert.NotNull(root.ChildElement);
        Assert.Same(probe, root.ChildElement!.Widget);
    }

    [Fact]
    public void RootWidget_Attach_PutsTheRootInTheOwnerRootBuildScope()
    {
        var owner = new BuildOwner();
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);

        Assert.Same(owner.RootBuildScope, root.BuildScope);
        Assert.Same(owner.RootBuildScope, root.ChildElement!.BuildScope);
    }

    [Fact]
    public void RootWidget_Attach_MountsInsideALockedBuildScope()
    {
        var owner = new BuildOwner();
        bool? lockedDuringMount = null;
        bool? buildingDuringMount = null;
        Element? targetDuringMount = null;

        RootElement root = new RootWidget(child: new Probe("a", onBuild: state =>
        {
            BuildOwner stateOwner = state.Element.Owner!;
            lockedDuringMount = stateOwner.DebugStateLocked;
            buildingDuringMount = stateOwner.DebugBuilding;
            targetDuringMount = stateOwner.DebugCurrentBuildTarget;
        })).Attach(owner);

        Assert.True(lockedDuringMount);
        Assert.True(buildingDuringMount);
        Assert.NotNull(targetDuringMount);
        Assert.False(owner.DebugStateLocked);
        Assert.False(owner.DebugBuilding);
        Assert.NotNull(root.ChildElement);
    }

    [Fact]
    public void RootWidget_Attach_WithAnExistingElement_ParksTheWidgetUntilTheNextBuild()
    {
        var owner = new BuildOwner();
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);
        Element firstChild = root.ChildElement!;

        RootElement again = new RootWidget(child: new Probe("b")).Attach(owner, root);

        // The root is not remounted; it is dirtied and the swap waits for the flush.
        Assert.Same(root, again);
        Assert.True(root.Dirty);
        Assert.Same(firstChild, root.ChildElement);

        owner.FlushBuild();

        Assert.False(root.Dirty);
        Assert.Same(firstChild, root.ChildElement);
        Assert.Equal("b", ((Probe)root.ChildElement!.Widget).Name);
    }

    [Fact]
    public void RootWidget_Attach_WithAnExistingElement_AsksTheOwnerForABuild()
    {
        var owner = new BuildOwner();
        int scheduled = 0;
        owner.OnBuildScheduled = () => scheduled++;
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);
        scheduled = 0;

        new RootWidget(child: new Probe("b")).Attach(owner, root);

        Assert.Equal(1, scheduled);
    }

    [Fact]
    public void RootElement_ReplacesTheChildElementWhenTheChildWidgetTypeChanges()
    {
        var owner = new BuildOwner();
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);
        Element firstChild = root.ChildElement!;

        new RootWidget(child: new SizedBox(width: 3, height: 4)).Attach(owner, root);
        owner.FlushBuild();

        Assert.NotSame(firstChild, root.ChildElement);
        Assert.IsType<SizedBox>(root.ChildElement!.Widget);
    }

    [Fact]
    public void RootWidget_ToStringShort_UsesTheDebugShortDescription()
    {
        Assert.Equal("[root]", new RootWidget(debugShortDescription: "[root]").ToStringShort());
        Assert.Equal("RootWidget", new RootWidget().ToStringShort());
    }

    [Fact]
    public void RootElement_PublishesItsChildRenderObjectToTheHost()
    {
        var owner = new BuildOwner();
        var host = new RecordingHost();
        RootElement root = new RootWidget(
            child: new SizedBox(width: 5, height: 6),
            renderObjectHost: host).Attach(owner);

        Assert.NotNull(host.Child);
        Assert.Same(root.ChildElement!.RenderObject, host.Child);
        Assert.Same(host.Child, root.RenderObject);
    }

    [Fact]
    public void RootElement_ClearsTheHostWhenTheChildRenderObjectGoesAway()
    {
        var owner = new BuildOwner();
        var host = new RecordingHost();
        RootElement root = new RootWidget(
            child: new SizedBox(width: 5, height: 6),
            renderObjectHost: host).Attach(owner);
        Assert.NotNull(host.Child);

        root.Unmount();

        Assert.Null(host.Child);
    }

    [Fact]
    public void RootElement_Mount_RejectsAParent()
    {
        var owner = new BuildOwner();
        RootElement parent = new RootWidget(child: new Probe("a")).Attach(owner);
        var orphan = (RootElement)new RootWidget(child: new Probe("b")).CreateElement();
        orphan.AssignOwner(owner);

        Assert.Throws<AssertionError>(() => orphan.Mount(parent, newSlot: null));
    }

    [Fact]
    public void RootElement_Mount_RejectsARootWidgetWithNoChild()
    {
        var owner = new BuildOwner();
        Assert.Throws<AssertionError>(() => new RootWidget().Attach(owner));
    }

    [Fact]
    public void RootElement_ForgetChild_RejectsAnElementThatIsNotItsChild()
    {
        var owner = new BuildOwner();
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);
        RootElement other = new RootWidget(child: new Probe("b")).Attach(new BuildOwner());

        Assert.Throws<AssertionError>(() => root.ForgetChild(other.ChildElement!));
    }

    [Fact]
    public void RootElement_ReportsABuildFailureInsteadOfSubstitutingAnErrorWidget()
    {
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            var owner = new BuildOwner();
            RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);
            new RootWidget(child: new Thrower()).Attach(owner, root);
            owner.FlushBuild();

            Assert.Null(root.ChildElement);
            FlutterErrorDetails details = Assert.Single(reported);
            Assert.Equal("widgets library", details.Library);
            Assert.Equal("attaching to the render tree", details.Context!.ToDescription());
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    [Fact]
    public void ScheduleBuildFor_RejectsAnElementThatIsNotDirty()
    {
        var owner = new BuildOwner();
        RootElement root = new RootWidget(child: new Probe("a")).Attach(owner);

        FlutterError error = Assert.Throws<FlutterError>(() => owner.ScheduleBuild(root.ChildElement!));
        Assert.StartsWith(
            "scheduleBuildFor() called for a widget that is not marked as dirty.",
            error.Message);
    }

    [Fact]
    public void ScheduleBuildFor_RejectsAnElementThatIsAlreadyInTheDirtyList()
    {
        var owner = new BuildOwner();
        ProbeState? state = null;
        RootElement root = new RootWidget(child: new Probe("a", report: s => state = s)).Attach(owner);
        _ = root;

        state!.Bump();
        Assert.True(state.Element.Dirty);

        FlutterError error = Assert.Throws<FlutterError>(() => owner.ScheduleBuild(state.Element));
        Assert.StartsWith("BuildOwner.scheduleBuildFor() called inappropriately.", error.Message);
        Assert.Contains(
            "already in the dirty list",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ScheduleBuildFor_AcceptsAnElementThatIsAlreadyInTheDirtyListWhileBuilding()
    {
        var owner = new BuildOwner();
        ProbeState? state = null;
        RootElement root = new RootWidget(child: new Probe("a", report: s => state = s)).Attach(owner);

        state!.Bump();
        owner.BuildScope(root, () => owner.ScheduleBuild(state.Element));

        Assert.False(state.Element.Dirty);
    }

    private sealed class RecordingHost : IRootRenderObjectHost
    {
        public RenderObject? Child { get; private set; }

        public void AttachRootRenderObject(RenderObject? child) => Child = child;
    }

    // ComponentElement.PerformRebuild catches a throwing Build() and substitutes an ErrorWidget, so
    // reaching RootElement's own catch takes a widget that fails while it is being inflated.
    private sealed class Thrower : Widget
    {
        public override Element CreateElement() => throw new InvalidOperationException("boom");
    }

    private sealed class Probe : StatefulWidget
    {
        public Probe(string name, Action<ProbeState>? report = null, Action<ProbeState>? onBuild = null)
        {
            Name = name;
            Report = report;
            OnBuild = onBuild;
        }

        public string Name { get; }
        public Action<ProbeState>? Report { get; }
        public Action<ProbeState>? OnBuild { get; }

        public override State CreateState() => new ProbeState();
    }

    private sealed class ProbeState : State
    {
        private int _generation;

        public Element Element => (Element)Context;

        public void Bump() => SetState(() => _generation++);

        public override Widget Build(BuildContext context)
        {
            var probe = (Probe)StateWidget;
            probe.Report?.Invoke(this);
            probe.OnBuild?.Invoke(this);
            return new SizedBox(width: 1 + _generation, height: 1);
        }
    }
}
