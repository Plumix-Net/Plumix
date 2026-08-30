using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/focus_manager.dart; flutter/packages/flutter/lib/src/widgets/focus_scope.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FocusTests : IDisposable
{
    public FocusTests()
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
    public void FocusManager_RequestFocus_IsDeferredAndCoalescesToTheLastRequest()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        int notificationCount = 0;
        manager.RegisterNode(first);
        manager.RegisterNode(second);
        manager.AddListener(() => notificationCount += 1);

        Assert.True(first.RequestFocus());
        Assert.True(second.RequestFocus());
        Assert.Null(manager.PrimaryFocus);
        Assert.Equal(0, notificationCount);

        Scheduler.FlushMicrotasks();

        Assert.Same(second, manager.PrimaryFocus);
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public void FocusManager_FinalRequestForCurrentFocusProducesNoNotifications()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        manager.RegisterNode(first);
        manager.RegisterNode(second);
        first.RequestFocus();
        Scheduler.FlushMicrotasks();

        int managerNotificationCount = 0;
        int firstNotificationCount = 0;
        int secondNotificationCount = 0;
        manager.AddListener(() => managerNotificationCount += 1);
        first.AddListener(() => firstNotificationCount += 1);
        second.AddListener(() => secondNotificationCount += 1);

        second.RequestFocus();
        first.RequestFocus();
        Scheduler.FlushMicrotasks();

        Assert.Same(first, manager.PrimaryFocus);
        Assert.Equal(0, managerNotificationCount);
        Assert.Equal(0, firstNotificationCount);
        Assert.Equal(0, secondNotificationCount);
    }

    [Fact]
    public void FocusManager_ListenerRequestSchedulesAFollowUpMicrotask()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        var third = new FocusNode();
        manager.RegisterNode(first);
        manager.RegisterNode(second);
        manager.RegisterNode(third);
        first.RequestFocus();
        Scheduler.FlushMicrotasks();

        int managerNotificationCount = 0;
        manager.AddListener(() => managerNotificationCount += 1);
        first.AddListener(() => third.RequestFocus());

        second.RequestFocus();
        Scheduler.FlushMicrotasks();

        Assert.Same(third, manager.PrimaryFocus);
        Assert.Equal(2, managerNotificationCount);
    }

    [Fact]
    public void FocusManager_RequestFocus_UpdatesPrimaryFocusAndNodeFlags()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();

        manager.RegisterNode(first);
        manager.RegisterNode(second);

        Assert.True(PumpFocus(() => manager.RequestFocus(first)));
        Assert.Same(first, manager.PrimaryFocus);
        Assert.True(first.HasFocus);
        Assert.False(second.HasFocus);

        Assert.True(PumpFocus(() => manager.RequestFocus(second)));
        Assert.Same(second, manager.PrimaryFocus);
        Assert.False(first.HasFocus);
        Assert.True(second.HasFocus);
    }

    [Fact]
    public void FocusScope_RemembersItsLastFocusedChildAfterFocusMovesToASiblingScope()
    {
        var manager = new FocusManager();
        var parent = new FocusScopeNode();
        var firstScope = new FocusScopeNode();
        var secondScope = new FocusScopeNode();
        var firstChild = new FocusNode();
        var secondChild = new FocusNode();
        manager.RegisterNode(parent);
        manager.RegisterNode(firstScope, parent);
        manager.RegisterNode(secondScope, parent);
        manager.RegisterNode(firstChild, firstScope);
        manager.RegisterNode(secondChild, secondScope);

        Assert.True(PumpFocus(firstChild.RequestFocus));
        Assert.Same(firstChild, firstScope.FocusedChild);
        Assert.True(PumpFocus(secondChild.RequestFocus));
        Assert.Same(firstChild, firstScope.FocusedChild);
        Assert.Same(secondChild, secondScope.FocusedChild);

        parent.SetFirstFocus(firstScope);
        Scheduler.FlushMicrotasks();

        Assert.Same(firstChild, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusNode_HasFocusTracksFocusedDescendantsWithoutFlickeringBetweenThem()
    {
        var owner = new BuildOwner();
        var ancestor = new FocusNode();
        var first = new FocusNode();
        var second = new FocusNode();
        var outside = new FocusNode();
        var changes = new List<bool>();
        var root = new TestRootElement(new Column(children:
        [
            new Focus(
                focusNode: ancestor,
                canRequestFocus: false,
                onFocusChange: changes.Add,
                child: new Column(children:
                [
                    new Focus(focusNode: first, child: new SizedBox(width: 10.0, height: 10.0)),
                    new Focus(focusNode: second, child: new SizedBox(width: 10.0, height: 10.0)),
                ])),
            new Focus(focusNode: outside, child: new SizedBox(width: 10.0, height: 10.0)),
        ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(first.RequestFocus());
        owner.FlushBuild();
        Assert.True(ancestor.HasFocus);
        Assert.Equal([true], changes);

        Assert.True(second.RequestFocus());
        owner.FlushBuild();
        Assert.True(ancestor.HasFocus);
        Assert.Equal([true], changes);

        Assert.True(outside.RequestFocus());
        owner.FlushBuild();
        Assert.False(ancestor.HasFocus);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void FocusManager_HandleKeyEvent_InvokesPrimaryNodeCallback()
    {
        var manager = new FocusManager();
        int callbackInvocationCount = 0;
        var node = new FocusNode
        {
            OnKeyEvent = (_, @event) =>
            {
                if (@event.LogicalKey.Equals(LogicalKeyboardKey.Enter))
                {
                    callbackInvocationCount += 1;
                    return KeyEventResult.Handled;
                }

                return KeyEventResult.Ignored;
            }
        };

        manager.RegisterNode(node);
        manager.RequestFocus(node);
        Scheduler.FlushMicrotasks();

        bool handled = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Enter));

        Assert.True(handled);
        Assert.Equal(1, callbackInvocationCount);
    }

    [Fact]
    public void FocusManager_TabTraversal_MovesForwardAndBackward()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        var third = new FocusNode
        {
            CanRequestFocus = false
        };

        manager.RegisterNode(first);
        manager.RegisterNode(second);
        manager.RegisterNode(third);
        manager.RequestFocus(first);
        Scheduler.FlushMicrotasks();

        bool movedForward = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Scheduler.FlushMicrotasks();
        Assert.True(movedForward);
        Assert.Same(second, manager.PrimaryFocus);

        bool movedBackward = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab, shift: true));
        Scheduler.FlushMicrotasks();
        Assert.True(movedBackward);
        Assert.Same(first, manager.PrimaryFocus);

        PumpFocus(() => manager.RequestFocus(second));
        bool movedPastLast = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Scheduler.FlushMicrotasks();
        Assert.True(movedPastLast);
        Assert.Same(first, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_TabTraversal_StaysWithinCurrentFocusScope()
    {
        var manager = new FocusManager();
        var leftScope = new FocusScopeNode();
        var rightScope = new FocusScopeNode();
        var leftFirst = new FocusNode();
        var leftSecond = new FocusNode();
        var rightOnly = new FocusNode();

        manager.RegisterNode(leftScope);
        manager.RegisterNode(rightScope);
        manager.RegisterNode(leftFirst, leftScope);
        manager.RegisterNode(leftSecond, leftScope);
        manager.RegisterNode(rightOnly, rightScope);

        manager.RequestFocus(leftFirst);
        Scheduler.FlushMicrotasks();
        Assert.Same(leftFirst, leftScope.FocusedChild);

        Assert.True(PumpFocus(manager.FocusNext));
        Assert.Same(leftSecond, manager.PrimaryFocus);
        Assert.Same(leftSecond, leftScope.FocusedChild);

        // The default closed loop wraps within the scope instead of escaping into the sibling scope.
        Assert.True(PumpFocus(manager.FocusNext));
        Assert.Same(leftFirst, manager.PrimaryFocus);
        Assert.False(rightOnly.HasFocus);

        Assert.True(PumpFocus(manager.FocusPrevious));
        Assert.Same(leftSecond, manager.PrimaryFocus);
        Assert.False(rightOnly.HasFocus);

        leftScope.TraversalEdgeBehavior = TraversalEdgeBehavior.Stop;
        Assert.False(PumpFocus(manager.FocusNext));
        Assert.Same(leftSecond, manager.PrimaryFocus);
    }

    /// <summary>
    /// Dart's directional traversal is purely geometric: every candidate is filtered by
    /// <c>node.rect != target</c>, so nodes that have no render geometry are all filtered out and
    /// the arrow keys move nothing. Tab traversal, which is ordinal, still works on the same nodes.
    /// </summary>
    [Fact]
    public void FocusManager_DirectionalKeys_DoNothingWithoutGeometry()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        var third = new FocusNode();

        manager.RegisterNode(first);
        manager.RegisterNode(second);
        manager.RegisterNode(third);
        manager.RequestFocus(first);
        Scheduler.FlushMicrotasks();

        Assert.False(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Same(first, manager.PrimaryFocus);

        Assert.False(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.Same(first, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_DirectionalKeys_UseGeometryWhenTraversalRectsAvailable()
    {
        var manager = new FocusManager();
        var source = new FocusNode
        {
            TraversalRect = new Rect(0, 0, 20, 20)
        };
        var right = new FocusNode
        {
            TraversalRect = new Rect(120, 0, 20, 20)
        };
        var down = new FocusNode
        {
            TraversalRect = new Rect(0, 120, 20, 20)
        };
        var diagonal = new FocusNode
        {
            TraversalRect = new Rect(120, 120, 20, 20)
        };

        manager.RegisterNode(source);
        manager.RegisterNode(right);
        manager.RegisterNode(down);
        manager.RegisterNode(diagonal);
        manager.RequestFocus(source);
        Scheduler.FlushMicrotasks();

        Assert.True(PumpFocus(() => manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown))));
        Assert.Same(down, manager.PrimaryFocus);

        Assert.True(PumpFocus(() => manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight))));
        Assert.Same(diagonal, manager.PrimaryFocus);

        Assert.True(PumpFocus(() => manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp))));
        Assert.Same(right, manager.PrimaryFocus);

        Assert.True(PumpFocus(() => manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft))));
        Assert.Same(source, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_DirectionalKeys_ResolveTraversalRectsThroughRenderTransforms()
    {
        var source = new FocusNode();
        var right = new FocusNode();
        var transformedDown = new FocusNode();
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(
                    focusNode: source,
                    autofocus: true,
                    child: new SizedBox(width: 20, height: 20)),
                new SizedBox(width: 100),
                new Focus(
                    focusNode: right,
                    child: new SizedBox(width: 20, height: 20)),
                new SizedBox(width: 100),
                new TestTransform(
                    transform: Matrix4.TranslationValues(-120, 120, 0.0),
                    child: new Focus(
                        focusNode: transformedDown,
                        child: new SizedBox(width: 20, height: 20))),
            ])));
        harness.Layout(new Size(400, 400));

        Assert.Same(source, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(
            () => FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight))));
        Assert.Same(right, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(
            () => FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown))));
        Assert.Same(transformedDown, FocusManager.Instance.PrimaryFocus);

        Assert.True(PumpFocus(
            () => FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp))));
        Assert.Same(right, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusWidget_Autofocus_RequestsFocusOnMount()
    {
        var owner = new BuildOwner();
        var focusNode = new FocusNode();
        var root = new TestRootElement(
            new Focus(
                focusNode: focusNode,
                autofocus: true,
                child: new SizedBox(width: 20, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.True(focusNode.HasFocus);
        Assert.Same(focusNode, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusWidget_OnKeyEvent_CallbackIsUsedByFocusManager()
    {
        var owner = new BuildOwner();
        int keyEventCount = 0;
        var root = new TestRootElement(
            new Focus(
                autofocus: true,
                onKeyEvent: (_, @event) =>
                {
                    if (@event.LogicalKey.Equals(LogicalKeyboardKey.Space))
                    {
                        keyEventCount += 1;
                        return KeyEventResult.Handled;
                    }

                    return KeyEventResult.Ignored;
                },
                child: new SizedBox(width: 12, height: 12)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Space));

        Assert.True(handled);
        Assert.Equal(1, keyEventCount);
    }

    [Fact]
    public void FocusWidgets_TabKey_TraversesRegisteredFocusNodes()
    {
        var owner = new BuildOwner();
        var first = new FocusNode();
        var second = new FocusNode();

        var root = new TestRootElement(
            new Row(children:
            [
                new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 12, height: 12)),
                new Focus(focusNode: second, child: new SizedBox(width: 12, height: 12))
            ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
        Assert.True(first.HasFocus);
        Assert.False(second.HasFocus);

        bool handled = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Scheduler.FlushMicrotasks();

        Assert.True(handled);
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.False(first.HasFocus);
        Assert.True(second.HasFocus);
    }

    [Fact]
    public void FocusScopeWidget_TabTraversal_DoesNotEscapeScopeBoundaries()
    {
        var leadingSibling = new FocusNode();
        var firstInScope = new FocusNode();
        var secondInScope = new FocusNode();
        var trailingSibling = new FocusNode();

        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: leadingSibling, child: new SizedBox(width: 12, height: 12)),
                new FocusScope(
                    child: new Row(children:
                    [
                        new Focus(focusNode: firstInScope, autofocus: true, child: new SizedBox(width: 12, height: 12)),
                        new Focus(focusNode: secondInScope, child: new SizedBox(width: 12, height: 12))
                    ])),
                new Focus(focusNode: trailingSibling, child: new SizedBox(width: 12, height: 12))
            ])));
        harness.Layout(new Size(400, 400));

        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        // The default closed loop wraps within the scope instead of escaping into the siblings.
        bool movedBeforeScopeStart = FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.Tab, shift: true));
        Scheduler.FlushMicrotasks();
        Assert.True(movedBeforeScopeStart);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(leadingSibling.HasFocus);

        bool movedInsideScope = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Scheduler.FlushMicrotasks();
        Assert.True(movedInsideScope);
        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        bool movedAfterScopeEnd = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Scheduler.FlushMicrotasks();
        Assert.True(movedAfterScopeEnd);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(trailingSibling.HasFocus);
    }

    [Fact]
    public void FocusScopeWidget_DirectionalTraversal_DoesNotEscapeScopeBoundaries()
    {
        var leadingSibling = new FocusNode();
        var firstInScope = new FocusNode();
        var secondInScope = new FocusNode();
        var trailingSibling = new FocusNode();

        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                new Focus(focusNode: leadingSibling, child: new SizedBox(width: 12, height: 12)),
                new FocusScope(
                    child: new Row(children:
                    [
                        new Focus(focusNode: firstInScope, autofocus: true, child: new SizedBox(width: 12, height: 12)),
                        new Focus(focusNode: secondInScope, child: new SizedBox(width: 12, height: 12))
                    ])),
                new Focus(focusNode: trailingSibling, child: new SizedBox(width: 12, height: 12))
            ])));
        harness.Layout(new Size(400, 400));

        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        bool movedBeforeScopeStart = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft));
        Scheduler.FlushMicrotasks();
        Assert.False(movedBeforeScopeStart);
        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(leadingSibling.HasFocus);

        bool movedInsideScope = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight));
        Scheduler.FlushMicrotasks();
        Assert.True(movedInsideScope);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);

        bool movedAfterScopeEnd = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight));
        Scheduler.FlushMicrotasks();
        Assert.False(movedAfterScopeEnd);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(trailingSibling.HasFocus);
    }

    [Fact]
    public void FocusNode_TreeAccessorsFollowTheAttachedWidgetHierarchy()
    {
        var scopeNode = new FocusScopeNode(debugLabel: "scope");
        var parent = new FocusNode(debugLabel: "parent");
        var child = new FocusNode(debugLabel: "child");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scopeNode,
                new Focus(
                    focusNode: parent,
                    child: new Focus(focusNode: child, child: new SizedBox(width: 20, height: 20))))));
        harness.Layout(new Size(200, 200));

        Assert.Same(parent, child.Parent);
        Assert.Same(scopeNode, parent.Parent);
        Assert.Equal([parent, scopeNode, FocusManager.Instance.RootScope], child.Ancestors);
        Assert.Contains(child, parent.Descendants);
        Assert.Same(scopeNode, child.EnclosingScope);
        Assert.Same(scopeNode, child.NearestScope);
        Assert.Same(scopeNode, scopeNode.NearestScope);
        Assert.Contains(child, scopeNode.TraversalDescendants);
    }

    [Fact]
    public void FocusNode_DescendantsAreFocusableBlocksTheWholeSubtree()
    {
        var blocker = new FocusNode(debugLabel: "blocker");
        var blocked = new FocusNode(debugLabel: "blocked");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Focus(
                focusNode: blocker,
                descendantsAreFocusable: false,
                child: new Focus(focusNode: blocked, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(new Size(200, 200));

        Assert.False(blocked.CanRequestFocus);
        Assert.False(blocked.RequestFocus());
        Assert.Empty(blocker.TraversalDescendants);
    }

    [Fact]
    public void FocusNode_UnfocusScopeDispositionForgetsTheScopesFocusedChildren()
    {
        var scopeNode = new FocusScopeNode(debugLabel: "scope");
        var node = new FocusNode(debugLabel: "node");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scopeNode,
                new Focus(focusNode: node, autofocus: true, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(new Size(200, 200));

        Assert.Same(node, scopeNode.FocusedChild);
        node.Unfocus();
        Scheduler.FlushMicrotasks();

        Assert.Null(scopeNode.FocusedChild);
        Assert.Same(scopeNode, FocusManager.Instance.PrimaryFocus);
    }

    [Fact]
    public void FocusNode_UnfocusPreviouslyFocusedChildKeepsTheRestOfTheScopeHistory()
    {
        var scopeNode = new FocusScopeNode(debugLabel: "scope");
        var first = new FocusNode(debugLabel: "first");
        var second = new FocusNode(debugLabel: "second");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scopeNode,
                new Row(children:
                [
                    new Focus(focusNode: first, autofocus: true, child: new SizedBox(width: 20, height: 20)),
                    new Focus(focusNode: second, child: new SizedBox(width: 20, height: 20)),
                ]))));
        harness.Layout(new Size(200, 200));

        Assert.True(PumpFocus(second.RequestFocus));
        second.Unfocus(UnfocusDisposition.PreviouslyFocusedChild);
        Scheduler.FlushMicrotasks();

        Assert.Same(first, FocusManager.Instance.PrimaryFocus);
        Assert.Same(first, scopeNode.FocusedChild);
    }

    [Fact]
    public void FocusNode_RequestFocusOnAnUnparentedNodeIsAppliedWhenItIsReparented()
    {
        var manager = new FocusManager();
        var node = new FocusNode(debugLabel: "node");

        Assert.False(node.RequestFocus());
        Assert.Null(manager.PrimaryFocus);

        manager.RegisterNode(node);
        Scheduler.FlushMicrotasks();

        Assert.True(node.HasPrimaryFocus);
    }

    [Fact]
    public void FocusAttachment_DetachRemovesTheNodeFromTheTreeAndDropsItsFocus()
    {
        var manager = new FocusManager();
        var node = new FocusNode(debugLabel: "node");
        FocusAttachment attachment = node.Attach(context: null);
        manager.RootScope.Reparent(node);

        Assert.True(PumpFocus(node.RequestFocus));
        Assert.True(attachment.IsAttached);

        attachment.Detach();
        Scheduler.FlushMicrotasks();

        Assert.False(attachment.IsAttached);
        Assert.Null(node.Parent);
        Assert.False(node.HasPrimaryFocus);
    }

    [Fact]
    public void FocusScopeNode_AutofocusOnlyAppliesWhileTheScopeHasNoFocusedChild()
    {
        var manager = new FocusManager();
        var scopeNode = new FocusScopeNode(debugLabel: "scope");
        var taken = new FocusNode(debugLabel: "taken");
        var late = new FocusNode(debugLabel: "late");
        manager.RegisterNode(scopeNode);
        manager.RegisterNode(taken, scopeNode);
        manager.RegisterNode(late, scopeNode);

        Assert.True(taken.RequestFocus());
        scopeNode.Autofocus(late);
        Scheduler.FlushMicrotasks();

        Assert.Same(taken, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_EarlyAndLateKeyEventHandlersBracketTheFocusTree()
    {
        var manager = new FocusManager();
        var node = new FocusNode(debugLabel: "node");
        manager.RegisterNode(node);
        manager.RequestFocus(node);
        Scheduler.FlushMicrotasks();

        var order = new List<string>();
        node.OnKeyEvent = (_, _) =>
        {
            order.Add("node");
            return KeyEventResult.Ignored;
        };
        manager.AddEarlyKeyEventHandler(_ =>
        {
            order.Add("early");
            return KeyEventResult.Ignored;
        });
        manager.AddLateKeyEventHandler(_ =>
        {
            order.Add("late");
            return KeyEventResult.Handled;
        });

        Assert.True(manager.RouteKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA)));
        Assert.Equal(["early", "node", "late"], order);
    }

    [Fact]
    public void FocusManager_EarlyKeyEventHandlerCanSwallowTheEventBeforeTheFocusTree()
    {
        var manager = new FocusManager();
        var node = new FocusNode(debugLabel: "node");
        manager.RegisterNode(node);
        manager.RequestFocus(node);
        Scheduler.FlushMicrotasks();

        bool nodeSaw = false;
        node.OnKeyEvent = (_, _) =>
        {
            nodeSaw = true;
            return KeyEventResult.Handled;
        };
        manager.AddEarlyKeyEventHandler(_ => KeyEventResult.Handled);

        Assert.True(manager.RouteKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA)));
        Assert.False(nodeSaw);
    }

    [Fact]
    public void KeyEventResults_CombinePrefersHandledThenSkipRemainingHandlers()
    {
        Assert.Equal(KeyEventResult.Ignored, KeyEventResults.Combine([]));
        Assert.Equal(
            KeyEventResult.Ignored,
            KeyEventResults.Combine([KeyEventResult.Ignored, KeyEventResult.Ignored]));
        Assert.Equal(
            KeyEventResult.SkipRemainingHandlers,
            KeyEventResults.Combine([KeyEventResult.Ignored, KeyEventResult.SkipRemainingHandlers]));
        Assert.Equal(
            KeyEventResult.Handled,
            KeyEventResults.Combine([KeyEventResult.SkipRemainingHandlers, KeyEventResult.Handled]));
    }

    [Fact]
    public void FocusManager_SuspendsAndRestoresTheFocusAcrossAnAppLifecyclePause()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        try
        {
            var manager = new FocusManager();
            manager.ListenToApplicationLifecycleChangesIfSupported();
            var node = new FocusNode(debugLabel: "node");
            manager.RegisterNode(node);
            Assert.True(PumpFocus(node.RequestFocus));

            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            Assert.Same(manager.RootScope, manager.PrimaryFocus);

            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            Scheduler.FlushMicrotasks();
            Assert.Same(node, manager.PrimaryFocus);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        }
    }

    [Fact]
    public void FocusManager_DoesNotRestoreASuspendedNodeWhenSomethingElseTookTheFocus()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        try
        {
            var manager = new FocusManager();
            manager.ListenToApplicationLifecycleChangesIfSupported();
            var suspended = new FocusNode(debugLabel: "suspended");
            var other = new FocusNode(debugLabel: "other");
            manager.RegisterNode(suspended);
            manager.RegisterNode(other);
            Assert.True(PumpFocus(suspended.RequestFocus));

            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            Assert.True(other.RequestFocus());
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            Scheduler.FlushMicrotasks();

            Assert.Same(other, manager.PrimaryFocus);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
        }
    }

    [Fact]
    public void FocusManager_HighlightStrategyOverridesTheInteractionDrivenMode()
    {
        var manager = new FocusManager();

        manager.HighlightStrategy = FocusHighlightStrategy.AlwaysTouch;
        Assert.Equal(FocusHighlightMode.Touch, manager.HighlightMode);

        manager.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        Assert.Equal(FocusHighlightMode.Traditional, manager.HighlightMode);
    }

    private static bool PumpFocus(Func<bool> action)
    {
        bool result = action();
        Scheduler.FlushMicrotasks();
        return result;
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

    private sealed class TestTransform : SingleChildRenderObjectWidget
    {
        public TestTransform(Matrix4 transform, Widget child) : base(child)
        {
            Transform = transform;
        }

        public Matrix4 Transform { get; }

        internal override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderTransform(Transform);
        }

        internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderTransform)renderObject).Transform = Transform;
        }
    }
}
