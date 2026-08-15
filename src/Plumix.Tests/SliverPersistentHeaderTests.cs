using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Covers the persistent-header render hierarchy (scrolling / pinned / floating / floating-pinned),
/// its stretch and snap configurations, and the reveal-driven expansion, against Flutter's own
/// asserted behavior in <c>rendering/sliver_persistent_header.dart</c> and
/// <c>widgets/sliver_persistent_header.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class SliverPersistentHeaderTests : IDisposable
{
    private const double ViewportExtent = 600.0;

    public SliverPersistentHeaderTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void Configurations_ExposeFlutterDefaults()
    {
        var stretch = new OverScrollHeaderStretchConfiguration();
        Assert.Equal(100.0, stretch.StretchTriggerOffset);
        Assert.Null(stretch.OnStretchTrigger);

        var snap = new FloatingHeaderSnapConfiguration();
        Assert.Equal(TimeSpan.FromMilliseconds(300), snap.Duration);
        Assert.Equal(Curves.Ease(0.4), snap.Curve(0.4));

        var showOnScreen = new PersistentHeaderShowOnScreenConfiguration();
        Assert.Equal(double.NegativeInfinity, showOnScreen.MinShowOnScreenExtent);
        Assert.Equal(double.PositiveInfinity, showOnScreen.MaxShowOnScreenExtent);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PersistentHeaderShowOnScreenConfiguration(200.0, 100.0));
    }

    [Fact]
    public void ScrollingHeader_ShrinksThenScrollsOffWithFlutterGeometry()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400)));
        var header = new RenderSliverScrollingPersistentHeader(60, 200, child: child);

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0));
        Assert.Equal(200, child.Size.Height, precision: 3);
        Assert.Equal(200, header.Geometry.ScrollExtent, precision: 3);
        Assert.Equal(200, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(200, header.Geometry.MaxPaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.MaxScrollObstructionExtent, precision: 3);
        Assert.Equal(0, header.ChildMainAxisPosition(child), precision: 3);
        Assert.True(header.Geometry.HasVisualOverflow);

        // Shrinking: the child collapses toward minExtent and stays at the leading edge.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 150));
        Assert.Equal(60, child.Size.Height, precision: 3);
        Assert.Equal(50, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(-10, header.ChildMainAxisPosition(child), precision: 3);

        // Fully scrolled off: paintExtent clamps at zero, but the sliver keeps its scroll extent.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 260));
        Assert.Equal(0, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(200, header.Geometry.ScrollExtent, precision: 3);
    }

    [Fact]
    public void ScrollingHeader_PaintOriginFollowsNegativeOverlapOnly()
    {
        var header = new RenderSliverScrollingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))));

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -40));
        Assert.Equal(-40, header.Geometry.PaintOrigin, precision: 3);

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: 40));
        Assert.Equal(0, header.Geometry.PaintOrigin, precision: 3);
    }

    [Fact]
    public void PinnedHeader_HoldsMinExtentAndReportsObstruction()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400)));
        var header = new RenderSliverPinnedPersistentHeader(60, 200, child: child);

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 400));
        Assert.Equal(60, child.Size.Height, precision: 3);
        Assert.Equal(60, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.LayoutExtent, precision: 3);
        Assert.Equal(60, header.Geometry.MaxScrollObstructionExtent, precision: 3);
        Assert.Equal(0, header.ChildMainAxisPosition(child), precision: 3);

        // An incoming positive overlap is both the paint origin and the overlapsContent signal, and
        // it is discounted from the paint extent available to the header.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 400, overlap: 30));
        Assert.Equal(30, header.Geometry.PaintOrigin, precision: 3);
        Assert.True(header.LastOverlapsContent);
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 400, remainingPaintExtent: 40, overlap: 30));
        Assert.Equal(10, header.Geometry.PaintExtent, precision: 3);
    }

    [Fact]
    public void FloatingHeader_OnlyExpandsWhenTheUserScrollsForward()
    {
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))));

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 500));
        Assert.Equal(500, header.EffectiveScrollOffset);
        Assert.Equal(0, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.MaxScrollObstructionExtent, precision: 3);

        // Reverse (or idle) user scroll: no floating expansion, the header stays hidden.
        header.LayoutWithSliverConstraints(Constraints(
            scrollOffset: 460,
            userScrollDirection: ScrollDirection.Reverse));
        Assert.Equal(460, header.EffectiveScrollOffset);
        Assert.Equal(0, header.Geometry.PaintExtent, precision: 3);

        // Forward user scroll: the effective offset is pulled back to maxExtent and then floats in
        // by the scrolled delta.
        header.LayoutWithSliverConstraints(Constraints(
            scrollOffset: 420,
            userScrollDirection: ScrollDirection.Forward));
        Assert.Equal(160, header.EffectiveScrollOffset);
        Assert.Equal(40, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.LayoutExtent, precision: 3);
        Assert.True(header.LastOverlapsContent);
    }

    [Fact]
    public void FloatingHeader_ExpandsAfterAForwardGestureStartedEvenWhenTheDirectionIsIdle()
    {
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))));
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 500));

        header.UpdateScrollStartDirection(ScrollDirection.Forward);
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 470));
        Assert.Equal(170, header.EffectiveScrollOffset);
        Assert.Equal(30, header.Geometry.PaintExtent, precision: 3);
    }

    [Fact]
    public void FloatingPinnedHeader_KeepsMinExtentVisibleAndReportsObstruction()
    {
        var header = new RenderSliverFloatingPinnedPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))));

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 500));
        Assert.Equal(60, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.LayoutExtent, precision: 3);
        Assert.Equal(60, header.Geometry.MaxScrollObstructionExtent, precision: 3);

        // Regression for flutter/flutter#21887: less remaining paint extent than minExtent must not
        // grow the header past what is left.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 500, remainingPaintExtent: 50));
        Assert.Equal(50, header.Geometry.PaintExtent, precision: 3);
        Assert.Equal(0, header.Geometry.LayoutExtent, precision: 3);
    }

    [Fact]
    public void Stretch_GrowsTheChildIntoTheLeadingOverscrollOnly()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400)));
        var header = new RenderSliverScrollingPersistentHeader(
            60,
            200,
            child: child,
            stretchConfiguration: new OverScrollHeaderStretchConfiguration());

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -100));
        Assert.Equal(300, child.Size.Height, precision: 3);
        Assert.Equal(300, header.Geometry.MaxPaintExtent, precision: 3);
        Assert.Equal(0, header.ChildMainAxisPosition(child), precision: 3);

        // The stretch itself only applies at scroll offset zero.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 20, overlap: -100));
        Assert.Equal(180, child.Size.Height, precision: 3);
        Assert.Equal(300, header.Geometry.MaxPaintExtent, precision: 3);
    }

    [Fact]
    public void Stretch_WithoutOverscrollLeavesTheChildAtMaxExtent()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400)));
        var header = new RenderSliverPinnedPersistentHeader(
            60,
            200,
            child: child,
            stretchConfiguration: new OverScrollHeaderStretchConfiguration());

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0));
        Assert.Equal(200, child.Size.Height, precision: 3);
        Assert.Equal(200, header.Geometry.MaxPaintExtent, precision: 3);
    }

    [Theory]
    [InlineData(100.0, 50.0, 150.0)]
    [InlineData(150.0, 100.0, 300.0)]
    public void StretchTrigger_FiresOnceWhenCrossingTheTriggerOffset(
        double triggerOffset,
        double belowOverscroll,
        double aboveOverscroll)
    {
        int calls = 0;
        var header = new RenderSliverScrollingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            stretchConfiguration: new OverScrollHeaderStretchConfiguration(
                stretchTriggerOffset: triggerOffset,
                onStretchTrigger: () =>
                {
                    calls++;
                    return Task.CompletedTask;
                }));

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -belowOverscroll));
        Assert.Equal(0, calls);

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -aboveOverscroll));
        Assert.Equal(1, calls);

        // Staying past the trigger must not fire again; the trigger is edge-driven.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -(aboveOverscroll + 10)));
        Assert.Equal(1, calls);

        // Releasing back below the trigger re-arms it.
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -belowOverscroll));
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0, overlap: -aboveOverscroll));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void StretchTrigger_DoesNotFireWithoutOverscroll()
    {
        int calls = 0;
        var header = new RenderSliverScrollingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            stretchConfiguration: new OverScrollHeaderStretchConfiguration(
                onStretchTrigger: () =>
                {
                    calls++;
                    return Task.CompletedTask;
                }));

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0));
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 120));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Snap_IsSkippedWhenTheHeaderIsAlreadyFullyShownOrHidden()
    {
        var vsync = new TestTickerProvider();
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            vsync: vsync,
            snapConfiguration: new FloatingHeaderSnapConfiguration());

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 0));
        header.MaybeStartSnapAnimation(ScrollDirection.Forward);
        Assert.Equal(0, header.EffectiveScrollOffset);

        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 400));
        header.MaybeStartSnapAnimation(ScrollDirection.Reverse);
        Assert.Equal(400, header.EffectiveScrollOffset);
    }

    [Fact]
    public void Snap_WithoutAConfigurationDoesNothing()
    {
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            vsync: new TestTickerProvider());
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 500));

        // No snapConfiguration: the header must not touch its effective offset, and must not need a
        // ticker at all.
        header.MaybeStartSnapAnimation(ScrollDirection.Forward);
        Assert.Equal(500, header.EffectiveScrollOffset);
    }

    [Fact]
    public void ShowOnScreenConfiguration_ExpandsAFloatingHeaderToItsFullExtent()
    {
        var vsync = new TestTickerProvider();
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            vsync: vsync,
            showOnScreenConfiguration: new PersistentHeaderShowOnScreenConfiguration(
                minShowOnScreenExtent: double.PositiveInfinity));
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 180));
        Assert.Equal(20, header.Geometry.PaintExtent, precision: 3);

        header.ShowOnScreen(duration: TimeSpan.FromMilliseconds(100));
        vsync.Advance(TimeSpan.FromMilliseconds(200));
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 180));

        // SliverAppBar's snap configuration asks for an infinite extent, which clamps to the full
        // header: the effective offset animates all the way back to zero.
        Assert.Equal(0, header.EffectiveScrollOffset!.Value, precision: 3);
        Assert.Equal(200, header.Geometry.PaintExtent, precision: 3);
    }

    [Fact]
    public void ShowOnScreen_WithoutAConfigurationLeavesTheHeaderToTheViewport()
    {
        var header = new RenderSliverFloatingPersistentHeader(
            60,
            200,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(300, 400))),
            vsync: new TestTickerProvider());
        header.LayoutWithSliverConstraints(Constraints(scrollOffset: 180));

        header.ShowOnScreen();
        Assert.Equal(180, header.EffectiveScrollOffset);
        Assert.Equal(20, header.Geometry.PaintExtent, precision: 3);
    }

    [Theory]
    [InlineData(false, false, typeof(RenderSliverScrollingPersistentHeader))]
    [InlineData(true, false, typeof(RenderSliverPinnedPersistentHeader))]
    [InlineData(false, true, typeof(RenderSliverFloatingPersistentHeader))]
    [InlineData(true, true, typeof(RenderSliverFloatingPinnedPersistentHeader))]
    public void SliverPersistentHeader_SelectsTheRenderObjectForItsMode(
        bool pinned,
        bool floating,
        Type expected)
    {
        using var harness = new SliverHarness(new SliverPersistentHeader(
            new TestHeaderDelegate(60, 200),
            pinned: pinned,
            floating: floating));
        harness.Pump();

        RenderSliverPersistentHeader header = harness.Header;
        Assert.Equal(expected, header.GetType());
        Assert.Equal(60, header.MinExtent, precision: 3);
        Assert.Equal(200, header.MaxExtent, precision: 3);
    }

    [Fact]
    public void SliverPersistentHeader_BuildsItsChildFromTheShrinkOffsetDuringLayout()
    {
        var shrinkOffsets = new List<double>();
        var @delegate = new TestHeaderDelegate(60, 200, shrinkOffsets.Add);
        using var harness = new SliverHarness(new SliverPersistentHeader(@delegate, pinned: true));
        harness.Pump();
        Assert.Equal(0, Assert.Single(shrinkOffsets), precision: 3);

        harness.ScrollTo(150);
        harness.Pump();
        Assert.Equal(150, shrinkOffsets[^1], precision: 3);
        Assert.Equal(150, harness.Header.LastShrinkOffset, precision: 3);
        Assert.Equal(60, harness.Header.Child!.Size.Height, precision: 3);
    }

    [Fact]
    public void SliverPersistentHeader_PushesUpdatedConfigurationsOnRebuild()
    {
        var first = new TestHeaderDelegate(60, 200)
        {
            Stretch = new OverScrollHeaderStretchConfiguration(stretchTriggerOffset: 10),
            ShowOnScreen = new PersistentHeaderShowOnScreenConfiguration(maxShowOnScreenExtent: 1000),
        };
        using var harness = new SliverHarness(new SliverPersistentHeader(first, pinned: true));
        harness.Pump();
        var pinnedHeader = Assert.IsType<RenderSliverPinnedPersistentHeader>(harness.Header);
        Assert.Equal(10, pinnedHeader.StretchConfiguration!.StretchTriggerOffset);
        Assert.Equal(1000, pinnedHeader.ShowOnScreenConfiguration!.MaxShowOnScreenExtent);

        harness.Replace(new SliverPersistentHeader(
            new TestHeaderDelegate(60, 200)
            {
                Stretch = new OverScrollHeaderStretchConfiguration(stretchTriggerOffset: 20),
                ShowOnScreen = new PersistentHeaderShowOnScreenConfiguration(maxShowOnScreenExtent: 2000),
            },
            pinned: true));
        harness.Pump();
        pinnedHeader = Assert.IsType<RenderSliverPinnedPersistentHeader>(harness.Header);
        Assert.Equal(20, pinnedHeader.StretchConfiguration!.StretchTriggerOffset);
        Assert.Equal(2000, pinnedHeader.ShowOnScreenConfiguration!.MaxShowOnScreenExtent);
    }

    [Fact]
    public void SliverPersistentHeader_ValidatesTheDelegateExtents()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SliverPersistentHeader(new TestHeaderDelegate(200, 100)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SliverPersistentHeader(new TestHeaderDelegate(-1, 100)));
        Assert.Throws<ArgumentNullException>(() => new SliverPersistentHeader(null!));
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, true, true, true, true)]
    public void SliverAppBar_ConfiguresTheDelegateFromSnapFloatingAndStretch(
        bool floating,
        bool snap,
        bool stretch,
        bool expectsSnapConfiguration,
        bool expectsStretchConfiguration)
    {
        using var harness = new SliverHarness(new SliverAppBar(
            titleText: "Title",
            floating: floating,
            snap: snap,
            stretch: stretch));
        harness.Pump();

        var header = harness.Header;
        FloatingHeaderSnapConfiguration? snapConfiguration =
            (header as RenderSliverFloatingPersistentHeader)?.SnapConfiguration;
        PersistentHeaderShowOnScreenConfiguration? showOnScreen =
            (header as RenderSliverFloatingPersistentHeader)?.ShowOnScreenConfiguration;

        Assert.Equal(expectsSnapConfiguration, snapConfiguration is not null);
        // Flutter builds the show-on-screen configuration exactly when the bar both floats and snaps.
        Assert.Equal(expectsSnapConfiguration, showOnScreen is not null);
        Assert.Equal(expectsStretchConfiguration, header.StretchConfiguration is not null);
        if (snapConfiguration is not null)
        {
            Assert.Equal(TimeSpan.FromMilliseconds(200), snapConfiguration.Duration);
            Assert.Equal(Curves.EaseOut(0.4), snapConfiguration.Curve(0.4));
            Assert.Equal(double.PositiveInfinity, showOnScreen!.MinShowOnScreenExtent);
        }

        if (header is RenderSliverFloatingPersistentHeader floatingHeader)
        {
            Assert.NotNull(floatingHeader.Vsync);
        }
    }

    [Fact]
    public void SliverAppBar_ForwardsTheStretchTriggerToTheHeader()
    {
        using var harness = new SliverHarness(new SliverAppBar(
            titleText: "Title",
            stretch: true,
            stretchTriggerOffset: 42));
        harness.Pump();

        Assert.Equal(42, harness.Header.StretchConfiguration!.StretchTriggerOffset, precision: 3);
    }

    private static SliverConstraints Constraints(
        double scrollOffset,
        double remainingPaintExtent = ViewportExtent,
        double overlap = 0.0,
        ScrollDirection userScrollDirection = ScrollDirection.Idle) => new(
        Axis.Vertical,
        scrollOffset,
        remainingPaintExtent,
        CrossAxisExtent: 300,
        ViewportMainAxisExtent: ViewportExtent,
        RemainingCacheExtent: ViewportExtent,
        Overlap: overlap,
        UserScrollDirection: userScrollDirection);

    private sealed class TestTickerProvider : ITickerProvider
    {
        private readonly List<Ticker> _tickers = [];

        public Ticker CreateTicker(TickerCallback onTick)
        {
            var ticker = new Ticker(onTick);
            _tickers.Add(ticker);
            return ticker;
        }

        public void Advance(TimeSpan elapsed)
        {
            double now = Scheduler.CurrentSeconds;
            // The first frame only gives a freshly started ticker its start timestamp.
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now) + elapsed);
        }
    }

    private sealed class TestHeaderDelegate : SliverPersistentHeaderDelegate
    {
        private readonly Action<double>? _onBuild;

        public TestHeaderDelegate(double minExtent, double maxExtent, Action<double>? onBuild = null)
        {
            MinExtent = minExtent;
            MaxExtent = maxExtent;
            _onBuild = onBuild;
        }

        public override double MinExtent { get; }

        public override double MaxExtent { get; }

        public OverScrollHeaderStretchConfiguration? Stretch { get; init; }

        public PersistentHeaderShowOnScreenConfiguration? ShowOnScreen { get; init; }

        public override OverScrollHeaderStretchConfiguration? StretchConfiguration => Stretch;

        public override PersistentHeaderShowOnScreenConfiguration? ShowOnScreenConfiguration => ShowOnScreen;

        public override Widget Build(BuildContext context, double shrinkOffset, bool overlapsContent)
        {
            _onBuild?.Invoke(shrinkOffset);
            // Tall enough to always be squeezed into whatever extent the header hands it, the way a
            // real header's content fills the box it is given.
            return new SizedBox(height: 1000, child: new ColoredBox(Colors.Teal));
        }

        public override bool ShouldRebuild(SliverPersistentHeaderDelegate oldDelegate) => true;
    }

    private sealed class SliverHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly RootElement _root;
        private readonly ScrollController _controller = new();

        public SliverHarness(Widget sliver)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new RootElement(RenderView, Wrap(sliver));
            _root.Attach(_owner);
            _root.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public RenderSliverPersistentHeader Header =>
            Find<RenderSliverPersistentHeader>(RenderView)
            ?? throw new InvalidOperationException("No persistent header in the tree.");

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(new Size(300, ViewportExtent));
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void ScrollTo(double offset) => _controller.JumpTo(offset);

        public void Replace(Widget sliver) => _root.Replace(Wrap(sliver));

        public void Dispose() => _root.Unmount();

        private Widget Wrap(Widget sliver) => new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(300, ViewportExtent)),
                new Theme(
                    ThemeData.Light,
                    new CustomScrollView(
                        controller: _controller,
                        slivers: [sliver, new SliverToBoxAdapter(new SizedBox(height: 2000))]))));

        private static T? Find<T>(RenderObject? root) where T : RenderObject
        {
            if (root is null)
            {
                return null;
            }

            if (root is T value)
            {
                return value;
            }

            T? found = null;
            root.VisitChildren(child => found ??= Find<T>(child));
            return found;
        }

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;

            public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;

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

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _view.Child = (RenderBox)child;

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
