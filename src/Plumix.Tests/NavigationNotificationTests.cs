using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/navigator.dart
// flutter/packages/flutter/lib/src/widgets/routes.dart
// flutter/packages/flutter/lib/src/widgets/pop_scope.dart

namespace Plumix.Tests;

/// <summary>
/// Flutter's <c>NavigationNotification</c> flow: <c>NavigatorState._handleHistoryChanged</c>,
/// <c>ModalRoute._maybeDispatchNavigationNotification</c> and the
/// <c>NotificationListener&lt;NavigationNotification&gt;</c> in <c>NavigatorState.build</c>.
/// Mirrors <c>routes_test.dart</c> > <c>NavigationNotifications</c> and <c>pop_scope_test.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class NavigationNotificationTests : IDisposable
{
    private readonly List<bool> _notifications = [];

    public NavigationNotificationTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void WithNoPopScope_OneNotificationReportsThatNothingHandlesThePop()
    {
        var owner = new BuildOwner();
        TestRootElement root = MountNavigator(owner, out _, _ => new SizedBox());

        Pump(owner);

        Assert.Equal([false], _notifications);
        root.Unmount();
    }

    [Fact]
    public void WithAWillPopCallback_ASecondNotificationReportsThatThePopIsHandled()
    {
        var owner = new BuildOwner();
        ModalRoute? route = null;
        TestRootElement root = MountNavigator(owner, out _, context =>
        {
            route = ModalRoute.MaybeOf(context);
            return new SizedBox();
        });
        owner.FlushBuild();
#pragma warning disable CS0618 // Dart's routes_test covers this through the deprecated WillPopScope.
        route!.AddScopedWillPopCallback(() => false);
#pragma warning restore CS0618

        Pump(owner);

        // Dart's routes_test asserts exactly these two, in this order: the navigator's own history
        // change first — a will-pop callback does not change PopDisposition, so the navigator still
        // reports false — then the route reporting that its callback handles the pop.
        Assert.Equal([false, true], _notifications);
        root.Unmount();
    }

    [Fact]
    public void WithAPopScope_TheNavigatorAlsoReportsTheBlockedPop()
    {
        var owner = new BuildOwner();
        TestRootElement root = MountNavigator(
            owner,
            out _,
            _ => new PopScope<object>(canPop: false, child: new SizedBox()));

        Pump(owner);

        // Unlike a will-pop callback, a PopScope moves the route's PopDisposition to DoNotPop, which
        // `_getNavigatorCanHandlePop` reads, so both notifications report a handled pop.
        Assert.Equal([true, true], _notifications);
        root.Unmount();
    }

    [Fact]
    public void TogglingCanPopOnTheRootRoute_FlipsPopDispositionAndTheNotification()
    {
        var owner = new BuildOwner();
        Action<bool>? setCanPop = null;
        ModalRoute? route = null;
        TestRootElement root = MountNavigator(owner, out _, context =>
        {
            route = ModalRoute.MaybeOf(context);
            return new StatefulBuilder((_, setState) =>
            {
                setCanPop = next => setState(() => _canPop = next);
                return new PopScope<object>(canPop: _canPop, child: new SizedBox());
            });
        });
        Pump(owner);

        Assert.Equal(RoutePopDisposition.DoNotPop, route!.PopDisposition);
        Assert.Equal([true, true], _notifications);

        _notifications.Clear();
        setCanPop!(true);
        Pump(owner);

        Assert.Equal(RoutePopDisposition.Bubble, route.PopDisposition);
        Assert.Equal([false], _notifications);
        root.Unmount();
    }

    [Fact]
    public void RemovingThePopScopeFromTheTree_RemovesItsEffectOnNavigation()
    {
        var owner = new BuildOwner();
        Action<bool>? setPresent = null;
        ModalRoute? route = null;
        bool present = true;
        TestRootElement root = MountNavigator(owner, out _, context =>
        {
            route = ModalRoute.MaybeOf(context);
            return new StatefulBuilder((_, setState) =>
            {
                setPresent = next => setState(() => present = next);
                return present
                    ? new PopScope<object>(canPop: false, child: new SizedBox())
                    : new SizedBox();
            });
        });
        Pump(owner);

        Assert.Equal(RoutePopDisposition.DoNotPop, route!.PopDisposition);

        _notifications.Clear();
        setPresent!(false);
        Pump(owner);

        Assert.Equal(RoutePopDisposition.Bubble, route.PopDisposition);
        Assert.Equal([false], _notifications);
        root.Unmount();
    }

    [Fact]
    public void IdenticalPopScopes_KeepBlockingUntilTheLastOneIsRemoved()
    {
        var owner = new BuildOwner();
        Action<int>? setCount = null;
        ModalRoute? route = null;
        int count = 2;
        TestRootElement root = MountNavigator(owner, out _, context =>
        {
            route = ModalRoute.MaybeOf(context);
            return new StatefulBuilder((_, setState) =>
            {
                setCount = next => setState(() => count = next);
                Widget child = new SizedBox();
                for (int index = 0; index < count; index++)
                {
                    child = new PopScope<object>(canPop: false, child: child);
                }

                return child;
            });
        });
        Pump(owner);

        Assert.Equal(RoutePopDisposition.DoNotPop, route!.PopDisposition);

        setCount!(1);
        Pump(owner);
        Assert.Equal(RoutePopDisposition.DoNotPop, route.PopDisposition);

        _notifications.Clear();
        setCount(0);
        Pump(owner);
        Assert.Equal(RoutePopDisposition.Bubble, route.PopDisposition);
        Assert.Equal([false], _notifications);
        root.Unmount();
    }

    [Fact]
    public void TogglingCanPopOnASecondaryRoute_UpdatesTheNotification()
    {
        var owner = new BuildOwner();
        TestRootElement root = MountNavigator(owner, out NavigatorState? navigator, _ => new SizedBox());
        Pump(owner);
        Assert.Equal([false], _notifications);

        _notifications.Clear();
        navigator!.Push(new BuilderPageRoute(
            _ => new PopScope<object>(canPop: false, child: new SizedBox()),
            new RouteSettings(Name: "details")));
        Pump(owner);

        // Pushing a second route makes the navigator itself able to pop, and the pushed route's
        // PopScope then reports that it handles the pop instead.
        Assert.Contains(true, _notifications);
        Assert.Equal(
            RoutePopDisposition.DoNotPop,
            ((ModalRoute)navigator.CurrentRoute!).PopDisposition);
        root.Unmount();
    }

    [Fact]
    public void ANonCurrentRoute_DoesNotDispatch()
    {
        var owner = new BuildOwner();
        ModalRoute? bottomRoute = null;
        TestRootElement root = MountNavigator(owner, out NavigatorState? navigator, context =>
        {
            bottomRoute = ModalRoute.MaybeOf(context);
            return new SizedBox();
        });
        navigator!.Push(new BuilderPageRoute(_ => new SizedBox(), new RouteSettings(Name: "top")));
        Pump(owner);

        _notifications.Clear();
        // The route is no longer current, so `_maybeDispatchNavigationNotification` returns early.
        bottomRoute!.DidPopNext(navigator.CurrentRoute!);
        Pump(owner);

        Assert.Empty(_notifications);
        root.Unmount();
    }

    [Fact]
    public void ANestedNavigatorThatCanPop_UpgradesTheNotificationToTrue()
    {
        var owner = new BuildOwner();
        NavigatorState? inner = null;
        TestRootElement root = MountNavigator(owner, out _, _ => new Navigator(
            initialRoute: new BuilderPageRoute(
                context =>
                {
                    inner = Navigator.Of(context);
                    return new SizedBox();
                },
                new RouteSettings(Name: "inner-root"))));
        Pump(owner);

        _notifications.Clear();
        inner!.Push(new BuilderPageRoute(_ => new SizedBox(), new RouteSettings(Name: "inner-details")));
        Pump(owner);

        // The inner navigator can pop, so its listener absorbs the `false` notification and
        // re-dispatches `true` from its own context, above its own listener.
        Assert.Contains(true, _notifications);
        Assert.DoesNotContain(false, _notifications);
        root.Unmount();
    }

    [Fact]
    public void NotificationsNeverDispatchDuringABuild()
    {
        var owner = new BuildOwner();
        var duringBuild = new List<bool>();
        TestRootElement root = MountNavigator(owner, out NavigatorState? navigator, _ =>
            new PopScope<object>(canPop: false, child: new SizedBox()));

        // Every dispatch this test sees must arrive from a post-frame callback, never from inside
        // the flush that registered the pop entry or pushed the route.
        bool building = true;
        _onNotification = value =>
        {
            if (building)
            {
                duringBuild.Add(value);
            }
        };
        owner.FlushBuild();
        navigator!.Push(new BuilderPageRoute(_ => new SizedBox(), new RouteSettings(Name: "top")));
        owner.FlushBuild();
        building = false;

        Assert.Empty(duringBuild);
        Pump(owner);
        Assert.NotEmpty(_notifications);
        root.Unmount();
    }

    private bool _canPop;

    private Action<bool>? _onNotification;

    private TestRootElement MountNavigator(
        BuildOwner owner,
        out NavigatorState? navigator,
        Func<BuildContext, Widget> builder)
    {
        NavigatorState? captured = null;
        var widget = new NotificationListener<NavigationNotification>(
            onNotification: notification =>
            {
                _notifications.Add(notification.CanHandlePop);
                _onNotification?.Invoke(notification.CanHandlePop);
                return true;
            },
            child: new Navigator(
                initialRoute: new BuilderPageRoute(
                    context =>
                    {
                        captured = Navigator.Of(context);
                        return builder(context);
                    },
                    new RouteSettings(Name: "root"))));

        var root = new TestRootElement(widget);
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();
        navigator = captured;
        return root;
    }

    private static void Pump(BuildOwner owner)
    {
        for (int index = 0; index < 3; index++)
        {
            owner.FlushBuild();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.01));
            owner.FlushBuild();
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

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
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
