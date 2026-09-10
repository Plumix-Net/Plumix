using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Tests;

/// <summary>
/// The behaviors Flutter's own <c>test/widgets/framework_test.dart</c>, <c>parent_data_test.dart</c>,
/// <c>set_state_*_test.dart</c> and <c>global_keys_duplicated_test.dart</c> pin for
/// <c>widgets/framework.dart</c>: key identity, the <c>ComponentElement</c> build contract, the
/// <c>State</c> lifecycle guards, ParentData diagnostics and duplicate-GlobalKey reporting.
/// </summary>
public sealed class FrameworkParityTests
{
    // ---------------------------------------------------------------- keys

    [Fact]
    public void ObjectKey_TakesItsIdentityFromTheInstanceNotTheValue()
    {
        object first = new();
        object second = new();

        Assert.Equal(new ObjectKey(first), new ObjectKey(first));
        Assert.Equal(new ObjectKey(first).GetHashCode(), new ObjectKey(first).GetHashCode());
        Assert.NotEqual(new ObjectKey(first), new ObjectKey(second));

        // Two equal-but-distinct values are *not* interchangeable: Dart compares with `identical`.
        var left = new List<int> { 1 };
        var right = new List<int> { 1 };
        Assert.NotEqual(new ObjectKey(left), new ObjectKey(right));

        Assert.Equal($"[{Diagnostics.DescribeIdentity(first)}]", new ObjectKey(first).ToString());
    }

    [Fact]
    public void GlobalObjectKey_ComparesByIdentityAndPrintsWithoutTheStateSuffix()
    {
        object value = new();

        Assert.Equal(new GlobalObjectKey<State>(value), new GlobalObjectKey<State>(value));
        Assert.Equal(
            new GlobalObjectKey<State>(value).GetHashCode(),
            new GlobalObjectKey<State>(value).GetHashCode());
        Assert.NotEqual(new GlobalObjectKey<State>(value), new GlobalObjectKey<State>(new()));

        // Dart strips the bare `State` type argument but keeps a specific one.
        Assert.Equal(
            $"[GlobalObjectKey {Diagnostics.DescribeIdentity(value)}]",
            new GlobalObjectKey<State>(value).ToString());
        Assert.Contains("GlobalObjectKey<ProbeState>", new GlobalObjectKey<ProbeState>(value).ToString());
    }

    [Fact]
    public void LabeledGlobalKey_PrintsItsDebugLabelAndKeepsIdentityEquality()
    {
        var labeled = new LabeledGlobalKey<State>("problematic");

        Assert.StartsWith("[GlobalKey#", labeled.ToString());
        Assert.EndsWith(" problematic]", labeled.ToString());
        Assert.DoesNotContain("DebugLabel", labeled.ToString());
        Assert.NotEqual(labeled, new LabeledGlobalKey<State>("problematic"));

        var unlabeled = new LabeledGlobalKey<State>(null);
        Assert.Equal($"[GlobalKey#{Diagnostics.ShortHash(unlabeled)}]", unlabeled.ToString());
    }

    [Fact]
    public void IndexedSlot_ComparesByIndexAndPreviousSibling()
    {
        var sibling = new ProbeWidget().CreateElement();

        Assert.Equal(new IndexedSlot<Element?>(2, sibling), new IndexedSlot<Element?>(2, sibling));
        Assert.Equal(
            new IndexedSlot<Element?>(2, sibling).GetHashCode(),
            new IndexedSlot<Element?>(2, sibling).GetHashCode());
        Assert.NotEqual(new IndexedSlot<Element?>(2, sibling), new IndexedSlot<Element?>(3, sibling));
        Assert.NotEqual(new IndexedSlot<Element?>(2, sibling), new IndexedSlot<Element?>(2, null));
    }

