using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (MouseRegion)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderMouseRegion)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MouseRegionTests : IDisposable
{
    public MouseRegionTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructor_ExposesFlutterDefaults()
    {
        var region = new MouseRegion(child: new SizedBox(width: 10, height: 10));

        Assert.Null(region.OnEnter);
        Assert.Null(region.OnExit);
        Assert.Null(region.OnHover);
        Assert.True(region.Opaque);
        Assert.Equal(MouseCursor.Defer, region.Cursor);
    }

    [Fact]
    public void OnHover_FiresOnTheEnteringEventAndOnEverySubsequentMoveInside()
    {
        var events = new List<string>();
        using var harness = new WidgetHarness(
            new MouseRegion(
                child: new SizedBox(width: 60, height: 40),
                onEnter: _ => events.Add("enter"),
                onHover: e => events.Add($"hover:{e.Position.X}"),
                onExit: _ => events.Add("exit")));

        harness.Layout(new Size(200, 200));

        harness.SendPointer(Hover(new Point(10, 10)));
        Assert.Equal(["enter", "hover:10"], events);

        harness.SendPointer(Hover(new Point(20, 10)));
        Assert.Equal(["enter", "hover:10", "hover:20"], events);

        harness.SendPointer(Hover(new Point(120, 10)));
        Assert.Equal(["enter", "hover:10", "hover:20", "exit"], events);
    }

    [Fact]
    public void OnHover_IsNotRequiredForEnterAndExitDispatch()
    {
        int enters = 0;
        int exits = 0;
        using var harness = new WidgetHarness(
            new MouseRegion(
                child: new SizedBox(width: 60, height: 40),
                onEnter: _ => enters++,
                onExit: _ => exits++));

        harness.Layout(new Size(200, 200));

        harness.SendPointer(Hover(new Point(10, 10)));
        harness.SendPointer(Hover(new Point(120, 10)));

        Assert.Equal(1, enters);
        Assert.Equal(1, exits);
    }

    [Fact]
    public void OnHover_ReadsTheCallbackFromTheCurrentWidgetAfterAnUpdate()
    {
        var first = new List<double>();
        var second = new List<double>();

        using var harness = new WidgetHarness(
            new MouseRegion(
                child: new SizedBox(width: 60, height: 40),
                onHover: e => first.Add(e.Position.X)));

        harness.Layout(new Size(200, 200));
        harness.SendPointer(Hover(new Point(10, 10)));

        harness.Update(
            new MouseRegion(
                child: new SizedBox(width: 60, height: 40),
                onHover: e => second.Add(e.Position.X)));
        harness.Layout(new Size(200, 200));
        harness.SendPointer(Hover(new Point(30, 10)));

        Assert.Equal([10.0], first);
        Assert.Equal([30.0], second);
    }

    private static PointerHoverEvent Hover(Point position) => new(
        pointer: 1,
        kind: PointerDeviceKind.Mouse,
        position: position,
        buttons: PointerButtons.None,
        timestampUtc: DateTime.UtcNow);

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _rootElement;

        public WidgetHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, widget);
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

        public void Layout(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
        }

        public void SendPointer(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(RenderView, @event);
            _owner.FlushBuild();
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

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            public override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public override void Unmount()
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
        }
    }
}
