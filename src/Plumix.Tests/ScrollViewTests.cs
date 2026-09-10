using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scroll_view.dart (parity tests)

namespace Plumix.Tests;

/// <summary>
/// Covers the <see cref="ScrollView"/>/<see cref="BoxScrollView"/> hierarchy: the physics that the
/// base constructor derives, the sliver each child layout produces, the semantic child counts and
/// the <see cref="MediaQuery"/> padding split that <see cref="BoxScrollView.BuildSlivers"/> performs.
/// </summary>
public sealed class ScrollViewTests
{
    // -------------------------------------------------------------------------------------------
    // Derived physics (Flutter: "Primary ListViews are always scrollable" and friends)
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void PrimaryScrollViews_AreAlwaysScrollable()
    {
        Assert.IsType<AlwaysScrollableScrollPhysics>(new ListView(primary: true).Physics);
    }

    [Fact]
    public void NonPrimaryScrollViews_AreNotAlwaysScrollable()
    {
        Assert.Null(new ListView(primary: false).Physics);
    }

    [Fact]
    public void VerticalScrollViewsWithoutControllerOrPrimary_AreAlwaysScrollable()
    {
        Assert.IsType<AlwaysScrollableScrollPhysics>(new ListView().Physics);
        Assert.IsType<AlwaysScrollableScrollPhysics>(new CustomScrollView().Physics);
        Assert.IsType<AlwaysScrollableScrollPhysics>(GridView.Count(crossAxisCount: 1).Physics);
    }

    [Fact]
    public void HorizontalScrollViews_AreNotAlwaysScrollable()
    {
        Assert.Null(new ListView(scrollDirection: Axis.Horizontal).Physics);
    }

    [Fact]
    public void ScrollViewsWithAController_AreNotAlwaysScrollable()
    {
        using var controller = new ScrollController();
        Assert.Null(new ListView(controller: controller).Physics);
    }

    [Fact]
    public void ExplicitPhysics_OverridesTheDerivedDefault()
    {
        var physics = new AlwaysScrollableScrollPhysics();
        Assert.Same(physics, new ListView(primary: false, physics: physics).Physics);

        var clamping = new ClampingScrollPhysics();
        Assert.Same(clamping, new ListView(primary: true, physics: clamping).Physics);
    }

    // -------------------------------------------------------------------------------------------
    // Constructor assertions
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void PrimaryScrollView_WithAnExplicitController_Throws()
    {
        using var controller = new ScrollController();
        Assert.Throws<ArgumentException>(() => new ListView(primary: true, controller: controller));
    }

