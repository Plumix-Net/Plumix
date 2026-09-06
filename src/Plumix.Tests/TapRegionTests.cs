using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/tap_region.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class TapRegionTests
{
    [Fact]
    public void TapRegion_DefaultsAndTextFieldGroupingMatchFlutter()
    {
        var child = new SizedBox(width: 20, height: 20);
        var region = new TapRegion(child);

        Assert.Same(child, region.Child);
        Assert.True(region.Enabled);
        Assert.Equal(HitTestBehavior.DeferToChild, region.Behavior);
        Assert.Null(region.OnTapOutside);
        Assert.Null(region.OnTapInside);
        Assert.Null(region.OnTapUpOutside);
        Assert.Null(region.OnTapUpInside);
        Assert.Null(region.GroupId);
        Assert.False(region.ConsumeOutsideTaps);
        Assert.Null(region.DebugLabel);

        var textFieldRegion = new TextFieldTapRegion(child);
        Assert.Equal(typeof(EditableText), textFieldRegion.GroupId);
    }

    [Fact]
    public void TapRegion_GroupMembersActAsOneForDownAndUp()
    {
        object group = new();
        int firstInsideDown = 0;
        int secondInsideDown = 0;
        int firstInsideUp = 0;
        int secondInsideUp = 0;
        int outsideDown = 0;
        int outsideUp = 0;

        using var harness = new WidgetHarness(
            new TapRegionSurface(
                child: new Stack(
                    fit: StackFit.Expand,
                    children:
                    [
                        new Positioned(
                            left: 0,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                groupId: group,
                                behavior: HitTestBehavior.Opaque,
                                onTapInside: _ => firstInsideDown += 1,
                                onTapUpInside: _ => firstInsideUp += 1,
                                child: new SizedBox())),
                        new Positioned(
                            left: 80,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                groupId: group,
                                behavior: HitTestBehavior.Opaque,
                                onTapInside: _ => secondInsideDown += 1,
                                onTapUpInside: _ => secondInsideUp += 1,
                                child: new SizedBox())),
                        new Positioned(
                            left: 160,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                behavior: HitTestBehavior.Opaque,
                                onTapOutside: _ => outsideDown += 1,
                                onTapUpOutside: _ => outsideUp += 1,
                                child: new SizedBox())),
                    ])));

        DateTime now = DateTime.UtcNow;
        harness.Dispatch(new PointerDownEvent(
            1,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.Primary,
            now));
        harness.Dispatch(new PointerUpEvent(
            1,
            PointerDeviceKind.Mouse,
            new Point(10, 10),
            PointerButtons.None,
            now.AddMilliseconds(20)));

        Assert.Equal(1, firstInsideDown);
        Assert.Equal(1, secondInsideDown);
        Assert.Equal(1, firstInsideUp);
        Assert.Equal(1, secondInsideUp);
        Assert.Equal(1, outsideDown);
        Assert.Equal(1, outsideUp);
    }

    [Fact]
    public void TapRegion_DisabledRegionDoesNotRegister()
    {
        using var harness = new WidgetHarness(
            new TapRegionSurface(
                child: new TapRegion(
                    enabled: false,
                    behavior: HitTestBehavior.Opaque,
                    child: new SizedBox(width: 40, height: 40))));

        RenderTapRegionSurface surface = harness.FindRenderObject<RenderTapRegionSurface>();
        Assert.Equal(0, surface.RegisteredRegionCount);
    }

    [Fact]
    public void TapRegion_ConsumeOutsideTapsWinsGestureArena()
    {
        int outsideCalls = 0;
        int targetTaps = 0;
        using var harness = new WidgetHarness(
            new TapRegionSurface(
                child: new Stack(
                    fit: StackFit.Expand,
                    children:
                    [
                        new Positioned(
                            left: 0,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                consumeOutsideTaps: true,
                                onTapOutside: _ => outsideCalls += 1,
                                behavior: HitTestBehavior.Opaque,
                                child: new SizedBox())),
                        new Positioned(
                            left: 80,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new GestureDetector(
                                behavior: HitTestBehavior.Opaque,
                                onTap: () => targetTaps += 1,
                                child: new SizedBox())),
                    ])));

        DateTime now = DateTime.UtcNow;
        harness.Dispatch(new PointerDownEvent(
            2,
            PointerDeviceKind.Mouse,
            new Point(90, 10),
            PointerButtons.Primary,
            now));
        harness.Dispatch(new PointerUpEvent(
            2,
            PointerDeviceKind.Mouse,
            new Point(90, 10),
            PointerButtons.None,
            now.AddMilliseconds(20)));

        Assert.Equal(1, outsideCalls);
        Assert.Equal(0, targetTaps);
    }

    [Fact]
    public void TapRegion_OutsideCallbacksAreDisabledWhenRouteIsNotCurrent()
    {
        int backgroundOutsideCalls = 0;
        var initialRoute = new BuilderPageRoute(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 0,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new TapRegion(
                            behavior: HitTestBehavior.Opaque,
                            onTapOutside: _ => backgroundOutsideCalls += 1,
                            child: new SizedBox())),
                ]));
        using var harness = new WidgetHarness(
            new TapRegionSurface(
                child: new Navigator(initialRoute)));
        NavigatorState navigator = harness.FindState<NavigatorState>();
        navigator.Push(new NonOpaquePageRoute(_ =>
            new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(
                        left: 80,
                        top: 0,
                        width: 40,
                        height: 40,
                        child: new GestureDetector(
                            behavior: HitTestBehavior.Opaque,
                            child: new SizedBox())),
                ])));
        harness.Pump();

        harness.Dispatch(new PointerDownEvent(
            3,
            PointerDeviceKind.Mouse,
            new Point(90, 10),
            PointerButtons.Primary,
            DateTime.UtcNow));

        Assert.Equal(0, backgroundOutsideCalls);
    }

    [Fact]
    public void TapRegion_SemanticsTapReachesInsideAndOutsideRegions()
    {
        int insideCalls = 0;
        int outsideCalls = 0;
        int targetTaps = 0;
        using var harness = new WidgetHarness(
            new TapRegionSurface(
                child: new Stack(
                    fit: StackFit.Expand,
                    children:
                    [
                        new Positioned(
                            left: 0,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                behavior: HitTestBehavior.Opaque,
                                onTapInside: _ => insideCalls += 1,
                                child: new Semantics(
                                    label: "inside",
                                    container: true,
                                    onTap: () => targetTaps += 1,
                                    child: new SizedBox()))),
                        new Positioned(
                            left: 80,
                            top: 0,
                            width: 40,
                            height: 40,
                            child: new TapRegion(
                                behavior: HitTestBehavior.Opaque,
                                onTapOutside: _ => outsideCalls += 1,
                                child: new SizedBox())),
                    ])));

        SemanticsOwner semantics = harness.PumpSemantics();
        SemanticsNode? node = FindNode(semantics.RootNode, static candidate => candidate.Label == "inside");
        Assert.NotNull(node);

        Assert.True(semantics.PerformAction(node!.Id, SemanticsActions.Tap));

        // The accessibility tap hit-tests the surface at the node's centre, so the region that owns
        // the node counts as inside and every other registered region as outside.
        Assert.Equal(1, targetTaps);
        Assert.Equal(1, insideCalls);
        Assert.Equal(1, outsideCalls);

        // Actions the surface does not care about leave the regions alone.
        Assert.False(semantics.PerformAction(node.Id, SemanticsActions.Dismiss));
        Assert.Equal(1, insideCalls);
        Assert.Equal(1, outsideCalls);
    }

    private static SemanticsNode? FindNode(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null)
        {
            return null;
        }

        if (predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindNode(child, predicate) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly TestRootElement _root;
        private readonly RenderView _renderView;
        private readonly PipelineOwner _pipeline;

        public WidgetHarness(Widget widget)
        {
            GestureBinding.Instance.ResetForTests();
            // Stack's default alignment is AlignmentDirectional.topStart, which needs a direction.
            _root = new TestRootElement(new Directionality(TextDirection.Ltr, widget));
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            _renderView = new RenderView
            {
                Child = Assert.IsAssignableFrom<RenderBox>(_root.ChildElement?.RenderObject),
            };
            _pipeline = new PipelineOwner(_renderView);
            _pipeline.Attach(_renderView);
            Pump();
        }

        public void Dispatch(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(_renderView, @event);
        }

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.FlushLayout(new Size(240, 120));
        }

        public SemanticsOwner PumpSemantics()
        {
            Pump();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!;
        }

        public T FindRenderObject<T>() where T : RenderObject
        {
            T? result = null;
            Visit(_renderView);
            return Assert.IsType<T>(result);

            void Visit(RenderObject renderObject)
            {
                if (result is not null)
                {
                    return;
                }

                if (renderObject is T typed)
                {
                    result = typed;
                    return;
                }

                renderObject.VisitChildren(Visit);
            }
        }

        public T FindState<T>() where T : State
        {
            T? result = null;
            Visit(_root);
            return Assert.IsType<T>(result);

            void Visit(Element element)
            {
                if (result is not null)
                {
                    return;
                }

                if (element is StatefulElement { State: T state })
                {
                    result = state;
                    return;
                }

                element.VisitChildren(Visit);
            }
        }

        public void Dispose()
        {
            GestureBinding.Instance.ResetForTests();
            _root.Unmount();
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
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
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects a null slot.");
            }
        }
    }

    private sealed class NonOpaquePageRoute : PageRoute
    {
        private readonly WidgetBuilder _builder;

        public NonOpaquePageRoute(WidgetBuilder builder)
        {
            _builder = builder;
        }

        public override bool Opaque => false;

        public override Widget BuildPage(BuildContext context) => _builder(context);
    }
}
