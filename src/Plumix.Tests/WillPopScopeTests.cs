using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/will_pop_scope.dart
// (parity tests mirroring flutter/packages/flutter/test/widgets/will_pop_test.dart)

namespace Plumix.Tests;

#pragma warning disable CS0618 // The whole suite covers Flutter's deprecated scoped will-pop surface.

[Collection(SchedulerTestCollection.Name)]
public sealed class WillPopScopeTests
{
    [Fact]
    public void WillPopScope_VetoesThePopWhileItsCallbackReturnsFalse()
    {
        bool allowPop = false;
        var owner = TestBuildOwner.Create();
        NavigatorState? navigator = null;
        var root = new TestRootElement(new Navigator(
            initialRoute: new BuilderPageRoute(
                builder: context =>
                {
                    navigator ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: new RouteSettings(Name: "root"))));
        Mount(root, owner);

        navigator!.Push(new BuilderPageRoute(
            builder: context => new WillPopScope(
                onWillPop: () => Task.FromResult(allowPop),
                child: new SizedBox(width: 1, height: 1)),
            settings: new RouteSettings(Name: "details")));
        owner.PumpFrame();
        Assert.Equal("details", navigator.CurrentRoute?.Settings?.Name);

        Assert.True(TestBuildOwner.Complete(navigator.MaybePop()));
        owner.PumpFrame();
        Assert.Equal("details", navigator.CurrentRoute?.Settings?.Name);

        allowPop = true;
        Assert.True(TestBuildOwner.Complete(navigator.MaybePop()));
        owner.PumpFrame();
        Assert.Equal("root", navigator.CurrentRoute?.Settings?.Name);

        root.UnmountRoot();
    }

    [Fact]
    public void WillPopScope_RegistersItsCallbackWithTheEnclosingModalRouteAndBlocksTheBackGesture()
    {
        var owner = TestBuildOwner.Create();
        NavigatorState? navigator = null;
        var root = new TestRootElement(new Navigator(
            initialRoute: new BuilderPageRoute(
                builder: context =>
                {
                    navigator ??= Navigator.Of(context);
                    return new SizedBox(width: 1, height: 1);
                },
                settings: new RouteSettings(Name: "root"))));
        Mount(root, owner);

        var plain = new BuilderPageRoute(
            builder: _ => new SizedBox(width: 1, height: 1),
            settings: new RouteSettings(Name: "plain"));
        navigator!.Push(plain);
        owner.PumpFrame();
        Assert.False(plain.HasScopedWillPopCallback);

        var guarded = new BuilderPageRoute(
            builder: _ => new WillPopScope(
                onWillPop: () => Task.FromResult(false),
                child: new SizedBox(width: 1, height: 1)),
            settings: new RouteSettings(Name: "guarded"));
        navigator.Push(guarded);
        owner.PumpFrame();

        Assert.True(guarded.HasScopedWillPopCallback);
        // A route whose pop might be vetoed refuses the back-swipe gesture, the way Dart's
        // `PageRoute.popGestureEnabled` does.
        Assert.False(guarded.PopGestureEnabled);
        Assert.Equal(RoutePopDisposition.DoNotPop, TestBuildOwner.Complete(guarded.WillPop()));

        root.UnmountRoot();
    }

    [DebugOnlyFact]
    public void ModalRoute_ScopedWillPopCallbacks_RejectRegistrationOutsideTheTree()
    {
        var route = new BuilderPageRoute(
            builder: _ => new SizedBox(width: 1, height: 1),
            settings: new RouteSettings(Name: "detached"));

        Assert.Throws<InvalidOperationException>(() => route.AddScopedWillPopCallback(() => Task.FromResult(true)));
        Assert.Throws<InvalidOperationException>(() => route.RemoveScopedWillPopCallback(() => Task.FromResult(true)));
    }

    [Fact]
    public void Form_OnWillPop_RejectsBeingCombinedWithTheModernPopSurface()
    {
        Assert.Throws<ArgumentException>(() => new Form(
            child: new SizedBox(width: 1, height: 1),
            canPop: false,
            onWillPop: () => Task.FromResult(true)));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.PumpFrame();
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
            _child = UpdateChild(_child, new Directionality(Plumix.UI.TextDirection.Ltr, Widget), Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
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

    }
}

#pragma warning restore CS0618
