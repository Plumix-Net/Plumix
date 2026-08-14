using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/navigator.dart;
// flutter/packages/flutter/lib/src/widgets/routes.dart; flutter/packages/flutter/lib/src/widgets/overlay.dart

namespace Plumix.Tests;

/// <summary>
/// The Navigator installs its routes into an <see cref="Overlay"/>: every <see cref="ModalRoute"/>
/// contributes the <c>[barrier, scope]</c> entry pair, the bottom-most entry carries the route's opacity,
/// and disposal removes the entries before the route itself is disposed.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class NavigatorOverlayTests : IDisposable
{
    private static readonly Size ViewSize = new(400, 300);

    public NavigatorOverlayTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void ModalRoute_CreatesTheBarrierAndScopeEntryPair()
    {
        var route = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: route));

        Assert.Equal(2, route.OverlayEntries.Count);
        OverlayEntry barrier = route.OverlayEntries[0];
        OverlayEntry scope = route.OverlayEntries[1];

        // The barrier is created first, so it sits below the page and is the entry that carries `opaque`.
        Assert.False(barrier.MaintainState);
        Assert.False(barrier.CanSizeOverlay);
        Assert.Equal(route.MaintainState, scope.MaintainState);
        Assert.Equal(route.Opaque, scope.CanSizeOverlay);
        Assert.False(scope.Opaque);
        Assert.Equal([barrier, scope], harness.Navigator.Overlay!.Entries);
    }

    [Fact]
    public void Navigator_KeepsOverlayEntriesInHistoryOrder()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));
        var first = new ProbeRoute("first");
        var second = new ProbeRoute("second");

        harness.Navigator.Push(first);
        harness.Pump();
        harness.Navigator.Push(second);
        harness.Pump();

        Assert.Equal(
            root.OverlayEntries.Concat(first.OverlayEntries).Concat(second.OverlayEntries).ToArray(),
            harness.Navigator.Overlay!.Entries);
    }

    [Fact]
    public void TopmostOpaqueRoute_MarksItsBarrierEntryOpaqueAndHidesTheRouteBelow()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        Assert.True(root.OverlayEntries[0].Opaque);
        Assert.NotNull(harness.FindText("root"));

        var details = new ProbeRoute("details");
        harness.Navigator.Push(details);
        harness.Pump();

        Assert.True(details.OverlayEntries[0].Opaque);
        Assert.False(harness.Navigator.Overlay!.DebugIsVisible(root.OverlayEntries[1]));
        Assert.Null(harness.FindText("root"));
        Assert.NotNull(harness.FindText("details"));
    }

    [Fact]
    public void PushingAnOpaqueRoute_DoesNotRebuildTheRouteBelow()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        Assert.Equal(1, root.BuildCount);

        harness.Navigator.Push(new ProbeRoute("details"));
        harness.Pump();

        Assert.Equal(1, root.BuildCount);
    }

    [Fact]
    public void MaintainState_DecidesWhetherTheStateBelowSurvivesAnOpaqueRoute()
    {
        foreach (bool maintainState in new[] { true, false })
        {
            int disposals = 0;
            var root = new ProbeRoute("root", maintainState: maintainState, body: new DisposeProbe(() => disposals += 1));
            using var harness = new Harness(new Navigator(initialRoute: root));

            harness.Navigator.Push(new ProbeRoute("details"));
            harness.Pump();

            Assert.Equal(maintainState ? 0 : 1, disposals);
        }
    }

    [Fact]
    public void NonOpaqueRoute_LeavesTheRouteBelowOnstage()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        var popup = new ProbeRoute("popup", opaque: false);
        harness.Navigator.Push(popup);
        harness.Pump();

        Assert.False(popup.OverlayEntries[0].Opaque);
        Assert.True(harness.Navigator.Overlay!.DebugIsVisible(root.OverlayEntries[1]));
        Assert.NotNull(harness.FindText("root"));
        Assert.NotNull(harness.FindText("popup"));
    }

    [Fact]
    public void Offstage_ReportsEndOfTransitionAnimationsAndStopsPainting()
    {
        var root = new ProbeRoute("root", transitionDuration: TimeSpan.FromMilliseconds(200));
        using var harness = new Harness(new Navigator(initialRoute: root));

        // Initial routes are added, not pushed, so their transition starts at its end value.
        Assert.Equal(AnimationStatus.Completed, root.Animation.Status);

        root.Offstage = true;
        harness.Pump();

        Assert.Equal(1.0, root.Animation.Value);
        Assert.Equal(AnimationStatus.Completed, root.Animation.Status);
        Assert.Equal(AnimationStatus.Dismissed, root.SecondaryAnimation.Status);
        Assert.True(harness.FindOffstage()!.Offstage);

        root.Offstage = false;
        harness.Pump();

        Assert.Equal(AnimationStatus.Completed, root.Animation.Status);
        Assert.False(harness.FindOffstage()!.Offstage);
    }

    [Fact]
    public void OffstageRoute_DropsTheAnimatedBarrierColor()
    {
        var route = new ProbeRoute("root", barrierColor: Colors.Black);
        using var harness = new Harness(new Navigator(initialRoute: route));

        Assert.IsType<AnimatedModalBarrier>(route.BuildModalBarrier());

        route.Offstage = true;
        harness.Pump();

        Assert.IsType<ModalBarrier>(route.BuildModalBarrier());
    }

    [Fact]
    public void Filter_WrapsTheBarrierInABackdropFilter()
    {
        var withoutFilter = new ProbeRoute("plain", barrierColor: Colors.Black);
        using (var harness = new Harness(new Navigator(initialRoute: withoutFilter)))
        {
            Assert.Empty(harness.FindAll<RenderBackdropFilter>());
        }

        var withFilter = new ProbeRoute("blurred", barrierColor: Colors.Black, filter: new ImageFilter.Blur(4.0));
        using (var harness = new Harness(new Navigator(initialRoute: withFilter)))
        {
            Assert.Single(harness.FindAll<RenderBackdropFilter>());
        }
    }

    [Fact]
    public void PoppedRoute_RemovesItsOverlayEntriesAndIsDisposedAfterTheyUnmount()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));
        var details = new ProbeRoute("details");
        harness.Navigator.Push(details);
        harness.Pump();

        OverlayEntry[] entries = details.OverlayEntries.ToArray();
        Assert.All(entries, entry => Assert.True(entry.Mounted));

        harness.Navigator.Pop();
        harness.Pump();

        Assert.DoesNotContain(entries[0], harness.Navigator.Overlay!.Entries);
        Assert.DoesNotContain(entries[1], harness.Navigator.Overlay.Entries);
        Assert.All(entries, entry => Assert.False(entry.Mounted));
        Assert.Empty(details.OverlayEntries);
        Assert.Throws<InvalidOperationException>(() => _ = details.Animation);
        Assert.NotNull(harness.FindText("root"));
    }

    [Fact]
    public void DismissIntent_PopsOnlyWhenTheBarrierIsDismissible()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        var blocking = new ProbeRoute("blocking", opaque: false, barrierDismissible: false);
        harness.Navigator.Push(blocking);
        harness.Pump();

        BuildContext context = blocking.CapturedContext!.Value;
        var action = Assert.IsType<DismissModalAction>(Actions.MaybeFind(context, new DismissIntent()));
        Assert.False(action.IsEnabled(new DismissIntent()));

        var dismissible = new ProbeRoute("dismissible", opaque: false, barrierDismissible: true);
        harness.Navigator.Push(dismissible);
        harness.Pump();

        context = dismissible.CapturedContext!.Value;
        action = Assert.IsType<DismissModalAction>(Actions.MaybeFind(context, new DismissIntent()));
        Assert.True(action.IsEnabled(new DismissIntent()));

        _ = action.Invoke(new DismissIntent());
        harness.Pump();

        Assert.Same(blocking, harness.Navigator.CurrentRoute);
    }

    [Fact]
    public void RouteScope_TracksCanPopAndAppBarDismissalOfTheOwningRoute()
    {
        var root = new ProbeRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        Assert.False(root.CanPop);
        Assert.False(root.ImpliesAppBarDismissal);

        var details = new ProbeRoute("details");
        harness.Navigator.Push(details);
        harness.Pump();

        Assert.True(details.CanPop);
        Assert.True(details.ImpliesAppBarDismissal);
        Assert.True(details.HasActiveRouteBelow);
        Assert.False(root.HasActiveRouteBelow);
    }

    private sealed class ProbeRoute : PageRoute
    {
        private readonly string _label;
        private readonly Widget? _body;
        private readonly TimeSpan _transitionDuration;

        public ProbeRoute(
            string label,
            bool opaque = true,
            bool maintainState = true,
            bool barrierDismissible = false,
            Color? barrierColor = null,
            ImageFilter? filter = null,
            Widget? body = null,
            TimeSpan? transitionDuration = null)
            : base(new RouteSettings(Name: label), maintainState: maintainState, filter: filter)
        {
            _label = label;
            _body = body;
            _transitionDuration = transitionDuration ?? TimeSpan.Zero;
            Opaque = opaque;
            BarrierDismissible = barrierDismissible;
            BarrierColor = barrierColor;
        }

        public override bool Opaque { get; }

        public override bool BarrierDismissible { get; }

        public override Color? BarrierColor { get; }

        public override string? BarrierLabel => null;

        public override TimeSpan TransitionDuration => _transitionDuration;

        public int BuildCount { get; private set; }

        public BuildContext? CapturedContext { get; private set; }

        public override Widget BuildPage(BuildContext context)
        {
            BuildCount += 1;
            CapturedContext = context;
            return new Column(children: _body is null ? [new Text(_label)] : [new Text(_label), _body]);
        }
    }

    private sealed class DisposeProbe : StatefulWidget
    {
        public DisposeProbe(Action onDispose)
        {
            OnDispose = onDispose;
        }

        public Action OnDispose { get; }

        public override State CreateState() => new DisposeProbeState();

        private sealed class DisposeProbeState : State
        {
            public override Widget Build(BuildContext context) => new SizedBox(width: 1, height: 1);

            public override void Dispose()
            {
                ((DisposeProbe)StateWidget).OnDispose();
                base.Dispose();
            }
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _root;

        public Harness(Navigator navigator)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, navigator);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
            Pump();
        }

        public RenderView RenderView { get; }

        public NavigatorState Navigator => FindNavigatorState()
            ?? throw new InvalidOperationException("The navigator has not been mounted.");

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(ViewSize);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public RenderParagraph? FindText(string text)
        {
            return OverlayVisibility.FindOnstage<RenderParagraph>(RenderView, node => node.PlainText == text);
        }

        public RenderOffstage? FindOffstage()
        {
            return OverlayVisibility.FindOnstage<RenderOffstage>(RenderView);
        }

        public List<TRenderObject> FindAll<TRenderObject>() where TRenderObject : RenderObject
        {
            var result = new List<TRenderObject>();
            OverlayVisibility.VisitOnstage(RenderView, node =>
            {
                if (node is TRenderObject typed)
                {
                    result.Add(typed);
                }
            });

            return result;
        }

        public void Dispose() => _root.Unmount();

        private NavigatorState? FindNavigatorState()
        {
            NavigatorState? found = null;
            void Visit(Element element)
            {
                if (found is null && element is StatefulElement { State: NavigatorState state })
                {
                    found = state;
                }

                element.VisitChildren(Visit);
            }

            _root.VisitChildren(Visit);
            return found;
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;

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

            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child))
                {
                    _view.Child = null;
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
