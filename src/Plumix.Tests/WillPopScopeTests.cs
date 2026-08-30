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
    public WillPopScopeTests() => NavigatorBackButtonDispatcher.ResetForTests();

    [Fact]
    public void WillPopScope_VetoesThePopWhileItsCallbackReturnsFalse()
    {
        bool allowPop = false;
        var owner = new BuildOwner();
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
                onWillPop: () => allowPop,
                child: new SizedBox(width: 1, height: 1)),
            settings: new RouteSettings(Name: "details")));
        owner.FlushBuild();
        Assert.Equal("details", navigator.CurrentRoute?.Settings?.Name);

        Assert.True(navigator.MaybePop());
        owner.FlushBuild();
        Assert.Equal("details", navigator.CurrentRoute?.Settings?.Name);

        allowPop = true;
        Assert.True(navigator.MaybePop());
        owner.FlushBuild();
        Assert.Equal("root", navigator.CurrentRoute?.Settings?.Name);

        root.Unmount();
    }

    [Fact]
    public void WillPopScope_RegistersItsCallbackWithTheEnclosingModalRouteAndBlocksTheBackGesture()
    {
        var owner = new BuildOwner();
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
        owner.FlushBuild();
        Assert.False(plain.HasScopedWillPopCallback);

        var guarded = new BuilderPageRoute(
            builder: _ => new WillPopScope(
                onWillPop: () => false,
                child: new SizedBox(width: 1, height: 1)),
            settings: new RouteSettings(Name: "guarded"));
        navigator.Push(guarded);
        owner.FlushBuild();

        Assert.True(guarded.HasScopedWillPopCallback);
        // A route whose pop might be vetoed refuses the back-swipe gesture, the way Dart's
        // `PageRoute.popGestureEnabled` does.
        Assert.False(guarded.PopGestureEnabled);
        Assert.False(guarded.WillPop());

        root.Unmount();
    }

    [DebugOnlyFact]
    public void ModalRoute_ScopedWillPopCallbacks_RejectRegistrationOutsideTheTree()
    {
        var route = new BuilderPageRoute(
            builder: _ => new SizedBox(width: 1, height: 1),
            settings: new RouteSettings(Name: "detached"));

        Assert.Throws<InvalidOperationException>(() => route.AddScopedWillPopCallback(() => true));
        Assert.Throws<InvalidOperationException>(() => route.RemoveScopedWillPopCallback(() => true));
    }

    [Fact]
    public void Form_OnWillPop_RejectsBeingCombinedWithTheModernPopSurface()
    {
        Assert.Throws<ArgumentException>(() => new Form(
            child: new SizedBox(width: 1, height: 1),
            canPop: false,
            onWillPop: () => true));
    }

    private static void Mount(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
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

        internal override void Unmount()
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

#pragma warning restore CS0618
