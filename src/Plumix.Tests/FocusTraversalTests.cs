using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/focus_traversal_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FocusTraversalTests : IDisposable
{
    private static readonly Size ViewSize = new(400, 400);

    public FocusTraversalTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ReadingOrderTraversalPolicy_SortsTopBandsBeforeRegistrationOrder()
    {
        var bottom = new FocusNode
        {
            TraversalRect = new Rect(0, 80, 20, 20),
        };
        var middleRight = new FocusNode
        {
            TraversalRect = new Rect(40, 40, 20, 20),
        };
        var middleLeft = new FocusNode
        {
            TraversalRect = new Rect(0, 40, 20, 20),
        };
        var top = new FocusNode
        {
            TraversalRect = new Rect(0, 0, 20, 20),
        };

        IReadOnlyList<FocusNode> sorted = ReadingOrderTraversalPolicy.Sort(
            [bottom, middleRight, middleLeft, top]).ToList();

        Assert.Equal([top, middleLeft, middleRight, bottom], sorted);
    }

    [Fact]
    public void ReadingOrderTraversalPolicy_PreservesDegenerateNodeOrderWithoutCrashing()
    {
        var first = new FocusNode
        {
            TraversalRect = default(Rect),
        };
        var second = new FocusNode
        {
            TraversalRect = default(Rect),
        };

        IReadOnlyList<FocusNode> sorted = ReadingOrderTraversalPolicy.Sort([first, second]).ToList();

        Assert.Equal([first, second], sorted);
    }

    [Theory]
    [InlineData(TextDirection.Ltr)]
    [InlineData(TextDirection.Rtl)]
    public void ReadingOrderTraversalPolicy_SortsABandInTheAmbientDirectionality(TextDirection direction)
    {
        var leftNode = new FocusNode(debugLabel: "left");
        var rightNode = new FocusNode(debugLabel: "right");
        var anchor = new FocusNode(debugLabel: "anchor");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            direction,
            new Stack(children:
            [
                new Positioned(
                    left: 0,
                    top: 0,
                    child: new Focus(
                        focusNode: anchor,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20))),
                new Positioned(
                    left: 0,
                    top: 100,
                    child: new Focus(focusNode: leftNode, child: new SizedBox(width: 20, height: 20))),
                new Positioned(
                    left: 100,
                    top: 100,
                    child: new Focus(focusNode: rightNode, child: new SizedBox(width: 20, height: 20))),
            ])));
        harness.Layout(ViewSize);

        FocusNode expectedFirst = direction == TextDirection.Ltr ? leftNode : rightNode;
        FocusNode expectedSecond = direction == TextDirection.Ltr ? rightNode : leftNode;

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(expectedFirst, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(expectedSecond, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void WidgetOrderTraversalPolicy_KeepsRegistrationOrderRegardlessOfGeometry()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        var third = new FocusNode(debugLabel: "third");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new FocusTraversalGroup(
                policy: new WidgetOrderTraversalPolicy(),
                child: new Row(children:
                [
                    // Laid out right-to-left on screen, but declared first-to-last in the widget tree.
                    new Focus(focusNode: third, child: new SizedBox(width: 20, height: 20)),
                    new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
                    new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                ]))));
        harness.Layout(ViewSize);

        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusPrevious));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusPrevious));
        Assert.Same(third, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void OrderedTraversalPolicy_UsesNumericFocusOrderBeforeUnorderedNodes()
    {
        var ordered2 = new FocusNode(debugLabel: "ordered2");
        var ordered1 = new FocusNode(debugLabel: "ordered1");
        var unordered = new FocusNode(debugLabel: "unordered");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new FocusTraversalGroup(
                policy: new OrderedTraversalPolicy(),
                child: new Row(children:
                [
                    new Focus(focusNode: unordered, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                    new FocusTraversalOrder(
                        order: new NumericFocusOrder(2),
                        child: new Focus(focusNode: ordered2, child: new SizedBox(width: 20, height: 20))),
                    new FocusTraversalOrder(
                        order: new NumericFocusOrder(1),
                        child: new Focus(focusNode: ordered1, child: new SizedBox(width: 20, height: 20))),
                ]))));
        harness.Layout(ViewSize);

        Assert.Same(unordered, FocusManager.Instance.PrimaryFocus);
        // Ordered nodes come first in numeric order; the unordered node trails them.
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(ordered1, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(ordered2, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(unordered, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void OrderedTraversalPolicy_SortsLexicalFocusOrdersAsStrings()
    {
        var beta = new FocusNode(debugLabel: "beta");
        var alpha = new FocusNode(debugLabel: "alpha");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new FocusTraversalGroup(
                policy: new OrderedTraversalPolicy(),
                child: new Row(children:
                [
                    new FocusTraversalOrder(
                        order: new LexicalFocusOrder("b"),
                        child: new Focus(
                            focusNode: beta,
                            autofocus: true,
                            child: new SizedBox(width: 20, height: 20))),
                    new FocusTraversalOrder(
                        order: new LexicalFocusOrder("a"),
                        child: new Focus(focusNode: alpha, child: new SizedBox(width: 20, height: 20))),
                ]))));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(alpha, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusOrder_RefusesToCompareIncompatibleOrderTypes()
    {
        Assert.Throws<InvalidOperationException>(
            () => new NumericFocusOrder(1).CompareTo(new LexicalFocusOrder("a")));
    }

    /// <remarks>
    /// Dart parity source: <c>FocusTraversalGroup.of</c>'s assert — traversal outside a group is an
    /// error, not a silent fallback onto a default policy.
    /// </remarks>
    [Fact]
    public void FocusTraversalGroup_TraversalWithoutAGroupThrows()
    {
        var node = new FocusNode(debugLabel: "lonely");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Focus(focusNode: node, autofocus: true, child: new SizedBox(width: 20, height: 20))));
        harness.Layout(ViewSize);

        Assert.True(node.HasFocus);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => node.NextFocus());
        Assert.Contains("FocusTraversalGroup", error.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => node.PreviousFocus());
        Assert.Throws<InvalidOperationException>(() => node.FocusInDirection(TraversalDirection.Down));
    }

    /// <remarks>
    /// Dart routes Tab and the arrow keys through <c>WidgetsApp</c>'s default shortcut map into
    /// <c>NextFocusIntent</c>/<c>PreviousFocusIntent</c>/<c>DirectionalFocusIntent</c>; the focus
    /// manager itself has no traversal fallback.
    /// </remarks>
    [Fact]
    public void TraversalKeys_MoveTheFocusThroughTheAppShortcutMap()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        Assert.Same(first, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(() => FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab))));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(() =>
            FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab, shift: true))));
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(() =>
            FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight))));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
    }

    /// <remarks>
    /// Without the app's shortcuts nothing maps Tab to <c>NextFocusIntent</c>, so the key is
    /// unhandled and the focus stays put — the manager has no traversal fallback of its own.
    /// </remarks>
    [Fact]
    public void TraversalKeys_DoNothingWithoutTheAppShortcutMap()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new FocusTraversalGroup(child: new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
            ]))));
        harness.Layout(ViewSize);

        Assert.False(PumpFocus(() => FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab))));
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusTraversalGroup_SortsEachGroupThenSplicesItIntoTheOuterOrder()
    {
        var outerFirst = new FocusNode(debugLabel: "outerFirst");
        var innerLast = new FocusNode(debugLabel: "innerLast");
        var innerFirst = new FocusNode(debugLabel: "innerFirst");
        var outerLast = new FocusNode(debugLabel: "outerLast");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: outerFirst, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new FocusTraversalGroup(
                    policy: new WidgetOrderTraversalPolicy(),
                    child: new Row(children:
                    [
                        new Focus(focusNode: innerLast, child: new SizedBox(width: 20, height: 20)),
                        new Focus(focusNode: innerFirst, child: new SizedBox(width: 20, height: 20)),
                    ])),
                new Focus(focusNode: outerLast, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        // The group's own policy orders its members, and the whole group stays in reading order.
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(innerLast, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(innerFirst, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(outerLast, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusTraversalGroup_MaybeOfNodeResolvesTheGroupPolicyThroughTheNodeTree()
    {
        var policy = new WidgetOrderTraversalPolicy();
        var node = new FocusNode(debugLabel: "node");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new FocusTraversalGroup(
                policy: policy,
                child: new Focus(focusNode: node, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(ViewSize);

        Assert.Same(policy, FocusTraversalGroup.MaybeOfNode(node));
        Assert.Same(policy, FocusTraversalGroup.MaybeOf(node.Context!.Value));
    }

    [Fact]
    public void ExcludeFocusTraversal_KeepsDescendantsFocusableButOutOfTheTabOrder()
    {
        var first = new FocusNode(debugLabel: "first");
        var excluded = new FocusNode(debugLabel: "excluded");
        var last = new FocusNode(debugLabel: "last");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new ExcludeFocusTraversal(
                    child: new Focus(focusNode: excluded, child: new SizedBox(width: 20, height: 20))),
                new Focus(focusNode: last, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        Assert.True(excluded.CanRequestFocus);
        Assert.True(excluded.SkipTraversal);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(last, FocusManager.Instance.PrimaryFocus);
        PumpFocus(excluded.RequestFocus);
        Assert.Same(excluded, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void DirectionalTraversal_PrefersNodesInTheBandOfTheFocusedRect()
    {
        var origin = new FocusNode(debugLabel: "origin");
        var below = new FocusNode(debugLabel: "below");
        var farRight = new FocusNode(debugLabel: "farRight");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Stack(children:
            [
                new Positioned(
                    left: 0,
                    top: 0,
                    child: new Focus(
                        focusNode: origin,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20))),
                new Positioned(
                    left: 0,
                    top: 200,
                    child: new Focus(focusNode: below, child: new SizedBox(width: 20, height: 20))),
                new Positioned(
                    left: 300,
                    top: 100,
                    child: new Focus(focusNode: farRight, child: new SizedBox(width: 20, height: 20))),
            ])));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(() => origin.FocusInDirection(TraversalDirection.Down)));
        Assert.Same(below, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void DirectionalTraversal_ReversesThroughTheRecordedHistoryWhenTheDirectionFlips()
    {
        var topLeft = new FocusNode(debugLabel: "topLeft");
        var bottomWide = new FocusNode(debugLabel: "bottomWide");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Stack(children:
            [
                new Positioned(
                    left: 0,
                    top: 0,
                    child: new Focus(
                        focusNode: topLeft,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20))),
                new Positioned(
                    left: 0,
                    top: 100,
                    child: new Focus(focusNode: bottomWide, child: new SizedBox(width: 200, height: 20))),
            ])));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(() => topLeft.FocusInDirection(TraversalDirection.Down)));
        Assert.Same(bottomWide, FocusManager.Instance.PrimaryFocus);

        // Dart records the node it came from, so going back up returns to it instead of re-running
        // the geometric search from the wide node's centre.
        Assert.True(PumpFocus(() => bottomWide.FocusInDirection(TraversalDirection.Up)));
        Assert.Same(topLeft, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void DirectionalTraversal_StopsAtTheEdgeWithTheDefaultDirectionalEdgeBehavior()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(() => first.FocusInDirection(TraversalDirection.Right)));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        // FocusScopeNode.directionalTraversalEdgeBehavior defaults to stop.
        Assert.False(PumpFocus(() => second.FocusInDirection(TraversalDirection.Right)));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void DirectionalTraversal_WrapsWhenTheScopeUsesAClosedLoop()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        var scope = new FocusScopeNode(directionalTraversalEdgeBehavior: TraversalEdgeBehavior.ClosedLoop);
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scope,
                new Row(children:
                [
                    new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                    new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
                ]))));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(() => first.FocusInDirection(TraversalDirection.Right)));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(() => second.FocusInDirection(TraversalDirection.Right)));
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void TraversalEdgeBehavior_StopRefusesToWrapAtTheEndOfTheScope()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        var scope = new FocusScopeNode(traversalEdgeBehavior: TraversalEdgeBehavior.Stop);
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scope,
                new Row(children:
                [
                    new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                    new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
                ]))));
        harness.Layout(ViewSize);

        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.False(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void TraversalEdgeBehavior_LeaveFlutterViewUnfocusesInsteadOfWrapping()
    {
        var only = new FocusNode(debugLabel: "only");
        var scope = new FocusScopeNode(traversalEdgeBehavior: TraversalEdgeBehavior.LeaveFlutterView);
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scope,
                new Focus(focusNode: only, autofocus: true, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(ViewSize);

        Assert.Same(only, FocusManager.Instance.PrimaryFocus);
        Assert.False(PumpFocus(FocusManager.Instance.FocusNext));
        // Dart's unfocus hands the focus back to the enclosing scope rather than clearing it.
        Assert.False(only.HasPrimaryFocus);
        Assert.IsType<FocusScopeNode>(FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void TraversalEdgeBehavior_ParentScopeContinuesInTheEnclosingScope()
    {
        var outerFirst = new FocusNode(debugLabel: "outerFirst");
        var inner = new FocusNode(debugLabel: "inner");
        var outerLast = new FocusNode(debugLabel: "outerLast");
        var innerScope = new FocusScopeNode(traversalEdgeBehavior: TraversalEdgeBehavior.ParentScope);
        var outerScope = new FocusScopeNode();
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                outerScope,
                new Row(children:
                [
                    new Focus(focusNode: outerFirst, child: new SizedBox(width: 20, height: 20)),
                    FocusScope.WithExternalFocusNode(
                        innerScope,
                        new Focus(
                            focusNode: inner,
                            autofocus: true,
                            child: new SizedBox(width: 20, height: 20))),
                    new Focus(focusNode: outerLast, child: new SizedBox(width: 20, height: 20)),
                ]))));
        harness.Layout(ViewSize);

        Assert.Same(inner, FocusManager.Instance.PrimaryFocus);
        Assert.True(PumpFocus(FocusManager.Instance.FocusNext));
        Assert.Same(outerLast, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void NextAndPreviousFocusActions_ReportWhetherTheyMovedTheFocus()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        var next = new NextFocusAction();
        Assert.Equal(true, next.Invoke(new NextFocusIntent()));
        Scheduler.FlushMicrotasks();
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.Equal(KeyEventResult.Handled, next.ToKeyEventResult(new NextFocusIntent(), true));
        Assert.Equal(
            KeyEventResult.SkipRemainingHandlers,
            next.ToKeyEventResult(new NextFocusIntent(), false));

        var previous = new PreviousFocusAction();
        Assert.Equal(true, previous.Invoke(new PreviousFocusIntent()));
        Scheduler.FlushMicrotasks();
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void RequestFocusAction_RunsTheIntentsRequestFocusCallback()
    {
        var target = new FocusNode(debugLabel: "target");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Focus(focusNode: target, child: new SizedBox(width: 20, height: 20))));
        harness.Layout(ViewSize);

        FocusNode? requested = null;
        var intent = new RequestFocusIntent(
            target,
            requestFocusCallback: (node, _, _, _, _) => requested = node);
        new RequestFocusAction().Invoke(intent);

        Assert.Same(target, requested);
    }

    [Fact]
    public void DirectionalFocusAction_ForTextFieldIgnoresIntentsThatSkipTextFields()
    {
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = FocusLayoutHarness.WithTraversalGroup(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
            ])));
        harness.Layout(ViewSize);

        DirectionalFocusAction.ForTextField().Invoke(new DirectionalFocusIntent(TraversalDirection.Right));
        Assert.Same(first, FocusManager.Instance.PrimaryFocus);

        DirectionalFocusAction.ForTextField().Invoke(
            new DirectionalFocusIntent(TraversalDirection.Right, ignoreTextFields: false));
        Scheduler.FlushMicrotasks();
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
    }

    private static bool PumpFocus(Func<bool> action)
    {
        bool result = action();
        Scheduler.FlushMicrotasks();
        return result;
    }

    private static void PumpFocus(Action action)
    {
        action();
        Scheduler.FlushMicrotasks();
    }
}
