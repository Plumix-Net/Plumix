using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/rendering/pipeline_owner_tree_test.dart

namespace Plumix.Tests;

/// <summary>
/// Parity coverage for the <see cref="PipelineOwner"/> tree, the <see cref="PipelineManifold"/> and
/// the on-demand semantics-owner lifecycle, ported from Flutter's `pipeline_owner_tree_test.dart`.
/// </summary>
public sealed class PipelineOwnerTreeTests
{
    private static PipelineOwner NewOwner(
        Action? onNeedVisualUpdate = null,
        Action? onSemanticsOwnerCreated = null,
        Action? onSemanticsOwnerDisposed = null)
    {
        return new PipelineOwner(
            onNeedVisualUpdate: onNeedVisualUpdate,
            onSemanticsOwnerCreated: onSemanticsOwnerCreated,
            onSemanticsUpdate: static _ => { },
            onSemanticsOwnerDisposed: onSemanticsOwnerDisposed);
    }

    [Fact]
    public void OnNeedVisualUpdate_TakesPrecedenceOverManifold()
    {
        int manifoldCount = 0;
        var manifold = new HostPipelineManifold(() => manifoldCount += 1);

        int rootCount = 0;
        var rootRenderObject = new TestRenderObject();
        PipelineOwner root = NewOwner(onNeedVisualUpdate: () => rootCount += 1);
        root.RootNode = rootRenderObject;
        rootRenderObject.ScheduleInitialLayout();

        int child1Count = 0;
        var child1RenderObject = new TestRenderObject();
        PipelineOwner child1 = NewOwner(onNeedVisualUpdate: () => child1Count += 1);
        child1.RootNode = child1RenderObject;
        child1RenderObject.ScheduleInitialLayout();

        var child2RenderObject = new TestRenderObject();
        PipelineOwner child2 = NewOwner();
        child2.RootNode = child2RenderObject;
        child2RenderObject.ScheduleInitialLayout();

        root.AdoptChild(child1);
        root.AdoptChild(child2);
        root.Attach(manifold);
        root.FlushLayout();
        manifoldCount = 0;
        rootCount = 0;
        child1Count = 0;

        rootRenderObject.MarkNeedsLayout();
        Assert.Equal(0, manifoldCount);
        Assert.True(rootCount > 0);
        Assert.Equal(0, child1Count);

        child1RenderObject.MarkNeedsLayout();
        Assert.Equal(0, manifoldCount);
        Assert.True(child1Count > 0);

        // child2 has no callback of its own, so it falls back to the manifold it shares with the tree.
        child2RenderObject.MarkNeedsLayout();
        Assert.True(manifoldCount > 0);
    }

    [Fact]
    public void FlushLayout_LaysOutParentBeforeChild()
    {
        var manifold = new HostPipelineManifold();
        var log = new List<string>();

        var rootRenderObject = new TestRenderObject(onLayout: () => log.Add("layout parent"));
        PipelineOwner root = NewOwner();
        root.RootNode = rootRenderObject;
        rootRenderObject.ScheduleInitialLayout();

        var childRenderObject = new TestRenderObject(onLayout: () => log.Add("layout child"));
        PipelineOwner child = NewOwner();
        child.RootNode = childRenderObject;
        childRenderObject.ScheduleInitialLayout();

        root.AdoptChild(child);
        root.Attach(manifold);
        Assert.Empty(log);

        root.FlushLayout();
        Assert.Equal(["layout parent", "layout child"], log);
    }

    [Fact]
    public void FlushCompositingBits_UpdatesBitsOnChildren()
    {
        var manifold = new HostPipelineManifold();

        var rootRenderObject = new TestRenderObject();
        PipelineOwner root = NewOwner();
        root.RootNode = rootRenderObject;
        rootRenderObject.MarkNeedsCompositingBitsUpdate();

        var childRenderObject = new TestRenderObject();
        PipelineOwner child = NewOwner();
        child.RootNode = childRenderObject;
        childRenderObject.MarkNeedsCompositingBitsUpdate();

        root.AdoptChild(child);
        root.Attach(manifold);

        root.FlushCompositingBits();
        Assert.False(rootRenderObject.NeedsCompositingBitsUpdate);
        Assert.False(childRenderObject.NeedsCompositingBitsUpdate);
    }