    [Fact]
    public void MultiChildRenderObjectElement_DoesNotMoveChildrenWhenTheSlotsAreUnchanged()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new RecordingFlex([new ProbeWidget(), new ProbeWidget()]));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        RecordingFlexRenderBox render = Assert.IsType<RecordingFlexRenderBox>(root.RenderObject);
        render.Moves = 0;

        root.Update(new RecordingFlex([new ProbeWidget(), new ProbeWidget()]));
        owner.FlushBuild();

        // Before IndexedSlot gained value equality every rebuild re-moved every child.
        Assert.Equal(0, render.Moves);
    }

    // -------------------------------------------------- ComponentElement build contract

    [Fact]
    public void ThrowingBuild_IsReportedAndReplacedByAnErrorWidget()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new ThrowingBuilder("boom"));
        root.Attach(owner);

        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(reported);
        Assert.Equal("widgets library", details.Library);
        Assert.Contains("boom", details.Exception.ToString());

        // The subtree is replaced by an ErrorWidget rather than torn down.
        Element errorElement = FindDescendant(root, element => element.Widget is ErrorWidget)!;
        Assert.Contains("boom", ((ErrorWidget)errorElement.Widget).Message);
    }

    [Fact]
    public void BuildReturningTheContextWidget_IsRejected()
    {
        Exception failure = BuildErrors.TakeException(() =>
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new SelfReturningBuilder());
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
        });

        Assert.Contains("A build function returned context.widget.", failure.ToString());
    }

    // ------------------------------------------------------------- State lifecycle

    [Fact]
    public void SetState_AfterDispose_ExplainsTheAsynchronousCauses()
    {
        var widget = new ProbeWidget();
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        ProbeState state = widget.CreatedState!;
        root.Update(new SizedBox());
        owner.FlushBuild();
        owner.FinalizeTree();

        var error = Assert.Throws<FlutterError>(() => state.Poke());
        Assert.Contains("setState() called after dispose()", error.Message);
        Assert.Contains("asynchronous", error.Message);
        Assert.Contains("await", error.Message);
    }

    [Fact]
    public void SetState_InTheConstructor_IsRejected()
    {
        var state = new ProbeState();
        var error = Assert.Throws<FlutterError>(state.Poke);
        Assert.Contains("setState() called in constructor", error.Message);
    }

    [Fact]
    public void SetState_WithAnAsynchronousCallback_IsRejected()
    {
        var widget = new ProbeWidget();
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        var error = Assert.Throws<FlutterError>(() => widget.CreatedState!.PokeAsynchronously());
        Assert.Contains("setState() callback argument returned a Future.", error.Message);
    }

    [Fact]
    public void State_ThatNeverMounted_ReportsNoWidget()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new ProbeState().DebugFillProperties(properties);

        Assert.Equal(
            ["lifecycle state: created", "no widget", "not mounted"],
            properties.Properties.Select(property => property.ToString()));
    }

    [Fact]
    public void Dispose_ThatForgetsToCallBase_IsReported()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(new RudeDisposeWidget());
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        var error = Assert.Throws<FlutterError>(() =>
        {
            root.Update(new SizedBox());
            owner.FlushBuild();
            owner.FinalizeTree();
        });
        Assert.Contains("failed to call base.Dispose", error.Message);
    }

    [Fact]
    public void ThrowingDeactivate_LeavesTheElementNeitherActiveNorDefunct()
    {
        var widget = new ProbeWidget(throwOnDeactivate: true);
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        Element probe = FindDescendant(root, element => element is StatefulElement)!;
        Assert.True(probe.DebugIsActive);

        Assert.ThrowsAny<Exception>(() =>
        {
            root.Update(new SizedBox());
            owner.FlushBuild();
        });

        Assert.False(probe.DebugIsActive);
        Assert.False(probe.DebugIsDefunct);
    }

    [Fact]
    public void ThrowingDispose_StillUnmountsTheElement()
    {
        var widget = new ProbeWidget(throwOnDispose: true);
        var owner = new BuildOwner();
        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        Element probe = FindDescendant(root, element => element is StatefulElement)!;

        Assert.ThrowsAny<Exception>(() =>
        {
            root.Update(new SizedBox());
            owner.FlushBuild();
            owner.FinalizeTree();
        });

        Assert.True(probe.DebugIsDefunct);
    }

    [Fact]
    public void Depth_IsUnavailableBeforeTheElementIsMounted()
    {
        Element element = new ProbeWidget().CreateElement();
        var error = Assert.Throws<FlutterError>(() => _ = element.Depth);
        Assert.Contains("Depth is only available when element has been mounted.", error.Message);
    }

    // ------------------------------------------------------------- ParentData

    [Fact]
    public void ParentDataWidget_UnderTheWrongAncestor_IsReportedAndItsDataIsNotApplied()
    {
        Exception failure = BuildErrors.TakeException(() =>
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new RecordingFlex([new TestParentDataWidget(7.0, new ProbeWidget())]));
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
        });

        string message = failure.ToString();
        Assert.Contains("Incorrect use of ParentDataWidget.", message);
        Assert.Contains("wants to apply ParentData of type TestParentData to a RenderObject", message);
        Assert.Contains("which has been set up to accept ParentData of incompatible type", message);
        Assert.Contains("The ownership chain for the RenderObject", message);
    }

    [Fact]
    public void CompetingParentDataWidgets_AreReportedWithBothCulprits()
    {
        Exception failure = BuildErrors.TakeException(() =>
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new SlotHost(
                new TestParentDataWidget(1.0, new TestParentDataWidget(2.0, new ProbeWidget()))));
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
        });

        string message = failure.ToString();
        Assert.Contains("Incorrect use of ParentDataWidget.", message);
        Assert.Contains("Competing ParentDataWidgets are providing parent data to the same RenderObject:", message);
        Assert.Contains("which writes ParentData of type TestParentData", message);
    }

    // ------------------------------------------------------------- GlobalKey duplication

    [Fact]
    public void OneGlobalKeyInTwoChildrenOfOneParent_NamesBothChildren()
    {
        var key = new LabeledGlobalKey<State>("problematic");

        Exception failure = BuildErrors.TakeException(() =>
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new RecordingFlex(
            [
                new ProbeWidget(key: key),
                new ProbeWidget(key: key),
            ]));
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
            owner.FinalizeTree();
        });

        Assert.Contains("A GlobalKey was used multiple times inside one widget's child list.", failure.ToString());
    }

    [Fact]
    public void OneGlobalKeyUnderTwoParents_IsReportedByFinalizeTree()
    {
        var key = new LabeledGlobalKey<State>("problematic");
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new RecordingFlex(
            [
                new SlotHost(new ProbeWidget(key: key), key: new ValueKey<int>(1)),
                new SlotHost(new ProbeWidget(key: key), key: new ValueKey<int>(2)),
            ]));
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
            owner.FinalizeTree();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        FlutterErrorDetails details = Assert.Single(reported);
        string message = details.Exception.ToString()!;

        // Dart's `finalizeTree` reports the parent that lost the key and never rebuilt, with singular
        // grammar for a single key and a single parent.
        Assert.Contains("Duplicate GlobalKey detected in widget tree.", message);
        Assert.Contains("The following GlobalKey was specified multiple times", message);
        Assert.Contains("it still thinks that it should have a child with that global key.", message);
        Assert.Contains(
            "A GlobalKey can only be specified on one widget at a time in the widget tree.",
            message);
    }

    [Fact]
    public void DebugPrintGlobalKeyedWidgetLifecycle_LogsDiscardingFromTheInactiveList()
    {
        var log = new List<string>();
        DebugPrintCallback previousPrint = Print.DebugPrint;
        Print.DebugPrint = (message, _) => log.Add(message ?? string.Empty);
        WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle = true;
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new ProbeWidget(key: new LabeledGlobalKey<State>("k")));
            root.Attach(owner);
            owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
            owner.FlushBuild();
            log.Clear();

            root.Update(new SizedBox());
            owner.FlushBuild();
            owner.FinalizeTree();
        }
        finally
        {
            WidgetsDebug.DebugPrintGlobalKeyedWidgetLifecycle = false;
            Print.DebugPrint = previousPrint;
        }

        // Dart prints exactly one line for this transition, from _InactiveElements._unmount.
        Assert.Contains(log, line => line.Contains("from inactive elements list."));
    }

    [Fact]
    public void DebugAssertAllWidgetVarsUnset_CatchesALeakedFlag()
    {
        Assert.True(WidgetsDebug.DebugAssertAllWidgetVarsUnset("clean"));
        WidgetsDebug.DebugPrintBuildScope = true;
        try
        {
            Assert.Throws<FlutterError>(() => WidgetsDebug.DebugAssertAllWidgetVarsUnset("leaked"));
        }
        finally
        {
            WidgetsDebug.DebugPrintBuildScope = false;
        }
    }

    // ------------------------------------------------------------- helpers

    private static Element? FindDescendant(Element root, Func<Element, bool> predicate)
    {
        Element? found = null;
        void Visit(Element element)
        {
            if (found != null)
            {
                return;
            }

            if (predicate(element))
            {
                found = element;
                return;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }

    private sealed class ProbeWidget : StatefulWidget
    {
        public ProbeWidget(bool throwOnDeactivate = false, bool throwOnDispose = false, Key? key = null)
            : base(key)
        {
            ThrowOnDeactivate = throwOnDeactivate;
            ThrowOnDispose = throwOnDispose;
        }

        public bool ThrowOnDeactivate { get; }

        public bool ThrowOnDispose { get; }

        public ProbeState? CreatedState { get; private set; }

        public override State CreateState()
        {
            var state = new ProbeState();
            CreatedState = state;
            return state;
        }
    }

    internal sealed class ProbeState : State
    {
        private ProbeWidget Current => (ProbeWidget)StateWidget;

        public void Poke() => SetState(() => { });

        public void PokeAsynchronously() => SetState(async () => await Task.Yield());

        public override void Deactivate()
        {
            if (Mounted && Current.ThrowOnDeactivate)
            {
                throw new InvalidOperationException("deactivate failed");
            }

            base.Deactivate();
        }

        public override void Dispose()
        {
            bool shouldThrow = Mounted && Current.ThrowOnDispose;
            base.Dispose();
            if (shouldThrow)
            {
                throw new InvalidOperationException("dispose failed");
            }
        }

        public override Widget Build(BuildContext context) => new SizedBox(width: 1.0, height: 1.0);
    }

    private sealed class RudeDisposeWidget : StatefulWidget
    {
        public override State CreateState() => new RudeDisposeState();
    }

    private sealed class RudeDisposeState : State
    {
        // Deliberately does not call base.Dispose(), which Dart reports as a contract violation.
        public override void Dispose()
        {
        }

        public override Widget Build(BuildContext context) => new SizedBox(width: 1.0, height: 1.0);
    }

    private sealed class ThrowingBuilder(string message) : StatelessWidget
    {
        public override Widget Build(BuildContext context) => throw new InvalidOperationException(message);
    }

    private sealed class SelfReturningBuilder : StatelessWidget
    {
        public override Widget Build(BuildContext context) => context.Widget;
    }

    private sealed class TestParentData : BoxParentData
    {
        public double Value { get; set; }
    }

    private sealed class TestParentDataWidget(double value, Widget child) : ParentDataWidget<TestParentData>(child)
    {
        public override Type DebugTypicalAncestorWidgetType => typeof(SlotHost);

        protected override void ApplyParentData(RenderObject renderObject)
        {
            ((TestParentData)renderObject.parentData!).Value = value;
        }
    }

    private sealed class SlotHost(Widget child, Key? key = null) : SingleChildRenderObjectWidget(child, key)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new SlotHostRenderBox();
    }

    private sealed class SlotHostRenderBox : RenderProxyBox
    {
        public override void SetupParentData(RenderObject child)
        {
            if (child.parentData is not TestParentData)
            {
                child.parentData = new TestParentData();
            }
        }

        protected override void PerformLayout()
        {
            Child?.Layout(Constraints, parentUsesSize: true);
            Size = Constraints.Constrain(Child?.Size ?? default);
        }
    }

    private sealed class RecordingFlex(IReadOnlyList<Widget> children) : MultiChildRenderObjectWidget(children)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RecordingFlexRenderBox();
    }

    private sealed class RecordingFlexRenderBox : RenderBox, IRenderObjectContainer
    {
        private readonly List<RenderObject> _children = [];

        public int Moves { get; set; }

        public void Insert(RenderObject child, RenderObject? after)
        {
            int index = after is null ? 0 : _children.IndexOf(after) + 1;
            _children.Insert(index, child);
            SetupParentData(child);
            AdoptChild(child);
        }

        public void Move(RenderObject child, RenderObject? after)
        {
            Moves += 1;
            _children.Remove(child);
            int index = after is null ? 0 : _children.IndexOf(after) + 1;
            _children.Insert(index, child);
            MarkNeedsLayout();
        }

        public void Remove(RenderObject child)
        {
            _children.Remove(child);
            DropChild(child);
        }

        public override void SetupParentData(RenderObject child)
        {
            if (child.parentData is not BoxParentData)
            {
                child.parentData = new BoxParentData();
            }
        }

        public override void VisitChildren(Action<RenderObject> visitor)
        {
            foreach (RenderObject child in _children)
            {
                visitor(child);
            }
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }

        protected override void PerformLayout()
        {
            foreach (RenderObject child in _children)
            {
                ((RenderBox)child).Layout(BoxConstraints.Loose(Constraints.Biggest), parentUsesSize: true);
            }

            Size = Constraints.Smallest;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public override Element? RenderObjectAttachingChild => _child;

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
            Rebuild(force: true);
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
            base.ForgetChild(child);
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
