using Avalonia;
using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/raw_menu_anchor.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class RawMenuAnchorTests : IDisposable
{
    public RawMenuAnchorTests()
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
    public void MenuController_DetachedOperationsMatchSourceAsymmetry()
    {
        var controller = new MenuController();

        Assert.False(controller.IsOpen);
        controller.Close();
        Assert.Throws<InvalidOperationException>(() => controller.Open());
        Assert.Throws<InvalidOperationException>(controller.CloseChildren);
    }

    [Fact]
    public void MenuTraversalShortcuts_MatchTheSourceSixEntryMap()
    {
        IReadOnlyDictionary<ShortcutActivator, Intent> shortcuts = RawMenuAnchor.MenuTraversalShortcuts;

        Assert.Equal(6, shortcuts.Count);
        Assert.IsType<ActivateIntent>(shortcuts[new SingleActivator("GameButtonA")]);
        Assert.IsType<DismissIntent>(shortcuts[new SingleActivator("Escape")]);
        Assert.Equal(
            TraversalDirection.Down,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowDown")]).Direction);
        Assert.Equal(
            TraversalDirection.Up,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowUp")]).Direction);
        Assert.Equal(
            TraversalDirection.Left,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowLeft")]).Direction);
        Assert.Equal(
            TraversalDirection.Right,
            ((DirectionalFocusIntent)shortcuts[new SingleActivator("ArrowRight")]).Direction);
    }

    [Fact]
    public void Anchor_OpenAndCloseToggleTheOverlayAndFireCallbacksOnce()
    {
        var controller = new MenuController();
        int opened = 0;
        int closed = 0;
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
            onOpen: () => opened++,
            onClose: () => closed++,
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));

        Assert.False(controller.IsOpen);
        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);
        Assert.Equal(1, opened);

        controller.Close();
        harness.Pump(new Size(400, 300));
        Assert.False(controller.IsOpen);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void Anchor_OpeningASiblingClosesThePreviouslyOpenSibling()
    {
        var groupController = new MenuController();
        var first = new MenuController();
        var second = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchorGroup(
            controller: groupController,
            child: new Row(children:
            [
                Anchor(first),
                Anchor(second),
            ]))));
        harness.Pump(new Size(400, 300));

        first.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(first.IsOpen);
        Assert.True(groupController.IsOpen);

        second.Open();
        harness.Pump(new Size(400, 300));
        Assert.False(first.IsOpen);
        Assert.True(second.IsOpen);

        groupController.Close();
        harness.Pump(new Size(400, 300));
        Assert.False(second.IsOpen);
        Assert.False(groupController.IsOpen);
    }

    [Fact]
    public void Group_OpenIsANoOpAndCloseChildrenClosesEveryChildAnchor()
    {
        var groupController = new MenuController();
        var child = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchorGroup(
            controller: groupController,
            child: Anchor(child))));
        harness.Pump(new Size(400, 300));

        groupController.Open();
        harness.Pump(new Size(400, 300));
        Assert.False(groupController.IsOpen);

        child.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(groupController.IsOpen);

        groupController.CloseChildren();
        harness.Pump(new Size(400, 300));
        Assert.False(child.IsOpen);
        Assert.False(groupController.IsOpen);
    }

    [Fact]
    public void Anchor_CloseChildrenClosesSubmenusButLeavesTheMenuOpen()
    {
        var root = new MenuController();
        var nested = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: root,
            overlayBuilder: (_, _) => Anchor(nested),
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));

        root.Open();
        harness.Pump(new Size(400, 300));
        nested.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(nested.IsOpen);

        root.CloseChildren();
        harness.Pump(new Size(400, 300));
        Assert.False(nested.IsOpen);
        Assert.True(root.IsOpen);
    }

    [Fact]
    public void OnOpenRequested_SwallowingShowOverlayPreventsTheMenuFromOpening()
    {
        var controller = new MenuController();
        int requests = 0;
        int opened = 0;
        Action? capturedShowOverlay = null;
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
            onOpen: () => opened++,
            onOpenRequested: (_, showOverlay) =>
            {
                requests++;
                capturedShowOverlay = showOverlay;
            },
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));

        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.Equal(1, requests);
        Assert.False(controller.IsOpen);
        Assert.Equal(0, opened);

        capturedShowOverlay!();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);
        Assert.Equal(1, opened);
    }

    [Fact]
    public void OnCloseRequested_DeferringHideOverlayKeepsTheMenuOpenUntilItRuns()
    {
        var controller = new MenuController();
        int closed = 0;
        Action? capturedHideOverlay = null;
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
            onClose: () => closed++,
            onCloseRequested: hideOverlay => capturedHideOverlay = hideOverlay,
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));

        controller.Open();
        harness.Pump(new Size(400, 300));
        controller.Close();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);
        Assert.Equal(0, closed);

        capturedHideOverlay!();
        harness.Pump(new Size(400, 300));
        Assert.False(controller.IsOpen);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void DismissMenuAction_ClosesTheWholeTreeFromTheRootAnchor()
    {
        var root = new MenuController();
        var nested = new MenuController();
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: root,
            overlayBuilder: (_, _) => Anchor(nested),
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));
        root.Open();
        harness.Pump(new Size(400, 300));
        nested.Open();
        harness.Pump(new Size(400, 300));

        var action = new DismissMenuAction(nested);
        Assert.True(action.IsEnabled(new DismissIntent()));
        action.Invoke(new DismissIntent());
        harness.Pump(new Size(400, 300));

        Assert.False(root.IsOpen);
        Assert.False(nested.IsOpen);
        Assert.False(new DismissMenuAction(new MenuController()).IsEnabled(new DismissIntent()));
    }

    [Fact]
    public void Anchor_ClosesWhenAnAncestorScrolls()
    {
        var controller = new MenuController();
        var scrollController = new ScrollController();
        using var harness = new WidgetRenderHarness(Wrap(new SingleChildScrollView(
            controller: scrollController,
            child: new SizedBox(
                height: 2000,
                child: new RawMenuAnchor(
                    controller: controller,
                    overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
                    child: new SizedBox(width: 80, height: 40))))));
        harness.Pump(new Size(400, 300));

        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);

        // A scroll activity flips `isScrollingNotifier`, which is what the anchor listens to.
        scrollController.Position.AnimateTo(400.0, TimeSpan.FromMilliseconds(100));
        harness.Pump(new Size(400, 300));
        Assert.False(controller.IsOpen);
    }

    [Fact]
    public void Anchor_ClosesWhenTheViewSizeChanges()
    {
        var controller = new MenuController();
        Widget Build(Size size) => new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: size),
                new Overlay(initialEntries: [new OverlayEntry(_ => new RawMenuAnchor(
                    controller: controller,
                    overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
                    child: new SizedBox(width: 80, height: 40)))])));

        using var harness = new WidgetRenderHarness(Build(new Size(400, 300)));
        harness.Pump(new Size(400, 300));
        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);

        harness.Update(Build(new Size(200, 200)));
        harness.Pump(new Size(200, 200));
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(200, 200));
        Assert.False(controller.IsOpen);
    }

    [Fact]
    public void MaybeIsOpenOf_TracksTheNearestAnchorWhileMaybeOfReturnsItsController()
    {
        var controller = new MenuController();
        var seen = new List<bool>();
        MenuController? resolved = null;
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
            builder: (context, _, _) => new Builder(inner =>
            {
                seen.Add(MenuController.MaybeIsOpenOf(inner) ?? false);
                resolved = MenuController.MaybeOf(inner);
                return new SizedBox(width: 80, height: 40);
            }))));
        harness.Pump(new Size(400, 300));

        Assert.Equal([false], seen);
        Assert.Same(controller, resolved);

        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.Equal([false, true], seen);
    }

    [Fact]
    public void Anchor_DetachesItsControllerWhenTheStateIsDisposed()
    {
        var controller = new MenuController();
        var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));
        controller.Open();
        harness.Pump(new Size(400, 300));
        Assert.True(controller.IsOpen);

        harness.Dispose();

        Assert.False(controller.IsOpen);
        Assert.Throws<InvalidOperationException>(controller.CloseChildren);
    }

    [Fact]
    public void OverlayInfo_ReportsTheAnchorRectTheOverlaySizeAndTheRequestedPosition()
    {
        var controller = new MenuController();
        RawMenuOverlayInfo? info = null;
        using var harness = new WidgetRenderHarness(Wrap(new RawMenuAnchor(
            controller: controller,
            overlayBuilder: (_, overlayInfo) =>
            {
                info = overlayInfo;
                return new SizedBox(width: 40, height: 40);
            },
            child: new SizedBox(width: 80, height: 40))));
        harness.Pump(new Size(400, 300));

        controller.Open(new Vector(10.0, 15.0));
        harness.Pump(new Size(400, 300));

        Assert.NotNull(info);
        Assert.Equal(new Point(0, 0), info!.AnchorRect.TopLeft);
        Assert.Equal(new Size(400, 300), info.OverlaySize);
        Assert.Equal(new Vector(10.0, 15.0), info.Position);
        Assert.Same(controller, info.TapRegionGroupId);
    }

    private static Widget Anchor(MenuController controller) => new RawMenuAnchor(
        controller: controller,
        overlayBuilder: (_, _) => new SizedBox(width: 40, height: 40),
        child: new SizedBox(width: 80, height: 40));

    private static Widget Wrap(Widget child) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(Size: new Size(400, 300)),
            new Overlay(initialEntries: [new OverlayEntry(_ => child)])));

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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Update(Widget widget)
        {
            _rootElement.Update(widget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) =>
                _renderView = renderView;

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
                if (ReferenceEquals(_child, child)) _child = null;
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null) visitor(_child);
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
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
