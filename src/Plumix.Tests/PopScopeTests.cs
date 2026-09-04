using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/pop_scope.dart
// flutter/packages/flutter/lib/src/widgets/navigator_pop_handler.dart
// flutter/packages/flutter/lib/src/widgets/navigator.dart
// flutter/packages/flutter/lib/src/widgets/routes.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class PopScopeTests : IDisposable
{
    public PopScopeTests()
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
    public void Constructors_ExposeFlutterDefaults_AndRejectLegacyCallbackConflicts()
    {
        var child = new SizedBox();
        var scope = new PopScope<string>(child);
        var handler = new NavigatorPopHandler<string>(child);

        Assert.Same(child, scope.Child);
        Assert.True(scope.CanPop);
        Assert.Null(scope.OnPopInvokedWithResult);
        Assert.Same(child, handler.Child);
        Assert.True(handler.Enabled);
        Assert.Null(handler.OnPopWithResult);

        Assert.Throws<ArgumentException>(() => new PopScope<string>(
            child,
            onPopInvoked: _ => { },
            onPopInvokedWithResult: (_, _) => { }));
        Assert.Throws<ArgumentException>(() => new NavigatorPopHandler<string>(
            child,
            onPop: () => { },
            onPopWithResult: _ => { }));
    }

    [Fact]
    public void PopScope_BlocksCollectively_ReportsResult_AndUpdatesCanPop()
    {
        var owner = new BuildOwner();
        NavigatorState? navigator = null;
        PopScopeProbeState? probe = null;
        var invocations = new List<(bool DidPop, string? Result)>();

        var root = Mount(
            owner,
            new Navigator(
                initialRoute: BuildRoute("root", context =>
                {
                    navigator = Navigator.Of(context);
                    return new SizedBox();
                })));

        navigator!.Push(BuildRoute(
            "details",
            _ => new PopScope<string>(
                canPop: true,
                child: new PopScopeProbe(
                    state => probe = state,
                    (didPop, result) => invocations.Add((didPop, result))))));
        owner.FlushBuild();

        Assert.True(navigator.MaybePop("blocked"));
        Assert.Equal("details", navigator.CurrentRoute?.Settings.Name);
        Assert.Equal([(false, "blocked")], invocations);

        probe!.SetCanPop(true);
        owner.FlushBuild();

        Assert.True(navigator.MaybePop("accepted"));
        owner.FlushBuild();

        Assert.Equal("root", navigator.CurrentRoute?.Settings.Name);
        Assert.Equal([(false, "blocked"), (true, "accepted")], invocations);

        root.Unmount();
    }

    [Fact]
    public void NavigatorPop_BypassesPopScopeVeto_AndReportsSuccessfulResult()
    {
        var owner = new BuildOwner();
        NavigatorState? navigator = null;
        bool? didPop = null;
        string? callbackResult = null;

        var root = Mount(
            owner,
            new Navigator(
                initialRoute: BuildRoute("root", context =>
                {
                    navigator = Navigator.Of(context);
                    return new SizedBox();
                })));

        navigator!.Push(BuildRoute(
            "details",
            _ => new PopScope<string>(
                canPop: false,
                onPopInvokedWithResult: (resultDidPop, result) =>
                {
                    didPop = resultDidPop;
                    callbackResult = result;
                },
                child: new SizedBox())));
        owner.FlushBuild();

        navigator.Pop("forced");
        owner.FlushBuild();

        Assert.True(didPop);
        Assert.Equal("forced", callbackResult);
        Assert.Equal("root", navigator.CurrentRoute?.Settings.Name);

        root.Unmount();
    }

    [Fact]
    public void NavigatorPopHandler_UsesChildNavigationNotification_ToHandleOuterPop()
    {
        var owner = new BuildOwner();
        NavigatorState? outerNavigator = null;
        NavigatorState? innerNavigator = null;
        int callbackCount = 0;
        string? callbackResult = null;

        var root = Mount(
            owner,
            new Navigator(
                initialRoute: BuildRoute("outer-root", context =>
                {
                    outerNavigator = Navigator.Of(context);
                    return new SizedBox();
                })));

        outerNavigator!.Push(BuildRoute(
            "outer-details",
            _ => new NavigatorPopHandler<string>(
                onPopWithResult: result =>
                {
                    callbackCount += 1;
                    callbackResult = result;
                    innerNavigator!.Pop(result);
                },
                child: new Navigator(
                    initialRoute: BuildRoute("inner-root", context =>
                    {
                        innerNavigator = Navigator.Of(context);
                        return new SizedBox();
                    })))));
        owner.FlushBuild();
        PumpNavigationNotifications(owner);

        innerNavigator!.Push(BuildRoute("inner-details", _ => new SizedBox()));
        owner.FlushBuild();
        PumpNavigationNotifications(owner);

        Assert.True(outerNavigator.MaybePop("nested-result"));
        owner.FlushBuild();

        Assert.Equal(1, callbackCount);
        Assert.Equal("nested-result", callbackResult);
        Assert.Equal("inner-root", innerNavigator.CurrentRoute?.Settings.Name);
        Assert.Equal("outer-details", outerNavigator.CurrentRoute?.Settings.Name);

        PumpNavigationNotifications(owner);
        Assert.True(outerNavigator.MaybePop("outer-result"));
        owner.FlushBuild();
        Assert.Equal("outer-root", outerNavigator.CurrentRoute?.Settings.Name);
        Assert.Equal(1, callbackCount);

        root.Unmount();
    }

    [Fact]
    public void Form_DelegatesCanPopAndResultCallback_ToPopScope()
    {
        var owner = new BuildOwner();
        NavigatorState? navigator = null;
        bool? didPop = null;
        object? callbackResult = null;

        var root = Mount(
            owner,
            new Navigator(
                initialRoute: BuildRoute("root", context =>
                {
                    navigator = Navigator.Of(context);
                    return new SizedBox();
                })));

        navigator!.Push(BuildRoute(
            "form",
            _ => new Form(
                canPop: false,
                onPopInvokedWithResult: (resultDidPop, result) =>
                {
                    didPop = resultDidPop;
                    callbackResult = result;
                },
                child: new SizedBox())));
        owner.FlushBuild();

        Assert.True(navigator.MaybePop("unsaved"));
        Assert.False(didPop);
        Assert.Equal("unsaved", callbackResult);
        Assert.Equal("form", navigator.CurrentRoute?.Settings.Name);

        root.Unmount();
    }

    private static BuilderPageRoute BuildRoute(string name, Func<BuildContext, Widget> builder)
    {
        return new BuilderPageRoute(builder, new RouteSettings(Name: name));
    }

    private static TestRootElement Mount(BuildOwner owner, Widget widget)
    {
        var root = new TestRootElement(widget);
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        return root;
    }

    private static void PumpNavigationNotifications(BuildOwner owner)
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.01));
        owner.FlushBuild();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.02));
        owner.FlushBuild();
    }

    private sealed class PopScopeProbe : StatefulWidget
    {
        public PopScopeProbe(
            Action<PopScopeProbeState> onState,
            PopInvokedWithResultCallback<string> onPopInvokedWithResult)
        {
            OnState = onState;
            OnPopInvokedWithResult = onPopInvokedWithResult;
        }

        public Action<PopScopeProbeState> OnState { get; }

        public PopInvokedWithResultCallback<string> OnPopInvokedWithResult { get; }

        public override State CreateState()
        {
            return new PopScopeProbeState();
        }
    }

    private sealed class PopScopeProbeState : State
    {
        private bool _canPop;

        private PopScopeProbe CurrentWidget => (PopScopeProbe)StateWidget;

        public override void InitState()
        {
            base.InitState();
            CurrentWidget.OnState(this);
        }

        public void SetCanPop(bool canPop)
        {
            SetState(() => _canPop = canPop);
        }

        public override Widget Build(BuildContext context)
        {
            return new PopScope<string>(
                canPop: _canPop,
                onPopInvokedWithResult: CurrentWidget.OnPopInvokedWithResult,
                child: new SizedBox());
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
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

        public override void Unmount()
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
