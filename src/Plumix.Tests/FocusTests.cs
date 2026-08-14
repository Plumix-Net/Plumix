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
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void FocusManager_RequestFocus_UpdatesPrimaryFocusAndNodeFlags()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();

        manager.RegisterNode(first);
        manager.RegisterNode(second);

        Assert.True(manager.RequestFocus(first));
        Assert.Same(first, manager.PrimaryFocus);
        Assert.True(first.HasFocus);
        Assert.False(second.HasFocus);

        Assert.True(manager.RequestFocus(second));
        Assert.Same(second, manager.PrimaryFocus);
        Assert.False(first.HasFocus);
        Assert.True(second.HasFocus);
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

        bool movedForward = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Assert.True(movedForward);
        Assert.Same(second, manager.PrimaryFocus);

        bool movedBackward = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab, shift: true));
        Assert.True(movedBackward);
        Assert.Same(first, manager.PrimaryFocus);

        manager.RequestFocus(second);
        bool movedPastLast = manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
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
        Assert.Same(leftFirst, leftScope.FocusedChild);

        Assert.True(manager.FocusNext());
        Assert.Same(leftSecond, manager.PrimaryFocus);
        Assert.Same(leftSecond, leftScope.FocusedChild);

        // The default closed loop wraps within the scope instead of escaping into the sibling scope.
        Assert.True(manager.FocusNext());
        Assert.Same(leftFirst, manager.PrimaryFocus);
        Assert.False(rightOnly.HasFocus);

        Assert.True(manager.FocusPrevious());
        Assert.Same(leftSecond, manager.PrimaryFocus);
        Assert.False(rightOnly.HasFocus);

        leftScope.TraversalEdgeBehavior = TraversalEdgeBehavior.Stop;
        Assert.False(manager.FocusNext());
        Assert.Same(leftSecond, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_DirectionalKeys_FollowTraversalOrder()
    {
        var manager = new FocusManager();
        var first = new FocusNode();
        var second = new FocusNode();
        var third = new FocusNode();

        manager.RegisterNode(first);
        manager.RegisterNode(second);
        manager.RegisterNode(third);
        manager.RequestFocus(first);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Same(second, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.Same(third, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Same(second, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp)));
        Assert.Same(first, manager.PrimaryFocus);

        Assert.False(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
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

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.Same(down, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Same(diagonal, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp)));
        Assert.Same(right, manager.PrimaryFocus);

        Assert.True(manager.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft)));
        Assert.Same(source, manager.PrimaryFocus);
    }

    [Fact]
    public void FocusManager_DirectionalKeys_ResolveTraversalRectsThroughRenderTransforms()
    {
        var owner = new BuildOwner();
        var source = new FocusNode();
        var right = new FocusNode();
        var transformedDown = new FocusNode();
        var root = new TestRootElement(
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
                    transform: Matrix.CreateTranslation(-120, 120),
                    child: new Focus(
                        focusNode: transformedDown,
                        child: new SizedBox(width: 20, height: 20))),
            ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Same(source, FocusManager.Instance.PrimaryFocus);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight)));
        Assert.Same(right, FocusManager.Instance.PrimaryFocus);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowDown)));
        Assert.Same(transformedDown, FocusManager.Instance.PrimaryFocus);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowUp)));
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

        Assert.True(handled);
        Assert.Same(second, FocusManager.Instance.PrimaryFocus);
        Assert.False(first.HasFocus);
        Assert.True(second.HasFocus);
    }

    [Fact]
    public void FocusScopeWidget_TabTraversal_DoesNotEscapeScopeBoundaries()
    {
        var owner = new BuildOwner();
        var leadingSibling = new FocusNode();
        var firstInScope = new FocusNode();
        var secondInScope = new FocusNode();
        var trailingSibling = new FocusNode();

        var root = new TestRootElement(
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
            ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        // The default closed loop wraps within the scope instead of escaping into the siblings.
        bool movedBeforeScopeStart = FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.Tab, shift: true));
        Assert.True(movedBeforeScopeStart);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(leadingSibling.HasFocus);

        bool movedInsideScope = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Assert.True(movedInsideScope);
        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        bool movedAfterScopeEnd = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Assert.True(movedAfterScopeEnd);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(trailingSibling.HasFocus);
    }

    [Fact]
    public void FocusScopeWidget_DirectionalTraversal_DoesNotEscapeScopeBoundaries()
    {
        var owner = new BuildOwner();
        var leadingSibling = new FocusNode();
        var firstInScope = new FocusNode();
        var secondInScope = new FocusNode();
        var trailingSibling = new FocusNode();

        var root = new TestRootElement(
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
            ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);

        bool movedBeforeScopeStart = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowLeft));
        Assert.False(movedBeforeScopeStart);
        Assert.Same(firstInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(leadingSibling.HasFocus);

        bool movedInsideScope = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight));
        Assert.True(movedInsideScope);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);

        bool movedAfterScopeEnd = FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.ArrowRight));
        Assert.False(movedAfterScopeEnd);
        Assert.Same(secondInScope, FocusManager.Instance.PrimaryFocus);
        Assert.False(trailingSibling.HasFocus);
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
        public TestTransform(Matrix transform, Widget child) : base(child)
        {
            Transform = transform;
        }

        public Matrix Transform { get; }

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
