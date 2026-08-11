using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialCarouselTests
{
    [Fact]
    public void CarouselView_ValidatesFixedWeightedAndBuilderContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CarouselView(0, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => CarouselView.Weighted([], []));
        Assert.Throws<ArgumentOutOfRangeException>(() => CarouselView.Weighted([1, 0], []));
        Assert.Throws<ArgumentOutOfRangeException>(() => CarouselView.Builder(50, (_, _) => new SizedBox(), itemCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CarouselController(-1));

        CarouselView fixedCarousel = new(80, [new SizedBox()]);
        CarouselView weightedCarousel = CarouselView.Weighted([1, 7, 1], [new SizedBox(), new SizedBox(), new SizedBox()]);
        CarouselView lazyCarousel = CarouselView.WeightedBuilder([1, 2], (_, index) => new Text(index.ToString()), itemCount: 20);

        Assert.Equal(80, fixedCarousel.ItemExtent);
        Assert.Null(fixedCarousel.FlexWeights);
        Assert.Null(weightedCarousel.ItemExtent);
        Assert.Equal([1, 7, 1], weightedCarousel.FlexWeights);
        Assert.NotNull(lazyCarousel.ItemBuilder);
        Assert.Equal(20, lazyCarousel.ItemCount);
    }

    [Fact]
    public void CarouselViewTheme_UsesLocalThenThemeDataThenWidgetValues()
    {
        Color themeColor = Color.Parse("#FFE0F2F1");
        Color localColor = Color.Parse("#FFFFF3E0");
        Color widgetColor = Color.Parse("#FFE8F5E9");
        ThemeData theme = ThemeData.Light with
        {
            CarouselViewTheme = new CarouselViewThemeData(
                BackgroundColor: themeColor,
                Shape: ShapeBorder.RoundedRectangle(12),
                Padding: new Thickness(6)),
        };

        using WidgetRenderHarness themeHarness = new(Wrap(
            new CarouselView(80, [new SizedBox()]), theme));
        themeHarness.Pump(new Size(240, 100));
        RenderDecoratedBox themeSurface = Assert.Single(FindDescendants<RenderDecoratedBox>(themeHarness.RenderView));
        Assert.Equal(themeColor, themeSurface.Decoration.Color);
        Assert.Equal(12, themeSurface.Decoration.EffectiveBorderRadius.Radius);

        using WidgetRenderHarness localHarness = new(Wrap(
            new CarouselViewTheme(
                new CarouselViewThemeData(BackgroundColor: localColor, Shape: ShapeBorder.RoundedRectangle(18)),
                new CarouselView(80, [new SizedBox()])),
            theme));
        localHarness.Pump(new Size(240, 100));
        RenderDecoratedBox localSurface = Assert.Single(FindDescendants<RenderDecoratedBox>(localHarness.RenderView));
        Assert.Equal(localColor, localSurface.Decoration.Color);
        Assert.Equal(18, localSurface.Decoration.EffectiveBorderRadius.Radius);

        using WidgetRenderHarness widgetHarness = new(Wrap(
            new CarouselView(80, [new SizedBox()], backgroundColor: widgetColor, shape: ShapeBorder.RoundedRectangle(6)),
            theme));
        widgetHarness.Pump(new Size(240, 100));
        RenderDecoratedBox widgetSurface = Assert.Single(FindDescendants<RenderDecoratedBox>(widgetHarness.RenderView));
        Assert.Equal(widgetColor, widgetSurface.Decoration.Color);
        Assert.Equal(6, widgetSurface.Decoration.EffectiveBorderRadius.Radius);
    }

    [Fact]
    public void CarouselController_UsesCarouselPositionAndReportsLeadingItem()
    {
        CarouselController controller = new(initialItem: 2);
        List<int> reported = [];
        using WidgetRenderHarness harness = new(Wrap(new CarouselView(
            80,
            [new SizedBox(), new SizedBox(), new SizedBox(), new SizedBox(), new SizedBox(), new SizedBox()],
            controller: controller,
            onIndexChanged: reported.Add)));

        harness.Pump(new Size(240, 100));
        harness.Pump(new Size(240, 100));

        CarouselScrollPosition position = Assert.IsType<CarouselScrollPosition>(controller.PrimaryPosition);
        Assert.Equal(160, position.Pixels, precision: 3);
        Assert.Equal(2, controller.LeadingItem);

        controller.JumpToItem(3);
        harness.Pump(new Size(240, 100));

        Assert.Equal(3, controller.LeadingItem);
        Assert.Contains(3, reported);

        controller.AnimateToItem(1, TimeSpan.Zero);
        harness.Pump(new Size(240, 100));
        Assert.Equal(1, controller.LeadingItem);
    }

    [Fact]
    public void CarouselScrollPosition_PreservesItemOnResizeAndSnapsToItemBoundaries()
    {
        CarouselScrollPosition position = new(
            initialItem: 2,
            itemExtent: 100,
            flexWeights: null,
            consumeMaxWeight: true,
            infinite: false,
            itemCount: 8,
            physics: new CarouselScrollPhysics());
        position.ApplyViewportDimension(300);
        position.ApplyContentDimensions(0, 500);

        Assert.Equal(200, position.Pixels, precision: 3);
        position.ApplyViewportDimension(240);
        Assert.Equal(200, position.Pixels, precision: 3);

        Simulation? simulation = position.Physics.CreateBallisticSimulation(position, 80);
        Assert.NotNull(simulation);
        Assert.Equal(300, simulation!.X(10), precision: 0);
    }

    [Fact]
    public void VariableExtentLayouts_MatchFixedCompressionAndWeightedInterpolation()
    {
        SliverConstraints fixedConstraints = new(Axis.Horizontal, 30, 240, 100, 240, RemainingCacheExtent: 240);
        FixedCarouselLayout fixedLayout = new(100, 20, infinite: false);
        Assert.Equal(70, fixedLayout.GetChildMainAxisExtent(fixedConstraints, 0), precision: 3);
        Assert.Equal(70, fixedLayout.GetChildMainAxisExtent(fixedConstraints, 2), precision: 3);
        Assert.Equal(30, fixedLayout.GetChildLayoutOffset(fixedConstraints, 0), precision: 3);

        SliverConstraints weightedConstraints = new(Axis.Horizontal, 30, 800, 100, 800, RemainingCacheExtent: 800);
        WeightedCarouselLayout weightedLayout = new([1, 6, 1], 0, consumeMaxWeight: false, infinite: false);
        Assert.Equal(70, weightedLayout.GetChildMainAxisExtent(weightedConstraints, 0), precision: 3);
        Assert.Equal(450, weightedLayout.GetChildMainAxisExtent(weightedConstraints, 1), precision: 3);
        Assert.Equal(250, weightedLayout.GetChildMainAxisExtent(weightedConstraints, 2), precision: 3);
    }

    private static Widget Wrap(Widget child, ThemeData? theme = null) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(new Size(360, 640)),
            new Theme(theme ?? ThemeData.Light, child)));

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

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly RootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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
