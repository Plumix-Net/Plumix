using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialSurface = Plumix.Material.Material;

namespace Plumix.Tests;

public sealed class MaterialCarouselTests
{
    [Fact]
    public void CarouselView_ItemSurfaceUsesMaterial3Defaults()
    {
        using CarouselHarness harness = new(new CarouselView(200, Items(5)));
        harness.Pump();

        MaterialSurface surface = harness.FindWidgets<MaterialSurface>()[0];
        Assert.Equal(Clip.AntiAlias, surface.ClipBehavior);
        Assert.Equal(ThemeData.Light.ColorScheme.Surface, surface.Color);
        Assert.Equal(0.0, surface.Elevation);
        RoundedRectangleBorder shape = Assert.IsType<RoundedRectangleBorder>(surface.Shape);
        Assert.Equal(28.0, shape.BorderRadius.Physical.Radius);
        Assert.Null(surface.BorderRadius);
        Assert.Equal(new Thickness(4), harness.FindWidgets<Padding>()[0].Insets);
    }

    [Fact]
    public void CarouselView_ItemCustomizationOverridesDefaults()
    {
        using CarouselHarness harness = new(new CarouselView(
            200,
            Items(5),
            padding: new Thickness(20),
            backgroundColor: Color.Parse("#FFFFC107"),
            elevation: 10.0,
            shape: new StadiumBorder(),
            itemClipBehavior: Clip.HardEdge,
            overlayColor: MaterialStateProperty<Color?>.All(Colors.Purple)));
        harness.Pump();

        MaterialSurface surface = harness.FindWidgets<MaterialSurface>()[0];
        Assert.Equal(Clip.HardEdge, surface.ClipBehavior);
        Assert.Equal(Color.Parse("#FFFFC107"), surface.Color);
        Assert.Equal(10.0, surface.Elevation);
        Assert.IsType<StadiumBorder>(surface.Shape);
        Assert.Equal(new Thickness(20), harness.FindWidgets<Padding>()[0].Insets);

        InkWell ink = harness.FindWidgets<InkWell>()[0];
        Assert.Equal(Colors.Purple, ink.OverlayColor!.Resolve(MaterialState.Focused));
    }

    [Fact]
    public void CarouselView_DefaultOverlayColorFollowsOnSurfaceStates()
    {
        using CarouselHarness harness = new(new CarouselView(200, Items(2)));
        harness.Pump();

        Color onSurface = ThemeData.Light.ColorScheme.OnSurface;
        InkWell ink = harness.FindWidgets<InkWell>()[0];
        Assert.Equal((byte)Math.Round(onSurface.A * 0.1), ink.OverlayColor!.Resolve(MaterialState.Pressed)!.Value.A);
        Assert.Equal((byte)Math.Round(onSurface.A * 0.08), ink.OverlayColor.Resolve(MaterialState.Hovered)!.Value.A);
        Assert.Equal((byte)Math.Round(onSurface.A * 0.1), ink.OverlayColor.Resolve(MaterialState.Focused)!.Value.A);
        Assert.Null(ink.OverlayColor.Resolve(MaterialState.None));
    }

    [Fact]
    public void CarouselViewTheme_ResolvesWidgetThenLocalThenThemeData()
    {
        Color themeColor = Color.Parse("#FFE0F2F1");
        Color localColor = Color.Parse("#FFFFF3E0");
        Color widgetColor = Color.Parse("#FFE8F5E9");
        ThemeData theme = ThemeData.Light with
        {
            CarouselViewTheme = new CarouselViewThemeData(
                BackgroundColor: themeColor,
                Elevation: 3.0,
                Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(12)),
                Padding: new Thickness(6),
                ItemClipBehavior: Clip.HardEdge),
        };

        using CarouselHarness themeHarness = new(new CarouselView(200, Items(2)), theme);
        themeHarness.Pump();
        MaterialSurface fromTheme = themeHarness.FindWidgets<MaterialSurface>()[0];
        Assert.Equal(themeColor, fromTheme.Color);
        Assert.Equal(3.0, fromTheme.Elevation);
        Assert.Equal(Clip.HardEdge, fromTheme.ClipBehavior);
        Assert.Equal(12.0, Assert.IsType<RoundedRectangleBorder>(fromTheme.Shape).BorderRadius.Physical.Radius);
        Assert.Equal(new Thickness(6), themeHarness.FindWidgets<Padding>()[0].Insets);

