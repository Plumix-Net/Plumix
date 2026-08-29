using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/keyboard_listener.dart;
// flutter/packages/flutter/lib/src/widgets/raw_keyboard_listener.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class KeyboardListenerTests : IDisposable
{
    public KeyboardListenerTests()
    {
        FocusManager.Instance.ResetForTests();
#pragma warning disable CS0618
        RawKeyboard.Instance.ClearKeysPressed();
        RawKeyboard.Instance.ClearListeners();
#pragma warning restore CS0618
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
#pragma warning disable CS0618
        RawKeyboard.Instance.ClearKeysPressed();
        RawKeyboard.Instance.ClearListeners();
#pragma warning restore CS0618
    }

    [Fact]
    public void KeyboardListener_ExposesFlutterDefaultsAndRequiredArguments()
    {
        var focusNode = new FocusNode();
        var child = new SizedBox(width: 12, height: 8);
        var listener = new KeyboardListener(focusNode: focusNode, child: child);

        Assert.Same(focusNode, listener.FocusNode);
        Assert.Same(child, listener.Child);
        Assert.False(listener.Autofocus);
        Assert.True(listener.IncludeSemantics);
        Assert.Null(listener.OnKeyEvent);
        Assert.Throws<ArgumentNullException>(() => new KeyboardListener(null!, child));
        Assert.Throws<ArgumentNullException>(() => new KeyboardListener(focusNode, null!));

#pragma warning disable CS0618
        var rawListener = new RawKeyboardListener(focusNode: focusNode, child: child);
        Assert.Same(focusNode, rawListener.FocusNode);
        Assert.Same(child, rawListener.Child);
        Assert.False(rawListener.Autofocus);
        Assert.True(rawListener.IncludeSemantics);
        Assert.Null(rawListener.OnKey);
        Assert.Throws<ArgumentNullException>(() => new RawKeyboardListener(null!, child));
        Assert.Throws<ArgumentNullException>(() => new RawKeyboardListener(focusNode, null!));
#pragma warning restore CS0618
    }

    [Fact]
    public void KeyboardListener_AutofocusDispatchesDownAndUpWithoutConsumingThem()
    {
        var focusNode = new FocusNode();
        var events = new List<KeyEvent>();
        using var harness = new WidgetRenderHarness(
            new KeyboardListener(
                focusNode: focusNode,
                autofocus: true,
                onKeyEvent: events.Add,
                child: new SizedBox(width: 20, height: 12)));

        Assert.True(focusNode.HasFocus);
        var down = KeySim.Down(LogicalKeyboardKey.KeyA);
        var up = KeySim.Up(LogicalKeyboardKey.KeyA);

        Assert.False(FocusManager.Instance.HandleKeyEvent(down));
        Assert.False(FocusManager.Instance.HandleKeyEvent(up));
        Assert.Equal([down, up], events);
    }

    [Fact]
    public void KeyboardListener_IncludeSemanticsControlsFocusableNodeAndFocusAction()
    {
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(
            new KeyboardListener(
                focusNode: focusNode,
                includeSemantics: true,
                child: new SizedBox(width: 20, height: 12)));

        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(120, 60));
        SemanticsNode focusable = Assert.Single(FindNodes(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsFocusable)));
        Assert.False(focusable.Flags.HasFlag(SemanticsFlags.IsFocused));
        Assert.True(focusable.Actions.HasFlag(SemanticsActions.Focus));
        Assert.True(harness.PerformSemanticsAction(focusable.Id, SemanticsActions.Focus));
        Assert.True(focusNode.HasFocus);

        semantics = harness.PumpAndGetSemantics(new Size(120, 60));
        focusable = Assert.Single(FindNodes(
            semantics,
            node => node.Flags.HasFlag(SemanticsFlags.IsFocusable)));
        Assert.True(focusable.Flags.HasFlag(SemanticsFlags.IsFocused));

        harness.Update(new KeyboardListener(
            focusNode: focusNode,
            includeSemantics: false,
            child: new SizedBox(width: 20, height: 12)));
        semantics = harness.PumpAndGetSemantics(new Size(120, 60));
        Assert.Empty(FindNodes(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsFocusable)));
    }

    [Fact]
    public void KeyboardListener_PreservesExternalFocusNodePolicies()
    {
        var focusNode = new FocusNode
        {
            CanRequestFocus = false,
            SkipTraversal = true
        };
        using var harness = new WidgetRenderHarness(
            new KeyboardListener(
                focusNode: focusNode,
                autofocus: true,
                child: new SizedBox(width: 20, height: 12)));

        Assert.False(focusNode.CanRequestFocus);
        Assert.True(focusNode.SkipTraversal);
        Assert.False(focusNode.HasFocus);

        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(120, 60));
        Assert.Empty(FindNodes(semantics, node => node.Flags.HasFlag(SemanticsFlags.IsFocusable)));
    }

    [Fact]
    public void RawKeyboardListener_ReceivesRawEventsOnlyWhileItsNodeHasFocus()
    {
#pragma warning disable CS0618
        var focusNode = new FocusNode();
        var rawEvents = new List<RawKeyEvent>();
        using var harness = new WidgetRenderHarness(
            new RawKeyboardListener(
                focusNode: focusNode,
                autofocus: true,
                onKey: rawEvents.Add,
                child: new SizedBox(width: 20, height: 12)));

        KeySim.DispatchRaw(LogicalKeyboardKey.Enter, down: true);
        KeySim.DispatchRaw(LogicalKeyboardKey.Enter, down: false);

        Assert.Collection(
            rawEvents,
            first => Assert.IsType<RawKeyDownEvent>(first),
            second => Assert.IsType<RawKeyUpEvent>(second));
        Assert.All(rawEvents, @event => Assert.Equal(LogicalKeyboardKey.Enter, @event.LogicalKey));

        focusNode.Unfocus();
        KeySim.DispatchRaw(LogicalKeyboardKey.Escape, down: true);
        Assert.Equal(2, rawEvents.Count);
#pragma warning restore CS0618
    }

    [Fact]
    public void RawKeyboardListener_RebindsFocusNodeAndDetachesOnDispose()
    {
#pragma warning disable CS0618
        var firstNode = new FocusNode();
        var secondNode = new FocusNode();
        int eventCount = 0;
        var harness = new WidgetRenderHarness(
            new RawKeyboardListener(
                focusNode: firstNode,
                autofocus: true,
                onKey: _ => eventCount += 1,
                child: new SizedBox(width: 20, height: 12)));

        harness.Update(new RawKeyboardListener(
            focusNode: secondNode,
            autofocus: true,
            onKey: _ => eventCount += 1,
            child: new SizedBox(width: 20, height: 12)));
        Assert.False(firstNode.HasFocus);
        Assert.True(secondNode.HasFocus);

        KeySim.DispatchRaw(LogicalKeyboardKey.Space, down: true);
        Assert.Equal(1, eventCount);

        harness.Dispose();
        KeySim.DispatchRaw(LogicalKeyboardKey.Space, down: false);
        Assert.Equal(1, eventCount);
#pragma warning restore CS0618
    }

    private static List<SemanticsNode> FindNodes(
        SemanticsNode? root,
        Func<SemanticsNode, bool> predicate)
    {
        var result = new List<SemanticsNode>();
        if (root is null)
        {
            return result;
        }

        if (predicate(root))
        {
            result.Add(root);
        }

        foreach (SemanticsNode child in root.Children)
        {
            result.AddRange(FindNodes(child, predicate));
        }

        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Update(Widget widget)
        {
            _rootElement.Update(widget);
            _owner.FlushBuild();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
        {
            return _pipeline.SemanticsOwner!.PerformAction(nodeId, action);
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

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

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