    [Fact]
    public void ShrinkWrappingScrollView_WithACenter_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new CustomScrollView(shrinkWrap: true, center: new ValueKey<string>("center")));
    }

    [Fact]
    public void ScrollView_WithAnAnchorOutsideTheUnitRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CustomScrollView(anchor: 1.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CustomScrollView(anchor: -0.5));
    }

    [Fact]
    public void ScrollView_WithANegativeSemanticChildCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CustomScrollView(semanticChildCount: -1));
    }

    [Fact]
    public void ListViewBuilder_WithASemanticChildCountAboveItemCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ListView.Builder(static (_, _) => new SizedBox(), itemCount: 2, semanticChildCount: 3));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ListView.Builder(static (_, _) => new SizedBox(), itemCount: -1));
    }

    [Fact]
    public void ListView_WithTwoExtentStrategies_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new ListView(itemExtent: 10, prototypeItem: new SizedBox()));
        Assert.Throws<ArgumentException>(
            () => new ListView(itemExtent: 10, itemExtentBuilder: static (_, _) => 10));
        Assert.Throws<ArgumentException>(
            () => ListView.Custom(
                new SliverChildListDelegate([]),
                prototypeItem: new SizedBox(),
                itemExtentBuilder: static (_, _) => 10));
    }

    // -------------------------------------------------------------------------------------------
    // Semantic child counts
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void SemanticChildCount_DefaultsToTheChildOrItemCount()
    {
        Assert.Equal(3, new ListView(children: [new SizedBox(), new SizedBox(), new SizedBox()])
            .SemanticChildCount);
        Assert.Equal(7, ListView.Builder(static (_, _) => new SizedBox(), itemCount: 7).SemanticChildCount);
        Assert.Null(ListView.Builder(static (_, _) => new SizedBox()).SemanticChildCount);
        Assert.Equal(2, new GridView(
            new SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2),
            children: [new SizedBox(), new SizedBox()]).SemanticChildCount);
    }

    [Fact]
    public void SeparatedListView_ReportsItemCountAndBuildsTwoChildrenPerItem()
    {
        ListView list = ListView.Separated(
            itemCount: 4,
            itemBuilder: static (_, index) => new SizedBox(width: index),
            separatorBuilder: static (_, _) => new SizedBox());

        Assert.Equal(4, list.SemanticChildCount);
        Assert.Equal(7, list.ChildrenDelegate.EstimatedChildCount);
    }

    // -------------------------------------------------------------------------------------------
    // buildChildLayout precedence
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void ListView_ChildLayout_PrefersItemExtentThenBuilderThenPrototype()
    {
        using var harness = new ScrollViewProbeHarness();

        Assert.IsType<SliverFixedExtentList>(
            harness.BuildChildLayout(new ListView(itemExtent: 24)));
        Assert.IsType<SliverVariedExtentList>(
            harness.BuildChildLayout(new ListView(itemExtentBuilder: static (_, _) => 24)));
        Assert.IsType<SliverPrototypeExtentList>(
            harness.BuildChildLayout(new ListView(prototypeItem: new SizedBox(height: 24))));
        Assert.IsType<SliverList>(harness.BuildChildLayout(new ListView()));
    }

    [Fact]
    public void GridView_ChildLayout_IsASliverGridOverTheGivenDelegates()
    {
        using var harness = new ScrollViewProbeHarness();
        var gridDelegate = new SliverGridDelegateWithMaxCrossAxisExtent(maxCrossAxisExtent: 100);
        var childrenDelegate = new SliverChildListDelegate([new SizedBox()]);

        var grid = Assert.IsType<SliverGrid>(
            harness.BuildChildLayout(GridView.Custom(gridDelegate, childrenDelegate)));
        Assert.Same(gridDelegate, grid.GridDelegate);
        Assert.Same(childrenDelegate, grid.Delegate);
    }

    [Fact]
    public void GridViewCountAndExtent_BuildTheMatchingGridDelegates()
    {
        var count = Assert.IsType<SliverGridDelegateWithFixedCrossAxisCount>(
            GridView.Count(crossAxisCount: 3, mainAxisSpacing: 4, crossAxisSpacing: 5).GridDelegate);
        Assert.Equal(3, count.CrossAxisCount);
        Assert.Equal(4, count.MainAxisSpacing);
        Assert.Equal(5, count.CrossAxisSpacing);

        var extent = Assert.IsType<SliverGridDelegateWithMaxCrossAxisExtent>(
            GridView.Extent(maxCrossAxisExtent: 120, mainAxisExtent: 40).GridDelegate);
        Assert.Equal(120, extent.MaxCrossAxisExtent);
        Assert.Equal(40, extent.MainAxisExtent);
    }

    [Fact]
    public void ListViewCustom_KeepsTheSuppliedDelegate()
    {
        var childrenDelegate = new SliverChildListDelegate([new SizedBox()]);
        Assert.Same(childrenDelegate, ListView.Custom(childrenDelegate).ChildrenDelegate);
    }

    // -------------------------------------------------------------------------------------------
    // BoxScrollView padding
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void ExplicitPadding_WrapsTheChildLayoutInASliverPadding()
    {
        using var harness = new ScrollViewProbeHarness();
        var padding = new Thickness(4, 8, 12, 16);

        Widget sliver = Assert.Single(harness.BuildSlivers(new ListView(padding: padding)));
        var sliverPadding = Assert.IsType<SliverPadding>(sliver);
        Assert.Equal(padding, sliverPadding.Padding);
        Assert.IsType<SliverList>(sliverPadding.Child);
    }

    [Fact]
    public void WithoutPadding_TheMainAxisMediaQueryPaddingIsConsumedAndTheCrossAxisPaddingRepublished()
    {
        using var harness = new ScrollViewProbeHarness(
            new MediaQueryData(Padding: new Thickness(30, 30, 30, 30)));

        Widget sliver = Assert.Single(harness.BuildSlivers(new ListView()));
        var sliverPadding = Assert.IsType<SliverPadding>(sliver);
        // A vertical list consumes the vertical padding.
        Assert.Equal(new Thickness(0, 30, 0, 30), sliverPadding.Padding);
        // ... and republishes the horizontal padding to its children.
        var republished = Assert.IsType<MediaQuery>(sliverPadding.Child);
        Assert.Equal(new Thickness(30, 0, 30, 0), republished.Data.Padding);
        Assert.IsType<SliverList>(republished.Child);
    }

    [Fact]
    public void WithoutPadding_AHorizontalListSwapsTheConsumedAxis()
    {
        using var harness = new ScrollViewProbeHarness(
            new MediaQueryData(Padding: new Thickness(30, 30, 30, 30)));

        Widget sliver = Assert.Single(
            harness.BuildSlivers(new ListView(scrollDirection: Axis.Horizontal)));
        var sliverPadding = Assert.IsType<SliverPadding>(sliver);
        Assert.Equal(new Thickness(30, 0, 30, 0), sliverPadding.Padding);
        var republished = Assert.IsType<MediaQuery>(sliverPadding.Child);
        Assert.Equal(new Thickness(0, 30, 0, 30), republished.Data.Padding);
    }

    [Fact]
    public void WithoutPaddingAndWithoutAMediaQuery_NoSliverPaddingIsInserted()
    {
        using var harness = new ScrollViewProbeHarness();
        Assert.IsType<SliverList>(Assert.Single(harness.BuildSlivers(new ListView())));
    }

    // -------------------------------------------------------------------------------------------
    // Diagnostics
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void DebugFillProperties_ReportsTheScrollViewConfiguration()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new ListView(
            scrollDirection: Axis.Horizontal,
            reverse: true,
            shrinkWrap: true,
            padding: new Thickness(3),
            itemExtent: 12).DebugFillProperties(properties);

        List<string> names = properties.Properties.Select(property => property.Name!).ToList();
        Assert.Contains("scrollDirection", names);
        Assert.Contains("reverse", names);
        Assert.Contains("shrinkWrap", names);
        Assert.Contains("padding", names);
        Assert.Contains("itemExtent", names);
    }

    /// <summary>
    /// Runs a <see cref="BoxScrollView"/>'s sliver-building hooks inside a mounted element, so the
    /// <see cref="MediaQuery"/> lookups they perform resolve against a real tree.
    /// </summary>
    private sealed class ScrollViewProbeHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly ProbeRoot _root;

        public ScrollViewProbeHarness(MediaQueryData? mediaQuery = null)
        {
            Widget probe = new Probe(context => Context = context);
            if (mediaQuery is { } data)
            {
                probe = new MediaQuery(data, probe);
            }

            _root = new ProbeRoot(new Directionality(TextDirection.Ltr, probe));
            _root.Attach(_owner);
            _owner.BuildScope(_root, () => _root.Mount(parent: null, newSlot: null));
            _owner.FlushBuild();
        }

        private BuildContext Context { get; set; } = null!;

        public IReadOnlyList<Widget> BuildSlivers(ScrollView view) => view.BuildSlivers(Context);

        public Widget BuildChildLayout(BoxScrollView view) => view.BuildChildLayout(Context);

        public void Dispose() => _root.UnmountRoot();

        private sealed class Probe : StatelessWidget
        {
            private readonly Action<BuildContext> _onBuilt;

            public Probe(Action<BuildContext> onBuilt)
            {
                _onBuilt = onBuilt;
            }

            public override Widget Build(BuildContext context)
            {
                _onBuilt(context);
                return new SizedBox();
            }
        }

        private sealed class ProbeRoot : Element, IRenderObjectHost
        {
            private Element? _child;

            public ProbeRoot(Widget widget) : base(widget)
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

        }
    }
}
