using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/scrollable.dart (_ScrollSemantics, _RenderScrollSemantics)
// flutter/packages/flutter/lib/src/rendering/viewport.dart (useTwoPaneSemantics, excludeFromScrolling)
// flutter/packages/flutter/lib/src/rendering/sliver_persistent_header.dart
// flutter/packages/flutter/lib/src/widgets/pinned_header_sliver.dart
// flutter/packages/flutter/lib/src/widgets/scroll_delegate.dart (semantic indexes)

namespace Plumix.Tests;

internal sealed class ScrollSemanticsHarness
{
    private readonly BuildOwner _owner = new();
    private readonly HarnessRootElement _rootElement;
    private readonly PipelineOwner _pipeline;

    public ScrollSemanticsHarness(Widget rootWidget)
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

    /// <summary>Rebuilds the tree from a new root widget, the way a `setState` above it would.</summary>
    public void UpdateRoot(Widget rootWidget)
    {
        _rootElement.Update(rootWidget);
    }

    /// <summary>The element hosting the root widget, for `FindRenderObject`-style lookups.</summary>
    public Element RootElement => _rootElement;

    public void Pump(Size size)
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
        _pipeline.FlushSemantics();
    }

    public SemanticsNode? FindSemanticsNode(string label)
    {
        return FindSemanticsNode(_pipeline.SemanticsOwner!.RootNode, label);
    }

    public SemanticsNode? SemanticsRoot => _pipeline.SemanticsOwner!.RootNode;

    public string SemanticsDump => _pipeline.SemanticsOwner!.DebugDumpTree();

    public bool PerformSemanticsAction(int nodeId, SemanticsActions action, object? args = null)
    {
        return _pipeline.SemanticsOwner!.PerformAction(nodeId, action, args);
    }

    private static SemanticsNode? FindSemanticsNode(SemanticsNode? node, string label)
    {
        if (node is null)
        {
            return null;
        }

        if (node.Label == label)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindSemanticsNode(child, label) is { } match)
            {
                return match;
            }
        }

        return null;
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
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("HarnessRootElement expects null slot.");
            }

            if (child is not RenderBox renderBox)
            {
                throw new InvalidOperationException("HarnessRootElement can host only RenderBox.");
            }

            _renderView.Child = renderBox;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("HarnessRootElement does not support non-null slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("HarnessRootElement expects null slot.");
            }

            if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
            {
                _renderView.Child = null;
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
    }
}
