using Avalonia;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/actions.dart (FocusableActionDetector)
// flutter/packages/flutter/lib/src/widgets/focus_traversal.dart (ExcludeFocusTraversal)
// flutter/packages/flutter/lib/src/widgets/focus_manager.dart (highlight mode)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FocusableActionDetectorTests : IDisposable
{
    public FocusableActionDetectorTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        StatefulProbe.Reset();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
        StatefulProbe.Reset();
    }

    [Fact]
    public void Constructor_ExposesFlutterDefaultsAndRequiredChild()
    {
        var child = new SizedBox(width: 20, height: 10);
        var detector = new FocusableActionDetector(child);
        var excluded = new ExcludeFocusTraversal(child);

        Assert.Same(child, detector.Child);
        Assert.True(detector.Enabled);
        Assert.Null(detector.FocusNode);
        Assert.False(detector.Autofocus);
        Assert.True(detector.DescendantsAreFocusable);
        Assert.True(detector.DescendantsAreTraversable);
        Assert.Null(detector.Shortcuts);
        Assert.Null(detector.Actions);
        Assert.Null(detector.OnShowFocusHighlight);
        Assert.Null(detector.OnShowHoverHighlight);
        Assert.Null(detector.OnFocusChange);
        Assert.Equal(MouseCursor.Defer, detector.MouseCursor);
        Assert.True(detector.IncludeFocusSemantics);
        Assert.Same(child, excluded.Child);
        Assert.True(excluded.Excluding);
        Assert.Throws<ArgumentNullException>(() => new FocusableActionDetector(null!));
        Assert.Throws<ArgumentNullException>(() => new ExcludeFocusTraversal(null!));
    }

    [Fact]
    public void EnabledDetector_DispatchesShortcutsAndPreservesChildStateWhenDisabled()
    {
        int invocationCount = 0;
        var focusNode = new FocusNode();
        var harness = new WidgetHarness(
            BuildDetector(enabled: true, focusNode, () => invocationCount++));

        Assert.True(focusNode.HasFocus);
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyX)));
        Assert.Equal(1, invocationCount);
        Assert.Equal(1, StatefulProbe.InitCount);
        Assert.Equal(0, StatefulProbe.DisposeCount);

        harness.Update(BuildDetector(enabled: false, focusNode, () => invocationCount++));

        Assert.False(focusNode.CanRequestFocus);
        Assert.False(focusNode.HasFocus);
        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyX)));
        Assert.Equal(1, invocationCount);
        Assert.Equal(1, StatefulProbe.InitCount);
        Assert.Equal(0, StatefulProbe.DisposeCount);

        harness.Dispose();
        Assert.Equal(1, StatefulProbe.DisposeCount);
    }

    [Fact]
    public void FocusAndHoverHighlights_FollowTraditionalTouchAndKeyboardModes()
    {
        var focusHighlights = new List<bool>();
        var hoverHighlights = new List<bool>();
        var focusChanges = new List<bool>();
        var focusNode = new FocusNode();
        using var harness = new WidgetHarness(
            new FocusableActionDetector(
                child: new SizedBox(width: 80, height: 40),
                focusNode: focusNode,
                autofocus: true,
                onShowFocusHighlight: focusHighlights.Add,
                onShowHoverHighlight: hoverHighlights.Add,
                onFocusChange: focusChanges.Add));

        harness.Layout(new Size(120, 80));
        Scheduler.PumpFrameForTests();
        Assert.Equal([true], focusChanges);
        Assert.Equal([true], focusHighlights);

        harness.SendPointer(new PointerHoverEvent(
            pointer: 1,
            kind: PointerDeviceKind.Mouse,
            position: new Point(20, 20),
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));
        Assert.Equal([true], hoverHighlights);

        harness.SendPointer(new PointerDownEvent(
            pointer: 2,
            kind: PointerDeviceKind.Touch,
            position: new Point(20, 20),
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow));
        Assert.Equal(FocusHighlightMode.Touch, FocusManager.Instance.HighlightMode);
        Assert.Equal([true, false], focusHighlights);
        Assert.Equal([true, false], hoverHighlights);

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.Tab));
        Assert.Equal(FocusHighlightMode.Traditional, FocusManager.Instance.HighlightMode);
        Assert.Equal([true, false, true], focusHighlights);
        Assert.Equal([true, false, true], hoverHighlights);
    }

    [Fact]
    public void DisabledDetector_RemainsFocusableOnlyInDirectionalNavigationMode()
    {
        var traditionalNode = new FocusNode();
        using (var traditionalHarness = new WidgetHarness(
                   new MediaQuery(
                       data: new MediaQueryData(NavigationMode: NavigationMode.Traditional),
                       child: new FocusableActionDetector(
                           child: new SizedBox(width: 20, height: 10),
                           enabled: false,
                           autofocus: true,
                           focusNode: traditionalNode))))
        {
            Assert.False(traditionalNode.CanRequestFocus);
            Assert.False(traditionalNode.HasFocus);
        }

        FocusManager.Instance.ResetForTests();
        var directionalNode = new FocusNode();
        using var directionalHarness = new WidgetHarness(
            new MediaQuery(
                data: new MediaQueryData(NavigationMode: NavigationMode.Directional),
                child: new FocusableActionDetector(
                    child: new SizedBox(width: 20, height: 10),
                    enabled: false,
                    autofocus: true,
                    focusNode: directionalNode)));

        Assert.True(directionalNode.CanRequestFocus);
        Assert.True(directionalNode.HasFocus);
    }

    [Fact]
    public void ExcludeFocusTraversal_SkipsTabButAllowsDirectFocus()
    {
        var first = new FocusNode();
        var excluded = new FocusNode();
        var last = new FocusNode();
        using var harness = new WidgetHarness(
            new Row(
                children:
                [
                    new Focus(
                        focusNode: first,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20)),
                    new ExcludeFocusTraversal(
                        child: new Focus(
                            focusNode: excluded,
                            child: new SizedBox(width: 20, height: 20))),
                    new Focus(
                        focusNode: last,
                        child: new SizedBox(width: 20, height: 20)),
                ]));

        Assert.True(FocusManager.Instance.FocusNext());
        Assert.Same(last, FocusManager.Instance.PrimaryFocus);
        Assert.True(excluded.RequestFocus());
        Assert.Same(excluded, FocusManager.Instance.PrimaryFocus);
        Assert.True(first.RequestFocus());
        Assert.True(FocusManager.Instance.FocusNext());
        Assert.Same(last, FocusManager.Instance.PrimaryFocus);
        Assert.False(excluded.SkipTraversal);
    }

    [Fact]
    public void FocusDescendantPolicies_SeparateFocusabilityFromTraversal()
    {
        var blocked = new FocusNode();
        var skipped = new FocusNode();
        var last = new FocusNode();
        using var harness = new WidgetHarness(
            new Column(
                children:
                [
                    new Focus(
                        canRequestFocus: false,
                        skipTraversal: true,
                        descendantsAreFocusable: false,
                        child: new Focus(
                            focusNode: blocked,
                            child: new SizedBox(width: 20, height: 20))),
                    new Focus(
                        canRequestFocus: false,
                        skipTraversal: true,
                        descendantsAreTraversable: false,
                        child: new Focus(
                            focusNode: skipped,
                            child: new SizedBox(width: 20, height: 20))),
                    new Focus(
                        focusNode: last,
                        child: new SizedBox(width: 20, height: 20)),
                ]));

        Assert.False(blocked.CanRequestFocus);
        Assert.False(blocked.RequestFocus());
        Assert.True(skipped.CanRequestFocus);
        Assert.True(skipped.RequestFocus());
        skipped.Unfocus();
        Assert.True(FocusManager.Instance.FocusNext());
        Assert.Same(last, FocusManager.Instance.PrimaryFocus);
        Assert.False(skipped.SkipTraversal);
    }

    private static FocusableActionDetector BuildDetector(
        bool enabled,
        FocusNode focusNode,
        System.Action onInvoke)
    {
        return new FocusableActionDetector(
            child: new StatefulProbe(),
            enabled: enabled,
            focusNode: focusNode,
            autofocus: true,
            shortcuts: new Dictionary<ShortcutActivator, Intent>
            {
                [new SingleActivator(LogicalKeyboardKey.KeyX)] = new ProbeIntent(),
            },
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(ProbeIntent)] = new CallbackAction<ProbeIntent>(
                    _ =>
                    {
                        onInvoke();
                        return null;
                    }),
            });
    }

    private sealed class ProbeIntent : Intent;

    private sealed class StatefulProbe : StatefulWidget
    {
        public static int InitCount { get; private set; }

        public static int DisposeCount { get; private set; }

        public override State CreateState() => new StatefulProbeState();

        public static void Reset()
        {
            InitCount = 0;
            DisposeCount = 0;
        }

        private sealed class StatefulProbeState : State
        {
            public override void InitState()
            {
                InitCount++;
            }

            public override Widget Build(BuildContext context)
            {
                return new SizedBox(width: 40, height: 20);
            }

            public override void Dispose()
            {
                DisposeCount++;
            }
        }
    }

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
