using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTabsTests
{
    public MaterialTabsTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Tab_ValidatesContentAndMatchesPreferredHeights()
    {
        Assert.Throws<ArgumentException>(() => new Tab());
        Assert.Throws<ArgumentException>(() => new Tab(text: "A", child: new Text("B")));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tab(text: "A", height: -1));

        Assert.Equal(46, new Tab(text: "Text").PreferredSize.Height);
        Assert.Equal(46, new Tab(icon: new Icon(Icons.Menu)).PreferredSize.Height);
        Assert.Equal(72, new Tab(text: "Text", icon: new Icon(Icons.Menu)).PreferredSize.Height);
        Assert.Equal(24, new Tab(text: "Text", height: 24).PreferredSize.Height);
    }

    [Fact]
    public void TabController_DefaultsAndImmediateIndexChangeMatchFlutter()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);

        Assert.Equal(3, controller.Length);
        Assert.Equal(1, controller.Index);
        Assert.Equal(1, controller.PreviousIndex);
        Assert.Equal(1, controller.AnimationValue);
        Assert.Equal(TimeSpan.FromMilliseconds(300), controller.AnimationDuration);
        Assert.False(controller.IndexIsChanging);

        controller.Index = 2;

        Assert.Equal(2, controller.Index);
        Assert.Equal(1, controller.PreviousIndex);
        Assert.Equal(2, controller.AnimationValue);
        Assert.Equal(0, controller.Offset);
        Assert.False(controller.IndexIsChanging);
    }

    [Fact]
    public void TabController_AnimateToExposesChangingLifecycleAndInterpolatedValue()
    {
        using var controller = new TabController(length: 3);
        controller.AnimateTo(2);

        Assert.Equal(2, controller.Index);
        Assert.Equal(0, controller.PreviousIndex);
        Assert.True(controller.IndexIsChanging);
        Assert.Equal(0, controller.AnimationValue);

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.16));
        Assert.InRange(controller.AnimationValue, 0.5, 1.9);
        Assert.True(controller.IndexIsChanging);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        Assert.Equal(2, controller.AnimationValue);
        Assert.False(controller.IndexIsChanging);
    }

    [Fact]
    public void TabController_ValidatesLengthIndexAndOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabController(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabController(2, 2));
        using var empty = new TabController(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.Index = 1);

        using var controller = new TabController(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.Offset = 1.1);
        controller.Offset = 0.5;
        Assert.Equal(0.5, controller.AnimationValue);
    }

    [Fact]
    public void DefaultTabController_ProvidesConfiguredControllerToDescendants()
    {
        TabController? captured = null;
        using var harness = new WidgetRenderHarness(Wrap(new DefaultTabController(
            length: 3,
            initialIndex: 2,
            animationDuration: TimeSpan.FromMilliseconds(450),
            child: new ControllerProbe(controller => captured = controller))));
        harness.Pump(new Size(100, 40));

        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Length);
        Assert.Equal(2, captured.Index);
        Assert.Equal(TimeSpan.FromMilliseconds(450), captured.AnimationDuration);
    }

    [Fact]
    public void TabBar_DefaultSurfaceMatchesFlutter()
    {
        var tabs = new[] { new Tab(text: "One"), new Tab(text: "Two") };
        var bar = new TabBar(tabs);

        Assert.Same(tabs, bar.Tabs);
        Assert.False(bar.IsScrollable);
        Assert.True(bar.AutomaticIndicatorColorAdjustment);
        Assert.Equal(2, bar.IndicatorWeight);
        Assert.Equal(new Thickness(), bar.IndicatorPadding);
        Assert.Equal(DragStartBehavior.Start, bar.DragStartBehavior);
        Assert.Null(bar.Controller);
        Assert.Null(bar.IndicatorSize);
        Assert.Equal(48, bar.PreferredSize.Height);

        var appBar = new AppBar(bottom: bar);
        Assert.Equal(104, appBar.PreferredSize.Height);
    }

    [Fact]
    public void TabBar_Material3FillLayoutAndLabelIndicatorMatchDefaults()
    {
        using var controller = new TabController(3);
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs:
            [
                new Tab(text: "One"),
                new Tab(text: "Two"),
                new Tab(text: "Three"),
            ])));

        harness.Pump(new Size(300, 100));
        var render = RequireRenderTabBar(harness.RenderView);

        Assert.Equal(3, render.TabRects.Count);
        Assert.All(render.TabRects, rect => Assert.Equal(100, rect.Width, precision: 3));
        Assert.Equal(ThemeData.Light.PrimaryColor, render.IndicatorColor);
        Assert.Equal(ThemeData.Light.OutlineVariantColor, render.DividerColor);
        Assert.Equal(1, render.DividerHeight);
        Assert.NotNull(render.IndicatorRect);
        Assert.InRange(render.IndicatorRect!.Value.Width, 20, 40);
        Assert.Equal(3, render.IndicatorRect.Value.Height, precision: 3);
    }

    [Fact]
    public void TabBar_ThemeOverridesIndicatorDividerAndTabAlignmentValidation()
    {
        using var controller = new TabController(2);
        var theme = ThemeData.Light with
        {
            TabBarTheme = new TabBarThemeData(
                IndicatorColor: Colors.Crimson,
                IndicatorSize: TabBarIndicatorSize.Tab,
                DividerColor: Colors.DarkCyan,
                DividerHeight: 4,
                TabAlignment: TabAlignment.Center),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            new TabBar(controller: controller, tabs: [new Tab(text: "One"), new Tab(text: "Two")]),
            theme));
        harness.Pump(new Size(300, 100));

        var render = RequireRenderTabBar(harness.RenderView);
        Assert.Equal(Colors.Crimson, render.IndicatorColor);
        Assert.Equal(Colors.DarkCyan, render.DividerColor);
        Assert.Equal(4, render.DividerHeight);
        Assert.Equal(render.TabRects[0].Width, render.IndicatorRect!.Value.Width, precision: 3);

        Assert.Throws<ArgumentException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            isScrollable: true,
            tabAlignment: TabAlignment.Fill,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")]))));
    }

    [Fact]
    public void TabBar_TapUsesGestureRouteAndAnimatesIndicator()
    {
        using var controller = new TabController(2);
        int tapped = -1;
        using var harness = new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            onTap: index => tapped = index,
            tabs: [new Tab(text: "One"), new Tab(text: "Two")])));
        harness.Pump(new Size(300, 100));

        var render = RequireRenderTabBar(harness.RenderView);
        var target = render.TabRects[1].Center;
        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            30, PointerDeviceKind.Mouse, target, PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            30, PointerDeviceKind.Mouse, target, PointerButtons.None, now.AddMilliseconds(20)));

        Assert.Equal(1, controller.Index);
        Assert.Equal(1, tapped);
        Assert.True(controller.IndexIsChanging);

        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 100));
        Assert.False(controller.IndexIsChanging);
        Assert.Equal(render.TabRects[1].Center.X, RequireRenderTabBar(harness.RenderView).IndicatorRect!.Value.Center.X, precision: 3);
    }

    [Fact]
    public void TabBarView_InitialPageAndViewportFractionUsePageGeometry()
    {
        using var controller = new TabController(3, initialIndex: 1);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            viewportFraction: 0.8,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
            ])));
        harness.Pump(new Size(300, 180));

        var viewport = RequirePageViewport(harness.RenderView);
        Assert.Equal(new Size(240, 180), viewport.FirstChild!.Size);
        Assert.Equal(-210, ((PageViewportParentData)viewport.FirstChild.parentData!).offset.X, precision: 3);
        var selected = viewport.ChildAfter(viewport.FirstChild)!;
        Assert.Equal(30, ((PageViewportParentData)selected.parentData!).offset.X, precision: 3);
    }

    [Fact]
    public void TabBarView_ControllerAnimationAndSwipeStaySynchronized()
    {
        using var controller = new TabController(3);
        using var harness = new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children:
            [
                new ColoredBox(Colors.Red),
                new ColoredBox(Colors.Green),
                new ColoredBox(Colors.Blue),
            ])));
        harness.Pump(new Size(300, 180));

        controller.AnimateTo(2);
        double clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));
        Assert.Equal(2, controller.Index);
        var programmaticViewport = RequirePageViewport(harness.RenderView);
        Assert.Equal(2, programmaticViewport.Controller.EffectivePage, precision: 3);
        Assert.Equal(0, ((PageViewportParentData)programmaticViewport.LastChild!.parentData!).offset.X, precision: 3);

        controller.AnimateTo(0);
        clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));
        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            44, PointerDeviceKind.Touch, new Point(260, 90), PointerButtons.Primary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            44, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.Primary, true, now.AddMilliseconds(40)));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            44, PointerDeviceKind.Touch, new Point(20, 90), PointerButtons.None, now.AddMilliseconds(50)));
        clock = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.35));
        harness.Pump(new Size(300, 180));

        Assert.Equal(1, controller.Index);
        var second = RequirePageViewport(harness.RenderView).ChildAfter(RequirePageViewport(harness.RenderView).FirstChild!)!;
        Assert.Equal(0, ((PageViewportParentData)second.parentData!).offset.X, precision: 3);
    }

    [Fact]
    public void TabBarAndView_RejectControllerCountMismatch()
    {
        using var controller = new TabController(2);
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabBar(
            controller: controller,
            tabs: [new Tab(text: "Only")]))));

        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabBarView(
            controller: controller,
            children: [new SizedBox()]))));
    }

    [Fact]
    public void TabPageSelectorIndicator_MatchesCircleGeometryAndBorderStyle()
    {
        var indicator = new TabPageSelectorIndicator(
            backgroundColor: Colors.Red,
            borderColor: Colors.Blue,
            size: 16);
        Assert.Equal(Colors.Red, indicator.BackgroundColor);
        Assert.Equal(Colors.Blue, indicator.BorderColor);
        Assert.Equal(16, indicator.Size);
        Assert.Equal(BorderStyle.Solid, indicator.BorderStyle);

        using var harness = new WidgetRenderHarness(Wrap(indicator));
        harness.Pump(new Size(100, 100));
        var decoration = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Shape == BoxShape.Circle);
        Assert.Equal(Colors.Red, decoration.Decoration.Color);
        Assert.Equal(new BorderSide(Colors.Blue), decoration.Decoration.Border);
        Assert.Equal(new Size(16, 16), decoration.Size);
        Assert.Equal(new Size(24, 24), harness.RenderView.Child!.Size);

        using var borderless = new WidgetRenderHarness(Wrap(new TabPageSelectorIndicator(
            backgroundColor: Colors.Red,
            borderColor: Colors.Blue,
            size: 12,
            borderStyle: BorderStyle.None)));
        borderless.Pump(new Size(100, 100));
        Assert.Equal(
            new BorderSide(Colors.Blue, style: BorderStyle.None),
            Assert.Single(FindDescendants<RenderDecoratedBox>(borderless.RenderView),
                box => box.Decoration.Shape == BoxShape.Circle).Decoration.Border);
    }

    [Fact]
    public void TabPageSelector_DefaultsUseControllerLengthAndThemeSecondaryColor()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);
        var selector = new TabPageSelector(controller: controller);
        Assert.Equal(12, selector.IndicatorSize);
        Assert.Null(selector.Color);
        Assert.Null(selector.SelectedColor);
        Assert.Null(selector.BorderStyle);

        using var harness = new WidgetRenderHarness(Wrap(selector));
        harness.Pump(new Size(200, 60));
        var circles = FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Where(box => box.Decoration.Shape == BoxShape.Circle)
            .ToArray();
        Assert.Equal(3, circles.Length);
        Assert.Equal([Colors.Transparent, ThemeData.Light.SecondaryColor, Colors.Transparent],
            circles.Select(circle => circle.Decoration.Color!.Value).ToArray());
        Assert.All(circles, circle => Assert.Equal(
            new BorderSide(ThemeData.Light.SecondaryColor),
            circle.Decoration.Border));
        Assert.Equal(new Size(60, 20), harness.RenderView.Child!.Size);
    }

    [Fact]
    public void TabPageSelector_RespondsToImmediateIndexAndDragOffsetChanges()
    {
        using var controller = new TabController(length: 3, initialIndex: 1);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(
            controller: controller,
            color: Colors.Transparent,
            selectedColor: Colors.Red)));
        harness.Pump(new Size(200, 60));

        controller.Index = 2;
        harness.Pump(new Size(200, 60));
        Assert.Equal([0, 0, 255], SelectorAlphas(harness.RenderView));

        controller.Index = 1;
        controller.Offset = 0.4;
        harness.Pump(new Size(200, 60));
        int[] alphas = SelectorAlphas(harness.RenderView);
        Assert.Equal(0, alphas[0]);
        Assert.InRange(alphas[1], 150, 155);
        Assert.InRange(alphas[2], 100, 105);
    }

    [Fact]
    public void TabPageSelector_InterpolatesOutgoingAndIncomingColorsDuringAnimateTo()
    {
        using var controller = new TabController(length: 3);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(
            controller: controller,
            color: Colors.Transparent,
            selectedColor: Colors.Red)));
        harness.Pump(new Size(200, 60));

        controller.AnimateTo(1, duration: TimeSpan.FromMilliseconds(200));
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.04));
        harness.Pump(new Size(200, 60));
        int[] early = SelectorAlphas(harness.RenderView);
        Assert.True(early[0] > early[1]);
        Assert.Equal(0, early[2]);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.18));
        harness.Pump(new Size(200, 60));
        int[] late = SelectorAlphas(harness.RenderView);
        Assert.True(late[0] < late[1]);
        Assert.Equal(0, late[2]);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(200, 60));
        Assert.Equal([0, 255, 0], SelectorAlphas(harness.RenderView));
    }

    [Fact]
    public void TabPageSelector_UsesDefaultControllerAndLocalizedSemantics()
    {
        using var harness = new WidgetRenderHarness(Wrap(new DefaultTabController(
            length: 3,
            initialIndex: 1,
            child: new TabPageSelector())));
        harness.Pump(new Size(200, 60));

        Assert.Contains(FindDescendants<RenderSemanticsAnnotations>(harness.RenderView),
            semantics => semantics.Label == "Tab 2 of 3");
        Assert.Equal([0, 255, 0], SelectorAlphas(harness.RenderView));
    }

    [Fact]
    public void TabPageSelector_ValidatesControllerAndHandlesZeroArea()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabPageSelector(indicatorSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TabPageSelector(indicatorSize: double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(() => new WidgetRenderHarness(Wrap(new TabPageSelector())));

        using var controller = new TabController(2);
        using var harness = new WidgetRenderHarness(Wrap(new TabPageSelector(controller: controller)));
        harness.Pump(new Size());
        controller.AnimateTo(1);
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size());
        Assert.Equal(new Size(), harness.RenderView.Child!.Size);
    }

    [Fact]
    public void TabsDemoPage_RendersNestedAppBarBottomAndPageViewAtDesktopSize()
    {
        using var harness = new WidgetRenderHarness(Wrap(new TabsDemoPage()));
        harness.Pump(new Size(1000, 700));

        Assert.NotNull(FindDescendant<RenderTabBar>(harness.RenderView));
        Assert.NotNull(FindDescendant<RenderPageViewport>(harness.RenderView));
        Assert.Equal(4, FindDescendants<RenderDecoratedBox>(harness.RenderView)
            .Count(box => box.Decoration.Shape == BoxShape.Circle));
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new Theme(theme ?? ThemeData.Light, child));

    private static RenderTabBar RequireRenderTabBar(RenderObject root) =>
        Assert.IsType<RenderTabBar>(FindDescendant<RenderTabBar>(root));

    private static RenderPageViewport RequirePageViewport(RenderObject root) =>
        Assert.IsType<RenderPageViewport>(FindDescendant<RenderPageViewport>(root));

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null) return null;
        if (root is T match) return match;
        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T match) result.Add(match);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static int[] SelectorAlphas(RenderObject root)
    {
        return FindDescendants<RenderDecoratedBox>(root)
            .Where(box => box.Decoration.Shape == BoxShape.Circle)
            .Select(box => (int)box.Decoration.Color!.Value.A)
            .ToArray();
    }

    private sealed class ControllerProbe(Action<TabController> capture) : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            capture(DefaultTabController.Of(context));
            return new SizedBox();
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
        public override RenderObject? RenderObject => _child?.RenderObject;
        internal override Element? RenderObjectAttachingChild => _child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
        internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
        internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
        internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
        }
    }
}
