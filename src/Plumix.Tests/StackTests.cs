using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class StackTests
{
    [Fact]
    public void RenderStack_NonPositionedChild_UsesAlignment()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack(alignment: Alignment.BottomRight);
        stack.Insert(child);
        var constrained = new RenderConstrainedBox(
            additionalConstraints: BoxConstraints.TightFor(width: 100, height: 80),
            child: stack);
        var root = new RenderView
        {
            Child = constrained
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(100, 80), stack.Size);
        Assert.Equal(new Point(80, 70), ((StackParentData)child.parentData!).offset);
    }

    [Fact]
    public void RenderStack_PositionedChild_UsesLeftTop()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack();
        stack.Insert(child);
        var parentData = (StackParentData)child.parentData!;
        parentData.Left = 5;
        parentData.Top = 7;
        var root = new RenderView
        {
            Child = stack
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Point(5, 7), parentData.offset);
    }

    [Fact]
    public void RenderStack_PositionedChild_UsesRightBottom()
    {
        var child = new FixedSizeRenderBox(new Size(20, 10));
        var stack = new RenderStack();
        stack.Insert(child);
        var parentData = (StackParentData)child.parentData!;
        parentData.Right = 6;
        parentData.Bottom = 4;
        var root = new RenderView
        {
            Child = stack
        };

        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Point(74, 66), parentData.offset);
    }

    [Fact]
    public void StackWidget_PositionedParentData_AppliesAndUpdates()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Stack(
                alignment: Alignment.TopLeft,
                children:
                [
                    new Positioned(
                        left: 3,
                        top: 4,
                        child: new SizedBox(width: 10, height: 10)),
                ]));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var stack = RequireRenderObject<RenderStack>(root.ChildElement);
        var firstChild = Assert.IsAssignableFrom<RenderBox>(stack.FirstChild);
        var parentData = Assert.IsType<StackParentData>(firstChild.parentData);
        Assert.Equal(3, parentData.Left);
        Assert.Equal(4, parentData.Top);
        Assert.Null(parentData.Right);
        Assert.Null(parentData.Bottom);

        root.Update(new Stack(
            alignment: Alignment.BottomRight,
            children:
            [
                new Positioned(
                    right: 2,
                    bottom: 5,
                    child: new SizedBox(width: 10, height: 10)),
            ]));
        owner.FlushBuild();

        var updatedStack = RequireRenderObject<RenderStack>(root.ChildElement);
        Assert.True(ReferenceEquals(stack, updatedStack));
        Assert.Equal(Alignment.BottomRight, updatedStack.Alignment);
        var updatedFirstChild = Assert.IsAssignableFrom<RenderBox>(updatedStack.FirstChild);
        var updatedParentData = Assert.IsType<StackParentData>(updatedFirstChild.parentData);
        Assert.Null(updatedParentData.Left);
        Assert.Null(updatedParentData.Top);
        Assert.Equal(2, updatedParentData.Right);
        Assert.Equal(5, updatedParentData.Bottom);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class FixedSizeRenderBox : RenderBox
    {
        private readonly Size _size;

        public FixedSizeRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        public override void Paint(PaintingContext ctx, Point offset)
        {
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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
}
