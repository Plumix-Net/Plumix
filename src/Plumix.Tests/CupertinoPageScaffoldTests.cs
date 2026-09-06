using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/scaffold_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoPageScaffoldTests : IDisposable
{
    public CupertinoPageScaffoldTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructor_ExposesPinnedDefaults()
    {
        var child = new SizedBox();
        var scaffold = new CupertinoPageScaffold(child);

        Assert.Same(child, scaffold.Child);
        Assert.Null(scaffold.NavigationBar);
        Assert.Null(scaffold.BackgroundColor);
        Assert.True(scaffold.ResizeToAvoidBottomInset);
        Assert.Throws<ArgumentException>(() => new CupertinoPageScaffold(
            child,
            navigationBar: new NonWidgetNavigationBar()));
    }

    [Fact]
    public void BackgroundColor_ResolvesForPaintAndInheritedConsumers()
    {
        Color light = Color.FromUInt32(0xFF010203);
        Color dark = Color.FromUInt32(0xFF040506);
        Color? inheritedColor = null;
        var dynamicColor = CupertinoDynamicColor.WithBrightness(light, dark);

        using var harness = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(
                backgroundColor: dynamicColor,
                child: new CaptureBuildContextWidget(
                    context => inheritedColor = CupertinoPageScaffoldBackgroundColor.MaybeOf(context))),
            platformBrightness: PlatformBrightness.Dark));

        var decoratedBox = Assert.Single(harness.FindWidgets<DecoratedBox>());
        var decoration = Assert.IsType<BoxDecoration>(decoratedBox.Decoration);
        Assert.Equal(dark, decoration.Color);
        Assert.Equal(dark, inheritedColor);
        Assert.Single(harness.FindWidgets<ScrollNotificationObserver>());
    }

    [Fact]
    public void BackgroundColor_DefaultsToTheResolvedThemeScaffoldColor()
    {
        using var harness = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(child: new SizedBox()),
            platformBrightness: PlatformBrightness.Dark));

        var decoratedBox = Assert.Single(harness.FindWidgets<DecoratedBox>());
        var decoration = Assert.IsType<BoxDecoration>(decoratedBox.Decoration);
        Assert.Equal(CupertinoColors.Black, decoration.Color);
    }

    [Fact]
    public void OpaqueNavigationBar_ShiftsContentAndConsumesTopPaddingAndKeyboardInset()
    {
        MediaQueryData? childMediaQuery = null;
        double? childTextScale = null;
        double? navigationBarTextScale = null;
        var navigationBar = new TestNavigationBar(
            fullyObstructs: true,
            captureTextScale: scale => navigationBarTextScale = scale);

        using var harness = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(
                navigationBar: navigationBar,
                child: new CaptureBuildContextWidget(context =>
                {
                    childMediaQuery = MediaQuery.Of(context);
                    childTextScale = MediaQuery.TextScaleFactorOf(context);
                })),
            padding: new Thickness(0.0, 20.0, 0.0, 0.0),
            viewInsets: new Thickness(0.0, 0.0, 0.0, 100.0),
            textScaleFactor: 3.0));

        var contentPadding = Assert.Single(harness.FindWidgets<Padding>());
        Assert.Equal(new Thickness(0.0, 64.0, 0.0, 100.0), contentPadding.Insets);
        Assert.NotNull(childMediaQuery);
        Assert.Equal(0.0, childMediaQuery.Padding.Top);
        Assert.Equal(0.0, childMediaQuery.ViewInsets.Bottom);
        Assert.Equal(3.0, childTextScale);
        Assert.Equal(1.0, navigationBarTextScale);
    }

    [Fact]
    public void TranslucentNavigationBar_LeavesContentBehindBarAndPublishesOverlapPadding()
    {
        MediaQueryData? childMediaQuery = null;
        var navigationBar = new TestNavigationBar(fullyObstructs: false);

        using var harness = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(
                navigationBar: navigationBar,
                child: new CaptureBuildContextWidget(context => childMediaQuery = MediaQuery.Of(context))),
            padding: new Thickness(0.0, 20.0, 0.0, 0.0),
            viewInsets: new Thickness(0.0, 0.0, 0.0, 100.0)));

        var contentPadding = Assert.Single(harness.FindWidgets<Padding>());
        Assert.Equal(new Thickness(0.0, 0.0, 0.0, 100.0), contentPadding.Insets);
        Assert.NotNull(childMediaQuery);
        Assert.Equal(64.0, childMediaQuery.Padding.Top);
        Assert.Equal(0.0, childMediaQuery.ViewInsets.Bottom);
    }

    [Fact]
    public void ResizeToAvoidBottomInset_IsAppliedWithoutANavigationBarAndCanBeDisabled()
    {
        MediaQueryData? resizedMediaQuery = null;
        using (var resized = new WidgetRenderHarness(BuildRoot(
                   new CupertinoPageScaffold(
                       child: new CaptureBuildContextWidget(context => resizedMediaQuery = MediaQuery.Of(context))),
                   viewInsets: new Thickness(0.0, 0.0, 0.0, 80.0))))
        {
            var contentPadding = Assert.Single(resized.FindWidgets<Padding>());
            Assert.Equal(80.0, contentPadding.Insets.Bottom);
            Assert.Equal(0.0, resizedMediaQuery!.ViewInsets.Bottom);
        }

        MediaQueryData? unresizedMediaQuery = null;
        using var unresized = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(
                resizeToAvoidBottomInset: false,
                child: new CaptureBuildContextWidget(context => unresizedMediaQuery = MediaQuery.Of(context))),
            viewInsets: new Thickness(0.0, 0.0, 0.0, 80.0)));

        Assert.Empty(unresized.FindWidgets<Padding>());
        Assert.Equal(80.0, unresizedMediaQuery!.ViewInsets.Bottom);
    }

    [Fact]
    public void StatusBarTap_ScrollsThePrimaryControllerToTheTop()
    {
        using var controller = new ScrollController(initialScrollOffset: 1000.0);
        using var harness = new WidgetRenderHarness(BuildRoot(
            new PrimaryScrollController(
                controller: controller,
                child: new CupertinoPageScaffold(
                    child: new SingleChildScrollView(
                        primary: true,
                        child: new SizedBox(width: double.PositiveInfinity, height: 2000.0)))),
            padding: new Thickness(0.0, 25.0, 0.0, 0.0)));

        harness.Pump(new Size(400.0, 600.0));
        Assert.Equal(1000.0, controller.Offset);

        WidgetsBinding.Instance.HandleStatusBarTap();
        AnimationPump.Prime();
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        harness.Pump(new Size(400.0, 600.0));

        Assert.Equal(0.0, controller.Offset, precision: 3);
    }

    [Fact]
    public void StatusBarTap_OnlyScrollsTheForegroundScaffold()
    {
        using var backgroundController = new ScrollController(initialScrollOffset: 1000.0);
        using var foregroundController = new ScrollController(initialScrollOffset: 1000.0);
        using var harness = new WidgetRenderHarness(BuildRoot(
            new Stack(
                children:
                [
                    BuildScrollableScaffold(backgroundController),
                    BuildScrollableScaffold(foregroundController),
                ]),
            padding: new Thickness(0.0, 25.0, 0.0, 0.0)));

        harness.Pump(new Size(400.0, 600.0));
        WidgetsBinding.Instance.HandleStatusBarTap();
        AnimationPump.Prime();
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        harness.Pump(new Size(400.0, 600.0));

        Assert.Equal(1000.0, backgroundController.Offset, precision: 3);
        Assert.Equal(0.0, foregroundController.Offset, precision: 3);
    }

    [Fact]
    public void ZeroArea_DoesNotCrashLayoutOrPaint()
    {
        using var harness = new WidgetRenderHarness(BuildRoot(
            new CupertinoPageScaffold(child: new SizedBox())));

        harness.Pump(default);
        Assert.Equal(default, harness.RenderView.Size);
    }

    private static Widget BuildRoot(
        Widget child,
        Thickness padding = default,
        Thickness viewInsets = default,
        double textScaleFactor = 1.0,
        PlatformBrightness platformBrightness = PlatformBrightness.Light)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(
                    Size: new Size(400.0, 600.0),
                    Padding: padding,
                    ViewInsets: viewInsets,
                    TextScaleFactor: textScaleFactor,
                    PlatformBrightness: platformBrightness),
                new CupertinoTheme(new CupertinoThemeData(), child)));
    }

    private static Widget BuildScrollableScaffold(ScrollController controller)
    {
        return new PrimaryScrollController(
            controller: controller,
            child: new CupertinoPageScaffold(
                child: new SingleChildScrollView(
                    primary: true,
                    child: new SizedBox(width: double.PositiveInfinity, height: 2000.0))));
    }

    private sealed class TestNavigationBar : StatelessWidget, IObstructingPreferredSizeWidget
    {
        private readonly bool _fullyObstructs;
        private readonly Action<double>? _captureTextScale;

        public TestNavigationBar(bool fullyObstructs, Action<double>? captureTextScale = null)
        {
            _fullyObstructs = fullyObstructs;
            _captureTextScale = captureTextScale;
        }

        public Size PreferredSize => new(double.PositiveInfinity, 44.0);

        public bool ShouldFullyObstruct(BuildContext context) => _fullyObstructs;

        public override Widget Build(BuildContext context)
        {
            _captureTextScale?.Invoke(MediaQuery.TextScaleFactorOf(context));
            return new SizedBox(height: 44.0);
        }
    }

    private sealed class NonWidgetNavigationBar : IObstructingPreferredSizeWidget
    {
        public Size PreferredSize => new(100.0, 44.0);

        public bool ShouldFullyObstruct(BuildContext context) => true;
    }

    private sealed class CaptureBuildContextWidget : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;

        public CaptureBuildContextWidget(Action<BuildContext> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return new SizedBox(width: double.PositiveInfinity, height: double.PositiveInfinity);
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget)
            {
                widgets.Add(widget);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

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

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
