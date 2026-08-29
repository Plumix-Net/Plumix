using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/sheet_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoSheetTests : IDisposable
{
    public CupertinoSheetTests()
    {
        Scheduler.ResetForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    /// <summary>
    /// The sheet's vertical drag recognizer, built from the `gestures` map the detector registers.
    /// </summary>
    private static VerticalDragGestureRecognizer? FindSheetDragRecognizer(Element root)
    {
        foreach (RawGestureDetector detector in FindWidgets<RawGestureDetector>(root))
        {
            if (detector.Gestures.TryGetValue(typeof(VerticalDragGestureRecognizer), out var factory))
            {
                var recognizer = (VerticalDragGestureRecognizer)factory.ConstructorRaw();
                factory.InitializerRaw(recognizer);
                return recognizer;
            }
        }

        return null;
    }

    [Fact]
    public void Route_ExposesPinnedDefaultsAndValidatesBuildersAndTopGap()
    {
        var route = new CupertinoSheetRoute<string>(builder: _ => new SizedBox());

        Assert.True(route.EnableDrag);
        Assert.False(route.ShowDragHandle);
        Assert.Equal(0.08, route.TopGap);
        Assert.Equal(TimeSpan.FromMilliseconds(500), route.TransitionDuration);
        Assert.Equal(CupertinoColors.Transparent, route.BarrierColor);
        Assert.False(route.BarrierDismissible);
        Assert.Null(route.BarrierLabel);
        Assert.True(route.MaintainState);
        Assert.False(route.Opaque);
        Assert.NotNull(route.DelegatedTransition);

        Assert.Throws<ArgumentException>(() => new CupertinoSheetRoute<object?>());
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoSheetRoute<object?>(
            builder: _ => new SizedBox(),
            topGap: -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoSheetRoute<object?>(
            builder: _ => new SizedBox(),
            topGap: 0.91));

        var custom = new CupertinoSheetRoute<object?>(
            builder: _ => new SizedBox(),
            topGap: 0.08);
        Assert.Equal(0.08, custom.TopGap);
        Assert.Null(custom.DelegatedTransition);
        Assert.False(custom.CanTransitionFrom(route));
        Assert.False(custom.CanTransitionTo(route));
    }

    [Fact]
    public void Route_PrefersScrollableBuilderAndBuildsElevatedClippedContentWithHandle()
    {
        bool plainBuilderCalled = false;
        bool scrollableBuilderCalled = false;
        var route = new CupertinoSheetRoute<object?>(
            builder: _ =>
            {
                plainBuilderCalled = true;
                return new Text("plain");
            },
            scrollableBuilder: (_, _) =>
            {
                scrollableBuilderCalled = true;
                return new SafeArea(child: new Text("scrollable"));
            },
            showDragHandle: true);
        SheetHarness harness = MountSheet(route);
        try
        {
            Assert.False(plainBuilderCalled);
            Assert.True(scrollableBuilderCalled);
            Assert.NotEmpty(FindWidgets<CupertinoUserInterfaceLevel>(harness.Root));
            Assert.Contains(
                FindWidgets<ClipRSuperellipse>(harness.Root),
                clip => clip.BorderRadius.Resolve(TextDirection.Ltr).TopLeft == 12.0);
            Assert.Contains(
                FindWidgets<ShapeDecoration>(harness.Root),
                decoration => decoration.Color == Color.FromArgb(76, 60, 60, 67));

            MediaQuery bodyMedia = Assert.Single(
                FindWidgets<MediaQuery>(harness.Root),
                query => query.Data.Padding.Top == 15.0);
            Assert.Equal(new Thickness(0.0, 15.0, 0.0, 0.0), bodyMedia.Data.Padding);
            Assert.Contains(
                FindWidgets<SizedBox>(harness.Root),
                box => box.Width == 36.0 && box.Height == 5.0);
        }
        finally
        {
            harness.Root.Unmount();
        }
    }

    [Fact]
    public void Transition_UsesExactTopGapAndCoveredSheetGeometry()
    {
        var primary = new ConstantAnimation<double>(1.0);
        var secondary = new ConstantAnimation<double>(0.5);
        var transition = new CupertinoSheetTransition(
            primaryRouteAnimation: primary,
            secondaryRouteAnimation: secondary,
            linearTransition: false,
            topGap: 0.15,
            child: new SizedBox());
        var root = MountWidget(transition);
        try
        {
            Padding topPadding = Assert.Single(FindWidgets<Padding>(root));
            Assert.Equal(120.0, topPadding.Insets.Top, precision: 6);

            ScaleTransition scale = Assert.Single(FindWidgets<ScaleTransition>(root));
            Assert.Equal(Alignment.TopCenter, scale.Alignment);
            Assert.Equal(FilterQuality.Medium, scale.FilterQuality);
            Assert.InRange(scale.Scale.Value, 0.9165, 1.0);

            SlideTransition covered = FindWidgets<SlideTransition>(root)
                .Single(slide => !slide.TransformHitTests);
            Assert.InRange(covered.Position.Value.Y, -0.005, 0.0);
        }
        finally
        {
            root.Unmount();
        }
    }

    [Fact]
    public void PartialDragRestoresButLongDragPopsTheSheet()
    {
        SheetHarness partial = MountSheet(new CupertinoSheetRoute<object?>(builder: _ => new SizedBox()));
        try
        {
            VerticalDragGestureRecognizer recognizer = Assert.IsType<VerticalDragGestureRecognizer>(
                FindSheetDragRecognizer(partial.Root));
            recognizer.OnStart!(new DragStartDetails(default));
            recognizer.OnUpdate!(new DragUpdateDetails(default, default, new Point(0.0, 200.0), 200.0));
            recognizer.OnEnd!(new DragEndDetails(0.0));
            Settle(partial.Root.TestOwner);

            Assert.Same(partial.SheetRoute, partial.Navigator.CurrentRoute);
        }
        finally
        {
            partial.Root.Unmount();
        }

        SheetHarness longDrag = MountSheet(new CupertinoSheetRoute<object?>(builder: _ => new SizedBox()));
        try
        {
            VerticalDragGestureRecognizer recognizer = Assert.IsType<VerticalDragGestureRecognizer>(
                FindSheetDragRecognizer(longDrag.Root));
            recognizer.OnStart!(new DragStartDetails(default));
            recognizer.OnUpdate!(new DragUpdateDetails(default, default, new Point(0.0, 400.0), 400.0));
            recognizer.OnEnd!(new DragEndDetails(0.0));
            Settle(longDrag.Root.TestOwner);

            Assert.Same(longDrag.RootRoute, longDrag.Navigator.CurrentRoute);
        }
        finally
        {
            longDrag.Root.Unmount();
        }
    }

    [Fact]
    public void DisabledDragDoesNotInstallTheSheetDragDetector()
    {
        SheetHarness harness = MountSheet(new CupertinoSheetRoute<object?>(
            builder: _ => new SizedBox(),
            enableDrag: false));
        try
        {
            Assert.Null(FindSheetDragRecognizer(harness.Root));
        }
        finally
        {
            harness.Root.Unmount();
        }
    }

    [Fact]
    public void ShowCupertinoSheet_PushesRootRoutePreservesSettingsAndSupportsNestedNavigation()
    {
        BuildContext? pageContext = null;
        NavigatorState? navigator = null;
        var rootRoute = new CupertinoPageRoute<object?>(context =>
        {
            pageContext = context;
            navigator = Navigator.Of(context);
            return new SizedBox();
        });
        TestRootElement root = MountNavigator(rootRoute);
        try
        {
            _ = CupertinoSheets.ShowCupertinoSheet<object?>(
                pageContext!.Value,
                builder: _ => new Text("nested sheet"),
                useNestedNavigation: true,
                settings: new RouteSettings(Name: "/sheet"),
                showDragHandle: true);
            root.TestOwner.FlushBuild();
            Settle(root.TestOwner);

            var sheetRoute = Assert.IsType<CupertinoSheetRoute<object?>>(navigator!.CurrentRoute);
            Assert.Equal("/sheet", sheetRoute.Settings.Name);
            Assert.False(sheetRoute.ShowDragHandle);
            Assert.Equal(2, FindWidgets<Navigator>(root).Count);
            Assert.NotEmpty(FindWidgets<NavigatorPopHandler<object?>>(root));
        }
        finally
        {
            root.Unmount();
        }
    }

    [Fact]
    public async Task Route_CompletesWithTypedResult()
    {
        var route = new CupertinoSheetRoute<string>(builder: _ => new SizedBox());
        route.DidComplete("done");
        Assert.Equal("done", await route.Completed);
    }

    private static SheetHarness MountSheet(CupertinoSheetRoute<object?> sheetRoute)
    {
        NavigatorState? navigator = null;
        var rootRoute = new CupertinoPageRoute<object?>(context =>
        {
            navigator = Navigator.Of(context);
            return new SizedBox();
        });
        TestRootElement root = MountNavigator(rootRoute);
        navigator!.Push(sheetRoute);
        root.TestOwner.FlushBuild();
        Settle(root.TestOwner);
        return new SheetHarness(root, navigator, rootRoute, sheetRoute);
    }

    private static TestRootElement MountNavigator(Route initialRoute)
    {
        Widget widget = new MediaQuery(
            new MediaQueryData(
                Size: new Size(400.0, 800.0),
                ViewPadding: new Thickness(0.0, 24.0, 0.0, 20.0)),
            new Directionality(
                TextDirection.Ltr,
                new CupertinoTheme(
                    new CupertinoThemeData(brightness: PlatformBrightness.Light),
                    new Navigator(initialRoute))));
        return MountWidget(widget);
    }

    private static TestRootElement MountWidget(Widget widget)
    {
        Widget wrapped = widget is MediaQuery
            ? widget
            : new MediaQuery(
                new MediaQueryData(Size: new Size(400.0, 800.0)),
                new Directionality(TextDirection.Ltr, widget));
        var root = new TestRootElement(wrapped);
        root.Attach(root.TestOwner);
        root.Mount(parent: null, newSlot: null);
        root.TestOwner.FlushBuild();
        return root;
    }

    private static void Settle(BuildOwner owner)
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
    }

    private static IReadOnlyList<T> FindWidgets<T>(Element root) where T : class
    {
        var matches = new List<T>();
        CollectWidgets(root, matches);
        return matches;
    }

    private static void CollectWidgets<T>(Element element, ICollection<T> matches) where T : class
    {
        if (element.Widget is T match)
        {
            matches.Add(match);
        }
        else if (element.Widget is DecoratedBox decorated && decorated.Decoration is T decoration)
        {
            matches.Add(decoration);
        }

        element.VisitChildren(child => CollectWidgets(child, matches));
    }

    private sealed record SheetHarness(
        TestRootElement Root,
        NavigatorState Navigator,
        Route RootRoute,
        CupertinoSheetRoute<object?> SheetRoute);

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
            if (_child is not null)
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
