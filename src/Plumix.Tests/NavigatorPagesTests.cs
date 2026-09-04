using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Plumix.Foundation;

#pragma warning disable CS8714
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/navigator.dart

namespace Plumix.Tests;

/// <summary>
/// The declarative <see cref="Navigator.Pages"/> API, the <see cref="TransitionDelegate"/> contract, and the
/// route-entry lifecycle the navigator drives through its history flush.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class NavigatorPagesTests : IDisposable
{
    private static readonly Size ViewSize = new(400, 300);

    public NavigatorPagesTests()
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
    public void Pages_InitialList_ShowsOnlyTheTopPageAndPopRevealsTheOneBelow()
    {
        var host = new PagesHost([new TestPage("first"), new TestPage("second"), new TestPage("third")]);
        using var harness = new Harness(host);

        Assert.Null(harness.FindText("first"));
        Assert.Null(harness.FindText("second"));
        Assert.NotNull(harness.FindText("third"));

        harness.Navigator.Pop();
        harness.Pump();

        Assert.NotNull(harness.FindText("second"));
        Assert.Null(harness.FindText("third"));
    }

    [Fact]
    public void Pages_EmptyList_Throws()
    {
        var host = new PagesHost([]);
        Assert.Throws<InvalidOperationException>(() => new Harness(host).Dispose());
    }

    [Fact]
    public void Pages_RemovingTheTopPage_RemovesItsRouteAndRevealsThePageBelow()
    {
        var host = new PagesHost([new TestPage("first"), new TestPage("second")]);
        using var harness = new Harness(host);

        Assert.NotNull(harness.FindText("second"));

        host.Pages.RemoveAt(1);
        harness.Rebuild();

        Assert.NotNull(harness.FindText("first"));
        Assert.Null(harness.FindText("second"));
    }

    [Fact]
    public void Pages_ReorderingKeyedPages_ReusesTheExistingRoutes()
    {
        var first = new TestPage("first", key: new ValueKey<string>("1"));
        var second = new TestPage("second", key: new ValueKey<string>("2"));
        var host = new PagesHost([first, second]);
        using var harness = new Harness(host);

        int createdBefore = TestPageRoute.CreatedCount;
        host.Pages.Clear();
        host.Pages.AddRange([second, first]);
        harness.Rebuild();

        // Both pages were matched by key, so no new route was created and the reorder had no transition.
        Assert.Equal(createdBefore, TestPageRoute.CreatedCount);
        Assert.NotNull(harness.FindText("first"));
        Assert.Null(harness.FindText("second"));
    }

    [Fact]
    public void Pages_AddingAPageOnTop_PushesItAndKeepsTheOldRoute()
    {
        var host = new PagesHost([new TestPage("first", key: new ValueKey<string>("1"))]);
        using var harness = new Harness(host);
        Route firstRoute = harness.Navigator.CurrentRoute!;

        host.Pages.Add(new TestPage("second", key: new ValueKey<string>("2")));
        harness.Rebuild();

        Assert.NotSame(firstRoute, harness.Navigator.CurrentRoute);
        Assert.True(firstRoute.IsActive);
        Assert.NotNull(harness.FindText("second"));
    }

    [Fact]
    public void Pages_PagelessRoutesStayAttachedToTheirPageRoute()
    {
        var first = new TestPage("first", key: new ValueKey<string>("1"));
        var second = new TestPage("second", key: new ValueKey<string>("2"));
        var host = new PagesHost([first, second]);
        using var harness = new Harness(host);

        var dialog = new ProbePageRoute("dialog");
        harness.Navigator.Push(dialog);
        harness.Pump();
        Assert.NotNull(harness.FindText("dialog"));

        // Removing the page the pageless route sits above takes the pageless route with it.
        host.Pages.RemoveAt(1);
        harness.Rebuild();

        Assert.Null(harness.FindText("dialog"));
        Assert.NotNull(harness.FindText("first"));
        Assert.True(dialog.Popped.IsCompleted);
    }

    [Fact]
    public void Pages_ObserverOrder_PushesTheNewRoutesBeforeRemovingTheOldOnes()
    {
        var observer = new RecordingObserver();
        var host = new PagesHost(
            [new TestPage("first", key: new ValueKey<string>("1"))],
            observers: [observer]);
        using var harness = new Harness(host);

        observer.Events.Clear();
        host.Pages.Clear();
        host.Pages.Add(new TestPage("second", key: new ValueKey<string>("2")));
        harness.Rebuild();

        Assert.Equal(["push:second", "remove:first"], observer.Events);
    }

    [Fact]
    public void Page_CanPopFalse_BlocksMaybePopAndReportsOnPopInvoked()
    {
        var invocations = new List<bool>();
        var host = new PagesHost(
        [
            new TestPage("first"),
            new TestPage("second", canPop: false, onPopInvoked: (didPop, _) => invocations.Add(didPop)),
        ]);
        using var harness = new Harness(host);

        Assert.True(harness.Navigator.MaybePop());
        harness.Pump();

        Assert.Equal([false], invocations);
        Assert.NotNull(harness.FindText("second"));
    }

    [Fact]
    public void Page_ImperativePop_CallsOnDidRemovePage()
    {
        var host = new PagesHost([new TestPage("first"), new TestPage("second")]);
        using var harness = new Harness(host);

        harness.Navigator.Pop();
        harness.Pump();

        Assert.Equal(["second"], host.RemovedPages.Select(page => page.Name ?? string.Empty).ToArray());
    }

    [Fact]
    public void TransitionDelegate_CustomResolve_CanRemoveEveryExitingRouteWithoutATransition()
    {
        var host = new PagesHost(
            [new TestPage("first", key: new ValueKey<string>("1"))],
            transitionDelegate: new AlwaysRemoveTransitionDelegate());
        using var harness = new Harness(host);

        host.Pages.Clear();
        host.Pages.Add(new TestPage("second", key: new ValueKey<string>("2")));
        harness.Rebuild();

        Assert.Null(harness.FindText("first"));
        Assert.NotNull(harness.FindText("second"));
    }

    [Fact]
    public void TransitionDelegate_DroppingARequiredRoute_Throws()
    {
        var host = new PagesHost(
            [new TestPage("first", key: new ValueKey<string>("1"))],
            transitionDelegate: new DropEverythingTransitionDelegate());
        using var harness = new Harness(host);

        host.Pages.Clear();
        host.Pages.Add(new TestPage("second", key: new ValueKey<string>("2")));

        Assert.Throws<InvalidOperationException>(harness.Rebuild);
    }

    [Fact]
    public void DefaultTransitionDelegate_PushesOnlyTheTopMostEnteringRoute()
    {
        var first = new RouteTransitionRecordProbe(new ProbePageRoute("first"));
        var second = new RouteTransitionRecordProbe(new ProbePageRoute("second"));
        var results = new DefaultTransitionDelegate()
            .Resolve(
                [first, second],
                new RouteRecordMap<RouteTransitionRecord, RouteTransitionRecord>(),
                new RouteRecordMap<RouteTransitionRecord, IReadOnlyList<RouteTransitionRecord>>())
            .ToList();

        Assert.Equal([first, second], results);
        Assert.Equal("add", first.Decision);
        Assert.Equal("push", second.Decision);
    }

    [Fact]
    public void DefaultTransitionDelegate_PopsOnlyTheTopMostExitingRoute()
    {
        var exitingLower = new RouteTransitionRecordProbe(new ProbePageRoute("lower"), waitingForExit: true);
        var exitingUpper = new RouteTransitionRecordProbe(new ProbePageRoute("upper"), waitingForExit: true);
        var locations = new RouteRecordMap<RouteTransitionRecord, RouteTransitionRecord>
        {
            [null] = exitingLower,
            [exitingLower] = exitingUpper,
        };

        var results = new DefaultTransitionDelegate()
            .Resolve([], locations, new RouteRecordMap<RouteTransitionRecord, IReadOnlyList<RouteTransitionRecord>>())
            .ToList();

        Assert.Equal([exitingLower, exitingUpper], results);
        Assert.Equal("complete", exitingLower.Decision);
        Assert.Equal("pop", exitingUpper.Decision);
    }

    [Fact]
    public void Page_CreateRoute_CanBePushedAsAPagelessRoute()
    {
        var host = new PagesHost([new TestPage("first")]);
        using var harness = new Harness(host);

        harness.Navigator.Push(new TestPage("pushed").CreateRoute(harness.Navigator.Context));
        harness.Pump();

        Assert.NotNull(harness.FindText("pushed"));
        Assert.Empty(host.RemovedPages);
    }

    [Fact]
    public void NavigatorObserver_DidChangeTop_ReportsEveryChangeOfTheTopMostRoute()
    {
        var observer = new RecordingObserver();
        var root = new ProbePageRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root, observers: [observer]));

        Assert.Equal(["top:root:null"], observer.TopChanges);

        var details = new ProbePageRoute("details");
        harness.Navigator.Push(details);
        harness.Pump();
        Assert.Equal(["top:root:null", "top:details:root"], observer.TopChanges);

        harness.Navigator.Pop();
        harness.Pump();
        Assert.Equal(["top:root:null", "top:details:root", "top:root:details"], observer.TopChanges);
    }

    [Fact]
    public void Navigator_ClipBehavior_DefaultsToHardEdgeAndReachesTheOverlay()
    {
        using var harness = new Harness(new Navigator(initialRoute: new ProbePageRoute("root")));
        Assert.Equal(Plumix.UI.Clip.HardEdge, harness.NavigatorWidget.ClipBehavior);

        using var noneHarness = new Harness(
            new Navigator(initialRoute: new ProbePageRoute("root"), clipBehavior: Plumix.UI.Clip.None));
        Assert.Equal(Plumix.UI.Clip.None, noneHarness.NavigatorWidget.ClipBehavior);
    }

    [Fact]
    public void Route_PoppedTask_CompletesWithTheResult()
    {
        var root = new ProbePageRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));
        var details = new ProbePageRoute("details");

        harness.Navigator.Push(details);
        harness.Pump();
        Assert.False(details.Popped.IsCompleted);

        harness.Navigator.Pop("answer");
        harness.Pump();

        Assert.True(details.Popped.IsCompleted);
        Assert.Equal("answer", details.Popped.Result);
    }

    [Fact]
    public void Route_RestorationScopeId_StaysNullWhileRestorationIsUnavailable()
    {
        var root = new ProbePageRoute("root");
        using var harness = new Harness(new Navigator(initialRoute: root));

        // No restoration bucket reaches this navigator, so no route reports a restoration scope id.
        Assert.Null(root.RestorationScopeId.Value);
    }

    // -----------------------------------------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------------------------------------

    private sealed record TestPage : Page
    {
        private readonly string _label;

        public TestPage(
            string label,
            Key? key = null,
            bool canPop = true,
            PopInvokedWithResultCallback<object>? onPopInvoked = null)
            : base(key: key, name: label, canPop: canPop, onPopInvoked: onPopInvoked)
        {
            _label = label;
        }

        public string Label => _label;

        public override Route CreateRoute(BuildContext context) => new TestPageRoute(this);
    }

    private sealed class TestPageRoute : PageRoute
    {
        public TestPageRoute(TestPage page) : base(settings: page)
        {
            CreatedCount += 1;
        }

        public static int CreatedCount { get; private set; }

        public override Widget BuildPage(BuildContext context) => new Text(((TestPage)Settings).Label);
    }

    private sealed class ProbePageRoute : PageRoute
    {
        private readonly string _label;

        public ProbePageRoute(string label, string? name = null) : base(settings: new RouteSettings(name ?? label))
        {
            _label = label;
        }

        public override Widget BuildPage(BuildContext context) => new Text(_label);
    }

    private sealed class RecordingObserver : NavigatorObserver
    {
        public List<string> Events { get; } = [];

        public List<string> TopChanges { get; } = [];

        public override void DidPush(Route route, Route? previousRoute) =>
            Events.Add($"push:{route.Settings.Name}");

        public override void DidPop(Route route, Route? previousRoute) => Events.Add($"pop:{route.Settings.Name}");

        public override void DidRemove(Route route, Route? previousRoute) =>
            Events.Add($"remove:{route.Settings.Name}");

        public override void DidReplace(Route newRoute, Route? oldRoute) =>
            Events.Add($"replace:{newRoute.Settings.Name}");

        public override void DidChangeTop(Route topRoute, Route? previousTopRoute) =>
            TopChanges.Add($"top:{topRoute.Settings.Name}:{previousTopRoute?.Settings.Name ?? "null"}");
    }

    /// <summary>A delegate that never animates: every entering route is added and every exiting one completes.</summary>
    private sealed class AlwaysRemoveTransitionDelegate : TransitionDelegate
    {
        public override IEnumerable<RouteTransitionRecord> Resolve(
            IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
            IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
            IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPageless)
        {
            var results = new List<RouteTransitionRecord>();

            void HandleExiting(RouteTransitionRecord? location)
            {
                while (locationToExitingPageRoute.TryGetValue(location!, out RouteTransitionRecord? exiting))
                {
                    if (exiting.IsWaitingForExitingDecision)
                    {
                        exiting.MarkForComplete();
                        if (pageRouteToPageless.TryGetValue(exiting, out IReadOnlyList<RouteTransitionRecord>? p))
                        {
                            foreach (RouteTransitionRecord pageless in p)
                            {
                                if (pageless.IsWaitingForExitingDecision)
                                {
                                    pageless.MarkForComplete();
                                }
                            }
                        }
                    }

                    results.Add(exiting);
                    location = exiting;
                }
            }

            HandleExiting(null);
            foreach (RouteTransitionRecord entering in newPageRouteHistory)
            {
                if (entering.IsWaitingForEnteringDecision)
                {
                    entering.MarkForAdd();
                }

                results.Add(entering);
                HandleExiting(entering);
            }

            return results;
        }
    }

    private sealed class DropEverythingTransitionDelegate : TransitionDelegate
    {
        public override IEnumerable<RouteTransitionRecord> Resolve(
            IReadOnlyList<RouteTransitionRecord> newPageRouteHistory,
            IReadOnlyDictionary<RouteTransitionRecord?, RouteTransitionRecord> locationToExitingPageRoute,
            IReadOnlyDictionary<RouteTransitionRecord?, IReadOnlyList<RouteTransitionRecord>> pageRouteToPageless)
        {
            foreach (RouteTransitionRecord entering in newPageRouteHistory)
            {
                if (entering.IsWaitingForEnteringDecision)
                {
                    entering.MarkForAdd();
                }
            }

            foreach (RouteTransitionRecord exiting in locationToExitingPageRoute.Values)
            {
                if (exiting.IsWaitingForExitingDecision)
                {
                    exiting.MarkForComplete();
                }
            }

            return newPageRouteHistory;
        }
    }

    private sealed class RouteTransitionRecordProbe : RouteTransitionRecord
    {
        private bool _waitingForEntering;
        private bool _waitingForExit;

        public RouteTransitionRecordProbe(Route route, bool waitingForExit = false)
        {
            Route = route;
            _waitingForEntering = !waitingForExit;
            _waitingForExit = waitingForExit;
        }

        public override Route Route { get; }

        public string? Decision { get; private set; }

        public override bool IsWaitingForEnteringDecision => _waitingForEntering;

        public override bool IsWaitingForExitingDecision => _waitingForExit;

        public override void MarkForPush()
        {
            Decision = "push";
            _waitingForEntering = false;
        }

        public override void MarkForAdd()
        {
            Decision = "add";
            _waitingForEntering = false;
        }

        public override void MarkForPop(object? result = null)
        {
            Decision = "pop";
            _waitingForExit = false;
        }

        public override void MarkForComplete(object? result = null)
        {
            Decision = "complete";
            _waitingForExit = false;
        }
    }

    private sealed class PagesHost : StatefulWidget
    {
        public PagesHost(
            List<Page> pages,
            IReadOnlyList<NavigatorObserver>? observers = null,
            TransitionDelegate? transitionDelegate = null)
        {
            Pages = pages;
            Observers = observers;
            TransitionDelegate = transitionDelegate;
        }

        public List<Page> Pages { get; }

        public IReadOnlyList<NavigatorObserver>? Observers { get; }

        public TransitionDelegate? TransitionDelegate { get; }

        public List<Page> RemovedPages { get; } = [];

        public override State CreateState() => new PagesHostState();

        internal sealed class PagesHostState : State
        {
            private PagesHost Host => (PagesHost)StateWidget;

            public override Widget Build(BuildContext context)
            {
                return new Navigator(
                    pages: [.. Host.Pages],
                    onDidRemovePage: page =>
                    {
                        Host.RemovedPages.Add(page);
                        Host.Pages.Remove(page);
                    },
                    observers: Host.Observers,
                    transitionDelegate: Host.TransitionDelegate);
            }
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _root;

        public Harness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, rootWidget);
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
            Pump();
        }

        public RenderView RenderView { get; }

        public NavigatorState Navigator => FindNavigatorState()
            ?? throw new InvalidOperationException("The navigator has not been mounted.");

        /// <summary>Marks the pages host dirty and pumps, mirroring a page-list update from the app.</summary>
        public void Rebuild()
        {
            MarkHostDirty();
            Pump();
        }

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(ViewSize);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public Navigator NavigatorWidget
        {
            get
            {
                Navigator? found = null;
                void Visit(Element element)
                {
                    if (found is null && element.Widget is Navigator navigator)
                    {
                        found = navigator;
                    }

                    element.VisitChildren(Visit);
                }

                _root.VisitChildren(Visit);
                return found ?? throw new InvalidOperationException("The navigator has not been mounted.");
            }
        }

        public RenderParagraph? FindText(string text) =>
            OverlayVisibility.FindOnstage<RenderParagraph>(RenderView, node => node.PlainText == text);

        public void Dispose() => _root.Unmount();

        private void MarkHostDirty()
        {
            void Visit(Element element)
            {
                if (element is StatefulElement { State: PagesHost.PagesHostState })
                {
                    element.MarkNeedsBuild();
                }

                element.VisitChildren(Visit);
            }

            _root.VisitChildren(Visit);
        }

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

            public override void Unmount()
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