        using CarouselHarness localHarness = new(
            new CarouselViewTheme(
                new CarouselViewThemeData(
                    BackgroundColor: localColor,
                    Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(18))),
                new CarouselView(200, Items(2))),
            theme);
        localHarness.Pump();
        MaterialSurface fromLocal = localHarness.FindWidgets<MaterialSurface>()[0];
        Assert.Equal(localColor, fromLocal.Color);
        Assert.Equal(18.0, Assert.IsType<RoundedRectangleBorder>(fromLocal.Shape).BorderRadius.Physical.Radius);

        using CarouselHarness widgetHarness = new(
            new CarouselView(
                200,
                Items(2),
                backgroundColor: widgetColor,
                shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(6))),
            theme);
        widgetHarness.Pump();
        MaterialSurface fromWidget = widgetHarness.FindWidgets<MaterialSurface>()[0];
        Assert.Equal(widgetColor, fromWidget.Color);
        Assert.Equal(6.0, Assert.IsType<RoundedRectangleBorder>(fromWidget.Shape).BorderRadius.Physical.Radius);
    }

    [Fact]
    public void CarouselViewThemeData_CopyWithLerpAndEqualityFollowDart()
    {
        CarouselViewThemeData empty = new();
        Assert.Equal(empty, empty.CopyWith());
        Assert.Equal(empty.GetHashCode(), empty.CopyWith().GetHashCode());
        Assert.Null(empty.Elevation);
        Assert.Null(empty.BackgroundColor);
        Assert.Null(empty.OverlayColor);
        Assert.Null(empty.Shape);
        Assert.Null(empty.Padding);
        Assert.Null(empty.ItemClipBehavior);

        CarouselViewThemeData a = new(
            Elevation: 2,
            BackgroundColor: Colors.Black,
            Padding: new Thickness(0),
            ItemClipBehavior: Clip.HardEdge);
        CarouselViewThemeData b = new(
            Elevation: 6,
            BackgroundColor: Colors.White,
            Padding: new Thickness(8),
            ItemClipBehavior: Clip.AntiAlias);

        CarouselViewThemeData mid = CarouselViewThemeData.Lerp(a, b, 0.5);
        Assert.Equal(4.0, mid.Elevation);
        Assert.Equal(new Thickness(4), mid.Padding);
        Assert.Equal(Clip.AntiAlias, mid.ItemClipBehavior);
        Assert.Equal(Clip.HardEdge, CarouselViewThemeData.Lerp(a, b, 0.4).ItemClipBehavior);
        Assert.Same(a, CarouselViewThemeData.Lerp(a, a, 0.3));
    }

    [Fact]
    public void CarouselView_UncontainedLayoutShrinksTheTrailingItem()
    {
        using CarouselHarness harness = new(new CarouselView(250, Items(5)));
        harness.Pump();

        Assert.Equal(
            [(0, 0.0, 250.0), (1, 250.0, 250.0), (2, 500.0, 250.0), (3, 750.0, 50.0)],
            harness.ItemGeometry());
    }

    [Fact]
    public void CarouselView_HonorsInitialItem()
    {
        CarouselController controller = new(initialItem: 5);
        using CarouselHarness harness = new(new CarouselView(400, Items(10), controller: controller));
        harness.Pump();

        Assert.Equal([(5, 0.0, 400.0), (6, 400.0, 400.0)], harness.ItemGeometry());
        Assert.Equal(5, controller.LeadingItem);
    }

    [Fact]
    public void CarouselView_KeepsTheLeadingItemWhenItemExtentChanges()
    {
        CarouselController controller = new(initialItem: 3);
        using CarouselHarness harness = new(new CarouselView(234, Items(10), controller: controller));
        harness.Pump();
        Assert.Equal(234.0, harness.ItemGeometry()[0].Extent);

        harness.Rebuild(new CarouselView(400, Items(10), controller: controller));
        Assert.Equal(400.0, harness.ItemGeometry()[0].Extent);
        Assert.Equal(3, controller.LeadingItem);

        harness.Rebuild(new CarouselView(100, Items(10), controller: controller));
        Assert.Equal(100.0, harness.ItemGeometry()[0].Extent);
        Assert.Equal(3, controller.LeadingItem);
    }

    [Fact]
    public void CarouselView_RespectsShrinkExtent()
    {
        using CarouselHarness harness = new(new CarouselView(350, Items(5), shrinkExtent: 300));
        harness.Pump();
        Assert.Equal(
            [(0, 0.0, 350.0), (1, 350.0, 350.0), (2, 700.0, 300.0)],
            harness.ItemGeometry());

        harness.ScrollTo(50);
        Assert.Equal((0, 0.0, 300.0), harness.ItemGeometry()[0]);

        harness.ScrollTo(100);
        Assert.Equal((0, -50.0, 300.0), harness.ItemGeometry()[0]);
    }

    [Fact]
    public void CarouselView_ItemExtentZeroOrInfiniteDoesNotCrash()
    {
        using CarouselHarness zero = new(new CarouselView(0, Items(3)));
        zero.Pump();

        using CarouselHarness infinite = new(new CarouselView(double.PositiveInfinity, Items(3)));
        infinite.Pump();
        Assert.Equal(800.0, infinite.ItemGeometry()[0].Extent);
    }

    [Fact]
    public void CarouselView_ZeroSizedViewportDoesNotCrash()
    {
        using CarouselHarness harness = new(new CarouselView(200, Items(3)));
        harness.Pump(new Size(0, 0));
        harness.Pump(new Size(500, 400));

        Assert.Equal(200.0, harness.ItemGeometry()[0].Extent);
    }

    [Fact]
    public void CarouselViewWeighted_DistributesExtentsByWeight()
    {
        using CarouselHarness harness = new(CarouselView.Weighted([4, 3, 2, 1], Items(5), consumeMaxWeight: false));
        harness.Pump();

        Assert.Equal(
            [(0, 0.0, 320.0), (1, 320.0, 240.0), (2, 560.0, 160.0), (3, 720.0, 80.0)],
            harness.ItemGeometry());
    }

    [Fact]
    public void CarouselViewWeighted_ConsumeMaxWeightReservesTheLeadingSlots()
    {
        using CarouselHarness harness = new(CarouselView.Weighted([1, 2, 4, 2, 1], Items(10)));
        harness.Pump();

        (int Index, double Offset, double Extent) leading =
            harness.ItemGeometry().Single(item => item.Index == 0);
        Assert.Equal(240.0, leading.Offset, precision: 3);
        Assert.Equal(320.0, leading.Extent, precision: 3);
    }

    [Fact]
    public void CarouselViewWeighted_InterpolatesExtentsWhileScrolling()
    {
        using CarouselHarness harness = new(
            CarouselView.Weighted([1, 2, 4, 2, 1], Items(10), consumeMaxWeight: false));
        harness.Pump();
        Assert.Equal([80.0, 160.0, 320.0, 160.0, 80.0], harness.ItemGeometry().Take(5).Select(i => i.Extent));

        harness.ScrollTo(40);
        Assert.Equal(
            [40.0, 120.0, 240.0, 240.0, 120.0, 40.0],
            harness.ItemGeometry().Select(item => Math.Round(item.Extent, 3)));
    }

    [Fact]
    public void CarouselViewWeighted_HonorsInitialItemInTheMaxWeightSlot()
    {
        CarouselController controller = new(initialItem: 5);
        using CarouselHarness harness = new(CarouselView.Weighted([1, 8, 1], Items(10), controller: controller));
        harness.Pump();

        List<(int Index, double Offset, double Extent)> geometry = harness.ItemGeometry();
        Assert.Equal((4, 0.0, 80.0), geometry[0]);
        Assert.Equal((5, 80.0, 640.0), geometry[1]);
        Assert.Equal((6, 720.0, 80.0), geometry[2]);
        Assert.DoesNotContain(geometry, item => item.Index == 7);

        // Item 5 fills the max-weight slot, so the item that leads the viewport is 4.
        Assert.Equal(4, controller.LeadingItem);
    }

    [Fact]
    public void CarouselViewWeighted_ShrinkExtentClampsToTheSmallestWeight()
    {
        using CarouselHarness harness = new(
            CarouselView.Weighted([1, 6, 1], Items(5), consumeMaxWeight: false, shrinkExtent: 1000));
        harness.Pump();

        Assert.Equal(100.0, harness.ItemGeometry()[0].Extent, precision: 3);
    }

    [Fact]
    public void CarouselView_VerticalScrollDirectionLaysOutDownwards()
    {
        using CarouselHarness harness = new(new CarouselView(
            200,
            Items(5),
            padding: new Thickness(0),
            scrollDirection: Axis.Vertical));
        harness.Pump();

        Assert.Equal(
            [(0, 0.0, 200.0), (1, 200.0, 200.0), (2, 400.0, 200.0)],
            harness.ItemGeometry(Axis.Vertical));
    }

    [Fact]
    public void CarouselScrollPhysics_SnapsToTheNearestItemBoundary()
    {
        CarouselScrollPosition position = new(
            context: new TestScrollContext(),
            initialItem: 0,
            itemExtent: 300,
            physics: new CarouselScrollPhysics());
        position.ApplyViewportDimension(800);
        position.ApplyContentDimensions(0, 1200);
        position.JumpTo(100);

        Assert.True(position.Physics.AllowImplicitScrolling);
        Simulation? settle = position.Physics.CreateBallisticSimulation(position, 0);
        Assert.NotNull(settle);
        Assert.Equal(0.0, settle!.X(10), precision: 0);

        position.JumpTo(160);
        Simulation? forward = position.Physics.CreateBallisticSimulation(position, 0);
        Assert.Equal(300.0, forward!.X(10), precision: 0);

        position.JumpTo(100);
        Simulation? fling = position.Physics.CreateBallisticSimulation(position, 800);
        Assert.Equal(300.0, fling!.X(10), precision: 0);
    }

    [Fact]
    public void CarouselScrollPhysics_RejectsForeignPositions()
    {
        CarouselScrollPhysics physics = new();
        FixedScrollMetrics metrics = new(0, 1200, 100, 800, AxisDirection.Right, 1.0);

        Assert.Throws<InvalidOperationException>(() => physics.CreateBallisticSimulation(metrics, 0));
    }

    [Fact]
    public void CarouselScrollPosition_KeepsTheItemAcrossViewportChanges()
    {
        CarouselScrollPosition position = new(
            context: new TestScrollContext(),
            initialItem: 2,
            itemExtent: 100,
            physics: new CarouselScrollPhysics());
        position.ApplyViewportDimension(300);
        position.ApplyContentDimensions(0, 500);
        Assert.Equal(200, position.Pixels, precision: 3);

        position.ApplyViewportDimension(0);
        position.ApplyViewportDimension(300);
        Assert.Equal(200, position.Pixels, precision: 3);
        Assert.Equal(2, position.LeadingItem);
    }

    [Fact]
    public void CarouselScrollPosition_WeightedLeadingItemAccountsForConsumeMaxWeight()
    {
        CarouselScrollPosition position = new(
            context: new TestScrollContext(),
            initialItem: 0,
            flexWeights: [1, 2, 4, 2, 1],
            consumeMaxWeight: true,
            physics: new CarouselScrollPhysics());
        position.ApplyViewportDimension(800);
        position.ApplyContentDimensions(0, 8000);

        position.JumpTo(80 * 2);
        Assert.Equal(0, position.LeadingItem);

        position.JumpTo(80 * 5);
        Assert.Equal(3, position.LeadingItem);
    }

    [Fact]
    public void CarouselScrollPosition_CopyWithCarriesTheCarouselMetrics()
    {
        CarouselScrollPosition position = new(
            context: new TestScrollContext(),
            initialItem: 0,
            flexWeights: [1, 7],
            consumeMaxWeight: false,
            physics: new CarouselScrollPhysics());
        position.ApplyViewportDimension(800);
        position.ApplyContentDimensions(0, 8000);

        CarouselMetrics metrics = position.CopyWith(pixels: 120);
        Assert.Equal(120, metrics.Pixels);
        Assert.Equal([1, 7], metrics.FlexWeights);
        Assert.False(metrics.ConsumeMaxWeight);
        Assert.Null(metrics.ItemExtent);
    }

    [Fact]
    public void CarouselController_ReportsLeadingItemAndIndexChanges()
    {
        CarouselController controller = new(initialItem: 2);
        List<int> reported = [];
        using CarouselHarness harness = new(new CarouselView(
            200,
            Items(6),
            controller: controller,
            onIndexChanged: reported.Add));
        harness.Pump();

        Assert.Equal(400, controller.PrimaryPosition!.Pixels, precision: 3);
        Assert.Equal(2, controller.LeadingItem);

        controller.JumpToItem(3);
        harness.Pump();
        Assert.Equal(3, controller.LeadingItem);
        Assert.Contains(3, reported);

        controller.AnimateToItem(1, TimeSpan.Zero);
        harness.Pump();
        Assert.Equal(1, controller.LeadingItem);
        Assert.Contains(1, reported);
    }

    [Fact]
    public void CarouselController_ClampsTheRequestedItem()
    {
        CarouselController controller = new();
        using CarouselHarness harness = new(new CarouselView(200, Items(4), controller: controller));
        harness.Pump();

        controller.JumpToItem(99);
        harness.Pump();
        Assert.Equal(3, controller.LeadingItem);

        controller.JumpToItem(-5);
        harness.Pump();
        Assert.Equal(0, controller.LeadingItem);
    }

    [Fact]
    public void CarouselController_LeadingItemRequiresAnAttachedCarousel()
    {
        CarouselController controller = new();
        Assert.Throws<InvalidOperationException>(() => controller.LeadingItem);
    }

    [Fact]
    public void CarouselControllerWeighted_AnimatesToTheMaxWeightSlot()
    {
        CarouselController controller = new();
        List<int> reported = [];
        using CarouselHarness harness = new(CarouselView.Weighted(
            [2, 5, 2],
            Items(5),
            controller: controller,
            itemSnapping: true,
            onIndexChanged: reported.Add));
        harness.Pump();

        controller.AnimateToItem(4, TimeSpan.Zero);
        harness.Pump();
        Assert.Equal(3, controller.LeadingItem);
        Assert.Contains(3, reported);
    }

    [Fact]
    public void CarouselView_LazyBuilderOnlyBuildsVisibleItems()
    {
        List<int> built = [];
        using CarouselHarness harness = new(CarouselView.Builder(
            300,
            (_, index) =>
            {
                built.Add(index);
                return new SizedBox();
            },
            itemCount: 1000));
        harness.Pump();

        Assert.True(built.Count < 10, $"expected a lazy build, got {built.Count} items");
        Assert.Contains(0, built);
        Assert.Contains(1, built);
    }

    [Fact]
    public void CarouselView_InfiniteRepeatsTheChildren()
    {
        CarouselController controller = new();
        using CarouselHarness harness = new(new CarouselView(
            200,
            Items(3),
            controller: controller,
            infinite: true));
        harness.Pump();

        List<(int Index, double Offset, double Extent)> geometry = harness.ItemGeometry();
        Assert.True(geometry.Count >= 4, "an infinite carousel keeps building past the last child");
        Assert.Equal(200.0, geometry[0].Extent);
        Assert.Equal(0, controller.LeadingItem);

        controller.JumpToItem(5);
        harness.Pump();
        Assert.Equal(2, controller.LeadingItem);
    }

    [Fact]
    public void CarouselView_WeightedRequiresPositiveWeights()
    {
        using CarouselHarness empty = new(CarouselView.Weighted([], Items(2)));
        Assert.Throws<InvalidOperationException>(empty.Pump);

        using CarouselHarness negative = new(CarouselView.Weighted([1, 0], Items(2)));
        Assert.Throws<InvalidOperationException>(negative.Pump);
    }

    [Fact]
    public void CarouselView_TapReportsTheItemIndex()
    {
        List<int> tapped = [];
        using CarouselHarness harness = new(new CarouselView(200, Items(4), onTap: tapped.Add));
        harness.Pump();

        IReadOnlyList<InkWell> inks = harness.FindWidgets<InkWell>();
        inks[1].OnTap!();
        inks[2].OnTap!();

        Assert.Equal([1, 2], tapped);
    }

    [Fact]
    public void CarouselView_WithoutSplashWrapsTheItemInAGestureDetector()
    {
        List<int> tapped = [];
        using CarouselHarness harness = new(new CarouselView(
            200,
            Items(4),
            enableSplash: false,
            onTap: tapped.Add));
        harness.Pump();

        Assert.Empty(harness.FindWidgets<InkWell>());
        harness.FindWidgets<GestureDetector>()[1].OnTap!();
        Assert.Equal([1], tapped);
    }

    [Fact]
    public void CarouselView_WithoutSplashAndWithoutOnTapAddsNoWrapper()
    {
        using CarouselHarness harness = new(new CarouselView(200, Items(4), enableSplash: false));
        harness.Pump();

        Assert.Empty(harness.FindWidgets<InkWell>());
        Assert.Empty(harness.FindWidgets<GestureDetector>());
    }

    private static IReadOnlyList<Widget> Items(int count)
    {
        List<Widget> items = [];
        for (int index = 0; index < count; index += 1)
        {
            items.Add(new SizedBox());
        }

        return items;
    }

    private sealed class CarouselHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;
        private readonly ThemeData _theme;
        private Size _surface = new(800, 600);

        public CarouselHarness(Widget carousel, ThemeData? theme = null)
        {
            _theme = theme ?? ThemeData.Light;
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, Wrap(carousel, _theme));
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump() => Pump(_surface);

        public void Pump(Size size)
        {
            _surface = size;
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        /// <summary>Replaces the carousel with a new configuration and lays out again.</summary>
        public void Rebuild(Widget carousel)
        {
            _root.Replace(Wrap(carousel, _theme));
            Pump();
            Pump();
        }

        /// <summary>Scrolls the attached carousel position and lays out again.</summary>
        public void ScrollTo(double pixels)
        {
            CarouselScrollPosition position = Assert.IsType<CarouselScrollPosition>(
                FindDescendants<RenderSliverMultiBoxAdaptor>(RenderView)
                    .Select(_ => AttachedPosition())
                    .First());
            position.JumpTo(pixels);
            Pump();
        }

        public List<(int Index, double Offset, double Extent)> ItemGeometry(Axis axis = Axis.Horizontal)
        {
            List<(int Index, double Offset, double Extent)> geometry = [];
            foreach (RenderObject child in Children(FindSliver()))
            {
                RenderBox box = (RenderBox)child;
                var data = (SliverMultiBoxAdaptorParentData)box.parentData!;
                geometry.Add((
                    data.Index!.Value,
                    axis == Axis.Horizontal ? data.offset.X : data.offset.Y,
                    axis == Axis.Horizontal ? box.Size.Width : box.Size.Height));
            }

            geometry.Sort((left, right) => left.Index.CompareTo(right.Index));
            return geometry;
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            List<T> widgets = [];
            Visit(_root);
            return widgets;

            void Visit(Element element)
            {
                if (element.Widget is T widget)
                {
                    widgets.Add(widget);
                }

                element.VisitChildren(Visit);
            }
        }

        public void Dispose() => _root.Unmount();

        private static Widget Wrap(Widget child, ThemeData theme) => new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(new Size(800, 600)),
                new Theme(theme, child)));

        private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
        {
            List<T> result = [];
            if (root is null)
            {
                return result;
            }

            if (root is T value)
            {
                result.Add(value);
            }

            root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
            return result;
        }

        private static List<RenderObject> Children(RenderObject parent)
        {
            List<RenderObject> children = [];
            parent.VisitChildren(children.Add);
            return children;
        }

        private CarouselScrollPosition AttachedPosition()
        {
            CarouselViewState state = FindState();
            return Assert.IsType<CarouselScrollPosition>(state.Controller.PrimaryPosition);
        }

        private CarouselViewState FindState()
        {
            CarouselViewState? found = null;
            Visit(_root);
            return found ?? throw new InvalidOperationException("No CarouselView is mounted.");

            void Visit(Element element)
            {
                if (found is not null)
                {
                    return;
                }

                if (element is StatefulElement { State: CarouselViewState state })
                {
                    found = state;
                    return;
                }

                element.VisitChildren(Visit);
            }
        }

        private RenderSliverMultiBoxAdaptor FindSliver() =>
            Assert.Single(FindDescendants<RenderSliverMultiBoxAdaptor>(RenderView));

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget)
            {
                _view = view;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            public void Replace(Widget widget)
            {
                Update(widget);
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