    [Fact]
    public void FlushPaint_PaintsParentBeforeChild()
    {
        var manifold = new HostPipelineManifold();
        var log = new List<string>();

        var rootView = new RenderView { Child = new LoggingRenderBox(() => log.Add("paint parent")) };
        var root = new PipelineOwner(rootView);
        root.Attach(rootView);

        var childView = new RenderView { Child = new LoggingRenderBox(() => log.Add("paint child")) };
        var child = new PipelineOwner(childView);
        child.Attach(childView);

        root.AdoptChild(child);
        root.Attach(manifold);
        root.FlushLayout(new Size(100, 100));
        root.FlushCompositingBits();
        log.Clear();

        root.FlushPaint();
        Assert.Equal(["paint parent", "paint child"], log);
    }

    [Fact]
    public void FlushSemantics_RunsParentBeforeChild()
    {
        var manifold = new HostPipelineManifold(semanticsEnabled: true);
        var log = new List<string>();

        var rootRenderObject = new TestRenderObject(onSemantics: () => log.Add("semantics parent"));
        PipelineOwner root = NewOwner(
            onSemanticsOwnerCreated: () => rootRenderObject.ScheduleInitialSemantics());
        root.RootNode = rootRenderObject;

        var childRenderObject = new TestRenderObject(onSemantics: () => log.Add("semantics child"));
        PipelineOwner child = NewOwner(
            onSemanticsOwnerCreated: () => childRenderObject.ScheduleInitialSemantics());
        child.RootNode = childRenderObject;

        root.AdoptChild(child);
        root.Attach(manifold);
        rootRenderObject.ScheduleInitialLayout();
        childRenderObject.ScheduleInitialLayout();
        root.FlushLayout();
        log.Clear();

        rootRenderObject.MarkNeedsSemanticsUpdate();
        childRenderObject.MarkNeedsSemanticsUpdate();
        root.FlushSemantics();

        Assert.Equal(["semantics parent", "semantics child"], log);
    }

