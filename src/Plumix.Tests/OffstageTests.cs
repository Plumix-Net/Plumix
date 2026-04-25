using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class OffstageTests
{
    [Fact]
    public void RenderOffstage_True_LayoutsChildButTakesNoSpace()
    {
        var child = new HitTestRenderBox(new Size(50, 30));
        var offstage = new RenderOffstage(offstage: true, child: child);
        var root = new RenderView
        {
            Child = offstage
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(0, 0), offstage.Size);
        Assert.Equal(new Size(50, 30), child.Size);

        var result = new BoxHitTestResult();
        Assert.False(offstage.HitTest(result, new Point(0, 0)));
    }

    [Fact]
    public void RenderOffstage_False_BehavesLikeProxyBoxAndHitTestsChild()
    {
        var child = new HitTestRenderBox(new Size(50, 30));
        var offstage = new RenderOffstage(offstage: false, child: child);
        var root = new RenderView
        {
            Child = offstage
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));

        Assert.Equal(new Size(50, 30), offstage.Size);
        Assert.Equal(new Point(0, 0), ((BoxParentData)child.parentData!).offset);

        var result = new BoxHitTestResult();
        Assert.True(offstage.HitTest(result, new Point(10, 10)));
    }

    [Fact]
    public void RenderOffstage_Toggle_UpdatesLayoutSize()
    {
        var child = new HitTestRenderBox(new Size(60, 40));
        var offstage = new RenderOffstage(offstage: false, child: child);
        var root = new RenderView
        {
            Child = offstage
        };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);

        pipeline.FlushLayout(new Size(100, 80));
        Assert.Equal(new Size(60, 40), offstage.Size);

        offstage.Offstage = true;
        pipeline.FlushLayout(new Size(100, 80));
        Assert.Equal(new Size(0, 0), offstage.Size);
    }

    [Fact]
    public void OffstageWidget_CreatesRenderObject_AndUpdatesProperty()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Offstage(
                offstage: true,
                child: new SizedBox(width: 10, height: 10)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderBox = RequireRenderObject<RenderOffstage>(root.ChildElement);
        Assert.True(renderBox.Offstage);

        root.Update(new Offstage(
            offstage: false,
            child: new SizedBox(width: 10, height: 10)));
        owner.FlushBuild();

        var updated = RequireRenderObject<RenderOffstage>(root.ChildElement);
        Assert.Same(renderBox, updated);
        Assert.False(updated.Offstage);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        Assert.NotNull(element!.RenderObject);
        return Assert.IsType<T>(element.RenderObject);
    }

    private sealed class HitTestRenderBox : RenderBox
    {
        private readonly Size _size;

        public HitTestRenderBox(Size size)
        {
            _size = size;
        }

        protected override void PerformLayout()
        {
            Size = Constraints.Constrain(_size);
        }

        protected override bool HitTestSelf(Point position)
        {
            return true;
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
