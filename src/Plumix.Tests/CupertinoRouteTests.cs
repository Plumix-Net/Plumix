using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/route_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoRouteTests : IDisposable
{
    public CupertinoRouteTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void PageRoute_ExposesPinnedDefaultsAndRequiresInstallationForPreviousTitle()
    {
        var route = new CupertinoPageRoute<string>(
            _ => new SizedBox(),
            title: "Details");

        Assert.Equal("Details", route.Title);
        Assert.True(route.MaintainState);
        Assert.False(route.FullscreenDialog);
        Assert.True(route.AllowSnapshotting);
        Assert.False(route.BarrierDismissible);
        Assert.Equal(Color.FromUInt32(0x18000000), route.BarrierColor);
        Assert.Null(route.BarrierLabel);
        Assert.Equal(TimeSpan.FromMilliseconds(500), route.TransitionDuration);
        Assert.NotNull(route.DelegatedTransition);
        Assert.Throws<InvalidOperationException>(() => route.PreviousTitle);

        var fullscreen = new CupertinoPageRoute<object?>(
            _ => new SizedBox(),
            fullscreenDialog: true,
            allowSnapshotting: false,
            barrierDismissible: true);
        Assert.Null(fullscreen.BarrierColor);
        Assert.False(fullscreen.AllowSnapshotting);
        Assert.True(fullscreen.BarrierDismissible);
    }

    [Fact]
    public void PreviousTitle_IsAvailableOnFirstFrameAndTracksRouteReplacement()
    {
        NavigatorState? navigator = null;
        var rootRoute = new CupertinoPageRoute<object?>(
            context =>
            {
                navigator ??= Navigator.Of(context);
                return new SizedBox();
            },
            title: "Root");
        TestRootElement root = MountNavigator(rootRoute);
        try
        {
            var detailsRoute = new CupertinoPageRoute<object?>(
                _ => new SizedBox(),
                title: "Details");
            navigator!.Push(detailsRoute);
            root.TestOwner.FlushBuild();

            Assert.Equal("Root", detailsRoute.PreviousTitle.Value);

            var replacement = new CupertinoPageRoute<object?>(
                _ => new SizedBox(),
                title: "Replacement");
            navigator.Replace(rootRoute, replacement);
            root.TestOwner.FlushBuild();

            Assert.Equal("Replacement", detailsRoute.PreviousTitle.Value);
        }
        finally
        {
            root.Unmount();
        }
    }

    [Fact]
    public void PageBackedRoute_UpdatesContentTitleAndMaintainStateWithoutReplacingTheRoute()
    {
        var firstChild = new SizedBox(width: 10.0, height: 10.0);
        var firstPage = new CupertinoPage<object?>(
            child: firstChild,
            title: "First",
            maintainState: false,
            fullscreenDialog: true,
            allowSnapshotting: false,
            name: "/details");
        var route = Assert.IsType<PageBasedCupertinoPageRoute<object?>>(firstPage.CreateRoute(default));

        Assert.Same(firstPage, route.Settings);
        Assert.Equal("First", route.Title);
        Assert.False(route.MaintainState);
        Assert.True(route.FullscreenDialog);
        Assert.False(route.AllowSnapshotting);
        Assert.Null(route.DelegatedTransition);

        var secondChild = new SizedBox(width: 20.0, height: 20.0);
        var secondPage = new CupertinoPage<object?>(
            child: secondChild,
            title: "Second",
            maintainState: true,
            name: "/details");
        route.UpdateSettings(secondPage);

        Assert.Equal("Second", route.Title);
        Assert.True(route.MaintainState);
        Assert.False(route.FullscreenDialog);
        Assert.True(route.AllowSnapshotting);
        Assert.NotNull(route.DelegatedTransition);
        var semantics = Assert.IsType<Semantics>(route.BuildPage(default));
        Assert.Same(secondChild, semantics.Child);
    }

    [Fact]
    public void PageRoutes_BuildHorizontalOrFullscreenTransitionsAndSuppressFullscreenParallax()
    {
        NavigatorState? navigator = null;
        var rootRoute = new CupertinoPageRoute<object?>(context =>
        {
            navigator ??= Navigator.Of(context);
            return new SizedBox();
        });
        TestRootElement root = MountNavigator(rootRoute);
        try
        {
            Assert.NotEmpty(FindWidgets<CupertinoPageTransition>(root));

            var fullscreen = new CupertinoPageRoute<object?>(
                _ => new SizedBox(),
                fullscreenDialog: true);
            navigator!.Push(fullscreen);
            root.TestOwner.FlushBuild();

            Assert.NotEmpty(FindWidgets<CupertinoFullscreenDialogTransition>(root));
            Assert.False(rootRoute.CanTransitionTo(fullscreen));
            Assert.False(fullscreen.CanTransitionFrom(rootRoute));
        }
        finally
        {
            root.Unmount();
        }
    }

    [Fact]
    public void ModalPopupRoute_UsesPinnedDefaultsElevatedSubScreenAndBottomTranslation()
    {
        var route = new CupertinoModalPopupRoute<string>(_ => new Text("Popup"));

        Assert.True(route.BarrierDismissible);
        Assert.False(route.SemanticsDismissible);
        Assert.Equal("Dismiss", route.BarrierLabel);
        Assert.Equal(Color.FromUInt32(0x33000000), route.BarrierColor);
        Assert.Equal(TimeSpan.FromMilliseconds(335), route.TransitionDuration);
        Assert.False(route.AllowSnapshotting);

        var level = Assert.IsType<CupertinoUserInterfaceLevel>(route.BuildPage(default));
        Assert.Equal(CupertinoUserInterfaceLevelData.Elevated, level.Data);
        var subScreen = Assert.IsType<DisplayFeatureSubScreen>(level.Child);
        Assert.IsType<Builder>(subScreen.Child);

        var transition = Assert.IsType<Align>(route.BuildTransitions(
            default,
            new ConstantAnimation<double>(0.25),
            new ConstantAnimation<double>(0.0),
            new SizedBox()));
        Assert.Equal(Alignment.BottomCenter, transition.Alignment.Resolve(TextDirection.Ltr));
        var translation = Assert.IsType<FractionalTranslation>(transition.Child);
        Assert.Equal(new Vector(0.0, 0.75), translation.Translation);
    }

    [Fact]
    public async Task PageAndPopupRoutes_CompleteWithTypedResults()
    {
        var pageRoute = new CupertinoPageRoute<string>(_ => new SizedBox());
        pageRoute.DidComplete("page-result");
        Assert.Equal("page-result", await pageRoute.Completed);

        var popupRoute = new CupertinoModalPopupRoute<int>(_ => new SizedBox());
        popupRoute.DidComplete(42);
        Assert.Equal(42, await popupRoute.Completed);
    }

    private static TestRootElement MountNavigator(Route initialRoute)
    {
        Widget rootWidget = new MediaQuery(
            new MediaQueryData(Size: new Size(400.0, 800.0)),
            new Directionality(
                TextDirection.Ltr,
                new Navigator(initialRoute)));
        var root = new TestRootElement(rootWidget);
        root.Attach(root.TestOwner);
        root.Mount(parent: null, newSlot: null);
        root.TestOwner.FlushBuild();
        Settle(root.TestOwner);
        return root;
    }

    private static void Settle(BuildOwner owner)
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
    }

    private static IReadOnlyList<T> FindWidgets<T>(Element root) where T : Widget
    {
        var widgets = new List<T>();
        CollectWidgets(root, widgets);
        return widgets;
    }

    private static void CollectWidgets<T>(Element element, ICollection<T> widgets) where T : Widget
    {
        if (element.Widget is T match)
        {
            widgets.Add(match);
        }

        element.VisitChildren(child => CollectWidgets(child, widgets));
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public BuildOwner TestOwner { get; } = new();

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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
