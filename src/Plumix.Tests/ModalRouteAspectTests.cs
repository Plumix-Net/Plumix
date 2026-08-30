using System;
using System.Collections.Generic;
using Avalonia;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/routes.dart

namespace Plumix.Tests;

/// <summary>
/// <c>_ModalScopeStatus</c> is an <see cref="InheritedModel{TAspect}"/>: a dependent that asks for one
/// <see cref="ModalRouteAspect"/> rebuilds only when that aspect changes, while <see cref="ModalRoute.MaybeOf"/>
/// depends on the whole status. The route's focus scope and traversal edges follow the navigator's settings.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class ModalRouteAspectTests : IDisposable
{
    private static readonly Size ViewSize = new(400, 300);

    public ModalRouteAspectTests()
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
    public void IsFirstOf_DoesNotRebuildWhenAnotherRouteIsPushed()
    {
        AssertAspectIsStableAcrossAPush(context => $"isFirst:{ModalRoute.IsFirstOf(context)}", "isFirst:True");
    }

    [Fact]
    public void IsActiveOf_DoesNotRebuildWhenAnotherRouteIsPushed()
    {
        AssertAspectIsStableAcrossAPush(context => $"isActive:{ModalRoute.IsActiveOf(context)}", "isActive:True");
    }

    [Fact]
    public void OpaqueOf_DoesNotRebuildWhenAnotherRouteIsPushed()
    {
        AssertAspectIsStableAcrossAPush(context => $"opaque:{ModalRoute.OpaqueOf(context)}", "opaque:True");
    }

    [Fact]
    public void SettingsOf_DoesNotRebuildWhenAnotherRouteIsPushed()
    {
        AssertAspectIsStableAcrossAPush(
            context => $"name:{ModalRoute.SettingsOf(context)?.Name}",
            "name:root");
    }

    [Fact]
    public void IsCurrentOf_RebuildsWhenAnotherRouteIsPushed()
    {
        var probe = new AspectProbe(context => $"isCurrent:{ModalRoute.IsCurrentOf(context)}");
        using var harness = new Harness(new Navigator(initialRoute: new ProbeRoute("root", probe)));

        int buildsBefore = probe.BuildCount;
        Assert.Equal("isCurrent:True", probe.LastValue);

        harness.Navigator.Push(new ProbeRoute("details", child: null));
        harness.Pump();

        Assert.True(probe.BuildCount > buildsBefore);
        Assert.Equal("isCurrent:False", probe.LastValue);
    }

    [Fact]
    public void CanPopOf_ReportsWhetherTheRouteHasSomethingBelowIt()
    {
        var probe = new AspectProbe(context => $"canPop:{ModalRoute.CanPopOf(context)}");
        var details = new ProbeRoute("details", probe);
        using var harness = new Harness(new Navigator(initialRoute: new ProbeRoute("root", child: null)));

        harness.Navigator.Push(details);
        harness.Pump();

        Assert.Equal("canPop:True", probe.LastValue);
    }

    [Fact]
    public void PopDispositionOf_ReportsTheDispositionAPopScopeInstalls()
    {
        var probe = new AspectProbe(context => $"popDisposition:{ModalRoute.PopDispositionOf(context)}");
        var root = new ProbeRoute("root", new PopScope<object>(canPop: false, child: probe));
        using var harness = new Harness(new Navigator(initialRoute: root));

        Assert.Equal($"popDisposition:{RoutePopDisposition.DoNotPop}", probe.LastValue);
    }

    [Fact]
    public void MaybeOf_ResolvesTheEnclosingModalRoute()
    {
        ModalRoute? resolved = null;
        var probe = new AspectProbe(context =>
        {
            resolved = ModalRoute.MaybeOf(context);
            return "ok";
        });
        var root = new ProbeRoute("root", probe);
        using var harness = new Harness(new Navigator(initialRoute: root));

        Assert.Same(root, resolved);
    }

    [Fact]
    public void RouteScope_TraversalEdgeBehavior_FollowsTheNavigatorSetting()
    {
        FocusScopeNode? scope = null;
        var probe = new AspectProbe(context =>
        {
            scope = FocusScope.MaybeOf(context);
            return "ok";
        });

        using var harness = new Harness(new Navigator(
            initialRoute: new ProbeRoute("root", probe),
            routeTraversalEdgeBehavior: TraversalEdgeBehavior.LeaveFlutterView,
            routeDirectionalTraversalEdgeBehavior: TraversalEdgeBehavior.ClosedLoop));

        Assert.NotNull(scope);
        Assert.Equal(TraversalEdgeBehavior.LeaveFlutterView, scope!.TraversalEdgeBehavior);
        Assert.Equal(TraversalEdgeBehavior.ClosedLoop, scope.DirectionalTraversalEdgeBehavior);
    }

    [Fact]
    public void RouteScope_TraversalEdgeBehavior_DefaultsToParentScopeAndStop()
    {
        FocusScopeNode? scope = null;
        var probe = new AspectProbe(context =>
        {
            scope = FocusScope.MaybeOf(context);
            return "ok";
        });

        using var harness = new Harness(new Navigator(initialRoute: new ProbeRoute("root", probe)));

        Assert.NotNull(scope);
        Assert.Equal(TraversalEdgeBehavior.ParentScope, scope!.TraversalEdgeBehavior);
        Assert.Equal(TraversalEdgeBehavior.Stop, scope.DirectionalTraversalEdgeBehavior);
    }

    [Fact]
    public void Route_RequestFocus_DefaultsToTheNavigatorSetting()
    {
        var root = new ProbeRoute("root", child: null);
        using var harness = new Harness(new Navigator(initialRoute: root));
        Assert.True(root.RequestFocus);

        var quiet = new ProbeRoute("quiet", child: null);
        using var quietHarness = new Harness(new Navigator(initialRoute: quiet, requestFocus: false));
        Assert.False(quiet.RequestFocus);
    }

    [Fact]
    public void Navigator_RequestFocusFalse_LeavesAnExistingFocusAlone()
    {
        using var outside = new FocusScopeNode();
        FocusManager.Instance.RegisterNode(outside, FocusManager.Instance.RootScope);
        outside.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(outside.HasPrimaryFocus);

        using var harness = new Harness(new Navigator(
            initialRoute: new ProbeRoute("root", child: null),
            requestFocus: false));

        harness.Navigator.Push(new ProbeRoute("details", child: null));
        harness.Pump();

        Assert.True(outside.HasPrimaryFocus);
    }

    [Fact]
    public void FocusScopeNode_SetFirstFocus_MovesFocusWhenTheParentAlreadyHasIt()
    {
        var parent = new FocusScopeNode();
        var child = new FocusScopeNode();
        FocusManager.Instance.RegisterNode(parent, FocusManager.Instance.RootScope);
        FocusManager.Instance.RegisterNode(child, parent);

        parent.RequestFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(parent.HasFocusInScope);

        parent.SetFirstFocus(child);
        Scheduler.FlushMicrotasks();

        Assert.True(child.HasPrimaryFocus);
    }

    [Fact]
    public void FocusScopeNode_SetFirstFocus_OnlyRecordsTheChildWhenTheParentIsUnfocused()
    {
        var parent = new FocusScopeNode();
        var child = new FocusScopeNode();
        FocusManager.Instance.RegisterNode(parent, FocusManager.Instance.RootScope);
        FocusManager.Instance.RegisterNode(child, parent);

        parent.SetFirstFocus(child);

        Assert.False(child.HasPrimaryFocus);
        Assert.Same(child, parent.FocusedChild);

        // Focusing the parent now walks back down to the recorded child.
        parent.RequestFirstFocus();
        Scheduler.FlushMicrotasks();
        Assert.True(child.HasPrimaryFocus);
    }

    [Fact]
    public void FocusScopeNode_SetFirstFocus_RejectsASelfReference()
    {
        var scope = new FocusScopeNode();
        Assert.Throws<ArgumentException>(() => scope.SetFirstFocus(scope));
    }

    private void AssertAspectIsStableAcrossAPush(Func<BuildContext, string> read, string expected)
    {
        var probe = new AspectProbe(read);
        using var harness = new Harness(new Navigator(initialRoute: new ProbeRoute("root", probe)));

        Assert.Equal(expected, probe.LastValue);
        int buildsBefore = probe.BuildCount;

        harness.Navigator.Push(new ProbeRoute("details", child: null));
        harness.Pump();

        Assert.Equal(buildsBefore, probe.BuildCount);
        Assert.Equal(expected, probe.LastValue);
    }

    // -----------------------------------------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------------------------------------

    /// <summary>Reads one aspect of the enclosing modal route and records how often it was rebuilt.</summary>
    private sealed class AspectProbe : StatelessWidget
    {
        private readonly Func<BuildContext, string> _read;

        public AspectProbe(Func<BuildContext, string> read)
        {
            _read = read;
        }

        public int BuildCount { get; private set; }

        public string? LastValue { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount += 1;
            LastValue = _read(context);
            return new Text(LastValue);
        }
    }

    private sealed class ProbeRoute : PageRoute
    {
        private readonly string _label;
        private readonly Widget? _child;

        public ProbeRoute(string label, Widget? child) : base(settings: new RouteSettings(label))
        {
            _label = label;
            _child = child;
        }

        public override TimeSpan TransitionDuration => TimeSpan.Zero;

        public override Widget BuildPage(BuildContext context) =>
            _child is null ? new Text(_label) : new Column(children: [new Text(_label), _child]);
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