    [Fact]
    public void ManifoldSemantics_CreatesAndDisposesOwnersAcrossTree()
    {
        var manifold = new HostPipelineManifold();

        int rootCreated = 0;
        int rootDisposed = 0;
        PipelineOwner root = NewOwner(
            onSemanticsOwnerCreated: () => rootCreated += 1,
            onSemanticsOwnerDisposed: () => rootDisposed += 1);

        int childCreated = 0;
        int childDisposed = 0;
        PipelineOwner child = NewOwner(
            onSemanticsOwnerCreated: () => childCreated += 1,
            onSemanticsOwnerDisposed: () => childDisposed += 1);

        root.AdoptChild(child);
        root.Attach(manifold);
        Assert.Equal(0, rootCreated);
        Assert.Equal(0, childCreated);
        Assert.Null(root.SemanticsOwner);
        Assert.Null(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(true);
        Assert.Equal(1, rootCreated);
        Assert.Equal(1, childCreated);
        Assert.Equal(0, rootDisposed);
        Assert.Equal(0, childDisposed);
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(1, rootCreated);
        Assert.Equal(1, childCreated);
        Assert.Equal(1, rootDisposed);
        Assert.Equal(1, childDisposed);
        Assert.Null(root.SemanticsOwner);
        Assert.Null(child.SemanticsOwner);
    }

    [Fact]
    public void ManifoldSemantics_OnlyCreatesOwnersThatDoNotHaveOne()
    {
        var manifold = new HostPipelineManifold();

        int rootCreated = 0;
        int rootDisposed = 0;
        PipelineOwner root = NewOwner(
            onSemanticsOwnerCreated: () => rootCreated += 1,
            onSemanticsOwnerDisposed: () => rootDisposed += 1);

        int childCreated = 0;
        int childDisposed = 0;
        PipelineOwner child = NewOwner(
            onSemanticsOwnerCreated: () => childCreated += 1,
            onSemanticsOwnerDisposed: () => childDisposed += 1);

        root.AdoptChild(child);
        root.Attach(manifold);

        SemanticsHandle childSemantics = child.EnsureSemantics();
        Assert.Equal(0, rootCreated);
        Assert.Equal(1, childCreated);
        Assert.Null(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(true);
        Assert.Equal(1, rootCreated);
        Assert.Equal(1, childCreated);
        Assert.Equal(0, rootDisposed);
        Assert.Equal(0, childDisposed);
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(1, rootDisposed);
        Assert.Equal(0, childDisposed);
        Assert.Null(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        childSemantics.Dispose();
        Assert.Equal(1, childDisposed);
        Assert.Null(child.SemanticsOwner);
    }

    [Fact]
    public void LocalSemanticsHandle_CanBeDisposedWhileManifoldForcesSemanticsOn()
    {
        var manifold = new HostPipelineManifold();

        int rootCreated = 0;
        int rootDisposed = 0;
        PipelineOwner root = NewOwner(
            onSemanticsOwnerCreated: () => rootCreated += 1,
            onSemanticsOwnerDisposed: () => rootDisposed += 1);

        int childCreated = 0;
        int childDisposed = 0;
        PipelineOwner child = NewOwner(
            onSemanticsOwnerCreated: () => childCreated += 1,
            onSemanticsOwnerDisposed: () => childDisposed += 1);

        root.AdoptChild(child);
        root.Attach(manifold);

        SemanticsHandle childSemantics = child.EnsureSemantics();
        manifold.SetSemanticsEnabled(true);

        childSemantics.Dispose();
        Assert.Equal(1, rootCreated);
        Assert.Equal(1, childCreated);
        Assert.Equal(0, rootDisposed);
        Assert.Equal(0, childDisposed);
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(1, rootDisposed);
        Assert.Equal(1, childDisposed);
        Assert.Null(root.SemanticsOwner);
        Assert.Null(child.SemanticsOwner);
    }

    [Fact]
    public void LocalSemanticsHandle_SurvivesManifoldTurningSemanticsOff()
    {
        var manifold = new HostPipelineManifold();

        int rootDisposed = 0;
        PipelineOwner root = NewOwner(onSemanticsOwnerDisposed: () => rootDisposed += 1);

        int childDisposed = 0;
        PipelineOwner child = NewOwner(onSemanticsOwnerDisposed: () => childDisposed += 1);

        root.AdoptChild(child);
        root.Attach(manifold);
        manifold.SetSemanticsEnabled(true);

        SemanticsHandle childSemantics = child.EnsureSemantics();
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(1, rootDisposed);
        Assert.Equal(0, childDisposed);
        Assert.Null(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);

        childSemantics.Dispose();
        Assert.Equal(1, childDisposed);
        Assert.Null(child.SemanticsOwner);
    }

    [Fact]
    public void EnsureSemantics_NotifiesItsListenerUntilTheHandleIsClosed()
    {
        var manifold = new HostPipelineManifold();
        PipelineOwner owner = NewOwner();
        owner.Attach(manifold);

        int notifications = 0;
        SemanticsHandle handle = owner.EnsureSemantics(() => notifications += 1);
        SemanticsOwner semanticsOwner = Assert.IsType<SemanticsOwner>(owner.SemanticsOwner);

        semanticsOwner.NotifyListeners();
        Assert.Equal(1, notifications);

        handle.Dispose();
        Assert.Null(owner.SemanticsOwner);
        semanticsOwner.NotifyListeners();
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void EnsureSemantics_WithoutOnSemanticsUpdate_Throws()
    {
        var owner = new PipelineOwner();
        AssertionError error = Assert.Throws<AssertionError>(() => owner.EnsureSemantics());
        Assert.Contains("onSemanticsUpdate", error.Message);
    }

    [Fact]
    public void Attach_WhenAlreadyAttached_Throws()
    {
        var manifold = new HostPipelineManifold();
        var owner = new PipelineOwner();

        owner.Attach(manifold);
        Assert.Throws<AssertionError>(() => owner.Attach(manifold));
    }

    [Fact]
    public void Attach_UpdatesSemanticsOwner()
    {
        var manifold = new HostPipelineManifold(semanticsEnabled: true);
        PipelineOwner owner = NewOwner();

        Assert.Null(owner.SemanticsOwner);
        owner.Attach(manifold);
        Assert.NotNull(owner.SemanticsOwner);
    }

    [Fact]
    public void Attach_DoesNotRequestVisualUpdateIfNothingIsDirty()
    {
        int manifoldCount = 0;
        var manifold = new HostPipelineManifold(() => manifoldCount += 1);
        var renderObject = new TestRenderObject();
        var owner = new PipelineOwner();
        owner.RootNode = renderObject;

        Assert.Equal(0, manifoldCount);
        owner.Attach(manifold);
        Assert.Equal(0, manifoldCount);
    }

    [Fact]
    public void Detach_WhenNotAttached_Throws()
    {
        var owner = new PipelineOwner();
        Assert.Throws<AssertionError>(owner.Detach);
    }

    [Fact]
    public void AdoptChild_Twice_Throws()
    {
        var root = new PipelineOwner();
        var child = new PipelineOwner();
        root.AdoptChild(child);
        Assert.Throws<AssertionError>(() => root.AdoptChild(child));
    }

    [Fact]
    public void AdoptChild_OfOtherParent_Throws()
    {
        var root = new PipelineOwner();
        var child = new PipelineOwner();
        var otherRoot = new PipelineOwner();
        root.AdoptChild(child);
        Assert.Throws<AssertionError>(() => otherRoot.AdoptChild(child));
    }

    [Fact]
    public void AdoptChild_CreatesSemanticsOwnerIfNecessary()
    {
        var manifold = new HostPipelineManifold();
        PipelineOwner root = NewOwner();
        PipelineOwner child = NewOwner();
        PipelineOwner childOfChild = NewOwner();
        root.Attach(manifold);

        Assert.Null(root.SemanticsOwner);
        root.AdoptChild(child);
        Assert.Null(child.SemanticsOwner);

        manifold.SetSemanticsEnabled(true);
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);
        Assert.Null(childOfChild.SemanticsOwner);

        child.AdoptChild(childOfChild);
        Assert.NotNull(childOfChild.SemanticsOwner);
    }

    [Fact]
    public void DropChild_UnattachedChild_Throws()
    {
        var root = new PipelineOwner();
        var child = new PipelineOwner();
        Assert.Throws<AssertionError>(() => root.DropChild(child));
    }

    [Fact]
    public void DropChild_ChildOfOtherParent_Throws()
    {
        var root = new PipelineOwner();
        var child = new PipelineOwner();
        var otherRoot = new PipelineOwner();
        otherRoot.AdoptChild(child);
        Assert.Throws<AssertionError>(() => root.DropChild(child));
    }

    [Fact]
    public void DropChild_RetainsSemanticsOwnerUntilItIsNoLongerNeeded()
    {
        var manifold = new HostPipelineManifold(semanticsEnabled: true);
        PipelineOwner root = NewOwner();
        PipelineOwner child = NewOwner();
        PipelineOwner childOfChild = NewOwner();
        root.Attach(manifold);
        root.AdoptChild(child);
        child.AdoptChild(childOfChild);

        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(child.SemanticsOwner);
        Assert.NotNull(childOfChild.SemanticsOwner);

        child.DropChild(childOfChild);
        // Retained in case we get re-attached.
        Assert.NotNull(childOfChild.SemanticsOwner);

        SemanticsHandle childSemantics = child.EnsureSemantics();
        root.DropChild(child);
        Assert.NotNull(child.SemanticsOwner);

        childSemantics.Dispose();
        Assert.NotNull(root.SemanticsOwner);
        Assert.Null(child.SemanticsOwner);
        Assert.NotNull(childOfChild.SemanticsOwner);

        manifold.SetSemanticsEnabled(false);
        Assert.Null(root.SemanticsOwner);
        Assert.NotNull(childOfChild.SemanticsOwner);

        root.AdoptChild(childOfChild);
        // Disposed on re-attachment, because semantics are off by then.
        Assert.Null(childOfChild.SemanticsOwner);

        manifold.SetSemanticsEnabled(true);
        Assert.NotNull(root.SemanticsOwner);
        Assert.NotNull(childOfChild.SemanticsOwner);

        root.DropChild(childOfChild);
        Assert.NotNull(childOfChild.SemanticsOwner);

        childOfChild.Dispose();
        Assert.Null(childOfChild.SemanticsOwner);
    }

    [Fact]
    public void FlushLayout_AllowsAdoptingAndDroppingChildrenDuringOwnLayout()
    {
        var manifold = new HostPipelineManifold();

        var root = new PipelineOwner();
        var child1 = new PipelineOwner();
        var child2 = new PipelineOwner();

        var rootRenderObject = new TestRenderObject(onLayout: () =>
        {
            child1.DropChild(child2);
            root.DropChild(child1);
            root.AdoptChild(child2);
            child2.AdoptChild(child1);
        });

        root.RootNode = rootRenderObject;
        rootRenderObject.ScheduleInitialLayout();

        root.AdoptChild(child1);
        child1.AdoptChild(child2);
        root.Attach(manifold);
        Assert.Equal([root, child1, child2], TreeWalk(root));

        root.FlushLayout();
        Assert.Equal([root, child2, child1], TreeWalk(root));
    }

    [Fact]
    public void FlushLayout_RejectsAdoptingAndDroppingChildrenDuringChildLayout()
    {
        var manifold = new HostPipelineManifold();

        var root = new PipelineOwner();
        var child1 = new PipelineOwner();
        var child2 = new PipelineOwner();
        var child3 = new PipelineOwner();

        Exception? droppingError = null;
        Exception? adoptingError = null;

        var childRenderObject = new TestRenderObject(onLayout: () =>
        {
            child1.DropChild(child2);
            child1.AdoptChild(child3);
            droppingError = Record.Exception(() => root.DropChild(child1));
            adoptingError = Record.Exception(() => root.AdoptChild(child2));
        });

        child1.RootNode = childRenderObject;
        childRenderObject.ScheduleInitialLayout();

        root.AdoptChild(child1);
        child1.AdoptChild(child2);
        root.Attach(manifold);
        Assert.Equal([root, child1, child2], TreeWalk(root));

        root.FlushLayout();

        Assert.Contains("Cannot modify child list after layout.", Assert.IsType<AssertionError>(adoptingError).Message);
        Assert.Contains("Cannot modify child list after layout.", Assert.IsType<AssertionError>(droppingError).Message);
    }

    [Fact]
    public void VisitChildren_VisitsAllImmediateChildren()
    {
        var root = new PipelineOwner();
        var child1 = new PipelineOwner();
        var child2 = new PipelineOwner();
        var child3 = new PipelineOwner();
        var childOfChild3 = new PipelineOwner();

        root.AdoptChild(child1);
        root.AdoptChild(child2);
        root.AdoptChild(child3);
        child3.AdoptChild(childOfChild3);

        var children = new List<PipelineOwner>();
        root.VisitChildren(children.Add);
        Assert.Equal([child1, child2, child3], children);

        children.Clear();
        child3.VisitChildren(children.Add);
        Assert.Equal(childOfChild3, Assert.Single(children));
    }

    [Fact]
    public void ToStringDeep_PrintsTheOwnerTree()
    {
        if (!Constants.KDebugMode)
        {
            // Diagnostics elide their bodies outside debug builds, so there is no dump to compare.
            return;
        }

        var root = new PipelineOwner();
        var child1 = new PipelineOwner { RootNode = new TestRenderObject() };
        var childOfChild1 = new PipelineOwner { RootNode = new TestRenderObject() };
        var child2 = new PipelineOwner { RootNode = new TestRenderObject() };

        root.AdoptChild(child1);
        child1.AdoptChild(childOfChild1);
        root.AdoptChild(child2);

        string dump = root.ToStringDeep();
        Assert.StartsWith("PipelineOwner", dump);
        Assert.Contains("├─PipelineOwner", dump);
        Assert.Contains("└─PipelineOwner", dump);
        Assert.Contains("rootNode: TestRenderObject", dump);
        Assert.Equal(4, dump.Split("PipelineOwner").Length - 1);
    }

    [Fact]
    public void RootNode_DetachesTheOldRootAndAttachesTheNewOne()
    {
        var owner = new PipelineOwner();
        var first = new TestRenderObject();
        var second = new TestRenderObject();

        owner.RootNode = first;
        Assert.True(first.Attached);
        Assert.Same(owner, first.Owner);

        owner.RootNode = second;
        Assert.False(first.Attached);
        Assert.True(second.Attached);

        // Assigning the same node again is a no-op rather than a detach/attach cycle.
        owner.RootNode = second;
        Assert.True(second.Attached);

        owner.RootNode = null;
        Assert.False(second.Attached);
    }

    [Fact]
    public void Dispose_RequiresTheOwnerToBeOutOfTheTree()
    {
        var manifold = new HostPipelineManifold();
        var root = new PipelineOwner();
        var child = new PipelineOwner();
        root.AdoptChild(child);

        Assert.Throws<AssertionError>(root.Dispose);
        Assert.Throws<AssertionError>(child.Dispose);

        root.DropChild(child);
        child.Dispose();

        root.Attach(manifold);
        Assert.Throws<AssertionError>(root.Dispose);
        root.Detach();
        root.Dispose();
    }

    [Fact]
    public void Dispose_ReleasesTheSemanticsOwner()
    {
        PipelineOwner owner = NewOwner();
        SemanticsHandle handle = owner.EnsureSemantics();
        Assert.NotNull(owner.SemanticsOwner);

        owner.Dispose();
        Assert.Null(owner.SemanticsOwner);
        GC.KeepAlive(handle);
    }

    [Fact]
    public void RequestVisualUpdate_WithoutCallbackOrManifold_IsANoOp()
    {
        var owner = new PipelineOwner();
        owner.RequestVisualUpdate();
    }

    [Fact]
    public void HostPipelineManifold_NotifiesOnlyWhenSemanticsEnabledChanges()
    {
        var manifold = new HostPipelineManifold();
        int notifications = 0;
        manifold.AddListener(() => notifications += 1);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(0, notifications);

        manifold.SetSemanticsEnabled(true);
        Assert.Equal(1, notifications);
        Assert.True(manifold.SemanticsEnabled);

        manifold.SetSemanticsEnabled(true);
        Assert.Equal(1, notifications);

        manifold.SetSemanticsEnabled(false);
        Assert.Equal(2, notifications);
    }

    private static List<PipelineOwner> TreeWalk(PipelineOwner root)
    {
        var results = new List<PipelineOwner> { root };

        void Visitor(PipelineOwner child)
        {
            results.Add(child);
            child.VisitChildren(Visitor);
        }

        root.VisitChildren(Visitor);
        return results;
    }

    /// <summary>Flutter's `TestRenderObject` from `pipeline_owner_tree_test.dart`.</summary>
    private sealed class TestRenderObject : RenderObject
    {
        private readonly Action? _onLayout;
        private readonly Action? _onSemantics;

        public TestRenderObject(Action? onLayout = null, Action? onSemantics = null)
        {
            _onLayout = onLayout;
            _onSemantics = onSemantics;
        }

        public override bool IsRepaintBoundary => true;

        public override Rect PaintBounds => default;

        protected override Rect SemanticBounds => default;

        protected override void PerformLayout() => _onLayout?.Invoke();

        protected override void PerformResize()
        {
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
        {
            _onSemantics?.Invoke();
        }
    }

    private sealed class LoggingRenderBox : RenderBox
    {
        private readonly Action _onPaint;

        public LoggingRenderBox(Action onPaint) => _onPaint = onPaint;

        protected override void PerformLayout() => Size = Constraints.Constrain(new Size(10, 10));

        public override void Paint(PaintingContext ctx, Point offset) => _onPaint();
    }
}
