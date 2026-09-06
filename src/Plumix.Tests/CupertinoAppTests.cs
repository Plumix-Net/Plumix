using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/app_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoAppTests : IDisposable
{
    private readonly TargetPlatform? _previousPlatform = PlatformDefaults.DebugTargetPlatformOverride;

    public CupertinoAppTests()
    {
        Scheduler.ResetForTests();
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = _previousPlatform;
        Scheduler.ResetForTests();
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
        SystemChrome.ResetSystemUiOverlayStyleForTests();
    }

    [Fact]
    public void App_ExposesPinnedDefaultsAndValidatesRoutingContracts()
    {
        var app = new CupertinoApp(home: new SizedBox());

        Assert.Null(app.Theme);
        Assert.Null(app.Title);
        Assert.Single(app.SupportedLocales);
        Assert.Equal(new Locale("en", "US"), app.SupportedLocales[0]);
        Assert.Empty(app.Routes);
        Assert.Empty(app.NavigatorObservers);
        Assert.False(app.ShowPerformanceOverlay);
        Assert.False(app.CheckerboardRasterCacheImages);
        Assert.False(app.CheckerboardOffscreenLayers);
        Assert.False(app.ShowSemanticsDebugger);
        Assert.True(app.DebugShowCheckedModeBanner);
        Assert.False(app.UseInheritedMediaQuery);

        HeroController controller = CupertinoApp.CreateCupertinoHeroController();
        Assert.False(controller.IsDisposed);
        controller.Dispose();
        Assert.True(controller.IsDisposed);

        Assert.Throws<ArgumentException>(() => new CupertinoApp());
        Assert.Throws<ArgumentException>(() => new CupertinoApp(supportedLocales: [], home: new SizedBox()));
        Assert.Throws<ArgumentException>(() => new CupertinoApp(
            home: new SizedBox(),
            routes: new Dictionary<string, WidgetBuilder> { ["/"] = _ => new SizedBox() }));
        Assert.Throws<ArgumentException>(() => new CupertinoApp(
            navigatorKey: new LabeledGlobalKey<NavigatorState>("builder-only navigator"),
            builder: (_, _) => new SizedBox()));
        Assert.Throws<ArgumentException>(() => CupertinoApp.Router<RouteInformation>());
    }

    [Fact]
    public void App_ComposesThemeSelectionLocalizationRoutingScrollAndSystemChrome()
    {
        Color lightPrimary = Color.FromRgb(10, 20, 30);
        Color darkPrimary = Color.FromRgb(40, 50, 60);
        var primary = CupertinoDynamicColor.WithBrightness(lightPrimary, darkPrimary);
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("Cupertino app navigator");
        CupertinoThemeData? resolvedTheme = null;
        DefaultSelectionStyle? selectionStyle = null;
        CupertinoLocalizations? localizations = null;
        ScrollBehavior? scrollBehavior = null;
        CupertinoUserInterfaceLevelData? interfaceLevel = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
            child: new CupertinoApp(
                navigatorKey: navigatorKey,
                debugShowCheckedModeBanner: false,
                title: "Cupertino shell",
                theme: new CupertinoThemeData(primaryColor: primary),
                home: new Builder(context =>
                {
                    resolvedTheme = CupertinoTheme.Of(context);
                    selectionStyle = DefaultSelectionStyle.Of(context);
                    localizations = CupertinoLocalizations.Of(context);
                    scrollBehavior = ScrollConfiguration.Of(context);
                    interfaceLevel = CupertinoUserInterfaceLevel.Of(context);
                    return new SizedBox();
                }))));

        MountAndFlush(root, owner);

        Assert.Equal(darkPrimary, resolvedTheme!.PrimaryColor.Value);
        Assert.Equal(Color.FromArgb(51, darkPrimary.R, darkPrimary.G, darkPrimary.B),
            selectionStyle!.SelectionColor);
        Assert.Equal(darkPrimary, selectionStyle.CursorColor);
        Assert.Same(DefaultCupertinoLocalizations.Instance, localizations);
        Assert.IsType<CupertinoScrollBehavior>(scrollBehavior);
        Assert.Equal(CupertinoUserInterfaceLevelData.Base, interfaceLevel);
        Assert.IsType<CupertinoPageRoute<object?>>(navigatorKey.CurrentState!.CurrentRoute);
        Assert.Equal(SystemUiIconBrightness.Light,
            SystemChrome.CurrentSystemUiOverlayStyle.StatusBarIconBrightness);
        Assert.Equal(
            new ApplicationSwitcherDescription("Cupertino shell", 0xFF28323C),
            SystemChrome.CurrentApplicationSwitcherDescription);
        root.Unmount();
    }

    [Fact]
    public void App_UsesCallerLocalizationAndScrollBehaviorBeforeDefaults()
    {
        CupertinoLocalizations? localizations = null;
        ScrollBehavior? scrollBehavior = null;
        var customScrollBehavior = new TestScrollBehavior();
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(),
            child: new CupertinoApp(
                debugShowCheckedModeBanner: false,
                scrollBehavior: customScrollBehavior,
                localizationsDelegates: [new TestCupertinoLocalizationsDelegate()],
                home: new Builder(context =>
                {
                    localizations = CupertinoLocalizations.Of(context);
                    scrollBehavior = ScrollConfiguration.Of(context);
                    return new SizedBox();
                }))));

        MountAndFlush(root, owner);

        Assert.IsType<TestCupertinoLocalizations>(localizations);
        Assert.Equal("Custom Select All", localizations!.SelectAllButtonLabel);
        Assert.Same(customScrollBehavior, scrollBehavior);
        root.Unmount();
    }

    [Fact]
    public void App_ResolvesExplicitDynamicColorAgainstInstalledThemeBrightness()
    {
        Color lightColor = Color.FromRgb(1, 2, 3);
        Color darkColor = Color.FromRgb(4, 5, 6);
        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            child: new CupertinoApp(
                debugShowCheckedModeBanner: false,
                title: "Dynamic title",
                color: CupertinoDynamicColor.WithBrightness(lightColor, darkColor),
                theme: new CupertinoThemeData(brightness: PlatformBrightness.Dark),
                home: new SizedBox())));

        MountAndFlush(root, owner);

        Assert.Equal(
            new ApplicationSwitcherDescription("Dynamic title", 0xFF040506),
            SystemChrome.CurrentApplicationSwitcherDescription);
        root.Unmount();
    }

    [Fact]
    public void AppRouter_BuildsRouterAndPreservesTypedConfiguration()
    {
        bool built = false;
        var routerDelegate = new TestRouterDelegate(() => built = true);
        CupertinoApp app = CupertinoApp.Router(
            routerDelegate: routerDelegate,
            debugShowCheckedModeBanner: false);

        Assert.Same(routerDelegate, app.RouterDelegate);
        Assert.Null(app.RouteInformationParser);
        Assert.Null(app.RouterConfig);

        var owner = new BuildOwner();
        var root = new TestRootElement(new MediaQuery(
            data: new MediaQueryData(),
            child: app));
        MountAndFlush(root, owner);

        Assert.True(built);
        root.Unmount();
    }

    [Fact]
    public void ScrollBehavior_MatchesCupertinoPlatformPolicy()
    {
        var behavior = new CupertinoScrollBehavior();
        var child = new SizedBox();
        var controller = new ScrollController();
        var details = ScrollableDetails.Vertical(controller: controller);

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        var macPhysics = Assert.IsType<BouncingScrollPhysics>(behavior.GetScrollPhysics(null!));
        Assert.Equal(ScrollDecelerationRate.Fast, macPhysics.DecelerationRate);
        var scrollbar = Assert.IsType<CupertinoScrollbar>(behavior.BuildScrollbar(null!, child, details));
        Assert.Same(controller, scrollbar.Controller);
        Assert.Same(child, scrollbar.Child);
        Assert.Throws<InvalidOperationException>(() => behavior.BuildScrollbar(
            null!,
            child,
            ScrollableDetails.Vertical()));

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        var iosPhysics = Assert.IsType<BouncingScrollPhysics>(behavior.GetScrollPhysics(null!));
        Assert.Equal(ScrollDecelerationRate.Normal, iosPhysics.DecelerationRate);
        Assert.Same(child, behavior.BuildScrollbar(null!, child, details));
        Assert.Same(child, behavior.BuildOverscrollIndicator(null!, child, details));
        Assert.Equal(
            MultitouchDragStrategy.AverageBoundaryPointers,
            behavior.GetMultitouchDragStrategy(null!));
        Assert.Equal(
            ScrollViewKeyboardDismissBehavior.OnDrag,
            behavior.CopyWith(keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.OnDrag)
                .GetKeyboardDismissBehavior(null!));
    }

    private static void MountAndFlush(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class TestScrollBehavior : ScrollBehavior
    {
        public override ScrollPhysics GetScrollPhysics(BuildContext context)
        {
            return new NeverScrollableScrollPhysics();
        }
    }

    private sealed class TestCupertinoLocalizations : DefaultCupertinoLocalizations
    {
        public override string SelectAllButtonLabel => "Custom Select All";
    }

    private sealed class TestCupertinoLocalizationsDelegate : LocalizationsDelegate<CupertinoLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override CupertinoLocalizations LoadTyped(Locale locale) => new TestCupertinoLocalizations();

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class TestRouterDelegate : RouterDelegate<RouteInformation>
    {
        private readonly Action _onBuild;

        public TestRouterDelegate(Action onBuild)
        {
            _onBuild = onBuild;
        }

        public override Task SetNewRoutePath(RouteInformation configuration) => Task.CompletedTask;

        public override Task<bool> PopRoute() => Task.FromResult(false);

        public override Widget Build(BuildContext context)
        {
            _onBuild();
            return new SizedBox();
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

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
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
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
